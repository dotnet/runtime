// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

public partial class ValueNumberingCheckedCastsOfConstants
{
    private static void TestCastingSingleToSByte()
    {
        ConfirmIntegerZeroCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToSByteIsFoldedCorrectly()
        {
            float integerZero = 0.0f;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)integerZero) != 0)
            {
                Console.WriteLine($"'(sbyte)0.0f' was evaluated to '{(sbyte)integerZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmFloatMinusZeroCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmFloatMinusZeroCastToSByteIsFoldedCorrectly()
        {
            float floatMinusZero = -0f;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)floatMinusZero) != 0)
            {
                Console.WriteLine($"'(sbyte)-0f' was evaluated to '{(sbyte)floatMinusZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmFloatZeroCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmFloatZeroCastToSByteIsFoldedCorrectly()
        {
            float floatZero = 0f;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)floatZero) != 0)
            {
                Console.WriteLine($"'(sbyte)0f' was evaluated to '{(sbyte)floatZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmFloatHalfOfMaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmFloatHalfOfMaxValueCastToSByteOverflows()
        {
            float from = 1.7014117E+38f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)1.7014117E+38f)' did not throw OverflowException.");
        }
        ConfirmFloatHalfOfMinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmFloatHalfOfMinValueCastToSByteOverflows()
        {
            float from = -1.7014117E+38f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-1.7014117E+38f)' did not throw OverflowException.");
        }
        ConfirmSingleMinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleMinValueCastToSByteOverflows()
        {
            float from = -3.4028235E+38f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-3.4028235E+38f)' did not throw OverflowException.");
        }
        ConfirmSingleMaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleMaxValueCastToSByteOverflows()
        {
            float from = 3.4028235E+38f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)3.4028235E+38f)' did not throw OverflowException.");
        }
        ConfirmSByteMinValueCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMinValueCastToSByteIsFoldedCorrectly()
        {
            float sByteMinValue = -128.0f;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)sByteMinValue) != -128)
            {
                Console.WriteLine($"'(sbyte)-128.0f' was evaluated to '{(sbyte)sByteMinValue}'. Expected: '-128'.");
                _counter++;
            }
        }
        ConfirmSByteMaxValueCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToSByteIsFoldedCorrectly()
        {
            float sByteMaxValue = 127.0f;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(sbyte)127.0f' was evaluated to '{(sbyte)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmSingleOneDecrementUnderSByteMinValueCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderSByteMinValueCastToSByteIsFoldedCorrectly()
        {
            float singleOneDecrementUnderSByteMinValue = -128.00002f;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)singleOneDecrementUnderSByteMinValue) != -128)
            {
                Console.WriteLine($"'(sbyte)-128.00002f' was evaluated to '{(sbyte)singleOneDecrementUnderSByteMinValue}'. Expected: '-128'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullDecrementUnderSByteMinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderSByteMinValueCastToSByteOverflows()
        {
            float from = -129f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-129f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveSByteMinValueCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveSByteMinValueCastToSByteIsFoldedCorrectly()
        {
            float singleOneIncrementAboveSByteMinValue = -127.99999f;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)singleOneIncrementAboveSByteMinValue) != -127)
            {
                Console.WriteLine($"'(sbyte)-127.99999f' was evaluated to '{(sbyte)singleOneIncrementAboveSByteMinValue}'. Expected: '-127'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullIncrementAboveSByteMinValueCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveSByteMinValueCastToSByteIsFoldedCorrectly()
        {
            float singleOneFullIncrementAboveSByteMinValue = -127f;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)singleOneFullIncrementAboveSByteMinValue) != -127)
            {
                Console.WriteLine($"'(sbyte)-127f' was evaluated to '{(sbyte)singleOneFullIncrementAboveSByteMinValue}'. Expected: '-127'.");
                _counter++;
            }
        }
        ConfirmSingleOneDecrementUnderSByteMaxValueCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderSByteMaxValueCastToSByteIsFoldedCorrectly()
        {
            float singleOneDecrementUnderSByteMaxValue = 126.99999f;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)singleOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(sbyte)126.99999f' was evaluated to '{(sbyte)singleOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullDecrementUnderSByteMaxValueCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderSByteMaxValueCastToSByteIsFoldedCorrectly()
        {
            float singleOneFullDecrementUnderSByteMaxValue = 126f;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)singleOneFullDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(sbyte)126f' was evaluated to '{(sbyte)singleOneFullDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmSingleOneIncrementAboveSByteMaxValueCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveSByteMaxValueCastToSByteIsFoldedCorrectly()
        {
            float singleOneIncrementAboveSByteMaxValue = 127.00001f;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)singleOneIncrementAboveSByteMaxValue) != 127)
            {
                Console.WriteLine($"'(sbyte)127.00001f' was evaluated to '{(sbyte)singleOneIncrementAboveSByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullIncrementAboveSByteMaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveSByteMaxValueCastToSByteOverflows()
        {
            float from = 128f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)128f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderSByteMinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMinValueCastToSByteOverflows()
        {
            float from = -129.0f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-129.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveSByteMinValueCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMinValueCastToSByteIsFoldedCorrectly()
        {
            float integerOneIncrementAboveSByteMinValue = -127.0f;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)integerOneIncrementAboveSByteMinValue) != -127)
            {
                Console.WriteLine($"'(sbyte)-127.0f' was evaluated to '{(sbyte)integerOneIncrementAboveSByteMinValue}'. Expected: '-127'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToSByteIsFoldedCorrectly()
        {
            float integerOneDecrementUnderSByteMaxValue = 126.0f;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(sbyte)126.0f' was evaluated to '{(sbyte)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMaxValueCastToSByteOverflows()
        {
            float from = 128.0f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)128.0f)' did not throw OverflowException.");
        }
        ConfirmByteMaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmByteMaxValueCastToSByteOverflows()
        {
            float from = 255.0f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)255.0f)' did not throw OverflowException.");
        }
        ConfirmSingleOneDecrementUnderByteMinValueCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderByteMinValueCastToSByteIsFoldedCorrectly()
        {
            float singleOneDecrementUnderByteMinValue = -1E-45f;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)singleOneDecrementUnderByteMinValue) != 0)
            {
                Console.WriteLine($"'(sbyte)-1E-45f' was evaluated to '{(sbyte)singleOneDecrementUnderByteMinValue}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullDecrementUnderByteMinValueCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderByteMinValueCastToSByteIsFoldedCorrectly()
        {
            float singleOneFullDecrementUnderByteMinValue = -1f;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)singleOneFullDecrementUnderByteMinValue) != -1)
            {
                Console.WriteLine($"'(sbyte)-1f' was evaluated to '{(sbyte)singleOneFullDecrementUnderByteMinValue}'. Expected: '-1'.");
                _counter++;
            }
        }
        ConfirmSingleOneIncrementAboveByteMinValueCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveByteMinValueCastToSByteIsFoldedCorrectly()
        {
            float singleOneIncrementAboveByteMinValue = 1E-45f;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)singleOneIncrementAboveByteMinValue) != 0)
            {
                Console.WriteLine($"'(sbyte)1E-45f' was evaluated to '{(sbyte)singleOneIncrementAboveByteMinValue}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullIncrementAboveByteMinValueCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveByteMinValueCastToSByteIsFoldedCorrectly()
        {
            float singleOneFullIncrementAboveByteMinValue = 1f;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)singleOneFullIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(sbyte)1f' was evaluated to '{(sbyte)singleOneFullIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmSingleOneDecrementUnderByteMaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderByteMaxValueCastToSByteOverflows()
        {
            float from = 254.99998f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)254.99998f)' did not throw OverflowException.");
        }
        ConfirmSingleOneFullDecrementUnderByteMaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderByteMaxValueCastToSByteOverflows()
        {
            float from = 254f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)254f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveByteMaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveByteMaxValueCastToSByteOverflows()
        {
            float from = 255.00002f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)255.00002f)' did not throw OverflowException.");
        }
        ConfirmSingleOneFullIncrementAboveByteMaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveByteMaxValueCastToSByteOverflows()
        {
            float from = 256f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)256f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderByteMinValueCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMinValueCastToSByteIsFoldedCorrectly()
        {
            float integerOneDecrementUnderByteMinValue = -1.0f;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)integerOneDecrementUnderByteMinValue) != -1)
            {
                Console.WriteLine($"'(sbyte)-1.0f' was evaluated to '{(sbyte)integerOneDecrementUnderByteMinValue}'. Expected: '-1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToSByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToSByteIsFoldedCorrectly()
        {
            float integerOneIncrementAboveByteMinValue = 1.0f;

            if (BreakUpFlow())
                return;

            if (checked((sbyte)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(sbyte)1.0f' was evaluated to '{(sbyte)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMaxValueCastToSByteOverflows()
        {
            float from = 254.0f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)254.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveByteMaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMaxValueCastToSByteOverflows()
        {
            float from = 256.0f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)256.0f)' did not throw OverflowException.");
        }
        ConfirmInt16MinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MinValueCastToSByteOverflows()
        {
            float from = -32768.0f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-32768.0f)' did not throw OverflowException.");
        }
        ConfirmInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MaxValueCastToSByteOverflows()
        {
            float from = 32767.0f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)32767.0f)' did not throw OverflowException.");
        }
        ConfirmSingleOneDecrementUnderInt16MinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderInt16MinValueCastToSByteOverflows()
        {
            float from = -32768.004f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-32768.004f)' did not throw OverflowException.");
        }
        ConfirmSingleOneFullDecrementUnderInt16MinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderInt16MinValueCastToSByteOverflows()
        {
            float from = -32769f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-32769f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveInt16MinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveInt16MinValueCastToSByteOverflows()
        {
            float from = -32767.998f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-32767.998f)' did not throw OverflowException.");
        }
        ConfirmSingleOneFullIncrementAboveInt16MinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveInt16MinValueCastToSByteOverflows()
        {
            float from = -32767f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-32767f)' did not throw OverflowException.");
        }
        ConfirmSingleOneDecrementUnderInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderInt16MaxValueCastToSByteOverflows()
        {
            float from = 32766.998f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)32766.998f)' did not throw OverflowException.");
        }
        ConfirmSingleOneFullDecrementUnderInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderInt16MaxValueCastToSByteOverflows()
        {
            float from = 32766f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)32766f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveInt16MaxValueCastToSByteOverflows()
        {
            float from = 32767.002f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)32767.002f)' did not throw OverflowException.");
        }
        ConfirmSingleOneFullIncrementAboveInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveInt16MaxValueCastToSByteOverflows()
        {
            float from = 32768f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)32768f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt16MinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MinValueCastToSByteOverflows()
        {
            float from = -32769.0f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-32769.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt16MinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt16MinValueCastToSByteOverflows()
        {
            float from = -32767.0f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-32767.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MaxValueCastToSByteOverflows()
        {
            float from = 32766.0f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)32766.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt16MaxValueCastToSByteOverflows()
        {
            float from = 32768.0f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)32768.0f)' did not throw OverflowException.");
        }
        ConfirmUInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt16MaxValueCastToSByteOverflows()
        {
            float from = 65535.0f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)65535.0f)' did not throw OverflowException.");
        }
        ConfirmSingleOneDecrementUnderUInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderUInt16MaxValueCastToSByteOverflows()
        {
            float from = 65534.996f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)65534.996f)' did not throw OverflowException.");
        }
        ConfirmSingleOneFullDecrementUnderUInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderUInt16MaxValueCastToSByteOverflows()
        {
            float from = 65534f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)65534f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveUInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveUInt16MaxValueCastToSByteOverflows()
        {
            float from = 65535.004f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)65535.004f)' did not throw OverflowException.");
        }
        ConfirmSingleOneFullIncrementAboveUInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveUInt16MaxValueCastToSByteOverflows()
        {
            float from = 65536f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)65536f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderUInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt16MaxValueCastToSByteOverflows()
        {
            float from = 65534.0f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)65534.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToSByteOverflows()
        {
            float from = 65536.0f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)65536.0f)' did not throw OverflowException.");
        }
        ConfirmInt32MinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MinValueCastToSByteOverflows()
        {
            float from = -2147483648.0f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-2147483648.0f)' did not throw OverflowException.");
        }
        ConfirmInt32MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MaxValueCastToSByteOverflows()
        {
            float from = 2147483647.0f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)2147483647.0f)' did not throw OverflowException.");
        }
        ConfirmSingleOneDecrementUnderInt32MinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderInt32MinValueCastToSByteOverflows()
        {
            float from = -2.147484E+09f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-2.147484E+09f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveInt32MinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveInt32MinValueCastToSByteOverflows()
        {
            float from = -2.1474835E+09f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-2.1474835E+09f)' did not throw OverflowException.");
        }
        ConfirmSingleOneDecrementUnderInt32MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderInt32MaxValueCastToSByteOverflows()
        {
            float from = 2.1474835E+09f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)2.1474835E+09f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveInt32MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveInt32MaxValueCastToSByteOverflows()
        {
            float from = 2.147484E+09f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)2.147484E+09f)' did not throw OverflowException.");
        }
        ConfirmSingleOneFullIncrementAboveInt32MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveInt32MaxValueCastToSByteOverflows()
        {
            float from = 2.1474836E+09f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)2.1474836E+09f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt32MinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt32MinValueCastToSByteOverflows()
        {
            float from = -2147483649.0f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-2147483649.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt32MinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt32MinValueCastToSByteOverflows()
        {
            float from = -2147483647.0f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-2147483647.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt32MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt32MaxValueCastToSByteOverflows()
        {
            float from = 2147483646.0f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)2147483646.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt32MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt32MaxValueCastToSByteOverflows()
        {
            float from = 2147483648.0f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)2147483648.0f)' did not throw OverflowException.");
        }
        ConfirmUInt32MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32MaxValueCastToSByteOverflows()
        {
            float from = 4294967295.0f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)4294967295.0f)' did not throw OverflowException.");
        }
        ConfirmSingleOneDecrementUnderUInt32MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderUInt32MaxValueCastToSByteOverflows()
        {
            float from = 4.294967E+09f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)4.294967E+09f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveUInt32MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveUInt32MaxValueCastToSByteOverflows()
        {
            float from = 4.294968E+09f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)4.294968E+09f)' did not throw OverflowException.");
        }
        ConfirmSingleOneFullIncrementAboveUInt32MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveUInt32MaxValueCastToSByteOverflows()
        {
            float from = 4.2949673E+09f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)4.2949673E+09f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderUInt32MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt32MaxValueCastToSByteOverflows()
        {
            float from = 4294967294.0f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)4294967294.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveUInt32MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt32MaxValueCastToSByteOverflows()
        {
            float from = 4294967296.0f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)4294967296.0f)' did not throw OverflowException.");
        }
        ConfirmInt64MinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MinValueCastToSByteOverflows()
        {
            float from = -9223372036854775808.0f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-9223372036854775808.0f)' did not throw OverflowException.");
        }
        ConfirmInt64MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MaxValueCastToSByteOverflows()
        {
            float from = 9223372036854775807.0f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)9223372036854775807.0f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveInt64MinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveInt64MinValueCastToSByteOverflows()
        {
            float from = -9.2233715E+18f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-9.2233715E+18f)' did not throw OverflowException.");
        }
        ConfirmSingleOneDecrementUnderInt64MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderInt64MaxValueCastToSByteOverflows()
        {
            float from = 9.2233715E+18f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)9.2233715E+18f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveInt64MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveInt64MaxValueCastToSByteOverflows()
        {
            float from = 9.223373E+18f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)9.223373E+18f)' did not throw OverflowException.");
        }
        ConfirmSingleOneFullIncrementAboveInt64MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveInt64MaxValueCastToSByteOverflows()
        {
            float from = 9.223372E+18f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)9.223372E+18f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt64MinValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt64MinValueCastToSByteOverflows()
        {
            float from = -9223372036854775807.0f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)-9223372036854775807.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt64MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt64MaxValueCastToSByteOverflows()
        {
            float from = 9223372036854775806.0f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)9223372036854775806.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt64MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt64MaxValueCastToSByteOverflows()
        {
            float from = 9223372036854775808.0f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)9223372036854775808.0f)' did not throw OverflowException.");
        }
        ConfirmUInt64MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64MaxValueCastToSByteOverflows()
        {
            float from = 18446744073709551615.0f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)18446744073709551615.0f)' did not throw OverflowException.");
        }
        ConfirmSingleOneDecrementUnderUInt64MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderUInt64MaxValueCastToSByteOverflows()
        {
            float from = 1.8446743E+19f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)1.8446743E+19f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderUInt64MaxValueCastToSByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt64MaxValueCastToSByteOverflows()
        {
            float from = 18446744073709551614.0f;
            _counter++;
            try
            {
                _ = checked((sbyte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((sbyte)18446744073709551614.0f)' did not throw OverflowException.");
        }
    }

    private static void TestCastingSingleToByte()
    {
        ConfirmIntegerZeroCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToByteIsFoldedCorrectly()
        {
            float integerZero = 0.0f;

            if (BreakUpFlow())
                return;

            if (checked((byte)integerZero) != 0)
            {
                Console.WriteLine($"'(byte)0.0f' was evaluated to '{(byte)integerZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmFloatMinusZeroCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmFloatMinusZeroCastToByteIsFoldedCorrectly()
        {
            float floatMinusZero = -0f;

            if (BreakUpFlow())
                return;

            if (checked((byte)floatMinusZero) != 0)
            {
                Console.WriteLine($"'(byte)-0f' was evaluated to '{(byte)floatMinusZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmFloatZeroCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmFloatZeroCastToByteIsFoldedCorrectly()
        {
            float floatZero = 0f;

            if (BreakUpFlow())
                return;

            if (checked((byte)floatZero) != 0)
            {
                Console.WriteLine($"'(byte)0f' was evaluated to '{(byte)floatZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmFloatHalfOfMaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmFloatHalfOfMaxValueCastToByteOverflows()
        {
            float from = 1.7014117E+38f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)1.7014117E+38f)' did not throw OverflowException.");
        }
        ConfirmFloatHalfOfMinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmFloatHalfOfMinValueCastToByteOverflows()
        {
            float from = -1.7014117E+38f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-1.7014117E+38f)' did not throw OverflowException.");
        }
        ConfirmSingleMinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleMinValueCastToByteOverflows()
        {
            float from = -3.4028235E+38f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-3.4028235E+38f)' did not throw OverflowException.");
        }
        ConfirmSingleMaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleMaxValueCastToByteOverflows()
        {
            float from = 3.4028235E+38f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)3.4028235E+38f)' did not throw OverflowException.");
        }
        ConfirmSByteMinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMinValueCastToByteOverflows()
        {
            float from = -128.0f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-128.0f)' did not throw OverflowException.");
        }
        ConfirmSByteMaxValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToByteIsFoldedCorrectly()
        {
            float sByteMaxValue = 127.0f;

            if (BreakUpFlow())
                return;

            if (checked((byte)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(byte)127.0f' was evaluated to '{(byte)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmSingleOneDecrementUnderSByteMinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderSByteMinValueCastToByteOverflows()
        {
            float from = -128.00002f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-128.00002f)' did not throw OverflowException.");
        }
        ConfirmSingleOneFullDecrementUnderSByteMinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderSByteMinValueCastToByteOverflows()
        {
            float from = -129f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-129f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveSByteMinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveSByteMinValueCastToByteOverflows()
        {
            float from = -127.99999f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-127.99999f)' did not throw OverflowException.");
        }
        ConfirmSingleOneFullIncrementAboveSByteMinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveSByteMinValueCastToByteOverflows()
        {
            float from = -127f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-127f)' did not throw OverflowException.");
        }
        ConfirmSingleOneDecrementUnderSByteMaxValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderSByteMaxValueCastToByteIsFoldedCorrectly()
        {
            float singleOneDecrementUnderSByteMaxValue = 126.99999f;

            if (BreakUpFlow())
                return;

            if (checked((byte)singleOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(byte)126.99999f' was evaluated to '{(byte)singleOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullDecrementUnderSByteMaxValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderSByteMaxValueCastToByteIsFoldedCorrectly()
        {
            float singleOneFullDecrementUnderSByteMaxValue = 126f;

            if (BreakUpFlow())
                return;

            if (checked((byte)singleOneFullDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(byte)126f' was evaluated to '{(byte)singleOneFullDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmSingleOneIncrementAboveSByteMaxValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveSByteMaxValueCastToByteIsFoldedCorrectly()
        {
            float singleOneIncrementAboveSByteMaxValue = 127.00001f;

            if (BreakUpFlow())
                return;

            if (checked((byte)singleOneIncrementAboveSByteMaxValue) != 127)
            {
                Console.WriteLine($"'(byte)127.00001f' was evaluated to '{(byte)singleOneIncrementAboveSByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullIncrementAboveSByteMaxValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveSByteMaxValueCastToByteIsFoldedCorrectly()
        {
            float singleOneFullIncrementAboveSByteMaxValue = 128f;

            if (BreakUpFlow())
                return;

            if (checked((byte)singleOneFullIncrementAboveSByteMaxValue) != 128)
            {
                Console.WriteLine($"'(byte)128f' was evaluated to '{(byte)singleOneFullIncrementAboveSByteMaxValue}'. Expected: '128'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMinValueCastToByteOverflows()
        {
            float from = -129.0f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-129.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveSByteMinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMinValueCastToByteOverflows()
        {
            float from = -127.0f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-127.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToByteIsFoldedCorrectly()
        {
            float integerOneDecrementUnderSByteMaxValue = 126.0f;

            if (BreakUpFlow())
                return;

            if (checked((byte)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(byte)126.0f' was evaluated to '{(byte)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMaxValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMaxValueCastToByteIsFoldedCorrectly()
        {
            float integerOneIncrementAboveSByteMaxValue = 128.0f;

            if (BreakUpFlow())
                return;

            if (checked((byte)integerOneIncrementAboveSByteMaxValue) != 128)
            {
                Console.WriteLine($"'(byte)128.0f' was evaluated to '{(byte)integerOneIncrementAboveSByteMaxValue}'. Expected: '128'.");
                _counter++;
            }
        }
        ConfirmByteMaxValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmByteMaxValueCastToByteIsFoldedCorrectly()
        {
            float byteMaxValue = 255.0f;

            if (BreakUpFlow())
                return;

            if (checked((byte)byteMaxValue) != 255)
            {
                Console.WriteLine($"'(byte)255.0f' was evaluated to '{(byte)byteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmSingleOneDecrementUnderByteMinValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderByteMinValueCastToByteIsFoldedCorrectly()
        {
            float singleOneDecrementUnderByteMinValue = -1E-45f;

            if (BreakUpFlow())
                return;

            if (checked((byte)singleOneDecrementUnderByteMinValue) != 0)
            {
                Console.WriteLine($"'(byte)-1E-45f' was evaluated to '{(byte)singleOneDecrementUnderByteMinValue}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullDecrementUnderByteMinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderByteMinValueCastToByteOverflows()
        {
            float from = -1f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-1f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveByteMinValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveByteMinValueCastToByteIsFoldedCorrectly()
        {
            float singleOneIncrementAboveByteMinValue = 1E-45f;

            if (BreakUpFlow())
                return;

            if (checked((byte)singleOneIncrementAboveByteMinValue) != 0)
            {
                Console.WriteLine($"'(byte)1E-45f' was evaluated to '{(byte)singleOneIncrementAboveByteMinValue}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullIncrementAboveByteMinValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveByteMinValueCastToByteIsFoldedCorrectly()
        {
            float singleOneFullIncrementAboveByteMinValue = 1f;

            if (BreakUpFlow())
                return;

            if (checked((byte)singleOneFullIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(byte)1f' was evaluated to '{(byte)singleOneFullIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmSingleOneDecrementUnderByteMaxValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderByteMaxValueCastToByteIsFoldedCorrectly()
        {
            float singleOneDecrementUnderByteMaxValue = 254.99998f;

            if (BreakUpFlow())
                return;

            if (checked((byte)singleOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(byte)254.99998f' was evaluated to '{(byte)singleOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullDecrementUnderByteMaxValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderByteMaxValueCastToByteIsFoldedCorrectly()
        {
            float singleOneFullDecrementUnderByteMaxValue = 254f;

            if (BreakUpFlow())
                return;

            if (checked((byte)singleOneFullDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(byte)254f' was evaluated to '{(byte)singleOneFullDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmSingleOneIncrementAboveByteMaxValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveByteMaxValueCastToByteIsFoldedCorrectly()
        {
            float singleOneIncrementAboveByteMaxValue = 255.00002f;

            if (BreakUpFlow())
                return;

            if (checked((byte)singleOneIncrementAboveByteMaxValue) != 255)
            {
                Console.WriteLine($"'(byte)255.00002f' was evaluated to '{(byte)singleOneIncrementAboveByteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullIncrementAboveByteMaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveByteMaxValueCastToByteOverflows()
        {
            float from = 256f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)256f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderByteMinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMinValueCastToByteOverflows()
        {
            float from = -1.0f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-1.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToByteIsFoldedCorrectly()
        {
            float integerOneIncrementAboveByteMinValue = 1.0f;

            if (BreakUpFlow())
                return;

            if (checked((byte)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(byte)1.0f' was evaluated to '{(byte)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMaxValueCastToByteIsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMaxValueCastToByteIsFoldedCorrectly()
        {
            float integerOneDecrementUnderByteMaxValue = 254.0f;

            if (BreakUpFlow())
                return;

            if (checked((byte)integerOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(byte)254.0f' was evaluated to '{(byte)integerOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMaxValueCastToByteOverflows()
        {
            float from = 256.0f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)256.0f)' did not throw OverflowException.");
        }
        ConfirmInt16MinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MinValueCastToByteOverflows()
        {
            float from = -32768.0f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-32768.0f)' did not throw OverflowException.");
        }
        ConfirmInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MaxValueCastToByteOverflows()
        {
            float from = 32767.0f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)32767.0f)' did not throw OverflowException.");
        }
        ConfirmSingleOneDecrementUnderInt16MinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderInt16MinValueCastToByteOverflows()
        {
            float from = -32768.004f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-32768.004f)' did not throw OverflowException.");
        }
        ConfirmSingleOneFullDecrementUnderInt16MinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderInt16MinValueCastToByteOverflows()
        {
            float from = -32769f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-32769f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveInt16MinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveInt16MinValueCastToByteOverflows()
        {
            float from = -32767.998f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-32767.998f)' did not throw OverflowException.");
        }
        ConfirmSingleOneFullIncrementAboveInt16MinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveInt16MinValueCastToByteOverflows()
        {
            float from = -32767f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-32767f)' did not throw OverflowException.");
        }
        ConfirmSingleOneDecrementUnderInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderInt16MaxValueCastToByteOverflows()
        {
            float from = 32766.998f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)32766.998f)' did not throw OverflowException.");
        }
        ConfirmSingleOneFullDecrementUnderInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderInt16MaxValueCastToByteOverflows()
        {
            float from = 32766f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)32766f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveInt16MaxValueCastToByteOverflows()
        {
            float from = 32767.002f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)32767.002f)' did not throw OverflowException.");
        }
        ConfirmSingleOneFullIncrementAboveInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveInt16MaxValueCastToByteOverflows()
        {
            float from = 32768f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)32768f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt16MinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MinValueCastToByteOverflows()
        {
            float from = -32769.0f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-32769.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt16MinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt16MinValueCastToByteOverflows()
        {
            float from = -32767.0f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-32767.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MaxValueCastToByteOverflows()
        {
            float from = 32766.0f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)32766.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt16MaxValueCastToByteOverflows()
        {
            float from = 32768.0f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)32768.0f)' did not throw OverflowException.");
        }
        ConfirmUInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt16MaxValueCastToByteOverflows()
        {
            float from = 65535.0f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)65535.0f)' did not throw OverflowException.");
        }
        ConfirmSingleOneDecrementUnderUInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderUInt16MaxValueCastToByteOverflows()
        {
            float from = 65534.996f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)65534.996f)' did not throw OverflowException.");
        }
        ConfirmSingleOneFullDecrementUnderUInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderUInt16MaxValueCastToByteOverflows()
        {
            float from = 65534f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)65534f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveUInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveUInt16MaxValueCastToByteOverflows()
        {
            float from = 65535.004f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)65535.004f)' did not throw OverflowException.");
        }
        ConfirmSingleOneFullIncrementAboveUInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveUInt16MaxValueCastToByteOverflows()
        {
            float from = 65536f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)65536f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderUInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt16MaxValueCastToByteOverflows()
        {
            float from = 65534.0f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)65534.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToByteOverflows()
        {
            float from = 65536.0f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)65536.0f)' did not throw OverflowException.");
        }
        ConfirmInt32MinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MinValueCastToByteOverflows()
        {
            float from = -2147483648.0f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-2147483648.0f)' did not throw OverflowException.");
        }
        ConfirmInt32MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MaxValueCastToByteOverflows()
        {
            float from = 2147483647.0f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)2147483647.0f)' did not throw OverflowException.");
        }
        ConfirmSingleOneDecrementUnderInt32MinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderInt32MinValueCastToByteOverflows()
        {
            float from = -2.147484E+09f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-2.147484E+09f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveInt32MinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveInt32MinValueCastToByteOverflows()
        {
            float from = -2.1474835E+09f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-2.1474835E+09f)' did not throw OverflowException.");
        }
        ConfirmSingleOneDecrementUnderInt32MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderInt32MaxValueCastToByteOverflows()
        {
            float from = 2.1474835E+09f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)2.1474835E+09f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveInt32MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveInt32MaxValueCastToByteOverflows()
        {
            float from = 2.147484E+09f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)2.147484E+09f)' did not throw OverflowException.");
        }
        ConfirmSingleOneFullIncrementAboveInt32MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveInt32MaxValueCastToByteOverflows()
        {
            float from = 2.1474836E+09f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)2.1474836E+09f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt32MinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt32MinValueCastToByteOverflows()
        {
            float from = -2147483649.0f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-2147483649.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt32MinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt32MinValueCastToByteOverflows()
        {
            float from = -2147483647.0f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-2147483647.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt32MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt32MaxValueCastToByteOverflows()
        {
            float from = 2147483646.0f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)2147483646.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt32MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt32MaxValueCastToByteOverflows()
        {
            float from = 2147483648.0f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)2147483648.0f)' did not throw OverflowException.");
        }
        ConfirmUInt32MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32MaxValueCastToByteOverflows()
        {
            float from = 4294967295.0f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)4294967295.0f)' did not throw OverflowException.");
        }
        ConfirmSingleOneDecrementUnderUInt32MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderUInt32MaxValueCastToByteOverflows()
        {
            float from = 4.294967E+09f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)4.294967E+09f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveUInt32MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveUInt32MaxValueCastToByteOverflows()
        {
            float from = 4.294968E+09f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)4.294968E+09f)' did not throw OverflowException.");
        }
        ConfirmSingleOneFullIncrementAboveUInt32MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveUInt32MaxValueCastToByteOverflows()
        {
            float from = 4.2949673E+09f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)4.2949673E+09f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderUInt32MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt32MaxValueCastToByteOverflows()
        {
            float from = 4294967294.0f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)4294967294.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveUInt32MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt32MaxValueCastToByteOverflows()
        {
            float from = 4294967296.0f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)4294967296.0f)' did not throw OverflowException.");
        }
        ConfirmInt64MinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MinValueCastToByteOverflows()
        {
            float from = -9223372036854775808.0f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-9223372036854775808.0f)' did not throw OverflowException.");
        }
        ConfirmInt64MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MaxValueCastToByteOverflows()
        {
            float from = 9223372036854775807.0f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)9223372036854775807.0f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveInt64MinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveInt64MinValueCastToByteOverflows()
        {
            float from = -9.2233715E+18f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-9.2233715E+18f)' did not throw OverflowException.");
        }
        ConfirmSingleOneDecrementUnderInt64MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderInt64MaxValueCastToByteOverflows()
        {
            float from = 9.2233715E+18f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)9.2233715E+18f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveInt64MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveInt64MaxValueCastToByteOverflows()
        {
            float from = 9.223373E+18f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)9.223373E+18f)' did not throw OverflowException.");
        }
        ConfirmSingleOneFullIncrementAboveInt64MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveInt64MaxValueCastToByteOverflows()
        {
            float from = 9.223372E+18f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)9.223372E+18f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt64MinValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt64MinValueCastToByteOverflows()
        {
            float from = -9223372036854775807.0f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)-9223372036854775807.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt64MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt64MaxValueCastToByteOverflows()
        {
            float from = 9223372036854775806.0f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)9223372036854775806.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt64MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt64MaxValueCastToByteOverflows()
        {
            float from = 9223372036854775808.0f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)9223372036854775808.0f)' did not throw OverflowException.");
        }
        ConfirmUInt64MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64MaxValueCastToByteOverflows()
        {
            float from = 18446744073709551615.0f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)18446744073709551615.0f)' did not throw OverflowException.");
        }
        ConfirmSingleOneDecrementUnderUInt64MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderUInt64MaxValueCastToByteOverflows()
        {
            float from = 1.8446743E+19f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)1.8446743E+19f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderUInt64MaxValueCastToByteOverflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt64MaxValueCastToByteOverflows()
        {
            float from = 18446744073709551614.0f;
            _counter++;
            try
            {
                _ = checked((byte)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((byte)18446744073709551614.0f)' did not throw OverflowException.");
        }
    }

    private static void TestCastingSingleToInt16()
    {
        ConfirmIntegerZeroCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToInt16IsFoldedCorrectly()
        {
            float integerZero = 0.0f;

            if (BreakUpFlow())
                return;

            if (checked((short)integerZero) != 0)
            {
                Console.WriteLine($"'(short)0.0f' was evaluated to '{(short)integerZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmFloatMinusZeroCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmFloatMinusZeroCastToInt16IsFoldedCorrectly()
        {
            float floatMinusZero = -0f;

            if (BreakUpFlow())
                return;

            if (checked((short)floatMinusZero) != 0)
            {
                Console.WriteLine($"'(short)-0f' was evaluated to '{(short)floatMinusZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmFloatZeroCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmFloatZeroCastToInt16IsFoldedCorrectly()
        {
            float floatZero = 0f;

            if (BreakUpFlow())
                return;

            if (checked((short)floatZero) != 0)
            {
                Console.WriteLine($"'(short)0f' was evaluated to '{(short)floatZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmFloatHalfOfMaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmFloatHalfOfMaxValueCastToInt16Overflows()
        {
            float from = 1.7014117E+38f;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)1.7014117E+38f)' did not throw OverflowException.");
        }
        ConfirmFloatHalfOfMinValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmFloatHalfOfMinValueCastToInt16Overflows()
        {
            float from = -1.7014117E+38f;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)-1.7014117E+38f)' did not throw OverflowException.");
        }
        ConfirmSingleMinValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleMinValueCastToInt16Overflows()
        {
            float from = -3.4028235E+38f;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)-3.4028235E+38f)' did not throw OverflowException.");
        }
        ConfirmSingleMaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleMaxValueCastToInt16Overflows()
        {
            float from = 3.4028235E+38f;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)3.4028235E+38f)' did not throw OverflowException.");
        }
        ConfirmSByteMinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMinValueCastToInt16IsFoldedCorrectly()
        {
            float sByteMinValue = -128.0f;

            if (BreakUpFlow())
                return;

            if (checked((short)sByteMinValue) != -128)
            {
                Console.WriteLine($"'(short)-128.0f' was evaluated to '{(short)sByteMinValue}'. Expected: '-128'.");
                _counter++;
            }
        }
        ConfirmSByteMaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToInt16IsFoldedCorrectly()
        {
            float sByteMaxValue = 127.0f;

            if (BreakUpFlow())
                return;

            if (checked((short)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(short)127.0f' was evaluated to '{(short)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmSingleOneDecrementUnderSByteMinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderSByteMinValueCastToInt16IsFoldedCorrectly()
        {
            float singleOneDecrementUnderSByteMinValue = -128.00002f;

            if (BreakUpFlow())
                return;

            if (checked((short)singleOneDecrementUnderSByteMinValue) != -128)
            {
                Console.WriteLine($"'(short)-128.00002f' was evaluated to '{(short)singleOneDecrementUnderSByteMinValue}'. Expected: '-128'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullDecrementUnderSByteMinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderSByteMinValueCastToInt16IsFoldedCorrectly()
        {
            float singleOneFullDecrementUnderSByteMinValue = -129f;

            if (BreakUpFlow())
                return;

            if (checked((short)singleOneFullDecrementUnderSByteMinValue) != -129)
            {
                Console.WriteLine($"'(short)-129f' was evaluated to '{(short)singleOneFullDecrementUnderSByteMinValue}'. Expected: '-129'.");
                _counter++;
            }
        }
        ConfirmSingleOneIncrementAboveSByteMinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveSByteMinValueCastToInt16IsFoldedCorrectly()
        {
            float singleOneIncrementAboveSByteMinValue = -127.99999f;

            if (BreakUpFlow())
                return;

            if (checked((short)singleOneIncrementAboveSByteMinValue) != -127)
            {
                Console.WriteLine($"'(short)-127.99999f' was evaluated to '{(short)singleOneIncrementAboveSByteMinValue}'. Expected: '-127'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullIncrementAboveSByteMinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveSByteMinValueCastToInt16IsFoldedCorrectly()
        {
            float singleOneFullIncrementAboveSByteMinValue = -127f;

            if (BreakUpFlow())
                return;

            if (checked((short)singleOneFullIncrementAboveSByteMinValue) != -127)
            {
                Console.WriteLine($"'(short)-127f' was evaluated to '{(short)singleOneFullIncrementAboveSByteMinValue}'. Expected: '-127'.");
                _counter++;
            }
        }
        ConfirmSingleOneDecrementUnderSByteMaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderSByteMaxValueCastToInt16IsFoldedCorrectly()
        {
            float singleOneDecrementUnderSByteMaxValue = 126.99999f;

            if (BreakUpFlow())
                return;

            if (checked((short)singleOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(short)126.99999f' was evaluated to '{(short)singleOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullDecrementUnderSByteMaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderSByteMaxValueCastToInt16IsFoldedCorrectly()
        {
            float singleOneFullDecrementUnderSByteMaxValue = 126f;

            if (BreakUpFlow())
                return;

            if (checked((short)singleOneFullDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(short)126f' was evaluated to '{(short)singleOneFullDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmSingleOneIncrementAboveSByteMaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveSByteMaxValueCastToInt16IsFoldedCorrectly()
        {
            float singleOneIncrementAboveSByteMaxValue = 127.00001f;

            if (BreakUpFlow())
                return;

            if (checked((short)singleOneIncrementAboveSByteMaxValue) != 127)
            {
                Console.WriteLine($"'(short)127.00001f' was evaluated to '{(short)singleOneIncrementAboveSByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullIncrementAboveSByteMaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveSByteMaxValueCastToInt16IsFoldedCorrectly()
        {
            float singleOneFullIncrementAboveSByteMaxValue = 128f;

            if (BreakUpFlow())
                return;

            if (checked((short)singleOneFullIncrementAboveSByteMaxValue) != 128)
            {
                Console.WriteLine($"'(short)128f' was evaluated to '{(short)singleOneFullIncrementAboveSByteMaxValue}'. Expected: '128'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMinValueCastToInt16IsFoldedCorrectly()
        {
            float integerOneDecrementUnderSByteMinValue = -129.0f;

            if (BreakUpFlow())
                return;

            if (checked((short)integerOneDecrementUnderSByteMinValue) != -129)
            {
                Console.WriteLine($"'(short)-129.0f' was evaluated to '{(short)integerOneDecrementUnderSByteMinValue}'. Expected: '-129'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMinValueCastToInt16IsFoldedCorrectly()
        {
            float integerOneIncrementAboveSByteMinValue = -127.0f;

            if (BreakUpFlow())
                return;

            if (checked((short)integerOneIncrementAboveSByteMinValue) != -127)
            {
                Console.WriteLine($"'(short)-127.0f' was evaluated to '{(short)integerOneIncrementAboveSByteMinValue}'. Expected: '-127'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToInt16IsFoldedCorrectly()
        {
            float integerOneDecrementUnderSByteMaxValue = 126.0f;

            if (BreakUpFlow())
                return;

            if (checked((short)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(short)126.0f' was evaluated to '{(short)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMaxValueCastToInt16IsFoldedCorrectly()
        {
            float integerOneIncrementAboveSByteMaxValue = 128.0f;

            if (BreakUpFlow())
                return;

            if (checked((short)integerOneIncrementAboveSByteMaxValue) != 128)
            {
                Console.WriteLine($"'(short)128.0f' was evaluated to '{(short)integerOneIncrementAboveSByteMaxValue}'. Expected: '128'.");
                _counter++;
            }
        }
        ConfirmByteMaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmByteMaxValueCastToInt16IsFoldedCorrectly()
        {
            float byteMaxValue = 255.0f;

            if (BreakUpFlow())
                return;

            if (checked((short)byteMaxValue) != 255)
            {
                Console.WriteLine($"'(short)255.0f' was evaluated to '{(short)byteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmSingleOneDecrementUnderByteMinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderByteMinValueCastToInt16IsFoldedCorrectly()
        {
            float singleOneDecrementUnderByteMinValue = -1E-45f;

            if (BreakUpFlow())
                return;

            if (checked((short)singleOneDecrementUnderByteMinValue) != 0)
            {
                Console.WriteLine($"'(short)-1E-45f' was evaluated to '{(short)singleOneDecrementUnderByteMinValue}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullDecrementUnderByteMinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderByteMinValueCastToInt16IsFoldedCorrectly()
        {
            float singleOneFullDecrementUnderByteMinValue = -1f;

            if (BreakUpFlow())
                return;

            if (checked((short)singleOneFullDecrementUnderByteMinValue) != -1)
            {
                Console.WriteLine($"'(short)-1f' was evaluated to '{(short)singleOneFullDecrementUnderByteMinValue}'. Expected: '-1'.");
                _counter++;
            }
        }
        ConfirmSingleOneIncrementAboveByteMinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveByteMinValueCastToInt16IsFoldedCorrectly()
        {
            float singleOneIncrementAboveByteMinValue = 1E-45f;

            if (BreakUpFlow())
                return;

            if (checked((short)singleOneIncrementAboveByteMinValue) != 0)
            {
                Console.WriteLine($"'(short)1E-45f' was evaluated to '{(short)singleOneIncrementAboveByteMinValue}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullIncrementAboveByteMinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveByteMinValueCastToInt16IsFoldedCorrectly()
        {
            float singleOneFullIncrementAboveByteMinValue = 1f;

            if (BreakUpFlow())
                return;

            if (checked((short)singleOneFullIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(short)1f' was evaluated to '{(short)singleOneFullIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmSingleOneDecrementUnderByteMaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderByteMaxValueCastToInt16IsFoldedCorrectly()
        {
            float singleOneDecrementUnderByteMaxValue = 254.99998f;

            if (BreakUpFlow())
                return;

            if (checked((short)singleOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(short)254.99998f' was evaluated to '{(short)singleOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullDecrementUnderByteMaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderByteMaxValueCastToInt16IsFoldedCorrectly()
        {
            float singleOneFullDecrementUnderByteMaxValue = 254f;

            if (BreakUpFlow())
                return;

            if (checked((short)singleOneFullDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(short)254f' was evaluated to '{(short)singleOneFullDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmSingleOneIncrementAboveByteMaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveByteMaxValueCastToInt16IsFoldedCorrectly()
        {
            float singleOneIncrementAboveByteMaxValue = 255.00002f;

            if (BreakUpFlow())
                return;

            if (checked((short)singleOneIncrementAboveByteMaxValue) != 255)
            {
                Console.WriteLine($"'(short)255.00002f' was evaluated to '{(short)singleOneIncrementAboveByteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullIncrementAboveByteMaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveByteMaxValueCastToInt16IsFoldedCorrectly()
        {
            float singleOneFullIncrementAboveByteMaxValue = 256f;

            if (BreakUpFlow())
                return;

            if (checked((short)singleOneFullIncrementAboveByteMaxValue) != 256)
            {
                Console.WriteLine($"'(short)256f' was evaluated to '{(short)singleOneFullIncrementAboveByteMaxValue}'. Expected: '256'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMinValueCastToInt16IsFoldedCorrectly()
        {
            float integerOneDecrementUnderByteMinValue = -1.0f;

            if (BreakUpFlow())
                return;

            if (checked((short)integerOneDecrementUnderByteMinValue) != -1)
            {
                Console.WriteLine($"'(short)-1.0f' was evaluated to '{(short)integerOneDecrementUnderByteMinValue}'. Expected: '-1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToInt16IsFoldedCorrectly()
        {
            float integerOneIncrementAboveByteMinValue = 1.0f;

            if (BreakUpFlow())
                return;

            if (checked((short)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(short)1.0f' was evaluated to '{(short)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMaxValueCastToInt16IsFoldedCorrectly()
        {
            float integerOneDecrementUnderByteMaxValue = 254.0f;

            if (BreakUpFlow())
                return;

            if (checked((short)integerOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(short)254.0f' was evaluated to '{(short)integerOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMaxValueCastToInt16IsFoldedCorrectly()
        {
            float integerOneIncrementAboveByteMaxValue = 256.0f;

            if (BreakUpFlow())
                return;

            if (checked((short)integerOneIncrementAboveByteMaxValue) != 256)
            {
                Console.WriteLine($"'(short)256.0f' was evaluated to '{(short)integerOneIncrementAboveByteMaxValue}'. Expected: '256'.");
                _counter++;
            }
        }
        ConfirmInt16MinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MinValueCastToInt16IsFoldedCorrectly()
        {
            float int16MinValue = -32768.0f;

            if (BreakUpFlow())
                return;

            if (checked((short)int16MinValue) != -32768)
            {
                Console.WriteLine($"'(short)-32768.0f' was evaluated to '{(short)int16MinValue}'. Expected: '-32768'.");
                _counter++;
            }
        }
        ConfirmInt16MaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MaxValueCastToInt16IsFoldedCorrectly()
        {
            float int16MaxValue = 32767.0f;

            if (BreakUpFlow())
                return;

            if (checked((short)int16MaxValue) != 32767)
            {
                Console.WriteLine($"'(short)32767.0f' was evaluated to '{(short)int16MaxValue}'. Expected: '32767'.");
                _counter++;
            }
        }
        ConfirmSingleOneDecrementUnderInt16MinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderInt16MinValueCastToInt16IsFoldedCorrectly()
        {
            float singleOneDecrementUnderInt16MinValue = -32768.004f;

            if (BreakUpFlow())
                return;

            if (checked((short)singleOneDecrementUnderInt16MinValue) != -32768)
            {
                Console.WriteLine($"'(short)-32768.004f' was evaluated to '{(short)singleOneDecrementUnderInt16MinValue}'. Expected: '-32768'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullDecrementUnderInt16MinValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderInt16MinValueCastToInt16Overflows()
        {
            float from = -32769f;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)-32769f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveInt16MinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveInt16MinValueCastToInt16IsFoldedCorrectly()
        {
            float singleOneIncrementAboveInt16MinValue = -32767.998f;

            if (BreakUpFlow())
                return;

            if (checked((short)singleOneIncrementAboveInt16MinValue) != -32767)
            {
                Console.WriteLine($"'(short)-32767.998f' was evaluated to '{(short)singleOneIncrementAboveInt16MinValue}'. Expected: '-32767'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullIncrementAboveInt16MinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveInt16MinValueCastToInt16IsFoldedCorrectly()
        {
            float singleOneFullIncrementAboveInt16MinValue = -32767f;

            if (BreakUpFlow())
                return;

            if (checked((short)singleOneFullIncrementAboveInt16MinValue) != -32767)
            {
                Console.WriteLine($"'(short)-32767f' was evaluated to '{(short)singleOneFullIncrementAboveInt16MinValue}'. Expected: '-32767'.");
                _counter++;
            }
        }
        ConfirmSingleOneDecrementUnderInt16MaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderInt16MaxValueCastToInt16IsFoldedCorrectly()
        {
            float singleOneDecrementUnderInt16MaxValue = 32766.998f;

            if (BreakUpFlow())
                return;

            if (checked((short)singleOneDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(short)32766.998f' was evaluated to '{(short)singleOneDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullDecrementUnderInt16MaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderInt16MaxValueCastToInt16IsFoldedCorrectly()
        {
            float singleOneFullDecrementUnderInt16MaxValue = 32766f;

            if (BreakUpFlow())
                return;

            if (checked((short)singleOneFullDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(short)32766f' was evaluated to '{(short)singleOneFullDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
        ConfirmSingleOneIncrementAboveInt16MaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveInt16MaxValueCastToInt16IsFoldedCorrectly()
        {
            float singleOneIncrementAboveInt16MaxValue = 32767.002f;

            if (BreakUpFlow())
                return;

            if (checked((short)singleOneIncrementAboveInt16MaxValue) != 32767)
            {
                Console.WriteLine($"'(short)32767.002f' was evaluated to '{(short)singleOneIncrementAboveInt16MaxValue}'. Expected: '32767'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullIncrementAboveInt16MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveInt16MaxValueCastToInt16Overflows()
        {
            float from = 32768f;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)32768f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt16MinValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MinValueCastToInt16Overflows()
        {
            float from = -32769.0f;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)-32769.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt16MinValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt16MinValueCastToInt16IsFoldedCorrectly()
        {
            float integerOneIncrementAboveInt16MinValue = -32767.0f;

            if (BreakUpFlow())
                return;

            if (checked((short)integerOneIncrementAboveInt16MinValue) != -32767)
            {
                Console.WriteLine($"'(short)-32767.0f' was evaluated to '{(short)integerOneIncrementAboveInt16MinValue}'. Expected: '-32767'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt16MaxValueCastToInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MaxValueCastToInt16IsFoldedCorrectly()
        {
            float integerOneDecrementUnderInt16MaxValue = 32766.0f;

            if (BreakUpFlow())
                return;

            if (checked((short)integerOneDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(short)32766.0f' was evaluated to '{(short)integerOneDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt16MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt16MaxValueCastToInt16Overflows()
        {
            float from = 32768.0f;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)32768.0f)' did not throw OverflowException.");
        }
        ConfirmUInt16MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt16MaxValueCastToInt16Overflows()
        {
            float from = 65535.0f;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)65535.0f)' did not throw OverflowException.");
        }
        ConfirmSingleOneDecrementUnderUInt16MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderUInt16MaxValueCastToInt16Overflows()
        {
            float from = 65534.996f;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)65534.996f)' did not throw OverflowException.");
        }
        ConfirmSingleOneFullDecrementUnderUInt16MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderUInt16MaxValueCastToInt16Overflows()
        {
            float from = 65534f;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)65534f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveUInt16MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveUInt16MaxValueCastToInt16Overflows()
        {
            float from = 65535.004f;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)65535.004f)' did not throw OverflowException.");
        }
        ConfirmSingleOneFullIncrementAboveUInt16MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveUInt16MaxValueCastToInt16Overflows()
        {
            float from = 65536f;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)65536f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderUInt16MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt16MaxValueCastToInt16Overflows()
        {
            float from = 65534.0f;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)65534.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToInt16Overflows()
        {
            float from = 65536.0f;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)65536.0f)' did not throw OverflowException.");
        }
        ConfirmInt32MinValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MinValueCastToInt16Overflows()
        {
            float from = -2147483648.0f;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)-2147483648.0f)' did not throw OverflowException.");
        }
        ConfirmInt32MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MaxValueCastToInt16Overflows()
        {
            float from = 2147483647.0f;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)2147483647.0f)' did not throw OverflowException.");
        }
        ConfirmSingleOneDecrementUnderInt32MinValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderInt32MinValueCastToInt16Overflows()
        {
            float from = -2.147484E+09f;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)-2.147484E+09f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveInt32MinValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveInt32MinValueCastToInt16Overflows()
        {
            float from = -2.1474835E+09f;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)-2.1474835E+09f)' did not throw OverflowException.");
        }
        ConfirmSingleOneDecrementUnderInt32MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderInt32MaxValueCastToInt16Overflows()
        {
            float from = 2.1474835E+09f;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)2.1474835E+09f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveInt32MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveInt32MaxValueCastToInt16Overflows()
        {
            float from = 2.147484E+09f;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)2.147484E+09f)' did not throw OverflowException.");
        }
        ConfirmSingleOneFullIncrementAboveInt32MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveInt32MaxValueCastToInt16Overflows()
        {
            float from = 2.1474836E+09f;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)2.1474836E+09f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt32MinValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt32MinValueCastToInt16Overflows()
        {
            float from = -2147483649.0f;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)-2147483649.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt32MinValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt32MinValueCastToInt16Overflows()
        {
            float from = -2147483647.0f;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)-2147483647.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt32MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt32MaxValueCastToInt16Overflows()
        {
            float from = 2147483646.0f;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)2147483646.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt32MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt32MaxValueCastToInt16Overflows()
        {
            float from = 2147483648.0f;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)2147483648.0f)' did not throw OverflowException.");
        }
        ConfirmUInt32MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32MaxValueCastToInt16Overflows()
        {
            float from = 4294967295.0f;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)4294967295.0f)' did not throw OverflowException.");
        }
        ConfirmSingleOneDecrementUnderUInt32MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderUInt32MaxValueCastToInt16Overflows()
        {
            float from = 4.294967E+09f;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)4.294967E+09f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveUInt32MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveUInt32MaxValueCastToInt16Overflows()
        {
            float from = 4.294968E+09f;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)4.294968E+09f)' did not throw OverflowException.");
        }
        ConfirmSingleOneFullIncrementAboveUInt32MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveUInt32MaxValueCastToInt16Overflows()
        {
            float from = 4.2949673E+09f;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)4.2949673E+09f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderUInt32MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt32MaxValueCastToInt16Overflows()
        {
            float from = 4294967294.0f;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)4294967294.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveUInt32MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt32MaxValueCastToInt16Overflows()
        {
            float from = 4294967296.0f;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)4294967296.0f)' did not throw OverflowException.");
        }
        ConfirmInt64MinValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MinValueCastToInt16Overflows()
        {
            float from = -9223372036854775808.0f;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)-9223372036854775808.0f)' did not throw OverflowException.");
        }
        ConfirmInt64MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MaxValueCastToInt16Overflows()
        {
            float from = 9223372036854775807.0f;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)9223372036854775807.0f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveInt64MinValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveInt64MinValueCastToInt16Overflows()
        {
            float from = -9.2233715E+18f;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)-9.2233715E+18f)' did not throw OverflowException.");
        }
        ConfirmSingleOneDecrementUnderInt64MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderInt64MaxValueCastToInt16Overflows()
        {
            float from = 9.2233715E+18f;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)9.2233715E+18f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveInt64MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveInt64MaxValueCastToInt16Overflows()
        {
            float from = 9.223373E+18f;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)9.223373E+18f)' did not throw OverflowException.");
        }
        ConfirmSingleOneFullIncrementAboveInt64MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveInt64MaxValueCastToInt16Overflows()
        {
            float from = 9.223372E+18f;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)9.223372E+18f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt64MinValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt64MinValueCastToInt16Overflows()
        {
            float from = -9223372036854775807.0f;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)-9223372036854775807.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt64MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt64MaxValueCastToInt16Overflows()
        {
            float from = 9223372036854775806.0f;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)9223372036854775806.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt64MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt64MaxValueCastToInt16Overflows()
        {
            float from = 9223372036854775808.0f;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)9223372036854775808.0f)' did not throw OverflowException.");
        }
        ConfirmUInt64MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64MaxValueCastToInt16Overflows()
        {
            float from = 18446744073709551615.0f;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)18446744073709551615.0f)' did not throw OverflowException.");
        }
        ConfirmSingleOneDecrementUnderUInt64MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderUInt64MaxValueCastToInt16Overflows()
        {
            float from = 1.8446743E+19f;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)1.8446743E+19f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderUInt64MaxValueCastToInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt64MaxValueCastToInt16Overflows()
        {
            float from = 18446744073709551614.0f;
            _counter++;
            try
            {
                _ = checked((short)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((short)18446744073709551614.0f)' did not throw OverflowException.");
        }
    }

    private static void TestCastingSingleToUInt16()
    {
        ConfirmIntegerZeroCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToUInt16IsFoldedCorrectly()
        {
            float integerZero = 0.0f;

            if (BreakUpFlow())
                return;

            if (checked((ushort)integerZero) != 0)
            {
                Console.WriteLine($"'(ushort)0.0f' was evaluated to '{(ushort)integerZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmFloatMinusZeroCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmFloatMinusZeroCastToUInt16IsFoldedCorrectly()
        {
            float floatMinusZero = -0f;

            if (BreakUpFlow())
                return;

            if (checked((ushort)floatMinusZero) != 0)
            {
                Console.WriteLine($"'(ushort)-0f' was evaluated to '{(ushort)floatMinusZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmFloatZeroCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmFloatZeroCastToUInt16IsFoldedCorrectly()
        {
            float floatZero = 0f;

            if (BreakUpFlow())
                return;

            if (checked((ushort)floatZero) != 0)
            {
                Console.WriteLine($"'(ushort)0f' was evaluated to '{(ushort)floatZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmFloatHalfOfMaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmFloatHalfOfMaxValueCastToUInt16Overflows()
        {
            float from = 1.7014117E+38f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)1.7014117E+38f)' did not throw OverflowException.");
        }
        ConfirmFloatHalfOfMinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmFloatHalfOfMinValueCastToUInt16Overflows()
        {
            float from = -1.7014117E+38f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-1.7014117E+38f)' did not throw OverflowException.");
        }
        ConfirmSingleMinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleMinValueCastToUInt16Overflows()
        {
            float from = -3.4028235E+38f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-3.4028235E+38f)' did not throw OverflowException.");
        }
        ConfirmSingleMaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleMaxValueCastToUInt16Overflows()
        {
            float from = 3.4028235E+38f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)3.4028235E+38f)' did not throw OverflowException.");
        }
        ConfirmSByteMinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMinValueCastToUInt16Overflows()
        {
            float from = -128.0f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-128.0f)' did not throw OverflowException.");
        }
        ConfirmSByteMaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToUInt16IsFoldedCorrectly()
        {
            float sByteMaxValue = 127.0f;

            if (BreakUpFlow())
                return;

            if (checked((ushort)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(ushort)127.0f' was evaluated to '{(ushort)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmSingleOneDecrementUnderSByteMinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderSByteMinValueCastToUInt16Overflows()
        {
            float from = -128.00002f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-128.00002f)' did not throw OverflowException.");
        }
        ConfirmSingleOneFullDecrementUnderSByteMinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderSByteMinValueCastToUInt16Overflows()
        {
            float from = -129f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-129f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveSByteMinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveSByteMinValueCastToUInt16Overflows()
        {
            float from = -127.99999f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-127.99999f)' did not throw OverflowException.");
        }
        ConfirmSingleOneFullIncrementAboveSByteMinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveSByteMinValueCastToUInt16Overflows()
        {
            float from = -127f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-127f)' did not throw OverflowException.");
        }
        ConfirmSingleOneDecrementUnderSByteMaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderSByteMaxValueCastToUInt16IsFoldedCorrectly()
        {
            float singleOneDecrementUnderSByteMaxValue = 126.99999f;

            if (BreakUpFlow())
                return;

            if (checked((ushort)singleOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(ushort)126.99999f' was evaluated to '{(ushort)singleOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullDecrementUnderSByteMaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderSByteMaxValueCastToUInt16IsFoldedCorrectly()
        {
            float singleOneFullDecrementUnderSByteMaxValue = 126f;

            if (BreakUpFlow())
                return;

            if (checked((ushort)singleOneFullDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(ushort)126f' was evaluated to '{(ushort)singleOneFullDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmSingleOneIncrementAboveSByteMaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveSByteMaxValueCastToUInt16IsFoldedCorrectly()
        {
            float singleOneIncrementAboveSByteMaxValue = 127.00001f;

            if (BreakUpFlow())
                return;

            if (checked((ushort)singleOneIncrementAboveSByteMaxValue) != 127)
            {
                Console.WriteLine($"'(ushort)127.00001f' was evaluated to '{(ushort)singleOneIncrementAboveSByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullIncrementAboveSByteMaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveSByteMaxValueCastToUInt16IsFoldedCorrectly()
        {
            float singleOneFullIncrementAboveSByteMaxValue = 128f;

            if (BreakUpFlow())
                return;

            if (checked((ushort)singleOneFullIncrementAboveSByteMaxValue) != 128)
            {
                Console.WriteLine($"'(ushort)128f' was evaluated to '{(ushort)singleOneFullIncrementAboveSByteMaxValue}'. Expected: '128'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMinValueCastToUInt16Overflows()
        {
            float from = -129.0f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-129.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveSByteMinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMinValueCastToUInt16Overflows()
        {
            float from = -127.0f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-127.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToUInt16IsFoldedCorrectly()
        {
            float integerOneDecrementUnderSByteMaxValue = 126.0f;

            if (BreakUpFlow())
                return;

            if (checked((ushort)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(ushort)126.0f' was evaluated to '{(ushort)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMaxValueCastToUInt16IsFoldedCorrectly()
        {
            float integerOneIncrementAboveSByteMaxValue = 128.0f;

            if (BreakUpFlow())
                return;

            if (checked((ushort)integerOneIncrementAboveSByteMaxValue) != 128)
            {
                Console.WriteLine($"'(ushort)128.0f' was evaluated to '{(ushort)integerOneIncrementAboveSByteMaxValue}'. Expected: '128'.");
                _counter++;
            }
        }
        ConfirmByteMaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmByteMaxValueCastToUInt16IsFoldedCorrectly()
        {
            float byteMaxValue = 255.0f;

            if (BreakUpFlow())
                return;

            if (checked((ushort)byteMaxValue) != 255)
            {
                Console.WriteLine($"'(ushort)255.0f' was evaluated to '{(ushort)byteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmSingleOneDecrementUnderByteMinValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderByteMinValueCastToUInt16IsFoldedCorrectly()
        {
            float singleOneDecrementUnderByteMinValue = -1E-45f;

            if (BreakUpFlow())
                return;

            if (checked((ushort)singleOneDecrementUnderByteMinValue) != 0)
            {
                Console.WriteLine($"'(ushort)-1E-45f' was evaluated to '{(ushort)singleOneDecrementUnderByteMinValue}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullDecrementUnderByteMinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderByteMinValueCastToUInt16Overflows()
        {
            float from = -1f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-1f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveByteMinValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveByteMinValueCastToUInt16IsFoldedCorrectly()
        {
            float singleOneIncrementAboveByteMinValue = 1E-45f;

            if (BreakUpFlow())
                return;

            if (checked((ushort)singleOneIncrementAboveByteMinValue) != 0)
            {
                Console.WriteLine($"'(ushort)1E-45f' was evaluated to '{(ushort)singleOneIncrementAboveByteMinValue}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullIncrementAboveByteMinValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveByteMinValueCastToUInt16IsFoldedCorrectly()
        {
            float singleOneFullIncrementAboveByteMinValue = 1f;

            if (BreakUpFlow())
                return;

            if (checked((ushort)singleOneFullIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(ushort)1f' was evaluated to '{(ushort)singleOneFullIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmSingleOneDecrementUnderByteMaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderByteMaxValueCastToUInt16IsFoldedCorrectly()
        {
            float singleOneDecrementUnderByteMaxValue = 254.99998f;

            if (BreakUpFlow())
                return;

            if (checked((ushort)singleOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(ushort)254.99998f' was evaluated to '{(ushort)singleOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullDecrementUnderByteMaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderByteMaxValueCastToUInt16IsFoldedCorrectly()
        {
            float singleOneFullDecrementUnderByteMaxValue = 254f;

            if (BreakUpFlow())
                return;

            if (checked((ushort)singleOneFullDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(ushort)254f' was evaluated to '{(ushort)singleOneFullDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmSingleOneIncrementAboveByteMaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveByteMaxValueCastToUInt16IsFoldedCorrectly()
        {
            float singleOneIncrementAboveByteMaxValue = 255.00002f;

            if (BreakUpFlow())
                return;

            if (checked((ushort)singleOneIncrementAboveByteMaxValue) != 255)
            {
                Console.WriteLine($"'(ushort)255.00002f' was evaluated to '{(ushort)singleOneIncrementAboveByteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullIncrementAboveByteMaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveByteMaxValueCastToUInt16IsFoldedCorrectly()
        {
            float singleOneFullIncrementAboveByteMaxValue = 256f;

            if (BreakUpFlow())
                return;

            if (checked((ushort)singleOneFullIncrementAboveByteMaxValue) != 256)
            {
                Console.WriteLine($"'(ushort)256f' was evaluated to '{(ushort)singleOneFullIncrementAboveByteMaxValue}'. Expected: '256'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMinValueCastToUInt16Overflows()
        {
            float from = -1.0f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-1.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToUInt16IsFoldedCorrectly()
        {
            float integerOneIncrementAboveByteMinValue = 1.0f;

            if (BreakUpFlow())
                return;

            if (checked((ushort)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(ushort)1.0f' was evaluated to '{(ushort)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMaxValueCastToUInt16IsFoldedCorrectly()
        {
            float integerOneDecrementUnderByteMaxValue = 254.0f;

            if (BreakUpFlow())
                return;

            if (checked((ushort)integerOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(ushort)254.0f' was evaluated to '{(ushort)integerOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMaxValueCastToUInt16IsFoldedCorrectly()
        {
            float integerOneIncrementAboveByteMaxValue = 256.0f;

            if (BreakUpFlow())
                return;

            if (checked((ushort)integerOneIncrementAboveByteMaxValue) != 256)
            {
                Console.WriteLine($"'(ushort)256.0f' was evaluated to '{(ushort)integerOneIncrementAboveByteMaxValue}'. Expected: '256'.");
                _counter++;
            }
        }
        ConfirmInt16MinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MinValueCastToUInt16Overflows()
        {
            float from = -32768.0f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-32768.0f)' did not throw OverflowException.");
        }
        ConfirmInt16MaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MaxValueCastToUInt16IsFoldedCorrectly()
        {
            float int16MaxValue = 32767.0f;

            if (BreakUpFlow())
                return;

            if (checked((ushort)int16MaxValue) != 32767)
            {
                Console.WriteLine($"'(ushort)32767.0f' was evaluated to '{(ushort)int16MaxValue}'. Expected: '32767'.");
                _counter++;
            }
        }
        ConfirmSingleOneDecrementUnderInt16MinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderInt16MinValueCastToUInt16Overflows()
        {
            float from = -32768.004f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-32768.004f)' did not throw OverflowException.");
        }
        ConfirmSingleOneFullDecrementUnderInt16MinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderInt16MinValueCastToUInt16Overflows()
        {
            float from = -32769f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-32769f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveInt16MinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveInt16MinValueCastToUInt16Overflows()
        {
            float from = -32767.998f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-32767.998f)' did not throw OverflowException.");
        }
        ConfirmSingleOneFullIncrementAboveInt16MinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveInt16MinValueCastToUInt16Overflows()
        {
            float from = -32767f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-32767f)' did not throw OverflowException.");
        }
        ConfirmSingleOneDecrementUnderInt16MaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderInt16MaxValueCastToUInt16IsFoldedCorrectly()
        {
            float singleOneDecrementUnderInt16MaxValue = 32766.998f;

            if (BreakUpFlow())
                return;

            if (checked((ushort)singleOneDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(ushort)32766.998f' was evaluated to '{(ushort)singleOneDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullDecrementUnderInt16MaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderInt16MaxValueCastToUInt16IsFoldedCorrectly()
        {
            float singleOneFullDecrementUnderInt16MaxValue = 32766f;

            if (BreakUpFlow())
                return;

            if (checked((ushort)singleOneFullDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(ushort)32766f' was evaluated to '{(ushort)singleOneFullDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
        ConfirmSingleOneIncrementAboveInt16MaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveInt16MaxValueCastToUInt16IsFoldedCorrectly()
        {
            float singleOneIncrementAboveInt16MaxValue = 32767.002f;

            if (BreakUpFlow())
                return;

            if (checked((ushort)singleOneIncrementAboveInt16MaxValue) != 32767)
            {
                Console.WriteLine($"'(ushort)32767.002f' was evaluated to '{(ushort)singleOneIncrementAboveInt16MaxValue}'. Expected: '32767'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullIncrementAboveInt16MaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveInt16MaxValueCastToUInt16IsFoldedCorrectly()
        {
            float singleOneFullIncrementAboveInt16MaxValue = 32768f;

            if (BreakUpFlow())
                return;

            if (checked((ushort)singleOneFullIncrementAboveInt16MaxValue) != 32768)
            {
                Console.WriteLine($"'(ushort)32768f' was evaluated to '{(ushort)singleOneFullIncrementAboveInt16MaxValue}'. Expected: '32768'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt16MinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MinValueCastToUInt16Overflows()
        {
            float from = -32769.0f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-32769.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt16MinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt16MinValueCastToUInt16Overflows()
        {
            float from = -32767.0f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-32767.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt16MaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MaxValueCastToUInt16IsFoldedCorrectly()
        {
            float integerOneDecrementUnderInt16MaxValue = 32766.0f;

            if (BreakUpFlow())
                return;

            if (checked((ushort)integerOneDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(ushort)32766.0f' was evaluated to '{(ushort)integerOneDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt16MaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt16MaxValueCastToUInt16IsFoldedCorrectly()
        {
            float integerOneIncrementAboveInt16MaxValue = 32768.0f;

            if (BreakUpFlow())
                return;

            if (checked((ushort)integerOneIncrementAboveInt16MaxValue) != 32768)
            {
                Console.WriteLine($"'(ushort)32768.0f' was evaluated to '{(ushort)integerOneIncrementAboveInt16MaxValue}'. Expected: '32768'.");
                _counter++;
            }
        }
        ConfirmUInt16MaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt16MaxValueCastToUInt16IsFoldedCorrectly()
        {
            float uInt16MaxValue = 65535.0f;

            if (BreakUpFlow())
                return;

            if (checked((ushort)uInt16MaxValue) != 65535)
            {
                Console.WriteLine($"'(ushort)65535.0f' was evaluated to '{(ushort)uInt16MaxValue}'. Expected: '65535'.");
                _counter++;
            }
        }
        ConfirmSingleOneDecrementUnderUInt16MaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderUInt16MaxValueCastToUInt16IsFoldedCorrectly()
        {
            float singleOneDecrementUnderUInt16MaxValue = 65534.996f;

            if (BreakUpFlow())
                return;

            if (checked((ushort)singleOneDecrementUnderUInt16MaxValue) != 65534)
            {
                Console.WriteLine($"'(ushort)65534.996f' was evaluated to '{(ushort)singleOneDecrementUnderUInt16MaxValue}'. Expected: '65534'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullDecrementUnderUInt16MaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderUInt16MaxValueCastToUInt16IsFoldedCorrectly()
        {
            float singleOneFullDecrementUnderUInt16MaxValue = 65534f;

            if (BreakUpFlow())
                return;

            if (checked((ushort)singleOneFullDecrementUnderUInt16MaxValue) != 65534)
            {
                Console.WriteLine($"'(ushort)65534f' was evaluated to '{(ushort)singleOneFullDecrementUnderUInt16MaxValue}'. Expected: '65534'.");
                _counter++;
            }
        }
        ConfirmSingleOneIncrementAboveUInt16MaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveUInt16MaxValueCastToUInt16IsFoldedCorrectly()
        {
            float singleOneIncrementAboveUInt16MaxValue = 65535.004f;

            if (BreakUpFlow())
                return;

            if (checked((ushort)singleOneIncrementAboveUInt16MaxValue) != 65535)
            {
                Console.WriteLine($"'(ushort)65535.004f' was evaluated to '{(ushort)singleOneIncrementAboveUInt16MaxValue}'. Expected: '65535'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullIncrementAboveUInt16MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveUInt16MaxValueCastToUInt16Overflows()
        {
            float from = 65536f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)65536f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderUInt16MaxValueCastToUInt16IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt16MaxValueCastToUInt16IsFoldedCorrectly()
        {
            float integerOneDecrementUnderUInt16MaxValue = 65534.0f;

            if (BreakUpFlow())
                return;

            if (checked((ushort)integerOneDecrementUnderUInt16MaxValue) != 65534)
            {
                Console.WriteLine($"'(ushort)65534.0f' was evaluated to '{(ushort)integerOneDecrementUnderUInt16MaxValue}'. Expected: '65534'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToUInt16Overflows()
        {
            float from = 65536.0f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)65536.0f)' did not throw OverflowException.");
        }
        ConfirmInt32MinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MinValueCastToUInt16Overflows()
        {
            float from = -2147483648.0f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-2147483648.0f)' did not throw OverflowException.");
        }
        ConfirmInt32MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MaxValueCastToUInt16Overflows()
        {
            float from = 2147483647.0f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)2147483647.0f)' did not throw OverflowException.");
        }
        ConfirmSingleOneDecrementUnderInt32MinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderInt32MinValueCastToUInt16Overflows()
        {
            float from = -2.147484E+09f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-2.147484E+09f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveInt32MinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveInt32MinValueCastToUInt16Overflows()
        {
            float from = -2.1474835E+09f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-2.1474835E+09f)' did not throw OverflowException.");
        }
        ConfirmSingleOneDecrementUnderInt32MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderInt32MaxValueCastToUInt16Overflows()
        {
            float from = 2.1474835E+09f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)2.1474835E+09f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveInt32MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveInt32MaxValueCastToUInt16Overflows()
        {
            float from = 2.147484E+09f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)2.147484E+09f)' did not throw OverflowException.");
        }
        ConfirmSingleOneFullIncrementAboveInt32MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveInt32MaxValueCastToUInt16Overflows()
        {
            float from = 2.1474836E+09f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)2.1474836E+09f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt32MinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt32MinValueCastToUInt16Overflows()
        {
            float from = -2147483649.0f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-2147483649.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt32MinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt32MinValueCastToUInt16Overflows()
        {
            float from = -2147483647.0f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-2147483647.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt32MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt32MaxValueCastToUInt16Overflows()
        {
            float from = 2147483646.0f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)2147483646.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt32MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt32MaxValueCastToUInt16Overflows()
        {
            float from = 2147483648.0f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)2147483648.0f)' did not throw OverflowException.");
        }
        ConfirmUInt32MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32MaxValueCastToUInt16Overflows()
        {
            float from = 4294967295.0f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)4294967295.0f)' did not throw OverflowException.");
        }
        ConfirmSingleOneDecrementUnderUInt32MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderUInt32MaxValueCastToUInt16Overflows()
        {
            float from = 4.294967E+09f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)4.294967E+09f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveUInt32MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveUInt32MaxValueCastToUInt16Overflows()
        {
            float from = 4.294968E+09f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)4.294968E+09f)' did not throw OverflowException.");
        }
        ConfirmSingleOneFullIncrementAboveUInt32MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveUInt32MaxValueCastToUInt16Overflows()
        {
            float from = 4.2949673E+09f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)4.2949673E+09f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderUInt32MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt32MaxValueCastToUInt16Overflows()
        {
            float from = 4294967294.0f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)4294967294.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveUInt32MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt32MaxValueCastToUInt16Overflows()
        {
            float from = 4294967296.0f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)4294967296.0f)' did not throw OverflowException.");
        }
        ConfirmInt64MinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MinValueCastToUInt16Overflows()
        {
            float from = -9223372036854775808.0f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-9223372036854775808.0f)' did not throw OverflowException.");
        }
        ConfirmInt64MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MaxValueCastToUInt16Overflows()
        {
            float from = 9223372036854775807.0f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)9223372036854775807.0f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveInt64MinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveInt64MinValueCastToUInt16Overflows()
        {
            float from = -9.2233715E+18f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-9.2233715E+18f)' did not throw OverflowException.");
        }
        ConfirmSingleOneDecrementUnderInt64MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderInt64MaxValueCastToUInt16Overflows()
        {
            float from = 9.2233715E+18f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)9.2233715E+18f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveInt64MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveInt64MaxValueCastToUInt16Overflows()
        {
            float from = 9.223373E+18f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)9.223373E+18f)' did not throw OverflowException.");
        }
        ConfirmSingleOneFullIncrementAboveInt64MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveInt64MaxValueCastToUInt16Overflows()
        {
            float from = 9.223372E+18f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)9.223372E+18f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt64MinValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt64MinValueCastToUInt16Overflows()
        {
            float from = -9223372036854775807.0f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)-9223372036854775807.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt64MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt64MaxValueCastToUInt16Overflows()
        {
            float from = 9223372036854775806.0f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)9223372036854775806.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt64MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt64MaxValueCastToUInt16Overflows()
        {
            float from = 9223372036854775808.0f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)9223372036854775808.0f)' did not throw OverflowException.");
        }
        ConfirmUInt64MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64MaxValueCastToUInt16Overflows()
        {
            float from = 18446744073709551615.0f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)18446744073709551615.0f)' did not throw OverflowException.");
        }
        ConfirmSingleOneDecrementUnderUInt64MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderUInt64MaxValueCastToUInt16Overflows()
        {
            float from = 1.8446743E+19f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)1.8446743E+19f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderUInt64MaxValueCastToUInt16Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt64MaxValueCastToUInt16Overflows()
        {
            float from = 18446744073709551614.0f;
            _counter++;
            try
            {
                _ = checked((ushort)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ushort)18446744073709551614.0f)' did not throw OverflowException.");
        }
    }

    private static void TestCastingSingleToInt32()
    {
        ConfirmIntegerZeroCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToInt32IsFoldedCorrectly()
        {
            float integerZero = 0.0f;

            if (BreakUpFlow())
                return;

            if (checked((int)integerZero) != 0)
            {
                Console.WriteLine($"'(int)0.0f' was evaluated to '{(int)integerZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmFloatMinusZeroCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmFloatMinusZeroCastToInt32IsFoldedCorrectly()
        {
            float floatMinusZero = -0f;

            if (BreakUpFlow())
                return;

            if (checked((int)floatMinusZero) != 0)
            {
                Console.WriteLine($"'(int)-0f' was evaluated to '{(int)floatMinusZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmFloatZeroCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmFloatZeroCastToInt32IsFoldedCorrectly()
        {
            float floatZero = 0f;

            if (BreakUpFlow())
                return;

            if (checked((int)floatZero) != 0)
            {
                Console.WriteLine($"'(int)0f' was evaluated to '{(int)floatZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmFloatHalfOfMaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmFloatHalfOfMaxValueCastToInt32Overflows()
        {
            float from = 1.7014117E+38f;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)1.7014117E+38f)' did not throw OverflowException.");
        }
        ConfirmFloatHalfOfMinValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmFloatHalfOfMinValueCastToInt32Overflows()
        {
            float from = -1.7014117E+38f;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)-1.7014117E+38f)' did not throw OverflowException.");
        }
        ConfirmSingleMinValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleMinValueCastToInt32Overflows()
        {
            float from = -3.4028235E+38f;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)-3.4028235E+38f)' did not throw OverflowException.");
        }
        ConfirmSingleMaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleMaxValueCastToInt32Overflows()
        {
            float from = 3.4028235E+38f;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)3.4028235E+38f)' did not throw OverflowException.");
        }
        ConfirmSByteMinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMinValueCastToInt32IsFoldedCorrectly()
        {
            float sByteMinValue = -128.0f;

            if (BreakUpFlow())
                return;

            if (checked((int)sByteMinValue) != -128)
            {
                Console.WriteLine($"'(int)-128.0f' was evaluated to '{(int)sByteMinValue}'. Expected: '-128'.");
                _counter++;
            }
        }
        ConfirmSByteMaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToInt32IsFoldedCorrectly()
        {
            float sByteMaxValue = 127.0f;

            if (BreakUpFlow())
                return;

            if (checked((int)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(int)127.0f' was evaluated to '{(int)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmSingleOneDecrementUnderSByteMinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderSByteMinValueCastToInt32IsFoldedCorrectly()
        {
            float singleOneDecrementUnderSByteMinValue = -128.00002f;

            if (BreakUpFlow())
                return;

            if (checked((int)singleOneDecrementUnderSByteMinValue) != -128)
            {
                Console.WriteLine($"'(int)-128.00002f' was evaluated to '{(int)singleOneDecrementUnderSByteMinValue}'. Expected: '-128'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullDecrementUnderSByteMinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderSByteMinValueCastToInt32IsFoldedCorrectly()
        {
            float singleOneFullDecrementUnderSByteMinValue = -129f;

            if (BreakUpFlow())
                return;

            if (checked((int)singleOneFullDecrementUnderSByteMinValue) != -129)
            {
                Console.WriteLine($"'(int)-129f' was evaluated to '{(int)singleOneFullDecrementUnderSByteMinValue}'. Expected: '-129'.");
                _counter++;
            }
        }
        ConfirmSingleOneIncrementAboveSByteMinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveSByteMinValueCastToInt32IsFoldedCorrectly()
        {
            float singleOneIncrementAboveSByteMinValue = -127.99999f;

            if (BreakUpFlow())
                return;

            if (checked((int)singleOneIncrementAboveSByteMinValue) != -127)
            {
                Console.WriteLine($"'(int)-127.99999f' was evaluated to '{(int)singleOneIncrementAboveSByteMinValue}'. Expected: '-127'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullIncrementAboveSByteMinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveSByteMinValueCastToInt32IsFoldedCorrectly()
        {
            float singleOneFullIncrementAboveSByteMinValue = -127f;

            if (BreakUpFlow())
                return;

            if (checked((int)singleOneFullIncrementAboveSByteMinValue) != -127)
            {
                Console.WriteLine($"'(int)-127f' was evaluated to '{(int)singleOneFullIncrementAboveSByteMinValue}'. Expected: '-127'.");
                _counter++;
            }
        }
        ConfirmSingleOneDecrementUnderSByteMaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderSByteMaxValueCastToInt32IsFoldedCorrectly()
        {
            float singleOneDecrementUnderSByteMaxValue = 126.99999f;

            if (BreakUpFlow())
                return;

            if (checked((int)singleOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(int)126.99999f' was evaluated to '{(int)singleOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullDecrementUnderSByteMaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderSByteMaxValueCastToInt32IsFoldedCorrectly()
        {
            float singleOneFullDecrementUnderSByteMaxValue = 126f;

            if (BreakUpFlow())
                return;

            if (checked((int)singleOneFullDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(int)126f' was evaluated to '{(int)singleOneFullDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmSingleOneIncrementAboveSByteMaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveSByteMaxValueCastToInt32IsFoldedCorrectly()
        {
            float singleOneIncrementAboveSByteMaxValue = 127.00001f;

            if (BreakUpFlow())
                return;

            if (checked((int)singleOneIncrementAboveSByteMaxValue) != 127)
            {
                Console.WriteLine($"'(int)127.00001f' was evaluated to '{(int)singleOneIncrementAboveSByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullIncrementAboveSByteMaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveSByteMaxValueCastToInt32IsFoldedCorrectly()
        {
            float singleOneFullIncrementAboveSByteMaxValue = 128f;

            if (BreakUpFlow())
                return;

            if (checked((int)singleOneFullIncrementAboveSByteMaxValue) != 128)
            {
                Console.WriteLine($"'(int)128f' was evaluated to '{(int)singleOneFullIncrementAboveSByteMaxValue}'. Expected: '128'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMinValueCastToInt32IsFoldedCorrectly()
        {
            float integerOneDecrementUnderSByteMinValue = -129.0f;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneDecrementUnderSByteMinValue) != -129)
            {
                Console.WriteLine($"'(int)-129.0f' was evaluated to '{(int)integerOneDecrementUnderSByteMinValue}'. Expected: '-129'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMinValueCastToInt32IsFoldedCorrectly()
        {
            float integerOneIncrementAboveSByteMinValue = -127.0f;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneIncrementAboveSByteMinValue) != -127)
            {
                Console.WriteLine($"'(int)-127.0f' was evaluated to '{(int)integerOneIncrementAboveSByteMinValue}'. Expected: '-127'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToInt32IsFoldedCorrectly()
        {
            float integerOneDecrementUnderSByteMaxValue = 126.0f;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(int)126.0f' was evaluated to '{(int)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMaxValueCastToInt32IsFoldedCorrectly()
        {
            float integerOneIncrementAboveSByteMaxValue = 128.0f;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneIncrementAboveSByteMaxValue) != 128)
            {
                Console.WriteLine($"'(int)128.0f' was evaluated to '{(int)integerOneIncrementAboveSByteMaxValue}'. Expected: '128'.");
                _counter++;
            }
        }
        ConfirmByteMaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmByteMaxValueCastToInt32IsFoldedCorrectly()
        {
            float byteMaxValue = 255.0f;

            if (BreakUpFlow())
                return;

            if (checked((int)byteMaxValue) != 255)
            {
                Console.WriteLine($"'(int)255.0f' was evaluated to '{(int)byteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmSingleOneDecrementUnderByteMinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderByteMinValueCastToInt32IsFoldedCorrectly()
        {
            float singleOneDecrementUnderByteMinValue = -1E-45f;

            if (BreakUpFlow())
                return;

            if (checked((int)singleOneDecrementUnderByteMinValue) != 0)
            {
                Console.WriteLine($"'(int)-1E-45f' was evaluated to '{(int)singleOneDecrementUnderByteMinValue}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullDecrementUnderByteMinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderByteMinValueCastToInt32IsFoldedCorrectly()
        {
            float singleOneFullDecrementUnderByteMinValue = -1f;

            if (BreakUpFlow())
                return;

            if (checked((int)singleOneFullDecrementUnderByteMinValue) != -1)
            {
                Console.WriteLine($"'(int)-1f' was evaluated to '{(int)singleOneFullDecrementUnderByteMinValue}'. Expected: '-1'.");
                _counter++;
            }
        }
        ConfirmSingleOneIncrementAboveByteMinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveByteMinValueCastToInt32IsFoldedCorrectly()
        {
            float singleOneIncrementAboveByteMinValue = 1E-45f;

            if (BreakUpFlow())
                return;

            if (checked((int)singleOneIncrementAboveByteMinValue) != 0)
            {
                Console.WriteLine($"'(int)1E-45f' was evaluated to '{(int)singleOneIncrementAboveByteMinValue}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullIncrementAboveByteMinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveByteMinValueCastToInt32IsFoldedCorrectly()
        {
            float singleOneFullIncrementAboveByteMinValue = 1f;

            if (BreakUpFlow())
                return;

            if (checked((int)singleOneFullIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(int)1f' was evaluated to '{(int)singleOneFullIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmSingleOneDecrementUnderByteMaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderByteMaxValueCastToInt32IsFoldedCorrectly()
        {
            float singleOneDecrementUnderByteMaxValue = 254.99998f;

            if (BreakUpFlow())
                return;

            if (checked((int)singleOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(int)254.99998f' was evaluated to '{(int)singleOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullDecrementUnderByteMaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderByteMaxValueCastToInt32IsFoldedCorrectly()
        {
            float singleOneFullDecrementUnderByteMaxValue = 254f;

            if (BreakUpFlow())
                return;

            if (checked((int)singleOneFullDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(int)254f' was evaluated to '{(int)singleOneFullDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmSingleOneIncrementAboveByteMaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveByteMaxValueCastToInt32IsFoldedCorrectly()
        {
            float singleOneIncrementAboveByteMaxValue = 255.00002f;

            if (BreakUpFlow())
                return;

            if (checked((int)singleOneIncrementAboveByteMaxValue) != 255)
            {
                Console.WriteLine($"'(int)255.00002f' was evaluated to '{(int)singleOneIncrementAboveByteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullIncrementAboveByteMaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveByteMaxValueCastToInt32IsFoldedCorrectly()
        {
            float singleOneFullIncrementAboveByteMaxValue = 256f;

            if (BreakUpFlow())
                return;

            if (checked((int)singleOneFullIncrementAboveByteMaxValue) != 256)
            {
                Console.WriteLine($"'(int)256f' was evaluated to '{(int)singleOneFullIncrementAboveByteMaxValue}'. Expected: '256'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMinValueCastToInt32IsFoldedCorrectly()
        {
            float integerOneDecrementUnderByteMinValue = -1.0f;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneDecrementUnderByteMinValue) != -1)
            {
                Console.WriteLine($"'(int)-1.0f' was evaluated to '{(int)integerOneDecrementUnderByteMinValue}'. Expected: '-1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToInt32IsFoldedCorrectly()
        {
            float integerOneIncrementAboveByteMinValue = 1.0f;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(int)1.0f' was evaluated to '{(int)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMaxValueCastToInt32IsFoldedCorrectly()
        {
            float integerOneDecrementUnderByteMaxValue = 254.0f;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(int)254.0f' was evaluated to '{(int)integerOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMaxValueCastToInt32IsFoldedCorrectly()
        {
            float integerOneIncrementAboveByteMaxValue = 256.0f;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneIncrementAboveByteMaxValue) != 256)
            {
                Console.WriteLine($"'(int)256.0f' was evaluated to '{(int)integerOneIncrementAboveByteMaxValue}'. Expected: '256'.");
                _counter++;
            }
        }
        ConfirmInt16MinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MinValueCastToInt32IsFoldedCorrectly()
        {
            float int16MinValue = -32768.0f;

            if (BreakUpFlow())
                return;

            if (checked((int)int16MinValue) != -32768)
            {
                Console.WriteLine($"'(int)-32768.0f' was evaluated to '{(int)int16MinValue}'. Expected: '-32768'.");
                _counter++;
            }
        }
        ConfirmInt16MaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MaxValueCastToInt32IsFoldedCorrectly()
        {
            float int16MaxValue = 32767.0f;

            if (BreakUpFlow())
                return;

            if (checked((int)int16MaxValue) != 32767)
            {
                Console.WriteLine($"'(int)32767.0f' was evaluated to '{(int)int16MaxValue}'. Expected: '32767'.");
                _counter++;
            }
        }
        ConfirmSingleOneDecrementUnderInt16MinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderInt16MinValueCastToInt32IsFoldedCorrectly()
        {
            float singleOneDecrementUnderInt16MinValue = -32768.004f;

            if (BreakUpFlow())
                return;

            if (checked((int)singleOneDecrementUnderInt16MinValue) != -32768)
            {
                Console.WriteLine($"'(int)-32768.004f' was evaluated to '{(int)singleOneDecrementUnderInt16MinValue}'. Expected: '-32768'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullDecrementUnderInt16MinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderInt16MinValueCastToInt32IsFoldedCorrectly()
        {
            float singleOneFullDecrementUnderInt16MinValue = -32769f;

            if (BreakUpFlow())
                return;

            if (checked((int)singleOneFullDecrementUnderInt16MinValue) != -32769)
            {
                Console.WriteLine($"'(int)-32769f' was evaluated to '{(int)singleOneFullDecrementUnderInt16MinValue}'. Expected: '-32769'.");
                _counter++;
            }
        }
        ConfirmSingleOneIncrementAboveInt16MinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveInt16MinValueCastToInt32IsFoldedCorrectly()
        {
            float singleOneIncrementAboveInt16MinValue = -32767.998f;

            if (BreakUpFlow())
                return;

            if (checked((int)singleOneIncrementAboveInt16MinValue) != -32767)
            {
                Console.WriteLine($"'(int)-32767.998f' was evaluated to '{(int)singleOneIncrementAboveInt16MinValue}'. Expected: '-32767'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullIncrementAboveInt16MinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveInt16MinValueCastToInt32IsFoldedCorrectly()
        {
            float singleOneFullIncrementAboveInt16MinValue = -32767f;

            if (BreakUpFlow())
                return;

            if (checked((int)singleOneFullIncrementAboveInt16MinValue) != -32767)
            {
                Console.WriteLine($"'(int)-32767f' was evaluated to '{(int)singleOneFullIncrementAboveInt16MinValue}'. Expected: '-32767'.");
                _counter++;
            }
        }
        ConfirmSingleOneDecrementUnderInt16MaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderInt16MaxValueCastToInt32IsFoldedCorrectly()
        {
            float singleOneDecrementUnderInt16MaxValue = 32766.998f;

            if (BreakUpFlow())
                return;

            if (checked((int)singleOneDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(int)32766.998f' was evaluated to '{(int)singleOneDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullDecrementUnderInt16MaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderInt16MaxValueCastToInt32IsFoldedCorrectly()
        {
            float singleOneFullDecrementUnderInt16MaxValue = 32766f;

            if (BreakUpFlow())
                return;

            if (checked((int)singleOneFullDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(int)32766f' was evaluated to '{(int)singleOneFullDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
        ConfirmSingleOneIncrementAboveInt16MaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveInt16MaxValueCastToInt32IsFoldedCorrectly()
        {
            float singleOneIncrementAboveInt16MaxValue = 32767.002f;

            if (BreakUpFlow())
                return;

            if (checked((int)singleOneIncrementAboveInt16MaxValue) != 32767)
            {
                Console.WriteLine($"'(int)32767.002f' was evaluated to '{(int)singleOneIncrementAboveInt16MaxValue}'. Expected: '32767'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullIncrementAboveInt16MaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveInt16MaxValueCastToInt32IsFoldedCorrectly()
        {
            float singleOneFullIncrementAboveInt16MaxValue = 32768f;

            if (BreakUpFlow())
                return;

            if (checked((int)singleOneFullIncrementAboveInt16MaxValue) != 32768)
            {
                Console.WriteLine($"'(int)32768f' was evaluated to '{(int)singleOneFullIncrementAboveInt16MaxValue}'. Expected: '32768'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt16MinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MinValueCastToInt32IsFoldedCorrectly()
        {
            float integerOneDecrementUnderInt16MinValue = -32769.0f;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneDecrementUnderInt16MinValue) != -32769)
            {
                Console.WriteLine($"'(int)-32769.0f' was evaluated to '{(int)integerOneDecrementUnderInt16MinValue}'. Expected: '-32769'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt16MinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt16MinValueCastToInt32IsFoldedCorrectly()
        {
            float integerOneIncrementAboveInt16MinValue = -32767.0f;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneIncrementAboveInt16MinValue) != -32767)
            {
                Console.WriteLine($"'(int)-32767.0f' was evaluated to '{(int)integerOneIncrementAboveInt16MinValue}'. Expected: '-32767'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt16MaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MaxValueCastToInt32IsFoldedCorrectly()
        {
            float integerOneDecrementUnderInt16MaxValue = 32766.0f;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(int)32766.0f' was evaluated to '{(int)integerOneDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt16MaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt16MaxValueCastToInt32IsFoldedCorrectly()
        {
            float integerOneIncrementAboveInt16MaxValue = 32768.0f;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneIncrementAboveInt16MaxValue) != 32768)
            {
                Console.WriteLine($"'(int)32768.0f' was evaluated to '{(int)integerOneIncrementAboveInt16MaxValue}'. Expected: '32768'.");
                _counter++;
            }
        }
        ConfirmUInt16MaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt16MaxValueCastToInt32IsFoldedCorrectly()
        {
            float uInt16MaxValue = 65535.0f;

            if (BreakUpFlow())
                return;

            if (checked((int)uInt16MaxValue) != 65535)
            {
                Console.WriteLine($"'(int)65535.0f' was evaluated to '{(int)uInt16MaxValue}'. Expected: '65535'.");
                _counter++;
            }
        }
        ConfirmSingleOneDecrementUnderUInt16MaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderUInt16MaxValueCastToInt32IsFoldedCorrectly()
        {
            float singleOneDecrementUnderUInt16MaxValue = 65534.996f;

            if (BreakUpFlow())
                return;

            if (checked((int)singleOneDecrementUnderUInt16MaxValue) != 65534)
            {
                Console.WriteLine($"'(int)65534.996f' was evaluated to '{(int)singleOneDecrementUnderUInt16MaxValue}'. Expected: '65534'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullDecrementUnderUInt16MaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderUInt16MaxValueCastToInt32IsFoldedCorrectly()
        {
            float singleOneFullDecrementUnderUInt16MaxValue = 65534f;

            if (BreakUpFlow())
                return;

            if (checked((int)singleOneFullDecrementUnderUInt16MaxValue) != 65534)
            {
                Console.WriteLine($"'(int)65534f' was evaluated to '{(int)singleOneFullDecrementUnderUInt16MaxValue}'. Expected: '65534'.");
                _counter++;
            }
        }
        ConfirmSingleOneIncrementAboveUInt16MaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveUInt16MaxValueCastToInt32IsFoldedCorrectly()
        {
            float singleOneIncrementAboveUInt16MaxValue = 65535.004f;

            if (BreakUpFlow())
                return;

            if (checked((int)singleOneIncrementAboveUInt16MaxValue) != 65535)
            {
                Console.WriteLine($"'(int)65535.004f' was evaluated to '{(int)singleOneIncrementAboveUInt16MaxValue}'. Expected: '65535'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullIncrementAboveUInt16MaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveUInt16MaxValueCastToInt32IsFoldedCorrectly()
        {
            float singleOneFullIncrementAboveUInt16MaxValue = 65536f;

            if (BreakUpFlow())
                return;

            if (checked((int)singleOneFullIncrementAboveUInt16MaxValue) != 65536)
            {
                Console.WriteLine($"'(int)65536f' was evaluated to '{(int)singleOneFullIncrementAboveUInt16MaxValue}'. Expected: '65536'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderUInt16MaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt16MaxValueCastToInt32IsFoldedCorrectly()
        {
            float integerOneDecrementUnderUInt16MaxValue = 65534.0f;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneDecrementUnderUInt16MaxValue) != 65534)
            {
                Console.WriteLine($"'(int)65534.0f' was evaluated to '{(int)integerOneDecrementUnderUInt16MaxValue}'. Expected: '65534'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToInt32IsFoldedCorrectly()
        {
            float integerOneIncrementAboveUInt16MaxValue = 65536.0f;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneIncrementAboveUInt16MaxValue) != 65536)
            {
                Console.WriteLine($"'(int)65536.0f' was evaluated to '{(int)integerOneIncrementAboveUInt16MaxValue}'. Expected: '65536'.");
                _counter++;
            }
        }
        ConfirmInt32MinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MinValueCastToInt32IsFoldedCorrectly()
        {
            float int32MinValue = -2147483648.0f;

            if (BreakUpFlow())
                return;

            if (checked((int)int32MinValue) != -2147483648)
            {
                Console.WriteLine($"'(int)-2147483648.0f' was evaluated to '{(int)int32MinValue}'. Expected: '-2147483648'.");
                _counter++;
            }
        }
        ConfirmInt32MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MaxValueCastToInt32Overflows()
        {
            float from = 2147483647.0f;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)2147483647.0f)' did not throw OverflowException.");
        }
        ConfirmSingleOneDecrementUnderInt32MinValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderInt32MinValueCastToInt32Overflows()
        {
            float from = -2.147484E+09f;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)-2.147484E+09f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveInt32MinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveInt32MinValueCastToInt32IsFoldedCorrectly()
        {
            float singleOneIncrementAboveInt32MinValue = -2.1474835E+09f;

            if (BreakUpFlow())
                return;

            if (checked((int)singleOneIncrementAboveInt32MinValue) != -2147483520)
            {
                Console.WriteLine($"'(int)-2.1474835E+09f' was evaluated to '{(int)singleOneIncrementAboveInt32MinValue}'. Expected: '-2147483520'.");
                _counter++;
            }
        }
        ConfirmSingleOneDecrementUnderInt32MaxValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderInt32MaxValueCastToInt32IsFoldedCorrectly()
        {
            float singleOneDecrementUnderInt32MaxValue = 2.1474835E+09f;

            if (BreakUpFlow())
                return;

            if (checked((int)singleOneDecrementUnderInt32MaxValue) != 2147483520)
            {
                Console.WriteLine($"'(int)2.1474835E+09f' was evaluated to '{(int)singleOneDecrementUnderInt32MaxValue}'. Expected: '2147483520'.");
                _counter++;
            }
        }
        ConfirmSingleOneIncrementAboveInt32MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveInt32MaxValueCastToInt32Overflows()
        {
            float from = 2.147484E+09f;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)2.147484E+09f)' did not throw OverflowException.");
        }
        ConfirmSingleOneFullIncrementAboveInt32MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveInt32MaxValueCastToInt32Overflows()
        {
            float from = 2.1474836E+09f;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)2.1474836E+09f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt32MinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt32MinValueCastToInt32IsFoldedCorrectly()
        {
            float integerOneDecrementUnderInt32MinValue = -2147483649.0f;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneDecrementUnderInt32MinValue) != -2147483648)
            {
                Console.WriteLine($"'(int)-2147483649.0f' was evaluated to '{(int)integerOneDecrementUnderInt32MinValue}'. Expected: '-2147483648'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt32MinValueCastToInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt32MinValueCastToInt32IsFoldedCorrectly()
        {
            float integerOneIncrementAboveInt32MinValue = -2147483647.0f;

            if (BreakUpFlow())
                return;

            if (checked((int)integerOneIncrementAboveInt32MinValue) != -2147483648)
            {
                Console.WriteLine($"'(int)-2147483647.0f' was evaluated to '{(int)integerOneIncrementAboveInt32MinValue}'. Expected: '-2147483648'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt32MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt32MaxValueCastToInt32Overflows()
        {
            float from = 2147483646.0f;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)2147483646.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt32MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt32MaxValueCastToInt32Overflows()
        {
            float from = 2147483648.0f;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)2147483648.0f)' did not throw OverflowException.");
        }
        ConfirmUInt32MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32MaxValueCastToInt32Overflows()
        {
            float from = 4294967295.0f;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)4294967295.0f)' did not throw OverflowException.");
        }
        ConfirmSingleOneDecrementUnderUInt32MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderUInt32MaxValueCastToInt32Overflows()
        {
            float from = 4.294967E+09f;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)4.294967E+09f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveUInt32MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveUInt32MaxValueCastToInt32Overflows()
        {
            float from = 4.294968E+09f;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)4.294968E+09f)' did not throw OverflowException.");
        }
        ConfirmSingleOneFullIncrementAboveUInt32MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveUInt32MaxValueCastToInt32Overflows()
        {
            float from = 4.2949673E+09f;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)4.2949673E+09f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderUInt32MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt32MaxValueCastToInt32Overflows()
        {
            float from = 4294967294.0f;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)4294967294.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveUInt32MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt32MaxValueCastToInt32Overflows()
        {
            float from = 4294967296.0f;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)4294967296.0f)' did not throw OverflowException.");
        }
        ConfirmInt64MinValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MinValueCastToInt32Overflows()
        {
            float from = -9223372036854775808.0f;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)-9223372036854775808.0f)' did not throw OverflowException.");
        }
        ConfirmInt64MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MaxValueCastToInt32Overflows()
        {
            float from = 9223372036854775807.0f;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)9223372036854775807.0f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveInt64MinValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveInt64MinValueCastToInt32Overflows()
        {
            float from = -9.2233715E+18f;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)-9.2233715E+18f)' did not throw OverflowException.");
        }
        ConfirmSingleOneDecrementUnderInt64MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderInt64MaxValueCastToInt32Overflows()
        {
            float from = 9.2233715E+18f;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)9.2233715E+18f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveInt64MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveInt64MaxValueCastToInt32Overflows()
        {
            float from = 9.223373E+18f;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)9.223373E+18f)' did not throw OverflowException.");
        }
        ConfirmSingleOneFullIncrementAboveInt64MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveInt64MaxValueCastToInt32Overflows()
        {
            float from = 9.223372E+18f;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)9.223372E+18f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt64MinValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt64MinValueCastToInt32Overflows()
        {
            float from = -9223372036854775807.0f;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)-9223372036854775807.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt64MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt64MaxValueCastToInt32Overflows()
        {
            float from = 9223372036854775806.0f;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)9223372036854775806.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt64MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt64MaxValueCastToInt32Overflows()
        {
            float from = 9223372036854775808.0f;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)9223372036854775808.0f)' did not throw OverflowException.");
        }
        ConfirmUInt64MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64MaxValueCastToInt32Overflows()
        {
            float from = 18446744073709551615.0f;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)18446744073709551615.0f)' did not throw OverflowException.");
        }
        ConfirmSingleOneDecrementUnderUInt64MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderUInt64MaxValueCastToInt32Overflows()
        {
            float from = 1.8446743E+19f;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)1.8446743E+19f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderUInt64MaxValueCastToInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt64MaxValueCastToInt32Overflows()
        {
            float from = 18446744073709551614.0f;
            _counter++;
            try
            {
                _ = checked((int)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((int)18446744073709551614.0f)' did not throw OverflowException.");
        }
    }

    private static void TestCastingSingleToUInt32()
    {
        ConfirmIntegerZeroCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToUInt32IsFoldedCorrectly()
        {
            float integerZero = 0.0f;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerZero) != 0)
            {
                Console.WriteLine($"'(uint)0.0f' was evaluated to '{(uint)integerZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmFloatMinusZeroCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmFloatMinusZeroCastToUInt32IsFoldedCorrectly()
        {
            float floatMinusZero = -0f;

            if (BreakUpFlow())
                return;

            if (checked((uint)floatMinusZero) != 0)
            {
                Console.WriteLine($"'(uint)-0f' was evaluated to '{(uint)floatMinusZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmFloatZeroCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmFloatZeroCastToUInt32IsFoldedCorrectly()
        {
            float floatZero = 0f;

            if (BreakUpFlow())
                return;

            if (checked((uint)floatZero) != 0)
            {
                Console.WriteLine($"'(uint)0f' was evaluated to '{(uint)floatZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmFloatHalfOfMaxValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmFloatHalfOfMaxValueCastToUInt32Overflows()
        {
            float from = 1.7014117E+38f;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)1.7014117E+38f)' did not throw OverflowException.");
        }
        ConfirmFloatHalfOfMinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmFloatHalfOfMinValueCastToUInt32Overflows()
        {
            float from = -1.7014117E+38f;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-1.7014117E+38f)' did not throw OverflowException.");
        }
        ConfirmSingleMinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleMinValueCastToUInt32Overflows()
        {
            float from = -3.4028235E+38f;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-3.4028235E+38f)' did not throw OverflowException.");
        }
        ConfirmSingleMaxValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleMaxValueCastToUInt32Overflows()
        {
            float from = 3.4028235E+38f;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)3.4028235E+38f)' did not throw OverflowException.");
        }
        ConfirmSByteMinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMinValueCastToUInt32Overflows()
        {
            float from = -128.0f;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-128.0f)' did not throw OverflowException.");
        }
        ConfirmSByteMaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToUInt32IsFoldedCorrectly()
        {
            float sByteMaxValue = 127.0f;

            if (BreakUpFlow())
                return;

            if (checked((uint)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(uint)127.0f' was evaluated to '{(uint)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmSingleOneDecrementUnderSByteMinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderSByteMinValueCastToUInt32Overflows()
        {
            float from = -128.00002f;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-128.00002f)' did not throw OverflowException.");
        }
        ConfirmSingleOneFullDecrementUnderSByteMinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderSByteMinValueCastToUInt32Overflows()
        {
            float from = -129f;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-129f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveSByteMinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveSByteMinValueCastToUInt32Overflows()
        {
            float from = -127.99999f;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-127.99999f)' did not throw OverflowException.");
        }
        ConfirmSingleOneFullIncrementAboveSByteMinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveSByteMinValueCastToUInt32Overflows()
        {
            float from = -127f;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-127f)' did not throw OverflowException.");
        }
        ConfirmSingleOneDecrementUnderSByteMaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderSByteMaxValueCastToUInt32IsFoldedCorrectly()
        {
            float singleOneDecrementUnderSByteMaxValue = 126.99999f;

            if (BreakUpFlow())
                return;

            if (checked((uint)singleOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(uint)126.99999f' was evaluated to '{(uint)singleOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullDecrementUnderSByteMaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderSByteMaxValueCastToUInt32IsFoldedCorrectly()
        {
            float singleOneFullDecrementUnderSByteMaxValue = 126f;

            if (BreakUpFlow())
                return;

            if (checked((uint)singleOneFullDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(uint)126f' was evaluated to '{(uint)singleOneFullDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmSingleOneIncrementAboveSByteMaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveSByteMaxValueCastToUInt32IsFoldedCorrectly()
        {
            float singleOneIncrementAboveSByteMaxValue = 127.00001f;

            if (BreakUpFlow())
                return;

            if (checked((uint)singleOneIncrementAboveSByteMaxValue) != 127)
            {
                Console.WriteLine($"'(uint)127.00001f' was evaluated to '{(uint)singleOneIncrementAboveSByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullIncrementAboveSByteMaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveSByteMaxValueCastToUInt32IsFoldedCorrectly()
        {
            float singleOneFullIncrementAboveSByteMaxValue = 128f;

            if (BreakUpFlow())
                return;

            if (checked((uint)singleOneFullIncrementAboveSByteMaxValue) != 128)
            {
                Console.WriteLine($"'(uint)128f' was evaluated to '{(uint)singleOneFullIncrementAboveSByteMaxValue}'. Expected: '128'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMinValueCastToUInt32Overflows()
        {
            float from = -129.0f;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-129.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveSByteMinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMinValueCastToUInt32Overflows()
        {
            float from = -127.0f;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-127.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToUInt32IsFoldedCorrectly()
        {
            float integerOneDecrementUnderSByteMaxValue = 126.0f;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(uint)126.0f' was evaluated to '{(uint)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMaxValueCastToUInt32IsFoldedCorrectly()
        {
            float integerOneIncrementAboveSByteMaxValue = 128.0f;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneIncrementAboveSByteMaxValue) != 128)
            {
                Console.WriteLine($"'(uint)128.0f' was evaluated to '{(uint)integerOneIncrementAboveSByteMaxValue}'. Expected: '128'.");
                _counter++;
            }
        }
        ConfirmByteMaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmByteMaxValueCastToUInt32IsFoldedCorrectly()
        {
            float byteMaxValue = 255.0f;

            if (BreakUpFlow())
                return;

            if (checked((uint)byteMaxValue) != 255)
            {
                Console.WriteLine($"'(uint)255.0f' was evaluated to '{(uint)byteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmSingleOneDecrementUnderByteMinValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderByteMinValueCastToUInt32IsFoldedCorrectly()
        {
            float singleOneDecrementUnderByteMinValue = -1E-45f;

            if (BreakUpFlow())
                return;

            if (checked((uint)singleOneDecrementUnderByteMinValue) != 0)
            {
                Console.WriteLine($"'(uint)-1E-45f' was evaluated to '{(uint)singleOneDecrementUnderByteMinValue}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullDecrementUnderByteMinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderByteMinValueCastToUInt32Overflows()
        {
            float from = -1f;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-1f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveByteMinValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveByteMinValueCastToUInt32IsFoldedCorrectly()
        {
            float singleOneIncrementAboveByteMinValue = 1E-45f;

            if (BreakUpFlow())
                return;

            if (checked((uint)singleOneIncrementAboveByteMinValue) != 0)
            {
                Console.WriteLine($"'(uint)1E-45f' was evaluated to '{(uint)singleOneIncrementAboveByteMinValue}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullIncrementAboveByteMinValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveByteMinValueCastToUInt32IsFoldedCorrectly()
        {
            float singleOneFullIncrementAboveByteMinValue = 1f;

            if (BreakUpFlow())
                return;

            if (checked((uint)singleOneFullIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(uint)1f' was evaluated to '{(uint)singleOneFullIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmSingleOneDecrementUnderByteMaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderByteMaxValueCastToUInt32IsFoldedCorrectly()
        {
            float singleOneDecrementUnderByteMaxValue = 254.99998f;

            if (BreakUpFlow())
                return;

            if (checked((uint)singleOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(uint)254.99998f' was evaluated to '{(uint)singleOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullDecrementUnderByteMaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderByteMaxValueCastToUInt32IsFoldedCorrectly()
        {
            float singleOneFullDecrementUnderByteMaxValue = 254f;

            if (BreakUpFlow())
                return;

            if (checked((uint)singleOneFullDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(uint)254f' was evaluated to '{(uint)singleOneFullDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmSingleOneIncrementAboveByteMaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveByteMaxValueCastToUInt32IsFoldedCorrectly()
        {
            float singleOneIncrementAboveByteMaxValue = 255.00002f;

            if (BreakUpFlow())
                return;

            if (checked((uint)singleOneIncrementAboveByteMaxValue) != 255)
            {
                Console.WriteLine($"'(uint)255.00002f' was evaluated to '{(uint)singleOneIncrementAboveByteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullIncrementAboveByteMaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveByteMaxValueCastToUInt32IsFoldedCorrectly()
        {
            float singleOneFullIncrementAboveByteMaxValue = 256f;

            if (BreakUpFlow())
                return;

            if (checked((uint)singleOneFullIncrementAboveByteMaxValue) != 256)
            {
                Console.WriteLine($"'(uint)256f' was evaluated to '{(uint)singleOneFullIncrementAboveByteMaxValue}'. Expected: '256'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMinValueCastToUInt32Overflows()
        {
            float from = -1.0f;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-1.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToUInt32IsFoldedCorrectly()
        {
            float integerOneIncrementAboveByteMinValue = 1.0f;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(uint)1.0f' was evaluated to '{(uint)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMaxValueCastToUInt32IsFoldedCorrectly()
        {
            float integerOneDecrementUnderByteMaxValue = 254.0f;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(uint)254.0f' was evaluated to '{(uint)integerOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMaxValueCastToUInt32IsFoldedCorrectly()
        {
            float integerOneIncrementAboveByteMaxValue = 256.0f;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneIncrementAboveByteMaxValue) != 256)
            {
                Console.WriteLine($"'(uint)256.0f' was evaluated to '{(uint)integerOneIncrementAboveByteMaxValue}'. Expected: '256'.");
                _counter++;
            }
        }
        ConfirmInt16MinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MinValueCastToUInt32Overflows()
        {
            float from = -32768.0f;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-32768.0f)' did not throw OverflowException.");
        }
        ConfirmInt16MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MaxValueCastToUInt32IsFoldedCorrectly()
        {
            float int16MaxValue = 32767.0f;

            if (BreakUpFlow())
                return;

            if (checked((uint)int16MaxValue) != 32767)
            {
                Console.WriteLine($"'(uint)32767.0f' was evaluated to '{(uint)int16MaxValue}'. Expected: '32767'.");
                _counter++;
            }
        }
        ConfirmSingleOneDecrementUnderInt16MinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderInt16MinValueCastToUInt32Overflows()
        {
            float from = -32768.004f;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-32768.004f)' did not throw OverflowException.");
        }
        ConfirmSingleOneFullDecrementUnderInt16MinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderInt16MinValueCastToUInt32Overflows()
        {
            float from = -32769f;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-32769f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveInt16MinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveInt16MinValueCastToUInt32Overflows()
        {
            float from = -32767.998f;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-32767.998f)' did not throw OverflowException.");
        }
        ConfirmSingleOneFullIncrementAboveInt16MinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveInt16MinValueCastToUInt32Overflows()
        {
            float from = -32767f;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-32767f)' did not throw OverflowException.");
        }
        ConfirmSingleOneDecrementUnderInt16MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderInt16MaxValueCastToUInt32IsFoldedCorrectly()
        {
            float singleOneDecrementUnderInt16MaxValue = 32766.998f;

            if (BreakUpFlow())
                return;

            if (checked((uint)singleOneDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(uint)32766.998f' was evaluated to '{(uint)singleOneDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullDecrementUnderInt16MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderInt16MaxValueCastToUInt32IsFoldedCorrectly()
        {
            float singleOneFullDecrementUnderInt16MaxValue = 32766f;

            if (BreakUpFlow())
                return;

            if (checked((uint)singleOneFullDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(uint)32766f' was evaluated to '{(uint)singleOneFullDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
        ConfirmSingleOneIncrementAboveInt16MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveInt16MaxValueCastToUInt32IsFoldedCorrectly()
        {
            float singleOneIncrementAboveInt16MaxValue = 32767.002f;

            if (BreakUpFlow())
                return;

            if (checked((uint)singleOneIncrementAboveInt16MaxValue) != 32767)
            {
                Console.WriteLine($"'(uint)32767.002f' was evaluated to '{(uint)singleOneIncrementAboveInt16MaxValue}'. Expected: '32767'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullIncrementAboveInt16MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveInt16MaxValueCastToUInt32IsFoldedCorrectly()
        {
            float singleOneFullIncrementAboveInt16MaxValue = 32768f;

            if (BreakUpFlow())
                return;

            if (checked((uint)singleOneFullIncrementAboveInt16MaxValue) != 32768)
            {
                Console.WriteLine($"'(uint)32768f' was evaluated to '{(uint)singleOneFullIncrementAboveInt16MaxValue}'. Expected: '32768'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt16MinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MinValueCastToUInt32Overflows()
        {
            float from = -32769.0f;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-32769.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt16MinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt16MinValueCastToUInt32Overflows()
        {
            float from = -32767.0f;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-32767.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt16MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MaxValueCastToUInt32IsFoldedCorrectly()
        {
            float integerOneDecrementUnderInt16MaxValue = 32766.0f;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(uint)32766.0f' was evaluated to '{(uint)integerOneDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt16MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt16MaxValueCastToUInt32IsFoldedCorrectly()
        {
            float integerOneIncrementAboveInt16MaxValue = 32768.0f;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneIncrementAboveInt16MaxValue) != 32768)
            {
                Console.WriteLine($"'(uint)32768.0f' was evaluated to '{(uint)integerOneIncrementAboveInt16MaxValue}'. Expected: '32768'.");
                _counter++;
            }
        }
        ConfirmUInt16MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt16MaxValueCastToUInt32IsFoldedCorrectly()
        {
            float uInt16MaxValue = 65535.0f;

            if (BreakUpFlow())
                return;

            if (checked((uint)uInt16MaxValue) != 65535)
            {
                Console.WriteLine($"'(uint)65535.0f' was evaluated to '{(uint)uInt16MaxValue}'. Expected: '65535'.");
                _counter++;
            }
        }
        ConfirmSingleOneDecrementUnderUInt16MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderUInt16MaxValueCastToUInt32IsFoldedCorrectly()
        {
            float singleOneDecrementUnderUInt16MaxValue = 65534.996f;

            if (BreakUpFlow())
                return;

            if (checked((uint)singleOneDecrementUnderUInt16MaxValue) != 65534)
            {
                Console.WriteLine($"'(uint)65534.996f' was evaluated to '{(uint)singleOneDecrementUnderUInt16MaxValue}'. Expected: '65534'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullDecrementUnderUInt16MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderUInt16MaxValueCastToUInt32IsFoldedCorrectly()
        {
            float singleOneFullDecrementUnderUInt16MaxValue = 65534f;

            if (BreakUpFlow())
                return;

            if (checked((uint)singleOneFullDecrementUnderUInt16MaxValue) != 65534)
            {
                Console.WriteLine($"'(uint)65534f' was evaluated to '{(uint)singleOneFullDecrementUnderUInt16MaxValue}'. Expected: '65534'.");
                _counter++;
            }
        }
        ConfirmSingleOneIncrementAboveUInt16MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveUInt16MaxValueCastToUInt32IsFoldedCorrectly()
        {
            float singleOneIncrementAboveUInt16MaxValue = 65535.004f;

            if (BreakUpFlow())
                return;

            if (checked((uint)singleOneIncrementAboveUInt16MaxValue) != 65535)
            {
                Console.WriteLine($"'(uint)65535.004f' was evaluated to '{(uint)singleOneIncrementAboveUInt16MaxValue}'. Expected: '65535'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullIncrementAboveUInt16MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveUInt16MaxValueCastToUInt32IsFoldedCorrectly()
        {
            float singleOneFullIncrementAboveUInt16MaxValue = 65536f;

            if (BreakUpFlow())
                return;

            if (checked((uint)singleOneFullIncrementAboveUInt16MaxValue) != 65536)
            {
                Console.WriteLine($"'(uint)65536f' was evaluated to '{(uint)singleOneFullIncrementAboveUInt16MaxValue}'. Expected: '65536'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderUInt16MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt16MaxValueCastToUInt32IsFoldedCorrectly()
        {
            float integerOneDecrementUnderUInt16MaxValue = 65534.0f;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneDecrementUnderUInt16MaxValue) != 65534)
            {
                Console.WriteLine($"'(uint)65534.0f' was evaluated to '{(uint)integerOneDecrementUnderUInt16MaxValue}'. Expected: '65534'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToUInt32IsFoldedCorrectly()
        {
            float integerOneIncrementAboveUInt16MaxValue = 65536.0f;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneIncrementAboveUInt16MaxValue) != 65536)
            {
                Console.WriteLine($"'(uint)65536.0f' was evaluated to '{(uint)integerOneIncrementAboveUInt16MaxValue}'. Expected: '65536'.");
                _counter++;
            }
        }
        ConfirmInt32MinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MinValueCastToUInt32Overflows()
        {
            float from = -2147483648.0f;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-2147483648.0f)' did not throw OverflowException.");
        }
        ConfirmInt32MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MaxValueCastToUInt32IsFoldedCorrectly()
        {
            float int32MaxValue = 2147483647.0f;

            if (BreakUpFlow())
                return;

            if (checked((uint)int32MaxValue) != 2147483648)
            {
                Console.WriteLine($"'(uint)2147483647.0f' was evaluated to '{(uint)int32MaxValue}'. Expected: '2147483648'.");
                _counter++;
            }
        }
        ConfirmSingleOneDecrementUnderInt32MinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderInt32MinValueCastToUInt32Overflows()
        {
            float from = -2.147484E+09f;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-2.147484E+09f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveInt32MinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveInt32MinValueCastToUInt32Overflows()
        {
            float from = -2.1474835E+09f;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-2.1474835E+09f)' did not throw OverflowException.");
        }
        ConfirmSingleOneDecrementUnderInt32MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderInt32MaxValueCastToUInt32IsFoldedCorrectly()
        {
            float singleOneDecrementUnderInt32MaxValue = 2.1474835E+09f;

            if (BreakUpFlow())
                return;

            if (checked((uint)singleOneDecrementUnderInt32MaxValue) != 2147483520)
            {
                Console.WriteLine($"'(uint)2.1474835E+09f' was evaluated to '{(uint)singleOneDecrementUnderInt32MaxValue}'. Expected: '2147483520'.");
                _counter++;
            }
        }
        ConfirmSingleOneIncrementAboveInt32MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveInt32MaxValueCastToUInt32IsFoldedCorrectly()
        {
            float singleOneIncrementAboveInt32MaxValue = 2.147484E+09f;

            if (BreakUpFlow())
                return;

            if (checked((uint)singleOneIncrementAboveInt32MaxValue) != 2147483904)
            {
                Console.WriteLine($"'(uint)2.147484E+09f' was evaluated to '{(uint)singleOneIncrementAboveInt32MaxValue}'. Expected: '2147483904'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullIncrementAboveInt32MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveInt32MaxValueCastToUInt32IsFoldedCorrectly()
        {
            float singleOneFullIncrementAboveInt32MaxValue = 2.1474836E+09f;

            if (BreakUpFlow())
                return;

            if (checked((uint)singleOneFullIncrementAboveInt32MaxValue) != 2147483648)
            {
                Console.WriteLine($"'(uint)2.1474836E+09f' was evaluated to '{(uint)singleOneFullIncrementAboveInt32MaxValue}'. Expected: '2147483648'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt32MinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt32MinValueCastToUInt32Overflows()
        {
            float from = -2147483649.0f;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-2147483649.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt32MinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt32MinValueCastToUInt32Overflows()
        {
            float from = -2147483647.0f;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-2147483647.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt32MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt32MaxValueCastToUInt32IsFoldedCorrectly()
        {
            float integerOneDecrementUnderInt32MaxValue = 2147483646.0f;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneDecrementUnderInt32MaxValue) != 2147483648)
            {
                Console.WriteLine($"'(uint)2147483646.0f' was evaluated to '{(uint)integerOneDecrementUnderInt32MaxValue}'. Expected: '2147483648'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt32MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt32MaxValueCastToUInt32IsFoldedCorrectly()
        {
            float integerOneIncrementAboveInt32MaxValue = 2147483648.0f;

            if (BreakUpFlow())
                return;

            if (checked((uint)integerOneIncrementAboveInt32MaxValue) != 2147483648)
            {
                Console.WriteLine($"'(uint)2147483648.0f' was evaluated to '{(uint)integerOneIncrementAboveInt32MaxValue}'. Expected: '2147483648'.");
                _counter++;
            }
        }
        ConfirmUInt32MaxValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32MaxValueCastToUInt32Overflows()
        {
            float from = 4294967295.0f;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)4294967295.0f)' did not throw OverflowException.");
        }
        ConfirmSingleOneDecrementUnderUInt32MaxValueCastToUInt32IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderUInt32MaxValueCastToUInt32IsFoldedCorrectly()
        {
            float singleOneDecrementUnderUInt32MaxValue = 4.294967E+09f;

            if (BreakUpFlow())
                return;

            if (checked((uint)singleOneDecrementUnderUInt32MaxValue) != 4294967040)
            {
                Console.WriteLine($"'(uint)4.294967E+09f' was evaluated to '{(uint)singleOneDecrementUnderUInt32MaxValue}'. Expected: '4294967040'.");
                _counter++;
            }
        }
        ConfirmSingleOneIncrementAboveUInt32MaxValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveUInt32MaxValueCastToUInt32Overflows()
        {
            float from = 4.294968E+09f;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)4.294968E+09f)' did not throw OverflowException.");
        }
        ConfirmSingleOneFullIncrementAboveUInt32MaxValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveUInt32MaxValueCastToUInt32Overflows()
        {
            float from = 4.2949673E+09f;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)4.2949673E+09f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderUInt32MaxValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt32MaxValueCastToUInt32Overflows()
        {
            float from = 4294967294.0f;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)4294967294.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveUInt32MaxValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt32MaxValueCastToUInt32Overflows()
        {
            float from = 4294967296.0f;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)4294967296.0f)' did not throw OverflowException.");
        }
        ConfirmInt64MinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MinValueCastToUInt32Overflows()
        {
            float from = -9223372036854775808.0f;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-9223372036854775808.0f)' did not throw OverflowException.");
        }
        ConfirmInt64MaxValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MaxValueCastToUInt32Overflows()
        {
            float from = 9223372036854775807.0f;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)9223372036854775807.0f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveInt64MinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveInt64MinValueCastToUInt32Overflows()
        {
            float from = -9.2233715E+18f;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-9.2233715E+18f)' did not throw OverflowException.");
        }
        ConfirmSingleOneDecrementUnderInt64MaxValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderInt64MaxValueCastToUInt32Overflows()
        {
            float from = 9.2233715E+18f;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)9.2233715E+18f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveInt64MaxValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveInt64MaxValueCastToUInt32Overflows()
        {
            float from = 9.223373E+18f;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)9.223373E+18f)' did not throw OverflowException.");
        }
        ConfirmSingleOneFullIncrementAboveInt64MaxValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveInt64MaxValueCastToUInt32Overflows()
        {
            float from = 9.223372E+18f;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)9.223372E+18f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt64MinValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt64MinValueCastToUInt32Overflows()
        {
            float from = -9223372036854775807.0f;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)-9223372036854775807.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt64MaxValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt64MaxValueCastToUInt32Overflows()
        {
            float from = 9223372036854775806.0f;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)9223372036854775806.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt64MaxValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt64MaxValueCastToUInt32Overflows()
        {
            float from = 9223372036854775808.0f;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)9223372036854775808.0f)' did not throw OverflowException.");
        }
        ConfirmUInt64MaxValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64MaxValueCastToUInt32Overflows()
        {
            float from = 18446744073709551615.0f;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)18446744073709551615.0f)' did not throw OverflowException.");
        }
        ConfirmSingleOneDecrementUnderUInt64MaxValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderUInt64MaxValueCastToUInt32Overflows()
        {
            float from = 1.8446743E+19f;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)1.8446743E+19f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderUInt64MaxValueCastToUInt32Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt64MaxValueCastToUInt32Overflows()
        {
            float from = 18446744073709551614.0f;
            _counter++;
            try
            {
                _ = checked((uint)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((uint)18446744073709551614.0f)' did not throw OverflowException.");
        }
    }

    private static void TestCastingSingleToInt64()
    {
        ConfirmIntegerZeroCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToInt64IsFoldedCorrectly()
        {
            float integerZero = 0.0f;

            if (BreakUpFlow())
                return;

            if (checked((long)integerZero) != 0)
            {
                Console.WriteLine($"'(long)0.0f' was evaluated to '{(long)integerZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmFloatMinusZeroCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmFloatMinusZeroCastToInt64IsFoldedCorrectly()
        {
            float floatMinusZero = -0f;

            if (BreakUpFlow())
                return;

            if (checked((long)floatMinusZero) != 0)
            {
                Console.WriteLine($"'(long)-0f' was evaluated to '{(long)floatMinusZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmFloatZeroCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmFloatZeroCastToInt64IsFoldedCorrectly()
        {
            float floatZero = 0f;

            if (BreakUpFlow())
                return;

            if (checked((long)floatZero) != 0)
            {
                Console.WriteLine($"'(long)0f' was evaluated to '{(long)floatZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmFloatHalfOfMaxValueCastToInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmFloatHalfOfMaxValueCastToInt64Overflows()
        {
            float from = 1.7014117E+38f;
            _counter++;
            try
            {
                _ = checked((long)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((long)1.7014117E+38f)' did not throw OverflowException.");
        }
        ConfirmFloatHalfOfMinValueCastToInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmFloatHalfOfMinValueCastToInt64Overflows()
        {
            float from = -1.7014117E+38f;
            _counter++;
            try
            {
                _ = checked((long)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((long)-1.7014117E+38f)' did not throw OverflowException.");
        }
        ConfirmSingleMinValueCastToInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleMinValueCastToInt64Overflows()
        {
            float from = -3.4028235E+38f;
            _counter++;
            try
            {
                _ = checked((long)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((long)-3.4028235E+38f)' did not throw OverflowException.");
        }
        ConfirmSingleMaxValueCastToInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleMaxValueCastToInt64Overflows()
        {
            float from = 3.4028235E+38f;
            _counter++;
            try
            {
                _ = checked((long)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((long)3.4028235E+38f)' did not throw OverflowException.");
        }
        ConfirmSByteMinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMinValueCastToInt64IsFoldedCorrectly()
        {
            float sByteMinValue = -128.0f;

            if (BreakUpFlow())
                return;

            if (checked((long)sByteMinValue) != -128)
            {
                Console.WriteLine($"'(long)-128.0f' was evaluated to '{(long)sByteMinValue}'. Expected: '-128'.");
                _counter++;
            }
        }
        ConfirmSByteMaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToInt64IsFoldedCorrectly()
        {
            float sByteMaxValue = 127.0f;

            if (BreakUpFlow())
                return;

            if (checked((long)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(long)127.0f' was evaluated to '{(long)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmSingleOneDecrementUnderSByteMinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderSByteMinValueCastToInt64IsFoldedCorrectly()
        {
            float singleOneDecrementUnderSByteMinValue = -128.00002f;

            if (BreakUpFlow())
                return;

            if (checked((long)singleOneDecrementUnderSByteMinValue) != -128)
            {
                Console.WriteLine($"'(long)-128.00002f' was evaluated to '{(long)singleOneDecrementUnderSByteMinValue}'. Expected: '-128'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullDecrementUnderSByteMinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderSByteMinValueCastToInt64IsFoldedCorrectly()
        {
            float singleOneFullDecrementUnderSByteMinValue = -129f;

            if (BreakUpFlow())
                return;

            if (checked((long)singleOneFullDecrementUnderSByteMinValue) != -129)
            {
                Console.WriteLine($"'(long)-129f' was evaluated to '{(long)singleOneFullDecrementUnderSByteMinValue}'. Expected: '-129'.");
                _counter++;
            }
        }
        ConfirmSingleOneIncrementAboveSByteMinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveSByteMinValueCastToInt64IsFoldedCorrectly()
        {
            float singleOneIncrementAboveSByteMinValue = -127.99999f;

            if (BreakUpFlow())
                return;

            if (checked((long)singleOneIncrementAboveSByteMinValue) != -127)
            {
                Console.WriteLine($"'(long)-127.99999f' was evaluated to '{(long)singleOneIncrementAboveSByteMinValue}'. Expected: '-127'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullIncrementAboveSByteMinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveSByteMinValueCastToInt64IsFoldedCorrectly()
        {
            float singleOneFullIncrementAboveSByteMinValue = -127f;

            if (BreakUpFlow())
                return;

            if (checked((long)singleOneFullIncrementAboveSByteMinValue) != -127)
            {
                Console.WriteLine($"'(long)-127f' was evaluated to '{(long)singleOneFullIncrementAboveSByteMinValue}'. Expected: '-127'.");
                _counter++;
            }
        }
        ConfirmSingleOneDecrementUnderSByteMaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderSByteMaxValueCastToInt64IsFoldedCorrectly()
        {
            float singleOneDecrementUnderSByteMaxValue = 126.99999f;

            if (BreakUpFlow())
                return;

            if (checked((long)singleOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(long)126.99999f' was evaluated to '{(long)singleOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullDecrementUnderSByteMaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderSByteMaxValueCastToInt64IsFoldedCorrectly()
        {
            float singleOneFullDecrementUnderSByteMaxValue = 126f;

            if (BreakUpFlow())
                return;

            if (checked((long)singleOneFullDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(long)126f' was evaluated to '{(long)singleOneFullDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmSingleOneIncrementAboveSByteMaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveSByteMaxValueCastToInt64IsFoldedCorrectly()
        {
            float singleOneIncrementAboveSByteMaxValue = 127.00001f;

            if (BreakUpFlow())
                return;

            if (checked((long)singleOneIncrementAboveSByteMaxValue) != 127)
            {
                Console.WriteLine($"'(long)127.00001f' was evaluated to '{(long)singleOneIncrementAboveSByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullIncrementAboveSByteMaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveSByteMaxValueCastToInt64IsFoldedCorrectly()
        {
            float singleOneFullIncrementAboveSByteMaxValue = 128f;

            if (BreakUpFlow())
                return;

            if (checked((long)singleOneFullIncrementAboveSByteMaxValue) != 128)
            {
                Console.WriteLine($"'(long)128f' was evaluated to '{(long)singleOneFullIncrementAboveSByteMaxValue}'. Expected: '128'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMinValueCastToInt64IsFoldedCorrectly()
        {
            float integerOneDecrementUnderSByteMinValue = -129.0f;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneDecrementUnderSByteMinValue) != -129)
            {
                Console.WriteLine($"'(long)-129.0f' was evaluated to '{(long)integerOneDecrementUnderSByteMinValue}'. Expected: '-129'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMinValueCastToInt64IsFoldedCorrectly()
        {
            float integerOneIncrementAboveSByteMinValue = -127.0f;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneIncrementAboveSByteMinValue) != -127)
            {
                Console.WriteLine($"'(long)-127.0f' was evaluated to '{(long)integerOneIncrementAboveSByteMinValue}'. Expected: '-127'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToInt64IsFoldedCorrectly()
        {
            float integerOneDecrementUnderSByteMaxValue = 126.0f;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(long)126.0f' was evaluated to '{(long)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMaxValueCastToInt64IsFoldedCorrectly()
        {
            float integerOneIncrementAboveSByteMaxValue = 128.0f;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneIncrementAboveSByteMaxValue) != 128)
            {
                Console.WriteLine($"'(long)128.0f' was evaluated to '{(long)integerOneIncrementAboveSByteMaxValue}'. Expected: '128'.");
                _counter++;
            }
        }
        ConfirmByteMaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmByteMaxValueCastToInt64IsFoldedCorrectly()
        {
            float byteMaxValue = 255.0f;

            if (BreakUpFlow())
                return;

            if (checked((long)byteMaxValue) != 255)
            {
                Console.WriteLine($"'(long)255.0f' was evaluated to '{(long)byteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmSingleOneDecrementUnderByteMinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderByteMinValueCastToInt64IsFoldedCorrectly()
        {
            float singleOneDecrementUnderByteMinValue = -1E-45f;

            if (BreakUpFlow())
                return;

            if (checked((long)singleOneDecrementUnderByteMinValue) != 0)
            {
                Console.WriteLine($"'(long)-1E-45f' was evaluated to '{(long)singleOneDecrementUnderByteMinValue}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullDecrementUnderByteMinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderByteMinValueCastToInt64IsFoldedCorrectly()
        {
            float singleOneFullDecrementUnderByteMinValue = -1f;

            if (BreakUpFlow())
                return;

            if (checked((long)singleOneFullDecrementUnderByteMinValue) != -1)
            {
                Console.WriteLine($"'(long)-1f' was evaluated to '{(long)singleOneFullDecrementUnderByteMinValue}'. Expected: '-1'.");
                _counter++;
            }
        }
        ConfirmSingleOneIncrementAboveByteMinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveByteMinValueCastToInt64IsFoldedCorrectly()
        {
            float singleOneIncrementAboveByteMinValue = 1E-45f;

            if (BreakUpFlow())
                return;

            if (checked((long)singleOneIncrementAboveByteMinValue) != 0)
            {
                Console.WriteLine($"'(long)1E-45f' was evaluated to '{(long)singleOneIncrementAboveByteMinValue}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullIncrementAboveByteMinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveByteMinValueCastToInt64IsFoldedCorrectly()
        {
            float singleOneFullIncrementAboveByteMinValue = 1f;

            if (BreakUpFlow())
                return;

            if (checked((long)singleOneFullIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(long)1f' was evaluated to '{(long)singleOneFullIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmSingleOneDecrementUnderByteMaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderByteMaxValueCastToInt64IsFoldedCorrectly()
        {
            float singleOneDecrementUnderByteMaxValue = 254.99998f;

            if (BreakUpFlow())
                return;

            if (checked((long)singleOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(long)254.99998f' was evaluated to '{(long)singleOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullDecrementUnderByteMaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderByteMaxValueCastToInt64IsFoldedCorrectly()
        {
            float singleOneFullDecrementUnderByteMaxValue = 254f;

            if (BreakUpFlow())
                return;

            if (checked((long)singleOneFullDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(long)254f' was evaluated to '{(long)singleOneFullDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmSingleOneIncrementAboveByteMaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveByteMaxValueCastToInt64IsFoldedCorrectly()
        {
            float singleOneIncrementAboveByteMaxValue = 255.00002f;

            if (BreakUpFlow())
                return;

            if (checked((long)singleOneIncrementAboveByteMaxValue) != 255)
            {
                Console.WriteLine($"'(long)255.00002f' was evaluated to '{(long)singleOneIncrementAboveByteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullIncrementAboveByteMaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveByteMaxValueCastToInt64IsFoldedCorrectly()
        {
            float singleOneFullIncrementAboveByteMaxValue = 256f;

            if (BreakUpFlow())
                return;

            if (checked((long)singleOneFullIncrementAboveByteMaxValue) != 256)
            {
                Console.WriteLine($"'(long)256f' was evaluated to '{(long)singleOneFullIncrementAboveByteMaxValue}'. Expected: '256'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMinValueCastToInt64IsFoldedCorrectly()
        {
            float integerOneDecrementUnderByteMinValue = -1.0f;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneDecrementUnderByteMinValue) != -1)
            {
                Console.WriteLine($"'(long)-1.0f' was evaluated to '{(long)integerOneDecrementUnderByteMinValue}'. Expected: '-1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToInt64IsFoldedCorrectly()
        {
            float integerOneIncrementAboveByteMinValue = 1.0f;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(long)1.0f' was evaluated to '{(long)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMaxValueCastToInt64IsFoldedCorrectly()
        {
            float integerOneDecrementUnderByteMaxValue = 254.0f;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(long)254.0f' was evaluated to '{(long)integerOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMaxValueCastToInt64IsFoldedCorrectly()
        {
            float integerOneIncrementAboveByteMaxValue = 256.0f;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneIncrementAboveByteMaxValue) != 256)
            {
                Console.WriteLine($"'(long)256.0f' was evaluated to '{(long)integerOneIncrementAboveByteMaxValue}'. Expected: '256'.");
                _counter++;
            }
        }
        ConfirmInt16MinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MinValueCastToInt64IsFoldedCorrectly()
        {
            float int16MinValue = -32768.0f;

            if (BreakUpFlow())
                return;

            if (checked((long)int16MinValue) != -32768)
            {
                Console.WriteLine($"'(long)-32768.0f' was evaluated to '{(long)int16MinValue}'. Expected: '-32768'.");
                _counter++;
            }
        }
        ConfirmInt16MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MaxValueCastToInt64IsFoldedCorrectly()
        {
            float int16MaxValue = 32767.0f;

            if (BreakUpFlow())
                return;

            if (checked((long)int16MaxValue) != 32767)
            {
                Console.WriteLine($"'(long)32767.0f' was evaluated to '{(long)int16MaxValue}'. Expected: '32767'.");
                _counter++;
            }
        }
        ConfirmSingleOneDecrementUnderInt16MinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderInt16MinValueCastToInt64IsFoldedCorrectly()
        {
            float singleOneDecrementUnderInt16MinValue = -32768.004f;

            if (BreakUpFlow())
                return;

            if (checked((long)singleOneDecrementUnderInt16MinValue) != -32768)
            {
                Console.WriteLine($"'(long)-32768.004f' was evaluated to '{(long)singleOneDecrementUnderInt16MinValue}'. Expected: '-32768'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullDecrementUnderInt16MinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderInt16MinValueCastToInt64IsFoldedCorrectly()
        {
            float singleOneFullDecrementUnderInt16MinValue = -32769f;

            if (BreakUpFlow())
                return;

            if (checked((long)singleOneFullDecrementUnderInt16MinValue) != -32769)
            {
                Console.WriteLine($"'(long)-32769f' was evaluated to '{(long)singleOneFullDecrementUnderInt16MinValue}'. Expected: '-32769'.");
                _counter++;
            }
        }
        ConfirmSingleOneIncrementAboveInt16MinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveInt16MinValueCastToInt64IsFoldedCorrectly()
        {
            float singleOneIncrementAboveInt16MinValue = -32767.998f;

            if (BreakUpFlow())
                return;

            if (checked((long)singleOneIncrementAboveInt16MinValue) != -32767)
            {
                Console.WriteLine($"'(long)-32767.998f' was evaluated to '{(long)singleOneIncrementAboveInt16MinValue}'. Expected: '-32767'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullIncrementAboveInt16MinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveInt16MinValueCastToInt64IsFoldedCorrectly()
        {
            float singleOneFullIncrementAboveInt16MinValue = -32767f;

            if (BreakUpFlow())
                return;

            if (checked((long)singleOneFullIncrementAboveInt16MinValue) != -32767)
            {
                Console.WriteLine($"'(long)-32767f' was evaluated to '{(long)singleOneFullIncrementAboveInt16MinValue}'. Expected: '-32767'.");
                _counter++;
            }
        }
        ConfirmSingleOneDecrementUnderInt16MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderInt16MaxValueCastToInt64IsFoldedCorrectly()
        {
            float singleOneDecrementUnderInt16MaxValue = 32766.998f;

            if (BreakUpFlow())
                return;

            if (checked((long)singleOneDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(long)32766.998f' was evaluated to '{(long)singleOneDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullDecrementUnderInt16MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderInt16MaxValueCastToInt64IsFoldedCorrectly()
        {
            float singleOneFullDecrementUnderInt16MaxValue = 32766f;

            if (BreakUpFlow())
                return;

            if (checked((long)singleOneFullDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(long)32766f' was evaluated to '{(long)singleOneFullDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
        ConfirmSingleOneIncrementAboveInt16MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveInt16MaxValueCastToInt64IsFoldedCorrectly()
        {
            float singleOneIncrementAboveInt16MaxValue = 32767.002f;

            if (BreakUpFlow())
                return;

            if (checked((long)singleOneIncrementAboveInt16MaxValue) != 32767)
            {
                Console.WriteLine($"'(long)32767.002f' was evaluated to '{(long)singleOneIncrementAboveInt16MaxValue}'. Expected: '32767'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullIncrementAboveInt16MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveInt16MaxValueCastToInt64IsFoldedCorrectly()
        {
            float singleOneFullIncrementAboveInt16MaxValue = 32768f;

            if (BreakUpFlow())
                return;

            if (checked((long)singleOneFullIncrementAboveInt16MaxValue) != 32768)
            {
                Console.WriteLine($"'(long)32768f' was evaluated to '{(long)singleOneFullIncrementAboveInt16MaxValue}'. Expected: '32768'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt16MinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MinValueCastToInt64IsFoldedCorrectly()
        {
            float integerOneDecrementUnderInt16MinValue = -32769.0f;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneDecrementUnderInt16MinValue) != -32769)
            {
                Console.WriteLine($"'(long)-32769.0f' was evaluated to '{(long)integerOneDecrementUnderInt16MinValue}'. Expected: '-32769'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt16MinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt16MinValueCastToInt64IsFoldedCorrectly()
        {
            float integerOneIncrementAboveInt16MinValue = -32767.0f;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneIncrementAboveInt16MinValue) != -32767)
            {
                Console.WriteLine($"'(long)-32767.0f' was evaluated to '{(long)integerOneIncrementAboveInt16MinValue}'. Expected: '-32767'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt16MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MaxValueCastToInt64IsFoldedCorrectly()
        {
            float integerOneDecrementUnderInt16MaxValue = 32766.0f;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(long)32766.0f' was evaluated to '{(long)integerOneDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt16MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt16MaxValueCastToInt64IsFoldedCorrectly()
        {
            float integerOneIncrementAboveInt16MaxValue = 32768.0f;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneIncrementAboveInt16MaxValue) != 32768)
            {
                Console.WriteLine($"'(long)32768.0f' was evaluated to '{(long)integerOneIncrementAboveInt16MaxValue}'. Expected: '32768'.");
                _counter++;
            }
        }
        ConfirmUInt16MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt16MaxValueCastToInt64IsFoldedCorrectly()
        {
            float uInt16MaxValue = 65535.0f;

            if (BreakUpFlow())
                return;

            if (checked((long)uInt16MaxValue) != 65535)
            {
                Console.WriteLine($"'(long)65535.0f' was evaluated to '{(long)uInt16MaxValue}'. Expected: '65535'.");
                _counter++;
            }
        }
        ConfirmSingleOneDecrementUnderUInt16MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderUInt16MaxValueCastToInt64IsFoldedCorrectly()
        {
            float singleOneDecrementUnderUInt16MaxValue = 65534.996f;

            if (BreakUpFlow())
                return;

            if (checked((long)singleOneDecrementUnderUInt16MaxValue) != 65534)
            {
                Console.WriteLine($"'(long)65534.996f' was evaluated to '{(long)singleOneDecrementUnderUInt16MaxValue}'. Expected: '65534'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullDecrementUnderUInt16MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderUInt16MaxValueCastToInt64IsFoldedCorrectly()
        {
            float singleOneFullDecrementUnderUInt16MaxValue = 65534f;

            if (BreakUpFlow())
                return;

            if (checked((long)singleOneFullDecrementUnderUInt16MaxValue) != 65534)
            {
                Console.WriteLine($"'(long)65534f' was evaluated to '{(long)singleOneFullDecrementUnderUInt16MaxValue}'. Expected: '65534'.");
                _counter++;
            }
        }
        ConfirmSingleOneIncrementAboveUInt16MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveUInt16MaxValueCastToInt64IsFoldedCorrectly()
        {
            float singleOneIncrementAboveUInt16MaxValue = 65535.004f;

            if (BreakUpFlow())
                return;

            if (checked((long)singleOneIncrementAboveUInt16MaxValue) != 65535)
            {
                Console.WriteLine($"'(long)65535.004f' was evaluated to '{(long)singleOneIncrementAboveUInt16MaxValue}'. Expected: '65535'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullIncrementAboveUInt16MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveUInt16MaxValueCastToInt64IsFoldedCorrectly()
        {
            float singleOneFullIncrementAboveUInt16MaxValue = 65536f;

            if (BreakUpFlow())
                return;

            if (checked((long)singleOneFullIncrementAboveUInt16MaxValue) != 65536)
            {
                Console.WriteLine($"'(long)65536f' was evaluated to '{(long)singleOneFullIncrementAboveUInt16MaxValue}'. Expected: '65536'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderUInt16MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt16MaxValueCastToInt64IsFoldedCorrectly()
        {
            float integerOneDecrementUnderUInt16MaxValue = 65534.0f;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneDecrementUnderUInt16MaxValue) != 65534)
            {
                Console.WriteLine($"'(long)65534.0f' was evaluated to '{(long)integerOneDecrementUnderUInt16MaxValue}'. Expected: '65534'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToInt64IsFoldedCorrectly()
        {
            float integerOneIncrementAboveUInt16MaxValue = 65536.0f;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneIncrementAboveUInt16MaxValue) != 65536)
            {
                Console.WriteLine($"'(long)65536.0f' was evaluated to '{(long)integerOneIncrementAboveUInt16MaxValue}'. Expected: '65536'.");
                _counter++;
            }
        }
        ConfirmInt32MinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MinValueCastToInt64IsFoldedCorrectly()
        {
            float int32MinValue = -2147483648.0f;

            if (BreakUpFlow())
                return;

            if (checked((long)int32MinValue) != -2147483648)
            {
                Console.WriteLine($"'(long)-2147483648.0f' was evaluated to '{(long)int32MinValue}'. Expected: '-2147483648'.");
                _counter++;
            }
        }
        ConfirmInt32MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MaxValueCastToInt64IsFoldedCorrectly()
        {
            float int32MaxValue = 2147483647.0f;

            if (BreakUpFlow())
                return;

            if (checked((long)int32MaxValue) != 2147483648)
            {
                Console.WriteLine($"'(long)2147483647.0f' was evaluated to '{(long)int32MaxValue}'. Expected: '2147483648'.");
                _counter++;
            }
        }
        ConfirmSingleOneDecrementUnderInt32MinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderInt32MinValueCastToInt64IsFoldedCorrectly()
        {
            float singleOneDecrementUnderInt32MinValue = -2.147484E+09f;

            if (BreakUpFlow())
                return;

            if (checked((long)singleOneDecrementUnderInt32MinValue) != -2147483904)
            {
                Console.WriteLine($"'(long)-2.147484E+09f' was evaluated to '{(long)singleOneDecrementUnderInt32MinValue}'. Expected: '-2147483904'.");
                _counter++;
            }
        }
        ConfirmSingleOneIncrementAboveInt32MinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveInt32MinValueCastToInt64IsFoldedCorrectly()
        {
            float singleOneIncrementAboveInt32MinValue = -2.1474835E+09f;

            if (BreakUpFlow())
                return;

            if (checked((long)singleOneIncrementAboveInt32MinValue) != -2147483520)
            {
                Console.WriteLine($"'(long)-2.1474835E+09f' was evaluated to '{(long)singleOneIncrementAboveInt32MinValue}'. Expected: '-2147483520'.");
                _counter++;
            }
        }
        ConfirmSingleOneDecrementUnderInt32MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderInt32MaxValueCastToInt64IsFoldedCorrectly()
        {
            float singleOneDecrementUnderInt32MaxValue = 2.1474835E+09f;

            if (BreakUpFlow())
                return;

            if (checked((long)singleOneDecrementUnderInt32MaxValue) != 2147483520)
            {
                Console.WriteLine($"'(long)2.1474835E+09f' was evaluated to '{(long)singleOneDecrementUnderInt32MaxValue}'. Expected: '2147483520'.");
                _counter++;
            }
        }
        ConfirmSingleOneIncrementAboveInt32MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveInt32MaxValueCastToInt64IsFoldedCorrectly()
        {
            float singleOneIncrementAboveInt32MaxValue = 2.147484E+09f;

            if (BreakUpFlow())
                return;

            if (checked((long)singleOneIncrementAboveInt32MaxValue) != 2147483904)
            {
                Console.WriteLine($"'(long)2.147484E+09f' was evaluated to '{(long)singleOneIncrementAboveInt32MaxValue}'. Expected: '2147483904'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullIncrementAboveInt32MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveInt32MaxValueCastToInt64IsFoldedCorrectly()
        {
            float singleOneFullIncrementAboveInt32MaxValue = 2.1474836E+09f;

            if (BreakUpFlow())
                return;

            if (checked((long)singleOneFullIncrementAboveInt32MaxValue) != 2147483648)
            {
                Console.WriteLine($"'(long)2.1474836E+09f' was evaluated to '{(long)singleOneFullIncrementAboveInt32MaxValue}'. Expected: '2147483648'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt32MinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt32MinValueCastToInt64IsFoldedCorrectly()
        {
            float integerOneDecrementUnderInt32MinValue = -2147483649.0f;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneDecrementUnderInt32MinValue) != -2147483648)
            {
                Console.WriteLine($"'(long)-2147483649.0f' was evaluated to '{(long)integerOneDecrementUnderInt32MinValue}'. Expected: '-2147483648'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt32MinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt32MinValueCastToInt64IsFoldedCorrectly()
        {
            float integerOneIncrementAboveInt32MinValue = -2147483647.0f;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneIncrementAboveInt32MinValue) != -2147483648)
            {
                Console.WriteLine($"'(long)-2147483647.0f' was evaluated to '{(long)integerOneIncrementAboveInt32MinValue}'. Expected: '-2147483648'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt32MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt32MaxValueCastToInt64IsFoldedCorrectly()
        {
            float integerOneDecrementUnderInt32MaxValue = 2147483646.0f;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneDecrementUnderInt32MaxValue) != 2147483648)
            {
                Console.WriteLine($"'(long)2147483646.0f' was evaluated to '{(long)integerOneDecrementUnderInt32MaxValue}'. Expected: '2147483648'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt32MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt32MaxValueCastToInt64IsFoldedCorrectly()
        {
            float integerOneIncrementAboveInt32MaxValue = 2147483648.0f;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneIncrementAboveInt32MaxValue) != 2147483648)
            {
                Console.WriteLine($"'(long)2147483648.0f' was evaluated to '{(long)integerOneIncrementAboveInt32MaxValue}'. Expected: '2147483648'.");
                _counter++;
            }
        }
        ConfirmUInt32MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32MaxValueCastToInt64IsFoldedCorrectly()
        {
            float uInt32MaxValue = 4294967295.0f;

            if (BreakUpFlow())
                return;

            if (checked((long)uInt32MaxValue) != 4294967296)
            {
                Console.WriteLine($"'(long)4294967295.0f' was evaluated to '{(long)uInt32MaxValue}'. Expected: '4294967296'.");
                _counter++;
            }
        }
        ConfirmSingleOneDecrementUnderUInt32MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderUInt32MaxValueCastToInt64IsFoldedCorrectly()
        {
            float singleOneDecrementUnderUInt32MaxValue = 4.294967E+09f;

            if (BreakUpFlow())
                return;

            if (checked((long)singleOneDecrementUnderUInt32MaxValue) != 4294967040)
            {
                Console.WriteLine($"'(long)4.294967E+09f' was evaluated to '{(long)singleOneDecrementUnderUInt32MaxValue}'. Expected: '4294967040'.");
                _counter++;
            }
        }
        ConfirmSingleOneIncrementAboveUInt32MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveUInt32MaxValueCastToInt64IsFoldedCorrectly()
        {
            float singleOneIncrementAboveUInt32MaxValue = 4.294968E+09f;

            if (BreakUpFlow())
                return;

            if (checked((long)singleOneIncrementAboveUInt32MaxValue) != 4294967808)
            {
                Console.WriteLine($"'(long)4.294968E+09f' was evaluated to '{(long)singleOneIncrementAboveUInt32MaxValue}'. Expected: '4294967808'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullIncrementAboveUInt32MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveUInt32MaxValueCastToInt64IsFoldedCorrectly()
        {
            float singleOneFullIncrementAboveUInt32MaxValue = 4.2949673E+09f;

            if (BreakUpFlow())
                return;

            if (checked((long)singleOneFullIncrementAboveUInt32MaxValue) != 4294967296)
            {
                Console.WriteLine($"'(long)4.2949673E+09f' was evaluated to '{(long)singleOneFullIncrementAboveUInt32MaxValue}'. Expected: '4294967296'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderUInt32MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt32MaxValueCastToInt64IsFoldedCorrectly()
        {
            float integerOneDecrementUnderUInt32MaxValue = 4294967294.0f;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneDecrementUnderUInt32MaxValue) != 4294967296)
            {
                Console.WriteLine($"'(long)4294967294.0f' was evaluated to '{(long)integerOneDecrementUnderUInt32MaxValue}'. Expected: '4294967296'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveUInt32MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt32MaxValueCastToInt64IsFoldedCorrectly()
        {
            float integerOneIncrementAboveUInt32MaxValue = 4294967296.0f;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneIncrementAboveUInt32MaxValue) != 4294967296)
            {
                Console.WriteLine($"'(long)4294967296.0f' was evaluated to '{(long)integerOneIncrementAboveUInt32MaxValue}'. Expected: '4294967296'.");
                _counter++;
            }
        }
        ConfirmInt64MinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MinValueCastToInt64IsFoldedCorrectly()
        {
            float int64MinValue = -9223372036854775808.0f;

            if (BreakUpFlow())
                return;

            if (checked((long)int64MinValue) != -9223372036854775808)
            {
                Console.WriteLine($"'(long)-9223372036854775808.0f' was evaluated to '{(long)int64MinValue}'. Expected: '-9223372036854775808'.");
                _counter++;
            }
        }
        ConfirmInt64MaxValueCastToInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MaxValueCastToInt64Overflows()
        {
            float from = 9223372036854775807.0f;
            _counter++;
            try
            {
                _ = checked((long)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((long)9223372036854775807.0f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveInt64MinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveInt64MinValueCastToInt64IsFoldedCorrectly()
        {
            float singleOneIncrementAboveInt64MinValue = -9.2233715E+18f;

            if (BreakUpFlow())
                return;

            if (checked((long)singleOneIncrementAboveInt64MinValue) != -9223371487098961920)
            {
                Console.WriteLine($"'(long)-9.2233715E+18f' was evaluated to '{(long)singleOneIncrementAboveInt64MinValue}'. Expected: '-9223371487098961920'.");
                _counter++;
            }
        }
        ConfirmSingleOneDecrementUnderInt64MaxValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderInt64MaxValueCastToInt64IsFoldedCorrectly()
        {
            float singleOneDecrementUnderInt64MaxValue = 9.2233715E+18f;

            if (BreakUpFlow())
                return;

            if (checked((long)singleOneDecrementUnderInt64MaxValue) != 9223371487098961920)
            {
                Console.WriteLine($"'(long)9.2233715E+18f' was evaluated to '{(long)singleOneDecrementUnderInt64MaxValue}'. Expected: '9223371487098961920'.");
                _counter++;
            }
        }
        ConfirmSingleOneIncrementAboveInt64MaxValueCastToInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveInt64MaxValueCastToInt64Overflows()
        {
            float from = 9.223373E+18f;
            _counter++;
            try
            {
                _ = checked((long)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((long)9.223373E+18f)' did not throw OverflowException.");
        }
        ConfirmSingleOneFullIncrementAboveInt64MaxValueCastToInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveInt64MaxValueCastToInt64Overflows()
        {
            float from = 9.223372E+18f;
            _counter++;
            try
            {
                _ = checked((long)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((long)9.223372E+18f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt64MinValueCastToInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt64MinValueCastToInt64IsFoldedCorrectly()
        {
            float integerOneIncrementAboveInt64MinValue = -9223372036854775807.0f;

            if (BreakUpFlow())
                return;

            if (checked((long)integerOneIncrementAboveInt64MinValue) != -9223372036854775808)
            {
                Console.WriteLine($"'(long)-9223372036854775807.0f' was evaluated to '{(long)integerOneIncrementAboveInt64MinValue}'. Expected: '-9223372036854775808'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt64MaxValueCastToInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt64MaxValueCastToInt64Overflows()
        {
            float from = 9223372036854775806.0f;
            _counter++;
            try
            {
                _ = checked((long)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((long)9223372036854775806.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt64MaxValueCastToInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt64MaxValueCastToInt64Overflows()
        {
            float from = 9223372036854775808.0f;
            _counter++;
            try
            {
                _ = checked((long)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((long)9223372036854775808.0f)' did not throw OverflowException.");
        }
        ConfirmUInt64MaxValueCastToInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64MaxValueCastToInt64Overflows()
        {
            float from = 18446744073709551615.0f;
            _counter++;
            try
            {
                _ = checked((long)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((long)18446744073709551615.0f)' did not throw OverflowException.");
        }
        ConfirmSingleOneDecrementUnderUInt64MaxValueCastToInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderUInt64MaxValueCastToInt64Overflows()
        {
            float from = 1.8446743E+19f;
            _counter++;
            try
            {
                _ = checked((long)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((long)1.8446743E+19f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderUInt64MaxValueCastToInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt64MaxValueCastToInt64Overflows()
        {
            float from = 18446744073709551614.0f;
            _counter++;
            try
            {
                _ = checked((long)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((long)18446744073709551614.0f)' did not throw OverflowException.");
        }
    }

    private static void TestCastingSingleToUInt64()
    {
        ConfirmIntegerZeroCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerZeroCastToUInt64IsFoldedCorrectly()
        {
            float integerZero = 0.0f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerZero) != 0)
            {
                Console.WriteLine($"'(ulong)0.0f' was evaluated to '{(ulong)integerZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmFloatMinusZeroCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmFloatMinusZeroCastToUInt64IsFoldedCorrectly()
        {
            float floatMinusZero = -0f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)floatMinusZero) != 0)
            {
                Console.WriteLine($"'(ulong)-0f' was evaluated to '{(ulong)floatMinusZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmFloatZeroCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmFloatZeroCastToUInt64IsFoldedCorrectly()
        {
            float floatZero = 0f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)floatZero) != 0)
            {
                Console.WriteLine($"'(ulong)0f' was evaluated to '{(ulong)floatZero}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmFloatHalfOfMaxValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmFloatHalfOfMaxValueCastToUInt64Overflows()
        {
            float from = 1.7014117E+38f;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)1.7014117E+38f)' did not throw OverflowException.");
        }
        ConfirmFloatHalfOfMinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmFloatHalfOfMinValueCastToUInt64Overflows()
        {
            float from = -1.7014117E+38f;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-1.7014117E+38f)' did not throw OverflowException.");
        }
        ConfirmSingleMinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleMinValueCastToUInt64Overflows()
        {
            float from = -3.4028235E+38f;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-3.4028235E+38f)' did not throw OverflowException.");
        }
        ConfirmSingleMaxValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleMaxValueCastToUInt64Overflows()
        {
            float from = 3.4028235E+38f;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)3.4028235E+38f)' did not throw OverflowException.");
        }
        ConfirmSByteMinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMinValueCastToUInt64Overflows()
        {
            float from = -128.0f;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-128.0f)' did not throw OverflowException.");
        }
        ConfirmSByteMaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSByteMaxValueCastToUInt64IsFoldedCorrectly()
        {
            float sByteMaxValue = 127.0f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)sByteMaxValue) != 127)
            {
                Console.WriteLine($"'(ulong)127.0f' was evaluated to '{(ulong)sByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmSingleOneDecrementUnderSByteMinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderSByteMinValueCastToUInt64Overflows()
        {
            float from = -128.00002f;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-128.00002f)' did not throw OverflowException.");
        }
        ConfirmSingleOneFullDecrementUnderSByteMinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderSByteMinValueCastToUInt64Overflows()
        {
            float from = -129f;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-129f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveSByteMinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveSByteMinValueCastToUInt64Overflows()
        {
            float from = -127.99999f;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-127.99999f)' did not throw OverflowException.");
        }
        ConfirmSingleOneFullIncrementAboveSByteMinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveSByteMinValueCastToUInt64Overflows()
        {
            float from = -127f;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-127f)' did not throw OverflowException.");
        }
        ConfirmSingleOneDecrementUnderSByteMaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderSByteMaxValueCastToUInt64IsFoldedCorrectly()
        {
            float singleOneDecrementUnderSByteMaxValue = 126.99999f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)singleOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(ulong)126.99999f' was evaluated to '{(ulong)singleOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullDecrementUnderSByteMaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderSByteMaxValueCastToUInt64IsFoldedCorrectly()
        {
            float singleOneFullDecrementUnderSByteMaxValue = 126f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)singleOneFullDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(ulong)126f' was evaluated to '{(ulong)singleOneFullDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmSingleOneIncrementAboveSByteMaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveSByteMaxValueCastToUInt64IsFoldedCorrectly()
        {
            float singleOneIncrementAboveSByteMaxValue = 127.00001f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)singleOneIncrementAboveSByteMaxValue) != 127)
            {
                Console.WriteLine($"'(ulong)127.00001f' was evaluated to '{(ulong)singleOneIncrementAboveSByteMaxValue}'. Expected: '127'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullIncrementAboveSByteMaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveSByteMaxValueCastToUInt64IsFoldedCorrectly()
        {
            float singleOneFullIncrementAboveSByteMaxValue = 128f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)singleOneFullIncrementAboveSByteMaxValue) != 128)
            {
                Console.WriteLine($"'(ulong)128f' was evaluated to '{(ulong)singleOneFullIncrementAboveSByteMaxValue}'. Expected: '128'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderSByteMinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMinValueCastToUInt64Overflows()
        {
            float from = -129.0f;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-129.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveSByteMinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMinValueCastToUInt64Overflows()
        {
            float from = -127.0f;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-127.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderSByteMaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderSByteMaxValueCastToUInt64IsFoldedCorrectly()
        {
            float integerOneDecrementUnderSByteMaxValue = 126.0f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneDecrementUnderSByteMaxValue) != 126)
            {
                Console.WriteLine($"'(ulong)126.0f' was evaluated to '{(ulong)integerOneDecrementUnderSByteMaxValue}'. Expected: '126'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveSByteMaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveSByteMaxValueCastToUInt64IsFoldedCorrectly()
        {
            float integerOneIncrementAboveSByteMaxValue = 128.0f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneIncrementAboveSByteMaxValue) != 128)
            {
                Console.WriteLine($"'(ulong)128.0f' was evaluated to '{(ulong)integerOneIncrementAboveSByteMaxValue}'. Expected: '128'.");
                _counter++;
            }
        }
        ConfirmByteMaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmByteMaxValueCastToUInt64IsFoldedCorrectly()
        {
            float byteMaxValue = 255.0f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)byteMaxValue) != 255)
            {
                Console.WriteLine($"'(ulong)255.0f' was evaluated to '{(ulong)byteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmSingleOneDecrementUnderByteMinValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderByteMinValueCastToUInt64IsFoldedCorrectly()
        {
            float singleOneDecrementUnderByteMinValue = -1E-45f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)singleOneDecrementUnderByteMinValue) != 0)
            {
                Console.WriteLine($"'(ulong)-1E-45f' was evaluated to '{(ulong)singleOneDecrementUnderByteMinValue}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullDecrementUnderByteMinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderByteMinValueCastToUInt64Overflows()
        {
            float from = -1f;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-1f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveByteMinValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveByteMinValueCastToUInt64IsFoldedCorrectly()
        {
            float singleOneIncrementAboveByteMinValue = 1E-45f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)singleOneIncrementAboveByteMinValue) != 0)
            {
                Console.WriteLine($"'(ulong)1E-45f' was evaluated to '{(ulong)singleOneIncrementAboveByteMinValue}'. Expected: '0'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullIncrementAboveByteMinValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveByteMinValueCastToUInt64IsFoldedCorrectly()
        {
            float singleOneFullIncrementAboveByteMinValue = 1f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)singleOneFullIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(ulong)1f' was evaluated to '{(ulong)singleOneFullIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmSingleOneDecrementUnderByteMaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderByteMaxValueCastToUInt64IsFoldedCorrectly()
        {
            float singleOneDecrementUnderByteMaxValue = 254.99998f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)singleOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(ulong)254.99998f' was evaluated to '{(ulong)singleOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullDecrementUnderByteMaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderByteMaxValueCastToUInt64IsFoldedCorrectly()
        {
            float singleOneFullDecrementUnderByteMaxValue = 254f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)singleOneFullDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(ulong)254f' was evaluated to '{(ulong)singleOneFullDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmSingleOneIncrementAboveByteMaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveByteMaxValueCastToUInt64IsFoldedCorrectly()
        {
            float singleOneIncrementAboveByteMaxValue = 255.00002f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)singleOneIncrementAboveByteMaxValue) != 255)
            {
                Console.WriteLine($"'(ulong)255.00002f' was evaluated to '{(ulong)singleOneIncrementAboveByteMaxValue}'. Expected: '255'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullIncrementAboveByteMaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveByteMaxValueCastToUInt64IsFoldedCorrectly()
        {
            float singleOneFullIncrementAboveByteMaxValue = 256f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)singleOneFullIncrementAboveByteMaxValue) != 256)
            {
                Console.WriteLine($"'(ulong)256f' was evaluated to '{(ulong)singleOneFullIncrementAboveByteMaxValue}'. Expected: '256'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMinValueCastToUInt64Overflows()
        {
            float from = -1.0f;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-1.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveByteMinValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMinValueCastToUInt64IsFoldedCorrectly()
        {
            float integerOneIncrementAboveByteMinValue = 1.0f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneIncrementAboveByteMinValue) != 1)
            {
                Console.WriteLine($"'(ulong)1.0f' was evaluated to '{(ulong)integerOneIncrementAboveByteMinValue}'. Expected: '1'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderByteMaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderByteMaxValueCastToUInt64IsFoldedCorrectly()
        {
            float integerOneDecrementUnderByteMaxValue = 254.0f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneDecrementUnderByteMaxValue) != 254)
            {
                Console.WriteLine($"'(ulong)254.0f' was evaluated to '{(ulong)integerOneDecrementUnderByteMaxValue}'. Expected: '254'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveByteMaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveByteMaxValueCastToUInt64IsFoldedCorrectly()
        {
            float integerOneIncrementAboveByteMaxValue = 256.0f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneIncrementAboveByteMaxValue) != 256)
            {
                Console.WriteLine($"'(ulong)256.0f' was evaluated to '{(ulong)integerOneIncrementAboveByteMaxValue}'. Expected: '256'.");
                _counter++;
            }
        }
        ConfirmInt16MinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MinValueCastToUInt64Overflows()
        {
            float from = -32768.0f;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-32768.0f)' did not throw OverflowException.");
        }
        ConfirmInt16MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt16MaxValueCastToUInt64IsFoldedCorrectly()
        {
            float int16MaxValue = 32767.0f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)int16MaxValue) != 32767)
            {
                Console.WriteLine($"'(ulong)32767.0f' was evaluated to '{(ulong)int16MaxValue}'. Expected: '32767'.");
                _counter++;
            }
        }
        ConfirmSingleOneDecrementUnderInt16MinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderInt16MinValueCastToUInt64Overflows()
        {
            float from = -32768.004f;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-32768.004f)' did not throw OverflowException.");
        }
        ConfirmSingleOneFullDecrementUnderInt16MinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderInt16MinValueCastToUInt64Overflows()
        {
            float from = -32769f;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-32769f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveInt16MinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveInt16MinValueCastToUInt64Overflows()
        {
            float from = -32767.998f;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-32767.998f)' did not throw OverflowException.");
        }
        ConfirmSingleOneFullIncrementAboveInt16MinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveInt16MinValueCastToUInt64Overflows()
        {
            float from = -32767f;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-32767f)' did not throw OverflowException.");
        }
        ConfirmSingleOneDecrementUnderInt16MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderInt16MaxValueCastToUInt64IsFoldedCorrectly()
        {
            float singleOneDecrementUnderInt16MaxValue = 32766.998f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)singleOneDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(ulong)32766.998f' was evaluated to '{(ulong)singleOneDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullDecrementUnderInt16MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderInt16MaxValueCastToUInt64IsFoldedCorrectly()
        {
            float singleOneFullDecrementUnderInt16MaxValue = 32766f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)singleOneFullDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(ulong)32766f' was evaluated to '{(ulong)singleOneFullDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
        ConfirmSingleOneIncrementAboveInt16MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveInt16MaxValueCastToUInt64IsFoldedCorrectly()
        {
            float singleOneIncrementAboveInt16MaxValue = 32767.002f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)singleOneIncrementAboveInt16MaxValue) != 32767)
            {
                Console.WriteLine($"'(ulong)32767.002f' was evaluated to '{(ulong)singleOneIncrementAboveInt16MaxValue}'. Expected: '32767'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullIncrementAboveInt16MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveInt16MaxValueCastToUInt64IsFoldedCorrectly()
        {
            float singleOneFullIncrementAboveInt16MaxValue = 32768f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)singleOneFullIncrementAboveInt16MaxValue) != 32768)
            {
                Console.WriteLine($"'(ulong)32768f' was evaluated to '{(ulong)singleOneFullIncrementAboveInt16MaxValue}'. Expected: '32768'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt16MinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MinValueCastToUInt64Overflows()
        {
            float from = -32769.0f;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-32769.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt16MinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt16MinValueCastToUInt64Overflows()
        {
            float from = -32767.0f;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-32767.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt16MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt16MaxValueCastToUInt64IsFoldedCorrectly()
        {
            float integerOneDecrementUnderInt16MaxValue = 32766.0f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneDecrementUnderInt16MaxValue) != 32766)
            {
                Console.WriteLine($"'(ulong)32766.0f' was evaluated to '{(ulong)integerOneDecrementUnderInt16MaxValue}'. Expected: '32766'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt16MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt16MaxValueCastToUInt64IsFoldedCorrectly()
        {
            float integerOneIncrementAboveInt16MaxValue = 32768.0f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneIncrementAboveInt16MaxValue) != 32768)
            {
                Console.WriteLine($"'(ulong)32768.0f' was evaluated to '{(ulong)integerOneIncrementAboveInt16MaxValue}'. Expected: '32768'.");
                _counter++;
            }
        }
        ConfirmUInt16MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt16MaxValueCastToUInt64IsFoldedCorrectly()
        {
            float uInt16MaxValue = 65535.0f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)uInt16MaxValue) != 65535)
            {
                Console.WriteLine($"'(ulong)65535.0f' was evaluated to '{(ulong)uInt16MaxValue}'. Expected: '65535'.");
                _counter++;
            }
        }
        ConfirmSingleOneDecrementUnderUInt16MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderUInt16MaxValueCastToUInt64IsFoldedCorrectly()
        {
            float singleOneDecrementUnderUInt16MaxValue = 65534.996f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)singleOneDecrementUnderUInt16MaxValue) != 65534)
            {
                Console.WriteLine($"'(ulong)65534.996f' was evaluated to '{(ulong)singleOneDecrementUnderUInt16MaxValue}'. Expected: '65534'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullDecrementUnderUInt16MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullDecrementUnderUInt16MaxValueCastToUInt64IsFoldedCorrectly()
        {
            float singleOneFullDecrementUnderUInt16MaxValue = 65534f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)singleOneFullDecrementUnderUInt16MaxValue) != 65534)
            {
                Console.WriteLine($"'(ulong)65534f' was evaluated to '{(ulong)singleOneFullDecrementUnderUInt16MaxValue}'. Expected: '65534'.");
                _counter++;
            }
        }
        ConfirmSingleOneIncrementAboveUInt16MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveUInt16MaxValueCastToUInt64IsFoldedCorrectly()
        {
            float singleOneIncrementAboveUInt16MaxValue = 65535.004f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)singleOneIncrementAboveUInt16MaxValue) != 65535)
            {
                Console.WriteLine($"'(ulong)65535.004f' was evaluated to '{(ulong)singleOneIncrementAboveUInt16MaxValue}'. Expected: '65535'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullIncrementAboveUInt16MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveUInt16MaxValueCastToUInt64IsFoldedCorrectly()
        {
            float singleOneFullIncrementAboveUInt16MaxValue = 65536f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)singleOneFullIncrementAboveUInt16MaxValue) != 65536)
            {
                Console.WriteLine($"'(ulong)65536f' was evaluated to '{(ulong)singleOneFullIncrementAboveUInt16MaxValue}'. Expected: '65536'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderUInt16MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt16MaxValueCastToUInt64IsFoldedCorrectly()
        {
            float integerOneDecrementUnderUInt16MaxValue = 65534.0f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneDecrementUnderUInt16MaxValue) != 65534)
            {
                Console.WriteLine($"'(ulong)65534.0f' was evaluated to '{(ulong)integerOneDecrementUnderUInt16MaxValue}'. Expected: '65534'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt16MaxValueCastToUInt64IsFoldedCorrectly()
        {
            float integerOneIncrementAboveUInt16MaxValue = 65536.0f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneIncrementAboveUInt16MaxValue) != 65536)
            {
                Console.WriteLine($"'(ulong)65536.0f' was evaluated to '{(ulong)integerOneIncrementAboveUInt16MaxValue}'. Expected: '65536'.");
                _counter++;
            }
        }
        ConfirmInt32MinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MinValueCastToUInt64Overflows()
        {
            float from = -2147483648.0f;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-2147483648.0f)' did not throw OverflowException.");
        }
        ConfirmInt32MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt32MaxValueCastToUInt64IsFoldedCorrectly()
        {
            float int32MaxValue = 2147483647.0f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)int32MaxValue) != 2147483648)
            {
                Console.WriteLine($"'(ulong)2147483647.0f' was evaluated to '{(ulong)int32MaxValue}'. Expected: '2147483648'.");
                _counter++;
            }
        }
        ConfirmSingleOneDecrementUnderInt32MinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderInt32MinValueCastToUInt64Overflows()
        {
            float from = -2.147484E+09f;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-2.147484E+09f)' did not throw OverflowException.");
        }
        ConfirmSingleOneIncrementAboveInt32MinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveInt32MinValueCastToUInt64Overflows()
        {
            float from = -2.1474835E+09f;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-2.1474835E+09f)' did not throw OverflowException.");
        }
        ConfirmSingleOneDecrementUnderInt32MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderInt32MaxValueCastToUInt64IsFoldedCorrectly()
        {
            float singleOneDecrementUnderInt32MaxValue = 2.1474835E+09f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)singleOneDecrementUnderInt32MaxValue) != 2147483520)
            {
                Console.WriteLine($"'(ulong)2.1474835E+09f' was evaluated to '{(ulong)singleOneDecrementUnderInt32MaxValue}'. Expected: '2147483520'.");
                _counter++;
            }
        }
        ConfirmSingleOneIncrementAboveInt32MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveInt32MaxValueCastToUInt64IsFoldedCorrectly()
        {
            float singleOneIncrementAboveInt32MaxValue = 2.147484E+09f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)singleOneIncrementAboveInt32MaxValue) != 2147483904)
            {
                Console.WriteLine($"'(ulong)2.147484E+09f' was evaluated to '{(ulong)singleOneIncrementAboveInt32MaxValue}'. Expected: '2147483904'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullIncrementAboveInt32MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveInt32MaxValueCastToUInt64IsFoldedCorrectly()
        {
            float singleOneFullIncrementAboveInt32MaxValue = 2.1474836E+09f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)singleOneFullIncrementAboveInt32MaxValue) != 2147483648)
            {
                Console.WriteLine($"'(ulong)2.1474836E+09f' was evaluated to '{(ulong)singleOneFullIncrementAboveInt32MaxValue}'. Expected: '2147483648'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderInt32MinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt32MinValueCastToUInt64Overflows()
        {
            float from = -2147483649.0f;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-2147483649.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneIncrementAboveInt32MinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt32MinValueCastToUInt64Overflows()
        {
            float from = -2147483647.0f;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-2147483647.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt32MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt32MaxValueCastToUInt64IsFoldedCorrectly()
        {
            float integerOneDecrementUnderInt32MaxValue = 2147483646.0f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneDecrementUnderInt32MaxValue) != 2147483648)
            {
                Console.WriteLine($"'(ulong)2147483646.0f' was evaluated to '{(ulong)integerOneDecrementUnderInt32MaxValue}'. Expected: '2147483648'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt32MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt32MaxValueCastToUInt64IsFoldedCorrectly()
        {
            float integerOneIncrementAboveInt32MaxValue = 2147483648.0f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneIncrementAboveInt32MaxValue) != 2147483648)
            {
                Console.WriteLine($"'(ulong)2147483648.0f' was evaluated to '{(ulong)integerOneIncrementAboveInt32MaxValue}'. Expected: '2147483648'.");
                _counter++;
            }
        }
        ConfirmUInt32MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt32MaxValueCastToUInt64IsFoldedCorrectly()
        {
            float uInt32MaxValue = 4294967295.0f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)uInt32MaxValue) != 4294967296)
            {
                Console.WriteLine($"'(ulong)4294967295.0f' was evaluated to '{(ulong)uInt32MaxValue}'. Expected: '4294967296'.");
                _counter++;
            }
        }
        ConfirmSingleOneDecrementUnderUInt32MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderUInt32MaxValueCastToUInt64IsFoldedCorrectly()
        {
            float singleOneDecrementUnderUInt32MaxValue = 4.294967E+09f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)singleOneDecrementUnderUInt32MaxValue) != 4294967040)
            {
                Console.WriteLine($"'(ulong)4.294967E+09f' was evaluated to '{(ulong)singleOneDecrementUnderUInt32MaxValue}'. Expected: '4294967040'.");
                _counter++;
            }
        }
        ConfirmSingleOneIncrementAboveUInt32MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveUInt32MaxValueCastToUInt64IsFoldedCorrectly()
        {
            float singleOneIncrementAboveUInt32MaxValue = 4.294968E+09f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)singleOneIncrementAboveUInt32MaxValue) != 4294967808)
            {
                Console.WriteLine($"'(ulong)4.294968E+09f' was evaluated to '{(ulong)singleOneIncrementAboveUInt32MaxValue}'. Expected: '4294967808'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullIncrementAboveUInt32MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveUInt32MaxValueCastToUInt64IsFoldedCorrectly()
        {
            float singleOneFullIncrementAboveUInt32MaxValue = 4.2949673E+09f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)singleOneFullIncrementAboveUInt32MaxValue) != 4294967296)
            {
                Console.WriteLine($"'(ulong)4.2949673E+09f' was evaluated to '{(ulong)singleOneFullIncrementAboveUInt32MaxValue}'. Expected: '4294967296'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderUInt32MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt32MaxValueCastToUInt64IsFoldedCorrectly()
        {
            float integerOneDecrementUnderUInt32MaxValue = 4294967294.0f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneDecrementUnderUInt32MaxValue) != 4294967296)
            {
                Console.WriteLine($"'(ulong)4294967294.0f' was evaluated to '{(ulong)integerOneDecrementUnderUInt32MaxValue}'. Expected: '4294967296'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveUInt32MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveUInt32MaxValueCastToUInt64IsFoldedCorrectly()
        {
            float integerOneIncrementAboveUInt32MaxValue = 4294967296.0f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneIncrementAboveUInt32MaxValue) != 4294967296)
            {
                Console.WriteLine($"'(ulong)4294967296.0f' was evaluated to '{(ulong)integerOneIncrementAboveUInt32MaxValue}'. Expected: '4294967296'.");
                _counter++;
            }
        }
        ConfirmInt64MinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MinValueCastToUInt64Overflows()
        {
            float from = -9223372036854775808.0f;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-9223372036854775808.0f)' did not throw OverflowException.");
        }
        ConfirmInt64MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmInt64MaxValueCastToUInt64IsFoldedCorrectly()
        {
            float int64MaxValue = 9223372036854775807.0f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)int64MaxValue) != 9223372036854775808)
            {
                Console.WriteLine($"'(ulong)9223372036854775807.0f' was evaluated to '{(ulong)int64MaxValue}'. Expected: '9223372036854775808'.");
                _counter++;
            }
        }
        ConfirmSingleOneIncrementAboveInt64MinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveInt64MinValueCastToUInt64Overflows()
        {
            float from = -9.2233715E+18f;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-9.2233715E+18f)' did not throw OverflowException.");
        }
        ConfirmSingleOneDecrementUnderInt64MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderInt64MaxValueCastToUInt64IsFoldedCorrectly()
        {
            float singleOneDecrementUnderInt64MaxValue = 9.2233715E+18f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)singleOneDecrementUnderInt64MaxValue) != 9223371487098961920)
            {
                Console.WriteLine($"'(ulong)9.2233715E+18f' was evaluated to '{(ulong)singleOneDecrementUnderInt64MaxValue}'. Expected: '9223371487098961920'.");
                _counter++;
            }
        }
        ConfirmSingleOneIncrementAboveInt64MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneIncrementAboveInt64MaxValueCastToUInt64IsFoldedCorrectly()
        {
            float singleOneIncrementAboveInt64MaxValue = 9.223373E+18f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)singleOneIncrementAboveInt64MaxValue) != 9223373136366403584)
            {
                Console.WriteLine($"'(ulong)9.223373E+18f' was evaluated to '{(ulong)singleOneIncrementAboveInt64MaxValue}'. Expected: '9223373136366403584'.");
                _counter++;
            }
        }
        ConfirmSingleOneFullIncrementAboveInt64MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneFullIncrementAboveInt64MaxValueCastToUInt64IsFoldedCorrectly()
        {
            float singleOneFullIncrementAboveInt64MaxValue = 9.223372E+18f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)singleOneFullIncrementAboveInt64MaxValue) != 9223372036854775808)
            {
                Console.WriteLine($"'(ulong)9.223372E+18f' was evaluated to '{(ulong)singleOneFullIncrementAboveInt64MaxValue}'. Expected: '9223372036854775808'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt64MinValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt64MinValueCastToUInt64Overflows()
        {
            float from = -9223372036854775807.0f;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)-9223372036854775807.0f)' did not throw OverflowException.");
        }
        ConfirmIntegerOneDecrementUnderInt64MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderInt64MaxValueCastToUInt64IsFoldedCorrectly()
        {
            float integerOneDecrementUnderInt64MaxValue = 9223372036854775806.0f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneDecrementUnderInt64MaxValue) != 9223372036854775808)
            {
                Console.WriteLine($"'(ulong)9223372036854775806.0f' was evaluated to '{(ulong)integerOneDecrementUnderInt64MaxValue}'. Expected: '9223372036854775808'.");
                _counter++;
            }
        }
        ConfirmIntegerOneIncrementAboveInt64MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneIncrementAboveInt64MaxValueCastToUInt64IsFoldedCorrectly()
        {
            float integerOneIncrementAboveInt64MaxValue = 9223372036854775808.0f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)integerOneIncrementAboveInt64MaxValue) != 9223372036854775808)
            {
                Console.WriteLine($"'(ulong)9223372036854775808.0f' was evaluated to '{(ulong)integerOneIncrementAboveInt64MaxValue}'. Expected: '9223372036854775808'.");
                _counter++;
            }
        }
        ConfirmUInt64MaxValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmUInt64MaxValueCastToUInt64Overflows()
        {
            float from = 18446744073709551615.0f;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)18446744073709551615.0f)' did not throw OverflowException.");
        }
        ConfirmSingleOneDecrementUnderUInt64MaxValueCastToUInt64IsFoldedCorrectly();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmSingleOneDecrementUnderUInt64MaxValueCastToUInt64IsFoldedCorrectly()
        {
            float singleOneDecrementUnderUInt64MaxValue = 1.8446743E+19f;

            if (BreakUpFlow())
                return;

            if (checked((ulong)singleOneDecrementUnderUInt64MaxValue) != 18446742974197923840)
            {
                Console.WriteLine($"'(ulong)1.8446743E+19f' was evaluated to '{(ulong)singleOneDecrementUnderUInt64MaxValue}'. Expected: '18446742974197923840'.");
                _counter++;
            }
        }
        ConfirmIntegerOneDecrementUnderUInt64MaxValueCastToUInt64Overflows();
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmIntegerOneDecrementUnderUInt64MaxValueCastToUInt64Overflows()
        {
            float from = 18446744073709551614.0f;
            _counter++;
            try
            {
                _ = checked((ulong)from);
            }
            catch (OverflowException) { _counter--; }
            if (_counter != 100)
                Console.WriteLine("'checked((ulong)18446744073709551614.0f)' did not throw OverflowException.");
        }
    }
}
