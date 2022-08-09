using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RiptideNetworking;
using Multiplayer;

public class Player : MonoBehaviour
{
    public static Dictionary<ushort, Player> PlayerMap = new Dictionary<ushort, Player>();

    public ushort Id { get; private set; }
    public bool IsLocal { get; private set; }
    public string Username { get; private set; }
    [SerializeField] private Transform camTransform;
    [SerializeField] private Interpolator interpolator;
    [SerializeField] private ClientSidePredictor predictor;

    private void OnDestroy()
    {
        PlayerMap.Remove(Id);
    }

    private void Move(ushort tick, Vector3 position, Vector3 forwardVector)
    {
        if (IsLocal)
        {
            predictor.NewUpdate(tick, position);
        }
        else
        {
            interpolator.NewUpdate(tick, position, forwardVector);
            //camTransform.forward = forwardVector;
        }
    }

    public static void Spawn(ushort id, string username, Vector3 position)
    {
        bool isLocal = (id == NetworkManager.Singleton.Client.Id);
        GameObject prefab;
        if (isLocal)
        {
            prefab = GameLogic.Singleton.LocalPlayerPrefab;
        } else
        {
            prefab = GameLogic.Singleton.PlayerPrefab;
        }

        Player player = Instantiate(prefab, position, Quaternion.identity).GetComponent<Player>();
        player.IsLocal = isLocal;

        player.name = $"Player_{id}";
        player.Username = username;
        player.Id = id;

        PlayerMap.Add(id, player);
    }

    #region Messages
    [MessageHandler((ushort)ServerToClientId.playerSpawned)]
    private static void PlayerSpawned(Message message)
    {
        ushort id = message.GetUShort();
        string username = message.GetString();
        Vector3 position = message.GetVector3();

        Spawn(id, username, position);
    }

    [MessageHandler((ushort)ServerToClientId.playerMovement)]
    private static void PlayerMoved(Message message)
    {
        ushort id = message.GetUShort();
        ushort tick = message.GetUShort();
        Vector3 position = message.GetVector3();
        Vector3 forwardVector = message.GetVector3();

        if (PlayerMap.TryGetValue(id, out Player player))
        {
            player.Move(tick, position, forwardVector);
        }

    }
    #endregion
}
