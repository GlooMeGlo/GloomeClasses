using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.ServerMods;

namespace GloomeClasses.src.Diagnostics
{
	/// <summary>
	/// centralized diagnostic logging system for GloomeClasses.
	/// </summary>
	public static class DiagnosticLogger {
		private static ILogger logger;
		private static ICoreAPI api;
		private static bool initialized = false;
		private static readonly List<string> sessionLog = [];

		public enum LogCategory {
			RecipeLoading,
			TraitSystem,
			ModCompatibility,
			RuntimeError,
			EnvironmentDetection,
			CharacterSystem
		}

		public static void Initialize(ICoreAPI coreApi, ILogger coreLogger) {
			api = coreApi;
			logger = coreLogger;
			initialized = true;

			logger.Notification("[GloomeClasses] Diagnostic logging system initialized");
			LogEntry(LogCategory.ModCompatibility, "Session started");
		}

		private static void LogEntry(LogCategory category, string message) {
			if (!initialized) return;

			string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
			string entry = $"[{timestamp}] [{category}] {message}";
			sessionLog.Add(entry);

			// keep session log at reasonable size
			if (sessionLog.Count > 1000) {
				sessionLog.RemoveRange(0, 200); // Remove oldest 200 entries
			}
		}

		public static void LogModLoadOrder(ICoreAPI coreApi) {
			if (!initialized) return;

			try {
				logger.Notification("[GloomeClasses] === Mod Load Order Analysis ===");

				var allMods = coreApi.ModLoader.Mods;
				var modList = new StringBuilder();

				foreach (var mod in allMods) {
					modList.AppendLine($"  - {mod.Info.ModID} v{mod.Info.Version} (type: {mod.Info.Type})");
				}

				logger.Debug("[GloomeClasses] Loaded mods:\n{0}", modList.ToString());
				LogEntry(LogCategory.ModCompatibility, $"Total mods loaded: {allMods.Count()}");

			} catch (Exception ex) {
				logger.Error("[GloomeClasses] Error logging mod load order: {0}", ex.Message);
			}
		}

