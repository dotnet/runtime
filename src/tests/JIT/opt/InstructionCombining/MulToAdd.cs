// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

// Test "X * 2" to "X + X"

public class Program
{
    private static int resultCode = 100;

    [Fact]
    public static int TestEntryPoint()
    {
        float[] testValues =
            {
                0, 0.01f, 1.333f, 1/3.0f, 0.5f, 1, 2, 3, 4,
                MathF.PI, MathF.E,
                float.MinValue, float.MaxValue,
                int.MaxValue, long.MaxValue,
                int.MinValue, long.MinValue,
                float.NegativeInfinity,
                float.PositiveInfinity,
                float.NaN,
            };

        testValues = testValues.Concat(testValues.Select(v => -v)).ToArray();

        foreach (float testValue in testValues)
        {
            var tf = new TestFloats();

            // Case 1: argument
            AssertEquals(tf.TestArg(testValue), tf.TestArg_var(testValue));

            // Case 2: ref argument
            float t1 = testValue, t2 = testValue;
            tf.TestArgRef(ref t1);
            tf.TestArgRef_var(ref t2);
            AssertEquals(t1, t2);

            // Case 3: out argument
            tf.TestArgOut(t1, out t1);
            tf.TestArgOut_var(t2, out t2);
            AssertEquals(t1, t2);

            // Case 4: field
            tf.TestField();
            tf.TestField_var();
            AssertEquals(tf.field1, tf.field2);
            
            // Case 5: call
            AssertEquals(tf.TestCall(), tf.TestCall_var());
            AssertEquals(tf.field1, tf.field2);
        }

        return resultCode;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AssertEquals(float expected, float actual)
    {
        int expectedBits = BitConverter.SingleToInt32Bits(expected);
        int actualBits = BitConverter.SingleToInt32Bits(actual);
        if (expectedBits != actualBits)
        {
            resultCode++;
            Console.WriteLine($"AssertEquals: {expected} != {actual}");
        }
    }
}

public class TestFloats
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T Var<T>(T t) => t;


    // Case 1: argument

    [MethodImpl(MethodImplOptions.NoInlining)]
    public float TestArg(float x) => x * 2;
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    public float TestArg_var(float x) => x * Var(2);


    // Case 2: ref argument

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void TestArgRef(ref float x) => x *= 2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void TestArgRef_var(ref float x) => x *= Var(2);


    // Case 3: out argument

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void TestArgOut(float x, out float y) => y = x * 2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void TestArgOut_var(float x, out float y) => y = x * Var(2);


    // Case 4: field

    public float field1 = 3.14f;
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void TestField() => field1 *= 2;

    public float field2 = 3.14f;
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void TestField_var() => field2 *= Var(2);


    // Case 5: Call

    [MethodImpl(MethodImplOptions.NoInlining)]
    public float Call1() => field1++; // with side-effect

    [MethodImpl(MethodImplOptions.NoInlining)]
    public float Call2() => field2++; // with side-effect


    [MethodImpl(MethodImplOptions.NoInlining)]
    public float TestCall() => Call1() * 2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public float TestCall_var() => Call2() * Var(2);
}
