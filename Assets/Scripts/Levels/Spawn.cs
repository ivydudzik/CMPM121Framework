using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

public class Spawn
{
    public string enemy; // "zombie"
    public string count; // "5 wave +"
    public string hp = "base"; // "base 5 wave * +"
    public string speed = "base";
    public string damage = "base";
    public int delay = 2; // 5
    public List<int> sequence = new List<int>(){1}; // [1,2,3]
    public string location = "random"; // "random"
}