// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;


Console.WriteLine(new SelfReferentialStructWithNoFieldsAuto());
Console.WriteLine(new SelfReferentialStructWithNoFieldsSequential());
Console.WriteLine(new SelfReferentialStructWithNoFieldsSequential());

Console.WriteLine(new SelfReferentialGenericStructWithNoFieldsAuto<int>());
Console.WriteLine(new SelfReferentialGenericStructWithNoFieldsSequential<int>());
Console.WriteLine(new SelfReferentialGenericStructWithNoFieldsSequential<int>());

Console.WriteLine(new SelfReferentialGenericStructWithNoFieldsAuto<string>());
Console.WriteLine(new SelfReferentialGenericStructWithNoFieldsSequential<string>());
Console.WriteLine(new SelfReferentialGenericStructWithNoFieldsSequential<string>());

Console.WriteLine(typeof(MyNodeAuto).FullName);
Console.WriteLine(typeof(MyNodeSequential).FullName);

public class Container<T> {
    public struct Nested { }
}

[StructLayout(LayoutKind.Auto)]
public struct SelfReferentialStructWithNoFieldsAuto {
    public Container<SelfReferentialStructWithNoFieldsAuto>.Nested Nested;
}
[StructLayout(LayoutKind.Sequential)]
public struct SelfReferentialStructWithNoFieldsSequential {
    public Container<SelfReferentialStructWithNoFieldsSequential>.Nested Nested;
}
[StructLayout(LayoutKind.Sequential)]
public struct SelfReferentialStructWithStringFieldSequential {
    public Container<SelfReferentialStructWithStringFieldSequential>.Nested Nested;
    public string String;
}

[StructLayout(LayoutKind.Auto)]
public struct SelfReferentialGenericStructWithNoFieldsAuto<T> {
    public Container<SelfReferentialGenericStructWithNoFieldsAuto<T>>.Nested Nested;
}
[StructLayout(LayoutKind.Sequential)]
public struct SelfReferentialGenericStructWithNoFieldsSequential<T> {
    public Container<SelfReferentialGenericStructWithNoFieldsSequential<T>>.Nested Nested;
}
[StructLayout(LayoutKind.Sequential)]
public struct SelfReferentialGenericStructWithStringFieldSequential<T> {
    public Container<SelfReferentialGenericStructWithStringFieldSequential<T>>.Nested Nested;
    public string String;
}


/// <summary>
/// List of T expressed as a value type
/// </summary>
public struct ValueList<T>
{
    private T[] _arr;
    private int _count;
}

[StructLayout(LayoutKind.Auto)]
public struct MyNodeAuto
{
    public int NodeData;

    public ValueList<MyNodeAuto> Nodes;
}

[StructLayout(LayoutKind.Sequential)]
public struct MyNodeSequential
{
    public int NodeData;

    public ValueList<MyNodeSequential> Nodes;
}