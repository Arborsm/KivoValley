using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace KivoValleyFonts;

internal static class SpriteFontMerger
{
    private const int Padding = 1;
    private const string ScaleProbeCharacters =
        "的一是在不了有和人这中大为上个国我以要他时来用们生到作地于出就分对成会可主发年动同工也能下过子说产种面而方后多定行学法所民得经十三之进着等部度家电力里如水化高自二理起小物现实加量都两体制机当使点从业本去把性好应开它合还因由其些然前外天政四日那社义事平形相全表间样与关各重新线内数正心反你明看原又么利比或但质气第向道命此变条只没结解问意建月公无系军很情者最立代想已通并提直题程展五果料象员入常文总次品式活设及管特件长求老头基资边流路级少图山统接知较将组见计别";

    public static SpriteFontMergeResult MergeMissingGlyphs(
        GraphicsDevice graphicsDevice,
        SpriteFont primary,
        SpriteFont fallback,
        string charsToAdd,
        int? fallbackYOffsetOverride = null,
        float fallbackGlyphScale = 0f,
        bool smoothScaledFallbackGlyphs = true
    )
    {
        if (string.IsNullOrEmpty(charsToAdd))
        {
            return new SpriteFontMergeResult(primary, string.Empty, string.Empty, string.Empty, 1f, fallbackYOffsetOverride ?? 0);
        }

        var primaryGlyphs = primary.GetGlyphs();
        var fallbackGlyphs = fallback.GetGlyphs();
        var inkMeasurement = MeasureProbeGlyphInk(graphicsDevice, primary, fallback, primaryGlyphs, fallbackGlyphs);
        var glyphScale = fallbackGlyphScale > 0
            ? Math.Clamp(fallbackGlyphScale, 0.1f, 2f)
            : EstimateFallbackGlyphScale(primary, fallback, primaryGlyphs, fallbackGlyphs, inkMeasurement);
        var glyphYOffset = fallbackYOffsetOverride
            ?? EstimateFallbackGlyphYOffset(primaryGlyphs, fallbackGlyphs, glyphScale, inkMeasurement);
        var configuredCharacters = charsToAdd
            .Distinct()
            .Where(ch => !char.IsSurrogate(ch))
            .OrderBy(ch => ch)
            .ToList();
        var alreadyPresentCharacters = configuredCharacters
            .Where(primaryGlyphs.ContainsKey)
            .ToList();
        var unavailableCharacters = configuredCharacters
            .Where(ch => !primaryGlyphs.ContainsKey(ch))
            .Where(ch => !fallbackGlyphs.ContainsKey(ch))
            .ToList();
        var missingCharacters = configuredCharacters
            .Where(ch => !primaryGlyphs.ContainsKey(ch))
            .Where(ch => fallbackGlyphs.ContainsKey(ch))
            .ToList();

        if (missingCharacters.Count == 0)
        {
            return new SpriteFontMergeResult(
                primary,
                string.Empty,
                new string(alreadyPresentCharacters.ToArray()),
                new string(unavailableCharacters.ToArray()),
                glyphScale,
                glyphYOffset
            );
        }

        var placements = PackMissingGlyphs(
            primary.Texture.Width,
            primary.Texture.Height,
            fallbackGlyphs,
            missingCharacters,
            glyphScale
        );
        if (placements.Count == 0)
        {
            return new SpriteFontMergeResult(
                primary,
                string.Empty,
                new string(alreadyPresentCharacters.ToArray()),
                new string(unavailableCharacters.ToArray()),
                glyphScale,
                glyphYOffset
            );
        }

        var mergedTexture = BuildMergedTexture(
            graphicsDevice,
            primary.Texture,
            fallback.Texture,
            placements,
            glyphScale,
            smoothScaledFallbackGlyphs
        );
        return new SpriteFontMergeResult(
            BuildMergedFont(primary, mergedTexture, primaryGlyphs, placements, glyphYOffset, glyphScale),
            new string(placements.Select(placement => placement.Character).ToArray()),
            new string(alreadyPresentCharacters.ToArray()),
            new string(unavailableCharacters.ToArray()),
            glyphScale,
            glyphYOffset
        );
    }

