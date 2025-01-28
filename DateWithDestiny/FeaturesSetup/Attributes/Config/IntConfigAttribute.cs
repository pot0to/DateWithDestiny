using ImGuiNET;
using System.Globalization;
using System.Reflection;

namespace DateWithDestiny.FeaturesSetup.Attributes;

[AttributeUsage(AttributeTargets.Field)]
public class IntConfigAttribute : BaseConfigAttribute
{
    public int DefaultValue = 0;
    public int Min = 0;
    public int Max = 100;

    public override void Draw(Tweak tweak, object config, FieldInfo fieldInfo)
    {
        var value = (int)fieldInfo.GetValue(config)!;
        var attr = fieldInfo.GetCustomAttribute<BaseConfigAttribute>();

        ImGui.TextUnformatted(fieldInfo.Name.SplitWords());

        using var indent = ImGuiX.ConfigIndent();

        if (ImGui.DragInt("##Input", ref value, 0.01f, Min, Max))
        {
            fieldInfo.SetValue(config, value);
            OnChangeInternal(tweak, fieldInfo);
        }

        if (DrawResetButton(string.Format(CultureInfo.InvariantCulture, "{0:0.00}", DefaultValue)))
        {
            fieldInfo.SetValue(config, DefaultValue);
            OnChangeInternal(tweak, fieldInfo);
        }

        if (!attr?.Description.IsNullOrEmpty() ?? false)
            ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey, attr!.Description);
    }
}
