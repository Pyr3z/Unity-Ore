/*! @file       Editor/VisualTestingWindow.cs
 *  @author     Levi Perez (levi\@leviperez.dev)
 *  @date       2022-10-18
**/

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using EG  = UnityEditor.EditorGUI;
using EGL = UnityEditor.EditorGUILayout;
using EGU = UnityEditor.EditorGUIUtility;


namespace Ore.Editor
{
  public class VisualTestingWindow : EditorWindow, ISerializationCallbackReceiver
  {

    [MenuItem("Ore/Tools/Visual Testing (Ore)")]
    private static void OpenWindow()
    {
      var win = GetWindow<VisualTestingWindow>();
      if (!win)
      {
        win = CreateWindow<VisualTestingWindow>();
      }

      win.Show();
    }


    private enum Mode
    {
      None,
      Self,
      RasterLine,
      RasterCircle,
      ColorAnalysis,
      HashMaps
    }


    [SerializeField]
    private bool m_Foldout = true;

    [SerializeField]
    private Mode m_Mode;

    [SerializeField]
    private Color32 m_PrimaryColor = Colors.Pending;
    [SerializeField]
    private Color32 m_SecondaryColor = Colors.Boring;

    [SerializeField]
    private float m_MaxLength = 64f;
    [SerializeField]
    private float m_Length;
    [SerializeField]
    private bool m_UseExtraInts;
    [SerializeField]
    private int[] m_ExtraInts = { 0, 1 };

    [SerializeField]
    private int m_CircleErrorX = Raster.CircleDrawer.ERROR_X;
    [SerializeField]
    private int m_MaxCircleErrorX = 16;
    [SerializeField]
    private int m_CircleErrorY = Raster.CircleDrawer.ERROR_Y;
    [SerializeField]
    private int m_MaxCircleErrorY = 16;
    [SerializeField]
    private float m_CircleRadiusBias = Raster.CircleDrawer.RADIUS_BIAS;

    [SerializeField]
    private int[] m_HashMapKeys;
    [SerializeField]
    private string[] m_HashMapValues;

    [System.NonSerialized]
    private readonly HashMap<int,string> m_HashMap = new HashMap<int, string>();

    [System.NonSerialized]
    private GUIStyle m_SceneLabelStyle;


    private void OnBecameVisible()
    {
      titleContent.text = "[Ore]";
      name              = "";
      minSize           = new Vector2(300f, 300f);
    }

    private void OnEnable()
    {
      SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
      SceneView.duringSceneGui -= OnSceneGUI;
    }

    void ISerializationCallbackReceiver.OnBeforeSerialize()
    {
      m_HashMapKeys = new int[m_HashMap.Count];
      m_HashMapValues = new string[m_HashMap.Count];

      int i = 0;
      foreach (var (key,val) in m_HashMap)
      {
        m_HashMapKeys[i] = key;
        m_HashMapValues[i] = val;
        ++ i;
      }
    }

    void ISerializationCallbackReceiver.OnAfterDeserialize()
    {
      m_HashMap.Clear();

      int i = m_HashMapKeys?.Length ?? 0;
      while (i --> 0)
      {
        m_HashMap[m_HashMapKeys[i]] = m_HashMapValues[i];
      }
    }


    private void OnGUI()
    {
      m_Foldout = EGL.InspectorTitlebar(m_Foldout, this);

      if (!m_Foldout)
        return;

      OGUI.LabelWidth.Push(EGU.currentViewWidth * 0.333f);

      EGL.Space();

      m_Mode = (Mode)EGL.EnumPopup(Styles.BoldText("Active Mode"), m_Mode);

      EGL.Space();

      m_PrimaryColor   = EGL.ColorField("Color 1", m_PrimaryColor);
      m_SecondaryColor = EGL.ColorField("Color 2", m_SecondaryColor);

      OGUI.Draw.Separator();

      ++EG.indentLevel;

      switch (m_Mode)
      {
        case Mode.Self:           SelfInspector();          break;
        case Mode.RasterLine:     RasterLineInspector();    break;
        case Mode.RasterCircle:   RasterCircleInspector();  break;
        case Mode.ColorAnalysis:  ColorAnalysisInspector(); break;
        case Mode.HashMaps:       HashMapsInspector();      break;

        default:
          EGL.SelectableLabel("(there's nothing else here...)");
          break;
      }

      --EG.indentLevel;
      OGUI.LabelWidth.Pop();
    }

