using Dalamud.Bindings.ImGui;
using ECommons;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using Lumina.Excel.Sheets;
using Newtonsoft.Json;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using Status = Lumina.Excel.Sheets.Status;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.Dancing_Mad;

public class P4_Debuff_Reminder_JP_Fixed : SplatoonScript
{
    public override Metadata Metadata { get; } = new(6, "NightmareXIV / JP fixed / Japanese hints");
    public override HashSet<uint>? ValidTerritories { get; } = new() { 1363 };

    private readonly List<string> VfxLie = new()
    {
        "vfx/common/eff/z3oy_stlp6_c0c.avfx",
        "vfx/common/eff/z3oy_stlp4_c0c.avfx"
    };

    private readonly List<string> VfxTruth = new()
    {
        "vfx/common/eff/z3oy_stlp7_c0c.avfx",
        "vfx/common/eff/z3oy_stlp5_c0c.avfx"
    };

    private record struct StatusInfo(uint objectId, uint statusId);

    private List<StatusInfo> FakeStatuses = new();
    private List<uint>? _debuffList;

    public class Debuffs
    {
        /// <summary>
        /// 本物: 止まる / 嘘: 動く
        /// </summary>
        public static uint[] DebuffDontMove = new uint[] { 5546, 1072, 1384, 2657, 3793, 3802, 4144 };

        /// <summary>
        /// 本物: 見ない / 嘘: 見る
        /// </summary>
        public static uint[] DebuffLookAway = new uint[] { 5543, 452 };

        /// <summary>
        /// 本物: 頭割り / 嘘: 散開
        /// </summary>
        public static uint[] DebuffStack = new uint[] { 1023, 5545, 2142 };

        /// <summary>
        /// 本物: 散開 / 嘘: 頭割り
        /// </summary>
        public static uint[] DebuffSpread = new uint[] { 587, 3799, 5544 };

        /// <summary>
        /// 本物: 円AoE / 嘘: ドーナツ
        /// </summary>
        public static uint[] DebuffFireSpread = new uint[] { 1600, 5547 };

        /// <summary>
        /// 本物: ドーナツ / 嘘: 円AoE
        /// </summary>
        public static uint[] DebuffDonut = new uint[] { 1601, 5548 };

        /// <summary>
        /// 本物ならギミック成功側。嘘なら失敗側として扱う。
        /// </summary>
        public static uint DebuffLive = 454;

        /// <summary>
        /// 本物ならギミック失敗側。嘘なら成功側として扱う。
        /// </summary>
        public static uint[] DebuffDie = new uint[] { 1382, 5464 };

        /// <summary>
        /// DebuffLiveと組なら黒、DebuffDieと組なら白。
        /// 嘘なら反転。
        /// </summary>
        public static uint[] DebuffWhitewould = new uint[] { 4887, 5541 };

        /// <summary>
        /// DebuffLiveと組なら白、DebuffDieと組なら黒。
        /// 嘘なら反転。
        /// </summary>
        public static uint[] DebuffBlackwound = new uint[] { 4888, 5542 };
    }

    private Dictionary<uint, bool> IsTruth = new();

    public List<uint> DebuffList
    {
        get
        {
            if (_debuffList == null)
            {
                _debuffList = new();

                foreach (var x in typeof(Debuffs).GetFields().Select(f => f.GetValue(null)!))
                {
                    if (x is uint u)
                    {
                        _debuffList.Add(u);
                    }
                    else if (x is uint[] arr)
                    {
                        _debuffList.AddRange(arr);
                    }
                }
            }

            return _debuffList;
        }
    }

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("Black", """
        {"Name":"","type":3,"refY":40.0,"radius":12,"fillIntensity":0.6,"refActorNPCNameID":6055,"refActorRequireCast":true,"refActorCastId":[50069],"refActorComparisonType":6,"includeRotation":true}
        """);

        Controller.RegisterElementFromCode("White", """
        {"Name":"","type":3,"refY":40.0,"radius":12,"fillIntensity":0.6,"refActorNPCNameID":6055,"refActorRequireCast":true,"refActorCastId":[50068],"refActorComparisonType":6,"includeRotation":true}
        """);

