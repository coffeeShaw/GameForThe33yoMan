﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Jumping : MonoBehaviour
{
    Rigidbody2D rb2d;
    Transform filSprite;
    protected SpriteRenderer spriteRenderer;
    protected GameObject filSpritePivot;
    GameObject ColliderObj;

    public static bool canDoubleJump;
    bool freeFrame = true;
    public LayerMask groundLayers;
    public Vector2 colSize;
    public Vector3 offset;
    public float terminalVelocity;
    public float doubleJumpMulti;
    public float jumpMulti;
    public float jumpDecay;
    public float fallMulti;
    Transform feet;
    public int numFreeFrames;
    int freeFrameDelta;
    public AudioClip SingleJumpSound;
    public AudioClip DoubleJumpSound;
    AudioSource AbilityTrigger;
    bool isInFlip;
    bool isDoubleJumping;
    public float flipRate;

    float internalRotTracker;

    // Start is called before the first frame update
    void Start()
    {
        internalRotTracker = 0;
        canDoubleJump = true;
        rb2d = GameObject.Find("Bubble Player").GetComponent<Rigidbody2D>();
        spriteRenderer = GameObject.Find("FilSprite").GetComponent<SpriteRenderer>();
        filSprite = GameObject.FindGameObjectWithTag("FilSprite").GetComponent<Transform>();
        ColliderObj = GameObject.Find("FilHealthObj");
        filSpritePivot = GameObject.Find("FilSpritePivot");
        AbilityTrigger = GetComponent<AudioSource>();
        feet = GetComponent<Transform>();
    }

    public void handleJumping()
    {
        // reset double jump on grapple
        if (GrappleRope.disableGravitySim)
            canDoubleJump = true;

        // handle jump logic
        Vector2 velocity = rb2d.velocity;
        if (Input.GetKeyDown(ControlMapping.KeyMap["Jump"]) && (grounded() || canDoubleJump) && !GrappleRope.disableGravitySim && ControlMapping.validateInput())
        {
            // handle jumptypes here
            // double jump
            if (!grounded() && canDoubleJump && !checkCoyote())
            {
                isDoubleJumping = true;
                GameObject.Find("FilAttacks").GetComponent<FilAttacks>().isDoubleJumping = true; // move this out of attacks i swtg
                filSprite.localRotation = Quaternion.Euler(filSprite.localRotation.eulerAngles.x, filSprite.localRotation.eulerAngles.y, filSprite.localRotation.eulerAngles.z);
                canDoubleJump = false;
                AbilityTrigger.clip = DoubleJumpSound;
                velocity.y = doubleJumpMulti;
            }
            //single jump
            else
            {
                filSprite.localRotation = Quaternion.Euler(filSprite.localRotation.eulerAngles.x, filSprite.localRotation.eulerAngles.y, 0);
                AbilityTrigger.clip = SingleJumpSound;
                velocity.y = jumpMulti;
            }
            AbilityTrigger.Play();
        }

        // gravity sim rising
        if (rb2d.velocity.y > 0 && !grounded())
        {
            velocity.y -= jumpDecay * Time.deltaTime;
        }
        // gravity sim falling
        if (rb2d.velocity.y < 0 && rb2d.velocity.y > -1 * terminalVelocity)
        {
            velocity += Vector2.up * Physics2D.gravity.y * fallMulti * Time.deltaTime;
        }
        // terminal velocity force
        else if (rb2d.velocity.y < 0 && velocity.y < 0)
        {
            velocity.y = terminalVelocity * -1;
        }

        // only modifies Y value
        rb2d.velocity = velocity;
    }

    // HELPER FUNCTIONS
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

    // Coyote time allows us to let the player jump N frame after they have left a platform
    // feels better for player
    public void setCoyote()
    {
        if (grounded())
        {
            freeFrame = true;
            freeFrameDelta = numFreeFrames;
            return;
        }
        if (freeFrameDelta > 0)
        {
            freeFrameDelta--;
            return;
        }
        freeFrame = false;
    }
    private bool checkCoyote()
    {
        return freeFrame;
    }

    public void doubleJumpFlipCheck()
    {
        if (isDoubleJumping)
        {
          /*  filSprite.localRotation = Quaternion.Euler(filSprite.localRotation.eulerAngles.x,
                filSprite.localRotation.eulerAngles.y,
                filSprite.localRotation.eulerAngles.z + (flipRate * Time.deltaTime));*/
           
            filSpritePivot.transform.localRotation = Quaternion.Euler(filSpritePivot.transform.localRotation.eulerAngles.x,
                filSpritePivot.transform.localRotation.eulerAngles.y,
                filSpritePivot.transform.localRotation.eulerAngles.z - (flipRate * Time.deltaTime));

            internalRotTracker -= (flipRate * Time.deltaTime);
            if (internalRotTracker < -360)
            {
                isInFlip = true;

            }
            if (isInFlip)
            {
                if (internalRotTracker < 180)
                {
                    isInFlip = false;
                    isDoubleJumping = false;
                    internalRotTracker = 0;
                    filSpritePivot.transform.localRotation = Quaternion.Euler(filSprite.localRotation.x, filSprite.localRotation.y, 0);
                }
            }
        }
    }
}
