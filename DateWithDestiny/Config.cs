using ECommons.Configuration;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace DateWithDestiny;

public class Config : IEzConfig
{
    public HashSet<uint> blacklist = [];
    public HashSet<uint> whitelist = [];
    public List<uint> zones = [];
    public bool YokaiMode;
    public bool StayInMeleeRange;
    public bool PrioritizeForlorns = true;
    public bool PrioritizeBonusFates = true;
    public bool PrioritizeStartedFates;
    public bool BonusWhenTwist = false;
    public bool EquipWatch = true;
    public bool SwapMinions = true;
    public bool SwapZones = true;
    public bool ChangeInstances = true;

    //public bool FullAuto = true;
    //public bool AutoMount = true;
    //public bool AutoFly = true;
    public bool PathToFate = true;
    public bool AutoSync = true;
    public bool AutoTarget = true;
    public bool AutoMoveToMobs = true;
    public int MaxDuration = 900;
    public int MinTimeRemaining = 120;
    public int MaxProgress = 90;

    public bool ShowFateTimeRemaining;
    public bool ShowFateBonusIndicator;

    public bool AbortTasksOnTimeout;

    public int BicolorThreshold = 1400;
    public ulong BicolorShopId = 0;
    public uint BicolorPurchaseItem = 0;
}

public class YamlFactory : ISerializationFactory
{
    public string DefaultConfigFileName => $"ezAutomaton.yaml";

    public bool IsBinary => false;

    public T Deserialize<T>(string inputData)
    {
        return new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .Build().Deserialize<T>(inputData);
    }

    public T? Deserialize<T>(byte[] inputData) => throw new NotImplementedException();

    //public T? Deserialize<T>(byte[] inputData) => throw new NotImplementedException();

    public string Serialize(object s, bool prettyPrint)
    {
        return new SerializerBuilder().Build().Serialize(s);
    }

    public string? Serialize(object config) => Serialize(config, false);
    public byte[]? SerializeAsBin(object config) => Encoding.UTF8.GetBytes(Serialize(config) ?? "");
}
