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
using Dalamud.Game.Command;

namespace DateWithDestiny;

public class Plugin : IDalamudPlugin
{
    public static string Name => "DateWithDestiny";
    public static string VersionString => $"v{P.GetType().Assembly.GetName().Version?.Major}.{P.GetType().Assembly.GetName().Version?.Minor}";
    private const string CommandName = "/dwd";

    internal static Plugin P = null!;
    public Config Config = null!;
    public Version Version { get; private set; } = null!;

    internal DateWithDestiny DateWithDestiny;

    internal TaskManager TaskManager;
    //internal AddonObserver AddonObserver;
    public readonly WindowSystem WindowSystem = new("DateWithDestiny");
    private MainWindow MainWindow { get; init; }

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

        //AddonObserver = new();
        TaskManager = new();
        Navmesh = new();
        AutoRetainerAPI = new();
        Lifestream = new();
        Deliveroo = new();
        AutoRetainer = new();

        DateWithDestiny = new DateWithDestiny();

        Svc.Commands.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Toggle open DateWithDestiny window."
        });
        MainWindow = new MainWindow(DateWithDestiny);
        WindowSystem.AddWindow(MainWindow);

        pluginInterface.UiBuilder.Draw += DrawUI;
        pluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;
    }

    private void EventWatcher(IFramework framework)
    {
    }

    public void Dispose()
    {
        Svc.Framework.Update -= EventWatcher;
        //AddonObserver.Dispose();
        ECommonsMain.Dispose();
    }

    private void OnCommand(string command, string args) => EzConfigGui.GetWindow<MainWindow>()!.IsOpen ^= true;

    private void DrawUI() => WindowSystem.Draw();
    public void ToggleMainUI() => MainWindow.Toggle();
}
