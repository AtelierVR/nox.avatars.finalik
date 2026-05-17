#if HAS_FINALIK
using Nox.CCK.Avatars.Rigging;
using Nox.CCK.Utils;
using RootMotion.FinalIK;
using UnityEngine;
using static Nox.Avatars.FinalIK.VRIKWeightParameter;

namespace Nox.Avatars.FinalIK {
	public class FinalIKAvatarModule : BaseRiggingModule {
		private VRIK _rig;

		public VRIK GetRig()
			=> _rig ??= Descriptor.Anchor?.GetOrAddComponent<VRIK>();

		public override bool SetupParameters(BaseRiggingModule m) {
			if (m is not FinalIKAvatarModule module)
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

		public override bool IsActive(HumanBodyBones bone) {
			var rig = GetRig();
			if (!rig) return false;
			return bone switch {
				HumanBodyBones.Head         => rig.solver.spine.positionWeight    > 0f,
				HumanBodyBones.LeftHand     => rig.solver.leftArm.positionWeight  > 0f,
				HumanBodyBones.RightHand    => rig.solver.rightArm.positionWeight > 0f,
				HumanBodyBones.LeftFoot     => rig.solver.leftLeg.positionWeight  > 0f,
				HumanBodyBones.RightFoot    => rig.solver.rightLeg.positionWeight > 0f,
				_                           => false
			};
		}

		public override void SetActive(HumanBodyBones bone, bool active) {
			var rig = GetRig();
			if (!rig) return;
			var w = active ? 1f : 0f;
			switch (bone) {
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
					rig.solver.leftLeg.positionWeight  = w;
					rig.solver.leftLeg.rotationWeight  = w;
					break;
				case HumanBodyBones.RightFoot:
					rig.solver.rightLeg.positionWeight = w;
					rig.solver.rightLeg.rotationWeight = w;
					break;
			}
		}
	}
}
#endif