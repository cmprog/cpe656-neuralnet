using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityStandardAssets.Utility;
using Random = UnityEngine.Random;

namespace UnityStandardAssets.Vehicles.Car
{
    [RequireComponent(typeof(WaypointProgressTracker))]
    public class GameObjectSpawner : MonoBehaviour
    {
        private WaypointProgressTracker progressTracker;
        private GameObject spawnParent;

        public SpawnableGameObject[] spawnableGameObjects;
        private List<SpawnableGameObject> candidateSpawnableGameObjectList;

        public float gameObjectLifeSpan;

        private void Awake()
        {
            this.progressTracker = this.GetComponent<WaypointProgressTracker>();

            // Create a dummy game object in order to keep our spawned game objects
            // contained neatly in the object heirarchy
            this.spawnParent = new GameObject("Spawn Parent");

            this.candidateSpawnableGameObjectList = new List<SpawnableGameObject>(
                this.spawnableGameObjects.Where(x => x.enabled));
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.LeftShift)) return;

            var spawnableGameObject = this.NextSpawnableGameObject();
            if (spawnableGameObject == null) return;

            var targetTransform = this.progressTracker.target;
            var generatedGameObject = spawnableGameObject.Create(targetTransform, this.spawnParent.transform);
            
            Destroy(generatedGameObject.gameObject, this.gameObjectLifeSpan);
        }

        public void ToggleSpawnableGameObjectEnabled(int index)
        {
            if (this.spawnableGameObjects == null) return;
            if ((index < 0) || (index >= this.spawnableGameObjects.Length)) return;

            var spawnableGameObject = this.spawnableGameObjects[index];
            spawnableGameObject.enabled = !spawnableGameObject.enabled;

            if (spawnableGameObject.enabled)
            {
                this.candidateSpawnableGameObjectList.Add(spawnableGameObject);
            }
            else
            {
                this.candidateSpawnableGameObjectList.Remove(spawnableGameObject);
            }
        }

        private SpawnableGameObject NextSpawnableGameObject()
        {
            if (this.candidateSpawnableGameObjectList == null) return null;
            if (this.candidateSpawnableGameObjectList.Count == 0) return null;
            var index = Random.Range(0, this.candidateSpawnableGameObjectList.Count);
            return this.candidateSpawnableGameObjectList[index];
        }

        [Serializable]
        public sealed class SpawnableGameObject
        {
            public GameObject gameObject;
            public bool enabled;

            public float rotationX;
            public float rotationY;
            public float rotationZ;

            /// <summary>
            /// Creates a new <see cref="GameObject"/> with the position and rotation of the <paramref name="targetTransform"/>
            /// as a child of the <paramref name="parentTransform"/>.
            /// </summary>
            public GameObject Create(Transform targetTransform, Transform parentTransform)
            {
                var generatedGameObject = Instantiate(this.gameObject, targetTransform.position, targetTransform.rotation, parentTransform);

                // Some game objects need additional rotation for them to be oriented correctly,
                // usually these rotations would be a multiple of 90.0 degrees
                generatedGameObject.transform.Rotate(Vector3.right, this.rotationX, Space.Self);
                generatedGameObject.transform.Rotate(Vector3.up, this.rotationY, Space.Self);
                generatedGameObject.transform.Rotate(Vector3.forward, this.rotationZ, Space.Self);

                return generatedGameObject;
            }
        }
    }
}