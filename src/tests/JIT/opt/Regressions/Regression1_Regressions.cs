// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class C0
{
   public short F0;
   public bool F1;
   public sbyte F2;
   public short F3;
   public C0(sbyte f2, short f3)
   {
       F2 = f2;
       F3 = f3;
   }
}

public class Program
{
   public static IRuntime s_rt;
   public static int[][] s_13 = new int[][] { new int[] { 0 } };

   [Fact]
   public static int TestEntryPoint()
   {
       s_rt = new Runtime();
       var result = M74(0);

       if (result != -1)
           return 0;

       return 100;
   }

   [MethodImpl(MethodImplOptions.NoInlining)]
   public static short M74(short arg1)
   {
       try
       {
           M75();
       }
       finally
       {
           C0 var5 = new C0(0, 1);
           int var6 = s_13[0][0];
           arg1 = var5.F3;
           s_rt.WriteLine(var5.F0);
           s_rt.WriteLine(var5.F1);
           s_rt.WriteLine(var5.F2);
       }

       arg1 = (short)~arg1;
       arg1++;
       return arg1;
   }

   public static sbyte[] M75()
   {
       return default(sbyte[]);
   }
}

public interface IRuntime
{
   void WriteLine<T>(T value);
}

public class Runtime : IRuntime
{
   public void WriteLine<T>(T value) { }
}
