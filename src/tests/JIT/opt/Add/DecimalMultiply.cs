using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

public class DecimalMultiplyTests
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static decimal Multiply(decimal a, decimal b) => a * b;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static decimal Square(decimal value) => value * value;

    private static readonly BigInteger MaxCoefficient = (BigInteger.One << 96) - 1;

    private static BigInteger Coefficient(int[] bits) =>
        ((BigInteger)(uint)bits[2] << 64) | ((BigInteger)(uint)bits[1] << 32) | (uint)bits[0];

    private static decimal Expected(decimal a, decimal b)
    {
        int[] left = decimal.GetBits(a);
        int[] right = decimal.GetBits(b);
        BigInteger product = Coefficient(left) * Coefficient(right);
        int scale = ((left[3] >> 16) & 0xFF) + ((right[3] >> 16) & 0xFF);
        bool negative = ((left[3] ^ right[3]) & int.MinValue) != 0;

        // Round the exact product directly, avoiding double rounding as digits
        // are removed to fit decimal's 96-bit coefficient and maximum scale.
        for (int dropped = Math.Max(0, scale - 28); dropped <= scale; dropped++)
        {
            BigInteger divisor = BigInteger.Pow(10, dropped);
            BigInteger coefficient = BigInteger.DivRem(product, divisor, out BigInteger remainder);
            int comparison = (remainder * 2).CompareTo(divisor);
            if (comparison > 0 || (comparison == 0 && !coefficient.IsEven))
            {
                coefficient++;
            }
            if (coefficient <= MaxCoefficient)
            {
                return new decimal((int)(uint)(coefficient & uint.MaxValue),
                    (int)(uint)((coefficient >> 32) & uint.MaxValue),
                    (int)(uint)(coefficient >> 64), negative, (byte)(scale - dropped));
            }
        }
        throw new OverflowException();
    }

    private static void Check(decimal a, decimal b, bool square = false)
    {
        decimal expected = 0;
        bool overflow = false;
        try
        {
            expected = Expected(a, b);
        }
        catch (OverflowException)
        {
            overflow = true;
        }
        try
        {
            decimal actual = square ? Square(a) : Multiply(a, b);
            if (overflow || actual != expected)
            {
                throw new Exception("Decimal multiplication mismatch");
            }
        }
        catch (OverflowException)
        {
            if (!overflow)
            {
                throw;
            }
        }
    }

    [Fact]
    public static void TestEntryPoint()
    {
        decimal[] edges =
        {
            0m, 1m, -1m, 0.5m, uint.MaxValue, ulong.MaxValue,
            decimal.MaxValue, decimal.MinValue,
            new decimal(-1, -1, -1, false, 28),
            new decimal(-1, -1, -1, true, 28),
            new decimal(1, 0, 0, false, 28),
            new decimal(3, 0, 0, false, 28),
            new decimal(1, 0, 1, false, 14),
        };
        foreach (decimal a in edges)
        {
            Check(a, a, square: true);
            foreach (decimal b in edges)
            {
                Check(a, b);
            }
        }

        var random = new Random(80674);
        byte[] bytes = new byte[24];
        for (int i = 0; i < 3000; i++)
        {
            random.NextBytes(bytes);
            int leftMid = i % 4 == 0 ? 0 : BitConverter.ToInt32(bytes, 4);
            int leftHigh = i % 3 == 0 || i % 4 == 0 ? 0 : BitConverter.ToInt32(bytes, 8);
            int rightMid = i % 5 == 0 ? 0 : BitConverter.ToInt32(bytes, 16);
            int rightHigh = i % 3 == 0 || i % 5 == 0 ? 0 : BitConverter.ToInt32(bytes, 20);
            decimal a = new decimal(BitConverter.ToInt32(bytes, 0), leftMid, leftHigh,
                i % 2 == 0, (byte)random.Next(29));
            decimal b = new decimal(BitConverter.ToInt32(bytes, 12), rightMid, rightHigh,
                i % 7 == 0, (byte)random.Next(29));
            Check(a, b);
            if (i % 17 == 0)
            {
                Check(a, a, square: true);
            }
        }
    }
}
