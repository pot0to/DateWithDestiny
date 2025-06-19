using DateWithDestiny.IPC;
using Dalamud.Game.ClientState.Fates;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Utility.Raii;
using ECommons.GameFunctions;
using ECommons.SimpleGui;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using ImGuiNET;
using Lumina.Excel.Sheets;
using static ECommons.GameFunctions.ObjectFunctions;
using DateWithDestiny.Utilities;
using System.Security.Policy;
using ECommons.Automation.NeoTaskManager.Tasks;
using ECommons.UIHelpers.AddonMasterImplementations;
using ECommons;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace DateWithDestiny;

public enum DateWithDestinyState
{
    Unknown,
    Ready,
    Mounting,
    MovingToFate,
    InteractingWithNpc,
    InCombat,
    ChangingInstances,
    BicolorExchange,
    Dead,
    SummonChocobo
}

//[Tweak, Requirement(NavmeshIPC.Name, NavmeshIPC.Repo)]
internal class DateWithDestiny
{
    public string Name => "Date with Destiny";
    public string Description => $"Fate tracker and mover. Doesn't handle combat. Open the menu with /vfate.";

    public bool active = false;
    private static Vector3 TargetPos;
    private readonly Throttle action = new();
    private Random random = null!;
    private DateWithDestinyState State { get; set; }
    private DateWithDestinyState PreviousState { get; set; }
    private uint ZoneToFarm { get; set; }

    private static readonly int _teleportTimePenalty = 50; // to account for how long teleport takes you
    private static readonly int _interactDistance = 5;

    public DateWithDestiny()
    {
        State = DateWithDestinyState.Ready;
        PreviousState = DateWithDestinyState.Ready;
        ZoneToFarm = Svc.ClientState.TerritoryType;
        //P.TaskManager.AbortOnTimeout = true;
    }

    private static readonly uint[] ForlornIDs = [7586, 7587];
    private static readonly uint[] TwistOfFateStatusIDs = [1288, 1289];

    private ushort nextFateID;
    private byte fateMaxLevel;

    private ushort FateID
    {
        get; set
        {
            if (field != value)
                SyncFate(value);
            field = value;
        }
    }

    public void Enable()
    {
        //EzConfigGui.WindowSystem.AddWindow(new FateTrackerUI(this));
        random = new();
        Svc.Framework.Update += OnUpdate;
    }

    public void Disable()
    {
        //EzConfigGui.RemoveWindow<FateTrackerUI>();
        P.Navmesh.Stop();
        Svc.Framework.Update -= OnUpdate;
    }

    //[CommandHandler("/vfate", "Opens the FATE tracker")]
    public void OnCommand(string command, string arguments)
    {
        EzConfigGui.GetWindow<MainWindow>()!.IsOpen ^= true;
        EzConfigGui.Window.Toggle();
    }

    private int _successiveInstanceChanges = 0;
    private readonly int _distanceToTargetAetheryte = 50; // object.IsTargetable has a larger range than actually clickable

