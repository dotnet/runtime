// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// Regression test case for importer bug.
// If Release is inlined into Main, the importer may unsafely re-order trees.
    
public struct Ptr<T> where T: class
{
    private T _value;
    
    public Ptr(T value)
    {
        _value = value;
    }
    
    public T Release()
    {
        T tmp = _value;
        _value = null;
        return tmp;
    }
}

public class Runtime_764
{
    [Fact]
    public static int TestEntryPoint()
    {
        Ptr<string> ptr = new Ptr<string>("Hello, world");
        
        bool res = false;
        while (res)
        {
        }
        
        string summary = ptr.Release();
        
        if (summary == null)
        {
            Console.WriteLine("FAILED");            
            return -1;
        }
        else
        {
            Console.WriteLine("PASSED");            
            return 100;
        }
    }
}
