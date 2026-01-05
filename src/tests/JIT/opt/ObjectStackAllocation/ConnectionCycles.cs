// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class X
{
    public X() { a = 15; b = 35; }
    public int a;
    public int b;
}

public class ConnectionCycles
{
   static bool b;

   [Fact]
   public static int Problem()
   {
       return SubProblem(false) + SubProblem(true);
   }

   [MethodImpl(MethodImplOptions.NoInlining)]
   static int SubProblem(bool b)
   {
       X x1 = new X();
       X x2 = new X();

       if (b)
       {
           x1 = x2;
       }
       else
       {
           x2 = x1;
       }

       SideEffect();

       return x1.a + x2.b;
   }


   [MethodImpl(MethodImplOptions.NoInlining)]
   static void SideEffect() { }
}
