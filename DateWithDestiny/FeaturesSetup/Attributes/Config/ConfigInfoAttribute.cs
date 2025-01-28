using Dalamud.Interface;

namespace DateWithDestiny.FeaturesSetup.Attributes;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public class ConfigInfoAttribute(string label, string desc) : Attribute
{
    public string Label { get; init; } = label;
    public string Description { get; init; } = desc;
    public FontAwesomeIcon Icon { get; init; } = FontAwesomeIcon.InfoCircle;
    public HaselColor Color { get; init; } = Colors.Grey;
}
