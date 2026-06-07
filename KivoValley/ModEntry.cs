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
    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        Helper.ModContent.Load<Texture2D>("assets/objects.png");

        // 添加自定义物品数据
        helper.Events.Content.AssetRequested += OnAssetRequested;

        // Harmony补丁注入
        var harmony = new Harmony(ModManifest.UniqueID);
        harmony.PatchAll();

        // 注册事件监听
        helper.Events.Input.ButtonPressed += OnButtonPressed;

        Monitor.Log(I18n.Mod_Loaded(), LogLevel.Info);
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

                var itemData = new ObjectData
                {
                    Name = Teleport.ItemId,
                    DisplayName = @$"[LocalizedText Strings\\Objects:{Teleport.ItemId}_Name]",
                    Description = @$"[LocalizedText Strings\\Objects:{Teleport.ItemId}_Description]",
                    Type = "Quest",
                    Category = StardewValley.Object.toolCategory,
                    Price = 0,
                    Texture = Helper.ModContent.GetInternalAssetName( "assets/objects.png" ).Name,
                    SpriteIndex = 0,
                    CanBeTrashed = false,
                    CanBeGivenAsGift = false,
                    ExcludeFromShippingCollection = true,
                    ExcludeFromRandomSale = true,
                };
                data[Teleport.ItemId] = itemData;
            });
        }
        else if (e.NameWithoutLocale.IsEquivalentTo("Strings/Objects"))
        {
            e.Edit(asset => {
                var dict = asset.AsDictionary<string, string>();
                dict.Data[$"{Teleport.ItemId}_Name"] = I18n.Item_KivoShrine_Name();
                dict.Data[$"{Teleport.ItemId}_Description"] = I18n.Item_KivoShrine_Description();
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
    }

    /// <summary>
    /// 处理右键点击传送
    /// </summary>
    private void HandleRightClick()
    {
        var heldItem = Game1.player.ActiveItem;
        if (heldItem == null) return;

        if (!heldItem.ItemId?.Equals(Teleport.ItemId) ?? false) return;

        try
        {
            if (Game1.isFestival())
            {
                Game1.addHUDMessage(new HUDMessage(I18n.Teleport_FestivalBlocked(), 3));
                return;
            }

            // 检查玩家是否在可以传送的状态
            if (Game1.player.isRidingHorse())
            {
                Game1.addHUDMessage(new HUDMessage(I18n.Teleport_HorseBlocked(), 3));
                return;
            }

            if (Game1.uiMode || Game1.activeClickableMenu != null)
            {
                return;
            }

            // 处理传送：当前位置与目标位置互跳
            if (Teleport.HandleTeleport(Game1.player, Teleport.Location, out var error))
            {
                Game1.playSound("wand");
            }
            else
            {
                Game1.addHUDMessage(new HUDMessage(I18n.Teleport_Fail(error), 3));
            }
        }
        catch (Exception ex)
        {
            Game1.addHUDMessage(new HUDMessage(I18n.Teleport_Fail(ex.Message), 3));
        }
    }
}
