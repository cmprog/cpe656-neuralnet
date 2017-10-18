using System.IO;
using UnityEngine;
using UnityStandardAssets.Vehicles.Car;

public class AutonomousOutputGenerator : MonoBehaviour
{
    private const string sOutputFileName = "output_log.csv";
    private const string sOutputImageDirName = "IMG";
    
    private ResolvedSampleWriter mWriter;

    public Camera Camera;
    public GameObjectSpawner GameObjectSpawner;
    public CommandServer CommandServer;

    private GameObject mCurrentGameObject;
    private UnresolvedSample mPendingUnresolvedSample;
    
	void Start ()
	{
	    this.GameObjectSpawner.GameObjectSpawned += this.GameObjectSpawner_GameObjectSpawned;
	    this.CommandServer.TelemetrySending += this.CommandServer_TelemetrySending;
	    this.CommandServer.DetectionResponse += this.CommandServer_DetectionResponse;
	}

    void OnDestroy()
    {
        this.StopRecording();
    }

    private void CommandServer_DetectionResponse(object sender, DetectionResponseEventArgs e)
    {
        if (!this.IsRecording) return;

        // This can happen if the recording was started between events
        if (this.mPendingUnresolvedSample == null) return;

        var lResolvedResponse = this.mPendingUnresolvedSample.Resolve(e.WasDetected);
        this.mWriter.Write(lResolvedResponse);
    }

    private void CommandServer_TelemetrySending(object sender, TelemetryEventArgs e)
    {
        if (!this.IsRecording) return;
        
        this.mPendingUnresolvedSample = new UnresolvedSample(e, this.mCurrentGameObject);
    }

    private void GameObjectSpawner_GameObjectSpawned(object sender, GameObjectEventArgs e)
    {
        this.mCurrentGameObject = e.GameObject;
    }

    public bool IsRecording { get; private set; }

    public void StartRecording(DirectoryInfo directory)
    {
        var lOutputFile = new FileInfo(Path.Combine(directory.FullName, sOutputFileName));
        var lOutputImageDirectory = directory.CreateSubdirectory(sOutputImageDirName);
        this.mWriter = new ResolvedSampleWriter(lOutputFile, lOutputImageDirectory, this.Camera);
        this.IsRecording = true;
    }

    public void StopRecording()
    {
        this.IsRecording = false;
        this.mWriter.Dispose();
    }
}