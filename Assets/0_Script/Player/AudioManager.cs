using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable] //커스컴 클래스는 유니티 인스펙터 창에서 안나온다. 강제로 나오게 하는 것.
public class Sound
{
    public string name; //사운드의 이름 

    public AudioClip clip; //사운드 파일
    private AudioSource source; //사운드 플레이어

    public float Volumn;
    public bool loop;

    public void SetSource(AudioSource _source) //밑에 AudioManager 코드에서 접근을 해야하기 때문에 함수 만든거임 
    {
        source = _source;
        source.clip = clip;
        source.loop = loop;
    }

    public void SetVolumn()
    {
        if (source != null)
        {
            source.volume = Volumn;
        }
    }


    public void Play()
    {
        if (source != null)
        {
            source.Play();
        }
    }

    public void Stop()
    {
        if (source != null)
        {
            source.Stop();
        }
    }

    public void SetLoop()
    {
        if (source != null)
        {
            source.loop = true;
        }
    }

    public void SetLoopCancel()
    {
        if (source != null)
        {
            source.loop = false;
        }
    }
}

public class AudioManager : MonoBehaviour
{
    [SerializeField]
    public Sound[] sounds;

    void Start()
    {
        for(int i = 0; i < sounds.Length; i++)
        {
            GameObject soundObject = new GameObject("사운드 파일 이름 : " + i + " = " + sounds[i].name);
            sounds[i].SetSource(soundObject.AddComponent<AudioSource>());
            soundObject.transform.SetParent(this.transform);

            if (sounds[i].name == "background")
            {
                sounds[i].Play();
            }
        }
    }

    public void Play(string _name)
    {
        for (int i = 0; i < sounds.Length; i++)
        {
            if(_name == sounds[i].name)
            {
                sounds[i].Play();
                return;
            }
        }
    }

    public void Stop(string _name)
    {
        for (int i = 0; i < sounds.Length; i++)
        {
            if(_name == sounds[i].name)
            {
                sounds[i].Stop();
                return;
            }
        }
    }

    public void SetLoop(string _name)
    {
        for (int i = 0; i < sounds.Length; i++)
        {
            if(_name == sounds[i].name)
            {
                sounds[i].SetLoop();
                return;
            }
        }
    }

    public void SetLoopCancel(string _name)
    {
        for (int i = 0; i < sounds.Length; i++)
        {
            if(_name == sounds[i].name)
            {
                sounds[i].SetLoopCancel();
                return;
            }
        }
    }

    public void SetVolumn(string _name, float _Volumn)
    {
        for (int i = 0; i < sounds.Length; i++)
        {
            if(_name == sounds[i].name)
            {
                sounds[i].Volumn = _Volumn;
                sounds[i].SetVolumn();
                return;
            }
        }
    }
}