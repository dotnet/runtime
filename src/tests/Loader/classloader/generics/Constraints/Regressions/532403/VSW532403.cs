// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// this test is regression test for VSW 532403.
// the first test is directly from the bug. The rest of the tests were 
// added to add better coverage for this area where base type has a special
// constraint and the child has recursion in inheritance.

using System;
using Xunit;

public class Test1
{
    public class Base<T> where T : new()
    {
    }
    public class Derived<T> : Base<Derived<T>>
    {
    }
 
    public static void Test()
    {
        Derived<int> m = new Derived<int>();
    Base<Derived<int>> m2 = new Derived<int>();
    }
}

public class Test2
{
    public class Base<T> where T : class
    {
    }
    public class Derived<T> : Base<Derived<T>>
    {
    }
 
    public static void Test()
    {
        Derived<int> m = new Derived<int>();
    Base<Derived<int>> m2 = new Derived<int>();
    }
}

public class Test3
{
        public interface Base<T> where T : struct
        {
        }
        public struct Derived<T> : Base<Derived<T>>
        {
        }
 
        public static void Test()
    {
        #pragma warning disable 219
            Derived<int> m = new Derived<int>();
        Base<Derived<int>> m2 = new Derived<int>();
        #pragma warning restore 219
        }
}

public class Test4
{
    public class Base<T> where T : class, new()
    {
    }
    public class Derived<T> : Base<Derived<T>>
    {
    }
 
    public static void Test()
    {
        Derived<int> m = new Derived<int>();
    Base<Derived<int>> m2 = new Derived<int>();
    }
}

public class RunTests
{

    static bool pass;

    delegate void Case();
       
    static void Check(Case mytest, string testName)
        {

        Console.Write(testName);

        try
        {
            mytest();
            
            Console.WriteLine("PASS");
        }
        catch (Exception e) 
        {
            Console.WriteLine("FAIL: Caught unexpected exception: " + e);
            pass = false;
        }

    }

    [Fact]
    public static void TestEntryPoint()
    {
        pass = true;

        Check(new Case(Test1.Test), "Test 1: Base class with new() constraint  : ");
        Check(new Case(Test2.Test), "Test 2: Base class with class constraint  : ");
        Check(new Case(Test3.Test), "Test 3: Base class with struct constraint  : ");
        Check(new Case(Test4.Test), "Test 4: Base class with class and new() constraints : ");

        Assert.True(pass);
    }
}
