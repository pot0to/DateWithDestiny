using DateWithDestiny.IPC;
using AutoRetainerAPI;
using Dalamud.Plugin;
using ECommons;
using ECommons.Automation.LegacyTaskManager;
using ECommons.Configuration;
using ECommons.SimpleGui;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Reflection;
using Dalamud.Interface.Windowing;

namespace DateWithDestiny;

public class Plugin : IDalamudPlugin
{
    public static string Name => "DateWithDestiny";
    public static string VersionString => $"v{P.GetType().Assembly.GetName().Version?.Major}.{P.GetType().Assembly.GetName().Version?.Minor}";
    private const string Command = "/dwd";

    internal static Plugin P = null!;
    public static Config Config = null!;
    public Version Version { get; private set; } = null!;

    internal DateWithDestiny DateWithDestiny;

    internal TaskManager TaskManager;
    internal AddonObserver AddonObserver;
    public readonly WindowSystem WindowSystem = new("DateWithDestiny");

    //internal Provider Provider;
    internal NavmeshIPC Navmesh;
    internal AutoRetainerApi AutoRetainerAPI;
    internal LifestreamIPC Lifestream;
    internal DeliverooIPC Deliveroo;
    internal AutoRetainerIPC AutoRetainer;
    internal bool UsingARPostProcess;

    //internal Memory Memory = null!;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        P = this;
        Version = P.GetType().Assembly.GetName().Version ?? new(0, 0);
        ECommonsMain.Init(pluginInterface, P, ECommons.Module.DalamudReflector, ECommons.Module.ObjectFunctions);

        EzConfig.DefaultSerializationFactory = new YamlFactory();
        Config = EzConfig.Init<Config>();

        Svc.Framework.Update += EventWatcher;

        AddonObserver = new();
        TaskManager = new();
        Navmesh = new();
        AutoRetainerAPI = new();
        Lifestream = new();
        Deliveroo = new();
        AutoRetainer = new();

        DateWithDestiny = new DateWithDestiny();

        EzCmd.Add(Command, DateWithDestiny.OnCommand, $"Opens the {Name} menu");
        var gui = new FateTrackerUI(DateWithDestiny);
        EzConfigGui.Init(gui.Draw, nameOverride: $"{Name} v{P.Version.ToString(2)}");
        EzConfigGui.WindowSystem.AddWindow(gui);
    }

    private bool inpvp = false;
    private void EventWatcher(IFramework framework)
    {
        if (PlayerEx.InPvP)
        {
            if (!inpvp)
            {
                inpvp = true;
                Events.OnEnteredPvPInstance();
            }
        }
        else
            inpvp = false;
    }

    public void Dispose()
    {
        Svc.Framework.Update -= EventWatcher;
        AddonObserver.Dispose();
        ECommonsMain.Dispose();
    }

    private void OnCommand(string command, string args) => EzConfigGui.GetWindow<FateTrackerUI>()!.IsOpen ^= true;
}
