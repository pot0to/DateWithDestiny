using ECommons.EzIpcManager;

namespace DateWithDestiny.IPC;

#nullable disable
public class AutoRetainerIPC
{
    public const string Name = "AutoRetainer";
    public const string Repo = "https://love.puni.sh/ment.json";
    public AutoRetainerIPC() => EzIPC.Init(this, Name);
    public static bool Installed => Utils.HasPlugin(Name);

    [EzIPC("PluginState.%m")] public readonly Func<bool> IsBusy;
    [EzIPC("PluginState.%m")] public readonly Func<int> GetInventoryFreeSlotCount;
    [EzIPC] public readonly Func<bool> GetMultiModeEnabled;
    [EzIPC] public readonly Action<bool> SetMultiModeEnabled;
    [EzIPC] public readonly Func<bool> GetSuppressed;
    [EzIPC] public readonly Action<bool> SetSuppressed;
}
