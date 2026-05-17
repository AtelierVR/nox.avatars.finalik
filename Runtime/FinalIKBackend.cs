#if HAS_FINALIK
using Nox.Avatars;
using Nox.Avatars.Rigging;
using Nox.CCK.Avatars.Rigging;
using Nox.CCK.Utils;

namespace Nox.Avatars.FinalIK {
	/// <summary>
	/// Rigging backend that uses FinalIK's VRIK solver.
	/// Only compiled when the <c>HAS_FINALIK</c> scripting define is active.
	/// Register this backend from the <c>nox.avatars.finalik</c> mod entry point.
	/// </summary>
	public class FinalIKBackend : IRiggingBackend {
		public const string BACKEND_ID = "finalik";
		
		public string Id
			=> BACKEND_ID;

		/// <inheritdoc/>
		/// Returns 10 for XR only. Desktop uses RigBuilder (score 0) as fallback.
		public int CanHandle(IRuntimeAvatar runtime) {
			var args = runtime.Arguments;
			if (args.TryGetValue(RiggingControllerType.XR, out var xr) && xr is true) return 10;
			return -1;
		}

		/// <inheritdoc/>
		public IRiggingModule Instantiate(IRuntimeAvatar runtime)
			=> runtime.Descriptor.Anchor.GetOrAddComponent<FinalIKAvatarModule>();

		/// <inheritdoc/>
		public void SetupRig(IRiggingModule module) {
			if (module is FinalIKAvatarModule fik)
				FinalIKRigGenerator.Create(fik);
		}

		/// <inheritdoc/>
		public void Dispose() { }
	}
}
#endif
