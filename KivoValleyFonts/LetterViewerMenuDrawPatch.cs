using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Menus;

namespace KivoValleyFonts;

[HarmonyPatch(typeof(LetterViewerMenu))]
[HarmonyPatch("draw")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Local")]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public class LetterViewerMenuDrawPatch
{
    private const float OriginalDrawScale = 4f;
    private const float LetterBackgroundScaleMultiplier = 1.12f;
    private const float LetterBackgroundOffsetX = -4f;
    private const float LetterBackgroundOffsetY = -4f;
    private const float FloatTolerance = 0.001f;

    private static readonly MethodInfo SpriteBatchDrawMethod = AccessTools.Method(
        typeof(SpriteBatch),
        nameof(SpriteBatch.Draw),
        new[]
        {
            typeof(Texture2D),
            typeof(Vector2),
            typeof(Rectangle?),
            typeof(Color),
            typeof(float),
            typeof(Vector2),
            typeof(float),
            typeof(SpriteEffects),
            typeof(float)
        }
    );

    private static readonly MethodInfo AdjustLetterBackgroundPositionMethod = AccessTools.Method(
        typeof(LetterViewerMenuDrawPatch),
        nameof(AdjustLetterBackgroundPosition)
    );

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();
        var backgroundDrawIndex = codes.FindIndex(instruction => instruction.Calls(SpriteBatchDrawMethod));

        if (backgroundDrawIndex < 0)
        {
            return codes;
        }

        for (var i = backgroundDrawIndex - 1; i >= 0; i--)
        {
            if (codes[i].opcode == OpCodes.Ldc_R4 && codes[i].operand is float value && MathF.Abs(value - OriginalDrawScale) < FloatTolerance)
            {
                codes[i].operand = OriginalDrawScale * LetterBackgroundScaleMultiplier;
                continue;
            }

            if (codes[i].opcode == OpCodes.Newobj && codes[i].operand is ConstructorInfo constructor && constructor.DeclaringType == typeof(Vector2))
            {
                codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, AdjustLetterBackgroundPositionMethod));
                break;
            }
        }

        return codes;
    }

    private static Vector2 AdjustLetterBackgroundPosition(Vector2 position)
    {
        return new Vector2(position.X + LetterBackgroundOffsetX, position.Y + LetterBackgroundOffsetY);
    }
}
