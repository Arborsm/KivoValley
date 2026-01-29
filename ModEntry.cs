using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Objects;

namespace KivoValley;

/// <summary>
/// KivoValley模组入口类
/// </summary>
[SuppressMessage("ReSharper", "UnusedType.Global")]
public class ModEntry : Mod
{
    public static Texture2D SpriteSheet;
    
    public override void Entry(IModHelper helper)
    {
        // 初始化传送系统
        Teleport.ModUniqueId = ModManifest.UniqueID;
        // 贴图通过Content Patcher加载，不再直接加载
        SpriteSheet = Helper.ModContent.Load<Texture2D>("assets/objects.png");

        // 添加自定义物品数据
        helper.Events.Content.AssetRequested += OnAssetRequested;

        // Harmony补丁注入
        var harmony = new Harmony(ModManifest.UniqueID);
        harmony.PatchAll();

        // 注册事件监听
        helper.Events.Input.ButtonPressed += OnButtonPressed;

        Monitor.Log("KivoValley模组已加载！", LogLevel.Info);
    }

    /// <summary>
    /// 注册自定义资产（物品数据）
    /// </summary>
    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
        {
            e.Edit(asset =>
            {
                var data = asset.AsDictionary<string, ObjectData>().Data;

                // 添加传送物品数据
                foreach (var location in Teleport.Locations)
                {
                    var itemId = $"{ModManifest.UniqueID}_{location.MapName}";

                    // 获取i18n翻译（使用实际文本，不是键）
                    var displayName = Helper.Translation.Get("item.kivo-shrine.name");
                    var description = Helper.Translation.Get("item.kivo-shrine.description");

                    // Data/Objects数据格式：Name/DisplayName/Description/Type/Category/Price/Texture/SpriteIndex
                    // Texture字段：使用Content Patcher加载的资产名称，不带路径
                    var itemData = new ObjectData
                    {
                        Name = itemId,
                        DisplayName = displayName,
                        Description = description,
                        Type = "Basic",
                        Category = StardewValley.Object.toolCategory,
                        Price = 2000,
                        Texture = Helper.ModContent.GetInternalAssetName( "assets/objects.png" ).Name,
                        SpriteIndex = 0
                    };
                    data[itemId] = itemData;
                }
            });
        }
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
            .FirstOrDefault(loc => heldItem.QualifiedItemId?.Contains(loc.MapName) ?? false);

        if (targetLocation == null) return;

        try
        {
            // 检查玩家是否在可以传送的状态
            if (Game1.player.isRidingHorse())
            {
                Game1.addHUDMessage(new HUDMessage("骑马时无法使用传送卷轴！", 3));
                return;
            }

            // 处理传送：当前位置与目标位置互跳
            if (Teleport.HandleTeleport(Game1.player, targetLocation, out var error))
            {
                Game1.playSound("wand");
            }
            else
            {
                Game1.addHUDMessage(new HUDMessage($"传送失败: {error}", 3));
            }
        }
        catch (Exception ex)
        {
            Game1.addHUDMessage(new HUDMessage($"传送失败: {ex.Message}", 3));
        }
    }
}