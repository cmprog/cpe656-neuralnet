using UnityEngine;

internal sealed class ResolvedSample
{
    private readonly TelemetryEventArgs mTelemetryEventArgs;
    private readonly GameObject mGameObject;
    private readonly bool mWasDetected;

    public ResolvedSample(TelemetryEventArgs telemetryEventArgs, GameObject gameObject, bool wasDetected)
    {
        this.mTelemetryEventArgs = telemetryEventArgs;
        this.mGameObject = gameObject;
        this.mWasDetected = wasDetected;
    }

    public TelemetryEventArgs TelemetryEventArgs { get { return this.mTelemetryEventArgs; } }

    public GameObject GameObject { get { return this.mGameObject; } }

    public bool WasDetected { get { return this.mWasDetected; } }
}