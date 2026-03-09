using System;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SolanaAuth
{
    /// <summary>
    /// Connects to Solana wallet (via Solana.Unity.SDK), retrieves address, signs SIWS message.
    /// Backend expects: wallet = base64(32-byte public key), signature = base64(64-byte), signedMessage = base64(UTF-8 message bytes).
    /// </summary>
    public class WalletService
    {
        readonly MonoBehaviour _runner;
        bool _connected;

        public WalletService(MonoBehaviour coroutineRunner)
        {
            _runner = coroutineRunner;
        }

        public bool IsConnected => _connected;

        /// <summary>
        /// Connect wallet using Solana Unity SDK (Wallet Adapter). Call from main thread.
        /// </summary>
        public void Connect(Action<bool, string> onComplete)
        {
            _runner.StartCoroutine(ConnectCoroutine(onComplete));
        }

        IEnumerator ConnectCoroutine(Action<bool, string> onComplete)
        {
            Task loginTask = null;
            try
            {
                var web3 = GetWeb3Instance();
                if (web3 == null) { onComplete?.Invoke(false, "Web3 not found. Add Web3 component to scene (Solana.Unity.SDK)."); yield break; }
                var loginMethod = web3.GetType().GetMethod("LoginWalletAdapter", Type.EmptyTypes);
                if (loginMethod == null) { onComplete?.Invoke(false, "LoginWalletAdapter not found."); yield break; }
                loginTask = loginMethod.Invoke(web3, null) as Task;
            }
            catch (Exception e)
            {
                onComplete?.Invoke(false, e.Message);
                yield break;
            }

            if (loginTask == null) { onComplete?.Invoke(false, "Login not started"); yield break; }

            while (!loginTask.IsCompleted) yield return null;
            if (loginTask.IsFaulted)
            {
                _connected = false;
                onComplete?.Invoke(false, loginTask.Exception?.GetBaseException()?.Message ?? "Login failed");
                yield break;
            }

            _connected = GetAccount() != null;
            onComplete?.Invoke(_connected, _connected ? null : "No account after login");
        }

        /// <summary>
        /// Disconnect wallet and clear local wallet state (tokens are cleared by AuthService.Logout).
        /// </summary>
        public void Disconnect()
        {
            _connected = false;
            try
            {
                var web3 = GetWeb3Instance();
                web3?.GetType().GetMethod("Logout", Type.EmptyTypes)?.Invoke(web3, null);
            }
            catch { /* ignore */ }
        }

        /// <summary>
        /// Sign the SIWS message via WalletBase.SignMessage (SDK API). Callback receives (walletBase64, signatureBase64, signedMessageBase64, error).
        /// </summary>
        public void SignMessage(string message, Action<string, string, string, string> onComplete)
        {
            if (string.IsNullOrEmpty(message)) { onComplete?.Invoke(null, null, null, "Message is empty"); return; }
            message = message.Replace("\\n", "\n").Replace("\\r", "\r");
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            _runner.StartCoroutine(SignMessageCoroutine(messageBytes, onComplete));
        }

        IEnumerator SignMessageCoroutine(byte[] messageBytes, Action<string, string, string, string> onComplete)
        {
            object account = GetAccount();
            if (account == null) { onComplete?.Invoke(null, null, null, "Wallet not connected"); yield break; }

            byte[] signature = null;
            object wallet = GetWallet();
            if (wallet != null)
            {
                var signMethod = wallet.GetType().GetMethod("SignMessage", new[] { typeof(byte[]) });
                if (signMethod != null && signMethod.ReturnType != null)
                {
                    var taskObj = signMethod.Invoke(wallet, new object[] { messageBytes });
                    if (taskObj != null)
                    {
                        var taskType = taskObj.GetType();
                        var isCompletedProp = taskType.GetProperty("IsCompleted");
                        if (isCompletedProp != null)
                        {
                            while (!(bool)isCompletedProp.GetValue(taskObj)) yield return null;
                            var isFaultedProp = taskType.GetProperty("IsFaulted");
                            if (isFaultedProp != null && (bool)isFaultedProp.GetValue(taskObj))
                            {
                                var exProp = taskType.GetProperty("Exception");
                                var ex = exProp?.GetValue(taskObj) as Exception;
                                onComplete?.Invoke(null, null, null, ex?.GetBaseException()?.Message ?? "Sign failed");
                                yield break;
                            }
                            var resultProp = taskType.GetProperty("Result");
                            if (resultProp != null) signature = resultProp.GetValue(taskObj) as byte[];
                        }
                    }
                }
            }
            if (signature == null) signature = SignMessageBytes(account, messageBytes);
            if (signature == null) { onComplete?.Invoke(null, null, null, "Invalid signature from wallet"); yield break; }
            if (signature.Length == 128) { var s = new byte[64]; System.Array.Copy(signature, 0, s, 0, 64); signature = s; }
            if (signature.Length != 64) { onComplete?.Invoke(null, null, null, "Invalid signature from wallet"); yield break; }

            byte[] publicKeyBytes = GetPublicKeyBytes(account);
            if (publicKeyBytes == null || publicKeyBytes.Length != 32) { onComplete?.Invoke(null, null, null, "Invalid public key"); yield break; }

            string walletB64 = Convert.ToBase64String(publicKeyBytes);
            string signatureB64 = Convert.ToBase64String(signature);
            string signedMessageB64 = Convert.ToBase64String(messageBytes);
            onComplete?.Invoke(walletB64, signatureB64, signedMessageB64, null);
        }

        static object GetWallet()
        {
            var web3 = GetWeb3Instance();
            if (web3 == null) return null;
            var walletProp = web3.GetType().GetProperty("WalletBase", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (walletProp != null) return walletProp.GetValue(web3);
            return web3.GetType().GetProperty("Wallet", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)?.GetValue(web3);
        }

        public string GetWalletAddressForStorage()
        {
            var acc = GetAccount();
            if (acc == null) return null;
            var pk = GetPublicKeyBytes(acc);
            if (pk == null) return null;
            return Convert.ToBase64String(pk);
        }

        static Type FindWeb3Type()
        {
            var name = "Solana.Unity.SDK.Web3";
            var t = Type.GetType(name + ", Solana.Unity.SDK");
            if (t != null) return t;
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    t = asm.GetType(name);
                    if (t != null) return t;
                }
                catch { /* ignore */ }
            }
            return null;
        }

        static object GetWeb3Instance()
        {
            var t = FindWeb3Type();
            if (t == null) return null;
            var prop = t.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            var instance = prop?.GetValue(null);
            if (instance != null) return instance;
            var findMethod = typeof(UnityEngine.Object).GetMethod("FindObjectOfType", new[] { typeof(System.Type) });
            return findMethod?.Invoke(null, new object[] { t });
        }

        static object GetAccount()
        {
            var web3Type = FindWeb3Type();
            if (web3Type == null) return null;
            var prop = web3Type.GetProperty("Account", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            return prop?.GetValue(null);
        }

        static byte[] GetPublicKeyBytes(object account)
        {
            if (account == null) return null;
            var pkProp = account.GetType().GetProperty("PublicKey");
            var pk = pkProp?.GetValue(account);
            if (pk == null) return null;
            var t = pk.GetType();
            foreach (var name in new[] { "Key", "KeyBytes", "key", "KeySpan", "Data" })
            {
                var prop = t.GetProperty(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (prop == null) continue;
                var val = prop.GetValue(pk);
                if (val is byte[] arr && arr.Length == 32) return arr;
                if (val != null && val.GetType().IsArray)
                {
                    var a = (Array)val;
                    if (a.Length == 32)
                    {
                        var bytes = new byte[32];
                        for (int i = 0; i < 32; i++) bytes[i] = (byte)a.GetValue(i);
                        return bytes;
                    }
                }
            }
            foreach (var methodName in new[] { "ToByteArray", "GetBytes", "ToBytes" })
            {
                var method = t.GetMethod(methodName, Type.EmptyTypes);
                if (method != null && method.ReturnType == typeof(byte[]))
                {
                    var arr = method.Invoke(pk, null) as byte[];
                    if (arr != null && arr.Length == 32) return arr;
                }
            }
            var str = pk.ToString();
            if (!string.IsNullOrEmpty(str) && str.Length >= 32 && str.Length < 50 && str.IndexOf("PublicKey") < 0)
                return Base58Decode(str);
            return null;
        }

        static byte[] Base58Decode(string input)
        {
            const string alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
            var bytes = new System.Collections.Generic.List<int>();
            for (int i = 0; i < input.Length; i++)
            {
                int carry = alphabet.IndexOf(input[i]);
                if (carry < 0) return null;
                for (int j = 0; j < bytes.Count; j++)
                {
                    carry += 58 * bytes[j];
                    bytes[j] = carry % 256;
                    carry /= 256;
                }
                while (carry > 0) { bytes.Add(carry % 256); carry /= 256; }
            }
            for (int i = 0; i < input.Length && input[i] == '1'; i++) bytes.Add(0);
            bytes.Reverse();
            var result = new byte[bytes.Count];
            for (int i = 0; i < bytes.Count; i++) result[i] = (byte)bytes[i];
            if (result.Length == 32) return result;
            if (result.Length == 33 && result[0] == 0) { var r = new byte[32]; System.Array.Copy(result, 1, r, 0, 32); return r; }
            if (result.Length == 31) { var r = new byte[32]; System.Array.Copy(result, 0, r, 1, 31); return r; }
            return null;
        }

        static byte[] SignMessageBytes(object account, byte[] messageBytes)
        {
            if (account == null || messageBytes == null) return null;
            var signMethod = account.GetType().GetMethod("Sign", new[] { typeof(byte[]) });
            if (signMethod == null) return null;
            var result = signMethod.Invoke(account, new object[] { messageBytes });
            return result as byte[];
        }
    }
}
