using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Tema
{
    /// <summary>
    /// Planta procedurala generata cu un L-system (turtle graphics), configurabila:
    /// axioma, reguli, unghi, iteratii. Deseneaza segmentele cu LineRenderer.
    /// Spawner-ul o configureaza in cod (Configure) si apoi cheama Build().
    /// </summary>
    public class TemaLSystemPlant : MonoBehaviour
    {
        [Header("L-System")]
        [SerializeField] private string axiom = "X";
        // Reguli in format "simbol=inlocuire", ex: "X=F-[[X]+X]+F[+FX]-X".
        [SerializeField] private string[] rules = { "X=F-[[X]+X]+F[+FX]-X", "F=FF" };
        [SerializeField] private int iterations = 4;
        [SerializeField] private float angle = 22.5f;
        [SerializeField] private float segmentLength = 0.35f;
        [SerializeField] private float lengthDecayPerDepth = 0.92f;
        [SerializeField] private float baseWidth = 0.06f;
        [SerializeField] private Color color = new Color(0.30f, 0.55f, 0.25f);

        private readonly Dictionary<char, string> _ruleMap = new Dictionary<char, string>();
        private readonly Stack<TurtleState> _stack = new Stack<TurtleState>();
        private Material _sharedMaterial;

        private struct TurtleState
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public int Depth;
        }

        /// <summary>Configurare din cod (folosita de spawner).</summary>
        public void Configure(string newAxiom, string[] newRules, int newIterations,
            float newAngle, float newSegmentLength, Color newColor)
        {
            axiom = newAxiom;
            rules = newRules;
            iterations = newIterations;
            angle = newAngle;
            segmentLength = newSegmentLength;
            color = newColor;
        }

        [ContextMenu("Build Plant")]
        public void Build()
        {
            ClearSegments();
            ParseRules();
            string sentence = Expand();
            Draw(sentence);
        }

        private void ParseRules()
        {
            _ruleMap.Clear();
            if (rules == null) return;
            foreach (string rule in rules)
            {
                if (string.IsNullOrEmpty(rule)) continue;
                int eq = rule.IndexOf('=');
                if (eq <= 0 || eq >= rule.Length - 1) continue;
                char key = rule[0];
                string value = rule.Substring(eq + 1);
                _ruleMap[key] = value;
            }
        }

        private string Expand()
        {
            string current = string.IsNullOrEmpty(axiom) ? "X" : axiom;
            int safeIterations = Mathf.Clamp(iterations, 0, 7);
            for (int i = 0; i < safeIterations; i++)
            {
                StringBuilder next = new StringBuilder(current.Length * 2);
                foreach (char c in current)
                {
                    if (_ruleMap.TryGetValue(c, out string replacement)) next.Append(replacement);
                    else next.Append(c);
                }
                current = next.ToString();
            }
            return current;
        }

        private void Draw(string sentence)
        {
            EnsureMaterial();
            _stack.Clear();

            Vector3 pos = Vector3.zero;
            Quaternion rot = Quaternion.identity;
            int depth = 0;

            foreach (char symbol in sentence)
            {
                switch (symbol)
                {
                    case 'F':
                        float len = segmentLength * Mathf.Pow(lengthDecayPerDepth, depth);
                        Vector3 next = pos + rot * Vector3.up * len;
                        CreateSegment(pos, next, depth);
                        pos = next;
                        break;
                    case '+':
                        rot *= Quaternion.Euler(0f, 0f, angle);
                        break;
                    case '-':
                        rot *= Quaternion.Euler(0f, 0f, -angle);
                        break;
                    case '[':
                        _stack.Push(new TurtleState { Position = pos, Rotation = rot, Depth = depth });
                        depth++;
                        break;
                    case ']':
                        if (_stack.Count > 0)
                        {
                            TurtleState s = _stack.Pop();
                            pos = s.Position; rot = s.Rotation; depth = s.Depth;
                        }
                        break;
                }
            }
        }

        private void EnsureMaterial()
        {
            if (_sharedMaterial != null) return;
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            _sharedMaterial = new Material(shader);
            if (_sharedMaterial.HasProperty("_BaseColor")) _sharedMaterial.SetColor("_BaseColor", color);
            _sharedMaterial.color = color;
        }

        private void CreateSegment(Vector3 from, Vector3 to, int depth)
        {
            GameObject go = new GameObject("Segment");
            go.transform.SetParent(transform, false);

            LineRenderer line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.SetPosition(0, from);
            line.SetPosition(1, to);
            line.numCapVertices = 2;
            line.numCornerVertices = 2;
            line.material = _sharedMaterial;
            line.startColor = color;
            line.endColor = color;

            float width = Mathf.Max(0.005f, baseWidth * Mathf.Pow(0.85f, depth));
            line.startWidth = width;
            line.endWidth = width;
        }

        private void ClearSegments()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
        }
    }
}