    private static GlyphInkMeasurement MeasureProbeGlyphInk(
        GraphicsDevice graphicsDevice,
        SpriteFont primary,
        SpriteFont fallback,
        Dictionary<char, SpriteFont.Glyph> primaryGlyphs,
        Dictionary<char, SpriteFont.Glyph> fallbackGlyphs
    )
    {
        try
        {
            var primaryPixels = ReadColorPixels(graphicsDevice, primary.Texture);
            var fallbackPixels = ReadColorPixels(graphicsDevice, fallback.Texture);
            var primaryInk = new Dictionary<char, GlyphInkBounds>();
            var fallbackInk = new Dictionary<char, GlyphInkBounds>();

            foreach (var character in ScaleProbeCharacters.Distinct())
            {
                if (
                    primaryGlyphs.TryGetValue(character, out var primaryGlyph)
                    && TryMeasureGlyphInk(primaryPixels, primary.Texture.Width, primaryGlyph.BoundsInTexture, out var primaryBounds)
                )
                {
                    primaryInk[character] = primaryBounds;
                }

                if (
                    fallbackGlyphs.TryGetValue(character, out var fallbackGlyph)
                    && TryMeasureGlyphInk(fallbackPixels, fallback.Texture.Width, fallbackGlyph.BoundsInTexture, out var fallbackBounds)
                )
                {
                    fallbackInk[character] = fallbackBounds;
                }
            }

            return new GlyphInkMeasurement(primaryInk, fallbackInk);
        }
        catch
        {
            return GlyphInkMeasurement.Empty;
        }
    }

