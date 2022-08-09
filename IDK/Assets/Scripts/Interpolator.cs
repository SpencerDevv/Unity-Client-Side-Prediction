using System.Collections.Generic;
using UnityEngine;
using Multiplayer;

public class Interpolator : MonoBehaviour
{
    [SerializeField] private float timeElapsed = 0f;
    [SerializeField] private float timeToReachTarget = 0.05f;
    [SerializeField] private float movementThreshold = 0.05f;

    private readonly List<TransformUpdate> futureTransformUpdates = new List<TransformUpdate>();

    private float squareMovementThreshold;
    private TransformUpdate to;
    private TransformUpdate from;
    private TransformUpdate previous;

    private void Start()
    {
        squareMovementThreshold = movementThreshold * movementThreshold;
        to = new TransformUpdate(NetworkManager.Singleton.ServerTick, transform);
        from = new TransformUpdate(NetworkManager.Singleton.InterpolationTick, transform);
        previous = new TransformUpdate(NetworkManager.Singleton.InterpolationTick, transform);
    }

    private void Update()
    {
        for (int i = 0; i < futureTransformUpdates.Count; i++)
        {
            if (NetworkManager.Singleton.ServerTick >= futureTransformUpdates[i].Tick)
            {
                previous = to;
                to = futureTransformUpdates[i];
                from = new TransformUpdate(NetworkManager.Singleton.InterpolationTick, transform);

                futureTransformUpdates.RemoveAt(i);
                i--;
                timeElapsed = 0f;
                timeToReachTarget = (to.Tick - from.Tick) * Time.fixedDeltaTime;
            }
        }

        timeElapsed += Time.deltaTime;
        InterpolatePosition(timeElapsed / timeToReachTarget);
        InterpolateRotation(timeElapsed / timeToReachTarget);
    }

    private void InterpolatePosition(float lerpAmount)
    {
        if ((to.Position - previous.Position).sqrMagnitude < squareMovementThreshold)
        {
            if (to.Position != from.Position)
                transform.position = Vector3.Lerp(from.Position, to.Position, lerpAmount);

            return;
        }

        transform.position = Vector3.LerpUnclamped(from.Position, to.Position, lerpAmount);
    }

    private void InterpolateRotation(float lerpAmount)
    {
        if (to.Forward == previous.Forward)
        {
            if (to.Forward != from.Forward)
                transform.forward = Vector3.Lerp(from.Forward, to.Forward, lerpAmount);

            return;
        }

        transform.forward = Vector3.LerpUnclamped(from.Forward, to.Forward, lerpAmount);
    }

    public void NewUpdate(ushort tick, Vector3 position, Vector3 forward)
    {
        if (tick <= NetworkManager.Singleton.InterpolationTick)
            return;

        for (int i = 0; i < futureTransformUpdates.Count; i++)
        {
            if (tick < futureTransformUpdates[i].Tick)
            {
                futureTransformUpdates.Insert(i, new TransformUpdate(tick, position, forward));
                return;
            }
        }

        futureTransformUpdates.Add(new TransformUpdate(tick, position, forward));
    }
}