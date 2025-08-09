using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Appwrite;
using Appwrite.Services;
using Appwrite.Models;
using Appwrite.Enums;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Locale = Appwrite.Services.Locale;

/// <summary>
/// Comprehensive Appwrite Unity Playground - Demonstrates all SDK capabilities
/// </summary>
public class AppwriteUnityPlayground : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private AppwriteConfig config;
    [SerializeField] private bool useAppwriteManager = true;
    
    [Header("UI References")]
    [SerializeField] private Transform contentParent;
    [SerializeField] private Button buttonPrefab;
    [SerializeField] private TextMeshProUGUI outputText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TMP_InputField emailField;
    [SerializeField] private TMP_InputField passwordField;
    [SerializeField] private TMP_InputField nameField;
    [SerializeField] private Transform filesList;
    [SerializeField] private Transform documentsList;
    [SerializeField] private Transform realtimeEventsList;
    [SerializeField] private GameObject sectionPrefab;

    // Services
    private Client _client;
    private AppwriteManager _appwriteManager;
    private Account _account;
    private Databases _databases;
    private Storage _storage;
    private Functions _functions;
    private Teams _teams;
    private Locale _locale;
    private Realtime _realtime;
    
    // Test data IDs (should be configured for your Appwrite project)
    private const string TEST_DATABASE_ID = "6888d7dd001f35bf501b";
    private const string TEST_COLLECTION_ID = "6895028d001b7e9d541d";
    private const string TEST_BUCKET_ID = "689502a5003ae7e65d9a";
    private const string TEST_FUNCTION_ID = "68950311001d6d4f6b24";
    
    private string _testTeamID;
    private bool _isInitialized;

    private void Start() => SafeExecuteAsync(Initialize, nameof(Initialize));

    private async UniTaskVoid Initialize()
    {
        SetStatus("Initializing Appwrite Playground...");
        
        if (useAppwriteManager)
        {
            await InitializeWithManager();
        }
        else
        {
            await InitializeWithClient();
        }
        
        CreateUI();
        SetStatus("Ready! Click buttons to test Appwrite features.");
        
        // Subscribe to realtime events
        await SetupRealtime();
        
        LogOutput("=== APPWRITE UNITY PLAYGROUND ===");
        LogOutput("This playground demonstrates all major Appwrite SDK features.");
        LogOutput("Configure TEST_DATABASE_ID, TEST_COLLECTION_ID, etc. in the script for your project.");
        LogOutput("Make sure you have the required collections, buckets, and functions set up in Appwrite Console.");
    }

    #region Initialization
    
    private async UniTask InitializeWithManager()
    {
        _appwriteManager = AppwriteManager.Instance;
        if (_appwriteManager == null)
        {
            var managerGo = new GameObject("AppwriteManager");
            _appwriteManager = managerGo.AddComponent<AppwriteManager>();
            _appwriteManager.SetConfig(config);
        }
        
        var success = await _appwriteManager.Initialize(needRealtime: true);
        if (!success)
        {
            SetStatus("Failed to initialize AppwriteManager");
            return;
        }
        
        _client = _appwriteManager.Client;
        _account = _appwriteManager.GetService<Account>();
        _databases = _appwriteManager.GetService<Databases>();
        _storage = _appwriteManager.GetService<Storage>();
        _functions = _appwriteManager.GetService<Functions>();
        _teams = _appwriteManager.GetService<Teams>();
        _locale = _appwriteManager.GetService<Locale>();
        _realtime = _appwriteManager.Realtime;
        
        _isInitialized = true;
    }
    
    private async UniTask InitializeWithClient()
    {
        _client = new Client()
            .SetEndpoint(config.Endpoint)
            .SetProject(config.ProjectId);
            
        if (!string.IsNullOrEmpty(config.DevKey))
            _client.SetDevKey(config.DevKey);
            
        if (!string.IsNullOrEmpty(config.RealtimeEndpoint))
            _client.SetEndPointRealtime(config.RealtimeEndpoint);
        
        _account = new Account(_client);
        _databases = new Databases(_client);
        _storage = new Storage(_client);
        _functions = new Functions(_client);
        _teams = new Teams(_client);
        _locale = new Locale(_client);
        
        // Setup realtime manually
        var realtimeGo = new GameObject("Realtime");
        _realtime = realtimeGo.AddComponent<Realtime>();
        _realtime.Initialize(_client);
        
        // Test connection
        try
        {
            var pingResult = await _client.Ping();
            LogOutput($"Connected to Appwrite: {pingResult}");
        }
        catch (Exception ex)
        {
            LogOutput($"Connection failed: {ex.Message}", LogType.Error);
        }
        
        _isInitialized = true;
    }
    
    private UniTask SetupRealtime()
    {
        if (_realtime == null) Debug.LogError("Realtime not initialized");
        
        try
        {
            // Subscribe to various realtime events
            var subscription = _realtime.Subscribe(
                new[] { 
                    "files", 
                    "documents",
                    "databases.*",
                    "databases.*.collections.*.documents",
                    "account",
                    "teams",
                    "memberships"
                },
                response =>
                {
                    var eventText = $"[REALTIME] {response.Events.FirstOrDefault()}: {response.Payload.Keys.FirstOrDefault()}";
                    LogOutput(eventText);
                    AddRealtimeEvent(eventText);
                }
            );
            
            
            LogOutput("Realtime subscriptions active");
        }
        catch (Exception ex)
        {
            LogOutput($"Realtime setup failed: {ex.Message}", LogType.Error);
        }
        return UniTask.CompletedTask;
    }
    
    #endregion
    
    #region UI Creation
    
    private void CreateUI()
    {
        if (contentParent == null || buttonPrefab == null) return;

        CreateSectionHeader("🔐 AUTH & ACCOUNT", out var grid);
        CreateButton("Is Logged?", IsLogged, grid);
        CreateButton("Register Account", RegisterAccount, grid);
        CreateButton("Login", LoginWithEmail, grid);
        CreateButton("Login Anonymously", LoginAnonymously, grid);
        CreateButton("OAuth with Google", () => LoginWithOAuth(OAuthProvider.Google), grid);
        CreateButton("OAuth with GitHub", () => LoginWithOAuth(OAuthProvider.Github), grid);
        CreateButton("OAuth with Apple", () => LoginWithOAuth(OAuthProvider.Apple), grid);
        CreateButton("OAuth with Facebook", () => LoginWithOAuth(OAuthProvider.Facebook), grid);
        CreateButton("Create JWT", CreateJWT, grid);
        CreateButton("Get User Info", GetUserInfo, grid);
        CreateButton("Update Name", UpdateUserName, grid);
        CreateButton("Update Email", UpdateUserEmail, grid);
        CreateButton("Update Password", UpdateUserPassword, grid);
        CreateButton("Update Preferences", UpdateUserPrefs, grid);
        CreateButton("Get User Sessions", GetUserSessions, grid);
        CreateButton("Get User Logs", GetUserLogs, grid);
        CreateButton("Create Verification", CreateEmailVerification, grid);
        CreateButton("Create Phone Verification", CreatePhoneVerification, grid);
        CreateButton("Create Recovery", CreatePasswordRecovery, grid);
        CreateButton("Update Status (Block)", BlockUserAccount, grid);
        CreateButton("Delete Sessions", DeleteAllSessions, grid);
        CreateButton("Delete Current Session", DeleteCurrentSession, grid);
        CreateButton("Logout", Logout, grid);
        
        CreateSectionHeader("💾 DATABASES", out grid);
        CreateButton("List Documents", ListDocuments, grid);
        CreateButton("Create Document", CreateDocument, grid);
        CreateButton("Get Document", GetDocument, grid);
        CreateButton("Update Document", UpdateDocument, grid);
        CreateButton("Delete Document", DeleteDocument, grid);
        CreateButton("Upsert Document", UpsertDocument, grid);
        CreateButton("Increment Attribute", IncrementDocumentAttribute, grid);
        CreateButton("Decrement Attribute", DecrementDocumentAttribute, grid);
        
        CreateSectionHeader("📁 STORAGE", out grid);
        CreateButton("List Files", ListFiles, grid);
        CreateButton("Upload File (Text)", UploadTextFile, grid);
        CreateButton("Get File Info", GetFileInfo, grid);
        CreateButton("Update File", UpdateFile, grid);
        CreateButton("Download File", DownloadFile, grid);
        CreateButton("Get File Preview", GetFilePreview, grid);
        CreateButton("Delete File", DeleteFile, grid);

        CreateSectionHeader("⚡ FUNCTIONS", out grid);
        CreateButton("List Functions", ListFunctions, grid);
        CreateButton("Execute Function", ExecuteFunction, grid);
        CreateButton("List Executions", ListExecutions, grid);
        CreateButton("Get Execution", GetExecution, grid);

        CreateSectionHeader("👥 TEAMS", out grid);
        CreateButton("List Teams", ListTeams, grid);
        CreateButton("Create Team", CreateTeam, grid);
        CreateButton("Get Team", GetTeam, grid);
        CreateButton("Update Team", UpdateTeam, grid);
        CreateButton("Delete Team", DeleteTeam, grid);
        CreateButton("List Team Memberships", ListTeamMemberships, grid);
        CreateButton("Create Team Membership", CreateTeamMembership, grid);
        CreateButton("Get Team Membership", GetTeamMembership, grid);
        CreateButton("Update Team Membership", UpdateTeamMembership, grid);
        CreateButton("Delete Team Membership", DeleteTeamMembership, grid);
        CreateButton("Update Team Preferences", UpdateTeamPrefs, grid);
        CreateButton("Get Team Preferences", GetTeamPrefs, grid);

        CreateSectionHeader("🌍 LOCALE", out grid);
        CreateButton("Get User Location", GetUserLocation, grid);
        CreateButton("List Countries", ListCountries, grid);
        CreateButton("List EU Countries", ListCountriesEU, grid);
        CreateButton("List Continents", ListContinents, grid);
        CreateButton("List Currencies", ListCurrencies, grid);
        CreateButton("List Languages", ListLanguages, grid);
        CreateButton("List Phone Codes", ListCountriesPhones, grid);
        CreateButton("List Locale Codes", ListLocaleCodes, grid);

        CreateSectionHeader("🔄 REALTIME", out grid);
        CreateButton("Test Realtime", TestRealtimeConnection, grid);

        CreateSectionHeader("🧪 UTILITIES", out grid);
        CreateButton("Ping Server", PingServer, grid);
        CreateButton("Clear Output", ClearOutput, grid);
        CreateButton("Clear Realtime Events", ClearRealtimeEvents, grid);
        CreateButton("See cookies", SeeCookies, grid);
    }
    
    private void CreateSectionHeader(string title, out GameObject grid)
    {
        if (buttonPrefab == null)
        {
            grid = null;
            return ;
        }
        
        var headerButton = Instantiate(sectionPrefab, contentParent);
        var headerText = headerButton.GetComponentInChildren<TextMeshProUGUI>();
        if (headerText != null)
        {
            headerText.text = title;
            headerText.fontStyle = FontStyles.Bold;
            headerText.fontSize = 16f;
        }
        grid = headerButton.GetComponentInChildren<GridLayoutGroup>().gameObject;
    }
    
    private void CreateButton(string buttonText, Func<UniTaskVoid> action, GameObject grid)
    {
        if (buttonPrefab == null) return;
        
        var button = Instantiate(buttonPrefab, grid.transform);
        var text = button.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
            text.text = buttonText;
        
        button.onClick.AddListener(() =>
        {
            if (!_isInitialized)
            {
                LogOutput("❌ Playground not initialized yet!", LogType.Warning);
                return;
            }

            SafeExecuteAsync(action, buttonText);
        });
    }
    
    #endregion
    
    #region Account & Authentication
    
    private async UniTaskVoid IsLogged()
    {
        try
        {
            var user = await _account.Get();
            LogOutput($"✅ User is logged in: {user.Name} ({user.Email})");
        }
        catch (Exception ex)
        {
            LogOutput($"❌ User not logged in: {ex.Message}", LogType.Warning);
        }
    }
    
    private async UniTaskVoid RegisterAccount()
    {
        try
        {
            var email = GetEmail();
            var password = GetPassword();
            var username = GetName();
            
            var user = await _account.Create(
                userId: ID.Unique(),
                email: email,
                password: password,
                name: username
            );
            
            LogOutput($"✅ Account created: {user.Name} ({user.Email})");
        }
        catch (Exception ex)
        {
            LogOutput($"❌ Registration failed: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid LoginWithEmail()
    {
        try
        {
            var email = GetEmail();
            var password = GetPassword();
            
            var session = await _account.CreateEmailPasswordSession(email, password);
            LogOutput($"✅ Login successful: {session.UserId}");
        }
        catch (Exception ex)
        {
            LogOutput($"❌ Login failed: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid LoginAnonymously()
    {
        try
        {
            var session = await _account.CreateAnonymousSession();
            LogOutput($"✅ Anonymous login successful: {session.UserId}");
        }
        catch (Exception ex)
        {
            LogOutput($"❌ Anonymous login failed: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid LoginWithOAuth(OAuthProvider provider)
    {
        try
        {
            LogOutput($"🔄 Starting {provider} OAuth...");
            #if UNITY_WEBGL
            await _account.CreateOAuth2Session(provider: provider, success: Application.absoluteURL, failure: "http://localhost:8000");
            #else 
            await _account.CreateOAuth2Session(provider: provider);
            #endif
            LogOutput($"✅ {provider} OAuth initiated");
        }
        catch (Exception ex)
        {
            LogOutput($"❌ {provider} OAuth failed: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid CreateJWT()
    {
        try
        {
            var token = await _account.CreateJWT();
            LogOutput($"✅ JWT created: {token.Jwt.Substring(0, 50)}...");
        }
        catch (Exception ex)
        {
            LogOutput($"❌ JWT creation failed: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid GetUserInfo()
    {
        try
        {
            var user = await _account.Get();
            LogOutput("✅ User Info:");
            LogOutput($"  Name: {user.Name}");
            LogOutput($"  Email: {user.Email}");
            LogOutput($"  ID: {user.Id}");
            LogOutput($"  Created: {user.CreatedAt}");
            LogOutput($"  Status: {user.Status}");
            LogOutput($"  Email Verified: {user.EmailVerification}");
            LogOutput($"  Phone Verified: {user.PhoneVerification}");
        }
        catch (Exception ex)
        {
            LogOutput($"❌ Failed to get user info: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid UpdateUserName()
    {
        try
        {
            var username = GetName();
            var user = await _account.UpdateName(username);
            LogOutput($"✅ Name updated to: {user.Name}");
        }
        catch (Exception ex)
        {
            LogOutput($"❌ Name update failed: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid UpdateUserEmail()
    {
        try
        {
            var email = GetEmail();
            var password = GetPassword();
            var user = await _account.UpdateEmail(email, password);
            LogOutput($"✅ Email updated to: {user.Email}");
        }
        catch (Exception ex)
        {
            LogOutput($"❌ Email update failed: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid UpdateUserPassword()
    {
        try
        {
            var password = GetPassword();
            var user = await _account.UpdatePassword(password);
            LogOutput($"✅ Password updated successfully { user.PasswordUpdate}");
        }
        catch (Exception ex)
        {
            LogOutput($"❌ Password update failed: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid UpdateUserPrefs()
    {
        try
        {
            var prefs = new { theme = "dark", language = "en", notifications = true };
            var user = await _account.UpdatePrefs(prefs);
            LogOutput($"✅ Preferences updated { user.Name}");
        }
        catch (Exception ex)
        {
            LogOutput($"❌ Preferences update failed: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid GetUserSessions()
    {
        try
        {
            var sessions = await _account.ListSessions();
            LogOutput($"✅ User has {sessions.Total} sessions");
            foreach (var session in sessions.Sessions)
            {
                LogOutput($"  Session: {session.ClientName} - {session.CreatedAt}");
            }
        }
        catch (Exception ex)
        {
            LogOutput($"❌ Failed to get sessions: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid GetUserLogs()
    {
        try
        {
            var logs = await _account.ListLogs();
            LogOutput($"✅ User has {logs.Total} logs");
            foreach (var log in logs.Logs.Take(5))
            {
                LogOutput($"  Log: {log.Event} - {log.Time}");
            }
        }
        catch (Exception ex)
        {
            LogOutput($"❌ Failed to get logs: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid CreateEmailVerification()
    {
        try
        {
            var token = await _account.CreateVerification("https://appwrite.io");
            LogOutput($"✅ Email verification sent: {token.Expire}");
        }
        catch (Exception ex)
        {
            LogOutput($"❌ Email verification failed: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid CreatePhoneVerification()
    {
        try
        {
            var token = await _account.CreatePhoneVerification();
            LogOutput($"✅ Phone verification sent: {token.Expire}");
        }
        catch (Exception ex)
        {
            LogOutput($"❌ Phone verification failed: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid CreatePasswordRecovery()
    {
        try
        {
            var email = GetEmail();
            var token = await _account.CreateRecovery(email, "https://appwrite.io");
            LogOutput($"✅ Password recovery sent: {token.Expire}");
        }
        catch (Exception ex)
        {
            LogOutput($"❌ Password recovery failed: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid BlockUserAccount()
    {
        try
        {
            var user = await _account.UpdateStatus();
            LogOutput($"✅ Account status updated: {user.Status}");
        }
        catch (Exception ex)
        {
            LogOutput($"❌ Status update failed: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid DeleteAllSessions()
    {
        try
        {
            await _account.DeleteSessions();
            LogOutput($"✅ All sessions deleted");
        }
        catch (Exception ex)
        {
            LogOutput($"❌ Delete sessions failed: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid DeleteCurrentSession()
    {
        try
        {
            await _account.DeleteSession("current");
            LogOutput($"✅ Current session deleted");
        }
        catch (Exception ex)
        {
            LogOutput($"❌ Delete session failed: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid Logout()
    {
        try
        {
            await _account.DeleteSession("current");
            _client.ClearSession();
            LogOutput($"✅ Logged out successfully");
        }
        catch (Exception ex)
        {
            LogOutput($"❌ Logout failed: {ex.Message}", LogType.Error);
        }
    }
    
    #endregion
    
    #region Databases
    
    private async UniTaskVoid ListDocuments()
    {
        try
        {
            var documents = await _databases.ListDocuments(TEST_DATABASE_ID, TEST_COLLECTION_ID);
            LogOutput($"✅ Found {documents.Total} documents");
            
            foreach (var doc in documents.Documents.Take(3))
            {
                LogOutput($"  Document {doc.Id}: {doc.CreatedAt}");
            }
            
            UpdateDocumentsList(documents.Documents);
        }
        catch (Exception ex)
        {
            LogOutput($"❌ List documents failed: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid CreateDocument()
    {
        try
        {
            var data = new PlaygroundDocument
            {
                Title = "Test Document",
                Content = "This is a test document created from Unity playground",
                Author = "Unity Playground",
                CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Completed = false,
                Priority = UnityEngine.Random.Range(1, 5)
            };
            
            var document = await _databases.CreateDocument(
                TEST_DATABASE_ID,
                TEST_COLLECTION_ID,
                ID.Unique(),
                data
            );
            
            LogOutput($"✅ Document created: {document.Id}");
            LogOutput($"  Data: {data.Title}");
        }
        catch (Exception ex)
        {
            LogOutput($"❌ Create document failed: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid GetDocument()
    {
        try
        {
            // First get a list to find an existing document
            var documents = await _databases.ListDocuments(TEST_DATABASE_ID, TEST_COLLECTION_ID);
            
            if (documents.Documents.Count == 0)
            {
                LogOutput($"❌ No documents found. Create one first!");
                return;
            }
            
            var firstDoc = documents.Documents[0];
            var document = await _databases.GetDocument(TEST_DATABASE_ID, TEST_COLLECTION_ID, firstDoc.Id);
            
            LogOutput($"✅ Retrieved document: {document.Id}");
            LogOutput($"  Created: {document.CreatedAt}");
            LogOutput($"  Updated: {document.UpdatedAt}");
            
            // Log document data
            foreach (var kvp in document.Data)
            {
                if (!kvp.Key.StartsWith("$"))
                    LogOutput($"  {kvp.Key}: {kvp.Value}");
            }
        }
        catch (Exception ex)
        {
            LogOutput($"❌ Get document failed: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid UpdateDocument()
    {
        try
        {
            var documents = await _databases.ListDocuments(TEST_DATABASE_ID, TEST_COLLECTION_ID);
            
            if (documents.Documents.Count == 0)
            {
                LogOutput($"❌ No documents found. Create one first!");
                return;
            }
            
            var firstDoc = documents.Documents[0];
            var updateData = new PlaygroundDocument
            {
                Content = "Updated from Unity playground at " + DateTime.Now,
                Completed = true,
                Author = "Unity Playground"
            };
            
            var document = await _databases.UpdateDocument(
                TEST_DATABASE_ID,
                TEST_COLLECTION_ID,
                firstDoc.Id,
                updateData
            );
            
            LogOutput($"✅ Document updated: {document.Id}");
        }
        catch (Exception ex)
        {
            LogOutput($"❌ Update document failed: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid DeleteDocument()
    {
        try
        {
            var documents = await _databases.ListDocuments(TEST_DATABASE_ID, TEST_COLLECTION_ID);
            
            if (documents.Documents.Count == 0)
            {
                LogOutput($"❌ No documents found to delete!");
                return;
            }
            
            var firstDoc = documents.Documents[0];
            await _databases.DeleteDocument(TEST_DATABASE_ID, TEST_COLLECTION_ID, firstDoc.Id);
            
            LogOutput($"✅ Document deleted: {firstDoc.Id}");
        }
        catch (Exception ex)
        {
            LogOutput($"❌ Delete document failed: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid UpsertDocument()
    {
        try
        {
            var data = new
            {
                title = "Upserted Document",
                content = "This document was created/updated using upsert",
            };
            
            var document = await _databases.UpsertDocument(
                TEST_DATABASE_ID,
                TEST_COLLECTION_ID,
                "upsert_test_doc",
                data
            );
            
            LogOutput($"✅ Document upserted: {document.Id}");
        }
        catch (Exception ex)
        {
            LogOutput($"❌ Upsert document failed: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid IncrementDocumentAttribute()
    {
        try
        {
            var documents = await _databases.ListDocuments(TEST_DATABASE_ID, TEST_COLLECTION_ID);
            
            if (documents.Documents.Count == 0)
            {
                LogOutput($"❌ No documents found. Create one first!");
                return;
            }
            
            var firstDoc = documents.Documents[0];
            var document = await _databases.IncrementDocumentAttribute(
                TEST_DATABASE_ID,
                TEST_COLLECTION_ID,
                firstDoc.Id,
                "Priority",
                1.0
            );
            
            LogOutput($"✅ Document attribute incremented: {document.Id}");
        }
        catch (Exception ex)
        {
            LogOutput($"❌ Increment attribute failed: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid DecrementDocumentAttribute()
    {
        try
        {
            var documents = await _databases.ListDocuments(TEST_DATABASE_ID, TEST_COLLECTION_ID);
            
            if (documents.Documents.Count == 0)
            {
                LogOutput($"❌ No documents found. Create one first!");
                return;
            }
            
            var firstDoc = documents.Documents[0];
            var document = await _databases.DecrementDocumentAttribute(
                TEST_DATABASE_ID,
                TEST_COLLECTION_ID,
                firstDoc.Id,
                "Priority",
                1.0
            );
            
            LogOutput($"✅ Document attribute decremented: {document.Id}");
        }
        catch (Exception ex)
        {
            LogOutput($"❌ Decrement attribute failed: {ex.Message}", LogType.Error);
        }
    }
    
    #endregion
    
    #region Storage
    
    private async UniTaskVoid ListFiles()
    {
        try
        {
            var files = await _storage.ListFiles(TEST_BUCKET_ID);
            LogOutput($"✅ Found {files.Total} files");
            
            foreach (var file in files.Files.Take(5))
            {
                LogOutput($"  File: {file.Name} ({file.SizeOriginal} bytes)");
            }
            
            UpdateFilesList(files.Files);
        }
        catch (Exception ex)
        {
            LogOutput($"❌ List files failed: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid UploadTextFile()
    {
        try
        {
            var content = $"This is a test file created from Unity Playground at {DateTime.Now}";
            var bytes = System.Text.Encoding.UTF8.GetBytes(content);
            
            var inputFile = new InputFile
            {
                Data = bytes,
                Filename = $"unity_playground_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
                MimeType = "text/plain",
                SourceType = "bytes"
            };
            
            var file = await _storage.CreateFile(
                TEST_BUCKET_ID,
                ID.Unique(),
                inputFile,
                onProgress: (progress) =>
                {
                    LogOutput($"📤 Upload progress: {progress.Progress:F1}%");
                }
            );
            
            LogOutput($"✅ File uploaded: {file.Name} ({file.SizeOriginal} bytes)");
        }
        catch (Exception ex)
        {
            LogOutput($"❌ File upload failed: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid GetFileInfo()
    {
        try
        {
            var files = await _storage.ListFiles(TEST_BUCKET_ID);
            
            if (files.Files.Count == 0)
            {
                LogOutput($"❌ No files found. Upload one first!");
                return;
            }
            
            var firstFile = files.Files[0];
            var file = await _storage.GetFile(TEST_BUCKET_ID, firstFile.Id);
            
            LogOutput($"✅ File info for: {file.Name}");
            LogOutput($"  Size: {file.SizeOriginal} bytes");
            LogOutput($"  MIME Type: {file.MimeType}");
            LogOutput($"  Created: {file.CreatedAt}");
            LogOutput($"  Signature: {file.Signature}");
        }
        catch (Exception ex)
        {
            LogOutput($"❌ Get file info failed: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid UpdateFile()
    {
        try
        {
            var files = await _storage.ListFiles(TEST_BUCKET_ID);
            
            if (files.Files.Count == 0)
            {
                LogOutput($"❌ No files found. Upload one first!");
                return;
            }
            
            var firstFile = files.Files[0];
            var file = await _storage.UpdateFile(
                TEST_BUCKET_ID,
                firstFile.Id,
                name: $"updated_{firstFile.Name}"
            );
            
            LogOutput($"✅ File updated: {file.Name}");
        }
        catch (Exception ex)
        {
            LogOutput($"❌ Update file failed: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid DownloadFile()
    {
        try
        {
            var files = await _storage.ListFiles(TEST_BUCKET_ID);
            
            if (files.Files.Count == 0)
            {
                LogOutput($"❌ No files found. Upload one first!");
                return;
            }
            
            var firstFile = files.Files[0];
            var fileData = await _storage.GetFileDownload(TEST_BUCKET_ID, firstFile.Id);
            
            LogOutput($"✅ File downloaded: {firstFile.Name}");
            LogOutput($"  Downloaded {fileData.Length} bytes");
            
            // If it's a text file, show some content
            if (firstFile.MimeType.StartsWith("text/"))
            {
                var content = System.Text.Encoding.UTF8.GetString(fileData);
                LogOutput($"  Content: {content.Substring(0, Math.Min(100, content.Length))}...");
            }
        }
        catch (Exception ex)
        {
            LogOutput($"❌ Download file failed: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid GetFilePreview()
    {
        try
        {
            var files = await _storage.ListFiles(TEST_BUCKET_ID);
            
            if (files.Files.Count == 0)
            {
                LogOutput($"❌ No files found. Upload one first!");
                return;
            }
            
            var firstFile = files.Files[0];
            var preview = await _storage.GetFilePreview(
                TEST_BUCKET_ID,
                firstFile.Id,
                width: 200,
                height: 200
            );
            
            LogOutput($"✅ File preview generated: {firstFile.Name}");
            LogOutput($"  Preview size: {preview.Length} bytes");
        }
        catch (Exception ex)
        {
            LogOutput($"❌ Get file preview failed: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid DeleteFile()
    {
        try
        {
            var files = await _storage.ListFiles(TEST_BUCKET_ID);
            
            if (files.Files.Count == 0)
            {
                LogOutput($"❌ No files found to delete!");
                return;
            }
            
            var firstFile = files.Files[0];
            await _storage.DeleteFile(TEST_BUCKET_ID, firstFile.Id);
            
            LogOutput($"✅ File deleted: {firstFile.Name}");
        }
        catch (Exception ex)
        {
            LogOutput($"❌ Delete file failed: {ex.Message}", LogType.Error);
        }
    }
    
    #endregion
    
    #region Functions
    
    private async UniTaskVoid ListFunctions()
    {
        try
        {
            await UniTask.NextFrame();
            // Function's service only provides execution-related methods in client SDK
            // List functions is typically a server-side operation
            LogOutput($"ℹ️ List functions is not available in client SDK");
            LogOutput($"   You can only execute functions and view executions");
            LogOutput($"   Configure TEST_FUNCTION_ID to test function execution");
        }
        catch (Exception ex)
        {
            LogOutput($"❌ List functions failed: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid ExecuteFunction()
    {
        try
        {
            var data = new { message = "Hello from Unity Playground!", timestamp = DateTime.UtcNow };
            var execution = await _functions.CreateExecution(
                TEST_FUNCTION_ID,
                body: System.Text.Json.JsonSerializer.Serialize(data)
            );
            
            LogOutput($"✅ Function executed: {execution.Id}");
            LogOutput($"  Status: {execution.Status}");
            LogOutput($"  Response: {execution.ResponseBody}");
        }
        catch (Exception ex)
        {
            LogOutput($"❌ Execute function failed: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid ListExecutions()
    {
        try
        {
            var executions = await _functions.ListExecutions(TEST_FUNCTION_ID);
            LogOutput($"✅ Found {executions.Total} executions");
            
            foreach (var exec in executions.Executions.Take(3))
            {
                LogOutput($"  Execution: {exec.Id} - {exec.Status}");
            }
        }
        catch (Exception ex)
        {
            LogOutput($"❌ List executions failed: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid GetExecution()
    {
        try
        {
            var executions = await _functions.ListExecutions(TEST_FUNCTION_ID);
            
            if (executions.Executions.Count == 0)
            {
                LogOutput($"❌ No executions found. Execute a function first!");
                return;
            }
            
            var firstExecution = executions.Executions[0];
            var execution = await _functions.GetExecution(TEST_FUNCTION_ID, firstExecution.Id);
            
            LogOutput($"✅ Execution info: {execution.Id}");
            LogOutput($"  Status: {execution.Status}");
            LogOutput($"  Duration: {execution.Duration} seconds");
            LogOutput($"  Response: {execution.ResponseBody}");
        }
        catch (Exception ex)
        {
            LogOutput($"❌ Get execution failed: {ex.Message}", LogType.Error);
        }
    }
    
    #endregion
    
    #region Teams
    
    private async UniTaskVoid ListTeams()
    {
        try
        {
            var teams = await this._teams.List();
            LogOutput($"✅ Found {teams.Total} teams");
            
            foreach (var team in teams.Teams.Take(5))
            {
                LogOutput($"  Team: {team.Name} ({team.Total} members)");
            }
        }
        catch (Exception ex)
        {
            LogOutput($"❌ List teams failed: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid CreateTeam()
    {
        try
        {
            var team = await _teams.Create(
                ID.Unique(),
                $"Unity Playground Team {DateTime.Now:yyyy-MM-dd}",
                new List<string> { "owner" }
            );
            
            _testTeamID = team.Id; // Store the created team ID for later use
            LogOutput($"✅ Team created: {team.Name} ({team.Id})");
        }
        catch (Exception ex)
        {
            LogOutput($"❌ Create team failed: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid GetTeam()
    {
        try
        {
            var team = await _teams.Get(_testTeamID);
            LogOutput($"✅ Team info: {team.Name}");
            LogOutput($"  ID: {team.Id}");
            LogOutput($"  Members: {team.Total}");
            LogOutput($"  Created: {team.CreatedAt}");
        }
        catch (Exception ex)
        {
            LogOutput($"❌ Get team failed: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid UpdateTeam()
    {
        try
        {
            var team = await _teams.UpdateName(
                _testTeamID,
                $"Updated Team {DateTime.Now:HH:mm:ss}"
            );
            
            LogOutput($"✅ Team updated: {team.Name}");
        }
        catch (Exception ex)
        {
            LogOutput($"❌ Update team failed: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid DeleteTeam()
    {
        try
        {
            await _teams.Delete(_testTeamID);
            LogOutput($"✅ Team deleted: {_testTeamID}");
        }
        catch (Exception ex)
        {
            LogOutput($"❌ Delete team failed: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid ListTeamMemberships()
    {
        try
        {
            var memberships = await _teams.ListMemberships(_testTeamID);
            LogOutput($"✅ Found {memberships.Total} memberships");
            
            foreach (var membership in memberships.Memberships.Take(5))
            {
                LogOutput($"  Member: {membership.UserName} - {string.Join(", ", membership.Roles)}");
            }
        }
        catch (Exception ex)
        {
            LogOutput($"❌ List memberships failed: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid CreateTeamMembership()
    {
        try
        {
            var email = GetEmail();
            var membership = await _teams.CreateMembership(
                _testTeamID,
                new List<string> { "member" },
                email: email,
                url: "https://appwrite.io"
            );
            
            LogOutput($"✅ Membership created: {membership.Id}");
        }
        catch (Exception ex)
        {
            LogOutput($"❌ Create membership failed: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid GetTeamMembership()
    {
        try
        {
            var memberships = await _teams.ListMemberships(_testTeamID);
            
            if (memberships.Memberships.Count == 0)
            {
                LogOutput($"❌ No memberships found!");
                return;
            }
            
            var firstMembership = memberships.Memberships[0];
            var membership = await _teams.GetMembership(_testTeamID, firstMembership.Id);
            
            LogOutput($"✅ Membership info: {membership.UserName}");
            LogOutput($"  Roles: {string.Join(", ", membership.Roles)}");
            LogOutput($"  Status: {membership.Confirm}");
        }
        catch (Exception ex)
        {
            LogOutput($"❌ Get membership failed: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid UpdateTeamMembership()
    {
        try
        {
            var memberships = await _teams.ListMemberships(_testTeamID);
            
            if (memberships.Memberships.Count == 0)
            {
                LogOutput($"❌ No memberships found!");
                return;
            }
            
            var firstMembership = memberships.Memberships[0];
            var membership = await _teams.UpdateMembership(
                _testTeamID,
                firstMembership.Id,
                new List<string> { "admin" }
            );
            
            LogOutput($"✅ Membership updated: {membership.UserName}");
            LogOutput($"  New roles: {string.Join(", ", membership.Roles)}");
        }
        catch (Exception ex)
        {
            LogOutput($"❌ Update membership failed: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid DeleteTeamMembership()
    {
        try
        {
            var memberships = await _teams.ListMemberships(_testTeamID);
            
            if (memberships.Memberships.Count == 0)
            {
                LogOutput($"❌ No memberships found!");
                return;
            }
            
            var firstMembership = memberships.Memberships[0];
            await _teams.DeleteMembership(_testTeamID, firstMembership.Id);
            
            LogOutput($"✅ Membership deleted: {firstMembership.Id}");
        }
        catch (Exception ex)
        {
            LogOutput($"❌ Delete membership failed: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid GetTeamPrefs()
    {
        try
        {
            var prefs = await _teams.GetPrefs(_testTeamID);
            LogOutput($"✅ Team preferences retrieved");
            LogOutput($"  Data: {System.Text.Json.JsonSerializer.Serialize(prefs.Data)}");
        }
        catch (Exception ex)
        {
            LogOutput($"❌ Get team prefs failed: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid UpdateTeamPrefs()
    {
        try
        {
            var prefsData = new { theme = "dark", notifications = true, language = "en" };
            var prefs = await _teams.UpdatePrefs(_testTeamID, prefsData);
            LogOutput($"✅ Team preferences updated {prefs.Data.Keys.Count}");
        }
        catch (Exception ex)
        {
            LogOutput($"❌ Update team prefs failed: {ex.Message}", LogType.Error);
        }
    }
    
    #endregion
    
    #region Locale
    
    private async UniTaskVoid GetUserLocation()
    {
        try
        {
            var location = await _locale.Get();
            LogOutput("✅ User location:");
            LogOutput($"  IP: {location.Ip}");
            LogOutput($"  Country: {location.Country} ({location.CountryCode})");
            LogOutput($"  Continent: {location.Continent} ({location.ContinentCode})");
            LogOutput($"  Currency: {location.Currency}");
            LogOutput($"  EU Member: {location.Eu}");
        }
        catch (Exception ex)
        {
            LogOutput($"❌ Get location failed: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid ListCountries()
    {
        try
        {
            var countries = await _locale.ListCountries();
            LogOutput($"✅ Found {countries.Total} countries");
            
            foreach (var country in countries.Countries.Take(5))
            {
                LogOutput($"  {country.Name} ({country.Code})");
            }
        }
        catch (Exception ex)
        {
            LogOutput($"❌ List countries failed: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid ListCountriesEU()
    {
        try
        {
            await UniTask.NextFrame();

            var countries = await _locale.ListCountriesEU();
            LogOutput($"✅ Found {countries.Total} EU countries");
            
            foreach (var country in countries.Countries.Take(5))
            {
                LogOutput($"  {country.Name} ({country.Code})");
            }
        }
        catch (Exception ex)
        {
            LogOutput($"❌ List EU countries failed: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid ListContinents()
    {
        try
        {
            var continents = await _locale.ListContinents();
            LogOutput($"✅ Found {continents.Total} continents");
            
            foreach (var continent in continents.Continents)
            {
                LogOutput($"  {continent.Name} ({continent.Code})");
            }
        }
        catch (Exception ex)
        {
            LogOutput($"❌ List continents failed: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid ListCurrencies()
    {
        try
        {
            var currencies = await _locale.ListCurrencies();
            LogOutput($"✅ Found {currencies.Total} currencies");
            
            foreach (var currency in currencies.Currencies.Take(5))
            {
                LogOutput($"  {currency.Name} ({currency.Code}) - {currency.Symbol}");
            }
        }
        catch (Exception ex)
        {
            LogOutput($"❌ List currencies failed: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid ListLanguages()
    {
        try
        {
            var languages = await _locale.ListLanguages();
            LogOutput($"✅ Found {languages.Total} languages");
            
            foreach (var language in languages.Languages.Take(5))
            {
                LogOutput($"  {language.Name} ({language.Code})");
            }
        }
        catch (Exception ex)
        {
            LogOutput($"❌ List languages failed: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid ListCountriesPhones()
    {
        try
        {
            var phones = await _locale.ListCountriesPhones();
            LogOutput($"✅ Found {phones.Total} phone codes");
            
            foreach (var phone in phones.Phones.Take(5))
            {
                LogOutput($"  {phone.CountryName}: {phone.CountryCode}");
            }
        }
        catch (Exception ex)
        {
            LogOutput($"❌ List phone codes failed: {ex.Message}", LogType.Error);
        }
    }
    
    private async UniTaskVoid ListLocaleCodes()
    {
        try
        {
            var codes = await _locale.ListCodes();
            LogOutput($"✅ Found {codes.Total} locale codes");
            
            foreach (var code in codes.LocaleCodes.Take(5))
            {
                LogOutput($"  {code.Name} ({code.Code})");
            }
        }
        catch (Exception ex)
        {
            LogOutput($"❌ List locale codes failed: {ex.Message}", LogType.Error);
        }
    }
    
    #endregion
    
    #region Realtime
    
    private async UniTaskVoid TestRealtimeConnection()
    {
        if (_realtime == null)
        {
            LogOutput($"❌ Realtime not initialized", LogType.Error);
            return;
        }
        await UniTask.NextFrame();
        try
        {
            LogOutput($"✅ Realtime connection active");
            LogOutput($"  Channels subscribed: {_realtime.Channels.Count}");
            LogOutput($"  Connection status: {(_realtime.IsConnected ? "Connected" : "Disconnected")}");
        }
        catch (Exception ex)
        {
            LogOutput($"❌ Realtime test failed: {ex.Message}", LogType.Error);
        }
    }
    
    #endregion
    
    #region Utilities
    
    private async UniTaskVoid PingServer()
    {
        try
        {
            var pingResult = await _client.Ping();
            LogOutput($"✅ Server ping successful: {pingResult}");
        }
        catch (Exception ex)
        {
            LogOutput($"❌ Server ping failed: {ex.Message}", LogType.Error);
        }
    }
    
    #endregion
    
    #region Helper Methods
    
    private void SafeExecuteAsync(Func<UniTaskVoid> action, string actionName)
    {
        try
        {
            SetStatus($"Executing: {actionName}...");
            UniTask.Void(action);
            SetStatus("Ready");
        }
        catch (Exception ex)
        {
            LogOutput($"❌ {actionName} failed: {ex.Message}", LogType.Error);
            SetStatus("Error occurred");
        }
    }
    
    private async UniTaskVoid SeeCookies()
    {
        await UniTask.NextFrame();
        LogOutput($"Cookies count: {_client.CookieContainer.Count}");
        LogOutput($"Cookies: {_client.CookieContainer.GetContents()}");
    }

    private async UniTaskVoid ClearOutput()
    {
        await UniTask.NextFrame();
        outputText.text = "";
    }
    
    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = $"Status: {message}";
    }

    private enum LogType
    {
        Info,
        Warning,
        Error
    }

    private void LogOutput(string message, LogType type = LogType.Info)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var logEntry = $"[{timestamp}] {message}";

        if (outputText != null)
        {
            string colorTag = type switch
            {
                LogType.Warning => "<color=#FFD600>", // Желтый
                LogType.Error => "<color=#FF5252>",   // Красный
                _ => "<color=#FFFFFF>"                 // Белый (обычный)
            };
            string coloredEntry = $"{colorTag}{logEntry}</color>";
            outputText.text = coloredEntry + "\n" + outputText.text;

            // Limit output to prevent performance issues
            var lines = outputText.text.Split('\n');
            if (lines.Length > 500)
            {
                outputText.text = string.Join("\n", lines.Take(500));
            }
        }

        switch (type)
        {
            case LogType.Warning:
                Debug.LogWarning(logEntry);
                break;
            case LogType.Error:
                Debug.LogError(logEntry);
                break;
            default:
                Debug.Log(logEntry);
                break;
        }
    }
    
    private string GetEmail()
    {
        return emailField?.text ?? "playground@ma.com";
    }
    
    private string GetPassword()
    {
        return passwordField?.text ?? "playground123";
    }
    
    private string GetName()
    {
        return nameField?.text ?? "Unity Playground User";
    }
    
    private void UpdateFilesList(List<File> files)
    {
        if (filesList == null) return;
        
        // Clear existing items
        foreach (Transform child in filesList)
        {
            Destroy(child.gameObject);
        }
        
        // Add new items
        foreach (var file in files.Take(10))
        {
            var item = new GameObject($"File_{file.Id}");
            item.transform.SetParent(filesList);
            
            var text = item.AddComponent<TextMeshProUGUI>();
            text.text = $"{file.Name} ({file.SizeOriginal} bytes)";
            text.fontSize = 16f;
        }
    }
    
    private void UpdateDocumentsList(List<Document> documents)
    {
        if (documentsList == null) return;
        
        // Clear existing items
        foreach (Transform child in documentsList)
        {
            Destroy(child.gameObject);
        }
        
        // Add new items
        foreach (var doc in documents.Take(10))
        {
            var item = new GameObject($"Doc_{doc.Id}");
            item.transform.SetParent(documentsList);
            
            var text = item.AddComponent<TextMeshProUGUI>();
            var title = doc.Data.TryGetValue("title", out var value) ? value.ToString() : "Untitled";
            text.text = $"{title} (ID: {doc.Id.Substring(0, 8)}...)";
            text.fontSize = 16f;
        }
    }
    
    private void AddRealtimeEvent(string eventText)
    {
        if (realtimeEventsList == null) return;
        
        var item = new GameObject($"Event_{DateTime.Now.Ticks}");
        item.transform.SetParent(realtimeEventsList);
        
        var text = item.AddComponent<TextMeshProUGUI>();
        text.text = eventText;
        text.fontSize = 16f;
        
        // Limit events list
        if (realtimeEventsList.childCount > 20)
        {
            for (int i = 20; i < realtimeEventsList.childCount; i++)
            {
                Destroy(realtimeEventsList.GetChild(i).gameObject);
            }
        }
    }
    
    private async UniTaskVoid ClearRealtimeEvents()
    {
        if (realtimeEventsList == null) return;
        
        await UniTask.NextFrame();

        foreach (Transform child in realtimeEventsList)
        {
            Destroy(child.gameObject);
        }
        
        LogOutput("✅ Realtime events cleared");
    }
    
    #endregion
    
    // Test model for database operations
    [Serializable]
    public class PlaygroundDocument
    {
        [JsonPropertyName("Title")]
        public string Title { get; set; }
        
        [JsonPropertyName("Content")]
        public string Content { get; set; }
        
        [JsonPropertyName("Author")]
        public string Author { get; set; }
        
        [JsonPropertyName("CreatedAt")]
        public string CreatedAt { get; set; }
        
        [JsonPropertyName("Completed")]
        public bool Completed { get; set; }
        
        [JsonPropertyName("Priority")]
        public int Priority { get; set; }
    }
}
