// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Reflection;
using Xunit;

public class DecimalWideDivisionTests
{
    private static void Check<T>(int width, int maximumPower) where T : IBinaryInteger<T>
    {
        Type number = typeof(object).Assembly.GetType("System.Number", throwOnError: true)!;
        MethodInfo Method(string name) => number.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!.MakeGenericMethod(typeof(T));
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
            // Include zero, maximal limbs, and values crossing the limb boundary.
            if (i < 4)
            {
                x = i switch
                {
                    0 => 0,
                    1 => (BigInteger.One << (2 * width)) - 1,
                    2 => BigInteger.One << width,
                    _ => (BigInteger.One << width) - 1,
                };
            }

            BigInteger divisor = BigInteger.Pow(10, i % (maximumPower + 1));
            object[] quotient = { Limb(x >> width), Limb(x), Limb(divisor) };
            object remainder = divide.Invoke(null, quotient)!;
            BigInteger expected = BigInteger.DivRem(x, divisor, out BigInteger expectedRemainder);
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
