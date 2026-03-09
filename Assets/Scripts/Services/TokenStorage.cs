using UnityEngine;

namespace SolanaAuth
{
    /// <summary>
    /// Persists access and refresh tokens between sessions (PlayerPrefs).
    /// </summary>
    public static class TokenStorage
    {
        const string KeyAccessToken = "SolanaAuth_AccessToken";
        const string KeyRefreshToken = "SolanaAuth_RefreshToken";
        const string KeyWalletAddress = "SolanaAuth_WalletAddress";

        public static void SaveTokens(string accessToken, string refreshToken, string walletAddress = null)
        {
            if (!string.IsNullOrEmpty(accessToken)) PlayerPrefs.SetString(KeyAccessToken, accessToken);
            if (!string.IsNullOrEmpty(refreshToken)) PlayerPrefs.SetString(KeyRefreshToken, refreshToken);
            if (walletAddress != null) PlayerPrefs.SetString(KeyWalletAddress, walletAddress);
            PlayerPrefs.Save();
        }

        public static string GetAccessToken()
        {
            return PlayerPrefs.GetString(KeyAccessToken, null);
        }

        public static string GetRefreshToken()
        {
            return PlayerPrefs.GetString(KeyRefreshToken, null);
        }

        public static string GetStoredWalletAddress()
        {
            return PlayerPrefs.GetString(KeyWalletAddress, null);
        }

        public static void ClearAll()
        {
            PlayerPrefs.DeleteKey(KeyAccessToken);
            PlayerPrefs.DeleteKey(KeyRefreshToken);
            PlayerPrefs.DeleteKey(KeyWalletAddress);
            PlayerPrefs.Save();
        }

        public static bool HasStoredTokens()
        {
            return !string.IsNullOrEmpty(GetAccessToken()) || !string.IsNullOrEmpty(GetRefreshToken());
        }
    }
}
