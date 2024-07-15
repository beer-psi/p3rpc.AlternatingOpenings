using System.Drawing;
using System.Runtime.CompilerServices;
using Reloaded.Mod.Interfaces;

namespace p3rpc.AlternatingOpenings;

public enum ModLoggerLevel
{
    Debug,
    Info,
    Warning,
    Error,
}

internal class ModLogger
{
    private readonly ILogger _logger;
    private readonly string _tag;
    internal ModLoggerLevel Level { get; set; }
    
    public ModLogger(ILogger logger, string tag, ModLoggerLevel level = ModLoggerLevel.Info)
    {
        _logger = logger;
        _tag = $"[{tag}]";
        Level = level;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string LogLevelToString(ModLoggerLevel level)
    {
        return level switch
        {
            ModLoggerLevel.Debug => "DBG",
            ModLoggerLevel.Info => "INF",
            ModLoggerLevel.Warning => "WRN",
            ModLoggerLevel.Error => "ERR",
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Log(ModLoggerLevel level, Color color, string format, params object?[] args)
    {
        if (Level <= level)
        {
            _logger.WriteLine(_tag + " [" + LogLevelToString(level) + "] " + string.Format(format, args), color);
        }
    }
    
    public void Debug(string format, params object?[] args)
    {
        Log(ModLoggerLevel.Debug, Color.Gray, format, args);
    }
    
    public void Info(string format, params object?[] args)
    {
        Log(ModLoggerLevel.Info, Color.White, format, args);
    }
    
    public void Warning(string format, params object?[] args)
    {
        Log(ModLoggerLevel.Warning, Color.Yellow, format, args);
    }
    
    public void Error(string format, params object?[] args)
    {
        Log(ModLoggerLevel.Error, Color.Red, format, args);
    }
}