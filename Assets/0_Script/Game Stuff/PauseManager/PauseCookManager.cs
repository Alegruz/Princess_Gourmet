using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseCookManager : IPauseManager
{
    //Start is called before the first frame update
    void Start()
    {
        ButtonName = "cook";
    }
}