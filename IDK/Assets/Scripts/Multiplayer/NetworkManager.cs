using RiptideNetworking;
using RiptideNetworking.Utils;
using System;
using UnityEngine;

namespace Multiplayer
{
    public enum ServerToClientId : ushort
    {
        sync = 1,
        playerSpawned,
        playerMovement
    };

    public enum ClientToServerId : ushort
    {
        auth = 1,
        input
    }

    public class NetworkManager : MonoBehaviour
    {
        private static NetworkManager _singleton;

        public static NetworkManager Singleton
        {
            get => _singleton;
            private set
            {
                if (_singleton == null)
                    _singleton = value;
                else if (_singleton != value)
                {
                    Debug.Log($"{nameof(NetworkManager)} instance already exists, destroying duplicate");
                    Destroy(value);
                }
            }
        }

        public Client Client { get; private set; }

        private ushort _serverTick;
        public ushort ServerTick
        {
            get => _serverTick;
            private set
            {
                _serverTick = value;
                InterpolationTick = (ushort)(value - TicksBetweenPositionUpdates);
            }
        }

        private ushort _ticksBetweenPositionUpdates = 2;
        public ushort TicksBetweenPositionUpdates
        {
            get => _ticksBetweenPositionUpdates;
            private set
            {
                _ticksBetweenPositionUpdates = value;
                InterpolationTick = (ushort)(ServerTick - value);
            }
        }

        public ushort InterpolationTick { get; private set; }

        [SerializeField] private string ip;
        [SerializeField] private ushort port;
        [Space(10)]
        [SerializeField] private ushort tickDivergenceTolerence = 1;

        private void Awake()
        {
            Singleton = this;
        }

        private void Start()
        {
            RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, false);

            Client = new Client();
            Client.Connect($"{ip}:{port}");
            Client.ClientDisconnected += PlayerLeft;
            Client.Disconnected += DidDisconnect;

            ServerTick = 2;

            // Testing
            Message message = Message.Create(MessageSendMode.reliable, ClientToServerId.auth);
            message.AddString("Player_0");
            Client.Send(message);
        }

        private void FixedUpdate()
        {
            Client.Tick();
            ServerTick++;
        }

        private void OnApplicationQuit()
        {
            Client.Disconnect();
        }

        private void PlayerLeft(object sender, ClientDisconnectedEventArgs e)
        {
            if (Player.PlayerMap.TryGetValue(e.Id, out Player player))
                Destroy(player.gameObject);
        }

        private void DidDisconnect(object sender, EventArgs e)
        {
            foreach (Player player in Player.PlayerMap.Values)
                Destroy(player.gameObject);
        }

        private void SetTick(ushort serverTick)
        {
            if (Mathf.Abs(ServerTick - serverTick) > tickDivergenceTolerence)
            {
                Debug.Log($"Client Tick: {ServerTick} -> {serverTick}");
                ServerTick = serverTick;
            }
        }

        [MessageHandler((ushort) ServerToClientId.sync)]
        public static void Sync(Message message)
        {
            ushort serverTick = message.GetUShort();

            Singleton.SetTick(serverTick);
        }
    }
}