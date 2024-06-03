using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace BetterInBlue.Windows;

public class ConfigWindow : Window, IDisposable {
    public ConfigWindow() : base("Better in Blue Config") {
        this.Size = new Vector2(450, 400);
        this.SizeCondition = ImGuiCond.FirstUseEver;
    }

    public void Dispose() { }

    public override void Draw() {
        var applyToHotbars = Plugin.Configuration.RestoreHotbars;
        if (ImGui.Checkbox("Apply to hotbars", ref applyToHotbars)) {
            Plugin.Configuration.RestoreHotbars = applyToHotbars;
            Plugin.Configuration.Save();
        }

        if (ImGui.IsItemHovered()) {
            ImGui.SetTooltip(
                "If checked, applying a loadout will write each action to your hotbars.\n"
                + "Hotbar contents are not saved to your character config until an action is moved."
            );
        }

        if (!applyToHotbars) ImGui.BeginDisabled();
        this.HotbarsSelector("Hotbar", Plugin.Configuration.Hotbars);
        if (!applyToHotbars) ImGui.EndDisabled();

        ImGui.Spacing();
        ImGui.Spacing();
        var applyToCrossHotbars = Plugin.Configuration.RestoreCrossHotbars;
        if (ImGui.Checkbox("Apply to cross hotbars", ref applyToCrossHotbars)) {
            Plugin.Configuration.RestoreCrossHotbars = applyToCrossHotbars;
            Plugin.Configuration.Save();
        }

        if (ImGui.IsItemHovered()) {
            ImGui.SetTooltip(
                "If checked, applying a loadout will write each action to your hotbars.\n"
                + "Hotbar contents are not saved to your character config until an action is moved."
            );
        }

        if (!applyToCrossHotbars) ImGui.BeginDisabled();
        this.HotbarsSelector("Cross Hotbar", Plugin.Configuration.CrossHotbars);
        if (!applyToCrossHotbars) ImGui.EndDisabled();
    }

    private void HotbarsSelector(string name, List<int> hotbars) {
        for (int i = 0; i < hotbars.Count; i++) {
            ImGui.PushID($"selector-{name}-{i}");

            var value = hotbars[i];
            if (ImGui.InputInt("##hotbar", ref value)) {
                if (value > 10) value = 10;
                if (value < 1) value = 1;

                hotbars[i] = value;
                Plugin.Configuration.Save();
            }
            ImGui.SameLine();

            var canDelete = ImGui.GetIO().KeyCtrl;
            if (Plugin.DisabledButtonWithTooltip(
                    FontAwesomeIcon.Trash,
                    !canDelete,
                    "",
                    "Delete this hotbar. Hold Ctrl to enable the delete button."
                )) {
                hotbars.RemoveAt(i);
                i--;
                Plugin.Configuration.Save();
            }
            ImGui.SameLine();
            ImGui.Text($"{name} {i + 1}");

            ImGui.PopID();
        }

        if (ImGui.Button($"Add {name}") && hotbars.Count < 10) {
            hotbars.Add(1);
            Plugin.Configuration.Save();
        }
    }
}
