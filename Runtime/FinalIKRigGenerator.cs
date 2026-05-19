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
		public static VRIK Create(FinalIKAvatarModule module, IRuntimeAvatar runtime) {
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
			rig.solver.leftLeg.bendGoal = CreateTarget(module, HumanBodyBones.LeftLowerLeg);
			// Right Leg
			rig.solver.rightLeg.target   = CreateTarget(module, HumanBodyBones.RightFoot);
			rig.solver.rightLeg.bendGoal = CreateTarget(module, HumanBodyBones.RightLowerLeg);
			// Spine - bodyRotStiffness=0 prevents VRIK from transferring head roll/pitch to the
			// pelvis. In 3-point VR (no pelvis tracker) lateral head tilt should NOT rotate the
			// hips. Horizontal body rotation is handled by locomotion.maxRootAngle instead.
			rig.solver.spine.bodyRotStiffness = 0f;

			// Locomotion - Animated mode; weight=1 lets VRIK reposition the root XZ each frame
			// to follow the head target. Y is corrected manually by the controller's LateUpdate.
			rig.solver.locomotion.mode               = IKSolverVR.Locomotion.Mode.Animated;
			rig.solver.locomotion.weight             = 1f;
			rig.solver.locomotion.maxRootAngleMoving = 10f;
			// limit the rotation of the body by the head
			rig.solver.locomotion.maxRootAngleStanding = 50f;

			// force the pelvis yo not break by the height of the head
			rig.solver.spine.maintainPelvisPosition = 0f;
			rig.solver.spine.minHeadHeight          = 0f;

			// Allow the avatar to follow the head with high fidelity,
			// even if the feet are off the ground (e.g. crouching, sitting).
			// This is important for 3-point VR where the pelvis is not tracked,
			// and the head is the only reference for root motion.
			rig.solver.plantFeet = false;

			// Escape the animation head.
			rig.solver.spine.bodyPosStiffness = 1f;
			rig.solver.spine.bodyRotStiffness = 0f;
			rig.solver.spine.neckStiffness    = 0f;


			return rig;
		}

		private static Transform CreateTarget(FinalIKAvatarModule module, HumanBodyBones bone) {
			var transform = new GameObject($"VRIK_{bone.ToString()}").transform;
			transform.parent     = module.transform;
			transform.localScale = Vector3.one;

			// Initialize at the actual bone world position (same pattern as RigBuilder's GetOrAddPart).
			// This prevents VRIK from starting with targets at (0,0,0) and drifting the avatar root.
			var boneTransform = module.GetBone(bone);
			if (boneTransform != null) {
				transform.position = boneTransform.position;
				transform.rotation = boneTransform.rotation;
			} else {
				transform.localPosition = Vector3.zero;
				transform.localRotation = Quaternion.identity;
			}

			module.Parts.Add(new RiggingPart(bone.ToPlayerRig().ToIndex(), transform));
			return transform;
		}
	}
}
#endif