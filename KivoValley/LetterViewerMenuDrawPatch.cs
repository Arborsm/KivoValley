using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace KivoValley;

[HarmonyPatch(typeof(LetterViewerMenu))]
[HarmonyPatch("draw")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Local")]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public class LetterViewerMenuDrawPatch
{
    public static Texture2D? BorderTexture; 

    static void Postfix(LetterViewerMenu __instance, SpriteBatch b)
    {
        var backgroundWidth = 320 * 4f * __instance.scale;
        var backgroundHeight = 180 * 4f * __instance.scale;
        var backgroundPosition = new Vector2(
            __instance.xPositionOnScreen + __instance.width / 2f,
            __instance.yPositionOnScreen + __instance.height / 2f
        );
        
        DrawBorderAroundBackground(b, backgroundPosition, backgroundWidth, backgroundHeight, __instance);
    }

    private static void DrawBorderAroundBackground(SpriteBatch b, Vector2 center, float width, float height,
        LetterViewerMenu letter)
    {
        if (BorderTexture == null)
        {
            DrawSimpleBorder(b, center, width, height, letter);
            return;
        }
        
        var borderSize = 12f * letter.scale;
        
        var borderRect = new Rectangle(
            (int)(center.X - (width / 2 + borderSize)),
            (int)(center.Y - (height / 2 + borderSize)),
            (int)(width + borderSize * 2),
            (int)(height + borderSize * 2)
        );
        
        b.Draw(
            BorderTexture,
            borderRect,
            Color.White * 0.8f
        );
    }

    private static void DrawSimpleBorder(SpriteBatch b, Vector2 center, float width, float height, LetterViewerMenu letter)
    {
        var borderThickness = 8f * letter.scale;
        var borderColor = Color.Goldenrod * 0.9f;
        
        var borderRect = new Rectangle(
            (int)(center.X - width / 2 - borderThickness),
            (int)(center.Y - height / 2 - borderThickness),
            (int)(width + borderThickness * 2),
            (int)(height + borderThickness * 2)
        );
        
        var pixel = Game1.staminaRect;
        
        b.Draw(pixel, new Rectangle(borderRect.X, borderRect.Y, borderRect.Width, (int)borderThickness), borderColor);
        b.Draw(pixel,
            new Rectangle(borderRect.X, borderRect.Y + borderRect.Height - (int)borderThickness, borderRect.Width,
                (int)borderThickness), borderColor);
        b.Draw(pixel, new Rectangle(borderRect.X, borderRect.Y, (int)borderThickness, borderRect.Height), borderColor);
        b.Draw(pixel,
            new Rectangle(borderRect.X + borderRect.Width - (int)borderThickness, borderRect.Y, (int)borderThickness,
                borderRect.Height), borderColor);
        
        DrawBorderCorners(b, borderRect, borderColor, letter);
    }

    static void DrawBorderCorners(SpriteBatch b, Rectangle borderRect, Color color, LetterViewerMenu letter)
    {
        var cornerSize = 16f * letter.scale;
        var pixel = Game1.staminaRect;
        b.Draw(pixel, new Rectangle(borderRect.X, borderRect.Y, (int)cornerSize, 4), color);
        b.Draw(pixel, new Rectangle(borderRect.X, borderRect.Y, 4, (int)cornerSize), color);
        b.Draw(pixel, new Rectangle(borderRect.X + borderRect.Width - (int)cornerSize, borderRect.Y, (int)cornerSize, 4), color);
        b.Draw(pixel, new Rectangle(borderRect.X + borderRect.Width - 4, borderRect.Y, 4, (int)cornerSize), color);
        b.Draw(pixel, new Rectangle(borderRect.X, borderRect.Y + borderRect.Height - 4, (int)cornerSize, 4), color);
        b.Draw(pixel, new Rectangle(borderRect.X, borderRect.Y + borderRect.Height - (int)cornerSize, 4, (int)cornerSize), color);
        b.Draw(pixel,
            new Rectangle(borderRect.X + borderRect.Width - (int)cornerSize, borderRect.Y + borderRect.Height - 4,
                (int)cornerSize, 4), color);
        b.Draw(pixel,
            new Rectangle(borderRect.X + borderRect.Width - 4, borderRect.Y + borderRect.Height - (int)cornerSize, 4,
                (int)cornerSize), color);
    }
}