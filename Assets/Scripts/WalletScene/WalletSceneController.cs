using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SolanaAuth;

/// <summary>
/// WalletScene UI: Connect (wallet + sign in) и Disconnect. Uses AuthService and WalletService.
/// </summary>
public class WalletSceneController : MonoBehaviour
{
    [Header("API")]
    [Tooltip("Base URL of the backend, e.g. http://localhost:3000")]
    public string apiBaseUrl = "http://localhost:3000";

    [Header("UI References")]
    public Button connectButton;
    public Button disconnectButton;
    public TextMeshProUGUI statusText;
    public GameObject loadingIndicator;

    [Header("Optional")]
    [Tooltip("Если нужна отдельная кнопка только Sign In (без connect) — привяжи сюда. Иначе оставь пустым.")]
    public Button signInButton;

    ApiClient _apiClient;
    WalletService _walletService;
    AuthService _authService;

    void Awake()
    {
        _apiClient = new ApiClient(apiBaseUrl, this);
        _walletService = new WalletService(this);
        _authService = new AuthService(_apiClient, _walletService);

        if (connectButton != null) connectButton.onClick.AddListener(OnConnectClicked);
        if (signInButton != null) signInButton.onClick.AddListener(OnSignInClicked);
        if (disconnectButton != null) disconnectButton.onClick.AddListener(OnDisconnectClicked);

        SetLoading(false);
        RefreshUI();
    }

    void RefreshUI()
    {
        bool hasTokens = TokenStorage.HasStoredTokens();
        bool walletConnected = _walletService.IsConnected;

        if (connectButton != null) connectButton.interactable = !hasTokens;
        if (signInButton != null) signInButton.gameObject.SetActive(walletConnected && !hasTokens);
        if (disconnectButton != null) disconnectButton.interactable = hasTokens || walletConnected;

        if (statusText != null)
        {
            if (hasTokens)
                statusText.text = "Signed in. Wallet: " + ShortAddress(TokenStorage.GetStoredWalletAddress());
            else
                statusText.text = "Tap Connect to sign in with wallet.";
        }
    }

    static string ShortAddress(string address)
    {
        if (string.IsNullOrEmpty(address) || address.Length < 12) return address ?? "";
        return address.Substring(0, 6) + "..." + address.Substring(address.Length - 4);
    }

    void OnConnectClicked()
    {
        SetLoading(true);
        SetStatus("Preparing…");
        _authService.GetMessageThenConnectAndLogin(result =>
        {
            SetLoading(false);
            if (result.Success)
            {
                SetStatus("Signed in.");
                RefreshUI();
            }
            else
                SetStatus(result.Error ?? "Failed");
        });
    }

    void OnSignInClicked()
    {
        SetLoading(true);
        SetStatus("Signing message...");
        _authService.SignInWithSolana(result =>
        {
            SetLoading(false);
            if (result.Success)
            {
                SetStatus("Signed in successfully.");
                RefreshUI();
            }
            else
                SetStatus("Sign in failed: " + (result.Error ?? "unknown"));
        });
    }

    void OnDisconnectClicked()
    {
        SetLoading(true);
        _authService.Logout(() =>
        {
            _walletService.Disconnect();
            SetLoading(false);
            SetStatus("Disconnected.");
            RefreshUI();
        });
    }

    void SetLoading(bool loading)
    {
        if (loadingIndicator != null) loadingIndicator.SetActive(loading);
        if (connectButton != null) connectButton.interactable = !loading;
        if (signInButton != null) signInButton.interactable = !loading;
        if (disconnectButton != null) disconnectButton.interactable = !loading;
    }

    void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }
}
