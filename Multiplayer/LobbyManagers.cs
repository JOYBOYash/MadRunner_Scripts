using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;

public class LobbyManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject roomPanel;
    public GameObject difficultyPanel;
    public GameObject lobbyPanel;

    [Header("Room Panel")]
    public TMP_InputField playerNameInput;
    public TMP_InputField roomCodeInput;
    public Button createRoomButton;
    public Button joinRoomButton;

    [Header("Difficulty Panel")]
    public TMP_Dropdown difficultyDropdown;
    public Button difficultyConfirmButton;

    [Header("Lobby Panel")]
    public TMP_Text lobbyRoomCodeText;
    public TMP_Text hostNameText;
    public TMP_Text clientNameText;
    public Toggle hostReadyToggle;
    public Toggle clientReadyToggle;
    public Button startGameButton;
    public Button backButton;
    public string SceneName = "Game";

    [Header("Prefabs")]
    public GameObject roomSettingsPrefab;   // MUST be assigned in Inspector AND be in NetworkPrefabs list

    private RoomSettings roomSettings;
    private NetworkManager net;

    void Awake()
    {
        net = NetworkManager.Singleton;

        if (net == null)
        {
            Debug.LogError("❌ NetworkManager not found in scene! Add a NetworkManager object.");
        }
    }

    void Start()
    {
        createRoomButton.onClick.AddListener(CreateRoom);
        joinRoomButton.onClick.AddListener(JoinRoom);
        difficultyConfirmButton.onClick.AddListener(ConfirmDifficulty);
        startGameButton.onClick.AddListener(StartMatch);
        backButton.onClick.AddListener(ReturnToRoomPanel);

        roomPanel.SetActive(true);
        difficultyPanel.SetActive(false);
        lobbyPanel.SetActive(false);
    }

    // ============================================================
    //                     HOST FLOW
    // ============================================================

    void CreateRoom()
    {
        // ---------------- SAFETY CHECKS ----------------
        if (net == null)
        {
            Debug.LogError("❌ NetworkManager.Singleton is NULL. Fix your scene setup.");
            return;
        }

        if (roomSettingsPrefab == null)
        {
            Debug.LogError("❌ roomSettingsPrefab is NOT assigned in inspector.");
            return;
        }

        var no = roomSettingsPrefab.GetComponent<NetworkObject>();
        if (no == null)
        {
            Debug.LogError("❌ roomSettingsPrefab has NO NetworkObject! Add a NetworkObject component.");
            return;
        }

        // Start Host
        if (!net.StartHost())
        {
            Debug.LogError("❌ Failed to start HOST.");
            return;
        }

        // Instantiate the networked RoomSettings
        GameObject go = Instantiate(roomSettingsPrefab);
        var netObj = go.GetComponent<NetworkObject>();

        if (netObj == null)
        {
            Debug.LogError("❌ Spawned RoomSettings has no NetworkObject. This should NEVER happen.");
            Destroy(go);
            return;
        }

        netObj.Spawn(true);

        roomSettings = go.GetComponent<RoomSettings>();
        if (roomSettings == null)
        {
            Debug.LogError("❌ Spawned RoomSettings has no RoomSettings component!");
            return;
        }

        // Set host details
        string hostName = string.IsNullOrWhiteSpace(playerNameInput.text) ? "Host" : playerNameInput.text;
        roomSettings.SetHostNameServerRpc(hostName);
        roomSettings.GenerateRoomCodeServerRpc();

        // Move to choosing difficulty
        roomPanel.SetActive(false);
        difficultyPanel.SetActive(true);
    }

    void ConfirmDifficulty()
    {
        if (roomSettings == null)
        {
            roomSettings = FindObjectOfType<RoomSettings>();
            if (roomSettings == null)
            {
                Debug.LogError("❌ RoomSettings not found when confirming difficulty!");
                return;
            }
        }

        roomSettings.SetDifficultyServerRpc(difficultyDropdown.value);

        OpenLobbyPanel();
    }

    // ============================================================
    //                     CLIENT FLOW
    // ============================================================

    void JoinRoom()
    {
        if (net == null)
        {
            Debug.LogError("❌ NetworkManager missing!");
            return;
        }

        if (!net.StartClient())
        {
            Debug.LogError("❌ Failed to start CLIENT.");
            return;
        }

        net.OnClientConnectedCallback += OnClientConnected;
    }

    void OnClientConnected(ulong id)
    {
        if (id != net.LocalClientId)
            return;

        net.OnClientConnectedCallback -= OnClientConnected;

        Invoke(nameof(LoadLobbyAsClient), 0.4f);
    }

    void LoadLobbyAsClient()
    {
        roomSettings = FindObjectOfType<RoomSettings>();

        if (roomSettings == null)
        {
            Debug.Log("⌛ Waiting for RoomSettings to spawn...");
            Invoke(nameof(LoadLobbyAsClient), 0.3f);
            return;
        }

        string clientName = string.IsNullOrWhiteSpace(playerNameInput.text) ? "Client" : playerNameInput.text;
        roomSettings.SetClientNameServerRpc(clientName);

        roomPanel.SetActive(false);
        difficultyPanel.SetActive(false);
        lobbyPanel.SetActive(true);

        SetupLobbyUI();
    }

    // ============================================================
    //                     LOBBY UI SYNC
    // ============================================================

    void OpenLobbyPanel()
    {
        difficultyPanel.SetActive(false);
        roomPanel.SetActive(false);
        lobbyPanel.SetActive(true);
        SetupLobbyUI();
    }

    void SetupLobbyUI()
    {
        if (roomSettings == null)
            roomSettings = FindObjectOfType<RoomSettings>();

        if (roomSettings == null)
        {
            Debug.LogError("❌ SetupLobbyUI called but RoomSettings is NULL!");
            return;
        }

        lobbyRoomCodeText.text = "ROOM: " + roomSettings.RoomCode.Value.ToString();

        if (net.IsHost)
        {
            hostReadyToggle.gameObject.SetActive(true);
            clientReadyToggle.gameObject.SetActive(false);
            startGameButton.gameObject.SetActive(true);
        }
        else
        {
            hostReadyToggle.gameObject.SetActive(false);
            clientReadyToggle.gameObject.SetActive(true);
            startGameButton.gameObject.SetActive(false);
        }

        // Clear listeners
        hostReadyToggle.onValueChanged.RemoveAllListeners();
        clientReadyToggle.onValueChanged.RemoveAllListeners();

        hostReadyToggle.onValueChanged.AddListener(v =>
        {
            if (net.IsHost)
                roomSettings.SetHostReadyServerRpc(v);
        });

        clientReadyToggle.onValueChanged.AddListener(v =>
        {
            if (!net.IsHost)
                roomSettings.SetClientReadyServerRpc(v);
        });

        roomSettings.OnLobbyUpdated -= RefreshLobbyUI;
        roomSettings.OnLobbyUpdated += RefreshLobbyUI;

        RefreshLobbyUI();
    }

    void RefreshLobbyUI()
    {
        if (roomSettings == null) return;

        hostNameText.text =
            $"{roomSettings.HostName.Value} (Host) - {(roomSettings.HostReady.Value ? "Ready" : "Waiting")}";

        string clientName = roomSettings.ClientName.Value.ToString();
        if (string.IsNullOrEmpty(clientName))
            clientNameText.text = "Waiting for Client...";
        else
            clientNameText.text =
                $"{clientName} - {(roomSettings.ClientReady.Value ? "Ready" : "Waiting")}";
    }

    // ============================================================
    //                       START MATCH
    // ============================================================

    void StartMatch()
    {
        if (!net.IsHost) return;

        if (roomSettings.HostReady.Value && roomSettings.ClientReady.Value)
        {
            net.SceneManager.LoadScene(SceneName,
                UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
        else
        {
            Debug.Log("⚠ Both players must be ready.");
        }
    }

    void ReturnToRoomPanel()
    {
        lobbyPanel.SetActive(false);
        roomPanel.SetActive(true);
    }

    private void OnDestroy()
    {
        if (roomSettings != null)
            roomSettings.OnLobbyUpdated -= RefreshLobbyUI;
    }
}