    private static bool TryMeasureGlyphInk(
        Color[] pixels,
        int textureWidth,
        Rectangle bounds,
        out GlyphInkBounds inkBounds
    )
    {
        const byte alphaThreshold = 16;

        var minX = int.MaxValue;
        var minY = int.MaxValue;
        var maxX = int.MinValue;
        var maxY = int.MinValue;

        for (var y = 0; y < bounds.Height; y++)
        {
            var rowOffset = (bounds.Y + y) * textureWidth + bounds.X;
            for (var x = 0; x < bounds.Width; x++)
            {
                if (pixels[rowOffset + x].A <= alphaThreshold)
                {
                    continue;
                }

                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
        }

        if (minX == int.MaxValue)
        {
            inkBounds = default;
            return false;
        }

        inkBounds = new GlyphInkBounds(minX, minY, maxX, maxY);
        return true;
    }

    private static float EstimateFallbackGlyphScale(
        SpriteFont primary,
        SpriteFont fallback,
        Dictionary<char, SpriteFont.Glyph> primaryGlyphs,
        Dictionary<char, SpriteFont.Glyph> fallbackGlyphs,
        GlyphInkMeasurement inkMeasurement
    )
    {
        var ratios = new List<float>();

        foreach (var character in ScaleProbeCharacters.Distinct())
        {
            if (!primaryGlyphs.TryGetValue(character, out var primaryGlyph))
            {
                continue;
            }

            if (!fallbackGlyphs.TryGetValue(character, out var fallbackGlyph))
            {
                continue;
            }

            if (
                inkMeasurement.Primary.TryGetValue(character, out var primaryInk)
                && inkMeasurement.Fallback.TryGetValue(character, out var fallbackInk)
            )
            {
                AddRatio(ratios, primaryInk.Height, fallbackInk.Height);
                continue;
            }

            AddRatio(ratios, MeasureGlyphHeight(primaryGlyph), MeasureGlyphHeight(fallbackGlyph));
        }

        if (ratios.Count == 0)
        {
            AddRatio(ratios, primary.LineSpacing, fallback.LineSpacing);
        }

        if (ratios.Count == 0)
        {
            return 1f;
        }

        ratios.Sort();
        var middle = ratios.Count / 2;
        var median = ratios.Count % 2 == 0
            ? (ratios[middle - 1] + ratios[middle]) / 2f
            : ratios[middle];

        return Math.Clamp(median, 0.65f, 1f);
    }

    private static int MeasureGlyphHeight(SpriteFont.Glyph glyph)
    {
        return glyph.Cropping.Height > 0
            ? glyph.Cropping.Height
            : glyph.BoundsInTexture.Height;
    }

    private static int EstimateFallbackGlyphYOffset(
        Dictionary<char, SpriteFont.Glyph> primaryGlyphs,
        Dictionary<char, SpriteFont.Glyph> fallbackGlyphs,
        float fallbackGlyphScale,
        GlyphInkMeasurement inkMeasurement
    )
    {
        var offsets = new List<float>();

        foreach (var character in ScaleProbeCharacters.Distinct())
        {
            if (!primaryGlyphs.TryGetValue(character, out var primaryGlyph))
            {
                continue;
            }

            if (!fallbackGlyphs.TryGetValue(character, out var fallbackGlyph))
            {
                continue;
            }

            if (
                inkMeasurement.Primary.TryGetValue(character, out var primaryInk)
                && inkMeasurement.Fallback.TryGetValue(character, out var fallbackInk)
            )
            {
                var primaryCenter = primaryGlyph.Cropping.Y + primaryInk.CenterY;
                var fallbackCenter = (fallbackGlyph.Cropping.Y + fallbackInk.CenterY) * fallbackGlyphScale;
                AddOffset(offsets, primaryCenter - fallbackCenter);
                continue;
            }

            var primaryCenterFromMetadata = primaryGlyph.Cropping.Y + MeasureGlyphHeight(primaryGlyph) / 2f;
            var fallbackCenterFromMetadata = (fallbackGlyph.Cropping.Y + MeasureGlyphHeight(fallbackGlyph) / 2f) * fallbackGlyphScale;
            AddOffset(offsets, primaryCenterFromMetadata - fallbackCenterFromMetadata);
        }

        if (offsets.Count == 0)
        {
            return 0;
        }

        offsets.Sort();
        var middle = offsets.Count / 2;
        var median = offsets.Count % 2 == 0
            ? (offsets[middle - 1] + offsets[middle]) / 2f
            : offsets[middle];

        return (int)MathF.Round(Math.Clamp(median, -12f, 12f));
    }

    private static void AddRatio(List<float> ratios, float primaryValue, float fallbackValue)
    {
        if (primaryValue <= 0 || fallbackValue <= 0)
        {
            return;
        }

        var ratio = primaryValue / fallbackValue;
        if (ratio is >= 0.4f and <= 1.8f)
        {
            ratios.Add(ratio);
        }
    }

    private static void AddOffset(List<float> offsets, float offset)
    {
        if (float.IsNaN(offset) || float.IsInfinity(offset))
        {
            return;
        }

        if (offset is >= -24f and <= 24f)
        {
            offsets.Add(offset);
        }
    }

    private static List<(char Character, SpriteFont.Glyph SourceGlyph, Rectangle TargetBounds)> PackMissingGlyphs(
        int primaryTextureWidth,
        int primaryTextureHeight,
        Dictionary<char, SpriteFont.Glyph> fallbackGlyphs,
        List<char> missingCharacters,
        float fallbackGlyphScale
    )
    {
        var atlasWidth = Math.Max(
            primaryTextureWidth,
            missingCharacters.Max(ch => ScalePixelLength(fallbackGlyphs[ch].BoundsInTexture.Width, fallbackGlyphScale))
        );
        var cursorX = 0;
        var cursorY = primaryTextureHeight + Padding;
        var rowHeight = 0;
        var placements = new List<(char Character, SpriteFont.Glyph SourceGlyph, Rectangle TargetBounds)>();

        foreach (var character in missingCharacters)
        {
            var glyph = fallbackGlyphs[character];
            var sourceBounds = glyph.BoundsInTexture;
            if (sourceBounds.Width <= 0 || sourceBounds.Height <= 0)
            {
                continue;
            }

            var targetWidth = ScalePixelLength(sourceBounds.Width, fallbackGlyphScale);
            var targetHeight = ScalePixelLength(sourceBounds.Height, fallbackGlyphScale);

            if (cursorX > 0 && cursorX + targetWidth > atlasWidth)
            {
                cursorX = 0;
                cursorY += rowHeight + Padding;
                rowHeight = 0;
            }

            placements.Add((
                character,
                glyph,
                new Rectangle(cursorX, cursorY, targetWidth, targetHeight)
            ));

            cursorX += targetWidth + Padding;
            rowHeight = Math.Max(rowHeight, targetHeight);
        }

        return placements;
    }

    private static Texture2D BuildMergedTexture(
        GraphicsDevice graphicsDevice,
        Texture2D primaryTexture,
        Texture2D fallbackTexture,
        List<(char Character, SpriteFont.Glyph SourceGlyph, Rectangle TargetBounds)> placements,
        float fallbackGlyphScale,
        bool smoothScaledFallbackGlyphs
    )
    {
        var width = Math.Max(primaryTexture.Width, placements.Max(placement => placement.TargetBounds.Right));
        var height = Math.Max(primaryTexture.Height, placements.Max(placement => placement.TargetBounds.Bottom));
        var canCopyWithoutScaling = Math.Abs(fallbackGlyphScale - 1f) < 0.001f;

        if (canCopyWithoutScaling && primaryTexture.Format == SurfaceFormat.Alpha8)
        {
            var mergedPixels = new byte[width * height];

            CopyPixels(
                ReadAlphaPixels(primaryTexture),
                primaryTexture.Width,
                mergedPixels,
                width,
                new Rectangle(0, 0, primaryTexture.Width, primaryTexture.Height),
                0,
                0
            );

            var fallbackPixels = ReadAlphaPixels(fallbackTexture);
            foreach (var placement in placements)
            {
                CopyPixels(
                    fallbackPixels,
                    fallbackTexture.Width,
                    mergedPixels,
                    width,
                    placement.SourceGlyph.BoundsInTexture,
                    placement.TargetBounds.X,
                    placement.TargetBounds.Y
                );
            }

            var texture = new Texture2D(graphicsDevice, width, height, false, SurfaceFormat.Alpha8);
            texture.SetData(mergedPixels);
            return texture;
        }

        if (canCopyWithoutScaling && primaryTexture.Format == SurfaceFormat.Color)
        {
            var mergedPixels = new Color[width * height];

            CopyPixels(
                ReadColorPixels(primaryTexture),
                primaryTexture.Width,
                mergedPixels,
                width,
                new Rectangle(0, 0, primaryTexture.Width, primaryTexture.Height),
                0,
                0
            );

            var fallbackPixels = ReadColorPixels(fallbackTexture);
            foreach (var placement in placements)
            {
                CopyPixels(
                    fallbackPixels,
                    fallbackTexture.Width,
                    mergedPixels,
                    width,
                    placement.SourceGlyph.BoundsInTexture,
                    placement.TargetBounds.X,
                    placement.TargetBounds.Y
                );
            }

            var texture = new Texture2D(graphicsDevice, width, height, false, SurfaceFormat.Color);
            texture.SetData(mergedPixels);
            return texture;
        }

        return BuildMergedTextureWithGpu(
            graphicsDevice,
            primaryTexture,
            fallbackTexture,
            placements,
            width,
            height,
            smoothScaledFallbackGlyphs && !canCopyWithoutScaling
        );
    }

    private static Texture2D BuildMergedTextureWithGpu(
        GraphicsDevice graphicsDevice,
        Texture2D primaryTexture,
        Texture2D fallbackTexture,
        List<(char Character, SpriteFont.Glyph SourceGlyph, Rectangle TargetBounds)> placements,
        int width,
        int height,
        bool smoothFallbackGlyphs
    )
    {
        var previousRenderTargets = graphicsDevice.GetRenderTargets();
        var previousViewport = graphicsDevice.Viewport;
        var renderTarget = new RenderTarget2D(
            graphicsDevice,
            width,
            height,
            false,
            SurfaceFormat.Color,
            DepthFormat.None
        );

        try
        {
            graphicsDevice.SetRenderTarget(renderTarget);
            graphicsDevice.Clear(Color.Transparent);

            using var spriteBatch = new SpriteBatch(graphicsDevice);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp);
            spriteBatch.Draw(
                primaryTexture,
                new Rectangle(0, 0, primaryTexture.Width, primaryTexture.Height),
                new Rectangle(0, 0, primaryTexture.Width, primaryTexture.Height),
                Color.White
            );
            spriteBatch.End();

            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.Opaque,
                smoothFallbackGlyphs ? SamplerState.LinearClamp : SamplerState.PointClamp
            );
            foreach (var placement in placements)
            {
                spriteBatch.Draw(
                    fallbackTexture,
                    placement.TargetBounds,
                    placement.SourceGlyph.BoundsInTexture,
                    Color.White
                );
            }

            spriteBatch.End();
        }
        finally
        {
            graphicsDevice.SetRenderTargets(previousRenderTargets);
            graphicsDevice.Viewport = previousViewport;
        }

