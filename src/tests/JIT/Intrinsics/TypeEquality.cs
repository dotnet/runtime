// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using Xunit;

// Optimization of type equality tests to various
// vtable and handle comparisons.

class X<Q>
{
   [MethodImpl(MethodImplOptions.NoInlining)]
   public static bool Is(object o)
   {
        return typeof(Q) == o.GetType();
   }

   [MethodImpl(MethodImplOptions.NoInlining)]
   public static bool IsR(object o)
   {
        return o.GetType() == typeof(Q);
   }

   [MethodImpl(MethodImplOptions.NoInlining)]
   public static bool Is<P>()
   {
       return typeof(Q) == typeof(P);
   }
}

class X
{
   [MethodImpl(MethodImplOptions.NoInlining)]
   public static bool Is<Q>(object o)
   {
        return typeof(Q) == o.GetType();
   }

   [MethodImpl(MethodImplOptions.NoInlining)]
   public static bool IsR<Q>(object o)
   {
       return o.GetType() == typeof(Q);
   }

   [MethodImpl(MethodImplOptions.NoInlining)]
   public static bool Is<P,Q>()
   {
       return typeof(Q) == typeof(P);
   }
}

public class P
{
   [Fact]
   public static int TestEntryPoint()
   {   
      bool passed = true;

      string s = "string";
      object o = new object();
      string[] sarray = new string[0];
      object[] oarray = new object[0];

      // positive cases
      passed &= X<string>.Is(s);
      passed &= X<object>.Is(o);
      passed &= X<string[]>.Is(sarray);
      passed &= X<object[]>.Is(oarray);

      passed &= X<string>.IsR(s);

      passed &= X.Is<string, string>();

      passed &= X.Is<string>(s);
      passed &= X.Is<object>(o);
      passed &= X.Is<string[]>(sarray);
      passed &= X.Is<object[]>(oarray);

      passed &= X.IsR<string>(s);

      passed &= X<string>.Is<string>();

      // negative cases
      bool failed = false;

      failed |= X<string>.Is(o);
      failed |= X<object>.Is(s);
      failed |= X<string[]>.Is(oarray);
      failed |= X<object[]>.Is(sarray);

      failed |= X<string>.IsR(o);

      failed |= X.Is<string, object>();

      failed |= X.Is<string>(o);
      failed |= X.Is<object>(s);
      failed |= X.Is<string[]>(oarray);
      failed |= X.Is<object[]>(sarray);

      failed |= X.IsR<string>(o);

      failed |= X<object>.Is<string>();

      return passed && !failed ? 100 : -1;
   }
}
