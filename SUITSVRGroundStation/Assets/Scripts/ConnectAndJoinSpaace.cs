
#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;

using Photon.Pun;
using Photon.Realtime;

namespace Photon.Pun.UtilityScripts
{
    /// <summary>Simple component to call ConnectUsingSettings and to get into a PUN room easily.</summary>
    /// <remarks>A custom inspector provides a button to connect in PlayMode, should AutoConnect be false.</remarks>
    public class ConnectAndJoinSpaace : MonoBehaviourPunCallbacks
    {
        /// <summary>Connect automatically? If false you can set this to true later on or call ConnectUsingSettings in your own scripts.</summary>
        public bool AutoConnect = true;

        /// <summary>Used as PhotonNetwork.GameVersion.</summary>
        public byte Version = 1;


        public void Start()
        {
            if (this.AutoConnect)
            {
                this.ConnectNow();
            }
        }

        public void ConnectNow()
        {
            Debug.Log("ConnectAndJoinSpaace.ConnectNow() will now call: PhotonNetwork.ConnectUsingSettings().");
            PhotonNetwork.ConnectUsingSettings();
            PhotonNetwork.GameVersion = this.Version+"";
            
        }


        // below, we implement some callbacks of the Photon Realtime API.
        // Being a MonoBehaviourPunCallbacks means, we can override the few methods which are needed here.


        public override void OnConnectedToMaster()
        {
            Debug.Log("OnConnectedToMaster() was called by PUN. Now this client is connected and could join a room. Calling: PhotonNetwork.JoinOrCreateRoom(Spaace, roomOptions, TypedLobby.Default);");
            //PhotonNetwork.JoinRandomRoom();
            RoomOptions roomOptions = new RoomOptions();
            roomOptions.IsVisible = true;
            roomOptions.MaxPlayers = 4;
            roomOptions.IsOpen = true;           
            PhotonNetwork.JoinOrCreateRoom("Spaace", roomOptions, TypedLobby.Default);
        }

        public override void OnJoinedLobby()
        {
            Debug.Log("OnJoinedLobby(). This client is connected. This script now calls: PhotonNetwork.JoinOrCreateRoom(Spaace, roomOptions, TypedLobby.Default);");

            RoomOptions roomOptions = new RoomOptions();
            roomOptions.IsVisible = true;
            roomOptions.MaxPlayers = 4;
            roomOptions.IsOpen = true;
            PhotonNetwork.JoinOrCreateRoom("Spaace", roomOptions, TypedLobby.Default);
        }

        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            Debug.Log("OnJoinRandomFailed() was called by PUN. No random room available, so we create one. Calling: PhotonNetwork.CreateRoom(null, new RoomOptions() {maxPlayers = 4}, null);");
            PhotonNetwork.CreateRoom(null, new RoomOptions() { MaxPlayers = 4 }, null);
        }

        // the following methods are implemented to give you some context. re-implement them as needed.
        public override void OnDisconnected(DisconnectCause cause)
        {
			Debug.Log("OnDisconnected("+cause+")");
        }

        public override void OnJoinedRoom()
        {
            Debug.Log("OnJoinedRoom() called by PUN. Now this client is in a room. From here on, your game would be running.");
        }
    }
}
