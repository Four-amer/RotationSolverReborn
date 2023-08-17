﻿using ECommons.ImGuiMethods;
using RotationSolver.ActionSequencer;
using RotationSolver.Basic.Configuration;
using RotationSolver.UI;
using System.Diagnostics;

namespace RotationSolver.Updaters;

internal class ActionSequencerUpdater
{
    static string _actionSequencerFolder;

    static IEnumerable<MajorConditionSet> _conditionSet;
    public static MajorConditionSet RightSet => _conditionSet?
        .ElementAtOrDefault(Service.Config.GetValue(PluginConfigInt.ActionSequencerIndex));

    public static string[] ConditionSetsName => _conditionSet?.Select(s => s.Name).ToArray() ?? Array.Empty<string>();

    public static void UpdateActionSequencerAction()
    {
        if (_conditionSet == null) return;
        var customRotation = RotationUpdater.RightNowRotation;
        if (customRotation == null) return;

        var allActions = RotationUpdater.RightRotationActions;

        var set = RightSet;
        if (set == null) return;

        DataCenter.DisabledActionSequencer = new HashSet<uint>(set.DiabledConditions.Where(pair => pair.Value.IsTrue(customRotation))
             .Select(pair => pair.Key));

        bool find = false;
        foreach (var conditionPair in set.Conditions)
        {
            var nextAct = allActions.FirstOrDefault(a => a.ID == conditionPair.Key);
            if (nextAct == null) continue;

            if (!conditionPair.Value.IsTrue(customRotation)) continue;

            DataCenter.ActionSequencerAction = nextAct;
            find = true;
            break;
        }
        if (!find)
        {
            DataCenter.ActionSequencerAction = null;
        }
    }

    public static void Enable(string folder)
    {
        _actionSequencerFolder = folder;
        if (!Directory.Exists(_actionSequencerFolder)) Directory.CreateDirectory(_actionSequencerFolder);

        _conditionSet = MajorConditionSet.Read(_actionSequencerFolder);
    }

    public static void SaveFiles()
    {
        try
        {
            Directory.Delete(_actionSequencerFolder);
            Directory.CreateDirectory(_actionSequencerFolder);
        }
        catch
        {

        }
        foreach (var set in _conditionSet)
        {
            set.Save(_actionSequencerFolder);
        }
    }

    public static void LoadFiles()
    {
        _conditionSet = MajorConditionSet.Read(_actionSequencerFolder);
    }

    private static void AddNew()
    {
        const string conditionName = "Unnamed";
        if (!_conditionSet.Any(c => c.Name == conditionName))
        {
            _conditionSet = _conditionSet.Union(new[] { new MajorConditionSet(conditionName) });
        }
    }

    private static void Delete(string name)
    {
        _conditionSet = _conditionSet.Where(c => c.Name != name);
    }

    public static void DrawHeader(float width)
    {
        var set = RightSet;
        bool hasSet = set != null;

        if (hasSet)
        {
            ImGuiHelper.SetNextWidthWithName(set.Name);
            ImGui.InputText("##MajorConditionSet", ref set.Name, 100);

            ImGui.SameLine();
        }

        var index = Service.Config.GetValue(PluginConfigInt.ActionSequencerIndex);

        var combos = ConditionSetsName;
        if (combos != null && combos.Length > index)
        {
            ImGui.SetNextItemWidth(ImGui.CalcTextSize(combos[index]).X + width);
        }
        else
        {
            ImGui.SetNextItemWidth(width);
        }

        if(ImGui.Combo("##MajorConditionCombo", ref index, combos, combos.Length))
        {
            Service.Config.SetValue(PluginConfigInt.ActionSequencerIndex, index);
        }

        if (hasSet)
        {
            ImGui.SameLine();
            if (ImGuiEx.IconButton(FontAwesomeIcon.Ban, "##DeleteTimelineConditionSet"))
            {
                Delete(set.Name);
            }

            ImGui.SameLine();
        }

        ImGui.SameLine();

        if (ImGuiEx.IconButton(FontAwesomeIcon.Plus, "##AddNewTimelineConditionSet"))
        {
            AddNew();
        }

        ImGui.SameLine();
        if (ImGuiEx.IconButton(FontAwesomeIcon.Folder, "##OpenDefinationFolder"))
        {
            Process.Start("explorer.exe", _actionSequencerFolder);
        }

        ImGui.SameLine();
        if (ImGuiEx.IconButton(FontAwesomeIcon.Save, "##SaveTheConditions"))
        {
            SaveFiles();
        }

        ImGui.SameLine();
        if (ImGuiEx.IconButton(FontAwesomeIcon.FileDownload, "##LoadTheConditions"))
        {
            LoadFiles();
        }
    }
}
