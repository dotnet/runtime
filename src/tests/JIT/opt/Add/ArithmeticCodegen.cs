// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

public class ArithmeticCodegenTests
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong FoldMultiply(ulong left, ulong right)
    {
        // Independent products do not require flags-preserving multiplication.
        // X64: {{^ +}}mul {{.*}}
        // X64-NOT: mulx
        // X64: ret
        ulong high = Math.BigMul(left, right, out ulong low);
        return high ^ low;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong FoldMultiplyWithCarry(ulong left, ulong right, ulong addend, out ulong high)
    {
        // An unrelated carry in the same block must not force this product to MULX.
        // X64-NOT: mulx
        // X64: {{^ +}}mul {{.*}}
        // X64-NOT: mulx
        // X64: {{^ +}}adc {{.*}}
        // X64-NOT: mulx
        // X64: ret
        ulong productHigh = Math.BigMul(left, right, out ulong productLow);
        ulong sum = unchecked(left + addend);
        high = unchecked(right + (sum < left ? 1UL : 0UL));
        return productHigh ^ productLow ^ sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool DecrementIsZero(ref int value)
    {
        // ZF-only consumers can still use the shorter DEC instruction.
        // X64: {{^ +}}dec
        // X64-NEXT: sete
        return Interlocked.Decrement(ref value) == 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong IncrementWithCarry(ulong value, out ulong high)
    {
        // INC would preserve stale CF instead of producing the overflow bit.
        // X64: {{^ +}}add {{.*}}, 1
        // X64-NOT: {{^ +}}inc
        // X64: ret
        ulong result = unchecked(value + 1);
        high = result < value ? 1UL : 0UL;
        return result;
    }

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

    private static class CarryState<T>
    {
        public static readonly ulong Mask;
        static CarryState() => Mask = 0x123456789ABCDEF0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong ObserveCarry(ulong carry) => carry;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong CarryAcrossHelper<T>(ulong left, ulong right, ulong high, bool observe)
    {
        // Generic static initialization may split the block before lowering.
        // The carry is consumed here and remains observable in successor blocks.
        ulong mask = CarryState<T>.Mask;
        ulong low = unchecked(left + right);
        ulong carry = low < left ? 1UL : 0UL;
        high = unchecked(high + carry);
        if (observe)
        {
            high ^= ObserveCarry(carry) + mask;
        }
        return low ^ high ^ carry;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong CarryAcrossException(ulong left, ulong right, ulong high, bool fail)
    {
        ulong carry = 123;
        try
        {
            ulong low = unchecked(left + right);
            carry = low < left ? 1UL : 0UL;
            high = unchecked(high + carry);
            MayThrow(fail);
            return low ^ high;
        }
        catch (InvalidOperationException)
        {
            return carry;
        }
    }

    private struct LimbPair
    {
        public ulong Low;
        public ulong High;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong CarryAcrossStructStore(ulong left, ulong right, LimbPair replacement)
    {
        LimbPair value = new LimbPair { Low = left, High = right };
        ulong sum = unchecked(value.Low + right);
        ulong carry = sum < value.Low ? 1UL : 0UL;
        // Reading High must observe the replacement, even if the field is promoted.
        value = replacement;
        return sum ^ unchecked(value.High + carry) ^ value.Low;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong BorrowAcrossStructStore(ulong left, ulong right, LimbPair replacement)
    {
        LimbPair value = new LimbPair { Low = left, High = right };
        ulong difference = unchecked(value.Low - right);
        ulong borrow = value.Low < right ? 1UL : 0UL;
        value = replacement;
        return difference ^ unchecked(value.High - borrow) ^ value.Low;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong CarryAcrossFieldAlias(ulong left, ulong right, ulong high)
    {
        LimbPair value = new LimbPair { Low = left, High = right };
        ref ulong alias = ref value.High;
        ulong sum = unchecked(value.Low + right);
        ulong carry = sum < value.Low ? 1UL : 0UL;
        alias = high;
        return sum ^ unchecked(value.High + carry);
    }

    [Fact]
    public static void TestEntryPoint()
    {
        ulong[] edges = { 0, 1, 2, uint.MaxValue, 1UL << 32, 1UL << 63, ulong.MaxValue - 1, ulong.MaxValue };
        foreach (ulong left in edges)
        {
            foreach (ulong right in edges)
            {
                BigInteger product = (BigInteger)left * right;
                ulong folded = (ulong)(product & ulong.MaxValue) ^ (ulong)(product >> 64);
                if (FoldMultiply(left, right) != folded)
                {
                    throw new Exception("FoldMultiply");
                }
                foreach (ulong addend in edges)
                {
                    LimbPair replacement = new LimbPair { Low = right, High = addend };
                    ulong lowSum = unchecked(left + right);
                    ulong carry = (ulong)(((BigInteger)left + right) >> 64);
                    ulong difference = unchecked(left - right);
                    ulong borrow = left < right ? 1UL : 0UL;
                    if (CarryAcrossStructStore(left, right, replacement) !=
                            (lowSum ^ unchecked(addend + carry) ^ right) ||
                        BorrowAcrossStructStore(left, right, replacement) !=
                            (difference ^ unchecked(addend - borrow) ^ right) ||
                        CarryAcrossFieldAlias(left, right, addend) != (lowSum ^ unchecked(addend + carry)))
                    {
                        throw new Exception("Carry/borrow operand observed across struct or field store");
                    }
                    ulong actual = FoldMultiplyWithCarry(left, right, addend, out ulong mixedHigh);
                    BigInteger sum = (BigInteger)left + addend;
                    ulong expected = (ulong)(product >> 64) ^ (ulong)(product & ulong.MaxValue) ^
                        (ulong)(sum & ulong.MaxValue);
                    if (actual != expected || mixedHigh != unchecked(right + (ulong)(sum >> 64)))
                    {
                        throw new Exception("Independent multiply with carry");
                    }
                }
                foreach (bool observe in new[] { false, true })
                {
                    BigInteger sum = (BigInteger)left + right;
                    ulong carry = (ulong)(sum >> 64);
                    ulong highPart = unchecked(left + carry);
                    if (observe)
                    {
                        highPart ^= carry + 0x123456789ABCDEF0;
                    }
                    ulong expected = (ulong)(sum & ulong.MaxValue) ^ highPart ^ carry;
                    if (CarryAcrossHelper<object>(left, right, left, observe) != expected ||
                        CarryAcrossHelper<string>(left, right, left, observe) != expected)
                    {
                        throw new Exception("Carry observed across helper expansion and branches");
                    }
                }
                ulong expectedCarry = (ulong)(((BigInteger)left + right) >> 64);
                if (CarryAcrossException(left, right, left, true) != expectedCarry ||
                    CarryAcrossException(left, right, left, false) !=
                        (unchecked(left + right) ^ unchecked(left + expectedCarry)))
                {
                    throw new Exception("Carry observed by exception handler");
                }
                if (right != 0)
                {
                    ulong rem = StoredRemainder(left, right, out ulong quotient);
                    var observed = DestinationRead(left, right, 0x123456789ABCDEF0);
                    if (rem != left % right || quotient != left / right ||
                        observed != (quotient, rem, 0x123456789ABCDEF0UL) || ReplaceDividend(left, right) != rem)
                    {
                        throw new Exception("Stored remainder");
                    }
                }
            }
            if (RemainderAcrossException(left, 7, 123, true) != 123 ||
                RemainderAcrossException(left, 7, 123, false) != left % 7)
            {
                throw new Exception("Remainder exception ordering");
            }
            if (IncrementWithCarry(left, out ulong high) != unchecked(left + 1) ||
                high != (left == ulong.MaxValue ? 1UL : 0UL))
            {
                throw new Exception("Carry flag");
            }
        }
        foreach (int start in new[] { int.MinValue, -1, 0, 1, 2, int.MaxValue })
        {
            int value = start;
            if (DecrementIsZero(ref value) != (start == 1) || value != unchecked(start - 1))
            {
                throw new Exception("Zero flag");
            }
        }
        try
        {
            StoredRemainder(1, 0, out _);
            throw new Exception("Missing division exception");
        }
        catch (DivideByZeroException)
        {
        }
    }
}
