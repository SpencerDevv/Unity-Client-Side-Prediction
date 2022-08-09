using RiptideNetworking;
using RiptideNetworking.Utils;
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

        public Server Server { get; private set; }
        public ushort CurrentTick { get; private set; } = 0;

        [SerializeField] private ushort port;
        [SerializeField] private ushort maxClientCount;

        private void Awake()
        {
            Singleton = this;
        }

        private void Start()
        {
            RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, false);

            Server = new Server();
            Server.Start(port, maxClientCount);
            Server.ClientDisconnected += PlayerLeft;
        }

        private void FixedUpdate()
        {
            Server.Tick();

            if (CurrentTick % 200 == 0)
                SendSync();

            CurrentTick++;
        }

        private void OnApplicationQuit()
        {
            Server.Stop();
        }

        private void PlayerLeft(object sender, ClientDisconnectedEventArgs e)
        {
            Destroy(Player.PlayerMap[e.Id].gameObject);
        }

        #region Messages
        private void SendSync()
        {
            Message message = Message.Create(MessageSendMode.unreliable, ServerToClientId.sync);
            message.AddUShort(CurrentTick);

            Server.SendToAll(message);
        }
        #endregion
    }
}
