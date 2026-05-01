using Microsoft.Xna.Framework;
using StardewModdingAPI.Utilities;
using StardewValley;
using Object = StardewValley.Object;

namespace KivoValley;

/// <summary>
/// 传送系统管理类
/// </summary>
public static class Teleport
{
    public const string ItemId = "KivoShrine";

    /// <summary>
    /// 保存的返回传送位置（每个玩家一个）
    /// Key: PlayerID, Value: (MapName, X, Y)
    /// </summary>
    private static readonly Dictionary<long, LastPosition> LastPositions = new();

    /// <summary>
    /// 所有可用的传送地点
    /// </summary>
    public static readonly TeleportLocation Location = new("Custom_KivoVallay_CasketShittin", 20, 20);

    /// <summary>
    /// 保存的位置信息
    /// </summary>
    private class LastPosition
    {
        public string MapName { get; set; }
        public int X { get; set; }
        public int Y { get; set; }

        public LastPosition(string mapName, int x, int y)
        {
            MapName = mapName;
            X = x;
            Y = y;
        }

        public void Deconstruct(out string mapName, out int x, out int y)
        {
            mapName = MapName;
            x = X;
            y = Y;
        }
    }

    /// <summary>
    /// 将所有传送卷轴给予玩家
    /// </summary>
    public static void GiveAllScrollsToPlayer(Farmer player)
    {
        var scroll = CreateTeleportScroll(Location);
        player.addItemToInventory(scroll);
    }

    /// <summary>
    /// 创建传送卷轴物品
    /// </summary>
    private static Object CreateTeleportScroll(TeleportLocation location)
    {
        // 使用ItemRegistry创建物品
        var scroll = ItemRegistry.Create<Object>(ItemId);
        if (scroll != null)
        {
            scroll.Stack = 1;
        }

        return scroll ?? new Object(ItemId, 1);
    }

    /// <summary>
    /// 处理传送：当前位置与目标位置互跳
    /// </summary>
    public static bool HandleTeleport(Farmer player, TeleportLocation targetLocation, out string? error)
    {
        error = null;

        try
        {
            var currentMap = Game1.currentLocation.Name;
            var targetMap = targetLocation.MapName;

            // 如果当前位置是目标地图（教室），则返回上次位置
            if (currentMap == targetMap)
            {
                var playerId = player.UniqueMultiplayerID;
                if (!LastPositions.TryGetValue(playerId, out var lastPos))
                {
                    error = "没有保存的返回位置！";
                    return false;
                }

                // 传回上次位置
                Game1.warpFarmer(lastPos.MapName, lastPos.X, lastPos.Y, false);

                // 清除返回点
                LastPositions.Remove(playerId);
            }
            else
            {
                // 当前位置不是目标地图，保存当前位置并传送到目标
                var playerId = player.UniqueMultiplayerID;
                LastPositions[playerId] = new LastPosition(
                    Game1.currentLocation.Name,
                    (int)(player.Position.X / Game1.tileSize),
                    (int)(player.Position.Y / Game1.tileSize)
                );

                // 传送到目标位置
                Game1.warpFarmer(targetMap, targetLocation.X, targetLocation.Y, false);
            }

            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }
}

/// <summary>
/// 传送地点信息
/// </summary>
public class TeleportLocation
{
    /// <summary>
    /// 目标地图名称
    /// </summary>
    public string MapName { get; }

    /// <summary>
    /// 传送目标X坐标
    /// </summary>
    public int X { get; }

    /// <summary>
    /// 传送目标Y坐标
    /// </summary>
    public int Y { get; }

    /// <summary>
    /// 初始化传送地点
    /// </summary>
    /// <param name="mapName">目标地图名称</param>
    /// <param name="x">X坐标</param>
    /// <param name="y">Y坐标</param>
    public TeleportLocation(string mapName, int x, int y)
    {
        MapName = mapName;
        X = x;
        Y = y;
    }
}