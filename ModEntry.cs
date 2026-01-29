using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace KivoValley;

/// <summary>
/// KivoValley模组入口类
/// </summary>
[SuppressMessage("ReSharper", "UnusedType.Global")]
public class ModEntry : Mod
{
    public override void Entry(IModHelper helper)
    {
        // 初始化传送系统
        Teleport.Helper = Helper;
        Teleport.TeleportTexture = helper.ModContent.Load<Texture2D>("assets/teleped.png");

        // Harmony补丁注入
        var harmony = new Harmony(ModManifest.UniqueID);
        harmony.PatchAll();

        // 注册事件监听
        helper.Events.Input.ButtonPressed += OnButtonPressed;

        Monitor.Log("KivoValley模组已加载！", LogLevel.Info);
    }

    /// <summary>
    /// 处理按键事件
    /// </summary>
    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (Game1.player == null) return;

        if (e.Button == SButton.MouseRight)
        {
            HandleRightClick();
        }
        else if (e.Button == SButton.F9)
        {
            Teleport.GiveAllScrollsToPlayer(Game1.player);
        }
    }

    /// <summary>
    /// 处理右键点击传送
    /// </summary>
    private void HandleRightClick()
    {
        var heldItem = Game1.player.ActiveItem;
        if (heldItem == null) return;

        // 检查是否持有任意传送物品
        var targetLocation = Teleport.Locations
            .FirstOrDefault(loc => heldItem.itemId.Value?.Equals(loc.MapName) ?? false);

        if (targetLocation == null) return;

        try
        {
            // 检查目标地图是否存在
            var mapLocation = Game1.getLocationFromName(targetLocation.MapName);
            if (mapLocation == null)
            {
                Game1.addHUDMessage(new HUDMessage($"目标地图 '{targetLocation.MapName}' 不存在！", 3));
                return;
            }

            // 检查玩家是否在可以传送的状态
            if (Game1.player.isRidingHorse())
            {
                Game1.addHUDMessage(new HUDMessage("骑马时无法使用传送卷轴！", 3));
                return;
            }

            // 执行传送
            Game1.warpFarmer(targetLocation.MapName, targetLocation.X, targetLocation.Y, false);
            Game1.addHUDMessage(new HUDMessage($"已传送到 {targetLocation.MapName}！", 2));

            // 播放传送音效
            Game1.playSound("wand");
        }
        catch (Exception ex)
        {
            Game1.addHUDMessage(new HUDMessage($"传送失败: {ex.Message}", 3));
        }
    }
}