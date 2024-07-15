using System.ComponentModel;
using p3rpc.AlternatingOpenings.Template.Configuration;
using Reloaded.Mod.Interfaces.Structs;

namespace p3rpc.AlternatingOpenings.Configuration;

public class Config : Configurable<Config>
{
    /*
        User Properties:
            - Please put all of your configurable properties here.

        By default, configuration saves as "Config.json" in mod user config folder.
        Need more config files/classes? See Configuration.cs

        Available Attributes:
        - Category
        - DisplayName
        - Description
        - DefaultValue

        // Technically Supported but not Useful
        - Browsable
        - Localizable

        The `DefaultValue` attribute is used as part of the `Reset` button in Reloaded-Launcher.
    */

    [DisplayName("Log Level")]
    [DefaultValue(ModLoggerLevel.Info)]
    public ModLoggerLevel LogLevel { get; set; } = ModLoggerLevel.Info;

    [Category("Opening Movie Order")]
    [DisplayName("Shuffle Movie Order")]
    [Description("Shuffles the order of the movies below. Done every time all movies have finished playing")]
    [DefaultValue(false)]
    public bool ShuffleOrder { get; set; } = false;

    [Category("Opening Movie Order")]
    [DisplayName("Opening Movie 1")]
    [DefaultValue(OpeningMovies.FullMoonFullLife)]
    public OpeningMovies OpeningMovie1 { get; set; } = OpeningMovies.FullMoonFullLife;

    [Category("Opening Movie Order")]
    [DisplayName("Opening Movie 2")]
    [DefaultValue(OpeningMovies.BurnMyDread)]
    public OpeningMovies OpeningMovie2 { get; set; } = OpeningMovies.BurnMyDread;

    [Category("Opening Movie Order")]
    [DisplayName("Opening Movie 3")]
    [DefaultValue(OpeningMovies.P3Fes)]
    public OpeningMovies OpeningMovie3 { get; set; } = OpeningMovies.P3Fes;

    [Category("Opening Movie Order")]
    [DisplayName("Opening Movie 4")]
    [DefaultValue(OpeningMovies.SoulPhrase)]
    public OpeningMovies OpeningMovie4 { get; set; } = OpeningMovies.SoulPhrase;
}

/// <summary>
/// Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
/// Override elements in <see cref="ConfiguratorMixinBase"/> for finer control.
/// </summary>
public class ConfiguratorMixin : ConfiguratorMixinBase
{
    // 
}