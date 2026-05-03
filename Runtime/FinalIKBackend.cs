#if HAS_FINALIK
using System.Collections.Generic;
using UnityEngine;
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
		/// Returns 10 for XR, 5 for Desktop, -1 otherwise.
		public int CanHandle(Dictionary<string, object> arguments) {
			var type = RiggingBackendRegistry.GetControllerType(arguments);
			if (type == RiggingControllerType.XR)      return 10;
			if (type == RiggingControllerType.Desktop) return 5;
			return -1;
		}

		/// <inheritdoc/>
		public BaseRiggingModule CreateModule(GameObject anchor)
			=> anchor.GetOrAddComponent<FinalIKAvatarModule>();

		/// <inheritdoc/>
		public void SetupRig(BaseRiggingModule module) {
			if (module is FinalIKAvatarModule fik)
				FinalIKRigGenerator.Create(fik);
		}

		/// <inheritdoc/>
		public void Dispose() { }
	}
}
#endif
