using UnityEngine;
using System.Collections.Generic;
using SocketIO;
using UnityStandardAssets.Vehicles.Car;
using System;

public sealed class TelemetryEventArgs : EventArgs
{
    public TelemetryEventArgs(byte[] image)
    {
        this.Image = image;
    }

    public byte[] Image { get; private set; }
}

public sealed class DetectionResponseEventArgs : EventArgs
{
    public DetectionResponseEventArgs(bool wasDetected)
    {
        this.WasDetected = wasDetected;
    }

    public bool WasDetected { get; private set; }
}

public class CommandServer : MonoBehaviour
{
	public CarRemoteControl CarRemoteControl;
	public Camera FrontFacingCamera;
    public UISystem UISystem;
	private SocketIOComponent _socket;
	private CarController _carController;

    /// <summary>
    /// Fired as a telementry message is being sent out.
    /// </summary>
    public event EventHandler<TelemetryEventArgs> TelemetrySending;

    public event EventHandler<DetectionResponseEventArgs> DetectionResponse;

	// Use this for initialization
	void Start()
	{
		_socket = GameObject.Find("SocketIO").GetComponent<SocketIOComponent>();
		_socket.On("open", OnOpen);
	    _socket.On("detect", OnDetect);
	    _socket.On("track", OnTrack);
		_socket.On("manual", onManual);
		_carController = CarRemoteControl.GetComponent<CarController>();
	}

    // Update is called once per frame
	void Update()
	{
	}

	void OnOpen(SocketIOEvent obj)
	{
		EmitTelemetry(obj);
	}

	// 
	void onManual(SocketIOEvent obj)
	{
		EmitTelemetry (obj);
    }
    
    private void OnDetect(SocketIOEvent obj)
    {
        var jsonObject = obj.data;

        const string lcHasDetectionFieldKey = "hasDetection";

        var hasDetectionJsonField = jsonObject.GetField(lcHasDetectionFieldKey);
        if (hasDetectionJsonField == null)
        {
            Debug.LogWarning(string.Format(
                "Expecting JSON field named \"{0}\" but field was not found.", 
                lcHasDetectionFieldKey));
        }
        else if (!hasDetectionJsonField.IsBool)
        {
            Debug.LogWarning(string.Format(
                "Expecting JSON field named \"{0}\" to be a boolean but found a {1}.",
                lcHasDetectionFieldKey, hasDetectionJsonField.type));
        }
        else
        {
            var hasDetection = hasDetectionJsonField.b;
            this.UISystem.SetDetection(hasDetection);

            var lDetectionResponseHandler = this.DetectionResponse;
            if (lDetectionResponseHandler != null)
            {
                var lEventArgs = new DetectionResponseEventArgs(hasDetection);
                lDetectionResponseHandler(this, lEventArgs);
            }
        }

        EmitTelemetry(obj);
    }
    
    private void OnTrack(SocketIOEvent obj)
    {
        // Currently this method stub is a plceholder for once we get tracking working
        EmitTelemetry(obj);
    }

	void EmitTelemetry(SocketIOEvent obj)
	{
		UnityMainThreadDispatcher.Instance().Enqueue(() =>
		{
            this.FrontFacingCamera.Render();
		    var lImageData = CameraHelper.CaptureFrame(this.FrontFacingCamera);

		    var lTelemetrySendingHandler = this.TelemetrySending;
		    if (lTelemetrySendingHandler != null)
		    {
		        var lEventArgs = new TelemetryEventArgs(lImageData);
		        lTelemetrySendingHandler(this, lEventArgs);
		    }

            var telemetryData = this.CreateTelemetryData(lImageData);
		    this._socket.Emit("telemetry", telemetryData);
		});
	}

    private JSONObject CreateTelemetryData(byte[] imageData)
    {
        var data = new Dictionary<string, string>();
        data["image"] = Convert.ToBase64String(imageData);
        return new JSONObject(data);
    }
}