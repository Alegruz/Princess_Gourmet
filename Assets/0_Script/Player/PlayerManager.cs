using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Tilemaps;
using Unity.VisualScripting;
using UnityEngine.InputSystem.LowLevel;

public enum PlayerState
{
    walk,
    run,
    attack,
    // interact,
    stagger,
    idle
}


public class PlayerManager : MonoBehaviour
{
    // 다이얼로그 매니저 
    public DialogueManager manager;
    // 오브젝트 조사
    float h;
    float v; 
    Vector3 dirVec;
    GameObject scanObject;


    public float walkSpeed;
    public float runSpeed;

    private float speed;
    public PlayerState currentState = PlayerState.idle;
    private Rigidbody2D myRigidbody;
    private Vector3 change;
    private Animator animator;
    public FloatValue currentHealth;
    public Signal playerHealthSignal;
    public VectorValue startingPosition;

    private List<GameObject> mCurrentCollidingItems;
    private List<GameObject> mCurrentPickUpObjects;
    private bool hasConsumedSpaceKey;
    private bool mIsKnockingBack;

    //AudioManager 인스펙터 창에 추가
    public string walkSound;
    public string runSound;
    public string pickUpSound;
    public List<Tilemap> grassTilemaps;
    public List<Tilemap> rockTilemaps;
    public List<Tilemap> woodTilemaps;
    private Tilemap currentTilemapOrNull;
    private string currentWalkSound;
    private AudioManager theAudio;
    private Vector3Int mPreviousTilePosition = Vector3Int.zero;
    private bool mbHasInitializedPreviousTilePosition = false;
    void Start()
    {
        // Limit the framerate to 30
        Application.targetFrameRate = 30;

        SetCurrentState(PlayerState.idle);
        animator = GetComponent<Animator>();
        myRigidbody = GetComponent<Rigidbody2D>();
        animator.SetFloat("moveX", 0);
        animator.SetFloat("moveY", -1);
        transform.position = startingPosition.initialValue;
        mCurrentCollidingItems = new List<GameObject>();
        mCurrentPickUpObjects = new List<GameObject>();
        hasConsumedSpaceKey = false;
        mIsKnockingBack = false;
    }

    private void OnDestroy()
    {
        
    }

    // 다이얼로그 매니저, 오브젝트 조사 
    void Update() 
    {
        // 오브젝트 조사 
            //Move Value
            h = manager.isAction ? 0 : Input.GetAxisRaw("Horizontal");
            v = manager.isAction ? 0 : Input.GetAxisRaw("Vertical");

            //Check Button Down & Up
            bool hDown = manager.isAction ? false : Input.GetButtonDown("Horizontal");
            bool vDown = manager.isAction ? false : Input.GetButtonDown("Vertical");
            bool hUp = manager.isAction ? false : Input.GetButtonUp("Horizontal");
            bool vUp = manager.isAction ? false : Input.GetButtonUp("Vertical");

            //Direction 
            if (vDown && v == 1)
                dirVec = Vector3.up;
            else if (vDown && v == -1)
                dirVec = Vector3.down;
            else if (hDown && v == -1)
                dirVec = Vector3.left;
            else if (hDown && h == 1)
                dirVec = Vector3.right;

            //scan object & Action
            if (Input.GetKeyDown(KeyCode.Space) && scanObject != null)
            {
                manager.Action(scanObject);
            }
    }

