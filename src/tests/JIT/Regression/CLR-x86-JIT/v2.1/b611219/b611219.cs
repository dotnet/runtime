// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//A constrained virtual call to an intrinsic returns incorrect value.
//On x86 the returned value is off by one level of indirection so the address of the string length is
//returned instead of the actual length.  On x64 the intrinsic returns 0 instead of string length.
//Only the String intrinsics seem to be affected by this bug as the array intrinsics aren't used with
//generics.
//Test returns 100 on success and 1 on failure.

using System;
using Xunit;

public abstract class Base<U>
{
    public abstract int Foo<T>(T obj) where T : U;
}

public class Derived : Base<string>
{
    public override int Foo<T>(T obj)
    {
        int n = obj.Length;
        Console.WriteLine("obj.Length={0}", n);
        Console.WriteLine("obj={0}", obj);
        return n;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int ret = 100;
        string s = "abc";
        Derived d = new Derived();
        int len = d.Foo(s);
        if (len != s.Length)
        {
            Console.WriteLine("FAIL: Length returned {0}", len);
            ret = 1;
        }
        else
        {
            Console.WriteLine("Pass");
        }
        return ret;
    }
}
