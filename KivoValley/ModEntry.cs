using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using Microsoft.Xna.Framework.Content;
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
    private static readonly int? FallbackGlyphYOffsetOverride = null;

    private static readonly string[] FontAssetsToMerge = new[]
    {
        "Fonts/SpriteFont1",
        "Fonts/SmallFont"
    };

    private ContentManager? rawGameContent;
    private ModConfig config = new();

    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        config = helper.ReadConfig<ModConfig>();
        helper.WriteConfig(config);

        Helper.ModContent.Load<Texture2D>("assets/objects.png");

        // 注册内容替换
        helper.Events.Content.AssetRequested += OnAssetRequested;

        // Harmony补丁注入
        var harmony = new Harmony(ModManifest.UniqueID);
        harmony.PatchAll();

        // 注册事件监听
        helper.Events.Input.ButtonPressed += OnButtonPressed;

        Monitor.Log("KivoValley模组已加载！", LogLevel.Info);
    }

    /// <summary>
    /// 注册自定义资产（物品数据）和中文字体补字
    /// </summary>
    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        var fontAssetName = FontAssetsToMerge.FirstOrDefault(assetName => e.NameWithoutLocale.IsEquivalentTo(assetName));
        if (fontAssetName != null)
        {
            EditChineseSpriteFont(e, fontAssetName);
        }
        else if (e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
        {
            e.Edit(asset =>
            {
                var data = asset.AsDictionary<string, ObjectData>().Data;

                var itemData = new ObjectData
                {
                    Name = Teleport.ItemId,
                    DisplayName = @$"[LocalizedText Strings\\Objects:{Teleport.ItemId}_Name]",
                    Description = @$"[LocalizedText Strings\\Objects:{Teleport.ItemId}_Description]",
                    Type = "Basic",
                    Category = StardewValley.Object.toolCategory,
                    Texture = Helper.ModContent.GetInternalAssetName("assets/objects.png").Name,
                    SpriteIndex = 0,
                    CanBeTrashed = false,
                    CanBeGivenAsGift = false,
                };
                data[Teleport.ItemId] = itemData;
            });
        }
        else if (e.NameWithoutLocale.IsEquivalentTo("Strings/Objects"))
        {
            e.Edit(asset =>
            {
                var dict = asset.AsDictionary<string, string>();
                dict.Data[$"{Teleport.ItemId}_Name"] = I18n.Item_KivoShrine_Name();
                dict.Data[$"{Teleport.ItemId}_Description"] = I18n.Item_KivoShrine_Description();
            });
        }
    }

    private void EditChineseSpriteFont(AssetRequestedEventArgs e, string assetName)
    {
        if (LocalizedContentManager.CurrentLanguageCode != LocalizedContentManager.LanguageCode.zh)
        {
            return;
        }

        e.Edit(asset =>
        {
            if (asset.Data is not SpriteFont zhFont)
            {
                return;
            }

            try
            {
                var jaFont = LoadRawGameFont($"{assetName}.ja-JP");
                var mergeResult = SpriteFontMerger.MergeMissingGlyphs(
                    Game1.graphics.GraphicsDevice,
                    zhFont,
                    jaFont,
                    config.ExtraFontCharacters,
                    FallbackGlyphYOffsetOverride,
                    config.DisableFallbackGlyphScale ? 1f : config.FallbackGlyphScaleOverride
                );

                if (mergeResult.AddedCharacters.Length > 0)
                {
                    asset.ReplaceWith(mergeResult.Font);
                    ApplyMergedFontToGameStatics(assetName, mergeResult.Font);

                    var mergedGlyphs = mergeResult.Font.GetGlyphs();
                    var confirmedCharacters = new string(mergeResult.AddedCharacters.Where(mergedGlyphs.ContainsKey).ToArray());
                    var missingAfterMerge = new string(mergeResult.AddedCharacters.Where(ch => !mergedGlyphs.ContainsKey(ch)).ToArray());

                    Monitor.Log(
                        $"已为中文 {assetName} 合并日文字形: {FormatFontCharactersWithCodes(mergeResult.AddedCharacters)}；自检存在: {FormatFontCharactersWithCodes(confirmedCharacters)}；自检缺失: {FormatFontCharactersWithCodes(missingAfterMerge)}；fallbackScale={mergeResult.FallbackGlyphScale:0.###}；disableScale={config.DisableFallbackGlyphScale}；fallbackYOffset={mergeResult.FallbackGlyphYOffset}；格式: zh={zhFont.Texture.Format}, ja={jaFont.Texture.Format}, merged={mergeResult.Font.Texture.Format}；atlas: {mergeResult.Font.Texture.Width}x{mergeResult.Font.Texture.Height}",
                        LogLevel.Info
                    );
                }
                else
                {
                    Monitor.Log(
                        $"中文 {assetName} 未合并新字形。已存在: {FormatFontCharacters(mergeResult.AlreadyPresentCharacters)}；日文字体缺失: {FormatFontCharacters(mergeResult.UnavailableCharacters)}；配置: {config.ExtraFontCharacters}",
                        LogLevel.Trace
                    );
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"合并中文 {assetName} 的日文字形失败，保留原字体。{ex}", LogLevel.Warn);
            }
        }, AssetEditPriority.Late);
    }

    private static void ApplyMergedFontToGameStatics(string assetName, SpriteFont mergedFont)
    {
        if (assetName == "Fonts/SpriteFont1")
        {
            Game1.dialogueFont = mergedFont;
        }
        else if (assetName == "Fonts/SmallFont")
        {
            Game1.smallFont = mergedFont;
        }
    }

    private SpriteFont LoadRawGameFont(string assetName)
    {
        rawGameContent ??= new ContentManager(
            Game1.content.ServiceProvider,
            Path.Combine(Constants.GamePath, "Content")
        );

        return rawGameContent.Load<SpriteFont>(assetName);
    }

    private static string FormatFontCharacters(string characters)
    {
        return string.IsNullOrEmpty(characters) ? "(无)" : characters;
    }

    private static string FormatFontCharactersWithCodes(string characters)
    {
        if (string.IsNullOrEmpty(characters))
        {
            return "(无)";
        }

        return string.Join(", ", characters.Select(ch => $"{ch}(U+{(int)ch:X4})"));
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

        if (!heldItem.ItemId?.Equals(Teleport.ItemId) ?? false) return;

        try
        {
            // 检查玩家是否在可以传送的状态
            if (Game1.player.isRidingHorse())
            {
                Game1.addHUDMessage(new HUDMessage("骑马时无法使用传送卷轴！", 3));
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
                Game1.addHUDMessage(new HUDMessage($"传送失败: {error}", 3));
            }
        }
        catch (Exception ex)
        {
            Game1.addHUDMessage(new HUDMessage($"传送失败: {ex.Message}", 3));
        }
    }
}
