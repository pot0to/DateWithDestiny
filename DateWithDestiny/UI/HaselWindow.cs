using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ECommons.ImGuiMethods;
using ECommons.SimpleGui;
using ImGuiNET;
using System.Drawing;
using System.Reflection;
using System.Text.RegularExpressions;

namespace DateWithDestiny.UI;

public partial class HaselWindow : Window
{
    // Style from HaselTweaks
    // https://github.com/Haselnussbomber/HaselTweaks
    public HaselWindow() : base($"{Name} {VersionString}###{nameof(HaselWindow)}")
    {
        var width = SidebarWidth * 3 + ImGui.GetStyle().ItemSpacing.X + ImGui.GetStyle().FramePadding.X * 2;
        Size = new(width, 600);
        SizeConstraints = new() { MinimumSize = new(width, 600), MaximumSize = new(4096, 2160) };
        SizeCondition = ImGuiCond.Always;
        Flags |= ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings;
        AllowClickthrough = false;
        AllowPinning = false;
    }

    private const uint SidebarWidth = 250;
    private const string LogoManifestResource = "Automaton.Assets.rat.png";

    private string _selectedTweak = string.Empty;
    private Point _logoSize = new(789, 983);
    private const float _logoScale = 0.3f;

    private Tweak? SelectedTweak => Tweaks.FirstOrDefault(t => t.Name == _selectedTweak);

    public static void SetWindowProperties()
    {
        var width = SidebarWidth * 3 + ImGui.GetStyle().ItemSpacing.X + ImGui.GetStyle().FramePadding.X * 2;

        EzConfigGui.Window.Size = new Vector2(width, 600);
        EzConfigGui.Window.SizeConstraints = new()
        {
            MinimumSize = new Vector2(width, 600),
            MaximumSize = new Vector2(4096, 2160)
        };

        EzConfigGui.Window.SizeCondition = ImGuiCond.Always;

        EzConfigGui.Window.Flags |= ImGuiWindowFlags.AlwaysAutoResize;
        EzConfigGui.Window.Flags |= ImGuiWindowFlags.NoSavedSettings;

        EzConfigGui.Window.AllowClickthrough = false;
        EzConfigGui.Window.AllowPinning = false;
    }

    public override void Draw()
    {
        DrawSidebar();
        ImGui.SameLine();
        DrawConfig();
    }

    private void DrawSidebar()
    {
        var scale = ImGuiHelpers.GlobalScale;
        using var child = ImRaii.Child("##Sidebar", new Vector2(SidebarWidth * scale, -1), true);
        if (!child.Success)
            return;

        using var table = ImRaii.Table("##SidebarTable", 2, ImGuiTableFlags.NoSavedSettings);
        if (!table.Success)
            return;

        ImGui.TableSetupColumn("Checkbox", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Tweak Name", ImGuiTableColumnFlags.WidthStretch);

        foreach (var tweak in Tweaks.Where(t => !t.Disabled && (!t.IsDebug || C.ShowDebug)).OrderBy(t => t.Name))
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            var enabled = tweak.Enabled;
            var fixY = false;

            if (!tweak.Ready || tweak.Outdated)
            {
                var startPos = ImGui.GetCursorPos();
                var drawList = ImGui.GetWindowDrawList();
                var pos = ImGui.GetWindowPos() + startPos - new Vector2(0, ImGui.GetScrollY());
                var frameHeight = ImGui.GetFrameHeight();

                var size = new Vector2(frameHeight);
                ImGui.SetCursorPos(startPos);
                ImGui.Dummy(size);

                if (ImGui.IsItemHovered())
                {
                    var (status, color) = GetTweakStatus(tweak);
                    using var tooltip = ImRaii.Tooltip();
                    if (tooltip.Success)
                    {
                        ImGuiEx.Text((uint)color, status);
                    }
                }

                drawList.AddRectFilled(pos, pos + size, ImGui.GetColorU32(ImGuiCol.FrameBg), 3f, ImDrawFlags.RoundCornersAll);

                var pad = frameHeight / 4f;
                pos += new Vector2(pad);
                size -= new Vector2(pad) * 2;

                drawList.PathLineTo(pos);
                drawList.PathLineTo(pos + size);
                drawList.PathStroke(Colors.Red, ImDrawFlags.None, frameHeight / 5f * 0.5f);

                drawList.PathLineTo(pos + new Vector2(0, size.Y));
                drawList.PathLineTo(pos + new Vector2(size.X, 0));
                drawList.PathStroke(Colors.Red, ImDrawFlags.None, frameHeight / 5f * 0.5f);

                fixY = true;
            }
            else
            {
                ImGuiEx.CollectionCheckbox($"##Enabled_{tweak.InternalName}", tweak.InternalName, C.EnabledTweaks);
            }

            ImGui.TableNextColumn();

            if (fixY)
                ImGuiX.PushCursorY(3); // if i only knew why this happens

            using var colour = ImRaii.PushColor(ImGuiCol.Text, !tweak.Ready || tweak.Outdated ? (uint)Colors.Red : !enabled ? (uint)Colors.Grey : ImGui.GetColorU32(ImGuiCol.Text), !tweak.Ready || tweak.Outdated || !enabled);

            if (ImGui.Selectable($"{tweak.Name}##Selectable_{tweak.Name}", _selectedTweak == tweak.Name))
            {
                _selectedTweak = _selectedTweak != tweak.Name ? tweak.Name : string.Empty;
            }
        }
    }

