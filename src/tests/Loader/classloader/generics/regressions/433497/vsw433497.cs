// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using Xunit;

public class Map<K,D> {}

public class C 
{
    [Fact]
    public static void TestEntryPoint()
    {
        Type t = Type.GetType("Map`2[System.Int32,System.Int32]");
        Console.WriteLine("Map<int,int>: {0}", t);
    }
}
