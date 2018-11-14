using UnityEngine;

public class Main : MonoBehaviour
{
    public Texture2D texture;
    public GameObject maskDisplay;
    public GameObject bevelDisplay;
    public GameObject distanceDisplay;
    public GameObject heightDisplay;
    public GameObject normalDisplay;

    private void Awake ()
    {
        int width = texture.width;
        int height = texture.height;
        Color[] maskPixels = texture.GetPixels();
        float[] mask = ColorToFloat(maskPixels);
        Display(maskDisplay, width, height, maskPixels);

        float[] bevel = GenerateBevel(width, height, ref mask);
        Display(bevelDisplay, width, height, FloatToColor(bevel));

        int N = 50;
        float[] distance = bevel;
        for (int i = 0; i < N; ++i)
        {
            distance = GenerateDistance(width, height, ref distance, N);
        }
        Display(distanceDisplay, width, height, FloatToColor(distance));

        float[] heightMap = GenerateHeight(width, height, ref mask, ref distance);
        heightMap = Normalize(ref heightMap);
        Display(heightDisplay, width, height, FloatToColor(heightMap));

        Vector3[] normalPixels = GenerateNormal(width, height, ref heightMap);
        Display(normalDisplay, width, height, VectorToColor(normalPixels));
    }

    private float[] GenerateBevel(int width, int height, ref float[] input)
    {
        float[] output = new float[width * height];
        for (int y = 0; y < height; ++y)
        {
            for (int x = 0; x < width; ++x)
            {
                int mainIndex = GetIndex(x, y, width, height);
                float mainMask = input[mainIndex];

                bool edge = false;
                for (int ky = -1; ky <= 1; ++ky)
                {
                    for (int kx = -1; kx <= 1; ++kx)
                    {
                        int index = GetIndex(x + kx, y + ky, width, height);
                        float mask = input[index];

                        // Edge is found if pixels in kernel differs from main pixel.
                        edge = (mainMask != mask) || edge;
                    }
                }

                // True edge if pixel inside kernel differs.
                float final = edge ? 1.0f : 0.0f;

                output[mainIndex] = 1.0f - (final * mainMask);
            }
        }

        return output;
    }

    private float[] GenerateDistance(int width, int height, ref float[] input, int N)
    {
        float[] output = new float[width * height];
        for (int y = 0; y < height; ++y)
        {
            for (int x = 0; x < width; ++x)
            {
                int mainIndex = GetIndex(x, y, width, height);
                float mainBevel = input[mainIndex];

                //if (mainBevel < 0.5f)
                //{
                //    bool debug = true;
                //}

                float min = Mathf.Sqrt(2);
                for (int ky = -1; ky <= 1; ++ky)
                {
                    for (int kx = -1; kx <= 1; ++kx)
                    {
                        int index = GetIndex(x + kx, y + ky, width, height);
                        float pixel = input[index];
                        float weight = GetWeight(kx, ky) / Mathf.Sqrt(2) / N;
                        float value = pixel + weight / Mathf.Sqrt(2);

                        // Store minimum.
                        min = System.Math.Min(min, value);
                    }
                }

                output[mainIndex] = min;
            }
        }

        return output;
    }

    private float[] GenerateHeight(int width, int height, ref float[] inputMask, ref float[] inputDistance)
    {
        float[] output = new float[width * height];
        for (int y = 0; y < height; ++y)
        {
            for (int x = 0; x < width; ++x)
            {
                int mainIndex = GetIndex(x, y, width, height);
                float maskMain = inputMask[mainIndex];
                float distMain = inputDistance[mainIndex];

                output[mainIndex] = maskMain * distMain;
            }
        }

        return output;
    }

    private float[] Normalize(ref float[] input)
    {
        float max = 0.0f;
        for (int i = 0; i < input.Length; ++i)
        {
            max = System.Math.Max(input[i], max);
        }

        float[] output = new float[input.Length];
        for (int i = 0; i < input.Length; ++i)
        {
            output[i] = input[i] / max;
        }

        return output;
    }

    private Vector3[] GenerateNormal(int width, int height, ref float[] input)
    {
        Vector3[] output = new Vector3[width * height];
        for (int y = 0; y < height; ++y)
        {
            for (int x = 0; x < width; ++x)
            {
                int mainIndex = GetIndex(x, y, width, height);
                float mainHeight = input[mainIndex];

                float s11 = input[GetIndex(x, y, width, height)];
                float s01 = input[GetIndex(x - 1, y, width, height)];
                float s21 = input[GetIndex(x + 1, y, width, height)];
                float s10 = input[GetIndex(x, y - 1, width, height)];
                float s12 = input[GetIndex(x, y + 1, width, height)];

                Vector3 va = Vector3.Normalize(new Vector3(2.0f, 0.0f, s21 - s01));
                Vector3 vb = Vector3.Normalize(new Vector3(0.0f, 2.0f, s12 - s10));
                Vector3 normal = Vector3.Cross(va, vb);

                normal = Vector3.Scale(normal + Vector3.one, new Vector3(0.5f, 0.5f, 0.5f));

                output[mainIndex] = normal;
            }
        }

        return output;
    }

    private float GetWeight(int x, int y)
    {
        return Vector2.Distance(Vector2.zero, new Vector2(x, y));
    }

    private int GetIndex(int x, int y, int width, int height)
    {
        return Clamp(x, 0, width - 1) + (Clamp(y, 0, height - 1) * width);
    }

    private int Clamp(int value, int min, int max)
    {
        return System.Math.Min(System.Math.Max(value, min), max);
    }

    private void Display(GameObject display, int width, int height, Color[] pixels)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.ARGB32, false);
        tex.filterMode = FilterMode.Point;
        tex.SetPixels(pixels);
        tex.Apply();

        display.GetComponent<MeshRenderer>().material.mainTexture = tex;
    }

    private Color[] FloatToColor(float[] input)
    {
        Color[] output = new Color[input.Length];
        for (int i = 0; i < input.Length; ++i)
        {
            float v = input[i];
            output[i] = new Color(v, v, v, 1);
        }

        return output;
    }

    private float[] ColorToFloat(Color[] input)
    {
        float[] output = new float[input.Length];
        for (int i = 0; i < input.Length; ++i)
        {
            output[i] = input[i].r;
        }

        return output;
    }

    private Color[] VectorToColor(Vector3[] input)
    {
        Color[] output = new Color[input.Length];
        for (int i = 0; i < input.Length; ++i)
        {
            Vector3 v = input[i];
            output[i] = new Color(v.x, v.y, v.z, 1);
        }

        return output;
    }
}
