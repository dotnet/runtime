// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public class TailCallOptTest
{
    [Fact]
    public static int TestEntryPoint()
    {
        bool res1 = Caller1(new object(), 0L, 0xBEEF, new TypedDouble(1.0), new TypedDouble(2.0), new TypedDouble(3.0));
        bool res2 = Caller2(new object(), 0L, 0xBEEF, new TypedDouble(1.0), new TwoInts(3, 5), new TypedDouble(3.0));
        return (res1 && res2) ? 100 : 0;
    }

    // In this test typedDouble2 is passed to Caller1 on the stack. Then typedDouble2.Value is passed to Callee1 in a register.
    // Since Calee1 also has a stack argument (typedDouble3.Value) and the call is dispatched as a fast tail call,
    // it's set up in the incoming argument area of Caller1. JIT lowering code needs to ensure that typedDouble2
    // in that area is not overwritten by typedDouble3.Value before typedDouble2.Value is computed. The JIT had a bug in that code
    // because typedDouble2.Value was represented as GT_LCL_FLD and it was incorrectly converted into GT_LCL_VAR resulting in type
    // mismatches (long vs. double since the struct is passed as a long but its only field is double).
    public static bool Caller1(object parameters, long l,
        double doubleArg, TypedDouble typedDouble1, TypedDouble typedDouble2, TypedDouble typedDouble3)
    {
        double param = 19.0;

        Console.Write("Let's ");
        Console.Write("Discourage ");
        Console.Write("Inlining ");
        Console.Write("Of ");
        Console.Write("Caller ");
        Console.Write("Into ");
        Console.WriteLine("Main.");

        return Callee1(doubleArg, param, typedDouble1.Value, typedDouble2.Value, typedDouble3.Value);
    }

    public static bool Callee1(double doubleArg, double param, double typedDoubleArg1, double typedDoubleArg2, double typedDoubleArg3)
    {
        Console.WriteLine("{0} {1} {2} {3} {4}", doubleArg, param, typedDoubleArg1, typedDoubleArg2, typedDoubleArg3);
        if ((doubleArg == 0xBEEF) && (param == 19.0) && (typedDoubleArg1 == 1.0) && (typedDoubleArg2 == 2.0) && (typedDoubleArg3 == 3.0))
        {
            Console.WriteLine("PASSED");
            return true;
        }
        else
        {
            Console.WriteLine("FAILED");
            return false;
        }
    }

    // In this test twoInts is passed to Caller2 on the stack. Then twoInts.Value1 and twoInts.Value2 were passed to Callee2 in registers.
    // Since Calee2 also has a stack argument (i3) and the call is dispatched as a fast tail call,
    // it's set up in the incoming argument area of Caller2. JIT lowering code needs to ensure that twoInts
    // in that area is not overwritten by i3 before twoInts.Value1 and twoInts.Value2 are computed. The JIT had a bug in that code
    // because twoInts.Value1 and twoInts.Value2 were represented as GT_LCL_FLD and they were incorrectly converted into GT_LCL_VAR
    // resulting in an identical value passed for both fields.
    public static bool Caller2(object parameters, long l,
        double doubleArg, TypedDouble typedDouble1, TwoInts twoInts, TypedDouble typedDouble3)
    {
        double param = 19.0;

        Console.Write("Let's ");
        Console.Write("Discourage ");
        Console.Write("Inlining ");
        Console.Write("Of ");
        Console.Write("Caller ");
        Console.Write("Into ");
        Console.WriteLine("Main.");

        return Callee2(twoInts.Value1, twoInts.Value2, typedDouble1.Value, param, 11);
    }

    public static bool Callee2(int i1, int i2, double typedDoubleArg1, double typedDoubleArg2, int i3)
    {
        Console.WriteLine("{0} {1} {2} {3} {4}", i1, i2, typedDoubleArg1, typedDoubleArg2, i3);
        if ((i1 == 3) && (i2 == 5) && (typedDoubleArg1 == 1.0) && (typedDoubleArg2 == 19) && (i3 == 11))
        {
            Console.WriteLine("PASSED");
            return true;
        }
        else
        {
            Console.WriteLine("FAILED");
            return false;
        }
    }

    public struct TypedDouble
    {
        public TypedDouble(double value)
        {
            Value = value;
        }
        public readonly double Value;
    }

    public struct TwoInts
    {
        public TwoInts(int value1, int value2)
        {
            Value1 = value1;
            Value2 = value2;
        }
        public readonly int Value1;
        public readonly int Value2;
    }
}
