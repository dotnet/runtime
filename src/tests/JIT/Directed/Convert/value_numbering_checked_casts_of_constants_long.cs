// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

public partial class ValueNumberingCheckedCastsOfConstants
{
    private static void TestCastingInt64ToSingle()
    {
        ConfirmIntegerZeroCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToSingleIsFoldedCorrectly()
        {
            long integerZero = 0;

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
            long sByteMinValue = -128;

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
            long sByteMaxValue = 127;

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
            long integerOneDecrementUnderSByteMinValue = -129;

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
            long integerOneIncrementAboveSByteMinValue = -127;

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
            long integerOneDecrementUnderSByteMaxValue = 126;

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
            long integerOneIncrementAboveSByteMaxValue = 128;

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
            long byteMaxValue = 255;

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
            long integerOneDecrementUnderByteMinValue = -1;

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
            long integerOneIncrementAboveByteMinValue = 1;

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
            long integerOneDecrementUnderByteMaxValue = 254;

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
            long integerOneIncrementAboveByteMaxValue = 256;

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
            long int16MinValue = -32768;

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
            long int16MaxValue = 32767;

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
            long integerOneDecrementUnderInt16MinValue = -32769;

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
            long integerOneIncrementAboveInt16MinValue = -32767;

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
            long integerOneDecrementUnderInt16MaxValue = 32766;

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
            long integerOneIncrementAboveInt16MaxValue = 32768;

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
            long uInt16MaxValue = 65535;

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
            long integerOneDecrementUnderUInt16MaxValue = 65534;

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
            long integerOneIncrementAboveUInt16MaxValue = 65536;

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
            long int32MinValue = -2147483648;

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
            long int32MaxValue = 2147483647;

            if (BreakUpFlow())
                return;

            if (checked((float)int32MaxValue) != 2.1474836E+09f)
            {
                Console.WriteLine($"'(float)2147483647' was evaluted to '{(float)int32MaxValue}'. Expected: '2.1474836E+09f'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt32MinValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt32MinValueCastToSingleIsFoldedCorrectly()
        {
            long integerOneDecrementUnderInt32MinValue = -2147483649;

            if (BreakUpFlow())
                return;

            if (checked((float)integerOneDecrementUnderInt32MinValue) != -2.1474836E+09f)
            {
                Console.WriteLine($"'(float)-2147483649' was evaluted to '{(float)integerOneDecrementUnderInt32MinValue}'. Expected: '-2.1474836E+09f'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt32MinValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt32MinValueCastToSingleIsFoldedCorrectly()
        {
            long integerOneIncrementAboveInt32MinValue = -2147483647;

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
            long integerOneDecrementUnderInt32MaxValue = 2147483646;

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
            long integerOneIncrementAboveInt32MaxValue = 2147483648;

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
            long uInt32MaxValue = 4294967295;

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
            long integerOneDecrementUnderUInt32MaxValue = 4294967294;

            if (BreakUpFlow())
                return;

            if (checked((float)integerOneDecrementUnderUInt32MaxValue) != 4.2949673E+09f)
            {
                Console.WriteLine($"'(float)4294967294' was evaluted to '{(float)integerOneDecrementUnderUInt32MaxValue}'. Expected: '4.2949673E+09f'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveUInt32MaxValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt32MaxValueCastToSingleIsFoldedCorrectly()
        {
            long integerOneIncrementAboveUInt32MaxValue = 4294967296;

            if (BreakUpFlow())
                return;

            if (checked((float)integerOneIncrementAboveUInt32MaxValue) != 4.2949673E+09f)
            {
                Console.WriteLine($"'(float)4294967296' was evaluted to '{(float)integerOneIncrementAboveUInt32MaxValue}'. Expected: '4.2949673E+09f'.");
                _counter++;
            }
        }
        ConfirmInt64MinValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MinValueCastToSingleIsFoldedCorrectly()
        {
            long int64MinValue = -9223372036854775808;

            if (BreakUpFlow())
                return;

            if (checked((float)int64MinValue) != -9.223372E+18f)
            {
                Console.WriteLine($"'(float)-9223372036854775808' was evaluted to '{(float)int64MinValue}'. Expected: '-9.223372E+18f'.");
                _counter++;
            }
        }
        ConfirmInt64MaxValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MaxValueCastToSingleIsFoldedCorrectly()
        {
            long int64MaxValue = 9223372036854775807;

            if (BreakUpFlow())
                return;

            if (checked((float)int64MaxValue) != 9.223372E+18f)
            {
                Console.WriteLine($"'(float)9223372036854775807' was evaluted to '{(float)int64MaxValue}'. Expected: '9.223372E+18f'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt64MinValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt64MinValueCastToSingleIsFoldedCorrectly()
        {
            long integerOneIncrementAboveInt64MinValue = -9223372036854775807;

            if (BreakUpFlow())
                return;

            if (checked((float)integerOneIncrementAboveInt64MinValue) != -9.223372E+18f)
            {
                Console.WriteLine($"'(float)-9223372036854775807' was evaluted to '{(float)integerOneIncrementAboveInt64MinValue}'. Expected: '-9.223372E+18f'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt64MaxValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt64MaxValueCastToSingleIsFoldedCorrectly()
        {
            long integerOneDecrementUnderInt64MaxValue = 9223372036854775806;

            if (BreakUpFlow())
                return;

            if (checked((float)integerOneDecrementUnderInt64MaxValue) != 9.223372E+18f)
            {
                Console.WriteLine($"'(float)9223372036854775806' was evaluted to '{(float)integerOneDecrementUnderInt64MaxValue}'. Expected: '9.223372E+18f'.");
                _counter++;
            }
        }
    }

    private static void TestCastingInt64ToDouble()
    {
        ConfirmIntegerZeroCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToDoubleIsFoldedCorrectly()
        {
            long integerZero = 0;

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
            long sByteMinValue = -128;

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
            long sByteMaxValue = 127;

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
            long integerOneDecrementUnderSByteMinValue = -129;

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
            long integerOneIncrementAboveSByteMinValue = -127;

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
            long integerOneDecrementUnderSByteMaxValue = 126;

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
            long integerOneIncrementAboveSByteMaxValue = 128;

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
            long byteMaxValue = 255;

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
            long integerOneDecrementUnderByteMinValue = -1;

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
            long integerOneIncrementAboveByteMinValue = 1;

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
            long integerOneDecrementUnderByteMaxValue = 254;

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
            long integerOneIncrementAboveByteMaxValue = 256;

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
            long int16MinValue = -32768;

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
            long int16MaxValue = 32767;

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
            long integerOneDecrementUnderInt16MinValue = -32769;

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
            long integerOneIncrementAboveInt16MinValue = -32767;

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
            long integerOneDecrementUnderInt16MaxValue = 32766;

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
            long integerOneIncrementAboveInt16MaxValue = 32768;

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
            long uInt16MaxValue = 65535;

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
            long integerOneDecrementUnderUInt16MaxValue = 65534;

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
            long integerOneIncrementAboveUInt16MaxValue = 65536;

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
            long int32MinValue = -2147483648;

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
            long int32MaxValue = 2147483647;

            if (BreakUpFlow())
                return;

            if (checked((double)int32MaxValue) != 2147483647d)
            {
                Console.WriteLine($"'(double)2147483647' was evaluted to '{(double)int32MaxValue}'. Expected: '2147483647d'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt32MinValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt32MinValueCastToDoubleIsFoldedCorrectly()
        {
            long integerOneDecrementUnderInt32MinValue = -2147483649;

            if (BreakUpFlow())
                return;

            if (checked((double)integerOneDecrementUnderInt32MinValue) != -2147483649d)
            {
                Console.WriteLine($"'(double)-2147483649' was evaluted to '{(double)integerOneDecrementUnderInt32MinValue}'. Expected: '-2147483649d'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt32MinValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt32MinValueCastToDoubleIsFoldedCorrectly()
        {
            long integerOneIncrementAboveInt32MinValue = -2147483647;

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
            long integerOneDecrementUnderInt32MaxValue = 2147483646;

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
            long integerOneIncrementAboveInt32MaxValue = 2147483648;

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
            long uInt32MaxValue = 4294967295;

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
            long integerOneDecrementUnderUInt32MaxValue = 4294967294;

            if (BreakUpFlow())
                return;

            if (checked((double)integerOneDecrementUnderUInt32MaxValue) != 4294967294d)
            {
                Console.WriteLine($"'(double)4294967294' was evaluted to '{(double)integerOneDecrementUnderUInt32MaxValue}'. Expected: '4294967294d'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveUInt32MaxValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt32MaxValueCastToDoubleIsFoldedCorrectly()
        {
            long integerOneIncrementAboveUInt32MaxValue = 4294967296;

            if (BreakUpFlow())
                return;

            if (checked((double)integerOneIncrementAboveUInt32MaxValue) != 4294967296d)
            {
                Console.WriteLine($"'(double)4294967296' was evaluted to '{(double)integerOneIncrementAboveUInt32MaxValue}'. Expected: '4294967296d'.");
                _counter++;
            }
        }
        ConfirmInt64MinValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MinValueCastToDoubleIsFoldedCorrectly()
        {
            long int64MinValue = -9223372036854775808;

            if (BreakUpFlow())
                return;

            if (checked((double)int64MinValue) != -9.223372036854776E+18d)
            {
                Console.WriteLine($"'(double)-9223372036854775808' was evaluted to '{(double)int64MinValue}'. Expected: '-9.223372036854776E+18d'.");
                _counter++;
            }
        }
        ConfirmInt64MaxValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MaxValueCastToDoubleIsFoldedCorrectly()
        {
            long int64MaxValue = 9223372036854775807;

            if (BreakUpFlow())
                return;

            if (checked((double)int64MaxValue) != 9.223372036854776E+18d)
            {
                Console.WriteLine($"'(double)9223372036854775807' was evaluted to '{(double)int64MaxValue}'. Expected: '9.223372036854776E+18d'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt64MinValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt64MinValueCastToDoubleIsFoldedCorrectly()
        {
            long integerOneIncrementAboveInt64MinValue = -9223372036854775807;

            if (BreakUpFlow())
                return;

            if (checked((double)integerOneIncrementAboveInt64MinValue) != -9.223372036854776E+18d)
            {
                Console.WriteLine($"'(double)-9223372036854775807' was evaluted to '{(double)integerOneIncrementAboveInt64MinValue}'. Expected: '-9.223372036854776E+18d'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt64MaxValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt64MaxValueCastToDoubleIsFoldedCorrectly()
        {
            long integerOneDecrementUnderInt64MaxValue = 9223372036854775806;

            if (BreakUpFlow())
                return;

            if (checked((double)integerOneDecrementUnderInt64MaxValue) != 9.223372036854776E+18d)
            {
                Console.WriteLine($"'(double)9223372036854775806' was evaluted to '{(double)integerOneDecrementUnderInt64MaxValue}'. Expected: '9.223372036854776E+18d'.");
                _counter++;
            }
        }
    }

    private static void TestCastingInt64ToSByte()
    {
        ConfirmIntegerZeroCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToSByteIsFoldedCorrectly()
        {
            long integerZero = 0;

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
            long sByteMinValue = -128;

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
            long sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(sbyte)127' was evaluted to '{(sbyte)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmInt64OneDecrementUnderSByteMinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneDecrementUnderSByteMinValueCastToSByteOverflows()
        {
            long from = -129;
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
            long integerOneIncrementAboveSByteMinValue = -127;

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
            long integerOneDecrementUnderSByteMaxValue = 126;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(sbyte)126' was evaluted to '{(sbyte)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmInt64OneIncrementAboveSByteMaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneIncrementAboveSByteMaxValueCastToSByteOverflows()
        {
            long from = 128;
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
            long from = 255;
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
            long integerOneDecrementUnderByteMinValue = -1;

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
            long integerOneIncrementAboveByteMinValue = 1;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(sbyte)1' was evaluted to '{(sbyte)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmInt64OneDecrementUnderByteMaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneDecrementUnderByteMaxValueCastToSByteOverflows()
        {
            long from = 254;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)254)' did not throw OverflowException.");
        }
        ConfirmInt64OneIncrementAboveByteMaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneIncrementAboveByteMaxValueCastToSByteOverflows()
        {
            long from = 256;
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
            long from = -32768;
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
            long from = 32767;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)32767)' did not throw OverflowException.");
        }
        ConfirmInt64OneDecrementUnderInt16MinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneDecrementUnderInt16MinValueCastToSByteOverflows()
        {
            long from = -32769;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-32769)' did not throw OverflowException.");
        }
        ConfirmInt64OneIncrementAboveInt16MinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneIncrementAboveInt16MinValueCastToSByteOverflows()
        {
            long from = -32767;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-32767)' did not throw OverflowException.");
        }
        ConfirmInt64OneDecrementUnderInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneDecrementUnderInt16MaxValueCastToSByteOverflows()
        {
            long from = 32766;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)32766)' did not throw OverflowException.");
        }
        ConfirmInt64OneIncrementAboveInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneIncrementAboveInt16MaxValueCastToSByteOverflows()
        {
            long from = 32768;
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
            long from = 65535;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)65535)' did not throw OverflowException.");
        }
        ConfirmInt64OneDecrementUnderUInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneDecrementUnderUInt16MaxValueCastToSByteOverflows()
        {
            long from = 65534;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)65534)' did not throw OverflowException.");
        }
        ConfirmInt64OneIncrementAboveUInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneIncrementAboveUInt16MaxValueCastToSByteOverflows()
        {
            long from = 65536;
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
            long from = -2147483648;
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
            long from = 2147483647;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)2147483647)' did not throw OverflowException.");
        }
        ConfirmInt64OneDecrementUnderInt32MinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneDecrementUnderInt32MinValueCastToSByteOverflows()
        {
            long from = -2147483649;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-2147483649)' did not throw OverflowException.");
        }
        ConfirmInt64OneIncrementAboveInt32MinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneIncrementAboveInt32MinValueCastToSByteOverflows()
        {
            long from = -2147483647;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-2147483647)' did not throw OverflowException.");
        }
        ConfirmInt64OneDecrementUnderInt32MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneDecrementUnderInt32MaxValueCastToSByteOverflows()
        {
            long from = 2147483646;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)2147483646)' did not throw OverflowException.");
        }
        ConfirmInt64OneIncrementAboveInt32MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneIncrementAboveInt32MaxValueCastToSByteOverflows()
        {
            long from = 2147483648;
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
            long from = 4294967295;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)4294967295)' did not throw OverflowException.");
        }
        ConfirmInt64OneDecrementUnderUInt32MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneDecrementUnderUInt32MaxValueCastToSByteOverflows()
        {
            long from = 4294967294;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)4294967294)' did not throw OverflowException.");
        }
        ConfirmInt64OneIncrementAboveUInt32MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneIncrementAboveUInt32MaxValueCastToSByteOverflows()
        {
            long from = 4294967296;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)4294967296)' did not throw OverflowException.");
        }
        ConfirmInt64MinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MinValueCastToSByteOverflows()
        {
            long from = -9223372036854775808;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-9223372036854775808)' did not throw OverflowException.");
        }
        ConfirmInt64MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MaxValueCastToSByteOverflows()
        {
            long from = 9223372036854775807;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)9223372036854775807)' did not throw OverflowException.");
        }
        ConfirmInt64OneIncrementAboveInt64MinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneIncrementAboveInt64MinValueCastToSByteOverflows()
        {
            long from = -9223372036854775807;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-9223372036854775807)' did not throw OverflowException.");
        }
        ConfirmInt64OneDecrementUnderInt64MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneDecrementUnderInt64MaxValueCastToSByteOverflows()
        {
            long from = 9223372036854775806;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)9223372036854775806)' did not throw OverflowException.");
        }
    }

    private static void TestCastingInt64ToByte()
    {
        ConfirmIntegerZeroCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToByteIsFoldedCorrectly()
        {
            long integerZero = 0;

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
            long from = -128;
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
            long sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((byte)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(byte)127' was evaluted to '{(byte)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmInt64OneDecrementUnderSByteMinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneDecrementUnderSByteMinValueCastToByteOverflows()
        {
            long from = -129;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-129)' did not throw OverflowException.");
        }
        ConfirmInt64OneIncrementAboveSByteMinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneIncrementAboveSByteMinValueCastToByteOverflows()
        {
            long from = -127;
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
            long integerOneDecrementUnderSByteMaxValue = 126;

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
            long integerOneIncrementAboveSByteMaxValue = 128;

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
            long byteMaxValue = 255;

            if (BreakUpFlow())
                return;

            if (checked((byte)byteMaxValue) != 255)
            {
                Console.WriteLine($"'(byte)255' was evaluted to '{(byte)byteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmInt64OneDecrementUnderByteMinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneDecrementUnderByteMinValueCastToByteOverflows()
        {
            long from = -1;
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
            long integerOneIncrementAboveByteMinValue = 1;

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
            long integerOneDecrementUnderByteMaxValue = 254;

            if (BreakUpFlow())
                return;

            if (checked((byte)integerOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(byte)254' was evaluted to '{(byte)integerOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmInt64OneIncrementAboveByteMaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneIncrementAboveByteMaxValueCastToByteOverflows()
        {
            long from = 256;
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
            long from = -32768;
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
            long from = 32767;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)32767)' did not throw OverflowException.");
        }
        ConfirmInt64OneDecrementUnderInt16MinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneDecrementUnderInt16MinValueCastToByteOverflows()
        {
            long from = -32769;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-32769)' did not throw OverflowException.");
        }
        ConfirmInt64OneIncrementAboveInt16MinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneIncrementAboveInt16MinValueCastToByteOverflows()
        {
            long from = -32767;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-32767)' did not throw OverflowException.");
        }
        ConfirmInt64OneDecrementUnderInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneDecrementUnderInt16MaxValueCastToByteOverflows()
        {
            long from = 32766;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)32766)' did not throw OverflowException.");
        }
        ConfirmInt64OneIncrementAboveInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneIncrementAboveInt16MaxValueCastToByteOverflows()
        {
            long from = 32768;
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
            long from = 65535;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)65535)' did not throw OverflowException.");
        }
        ConfirmInt64OneDecrementUnderUInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneDecrementUnderUInt16MaxValueCastToByteOverflows()
        {
            long from = 65534;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)65534)' did not throw OverflowException.");
        }
        ConfirmInt64OneIncrementAboveUInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneIncrementAboveUInt16MaxValueCastToByteOverflows()
        {
            long from = 65536;
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
            long from = -2147483648;
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
            long from = 2147483647;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)2147483647)' did not throw OverflowException.");
        }
        ConfirmInt64OneDecrementUnderInt32MinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneDecrementUnderInt32MinValueCastToByteOverflows()
        {
            long from = -2147483649;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-2147483649)' did not throw OverflowException.");
        }
        ConfirmInt64OneIncrementAboveInt32MinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneIncrementAboveInt32MinValueCastToByteOverflows()
        {
            long from = -2147483647;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-2147483647)' did not throw OverflowException.");
        }
        ConfirmInt64OneDecrementUnderInt32MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneDecrementUnderInt32MaxValueCastToByteOverflows()
        {
            long from = 2147483646;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)2147483646)' did not throw OverflowException.");
        }
        ConfirmInt64OneIncrementAboveInt32MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneIncrementAboveInt32MaxValueCastToByteOverflows()
        {
            long from = 2147483648;
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
            long from = 4294967295;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)4294967295)' did not throw OverflowException.");
        }
        ConfirmInt64OneDecrementUnderUInt32MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneDecrementUnderUInt32MaxValueCastToByteOverflows()
        {
            long from = 4294967294;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)4294967294)' did not throw OverflowException.");
        }
        ConfirmInt64OneIncrementAboveUInt32MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneIncrementAboveUInt32MaxValueCastToByteOverflows()
        {
            long from = 4294967296;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)4294967296)' did not throw OverflowException.");
        }
        ConfirmInt64MinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MinValueCastToByteOverflows()
        {
            long from = -9223372036854775808;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-9223372036854775808)' did not throw OverflowException.");
        }
        ConfirmInt64MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MaxValueCastToByteOverflows()
        {
            long from = 9223372036854775807;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)9223372036854775807)' did not throw OverflowException.");
        }
        ConfirmInt64OneIncrementAboveInt64MinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneIncrementAboveInt64MinValueCastToByteOverflows()
        {
            long from = -9223372036854775807;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-9223372036854775807)' did not throw OverflowException.");
        }
        ConfirmInt64OneDecrementUnderInt64MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneDecrementUnderInt64MaxValueCastToByteOverflows()
        {
            long from = 9223372036854775806;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)9223372036854775806)' did not throw OverflowException.");
        }
    }

    private static void TestCastingInt64ToInt16()
    {
        ConfirmIntegerZeroCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToInt16IsFoldedCorrectly()
        {
            long integerZero = 0;

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
            long sByteMinValue = -128;

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
            long sByteMaxValue = 127;

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
            long integerOneDecrementUnderSByteMinValue = -129;

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
            long integerOneIncrementAboveSByteMinValue = -127;

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
            long integerOneDecrementUnderSByteMaxValue = 126;

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
            long integerOneIncrementAboveSByteMaxValue = 128;

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
            long byteMaxValue = 255;

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
            long integerOneDecrementUnderByteMinValue = -1;

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
            long integerOneIncrementAboveByteMinValue = 1;

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
            long integerOneDecrementUnderByteMaxValue = 254;

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
            long integerOneIncrementAboveByteMaxValue = 256;

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
            long int16MinValue = -32768;

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
            long int16MaxValue = 32767;

            if (BreakUpFlow())
                return;

            if (checked((short)int16MaxValue) != 32767)
            {
                Console.WriteLine($"'(short)32767' was evaluted to '{(short)int16MaxValue}'. Expected: '32767'.");
                _counter++;
            }
        }
        ConfirmInt64OneDecrementUnderInt16MinValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneDecrementUnderInt16MinValueCastToInt16Overflows()
        {
            long from = -32769;
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
            long integerOneIncrementAboveInt16MinValue = -32767;

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
            long integerOneDecrementUnderInt16MaxValue = 32766;

            if (BreakUpFlow())
                return;

            if (checked((short)integerOneDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(short)32766' was evaluted to '{(short)integerOneDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
        ConfirmInt64OneIncrementAboveInt16MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneIncrementAboveInt16MaxValueCastToInt16Overflows()
        {
            long from = 32768;
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
            long from = 65535;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)65535)' did not throw OverflowException.");
        }
        ConfirmInt64OneDecrementUnderUInt16MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneDecrementUnderUInt16MaxValueCastToInt16Overflows()
        {
            long from = 65534;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)65534)' did not throw OverflowException.");
        }
        ConfirmInt64OneIncrementAboveUInt16MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneIncrementAboveUInt16MaxValueCastToInt16Overflows()
        {
            long from = 65536;
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
            long from = -2147483648;
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
            long from = 2147483647;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)2147483647)' did not throw OverflowException.");
        }
        ConfirmInt64OneDecrementUnderInt32MinValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneDecrementUnderInt32MinValueCastToInt16Overflows()
        {
            long from = -2147483649;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)-2147483649)' did not throw OverflowException.");
        }
        ConfirmInt64OneIncrementAboveInt32MinValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneIncrementAboveInt32MinValueCastToInt16Overflows()
        {
            long from = -2147483647;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)-2147483647)' did not throw OverflowException.");
        }
        ConfirmInt64OneDecrementUnderInt32MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneDecrementUnderInt32MaxValueCastToInt16Overflows()
        {
            long from = 2147483646;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)2147483646)' did not throw OverflowException.");
        }
        ConfirmInt64OneIncrementAboveInt32MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneIncrementAboveInt32MaxValueCastToInt16Overflows()
        {
            long from = 2147483648;
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
            long from = 4294967295;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)4294967295)' did not throw OverflowException.");
        }
        ConfirmInt64OneDecrementUnderUInt32MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneDecrementUnderUInt32MaxValueCastToInt16Overflows()
        {
            long from = 4294967294;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)4294967294)' did not throw OverflowException.");
        }
        ConfirmInt64OneIncrementAboveUInt32MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneIncrementAboveUInt32MaxValueCastToInt16Overflows()
        {
            long from = 4294967296;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)4294967296)' did not throw OverflowException.");
        }
        ConfirmInt64MinValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MinValueCastToInt16Overflows()
        {
            long from = -9223372036854775808;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)-9223372036854775808)' did not throw OverflowException.");
        }
        ConfirmInt64MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MaxValueCastToInt16Overflows()
        {
            long from = 9223372036854775807;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)9223372036854775807)' did not throw OverflowException.");
        }
        ConfirmInt64OneIncrementAboveInt64MinValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneIncrementAboveInt64MinValueCastToInt16Overflows()
        {
            long from = -9223372036854775807;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)-9223372036854775807)' did not throw OverflowException.");
        }
        ConfirmInt64OneDecrementUnderInt64MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneDecrementUnderInt64MaxValueCastToInt16Overflows()
        {
            long from = 9223372036854775806;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)9223372036854775806)' did not throw OverflowException.");
        }
    }

    private static void TestCastingInt64ToUInt16()
    {
        ConfirmIntegerZeroCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToUInt16IsFoldedCorrectly()
        {
            long integerZero = 0;

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
            long from = -128;
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
            long sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((ushort)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(ushort)127' was evaluted to '{(ushort)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmInt64OneDecrementUnderSByteMinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneDecrementUnderSByteMinValueCastToUInt16Overflows()
        {
            long from = -129;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-129)' did not throw OverflowException.");
        }
        ConfirmInt64OneIncrementAboveSByteMinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneIncrementAboveSByteMinValueCastToUInt16Overflows()
        {
            long from = -127;
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
            long integerOneDecrementUnderSByteMaxValue = 126;

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
            long integerOneIncrementAboveSByteMaxValue = 128;

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
            long byteMaxValue = 255;

            if (BreakUpFlow())
                return;

            if (checked((ushort)byteMaxValue) != 255)
            {
                Console.WriteLine($"'(ushort)255' was evaluted to '{(ushort)byteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmInt64OneDecrementUnderByteMinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneDecrementUnderByteMinValueCastToUInt16Overflows()
        {
            long from = -1;
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
            long integerOneIncrementAboveByteMinValue = 1;

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
            long integerOneDecrementUnderByteMaxValue = 254;

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
            long integerOneIncrementAboveByteMaxValue = 256;

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
            long from = -32768;
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
            long int16MaxValue = 32767;

            if (BreakUpFlow())
                return;

            if (checked((ushort)int16MaxValue) != 32767)
            {
                Console.WriteLine($"'(ushort)32767' was evaluted to '{(ushort)int16MaxValue}'. Expected: '32767'.");
                _counter++;
            }
        }
        ConfirmInt64OneDecrementUnderInt16MinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneDecrementUnderInt16MinValueCastToUInt16Overflows()
        {
            long from = -32769;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-32769)' did not throw OverflowException.");
        }
        ConfirmInt64OneIncrementAboveInt16MinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneIncrementAboveInt16MinValueCastToUInt16Overflows()
        {
            long from = -32767;
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
            long integerOneDecrementUnderInt16MaxValue = 32766;

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
            long integerOneIncrementAboveInt16MaxValue = 32768;

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
            long uInt16MaxValue = 65535;

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
            long integerOneDecrementUnderUInt16MaxValue = 65534;

            if (BreakUpFlow())
                return;

            if (checked((ushort)integerOneDecrementUnderUInt16MaxValue) != 65534)
            {
                Console.WriteLine($"'(ushort)65534' was evaluted to '{(ushort)integerOneDecrementUnderUInt16MaxValue}'. Expected: '65534'.");
                _counter++;
            }
        }
        ConfirmInt64OneIncrementAboveUInt16MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneIncrementAboveUInt16MaxValueCastToUInt16Overflows()
        {
            long from = 65536;
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
            long from = -2147483648;
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
            long from = 2147483647;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)2147483647)' did not throw OverflowException.");
        }
        ConfirmInt64OneDecrementUnderInt32MinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneDecrementUnderInt32MinValueCastToUInt16Overflows()
        {
            long from = -2147483649;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-2147483649)' did not throw OverflowException.");
        }
        ConfirmInt64OneIncrementAboveInt32MinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneIncrementAboveInt32MinValueCastToUInt16Overflows()
        {
            long from = -2147483647;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-2147483647)' did not throw OverflowException.");
        }
        ConfirmInt64OneDecrementUnderInt32MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneDecrementUnderInt32MaxValueCastToUInt16Overflows()
        {
            long from = 2147483646;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)2147483646)' did not throw OverflowException.");
        }
        ConfirmInt64OneIncrementAboveInt32MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneIncrementAboveInt32MaxValueCastToUInt16Overflows()
        {
            long from = 2147483648;
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
            long from = 4294967295;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)4294967295)' did not throw OverflowException.");
        }
        ConfirmInt64OneDecrementUnderUInt32MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneDecrementUnderUInt32MaxValueCastToUInt16Overflows()
        {
            long from = 4294967294;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)4294967294)' did not throw OverflowException.");
        }
        ConfirmInt64OneIncrementAboveUInt32MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneIncrementAboveUInt32MaxValueCastToUInt16Overflows()
        {
            long from = 4294967296;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)4294967296)' did not throw OverflowException.");
        }
        ConfirmInt64MinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MinValueCastToUInt16Overflows()
        {
            long from = -9223372036854775808;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-9223372036854775808)' did not throw OverflowException.");
        }
        ConfirmInt64MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MaxValueCastToUInt16Overflows()
        {
            long from = 9223372036854775807;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)9223372036854775807)' did not throw OverflowException.");
        }
        ConfirmInt64OneIncrementAboveInt64MinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneIncrementAboveInt64MinValueCastToUInt16Overflows()
        {
            long from = -9223372036854775807;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-9223372036854775807)' did not throw OverflowException.");
        }
        ConfirmInt64OneDecrementUnderInt64MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneDecrementUnderInt64MaxValueCastToUInt16Overflows()
        {
            long from = 9223372036854775806;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)9223372036854775806)' did not throw OverflowException.");
        }
    }

    private static void TestCastingInt64ToInt32()
    {
        ConfirmIntegerZeroCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToInt32IsFoldedCorrectly()
        {
            long integerZero = 0;

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
            long sByteMinValue = -128;

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
            long sByteMaxValue = 127;

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
            long integerOneDecrementUnderSByteMinValue = -129;

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
            long integerOneIncrementAboveSByteMinValue = -127;

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
            long integerOneDecrementUnderSByteMaxValue = 126;

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
            long integerOneIncrementAboveSByteMaxValue = 128;

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
            long byteMaxValue = 255;

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
            long integerOneDecrementUnderByteMinValue = -1;

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
            long integerOneIncrementAboveByteMinValue = 1;

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
            long integerOneDecrementUnderByteMaxValue = 254;

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
            long integerOneIncrementAboveByteMaxValue = 256;

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
            long int16MinValue = -32768;

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
            long int16MaxValue = 32767;

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
            long integerOneDecrementUnderInt16MinValue = -32769;

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
            long integerOneIncrementAboveInt16MinValue = -32767;

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
            long integerOneDecrementUnderInt16MaxValue = 32766;

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
            long integerOneIncrementAboveInt16MaxValue = 32768;

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
            long uInt16MaxValue = 65535;

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
            long integerOneDecrementUnderUInt16MaxValue = 65534;

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
            long integerOneIncrementAboveUInt16MaxValue = 65536;

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
            long int32MinValue = -2147483648;

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
            long int32MaxValue = 2147483647;

            if (BreakUpFlow())
                return;

            if (checked((int)int32MaxValue) != 2147483647)
            {
                Console.WriteLine($"'(int)2147483647' was evaluted to '{(int)int32MaxValue}'. Expected: '2147483647'.");
                _counter++;
            }
        }
        ConfirmInt64OneDecrementUnderInt32MinValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneDecrementUnderInt32MinValueCastToInt32Overflows()
        {
            long from = -2147483649;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)-2147483649)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt32MinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt32MinValueCastToInt32IsFoldedCorrectly()
        {
            long integerOneIncrementAboveInt32MinValue = -2147483647;

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
            long integerOneDecrementUnderInt32MaxValue = 2147483646;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneDecrementUnderInt32MaxValue) != 2147483646)
            {
                Console.WriteLine($"'(int)2147483646' was evaluted to '{(int)integerOneDecrementUnderInt32MaxValue}'. Expected: '2147483646'.");
                _counter++;
            }
        }
        ConfirmInt64OneIncrementAboveInt32MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneIncrementAboveInt32MaxValueCastToInt32Overflows()
        {
            long from = 2147483648;
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
            long from = 4294967295;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)4294967295)' did not throw OverflowException.");
        }
        ConfirmInt64OneDecrementUnderUInt32MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneDecrementUnderUInt32MaxValueCastToInt32Overflows()
        {
            long from = 4294967294;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)4294967294)' did not throw OverflowException.");
        }
        ConfirmInt64OneIncrementAboveUInt32MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneIncrementAboveUInt32MaxValueCastToInt32Overflows()
        {
            long from = 4294967296;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)4294967296)' did not throw OverflowException.");
        }
        ConfirmInt64MinValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MinValueCastToInt32Overflows()
        {
            long from = -9223372036854775808;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)-9223372036854775808)' did not throw OverflowException.");
        }
        ConfirmInt64MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MaxValueCastToInt32Overflows()
        {
            long from = 9223372036854775807;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)9223372036854775807)' did not throw OverflowException.");
        }
        ConfirmInt64OneIncrementAboveInt64MinValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneIncrementAboveInt64MinValueCastToInt32Overflows()
        {
            long from = -9223372036854775807;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)-9223372036854775807)' did not throw OverflowException.");
        }
        ConfirmInt64OneDecrementUnderInt64MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneDecrementUnderInt64MaxValueCastToInt32Overflows()
        {
            long from = 9223372036854775806;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)9223372036854775806)' did not throw OverflowException.");
        }
    }

    private static void TestCastingInt64ToUInt32()
    {
        ConfirmIntegerZeroCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToUInt32IsFoldedCorrectly()
        {
            long integerZero = 0;

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
            long from = -128;
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
            long sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((uint)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(uint)127' was evaluted to '{(uint)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmInt64OneDecrementUnderSByteMinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneDecrementUnderSByteMinValueCastToUInt32Overflows()
        {
            long from = -129;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-129)' did not throw OverflowException.");
        }
        ConfirmInt64OneIncrementAboveSByteMinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneIncrementAboveSByteMinValueCastToUInt32Overflows()
        {
            long from = -127;
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
            long integerOneDecrementUnderSByteMaxValue = 126;

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
            long integerOneIncrementAboveSByteMaxValue = 128;

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
            long byteMaxValue = 255;

            if (BreakUpFlow())
                return;

            if (checked((uint)byteMaxValue) != 255)
            {
                Console.WriteLine($"'(uint)255' was evaluted to '{(uint)byteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmInt64OneDecrementUnderByteMinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneDecrementUnderByteMinValueCastToUInt32Overflows()
        {
            long from = -1;
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
            long integerOneIncrementAboveByteMinValue = 1;

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
            long integerOneDecrementUnderByteMaxValue = 254;

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
            long integerOneIncrementAboveByteMaxValue = 256;

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
            long from = -32768;
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
            long int16MaxValue = 32767;

            if (BreakUpFlow())
                return;

            if (checked((uint)int16MaxValue) != 32767)
            {
                Console.WriteLine($"'(uint)32767' was evaluted to '{(uint)int16MaxValue}'. Expected: '32767'.");
                _counter++;
            }
        }
        ConfirmInt64OneDecrementUnderInt16MinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneDecrementUnderInt16MinValueCastToUInt32Overflows()
        {
            long from = -32769;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-32769)' did not throw OverflowException.");
        }
        ConfirmInt64OneIncrementAboveInt16MinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneIncrementAboveInt16MinValueCastToUInt32Overflows()
        {
            long from = -32767;
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
            long integerOneDecrementUnderInt16MaxValue = 32766;

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
            long integerOneIncrementAboveInt16MaxValue = 32768;

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
            long uInt16MaxValue = 65535;

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
            long integerOneDecrementUnderUInt16MaxValue = 65534;

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
            long integerOneIncrementAboveUInt16MaxValue = 65536;

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
            long from = -2147483648;
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
            long int32MaxValue = 2147483647;

            if (BreakUpFlow())
                return;

            if (checked((uint)int32MaxValue) != 2147483647)
            {
                Console.WriteLine($"'(uint)2147483647' was evaluted to '{(uint)int32MaxValue}'. Expected: '2147483647'.");
                _counter++;
            }
        }
        ConfirmInt64OneDecrementUnderInt32MinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneDecrementUnderInt32MinValueCastToUInt32Overflows()
        {
            long from = -2147483649;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-2147483649)' did not throw OverflowException.");
        }
        ConfirmInt64OneIncrementAboveInt32MinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneIncrementAboveInt32MinValueCastToUInt32Overflows()
        {
            long from = -2147483647;
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
            long integerOneDecrementUnderInt32MaxValue = 2147483646;

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
            long integerOneIncrementAboveInt32MaxValue = 2147483648;

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
            long uInt32MaxValue = 4294967295;

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
            long integerOneDecrementUnderUInt32MaxValue = 4294967294;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneDecrementUnderUInt32MaxValue) != 4294967294)
            {
                Console.WriteLine($"'(uint)4294967294' was evaluted to '{(uint)integerOneDecrementUnderUInt32MaxValue}'. Expected: '4294967294'.");
                _counter++;
            }
        }
        ConfirmInt64OneIncrementAboveUInt32MaxValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneIncrementAboveUInt32MaxValueCastToUInt32Overflows()
        {
            long from = 4294967296;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)4294967296)' did not throw OverflowException.");
        }
        ConfirmInt64MinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MinValueCastToUInt32Overflows()
        {
            long from = -9223372036854775808;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-9223372036854775808)' did not throw OverflowException.");
        }
        ConfirmInt64MaxValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MaxValueCastToUInt32Overflows()
        {
            long from = 9223372036854775807;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)9223372036854775807)' did not throw OverflowException.");
        }
        ConfirmInt64OneIncrementAboveInt64MinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneIncrementAboveInt64MinValueCastToUInt32Overflows()
        {
            long from = -9223372036854775807;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-9223372036854775807)' did not throw OverflowException.");
        }
        ConfirmInt64OneDecrementUnderInt64MaxValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneDecrementUnderInt64MaxValueCastToUInt32Overflows()
        {
            long from = 9223372036854775806;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)9223372036854775806)' did not throw OverflowException.");
        }
    }

    private static void TestCastingInt64ToInt64()
    {
        ConfirmIntegerZeroCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToInt64IsFoldedCorrectly()
        {
            long integerZero = 0;

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
            long sByteMinValue = -128;

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
            long sByteMaxValue = 127;

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
            long integerOneDecrementUnderSByteMinValue = -129;

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
            long integerOneIncrementAboveSByteMinValue = -127;

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
            long integerOneDecrementUnderSByteMaxValue = 126;

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
            long integerOneIncrementAboveSByteMaxValue = 128;

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
            long byteMaxValue = 255;

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
            long integerOneDecrementUnderByteMinValue = -1;

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
            long integerOneIncrementAboveByteMinValue = 1;

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
            long integerOneDecrementUnderByteMaxValue = 254;

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
            long integerOneIncrementAboveByteMaxValue = 256;

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
            long int16MinValue = -32768;

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
            long int16MaxValue = 32767;

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
            long integerOneDecrementUnderInt16MinValue = -32769;

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
            long integerOneIncrementAboveInt16MinValue = -32767;

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
            long integerOneDecrementUnderInt16MaxValue = 32766;

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
            long integerOneIncrementAboveInt16MaxValue = 32768;

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
            long uInt16MaxValue = 65535;

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
            long integerOneDecrementUnderUInt16MaxValue = 65534;

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
            long integerOneIncrementAboveUInt16MaxValue = 65536;

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
            long int32MinValue = -2147483648;

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
            long int32MaxValue = 2147483647;

            if (BreakUpFlow())
                return;

            if (checked((long)int32MaxValue) != 2147483647)
            {
                Console.WriteLine($"'(long)2147483647' was evaluted to '{(long)int32MaxValue}'. Expected: '2147483647'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt32MinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt32MinValueCastToInt64IsFoldedCorrectly()
        {
            long integerOneDecrementUnderInt32MinValue = -2147483649;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneDecrementUnderInt32MinValue) != -2147483649)
            {
                Console.WriteLine($"'(long)-2147483649' was evaluted to '{(long)integerOneDecrementUnderInt32MinValue}'. Expected: '-2147483649'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt32MinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt32MinValueCastToInt64IsFoldedCorrectly()
        {
            long integerOneIncrementAboveInt32MinValue = -2147483647;

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
            long integerOneDecrementUnderInt32MaxValue = 2147483646;

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
            long integerOneIncrementAboveInt32MaxValue = 2147483648;

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
            long uInt32MaxValue = 4294967295;

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
            long integerOneDecrementUnderUInt32MaxValue = 4294967294;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneDecrementUnderUInt32MaxValue) != 4294967294)
            {
                Console.WriteLine($"'(long)4294967294' was evaluted to '{(long)integerOneDecrementUnderUInt32MaxValue}'. Expected: '4294967294'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveUInt32MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt32MaxValueCastToInt64IsFoldedCorrectly()
        {
            long integerOneIncrementAboveUInt32MaxValue = 4294967296;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneIncrementAboveUInt32MaxValue) != 4294967296)
            {
                Console.WriteLine($"'(long)4294967296' was evaluted to '{(long)integerOneIncrementAboveUInt32MaxValue}'. Expected: '4294967296'.");
                _counter++;
            }
        }
        ConfirmInt64MinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MinValueCastToInt64IsFoldedCorrectly()
        {
            long int64MinValue = -9223372036854775808;

            if (BreakUpFlow())
                return;

            if (checked((long)int64MinValue) != -9223372036854775808)
            {
                Console.WriteLine($"'(long)-9223372036854775808' was evaluted to '{(long)int64MinValue}'. Expected: '-9223372036854775808'.");
                _counter++;
            }
        }
        ConfirmInt64MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MaxValueCastToInt64IsFoldedCorrectly()
        {
            long int64MaxValue = 9223372036854775807;

            if (BreakUpFlow())
                return;

            if (checked((long)int64MaxValue) != 9223372036854775807)
            {
                Console.WriteLine($"'(long)9223372036854775807' was evaluted to '{(long)int64MaxValue}'. Expected: '9223372036854775807'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt64MinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt64MinValueCastToInt64IsFoldedCorrectly()
        {
            long integerOneIncrementAboveInt64MinValue = -9223372036854775807;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneIncrementAboveInt64MinValue) != -9223372036854775807)
            {
                Console.WriteLine($"'(long)-9223372036854775807' was evaluted to '{(long)integerOneIncrementAboveInt64MinValue}'. Expected: '-9223372036854775807'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt64MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt64MaxValueCastToInt64IsFoldedCorrectly()
        {
            long integerOneDecrementUnderInt64MaxValue = 9223372036854775806;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneDecrementUnderInt64MaxValue) != 9223372036854775806)
            {
                Console.WriteLine($"'(long)9223372036854775806' was evaluted to '{(long)integerOneDecrementUnderInt64MaxValue}'. Expected: '9223372036854775806'.");
                _counter++;
            }
        }
    }

    private static void TestCastingInt64ToUInt64()
    {
        ConfirmIntegerZeroCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToUInt64IsFoldedCorrectly()
        {
            long integerZero = 0;

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
            long from = -128;
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
            long sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((ulong)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(ulong)127' was evaluted to '{(ulong)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmInt64OneDecrementUnderSByteMinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneDecrementUnderSByteMinValueCastToUInt64Overflows()
        {
            long from = -129;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-129)' did not throw OverflowException.");
        }
        ConfirmInt64OneIncrementAboveSByteMinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneIncrementAboveSByteMinValueCastToUInt64Overflows()
        {
            long from = -127;
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
            long integerOneDecrementUnderSByteMaxValue = 126;

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
            long integerOneIncrementAboveSByteMaxValue = 128;

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
            long byteMaxValue = 255;

            if (BreakUpFlow())
                return;

            if (checked((ulong)byteMaxValue) != 255)
            {
                Console.WriteLine($"'(ulong)255' was evaluted to '{(ulong)byteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmInt64OneDecrementUnderByteMinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneDecrementUnderByteMinValueCastToUInt64Overflows()
        {
            long from = -1;
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
            long integerOneIncrementAboveByteMinValue = 1;

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
            long integerOneDecrementUnderByteMaxValue = 254;

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
            long integerOneIncrementAboveByteMaxValue = 256;

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
            long from = -32768;
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
            long int16MaxValue = 32767;

            if (BreakUpFlow())
                return;

            if (checked((ulong)int16MaxValue) != 32767)
            {
                Console.WriteLine($"'(ulong)32767' was evaluted to '{(ulong)int16MaxValue}'. Expected: '32767'.");
                _counter++;
            }
        }
        ConfirmInt64OneDecrementUnderInt16MinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneDecrementUnderInt16MinValueCastToUInt64Overflows()
        {
            long from = -32769;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-32769)' did not throw OverflowException.");
        }
        ConfirmInt64OneIncrementAboveInt16MinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneIncrementAboveInt16MinValueCastToUInt64Overflows()
        {
            long from = -32767;
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
            long integerOneDecrementUnderInt16MaxValue = 32766;

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
            long integerOneIncrementAboveInt16MaxValue = 32768;

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
            long uInt16MaxValue = 65535;

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
            long integerOneDecrementUnderUInt16MaxValue = 65534;

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
            long integerOneIncrementAboveUInt16MaxValue = 65536;

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
            long from = -2147483648;
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
            long int32MaxValue = 2147483647;

            if (BreakUpFlow())
                return;

            if (checked((ulong)int32MaxValue) != 2147483647)
            {
                Console.WriteLine($"'(ulong)2147483647' was evaluted to '{(ulong)int32MaxValue}'. Expected: '2147483647'.");
                _counter++;
            }
        }
        ConfirmInt64OneDecrementUnderInt32MinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneDecrementUnderInt32MinValueCastToUInt64Overflows()
        {
            long from = -2147483649;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-2147483649)' did not throw OverflowException.");
        }
        ConfirmInt64OneIncrementAboveInt32MinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneIncrementAboveInt32MinValueCastToUInt64Overflows()
        {
            long from = -2147483647;
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
            long integerOneDecrementUnderInt32MaxValue = 2147483646;

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
            long integerOneIncrementAboveInt32MaxValue = 2147483648;

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
            long uInt32MaxValue = 4294967295;

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
            long integerOneDecrementUnderUInt32MaxValue = 4294967294;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneDecrementUnderUInt32MaxValue) != 4294967294)
            {
                Console.WriteLine($"'(ulong)4294967294' was evaluted to '{(ulong)integerOneDecrementUnderUInt32MaxValue}'. Expected: '4294967294'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveUInt32MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt32MaxValueCastToUInt64IsFoldedCorrectly()
        {
            long integerOneIncrementAboveUInt32MaxValue = 4294967296;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneIncrementAboveUInt32MaxValue) != 4294967296)
            {
                Console.WriteLine($"'(ulong)4294967296' was evaluted to '{(ulong)integerOneIncrementAboveUInt32MaxValue}'. Expected: '4294967296'.");
                _counter++;
            }
        }
        ConfirmInt64MinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MinValueCastToUInt64Overflows()
        {
            long from = -9223372036854775808;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-9223372036854775808)' did not throw OverflowException.");
        }
        ConfirmInt64MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MaxValueCastToUInt64IsFoldedCorrectly()
        {
            long int64MaxValue = 9223372036854775807;

            if (BreakUpFlow())
                return;

            if (checked((ulong)int64MaxValue) != 9223372036854775807)
            {
                Console.WriteLine($"'(ulong)9223372036854775807' was evaluted to '{(ulong)int64MaxValue}'. Expected: '9223372036854775807'.");
                _counter++;
            }
        }
        ConfirmInt64OneIncrementAboveInt64MinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64OneIncrementAboveInt64MinValueCastToUInt64Overflows()
        {
            long from = -9223372036854775807;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-9223372036854775807)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt64MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt64MaxValueCastToUInt64IsFoldedCorrectly()
        {
            long integerOneDecrementUnderInt64MaxValue = 9223372036854775806;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneDecrementUnderInt64MaxValue) != 9223372036854775806)
            {
                Console.WriteLine($"'(ulong)9223372036854775806' was evaluted to '{(ulong)integerOneDecrementUnderInt64MaxValue}'. Expected: '9223372036854775806'.");
                _counter++;
            }
        }
    }

    private static void TestCastingUInt64ToSingle()
    {
        ConfirmIntegerZeroCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToSingleIsFoldedCorrectly()
        {
            ulong integerZero = 0;

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
            ulong sByteMaxValue = 127;

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
            ulong integerOneDecrementUnderSByteMaxValue = 126;

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
            ulong integerOneIncrementAboveSByteMaxValue = 128;

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
            ulong byteMaxValue = 255;

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
            ulong integerOneIncrementAboveByteMinValue = 1;

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
            ulong integerOneDecrementUnderByteMaxValue = 254;

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
            ulong integerOneIncrementAboveByteMaxValue = 256;

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
            ulong int16MaxValue = 32767;

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
            ulong integerOneDecrementUnderInt16MaxValue = 32766;

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
            ulong integerOneIncrementAboveInt16MaxValue = 32768;

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
            ulong uInt16MaxValue = 65535;

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
            ulong integerOneDecrementUnderUInt16MaxValue = 65534;

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
            ulong integerOneIncrementAboveUInt16MaxValue = 65536;

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
            ulong int32MaxValue = 2147483647;

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
            ulong integerOneDecrementUnderInt32MaxValue = 2147483646;

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
            ulong integerOneIncrementAboveInt32MaxValue = 2147483648;

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
            ulong uInt32MaxValue = 4294967295;

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
            ulong integerOneDecrementUnderUInt32MaxValue = 4294967294;

            if (BreakUpFlow())
                return;

            if (checked((float)integerOneDecrementUnderUInt32MaxValue) != 4.2949673E+09f)
            {
                Console.WriteLine($"'(float)4294967294' was evaluted to '{(float)integerOneDecrementUnderUInt32MaxValue}'. Expected: '4.2949673E+09f'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveUInt32MaxValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt32MaxValueCastToSingleIsFoldedCorrectly()
        {
            ulong integerOneIncrementAboveUInt32MaxValue = 4294967296;

            if (BreakUpFlow())
                return;

            if (checked((float)integerOneIncrementAboveUInt32MaxValue) != 4.2949673E+09f)
            {
                Console.WriteLine($"'(float)4294967296' was evaluted to '{(float)integerOneIncrementAboveUInt32MaxValue}'. Expected: '4.2949673E+09f'.");
                _counter++;
            }
        }
        ConfirmInt64MaxValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MaxValueCastToSingleIsFoldedCorrectly()
        {
            ulong int64MaxValue = 9223372036854775807;

            if (BreakUpFlow())
                return;

            if (checked((float)int64MaxValue) != 9.223372E+18f)
            {
                Console.WriteLine($"'(float)9223372036854775807' was evaluted to '{(float)int64MaxValue}'. Expected: '9.223372E+18f'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt64MaxValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt64MaxValueCastToSingleIsFoldedCorrectly()
        {
            ulong integerOneDecrementUnderInt64MaxValue = 9223372036854775806;

            if (BreakUpFlow())
                return;

            if (checked((float)integerOneDecrementUnderInt64MaxValue) != 9.223372E+18f)
            {
                Console.WriteLine($"'(float)9223372036854775806' was evaluted to '{(float)integerOneDecrementUnderInt64MaxValue}'. Expected: '9.223372E+18f'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt64MaxValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt64MaxValueCastToSingleIsFoldedCorrectly()
        {
            ulong integerOneIncrementAboveInt64MaxValue = 9223372036854775808;

            if (BreakUpFlow())
                return;

            if (checked((float)integerOneIncrementAboveInt64MaxValue) != 9.223372E+18f)
            {
                Console.WriteLine($"'(float)9223372036854775808' was evaluted to '{(float)integerOneIncrementAboveInt64MaxValue}'. Expected: '9.223372E+18f'.");
                _counter++;
            }
        }
        ConfirmUInt64MaxValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64MaxValueCastToSingleIsFoldedCorrectly()
        {
            ulong uInt64MaxValue = 18446744073709551615;

            if (BreakUpFlow())
                return;

            if (checked((float)uInt64MaxValue) != 1.8446744E+19f)
            {
                Console.WriteLine($"'(float)18446744073709551615' was evaluted to '{(float)uInt64MaxValue}'. Expected: '1.8446744E+19f'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderUInt64MaxValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt64MaxValueCastToSingleIsFoldedCorrectly()
        {
            ulong integerOneDecrementUnderUInt64MaxValue = 18446744073709551614;

            if (BreakUpFlow())
                return;

            if (checked((float)integerOneDecrementUnderUInt64MaxValue) != 1.8446744E+19f)
            {
                Console.WriteLine($"'(float)18446744073709551614' was evaluted to '{(float)integerOneDecrementUnderUInt64MaxValue}'. Expected: '1.8446744E+19f'.");
                _counter++;
            }
        }
    }

    private static void TestCastingUInt64ToDouble()
    {
        ConfirmIntegerZeroCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToDoubleIsFoldedCorrectly()
        {
            ulong integerZero = 0;

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
            ulong sByteMaxValue = 127;

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
            ulong integerOneDecrementUnderSByteMaxValue = 126;

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
            ulong integerOneIncrementAboveSByteMaxValue = 128;

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
            ulong byteMaxValue = 255;

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
            ulong integerOneIncrementAboveByteMinValue = 1;

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
            ulong integerOneDecrementUnderByteMaxValue = 254;

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
            ulong integerOneIncrementAboveByteMaxValue = 256;

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
            ulong int16MaxValue = 32767;

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
            ulong integerOneDecrementUnderInt16MaxValue = 32766;

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
            ulong integerOneIncrementAboveInt16MaxValue = 32768;

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
            ulong uInt16MaxValue = 65535;

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
            ulong integerOneDecrementUnderUInt16MaxValue = 65534;

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
            ulong integerOneIncrementAboveUInt16MaxValue = 65536;

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
            ulong int32MaxValue = 2147483647;

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
            ulong integerOneDecrementUnderInt32MaxValue = 2147483646;

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
            ulong integerOneIncrementAboveInt32MaxValue = 2147483648;

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
            ulong uInt32MaxValue = 4294967295;

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
            ulong integerOneDecrementUnderUInt32MaxValue = 4294967294;

            if (BreakUpFlow())
                return;

            if (checked((double)integerOneDecrementUnderUInt32MaxValue) != 4294967294d)
            {
                Console.WriteLine($"'(double)4294967294' was evaluted to '{(double)integerOneDecrementUnderUInt32MaxValue}'. Expected: '4294967294d'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveUInt32MaxValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt32MaxValueCastToDoubleIsFoldedCorrectly()
        {
            ulong integerOneIncrementAboveUInt32MaxValue = 4294967296;

            if (BreakUpFlow())
                return;

            if (checked((double)integerOneIncrementAboveUInt32MaxValue) != 4294967296d)
            {
                Console.WriteLine($"'(double)4294967296' was evaluted to '{(double)integerOneIncrementAboveUInt32MaxValue}'. Expected: '4294967296d'.");
                _counter++;
            }
        }
        ConfirmInt64MaxValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MaxValueCastToDoubleIsFoldedCorrectly()
        {
            ulong int64MaxValue = 9223372036854775807;

            if (BreakUpFlow())
                return;

            if (checked((double)int64MaxValue) != 9.223372036854776E+18d)
            {
                Console.WriteLine($"'(double)9223372036854775807' was evaluted to '{(double)int64MaxValue}'. Expected: '9.223372036854776E+18d'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt64MaxValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt64MaxValueCastToDoubleIsFoldedCorrectly()
        {
            ulong integerOneDecrementUnderInt64MaxValue = 9223372036854775806;

            if (BreakUpFlow())
                return;

            if (checked((double)integerOneDecrementUnderInt64MaxValue) != 9.223372036854776E+18d)
            {
                Console.WriteLine($"'(double)9223372036854775806' was evaluted to '{(double)integerOneDecrementUnderInt64MaxValue}'. Expected: '9.223372036854776E+18d'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt64MaxValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt64MaxValueCastToDoubleIsFoldedCorrectly()
        {
            ulong integerOneIncrementAboveInt64MaxValue = 9223372036854775808;

            if (BreakUpFlow())
                return;

            if (checked((double)integerOneIncrementAboveInt64MaxValue) != 9.223372036854776E+18d)
            {
                Console.WriteLine($"'(double)9223372036854775808' was evaluted to '{(double)integerOneIncrementAboveInt64MaxValue}'. Expected: '9.223372036854776E+18d'.");
                _counter++;
            }
        }
        ConfirmUInt64MaxValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64MaxValueCastToDoubleIsFoldedCorrectly()
        {
            ulong uInt64MaxValue = 18446744073709551615;

            if (BreakUpFlow())
                return;

            if (checked((double)uInt64MaxValue) != 1.8446744073709552E+19d)
            {
                Console.WriteLine($"'(double)18446744073709551615' was evaluted to '{(double)uInt64MaxValue}'. Expected: '1.8446744073709552E+19d'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderUInt64MaxValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt64MaxValueCastToDoubleIsFoldedCorrectly()
        {
            ulong integerOneDecrementUnderUInt64MaxValue = 18446744073709551614;

            if (BreakUpFlow())
                return;

            if (checked((double)integerOneDecrementUnderUInt64MaxValue) != 1.8446744073709552E+19d)
            {
                Console.WriteLine($"'(double)18446744073709551614' was evaluted to '{(double)integerOneDecrementUnderUInt64MaxValue}'. Expected: '1.8446744073709552E+19d'.");
                _counter++;
            }
        }
    }

    private static void TestCastingUInt64ToSByte()
    {
        ConfirmIntegerZeroCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToSByteIsFoldedCorrectly()
        {
            ulong integerZero = 0;

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
            ulong sByteMaxValue = 127;

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
            ulong integerOneDecrementUnderSByteMaxValue = 126;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(sbyte)126' was evaluted to '{(sbyte)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmUInt64OneIncrementAboveSByteMaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneIncrementAboveSByteMaxValueCastToSByteOverflows()
        {
            ulong from = 128;
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
            ulong from = 255;
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
            ulong integerOneIncrementAboveByteMinValue = 1;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(sbyte)1' was evaluted to '{(sbyte)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmUInt64OneDecrementUnderByteMaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneDecrementUnderByteMaxValueCastToSByteOverflows()
        {
            ulong from = 254;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)254)' did not throw OverflowException.");
        }
        ConfirmUInt64OneIncrementAboveByteMaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneIncrementAboveByteMaxValueCastToSByteOverflows()
        {
            ulong from = 256;
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
            ulong from = 32767;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)32767)' did not throw OverflowException.");
        }
        ConfirmUInt64OneDecrementUnderInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneDecrementUnderInt16MaxValueCastToSByteOverflows()
        {
            ulong from = 32766;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)32766)' did not throw OverflowException.");
        }
        ConfirmUInt64OneIncrementAboveInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneIncrementAboveInt16MaxValueCastToSByteOverflows()
        {
            ulong from = 32768;
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
            ulong from = 65535;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)65535)' did not throw OverflowException.");
        }
        ConfirmUInt64OneDecrementUnderUInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneDecrementUnderUInt16MaxValueCastToSByteOverflows()
        {
            ulong from = 65534;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)65534)' did not throw OverflowException.");
        }
        ConfirmUInt64OneIncrementAboveUInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneIncrementAboveUInt16MaxValueCastToSByteOverflows()
        {
            ulong from = 65536;
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
            ulong from = 2147483647;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)2147483647)' did not throw OverflowException.");
        }
        ConfirmUInt64OneDecrementUnderInt32MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneDecrementUnderInt32MaxValueCastToSByteOverflows()
        {
            ulong from = 2147483646;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)2147483646)' did not throw OverflowException.");
        }
        ConfirmUInt64OneIncrementAboveInt32MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneIncrementAboveInt32MaxValueCastToSByteOverflows()
        {
            ulong from = 2147483648;
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
            ulong from = 4294967295;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)4294967295)' did not throw OverflowException.");
        }
        ConfirmUInt64OneDecrementUnderUInt32MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneDecrementUnderUInt32MaxValueCastToSByteOverflows()
        {
            ulong from = 4294967294;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)4294967294)' did not throw OverflowException.");
        }
        ConfirmUInt64OneIncrementAboveUInt32MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneIncrementAboveUInt32MaxValueCastToSByteOverflows()
        {
            ulong from = 4294967296;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)4294967296)' did not throw OverflowException.");
        }
        ConfirmInt64MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MaxValueCastToSByteOverflows()
        {
            ulong from = 9223372036854775807;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)9223372036854775807)' did not throw OverflowException.");
        }
        ConfirmUInt64OneDecrementUnderInt64MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneDecrementUnderInt64MaxValueCastToSByteOverflows()
        {
            ulong from = 9223372036854775806;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)9223372036854775806)' did not throw OverflowException.");
        }
        ConfirmUInt64OneIncrementAboveInt64MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneIncrementAboveInt64MaxValueCastToSByteOverflows()
        {
            ulong from = 9223372036854775808;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)9223372036854775808)' did not throw OverflowException.");
        }
        ConfirmUInt64MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64MaxValueCastToSByteOverflows()
        {
            ulong from = 18446744073709551615;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)18446744073709551615)' did not throw OverflowException.");
        }
        ConfirmUInt64OneDecrementUnderUInt64MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneDecrementUnderUInt64MaxValueCastToSByteOverflows()
        {
            ulong from = 18446744073709551614;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)18446744073709551614)' did not throw OverflowException.");
        }
    }

    private static void TestCastingUInt64ToByte()
    {
        ConfirmIntegerZeroCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToByteIsFoldedCorrectly()
        {
            ulong integerZero = 0;

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
            ulong sByteMaxValue = 127;

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
            ulong integerOneDecrementUnderSByteMaxValue = 126;

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
            ulong integerOneIncrementAboveSByteMaxValue = 128;

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
            ulong byteMaxValue = 255;

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
            ulong integerOneIncrementAboveByteMinValue = 1;

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
            ulong integerOneDecrementUnderByteMaxValue = 254;

            if (BreakUpFlow())
                return;

            if (checked((byte)integerOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(byte)254' was evaluted to '{(byte)integerOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmUInt64OneIncrementAboveByteMaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneIncrementAboveByteMaxValueCastToByteOverflows()
        {
            ulong from = 256;
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
            ulong from = 32767;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)32767)' did not throw OverflowException.");
        }
        ConfirmUInt64OneDecrementUnderInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneDecrementUnderInt16MaxValueCastToByteOverflows()
        {
            ulong from = 32766;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)32766)' did not throw OverflowException.");
        }
        ConfirmUInt64OneIncrementAboveInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneIncrementAboveInt16MaxValueCastToByteOverflows()
        {
            ulong from = 32768;
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
            ulong from = 65535;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)65535)' did not throw OverflowException.");
        }
        ConfirmUInt64OneDecrementUnderUInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneDecrementUnderUInt16MaxValueCastToByteOverflows()
        {
            ulong from = 65534;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)65534)' did not throw OverflowException.");
        }
        ConfirmUInt64OneIncrementAboveUInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneIncrementAboveUInt16MaxValueCastToByteOverflows()
        {
            ulong from = 65536;
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
            ulong from = 2147483647;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)2147483647)' did not throw OverflowException.");
        }
        ConfirmUInt64OneDecrementUnderInt32MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneDecrementUnderInt32MaxValueCastToByteOverflows()
        {
            ulong from = 2147483646;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)2147483646)' did not throw OverflowException.");
        }
        ConfirmUInt64OneIncrementAboveInt32MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneIncrementAboveInt32MaxValueCastToByteOverflows()
        {
            ulong from = 2147483648;
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
            ulong from = 4294967295;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)4294967295)' did not throw OverflowException.");
        }
        ConfirmUInt64OneDecrementUnderUInt32MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneDecrementUnderUInt32MaxValueCastToByteOverflows()
        {
            ulong from = 4294967294;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)4294967294)' did not throw OverflowException.");
        }
        ConfirmUInt64OneIncrementAboveUInt32MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneIncrementAboveUInt32MaxValueCastToByteOverflows()
        {
            ulong from = 4294967296;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)4294967296)' did not throw OverflowException.");
        }
        ConfirmInt64MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MaxValueCastToByteOverflows()
        {
            ulong from = 9223372036854775807;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)9223372036854775807)' did not throw OverflowException.");
        }
        ConfirmUInt64OneDecrementUnderInt64MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneDecrementUnderInt64MaxValueCastToByteOverflows()
        {
            ulong from = 9223372036854775806;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)9223372036854775806)' did not throw OverflowException.");
        }
        ConfirmUInt64OneIncrementAboveInt64MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneIncrementAboveInt64MaxValueCastToByteOverflows()
        {
            ulong from = 9223372036854775808;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)9223372036854775808)' did not throw OverflowException.");
        }
        ConfirmUInt64MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64MaxValueCastToByteOverflows()
        {
            ulong from = 18446744073709551615;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)18446744073709551615)' did not throw OverflowException.");
        }
        ConfirmUInt64OneDecrementUnderUInt64MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneDecrementUnderUInt64MaxValueCastToByteOverflows()
        {
            ulong from = 18446744073709551614;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)18446744073709551614)' did not throw OverflowException.");
        }
    }

    private static void TestCastingUInt64ToInt16()
    {
        ConfirmIntegerZeroCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToInt16IsFoldedCorrectly()
        {
            ulong integerZero = 0;

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
            ulong sByteMaxValue = 127;

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
            ulong integerOneDecrementUnderSByteMaxValue = 126;

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
            ulong integerOneIncrementAboveSByteMaxValue = 128;

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
            ulong byteMaxValue = 255;

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
            ulong integerOneIncrementAboveByteMinValue = 1;

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
            ulong integerOneDecrementUnderByteMaxValue = 254;

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
            ulong integerOneIncrementAboveByteMaxValue = 256;

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
            ulong int16MaxValue = 32767;

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
            ulong integerOneDecrementUnderInt16MaxValue = 32766;

            if (BreakUpFlow())
                return;

            if (checked((short)integerOneDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(short)32766' was evaluted to '{(short)integerOneDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
        ConfirmUInt64OneIncrementAboveInt16MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneIncrementAboveInt16MaxValueCastToInt16Overflows()
        {
            ulong from = 32768;
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
            ulong from = 65535;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)65535)' did not throw OverflowException.");
        }
        ConfirmUInt64OneDecrementUnderUInt16MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneDecrementUnderUInt16MaxValueCastToInt16Overflows()
        {
            ulong from = 65534;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)65534)' did not throw OverflowException.");
        }
        ConfirmUInt64OneIncrementAboveUInt16MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneIncrementAboveUInt16MaxValueCastToInt16Overflows()
        {
            ulong from = 65536;
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
            ulong from = 2147483647;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)2147483647)' did not throw OverflowException.");
        }
        ConfirmUInt64OneDecrementUnderInt32MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneDecrementUnderInt32MaxValueCastToInt16Overflows()
        {
            ulong from = 2147483646;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)2147483646)' did not throw OverflowException.");
        }
        ConfirmUInt64OneIncrementAboveInt32MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneIncrementAboveInt32MaxValueCastToInt16Overflows()
        {
            ulong from = 2147483648;
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
            ulong from = 4294967295;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)4294967295)' did not throw OverflowException.");
        }
        ConfirmUInt64OneDecrementUnderUInt32MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneDecrementUnderUInt32MaxValueCastToInt16Overflows()
        {
            ulong from = 4294967294;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)4294967294)' did not throw OverflowException.");
        }
        ConfirmUInt64OneIncrementAboveUInt32MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneIncrementAboveUInt32MaxValueCastToInt16Overflows()
        {
            ulong from = 4294967296;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)4294967296)' did not throw OverflowException.");
        }
        ConfirmInt64MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MaxValueCastToInt16Overflows()
        {
            ulong from = 9223372036854775807;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)9223372036854775807)' did not throw OverflowException.");
        }
        ConfirmUInt64OneDecrementUnderInt64MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneDecrementUnderInt64MaxValueCastToInt16Overflows()
        {
            ulong from = 9223372036854775806;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)9223372036854775806)' did not throw OverflowException.");
        }
        ConfirmUInt64OneIncrementAboveInt64MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneIncrementAboveInt64MaxValueCastToInt16Overflows()
        {
            ulong from = 9223372036854775808;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)9223372036854775808)' did not throw OverflowException.");
        }
        ConfirmUInt64MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64MaxValueCastToInt16Overflows()
        {
            ulong from = 18446744073709551615;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)18446744073709551615)' did not throw OverflowException.");
        }
        ConfirmUInt64OneDecrementUnderUInt64MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneDecrementUnderUInt64MaxValueCastToInt16Overflows()
        {
            ulong from = 18446744073709551614;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)18446744073709551614)' did not throw OverflowException.");
        }
    }

    private static void TestCastingUInt64ToUInt16()
    {
        ConfirmIntegerZeroCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToUInt16IsFoldedCorrectly()
        {
            ulong integerZero = 0;

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
            ulong sByteMaxValue = 127;

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
            ulong integerOneDecrementUnderSByteMaxValue = 126;

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
            ulong integerOneIncrementAboveSByteMaxValue = 128;

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
            ulong byteMaxValue = 255;

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
            ulong integerOneIncrementAboveByteMinValue = 1;

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
            ulong integerOneDecrementUnderByteMaxValue = 254;

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
            ulong integerOneIncrementAboveByteMaxValue = 256;

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
            ulong int16MaxValue = 32767;

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
            ulong integerOneDecrementUnderInt16MaxValue = 32766;

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
            ulong integerOneIncrementAboveInt16MaxValue = 32768;

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
            ulong uInt16MaxValue = 65535;

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
            ulong integerOneDecrementUnderUInt16MaxValue = 65534;

            if (BreakUpFlow())
                return;

            if (checked((ushort)integerOneDecrementUnderUInt16MaxValue) != 65534)
            {
                Console.WriteLine($"'(ushort)65534' was evaluted to '{(ushort)integerOneDecrementUnderUInt16MaxValue}'. Expected: '65534'.");
                _counter++;
            }
        }
        ConfirmUInt64OneIncrementAboveUInt16MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneIncrementAboveUInt16MaxValueCastToUInt16Overflows()
        {
            ulong from = 65536;
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
            ulong from = 2147483647;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)2147483647)' did not throw OverflowException.");
        }
        ConfirmUInt64OneDecrementUnderInt32MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneDecrementUnderInt32MaxValueCastToUInt16Overflows()
        {
            ulong from = 2147483646;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)2147483646)' did not throw OverflowException.");
        }
        ConfirmUInt64OneIncrementAboveInt32MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneIncrementAboveInt32MaxValueCastToUInt16Overflows()
        {
            ulong from = 2147483648;
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
            ulong from = 4294967295;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)4294967295)' did not throw OverflowException.");
        }
        ConfirmUInt64OneDecrementUnderUInt32MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneDecrementUnderUInt32MaxValueCastToUInt16Overflows()
        {
            ulong from = 4294967294;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)4294967294)' did not throw OverflowException.");
        }
        ConfirmUInt64OneIncrementAboveUInt32MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneIncrementAboveUInt32MaxValueCastToUInt16Overflows()
        {
            ulong from = 4294967296;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)4294967296)' did not throw OverflowException.");
        }
        ConfirmInt64MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MaxValueCastToUInt16Overflows()
        {
            ulong from = 9223372036854775807;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)9223372036854775807)' did not throw OverflowException.");
        }
        ConfirmUInt64OneDecrementUnderInt64MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneDecrementUnderInt64MaxValueCastToUInt16Overflows()
        {
            ulong from = 9223372036854775806;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)9223372036854775806)' did not throw OverflowException.");
        }
        ConfirmUInt64OneIncrementAboveInt64MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneIncrementAboveInt64MaxValueCastToUInt16Overflows()
        {
            ulong from = 9223372036854775808;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)9223372036854775808)' did not throw OverflowException.");
        }
        ConfirmUInt64MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64MaxValueCastToUInt16Overflows()
        {
            ulong from = 18446744073709551615;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)18446744073709551615)' did not throw OverflowException.");
        }
        ConfirmUInt64OneDecrementUnderUInt64MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneDecrementUnderUInt64MaxValueCastToUInt16Overflows()
        {
            ulong from = 18446744073709551614;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)18446744073709551614)' did not throw OverflowException.");
        }
    }

    private static void TestCastingUInt64ToInt32()
    {
        ConfirmIntegerZeroCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToInt32IsFoldedCorrectly()
        {
            ulong integerZero = 0;

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
            ulong sByteMaxValue = 127;

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
            ulong integerOneDecrementUnderSByteMaxValue = 126;

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
            ulong integerOneIncrementAboveSByteMaxValue = 128;

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
            ulong byteMaxValue = 255;

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
            ulong integerOneIncrementAboveByteMinValue = 1;

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
            ulong integerOneDecrementUnderByteMaxValue = 254;

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
            ulong integerOneIncrementAboveByteMaxValue = 256;

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
            ulong int16MaxValue = 32767;

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
            ulong integerOneDecrementUnderInt16MaxValue = 32766;

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
            ulong integerOneIncrementAboveInt16MaxValue = 32768;

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
            ulong uInt16MaxValue = 65535;

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
            ulong integerOneDecrementUnderUInt16MaxValue = 65534;

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
            ulong integerOneIncrementAboveUInt16MaxValue = 65536;

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
            ulong int32MaxValue = 2147483647;

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
            ulong integerOneDecrementUnderInt32MaxValue = 2147483646;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneDecrementUnderInt32MaxValue) != 2147483646)
            {
                Console.WriteLine($"'(int)2147483646' was evaluted to '{(int)integerOneDecrementUnderInt32MaxValue}'. Expected: '2147483646'.");
                _counter++;
            }
        }
        ConfirmUInt64OneIncrementAboveInt32MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneIncrementAboveInt32MaxValueCastToInt32Overflows()
        {
            ulong from = 2147483648;
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
            ulong from = 4294967295;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)4294967295)' did not throw OverflowException.");
        }
        ConfirmUInt64OneDecrementUnderUInt32MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneDecrementUnderUInt32MaxValueCastToInt32Overflows()
        {
            ulong from = 4294967294;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)4294967294)' did not throw OverflowException.");
        }
        ConfirmUInt64OneIncrementAboveUInt32MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneIncrementAboveUInt32MaxValueCastToInt32Overflows()
        {
            ulong from = 4294967296;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)4294967296)' did not throw OverflowException.");
        }
        ConfirmInt64MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MaxValueCastToInt32Overflows()
        {
            ulong from = 9223372036854775807;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)9223372036854775807)' did not throw OverflowException.");
        }
        ConfirmUInt64OneDecrementUnderInt64MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneDecrementUnderInt64MaxValueCastToInt32Overflows()
        {
            ulong from = 9223372036854775806;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)9223372036854775806)' did not throw OverflowException.");
        }
        ConfirmUInt64OneIncrementAboveInt64MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneIncrementAboveInt64MaxValueCastToInt32Overflows()
        {
            ulong from = 9223372036854775808;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)9223372036854775808)' did not throw OverflowException.");
        }
        ConfirmUInt64MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64MaxValueCastToInt32Overflows()
        {
            ulong from = 18446744073709551615;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)18446744073709551615)' did not throw OverflowException.");
        }
        ConfirmUInt64OneDecrementUnderUInt64MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneDecrementUnderUInt64MaxValueCastToInt32Overflows()
        {
            ulong from = 18446744073709551614;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)18446744073709551614)' did not throw OverflowException.");
        }
    }

    private static void TestCastingUInt64ToUInt32()
    {
        ConfirmIntegerZeroCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToUInt32IsFoldedCorrectly()
        {
            ulong integerZero = 0;

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
            ulong sByteMaxValue = 127;

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
            ulong integerOneDecrementUnderSByteMaxValue = 126;

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
            ulong integerOneIncrementAboveSByteMaxValue = 128;

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
            ulong byteMaxValue = 255;

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
            ulong integerOneIncrementAboveByteMinValue = 1;

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
            ulong integerOneDecrementUnderByteMaxValue = 254;

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
            ulong integerOneIncrementAboveByteMaxValue = 256;

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
            ulong int16MaxValue = 32767;

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
            ulong integerOneDecrementUnderInt16MaxValue = 32766;

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
            ulong integerOneIncrementAboveInt16MaxValue = 32768;

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
            ulong uInt16MaxValue = 65535;

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
            ulong integerOneDecrementUnderUInt16MaxValue = 65534;

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
            ulong integerOneIncrementAboveUInt16MaxValue = 65536;

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
            ulong int32MaxValue = 2147483647;

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
            ulong integerOneDecrementUnderInt32MaxValue = 2147483646;

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
            ulong integerOneIncrementAboveInt32MaxValue = 2147483648;

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
            ulong uInt32MaxValue = 4294967295;

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
            ulong integerOneDecrementUnderUInt32MaxValue = 4294967294;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneDecrementUnderUInt32MaxValue) != 4294967294)
            {
                Console.WriteLine($"'(uint)4294967294' was evaluted to '{(uint)integerOneDecrementUnderUInt32MaxValue}'. Expected: '4294967294'.");
                _counter++;
            }
        }
        ConfirmUInt64OneIncrementAboveUInt32MaxValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneIncrementAboveUInt32MaxValueCastToUInt32Overflows()
        {
            ulong from = 4294967296;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)4294967296)' did not throw OverflowException.");
        }
        ConfirmInt64MaxValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MaxValueCastToUInt32Overflows()
        {
            ulong from = 9223372036854775807;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)9223372036854775807)' did not throw OverflowException.");
        }
        ConfirmUInt64OneDecrementUnderInt64MaxValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneDecrementUnderInt64MaxValueCastToUInt32Overflows()
        {
            ulong from = 9223372036854775806;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)9223372036854775806)' did not throw OverflowException.");
        }
        ConfirmUInt64OneIncrementAboveInt64MaxValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneIncrementAboveInt64MaxValueCastToUInt32Overflows()
        {
            ulong from = 9223372036854775808;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)9223372036854775808)' did not throw OverflowException.");
        }
        ConfirmUInt64MaxValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64MaxValueCastToUInt32Overflows()
        {
            ulong from = 18446744073709551615;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)18446744073709551615)' did not throw OverflowException.");
        }
        ConfirmUInt64OneDecrementUnderUInt64MaxValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneDecrementUnderUInt64MaxValueCastToUInt32Overflows()
        {
            ulong from = 18446744073709551614;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)18446744073709551614)' did not throw OverflowException.");
        }
    }

    private static void TestCastingUInt64ToInt64()
    {
        ConfirmIntegerZeroCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToInt64IsFoldedCorrectly()
        {
            ulong integerZero = 0;

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
            ulong sByteMaxValue = 127;

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
            ulong integerOneDecrementUnderSByteMaxValue = 126;

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
            ulong integerOneIncrementAboveSByteMaxValue = 128;

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
            ulong byteMaxValue = 255;

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
            ulong integerOneIncrementAboveByteMinValue = 1;

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
            ulong integerOneDecrementUnderByteMaxValue = 254;

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
            ulong integerOneIncrementAboveByteMaxValue = 256;

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
            ulong int16MaxValue = 32767;

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
            ulong integerOneDecrementUnderInt16MaxValue = 32766;

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
            ulong integerOneIncrementAboveInt16MaxValue = 32768;

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
            ulong uInt16MaxValue = 65535;

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
            ulong integerOneDecrementUnderUInt16MaxValue = 65534;

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
            ulong integerOneIncrementAboveUInt16MaxValue = 65536;

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
            ulong int32MaxValue = 2147483647;

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
            ulong integerOneDecrementUnderInt32MaxValue = 2147483646;

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
            ulong integerOneIncrementAboveInt32MaxValue = 2147483648;

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
            ulong uInt32MaxValue = 4294967295;

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
            ulong integerOneDecrementUnderUInt32MaxValue = 4294967294;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneDecrementUnderUInt32MaxValue) != 4294967294)
            {
                Console.WriteLine($"'(long)4294967294' was evaluted to '{(long)integerOneDecrementUnderUInt32MaxValue}'. Expected: '4294967294'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveUInt32MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt32MaxValueCastToInt64IsFoldedCorrectly()
        {
            ulong integerOneIncrementAboveUInt32MaxValue = 4294967296;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneIncrementAboveUInt32MaxValue) != 4294967296)
            {
                Console.WriteLine($"'(long)4294967296' was evaluted to '{(long)integerOneIncrementAboveUInt32MaxValue}'. Expected: '4294967296'.");
                _counter++;
            }
        }
        ConfirmInt64MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MaxValueCastToInt64IsFoldedCorrectly()
        {
            ulong int64MaxValue = 9223372036854775807;

            if (BreakUpFlow())
                return;

            if (checked((long)int64MaxValue) != 9223372036854775807)
            {
                Console.WriteLine($"'(long)9223372036854775807' was evaluted to '{(long)int64MaxValue}'. Expected: '9223372036854775807'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt64MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt64MaxValueCastToInt64IsFoldedCorrectly()
        {
            ulong integerOneDecrementUnderInt64MaxValue = 9223372036854775806;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneDecrementUnderInt64MaxValue) != 9223372036854775806)
            {
                Console.WriteLine($"'(long)9223372036854775806' was evaluted to '{(long)integerOneDecrementUnderInt64MaxValue}'. Expected: '9223372036854775806'.");
                _counter++;
            }
        }
        ConfirmUInt64OneIncrementAboveInt64MaxValueCastToInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneIncrementAboveInt64MaxValueCastToInt64Overflows()
        {
            ulong from = 9223372036854775808;
            _counter++;
            try
            {
                _ = checked((long)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((long)9223372036854775808)' did not throw OverflowException.");
        }
        ConfirmUInt64MaxValueCastToInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64MaxValueCastToInt64Overflows()
        {
            ulong from = 18446744073709551615;
            _counter++;
            try
            {
                _ = checked((long)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((long)18446744073709551615)' did not throw OverflowException.");
        }
        ConfirmUInt64OneDecrementUnderUInt64MaxValueCastToInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64OneDecrementUnderUInt64MaxValueCastToInt64Overflows()
        {
            ulong from = 18446744073709551614;
            _counter++;
            try
            {
                _ = checked((long)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((long)18446744073709551614)' did not throw OverflowException.");
        }
    }

    private static void TestCastingUInt64ToUInt64()
    {
        ConfirmIntegerZeroCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToUInt64IsFoldedCorrectly()
        {
            ulong integerZero = 0;

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
            ulong sByteMaxValue = 127;

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
            ulong integerOneDecrementUnderSByteMaxValue = 126;

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
            ulong integerOneIncrementAboveSByteMaxValue = 128;

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
            ulong byteMaxValue = 255;

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
            ulong integerOneIncrementAboveByteMinValue = 1;

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
            ulong integerOneDecrementUnderByteMaxValue = 254;

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
            ulong integerOneIncrementAboveByteMaxValue = 256;

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
            ulong int16MaxValue = 32767;

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
            ulong integerOneDecrementUnderInt16MaxValue = 32766;

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
            ulong integerOneIncrementAboveInt16MaxValue = 32768;

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
            ulong uInt16MaxValue = 65535;

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
            ulong integerOneDecrementUnderUInt16MaxValue = 65534;

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
            ulong integerOneIncrementAboveUInt16MaxValue = 65536;

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
            ulong int32MaxValue = 2147483647;

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
            ulong integerOneDecrementUnderInt32MaxValue = 2147483646;

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
            ulong integerOneIncrementAboveInt32MaxValue = 2147483648;

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
            ulong uInt32MaxValue = 4294967295;

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
            ulong integerOneDecrementUnderUInt32MaxValue = 4294967294;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneDecrementUnderUInt32MaxValue) != 4294967294)
            {
                Console.WriteLine($"'(ulong)4294967294' was evaluted to '{(ulong)integerOneDecrementUnderUInt32MaxValue}'. Expected: '4294967294'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveUInt32MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt32MaxValueCastToUInt64IsFoldedCorrectly()
        {
            ulong integerOneIncrementAboveUInt32MaxValue = 4294967296;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneIncrementAboveUInt32MaxValue) != 4294967296)
            {
                Console.WriteLine($"'(ulong)4294967296' was evaluted to '{(ulong)integerOneIncrementAboveUInt32MaxValue}'. Expected: '4294967296'.");
                _counter++;
            }
        }
        ConfirmInt64MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MaxValueCastToUInt64IsFoldedCorrectly()
        {
            ulong int64MaxValue = 9223372036854775807;

            if (BreakUpFlow())
                return;

            if (checked((ulong)int64MaxValue) != 9223372036854775807)
            {
                Console.WriteLine($"'(ulong)9223372036854775807' was evaluted to '{(ulong)int64MaxValue}'. Expected: '9223372036854775807'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt64MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt64MaxValueCastToUInt64IsFoldedCorrectly()
        {
            ulong integerOneDecrementUnderInt64MaxValue = 9223372036854775806;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneDecrementUnderInt64MaxValue) != 9223372036854775806)
            {
                Console.WriteLine($"'(ulong)9223372036854775806' was evaluted to '{(ulong)integerOneDecrementUnderInt64MaxValue}'. Expected: '9223372036854775806'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt64MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt64MaxValueCastToUInt64IsFoldedCorrectly()
        {
            ulong integerOneIncrementAboveInt64MaxValue = 9223372036854775808;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneIncrementAboveInt64MaxValue) != 9223372036854775808)
            {
                Console.WriteLine($"'(ulong)9223372036854775808' was evaluted to '{(ulong)integerOneIncrementAboveInt64MaxValue}'. Expected: '9223372036854775808'.");
                _counter++;
            }
        }
        ConfirmUInt64MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64MaxValueCastToUInt64IsFoldedCorrectly()
        {
            ulong uInt64MaxValue = 18446744073709551615;

            if (BreakUpFlow())
                return;

            if (checked((ulong)uInt64MaxValue) != 18446744073709551615)
            {
                Console.WriteLine($"'(ulong)18446744073709551615' was evaluted to '{(ulong)uInt64MaxValue}'. Expected: '18446744073709551615'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderUInt64MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt64MaxValueCastToUInt64IsFoldedCorrectly()
        {
            ulong integerOneDecrementUnderUInt64MaxValue = 18446744073709551614;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneDecrementUnderUInt64MaxValue) != 18446744073709551614)
            {
                Console.WriteLine($"'(ulong)18446744073709551614' was evaluted to '{(ulong)integerOneDecrementUnderUInt64MaxValue}'. Expected: '18446744073709551614'.");
                _counter++;
            }
        }
    }
}