    void FixedUpdate()
    {
        bool bHasFoundTile = false;

        if (grassTilemaps.Count > 0)
        {
            foreach (var grassTilemap in grassTilemaps)
            {
                Vector3Int grassTilePosition = grassTilemap.WorldToCell(transform.position);
                if (mbHasInitializedPreviousTilePosition == false || mPreviousTilePosition != grassTilePosition)
                {
                    if (grassTilemap.HasTile(grassTilePosition))
                    {
                        mPreviousTilePosition = grassTilePosition;
                        mbHasInitializedPreviousTilePosition = true;
                        currentTilemapOrNull = grassTilemap;
                        currentWalkSound = walkSound + "_grass";
                        bHasFoundTile = true;
                        break;
                    }
                }
            }
        }

        if (rockTilemaps.Count > 0 && bHasFoundTile == false)
        {
            foreach (var rockTilemap in rockTilemaps)
            {
                Vector3Int rockTilePosition = rockTilemap.WorldToCell(transform.position);
                if (mbHasInitializedPreviousTilePosition == false || mPreviousTilePosition != rockTilePosition)
                {
                    if (rockTilemap.HasTile(rockTilePosition))
                    {
                        mPreviousTilePosition = rockTilePosition;
                        mbHasInitializedPreviousTilePosition = true;
                        currentTilemapOrNull = rockTilemap;
                        currentWalkSound = walkSound + "_rock";
                        bHasFoundTile = true;
                        break;
                    }
                }
            }
        }

        if (woodTilemaps.Count > 0 && bHasFoundTile == false)
        {
            foreach (var woodTilemap in woodTilemaps)
            {
                Vector3Int woodTilePosition = woodTilemap.WorldToCell(transform.position);
                if (mbHasInitializedPreviousTilePosition == false || mPreviousTilePosition != woodTilePosition)
                {
                    if (woodTilemap.HasTile(woodTilePosition))
                    {
                        mPreviousTilePosition = woodTilePosition;
                        mbHasInitializedPreviousTilePosition = true;
                        currentTilemapOrNull = woodTilemap;
                        currentWalkSound = walkSound + "_rock";
                        bHasFoundTile = true;
                        break;
                    }
                }
            }
        }

        // 오브젝트 조사
        //Ray 
        Debug.DrawRay(myRigidbody.position, dirVec * 2.0f, new Color(0, 1, 0));
        RaycastHit2D rayHit = Physics2D.Raycast(myRigidbody.position, dirVec, 2.0f, LayerMask.GetMask("Object_goldmetal"));

        if (rayHit.collider != null)
        {
            scanObject = rayHit.collider.gameObject;
        }
        else
        {
            scanObject = null;
        }

        change = Vector3.zero;
        change.x = Input.GetAxisRaw("Horizontal");
        change.y = Input.GetAxisRaw("Vertical");

        // 아이템 줍기 
        //당근 줍기
        if (mCurrentCollidingItems.Count > 0)
        {
            if (!hasConsumedSpaceKey && Input.GetKeyDown(KeyCode.Space))
            {
                GameObject itemGameObjectToPickUp = mCurrentCollidingItems[0];
                PhysicalInventoryItem physicalInventoryItem = itemGameObjectToPickUp.GetComponent<PhysicalInventoryItem>();
                bool hasPickedUpObject = physicalInventoryItem.PickUp();

                mCurrentCollidingItems.RemoveAt(0);
                Destroy(itemGameObjectToPickUp);
                hasConsumedSpaceKey = true;

                if (hasPickedUpObject)
                {
                    //AudioManager 추가 
                    theAudio = FindObjectOfType<AudioManager>();
                    //AudioManager pickUp sound
                    theAudio.Play(pickUpSound);
                }
            }
        }

        //시럽나무 줍기
        if (mCurrentPickUpObjects.Count > 0)
        {
            if (!hasConsumedSpaceKey && Input.GetKeyDown(KeyCode.Space))
            {
                GameObject itemGameObjectToPickUp = mCurrentPickUpObjects[0];
                bool hasPickedUpObject = itemGameObjectToPickUp.GetComponent<PhysicalInventoryItem>().PickUp();

                hasConsumedSpaceKey = true;

                if (hasPickedUpObject)
                {
                    //AudioManager 추가 
                    theAudio = FindObjectOfType<AudioManager>();
                    //AudioManager pickUp sound
                    theAudio.Play(pickUpSound);
                }
            }
        }

        // 젤다 튜토리얼 - 플레이어 기본 움직임 셋팅 
        if (hasConsumedSpaceKey && Input.GetKeyDown(KeyCode.Space) == false)
        {
            hasConsumedSpaceKey = false;
        }

        if (Input.GetButtonDown("attack") && currentState != PlayerState.attack
            && currentState != PlayerState.stagger)
        {
            StartCoroutine(AttackCo());
        }

        if (mIsKnockingBack == false)
        {
            myRigidbody.velocity = Vector2.zero;
        }

        if (currentState == PlayerState.walk || currentState == PlayerState.run
            || currentState == PlayerState.idle)
        {
            UpdateAnimationAndMove();
        }
    }

    private IEnumerator AttackCo()
    {
        PlayerState prevState = currentState;
        animator.SetBool("attacking", true);
        SetCurrentState(PlayerState.attack);
        yield return null;
        animator.SetBool("attacking", false);
        yield return new WaitForSeconds(.3f);
        SetCurrentState(prevState);
    }

    // 아이템 줍기 OnCollision (채집 가능 아이콘 표시)
    private void OnCollisionEnter2D(Collision2D other)
    {
        // 당근 줍기 
        Collider2D collider = GetComponent<Collider2D>();
        if(collider.isTrigger == false && other.gameObject.CompareTag("item"))
        {
            if (mCurrentCollidingItems.Contains(other.gameObject) == false)
            {
                mCurrentCollidingItems.Add(other.gameObject);
            }

            if (mCurrentCollidingItems.Count == 1)
            {
                GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 1f);
            }
        }

