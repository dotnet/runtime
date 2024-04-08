// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Xunit;

public class TestSmallStackArgsClass
{
    public TestSmallStackArgsClass()
    { }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int TestSmallStackArgsMethod(short s1, short s2, short s3, short s4, short s5, short s6, short s7, short s8, short s9, short s10, short s11, short s12)
    {
        if (s1 != 1 || s2 != 2 || s3 != 3 || s4 != 4 || s5 != 5 || s6 != 6 || s7 != 7 || s8 != 8 || s9 != 9 || s10 != 10 || s11 != 11 || s12 != 12)
        {
            Console.WriteLine("small stack arguments do not match.");
            Console.WriteLine("Expected: 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12");
            Console.WriteLine("Actual: " + s1 + ", " + s2 + ", " + s3 + ", " + s4 + ", " + s5 + ", " + s6 + ", " + s7 + ", " + s8 + ", " + s9 + ", " + s10 + ", " + s11 + ", " + s12);
            return 101;
        }
        return 100;
    }
}

public class TestMethodInfo
{

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int InvokeReflection(object testClassObject, MethodInfo testMethod, object[] args)
    {

        object retValue = testMethod.Invoke(testClassObject, args);
        int r = Convert.ToInt32(retValue);
        return r;
    }


    [Fact]
    public static int TestEntryPoint()
    {
        Type testClass = Type.GetType("TestSmallStackArgsClass");
        ConstructorInfo testConstructor = testClass.GetConstructor(Type.EmptyTypes);
        object testClassObject = testConstructor.Invoke(new object[] { });

        MethodInfo testMethod = testClass.GetMethod("TestSmallStackArgsMethod");
        var args = new object[12];
        for (short i = 0; i < 12; ++i)
        {
            args[i] = (short)(i + 1);
        }

        int r = InvokeReflection(testClassObject, testMethod, args);

        if (r != 100)
        {
            Console.WriteLine("The test failed");
            return 101;
        }
        Console.WriteLine("The test passed");
        return 100;
    }
}
