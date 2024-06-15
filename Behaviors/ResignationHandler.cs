using Unity.Netcode;

namespace IQuit.Behaviors;

public class ResignationHandler : NetworkBehaviour
{
    private bool _fromSelf = false;
    
    public void HandleResignation()
    {
        _fromSelf = true;
        PlayerQuittedServerRPC();
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void PlayerQuittedServerRPC()
    {
        if (!StartOfRound.Instance.inShipPhase || (!Plugin.GameConfig.AllowOthers.Value && !_fromSelf)) return;
        _fromSelf = false;
        Plugin.Log.LogDebug("[Event] PlayerQuittedServerRPC");
        int[] endGameStats =
        [
            StartOfRound.Instance.gameStats.daysSpent,
            StartOfRound.Instance.gameStats.scrapValueCollected,
            StartOfRound.Instance.gameStats.deaths,
            StartOfRound.Instance.gameStats.allStepsTaken
        ];
        StartOfRound.Instance.FirePlayersAfterDeadlineClientRpc(endGameStats);
    }
}