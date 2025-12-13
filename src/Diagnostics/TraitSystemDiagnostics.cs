using HarmonyLib;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Vintagestory.API.Common;
using GloomeClasses.src;

namespace GloomeClasses.src.Diagnostics
{
	/// <summary>
	/// diagnostic patches to monitor CharacterSystem trait accessibility.
	/// should identify why other mods report they cannot "pull traits" from GloomeClasses.
	///
	/// NOTE: CharacterSystem and CharacterClass are internal types in VSSurvivalMod,
	/// so we use reflection-based patching via manual MethodInfo targeting.
	/// these patches are applied manually in ApplyTraitSystemPatches().
	/// </summary>
	public static class TraitSystemDiagnostics {

		private static ILogger Logger => GloomeClassesModSystem.Logger;
		private static int accessCount = 0;
		private static readonly object lockObj = new();

		public static void ApplyTraitSystemPatches(Harmony harmony) {
			if (harmony == null || Logger == null) return;

			// CharacterSystem is server-side only
			var api = GloomeClassesModSystem.Api;
			if (api != null && api.Side == EnumAppSide.Client) {
				Logger.VerboseDebug("[GloomeClasses] CharacterSystem patches skipped (client-side only)");
				return;
			}

			try {
				// find VSSurvivalMod assembly
				var vsSurvivalMod = AppDomain.CurrentDomain.GetAssemblies()
					.FirstOrDefault(a => a.GetName().Name == "VSSurvivalMod");

				if (vsSurvivalMod == null) {
					Logger.VerboseDebug("[GloomeClasses] VSSurvivalMod assembly not found (expected on client)");
					return;
				}

				// find CharacterSystem type
				var characterSystemType = vsSurvivalMod.GetType("Vintagestory.ServerMods.CharacterSystem");
				if (characterSystemType == null) {
					Logger.VerboseDebug("[GloomeClasses] CharacterSystem type not found (expected on client)");
					return;
				}

				// patch characterClasses property getter
				var characterClassesProperty = characterSystemType.GetProperty("characterClasses",
					BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				if (characterClassesProperty != null) {
					var getterMethod = characterClassesProperty.GetGetMethod(true);
					if (getterMethod != null) {
						harmony.Patch(getterMethod,
							postfix: new HarmonyMethod(typeof(TraitSystemDiagnostics).GetMethod(nameof(CharacterClassesGetter_Postfix),
								BindingFlags.Public | BindingFlags.Static)));
						Logger.VerboseDebug("[GloomeClasses]   ✓ Patched CharacterSystem.characterClasses getter");
					}
				}

				// patch LoadCharacterClasses method
				var loadMethod = characterSystemType.GetMethod("LoadCharacterClasses",
					BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				if (loadMethod != null) {
					harmony.Patch(loadMethod,
						postfix: new HarmonyMethod(typeof(TraitSystemDiagnostics).GetMethod(nameof(LoadCharacterClasses_Postfix),
							BindingFlags.Public | BindingFlags.Static)));
					Logger.VerboseDebug("[GloomeClasses]   ✓ Patched CharacterSystem.LoadCharacterClasses");
				}

				// patch GetClass method
				var getClassMethod = characterSystemType.GetMethod("GetClass",
					BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
					null, [typeof(string)], null);
				if (getClassMethod != null) {
					harmony.Patch(getClassMethod,
						prefix: new HarmonyMethod(typeof(TraitSystemDiagnostics).GetMethod(nameof(GetClass_Prefix),
							BindingFlags.Public | BindingFlags.Static)),
						postfix: new HarmonyMethod(typeof(TraitSystemDiagnostics).GetMethod(nameof(GetClass_Postfix),
							BindingFlags.Public | BindingFlags.Static)));
					Logger.VerboseDebug("[GloomeClasses]   ✓ Patched CharacterSystem.GetClass");
				}

			} catch (Exception ex) {
				Logger.Error("[GloomeClasses] Failed to apply trait system patches: {0}", ex.Message);
			}
		}

		public static void CharacterClassesGetter_Postfix(object __result) {
			if (Logger == null || __result == null) return;

			lock (lockObj) {
				accessCount++;

				// Only log every 10th access to avoid spam, but always log first 3
				if (accessCount <= 3 || accessCount % 10 == 0) {
					try {
						// Get calling mod via stack trace
						var stackTrace = new StackTrace();
						var callingMethod = stackTrace.GetFrame(2)?.GetMethod();
						var callingType = callingMethod?.DeclaringType;
						var callingAssembly = callingType?.Assembly.GetName().Name ?? "Unknown";

                        // __result is CharacterClass[] - use reflection to access
                        if (__result is not Array resultArray) return;

                        Logger.Debug("[GloomeClasses] CharacterSystem.characterClasses accessed (#{0})", accessCount);
						Logger.Debug("[GloomeClasses]   Called by: {0}", callingAssembly);
						Logger.Debug("[GloomeClasses]   Returned {0} character classes", resultArray.Length);

						// check if GloomeClasses traits are visible
						int glooCount = 0;
						foreach (var item in resultArray) {
                            if (item?.GetType().GetProperty("Code")?.GetValue(item) is string codeField && codeField.ToLower().Contains("gloo"))
                            {
                                glooCount++;
                            }
                            else if (item?.GetType().GetProperty("Traits")?.GetValue(item) is string[] traitsField && traitsField.Any(t => t.ToLower().Contains("gloo")))
                            {
                                glooCount++;
                            }
                        }

						if (glooCount > 0) {
							Logger.Debug("[GloomeClasses]   GloomeClasses visible: YES ({0} classes)", glooCount);
						} else {
							Logger.Warning("[GloomeClasses]   GloomeClasses visible: NO - other mods may not see our traits!");
						}

					} catch (Exception ex) {
						Logger.VerboseDebug("[GloomeClasses] Error in trait system diagnostic: {0}", ex.Message);
					}
				}
			}
		}

		/// <summary>
		/// log initial state when CharacterSystem loads character classes.
		/// </summary>
		public static void LoadCharacterClasses_Postfix(object __instance) {
			if (Logger == null) return;

			try {
				Logger.Notification("[GloomeClasses] CharacterSystem.LoadCharacterClasses completed");

				// get characterClasses property via reflection
				var characterClassesProperty = __instance.GetType().GetProperty("characterClasses",
					BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				if (characterClassesProperty == null) {
					Logger.Error("[GloomeClasses] Could not find characterClasses property!");
					return;
				}

                if (characterClassesProperty.GetValue(__instance) is not Array characterClasses)
                {
                    Logger.Error("[GloomeClasses] characterClasses is NULL after loading!");
                    return;
                }

                Logger.Notification("[GloomeClasses] Total classes loaded: {0}", characterClasses.Length);

				// check for GloomeClasses presence
				int glooCount = 0;
				foreach (var item in characterClasses) {
                    if (item?.GetType().GetProperty("Code")?.GetValue(item) is string codeField && codeField.ToLower().Contains("gloo"))
                    {
                        glooCount++;
                        var traitsField = item.GetType().GetProperty("Traits")?.GetValue(item) as string[];
                        var traitCount = traitsField?.Length ?? 0;

                        Logger.Debug("[GloomeClasses]   - {0}: {1} traits", codeField, traitCount);

                        if (traitsField != null)
                        {
                            foreach (var trait in traitsField)
                            {
                                Logger.VerboseDebug("[GloomeClasses]     * {0}", trait);
                            }
                        }
                    }
                }

				if (glooCount > 0) {
					Logger.Notification("[GloomeClasses] ✓ GloomeClasses character classes found: {0}", glooCount);
				} else {
					Logger.Warning("[GloomeClasses] ⚠ No GloomeClasses character classes found!");
					Logger.Warning("[GloomeClasses]   This indicates a load order or asset loading problem");
				}

			} catch (Exception ex) {
				Logger.Error("[GloomeClasses] Error in LoadCharacterClasses diagnostic: {0}", ex.Message);
			}
		}

		/// <summary>
		/// log when the character system is queried for a specific class code.
		/// </summary>
		public static void GetClass_Prefix(string classCode) {
			if (Logger == null || string.IsNullOrEmpty(classCode)) return;

			if (classCode.ToLower().Contains("gloo")) {
				Logger.Debug("[GloomeClasses] GetClass called for GloomeClasses class: {0}", classCode);
			}
		}

		public static void GetClass_Postfix(string classCode, object __result) {
			if (Logger == null || string.IsNullOrEmpty(classCode)) return;

			if (classCode.ToLower().Contains("gloo")) {
				if (__result != null) {
					var codeField = __result.GetType().GetProperty("Code")?.GetValue(__result) as string;
					var traitsField = __result.GetType().GetProperty("Traits")?.GetValue(__result) as string[];
					var traitCount = traitsField?.Length ?? 0;
					Logger.Debug("[GloomeClasses] GetClass returned: {0} ({1} traits)", codeField, traitCount);
				} else {
					Logger.Warning("[GloomeClasses] GetClass returned NULL for: {0}", classCode);
				}
			}
		}
	}
}
