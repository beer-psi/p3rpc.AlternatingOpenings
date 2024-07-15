using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Reloaded.Mod.Interfaces;
using p3rpc.AlternatingOpenings.Template;
using p3rpc.AlternatingOpenings.Configuration;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using SharedScans.Interfaces;
using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace p3rpc.AlternatingOpenings;

/// <summary>
/// Your mod logic goes here.
/// </summary>
public class Mod : ModBase // <= Do not Remove.
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
    private readonly ILogger _rLogger;

    /// <summary>
    /// Entry point into the mod instance that created this class.
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

    [Function(CallingConventions.Microsoft)]
    private delegate void criManaPlayer_SetFile(nint player, nint binder, nint path);

    private readonly ModLogger _logger;
    private HookContainer<criManaPlayer_SetFile>? _setFileContainer;
    private IHook<criManaPlayer_SetFile>? _setFileHook;
    private List<(OpeningMovies, nint)> _movies = new(4);
    private int _movieIndex;

    public Mod(ModContext context)
    {
        _modLoader = context.ModLoader;
        _hooks = context.Hooks;
        _rLogger = context.Logger;
        _owner = context.Owner;
        _configuration = context.Configuration;
        _modConfig = context.ModConfig;
        _logger = new ModLogger(_rLogger, _modConfig.ModName, _configuration.LogLevel);
        
        LoadConfiguredMovies();
        
        var activeMods = _modLoader.GetActiveMods()
            .Select(m => m.Generic.ModId)
            .ToImmutableList();

        if (_configuration.ShuffleOrder)
        {
            _movieIndex = Random.Shared.Next(0, _movies.Count);
            _logger.Debug("Spinning the wheeeeeeel: Landed on movieIndex {0}", _movieIndex);
        }

        if (activeMods.Contains("Ryo.Reloaded") && activeMods.Contains("SharedScans.Reloaded"))
        {
            _logger.Debug("Shared Scans + Ryo Framework detected, using shared scan for criManaPlayer_SetFile.");
            HookWithSharedScans();
            return;
        }
        
        HookWithReloadedScanner();
    }

    private void LoadConfiguredMovies()
    {
        _movies.Clear();
        
        var modDirectory = _modLoader.GetDirectoryForModId(_modConfig.ModId)!;
        var moviesPath = Path.Join(modDirectory, "Movies");
        var configuredMovies = new[]
        {
            _configuration.OpeningMovie1,
            _configuration.OpeningMovie2,
            _configuration.OpeningMovie3,
            _configuration.OpeningMovie4,
        };
        
        foreach (var movie in configuredMovies)
        {
            switch (movie)
            {
                case OpeningMovies.None:
                    continue;
                case OpeningMovies.FullMoonFullLife:
                    _movies.Add((movie, 0));
                    break;
                default:
                    var moviePath = Path.Join(moviesPath, movie + ".usm");

                    if (!Path.Exists(moviePath))
                    {
                        _logger.Warning("{0} not found. {1} will not be played.", moviePath, movie);
                        continue;
                    }

                    _movies.Add((movie, Marshal.StringToHGlobalAnsi(moviePath)));
                    break;
            }
        }

        if (_movies.Count == 0)
        {
            _logger.Warning("No valid movies set, falling back to Full Moon Full Life.");
            _movies.Add((OpeningMovies.FullMoonFullLife, 0));
        }

        if (_configuration.ShuffleOrder && _movies.Count > 1)
        {
            ShuffleMovies();
        }
    }

    private void ShuffleMovies()
    {
        _logger.Debug("Shuffling movies.");

        var n = _movies.Count;

        while (n > 1)
        {
            var k = Random.Shared.Next(n--);
            
            (_movies[n], _movies[k]) = (_movies[k], _movies[n]);
        }
    }

    private void HookWithSharedScans()
    {
        var sharedScansController = _modLoader.GetController<ISharedScans>();

        if (sharedScansController == null || !sharedScansController.TryGetTarget(out var scanner))
        {
            _logger.Warning("Unable to get controller for Shared Scans. Falling back to Reloaded's signature scanner.");
            HookWithReloadedScanner();
            return;
        }

        _setFileContainer =
            scanner.CreateHook<criManaPlayer_SetFile>(criManaPlayer_SetFile_Hook, _modConfig.ModName);
    }

    private void HookWithReloadedScanner()
    {
        var startupScannerController = _modLoader.GetController<IStartupScanner>();

        if (startupScannerController == null || !startupScannerController.TryGetTarget(out var scanner))
        {
            _logger.Error("Unable to get controller for signature scanner. This mod won't work.");
            return;
        }

        if (_hooks == null)
        {
            _logger.Error("Unable to access Reloaded Hooks API. Does this mod depend on Reloaded.SharedLib.Hooks?");
            return;
        }
        
        scanner.AddMainModuleScan(
            "4C 89 44 24 ?? 48 89 54 24 ?? 48 89 4C 24 ?? 48 83 EC 38 48 83 7C 24 ?? 00 75 ?? 41 B8 FE FF FF FF 48 8D 15 ?? ?? ?? ?? 31 C9",
            result =>
            {
                if (!result.Found)
                {
                    _logger.Error("Could not find criManaPlayer_SetFile.");
                    return;
                }
                
                var address = result.Offset + Process.GetCurrentProcess().MainModule!.BaseAddress;
                
                _logger.Debug("Found criManaPlayer_SetFile at 0x{0:X}", address);
                
                _setFileHook = _hooks.CreateHook<criManaPlayer_SetFile>(criManaPlayer_SetFile_Hook, address);
                _setFileHook.Activate();
            });
    }

    private void criManaPlayer_SetFile_Hook(nint player, nint binder, nint path)
    {
        var file = Marshal.PtrToStringAnsi(path) ?? string.Empty;
        var hook = _setFileContainer?.Hook ?? _setFileHook;

        if (hook == null)
        {
            _logger.Error("Hooked function called but hook is null?!");
            return;
        }

        if (!file.EndsWith("Anim/MS_Event_Main_100_010_M_Movi_VP9.usm"))
        {
            _logger.Debug("Calling original function (not the opening movie).");
            hook.OriginalFunction(player, binder, path);
            return;
        }
        
        _logger.Debug("Starting opening movie replacement.");

        var (movie, moviePath) = _movies[_movieIndex];
        var targetPath = path;
        
        _logger.Debug("Picked opening movie {0}.", movie);

        if (movie == OpeningMovies.FullMoonFullLife)
        {
            _logger.Debug("Keeping original opening movie.");
        }
        else if (moviePath == 0)
        {
            _logger.Error("Attempted to play {0}, but pointer to its path was null?!", movie);
        }
        else
        {
            targetPath = moviePath;
        }

        hook.OriginalFunction(player, binder, targetPath);
        _movieIndex = (_movieIndex + 1) % _movies.Count;

        if (_movieIndex == 0)
        {
            ShuffleMovies();
        }
    }

    #region Standard Overrides

    public override bool CanSuspend() => true;
    public override bool CanUnload() => true;

    public override void Suspend()
    {
        (_setFileContainer?.Hook ?? _setFileHook)?.Disable();
    }

    public override void Unload()
    {
        Suspend();

        foreach (var (_, moviePath) in _movies)
        {
            if (moviePath != 0)
            {
                Marshal.FreeHGlobal(moviePath);
            }
        }

        _movies = null!;
    }

    public override void Resume()
    {
        (_setFileContainer?.Hook ?? _setFileHook)?.Enable();
    }

    public override void ConfigurationUpdated(Config configuration)
    {
        // Apply settings from configuration.
        // ... your code here.
        _configuration = configuration;
        _logger.Level = _configuration.LogLevel;
        
        _logger.Debug("Config Updated: Applying");
        
        LoadConfiguredMovies();
    }

    #endregion

    #region For Exports, Serialization etc.

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod()
    {
    }
#pragma warning restore CS8618

    #endregion
}