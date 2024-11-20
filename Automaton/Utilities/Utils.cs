using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin;
using ECommons.Reflection;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;
using ImGuiNET;
using System.Reflection;

namespace Automaton.Utilities;
public static class Utils
{
    public enum MovementType
    {
        Direct,
        Pathfind
    }

    public static IDalamudTextureWrap? GetIcon(uint iconId) => iconId != 0 ? Svc.Texture?.GetFromGameIcon(iconId).GetWrapOrEmpty() : null;

    public static bool HasPlugin(string name) => DalamudReflector.TryGetDalamudPlugin(name, out _, false, true);

    private static readonly Dictionary<Type, AgentId> AgentIdCache = [];
    public static unsafe T* GetAgent<T>(AgentId id) where T : unmanaged
        => (T*)AgentModule.Instance()->GetAgentByInternalId(id);

    public static unsafe T* GetAgent<T>() where T : unmanaged
    {
        var type = typeof(T);

        if (!AgentIdCache.TryGetValue(type, out var id))
        {
            var attr = type.GetCustomAttribute<AgentAttribute>(false)
                ?? throw new Exception($"Agent {type.FullName} is missing AgentAttribute");

            AgentIdCache.Add(type, id = attr.Id);
        }

        return GetAgent<T>(id);
    }

    public const int UnitListCount = 18;
    public static unsafe AtkUnitBase* GetAddonByID(uint id)
    {
        var unitManagers = &AtkStage.Instance()->RaptureAtkUnitManager->AtkUnitManager.DepthLayerOneList;
        for (var i = 0; i < UnitListCount; i++)
        {
            var unitManager = &unitManagers[i];
            foreach (var j in Enumerable.Range(0, Math.Min(unitManager->Count, unitManager->Entries.Length)))
            {
                var unitBase = unitManager->Entries[j].Value;
                if (unitBase != null && unitBase->Id == id)
                {
                    return unitBase;
                }
            }
        }

        return null;
    }

