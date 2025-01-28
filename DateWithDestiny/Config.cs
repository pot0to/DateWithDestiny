using ECommons.Configuration;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace DateWithDestiny.Configuration;

public class Config : IEzConfig
{
    [JsonIgnore]
    public const int CURRENT_CONFIG_VERSION = 3;

    public int Version = CURRENT_CONFIG_VERSION;
    public ObservableCollection<string> EnabledTweaks = [];
    public TweakConfigs Tweaks = new();
    public bool ShowDebug;
}

public class TweakConfigs
{
    public DateWithDestinyConfiguration DateWithDestiny { get; init; } = new();
}

//public class YamlFactory : ISerializationFactory
//{
//    public string DefaultConfigFileName => $"ezAutomaton.yaml";

//    public T Deserialize<T>(string inputData)
//    {
//        return new DeserializerBuilder()
//            .IgnoreUnmatchedProperties()
//            .Build().Deserialize<T>(inputData);
//    }

//    public string Serialize(object s, bool prettyPrint)
//    {
//        return new SerializerBuilder().Build().Serialize(s);
//    }
//}

public interface IMigration
{
    int Version { get; }
    void Migrate(ref Config config);
}
