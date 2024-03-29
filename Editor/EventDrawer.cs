/*! @file       Editor/EventDrawer.cs
 *  @author     Levi Perez (levi\@leviperez.dev)
 *  @date       2022-06-14
**/

using UnityEngine;
using UnityEditor;


namespace Ore.Editor
{

  [CustomPropertyDrawer(typeof(IEvent), useForChildren: true)] // interfaces don't actually work..
  [CustomPropertyDrawer(typeof(DelayedEvent), useForChildren: true)] // ... so gotta be explicit.
  [CustomPropertyDrawer(typeof(VoidEvent), useForChildren: true)]
  internal class EventDrawer : UnityEditorInternal.UnityEventDrawer
  {
    private const string UNITYEVENT_LAST_PROPERTY = "m_PersistentCalls";
    private const string LABEL_SUFFIX_DISABLED = " (event disabled)";

    private const float STD_PAD           = OGUI.STD_PAD;
    private const float STD_LINE_HEIGHT   = OGUI.STD_LINE_HEIGHT;
    private const float STD_PAD_HALF      = OGUI.STD_PAD / 2f;
    private const float UNEXPANDED_HEIGHT = STD_LINE_HEIGHT + OGUI.STD_PAD;

    private class DrawerState : PropertyDrawerState
    {
      public IEvent Event;
      public int ChildCount;
      public float ExtraHeight;
      public string EventLabel;

      public SerializedProperty RunInGlobalContext, Context;

      protected override void OnUpdateProperty()
      {
        if (CheckFails(out var root))
          return;

        _ = root.TryGetUnderlyingValue(out Event);

        ChildCount = 0;
        ExtraHeight = UNEXPANDED_HEIGHT;

        var iterator = root.FindPropertyRelative(UNITYEVENT_LAST_PROPERTY);

        while (iterator.NextVisible(false) &&
               iterator.depth == root.depth + 1 &&
               iterator.propertyPath.StartsWith(root.propertyPath))
        {
          ExtraHeight += EditorGUI.GetPropertyHeight(iterator, iterator.isExpanded) + OGUI.STD_PAD;
          ++ChildCount;
        }

        EventLabel = $"{Event.GetType().Name}: {root.displayName}";

        // done filling state.

        AutoUpdateHiddenFields(root);
      }

      public void UpdateExtraHeight()
      {
        if (CheckFails(out var root))
          return;

        IsStale = false;

        if (ChildCount == 0)
          return;

        ExtraHeight = UNEXPANDED_HEIGHT;

        if (Context != null && RunInGlobalContext != null)
          ExtraHeight += STD_LINE_HEIGHT + STD_PAD;

        var iterator = root.FindPropertyRelative(UNITYEVENT_LAST_PROPERTY);

        int i = 0;
        while (i++ < ChildCount && iterator.NextVisible(false))
        {
          ExtraHeight += EditorGUI.GetPropertyHeight(iterator, iterator.isExpanded) + STD_PAD;
        }
      }

      public SerializedProperty GetChildIterator()
      {
        if (ChildCount == 0 || CheckFails(out var root))
          return null;

        return root.FindPropertyRelative(UNITYEVENT_LAST_PROPERTY);
      }


      private void AutoUpdateHiddenFields(SerializedProperty root)
      {
        // MonoBehaviour m_Context
        Context = root.FindPropertyRelative("m_Context");
        if (Context is null)
        {
        }
        else if (OAssert.Fails(Context.propertyType == SerializedPropertyType.ObjectReference))
        {
          Context = null;
        }
        else
        {
          if (root.serializedObject.targetObject is MonoBehaviour owner)
            Context.objectReferenceValue = owner;
          else if (root.serializedObject.targetObject is ScriptableObject contract)
            Context.objectReferenceValue = contract;
          else
            Context = null;
        }

        // bool m_RunInGlobalContext
        RunInGlobalContext = root.FindPropertyRelative("m_RunInGlobalContext");
        if (RunInGlobalContext is null)
        {
        }
        else if (OAssert.Fails(RunInGlobalContext.propertyType == SerializedPropertyType.Boolean))
        {
          RunInGlobalContext = null;
        }
        else if (Context == null || !Context.objectReferenceValue || Context.objectReferenceValue is ScriptableObject)
        {
          RunInGlobalContext.boolValue = true;
        }

        root.serializedObject.ApplyModifiedProperties();
      }

    } // end internal class DrawerState


