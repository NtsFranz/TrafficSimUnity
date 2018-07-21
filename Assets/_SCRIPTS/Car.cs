using System.Collections;
using System.Collections.Generic;
using UnityEditor;
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
	public float goalSpeed;

	[Header("Components")] public MeshRenderer carBodyRenderer;
	private Material carBodyMaterial;
	public WheelCollider[] wheels;

	//[Header("Navigtion Parameters")] 
	public float freeDistance;
	public float cautionDistance;
	public float stopDistance;

	private Rigidbody rb;
	private GameObject lineRendererParent;
	private LineRenderer[] lines = new LineRenderer[5];

	[HideInInspector] public GameObject touching;
	[HideInInspector] public float maxRayDistance = 5f;
	[HideInInspector] public RaycastHit[] rayHits = new RaycastHit[5];

	[HideInInspector] public List<Vector3> path = new List<Vector3>();
	[HideInInspector] public float distTraveled;
	private int numRays = 5;
	private Ray[] rays = new Ray[5];

	/// <summary>
	/// The forward speed of the car
	/// </summary>
	[HideInInspector] public float speed;

	public bool manualControl;

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
		speed = rb.velocity.magnitude;

		if (manualControl)
		{
			if (Input.GetKey(KeyCode.W))
			{
				Accelerate(1);
			}

			if (Input.GetKey(KeyCode.S))
			{
				Accelerate(-1);
			}

			if (Input.GetKey(KeyCode.A))
			{
				Turn(-1);
			}

			if (Input.GetKey(KeyCode.D))
			{
				Turn(1);
			}
		}
		
		//CastRays();

		// set goal speeds
		/*goalSpeed = map(0, freeDistance, 0, maxSpeed, rayHits[0].distance);
		if (rayHits[1].distance < cautionDistance || rayHits[2].distance < cautionDistance)
		{
			goalSpeed = 0;
		}*/


		// accelerate
		/*if (rb.velocity.magnitude < goalSpeed)
		{
			Accelerate(1);
		}
		// deccelerate
		else if (rb.velocity.magnitude > goalSpeed)
		{
			Accelerate(-1);
		}


		// turn
		if (rayHits[1].distance > rayHits[2].distance)
		{
			Turn(-1);
		}
		else
		{
			Turn(1);
		}*/

		// remove sideways motion
		rb.velocity -= Vector3.Project(rb.velocity, transform.right);


		path.Add(transform.position);
		if (path.Count > 1)
			distTraveled += Vector3.Distance(path[path.Count - 2], path[path.Count - 1]);


		// draw rays
		if (visualizeRays)
		{
			DrawRays();
		}
	}

	void DrawRays()
	{
		if (lineRendererParent == null)
		{
			lineRendererParent = new GameObject("LineRenderers");
			lineRendererParent.transform.SetParent(transform);
			lineRendererParent.transform.localEulerAngles = Vector3.zero;
			lineRendererParent.transform.localPosition = Vector3.zero;
		}

		Color[] colors = {Color.white, Color.red, Color.black, Color.green, Color.yellow};

		for (int i = 0; i < numRays; i++)
		{
			if (lines[i] == null)
			{
				GameObject g = new GameObject();
				g.transform.SetParent(lineRendererParent.transform);
				lines[i] = g.AddComponent<LineRenderer>();
				lines[i].material.color = colors[i];
				lines[i].material.shader = Shader.Find("Unlit/Color");
				lines[i].positionCount = 2;
				lines[i].widthMultiplier = lineWidth;
			}

			lines[i].SetPositions(new[] {rays[i].origin, rayHits[i].point});
		}
	}

	void UpdateCarDimensions()
	{
		BoxCollider c = GetComponentInChildren<BoxCollider>();
		carDim = c.size;
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

	private void OnCollisionEnter(Collision other)
	{
		touching = other.gameObject;
		Debug.Log(other.gameObject.name);
	}

	private void OnCollisionExit(Collision other)
	{
		if (touching == other.gameObject)
		{
			touching = null;
			Debug.Log("Exit: " + other.gameObject.name);
		}

		
	}


	/// <summary>
	/// Rotate the car according to val
	/// </summary>
	/// <param name="amount">Value between -1 and 1. Negative is left and positive is right.</param>
	public void Turn(float amount)
	{
		rb.AddTorque(amount * turningSpeed * transform.up * Time.deltaTime);
	}

	/// <summary>
	/// Speed up or slow down according to input val
	/// </summary>
	/// <param name="amount">The amount to slow down or speed up. Between -1 and 1.</param>
	public void Accelerate(float amount)
	{
		if (amount > 0)
		{
			rb.AddForce(amount * acceleration * rb.mass * transform.forward * Time.deltaTime);
		}
		else
		{
			rb.AddForce(amount * decceleration * rb.mass * transform.forward * Time.deltaTime);
		}

		carBodyMaterial.color = Color.Lerp(Color.red, Color.green, (amount + 1) / 2);
	}

	public void ResetCar()
	{
		distTraveled = 0;
		path = new List<Vector3>();
		path.Add(transform.position);
		rb.velocity = Vector3.zero;
		rb.angularVelocity = Vector3.zero;
		carBodyMaterial.color = Color.white;
	}

	public void CastRays()
	{
		// check surroundings with raycasts
		// future optimization: only define rays once as coming from origin, then offset every frame
		Ray forwardRay =
			new Ray(
				transform.position + ((transform.up * rayHeight) + (transform.forward * (carDim.y / 2))) * transform.localScale.x,
				transform.forward);
		rays[0] = (forwardRay);
		Ray forwardLeftRay = new Ray(
			transform.position +
			((transform.up * rayHeight) + (-transform.right * horizontalForwardRayDistance) +
			 (transform.forward * (carDim.y / 2))) * transform.localScale.x,
			Quaternion.AngleAxis(-sideRayAngle, transform.up) * transform.forward);
		rays[1] = (forwardLeftRay);
		Ray forwardRightRay = new Ray(
			transform.position +
			((transform.up * rayHeight) + (transform.right * horizontalForwardRayDistance) +
			 (transform.forward * (carDim.y / 2))) * transform.localScale.x,
			Quaternion.AngleAxis(sideRayAngle, transform.up) * transform.forward);
		rays[2] = (forwardRightRay);
		Ray leftRay =
			new Ray(
				transform.position + ((transform.up * rayHeight) + (-transform.right * (carDim.x / 2))) * transform.localScale.x,
				-transform.right);
		rays[3] = (leftRay);
		Ray rightRay =
			new Ray(
				transform.position + ((transform.up * rayHeight) + (transform.right * (carDim.x / 2))) * transform.localScale.x,
				transform.right);
		rays[4] = (rightRay);

		rayHits = new RaycastHit[numRays];


		for (int i = 0; i < numRays; i++)
		{
			Physics.Raycast(rays[i], out rayHits[i]);
		}
	}
}