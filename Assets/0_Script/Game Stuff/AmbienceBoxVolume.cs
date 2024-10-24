using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEditor.Experimental.GraphView.GraphView;

public class AmbienceBoxVolume : MonoBehaviour
{
    public string audioName;
    public bool enableAttenuation = false;
    public float attenuationBeginRatio = 1.0f;
    private AudioManager mAudioManager = null;
    private bool mbIsPlaying = false;
    private GameObject mPlayer = null;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (mbIsPlaying == true)
        {
            float distance = Vector3.Distance(mPlayer.transform.position, transform.position);

            float maxDistance = 0.0f;

            CircleCollider2D circleCollider2DOrNull = GetComponent<CircleCollider2D>();
            if (circleCollider2DOrNull != null)
            {
                maxDistance = circleCollider2DOrNull.radius;
            }

            PolygonCollider2D polygonCollider2DOrNull = GetComponent<PolygonCollider2D>();
            if (polygonCollider2DOrNull != null)
            { // Get the bounds of the PolygonCollider2D
                Bounds bounds = polygonCollider2DOrNull.bounds;

                // Calculate the closest point on the bounds
                Vector2 closestPoint = new Vector2(
                    Mathf.Clamp(mPlayer.transform.position.x, bounds.min.x, bounds.max.x),
                    Mathf.Clamp(mPlayer.transform.position.y, bounds.min.y, bounds.max.y)
                );
            }

            float attenuationBeginDistance = maxDistance * attenuationBeginRatio;

            float scale = Mathf.Abs(maxDistance - attenuationBeginDistance);
            float x = (distance - maxDistance);
            if (scale != 0.0f)
            {
                x /= scale;
            }
            x *= -1.0f;
            float ratio = x <= 0.0f ? 0.0f : (x >= 1.0f ? 1.0f : 3 * x * x - 2 * x * x * x);

            mAudioManager.SetVolumn(audioName, ratio);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (mPlayer == collision.gameObject || (mPlayer == null && collision.CompareTag("Player")))
        {
            if (mAudioManager == null)
            {
                mAudioManager = GetComponentInParent<AudioManager>();
            }
            mPlayer = collision.gameObject;
            if (mAudioManager.IsPlaying(audioName) == false)
            {
                mAudioManager.SetVolumn(audioName, 0.0f);
                mAudioManager.Play(audioName);
            }
            mbIsPlaying = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (mPlayer == collision.gameObject || (mPlayer == null && collision.CompareTag("Player")))
        {
            if (mAudioManager == null)
            {
                mAudioManager = GetComponentInParent<AudioManager>();
            }
            mPlayer = collision.gameObject;
            mbIsPlaying = false;

            if (mAudioManager.GetVolume(audioName) <= 0.000001f)
            {
                mAudioManager.Stop(audioName);
            }
        }
    }
}
