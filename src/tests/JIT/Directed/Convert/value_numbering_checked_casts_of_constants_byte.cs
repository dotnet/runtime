// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

public partial class ValueNumberingCheckedCastsOfConstants
{
    private static void TestCastingSByteToSingle()
    {
        ConfirmIntegerZeroCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToSingleIsFoldedCorrectly()
        {
            sbyte integerZero = 0;

            if (BreakUpFlow())
                return;

            if (checked((float)integerZero) != 0f)
            {
                Console.WriteLine($"'(float)0' was evaluated to '{(float)integerZero}'. Expected: '0f'.");
                _counter++;
            }
        }
        ConfirmSByteMinValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMinValueCastToSingleIsFoldedCorrectly()
        {
            sbyte sByteMinValue = -128;

            if (BreakUpFlow())
                return;

            if (checked((float)sByteMinValue) != -128f)
            {
                Console.WriteLine($"'(float)-128' was evaluated to '{(float)sByteMinValue}'. Expected: '-128f'.");
                _counter++;
            }
        }
        ConfirmSByteMaxValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToSingleIsFoldedCorrectly()
        {
            sbyte sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((float)sByteMaxValue) != 127f)
            {
                Console.WriteLine($"'(float)127' was evaluated to '{(float)sByteMaxValue}'. Expected: '127f'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMinValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMinValueCastToSingleIsFoldedCorrectly()
        {
            sbyte integerOneIncrementAboveSByteMinValue = -127;

            if (BreakUpFlow())
                return;

            if (checked((float)integerOneIncrementAboveSByteMinValue) != -127f)
            {
                Console.WriteLine($"'(float)-127' was evaluated to '{(float)integerOneIncrementAboveSByteMinValue}'. Expected: '-127f'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToSingleIsFoldedCorrectly()
        {
            sbyte integerOneDecrementUnderSByteMaxValue = 126;

            if (BreakUpFlow())
                return;

            if (checked((float)integerOneDecrementUnderSByteMaxValue) != 126f)
            {
                Console.WriteLine($"'(float)126' was evaluated to '{(float)integerOneDecrementUnderSByteMaxValue}'. Expected: '126f'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMinValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMinValueCastToSingleIsFoldedCorrectly()
        {
            sbyte integerOneDecrementUnderByteMinValue = -1;

            if (BreakUpFlow())
                return;

            if (checked((float)integerOneDecrementUnderByteMinValue) != -1f)
            {
                Console.WriteLine($"'(float)-1' was evaluated to '{(float)integerOneDecrementUnderByteMinValue}'. Expected: '-1f'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToSingleIsFoldedCorrectly()
        {
            sbyte integerOneIncrementAboveByteMinValue = 1;

            if (BreakUpFlow())
                return;

            if (checked((float)integerOneIncrementAboveByteMinValue) != 1f)
            {
                Console.WriteLine($"'(float)1' was evaluated to '{(float)integerOneIncrementAboveByteMinValue}'. Expected: '1f'.");
                _counter++;
            }
        }
    }

    private static void TestCastingSByteToDouble()
    {
        ConfirmIntegerZeroCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToDoubleIsFoldedCorrectly()
        {
            sbyte integerZero = 0;

            if (BreakUpFlow())
                return;

            if (checked((double)integerZero) != 0d)
            {
                Console.WriteLine($"'(double)0' was evaluated to '{(double)integerZero}'. Expected: '0d'.");
                _counter++;
            }
        }
        ConfirmSByteMinValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMinValueCastToDoubleIsFoldedCorrectly()
        {
            sbyte sByteMinValue = -128;

            if (BreakUpFlow())
                return;

            if (checked((double)sByteMinValue) != -128d)
            {
                Console.WriteLine($"'(double)-128' was evaluated to '{(double)sByteMinValue}'. Expected: '-128d'.");
                _counter++;
            }
        }
        ConfirmSByteMaxValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToDoubleIsFoldedCorrectly()
        {
            sbyte sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((double)sByteMaxValue) != 127d)
            {
                Console.WriteLine($"'(double)127' was evaluated to '{(double)sByteMaxValue}'. Expected: '127d'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMinValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMinValueCastToDoubleIsFoldedCorrectly()
        {
            sbyte integerOneIncrementAboveSByteMinValue = -127;

            if (BreakUpFlow())
                return;

            if (checked((double)integerOneIncrementAboveSByteMinValue) != -127d)
            {
                Console.WriteLine($"'(double)-127' was evaluated to '{(double)integerOneIncrementAboveSByteMinValue}'. Expected: '-127d'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToDoubleIsFoldedCorrectly()
        {
            sbyte integerOneDecrementUnderSByteMaxValue = 126;

            if (BreakUpFlow())
                return;

            if (checked((double)integerOneDecrementUnderSByteMaxValue) != 126d)
            {
                Console.WriteLine($"'(double)126' was evaluated to '{(double)integerOneDecrementUnderSByteMaxValue}'. Expected: '126d'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMinValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMinValueCastToDoubleIsFoldedCorrectly()
        {
            sbyte integerOneDecrementUnderByteMinValue = -1;

            if (BreakUpFlow())
                return;

            if (checked((double)integerOneDecrementUnderByteMinValue) != -1d)
            {
                Console.WriteLine($"'(double)-1' was evaluated to '{(double)integerOneDecrementUnderByteMinValue}'. Expected: '-1d'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToDoubleIsFoldedCorrectly()
        {
            sbyte integerOneIncrementAboveByteMinValue = 1;

            if (BreakUpFlow())
                return;

            if (checked((double)integerOneIncrementAboveByteMinValue) != 1d)
            {
                Console.WriteLine($"'(double)1' was evaluated to '{(double)integerOneIncrementAboveByteMinValue}'. Expected: '1d'.");
                _counter++;
            }
        }
    }

    private static void TestCastingSByteToSByte()
    {
        ConfirmIntegerZeroCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToSByteIsFoldedCorrectly()
        {
            sbyte integerZero = 0;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)integerZero) != 0)
            {
                Console.WriteLine($"'(sbyte)0' was evaluated to '{(sbyte)integerZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmSByteMinValueCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMinValueCastToSByteIsFoldedCorrectly()
        {
            sbyte sByteMinValue = -128;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)sByteMinValue) != -128)
            {
                Console.WriteLine($"'(sbyte)-128' was evaluated to '{(sbyte)sByteMinValue}'. Expected: '-128'.");
                _counter++;
            }
        }
        ConfirmSByteMaxValueCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToSByteIsFoldedCorrectly()
        {
            sbyte sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(sbyte)127' was evaluated to '{(sbyte)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMinValueCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMinValueCastToSByteIsFoldedCorrectly()
        {
            sbyte integerOneIncrementAboveSByteMinValue = -127;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)integerOneIncrementAboveSByteMinValue) != -127)
            {
                Console.WriteLine($"'(sbyte)-127' was evaluated to '{(sbyte)integerOneIncrementAboveSByteMinValue}'. Expected: '-127'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToSByteIsFoldedCorrectly()
        {
            sbyte integerOneDecrementUnderSByteMaxValue = 126;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(sbyte)126' was evaluated to '{(sbyte)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMinValueCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMinValueCastToSByteIsFoldedCorrectly()
        {
            sbyte integerOneDecrementUnderByteMinValue = -1;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)integerOneDecrementUnderByteMinValue) != -1)
            {
                Console.WriteLine($"'(sbyte)-1' was evaluated to '{(sbyte)integerOneDecrementUnderByteMinValue}'. Expected: '-1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToSByteIsFoldedCorrectly()
        {
            sbyte integerOneIncrementAboveByteMinValue = 1;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(sbyte)1' was evaluated to '{(sbyte)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
    }

    private static void TestCastingSByteToByte()
    {
        ConfirmIntegerZeroCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToByteIsFoldedCorrectly()
        {
            sbyte integerZero = 0;

            if (BreakUpFlow())
                return;

            if (checked((byte)integerZero) != 0)
            {
                Console.WriteLine($"'(byte)0' was evaluated to '{(byte)integerZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmSByteMinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMinValueCastToByteOverflows()
        {
            sbyte from = -128;
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
            sbyte sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((byte)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(byte)127' was evaluated to '{(byte)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmSByteOneIncrementAboveSByteMinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteOneIncrementAboveSByteMinValueCastToByteOverflows()
        {
            sbyte from = -127;
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
            sbyte integerOneDecrementUnderSByteMaxValue = 126;

            if (BreakUpFlow())
                return;

            if (checked((byte)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(byte)126' was evaluated to '{(byte)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmSByteOneDecrementUnderByteMinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteOneDecrementUnderByteMinValueCastToByteOverflows()
        {
            sbyte from = -1;
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
            sbyte integerOneIncrementAboveByteMinValue = 1;

            if (BreakUpFlow())
                return;

            if (checked((byte)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(byte)1' was evaluated to '{(byte)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
    }

    private static void TestCastingSByteToInt16()
    {
        ConfirmIntegerZeroCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToInt16IsFoldedCorrectly()
        {
            sbyte integerZero = 0;

            if (BreakUpFlow())
                return;

            if (checked((short)integerZero) != 0)
            {
                Console.WriteLine($"'(short)0' was evaluated to '{(short)integerZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmSByteMinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMinValueCastToInt16IsFoldedCorrectly()
        {
            sbyte sByteMinValue = -128;

            if (BreakUpFlow())
                return;

            if (checked((short)sByteMinValue) != -128)
            {
                Console.WriteLine($"'(short)-128' was evaluated to '{(short)sByteMinValue}'. Expected: '-128'.");
                _counter++;
            }
        }
        ConfirmSByteMaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToInt16IsFoldedCorrectly()
        {
            sbyte sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((short)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(short)127' was evaluated to '{(short)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMinValueCastToInt16IsFoldedCorrectly()
        {
            sbyte integerOneIncrementAboveSByteMinValue = -127;

            if (BreakUpFlow())
                return;

            if (checked((short)integerOneIncrementAboveSByteMinValue) != -127)
            {
                Console.WriteLine($"'(short)-127' was evaluated to '{(short)integerOneIncrementAboveSByteMinValue}'. Expected: '-127'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToInt16IsFoldedCorrectly()
        {
            sbyte integerOneDecrementUnderSByteMaxValue = 126;

            if (BreakUpFlow())
                return;

            if (checked((short)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(short)126' was evaluated to '{(short)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMinValueCastToInt16IsFoldedCorrectly()
        {
            sbyte integerOneDecrementUnderByteMinValue = -1;

            if (BreakUpFlow())
                return;

            if (checked((short)integerOneDecrementUnderByteMinValue) != -1)
            {
                Console.WriteLine($"'(short)-1' was evaluated to '{(short)integerOneDecrementUnderByteMinValue}'. Expected: '-1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToInt16IsFoldedCorrectly()
        {
            sbyte integerOneIncrementAboveByteMinValue = 1;

            if (BreakUpFlow())
                return;

            if (checked((short)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(short)1' was evaluated to '{(short)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
    }

    private static void TestCastingSByteToUInt16()
    {
        ConfirmIntegerZeroCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToUInt16IsFoldedCorrectly()
        {
            sbyte integerZero = 0;

            if (BreakUpFlow())
                return;

            if (checked((ushort)integerZero) != 0)
            {
                Console.WriteLine($"'(ushort)0' was evaluated to '{(ushort)integerZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmSByteMinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMinValueCastToUInt16Overflows()
        {
            sbyte from = -128;
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
            sbyte sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((ushort)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(ushort)127' was evaluated to '{(ushort)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmSByteOneIncrementAboveSByteMinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteOneIncrementAboveSByteMinValueCastToUInt16Overflows()
        {
            sbyte from = -127;
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
            sbyte integerOneDecrementUnderSByteMaxValue = 126;

            if (BreakUpFlow())
                return;

            if (checked((ushort)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(ushort)126' was evaluated to '{(ushort)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmSByteOneDecrementUnderByteMinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteOneDecrementUnderByteMinValueCastToUInt16Overflows()
        {
            sbyte from = -1;
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
            sbyte integerOneIncrementAboveByteMinValue = 1;

            if (BreakUpFlow())
                return;

            if (checked((ushort)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(ushort)1' was evaluated to '{(ushort)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
    }

    private static void TestCastingSByteToInt32()
    {
        ConfirmIntegerZeroCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToInt32IsFoldedCorrectly()
        {
            sbyte integerZero = 0;

            if (BreakUpFlow())
                return;

            if (checked((int)integerZero) != 0)
            {
                Console.WriteLine($"'(int)0' was evaluated to '{(int)integerZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmSByteMinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMinValueCastToInt32IsFoldedCorrectly()
        {
            sbyte sByteMinValue = -128;

            if (BreakUpFlow())
                return;

            if (checked((int)sByteMinValue) != -128)
            {
                Console.WriteLine($"'(int)-128' was evaluated to '{(int)sByteMinValue}'. Expected: '-128'.");
                _counter++;
            }
        }
        ConfirmSByteMaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToInt32IsFoldedCorrectly()
        {
            sbyte sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((int)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(int)127' was evaluated to '{(int)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMinValueCastToInt32IsFoldedCorrectly()
        {
            sbyte integerOneIncrementAboveSByteMinValue = -127;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneIncrementAboveSByteMinValue) != -127)
            {
                Console.WriteLine($"'(int)-127' was evaluated to '{(int)integerOneIncrementAboveSByteMinValue}'. Expected: '-127'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToInt32IsFoldedCorrectly()
        {
            sbyte integerOneDecrementUnderSByteMaxValue = 126;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(int)126' was evaluated to '{(int)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMinValueCastToInt32IsFoldedCorrectly()
        {
            sbyte integerOneDecrementUnderByteMinValue = -1;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneDecrementUnderByteMinValue) != -1)
            {
                Console.WriteLine($"'(int)-1' was evaluated to '{(int)integerOneDecrementUnderByteMinValue}'. Expected: '-1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToInt32IsFoldedCorrectly()
        {
            sbyte integerOneIncrementAboveByteMinValue = 1;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(int)1' was evaluated to '{(int)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
    }

    private static void TestCastingSByteToUInt32()
    {
        ConfirmIntegerZeroCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToUInt32IsFoldedCorrectly()
        {
            sbyte integerZero = 0;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerZero) != 0)
            {
                Console.WriteLine($"'(uint)0' was evaluated to '{(uint)integerZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmSByteMinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMinValueCastToUInt32Overflows()
        {
            sbyte from = -128;
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
            sbyte sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((uint)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(uint)127' was evaluated to '{(uint)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmSByteOneIncrementAboveSByteMinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteOneIncrementAboveSByteMinValueCastToUInt32Overflows()
        {
            sbyte from = -127;
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
            sbyte integerOneDecrementUnderSByteMaxValue = 126;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(uint)126' was evaluated to '{(uint)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmSByteOneDecrementUnderByteMinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteOneDecrementUnderByteMinValueCastToUInt32Overflows()
        {
            sbyte from = -1;
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
            sbyte integerOneIncrementAboveByteMinValue = 1;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(uint)1' was evaluated to '{(uint)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
    }

    private static void TestCastingSByteToInt64()
    {
        ConfirmIntegerZeroCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToInt64IsFoldedCorrectly()
        {
            sbyte integerZero = 0;

            if (BreakUpFlow())
                return;

            if (checked((long)integerZero) != 0)
            {
                Console.WriteLine($"'(long)0' was evaluated to '{(long)integerZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmSByteMinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMinValueCastToInt64IsFoldedCorrectly()
        {
            sbyte sByteMinValue = -128;

            if (BreakUpFlow())
                return;

            if (checked((long)sByteMinValue) != -128)
            {
                Console.WriteLine($"'(long)-128' was evaluated to '{(long)sByteMinValue}'. Expected: '-128'.");
                _counter++;
            }
        }
        ConfirmSByteMaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToInt64IsFoldedCorrectly()
        {
            sbyte sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((long)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(long)127' was evaluated to '{(long)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMinValueCastToInt64IsFoldedCorrectly()
        {
            sbyte integerOneIncrementAboveSByteMinValue = -127;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneIncrementAboveSByteMinValue) != -127)
            {
                Console.WriteLine($"'(long)-127' was evaluated to '{(long)integerOneIncrementAboveSByteMinValue}'. Expected: '-127'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToInt64IsFoldedCorrectly()
        {
            sbyte integerOneDecrementUnderSByteMaxValue = 126;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(long)126' was evaluated to '{(long)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMinValueCastToInt64IsFoldedCorrectly()
        {
            sbyte integerOneDecrementUnderByteMinValue = -1;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneDecrementUnderByteMinValue) != -1)
            {
                Console.WriteLine($"'(long)-1' was evaluated to '{(long)integerOneDecrementUnderByteMinValue}'. Expected: '-1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToInt64IsFoldedCorrectly()
        {
            sbyte integerOneIncrementAboveByteMinValue = 1;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(long)1' was evaluated to '{(long)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
    }

    private static void TestCastingSByteToUInt64()
    {
        ConfirmIntegerZeroCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToUInt64IsFoldedCorrectly()
        {
            sbyte integerZero = 0;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerZero) != 0)
            {
                Console.WriteLine($"'(ulong)0' was evaluated to '{(ulong)integerZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmSByteMinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMinValueCastToUInt64Overflows()
        {
            sbyte from = -128;
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
            sbyte sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((ulong)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(ulong)127' was evaluated to '{(ulong)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmSByteOneIncrementAboveSByteMinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteOneIncrementAboveSByteMinValueCastToUInt64Overflows()
        {
            sbyte from = -127;
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
            sbyte integerOneDecrementUnderSByteMaxValue = 126;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(ulong)126' was evaluated to '{(ulong)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmSByteOneDecrementUnderByteMinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteOneDecrementUnderByteMinValueCastToUInt64Overflows()
        {
            sbyte from = -1;
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
            sbyte integerOneIncrementAboveByteMinValue = 1;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(ulong)1' was evaluated to '{(ulong)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
    }

    private static void TestCastingByteToSingle()
    {
        ConfirmIntegerZeroCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToSingleIsFoldedCorrectly()
        {
            byte integerZero = 0;

            if (BreakUpFlow())
                return;

            if (checked((float)integerZero) != 0f)
            {
                Console.WriteLine($"'(float)0' was evaluated to '{(float)integerZero}'. Expected: '0f'.");
                _counter++;
            }
        }
        ConfirmSByteMaxValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToSingleIsFoldedCorrectly()
        {
            byte sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((float)sByteMaxValue) != 127f)
            {
                Console.WriteLine($"'(float)127' was evaluated to '{(float)sByteMaxValue}'. Expected: '127f'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToSingleIsFoldedCorrectly()
        {
            byte integerOneDecrementUnderSByteMaxValue = 126;

            if (BreakUpFlow())
                return;

            if (checked((float)integerOneDecrementUnderSByteMaxValue) != 126f)
            {
                Console.WriteLine($"'(float)126' was evaluated to '{(float)integerOneDecrementUnderSByteMaxValue}'. Expected: '126f'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMaxValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMaxValueCastToSingleIsFoldedCorrectly()
        {
            byte integerOneIncrementAboveSByteMaxValue = 128;

            if (BreakUpFlow())
                return;

            if (checked((float)integerOneIncrementAboveSByteMaxValue) != 128f)
            {
                Console.WriteLine($"'(float)128' was evaluated to '{(float)integerOneIncrementAboveSByteMaxValue}'. Expected: '128f'.");
                _counter++;
            }
        }
        ConfirmByteMaxValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmByteMaxValueCastToSingleIsFoldedCorrectly()
        {
            byte byteMaxValue = 255;

            if (BreakUpFlow())
                return;

            if (checked((float)byteMaxValue) != 255f)
            {
                Console.WriteLine($"'(float)255' was evaluated to '{(float)byteMaxValue}'. Expected: '255f'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToSingleIsFoldedCorrectly()
        {
            byte integerOneIncrementAboveByteMinValue = 1;

            if (BreakUpFlow())
                return;

            if (checked((float)integerOneIncrementAboveByteMinValue) != 1f)
            {
                Console.WriteLine($"'(float)1' was evaluated to '{(float)integerOneIncrementAboveByteMinValue}'. Expected: '1f'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMaxValueCastToSingleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMaxValueCastToSingleIsFoldedCorrectly()
        {
            byte integerOneDecrementUnderByteMaxValue = 254;

            if (BreakUpFlow())
                return;

            if (checked((float)integerOneDecrementUnderByteMaxValue) != 254f)
            {
                Console.WriteLine($"'(float)254' was evaluated to '{(float)integerOneDecrementUnderByteMaxValue}'. Expected: '254f'.");
                _counter++;
            }
        }
    }

    private static void TestCastingByteToDouble()
    {
        ConfirmIntegerZeroCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToDoubleIsFoldedCorrectly()
        {
            byte integerZero = 0;

            if (BreakUpFlow())
                return;

            if (checked((double)integerZero) != 0d)
            {
                Console.WriteLine($"'(double)0' was evaluated to '{(double)integerZero}'. Expected: '0d'.");
                _counter++;
            }
        }
        ConfirmSByteMaxValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToDoubleIsFoldedCorrectly()
        {
            byte sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((double)sByteMaxValue) != 127d)
            {
                Console.WriteLine($"'(double)127' was evaluated to '{(double)sByteMaxValue}'. Expected: '127d'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToDoubleIsFoldedCorrectly()
        {
            byte integerOneDecrementUnderSByteMaxValue = 126;

            if (BreakUpFlow())
                return;

            if (checked((double)integerOneDecrementUnderSByteMaxValue) != 126d)
            {
                Console.WriteLine($"'(double)126' was evaluated to '{(double)integerOneDecrementUnderSByteMaxValue}'. Expected: '126d'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMaxValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMaxValueCastToDoubleIsFoldedCorrectly()
        {
            byte integerOneIncrementAboveSByteMaxValue = 128;

            if (BreakUpFlow())
                return;

            if (checked((double)integerOneIncrementAboveSByteMaxValue) != 128d)
            {
                Console.WriteLine($"'(double)128' was evaluated to '{(double)integerOneIncrementAboveSByteMaxValue}'. Expected: '128d'.");
                _counter++;
            }
        }
        ConfirmByteMaxValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmByteMaxValueCastToDoubleIsFoldedCorrectly()
        {
            byte byteMaxValue = 255;

            if (BreakUpFlow())
                return;

            if (checked((double)byteMaxValue) != 255d)
            {
                Console.WriteLine($"'(double)255' was evaluated to '{(double)byteMaxValue}'. Expected: '255d'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToDoubleIsFoldedCorrectly()
        {
            byte integerOneIncrementAboveByteMinValue = 1;

            if (BreakUpFlow())
                return;

            if (checked((double)integerOneIncrementAboveByteMinValue) != 1d)
            {
                Console.WriteLine($"'(double)1' was evaluated to '{(double)integerOneIncrementAboveByteMinValue}'. Expected: '1d'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMaxValueCastToDoubleIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMaxValueCastToDoubleIsFoldedCorrectly()
        {
            byte integerOneDecrementUnderByteMaxValue = 254;

            if (BreakUpFlow())
                return;

            if (checked((double)integerOneDecrementUnderByteMaxValue) != 254d)
            {
                Console.WriteLine($"'(double)254' was evaluated to '{(double)integerOneDecrementUnderByteMaxValue}'. Expected: '254d'.");
                _counter++;
            }
        }
    }

    private static void TestCastingByteToSByte()
    {
        ConfirmIntegerZeroCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToSByteIsFoldedCorrectly()
        {
            byte integerZero = 0;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)integerZero) != 0)
            {
                Console.WriteLine($"'(sbyte)0' was evaluated to '{(sbyte)integerZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmSByteMaxValueCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToSByteIsFoldedCorrectly()
        {
            byte sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(sbyte)127' was evaluated to '{(sbyte)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToSByteIsFoldedCorrectly()
        {
            byte integerOneDecrementUnderSByteMaxValue = 126;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(sbyte)126' was evaluated to '{(sbyte)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmByteOneIncrementAboveSByteMaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmByteOneIncrementAboveSByteMaxValueCastToSByteOverflows()
        {
            byte from = 128;
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
            byte from = 255;
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
            byte integerOneIncrementAboveByteMinValue = 1;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(sbyte)1' was evaluated to '{(sbyte)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmByteOneDecrementUnderByteMaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmByteOneDecrementUnderByteMaxValueCastToSByteOverflows()
        {
            byte from = 254;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)254)' did not throw OverflowException.");
        }
    }

    private static void TestCastingByteToByte()
    {
        ConfirmIntegerZeroCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToByteIsFoldedCorrectly()
        {
            byte integerZero = 0;

            if (BreakUpFlow())
                return;

            if (checked((byte)integerZero) != 0)
            {
                Console.WriteLine($"'(byte)0' was evaluated to '{(byte)integerZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmSByteMaxValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToByteIsFoldedCorrectly()
        {
            byte sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((byte)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(byte)127' was evaluated to '{(byte)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToByteIsFoldedCorrectly()
        {
            byte integerOneDecrementUnderSByteMaxValue = 126;

            if (BreakUpFlow())
                return;

            if (checked((byte)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(byte)126' was evaluated to '{(byte)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMaxValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMaxValueCastToByteIsFoldedCorrectly()
        {
            byte integerOneIncrementAboveSByteMaxValue = 128;

            if (BreakUpFlow())
                return;

            if (checked((byte)integerOneIncrementAboveSByteMaxValue) != 128)
            {
                Console.WriteLine($"'(byte)128' was evaluated to '{(byte)integerOneIncrementAboveSByteMaxValue}'. Expected: '128'.");
                _counter++;
            }
        }
        ConfirmByteMaxValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmByteMaxValueCastToByteIsFoldedCorrectly()
        {
            byte byteMaxValue = 255;

            if (BreakUpFlow())
                return;

            if (checked((byte)byteMaxValue) != 255)
            {
                Console.WriteLine($"'(byte)255' was evaluated to '{(byte)byteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToByteIsFoldedCorrectly()
        {
            byte integerOneIncrementAboveByteMinValue = 1;

            if (BreakUpFlow())
                return;

            if (checked((byte)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(byte)1' was evaluated to '{(byte)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMaxValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMaxValueCastToByteIsFoldedCorrectly()
        {
            byte integerOneDecrementUnderByteMaxValue = 254;

            if (BreakUpFlow())
                return;

            if (checked((byte)integerOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(byte)254' was evaluated to '{(byte)integerOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
    }

    private static void TestCastingByteToInt16()
    {
        ConfirmIntegerZeroCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToInt16IsFoldedCorrectly()
        {
            byte integerZero = 0;

            if (BreakUpFlow())
                return;

            if (checked((short)integerZero) != 0)
            {
                Console.WriteLine($"'(short)0' was evaluated to '{(short)integerZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmSByteMaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToInt16IsFoldedCorrectly()
        {
            byte sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((short)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(short)127' was evaluated to '{(short)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToInt16IsFoldedCorrectly()
        {
            byte integerOneDecrementUnderSByteMaxValue = 126;

            if (BreakUpFlow())
                return;

            if (checked((short)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(short)126' was evaluated to '{(short)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMaxValueCastToInt16IsFoldedCorrectly()
        {
            byte integerOneIncrementAboveSByteMaxValue = 128;

            if (BreakUpFlow())
                return;

            if (checked((short)integerOneIncrementAboveSByteMaxValue) != 128)
            {
                Console.WriteLine($"'(short)128' was evaluated to '{(short)integerOneIncrementAboveSByteMaxValue}'. Expected: '128'.");
                _counter++;
            }
        }
        ConfirmByteMaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmByteMaxValueCastToInt16IsFoldedCorrectly()
        {
            byte byteMaxValue = 255;

            if (BreakUpFlow())
                return;

            if (checked((short)byteMaxValue) != 255)
            {
                Console.WriteLine($"'(short)255' was evaluated to '{(short)byteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToInt16IsFoldedCorrectly()
        {
            byte integerOneIncrementAboveByteMinValue = 1;

            if (BreakUpFlow())
                return;

            if (checked((short)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(short)1' was evaluated to '{(short)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMaxValueCastToInt16IsFoldedCorrectly()
        {
            byte integerOneDecrementUnderByteMaxValue = 254;

            if (BreakUpFlow())
                return;

            if (checked((short)integerOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(short)254' was evaluated to '{(short)integerOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
    }

    private static void TestCastingByteToUInt16()
    {
        ConfirmIntegerZeroCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToUInt16IsFoldedCorrectly()
        {
            byte integerZero = 0;

            if (BreakUpFlow())
                return;

            if (checked((ushort)integerZero) != 0)
            {
                Console.WriteLine($"'(ushort)0' was evaluated to '{(ushort)integerZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmSByteMaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToUInt16IsFoldedCorrectly()
        {
            byte sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((ushort)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(ushort)127' was evaluated to '{(ushort)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToUInt16IsFoldedCorrectly()
        {
            byte integerOneDecrementUnderSByteMaxValue = 126;

            if (BreakUpFlow())
                return;

            if (checked((ushort)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(ushort)126' was evaluated to '{(ushort)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMaxValueCastToUInt16IsFoldedCorrectly()
        {
            byte integerOneIncrementAboveSByteMaxValue = 128;

            if (BreakUpFlow())
                return;

            if (checked((ushort)integerOneIncrementAboveSByteMaxValue) != 128)
            {
                Console.WriteLine($"'(ushort)128' was evaluated to '{(ushort)integerOneIncrementAboveSByteMaxValue}'. Expected: '128'.");
                _counter++;
            }
        }
        ConfirmByteMaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmByteMaxValueCastToUInt16IsFoldedCorrectly()
        {
            byte byteMaxValue = 255;

            if (BreakUpFlow())
                return;

            if (checked((ushort)byteMaxValue) != 255)
            {
                Console.WriteLine($"'(ushort)255' was evaluated to '{(ushort)byteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToUInt16IsFoldedCorrectly()
        {
            byte integerOneIncrementAboveByteMinValue = 1;

            if (BreakUpFlow())
                return;

            if (checked((ushort)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(ushort)1' was evaluated to '{(ushort)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMaxValueCastToUInt16IsFoldedCorrectly()
        {
            byte integerOneDecrementUnderByteMaxValue = 254;

            if (BreakUpFlow())
                return;

            if (checked((ushort)integerOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(ushort)254' was evaluated to '{(ushort)integerOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
    }

    private static void TestCastingByteToInt32()
    {
        ConfirmIntegerZeroCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToInt32IsFoldedCorrectly()
        {
            byte integerZero = 0;

            if (BreakUpFlow())
                return;

            if (checked((int)integerZero) != 0)
            {
                Console.WriteLine($"'(int)0' was evaluated to '{(int)integerZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmSByteMaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToInt32IsFoldedCorrectly()
        {
            byte sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((int)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(int)127' was evaluated to '{(int)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToInt32IsFoldedCorrectly()
        {
            byte integerOneDecrementUnderSByteMaxValue = 126;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(int)126' was evaluated to '{(int)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMaxValueCastToInt32IsFoldedCorrectly()
        {
            byte integerOneIncrementAboveSByteMaxValue = 128;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneIncrementAboveSByteMaxValue) != 128)
            {
                Console.WriteLine($"'(int)128' was evaluated to '{(int)integerOneIncrementAboveSByteMaxValue}'. Expected: '128'.");
                _counter++;
            }
        }
        ConfirmByteMaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmByteMaxValueCastToInt32IsFoldedCorrectly()
        {
            byte byteMaxValue = 255;

            if (BreakUpFlow())
                return;

            if (checked((int)byteMaxValue) != 255)
            {
                Console.WriteLine($"'(int)255' was evaluated to '{(int)byteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToInt32IsFoldedCorrectly()
        {
            byte integerOneIncrementAboveByteMinValue = 1;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(int)1' was evaluated to '{(int)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMaxValueCastToInt32IsFoldedCorrectly()
        {
            byte integerOneDecrementUnderByteMaxValue = 254;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(int)254' was evaluated to '{(int)integerOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
    }

    private static void TestCastingByteToUInt32()
    {
        ConfirmIntegerZeroCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToUInt32IsFoldedCorrectly()
        {
            byte integerZero = 0;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerZero) != 0)
            {
                Console.WriteLine($"'(uint)0' was evaluated to '{(uint)integerZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmSByteMaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToUInt32IsFoldedCorrectly()
        {
            byte sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((uint)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(uint)127' was evaluated to '{(uint)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToUInt32IsFoldedCorrectly()
        {
            byte integerOneDecrementUnderSByteMaxValue = 126;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(uint)126' was evaluated to '{(uint)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMaxValueCastToUInt32IsFoldedCorrectly()
        {
            byte integerOneIncrementAboveSByteMaxValue = 128;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneIncrementAboveSByteMaxValue) != 128)
            {
                Console.WriteLine($"'(uint)128' was evaluated to '{(uint)integerOneIncrementAboveSByteMaxValue}'. Expected: '128'.");
                _counter++;
            }
        }
        ConfirmByteMaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmByteMaxValueCastToUInt32IsFoldedCorrectly()
        {
            byte byteMaxValue = 255;

            if (BreakUpFlow())
                return;

            if (checked((uint)byteMaxValue) != 255)
            {
                Console.WriteLine($"'(uint)255' was evaluated to '{(uint)byteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToUInt32IsFoldedCorrectly()
        {
            byte integerOneIncrementAboveByteMinValue = 1;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(uint)1' was evaluated to '{(uint)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMaxValueCastToUInt32IsFoldedCorrectly()
        {
            byte integerOneDecrementUnderByteMaxValue = 254;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(uint)254' was evaluated to '{(uint)integerOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
    }

    private static void TestCastingByteToInt64()
    {
        ConfirmIntegerZeroCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToInt64IsFoldedCorrectly()
        {
            byte integerZero = 0;

            if (BreakUpFlow())
                return;

            if (checked((long)integerZero) != 0)
            {
                Console.WriteLine($"'(long)0' was evaluated to '{(long)integerZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmSByteMaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToInt64IsFoldedCorrectly()
        {
            byte sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((long)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(long)127' was evaluated to '{(long)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToInt64IsFoldedCorrectly()
        {
            byte integerOneDecrementUnderSByteMaxValue = 126;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(long)126' was evaluated to '{(long)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMaxValueCastToInt64IsFoldedCorrectly()
        {
            byte integerOneIncrementAboveSByteMaxValue = 128;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneIncrementAboveSByteMaxValue) != 128)
            {
                Console.WriteLine($"'(long)128' was evaluated to '{(long)integerOneIncrementAboveSByteMaxValue}'. Expected: '128'.");
                _counter++;
            }
        }
        ConfirmByteMaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmByteMaxValueCastToInt64IsFoldedCorrectly()
        {
            byte byteMaxValue = 255;

            if (BreakUpFlow())
                return;

            if (checked((long)byteMaxValue) != 255)
            {
                Console.WriteLine($"'(long)255' was evaluated to '{(long)byteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToInt64IsFoldedCorrectly()
        {
            byte integerOneIncrementAboveByteMinValue = 1;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(long)1' was evaluated to '{(long)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMaxValueCastToInt64IsFoldedCorrectly()
        {
            byte integerOneDecrementUnderByteMaxValue = 254;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(long)254' was evaluated to '{(long)integerOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
    }

    private static void TestCastingByteToUInt64()
    {
        ConfirmIntegerZeroCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToUInt64IsFoldedCorrectly()
        {
            byte integerZero = 0;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerZero) != 0)
            {
                Console.WriteLine($"'(ulong)0' was evaluated to '{(ulong)integerZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmSByteMaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToUInt64IsFoldedCorrectly()
        {
            byte sByteMaxValue = 127;

            if (BreakUpFlow())
                return;

            if (checked((ulong)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(ulong)127' was evaluated to '{(ulong)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToUInt64IsFoldedCorrectly()
        {
            byte integerOneDecrementUnderSByteMaxValue = 126;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(ulong)126' was evaluated to '{(ulong)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMaxValueCastToUInt64IsFoldedCorrectly()
        {
            byte integerOneIncrementAboveSByteMaxValue = 128;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneIncrementAboveSByteMaxValue) != 128)
            {
                Console.WriteLine($"'(ulong)128' was evaluated to '{(ulong)integerOneIncrementAboveSByteMaxValue}'. Expected: '128'.");
                _counter++;
            }
        }
        ConfirmByteMaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmByteMaxValueCastToUInt64IsFoldedCorrectly()
        {
            byte byteMaxValue = 255;

            if (BreakUpFlow())
                return;

            if (checked((ulong)byteMaxValue) != 255)
            {
                Console.WriteLine($"'(ulong)255' was evaluated to '{(ulong)byteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToUInt64IsFoldedCorrectly()
        {
            byte integerOneIncrementAboveByteMinValue = 1;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(ulong)1' was evaluated to '{(ulong)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMaxValueCastToUInt64IsFoldedCorrectly()
        {
            byte integerOneDecrementUnderByteMaxValue = 254;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(ulong)254' was evaluated to '{(ulong)integerOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
    }
}
