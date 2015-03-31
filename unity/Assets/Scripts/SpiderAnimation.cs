﻿using UnityEngine;

public class SpiderAnimation : MonoBehaviour
{

    MovementMotor motor;
    AnimationClip activateAnim, forwardAnim, backAnim, leftAnim, rightAnim, audioSource;
    SignalSender footstepSignals;
    bool skiddingSounds, footstepSounds;

    private Transform tr;
    private float lastFootstepTime = 0;
    private float lastAnimTime = 0;
    void OnEnable()
    {
        tr = motor.transform;

        animation[activateAnim.name].enabled = true;
        animation[activateAnim.name].weight = 1;
        animation[activateAnim.name].time = 0;
        animation[activateAnim.name].speed = 1;

        animation[forwardAnim.name].layer = 1;
        animation[forwardAnim.name].enabled = true;
        animation[forwardAnim.name].weight = 0;
        animation[backAnim.name].layer = 1;
        animation[backAnim.name].enabled = true;
        animation[backAnim.name].weight = 0;
        animation[leftAnim.name].layer = 1;
        animation[leftAnim.name].enabled = true;
        animation[leftAnim.name].weight = 0;
        animation[rightAnim.name].layer = 1;
        animation[rightAnim.name].enabled = true;
        animation[rightAnim.name].weight = 0;

    }

    void OnDisable()
    {
        animation[activateAnim.name].enabled = true;
        animation[activateAnim.name].weight = 1;
        animation[activateAnim.name].normalizedTime = 1;
        animation[activateAnim.name].speed = -1;
        animation.CrossFade(activateAnim.name, 0.3f, PlayMode.StopAll);
    }

    void Update()
    {
        Vector3 direction = motor.movementDirection;
        direction.y = 0;

        float walkWeight = direction.magnitude;

        animation[forwardAnim.name].speed = walkWeight;
        animation[rightAnim.name].speed = walkWeight;
        animation[backAnim.name].speed = walkWeight;
        animation[leftAnim.name].speed = walkWeight;

        float angle = Mathf.DeltaAngle(
            HorizontalAngle(tr.forward),
            HorizontalAngle(direction)
        );

        if (walkWeight > 0.01)
        {
            float w;
            if (angle < -90)
            {
                w = Mathf.InverseLerp(-180, -90, angle);
                animation[forwardAnim.name].weight = 0;
                animation[rightAnim.name].weight = 0;
                animation[backAnim.name].weight = 1 - w;
                animation[leftAnim.name].weight = 1;
            }
            else if (angle < 0)
            {
                w = Mathf.InverseLerp(-90, 0, angle);
                animation[forwardAnim.name].weight = w;
                animation[rightAnim.name].weight = 0;
                animation[backAnim.name].weight = 0;
                animation[leftAnim.name].weight = 1 - w;
            }
            else if (angle < 90)
            {
                w = Mathf.InverseLerp(0, 90, angle);
                animation[forwardAnim.name].weight = 1 - w;
                animation[rightAnim.name].weight = w;
                animation[backAnim.name].weight = 0;
                animation[leftAnim.name].weight = 0;
            }
            else
            {
                w = Mathf.InverseLerp(90, 180, angle);
                animation[forwardAnim.name].weight = 0;
                animation[rightAnim.name].weight = 1 - w;
                animation[backAnim.name].weight = w;
                animation[leftAnim.name].weight = 0;
            }
        }

        if (skiddingSounds)
        {
            if (walkWeight > 0.2 && !animation.isPlaying)
                animation.Play();
            else if (walkWeight < 0.2 && animation.isPlaying)
                animation.Stop();
        }

        if (footstepSounds && walkWeight > 0.2)
        {
            var newAnimTime = Mathf.Repeat((float)(animation[forwardAnim.name].normalizedTime * 4 + 0.1), 1);
            if (newAnimTime < lastAnimTime)
            {
                if (Time.time > lastFootstepTime + 0.1)
                {
                    footstepSignals.SendSignals(this);
                    lastFootstepTime = Time.time;
                }
            }
            lastAnimTime = newAnimTime;
        }
    }

    static float HorizontalAngle(Vector3 direction)
    {
        return Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
    }
}
