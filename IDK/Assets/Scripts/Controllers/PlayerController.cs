using System;
using System.Collections;
using System.Collections.Generic;
using RiptideNetworking;
using Multiplayer;
using UnityEngine;

namespace Controllers
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private Transform camTransform;
        [SerializeField] private ClientSidePredictor predictor;
        private bool[] inputs;

        private void Start()
        {
            inputs = new bool[5];
        }

        private void Update()
        {
            if (Input.GetKey(KeyCode.W))
                inputs[0] = true;

            if (Input.GetKey(KeyCode.S))
                inputs[1] = true;

            if (Input.GetKey(KeyCode.A))
                inputs[2] = true;

            if (Input.GetKey(KeyCode.D))
                inputs[3] = true;

            if (Input.GetKey(KeyCode.Space))
                inputs[4] = true;
        }

        private void FixedUpdate()
        {
            predictor.SetInputs(NetworkManager.Singleton.ServerTick, inputs, camTransform.forward);
            SendInput();

            for (int i = 0; i < inputs.Length; i++)
            {
                inputs[i] = false;
            }
        }

        #region Messages
        private void SendInput()
        {
            Message message = Message.Create(MessageSendMode.unreliable, ClientToServerId.input);
            message.AddUShort(NetworkManager.Singleton.ServerTick);
            message.AddBools(inputs, false);
            message.AddVector3(camTransform.forward);

            NetworkManager.Singleton.Client.Send(message);
        }
        #endregion
    }
}