using UnityEngine;
using UnityStandardAssets.Utility;

namespace UnityStandardAssets.Vehicles.Car
{
    [RequireComponent(typeof(WaypointProgressTracker))]
    public class GameObjectSpawner : MonoBehaviour
    {
        private WaypointProgressTracker progressTracker;

        public GameObject gameObject;

        public float gameObjectLifeSpan;

        private void Awake()
        {
            this.progressTracker = this.GetComponent<WaypointProgressTracker>();
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.LeftShift)) return;

            var target = this.progressTracker.target;

            var generatedSpeedBump = Instantiate(this.gameObject, target.position, target.rotation);
            Destroy(generatedSpeedBump.gameObject, this.gameObjectLifeSpan);
        }
    }
}