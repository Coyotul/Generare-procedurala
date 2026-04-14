using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class Lab3LSystemPlantFGenerator : MonoBehaviour
{
    [Header("L-System (Plant f)")]
    [SerializeField] private string axiom = "X";
    [SerializeField] private int iterations = 5;
    [SerializeField] private float turnAngle = 22.5f;
    [SerializeField] private float segmentLength = 0.25f;
    [SerializeField] private float lengthScalePerDepth = 0.96f;

    [Header("Rendering")]
    [SerializeField] private Material lineMaterial;
    [SerializeField] private float lineWidth = 0.02f;
    [SerializeField] private bool autoGenerateOnStart;

    private readonly Dictionary<char, string> _rules = new Dictionary<char, string>
    {
        { 'X', "F-[[X]+X]+F[+FX]-X" },
        { 'F', "FF" }
    };

    private readonly Stack<TurtleState> _stateStack = new Stack<TurtleState>();
    private readonly List<LineRenderer> _linePool = new List<LineRenderer>();

    private struct TurtleState
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public int Depth;
    }

    private void Start()
    {
        if (autoGenerateOnStart)
        {
            GeneratePlant();
        }
    }

    [ContextMenu("Generate Plant F")]
    public void GeneratePlant()
    {
        ClearLines();

        string sentence = BuildSentence();
        DrawSentence(sentence);
    }

    [ContextMenu("Clear Plant")]
    public void ClearPlant()
    {
        ClearLines();
    }

    private string BuildSentence()
    {
        string current = string.IsNullOrWhiteSpace(axiom) ? "X" : axiom;
        int safeIterations = Mathf.Max(0, iterations);

        for (int i = 0; i < safeIterations; i++)
        {
            StringBuilder next = new StringBuilder(current.Length * 2);

            for (int j = 0; j < current.Length; j++)
            {
                char symbol = current[j];
                if (_rules.TryGetValue(symbol, out string replacement))
                {
                    next.Append(replacement);
                }
                else
                {
                    next.Append(symbol);
                }
            }

            current = next.ToString();
        }

        return current;
    }

    private void DrawSentence(string sentence)
    {
        _stateStack.Clear();

        Vector3 currentPosition = Vector3.zero;
        Quaternion currentRotation = Quaternion.identity;
        int depth = 0;

        for (int i = 0; i < sentence.Length; i++)
        {
            char symbol = sentence[i];

            if (symbol == 'F')
            {
                float scaledLength = segmentLength * Mathf.Pow(lengthScalePerDepth, depth);
                Vector3 nextPosition = currentPosition + (currentRotation * Vector3.up) * scaledLength;
                CreateLine(currentPosition, nextPosition, depth);
                currentPosition = nextPosition;
            }
            else if (symbol == '+')
            {
                currentRotation *= Quaternion.Euler(0f, 0f, turnAngle);
            }
            else if (symbol == '-')
            {
                currentRotation *= Quaternion.Euler(0f, 0f, -turnAngle);
            }
            else if (symbol == '[')
            {
                _stateStack.Push(new TurtleState
                {
                    Position = currentPosition,
                    Rotation = currentRotation,
                    Depth = depth
                });
                depth++;
            }
            else if (symbol == ']')
            {
                if (_stateStack.Count > 0)
                {
                    TurtleState state = _stateStack.Pop();
                    currentPosition = state.Position;
                    currentRotation = state.Rotation;
                    depth = state.Depth;
                }
            }
        }
    }

    private void CreateLine(Vector3 from, Vector3 to, int depth)
    {
        GameObject lineObject = new GameObject("PlantSegment");
        lineObject.transform.SetParent(transform, false);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.positionCount = 2;
        line.SetPosition(0, from);
        line.SetPosition(1, to);
        line.numCapVertices = 2;
        line.numCornerVertices = 2;

        float width = Mathf.Max(0.001f, lineWidth * Mathf.Pow(0.93f, depth));
        line.startWidth = width;
        line.endWidth = width;

        if (lineMaterial != null)
        {
            line.material = lineMaterial;
        }

        _linePool.Add(line);
    }

    private void ClearLines()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }

        _linePool.Clear();
    }
}
