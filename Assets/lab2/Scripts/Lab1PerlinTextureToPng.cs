using System.IO;
using Lab1;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Lab1PerlinTextureToPng : MonoBehaviour
{
    [Header("Texture")]
    [SerializeField] private int width = 512;
    [SerializeField] private int height = 512;
    [SerializeField] private float scale = 64f;
    [SerializeField] private Vector2 offset;

    [Header("Fractal Noise")]
    [SerializeField] private int octaves = 5;
    [SerializeField] private float lacunarity = 2f;
    [SerializeField] private float persistence = 0.5f;
    [SerializeField] private int seed = 12345;

    [Header("Output")]
    [SerializeField] private string fileName = "lab1_perlin.png";
    [SerializeField] private bool saveInProjectFolder = true;
    [SerializeField] private bool autoGenerateOnStart;

    private void Start()
    {
        if (autoGenerateOnStart)
        {
            GenerateAndSave();
        }
    }

    [ContextMenu("Generate PNG")]
    public void GenerateAndSave()
    {
        int safeWidth = Mathf.Max(2, width);
        int safeHeight = Mathf.Max(2, height);
        float safeScale = Mathf.Max(0.0001f, scale);

        Texture2D texture = new Texture2D(safeWidth, safeHeight, TextureFormat.RGBA32, false);

        for (int y = 0; y < safeHeight; y++)
        {
            for (int x = 0; x < safeWidth; x++)
            {
                float nx = offset.x + (x / safeScale);
                float ny = offset.y + (y / safeScale);

                float value = Lab1PerlinNoise.FractalNoise2D(nx, ny, octaves, lacunarity, persistence, seed);
                Color color = new Color(value, value, value, 1f);
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();

        string cleanName = string.IsNullOrWhiteSpace(fileName) ? "lab1_perlin.png" : fileName;
        if (!cleanName.EndsWith(".png"))
        {
            cleanName += ".png";
        }

        string directory = saveInProjectFolder
            ? Path.Combine(Application.dataPath, "lab1", "Generated")
            : Application.persistentDataPath;

        Directory.CreateDirectory(directory);

        string outputPath = Path.Combine(directory, cleanName);
        byte[] png = texture.EncodeToPNG();
        File.WriteAllBytes(outputPath, png);

#if UNITY_EDITOR
        if (saveInProjectFolder)
        {
            AssetDatabase.Refresh();
        }
#endif

        Debug.Log($"PNG generated: {outputPath}", this);
    }
}
