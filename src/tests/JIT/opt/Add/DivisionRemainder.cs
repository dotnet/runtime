// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

public class DivisionRemainderTests
{
    private static int s_calls;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong RemainderNonZero(ulong value, ulong divisor)
    {
        // X64: div
        // X64: ret
        divisor |= 1;
        return Math.DivRem(value, divisor).Remainder;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Unsigned(ulong x, ulong y, out ulong remainder)
    {
        // X64: div
        // X64-NOT: imul
        // X64-NOT: sub
        // X64: ret
        ulong q = x / y;
        remainder = x - q * y;
        return q;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Signed(long x, long y, out long remainder)
    {
        // X64: idiv
        // X64-NOT: imul
        // X64-NOT: sub
        // X64: ret
        long q = x / y;
        remainder = x - y * q;
        return q;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static uint Unsigned32(uint x, uint y, out uint remainder)
    {
        // X64: div
        // X64-NOT: imul
        // X64: ret
        // X86: div
        // X86-NOT: imul
        // X86: ret
        uint q = x / y;
        remainder = x - q * y;
        return q;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Signed32(int x, int y, out int remainder)
    {
        // X64: idiv
        // X64-NOT: imul
        // X64: ret
        // X86: idiv
        // X86-NOT: imul
        // X86: ret
        int q = x / y;
        remainder = x - q * y;
        return q;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong RemainderOnly(ulong x, ulong y)
    {
        return x - (x / y) * y;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong AcrossCall(ulong x, ulong y)
    {
        ulong q = x / y;
        ulong z = Touch(x);
        return z ^ q ^ (x - q * y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong KeepQuotient(ulong x, ulong y, ulong z, out ulong remainder)
    {
        ulong q = x / y;
        s_calls++;
        remainder = x - q * y;
        return q ^ z;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Changed(ulong x, ulong y)
    {
        ulong q = x / y;
        x += 17;
        y += 3;
        q += 2;
        return x - q * y;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Aliased(ulong[] values, ulong y)
    {
        ulong q = values[0] / y;
        values[0] += 17;
        return values[0] - q * y;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong ChangedByRef(ulong x, ulong y)
    {
        ulong q = x / y;
        Mutate(ref x);
        return x - q * y;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong QuotientChangedByRef(ulong x, ulong y)
    {
        ulong q = x / y;
        Mutate(ref q);
        return x - q * y;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong CheckedProduct(ulong x, ulong y, ulong z)
    {
        ulong q = x / y;
        return checked(x - checked(q * z));
    }

    private static void CheckScalar(ulong x, ulong y)
    {
        if (RemainderNonZero(x, y) != x % (y | 1))
        {
            throw new Exception("The unused quotient must not remove the remainder's division");
        }
        if (y == 0)
        {
            return;
        }
        ulong q = Unsigned(x, y, out ulong r);
        if ((BigInteger)q * y + r != x || r >= y || RemainderOnly(x, y) != r ||
            InlinedRemainder(x, y) != r || AcrossCall(x, y) != ((x + 3) ^ q ^ r))
        {
            throw new Exception("unsigned scalar");
        }
        int calls = s_calls;
        if (KeepQuotient(x, y, ~x, out ulong r2) != (q ^ ~x) || r2 != r || s_calls != calls + 1)
        {
            throw new Exception("quotient lifetime");
        }
        if (Changed(x, y) != unchecked((x + 17) - (q + 2) * (y + 3)) ||
            Aliased(new[] { x }, y) != unchecked(x + 17 - q * y) ||
            ChangedByRef(x, y) != unchecked(x + 17 - q * y) ||
            QuotientChangedByRef(x, y) != unchecked(x - (q + 17) * y) ||
            EmbeddedAssignment(x, y) != unchecked(x - ((x + 17) / y) * y))
        {
            throw new Exception("changed operands");
        }

        long sx = (long)x, sy = (long)y;
        if (!(sx == long.MinValue && sy == -1))
        {
            long sq = Signed(sx, sy, out long sr);
            if ((BigInteger)sq * sy + sr != sx || BigInteger.Abs(sr) >= BigInteger.Abs(sy) ||
                (sr != 0 && (sr < 0) != (sx < 0)))
            {
                throw new Exception("signed scalar");
            }
        }

        uint ux = (uint)x, uy = (uint)y;
        if (uy == 0)
        {
            return;
        }
        uint uq = Unsigned32(ux, uy, out uint ur);
        if ((ulong)uq * uy + ur != ux || ur >= uy)
        {
            throw new Exception("uint division");
        }
        int ix = (int)x, iy = (int)y;
        if (!(ix == int.MinValue && iy == -1))
        {
            int iq = Signed32(ix, iy, out int ir);
            if ((long)iq * iy + ir != ix || Math.Abs((long)ir) >= Math.Abs((long)iy) ||
                (ir != 0 && (ir < 0) != (ix < 0)))
            {
                throw new Exception("int division");
            }
        }
    }

    private static void Throws<T>(Action action) where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            return;
        }
        throw new Exception($"Expected {typeof(T)}");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong InlinedRemainder(ulong x, ulong y) => Math.DivRem(x, y).Remainder;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Touch(ulong x) => x + 3;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Mutate(ref ulong x) => x += 17;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong EmbeddedAssignment(ulong x, ulong y) => x - ((x += 17) / y) * y;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong StoredRemainder(ulong value, ulong divisor, out ulong quotient)
    {
        ulong q = value / divisor;
        ulong remainder = value - q * divisor;
        quotient = q;
        return remainder;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (ulong, ulong, ulong) DestinationRead(ulong value, ulong divisor, ulong remainder)
    {
        ulong quotient = value / divisor;
        ulong oldRemainder = remainder;
        remainder = value - quotient * divisor;
        return (quotient, remainder, oldRemainder);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong ReplaceDividend(ulong value, ulong divisor)
    {
        ulong quotient = value / divisor;
        value = value - quotient * divisor;
        return value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void MayThrow(bool fail)
    {
        if (fail)
        {
            throw new InvalidOperationException();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong RemainderAcrossException(ulong value, ulong divisor, ulong remainder, bool fail)
    {
        try
        {
            ulong quotient = value / divisor;
            MayThrow(fail);
            remainder = value - quotient * divisor;
        }
        catch (InvalidOperationException)
        {
            return remainder;
        }
        return remainder;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        ulong[] edges = { 0, 1, 2, 3, uint.MaxValue, (ulong)uint.MaxValue + 1, 1UL << 63, (1UL << 63) - 1, ulong.MaxValue };
        foreach (ulong x in edges)
        {
            foreach (ulong y in edges)
            {
                CheckScalar(x, y);
            }
        }
        Throws<DivideByZeroException>(() => Unsigned(123, 0, out _));
        Throws<DivideByZeroException>(() => Signed(123, 0, out _));
        Throws<DivideByZeroException>(() => Unsigned32(123, 0, out _));
        Throws<DivideByZeroException>(() => Signed32(123, 0, out _));
        int callsBeforeException = s_calls;
        Throws<DivideByZeroException>(() => KeepQuotient(123, 0, 456, out _));
        if (s_calls != callsBeforeException)
        {
            throw new Exception("division exception order");
        }
        Throws<OverflowException>(() => Signed(long.MinValue, -1, out _));
        Throws<OverflowException>(() => Signed32(int.MinValue, -1, out _));
        Throws<OverflowException>(() => CheckedProduct(ulong.MaxValue, 1, 2));

        Random random = new Random(42);
        byte[] bytes = new byte[16];
        for (int i = 0; i < 3000; i++)
        {
            random.NextBytes(bytes);
            CheckScalar(BitConverter.ToUInt64(bytes, 0), BitConverter.ToUInt64(bytes, 8));
        }
        foreach (ulong value in edges)
        {
            foreach (ulong divisor in edges)
            {
                if (divisor == 0)
                {
                    continue;
                }
                ulong remainder = StoredRemainder(value, divisor, out ulong quotient);
                Assert.Equal(value / divisor, quotient);
                Assert.Equal(value % divisor, remainder);
                Assert.Equal((quotient, remainder, 123UL), DestinationRead(value, divisor, 123));
                Assert.Equal(remainder, ReplaceDividend(value, divisor));
            }
            Assert.Equal(123UL, RemainderAcrossException(value, 7, 123, true));
            Assert.Equal(value % 7, RemainderAcrossException(value, 7, 123, false));
        }
    }
}
