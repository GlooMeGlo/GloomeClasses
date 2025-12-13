using HarmonyLib;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using GloomeClasses.src;
using GloomeClasses.src.Diagnostics;

namespace GloomeClasses.src.Diagnostics.Patches
{
	[HarmonyPatch]
	public static class CharacterSystemDiagnosticPatches
	{
		private static ILogger Logger => GloomeClassesModSystem.Logger;

		[HarmonyPrefix]
		[HarmonyPatch(typeof(Vintagestory.GameContent.CharacterSystem), "setCharacterClass")]
		public static void Prefix_SetCharacterClass(
			EntityPlayer eplayer,
			string classCode,
			bool initializeGear)
		{
			try
			{
				string playerName = eplayer.GetName() ?? "Unknown";

				// log the attempt
				DiagnosticLogger.LogCharselAttempt(classCode, playerName);

				Logger.Debug("[GloomeClasses] setCharacterClass called:");
				Logger.Debug("[GloomeClasses]   Player: {0}", playerName);
				Logger.Debug("[GloomeClasses]   Target Class: {0}", classCode);
				Logger.Debug("[GloomeClasses]   Initialize Gear: {0}", initializeGear);

			}
			catch (Exception ex)
			{
				Logger.Error("[GloomeClasses] Error in setCharacterClass prefix patch: {0}", ex.Message);
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(Vintagestory.GameContent.CharacterSystem), "setCharacterClass")]
		public static void Postfix_SetCharacterClass(
			EntityPlayer eplayer,
			string classCode)
		{
			try
			{
				string playerName = eplayer.GetName() ?? "Unknown";
				string actualClass = eplayer.WatchedAttributes.GetString("characterClass");

				if (actualClass == classCode)
				{
					DiagnosticLogger.LogCharselSuccess(classCode, playerName);
				}
				else
				{
					DiagnosticLogger.LogCharselFailure(classCode, playerName,
						$"Class mismatch: expected '{classCode}', got '{actualClass}'");
				}

			}
			catch (Exception ex)
			{
				Logger.Error("[GloomeClasses] Error in setCharacterClass postfix patch: {0}", ex.Message);
			}
		}

		[HarmonyFinalizer]
		[HarmonyPatch(typeof(Vintagestory.GameContent.CharacterSystem), "setCharacterClass")]
		public static Exception Finalizer_SetCharacterClass(
			Exception __exception,
			EntityPlayer eplayer,
			string classCode)
		{
			if (__exception != null)
			{
				try
				{
					string playerName = eplayer?.GetName() ?? "Unknown";
					DiagnosticLogger.LogCharselFailure(classCode, playerName, __exception.Message);

					Logger.Error("[GloomeClasses] setCharacterClass threw exception:");
					Logger.Error("[GloomeClasses]   Player: {0}", playerName);
					Logger.Error("[GloomeClasses]   Class: {0}", classCode);
					Logger.Error("[GloomeClasses]   Exception: {0}", __exception);

				}
				catch (Exception ex)
				{
					Logger?.Error("[GloomeClasses] Error in setCharacterClass finalizer patch: {0}", ex.Message);
				}
			}

			// return the exception
			return __exception;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(Vintagestory.GameContent.CharacterSystem), "onCharacterSelection")]
		public static void Prefix_OnCharacterSelection(
			IServerPlayer fromPlayer,
			object p) // CharacterSelectionPacket
		{
			try
			{
				// use reflection to get the packet data since we can't reference the internal type
				var packetType = p.GetType();
				var didSelectProp = packetType.GetProperty("DidSelect");
				var characterClassProp = packetType.GetProperty("CharacterClass");

				if (didSelectProp != null && characterClassProp != null)
				{
					bool didSelect = (bool)didSelectProp.GetValue(p);
					string characterClass = (string)characterClassProp.GetValue(p);

					if (didSelect)
					{
						Logger.Notification("[GloomeClasses] Character selection packet received:");
						Logger.Notification("[GloomeClasses]   Player: {0}", fromPlayer.PlayerName);
						Logger.Notification("[GloomeClasses]   Selected Class: {0}", characterClass);
						Logger.Notification("[GloomeClasses]   Game Mode: {0}", fromPlayer.WorldData.CurrentGameMode);

						bool allowCharselOnce = fromPlayer.Entity.WatchedAttributes.GetBool("allowcharselonce");
						Logger.Debug("[GloomeClasses]   allowcharselonce: {0}", allowCharselOnce);
					}
				}

			}
			catch (Exception ex)
			{
				Logger.Error("[GloomeClasses] Error in onCharacterSelection prefix patch: {0}", ex.Message);
			}
		}
	}
}
