using ImGuiNET;
using Lumina.Excel.Sheets;

namespace DateWithDestiny.Utilities;

public struct HaselColor
{
    public float R { get; set; }
    public float G { get; set; }
    public float B { get; set; }
    public float A { get; set; }

    public HaselColor()
    {
    }

    public HaselColor(float r, float g, float b, float a = 1)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    public HaselColor(Vector4 vec) : this(vec.X, vec.Y, vec.Z, vec.W)
    {
    }

    public HaselColor(uint col) : this(ImGui.ColorConvertU32ToFloat4(col))
    {
    }

    public readonly HaselColor WithRed(float r)
        => new(r, G, B, A);

    public readonly HaselColor WithGreen(float g)
        => new(R, g, B, A);

    public readonly HaselColor WithBlue(float b)
        => new(R, G, b, A);

    public readonly HaselColor WithAlpha(float a)
        => new(R, G, B, a);

    public static HaselColor From(float r, float g, float b, float a = 1)
        => new() { R = r, G = g, B = b, A = a };

    public static HaselColor From(Vector4 vec)
        => From(vec.X, vec.Y, vec.Z, vec.W);

    public static HaselColor From(uint col)
        => From(ImGui.ColorConvertU32ToFloat4(col));

    public static HaselColor From(ImGuiCol col)
        => From(ImGui.GetColorU32(col));

    public static HaselColor FromABGR(uint abgr)
        => From(abgr.Reverse());

    public static HaselColor FromUiForeground(uint id)
        => FromABGR(GetRow<UIColor>(id)!.Value.UIForeground);

    public static HaselColor FromUiGlow(uint id)
        => FromABGR(GetRow<UIColor>(id)!.Value.UIGlow);

    public static HaselColor FromStain(uint id)
        => From(GetRow<Stain>(id)!.Value.Color.Reverse() >> 8).WithAlpha(1);

    public static implicit operator Vector4(HaselColor col)
        => new(col.R, col.G, col.B, col.A);

    public static implicit operator uint(HaselColor col)
        => ImGui.ColorConvertFloat4ToU32(col);
}
