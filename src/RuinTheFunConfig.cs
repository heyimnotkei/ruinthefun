using System;
using System.Collections.Generic;
using Vintagestory.API.Client;

namespace RuinTheFun;

internal class HomeEntry
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public HomeEntry() { }
    public HomeEntry(double x, double y, double z) { X = x; Y = y; Z = z; }
}

internal class RuinTheFunConfig
{
    public string Language { get; set; } = "en";

    public Dictionary<string, string> Hotkeys { get; set; } = new Dictionary<string, string>
    {
        ["ruinthefunsettings"]      = GlKeys.R.ToString(),
        ["ruinthefunhotkeyoverlay"] = GlKeys.O.ToString(),
        ["ruinthefunonehitblock"]   = GlKeys.K.ToString(),
        ["ruinthefunhomeport"]      = GlKeys.L.ToString(),
        ["ruinthefunflight"]        = GlKeys.F.ToString(),
        ["ruinthefunfastplace"]     = GlKeys.G.ToString()
    };

    public bool ExampleGreetingEnabled { get; set; } = true;
    public string ExampleGreetingText  { get; set; } = "Ruin the Fun ready!";

    public float MovementSpeedMultiplier { get; set; } = 1f;

    public bool OneHitBlockEnabled { get; set; } = false;
    public bool NoFallEnabled      { get; set; } = false;
    public bool FlightEnabled      { get; set; } = false;
    public bool FastPlaceEnabled   { get; set; } = false;

    public Dictionary<string, HomeEntry> HomePoses { get; set; } = new Dictionary<string, HomeEntry>();
}
