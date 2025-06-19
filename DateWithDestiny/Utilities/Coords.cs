using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;
using System.Security.Policy;

namespace DateWithDestiny.Utilities;

public static class Coords
{
    public static string GetAetheryteName(Aetheryte aetheryte) => aetheryte.PlaceName.Value.Name.GetText() + aetheryte.AethernetName.Value.Name.GetText();

    public static Vector2 GetAetherytePosition(Aetheryte aetheryte)
    {
        var mapMarker = FindRow<MapMarker>(m =>
                    m.DataType == (aetheryte.IsAetheryte ? 3 : 4) &&
                    m.DataKey.RowId == (aetheryte.IsAetheryte ? aetheryte.RowId : aetheryte.AethernetName.RowId));
        if (mapMarker == null)
        {
            Svc.Log.Error($"Cannot find map marker for aetheryte #{aetheryte.RowId}: {aetheryte.RowId}#{GetAetheryteName(aetheryte)}");
            return Vector2.Zero;
        }
        var map = Svc.Data.GetExcelSheet<Lumina.Excel.Sheets.Map>().FirstOrDefault(m => m.TerritoryType.RowId == aetheryte.Territory.Value.RowId);
        var scale = map.SizeFactor;

        var AethersX = ConvertMapMarkerToRawPosition(mapMarker.Value.X, scale);
        var AethersY = ConvertMapMarkerToRawPosition(mapMarker.Value.Y, scale);
        Svc.Log.Debug($"Aetheryte Position for #{aetheryte.RowId}: {AethersX}, {AethersY}");

        return new Vector2(AethersX, AethersY);
    }

    public static float GetDistanceToAetheryte(Aetheryte aetheryte, Vector3 pos)
    {
        var pos2d = new Vector2(pos.X, pos.Z);
        var aetherytePos = GetAetherytePosition(aetheryte);
        return Vector2.Distance(aetherytePos, pos2d);
    }

    public static Aetheryte? GetNearestAetheryte(MapMarkerData marker) => GetNearestAetheryte(marker.TerritoryTypeId, new Vector3(marker.X, marker.Y, marker.Z));
    public static Aetheryte? GetNearestAetheryte(FlagMapMarker flag) => GetNearestAetheryte(flag.TerritoryId, new Vector3(flag.XFloat, 0, flag.YFloat));
    public static Aetheryte? GetNearestAetheryte(uint zoneID, Vector3 pos)
    {
        Aetheryte? nearestAetheryte = null;
        double distance = double.MaxValue;
        foreach (var aetheryte in GetSheet<Aetheryte>())
        {
            //if (!data.IsAetheryte) continue;
            if (!aetheryte.Territory.IsValid) continue;
            //if (!aetheryte.PlaceName.IsValid) continue;
            if (aetheryte.Territory.Value.RowId == zoneID)
            {
                var temp_distance = GetDistanceToAetheryte(aetheryte, pos);
                Svc.Log.Info($"Distance from {aetheryte.PlaceName.Value.Name}{aetheryte.AethernetName.Value.Name}: {temp_distance}");
                if (nearestAetheryte is null || temp_distance < distance)
                {
                    distance = temp_distance;
                    nearestAetheryte = aetheryte;
                }
            }
        }

        return nearestAetheryte;
    }

    public static uint? GetPrimaryAetheryte(uint zoneID) => FindRow<Aetheryte>(a => a.Territory.IsValid && a.Territory.Value.RowId == zoneID)?.RowId ?? null;

    public static Aetheryte GetPrimaryAetheryte(Aetheryte aetheryte)
    {
        if (aetheryte.IsAetheryte)
            return aetheryte;
        else
            return FindRow<Aetheryte>(a => a.IsAetheryte && a.AethernetGroup == aetheryte.AethernetGroup)!.Value;
    }

    private static float ConvertMapMarkerToRawPosition(int pos, float scale = 100f)
    {
        var num = scale / 100f;
        var rawPosition = ((float)(pos - 1024.0) / num);
        return rawPosition;
    }

    private static float ConvertMapMarkerToMapCoordinate(int pos, float scale)
    {
        var rawPosition = ConvertMapMarkerToRawPosition(pos, scale);
        return ConvertRawPositionToMapCoordinate((int)(rawPosition * 1000), scale);
    }

    private static float ConvertRawPositionToMapCoordinate(int pos, float scale)
    {
        var num = scale / 100f;
        return (float)((pos / 1000f * num + 1024.0) / 2048.0 * 41.0 / num + 1.0);
    }

    public static unsafe void TeleportToAetheryte(uint aetheryteID)
    {
        Telepo.Instance()->Teleport(aetheryteID, 0);
    }

    private static TextPayload? GetInstanceIcon(int? instance)
    {
        return instance switch
        {
            1 => new TextPayload(SeIconChar.Instance1.ToIconString()),
            2 => new TextPayload(SeIconChar.Instance2.ToIconString()),
            3 => new TextPayload(SeIconChar.Instance3.ToIconString()),
            _ => default,
        };
    }

    public static uint? GetMapID(uint territory) => GetRow<TerritoryType>(territory)?.Map.Value.RowId ?? null;
    public static float GetMapScale(uint? territory = null) => GetRow<TerritoryType>(territory ?? Svc.ClientState.TerritoryType)?.Map.Value.SizeFactor ?? 100f;
}
