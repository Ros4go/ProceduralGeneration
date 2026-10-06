using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Planet : MonoBehaviour
{
    [Header("Sphere")]
    [SerializeField, Range(1f, 50f)] private float _radius = 10f;
    [SerializeField, Range(8, 128)] private int _resolution = 48;

    [Header("Relief")]
    [SerializeField, Range(0f, 10f)] private float _amplitude = 2f;
    [SerializeField, Range(0.1f, 10f)] private float _frequency = 1.5f;
    [SerializeField, Range(1, 8)] private int _octaves = 4;
    [SerializeField] private Vector3 _noiseOffset = Vector3.zero;

    private Mesh _mesh;

    public int Octaves
    {
        get { return _octaves; }
    }

    private void OnEnable()
    {
        Generate();
    }

    private void OnDisable()
    {
        if (_mesh != null)
        {
            DestroyImmediate(_mesh);
        }
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall += DelayedGenerate;
#endif
    }

    private void DelayedGenerate()
    {
        if (this != null)
        {
            Generate();
        }
    }

    private float Density(Vector3 point)
    {
        float distance = point.magnitude;
        if (distance < 0.0001f)
        {
            return _radius;
        }

        Vector3 direction = point / distance;
        float surface = _radius + Relief(direction);
        return surface - distance;
    }

    public float Relief(Vector3 direction)
    {
        float height = 0f;
        for (int i = 0; i < _octaves; i++)
        {
            height += Octave(direction, i);
        }

        return height * _amplitude;
    }

    public float Octave(Vector3 direction, int i)
    {
        return Noise.Perlin(direction * OctaveFrequency(i) + _noiseOffset) * OctaveWeight(i);
    }

    public float OctaveFrequency(int i)
    {
        return _frequency * Mathf.Pow(2f, i);
    }

    public float OctaveWeight(int i)
    {
        return Mathf.Pow(0.5f, i);
    }

    private void Generate()
    {
        float size = 2f * (_radius + _amplitude * 2f) + 1f;
        float cubeSize = size / _resolution;
        Vector3 gridOrigin = -Vector3.one * size / 2f;

        int points = _resolution + 1;
        float[,,] density = new float[points, points, points];

        for (int x = 0; x < points; x++)
        {
            for (int y = 0; y < points; y++)
            {
                for (int z = 0; z < points; z++)
                {
                    density[x, y, z] = Density(gridOrigin + new Vector3(x, y, z) * cubeSize);
                }
            }
        }

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        float[] cornerDensity = new float[8];
        Vector3[] cornerPosition = new Vector3[8];

        for (int x = 0; x < _resolution; x++)
        {
            for (int y = 0; y < _resolution; y++)
            {
                for (int z = 0; z < _resolution; z++)
                {
                    int cubeIndex = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        int cx = x + MarchingCubesTables.CornerOffsets[i, 0];
                        int cy = y + MarchingCubesTables.CornerOffsets[i, 1];
                        int cz = z + MarchingCubesTables.CornerOffsets[i, 2];

                        cornerDensity[i] = density[cx, cy, cz];
                        cornerPosition[i] = gridOrigin + new Vector3(cx, cy, cz) * cubeSize;

                        if (cornerDensity[i] > 0f)
                        {
                            cubeIndex |= 1 << i;
                        }
                    }

                    for (int t = 0; t < 16; t++)
                    {
                        int edge = MarchingCubesTables.TriTable[cubeIndex, t];
                        if (edge == -1)
                        {
                            break;
                        }

                        int a = MarchingCubesTables.EdgeCorners[edge, 0];
                        int b = MarchingCubesTables.EdgeCorners[edge, 1];

                        float k = cornerDensity[a] / (cornerDensity[a] - cornerDensity[b]);
                        vertices.Add(Vector3.Lerp(cornerPosition[a], cornerPosition[b], k));
                        triangles.Add(vertices.Count - 1);
                    }
                }
            }
        }

        if (_mesh == null)
        {
            _mesh = new Mesh();
            _mesh.name = "Planet";
            _mesh.hideFlags = HideFlags.DontSave;
            _mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            GetComponent<MeshFilter>().sharedMesh = _mesh;
        }

        _mesh.Clear();
        _mesh.SetVertices(vertices);
        _mesh.SetTriangles(triangles, 0);
        _mesh.RecalculateNormals();
    }
}
