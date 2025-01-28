using ECommons.EzIpcManager;

namespace DateWithDestiny.IPC;

#nullable disable
#pragma warning disable CS0649
internal class PandorasBoxIPC
{
    public static string Name = "PandorasBox";
    public PandorasBoxIPC() => EzIPC.Init(this, Name);
    public static bool IsEnabled => Utils.HasPlugin(Name);

    [EzIPC] public readonly Func<string, bool?> GetFeatureEnabled;
    [EzIPC] public readonly Func<string, string, bool?> GetConfigEnabled;

    [EzIPC] public readonly Action<string, bool, object> SetFeatureEnabled;
    [EzIPC] public readonly Action<string, string, bool, object> SetConfigEnabled;
    [EzIPC] public readonly Action<string, int, object> PauseFeature;
}