    public override void OnGUI(Rect total, SerializedProperty prop, GUIContent label)
    {
      PropertyDrawerState.Restore(prop, out DrawerState state);

      // enable/disable button
      float btn_begin = OGUI.FieldStartX + OGUI.FieldWidth * 0.45f;
      var pos = new Rect(btn_begin, total.y + STD_PAD_HALF, total.xMax - btn_begin, STD_LINE_HEIGHT);

      string btn_label;
      if (state.Event.IsEnabled)
        btn_label = "Disable Event";
      else
        btn_label = "Enable Event";

      if (GUI.Button(pos, btn_label))
      {
        Undo.RecordObject(prop.serializedObject.targetObject, btn_label);

        state.Event.IsEnabled = !state.Event.IsEnabled;
        prop.serializedObject.Update();

        return;
      }

      if (!state.Event.IsEnabled)
        label.text += LABEL_SUFFIX_DISABLED;

      // now do foldout header:
      pos.x = total.x;
      pos.xMax = btn_begin - STD_PAD * 2;

      using (var header = FoldoutHeader.Open(pos, label, prop, !state.Event.IsEnabled, indent: prop.depth+1))
      {
        if (header.IsOpen)
        {
          pos.x = header.Rect.x;
          pos.xMax = total.xMax;
          pos.y += pos.height + STD_PAD;

          // draw the optional field for "Run In Global Context" bool (if applicable)
          if (state.RunInGlobalContext != null)
          {
            label.text = state.RunInGlobalContext.displayName;
            pos.height = STD_LINE_HEIGHT;

            pos.xMax = OGUI.LabelEndX;

            EditorGUI.BeginDisabledGroup(prop.serializedObject.targetObject is ScriptableObject);
            _ = EditorGUI.PropertyField(pos, state.RunInGlobalContext, label);
            EditorGUI.EndDisabledGroup();

            // draw context reference (read-only info)
            pos.x = pos.xMax + STD_LINE_HEIGHT;
            pos.xMax = total.xMax;

            if (state.RunInGlobalContext.boolValue)
            {
              label.text = $"→ will run on Runtime {Styles.ColorText(nameof(ActiveScene), Colors.Reference)}";
            }
            else if (state.Context != null && state.Context.objectReferenceValue)
            {
              label.text = $"→ will run on this {Styles.ColorText(state.Context.objectReferenceValue.GetType().Name, Colors.Reference)}";
            }
            else
            {
              Orator.Error($"{nameof(EventDrawer)} failed to set a valid context reference when one was expected.", prop.serializedObject.targetObject);
              label.text = Styles.ColorText("ERR: bad serial context", Colors.Attention);
            }

            EditorGUI.LabelField(pos, label);

            pos.x = header.Rect.x;

            pos.xMax = total.xMax;
            pos.y += pos.height + STD_PAD;
          }

          // get the property iterator for our extra members:
          var child_prop = state.GetChildIterator();
          if (child_prop != null)
          {
            int i = 0;

            // iterate:
            while (i++ < state.ChildCount && child_prop.NextVisible(false))
            {
              label.text = child_prop.displayName;
              pos.height = EditorGUI.GetPropertyHeight(child_prop, label, includeChildren: true);

              _ = EditorGUI.PropertyField(pos, child_prop, label, includeChildren: true);

              pos.y += pos.height + STD_PAD;
            }

            child_prop.Dispose();
          }

          // finally, draw the vanilla event interface:
          pos.xMin += OGUI.Indent;
          pos.yMax = total.yMax;

          label.text = state.EventLabel;
          base.OnGUI(pos, prop, label);
        }
      }
    }


    public override float GetPropertyHeight(SerializedProperty prop, GUIContent label)
    {
      if (!prop.isExpanded)
        return UNEXPANDED_HEIGHT;

      PropertyDrawerState.Restore(prop, out DrawerState state);

      if (state.IsStale)
        state.UpdateExtraHeight();

      return base.GetPropertyHeight(prop, label) + STD_PAD_HALF + state.ExtraHeight;
    }

  } // end class EventDrawer

}
