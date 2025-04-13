using System.Collections.Generic;
using UnityEngine;

public static class RPNEvaluator
{
    public static int Evaluate(string stringToEvaluate, Dictionary<string, int> variableDict)
    {
        // DEBUG: Test print
        Debug.Log(variableDict.Values);

        // TEMP: Evaluation code that can only parse "base" and integer values
        if (stringToEvaluate == "base")
        {
            return variableDict["base"];
        } else
        {
            return int.Parse(stringToEvaluate);
        }
    }

}
