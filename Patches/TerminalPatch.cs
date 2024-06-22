using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HarmonyLib;
using IQuit.Behaviors;
using UnityEngine;

namespace IQuit.Patches;

[HarmonyPatch(typeof(Terminal))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal class TerminalPatch
{
    private static ResignationHandler? _resignationHandler;
    
    [HarmonyPatch("Awake")]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    private static void Awake(ref Terminal __instance)
    {
        var result = ScriptableObject.CreateInstance<TerminalNode>();
        result.clearPreviousText = true;
        result.terminalEvent = "iquit_goodbye";
        result.displayText = Locale.SendingResignation + "\n\n";
        
        var noun = ScriptableObject.CreateInstance<TerminalKeyword>();
        noun.isVerb = false;
        noun.word = "iquit";
        noun.specialKeywordResult = result;
        
        // That's how we inject our own command into the Terminal :o
        __instance.terminalNodes.allKeywords = __instance.terminalNodes.allKeywords.AddToArray(noun);
        
        // Attach our custom component to the Terminal (this is where the magic happens)
        _resignationHandler = __instance.gameObject.AddComponent<ResignationHandler>();

        // Patch HELP node text
        var helpNode = __instance.terminalNodes.specialNodes.Where(x => x.name == "HelpCommands").Select(x => x).FirstOrDefault();
        if (helpNode == null) return;
        
        helpNode.displayText = helpNode.displayText[..^23] + $@">IQUIT
{Locale.CommandDesc}

[numberOfItemsOnRoute]
";
    }

    [HarmonyPatch("RunTerminalEvents")]
    [HarmonyPostfix]
    private static void RunTerminalEvents(ref Terminal __instance, ref TerminalNode node)
    {
        if (node.terminalEvent == "iquit_goodbye")
            _resignationHandler?.HandleResignation();
    }
}