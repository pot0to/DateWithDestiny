using ImGuiNET;
using System.Globalization;
using System.Reflection;

namespace DateWithDestiny.FeaturesSetup.Attributes;

[AttributeUsage(AttributeTargets.Field)]
public class FloatConfigAttribute : BaseConfigAttribute
{
    public float DefaultValue = 0;
    public float Min = 0;
    public float Max = 100;

    public override void Draw(Tweak tweak, object config, FieldInfo fieldInfo)
    {
        var value = (float)fieldInfo.GetValue(config)!;
        var attr = fieldInfo.GetCustomAttribute<BaseConfigAttribute>();

        ImGui.TextUnformatted(fieldInfo.Name.SplitWords());

        using var indent = ImGuiX.ConfigIndent();

        if (ImGui.DragFloat("##Input", ref value, 0.01f, Min, Max, "%.2f"))
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
