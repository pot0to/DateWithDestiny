using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using System.Reflection;

namespace DateWithDestiny.FeaturesSetup.Attributes;

[AttributeUsage(AttributeTargets.Field)]
public class EnumConfigAttribute : BaseConfigAttribute
{
    public bool NoLabel = false;

    public override void Draw(Tweak tweak, object config, FieldInfo fieldInfo)
    {
        var enumType = fieldInfo.FieldType;
        var attr = fieldInfo.GetCustomAttribute<BaseConfigAttribute>();

        string GetOptionLabel(int value) => $"{Enum.GetName(enumType, value)}";

        if (!NoLabel)
        {
            ImGui.TextUnformatted(fieldInfo.Name.SplitWords());
        }

        using var indent = ImGuiX.ConfigIndent(!NoLabel);

        var selectedValue = Convert.ToInt32(fieldInfo.GetValue(config) ?? 0);
        using var combo = ImRaii.Combo("##Input", GetOptionLabel(selectedValue));
        if (combo.Success)
        {
            foreach (var name in Enum.GetNames(enumType))
            {
                var value = Convert.ToInt32(Enum.Parse(enumType, name));

                if (ImGui.Selectable(GetOptionLabel(value), selectedValue == value))
                {
                    fieldInfo.SetValue(config, Enum.ToObject(fieldInfo.FieldType, value));
                    OnChangeInternal(tweak, fieldInfo);
                }

                if (selectedValue == value)
                {
                    ImGui.SetItemDefaultFocus();
                }
            }
        }
        combo?.Dispose();
        if (!attr?.Description.IsNullOrEmpty() ?? false)
            ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey, attr!.Description);
    }
}
