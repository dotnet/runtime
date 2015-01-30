// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using System;
using System.Runtime.CompilerServices;

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

public class BringUpTest
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


    public static int Main()
    {
        Point obj = ObjAlloc();
        if (obj == null) return Fail;
        return Pass;
    }
}
