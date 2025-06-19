using DateWithDestiny.IPC;
using DateWithDestiny.Utilities;
using ECommons.Automation.NeoTaskManager.Tasks;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DateWithDestiny;
public static unsafe class ShopInteraction
{

    public static bool IsShopOpen(uint shopId = 0)
    {
        var agent = AgentShop.Instance();
        if (agent == null || !agent->IsAgentActive() || agent->EventReceiver == null || !agent->IsAddonReady())
            return false;
        if (shopId == 0)
            return true; // some shop is open...
        if (!EventFramework.Instance()->EventHandlerModule.EventHandlerMap.TryGetValuePointer(shopId, out var eh) || eh == null || eh->Value == null)
            return false;
        var proxy = (ShopEventHandler.AgentProxy*)agent->EventReceiver;
        return proxy->Handler == eh->Value;
    }

    public static bool OpenShop(ulong vendorInstanceId, uint shopId)
    {
        var vendor = GameObjectManager.Instance()->Objects.GetObjectByGameObjectId(vendorInstanceId);
        if (vendor == null)
        {
            Svc.Log.Error($"Failed to find vendor {vendorInstanceId:X}");
            return false;
        }
        return OpenShop(vendor, shopId);
    }

    public static bool OpenShop(GameObject* vendor, uint shopId)
    {
        Svc.Log.Debug($"Interacting with {(ulong)vendor->GetGameObjectId():X}");
        TargetSystem.Instance()->InteractWithObject(vendor);
        var selector = EventHandlerSelector.Instance();
        if (selector->Target == null)
            return true; // assume interaction was successful without selector

        if (selector->Target != vendor)
        {
            Svc.Log.Error($"Unexpected selector target {(ulong)selector->Target->GetGameObjectId():X} when trying to interact with {(ulong)vendor->GetGameObjectId():X}");
            return false;
        }

        for (int i = 0; i < selector->OptionsCount; ++i)
        {
            if (selector->Options[i].Handler->Info.EventId.Id == shopId)
            {
                Svc.Log.Debug($"Selecting selector option {i} for shop {shopId:X}");
                EventFramework.Instance()->InteractWithHandlerFromSelector(i);
                return true;
            }
        }

        Svc.Log.Error($"Failed to find shop {shopId:X} in selector for {(ulong)vendor->GetGameObjectId():X}");
        return false;
    }

    public static bool ShopTransactionInProgress(uint shopId)
    {
        if (!EventFramework.Instance()->EventHandlerModule.EventHandlerMap.TryGetValuePointer(shopId, out var eh) || eh == null || eh->Value == null)
        {
            Svc.Log.Error($"Event handler for shop {shopId:X} not found");
            return false;
        }

        if (eh->Value->Info.EventId.ContentId != EventHandlerContent.Shop)
        {
            Svc.Log.Error($"{shopId:X} is not a shop");
            return false;
        }

        var shop = (ShopEventHandler*)eh->Value;
        return shop->WaitingForTransactionToFinish;
    }

    public static void BuyFromShop(ulong vendorInstanceId, uint shopId, uint itemId, int count)
    {
        if (!IsShopOpen(shopId))
        {
            Svc.Log.Info("Opening shop...");
            P.TaskManager.Enqueue(() => OpenShop(vendorInstanceId, shopId));
            //ErrorIf(!OpenShop(vendorInstanceId, shopId), $"Failed to open shop {vendorInstanceId:X}.{shopId:X}");
            P.TaskManager.Enqueue(() => !IsShopOpen(shopId), "WaitForOpen");
            P.TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.OccupiedInEvent], "WaitForCondition");
        }

        Svc.Log.Info("Buying...");
        //ErrorIf(!BuyItemFromShop(shopId, itemId, count), $"Failed to buy {count}x {itemId} from shop {vendorInstanceId:X}.{shopId:X}");
        P.TaskManager.Enqueue(() => ShopTransactionInProgress(shopId), "Transaction");
        Svc.Log.Info("Closing shop...");
        //ErrorIf(!CloseShop(), $"Failed to close shop {vendorInstanceId:X}.{shopId:X}");
        P.TaskManager.Enqueue(() => IsShopOpen(), "WaitForClose");
        P.TaskManager.Enqueue(() => Svc.Condition[ConditionFlag.OccupiedInEvent], "WaitForCondition");
    }
}