    public static unsafe bool IsClickingInGameWorld()
        => !ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow)
        && !ImGui.GetIO().WantCaptureMouse
        && AtkStage.Instance()->RaptureAtkUnitManager->AtkUnitManager.FocusedUnitsList.Count == 0
        && Framework.Instance()->Cursor->ActiveCursorType == 0;

    public static Vector3 RotatePoint(float cx, float cy, float angle, Vector3 p)
    {
        if (angle == 0f) return p;
        var s = (float)Math.Sin(angle);
        var c = (float)Math.Cos(angle);

        // translate point back to origin:
        p.X -= cx;
        p.Z -= cy;

        // rotate point
        var xnew = p.X * c - p.Z * s;
        var ynew = p.X * s + p.Z * c;

        // translate point back:
        p.X = xnew + cx;
        p.Z = ynew + cy;
        return p;
    }

    public static unsafe Structs.AgentMJICraftSchedule* Agent = (Structs.AgentMJICraftSchedule*)AgentModule.Instance()->GetAgentByInternalId(AgentId.MJICraftSchedule);
    public static unsafe Structs.AgentMJICraftSchedule.AgentData* AgentData => Agent != null ? Agent->Data : null;

    public static unsafe void SetRestCycles(uint mask)
    {
        Svc.Log.Debug($"Setting rest: {mask:X}");
        AgentData->NewRestCycles = mask;
        SynthesizeEvent(5, [new() { Type = AtkValueType.Int, Int = 0 }]);
    }

    private static unsafe void SynthesizeEvent(ulong eventKind, Span<AtkValue> args)
    {
        var eventData = stackalloc int[] { 0, 0, 0 };
        Agent->AgentInterface.ReceiveEvent((AtkValue*)eventData, args.GetPointer(0), (uint)args.Length, eventKind);
    }

    public static T GetService<T>()
    {
        Svc.Log.Info($"Requesting {typeof(T)}");
        var service = typeof(IDalamudPlugin).Assembly.GetType("Dalamud.Service`1")!.MakeGenericType(typeof(T));
        var get = service.GetMethod("Get", BindingFlags.Public | BindingFlags.Static)!;
        return (T)get.Invoke(null, null)!;
    }

    public static bool AllNull(params object[] objects) => objects.All(s => s == null);
    public static bool AnyNull(params object[] objects) => objects.Any(s => s == null);

    public enum ExecuteCommandFlag
    {
        DrawOrSheatheWeapon = 1,
        AutoAttack = 2,
        Target = 3,
        Dismount = 101,
        SummonPet = 102,
        WithdrawPet = 103,
        StatusOff = 104,
        CancelCast = 105,
        RidePillion = 106,
        WithdrawParasol = 109,
        const_10 = 110,
        Revive = 200,
        TerritoryTransport = 201,
        Teleport = 202,
        AcceptTeleportOffer = 203,
        RequestFriendHouseTeleport = 210,
        TeleportToFriendHouse = 211,
        ReturnIfNotLalafell = 213,
        InstantReturn = 214,
        Inspect = 300,
        ChangeTitle = 302,
        RequestCutscene307 = 307,
        RequestContentsNoteCategory = 310,
        ClearFieldMarkers = 313,
        AssignBLUActionToSlot = 315,
        RequestWorldTravel = 316,
        PlaceFieldMarker = 317,
        RemoveFieldMarker = 318,
        ResetStrikingDummy = 319,
        RequestInventory = 405,
        EnterMateriaAttachState = 408,
        LeaveMateriaAttachState = 410,
        CancelMateriaMeldRequest = 419,
        RequestCabinet = 424,
        StoreToCabinet = 425,
        RestoreFromCabinet = 426,
        RepairItem = 434,
        RepairAllItems = 435,
        ExtractMateria = 437,
        GearsetChange = 441,
        RequestSaddleBag = 444,
        InterruptEmote = 502,
        InterruptEmoteSpecial = 503,
        IdlePostureChange = 505,
        IdlePostureEnter = 506,
        IdlePostureExit = 507,
        EnterSwim = 608,
        LeaveSwim = 609,
        DisableMounting = 612,
        EnterFlight = 616,
        Craft = 700,
        Fish = 701,
        LoadCraftLog = 710,
        ExitCraft = 711,
        AbandonQuest = 800,
        RefreshLeveQuest = 801,
        StartLeveQuest = 804,
        Content = 808,
        FateStart = 809,
        FateLoad = 810,
        FateEnter = 812,
        FateLevelSync = 813,
        FateMobSpawn = 814,
        TerritoryTransportFinish = 816,
        LeaveDuty = 819,
        QuestRedo = 824,
        InventoryRefresh = 830,
        RequestCutscene831 = 831,
        RequestAchievement = 1000,
        RequestAllAchievement = 1001,
        RequestNearCompletionAchievement = 1002,
        RequestLotteryData = 1105,
        RequestPlacardData = 1106,
        RequestHousingAreaData = 1107,
        StoreFurniture = 1112,
        RestoreFurniture = 1113,
        RequestHousingName = 1114,
        RequestHousingGreeting = 1115,
        RequestHousingGuestAccess = 1117,
        SaveHousingGuestAccess = 1118,
        RequestHousingEstateTag = 1119,
        SaveHousingEstateTag = 1120,
        MoveToHouseFrontGate = 1122,
        FurnishState = 1123,
        ViewHouseDetail = 1126,
        AdjustHouseLight = 1137,
        RefreshFCMaterialDelivery = 1143,
        SetHouseBackgroundMusic = 1145,
        Furnish = 1150,
        RepairSubmarinePart = 1153,
        CollectTrophyCrystal = 1200,
        RequestContentsNote = 1301,
        RepairItemNPC = 1600,
        RepairAllItemsNPC = 1602,
        BuddyAction = 1700,
        BuddyEquip = 1701,
        BuddyLearnSkill = 1702,
        RequestGSGeneral = 1850,
        RequestGSLordofVerminion = 2010,
        EnableAutoJoinNoviceNetwork = 2102,
        SendDuel = 2200,
        RequestDuel = 2201,
        ConfirmDuel = 2202,
        WondrousTailsConfirm = 2253,
        WondrousTailsOperate = 2253,
        RequestPrismBox = 2350,
        RestorePrsimBoxItem = 2352,
        RequestGlamourPlates = 2355,
        EnterGlamourPlateState = 2356,
        ApplyGlamourPlate = 2357,
        RequestGSMahjong = 2550,
        RequstAOZNotebook = 2601,
        RequestTrustedFriend = 2651,
        RequestDutySupport = 2653,
        Desynthesize = 2800,
        RequestPortraits = 3200,
        MJIWorkshopRequest = 3254,
        MJIWorkshopRequestItem = 3258,
        MJIWorkshopAssign = 3259,
        MJIGranaryCollect = 3262,
        MJIGranaryAssign = 3264,
        MJIPastureCollect = 3271,
        MJIFarmEntrustSingle = 3279,
        MJIFarmDismiss = 3280,
        MJIFarmCollectSingle = 3281,
        MJIFarmCollectAll = 3282,
        MJIFavorStateRequest = 3292,
        RollDice = 9000,
        Retainer = 9003
    }

    public enum ExecuteCommandComplexFlag
    {
        Dismount = 101,
        PlaceMarker = 301,
        Emote = 500,
        EmoteLocation = 501,
        EmoteInterruptLocation = 504,
        Dive = 607,
        PetAction = 1800,
        BgcArmyAction = 1810
    }
}