		public static void LogCharacterSystemState(ICoreAPI coreApi) {
			if (!initialized) return;

			try {
				logger.Notification("[GloomeClasses] === Character System State ===");

				// use reflection to get CharacterSystem - it's an internal type
				var characterSystemType = AppDomain.CurrentDomain.GetAssemblies()
					.FirstOrDefault(a => a.GetName().Name == "VSSurvivalMod")?
					.GetType("Vintagestory.ServerMods.CharacterSystem");

				if (characterSystemType == null) {
					logger.VerboseDebug("[GloomeClasses] CharacterSystem type not found (expected for client-only)");
					return;
				}

				// get the actual mod system instance using reflection on ModLoader
				var modLoaderType = coreApi.ModLoader.GetType();
				var getModSystemMethod = modLoaderType.GetMethod("GetModSystem", []);
				var getModSystemGeneric = modLoaderType.GetMethod("GetModSystem", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

				// use the generic GetModSystem<T> method via reflection
				var genericMethod = modLoaderType.GetMethods()
					.FirstOrDefault(m => m.Name == "GetModSystem" && m.IsGenericMethod);
				if (genericMethod == null) return;

				var typedMethod = genericMethod.MakeGenericMethod(characterSystemType);
				var charSys = typedMethod.Invoke(coreApi.ModLoader, null);
				if (charSys == null) {
					logger.Warning("[GloomeClasses] CharacterSystem not found!");
					LogEntry(LogCategory.CharacterSystem, "CharacterSystem is NULL");
					return;
				}

				// get characterClasses property via reflection
				var characterClassesProperty = characterSystemType.GetProperty("characterClasses",
					System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
				if (characterClassesProperty == null) {
					logger.Warning("[GloomeClasses] characterClasses property not found!");
					return;
				}

                if (characterClassesProperty.GetValue(charSys) is not Array characterClasses)
                {
                    logger.Warning("[GloomeClasses] characterClasses array is NULL!");
                    LogEntry(LogCategory.CharacterSystem, "characterClasses array is NULL");
                    return;
                }

                logger.Notification("[GloomeClasses] Total character classes: {0}", characterClasses.Length);
				LogEntry(LogCategory.CharacterSystem, $"Total classes: {characterClasses.Length}");

				// find GloomeClasses characters using reflection
				int glooCount = 0;
				foreach (var item in characterClasses) {
                    var traitsField = item?.GetType().GetProperty("Traits")?.GetValue(item) as string[];

                    if (item?.GetType().GetProperty("Code")?.GetValue(item) is string codeField && codeField.ToLower().Contains("gloo")) {
						glooCount++;
						var traits = traitsField != null ? string.Join(", ", traitsField) : "none";
						logger.Debug("[GloomeClasses]   Class: {0}", codeField);
						logger.Debug("[GloomeClasses]     Traits: {0}", traits);
						LogEntry(LogCategory.CharacterSystem, $"Class {codeField}: {traitsField?.Length ?? 0} traits");
					}
				}

				logger.Notification("[GloomeClasses] GloomeClasses character classes: {0}", glooCount);

			} catch (Exception ex) {
				logger.Error("[GloomeClasses] Error logging character system state: {0}", ex.Message);
				LogEntry(LogCategory.RuntimeError, $"CharacterSystem state error: {ex.Message}");
			}
		}

		public static void LogThirdPartyModPresence(ICoreAPI coreApi) {
			if (!initialized) return;

			try {
				logger.Notification("[GloomeClasses] === Third-Party Mod Detection ===");

				var mods = coreApi.ModLoader.Mods;

				// check for RacialEquality
				var racialEquality = mods.FirstOrDefault(m => m.Info.ModID == "racialequality");
				if (racialEquality != null) {
					logger.Notification("[GloomeClasses] ⚠ RacialEquality detected: v{0}", racialEquality.Info.Version);
					logger.Notification("[GloomeClasses]   Many reported issues involve this mod");
					logger.Notification("[GloomeClasses]   Extra logging enabled for compatibility debugging");
					LogEntry(LogCategory.ModCompatibility, $"RacialEquality v{racialEquality.Info.Version} detected");
				} else {
					logger.Debug("[GloomeClasses] RacialEquality not detected");
				}

				// check for GloomeRaces
				var gloomeRaces = mods.FirstOrDefault(m => m.Info.ModID == "gloomeraces");
				if (gloomeRaces != null) {
					logger.Notification("[GloomeClasses] ⚠ GloomeRaces detected: v{0}", gloomeRaces.Info.Version);
					logger.Notification("[GloomeClasses]   This mod patches GloomeClasses for RacialEquality");
					LogEntry(LogCategory.ModCompatibility, $"GloomeRaces v{gloomeRaces.Info.Version} detected");
				} else {
					logger.Debug("[GloomeClasses] GloomeRaces not detected");
				}

				// check for other character/class mods
				var classMods = mods.Where(m =>
					m.Info.ModID.ToLower().Contains("class") ||
					m.Info.ModID.ToLower().Contains("race") ||
					m.Info.ModID.ToLower().Contains("character")
				).ToList();

				if (classMods.Count > 0) {
					logger.Debug("[GloomeClasses] Other character/class mods detected:");
					foreach (var mod in classMods) {
						logger.Debug("[GloomeClasses]   - {0} v{1}", mod.Info.ModID, mod.Info.Version);
						LogEntry(LogCategory.ModCompatibility, $"Detected: {mod.Info.ModID} v{mod.Info.Version}");
					}
				}

			} catch (Exception ex) {
				logger.Error("[GloomeClasses] Error detecting third-party mods: {0}", ex.Message);
			}
		}

		public static void LogPlayerEnvironment(EntityPlayer player, string context) {
			if (!initialized || player == null) return;

			try {
				var pos = player.Pos.AsBlockPos;
				var world = player.World;

				logger.Debug("[GloomeClasses] Player environment ({0}):", context);
				logger.Debug("[GloomeClasses]   Position: {0}, {1}, {2}", pos.X, pos.Y, pos.Z);
				logger.Debug("[GloomeClasses]   Dimension: {0}", player.Pos.Dimension);

				int sunlight = world.BlockAccessor.GetLightLevel(pos, EnumLightLevelType.OnlySunLight);
				int totalLight = world.BlockAccessor.GetLightLevel(pos, EnumLightLevelType.MaxLight);

				logger.Debug("[GloomeClasses]   Sunlight: {0}, Total light: {1}", sunlight, totalLight);

				LogEntry(LogCategory.EnvironmentDetection, $"{context}: pos=({pos.X},{pos.Y},{pos.Z}), sun={sunlight}");

			} catch (Exception ex) {
				logger.Warning("[GloomeClasses] Error logging player environment: {0}", ex.Message);
			}
		}

		public static void LogRecipeLoadingState(ICoreAPI coreApi) {
			if (!initialized) return;

			try {
				logger.Notification("[GloomeClasses] === Recipe Loading State ===");
				logger.Debug("[GloomeClasses] Recipe validation will occur after all recipes load");
				logger.Debug("[GloomeClasses] Check logs for recipe analysis report");

				LogEntry(LogCategory.RecipeLoading, "Recipe loading state logged");

			} catch (Exception ex) {
				logger.Error("[GloomeClasses] Error logging recipe state: {0}", ex.Message);
			}
		}

		public static void LogCharselCommandNote() {
			if (!initialized) return;

			try {
				logger.Notification("[GloomeClasses] === Character Selection System ===");
				logger.Notification("[GloomeClasses] .charsel command is available for testing");
				logger.Debug("[GloomeClasses] Use .charsel to change character class in-game if you have the permission!");

				LogEntry(LogCategory.CharacterSystem, "Character selection diagnostics initialized");

			} catch (Exception ex) {
				logger.Error("[GloomeClasses] Error logging charsel command note: {0}", ex.Message);
			}
		}

		public static void LogAvailableCharacterClasses(ICoreAPI coreApi) {
			if (!initialized) return;

			try {
				logger.Notification("[GloomeClasses] === Detecting Available Character Classes ===");

				// use reflection to get CharacterSystem
				var characterSystemType = AppDomain.CurrentDomain.GetAssemblies()
					.FirstOrDefault(a => a.GetName().Name == "VSSurvivalMod")?
					.GetType("Vintagestory.ServerMods.CharacterSystem");

				if (characterSystemType == null) {
					logger.VerboseDebug("[GloomeClasses] CharacterSystem type not found (client-only or not loaded yet)");
					return;
				}

				// get the CharacterSystem instance
				var modLoaderType = coreApi.ModLoader.GetType();
				var genericMethod = modLoaderType.GetMethods()
					.FirstOrDefault(m => m.Name == "GetModSystem" && m.IsGenericMethod);
				if (genericMethod == null) return;

				var typedMethod = genericMethod.MakeGenericMethod(characterSystemType);
				var charSys = typedMethod.Invoke(coreApi.ModLoader, null);
				if (charSys == null) {
					logger.Warning("[GloomeClasses] CharacterSystem instance not found!");
					return;
				}

				// get characterClasses array
				var characterClassesProperty = characterSystemType.GetProperty("characterClasses",
					System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
				if (characterClassesProperty == null) {
					logger.Warning("[GloomeClasses] characterClasses property not found!");
					return;
				}

				if (characterClassesProperty.GetValue(charSys) is not Array characterClasses) {
					logger.Warning("[GloomeClasses] characterClasses array is NULL!");
					return;
				}

				logger.Notification("[GloomeClasses] Total character classes detected: {0}", characterClasses.Length);

				// categorize classes
				int vanillaCount = 0;
				int glooCount = 0;
				int otherModCount = 0;

				var vanillaClasses = new List<string>();
				var glooClasses = new List<string>();
				var otherModClasses = new List<string>();

				foreach (var item in characterClasses) {
					if (item?.GetType().GetProperty("Code")?.GetValue(item) is not string code) continue;

					// detect vanilla classes (commoner, malefactor, etc)
					if (code.StartsWith("commoner") || code.StartsWith("malefactor") ||
					    code.StartsWith("hunter") || code.StartsWith("tailor") || code.StartsWith("smith") ||
					    code.StartsWith("blackguard") || code.StartsWith("clockmaker")) {
						vanillaCount++;
						vanillaClasses.Add(code);
					}
					// detect GloomeClasses
					else if (code.ToLower().Contains("gloo")) {
						glooCount++;
						glooClasses.Add(code);
					}
					// other mods
					else {
						otherModCount++;
						otherModClasses.Add(code);
					}
				}

				logger.Notification("[GloomeClasses] Vanilla classes: {0}", vanillaCount);
				foreach (var className in vanillaClasses) {
					logger.Debug("[GloomeClasses]   - {0}", className);
				}

				logger.Notification("[GloomeClasses] GloomeClasses: {0}", glooCount);
				foreach (var className in glooClasses) {
					logger.Debug("[GloomeClasses]   - {0}", className);
				}

				if (otherModCount > 0) {
					logger.Notification("[GloomeClasses] Other mod classes: {0}", otherModCount);
					foreach (var className in otherModClasses) {
						logger.Debug("[GloomeClasses]   - {0}", className);
					}
				}

				LogEntry(LogCategory.CharacterSystem, $"Classes detected: {vanillaCount} vanilla, {glooCount} GloomeClasses, {otherModCount} other");

			} catch (Exception ex) {
				logger.Error("[GloomeClasses] Error detecting character classes: {0}", ex.Message);
				LogEntry(LogCategory.RuntimeError, $"Character class detection error: {ex.Message}");
			}
		}

		public static void LogCharselAttempt(string className, string playerName) {
			if (!initialized) return;

			try {
				logger.Notification("[GloomeClasses] .charsel attempt by player '{0}' for class '{1}'", playerName, className);
				LogEntry(LogCategory.CharacterSystem, $"Player {playerName} attempting .charsel {className}");

			} catch (Exception ex) {
				logger.Error("[GloomeClasses] Error logging charsel attempt: {0}", ex.Message);
			}
		}

		public static void LogCharselSuccess(string className, string playerName) {
			if (!initialized) return;

			try {
				logger.Notification("[GloomeClasses] ✓ .charsel SUCCESS for player '{0}' -> class '{1}'", playerName, className);
				LogEntry(LogCategory.CharacterSystem, $"SUCCESS: {playerName} changed to {className}");

			} catch (Exception ex) {
				logger.Error("[GloomeClasses] Error logging charsel success: {0}", ex.Message);
			}
		}

		public static void LogCharselFailure(string className, string playerName, string reason) {
			if (!initialized) return;

			try {
				logger.Error("[GloomeClasses] ✗ .charsel FAILED for player '{0}' -> class '{1}'", playerName, className);
				logger.Error("[GloomeClasses]   Reason: {0}", reason);
				LogEntry(LogCategory.RuntimeError, $"FAILED: {playerName} -> {className} | Reason: {reason}");

			} catch (Exception ex) {
				logger.Error("[GloomeClasses] Error logging charsel failure: {0}", ex.Message);
			}
		}

		public static void GenerateSessionReport() {
			if (!initialized) return;

			try {
				logger.Notification("[GloomeClasses] === Session Diagnostic Summary ===");

				var categoryCounts = sessionLog
					.GroupBy(entry => {
						var parts = entry.Split(['[', ']'], StringSplitOptions.RemoveEmptyEntries);
						return parts.Length > 1 ? parts[1] : "Unknown";
					})
					.ToDictionary(g => g.Key, g => g.Count());

				logger.Notification("[GloomeClasses] Session log entries: {0}", sessionLog.Count);
				foreach (var kvp in categoryCounts.OrderByDescending(x => x.Value)) {
					logger.Debug("[GloomeClasses]   {0}: {1} entries", kvp.Key, kvp.Value);
				}

				// log recent errors if any
				var recentErrors = sessionLog.Where(e => e.Contains("RuntimeError")).Take(5).ToList();
				if (recentErrors.Count > 0) {
					logger.Warning("[GloomeClasses] Recent errors detected:");
					foreach (var error in recentErrors) {
						logger.Warning("[GloomeClasses]   {0}", error);
					}
				}

			} catch (Exception ex) {
				logger.Error("[GloomeClasses] Error generating session report: {0}", ex.Message);
			}
		}

		public static void Log(LogCategory category, string message, params object[] args) {
			if (!initialized) return;

			try {
				string formatted = args.Length > 0 ? string.Format(message, args) : message;
				logger.Debug("[GloomeClasses] [{0}] {1}", category, formatted);
				LogEntry(category, formatted);
			} catch (Exception ex) {
				logger.Error("[GloomeClasses] Error in diagnostic log: {0}", ex.Message);
			}
		}

		public static void LogWarning(LogCategory category, string message, params object[] args) {
			if (!initialized) return;

			try {
				string formatted = args.Length > 0 ? string.Format(message, args) : message;
				logger.Warning("[GloomeClasses] [{0}] {1}", category, formatted);
				LogEntry(category, "WARNING: " + formatted);
			} catch (Exception ex) {
				logger.Error("[GloomeClasses] Error in diagnostic warning: {0}", ex.Message);
			}
		}

		public static void LogError(LogCategory category, string message, Exception ex = null) {
			if (!initialized) return;

			try {
				logger.Error("[GloomeClasses] [{0}] {1}", category, message);
				if (ex != null) {
					logger.Error("[GloomeClasses] Exception: {0}", ex);
				}
				LogEntry(category, "ERROR: " + message + (ex != null ? $" | {ex.Message}" : ""));
			} catch {
				// silent fail cuz we're already in error handling
			}
		}
	}
}