    private void OnSceneGUI(SceneView view)
    {
      if (m_Mode == Mode.None || view != SceneView.lastActiveSceneView)
        return;

      if (m_SceneLabelStyle is null)
      {
        m_SceneLabelStyle = new GUIStyle(EditorStyles.miniLabel)
        {
          alignment = TextAnchor.MiddleCenter,
          clipping  = TextClipping.Overflow,
          fontSize  = 9,
          normal =
          {
            textColor = m_PrimaryColor
          }
        };
      }

      view.sceneViewState.alwaysRefresh = true;

      var cam = view.camera;
      var min = cam.ViewportToWorldPoint(new Vector2(0.01f, 0.01f));
      var max = cam.ViewportToWorldPoint(new Vector2(0.95f, 0.95f));
      var visible = new RectInt(
        xMin:   (int) min.x,
        yMin:   (int) min.y, 
        width:  (int)(max.x - min.x),
        height: (int)(max.y - min.y)
      );

      var mouse = Event.current.mousePosition;
      mouse.y += 41f; // don't fuckin ask, just do it.
      mouse.y = Screen.height - mouse.y;
      mouse = cam.ScreenToWorldPoint(mouse);

      if (m_Mode == Mode.RasterLine)
      {
        RasterLineSceneGUI(visible, mouse);
      }
      else if (m_Mode == Mode.RasterCircle)
      {
        RasterCircleSceneGUI(visible, mouse);
      }
      else if (m_Mode == Mode.HashMaps)
      {
        HashMapsSceneGUI(visible, mouse);
      }
    }


    private void SelfInspector()
    {
      EGL.LabelField("Docked?", docked.ToInvariant());

      EG.BeginDisabledGroup(docked);
      if (docked)
      {
        EGL.RectField("Window Pos:", position);
      }
      else
      {
        position = EGL.RectField("Window Pos:", position);
      }
      EG.EndDisabledGroup();

      EGL.Space();

      EGL.LabelField("Label Width", $"{EGU.labelWidth:N1}");
      EGL.LabelField("Field Width", $"{EGU.fieldWidth:N1}");

      EGL.Space();

      EGL.LabelField("This", "is a GUI rectangle");
      var rect = GUILayoutUtility.GetLastRect();
      OGUI.Draw.Rect(rect, Colors.Comment);

      EGL.Space();

      var target = EGL.BeginBuildTargetSelectionGrouping();

      EGL.LabelField("Platform:", target.ToInvariant());

      EGL.EndBuildTargetSelectionGrouping();
    }


    private void RasterLineInspector()
    {
      EGL.BeginHorizontal();

      EGL.PrefixLabel("Line Length");
      m_Length = EGL.Slider(m_Length, 0f, m_MaxLength);
      m_MaxLength = EGL.DelayedFloatField(m_MaxLength, GUILayout.Width(60f));

      EGL.EndHorizontal();
    }

    private void RasterLineSceneGUI(RectInt visible, Vector2 mouse)
    {
      var start = visible.center;
      var direction = mouse - start;
      float distance = direction.magnitude;
      direction /= distance;

      if (m_Length > 0f)
      {
        distance = m_Length;
      }

      using (new Handles.DrawingScope(m_PrimaryColor.Inverted()))
      {
        Handles.DrawLine(new Vector2((int)start.x, (int)start.y), mouse);
      }

      var tile = new Rect(0f, 0f, 1f, 1f);
      var line = new Raster.LineDrawer().Prepare(start, direction, distance);

      while (line.MoveNext())
      {
        tile.position = line.Current;
        Handles.DrawSolidRectangleWithOutline(tile, m_PrimaryColor, m_SecondaryColor);
      }
    }


    private void RasterCircleInspector()
    {
      OGUI.SliderPlus("Radius", ref m_Length, 0f, ref m_MaxLength);

      OGUI.SliderPlus("Radius Bias", ref m_CircleRadiusBias, 0f, 1f);

      OGUI.SliderPlus("Error X", ref m_CircleErrorX, -10, ref m_MaxCircleErrorX);

      OGUI.SliderPlus("Error Y", ref m_CircleErrorY, -10, ref m_MaxCircleErrorY);

      m_UseExtraInts = EGL.BeginToggleGroup("Force Octant?", m_UseExtraInts);
      if (m_UseExtraInts)
      {
        OGUI.LabelWidth.Push(50f);
        --EG.indentLevel;

        for (int i = 0, ilen = Mathf.Min(m_ExtraInts.Length, 8); i < ilen; ++i)
        {
          m_ExtraInts[i] = EGL.IntSlider(i.ToString(), m_ExtraInts[i], 0, 7).Clamp(0, 7);

          if (GUILayout.Button("-", GUILayout.Width(EGU.labelWidth)))
          {
            var arr = new int[m_ExtraInts.Length - 1];
            for (int j = 0, k = 0; j < arr.Length; ++j, ++k)
            {
              if (j == i)
              {
                if (++k == m_ExtraInts.Length)
                  break;
              }

              arr[j] = m_ExtraInts[k];
            }

            m_ExtraInts = arr;
            break;
          }
        }

        if (m_ExtraInts.Length < 8)
        {
          if (GUILayout.Button("+ Add Octant"))
          {
            System.Array.Resize(ref m_ExtraInts, m_ExtraInts.Length + 1);
          }
        }

        ++EG.indentLevel;
        OGUI.LabelWidth.Pop();
      }

      EGL.EndToggleGroup(); // end "Force Octant?" group
    }

