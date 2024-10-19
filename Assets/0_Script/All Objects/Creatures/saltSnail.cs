using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class saltSnail : Animal
{
    private Rigidbody2D myRigidbody;
    public Transform target; 
    public float hideRadius;
    public Transform homePosition;
    public Animator anim;
    private Vector3 mCurrentRoamDirection;
    private float mRoamTimer;
    private float mCurrentRoamTimer;
    private float mCurrentWaitTimer;
    public float saltRegenerateTimer = 5.0f;
    private float mCurrentSaltRegenerateTimer;
    private bool mIsHidden;

    void Start()
    {
        currentState = AnimalState.idle;
        myRigidbody = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        target = GameObject.FindWithTag("Player").transform;
        mCurrentWaitTimer = 5.0f;
        mCurrentSaltRegenerateTimer = saltRegenerateTimer;
        mIsPickUpable = false;
        mIsHidden = true;
        anim.SetBool("isPickUpable", mIsPickUpable);
    }

    void FixedUpdate()
    {
        CheckDistance();

        if (mCurrentSaltRegenerateTimer > 0.0f)
        {
            mCurrentSaltRegenerateTimer -= Time.deltaTime;
        }
        else
        {
            mIsPickUpable = mIsHidden;
            anim.SetBool("isPickUpable", true);
        }
    }

    void CheckDistance()
    {
        float distance = Vector3.Distance(target.position, transform.position);
        
        // 숨는 범위 안으로 플레이어가 들어오면
        if(distance <= hideRadius)
        {
            // 움직임 X
            anim.SetBool("isMoving", false);

            // 움직이기 타이머 X
            mCurrentRoamTimer = 0.0f;
            mRoamTimer = 0.0f;
            
            // Hiding state로 transition 하도록
            ChangeState(AnimalState.hide);
            anim.SetBool("wakeUp", false);
        }

        // Pick up 가능 여부 확인 - Hidden state인지?
        mIsHidden = anim.GetCurrentAnimatorStateInfo(0).IsName("Hidden") || anim.GetCurrentAnimatorStateInfo(0).IsName("NsaltSnail_Stop");

        // Hidden 타이머
        mCurrentWaitTimer -= Time.fixedDeltaTime;

        // Hidden 타이머 끝나면
        //  아직 플레이어가 범위에 있으면 숨고
        //  플레이어 범위에 없으면 idle하든, 움직이든!
        if (mCurrentWaitTimer <= 0.0f)
        {
            // 일단 일어나고 timer 5초로 초기화
            anim.SetBool("wakeUp", true);
            mCurrentWaitTimer = 5.0f;
        }

        // 플레이어가 범위에 없어
        if (distance > hideRadius)
        {
            // 만약 움직/가만히 타이머가 끝났어
            if (mCurrentRoamTimer <= 0.0f)
            {
                // 일단 멈춰
                ChangeState(AnimalState.idle);
                anim.SetBool("isMoving", false);
                
                // 지금 idle 상태면
                if (anim.GetCurrentAnimatorStateInfo(0).IsName("Idle"))
                {
                    bool roam = Random.value > 0.5; // 다음에 움직일지, 가만히 있을 지?
                    mRoamTimer = Random.Range( 3.0f, 5.0f); // 움직/가만히 있을 시간
                    mCurrentRoamTimer = mRoamTimer - (0.0f - mCurrentRoamTimer);

                    // 돌아다닐거면 walk로
                    if (roam)
                    {
                        ChangeState(AnimalState.walk);
                        anim.SetBool("isMoving", true);
                    }
                }
            }
            
            // 움직/가만히 타이머 돌아가는 중이라면
            if (mCurrentRoamTimer > 0.0f)
            {
                // 움직여
                if (currentState == AnimalState.walk && anim.GetCurrentAnimatorStateInfo(0).IsName("Walking"))
                {
                    Vector2 moveVelocity = new Vector2(mCurrentRoamDirection.x * moveSpeed, mCurrentRoamDirection.y * moveSpeed);
                    changeAnim(new Vector2(mCurrentRoamDirection.x, mCurrentRoamDirection.y));
                    myRigidbody.MovePosition(transform.position + new Vector3(moveVelocity.x, moveVelocity.y, 1.0f) * Time.fixedDeltaTime);
                }

                mCurrentRoamTimer -= Time.fixedDeltaTime;
            }
        }
    }


    // 걸어다니는 것 기본 설정 
    private void SetAnimFloat(Vector2 setVector)
    {
        anim.SetFloat("moveX", setVector.x);
        anim.SetFloat("moveY", setVector.y);
    }

    private void changeAnim(Vector2 direction)
    {
        SetAnimFloat(direction);
    }

    // 상태 설정 
    private void ChangeState(AnimalState newState)
    {
        if(currentState != newState)
        {
            currentState = newState;
            if (currentState == AnimalState.walk)
            {
                mCurrentRoamDirection = Random.insideUnitCircle.normalized;
                if(mCurrentRoamDirection.x < 0)
                {
                    transform.localScale = new Vector2(1.0f, 1.0f);
                }
                else
                {
                    transform.localScale = new Vector2(-1.0f, 1.0f);
                }
            }
        }
    }

    public override bool PickUp()
    {
        if (mIsHidden == true && mIsPickUpable == true)
        {
            mIsPickUpable = false;
            anim.SetBool("isPickUpable", false);
            mCurrentSaltRegenerateTimer = saltRegenerateTimer;
            return base.PickUp();  // 인벤토리에 넣기
        }

        return false;
    }
}
