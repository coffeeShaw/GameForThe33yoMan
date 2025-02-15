﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharMovement : CharStats 
{
    Rigidbody2D rb2d;
    SpriteRenderer spriteRenderer;
    AudioSource audiosource;
    AudioSource groundContact;
    AudioSource AbilityTrigger;
    public AudioClip grassWalkSound;
    public float walkSoundCoolDown;
    private float soundTimeLock = 0;
    public float terminalVelocity;
    public LayerMask groundLayers;
    public Vector2 colSize;
    public Vector3 offset;
    Transform feet;
    bool rotateToggle = true;
    Transform filSprite;

    // refactor phasing and stamina into their own classes (maybe double jumps as well?)
    public GameObject PhasingColliderObj; // also use for checking cutscene
    public GameObject SpritePivoter;
    public static bool isCutscene;
    // Start is called before the first frame update
    void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = Screen.currentResolution.refreshRate;
        Screen.SetResolution(2560, 1440, FullScreenMode.MaximizedWindow);
        //Application.targetFrameRate = 60;

        feet = GetComponentInChildren<Transform>();
        rb2d = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        audiosource = GetComponent<AudioSource>();
        groundContact = GetComponent<AudioSource>();
        AbilityTrigger = GameObject.FindGameObjectWithTag("FilSprite").GetComponent<AudioSource>();
        filSprite = GameObject.FindGameObjectWithTag("FilSprite").GetComponent<Transform>();
        SpritePivoter = GameObject.Find("FilSpritePivot");
        canDoubleJump = true;
        doubleJumpUnlock = true;
        canPhaseShift = true;
        phaseShiftUnlock = true;
        isCutscene = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (!ControlMapping.validateInput())
        {
            rb2d.velocity = Vector2.zero;
        }

        checkCutscene();
        // setCoyote();
        if (!Snailian.isSnailianActive && !FilHealth.isDead)
        {
            rb2d.velocity = !isCutscene ? calcVelocity(rb2d.velocity) : Vector2.zero;
        }
        createGroundSound();
        if (rb2d.velocity.x > 0.00001)
        {
            //spriteRenderer.flipX = false;
            SpritePivoter.transform.eulerAngles = new Vector3(SpritePivoter.transform.eulerAngles.x, 0, SpritePivoter.transform.eulerAngles.z);
        }
        if(rb2d.velocity.x < -0.00001)
        {
            //spriteRenderer.flipX = true;
            SpritePivoter.transform.eulerAngles = new Vector3(SpritePivoter.transform.eulerAngles.x, 180, SpritePivoter.transform.eulerAngles.z);

        }
        if (FilHealth.isDead)
            rb2d.velocity = Vector3.zero;
    }

    void checkCutscene()
    {
        isCutscene = PhasingColliderObj.GetComponent<FilConvo>().checkIsCutscene();
    }
    
    void createGroundSound()
    {
        if(grounded() && Mathf.Abs((int)rb2d.velocity.x) > 1 && !groundContact.isPlaying)
        {
            groundContact.clip = grassWalkSound;
            groundContact.volume = .15f;
            groundContact.pitch = 1.5f;
            groundContact.Play();
            filSprite.localRotation = rotateToggle ? Quaternion.Euler(filSprite.localRotation.eulerAngles.x, filSprite.localRotation.eulerAngles.y, 10) : Quaternion.Euler(0, filSprite.localRotation.eulerAngles.y, -10);
            rotateToggle = !rotateToggle;
        }
    }

    Vector2 calcVelocity(Vector2 vel)
    {
        Vector2 velocity = vel;

        // offset acceleration for boost/phase shifting
        if(Mathf.Abs(velocity.x) > maxVelocity)
        {
            if (PhaseShift.isPhaseShifting)
            {
                if (velocity.x > 0)
                    velocity.x -= Time.deltaTime * velIncr * 10;
                else
                    velocity.x += Time.deltaTime * velIncr * 10;
            }
            else
            {
                if (velocity.x > 0)
                    velocity.x -= Time.deltaTime * velIncr;
                else
                    velocity.x += Time.deltaTime * velIncr;
            }
        }

        // disable movement when menu active
        if (!ActiveToggle.isMenuActive)
        {
            // sim physics based, less snappy, independent to frame rate
            if (Input.GetKey(ControlMapping.KeyMap["Move Right"]) && velocity.x < maxVelocity && ControlMapping.validateInput())
            {
                if (velocity.x < 0)
                    velocity.x += Time.deltaTime * velRedirect;
                else
                    velocity.x += Time.deltaTime * velIncr;
                if (velocity.x > maxVelocity)
                    velocity.x = maxVelocity;
            }
            if (Input.GetKey(ControlMapping.KeyMap["Move Left"]) && velocity.x > maxVelocity * -1 && ControlMapping.validateInput())
            {
                if (velocity.x > 0)
                    velocity.x -= Time.deltaTime * velRedirect;
                else
                    velocity.x -= Time.deltaTime * velIncr;
                if (velocity.x < maxVelocity * -1)
                    velocity.x = maxVelocity * -1;
            }
        }
        if((!Input.GetKey(ControlMapping.KeyMap["Move Left"]) && !Input.GetKey(ControlMapping.KeyMap["Move Right"]) && ControlMapping.validateInput()) || !grounded())
        {
            filSprite.localRotation = Quaternion.Euler(filSprite.localRotation.eulerAngles.x, filSprite.localRotation.eulerAngles.y, filSprite.localRotation.eulerAngles.z);
        }
        // natural deceleration lateral
        if((!Input.GetKey(ControlMapping.KeyMap["Move Left"]) && !Input.GetKey(ControlMapping.KeyMap["Move Right"]) && ControlMapping.validateInput() && !GrappleRope.disableGravitySim) || ActiveToggle.isMenuActive)
        {
            if (!grounded())    // if not on ground, decelerate more slowly
            {
                if (velocity.x > 0)
                {
                    velocity.x -= Time.deltaTime * aerialVelDecr;
                    if (velocity.x < 0)
                        velocity.x = 0;
                }
                if (velocity.x < 0)
                {
                    velocity.x += Time.deltaTime * aerialVelDecr;
                    if (velocity.x > 0)
                        velocity.x = 0;
                }
            }
            else
            {
                filSprite.localRotation = Quaternion.Euler(filSprite.localRotation.eulerAngles.x, filSprite.localRotation.eulerAngles.y, 0);
                if (velocity.x > 0)
                {
                    velocity.x -= Time.deltaTime * velDecr;
                    if (velocity.x < 0)
                        velocity.x = 0;
                }
                if (velocity.x < 0)
                {
                    velocity.x += Time.deltaTime * velDecr;
                    if (velocity.x > 0)
                        velocity.x = 0;
                }
            }
        }
        return velocity;
    }

    // checks whether player is in contact with a ground surface
    // used to reset jump
    bool grounded()
    {
        Collider2D[] groundImpact = Physics2D.OverlapBoxAll(feet.position + offset, colSize, 0, groundLayers);
        if (groundImpact.Length == 0)
        {
            return false;
        }
        else
        {
            canDoubleJump = true;
            return true;
        }
    }

    public Vector3 getPosition()
    {
        return transform.position;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireCube(feet.position + offset, colSize);
    }
}
