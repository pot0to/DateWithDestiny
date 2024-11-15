namespace Automaton.Features;

[Tweak(HasHooks = true)]
internal class DisableObjectCulling : Tweak
{
    public override string Name => "Disable Object Culling";
    public override string Description => "Prevents the game from hiding objects when your camera collides with them.";

    public override void Enable() => P.Memory.ShouldDrawHook.Enable();
    public override void Disable() => P.Memory.ShouldDrawHook.Disable();
}
