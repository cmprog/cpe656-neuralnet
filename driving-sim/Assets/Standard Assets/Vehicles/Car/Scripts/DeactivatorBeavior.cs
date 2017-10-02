using System.Collections;
using UnityEngine;

namespace UnityStandardAssets.Vehicles.Car
{
    /// <summary>
    /// Deactivates the attached game object after a duration
    /// </summary>
    public sealed class DeactivatorBeavior : MonoBehaviour
    {
        public float delaySeconds;

        private void Start()
        {
            this.StartCoroutine(this.DelayedDeactivate());
        }

        private IEnumerator DelayedDeactivate()
        {
            yield return new WaitForSeconds(this.delaySeconds);
            this.gameObject.SetActive(false);
            Destroy(this);
        }
    }
}