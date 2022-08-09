using UnityEngine;

public class TransformUpdate
{
    public ushort Tick { get; private set; }
    public Vector3 Position { get; private set; }
    public Vector3 Forward { get; private set; }

    public TransformUpdate(ushort tick, Transform transform)
    {
        Tick = tick;
        Position = transform.position;
        Forward = FlattenVector3(transform.forward);
    }

    public TransformUpdate(ushort tick, Vector3 position, Vector3 forward)
    {
        Tick = tick;
        Position = position;
        Forward = FlattenVector3(forward);
    }

    private Vector3 FlattenVector3(Vector3 vector)
    {
        vector.y = 0;
        return vector;
    }
}