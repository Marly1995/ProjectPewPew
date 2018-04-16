﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyState
{
    Idle,
    Patrol,
    Sneak,
    Cover,
    TakingCover,
    CoverShooting,
    Shooting,
    Dead
}

public class EnemyBehaviours : MonoBehaviour
{
    public int personalIndex;

    AudioSource audioSource;
    public AudioClip laserSound;

    [SerializeField]
    EnemyState state;
    public EnemyState State
    {
        get { return state; }
        set { state = value; }
    }

    [HideInInspector]
    public EnemyStatemanager manager;
    FriendStateManager fmanager;
    
    AgentMove controller;

    bool gunShown;
    MeshRenderer gunRenderer;
    Transform barrell;
    public GameObject hitParticles;
    public GameObject hitTrail;

    public Transform target;

    bool hasCoverPoint = false;

    public Transform[] patrolPositions;
    int patrolIndex;

    public float fireRate;
    public float rotationSpeed;
    float shotTime;

    public SkinnedMeshRenderer robotRenderer;
    float deathtime = 2.0f;
    bool dead;

    private void Start()
    {
        manager = FindObjectOfType<EnemyStatemanager>();
        fmanager = FindObjectOfType<FriendStateManager>();
        gunRenderer = GetComponentInChildren<MeshRenderer>();
        barrell = gunRenderer.transform.GetChild(0);
        robotRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        controller = GetComponent<AgentMove>();
        ChooseNextPatrolPosition();
    }

    private void Update()
    {
        if (Vector3.Distance(manager.player.transform.position, transform.position) <= 20f)
        {
            if (target == null)
            {
                target = manager.SearchForTarget(transform.position);
                if (target != null)
                {
                    state = EnemyState.TakingCover;
                }
            }
            switch (state)
            {
                case EnemyState.Idle:
                    if (gunShown)
                        StartCoroutine(DissolveGunOut());
                    gunShown = false;
                    controller.Agent.isStopped = true;
                    controller.Animator.SetBool("Crouch", false);
                    controller.Animator.SetBool("Shoot", false);
                    break;
                case EnemyState.Cover:
                    if (gunShown)
                        StartCoroutine(DissolveGunOut());
                    gunShown = false;
                    controller.Agent.isStopped = true;
                    controller.Animator.SetBool("Crouch", true);
                    controller.Animator.SetBool("Shoot", false);
                    break;
                case EnemyState.TakingCover:
                    if (gunShown)
                        StartCoroutine(DissolveGunOut());
                    gunShown = false;
                    controller.Agent.isStopped = false;
                    controller.Animator.SetBool("Crouch", false);
                    controller.Animator.SetBool("Shoot", false);
                    if (!hasCoverPoint)
                    { TakeCover(); }
                    if (Vector3.Distance(controller.transform.position, controller.goal.position) <= 1f)
                    {
                        controller.Agent.isStopped = true;
                        controller.Animator.SetBool("Crouch", true);
                        hasCoverPoint = true;
                        state = EnemyState.CoverShooting;
                    }
                    break;
                case EnemyState.Patrol:
                    if (gunShown)
                        StartCoroutine(DissolveGunOut());
                    gunShown = false;
                    controller.Agent.isStopped = false;
                    controller.Animator.SetBool("Crouch", false);
                    controller.Animator.SetBool("Shoot", false);
                    if (controller.goal == null) { state = EnemyState.Idle; }
                    if (Vector3.Distance(controller.transform.position, controller.goal.position) <= 1f)
                    {
                        ChooseNextPatrolPosition();
                    }
                    break;
                case EnemyState.Sneak:
                    if (gunShown)
                        StartCoroutine(DissolveGunOut());
                    gunShown = false;
                    controller.Agent.isStopped = false;
                    controller.Animator.SetBool("Crouch", true);
                    controller.Animator.SetBool("Shoot", false);
                    if (Vector3.Distance(controller.transform.position, controller.goal.position) <= 1f)
                    {
                        ChooseNextPatrolPosition();
                    }
                    break;
                case EnemyState.Shooting:
                    controller.Agent.isStopped = true;
                    controller.Animator.SetBool("Crouch", false);
                    controller.Animator.SetBool("Shoot", true);
                    ShootTarget();
                    break;
                case EnemyState.CoverShooting:
                    controller.Agent.isStopped = true;
                    controller.Animator.SetBool("Crouch", true);
                    controller.Animator.SetBool("Shoot", true);
                    ShootTarget();
                    break;
                case EnemyState.Dead:
                    controller.Agent.isStopped = true;
                    controller.Animator.SetBool("Dead", true);
                    Die();
                    break;
            }
        }
    }

    void ChooseNextPatrolPosition()
    {
        if (patrolPositions.Length <= 0) { state = EnemyState.Idle; return; }
        if (patrolIndex < patrolPositions.Length)
        {
            controller.goal = patrolPositions[patrolIndex++];
        }
        else
        {
            patrolIndex = 0;
            controller.goal = patrolPositions[patrolIndex];
        }
    }

    void TakeCover()
    {
        Transform coverpoint = manager.SearchForCover(transform.position);
        if (coverpoint != null)
        {
            controller.goal = coverpoint;
            hasCoverPoint = true;
        }
        else
        {
            state = EnemyState.Shooting;
        }
    }

    void ShootTarget()
    {
        if (target == null)
        target = manager.SearchForTarget(transform.position);

        if (target != null)
        {
            shotTime -= Time.deltaTime;
            if (!gunShown)
            {
                StartCoroutine(DissolveGunIn());
            }
            gunShown = true;

            Vector3 direction = (target.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));    // flattens the vector3
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);

            if (shotTime <= 0f)
            {
                Vector3 shotDir = (target.position + new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f))) - barrell.position;
                Ray ray = new Ray(barrell.position, shotDir);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    GameObject particles = Instantiate(hitParticles, hit.point, Quaternion.identity);
                    Destroy(particles, 0.4f);
                    if (hit.collider.tag == "Friendly")
                    {
                        hit.collider.gameObject.GetComponent<FriendlyBehaviour>().State = FriendState.Downed;
                    }
                }
                GameObject trail = Instantiate(hitTrail);
                VolumetricLines.VolumetricLineBehavior vol = trail.GetComponent<VolumetricLines.VolumetricLineBehavior>();
                vol.StartPos = barrell.position;
                vol.EndPos = hit.point;
                shotTime = fireRate;
                audioSource.clip = laserSound;
                audioSource.Play();
            }
        }
    }

    void Die()
    {
        deathtime -= Time.deltaTime;
        if (!dead)
        {
            if (deathtime <= 0f)
            { StartCoroutine(DissolvePlayerOut()); dead = true; fmanager.targets.RemoveAt(personalIndex); }
        }
    }
    
    IEnumerator DissolveGunOut()
    {
        for (float i = 0f; i <= 1f; i += 0.02f)
        {
            gunRenderer.material.SetFloat("_SliceAmount", i);
            yield return new WaitForSeconds(0.002f);
        }
    }

    IEnumerator DissolveGunIn()
    {
        for (float i = 1f; i >= -0.1f; i -= 0.02f)
        {
            gunRenderer.material.SetFloat("_SliceAmount", i);
            yield return new WaitForSeconds(0.002f);
        }
    }

    IEnumerator DissolvePlayerOut()
    {
        for (float i = 0f; i <= 1f; i += 0.02f)
        {
            robotRenderer.material.SetFloat("_SliceAmount", i);
            yield return new WaitForSeconds(0.002f);
        }
        Destroy(gameObject);
    }
}