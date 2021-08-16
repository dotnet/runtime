// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

public partial class ValueNumberingCheckedCastsOfConstants
{
    private static void TestCastingInt16ToSingle()
    {
        ConfirmIntegerZeroCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToSingleIsFoldedCorrectly()
        {
            short integerZero = 0;

            if (BreakUpFlow())
                return;

            if (checked((float)integerZero) != 0f)
            {
                Console.WriteLine($"'(float)0' was evaluted to '{(float)integerZero}'. Expected: '0f'.");
                _counter++;
            }
        }
        ConfirmSByteMinValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMinValueCastToSingleIsFoldedCorrectly()
        {
            short sByteMinValue = -128;

            if (BreakUpFlow())
                return;

            if (checked((float)sByteMinValue) != -128f)
            {
                Console.WriteLine($"'(float)-128' was evaluted to '{(float)sByteMinValue}'. Expected: '-128f'.");
                _counter++;
            }
        }
        ConfirmSByteMaxValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToSingleIsFoldedCorrectly()
        {
            short sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((float)sByteMaxValue) != 127f)
            {
                Console.WriteLine($"'(float)127' was evaluted to '{(float)sByteMaxValue}'. Expected: '127f'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMinValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMinValueCastToSingleIsFoldedCorrectly()
        {
            short integerOneDecrementUnderSByteMinValue = -129;

            if (BreakUpFlow())
                return;

            if (checked((float)integerOneDecrementUnderSByteMinValue) != -129f)
            {
                Console.WriteLine($"'(float)-129' was evaluted to '{(float)integerOneDecrementUnderSByteMinValue}'. Expected: '-129f'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMinValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMinValueCastToSingleIsFoldedCorrectly()
        {
            short integerOneIncrementAboveSByteMinValue = -127;

            if (BreakUpFlow())
                return;

            if (checked((float)integerOneIncrementAboveSByteMinValue) != -127f)
            {
                Console.WriteLine($"'(float)-127' was evaluted to '{(float)integerOneIncrementAboveSByteMinValue}'. Expected: '-127f'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToSingleIsFoldedCorrectly()
        {
            short integerOneDecrementUnderSByteMaxValue = 126;

            if (BreakUpFlow())
                return;

            if (checked((float)integerOneDecrementUnderSByteMaxValue) != 126f)
            {
                Console.WriteLine($"'(float)126' was evaluted to '{(float)integerOneDecrementUnderSByteMaxValue}'. Expected: '126f'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMaxValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMaxValueCastToSingleIsFoldedCorrectly()
        {
            short integerOneIncrementAboveSByteMaxValue = 128;

            if (BreakUpFlow())
                return;

            if (checked((float)integerOneIncrementAboveSByteMaxValue) != 128f)
            {
                Console.WriteLine($"'(float)128' was evaluted to '{(float)integerOneIncrementAboveSByteMaxValue}'. Expected: '128f'.");
                _counter++;
            }
        }
        ConfirmByteMaxValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmByteMaxValueCastToSingleIsFoldedCorrectly()
        {
            short byteMaxValue = 255;

            if (BreakUpFlow())
                return;

            if (checked((float)byteMaxValue) != 255f)
            {
                Console.WriteLine($"'(float)255' was evaluted to '{(float)byteMaxValue}'. Expected: '255f'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMinValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMinValueCastToSingleIsFoldedCorrectly()
        {
            short integerOneDecrementUnderByteMinValue = -1;

            if (BreakUpFlow())
                return;

            if (checked((float)integerOneDecrementUnderByteMinValue) != -1f)
            {
                Console.WriteLine($"'(float)-1' was evaluted to '{(float)integerOneDecrementUnderByteMinValue}'. Expected: '-1f'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToSingleIsFoldedCorrectly()
        {
            short integerOneIncrementAboveByteMinValue = 1;

            if (BreakUpFlow())
                return;

            if (checked((float)integerOneIncrementAboveByteMinValue) != 1f)
            {
                Console.WriteLine($"'(float)1' was evaluted to '{(float)integerOneIncrementAboveByteMinValue}'. Expected: '1f'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMaxValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMaxValueCastToSingleIsFoldedCorrectly()
        {
            short integerOneDecrementUnderByteMaxValue = 254;

            if (BreakUpFlow())
                return;

            if (checked((float)integerOneDecrementUnderByteMaxValue) != 254f)
            {
                Console.WriteLine($"'(float)254' was evaluted to '{(float)integerOneDecrementUnderByteMaxValue}'. Expected: '254f'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMaxValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMaxValueCastToSingleIsFoldedCorrectly()
        {
            short integerOneIncrementAboveByteMaxValue = 256;

            if (BreakUpFlow())
                return;

            if (checked((float)integerOneIncrementAboveByteMaxValue) != 256f)
            {
                Console.WriteLine($"'(float)256' was evaluted to '{(float)integerOneIncrementAboveByteMaxValue}'. Expected: '256f'.");
                _counter++;
            }
        }
        ConfirmInt16MinValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MinValueCastToSingleIsFoldedCorrectly()
        {
            short int16MinValue = -32768;

            if (BreakUpFlow())
                return;

            if (checked((float)int16MinValue) != -32768f)
            {
                Console.WriteLine($"'(float)-32768' was evaluted to '{(float)int16MinValue}'. Expected: '-32768f'.");
                _counter++;
            }
        }
        ConfirmInt16MaxValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MaxValueCastToSingleIsFoldedCorrectly()
        {
            short int16MaxValue = 32767;

            if (BreakUpFlow())
                return;

            if (checked((float)int16MaxValue) != 32767f)
            {
                Console.WriteLine($"'(float)32767' was evaluted to '{(float)int16MaxValue}'. Expected: '32767f'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt16MinValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt16MinValueCastToSingleIsFoldedCorrectly()
        {
            short integerOneIncrementAboveInt16MinValue = -32767;

            if (BreakUpFlow())
                return;

            if (checked((float)integerOneIncrementAboveInt16MinValue) != -32767f)
            {
                Console.WriteLine($"'(float)-32767' was evaluted to '{(float)integerOneIncrementAboveInt16MinValue}'. Expected: '-32767f'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt16MaxValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MaxValueCastToSingleIsFoldedCorrectly()
        {
            short integerOneDecrementUnderInt16MaxValue = 32766;

            if (BreakUpFlow())
                return;

            if (checked((float)integerOneDecrementUnderInt16MaxValue) != 32766f)
            {
                Console.WriteLine($"'(float)32766' was evaluted to '{(float)integerOneDecrementUnderInt16MaxValue}'. Expected: '32766f'.");
                _counter++;
            }
        }
    }

    private static void TestCastingInt16ToDouble()
    {
        ConfirmIntegerZeroCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToDoubleIsFoldedCorrectly()
        {
            short integerZero = 0;

            if (BreakUpFlow())
                return;

            if (checked((double)integerZero) != 0d)
            {
                Console.WriteLine($"'(double)0' was evaluted to '{(double)integerZero}'. Expected: '0d'.");
                _counter++;
            }
        }
        ConfirmSByteMinValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMinValueCastToDoubleIsFoldedCorrectly()
        {
            short sByteMinValue = -128;

            if (BreakUpFlow())
                return;

            if (checked((double)sByteMinValue) != -128d)
            {
                Console.WriteLine($"'(double)-128' was evaluted to '{(double)sByteMinValue}'. Expected: '-128d'.");
                _counter++;
            }
        }
        ConfirmSByteMaxValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToDoubleIsFoldedCorrectly()
        {
            short sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((double)sByteMaxValue) != 127d)
            {
                Console.WriteLine($"'(double)127' was evaluted to '{(double)sByteMaxValue}'. Expected: '127d'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMinValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMinValueCastToDoubleIsFoldedCorrectly()
        {
            short integerOneDecrementUnderSByteMinValue = -129;

            if (BreakUpFlow())
                return;

            if (checked((double)integerOneDecrementUnderSByteMinValue) != -129d)
            {
                Console.WriteLine($"'(double)-129' was evaluted to '{(double)integerOneDecrementUnderSByteMinValue}'. Expected: '-129d'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMinValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMinValueCastToDoubleIsFoldedCorrectly()
        {
            short integerOneIncrementAboveSByteMinValue = -127;

            if (BreakUpFlow())
                return;

            if (checked((double)integerOneIncrementAboveSByteMinValue) != -127d)
            {
                Console.WriteLine($"'(double)-127' was evaluted to '{(double)integerOneIncrementAboveSByteMinValue}'. Expected: '-127d'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToDoubleIsFoldedCorrectly()
        {
            short integerOneDecrementUnderSByteMaxValue = 126;

            if (BreakUpFlow())
                return;

            if (checked((double)integerOneDecrementUnderSByteMaxValue) != 126d)
            {
                Console.WriteLine($"'(double)126' was evaluted to '{(double)integerOneDecrementUnderSByteMaxValue}'. Expected: '126d'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMaxValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMaxValueCastToDoubleIsFoldedCorrectly()
        {
            short integerOneIncrementAboveSByteMaxValue = 128;

            if (BreakUpFlow())
                return;

            if (checked((double)integerOneIncrementAboveSByteMaxValue) != 128d)
            {
                Console.WriteLine($"'(double)128' was evaluted to '{(double)integerOneIncrementAboveSByteMaxValue}'. Expected: '128d'.");
                _counter++;
            }
        }
        ConfirmByteMaxValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmByteMaxValueCastToDoubleIsFoldedCorrectly()
        {
            short byteMaxValue = 255;

            if (BreakUpFlow())
                return;

            if (checked((double)byteMaxValue) != 255d)
            {
                Console.WriteLine($"'(double)255' was evaluted to '{(double)byteMaxValue}'. Expected: '255d'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMinValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMinValueCastToDoubleIsFoldedCorrectly()
        {
            short integerOneDecrementUnderByteMinValue = -1;

            if (BreakUpFlow())
                return;

            if (checked((double)integerOneDecrementUnderByteMinValue) != -1d)
            {
                Console.WriteLine($"'(double)-1' was evaluted to '{(double)integerOneDecrementUnderByteMinValue}'. Expected: '-1d'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToDoubleIsFoldedCorrectly()
        {
            short integerOneIncrementAboveByteMinValue = 1;

            if (BreakUpFlow())
                return;

            if (checked((double)integerOneIncrementAboveByteMinValue) != 1d)
            {
                Console.WriteLine($"'(double)1' was evaluted to '{(double)integerOneIncrementAboveByteMinValue}'. Expected: '1d'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMaxValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMaxValueCastToDoubleIsFoldedCorrectly()
        {
            short integerOneDecrementUnderByteMaxValue = 254;

            if (BreakUpFlow())
                return;

            if (checked((double)integerOneDecrementUnderByteMaxValue) != 254d)
            {
                Console.WriteLine($"'(double)254' was evaluted to '{(double)integerOneDecrementUnderByteMaxValue}'. Expected: '254d'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMaxValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMaxValueCastToDoubleIsFoldedCorrectly()
        {
            short integerOneIncrementAboveByteMaxValue = 256;

            if (BreakUpFlow())
                return;

            if (checked((double)integerOneIncrementAboveByteMaxValue) != 256d)
            {
                Console.WriteLine($"'(double)256' was evaluted to '{(double)integerOneIncrementAboveByteMaxValue}'. Expected: '256d'.");
                _counter++;
            }
        }
        ConfirmInt16MinValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MinValueCastToDoubleIsFoldedCorrectly()
        {
            short int16MinValue = -32768;

            if (BreakUpFlow())
                return;

            if (checked((double)int16MinValue) != -32768d)
            {
                Console.WriteLine($"'(double)-32768' was evaluted to '{(double)int16MinValue}'. Expected: '-32768d'.");
                _counter++;
            }
        }
        ConfirmInt16MaxValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MaxValueCastToDoubleIsFoldedCorrectly()
        {
            short int16MaxValue = 32767;

            if (BreakUpFlow())
                return;

            if (checked((double)int16MaxValue) != 32767d)
            {
                Console.WriteLine($"'(double)32767' was evaluted to '{(double)int16MaxValue}'. Expected: '32767d'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt16MinValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt16MinValueCastToDoubleIsFoldedCorrectly()
        {
            short integerOneIncrementAboveInt16MinValue = -32767;

            if (BreakUpFlow())
                return;

            if (checked((double)integerOneIncrementAboveInt16MinValue) != -32767d)
            {
                Console.WriteLine($"'(double)-32767' was evaluted to '{(double)integerOneIncrementAboveInt16MinValue}'. Expected: '-32767d'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt16MaxValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MaxValueCastToDoubleIsFoldedCorrectly()
        {
            short integerOneDecrementUnderInt16MaxValue = 32766;

            if (BreakUpFlow())
                return;

            if (checked((double)integerOneDecrementUnderInt16MaxValue) != 32766d)
            {
                Console.WriteLine($"'(double)32766' was evaluted to '{(double)integerOneDecrementUnderInt16MaxValue}'. Expected: '32766d'.");
                _counter++;
            }
        }
    }

    private static void TestCastingInt16ToSByte()
    {
        ConfirmIntegerZeroCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToSByteIsFoldedCorrectly()
        {
            short integerZero = 0;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)integerZero) != 0)
            {
                Console.WriteLine($"'(sbyte)0' was evaluted to '{(sbyte)integerZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmSByteMinValueCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMinValueCastToSByteIsFoldedCorrectly()
        {
            short sByteMinValue = -128;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)sByteMinValue) != -128)
            {
                Console.WriteLine($"'(sbyte)-128' was evaluted to '{(sbyte)sByteMinValue}'. Expected: '-128'.");
                _counter++;
            }
        }
        ConfirmSByteMaxValueCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToSByteIsFoldedCorrectly()
        {
            short sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(sbyte)127' was evaluted to '{(sbyte)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmInt16OneDecrementUnderSByteMinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16OneDecrementUnderSByteMinValueCastToSByteOverflows()
        {
            short from = -129;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-129)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveSByteMinValueCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMinValueCastToSByteIsFoldedCorrectly()
        {
            short integerOneIncrementAboveSByteMinValue = -127;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)integerOneIncrementAboveSByteMinValue) != -127)
            {
                Console.WriteLine($"'(sbyte)-127' was evaluted to '{(sbyte)integerOneIncrementAboveSByteMinValue}'. Expected: '-127'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToSByteIsFoldedCorrectly()
        {
            short integerOneDecrementUnderSByteMaxValue = 126;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(sbyte)126' was evaluted to '{(sbyte)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmInt16OneIncrementAboveSByteMaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16OneIncrementAboveSByteMaxValueCastToSByteOverflows()
        {
            short from = 128;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)128)' did not throw OverflowException.");
        }
        ConfirmByteMaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmByteMaxValueCastToSByteOverflows()
        {
            short from = 255;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)255)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderByteMinValueCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMinValueCastToSByteIsFoldedCorrectly()
        {
            short integerOneDecrementUnderByteMinValue = -1;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)integerOneDecrementUnderByteMinValue) != -1)
            {
                Console.WriteLine($"'(sbyte)-1' was evaluted to '{(sbyte)integerOneDecrementUnderByteMinValue}'. Expected: '-1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToSByteIsFoldedCorrectly()
        {
            short integerOneIncrementAboveByteMinValue = 1;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(sbyte)1' was evaluted to '{(sbyte)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmInt16OneDecrementUnderByteMaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16OneDecrementUnderByteMaxValueCastToSByteOverflows()
        {
            short from = 254;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)254)' did not throw OverflowException.");
        }
        ConfirmInt16OneIncrementAboveByteMaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16OneIncrementAboveByteMaxValueCastToSByteOverflows()
        {
            short from = 256;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)256)' did not throw OverflowException.");
        }
        ConfirmInt16MinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MinValueCastToSByteOverflows()
        {
            short from = -32768;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-32768)' did not throw OverflowException.");
        }
        ConfirmInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MaxValueCastToSByteOverflows()
        {
            short from = 32767;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)32767)' did not throw OverflowException.");
        }
        ConfirmInt16OneIncrementAboveInt16MinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16OneIncrementAboveInt16MinValueCastToSByteOverflows()
        {
            short from = -32767;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-32767)' did not throw OverflowException.");
        }
        ConfirmInt16OneDecrementUnderInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16OneDecrementUnderInt16MaxValueCastToSByteOverflows()
        {
            short from = 32766;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)32766)' did not throw OverflowException.");
        }
    }

    private static void TestCastingInt16ToByte()
    {
        ConfirmIntegerZeroCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToByteIsFoldedCorrectly()
        {
            short integerZero = 0;

            if (BreakUpFlow())
                return;

            if (checked((byte)integerZero) != 0)
            {
                Console.WriteLine($"'(byte)0' was evaluted to '{(byte)integerZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmSByteMinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMinValueCastToByteOverflows()
        {
            short from = -128;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-128)' did not throw OverflowException.");
        }
        ConfirmSByteMaxValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToByteIsFoldedCorrectly()
        {
            short sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((byte)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(byte)127' was evaluted to '{(byte)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmInt16OneDecrementUnderSByteMinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16OneDecrementUnderSByteMinValueCastToByteOverflows()
        {
            short from = -129;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-129)' did not throw OverflowException.");
        }
        ConfirmInt16OneIncrementAboveSByteMinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16OneIncrementAboveSByteMinValueCastToByteOverflows()
        {
            short from = -127;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-127)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToByteIsFoldedCorrectly()
        {
            short integerOneDecrementUnderSByteMaxValue = 126;

            if (BreakUpFlow())
                return;

            if (checked((byte)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(byte)126' was evaluted to '{(byte)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMaxValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMaxValueCastToByteIsFoldedCorrectly()
        {
            short integerOneIncrementAboveSByteMaxValue = 128;

            if (BreakUpFlow())
                return;

            if (checked((byte)integerOneIncrementAboveSByteMaxValue) != 128)
            {
                Console.WriteLine($"'(byte)128' was evaluted to '{(byte)integerOneIncrementAboveSByteMaxValue}'. Expected: '128'.");
                _counter++;
            }
        }
        ConfirmByteMaxValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmByteMaxValueCastToByteIsFoldedCorrectly()
        {
            short byteMaxValue = 255;

            if (BreakUpFlow())
                return;

            if (checked((byte)byteMaxValue) != 255)
            {
                Console.WriteLine($"'(byte)255' was evaluted to '{(byte)byteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmInt16OneDecrementUnderByteMinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16OneDecrementUnderByteMinValueCastToByteOverflows()
        {
            short from = -1;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-1)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToByteIsFoldedCorrectly()
        {
            short integerOneIncrementAboveByteMinValue = 1;

            if (BreakUpFlow())
                return;

            if (checked((byte)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(byte)1' was evaluted to '{(byte)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMaxValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMaxValueCastToByteIsFoldedCorrectly()
        {
            short integerOneDecrementUnderByteMaxValue = 254;

            if (BreakUpFlow())
                return;

            if (checked((byte)integerOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(byte)254' was evaluted to '{(byte)integerOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmInt16OneIncrementAboveByteMaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16OneIncrementAboveByteMaxValueCastToByteOverflows()
        {
            short from = 256;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)256)' did not throw OverflowException.");
        }
        ConfirmInt16MinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MinValueCastToByteOverflows()
        {
            short from = -32768;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-32768)' did not throw OverflowException.");
        }
        ConfirmInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MaxValueCastToByteOverflows()
        {
            short from = 32767;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)32767)' did not throw OverflowException.");
        }
        ConfirmInt16OneIncrementAboveInt16MinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16OneIncrementAboveInt16MinValueCastToByteOverflows()
        {
            short from = -32767;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-32767)' did not throw OverflowException.");
        }
        ConfirmInt16OneDecrementUnderInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16OneDecrementUnderInt16MaxValueCastToByteOverflows()
        {
            short from = 32766;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)32766)' did not throw OverflowException.");
        }
    }

    private static void TestCastingInt16ToInt16()
    {
        ConfirmIntegerZeroCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToInt16IsFoldedCorrectly()
        {
            short integerZero = 0;

            if (BreakUpFlow())
                return;

            if (checked((short)integerZero) != 0)
            {
                Console.WriteLine($"'(short)0' was evaluted to '{(short)integerZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmSByteMinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMinValueCastToInt16IsFoldedCorrectly()
        {
            short sByteMinValue = -128;

            if (BreakUpFlow())
                return;

            if (checked((short)sByteMinValue) != -128)
            {
                Console.WriteLine($"'(short)-128' was evaluted to '{(short)sByteMinValue}'. Expected: '-128'.");
                _counter++;
            }
        }
        ConfirmSByteMaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToInt16IsFoldedCorrectly()
        {
            short sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((short)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(short)127' was evaluted to '{(short)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMinValueCastToInt16IsFoldedCorrectly()
        {
            short integerOneDecrementUnderSByteMinValue = -129;

            if (BreakUpFlow())
                return;

            if (checked((short)integerOneDecrementUnderSByteMinValue) != -129)
            {
                Console.WriteLine($"'(short)-129' was evaluted to '{(short)integerOneDecrementUnderSByteMinValue}'. Expected: '-129'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMinValueCastToInt16IsFoldedCorrectly()
        {
            short integerOneIncrementAboveSByteMinValue = -127;

            if (BreakUpFlow())
                return;

            if (checked((short)integerOneIncrementAboveSByteMinValue) != -127)
            {
                Console.WriteLine($"'(short)-127' was evaluted to '{(short)integerOneIncrementAboveSByteMinValue}'. Expected: '-127'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToInt16IsFoldedCorrectly()
        {
            short integerOneDecrementUnderSByteMaxValue = 126;

            if (BreakUpFlow())
                return;

            if (checked((short)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(short)126' was evaluted to '{(short)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMaxValueCastToInt16IsFoldedCorrectly()
        {
            short integerOneIncrementAboveSByteMaxValue = 128;

            if (BreakUpFlow())
                return;

            if (checked((short)integerOneIncrementAboveSByteMaxValue) != 128)
            {
                Console.WriteLine($"'(short)128' was evaluted to '{(short)integerOneIncrementAboveSByteMaxValue}'. Expected: '128'.");
                _counter++;
            }
        }
        ConfirmByteMaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmByteMaxValueCastToInt16IsFoldedCorrectly()
        {
            short byteMaxValue = 255;

            if (BreakUpFlow())
                return;

            if (checked((short)byteMaxValue) != 255)
            {
                Console.WriteLine($"'(short)255' was evaluted to '{(short)byteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMinValueCastToInt16IsFoldedCorrectly()
        {
            short integerOneDecrementUnderByteMinValue = -1;

            if (BreakUpFlow())
                return;

            if (checked((short)integerOneDecrementUnderByteMinValue) != -1)
            {
                Console.WriteLine($"'(short)-1' was evaluted to '{(short)integerOneDecrementUnderByteMinValue}'. Expected: '-1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToInt16IsFoldedCorrectly()
        {
            short integerOneIncrementAboveByteMinValue = 1;

            if (BreakUpFlow())
                return;

            if (checked((short)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(short)1' was evaluted to '{(short)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMaxValueCastToInt16IsFoldedCorrectly()
        {
            short integerOneDecrementUnderByteMaxValue = 254;

            if (BreakUpFlow())
                return;

            if (checked((short)integerOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(short)254' was evaluted to '{(short)integerOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMaxValueCastToInt16IsFoldedCorrectly()
        {
            short integerOneIncrementAboveByteMaxValue = 256;

            if (BreakUpFlow())
                return;

            if (checked((short)integerOneIncrementAboveByteMaxValue) != 256)
            {
                Console.WriteLine($"'(short)256' was evaluted to '{(short)integerOneIncrementAboveByteMaxValue}'. Expected: '256'.");
                _counter++;
            }
        }
        ConfirmInt16MinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MinValueCastToInt16IsFoldedCorrectly()
        {
            short int16MinValue = -32768;

            if (BreakUpFlow())
                return;

            if (checked((short)int16MinValue) != -32768)
            {
                Console.WriteLine($"'(short)-32768' was evaluted to '{(short)int16MinValue}'. Expected: '-32768'.");
                _counter++;
            }
        }
        ConfirmInt16MaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MaxValueCastToInt16IsFoldedCorrectly()
        {
            short int16MaxValue = 32767;

            if (BreakUpFlow())
                return;

            if (checked((short)int16MaxValue) != 32767)
            {
                Console.WriteLine($"'(short)32767' was evaluted to '{(short)int16MaxValue}'. Expected: '32767'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt16MinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt16MinValueCastToInt16IsFoldedCorrectly()
        {
            short integerOneIncrementAboveInt16MinValue = -32767;

            if (BreakUpFlow())
                return;

            if (checked((short)integerOneIncrementAboveInt16MinValue) != -32767)
            {
                Console.WriteLine($"'(short)-32767' was evaluted to '{(short)integerOneIncrementAboveInt16MinValue}'. Expected: '-32767'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt16MaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MaxValueCastToInt16IsFoldedCorrectly()
        {
            short integerOneDecrementUnderInt16MaxValue = 32766;

            if (BreakUpFlow())
                return;

            if (checked((short)integerOneDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(short)32766' was evaluted to '{(short)integerOneDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
    }

    private static void TestCastingInt16ToUInt16()
    {
        ConfirmIntegerZeroCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToUInt16IsFoldedCorrectly()
        {
            short integerZero = 0;

            if (BreakUpFlow())
                return;

            if (checked((ushort)integerZero) != 0)
            {
                Console.WriteLine($"'(ushort)0' was evaluted to '{(ushort)integerZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmSByteMinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMinValueCastToUInt16Overflows()
        {
            short from = -128;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-128)' did not throw OverflowException.");
        }
        ConfirmSByteMaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToUInt16IsFoldedCorrectly()
        {
            short sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((ushort)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(ushort)127' was evaluted to '{(ushort)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmInt16OneDecrementUnderSByteMinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16OneDecrementUnderSByteMinValueCastToUInt16Overflows()
        {
            short from = -129;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-129)' did not throw OverflowException.");
        }
        ConfirmInt16OneIncrementAboveSByteMinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16OneIncrementAboveSByteMinValueCastToUInt16Overflows()
        {
            short from = -127;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-127)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToUInt16IsFoldedCorrectly()
        {
            short integerOneDecrementUnderSByteMaxValue = 126;

            if (BreakUpFlow())
                return;

            if (checked((ushort)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(ushort)126' was evaluted to '{(ushort)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMaxValueCastToUInt16IsFoldedCorrectly()
        {
            short integerOneIncrementAboveSByteMaxValue = 128;

            if (BreakUpFlow())
                return;

            if (checked((ushort)integerOneIncrementAboveSByteMaxValue) != 128)
            {
                Console.WriteLine($"'(ushort)128' was evaluted to '{(ushort)integerOneIncrementAboveSByteMaxValue}'. Expected: '128'.");
                _counter++;
            }
        }
        ConfirmByteMaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmByteMaxValueCastToUInt16IsFoldedCorrectly()
        {
            short byteMaxValue = 255;

            if (BreakUpFlow())
                return;

            if (checked((ushort)byteMaxValue) != 255)
            {
                Console.WriteLine($"'(ushort)255' was evaluted to '{(ushort)byteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmInt16OneDecrementUnderByteMinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16OneDecrementUnderByteMinValueCastToUInt16Overflows()
        {
            short from = -1;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-1)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToUInt16IsFoldedCorrectly()
        {
            short integerOneIncrementAboveByteMinValue = 1;

            if (BreakUpFlow())
                return;

            if (checked((ushort)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(ushort)1' was evaluted to '{(ushort)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMaxValueCastToUInt16IsFoldedCorrectly()
        {
            short integerOneDecrementUnderByteMaxValue = 254;

            if (BreakUpFlow())
                return;

            if (checked((ushort)integerOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(ushort)254' was evaluted to '{(ushort)integerOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMaxValueCastToUInt16IsFoldedCorrectly()
        {
            short integerOneIncrementAboveByteMaxValue = 256;

            if (BreakUpFlow())
                return;

            if (checked((ushort)integerOneIncrementAboveByteMaxValue) != 256)
            {
                Console.WriteLine($"'(ushort)256' was evaluted to '{(ushort)integerOneIncrementAboveByteMaxValue}'. Expected: '256'.");
                _counter++;
            }
        }
        ConfirmInt16MinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MinValueCastToUInt16Overflows()
        {
            short from = -32768;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-32768)' did not throw OverflowException.");
        }
        ConfirmInt16MaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MaxValueCastToUInt16IsFoldedCorrectly()
        {
            short int16MaxValue = 32767;

            if (BreakUpFlow())
                return;

            if (checked((ushort)int16MaxValue) != 32767)
            {
                Console.WriteLine($"'(ushort)32767' was evaluted to '{(ushort)int16MaxValue}'. Expected: '32767'.");
                _counter++;
            }
        }
        ConfirmInt16OneIncrementAboveInt16MinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16OneIncrementAboveInt16MinValueCastToUInt16Overflows()
        {
            short from = -32767;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-32767)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt16MaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MaxValueCastToUInt16IsFoldedCorrectly()
        {
            short integerOneDecrementUnderInt16MaxValue = 32766;

            if (BreakUpFlow())
                return;

            if (checked((ushort)integerOneDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(ushort)32766' was evaluted to '{(ushort)integerOneDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
    }

    private static void TestCastingInt16ToInt32()
    {
        ConfirmIntegerZeroCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToInt32IsFoldedCorrectly()
        {
            short integerZero = 0;

            if (BreakUpFlow())
                return;

            if (checked((int)integerZero) != 0)
            {
                Console.WriteLine($"'(int)0' was evaluted to '{(int)integerZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmSByteMinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMinValueCastToInt32IsFoldedCorrectly()
        {
            short sByteMinValue = -128;

            if (BreakUpFlow())
                return;

            if (checked((int)sByteMinValue) != -128)
            {
                Console.WriteLine($"'(int)-128' was evaluted to '{(int)sByteMinValue}'. Expected: '-128'.");
                _counter++;
            }
        }
        ConfirmSByteMaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToInt32IsFoldedCorrectly()
        {
            short sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((int)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(int)127' was evaluted to '{(int)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMinValueCastToInt32IsFoldedCorrectly()
        {
            short integerOneDecrementUnderSByteMinValue = -129;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneDecrementUnderSByteMinValue) != -129)
            {
                Console.WriteLine($"'(int)-129' was evaluted to '{(int)integerOneDecrementUnderSByteMinValue}'. Expected: '-129'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMinValueCastToInt32IsFoldedCorrectly()
        {
            short integerOneIncrementAboveSByteMinValue = -127;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneIncrementAboveSByteMinValue) != -127)
            {
                Console.WriteLine($"'(int)-127' was evaluted to '{(int)integerOneIncrementAboveSByteMinValue}'. Expected: '-127'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToInt32IsFoldedCorrectly()
        {
            short integerOneDecrementUnderSByteMaxValue = 126;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(int)126' was evaluted to '{(int)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMaxValueCastToInt32IsFoldedCorrectly()
        {
            short integerOneIncrementAboveSByteMaxValue = 128;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneIncrementAboveSByteMaxValue) != 128)
            {
                Console.WriteLine($"'(int)128' was evaluted to '{(int)integerOneIncrementAboveSByteMaxValue}'. Expected: '128'.");
                _counter++;
            }
        }
        ConfirmByteMaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmByteMaxValueCastToInt32IsFoldedCorrectly()
        {
            short byteMaxValue = 255;

            if (BreakUpFlow())
                return;

            if (checked((int)byteMaxValue) != 255)
            {
                Console.WriteLine($"'(int)255' was evaluted to '{(int)byteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMinValueCastToInt32IsFoldedCorrectly()
        {
            short integerOneDecrementUnderByteMinValue = -1;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneDecrementUnderByteMinValue) != -1)
            {
                Console.WriteLine($"'(int)-1' was evaluted to '{(int)integerOneDecrementUnderByteMinValue}'. Expected: '-1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToInt32IsFoldedCorrectly()
        {
            short integerOneIncrementAboveByteMinValue = 1;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(int)1' was evaluted to '{(int)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMaxValueCastToInt32IsFoldedCorrectly()
        {
            short integerOneDecrementUnderByteMaxValue = 254;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(int)254' was evaluted to '{(int)integerOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMaxValueCastToInt32IsFoldedCorrectly()
        {
            short integerOneIncrementAboveByteMaxValue = 256;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneIncrementAboveByteMaxValue) != 256)
            {
                Console.WriteLine($"'(int)256' was evaluted to '{(int)integerOneIncrementAboveByteMaxValue}'. Expected: '256'.");
                _counter++;
            }
        }
        ConfirmInt16MinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MinValueCastToInt32IsFoldedCorrectly()
        {
            short int16MinValue = -32768;

            if (BreakUpFlow())
                return;

            if (checked((int)int16MinValue) != -32768)
            {
                Console.WriteLine($"'(int)-32768' was evaluted to '{(int)int16MinValue}'. Expected: '-32768'.");
                _counter++;
            }
        }
        ConfirmInt16MaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MaxValueCastToInt32IsFoldedCorrectly()
        {
            short int16MaxValue = 32767;

            if (BreakUpFlow())
                return;

            if (checked((int)int16MaxValue) != 32767)
            {
                Console.WriteLine($"'(int)32767' was evaluted to '{(int)int16MaxValue}'. Expected: '32767'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt16MinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt16MinValueCastToInt32IsFoldedCorrectly()
        {
            short integerOneIncrementAboveInt16MinValue = -32767;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneIncrementAboveInt16MinValue) != -32767)
            {
                Console.WriteLine($"'(int)-32767' was evaluted to '{(int)integerOneIncrementAboveInt16MinValue}'. Expected: '-32767'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt16MaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MaxValueCastToInt32IsFoldedCorrectly()
        {
            short integerOneDecrementUnderInt16MaxValue = 32766;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(int)32766' was evaluted to '{(int)integerOneDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
    }

    private static void TestCastingInt16ToUInt32()
    {
        ConfirmIntegerZeroCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToUInt32IsFoldedCorrectly()
        {
            short integerZero = 0;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerZero) != 0)
            {
                Console.WriteLine($"'(uint)0' was evaluted to '{(uint)integerZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmSByteMinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMinValueCastToUInt32Overflows()
        {
            short from = -128;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-128)' did not throw OverflowException.");
        }
        ConfirmSByteMaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToUInt32IsFoldedCorrectly()
        {
            short sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((uint)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(uint)127' was evaluted to '{(uint)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmInt16OneDecrementUnderSByteMinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16OneDecrementUnderSByteMinValueCastToUInt32Overflows()
        {
            short from = -129;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-129)' did not throw OverflowException.");
        }
        ConfirmInt16OneIncrementAboveSByteMinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16OneIncrementAboveSByteMinValueCastToUInt32Overflows()
        {
            short from = -127;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-127)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToUInt32IsFoldedCorrectly()
        {
            short integerOneDecrementUnderSByteMaxValue = 126;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(uint)126' was evaluted to '{(uint)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMaxValueCastToUInt32IsFoldedCorrectly()
        {
            short integerOneIncrementAboveSByteMaxValue = 128;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneIncrementAboveSByteMaxValue) != 128)
            {
                Console.WriteLine($"'(uint)128' was evaluted to '{(uint)integerOneIncrementAboveSByteMaxValue}'. Expected: '128'.");
                _counter++;
            }
        }
        ConfirmByteMaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmByteMaxValueCastToUInt32IsFoldedCorrectly()
        {
            short byteMaxValue = 255;

            if (BreakUpFlow())
                return;

            if (checked((uint)byteMaxValue) != 255)
            {
                Console.WriteLine($"'(uint)255' was evaluted to '{(uint)byteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmInt16OneDecrementUnderByteMinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16OneDecrementUnderByteMinValueCastToUInt32Overflows()
        {
            short from = -1;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-1)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToUInt32IsFoldedCorrectly()
        {
            short integerOneIncrementAboveByteMinValue = 1;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(uint)1' was evaluted to '{(uint)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMaxValueCastToUInt32IsFoldedCorrectly()
        {
            short integerOneDecrementUnderByteMaxValue = 254;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(uint)254' was evaluted to '{(uint)integerOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMaxValueCastToUInt32IsFoldedCorrectly()
        {
            short integerOneIncrementAboveByteMaxValue = 256;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneIncrementAboveByteMaxValue) != 256)
            {
                Console.WriteLine($"'(uint)256' was evaluted to '{(uint)integerOneIncrementAboveByteMaxValue}'. Expected: '256'.");
                _counter++;
            }
        }
        ConfirmInt16MinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MinValueCastToUInt32Overflows()
        {
            short from = -32768;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-32768)' did not throw OverflowException.");
        }
        ConfirmInt16MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MaxValueCastToUInt32IsFoldedCorrectly()
        {
            short int16MaxValue = 32767;

            if (BreakUpFlow())
                return;

            if (checked((uint)int16MaxValue) != 32767)
            {
                Console.WriteLine($"'(uint)32767' was evaluted to '{(uint)int16MaxValue}'. Expected: '32767'.");
                _counter++;
            }
        }
        ConfirmInt16OneIncrementAboveInt16MinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16OneIncrementAboveInt16MinValueCastToUInt32Overflows()
        {
            short from = -32767;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-32767)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt16MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MaxValueCastToUInt32IsFoldedCorrectly()
        {
            short integerOneDecrementUnderInt16MaxValue = 32766;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(uint)32766' was evaluted to '{(uint)integerOneDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
    }

    private static void TestCastingInt16ToInt64()
    {
        ConfirmIntegerZeroCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToInt64IsFoldedCorrectly()
        {
            short integerZero = 0;

            if (BreakUpFlow())
                return;

            if (checked((long)integerZero) != 0)
            {
                Console.WriteLine($"'(long)0' was evaluted to '{(long)integerZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmSByteMinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMinValueCastToInt64IsFoldedCorrectly()
        {
            short sByteMinValue = -128;

            if (BreakUpFlow())
                return;

            if (checked((long)sByteMinValue) != -128)
            {
                Console.WriteLine($"'(long)-128' was evaluted to '{(long)sByteMinValue}'. Expected: '-128'.");
                _counter++;
            }
        }
        ConfirmSByteMaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToInt64IsFoldedCorrectly()
        {
            short sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((long)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(long)127' was evaluted to '{(long)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMinValueCastToInt64IsFoldedCorrectly()
        {
            short integerOneDecrementUnderSByteMinValue = -129;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneDecrementUnderSByteMinValue) != -129)
            {
                Console.WriteLine($"'(long)-129' was evaluted to '{(long)integerOneDecrementUnderSByteMinValue}'. Expected: '-129'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMinValueCastToInt64IsFoldedCorrectly()
        {
            short integerOneIncrementAboveSByteMinValue = -127;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneIncrementAboveSByteMinValue) != -127)
            {
                Console.WriteLine($"'(long)-127' was evaluted to '{(long)integerOneIncrementAboveSByteMinValue}'. Expected: '-127'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToInt64IsFoldedCorrectly()
        {
            short integerOneDecrementUnderSByteMaxValue = 126;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(long)126' was evaluted to '{(long)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMaxValueCastToInt64IsFoldedCorrectly()
        {
            short integerOneIncrementAboveSByteMaxValue = 128;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneIncrementAboveSByteMaxValue) != 128)
            {
                Console.WriteLine($"'(long)128' was evaluted to '{(long)integerOneIncrementAboveSByteMaxValue}'. Expected: '128'.");
                _counter++;
            }
        }
        ConfirmByteMaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmByteMaxValueCastToInt64IsFoldedCorrectly()
        {
            short byteMaxValue = 255;

            if (BreakUpFlow())
                return;

            if (checked((long)byteMaxValue) != 255)
            {
                Console.WriteLine($"'(long)255' was evaluted to '{(long)byteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMinValueCastToInt64IsFoldedCorrectly()
        {
            short integerOneDecrementUnderByteMinValue = -1;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneDecrementUnderByteMinValue) != -1)
            {
                Console.WriteLine($"'(long)-1' was evaluted to '{(long)integerOneDecrementUnderByteMinValue}'. Expected: '-1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToInt64IsFoldedCorrectly()
        {
            short integerOneIncrementAboveByteMinValue = 1;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(long)1' was evaluted to '{(long)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMaxValueCastToInt64IsFoldedCorrectly()
        {
            short integerOneDecrementUnderByteMaxValue = 254;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(long)254' was evaluted to '{(long)integerOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMaxValueCastToInt64IsFoldedCorrectly()
        {
            short integerOneIncrementAboveByteMaxValue = 256;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneIncrementAboveByteMaxValue) != 256)
            {
                Console.WriteLine($"'(long)256' was evaluted to '{(long)integerOneIncrementAboveByteMaxValue}'. Expected: '256'.");
                _counter++;
            }
        }
        ConfirmInt16MinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MinValueCastToInt64IsFoldedCorrectly()
        {
            short int16MinValue = -32768;

            if (BreakUpFlow())
                return;

            if (checked((long)int16MinValue) != -32768)
            {
                Console.WriteLine($"'(long)-32768' was evaluted to '{(long)int16MinValue}'. Expected: '-32768'.");
                _counter++;
            }
        }
        ConfirmInt16MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MaxValueCastToInt64IsFoldedCorrectly()
        {
            short int16MaxValue = 32767;

            if (BreakUpFlow())
                return;

            if (checked((long)int16MaxValue) != 32767)
            {
                Console.WriteLine($"'(long)32767' was evaluted to '{(long)int16MaxValue}'. Expected: '32767'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt16MinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt16MinValueCastToInt64IsFoldedCorrectly()
        {
            short integerOneIncrementAboveInt16MinValue = -32767;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneIncrementAboveInt16MinValue) != -32767)
            {
                Console.WriteLine($"'(long)-32767' was evaluted to '{(long)integerOneIncrementAboveInt16MinValue}'. Expected: '-32767'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt16MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MaxValueCastToInt64IsFoldedCorrectly()
        {
            short integerOneDecrementUnderInt16MaxValue = 32766;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(long)32766' was evaluted to '{(long)integerOneDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
    }

    private static void TestCastingInt16ToUInt64()
    {
        ConfirmIntegerZeroCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToUInt64IsFoldedCorrectly()
        {
            short integerZero = 0;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerZero) != 0)
            {
                Console.WriteLine($"'(ulong)0' was evaluted to '{(ulong)integerZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmSByteMinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMinValueCastToUInt64Overflows()
        {
            short from = -128;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-128)' did not throw OverflowException.");
        }
        ConfirmSByteMaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToUInt64IsFoldedCorrectly()
        {
            short sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((ulong)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(ulong)127' was evaluted to '{(ulong)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmInt16OneDecrementUnderSByteMinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16OneDecrementUnderSByteMinValueCastToUInt64Overflows()
        {
            short from = -129;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-129)' did not throw OverflowException.");
        }
        ConfirmInt16OneIncrementAboveSByteMinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16OneIncrementAboveSByteMinValueCastToUInt64Overflows()
        {
            short from = -127;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-127)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToUInt64IsFoldedCorrectly()
        {
            short integerOneDecrementUnderSByteMaxValue = 126;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(ulong)126' was evaluted to '{(ulong)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMaxValueCastToUInt64IsFoldedCorrectly()
        {
            short integerOneIncrementAboveSByteMaxValue = 128;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneIncrementAboveSByteMaxValue) != 128)
            {
                Console.WriteLine($"'(ulong)128' was evaluted to '{(ulong)integerOneIncrementAboveSByteMaxValue}'. Expected: '128'.");
                _counter++;
            }
        }
        ConfirmByteMaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmByteMaxValueCastToUInt64IsFoldedCorrectly()
        {
            short byteMaxValue = 255;

            if (BreakUpFlow())
                return;

            if (checked((ulong)byteMaxValue) != 255)
            {
                Console.WriteLine($"'(ulong)255' was evaluted to '{(ulong)byteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmInt16OneDecrementUnderByteMinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16OneDecrementUnderByteMinValueCastToUInt64Overflows()
        {
            short from = -1;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-1)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToUInt64IsFoldedCorrectly()
        {
            short integerOneIncrementAboveByteMinValue = 1;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(ulong)1' was evaluted to '{(ulong)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMaxValueCastToUInt64IsFoldedCorrectly()
        {
            short integerOneDecrementUnderByteMaxValue = 254;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(ulong)254' was evaluted to '{(ulong)integerOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMaxValueCastToUInt64IsFoldedCorrectly()
        {
            short integerOneIncrementAboveByteMaxValue = 256;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneIncrementAboveByteMaxValue) != 256)
            {
                Console.WriteLine($"'(ulong)256' was evaluted to '{(ulong)integerOneIncrementAboveByteMaxValue}'. Expected: '256'.");
                _counter++;
            }
        }
        ConfirmInt16MinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MinValueCastToUInt64Overflows()
        {
            short from = -32768;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-32768)' did not throw OverflowException.");
        }
        ConfirmInt16MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MaxValueCastToUInt64IsFoldedCorrectly()
        {
            short int16MaxValue = 32767;

            if (BreakUpFlow())
                return;

            if (checked((ulong)int16MaxValue) != 32767)
            {
                Console.WriteLine($"'(ulong)32767' was evaluted to '{(ulong)int16MaxValue}'. Expected: '32767'.");
                _counter++;
            }
        }
        ConfirmInt16OneIncrementAboveInt16MinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16OneIncrementAboveInt16MinValueCastToUInt64Overflows()
        {
            short from = -32767;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-32767)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt16MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MaxValueCastToUInt64IsFoldedCorrectly()
        {
            short integerOneDecrementUnderInt16MaxValue = 32766;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(ulong)32766' was evaluted to '{(ulong)integerOneDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
    }

    private static void TestCastingUInt16ToSingle()
    {
        ConfirmIntegerZeroCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToSingleIsFoldedCorrectly()
        {
            ushort integerZero = 0;

            if (BreakUpFlow())
                return;

            if (checked((float)integerZero) != 0f)
            {
                Console.WriteLine($"'(float)0' was evaluted to '{(float)integerZero}'. Expected: '0f'.");
                _counter++;
            }
        }
        ConfirmSByteMaxValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToSingleIsFoldedCorrectly()
        {
            ushort sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((float)sByteMaxValue) != 127f)
            {
                Console.WriteLine($"'(float)127' was evaluted to '{(float)sByteMaxValue}'. Expected: '127f'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToSingleIsFoldedCorrectly()
        {
            ushort integerOneDecrementUnderSByteMaxValue = 126;

            if (BreakUpFlow())
                return;

            if (checked((float)integerOneDecrementUnderSByteMaxValue) != 126f)
            {
                Console.WriteLine($"'(float)126' was evaluted to '{(float)integerOneDecrementUnderSByteMaxValue}'. Expected: '126f'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMaxValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMaxValueCastToSingleIsFoldedCorrectly()
        {
            ushort integerOneIncrementAboveSByteMaxValue = 128;

            if (BreakUpFlow())
                return;

            if (checked((float)integerOneIncrementAboveSByteMaxValue) != 128f)
            {
                Console.WriteLine($"'(float)128' was evaluted to '{(float)integerOneIncrementAboveSByteMaxValue}'. Expected: '128f'.");
                _counter++;
            }
        }
        ConfirmByteMaxValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmByteMaxValueCastToSingleIsFoldedCorrectly()
        {
            ushort byteMaxValue = 255;

            if (BreakUpFlow())
                return;

            if (checked((float)byteMaxValue) != 255f)
            {
                Console.WriteLine($"'(float)255' was evaluted to '{(float)byteMaxValue}'. Expected: '255f'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToSingleIsFoldedCorrectly()
        {
            ushort integerOneIncrementAboveByteMinValue = 1;

            if (BreakUpFlow())
                return;

            if (checked((float)integerOneIncrementAboveByteMinValue) != 1f)
            {
                Console.WriteLine($"'(float)1' was evaluted to '{(float)integerOneIncrementAboveByteMinValue}'. Expected: '1f'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMaxValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMaxValueCastToSingleIsFoldedCorrectly()
        {
            ushort integerOneDecrementUnderByteMaxValue = 254;

            if (BreakUpFlow())
                return;

            if (checked((float)integerOneDecrementUnderByteMaxValue) != 254f)
            {
                Console.WriteLine($"'(float)254' was evaluted to '{(float)integerOneDecrementUnderByteMaxValue}'. Expected: '254f'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMaxValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMaxValueCastToSingleIsFoldedCorrectly()
        {
            ushort integerOneIncrementAboveByteMaxValue = 256;

            if (BreakUpFlow())
                return;

            if (checked((float)integerOneIncrementAboveByteMaxValue) != 256f)
            {
                Console.WriteLine($"'(float)256' was evaluted to '{(float)integerOneIncrementAboveByteMaxValue}'. Expected: '256f'.");
                _counter++;
            }
        }
        ConfirmInt16MaxValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MaxValueCastToSingleIsFoldedCorrectly()
        {
            ushort int16MaxValue = 32767;

            if (BreakUpFlow())
                return;

            if (checked((float)int16MaxValue) != 32767f)
            {
                Console.WriteLine($"'(float)32767' was evaluted to '{(float)int16MaxValue}'. Expected: '32767f'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt16MaxValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MaxValueCastToSingleIsFoldedCorrectly()
        {
            ushort integerOneDecrementUnderInt16MaxValue = 32766;

            if (BreakUpFlow())
                return;

            if (checked((float)integerOneDecrementUnderInt16MaxValue) != 32766f)
            {
                Console.WriteLine($"'(float)32766' was evaluted to '{(float)integerOneDecrementUnderInt16MaxValue}'. Expected: '32766f'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt16MaxValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt16MaxValueCastToSingleIsFoldedCorrectly()
        {
            ushort integerOneIncrementAboveInt16MaxValue = 32768;

            if (BreakUpFlow())
                return;

            if (checked((float)integerOneIncrementAboveInt16MaxValue) != 32768f)
            {
                Console.WriteLine($"'(float)32768' was evaluted to '{(float)integerOneIncrementAboveInt16MaxValue}'. Expected: '32768f'.");
                _counter++;
            }
        }
        ConfirmUInt16MaxValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt16MaxValueCastToSingleIsFoldedCorrectly()
        {
            ushort uInt16MaxValue = 65535;

            if (BreakUpFlow())
                return;

            if (checked((float)uInt16MaxValue) != 65535f)
            {
                Console.WriteLine($"'(float)65535' was evaluted to '{(float)uInt16MaxValue}'. Expected: '65535f'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderUInt16MaxValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt16MaxValueCastToSingleIsFoldedCorrectly()
        {
            ushort integerOneDecrementUnderUInt16MaxValue = 65534;

            if (BreakUpFlow())
                return;

            if (checked((float)integerOneDecrementUnderUInt16MaxValue) != 65534f)
            {
                Console.WriteLine($"'(float)65534' was evaluted to '{(float)integerOneDecrementUnderUInt16MaxValue}'. Expected: '65534f'.");
                _counter++;
            }
        }
    }

    private static void TestCastingUInt16ToDouble()
    {
        ConfirmIntegerZeroCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToDoubleIsFoldedCorrectly()
        {
            ushort integerZero = 0;

            if (BreakUpFlow())
                return;

            if (checked((double)integerZero) != 0d)
            {
                Console.WriteLine($"'(double)0' was evaluted to '{(double)integerZero}'. Expected: '0d'.");
                _counter++;
            }
        }
        ConfirmSByteMaxValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToDoubleIsFoldedCorrectly()
        {
            ushort sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((double)sByteMaxValue) != 127d)
            {
                Console.WriteLine($"'(double)127' was evaluted to '{(double)sByteMaxValue}'. Expected: '127d'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToDoubleIsFoldedCorrectly()
        {
            ushort integerOneDecrementUnderSByteMaxValue = 126;

            if (BreakUpFlow())
                return;

            if (checked((double)integerOneDecrementUnderSByteMaxValue) != 126d)
            {
                Console.WriteLine($"'(double)126' was evaluted to '{(double)integerOneDecrementUnderSByteMaxValue}'. Expected: '126d'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMaxValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMaxValueCastToDoubleIsFoldedCorrectly()
        {
            ushort integerOneIncrementAboveSByteMaxValue = 128;

            if (BreakUpFlow())
                return;

            if (checked((double)integerOneIncrementAboveSByteMaxValue) != 128d)
            {
                Console.WriteLine($"'(double)128' was evaluted to '{(double)integerOneIncrementAboveSByteMaxValue}'. Expected: '128d'.");
                _counter++;
            }
        }
        ConfirmByteMaxValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmByteMaxValueCastToDoubleIsFoldedCorrectly()
        {
            ushort byteMaxValue = 255;

            if (BreakUpFlow())
                return;

            if (checked((double)byteMaxValue) != 255d)
            {
                Console.WriteLine($"'(double)255' was evaluted to '{(double)byteMaxValue}'. Expected: '255d'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToDoubleIsFoldedCorrectly()
        {
            ushort integerOneIncrementAboveByteMinValue = 1;

            if (BreakUpFlow())
                return;

            if (checked((double)integerOneIncrementAboveByteMinValue) != 1d)
            {
                Console.WriteLine($"'(double)1' was evaluted to '{(double)integerOneIncrementAboveByteMinValue}'. Expected: '1d'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMaxValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMaxValueCastToDoubleIsFoldedCorrectly()
        {
            ushort integerOneDecrementUnderByteMaxValue = 254;

            if (BreakUpFlow())
                return;

            if (checked((double)integerOneDecrementUnderByteMaxValue) != 254d)
            {
                Console.WriteLine($"'(double)254' was evaluted to '{(double)integerOneDecrementUnderByteMaxValue}'. Expected: '254d'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMaxValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMaxValueCastToDoubleIsFoldedCorrectly()
        {
            ushort integerOneIncrementAboveByteMaxValue = 256;

            if (BreakUpFlow())
                return;

            if (checked((double)integerOneIncrementAboveByteMaxValue) != 256d)
            {
                Console.WriteLine($"'(double)256' was evaluted to '{(double)integerOneIncrementAboveByteMaxValue}'. Expected: '256d'.");
                _counter++;
            }
        }
        ConfirmInt16MaxValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MaxValueCastToDoubleIsFoldedCorrectly()
        {
            ushort int16MaxValue = 32767;

            if (BreakUpFlow())
                return;

            if (checked((double)int16MaxValue) != 32767d)
            {
                Console.WriteLine($"'(double)32767' was evaluted to '{(double)int16MaxValue}'. Expected: '32767d'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt16MaxValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MaxValueCastToDoubleIsFoldedCorrectly()
        {
            ushort integerOneDecrementUnderInt16MaxValue = 32766;

            if (BreakUpFlow())
                return;

            if (checked((double)integerOneDecrementUnderInt16MaxValue) != 32766d)
            {
                Console.WriteLine($"'(double)32766' was evaluted to '{(double)integerOneDecrementUnderInt16MaxValue}'. Expected: '32766d'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt16MaxValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt16MaxValueCastToDoubleIsFoldedCorrectly()
        {
            ushort integerOneIncrementAboveInt16MaxValue = 32768;

            if (BreakUpFlow())
                return;

            if (checked((double)integerOneIncrementAboveInt16MaxValue) != 32768d)
            {
                Console.WriteLine($"'(double)32768' was evaluted to '{(double)integerOneIncrementAboveInt16MaxValue}'. Expected: '32768d'.");
                _counter++;
            }
        }
        ConfirmUInt16MaxValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt16MaxValueCastToDoubleIsFoldedCorrectly()
        {
            ushort uInt16MaxValue = 65535;

            if (BreakUpFlow())
                return;

            if (checked((double)uInt16MaxValue) != 65535d)
            {
                Console.WriteLine($"'(double)65535' was evaluted to '{(double)uInt16MaxValue}'. Expected: '65535d'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderUInt16MaxValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt16MaxValueCastToDoubleIsFoldedCorrectly()
        {
            ushort integerOneDecrementUnderUInt16MaxValue = 65534;

            if (BreakUpFlow())
                return;

            if (checked((double)integerOneDecrementUnderUInt16MaxValue) != 65534d)
            {
                Console.WriteLine($"'(double)65534' was evaluted to '{(double)integerOneDecrementUnderUInt16MaxValue}'. Expected: '65534d'.");
                _counter++;
            }
        }
    }

    private static void TestCastingUInt16ToSByte()
    {
        ConfirmIntegerZeroCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToSByteIsFoldedCorrectly()
        {
            ushort integerZero = 0;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)integerZero) != 0)
            {
                Console.WriteLine($"'(sbyte)0' was evaluted to '{(sbyte)integerZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmSByteMaxValueCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToSByteIsFoldedCorrectly()
        {
            ushort sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(sbyte)127' was evaluted to '{(sbyte)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToSByteIsFoldedCorrectly()
        {
            ushort integerOneDecrementUnderSByteMaxValue = 126;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(sbyte)126' was evaluted to '{(sbyte)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmUInt16OneIncrementAboveSByteMaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt16OneIncrementAboveSByteMaxValueCastToSByteOverflows()
        {
            ushort from = 128;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)128)' did not throw OverflowException.");
        }
        ConfirmByteMaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmByteMaxValueCastToSByteOverflows()
        {
            ushort from = 255;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)255)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToSByteIsFoldedCorrectly()
        {
            ushort integerOneIncrementAboveByteMinValue = 1;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(sbyte)1' was evaluted to '{(sbyte)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmUInt16OneDecrementUnderByteMaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt16OneDecrementUnderByteMaxValueCastToSByteOverflows()
        {
            ushort from = 254;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)254)' did not throw OverflowException.");
        }
        ConfirmUInt16OneIncrementAboveByteMaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt16OneIncrementAboveByteMaxValueCastToSByteOverflows()
        {
            ushort from = 256;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)256)' did not throw OverflowException.");
        }
        ConfirmInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MaxValueCastToSByteOverflows()
        {
            ushort from = 32767;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)32767)' did not throw OverflowException.");
        }
        ConfirmUInt16OneDecrementUnderInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt16OneDecrementUnderInt16MaxValueCastToSByteOverflows()
        {
            ushort from = 32766;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)32766)' did not throw OverflowException.");
        }
        ConfirmUInt16OneIncrementAboveInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt16OneIncrementAboveInt16MaxValueCastToSByteOverflows()
        {
            ushort from = 32768;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)32768)' did not throw OverflowException.");
        }
        ConfirmUInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt16MaxValueCastToSByteOverflows()
        {
            ushort from = 65535;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)65535)' did not throw OverflowException.");
        }
        ConfirmUInt16OneDecrementUnderUInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt16OneDecrementUnderUInt16MaxValueCastToSByteOverflows()
        {
            ushort from = 65534;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)65534)' did not throw OverflowException.");
        }
    }

    private static void TestCastingUInt16ToByte()
    {
        ConfirmIntegerZeroCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToByteIsFoldedCorrectly()
        {
            ushort integerZero = 0;

            if (BreakUpFlow())
                return;

            if (checked((byte)integerZero) != 0)
            {
                Console.WriteLine($"'(byte)0' was evaluted to '{(byte)integerZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmSByteMaxValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToByteIsFoldedCorrectly()
        {
            ushort sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((byte)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(byte)127' was evaluted to '{(byte)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToByteIsFoldedCorrectly()
        {
            ushort integerOneDecrementUnderSByteMaxValue = 126;

            if (BreakUpFlow())
                return;

            if (checked((byte)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(byte)126' was evaluted to '{(byte)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMaxValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMaxValueCastToByteIsFoldedCorrectly()
        {
            ushort integerOneIncrementAboveSByteMaxValue = 128;

            if (BreakUpFlow())
                return;

            if (checked((byte)integerOneIncrementAboveSByteMaxValue) != 128)
            {
                Console.WriteLine($"'(byte)128' was evaluted to '{(byte)integerOneIncrementAboveSByteMaxValue}'. Expected: '128'.");
                _counter++;
            }
        }
        ConfirmByteMaxValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmByteMaxValueCastToByteIsFoldedCorrectly()
        {
            ushort byteMaxValue = 255;

            if (BreakUpFlow())
                return;

            if (checked((byte)byteMaxValue) != 255)
            {
                Console.WriteLine($"'(byte)255' was evaluted to '{(byte)byteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToByteIsFoldedCorrectly()
        {
            ushort integerOneIncrementAboveByteMinValue = 1;

            if (BreakUpFlow())
                return;

            if (checked((byte)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(byte)1' was evaluted to '{(byte)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMaxValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMaxValueCastToByteIsFoldedCorrectly()
        {
            ushort integerOneDecrementUnderByteMaxValue = 254;

            if (BreakUpFlow())
                return;

            if (checked((byte)integerOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(byte)254' was evaluted to '{(byte)integerOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmUInt16OneIncrementAboveByteMaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt16OneIncrementAboveByteMaxValueCastToByteOverflows()
        {
            ushort from = 256;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)256)' did not throw OverflowException.");
        }
        ConfirmInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MaxValueCastToByteOverflows()
        {
            ushort from = 32767;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)32767)' did not throw OverflowException.");
        }
        ConfirmUInt16OneDecrementUnderInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt16OneDecrementUnderInt16MaxValueCastToByteOverflows()
        {
            ushort from = 32766;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)32766)' did not throw OverflowException.");
        }
        ConfirmUInt16OneIncrementAboveInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt16OneIncrementAboveInt16MaxValueCastToByteOverflows()
        {
            ushort from = 32768;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)32768)' did not throw OverflowException.");
        }
        ConfirmUInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt16MaxValueCastToByteOverflows()
        {
            ushort from = 65535;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)65535)' did not throw OverflowException.");
        }
        ConfirmUInt16OneDecrementUnderUInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt16OneDecrementUnderUInt16MaxValueCastToByteOverflows()
        {
            ushort from = 65534;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)65534)' did not throw OverflowException.");
        }
    }

    private static void TestCastingUInt16ToInt16()
    {
        ConfirmIntegerZeroCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToInt16IsFoldedCorrectly()
        {
            ushort integerZero = 0;

            if (BreakUpFlow())
                return;

            if (checked((short)integerZero) != 0)
            {
                Console.WriteLine($"'(short)0' was evaluted to '{(short)integerZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmSByteMaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToInt16IsFoldedCorrectly()
        {
            ushort sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((short)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(short)127' was evaluted to '{(short)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToInt16IsFoldedCorrectly()
        {
            ushort integerOneDecrementUnderSByteMaxValue = 126;

            if (BreakUpFlow())
                return;

            if (checked((short)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(short)126' was evaluted to '{(short)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMaxValueCastToInt16IsFoldedCorrectly()
        {
            ushort integerOneIncrementAboveSByteMaxValue = 128;

            if (BreakUpFlow())
                return;

            if (checked((short)integerOneIncrementAboveSByteMaxValue) != 128)
            {
                Console.WriteLine($"'(short)128' was evaluted to '{(short)integerOneIncrementAboveSByteMaxValue}'. Expected: '128'.");
                _counter++;
            }
        }
        ConfirmByteMaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmByteMaxValueCastToInt16IsFoldedCorrectly()
        {
            ushort byteMaxValue = 255;

            if (BreakUpFlow())
                return;

            if (checked((short)byteMaxValue) != 255)
            {
                Console.WriteLine($"'(short)255' was evaluted to '{(short)byteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToInt16IsFoldedCorrectly()
        {
            ushort integerOneIncrementAboveByteMinValue = 1;

            if (BreakUpFlow())
                return;

            if (checked((short)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(short)1' was evaluted to '{(short)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMaxValueCastToInt16IsFoldedCorrectly()
        {
            ushort integerOneDecrementUnderByteMaxValue = 254;

            if (BreakUpFlow())
                return;

            if (checked((short)integerOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(short)254' was evaluted to '{(short)integerOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMaxValueCastToInt16IsFoldedCorrectly()
        {
            ushort integerOneIncrementAboveByteMaxValue = 256;

            if (BreakUpFlow())
                return;

            if (checked((short)integerOneIncrementAboveByteMaxValue) != 256)
            {
                Console.WriteLine($"'(short)256' was evaluted to '{(short)integerOneIncrementAboveByteMaxValue}'. Expected: '256'.");
                _counter++;
            }
        }
        ConfirmInt16MaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MaxValueCastToInt16IsFoldedCorrectly()
        {
            ushort int16MaxValue = 32767;

            if (BreakUpFlow())
                return;

            if (checked((short)int16MaxValue) != 32767)
            {
                Console.WriteLine($"'(short)32767' was evaluted to '{(short)int16MaxValue}'. Expected: '32767'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt16MaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MaxValueCastToInt16IsFoldedCorrectly()
        {
            ushort integerOneDecrementUnderInt16MaxValue = 32766;

            if (BreakUpFlow())
                return;

            if (checked((short)integerOneDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(short)32766' was evaluted to '{(short)integerOneDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
        ConfirmUInt16OneIncrementAboveInt16MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt16OneIncrementAboveInt16MaxValueCastToInt16Overflows()
        {
            ushort from = 32768;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)32768)' did not throw OverflowException.");
        }
        ConfirmUInt16MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt16MaxValueCastToInt16Overflows()
        {
            ushort from = 65535;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)65535)' did not throw OverflowException.");
        }
        ConfirmUInt16OneDecrementUnderUInt16MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt16OneDecrementUnderUInt16MaxValueCastToInt16Overflows()
        {
            ushort from = 65534;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)65534)' did not throw OverflowException.");
        }
    }

    private static void TestCastingUInt16ToUInt16()
    {
        ConfirmIntegerZeroCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToUInt16IsFoldedCorrectly()
        {
            ushort integerZero = 0;

            if (BreakUpFlow())
                return;

            if (checked((ushort)integerZero) != 0)
            {
                Console.WriteLine($"'(ushort)0' was evaluted to '{(ushort)integerZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmSByteMaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToUInt16IsFoldedCorrectly()
        {
            ushort sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((ushort)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(ushort)127' was evaluted to '{(ushort)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToUInt16IsFoldedCorrectly()
        {
            ushort integerOneDecrementUnderSByteMaxValue = 126;

            if (BreakUpFlow())
                return;

            if (checked((ushort)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(ushort)126' was evaluted to '{(ushort)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMaxValueCastToUInt16IsFoldedCorrectly()
        {
            ushort integerOneIncrementAboveSByteMaxValue = 128;

            if (BreakUpFlow())
                return;

            if (checked((ushort)integerOneIncrementAboveSByteMaxValue) != 128)
            {
                Console.WriteLine($"'(ushort)128' was evaluted to '{(ushort)integerOneIncrementAboveSByteMaxValue}'. Expected: '128'.");
                _counter++;
            }
        }
        ConfirmByteMaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmByteMaxValueCastToUInt16IsFoldedCorrectly()
        {
            ushort byteMaxValue = 255;

            if (BreakUpFlow())
                return;

            if (checked((ushort)byteMaxValue) != 255)
            {
                Console.WriteLine($"'(ushort)255' was evaluted to '{(ushort)byteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToUInt16IsFoldedCorrectly()
        {
            ushort integerOneIncrementAboveByteMinValue = 1;

            if (BreakUpFlow())
                return;

            if (checked((ushort)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(ushort)1' was evaluted to '{(ushort)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMaxValueCastToUInt16IsFoldedCorrectly()
        {
            ushort integerOneDecrementUnderByteMaxValue = 254;

            if (BreakUpFlow())
                return;

            if (checked((ushort)integerOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(ushort)254' was evaluted to '{(ushort)integerOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMaxValueCastToUInt16IsFoldedCorrectly()
        {
            ushort integerOneIncrementAboveByteMaxValue = 256;

            if (BreakUpFlow())
                return;

            if (checked((ushort)integerOneIncrementAboveByteMaxValue) != 256)
            {
                Console.WriteLine($"'(ushort)256' was evaluted to '{(ushort)integerOneIncrementAboveByteMaxValue}'. Expected: '256'.");
                _counter++;
            }
        }
        ConfirmInt16MaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MaxValueCastToUInt16IsFoldedCorrectly()
        {
            ushort int16MaxValue = 32767;

            if (BreakUpFlow())
                return;

            if (checked((ushort)int16MaxValue) != 32767)
            {
                Console.WriteLine($"'(ushort)32767' was evaluted to '{(ushort)int16MaxValue}'. Expected: '32767'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt16MaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MaxValueCastToUInt16IsFoldedCorrectly()
        {
            ushort integerOneDecrementUnderInt16MaxValue = 32766;

            if (BreakUpFlow())
                return;

            if (checked((ushort)integerOneDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(ushort)32766' was evaluted to '{(ushort)integerOneDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt16MaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt16MaxValueCastToUInt16IsFoldedCorrectly()
        {
            ushort integerOneIncrementAboveInt16MaxValue = 32768;

            if (BreakUpFlow())
                return;

            if (checked((ushort)integerOneIncrementAboveInt16MaxValue) != 32768)
            {
                Console.WriteLine($"'(ushort)32768' was evaluted to '{(ushort)integerOneIncrementAboveInt16MaxValue}'. Expected: '32768'.");
                _counter++;
            }
        }
        ConfirmUInt16MaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt16MaxValueCastToUInt16IsFoldedCorrectly()
        {
            ushort uInt16MaxValue = 65535;

            if (BreakUpFlow())
                return;

            if (checked((ushort)uInt16MaxValue) != 65535)
            {
                Console.WriteLine($"'(ushort)65535' was evaluted to '{(ushort)uInt16MaxValue}'. Expected: '65535'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderUInt16MaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt16MaxValueCastToUInt16IsFoldedCorrectly()
        {
            ushort integerOneDecrementUnderUInt16MaxValue = 65534;

            if (BreakUpFlow())
                return;

            if (checked((ushort)integerOneDecrementUnderUInt16MaxValue) != 65534)
            {
                Console.WriteLine($"'(ushort)65534' was evaluted to '{(ushort)integerOneDecrementUnderUInt16MaxValue}'. Expected: '65534'.");
                _counter++;
            }
        }
    }

    private static void TestCastingUInt16ToInt32()
    {
        ConfirmIntegerZeroCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToInt32IsFoldedCorrectly()
        {
            ushort integerZero = 0;

            if (BreakUpFlow())
                return;

            if (checked((int)integerZero) != 0)
            {
                Console.WriteLine($"'(int)0' was evaluted to '{(int)integerZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmSByteMaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToInt32IsFoldedCorrectly()
        {
            ushort sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((int)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(int)127' was evaluted to '{(int)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToInt32IsFoldedCorrectly()
        {
            ushort integerOneDecrementUnderSByteMaxValue = 126;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(int)126' was evaluted to '{(int)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMaxValueCastToInt32IsFoldedCorrectly()
        {
            ushort integerOneIncrementAboveSByteMaxValue = 128;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneIncrementAboveSByteMaxValue) != 128)
            {
                Console.WriteLine($"'(int)128' was evaluted to '{(int)integerOneIncrementAboveSByteMaxValue}'. Expected: '128'.");
                _counter++;
            }
        }
        ConfirmByteMaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmByteMaxValueCastToInt32IsFoldedCorrectly()
        {
            ushort byteMaxValue = 255;

            if (BreakUpFlow())
                return;

            if (checked((int)byteMaxValue) != 255)
            {
                Console.WriteLine($"'(int)255' was evaluted to '{(int)byteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToInt32IsFoldedCorrectly()
        {
            ushort integerOneIncrementAboveByteMinValue = 1;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(int)1' was evaluted to '{(int)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMaxValueCastToInt32IsFoldedCorrectly()
        {
            ushort integerOneDecrementUnderByteMaxValue = 254;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(int)254' was evaluted to '{(int)integerOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMaxValueCastToInt32IsFoldedCorrectly()
        {
            ushort integerOneIncrementAboveByteMaxValue = 256;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneIncrementAboveByteMaxValue) != 256)
            {
                Console.WriteLine($"'(int)256' was evaluted to '{(int)integerOneIncrementAboveByteMaxValue}'. Expected: '256'.");
                _counter++;
            }
        }
        ConfirmInt16MaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MaxValueCastToInt32IsFoldedCorrectly()
        {
            ushort int16MaxValue = 32767;

            if (BreakUpFlow())
                return;

            if (checked((int)int16MaxValue) != 32767)
            {
                Console.WriteLine($"'(int)32767' was evaluted to '{(int)int16MaxValue}'. Expected: '32767'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt16MaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MaxValueCastToInt32IsFoldedCorrectly()
        {
            ushort integerOneDecrementUnderInt16MaxValue = 32766;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(int)32766' was evaluted to '{(int)integerOneDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt16MaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt16MaxValueCastToInt32IsFoldedCorrectly()
        {
            ushort integerOneIncrementAboveInt16MaxValue = 32768;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneIncrementAboveInt16MaxValue) != 32768)
            {
                Console.WriteLine($"'(int)32768' was evaluted to '{(int)integerOneIncrementAboveInt16MaxValue}'. Expected: '32768'.");
                _counter++;
            }
        }
        ConfirmUInt16MaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt16MaxValueCastToInt32IsFoldedCorrectly()
        {
            ushort uInt16MaxValue = 65535;

            if (BreakUpFlow())
                return;

            if (checked((int)uInt16MaxValue) != 65535)
            {
                Console.WriteLine($"'(int)65535' was evaluted to '{(int)uInt16MaxValue}'. Expected: '65535'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderUInt16MaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt16MaxValueCastToInt32IsFoldedCorrectly()
        {
            ushort integerOneDecrementUnderUInt16MaxValue = 65534;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneDecrementUnderUInt16MaxValue) != 65534)
            {
                Console.WriteLine($"'(int)65534' was evaluted to '{(int)integerOneDecrementUnderUInt16MaxValue}'. Expected: '65534'.");
                _counter++;
            }
        }
    }

    private static void TestCastingUInt16ToUInt32()
    {
        ConfirmIntegerZeroCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToUInt32IsFoldedCorrectly()
        {
            ushort integerZero = 0;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerZero) != 0)
            {
                Console.WriteLine($"'(uint)0' was evaluted to '{(uint)integerZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmSByteMaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToUInt32IsFoldedCorrectly()
        {
            ushort sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((uint)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(uint)127' was evaluted to '{(uint)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToUInt32IsFoldedCorrectly()
        {
            ushort integerOneDecrementUnderSByteMaxValue = 126;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(uint)126' was evaluted to '{(uint)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMaxValueCastToUInt32IsFoldedCorrectly()
        {
            ushort integerOneIncrementAboveSByteMaxValue = 128;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneIncrementAboveSByteMaxValue) != 128)
            {
                Console.WriteLine($"'(uint)128' was evaluted to '{(uint)integerOneIncrementAboveSByteMaxValue}'. Expected: '128'.");
                _counter++;
            }
        }
        ConfirmByteMaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmByteMaxValueCastToUInt32IsFoldedCorrectly()
        {
            ushort byteMaxValue = 255;

            if (BreakUpFlow())
                return;

            if (checked((uint)byteMaxValue) != 255)
            {
                Console.WriteLine($"'(uint)255' was evaluted to '{(uint)byteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToUInt32IsFoldedCorrectly()
        {
            ushort integerOneIncrementAboveByteMinValue = 1;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(uint)1' was evaluted to '{(uint)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMaxValueCastToUInt32IsFoldedCorrectly()
        {
            ushort integerOneDecrementUnderByteMaxValue = 254;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(uint)254' was evaluted to '{(uint)integerOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMaxValueCastToUInt32IsFoldedCorrectly()
        {
            ushort integerOneIncrementAboveByteMaxValue = 256;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneIncrementAboveByteMaxValue) != 256)
            {
                Console.WriteLine($"'(uint)256' was evaluted to '{(uint)integerOneIncrementAboveByteMaxValue}'. Expected: '256'.");
                _counter++;
            }
        }
        ConfirmInt16MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MaxValueCastToUInt32IsFoldedCorrectly()
        {
            ushort int16MaxValue = 32767;

            if (BreakUpFlow())
                return;

            if (checked((uint)int16MaxValue) != 32767)
            {
                Console.WriteLine($"'(uint)32767' was evaluted to '{(uint)int16MaxValue}'. Expected: '32767'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt16MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MaxValueCastToUInt32IsFoldedCorrectly()
        {
            ushort integerOneDecrementUnderInt16MaxValue = 32766;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(uint)32766' was evaluted to '{(uint)integerOneDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt16MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt16MaxValueCastToUInt32IsFoldedCorrectly()
        {
            ushort integerOneIncrementAboveInt16MaxValue = 32768;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneIncrementAboveInt16MaxValue) != 32768)
            {
                Console.WriteLine($"'(uint)32768' was evaluted to '{(uint)integerOneIncrementAboveInt16MaxValue}'. Expected: '32768'.");
                _counter++;
            }
        }
        ConfirmUInt16MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt16MaxValueCastToUInt32IsFoldedCorrectly()
        {
            ushort uInt16MaxValue = 65535;

            if (BreakUpFlow())
                return;

            if (checked((uint)uInt16MaxValue) != 65535)
            {
                Console.WriteLine($"'(uint)65535' was evaluted to '{(uint)uInt16MaxValue}'. Expected: '65535'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderUInt16MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt16MaxValueCastToUInt32IsFoldedCorrectly()
        {
            ushort integerOneDecrementUnderUInt16MaxValue = 65534;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneDecrementUnderUInt16MaxValue) != 65534)
            {
                Console.WriteLine($"'(uint)65534' was evaluted to '{(uint)integerOneDecrementUnderUInt16MaxValue}'. Expected: '65534'.");
                _counter++;
            }
        }
    }

    private static void TestCastingUInt16ToInt64()
    {
        ConfirmIntegerZeroCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToInt64IsFoldedCorrectly()
        {
            ushort integerZero = 0;

            if (BreakUpFlow())
                return;

            if (checked((long)integerZero) != 0)
            {
                Console.WriteLine($"'(long)0' was evaluted to '{(long)integerZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmSByteMaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToInt64IsFoldedCorrectly()
        {
            ushort sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((long)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(long)127' was evaluted to '{(long)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToInt64IsFoldedCorrectly()
        {
            ushort integerOneDecrementUnderSByteMaxValue = 126;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(long)126' was evaluted to '{(long)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMaxValueCastToInt64IsFoldedCorrectly()
        {
            ushort integerOneIncrementAboveSByteMaxValue = 128;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneIncrementAboveSByteMaxValue) != 128)
            {
                Console.WriteLine($"'(long)128' was evaluted to '{(long)integerOneIncrementAboveSByteMaxValue}'. Expected: '128'.");
                _counter++;
            }
        }
        ConfirmByteMaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmByteMaxValueCastToInt64IsFoldedCorrectly()
        {
            ushort byteMaxValue = 255;

            if (BreakUpFlow())
                return;

            if (checked((long)byteMaxValue) != 255)
            {
                Console.WriteLine($"'(long)255' was evaluted to '{(long)byteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToInt64IsFoldedCorrectly()
        {
            ushort integerOneIncrementAboveByteMinValue = 1;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(long)1' was evaluted to '{(long)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMaxValueCastToInt64IsFoldedCorrectly()
        {
            ushort integerOneDecrementUnderByteMaxValue = 254;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(long)254' was evaluted to '{(long)integerOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMaxValueCastToInt64IsFoldedCorrectly()
        {
            ushort integerOneIncrementAboveByteMaxValue = 256;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneIncrementAboveByteMaxValue) != 256)
            {
                Console.WriteLine($"'(long)256' was evaluted to '{(long)integerOneIncrementAboveByteMaxValue}'. Expected: '256'.");
                _counter++;
            }
        }
        ConfirmInt16MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MaxValueCastToInt64IsFoldedCorrectly()
        {
            ushort int16MaxValue = 32767;

            if (BreakUpFlow())
                return;

            if (checked((long)int16MaxValue) != 32767)
            {
                Console.WriteLine($"'(long)32767' was evaluted to '{(long)int16MaxValue}'. Expected: '32767'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt16MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MaxValueCastToInt64IsFoldedCorrectly()
        {
            ushort integerOneDecrementUnderInt16MaxValue = 32766;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(long)32766' was evaluted to '{(long)integerOneDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt16MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt16MaxValueCastToInt64IsFoldedCorrectly()
        {
            ushort integerOneIncrementAboveInt16MaxValue = 32768;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneIncrementAboveInt16MaxValue) != 32768)
            {
                Console.WriteLine($"'(long)32768' was evaluted to '{(long)integerOneIncrementAboveInt16MaxValue}'. Expected: '32768'.");
                _counter++;
            }
        }
        ConfirmUInt16MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt16MaxValueCastToInt64IsFoldedCorrectly()
        {
            ushort uInt16MaxValue = 65535;

            if (BreakUpFlow())
                return;

            if (checked((long)uInt16MaxValue) != 65535)
            {
                Console.WriteLine($"'(long)65535' was evaluted to '{(long)uInt16MaxValue}'. Expected: '65535'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderUInt16MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt16MaxValueCastToInt64IsFoldedCorrectly()
        {
            ushort integerOneDecrementUnderUInt16MaxValue = 65534;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneDecrementUnderUInt16MaxValue) != 65534)
            {
                Console.WriteLine($"'(long)65534' was evaluted to '{(long)integerOneDecrementUnderUInt16MaxValue}'. Expected: '65534'.");
                _counter++;
            }
        }
    }

    private static void TestCastingUInt16ToUInt64()
    {
        ConfirmIntegerZeroCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToUInt64IsFoldedCorrectly()
        {
            ushort integerZero = 0;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerZero) != 0)
            {
                Console.WriteLine($"'(ulong)0' was evaluted to '{(ulong)integerZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmSByteMaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToUInt64IsFoldedCorrectly()
        {
            ushort sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((ulong)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(ulong)127' was evaluted to '{(ulong)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToUInt64IsFoldedCorrectly()
        {
            ushort integerOneDecrementUnderSByteMaxValue = 126;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(ulong)126' was evaluted to '{(ulong)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMaxValueCastToUInt64IsFoldedCorrectly()
        {
            ushort integerOneIncrementAboveSByteMaxValue = 128;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneIncrementAboveSByteMaxValue) != 128)
            {
                Console.WriteLine($"'(ulong)128' was evaluted to '{(ulong)integerOneIncrementAboveSByteMaxValue}'. Expected: '128'.");
                _counter++;
            }
        }
        ConfirmByteMaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmByteMaxValueCastToUInt64IsFoldedCorrectly()
        {
            ushort byteMaxValue = 255;

            if (BreakUpFlow())
                return;

            if (checked((ulong)byteMaxValue) != 255)
            {
                Console.WriteLine($"'(ulong)255' was evaluted to '{(ulong)byteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToUInt64IsFoldedCorrectly()
        {
            ushort integerOneIncrementAboveByteMinValue = 1;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(ulong)1' was evaluted to '{(ulong)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMaxValueCastToUInt64IsFoldedCorrectly()
        {
            ushort integerOneDecrementUnderByteMaxValue = 254;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(ulong)254' was evaluted to '{(ulong)integerOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMaxValueCastToUInt64IsFoldedCorrectly()
        {
            ushort integerOneIncrementAboveByteMaxValue = 256;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneIncrementAboveByteMaxValue) != 256)
            {
                Console.WriteLine($"'(ulong)256' was evaluted to '{(ulong)integerOneIncrementAboveByteMaxValue}'. Expected: '256'.");
                _counter++;
            }
        }
        ConfirmInt16MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MaxValueCastToUInt64IsFoldedCorrectly()
        {
            ushort int16MaxValue = 32767;

            if (BreakUpFlow())
                return;

            if (checked((ulong)int16MaxValue) != 32767)
            {
                Console.WriteLine($"'(ulong)32767' was evaluted to '{(ulong)int16MaxValue}'. Expected: '32767'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt16MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MaxValueCastToUInt64IsFoldedCorrectly()
        {
            ushort integerOneDecrementUnderInt16MaxValue = 32766;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(ulong)32766' was evaluted to '{(ulong)integerOneDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt16MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt16MaxValueCastToUInt64IsFoldedCorrectly()
        {
            ushort integerOneIncrementAboveInt16MaxValue = 32768;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneIncrementAboveInt16MaxValue) != 32768)
            {
                Console.WriteLine($"'(ulong)32768' was evaluted to '{(ulong)integerOneIncrementAboveInt16MaxValue}'. Expected: '32768'.");
                _counter++;
            }
        }
        ConfirmUInt16MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt16MaxValueCastToUInt64IsFoldedCorrectly()
        {
            ushort uInt16MaxValue = 65535;

            if (BreakUpFlow())
                return;

            if (checked((ulong)uInt16MaxValue) != 65535)
            {
                Console.WriteLine($"'(ulong)65535' was evaluted to '{(ulong)uInt16MaxValue}'. Expected: '65535'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderUInt16MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt16MaxValueCastToUInt64IsFoldedCorrectly()
        {
            ushort integerOneDecrementUnderUInt16MaxValue = 65534;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneDecrementUnderUInt16MaxValue) != 65534)
            {
                Console.WriteLine($"'(ulong)65534' was evaluted to '{(ulong)integerOneDecrementUnderUInt16MaxValue}'. Expected: '65534'.");
                _counter++;
            }
        }
    }
}
