# Solana Auth (SIWS) – Unity Setup

## Backend (unchanged)

- `GET /api/auth/siws/message` → `{ nonce, message }`
- `POST /api/auth/login` → body: `wallet`, `signature`, `signedMessage`, `nonce` (all base64)
- `POST /api/auth/refresh` → body: `refreshToken` → `{ accessToken, client }`
- `POST /api/auth/logout` → body: `refreshToken` (optional)

## Unity

### 1. Solana SDK

- Added in `Packages/manifest.json`: `com.solana.unity_sdk` (Git URL).
- After opening the project, add the **Web3** component to a GameObject in **WalletScene** (e.g. empty “SolanaWeb3”):
  - Solana.Unity.SDK **Web3** script must be in the scene for wallet connection.
  - Configure RPC / Wallet Adapter in the Web3 inspector if needed.

### 2. WalletScene

- **WalletSceneController** (on a GameObject in WalletScene):
  - **Api Base Url**: backend root (e.g. `http://localhost:3000`).
  - **Connect Button**: connect wallet (Wallet Adapter).
  - **Sign In Button**: run SIWS (get message → sign → login) and store tokens.
  - **Disconnect Button**: logout and clear tokens.
  - **Status Text**: optional TextMeshPro for status.
  - **Loading Indicator**: optional GameObject to show while connecting/signing.

### 3. Token storage

- **TokenStorage** (static) uses **PlayerPrefs** for `accessToken`, `refreshToken`, and wallet address so they persist between sessions.

### 4. API client

- **ApiClient** sends `Authorization: Bearer <accessToken>` on requests.
- On **401**, it calls the refresh handler (AuthService), then retries the request once.

### 5. CORS

- Backend must allow the Unity player origin (e.g. `http://localhost` for editor, or your build URL). Adjust `allowedOrigins` in `server.js` if needed.
