// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

interface IValue
{
    int GetValue();
    void SetValue(int a);
    int Add(int a);
}

// This class is only needed to spit out IL that assumes 'this' is an object (and therefore don't box)
struct FooBarStruct_ : IValue
{
    public int GetValue()
    {
        return 0;
    }

    public void SetValue(int val)
    {
    }

    public int Add(int a)
    {
        // Force cast and boxing
        IValue valueIntf = this as IValue; 
        int val = valueIntf.GetValue();
        val += a;
        valueIntf.SetValue(val);
        return val;
    }
}

struct FooBarStruct : IValue
{
    public int _val;

    public int GetValue()
    {
        return _val;
    }

    public void SetValue(int val)
    {
        _val = val;
    }

    public int Add(int a)
    {
        // Dummy
        return 0;
    }   
}

class Program
{
    public static int Main()
    {
        FooBarStruct fooBar = new FooBarStruct();

        fooBar._val = 10;

        IValue foo = (IValue) fooBar;

        Console.WriteLine("Calling IFoo.Foo on FooBarStruct");
        Test.Assert(foo.Add(10) == 20, "Calling default method IValue.Add on FooBarStruct failed");
        Test.Assert(fooBar.GetValue() == 10, "FooBarStruct value should remain unchanged");

        return Test.Ret();
    }
}

class Test
{
    private static bool Pass = true;

    public static int Ret()
    {
        return Pass? 100 : 101;
    }

    public static void Assert(bool cond, string msg)
    {
        if (cond)
        {
            Console.WriteLine("PASS");
        }
        else
        {
            Console.WriteLine("FAIL: " + msg);
            Pass = false;
        }
    }
}                    

