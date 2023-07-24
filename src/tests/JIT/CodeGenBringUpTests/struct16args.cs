// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;

public struct Point
{
  public int w;
  public int x;
  public int y;
  public int z;

  [MethodImplAttribute(MethodImplOptions.NoInlining)]
  public Point(int a, int b, int c, int d) { w=a; x=a; y=b; z=d; }

  public int W
  {
     [MethodImplAttribute(MethodImplOptions.NoInlining)]
     get { return this.w; }

     [MethodImplAttribute(MethodImplOptions.NoInlining)]
     set { this.w = value; }
  }

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
  public bool StructInstMethod() { return (x==0 && y == 0 && z==0); }

}

public class BringUpTest_struct16args
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int method_4S(Point p0, Point p1, Point p2, Point p3)
    {
        Console.Write("method_4S");
    
        if (p0.W != 0)
            return Fail;

        if (p0.X != 0)
            return Fail;

        if (p0.Y != 0)
            return Fail;

        if (p0.Z != 0)
            return Fail;

        if (p1.W != 1)
            return Fail;

        if (p1.X != 1)
            return Fail;

        if (p1.Y != 1)
            return Fail;

        if (p1.Z != 1)
            return Fail;

        if (p2.W != 9)
            return Fail;

        if (p2.X != 99)
            return Fail;

        if (p2.Y != 999)
            return Fail;

        if (p2.Z != 9999)
            return Fail;

        if (p3.W != 10)
            return Fail;

        if (p3.X != 100)
            return Fail;

        if (p3.Y != 1000)
            return Fail;

        if (p3.Z != 10000)
            return Fail;

        Console.WriteLine(" Pass");

        return Pass;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int method_4S4I(Point p0, Point p1, Point p2, Point p3, int i0, int i1, int i2, int i3)
    {
        Console.Write("method_4S4I");

        if (i0 != 2)
            return Fail;

        if (i1 != 3)
            return Fail;

        if (i2 != 5)
            return Fail;

        if (i3 != 7)
            return Fail;

        if (p0.W != 0)
            return Fail;

        if (p0.X != 0)
            return Fail;

        if (p0.Y != 0)
            return Fail;

        if (p0.Z != 0)
            return Fail;

        if (p1.W != 1)
            return Fail;

        if (p1.X != 1)
            return Fail;

        if (p1.Y != 1)
            return Fail;

        if (p1.Z != 1)
            return Fail;

        if (p2.W != 9)
            return Fail;

        if (p2.X != 99)
            return Fail;

        if (p2.Y != 999)
            return Fail;

        if (p2.Z != 9999)
            return Fail;

        if (p3.W != 10)
            return Fail;

        if (p3.X != 100)
            return Fail;

        if (p3.Y != 1000)
            return Fail;

        if (p3.Z != 10000)
            return Fail;

        Console.WriteLine(" Pass");

        return Pass;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int method_4I4S(int i0, int i1, int i2, int i3, Point p0, Point p1, Point p2, Point p3)
    {
        Console.Write("method_4I4S");

        if (i0 != 2)
            return Fail;

        if (i1 != 3)
            return Fail;

        if (i2 != 5)
            return Fail;

        if (i3 != 7)
            return Fail;

        if (p0.W != 0)
            return Fail;

        if (p0.X != 0)
            return Fail;

        if (p0.Y != 0)
            return Fail;

        if (p0.Z != 0)
            return Fail;

        if (p1.W != 1)
            return Fail;

        if (p1.X != 1)
            return Fail;

        if (p1.Y != 1)
            return Fail;

        if (p1.Z != 1)
            return Fail;

        if (p2.W != 9)
            return Fail;

        if (p2.X != 99)
            return Fail;

        if (p2.Y != 999)
            return Fail;

        if (p2.Z != 9999)
            return Fail;

        if (p3.W != 10)
            return Fail;

        if (p3.X != 100)
            return Fail;

        if (p3.Y != 1000)
            return Fail;

        if (p3.Z != 10000)
            return Fail;

        Console.WriteLine(" Pass");

        return Pass;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int method_1I4S(int i0, Point p0, Point p1, Point p2, Point p3)
    {
        Console.Write("method_1I4S");

        if (i0 != 2)
            return Fail;

        if (p0.W != 0)
            return Fail;

        if (p0.X != 0)
            return Fail;

        if (p0.Y != 0)
            return Fail;

        if (p0.Z != 0)
            return Fail;

        if (p1.W != 1)
            return Fail;

        if (p1.X != 1)
            return Fail;

        if (p1.Y != 1)
            return Fail;

        if (p1.Z != 1)
            return Fail;

        if (p2.W != 9)
            return Fail;

        if (p2.X != 99)
            return Fail;

        if (p2.Y != 999)
            return Fail;

        if (p2.Z != 9999)
            return Fail;

        if (p3.W != 10)
            return Fail;

        if (p3.X != 100)
            return Fail;

        if (p3.Y != 1000)
            return Fail;

        if (p3.Z != 10000)
            return Fail;

        Console.WriteLine(" Pass");

        return Pass;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int method_2I4S(int i0, int i1, Point p0, Point p1, Point p2, Point p3)
    {
        Console.Write("method_2I4S");

        if (i0 != 2)
            return Fail;

        if (i1 != 3)
            return Fail;

        if (p0.W != 0)
            return Fail;

        if (p0.X != 0)
            return Fail;

        if (p0.Y != 0)
            return Fail;

        if (p0.Z != 0)
            return Fail;

        if (p1.W != 1)
            return Fail;

        if (p1.X != 1)
            return Fail;

        if (p1.Y != 1)
            return Fail;

        if (p1.Z != 1)
            return Fail;

        if (p2.W != 9)
            return Fail;

        if (p2.X != 99)
            return Fail;

        if (p2.Y != 999)
            return Fail;

        if (p2.Z != 9999)
            return Fail;

        if (p3.W != 10)
            return Fail;

        if (p3.X != 100)
            return Fail;

        if (p3.Y != 1000)
            return Fail;

        if (p3.Z != 10000)
            return Fail;

        Console.WriteLine(" Pass");

        return Pass;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int method_3I4S(int i0, int i1, int i2, Point p0, Point p1, Point p2, Point p3)
    {
        Console.Write("method_3I4S");

        if (i0 != 2)
            return Fail;

        if (i1 != 3)
            return Fail;

        if (i2 != 5)
            return Fail;

        if (p0.W != 0)
            return Fail;

        if (p0.X != 0)
            return Fail;

        if (p0.Y != 0)
            return Fail;

        if (p0.Z != 0)
            return Fail;

        if (p1.W != 1)
            return Fail;

        if (p1.X != 1)
            return Fail;

        if (p1.Y != 1)
            return Fail;

        if (p1.Z != 1)
            return Fail;

        if (p2.W != 9)
            return Fail;

        if (p2.X != 99)
            return Fail;

        if (p2.Y != 999)
            return Fail;

        if (p2.Z != 9999)
            return Fail;

        if (p3.W != 10)
            return Fail;

        if (p3.X != 100)
            return Fail;

        if (p3.Y != 1000)
            return Fail;

        if (p3.Z != 10000)
            return Fail;

        Console.WriteLine(" Pass");

        return Pass;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int method_2I4S2D(int i0, int i1, Point p0, Point p1, Point p2, Point p3, double d0, double d1)
    {
        Console.Write("method_2I4S2D");

        if (i0 != 2)
            return Fail;

        if (i1 != 3)
            return Fail;

        if (d0 != 11.0d)
            return Fail;

        if (d1 != 13.0d)
            return Fail;

        if (p0.W != 0)
            return Fail;

        if (p0.X != 0)
            return Fail;

        if (p0.Y != 0)
            return Fail;

        if (p0.Z != 0)
            return Fail;

        if (p1.W != 1)
            return Fail;

        if (p1.X != 1)
            return Fail;

        if (p1.Y != 1)
            return Fail;

        if (p1.Z != 1)
            return Fail;

        if (p2.W != 9)
            return Fail;

        if (p2.X != 99)
            return Fail;

        if (p2.Y != 999)
            return Fail;

        if (p2.Z != 9999)
            return Fail;

        if (p3.W != 10)
            return Fail;

        if (p3.X != 100)
            return Fail;

        if (p3.Y != 1000)
            return Fail;

        if (p3.Z != 10000)
            return Fail;

        Console.WriteLine(" Pass");

        return Pass;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int method_2I2D4S(int i0, int i1, double d0, double d1, Point p0, Point p1, Point p2, Point p3)
    {
        Console.Write("method_2I2D4S");

        if (i0 != 2)
            return Fail;

        if (i1 != 3)
            return Fail;

        if (d0 != 11.0d)
            return Fail;

        if (d1 != 13.0d)
            return Fail;

        if (p0.W != 0)
            return Fail;

        if (p0.X != 0)
            return Fail;

        if (p0.Y != 0)
            return Fail;

        if (p0.Z != 0)
            return Fail;

        if (p1.W != 1)
            return Fail;

        if (p1.X != 1)
            return Fail;

        if (p1.Y != 1)
            return Fail;

        if (p1.Z != 1)
            return Fail;

        if (p2.W != 9)
            return Fail;

        if (p2.X != 99)
            return Fail;

        if (p2.Y != 999)
            return Fail;

        if (p2.Z != 9999)
            return Fail;

        if (p3.W != 10)
            return Fail;

        if (p3.X != 100)
            return Fail;

        if (p3.Y != 1000)
            return Fail;

        if (p3.Z != 10000)
            return Fail;

        Console.WriteLine(" Pass");

        return Pass;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int method_2I2D4S2D(int i0, int i1, double d0, double d1, Point p0, Point p1, Point p2, Point p3, double d2, double d3)
    {
        Console.Write("method_2I2D4S2D");

        if (i0 != 2)
            return Fail;

        if (i1 != 3)
            return Fail;

        if (d0 != 11.0d)
            return Fail;

        if (d1 != 13.0d)
            return Fail;

        if (d2 != 15.0d)
            return Fail;

        if (d3 != 17.0d)
            return Fail;

        if (p0.W != 0)
            return Fail;

        if (p0.X != 0)
            return Fail;

        if (p0.Y != 0)
            return Fail;

        if (p0.Z != 0)
            return Fail;

        if (p1.W != 1)
            return Fail;

        if (p1.X != 1)
            return Fail;

        if (p1.Y != 1)
            return Fail;

        if (p1.Z != 1)
            return Fail;

        if (p2.W != 9)
            return Fail;

        if (p2.X != 99)
            return Fail;

        if (p2.Y != 999)
            return Fail;

        if (p2.Z != 9999)
            return Fail;

        if (p3.W != 10)
            return Fail;

        if (p3.X != 100)
            return Fail;

        if (p3.Y != 1000)
            return Fail;

        if (p3.Z != 10000)
            return Fail;

        Console.WriteLine(" Pass");

        return Pass;
    }

    [Fact]
    public static int TestEntryPoint()
    {       
       int i0 = 2;
       int i1 = 3;
       int i2 = 5;
       int i3 = 7;

       double d0 = 11.0d;
       double d1 = 13.0d;
       double d2 = 15.0d;
       double d3 = 17.0d;

       Point p0;
       Point p1;
       Point p2;
       Point p3;

       p0.w = 0;
       p0.x = 0;
       p0.y = 0;
       p0.z = 0;

       p1.w = 1;
       p1.x = 1;
       p1.y = 1;
       p1.z = 1;

       p2.w = 9;
       p2.x = 99;
       p2.y = 999;
       p2.z = 9999;

       p3.w = 10;
       p3.x = 100;
       p3.y = 1000;
       p3.z = 10000;

       if (method_4S(p0,p1,p2,p3) != Pass)
           return Fail;

       if (method_4S4I(p0,p1,p2,p3, i0,i1,i2,i3) != Pass)
           return Fail;

       if (method_4I4S(i0,i1,i2,i3, p0,p1,p2,p3) != Pass)
           return Fail;

       if (method_1I4S(i0, p0,p1,p2,p3) != Pass)
           return Fail;

       if (method_2I4S(i0,i1, p0,p1,p2,p3) != Pass)
           return Fail;

       if (method_3I4S(i0,i1,i2, p0,p1,p2,p3) != Pass)
           return Fail;

       if (method_2I4S2D(i0,i1, p0,p1,p2,p3, d0,d1) != Pass)
           return Fail;

       if (method_2I2D4S(i0,i1, d0,d1, p0,p1,p2,p3) != Pass)
           return Fail;

       if (method_2I2D4S2D(i0,i1, d0,d1, p0,p1,p2,p3, d2,d3) != Pass)
           return Fail;

       return Pass;
    }
}
