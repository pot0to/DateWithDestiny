using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;

namespace Automaton.UI;

internal class DebugWindow : Window
{
    public DebugWindow() : base($"{Name} - Debug {P.GetType().Assembly.GetName().Version}###{nameof(DebugWindow)}")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
    }

    public static void Dispose() { }

    public override bool DrawConditions() => Player.Available;

    private Utils.ExecuteCommandFlag flag;
    private Utils.ExecuteCommandComplexFlag flag2;
    private int ec1 = 0;
    private int ec2 = 0;
    private int ec3 = 0;
    private int ec4 = 0;
    private int ecc1 = 0;
    private int ecc2 = 0;
    private int ecc3 = 0;
    private int ecc4 = 0;
    public override unsafe void Draw()
    {
        ImGuiX.Enum("ExecuteCommand", ref flag);
        ImGui.InputInt("p1", ref ec1);
        ImGui.InputInt("p2", ref ec2);
        ImGui.InputInt("p3", ref ec3);
        ImGui.InputInt("p4", ref ec4);
        if (ImGui.Button("exeucte"))
            P.Memory.ExecuteCommand(flag, ec1, ec2, ec3, ec4);

        using var id = ImRaii.PushId("complex");
        ImGuiX.Enum("ExecuteCommandComplex", ref flag2);
        ImGui.InputInt("p1", ref ecc1);
        ImGui.InputInt("p2", ref ecc2);
        ImGui.InputInt("p3", ref ecc3);
        ImGui.InputInt("p4", ref ecc4);
        if (ImGui.Button("exeucte"))
            P.Memory.ExecuteCommandComplexLocation(flag2, Player.Position, ecc1, ecc2, ecc3, ecc4);
    }
}
