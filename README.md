
**You can view the demo in your browser:**
👉 [Open the Unity WebGL Playground Demo](http://localhost:8000)

# Appwrite's Unity Playground 🎮

This Unity project is a simple way to explore the Appwrite API with the Appwrite C# SDK in Unity. Use it to learn how to integrate Appwrite into a Unity app and try common features end‑to‑end.

The main sample script lives at `Assets/Scripts/AppwriteUnityPlayground.cs` and wires up UI buttons to SDK calls (Auth, Databases, Storage, Functions, Teams, Locale, Realtime).

![Appwrite Playground](preview.png)

## Get Started

The scene in `Assets/Scenes/` includes a demo UI. Enter Play Mode and click buttons to exercise API calls. Make sure your Appwrite instance and project are configured first.

> This playground intentionally favors clarity over best practices. It shows minimal examples of using the API.

## Backend Setup (Appwrite)

We recommend using the Appwrite Console for setup.

1. Create a Project in the Appwrite Console.
2. Add a Platform for your target:
	- WebGL builds: add a Web platform and set allowed domains (e.g., `http://localhost:8000`, your dev hostname).
	- Desktop builds: add the corresponding desktop platform(s) in Console.
3. Create resources you want to test and note their IDs:
	- Database and Collection (IDs will be used from the script constants)
	- Storage Bucket
	- (Optional) Function
	- (Optional) OAuth providers (Google, GitHub, Apple, Facebook) if you plan to test OAuth
4. Create a test user (email/password) for quick sign‑in.

In `Assets/Scripts/AppwriteUnityPlayground.cs`, update the test IDs at the top of the file to match your Console resources:

- `TEST_DATABASE_ID`
- `TEST_COLLECTION_ID`
- `TEST_BUCKET_ID`
- `TEST_FUNCTION_ID`

These are used by the buttons that list/create/update/delete data.

## Unity Client Setup

1. Open the project in Unity.
2. Open a scene from `Assets/Scenes/` (the demo scene contains the UI and script hook‑ups).
3. In Hierarchy, select the object with `AppwriteUnityPlayground`.
4. In the Inspector:
	- Assign or create an `AppwriteConfig` asset (or fill the serialized fields) and set:
	  - Endpoint (e.g., `https://cloud.appwrite.io/v1` or your self‑hosted URL)
	  - Project ID
	  - Optional: Dev Key (for admin‑level endpoints in editor/testing)
	  - Optional: Realtime Endpoint (if different)
	- Toggle “Use Appwrite Manager” if you want to initialize via `AppwriteManager`.

> You can also switch off the manager and let the script initialize the `Client` directly using the same config values.

## Run in Editor

1. Press Play in the Unity Editor.
2. Use the UI to:
	- Register/Login/Logout
	- List/Create/Update/Delete Documents
	- Upload/Download/List/Delete Files
	- Execute Functions and view Executions
	- Manage Teams and Memberships
	- Query Locale info
	- Subscribe to Realtime events

All responses get printed to the on‑screen log.

## Build for WebGL (optional)

1. File → Build Settings → WebGL, add your scene(s), then Build to `./build`.
2. Serve the `build/` folder with any static server (example using PowerShell + Node):

```powershell
# Optional if you have Node and npx available
npx serve ./build -l 8000
# Then open http://localhost:8000
```

Ensure your Appwrite project allows this origin in the Web platform’s allowed domains and CORS.

## Notes & Troubleshooting

- CORS/Origins: For WebGL, add your local dev origin (e.g., `http://localhost:8000`) to the Web platform and configure CORS in Appwrite.
- Cookies/Sessions: Some auth flows require secure contexts; ensure correct domain/protocol settings.
- OAuth: Enable and configure each provider in Appwrite before using OAuth buttons.
- Network/SSL: If self‑hosting with self‑signed certs, Unity WebGL or desktop may block requests; use valid certificates.

