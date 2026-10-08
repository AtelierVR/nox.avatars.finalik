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

		/// <summary>The hips are driven by a tracker.</summary>
		private bool _hipsTracked;

		/// <summary>The game drives the root (full-body calibration), see <see cref="SetExternalRootControl"/>.</summary>
		private bool _externalRootControl;

		/// <summary>Locomotion and root angle set aside while the rig must not move the root.</summary>
		private float? _savedLocomotionWeight;
		private float? _savedMaxRootAngle;

		/// <summary>Root (hips) controller installed once a hips tracker takes over.</summary>
		private VRIKRootController _rootController;

		public VRIK GetRig() {
			_rig ??= Descriptor.Anchor?.GetOrAddComponent<VRIK>();
			FinalIKLegBendPlane.Pin(_rig);
			return _rig;
		}

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
					// The leg solver applies the yaw of the target, so the rotation stays active.
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
		/// The (XR) controller takes over the avatar root: see <see cref="IRigging.SetExternalRootControl"/>.
		/// </summary>
		public override void SetExternalRootControl(bool external) {
			if (_externalRootControl == external)
				return;

			_externalRootControl = external;
			RefreshRootControl();
		}

		/// <summary>
		/// Recomputes who drives the root. While the rig must not move it (tracked hips or a game-driven root),
		/// the VRIK locomotion is cut and <c>spine.maxRootAngle</c> is forced to 180 so the spine does not move
		/// the root itself. The <see cref="VRIKRootController"/> is only active for tracked hips outside a
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
		/// Installs (or enables) the anchor's <see cref="VRIKRootController"/>: it moves the avatar root onto the
		/// tracked hips (XZ + yaw) and pushes the pelvis bone towards its target before each solve.
		/// </summary>
		private VRIKRootController EnableRootController() {
			var anchor = Descriptor?.Anchor;
			if (!anchor)
				return null;

			var controller = anchor.GetOrAddComponent<VRIKRootController>();
			controller.enabled = _hipsTracked && !_externalRootControl;

			// Re-pins the pelvis "right" axis (the root yaw follows from it) on the current pose.
			controller.Calibrate();

			return controller;
		}
	}
}
#endif