    private void DrawConfig()
    {
        using var child = ImRaii.Child("##Config", new Vector2(-1), true);
        if (!child.Success)
            return;

        var tweak = SelectedTweak;
        if (tweak == null)
        {
            var cursorPos = ImGui.GetCursorPos();
            var contentAvail = ImGui.GetContentRegionAvail();

            if (Svc.Texture.GetFromManifestResource(Assembly.GetExecutingAssembly(), LogoManifestResource).TryGetWrap(out var logo, out var _))
            {
                var maxWidth = SidebarWidth * 2 * 0.85f * ImGuiHelpers.GlobalScale;
                var ratio = maxWidth / _logoSize.X;
                var scaledLogoSize = _logoSize.ToVec2() * _logoScale;

                ImGui.SetCursorPos(contentAvail / 2 - scaledLogoSize / 2 + new Vector2(ImGui.GetStyle().ItemSpacing.X, 0));
                ImGui.Image(logo.ImGuiHandle, scaledLogoSize);
            }

            //var welcomeStr = "still working on updating a few things, hope you're enjoying the new toys";
            //ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetColumnWidth() * 0.5f - ImGui.CalcTextSize(welcomeStr).X * 0.5f);
            //ImGuiX.FlashText(welcomeStr, Colors.Gold, ImGui.GetStyle().Colors[(int)ImGuiCol.WindowBg], 2);

            // links, bottom left
            ImGui.SetCursorPos(cursorPos + new Vector2(0, contentAvail.Y - ImGui.GetTextLineHeight()));
            ImGuiX.DrawLink("GitHub", "GitHub", "https://github.com/Jaksuhn/Automaton");
            ImGui.SameLine();
            ImGui.TextUnformatted("•");
            ImGui.SameLine();
            ImGuiX.DrawLink("Ko-fi", "Ko-fi", "https://ko-fi.com/croizat");

            // version, bottom right
            if (VersionString.Length > 1)
            {
                ImGui.SetCursorPos(cursorPos + contentAvail - ImGui.CalcTextSize(VersionString));
                ImGui.TextUnformatted(VersionString);
            }

            return;
        }

        using var id = ImRaii.PushId(tweak.Name);

        ImGuiEx.Text((uint)Colors.Gold, tweak.Name);

        var (status, color) = GetTweakStatus(tweak);

        var posX = ImGui.GetCursorPosX();
        var windowX = ImGui.GetContentRegionAvail().X;
        var textSize = ImGui.CalcTextSize(status);

        ImGui.SameLine(windowX - textSize.X);

        ImGuiEx.Text((uint)color, status);

        if (!string.IsNullOrEmpty(tweak.Description))
        {
            ImGuiX.DrawPaddedSeparator();
            ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey2, tweak.Description);
        }

