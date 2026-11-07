using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Cake", menuName = "Cake")]
public class Cake : ScriptableObject
{
    public Sprite skin;
    public int price;
    public enum State { Locked, Unlocked };
    [SerializeField] private State currentState;

    public void Unlock()
    {
        currentState = State.Unlocked;
    }

    public void Lock()
    {
        if (price == 0)
        {
            currentState = State.Unlocked;
            return;
        }
        currentState = State.Locked;
    }

    public State GetState()
    {
        if (price == 0)
        {
            return State.Unlocked;
        }
        return currentState;
    }

    public int GetPrice()
    {
        return price;
    }
}
