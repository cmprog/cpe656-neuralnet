using UnityEngine;

namespace UnityStandardAssets.Vehicles.Car
{
    public sealed class CameraViewportBounds
    {
        private readonly Vector3 mPosition;
        private readonly Vector2 mSize;

        private static readonly CameraViewportBounds sEmpty;
        public static CameraViewportBounds Empty { get { return sEmpty; } }

        static CameraViewportBounds()
        {
            sEmpty = new CameraViewportBounds(
                new Vector3(-1.0f, -1.0f, -1.0f),
                new Vector2(0.0f, 0.0f));

            System.Diagnostics.Debug.Assert(!sEmpty.IsInBounds);
        }

        public CameraViewportBounds(Vector3 position, Vector2 size)
        {
            this.mPosition = position;
            this.mSize = size;
        }

        private bool IsInFrontOfCamera
        {
            get { return this.mPosition.z > 0; }
        }

        public bool IsInBounds
        {
            get
            {
                return this.IsInFrontOfCamera &&
                       (0.0 <= this.mPosition.x) && (this.mPosition.x <= 1.0f) &&
                       (0.0 <= this.mPosition.y) && (this.mPosition.y <= 1.0f);
            }
        }

        public float X { get { return this.mPosition.x; } }
        public float Y { get { return this.mPosition.y; } }
        public float Width { get { return this.mSize.x; } }
        public float Height { get { return this.mSize.y; } }

        public override string ToString()
        {
            return string.Format("({0}, {1}, {2}, {3}, [{4}])", this.X, this.Y, this.Width, this.Height, this.mPosition.z);
        }
    }
}