        return renderTarget;
    }

    private static SpriteFont BuildMergedFont(
        SpriteFont primary,
        Texture2D mergedTexture,
        Dictionary<char, SpriteFont.Glyph> primaryGlyphs,
        List<(char Character, SpriteFont.Glyph SourceGlyph, Rectangle TargetBounds)> placements,
        int fallbackYOffset,
        float fallbackGlyphScale
    )
    {
        var glyphs = primaryGlyphs.Values.ToList();

        foreach (var placement in placements)
        {
            var sourceGlyph = placement.SourceGlyph;
            var cropping = sourceGlyph.Cropping;
            if (Math.Abs(fallbackGlyphScale - 1f) >= 0.001f)
            {
                cropping.X = ScaleSignedPixelLength(cropping.X, fallbackGlyphScale);
                cropping.Y = ScaleSignedPixelLength(cropping.Y, fallbackGlyphScale);
                cropping.Width = placement.TargetBounds.Width;
                cropping.Height = placement.TargetBounds.Height;
            }

            cropping.Y += fallbackYOffset;
            var leftSideBearing = sourceGlyph.LeftSideBearing * fallbackGlyphScale;
            var width = sourceGlyph.Width * fallbackGlyphScale;
            var rightSideBearing = sourceGlyph.RightSideBearing * fallbackGlyphScale;

            glyphs.Add(new SpriteFont.Glyph
            {
                Character = placement.Character,
                BoundsInTexture = placement.TargetBounds,
                Cropping = cropping,
                LeftSideBearing = leftSideBearing,
                Width = width,
                RightSideBearing = rightSideBearing,
                WidthIncludingBearings = leftSideBearing + width + rightSideBearing
            });
        }

        glyphs = glyphs.OrderBy(glyph => glyph.Character).ToList();

        return new SpriteFont(
            mergedTexture,
            glyphs.Select(glyph => glyph.BoundsInTexture).ToList(),
            glyphs.Select(glyph => glyph.Cropping).ToList(),
            glyphs.Select(glyph => glyph.Character).ToList(),
            primary.LineSpacing,
            primary.Spacing,
            glyphs.Select(glyph => new Vector3(glyph.LeftSideBearing, glyph.Width, glyph.RightSideBearing)).ToList(),
            primary.DefaultCharacter
        );
    }

    private static byte[] ReadAlphaPixels(Texture2D texture)
    {
        if (texture.Format == SurfaceFormat.Alpha8)
        {
            var pixels = new byte[texture.Width * texture.Height];
            texture.GetData(pixels);
            return pixels;
        }

        var colorPixels = ReadColorPixels(texture);
        var alphaPixels = new byte[colorPixels.Length];
        for (var i = 0; i < colorPixels.Length; i++)
        {
            alphaPixels[i] = colorPixels[i].A;
        }

        return alphaPixels;
    }

    private static Color[] ReadColorPixels(GraphicsDevice graphicsDevice, Texture2D texture)
    {
        if (texture.Format is SurfaceFormat.Alpha8 or SurfaceFormat.Color)
        {
            return ReadColorPixels(texture);
        }

        return ReadColorPixelsWithGpu(graphicsDevice, texture);
    }

    private static Color[] ReadColorPixelsWithGpu(GraphicsDevice graphicsDevice, Texture2D texture)
    {
        var previousRenderTargets = graphicsDevice.GetRenderTargets();
        var previousViewport = graphicsDevice.Viewport;
        using var renderTarget = new RenderTarget2D(
            graphicsDevice,
            texture.Width,
            texture.Height,
            false,
            SurfaceFormat.Color,
            DepthFormat.None
        );

        try
        {
            graphicsDevice.SetRenderTarget(renderTarget);
            graphicsDevice.Clear(Color.Transparent);

            using var spriteBatch = new SpriteBatch(graphicsDevice);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp);
            spriteBatch.Draw(
                texture,
                new Rectangle(0, 0, texture.Width, texture.Height),
                new Rectangle(0, 0, texture.Width, texture.Height),
                Color.White
            );
            spriteBatch.End();
        }
        finally
        {
            graphicsDevice.SetRenderTargets(previousRenderTargets);
            graphicsDevice.Viewport = previousViewport;
        }

        var pixels = new Color[texture.Width * texture.Height];
        renderTarget.GetData(pixels);
        return pixels;
    }

    private static Color[] ReadColorPixels(Texture2D texture)
    {
        if (texture.Format == SurfaceFormat.Alpha8)
        {
            var alphaPixels = ReadAlphaPixels(texture);
            var colorPixels = new Color[alphaPixels.Length];
            for (var i = 0; i < alphaPixels.Length; i++)
            {
                var alpha = alphaPixels[i];
                colorPixels[i] = new Color(alpha, alpha, alpha, alpha);
            }

            return colorPixels;
        }

        var pixels = new Color[texture.Width * texture.Height];
        texture.GetData(pixels);
        return pixels;
    }

    private static int ScalePixelLength(int pixelLength, float scale)
    {
        return Math.Max(1, (int)MathF.Ceiling(pixelLength * scale));
    }

    private static int ScaleSignedPixelLength(int pixelLength, float scale)
    {
        return (int)MathF.Round(pixelLength * scale);
    }

    private static void CopyPixels<T>(
        T[] sourcePixels,
        int sourceWidth,
        T[] targetPixels,
        int targetWidth,
        Rectangle sourceArea,
        int targetX,
        int targetY
    )
    {
        for (var y = 0; y < sourceArea.Height; y++)
        {
            var sourceOffset = (sourceArea.Y + y) * sourceWidth + sourceArea.X;
            var targetOffset = (targetY + y) * targetWidth + targetX;
            Array.Copy(sourcePixels, sourceOffset, targetPixels, targetOffset, sourceArea.Width);
        }
    }
}

internal sealed record SpriteFontMergeResult(
    SpriteFont Font,
    string AddedCharacters,
    string AlreadyPresentCharacters,
    string UnavailableCharacters,
    float FallbackGlyphScale,
    int FallbackGlyphYOffset
);

internal sealed record GlyphInkMeasurement(
    Dictionary<char, GlyphInkBounds> Primary,
    Dictionary<char, GlyphInkBounds> Fallback
)
{
    public static GlyphInkMeasurement Empty { get; } = new(new Dictionary<char, GlyphInkBounds>(), new Dictionary<char, GlyphInkBounds>());
}

internal readonly record struct GlyphInkBounds(int MinX, int MinY, int MaxX, int MaxY)
{
    public int Height => MaxY - MinY + 1;
    public float CenterY => (MinY + MaxY + 1) / 2f;
}
