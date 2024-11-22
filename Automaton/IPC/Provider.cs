using ECommons.EzIpcManager;

namespace Automaton.IPC;
internal class Provider
{
    public Provider() => EzIPC.Init(this);

    [EzIPC]
    public bool IsTweakEnabled(string assemblyName) => C.EnabledTweaks.Contains(assemblyName);

    [EzIPC]
    public void SetTweakState(string assemblyName, bool state)
    {
        if (state)
            C.EnabledTweaks.Add(assemblyName);
        else
            C.EnabledTweaks.Remove(assemblyName);
    }
}
