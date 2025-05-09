// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Xunit;

class C1
{
    public C1(int x) { a = x; b = x; }
    public int a;
    public int b;
}

struct S1
{
    public S1(C1 z) { c = z; }
    public int a;
    public int b;
    public C1 c;
}

public class Runtime_111922
{
   [Fact]
   public static int Problem()
   {
       S1 s = new S1(new C1(4));
       return 95 + SubProblem(1, s);
   }

   [MethodImpl(MethodImplOptions.NoInlining)]
   static int SubProblem(int x, S1 s)
   {
       s = new S1(new C1(5));

       SideEffect();

       C1 v = s.c;
       return v.a;
   }

   [Fact]
   public static int Problem1()
   {
       S1 s = new S1(new C1(4));
       return 95 + SubProblem1(1, ref s);
   }

   [MethodImpl(MethodImplOptions.NoInlining)]
   static int SubProblem1(int x, ref S1 s)
   {
       s = new S1(new C1(5));

       SideEffect();

       C1 v = s.c;
       return v.a;
   }

   [Fact]
   public static int Problem2()
   {
       S1 s = new S1(new C1(4));
       return 91 + SubProblem2(0, s) + SubProblem2(1, s);
   }

   [MethodImpl(MethodImplOptions.NoInlining)]
   static int SubProblem2(int x, S1 s)
   {
       if (x == 0)
       {
           s = new S1(new C1(5));
       }

       SideEffect();

       C1 v = s.c;
       return v.a;
   }

   [Fact]
   public static int Problem3()
   {
       C1 c = new C1(6);
       return 1 + SubProblem3(0, c, c, c, c, c, c, c, c) + SubProblem3(1, c, c, c, c, c, c, c, c);
   }

   [MethodImpl(MethodImplOptions.NoInlining)]
   static int SubProblem3(int x, C1 c1, C1 c2, C1 c3, C1 c4, C1 c5, C1 c6, C1 c7, C1 c8)
   {
       if (x == 0)
       {
           c1 = new C1(7);
           c8 = new C1(8);
       }

       SideEffect();

       return c1.a + c2.a + c3.a + c4.a + c5.a + c6.a + c7.a + c8.a;
   }


   [MethodImpl(MethodImplOptions.NoInlining)]
   static void SideEffect() { }
}
