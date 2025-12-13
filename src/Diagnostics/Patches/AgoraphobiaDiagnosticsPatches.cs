using HarmonyLib;
using System;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using GloomeClasses.src;

namespace GloomeClasses.src.Diagnostics.Patches
{
	/// <summary>
	/// diagnostic patches for the Agoraphobia trait (Miner class Issue #2).
	/// logs detailed information about why Agoraphobia is/isn't triggering.
	/// </summary>
	[HarmonyPatch]
	[HarmonyPatchCategory(GloomeClassesModSystem.DiagnosticPatchCategory)]
	public class AgoraphobiaDiagnosticsPatches {

		private static ILogger Logger => GloomeClassesModSystem.Logger;
		private static int logInterval = 0;
		private const int LOG_EVERY_N_TICKS = 20; // log every 20 seconds (since trait ticks once per second)

		/// <summary>
		/// patch TemporalStabilityTraitBehavior.HandleTraits to log Agoraphobia detection.
		/// </summary>
		[HarmonyPostfix]
		[HarmonyPatch("GloomeClasses.src.EntityBehaviors.TemporalStabilityTraitBehavior", "HandleTraits")]
		public static void HandleTraits_Postfix(object __instance, float deltaTime) {
			if (Logger == null) return;

			// !! RoomRegistry is server-side only !!
			var api = GloomeClassesModSystem.Api;
			if (api != null && api.Side == EnumAppSide.Client) return;

			try {
				// use reflection to access private fields
				var instanceType = __instance.GetType();
				var entityField = instanceType.BaseType.GetField("entity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
				var hasAgoraphobiaField = instanceType.GetField("hasAgoraphobia", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
				var hasShelteredStoneField = instanceType.GetField("hasShelteredStone", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

				if (entityField == null || hasAgoraphobiaField == null) return;

                var hasAgoraphobia = (bool)hasAgoraphobiaField.GetValue(__instance);
                var hasShelteredStone = hasShelteredStoneField != null && (bool)hasShelteredStoneField.GetValue(__instance);

				if (entityField.GetValue(__instance) is not EntityPlayer entity || !hasAgoraphobia) return;

				// only log periodically to avoid spam
				logInterval++;
				if (logInterval < LOG_EVERY_N_TICKS) return;
				logInterval = 0;

				LogAgoraphobiaState(entity, hasShelteredStone);

			} catch (Exception ex) {
				// silently fail so we don't spam logs with reflection errors
				if (logInterval == 0) {
					Logger.VerboseDebug("[GloomeClasses] Agoraphobia diagnostic reflection failed: {0}", ex.Message);
				}
			}
		}

		private static void LogAgoraphobiaState(EntityPlayer player, bool hasShelteredStone) {
			try {
				var pos = player.Pos.AsBlockPos;
				var world = player.World;
				var api = player.Api;

				// grab environment info
				int sunlight = world.BlockAccessor.GetLightLevel(pos, EnumLightLevelType.OnlySunLight);
				int blockLight = world.BlockAccessor.GetLightLevel(pos, EnumLightLevelType.OnlyBlockLight);
				int totalLight = world.BlockAccessor.GetLightLevel(pos, EnumLightLevelType.MaxLight);

				// grab room info - RoomRegistry is internal type, use reflection
				var roomRegistryType = api.World.GetType().Assembly.GetType("Vintagestory.GameContent.RoomRegistry");
				object roomRegistry = null;
				if (roomRegistryType != null) {
					// use generic GetModSystem<T> via reflection
					var modLoaderType = api.ModLoader.GetType();
					var genericMethod = modLoaderType.GetMethods()
						.FirstOrDefault(m => m.Name == "GetModSystem" && m.IsGenericMethod);
					if (genericMethod != null) {
						var typedMethod = genericMethod.MakeGenericMethod(roomRegistryType);
						roomRegistry = typedMethod.Invoke(api.ModLoader, null);
					}
				}
				object room = null;
				if (roomRegistry != null) {
					var getRoomMethod = roomRegistryType.GetMethod("GetRoomForPosition");
					room = getRoomMethod?.Invoke(roomRegistry, [pos]);
				}

				// grab temporal stability
				var tempAffected = player.GetBehavior<EntityBehaviorTemporalStabilityAffected>();
				double tempStabVelocity = tempAffected != null ? tempAffected.TempStabChangeVelocity : 0;

				// grab player's surfaceStabilityLoss stat
				double surfaceLoss = player.Stats.GetBlended("surfaceStabilityLoss") - 1.0;

				Logger.Debug("[GloomeClasses] === Agoraphobia Debug ===");
				Logger.Debug("[GloomeClasses] Player: {0}", player.GetName());
				Logger.Debug("[GloomeClasses] Position: ({0}, {1}, {2}) Dimension: {3}", pos.X, pos.Y, pos.Z, player.Pos.Dimension);
				Logger.Debug("[GloomeClasses] Lighting: sun={0}, block={1}, total={2}", sunlight, blockLight, totalLight);
				Logger.Debug("[GloomeClasses] Has Sheltered Stone: {0}", hasShelteredStone);

				if (room != null) {
					// use reflection to access room properties
					var roomType = room.GetType();
					var exitCount = (int)(roomType.GetProperty("ExitCount")?.GetValue(room) ?? 0);
					var skylightCount = (int)(roomType.GetProperty("SkylightCount")?.GetValue(room) ?? 0);
					var nonSkylightCount = (int)(roomType.GetProperty("NonSkylightCount")?.GetValue(room) ?? 0);

					Logger.Debug("[GloomeClasses] Room: exits={0}, skylight={1}, non-skylight={2}",
						exitCount, skylightCount, nonSkylightCount);

					// determine if Agoraphobia SHOULD trigger based on code logic
					bool shouldTrigger = exitCount > 0 || skylightCount >= nonSkylightCount;
					Logger.Debug("[GloomeClasses] Room condition: exits > 0 OR skylight >= non-skylight = {0}", shouldTrigger);

					if (shouldTrigger) {
						Logger.Debug("[GloomeClasses] → Agoraphobia SHOULD be active (on surface/open room)");
					} else {
						Logger.Debug("[GloomeClasses] → Agoraphobia should NOT be active (enclosed room)");
					}
				} else {
					Logger.Debug("[GloomeClasses] Room: NULL (not in a room)");
					Logger.Debug("[GloomeClasses] → Agoraphobia SHOULD be active (no room = surface)");
				}

				Logger.Debug("[GloomeClasses] Current temp stab velocity: {0:F6}", tempStabVelocity);
				Logger.Debug("[GloomeClasses] Surface loss stat: {0:F6}", surfaceLoss);

				// check if ShelteredStone would override
				if (hasShelteredStone && sunlight < 5) {
					Logger.Debug("[GloomeClasses] !!! ShelteredStone is active (sunlight < 5)");
					Logger.Debug("[GloomeClasses]   This takes priority over Agoraphobia!");
				}

				// expected behavior from trait description
				Logger.Debug("[GloomeClasses] Expected: No temp loss underground, loss on surface");

				// actual behavior analysis
				if (tempStabVelocity < 0) {
					Logger.Warning("[GloomeClasses] !!! LOSING temp stability (velocity={0:F6})", tempStabVelocity);
					if (room != null) {
						var roomType = room.GetType();
						var exitCount = (int)(roomType.GetProperty("ExitCount")?.GetValue(room) ?? 0);
						var skylightCount = (int)(roomType.GetProperty("SkylightCount")?.GetValue(room) ?? 0);
						var nonSkylightCount = (int)(roomType.GetProperty("NonSkylightCount")?.GetValue(room) ?? 0);

						if (exitCount == 0 && skylightCount < nonSkylightCount) {
							Logger.Warning("[GloomeClasses] !!! BUG: Losing stability in enclosed room!");
						}
					}
					if (sunlight < 5) {
						Logger.Warning("[GloomeClasses] !!! BUG: Losing stability underground (sunlight < 5)!");
					}
				} else if (tempStabVelocity > 0) {
					Logger.Debug("[GloomeClasses] :) GAINING temp stability (velocity={0:F6})", tempStabVelocity);
				} else {
					Logger.Debug("[GloomeClasses] = STABLE temp stability (velocity=0)");
				}

			} catch (Exception ex) {
				Logger.Error("[GloomeClasses] Error in Agoraphobia diagnostic logging: {0}", ex.Message);
			}
		}
	}
}
