using p3rpc.classconstructor.Configuration;
using p3rpc.classconstructor.Interfaces;
using p3rpc.classconstructor.Template;
using p3rpc.commonmodutils;
using p3rpc.nativetypes.Interfaces;
using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Memory;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using SharedScans.Interfaces;
using System.Diagnostics;

namespace p3rpc.classconstructor
{
    /// <summary>
    /// Your mod logic goes here.
    /// </summary>
    public class Mod : ModBase, IExports // <= Do not Remove.
    {
        /// <summary>
        /// Provides access to the mod loader API.
        /// </summary>
        private readonly IModLoader _modLoader;

        /// <summary>
        /// Provides access to the Reloaded.Hooks API.
        /// </summary>
        /// <remarks>This is null if you remove dependency on Reloaded.SharedLib.Hooks in your mod.</remarks>
        private readonly IReloadedHooks? _hooks;

        /// <summary>
        /// Provides access to the Reloaded logger.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Entry point into the mod, instance that created this class.
        /// </summary>
        private readonly IMod _owner;

        /// <summary>
        /// Provides access to this mod's configuration.
        /// </summary>
        private Config _configuration;

        /// <summary>
        /// The configuration of the currently executing mod.
        /// </summary>
        private readonly IModConfig _modConfig;

        private ClassConstructorContext _context;
        private ModuleRuntime<ClassConstructorContext> _modRuntime;

        public Mod(ModContext context)
        {
            _modLoader = context.ModLoader;
            _hooks = context.Hooks;
            _logger = context.Logger;
            _owner = context.Owner;
            _configuration = context.Configuration;
            _modConfig = context.ModConfig;

            var mainModule = Process.GetCurrentProcess().MainModule;
            if (mainModule == null) throw new Exception($"[{_modConfig.ModName}] Could not get main module");
            if (_hooks == null) throw new Exception($"[{_modConfig.ModName}] Could not get controller for Reloaded hooks");
            _modLoader.GetController<ISharedScans>().TryGetTarget(out var sharedScans);
            if (sharedScans == null) throw new Exception($"[{_modConfig.ModName}] Could not get controller for Shared Scans");
            _modLoader.GetController<IStartupScanner>().TryGetTarget(out var startupScanner);
            if (startupScanner == null) throw new Exception($"[{_modConfig.ModName}] Could not get controller for Startup Scans");
            _modLoader.GetController<IMemoryMethods>().TryGetTarget(out var memoryMethods);
            if (memoryMethods == null) throw new Exception($"[{_modConfig.ModName}] Could not get controller for Memory Methods");
            Utils utils = new(startupScanner, _logger, _hooks, mainModule.BaseAddress, "Class Constructor", System.Drawing.Color.PeachPuff);
            Memory memory = new Memory();
            _context = new(mainModule.BaseAddress, _configuration, _logger, startupScanner, _hooks, _modLoader.GetDirectoryForModId(_modConfig.ModId), utils, memory, sharedScans, memoryMethods);
            _modRuntime = new(_context);

            _modRuntime.AddModule<ClassMethods>();
            _modRuntime.AddModule<ObjectMethods>();
            _modRuntime.RegisterModules();

            _modLoader.AddOrReplaceController<IClassMethods>(_owner, _modRuntime.GetModule<ClassMethods>());
            _modLoader.AddOrReplaceController<IObjectMethods>(_owner, _modRuntime.GetModule<ObjectMethods>());
        }

        #region Standard Overrides
        public override void ConfigurationUpdated(Config configuration)
        {
            // Apply settings from configuration.
            // ... your code here.
            _configuration = configuration;
            _logger.WriteLine($"[{_modConfig.ModId}] Config Updated: Applying");
        }
        #endregion

        #region For Exports, Serialization etc.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Mod() { }
#pragma warning restore CS8618
        #endregion

        public Type[] GetTypes() => new[] {
            typeof(IClassMethods),
            typeof(IObjectMethods),
        };
    }
}