        Controller.RegisterElementsFromMultilineCode("""
        {"Name":"LookAway","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":2550136832,"overlayTextColor":4278190335,"thicc":3.0,"overlayText":"見ない","refActorName":"*","refActorRequireBuff":true,"refActorBuffId":[5543,452],"refActorUseBuffTime":true,"refActorBuffTimeMax":15.0,"tether":true}
        {"Name":"LookAt","type":1,"radius":0.0,"color":3355508521,"fillIntensity":0.5,"overlayBGColor":2550136832,"overlayTextColor":4278255376,"thicc":3.0,"overlayText":"見る","refActorName":"*","refActorRequireBuff":true,"refActorBuffId":[5543,452],"refActorUseBuffTime":true,"refActorBuffTimeMax":15.0,"tether":true}
        {"Name":"EyeScope","type":4,"radius":15.0,"coneAngleMin":-45,"coneAngleMax":45,"color":3355506687,"fillIntensity":0.125,"thicc":3.0,"refActorType":1,"includeRotation":true,"FillStep":99.0,"RenderEngineKind":2}
        {"Name":"Hint","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayTextColor":4292739327,"overlayVOffset":5.0,"thicc":0.0,"overlayText":"","refActorType":1}
        """);
    }

    public override void OnUpdate()
    {
        Controller.Hide();
        PruneFakeStatuses();

        if (BasePlayer.HasStatus(Debuffs.DebuffWhitewould.Concat(Debuffs.DebuffBlackwound).ToArray(), out var status))
        {
            var showWhite = status[0].ID.EqualsAny(Debuffs.DebuffWhitewould);

            if (FakeStatuses.Contains(new(BasePlayer.ObjectId, status[0].ID)))
            {
                showWhite = !showWhite;
            }

            if (BasePlayer.HasStatus(Debuffs.DebuffDie) && !FakeStatuses.ContainsAny(Debuffs.DebuffDie.Select(x => new StatusInfo(BasePlayer.ObjectId, x))))
            {
                showWhite = !showWhite;
            }

            if (BasePlayer.HasStatus(Debuffs.DebuffLive) && FakeStatuses.Contains(new(BasePlayer.ObjectId, Debuffs.DebuffLive)))
            {
                showWhite = !showWhite;
            }

            Controller.GetElementByName(showWhite ? "White" : "Black")!.Enabled = true;
        }

        List<(string Text, float Time)> hints = new();

        foreach (var x in Controller.GetPartyMembers())
        {
            if (x.HasStatus(Debuffs.DebuffLookAway, out var time, lessThan: 10))
            {
                var fake = FakeStatuses.ContainsAny(Debuffs.DebuffLookAway.Select(s => new StatusInfo(x.ObjectId, s)));
                var remain = time.SafeSelect(0).Time;

                hints.Add((fake ? $"見る：あと {remain:F1}s" : $"見ない：あと {remain:F1}s", remain));
                Controller.GetElementByName(fake ? "LookAt" : "LookAway")!.Enabled = true;
                Controller.GetElementByName("EyeScope")!.Enabled = true;
                break;
            }
        }

        bool spread = false;

        if (BasePlayer.HasStatus(Debuffs.DebuffStack, out var stackTime, lessThan: 10f)
            && FakeStatuses.ContainsAny(Debuffs.DebuffStack.Select(s => new StatusInfo(BasePlayer.ObjectId, s))))
        {
            var remain = stackTime.SafeSelect(0).Time;
            hints.Add(($"散開：あと {remain:F1}s", remain));
            spread = true;
        }

        if (BasePlayer.HasStatus(Debuffs.DebuffSpread, out var spreadTime, lessThan: 10f)
            && !FakeStatuses.ContainsAny(Debuffs.DebuffSpread.Select(s => new StatusInfo(BasePlayer.ObjectId, s))))
        {
            var remain = spreadTime.SafeSelect(0).Time;
            hints.Add(($"散開：あと {remain:F1}s", remain));
            spread = true;
        }

        if (!spread)
        {
            foreach (var x in Controller.GetPartyMembers())
            {
                if (x.HasStatus(Debuffs.DebuffStack, out var time, lessThan: 10f)
                    && !FakeStatuses.ContainsAny(Debuffs.DebuffStack.Select(s => new StatusInfo(x.ObjectId, s))))
                {
                    var remain = time.SafeSelect(0).Time;
                    hints.Add(($"頭割り：あと {remain:F1}s", remain));
                    break;
                }

                if (x.HasStatus(Debuffs.DebuffSpread, out time, lessThan: 10f)
                    && FakeStatuses.ContainsAny(Debuffs.DebuffSpread.Select(s => new StatusInfo(x.ObjectId, s))))
                {
                    var remain = time.SafeSelect(0).Time;
                    hints.Add(($"頭割り：あと {remain:F1}s", remain));
                    break;
                }
            }
        }

        if (BasePlayer.HasStatus(Debuffs.DebuffDontMove, out var dontMoveTime, lessThan: 10f))
        {
            var fake = FakeStatuses.ContainsAny(Debuffs.DebuffDontMove.Select(s => new StatusInfo(BasePlayer.ObjectId, s)));
            var remain = dontMoveTime.SafeSelect(0).Time;
            hints.Add((fake ? $"動く：あと {remain:F1}s" : $"止まる：あと {remain:F1}s", remain));
        }

        if (BasePlayer.HasStatus(Debuffs.DebuffDonut, out var donutTime, lessThan: 10f))
        {
            var fake = FakeStatuses.ContainsAny(Debuffs.DebuffDonut.Select(s => new StatusInfo(BasePlayer.ObjectId, s)));
            var remain = donutTime.SafeSelect(0).Time;
            hints.Add((fake ? $"円AoE：あと {remain:F1}s" : $"ドーナツ：あと {remain:F1}s", remain));
        }

        if (BasePlayer.HasStatus(Debuffs.DebuffFireSpread, out var fireSpreadTime, lessThan: 10f))
        {
            var fake = FakeStatuses.ContainsAny(Debuffs.DebuffFireSpread.Select(s => new StatusInfo(BasePlayer.ObjectId, s)));
            var remain = fireSpreadTime.SafeSelect(0).Time;
            hints.Add((fake ? $"ドーナツ：あと {remain:F1}s" : $"円AoE：あと {remain:F1}s", remain));
        }

        if (Controller.TryGetElementByName("Hint", out var e))
        {
            var text = hints
                .OrderBy(x => x.Time)
                .ThenBy(x => x.Text)
                .Select(x => x.Text)
                .Print("\n");

            e.Enabled = !string.IsNullOrWhiteSpace(text);
            e.overlayText = text;
        }
    }

    public override void OnReset()
    {
        IsTruth.Clear();
        FakeStatuses.Clear();
        IsLie = false;
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if (target.GetObject()?.DataId.EqualsAny<uint>(19510, 19507) == true)
        {
            if (VfxTruth.Contains(vfxPath))
            {
                IsTruth[target] = true;
            }
            else if (VfxLie.Contains(vfxPath))
            {
                IsTruth[target] = false;
            }
        }
    }

    public bool IsLie = false;

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (set.Action != null && set.Source?.ObjectId.EqualsAny(IsTruth.Keys) == true)
        {
            IsLie = !IsTruth[set.Source.ObjectId];
        }
    }

    public override void OnGainBuffEffect(uint sourceId, FFXIVClientStructs.FFXIV.Client.Game.Status Status)
    {
        if (DebuffList.Contains(Status.StatusId) && sourceId.TryGetPlayer(out var pc))
        {
            if (IsLie)
            {
                var info = new StatusInfo(sourceId, Status.StatusId);
                if (!FakeStatuses.Contains(info))
                {
                    FakeStatuses.Add(info);
                }
            }
        }
    }

    private void PruneFakeStatuses()
    {
        FakeStatuses.RemoveAll(x =>
        {
            if (!x.objectId.TryGetPlayer(out var pc))
            {
                return true;
            }

            return !pc.HasStatus(x.statusId);
        });
    }

    public override void OnSettingsDraw()
    {
        if (ImGui.CollapsingHeader("Debug"))
        {
            if (ImGui.Button("Export")) GenericHelpers.Copy(JsonConvert.SerializeObject(FakeStatuses));
            if (ImGui.Button("Import")) FakeStatuses = JsonConvert.DeserializeObject<List<StatusInfo>>(GenericHelpers.Paste()) ?? throw new NullReferenceException();

            ImGui.Checkbox(nameof(IsLie), ref IsLie);

            ImGuiEx.Text($"List: {DebuffList.Print()}");
            ImGuiEx.Text($"Casters: {IsTruth.Select(x => $"{x.Key}: {x.Value}").Print("\n")}");
            ImGuiEx.Text($"Fakes: \n{FakeStatuses.Select(x => $"{x.objectId.GetObject()} / {x.statusId} ({Svc.Data.GetExcelSheet<Status>().GetRowOrDefault(x.statusId)?.Name})").Print("\n")}");
        }
    }
}
