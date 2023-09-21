// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

// Use multiple classes to trigger multiple statics allocation events
public class StaticTest2
{
    [ThreadStatic]
    public static int ThreadStaticValue;
    public static int StaticValue;
}

public class StaticTest3
{
    public static int StaticValue;
}

public class StaticTest4
{
    [ThreadStatic]
    public static object ThreadStaticValue;
    public static object StaticValue;
}
public class StaticTest5
{
    [ThreadStatic]
    public static object ThreadStaticValue;
    public static object StaticValue;
}
public class StaticTest6
{
    public static object StaticValue;
}


public class StaticTest : IStaticTest
{
    [ThreadStatic]
    public static int ThreadStaticValue;
    public static int StaticValue;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void SetValues(out int valTargetA, int valA
                                , out int valTargetB, int valB
                                , out int valTargetC, int valC
                                , out int valTargetD, int valD
                                , out int valTargetE, int valE
                                )
    {
        valTargetA = valA;
        valTargetB = valB;
        valTargetC = valC;
        valTargetD = valD;
        valTargetE = valE;
    }

    public void SetStatic(int val, int val2, int val3, int val4, int val5)
    {
        // Use this odd pathway to increase the chance that in the presence of GCStress issues will be found
        SetValues(out ThreadStaticValue, val
                , out StaticValue, val2
                , out StaticTest2.StaticValue, val3
                , out StaticTest2.ThreadStaticValue, val4
                , out StaticTest3.StaticValue, val5
                );
    }

    public void GetStatic(out int val1, out int val2, out int val3, out int val4, out int val5)
    {
        val1 = ThreadStaticValue;
        val2 = StaticValue;
        val3 = StaticTest2.StaticValue;
        val4 = StaticTest2.ThreadStaticValue;
        val5 = StaticTest3.StaticValue;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void SetValuesObject(out object valTargetA, object valA
                                , out object valTargetB, object valB
                                , out object valTargetC, object valC
                                , out object valTargetD, object valD
                                , out object valTargetE, object valE
                                )
    {
        valTargetA = valA;
        valTargetB = valB;
        valTargetC = valC;
        valTargetD = valD;
        valTargetE = valE;
    }

    public void SetStaticObject(object val, object val2, object val3, object val4, object val5)
    {
        // Use this odd pathway to increase the chance that in the presence of GCStress issues will be found
        SetValuesObject(out StaticTest4.ThreadStaticValue, val
                , out StaticTest4.StaticValue, val2
                , out StaticTest5.StaticValue, val3
                , out StaticTest5.ThreadStaticValue, val4
                , out StaticTest6.StaticValue, val5
                );
    }

    public void GetStaticObject(out object val1, out object val2, out object val3, out object val4, out object val5)
    {
        val1 = StaticTest4.ThreadStaticValue;
        val2 = StaticTest4.StaticValue;
        val3 = StaticTest5.StaticValue;
        val4 = StaticTest5.ThreadStaticValue;
        val5 = StaticTest6.StaticValue;
    }
}
