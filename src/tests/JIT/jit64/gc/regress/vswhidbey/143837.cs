// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public class Test_143837
{
    public static TestClass static_TestClass;
    public static DerivedClass static_DerivedClass;

    [Fact]
    public static int TestEntryPoint()
    {
        if (Test1() != 100) return 1;

        if (Test2() != 100) return 1;

        Console.WriteLine("Pass");
        return 100;
    }

    public static int Test1()
    {
        try
        {
            // trying to load a field on an static object
            static_TestClass.strField = "Test";
        }
        catch (NullReferenceException e)
        {
            Console.WriteLine("caught expected exception " + e.GetType());
            // trying to load a property on an static object
            try
            {
                Console.WriteLine(static_TestClass.strProperty);
            }
            catch (NullReferenceException e1)
            {
                Console.WriteLine("caught expected exception " + e1.GetType());
                try
                {
                    static_TestClass.strProperty = "abcd";
                }
                catch (NullReferenceException e2)
                {
                    Console.WriteLine("caught expected exception " + e2.GetType());
                    return 100;
                }
            }
        }
        Console.WriteLine("Error");
        return -1;
    }

    public static int Test2()
    {
        try
        {
            // trying to load a field on an static object
            static_DerivedClass.strField = "Test";
        }
        catch (NullReferenceException e)
        {
            Console.WriteLine("caught expected exception " + e.GetType());

            // trying to load a property on an static object
            try
            {
                Console.WriteLine(static_DerivedClass.strProperty);
            }
            catch (NullReferenceException e1)
            {
                Console.WriteLine("caught expected exception " + e1.GetType());
                try
                {
                    static_DerivedClass.strProperty = "abcd";
                }
                catch (NullReferenceException e2)
                {
                    Console.WriteLine("caught expected exception " + e2.GetType());
                    return 100;
                }
            }
        }
        Console.WriteLine("Error");
        return -1;
    }
};

public class TestClass
{
    public string strField;
    public TestClass()
    {
    }
    public TestClass(String strIn)
    {
        strField = strIn;
    }
    public virtual string strProperty
    {
        get
        {
            return strField;
        }
        set
        {
            strField = value;
        }
    }
}

public class DerivedClass : TestClass
{
    public DerivedClass()
    {
    }
    public override string strProperty
    {
        get
        {
            return strField;
        }
        set
        {
            strField = value;
        }
    }
}


