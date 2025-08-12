// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using AssemblyC;

namespace AssemblyB;

public class ClassB
{
    public string GetMessage()
    {
        var b = new GenericClass<AssemblyA.ClassA>();
        var c = new ClassC();
        return $"Hello from Assembly B! -> {c.GetMessage()} -> {b.ToString()}";
    }
}

public class GenericClass<T>
{
}