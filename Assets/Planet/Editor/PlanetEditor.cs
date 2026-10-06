using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Planet))]
public class PlanetEditor : Editor
{
    private const int Width = 256;
    private const int Height = 128;

    private List<Texture2D> _octaveTextures = new List<Texture2D>();
    private Texture2D _sumTexture;

    public override void OnInspectorGUI()
    {
        Planet planet = (Planet)target;

        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        bool changed = EditorGUI.EndChangeCheck();

        if (changed || _sumTexture == null || _octaveTextures.Count != planet.Octaves)
        {
            UpdateTextures(planet);
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Noise preview", EditorStyles.boldLabel);

        for (int i = 0; i < _octaveTextures.Count; i++)
        {
            string label = "Octave " + i + "   (frequency " + planet.OctaveFrequency(i).ToString("0.##") + ", weight x" + planet.OctaveWeight(i).ToString("0.###") + ")";
            EditorGUILayout.LabelField(label);
            DrawTexture(_octaveTextures[i]);
        }

        EditorGUILayout.LabelField("Sum", EditorStyles.boldLabel);
        DrawTexture(_sumTexture);
    }

    private void DrawTexture(Texture2D texture)
    {
        Rect rect = GUILayoutUtility.GetAspectRect((float)Width / Height);
        EditorGUI.DrawPreviewTexture(rect, texture);
        EditorGUILayout.Space(4);
    }

    private void UpdateTextures(Planet planet)
    {
        DestroyTextures();

        int octaves = planet.Octaves;

        Color[][] octavePixels = new Color[octaves][];
        for (int i = 0; i < octaves; i++)
        {
            octavePixels[i] = new Color[Width * Height];
        }

        Color[] sumPixels = new Color[Width * Height];

        float maxSum = 0f;
        for (int i = 0; i < octaves; i++)
        {
            maxSum += planet.OctaveWeight(i);
        }

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                float longitude = (x / (Width - 1f)) * 2f * Mathf.PI - Mathf.PI;
                float latitude = (y / (Height - 1f)) * Mathf.PI - Mathf.PI / 2f;

                Vector3 direction = new Vector3(
                    Mathf.Cos(latitude) * Mathf.Cos(longitude),
                    Mathf.Sin(latitude),
                    Mathf.Cos(latitude) * Mathf.Sin(longitude));

                int pixel = y * Width + x;
                float sum = 0f;

                for (int i = 0; i < octaves; i++)
                {
                    float value = planet.Octave(direction, i);
                    sum += value;
                    octavePixels[i][pixel] = Gray(value / planet.OctaveWeight(i));
                }

                sumPixels[pixel] = Gray(sum / maxSum);
            }
        }

        for (int i = 0; i < octaves; i++)
        {
            _octaveTextures.Add(CreateTexture(octavePixels[i]));
        }

        _sumTexture = CreateTexture(sumPixels);
    }

    private Color Gray(float value)
    {
        float g = value * 0.5f + 0.5f;
        return new Color(g, g, g);
    }

    private Texture2D CreateTexture(Color[] pixels)
    {
        Texture2D texture = new Texture2D(Width, Height);
        texture.hideFlags = HideFlags.DontSave;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    private void DestroyTextures()
    {
        foreach (Texture2D texture in _octaveTextures)
        {
            DestroyImmediate(texture);
        }

        _octaveTextures.Clear();

        if (_sumTexture != null)
        {
            DestroyImmediate(_sumTexture);
        }
    }

    private void OnDisable()
    {
        DestroyTextures();
    }
}
