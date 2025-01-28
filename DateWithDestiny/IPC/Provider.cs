﻿using ECommons.EzIpcManager;

namespace DateWithDestiny.IPC;
internal class Provider
{
    public Provider() => EzIPC.Init(this);

    [EzIPC]
    public bool IsTweakEnabled(string className) => C.EnabledTweaks.Contains(className);

    [EzIPC]
    public void SetTweakState(string className, bool state)
    {
        if (state)
            C.EnabledTweaks.Add(className);
        else
            C.EnabledTweaks.Remove(className);
    }
}
