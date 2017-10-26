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

        public SpawnableGameObjectGroup[] spawnableGameObjectGroups;
        private List<SpawnableGameObjectGroup> candidateSpawnableGameObjectGroupList;

        public float gameObjectLifeSpan;

        public event EventHandler<GameObjectEventArgs> GameObjectSpawned;

        private void Awake()
        {
            this.progressTracker = this.GetComponent<WaypointProgressTracker>();

            // Create a dummy game object in order to keep our spawned game objects
            // contained neatly in the object heirarchy
            this.spawnParent = new GameObject("Spawn Parent");

            this.candidateSpawnableGameObjectGroupList = new List<SpawnableGameObjectGroup>(
                this.spawnableGameObjectGroups.Where(x => x.enabled));
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.LeftShift)) return;

            SpawnGameObject();
        }

        public void SpawnGameObject()
        {
            var spawnableGameObject = this.NextSpawnableGameObject();
            if (spawnableGameObject == null) return;

            var targetTransform = this.progressTracker.target;

            var spawnedGameObject = spawnableGameObject.Create(targetTransform, this.spawnParent.transform);
            
            if (spawnedGameObject != null)
            {
                var deactivatorBehavior = spawnedGameObject.AddComponent<DeactivatorBeavior>();
                deactivatorBehavior.delaySeconds = this.gameObjectLifeSpan;

                this.OnGameObjectSpawned(new GameObjectEventArgs(spawnedGameObject));
            }
        }

        public void ToggleSpawnableGameObjectEnabled(int index)
        {
            if (this.spawnableGameObjectGroups == null) return;
            if ((index < 0) || (index >= this.spawnableGameObjectGroups.Length)) return;

            var spawnableGameObject = this.spawnableGameObjectGroups[index];
            spawnableGameObject.enabled = !spawnableGameObject.enabled;

            if (spawnableGameObject.enabled)
            {
                this.candidateSpawnableGameObjectGroupList.Add(spawnableGameObject);
            }
            else
            {
                this.candidateSpawnableGameObjectGroupList.Remove(spawnableGameObject);
            }
        }

        private SpawnableGameObjectGroup NextSpawnableGameObject()
        {
            if (this.candidateSpawnableGameObjectGroupList == null) return null;
            if (this.candidateSpawnableGameObjectGroupList.Count == 0) return null;
            var index = Random.Range(0, this.candidateSpawnableGameObjectGroupList.Count);
            return this.candidateSpawnableGameObjectGroupList[index];
        }

        private void OnGameObjectSpawned(GameObjectEventArgs e)
        {
            var handler = this.GameObjectSpawned;
            if (handler != null) handler(this, e);
        }

        [Serializable]
        public struct RandomOffset
        {
            public float min;

            public float max;

            public RandomOffset ReverseBounds()
            {
                return new RandomOffset
                {
                    min = this.max,
                    max = this.min,
                };
            }

            public bool IsValidBounds
            {
                get { return this.min <= this.max; }
            }

            public float NextOffset()
            {
                return Random.Range(this.min, this.max);
            }

            public float FromPercent(float percent)
            {
                return this.min + ((this.max - this.min) * Mathf.Clamp01(percent));
            }

            public static float operator +(float value, RandomOffset offset)
            {
                return value + offset.NextOffset();
            }
        }

        [Serializable]
        public sealed class SpwnableGameObjectTransform
        {
            public bool enabled;

            public RandomOffset x;
            public RandomOffset y;
            public RandomOffset z;

            public bool lockXYRatio;
            public bool lockXZRatio;
            public bool lockYZRatio;

            public Vector3 CreateVector()
            {
                var randomPercentX = Random.Range(0.0f, 1.0f);
                var randomX = this.x.FromPercent(randomPercentX);

                var randomPercentY = this.lockXYRatio ? randomPercentX : Random.Range(0.0f, 1.0f);
                var randomY = this.y.FromPercent(randomPercentY);
                
                var randomPercentZ = this.lockXZRatio ? randomPercentX : (this.lockYZRatio ? randomPercentY : Random.Range(0.0f, 1.0f));
                var randomZ = this.z.FromPercent(randomPercentZ);

                return new Vector3(randomX, randomY, randomZ);
            }

            public void ApplyScale(GameObject gameObject)
            {
                if (!this.enabled) return;

                var scaleVector = this.CreateVector();
                Debug.LogFormat("Appling scaling of {0} to game object.", scaleVector);
                gameObject.transform.localScale = Vector3.Scale(gameObject.transform.localScale, scaleVector);
            }

            public void ApplyRotation(GameObject gameObject)
            {
                if (!this.enabled) return;

                var scaleVector = this.CreateVector();
                Debug.LogFormat("Appling rotation of {0} to game object.", scaleVector);
                gameObject.transform.Rotate(scaleVector);
            }
        }

        [Serializable]
        public sealed class SpawnableGameObjectGroup
        {
            public bool enabled;

            public SpawnableGameObject[] candidateGameObjects;
            
            public GameObject Create(Transform tragetTransform, Transform parentTransform)
            {
                if (this.candidateGameObjects.Length == 0) return null;
                var spawnableGameObject = this.candidateGameObjects[Random.Range(0, this.candidateGameObjects.Length)];
                return spawnableGameObject.Create(tragetTransform, parentTransform);
            }
        }
        
        [Serializable]
        public sealed class SpawnableGameObject
        {
            public GameObject gameObject;

            public Vector3 rotation;
            public Vector3 translation;
            
            public SpwnableGameObjectTransform randomRotation;
            public SpwnableGameObjectTransform randomScale;

            /// <summary>
            /// Creates a new <see cref="GameObject"/> with the position and rotation of the <paramref name="targetTransform"/>
            /// as a child of the <paramref name="parentTransform"/>.
            /// </summary>
            public GameObject Create(Transform targetTransform, Transform parentTransform)
            {
                var generatedGameObject = Instantiate(gameObject, targetTransform.position, targetTransform.rotation, parentTransform);

                // Some game objects need additional rotation for them to be oriented correctly,
                // usually these rotations would be a multiple of 90.0 degrees
                generatedGameObject.transform.Rotate(this.rotation, Space.Self);
                generatedGameObject.transform.Translate(this.translation);

                this.randomRotation.ApplyRotation(generatedGameObject);
                this.randomScale.ApplyScale(generatedGameObject);

                return generatedGameObject;
            }
        }
    }
}