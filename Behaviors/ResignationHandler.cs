using System.Collections;
using Unity.Netcode;
using UnityEngine;
using WaitUntil = UnityEngine.WaitUntil;

namespace IQuit.Behaviors;

public class ResignationHandler : NetworkBehaviour
{
    private bool _fromSelf;
    private readonly NetworkVariable<bool> _allowOther = new(Plugin.GameConfig.AllowOthers.Value);
    private readonly NetworkVariable<bool> _fastReset = new(Plugin.GameConfig.FastReset.Value);
    private bool _isRunning;
    
    public void HandleResignation()
    {
        _fromSelf = true;
        PlayerQuitedServerRpc();
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void PlayerQuitedServerRpc()
    {
        if (_isRunning) return;
        Plugin.Log.LogDebug("[Event] PlayerQuitedServerRPC");
        ResetClientRpc(!StartOfRound.Instance.firingPlayersCutsceneRunning && (_allowOther.Value || _fromSelf));
        _fromSelf = false;
    }

    [ClientRpc]
    private void ResetClientRpc(bool granted)
    {
        StartCoroutine(ResetCoroutine(granted));
    }

    private IEnumerator ResetCoroutine(bool granted)
    {
        if (_isRunning) yield break;
        _isRunning = true;
        
        var terminal = FindObjectOfType<Terminal>();
        
        if (!_fastReset.Value || !StartOfRound.Instance.inShipPhase)
        {
            // Lock terminal screen for 2 seconds
            if (!_fastReset.Value)
            {
                terminal.screenText.ReleaseSelection();
                terminal.screenText.DeactivateInputField();
                yield return new WaitForSeconds(2);
                terminal.screenText.ActivateInputField();
                terminal.screenText.Select();
            }

            // Display text node if terminal is in use only
            if (terminal.terminalInUse)
            {
                var loadNode = ScriptableObject.CreateInstance<TerminalNode>();
                loadNode.clearPreviousText = true;
                loadNode.displayText = $"""
// <size=20><color={(granted ? "#0000ff" : "#ff0000")}>{(granted ? Locale.RequestGranted : Locale.RequestDenied)}</color></size> //
{(granted ? Locale.ReturnEquipment : Locale.HostOnly)}


""";
                terminal.LoadNewNode(loadNode);
            }

            if(!granted) yield break;
            if (!_fastReset.Value)
                yield return new WaitForSeconds(_fastReset.Value ? 0 : 5);
        }
        
        //terminal.QuitTerminal();
        
        // If you are in orbit, leave first
        if (!StartOfRound.Instance.inShipPhase)
        {
            StartOfRound.Instance.firingPlayersCutsceneRunning = true;
            StartOfRound.Instance.ShipLeaveAutomatically();
            yield return new WaitUntil(() => StartOfRound.Instance.inShipPhase);
            StartOfRound.Instance.firingPlayersCutsceneRunning = false;
        }

        // If the host chose the long route
        if (!_fastReset.Value)
        {
            if(IsServer)
                StartOfRound.Instance.ManuallyEjectPlayersServerRpc();
            yield return new WaitForSeconds(10f);
            _isRunning = false;
            yield break;
        }
        
        // Reset Terminal
        terminal.ClearBoughtItems();
        terminal.SetItemSales();
        
        // Reset game context
        GameNetworkManager.Instance.localPlayerController.inSpecialInteractAnimation = true;
        GameNetworkManager.Instance.localPlayerController.DropAllHeldItems();
        SoundManager.Instance.SetDiageticMixerSnapshot(3, 2f);
        StartOfRound.Instance.shipDoorAudioSource.Stop();
        StartOfRound.Instance.speakerAudioSource.Stop();
        if (IsServer)
            GameNetworkManager.Instance.ResetSavedGameValues();
        StartOfRound.Instance.ResetShip();
        yield return new WaitForSeconds(.1f);
        
        // Reset players position
        GameNetworkManager.Instance.localPlayerController.TeleportPlayer(
            StartOfRound.Instance.playerSpawnPositions[GameNetworkManager.Instance.localPlayerController.playerClientId].position);
        StartOfRound.Instance.currentPlanetPrefab.transform.position = StartOfRound.Instance.planetContainer.transform.position;
        yield return new WaitForSeconds(.1f);
        
        // Once everything is set up, the game will resume
        if (IsServer)
        {
            StartOfRound.Instance.playersRevived++;
            yield return new WaitUntil(() => StartOfRound.Instance.playersRevived >= GameNetworkManager.Instance.connectedPlayers);
            StartOfRound.Instance.playersRevived = 0;
            StartOfRound.Instance.EndPlayersFiredSequenceClientRpc();
        }
        else
        {
            StartOfRound.Instance.PlayerHasRevivedServerRpc();
        }

        _isRunning = false;
    }
}