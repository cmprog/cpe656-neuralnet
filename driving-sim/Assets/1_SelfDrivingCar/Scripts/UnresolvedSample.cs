using UnityEngine;

internal sealed class UnresolvedSample
{
    private readonly TelemetryEventArgs mTelemetryEventArgs;
    private readonly GameObject mGameObject;

    public UnresolvedSample(TelemetryEventArgs telemetryEventArgs, GameObject currentGameObject)
    {
        this.mTelemetryEventArgs = telemetryEventArgs;
        this.mGameObject = currentGameObject;
    }

    public ResolvedSample Resolve(bool wasDetected)
    {
        return new ResolvedSample(this.mTelemetryEventArgs, this.mGameObject, wasDetected);
    }
}