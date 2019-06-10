using Harmony;
using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection.Emit;
using System;
using System.Reflection;

namespace RandomPlus
{
	[HarmonyPatch (typeof(Page_ConfigureStartingPawns), "RandomizeCurPawn")]
	class Patch_RandomizeMethod
	{
		static void Prefix ()
		{
			RandomSettings.ResetRerollCounter ();
		}

		static IEnumerable<CodeInstruction> Transpiler (IEnumerable<CodeInstruction> instructions)
		{
			var curPawnFieldInfo = typeof(Page_ConfigureStartingPawns)
				.GetField ("curPawn", BindingFlags.NonPublic | BindingFlags.Instance);
			var randomizeInPlaceMethodInfo = typeof(StartingPawnUtility)
				.GetMethod ("RandomizeInPlace", BindingFlags.Public | BindingFlags.Static);
			var checkPawnIsSatisfiedMethodInfo = typeof(RandomSettings)
				.GetMethod ("CheckPawnIsSatisfied", BindingFlags.Public | BindingFlags.Static);

			var codes = new List<CodeInstruction> (instructions);

			var appropriatePlace = 6;

			/* Removing the following code in its IL form */

//			this.curPawn = StartingPawnUtility.RandomizeInPlace (this.curPawn);

			codes.RemoveRange (appropriatePlace, 5);

			/* Adding the following code in its IL form: */

//			do {
//				this.curPawn = StartingPawnUtility.RandomizeInPlace (this.curPawn);
//			} while (!RandomSettings.CheckPawnIsSatisfied (this.curPawn));

			List <CodeInstruction> newCodes = new List<CodeInstruction> {
				new CodeInstruction (OpCodes.Ldarg_0),
				new CodeInstruction (OpCodes.Ldarg_0),
				new CodeInstruction (OpCodes.Ldfld, curPawnFieldInfo),
				new CodeInstruction (OpCodes.Call, randomizeInPlaceMethodInfo),
				new CodeInstruction (OpCodes.Stfld, curPawnFieldInfo),
				new CodeInstruction (OpCodes.Ldarg_0),
				new CodeInstruction (OpCodes.Ldfld, curPawnFieldInfo),
				new CodeInstruction (OpCodes.Call, checkPawnIsSatisfiedMethodInfo),
			};

			newCodes [0].labels.Add (new Label ());

			var ldargLabel = newCodes [0].labels [0];

			newCodes.Add (new CodeInstruction (OpCodes.Brfalse_S, ldargLabel));

			codes.InsertRange (appropriatePlace, newCodes);

			return codes;
		}
	}
}
