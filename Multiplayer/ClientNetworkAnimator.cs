using UnityEngine;
using Unity.Netcode.Components;

public class ClientNetworkAnimator : NetworkAnimator
{
    protected override bool OnIsServerAuthoritative()
    {
        // Make the client authoritative for its own transform
        return false;
    }
}