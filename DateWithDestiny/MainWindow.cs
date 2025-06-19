using DateWithDestiny.Utilities;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ECommons.ImGuiMethods;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using ImGuiNET;
using static FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentHousingPlant;
using Lumina.Excel.Sheets;
using ECommons.Configuration;
using System;
using ECommons.ExcelServices;

namespace DateWithDestiny;
internal class MainWindow(DateWithDestiny tweak) : Window($"Fate Tracker##{nameof(MainWindow)}"), IDisposable
{
    private readonly DateWithDestiny _tweak = tweak;
    internal uint SelectedTerritory = 0;

    public override bool DrawConditions() => Player.Available;

    public override unsafe void Draw()
    {
        if (ImGui.BeginTabBar("Tabs"))
        {
            if (ImGui.BeginTabItem("Fates"))
            {
                DrawMainTab();
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Config"))
            {
                DrawConfigTab();
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }
    }

    public unsafe void DrawConfigTab()
    {
        //ImGui.Checkbox("Yo-Kai Mode (Very Experimental)", ref yokaiMode);
        ImGui.Checkbox("Prioritize targeting Forlorns", ref P.Config.PrioritizeForlorns);
        ImGui.Checkbox("Prioritize Fates with EXP bonus", ref P.Config.PrioritizeBonusFates);
        ImGui.Indent();
        using (var _ = ImRaii.Disabled(!P.Config.PrioritizeBonusFates))
        {
            ImGui.Checkbox("Only with Twist of Fate", ref P.Config.BonusWhenTwist);
        }
        ImGui.Unindent();
        ImGui.Checkbox("Prioritize fates that have progress already (up to configured limit)", ref P.Config.PrioritizeStartedFates);
        ImGui.Checkbox("Always close to melee range of target", ref P.Config.StayInMeleeRange);
        //ImGui.Checkbox("Full Auto Mode", ref P.Config.FullAuto);
        //if (ImGui.IsItemHovered()) ImGui.SetTooltip($"All the below options will be treated as true if this is enabled.");
        //ImGui.Indent();
        //using (var _ = ImRaii.Disabled(P.Config.FullAuto))
        //{
        //    ImGui.Checkbox("Auto Mount", ref P.Config.AutoMount);
        //    ImGui.Checkbox("Auto Fly", ref P.Config.AutoFly);
        //    ImGui.Checkbox("Auto Sync", ref P.Config.AutoSync);
        //    ImGui.Checkbox("Auto Target Mobs", ref P.Config.AutoTarget);
        //    ImGui.Checkbox("Auto Move To Mobs", ref P.Config.AutoMoveToMobs);
        //    ImGui.Checkbox("Path To Next Fate", ref P.Config.PathToFate);
        //}
        //ImGui.Unindent();

        ImGuiX.DrawSection("Fate Options");
        ImGui.DragInt("Max Duration (s)", ref P.Config.MaxDuration);
        ImGui.SameLine();
        ImGuiX.ResetButton(ref P.Config.MaxDuration, 900);

        ImGui.DragInt("Min Time Remaining (s)", ref P.Config.MinTimeRemaining);
        ImGui.SameLine();
        ImGuiX.ResetButton(ref P.Config.MinTimeRemaining, 120);

        ImGui.DragInt("Max Progress (%)", ref P.Config.MaxProgress, 1, 0, 100);
        ImGui.SameLine();
        ImGuiX.ResetButton(ref P.Config.MaxProgress, 90);

        ImGuiX.DrawSection("Fate Window Options");
        ImGui.Checkbox("Show Time Remaining", ref P.Config.ShowFateTimeRemaining);
        ImGui.Checkbox("Show Bonus Indicator", ref P.Config.ShowFateBonusIndicator);
        ImGui.Checkbox("Change Instances (Requires Lifestream)", ref P.Config.ChangeInstances);


        ImGui.SliderInt("Bicolor Exchange Threshold", ref P.Config.BicolorThreshold, 0, 1500);

        var fateShops = Svc.Data.GetExcelSheet<FateShop>();

        var savedShopName = "";
        var savedShopLevel = Svc.Data.GetExcelSheet<Level>().FirstOrDefault(r => r.Object.RowId == P.Config.BicolorShopId);
        savedShopName = savedShopLevel.Territory.Value.PlaceName.Value.Name.ExtractText();
        if (ImGui.BeginCombo($"Bicolor Exchange Location##FateShop", savedShopName))
        {
            foreach (var shop in fateShops)
            {
                // var eNpcResident = Svc.Data.GetExcelSheet<ENpcResident>().Where(r => r.RowId == shop.RowId);
                // var name = eNpcResident.FirstOrDefault().Singular;

                var level = Svc.Data.GetExcelSheet<Level>().FirstOrDefault(r => r.Object.RowId == shop.RowId);
                var shopLocation = level.Territory.Value.PlaceName.Value.Name.ExtractText();
                // shopLocation += level.RowId;

                var selected = ImGui.Selectable(shopLocation, P.Config.BicolorShopId == shop.RowId);

                if (selected)
                {
                    P.Config.BicolorShopId = shop.RowId;
                    P.Config.BicolorPurchaseItem = 0;
                }
            }

            ImGui.EndCombo();
        }

        var savedItemName = (P.Config.BicolorPurchaseItem == 0) ? "" : GetSheet<Item>().FirstOrDefault(i => i.RowId == P.Config.BicolorPurchaseItem).GetName();
        if (ImGui.BeginCombo($"Bicolor Exchange Item##SpecialShop", savedItemName))
        {
            // default blank
            ImGui.Selectable("", P.Config.BicolorPurchaseItem == 0);
            var specialShops = fateShops.FirstOrDefault(f => f.RowId == P.Config.BicolorShopId).SpecialShop;
            foreach (var shop in specialShops)
            {
                foreach (var itemStruct in shop.Value.Item)
                {
                    foreach (var recievedItem in itemStruct.ReceiveItems)
                    {
                        var item = recievedItem.Item;
                        if (item.RowId != 0)
                        {
                            var selected = ImGui.Selectable(item.Value.GetName(), P.Config.BicolorShopId == shop.RowId);
                            if (selected)
                            {
                                P.Config.BicolorPurchaseItem = item.RowId;
                            }
                        }
                    }
                }
            }

            ImGui.EndCombo();
        }
    }

    public unsafe void DrawMainTab()
    {
        using var table = ImRaii.Table("Fates", 2, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.ScrollX | ImGuiTableFlags.NoHostExtendX);
        if (!table)
            return;

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        //ImGui.TextUnformatted($"Status: {(_tweak.active ? "on" : "off")}"); // (Yo-Kai: {(P.Config.YokaiMode ? "on" : "off")})");
        //ImGui.SetColumnOffset(1, ImGui.GetContentRegionAvail().X - 2 * ImGuiX.IconUnitWidth() - ImGuiHelpers.GetButtonSize("1500").X);
        if (ImGuiComponents.IconButton(!_tweak.active ? FontAwesomeIcon.Play : FontAwesomeIcon.Stop))
        {
            _tweak.active ^= true;
            if (_tweak.active)
            {
                _tweak.Enable();
            }
            else
            {
                _tweak.Disable();
            }
        }
        ImGui.TableNextColumn();
        var bicolorGemstoneCount = InventoryManager.Instance()->GetInventoryItemCount(26807).ToString();
        var bicolorGemstoneX = ImGui.GetWindowWidth() - ImGui.CalcTextSize(bicolorGemstoneCount).X - ImGuiX.IconUnitHeight() - ImGui.GetStyle().ItemSpacing.X;
        ImGui.SetCursorPosX(bicolorGemstoneX);
        //ImGui.SameLine();
        ImGui.Image(Svc.Texture.GetFromGameIcon(new Dalamud.Interface.Textures.GameIconLookup(065071)).GetWrapOrEmpty().ImGuiHandle, new Vector2(ImGuiX.IconUnitHeight()));
        ImGui.SameLine();
        ImGui.TextUnformatted(bicolorGemstoneCount);

        //ImGui.SameLine();
        //if (ImGuiComponents.IconButtonWithText((FontAwesomeIcon)0xf002, "Browse"))
        //{
        //    new TerritorySelector(SelectedTerritory, (_, x) =>
        //    {
        //        SelectedTerritory = x;
        //    });
        //}

        foreach (var fate in Svc.Fates.OrderBy(x => Vector3.Distance(x.Position, Player.Position)))
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            if (ImGuiComponents.IconButton($"###Pathfind{fate.FateId}", FontAwesomeIcon.Map))
            {
                if (!P.Navmesh.IsRunning())
                    P.Navmesh.PathfindAndMoveTo(_tweak.GetRandomPointInFate(fate.FateId), Svc.Condition[ConditionFlag.InFlight]);
                else
                    P.Navmesh.Stop();
            }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip($"Pathfind to {fate.Position}");

            ImGui.SameLine();

            if (ImGuiComponents.IconButton($"###Flag{fate.FateId}", FontAwesomeIcon.Flag))
            {
                unsafe { AgentMap.Instance()->SetFlagMapMarker(Svc.ClientState.TerritoryType, Svc.ClientState.MapId, fate.Position); }
            }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip($"Set map flag to {fate.Position}");

            ImGui.SameLine();

            if (P.Config.ShowFateBonusIndicator && fate.HasBonus)
            {
                ImGui.Image(Svc.Texture.GetFromGameIcon(new Dalamud.Interface.Textures.GameIconLookup(65001)).GetWrapOrEmpty().ImGuiHandle, new Vector2(ImGuiX.IconUnitHeight()));

                ImGui.SameLine();
            }

            var nameColour = _tweak.FateConditions(fate) ? new Vector4(1, 1, 1, 1) : P.Config.blacklist.Contains(fate.FateId) ? new Vector4(1, 0, 0, 0.5f) : new Vector4(1, 1, 1, 0.5f);
            ImGuiEx.TextV(nameColour, $"{fate.Name} {(P.Config.ShowFateTimeRemaining && fate.TimeRemaining >= 0 ? new global::System.TimeSpan(0, 0, (int)fate.TimeRemaining) : string.Empty)}");
            if (ImGui.IsItemHovered()) ImGui.SetTooltip($"[{fate.FateId}] {fate.Position} {fate.Progress}%% {fate.TimeRemaining}/{fate.Duration}\nFate {(_tweak.FateConditions(fate) ? "meets" : "doesn't meet")} conditions and {(_tweak.FateConditions(fate) ? "will" : "won't")} be pathed to in auto mode.");

            ImGui.TableNextColumn();

            ImGuiX.DrawProgressBar(fate.Progress, 100, new Vector4(0.404f, 0.259f, 0.541f, 1));

            ImGui.SameLine();

            ImGui.SetCursorPosX(ImGui.GetContentRegionMax().X - ImGuiX.IconUnitWidth() - ImGui.GetStyle().WindowPadding.X);
            if (ImGuiComponents.IconButton($"###Blacklist{fate.FateId}", FontAwesomeIcon.Ban))
            {
                P.Config.blacklist.Add(fate.FateId);
            }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip($"Add to blacklist. Right click to remove.");
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                P.Config.blacklist.Remove(fate.FateId);
            }
        }
    }

    public void Dispose() { }
}
