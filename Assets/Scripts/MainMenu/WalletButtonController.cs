using UnityEngine;
using UnityEngine.UI;
using SolanaAuth;

/// <summary>
/// Одна кнопка кошелька в нижнем меню: по клику — connect/sign in или disconnect.
/// Меняет иконку кнопки по статусу. Без сцены, спиннера и текста.
/// </summary>
public class WalletButtonController : MonoBehaviour
{
    [Header("API")]
    public string apiBaseUrl = "http://localhost:3000";

    [Header("Wallet Button")]
    public Button walletButton;
    [Tooltip("Сюда перетащи Image с иконкой (например дочерний объект Icon). Если пусто — будет использоваться фон кнопки.")]
    public Image iconImage;
    [Tooltip("Иконка, когда кошелёк не подключён (например wallet_no).")]
    public Sprite disconnectedSprite;
    [Tooltip("Иконка, когда кошелёк подключён (например wallet).")]
    public Sprite connectedSprite;

    ApiClient _apiClient;
    WalletService _walletService;
    AuthService _authService;
    Image _imageToChange;
    bool _isConnecting;
    bool _isDisconnecting;
    float _disconnectBlockUntil;

    void Awake()
    {
        _apiClient = new ApiClient(apiBaseUrl, this);
        _walletService = new WalletService(this);
        _authService = new AuthService(_apiClient, _walletService);

        if (walletButton != null)
        {
            _imageToChange = iconImage != null ? iconImage : (walletButton.targetGraphic as Image);
            if (_imageToChange == null) _imageToChange = walletButton.GetComponentInChildren<Image>();
            walletButton.onClick.AddListener(OnWalletButtonClick);
        }

        RefreshButtonLook();
    }

    void OnEnable()
    {
        RefreshButtonLook();
    }

    void Update()
    {
        if (_disconnectBlockUntil > 0f && Time.unscaledTime >= _disconnectBlockUntil)
        {
            _disconnectBlockUntil = 0f;
            RefreshButtonLook();
        }
    }

    void RefreshButtonLook()
    {
        bool connected = TokenStorage.HasStoredTokens();
        SetButtonSprite(connected);
        if (walletButton != null) walletButton.interactable = !_isConnecting && !_isDisconnecting && _disconnectBlockUntil <= 0f;
    }

    void SetButtonSprite(bool connected)
    {
        if (_imageToChange == null) return;
        if (connected && connectedSprite != null)
            _imageToChange.sprite = connectedSprite;
        else if (disconnectedSprite != null)
            _imageToChange.sprite = disconnectedSprite;
    }

    public void OnWalletButtonClick()
    {
        if (_isConnecting || _isDisconnecting || _disconnectBlockUntil > 0f) return;

        if (TokenStorage.HasStoredTokens())
        {
            _isDisconnecting = true;
            if (walletButton != null) walletButton.interactable = false;
            _authService.Logout(() =>
            {
                _walletService.Disconnect();
                _isDisconnecting = false;
                _disconnectBlockUntil = Time.unscaledTime + 1.2f;
                RefreshButtonLook();
            });
            return;
        }

        _isConnecting = true;
        if (walletButton != null) walletButton.interactable = false;

        _authService.GetMessageThenConnectAndLogin(result =>
        {
            _isConnecting = false;
            RefreshButtonLook();
            if (!result.Success)
                Debug.LogWarning("[Wallet] " + (result.Error ?? "Failed"));
        });
    }
}
