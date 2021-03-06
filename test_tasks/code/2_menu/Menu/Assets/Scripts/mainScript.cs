﻿using UnityEngine;
using System.Collections;

public class mainScript : MonoBehaviour {
	public enum RotationAxes { MouseXAndY = 0, MouseX = 1, MouseY = 2 }
	public RotationAxes axes = RotationAxes.MouseXAndY;
	public float sensitivityX = 0F;
	public float sensitivityY = 0F;
	
	public float minimumX = -80F;
	public float maximumX = 80F;
	
	public float minimumY = -10F;
	public float maximumY = 10F;
	
	float rotationY = 0F;

	// Use this for initialization
	IEnumerator Start () {
		// Load Google logo as texture to quad primitive
		string url = "https://www.google.ru/images/srpr/logo11w.png";
		WWW www = new WWW(url);
		yield return www;
		GameObject quad =  GameObject.Find ("quad");
		// attach texture to gameObject
		quad.renderer.material.mainTexture = www.texture;
		// set shader for transparent background (not black)
		quad.renderer.material.shader = Shader.Find("Transparent/Diffuse");
	}
	
	// Update is called once per frame
	void Update () {
		if (axes == RotationAxes.MouseXAndY)
		{
			float rotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * sensitivityX;
			
			rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
			rotationY = Mathf.Clamp (rotationY, minimumY, maximumY);
			
			transform.localEulerAngles = new Vector3(-rotationY, rotationX, 0);
		}
		else if (axes == RotationAxes.MouseX)
		{
			transform.Rotate(0, Input.GetAxis("Mouse X") * sensitivityX, 0);
		}
		else
		{
			rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
			rotationY = Mathf.Clamp (rotationY, minimumY, maximumY);
			
			transform.localEulerAngles = new Vector3(-rotationY, transform.localEulerAngles.y, 0);
		}
	}
}
