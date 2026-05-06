// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Test_FastTailCallInlining
{
    [Theory]
    [InlineData(2)]
    public static void A(int i)
   {
       if (i > 0)
       {
           B(--i);
       }
   }  

   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   internal static void B(int i)
   {
       C(i);
       A(--i);
   }

    internal static void C(int i)
   {
       Console.WriteLine("In C");
       if (i==0)
       {
           Console.WriteLine("In C");
       }
   }
}
