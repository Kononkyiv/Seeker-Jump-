using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace SolanaAuth
{
    /// <summary>
    /// API client that attaches Bearer token and retries on 401 using refresh token.
    /// </summary>
    public class ApiClient
    {
        readonly string _baseUrl;
        readonly MonoBehaviour _coroutineRunner;
        Func<System.Threading.Tasks.Task<(string accessToken, string refreshToken)>> _onRefreshRequired;

        public ApiClient(string baseUrl, MonoBehaviour coroutineRunner)
        {
            _baseUrl = baseUrl?.TrimEnd('/') ?? "";
            _coroutineRunner = coroutineRunner;
        }

        public void SetRefreshHandler(Func<System.Threading.Tasks.Task<(string accessToken, string refreshToken)>> onRefreshRequired)
        {
            _onRefreshRequired = onRefreshRequired;
        }

        public void Get(string path, Action<string> onSuccess, Action<string> onError)
        {
            _coroutineRunner.StartCoroutine(GetCoroutine(path, onSuccess, onError));
        }

        public void Post(string path, string jsonBody, Action<string> onSuccess, Action<string> onError)
        {
            _coroutineRunner.StartCoroutine(PostCoroutine(path, jsonBody, onSuccess, onError));
        }

        const int RequestTimeoutSeconds = 15;

        IEnumerator GetCoroutine(string path, Action<string> onSuccess, Action<string> onError, bool isRetry = false)
        {
            using var req = UnityWebRequest.Get(_baseUrl + path);
            req.timeout = RequestTimeoutSeconds;
            AttachAuth(req);
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke(req.downloadHandler?.text);
                yield break;
            }

            if (req.responseCode == 401 && !isRetry && _onRefreshRequired != null)
            {
                var refreshTask = _onRefreshRequired();
                while (!refreshTask.IsCompleted) yield return null;
                if (refreshTask.Exception != null)
                {
                    onError?.Invoke(refreshTask.Exception.Message);
                    yield break;
                }
                var (_, _) = refreshTask.Result;
                yield return GetCoroutine(path, onSuccess, onError, isRetry: true);
                yield break;
            }

            onError?.Invoke(req.error ?? $"HTTP {req.responseCode}");
        }

        IEnumerator PostCoroutine(string path, string jsonBody, Action<string> onSuccess, Action<string> onError, bool isRetry = false)
        {
            using var req = new UnityWebRequest(_baseUrl + path, "POST");
            req.timeout = RequestTimeoutSeconds;
            req.uploadHandler = new UploadHandlerRaw(string.IsNullOrEmpty(jsonBody) ? null : System.Text.Encoding.UTF8.GetBytes(jsonBody));
            req.downloadHandler = new DownloadHandlerBuffer();
            AttachAuth(req);
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke(req.downloadHandler?.text);
                yield break;
            }

            if (req.responseCode == 401 && !isRetry && _onRefreshRequired != null)
            {
                var refreshTask = _onRefreshRequired();
                while (!refreshTask.IsCompleted) yield return null;
                if (refreshTask.Exception != null)
                {
                    onError?.Invoke(refreshTask.Exception.Message);
                    yield break;
                }
                yield return PostCoroutine(path, jsonBody, onSuccess, onError, isRetry: true);
                yield break;
            }

            onError?.Invoke(req.error ?? $"HTTP {req.responseCode}");
        }

        void AttachAuth(UnityWebRequest req)
        {
            string token = TokenStorage.GetAccessToken();
            if (!string.IsNullOrEmpty(token))
                req.SetRequestHeader("Authorization", "Bearer " + token);
        }
    }
}
