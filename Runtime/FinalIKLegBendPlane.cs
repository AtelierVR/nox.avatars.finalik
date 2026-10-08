#if HAS_FINALIK
using RootMotion.FinalIK;
using UnityEngine;

namespace Nox.Avatars.FinalIK {
	/// <summary>
	/// Locks the VRIK leg bend plane to the pelvis, every frame.
	/// <para>
	/// Why it can't be left to the generator or to the solver: <c>IKSolverVR.Leg.OnRead</c> <b>overwrites</b>
	/// <c>bendNormalRelToTarget</c>/<c>bendNormalRelToPelvis</c> the first time the solver initiates
	/// (<c>if (!initiated)</c>) with <c>Vector3.Cross(calf - thigh, foot - calf)</c> read from the pose of that
	/// moment. The owner's avatar is posed when that happens, a viewer's replayed avatar is not - the two
	/// clients therefore end up with different axes, and the knee bends towards a fixed, meaningless direction.
	/// A constant in the <i>target</i>'s frame doesn't work either: bone rolls are mirrored, so the same value
	/// inverts one of the two knees (180° knee, twisted tibia, ankle/toe joints torn apart).
	/// </para>
	/// <para>
	/// Measured on a working rig, the axis both legs actually bend around is the pelvis' lateral axis: the world
	/// normal each leg resolves with dots 0.94 (left) and 0.97 (right) with <c>hips.rotation * Vector3.right</c>.
	/// With <c>bendToTargetWeight = 0</c> the solver uses <c>rootRotation * bendNormalRelToPelvis</c>, i.e. that
	/// same axis, taken from the pelvis - a bone driven by the same synced data on every client, and not from
	/// the solved leg (unlike a live cross product, which merely reproduces a broken solve).
	/// </para>
	/// </summary>
	[DefaultExecutionOrder(10000)]
	public class FinalIKLegBendPlane : MonoBehaviour {
		private VRIK _rig;

		public void Initialize(VRIK rig)
			=> _rig = rig;

		private void LateUpdate() {
			if (!_rig) {
				_rig = GetComponent<VRIK>() ?? GetComponentInParent<VRIK>();
				if (!_rig)
					return;
			}

			Pin(_rig);
		}

		/// <summary>Locks both knees to the pelvis' lateral axis (see the class summary).</summary>
		public static void Pin(VRIK rig) {
			if (!rig || rig.solver == null)
				return;

			var solver = rig.solver;

			// Same axis for both legs: the knees bend forward around one shared lateral axis, and taking it from
			// the pelvis makes the mirroring of the bone rolls irrelevant.
			solver.leftLeg.bendNormalRelToPelvis  = Vector3.right;
			solver.rightLeg.bendNormalRelToPelvis = Vector3.right;

			// 0 = the plane follows the pelvis only (the target's rotation no longer twists the knee).
			solver.leftLeg.bendToTargetWeight  = 0f;
			solver.rightLeg.bendToTargetWeight = 0f;
		}
	}
}
#endif