        //시럽, 동물 수집 
        else if(collider.isTrigger == false && other.gameObject.CompareTag("PickUp Object"))
        {
            if (mCurrentPickUpObjects.Contains(other.gameObject) == false)
            {
                mCurrentPickUpObjects.Add(other.gameObject);
            }

            if (mCurrentPickUpObjects.Count == 1)
            {
                GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 1f);
            }
        }
    }
    private void OnCollisionExit2D(Collision2D other)
    {
        //당근 수집 
        if (other.gameObject.CompareTag("item"))
        {
            mCurrentCollidingItems.Remove(other.gameObject);

            if (mCurrentCollidingItems.Count == 0)
            {
                GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 1);
            }
        }

        //시럽, 동물 수집 
        else if (other.gameObject.CompareTag("PickUp Object"))
        {
            mCurrentPickUpObjects.Remove(other.gameObject);

            if (mCurrentPickUpObjects.Count == 0)
            {
                GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 1);
            }
        }
    }

    void UpdateAnimationAndMove()
    {
        if(change != Vector3.zero)
        {
            MoveCharacter();
            animator.SetFloat("moveX", change.x);
            animator.SetFloat("moveY", change.y);
            animator.SetBool("moving", true);
            
            if (Input.GetButton("run") == false)
            {
                if (currentState != PlayerState.walk)
                {
                    Debug.Log($"start walk {Input.GetButton("run")}");
                    SetCurrentState(PlayerState.walk);
                }
            }
            else
            {
                if (currentState != PlayerState.run && currentState != PlayerState.stagger && currentState != PlayerState.attack /*&& currentState != PlayerState.interact*/)
                {
                    SetCurrentState(PlayerState.run);
                }
            }
        }
        else
        {
            if (currentState == PlayerState.walk)
            {
                SetCurrentState(PlayerState.idle);
            }
            if (currentState == PlayerState.run)
            {
                SetCurrentState(PlayerState.idle);
            }
            animator.SetBool("moving", false);
        }
    }


    void MoveCharacter()
    {
        change.Normalize();
        myRigidbody.velocity = change * speed;
    }

    public void Knock(float knockTime, float damage)
    {
        currentHealth.RuntimeValue -= damage;
        playerHealthSignal.Raise();
        if(currentHealth.RuntimeValue > 0)
        {
            mIsKnockingBack = true;
            StartCoroutine(KnockCo(knockTime));
        }
        else
        {
            this.gameObject.SetActive(false);
        }
    }

    private IEnumerator KnockCo(float knockTime)
    {
        if(myRigidbody != null)
        {
            SetCurrentState(PlayerState.stagger);
            yield return new WaitForSeconds(knockTime);
            myRigidbody.velocity = Vector2.zero;
            SetCurrentState(PlayerState.idle);
                //넉백을 받으면 공주가 자꾸 가만히 멈춰서서 idle 에서 walk로 고쳐봤음
            myRigidbody.velocity = Vector2.zero;
            mIsKnockingBack = false;
        }
    }
  
    //run 기능을 만들기 위해 새로 만든
    private PlayerState SetCurrentState(PlayerState newState)
    //private void SetCurrentState(PlayerState newState)
    //넉백 스크립트에서 자꾸 player.currentstate 프로텍션 레벨 때문에 접근을 못한다고 해서
    //위에 playerstate 선언한거 퍼블릭으로 바꾸고 밑에 리턴을 주니까 해결했음. 맞나??
    {

        switch (newState)
        {
            case PlayerState.walk:
                speed = walkSpeed;
                animator.speed = 1.0f;
                break;
            case PlayerState.run:
                speed = runSpeed;
                animator.speed = runSpeed / walkSpeed;
                break;
            case PlayerState.attack:
                speed = 0;
                break;
            // case PlayerState.interact:
            // speed = 0;
            // break;
            case PlayerState.stagger:
                speed = 0;
                break;
            case PlayerState.idle:
                speed = 0;
                break;
            default:
                Assert.IsTrue(false);
                break;
        }
        currentState = newState;
        return currentState;
    }

    public List<GameObject> GetCurrentCollidingItems()
    {
        return mCurrentCollidingItems;
    }

    public List<GameObject> GetCurrentPickUpObjects()
    {
        return mCurrentPickUpObjects;
    }

    public void OnFootStep()
    {
        theAudio = FindObjectOfType<AudioManager>();
        theAudio.Play(currentWalkSound);
    }

    public void OnFriedPanHit()
    {
        theAudio = FindObjectOfType<AudioManager>();
    }
}
