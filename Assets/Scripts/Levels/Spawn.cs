using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

public class Spawn
{
    public string enemy; // "zombie"
    public string count; // "5 wave +"
    public string hp; // "base 5 wave * +"
    public int delay; // 5
    public List<int> sequence; // [1,2,3]
    public string location; // "random"
}