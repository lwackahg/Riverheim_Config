using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;

namespace RiverheimConfig
{
    // Makes Expand World Size's "Stretch world" work fully with Riverheim.
    //
    // EWS stretches a world by dividing the coordinates passed to WorldGenerator.GetBiome and GetBiomeHeight by its
    // stretch. Riverheim answers both from its own maps, so its biomes and terrain stretch along. Riverheim also
    // answers WorldGenerator.GetForestFactor from its own forest map, but EWS leaves that call alone (vanilla's forest
    // noise needs no stretching). In a stretched Riverheim world the game therefore reads forest density from the
    // wrong spot everywhere, and past Riverheim's own radius (about 10.5 km before stretching) from outside its map:
    // trees, grass and locations that want or avoid forest go missing there. Vegetation, clutter, location placement
    // and the minimap all ask GetForestFactor.
    //
    // This patch divides the position by the same stretch just before Riverheim's patch reads it. The minimap's call
    // is left alone because EWS already stretches the minimap's coordinates before that call.
    internal static class ExpandWorldSizeCompatibility
    {
        private static Func<float> worldStretch;
        private static Func<WorldGenerator, object> riverheimRenderer;

        [ThreadStatic]
        private static bool inMinimapMask;

        public static void Apply(Harmony harmony, ManualLogSource log)
        {
            // ExpandWorldSize.WorldInfo.WorldStretch is the value EWS publishes for other mods.
            Type worldInfo = AccessTools.TypeByName("ExpandWorldSize.WorldInfo");
            FieldInfo stretchField = worldInfo == null ? null : AccessTools.Field(worldInfo, "WorldStretch");
            if (stretchField == null || !stretchField.IsStatic || stretchField.FieldType != typeof(float))
            {
                return;
            }

            // Riverheim keeps a renderer for each WorldGenerator of a Riverheim world; vanilla worlds have none.
            MethodInfo getRenderer = AccessTools.Method("Riverheim.Plugin.WorldGeneratorExtensions:GetRenderer");
            MethodInfo forestFactor = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.GetForestFactor));
            MethodInfo maskColor = AccessTools.Method(typeof(Minimap), "GetMaskColor");
            if (getRenderer == null || forestFactor == null || maskColor == null)
            {
                log.LogWarning("Expand World Size is installed, but this Riverheim or Valheim version does not have the methods the stretch fix needs; forests in stretched worlds may not line up with the terrain.");
                return;
            }

            worldStretch = Expression.Lambda<Func<float>>(Expression.Field(null, stretchField)).Compile();
            ParameterExpression generator = Expression.Parameter(typeof(WorldGenerator), "generator");
            riverheimRenderer = Expression.Lambda<Func<WorldGenerator, object>>(
                Expression.Convert(Expression.Call(getRenderer, generator), typeof(object)), generator).Compile();

            harmony.Patch(forestFactor, prefix: new HarmonyMethod(typeof(ExpandWorldSizeCompatibility), nameof(StretchForestPosition)) { priority = Priority.First });
            harmony.Patch(maskColor,
                prefix: new HarmonyMethod(typeof(ExpandWorldSizeCompatibility), nameof(EnterMinimapMask)),
                finalizer: new HarmonyMethod(typeof(ExpandWorldSizeCompatibility), nameof(LeaveMinimapMask)));
            log.LogInfo("Expand World Size found: in stretched Riverheim worlds, forests, vegetation and forest-bound locations follow the stretch.");
        }

        private static void StretchForestPosition(ref Vector3 pos)
        {
            if (inMinimapMask)
            {
                return;
            }
            float stretch = worldStretch();
            if (stretch == 1f || stretch <= 0f)
            {
                return;
            }
            WorldGenerator generator = WorldGenerator.instance;
            if (generator == null || riverheimRenderer(generator) == null)
            {
                return;
            }
            pos.x /= stretch;
            pos.z /= stretch;
        }

        private static void EnterMinimapMask()
        {
            inMinimapMask = true;
        }

        private static void LeaveMinimapMask()
        {
            inMinimapMask = false;
        }
    }
}