    private void RasterCircleSceneGUI(RectInt visible, Vector2 mouse)
    {
      var center = Vector2Int.FloorToInt(visible.center);
      float radius = m_Length;
      if (radius <= 0f)
      {
        radius = (mouse - center).magnitude;
      }

      using (new Handles.DrawingScope(m_PrimaryColor.Inverted()))
      {
        Handles.DrawWireArc(new Vector3(center.x + 0.5f, center.y + 0.5f), Vector3.forward, Vector3.right, 360f, radius);
      }

      Raster.CircleDrawer.FORCE_OCTANT = null;
      Raster.CircleDrawer.RADIUS_BIAS = m_CircleRadiusBias;
      Raster.CircleDrawer.ERROR_X = m_CircleErrorX;
      Raster.CircleDrawer.ERROR_Y = m_CircleErrorY;

      var circle = new Raster.CircleDrawer();
      var tile = new Rect(0f, 0f, 1f, 1f);
      int i = 0;
      do
      {
        if (m_UseExtraInts && i < m_ExtraInts.Length)
        {
          Raster.CircleDrawer.FORCE_OCTANT = m_ExtraInts[i];
        }

        foreach (var cell in circle.Prepare(center.x, center.y, radius))
        {
          tile.position = cell;
          Handles.DrawSolidRectangleWithOutline(tile, m_PrimaryColor,  m_SecondaryColor);
        }

      } while (m_UseExtraInts && ++i < m_ExtraInts.Length);

      tile.position = center;
      Handles.DrawSolidRectangleWithOutline(tile, m_SecondaryColor, m_PrimaryColor);
    }


    private void ColorAnalysisInspector()
    {
      EGL.Space();
      EGL.LabelField("Color1: ToInt32():", m_PrimaryColor.ToInt32().ToString("X8"));
      EGL.LabelField("Color1: GetHashCode():", m_PrimaryColor.GetHashCode().ToString("X8"));
      EGL.Space();
      EGL.LabelField("Color2: ToInt32():", m_SecondaryColor.ToInt32().ToString("X8"));
      EGL.LabelField("Color2: GetHashCode():", m_SecondaryColor.GetHashCode().ToString("X8"));
      EGL.Space();

      if (GUILayout.Button("Randomize Colors"))
      {
        m_PrimaryColor = Colors.Random();
        m_SecondaryColor = Colors.Random();
      }

      if (GUILayout.Button("Randomize Colors (Gray)"))
      {
        m_PrimaryColor = Colors.RandomGray();
        m_SecondaryColor = Colors.RandomGray();
      }

      if (GUILayout.Button("Randomize Colors (Dark)"))
      {
        m_PrimaryColor = Colors.RandomDark();
        m_SecondaryColor = Colors.RandomDark();
      }

      if (GUILayout.Button("Randomize Colors (Light)"))
      {
        m_PrimaryColor = Colors.RandomLight();
        m_SecondaryColor = Colors.RandomLight();
      }

      if (GUILayout.Button("Randomize Colors (Dark + Light)"))
      {
        m_PrimaryColor = Colors.RandomDark();
        m_SecondaryColor = Colors.RandomLight();
      }

      if (GUILayout.Button("Randomize Colors (Light + Dark)"))
      {
        m_PrimaryColor   = Colors.RandomLight();
        m_SecondaryColor = Colors.RandomDark();
      }

      if (GUILayout.Button("Invert Secondary"))
      {
        m_SecondaryColor = m_PrimaryColor.Inverted();
      }

      EGL.Space();

      m_Length = EGL.Slider("Fill Bar %", m_Length, 0f, 1f);

      OGUI.Draw.FillBar(m_Length, fill: m_PrimaryColor, textColor: m_SecondaryColor);

      OGUI.Draw.FillBar(m_Length, "With Label", fill: m_PrimaryColor, textColor: m_SecondaryColor);

      OGUI.Draw.FillBar(m_Length, "Default Colors");
    }


