using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Threading;
using UnityStandardAssets.Vehicles.Car;
using UnityEngine.SceneManagement;

public class UISystem : MonoSingleton<UISystem> {

    public CarController carController;
    public GameObjectSpawner gameObjectSpawner;
    public AutonomousOutputGenerator autonomouseOutputGenerator;

    public string GoodCarStatusMessage;
    public string BadSCartatusMessage;
    public Text MPH_Text;
    public Image MPH_Animation;
    public Text Angle_Text;
    public Text RecordStatus_Text;
	public Text DriveStatus_Text;
	public Text SaveStatus_Text;
    public Text DetectStatus_Text;
    public GameObject RecordingPause; 
	public GameObject RecordDisabled;
    public bool isTraining = false;

    public Texture BoundingBoxTexture;
    private Rect mBoundingBoxBounds;
    private bool mDrawBouningBox;

    private bool recording;
    private float topSpeed;
	private bool saveRecording;

    public string DetectionMessage;
    public string NoDetectionMessage;

    public Color DetectionColor;
    public Color NoDetectionColor;

    public InputField spawnDistanceInputField;

    // Use this for initialization
    void Start()
    {
        topSpeed = carController.MaxSpeed;
        recording = false;
        RecordingPause.SetActive(false);
		RecordStatus_Text.text = "RECORD";
		DriveStatus_Text.text = "";
		SaveStatus_Text.text = "";
		SetAngleValue(0);
        SetMPHValue(0);
    }

    public void SetAngleValue(float value)
    {
        Angle_Text.text = value.ToString("N2") + "°";
    }

    public void SetMPHValue(float value)
    {
        MPH_Text.text = value.ToString("N2");
        //Do something with value for fill amounts
        MPH_Animation.fillAmount = value/topSpeed;
    }

    public void SetDetection(bool value)
    {
        if (value)
        {
            this.DetectStatus_Text.text = this.DetectionMessage;
            this.DetectStatus_Text.color = Color.green;
        }
        else
        {
            this.DetectStatus_Text.text = this.NoDetectionMessage;
            this.DetectStatus_Text.color = Color.red;
        }
    }

    private bool IsWithinViewport(Rect bounds)
    {
        if ((bounds.width <= 0.0f) || (bounds.height <= 0.0f)) return false;

        if (bounds.xMax <= 0.0f) return false;
        if (bounds.xMin >= 1.0f) return false;

        if (bounds.yMax <= 0.0f) return false;
        if (bounds.yMin >= 1.0f) return false;

        return true;
    }

    public void SetBoundingBox(Camera referenceCamera, Rect bounds)
    {
        if (!this.IsWithinViewport(bounds))
        {
            this.mDrawBouningBox = false;
            return;
        }

        this.mDrawBouningBox = true;
        var lScreenPosition = referenceCamera.ViewportToScreenPoint(bounds.position);
        this.mBoundingBoxBounds.position = new Vector2(lScreenPosition.x, lScreenPosition.y + referenceCamera.pixelHeight);
        this.mBoundingBoxBounds.size = bounds.size;
    }

    void OnGUI()
    {
        if (this.mDrawBouningBox)
        {
            GUI.DrawTexture(
                this.mBoundingBoxBounds, this.BoundingBoxTexture, 
                ScaleMode.StretchToFill, false, 0, Color.green, 0, 0);
        }
    }

    public void ToggleRecording()
    {
        if (this.isTraining)
        {
            if (!recording)
            {
                if (carController.checkSaveLocation())
                {
                    recording = true;
                    RecordingPause.SetActive(true);
                    RecordStatus_Text.text = "RECORDING";
                    carController.IsRecording = true;
                }
            }
            else
            {
                saveRecording = true;
                carController.IsRecording = false;
            }
        }
        else
        {
            if (this.autonomouseOutputGenerator.IsRecording)
            {
                this.RecordingPause.SetActive(false);
                this.RecordStatus_Text.text = "RECORD";
                this.autonomouseOutputGenerator.StopRecording();
            }
            else
            {
                SimpleFileBrowser.ShowSaveDialog(
                    location =>
                    {
                        this.recording = true;
                        this.RecordingPause.SetActive(true);
                        this.RecordStatus_Text.text = "RECORDING";
                        this.autonomouseOutputGenerator.StartRecording(new DirectoryInfo(location));
                    },
                    null, true, null, "Select Output File", "Select");
            }
        }
    }

    public void ToggleAsset(int index)
    {
        if (this.gameObjectSpawner == null) return;
        this.gameObjectSpawner.ToggleSpawnableGameObjectEnabled(index);
    }
	
    void UpdateCarValues()
    {
        SetMPHValue(carController.CurrentSpeed);
        SetAngleValue(carController.CurrentSteerAngle);
    }

	// Update is called once per frame
	void Update () {

        // Easier than pressing the actual button :-)
        // Should make recording training data more pleasant.

		if (carController.getSaveStatus ()) {
			SaveStatus_Text.text = "Capturing Data: " + (int)(100 * carController.getSavePercent ()) + "%";
			//Debug.Log ("save percent is: " + carController.getSavePercent ());
		} 
		else if(saveRecording) 
		{
			SaveStatus_Text.text = "";
			recording = false;
			RecordingPause.SetActive(false);
			RecordStatus_Text.text = "RECORD";
			saveRecording = false;
		}

        if (Input.GetKeyDown(KeyCode.R))
        {
            ToggleRecording();
        }

	    if(Input.GetKeyDown(KeyCode.Escape))
        {
            //Do Menu Here
            SceneManager.LoadScene("MenuScene");
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            //Do Console Here
        }

        UpdateCarValues();
    }
}
