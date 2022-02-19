using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;
using Photon.Realtime;

public class Launcher : MonoBehaviourPunCallbacks
{
    public static Launcher Instance;

    [SerializeField] TMP_InputField roomNameInputField;
    [SerializeField] TMP_Text errorText;
    [SerializeField] TMP_Text roomNameText;
    [SerializeField] Transform roomListContent;
    [SerializeField] GameObject roomListItemPrefab;
    [SerializeField] Transform playerListContent;
    [SerializeField] GameObject PlayerListItemPrefab;
    [SerializeField] GameObject startGameButton;

    private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();

    void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // 使用 Settings連接伺服器
        PhotonNetwork.ConnectUsingSettings();
    }
    
    // 當連線至Photon Master Server之後會自動呼叫
    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected To Master");
        // 加入大廳，在這裡可以創建Room或是加入已創建的Room
        PhotonNetwork.JoinLobby();
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    // 當加入大廳之後會自動呼叫
    public override void OnJoinedLobby()
    {
        Debug.Log("Joined To Lobby");
        MenuManager.Instance.OpenMenu("Title");
        PhotonNetwork.NickName = "Player " + Random.Range(0, 1000).ToString("0000");
    }


    public void CreateRoom()
    {
        if (string.IsNullOrEmpty(roomNameInputField.text))
        {
            return;
        }
        PhotonNetwork.CreateRoom(roomNameInputField.text, new RoomOptions { IsVisible = true });
        MenuManager.Instance.OpenMenu("Loading");
    }

    // 當加入Room的時候會呼叫(不論是加入或是創建Room)
    public override void OnJoinedRoom()
    {
        MenuManager.Instance.OpenMenu("Room");
        roomNameText.text = PhotonNetwork.CurrentRoom.Name;

        Player[] players = PhotonNetwork.PlayerList;

        foreach (Transform child in playerListContent)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < players.Length; i++)
        {
            Instantiate(PlayerListItemPrefab, playerListContent).GetComponent<PlayerListItem>().SetUp(players[i]);
        }

        startGameButton.SetActive(PhotonNetwork.IsMasterClient);
    }

    // 當創建Room失敗時呼叫
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        errorText.text = "Room Creation Failed: " + message;
        Debug.LogError("Room Creation Failed: " + message);
        MenuManager.Instance.OpenMenu("Error");
    }

    public void StartGame()
    {
        PhotonNetwork.LoadLevel(1);
    }

    public void JoinRoom(RoomInfo info)
    {
        PhotonNetwork.JoinRoom(info.Name);
        MenuManager.Instance.OpenMenu("Loading");
        cachedRoomList.Clear();
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
        MenuManager.Instance.OpenMenu("Loading");
    }

    // 當離開房間
    public override void OnLeftRoom()
    {
        MenuManager.Instance.OpenMenu("Title");
        cachedRoomList.Clear();
    }

    // 當有房間更新
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        UpdateCachedRoomList(roomList);

        foreach (Transform trans in roomListContent)
        {
            if (trans != null)
                if (trans.gameObject != null)
                    Destroy(trans.gameObject);
        }

        foreach (var info in cachedRoomList)
        {
            Instantiate(roomListItemPrefab, roomListContent).GetComponent<RoomListItem>().SetUp(info.Value);
        }
    }

    // 非教學內容(用於修復RoomList Bug)
    private void UpdateCachedRoomList(List<RoomInfo> roomList)
    {
        for (int i = 0; i < roomList.Count; i++)
        {
            RoomInfo info = roomList[i];
            if (info.RemovedFromList)
            {
                cachedRoomList.Remove(info.Name);
            }
            else
            {
                cachedRoomList[info.Name] = info;
            }
        }
    }

    // 當有玩家加入房間
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Instantiate(PlayerListItemPrefab, playerListContent).GetComponent<PlayerListItem>().SetUp(newPlayer);
    }
}
