﻿using DateWithDestiny.Utilities.Movement;
using ECommons.Automation;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using PlayerController = DateWithDestiny.Utilities.Structs.PlayerController;
#nullable disable

namespace DateWithDestiny.Utilities;

public static unsafe class PlayerEx
{
    public static Character* Character => (Character*)Svc.ClientState.LocalPlayer.Address;
    public static BattleChara* BattleChara => (BattleChara*)Svc.ClientState.LocalPlayer.Address;
    public static CSGameObject* GameObject => (CSGameObject*)Svc.ClientState.LocalPlayer.Address;

    public static bool Occupied => IsOccupied();

    //public static PlayerController* Controller => (PlayerController*)Svc.SigScanner.GetStaticAddressFromSig(Memory.Signatures.PlayerController);
    public static bool HasPenalty => FFXIVClientStructs.FFXIV.Client.Game.UI.InstanceContent.Instance()->GetPenaltyRemainingInMinutes(0) > 0;
    public static bool InPvP => GameMain.IsInPvPInstance();
    public static bool InFlightAllowedTerritory => GetRow<TerritoryType>(Svc.ClientState.TerritoryType)?.Unknown4 != 0;
    public static bool AllowedToFly => PlayerState.Instance()->IsAetherCurrentZoneComplete(Svc.ClientState.TerritoryType);
    public static Vector3 Position { get => Svc.ClientState.LocalPlayer.Position; set => GameObject->SetPosition(value.X, value.Y, value.Z); }
    //public static float Speed { get => Controller->MoveControllerWalk.BaseMovementSpeed; set => Memory.SetSpeed(6 * value); }
    public static DGameObject Target { get => Svc.Targets.Target; set => Svc.Targets.Target = value; }
    public static bool IsTargetLocked => *(byte*)((nint)TargetSystem.Instance() + 309) == 1;
    public static bool IsCasting => Svc.ClientState.LocalPlayer.IsCasting;
    public static float AnimationLock => ActionManager.Instance()->AnimationLock;

    public static FlagMapMarker MapFlag => AgentMap.Instance()->FlagMapMarker;

    public static unsafe Camera* Camera => CameraManager.Instance()->GetActiveCamera();
    public static unsafe CameraEx* CameraEx => (CameraEx*)CameraManager.Instance()->GetActiveCamera();

    public static List<MapMarkerData> QuestLocations => FFXIVClientStructs.FFXIV.Client.Game.UI.Map.Instance()->QuestMarkers.ToArray().SelectMany(i => i.MarkerData.ToList()).ToList();

    private static int EquipAttemptLoops = 0;
    public static void Equip(uint itemID)
    {
        if (Inventory.HasItemEquipped(itemID)) return;

        var pos = Inventory.GetItemLocationInInventory(itemID, Inventory.Equippable);
        if (pos == null)
        {
            DuoLog.Error($"Failed to find item {GetRow<Item>(itemID)?.Name} (ID: {itemID}) in inventory");
            return;
        }

        var agentId = Inventory.Armory.Contains(pos.Value.inv) ? AgentId.ArmouryBoard : AgentId.Inventory;
        var addonId = AgentModule.Instance()->GetAgentByInternalId(agentId)->GetAddonId();
        var ctx = AgentInventoryContext.Instance();
        ctx->OpenForItemSlot(pos.Value.inv, pos.Value.slot, addonId);

        var contextMenu = (AtkUnitBase*)Svc.GameGui.GetAddonByName("ContextMenu");
        if (contextMenu != null)
        {
            for (var i = 0; i < contextMenu->AtkValuesCount; i++)
            {
                var firstEntryIsEquip = ctx->EventIds[i] == 25; // i'th entry will fire eventid 7+i; eventid 25 is 'equip'
                if (firstEntryIsEquip)
                {
                    Svc.Log.Debug($"Equipping item #{itemID} from {pos.Value.inv} @ {pos.Value.slot}, index {i}");
                    Callback.Fire(contextMenu, true, 0, i - 7, 0, 0, 0); // p2=-1 is close, p2=0 is exec first command
                }
            }
            Callback.Fire(contextMenu, true, 0, -1, 0, 0, 0);
            EquipAttemptLoops++;

            if (EquipAttemptLoops >= 5)
            {
                DuoLog.Error($"Equip option not found after 5 attempts. Aborting.");
                return;
            }
        }
    }
}
