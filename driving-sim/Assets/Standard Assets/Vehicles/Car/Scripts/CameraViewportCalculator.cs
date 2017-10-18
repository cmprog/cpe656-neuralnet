using System.Collections.Generic;
using UnityEngine;

namespace UnityStandardAssets.Vehicles.Car
{
    public sealed class CameraViewportCalculator
    {
        private readonly Camera mCamera;

        public CameraViewportCalculator(Camera camera)
        {
            this.mCamera = camera;
        }

        public CameraViewportBounds GetBounds(GameObject gameObject)
        {
            if (gameObject == null) return CameraViewportBounds.Empty;

            var lPosition = this.mCamera.WorldToViewportPoint(gameObject.transform.position);
            var lSize = this.GetSize(gameObject);
            return new CameraViewportBounds(lPosition, lSize);
        }

        private Vector2 GetSize(GameObject gameObject)
        {
            List<Vector3> screenPoints = WorldToScreenPoints(GetVerticiesFromMesh(gameObject));

            //Calculate the min and max positions
            Vector3 min = screenPoints[0];
            Vector3 max = screenPoints[0];
            foreach (Vector3 screenPoint in screenPoints)
            {
                min = Vector3.Min(min, screenPoint);
                max = Vector3.Max(max, screenPoint);
            }

            //Construct a rect of the min and max positions
            Rect rect = GetBoundingRect(min, max);

            if (min.z > 0)
            {
                return new Vector2(rect.width, rect.height);
            }

            return Vector2.zero;
        }

        private Rect GetBoundingRect(Vector3 min, Vector3 max)
        {
            if (min.x < 0) min.x = 0;
            if (min.y < 0) min.y = 0;
            if (max.x > Screen.width) max.x = Screen.width;
            if (max.y > Screen.height) max.y = Screen.height;

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private List<Vector3> WorldToScreenPoints(List<Vector3> worldPoints)
        {
            List<Vector3> screenPoints = new List<Vector3>();

            foreach (Vector3 worldPoint in worldPoints)
            {
                Vector3 screenPoint = this.mCamera.WorldToScreenPoint(worldPoint);
                screenPoint.y = Screen.height - screenPoint.y;
                screenPoints.Add(screenPoint);
            }

            return screenPoints;
        }

        private List<Vector3> GetVerticiesFromMesh(GameObject gameObject)
        {
            Mesh mesh = gameObject.GetComponent<MeshFilter>().mesh;
            Vector3[] vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = gameObject.transform.TransformPoint(vertices[i]);
            }

            return new List<Vector3>(vertices);
        }
    }
}