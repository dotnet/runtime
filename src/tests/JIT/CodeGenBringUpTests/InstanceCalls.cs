// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;

public class MiscMethods
{
    private int x;
    private int y;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public MiscMethods(int a, int b) {x=a; y=b;}

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public int Sum(int[] a)
    {
        int s = 0;
        for (int i = 0; i < a.Length; ++i)
            s += a[i];
        return s;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public float Sum(float[] a)
    {
        float s = 0;
        for (int i = 0; i < a.Length; ++i)
            s += a[i];
        return s;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public double Sum(double[] a)
    {
        double s = 0;
        for (int i = 0; i < a.Length; ++i)
            s += a[i];
        return s;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public int Sum(int a, int b, int c, int d, int e)
    {
        return a + b + c + d + e;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public float Sum(float a, float b, float c, float d, float e)
    {
        return a + b + c + d + e;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public double Sum(double a, double b, double c, double d, double e)
    {
        return a + b + c + d + e;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public void Print(int s)
    {
        Console.WriteLine(s);
    }
 
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public bool InstanceCalls(MiscMethods m)    
    {
       return x == m.x && y == m.y;
    }
}

public class BringUpTest_InstanceCalls
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool floatEqual(float x, float y)     
    {
        return System.Math.Abs(x-y) <= Single.Epsilon;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool doubleEqual(double x, double y)     
    {
        return System.Math.Abs(x-y) <= Double.Epsilon;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int InstanceCalls(MiscMethods m, int[] a)
    {
        int result = Pass;

        int s = m.Sum(a);
        if (s != 15) result = Fail;

        s = m.Sum(1, 2, 3, 4, 5);
        if (s != 15) result = Fail;

        return result;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int InstanceCalls(MiscMethods m, float[] a)
    {
        int result = Pass;

        float s = m.Sum(a);
        if (!floatEqual(s, 15f)) result = Fail;

        s = m.Sum(1f, 2f, 3f, 4f, 5f);
        if (!floatEqual(s, 15f)) result = Fail;

        return result;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int InstanceCalls(MiscMethods m, double[] a, double v1, double v2, double v3, double v4, double v5)
    {
        int result = Pass;

        double s1 = m.Sum(a);
        double s2 = m.Sum(v1, v2, v3, v4, v5);
        if (!doubleEqual(s1, s2)) result = Fail;

        return result;
    }


    [Fact]
    public static int TestEntryPoint()
    {
        MiscMethods m = new MiscMethods(10,20);
        if (!m.InstanceCalls(m)) return Fail;

        int[] a = new int[5] { 1, 2, 3, 4, 5 };
        int x = InstanceCalls(m,a);

        float[] b = new float[5] { 1f, 2f, 3f, 4f, 5f };
        int y = InstanceCalls(m,b);

        double[] c = new double[5] { 1d, 2d, 3d, 4d, 5d };
        int z = InstanceCalls(m,c, 1d, 2d, 3d, 4d, 5d);

        if (x == Pass && y == Pass && z == Pass)
           return Pass;
        return Fail;        
    }
}
