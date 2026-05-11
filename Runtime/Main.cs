#if HAS_FINALIK
using Nox.Avatars.FinalIK;
using Nox.Avatars.Rigging;
using Nox.CCK.Avatars.Rigging;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;

namespace Nox.Avatars.FinalIK.Runtime {
	/// <summary>
	/// Entry point for the <c>nox.avatars.finalik</c> mod.
	/// Registers <see cref="FinalIKBackend"/> with the <see cref="IRiggingBackendRegistry"/>
	/// exposed by <c>nox.avatars.modules</c>, and unregisters it on dispose.
	/// Only active when the <c>HAS_FINALIK</c> scripting define is set.
	/// </summary>
	public class Main : IMainModInitializer {
		private IMainModCoreAPI _api;
		private FinalIKBackend  _backend;

		private IRiggingBackendRegistry Registry
			=> _api.ModAPI
			.GetMod("avatars.modules")
			.GetInstance<IRiggingBackendRegistry>();

		public void OnInitializeMain(IMainModCoreAPI api) {
			_api     = api;
			_backend = new FinalIKBackend();
			Registry.Register(_backend);
			api.LoggerAPI.LogDebug("FinalIK backend registered.");
		}

		public void OnDisposeMain() {
			Registry?.Unregister(_backend);
			_backend.Dispose();
			_backend = null;
			_api     = null;
		}
	}
}
#endif
