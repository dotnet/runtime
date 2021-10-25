// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

public class ValueNumberingCheckedIntegerArithemticWithConstants
{
    private static int _global = 0;
    private static int _counter = 100;

    public static int Main()
    {
        RuntimeHelpers.RunClassConstructor(typeof(ValueNumberingCheckedIntegerArithemticWithConstants).TypeHandle);
        TestInt32();
        TestUInt32();
        TestInt64();
        TestUInt64();

        return _counter;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int IncrementGlobal() => ++_global;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool BreakUpFlow() => false;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void EvalArg<T>(T arg) { }

    private static void TestInt32()
    {
        ConfirmAdditionIdentities(42);
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmAdditionIdentities(int value)
        {
            int zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(value + zero) != value)
            {
                Console.WriteLine($"Addition identity for int 'checked(value + zero)' was evaluted to '{checked(value + zero)}'. Expected: '{value}'.");
                _counter++;
            }

            if (checked(zero + value) != value)
            {
                Console.WriteLine($"Addition identity for int 'checked(zero + value)' was evaluted to '{checked(zero + value)}'. Expected: '{value}'.");
                _counter++;
            }
        }
        ConfirmSubtractionIdentities(42);
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSubtractionIdentities(int value)
        {
            int zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(value - zero) != value)
            {
                Console.WriteLine($"Subtraction identity for int 'checked(value - zero)' was evaluted to '{checked(value - zero)}'. Expected: '{value}'.");
                _counter++;
            }

            if (checked(value - value) != 0)
            {
                Console.WriteLine($"Subtraction identity for int 'checked(value - value)' was evaluted to '{checked(value - value)}'. Expected: '{0}'.");
                _counter++;
            }
        }
        ConfirmMultiplicationIdentities(42);
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMultiplicationIdentities(int value)
        {
            int zero = 0;
            int one = 1;

            if (BreakUpFlow())
                return;

            if (checked(value * zero) != 0)
            {
                Console.WriteLine($"Multiplication identity for int 'checked(value * zero)' was evaluted to '{checked(value * zero)}'. Expected: '{0}'.");
                _counter++;
            }

            if (checked(zero * value) != 0)
            {
                Console.WriteLine($"Multiplication identity for int 'checked(zero * value)' was evaluted to '{checked(zero * value)}'. Expected: '{0}'.");
                _counter++;
            }

            if (checked(value * one) != value)
            {
                Console.WriteLine($"Multiplication identity for int 'checked(value * one)' was evaluted to '{checked(value * one)}'. Expected: '{value}'.");
                _counter++;
            }

            if (checked(one * value) != value)
            {
                Console.WriteLine($"Multiplication identity for int 'checked(one * value)' was evaluted to '{checked(one * value)}'. Expected: '{value}'.");
                _counter++;
            }
        }
        ConfirmMinPlusMinOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinPlusMinOverflows()
        {
            int min = int.MinValue;

            _counter++;
            try
            {
                _ = checked(min + min);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(min + min)' did not throw OverflowException.");
        }
        ConfirmMinPlusMinusHalfOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinPlusMinusHalfOverflows()
        {
            int min = int.MinValue;
            int minusHalf = int.MinValue / 2;

            _counter++;
            try
            {
                _ = checked(min + minusHalf);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(min + minusHalf)' did not throw OverflowException.");
        }
        ConfirmMinPlusMinusOneOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinPlusMinusOneOverflows()
        {
            int min = int.MinValue;
            int minusOne = -1;

            _counter++;
            try
            {
                _ = checked(min + minusOne);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(min + minusOne)' did not throw OverflowException.");
        }
        ConfirmMinPlusZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinPlusZeroIsFoldedCorrectly()
        {
            int min = int.MinValue;
            int zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(min + zero) != int.MinValue + 0)
            {
                Console.WriteLine($"'checked(min + zero)' was evaluted to '{checked(min + zero)}'. Expected: '{int.MinValue + 0}'.");
                _counter++;
            }
        }
        ConfirmMinPlusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinPlusOneIsFoldedCorrectly()
        {
            int min = int.MinValue;
            int one = 1;

            if (BreakUpFlow())
                return;

            if (checked(min + one) != int.MinValue + 1)
            {
                Console.WriteLine($"'checked(min + one)' was evaluted to '{checked(min + one)}'. Expected: '{int.MinValue + 1}'.");
                _counter++;
            }
        }
        ConfirmMinPlusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinPlusHalfIsFoldedCorrectly()
        {
            int min = int.MinValue;
            int half = int.MaxValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(min + half) != int.MinValue + int.MaxValue / 2)
            {
                Console.WriteLine($"'checked(min + half)' was evaluted to '{checked(min + half)}'. Expected: '{int.MinValue + int.MaxValue / 2}'.");
                _counter++;
            }
        }
        ConfirmMinPlusMaxIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinPlusMaxIsFoldedCorrectly()
        {
            int min = int.MinValue;
            int max = int.MaxValue;

            if (BreakUpFlow())
                return;

            if (checked(min + max) != int.MinValue + int.MaxValue)
            {
                Console.WriteLine($"'checked(min + max)' was evaluted to '{checked(min + max)}'. Expected: '{int.MinValue + int.MaxValue}'.");
                _counter++;
            }
        }
        ConfirmMinusHalfPlusMinOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusHalfPlusMinOverflows()
        {
            int minusHalf = int.MinValue / 2;
            int min = int.MinValue;

            _counter++;
            try
            {
                _ = checked(minusHalf + min);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(minusHalf + min)' did not throw OverflowException.");
        }
        ConfirmMinusHalfPlusMinusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusHalfPlusMinusHalfIsFoldedCorrectly()
        {
            int minusHalf = int.MinValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(minusHalf + minusHalf) != int.MinValue / 2 + int.MinValue / 2)
            {
                Console.WriteLine($"'checked(minusHalf + minusHalf)' was evaluted to '{checked(minusHalf + minusHalf)}'. Expected: '{int.MinValue / 2 + int.MinValue / 2}'.");
                _counter++;
            }
        }
        ConfirmMinusHalfPlusMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusHalfPlusMinusOneIsFoldedCorrectly()
        {
            int minusHalf = int.MinValue / 2;
            int minusOne = -1;

            if (BreakUpFlow())
                return;

            if (checked(minusHalf + minusOne) != int.MinValue / 2 + -1)
            {
                Console.WriteLine($"'checked(minusHalf + minusOne)' was evaluted to '{checked(minusHalf + minusOne)}'. Expected: '{int.MinValue / 2 + -1}'.");
                _counter++;
            }
        }
        ConfirmMinusHalfPlusZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusHalfPlusZeroIsFoldedCorrectly()
        {
            int minusHalf = int.MinValue / 2;
            int zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(minusHalf + zero) != int.MinValue / 2 + 0)
            {
                Console.WriteLine($"'checked(minusHalf + zero)' was evaluted to '{checked(minusHalf + zero)}'. Expected: '{int.MinValue / 2 + 0}'.");
                _counter++;
            }
        }
        ConfirmMinusHalfPlusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusHalfPlusOneIsFoldedCorrectly()
        {
            int minusHalf = int.MinValue / 2;
            int one = 1;

            if (BreakUpFlow())
                return;

            if (checked(minusHalf + one) != int.MinValue / 2 + 1)
            {
                Console.WriteLine($"'checked(minusHalf + one)' was evaluted to '{checked(minusHalf + one)}'. Expected: '{int.MinValue / 2 + 1}'.");
                _counter++;
            }
        }
        ConfirmMinusHalfPlusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusHalfPlusHalfIsFoldedCorrectly()
        {
            int minusHalf = int.MinValue / 2;
            int half = int.MaxValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(minusHalf + half) != int.MinValue / 2 + int.MaxValue / 2)
            {
                Console.WriteLine($"'checked(minusHalf + half)' was evaluted to '{checked(minusHalf + half)}'. Expected: '{int.MinValue / 2 + int.MaxValue / 2}'.");
                _counter++;
            }
        }
        ConfirmMinusHalfPlusMaxIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusHalfPlusMaxIsFoldedCorrectly()
        {
            int minusHalf = int.MinValue / 2;
            int max = int.MaxValue;

            if (BreakUpFlow())
                return;

            if (checked(minusHalf + max) != int.MinValue / 2 + int.MaxValue)
            {
                Console.WriteLine($"'checked(minusHalf + max)' was evaluted to '{checked(minusHalf + max)}'. Expected: '{int.MinValue / 2 + int.MaxValue}'.");
                _counter++;
            }
        }
        ConfirmMinusOnePlusMinOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusOnePlusMinOverflows()
        {
            int minusOne = -1;
            int min = int.MinValue;

            _counter++;
            try
            {
                _ = checked(minusOne + min);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(minusOne + min)' did not throw OverflowException.");
        }
        ConfirmMinusOnePlusMinusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusOnePlusMinusHalfIsFoldedCorrectly()
        {
            int minusOne = -1;
            int minusHalf = int.MinValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(minusOne + minusHalf) != -1 + int.MinValue / 2)
            {
                Console.WriteLine($"'checked(minusOne + minusHalf)' was evaluted to '{checked(minusOne + minusHalf)}'. Expected: '{-1 + int.MinValue / 2}'.");
                _counter++;
            }
        }
        ConfirmMinusOnePlusMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusOnePlusMinusOneIsFoldedCorrectly()
        {
            int minusOne = -1;

            if (BreakUpFlow())
                return;

            if (checked(minusOne + minusOne) != -1 + -1)
            {
                Console.WriteLine($"'checked(minusOne + minusOne)' was evaluted to '{checked(minusOne + minusOne)}'. Expected: '{-1 + -1}'.");
                _counter++;
            }
        }
        ConfirmMinusOnePlusZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusOnePlusZeroIsFoldedCorrectly()
        {
            int minusOne = -1;
            int zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(minusOne + zero) != -1 + 0)
            {
                Console.WriteLine($"'checked(minusOne + zero)' was evaluted to '{checked(minusOne + zero)}'. Expected: '{-1 + 0}'.");
                _counter++;
            }
        }
        ConfirmMinusOnePlusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusOnePlusOneIsFoldedCorrectly()
        {
            int minusOne = -1;
            int one = 1;

            if (BreakUpFlow())
                return;

            if (checked(minusOne + one) != -1 + 1)
            {
                Console.WriteLine($"'checked(minusOne + one)' was evaluted to '{checked(minusOne + one)}'. Expected: '{-1 + 1}'.");
                _counter++;
            }
        }
        ConfirmMinusOnePlusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusOnePlusHalfIsFoldedCorrectly()
        {
            int minusOne = -1;
            int half = int.MaxValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(minusOne + half) != -1 + int.MaxValue / 2)
            {
                Console.WriteLine($"'checked(minusOne + half)' was evaluted to '{checked(minusOne + half)}'. Expected: '{-1 + int.MaxValue / 2}'.");
                _counter++;
            }
        }
        ConfirmMinusOnePlusMaxIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusOnePlusMaxIsFoldedCorrectly()
        {
            int minusOne = -1;
            int max = int.MaxValue;

            if (BreakUpFlow())
                return;

            if (checked(minusOne + max) != -1 + int.MaxValue)
            {
                Console.WriteLine($"'checked(minusOne + max)' was evaluted to '{checked(minusOne + max)}'. Expected: '{-1 + int.MaxValue}'.");
                _counter++;
            }
        }
        ConfirmZeroPlusMinIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroPlusMinIsFoldedCorrectly()
        {
            int zero = 0;
            int min = int.MinValue;

            if (BreakUpFlow())
                return;

            if (checked(zero + min) != 0 + int.MinValue)
            {
                Console.WriteLine($"'checked(zero + min)' was evaluted to '{checked(zero + min)}'. Expected: '{0 + int.MinValue}'.");
                _counter++;
            }
        }
        ConfirmZeroPlusMinusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroPlusMinusHalfIsFoldedCorrectly()
        {
            int zero = 0;
            int minusHalf = int.MinValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(zero + minusHalf) != 0 + int.MinValue / 2)
            {
                Console.WriteLine($"'checked(zero + minusHalf)' was evaluted to '{checked(zero + minusHalf)}'. Expected: '{0 + int.MinValue / 2}'.");
                _counter++;
            }
        }
        ConfirmZeroPlusMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroPlusMinusOneIsFoldedCorrectly()
        {
            int zero = 0;
            int minusOne = -1;

            if (BreakUpFlow())
                return;

            if (checked(zero + minusOne) != 0 + -1)
            {
                Console.WriteLine($"'checked(zero + minusOne)' was evaluted to '{checked(zero + minusOne)}'. Expected: '{0 + -1}'.");
                _counter++;
            }
        }
        ConfirmZeroPlusZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroPlusZeroIsFoldedCorrectly()
        {
            int zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(zero + zero) != 0 + 0)
            {
                Console.WriteLine($"'checked(zero + zero)' was evaluted to '{checked(zero + zero)}'. Expected: '{0 + 0}'.");
                _counter++;
            }
        }
        ConfirmZeroPlusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroPlusOneIsFoldedCorrectly()
        {
            int zero = 0;
            int one = 1;

            if (BreakUpFlow())
                return;

            if (checked(zero + one) != 0 + 1)
            {
                Console.WriteLine($"'checked(zero + one)' was evaluted to '{checked(zero + one)}'. Expected: '{0 + 1}'.");
                _counter++;
            }
        }
        ConfirmZeroPlusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroPlusHalfIsFoldedCorrectly()
        {
            int zero = 0;
            int half = int.MaxValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(zero + half) != 0 + int.MaxValue / 2)
            {
                Console.WriteLine($"'checked(zero + half)' was evaluted to '{checked(zero + half)}'. Expected: '{0 + int.MaxValue / 2}'.");
                _counter++;
            }
        }
        ConfirmZeroPlusMaxIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroPlusMaxIsFoldedCorrectly()
        {
            int zero = 0;
            int max = int.MaxValue;

            if (BreakUpFlow())
                return;

            if (checked(zero + max) != 0 + int.MaxValue)
            {
                Console.WriteLine($"'checked(zero + max)' was evaluted to '{checked(zero + max)}'. Expected: '{0 + int.MaxValue}'.");
                _counter++;
            }
        }
        ConfirmOnePlusMinIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOnePlusMinIsFoldedCorrectly()
        {
            int one = 1;
            int min = int.MinValue;

            if (BreakUpFlow())
                return;

            if (checked(one + min) != 1 + int.MinValue)
            {
                Console.WriteLine($"'checked(one + min)' was evaluted to '{checked(one + min)}'. Expected: '{1 + int.MinValue}'.");
                _counter++;
            }
        }
        ConfirmOnePlusMinusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOnePlusMinusHalfIsFoldedCorrectly()
        {
            int one = 1;
            int minusHalf = int.MinValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(one + minusHalf) != 1 + int.MinValue / 2)
            {
                Console.WriteLine($"'checked(one + minusHalf)' was evaluted to '{checked(one + minusHalf)}'. Expected: '{1 + int.MinValue / 2}'.");
                _counter++;
            }
        }
        ConfirmOnePlusMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOnePlusMinusOneIsFoldedCorrectly()
        {
            int one = 1;
            int minusOne = -1;

            if (BreakUpFlow())
                return;

            if (checked(one + minusOne) != 1 + -1)
            {
                Console.WriteLine($"'checked(one + minusOne)' was evaluted to '{checked(one + minusOne)}'. Expected: '{1 + -1}'.");
                _counter++;
            }
        }
        ConfirmOnePlusZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOnePlusZeroIsFoldedCorrectly()
        {
            int one = 1;
            int zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(one + zero) != 1 + 0)
            {
                Console.WriteLine($"'checked(one + zero)' was evaluted to '{checked(one + zero)}'. Expected: '{1 + 0}'.");
                _counter++;
            }
        }
        ConfirmOnePlusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOnePlusOneIsFoldedCorrectly()
        {
            int one = 1;

            if (BreakUpFlow())
                return;

            if (checked(one + one) != 1 + 1)
            {
                Console.WriteLine($"'checked(one + one)' was evaluted to '{checked(one + one)}'. Expected: '{1 + 1}'.");
                _counter++;
            }
        }
        ConfirmOnePlusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOnePlusHalfIsFoldedCorrectly()
        {
            int one = 1;
            int half = int.MaxValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(one + half) != 1 + int.MaxValue / 2)
            {
                Console.WriteLine($"'checked(one + half)' was evaluted to '{checked(one + half)}'. Expected: '{1 + int.MaxValue / 2}'.");
                _counter++;
            }
        }
        ConfirmOnePlusMaxOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOnePlusMaxOverflows()
        {
            int one = 1;
            int max = int.MaxValue;

            _counter++;
            try
            {
                _ = checked(one + max);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(one + max)' did not throw OverflowException.");
        }
        ConfirmHalfPlusMinIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfPlusMinIsFoldedCorrectly()
        {
            int half = int.MaxValue / 2;
            int min = int.MinValue;

            if (BreakUpFlow())
                return;

            if (checked(half + min) != int.MaxValue / 2 + int.MinValue)
            {
                Console.WriteLine($"'checked(half + min)' was evaluted to '{checked(half + min)}'. Expected: '{int.MaxValue / 2 + int.MinValue}'.");
                _counter++;
            }
        }
        ConfirmHalfPlusMinusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfPlusMinusHalfIsFoldedCorrectly()
        {
            int half = int.MaxValue / 2;
            int minusHalf = int.MinValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(half + minusHalf) != int.MaxValue / 2 + int.MinValue / 2)
            {
                Console.WriteLine($"'checked(half + minusHalf)' was evaluted to '{checked(half + minusHalf)}'. Expected: '{int.MaxValue / 2 + int.MinValue / 2}'.");
                _counter++;
            }
        }
        ConfirmHalfPlusMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfPlusMinusOneIsFoldedCorrectly()
        {
            int half = int.MaxValue / 2;
            int minusOne = -1;

            if (BreakUpFlow())
                return;

            if (checked(half + minusOne) != int.MaxValue / 2 + -1)
            {
                Console.WriteLine($"'checked(half + minusOne)' was evaluted to '{checked(half + minusOne)}'. Expected: '{int.MaxValue / 2 + -1}'.");
                _counter++;
            }
        }
        ConfirmHalfPlusZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfPlusZeroIsFoldedCorrectly()
        {
            int half = int.MaxValue / 2;
            int zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(half + zero) != int.MaxValue / 2 + 0)
            {
                Console.WriteLine($"'checked(half + zero)' was evaluted to '{checked(half + zero)}'. Expected: '{int.MaxValue / 2 + 0}'.");
                _counter++;
            }
        }
        ConfirmHalfPlusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfPlusOneIsFoldedCorrectly()
        {
            int half = int.MaxValue / 2;
            int one = 1;

            if (BreakUpFlow())
                return;

            if (checked(half + one) != int.MaxValue / 2 + 1)
            {
                Console.WriteLine($"'checked(half + one)' was evaluted to '{checked(half + one)}'. Expected: '{int.MaxValue / 2 + 1}'.");
                _counter++;
            }
        }
        ConfirmHalfPlusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfPlusHalfIsFoldedCorrectly()
        {
            int half = int.MaxValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(half + half) != int.MaxValue / 2 + int.MaxValue / 2)
            {
                Console.WriteLine($"'checked(half + half)' was evaluted to '{checked(half + half)}'. Expected: '{int.MaxValue / 2 + int.MaxValue / 2}'.");
                _counter++;
            }
        }
        ConfirmHalfPlusMaxOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfPlusMaxOverflows()
        {
            int half = int.MaxValue / 2;
            int max = int.MaxValue;

            _counter++;
            try
            {
                _ = checked(half + max);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(half + max)' did not throw OverflowException.");
        }
        ConfirmMaxPlusMinIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxPlusMinIsFoldedCorrectly()
        {
            int max = int.MaxValue;
            int min = int.MinValue;

            if (BreakUpFlow())
                return;

            if (checked(max + min) != int.MaxValue + int.MinValue)
            {
                Console.WriteLine($"'checked(max + min)' was evaluted to '{checked(max + min)}'. Expected: '{int.MaxValue + int.MinValue}'.");
                _counter++;
            }
        }
        ConfirmMaxPlusMinusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxPlusMinusHalfIsFoldedCorrectly()
        {
            int max = int.MaxValue;
            int minusHalf = int.MinValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(max + minusHalf) != int.MaxValue + int.MinValue / 2)
            {
                Console.WriteLine($"'checked(max + minusHalf)' was evaluted to '{checked(max + minusHalf)}'. Expected: '{int.MaxValue + int.MinValue / 2}'.");
                _counter++;
            }
        }
        ConfirmMaxPlusMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxPlusMinusOneIsFoldedCorrectly()
        {
            int max = int.MaxValue;
            int minusOne = -1;

            if (BreakUpFlow())
                return;

            if (checked(max + minusOne) != int.MaxValue + -1)
            {
                Console.WriteLine($"'checked(max + minusOne)' was evaluted to '{checked(max + minusOne)}'. Expected: '{int.MaxValue + -1}'.");
                _counter++;
            }
        }
        ConfirmMaxPlusZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxPlusZeroIsFoldedCorrectly()
        {
            int max = int.MaxValue;
            int zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(max + zero) != int.MaxValue + 0)
            {
                Console.WriteLine($"'checked(max + zero)' was evaluted to '{checked(max + zero)}'. Expected: '{int.MaxValue + 0}'.");
                _counter++;
            }
        }
        ConfirmMaxPlusOneOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxPlusOneOverflows()
        {
            int max = int.MaxValue;
            int one = 1;

            _counter++;
            try
            {
                _ = checked(max + one);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(max + one)' did not throw OverflowException.");
        }
        ConfirmMaxPlusHalfOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxPlusHalfOverflows()
        {
            int max = int.MaxValue;
            int half = int.MaxValue / 2;

            _counter++;
            try
            {
                _ = checked(max + half);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(max + half)' did not throw OverflowException.");
        }
        ConfirmMaxPlusMaxOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxPlusMaxOverflows()
        {
            int max = int.MaxValue;

            _counter++;
            try
            {
                _ = checked(max + max);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(max + max)' did not throw OverflowException.");
        }

        ConfirmMinMinusMinIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinMinusMinIsFoldedCorrectly()
        {
            int min = int.MinValue;

            if (BreakUpFlow())
                return;

            if (checked(min - min) != int.MinValue - int.MinValue)
            {
                Console.WriteLine($"'checked(min - min)' was evaluted to '{checked(min - min)}'. Expected: '{int.MinValue - int.MinValue}'.");
                _counter++;
            }
        }
        ConfirmMinMinusMinusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinMinusMinusHalfIsFoldedCorrectly()
        {
            int min = int.MinValue;
            int minusHalf = int.MinValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(min - minusHalf) != int.MinValue - int.MinValue / 2)
            {
                Console.WriteLine($"'checked(min - minusHalf)' was evaluted to '{checked(min - minusHalf)}'. Expected: '{int.MinValue - int.MinValue / 2}'.");
                _counter++;
            }
        }
        ConfirmMinMinusMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinMinusMinusOneIsFoldedCorrectly()
        {
            int min = int.MinValue;
            int minusOne = -1;

            if (BreakUpFlow())
                return;

            if (checked(min - minusOne) != int.MinValue - -1)
            {
                Console.WriteLine($"'checked(min - minusOne)' was evaluted to '{checked(min - minusOne)}'. Expected: '{int.MinValue - -1}'.");
                _counter++;
            }
        }
        ConfirmMinMinusZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinMinusZeroIsFoldedCorrectly()
        {
            int min = int.MinValue;
            int zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(min - zero) != int.MinValue - 0)
            {
                Console.WriteLine($"'checked(min - zero)' was evaluted to '{checked(min - zero)}'. Expected: '{int.MinValue - 0}'.");
                _counter++;
            }
        }
        ConfirmMinMinusOneOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinMinusOneOverflows()
        {
            int min = int.MinValue;
            int one = 1;

            _counter++;
            try
            {
                _ = checked(min - one);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(min - one)' did not throw OverflowException.");
        }
        ConfirmMinMinusHalfOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinMinusHalfOverflows()
        {
            int min = int.MinValue;
            int half = int.MaxValue / 2;

            _counter++;
            try
            {
                _ = checked(min - half);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(min - half)' did not throw OverflowException.");
        }
        ConfirmMinMinusMaxOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinMinusMaxOverflows()
        {
            int min = int.MinValue;
            int max = int.MaxValue;

            _counter++;
            try
            {
                _ = checked(min - max);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(min - max)' did not throw OverflowException.");
        }
        ConfirmMinusHalfMinusMinIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusHalfMinusMinIsFoldedCorrectly()
        {
            int minusHalf = int.MinValue / 2;
            int min = int.MinValue;

            if (BreakUpFlow())
                return;

            if (checked(minusHalf - min) != int.MinValue / 2 - int.MinValue)
            {
                Console.WriteLine($"'checked(minusHalf - min)' was evaluted to '{checked(minusHalf - min)}'. Expected: '{int.MinValue / 2 - int.MinValue}'.");
                _counter++;
            }
        }
        ConfirmMinusHalfMinusMinusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusHalfMinusMinusHalfIsFoldedCorrectly()
        {
            int minusHalf = int.MinValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(minusHalf - minusHalf) != int.MinValue / 2 - int.MinValue / 2)
            {
                Console.WriteLine($"'checked(minusHalf - minusHalf)' was evaluted to '{checked(minusHalf - minusHalf)}'. Expected: '{int.MinValue / 2 - int.MinValue / 2}'.");
                _counter++;
            }
        }
        ConfirmMinusHalfMinusMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusHalfMinusMinusOneIsFoldedCorrectly()
        {
            int minusHalf = int.MinValue / 2;
            int minusOne = -1;

            if (BreakUpFlow())
                return;

            if (checked(minusHalf - minusOne) != int.MinValue / 2 - -1)
            {
                Console.WriteLine($"'checked(minusHalf - minusOne)' was evaluted to '{checked(minusHalf - minusOne)}'. Expected: '{int.MinValue / 2 - -1}'.");
                _counter++;
            }
        }
        ConfirmMinusHalfMinusZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusHalfMinusZeroIsFoldedCorrectly()
        {
            int minusHalf = int.MinValue / 2;
            int zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(minusHalf - zero) != int.MinValue / 2 - 0)
            {
                Console.WriteLine($"'checked(minusHalf - zero)' was evaluted to '{checked(minusHalf - zero)}'. Expected: '{int.MinValue / 2 - 0}'.");
                _counter++;
            }
        }
        ConfirmMinusHalfMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusHalfMinusOneIsFoldedCorrectly()
        {
            int minusHalf = int.MinValue / 2;
            int one = 1;

            if (BreakUpFlow())
                return;

            if (checked(minusHalf - one) != int.MinValue / 2 - 1)
            {
                Console.WriteLine($"'checked(minusHalf - one)' was evaluted to '{checked(minusHalf - one)}'. Expected: '{int.MinValue / 2 - 1}'.");
                _counter++;
            }
        }
        ConfirmMinusHalfMinusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusHalfMinusHalfIsFoldedCorrectly()
        {
            int minusHalf = int.MinValue / 2;
            int half = int.MaxValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(minusHalf - half) != int.MinValue / 2 - int.MaxValue / 2)
            {
                Console.WriteLine($"'checked(minusHalf - half)' was evaluted to '{checked(minusHalf - half)}'. Expected: '{int.MinValue / 2 - int.MaxValue / 2}'.");
                _counter++;
            }
        }
        ConfirmMinusHalfMinusMaxOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusHalfMinusMaxOverflows()
        {
            int minusHalf = int.MinValue / 2;
            int max = int.MaxValue;

            _counter++;
            try
            {
                _ = checked(minusHalf - max);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(minusHalf - max)' did not throw OverflowException.");
        }
        ConfirmMinusOneMinusMinIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusOneMinusMinIsFoldedCorrectly()
        {
            int minusOne = -1;
            int min = int.MinValue;

            if (BreakUpFlow())
                return;

            if (checked(minusOne - min) != -1 - int.MinValue)
            {
                Console.WriteLine($"'checked(minusOne - min)' was evaluted to '{checked(minusOne - min)}'. Expected: '{-1 - int.MinValue}'.");
                _counter++;
            }
        }
        ConfirmMinusOneMinusMinusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusOneMinusMinusHalfIsFoldedCorrectly()
        {
            int minusOne = -1;
            int minusHalf = int.MinValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(minusOne - minusHalf) != -1 - int.MinValue / 2)
            {
                Console.WriteLine($"'checked(minusOne - minusHalf)' was evaluted to '{checked(minusOne - minusHalf)}'. Expected: '{-1 - int.MinValue / 2}'.");
                _counter++;
            }
        }
        ConfirmMinusOneMinusMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusOneMinusMinusOneIsFoldedCorrectly()
        {
            int minusOne = -1;

            if (BreakUpFlow())
                return;

            if (checked(minusOne - minusOne) != -1 - -1)
            {
                Console.WriteLine($"'checked(minusOne - minusOne)' was evaluted to '{checked(minusOne - minusOne)}'. Expected: '{-1 - -1}'.");
                _counter++;
            }
        }
        ConfirmMinusOneMinusZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusOneMinusZeroIsFoldedCorrectly()
        {
            int minusOne = -1;
            int zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(minusOne - zero) != -1 - 0)
            {
                Console.WriteLine($"'checked(minusOne - zero)' was evaluted to '{checked(minusOne - zero)}'. Expected: '{-1 - 0}'.");
                _counter++;
            }
        }
        ConfirmMinusOneMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusOneMinusOneIsFoldedCorrectly()
        {
            int minusOne = -1;
            int one = 1;

            if (BreakUpFlow())
                return;

            if (checked(minusOne - one) != -1 - 1)
            {
                Console.WriteLine($"'checked(minusOne - one)' was evaluted to '{checked(minusOne - one)}'. Expected: '{-1 - 1}'.");
                _counter++;
            }
        }
        ConfirmMinusOneMinusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusOneMinusHalfIsFoldedCorrectly()
        {
            int minusOne = -1;
            int half = int.MaxValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(minusOne - half) != -1 - int.MaxValue / 2)
            {
                Console.WriteLine($"'checked(minusOne - half)' was evaluted to '{checked(minusOne - half)}'. Expected: '{-1 - int.MaxValue / 2}'.");
                _counter++;
            }
        }
        ConfirmMinusOneMinusMaxIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusOneMinusMaxIsFoldedCorrectly()
        {
            int minusOne = -1;
            int max = int.MaxValue;

            if (BreakUpFlow())
                return;

            if (checked(minusOne - max) != -1 - int.MaxValue)
            {
                Console.WriteLine($"'checked(minusOne - max)' was evaluted to '{checked(minusOne - max)}'. Expected: '{-1 - int.MaxValue}'.");
                _counter++;
            }
        }
        ConfirmZeroMinusMinOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroMinusMinOverflows()
        {
            int zero = 0;
            int min = int.MinValue;

            _counter++;
            try
            {
                _ = checked(zero - min);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(zero - min)' did not throw OverflowException.");
        }
        ConfirmZeroMinusMinusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroMinusMinusHalfIsFoldedCorrectly()
        {
            int zero = 0;
            int minusHalf = int.MinValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(zero - minusHalf) != 0 - int.MinValue / 2)
            {
                Console.WriteLine($"'checked(zero - minusHalf)' was evaluted to '{checked(zero - minusHalf)}'. Expected: '{0 - int.MinValue / 2}'.");
                _counter++;
            }
        }
        ConfirmZeroMinusMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroMinusMinusOneIsFoldedCorrectly()
        {
            int zero = 0;
            int minusOne = -1;

            if (BreakUpFlow())
                return;

            if (checked(zero - minusOne) != 0 - -1)
            {
                Console.WriteLine($"'checked(zero - minusOne)' was evaluted to '{checked(zero - minusOne)}'. Expected: '{0 - -1}'.");
                _counter++;
            }
        }
        ConfirmZeroMinusZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroMinusZeroIsFoldedCorrectly()
        {
            int zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(zero - zero) != 0 - 0)
            {
                Console.WriteLine($"'checked(zero - zero)' was evaluted to '{checked(zero - zero)}'. Expected: '{0 - 0}'.");
                _counter++;
            }
        }
        ConfirmZeroMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroMinusOneIsFoldedCorrectly()
        {
            int zero = 0;
            int one = 1;

            if (BreakUpFlow())
                return;

            if (checked(zero - one) != 0 - 1)
            {
                Console.WriteLine($"'checked(zero - one)' was evaluted to '{checked(zero - one)}'. Expected: '{0 - 1}'.");
                _counter++;
            }
        }
        ConfirmZeroMinusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroMinusHalfIsFoldedCorrectly()
        {
            int zero = 0;
            int half = int.MaxValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(zero - half) != 0 - int.MaxValue / 2)
            {
                Console.WriteLine($"'checked(zero - half)' was evaluted to '{checked(zero - half)}'. Expected: '{0 - int.MaxValue / 2}'.");
                _counter++;
            }
        }
        ConfirmZeroMinusMaxIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroMinusMaxIsFoldedCorrectly()
        {
            int zero = 0;
            int max = int.MaxValue;

            if (BreakUpFlow())
                return;

            if (checked(zero - max) != 0 - int.MaxValue)
            {
                Console.WriteLine($"'checked(zero - max)' was evaluted to '{checked(zero - max)}'. Expected: '{0 - int.MaxValue}'.");
                _counter++;
            }
        }
        ConfirmOneMinusMinOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOneMinusMinOverflows()
        {
            int one = 1;
            int min = int.MinValue;

            _counter++;
            try
            {
                _ = checked(one - min);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(one - min)' did not throw OverflowException.");
        }
        ConfirmOneMinusMinusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOneMinusMinusHalfIsFoldedCorrectly()
        {
            int one = 1;
            int minusHalf = int.MinValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(one - minusHalf) != 1 - int.MinValue / 2)
            {
                Console.WriteLine($"'checked(one - minusHalf)' was evaluted to '{checked(one - minusHalf)}'. Expected: '{1 - int.MinValue / 2}'.");
                _counter++;
            }
        }
        ConfirmOneMinusMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOneMinusMinusOneIsFoldedCorrectly()
        {
            int one = 1;
            int minusOne = -1;

            if (BreakUpFlow())
                return;

            if (checked(one - minusOne) != 1 - -1)
            {
                Console.WriteLine($"'checked(one - minusOne)' was evaluted to '{checked(one - minusOne)}'. Expected: '{1 - -1}'.");
                _counter++;
            }
        }
        ConfirmOneMinusZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOneMinusZeroIsFoldedCorrectly()
        {
            int one = 1;
            int zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(one - zero) != 1 - 0)
            {
                Console.WriteLine($"'checked(one - zero)' was evaluted to '{checked(one - zero)}'. Expected: '{1 - 0}'.");
                _counter++;
            }
        }
        ConfirmOneMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOneMinusOneIsFoldedCorrectly()
        {
            int one = 1;

            if (BreakUpFlow())
                return;

            if (checked(one - one) != 1 - 1)
            {
                Console.WriteLine($"'checked(one - one)' was evaluted to '{checked(one - one)}'. Expected: '{1 - 1}'.");
                _counter++;
            }
        }
        ConfirmOneMinusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOneMinusHalfIsFoldedCorrectly()
        {
            int one = 1;
            int half = int.MaxValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(one - half) != 1 - int.MaxValue / 2)
            {
                Console.WriteLine($"'checked(one - half)' was evaluted to '{checked(one - half)}'. Expected: '{1 - int.MaxValue / 2}'.");
                _counter++;
            }
        }
        ConfirmOneMinusMaxIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOneMinusMaxIsFoldedCorrectly()
        {
            int one = 1;
            int max = int.MaxValue;

            if (BreakUpFlow())
                return;

            if (checked(one - max) != 1 - int.MaxValue)
            {
                Console.WriteLine($"'checked(one - max)' was evaluted to '{checked(one - max)}'. Expected: '{1 - int.MaxValue}'.");
                _counter++;
            }
        }
        ConfirmHalfMinusMinOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfMinusMinOverflows()
        {
            int half = int.MaxValue / 2;
            int min = int.MinValue;

            _counter++;
            try
            {
                _ = checked(half - min);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(half - min)' did not throw OverflowException.");
        }
        ConfirmHalfMinusMinusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfMinusMinusHalfIsFoldedCorrectly()
        {
            int half = int.MaxValue / 2;
            int minusHalf = int.MinValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(half - minusHalf) != int.MaxValue / 2 - int.MinValue / 2)
            {
                Console.WriteLine($"'checked(half - minusHalf)' was evaluted to '{checked(half - minusHalf)}'. Expected: '{int.MaxValue / 2 - int.MinValue / 2}'.");
                _counter++;
            }
        }
        ConfirmHalfMinusMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfMinusMinusOneIsFoldedCorrectly()
        {
            int half = int.MaxValue / 2;
            int minusOne = -1;

            if (BreakUpFlow())
                return;

            if (checked(half - minusOne) != int.MaxValue / 2 - -1)
            {
                Console.WriteLine($"'checked(half - minusOne)' was evaluted to '{checked(half - minusOne)}'. Expected: '{int.MaxValue / 2 - -1}'.");
                _counter++;
            }
        }
        ConfirmHalfMinusZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfMinusZeroIsFoldedCorrectly()
        {
            int half = int.MaxValue / 2;
            int zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(half - zero) != int.MaxValue / 2 - 0)
            {
                Console.WriteLine($"'checked(half - zero)' was evaluted to '{checked(half - zero)}'. Expected: '{int.MaxValue / 2 - 0}'.");
                _counter++;
            }
        }
        ConfirmHalfMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfMinusOneIsFoldedCorrectly()
        {
            int half = int.MaxValue / 2;
            int one = 1;

            if (BreakUpFlow())
                return;

            if (checked(half - one) != int.MaxValue / 2 - 1)
            {
                Console.WriteLine($"'checked(half - one)' was evaluted to '{checked(half - one)}'. Expected: '{int.MaxValue / 2 - 1}'.");
                _counter++;
            }
        }
        ConfirmHalfMinusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfMinusHalfIsFoldedCorrectly()
        {
            int half = int.MaxValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(half - half) != int.MaxValue / 2 - int.MaxValue / 2)
            {
                Console.WriteLine($"'checked(half - half)' was evaluted to '{checked(half - half)}'. Expected: '{int.MaxValue / 2 - int.MaxValue / 2}'.");
                _counter++;
            }
        }
        ConfirmHalfMinusMaxIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfMinusMaxIsFoldedCorrectly()
        {
            int half = int.MaxValue / 2;
            int max = int.MaxValue;

            if (BreakUpFlow())
                return;

            if (checked(half - max) != int.MaxValue / 2 - int.MaxValue)
            {
                Console.WriteLine($"'checked(half - max)' was evaluted to '{checked(half - max)}'. Expected: '{int.MaxValue / 2 - int.MaxValue}'.");
                _counter++;
            }
        }
        ConfirmMaxMinusMinOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxMinusMinOverflows()
        {
            int max = int.MaxValue;
            int min = int.MinValue;

            _counter++;
            try
            {
                _ = checked(max - min);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(max - min)' did not throw OverflowException.");
        }
        ConfirmMaxMinusMinusHalfOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxMinusMinusHalfOverflows()
        {
            int max = int.MaxValue;
            int minusHalf = int.MinValue / 2;

            _counter++;
            try
            {
                _ = checked(max - minusHalf);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(max - minusHalf)' did not throw OverflowException.");
        }
        ConfirmMaxMinusMinusOneOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxMinusMinusOneOverflows()
        {
            int max = int.MaxValue;
            int minusOne = -1;

            _counter++;
            try
            {
                _ = checked(max - minusOne);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(max - minusOne)' did not throw OverflowException.");
        }
        ConfirmMaxMinusZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxMinusZeroIsFoldedCorrectly()
        {
            int max = int.MaxValue;
            int zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(max - zero) != int.MaxValue - 0)
            {
                Console.WriteLine($"'checked(max - zero)' was evaluted to '{checked(max - zero)}'. Expected: '{int.MaxValue - 0}'.");
                _counter++;
            }
        }
        ConfirmMaxMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxMinusOneIsFoldedCorrectly()
        {
            int max = int.MaxValue;
            int one = 1;

            if (BreakUpFlow())
                return;

            if (checked(max - one) != int.MaxValue - 1)
            {
                Console.WriteLine($"'checked(max - one)' was evaluted to '{checked(max - one)}'. Expected: '{int.MaxValue - 1}'.");
                _counter++;
            }
        }
        ConfirmMaxMinusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxMinusHalfIsFoldedCorrectly()
        {
            int max = int.MaxValue;
            int half = int.MaxValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(max - half) != int.MaxValue - int.MaxValue / 2)
            {
                Console.WriteLine($"'checked(max - half)' was evaluted to '{checked(max - half)}'. Expected: '{int.MaxValue - int.MaxValue / 2}'.");
                _counter++;
            }
        }
        ConfirmMaxMinusMaxIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxMinusMaxIsFoldedCorrectly()
        {
            int max = int.MaxValue;

            if (BreakUpFlow())
                return;

            if (checked(max - max) != int.MaxValue - int.MaxValue)
            {
                Console.WriteLine($"'checked(max - max)' was evaluted to '{checked(max - max)}'. Expected: '{int.MaxValue - int.MaxValue}'.");
                _counter++;
            }
        }

        ConfirmMinMultipliedByMinOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinMultipliedByMinOverflows()
        {
            int min = int.MinValue;

            _counter++;
            try
            {
                _ = checked(min * min);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(min * min)' did not throw OverflowException.");
        }
        ConfirmMinMultipliedByMinusHalfOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinMultipliedByMinusHalfOverflows()
        {
            int min = int.MinValue;
            int minusHalf = (int.MinValue / 2);

            _counter++;
            try
            {
                _ = checked(min * minusHalf);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(min * minusHalf)' did not throw OverflowException.");
        }
        ConfirmMinMultipliedByMinusOneOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinMultipliedByMinusOneOverflows()
        {
            int min = int.MinValue;
            int minusOne = -1;

            _counter++;
            try
            {
                _ = checked(min * minusOne);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(min * minusOne)' did not throw OverflowException.");
        }
        ConfirmMinMultipliedByZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinMultipliedByZeroIsFoldedCorrectly()
        {
            int min = int.MinValue;
            int zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(min * zero) != int.MinValue * 0)
            {
                Console.WriteLine($"'checked(min * zero)' was evaluted to '{checked(min * zero)}'. Expected: '{int.MinValue * 0}'.");
                _counter++;
            }
        }
        ConfirmMinMultipliedByOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinMultipliedByOneIsFoldedCorrectly()
        {
            int min = int.MinValue;
            int one = 1;

            if (BreakUpFlow())
                return;

            if (checked(min * one) != int.MinValue * 1)
            {
                Console.WriteLine($"'checked(min * one)' was evaluted to '{checked(min * one)}'. Expected: '{int.MinValue * 1}'.");
                _counter++;
            }
        }
        ConfirmMinMultipliedByHalfOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinMultipliedByHalfOverflows()
        {
            int min = int.MinValue;
            int half = (int.MaxValue / 2);

            _counter++;
            try
            {
                _ = checked(min * half);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(min * half)' did not throw OverflowException.");
        }
        ConfirmMinMultipliedByMaxOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinMultipliedByMaxOverflows()
        {
            int min = int.MinValue;
            int max = int.MaxValue;

            _counter++;
            try
            {
                _ = checked(min * max);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(min * max)' did not throw OverflowException.");
        }
        ConfirmMinusHalfMultipliedByMinOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusHalfMultipliedByMinOverflows()
        {
            int minusHalf = int.MinValue / 2;
            int min = int.MinValue;

            _counter++;
            try
            {
                _ = checked(minusHalf * min);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(minusHalf * min)' did not throw OverflowException.");
        }
        ConfirmMinusHalfMultipliedByMinusHalfOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusHalfMultipliedByMinusHalfOverflows()
        {
            int minusHalf = int.MinValue / 2;

            _counter++;
            try
            {
                _ = checked(minusHalf * minusHalf);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(minusHalf * minusHalf)' did not throw OverflowException.");
        }
        ConfirmMinusHalfMultipliedByMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusHalfMultipliedByMinusOneIsFoldedCorrectly()
        {
            int minusHalf = int.MinValue / 2;
            int minusOne = -1;

            if (BreakUpFlow())
                return;

            if (checked(minusHalf * minusOne) != int.MinValue / 2 * -1)
            {
                Console.WriteLine($"'checked(minusHalf * minusOne)' was evaluted to '{checked(minusHalf * minusOne)}'. Expected: '{int.MinValue / 2 * -1}'.");
                _counter++;
            }
        }
        ConfirmMinusHalfMultipliedByZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusHalfMultipliedByZeroIsFoldedCorrectly()
        {
            int minusHalf = int.MinValue / 2;
            int zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(minusHalf * zero) != int.MinValue / 2 * 0)
            {
                Console.WriteLine($"'checked(minusHalf * zero)' was evaluted to '{checked(minusHalf * zero)}'. Expected: '{int.MinValue / 2 * 0}'.");
                _counter++;
            }
        }
        ConfirmMinusHalfMultipliedByOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusHalfMultipliedByOneIsFoldedCorrectly()
        {
            int minusHalf = int.MinValue / 2;
            int one = 1;

            if (BreakUpFlow())
                return;

            if (checked(minusHalf * one) != int.MinValue / 2 * 1)
            {
                Console.WriteLine($"'checked(minusHalf * one)' was evaluted to '{checked(minusHalf * one)}'. Expected: '{int.MinValue / 2 * 1}'.");
                _counter++;
            }
        }
        ConfirmMinusHalfMultipliedByHalfOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusHalfMultipliedByHalfOverflows()
        {
            int minusHalf = int.MinValue / 2;
            int half = (int.MaxValue / 2);

            _counter++;
            try
            {
                _ = checked(minusHalf * half);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(minusHalf * half)' did not throw OverflowException.");
        }
        ConfirmMinusHalfMultipliedByMaxOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusHalfMultipliedByMaxOverflows()
        {
            int minusHalf = int.MinValue / 2;
            int max = int.MaxValue;

            _counter++;
            try
            {
                _ = checked(minusHalf * max);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(minusHalf * max)' did not throw OverflowException.");
        }
        ConfirmMinusOneMultipliedByMinOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusOneMultipliedByMinOverflows()
        {
            int minusOne = -1;
            int min = int.MinValue;

            _counter++;
            try
            {
                _ = checked(minusOne * min);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(minusOne * min)' did not throw OverflowException.");
        }
        ConfirmMinusOneMultipliedByMinusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusOneMultipliedByMinusHalfIsFoldedCorrectly()
        {
            int minusOne = -1;
            int minusHalf = (int.MinValue / 2);

            if (BreakUpFlow())
                return;

            if (checked(minusOne * minusHalf) != -1 * (int.MinValue / 2))
            {
                Console.WriteLine($"'checked(minusOne * minusHalf)' was evaluted to '{checked(minusOne * minusHalf)}'. Expected: '{-1 * (int.MinValue / 2)}'.");
                _counter++;
            }
        }
        ConfirmMinusOneMultipliedByMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusOneMultipliedByMinusOneIsFoldedCorrectly()
        {
            int minusOne = -1;

            if (BreakUpFlow())
                return;

            if (checked(minusOne * minusOne) != -1 * -1)
            {
                Console.WriteLine($"'checked(minusOne * minusOne)' was evaluted to '{checked(minusOne * minusOne)}'. Expected: '{-1 * -1}'.");
                _counter++;
            }
        }
        ConfirmMinusOneMultipliedByZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusOneMultipliedByZeroIsFoldedCorrectly()
        {
            int minusOne = -1;
            int zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(minusOne * zero) != -1 * 0)
            {
                Console.WriteLine($"'checked(minusOne * zero)' was evaluted to '{checked(minusOne * zero)}'. Expected: '{-1 * 0}'.");
                _counter++;
            }
        }
        ConfirmMinusOneMultipliedByOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusOneMultipliedByOneIsFoldedCorrectly()
        {
            int minusOne = -1;
            int one = 1;

            if (BreakUpFlow())
                return;

            if (checked(minusOne * one) != -1 * 1)
            {
                Console.WriteLine($"'checked(minusOne * one)' was evaluted to '{checked(minusOne * one)}'. Expected: '{-1 * 1}'.");
                _counter++;
            }
        }
        ConfirmMinusOneMultipliedByHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusOneMultipliedByHalfIsFoldedCorrectly()
        {
            int minusOne = -1;
            int half = (int.MaxValue / 2);

            if (BreakUpFlow())
                return;

            if (checked(minusOne * half) != -1 * (int.MaxValue / 2))
            {
                Console.WriteLine($"'checked(minusOne * half)' was evaluted to '{checked(minusOne * half)}'. Expected: '{-1 * (int.MaxValue / 2)}'.");
                _counter++;
            }
        }
        ConfirmMinusOneMultipliedByMaxIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusOneMultipliedByMaxIsFoldedCorrectly()
        {
            int minusOne = -1;
            int max = int.MaxValue;

            if (BreakUpFlow())
                return;

            if (checked(minusOne * max) != -1 * int.MaxValue)
            {
                Console.WriteLine($"'checked(minusOne * max)' was evaluted to '{checked(minusOne * max)}'. Expected: '{-1 * int.MaxValue}'.");
                _counter++;
            }
        }
        ConfirmZeroMultipliedByMinIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroMultipliedByMinIsFoldedCorrectly()
        {
            int zero = 0;
            int min = int.MinValue;

            if (BreakUpFlow())
                return;

            if (checked(zero * min) != 0 * int.MinValue)
            {
                Console.WriteLine($"'checked(zero * min)' was evaluted to '{checked(zero * min)}'. Expected: '{0 * int.MinValue}'.");
                _counter++;
            }
        }
        ConfirmZeroMultipliedByMinusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroMultipliedByMinusHalfIsFoldedCorrectly()
        {
            int zero = 0;
            int minusHalf = (int.MinValue / 2);

            if (BreakUpFlow())
                return;

            if (checked(zero * minusHalf) != 0 * (int.MinValue / 2))
            {
                Console.WriteLine($"'checked(zero * minusHalf)' was evaluted to '{checked(zero * minusHalf)}'. Expected: '{0 * (int.MinValue / 2)}'.");
                _counter++;
            }
        }
        ConfirmZeroMultipliedByMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroMultipliedByMinusOneIsFoldedCorrectly()
        {
            int zero = 0;
            int minusOne = -1;

            if (BreakUpFlow())
                return;

            if (checked(zero * minusOne) != 0 * -1)
            {
                Console.WriteLine($"'checked(zero * minusOne)' was evaluted to '{checked(zero * minusOne)}'. Expected: '{0 * -1}'.");
                _counter++;
            }
        }
        ConfirmZeroMultipliedByZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroMultipliedByZeroIsFoldedCorrectly()
        {
            int zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(zero * zero) != 0 * 0)
            {
                Console.WriteLine($"'checked(zero * zero)' was evaluted to '{checked(zero * zero)}'. Expected: '{0 * 0}'.");
                _counter++;
            }
        }
        ConfirmZeroMultipliedByOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroMultipliedByOneIsFoldedCorrectly()
        {
            int zero = 0;
            int one = 1;

            if (BreakUpFlow())
                return;

            if (checked(zero * one) != 0 * 1)
            {
                Console.WriteLine($"'checked(zero * one)' was evaluted to '{checked(zero * one)}'. Expected: '{0 * 1}'.");
                _counter++;
            }
        }
        ConfirmZeroMultipliedByHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroMultipliedByHalfIsFoldedCorrectly()
        {
            int zero = 0;
            int half = (int.MaxValue / 2);

            if (BreakUpFlow())
                return;

            if (checked(zero * half) != 0 * (int.MaxValue / 2))
            {
                Console.WriteLine($"'checked(zero * half)' was evaluted to '{checked(zero * half)}'. Expected: '{0 * (int.MaxValue / 2)}'.");
                _counter++;
            }
        }
        ConfirmZeroMultipliedByMaxIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroMultipliedByMaxIsFoldedCorrectly()
        {
            int zero = 0;
            int max = int.MaxValue;

            if (BreakUpFlow())
                return;

            if (checked(zero * max) != 0 * int.MaxValue)
            {
                Console.WriteLine($"'checked(zero * max)' was evaluted to '{checked(zero * max)}'. Expected: '{0 * int.MaxValue}'.");
                _counter++;
            }
        }
        ConfirmOneMultipliedByMinIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOneMultipliedByMinIsFoldedCorrectly()
        {
            int one = 1;
            int min = int.MinValue;

            if (BreakUpFlow())
                return;

            if (checked(one * min) != 1 * int.MinValue)
            {
                Console.WriteLine($"'checked(one * min)' was evaluted to '{checked(one * min)}'. Expected: '{1 * int.MinValue}'.");
                _counter++;
            }
        }
        ConfirmOneMultipliedByMinusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOneMultipliedByMinusHalfIsFoldedCorrectly()
        {
            int one = 1;
            int minusHalf = (int.MinValue / 2);

            if (BreakUpFlow())
                return;

            if (checked(one * minusHalf) != 1 * (int.MinValue / 2))
            {
                Console.WriteLine($"'checked(one * minusHalf)' was evaluted to '{checked(one * minusHalf)}'. Expected: '{1 * (int.MinValue / 2)}'.");
                _counter++;
            }
        }
        ConfirmOneMultipliedByMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOneMultipliedByMinusOneIsFoldedCorrectly()
        {
            int one = 1;
            int minusOne = -1;

            if (BreakUpFlow())
                return;

            if (checked(one * minusOne) != 1 * -1)
            {
                Console.WriteLine($"'checked(one * minusOne)' was evaluted to '{checked(one * minusOne)}'. Expected: '{1 * -1}'.");
                _counter++;
            }
        }
        ConfirmOneMultipliedByZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOneMultipliedByZeroIsFoldedCorrectly()
        {
            int one = 1;
            int zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(one * zero) != 1 * 0)
            {
                Console.WriteLine($"'checked(one * zero)' was evaluted to '{checked(one * zero)}'. Expected: '{1 * 0}'.");
                _counter++;
            }
        }
        ConfirmOneMultipliedByOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOneMultipliedByOneIsFoldedCorrectly()
        {
            int one = 1;

            if (BreakUpFlow())
                return;

            if (checked(one * one) != 1 * 1)
            {
                Console.WriteLine($"'checked(one * one)' was evaluted to '{checked(one * one)}'. Expected: '{1 * 1}'.");
                _counter++;
            }
        }
        ConfirmOneMultipliedByHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOneMultipliedByHalfIsFoldedCorrectly()
        {
            int one = 1;
            int half = (int.MaxValue / 2);

            if (BreakUpFlow())
                return;

            if (checked(one * half) != 1 * (int.MaxValue / 2))
            {
                Console.WriteLine($"'checked(one * half)' was evaluted to '{checked(one * half)}'. Expected: '{1 * (int.MaxValue / 2)}'.");
                _counter++;
            }
        }
        ConfirmOneMultipliedByMaxIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOneMultipliedByMaxIsFoldedCorrectly()
        {
            int one = 1;
            int max = int.MaxValue;

            if (BreakUpFlow())
                return;

            if (checked(one * max) != 1 * int.MaxValue)
            {
                Console.WriteLine($"'checked(one * max)' was evaluted to '{checked(one * max)}'. Expected: '{1 * int.MaxValue}'.");
                _counter++;
            }
        }
        ConfirmHalfMultipliedByMinOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfMultipliedByMinOverflows()
        {
            int half = int.MaxValue / 2;
            int min = int.MinValue;

            _counter++;
            try
            {
                _ = checked(half * min);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(half * min)' did not throw OverflowException.");
        }
        ConfirmHalfMultipliedByMinusHalfOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfMultipliedByMinusHalfOverflows()
        {
            int half = int.MaxValue / 2;
            int minusHalf = (int.MinValue / 2);

            _counter++;
            try
            {
                _ = checked(half * minusHalf);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(half * minusHalf)' did not throw OverflowException.");
        }
        ConfirmHalfMultipliedByMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfMultipliedByMinusOneIsFoldedCorrectly()
        {
            int half = int.MaxValue / 2;
            int minusOne = -1;

            if (BreakUpFlow())
                return;

            if (checked(half * minusOne) != int.MaxValue / 2 * -1)
            {
                Console.WriteLine($"'checked(half * minusOne)' was evaluted to '{checked(half * minusOne)}'. Expected: '{int.MaxValue / 2 * -1}'.");
                _counter++;
            }
        }
        ConfirmHalfMultipliedByZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfMultipliedByZeroIsFoldedCorrectly()
        {
            int half = int.MaxValue / 2;
            int zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(half * zero) != int.MaxValue / 2 * 0)
            {
                Console.WriteLine($"'checked(half * zero)' was evaluted to '{checked(half * zero)}'. Expected: '{int.MaxValue / 2 * 0}'.");
                _counter++;
            }
        }
        ConfirmHalfMultipliedByOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfMultipliedByOneIsFoldedCorrectly()
        {
            int half = int.MaxValue / 2;
            int one = 1;

            if (BreakUpFlow())
                return;

            if (checked(half * one) != int.MaxValue / 2 * 1)
            {
                Console.WriteLine($"'checked(half * one)' was evaluted to '{checked(half * one)}'. Expected: '{int.MaxValue / 2 * 1}'.");
                _counter++;
            }
        }
        ConfirmHalfMultipliedByHalfOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfMultipliedByHalfOverflows()
        {
            int half = int.MaxValue / 2;

            _counter++;
            try
            {
                _ = checked(half * half);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(half * half)' did not throw OverflowException.");
        }
        ConfirmHalfMultipliedByMaxOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfMultipliedByMaxOverflows()
        {
            int half = int.MaxValue / 2;
            int max = int.MaxValue;

            _counter++;
            try
            {
                _ = checked(half * max);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(half * max)' did not throw OverflowException.");
        }
        ConfirmMaxMultipliedByMinOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxMultipliedByMinOverflows()
        {
            int max = int.MaxValue;
            int min = int.MinValue;

            _counter++;
            try
            {
                _ = checked(max * min);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(max * min)' did not throw OverflowException.");
        }
        ConfirmMaxMultipliedByMinusHalfOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxMultipliedByMinusHalfOverflows()
        {
            int max = int.MaxValue;
            int minusHalf = (int.MinValue / 2);

            _counter++;
            try
            {
                _ = checked(max * minusHalf);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(max * minusHalf)' did not throw OverflowException.");
        }
        ConfirmMaxMultipliedByMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxMultipliedByMinusOneIsFoldedCorrectly()
        {
            int max = int.MaxValue;
            int minusOne = -1;

            if (BreakUpFlow())
                return;

            if (checked(max * minusOne) != int.MaxValue * -1)
            {
                Console.WriteLine($"'checked(max * minusOne)' was evaluted to '{checked(max * minusOne)}'. Expected: '{int.MaxValue * -1}'.");
                _counter++;
            }
        }
        ConfirmMaxMultipliedByZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxMultipliedByZeroIsFoldedCorrectly()
        {
            int max = int.MaxValue;
            int zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(max * zero) != int.MaxValue * 0)
            {
                Console.WriteLine($"'checked(max * zero)' was evaluted to '{checked(max * zero)}'. Expected: '{int.MaxValue * 0}'.");
                _counter++;
            }
        }
        ConfirmMaxMultipliedByOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxMultipliedByOneIsFoldedCorrectly()
        {
            int max = int.MaxValue;
            int one = 1;

            if (BreakUpFlow())
                return;

            if (checked(max * one) != int.MaxValue * 1)
            {
                Console.WriteLine($"'checked(max * one)' was evaluted to '{checked(max * one)}'. Expected: '{int.MaxValue * 1}'.");
                _counter++;
            }
        }
        ConfirmMaxMultipliedByHalfOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxMultipliedByHalfOverflows()
        {
            int max = int.MaxValue;
            int half = (int.MaxValue / 2);

            _counter++;
            try
            {
                _ = checked(max * half);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(max * half)' did not throw OverflowException.");
        }
        ConfirmMaxMultipliedByMaxOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxMultipliedByMaxOverflows()
        {
            int max = int.MaxValue;

            _counter++;
            try
            {
                _ = checked(max * max);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(max * max)' did not throw OverflowException.");
        }
    }

    private static void TestUInt32()
    {
        ConfirmAdditionIdentities(42);
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmAdditionIdentities(uint value)
        {
            uint zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(value + zero) != value)
            {
                Console.WriteLine($"Addition identity for uint 'checked(value + zero)' was evaluted to '{checked(value + zero)}'. Expected: '{value}'.");
                _counter++;
            }

            if (checked(zero + value) != value)
            {
                Console.WriteLine($"Addition identity for uint 'checked(zero + value)' was evaluted to '{checked(zero + value)}'. Expected: '{value}'.");
                _counter++;
            }
        }
        ConfirmSubtractionIdentities(42);
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSubtractionIdentities(uint value)
        {
            uint zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(value - zero) != value)
            {
                Console.WriteLine($"Subtraction identity for uint 'checked(value - zero)' was evaluted to '{checked(value - zero)}'. Expected: '{value}'.");
                _counter++;
            }

            if (checked(value - value) != 0)
            {
                Console.WriteLine($"Subtraction identity for uint 'checked(value - value)' was evaluted to '{checked(value - value)}'. Expected: '{0}'.");
                _counter++;
            }
        }
        ConfirmMultiplicationIdentities(42);
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMultiplicationIdentities(uint value)
        {
            uint zero = 0;
            uint one = 1;

            if (BreakUpFlow())
                return;

            if (checked(value * zero) != 0)
            {
                Console.WriteLine($"Multiplication identity for uint 'checked(value * zero)' was evaluted to '{checked(value * zero)}'. Expected: '{0}'.");
                _counter++;
            }

            if (checked(zero * value) != 0)
            {
                Console.WriteLine($"Multiplication identity for uint 'checked(zero * value)' was evaluted to '{checked(zero * value)}'. Expected: '{0}'.");
                _counter++;
            }

            if (checked(value * one) != value)
            {
                Console.WriteLine($"Multiplication identity for uint 'checked(value * one)' was evaluted to '{checked(value * one)}'. Expected: '{value}'.");
                _counter++;
            }

            if (checked(one * value) != value)
            {
                Console.WriteLine($"Multiplication identity for uint 'checked(one * value)' was evaluted to '{checked(one * value)}'. Expected: '{value}'.");
                _counter++;
            }
        }
        ConfirmZeroPlusZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroPlusZeroIsFoldedCorrectly()
        {
            uint zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(zero + zero) != 0 + 0)
            {
                Console.WriteLine($"'checked(zero + zero)' was evaluted to '{checked(zero + zero)}'. Expected: '{0 + 0}'.");
                _counter++;
            }
        }
        ConfirmZeroPlusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroPlusOneIsFoldedCorrectly()
        {
            uint zero = 0;
            uint one = 1;

            if (BreakUpFlow())
                return;

            if (checked(zero + one) != 0 + 1)
            {
                Console.WriteLine($"'checked(zero + one)' was evaluted to '{checked(zero + one)}'. Expected: '{0 + 1}'.");
                _counter++;
            }
        }
        ConfirmZeroPlusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroPlusHalfIsFoldedCorrectly()
        {
            uint zero = 0;
            uint half = uint.MaxValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(zero + half) != 0 + uint.MaxValue / 2)
            {
                Console.WriteLine($"'checked(zero + half)' was evaluted to '{checked(zero + half)}'. Expected: '{0 + uint.MaxValue / 2}'.");
                _counter++;
            }
        }
        ConfirmZeroPlusMaxIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroPlusMaxIsFoldedCorrectly()
        {
            uint zero = 0;
            uint max = uint.MaxValue;

            if (BreakUpFlow())
                return;

            if (checked(zero + max) != 0 + uint.MaxValue)
            {
                Console.WriteLine($"'checked(zero + max)' was evaluted to '{checked(zero + max)}'. Expected: '{0 + uint.MaxValue}'.");
                _counter++;
            }
        }
        ConfirmOnePlusZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOnePlusZeroIsFoldedCorrectly()
        {
            uint one = 1;
            uint zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(one + zero) != 1 + 0)
            {
                Console.WriteLine($"'checked(one + zero)' was evaluted to '{checked(one + zero)}'. Expected: '{1 + 0}'.");
                _counter++;
            }
        }
        ConfirmOnePlusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOnePlusOneIsFoldedCorrectly()
        {
            uint one = 1;

            if (BreakUpFlow())
                return;

            if (checked(one + one) != 1 + 1)
            {
                Console.WriteLine($"'checked(one + one)' was evaluted to '{checked(one + one)}'. Expected: '{1 + 1}'.");
                _counter++;
            }
        }
        ConfirmOnePlusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOnePlusHalfIsFoldedCorrectly()
        {
            uint one = 1;
            uint half = uint.MaxValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(one + half) != 1 + uint.MaxValue / 2)
            {
                Console.WriteLine($"'checked(one + half)' was evaluted to '{checked(one + half)}'. Expected: '{1 + uint.MaxValue / 2}'.");
                _counter++;
            }
        }
        ConfirmOnePlusMaxOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOnePlusMaxOverflows()
        {
            uint one = 1;
            uint max = uint.MaxValue;

            _counter++;
            try
            {
                _ = checked(one + max);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(one + max)' did not throw OverflowException.");
        }
        ConfirmHalfPlusZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfPlusZeroIsFoldedCorrectly()
        {
            uint half = uint.MaxValue / 2;
            uint zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(half + zero) != uint.MaxValue / 2 + 0)
            {
                Console.WriteLine($"'checked(half + zero)' was evaluted to '{checked(half + zero)}'. Expected: '{uint.MaxValue / 2 + 0}'.");
                _counter++;
            }
        }
        ConfirmHalfPlusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfPlusOneIsFoldedCorrectly()
        {
            uint half = uint.MaxValue / 2;
            uint one = 1;

            if (BreakUpFlow())
                return;

            if (checked(half + one) != uint.MaxValue / 2 + 1)
            {
                Console.WriteLine($"'checked(half + one)' was evaluted to '{checked(half + one)}'. Expected: '{uint.MaxValue / 2 + 1}'.");
                _counter++;
            }
        }
        ConfirmHalfPlusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfPlusHalfIsFoldedCorrectly()
        {
            uint half = uint.MaxValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(half + half) != uint.MaxValue / 2 + uint.MaxValue / 2)
            {
                Console.WriteLine($"'checked(half + half)' was evaluted to '{checked(half + half)}'. Expected: '{uint.MaxValue / 2 + uint.MaxValue / 2}'.");
                _counter++;
            }
        }
        ConfirmHalfPlusMaxOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfPlusMaxOverflows()
        {
            uint half = uint.MaxValue / 2;
            uint max = uint.MaxValue;

            _counter++;
            try
            {
                _ = checked(half + max);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(half + max)' did not throw OverflowException.");
        }
        ConfirmMaxPlusZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxPlusZeroIsFoldedCorrectly()
        {
            uint max = uint.MaxValue;
            uint zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(max + zero) != uint.MaxValue + 0)
            {
                Console.WriteLine($"'checked(max + zero)' was evaluted to '{checked(max + zero)}'. Expected: '{uint.MaxValue + 0}'.");
                _counter++;
            }
        }
        ConfirmMaxPlusOneOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxPlusOneOverflows()
        {
            uint max = uint.MaxValue;
            uint one = 1;

            _counter++;
            try
            {
                _ = checked(max + one);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(max + one)' did not throw OverflowException.");
        }
        ConfirmMaxPlusHalfOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxPlusHalfOverflows()
        {
            uint max = uint.MaxValue;
            uint half = uint.MaxValue / 2;

            _counter++;
            try
            {
                _ = checked(max + half);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(max + half)' did not throw OverflowException.");
        }
        ConfirmMaxPlusMaxOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxPlusMaxOverflows()
        {
            uint max = uint.MaxValue;

            _counter++;
            try
            {
                _ = checked(max + max);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(max + max)' did not throw OverflowException.");
        }

        ConfirmZeroMinusZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroMinusZeroIsFoldedCorrectly()
        {
            uint zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(zero - zero) != 0 - 0)
            {
                Console.WriteLine($"'checked(zero - zero)' was evaluted to '{checked(zero - zero)}'. Expected: '{0 - 0}'.");
                _counter++;
            }
        }
        ConfirmZeroMinusOneOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroMinusOneOverflows()
        {
            uint zero = 0;
            uint one = 1;

            _counter++;
            try
            {
                _ = checked(zero - one);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(zero - one)' did not throw OverflowException.");
        }
        ConfirmZeroMinusHalfOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroMinusHalfOverflows()
        {
            uint zero = 0;
            uint half = uint.MaxValue / 2;

            _counter++;
            try
            {
                _ = checked(zero - half);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(zero - half)' did not throw OverflowException.");
        }
        ConfirmZeroMinusMaxOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroMinusMaxOverflows()
        {
            uint zero = 0;
            uint max = uint.MaxValue;

            _counter++;
            try
            {
                _ = checked(zero - max);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(zero - max)' did not throw OverflowException.");
        }
        ConfirmOneMinusZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOneMinusZeroIsFoldedCorrectly()
        {
            uint one = 1;
            uint zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(one - zero) != 1 - 0)
            {
                Console.WriteLine($"'checked(one - zero)' was evaluted to '{checked(one - zero)}'. Expected: '{1 - 0}'.");
                _counter++;
            }
        }
        ConfirmOneMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOneMinusOneIsFoldedCorrectly()
        {
            uint one = 1;

            if (BreakUpFlow())
                return;

            if (checked(one - one) != 1 - 1)
            {
                Console.WriteLine($"'checked(one - one)' was evaluted to '{checked(one - one)}'. Expected: '{1 - 1}'.");
                _counter++;
            }
        }
        ConfirmOneMinusHalfOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOneMinusHalfOverflows()
        {
            uint one = 1;
            uint half = uint.MaxValue / 2;

            _counter++;
            try
            {
                _ = checked(one - half);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(one - half)' did not throw OverflowException.");
        }
        ConfirmOneMinusMaxOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOneMinusMaxOverflows()
        {
            uint one = 1;
            uint max = uint.MaxValue;

            _counter++;
            try
            {
                _ = checked(one - max);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(one - max)' did not throw OverflowException.");
        }
        ConfirmHalfMinusZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfMinusZeroIsFoldedCorrectly()
        {
            uint half = uint.MaxValue / 2;
            uint zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(half - zero) != uint.MaxValue / 2 - 0)
            {
                Console.WriteLine($"'checked(half - zero)' was evaluted to '{checked(half - zero)}'. Expected: '{uint.MaxValue / 2 - 0}'.");
                _counter++;
            }
        }
        ConfirmHalfMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfMinusOneIsFoldedCorrectly()
        {
            uint half = uint.MaxValue / 2;
            uint one = 1;

            if (BreakUpFlow())
                return;

            if (checked(half - one) != uint.MaxValue / 2 - 1)
            {
                Console.WriteLine($"'checked(half - one)' was evaluted to '{checked(half - one)}'. Expected: '{uint.MaxValue / 2 - 1}'.");
                _counter++;
            }
        }
        ConfirmHalfMinusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfMinusHalfIsFoldedCorrectly()
        {
            uint half = uint.MaxValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(half - half) != uint.MaxValue / 2 - uint.MaxValue / 2)
            {
                Console.WriteLine($"'checked(half - half)' was evaluted to '{checked(half - half)}'. Expected: '{uint.MaxValue / 2 - uint.MaxValue / 2}'.");
                _counter++;
            }
        }
        ConfirmHalfMinusMaxOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfMinusMaxOverflows()
        {
            uint half = uint.MaxValue / 2;
            uint max = uint.MaxValue;

            _counter++;
            try
            {
                _ = checked(half - max);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(half - max)' did not throw OverflowException.");
        }
        ConfirmMaxMinusZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxMinusZeroIsFoldedCorrectly()
        {
            uint max = uint.MaxValue;
            uint zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(max - zero) != uint.MaxValue - 0)
            {
                Console.WriteLine($"'checked(max - zero)' was evaluted to '{checked(max - zero)}'. Expected: '{uint.MaxValue - 0}'.");
                _counter++;
            }
        }
        ConfirmMaxMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxMinusOneIsFoldedCorrectly()
        {
            uint max = uint.MaxValue;
            uint one = 1;

            if (BreakUpFlow())
                return;

            if (checked(max - one) != uint.MaxValue - 1)
            {
                Console.WriteLine($"'checked(max - one)' was evaluted to '{checked(max - one)}'. Expected: '{uint.MaxValue - 1}'.");
                _counter++;
            }
        }
        ConfirmMaxMinusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxMinusHalfIsFoldedCorrectly()
        {
            uint max = uint.MaxValue;
            uint half = uint.MaxValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(max - half) != uint.MaxValue - uint.MaxValue / 2)
            {
                Console.WriteLine($"'checked(max - half)' was evaluted to '{checked(max - half)}'. Expected: '{uint.MaxValue - uint.MaxValue / 2}'.");
                _counter++;
            }
        }
        ConfirmMaxMinusMaxIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxMinusMaxIsFoldedCorrectly()
        {
            uint max = uint.MaxValue;

            if (BreakUpFlow())
                return;

            if (checked(max - max) != uint.MaxValue - uint.MaxValue)
            {
                Console.WriteLine($"'checked(max - max)' was evaluted to '{checked(max - max)}'. Expected: '{uint.MaxValue - uint.MaxValue}'.");
                _counter++;
            }
        }

        ConfirmZeroMultipliedByZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroMultipliedByZeroIsFoldedCorrectly()
        {
            uint zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(zero * zero) != 0 * 0)
            {
                Console.WriteLine($"'checked(zero * zero)' was evaluted to '{checked(zero * zero)}'. Expected: '{0 * 0}'.");
                _counter++;
            }
        }
        ConfirmZeroMultipliedByOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroMultipliedByOneIsFoldedCorrectly()
        {
            uint zero = 0;
            uint one = 1;

            if (BreakUpFlow())
                return;

            if (checked(zero * one) != 0 * 1)
            {
                Console.WriteLine($"'checked(zero * one)' was evaluted to '{checked(zero * one)}'. Expected: '{0 * 1}'.");
                _counter++;
            }
        }
        ConfirmZeroMultipliedByHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroMultipliedByHalfIsFoldedCorrectly()
        {
            uint zero = 0;
            uint half = (uint.MaxValue / 2);

            if (BreakUpFlow())
                return;

            if (checked(zero * half) != 0 * (uint.MaxValue / 2))
            {
                Console.WriteLine($"'checked(zero * half)' was evaluted to '{checked(zero * half)}'. Expected: '{0 * (uint.MaxValue / 2)}'.");
                _counter++;
            }
        }
        ConfirmZeroMultipliedByMaxIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroMultipliedByMaxIsFoldedCorrectly()
        {
            uint zero = 0;
            uint max = uint.MaxValue;

            if (BreakUpFlow())
                return;

            if (checked(zero * max) != 0 * uint.MaxValue)
            {
                Console.WriteLine($"'checked(zero * max)' was evaluted to '{checked(zero * max)}'. Expected: '{0 * uint.MaxValue}'.");
                _counter++;
            }
        }
        ConfirmOneMultipliedByZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOneMultipliedByZeroIsFoldedCorrectly()
        {
            uint one = 1;
            uint zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(one * zero) != 1 * 0)
            {
                Console.WriteLine($"'checked(one * zero)' was evaluted to '{checked(one * zero)}'. Expected: '{1 * 0}'.");
                _counter++;
            }
        }
        ConfirmOneMultipliedByOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOneMultipliedByOneIsFoldedCorrectly()
        {
            uint one = 1;

            if (BreakUpFlow())
                return;

            if (checked(one * one) != 1 * 1)
            {
                Console.WriteLine($"'checked(one * one)' was evaluted to '{checked(one * one)}'. Expected: '{1 * 1}'.");
                _counter++;
            }
        }
        ConfirmOneMultipliedByHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOneMultipliedByHalfIsFoldedCorrectly()
        {
            uint one = 1;
            uint half = (uint.MaxValue / 2);

            if (BreakUpFlow())
                return;

            if (checked(one * half) != 1 * (uint.MaxValue / 2))
            {
                Console.WriteLine($"'checked(one * half)' was evaluted to '{checked(one * half)}'. Expected: '{1 * (uint.MaxValue / 2)}'.");
                _counter++;
            }
        }
        ConfirmOneMultipliedByMaxIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOneMultipliedByMaxIsFoldedCorrectly()
        {
            uint one = 1;
            uint max = uint.MaxValue;

            if (BreakUpFlow())
                return;

            if (checked(one * max) != 1 * uint.MaxValue)
            {
                Console.WriteLine($"'checked(one * max)' was evaluted to '{checked(one * max)}'. Expected: '{1 * uint.MaxValue}'.");
                _counter++;
            }
        }
        ConfirmHalfMultipliedByZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfMultipliedByZeroIsFoldedCorrectly()
        {
            uint half = uint.MaxValue / 2;
            uint zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(half * zero) != uint.MaxValue / 2 * 0)
            {
                Console.WriteLine($"'checked(half * zero)' was evaluted to '{checked(half * zero)}'. Expected: '{uint.MaxValue / 2 * 0}'.");
                _counter++;
            }
        }
        ConfirmHalfMultipliedByOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfMultipliedByOneIsFoldedCorrectly()
        {
            uint half = uint.MaxValue / 2;
            uint one = 1;

            if (BreakUpFlow())
                return;

            if (checked(half * one) != uint.MaxValue / 2 * 1)
            {
                Console.WriteLine($"'checked(half * one)' was evaluted to '{checked(half * one)}'. Expected: '{uint.MaxValue / 2 * 1}'.");
                _counter++;
            }
        }
        ConfirmHalfMultipliedByHalfOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfMultipliedByHalfOverflows()
        {
            uint half = uint.MaxValue / 2;

            _counter++;
            try
            {
                _ = checked(half * half);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(half * half)' did not throw OverflowException.");
        }
        ConfirmHalfMultipliedByMaxOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfMultipliedByMaxOverflows()
        {
            uint half = uint.MaxValue / 2;
            uint max = uint.MaxValue;

            _counter++;
            try
            {
                _ = checked(half * max);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(half * max)' did not throw OverflowException.");
        }
        ConfirmMaxMultipliedByZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxMultipliedByZeroIsFoldedCorrectly()
        {
            uint max = uint.MaxValue;
            uint zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(max * zero) != uint.MaxValue * 0)
            {
                Console.WriteLine($"'checked(max * zero)' was evaluted to '{checked(max * zero)}'. Expected: '{uint.MaxValue * 0}'.");
                _counter++;
            }
        }
        ConfirmMaxMultipliedByOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxMultipliedByOneIsFoldedCorrectly()
        {
            uint max = uint.MaxValue;
            uint one = 1;

            if (BreakUpFlow())
                return;

            if (checked(max * one) != uint.MaxValue * 1)
            {
                Console.WriteLine($"'checked(max * one)' was evaluted to '{checked(max * one)}'. Expected: '{uint.MaxValue * 1}'.");
                _counter++;
            }
        }
        ConfirmMaxMultipliedByHalfOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxMultipliedByHalfOverflows()
        {
            uint max = uint.MaxValue;
            uint half = (uint.MaxValue / 2);

            _counter++;
            try
            {
                _ = checked(max * half);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(max * half)' did not throw OverflowException.");
        }
        ConfirmMaxMultipliedByMaxOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxMultipliedByMaxOverflows()
        {
            uint max = uint.MaxValue;

            _counter++;
            try
            {
                _ = checked(max * max);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(max * max)' did not throw OverflowException.");
        }
    }

    private static void TestInt64()
    {
        ConfirmAdditionIdentities(42);
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmAdditionIdentities(long value)
        {
            long zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(value + zero) != value)
            {
                Console.WriteLine($"Addition identity for long 'checked(value + zero)' was evaluted to '{checked(value + zero)}'. Expected: '{value}'.");
                _counter++;
            }

            if (checked(zero + value) != value)
            {
                Console.WriteLine($"Addition identity for long 'checked(zero + value)' was evaluted to '{checked(zero + value)}'. Expected: '{value}'.");
                _counter++;
            }
        }
        ConfirmSubtractionIdentities(42);
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSubtractionIdentities(long value)
        {
            long zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(value - zero) != value)
            {
                Console.WriteLine($"Subtraction identity for long 'checked(value - zero)' was evaluted to '{checked(value - zero)}'. Expected: '{value}'.");
                _counter++;
            }

            if (checked(value - value) != 0)
            {
                Console.WriteLine($"Subtraction identity for long 'checked(value - value)' was evaluted to '{checked(value - value)}'. Expected: '{0}'.");
                _counter++;
            }
        }
        ConfirmMultiplicationIdentities(42);
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMultiplicationIdentities(long value)
        {
            long zero = 0;
            long one = 1;

            if (BreakUpFlow())
                return;

            if (checked(value * zero) != 0)
            {
                Console.WriteLine($"Multiplication identity for long 'checked(value * zero)' was evaluted to '{checked(value * zero)}'. Expected: '{0}'.");
                _counter++;
            }

            if (checked(zero * value) != 0)
            {
                Console.WriteLine($"Multiplication identity for long 'checked(zero * value)' was evaluted to '{checked(zero * value)}'. Expected: '{0}'.");
                _counter++;
            }

            if (checked(value * one) != value)
            {
                Console.WriteLine($"Multiplication identity for long 'checked(value * one)' was evaluted to '{checked(value * one)}'. Expected: '{value}'.");
                _counter++;
            }

            if (checked(one * value) != value)
            {
                Console.WriteLine($"Multiplication identity for long 'checked(one * value)' was evaluted to '{checked(one * value)}'. Expected: '{value}'.");
                _counter++;
            }
        }
        ConfirmMinPlusMinOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinPlusMinOverflows()
        {
            long min = long.MinValue;

            _counter++;
            try
            {
                _ = checked(min + min);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(min + min)' did not throw OverflowException.");
        }
        ConfirmMinPlusMinusHalfOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinPlusMinusHalfOverflows()
        {
            long min = long.MinValue;
            long minusHalf = long.MinValue / 2;

            _counter++;
            try
            {
                _ = checked(min + minusHalf);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(min + minusHalf)' did not throw OverflowException.");
        }
        ConfirmMinPlusMinusOneOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinPlusMinusOneOverflows()
        {
            long min = long.MinValue;
            long minusOne = -1;

            _counter++;
            try
            {
                _ = checked(min + minusOne);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(min + minusOne)' did not throw OverflowException.");
        }
        ConfirmMinPlusZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinPlusZeroIsFoldedCorrectly()
        {
            long min = long.MinValue;
            long zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(min + zero) != long.MinValue + 0)
            {
                Console.WriteLine($"'checked(min + zero)' was evaluted to '{checked(min + zero)}'. Expected: '{long.MinValue + 0}'.");
                _counter++;
            }
        }
        ConfirmMinPlusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinPlusOneIsFoldedCorrectly()
        {
            long min = long.MinValue;
            long one = 1;

            if (BreakUpFlow())
                return;

            if (checked(min + one) != long.MinValue + 1)
            {
                Console.WriteLine($"'checked(min + one)' was evaluted to '{checked(min + one)}'. Expected: '{long.MinValue + 1}'.");
                _counter++;
            }
        }
        ConfirmMinPlusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinPlusHalfIsFoldedCorrectly()
        {
            long min = long.MinValue;
            long half = long.MaxValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(min + half) != long.MinValue + long.MaxValue / 2)
            {
                Console.WriteLine($"'checked(min + half)' was evaluted to '{checked(min + half)}'. Expected: '{long.MinValue + long.MaxValue / 2}'.");
                _counter++;
            }
        }
        ConfirmMinPlusMaxIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinPlusMaxIsFoldedCorrectly()
        {
            long min = long.MinValue;
            long max = long.MaxValue;

            if (BreakUpFlow())
                return;

            if (checked(min + max) != long.MinValue + long.MaxValue)
            {
                Console.WriteLine($"'checked(min + max)' was evaluted to '{checked(min + max)}'. Expected: '{long.MinValue + long.MaxValue}'.");
                _counter++;
            }
        }
        ConfirmMinusHalfPlusMinOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusHalfPlusMinOverflows()
        {
            long minusHalf = long.MinValue / 2;
            long min = long.MinValue;

            _counter++;
            try
            {
                _ = checked(minusHalf + min);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(minusHalf + min)' did not throw OverflowException.");
        }
        ConfirmMinusHalfPlusMinusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusHalfPlusMinusHalfIsFoldedCorrectly()
        {
            long minusHalf = long.MinValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(minusHalf + minusHalf) != long.MinValue / 2 + long.MinValue / 2)
            {
                Console.WriteLine($"'checked(minusHalf + minusHalf)' was evaluted to '{checked(minusHalf + minusHalf)}'. Expected: '{long.MinValue / 2 + long.MinValue / 2}'.");
                _counter++;
            }
        }
        ConfirmMinusHalfPlusMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusHalfPlusMinusOneIsFoldedCorrectly()
        {
            long minusHalf = long.MinValue / 2;
            long minusOne = -1;

            if (BreakUpFlow())
                return;

            if (checked(minusHalf + minusOne) != long.MinValue / 2 + -1)
            {
                Console.WriteLine($"'checked(minusHalf + minusOne)' was evaluted to '{checked(minusHalf + minusOne)}'. Expected: '{long.MinValue / 2 + -1}'.");
                _counter++;
            }
        }
        ConfirmMinusHalfPlusZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusHalfPlusZeroIsFoldedCorrectly()
        {
            long minusHalf = long.MinValue / 2;
            long zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(minusHalf + zero) != long.MinValue / 2 + 0)
            {
                Console.WriteLine($"'checked(minusHalf + zero)' was evaluted to '{checked(minusHalf + zero)}'. Expected: '{long.MinValue / 2 + 0}'.");
                _counter++;
            }
        }
        ConfirmMinusHalfPlusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusHalfPlusOneIsFoldedCorrectly()
        {
            long minusHalf = long.MinValue / 2;
            long one = 1;

            if (BreakUpFlow())
                return;

            if (checked(minusHalf + one) != long.MinValue / 2 + 1)
            {
                Console.WriteLine($"'checked(minusHalf + one)' was evaluted to '{checked(minusHalf + one)}'. Expected: '{long.MinValue / 2 + 1}'.");
                _counter++;
            }
        }
        ConfirmMinusHalfPlusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusHalfPlusHalfIsFoldedCorrectly()
        {
            long minusHalf = long.MinValue / 2;
            long half = long.MaxValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(minusHalf + half) != long.MinValue / 2 + long.MaxValue / 2)
            {
                Console.WriteLine($"'checked(minusHalf + half)' was evaluted to '{checked(minusHalf + half)}'. Expected: '{long.MinValue / 2 + long.MaxValue / 2}'.");
                _counter++;
            }
        }
        ConfirmMinusHalfPlusMaxIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusHalfPlusMaxIsFoldedCorrectly()
        {
            long minusHalf = long.MinValue / 2;
            long max = long.MaxValue;

            if (BreakUpFlow())
                return;

            if (checked(minusHalf + max) != long.MinValue / 2 + long.MaxValue)
            {
                Console.WriteLine($"'checked(minusHalf + max)' was evaluted to '{checked(minusHalf + max)}'. Expected: '{long.MinValue / 2 + long.MaxValue}'.");
                _counter++;
            }
        }
        ConfirmMinusOnePlusMinOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusOnePlusMinOverflows()
        {
            long minusOne = -1;
            long min = long.MinValue;

            _counter++;
            try
            {
                _ = checked(minusOne + min);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(minusOne + min)' did not throw OverflowException.");
        }
        ConfirmMinusOnePlusMinusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusOnePlusMinusHalfIsFoldedCorrectly()
        {
            long minusOne = -1;
            long minusHalf = long.MinValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(minusOne + minusHalf) != -1 + long.MinValue / 2)
            {
                Console.WriteLine($"'checked(minusOne + minusHalf)' was evaluted to '{checked(minusOne + minusHalf)}'. Expected: '{-1 + long.MinValue / 2}'.");
                _counter++;
            }
        }
        ConfirmMinusOnePlusMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusOnePlusMinusOneIsFoldedCorrectly()
        {
            long minusOne = -1;

            if (BreakUpFlow())
                return;

            if (checked(minusOne + minusOne) != -1 + -1)
            {
                Console.WriteLine($"'checked(minusOne + minusOne)' was evaluted to '{checked(minusOne + minusOne)}'. Expected: '{-1 + -1}'.");
                _counter++;
            }
        }
        ConfirmMinusOnePlusZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusOnePlusZeroIsFoldedCorrectly()
        {
            long minusOne = -1;
            long zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(minusOne + zero) != -1 + 0)
            {
                Console.WriteLine($"'checked(minusOne + zero)' was evaluted to '{checked(minusOne + zero)}'. Expected: '{-1 + 0}'.");
                _counter++;
            }
        }
        ConfirmMinusOnePlusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusOnePlusOneIsFoldedCorrectly()
        {
            long minusOne = -1;
            long one = 1;

            if (BreakUpFlow())
                return;

            if (checked(minusOne + one) != -1 + 1)
            {
                Console.WriteLine($"'checked(minusOne + one)' was evaluted to '{checked(minusOne + one)}'. Expected: '{-1 + 1}'.");
                _counter++;
            }
        }
        ConfirmMinusOnePlusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusOnePlusHalfIsFoldedCorrectly()
        {
            long minusOne = -1;
            long half = long.MaxValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(minusOne + half) != -1 + long.MaxValue / 2)
            {
                Console.WriteLine($"'checked(minusOne + half)' was evaluted to '{checked(minusOne + half)}'. Expected: '{-1 + long.MaxValue / 2}'.");
                _counter++;
            }
        }
        ConfirmMinusOnePlusMaxIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusOnePlusMaxIsFoldedCorrectly()
        {
            long minusOne = -1;
            long max = long.MaxValue;

            if (BreakUpFlow())
                return;

            if (checked(minusOne + max) != -1 + long.MaxValue)
            {
                Console.WriteLine($"'checked(minusOne + max)' was evaluted to '{checked(minusOne + max)}'. Expected: '{-1 + long.MaxValue}'.");
                _counter++;
            }
        }
        ConfirmZeroPlusMinIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroPlusMinIsFoldedCorrectly()
        {
            long zero = 0;
            long min = long.MinValue;

            if (BreakUpFlow())
                return;

            if (checked(zero + min) != 0 + long.MinValue)
            {
                Console.WriteLine($"'checked(zero + min)' was evaluted to '{checked(zero + min)}'. Expected: '{0 + long.MinValue}'.");
                _counter++;
            }
        }
        ConfirmZeroPlusMinusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroPlusMinusHalfIsFoldedCorrectly()
        {
            long zero = 0;
            long minusHalf = long.MinValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(zero + minusHalf) != 0 + long.MinValue / 2)
            {
                Console.WriteLine($"'checked(zero + minusHalf)' was evaluted to '{checked(zero + minusHalf)}'. Expected: '{0 + long.MinValue / 2}'.");
                _counter++;
            }
        }
        ConfirmZeroPlusMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroPlusMinusOneIsFoldedCorrectly()
        {
            long zero = 0;
            long minusOne = -1;

            if (BreakUpFlow())
                return;

            if (checked(zero + minusOne) != 0 + -1)
            {
                Console.WriteLine($"'checked(zero + minusOne)' was evaluted to '{checked(zero + minusOne)}'. Expected: '{0 + -1}'.");
                _counter++;
            }
        }
        ConfirmZeroPlusZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroPlusZeroIsFoldedCorrectly()
        {
            long zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(zero + zero) != 0 + 0)
            {
                Console.WriteLine($"'checked(zero + zero)' was evaluted to '{checked(zero + zero)}'. Expected: '{0 + 0}'.");
                _counter++;
            }
        }
        ConfirmZeroPlusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroPlusOneIsFoldedCorrectly()
        {
            long zero = 0;
            long one = 1;

            if (BreakUpFlow())
                return;

            if (checked(zero + one) != 0 + 1)
            {
                Console.WriteLine($"'checked(zero + one)' was evaluted to '{checked(zero + one)}'. Expected: '{0 + 1}'.");
                _counter++;
            }
        }
        ConfirmZeroPlusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroPlusHalfIsFoldedCorrectly()
        {
            long zero = 0;
            long half = long.MaxValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(zero + half) != 0 + long.MaxValue / 2)
            {
                Console.WriteLine($"'checked(zero + half)' was evaluted to '{checked(zero + half)}'. Expected: '{0 + long.MaxValue / 2}'.");
                _counter++;
            }
        }
        ConfirmZeroPlusMaxIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroPlusMaxIsFoldedCorrectly()
        {
            long zero = 0;
            long max = long.MaxValue;

            if (BreakUpFlow())
                return;

            if (checked(zero + max) != 0 + long.MaxValue)
            {
                Console.WriteLine($"'checked(zero + max)' was evaluted to '{checked(zero + max)}'. Expected: '{0 + long.MaxValue}'.");
                _counter++;
            }
        }
        ConfirmOnePlusMinIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOnePlusMinIsFoldedCorrectly()
        {
            long one = 1;
            long min = long.MinValue;

            if (BreakUpFlow())
                return;

            if (checked(one + min) != 1 + long.MinValue)
            {
                Console.WriteLine($"'checked(one + min)' was evaluted to '{checked(one + min)}'. Expected: '{1 + long.MinValue}'.");
                _counter++;
            }
        }
        ConfirmOnePlusMinusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOnePlusMinusHalfIsFoldedCorrectly()
        {
            long one = 1;
            long minusHalf = long.MinValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(one + minusHalf) != 1 + long.MinValue / 2)
            {
                Console.WriteLine($"'checked(one + minusHalf)' was evaluted to '{checked(one + minusHalf)}'. Expected: '{1 + long.MinValue / 2}'.");
                _counter++;
            }
        }
        ConfirmOnePlusMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOnePlusMinusOneIsFoldedCorrectly()
        {
            long one = 1;
            long minusOne = -1;

            if (BreakUpFlow())
                return;

            if (checked(one + minusOne) != 1 + -1)
            {
                Console.WriteLine($"'checked(one + minusOne)' was evaluted to '{checked(one + minusOne)}'. Expected: '{1 + -1}'.");
                _counter++;
            }
        }
        ConfirmOnePlusZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOnePlusZeroIsFoldedCorrectly()
        {
            long one = 1;
            long zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(one + zero) != 1 + 0)
            {
                Console.WriteLine($"'checked(one + zero)' was evaluted to '{checked(one + zero)}'. Expected: '{1 + 0}'.");
                _counter++;
            }
        }
        ConfirmOnePlusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOnePlusOneIsFoldedCorrectly()
        {
            long one = 1;

            if (BreakUpFlow())
                return;

            if (checked(one + one) != 1 + 1)
            {
                Console.WriteLine($"'checked(one + one)' was evaluted to '{checked(one + one)}'. Expected: '{1 + 1}'.");
                _counter++;
            }
        }
        ConfirmOnePlusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOnePlusHalfIsFoldedCorrectly()
        {
            long one = 1;
            long half = long.MaxValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(one + half) != 1 + long.MaxValue / 2)
            {
                Console.WriteLine($"'checked(one + half)' was evaluted to '{checked(one + half)}'. Expected: '{1 + long.MaxValue / 2}'.");
                _counter++;
            }
        }
        ConfirmOnePlusMaxOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOnePlusMaxOverflows()
        {
            long one = 1;
            long max = long.MaxValue;

            _counter++;
            try
            {
                _ = checked(one + max);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(one + max)' did not throw OverflowException.");
        }
        ConfirmHalfPlusMinIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfPlusMinIsFoldedCorrectly()
        {
            long half = long.MaxValue / 2;
            long min = long.MinValue;

            if (BreakUpFlow())
                return;

            if (checked(half + min) != long.MaxValue / 2 + long.MinValue)
            {
                Console.WriteLine($"'checked(half + min)' was evaluted to '{checked(half + min)}'. Expected: '{long.MaxValue / 2 + long.MinValue}'.");
                _counter++;
            }
        }
        ConfirmHalfPlusMinusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfPlusMinusHalfIsFoldedCorrectly()
        {
            long half = long.MaxValue / 2;
            long minusHalf = long.MinValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(half + minusHalf) != long.MaxValue / 2 + long.MinValue / 2)
            {
                Console.WriteLine($"'checked(half + minusHalf)' was evaluted to '{checked(half + minusHalf)}'. Expected: '{long.MaxValue / 2 + long.MinValue / 2}'.");
                _counter++;
            }
        }
        ConfirmHalfPlusMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfPlusMinusOneIsFoldedCorrectly()
        {
            long half = long.MaxValue / 2;
            long minusOne = -1;

            if (BreakUpFlow())
                return;

            if (checked(half + minusOne) != long.MaxValue / 2 + -1)
            {
                Console.WriteLine($"'checked(half + minusOne)' was evaluted to '{checked(half + minusOne)}'. Expected: '{long.MaxValue / 2 + -1}'.");
                _counter++;
            }
        }
        ConfirmHalfPlusZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfPlusZeroIsFoldedCorrectly()
        {
            long half = long.MaxValue / 2;
            long zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(half + zero) != long.MaxValue / 2 + 0)
            {
                Console.WriteLine($"'checked(half + zero)' was evaluted to '{checked(half + zero)}'. Expected: '{long.MaxValue / 2 + 0}'.");
                _counter++;
            }
        }
        ConfirmHalfPlusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfPlusOneIsFoldedCorrectly()
        {
            long half = long.MaxValue / 2;
            long one = 1;

            if (BreakUpFlow())
                return;

            if (checked(half + one) != long.MaxValue / 2 + 1)
            {
                Console.WriteLine($"'checked(half + one)' was evaluted to '{checked(half + one)}'. Expected: '{long.MaxValue / 2 + 1}'.");
                _counter++;
            }
        }
        ConfirmHalfPlusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfPlusHalfIsFoldedCorrectly()
        {
            long half = long.MaxValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(half + half) != long.MaxValue / 2 + long.MaxValue / 2)
            {
                Console.WriteLine($"'checked(half + half)' was evaluted to '{checked(half + half)}'. Expected: '{long.MaxValue / 2 + long.MaxValue / 2}'.");
                _counter++;
            }
        }
        ConfirmHalfPlusMaxOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfPlusMaxOverflows()
        {
            long half = long.MaxValue / 2;
            long max = long.MaxValue;

            _counter++;
            try
            {
                _ = checked(half + max);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(half + max)' did not throw OverflowException.");
        }
        ConfirmMaxPlusMinIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxPlusMinIsFoldedCorrectly()
        {
            long max = long.MaxValue;
            long min = long.MinValue;

            if (BreakUpFlow())
                return;

            if (checked(max + min) != long.MaxValue + long.MinValue)
            {
                Console.WriteLine($"'checked(max + min)' was evaluted to '{checked(max + min)}'. Expected: '{long.MaxValue + long.MinValue}'.");
                _counter++;
            }
        }
        ConfirmMaxPlusMinusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxPlusMinusHalfIsFoldedCorrectly()
        {
            long max = long.MaxValue;
            long minusHalf = long.MinValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(max + minusHalf) != long.MaxValue + long.MinValue / 2)
            {
                Console.WriteLine($"'checked(max + minusHalf)' was evaluted to '{checked(max + minusHalf)}'. Expected: '{long.MaxValue + long.MinValue / 2}'.");
                _counter++;
            }
        }
        ConfirmMaxPlusMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxPlusMinusOneIsFoldedCorrectly()
        {
            long max = long.MaxValue;
            long minusOne = -1;

            if (BreakUpFlow())
                return;

            if (checked(max + minusOne) != long.MaxValue + -1)
            {
                Console.WriteLine($"'checked(max + minusOne)' was evaluted to '{checked(max + minusOne)}'. Expected: '{long.MaxValue + -1}'.");
                _counter++;
            }
        }
        ConfirmMaxPlusZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxPlusZeroIsFoldedCorrectly()
        {
            long max = long.MaxValue;
            long zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(max + zero) != long.MaxValue + 0)
            {
                Console.WriteLine($"'checked(max + zero)' was evaluted to '{checked(max + zero)}'. Expected: '{long.MaxValue + 0}'.");
                _counter++;
            }
        }
        ConfirmMaxPlusOneOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxPlusOneOverflows()
        {
            long max = long.MaxValue;
            long one = 1;

            _counter++;
            try
            {
                _ = checked(max + one);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(max + one)' did not throw OverflowException.");
        }
        ConfirmMaxPlusHalfOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxPlusHalfOverflows()
        {
            long max = long.MaxValue;
            long half = long.MaxValue / 2;

            _counter++;
            try
            {
                _ = checked(max + half);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(max + half)' did not throw OverflowException.");
        }
        ConfirmMaxPlusMaxOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxPlusMaxOverflows()
        {
            long max = long.MaxValue;

            _counter++;
            try
            {
                _ = checked(max + max);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(max + max)' did not throw OverflowException.");
        }

        ConfirmMinMinusMinIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinMinusMinIsFoldedCorrectly()
        {
            long min = long.MinValue;

            if (BreakUpFlow())
                return;

            if (checked(min - min) != long.MinValue - long.MinValue)
            {
                Console.WriteLine($"'checked(min - min)' was evaluted to '{checked(min - min)}'. Expected: '{long.MinValue - long.MinValue}'.");
                _counter++;
            }
        }
        ConfirmMinMinusMinusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinMinusMinusHalfIsFoldedCorrectly()
        {
            long min = long.MinValue;
            long minusHalf = long.MinValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(min - minusHalf) != long.MinValue - long.MinValue / 2)
            {
                Console.WriteLine($"'checked(min - minusHalf)' was evaluted to '{checked(min - minusHalf)}'. Expected: '{long.MinValue - long.MinValue / 2}'.");
                _counter++;
            }
        }
        ConfirmMinMinusMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinMinusMinusOneIsFoldedCorrectly()
        {
            long min = long.MinValue;
            long minusOne = -1;

            if (BreakUpFlow())
                return;

            if (checked(min - minusOne) != long.MinValue - -1)
            {
                Console.WriteLine($"'checked(min - minusOne)' was evaluted to '{checked(min - minusOne)}'. Expected: '{long.MinValue - -1}'.");
                _counter++;
            }
        }
        ConfirmMinMinusZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinMinusZeroIsFoldedCorrectly()
        {
            long min = long.MinValue;
            long zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(min - zero) != long.MinValue - 0)
            {
                Console.WriteLine($"'checked(min - zero)' was evaluted to '{checked(min - zero)}'. Expected: '{long.MinValue - 0}'.");
                _counter++;
            }
        }
        ConfirmMinMinusOneOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinMinusOneOverflows()
        {
            long min = long.MinValue;
            long one = 1;

            _counter++;
            try
            {
                _ = checked(min - one);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(min - one)' did not throw OverflowException.");
        }
        ConfirmMinMinusHalfOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinMinusHalfOverflows()
        {
            long min = long.MinValue;
            long half = long.MaxValue / 2;

            _counter++;
            try
            {
                _ = checked(min - half);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(min - half)' did not throw OverflowException.");
        }
        ConfirmMinMinusMaxOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinMinusMaxOverflows()
        {
            long min = long.MinValue;
            long max = long.MaxValue;

            _counter++;
            try
            {
                _ = checked(min - max);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(min - max)' did not throw OverflowException.");
        }
        ConfirmMinusHalfMinusMinIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusHalfMinusMinIsFoldedCorrectly()
        {
            long minusHalf = long.MinValue / 2;
            long min = long.MinValue;

            if (BreakUpFlow())
                return;

            if (checked(minusHalf - min) != long.MinValue / 2 - long.MinValue)
            {
                Console.WriteLine($"'checked(minusHalf - min)' was evaluted to '{checked(minusHalf - min)}'. Expected: '{long.MinValue / 2 - long.MinValue}'.");
                _counter++;
            }
        }
        ConfirmMinusHalfMinusMinusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusHalfMinusMinusHalfIsFoldedCorrectly()
        {
            long minusHalf = long.MinValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(minusHalf - minusHalf) != long.MinValue / 2 - long.MinValue / 2)
            {
                Console.WriteLine($"'checked(minusHalf - minusHalf)' was evaluted to '{checked(minusHalf - minusHalf)}'. Expected: '{long.MinValue / 2 - long.MinValue / 2}'.");
                _counter++;
            }
        }
        ConfirmMinusHalfMinusMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusHalfMinusMinusOneIsFoldedCorrectly()
        {
            long minusHalf = long.MinValue / 2;
            long minusOne = -1;

            if (BreakUpFlow())
                return;

            if (checked(minusHalf - minusOne) != long.MinValue / 2 - -1)
            {
                Console.WriteLine($"'checked(minusHalf - minusOne)' was evaluted to '{checked(minusHalf - minusOne)}'. Expected: '{long.MinValue / 2 - -1}'.");
                _counter++;
            }
        }
        ConfirmMinusHalfMinusZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusHalfMinusZeroIsFoldedCorrectly()
        {
            long minusHalf = long.MinValue / 2;
            long zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(minusHalf - zero) != long.MinValue / 2 - 0)
            {
                Console.WriteLine($"'checked(minusHalf - zero)' was evaluted to '{checked(minusHalf - zero)}'. Expected: '{long.MinValue / 2 - 0}'.");
                _counter++;
            }
        }
        ConfirmMinusHalfMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusHalfMinusOneIsFoldedCorrectly()
        {
            long minusHalf = long.MinValue / 2;
            long one = 1;

            if (BreakUpFlow())
                return;

            if (checked(minusHalf - one) != long.MinValue / 2 - 1)
            {
                Console.WriteLine($"'checked(minusHalf - one)' was evaluted to '{checked(minusHalf - one)}'. Expected: '{long.MinValue / 2 - 1}'.");
                _counter++;
            }
        }
        ConfirmMinusHalfMinusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusHalfMinusHalfIsFoldedCorrectly()
        {
            long minusHalf = long.MinValue / 2;
            long half = long.MaxValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(minusHalf - half) != long.MinValue / 2 - long.MaxValue / 2)
            {
                Console.WriteLine($"'checked(minusHalf - half)' was evaluted to '{checked(minusHalf - half)}'. Expected: '{long.MinValue / 2 - long.MaxValue / 2}'.");
                _counter++;
            }
        }
        ConfirmMinusHalfMinusMaxOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusHalfMinusMaxOverflows()
        {
            long minusHalf = long.MinValue / 2;
            long max = long.MaxValue;

            _counter++;
            try
            {
                _ = checked(minusHalf - max);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(minusHalf - max)' did not throw OverflowException.");
        }
        ConfirmMinusOneMinusMinIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusOneMinusMinIsFoldedCorrectly()
        {
            long minusOne = -1;
            long min = long.MinValue;

            if (BreakUpFlow())
                return;

            if (checked(minusOne - min) != -1 - long.MinValue)
            {
                Console.WriteLine($"'checked(minusOne - min)' was evaluted to '{checked(minusOne - min)}'. Expected: '{-1 - long.MinValue}'.");
                _counter++;
            }
        }
        ConfirmMinusOneMinusMinusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusOneMinusMinusHalfIsFoldedCorrectly()
        {
            long minusOne = -1;
            long minusHalf = long.MinValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(minusOne - minusHalf) != -1 - long.MinValue / 2)
            {
                Console.WriteLine($"'checked(minusOne - minusHalf)' was evaluted to '{checked(minusOne - minusHalf)}'. Expected: '{-1 - long.MinValue / 2}'.");
                _counter++;
            }
        }
        ConfirmMinusOneMinusMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusOneMinusMinusOneIsFoldedCorrectly()
        {
            long minusOne = -1;

            if (BreakUpFlow())
                return;

            if (checked(minusOne - minusOne) != -1 - -1)
            {
                Console.WriteLine($"'checked(minusOne - minusOne)' was evaluted to '{checked(minusOne - minusOne)}'. Expected: '{-1 - -1}'.");
                _counter++;
            }
        }
        ConfirmMinusOneMinusZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusOneMinusZeroIsFoldedCorrectly()
        {
            long minusOne = -1;
            long zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(minusOne - zero) != -1 - 0)
            {
                Console.WriteLine($"'checked(minusOne - zero)' was evaluted to '{checked(minusOne - zero)}'. Expected: '{-1 - 0}'.");
                _counter++;
            }
        }
        ConfirmMinusOneMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusOneMinusOneIsFoldedCorrectly()
        {
            long minusOne = -1;
            long one = 1;

            if (BreakUpFlow())
                return;

            if (checked(minusOne - one) != -1 - 1)
            {
                Console.WriteLine($"'checked(minusOne - one)' was evaluted to '{checked(minusOne - one)}'. Expected: '{-1 - 1}'.");
                _counter++;
            }
        }
        ConfirmMinusOneMinusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusOneMinusHalfIsFoldedCorrectly()
        {
            long minusOne = -1;
            long half = long.MaxValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(minusOne - half) != -1 - long.MaxValue / 2)
            {
                Console.WriteLine($"'checked(minusOne - half)' was evaluted to '{checked(minusOne - half)}'. Expected: '{-1 - long.MaxValue / 2}'.");
                _counter++;
            }
        }
        ConfirmMinusOneMinusMaxIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusOneMinusMaxIsFoldedCorrectly()
        {
            long minusOne = -1;
            long max = long.MaxValue;

            if (BreakUpFlow())
                return;

            if (checked(minusOne - max) != -1 - long.MaxValue)
            {
                Console.WriteLine($"'checked(minusOne - max)' was evaluted to '{checked(minusOne - max)}'. Expected: '{-1 - long.MaxValue}'.");
                _counter++;
            }
        }
        ConfirmZeroMinusMinOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroMinusMinOverflows()
        {
            long zero = 0;
            long min = long.MinValue;

            _counter++;
            try
            {
                _ = checked(zero - min);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(zero - min)' did not throw OverflowException.");
        }
        ConfirmZeroMinusMinusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroMinusMinusHalfIsFoldedCorrectly()
        {
            long zero = 0;
            long minusHalf = long.MinValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(zero - minusHalf) != 0 - long.MinValue / 2)
            {
                Console.WriteLine($"'checked(zero - minusHalf)' was evaluted to '{checked(zero - minusHalf)}'. Expected: '{0 - long.MinValue / 2}'.");
                _counter++;
            }
        }
        ConfirmZeroMinusMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroMinusMinusOneIsFoldedCorrectly()
        {
            long zero = 0;
            long minusOne = -1;

            if (BreakUpFlow())
                return;

            if (checked(zero - minusOne) != 0 - -1)
            {
                Console.WriteLine($"'checked(zero - minusOne)' was evaluted to '{checked(zero - minusOne)}'. Expected: '{0 - -1}'.");
                _counter++;
            }
        }
        ConfirmZeroMinusZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroMinusZeroIsFoldedCorrectly()
        {
            long zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(zero - zero) != 0 - 0)
            {
                Console.WriteLine($"'checked(zero - zero)' was evaluted to '{checked(zero - zero)}'. Expected: '{0 - 0}'.");
                _counter++;
            }
        }
        ConfirmZeroMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroMinusOneIsFoldedCorrectly()
        {
            long zero = 0;
            long one = 1;

            if (BreakUpFlow())
                return;

            if (checked(zero - one) != 0 - 1)
            {
                Console.WriteLine($"'checked(zero - one)' was evaluted to '{checked(zero - one)}'. Expected: '{0 - 1}'.");
                _counter++;
            }
        }
        ConfirmZeroMinusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroMinusHalfIsFoldedCorrectly()
        {
            long zero = 0;
            long half = long.MaxValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(zero - half) != 0 - long.MaxValue / 2)
            {
                Console.WriteLine($"'checked(zero - half)' was evaluted to '{checked(zero - half)}'. Expected: '{0 - long.MaxValue / 2}'.");
                _counter++;
            }
        }
        ConfirmZeroMinusMaxIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroMinusMaxIsFoldedCorrectly()
        {
            long zero = 0;
            long max = long.MaxValue;

            if (BreakUpFlow())
                return;

            if (checked(zero - max) != 0 - long.MaxValue)
            {
                Console.WriteLine($"'checked(zero - max)' was evaluted to '{checked(zero - max)}'. Expected: '{0 - long.MaxValue}'.");
                _counter++;
            }
        }
        ConfirmOneMinusMinOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOneMinusMinOverflows()
        {
            long one = 1;
            long min = long.MinValue;

            _counter++;
            try
            {
                _ = checked(one - min);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(one - min)' did not throw OverflowException.");
        }
        ConfirmOneMinusMinusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOneMinusMinusHalfIsFoldedCorrectly()
        {
            long one = 1;
            long minusHalf = long.MinValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(one - minusHalf) != 1 - long.MinValue / 2)
            {
                Console.WriteLine($"'checked(one - minusHalf)' was evaluted to '{checked(one - minusHalf)}'. Expected: '{1 - long.MinValue / 2}'.");
                _counter++;
            }
        }
        ConfirmOneMinusMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOneMinusMinusOneIsFoldedCorrectly()
        {
            long one = 1;
            long minusOne = -1;

            if (BreakUpFlow())
                return;

            if (checked(one - minusOne) != 1 - -1)
            {
                Console.WriteLine($"'checked(one - minusOne)' was evaluted to '{checked(one - minusOne)}'. Expected: '{1 - -1}'.");
                _counter++;
            }
        }
        ConfirmOneMinusZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOneMinusZeroIsFoldedCorrectly()
        {
            long one = 1;
            long zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(one - zero) != 1 - 0)
            {
                Console.WriteLine($"'checked(one - zero)' was evaluted to '{checked(one - zero)}'. Expected: '{1 - 0}'.");
                _counter++;
            }
        }
        ConfirmOneMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOneMinusOneIsFoldedCorrectly()
        {
            long one = 1;

            if (BreakUpFlow())
                return;

            if (checked(one - one) != 1 - 1)
            {
                Console.WriteLine($"'checked(one - one)' was evaluted to '{checked(one - one)}'. Expected: '{1 - 1}'.");
                _counter++;
            }
        }
        ConfirmOneMinusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOneMinusHalfIsFoldedCorrectly()
        {
            long one = 1;
            long half = long.MaxValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(one - half) != 1 - long.MaxValue / 2)
            {
                Console.WriteLine($"'checked(one - half)' was evaluted to '{checked(one - half)}'. Expected: '{1 - long.MaxValue / 2}'.");
                _counter++;
            }
        }
        ConfirmOneMinusMaxIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOneMinusMaxIsFoldedCorrectly()
        {
            long one = 1;
            long max = long.MaxValue;

            if (BreakUpFlow())
                return;

            if (checked(one - max) != 1 - long.MaxValue)
            {
                Console.WriteLine($"'checked(one - max)' was evaluted to '{checked(one - max)}'. Expected: '{1 - long.MaxValue}'.");
                _counter++;
            }
        }
        ConfirmHalfMinusMinOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfMinusMinOverflows()
        {
            long half = long.MaxValue / 2;
            long min = long.MinValue;

            _counter++;
            try
            {
                _ = checked(half - min);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(half - min)' did not throw OverflowException.");
        }
        ConfirmHalfMinusMinusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfMinusMinusHalfIsFoldedCorrectly()
        {
            long half = long.MaxValue / 2;
            long minusHalf = long.MinValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(half - minusHalf) != long.MaxValue / 2 - long.MinValue / 2)
            {
                Console.WriteLine($"'checked(half - minusHalf)' was evaluted to '{checked(half - minusHalf)}'. Expected: '{long.MaxValue / 2 - long.MinValue / 2}'.");
                _counter++;
            }
        }
        ConfirmHalfMinusMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfMinusMinusOneIsFoldedCorrectly()
        {
            long half = long.MaxValue / 2;
            long minusOne = -1;

            if (BreakUpFlow())
                return;

            if (checked(half - minusOne) != long.MaxValue / 2 - -1)
            {
                Console.WriteLine($"'checked(half - minusOne)' was evaluted to '{checked(half - minusOne)}'. Expected: '{long.MaxValue / 2 - -1}'.");
                _counter++;
            }
        }
        ConfirmHalfMinusZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfMinusZeroIsFoldedCorrectly()
        {
            long half = long.MaxValue / 2;
            long zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(half - zero) != long.MaxValue / 2 - 0)
            {
                Console.WriteLine($"'checked(half - zero)' was evaluted to '{checked(half - zero)}'. Expected: '{long.MaxValue / 2 - 0}'.");
                _counter++;
            }
        }
        ConfirmHalfMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfMinusOneIsFoldedCorrectly()
        {
            long half = long.MaxValue / 2;
            long one = 1;

            if (BreakUpFlow())
                return;

            if (checked(half - one) != long.MaxValue / 2 - 1)
            {
                Console.WriteLine($"'checked(half - one)' was evaluted to '{checked(half - one)}'. Expected: '{long.MaxValue / 2 - 1}'.");
                _counter++;
            }
        }
        ConfirmHalfMinusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfMinusHalfIsFoldedCorrectly()
        {
            long half = long.MaxValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(half - half) != long.MaxValue / 2 - long.MaxValue / 2)
            {
                Console.WriteLine($"'checked(half - half)' was evaluted to '{checked(half - half)}'. Expected: '{long.MaxValue / 2 - long.MaxValue / 2}'.");
                _counter++;
            }
        }
        ConfirmHalfMinusMaxIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfMinusMaxIsFoldedCorrectly()
        {
            long half = long.MaxValue / 2;
            long max = long.MaxValue;

            if (BreakUpFlow())
                return;

            if (checked(half - max) != long.MaxValue / 2 - long.MaxValue)
            {
                Console.WriteLine($"'checked(half - max)' was evaluted to '{checked(half - max)}'. Expected: '{long.MaxValue / 2 - long.MaxValue}'.");
                _counter++;
            }
        }
        ConfirmMaxMinusMinOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxMinusMinOverflows()
        {
            long max = long.MaxValue;
            long min = long.MinValue;

            _counter++;
            try
            {
                _ = checked(max - min);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(max - min)' did not throw OverflowException.");
        }
        ConfirmMaxMinusMinusHalfOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxMinusMinusHalfOverflows()
        {
            long max = long.MaxValue;
            long minusHalf = long.MinValue / 2;

            _counter++;
            try
            {
                _ = checked(max - minusHalf);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(max - minusHalf)' did not throw OverflowException.");
        }
        ConfirmMaxMinusMinusOneOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxMinusMinusOneOverflows()
        {
            long max = long.MaxValue;
            long minusOne = -1;

            _counter++;
            try
            {
                _ = checked(max - minusOne);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(max - minusOne)' did not throw OverflowException.");
        }
        ConfirmMaxMinusZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxMinusZeroIsFoldedCorrectly()
        {
            long max = long.MaxValue;
            long zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(max - zero) != long.MaxValue - 0)
            {
                Console.WriteLine($"'checked(max - zero)' was evaluted to '{checked(max - zero)}'. Expected: '{long.MaxValue - 0}'.");
                _counter++;
            }
        }
        ConfirmMaxMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxMinusOneIsFoldedCorrectly()
        {
            long max = long.MaxValue;
            long one = 1;

            if (BreakUpFlow())
                return;

            if (checked(max - one) != long.MaxValue - 1)
            {
                Console.WriteLine($"'checked(max - one)' was evaluted to '{checked(max - one)}'. Expected: '{long.MaxValue - 1}'.");
                _counter++;
            }
        }
        ConfirmMaxMinusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxMinusHalfIsFoldedCorrectly()
        {
            long max = long.MaxValue;
            long half = long.MaxValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(max - half) != long.MaxValue - long.MaxValue / 2)
            {
                Console.WriteLine($"'checked(max - half)' was evaluted to '{checked(max - half)}'. Expected: '{long.MaxValue - long.MaxValue / 2}'.");
                _counter++;
            }
        }
        ConfirmMaxMinusMaxIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxMinusMaxIsFoldedCorrectly()
        {
            long max = long.MaxValue;

            if (BreakUpFlow())
                return;

            if (checked(max - max) != long.MaxValue - long.MaxValue)
            {
                Console.WriteLine($"'checked(max - max)' was evaluted to '{checked(max - max)}'. Expected: '{long.MaxValue - long.MaxValue}'.");
                _counter++;
            }
        }

        ConfirmMinMultipliedByMinOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinMultipliedByMinOverflows()
        {
            long min = long.MinValue;

            _counter++;
            try
            {
                _ = checked(min * min);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(min * min)' did not throw OverflowException.");
        }
        ConfirmMinMultipliedByMinusHalfOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinMultipliedByMinusHalfOverflows()
        {
            long min = long.MinValue;
            long minusHalf = (long.MinValue / 2);

            _counter++;
            try
            {
                _ = checked(min * minusHalf);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(min * minusHalf)' did not throw OverflowException.");
        }
        ConfirmMinMultipliedByMinusOneOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinMultipliedByMinusOneOverflows()
        {
            long min = long.MinValue;
            long minusOne = -1;

            _counter++;
            try
            {
                _ = checked(min * minusOne);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(min * minusOne)' did not throw OverflowException.");
        }
        ConfirmMinMultipliedByZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinMultipliedByZeroIsFoldedCorrectly()
        {
            long min = long.MinValue;
            long zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(min * zero) != long.MinValue * 0)
            {
                Console.WriteLine($"'checked(min * zero)' was evaluted to '{checked(min * zero)}'. Expected: '{long.MinValue * 0}'.");
                _counter++;
            }
        }
        ConfirmMinMultipliedByOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinMultipliedByOneIsFoldedCorrectly()
        {
            long min = long.MinValue;
            long one = 1;

            if (BreakUpFlow())
                return;

            if (checked(min * one) != long.MinValue * 1)
            {
                Console.WriteLine($"'checked(min * one)' was evaluted to '{checked(min * one)}'. Expected: '{long.MinValue * 1}'.");
                _counter++;
            }
        }
        ConfirmMinMultipliedByHalfOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinMultipliedByHalfOverflows()
        {
            long min = long.MinValue;
            long half = (long.MaxValue / 2);

            _counter++;
            try
            {
                _ = checked(min * half);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(min * half)' did not throw OverflowException.");
        }
        ConfirmMinMultipliedByMaxOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinMultipliedByMaxOverflows()
        {
            long min = long.MinValue;
            long max = long.MaxValue;

            _counter++;
            try
            {
                _ = checked(min * max);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(min * max)' did not throw OverflowException.");
        }
        ConfirmMinusHalfMultipliedByMinOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusHalfMultipliedByMinOverflows()
        {
            long minusHalf = long.MinValue / 2;
            long min = long.MinValue;

            _counter++;
            try
            {
                _ = checked(minusHalf * min);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(minusHalf * min)' did not throw OverflowException.");
        }
        ConfirmMinusHalfMultipliedByMinusHalfOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusHalfMultipliedByMinusHalfOverflows()
        {
            long minusHalf = long.MinValue / 2;

            _counter++;
            try
            {
                _ = checked(minusHalf * minusHalf);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(minusHalf * minusHalf)' did not throw OverflowException.");
        }
        ConfirmMinusHalfMultipliedByMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusHalfMultipliedByMinusOneIsFoldedCorrectly()
        {
            long minusHalf = long.MinValue / 2;
            long minusOne = -1;

            if (BreakUpFlow())
                return;

            if (checked(minusHalf * minusOne) != long.MinValue / 2 * -1)
            {
                Console.WriteLine($"'checked(minusHalf * minusOne)' was evaluted to '{checked(minusHalf * minusOne)}'. Expected: '{long.MinValue / 2 * -1}'.");
                _counter++;
            }
        }
        ConfirmMinusHalfMultipliedByZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusHalfMultipliedByZeroIsFoldedCorrectly()
        {
            long minusHalf = long.MinValue / 2;
            long zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(minusHalf * zero) != long.MinValue / 2 * 0)
            {
                Console.WriteLine($"'checked(minusHalf * zero)' was evaluted to '{checked(minusHalf * zero)}'. Expected: '{long.MinValue / 2 * 0}'.");
                _counter++;
            }
        }
        ConfirmMinusHalfMultipliedByOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusHalfMultipliedByOneIsFoldedCorrectly()
        {
            long minusHalf = long.MinValue / 2;
            long one = 1;

            if (BreakUpFlow())
                return;

            if (checked(minusHalf * one) != long.MinValue / 2 * 1)
            {
                Console.WriteLine($"'checked(minusHalf * one)' was evaluted to '{checked(minusHalf * one)}'. Expected: '{long.MinValue / 2 * 1}'.");
                _counter++;
            }
        }
        ConfirmMinusHalfMultipliedByHalfOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusHalfMultipliedByHalfOverflows()
        {
            long minusHalf = long.MinValue / 2;
            long half = (long.MaxValue / 2);

            _counter++;
            try
            {
                _ = checked(minusHalf * half);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(minusHalf * half)' did not throw OverflowException.");
        }
        ConfirmMinusHalfMultipliedByMaxOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusHalfMultipliedByMaxOverflows()
        {
            long minusHalf = long.MinValue / 2;
            long max = long.MaxValue;

            _counter++;
            try
            {
                _ = checked(minusHalf * max);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(minusHalf * max)' did not throw OverflowException.");
        }
        ConfirmMinusOneMultipliedByMinOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusOneMultipliedByMinOverflows()
        {
            long minusOne = -1;
            long min = long.MinValue;

            _counter++;
            try
            {
                _ = checked(minusOne * min);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(minusOne * min)' did not throw OverflowException.");
        }
        ConfirmMinusOneMultipliedByMinusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusOneMultipliedByMinusHalfIsFoldedCorrectly()
        {
            long minusOne = -1;
            long minusHalf = (long.MinValue / 2);

            if (BreakUpFlow())
                return;

            if (checked(minusOne * minusHalf) != -1 * (long.MinValue / 2))
            {
                Console.WriteLine($"'checked(minusOne * minusHalf)' was evaluted to '{checked(minusOne * minusHalf)}'. Expected: '{-1 * (long.MinValue / 2)}'.");
                _counter++;
            }
        }
        ConfirmMinusOneMultipliedByMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusOneMultipliedByMinusOneIsFoldedCorrectly()
        {
            long minusOne = -1;

            if (BreakUpFlow())
                return;

            if (checked(minusOne * minusOne) != -1 * -1)
            {
                Console.WriteLine($"'checked(minusOne * minusOne)' was evaluted to '{checked(minusOne * minusOne)}'. Expected: '{-1 * -1}'.");
                _counter++;
            }
        }
        ConfirmMinusOneMultipliedByZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusOneMultipliedByZeroIsFoldedCorrectly()
        {
            long minusOne = -1;
            long zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(minusOne * zero) != -1 * 0)
            {
                Console.WriteLine($"'checked(minusOne * zero)' was evaluted to '{checked(minusOne * zero)}'. Expected: '{-1 * 0}'.");
                _counter++;
            }
        }
        ConfirmMinusOneMultipliedByOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusOneMultipliedByOneIsFoldedCorrectly()
        {
            long minusOne = -1;
            long one = 1;

            if (BreakUpFlow())
                return;

            if (checked(minusOne * one) != -1 * 1)
            {
                Console.WriteLine($"'checked(minusOne * one)' was evaluted to '{checked(minusOne * one)}'. Expected: '{-1 * 1}'.");
                _counter++;
            }
        }
        ConfirmMinusOneMultipliedByHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusOneMultipliedByHalfIsFoldedCorrectly()
        {
            long minusOne = -1;
            long half = (long.MaxValue / 2);

            if (BreakUpFlow())
                return;

            if (checked(minusOne * half) != -1 * (long.MaxValue / 2))
            {
                Console.WriteLine($"'checked(minusOne * half)' was evaluted to '{checked(minusOne * half)}'. Expected: '{-1 * (long.MaxValue / 2)}'.");
                _counter++;
            }
        }
        ConfirmMinusOneMultipliedByMaxIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMinusOneMultipliedByMaxIsFoldedCorrectly()
        {
            long minusOne = -1;
            long max = long.MaxValue;

            if (BreakUpFlow())
                return;

            if (checked(minusOne * max) != -1 * long.MaxValue)
            {
                Console.WriteLine($"'checked(minusOne * max)' was evaluted to '{checked(minusOne * max)}'. Expected: '{-1 * long.MaxValue}'.");
                _counter++;
            }
        }
        ConfirmZeroMultipliedByMinIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroMultipliedByMinIsFoldedCorrectly()
        {
            long zero = 0;
            long min = long.MinValue;

            if (BreakUpFlow())
                return;

            if (checked(zero * min) != 0 * long.MinValue)
            {
                Console.WriteLine($"'checked(zero * min)' was evaluted to '{checked(zero * min)}'. Expected: '{0 * long.MinValue}'.");
                _counter++;
            }
        }
        ConfirmZeroMultipliedByMinusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroMultipliedByMinusHalfIsFoldedCorrectly()
        {
            long zero = 0;
            long minusHalf = (long.MinValue / 2);

            if (BreakUpFlow())
                return;

            if (checked(zero * minusHalf) != 0 * (long.MinValue / 2))
            {
                Console.WriteLine($"'checked(zero * minusHalf)' was evaluted to '{checked(zero * minusHalf)}'. Expected: '{0 * (long.MinValue / 2)}'.");
                _counter++;
            }
        }
        ConfirmZeroMultipliedByMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroMultipliedByMinusOneIsFoldedCorrectly()
        {
            long zero = 0;
            long minusOne = -1;

            if (BreakUpFlow())
                return;

            if (checked(zero * minusOne) != 0 * -1)
            {
                Console.WriteLine($"'checked(zero * minusOne)' was evaluted to '{checked(zero * minusOne)}'. Expected: '{0 * -1}'.");
                _counter++;
            }
        }
        ConfirmZeroMultipliedByZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroMultipliedByZeroIsFoldedCorrectly()
        {
            long zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(zero * zero) != 0 * 0)
            {
                Console.WriteLine($"'checked(zero * zero)' was evaluted to '{checked(zero * zero)}'. Expected: '{0 * 0}'.");
                _counter++;
            }
        }
        ConfirmZeroMultipliedByOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroMultipliedByOneIsFoldedCorrectly()
        {
            long zero = 0;
            long one = 1;

            if (BreakUpFlow())
                return;

            if (checked(zero * one) != 0 * 1)
            {
                Console.WriteLine($"'checked(zero * one)' was evaluted to '{checked(zero * one)}'. Expected: '{0 * 1}'.");
                _counter++;
            }
        }
        ConfirmZeroMultipliedByHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroMultipliedByHalfIsFoldedCorrectly()
        {
            long zero = 0;
            long half = (long.MaxValue / 2);

            if (BreakUpFlow())
                return;

            if (checked(zero * half) != 0 * (long.MaxValue / 2))
            {
                Console.WriteLine($"'checked(zero * half)' was evaluted to '{checked(zero * half)}'. Expected: '{0 * (long.MaxValue / 2)}'.");
                _counter++;
            }
        }
        ConfirmZeroMultipliedByMaxIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroMultipliedByMaxIsFoldedCorrectly()
        {
            long zero = 0;
            long max = long.MaxValue;

            if (BreakUpFlow())
                return;

            if (checked(zero * max) != 0 * long.MaxValue)
            {
                Console.WriteLine($"'checked(zero * max)' was evaluted to '{checked(zero * max)}'. Expected: '{0 * long.MaxValue}'.");
                _counter++;
            }
        }
        ConfirmOneMultipliedByMinIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOneMultipliedByMinIsFoldedCorrectly()
        {
            long one = 1;
            long min = long.MinValue;

            if (BreakUpFlow())
                return;

            if (checked(one * min) != 1 * long.MinValue)
            {
                Console.WriteLine($"'checked(one * min)' was evaluted to '{checked(one * min)}'. Expected: '{1 * long.MinValue}'.");
                _counter++;
            }
        }
        ConfirmOneMultipliedByMinusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOneMultipliedByMinusHalfIsFoldedCorrectly()
        {
            long one = 1;
            long minusHalf = (long.MinValue / 2);

            if (BreakUpFlow())
                return;

            if (checked(one * minusHalf) != 1 * (long.MinValue / 2))
            {
                Console.WriteLine($"'checked(one * minusHalf)' was evaluted to '{checked(one * minusHalf)}'. Expected: '{1 * (long.MinValue / 2)}'.");
                _counter++;
            }
        }
        ConfirmOneMultipliedByMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOneMultipliedByMinusOneIsFoldedCorrectly()
        {
            long one = 1;
            long minusOne = -1;

            if (BreakUpFlow())
                return;

            if (checked(one * minusOne) != 1 * -1)
            {
                Console.WriteLine($"'checked(one * minusOne)' was evaluted to '{checked(one * minusOne)}'. Expected: '{1 * -1}'.");
                _counter++;
            }
        }
        ConfirmOneMultipliedByZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOneMultipliedByZeroIsFoldedCorrectly()
        {
            long one = 1;
            long zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(one * zero) != 1 * 0)
            {
                Console.WriteLine($"'checked(one * zero)' was evaluted to '{checked(one * zero)}'. Expected: '{1 * 0}'.");
                _counter++;
            }
        }
        ConfirmOneMultipliedByOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOneMultipliedByOneIsFoldedCorrectly()
        {
            long one = 1;

            if (BreakUpFlow())
                return;

            if (checked(one * one) != 1 * 1)
            {
                Console.WriteLine($"'checked(one * one)' was evaluted to '{checked(one * one)}'. Expected: '{1 * 1}'.");
                _counter++;
            }
        }
        ConfirmOneMultipliedByHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOneMultipliedByHalfIsFoldedCorrectly()
        {
            long one = 1;
            long half = (long.MaxValue / 2);

            if (BreakUpFlow())
                return;

            if (checked(one * half) != 1 * (long.MaxValue / 2))
            {
                Console.WriteLine($"'checked(one * half)' was evaluted to '{checked(one * half)}'. Expected: '{1 * (long.MaxValue / 2)}'.");
                _counter++;
            }
        }
        ConfirmOneMultipliedByMaxIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOneMultipliedByMaxIsFoldedCorrectly()
        {
            long one = 1;
            long max = long.MaxValue;

            if (BreakUpFlow())
                return;

            if (checked(one * max) != 1 * long.MaxValue)
            {
                Console.WriteLine($"'checked(one * max)' was evaluted to '{checked(one * max)}'. Expected: '{1 * long.MaxValue}'.");
                _counter++;
            }
        }
        ConfirmHalfMultipliedByMinOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfMultipliedByMinOverflows()
        {
            long half = long.MaxValue / 2;
            long min = long.MinValue;

            _counter++;
            try
            {
                _ = checked(half * min);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(half * min)' did not throw OverflowException.");
        }
        ConfirmHalfMultipliedByMinusHalfOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfMultipliedByMinusHalfOverflows()
        {
            long half = long.MaxValue / 2;
            long minusHalf = (long.MinValue / 2);

            _counter++;
            try
            {
                _ = checked(half * minusHalf);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(half * minusHalf)' did not throw OverflowException.");
        }
        ConfirmHalfMultipliedByMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfMultipliedByMinusOneIsFoldedCorrectly()
        {
            long half = long.MaxValue / 2;
            long minusOne = -1;

            if (BreakUpFlow())
                return;

            if (checked(half * minusOne) != long.MaxValue / 2 * -1)
            {
                Console.WriteLine($"'checked(half * minusOne)' was evaluted to '{checked(half * minusOne)}'. Expected: '{long.MaxValue / 2 * -1}'.");
                _counter++;
            }
        }
        ConfirmHalfMultipliedByZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfMultipliedByZeroIsFoldedCorrectly()
        {
            long half = long.MaxValue / 2;
            long zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(half * zero) != long.MaxValue / 2 * 0)
            {
                Console.WriteLine($"'checked(half * zero)' was evaluted to '{checked(half * zero)}'. Expected: '{long.MaxValue / 2 * 0}'.");
                _counter++;
            }
        }
        ConfirmHalfMultipliedByOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfMultipliedByOneIsFoldedCorrectly()
        {
            long half = long.MaxValue / 2;
            long one = 1;

            if (BreakUpFlow())
                return;

            if (checked(half * one) != long.MaxValue / 2 * 1)
            {
                Console.WriteLine($"'checked(half * one)' was evaluted to '{checked(half * one)}'. Expected: '{long.MaxValue / 2 * 1}'.");
                _counter++;
            }
        }
        ConfirmHalfMultipliedByHalfOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfMultipliedByHalfOverflows()
        {
            long half = long.MaxValue / 2;

            _counter++;
            try
            {
                _ = checked(half * half);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(half * half)' did not throw OverflowException.");
        }
        ConfirmHalfMultipliedByMaxOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfMultipliedByMaxOverflows()
        {
            long half = long.MaxValue / 2;
            long max = long.MaxValue;

            _counter++;
            try
            {
                _ = checked(half * max);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(half * max)' did not throw OverflowException.");
        }
        ConfirmMaxMultipliedByMinOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxMultipliedByMinOverflows()
        {
            long max = long.MaxValue;
            long min = long.MinValue;

            _counter++;
            try
            {
                _ = checked(max * min);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(max * min)' did not throw OverflowException.");
        }
        ConfirmMaxMultipliedByMinusHalfOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxMultipliedByMinusHalfOverflows()
        {
            long max = long.MaxValue;
            long minusHalf = (long.MinValue / 2);

            _counter++;
            try
            {
                _ = checked(max * minusHalf);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(max * minusHalf)' did not throw OverflowException.");
        }
        ConfirmMaxMultipliedByMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxMultipliedByMinusOneIsFoldedCorrectly()
        {
            long max = long.MaxValue;
            long minusOne = -1;

            if (BreakUpFlow())
                return;

            if (checked(max * minusOne) != long.MaxValue * -1)
            {
                Console.WriteLine($"'checked(max * minusOne)' was evaluted to '{checked(max * minusOne)}'. Expected: '{long.MaxValue * -1}'.");
                _counter++;
            }
        }
        ConfirmMaxMultipliedByZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxMultipliedByZeroIsFoldedCorrectly()
        {
            long max = long.MaxValue;
            long zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(max * zero) != long.MaxValue * 0)
            {
                Console.WriteLine($"'checked(max * zero)' was evaluted to '{checked(max * zero)}'. Expected: '{long.MaxValue * 0}'.");
                _counter++;
            }
        }
        ConfirmMaxMultipliedByOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxMultipliedByOneIsFoldedCorrectly()
        {
            long max = long.MaxValue;
            long one = 1;

            if (BreakUpFlow())
                return;

            if (checked(max * one) != long.MaxValue * 1)
            {
                Console.WriteLine($"'checked(max * one)' was evaluted to '{checked(max * one)}'. Expected: '{long.MaxValue * 1}'.");
                _counter++;
            }
        }
        ConfirmMaxMultipliedByHalfOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxMultipliedByHalfOverflows()
        {
            long max = long.MaxValue;
            long half = (long.MaxValue / 2);

            _counter++;
            try
            {
                _ = checked(max * half);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(max * half)' did not throw OverflowException.");
        }
        ConfirmMaxMultipliedByMaxOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxMultipliedByMaxOverflows()
        {
            long max = long.MaxValue;

            _counter++;
            try
            {
                _ = checked(max * max);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(max * max)' did not throw OverflowException.");
        }
    }

    private static void TestUInt64()
    {
        ConfirmAdditionIdentities(42);
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmAdditionIdentities(ulong value)
        {
            ulong zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(value + zero) != value)
            {
                Console.WriteLine($"Addition identity for ulong 'checked(value + zero)' was evaluted to '{checked(value + zero)}'. Expected: '{value}'.");
                _counter++;
            }

            if (checked(zero + value) != value)
            {
                Console.WriteLine($"Addition identity for ulong 'checked(zero + value)' was evaluted to '{checked(zero + value)}'. Expected: '{value}'.");
                _counter++;
            }
        }
        ConfirmSubtractionIdentities(42);
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSubtractionIdentities(ulong value)
        {
            ulong zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(value - zero) != value)
            {
                Console.WriteLine($"Subtraction identity for ulong 'checked(value - zero)' was evaluted to '{checked(value - zero)}'. Expected: '{value}'.");
                _counter++;
            }

            if (checked(value - value) != 0)
            {
                Console.WriteLine($"Subtraction identity for ulong 'checked(value - value)' was evaluted to '{checked(value - value)}'. Expected: '{0}'.");
                _counter++;
            }
        }
        ConfirmMultiplicationIdentities(42);
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMultiplicationIdentities(ulong value)
        {
            ulong zero = 0;
            ulong one = 1;

            if (BreakUpFlow())
                return;

            if (checked(value * zero) != 0)
            {
                Console.WriteLine($"Multiplication identity for ulong 'checked(value * zero)' was evaluted to '{checked(value * zero)}'. Expected: '{0}'.");
                _counter++;
            }

            if (checked(zero * value) != 0)
            {
                Console.WriteLine($"Multiplication identity for ulong 'checked(zero * value)' was evaluted to '{checked(zero * value)}'. Expected: '{0}'.");
                _counter++;
            }

            if (checked(value * one) != value)
            {
                Console.WriteLine($"Multiplication identity for ulong 'checked(value * one)' was evaluted to '{checked(value * one)}'. Expected: '{value}'.");
                _counter++;
            }

            if (checked(one * value) != value)
            {
                Console.WriteLine($"Multiplication identity for ulong 'checked(one * value)' was evaluted to '{checked(one * value)}'. Expected: '{value}'.");
                _counter++;
            }
        }
        ConfirmZeroPlusZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroPlusZeroIsFoldedCorrectly()
        {
            ulong zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(zero + zero) != 0 + 0)
            {
                Console.WriteLine($"'checked(zero + zero)' was evaluted to '{checked(zero + zero)}'. Expected: '{0 + 0}'.");
                _counter++;
            }
        }
        ConfirmZeroPlusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroPlusOneIsFoldedCorrectly()
        {
            ulong zero = 0;
            ulong one = 1;

            if (BreakUpFlow())
                return;

            if (checked(zero + one) != 0 + 1)
            {
                Console.WriteLine($"'checked(zero + one)' was evaluted to '{checked(zero + one)}'. Expected: '{0 + 1}'.");
                _counter++;
            }
        }
        ConfirmZeroPlusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroPlusHalfIsFoldedCorrectly()
        {
            ulong zero = 0;
            ulong half = ulong.MaxValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(zero + half) != 0 + ulong.MaxValue / 2)
            {
                Console.WriteLine($"'checked(zero + half)' was evaluted to '{checked(zero + half)}'. Expected: '{0 + ulong.MaxValue / 2}'.");
                _counter++;
            }
        }
        ConfirmZeroPlusMaxIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroPlusMaxIsFoldedCorrectly()
        {
            ulong zero = 0;
            ulong max = ulong.MaxValue;

            if (BreakUpFlow())
                return;

            if (checked(zero + max) != 0 + ulong.MaxValue)
            {
                Console.WriteLine($"'checked(zero + max)' was evaluted to '{checked(zero + max)}'. Expected: '{0 + ulong.MaxValue}'.");
                _counter++;
            }
        }
        ConfirmOnePlusZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOnePlusZeroIsFoldedCorrectly()
        {
            ulong one = 1;
            ulong zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(one + zero) != 1 + 0)
            {
                Console.WriteLine($"'checked(one + zero)' was evaluted to '{checked(one + zero)}'. Expected: '{1 + 0}'.");
                _counter++;
            }
        }
        ConfirmOnePlusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOnePlusOneIsFoldedCorrectly()
        {
            ulong one = 1;

            if (BreakUpFlow())
                return;

            if (checked(one + one) != 1 + 1)
            {
                Console.WriteLine($"'checked(one + one)' was evaluted to '{checked(one + one)}'. Expected: '{1 + 1}'.");
                _counter++;
            }
        }
        ConfirmOnePlusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOnePlusHalfIsFoldedCorrectly()
        {
            ulong one = 1;
            ulong half = ulong.MaxValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(one + half) != 1 + ulong.MaxValue / 2)
            {
                Console.WriteLine($"'checked(one + half)' was evaluted to '{checked(one + half)}'. Expected: '{1 + ulong.MaxValue / 2}'.");
                _counter++;
            }
        }
        ConfirmOnePlusMaxOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOnePlusMaxOverflows()
        {
            ulong one = 1;
            ulong max = ulong.MaxValue;

            _counter++;
            try
            {
                _ = checked(one + max);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(one + max)' did not throw OverflowException.");
        }
        ConfirmHalfPlusZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfPlusZeroIsFoldedCorrectly()
        {
            ulong half = ulong.MaxValue / 2;
            ulong zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(half + zero) != ulong.MaxValue / 2 + 0)
            {
                Console.WriteLine($"'checked(half + zero)' was evaluted to '{checked(half + zero)}'. Expected: '{ulong.MaxValue / 2 + 0}'.");
                _counter++;
            }
        }
        ConfirmHalfPlusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfPlusOneIsFoldedCorrectly()
        {
            ulong half = ulong.MaxValue / 2;
            ulong one = 1;

            if (BreakUpFlow())
                return;

            if (checked(half + one) != ulong.MaxValue / 2 + 1)
            {
                Console.WriteLine($"'checked(half + one)' was evaluted to '{checked(half + one)}'. Expected: '{ulong.MaxValue / 2 + 1}'.");
                _counter++;
            }
        }
        ConfirmHalfPlusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfPlusHalfIsFoldedCorrectly()
        {
            ulong half = ulong.MaxValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(half + half) != ulong.MaxValue / 2 + ulong.MaxValue / 2)
            {
                Console.WriteLine($"'checked(half + half)' was evaluted to '{checked(half + half)}'. Expected: '{ulong.MaxValue / 2 + ulong.MaxValue / 2}'.");
                _counter++;
            }
        }
        ConfirmHalfPlusMaxOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfPlusMaxOverflows()
        {
            ulong half = ulong.MaxValue / 2;
            ulong max = ulong.MaxValue;

            _counter++;
            try
            {
                _ = checked(half + max);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(half + max)' did not throw OverflowException.");
        }
        ConfirmMaxPlusZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxPlusZeroIsFoldedCorrectly()
        {
            ulong max = ulong.MaxValue;
            ulong zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(max + zero) != ulong.MaxValue + 0)
            {
                Console.WriteLine($"'checked(max + zero)' was evaluted to '{checked(max + zero)}'. Expected: '{ulong.MaxValue + 0}'.");
                _counter++;
            }
        }
        ConfirmMaxPlusOneOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxPlusOneOverflows()
        {
            ulong max = ulong.MaxValue;
            ulong one = 1;

            _counter++;
            try
            {
                _ = checked(max + one);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(max + one)' did not throw OverflowException.");
        }
        ConfirmMaxPlusHalfOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxPlusHalfOverflows()
        {
            ulong max = ulong.MaxValue;
            ulong half = ulong.MaxValue / 2;

            _counter++;
            try
            {
                _ = checked(max + half);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(max + half)' did not throw OverflowException.");
        }
        ConfirmMaxPlusMaxOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxPlusMaxOverflows()
        {
            ulong max = ulong.MaxValue;

            _counter++;
            try
            {
                _ = checked(max + max);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(max + max)' did not throw OverflowException.");
        }

        ConfirmZeroMinusZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroMinusZeroIsFoldedCorrectly()
        {
            ulong zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(zero - zero) != 0 - 0)
            {
                Console.WriteLine($"'checked(zero - zero)' was evaluted to '{checked(zero - zero)}'. Expected: '{0 - 0}'.");
                _counter++;
            }
        }
        ConfirmZeroMinusOneOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroMinusOneOverflows()
        {
            ulong zero = 0;
            ulong one = 1;

            _counter++;
            try
            {
                _ = checked(zero - one);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(zero - one)' did not throw OverflowException.");
        }
        ConfirmZeroMinusHalfOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroMinusHalfOverflows()
        {
            ulong zero = 0;
            ulong half = ulong.MaxValue / 2;

            _counter++;
            try
            {
                _ = checked(zero - half);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(zero - half)' did not throw OverflowException.");
        }
        ConfirmZeroMinusMaxOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroMinusMaxOverflows()
        {
            ulong zero = 0;
            ulong max = ulong.MaxValue;

            _counter++;
            try
            {
                _ = checked(zero - max);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(zero - max)' did not throw OverflowException.");
        }
        ConfirmOneMinusZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOneMinusZeroIsFoldedCorrectly()
        {
            ulong one = 1;
            ulong zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(one - zero) != 1 - 0)
            {
                Console.WriteLine($"'checked(one - zero)' was evaluted to '{checked(one - zero)}'. Expected: '{1 - 0}'.");
                _counter++;
            }
        }
        ConfirmOneMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOneMinusOneIsFoldedCorrectly()
        {
            ulong one = 1;

            if (BreakUpFlow())
                return;

            if (checked(one - one) != 1 - 1)
            {
                Console.WriteLine($"'checked(one - one)' was evaluted to '{checked(one - one)}'. Expected: '{1 - 1}'.");
                _counter++;
            }
        }
        ConfirmOneMinusHalfOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOneMinusHalfOverflows()
        {
            ulong one = 1;
            ulong half = ulong.MaxValue / 2;

            _counter++;
            try
            {
                _ = checked(one - half);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(one - half)' did not throw OverflowException.");
        }
        ConfirmOneMinusMaxOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOneMinusMaxOverflows()
        {
            ulong one = 1;
            ulong max = ulong.MaxValue;

            _counter++;
            try
            {
                _ = checked(one - max);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(one - max)' did not throw OverflowException.");
        }
        ConfirmHalfMinusZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfMinusZeroIsFoldedCorrectly()
        {
            ulong half = ulong.MaxValue / 2;
            ulong zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(half - zero) != ulong.MaxValue / 2 - 0)
            {
                Console.WriteLine($"'checked(half - zero)' was evaluted to '{checked(half - zero)}'. Expected: '{ulong.MaxValue / 2 - 0}'.");
                _counter++;
            }
        }
        ConfirmHalfMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfMinusOneIsFoldedCorrectly()
        {
            ulong half = ulong.MaxValue / 2;
            ulong one = 1;

            if (BreakUpFlow())
                return;

            if (checked(half - one) != ulong.MaxValue / 2 - 1)
            {
                Console.WriteLine($"'checked(half - one)' was evaluted to '{checked(half - one)}'. Expected: '{ulong.MaxValue / 2 - 1}'.");
                _counter++;
            }
        }
        ConfirmHalfMinusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfMinusHalfIsFoldedCorrectly()
        {
            ulong half = ulong.MaxValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(half - half) != ulong.MaxValue / 2 - ulong.MaxValue / 2)
            {
                Console.WriteLine($"'checked(half - half)' was evaluted to '{checked(half - half)}'. Expected: '{ulong.MaxValue / 2 - ulong.MaxValue / 2}'.");
                _counter++;
            }
        }
        ConfirmHalfMinusMaxOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfMinusMaxOverflows()
        {
            ulong half = ulong.MaxValue / 2;
            ulong max = ulong.MaxValue;

            _counter++;
            try
            {
                _ = checked(half - max);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(half - max)' did not throw OverflowException.");
        }
        ConfirmMaxMinusZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxMinusZeroIsFoldedCorrectly()
        {
            ulong max = ulong.MaxValue;
            ulong zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(max - zero) != ulong.MaxValue - 0)
            {
                Console.WriteLine($"'checked(max - zero)' was evaluted to '{checked(max - zero)}'. Expected: '{ulong.MaxValue - 0}'.");
                _counter++;
            }
        }
        ConfirmMaxMinusOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxMinusOneIsFoldedCorrectly()
        {
            ulong max = ulong.MaxValue;
            ulong one = 1;

            if (BreakUpFlow())
                return;

            if (checked(max - one) != ulong.MaxValue - 1)
            {
                Console.WriteLine($"'checked(max - one)' was evaluted to '{checked(max - one)}'. Expected: '{ulong.MaxValue - 1}'.");
                _counter++;
            }
        }
        ConfirmMaxMinusHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxMinusHalfIsFoldedCorrectly()
        {
            ulong max = ulong.MaxValue;
            ulong half = ulong.MaxValue / 2;

            if (BreakUpFlow())
                return;

            if (checked(max - half) != ulong.MaxValue - ulong.MaxValue / 2)
            {
                Console.WriteLine($"'checked(max - half)' was evaluted to '{checked(max - half)}'. Expected: '{ulong.MaxValue - ulong.MaxValue / 2}'.");
                _counter++;
            }
        }
        ConfirmMaxMinusMaxIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxMinusMaxIsFoldedCorrectly()
        {
            ulong max = ulong.MaxValue;

            if (BreakUpFlow())
                return;

            if (checked(max - max) != ulong.MaxValue - ulong.MaxValue)
            {
                Console.WriteLine($"'checked(max - max)' was evaluted to '{checked(max - max)}'. Expected: '{ulong.MaxValue - ulong.MaxValue}'.");
                _counter++;
            }
        }

        ConfirmZeroMultipliedByZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroMultipliedByZeroIsFoldedCorrectly()
        {
            ulong zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(zero * zero) != 0 * 0)
            {
                Console.WriteLine($"'checked(zero * zero)' was evaluted to '{checked(zero * zero)}'. Expected: '{0 * 0}'.");
                _counter++;
            }
        }
        ConfirmZeroMultipliedByOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroMultipliedByOneIsFoldedCorrectly()
        {
            ulong zero = 0;
            ulong one = 1;

            if (BreakUpFlow())
                return;

            if (checked(zero * one) != 0 * 1)
            {
                Console.WriteLine($"'checked(zero * one)' was evaluted to '{checked(zero * one)}'. Expected: '{0 * 1}'.");
                _counter++;
            }
        }
        ConfirmZeroMultipliedByHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroMultipliedByHalfIsFoldedCorrectly()
        {
            ulong zero = 0;
            ulong half = (ulong.MaxValue / 2);

            if (BreakUpFlow())
                return;

            if (checked(zero * half) != 0 * (ulong.MaxValue / 2))
            {
                Console.WriteLine($"'checked(zero * half)' was evaluted to '{checked(zero * half)}'. Expected: '{0 * (ulong.MaxValue / 2)}'.");
                _counter++;
            }
        }
        ConfirmZeroMultipliedByMaxIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmZeroMultipliedByMaxIsFoldedCorrectly()
        {
            ulong zero = 0;
            ulong max = ulong.MaxValue;

            if (BreakUpFlow())
                return;

            if (checked(zero * max) != 0 * ulong.MaxValue)
            {
                Console.WriteLine($"'checked(zero * max)' was evaluted to '{checked(zero * max)}'. Expected: '{0 * ulong.MaxValue}'.");
                _counter++;
            }
        }
        ConfirmOneMultipliedByZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOneMultipliedByZeroIsFoldedCorrectly()
        {
            ulong one = 1;
            ulong zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(one * zero) != 1 * 0)
            {
                Console.WriteLine($"'checked(one * zero)' was evaluted to '{checked(one * zero)}'. Expected: '{1 * 0}'.");
                _counter++;
            }
        }
        ConfirmOneMultipliedByOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOneMultipliedByOneIsFoldedCorrectly()
        {
            ulong one = 1;

            if (BreakUpFlow())
                return;

            if (checked(one * one) != 1 * 1)
            {
                Console.WriteLine($"'checked(one * one)' was evaluted to '{checked(one * one)}'. Expected: '{1 * 1}'.");
                _counter++;
            }
        }
        ConfirmOneMultipliedByHalfIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOneMultipliedByHalfIsFoldedCorrectly()
        {
            ulong one = 1;
            ulong half = (ulong.MaxValue / 2);

            if (BreakUpFlow())
                return;

            if (checked(one * half) != 1 * (ulong.MaxValue / 2))
            {
                Console.WriteLine($"'checked(one * half)' was evaluted to '{checked(one * half)}'. Expected: '{1 * (ulong.MaxValue / 2)}'.");
                _counter++;
            }
        }
        ConfirmOneMultipliedByMaxIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmOneMultipliedByMaxIsFoldedCorrectly()
        {
            ulong one = 1;
            ulong max = ulong.MaxValue;

            if (BreakUpFlow())
                return;

            if (checked(one * max) != 1 * ulong.MaxValue)
            {
                Console.WriteLine($"'checked(one * max)' was evaluted to '{checked(one * max)}'. Expected: '{1 * ulong.MaxValue}'.");
                _counter++;
            }
        }
        ConfirmHalfMultipliedByZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfMultipliedByZeroIsFoldedCorrectly()
        {
            ulong half = ulong.MaxValue / 2;
            ulong zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(half * zero) != ulong.MaxValue / 2 * 0)
            {
                Console.WriteLine($"'checked(half * zero)' was evaluted to '{checked(half * zero)}'. Expected: '{ulong.MaxValue / 2 * 0}'.");
                _counter++;
            }
        }
        ConfirmHalfMultipliedByOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfMultipliedByOneIsFoldedCorrectly()
        {
            ulong half = ulong.MaxValue / 2;
            ulong one = 1;

            if (BreakUpFlow())
                return;

            if (checked(half * one) != ulong.MaxValue / 2 * 1)
            {
                Console.WriteLine($"'checked(half * one)' was evaluted to '{checked(half * one)}'. Expected: '{ulong.MaxValue / 2 * 1}'.");
                _counter++;
            }
        }
        ConfirmHalfMultipliedByHalfOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfMultipliedByHalfOverflows()
        {
            ulong half = ulong.MaxValue / 2;

            _counter++;
            try
            {
                _ = checked(half * half);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(half * half)' did not throw OverflowException.");
        }
        ConfirmHalfMultipliedByMaxOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmHalfMultipliedByMaxOverflows()
        {
            ulong half = ulong.MaxValue / 2;
            ulong max = ulong.MaxValue;

            _counter++;
            try
            {
                _ = checked(half * max);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(half * max)' did not throw OverflowException.");
        }
        ConfirmMaxMultipliedByZeroIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxMultipliedByZeroIsFoldedCorrectly()
        {
            ulong max = ulong.MaxValue;
            ulong zero = 0;

            if (BreakUpFlow())
                return;

            if (checked(max * zero) != ulong.MaxValue * 0)
            {
                Console.WriteLine($"'checked(max * zero)' was evaluted to '{checked(max * zero)}'. Expected: '{ulong.MaxValue * 0}'.");
                _counter++;
            }
        }
        ConfirmMaxMultipliedByOneIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxMultipliedByOneIsFoldedCorrectly()
        {
            ulong max = ulong.MaxValue;
            ulong one = 1;

            if (BreakUpFlow())
                return;

            if (checked(max * one) != ulong.MaxValue * 1)
            {
                Console.WriteLine($"'checked(max * one)' was evaluted to '{checked(max * one)}'. Expected: '{ulong.MaxValue * 1}'.");
                _counter++;
            }
        }
        ConfirmMaxMultipliedByHalfOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxMultipliedByHalfOverflows()
        {
            ulong max = ulong.MaxValue;
            ulong half = (ulong.MaxValue / 2);

            _counter++;
            try
            {
                _ = checked(max * half);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(max * half)' did not throw OverflowException.");
        }
        ConfirmMaxMultipliedByMaxOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmMaxMultipliedByMaxOverflows()
        {
            ulong max = ulong.MaxValue;

            _counter++;
            try
            {
                _ = checked(max * max);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked(max * max)' did not throw OverflowException.");
        }
    }
}
