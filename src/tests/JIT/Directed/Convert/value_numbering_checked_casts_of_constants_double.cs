// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

public partial class ValueNumberingCheckedCastsOfConstants
{
    private static void TestCastingDoubleToSByte()
    {
        ConfirmIntegerZeroCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToSByteIsFoldedCorrectly()
        {
            double integerZero = 0.0;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)integerZero) != 0)
            {
                Console.WriteLine($"'(sbyte)0.0' was evaluated to '{(sbyte)integerZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmDoubleMinusZeroCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleMinusZeroCastToSByteIsFoldedCorrectly()
        {
            double doubleMinusZero = -0d;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)doubleMinusZero) != 0)
            {
                Console.WriteLine($"'(sbyte)-0d' was evaluated to '{(sbyte)doubleMinusZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmDoubleZeroCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleZeroCastToSByteIsFoldedCorrectly()
        {
            double doubleZero = 0d;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)doubleZero) != 0)
            {
                Console.WriteLine($"'(sbyte)0d' was evaluated to '{(sbyte)doubleZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmDoubleMinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleMinValueCastToSByteOverflows()
        {
            double from = -1.7976931348623157E+308d;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-1.7976931348623157E+308d)' did not throw OverflowException.");
        }
        ConfirmDoubleMaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleMaxValueCastToSByteOverflows()
        {
            double from = 1.7976931348623157E+308d;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)1.7976931348623157E+308d)' did not throw OverflowException.");
        }
        ConfirmSByteMinValueCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMinValueCastToSByteIsFoldedCorrectly()
        {
            double sByteMinValue = -128.0;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)sByteMinValue) != -128)
            {
                Console.WriteLine($"'(sbyte)-128.0' was evaluated to '{(sbyte)sByteMinValue}'. Expected: '-128'.");
                _counter++;
            }
        }
        ConfirmSByteMaxValueCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToSByteIsFoldedCorrectly()
        {
            double sByteMaxValue = 127.0;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(sbyte)127.0' was evaluated to '{(sbyte)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderSByteMinValueCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderSByteMinValueCastToSByteIsFoldedCorrectly()
        {
            double doubleOneDecrementUnderSByteMinValue = -128.00000000000003d;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)doubleOneDecrementUnderSByteMinValue) != -128)
            {
                Console.WriteLine($"'(sbyte)-128.00000000000003d' was evaluated to '{(sbyte)doubleOneDecrementUnderSByteMinValue}'. Expected: '-128'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderSByteMinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderSByteMinValueCastToSByteOverflows()
        {
            double from = -129d;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-129d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveSByteMinValueCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveSByteMinValueCastToSByteIsFoldedCorrectly()
        {
            double doubleOneIncrementAboveSByteMinValue = -127.99999999999999d;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)doubleOneIncrementAboveSByteMinValue) != -127)
            {
                Console.WriteLine($"'(sbyte)-127.99999999999999d' was evaluated to '{(sbyte)doubleOneIncrementAboveSByteMinValue}'. Expected: '-127'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveSByteMinValueCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveSByteMinValueCastToSByteIsFoldedCorrectly()
        {
            double doubleOneFullIncrementAboveSByteMinValue = -127d;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)doubleOneFullIncrementAboveSByteMinValue) != -127)
            {
                Console.WriteLine($"'(sbyte)-127d' was evaluated to '{(sbyte)doubleOneFullIncrementAboveSByteMinValue}'. Expected: '-127'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderSByteMaxValueCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderSByteMaxValueCastToSByteIsFoldedCorrectly()
        {
            double doubleOneDecrementUnderSByteMaxValue = 126.99999999999999d;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)doubleOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(sbyte)126.99999999999999d' was evaluated to '{(sbyte)doubleOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderSByteMaxValueCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderSByteMaxValueCastToSByteIsFoldedCorrectly()
        {
            double doubleOneFullDecrementUnderSByteMaxValue = 126d;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)doubleOneFullDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(sbyte)126d' was evaluated to '{(sbyte)doubleOneFullDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmDoubleOneIncrementAboveSByteMaxValueCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveSByteMaxValueCastToSByteIsFoldedCorrectly()
        {
            double doubleOneIncrementAboveSByteMaxValue = 127.00000000000001d;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)doubleOneIncrementAboveSByteMaxValue) != 127)
            {
                Console.WriteLine($"'(sbyte)127.00000000000001d' was evaluated to '{(sbyte)doubleOneIncrementAboveSByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveSByteMaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveSByteMaxValueCastToSByteOverflows()
        {
            double from = 128d;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)128d)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderSByteMinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMinValueCastToSByteOverflows()
        {
            double from = -129.0;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-129.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveSByteMinValueCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMinValueCastToSByteIsFoldedCorrectly()
        {
            double integerOneIncrementAboveSByteMinValue = -127.0;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)integerOneIncrementAboveSByteMinValue) != -127)
            {
                Console.WriteLine($"'(sbyte)-127.0' was evaluated to '{(sbyte)integerOneIncrementAboveSByteMinValue}'. Expected: '-127'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToSByteIsFoldedCorrectly()
        {
            double integerOneDecrementUnderSByteMaxValue = 126.0;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(sbyte)126.0' was evaluated to '{(sbyte)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMaxValueCastToSByteOverflows()
        {
            double from = 128.0;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)128.0)' did not throw OverflowException.");
        }
        ConfirmByteMaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmByteMaxValueCastToSByteOverflows()
        {
            double from = 255.0;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)255.0)' did not throw OverflowException.");
        }
        ConfirmDoubleOneDecrementUnderByteMinValueCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderByteMinValueCastToSByteIsFoldedCorrectly()
        {
            double doubleOneDecrementUnderByteMinValue = -5E-324d;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)doubleOneDecrementUnderByteMinValue) != 0)
            {
                Console.WriteLine($"'(sbyte)-5E-324d' was evaluated to '{(sbyte)doubleOneDecrementUnderByteMinValue}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderByteMinValueCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderByteMinValueCastToSByteIsFoldedCorrectly()
        {
            double doubleOneFullDecrementUnderByteMinValue = -1d;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)doubleOneFullDecrementUnderByteMinValue) != -1)
            {
                Console.WriteLine($"'(sbyte)-1d' was evaluated to '{(sbyte)doubleOneFullDecrementUnderByteMinValue}'. Expected: '-1'.");
                _counter++;
            }
        }
        ConfirmDoubleOneIncrementAboveByteMinValueCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveByteMinValueCastToSByteIsFoldedCorrectly()
        {
            double doubleOneIncrementAboveByteMinValue = 5E-324d;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)doubleOneIncrementAboveByteMinValue) != 0)
            {
                Console.WriteLine($"'(sbyte)5E-324d' was evaluated to '{(sbyte)doubleOneIncrementAboveByteMinValue}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveByteMinValueCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveByteMinValueCastToSByteIsFoldedCorrectly()
        {
            double doubleOneFullIncrementAboveByteMinValue = 1d;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)doubleOneFullIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(sbyte)1d' was evaluated to '{(sbyte)doubleOneFullIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderByteMaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderByteMaxValueCastToSByteOverflows()
        {
            double from = 254.99999999999997d;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)254.99999999999997d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullDecrementUnderByteMaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderByteMaxValueCastToSByteOverflows()
        {
            double from = 254d;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)254d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveByteMaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveByteMaxValueCastToSByteOverflows()
        {
            double from = 255.00000000000003d;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)255.00000000000003d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullIncrementAboveByteMaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveByteMaxValueCastToSByteOverflows()
        {
            double from = 256d;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)256d)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderByteMinValueCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMinValueCastToSByteIsFoldedCorrectly()
        {
            double integerOneDecrementUnderByteMinValue = -1.0;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)integerOneDecrementUnderByteMinValue) != -1)
            {
                Console.WriteLine($"'(sbyte)-1.0' was evaluated to '{(sbyte)integerOneDecrementUnderByteMinValue}'. Expected: '-1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToSByteIsFoldedCorrectly()
        {
            double integerOneIncrementAboveByteMinValue = 1.0;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(sbyte)1.0' was evaluated to '{(sbyte)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMaxValueCastToSByteOverflows()
        {
            double from = 254.0;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)254.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveByteMaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMaxValueCastToSByteOverflows()
        {
            double from = 256.0;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)256.0)' did not throw OverflowException.");
        }
        ConfirmInt16MinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MinValueCastToSByteOverflows()
        {
            double from = -32768.0;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-32768.0)' did not throw OverflowException.");
        }
        ConfirmInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MaxValueCastToSByteOverflows()
        {
            double from = 32767.0;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)32767.0)' did not throw OverflowException.");
        }
        ConfirmDoubleOneDecrementUnderInt16MinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderInt16MinValueCastToSByteOverflows()
        {
            double from = -32768.00000000001d;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-32768.00000000001d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullDecrementUnderInt16MinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderInt16MinValueCastToSByteOverflows()
        {
            double from = -32769d;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-32769d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveInt16MinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveInt16MinValueCastToSByteOverflows()
        {
            double from = -32767.999999999996d;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-32767.999999999996d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullIncrementAboveInt16MinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveInt16MinValueCastToSByteOverflows()
        {
            double from = -32767d;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-32767d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneDecrementUnderInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderInt16MaxValueCastToSByteOverflows()
        {
            double from = 32766.999999999996d;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)32766.999999999996d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullDecrementUnderInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderInt16MaxValueCastToSByteOverflows()
        {
            double from = 32766d;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)32766d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveInt16MaxValueCastToSByteOverflows()
        {
            double from = 32767.000000000004d;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)32767.000000000004d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullIncrementAboveInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveInt16MaxValueCastToSByteOverflows()
        {
            double from = 32768d;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)32768d)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt16MinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MinValueCastToSByteOverflows()
        {
            double from = -32769.0;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-32769.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt16MinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt16MinValueCastToSByteOverflows()
        {
            double from = -32767.0;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-32767.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MaxValueCastToSByteOverflows()
        {
            double from = 32766.0;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)32766.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt16MaxValueCastToSByteOverflows()
        {
            double from = 32768.0;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)32768.0)' did not throw OverflowException.");
        }
        ConfirmUInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt16MaxValueCastToSByteOverflows()
        {
            double from = 65535.0;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)65535.0)' did not throw OverflowException.");
        }
        ConfirmDoubleOneDecrementUnderUInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderUInt16MaxValueCastToSByteOverflows()
        {
            double from = 65534.99999999999d;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)65534.99999999999d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullDecrementUnderUInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderUInt16MaxValueCastToSByteOverflows()
        {
            double from = 65534d;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)65534d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveUInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveUInt16MaxValueCastToSByteOverflows()
        {
            double from = 65535.00000000001d;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)65535.00000000001d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullIncrementAboveUInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveUInt16MaxValueCastToSByteOverflows()
        {
            double from = 65536d;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)65536d)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderUInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt16MaxValueCastToSByteOverflows()
        {
            double from = 65534.0;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)65534.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToSByteOverflows()
        {
            double from = 65536.0;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)65536.0)' did not throw OverflowException.");
        }
        ConfirmInt32MinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MinValueCastToSByteOverflows()
        {
            double from = -2147483648.0;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-2147483648.0)' did not throw OverflowException.");
        }
        ConfirmInt32MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MaxValueCastToSByteOverflows()
        {
            double from = 2147483647.0;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)2147483647.0)' did not throw OverflowException.");
        }
        ConfirmDoubleOneDecrementUnderInt32MinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderInt32MinValueCastToSByteOverflows()
        {
            double from = -2147483648.0000005d;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-2147483648.0000005d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullDecrementUnderInt32MinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderInt32MinValueCastToSByteOverflows()
        {
            double from = -2147483649d;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-2147483649d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveInt32MinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveInt32MinValueCastToSByteOverflows()
        {
            double from = -2147483647.9999998d;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-2147483647.9999998d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullIncrementAboveInt32MinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveInt32MinValueCastToSByteOverflows()
        {
            double from = -2147483647d;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-2147483647d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneDecrementUnderInt32MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderInt32MaxValueCastToSByteOverflows()
        {
            double from = 2147483646.9999998d;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)2147483646.9999998d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullDecrementUnderInt32MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderInt32MaxValueCastToSByteOverflows()
        {
            double from = 2147483646d;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)2147483646d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveInt32MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveInt32MaxValueCastToSByteOverflows()
        {
            double from = 2147483647.0000002d;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)2147483647.0000002d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullIncrementAboveInt32MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveInt32MaxValueCastToSByteOverflows()
        {
            double from = 2147483648d;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)2147483648d)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt32MinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt32MinValueCastToSByteOverflows()
        {
            double from = -2147483649.0;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-2147483649.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt32MinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt32MinValueCastToSByteOverflows()
        {
            double from = -2147483647.0;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-2147483647.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt32MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt32MaxValueCastToSByteOverflows()
        {
            double from = 2147483646.0;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)2147483646.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt32MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt32MaxValueCastToSByteOverflows()
        {
            double from = 2147483648.0;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)2147483648.0)' did not throw OverflowException.");
        }
        ConfirmUInt32MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32MaxValueCastToSByteOverflows()
        {
            double from = 4294967295.0;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)4294967295.0)' did not throw OverflowException.");
        }
        ConfirmDoubleOneDecrementUnderUInt32MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderUInt32MaxValueCastToSByteOverflows()
        {
            double from = 4294967294.9999995d;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)4294967294.9999995d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullDecrementUnderUInt32MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderUInt32MaxValueCastToSByteOverflows()
        {
            double from = 4294967294d;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)4294967294d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveUInt32MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveUInt32MaxValueCastToSByteOverflows()
        {
            double from = 4294967295.0000005d;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)4294967295.0000005d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullIncrementAboveUInt32MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveUInt32MaxValueCastToSByteOverflows()
        {
            double from = 4294967296d;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)4294967296d)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderUInt32MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt32MaxValueCastToSByteOverflows()
        {
            double from = 4294967294.0;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)4294967294.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveUInt32MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt32MaxValueCastToSByteOverflows()
        {
            double from = 4294967296.0;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)4294967296.0)' did not throw OverflowException.");
        }
        ConfirmInt64MinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MinValueCastToSByteOverflows()
        {
            double from = -9223372036854775808.0;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-9223372036854775808.0)' did not throw OverflowException.");
        }
        ConfirmInt64MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MaxValueCastToSByteOverflows()
        {
            double from = 9223372036854775807.0;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)9223372036854775807.0)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveInt64MinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveInt64MinValueCastToSByteOverflows()
        {
            double from = -9.223372036854775E+18d;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-9.223372036854775E+18d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneDecrementUnderInt64MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderInt64MaxValueCastToSByteOverflows()
        {
            double from = 9.223372036854775E+18d;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)9.223372036854775E+18d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveInt64MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveInt64MaxValueCastToSByteOverflows()
        {
            double from = 9.223372036854778E+18d;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)9.223372036854778E+18d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullIncrementAboveInt64MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveInt64MaxValueCastToSByteOverflows()
        {
            double from = 9.223372036854776E+18d;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)9.223372036854776E+18d)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt64MinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt64MinValueCastToSByteOverflows()
        {
            double from = -9223372036854775807.0;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-9223372036854775807.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt64MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt64MaxValueCastToSByteOverflows()
        {
            double from = 9223372036854775806.0;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)9223372036854775806.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt64MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt64MaxValueCastToSByteOverflows()
        {
            double from = 9223372036854775808.0;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)9223372036854775808.0)' did not throw OverflowException.");
        }
        ConfirmUInt64MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64MaxValueCastToSByteOverflows()
        {
            double from = 18446744073709551615.0;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)18446744073709551615.0)' did not throw OverflowException.");
        }
        ConfirmDoubleOneDecrementUnderUInt64MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderUInt64MaxValueCastToSByteOverflows()
        {
            double from = 1.844674407370955E+19d;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)1.844674407370955E+19d)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderUInt64MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt64MaxValueCastToSByteOverflows()
        {
            double from = 18446744073709551614.0;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)18446744073709551614.0)' did not throw OverflowException.");
        }
    }

    private static void TestCastingDoubleToByte()
    {
        ConfirmIntegerZeroCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToByteIsFoldedCorrectly()
        {
            double integerZero = 0.0;

            if (BreakUpFlow())
                return;

            if (checked((byte)integerZero) != 0)
            {
                Console.WriteLine($"'(byte)0.0' was evaluated to '{(byte)integerZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmDoubleMinusZeroCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleMinusZeroCastToByteIsFoldedCorrectly()
        {
            double doubleMinusZero = -0d;

            if (BreakUpFlow())
                return;

            if (checked((byte)doubleMinusZero) != 0)
            {
                Console.WriteLine($"'(byte)-0d' was evaluated to '{(byte)doubleMinusZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmDoubleZeroCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleZeroCastToByteIsFoldedCorrectly()
        {
            double doubleZero = 0d;

            if (BreakUpFlow())
                return;

            if (checked((byte)doubleZero) != 0)
            {
                Console.WriteLine($"'(byte)0d' was evaluated to '{(byte)doubleZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmDoubleMinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleMinValueCastToByteOverflows()
        {
            double from = -1.7976931348623157E+308d;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-1.7976931348623157E+308d)' did not throw OverflowException.");
        }
        ConfirmDoubleMaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleMaxValueCastToByteOverflows()
        {
            double from = 1.7976931348623157E+308d;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)1.7976931348623157E+308d)' did not throw OverflowException.");
        }
        ConfirmSByteMinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMinValueCastToByteOverflows()
        {
            double from = -128.0;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-128.0)' did not throw OverflowException.");
        }
        ConfirmSByteMaxValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToByteIsFoldedCorrectly()
        {
            double sByteMaxValue = 127.0;

            if (BreakUpFlow())
                return;

            if (checked((byte)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(byte)127.0' was evaluated to '{(byte)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderSByteMinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderSByteMinValueCastToByteOverflows()
        {
            double from = -128.00000000000003d;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-128.00000000000003d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullDecrementUnderSByteMinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderSByteMinValueCastToByteOverflows()
        {
            double from = -129d;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-129d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveSByteMinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveSByteMinValueCastToByteOverflows()
        {
            double from = -127.99999999999999d;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-127.99999999999999d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullIncrementAboveSByteMinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveSByteMinValueCastToByteOverflows()
        {
            double from = -127d;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-127d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneDecrementUnderSByteMaxValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderSByteMaxValueCastToByteIsFoldedCorrectly()
        {
            double doubleOneDecrementUnderSByteMaxValue = 126.99999999999999d;

            if (BreakUpFlow())
                return;

            if (checked((byte)doubleOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(byte)126.99999999999999d' was evaluated to '{(byte)doubleOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderSByteMaxValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderSByteMaxValueCastToByteIsFoldedCorrectly()
        {
            double doubleOneFullDecrementUnderSByteMaxValue = 126d;

            if (BreakUpFlow())
                return;

            if (checked((byte)doubleOneFullDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(byte)126d' was evaluated to '{(byte)doubleOneFullDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmDoubleOneIncrementAboveSByteMaxValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveSByteMaxValueCastToByteIsFoldedCorrectly()
        {
            double doubleOneIncrementAboveSByteMaxValue = 127.00000000000001d;

            if (BreakUpFlow())
                return;

            if (checked((byte)doubleOneIncrementAboveSByteMaxValue) != 127)
            {
                Console.WriteLine($"'(byte)127.00000000000001d' was evaluated to '{(byte)doubleOneIncrementAboveSByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveSByteMaxValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveSByteMaxValueCastToByteIsFoldedCorrectly()
        {
            double doubleOneFullIncrementAboveSByteMaxValue = 128d;

            if (BreakUpFlow())
                return;

            if (checked((byte)doubleOneFullIncrementAboveSByteMaxValue) != 128)
            {
                Console.WriteLine($"'(byte)128d' was evaluated to '{(byte)doubleOneFullIncrementAboveSByteMaxValue}'. Expected: '128'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMinValueCastToByteOverflows()
        {
            double from = -129.0;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-129.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveSByteMinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMinValueCastToByteOverflows()
        {
            double from = -127.0;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-127.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToByteIsFoldedCorrectly()
        {
            double integerOneDecrementUnderSByteMaxValue = 126.0;

            if (BreakUpFlow())
                return;

            if (checked((byte)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(byte)126.0' was evaluated to '{(byte)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMaxValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMaxValueCastToByteIsFoldedCorrectly()
        {
            double integerOneIncrementAboveSByteMaxValue = 128.0;

            if (BreakUpFlow())
                return;

            if (checked((byte)integerOneIncrementAboveSByteMaxValue) != 128)
            {
                Console.WriteLine($"'(byte)128.0' was evaluated to '{(byte)integerOneIncrementAboveSByteMaxValue}'. Expected: '128'.");
                _counter++;
            }
        }
        ConfirmByteMaxValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmByteMaxValueCastToByteIsFoldedCorrectly()
        {
            double byteMaxValue = 255.0;

            if (BreakUpFlow())
                return;

            if (checked((byte)byteMaxValue) != 255)
            {
                Console.WriteLine($"'(byte)255.0' was evaluated to '{(byte)byteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderByteMinValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderByteMinValueCastToByteIsFoldedCorrectly()
        {
            double doubleOneDecrementUnderByteMinValue = -5E-324d;

            if (BreakUpFlow())
                return;

            if (checked((byte)doubleOneDecrementUnderByteMinValue) != 0)
            {
                Console.WriteLine($"'(byte)-5E-324d' was evaluated to '{(byte)doubleOneDecrementUnderByteMinValue}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderByteMinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderByteMinValueCastToByteOverflows()
        {
            double from = -1d;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-1d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveByteMinValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveByteMinValueCastToByteIsFoldedCorrectly()
        {
            double doubleOneIncrementAboveByteMinValue = 5E-324d;

            if (BreakUpFlow())
                return;

            if (checked((byte)doubleOneIncrementAboveByteMinValue) != 0)
            {
                Console.WriteLine($"'(byte)5E-324d' was evaluated to '{(byte)doubleOneIncrementAboveByteMinValue}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveByteMinValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveByteMinValueCastToByteIsFoldedCorrectly()
        {
            double doubleOneFullIncrementAboveByteMinValue = 1d;

            if (BreakUpFlow())
                return;

            if (checked((byte)doubleOneFullIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(byte)1d' was evaluated to '{(byte)doubleOneFullIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderByteMaxValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderByteMaxValueCastToByteIsFoldedCorrectly()
        {
            double doubleOneDecrementUnderByteMaxValue = 254.99999999999997d;

            if (BreakUpFlow())
                return;

            if (checked((byte)doubleOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(byte)254.99999999999997d' was evaluated to '{(byte)doubleOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderByteMaxValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderByteMaxValueCastToByteIsFoldedCorrectly()
        {
            double doubleOneFullDecrementUnderByteMaxValue = 254d;

            if (BreakUpFlow())
                return;

            if (checked((byte)doubleOneFullDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(byte)254d' was evaluated to '{(byte)doubleOneFullDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmDoubleOneIncrementAboveByteMaxValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveByteMaxValueCastToByteIsFoldedCorrectly()
        {
            double doubleOneIncrementAboveByteMaxValue = 255.00000000000003d;

            if (BreakUpFlow())
                return;

            if (checked((byte)doubleOneIncrementAboveByteMaxValue) != 255)
            {
                Console.WriteLine($"'(byte)255.00000000000003d' was evaluated to '{(byte)doubleOneIncrementAboveByteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveByteMaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveByteMaxValueCastToByteOverflows()
        {
            double from = 256d;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)256d)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderByteMinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMinValueCastToByteOverflows()
        {
            double from = -1.0;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-1.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToByteIsFoldedCorrectly()
        {
            double integerOneIncrementAboveByteMinValue = 1.0;

            if (BreakUpFlow())
                return;

            if (checked((byte)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(byte)1.0' was evaluated to '{(byte)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMaxValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMaxValueCastToByteIsFoldedCorrectly()
        {
            double integerOneDecrementUnderByteMaxValue = 254.0;

            if (BreakUpFlow())
                return;

            if (checked((byte)integerOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(byte)254.0' was evaluated to '{(byte)integerOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMaxValueCastToByteOverflows()
        {
            double from = 256.0;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)256.0)' did not throw OverflowException.");
        }
        ConfirmInt16MinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MinValueCastToByteOverflows()
        {
            double from = -32768.0;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-32768.0)' did not throw OverflowException.");
        }
        ConfirmInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MaxValueCastToByteOverflows()
        {
            double from = 32767.0;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)32767.0)' did not throw OverflowException.");
        }
        ConfirmDoubleOneDecrementUnderInt16MinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderInt16MinValueCastToByteOverflows()
        {
            double from = -32768.00000000001d;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-32768.00000000001d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullDecrementUnderInt16MinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderInt16MinValueCastToByteOverflows()
        {
            double from = -32769d;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-32769d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveInt16MinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveInt16MinValueCastToByteOverflows()
        {
            double from = -32767.999999999996d;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-32767.999999999996d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullIncrementAboveInt16MinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveInt16MinValueCastToByteOverflows()
        {
            double from = -32767d;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-32767d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneDecrementUnderInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderInt16MaxValueCastToByteOverflows()
        {
            double from = 32766.999999999996d;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)32766.999999999996d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullDecrementUnderInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderInt16MaxValueCastToByteOverflows()
        {
            double from = 32766d;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)32766d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveInt16MaxValueCastToByteOverflows()
        {
            double from = 32767.000000000004d;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)32767.000000000004d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullIncrementAboveInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveInt16MaxValueCastToByteOverflows()
        {
            double from = 32768d;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)32768d)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt16MinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MinValueCastToByteOverflows()
        {
            double from = -32769.0;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-32769.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt16MinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt16MinValueCastToByteOverflows()
        {
            double from = -32767.0;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-32767.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MaxValueCastToByteOverflows()
        {
            double from = 32766.0;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)32766.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt16MaxValueCastToByteOverflows()
        {
            double from = 32768.0;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)32768.0)' did not throw OverflowException.");
        }
        ConfirmUInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt16MaxValueCastToByteOverflows()
        {
            double from = 65535.0;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)65535.0)' did not throw OverflowException.");
        }
        ConfirmDoubleOneDecrementUnderUInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderUInt16MaxValueCastToByteOverflows()
        {
            double from = 65534.99999999999d;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)65534.99999999999d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullDecrementUnderUInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderUInt16MaxValueCastToByteOverflows()
        {
            double from = 65534d;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)65534d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveUInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveUInt16MaxValueCastToByteOverflows()
        {
            double from = 65535.00000000001d;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)65535.00000000001d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullIncrementAboveUInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveUInt16MaxValueCastToByteOverflows()
        {
            double from = 65536d;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)65536d)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderUInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt16MaxValueCastToByteOverflows()
        {
            double from = 65534.0;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)65534.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToByteOverflows()
        {
            double from = 65536.0;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)65536.0)' did not throw OverflowException.");
        }
        ConfirmInt32MinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MinValueCastToByteOverflows()
        {
            double from = -2147483648.0;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-2147483648.0)' did not throw OverflowException.");
        }
        ConfirmInt32MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MaxValueCastToByteOverflows()
        {
            double from = 2147483647.0;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)2147483647.0)' did not throw OverflowException.");
        }
        ConfirmDoubleOneDecrementUnderInt32MinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderInt32MinValueCastToByteOverflows()
        {
            double from = -2147483648.0000005d;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-2147483648.0000005d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullDecrementUnderInt32MinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderInt32MinValueCastToByteOverflows()
        {
            double from = -2147483649d;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-2147483649d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveInt32MinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveInt32MinValueCastToByteOverflows()
        {
            double from = -2147483647.9999998d;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-2147483647.9999998d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullIncrementAboveInt32MinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveInt32MinValueCastToByteOverflows()
        {
            double from = -2147483647d;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-2147483647d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneDecrementUnderInt32MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderInt32MaxValueCastToByteOverflows()
        {
            double from = 2147483646.9999998d;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)2147483646.9999998d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullDecrementUnderInt32MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderInt32MaxValueCastToByteOverflows()
        {
            double from = 2147483646d;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)2147483646d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveInt32MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveInt32MaxValueCastToByteOverflows()
        {
            double from = 2147483647.0000002d;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)2147483647.0000002d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullIncrementAboveInt32MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveInt32MaxValueCastToByteOverflows()
        {
            double from = 2147483648d;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)2147483648d)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt32MinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt32MinValueCastToByteOverflows()
        {
            double from = -2147483649.0;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-2147483649.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt32MinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt32MinValueCastToByteOverflows()
        {
            double from = -2147483647.0;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-2147483647.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt32MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt32MaxValueCastToByteOverflows()
        {
            double from = 2147483646.0;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)2147483646.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt32MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt32MaxValueCastToByteOverflows()
        {
            double from = 2147483648.0;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)2147483648.0)' did not throw OverflowException.");
        }
        ConfirmUInt32MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32MaxValueCastToByteOverflows()
        {
            double from = 4294967295.0;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)4294967295.0)' did not throw OverflowException.");
        }
        ConfirmDoubleOneDecrementUnderUInt32MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderUInt32MaxValueCastToByteOverflows()
        {
            double from = 4294967294.9999995d;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)4294967294.9999995d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullDecrementUnderUInt32MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderUInt32MaxValueCastToByteOverflows()
        {
            double from = 4294967294d;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)4294967294d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveUInt32MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveUInt32MaxValueCastToByteOverflows()
        {
            double from = 4294967295.0000005d;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)4294967295.0000005d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullIncrementAboveUInt32MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveUInt32MaxValueCastToByteOverflows()
        {
            double from = 4294967296d;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)4294967296d)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderUInt32MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt32MaxValueCastToByteOverflows()
        {
            double from = 4294967294.0;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)4294967294.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveUInt32MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt32MaxValueCastToByteOverflows()
        {
            double from = 4294967296.0;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)4294967296.0)' did not throw OverflowException.");
        }
        ConfirmInt64MinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MinValueCastToByteOverflows()
        {
            double from = -9223372036854775808.0;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-9223372036854775808.0)' did not throw OverflowException.");
        }
        ConfirmInt64MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MaxValueCastToByteOverflows()
        {
            double from = 9223372036854775807.0;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)9223372036854775807.0)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveInt64MinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveInt64MinValueCastToByteOverflows()
        {
            double from = -9.223372036854775E+18d;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-9.223372036854775E+18d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneDecrementUnderInt64MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderInt64MaxValueCastToByteOverflows()
        {
            double from = 9.223372036854775E+18d;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)9.223372036854775E+18d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveInt64MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveInt64MaxValueCastToByteOverflows()
        {
            double from = 9.223372036854778E+18d;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)9.223372036854778E+18d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullIncrementAboveInt64MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveInt64MaxValueCastToByteOverflows()
        {
            double from = 9.223372036854776E+18d;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)9.223372036854776E+18d)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt64MinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt64MinValueCastToByteOverflows()
        {
            double from = -9223372036854775807.0;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-9223372036854775807.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt64MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt64MaxValueCastToByteOverflows()
        {
            double from = 9223372036854775806.0;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)9223372036854775806.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt64MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt64MaxValueCastToByteOverflows()
        {
            double from = 9223372036854775808.0;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)9223372036854775808.0)' did not throw OverflowException.");
        }
        ConfirmUInt64MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64MaxValueCastToByteOverflows()
        {
            double from = 18446744073709551615.0;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)18446744073709551615.0)' did not throw OverflowException.");
        }
        ConfirmDoubleOneDecrementUnderUInt64MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderUInt64MaxValueCastToByteOverflows()
        {
            double from = 1.844674407370955E+19d;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)1.844674407370955E+19d)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderUInt64MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt64MaxValueCastToByteOverflows()
        {
            double from = 18446744073709551614.0;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)18446744073709551614.0)' did not throw OverflowException.");
        }
    }

    private static void TestCastingDoubleToInt16()
    {
        ConfirmIntegerZeroCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToInt16IsFoldedCorrectly()
        {
            double integerZero = 0.0;

            if (BreakUpFlow())
                return;

            if (checked((short)integerZero) != 0)
            {
                Console.WriteLine($"'(short)0.0' was evaluated to '{(short)integerZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmDoubleMinusZeroCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleMinusZeroCastToInt16IsFoldedCorrectly()
        {
            double doubleMinusZero = -0d;

            if (BreakUpFlow())
                return;

            if (checked((short)doubleMinusZero) != 0)
            {
                Console.WriteLine($"'(short)-0d' was evaluated to '{(short)doubleMinusZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmDoubleZeroCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleZeroCastToInt16IsFoldedCorrectly()
        {
            double doubleZero = 0d;

            if (BreakUpFlow())
                return;

            if (checked((short)doubleZero) != 0)
            {
                Console.WriteLine($"'(short)0d' was evaluated to '{(short)doubleZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmDoubleMinValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleMinValueCastToInt16Overflows()
        {
            double from = -1.7976931348623157E+308d;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)-1.7976931348623157E+308d)' did not throw OverflowException.");
        }
        ConfirmDoubleMaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleMaxValueCastToInt16Overflows()
        {
            double from = 1.7976931348623157E+308d;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)1.7976931348623157E+308d)' did not throw OverflowException.");
        }
        ConfirmSByteMinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMinValueCastToInt16IsFoldedCorrectly()
        {
            double sByteMinValue = -128.0;

            if (BreakUpFlow())
                return;

            if (checked((short)sByteMinValue) != -128)
            {
                Console.WriteLine($"'(short)-128.0' was evaluated to '{(short)sByteMinValue}'. Expected: '-128'.");
                _counter++;
            }
        }
        ConfirmSByteMaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToInt16IsFoldedCorrectly()
        {
            double sByteMaxValue = 127.0;

            if (BreakUpFlow())
                return;

            if (checked((short)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(short)127.0' was evaluated to '{(short)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderSByteMinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderSByteMinValueCastToInt16IsFoldedCorrectly()
        {
            double doubleOneDecrementUnderSByteMinValue = -128.00000000000003d;

            if (BreakUpFlow())
                return;

            if (checked((short)doubleOneDecrementUnderSByteMinValue) != -128)
            {
                Console.WriteLine($"'(short)-128.00000000000003d' was evaluated to '{(short)doubleOneDecrementUnderSByteMinValue}'. Expected: '-128'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderSByteMinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderSByteMinValueCastToInt16IsFoldedCorrectly()
        {
            double doubleOneFullDecrementUnderSByteMinValue = -129d;

            if (BreakUpFlow())
                return;

            if (checked((short)doubleOneFullDecrementUnderSByteMinValue) != -129)
            {
                Console.WriteLine($"'(short)-129d' was evaluated to '{(short)doubleOneFullDecrementUnderSByteMinValue}'. Expected: '-129'.");
                _counter++;
            }
        }
        ConfirmDoubleOneIncrementAboveSByteMinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveSByteMinValueCastToInt16IsFoldedCorrectly()
        {
            double doubleOneIncrementAboveSByteMinValue = -127.99999999999999d;

            if (BreakUpFlow())
                return;

            if (checked((short)doubleOneIncrementAboveSByteMinValue) != -127)
            {
                Console.WriteLine($"'(short)-127.99999999999999d' was evaluated to '{(short)doubleOneIncrementAboveSByteMinValue}'. Expected: '-127'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveSByteMinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveSByteMinValueCastToInt16IsFoldedCorrectly()
        {
            double doubleOneFullIncrementAboveSByteMinValue = -127d;

            if (BreakUpFlow())
                return;

            if (checked((short)doubleOneFullIncrementAboveSByteMinValue) != -127)
            {
                Console.WriteLine($"'(short)-127d' was evaluated to '{(short)doubleOneFullIncrementAboveSByteMinValue}'. Expected: '-127'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderSByteMaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderSByteMaxValueCastToInt16IsFoldedCorrectly()
        {
            double doubleOneDecrementUnderSByteMaxValue = 126.99999999999999d;

            if (BreakUpFlow())
                return;

            if (checked((short)doubleOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(short)126.99999999999999d' was evaluated to '{(short)doubleOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderSByteMaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderSByteMaxValueCastToInt16IsFoldedCorrectly()
        {
            double doubleOneFullDecrementUnderSByteMaxValue = 126d;

            if (BreakUpFlow())
                return;

            if (checked((short)doubleOneFullDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(short)126d' was evaluated to '{(short)doubleOneFullDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmDoubleOneIncrementAboveSByteMaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveSByteMaxValueCastToInt16IsFoldedCorrectly()
        {
            double doubleOneIncrementAboveSByteMaxValue = 127.00000000000001d;

            if (BreakUpFlow())
                return;

            if (checked((short)doubleOneIncrementAboveSByteMaxValue) != 127)
            {
                Console.WriteLine($"'(short)127.00000000000001d' was evaluated to '{(short)doubleOneIncrementAboveSByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveSByteMaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveSByteMaxValueCastToInt16IsFoldedCorrectly()
        {
            double doubleOneFullIncrementAboveSByteMaxValue = 128d;

            if (BreakUpFlow())
                return;

            if (checked((short)doubleOneFullIncrementAboveSByteMaxValue) != 128)
            {
                Console.WriteLine($"'(short)128d' was evaluated to '{(short)doubleOneFullIncrementAboveSByteMaxValue}'. Expected: '128'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMinValueCastToInt16IsFoldedCorrectly()
        {
            double integerOneDecrementUnderSByteMinValue = -129.0;

            if (BreakUpFlow())
                return;

            if (checked((short)integerOneDecrementUnderSByteMinValue) != -129)
            {
                Console.WriteLine($"'(short)-129.0' was evaluated to '{(short)integerOneDecrementUnderSByteMinValue}'. Expected: '-129'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMinValueCastToInt16IsFoldedCorrectly()
        {
            double integerOneIncrementAboveSByteMinValue = -127.0;

            if (BreakUpFlow())
                return;

            if (checked((short)integerOneIncrementAboveSByteMinValue) != -127)
            {
                Console.WriteLine($"'(short)-127.0' was evaluated to '{(short)integerOneIncrementAboveSByteMinValue}'. Expected: '-127'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToInt16IsFoldedCorrectly()
        {
            double integerOneDecrementUnderSByteMaxValue = 126.0;

            if (BreakUpFlow())
                return;

            if (checked((short)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(short)126.0' was evaluated to '{(short)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMaxValueCastToInt16IsFoldedCorrectly()
        {
            double integerOneIncrementAboveSByteMaxValue = 128.0;

            if (BreakUpFlow())
                return;

            if (checked((short)integerOneIncrementAboveSByteMaxValue) != 128)
            {
                Console.WriteLine($"'(short)128.0' was evaluated to '{(short)integerOneIncrementAboveSByteMaxValue}'. Expected: '128'.");
                _counter++;
            }
        }
        ConfirmByteMaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmByteMaxValueCastToInt16IsFoldedCorrectly()
        {
            double byteMaxValue = 255.0;

            if (BreakUpFlow())
                return;

            if (checked((short)byteMaxValue) != 255)
            {
                Console.WriteLine($"'(short)255.0' was evaluated to '{(short)byteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderByteMinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderByteMinValueCastToInt16IsFoldedCorrectly()
        {
            double doubleOneDecrementUnderByteMinValue = -5E-324d;

            if (BreakUpFlow())
                return;

            if (checked((short)doubleOneDecrementUnderByteMinValue) != 0)
            {
                Console.WriteLine($"'(short)-5E-324d' was evaluated to '{(short)doubleOneDecrementUnderByteMinValue}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderByteMinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderByteMinValueCastToInt16IsFoldedCorrectly()
        {
            double doubleOneFullDecrementUnderByteMinValue = -1d;

            if (BreakUpFlow())
                return;

            if (checked((short)doubleOneFullDecrementUnderByteMinValue) != -1)
            {
                Console.WriteLine($"'(short)-1d' was evaluated to '{(short)doubleOneFullDecrementUnderByteMinValue}'. Expected: '-1'.");
                _counter++;
            }
        }
        ConfirmDoubleOneIncrementAboveByteMinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveByteMinValueCastToInt16IsFoldedCorrectly()
        {
            double doubleOneIncrementAboveByteMinValue = 5E-324d;

            if (BreakUpFlow())
                return;

            if (checked((short)doubleOneIncrementAboveByteMinValue) != 0)
            {
                Console.WriteLine($"'(short)5E-324d' was evaluated to '{(short)doubleOneIncrementAboveByteMinValue}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveByteMinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveByteMinValueCastToInt16IsFoldedCorrectly()
        {
            double doubleOneFullIncrementAboveByteMinValue = 1d;

            if (BreakUpFlow())
                return;

            if (checked((short)doubleOneFullIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(short)1d' was evaluated to '{(short)doubleOneFullIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderByteMaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderByteMaxValueCastToInt16IsFoldedCorrectly()
        {
            double doubleOneDecrementUnderByteMaxValue = 254.99999999999997d;

            if (BreakUpFlow())
                return;

            if (checked((short)doubleOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(short)254.99999999999997d' was evaluated to '{(short)doubleOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderByteMaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderByteMaxValueCastToInt16IsFoldedCorrectly()
        {
            double doubleOneFullDecrementUnderByteMaxValue = 254d;

            if (BreakUpFlow())
                return;

            if (checked((short)doubleOneFullDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(short)254d' was evaluated to '{(short)doubleOneFullDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmDoubleOneIncrementAboveByteMaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveByteMaxValueCastToInt16IsFoldedCorrectly()
        {
            double doubleOneIncrementAboveByteMaxValue = 255.00000000000003d;

            if (BreakUpFlow())
                return;

            if (checked((short)doubleOneIncrementAboveByteMaxValue) != 255)
            {
                Console.WriteLine($"'(short)255.00000000000003d' was evaluated to '{(short)doubleOneIncrementAboveByteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveByteMaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveByteMaxValueCastToInt16IsFoldedCorrectly()
        {
            double doubleOneFullIncrementAboveByteMaxValue = 256d;

            if (BreakUpFlow())
                return;

            if (checked((short)doubleOneFullIncrementAboveByteMaxValue) != 256)
            {
                Console.WriteLine($"'(short)256d' was evaluated to '{(short)doubleOneFullIncrementAboveByteMaxValue}'. Expected: '256'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMinValueCastToInt16IsFoldedCorrectly()
        {
            double integerOneDecrementUnderByteMinValue = -1.0;

            if (BreakUpFlow())
                return;

            if (checked((short)integerOneDecrementUnderByteMinValue) != -1)
            {
                Console.WriteLine($"'(short)-1.0' was evaluated to '{(short)integerOneDecrementUnderByteMinValue}'. Expected: '-1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToInt16IsFoldedCorrectly()
        {
            double integerOneIncrementAboveByteMinValue = 1.0;

            if (BreakUpFlow())
                return;

            if (checked((short)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(short)1.0' was evaluated to '{(short)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMaxValueCastToInt16IsFoldedCorrectly()
        {
            double integerOneDecrementUnderByteMaxValue = 254.0;

            if (BreakUpFlow())
                return;

            if (checked((short)integerOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(short)254.0' was evaluated to '{(short)integerOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMaxValueCastToInt16IsFoldedCorrectly()
        {
            double integerOneIncrementAboveByteMaxValue = 256.0;

            if (BreakUpFlow())
                return;

            if (checked((short)integerOneIncrementAboveByteMaxValue) != 256)
            {
                Console.WriteLine($"'(short)256.0' was evaluated to '{(short)integerOneIncrementAboveByteMaxValue}'. Expected: '256'.");
                _counter++;
            }
        }
        ConfirmInt16MinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MinValueCastToInt16IsFoldedCorrectly()
        {
            double int16MinValue = -32768.0;

            if (BreakUpFlow())
                return;

            if (checked((short)int16MinValue) != -32768)
            {
                Console.WriteLine($"'(short)-32768.0' was evaluated to '{(short)int16MinValue}'. Expected: '-32768'.");
                _counter++;
            }
        }
        ConfirmInt16MaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MaxValueCastToInt16IsFoldedCorrectly()
        {
            double int16MaxValue = 32767.0;

            if (BreakUpFlow())
                return;

            if (checked((short)int16MaxValue) != 32767)
            {
                Console.WriteLine($"'(short)32767.0' was evaluated to '{(short)int16MaxValue}'. Expected: '32767'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderInt16MinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderInt16MinValueCastToInt16IsFoldedCorrectly()
        {
            double doubleOneDecrementUnderInt16MinValue = -32768.00000000001d;

            if (BreakUpFlow())
                return;

            if (checked((short)doubleOneDecrementUnderInt16MinValue) != -32768)
            {
                Console.WriteLine($"'(short)-32768.00000000001d' was evaluated to '{(short)doubleOneDecrementUnderInt16MinValue}'. Expected: '-32768'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderInt16MinValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderInt16MinValueCastToInt16Overflows()
        {
            double from = -32769d;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)-32769d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveInt16MinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveInt16MinValueCastToInt16IsFoldedCorrectly()
        {
            double doubleOneIncrementAboveInt16MinValue = -32767.999999999996d;

            if (BreakUpFlow())
                return;

            if (checked((short)doubleOneIncrementAboveInt16MinValue) != -32767)
            {
                Console.WriteLine($"'(short)-32767.999999999996d' was evaluated to '{(short)doubleOneIncrementAboveInt16MinValue}'. Expected: '-32767'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveInt16MinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveInt16MinValueCastToInt16IsFoldedCorrectly()
        {
            double doubleOneFullIncrementAboveInt16MinValue = -32767d;

            if (BreakUpFlow())
                return;

            if (checked((short)doubleOneFullIncrementAboveInt16MinValue) != -32767)
            {
                Console.WriteLine($"'(short)-32767d' was evaluated to '{(short)doubleOneFullIncrementAboveInt16MinValue}'. Expected: '-32767'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderInt16MaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderInt16MaxValueCastToInt16IsFoldedCorrectly()
        {
            double doubleOneDecrementUnderInt16MaxValue = 32766.999999999996d;

            if (BreakUpFlow())
                return;

            if (checked((short)doubleOneDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(short)32766.999999999996d' was evaluated to '{(short)doubleOneDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderInt16MaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderInt16MaxValueCastToInt16IsFoldedCorrectly()
        {
            double doubleOneFullDecrementUnderInt16MaxValue = 32766d;

            if (BreakUpFlow())
                return;

            if (checked((short)doubleOneFullDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(short)32766d' was evaluated to '{(short)doubleOneFullDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
        ConfirmDoubleOneIncrementAboveInt16MaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveInt16MaxValueCastToInt16IsFoldedCorrectly()
        {
            double doubleOneIncrementAboveInt16MaxValue = 32767.000000000004d;

            if (BreakUpFlow())
                return;

            if (checked((short)doubleOneIncrementAboveInt16MaxValue) != 32767)
            {
                Console.WriteLine($"'(short)32767.000000000004d' was evaluated to '{(short)doubleOneIncrementAboveInt16MaxValue}'. Expected: '32767'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveInt16MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveInt16MaxValueCastToInt16Overflows()
        {
            double from = 32768d;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)32768d)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt16MinValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MinValueCastToInt16Overflows()
        {
            double from = -32769.0;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)-32769.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt16MinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt16MinValueCastToInt16IsFoldedCorrectly()
        {
            double integerOneIncrementAboveInt16MinValue = -32767.0;

            if (BreakUpFlow())
                return;

            if (checked((short)integerOneIncrementAboveInt16MinValue) != -32767)
            {
                Console.WriteLine($"'(short)-32767.0' was evaluated to '{(short)integerOneIncrementAboveInt16MinValue}'. Expected: '-32767'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt16MaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MaxValueCastToInt16IsFoldedCorrectly()
        {
            double integerOneDecrementUnderInt16MaxValue = 32766.0;

            if (BreakUpFlow())
                return;

            if (checked((short)integerOneDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(short)32766.0' was evaluated to '{(short)integerOneDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt16MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt16MaxValueCastToInt16Overflows()
        {
            double from = 32768.0;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)32768.0)' did not throw OverflowException.");
        }
        ConfirmUInt16MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt16MaxValueCastToInt16Overflows()
        {
            double from = 65535.0;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)65535.0)' did not throw OverflowException.");
        }
        ConfirmDoubleOneDecrementUnderUInt16MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderUInt16MaxValueCastToInt16Overflows()
        {
            double from = 65534.99999999999d;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)65534.99999999999d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullDecrementUnderUInt16MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderUInt16MaxValueCastToInt16Overflows()
        {
            double from = 65534d;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)65534d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveUInt16MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveUInt16MaxValueCastToInt16Overflows()
        {
            double from = 65535.00000000001d;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)65535.00000000001d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullIncrementAboveUInt16MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveUInt16MaxValueCastToInt16Overflows()
        {
            double from = 65536d;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)65536d)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderUInt16MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt16MaxValueCastToInt16Overflows()
        {
            double from = 65534.0;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)65534.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToInt16Overflows()
        {
            double from = 65536.0;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)65536.0)' did not throw OverflowException.");
        }
        ConfirmInt32MinValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MinValueCastToInt16Overflows()
        {
            double from = -2147483648.0;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)-2147483648.0)' did not throw OverflowException.");
        }
        ConfirmInt32MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MaxValueCastToInt16Overflows()
        {
            double from = 2147483647.0;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)2147483647.0)' did not throw OverflowException.");
        }
        ConfirmDoubleOneDecrementUnderInt32MinValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderInt32MinValueCastToInt16Overflows()
        {
            double from = -2147483648.0000005d;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)-2147483648.0000005d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullDecrementUnderInt32MinValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderInt32MinValueCastToInt16Overflows()
        {
            double from = -2147483649d;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)-2147483649d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveInt32MinValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveInt32MinValueCastToInt16Overflows()
        {
            double from = -2147483647.9999998d;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)-2147483647.9999998d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullIncrementAboveInt32MinValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveInt32MinValueCastToInt16Overflows()
        {
            double from = -2147483647d;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)-2147483647d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneDecrementUnderInt32MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderInt32MaxValueCastToInt16Overflows()
        {
            double from = 2147483646.9999998d;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)2147483646.9999998d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullDecrementUnderInt32MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderInt32MaxValueCastToInt16Overflows()
        {
            double from = 2147483646d;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)2147483646d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveInt32MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveInt32MaxValueCastToInt16Overflows()
        {
            double from = 2147483647.0000002d;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)2147483647.0000002d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullIncrementAboveInt32MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveInt32MaxValueCastToInt16Overflows()
        {
            double from = 2147483648d;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)2147483648d)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt32MinValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt32MinValueCastToInt16Overflows()
        {
            double from = -2147483649.0;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)-2147483649.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt32MinValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt32MinValueCastToInt16Overflows()
        {
            double from = -2147483647.0;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)-2147483647.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt32MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt32MaxValueCastToInt16Overflows()
        {
            double from = 2147483646.0;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)2147483646.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt32MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt32MaxValueCastToInt16Overflows()
        {
            double from = 2147483648.0;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)2147483648.0)' did not throw OverflowException.");
        }
        ConfirmUInt32MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32MaxValueCastToInt16Overflows()
        {
            double from = 4294967295.0;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)4294967295.0)' did not throw OverflowException.");
        }
        ConfirmDoubleOneDecrementUnderUInt32MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderUInt32MaxValueCastToInt16Overflows()
        {
            double from = 4294967294.9999995d;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)4294967294.9999995d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullDecrementUnderUInt32MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderUInt32MaxValueCastToInt16Overflows()
        {
            double from = 4294967294d;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)4294967294d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveUInt32MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveUInt32MaxValueCastToInt16Overflows()
        {
            double from = 4294967295.0000005d;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)4294967295.0000005d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullIncrementAboveUInt32MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveUInt32MaxValueCastToInt16Overflows()
        {
            double from = 4294967296d;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)4294967296d)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderUInt32MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt32MaxValueCastToInt16Overflows()
        {
            double from = 4294967294.0;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)4294967294.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveUInt32MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt32MaxValueCastToInt16Overflows()
        {
            double from = 4294967296.0;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)4294967296.0)' did not throw OverflowException.");
        }
        ConfirmInt64MinValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MinValueCastToInt16Overflows()
        {
            double from = -9223372036854775808.0;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)-9223372036854775808.0)' did not throw OverflowException.");
        }
        ConfirmInt64MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MaxValueCastToInt16Overflows()
        {
            double from = 9223372036854775807.0;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)9223372036854775807.0)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveInt64MinValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveInt64MinValueCastToInt16Overflows()
        {
            double from = -9.223372036854775E+18d;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)-9.223372036854775E+18d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneDecrementUnderInt64MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderInt64MaxValueCastToInt16Overflows()
        {
            double from = 9.223372036854775E+18d;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)9.223372036854775E+18d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveInt64MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveInt64MaxValueCastToInt16Overflows()
        {
            double from = 9.223372036854778E+18d;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)9.223372036854778E+18d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullIncrementAboveInt64MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveInt64MaxValueCastToInt16Overflows()
        {
            double from = 9.223372036854776E+18d;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)9.223372036854776E+18d)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt64MinValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt64MinValueCastToInt16Overflows()
        {
            double from = -9223372036854775807.0;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)-9223372036854775807.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt64MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt64MaxValueCastToInt16Overflows()
        {
            double from = 9223372036854775806.0;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)9223372036854775806.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt64MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt64MaxValueCastToInt16Overflows()
        {
            double from = 9223372036854775808.0;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)9223372036854775808.0)' did not throw OverflowException.");
        }
        ConfirmUInt64MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64MaxValueCastToInt16Overflows()
        {
            double from = 18446744073709551615.0;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)18446744073709551615.0)' did not throw OverflowException.");
        }
        ConfirmDoubleOneDecrementUnderUInt64MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderUInt64MaxValueCastToInt16Overflows()
        {
            double from = 1.844674407370955E+19d;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)1.844674407370955E+19d)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderUInt64MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt64MaxValueCastToInt16Overflows()
        {
            double from = 18446744073709551614.0;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)18446744073709551614.0)' did not throw OverflowException.");
        }
    }

    private static void TestCastingDoubleToUInt16()
    {
        ConfirmIntegerZeroCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToUInt16IsFoldedCorrectly()
        {
            double integerZero = 0.0;

            if (BreakUpFlow())
                return;

            if (checked((ushort)integerZero) != 0)
            {
                Console.WriteLine($"'(ushort)0.0' was evaluated to '{(ushort)integerZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmDoubleMinusZeroCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleMinusZeroCastToUInt16IsFoldedCorrectly()
        {
            double doubleMinusZero = -0d;

            if (BreakUpFlow())
                return;

            if (checked((ushort)doubleMinusZero) != 0)
            {
                Console.WriteLine($"'(ushort)-0d' was evaluated to '{(ushort)doubleMinusZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmDoubleZeroCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleZeroCastToUInt16IsFoldedCorrectly()
        {
            double doubleZero = 0d;

            if (BreakUpFlow())
                return;

            if (checked((ushort)doubleZero) != 0)
            {
                Console.WriteLine($"'(ushort)0d' was evaluated to '{(ushort)doubleZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmDoubleMinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleMinValueCastToUInt16Overflows()
        {
            double from = -1.7976931348623157E+308d;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-1.7976931348623157E+308d)' did not throw OverflowException.");
        }
        ConfirmDoubleMaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleMaxValueCastToUInt16Overflows()
        {
            double from = 1.7976931348623157E+308d;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)1.7976931348623157E+308d)' did not throw OverflowException.");
        }
        ConfirmSByteMinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMinValueCastToUInt16Overflows()
        {
            double from = -128.0;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-128.0)' did not throw OverflowException.");
        }
        ConfirmSByteMaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToUInt16IsFoldedCorrectly()
        {
            double sByteMaxValue = 127.0;

            if (BreakUpFlow())
                return;

            if (checked((ushort)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(ushort)127.0' was evaluated to '{(ushort)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderSByteMinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderSByteMinValueCastToUInt16Overflows()
        {
            double from = -128.00000000000003d;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-128.00000000000003d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullDecrementUnderSByteMinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderSByteMinValueCastToUInt16Overflows()
        {
            double from = -129d;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-129d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveSByteMinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveSByteMinValueCastToUInt16Overflows()
        {
            double from = -127.99999999999999d;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-127.99999999999999d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullIncrementAboveSByteMinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveSByteMinValueCastToUInt16Overflows()
        {
            double from = -127d;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-127d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneDecrementUnderSByteMaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderSByteMaxValueCastToUInt16IsFoldedCorrectly()
        {
            double doubleOneDecrementUnderSByteMaxValue = 126.99999999999999d;

            if (BreakUpFlow())
                return;

            if (checked((ushort)doubleOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(ushort)126.99999999999999d' was evaluated to '{(ushort)doubleOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderSByteMaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderSByteMaxValueCastToUInt16IsFoldedCorrectly()
        {
            double doubleOneFullDecrementUnderSByteMaxValue = 126d;

            if (BreakUpFlow())
                return;

            if (checked((ushort)doubleOneFullDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(ushort)126d' was evaluated to '{(ushort)doubleOneFullDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmDoubleOneIncrementAboveSByteMaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveSByteMaxValueCastToUInt16IsFoldedCorrectly()
        {
            double doubleOneIncrementAboveSByteMaxValue = 127.00000000000001d;

            if (BreakUpFlow())
                return;

            if (checked((ushort)doubleOneIncrementAboveSByteMaxValue) != 127)
            {
                Console.WriteLine($"'(ushort)127.00000000000001d' was evaluated to '{(ushort)doubleOneIncrementAboveSByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveSByteMaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveSByteMaxValueCastToUInt16IsFoldedCorrectly()
        {
            double doubleOneFullIncrementAboveSByteMaxValue = 128d;

            if (BreakUpFlow())
                return;

            if (checked((ushort)doubleOneFullIncrementAboveSByteMaxValue) != 128)
            {
                Console.WriteLine($"'(ushort)128d' was evaluated to '{(ushort)doubleOneFullIncrementAboveSByteMaxValue}'. Expected: '128'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMinValueCastToUInt16Overflows()
        {
            double from = -129.0;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-129.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveSByteMinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMinValueCastToUInt16Overflows()
        {
            double from = -127.0;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-127.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToUInt16IsFoldedCorrectly()
        {
            double integerOneDecrementUnderSByteMaxValue = 126.0;

            if (BreakUpFlow())
                return;

            if (checked((ushort)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(ushort)126.0' was evaluated to '{(ushort)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMaxValueCastToUInt16IsFoldedCorrectly()
        {
            double integerOneIncrementAboveSByteMaxValue = 128.0;

            if (BreakUpFlow())
                return;

            if (checked((ushort)integerOneIncrementAboveSByteMaxValue) != 128)
            {
                Console.WriteLine($"'(ushort)128.0' was evaluated to '{(ushort)integerOneIncrementAboveSByteMaxValue}'. Expected: '128'.");
                _counter++;
            }
        }
        ConfirmByteMaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmByteMaxValueCastToUInt16IsFoldedCorrectly()
        {
            double byteMaxValue = 255.0;

            if (BreakUpFlow())
                return;

            if (checked((ushort)byteMaxValue) != 255)
            {
                Console.WriteLine($"'(ushort)255.0' was evaluated to '{(ushort)byteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderByteMinValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderByteMinValueCastToUInt16IsFoldedCorrectly()
        {
            double doubleOneDecrementUnderByteMinValue = -5E-324d;

            if (BreakUpFlow())
                return;

            if (checked((ushort)doubleOneDecrementUnderByteMinValue) != 0)
            {
                Console.WriteLine($"'(ushort)-5E-324d' was evaluated to '{(ushort)doubleOneDecrementUnderByteMinValue}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderByteMinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderByteMinValueCastToUInt16Overflows()
        {
            double from = -1d;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-1d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveByteMinValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveByteMinValueCastToUInt16IsFoldedCorrectly()
        {
            double doubleOneIncrementAboveByteMinValue = 5E-324d;

            if (BreakUpFlow())
                return;

            if (checked((ushort)doubleOneIncrementAboveByteMinValue) != 0)
            {
                Console.WriteLine($"'(ushort)5E-324d' was evaluated to '{(ushort)doubleOneIncrementAboveByteMinValue}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveByteMinValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveByteMinValueCastToUInt16IsFoldedCorrectly()
        {
            double doubleOneFullIncrementAboveByteMinValue = 1d;

            if (BreakUpFlow())
                return;

            if (checked((ushort)doubleOneFullIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(ushort)1d' was evaluated to '{(ushort)doubleOneFullIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderByteMaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderByteMaxValueCastToUInt16IsFoldedCorrectly()
        {
            double doubleOneDecrementUnderByteMaxValue = 254.99999999999997d;

            if (BreakUpFlow())
                return;

            if (checked((ushort)doubleOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(ushort)254.99999999999997d' was evaluated to '{(ushort)doubleOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderByteMaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderByteMaxValueCastToUInt16IsFoldedCorrectly()
        {
            double doubleOneFullDecrementUnderByteMaxValue = 254d;

            if (BreakUpFlow())
                return;

            if (checked((ushort)doubleOneFullDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(ushort)254d' was evaluated to '{(ushort)doubleOneFullDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmDoubleOneIncrementAboveByteMaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveByteMaxValueCastToUInt16IsFoldedCorrectly()
        {
            double doubleOneIncrementAboveByteMaxValue = 255.00000000000003d;

            if (BreakUpFlow())
                return;

            if (checked((ushort)doubleOneIncrementAboveByteMaxValue) != 255)
            {
                Console.WriteLine($"'(ushort)255.00000000000003d' was evaluated to '{(ushort)doubleOneIncrementAboveByteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveByteMaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveByteMaxValueCastToUInt16IsFoldedCorrectly()
        {
            double doubleOneFullIncrementAboveByteMaxValue = 256d;

            if (BreakUpFlow())
                return;

            if (checked((ushort)doubleOneFullIncrementAboveByteMaxValue) != 256)
            {
                Console.WriteLine($"'(ushort)256d' was evaluated to '{(ushort)doubleOneFullIncrementAboveByteMaxValue}'. Expected: '256'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMinValueCastToUInt16Overflows()
        {
            double from = -1.0;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-1.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToUInt16IsFoldedCorrectly()
        {
            double integerOneIncrementAboveByteMinValue = 1.0;

            if (BreakUpFlow())
                return;

            if (checked((ushort)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(ushort)1.0' was evaluated to '{(ushort)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMaxValueCastToUInt16IsFoldedCorrectly()
        {
            double integerOneDecrementUnderByteMaxValue = 254.0;

            if (BreakUpFlow())
                return;

            if (checked((ushort)integerOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(ushort)254.0' was evaluated to '{(ushort)integerOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMaxValueCastToUInt16IsFoldedCorrectly()
        {
            double integerOneIncrementAboveByteMaxValue = 256.0;

            if (BreakUpFlow())
                return;

            if (checked((ushort)integerOneIncrementAboveByteMaxValue) != 256)
            {
                Console.WriteLine($"'(ushort)256.0' was evaluated to '{(ushort)integerOneIncrementAboveByteMaxValue}'. Expected: '256'.");
                _counter++;
            }
        }
        ConfirmInt16MinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MinValueCastToUInt16Overflows()
        {
            double from = -32768.0;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-32768.0)' did not throw OverflowException.");
        }
        ConfirmInt16MaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MaxValueCastToUInt16IsFoldedCorrectly()
        {
            double int16MaxValue = 32767.0;

            if (BreakUpFlow())
                return;

            if (checked((ushort)int16MaxValue) != 32767)
            {
                Console.WriteLine($"'(ushort)32767.0' was evaluated to '{(ushort)int16MaxValue}'. Expected: '32767'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderInt16MinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderInt16MinValueCastToUInt16Overflows()
        {
            double from = -32768.00000000001d;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-32768.00000000001d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullDecrementUnderInt16MinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderInt16MinValueCastToUInt16Overflows()
        {
            double from = -32769d;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-32769d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveInt16MinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveInt16MinValueCastToUInt16Overflows()
        {
            double from = -32767.999999999996d;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-32767.999999999996d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullIncrementAboveInt16MinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveInt16MinValueCastToUInt16Overflows()
        {
            double from = -32767d;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-32767d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneDecrementUnderInt16MaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderInt16MaxValueCastToUInt16IsFoldedCorrectly()
        {
            double doubleOneDecrementUnderInt16MaxValue = 32766.999999999996d;

            if (BreakUpFlow())
                return;

            if (checked((ushort)doubleOneDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(ushort)32766.999999999996d' was evaluated to '{(ushort)doubleOneDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderInt16MaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderInt16MaxValueCastToUInt16IsFoldedCorrectly()
        {
            double doubleOneFullDecrementUnderInt16MaxValue = 32766d;

            if (BreakUpFlow())
                return;

            if (checked((ushort)doubleOneFullDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(ushort)32766d' was evaluated to '{(ushort)doubleOneFullDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
        ConfirmDoubleOneIncrementAboveInt16MaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveInt16MaxValueCastToUInt16IsFoldedCorrectly()
        {
            double doubleOneIncrementAboveInt16MaxValue = 32767.000000000004d;

            if (BreakUpFlow())
                return;

            if (checked((ushort)doubleOneIncrementAboveInt16MaxValue) != 32767)
            {
                Console.WriteLine($"'(ushort)32767.000000000004d' was evaluated to '{(ushort)doubleOneIncrementAboveInt16MaxValue}'. Expected: '32767'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveInt16MaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveInt16MaxValueCastToUInt16IsFoldedCorrectly()
        {
            double doubleOneFullIncrementAboveInt16MaxValue = 32768d;

            if (BreakUpFlow())
                return;

            if (checked((ushort)doubleOneFullIncrementAboveInt16MaxValue) != 32768)
            {
                Console.WriteLine($"'(ushort)32768d' was evaluated to '{(ushort)doubleOneFullIncrementAboveInt16MaxValue}'. Expected: '32768'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt16MinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MinValueCastToUInt16Overflows()
        {
            double from = -32769.0;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-32769.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt16MinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt16MinValueCastToUInt16Overflows()
        {
            double from = -32767.0;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-32767.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt16MaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MaxValueCastToUInt16IsFoldedCorrectly()
        {
            double integerOneDecrementUnderInt16MaxValue = 32766.0;

            if (BreakUpFlow())
                return;

            if (checked((ushort)integerOneDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(ushort)32766.0' was evaluated to '{(ushort)integerOneDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt16MaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt16MaxValueCastToUInt16IsFoldedCorrectly()
        {
            double integerOneIncrementAboveInt16MaxValue = 32768.0;

            if (BreakUpFlow())
                return;

            if (checked((ushort)integerOneIncrementAboveInt16MaxValue) != 32768)
            {
                Console.WriteLine($"'(ushort)32768.0' was evaluated to '{(ushort)integerOneIncrementAboveInt16MaxValue}'. Expected: '32768'.");
                _counter++;
            }
        }
        ConfirmUInt16MaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt16MaxValueCastToUInt16IsFoldedCorrectly()
        {
            double uInt16MaxValue = 65535.0;

            if (BreakUpFlow())
                return;

            if (checked((ushort)uInt16MaxValue) != 65535)
            {
                Console.WriteLine($"'(ushort)65535.0' was evaluated to '{(ushort)uInt16MaxValue}'. Expected: '65535'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderUInt16MaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderUInt16MaxValueCastToUInt16IsFoldedCorrectly()
        {
            double doubleOneDecrementUnderUInt16MaxValue = 65534.99999999999d;

            if (BreakUpFlow())
                return;

            if (checked((ushort)doubleOneDecrementUnderUInt16MaxValue) != 65534)
            {
                Console.WriteLine($"'(ushort)65534.99999999999d' was evaluated to '{(ushort)doubleOneDecrementUnderUInt16MaxValue}'. Expected: '65534'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderUInt16MaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderUInt16MaxValueCastToUInt16IsFoldedCorrectly()
        {
            double doubleOneFullDecrementUnderUInt16MaxValue = 65534d;

            if (BreakUpFlow())
                return;

            if (checked((ushort)doubleOneFullDecrementUnderUInt16MaxValue) != 65534)
            {
                Console.WriteLine($"'(ushort)65534d' was evaluated to '{(ushort)doubleOneFullDecrementUnderUInt16MaxValue}'. Expected: '65534'.");
                _counter++;
            }
        }
        ConfirmDoubleOneIncrementAboveUInt16MaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveUInt16MaxValueCastToUInt16IsFoldedCorrectly()
        {
            double doubleOneIncrementAboveUInt16MaxValue = 65535.00000000001d;

            if (BreakUpFlow())
                return;

            if (checked((ushort)doubleOneIncrementAboveUInt16MaxValue) != 65535)
            {
                Console.WriteLine($"'(ushort)65535.00000000001d' was evaluated to '{(ushort)doubleOneIncrementAboveUInt16MaxValue}'. Expected: '65535'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveUInt16MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveUInt16MaxValueCastToUInt16Overflows()
        {
            double from = 65536d;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)65536d)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderUInt16MaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt16MaxValueCastToUInt16IsFoldedCorrectly()
        {
            double integerOneDecrementUnderUInt16MaxValue = 65534.0;

            if (BreakUpFlow())
                return;

            if (checked((ushort)integerOneDecrementUnderUInt16MaxValue) != 65534)
            {
                Console.WriteLine($"'(ushort)65534.0' was evaluated to '{(ushort)integerOneDecrementUnderUInt16MaxValue}'. Expected: '65534'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToUInt16Overflows()
        {
            double from = 65536.0;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)65536.0)' did not throw OverflowException.");
        }
        ConfirmInt32MinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MinValueCastToUInt16Overflows()
        {
            double from = -2147483648.0;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-2147483648.0)' did not throw OverflowException.");
        }
        ConfirmInt32MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MaxValueCastToUInt16Overflows()
        {
            double from = 2147483647.0;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)2147483647.0)' did not throw OverflowException.");
        }
        ConfirmDoubleOneDecrementUnderInt32MinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderInt32MinValueCastToUInt16Overflows()
        {
            double from = -2147483648.0000005d;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-2147483648.0000005d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullDecrementUnderInt32MinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderInt32MinValueCastToUInt16Overflows()
        {
            double from = -2147483649d;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-2147483649d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveInt32MinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveInt32MinValueCastToUInt16Overflows()
        {
            double from = -2147483647.9999998d;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-2147483647.9999998d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullIncrementAboveInt32MinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveInt32MinValueCastToUInt16Overflows()
        {
            double from = -2147483647d;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-2147483647d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneDecrementUnderInt32MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderInt32MaxValueCastToUInt16Overflows()
        {
            double from = 2147483646.9999998d;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)2147483646.9999998d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullDecrementUnderInt32MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderInt32MaxValueCastToUInt16Overflows()
        {
            double from = 2147483646d;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)2147483646d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveInt32MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveInt32MaxValueCastToUInt16Overflows()
        {
            double from = 2147483647.0000002d;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)2147483647.0000002d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullIncrementAboveInt32MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveInt32MaxValueCastToUInt16Overflows()
        {
            double from = 2147483648d;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)2147483648d)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt32MinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt32MinValueCastToUInt16Overflows()
        {
            double from = -2147483649.0;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-2147483649.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt32MinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt32MinValueCastToUInt16Overflows()
        {
            double from = -2147483647.0;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-2147483647.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt32MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt32MaxValueCastToUInt16Overflows()
        {
            double from = 2147483646.0;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)2147483646.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt32MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt32MaxValueCastToUInt16Overflows()
        {
            double from = 2147483648.0;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)2147483648.0)' did not throw OverflowException.");
        }
        ConfirmUInt32MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32MaxValueCastToUInt16Overflows()
        {
            double from = 4294967295.0;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)4294967295.0)' did not throw OverflowException.");
        }
        ConfirmDoubleOneDecrementUnderUInt32MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderUInt32MaxValueCastToUInt16Overflows()
        {
            double from = 4294967294.9999995d;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)4294967294.9999995d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullDecrementUnderUInt32MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderUInt32MaxValueCastToUInt16Overflows()
        {
            double from = 4294967294d;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)4294967294d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveUInt32MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveUInt32MaxValueCastToUInt16Overflows()
        {
            double from = 4294967295.0000005d;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)4294967295.0000005d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullIncrementAboveUInt32MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveUInt32MaxValueCastToUInt16Overflows()
        {
            double from = 4294967296d;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)4294967296d)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderUInt32MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt32MaxValueCastToUInt16Overflows()
        {
            double from = 4294967294.0;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)4294967294.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveUInt32MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt32MaxValueCastToUInt16Overflows()
        {
            double from = 4294967296.0;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)4294967296.0)' did not throw OverflowException.");
        }
        ConfirmInt64MinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MinValueCastToUInt16Overflows()
        {
            double from = -9223372036854775808.0;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-9223372036854775808.0)' did not throw OverflowException.");
        }
        ConfirmInt64MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MaxValueCastToUInt16Overflows()
        {
            double from = 9223372036854775807.0;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)9223372036854775807.0)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveInt64MinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveInt64MinValueCastToUInt16Overflows()
        {
            double from = -9.223372036854775E+18d;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-9.223372036854775E+18d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneDecrementUnderInt64MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderInt64MaxValueCastToUInt16Overflows()
        {
            double from = 9.223372036854775E+18d;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)9.223372036854775E+18d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveInt64MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveInt64MaxValueCastToUInt16Overflows()
        {
            double from = 9.223372036854778E+18d;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)9.223372036854778E+18d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullIncrementAboveInt64MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveInt64MaxValueCastToUInt16Overflows()
        {
            double from = 9.223372036854776E+18d;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)9.223372036854776E+18d)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt64MinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt64MinValueCastToUInt16Overflows()
        {
            double from = -9223372036854775807.0;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-9223372036854775807.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt64MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt64MaxValueCastToUInt16Overflows()
        {
            double from = 9223372036854775806.0;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)9223372036854775806.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt64MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt64MaxValueCastToUInt16Overflows()
        {
            double from = 9223372036854775808.0;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)9223372036854775808.0)' did not throw OverflowException.");
        }
        ConfirmUInt64MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64MaxValueCastToUInt16Overflows()
        {
            double from = 18446744073709551615.0;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)18446744073709551615.0)' did not throw OverflowException.");
        }
        ConfirmDoubleOneDecrementUnderUInt64MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderUInt64MaxValueCastToUInt16Overflows()
        {
            double from = 1.844674407370955E+19d;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)1.844674407370955E+19d)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderUInt64MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt64MaxValueCastToUInt16Overflows()
        {
            double from = 18446744073709551614.0;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)18446744073709551614.0)' did not throw OverflowException.");
        }
    }

    private static void TestCastingDoubleToInt32()
    {
        ConfirmIntegerZeroCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToInt32IsFoldedCorrectly()
        {
            double integerZero = 0.0;

            if (BreakUpFlow())
                return;

            if (checked((int)integerZero) != 0)
            {
                Console.WriteLine($"'(int)0.0' was evaluated to '{(int)integerZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmDoubleMinusZeroCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleMinusZeroCastToInt32IsFoldedCorrectly()
        {
            double doubleMinusZero = -0d;

            if (BreakUpFlow())
                return;

            if (checked((int)doubleMinusZero) != 0)
            {
                Console.WriteLine($"'(int)-0d' was evaluated to '{(int)doubleMinusZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmDoubleZeroCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleZeroCastToInt32IsFoldedCorrectly()
        {
            double doubleZero = 0d;

            if (BreakUpFlow())
                return;

            if (checked((int)doubleZero) != 0)
            {
                Console.WriteLine($"'(int)0d' was evaluated to '{(int)doubleZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmDoubleMinValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleMinValueCastToInt32Overflows()
        {
            double from = -1.7976931348623157E+308d;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)-1.7976931348623157E+308d)' did not throw OverflowException.");
        }
        ConfirmDoubleMaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleMaxValueCastToInt32Overflows()
        {
            double from = 1.7976931348623157E+308d;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)1.7976931348623157E+308d)' did not throw OverflowException.");
        }
        ConfirmSByteMinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMinValueCastToInt32IsFoldedCorrectly()
        {
            double sByteMinValue = -128.0;

            if (BreakUpFlow())
                return;

            if (checked((int)sByteMinValue) != -128)
            {
                Console.WriteLine($"'(int)-128.0' was evaluated to '{(int)sByteMinValue}'. Expected: '-128'.");
                _counter++;
            }
        }
        ConfirmSByteMaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToInt32IsFoldedCorrectly()
        {
            double sByteMaxValue = 127.0;

            if (BreakUpFlow())
                return;

            if (checked((int)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(int)127.0' was evaluated to '{(int)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderSByteMinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderSByteMinValueCastToInt32IsFoldedCorrectly()
        {
            double doubleOneDecrementUnderSByteMinValue = -128.00000000000003d;

            if (BreakUpFlow())
                return;

            if (checked((int)doubleOneDecrementUnderSByteMinValue) != -128)
            {
                Console.WriteLine($"'(int)-128.00000000000003d' was evaluated to '{(int)doubleOneDecrementUnderSByteMinValue}'. Expected: '-128'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderSByteMinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderSByteMinValueCastToInt32IsFoldedCorrectly()
        {
            double doubleOneFullDecrementUnderSByteMinValue = -129d;

            if (BreakUpFlow())
                return;

            if (checked((int)doubleOneFullDecrementUnderSByteMinValue) != -129)
            {
                Console.WriteLine($"'(int)-129d' was evaluated to '{(int)doubleOneFullDecrementUnderSByteMinValue}'. Expected: '-129'.");
                _counter++;
            }
        }
        ConfirmDoubleOneIncrementAboveSByteMinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveSByteMinValueCastToInt32IsFoldedCorrectly()
        {
            double doubleOneIncrementAboveSByteMinValue = -127.99999999999999d;

            if (BreakUpFlow())
                return;

            if (checked((int)doubleOneIncrementAboveSByteMinValue) != -127)
            {
                Console.WriteLine($"'(int)-127.99999999999999d' was evaluated to '{(int)doubleOneIncrementAboveSByteMinValue}'. Expected: '-127'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveSByteMinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveSByteMinValueCastToInt32IsFoldedCorrectly()
        {
            double doubleOneFullIncrementAboveSByteMinValue = -127d;

            if (BreakUpFlow())
                return;

            if (checked((int)doubleOneFullIncrementAboveSByteMinValue) != -127)
            {
                Console.WriteLine($"'(int)-127d' was evaluated to '{(int)doubleOneFullIncrementAboveSByteMinValue}'. Expected: '-127'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderSByteMaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderSByteMaxValueCastToInt32IsFoldedCorrectly()
        {
            double doubleOneDecrementUnderSByteMaxValue = 126.99999999999999d;

            if (BreakUpFlow())
                return;

            if (checked((int)doubleOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(int)126.99999999999999d' was evaluated to '{(int)doubleOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderSByteMaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderSByteMaxValueCastToInt32IsFoldedCorrectly()
        {
            double doubleOneFullDecrementUnderSByteMaxValue = 126d;

            if (BreakUpFlow())
                return;

            if (checked((int)doubleOneFullDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(int)126d' was evaluated to '{(int)doubleOneFullDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmDoubleOneIncrementAboveSByteMaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveSByteMaxValueCastToInt32IsFoldedCorrectly()
        {
            double doubleOneIncrementAboveSByteMaxValue = 127.00000000000001d;

            if (BreakUpFlow())
                return;

            if (checked((int)doubleOneIncrementAboveSByteMaxValue) != 127)
            {
                Console.WriteLine($"'(int)127.00000000000001d' was evaluated to '{(int)doubleOneIncrementAboveSByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveSByteMaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveSByteMaxValueCastToInt32IsFoldedCorrectly()
        {
            double doubleOneFullIncrementAboveSByteMaxValue = 128d;

            if (BreakUpFlow())
                return;

            if (checked((int)doubleOneFullIncrementAboveSByteMaxValue) != 128)
            {
                Console.WriteLine($"'(int)128d' was evaluated to '{(int)doubleOneFullIncrementAboveSByteMaxValue}'. Expected: '128'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMinValueCastToInt32IsFoldedCorrectly()
        {
            double integerOneDecrementUnderSByteMinValue = -129.0;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneDecrementUnderSByteMinValue) != -129)
            {
                Console.WriteLine($"'(int)-129.0' was evaluated to '{(int)integerOneDecrementUnderSByteMinValue}'. Expected: '-129'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMinValueCastToInt32IsFoldedCorrectly()
        {
            double integerOneIncrementAboveSByteMinValue = -127.0;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneIncrementAboveSByteMinValue) != -127)
            {
                Console.WriteLine($"'(int)-127.0' was evaluated to '{(int)integerOneIncrementAboveSByteMinValue}'. Expected: '-127'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToInt32IsFoldedCorrectly()
        {
            double integerOneDecrementUnderSByteMaxValue = 126.0;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(int)126.0' was evaluated to '{(int)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMaxValueCastToInt32IsFoldedCorrectly()
        {
            double integerOneIncrementAboveSByteMaxValue = 128.0;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneIncrementAboveSByteMaxValue) != 128)
            {
                Console.WriteLine($"'(int)128.0' was evaluated to '{(int)integerOneIncrementAboveSByteMaxValue}'. Expected: '128'.");
                _counter++;
            }
        }
        ConfirmByteMaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmByteMaxValueCastToInt32IsFoldedCorrectly()
        {
            double byteMaxValue = 255.0;

            if (BreakUpFlow())
                return;

            if (checked((int)byteMaxValue) != 255)
            {
                Console.WriteLine($"'(int)255.0' was evaluated to '{(int)byteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderByteMinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderByteMinValueCastToInt32IsFoldedCorrectly()
        {
            double doubleOneDecrementUnderByteMinValue = -5E-324d;

            if (BreakUpFlow())
                return;

            if (checked((int)doubleOneDecrementUnderByteMinValue) != 0)
            {
                Console.WriteLine($"'(int)-5E-324d' was evaluated to '{(int)doubleOneDecrementUnderByteMinValue}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderByteMinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderByteMinValueCastToInt32IsFoldedCorrectly()
        {
            double doubleOneFullDecrementUnderByteMinValue = -1d;

            if (BreakUpFlow())
                return;

            if (checked((int)doubleOneFullDecrementUnderByteMinValue) != -1)
            {
                Console.WriteLine($"'(int)-1d' was evaluated to '{(int)doubleOneFullDecrementUnderByteMinValue}'. Expected: '-1'.");
                _counter++;
            }
        }
        ConfirmDoubleOneIncrementAboveByteMinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveByteMinValueCastToInt32IsFoldedCorrectly()
        {
            double doubleOneIncrementAboveByteMinValue = 5E-324d;

            if (BreakUpFlow())
                return;

            if (checked((int)doubleOneIncrementAboveByteMinValue) != 0)
            {
                Console.WriteLine($"'(int)5E-324d' was evaluated to '{(int)doubleOneIncrementAboveByteMinValue}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveByteMinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveByteMinValueCastToInt32IsFoldedCorrectly()
        {
            double doubleOneFullIncrementAboveByteMinValue = 1d;

            if (BreakUpFlow())
                return;

            if (checked((int)doubleOneFullIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(int)1d' was evaluated to '{(int)doubleOneFullIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderByteMaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderByteMaxValueCastToInt32IsFoldedCorrectly()
        {
            double doubleOneDecrementUnderByteMaxValue = 254.99999999999997d;

            if (BreakUpFlow())
                return;

            if (checked((int)doubleOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(int)254.99999999999997d' was evaluated to '{(int)doubleOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderByteMaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderByteMaxValueCastToInt32IsFoldedCorrectly()
        {
            double doubleOneFullDecrementUnderByteMaxValue = 254d;

            if (BreakUpFlow())
                return;

            if (checked((int)doubleOneFullDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(int)254d' was evaluated to '{(int)doubleOneFullDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmDoubleOneIncrementAboveByteMaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveByteMaxValueCastToInt32IsFoldedCorrectly()
        {
            double doubleOneIncrementAboveByteMaxValue = 255.00000000000003d;

            if (BreakUpFlow())
                return;

            if (checked((int)doubleOneIncrementAboveByteMaxValue) != 255)
            {
                Console.WriteLine($"'(int)255.00000000000003d' was evaluated to '{(int)doubleOneIncrementAboveByteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveByteMaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveByteMaxValueCastToInt32IsFoldedCorrectly()
        {
            double doubleOneFullIncrementAboveByteMaxValue = 256d;

            if (BreakUpFlow())
                return;

            if (checked((int)doubleOneFullIncrementAboveByteMaxValue) != 256)
            {
                Console.WriteLine($"'(int)256d' was evaluated to '{(int)doubleOneFullIncrementAboveByteMaxValue}'. Expected: '256'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMinValueCastToInt32IsFoldedCorrectly()
        {
            double integerOneDecrementUnderByteMinValue = -1.0;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneDecrementUnderByteMinValue) != -1)
            {
                Console.WriteLine($"'(int)-1.0' was evaluated to '{(int)integerOneDecrementUnderByteMinValue}'. Expected: '-1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToInt32IsFoldedCorrectly()
        {
            double integerOneIncrementAboveByteMinValue = 1.0;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(int)1.0' was evaluated to '{(int)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMaxValueCastToInt32IsFoldedCorrectly()
        {
            double integerOneDecrementUnderByteMaxValue = 254.0;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(int)254.0' was evaluated to '{(int)integerOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMaxValueCastToInt32IsFoldedCorrectly()
        {
            double integerOneIncrementAboveByteMaxValue = 256.0;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneIncrementAboveByteMaxValue) != 256)
            {
                Console.WriteLine($"'(int)256.0' was evaluated to '{(int)integerOneIncrementAboveByteMaxValue}'. Expected: '256'.");
                _counter++;
            }
        }
        ConfirmInt16MinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MinValueCastToInt32IsFoldedCorrectly()
        {
            double int16MinValue = -32768.0;

            if (BreakUpFlow())
                return;

            if (checked((int)int16MinValue) != -32768)
            {
                Console.WriteLine($"'(int)-32768.0' was evaluated to '{(int)int16MinValue}'. Expected: '-32768'.");
                _counter++;
            }
        }
        ConfirmInt16MaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MaxValueCastToInt32IsFoldedCorrectly()
        {
            double int16MaxValue = 32767.0;

            if (BreakUpFlow())
                return;

            if (checked((int)int16MaxValue) != 32767)
            {
                Console.WriteLine($"'(int)32767.0' was evaluated to '{(int)int16MaxValue}'. Expected: '32767'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderInt16MinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderInt16MinValueCastToInt32IsFoldedCorrectly()
        {
            double doubleOneDecrementUnderInt16MinValue = -32768.00000000001d;

            if (BreakUpFlow())
                return;

            if (checked((int)doubleOneDecrementUnderInt16MinValue) != -32768)
            {
                Console.WriteLine($"'(int)-32768.00000000001d' was evaluated to '{(int)doubleOneDecrementUnderInt16MinValue}'. Expected: '-32768'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderInt16MinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderInt16MinValueCastToInt32IsFoldedCorrectly()
        {
            double doubleOneFullDecrementUnderInt16MinValue = -32769d;

            if (BreakUpFlow())
                return;

            if (checked((int)doubleOneFullDecrementUnderInt16MinValue) != -32769)
            {
                Console.WriteLine($"'(int)-32769d' was evaluated to '{(int)doubleOneFullDecrementUnderInt16MinValue}'. Expected: '-32769'.");
                _counter++;
            }
        }
        ConfirmDoubleOneIncrementAboveInt16MinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveInt16MinValueCastToInt32IsFoldedCorrectly()
        {
            double doubleOneIncrementAboveInt16MinValue = -32767.999999999996d;

            if (BreakUpFlow())
                return;

            if (checked((int)doubleOneIncrementAboveInt16MinValue) != -32767)
            {
                Console.WriteLine($"'(int)-32767.999999999996d' was evaluated to '{(int)doubleOneIncrementAboveInt16MinValue}'. Expected: '-32767'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveInt16MinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveInt16MinValueCastToInt32IsFoldedCorrectly()
        {
            double doubleOneFullIncrementAboveInt16MinValue = -32767d;

            if (BreakUpFlow())
                return;

            if (checked((int)doubleOneFullIncrementAboveInt16MinValue) != -32767)
            {
                Console.WriteLine($"'(int)-32767d' was evaluated to '{(int)doubleOneFullIncrementAboveInt16MinValue}'. Expected: '-32767'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderInt16MaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderInt16MaxValueCastToInt32IsFoldedCorrectly()
        {
            double doubleOneDecrementUnderInt16MaxValue = 32766.999999999996d;

            if (BreakUpFlow())
                return;

            if (checked((int)doubleOneDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(int)32766.999999999996d' was evaluated to '{(int)doubleOneDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderInt16MaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderInt16MaxValueCastToInt32IsFoldedCorrectly()
        {
            double doubleOneFullDecrementUnderInt16MaxValue = 32766d;

            if (BreakUpFlow())
                return;

            if (checked((int)doubleOneFullDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(int)32766d' was evaluated to '{(int)doubleOneFullDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
        ConfirmDoubleOneIncrementAboveInt16MaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveInt16MaxValueCastToInt32IsFoldedCorrectly()
        {
            double doubleOneIncrementAboveInt16MaxValue = 32767.000000000004d;

            if (BreakUpFlow())
                return;

            if (checked((int)doubleOneIncrementAboveInt16MaxValue) != 32767)
            {
                Console.WriteLine($"'(int)32767.000000000004d' was evaluated to '{(int)doubleOneIncrementAboveInt16MaxValue}'. Expected: '32767'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveInt16MaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveInt16MaxValueCastToInt32IsFoldedCorrectly()
        {
            double doubleOneFullIncrementAboveInt16MaxValue = 32768d;

            if (BreakUpFlow())
                return;

            if (checked((int)doubleOneFullIncrementAboveInt16MaxValue) != 32768)
            {
                Console.WriteLine($"'(int)32768d' was evaluated to '{(int)doubleOneFullIncrementAboveInt16MaxValue}'. Expected: '32768'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt16MinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MinValueCastToInt32IsFoldedCorrectly()
        {
            double integerOneDecrementUnderInt16MinValue = -32769.0;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneDecrementUnderInt16MinValue) != -32769)
            {
                Console.WriteLine($"'(int)-32769.0' was evaluated to '{(int)integerOneDecrementUnderInt16MinValue}'. Expected: '-32769'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt16MinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt16MinValueCastToInt32IsFoldedCorrectly()
        {
            double integerOneIncrementAboveInt16MinValue = -32767.0;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneIncrementAboveInt16MinValue) != -32767)
            {
                Console.WriteLine($"'(int)-32767.0' was evaluated to '{(int)integerOneIncrementAboveInt16MinValue}'. Expected: '-32767'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt16MaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MaxValueCastToInt32IsFoldedCorrectly()
        {
            double integerOneDecrementUnderInt16MaxValue = 32766.0;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(int)32766.0' was evaluated to '{(int)integerOneDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt16MaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt16MaxValueCastToInt32IsFoldedCorrectly()
        {
            double integerOneIncrementAboveInt16MaxValue = 32768.0;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneIncrementAboveInt16MaxValue) != 32768)
            {
                Console.WriteLine($"'(int)32768.0' was evaluated to '{(int)integerOneIncrementAboveInt16MaxValue}'. Expected: '32768'.");
                _counter++;
            }
        }
        ConfirmUInt16MaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt16MaxValueCastToInt32IsFoldedCorrectly()
        {
            double uInt16MaxValue = 65535.0;

            if (BreakUpFlow())
                return;

            if (checked((int)uInt16MaxValue) != 65535)
            {
                Console.WriteLine($"'(int)65535.0' was evaluated to '{(int)uInt16MaxValue}'. Expected: '65535'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderUInt16MaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderUInt16MaxValueCastToInt32IsFoldedCorrectly()
        {
            double doubleOneDecrementUnderUInt16MaxValue = 65534.99999999999d;

            if (BreakUpFlow())
                return;

            if (checked((int)doubleOneDecrementUnderUInt16MaxValue) != 65534)
            {
                Console.WriteLine($"'(int)65534.99999999999d' was evaluated to '{(int)doubleOneDecrementUnderUInt16MaxValue}'. Expected: '65534'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderUInt16MaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderUInt16MaxValueCastToInt32IsFoldedCorrectly()
        {
            double doubleOneFullDecrementUnderUInt16MaxValue = 65534d;

            if (BreakUpFlow())
                return;

            if (checked((int)doubleOneFullDecrementUnderUInt16MaxValue) != 65534)
            {
                Console.WriteLine($"'(int)65534d' was evaluated to '{(int)doubleOneFullDecrementUnderUInt16MaxValue}'. Expected: '65534'.");
                _counter++;
            }
        }
        ConfirmDoubleOneIncrementAboveUInt16MaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveUInt16MaxValueCastToInt32IsFoldedCorrectly()
        {
            double doubleOneIncrementAboveUInt16MaxValue = 65535.00000000001d;

            if (BreakUpFlow())
                return;

            if (checked((int)doubleOneIncrementAboveUInt16MaxValue) != 65535)
            {
                Console.WriteLine($"'(int)65535.00000000001d' was evaluated to '{(int)doubleOneIncrementAboveUInt16MaxValue}'. Expected: '65535'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveUInt16MaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveUInt16MaxValueCastToInt32IsFoldedCorrectly()
        {
            double doubleOneFullIncrementAboveUInt16MaxValue = 65536d;

            if (BreakUpFlow())
                return;

            if (checked((int)doubleOneFullIncrementAboveUInt16MaxValue) != 65536)
            {
                Console.WriteLine($"'(int)65536d' was evaluated to '{(int)doubleOneFullIncrementAboveUInt16MaxValue}'. Expected: '65536'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderUInt16MaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt16MaxValueCastToInt32IsFoldedCorrectly()
        {
            double integerOneDecrementUnderUInt16MaxValue = 65534.0;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneDecrementUnderUInt16MaxValue) != 65534)
            {
                Console.WriteLine($"'(int)65534.0' was evaluated to '{(int)integerOneDecrementUnderUInt16MaxValue}'. Expected: '65534'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToInt32IsFoldedCorrectly()
        {
            double integerOneIncrementAboveUInt16MaxValue = 65536.0;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneIncrementAboveUInt16MaxValue) != 65536)
            {
                Console.WriteLine($"'(int)65536.0' was evaluated to '{(int)integerOneIncrementAboveUInt16MaxValue}'. Expected: '65536'.");
                _counter++;
            }
        }
        ConfirmInt32MinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MinValueCastToInt32IsFoldedCorrectly()
        {
            double int32MinValue = -2147483648.0;

            if (BreakUpFlow())
                return;

            if (checked((int)int32MinValue) != -2147483648)
            {
                Console.WriteLine($"'(int)-2147483648.0' was evaluated to '{(int)int32MinValue}'. Expected: '-2147483648'.");
                _counter++;
            }
        }
        ConfirmInt32MaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MaxValueCastToInt32IsFoldedCorrectly()
        {
            double int32MaxValue = 2147483647.0;

            if (BreakUpFlow())
                return;

            if (checked((int)int32MaxValue) != 2147483647)
            {
                Console.WriteLine($"'(int)2147483647.0' was evaluated to '{(int)int32MaxValue}'. Expected: '2147483647'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderInt32MinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderInt32MinValueCastToInt32IsFoldedCorrectly()
        {
            double doubleOneDecrementUnderInt32MinValue = -2147483648.0000005d;

            if (BreakUpFlow())
                return;

            if (checked((int)doubleOneDecrementUnderInt32MinValue) != -2147483648)
            {
                Console.WriteLine($"'(int)-2147483648.0000005d' was evaluated to '{(int)doubleOneDecrementUnderInt32MinValue}'. Expected: '-2147483648'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderInt32MinValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderInt32MinValueCastToInt32Overflows()
        {
            double from = -2147483649d;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)-2147483649d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveInt32MinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveInt32MinValueCastToInt32IsFoldedCorrectly()
        {
            double doubleOneIncrementAboveInt32MinValue = -2147483647.9999998d;

            if (BreakUpFlow())
                return;

            if (checked((int)doubleOneIncrementAboveInt32MinValue) != -2147483647)
            {
                Console.WriteLine($"'(int)-2147483647.9999998d' was evaluated to '{(int)doubleOneIncrementAboveInt32MinValue}'. Expected: '-2147483647'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveInt32MinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveInt32MinValueCastToInt32IsFoldedCorrectly()
        {
            double doubleOneFullIncrementAboveInt32MinValue = -2147483647d;

            if (BreakUpFlow())
                return;

            if (checked((int)doubleOneFullIncrementAboveInt32MinValue) != -2147483647)
            {
                Console.WriteLine($"'(int)-2147483647d' was evaluated to '{(int)doubleOneFullIncrementAboveInt32MinValue}'. Expected: '-2147483647'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderInt32MaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderInt32MaxValueCastToInt32IsFoldedCorrectly()
        {
            double doubleOneDecrementUnderInt32MaxValue = 2147483646.9999998d;

            if (BreakUpFlow())
                return;

            if (checked((int)doubleOneDecrementUnderInt32MaxValue) != 2147483646)
            {
                Console.WriteLine($"'(int)2147483646.9999998d' was evaluated to '{(int)doubleOneDecrementUnderInt32MaxValue}'. Expected: '2147483646'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderInt32MaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderInt32MaxValueCastToInt32IsFoldedCorrectly()
        {
            double doubleOneFullDecrementUnderInt32MaxValue = 2147483646d;

            if (BreakUpFlow())
                return;

            if (checked((int)doubleOneFullDecrementUnderInt32MaxValue) != 2147483646)
            {
                Console.WriteLine($"'(int)2147483646d' was evaluated to '{(int)doubleOneFullDecrementUnderInt32MaxValue}'. Expected: '2147483646'.");
                _counter++;
            }
        }
        ConfirmDoubleOneIncrementAboveInt32MaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveInt32MaxValueCastToInt32IsFoldedCorrectly()
        {
            double doubleOneIncrementAboveInt32MaxValue = 2147483647.0000002d;

            if (BreakUpFlow())
                return;

            if (checked((int)doubleOneIncrementAboveInt32MaxValue) != 2147483647)
            {
                Console.WriteLine($"'(int)2147483647.0000002d' was evaluated to '{(int)doubleOneIncrementAboveInt32MaxValue}'. Expected: '2147483647'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveInt32MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveInt32MaxValueCastToInt32Overflows()
        {
            double from = 2147483648d;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)2147483648d)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt32MinValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt32MinValueCastToInt32Overflows()
        {
            double from = -2147483649.0;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)-2147483649.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt32MinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt32MinValueCastToInt32IsFoldedCorrectly()
        {
            double integerOneIncrementAboveInt32MinValue = -2147483647.0;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneIncrementAboveInt32MinValue) != -2147483647)
            {
                Console.WriteLine($"'(int)-2147483647.0' was evaluated to '{(int)integerOneIncrementAboveInt32MinValue}'. Expected: '-2147483647'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt32MaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt32MaxValueCastToInt32IsFoldedCorrectly()
        {
            double integerOneDecrementUnderInt32MaxValue = 2147483646.0;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneDecrementUnderInt32MaxValue) != 2147483646)
            {
                Console.WriteLine($"'(int)2147483646.0' was evaluated to '{(int)integerOneDecrementUnderInt32MaxValue}'. Expected: '2147483646'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt32MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt32MaxValueCastToInt32Overflows()
        {
            double from = 2147483648.0;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)2147483648.0)' did not throw OverflowException.");
        }
        ConfirmUInt32MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32MaxValueCastToInt32Overflows()
        {
            double from = 4294967295.0;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)4294967295.0)' did not throw OverflowException.");
        }
        ConfirmDoubleOneDecrementUnderUInt32MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderUInt32MaxValueCastToInt32Overflows()
        {
            double from = 4294967294.9999995d;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)4294967294.9999995d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullDecrementUnderUInt32MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderUInt32MaxValueCastToInt32Overflows()
        {
            double from = 4294967294d;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)4294967294d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveUInt32MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveUInt32MaxValueCastToInt32Overflows()
        {
            double from = 4294967295.0000005d;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)4294967295.0000005d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullIncrementAboveUInt32MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveUInt32MaxValueCastToInt32Overflows()
        {
            double from = 4294967296d;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)4294967296d)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderUInt32MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt32MaxValueCastToInt32Overflows()
        {
            double from = 4294967294.0;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)4294967294.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveUInt32MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt32MaxValueCastToInt32Overflows()
        {
            double from = 4294967296.0;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)4294967296.0)' did not throw OverflowException.");
        }
        ConfirmInt64MinValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MinValueCastToInt32Overflows()
        {
            double from = -9223372036854775808.0;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)-9223372036854775808.0)' did not throw OverflowException.");
        }
        ConfirmInt64MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MaxValueCastToInt32Overflows()
        {
            double from = 9223372036854775807.0;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)9223372036854775807.0)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveInt64MinValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveInt64MinValueCastToInt32Overflows()
        {
            double from = -9.223372036854775E+18d;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)-9.223372036854775E+18d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneDecrementUnderInt64MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderInt64MaxValueCastToInt32Overflows()
        {
            double from = 9.223372036854775E+18d;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)9.223372036854775E+18d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveInt64MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveInt64MaxValueCastToInt32Overflows()
        {
            double from = 9.223372036854778E+18d;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)9.223372036854778E+18d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullIncrementAboveInt64MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveInt64MaxValueCastToInt32Overflows()
        {
            double from = 9.223372036854776E+18d;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)9.223372036854776E+18d)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt64MinValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt64MinValueCastToInt32Overflows()
        {
            double from = -9223372036854775807.0;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)-9223372036854775807.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt64MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt64MaxValueCastToInt32Overflows()
        {
            double from = 9223372036854775806.0;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)9223372036854775806.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt64MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt64MaxValueCastToInt32Overflows()
        {
            double from = 9223372036854775808.0;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)9223372036854775808.0)' did not throw OverflowException.");
        }
        ConfirmUInt64MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64MaxValueCastToInt32Overflows()
        {
            double from = 18446744073709551615.0;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)18446744073709551615.0)' did not throw OverflowException.");
        }
        ConfirmDoubleOneDecrementUnderUInt64MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderUInt64MaxValueCastToInt32Overflows()
        {
            double from = 1.844674407370955E+19d;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)1.844674407370955E+19d)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderUInt64MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt64MaxValueCastToInt32Overflows()
        {
            double from = 18446744073709551614.0;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)18446744073709551614.0)' did not throw OverflowException.");
        }
    }

    private static void TestCastingDoubleToUInt32()
    {
        ConfirmIntegerZeroCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToUInt32IsFoldedCorrectly()
        {
            double integerZero = 0.0;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerZero) != 0)
            {
                Console.WriteLine($"'(uint)0.0' was evaluated to '{(uint)integerZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmDoubleMinusZeroCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleMinusZeroCastToUInt32IsFoldedCorrectly()
        {
            double doubleMinusZero = -0d;

            if (BreakUpFlow())
                return;

            if (checked((uint)doubleMinusZero) != 0)
            {
                Console.WriteLine($"'(uint)-0d' was evaluated to '{(uint)doubleMinusZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmDoubleZeroCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleZeroCastToUInt32IsFoldedCorrectly()
        {
            double doubleZero = 0d;

            if (BreakUpFlow())
                return;

            if (checked((uint)doubleZero) != 0)
            {
                Console.WriteLine($"'(uint)0d' was evaluated to '{(uint)doubleZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmDoubleMinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleMinValueCastToUInt32Overflows()
        {
            double from = -1.7976931348623157E+308d;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-1.7976931348623157E+308d)' did not throw OverflowException.");
        }
        ConfirmDoubleMaxValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleMaxValueCastToUInt32Overflows()
        {
            double from = 1.7976931348623157E+308d;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)1.7976931348623157E+308d)' did not throw OverflowException.");
        }
        ConfirmSByteMinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMinValueCastToUInt32Overflows()
        {
            double from = -128.0;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-128.0)' did not throw OverflowException.");
        }
        ConfirmSByteMaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToUInt32IsFoldedCorrectly()
        {
            double sByteMaxValue = 127.0;

            if (BreakUpFlow())
                return;

            if (checked((uint)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(uint)127.0' was evaluated to '{(uint)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderSByteMinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderSByteMinValueCastToUInt32Overflows()
        {
            double from = -128.00000000000003d;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-128.00000000000003d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullDecrementUnderSByteMinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderSByteMinValueCastToUInt32Overflows()
        {
            double from = -129d;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-129d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveSByteMinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveSByteMinValueCastToUInt32Overflows()
        {
            double from = -127.99999999999999d;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-127.99999999999999d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullIncrementAboveSByteMinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveSByteMinValueCastToUInt32Overflows()
        {
            double from = -127d;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-127d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneDecrementUnderSByteMaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderSByteMaxValueCastToUInt32IsFoldedCorrectly()
        {
            double doubleOneDecrementUnderSByteMaxValue = 126.99999999999999d;

            if (BreakUpFlow())
                return;

            if (checked((uint)doubleOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(uint)126.99999999999999d' was evaluated to '{(uint)doubleOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderSByteMaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderSByteMaxValueCastToUInt32IsFoldedCorrectly()
        {
            double doubleOneFullDecrementUnderSByteMaxValue = 126d;

            if (BreakUpFlow())
                return;

            if (checked((uint)doubleOneFullDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(uint)126d' was evaluated to '{(uint)doubleOneFullDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmDoubleOneIncrementAboveSByteMaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveSByteMaxValueCastToUInt32IsFoldedCorrectly()
        {
            double doubleOneIncrementAboveSByteMaxValue = 127.00000000000001d;

            if (BreakUpFlow())
                return;

            if (checked((uint)doubleOneIncrementAboveSByteMaxValue) != 127)
            {
                Console.WriteLine($"'(uint)127.00000000000001d' was evaluated to '{(uint)doubleOneIncrementAboveSByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveSByteMaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveSByteMaxValueCastToUInt32IsFoldedCorrectly()
        {
            double doubleOneFullIncrementAboveSByteMaxValue = 128d;

            if (BreakUpFlow())
                return;

            if (checked((uint)doubleOneFullIncrementAboveSByteMaxValue) != 128)
            {
                Console.WriteLine($"'(uint)128d' was evaluated to '{(uint)doubleOneFullIncrementAboveSByteMaxValue}'. Expected: '128'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMinValueCastToUInt32Overflows()
        {
            double from = -129.0;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-129.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveSByteMinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMinValueCastToUInt32Overflows()
        {
            double from = -127.0;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-127.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToUInt32IsFoldedCorrectly()
        {
            double integerOneDecrementUnderSByteMaxValue = 126.0;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(uint)126.0' was evaluated to '{(uint)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMaxValueCastToUInt32IsFoldedCorrectly()
        {
            double integerOneIncrementAboveSByteMaxValue = 128.0;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneIncrementAboveSByteMaxValue) != 128)
            {
                Console.WriteLine($"'(uint)128.0' was evaluated to '{(uint)integerOneIncrementAboveSByteMaxValue}'. Expected: '128'.");
                _counter++;
            }
        }
        ConfirmByteMaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmByteMaxValueCastToUInt32IsFoldedCorrectly()
        {
            double byteMaxValue = 255.0;

            if (BreakUpFlow())
                return;

            if (checked((uint)byteMaxValue) != 255)
            {
                Console.WriteLine($"'(uint)255.0' was evaluated to '{(uint)byteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderByteMinValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderByteMinValueCastToUInt32IsFoldedCorrectly()
        {
            double doubleOneDecrementUnderByteMinValue = -5E-324d;

            if (BreakUpFlow())
                return;

            if (checked((uint)doubleOneDecrementUnderByteMinValue) != 0)
            {
                Console.WriteLine($"'(uint)-5E-324d' was evaluated to '{(uint)doubleOneDecrementUnderByteMinValue}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderByteMinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderByteMinValueCastToUInt32Overflows()
        {
            double from = -1d;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-1d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveByteMinValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveByteMinValueCastToUInt32IsFoldedCorrectly()
        {
            double doubleOneIncrementAboveByteMinValue = 5E-324d;

            if (BreakUpFlow())
                return;

            if (checked((uint)doubleOneIncrementAboveByteMinValue) != 0)
            {
                Console.WriteLine($"'(uint)5E-324d' was evaluated to '{(uint)doubleOneIncrementAboveByteMinValue}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveByteMinValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveByteMinValueCastToUInt32IsFoldedCorrectly()
        {
            double doubleOneFullIncrementAboveByteMinValue = 1d;

            if (BreakUpFlow())
                return;

            if (checked((uint)doubleOneFullIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(uint)1d' was evaluated to '{(uint)doubleOneFullIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderByteMaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderByteMaxValueCastToUInt32IsFoldedCorrectly()
        {
            double doubleOneDecrementUnderByteMaxValue = 254.99999999999997d;

            if (BreakUpFlow())
                return;

            if (checked((uint)doubleOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(uint)254.99999999999997d' was evaluated to '{(uint)doubleOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderByteMaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderByteMaxValueCastToUInt32IsFoldedCorrectly()
        {
            double doubleOneFullDecrementUnderByteMaxValue = 254d;

            if (BreakUpFlow())
                return;

            if (checked((uint)doubleOneFullDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(uint)254d' was evaluated to '{(uint)doubleOneFullDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmDoubleOneIncrementAboveByteMaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveByteMaxValueCastToUInt32IsFoldedCorrectly()
        {
            double doubleOneIncrementAboveByteMaxValue = 255.00000000000003d;

            if (BreakUpFlow())
                return;

            if (checked((uint)doubleOneIncrementAboveByteMaxValue) != 255)
            {
                Console.WriteLine($"'(uint)255.00000000000003d' was evaluated to '{(uint)doubleOneIncrementAboveByteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveByteMaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveByteMaxValueCastToUInt32IsFoldedCorrectly()
        {
            double doubleOneFullIncrementAboveByteMaxValue = 256d;

            if (BreakUpFlow())
                return;

            if (checked((uint)doubleOneFullIncrementAboveByteMaxValue) != 256)
            {
                Console.WriteLine($"'(uint)256d' was evaluated to '{(uint)doubleOneFullIncrementAboveByteMaxValue}'. Expected: '256'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMinValueCastToUInt32Overflows()
        {
            double from = -1.0;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-1.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToUInt32IsFoldedCorrectly()
        {
            double integerOneIncrementAboveByteMinValue = 1.0;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(uint)1.0' was evaluated to '{(uint)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMaxValueCastToUInt32IsFoldedCorrectly()
        {
            double integerOneDecrementUnderByteMaxValue = 254.0;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(uint)254.0' was evaluated to '{(uint)integerOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMaxValueCastToUInt32IsFoldedCorrectly()
        {
            double integerOneIncrementAboveByteMaxValue = 256.0;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneIncrementAboveByteMaxValue) != 256)
            {
                Console.WriteLine($"'(uint)256.0' was evaluated to '{(uint)integerOneIncrementAboveByteMaxValue}'. Expected: '256'.");
                _counter++;
            }
        }
        ConfirmInt16MinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MinValueCastToUInt32Overflows()
        {
            double from = -32768.0;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-32768.0)' did not throw OverflowException.");
        }
        ConfirmInt16MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MaxValueCastToUInt32IsFoldedCorrectly()
        {
            double int16MaxValue = 32767.0;

            if (BreakUpFlow())
                return;

            if (checked((uint)int16MaxValue) != 32767)
            {
                Console.WriteLine($"'(uint)32767.0' was evaluated to '{(uint)int16MaxValue}'. Expected: '32767'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderInt16MinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderInt16MinValueCastToUInt32Overflows()
        {
            double from = -32768.00000000001d;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-32768.00000000001d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullDecrementUnderInt16MinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderInt16MinValueCastToUInt32Overflows()
        {
            double from = -32769d;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-32769d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveInt16MinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveInt16MinValueCastToUInt32Overflows()
        {
            double from = -32767.999999999996d;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-32767.999999999996d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullIncrementAboveInt16MinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveInt16MinValueCastToUInt32Overflows()
        {
            double from = -32767d;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-32767d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneDecrementUnderInt16MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderInt16MaxValueCastToUInt32IsFoldedCorrectly()
        {
            double doubleOneDecrementUnderInt16MaxValue = 32766.999999999996d;

            if (BreakUpFlow())
                return;

            if (checked((uint)doubleOneDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(uint)32766.999999999996d' was evaluated to '{(uint)doubleOneDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderInt16MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderInt16MaxValueCastToUInt32IsFoldedCorrectly()
        {
            double doubleOneFullDecrementUnderInt16MaxValue = 32766d;

            if (BreakUpFlow())
                return;

            if (checked((uint)doubleOneFullDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(uint)32766d' was evaluated to '{(uint)doubleOneFullDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
        ConfirmDoubleOneIncrementAboveInt16MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveInt16MaxValueCastToUInt32IsFoldedCorrectly()
        {
            double doubleOneIncrementAboveInt16MaxValue = 32767.000000000004d;

            if (BreakUpFlow())
                return;

            if (checked((uint)doubleOneIncrementAboveInt16MaxValue) != 32767)
            {
                Console.WriteLine($"'(uint)32767.000000000004d' was evaluated to '{(uint)doubleOneIncrementAboveInt16MaxValue}'. Expected: '32767'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveInt16MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveInt16MaxValueCastToUInt32IsFoldedCorrectly()
        {
            double doubleOneFullIncrementAboveInt16MaxValue = 32768d;

            if (BreakUpFlow())
                return;

            if (checked((uint)doubleOneFullIncrementAboveInt16MaxValue) != 32768)
            {
                Console.WriteLine($"'(uint)32768d' was evaluated to '{(uint)doubleOneFullIncrementAboveInt16MaxValue}'. Expected: '32768'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt16MinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MinValueCastToUInt32Overflows()
        {
            double from = -32769.0;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-32769.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt16MinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt16MinValueCastToUInt32Overflows()
        {
            double from = -32767.0;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-32767.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt16MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MaxValueCastToUInt32IsFoldedCorrectly()
        {
            double integerOneDecrementUnderInt16MaxValue = 32766.0;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(uint)32766.0' was evaluated to '{(uint)integerOneDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt16MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt16MaxValueCastToUInt32IsFoldedCorrectly()
        {
            double integerOneIncrementAboveInt16MaxValue = 32768.0;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneIncrementAboveInt16MaxValue) != 32768)
            {
                Console.WriteLine($"'(uint)32768.0' was evaluated to '{(uint)integerOneIncrementAboveInt16MaxValue}'. Expected: '32768'.");
                _counter++;
            }
        }
        ConfirmUInt16MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt16MaxValueCastToUInt32IsFoldedCorrectly()
        {
            double uInt16MaxValue = 65535.0;

            if (BreakUpFlow())
                return;

            if (checked((uint)uInt16MaxValue) != 65535)
            {
                Console.WriteLine($"'(uint)65535.0' was evaluated to '{(uint)uInt16MaxValue}'. Expected: '65535'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderUInt16MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderUInt16MaxValueCastToUInt32IsFoldedCorrectly()
        {
            double doubleOneDecrementUnderUInt16MaxValue = 65534.99999999999d;

            if (BreakUpFlow())
                return;

            if (checked((uint)doubleOneDecrementUnderUInt16MaxValue) != 65534)
            {
                Console.WriteLine($"'(uint)65534.99999999999d' was evaluated to '{(uint)doubleOneDecrementUnderUInt16MaxValue}'. Expected: '65534'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderUInt16MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderUInt16MaxValueCastToUInt32IsFoldedCorrectly()
        {
            double doubleOneFullDecrementUnderUInt16MaxValue = 65534d;

            if (BreakUpFlow())
                return;

            if (checked((uint)doubleOneFullDecrementUnderUInt16MaxValue) != 65534)
            {
                Console.WriteLine($"'(uint)65534d' was evaluated to '{(uint)doubleOneFullDecrementUnderUInt16MaxValue}'. Expected: '65534'.");
                _counter++;
            }
        }
        ConfirmDoubleOneIncrementAboveUInt16MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveUInt16MaxValueCastToUInt32IsFoldedCorrectly()
        {
            double doubleOneIncrementAboveUInt16MaxValue = 65535.00000000001d;

            if (BreakUpFlow())
                return;

            if (checked((uint)doubleOneIncrementAboveUInt16MaxValue) != 65535)
            {
                Console.WriteLine($"'(uint)65535.00000000001d' was evaluated to '{(uint)doubleOneIncrementAboveUInt16MaxValue}'. Expected: '65535'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveUInt16MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveUInt16MaxValueCastToUInt32IsFoldedCorrectly()
        {
            double doubleOneFullIncrementAboveUInt16MaxValue = 65536d;

            if (BreakUpFlow())
                return;

            if (checked((uint)doubleOneFullIncrementAboveUInt16MaxValue) != 65536)
            {
                Console.WriteLine($"'(uint)65536d' was evaluated to '{(uint)doubleOneFullIncrementAboveUInt16MaxValue}'. Expected: '65536'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderUInt16MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt16MaxValueCastToUInt32IsFoldedCorrectly()
        {
            double integerOneDecrementUnderUInt16MaxValue = 65534.0;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneDecrementUnderUInt16MaxValue) != 65534)
            {
                Console.WriteLine($"'(uint)65534.0' was evaluated to '{(uint)integerOneDecrementUnderUInt16MaxValue}'. Expected: '65534'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToUInt32IsFoldedCorrectly()
        {
            double integerOneIncrementAboveUInt16MaxValue = 65536.0;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneIncrementAboveUInt16MaxValue) != 65536)
            {
                Console.WriteLine($"'(uint)65536.0' was evaluated to '{(uint)integerOneIncrementAboveUInt16MaxValue}'. Expected: '65536'.");
                _counter++;
            }
        }
        ConfirmInt32MinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MinValueCastToUInt32Overflows()
        {
            double from = -2147483648.0;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-2147483648.0)' did not throw OverflowException.");
        }
        ConfirmInt32MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MaxValueCastToUInt32IsFoldedCorrectly()
        {
            double int32MaxValue = 2147483647.0;

            if (BreakUpFlow())
                return;

            if (checked((uint)int32MaxValue) != 2147483647)
            {
                Console.WriteLine($"'(uint)2147483647.0' was evaluated to '{(uint)int32MaxValue}'. Expected: '2147483647'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderInt32MinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderInt32MinValueCastToUInt32Overflows()
        {
            double from = -2147483648.0000005d;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-2147483648.0000005d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullDecrementUnderInt32MinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderInt32MinValueCastToUInt32Overflows()
        {
            double from = -2147483649d;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-2147483649d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveInt32MinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveInt32MinValueCastToUInt32Overflows()
        {
            double from = -2147483647.9999998d;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-2147483647.9999998d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullIncrementAboveInt32MinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveInt32MinValueCastToUInt32Overflows()
        {
            double from = -2147483647d;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-2147483647d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneDecrementUnderInt32MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderInt32MaxValueCastToUInt32IsFoldedCorrectly()
        {
            double doubleOneDecrementUnderInt32MaxValue = 2147483646.9999998d;

            if (BreakUpFlow())
                return;

            if (checked((uint)doubleOneDecrementUnderInt32MaxValue) != 2147483646)
            {
                Console.WriteLine($"'(uint)2147483646.9999998d' was evaluated to '{(uint)doubleOneDecrementUnderInt32MaxValue}'. Expected: '2147483646'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderInt32MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderInt32MaxValueCastToUInt32IsFoldedCorrectly()
        {
            double doubleOneFullDecrementUnderInt32MaxValue = 2147483646d;

            if (BreakUpFlow())
                return;

            if (checked((uint)doubleOneFullDecrementUnderInt32MaxValue) != 2147483646)
            {
                Console.WriteLine($"'(uint)2147483646d' was evaluated to '{(uint)doubleOneFullDecrementUnderInt32MaxValue}'. Expected: '2147483646'.");
                _counter++;
            }
        }
        ConfirmDoubleOneIncrementAboveInt32MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveInt32MaxValueCastToUInt32IsFoldedCorrectly()
        {
            double doubleOneIncrementAboveInt32MaxValue = 2147483647.0000002d;

            if (BreakUpFlow())
                return;

            if (checked((uint)doubleOneIncrementAboveInt32MaxValue) != 2147483647)
            {
                Console.WriteLine($"'(uint)2147483647.0000002d' was evaluated to '{(uint)doubleOneIncrementAboveInt32MaxValue}'. Expected: '2147483647'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveInt32MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveInt32MaxValueCastToUInt32IsFoldedCorrectly()
        {
            double doubleOneFullIncrementAboveInt32MaxValue = 2147483648d;

            if (BreakUpFlow())
                return;

            if (checked((uint)doubleOneFullIncrementAboveInt32MaxValue) != 2147483648)
            {
                Console.WriteLine($"'(uint)2147483648d' was evaluated to '{(uint)doubleOneFullIncrementAboveInt32MaxValue}'. Expected: '2147483648'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt32MinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt32MinValueCastToUInt32Overflows()
        {
            double from = -2147483649.0;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-2147483649.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt32MinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt32MinValueCastToUInt32Overflows()
        {
            double from = -2147483647.0;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-2147483647.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt32MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt32MaxValueCastToUInt32IsFoldedCorrectly()
        {
            double integerOneDecrementUnderInt32MaxValue = 2147483646.0;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneDecrementUnderInt32MaxValue) != 2147483646)
            {
                Console.WriteLine($"'(uint)2147483646.0' was evaluated to '{(uint)integerOneDecrementUnderInt32MaxValue}'. Expected: '2147483646'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt32MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt32MaxValueCastToUInt32IsFoldedCorrectly()
        {
            double integerOneIncrementAboveInt32MaxValue = 2147483648.0;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneIncrementAboveInt32MaxValue) != 2147483648)
            {
                Console.WriteLine($"'(uint)2147483648.0' was evaluated to '{(uint)integerOneIncrementAboveInt32MaxValue}'. Expected: '2147483648'.");
                _counter++;
            }
        }
        ConfirmUInt32MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32MaxValueCastToUInt32IsFoldedCorrectly()
        {
            double uInt32MaxValue = 4294967295.0;

            if (BreakUpFlow())
                return;

            if (checked((uint)uInt32MaxValue) != 4294967295)
            {
                Console.WriteLine($"'(uint)4294967295.0' was evaluated to '{(uint)uInt32MaxValue}'. Expected: '4294967295'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderUInt32MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderUInt32MaxValueCastToUInt32IsFoldedCorrectly()
        {
            double doubleOneDecrementUnderUInt32MaxValue = 4294967294.9999995d;

            if (BreakUpFlow())
                return;

            if (checked((uint)doubleOneDecrementUnderUInt32MaxValue) != 4294967294)
            {
                Console.WriteLine($"'(uint)4294967294.9999995d' was evaluated to '{(uint)doubleOneDecrementUnderUInt32MaxValue}'. Expected: '4294967294'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderUInt32MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderUInt32MaxValueCastToUInt32IsFoldedCorrectly()
        {
            double doubleOneFullDecrementUnderUInt32MaxValue = 4294967294d;

            if (BreakUpFlow())
                return;

            if (checked((uint)doubleOneFullDecrementUnderUInt32MaxValue) != 4294967294)
            {
                Console.WriteLine($"'(uint)4294967294d' was evaluated to '{(uint)doubleOneFullDecrementUnderUInt32MaxValue}'. Expected: '4294967294'.");
                _counter++;
            }
        }
        ConfirmDoubleOneIncrementAboveUInt32MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveUInt32MaxValueCastToUInt32IsFoldedCorrectly()
        {
            double doubleOneIncrementAboveUInt32MaxValue = 4294967295.0000005d;

            if (BreakUpFlow())
                return;

            if (checked((uint)doubleOneIncrementAboveUInt32MaxValue) != 4294967295)
            {
                Console.WriteLine($"'(uint)4294967295.0000005d' was evaluated to '{(uint)doubleOneIncrementAboveUInt32MaxValue}'. Expected: '4294967295'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveUInt32MaxValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveUInt32MaxValueCastToUInt32Overflows()
        {
            double from = 4294967296d;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)4294967296d)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderUInt32MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt32MaxValueCastToUInt32IsFoldedCorrectly()
        {
            double integerOneDecrementUnderUInt32MaxValue = 4294967294.0;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneDecrementUnderUInt32MaxValue) != 4294967294)
            {
                Console.WriteLine($"'(uint)4294967294.0' was evaluated to '{(uint)integerOneDecrementUnderUInt32MaxValue}'. Expected: '4294967294'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveUInt32MaxValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt32MaxValueCastToUInt32Overflows()
        {
            double from = 4294967296.0;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)4294967296.0)' did not throw OverflowException.");
        }
        ConfirmInt64MinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MinValueCastToUInt32Overflows()
        {
            double from = -9223372036854775808.0;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-9223372036854775808.0)' did not throw OverflowException.");
        }
        ConfirmInt64MaxValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MaxValueCastToUInt32Overflows()
        {
            double from = 9223372036854775807.0;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)9223372036854775807.0)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveInt64MinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveInt64MinValueCastToUInt32Overflows()
        {
            double from = -9.223372036854775E+18d;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-9.223372036854775E+18d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneDecrementUnderInt64MaxValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderInt64MaxValueCastToUInt32Overflows()
        {
            double from = 9.223372036854775E+18d;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)9.223372036854775E+18d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveInt64MaxValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveInt64MaxValueCastToUInt32Overflows()
        {
            double from = 9.223372036854778E+18d;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)9.223372036854778E+18d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullIncrementAboveInt64MaxValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveInt64MaxValueCastToUInt32Overflows()
        {
            double from = 9.223372036854776E+18d;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)9.223372036854776E+18d)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt64MinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt64MinValueCastToUInt32Overflows()
        {
            double from = -9223372036854775807.0;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-9223372036854775807.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt64MaxValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt64MaxValueCastToUInt32Overflows()
        {
            double from = 9223372036854775806.0;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)9223372036854775806.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt64MaxValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt64MaxValueCastToUInt32Overflows()
        {
            double from = 9223372036854775808.0;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)9223372036854775808.0)' did not throw OverflowException.");
        }
        ConfirmUInt64MaxValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64MaxValueCastToUInt32Overflows()
        {
            double from = 18446744073709551615.0;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)18446744073709551615.0)' did not throw OverflowException.");
        }
        ConfirmDoubleOneDecrementUnderUInt64MaxValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderUInt64MaxValueCastToUInt32Overflows()
        {
            double from = 1.844674407370955E+19d;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)1.844674407370955E+19d)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderUInt64MaxValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt64MaxValueCastToUInt32Overflows()
        {
            double from = 18446744073709551614.0;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)18446744073709551614.0)' did not throw OverflowException.");
        }
    }

    private static void TestCastingDoubleToInt64()
    {
        ConfirmIntegerZeroCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToInt64IsFoldedCorrectly()
        {
            double integerZero = 0.0;

            if (BreakUpFlow())
                return;

            if (checked((long)integerZero) != 0)
            {
                Console.WriteLine($"'(long)0.0' was evaluated to '{(long)integerZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmDoubleMinusZeroCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleMinusZeroCastToInt64IsFoldedCorrectly()
        {
            double doubleMinusZero = -0d;

            if (BreakUpFlow())
                return;

            if (checked((long)doubleMinusZero) != 0)
            {
                Console.WriteLine($"'(long)-0d' was evaluated to '{(long)doubleMinusZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmDoubleZeroCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleZeroCastToInt64IsFoldedCorrectly()
        {
            double doubleZero = 0d;

            if (BreakUpFlow())
                return;

            if (checked((long)doubleZero) != 0)
            {
                Console.WriteLine($"'(long)0d' was evaluated to '{(long)doubleZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmDoubleMinValueCastToInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleMinValueCastToInt64Overflows()
        {
            double from = -1.7976931348623157E+308d;
            _counter++;
            try
            {
                _ = checked((long)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((long)-1.7976931348623157E+308d)' did not throw OverflowException.");
        }
        ConfirmDoubleMaxValueCastToInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleMaxValueCastToInt64Overflows()
        {
            double from = 1.7976931348623157E+308d;
            _counter++;
            try
            {
                _ = checked((long)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((long)1.7976931348623157E+308d)' did not throw OverflowException.");
        }
        ConfirmSByteMinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMinValueCastToInt64IsFoldedCorrectly()
        {
            double sByteMinValue = -128.0;

            if (BreakUpFlow())
                return;

            if (checked((long)sByteMinValue) != -128)
            {
                Console.WriteLine($"'(long)-128.0' was evaluated to '{(long)sByteMinValue}'. Expected: '-128'.");
                _counter++;
            }
        }
        ConfirmSByteMaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToInt64IsFoldedCorrectly()
        {
            double sByteMaxValue = 127.0;

            if (BreakUpFlow())
                return;

            if (checked((long)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(long)127.0' was evaluated to '{(long)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderSByteMinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderSByteMinValueCastToInt64IsFoldedCorrectly()
        {
            double doubleOneDecrementUnderSByteMinValue = -128.00000000000003d;

            if (BreakUpFlow())
                return;

            if (checked((long)doubleOneDecrementUnderSByteMinValue) != -128)
            {
                Console.WriteLine($"'(long)-128.00000000000003d' was evaluated to '{(long)doubleOneDecrementUnderSByteMinValue}'. Expected: '-128'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderSByteMinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderSByteMinValueCastToInt64IsFoldedCorrectly()
        {
            double doubleOneFullDecrementUnderSByteMinValue = -129d;

            if (BreakUpFlow())
                return;

            if (checked((long)doubleOneFullDecrementUnderSByteMinValue) != -129)
            {
                Console.WriteLine($"'(long)-129d' was evaluated to '{(long)doubleOneFullDecrementUnderSByteMinValue}'. Expected: '-129'.");
                _counter++;
            }
        }
        ConfirmDoubleOneIncrementAboveSByteMinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveSByteMinValueCastToInt64IsFoldedCorrectly()
        {
            double doubleOneIncrementAboveSByteMinValue = -127.99999999999999d;

            if (BreakUpFlow())
                return;

            if (checked((long)doubleOneIncrementAboveSByteMinValue) != -127)
            {
                Console.WriteLine($"'(long)-127.99999999999999d' was evaluated to '{(long)doubleOneIncrementAboveSByteMinValue}'. Expected: '-127'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveSByteMinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveSByteMinValueCastToInt64IsFoldedCorrectly()
        {
            double doubleOneFullIncrementAboveSByteMinValue = -127d;

            if (BreakUpFlow())
                return;

            if (checked((long)doubleOneFullIncrementAboveSByteMinValue) != -127)
            {
                Console.WriteLine($"'(long)-127d' was evaluated to '{(long)doubleOneFullIncrementAboveSByteMinValue}'. Expected: '-127'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderSByteMaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderSByteMaxValueCastToInt64IsFoldedCorrectly()
        {
            double doubleOneDecrementUnderSByteMaxValue = 126.99999999999999d;

            if (BreakUpFlow())
                return;

            if (checked((long)doubleOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(long)126.99999999999999d' was evaluated to '{(long)doubleOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderSByteMaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderSByteMaxValueCastToInt64IsFoldedCorrectly()
        {
            double doubleOneFullDecrementUnderSByteMaxValue = 126d;

            if (BreakUpFlow())
                return;

            if (checked((long)doubleOneFullDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(long)126d' was evaluated to '{(long)doubleOneFullDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmDoubleOneIncrementAboveSByteMaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveSByteMaxValueCastToInt64IsFoldedCorrectly()
        {
            double doubleOneIncrementAboveSByteMaxValue = 127.00000000000001d;

            if (BreakUpFlow())
                return;

            if (checked((long)doubleOneIncrementAboveSByteMaxValue) != 127)
            {
                Console.WriteLine($"'(long)127.00000000000001d' was evaluated to '{(long)doubleOneIncrementAboveSByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveSByteMaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveSByteMaxValueCastToInt64IsFoldedCorrectly()
        {
            double doubleOneFullIncrementAboveSByteMaxValue = 128d;

            if (BreakUpFlow())
                return;

            if (checked((long)doubleOneFullIncrementAboveSByteMaxValue) != 128)
            {
                Console.WriteLine($"'(long)128d' was evaluated to '{(long)doubleOneFullIncrementAboveSByteMaxValue}'. Expected: '128'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMinValueCastToInt64IsFoldedCorrectly()
        {
            double integerOneDecrementUnderSByteMinValue = -129.0;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneDecrementUnderSByteMinValue) != -129)
            {
                Console.WriteLine($"'(long)-129.0' was evaluated to '{(long)integerOneDecrementUnderSByteMinValue}'. Expected: '-129'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMinValueCastToInt64IsFoldedCorrectly()
        {
            double integerOneIncrementAboveSByteMinValue = -127.0;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneIncrementAboveSByteMinValue) != -127)
            {
                Console.WriteLine($"'(long)-127.0' was evaluated to '{(long)integerOneIncrementAboveSByteMinValue}'. Expected: '-127'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToInt64IsFoldedCorrectly()
        {
            double integerOneDecrementUnderSByteMaxValue = 126.0;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(long)126.0' was evaluated to '{(long)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMaxValueCastToInt64IsFoldedCorrectly()
        {
            double integerOneIncrementAboveSByteMaxValue = 128.0;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneIncrementAboveSByteMaxValue) != 128)
            {
                Console.WriteLine($"'(long)128.0' was evaluated to '{(long)integerOneIncrementAboveSByteMaxValue}'. Expected: '128'.");
                _counter++;
            }
        }
        ConfirmByteMaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmByteMaxValueCastToInt64IsFoldedCorrectly()
        {
            double byteMaxValue = 255.0;

            if (BreakUpFlow())
                return;

            if (checked((long)byteMaxValue) != 255)
            {
                Console.WriteLine($"'(long)255.0' was evaluated to '{(long)byteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderByteMinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderByteMinValueCastToInt64IsFoldedCorrectly()
        {
            double doubleOneDecrementUnderByteMinValue = -5E-324d;

            if (BreakUpFlow())
                return;

            if (checked((long)doubleOneDecrementUnderByteMinValue) != 0)
            {
                Console.WriteLine($"'(long)-5E-324d' was evaluated to '{(long)doubleOneDecrementUnderByteMinValue}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderByteMinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderByteMinValueCastToInt64IsFoldedCorrectly()
        {
            double doubleOneFullDecrementUnderByteMinValue = -1d;

            if (BreakUpFlow())
                return;

            if (checked((long)doubleOneFullDecrementUnderByteMinValue) != -1)
            {
                Console.WriteLine($"'(long)-1d' was evaluated to '{(long)doubleOneFullDecrementUnderByteMinValue}'. Expected: '-1'.");
                _counter++;
            }
        }
        ConfirmDoubleOneIncrementAboveByteMinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveByteMinValueCastToInt64IsFoldedCorrectly()
        {
            double doubleOneIncrementAboveByteMinValue = 5E-324d;

            if (BreakUpFlow())
                return;

            if (checked((long)doubleOneIncrementAboveByteMinValue) != 0)
            {
                Console.WriteLine($"'(long)5E-324d' was evaluated to '{(long)doubleOneIncrementAboveByteMinValue}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveByteMinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveByteMinValueCastToInt64IsFoldedCorrectly()
        {
            double doubleOneFullIncrementAboveByteMinValue = 1d;

            if (BreakUpFlow())
                return;

            if (checked((long)doubleOneFullIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(long)1d' was evaluated to '{(long)doubleOneFullIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderByteMaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderByteMaxValueCastToInt64IsFoldedCorrectly()
        {
            double doubleOneDecrementUnderByteMaxValue = 254.99999999999997d;

            if (BreakUpFlow())
                return;

            if (checked((long)doubleOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(long)254.99999999999997d' was evaluated to '{(long)doubleOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderByteMaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderByteMaxValueCastToInt64IsFoldedCorrectly()
        {
            double doubleOneFullDecrementUnderByteMaxValue = 254d;

            if (BreakUpFlow())
                return;

            if (checked((long)doubleOneFullDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(long)254d' was evaluated to '{(long)doubleOneFullDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmDoubleOneIncrementAboveByteMaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveByteMaxValueCastToInt64IsFoldedCorrectly()
        {
            double doubleOneIncrementAboveByteMaxValue = 255.00000000000003d;

            if (BreakUpFlow())
                return;

            if (checked((long)doubleOneIncrementAboveByteMaxValue) != 255)
            {
                Console.WriteLine($"'(long)255.00000000000003d' was evaluated to '{(long)doubleOneIncrementAboveByteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveByteMaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveByteMaxValueCastToInt64IsFoldedCorrectly()
        {
            double doubleOneFullIncrementAboveByteMaxValue = 256d;

            if (BreakUpFlow())
                return;

            if (checked((long)doubleOneFullIncrementAboveByteMaxValue) != 256)
            {
                Console.WriteLine($"'(long)256d' was evaluated to '{(long)doubleOneFullIncrementAboveByteMaxValue}'. Expected: '256'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMinValueCastToInt64IsFoldedCorrectly()
        {
            double integerOneDecrementUnderByteMinValue = -1.0;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneDecrementUnderByteMinValue) != -1)
            {
                Console.WriteLine($"'(long)-1.0' was evaluated to '{(long)integerOneDecrementUnderByteMinValue}'. Expected: '-1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToInt64IsFoldedCorrectly()
        {
            double integerOneIncrementAboveByteMinValue = 1.0;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(long)1.0' was evaluated to '{(long)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMaxValueCastToInt64IsFoldedCorrectly()
        {
            double integerOneDecrementUnderByteMaxValue = 254.0;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(long)254.0' was evaluated to '{(long)integerOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMaxValueCastToInt64IsFoldedCorrectly()
        {
            double integerOneIncrementAboveByteMaxValue = 256.0;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneIncrementAboveByteMaxValue) != 256)
            {
                Console.WriteLine($"'(long)256.0' was evaluated to '{(long)integerOneIncrementAboveByteMaxValue}'. Expected: '256'.");
                _counter++;
            }
        }
        ConfirmInt16MinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MinValueCastToInt64IsFoldedCorrectly()
        {
            double int16MinValue = -32768.0;

            if (BreakUpFlow())
                return;

            if (checked((long)int16MinValue) != -32768)
            {
                Console.WriteLine($"'(long)-32768.0' was evaluated to '{(long)int16MinValue}'. Expected: '-32768'.");
                _counter++;
            }
        }
        ConfirmInt16MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MaxValueCastToInt64IsFoldedCorrectly()
        {
            double int16MaxValue = 32767.0;

            if (BreakUpFlow())
                return;

            if (checked((long)int16MaxValue) != 32767)
            {
                Console.WriteLine($"'(long)32767.0' was evaluated to '{(long)int16MaxValue}'. Expected: '32767'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderInt16MinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderInt16MinValueCastToInt64IsFoldedCorrectly()
        {
            double doubleOneDecrementUnderInt16MinValue = -32768.00000000001d;

            if (BreakUpFlow())
                return;

            if (checked((long)doubleOneDecrementUnderInt16MinValue) != -32768)
            {
                Console.WriteLine($"'(long)-32768.00000000001d' was evaluated to '{(long)doubleOneDecrementUnderInt16MinValue}'. Expected: '-32768'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderInt16MinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderInt16MinValueCastToInt64IsFoldedCorrectly()
        {
            double doubleOneFullDecrementUnderInt16MinValue = -32769d;

            if (BreakUpFlow())
                return;

            if (checked((long)doubleOneFullDecrementUnderInt16MinValue) != -32769)
            {
                Console.WriteLine($"'(long)-32769d' was evaluated to '{(long)doubleOneFullDecrementUnderInt16MinValue}'. Expected: '-32769'.");
                _counter++;
            }
        }
        ConfirmDoubleOneIncrementAboveInt16MinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveInt16MinValueCastToInt64IsFoldedCorrectly()
        {
            double doubleOneIncrementAboveInt16MinValue = -32767.999999999996d;

            if (BreakUpFlow())
                return;

            if (checked((long)doubleOneIncrementAboveInt16MinValue) != -32767)
            {
                Console.WriteLine($"'(long)-32767.999999999996d' was evaluated to '{(long)doubleOneIncrementAboveInt16MinValue}'. Expected: '-32767'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveInt16MinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveInt16MinValueCastToInt64IsFoldedCorrectly()
        {
            double doubleOneFullIncrementAboveInt16MinValue = -32767d;

            if (BreakUpFlow())
                return;

            if (checked((long)doubleOneFullIncrementAboveInt16MinValue) != -32767)
            {
                Console.WriteLine($"'(long)-32767d' was evaluated to '{(long)doubleOneFullIncrementAboveInt16MinValue}'. Expected: '-32767'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderInt16MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderInt16MaxValueCastToInt64IsFoldedCorrectly()
        {
            double doubleOneDecrementUnderInt16MaxValue = 32766.999999999996d;

            if (BreakUpFlow())
                return;

            if (checked((long)doubleOneDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(long)32766.999999999996d' was evaluated to '{(long)doubleOneDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderInt16MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderInt16MaxValueCastToInt64IsFoldedCorrectly()
        {
            double doubleOneFullDecrementUnderInt16MaxValue = 32766d;

            if (BreakUpFlow())
                return;

            if (checked((long)doubleOneFullDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(long)32766d' was evaluated to '{(long)doubleOneFullDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
        ConfirmDoubleOneIncrementAboveInt16MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveInt16MaxValueCastToInt64IsFoldedCorrectly()
        {
            double doubleOneIncrementAboveInt16MaxValue = 32767.000000000004d;

            if (BreakUpFlow())
                return;

            if (checked((long)doubleOneIncrementAboveInt16MaxValue) != 32767)
            {
                Console.WriteLine($"'(long)32767.000000000004d' was evaluated to '{(long)doubleOneIncrementAboveInt16MaxValue}'. Expected: '32767'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveInt16MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveInt16MaxValueCastToInt64IsFoldedCorrectly()
        {
            double doubleOneFullIncrementAboveInt16MaxValue = 32768d;

            if (BreakUpFlow())
                return;

            if (checked((long)doubleOneFullIncrementAboveInt16MaxValue) != 32768)
            {
                Console.WriteLine($"'(long)32768d' was evaluated to '{(long)doubleOneFullIncrementAboveInt16MaxValue}'. Expected: '32768'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt16MinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MinValueCastToInt64IsFoldedCorrectly()
        {
            double integerOneDecrementUnderInt16MinValue = -32769.0;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneDecrementUnderInt16MinValue) != -32769)
            {
                Console.WriteLine($"'(long)-32769.0' was evaluated to '{(long)integerOneDecrementUnderInt16MinValue}'. Expected: '-32769'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt16MinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt16MinValueCastToInt64IsFoldedCorrectly()
        {
            double integerOneIncrementAboveInt16MinValue = -32767.0;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneIncrementAboveInt16MinValue) != -32767)
            {
                Console.WriteLine($"'(long)-32767.0' was evaluated to '{(long)integerOneIncrementAboveInt16MinValue}'. Expected: '-32767'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt16MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MaxValueCastToInt64IsFoldedCorrectly()
        {
            double integerOneDecrementUnderInt16MaxValue = 32766.0;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(long)32766.0' was evaluated to '{(long)integerOneDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt16MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt16MaxValueCastToInt64IsFoldedCorrectly()
        {
            double integerOneIncrementAboveInt16MaxValue = 32768.0;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneIncrementAboveInt16MaxValue) != 32768)
            {
                Console.WriteLine($"'(long)32768.0' was evaluated to '{(long)integerOneIncrementAboveInt16MaxValue}'. Expected: '32768'.");
                _counter++;
            }
        }
        ConfirmUInt16MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt16MaxValueCastToInt64IsFoldedCorrectly()
        {
            double uInt16MaxValue = 65535.0;

            if (BreakUpFlow())
                return;

            if (checked((long)uInt16MaxValue) != 65535)
            {
                Console.WriteLine($"'(long)65535.0' was evaluated to '{(long)uInt16MaxValue}'. Expected: '65535'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderUInt16MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderUInt16MaxValueCastToInt64IsFoldedCorrectly()
        {
            double doubleOneDecrementUnderUInt16MaxValue = 65534.99999999999d;

            if (BreakUpFlow())
                return;

            if (checked((long)doubleOneDecrementUnderUInt16MaxValue) != 65534)
            {
                Console.WriteLine($"'(long)65534.99999999999d' was evaluated to '{(long)doubleOneDecrementUnderUInt16MaxValue}'. Expected: '65534'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderUInt16MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderUInt16MaxValueCastToInt64IsFoldedCorrectly()
        {
            double doubleOneFullDecrementUnderUInt16MaxValue = 65534d;

            if (BreakUpFlow())
                return;

            if (checked((long)doubleOneFullDecrementUnderUInt16MaxValue) != 65534)
            {
                Console.WriteLine($"'(long)65534d' was evaluated to '{(long)doubleOneFullDecrementUnderUInt16MaxValue}'. Expected: '65534'.");
                _counter++;
            }
        }
        ConfirmDoubleOneIncrementAboveUInt16MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveUInt16MaxValueCastToInt64IsFoldedCorrectly()
        {
            double doubleOneIncrementAboveUInt16MaxValue = 65535.00000000001d;

            if (BreakUpFlow())
                return;

            if (checked((long)doubleOneIncrementAboveUInt16MaxValue) != 65535)
            {
                Console.WriteLine($"'(long)65535.00000000001d' was evaluated to '{(long)doubleOneIncrementAboveUInt16MaxValue}'. Expected: '65535'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveUInt16MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveUInt16MaxValueCastToInt64IsFoldedCorrectly()
        {
            double doubleOneFullIncrementAboveUInt16MaxValue = 65536d;

            if (BreakUpFlow())
                return;

            if (checked((long)doubleOneFullIncrementAboveUInt16MaxValue) != 65536)
            {
                Console.WriteLine($"'(long)65536d' was evaluated to '{(long)doubleOneFullIncrementAboveUInt16MaxValue}'. Expected: '65536'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderUInt16MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt16MaxValueCastToInt64IsFoldedCorrectly()
        {
            double integerOneDecrementUnderUInt16MaxValue = 65534.0;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneDecrementUnderUInt16MaxValue) != 65534)
            {
                Console.WriteLine($"'(long)65534.0' was evaluated to '{(long)integerOneDecrementUnderUInt16MaxValue}'. Expected: '65534'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToInt64IsFoldedCorrectly()
        {
            double integerOneIncrementAboveUInt16MaxValue = 65536.0;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneIncrementAboveUInt16MaxValue) != 65536)
            {
                Console.WriteLine($"'(long)65536.0' was evaluated to '{(long)integerOneIncrementAboveUInt16MaxValue}'. Expected: '65536'.");
                _counter++;
            }
        }
        ConfirmInt32MinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MinValueCastToInt64IsFoldedCorrectly()
        {
            double int32MinValue = -2147483648.0;

            if (BreakUpFlow())
                return;

            if (checked((long)int32MinValue) != -2147483648)
            {
                Console.WriteLine($"'(long)-2147483648.0' was evaluated to '{(long)int32MinValue}'. Expected: '-2147483648'.");
                _counter++;
            }
        }
        ConfirmInt32MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MaxValueCastToInt64IsFoldedCorrectly()
        {
            double int32MaxValue = 2147483647.0;

            if (BreakUpFlow())
                return;

            if (checked((long)int32MaxValue) != 2147483647)
            {
                Console.WriteLine($"'(long)2147483647.0' was evaluated to '{(long)int32MaxValue}'. Expected: '2147483647'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderInt32MinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderInt32MinValueCastToInt64IsFoldedCorrectly()
        {
            double doubleOneDecrementUnderInt32MinValue = -2147483648.0000005d;

            if (BreakUpFlow())
                return;

            if (checked((long)doubleOneDecrementUnderInt32MinValue) != -2147483648)
            {
                Console.WriteLine($"'(long)-2147483648.0000005d' was evaluated to '{(long)doubleOneDecrementUnderInt32MinValue}'. Expected: '-2147483648'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderInt32MinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderInt32MinValueCastToInt64IsFoldedCorrectly()
        {
            double doubleOneFullDecrementUnderInt32MinValue = -2147483649d;

            if (BreakUpFlow())
                return;

            if (checked((long)doubleOneFullDecrementUnderInt32MinValue) != -2147483649)
            {
                Console.WriteLine($"'(long)-2147483649d' was evaluated to '{(long)doubleOneFullDecrementUnderInt32MinValue}'. Expected: '-2147483649'.");
                _counter++;
            }
        }
        ConfirmDoubleOneIncrementAboveInt32MinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveInt32MinValueCastToInt64IsFoldedCorrectly()
        {
            double doubleOneIncrementAboveInt32MinValue = -2147483647.9999998d;

            if (BreakUpFlow())
                return;

            if (checked((long)doubleOneIncrementAboveInt32MinValue) != -2147483647)
            {
                Console.WriteLine($"'(long)-2147483647.9999998d' was evaluated to '{(long)doubleOneIncrementAboveInt32MinValue}'. Expected: '-2147483647'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveInt32MinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveInt32MinValueCastToInt64IsFoldedCorrectly()
        {
            double doubleOneFullIncrementAboveInt32MinValue = -2147483647d;

            if (BreakUpFlow())
                return;

            if (checked((long)doubleOneFullIncrementAboveInt32MinValue) != -2147483647)
            {
                Console.WriteLine($"'(long)-2147483647d' was evaluated to '{(long)doubleOneFullIncrementAboveInt32MinValue}'. Expected: '-2147483647'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderInt32MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderInt32MaxValueCastToInt64IsFoldedCorrectly()
        {
            double doubleOneDecrementUnderInt32MaxValue = 2147483646.9999998d;

            if (BreakUpFlow())
                return;

            if (checked((long)doubleOneDecrementUnderInt32MaxValue) != 2147483646)
            {
                Console.WriteLine($"'(long)2147483646.9999998d' was evaluated to '{(long)doubleOneDecrementUnderInt32MaxValue}'. Expected: '2147483646'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderInt32MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderInt32MaxValueCastToInt64IsFoldedCorrectly()
        {
            double doubleOneFullDecrementUnderInt32MaxValue = 2147483646d;

            if (BreakUpFlow())
                return;

            if (checked((long)doubleOneFullDecrementUnderInt32MaxValue) != 2147483646)
            {
                Console.WriteLine($"'(long)2147483646d' was evaluated to '{(long)doubleOneFullDecrementUnderInt32MaxValue}'. Expected: '2147483646'.");
                _counter++;
            }
        }
        ConfirmDoubleOneIncrementAboveInt32MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveInt32MaxValueCastToInt64IsFoldedCorrectly()
        {
            double doubleOneIncrementAboveInt32MaxValue = 2147483647.0000002d;

            if (BreakUpFlow())
                return;

            if (checked((long)doubleOneIncrementAboveInt32MaxValue) != 2147483647)
            {
                Console.WriteLine($"'(long)2147483647.0000002d' was evaluated to '{(long)doubleOneIncrementAboveInt32MaxValue}'. Expected: '2147483647'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveInt32MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveInt32MaxValueCastToInt64IsFoldedCorrectly()
        {
            double doubleOneFullIncrementAboveInt32MaxValue = 2147483648d;

            if (BreakUpFlow())
                return;

            if (checked((long)doubleOneFullIncrementAboveInt32MaxValue) != 2147483648)
            {
                Console.WriteLine($"'(long)2147483648d' was evaluated to '{(long)doubleOneFullIncrementAboveInt32MaxValue}'. Expected: '2147483648'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt32MinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt32MinValueCastToInt64IsFoldedCorrectly()
        {
            double integerOneDecrementUnderInt32MinValue = -2147483649.0;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneDecrementUnderInt32MinValue) != -2147483649)
            {
                Console.WriteLine($"'(long)-2147483649.0' was evaluated to '{(long)integerOneDecrementUnderInt32MinValue}'. Expected: '-2147483649'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt32MinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt32MinValueCastToInt64IsFoldedCorrectly()
        {
            double integerOneIncrementAboveInt32MinValue = -2147483647.0;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneIncrementAboveInt32MinValue) != -2147483647)
            {
                Console.WriteLine($"'(long)-2147483647.0' was evaluated to '{(long)integerOneIncrementAboveInt32MinValue}'. Expected: '-2147483647'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt32MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt32MaxValueCastToInt64IsFoldedCorrectly()
        {
            double integerOneDecrementUnderInt32MaxValue = 2147483646.0;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneDecrementUnderInt32MaxValue) != 2147483646)
            {
                Console.WriteLine($"'(long)2147483646.0' was evaluated to '{(long)integerOneDecrementUnderInt32MaxValue}'. Expected: '2147483646'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt32MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt32MaxValueCastToInt64IsFoldedCorrectly()
        {
            double integerOneIncrementAboveInt32MaxValue = 2147483648.0;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneIncrementAboveInt32MaxValue) != 2147483648)
            {
                Console.WriteLine($"'(long)2147483648.0' was evaluated to '{(long)integerOneIncrementAboveInt32MaxValue}'. Expected: '2147483648'.");
                _counter++;
            }
        }
        ConfirmUInt32MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32MaxValueCastToInt64IsFoldedCorrectly()
        {
            double uInt32MaxValue = 4294967295.0;

            if (BreakUpFlow())
                return;

            if (checked((long)uInt32MaxValue) != 4294967295)
            {
                Console.WriteLine($"'(long)4294967295.0' was evaluated to '{(long)uInt32MaxValue}'. Expected: '4294967295'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderUInt32MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderUInt32MaxValueCastToInt64IsFoldedCorrectly()
        {
            double doubleOneDecrementUnderUInt32MaxValue = 4294967294.9999995d;

            if (BreakUpFlow())
                return;

            if (checked((long)doubleOneDecrementUnderUInt32MaxValue) != 4294967294)
            {
                Console.WriteLine($"'(long)4294967294.9999995d' was evaluated to '{(long)doubleOneDecrementUnderUInt32MaxValue}'. Expected: '4294967294'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderUInt32MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderUInt32MaxValueCastToInt64IsFoldedCorrectly()
        {
            double doubleOneFullDecrementUnderUInt32MaxValue = 4294967294d;

            if (BreakUpFlow())
                return;

            if (checked((long)doubleOneFullDecrementUnderUInt32MaxValue) != 4294967294)
            {
                Console.WriteLine($"'(long)4294967294d' was evaluated to '{(long)doubleOneFullDecrementUnderUInt32MaxValue}'. Expected: '4294967294'.");
                _counter++;
            }
        }
        ConfirmDoubleOneIncrementAboveUInt32MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveUInt32MaxValueCastToInt64IsFoldedCorrectly()
        {
            double doubleOneIncrementAboveUInt32MaxValue = 4294967295.0000005d;

            if (BreakUpFlow())
                return;

            if (checked((long)doubleOneIncrementAboveUInt32MaxValue) != 4294967295)
            {
                Console.WriteLine($"'(long)4294967295.0000005d' was evaluated to '{(long)doubleOneIncrementAboveUInt32MaxValue}'. Expected: '4294967295'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveUInt32MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveUInt32MaxValueCastToInt64IsFoldedCorrectly()
        {
            double doubleOneFullIncrementAboveUInt32MaxValue = 4294967296d;

            if (BreakUpFlow())
                return;

            if (checked((long)doubleOneFullIncrementAboveUInt32MaxValue) != 4294967296)
            {
                Console.WriteLine($"'(long)4294967296d' was evaluated to '{(long)doubleOneFullIncrementAboveUInt32MaxValue}'. Expected: '4294967296'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderUInt32MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt32MaxValueCastToInt64IsFoldedCorrectly()
        {
            double integerOneDecrementUnderUInt32MaxValue = 4294967294.0;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneDecrementUnderUInt32MaxValue) != 4294967294)
            {
                Console.WriteLine($"'(long)4294967294.0' was evaluated to '{(long)integerOneDecrementUnderUInt32MaxValue}'. Expected: '4294967294'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveUInt32MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt32MaxValueCastToInt64IsFoldedCorrectly()
        {
            double integerOneIncrementAboveUInt32MaxValue = 4294967296.0;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneIncrementAboveUInt32MaxValue) != 4294967296)
            {
                Console.WriteLine($"'(long)4294967296.0' was evaluated to '{(long)integerOneIncrementAboveUInt32MaxValue}'. Expected: '4294967296'.");
                _counter++;
            }
        }
        ConfirmInt64MinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MinValueCastToInt64IsFoldedCorrectly()
        {
            double int64MinValue = -9223372036854775808.0;

            if (BreakUpFlow())
                return;

            if (checked((long)int64MinValue) != -9223372036854775808)
            {
                Console.WriteLine($"'(long)-9223372036854775808.0' was evaluated to '{(long)int64MinValue}'. Expected: '-9223372036854775808'.");
                _counter++;
            }
        }
        ConfirmInt64MaxValueCastToInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MaxValueCastToInt64Overflows()
        {
            double from = 9223372036854775807.0;
            _counter++;
            try
            {
                _ = checked((long)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((long)9223372036854775807.0)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveInt64MinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveInt64MinValueCastToInt64IsFoldedCorrectly()
        {
            double doubleOneIncrementAboveInt64MinValue = -9.223372036854775E+18d;

            if (BreakUpFlow())
                return;

            if (checked((long)doubleOneIncrementAboveInt64MinValue) != -9223372036854774784)
            {
                Console.WriteLine($"'(long)-9.223372036854775E+18d' was evaluated to '{(long)doubleOneIncrementAboveInt64MinValue}'. Expected: '-9223372036854774784'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderInt64MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderInt64MaxValueCastToInt64IsFoldedCorrectly()
        {
            double doubleOneDecrementUnderInt64MaxValue = 9.223372036854775E+18d;

            if (BreakUpFlow())
                return;

            if (checked((long)doubleOneDecrementUnderInt64MaxValue) != 9223372036854774784)
            {
                Console.WriteLine($"'(long)9.223372036854775E+18d' was evaluated to '{(long)doubleOneDecrementUnderInt64MaxValue}'. Expected: '9223372036854774784'.");
                _counter++;
            }
        }
        ConfirmDoubleOneIncrementAboveInt64MaxValueCastToInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveInt64MaxValueCastToInt64Overflows()
        {
            double from = 9.223372036854778E+18d;
            _counter++;
            try
            {
                _ = checked((long)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((long)9.223372036854778E+18d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullIncrementAboveInt64MaxValueCastToInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveInt64MaxValueCastToInt64Overflows()
        {
            double from = 9.223372036854776E+18d;
            _counter++;
            try
            {
                _ = checked((long)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((long)9.223372036854776E+18d)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt64MinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt64MinValueCastToInt64IsFoldedCorrectly()
        {
            double integerOneIncrementAboveInt64MinValue = -9223372036854775807.0;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneIncrementAboveInt64MinValue) != -9223372036854775808)
            {
                Console.WriteLine($"'(long)-9223372036854775807.0' was evaluated to '{(long)integerOneIncrementAboveInt64MinValue}'. Expected: '-9223372036854775808'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt64MaxValueCastToInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt64MaxValueCastToInt64Overflows()
        {
            double from = 9223372036854775806.0;
            _counter++;
            try
            {
                _ = checked((long)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((long)9223372036854775806.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt64MaxValueCastToInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt64MaxValueCastToInt64Overflows()
        {
            double from = 9223372036854775808.0;
            _counter++;
            try
            {
                _ = checked((long)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((long)9223372036854775808.0)' did not throw OverflowException.");
        }
        ConfirmUInt64MaxValueCastToInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64MaxValueCastToInt64Overflows()
        {
            double from = 18446744073709551615.0;
            _counter++;
            try
            {
                _ = checked((long)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((long)18446744073709551615.0)' did not throw OverflowException.");
        }
        ConfirmDoubleOneDecrementUnderUInt64MaxValueCastToInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderUInt64MaxValueCastToInt64Overflows()
        {
            double from = 1.844674407370955E+19d;
            _counter++;
            try
            {
                _ = checked((long)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((long)1.844674407370955E+19d)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderUInt64MaxValueCastToInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt64MaxValueCastToInt64Overflows()
        {
            double from = 18446744073709551614.0;
            _counter++;
            try
            {
                _ = checked((long)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((long)18446744073709551614.0)' did not throw OverflowException.");
        }
    }

    private static void TestCastingDoubleToUInt64()
    {
        ConfirmIntegerZeroCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToUInt64IsFoldedCorrectly()
        {
            double integerZero = 0.0;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerZero) != 0)
            {
                Console.WriteLine($"'(ulong)0.0' was evaluated to '{(ulong)integerZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmDoubleMinusZeroCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleMinusZeroCastToUInt64IsFoldedCorrectly()
        {
            double doubleMinusZero = -0d;

            if (BreakUpFlow())
                return;

            if (checked((ulong)doubleMinusZero) != 0)
            {
                Console.WriteLine($"'(ulong)-0d' was evaluated to '{(ulong)doubleMinusZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmDoubleZeroCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleZeroCastToUInt64IsFoldedCorrectly()
        {
            double doubleZero = 0d;

            if (BreakUpFlow())
                return;

            if (checked((ulong)doubleZero) != 0)
            {
                Console.WriteLine($"'(ulong)0d' was evaluated to '{(ulong)doubleZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmDoubleMinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleMinValueCastToUInt64Overflows()
        {
            double from = -1.7976931348623157E+308d;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-1.7976931348623157E+308d)' did not throw OverflowException.");
        }
        ConfirmDoubleMaxValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleMaxValueCastToUInt64Overflows()
        {
            double from = 1.7976931348623157E+308d;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)1.7976931348623157E+308d)' did not throw OverflowException.");
        }
        ConfirmSByteMinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMinValueCastToUInt64Overflows()
        {
            double from = -128.0;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-128.0)' did not throw OverflowException.");
        }
        ConfirmSByteMaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToUInt64IsFoldedCorrectly()
        {
            double sByteMaxValue = 127.0;

            if (BreakUpFlow())
                return;

            if (checked((ulong)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(ulong)127.0' was evaluated to '{(ulong)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderSByteMinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderSByteMinValueCastToUInt64Overflows()
        {
            double from = -128.00000000000003d;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-128.00000000000003d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullDecrementUnderSByteMinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderSByteMinValueCastToUInt64Overflows()
        {
            double from = -129d;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-129d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveSByteMinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveSByteMinValueCastToUInt64Overflows()
        {
            double from = -127.99999999999999d;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-127.99999999999999d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullIncrementAboveSByteMinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveSByteMinValueCastToUInt64Overflows()
        {
            double from = -127d;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-127d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneDecrementUnderSByteMaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderSByteMaxValueCastToUInt64IsFoldedCorrectly()
        {
            double doubleOneDecrementUnderSByteMaxValue = 126.99999999999999d;

            if (BreakUpFlow())
                return;

            if (checked((ulong)doubleOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(ulong)126.99999999999999d' was evaluated to '{(ulong)doubleOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderSByteMaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderSByteMaxValueCastToUInt64IsFoldedCorrectly()
        {
            double doubleOneFullDecrementUnderSByteMaxValue = 126d;

            if (BreakUpFlow())
                return;

            if (checked((ulong)doubleOneFullDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(ulong)126d' was evaluated to '{(ulong)doubleOneFullDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmDoubleOneIncrementAboveSByteMaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveSByteMaxValueCastToUInt64IsFoldedCorrectly()
        {
            double doubleOneIncrementAboveSByteMaxValue = 127.00000000000001d;

            if (BreakUpFlow())
                return;

            if (checked((ulong)doubleOneIncrementAboveSByteMaxValue) != 127)
            {
                Console.WriteLine($"'(ulong)127.00000000000001d' was evaluated to '{(ulong)doubleOneIncrementAboveSByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveSByteMaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveSByteMaxValueCastToUInt64IsFoldedCorrectly()
        {
            double doubleOneFullIncrementAboveSByteMaxValue = 128d;

            if (BreakUpFlow())
                return;

            if (checked((ulong)doubleOneFullIncrementAboveSByteMaxValue) != 128)
            {
                Console.WriteLine($"'(ulong)128d' was evaluated to '{(ulong)doubleOneFullIncrementAboveSByteMaxValue}'. Expected: '128'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMinValueCastToUInt64Overflows()
        {
            double from = -129.0;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-129.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveSByteMinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMinValueCastToUInt64Overflows()
        {
            double from = -127.0;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-127.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToUInt64IsFoldedCorrectly()
        {
            double integerOneDecrementUnderSByteMaxValue = 126.0;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(ulong)126.0' was evaluated to '{(ulong)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMaxValueCastToUInt64IsFoldedCorrectly()
        {
            double integerOneIncrementAboveSByteMaxValue = 128.0;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneIncrementAboveSByteMaxValue) != 128)
            {
                Console.WriteLine($"'(ulong)128.0' was evaluated to '{(ulong)integerOneIncrementAboveSByteMaxValue}'. Expected: '128'.");
                _counter++;
            }
        }
        ConfirmByteMaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmByteMaxValueCastToUInt64IsFoldedCorrectly()
        {
            double byteMaxValue = 255.0;

            if (BreakUpFlow())
                return;

            if (checked((ulong)byteMaxValue) != 255)
            {
                Console.WriteLine($"'(ulong)255.0' was evaluated to '{(ulong)byteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderByteMinValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderByteMinValueCastToUInt64IsFoldedCorrectly()
        {
            double doubleOneDecrementUnderByteMinValue = -5E-324d;

            if (BreakUpFlow())
                return;

            if (checked((ulong)doubleOneDecrementUnderByteMinValue) != 0)
            {
                Console.WriteLine($"'(ulong)-5E-324d' was evaluated to '{(ulong)doubleOneDecrementUnderByteMinValue}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderByteMinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderByteMinValueCastToUInt64Overflows()
        {
            double from = -1d;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-1d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveByteMinValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveByteMinValueCastToUInt64IsFoldedCorrectly()
        {
            double doubleOneIncrementAboveByteMinValue = 5E-324d;

            if (BreakUpFlow())
                return;

            if (checked((ulong)doubleOneIncrementAboveByteMinValue) != 0)
            {
                Console.WriteLine($"'(ulong)5E-324d' was evaluated to '{(ulong)doubleOneIncrementAboveByteMinValue}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveByteMinValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveByteMinValueCastToUInt64IsFoldedCorrectly()
        {
            double doubleOneFullIncrementAboveByteMinValue = 1d;

            if (BreakUpFlow())
                return;

            if (checked((ulong)doubleOneFullIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(ulong)1d' was evaluated to '{(ulong)doubleOneFullIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderByteMaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderByteMaxValueCastToUInt64IsFoldedCorrectly()
        {
            double doubleOneDecrementUnderByteMaxValue = 254.99999999999997d;

            if (BreakUpFlow())
                return;

            if (checked((ulong)doubleOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(ulong)254.99999999999997d' was evaluated to '{(ulong)doubleOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderByteMaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderByteMaxValueCastToUInt64IsFoldedCorrectly()
        {
            double doubleOneFullDecrementUnderByteMaxValue = 254d;

            if (BreakUpFlow())
                return;

            if (checked((ulong)doubleOneFullDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(ulong)254d' was evaluated to '{(ulong)doubleOneFullDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmDoubleOneIncrementAboveByteMaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveByteMaxValueCastToUInt64IsFoldedCorrectly()
        {
            double doubleOneIncrementAboveByteMaxValue = 255.00000000000003d;

            if (BreakUpFlow())
                return;

            if (checked((ulong)doubleOneIncrementAboveByteMaxValue) != 255)
            {
                Console.WriteLine($"'(ulong)255.00000000000003d' was evaluated to '{(ulong)doubleOneIncrementAboveByteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveByteMaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveByteMaxValueCastToUInt64IsFoldedCorrectly()
        {
            double doubleOneFullIncrementAboveByteMaxValue = 256d;

            if (BreakUpFlow())
                return;

            if (checked((ulong)doubleOneFullIncrementAboveByteMaxValue) != 256)
            {
                Console.WriteLine($"'(ulong)256d' was evaluated to '{(ulong)doubleOneFullIncrementAboveByteMaxValue}'. Expected: '256'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMinValueCastToUInt64Overflows()
        {
            double from = -1.0;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-1.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToUInt64IsFoldedCorrectly()
        {
            double integerOneIncrementAboveByteMinValue = 1.0;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(ulong)1.0' was evaluated to '{(ulong)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMaxValueCastToUInt64IsFoldedCorrectly()
        {
            double integerOneDecrementUnderByteMaxValue = 254.0;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(ulong)254.0' was evaluated to '{(ulong)integerOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMaxValueCastToUInt64IsFoldedCorrectly()
        {
            double integerOneIncrementAboveByteMaxValue = 256.0;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneIncrementAboveByteMaxValue) != 256)
            {
                Console.WriteLine($"'(ulong)256.0' was evaluated to '{(ulong)integerOneIncrementAboveByteMaxValue}'. Expected: '256'.");
                _counter++;
            }
        }
        ConfirmInt16MinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MinValueCastToUInt64Overflows()
        {
            double from = -32768.0;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-32768.0)' did not throw OverflowException.");
        }
        ConfirmInt16MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MaxValueCastToUInt64IsFoldedCorrectly()
        {
            double int16MaxValue = 32767.0;

            if (BreakUpFlow())
                return;

            if (checked((ulong)int16MaxValue) != 32767)
            {
                Console.WriteLine($"'(ulong)32767.0' was evaluated to '{(ulong)int16MaxValue}'. Expected: '32767'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderInt16MinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderInt16MinValueCastToUInt64Overflows()
        {
            double from = -32768.00000000001d;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-32768.00000000001d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullDecrementUnderInt16MinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderInt16MinValueCastToUInt64Overflows()
        {
            double from = -32769d;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-32769d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveInt16MinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveInt16MinValueCastToUInt64Overflows()
        {
            double from = -32767.999999999996d;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-32767.999999999996d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullIncrementAboveInt16MinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveInt16MinValueCastToUInt64Overflows()
        {
            double from = -32767d;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-32767d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneDecrementUnderInt16MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderInt16MaxValueCastToUInt64IsFoldedCorrectly()
        {
            double doubleOneDecrementUnderInt16MaxValue = 32766.999999999996d;

            if (BreakUpFlow())
                return;

            if (checked((ulong)doubleOneDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(ulong)32766.999999999996d' was evaluated to '{(ulong)doubleOneDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderInt16MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderInt16MaxValueCastToUInt64IsFoldedCorrectly()
        {
            double doubleOneFullDecrementUnderInt16MaxValue = 32766d;

            if (BreakUpFlow())
                return;

            if (checked((ulong)doubleOneFullDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(ulong)32766d' was evaluated to '{(ulong)doubleOneFullDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
        ConfirmDoubleOneIncrementAboveInt16MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveInt16MaxValueCastToUInt64IsFoldedCorrectly()
        {
            double doubleOneIncrementAboveInt16MaxValue = 32767.000000000004d;

            if (BreakUpFlow())
                return;

            if (checked((ulong)doubleOneIncrementAboveInt16MaxValue) != 32767)
            {
                Console.WriteLine($"'(ulong)32767.000000000004d' was evaluated to '{(ulong)doubleOneIncrementAboveInt16MaxValue}'. Expected: '32767'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveInt16MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveInt16MaxValueCastToUInt64IsFoldedCorrectly()
        {
            double doubleOneFullIncrementAboveInt16MaxValue = 32768d;

            if (BreakUpFlow())
                return;

            if (checked((ulong)doubleOneFullIncrementAboveInt16MaxValue) != 32768)
            {
                Console.WriteLine($"'(ulong)32768d' was evaluated to '{(ulong)doubleOneFullIncrementAboveInt16MaxValue}'. Expected: '32768'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt16MinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MinValueCastToUInt64Overflows()
        {
            double from = -32769.0;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-32769.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt16MinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt16MinValueCastToUInt64Overflows()
        {
            double from = -32767.0;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-32767.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt16MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MaxValueCastToUInt64IsFoldedCorrectly()
        {
            double integerOneDecrementUnderInt16MaxValue = 32766.0;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(ulong)32766.0' was evaluated to '{(ulong)integerOneDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt16MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt16MaxValueCastToUInt64IsFoldedCorrectly()
        {
            double integerOneIncrementAboveInt16MaxValue = 32768.0;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneIncrementAboveInt16MaxValue) != 32768)
            {
                Console.WriteLine($"'(ulong)32768.0' was evaluated to '{(ulong)integerOneIncrementAboveInt16MaxValue}'. Expected: '32768'.");
                _counter++;
            }
        }
        ConfirmUInt16MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt16MaxValueCastToUInt64IsFoldedCorrectly()
        {
            double uInt16MaxValue = 65535.0;

            if (BreakUpFlow())
                return;

            if (checked((ulong)uInt16MaxValue) != 65535)
            {
                Console.WriteLine($"'(ulong)65535.0' was evaluated to '{(ulong)uInt16MaxValue}'. Expected: '65535'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderUInt16MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderUInt16MaxValueCastToUInt64IsFoldedCorrectly()
        {
            double doubleOneDecrementUnderUInt16MaxValue = 65534.99999999999d;

            if (BreakUpFlow())
                return;

            if (checked((ulong)doubleOneDecrementUnderUInt16MaxValue) != 65534)
            {
                Console.WriteLine($"'(ulong)65534.99999999999d' was evaluated to '{(ulong)doubleOneDecrementUnderUInt16MaxValue}'. Expected: '65534'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderUInt16MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderUInt16MaxValueCastToUInt64IsFoldedCorrectly()
        {
            double doubleOneFullDecrementUnderUInt16MaxValue = 65534d;

            if (BreakUpFlow())
                return;

            if (checked((ulong)doubleOneFullDecrementUnderUInt16MaxValue) != 65534)
            {
                Console.WriteLine($"'(ulong)65534d' was evaluated to '{(ulong)doubleOneFullDecrementUnderUInt16MaxValue}'. Expected: '65534'.");
                _counter++;
            }
        }
        ConfirmDoubleOneIncrementAboveUInt16MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveUInt16MaxValueCastToUInt64IsFoldedCorrectly()
        {
            double doubleOneIncrementAboveUInt16MaxValue = 65535.00000000001d;

            if (BreakUpFlow())
                return;

            if (checked((ulong)doubleOneIncrementAboveUInt16MaxValue) != 65535)
            {
                Console.WriteLine($"'(ulong)65535.00000000001d' was evaluated to '{(ulong)doubleOneIncrementAboveUInt16MaxValue}'. Expected: '65535'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveUInt16MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveUInt16MaxValueCastToUInt64IsFoldedCorrectly()
        {
            double doubleOneFullIncrementAboveUInt16MaxValue = 65536d;

            if (BreakUpFlow())
                return;

            if (checked((ulong)doubleOneFullIncrementAboveUInt16MaxValue) != 65536)
            {
                Console.WriteLine($"'(ulong)65536d' was evaluated to '{(ulong)doubleOneFullIncrementAboveUInt16MaxValue}'. Expected: '65536'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderUInt16MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt16MaxValueCastToUInt64IsFoldedCorrectly()
        {
            double integerOneDecrementUnderUInt16MaxValue = 65534.0;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneDecrementUnderUInt16MaxValue) != 65534)
            {
                Console.WriteLine($"'(ulong)65534.0' was evaluated to '{(ulong)integerOneDecrementUnderUInt16MaxValue}'. Expected: '65534'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToUInt64IsFoldedCorrectly()
        {
            double integerOneIncrementAboveUInt16MaxValue = 65536.0;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneIncrementAboveUInt16MaxValue) != 65536)
            {
                Console.WriteLine($"'(ulong)65536.0' was evaluated to '{(ulong)integerOneIncrementAboveUInt16MaxValue}'. Expected: '65536'.");
                _counter++;
            }
        }
        ConfirmInt32MinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MinValueCastToUInt64Overflows()
        {
            double from = -2147483648.0;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-2147483648.0)' did not throw OverflowException.");
        }
        ConfirmInt32MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MaxValueCastToUInt64IsFoldedCorrectly()
        {
            double int32MaxValue = 2147483647.0;

            if (BreakUpFlow())
                return;

            if (checked((ulong)int32MaxValue) != 2147483647)
            {
                Console.WriteLine($"'(ulong)2147483647.0' was evaluated to '{(ulong)int32MaxValue}'. Expected: '2147483647'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderInt32MinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderInt32MinValueCastToUInt64Overflows()
        {
            double from = -2147483648.0000005d;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-2147483648.0000005d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullDecrementUnderInt32MinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderInt32MinValueCastToUInt64Overflows()
        {
            double from = -2147483649d;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-2147483649d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneIncrementAboveInt32MinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveInt32MinValueCastToUInt64Overflows()
        {
            double from = -2147483647.9999998d;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-2147483647.9999998d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneFullIncrementAboveInt32MinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveInt32MinValueCastToUInt64Overflows()
        {
            double from = -2147483647d;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-2147483647d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneDecrementUnderInt32MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderInt32MaxValueCastToUInt64IsFoldedCorrectly()
        {
            double doubleOneDecrementUnderInt32MaxValue = 2147483646.9999998d;

            if (BreakUpFlow())
                return;

            if (checked((ulong)doubleOneDecrementUnderInt32MaxValue) != 2147483646)
            {
                Console.WriteLine($"'(ulong)2147483646.9999998d' was evaluated to '{(ulong)doubleOneDecrementUnderInt32MaxValue}'. Expected: '2147483646'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderInt32MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderInt32MaxValueCastToUInt64IsFoldedCorrectly()
        {
            double doubleOneFullDecrementUnderInt32MaxValue = 2147483646d;

            if (BreakUpFlow())
                return;

            if (checked((ulong)doubleOneFullDecrementUnderInt32MaxValue) != 2147483646)
            {
                Console.WriteLine($"'(ulong)2147483646d' was evaluated to '{(ulong)doubleOneFullDecrementUnderInt32MaxValue}'. Expected: '2147483646'.");
                _counter++;
            }
        }
        ConfirmDoubleOneIncrementAboveInt32MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveInt32MaxValueCastToUInt64IsFoldedCorrectly()
        {
            double doubleOneIncrementAboveInt32MaxValue = 2147483647.0000002d;

            if (BreakUpFlow())
                return;

            if (checked((ulong)doubleOneIncrementAboveInt32MaxValue) != 2147483647)
            {
                Console.WriteLine($"'(ulong)2147483647.0000002d' was evaluated to '{(ulong)doubleOneIncrementAboveInt32MaxValue}'. Expected: '2147483647'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveInt32MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveInt32MaxValueCastToUInt64IsFoldedCorrectly()
        {
            double doubleOneFullIncrementAboveInt32MaxValue = 2147483648d;

            if (BreakUpFlow())
                return;

            if (checked((ulong)doubleOneFullIncrementAboveInt32MaxValue) != 2147483648)
            {
                Console.WriteLine($"'(ulong)2147483648d' was evaluated to '{(ulong)doubleOneFullIncrementAboveInt32MaxValue}'. Expected: '2147483648'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt32MinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt32MinValueCastToUInt64Overflows()
        {
            double from = -2147483649.0;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-2147483649.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt32MinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt32MinValueCastToUInt64Overflows()
        {
            double from = -2147483647.0;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-2147483647.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt32MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt32MaxValueCastToUInt64IsFoldedCorrectly()
        {
            double integerOneDecrementUnderInt32MaxValue = 2147483646.0;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneDecrementUnderInt32MaxValue) != 2147483646)
            {
                Console.WriteLine($"'(ulong)2147483646.0' was evaluated to '{(ulong)integerOneDecrementUnderInt32MaxValue}'. Expected: '2147483646'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt32MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt32MaxValueCastToUInt64IsFoldedCorrectly()
        {
            double integerOneIncrementAboveInt32MaxValue = 2147483648.0;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneIncrementAboveInt32MaxValue) != 2147483648)
            {
                Console.WriteLine($"'(ulong)2147483648.0' was evaluated to '{(ulong)integerOneIncrementAboveInt32MaxValue}'. Expected: '2147483648'.");
                _counter++;
            }
        }
        ConfirmUInt32MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32MaxValueCastToUInt64IsFoldedCorrectly()
        {
            double uInt32MaxValue = 4294967295.0;

            if (BreakUpFlow())
                return;

            if (checked((ulong)uInt32MaxValue) != 4294967295)
            {
                Console.WriteLine($"'(ulong)4294967295.0' was evaluated to '{(ulong)uInt32MaxValue}'. Expected: '4294967295'.");
                _counter++;
            }
        }
        ConfirmDoubleOneDecrementUnderUInt32MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderUInt32MaxValueCastToUInt64IsFoldedCorrectly()
        {
            double doubleOneDecrementUnderUInt32MaxValue = 4294967294.9999995d;

            if (BreakUpFlow())
                return;

            if (checked((ulong)doubleOneDecrementUnderUInt32MaxValue) != 4294967294)
            {
                Console.WriteLine($"'(ulong)4294967294.9999995d' was evaluated to '{(ulong)doubleOneDecrementUnderUInt32MaxValue}'. Expected: '4294967294'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullDecrementUnderUInt32MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullDecrementUnderUInt32MaxValueCastToUInt64IsFoldedCorrectly()
        {
            double doubleOneFullDecrementUnderUInt32MaxValue = 4294967294d;

            if (BreakUpFlow())
                return;

            if (checked((ulong)doubleOneFullDecrementUnderUInt32MaxValue) != 4294967294)
            {
                Console.WriteLine($"'(ulong)4294967294d' was evaluated to '{(ulong)doubleOneFullDecrementUnderUInt32MaxValue}'. Expected: '4294967294'.");
                _counter++;
            }
        }
        ConfirmDoubleOneIncrementAboveUInt32MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveUInt32MaxValueCastToUInt64IsFoldedCorrectly()
        {
            double doubleOneIncrementAboveUInt32MaxValue = 4294967295.0000005d;

            if (BreakUpFlow())
                return;

            if (checked((ulong)doubleOneIncrementAboveUInt32MaxValue) != 4294967295)
            {
                Console.WriteLine($"'(ulong)4294967295.0000005d' was evaluated to '{(ulong)doubleOneIncrementAboveUInt32MaxValue}'. Expected: '4294967295'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveUInt32MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveUInt32MaxValueCastToUInt64IsFoldedCorrectly()
        {
            double doubleOneFullIncrementAboveUInt32MaxValue = 4294967296d;

            if (BreakUpFlow())
                return;

            if (checked((ulong)doubleOneFullIncrementAboveUInt32MaxValue) != 4294967296)
            {
                Console.WriteLine($"'(ulong)4294967296d' was evaluated to '{(ulong)doubleOneFullIncrementAboveUInt32MaxValue}'. Expected: '4294967296'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderUInt32MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt32MaxValueCastToUInt64IsFoldedCorrectly()
        {
            double integerOneDecrementUnderUInt32MaxValue = 4294967294.0;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneDecrementUnderUInt32MaxValue) != 4294967294)
            {
                Console.WriteLine($"'(ulong)4294967294.0' was evaluated to '{(ulong)integerOneDecrementUnderUInt32MaxValue}'. Expected: '4294967294'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveUInt32MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt32MaxValueCastToUInt64IsFoldedCorrectly()
        {
            double integerOneIncrementAboveUInt32MaxValue = 4294967296.0;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneIncrementAboveUInt32MaxValue) != 4294967296)
            {
                Console.WriteLine($"'(ulong)4294967296.0' was evaluated to '{(ulong)integerOneIncrementAboveUInt32MaxValue}'. Expected: '4294967296'.");
                _counter++;
            }
        }
        ConfirmInt64MinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MinValueCastToUInt64Overflows()
        {
            double from = -9223372036854775808.0;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-9223372036854775808.0)' did not throw OverflowException.");
        }
        ConfirmInt64MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MaxValueCastToUInt64IsFoldedCorrectly()
        {
            double int64MaxValue = 9223372036854775807.0;

            if (BreakUpFlow())
                return;

            if (checked((ulong)int64MaxValue) != 9223372036854775808)
            {
                Console.WriteLine($"'(ulong)9223372036854775807.0' was evaluated to '{(ulong)int64MaxValue}'. Expected: '9223372036854775808'.");
                _counter++;
            }
        }
        ConfirmDoubleOneIncrementAboveInt64MinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveInt64MinValueCastToUInt64Overflows()
        {
            double from = -9.223372036854775E+18d;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-9.223372036854775E+18d)' did not throw OverflowException.");
        }
        ConfirmDoubleOneDecrementUnderInt64MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderInt64MaxValueCastToUInt64IsFoldedCorrectly()
        {
            double doubleOneDecrementUnderInt64MaxValue = 9.223372036854775E+18d;

            if (BreakUpFlow())
                return;

            if (checked((ulong)doubleOneDecrementUnderInt64MaxValue) != 9223372036854774784)
            {
                Console.WriteLine($"'(ulong)9.223372036854775E+18d' was evaluated to '{(ulong)doubleOneDecrementUnderInt64MaxValue}'. Expected: '9223372036854774784'.");
                _counter++;
            }
        }
        ConfirmDoubleOneIncrementAboveInt64MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneIncrementAboveInt64MaxValueCastToUInt64IsFoldedCorrectly()
        {
            double doubleOneIncrementAboveInt64MaxValue = 9.223372036854778E+18d;

            if (BreakUpFlow())
                return;

            if (checked((ulong)doubleOneIncrementAboveInt64MaxValue) != 9223372036854777856)
            {
                Console.WriteLine($"'(ulong)9.223372036854778E+18d' was evaluated to '{(ulong)doubleOneIncrementAboveInt64MaxValue}'. Expected: '9223372036854777856'.");
                _counter++;
            }
        }
        ConfirmDoubleOneFullIncrementAboveInt64MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneFullIncrementAboveInt64MaxValueCastToUInt64IsFoldedCorrectly()
        {
            double doubleOneFullIncrementAboveInt64MaxValue = 9.223372036854776E+18d;

            if (BreakUpFlow())
                return;

            if (checked((ulong)doubleOneFullIncrementAboveInt64MaxValue) != 9223372036854775808)
            {
                Console.WriteLine($"'(ulong)9.223372036854776E+18d' was evaluated to '{(ulong)doubleOneFullIncrementAboveInt64MaxValue}'. Expected: '9223372036854775808'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt64MinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt64MinValueCastToUInt64Overflows()
        {
            double from = -9223372036854775807.0;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-9223372036854775807.0)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt64MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt64MaxValueCastToUInt64IsFoldedCorrectly()
        {
            double integerOneDecrementUnderInt64MaxValue = 9223372036854775806.0;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneDecrementUnderInt64MaxValue) != 9223372036854775808)
            {
                Console.WriteLine($"'(ulong)9223372036854775806.0' was evaluated to '{(ulong)integerOneDecrementUnderInt64MaxValue}'. Expected: '9223372036854775808'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt64MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt64MaxValueCastToUInt64IsFoldedCorrectly()
        {
            double integerOneIncrementAboveInt64MaxValue = 9223372036854775808.0;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneIncrementAboveInt64MaxValue) != 9223372036854775808)
            {
                Console.WriteLine($"'(ulong)9223372036854775808.0' was evaluated to '{(ulong)integerOneIncrementAboveInt64MaxValue}'. Expected: '9223372036854775808'.");
                _counter++;
            }
        }
        ConfirmUInt64MaxValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64MaxValueCastToUInt64Overflows()
        {
            double from = 18446744073709551615.0;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)18446744073709551615.0)' did not throw OverflowException.");
        }
        ConfirmDoubleOneDecrementUnderUInt64MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmDoubleOneDecrementUnderUInt64MaxValueCastToUInt64IsFoldedCorrectly()
        {
            double doubleOneDecrementUnderUInt64MaxValue = 1.844674407370955E+19d;

            if (BreakUpFlow())
                return;

            if (checked((ulong)doubleOneDecrementUnderUInt64MaxValue) != 18446744073709549568)
            {
                Console.WriteLine($"'(ulong)1.844674407370955E+19d' was evaluated to '{(ulong)doubleOneDecrementUnderUInt64MaxValue}'. Expected: '18446744073709549568'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderUInt64MaxValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt64MaxValueCastToUInt64Overflows()
        {
            double from = 18446744073709551614.0;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)18446744073709551614.0)' did not throw OverflowException.");
        }
    }
}
