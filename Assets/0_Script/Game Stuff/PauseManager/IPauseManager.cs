using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IPauseManager : MonoBehaviour
{
    static public GameObject mCurrentActiveGameObjectOrNull = null;
    static public GameObject mCurrentActiveSubGameObjectOrNull = null;

    protected bool isPaused;
    public GameObject GameObjectToPause;
    protected string ButtonName;
    protected bool isInventoryPressed;
    private static bool isPausable = true;

    private AudioManager mAudioManager = null;

    //Start is called before the first frame update
    void Start()
    {
        isPaused = false;
        isInventoryPressed = false;
    }

    //FixedUpdate is called once per frame
    void Update()
    {
        if(isInventoryPressed == false)
        {
            if (Input.GetButtonDown(ButtonName))
            {
                isInventoryPressed = true;
                ChangePause(mCurrentActiveSubGameObjectOrNull == GameObjectToPause);
            }
        }
        else if (Input.GetButtonDown(ButtonName) == false)
        {
            isInventoryPressed = false;
        }
    }

    protected virtual void onChange(bool isActive)
    {
    }

    public void ChangePause(bool bOpeningSubGameObject)
    {
        if (isPausable == false)
        {
            return;
        }

        GameObject currentActiveGameObject = bOpeningSubGameObject ? mCurrentActiveSubGameObjectOrNull : mCurrentActiveGameObjectOrNull;
        GameObject currentSubGameObject = bOpeningSubGameObject == false ? mCurrentActiveSubGameObjectOrNull : mCurrentActiveGameObjectOrNull;
        if (currentActiveGameObject == null || (currentActiveGameObject == GameObjectToPause && (bOpeningSubGameObject || currentSubGameObject == null)))
        {
            isPaused = !isPaused;
            if (mAudioManager == null)
            {
                mAudioManager = FindObjectOfType<AudioManager>();
            }
            mAudioManager.Play("page turn");
            if (isPaused)
            {
                isPaused = true;
                GameObjectToPause.SetActive(true);
                onChange(true);
                Time.timeScale = 0f;
                if (bOpeningSubGameObject == true)
                {
                    mCurrentActiveSubGameObjectOrNull = GameObjectToPause;
                }
                else
                {
                    mCurrentActiveGameObjectOrNull = GameObjectToPause;
                }
            }
            else
            {
                GameObjectToPause.SetActive(false);
                onChange(false);
                Time.timeScale = 1f;
                currentActiveGameObject = null;
                if (bOpeningSubGameObject == true)
                {
                    mCurrentActiveSubGameObjectOrNull = null;
                }
                else
                {
                    mCurrentActiveGameObjectOrNull = null;
                }
            }
        }
    }

    public static void SetPausable(bool value)
    {
        isPausable = value;
    }

    public AudioManager GetAudioManager()
    {
        return mAudioManager;
    }
}
