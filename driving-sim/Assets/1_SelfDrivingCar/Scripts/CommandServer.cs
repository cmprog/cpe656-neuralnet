using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using SocketIO;
using UnityStandardAssets.Vehicles.Car;
using System;
using System.Security.AccessControl;

public class CommandServer : MonoBehaviour
{
	public CarRemoteControl CarRemoteControl;
	public Camera FrontFacingCamera;
    public UISystem UISystem;
	private SocketIOComponent _socket;
	private CarController _carController;

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
		Debug.Log("Connection Open");
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

        Debug.LogFormat("OnDetect: {0}", obj);

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
        }

        EmitTelemetry(obj);
    }
    
    private void OnTrack(SocketIOEvent obj)
    {
        // Currently this method stub is a plceholder for once we get tracking working
        Debug.LogFormat("OnTrack: {0}", obj);
        EmitTelemetry(obj);
    }

	void EmitTelemetry(SocketIOEvent obj)
	{
		UnityMainThreadDispatcher.Instance().Enqueue(() =>
		{
			print("Attempting to Send...");
		    var telemetryData = this.CreateTelemetryData();
		    this._socket.Emit("telemetry", telemetryData);
		});
	}

    private JSONObject CreateTelemetryData()
    {
        // If manually steering is detected, then we don't send any meaningful data
        if ((Input.GetKey(KeyCode.W)) || (Input.GetKey(KeyCode.S)))
        {
            return new JSONObject();
        }

        var data = new Dictionary<string, string>();
        data["image"] = Convert.ToBase64String(CameraHelper.CaptureFrame(FrontFacingCamera));
        return new JSONObject(data);
    }
}