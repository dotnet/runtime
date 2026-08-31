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
  public Point(int a, int b, int c) { x=a; y = b; z=c;}

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

public struct PointFlt
{
    public float x;
    public float y;
    public float z;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public PointFlt(float a, float b, float c) { x = a; y = b; z = c; }

    public float X
    {
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        get { return this.x; }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        set { this.x = value; }
    }

    public float Y
    {
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        get { return this.y; }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        set { this.y = value; }
    }

    public float Z
    {
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        get { return this.z; }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        set { this.z = value; }
    }

    // Returns true if this represents 'origin' otherwise false.
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public bool StructInstMethod() { return (Math.Abs(x) <= Single.Epsilon && Math.Abs(y) <= Single.Epsilon && Math.Abs(z) <= Single.Epsilon); }

}

public struct PointDbl
{
    public double x;
    public double y;
    public double z;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public PointDbl(double a, double b, double c) { x = a; y = b; z = c; }

    public double X
    {
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        get { return this.x; }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        set { this.x = value; }
    }

    public double Y
    {
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        get { return this.y; }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        set { this.y = value; }
    }

    public double Z
    {
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        get { return this.z; }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        set { this.z = value; }
    }

    // Returns true if this represents 'origin' otherwise false.
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public bool StructInstMethod() { return (Math.Abs(x) <= Double.Epsilon && Math.Abs(y) <= Double.Epsilon && Math.Abs(z) <= Double.Epsilon); }

}

public class BringUpTest_OpMembersOfStructLocal
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool equals(float a, float b)
    {
        return Math.Abs(a - b) <= Single.Epsilon;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool equals(double a, double b)
    {
        return Math.Abs(a - b) <= Double.Epsilon;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int OpMembersOfStructLocal()
    {
       Point p1 = new Point(10,20,30);
       Point p2;
       Point p3;

       p2.x = 0;
       p2.y = 0;
       p2.z = 0;


       p3.x = p1.x + 5;
       p3.y = p1.y + p2.y;
       p3.z = p1.z * p2.z;

       bool f = (p3.x == 15) && (p3.y == 20) && (p3.z == 0);
       if (!f) return Fail;

       int a = p1.x + p2.x;
       int b = p1.y * p2.y;
       int c = p1.z;

 
       f = (a==p1.x) && (b==p2.y);
       if (!f) return Fail;

       a += p1.x;
       if (20 != a) return Fail;
       b *= p1.y;      
       if (0 != b) return Fail;       
       c /= p1.x;       

       p1.x += 10;
       p1.y *= 3;
       p1.z /= 5;
       if (20 != p1.x) return Fail;
       if (60 != p1.y) return Fail;
       if (6 != p1.z) return Fail;

       PointFlt p1f = new PointFlt(10f, 20f, 30f);
       PointFlt p2f;
       PointFlt p3f;

       p2f.x = 0f;
       p2f.y = 0f;
       p2f.z = 0f;


       p3f.x = p1f.x + 5f;
       p3f.y = p1f.y + p2f.y;
       p3f.z = p1f.z * p2f.z;

       f = equals(p3f.x,15f) && equals(p3f.y,20f) && equals(p3f.z,0f);
       if (!f) return Fail;

       float af = p1f.x + p2f.x;
       float bf = p1f.y * p2f.y;
       float cf = p1f.z;


       f = equals(af, p1f.x) && equals(bf, p2f.y);
       if (!f) return Fail;

       af += p1f.x;
       if (!equals(20f,af)) return Fail;
       bf *= p1f.y;
       if (!equals(0f,bf)) return Fail;
       cf /= p1f.x;

       p1f.x += 10f;
       p1f.y *= 3f;
       p1f.z /= 5f;
       if (!equals(20f,p1f.x)) return Fail;
       if (!equals(60f,p1f.y)) return Fail;
       if (!equals(6f,p1f.z)) return Fail;

       PointDbl p1d = new PointDbl(10d, 20d, 30d);
       PointDbl p2d;
       PointDbl p3d;

       p2d.x = 0d;
       p2d.y = 0d;
       p2d.z = 0d;


       p3d.x = p1d.x + 5d;
       p3d.y = p1d.y + p2d.y;
       p3d.z = p1d.z * p2d.z;

       f = equals(p3d.x, 15d) && equals(p3d.y, 20d) && equals(p3d.z, 0d);
       if (!f) return Fail;

       double ad = p1d.x + p2d.x;
       double bd = p1d.y * p2d.y;
       double cd = p1d.z;


       f = equals(ad, p1d.x) && equals(bd, p2d.y);
       if (!f) return Fail;

       ad += p1d.x;
       if (!equals(20d, ad)) return Fail;
       bd *= p1d.y;
       if (!equals(0d, bd)) return Fail;
       cd /= p1d.x;

       p1d.x += 10d;
       p1d.y *= 3d;
       p1d.z /= 5d;
       if (!equals(20d, p1d.x)) return Fail;
       if (!equals(60d, p1d.y)) return Fail;
       if (!equals(6d, p1d.z)) return Fail; 

       return Pass;
    }
    

    [Fact]
    public static int TestEntryPoint()
    {       
       return OpMembersOfStructLocal();
    }
}
