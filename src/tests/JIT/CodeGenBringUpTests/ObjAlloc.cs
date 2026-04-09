// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Point
{
  int x;
  int y;

  [MethodImplAttribute(MethodImplOptions.NoInlining)]
  public Point(int a, int b) { x=a; y = b; }

  [MethodImplAttribute(MethodImplOptions.NoInlining)]
  public bool IsOrigin() { return (x==0 && y == 0); }

  [MethodImplAttribute(MethodImplOptions.NoInlining)]
  public int DistanceSquared(Point p) { return (x-p.x)*(x-p.x) + (y-p.y)*(y-p.y); }
}

public class BringUpTest_ObjAlloc
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static Point ObjAlloc()
    {
       Point p1 = new Point(10,20);
       Point p2 = new Point(10,20);

       int d = p1.DistanceSquared(p2);
       if (d != 0) return null;
        
       return new Point(0,0);
    }


    [Fact]
    public static int TestEntryPoint()
    {
        Point obj = ObjAlloc();
        if (obj == null) return Fail;
        return Pass;
    }
}
