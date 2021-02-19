using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeqManager : MonoBehaviour
{
    [SerializeField]
    private int[] solution = { 1, 2, 3 };
    private int[] sequence = new int[3];
    private int sequencePos = 0;

    public void AddId(int ID)
    {
        sequence[sequencePos] = ID;
        sequencePos++;
        if (sequencePos >= 3)
        {
            sequencePos = 0;
            if (CheckSeq())
                Debug.Log("Code is good. Opening Door.");
            else
                Debug.Log("Invalid Code. Calling Security.");
        }
    }

    private bool CheckSeq()
    {
        for (int i = 0; i < solution.Length; i++)
        {
            if (solution[i] != sequence[i])
                return false;
            else
                continue;
        }
        return true;
    }
}
