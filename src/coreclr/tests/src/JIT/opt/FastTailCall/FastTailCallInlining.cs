// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

public class Test
{
   public static int Main()
   {
       A(2);
       return 100;
   }
      
   public static void A(int i)
   {
       if (i > 0)
       {
           B(--i);
       }
   }  

   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   public static void B(int i)
   {
       C(i);
       A(--i);
   }

   public static void C(int i)
   {
       Console.WriteLine("In C");
       if (i==0)
       {
           Console.WriteLine("In C");
       }
   }
}
