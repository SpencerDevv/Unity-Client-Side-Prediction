using System.Collections;
using System.Collections.Generic;
using Multiplayer;
using UnityEngine;

public struct InputPayload
{
    public int tick;
    public bool[] inputs;
    public Vector3 camForward;
}

public struct StatePayload
{
    public int tick;
    public Vector3 position;
}

[RequireComponent(typeof(CharacterController))]
public class ClientSidePredictor : MonoBehaviour
{
    private const int BUFFER_SIZE = 1024;

    private InputPayload[] inputBuffer;
    private StatePayload[] stateBuffer;
    private StatePayload latestServerState;
    private StatePayload lastProcessedState;


    [SerializeField] private float gravity;
    [SerializeField] private CharacterController controller;
    [SerializeField] private float movementSpeed;
    [SerializeField] private Transform camProxy;
    [SerializeField] private float jumpHeight;

    private float gravityAcceleration;
    private float moveSpeed;
    private float jumpSpeed;
    private float yVelocity;

    private void OnValidate()
    {
        if (controller == null)
            controller = GetComponent<CharacterController>();
        Initialize();
    }

    private void Start()
    {
        stateBuffer = new StatePayload[BUFFER_SIZE];
        inputBuffer = new InputPayload[BUFFER_SIZE];

        for (int index = 0; index < stateBuffer.Length; index++)
        {
            StatePayload payload = stateBuffer[index];
            payload.position = transform.position;
        }

        Initialize();
    }

    private Vector3 FlattenVector3(Vector3 vector)
    {
        vector.y = 0;
        return vector;
    }

    private void Initialize()
    {
        gravityAcceleration = gravity * Time.fixedDeltaTime * Time.fixedDeltaTime;
        moveSpeed = movementSpeed * Time.fixedDeltaTime;
        jumpSpeed = Mathf.Sqrt(jumpHeight * -2f * gravityAcceleration);
    }

    public void NewUpdate(ushort tick, Vector3 position)
    {
        latestServerState = new StatePayload
        {
            tick = tick,
            position = position
        };
    }

    public void SetInputs(ushort tick, bool[] inputs, Vector3 camForward)
    {
        if (!latestServerState.Equals(default(StatePayload)) &&
            (lastProcessedState.Equals(default(StatePayload)) ||
            !latestServerState.Equals(lastProcessedState)))
        {
            HandleServerReconciliation();
        }

        int bufferIndex = tick % BUFFER_SIZE;

        InputPayload inputPayload = new InputPayload
        {
            tick = tick,
            inputs = inputs,
            camForward = camForward
        };

        inputBuffer[bufferIndex] = inputPayload;
        stateBuffer[bufferIndex] = ProcessMovement(inputPayload);
    }

    private StatePayload ProcessMovement(InputPayload inputPayload)
    {
        Vector2 inputDirection = Vector2.zero;
        if (inputPayload.inputs[0])
            inputDirection.y += 1;
        if (inputPayload.inputs[1])
            inputDirection.y -= 1;
        if (inputPayload.inputs[2])
            inputDirection.x -= 1;
        if (inputPayload.inputs[3])
            inputDirection.x += 1;

        Vector3 moveDirection = Vector3.Normalize(camProxy.right * inputDirection.x + Vector3.Normalize(FlattenVector3(camProxy.forward)) * inputDirection.y);
        moveDirection *= moveSpeed;

        if (controller.isGrounded)
        {
            yVelocity = 0f;
            if (inputPayload.inputs[4])
            {
                yVelocity = jumpSpeed;
            }
        }
        yVelocity += gravityAcceleration;
        moveDirection.y = yVelocity;
        controller.Move(moveDirection);

        return new StatePayload
        {
            tick = inputPayload.tick,
            position = transform.position
        };
    }

    private void HandleServerReconciliation()
    {
        lastProcessedState = latestServerState;

        int serverStateBufferIndex = latestServerState.tick % BUFFER_SIZE;
        float positionError = Vector3.Distance(latestServerState.position, stateBuffer[serverStateBufferIndex].position);

        if (positionError > 0.001f)
        {
            Debug.Log("--- Reconcile ---");
            Debug.Log(latestServerState.position);
            Debug.Log(stateBuffer[serverStateBufferIndex].position);
            Debug.Log("-----------------");

            transform.position = latestServerState.position;
            stateBuffer[serverStateBufferIndex] = latestServerState;

            int tickToProcess = latestServerState.tick + 1;
            while (tickToProcess < NetworkManager.Singleton.ServerTick)
            {
                int bufferIndex = tickToProcess % BUFFER_SIZE;

                StatePayload statePayload = ProcessMovement(inputBuffer[bufferIndex]);
                stateBuffer[bufferIndex] = statePayload;

                tickToProcess++;
            }
        }
    }
}
