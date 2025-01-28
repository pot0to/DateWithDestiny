using ImGuiNET;
using System.Reflection;

namespace DateWithDestiny.FeaturesSetup.Attributes;

[AttributeUsage(AttributeTargets.Field)]
public class StringConfigAttribute : BaseConfigAttribute
{
    public string DefaultValue = string.Empty;

    public override void Draw(Tweak tweak, object config, FieldInfo fieldInfo)
    {
        var value = (string)fieldInfo.GetValue(config)!;
        var attr = fieldInfo.GetCustomAttribute<BaseConfigAttribute>();

        ImGui.TextUnformatted(fieldInfo.Name.SplitWords());

        if (ImGui.InputText("##Input", ref value, 50))
        {
            fieldInfo.SetValue(config, value);
            OnChangeInternal(tweak, fieldInfo);
        }

        if (DrawResetButton(DefaultValue))
        {
            fieldInfo.SetValue(config, DefaultValue);
            OnChangeInternal(tweak, fieldInfo);
        }

        if (!attr?.Description.IsNullOrEmpty() ?? false)
            ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey, attr!.Description);
    }
}
