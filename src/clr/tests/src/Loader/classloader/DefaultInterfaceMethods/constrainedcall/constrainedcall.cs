// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

interface IFoo
{
    int Foo(int c);    
}

interface IAdd 
{
    int Add(int c);
}

// Only needed for writing IFoo.Foo code
class IFoo_Impl : IFoo
{
    public int Foo(int c)
    {
        IAdd adder = (IAdd) this;
        return adder.Add(c);
    }        
}

struct FooValue : IFoo, IAdd
{
    public int val;

    public int Foo(int c)
    {
        val +=c;
        return val;
    }

    public int Add(int c)
    {
        val +=c;
        return val;
    }
}

interface IHorrible<T>
{
    int GetLocalVal();
    void SetLocalVal(int val);
    int Horrible();
}

// Only needed for the default interface implementation
class IHorrible_Impl<T> : IHorrible<T>
{
    public int GetLocalVal() { return 0; }
    public void SetLocalVal(int val) {}
    public int Horrible()
    {
        int val = GetLocalVal(); 
        val++; 
        SetLocalVal(val); 
        return val;
    }
}

struct HorribleCase<Z> : IHorrible<IList<Z>>, IHorrible<IEnumerable<Z>>
{
    int localVal;
    public int GetLocalVal() { return localVal; }
    public void SetLocalVal(int val) { localVal = val; }
    int IHorrible<IList<Z>>.Horrible() { return ++localVal; }

    // Remove
    int IHorrible<IEnumerable<Z>>.Horrible() { return ++localVal; }    
}

class HorribleTest
{
    public static int Horror<T,U>(T t) where T:IHorrible<U>
    {
        return t.Horrible() + t.Horrible();
    }

    public static void RunTest()
    {
        Test.Assert(Horror<HorribleCase<object>,IEnumerable<object>>(new HorribleCase<object>())) == 2, "Fail");
        Test.Assert(Horror<HorribleCase<object>,IList<object>>(default(HorribleCase<object>)) == 3, "Fail");
    }
}

/*
interface IFoo<T>
{
    int Foo(int c);    
}

interface IAdd 
{
    int Add(int c);
}

// Only needed for writing IFoo.Foo code
class IFoo_Impl<T> : IFoo<T>
{
    public int Foo(int c)
    {
        IAdd adder = (IAdd) this;
        return adder.Add(c);
    }        
}

struct FooValue<T> : IFoo<T>, IAdd
{
    public int val;

    public int Foo(int c)
    {
        val +=c;
        return val;
    }

    public int Add(int c)
    {
        val +=c;
        return val;
    }
}
*/   

class SimpleConstraintTest
{
    public static int CallFoo_WithConstraints<T>(ref T foo, int val) where T : IFoo
    {
        return foo.Foo(val);
    }

    /*
    public static int CallFoo_WithConstraints<T>(ref T foo, int val) where T : IFoo<object>
    {
        return foo.Foo(val);
    }
    */

    public static void RunTest()
    {
        FooValue foo = new FooValue();
        foo.val = 10;

        Console.WriteLine("Calling CallFoo_WithConstraints on FooValue - expecting IFoo::Foo");
        Test.Assert(CallFoo_WithConstraints(ref foo, 10) == 20, "Calling CallFoo_WithConstraints on FooValue");

        Test.Assert(foo.val == 10, "Expecting boxing on CallFoo_WithConstraints");
    }
}

class Program
{
    public static int Main()
    {
        HorribleTest.RunTest();
        SimpleConstraintTest.RunTest();

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

