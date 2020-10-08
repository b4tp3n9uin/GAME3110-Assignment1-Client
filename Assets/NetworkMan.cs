using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Net.Sockets;
using System.Net;


public class NetworkMan : MonoBehaviour
{
    public UdpClient udp;
    public GameObject playerGO;

    public string myAddress;
    public Dictionary<string, GameObject> currentPlayers;
    public List<string> newPlayers, droppedPlayers;
    public GameState lastestGameState;
    public ListOfPlayers initialSetofPlayers;

    public MessageType latestMessage;


    void Start()
    {
        newPlayers = new List<string>();
        droppedPlayers = new List<string>();
        currentPlayers = new Dictionary<string, GameObject>();
        initialSetofPlayers = new ListOfPlayers();

        udp = new UdpClient();
        Debug.Log("Connecting...");
        udp.Connect("localhost", 12345);
        Byte[] sendBytes = Encoding.ASCII.GetBytes("connect");
        udp.Send(sendBytes, sendBytes.Length);
        udp.BeginReceive(new AsyncCallback(OnReceived), udp);

        InvokeRepeating("HeartBeat", 1, 1);

        InvokeRepeating("PositionRefresh", 1, 0.1f);
    }

    void OnDestroy()
    {
        udp.Dispose();
    }

    [Serializable]
    public struct receivedPosition
    {
        public float X;
        public float Y;
        public float Z;
    }

    [Serializable]
    public class Player
    {
        public string id;
        public receivedPosition position;
    }


    [Serializable]
    public class ListOfPlayers
    {
        public Player[] players;

        public ListOfPlayers()
        {
            players = new Player[0];
        }
    }
    [Serializable]
    public class ListOfDroppedPlayers
    {
        public string[] droppedPlayers;
    }

    [Serializable]
    public class GameState
    {
        public int pktID;
        public Player[] players;
    }

    [Serializable]
    public class UpdatePosition
    {
        public commands cmd;
        public receivedPosition position;
    }

    [Serializable]
    public class HeartBeatCMD
    {
        public commands cmd;
    }

    [Serializable]
    public class RequestCMD
    {
        public commands cmd;
    }
    
    [Serializable]
    public class MessageType
    {
        public commands cmd;
    }
    
    public enum commands
    {
        PLAYER_CONNECTED,       // 0
        GAME_UPDATE,            // 1
        PLAYER_DISCONNECTED,    // 2
        CONNECTION_APPROVED,    // 3
        LIST_OF_PLAYERS,        // 4
        REFRESH_POSITION,       // 5 
        HEART_BEAT,             // 6
        SEND_REQUEST            // 7
    };

    void OnReceived(IAsyncResult result)
    {
        UdpClient socket = result.AsyncState as UdpClient;
        
        IPEndPoint source = new IPEndPoint(0, 0);
        
        byte[] message = socket.EndReceive(result, ref source);
        
        string returnData = Encoding.ASCII.GetString(message);
        // Debug.Log("Got this: " + returnData);

        latestMessage = JsonUtility.FromJson<MessageType>(returnData);

        Debug.Log(returnData);
        try
        {
            switch (latestMessage.cmd)
            {
                case commands.PLAYER_CONNECTED:
                    ListOfPlayers latestPlayer = JsonUtility.FromJson<ListOfPlayers>(returnData);
                    Debug.Log(returnData);
                    foreach (Player player in latestPlayer.players)
                    {
                        newPlayers.Add(player.id);
                    }
                    break;
                case commands.GAME_UPDATE:
                    lastestGameState = JsonUtility.FromJson<GameState>(returnData);
                    break;
                case commands.PLAYER_DISCONNECTED:
                    ListOfDroppedPlayers latestDroppedPlayer = JsonUtility.FromJson<ListOfDroppedPlayers>(returnData);
                    foreach (string player in latestDroppedPlayer.droppedPlayers)
                    {
                        droppedPlayers.Add(player);
                    }
                    break;
                case commands.CONNECTION_APPROVED:
                    ListOfPlayers myPlayer = JsonUtility.FromJson<ListOfPlayers>(returnData);
                    Debug.Log(returnData);
                    foreach (Player player in myPlayer.players)
                    {
                        newPlayers.Add(player.id);
                        myAddress = player.id;
                    }
                    break;
                case commands.LIST_OF_PLAYERS:
                    initialSetofPlayers = JsonUtility.FromJson<ListOfPlayers>(returnData);
                    break;
                default:
                    Debug.Log("Error: " + returnData);
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
        socket.BeginReceive(new AsyncCallback(OnReceived), socket);
    }

    void SpawnPlayers()
    {
        if (newPlayers.Count > 0)
        {
            foreach (string playerID in newPlayers)
            {
                currentPlayers.Add(playerID, Instantiate(playerGO, new Vector3(0, 0, 0), Quaternion.identity));
                currentPlayers[playerID].name = playerID;
            }
            newPlayers.Clear();
        }
        if (initialSetofPlayers.players.Length > 0)
        {
            Debug.Log(initialSetofPlayers);
            foreach (Player player in initialSetofPlayers.players)
            {
                if (player.id == myAddress)
                    continue;
                currentPlayers.Add(player.id, Instantiate(playerGO, new Vector3(0, 0, 0), Quaternion.identity));
                currentPlayers[player.id].transform.position = new Vector3(player.position.X, player.position.Y, player.position.Z);
                currentPlayers[player.id].name = player.id;
            }
            initialSetofPlayers.players = new Player[0];
        }
    }

    void UpdatePlayers()
    {
        if (lastestGameState.players.Length > 0)
        {
            foreach (NetworkMan.Player player in lastestGameState.players)
            {
                string playerID = player.id;
                currentPlayers[player.id].transform.position = new Vector3(player.position.X, player.position.Y, player.position.Z);
            }
            lastestGameState.players = new Player[0];
        }
    }

    void DestroyPlayers()
    {
        if (droppedPlayers.Count > 0)
        {
            foreach (string playerID in droppedPlayers)
            {
                Debug.Log(playerID);
                Debug.Log(currentPlayers[playerID]);
                Destroy(currentPlayers[playerID].gameObject);
                currentPlayers.Remove(playerID);
            }
            droppedPlayers.Clear();
        }
    }

    void HeartBeat()
    {
        HeartBeatCMD heartBeatCMD = new HeartBeatCMD();
        heartBeatCMD.cmd = commands.HEART_BEAT;
        Byte[] sendBytes = Encoding.ASCII.GetBytes(JsonUtility.ToJson(heartBeatCMD));
        udp.Send(sendBytes, sendBytes.Length);
    }
    
    void PositionRefresh()
    {
        UpdatePosition posCMD = new UpdatePosition();
        posCMD.cmd = commands.REFRESH_POSITION;
        posCMD.position.X = currentPlayers[myAddress].transform.position.x;
        posCMD.position.Y = currentPlayers[myAddress].transform.position.y;
        posCMD.position.Z = currentPlayers[myAddress].transform.position.z;
        Byte[] sendBytes = Encoding.ASCII.GetBytes(JsonUtility.ToJson(posCMD));
        udp.Send(sendBytes, sendBytes.Length);
    } 
    
    void Update()
    {
        SpawnPlayers();
        UpdatePlayers();
        DestroyPlayers();
    }
}