    private void HashMapsInspector()
    {
      EGL.LabelField("Internal Size:", m_HashMap.Buckets.Length.ToString());

      EGL.BeginHorizontal();
      EGL.LabelField("Grow Threshold:", m_HashMap.Capacity.ToString());
      float ratio = m_HashMap.Parameters.LoadFactor;
      OGUI.Draw.FillBar(ratio);
      EGL.EndHorizontal();

      EGL.BeginHorizontal();
      EGL.LabelField("Current Count:", m_HashMap.Count.ToString());
      ratio = (float)m_HashMap.Count / m_HashMap.Buckets.Length;
      OGUI.Draw.FillBar(ratio);
      EGL.EndHorizontal();

      EGL.BeginHorizontal();
      EGL.LabelField("Hash Collisions:", m_HashMap.Collisions.ToString());
      ratio = (float)m_HashMap.Collisions / m_HashMap.Buckets.Length;
      if (ratio > m_HashMap.Parameters.LoadFactor / 2f)
        OGUI.Draw.FillBar(ratio, fill: Colors.Attention);
      else if (ratio > 0.2f)
        OGUI.Draw.FillBar(ratio, fill: Colors.Pending);
      else
        OGUI.Draw.FillBar(ratio);
      EGL.EndHorizontal();

      EGL.BeginHorizontal();
      if (GUILayout.Button("Clear"))
      {
        m_HashMap.Clear();
      }
      if (GUILayout.Button("Reset Capacity"))
      {
        m_HashMap.ResetCapacity();
      }
      if (GUILayout.Button("Rehash"))
      {
        m_HashMap.Rehash();
      }
      EGL.EndHorizontal();

      if (GUILayout.Button("Map random int->string"))
      {
        m_HashMap.Map(Integers.RandomIndex(Primes.MaxValue), Colors.Random().ToHex());
      }

      OGUI.IndentLevel.Push(0);
      OGUI.LabelWidth.Push(55f);

      var editQueue = new Queue<(int k,string v)>();

      for (int i = 0; i < m_HashMap.Buckets.Length; ++i)
      {
        var bucket = m_HashMap.Buckets[i];

        if (bucket.Key == default)
          continue;

        EGL.BeginHorizontal();

        if (bucket.DirtyHash < 0)
          OGUI.ScratchContent.text = $"<color=#{m_SecondaryColor.ToHex()}>slot {i}:</color>";
        else
          OGUI.ScratchContent.text = $"slot {i}:";

        EG.BeginChangeCheck();
        int editKey = EGL.DelayedIntField(OGUI.ScratchContent, bucket.Key);
        if (EG.EndChangeCheck())
        {
          editQueue.Enqueue((bucket.Key,null));
          editQueue.Enqueue((editKey,bucket.Value));
        }

        EG.BeginChangeCheck();
        string edit = EGL.DelayedTextField(bucket.Value);
        if (EG.EndChangeCheck())
        {
          editQueue.Enqueue((bucket.Key, edit));
        }

        EGL.EndHorizontal();
      }

      while (editQueue.Count > 0)
      {
        var (key,val) = editQueue.Dequeue();
        if (val.IsEmpty())
        {
          m_HashMap.Unmap(key);
        }
        else
        {
          m_HashMap[key] = val;
        }
      }

      OGUI.LabelWidth.Pop();
      OGUI.IndentLevel.Pop();
    }

    private void HashMapsSceneGUI(RectInt visible, Vector2 mouse)
    {
      const float kOffset = 0.015f;

      visible.xMax -= 1;
      visible.yMax -= 3;

      var tile = new Rect(
        visible.xMin + kOffset,
        visible.yMax + kOffset,
        1f - 2 * kOffset,
        1f - 2 * kOffset
      );

      var guiSize = HandleUtility.WorldToGUIPoint(tile.position + Vector2.one);
      guiSize -= HandleUtility.WorldToGUIPoint(tile.position);
      guiSize.y *= -1;

      for (int i = 0; i < m_HashMap.Buckets.Length; ++i)
      {
        var bucket = m_HashMap.Buckets[i];

        Color32 fill, outline;
        if (bucket.DirtyHash == default && bucket.Key == default)
        {
          fill = Colors.Clear;
          outline = Colors.Boring;
        }
        else if (bucket.DirtyHash < 0)
        {
          m_SceneLabelStyle.normal.textColor = m_SecondaryColor;

          if (bucket.Value is null)
          {
            fill = Colors.Clear;
            outline = m_SecondaryColor;
          }
          else
          {
            fill = Colors.Boring;
            outline = m_SecondaryColor;
          }
        }
        else
        {
          fill = Colors.Boring;
          outline = m_PrimaryColor;
        }

        Handles.DrawSolidRectangleWithOutline(tile, fill, outline);

        Handles.BeginGUI();

        GUI.Label(
          new Rect(HandleUtility.WorldToGUIPoint(tile.position + Vector2.up), guiSize), 
          $"{i:00}",
          m_SceneLabelStyle
        );

        Handles.EndGUI();

        m_SceneLabelStyle.normal.textColor = m_PrimaryColor;

        if ((int)tile.x == visible.xMax)
        {
          tile.x = visible.xMin + kOffset;
          tile.y -= 1f;
        }
        else
        {
          tile.x += 1f;
        }
      }
    }

  } // end class VisualTestingWindow
}