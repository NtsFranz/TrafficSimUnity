using System;
using System.Collections;
using System.Collections.Generic;
using MLAgents;
using UnityEngine;

public class CarAgent : Agent
{
	public Transform startPoint;
	public Car car;

	public override void AgentReset()
	{
		transform.position = startPoint.position;
		transform.rotation = startPoint.rotation;
		car.ResetCar();
	}

	public override void CollectObservations()
	{
		// rays
		car.CastRays();
		float s = car.maxRayDistance;
		AddVectorObs(Mathf.Clamp01(car.rayHits[0].distance/s));
		AddVectorObs(Mathf.Clamp01(car.rayHits[1].distance/s));
		AddVectorObs(Mathf.Clamp01(car.rayHits[2].distance/s));
		AddVectorObs(Mathf.Clamp01(car.rayHits[3].distance/s));
		AddVectorObs(Mathf.Clamp01(car.rayHits[4].distance/s));
		
		// velocity
		AddVectorObs(car.speed);
		
		// distance traveled
		//AddVectorObs(car.distTraveled);
	}

	public override void AgentAction(float[] vectorAction, string textAction)
	{
		// apply rewards
		// if the car moved forward
		if (car.path.Count < 2 || 
		    Vector3.Distance(car.path[car.path.Count - 2], car.path[car.path.Count - 1]) > 0)
		{
			AddReward(.1f);
		}
		
		// if the car is in the middle of the lane
		float d1 = car.rayHits[3].distance;
		float d2 = car.rayHits[4].distance;
		AddReward((Mathf.Min(d1, d2) / (d1 + d2))*.1f);
		
		// time penalty
		AddReward(-0.05f);

		// if the car hit something
		if (car.touching != null && car.touching.CompareTag("obstacle"))
		{
			AddReward(car.distTraveled);
			Done();
		}


		// do actions
		car.Turn(vectorAction[0]);
		car.Accelerate(vectorAction[1]);
	}
}