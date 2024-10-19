using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Growable : PhysicalInventoryItem
{
    [System.Serializable]
    public struct StageInfo
    {
        public Sprite sprite;
        public float secondsInThisStage;
        public bool isPickUpable;
        public bool runAnimation;
    };

    private float seconds;
    private float mElapsedSeconds;
    public List<StageInfo> stages;
    private int mCurrentStageIndex;
    
    public RuntimeAnimatorController controller;
    private Animator animator;
 
    // Start is called before the first frame update
    void Start()
    {
        bool needsAnimator = false;
        foreach (StageInfo stageInfo in stages)
        {
            if (stageInfo.runAnimation == true)
            {
                needsAnimator = true;
                break;
            }
        }

        if (needsAnimator == true)
        {
            if (controller == null)
            {
                Debug.LogError($"AnimatorController가 null입니다!!");
                Debug.Break();
            }

            Animator animatorComponent = GetComponent<Animator>();
            if (animatorComponent == null)
            {
                animator = gameObject.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.updateMode = AnimatorUpdateMode.Normal;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
            else
            {
                animator = animatorComponent;
            }
        }

        seconds = 0;
        mCurrentStageIndex = 0;
        setStage(mCurrentStageIndex);
    }
 
    // Update is called once per frame
    void FixedUpdate()
    {
        if (mElapsedSeconds < CheckToday.elapsedSeconds)
        {
            seconds += CheckToday.elapsedSeconds - mElapsedSeconds;
            mElapsedSeconds = CheckToday.elapsedSeconds;
            growing_up();
        }
    }

    public override bool PickUp()
    {
        if (mIsPickUpable == true)
        {
            base.PickUp();
            seconds = 0.0f;
            mCurrentStageIndex = 0;
            setStage(mCurrentStageIndex);

            return true;
        }

        return false;
    }
 
    void growing_up()
    {
        StageInfo currentStageInfo = stages[mCurrentStageIndex];
        if (currentStageInfo.isPickUpable == false && currentStageInfo.secondsInThisStage < seconds)
        {
            seconds -= currentStageInfo.secondsInThisStage;
            mCurrentStageIndex = (mCurrentStageIndex + 1) % stages.Count;
            setStage(mCurrentStageIndex);
        }
    }

    private void setStage(int stageIndex)
    {
        StageInfo newStageInfo = stages[stageIndex];
        GetComponent<SpriteRenderer>().sprite = newStageInfo.sprite;
        mIsPickUpable = newStageInfo.isPickUpable;

        if (animator != null)
        {
            animator.SetInteger("stageIndex", stageIndex);
            animator.enabled = newStageInfo.runAnimation;
        }
    }
}