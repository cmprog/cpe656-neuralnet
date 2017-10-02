using System;
using UnityEngine;

namespace UnityStandardAssets.Vehicles.Car
{
    public sealed class GameObjectEventArgs : EventArgs
    {
        private readonly GameObject mGameObject;

        public GameObjectEventArgs(GameObject gameObject)
        {
            this.mGameObject = gameObject;
        }

        public GameObject GameObject
        {
            get { return this.mGameObject; }
        }
    }
}