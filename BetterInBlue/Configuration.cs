using Dalamud.Configuration;
using System;
using System.Collections.Generic;

namespace BetterInBlue;

[Serializable]
public class Configuration : IPluginConfiguration {
    public int Version { get; set; } = 2;
    public List<Loadout> Loadouts { get; set; } = new();

    public bool RestoreHotbars { get; set; } = true;
    public bool RestoreCrossHotbars { get; set; } = true;
    
    public List<int> Hotbars { get; set; } = new();
    public List<int> CrossHotbars { get; set; } = new();

    public void Save() {
        Services.PluginInterface.SavePluginConfig(this);
    }
}
