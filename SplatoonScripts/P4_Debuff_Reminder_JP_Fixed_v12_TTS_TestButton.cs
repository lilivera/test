using Dalamud.Bindings.ImGui;
using ECommons;
using ECommons.GameFunctions;
using ECommons.Hooks.ActionEffectTypes;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.Dancing_Mad
{
    public class P4_Debuff_Reminder_JP_Fixed_v12_TTS_TestButton : SplatoonScript
    {
        public override Metadata Metadata { get; } = new Metadata(12, "NightmareXIV / JP fixed / large white text / TTS test button");
        public override HashSet<uint> ValidTerritories { get; } = new HashSet<uint> { 1363 };

        private const float TtsTriggerSeconds = 7.0f;
        private HashSet<string> _spokenTts = new HashSet<string>();

        private readonly List<string> VfxLie = new List<string>
        {
            "vfx/common/eff/z3oy_stlp6_c0c.avfx",
            "vfx/common/eff/z3oy_stlp4_c0c.avfx"
        };

        private readonly List<string> VfxTruth = new List<string>
        {
            "vfx/common/eff/z3oy_stlp7_c0c.avfx",
            "vfx/common/eff/z3oy_stlp5_c0c.avfx"
        };

        private struct StatusInfo : IEquatable<StatusInfo>
        {
            public uint objectId;
            public uint statusId;

            public StatusInfo(uint objectId, uint statusId)
            {
                this.objectId = objectId;
                this.statusId = statusId;
            }

            public bool Equals(StatusInfo other)
            {
                return objectId == other.objectId && statusId == other.statusId;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is StatusInfo)) return false;
                return Equals((StatusInfo)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((int)objectId * 397) ^ (int)statusId;
                }
            }
        }

        private struct HintInfo
        {
            public string Text;
            public float Time;

            public HintInfo(string text, float time)
            {
                Text = text;
                Time = time;
            }
        }

        private List<StatusInfo> FakeStatuses = new List<StatusInfo>();
        private List<uint> _debuffList;
        private Dictionary<uint, bool> IsTruth = new Dictionary<uint, bool>();
        public bool IsLie = false;

        public class Debuffs
        {
            public static uint[] DebuffDontMove = new uint[] { 5546, 1072, 1384, 2657, 3793, 3802, 4144 };
            public static uint[] DebuffLookAway = new uint[] { 5543, 452 };
            public static uint[] DebuffStack = new uint[] { 1023, 5545, 2142 };
            public static uint[] DebuffSpread = new uint[] { 587, 3799, 5544 };
            public static uint[] DebuffFireSpread = new uint[] { 1600, 5547 };
            public static uint[] DebuffDonut = new uint[] { 1601, 5548 };
            public static uint DebuffLive = 454;
            public static uint[] DebuffDie = new uint[] { 1382, 5464 };
            public static uint[] DebuffWhitewould = new uint[] { 4887, 5541 };
            public static uint[] DebuffBlackwound = new uint[] { 4888, 5542 };
        }

        public List<uint> DebuffList
        {
            get
            {
                if (_debuffList == null)
                {
                    _debuffList = new List<uint>();

                    foreach (var x in typeof(Debuffs).GetFields().Select(f => f.GetValue(null)))
                    {
                        if (x is uint)
                        {
                            _debuffList.Add((uint)x);
                        }
                        else if (x is uint[])
                        {
                            _debuffList.AddRange((uint[])x);
                        }
                    }
                }

                return _debuffList;
            }
        }

        public override void OnSetup()
        {
            Controller.RegisterElementFromCode("Black", "{\"Name\":\"\",\"type\":3,\"refY\":40.0,\"radius\":12,\"fillIntensity\":0.6,\"refActorNPCNameID\":6055,\"refActorRequireCast\":true,\"refActorCastId\":[50069],\"refActorComparisonType\":6,\"includeRotation\":true}");
            Controller.RegisterElementFromCode("White", "{\"Name\":\"\",\"type\":3,\"refY\":40.0,\"radius\":12,\"fillIntensity\":0.6,\"refActorNPCNameID\":6055,\"refActorRequireCast\":true,\"refActorCastId\":[50068],\"refActorComparisonType\":6,\"includeRotation\":true}");

            Controller.RegisterElementsFromMultilineCode(string.Join("\n", new string[]
            {
                "{\"Name\":\"LookAway\",\"type\":1,\"radius\":0.0,\"fillIntensity\":0.5,\"overlayBGColor\":3221225472,\"overlayTextColor\":4294967295,\"overlayFScale\":2.4,\"thicc\":3.0,\"overlayText\":\"見ない\",\"refActorName\":\"*\",\"refActorRequireBuff\":true,\"refActorBuffId\":[5543,452],\"refActorUseBuffTime\":true,\"refActorBuffTimeMax\":15.0,\"tether\":true}",
                "{\"Name\":\"LookAt\",\"type\":1,\"radius\":0.0,\"color\":3355508521,\"fillIntensity\":0.5,\"overlayBGColor\":3221225472,\"overlayTextColor\":4294967295,\"overlayFScale\":2.4,\"thicc\":3.0,\"overlayText\":\"見る\",\"refActorName\":\"*\",\"refActorRequireBuff\":true,\"refActorBuffId\":[5543,452],\"refActorUseBuffTime\":true,\"refActorBuffTimeMax\":15.0,\"tether\":true}",
                "{\"Name\":\"EyeScope\",\"type\":4,\"radius\":15.0,\"coneAngleMin\":-45,\"coneAngleMax\":45,\"color\":3355506687,\"fillIntensity\":0.125,\"thicc\":3.0,\"refActorType\":1,\"includeRotation\":true,\"FillStep\":99.0,\"RenderEngineKind\":2}",
                "{\"Name\":\"Hint\",\"type\":1,\"radius\":0.0,\"Filled\":false,\"fillIntensity\":0.5,\"overlayBGColor\":3221225472,\"overlayTextColor\":4294967295,\"overlayFScale\":3.0,\"overlayVOffset\":5.0,\"thicc\":0.0,\"overlayText\":\"\",\"refActorType\":1}"
            }));
        }

        public override void OnUpdate()
        {
            Controller.Hide();
            PruneFakeStatuses();

            if (BasePlayer.HasStatus(Debuffs.DebuffWhitewould.Concat(Debuffs.DebuffBlackwound).ToArray(), out var status))
            {
                var showWhite = status[0].ID.EqualsAny(Debuffs.DebuffWhitewould);

                if (FakeStatuses.Contains(new StatusInfo(BasePlayer.ObjectId, status[0].ID)))
                {
                    showWhite = !showWhite;
                }

                if (BasePlayer.HasStatus(Debuffs.DebuffDie) && !FakeStatuses.ContainsAny(Debuffs.DebuffDie.Select(x => new StatusInfo(BasePlayer.ObjectId, x))))
                {
                    showWhite = !showWhite;
                }

                if (BasePlayer.HasStatus(Debuffs.DebuffLive) && FakeStatuses.Contains(new StatusInfo(BasePlayer.ObjectId, Debuffs.DebuffLive)))
                {
                    showWhite = !showWhite;
                }

                Controller.GetElementByName(showWhite ? "White" : "Black").Enabled = true;
            }

            List<HintInfo> hints = new List<HintInfo>();
            List<string> ttsQueue = new List<string>();
            HashSet<string> activeTtsKeys = new HashSet<string>();

            foreach (var x in Controller.GetPartyMembers())
            {
                if (x.HasStatus(Debuffs.DebuffLookAway, out var time, lessThan: 10))
                {
                    var fake = FakeStatuses.ContainsAny(Debuffs.DebuffLookAway.Select(s => new StatusInfo(x.ObjectId, s)));
                    var remain = time.SafeSelect(0).Time;
                    var word = fake ? "見る" : "見ない";
                    AddHint(hints, activeTtsKeys, ttsQueue, fake ? "look_at" : "look_away", word, $"{word}：あと {remain:F1}s", remain);
                    Controller.GetElementByName(fake ? "LookAt" : "LookAway").Enabled = true;
                    Controller.GetElementByName("EyeScope").Enabled = true;
                    break;
                }
            }

            bool spread = false;

            if (BasePlayer.HasStatus(Debuffs.DebuffStack, out var stackTime, lessThan: 10f) && FakeStatuses.ContainsAny(Debuffs.DebuffStack.Select(s => new StatusInfo(BasePlayer.ObjectId, s))))
            {
                var remain = stackTime.SafeSelect(0).Time;
                AddHint(hints, activeTtsKeys, ttsQueue, "spread", "散開", $"散開：あと {remain:F1}s", remain);
                spread = true;
            }

            if (BasePlayer.HasStatus(Debuffs.DebuffSpread, out var spreadTime, lessThan: 10f) && !FakeStatuses.ContainsAny(Debuffs.DebuffSpread.Select(s => new StatusInfo(BasePlayer.ObjectId, s))))
            {
                var remain = spreadTime.SafeSelect(0).Time;
                AddHint(hints, activeTtsKeys, ttsQueue, "spread", "散開", $"散開：あと {remain:F1}s", remain);
                spread = true;
            }

            if (!spread)
            {
                foreach (var x in Controller.GetPartyMembers())
                {
                    if (x.HasStatus(Debuffs.DebuffStack, out var time, lessThan: 10f) && !FakeStatuses.ContainsAny(Debuffs.DebuffStack.Select(s => new StatusInfo(x.ObjectId, s))))
                    {
                        var remain = time.SafeSelect(0).Time;
                        AddHint(hints, activeTtsKeys, ttsQueue, "stack", "頭割り", $"頭割り：あと {remain:F1}s", remain);
                        break;
                    }

                    if (x.HasStatus(Debuffs.DebuffSpread, out time, lessThan: 10f) && FakeStatuses.ContainsAny(Debuffs.DebuffSpread.Select(s => new StatusInfo(x.ObjectId, s))))
                    {
                        var remain = time.SafeSelect(0).Time;
                        AddHint(hints, activeTtsKeys, ttsQueue, "stack", "頭割り", $"頭割り：あと {remain:F1}s", remain);
                        break;
                    }
                }
            }

            if (BasePlayer.HasStatus(Debuffs.DebuffDontMove, out var dontMoveTime, lessThan: 10f))
            {
                var fake = FakeStatuses.ContainsAny(Debuffs.DebuffDontMove.Select(s => new StatusInfo(BasePlayer.ObjectId, s)));
                var remain = dontMoveTime.SafeSelect(0).Time;
                var key = fake ? "move" : "stop";
                var word = fake ? "動く" : "止まる";
                AddHint(hints, activeTtsKeys, ttsQueue, key, word, $"{word}：あと {remain:F1}s", remain);
            }

            if (BasePlayer.HasStatus(Debuffs.DebuffDonut, out var donutTime, lessThan: 10f))
            {
                var fake = FakeStatuses.ContainsAny(Debuffs.DebuffDonut.Select(s => new StatusInfo(BasePlayer.ObjectId, s)));
                var remain = donutTime.SafeSelect(0).Time;
                if (fake)
                {
                    AddHint(hints, activeTtsKeys, ttsQueue, "aoe", "円エーオーイー", $"円AoE：あと {remain:F1}s", remain);
                }
                else
                {
                    AddHint(hints, activeTtsKeys, ttsQueue, "donut", "ドーナツ", $"ドーナツ：あと {remain:F1}s", remain);
                }
            }

            if (BasePlayer.HasStatus(Debuffs.DebuffFireSpread, out var fireSpreadTime, lessThan: 10f))
            {
                var fake = FakeStatuses.ContainsAny(Debuffs.DebuffFireSpread.Select(s => new StatusInfo(BasePlayer.ObjectId, s)));
                var remain = fireSpreadTime.SafeSelect(0).Time;
                if (fake)
                {
                    AddHint(hints, activeTtsKeys, ttsQueue, "donut", "ドーナツ", $"ドーナツ：あと {remain:F1}s", remain);
                }
                else
                {
                    AddHint(hints, activeTtsKeys, ttsQueue, "aoe", "円エーオーイー", $"円AoE：あと {remain:F1}s", remain);
                }
            }

            if (Controller.TryGetElementByName("Hint", out var e))
            {
                var text = hints.OrderBy(x => x.Time).ThenBy(x => x.Text).Select(x => x.Text).Print("\n");
                e.Enabled = !string.IsNullOrWhiteSpace(text);
                e.overlayText = text;
            }

            SpeakPending(ttsQueue);
            _spokenTts.RemoveWhere(x => !activeTtsKeys.Contains(x));
        }

        private void AddHint(List<HintInfo> hints, HashSet<string> activeTtsKeys, List<string> ttsQueue, string key, string speechText, string hintText, float remain)
        {
            hints.Add(new HintInfo(hintText, remain));
            activeTtsKeys.Add(key);

            if (remain <= TtsTriggerSeconds && !_spokenTts.Contains(key))
            {
                _spokenTts.Add(key);
                if (!ttsQueue.Contains(speechText))
                {
                    ttsQueue.Add(speechText);
                }
            }
        }

        private void SpeakPending(List<string> ttsQueue)
        {
            if (ttsQueue.Count == 0)
            {
                return;
            }

            Speak(string.Join("、", ttsQueue));
        }

        private void Speak(string text)
        {
            try
            {
                var safeText = text.Replace("'", "''");
                var script = "Add-Type -AssemblyName System.Speech; $s = New-Object System.Speech.Synthesis.SpeechSynthesizer; $s.Rate = 1; $s.Volume = 100; $s.Speak('" + safeText + "');";
                var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));

                var psi = new ProcessStartInfo();
                psi.FileName = "powershell.exe";
                psi.Arguments = "-NoProfile -ExecutionPolicy Bypass -EncodedCommand " + encoded;
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                Process.Start(psi);
            }
            catch
            {
                // TTS failure must not affect visual hints.
            }
        }

        public override void OnSettingsDraw()
        {
            if (ImGui.Button("TTSテスト"))
            {
                Speak("テスト、散開");
            }

            if (ImGui.Button("TTSテスト 複数"))
            {
                Speak("見ない、散開、止まる");
            }
        }

        public override void OnReset()
        {
            IsTruth.Clear();
            FakeStatuses.Clear();
            IsLie = false;
            _spokenTts.Clear();
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
    }
}
