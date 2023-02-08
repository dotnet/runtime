// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_77710
{
    [Fact]
    public static int TestEntryPoint()
    {
        return Test(new Derived());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Test(Base b)
    {
        b.M(callBase: false, recursing: false);
        b.M(callBase: true, recursing: false);

        int result = 100;

        if (Base.BaseCalls != 1)
        {
            Console.WriteLine("FAIL: Expected BaseCalls to be 1, is actually {0}", Base.BaseCalls);
            result = -1;
        }

        if (Derived.DerivedCalls != 1)
        {
            Console.WriteLine("FAIL: Expected DerivedCalls to be 1, is actually {0}", Derived.DerivedCalls);
            result = -1;
        }

        if (result == 100)
        {
            Console.WriteLine("PASS");
        }

        return result;
    }

    private class Base
    {
        public static int BaseCalls;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public virtual int M(bool callBase, bool recursing)
        {
            BaseCalls++;
            return 0;
        }
    }

    private class Derived : Base
    {
        public static int DerivedCalls;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override int M(bool callBase, bool recursing)
        {
            if (recursing)
            {
                DerivedCalls++;
                return 0;
            }

            if (callBase)
            {
                base.M(false, true);
            }
            else
            {
                M(false, true);
            }

            return 0;
        }
    }
}
