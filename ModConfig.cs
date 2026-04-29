namespace KivoValley;

internal sealed class ModConfig
{
    /// <summary>
    /// 需要从日文字体补进中文字体的字符。中文字体已有的字符不会被替换。
    /// </summary>
    public string ExtraFontCharacters { get; set; } = "咲";

    /// <summary>
    /// 是否禁用日文字形缩放。true 时强制使用 1.0，不进行自动测量或手动覆盖。
    /// </summary>
    public bool DisableFallbackGlyphScale { get; set; } = false;

    /// <summary>
    /// 日文字形缩放覆盖值。0 表示自动测量；正数表示手动覆盖，例如 0.85、0.9、0.95。
    /// </summary>
    public float FallbackGlyphScaleOverride { get; set; } = 0f;
}
