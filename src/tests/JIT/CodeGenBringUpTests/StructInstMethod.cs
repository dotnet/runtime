// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;

public struct Point
{
    public int x;
    public int y;
    public int z;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public Point(int a, int b, int c) { x = a; y = b; z = c; }

    public int X
    {
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        get { return this.x; }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        set { this.x = value; }
    }

    public int Y
    {
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        get { return this.y; }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        set { this.y = value; }
    }

    public int Z
    {
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        get { return this.z; }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        set { this.z = value; }
    }

    // Returns true if this represents 'origin' otherwise false.
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public bool StructInstMethod() { return (x == 0 && y == 0 && z == 0); }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public int StructInstMethod(ref Point p)
    {
        // JBToDo:The following is running into the assert put in place
        // for the case reg1 = reg2 op reg1 where op is not commutative.
        //int a = x-p.x;
        //int b = y-p.y;
        //int c = z-p.z;
        // return a+b+c;

        // Accessing field using get property
        int a = X;
        return a + p.x;
    }
}

public class BringUpTest_StructInstMethod
{
    const int Pass = 100;
    const int Fail = -1;


    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int StructInstMethod(ref Point p2)
    {
        Point p1 = new Point(10, 20, 30);

        p1.StructInstMethod();

        if (p1.StructInstMethod()) return Fail;
        if (!p2.StructInstMethod()) return Fail;

        int a = p1.StructInstMethod(ref p2);
        int b = p1.X;
        if (a != b) return Fail;

        return Pass;
    }


    [Fact]
    public static int TestEntryPoint()
    {
        Point p = new Point(10, 20, 30);
        if (p.StructInstMethod()) return Fail;

        if (p.StructInstMethod(ref p) != 20) return Fail;


        Point p2 = new Point(0, 0, 0);
        return StructInstMethod(ref p2);
    }
}
