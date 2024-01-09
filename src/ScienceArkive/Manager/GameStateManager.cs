using KSP.Game;

namespace ScienceArkive.Manager;

public class GameStateManager
{
    public static GameStateManager Instance { get; } = new();

    private static readonly int[] InvalidStates =
    {
        (int)GameState.Flag,
        (int)GameState.MainMenu,
        (int)GameState.Loading,
        (int)GameState.WarmUpLoading,
        (int)GameState.Invalid
    };

    public bool IsInvalidState()
    {
        var gameState = GameManager.Instance.Game?.GlobalGameState?.GetGameState()?.GameState;
        return gameState == null || InvalidStates.Contains((int)gameState);
    }
}