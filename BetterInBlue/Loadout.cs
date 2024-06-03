using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace BetterInBlue;

[Serializable]
public class Loadout {
    public string Name { get; set; }
    public uint[] Actions { get; set; } = new uint[24];
    public Dictionary<int, List<(HotbarSlotType Type, uint Id)>> HotbarActions { get; set; }

    public Loadout(string name = "Unnamed Loadout") {
        this.Name = name;
    }

    public static Loadout? FromPreset(string preset) {
        try {
            var bytes = Convert.FromBase64String(preset);
            var str = System.Text.Encoding.UTF8.GetString(bytes);
            return JsonSerializer.Deserialize<Loadout>(str);
        } catch (Exception) {
            return null;
        }
    }

    public string ToPreset() {
        var str = JsonSerializer.Serialize(this);
        var bytes = System.Text.Encoding.UTF8.GetBytes(str);
        return Convert.ToBase64String(bytes);
    }

    public int ActionCount(uint id) {
        return this.Actions.Count(x => x == id);
    }

    public unsafe bool ActionUnlocked(uint id) {
        var normalId = Plugin.AozToNormal(id);
        var link = Plugin.Action.GetRow(normalId)!.UnlockLink;
        return UIState.Instance()->IsUnlockLinkUnlocked(link);
    }

    public bool CanApply() {
        // Must be BLU to apply (id = 36)
        if (Services.ClientState.LocalPlayer?.ClassJob.Id != 36) return false;

        // Can't apply in combat
        if (Services.Condition[ConditionFlag.InCombat]) return false;

        foreach (var action in this.Actions) {
            // No out of bounds indexing
            if (action > Plugin.AozAction.RowCount) return false;

            if (action != 0) {
                // Can't have two actions in the same loadout
                if (this.ActionCount(action) > 1) return false;

                // Can't apply an action you don't have
                if (!this.ActionUnlocked(action)) return false;
            }
        }

        // aight we good
        return true;
    }

    public unsafe bool Apply() {
        var actionManager = ActionManager.Instance();

        var arr = new uint[24];
        for (var i = 0; i < 24; i++) {
            arr[i] = Plugin.AozToNormal(this.Actions[i]);
        }

        fixed (uint* ptr = arr) {
            var ret = actionManager->SetBlueMageActions(ptr);
            if (ret == false) return false;
        }

        if (Plugin.Configuration.RestoreHotbars) {
            ApplyHotbars(12, Plugin.Configuration.Hotbars.Select(x => x - 1));
        }

        if (Plugin.Configuration.RestoreCrossHotbars) {
            ApplyHotbars(16, Plugin.Configuration.CrossHotbars.Select(x => x - 1 + 10));
        }

        return true;
    }

    public unsafe void SaveHotbars() {
        HotbarActions.Clear();

        if (Plugin.Configuration.RestoreHotbars) {
            SaveHotbars(12, Plugin.Configuration.Hotbars.Select(x => x - 1));
        }
        if (Plugin.Configuration.RestoreCrossHotbars) {
            SaveHotbars(16, Plugin.Configuration.CrossHotbars.Select(x => x - 1 + 10));
        }
    }

    private unsafe void SaveHotbars(int maxSlots, IEnumerable<int> hotbars) {
        var hotbarModule = RaptureHotbarModule.Instance();

        foreach (var hotbar in hotbars) {
            if (hotbar < 0 || hotbar >= hotbarModule->HotBarsSpan.Length) {
                Services.Log.Warning("Invalid hotbar number: " + hotbar);
                continue;
            }
            var savedActions = new List<(HotbarSlotType Type, uint Id)>();
            HotbarActions[hotbar] = savedActions;

            for (uint i = 0; i < maxSlots; i++) {
                var slot = hotbarModule->GetSlotById((uint)hotbar, i);
                savedActions.Add((slot->CommandType, slot->CommandId));
            }
        }
    }

    private unsafe void ApplyHotbars(int maxSlots, IEnumerable<int> hotbars) {
        var hotbarModule = RaptureHotbarModule.Instance();

        foreach (var hotbar in hotbars) {
            if (hotbar < 0 || hotbar >= hotbarModule->HotBarsSpan.Length) {
                Services.Log.Warning("Invalid hotbar number: " + hotbar);
                continue;
            }

            if (!HotbarActions.TryGetValue(hotbar, out var savedActions))
                continue;

            for (int i = 0; i < maxSlots; i++) {
                var slot = hotbarModule->GetSlotById((uint)hotbar, (uint)i);
                slot->Set(savedActions[i].Type, savedActions[i].Id);
            }
        }
    }
}
