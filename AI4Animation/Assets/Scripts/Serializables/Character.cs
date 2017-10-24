﻿using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class Character {

	public bool Inspect = false;

	public float Phase = 0f;

	public Joint[] Joints = new Joint[0];

	private float JointRadius = 0.05f;
	private float BoneStartWidth = 0.025f;
	private float BoneEndWidth = 0.01f;

	public Character() {

	}

	//public void AddJoint() {
	public Joint AddJoint() {
		System.Array.Resize(ref Joints, Joints.Length+1);
		Joints[Joints.Length-1] = new Joint();
		return Joints[Joints.Length-1];
		/*
		for(int i=Joints.Length-2; i>=index; i--) {
			Joints[i+1] = Joints[i];
		}
		Joints[index] = new Joint();
		*/
	}

	//public void RemoveJoint(int index) {
	public void RemoveJoint() {
		/*
		if(Joints.Length < index || Joints.Length == 0) {
			return;
		}
		for(int i=index; i<Joints.Length-1; i++) {
			Joints[i] = Joints[i+1];
		}
		*/
		System.Array.Resize(ref Joints, Joints.Length-1);
	}

	public Joint FindJoint(Transform t) {
		return System.Array.Find(Joints, x => x.Transform == t);
	}

	public void Draw() {
		for(int i=0; i<Joints.Length; i++) {
			//Joints[i].RemoveVisual();
			//Joints[i].CreateVisual();
			if(Joints[i].Transform != null) {
				Joints[i].Visual.transform.localScale = JointRadius * Vector3.one;
				Joints[i].Visual.startWidth = BoneStartWidth;
				Joints[i].Visual.endWidth = BoneEndWidth;

				if(!Application.isPlaying) {
					if(Joints[i].Parent != null) {
						UnityGL.DrawLine(Joints[i].Parent.position, Joints[i].Transform.position, BoneStartWidth, BoneEndWidth, Color.cyan);
					}
				}

				UnityGL.DrawArrow(
					Joints[i].Transform.position,
					Joints[i].Transform.position + 10f*Joints[i].GetVelocity(),
					0.75f,
					0.0075f,
					0.05f,
					new Color(0f, 1f, 0f, 0.5f)
				);
			}
		}
	}

	#if UNITY_EDITOR
	public void Inspector() {
		using(new EditorGUILayout.VerticalScope ("Box")) {
			if(GUILayout.Button("Character")) {
				Inspect = !Inspect;
			}

			if(Inspect) {
				using(new EditorGUILayout.VerticalScope ("Box")) {
					//Target.Character.JointSmoothing = EditorGUILayout.Slider("Joint Smoothing", Target.Character.JointSmoothing, 0f, 1f);

					EditorGUILayout.LabelField("Joints");

					for(int i=0; i<Joints.Length; i++) {
						using(new EditorGUILayout.VerticalScope ("Box")) {
							EditorGUILayout.BeginHorizontal();
							EditorGUILayout.LabelField((i+1).ToString(), GUILayout.Width(20));
							Joints[i].SetTransform((Transform)EditorGUILayout.ObjectField(Joints[i].Transform, typeof(Transform), true), this);
							EditorGUILayout.EndHorizontal();
						}
					}
					
					if(GUILayout.Button("+")) {
						//Target.Character.AddJoint(Target.Character.Joints.Length);
						AddJoint();
					}
					if(GUILayout.Button("-")) {
						//Target.Character.RemoveJoint(Target.Character.Joints.Length);
						RemoveJoint();
					}
				}
			}
		}
	}
	#endif

	[System.Serializable]
	public class Joint {
		public Transform Transform;
		public Transform Parent;
		public Transform[] Childs;

		public LineRenderer Visual;

		private Vector3 Velocity;

		public Joint() {

		}

		public void CreateVisual() {
			if(Visual != null) {
				return;
			}
			Visual = GameObject.CreatePrimitive(PrimitiveType.Sphere).AddComponent<LineRenderer>();
			Visual.name = "Visual";
			Visual.transform.SetParent(Transform);
			Visual.transform.localPosition = Vector3.zero;
			Visual.transform.localRotation = Quaternion.identity;
			Visual.transform.localScale = Vector3.zero;
			Visual.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
			Visual.GetComponent<MeshRenderer>().material = Resources.Load("Materials/Joint", typeof(Material)) as Material;

			Visual.positionCount = 2;
			Visual.startWidth = 0f;
			Visual.endWidth = 0f;
			Visual.SetPosition(0, Visual.transform.position);
			Visual.SetPosition(1, Visual.transform.position);
			Visual.material = Resources.Load("Materials/Line", typeof(Material)) as Material;
			
			Utility.Destroy(Visual.GetComponent<Collider>());
		}

		public void RemoveVisual() {
			if(Visual == null) {
				return;
			}
			Utility.Destroy(Visual.gameObject);
		}

		public void SetTransform(Transform t, Character character) {
			if(t == Transform) {
				return;
			}
			if(t == null) {
				//Unset
				Transform = null;
				if(Parent != null) {
					character.FindJoint(Parent).UpdateChilds(character);
				}
				for(int i=0; i<Childs.Length; i++) {
					character.FindJoint(Childs[i]).UpdateParent(character);
				}
				Parent = null;
				Childs = new Transform[0];
				
				RemoveVisual();
			} else {
				//Set
				Transform = t;
				UpdateParent(character);
				UpdateChilds(character);
				if(Parent != null) {
					character.FindJoint(Parent).UpdateChilds(character);
				}
				for(int i=0; i<Childs.Length; i++) {
					character.FindJoint(Childs[i]).UpdateParent(character);
				}

				CreateVisual();
			}
		}

		private void UpdateParent(Character character) {
			Parent = null;
			if(Transform != Transform.root) {
				FindParent(Transform.parent, character);
			}
		}

		private void FindParent(Transform t, Character character) {
			Joint parentJoint = character.FindJoint(t);
			if(parentJoint != null) {
				Parent = t;
				return;
			}
			if(t != t.root) {
				FindParent(t.parent, character);
			}
		}

		private void UpdateChilds(Character character) {
			Childs = new Transform[0];
			FindChilds(Transform, character);
		}

		private void FindChilds(Transform t, Character character) {
			for(int i=0; i<t.childCount; i++) {
				Joint childJoint = character.FindJoint(t.GetChild(i));
				if(childJoint != null) {
					System.Array.Resize(ref Childs, Childs.Length+1);
					Childs[Childs.Length-1] = t.GetChild(i);
				} else {
					FindChilds(t.GetChild(i), character);
				}
			}
		}

		public void SetPosition(Vector3 position) {
			Transform[] childs = new Transform[Transform.childCount];
			for(int i=0; i<childs.Length; i++) {
				childs[i] = Transform.GetChild(i);
			}
			Transform.DetachChildren();
			Transform.position = position;
			for(int i=0; i<childs.Length; i++) {
				childs[i].SetParent(Transform);
			}

			Visual.transform.position = position;
			Visual.SetPosition(1, position);
			if(Parent == null) {
				Visual.SetPosition(0, position);
			}
			for(int i=0; i<Childs.Length; i++) {
				Childs[i].Find("Visual").GetComponent<LineRenderer>().SetPosition(0, position);
			}
		}

		public void SetPosition(Vector3 position, Transformation relativeTo) {
			SetPosition(relativeTo.Position + relativeTo.Rotation * position);
		}

		public Vector3 GetPosition() {
			return Transform.position;
		}

		public Vector3 GetPosition(Transformation relativeTo) {
			return Quaternion.Inverse(relativeTo.Rotation) * (GetPosition() - relativeTo.Position);
		}

		public void SetVelocity(Vector3 velocity) {
			Velocity = velocity;
		}

		public void SetVelocity(Vector3 velocity, Transformation relativeTo) {
			SetVelocity(relativeTo.Rotation * velocity);
		}

		public Vector3 GetVelocity() {
			return Velocity;
		}

		public Vector3 GetVelocity(Transformation relativeTo) {
			return Quaternion.Inverse(relativeTo.Rotation) * GetVelocity();
		}
	}

}