using System.Collections.Generic;
using UnityEngine;

public static class RPNEvaluator
{
    public static int Evaluate(string stringToEvaluate, Dictionary<string, int> variableDict)
    {
        Debug.Log(variableDict.Values);
        if (stringToEvaluate == "base")
        {
            return variableDict["base"];
        } else
        {
            return int.Parse(stringToEvaluate);
        }
    }

}
