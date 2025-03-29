using System.Runtime.CompilerServices;
using UnityEngine;


namespace GameAssets.VolumetricAudio
{
    public struct CullingRect
    {
        public float MinX;
        public float MinZ;
        public float MaxX;
        public float MaxZ;

        public CullingRect(float minX, float minZ, float maxX, float maxZ)
        {
            MinX = minX;
            MinZ = minZ;
            MaxX = maxX;
            MaxZ = maxZ;
        }

        public void Reset()
        {
            MinX = float.MaxValue;
            MinZ = float.MaxValue;
            MaxX = float.MinValue;
            MaxZ = float.MinValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Expand(Vector3 point)
        {
            if (point.x > MaxX)
                MaxX = point.x;
            else if (point.x < MinX)
                MinX = point.x;

            if (point.z > MaxZ)
                MaxZ = point.z;
            else if (point.z < MinZ)
                MinZ = point.z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Expand(float margin)
        {
            MinX -= margin;
            MinZ -= margin;
            MaxX += margin;
            MaxZ += margin;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsPointIn(Vector3 point)
        {
            return point.x >= MinX && point.x <= MaxX && point.z >= MinZ && point.z <= MaxZ;
        }

        public void Draw(float height)
        {
            var p1 = new Vector3(MinX, height, MinZ);
            var p2 = new Vector3(MinX, height, MaxZ);
            var p3 = new Vector3(MaxX, height, MaxZ);
            var p4 = new Vector3(MaxX, height, MinZ);
            Gizmos.DrawLine(p1, p2);
            Gizmos.DrawLine(p2, p3);
            Gizmos.DrawLine(p3, p4);
            Gizmos.DrawLine(p4, p1);
        }
    }
}
