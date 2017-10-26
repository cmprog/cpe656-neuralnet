using UnityEngine;
using UnityStandardAssets.Utility;

namespace UnityStandardAssets.Vehicles.Car
{
    public class AutoGameObjectSpawner : MonoBehaviour
    {
        private const int COOLDOWN_RATE = 100; //the higher the number, the further apart the assets from one another
        private const int AUTOSPAWN_RATE = 100; //the higher the number, the more time inbetween asset spawning

        private WaypointProgressTracker wpTracker;
        private GameObjectSpawner goSpawner;
        private CarController carController;

        private float checkpoint;
        private float randomDistance;
        
        private int coolDown = 0;

        private void Start()
        {
            goSpawner = GetComponent<GameObjectSpawner>();
            wpTracker = GetComponent<WaypointProgressTracker>();
            carController = GetComponent<CarController>();

            checkpoint = wpTracker.progressDistance;
            randomDistance = GetRandomDistance();
        }

        private void Update()
        {
            //gets diff of how far the car has progressed since the last checkpoint
            float distanceDiff = wpTracker.progressDistance - checkpoint;

            //update cool down if we are in cool down period
            if (coolDown > 0) coolDown--;

            //if the diff is greater than the last save random distance
            if (distanceDiff > randomDistance)
            {
                //checkpoint the current progression and set new random distance
                checkpoint = wpTracker.progressDistance;
                randomDistance = GetRandomDistance();

                //if we are still in cool down then skip this spawn opportunity
                if(coolDown == 0)
                {
                    //reset cool down to preset rate
                    coolDown = COOLDOWN_RATE;

                    //Finally, if we get here then spawn that object
                    goSpawner.SpawnGameObject();
                }
            }
        }

        private float GetRandomDistance()
        {
            //set lower bound to random value at AUTOSPAWN_RATE/2 to attempt to prevent
            //assets from getting generated one right after the other
            return (Random.value * AUTOSPAWN_RATE/2) + AUTOSPAWN_RATE/2;
        }
    }
}
