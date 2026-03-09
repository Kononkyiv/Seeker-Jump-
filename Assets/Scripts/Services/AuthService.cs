using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace SolanaAuth
{
    /// <summary>
    /// Handles SIWS flow and token refresh. Uses ApiClient and WalletService.
    /// </summary>
    public class AuthService
    {
        readonly ApiClient _api;
        readonly WalletService _wallet;

        public AuthService(ApiClient api, WalletService wallet)
        {
            _api = api;
            _wallet = wallet;
            _api.SetRefreshHandler(RefreshTokensAsync);
        }

        /// <summary>
        /// Как в React: сначала GET message, потом один раз открываем кошелёк (connect), сразу подписываем и логиним. Меньше скачков.
        /// </summary>
        public void GetMessageThenConnectAndLogin(Action<AuthResult> onComplete)
        {
            _api.Get("/api/auth/siws/message",
                json =>
                {
                    SiwsMessagePayload payload = JsonUtility.FromJson<SiwsMessagePayload>(json);
                    if (string.IsNullOrEmpty(payload.nonce))
                    {
                        onComplete?.Invoke(AuthResult.Fail("Invalid SIWS response"));
                        return;
                    }
                    string messageToSign = "Sign in to My App\nNonce: " + payload.nonce;
                    _wallet.Connect((connected, connectErr) =>
                    {
                        if (!connected)
                        {
                            onComplete?.Invoke(AuthResult.Fail(connectErr ?? "Connect failed"));
                            return;
                        }
                        _wallet.SignMessage(messageToSign, (walletB64, signatureB64, signedMessageB64, signErr) =>
                        {
                            if (!string.IsNullOrEmpty(signErr))
                            {
                                onComplete?.Invoke(AuthResult.Fail(signErr));
                                return;
                            }
                            var loginBody = new LoginRequest
                            {
                                wallet = walletB64,
                                signature = signatureB64,
                                signedMessage = signedMessageB64,
                                nonce = payload.nonce
                            };
                            string bodyJson = "{\"wallet\":\"" + EscapeJson(loginBody.wallet) + "\",\"signature\":\"" + EscapeJson(loginBody.signature) + "\",\"signedMessage\":\"" + EscapeJson(loginBody.signedMessage) + "\",\"nonce\":\"" + EscapeJson(loginBody.nonce) + "\"}";
                            _api.Post("/api/auth/login", bodyJson,
                                loginResp =>
                                {
                                    LoginResponse login = JsonUtility.FromJson<LoginResponse>(loginResp);
                                    if (login == null || string.IsNullOrEmpty(login.accessToken))
                                    {
                                        onComplete?.Invoke(AuthResult.Fail("No access token in response"));
                                        return;
                                    }
                                    TokenStorage.SaveTokens(login.accessToken, login.refreshToken, _wallet.GetWalletAddressForStorage());
                                    onComplete?.Invoke(AuthResult.Ok(login.client ?? new ClientInfo()));
                                },
                                e => onComplete?.Invoke(AuthResult.Fail(e)));
                        });
                    });
                },
                e => onComplete?.Invoke(AuthResult.Fail(e)));
        }

        /// <summary>
        /// Full Sign In With Solana: get message -> sign -> POST login -> store tokens. (Кошелёк уже должен быть подключён.)
        /// </summary>
        public void SignInWithSolana(Action<AuthResult> onComplete)
        {
            _api.Get("/api/auth/siws/message",
                json =>
                {
                    SiwsMessagePayload payload = JsonUtility.FromJson<SiwsMessagePayload>(json);
                    if (string.IsNullOrEmpty(payload.nonce))
                    {
                        onComplete?.Invoke(AuthResult.Fail("Invalid SIWS response"));
                        return;
                    }
                    string messageToSign = "Sign in to My App\nNonce: " + payload.nonce;
                    _wallet.SignMessage(messageToSign, (walletB64, signatureB64, signedMessageB64, err) =>
                    {
                        if (!string.IsNullOrEmpty(err))
                        {
                            onComplete?.Invoke(AuthResult.Fail(err));
                            return;
                        }
                        var loginBody = new LoginRequest
                        {
                            wallet = walletB64,
                            signature = signatureB64,
                            signedMessage = signedMessageB64,
                            nonce = payload.nonce
                        };
                        string bodyJson = "{\"wallet\":\"" + EscapeJson(loginBody.wallet) + "\",\"signature\":\"" + EscapeJson(loginBody.signature) + "\",\"signedMessage\":\"" + EscapeJson(loginBody.signedMessage) + "\",\"nonce\":\"" + EscapeJson(loginBody.nonce) + "\"}";
                        _api.Post("/api/auth/login", bodyJson,
                            loginResp =>
                            {
                                LoginResponse login = JsonUtility.FromJson<LoginResponse>(loginResp);
                                if (login == null || string.IsNullOrEmpty(login.accessToken))
                                {
                                    onComplete?.Invoke(AuthResult.Fail("No access token in response"));
                                    return;
                                }
                                TokenStorage.SaveTokens(login.accessToken, login.refreshToken, _wallet.GetWalletAddressForStorage());
                                onComplete?.Invoke(AuthResult.Ok(login.client ?? new ClientInfo()));
                            },
                            e => onComplete?.Invoke(AuthResult.Fail(e)));
                    });
                },
                e => onComplete?.Invoke(AuthResult.Fail(e)));
        }

        public Task<(string accessToken, string refreshToken)> RefreshTokensAsync()
        {
            var tcs = new TaskCompletionSource<(string, string)>();
            string refreshToken = TokenStorage.GetRefreshToken();
            if (string.IsNullOrEmpty(refreshToken))
            {
                tcs.SetException(new Exception("No refresh token"));
                return tcs.Task;
            }
            string body = "{\"refreshToken\":\"" + EscapeJson(refreshToken) + "\"}";
            _api.Post("/api/auth/refresh", body,
                json =>
                {
                    RefreshResponse r = JsonUtility.FromJson<RefreshResponse>(json);
                    if (!string.IsNullOrEmpty(r.accessToken))
                    {
                        TokenStorage.SaveTokens(r.accessToken, refreshToken, null);
                        tcs.SetResult((r.accessToken, refreshToken));
                    }
                    else
                        tcs.SetException(new Exception("Refresh returned no access token"));
                },
                ex => tcs.SetException(new Exception(ex)));
            return tcs.Task;
        }

        public void Logout(Action onDone)
        {
            string refreshToken = TokenStorage.GetRefreshToken();
            if (string.IsNullOrEmpty(refreshToken))
            {
                TokenStorage.ClearAll();
                onDone?.Invoke();
                return;
            }
            string body = "{\"refreshToken\":\"" + EscapeJson(refreshToken) + "\"}";
            _api.Post("/api/auth/logout", body, _ => { }, _ => { });
            TokenStorage.ClearAll();
            onDone?.Invoke();
        }

        static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }

        [Serializable]
        class SiwsMessagePayload
        {
            public string nonce;
            public string message;
        }

        [Serializable]
        class LoginRequest
        {
            public string wallet;
            public string signature;
            public string signedMessage;
            public string nonce;
        }

        [Serializable]
        class LoginResponse
        {
            public string accessToken;
            public string refreshToken;
            public ClientInfo client;
        }

        [Serializable]
        class RefreshResponse
        {
            public string accessToken;
            public ClientInfo client;
        }

    }

    [Serializable]
    public class ClientInfo
    {
        public int id;
        public string wallet;
        public string role;
    }

    public struct AuthResult
    {
        public bool Success;
        public string Error;
        public ClientInfo Client;

        public static AuthResult Ok(ClientInfo client) =>
            new AuthResult { Success = true, Client = client };
        public static AuthResult Fail(string error) =>
            new AuthResult { Success = false, Error = error };
    }
}
