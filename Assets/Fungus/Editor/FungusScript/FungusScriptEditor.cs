using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Fungus;
using Rotorz.ReorderableList;
using System.Linq;

namespace Fungus.Script
{
	[CustomEditor (typeof(FungusScript))]
	public class FungusScriptEditor : Editor 
	{
		SerializedProperty variablesProperty;
		
		void OnEnable() 
		{
			if (serializedObject != null)
			{
				variablesProperty = serializedObject.FindProperty("variables");
			}
		}
		
		public void OnInspectorUpdate()
		{
			Repaint();
		}
		
		public override void OnInspectorGUI() 
		{
			serializedObject.Update();
			
			FungusScript t = target as FungusScript;
			
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Open Fungus Editor"))
			{
				EditorWindow.GetWindow(typeof(FungusEditorWindow), false, "Fungus Editor");
			}

			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			
			GUIContent stepTimeLabel = new GUIContent("Step Time", "Minimum time to execute each step");
			t.stepTime = EditorGUILayout.FloatField(stepTimeLabel, t.stepTime);
			
			GUIContent startSequenceLabel = new GUIContent("Start Sequence", "Sequence to be executed when controller starts.");
			t.startSequence = SequenceEditor.SequenceField(startSequenceLabel, t, t.startSequence);

			if (t.startSequence == null)
			{
				GUIStyle style = new GUIStyle(GUI.skin.label);
				style.normal.textColor = new Color(1,0,0);
				EditorGUILayout.LabelField(new GUIContent("Error: Please select a Start Sequence"), style);
			}

			GUIContent startAutomaticallyLabel = new GUIContent("Start Automatically", "Start this Fungus Script when the scene starts.");
			t.startAutomatically = EditorGUILayout.Toggle(startAutomaticallyLabel, t.startAutomatically);

			EditorGUILayout.Separator();
			GUILayout.Box("Sequence Editor", GUILayout.ExpandWidth(true));
			
			GUILayout.BeginHorizontal();

			if (GUILayout.Button("New"))
			{
				GameObject go = new GameObject("Sequence");
				go.transform.parent = t.transform;
				Sequence s = go.AddComponent<Sequence>();
				FungusEditorWindow fungusEditorWindow = EditorWindow.GetWindow(typeof(FungusEditorWindow), false, "Fungus Editor") as FungusEditorWindow;
				s.nodeRect.x = fungusEditorWindow.scrollPos.x;
				s.nodeRect.y = fungusEditorWindow.scrollPos.y;
				Undo.RegisterCreatedObjectUndo(go, "Sequence");
				t.selectedSequence = s;
				return;
			}

			if (t.selectedSequence != null)
			{
				if (GUILayout.Button("Delete"))
				{
					Undo.DestroyObjectImmediate(t.selectedSequence.gameObject);
					t.selectedSequence = null;
				}
				if (GUILayout.Button("Duplicate"))
				{
					GameObject copy = GameObject.Instantiate(t.selectedSequence.gameObject) as GameObject;
					copy.transform.parent = t.transform;
					copy.name = t.selectedSequence.name;
					
					Sequence sequenceCopy = copy.GetComponent<Sequence>();
					sequenceCopy.nodeRect.x += sequenceCopy.nodeRect.width + 10;
					
					Undo.RegisterCreatedObjectUndo(copy, "Duplicate Sequence");
					t.selectedSequence = sequenceCopy;
				}
			}

			GUILayout.EndHorizontal();

			EditorGUILayout.Separator();

			if (t.selectedSequence != null)
			{
				DrawSequenceGUI(t.selectedSequence);
			}

			serializedObject.ApplyModifiedProperties();
		}

		public void DrawSequenceGUI(Sequence sequence)
		{
			EditorGUI.BeginChangeCheck();

			string sequenceName = EditorGUILayout.TextField(new GUIContent("Name", "Name of sequence displayed in editor window"), sequence.name);
			string sequenceDescription = EditorGUILayout.TextField(new GUIContent("Description", "Sequence description displayed in editor window"), sequence.description);

			EditorGUILayout.Separator();

			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(sequence, "Set Sequence");
				sequence.name = sequenceName;
				sequence.description = sequenceDescription;
			}

			EditorGUILayout.PrefixLabel("Commands");
			
			FungusCommand[] commands = sequence.GetComponents<FungusCommand>();
			foreach (FungusCommand command in commands)
			{
				if (GUILayout.Button(command.GetCommandTitle()))
				{
					command.expanded = !command.expanded;
				}
				
				if (command.expanded)
				{
					Editor commandEditor = Editor.CreateEditor(command);
					commandEditor.OnInspectorGUI();
				}
			}
		}

		public void DrawVariablesGUI()
		{
			serializedObject.Update();
			
			FungusScript t = target as FungusScript;
			
			ReorderableListGUI.Title("Variables");
			
			FungusVariableListAdaptor adaptor = new FungusVariableListAdaptor(variablesProperty, 0);
			ReorderableListControl.DrawControlFromState(adaptor, null, ReorderableListFlags.DisableContextMenu | ReorderableListFlags.HideAddButton);
			
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			
			if (!Application.isPlaying && GUILayout.Button("Add Variable"))
			{
				GenericMenu menu = new GenericMenu ();
				
				menu.AddItem(new GUIContent ("Boolean"), false, AddVariable<BooleanVariable>, t);
				menu.AddItem (new GUIContent ("Integer"), false, AddVariable<IntegerVariable>, t);
				menu.AddItem (new GUIContent ("Float"), false, AddVariable<FloatVariable>, t);
				menu.AddItem (new GUIContent ("String"), false, AddVariable<StringVariable>, t);
				
				menu.ShowAsContext ();
			}
			GUILayout.EndHorizontal();
			
			serializedObject.ApplyModifiedProperties();
		}
		
		void AddVariable<T>(object obj) where T : FungusVariable
		{
			FungusScript fungusScript = obj as FungusScript;
			if (fungusScript == null)
			{
				return;
			}
			
			Undo.RecordObject(fungusScript, "Add Variable");
			T variable = fungusScript.gameObject.AddComponent<T>();
			variable.key = MakeUniqueKey(fungusScript);
			fungusScript.variables.Add(variable);
		}

		string MakeUniqueKey(FungusScript fungusScript)
		{
			int index = 0;
			while (true)
			{
				string key = "Var" + index;
				
				bool found = false;
				foreach(FungusVariable variable in fungusScript.GetComponents<FungusVariable>())
				{
					if (variable.key == key)
					{
						found = true;
						index++;
					}
				}
				
				if (!found)
				{
					return key;
				}
			}
		}
	}
	
}