    private unsafe void OnUpdate(IFramework framework)
    {
        //P.TaskManager.AbortOnTimeout = P.Config.AbortTasksOnTimeout;

        if (!Player.Available || P.TaskManager.IsBusy) return;

        if (!active)
        {
            if (State != DateWithDestinyState.Ready)
            {
                State = DateWithDestinyState.Ready;
                Svc.Log.Info("State Change: " + State.ToString());
            }
            return;
        }

        var cf = FateManager.Instance()->CurrentFate;
        var nextFate = GetFates().FirstOrDefault();
        var bicolorGemstoneCount = Inventory.GetItemCount(26807);
        switch (State)
        {
            case DateWithDestinyState.Ready:
                if (cf != null)
                    State = DateWithDestinyState.InCombat;
                else if (bicolorGemstoneCount >= P.Config.BicolorThreshold)
                    State = DateWithDestinyState.BicolorExchange;
                else if (nextFate == null)
                    State = DateWithDestinyState.ChangingInstances;
                else
                    State = DateWithDestinyState.MovingToFate;
                Svc.Log.Info("State Change: " + State.ToString());
                return;
            case DateWithDestinyState.Mounting:
                if (!PlayerEx.Occupied && !(Svc.Condition[ConditionFlag.Mounted] || Svc.Condition[ConditionFlag.Mounted2]))
                    ExecuteMount();
                //else if ((P.Config.FullAuto || P.Config.AutoFly) && !PlayerEx.Occupied && Svc.Condition[ConditionFlag.Mounted] && !Svc.Condition[ConditionFlag.InFlight])
                //    ExecuteJump();
                else if (Svc.Condition[ConditionFlag.InFlight])
                {
                    State = DateWithDestinyState.MovingToFate;
                    Svc.Log.Info("State Change: " + State.ToString());
                }
                return;
            case DateWithDestinyState.MovingToFate:
                _successiveInstanceChanges = 0;
                unsafe { AgentMap.Instance()->SetFlagMapMarker(Svc.ClientState.TerritoryType, Svc.ClientState.MapId, FateManager.Instance()->GetFateById(nextFate!.FateId)->Location); }
                //if (!Svc.Condition[ConditionFlag.InFlight])
                //{
                //    State = DateWithDestinyState.Mounting;
                //    Svc.Log.Info("State Change: " + State.ToString());
                //    return;
                //}

                //if (P.Config.EquipWatch && YokaiWatchManager.HaveYokaiMinionsMissing() && !YokaiWatchManager.HasWatchEquipped() && GetItemCount(YokaiWatchManager.YokaiWatch) > 0)
                //    PlayerEx.Equip(15222);

                if (!P.Navmesh.PathfindInProgress() && !P.Navmesh.IsRunning())
                {
                    if (cf is not null)
                    {
                        State = DateWithDestinyState.InCombat;
                        Svc.Log.Info("State Change: " + State.ToString());
                    }
                    else
                        MoveToNextFate(nextFate!.FateId);
                }

                // mount while vnav is moving
                if (!PlayerEx.Occupied && !(Svc.Condition[ConditionFlag.Mounted] || Svc.Condition[ConditionFlag.Mounted2]))
                    ExecuteMount();
                //else if ((P.Config.FullAuto || P.Config.AutoFly) && !PlayerEx.Occupied && Svc.Condition[ConditionFlag.Mounted] && !Svc.Condition[ConditionFlag.InFlight])
                //    ExecuteJump();
                else if (Svc.Condition[ConditionFlag.InFlight])
                {
                    State = DateWithDestinyState.MovingToFate;
                    Svc.Log.Info("State Change: " + State.ToString());
                }

                return;
            case DateWithDestinyState.InteractingWithNpc:
                // TODO: not implemented
                return;
            case DateWithDestinyState.InCombat:

                if (cf == null && !Svc.Condition[ConditionFlag.InCombat] && !PlayerEx.IsCasting)
                {
                    State = DateWithDestinyState.Ready;
                    Svc.Log.Info("State Change: " + State.ToString());
                    FateID = 0;
                }
                else
                {
                    if (Svc.Condition[ConditionFlag.Mounted]) ExecuteDismount();

                    var target = Svc.Targets.Target;
                    if (P.Navmesh.IsRunning() && Svc.Targets.Target?.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc &&
                        (DistanceToTarget() < 2 || target != null && DistanceToHitboxEdge(target.HitboxRadius) <= (P.Config.StayInMeleeRange ? 0 : 15)))
                    {
                        P.Navmesh.Stop();
                        return;
                    }

                    // target mobs targeting player (includes unexpected combat outside of fate)
                    if (target == null && Svc.Condition[ConditionFlag.InCombat])
                        target = GetMobTargetingPlayer();

                    // target fate mobs if you're in a fate
                    if (cf != null)
                    {
                        FateID = cf->FateId;
                        if (target == null || target.ObjectKind != Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc)
                            target = GetFateMob();
                    }

                    // if you found a target, go fight it
                    if (target != null)
                    {
                        TargetPos = target.Position;
                        if ((Svc.Targets.Target == null || !IsInMeleeRange(target.HitboxRadius + (P.Config.StayInMeleeRange ? 0 : 15))))
                        {
                            TargetAndMoveToEnemy(target);
                            return;
                        }
                    }
                }
                return;
            case DateWithDestinyState.ChangingInstances:
                Svc.Log.Info("_successiveInstanceChanges: " + _successiveInstanceChanges);
                if (ChangeInstances())
                {
                    State = DateWithDestinyState.Ready;
                    Svc.Log.Info("State Change: " + State.ToString());
                }
                return;
            case DateWithDestinyState.BicolorExchange:
                if (bicolorGemstoneCount < P.Config.BicolorThreshold)
                {

                    State = DateWithDestinyState.Ready;
                }
                else
                {
                    var shopLevel = Svc.Data.GetExcelSheet<Level>().FirstOrDefault(r => r.Object.RowId == P.Config.BicolorShopId);
                    var shopCoords = new Vector3(shopLevel.X, shopLevel.Y, shopLevel.Z);
                    var shopTerritory = shopLevel.Territory;
                    var nearestAetheryte = Coords.GetNearestAetheryte(shopTerritory.RowId, shopCoords);

                    if (nearestAetheryte != null)
                    {
                        Svc.Log.Info($"Nearest aetheryte is: {nearestAetheryte.Value.RowId}");
                        var aetheryteToShop = Coords.GetDistanceToAetheryte(nearestAetheryte.Value, shopCoords);
                        var youToShop = Vector3.Distance(PlayerEx.Coordinates, shopCoords);

                        Svc.Log.Info($"AetheryteToShop: {aetheryteToShop}");
                        Svc.Log.Info($"YouToShop: {youToShop}");

                        if (Svc.ClientState.TerritoryType != shopTerritory.RowId)
                        {
                            var primaryAetheryte = Coords.GetPrimaryAetheryte(nearestAetheryte.Value);
                            P.TaskManager.Enqueue(() => P.Lifestream.Teleport(primaryAetheryte.RowId, 0), $"Teleporting to {primaryAetheryte.PlaceName.Value.Name.GetText()}");
                            P.TaskManager.Enqueue(() => Svc.Condition[ConditionFlag.BetweenAreas]);
                            P.TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.BetweenAreas]);
                            P.TaskManager.Enqueue(() => !Player.IsBusy);
                        }
                        else if (!nearestAetheryte.Value.IsAetheryte && (youToShop > (aetheryteToShop + _teleportTimePenalty)))
                        {
                            var aethernetName = nearestAetheryte.Value.AethernetName.Value.Name.GetText();
                            P.TaskManager.Enqueue(() => P.Lifestream.AethernetTeleport(aethernetName), $"Using aethernet for {aethernetName}");
                            P.TaskManager.Enqueue(() => Svc.Condition[ConditionFlag.BetweenAreas]);
                            P.TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.BetweenAreas]);
                            P.TaskManager.Enqueue(() => !Player.IsBusy);
                        }
                        else if (youToShop > _interactDistance)
                        {
                            Svc.Log.Info("Vnaving to shop");
                            Svc.Log.Info($"Nearest is aetheryte: {nearestAetheryte.Value.IsAetheryte}");
                            Svc.Log.Info($"Test: {youToShop > aetheryteToShop + _teleportTimePenalty}");
                            if (!P.Navmesh.PathfindInProgress() && !P.Navmesh.IsRunning())
                                P.Navmesh.PathfindAndMoveTo(shopCoords, PlayerEx.AllowedToFly);
                            if (PlayerEx.AllowedToFly && !Svc.Condition[ConditionFlag.Mounted])
                                ExecuteMount();
                        }
                        else if (Svc.Targets.Target?.DataId != P.Config.BicolorShopId)
                            Svc.Targets.Target = Svc.Objects.FirstOrDefault(o => o.DataId == P.Config.BicolorShopId);
                        else if (P.Navmesh.PathfindInProgress() || P.Navmesh.IsRunning())
                            P.Navmesh.Stop();
                        else
                        {
                            var fateShop = Svc.Data.GetExcelSheet<FateShop>().FirstOrDefault(f => f.RowId == P.Config.BicolorShopId);
                            ShopInteraction.BuyFromShop(Svc.Targets.Target.GameObjectId, (uint)P.Config.BicolorShopId, P.Config.BicolorPurchaseItem, 1);
                        }
                    }
                }
                return;
        };
    }

    private unsafe void MoveToNextFate(ushort nextFateID)
    {
        if (P.Navmesh.IsReady() &&
            !Svc.Condition[ConditionFlag.InCombat] && !PlayerEx.Occupied)
        {
            var targetPos = GetRandomPointInFate(nextFateID);
            var directTravelDistance = Vector3.Distance(Player.Position, targetPos);
            var closestAetheryte = Coords.GetNearestAetheryte(Svc.ClientState.TerritoryType, targetPos);

            if (closestAetheryte != null)
                if (Svc.Condition[ConditionFlag.Mounted] && !Svc.Condition[ConditionFlag.InFlight] && PlayerEx.InFlightAllowedTerritory)
                {
                    var aetheryteTravelDistance = Coords.GetDistanceToAetheryte(closestAetheryte.Value, targetPos) + _teleportTimePenalty;
                    if (aetheryteTravelDistance < directTravelDistance) // if the closest aetheryte is a shortcut, then teleport
                        ExecuteTeleport(closestAetheryte.Value.RowId);
                    else // if the closest aetheryte is too far away, just fly directly to the fate
                    {
                        if (P.Navmesh.IsReady() && !P.Navmesh.IsRunning() && !P.Navmesh.PathfindInProgress())
                            P.Navmesh.PathfindAndMoveTo(targetPos, true);
                    }
                }
                else
                {
                    // if there is no closest aetheryte (i.e. dravanian hinterlands with no aetherytes on the map)
                    // then fly directly to the fate
                    if (P.Navmesh.IsReady() && !P.Navmesh.IsRunning() && !P.Navmesh.PathfindInProgress())
                        P.Navmesh.PathfindAndMoveTo(targetPos, true);
                }
        }
    }

    private void TargetAndMoveToEnemy(DGameObject target)
    {
        if (Svc.Condition[ConditionFlag.Mounted])
            ExecuteDismount();

        TargetPos = target.Position;
        if (Svc.Targets.Target?.GameObjectId != target.GameObjectId)
            Svc.Targets.Target = target;
        if (!P.Navmesh.PathfindInProgress() && !IsInMeleeRange(target.HitboxRadius + (P.Config.StayInMeleeRange ? 0 : 15)))
            P.Navmesh.PathfindAndMoveTo(TargetPos, false);
    }

    private unsafe void ExecuteTeleport(uint closestAetheryteDataId)
    {
        P.TaskManager.Enqueue(() => Telepo.Instance()->Teleport(closestAetheryteDataId, 0));
        P.TaskManager.Enqueue(() => Player.Object.IsCasting);
        P.TaskManager.Enqueue(() => Svc.Condition[ConditionFlag.BetweenAreas]);
        P.TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.BetweenAreas]);
    }

    private unsafe bool ChangeInstances()
    {
        var numberOfInstances = P.Lifestream.GetNumberOfInstances();
        if (_successiveInstanceChanges >= numberOfInstances - 1)
        {
            P.TaskManager.Enqueue(() => EzThrottler.Throttle("SuccessiveInstanceChanges", 10000));
            P.TaskManager.Enqueue(() => EzThrottler.Check("SuccessiveInstanceChanges"));
            Svc.Log.Info("Cycled through all instances. Waiting 10s.");
            _successiveInstanceChanges = 0;
            return false;
        }

        if (P.Navmesh.PathfindInProgress() || P.Navmesh.IsRunning())
            return false;

        var closestAetheryte = Coords.GetNearestAetheryte(Player.Territory, Player.Position);
        var closestAetheryteGameObject = Svc.Objects
            .Where(x => x is { ObjectKind: Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Aetheryte })
            .FirstOrDefault(x => x.DataId == closestAetheryte.Value.RowId);
        if (Coords.GetDistanceToAetheryte(closestAetheryte.Value, Player.Position) >= _distanceToTargetAetheryte)
        {
            Svc.Log.Debug("Teleporting to nearby aetheryte: " + closestAetheryte);
            ExecuteTeleport(closestAetheryte.Value.RowId);
            return false;
        }

        if (Svc.Targets.Target?.ObjectKind != Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Aetheryte)
        {
            Svc.Targets.Target = closestAetheryteGameObject;
            return false;
        }

        // If too far away to target or "target is too far below you" error
        if (DistanceToTarget() > 10 || Player.Position.Y - Svc.Targets.Target.Position.Y > 2)
        {
            // interact distance is between 8 and 10. less than 8 and you will run into the base of the aetheryte
            var closerToAetheryte = Svc.Targets.Target.Position - Vector3.Normalize(Svc.Targets.Target.Position - Player.Position) * 8;
            closerToAetheryte.Y = Math.Min(closerToAetheryte.Y, Svc.Targets.Target.Position.Y + 1);
            P.Navmesh.PathfindAndMoveTo(closerToAetheryte, false);
            return false;
        }

        if (!P.Lifestream.CanChangeInstance())
        {
            Svc.Log.Debug("Cannot change instances at this time.");
            return false;
        }

        Svc.Log.Debug("Lifestream not busy.");
        Svc.Log.Debug("Changing instances.");

        var nextInstance = (P.Lifestream.GetCurrentInstance() + 1) % numberOfInstances + 1; // instances are 1-indexed

        P.TaskManager.Enqueue(() => P.Lifestream.ChangeInstance(nextInstance)); // flying mount roulette
        P.TaskManager.Enqueue(() => Svc.Condition[ConditionFlag.BetweenAreas] || Svc.Condition[ConditionFlag.BetweenAreas51]);
        P.TaskManager.Enqueue(() => !(Svc.Condition[ConditionFlag.BetweenAreas] || Svc.Condition[ConditionFlag.BetweenAreas51]));

        _successiveInstanceChanges += 1;

        return true;
    }

    private unsafe void ExecuteActionSafe(ActionType type, uint id) => action.Exec(() => ActionManager.Instance()->UseAction(type, id));
    private void ExecuteMount()
    {
        P.TaskManager.Enqueue(() => ExecuteActionSafe(ActionType.GeneralAction, 24)); // flying mount roulette
        P.TaskManager.Enqueue(() => Player.Object.IsCasting);
        P.TaskManager.Enqueue(() => Svc.Condition[ConditionFlag.Mounting] || Svc.Condition[ConditionFlag.Mounting71] || Svc.Condition[ConditionFlag.Unknown57]);
        P.TaskManager.Enqueue(() => Svc.Condition[ConditionFlag.Mounted] || Svc.Condition[ConditionFlag.Mounted2]);
    }
    private void ExecuteDismount() => ExecuteActionSafe(ActionType.GeneralAction, 23);

    //private void ExecuteJump()
    //{
    //    P.TaskManager.Enqueue(() => ExecuteActionSafe(ActionType.GeneralAction, 2));
    //    P.TaskManager.Enqueue(() => Svc.Condition[ConditionFlag.Jumping] || Svc.Condition[ConditionFlag.Jumping61]);
    //    P.TaskManager.Enqueue(() => Svc.Condition[ConditionFlag.InFlight]);
    //}

    private IOrderedEnumerable<IFate> GetFates() => Svc.Fates.Where(FateConditions)
        .OrderByDescending(x =>
        P.Config.PrioritizeBonusFates
        && x.HasBonus
        && (
        !P.Config.BonusWhenTwist
        || Player.Status.FirstOrDefault(x => TwistOfFateStatusIDs.Contains(x.StatusId)) != null)
        )
        .ThenByDescending(x => P.Config.PrioritizeStartedFates && x.Progress > 0)
        .ThenBy(f => Vector3.Distance(PlayerEx.Position, f.Position));
    public bool FateConditions(IFate f) => f.GameData.Value.Rule == 1 && f.State != Dalamud.Game.ClientState.Fates.FateState.Preparation && f.Duration <= P.Config.MaxDuration && f.Progress <= P.Config.MaxProgress && f.TimeRemaining > P.Config.MinTimeRemaining && !P.Config.blacklist.Contains(f.FateId);

    private unsafe DGameObject? GetMobTargetingPlayer()
        => Svc.Objects
        .FirstOrDefault(x => x is ICharacter { MaxHp: > 0 }
        && !x.IsDead
        && x.IsTargetable
        && x.IsHostile()
        && x.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc
        && x.SubKind == (byte)BattleNpcSubKind.Enemy
        && x.IsTargetingPlayer());

    private unsafe DGameObject? GetFateMob()
        => Svc.Objects
        .Where(x => x is ICharacter { MaxHp: > 0 }
        && !x.IsDead
        && x.IsTargetable
        && x.IsHostile()
        && x.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc
        && x.SubKind == (byte)BattleNpcSubKind.Enemy
        && x.Struct() != null && x.Struct()->FateId == FateID
        && Math.Sqrt(Math.Pow(x.Position.X - CurrentFate->Location.X, 2) + Math.Pow(x.Position.Z - CurrentFate->Location.Z, 2)) < CurrentFate->Radius)
        // Prioritize Forlorns if configured
        .OrderByDescending(x => P.Config.PrioritizeForlorns && ForlornIDs.Contains(x.DataId))
        // Prioritize enemies targeting us
        .ThenByDescending(x => x.IsTargetingPlayer())
        // Deprioritize mobs in combat with other players (hopefully avoid botlike pingpong behavior in trash fates)
        .ThenBy(x => x.GetNameplateKind() == NameplateKind.HostileEngagedOther && !x.IsTargetingPlayer())
        // Prioritize closest enemy        
        .ThenBy(x => Math.Floor(Vector3.Distance(PlayerEx.Position, x.Position)))
        // Prioritize lowest HP enemy
        .FirstOrDefault();

    private unsafe FateContext* CurrentFate => FateManager.Instance()->CurrentFate;

    private unsafe float DistanceToFate() => Vector3.Distance(CurrentFate->Location, Svc.ClientState.LocalPlayer!.Position);
    private unsafe float DistanceToTarget() => Svc.Targets.Target is not null ? Vector3.Distance(Svc.Targets.Target.Position, Svc.ClientState.LocalPlayer!.Position) : 0;

    //Will be negative if inside hitbox
    private unsafe float DistanceToHitboxEdge(float hitboxRadius) => DistanceToTarget() - hitboxRadius;
    private unsafe bool IsInMeleeRange(float hitboxRadius)
        => DistanceToHitboxEdge(hitboxRadius) < 2;
    public unsafe Vector3 GetRandomPointInFate(ushort fateID)
    {
        var fate = FateManager.Instance()->GetFateById(fateID);
        var angle = random.NextDouble() * 2 * Math.PI;
        var randomPoint = new Vector3((float)(fate->Location.X + fate->Radius / 2 * Math.Cos(angle)), fate->Location.Y, (float)(fate->Location.Z + fate->Radius / 2 * Math.Sin(angle)));
        var point = P.Navmesh.NearestPoint(randomPoint, 5, 5);
        return (Vector3)(point == null ? fate->Location : point);
    }

    private unsafe void SyncFate(ushort value)
    {
        if (value != 0 && PlayerState.Instance()->IsLevelSynced == 0)
        {
            if (Player.Level > CurrentFate->MaxLevel)
                ECommons.Automation.Chat.Instance.SendMessage("/lsync");
        }
    }

    private void InteractWithObject(ulong gameObjectId)
    {
        if (Svc.Targets.Target?.GameObjectId != gameObjectId)
            Svc.Targets.Target = Svc.Objects.FirstOrDefault(o => o.GameObjectId == gameObjectId);
        else
            P.TaskManager.Enqueue(() => NeoTasks.InteractWithObject(() => Svc.Targets.Target!, false));
    }
}
