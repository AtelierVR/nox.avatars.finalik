#if HAS_FINALIK
using Nox.Avatars.Parameters;
using Nox.CCK;
using Nox.CCK.Network;
using Nox.CCK.Utils;
using RootMotion.FinalIK;
using UnityEngine;

namespace Nox.Avatars.FinalIK {
	/// <summary>
	/// Exposes a VRIK solver arm/leg positional or rotational weight as an avatar parameter,
	/// mirroring the IKWeightParameter used by the RigBuilder backend.
	/// </summary>
	public class VRIKWeightParameter : IParameter {
		public enum WeightType { Position, Rotation }

		private readonly string     _name;
		private readonly VRIK       _rig;
		private readonly HumanBodyBonesGroup _group;
		private readonly WeightType _type;

		public enum HumanBodyBonesGroup { Head, LeftArm, RightArm, LeftLeg, RightLeg }

		public VRIKWeightParameter(string name, VRIK rig, HumanBodyBonesGroup group, WeightType type) {
			_name  = name;
			_rig   = rig;
			_group = group;
			_type  = type;
		}

		public string GetName() => _name;
		public bool   IsValid() => _rig;
		public int    GetKey()  => _name.GetHashCode();

		public ParameterType  GetValueType() => ParameterType.Float;
		public ParameterFlags GetFlags()     => ParameterFlags.OwnerEditable | ParameterFlags.OwnerSyncsToViewers;

		public object Get() {
			if (!_rig) return 0f;
			return (_group, _type) switch {
				(HumanBodyBonesGroup.Head,     WeightType.Position) => _rig.solver.spine.positionWeight,
				(HumanBodyBonesGroup.Head,     WeightType.Rotation) => _rig.solver.spine.rotationWeight,
				(HumanBodyBonesGroup.LeftArm,  WeightType.Position) => _rig.solver.leftArm.positionWeight,
				(HumanBodyBonesGroup.LeftArm,  WeightType.Rotation) => _rig.solver.leftArm.rotationWeight,
				(HumanBodyBonesGroup.RightArm, WeightType.Position) => _rig.solver.rightArm.positionWeight,
				(HumanBodyBonesGroup.RightArm, WeightType.Rotation) => _rig.solver.rightArm.rotationWeight,
				(HumanBodyBonesGroup.LeftLeg,  WeightType.Position) => _rig.solver.leftLeg.positionWeight,
				(HumanBodyBonesGroup.LeftLeg,  WeightType.Rotation) => _rig.solver.leftLeg.rotationWeight,
				(HumanBodyBonesGroup.RightLeg, WeightType.Position) => _rig.solver.rightLeg.positionWeight,
				(HumanBodyBonesGroup.RightLeg, WeightType.Rotation) => _rig.solver.rightLeg.rotationWeight,
				_                                                    => 0f
			};
		}

		public void Set(object value) {
			if (!_rig) return;
			var w = Mathf.Clamp01(value.ToFloat());
			switch (_group) {
				case HumanBodyBonesGroup.Head:
					if (_type == WeightType.Position) _rig.solver.spine.positionWeight = w;
					else                              _rig.solver.spine.rotationWeight = w;
					break;
				case HumanBodyBonesGroup.LeftArm:
					if (_type == WeightType.Position) _rig.solver.leftArm.positionWeight = w;
					else                              _rig.solver.leftArm.rotationWeight = w;
					break;
				case HumanBodyBonesGroup.RightArm:
					if (_type == WeightType.Position) _rig.solver.rightArm.positionWeight = w;
					else                              _rig.solver.rightArm.rotationWeight = w;
					break;
				case HumanBodyBonesGroup.LeftLeg:
					if (_type == WeightType.Position) _rig.solver.leftLeg.positionWeight = w;
					else                              _rig.solver.leftLeg.rotationWeight = w;
					break;
				case HumanBodyBonesGroup.RightLeg:
					if (_type == WeightType.Position) _rig.solver.rightLeg.positionWeight = w;
					else                              _rig.solver.rightLeg.rotationWeight = w;
					break;
			}
		}
	}
}
#endif