        if (tweak.Requirements.Any(entry => !entry.IsLoaded))
        {
            ImGuiX.DrawSection("Required Dependencies");
            ImGuiX.Icon(60074, 24);
            ImGui.SameLine();
            ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey2, $"Missing {tweak.Requirements.Count(entry => !entry.IsLoaded)} of the required plugins for this feature to work:");
            foreach (var entry in tweak.Requirements.Where(entry => !entry.IsLoaded))
            {
                ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey2, $"{entry.InternalName}:");
                ImGui.SameLine();
                ImGuiEx.TextCopy(entry.Repo);
            }
        }

        if (tweak.IncompatibilityWarnings.Any(entry => entry.IsLoaded))
        {
            ImGuiX.DrawSection("Incompatibility Warning");
            ImGuiX.Icon(60073, 24);
            ImGui.SameLine();
            var cursorPosX = ImGui.GetCursorPosX();

            static string getConfigName(string tweakName, string configName) => $"{tweakName}: {configName}";

            if (tweak.IncompatibilityWarnings.Length == 1)
            {
                var entry = tweak.IncompatibilityWarnings[0];
                var pluginName = $"{entry.InternalName}";

                if (entry.IsLoaded)
                {
                    if (entry.ConfigNames.Length == 0)
                    {
                        ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey2, $"In order for this tweak to work properly, please make sure {pluginName} is disabled.");
                    }
                    else if (entry.ConfigNames.Length == 1)
                    {
                        var configName = getConfigName(entry.InternalName, entry.ConfigNames[0]);
                        ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey2, $"In order for this tweak to work properly, please make sure {configName} is disabled in {pluginName}.");
                    }
                    else if (entry.ConfigNames.Length > 1)
                    {
                        var configNames = entry.ConfigNames.Select((configName) => $"{configName}");
                        ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey2, $"In order for this tweak to work properly, please make sure {pluginName} is disabled." + $"\n - {string.Join("\n- ", configNames)}");
                    }
                }
            }
            else if (tweak.IncompatibilityWarnings.Length > 1)
            {
                ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey2, "In order for this tweak to work properly, please make sure");

                foreach (var entry in tweak.IncompatibilityWarnings.Where(entry => entry.IsLoaded))
                {
                    var pluginName = $"{entry.InternalName}";

                    if (entry.ConfigNames.Length == 0)
                    {
                        ImGui.SetCursorPosX(cursorPosX);
                        ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey2, $"{pluginName} is disabled");
                    }
                    else if (entry.ConfigNames.Length == 1)
                    {
                        ImGui.SetCursorPosX(cursorPosX);
                        var configName = $"HaselTweaks.Config.IncompatibilityWarning.Plugin.{entry.InternalName}.Config.{entry.ConfigNames[0]}";
                        ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey2, $"{configName} is disabled in {pluginName}");
                    }
                    else if (entry.ConfigNames.Length > 1)
                    {
                        ImGui.SetCursorPosX(cursorPosX);
                        var configNames = entry.ConfigNames.Select((configName) => $"{configName}");
                        ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey2, ("{pluginName} is disabled", pluginName) + $"\n    - {string.Join("\n    - ", configNames)}");
                    }
                }
            }
        }

        tweak.DrawConfig();
    }

    private static (string, HaselColor) GetTweakStatus(Tweak tweak)
    {
        var status = "???";
        var color = Colors.Grey3;

        if (tweak.Outdated)
        {
            status = "Outdated";
            color = Colors.Red;
        }
        else if (!tweak.Ready)
        {
            status = "Initialization Failed";
            color = Colors.Red;
        }
        else if (tweak.Enabled)
        {
            status = "Enabled";
            color = Colors.Green;
        }
        else if (!tweak.Enabled)
        {
            status = "Disabled";
        }

        return (status, color);
    }
}
