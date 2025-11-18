using Unity.Netcode;
using UnityEngine;
using System;
using Unity.Collections;
using UnityEngine.UI;
using System.Collections.Generic;


public class RoomSettings : NetworkBehaviour
{
    public NetworkVariable<int> Difficulty = new(writePerm: NetworkVariableWritePermission.Server);

    public NetworkVariable<FixedString32Bytes> RoomCode = new(writePerm: NetworkVariableWritePermission.Server);
    public NetworkVariable<FixedString32Bytes> HostName = new(writePerm: NetworkVariableWritePermission.Server);
    public NetworkVariable<FixedString32Bytes> ClientName = new(writePerm: NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> HostReady = new(writePerm: NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> ClientReady = new(writePerm: NetworkVariableWritePermission.Server);

    public event Action OnLobbyUpdated;

    public override void OnNetworkSpawn()
    {
        Difficulty.OnValueChanged += (a, b) => OnLobbyUpdated?.Invoke();
        RoomCode.OnValueChanged += (a, b) => OnLobbyUpdated?.Invoke();
        HostName.OnValueChanged += (a, b) => OnLobbyUpdated?.Invoke();
        ClientName.OnValueChanged += (a, b) => OnLobbyUpdated?.Invoke();
        HostReady.OnValueChanged += (a, b) => OnLobbyUpdated?.Invoke();
        ClientReady.OnValueChanged += (a, b) => OnLobbyUpdated?.Invoke();
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetDifficultyServerRpc(int diff) => Difficulty.Value = diff;

    [ServerRpc(RequireOwnership = false)]
    public void GenerateRoomCodeServerRpc()
    {
        RoomCode.Value = UnityEngine.Random.Range(100000, 999999).ToString();
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetHostNameServerRpc(string name) => HostName.Value = name;

    [ServerRpc(RequireOwnership = false)]
    public void SetClientNameServerRpc(string name) => ClientName.Value = name;

    [ServerRpc(RequireOwnership = false)]
    public void SetHostReadyServerRpc(bool value) => HostReady.Value = value;

    [ServerRpc(RequireOwnership = false)]
    public void SetClientReadyServerRpc(bool value) => ClientReady.Value = value;

    public bool BothReady => HostReady.Value && ClientReady.Value;
}
