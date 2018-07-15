using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Car : MonoBehaviour
{
	private Vector3 carDim;

	[Header("Debug")] public bool visualizeRays;
	public float lineWidth = .01f;

	[Header("Parameters")] public float sideRayAngle = 10f;

	[Tooltip("Height in m above the ground for the rays")]
	public float rayHeight = .2f;

	public float horizontalForwardRayDistance = .5f;
	public float acceleration = 1f;
	public float decceleration = 1f;
	public float maxSpeed = 10f;
	public float turningSpeed;
	public float goalSpeed = 0f;

	[Header("Components")] public MeshRenderer carBodyRenderer;
	private Material carBodyMaterial;
	public WheelCollider[] wheels;

	//[Header("Navigtion Parameters")] 
	public float freeDistance;
	public float cautionDistance;
	public float stopDistance;

	private Rigidbody rb;
	private GameObject lineRendererParent;
	private LineRenderer forwardLine;
	private LineRenderer forwardLeftLine;
	private LineRenderer forwardRightLine;

	// Use this for initialization
	void Start()
	{
		UpdateCarDimensions();
		rb = GetComponent<Rigidbody>();
		carBodyMaterial = carBodyRenderer.material;
	}

	// Update is called once per frame
	void Update()
	{
		// check surroundings with raycasts
		Ray forwardRay =
			new Ray(
				transform.position + ((transform.up * rayHeight) + (transform.forward * (carDim.y / 2))) * transform.localScale.x,
				transform.forward);
		Ray forwardLeftRay = new Ray(
			transform.position +
			((transform.up * rayHeight) + (-transform.right * horizontalForwardRayDistance) +
			 (transform.forward * (carDim.y / 2))) * transform.localScale.x,
			Quaternion.AngleAxis(-sideRayAngle, transform.up) * transform.forward);
		Ray forwardRightRay = new Ray(
			transform.position +
			((transform.up * rayHeight) + (transform.right * horizontalForwardRayDistance) +
			 (transform.forward * (carDim.y / 2))) * transform.localScale.x,
			Quaternion.AngleAxis(sideRayAngle, transform.up) * transform.forward);

		RaycastHit forwardHit;
		RaycastHit forwardLeftHit;
		RaycastHit forwardRightHit;
		Physics.Raycast(forwardRay, out forwardHit);
		Physics.Raycast(forwardLeftRay, out forwardLeftHit);
		Physics.Raycast(forwardRightRay, out forwardRightHit);


		#region DRIVE

		// set goal speeds
		goalSpeed = map(0, freeDistance, 0, maxSpeed, forwardHit.distance);
		if (forwardLeftHit.distance < cautionDistance || forwardRightHit.distance < cautionDistance)
		{
			goalSpeed = 0;
		}


		// accelerate
		if (rb.velocity.magnitude < goalSpeed)
		{
			rb.AddForce(acceleration * rb.mass * transform.forward * Time.deltaTime);
			carBodyMaterial.color = Color.green;
		}
		// deccelerate
		else if (rb.velocity.magnitude > goalSpeed)
		{
			rb.AddForce(decceleration * rb.mass * -transform.forward * Time.deltaTime);
			carBodyMaterial.color = Color.red;
		}


		// turn
		if (forwardLeftHit.distance > forwardRightHit.distance)
		{
			rb.AddTorque(-turningSpeed * transform.up * Time.deltaTime);
		}
		else
		{
			rb.AddTorque(turningSpeed * transform.up * Time.deltaTime);
		}

		// remove sideways motion
		rb.velocity -= Vector3.Project(rb.velocity, transform.right);

		#endregion DRIVE


		// draw rays
		if (visualizeRays)
		{
			if (lineRendererParent == null)
			{
				lineRendererParent = new GameObject("LineRenderers");
				lineRendererParent.transform.SetParent(transform);
			}

			if (forwardLine == null)
			{
				GameObject g = new GameObject();
				g.transform.SetParent(lineRendererParent.transform);
				forwardLine = g.AddComponent<LineRenderer>();
				forwardLine.material.color = Color.white;
				forwardLine.material.shader = Shader.Find("Unlit/Color");
				forwardLine.positionCount = 2;
				forwardLine.widthMultiplier = lineWidth;
			}

			if (forwardLeftLine == null)
			{
				GameObject g = new GameObject();
				g.transform.SetParent(lineRendererParent.transform);
				forwardLeftLine = g.AddComponent<LineRenderer>();
				forwardLeftLine.material.color = Color.red;
				forwardLeftLine.material.shader = Shader.Find("Unlit/Color");
				forwardLeftLine.positionCount = 2;
				forwardLeftLine.widthMultiplier = lineWidth;
			}

			if (forwardRightLine == null)
			{
				GameObject g = new GameObject();
				g.transform.SetParent(lineRendererParent.transform);
				forwardRightLine = g.AddComponent<LineRenderer>();
				forwardRightLine.material.color = Color.black;
				forwardRightLine.material.shader = Shader.Find("Unlit/Color");
				forwardRightLine.positionCount = 2;
				forwardRightLine.widthMultiplier = lineWidth;
			}

			forwardLine.SetPositions(new[] {forwardRay.origin, forwardHit.point});
			forwardLeftLine.SetPositions(new[] {forwardLeftRay.origin, forwardLeftHit.point});
			forwardRightLine.SetPositions(new[] {forwardRightRay.origin, forwardRightHit.point});
		}
	}

	void UpdateCarDimensions()
	{
		BoxCollider collider = GetComponentInChildren<BoxCollider>();
		carDim = collider.size;
	}

	// maps and clamps
	float map(float in_low, float in_high, float out_low, float out_high, float input)
	{
		input -= in_low;
		input /= (in_high - in_low);
		input *= (out_high - out_low);
		input += out_low;
		input = Mathf.Clamp(input, out_low, out_high);
		return input;
	}

}