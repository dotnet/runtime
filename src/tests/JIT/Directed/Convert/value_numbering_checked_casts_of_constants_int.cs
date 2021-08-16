// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

public partial class ValueNumberingCheckedCastsOfConstants
{
    private static void TestCastingInt32ToSingle()
    {
        ConfirmIntegerZeroCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToSingleIsFoldedCorrectly()
        {
            int integerZero = 0;

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
            int sByteMinValue = -128;

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
            int sByteMaxValue = 127;

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
            int integerOneDecrementUnderSByteMinValue = -129;

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
            int integerOneIncrementAboveSByteMinValue = -127;

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
            int integerOneDecrementUnderSByteMaxValue = 126;

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
            int integerOneIncrementAboveSByteMaxValue = 128;

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
            int byteMaxValue = 255;

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
            int integerOneDecrementUnderByteMinValue = -1;

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
            int integerOneIncrementAboveByteMinValue = 1;

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
            int integerOneDecrementUnderByteMaxValue = 254;

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
            int integerOneIncrementAboveByteMaxValue = 256;

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
            int int16MinValue = -32768;

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
            int int16MaxValue = 32767;

            if (BreakUpFlow())
                return;

            if (checked((float)int16MaxValue) != 32767f)
            {
                Console.WriteLine($"'(float)32767' was evaluted to '{(float)int16MaxValue}'. Expected: '32767f'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt16MinValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MinValueCastToSingleIsFoldedCorrectly()
        {
            int integerOneDecrementUnderInt16MinValue = -32769;

            if (BreakUpFlow())
                return;

            if (checked((float)integerOneDecrementUnderInt16MinValue) != -32769f)
            {
                Console.WriteLine($"'(float)-32769' was evaluted to '{(float)integerOneDecrementUnderInt16MinValue}'. Expected: '-32769f'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt16MinValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt16MinValueCastToSingleIsFoldedCorrectly()
        {
            int integerOneIncrementAboveInt16MinValue = -32767;

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
            int integerOneDecrementUnderInt16MaxValue = 32766;

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
            int integerOneIncrementAboveInt16MaxValue = 32768;

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
            int uInt16MaxValue = 65535;

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
            int integerOneDecrementUnderUInt16MaxValue = 65534;

            if (BreakUpFlow())
                return;

            if (checked((float)integerOneDecrementUnderUInt16MaxValue) != 65534f)
            {
                Console.WriteLine($"'(float)65534' was evaluted to '{(float)integerOneDecrementUnderUInt16MaxValue}'. Expected: '65534f'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToSingleIsFoldedCorrectly()
        {
            int integerOneIncrementAboveUInt16MaxValue = 65536;

            if (BreakUpFlow())
                return;

            if (checked((float)integerOneIncrementAboveUInt16MaxValue) != 65536f)
            {
                Console.WriteLine($"'(float)65536' was evaluted to '{(float)integerOneIncrementAboveUInt16MaxValue}'. Expected: '65536f'.");
                _counter++;
            }
        }
        ConfirmInt32MinValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MinValueCastToSingleIsFoldedCorrectly()
        {
            int int32MinValue = -2147483648;

            if (BreakUpFlow())
                return;

            if (checked((float)int32MinValue) != -2.1474836E+09f)
            {
                Console.WriteLine($"'(float)-2147483648' was evaluted to '{(float)int32MinValue}'. Expected: '-2.1474836E+09f'.");
                _counter++;
            }
        }
        ConfirmInt32MaxValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MaxValueCastToSingleIsFoldedCorrectly()
        {
            int int32MaxValue = 2147483647;

            if (BreakUpFlow())
                return;

            if (checked((float)int32MaxValue) != 2.1474836E+09f)
            {
                Console.WriteLine($"'(float)2147483647' was evaluted to '{(float)int32MaxValue}'. Expected: '2.1474836E+09f'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt32MinValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt32MinValueCastToSingleIsFoldedCorrectly()
        {
            int integerOneIncrementAboveInt32MinValue = -2147483647;

            if (BreakUpFlow())
                return;

            if (checked((float)integerOneIncrementAboveInt32MinValue) != -2.1474836E+09f)
            {
                Console.WriteLine($"'(float)-2147483647' was evaluted to '{(float)integerOneIncrementAboveInt32MinValue}'. Expected: '-2.1474836E+09f'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt32MaxValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt32MaxValueCastToSingleIsFoldedCorrectly()
        {
            int integerOneDecrementUnderInt32MaxValue = 2147483646;

            if (BreakUpFlow())
                return;

            if (checked((float)integerOneDecrementUnderInt32MaxValue) != 2.1474836E+09f)
            {
                Console.WriteLine($"'(float)2147483646' was evaluted to '{(float)integerOneDecrementUnderInt32MaxValue}'. Expected: '2.1474836E+09f'.");
                _counter++;
            }
        }
    }

    private static void TestCastingInt32ToDouble()
    {
        ConfirmIntegerZeroCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToDoubleIsFoldedCorrectly()
        {
            int integerZero = 0;

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
            int sByteMinValue = -128;

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
            int sByteMaxValue = 127;

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
            int integerOneDecrementUnderSByteMinValue = -129;

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
            int integerOneIncrementAboveSByteMinValue = -127;

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
            int integerOneDecrementUnderSByteMaxValue = 126;

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
            int integerOneIncrementAboveSByteMaxValue = 128;

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
            int byteMaxValue = 255;

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
            int integerOneDecrementUnderByteMinValue = -1;

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
            int integerOneIncrementAboveByteMinValue = 1;

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
            int integerOneDecrementUnderByteMaxValue = 254;

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
            int integerOneIncrementAboveByteMaxValue = 256;

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
            int int16MinValue = -32768;

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
            int int16MaxValue = 32767;

            if (BreakUpFlow())
                return;

            if (checked((double)int16MaxValue) != 32767d)
            {
                Console.WriteLine($"'(double)32767' was evaluted to '{(double)int16MaxValue}'. Expected: '32767d'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt16MinValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MinValueCastToDoubleIsFoldedCorrectly()
        {
            int integerOneDecrementUnderInt16MinValue = -32769;

            if (BreakUpFlow())
                return;

            if (checked((double)integerOneDecrementUnderInt16MinValue) != -32769d)
            {
                Console.WriteLine($"'(double)-32769' was evaluted to '{(double)integerOneDecrementUnderInt16MinValue}'. Expected: '-32769d'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt16MinValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt16MinValueCastToDoubleIsFoldedCorrectly()
        {
            int integerOneIncrementAboveInt16MinValue = -32767;

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
            int integerOneDecrementUnderInt16MaxValue = 32766;

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
            int integerOneIncrementAboveInt16MaxValue = 32768;

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
            int uInt16MaxValue = 65535;

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
            int integerOneDecrementUnderUInt16MaxValue = 65534;

            if (BreakUpFlow())
                return;

            if (checked((double)integerOneDecrementUnderUInt16MaxValue) != 65534d)
            {
                Console.WriteLine($"'(double)65534' was evaluted to '{(double)integerOneDecrementUnderUInt16MaxValue}'. Expected: '65534d'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToDoubleIsFoldedCorrectly()
        {
            int integerOneIncrementAboveUInt16MaxValue = 65536;

            if (BreakUpFlow())
                return;

            if (checked((double)integerOneIncrementAboveUInt16MaxValue) != 65536d)
            {
                Console.WriteLine($"'(double)65536' was evaluted to '{(double)integerOneIncrementAboveUInt16MaxValue}'. Expected: '65536d'.");
                _counter++;
            }
        }
        ConfirmInt32MinValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MinValueCastToDoubleIsFoldedCorrectly()
        {
            int int32MinValue = -2147483648;

            if (BreakUpFlow())
                return;

            if (checked((double)int32MinValue) != -2147483648d)
            {
                Console.WriteLine($"'(double)-2147483648' was evaluted to '{(double)int32MinValue}'. Expected: '-2147483648d'.");
                _counter++;
            }
        }
        ConfirmInt32MaxValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MaxValueCastToDoubleIsFoldedCorrectly()
        {
            int int32MaxValue = 2147483647;

            if (BreakUpFlow())
                return;

            if (checked((double)int32MaxValue) != 2147483647d)
            {
                Console.WriteLine($"'(double)2147483647' was evaluted to '{(double)int32MaxValue}'. Expected: '2147483647d'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt32MinValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt32MinValueCastToDoubleIsFoldedCorrectly()
        {
            int integerOneIncrementAboveInt32MinValue = -2147483647;

            if (BreakUpFlow())
                return;

            if (checked((double)integerOneIncrementAboveInt32MinValue) != -2147483647d)
            {
                Console.WriteLine($"'(double)-2147483647' was evaluted to '{(double)integerOneIncrementAboveInt32MinValue}'. Expected: '-2147483647d'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt32MaxValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt32MaxValueCastToDoubleIsFoldedCorrectly()
        {
            int integerOneDecrementUnderInt32MaxValue = 2147483646;

            if (BreakUpFlow())
                return;

            if (checked((double)integerOneDecrementUnderInt32MaxValue) != 2147483646d)
            {
                Console.WriteLine($"'(double)2147483646' was evaluted to '{(double)integerOneDecrementUnderInt32MaxValue}'. Expected: '2147483646d'.");
                _counter++;
            }
        }
    }

    private static void TestCastingInt32ToSByte()
    {
        ConfirmIntegerZeroCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToSByteIsFoldedCorrectly()
        {
            int integerZero = 0;

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
            int sByteMinValue = -128;

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
            int sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(sbyte)127' was evaluted to '{(sbyte)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmInt32OneDecrementUnderSByteMinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneDecrementUnderSByteMinValueCastToSByteOverflows()
        {
            int from = -129;
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
            int integerOneIncrementAboveSByteMinValue = -127;

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
            int integerOneDecrementUnderSByteMaxValue = 126;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(sbyte)126' was evaluted to '{(sbyte)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmInt32OneIncrementAboveSByteMaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneIncrementAboveSByteMaxValueCastToSByteOverflows()
        {
            int from = 128;
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
            int from = 255;
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
            int integerOneDecrementUnderByteMinValue = -1;

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
            int integerOneIncrementAboveByteMinValue = 1;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(sbyte)1' was evaluted to '{(sbyte)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmInt32OneDecrementUnderByteMaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneDecrementUnderByteMaxValueCastToSByteOverflows()
        {
            int from = 254;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)254)' did not throw OverflowException.");
        }
        ConfirmInt32OneIncrementAboveByteMaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneIncrementAboveByteMaxValueCastToSByteOverflows()
        {
            int from = 256;
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
            int from = -32768;
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
            int from = 32767;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)32767)' did not throw OverflowException.");
        }
        ConfirmInt32OneDecrementUnderInt16MinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneDecrementUnderInt16MinValueCastToSByteOverflows()
        {
            int from = -32769;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-32769)' did not throw OverflowException.");
        }
        ConfirmInt32OneIncrementAboveInt16MinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneIncrementAboveInt16MinValueCastToSByteOverflows()
        {
            int from = -32767;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-32767)' did not throw OverflowException.");
        }
        ConfirmInt32OneDecrementUnderInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneDecrementUnderInt16MaxValueCastToSByteOverflows()
        {
            int from = 32766;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)32766)' did not throw OverflowException.");
        }
        ConfirmInt32OneIncrementAboveInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneIncrementAboveInt16MaxValueCastToSByteOverflows()
        {
            int from = 32768;
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
            int from = 65535;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)65535)' did not throw OverflowException.");
        }
        ConfirmInt32OneDecrementUnderUInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneDecrementUnderUInt16MaxValueCastToSByteOverflows()
        {
            int from = 65534;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)65534)' did not throw OverflowException.");
        }
        ConfirmInt32OneIncrementAboveUInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneIncrementAboveUInt16MaxValueCastToSByteOverflows()
        {
            int from = 65536;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)65536)' did not throw OverflowException.");
        }
        ConfirmInt32MinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MinValueCastToSByteOverflows()
        {
            int from = -2147483648;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-2147483648)' did not throw OverflowException.");
        }
        ConfirmInt32MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MaxValueCastToSByteOverflows()
        {
            int from = 2147483647;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)2147483647)' did not throw OverflowException.");
        }
        ConfirmInt32OneIncrementAboveInt32MinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneIncrementAboveInt32MinValueCastToSByteOverflows()
        {
            int from = -2147483647;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-2147483647)' did not throw OverflowException.");
        }
        ConfirmInt32OneDecrementUnderInt32MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneDecrementUnderInt32MaxValueCastToSByteOverflows()
        {
            int from = 2147483646;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)2147483646)' did not throw OverflowException.");
        }
    }

    private static void TestCastingInt32ToByte()
    {
        ConfirmIntegerZeroCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToByteIsFoldedCorrectly()
        {
            int integerZero = 0;

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
            int from = -128;
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
            int sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((byte)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(byte)127' was evaluted to '{(byte)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmInt32OneDecrementUnderSByteMinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneDecrementUnderSByteMinValueCastToByteOverflows()
        {
            int from = -129;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-129)' did not throw OverflowException.");
        }
        ConfirmInt32OneIncrementAboveSByteMinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneIncrementAboveSByteMinValueCastToByteOverflows()
        {
            int from = -127;
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
            int integerOneDecrementUnderSByteMaxValue = 126;

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
            int integerOneIncrementAboveSByteMaxValue = 128;

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
            int byteMaxValue = 255;

            if (BreakUpFlow())
                return;

            if (checked((byte)byteMaxValue) != 255)
            {
                Console.WriteLine($"'(byte)255' was evaluted to '{(byte)byteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmInt32OneDecrementUnderByteMinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneDecrementUnderByteMinValueCastToByteOverflows()
        {
            int from = -1;
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
            int integerOneIncrementAboveByteMinValue = 1;

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
            int integerOneDecrementUnderByteMaxValue = 254;

            if (BreakUpFlow())
                return;

            if (checked((byte)integerOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(byte)254' was evaluted to '{(byte)integerOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmInt32OneIncrementAboveByteMaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneIncrementAboveByteMaxValueCastToByteOverflows()
        {
            int from = 256;
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
            int from = -32768;
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
            int from = 32767;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)32767)' did not throw OverflowException.");
        }
        ConfirmInt32OneDecrementUnderInt16MinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneDecrementUnderInt16MinValueCastToByteOverflows()
        {
            int from = -32769;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-32769)' did not throw OverflowException.");
        }
        ConfirmInt32OneIncrementAboveInt16MinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneIncrementAboveInt16MinValueCastToByteOverflows()
        {
            int from = -32767;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-32767)' did not throw OverflowException.");
        }
        ConfirmInt32OneDecrementUnderInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneDecrementUnderInt16MaxValueCastToByteOverflows()
        {
            int from = 32766;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)32766)' did not throw OverflowException.");
        }
        ConfirmInt32OneIncrementAboveInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneIncrementAboveInt16MaxValueCastToByteOverflows()
        {
            int from = 32768;
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
            int from = 65535;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)65535)' did not throw OverflowException.");
        }
        ConfirmInt32OneDecrementUnderUInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneDecrementUnderUInt16MaxValueCastToByteOverflows()
        {
            int from = 65534;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)65534)' did not throw OverflowException.");
        }
        ConfirmInt32OneIncrementAboveUInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneIncrementAboveUInt16MaxValueCastToByteOverflows()
        {
            int from = 65536;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)65536)' did not throw OverflowException.");
        }
        ConfirmInt32MinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MinValueCastToByteOverflows()
        {
            int from = -2147483648;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-2147483648)' did not throw OverflowException.");
        }
        ConfirmInt32MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MaxValueCastToByteOverflows()
        {
            int from = 2147483647;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)2147483647)' did not throw OverflowException.");
        }
        ConfirmInt32OneIncrementAboveInt32MinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneIncrementAboveInt32MinValueCastToByteOverflows()
        {
            int from = -2147483647;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-2147483647)' did not throw OverflowException.");
        }
        ConfirmInt32OneDecrementUnderInt32MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneDecrementUnderInt32MaxValueCastToByteOverflows()
        {
            int from = 2147483646;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)2147483646)' did not throw OverflowException.");
        }
    }

    private static void TestCastingInt32ToInt16()
    {
        ConfirmIntegerZeroCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToInt16IsFoldedCorrectly()
        {
            int integerZero = 0;

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
            int sByteMinValue = -128;

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
            int sByteMaxValue = 127;

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
            int integerOneDecrementUnderSByteMinValue = -129;

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
            int integerOneIncrementAboveSByteMinValue = -127;

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
            int integerOneDecrementUnderSByteMaxValue = 126;

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
            int integerOneIncrementAboveSByteMaxValue = 128;

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
            int byteMaxValue = 255;

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
            int integerOneDecrementUnderByteMinValue = -1;

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
            int integerOneIncrementAboveByteMinValue = 1;

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
            int integerOneDecrementUnderByteMaxValue = 254;

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
            int integerOneIncrementAboveByteMaxValue = 256;

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
            int int16MinValue = -32768;

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
            int int16MaxValue = 32767;

            if (BreakUpFlow())
                return;

            if (checked((short)int16MaxValue) != 32767)
            {
                Console.WriteLine($"'(short)32767' was evaluted to '{(short)int16MaxValue}'. Expected: '32767'.");
                _counter++;
            }
        }
        ConfirmInt32OneDecrementUnderInt16MinValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneDecrementUnderInt16MinValueCastToInt16Overflows()
        {
            int from = -32769;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)-32769)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt16MinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt16MinValueCastToInt16IsFoldedCorrectly()
        {
            int integerOneIncrementAboveInt16MinValue = -32767;

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
            int integerOneDecrementUnderInt16MaxValue = 32766;

            if (BreakUpFlow())
                return;

            if (checked((short)integerOneDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(short)32766' was evaluted to '{(short)integerOneDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
        ConfirmInt32OneIncrementAboveInt16MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneIncrementAboveInt16MaxValueCastToInt16Overflows()
        {
            int from = 32768;
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
            int from = 65535;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)65535)' did not throw OverflowException.");
        }
        ConfirmInt32OneDecrementUnderUInt16MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneDecrementUnderUInt16MaxValueCastToInt16Overflows()
        {
            int from = 65534;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)65534)' did not throw OverflowException.");
        }
        ConfirmInt32OneIncrementAboveUInt16MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneIncrementAboveUInt16MaxValueCastToInt16Overflows()
        {
            int from = 65536;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)65536)' did not throw OverflowException.");
        }
        ConfirmInt32MinValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MinValueCastToInt16Overflows()
        {
            int from = -2147483648;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)-2147483648)' did not throw OverflowException.");
        }
        ConfirmInt32MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MaxValueCastToInt16Overflows()
        {
            int from = 2147483647;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)2147483647)' did not throw OverflowException.");
        }
        ConfirmInt32OneIncrementAboveInt32MinValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneIncrementAboveInt32MinValueCastToInt16Overflows()
        {
            int from = -2147483647;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)-2147483647)' did not throw OverflowException.");
        }
        ConfirmInt32OneDecrementUnderInt32MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneDecrementUnderInt32MaxValueCastToInt16Overflows()
        {
            int from = 2147483646;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)2147483646)' did not throw OverflowException.");
        }
    }

    private static void TestCastingInt32ToUInt16()
    {
        ConfirmIntegerZeroCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToUInt16IsFoldedCorrectly()
        {
            int integerZero = 0;

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
            int from = -128;
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
            int sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((ushort)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(ushort)127' was evaluted to '{(ushort)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmInt32OneDecrementUnderSByteMinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneDecrementUnderSByteMinValueCastToUInt16Overflows()
        {
            int from = -129;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-129)' did not throw OverflowException.");
        }
        ConfirmInt32OneIncrementAboveSByteMinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneIncrementAboveSByteMinValueCastToUInt16Overflows()
        {
            int from = -127;
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
            int integerOneDecrementUnderSByteMaxValue = 126;

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
            int integerOneIncrementAboveSByteMaxValue = 128;

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
            int byteMaxValue = 255;

            if (BreakUpFlow())
                return;

            if (checked((ushort)byteMaxValue) != 255)
            {
                Console.WriteLine($"'(ushort)255' was evaluted to '{(ushort)byteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmInt32OneDecrementUnderByteMinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneDecrementUnderByteMinValueCastToUInt16Overflows()
        {
            int from = -1;
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
            int integerOneIncrementAboveByteMinValue = 1;

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
            int integerOneDecrementUnderByteMaxValue = 254;

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
            int integerOneIncrementAboveByteMaxValue = 256;

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
            int from = -32768;
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
            int int16MaxValue = 32767;

            if (BreakUpFlow())
                return;

            if (checked((ushort)int16MaxValue) != 32767)
            {
                Console.WriteLine($"'(ushort)32767' was evaluted to '{(ushort)int16MaxValue}'. Expected: '32767'.");
                _counter++;
            }
        }
        ConfirmInt32OneDecrementUnderInt16MinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneDecrementUnderInt16MinValueCastToUInt16Overflows()
        {
            int from = -32769;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-32769)' did not throw OverflowException.");
        }
        ConfirmInt32OneIncrementAboveInt16MinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneIncrementAboveInt16MinValueCastToUInt16Overflows()
        {
            int from = -32767;
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
            int integerOneDecrementUnderInt16MaxValue = 32766;

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
            int integerOneIncrementAboveInt16MaxValue = 32768;

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
            int uInt16MaxValue = 65535;

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
            int integerOneDecrementUnderUInt16MaxValue = 65534;

            if (BreakUpFlow())
                return;

            if (checked((ushort)integerOneDecrementUnderUInt16MaxValue) != 65534)
            {
                Console.WriteLine($"'(ushort)65534' was evaluted to '{(ushort)integerOneDecrementUnderUInt16MaxValue}'. Expected: '65534'.");
                _counter++;
            }
        }
        ConfirmInt32OneIncrementAboveUInt16MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneIncrementAboveUInt16MaxValueCastToUInt16Overflows()
        {
            int from = 65536;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)65536)' did not throw OverflowException.");
        }
        ConfirmInt32MinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MinValueCastToUInt16Overflows()
        {
            int from = -2147483648;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-2147483648)' did not throw OverflowException.");
        }
        ConfirmInt32MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MaxValueCastToUInt16Overflows()
        {
            int from = 2147483647;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)2147483647)' did not throw OverflowException.");
        }
        ConfirmInt32OneIncrementAboveInt32MinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneIncrementAboveInt32MinValueCastToUInt16Overflows()
        {
            int from = -2147483647;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-2147483647)' did not throw OverflowException.");
        }
        ConfirmInt32OneDecrementUnderInt32MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneDecrementUnderInt32MaxValueCastToUInt16Overflows()
        {
            int from = 2147483646;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)2147483646)' did not throw OverflowException.");
        }
    }

    private static void TestCastingInt32ToInt32()
    {
        ConfirmIntegerZeroCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToInt32IsFoldedCorrectly()
        {
            int integerZero = 0;

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
            int sByteMinValue = -128;

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
            int sByteMaxValue = 127;

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
            int integerOneDecrementUnderSByteMinValue = -129;

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
            int integerOneIncrementAboveSByteMinValue = -127;

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
            int integerOneDecrementUnderSByteMaxValue = 126;

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
            int integerOneIncrementAboveSByteMaxValue = 128;

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
            int byteMaxValue = 255;

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
            int integerOneDecrementUnderByteMinValue = -1;

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
            int integerOneIncrementAboveByteMinValue = 1;

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
            int integerOneDecrementUnderByteMaxValue = 254;

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
            int integerOneIncrementAboveByteMaxValue = 256;

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
            int int16MinValue = -32768;

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
            int int16MaxValue = 32767;

            if (BreakUpFlow())
                return;

            if (checked((int)int16MaxValue) != 32767)
            {
                Console.WriteLine($"'(int)32767' was evaluted to '{(int)int16MaxValue}'. Expected: '32767'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt16MinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MinValueCastToInt32IsFoldedCorrectly()
        {
            int integerOneDecrementUnderInt16MinValue = -32769;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneDecrementUnderInt16MinValue) != -32769)
            {
                Console.WriteLine($"'(int)-32769' was evaluted to '{(int)integerOneDecrementUnderInt16MinValue}'. Expected: '-32769'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt16MinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt16MinValueCastToInt32IsFoldedCorrectly()
        {
            int integerOneIncrementAboveInt16MinValue = -32767;

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
            int integerOneDecrementUnderInt16MaxValue = 32766;

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
            int integerOneIncrementAboveInt16MaxValue = 32768;

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
            int uInt16MaxValue = 65535;

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
            int integerOneDecrementUnderUInt16MaxValue = 65534;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneDecrementUnderUInt16MaxValue) != 65534)
            {
                Console.WriteLine($"'(int)65534' was evaluted to '{(int)integerOneDecrementUnderUInt16MaxValue}'. Expected: '65534'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToInt32IsFoldedCorrectly()
        {
            int integerOneIncrementAboveUInt16MaxValue = 65536;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneIncrementAboveUInt16MaxValue) != 65536)
            {
                Console.WriteLine($"'(int)65536' was evaluted to '{(int)integerOneIncrementAboveUInt16MaxValue}'. Expected: '65536'.");
                _counter++;
            }
        }
        ConfirmInt32MinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MinValueCastToInt32IsFoldedCorrectly()
        {
            int int32MinValue = -2147483648;

            if (BreakUpFlow())
                return;

            if (checked((int)int32MinValue) != -2147483648)
            {
                Console.WriteLine($"'(int)-2147483648' was evaluted to '{(int)int32MinValue}'. Expected: '-2147483648'.");
                _counter++;
            }
        }
        ConfirmInt32MaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MaxValueCastToInt32IsFoldedCorrectly()
        {
            int int32MaxValue = 2147483647;

            if (BreakUpFlow())
                return;

            if (checked((int)int32MaxValue) != 2147483647)
            {
                Console.WriteLine($"'(int)2147483647' was evaluted to '{(int)int32MaxValue}'. Expected: '2147483647'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt32MinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt32MinValueCastToInt32IsFoldedCorrectly()
        {
            int integerOneIncrementAboveInt32MinValue = -2147483647;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneIncrementAboveInt32MinValue) != -2147483647)
            {
                Console.WriteLine($"'(int)-2147483647' was evaluted to '{(int)integerOneIncrementAboveInt32MinValue}'. Expected: '-2147483647'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt32MaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt32MaxValueCastToInt32IsFoldedCorrectly()
        {
            int integerOneDecrementUnderInt32MaxValue = 2147483646;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneDecrementUnderInt32MaxValue) != 2147483646)
            {
                Console.WriteLine($"'(int)2147483646' was evaluted to '{(int)integerOneDecrementUnderInt32MaxValue}'. Expected: '2147483646'.");
                _counter++;
            }
        }
    }

    private static void TestCastingInt32ToUInt32()
    {
        ConfirmIntegerZeroCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToUInt32IsFoldedCorrectly()
        {
            int integerZero = 0;

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
            int from = -128;
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
            int sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((uint)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(uint)127' was evaluted to '{(uint)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmInt32OneDecrementUnderSByteMinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneDecrementUnderSByteMinValueCastToUInt32Overflows()
        {
            int from = -129;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-129)' did not throw OverflowException.");
        }
        ConfirmInt32OneIncrementAboveSByteMinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneIncrementAboveSByteMinValueCastToUInt32Overflows()
        {
            int from = -127;
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
            int integerOneDecrementUnderSByteMaxValue = 126;

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
            int integerOneIncrementAboveSByteMaxValue = 128;

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
            int byteMaxValue = 255;

            if (BreakUpFlow())
                return;

            if (checked((uint)byteMaxValue) != 255)
            {
                Console.WriteLine($"'(uint)255' was evaluted to '{(uint)byteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmInt32OneDecrementUnderByteMinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneDecrementUnderByteMinValueCastToUInt32Overflows()
        {
            int from = -1;
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
            int integerOneIncrementAboveByteMinValue = 1;

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
            int integerOneDecrementUnderByteMaxValue = 254;

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
            int integerOneIncrementAboveByteMaxValue = 256;

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
            int from = -32768;
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
            int int16MaxValue = 32767;

            if (BreakUpFlow())
                return;

            if (checked((uint)int16MaxValue) != 32767)
            {
                Console.WriteLine($"'(uint)32767' was evaluted to '{(uint)int16MaxValue}'. Expected: '32767'.");
                _counter++;
            }
        }
        ConfirmInt32OneDecrementUnderInt16MinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneDecrementUnderInt16MinValueCastToUInt32Overflows()
        {
            int from = -32769;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-32769)' did not throw OverflowException.");
        }
        ConfirmInt32OneIncrementAboveInt16MinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneIncrementAboveInt16MinValueCastToUInt32Overflows()
        {
            int from = -32767;
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
            int integerOneDecrementUnderInt16MaxValue = 32766;

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
            int integerOneIncrementAboveInt16MaxValue = 32768;

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
            int uInt16MaxValue = 65535;

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
            int integerOneDecrementUnderUInt16MaxValue = 65534;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneDecrementUnderUInt16MaxValue) != 65534)
            {
                Console.WriteLine($"'(uint)65534' was evaluted to '{(uint)integerOneDecrementUnderUInt16MaxValue}'. Expected: '65534'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToUInt32IsFoldedCorrectly()
        {
            int integerOneIncrementAboveUInt16MaxValue = 65536;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneIncrementAboveUInt16MaxValue) != 65536)
            {
                Console.WriteLine($"'(uint)65536' was evaluted to '{(uint)integerOneIncrementAboveUInt16MaxValue}'. Expected: '65536'.");
                _counter++;
            }
        }
        ConfirmInt32MinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MinValueCastToUInt32Overflows()
        {
            int from = -2147483648;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-2147483648)' did not throw OverflowException.");
        }
        ConfirmInt32MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MaxValueCastToUInt32IsFoldedCorrectly()
        {
            int int32MaxValue = 2147483647;

            if (BreakUpFlow())
                return;

            if (checked((uint)int32MaxValue) != 2147483647)
            {
                Console.WriteLine($"'(uint)2147483647' was evaluted to '{(uint)int32MaxValue}'. Expected: '2147483647'.");
                _counter++;
            }
        }
        ConfirmInt32OneIncrementAboveInt32MinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneIncrementAboveInt32MinValueCastToUInt32Overflows()
        {
            int from = -2147483647;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-2147483647)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt32MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt32MaxValueCastToUInt32IsFoldedCorrectly()
        {
            int integerOneDecrementUnderInt32MaxValue = 2147483646;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneDecrementUnderInt32MaxValue) != 2147483646)
            {
                Console.WriteLine($"'(uint)2147483646' was evaluted to '{(uint)integerOneDecrementUnderInt32MaxValue}'. Expected: '2147483646'.");
                _counter++;
            }
        }
    }

    private static void TestCastingInt32ToInt64()
    {
        ConfirmIntegerZeroCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToInt64IsFoldedCorrectly()
        {
            int integerZero = 0;

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
            int sByteMinValue = -128;

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
            int sByteMaxValue = 127;

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
            int integerOneDecrementUnderSByteMinValue = -129;

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
            int integerOneIncrementAboveSByteMinValue = -127;

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
            int integerOneDecrementUnderSByteMaxValue = 126;

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
            int integerOneIncrementAboveSByteMaxValue = 128;

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
            int byteMaxValue = 255;

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
            int integerOneDecrementUnderByteMinValue = -1;

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
            int integerOneIncrementAboveByteMinValue = 1;

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
            int integerOneDecrementUnderByteMaxValue = 254;

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
            int integerOneIncrementAboveByteMaxValue = 256;

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
            int int16MinValue = -32768;

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
            int int16MaxValue = 32767;

            if (BreakUpFlow())
                return;

            if (checked((long)int16MaxValue) != 32767)
            {
                Console.WriteLine($"'(long)32767' was evaluted to '{(long)int16MaxValue}'. Expected: '32767'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt16MinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MinValueCastToInt64IsFoldedCorrectly()
        {
            int integerOneDecrementUnderInt16MinValue = -32769;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneDecrementUnderInt16MinValue) != -32769)
            {
                Console.WriteLine($"'(long)-32769' was evaluted to '{(long)integerOneDecrementUnderInt16MinValue}'. Expected: '-32769'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt16MinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt16MinValueCastToInt64IsFoldedCorrectly()
        {
            int integerOneIncrementAboveInt16MinValue = -32767;

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
            int integerOneDecrementUnderInt16MaxValue = 32766;

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
            int integerOneIncrementAboveInt16MaxValue = 32768;

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
            int uInt16MaxValue = 65535;

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
            int integerOneDecrementUnderUInt16MaxValue = 65534;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneDecrementUnderUInt16MaxValue) != 65534)
            {
                Console.WriteLine($"'(long)65534' was evaluted to '{(long)integerOneDecrementUnderUInt16MaxValue}'. Expected: '65534'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToInt64IsFoldedCorrectly()
        {
            int integerOneIncrementAboveUInt16MaxValue = 65536;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneIncrementAboveUInt16MaxValue) != 65536)
            {
                Console.WriteLine($"'(long)65536' was evaluted to '{(long)integerOneIncrementAboveUInt16MaxValue}'. Expected: '65536'.");
                _counter++;
            }
        }
        ConfirmInt32MinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MinValueCastToInt64IsFoldedCorrectly()
        {
            int int32MinValue = -2147483648;

            if (BreakUpFlow())
                return;

            if (checked((long)int32MinValue) != -2147483648)
            {
                Console.WriteLine($"'(long)-2147483648' was evaluted to '{(long)int32MinValue}'. Expected: '-2147483648'.");
                _counter++;
            }
        }
        ConfirmInt32MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MaxValueCastToInt64IsFoldedCorrectly()
        {
            int int32MaxValue = 2147483647;

            if (BreakUpFlow())
                return;

            if (checked((long)int32MaxValue) != 2147483647)
            {
                Console.WriteLine($"'(long)2147483647' was evaluted to '{(long)int32MaxValue}'. Expected: '2147483647'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt32MinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt32MinValueCastToInt64IsFoldedCorrectly()
        {
            int integerOneIncrementAboveInt32MinValue = -2147483647;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneIncrementAboveInt32MinValue) != -2147483647)
            {
                Console.WriteLine($"'(long)-2147483647' was evaluted to '{(long)integerOneIncrementAboveInt32MinValue}'. Expected: '-2147483647'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt32MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt32MaxValueCastToInt64IsFoldedCorrectly()
        {
            int integerOneDecrementUnderInt32MaxValue = 2147483646;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneDecrementUnderInt32MaxValue) != 2147483646)
            {
                Console.WriteLine($"'(long)2147483646' was evaluted to '{(long)integerOneDecrementUnderInt32MaxValue}'. Expected: '2147483646'.");
                _counter++;
            }
        }
    }

    private static void TestCastingInt32ToUInt64()
    {
        ConfirmIntegerZeroCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToUInt64IsFoldedCorrectly()
        {
            int integerZero = 0;

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
            int from = -128;
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
            int sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((ulong)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(ulong)127' was evaluted to '{(ulong)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmInt32OneDecrementUnderSByteMinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneDecrementUnderSByteMinValueCastToUInt64Overflows()
        {
            int from = -129;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-129)' did not throw OverflowException.");
        }
        ConfirmInt32OneIncrementAboveSByteMinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneIncrementAboveSByteMinValueCastToUInt64Overflows()
        {
            int from = -127;
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
            int integerOneDecrementUnderSByteMaxValue = 126;

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
            int integerOneIncrementAboveSByteMaxValue = 128;

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
            int byteMaxValue = 255;

            if (BreakUpFlow())
                return;

            if (checked((ulong)byteMaxValue) != 255)
            {
                Console.WriteLine($"'(ulong)255' was evaluted to '{(ulong)byteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmInt32OneDecrementUnderByteMinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneDecrementUnderByteMinValueCastToUInt64Overflows()
        {
            int from = -1;
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
            int integerOneIncrementAboveByteMinValue = 1;

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
            int integerOneDecrementUnderByteMaxValue = 254;

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
            int integerOneIncrementAboveByteMaxValue = 256;

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
            int from = -32768;
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
            int int16MaxValue = 32767;

            if (BreakUpFlow())
                return;

            if (checked((ulong)int16MaxValue) != 32767)
            {
                Console.WriteLine($"'(ulong)32767' was evaluted to '{(ulong)int16MaxValue}'. Expected: '32767'.");
                _counter++;
            }
        }
        ConfirmInt32OneDecrementUnderInt16MinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneDecrementUnderInt16MinValueCastToUInt64Overflows()
        {
            int from = -32769;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-32769)' did not throw OverflowException.");
        }
        ConfirmInt32OneIncrementAboveInt16MinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneIncrementAboveInt16MinValueCastToUInt64Overflows()
        {
            int from = -32767;
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
            int integerOneDecrementUnderInt16MaxValue = 32766;

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
            int integerOneIncrementAboveInt16MaxValue = 32768;

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
            int uInt16MaxValue = 65535;

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
            int integerOneDecrementUnderUInt16MaxValue = 65534;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneDecrementUnderUInt16MaxValue) != 65534)
            {
                Console.WriteLine($"'(ulong)65534' was evaluted to '{(ulong)integerOneDecrementUnderUInt16MaxValue}'. Expected: '65534'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToUInt64IsFoldedCorrectly()
        {
            int integerOneIncrementAboveUInt16MaxValue = 65536;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneIncrementAboveUInt16MaxValue) != 65536)
            {
                Console.WriteLine($"'(ulong)65536' was evaluted to '{(ulong)integerOneIncrementAboveUInt16MaxValue}'. Expected: '65536'.");
                _counter++;
            }
        }
        ConfirmInt32MinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MinValueCastToUInt64Overflows()
        {
            int from = -2147483648;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-2147483648)' did not throw OverflowException.");
        }
        ConfirmInt32MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MaxValueCastToUInt64IsFoldedCorrectly()
        {
            int int32MaxValue = 2147483647;

            if (BreakUpFlow())
                return;

            if (checked((ulong)int32MaxValue) != 2147483647)
            {
                Console.WriteLine($"'(ulong)2147483647' was evaluted to '{(ulong)int32MaxValue}'. Expected: '2147483647'.");
                _counter++;
            }
        }
        ConfirmInt32OneIncrementAboveInt32MinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32OneIncrementAboveInt32MinValueCastToUInt64Overflows()
        {
            int from = -2147483647;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-2147483647)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt32MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt32MaxValueCastToUInt64IsFoldedCorrectly()
        {
            int integerOneDecrementUnderInt32MaxValue = 2147483646;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneDecrementUnderInt32MaxValue) != 2147483646)
            {
                Console.WriteLine($"'(ulong)2147483646' was evaluted to '{(ulong)integerOneDecrementUnderInt32MaxValue}'. Expected: '2147483646'.");
                _counter++;
            }
        }
    }

    private static void TestCastingUInt32ToSingle()
    {
        ConfirmIntegerZeroCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToSingleIsFoldedCorrectly()
        {
            uint integerZero = 0;

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
            uint sByteMaxValue = 127;

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
            uint integerOneDecrementUnderSByteMaxValue = 126;

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
            uint integerOneIncrementAboveSByteMaxValue = 128;

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
            uint byteMaxValue = 255;

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
            uint integerOneIncrementAboveByteMinValue = 1;

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
            uint integerOneDecrementUnderByteMaxValue = 254;

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
            uint integerOneIncrementAboveByteMaxValue = 256;

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
            uint int16MaxValue = 32767;

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
            uint integerOneDecrementUnderInt16MaxValue = 32766;

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
            uint integerOneIncrementAboveInt16MaxValue = 32768;

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
            uint uInt16MaxValue = 65535;

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
            uint integerOneDecrementUnderUInt16MaxValue = 65534;

            if (BreakUpFlow())
                return;

            if (checked((float)integerOneDecrementUnderUInt16MaxValue) != 65534f)
            {
                Console.WriteLine($"'(float)65534' was evaluted to '{(float)integerOneDecrementUnderUInt16MaxValue}'. Expected: '65534f'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToSingleIsFoldedCorrectly()
        {
            uint integerOneIncrementAboveUInt16MaxValue = 65536;

            if (BreakUpFlow())
                return;

            if (checked((float)integerOneIncrementAboveUInt16MaxValue) != 65536f)
            {
                Console.WriteLine($"'(float)65536' was evaluted to '{(float)integerOneIncrementAboveUInt16MaxValue}'. Expected: '65536f'.");
                _counter++;
            }
        }
        ConfirmInt32MaxValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MaxValueCastToSingleIsFoldedCorrectly()
        {
            uint int32MaxValue = 2147483647;

            if (BreakUpFlow())
                return;

            if (checked((float)int32MaxValue) != 2.1474836E+09f)
            {
                Console.WriteLine($"'(float)2147483647' was evaluted to '{(float)int32MaxValue}'. Expected: '2.1474836E+09f'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt32MaxValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt32MaxValueCastToSingleIsFoldedCorrectly()
        {
            uint integerOneDecrementUnderInt32MaxValue = 2147483646;

            if (BreakUpFlow())
                return;

            if (checked((float)integerOneDecrementUnderInt32MaxValue) != 2.1474836E+09f)
            {
                Console.WriteLine($"'(float)2147483646' was evaluted to '{(float)integerOneDecrementUnderInt32MaxValue}'. Expected: '2.1474836E+09f'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt32MaxValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt32MaxValueCastToSingleIsFoldedCorrectly()
        {
            uint integerOneIncrementAboveInt32MaxValue = 2147483648;

            if (BreakUpFlow())
                return;

            if (checked((float)integerOneIncrementAboveInt32MaxValue) != 2.1474836E+09f)
            {
                Console.WriteLine($"'(float)2147483648' was evaluted to '{(float)integerOneIncrementAboveInt32MaxValue}'. Expected: '2.1474836E+09f'.");
                _counter++;
            }
        }
        ConfirmUInt32MaxValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32MaxValueCastToSingleIsFoldedCorrectly()
        {
            uint uInt32MaxValue = 4294967295;

            if (BreakUpFlow())
                return;

            if (checked((float)uInt32MaxValue) != 4.2949673E+09f)
            {
                Console.WriteLine($"'(float)4294967295' was evaluted to '{(float)uInt32MaxValue}'. Expected: '4.2949673E+09f'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderUInt32MaxValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt32MaxValueCastToSingleIsFoldedCorrectly()
        {
            uint integerOneDecrementUnderUInt32MaxValue = 4294967294;

            if (BreakUpFlow())
                return;

            if (checked((float)integerOneDecrementUnderUInt32MaxValue) != 4.2949673E+09f)
            {
                Console.WriteLine($"'(float)4294967294' was evaluted to '{(float)integerOneDecrementUnderUInt32MaxValue}'. Expected: '4.2949673E+09f'.");
                _counter++;
            }
        }
    }

    private static void TestCastingUInt32ToDouble()
    {
        ConfirmIntegerZeroCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToDoubleIsFoldedCorrectly()
        {
            uint integerZero = 0;

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
            uint sByteMaxValue = 127;

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
            uint integerOneDecrementUnderSByteMaxValue = 126;

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
            uint integerOneIncrementAboveSByteMaxValue = 128;

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
            uint byteMaxValue = 255;

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
            uint integerOneIncrementAboveByteMinValue = 1;

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
            uint integerOneDecrementUnderByteMaxValue = 254;

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
            uint integerOneIncrementAboveByteMaxValue = 256;

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
            uint int16MaxValue = 32767;

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
            uint integerOneDecrementUnderInt16MaxValue = 32766;

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
            uint integerOneIncrementAboveInt16MaxValue = 32768;

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
            uint uInt16MaxValue = 65535;

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
            uint integerOneDecrementUnderUInt16MaxValue = 65534;

            if (BreakUpFlow())
                return;

            if (checked((double)integerOneDecrementUnderUInt16MaxValue) != 65534d)
            {
                Console.WriteLine($"'(double)65534' was evaluted to '{(double)integerOneDecrementUnderUInt16MaxValue}'. Expected: '65534d'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToDoubleIsFoldedCorrectly()
        {
            uint integerOneIncrementAboveUInt16MaxValue = 65536;

            if (BreakUpFlow())
                return;

            if (checked((double)integerOneIncrementAboveUInt16MaxValue) != 65536d)
            {
                Console.WriteLine($"'(double)65536' was evaluted to '{(double)integerOneIncrementAboveUInt16MaxValue}'. Expected: '65536d'.");
                _counter++;
            }
        }
        ConfirmInt32MaxValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MaxValueCastToDoubleIsFoldedCorrectly()
        {
            uint int32MaxValue = 2147483647;

            if (BreakUpFlow())
                return;

            if (checked((double)int32MaxValue) != 2147483647d)
            {
                Console.WriteLine($"'(double)2147483647' was evaluted to '{(double)int32MaxValue}'. Expected: '2147483647d'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt32MaxValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt32MaxValueCastToDoubleIsFoldedCorrectly()
        {
            uint integerOneDecrementUnderInt32MaxValue = 2147483646;

            if (BreakUpFlow())
                return;

            if (checked((double)integerOneDecrementUnderInt32MaxValue) != 2147483646d)
            {
                Console.WriteLine($"'(double)2147483646' was evaluted to '{(double)integerOneDecrementUnderInt32MaxValue}'. Expected: '2147483646d'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt32MaxValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt32MaxValueCastToDoubleIsFoldedCorrectly()
        {
            uint integerOneIncrementAboveInt32MaxValue = 2147483648;

            if (BreakUpFlow())
                return;

            if (checked((double)integerOneIncrementAboveInt32MaxValue) != 2147483648d)
            {
                Console.WriteLine($"'(double)2147483648' was evaluted to '{(double)integerOneIncrementAboveInt32MaxValue}'. Expected: '2147483648d'.");
                _counter++;
            }
        }
        ConfirmUInt32MaxValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32MaxValueCastToDoubleIsFoldedCorrectly()
        {
            uint uInt32MaxValue = 4294967295;

            if (BreakUpFlow())
                return;

            if (checked((double)uInt32MaxValue) != 4294967295d)
            {
                Console.WriteLine($"'(double)4294967295' was evaluted to '{(double)uInt32MaxValue}'. Expected: '4294967295d'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderUInt32MaxValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt32MaxValueCastToDoubleIsFoldedCorrectly()
        {
            uint integerOneDecrementUnderUInt32MaxValue = 4294967294;

            if (BreakUpFlow())
                return;

            if (checked((double)integerOneDecrementUnderUInt32MaxValue) != 4294967294d)
            {
                Console.WriteLine($"'(double)4294967294' was evaluted to '{(double)integerOneDecrementUnderUInt32MaxValue}'. Expected: '4294967294d'.");
                _counter++;
            }
        }
    }

    private static void TestCastingUInt32ToSByte()
    {
        ConfirmIntegerZeroCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToSByteIsFoldedCorrectly()
        {
            uint integerZero = 0;

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
            uint sByteMaxValue = 127;

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
            uint integerOneDecrementUnderSByteMaxValue = 126;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(sbyte)126' was evaluted to '{(sbyte)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmUInt32OneIncrementAboveSByteMaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32OneIncrementAboveSByteMaxValueCastToSByteOverflows()
        {
            uint from = 128;
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
            uint from = 255;
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
            uint integerOneIncrementAboveByteMinValue = 1;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(sbyte)1' was evaluted to '{(sbyte)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmUInt32OneDecrementUnderByteMaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32OneDecrementUnderByteMaxValueCastToSByteOverflows()
        {
            uint from = 254;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)254)' did not throw OverflowException.");
        }
        ConfirmUInt32OneIncrementAboveByteMaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32OneIncrementAboveByteMaxValueCastToSByteOverflows()
        {
            uint from = 256;
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
            uint from = 32767;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)32767)' did not throw OverflowException.");
        }
        ConfirmUInt32OneDecrementUnderInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32OneDecrementUnderInt16MaxValueCastToSByteOverflows()
        {
            uint from = 32766;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)32766)' did not throw OverflowException.");
        }
        ConfirmUInt32OneIncrementAboveInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32OneIncrementAboveInt16MaxValueCastToSByteOverflows()
        {
            uint from = 32768;
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
            uint from = 65535;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)65535)' did not throw OverflowException.");
        }
        ConfirmUInt32OneDecrementUnderUInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32OneDecrementUnderUInt16MaxValueCastToSByteOverflows()
        {
            uint from = 65534;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)65534)' did not throw OverflowException.");
        }
        ConfirmUInt32OneIncrementAboveUInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32OneIncrementAboveUInt16MaxValueCastToSByteOverflows()
        {
            uint from = 65536;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)65536)' did not throw OverflowException.");
        }
        ConfirmInt32MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MaxValueCastToSByteOverflows()
        {
            uint from = 2147483647;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)2147483647)' did not throw OverflowException.");
        }
        ConfirmUInt32OneDecrementUnderInt32MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32OneDecrementUnderInt32MaxValueCastToSByteOverflows()
        {
            uint from = 2147483646;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)2147483646)' did not throw OverflowException.");
        }
        ConfirmUInt32OneIncrementAboveInt32MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32OneIncrementAboveInt32MaxValueCastToSByteOverflows()
        {
            uint from = 2147483648;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)2147483648)' did not throw OverflowException.");
        }
        ConfirmUInt32MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32MaxValueCastToSByteOverflows()
        {
            uint from = 4294967295;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)4294967295)' did not throw OverflowException.");
        }
        ConfirmUInt32OneDecrementUnderUInt32MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32OneDecrementUnderUInt32MaxValueCastToSByteOverflows()
        {
            uint from = 4294967294;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)4294967294)' did not throw OverflowException.");
        }
    }

    private static void TestCastingUInt32ToByte()
    {
        ConfirmIntegerZeroCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToByteIsFoldedCorrectly()
        {
            uint integerZero = 0;

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
            uint sByteMaxValue = 127;

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
            uint integerOneDecrementUnderSByteMaxValue = 126;

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
            uint integerOneIncrementAboveSByteMaxValue = 128;

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
            uint byteMaxValue = 255;

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
            uint integerOneIncrementAboveByteMinValue = 1;

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
            uint integerOneDecrementUnderByteMaxValue = 254;

            if (BreakUpFlow())
                return;

            if (checked((byte)integerOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(byte)254' was evaluted to '{(byte)integerOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmUInt32OneIncrementAboveByteMaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32OneIncrementAboveByteMaxValueCastToByteOverflows()
        {
            uint from = 256;
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
            uint from = 32767;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)32767)' did not throw OverflowException.");
        }
        ConfirmUInt32OneDecrementUnderInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32OneDecrementUnderInt16MaxValueCastToByteOverflows()
        {
            uint from = 32766;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)32766)' did not throw OverflowException.");
        }
        ConfirmUInt32OneIncrementAboveInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32OneIncrementAboveInt16MaxValueCastToByteOverflows()
        {
            uint from = 32768;
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
            uint from = 65535;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)65535)' did not throw OverflowException.");
        }
        ConfirmUInt32OneDecrementUnderUInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32OneDecrementUnderUInt16MaxValueCastToByteOverflows()
        {
            uint from = 65534;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)65534)' did not throw OverflowException.");
        }
        ConfirmUInt32OneIncrementAboveUInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32OneIncrementAboveUInt16MaxValueCastToByteOverflows()
        {
            uint from = 65536;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)65536)' did not throw OverflowException.");
        }
        ConfirmInt32MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MaxValueCastToByteOverflows()
        {
            uint from = 2147483647;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)2147483647)' did not throw OverflowException.");
        }
        ConfirmUInt32OneDecrementUnderInt32MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32OneDecrementUnderInt32MaxValueCastToByteOverflows()
        {
            uint from = 2147483646;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)2147483646)' did not throw OverflowException.");
        }
        ConfirmUInt32OneIncrementAboveInt32MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32OneIncrementAboveInt32MaxValueCastToByteOverflows()
        {
            uint from = 2147483648;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)2147483648)' did not throw OverflowException.");
        }
        ConfirmUInt32MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32MaxValueCastToByteOverflows()
        {
            uint from = 4294967295;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)4294967295)' did not throw OverflowException.");
        }
        ConfirmUInt32OneDecrementUnderUInt32MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32OneDecrementUnderUInt32MaxValueCastToByteOverflows()
        {
            uint from = 4294967294;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)4294967294)' did not throw OverflowException.");
        }
    }

    private static void TestCastingUInt32ToInt16()
    {
        ConfirmIntegerZeroCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToInt16IsFoldedCorrectly()
        {
            uint integerZero = 0;

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
            uint sByteMaxValue = 127;

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
            uint integerOneDecrementUnderSByteMaxValue = 126;

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
            uint integerOneIncrementAboveSByteMaxValue = 128;

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
            uint byteMaxValue = 255;

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
            uint integerOneIncrementAboveByteMinValue = 1;

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
            uint integerOneDecrementUnderByteMaxValue = 254;

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
            uint integerOneIncrementAboveByteMaxValue = 256;

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
            uint int16MaxValue = 32767;

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
            uint integerOneDecrementUnderInt16MaxValue = 32766;

            if (BreakUpFlow())
                return;

            if (checked((short)integerOneDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(short)32766' was evaluted to '{(short)integerOneDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
        ConfirmUInt32OneIncrementAboveInt16MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32OneIncrementAboveInt16MaxValueCastToInt16Overflows()
        {
            uint from = 32768;
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
            uint from = 65535;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)65535)' did not throw OverflowException.");
        }
        ConfirmUInt32OneDecrementUnderUInt16MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32OneDecrementUnderUInt16MaxValueCastToInt16Overflows()
        {
            uint from = 65534;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)65534)' did not throw OverflowException.");
        }
        ConfirmUInt32OneIncrementAboveUInt16MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32OneIncrementAboveUInt16MaxValueCastToInt16Overflows()
        {
            uint from = 65536;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)65536)' did not throw OverflowException.");
        }
        ConfirmInt32MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MaxValueCastToInt16Overflows()
        {
            uint from = 2147483647;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)2147483647)' did not throw OverflowException.");
        }
        ConfirmUInt32OneDecrementUnderInt32MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32OneDecrementUnderInt32MaxValueCastToInt16Overflows()
        {
            uint from = 2147483646;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)2147483646)' did not throw OverflowException.");
        }
        ConfirmUInt32OneIncrementAboveInt32MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32OneIncrementAboveInt32MaxValueCastToInt16Overflows()
        {
            uint from = 2147483648;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)2147483648)' did not throw OverflowException.");
        }
        ConfirmUInt32MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32MaxValueCastToInt16Overflows()
        {
            uint from = 4294967295;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)4294967295)' did not throw OverflowException.");
        }
        ConfirmUInt32OneDecrementUnderUInt32MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32OneDecrementUnderUInt32MaxValueCastToInt16Overflows()
        {
            uint from = 4294967294;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)4294967294)' did not throw OverflowException.");
        }
    }

    private static void TestCastingUInt32ToUInt16()
    {
        ConfirmIntegerZeroCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToUInt16IsFoldedCorrectly()
        {
            uint integerZero = 0;

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
            uint sByteMaxValue = 127;

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
            uint integerOneDecrementUnderSByteMaxValue = 126;

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
            uint integerOneIncrementAboveSByteMaxValue = 128;

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
            uint byteMaxValue = 255;

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
            uint integerOneIncrementAboveByteMinValue = 1;

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
            uint integerOneDecrementUnderByteMaxValue = 254;

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
            uint integerOneIncrementAboveByteMaxValue = 256;

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
            uint int16MaxValue = 32767;

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
            uint integerOneDecrementUnderInt16MaxValue = 32766;

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
            uint integerOneIncrementAboveInt16MaxValue = 32768;

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
            uint uInt16MaxValue = 65535;

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
            uint integerOneDecrementUnderUInt16MaxValue = 65534;

            if (BreakUpFlow())
                return;

            if (checked((ushort)integerOneDecrementUnderUInt16MaxValue) != 65534)
            {
                Console.WriteLine($"'(ushort)65534' was evaluted to '{(ushort)integerOneDecrementUnderUInt16MaxValue}'. Expected: '65534'.");
                _counter++;
            }
        }
        ConfirmUInt32OneIncrementAboveUInt16MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32OneIncrementAboveUInt16MaxValueCastToUInt16Overflows()
        {
            uint from = 65536;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)65536)' did not throw OverflowException.");
        }
        ConfirmInt32MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MaxValueCastToUInt16Overflows()
        {
            uint from = 2147483647;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)2147483647)' did not throw OverflowException.");
        }
        ConfirmUInt32OneDecrementUnderInt32MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32OneDecrementUnderInt32MaxValueCastToUInt16Overflows()
        {
            uint from = 2147483646;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)2147483646)' did not throw OverflowException.");
        }
        ConfirmUInt32OneIncrementAboveInt32MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32OneIncrementAboveInt32MaxValueCastToUInt16Overflows()
        {
            uint from = 2147483648;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)2147483648)' did not throw OverflowException.");
        }
        ConfirmUInt32MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32MaxValueCastToUInt16Overflows()
        {
            uint from = 4294967295;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)4294967295)' did not throw OverflowException.");
        }
        ConfirmUInt32OneDecrementUnderUInt32MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32OneDecrementUnderUInt32MaxValueCastToUInt16Overflows()
        {
            uint from = 4294967294;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)4294967294)' did not throw OverflowException.");
        }
    }

    private static void TestCastingUInt32ToInt32()
    {
        ConfirmIntegerZeroCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToInt32IsFoldedCorrectly()
        {
            uint integerZero = 0;

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
            uint sByteMaxValue = 127;

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
            uint integerOneDecrementUnderSByteMaxValue = 126;

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
            uint integerOneIncrementAboveSByteMaxValue = 128;

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
            uint byteMaxValue = 255;

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
            uint integerOneIncrementAboveByteMinValue = 1;

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
            uint integerOneDecrementUnderByteMaxValue = 254;

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
            uint integerOneIncrementAboveByteMaxValue = 256;

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
            uint int16MaxValue = 32767;

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
            uint integerOneDecrementUnderInt16MaxValue = 32766;

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
            uint integerOneIncrementAboveInt16MaxValue = 32768;

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
            uint uInt16MaxValue = 65535;

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
            uint integerOneDecrementUnderUInt16MaxValue = 65534;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneDecrementUnderUInt16MaxValue) != 65534)
            {
                Console.WriteLine($"'(int)65534' was evaluted to '{(int)integerOneDecrementUnderUInt16MaxValue}'. Expected: '65534'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToInt32IsFoldedCorrectly()
        {
            uint integerOneIncrementAboveUInt16MaxValue = 65536;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneIncrementAboveUInt16MaxValue) != 65536)
            {
                Console.WriteLine($"'(int)65536' was evaluted to '{(int)integerOneIncrementAboveUInt16MaxValue}'. Expected: '65536'.");
                _counter++;
            }
        }
        ConfirmInt32MaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MaxValueCastToInt32IsFoldedCorrectly()
        {
            uint int32MaxValue = 2147483647;

            if (BreakUpFlow())
                return;

            if (checked((int)int32MaxValue) != 2147483647)
            {
                Console.WriteLine($"'(int)2147483647' was evaluted to '{(int)int32MaxValue}'. Expected: '2147483647'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt32MaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt32MaxValueCastToInt32IsFoldedCorrectly()
        {
            uint integerOneDecrementUnderInt32MaxValue = 2147483646;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneDecrementUnderInt32MaxValue) != 2147483646)
            {
                Console.WriteLine($"'(int)2147483646' was evaluted to '{(int)integerOneDecrementUnderInt32MaxValue}'. Expected: '2147483646'.");
                _counter++;
            }
        }
        ConfirmUInt32OneIncrementAboveInt32MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32OneIncrementAboveInt32MaxValueCastToInt32Overflows()
        {
            uint from = 2147483648;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)2147483648)' did not throw OverflowException.");
        }
        ConfirmUInt32MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32MaxValueCastToInt32Overflows()
        {
            uint from = 4294967295;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)4294967295)' did not throw OverflowException.");
        }
        ConfirmUInt32OneDecrementUnderUInt32MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32OneDecrementUnderUInt32MaxValueCastToInt32Overflows()
        {
            uint from = 4294967294;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)4294967294)' did not throw OverflowException.");
        }
    }

    private static void TestCastingUInt32ToUInt32()
    {
        ConfirmIntegerZeroCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToUInt32IsFoldedCorrectly()
        {
            uint integerZero = 0;

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
            uint sByteMaxValue = 127;

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
            uint integerOneDecrementUnderSByteMaxValue = 126;

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
            uint integerOneIncrementAboveSByteMaxValue = 128;

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
            uint byteMaxValue = 255;

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
            uint integerOneIncrementAboveByteMinValue = 1;

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
            uint integerOneDecrementUnderByteMaxValue = 254;

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
            uint integerOneIncrementAboveByteMaxValue = 256;

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
            uint int16MaxValue = 32767;

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
            uint integerOneDecrementUnderInt16MaxValue = 32766;

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
            uint integerOneIncrementAboveInt16MaxValue = 32768;

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
            uint uInt16MaxValue = 65535;

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
            uint integerOneDecrementUnderUInt16MaxValue = 65534;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneDecrementUnderUInt16MaxValue) != 65534)
            {
                Console.WriteLine($"'(uint)65534' was evaluted to '{(uint)integerOneDecrementUnderUInt16MaxValue}'. Expected: '65534'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToUInt32IsFoldedCorrectly()
        {
            uint integerOneIncrementAboveUInt16MaxValue = 65536;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneIncrementAboveUInt16MaxValue) != 65536)
            {
                Console.WriteLine($"'(uint)65536' was evaluted to '{(uint)integerOneIncrementAboveUInt16MaxValue}'. Expected: '65536'.");
                _counter++;
            }
        }
        ConfirmInt32MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MaxValueCastToUInt32IsFoldedCorrectly()
        {
            uint int32MaxValue = 2147483647;

            if (BreakUpFlow())
                return;

            if (checked((uint)int32MaxValue) != 2147483647)
            {
                Console.WriteLine($"'(uint)2147483647' was evaluted to '{(uint)int32MaxValue}'. Expected: '2147483647'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt32MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt32MaxValueCastToUInt32IsFoldedCorrectly()
        {
            uint integerOneDecrementUnderInt32MaxValue = 2147483646;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneDecrementUnderInt32MaxValue) != 2147483646)
            {
                Console.WriteLine($"'(uint)2147483646' was evaluted to '{(uint)integerOneDecrementUnderInt32MaxValue}'. Expected: '2147483646'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt32MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt32MaxValueCastToUInt32IsFoldedCorrectly()
        {
            uint integerOneIncrementAboveInt32MaxValue = 2147483648;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneIncrementAboveInt32MaxValue) != 2147483648)
            {
                Console.WriteLine($"'(uint)2147483648' was evaluted to '{(uint)integerOneIncrementAboveInt32MaxValue}'. Expected: '2147483648'.");
                _counter++;
            }
        }
        ConfirmUInt32MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32MaxValueCastToUInt32IsFoldedCorrectly()
        {
            uint uInt32MaxValue = 4294967295;

            if (BreakUpFlow())
                return;

            if (checked((uint)uInt32MaxValue) != 4294967295)
            {
                Console.WriteLine($"'(uint)4294967295' was evaluted to '{(uint)uInt32MaxValue}'. Expected: '4294967295'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderUInt32MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt32MaxValueCastToUInt32IsFoldedCorrectly()
        {
            uint integerOneDecrementUnderUInt32MaxValue = 4294967294;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneDecrementUnderUInt32MaxValue) != 4294967294)
            {
                Console.WriteLine($"'(uint)4294967294' was evaluted to '{(uint)integerOneDecrementUnderUInt32MaxValue}'. Expected: '4294967294'.");
                _counter++;
            }
        }
    }

    private static void TestCastingUInt32ToInt64()
    {
        ConfirmIntegerZeroCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToInt64IsFoldedCorrectly()
        {
            uint integerZero = 0;

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
            uint sByteMaxValue = 127;

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
            uint integerOneDecrementUnderSByteMaxValue = 126;

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
            uint integerOneIncrementAboveSByteMaxValue = 128;

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
            uint byteMaxValue = 255;

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
            uint integerOneIncrementAboveByteMinValue = 1;

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
            uint integerOneDecrementUnderByteMaxValue = 254;

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
            uint integerOneIncrementAboveByteMaxValue = 256;

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
            uint int16MaxValue = 32767;

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
            uint integerOneDecrementUnderInt16MaxValue = 32766;

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
            uint integerOneIncrementAboveInt16MaxValue = 32768;

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
            uint uInt16MaxValue = 65535;

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
            uint integerOneDecrementUnderUInt16MaxValue = 65534;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneDecrementUnderUInt16MaxValue) != 65534)
            {
                Console.WriteLine($"'(long)65534' was evaluted to '{(long)integerOneDecrementUnderUInt16MaxValue}'. Expected: '65534'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToInt64IsFoldedCorrectly()
        {
            uint integerOneIncrementAboveUInt16MaxValue = 65536;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneIncrementAboveUInt16MaxValue) != 65536)
            {
                Console.WriteLine($"'(long)65536' was evaluted to '{(long)integerOneIncrementAboveUInt16MaxValue}'. Expected: '65536'.");
                _counter++;
            }
        }
        ConfirmInt32MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MaxValueCastToInt64IsFoldedCorrectly()
        {
            uint int32MaxValue = 2147483647;

            if (BreakUpFlow())
                return;

            if (checked((long)int32MaxValue) != 2147483647)
            {
                Console.WriteLine($"'(long)2147483647' was evaluted to '{(long)int32MaxValue}'. Expected: '2147483647'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt32MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt32MaxValueCastToInt64IsFoldedCorrectly()
        {
            uint integerOneDecrementUnderInt32MaxValue = 2147483646;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneDecrementUnderInt32MaxValue) != 2147483646)
            {
                Console.WriteLine($"'(long)2147483646' was evaluted to '{(long)integerOneDecrementUnderInt32MaxValue}'. Expected: '2147483646'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt32MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt32MaxValueCastToInt64IsFoldedCorrectly()
        {
            uint integerOneIncrementAboveInt32MaxValue = 2147483648;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneIncrementAboveInt32MaxValue) != 2147483648)
            {
                Console.WriteLine($"'(long)2147483648' was evaluted to '{(long)integerOneIncrementAboveInt32MaxValue}'. Expected: '2147483648'.");
                _counter++;
            }
        }
        ConfirmUInt32MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32MaxValueCastToInt64IsFoldedCorrectly()
        {
            uint uInt32MaxValue = 4294967295;

            if (BreakUpFlow())
                return;

            if (checked((long)uInt32MaxValue) != 4294967295)
            {
                Console.WriteLine($"'(long)4294967295' was evaluted to '{(long)uInt32MaxValue}'. Expected: '4294967295'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderUInt32MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt32MaxValueCastToInt64IsFoldedCorrectly()
        {
            uint integerOneDecrementUnderUInt32MaxValue = 4294967294;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneDecrementUnderUInt32MaxValue) != 4294967294)
            {
                Console.WriteLine($"'(long)4294967294' was evaluted to '{(long)integerOneDecrementUnderUInt32MaxValue}'. Expected: '4294967294'.");
                _counter++;
            }
        }
    }

    private static void TestCastingUInt32ToUInt64()
    {
        ConfirmIntegerZeroCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToUInt64IsFoldedCorrectly()
        {
            uint integerZero = 0;

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
            uint sByteMaxValue = 127;

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
            uint integerOneDecrementUnderSByteMaxValue = 126;

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
            uint integerOneIncrementAboveSByteMaxValue = 128;

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
            uint byteMaxValue = 255;

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
            uint integerOneIncrementAboveByteMinValue = 1;

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
            uint integerOneDecrementUnderByteMaxValue = 254;

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
            uint integerOneIncrementAboveByteMaxValue = 256;

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
            uint int16MaxValue = 32767;

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
            uint integerOneDecrementUnderInt16MaxValue = 32766;

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
            uint integerOneIncrementAboveInt16MaxValue = 32768;

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
            uint uInt16MaxValue = 65535;

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
            uint integerOneDecrementUnderUInt16MaxValue = 65534;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneDecrementUnderUInt16MaxValue) != 65534)
            {
                Console.WriteLine($"'(ulong)65534' was evaluted to '{(ulong)integerOneDecrementUnderUInt16MaxValue}'. Expected: '65534'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToUInt64IsFoldedCorrectly()
        {
            uint integerOneIncrementAboveUInt16MaxValue = 65536;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneIncrementAboveUInt16MaxValue) != 65536)
            {
                Console.WriteLine($"'(ulong)65536' was evaluted to '{(ulong)integerOneIncrementAboveUInt16MaxValue}'. Expected: '65536'.");
                _counter++;
            }
        }
        ConfirmInt32MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MaxValueCastToUInt64IsFoldedCorrectly()
        {
            uint int32MaxValue = 2147483647;

            if (BreakUpFlow())
                return;

            if (checked((ulong)int32MaxValue) != 2147483647)
            {
                Console.WriteLine($"'(ulong)2147483647' was evaluted to '{(ulong)int32MaxValue}'. Expected: '2147483647'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt32MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt32MaxValueCastToUInt64IsFoldedCorrectly()
        {
            uint integerOneDecrementUnderInt32MaxValue = 2147483646;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneDecrementUnderInt32MaxValue) != 2147483646)
            {
                Console.WriteLine($"'(ulong)2147483646' was evaluted to '{(ulong)integerOneDecrementUnderInt32MaxValue}'. Expected: '2147483646'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt32MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt32MaxValueCastToUInt64IsFoldedCorrectly()
        {
            uint integerOneIncrementAboveInt32MaxValue = 2147483648;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneIncrementAboveInt32MaxValue) != 2147483648)
            {
                Console.WriteLine($"'(ulong)2147483648' was evaluted to '{(ulong)integerOneIncrementAboveInt32MaxValue}'. Expected: '2147483648'.");
                _counter++;
            }
        }
        ConfirmUInt32MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32MaxValueCastToUInt64IsFoldedCorrectly()
        {
            uint uInt32MaxValue = 4294967295;

            if (BreakUpFlow())
                return;

            if (checked((ulong)uInt32MaxValue) != 4294967295)
            {
                Console.WriteLine($"'(ulong)4294967295' was evaluted to '{(ulong)uInt32MaxValue}'. Expected: '4294967295'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderUInt32MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt32MaxValueCastToUInt64IsFoldedCorrectly()
        {
            uint integerOneDecrementUnderUInt32MaxValue = 4294967294;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneDecrementUnderUInt32MaxValue) != 4294967294)
            {
                Console.WriteLine($"'(ulong)4294967294' was evaluted to '{(ulong)integerOneDecrementUnderUInt32MaxValue}'. Expected: '4294967294'.");
                _counter++;
            }
        }
    }
}
