using System.Collections;
using System.Collections.Generic;
using RiptideNetworking;
using Multiplayer;
using UnityEngine;

public class Player : MonoBehaviour
{
    public static Dictionary<ushort, Player> PlayerMap = new Dictionary<ushort, Player>();

    public ushort Id { get; private set; }
    public string Username { get; private set; }
    public PlayerMovement Movement => movement;

    [SerializeField] private PlayerMovement movement;

    private void OnDestroy()
    {
        PlayerMap.Remove(Id);
    }

    public static void Spawn(ushort id, string username)
    {
        foreach (Player otherPlayer in PlayerMap.Values)
            otherPlayer.SendSpawned(id);

        Player player = Instantiate(GameLogic.Singleton.PlayerPrefab, new Vector3(0f, 1f, 0f), Quaternion.identity).GetComponent<Player>();
        player.name = $"Player_{id}";
        player.Username = username;
        player.Id = id;

        player.SendSpawned();
        PlayerMap.Add(id, player);
    }

    #region Messages
    private void SendSpawned()
    {
        NetworkManager.Singleton.Server.SendToAll(AddSpawnData(Message.Create(MessageSendMode.reliable, ServerToClientId.playerSpawned)));
    }

    private void SendSpawned(ushort toClientId)
    {
        NetworkManager.Singleton.Server.Send(AddSpawnData(Message.Create(MessageSendMode.reliable, ServerToClientId.playerSpawned)), toClientId);
    }

    private Message AddSpawnData(Message message)
    {
        message.AddUShort(Id);
        message.AddString(Username);
        message.AddVector3(transform.position);
        return message;
    }

    [MessageHandler((ushort) ClientToServerId.auth)]
    private static void Authenticate(ushort fromClientId, Message message)
    {
        string userName = message.GetString();

        Spawn(fromClientId, userName);
    }

    [MessageHandler((ushort)ClientToServerId.input)]
    private static void Input(ushort fromClientId, Message message)
    {
        if (PlayerMap.TryGetValue(fromClientId, out Player player))
        {
            ushort tick = message.GetUShort();
            bool[] inputs = message.GetBools(6);
            Vector3 camForward = message.GetVector3();

            player.Movement.SetInputs(inputs, camForward);
        }
    }
    #endregion
}
