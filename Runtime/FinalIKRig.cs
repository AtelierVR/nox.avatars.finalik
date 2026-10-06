#if HAS_FINALIK
using Nox.CCK.Avatars.Rigging;
using Nox.CCK.Utils;
using RootMotion.FinalIK;
using UnityEngine;
using static Nox.Avatars.FinalIK.VRIKWeightParameter;

namespace Nox.Avatars.FinalIK {
	public class FinalIKRig : BaseRigging {
		private VRIK _rig;

		/// <summary>Réglages de locomotion mis de côté quand un tracker de bassin prend la main.</summary>
		private float? _rootAngleStanding;
		private float? _rootAngleMoving;

		/// <summary>Le bassin est piloté par un tracker.</summary>
		private bool _hipsTracked;

		/// <summary>Le jeu pilote la racine (calibration full-body) — voir <see cref="SetExternalRootControl"/>.</summary>
		private bool _externalRootControl;

		/// <summary>Locomotion / angle de racine mis de côté tant que le rig ne doit pas déplacer la racine.</summary>
		private float? _savedLocomotionWeight;
		private float? _savedMaxRootAngle;

		/// <summary>Contrôleur de racine (bassin) installé quand un tracker de bassin prend la main.</summary>
		private VRIKRootController _rootController;

		public VRIK GetRig()
			=> _rig ??= Descriptor.Anchor?.GetOrAddComponent<VRIK>();

		public override bool SetupParameters(BaseRigging m) {
			if (m is not FinalIKRig module)
				return false;

			var rig = module.GetRig();
			if (!rig) return false;

			// Expose VRIK solver weights as parameters so controllers (Desktop/XR) can
			// enable/disable limbs and set position/rotation weights — same surface as RigBuilder.
			module.Parameters.Add(new VRIKWeightParameter("rig/ik/head/position_weight",     rig, HumanBodyBonesGroup.Head,     WeightType.Position));
			module.Parameters.Add(new VRIKWeightParameter("rig/ik/head/rotation_weight",     rig, HumanBodyBonesGroup.Head,     WeightType.Rotation));
			module.Parameters.Add(new VRIKWeightParameter("rig/ik/left_arm/position_weight",  rig, HumanBodyBonesGroup.LeftArm,  WeightType.Position));
			module.Parameters.Add(new VRIKWeightParameter("rig/ik/left_arm/rotation_weight",  rig, HumanBodyBonesGroup.LeftArm,  WeightType.Rotation));
			module.Parameters.Add(new VRIKWeightParameter("rig/ik/right_arm/position_weight", rig, HumanBodyBonesGroup.RightArm, WeightType.Position));
			module.Parameters.Add(new VRIKWeightParameter("rig/ik/right_arm/rotation_weight", rig, HumanBodyBonesGroup.RightArm, WeightType.Rotation));
			module.Parameters.Add(new VRIKWeightParameter("rig/ik/left_leg/position_weight",  rig, HumanBodyBonesGroup.LeftLeg,  WeightType.Position));
			module.Parameters.Add(new VRIKWeightParameter("rig/ik/left_leg/rotation_weight",  rig, HumanBodyBonesGroup.LeftLeg,  WeightType.Rotation));
			module.Parameters.Add(new VRIKWeightParameter("rig/ik/right_leg/position_weight", rig, HumanBodyBonesGroup.RightLeg, WeightType.Position));
			module.Parameters.Add(new VRIKWeightParameter("rig/ik/right_leg/rotation_weight", rig, HumanBodyBonesGroup.RightLeg, WeightType.Rotation));

			return true;
		}

		protected override bool GetActive(HumanBodyBones bone) {
			var rig = GetRig();
			if (!rig) return false;
			return bone switch {			
				HumanBodyBones.Hips         => rig.solver.spine.pelvisPositionWeight > 0f,				
				HumanBodyBones.Head         => rig.solver.spine.positionWeight    > 0f,
				HumanBodyBones.LeftHand     => rig.solver.leftArm.positionWeight  > 0f,
				HumanBodyBones.RightHand    => rig.solver.rightArm.positionWeight > 0f,
				HumanBodyBones.LeftFoot     => rig.solver.leftLeg.positionWeight  > 0f,
				HumanBodyBones.RightFoot    => rig.solver.rightLeg.positionWeight > 0f,
				_                           => false
			};
		}

