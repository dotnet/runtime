// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// this test case contains various types with recursive constraints

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

using Xunit;

// Test 1: Base class with recursive constraint, derived class
public class Test1
{
    public class Base<T> where T: Base<T>
    {
        public List<Base<T>> selfList = new List<Base<T>>();
    }

    
    public class Derived : Base<Derived> {}

    public static void Test()
    {
        Derived d = new Derived();
        Base<Derived> bd = d;
    }
}   

// Test 2: Base interface with recursive constraint, derived class
public class Test2
{
    public interface I<T> where T: I<T>{}
    
    public class Derived : I<Derived> {}

    public static void Test()
    {
        Derived d = new Derived();
        I<Derived> id = d;
    }
}

// Test 3: Base interface with recursive constraint, derived struct
public class Test3
{
    public interface I<T> where T: I<T>{}
    
    public struct Derived : I<Derived> {}

    public static void Test()
    {
        #pragma warning disable 219
        Derived d = new Derived();
        I<Derived> id = d;
        #pragma warning restore 219
    }
}


// Test 4: Base class with recursive constraint, derived generic class
public class Test4
{
    public class Base<T> where T: Base<T>
    {
        public List<Base<T>> selfList = new List<Base<T>>();
    }

    
    public class Derived<T> : Base<Derived<T>> {}

    public static void Test()
    {
        Derived<Derived<int>> d = new Derived<Derived<int>>();
        Base<Derived<Derived<int>>> bdi = d;
    }
}   


// Test 5: Base interface with recursive constraint, derived generic class
public class Test5
{
    public interface I<T> where T: I<T>{}
    
    public class Derived<T> : I<Derived<T>> {}

    public static void Test()
    {
        Derived<Derived<int>> d = new Derived<Derived<int>>();
        I<Derived<Derived<int>>> idi = d;
    }
}

// Test 6: Base interface with recursive constraint, derived generic struct
public class Test6
{
    public interface I<T> where T: I<T>{}
    
    public struct Derived<T> : I<Derived<T>> {}

    public static void Test()
    {
        #pragma warning disable 219
        Derived<Derived<int>> d = new Derived<Derived<int>>();
        I<Derived<Derived<int>>> idi = d;
        #pragma warning restore 219
    }
}


// Test 7: Base interface with recursive constraint, derived generic interface, derived generic class
public class Test7
{
    public interface I1<T> where T: I1<T>{}
    
    public interface I2<T>{}
    
    public class Derived<T> : I2<Derived<T>> {}

    public static void Test()
    {
        Derived<Derived<int>> d = new Derived<Derived<int>>();
        I2<Derived<Derived<int>>> idi = d;
    }
}

// Test 8: Base interface with recursive constraint, derived generic interface, derived generic struct
public class Test8
{
    public interface I1<T> where T: I1<T>{}
    
    public interface I2<T>{}
    
    public struct Derived<T> : I2<Derived<T>> {}

    public static void Test()
    {
        #pragma warning disable 219
        Derived<Derived<int>> d = new Derived<Derived<int>>();
        I2<Derived<Derived<int>>> idi = d;
        #pragma warning restore 219
    }
}


public class RecursiveConstraints
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

        Check(new Case(Test1.Test), "Test 1: Base class with recursive constraint, derived class  : ");
        Check(new Case(Test2.Test), "Test 2: Base interface with recursive constraint, derived class : ");
        Check(new Case(Test3.Test), "Test 3: Base interface with recursive constraint, derived struct : ");
        Check(new Case(Test4.Test), "Test 4: Base class with recursive constraint, derived generic class : ");
        Check(new Case(Test5.Test), "Test 5: Base interface with recursive constraint, derived generic class : ");
        Check(new Case(Test6.Test), "Test 6: Base interface with recursive constraint, derived generic struct : ");
        Check(new Case(Test7.Test), "Test 7: Base interface with recursive constraint, derived generic interface, derived generic class : ");
        Check(new Case(Test8.Test), "Test 8: Base interface with recursive constraint, derived generic interface, derived generic struct : ");
        
        Assert.True(pass);
    }
}
