using UnityEngine;

public static class Noise
{
    private static readonly int[] _perm = CreatePermutation();

    private static int[] CreatePermutation()
    {
        System.Random rng = new System.Random(0);
        int[] p = new int[512];

        for (int i = 0; i < 256; i++)
        {
            p[i] = i;
        }

        for (int i = 255; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            int temp = p[i];
            p[i] = p[j];
            p[j] = temp;
        }

        for (int i = 0; i < 256; i++)
        {
            p[i + 256] = p[i];
        }

        return p;
    }

    public static float Perlin(Vector3 point)
    {
        int xi = Mathf.FloorToInt(point.x);
        int yi = Mathf.FloorToInt(point.y);
        int zi = Mathf.FloorToInt(point.z);

        int X = xi & 255;
        int Y = yi & 255;
        int Z = zi & 255;

        float x = point.x - xi;
        float y = point.y - yi;
        float z = point.z - zi;

        float u = Fade(x);
        float v = Fade(y);
        float w = Fade(z);

        int A = _perm[X] + Y;
        int AA = _perm[A] + Z;
        int AB = _perm[A + 1] + Z;
        int B = _perm[X + 1] + Y;
        int BA = _perm[B] + Z;
        int BB = _perm[B + 1] + Z;

        float x1 = Mathf.Lerp(Grad(_perm[AA], x, y, z), Grad(_perm[BA], x - 1, y, z), u);
        float x2 = Mathf.Lerp(Grad(_perm[AB], x, y - 1, z), Grad(_perm[BB], x - 1, y - 1, z), u);
        float y1 = Mathf.Lerp(x1, x2, v);

        float x3 = Mathf.Lerp(Grad(_perm[AA + 1], x, y, z - 1), Grad(_perm[BA + 1], x - 1, y, z - 1), u);
        float x4 = Mathf.Lerp(Grad(_perm[AB + 1], x, y - 1, z - 1), Grad(_perm[BB + 1], x - 1, y - 1, z - 1), u);
        float y2 = Mathf.Lerp(x3, x4, v);

        return Mathf.Lerp(y1, y2, w);
    }

    private static float Fade(float t)
    {
        return t * t * t * (t * (t * 6f - 15f) + 10f);
    }

    private static float Grad(int hash, float x, float y, float z)
    {
        int h = hash & 15;

        float u;
        if (h < 8)
        {
            u = x;
        }
        else
        {
            u = y;
        }

        float v;
        if (h < 4)
        {
            v = y;
        }
        else if (h == 12 || h == 14)
        {
            v = x;
        }
        else
        {
            v = z;
        }

        if ((h & 1) != 0)
        {
            u = -u;
        }

        if ((h & 2) != 0)
        {
            v = -v;
        }

        return u + v;
    }
}
