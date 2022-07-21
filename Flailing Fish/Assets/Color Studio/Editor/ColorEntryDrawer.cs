using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

namespace ColorStudio
{
	[CustomPropertyDrawer(typeof(ColorEntry))]
	public class ColorEntryDrawer : PropertyDrawer
	{

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			bool markSceneChanges = false;

			EditorGUI.BeginProperty(position, label, property);

            EditorGUI.BeginChangeCheck();

			// Calculate rects
			var colorRect = new Rect(position.x, position.y, 80, position.height);
			var opRect = new Rect(position.x + 90, position.y, 110, position.height);
            var replaceColorRect = new Rect(position.x + 200, position.y, 80, position.height);
            var removeRect = new Rect(position.x + 290, position.y, 60, position.height);

			// Draw fields - passs GUIContent.none to each so they are drawn without labels
			EditorGUI.PropertyField(colorRect, property.FindPropertyRelative("color"), GUIContent.none);
            SerializedProperty prop = property.FindPropertyRelative("operation");
            EditorGUI.PropertyField(opRect, prop, GUIContent.none);
            if (prop.intValue == (int)ColorOperation.Replace) {
                EditorGUI.PropertyField(replaceColorRect, property.FindPropertyRelative("replaceColor"), GUIContent.none);
            }
            Recolor rc = (Recolor)property.serializedObject.targetObject;
            if (GUI.Button(removeRect, "Remove", EditorStyles.miniButton))
			{
				if (rc.colorOperations == null)
					return;

				List<ColorEntry> od = new List<ColorEntry>(rc.colorOperations);
				int index = property.GetArrayIndex();
				od.RemoveAt(index);
				rc.colorOperations = od.ToArray();
				markSceneChanges = true;
                rc.dirty = true;
                EditorUtility.SetDirty(rc);
			}

			EditorGUI.EndProperty();

            if (EditorGUI.EndChangeCheck()) {
                markSceneChanges = true;
            }

			if (markSceneChanges && !Application.isPlaying)
			{
                rc.dirty = true;
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
			}

		}
	}

}