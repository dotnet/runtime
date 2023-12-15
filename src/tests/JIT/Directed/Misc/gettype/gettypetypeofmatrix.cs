// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

internal class Foo
{
}

public class Test_gettypetypeofmatrix
{
    private static object s_null = null;
    private static object s_object = new object();
    private static object[] s_objectArray = new object[10];
    private static Foo s_foo = new Foo();
    private static Foo[] s_fooArray = new Foo[10];

    [Fact]
    static public int TestEntryPoint()
    {
        int iReturn = 100;
        try
        {
            IsObjectType(s_object, true);
            IsObjectType(s_objectArray, false);
            IsObjectType(s_foo, false);
            IsObjectType(s_fooArray, false);

            IsObjectArrayType(s_object, false);
            IsObjectArrayType(s_objectArray, true);
            IsObjectArrayType(s_foo, false);
            IsObjectArrayType(s_fooArray, false);

            IsFooType(s_object, false);
            IsFooType(s_objectArray, false);
            IsFooType(s_foo, true);
            IsFooType(s_fooArray, false);

            IsFooArrayType(s_object, false);
            IsFooArrayType(s_objectArray, false);
            IsFooArrayType(s_foo, false);
            IsFooArrayType(s_fooArray, true);

            IsObjectTypeNullRef(s_null);
            IsObjectArrayTypeNullRef(s_null);
            IsFooTypeNullRef(s_null);
            IsFooArrayTypeNullRef(s_null);

            Console.WriteLine("\nPassed all tests.");
        }
        catch (Exception e)
        {
            Console.WriteLine("Failed \n{0}", e.StackTrace);

            iReturn = 666;
        }

        return iReturn;
    }

    private static void IsResultCorrect(bool result, bool baseline)
    {
        if (result != baseline)
        {
            throw new Exception("Failed test");
        }
        else
        {
            Console.WriteLine("Passed");
        }
    }


    private static void IsObjectTypeNullRef(object o)
    {
        Console.Write("Test: {0} == typeof(object) Expected: null ref exception...", o == null ? "null" : o.ToString());
        try
        {
            if (o.GetType() == typeof(object) ||
                o.GetType() != typeof(object))
            {
                throw new Exception("Failed test");
            }
        }
        catch (NullReferenceException)
        {
            Console.WriteLine("Passed");
        }
        catch (Exception)
        {
            throw new Exception("Failed test");
        }
    }

    private static void IsObjectArrayTypeNullRef(object o)
    {
        Console.Write("Test: {0} == typeof(object[]) Expected: null ref exception...", o == null ? "null" : o.ToString());
        try
        {
            if (o.GetType() == typeof(object[]) ||
                o.GetType() != typeof(object[]))
            {
                throw new Exception("Failed test");
            }
        }
        catch (NullReferenceException)
        {
            Console.WriteLine("Passed");
        }
        catch (Exception)
        {
            throw new Exception("Failed test");
        }
    }

    private static void IsFooTypeNullRef(object o)
    {
        Console.Write("Test: {0} == typeof(Foo) Expected: null ref exception...", o == null ? "null" : o.ToString());
        try
        {
            if (o.GetType() == typeof(Foo) ||
                o.GetType() != typeof(Foo))
            {
                throw new Exception("Failed test");
            }
        }
        catch (NullReferenceException)
        {
            Console.WriteLine("Passed");
        }
        catch (Exception)
        {
            throw new Exception("Failed test");
        }
    }

    private static void IsFooArrayTypeNullRef(object o)
    {
        Console.Write("Test: {0} == typeof(Foo[]) Expected: null ref exception...", o == null ? "null" : o.ToString());
        try
        {
            if (o.GetType() == typeof(Foo[]) ||
                o.GetType() != typeof(Foo[]))
            {
                throw new Exception("Failed test");
            }
        }
        catch (NullReferenceException)
        {
            Console.WriteLine("Passed");
        }
        catch (Exception)
        {
            throw new Exception("Failed test");
        }
    }

    private static void IsObjectType(object o, bool baseline)
    {
        Console.Write("Test: o_{0}.GetType() == typeof(object) Expected: {1}...", o.GetType(), baseline);
        IsResultCorrect(
            o.GetType() == typeof(object),
            baseline);
    }

    private static void IsObjectArrayType(object o, bool baseline)
    {
        Console.Write("Test: o_{0}.GetType() == typeof(object[]) Expected: {1}...", o.GetType(), baseline);
        IsResultCorrect(
            o.GetType() == typeof(object[]),
            baseline);
    }

    private static void IsFooType(object o, bool baseline)
    {
        Console.Write("Test: o_{0}.GetType() == typeof(Foo) Expected: {1}...", o.GetType(), baseline);
        IsResultCorrect(
            o.GetType() == typeof(Foo),
            baseline);
    }

    private static void IsFooArrayType(object o, bool baseline)
    {
        Console.Write("Test: o_{0}.GetType() == typeof(Foo[]) Expected: {1}...", o.GetType(), baseline);
        IsResultCorrect(
            o.GetType() == typeof(Foo[]),
            baseline);
    }
}
