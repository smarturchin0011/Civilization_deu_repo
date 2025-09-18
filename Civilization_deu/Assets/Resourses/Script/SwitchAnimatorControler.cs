using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchAnimatorControler : MonoBehaviour
{
    [SerializeField] public Animator switchAnimator;
    [SerializeField] public bool IsSwitch = false;

    public void OnSwitch()
    {
        switchAnimator.SetBool("IsSwitch",true);
        IsSwitch = true;
        switchAnimator.SetBool("IsSwitch",false);
        IsSwitch = false;
    }

}
