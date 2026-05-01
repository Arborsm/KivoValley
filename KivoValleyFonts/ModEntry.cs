using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using KivoValley;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace KivoValleyFonts;

/// <summary>
/// KivoValley模组入口类
/// </summary>
[SuppressMessage("ReSharper", "UnusedType.Global")]
public class ModEntry : Mod
{
    private static readonly int? FallbackGlyphYOffsetOverride = null;

    private static readonly string[] FontAssetsToMerge = {
        "Fonts/SpriteFont1",
        "Fonts/SmallFont"
    };

    private ContentManager? _rawGameContent;
    private ModConfig _config = new();

    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        _config = helper.ReadConfig<ModConfig>();
        helper.WriteConfig(_config);

        //Helper.ModContent.Load<Texture2D>("assets/objects.png");

        // 注册内容替换
        helper.Events.Content.AssetRequested += OnAssetRequested;

        // Harmony补丁注入
        var harmony = new Harmony(ModManifest.UniqueID);
        harmony.PatchAll();

        // 注册事件监听
        //helper.Events.Input.ButtonPressed += OnButtonPressed;

        //Monitor.Log("KivoValley模组已加载！", LogLevel.Info);
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
                    _config.ExtraFontCharacters,
                    FallbackGlyphYOffsetOverride,
                    _config.DisableFallbackGlyphScale ? 1f : _config.FallbackGlyphScaleOverride,
                    _config.SmoothScaledFallbackGlyphs
                );

                if (mergeResult.AddedCharacters.Length > 0)
                {
                    asset.ReplaceWith(mergeResult.Font);
                    ApplyMergedFontToGameStatics(assetName, mergeResult.Font);

                    var mergedGlyphs = mergeResult.Font.GetGlyphs();
                    var confirmedCharacters = new string(mergeResult.AddedCharacters.Where(mergedGlyphs.ContainsKey).ToArray());
                    var missingAfterMerge = new string(mergeResult.AddedCharacters.Where(ch => !mergedGlyphs.ContainsKey(ch)).ToArray());

                    Monitor.Log(
                        $"已为中文 {assetName} 合并日文字形: {FormatFontCharactersWithCodes(mergeResult.AddedCharacters)}；自检存在: {FormatFontCharactersWithCodes(confirmedCharacters)}；自检缺失: {FormatFontCharactersWithCodes(missingAfterMerge)}；fallbackScale={mergeResult.FallbackGlyphScale:0.###}；disableScale={_config.DisableFallbackGlyphScale}；smoothScale={_config.SmoothScaledFallbackGlyphs}；fallbackYOffset={mergeResult.FallbackGlyphYOffset}；格式: zh={zhFont.Texture.Format}, ja={jaFont.Texture.Format}, merged={mergeResult.Font.Texture.Format}；atlas: {mergeResult.Font.Texture.Width}x{mergeResult.Font.Texture.Height}",
                        LogLevel.Info
                    );
                }
                else
                {
                    Monitor.Log(
                        $"中文 {assetName} 未合并新字形。已存在: {FormatFontCharacters(mergeResult.AlreadyPresentCharacters)}；日文字体缺失: {FormatFontCharacters(mergeResult.UnavailableCharacters)}；配置: {_config.ExtraFontCharacters}"
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
        _rawGameContent ??= new ContentManager(
            Game1.content.ServiceProvider,
            Path.Combine(Constants.GamePath, "Content")
        );

        return _rawGameContent.Load<SpriteFont>(assetName);
    }

    private static string FormatFontCharacters(string characters)
    {
        return string.IsNullOrEmpty(characters) ? "(无)" : characters;
    }

    private static string FormatFontCharactersWithCodes(string characters)
    {
        return string.IsNullOrEmpty(characters) ? "(无)" : string.Join(", ", characters.Select(ch => $"{ch}(U+{(int)ch:X4})"));
    }
}
