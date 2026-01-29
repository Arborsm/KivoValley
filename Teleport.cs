using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using Object = StardewValley.Object;

namespace KivoValley;

/// <summary>
/// 传送系统管理类
/// </summary>
public static class Teleport
{
    /// <summary>
    /// 传送物品贴图
    /// </summary>
    public static Texture2D? TeleportTexture;

    /// <summary>
    /// SMAPI Helper引用（用于i18n）
    /// </summary>
    public static IModHelper? Helper;

    /// <summary>
    /// 所有可用的传送地点
    /// </summary>
    public static readonly TeleportLocation[] Locations = {
        new("Custom_KivoVallay_CasketShittin", 20, 20, "item.kivo-shrine.name", "item.kivo-shrine.description"),
    };

    /// <summary>
    /// 将所有传送卷轴给予玩家
    /// </summary>
    public static void GiveAllScrollsToPlayer(Farmer player)
    {
        foreach (var location in Locations)
        {
            var scroll = CreateTeleportScroll(location);
            player.addItemToInventory(scroll);
        }
    }

    /// <summary>
    /// 创建传送卷轴物品
    /// </summary>
    private static Object CreateTeleportScroll(TeleportLocation location)
    {
        // 使用i18n翻译
        var displayName = Helper?.Translation.Get(location.DisplayName) ?? location.DisplayName;
        var description = Helper?.Translation.Get(location.Description) ?? location.Description;

        var scroll = new Object(new Vector2(), location.MapName)
        {
            Stack = 1,
            Name = displayName,
            signText = { description }
        };

        // 设置自定义贴图
        if (TeleportTexture != null)
        {
            scroll.Name = location.MapName;
            scroll.Category = -999;
        }

        return scroll;
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
    /// 传送物品显示名称（i18n键）
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// 传送物品描述（i18n键）
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// 初始化传送地点
    /// </summary>
    /// <param name="mapName">目标地图名称</param>
    /// <param name="x">X坐标</param>
    /// <param name="y">Y坐标</param>
    /// <param name="displayName">显示名称i18n键</param>
    /// <param name="description">描述i18n键</param>
    public TeleportLocation(string mapName, int x, int y, string displayName, string description)
    {
        MapName = mapName;
        X = x;
        Y = y;
        DisplayName = displayName;
        Description = description;
    }
}