		protected override void ApplyActive(HumanBodyBones bone, bool active) {
			var rig = GetRig();
			if (!rig) return;
			var w = active ? 1f : 0f;
			switch (bone) {
				// The hips are a separate target in VRIK: a 3-point rig keeps them animation-driven
				// (pelvis*Weight = 0), a waist tracker turns them into a real IK target.
				case HumanBodyBones.Hips:
					rig.solver.spine.pelvisPositionWeight = w;
					rig.solver.spine.pelvisRotationWeight = w;

					_hipsTracked = active;
					RefreshRootControl();

					// Un bassin tracké pilote la racine par le VRIKRootController (montage canonique de VRIK pour un body
					// tracker, VRIKCalibrator.Calibrate) ; il est créé au premier tracker de bassin puis activé/désactivé
					// par RefreshRootControl.
					if (_hipsTracked && !_rootController)
						_rootController = EnableRootController();
					break;
				case HumanBodyBones.Head:
					rig.solver.spine.positionWeight    = w;
					rig.solver.spine.rotationWeight    = w;
					break;
				case HumanBodyBones.LeftHand:
					rig.solver.leftArm.positionWeight  = w;
					rig.solver.leftArm.rotationWeight  = w;
					break;
				case HumanBodyBones.RightHand:
					rig.solver.rightArm.positionWeight = w;
					rig.solver.rightArm.rotationWeight = w;
					break;
				case HumanBodyBones.LeftFoot:
					// Le solveur de jambe applique le *cap* de la cible (§ VRIKCalibrator) : le driver envoie un
					// cap pur, on garde donc la rotation active pour que le pied suive le tracker.
					rig.solver.leftLeg.positionWeight  = w;
					rig.solver.leftLeg.rotationWeight  = w;
					break;
				case HumanBodyBones.RightFoot:
					rig.solver.rightLeg.positionWeight = w;
					rig.solver.rightLeg.rotationWeight = w;
					break;
			}
		}

		/// <summary>
		/// Le contrôleur (XR) prend la main sur la racine de l'avatar : voir
		/// <see cref="IRigging.SetExternalRootControl"/>. Utilisé pendant la calibration full-body, où le jeu pose
		/// la racine (cap = tête, XZ = tête ou espace de jeu).
		/// </summary>
		public override void SetExternalRootControl(bool external) {
			if (_externalRootControl == external)
				return;

			_externalRootControl = external;
			RefreshRootControl();
		}

		/// <summary>
		/// Recalcule qui pilote la racine. Dès que le rig ne doit PAS la déplacer (bassin tracké ou racine pilotée
		/// par le jeu) :
		/// <list type="bullet">
		/// <item>la locomotion VRIK est coupée — sinon elle déplace l'avatar d'après la tête (les pieds « glissent »
		/// quand on penche la tête) ;</item>
		/// <item>`spine.maxRootAngle` est forcé à 180 — sinon `IKSolverVRSpine.Solve` active son rattrapage d'angle
		/// de racine, qui tourne <b>et translate</b> la racine autour du pivot de l'Animator (`(0,0,0)` chez nos
		/// avatars) : l'avatar se téléporte à chaque frame (il se réactive quand la locomotion recopie
		/// `maxRootAngleStanding/Moving`).</item>
		/// </list>
		/// Le <see cref="VRIKRootController"/> (racine = bassin) n'est actif que pour un bassin tracké hors
		/// calibration.
		/// </summary>
		private void RefreshRootControl() {
			var rig = GetRig();
			if (!rig)
				return;

			var rigOwnsRoot = !_hipsTracked && !_externalRootControl;

			if (!rigOwnsRoot && !_savedLocomotionWeight.HasValue) {
				_savedLocomotionWeight = rig.solver.locomotion.weight;
				_savedMaxRootAngle    = rig.solver.spine.maxRootAngle;
			}

			if (!rigOwnsRoot) {
				rig.solver.locomotion.weight  = 0f;
				rig.solver.spine.maxRootAngle = 180f;
			} else if (_savedLocomotionWeight.HasValue) {
				rig.solver.locomotion.weight  = _savedLocomotionWeight.Value;
				rig.solver.spine.maxRootAngle = _savedMaxRootAngle ?? 180f;
				_savedLocomotionWeight = null;
				_savedMaxRootAngle     = null;
			}

			if (_rootController)
				_rootController.enabled = _hipsTracked && !_externalRootControl;
		}

		/// <summary>
		/// Installe (ou réactive) le <see cref="VRIKRootController"/> de l'ancre : il déplace la racine de
		/// l'avatar sur le bassin tracké (XZ + cap) en remplacement de la locomotion « tête » de VRIK, et
		/// pousse le bone pelvis vers sa cible avant chaque solve (position <c>pelvisPositionWeight</c>,
		/// rotation <c>pelvisRotationWeight</c>) — c'est ce qui donne le suivi strict du bassin.
		/// </summary>
		private VRIKRootController EnableRootController() {
			var anchor = Descriptor?.Anchor;
			if (!anchor)
				return null;

			var controller = anchor.GetOrAddComponent<VRIKRootController>();
			controller.enabled = _hipsTracked && !_externalRootControl;

			// Re-fixe l'axe « droite » du bassin (le yaw de la racine en découle) sur la pose courante, qui
			// est celle juste après la calibration : le tracker et l'avatar sont alors cohérents.
			controller.Calibrate();

			return controller;
		}
	}
}
#endif
