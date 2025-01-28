using Dalamud.Interface;

namespace DateWithDestiny.FeaturesSetup.Attributes;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public class NetworkWarningAttribute : ConfigInfoAttribute
{
    public NetworkWarningAttribute() : base("Network Warning", "This option sends network/server requests. Use at your own risk.")
    {
        Icon = FontAwesomeIcon.Bolt;
        Color = Colors.Yellow;
    }
}
