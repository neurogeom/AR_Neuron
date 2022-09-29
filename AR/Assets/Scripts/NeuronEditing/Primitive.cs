using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Primitive
{
	//public static GameObject CreateCylinder(Marker marker, Transform parentTransform, float scale = 1 / 512.0f)
	//{
	//	GameObject newObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
	//	Vector3 a = marker.position * scale - new Vector3(0.5f, 0.5f, 0.5f); ;
	//	Vector3 b = marker.parent.position * scale - new Vector3(0.5f, 0.5f, 0.5f);
	//	float length = Vector3.Distance(a, b);
	//	Vector3 ab = (a - b).normalized;
	//	Vector3 y_axis = new Vector3(0, 1, 0);
	//	newObj.transform.parent = parentTransform;
	//	newObj.transform.localScale = new Vector3((float)marker.radius * scale, length / 2, (float)marker.radius * scale);
	//	newObj.transform.Rotate(Vector3.Cross(ab, y_axis), -Mathf.Acos(Vector3.Dot(ab, y_axis)) * 180 / Mathf.PI);
	//	newObj.transform.localPosition = (a + b) / 2;
	//	newObj.GetComponent<MeshRenderer>().material.color = Color.red;
	//	return newObj;
	//}

	public static GameObject CreateCylinder(Vector3 a, Vector3 b, Transform parentTransform, float radius)
	{
		GameObject newObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
		float length = Vector3.Distance(a, b);
		Vector3 ab = (a - b).normalized;
		Vector3 y_axis = new Vector3(0, 1, 0);
		newObj.transform.parent = parentTransform;
		newObj.transform.localScale = new Vector3(radius, length / 2, radius);
		newObj.transform.Rotate(Vector3.Cross(ab, y_axis), -Mathf.Acos(Vector3.Dot(ab, y_axis)) * 180 / Mathf.PI);
		newObj.transform.position = (a + b) / 2;

		newObj.GetComponent<MeshRenderer>().material.color = Color.red;
		var chosenScript = newObj.AddComponent<Chosen>();
		return newObj;
	}

	public static GameObject CreateCylinder(SwcNode node, Transform parentTransform)
	{
		
		GameObject newObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
		Vector3 a = parentTransform.TransformPoint(node.position);
		Vector3 b = parentTransform.TransformPoint(node.parent.position);
		float length = Vector3.Distance(a, b);
		Vector3 ab = (a - b).normalized;

		newObj.transform.localScale = new Vector3(node.radius, length / 2, (float)node.radius);
		newObj.transform.up = parentTransform.TransformDirection(ab);
		//newObj.transform.Rotate(Vector3.Cross(ab, y_axis), -Mathf.Acos(Vector3.Dot(ab, y_axis)) * 180 / Mathf.PI);
		newObj.transform.position = (a + b) / 2;
		Transform temp = GameObject.Find("Temp").transform;
		newObj.transform.SetParent(temp, true);
		newObj.GetComponent<MeshRenderer>().material.color = Color.red;
		return newObj;
	}

	//public static GameObject CreateSphere(Marker marker, Transform parentTransform, float scale = 1 / 512.0f)
	//{
	//	GameObject newObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
	//	newObj.GetComponent<MeshRenderer>().material.color = Color.red;
	//	newObj.transform.parent = parentTransform;
	//	newObj.transform.localPosition = marker.position * scale - new Vector3(0.5f, 0.5f, 0.5f);
	//	newObj.transform.localScale = new Vector3(1, 1, 1) * (float)marker.radius * scale;
	//	return newObj;
	//}

	public static GameObject CreateSphere(SwcNode node, Transform parentTransform)
	{
		GameObject newObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		newObj.GetComponent<MeshRenderer>().material.color = Color.red;
		newObj.transform.localScale = new Vector3(1, 1, 1) * node.radius;

		//newObj.transform.parent = parentTransform;
		//newObj.transform.localPosition = node.position;
		Transform temp = GameObject.Find("Temp").transform;
		newObj.transform.position = parentTransform.TransformPoint(node.position);
		newObj.transform.SetParent(temp, true);


		return newObj;
	}


	public static void CreateTree(List<Marker> tree, Transform parentTransform, int sz0, int sz1, int sz2 )
	{
		Vector3 scale = new Vector3(1/(float)sz0, 1/(float)sz1, 1/(float)sz2);
		//float scale = 1 / 512.0f;
		Dictionary<Marker, SwcNode> map = new Dictionary<Marker, SwcNode>();
		foreach(var marker in tree)
        {
			SwcNode node;
			if (marker.parent == null)
            {
				node = new SwcSoma(marker,scale);
				Debug.Log("create soma");
            }
            else
            {
				node = new SwcNode(marker, scale);
			}
			node.isBranch_root = marker.isBranch_root;
			map.Add(marker, node);
		}
		foreach (var marker in tree)
		{
			SwcNode node = map[marker];

			GameObject sphere = Primitive.CreateSphere(node, parentTransform);
			node.sphere = sphere;

			if (marker.parent != null)
            {
				var parent = map[marker.parent];
				parent.AddChild(node);
				GameObject cylinder =  Primitive.CreateCylinder(node, parentTransform);
				Chosen c = cylinder.AddComponent<Chosen>();
				App2.MarkerMap[marker] = c;
				c.nodeA = parent;
				c.nodeB = node;
				node.cylinder = cylinder;
			}
            else
            {
				sphere.name = "Soma";
				Chosen soma = sphere.AddComponent<Chosen>();
				App2.MarkerMap[marker] = soma;
				soma.nodeA = node;
				soma.nodeB = node;
            }
		}
	}

	public static void CreateBranch(List<Marker> branch, Marker brachParentMarker, Transform parentTransform, int sz0, int sz1, int sz2)
	{
		Vector3 scale = new Vector3(1 / (float)sz0, 1 / (float)sz1, 1 / (float)sz2);
		//float scale = 1 / 512.0f;
		Dictionary<Marker, SwcNode> map = new Dictionary<Marker, SwcNode>();
		foreach (var marker in branch)
		{
			SwcNode node = new SwcNode(marker, scale);
			node.isBranch_root = marker.isBranch_root;
			map.Add(marker, node);
		}
		foreach (var marker in branch)
		{
			SwcNode node = map[marker];

			GameObject sphere = Primitive.CreateSphere(node, parentTransform);
			node.sphere = sphere;

			SwcNode parent;
			if (marker.parent != null)
			{
				parent = map[marker.parent];
			}
            else
            {
				parent = App2.MarkerMap[brachParentMarker].nodeB;
			}
				parent.AddChild(node);
				GameObject cylinder = Primitive.CreateCylinder(node, parentTransform);
				Chosen c = cylinder.AddComponent<Chosen>();
				App2.MarkerMap[marker] = c;
				c.nodeA = parent;
				c.nodeB = node;
				node.cylinder = cylinder;
		}
	}

	public static void RepairTree(List<Marker> tree, SwcNode repairSeed, Transform parentTransform, int sz0, int sz1, int sz2)
	{
		Vector3 scale = new Vector3(1 / (float)sz0, 1 / (float)sz1, 1 / (float)sz2);
		Dictionary<Marker, SwcNode> map = new Dictionary<Marker, SwcNode>();
		foreach (var marker in tree)
		{
			SwcNode node = new SwcNode(marker, scale);
			node.isBranch_root = marker.isBranch_root;
            if (marker.parent == null)
				map.Add(marker, repairSeed);
            else
				map.Add(marker, node);
		}
		foreach (var marker in tree)
		{
			SwcNode node = map[marker];

			GameObject sphere = Primitive.CreateSphere(node, parentTransform);
			node.sphere = sphere;

			if (marker.parent != null)
			{
				SwcNode parent = map[marker.parent];
				parent.AddChild(node);
				GameObject cylinder = Primitive.CreateCylinder(node, parentTransform);
				Chosen c = cylinder.AddComponent<Chosen>();
				c.nodeA = parent;
				c.nodeB = node;
				node.cylinder = cylinder;
			}
		}
		GameObject HandTrackingManager = GameObject.Find("HandTrackingManager");
		HandTrackingManager.GetComponent<IndexTipTrackScript>().enabled = true;
	}
}
