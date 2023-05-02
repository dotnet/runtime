// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using Xunit;

public class Program
{
    struct BoundingBoxTest
    {
        public Vector3 Min;
        public Vector3 Max;
        
        public override int GetHashCode()
        {
            return Min.GetHashCode() + Max.GetHashCode();
        }
    }
    
    internal static void Test()
    {
        var box = new BoundingBoxTest();
        box.Min = Vector3.Min(box.Min, box.Min);
        var hmm = box.GetHashCode();
    }
    
    [Fact]
    public static int TestEntryPoint()
    {
        var someMemory = new int[1];
        var someMoreMemory = new int[1];
        Test();
        someMoreMemory[someMemory[0]] = 100;
        return someMoreMemory[0];
    }
}
