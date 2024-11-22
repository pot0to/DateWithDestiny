namespace Automaton.Features;

[Tweak]
internal class AutoSnipeQuests : Tweak
{
    public override string Name => "Sniper no sniping";
    public override string Description => "Automatically completes snipe quests.";

    public override void Enable() => P.Memory.SnipeHook.Enable();
    public override void Disable() => P.Memory.SnipeHook.Disable();
}
