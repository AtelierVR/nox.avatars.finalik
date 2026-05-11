#if HAS_FINALIK
using Nox.CCK.Avatars.Rigging;
using Nox.CCK.Players;
using Nox.CCK.Utils;
using UnityEngine;
using RootMotion.FinalIK;
using Transform = UnityEngine.Transform;

namespace Nox.Avatars.FinalIK {
	/// <summary>
	/// Générateur pour les systèmes IK utilisant FinalIK VR (préféré quand disponible)
	/// </summary>
	public static class FinalIKRigGenerator {
		public static VRIK Create(FinalIKAvatarModule module) {
			var rig      = module.GetRig();
			var animator = module.Descriptor.Animator;
			var anchor   = module.Descriptor.Anchor.transform;

			if (!animator) {
				Debug.LogError("Animator not found on avatar root!");
				return null;
			}

			// References
			rig.references.root = anchor;
			// Spine
			rig.references.pelvis = module.GetBone(HumanBodyBones.Hips);
			rig.references.spine  = module.GetBone(HumanBodyBones.Spine);
			rig.references.head   = module.GetBone(HumanBodyBones.Head);
			// Left Arm
			rig.references.leftShoulder = module.GetBone(HumanBodyBones.LeftShoulder);
			rig.references.leftUpperArm = module.GetBone(HumanBodyBones.LeftUpperArm);
			rig.references.leftForearm  = module.GetBone(HumanBodyBones.LeftLowerArm);
			rig.references.leftHand     = module.GetBone(HumanBodyBones.LeftHand);
			// Right Arm
			rig.references.rightShoulder = module.GetBone(HumanBodyBones.RightShoulder);
			rig.references.rightUpperArm = module.GetBone(HumanBodyBones.RightUpperArm);
			rig.references.rightForearm  = module.GetBone(HumanBodyBones.RightLowerArm);
			rig.references.rightHand     = module.GetBone(HumanBodyBones.RightHand);
			// Left Leg
			rig.references.leftThigh = module.GetBone(HumanBodyBones.LeftUpperLeg);
			rig.references.leftCalf  = module.GetBone(HumanBodyBones.LeftLowerLeg);
			rig.references.leftFoot  = module.GetBone(HumanBodyBones.LeftFoot);
			rig.references.leftToes  = module.GetBone(HumanBodyBones.LeftToes);
			// Right Leg
			rig.references.rightThigh = module.GetBone(HumanBodyBones.RightUpperLeg);
			rig.references.rightCalf  = module.GetBone(HumanBodyBones.RightLowerLeg);
			rig.references.rightFoot  = module.GetBone(HumanBodyBones.RightFoot);
			rig.references.rightToes  = module.GetBone(HumanBodyBones.RightToes);

			// Solver
			// Spine
			rig.solver.spine.headTarget   = CreateTarget(module, HumanBodyBones.Head);
			rig.solver.spine.pelvisTarget = CreateTarget(module, HumanBodyBones.Hips);
			rig.solver.spine.chestGoal    = CreateTarget(module, HumanBodyBones.Chest);
			// Left Arm
			rig.solver.leftArm.target   = CreateTarget(module, HumanBodyBones.LeftHand);
			rig.solver.leftArm.bendGoal = CreateTarget(module, HumanBodyBones.LeftUpperArm);
			// Right Arm
			rig.solver.rightArm.target   = CreateTarget(module, HumanBodyBones.RightHand);
			rig.solver.rightArm.bendGoal = CreateTarget(module, HumanBodyBones.RightUpperArm);
			// Left Leg
			rig.solver.leftLeg.target   = CreateTarget(module, HumanBodyBones.LeftFoot);
			rig.solver.leftLeg.bendGoal = CreateTarget(module, HumanBodyBones.LeftLowerArm);
			// Right Leg
			rig.solver.rightLeg.target   = CreateTarget(module, HumanBodyBones.RightHand);
			rig.solver.rightLeg.bendGoal = CreateTarget(module, HumanBodyBones.RightLowerArm);
			// Locomotion
			rig.solver.locomotion.mode = IKSolverVR.Locomotion.Mode.Animated;
			
			return rig;
		}

		private static Transform CreateTarget(FinalIKAvatarModule module, HumanBodyBones bone) {
			var transform = new GameObject($"VRIK_{bone.ToString()}").transform;
			transform.parent        = module.transform;
			transform.localPosition = Vector3.zero;
			transform.localRotation = Quaternion.identity;
			transform.localScale    = Vector3.one;
			module.Parts.Add(new RiggingPart(bone.ToPlayerRig().ToIndex(), transform));
			return transform;
		}
	}
}
#endif