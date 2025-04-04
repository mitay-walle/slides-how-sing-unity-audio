﻿#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameAssets.VolumetricAudio
{
    public static partial class VA_Helper
    {
        private static GUIStyle none;

        private static GUIStyle error;

        private static GUIStyle noError;

        [Serializable]
        private class PointsData
        {
            public Vector3[] points;
        }

        public static void Copy(IList<Vector3> points)
        {
            PointsData data = new PointsData() {points = points.ToArray()};
            EditorGUIUtility.systemCopyBuffer = JsonUtility.ToJson(data);
        }

        public static void Paste(out IList<Vector3> points)
        {
            PointsData data = JsonUtility.FromJson<PointsData>(EditorGUIUtility.systemCopyBuffer);
            points = data.points;
            EditorGUIUtility.systemCopyBuffer = JsonUtility.ToJson(data);
        }
    
        public static GUIStyle None
        {
            get
            {
                if (none == null)
                {
                    none = new GUIStyle();
                }

                return none;
            }
        }

        public static GUIStyle Error
        {
            get
            {
                if (error == null)
                {
                    error = new GUIStyle();
                    error.border = new RectOffset(3, 3, 3, 3);
                    error.normal = new GUIStyleState();
                    error.normal.background = CreateTempTexture(12, 12,
                        "iVBORw0KGgoAAAANSUhEUgAAAAUAAAAFCAYAAACNbyblAAAALElEQVQIHWP4z8CgC8SHgfg/lNZlQBIACYIlGEEMBjTABOQfQRM7AlKGYSYAoOwcvDRV9/MAAAAASUVORK5CYII=");
                }

                return error;
            }
        }

        public static GUIStyle NoError
        {
            get
            {
                if (noError == null)
                {
                    noError = new GUIStyle();
                    noError.border = new RectOffset(3, 3, 3, 3);
                    noError.normal = new GUIStyleState();
                }

                return noError;
            }
        }

        public static Texture2D CreateTempTexture(int width, int height, string encoded)
        {
            var texture = new Texture2D(width, height, TextureFormat.ARGB32, false);

            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.LoadImage(System.Convert.FromBase64String(encoded));
            texture.Apply();

            return texture;
        }

        public static Rect Reserve(float height = 16.0f)
        {
            var rect = default(Rect);

            EditorGUILayout.BeginVertical(NoError);
            {
                rect = EditorGUILayout.BeginVertical();
                {
                    EditorGUILayout.LabelField(string.Empty, GUILayout.Height(height));
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndVertical();

            return rect;
        }

        public static void SetDirty<T>(T t)
            where T : Object
        {
            if (t != null)
            {
                EditorUtility.SetDirty(t);
            }
        }
    }
}
#endif