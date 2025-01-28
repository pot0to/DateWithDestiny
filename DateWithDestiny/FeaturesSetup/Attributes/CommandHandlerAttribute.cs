namespace DateWithDestiny.FeaturesSetup.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class CommandHandlerAttribute(string[] commands, string helpMessage, string? configFieldName = null, bool hooks = false) : Attribute
{
    public string[] Commands { get; } = commands;
    public string HelpMessage { get; } = helpMessage;
    public string? ConfigFieldName { get; } = configFieldName;
    public bool Hooks { get; } = hooks;

    public CommandHandlerAttribute(string command, string helpMessage, string? configFieldName = null, bool hooks = false) : this([command], helpMessage, configFieldName, hooks) { }
}
