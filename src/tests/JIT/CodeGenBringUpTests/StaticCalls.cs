// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;

public class BringUpTest_StaticCalls
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int Max(int a, int b)
    {
        int result = a > b ? a : b;
        return result;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool IsLessThan(int a, int b)
    {        
        return a<b;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool IsEqual(int a, int b)
    {        
        return !IsLessThan(a, b) && !IsLessThan(b, a);
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool IsEqual(float a, float b)
    {        
        return System.Math.Abs(a-b) <= Single.Epsilon;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool IsEqual(double a, double b)
    {        
        return System.Math.Abs(a-b) <= Double.Epsilon;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int Sum(int a, int b, int c, int d)
    {
        int result = a+b+c+d;
        return result;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static float Sum(float a, float b, float c, float d)
    {
        float result = a+b+c+d;
        return result;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static double Sum(double a, double b, double c, double d)
    {
        double result = a+b+c+d;
        return result;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int Sum(int a, int b, int c, int d, int e)
    {
        int result = a+b+c+d+e;
        return result;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int Sum(int a, int b, int c, int d, int e, int f)
    {
        int result = a+b+c+d+e+f;
        return result;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static float Sum(float a, float b, float c, float d, float e, float f)
    {
        float result = a+b+c+d+e+f;
        return result;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static double Sum(double a, double b, double c, double d, double e, double f)
    {
        double result = a+b+c+d+e+f;
        return result;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int StaticCalls()
    {
        int a = 1;
        int b = 2;
        int c = 3;
        int d = 4;
        int e = 5;
        int f = 6;
        int result = Pass;

        int s = Sum(1,2,3,4);
        if (s != 10) result = Fail;
        
        s = Sum(1,2,3,4,5);
        if (s != 15) result = Fail;

        s = Sum(1,2,3,4,5,6);
        if (s != 21) result = Fail;

        s = Sum(a,b,c,d);
        if (s != 10) result = Fail;

        s = Sum(a,b,c,d,e);
        if (s != 15) result = Fail;

        s = Sum(a,b,c,d,e,f);
        if (s != 21) result = Fail;

        s = Max(b,f);
        if (s != f) result = Fail;


        bool equal = IsEqual(d, d);
        if (!equal) result = Fail;


        float f1 = 1f;
        float f2 = 2f;
        float f3 = 3f;
        float f4 = 4f;
        float f5 = 5f;
        float f6 = 6f;
        float fsum = Sum(1f,2f,3f,4f);
        if (!IsEqual(fsum, 10f)) result = Fail;

        fsum = Sum(1f, 2f, 3f, 4f, 5f, 6f);
        if (!IsEqual(fsum, 21f)) result = Fail;                 

        fsum = Sum(f1,f2,f3,f4);
        if (!IsEqual(fsum, 10f)) result = Fail;

        fsum = Sum(f1,f2,f3,f4, f5, f6);
        if (!IsEqual(fsum, 21f)) result = Fail;


        double d1 = 1d;
        double d2 = 2d;
        double d3 = 3d;
        double d4 = 4d;
        double d5 = 5d;
        double d6 = 6d;
        double dsum = Sum(1d,2d,3d,4d);
        if (!IsEqual(dsum, 10d)) result = Fail;

        dsum = Sum(1d, 2d, 3d, 4d, 5d, 6d);
        if (!IsEqual(dsum, 21d)) result = Fail;                 

        dsum = Sum(d1,d2,d3,d4);
        if (!IsEqual(dsum, 10d)) result = Fail;

        dsum = Sum(d1,d2,d3,d4,d5,d6);
        if (!IsEqual(dsum, 21d)) result = Fail;

        Console.WriteLine(result);
        return result;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int y = StaticCalls();      
        return y;        
    }
}
