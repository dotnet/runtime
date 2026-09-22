// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Reflection;
using Xunit;

public class DecimalWideTests
{
    private static void Check<T>(int width, int maximumPower) where T : IBinaryInteger<T>
    {
        Type number = typeof(object).Assembly.GetType("System.Number", throwOnError: true)!;
        MethodInfo Method(string name) => number.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!.MakeGenericMethod(typeof(T));
        MethodInfo multiply = Method("WideMultiply");
        MethodInfo add = Method("WideAdd");
        MethodInfo subtract = Method("WideSubtract");
        MethodInfo divide = Method("WideDivideByPow10");
        BigInteger mask = (BigInteger.One << width) - 1;
        Random random = new Random(125799);
        byte[] bytes = new byte[width / 4];

        BigInteger Next()
        {
            random.NextBytes(bytes);
            return new BigInteger(bytes, isUnsigned: true);
        }

        T Limb(BigInteger value) => T.CreateChecked(value & mask);

        void Equal(object actual, BigInteger expected)
        {
            if (BigInteger.CreateChecked((T)actual) != expected)
            {
                throw new Exception($"Incorrect {typeof(T)} wide arithmetic result: expected {expected}, got {actual}");
            }
        }

        for (int i = 0; i < 1000; i++)
        {
            BigInteger x = Next();
            BigInteger y = Next();
            // Include all-one limbs, zero, and carry/borrow across limb boundaries.
            if (i < 4)
            {
                x = i == 0 ? 0 : (BigInteger.One << (2 * width)) - 1;
                y = i < 2 ? x : BigInteger.One << ((i - 2) * width);
            }

            object[] product = { Limb(x), Limb(y), T.Zero, T.Zero };
            multiply.Invoke(null, product);
            BigInteger expected = (x & mask) * (y & mask);
            Equal(product[2], expected >> width);
            Equal(product[3], expected & mask);

            // The private addition helper's contract excludes high-limb overflow.
            BigInteger a = x >> 1;
            BigInteger b = y >> 1;
            object[] sum = { Limb(a >> width), Limb(a), Limb(b >> width), Limb(b), T.Zero, T.Zero };
            add.Invoke(null, sum);
            expected = a + b;
            Equal(sum[4], expected >> width);
            Equal(sum[5], expected & mask);

            a = BigInteger.Max(x, y);
            b = BigInteger.Min(x, y);
            object[] difference = { Limb(a >> width), Limb(a), Limb(b >> width), Limb(b), T.Zero, T.Zero };
            subtract.Invoke(null, difference);
            expected = a - b;
            Equal(difference[4], expected >> width);
            Equal(difference[5], expected & mask);

            BigInteger divisor = BigInteger.Pow(10, i % (maximumPower + 1));
            object[] quotient = { Limb(x >> width), Limb(x), Limb(divisor) };
            object remainder = divide.Invoke(null, quotient)!;
            expected = BigInteger.DivRem(x, divisor, out BigInteger expectedRemainder);
            Equal(quotient[0], expected >> width);
            Equal(quotient[1], expected & mask);
            Equal(remainder, expectedRemainder);
        }
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Check<uint>(32, 4);
        Check<ulong>(64, 9);
        Check<UInt128>(128, 19);
    }
}
