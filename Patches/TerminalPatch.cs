using HarmonyLib;
using IQuit.Behaviors;
using Unity.Netcode;
using UnityEngine;

namespace IQuit.Patches;

[HarmonyPatch(typeof(Terminal))]
internal class TerminalPatch
{
    private static ResignationHandler? _resignationHandler = null;
    
    [HarmonyPatch("Awake")]
    [HarmonyPrefix]
    private static void TerminalAwake(ref Terminal __instance)
    {
        var result = ScriptableObject.CreateInstance<TerminalNode>();
        result.clearPreviousText = true;
        result.terminalEvent = "iquit";
        result.displayText = "";
        
        var noun = ScriptableObject.CreateInstance<TerminalKeyword>();
        noun.isVerb = false;
        noun.word = "iquit";
        noun.specialKeywordResult = result;
        
        __instance.terminalNodes.allKeywords = __instance.terminalNodes.allKeywords.AddToArray(noun);
        _resignationHandler = __instance.gameObject.AddComponent<ResignationHandler>();
    }

    [HarmonyPatch("TextPostProcess")]
    [HarmonyPrefix]
    private static void TerminalTextPostProcess(ref Terminal __instance, ref string modifiedDisplayText, ref TerminalNode node)
    {
        if (node == null)
            return;

        if(node.name.ToLower().Contains("help"))
        {
            modifiedDisplayText = modifiedDisplayText[..^1];
            modifiedDisplayText += @">IQUIT
You don't feel like your crew is up to the mission? It's too late to quit! ... or is it?


";
        }
        else if (node.terminalEvent is "iquit")
        {
            if (!StartOfRound.Instance.inShipPhase)
            {
                modifiedDisplayText = @"

// <size=20><color=#ff0000>Request denied</color></size> //
Your resignation has been refused by the Company. Please leave the orbit of the moon before resigning.
It is for your safety and that of the local flora and fauna. Thank you.

";
            }
            else
            {
                modifiedDisplayText = @"

// <size=20><color=#ff0000>Request denied</color></size> //
Your resignation has been refused by the Company. Only the Chief Officer can resign for the entire crew.

";
                _resignationHandler?.HandleResignation();
            }
        }
    }
}