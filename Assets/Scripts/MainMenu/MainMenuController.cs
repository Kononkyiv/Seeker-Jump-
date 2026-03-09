using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public void PlayGame()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void LoadLeaderboardScene()
    {
        SceneManager.LoadScene("LeaderbordScene");
    }

    public void LoadMarketScene()
    {
        SceneManager.LoadScene("MarketScene");
    }

    public void LoadSettingsScene()
    {
        SceneManager.LoadScene("SettingsScene");
    }

    public void LoadTournamentScene()
    {
        SceneManager.LoadScene("TournamentScene");
    }

    public void LoadWalletScene()
    {
        SceneManager.LoadScene("WalletScene");
    }
}