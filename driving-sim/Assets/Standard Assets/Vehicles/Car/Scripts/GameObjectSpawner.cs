using UnityEngine;
using UnityStandardAssets.Utility;

namespace UnityStandardAssets.Vehicles.Car
{
    [RequireComponent(typeof(WaypointProgressTracker))]
    public class GameObjectSpawner : MonoBehaviour
    {
        private WaypointProgressTracker progressTracker;
        private GameObject spawnParent;

        public GameObject gameObject;

        public float gameObjectLifeSpan;

        public float rotationX;
        public float rotationY;
        public float rotationZ;

        private void Awake()
        {
            this.progressTracker = this.GetComponent<WaypointProgressTracker>();

            // Create a dummy game object in order to keep our spawned game objects
            // contained neatly in the object heirarchy
            this.spawnParent = new GameObject("Spawn Parent");
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.LeftShift)) return;

            var target = this.progressTracker.target;

            var generatedGameObject = Instantiate(this.gameObject, target.position, target.rotation, this.spawnParent.transform);

            // Some game objects need additional rotation for them to be oriented correctly,
            // usually these rotations would be a multiple of 90.0 degrees
            generatedGameObject.transform.Rotate(Vector3.right, this.rotationX, Space.Self);
            generatedGameObject.transform.Rotate(Vector3.up, this.rotationY, Space.Self);
            generatedGameObject.transform.Rotate(Vector3.forward, this.rotationZ, Space.Self);

            Destroy(generatedGameObject.gameObject, this.gameObjectLifeSpan);
        }
    }
}