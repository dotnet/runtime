// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class ValueNumberingUnorderedComparisonsOfConstants
{
    private static readonly double _quietDoubleNaN = BitConverter.Int64BitsToDouble(unchecked((long)0xfff8000000000001));
    private static readonly float _quietFloatNaN = BitConverter.Int32BitsToSingle(unchecked((int)0xffc00001));

    private static int _counter = 100;

    [Fact]
    public static int TestEntryPoint()
    {
        // The conditions of the loops get reversed and duplicated.
        // As part of this a comparison like a > b, which is really !IsNaN(a) && !IsNaN(b) && a > b
        // Gets turned into IsNaN(a) || IsNaN(b) || a <= b.
        // We are testing that the constant folding of these new unordered comparisons in VN is correct.

        TestDoubleComparisonsEvaluatingToTrue();
        TestSingleComparisonsEvaluatingToTrue();
        TestDoubleComparisonsEvaluatingToFalse();
        TestSingleComparisonsEvaluatingToFalse();

        return _counter;
    }

    // We rely on these static readonly fields being constants at compile time.
    // This means that by the time the test methods are being compiled, the static constructor must have run.
    [ModuleInitializer]
    internal static void InitializeNaNs() => RuntimeHelpers.RunClassConstructor(typeof(ValueNumberingUnorderedComparisonsOfConstants).TypeHandle);

    private static void TestDoubleComparisonsEvaluatingToTrue()
    {
        // The following inverted conditions must be folded to "true".
        // Meaning the loop body must never execute.

        // Basic scenarios
        // VNF_LT_UN
        for (double i = 1.0; i >= 2.0; i++)
            _counter++;
        // VNF_LE_UN
        for (double i = -3.0; i > 4.0; i++)
            _counter++;
        for (double i = 5.0; i > 5.0; i++)
            _counter++;
        for (double i = 0.0; i > -0.0; i++)
            _counter++;
        // VNF_GT_UN
        for (double i = 6.0; i <= -7.0; i++)
            _counter++;
        // VNF_GE_UN
        for (double i = 8.0; i < -9.0; i++)
            _counter++;
        for (double i = 10.0; i < 10.0; i++)
            _counter++;
        for (double i = -0.0; i < 0.0; i++)
            _counter++;

        // Positive infinities on the lhs
        // VNF_GT_UN
        for (double i = double.PositiveInfinity; i <= 11.0; i++)
            _counter++;
        // VNF_GE_UN
        for (double i = double.PositiveInfinity; i < 12.0; i++)
            _counter++;

        // Positive infinities on the rhs
        // VNF_LT_UN
        for (double i = 13.0; i >= double.PositiveInfinity; i++)
            _counter++;
        // VNF_LE_UN
        for (double i = -14.0; i > double.PositiveInfinity; i++)
            _counter++;

        // Positive infinities on both sides
        // VNF_LE_UN
        for (double i = double.PositiveInfinity; i > double.PositiveInfinity; i++)
            _counter++;
        // VNF_GE_UN
        for (double i = double.PositiveInfinity; i < double.PositiveInfinity; i++)
            _counter++;

        // Negative infinities on the lhs
        // VNF_LT_UN
        for (double i = double.NegativeInfinity; i >= 15.0; i++)
            _counter++;
        // VNF_LE_UN
        for (double i = double.NegativeInfinity; i > 16.0; i++)
            _counter++;

        // Negative infinities on the rhs
        // VNF_GT_UN
        for (double i = 17.0; i <= double.NegativeInfinity; i++)
            _counter++;
        // VNF_GE_UN
        for (double i = 18.0; i < double.NegativeInfinity; i++)
            _counter++;

        // Negative infinities on both sides
        // VNF_LE_UN
        for (double i = double.NegativeInfinity; i > double.NegativeInfinity; i++)
            _counter++;
        // VNF_GE_UN
        for (double i = double.NegativeInfinity; i < double.NegativeInfinity; i++)
            _counter++;

        // NaN on the lhs
        // VNF_LT_UN
        for (double i = double.NaN; i >= 19.0; i++)
            _counter++;
        for (double i = double.NaN; i >= double.PositiveInfinity; i++)
            _counter++;
        for (double i = double.NaN; i >= double.NegativeInfinity; i++)
            _counter++;
        for (double i = _quietDoubleNaN; i >= 19.0; i++)
            _counter++;
        for (double i = _quietDoubleNaN; i >= double.PositiveInfinity; i++)
            _counter++;
        for (double i = _quietDoubleNaN; i >= double.NegativeInfinity; i++)
            _counter++;
        // VNF_LE_UN
        for (double i = double.NaN; i > 20.0; i++)
            _counter++;
        for (double i = double.NaN; i > double.PositiveInfinity; i++)
            _counter++;
        for (double i = double.NaN; i > double.NegativeInfinity; i++)
            _counter++;
        for (double i = _quietDoubleNaN; i > 20.0; i++)
            _counter++;
        for (double i = _quietDoubleNaN; i > double.PositiveInfinity; i++)
            _counter++;
        for (double i = _quietDoubleNaN; i > double.NegativeInfinity; i++)
            _counter++;
        // VNF_GT_UN
        for (double i = double.NaN; i <= -21.0; i++)
            _counter++;
        for (double i = double.NaN; i <= double.PositiveInfinity; i++)
            _counter++;
        for (double i = double.NaN; i <= double.NegativeInfinity; i++)
            _counter++;
        for (double i = _quietDoubleNaN; i <= -21.0; i++)
            _counter++;
        for (double i = _quietDoubleNaN; i <= double.PositiveInfinity; i++)
            _counter++;
        for (double i = _quietDoubleNaN; i <= double.NegativeInfinity; i++)
            _counter++;
        // VNF_GE_UN
        for (double i = double.NaN; i < 22.0; i++)
            _counter++;
        for (double i = double.NaN; i < double.PositiveInfinity; i++)
            _counter++;
        for (double i = double.NaN; i < double.NegativeInfinity; i++)
            _counter++;
        for (double i = _quietDoubleNaN; i < 22.0; i++)
            _counter++;
        for (double i = _quietDoubleNaN; i < double.PositiveInfinity; i++)
            _counter++;
        for (double i = _quietDoubleNaN; i < double.NegativeInfinity; i++)
            _counter++;

        // NaN on the rhs
        // VNF_LT_UN
        for (double i = 23.0; i >= double.NaN; i++)
            _counter++;
        for (double i = double.NegativeInfinity; i >= double.NaN; i++)
            _counter++;
        for (double i = double.PositiveInfinity; i >= double.NaN; i++)
            _counter++;
        for (double i = 23.0; i >= _quietDoubleNaN; i++)
            _counter++;
        for (double i = double.NegativeInfinity; i >= _quietDoubleNaN; i++)
            _counter++;
        for (double i = double.PositiveInfinity; i >= _quietDoubleNaN; i++)
            _counter++;
        // VNF_LE_UN
        for (double i = -24.0; i > double.NaN; i++)
            _counter++;
        for (double i = double.NegativeInfinity; i > double.NaN; i++)
            _counter++;
        for (double i = double.PositiveInfinity; i > double.NaN; i++)
            _counter++;
        for (double i = -24.0; i > _quietDoubleNaN; i++)
            _counter++;
        for (double i = double.NegativeInfinity; i > _quietDoubleNaN; i++)
            _counter++;
        for (double i = double.PositiveInfinity; i > _quietDoubleNaN; i++)
            _counter++;
        // VNF_GT_UN
        for (double i = 25.0; i <= double.NaN; i++)
            _counter++;
        for (double i = double.NegativeInfinity; i <= double.NaN; i++)
            _counter++;
        for (double i = double.PositiveInfinity; i <= double.NaN; i++)
            _counter++;
        for (double i = 25.0; i <= _quietDoubleNaN; i++)
            _counter++;
        for (double i = double.NegativeInfinity; i <= _quietDoubleNaN; i++)
            _counter++;
        for (double i = double.PositiveInfinity; i <= _quietDoubleNaN; i++)
            _counter++;
        // VNF_GE_UN
        for (double i = 26.0; i < double.NaN; i++)
            _counter++;
        for (double i = double.NegativeInfinity; i < double.NaN; i++)
            _counter++;
        for (double i = double.PositiveInfinity; i < double.NaN; i++)
            _counter++;
        for (double i = 26.0; i < _quietDoubleNaN; i++)
            _counter++;
        for (double i = double.NegativeInfinity; i < _quietDoubleNaN; i++)
            _counter++;
        for (double i = double.PositiveInfinity; i < _quietDoubleNaN; i++)
            _counter++;

        // NaN on both sides
        // VNF_LT_UN
        for (double i = double.NaN; i >= double.NaN; i++)
            _counter++;
        for (double i = double.NaN; i >= _quietDoubleNaN; i++)
            _counter++;
        for (double i = _quietDoubleNaN; i >= double.NaN; i++)
            _counter++;
        for (double i = _quietDoubleNaN; i >= _quietDoubleNaN; i++)
            _counter++;
        // VNF_LE_UN
        for (double i = double.NaN; i > double.NaN; i++)
            _counter++;
        for (double i = double.NaN; i > _quietDoubleNaN; i++)
            _counter++;
        for (double i = _quietDoubleNaN; i > double.NaN; i++)
            _counter++;
        for (double i = _quietDoubleNaN; i > _quietDoubleNaN; i++)
            _counter++;
        // VNF_GT_UN
        for (double i = double.NaN; i <= double.NaN; i++)
            _counter++;
        for (double i = double.NaN; i <= _quietDoubleNaN; i++)
            _counter++;
        for (double i = _quietDoubleNaN; i <= double.NaN; i++)
            _counter++;
        for (double i = _quietDoubleNaN; i <= _quietDoubleNaN; i++)
            _counter++;
        // VNF_GE_UN
        for (double i = double.NaN; i < double.NaN; i++)
            _counter++;
        for (double i = double.NaN; i < _quietDoubleNaN; i++)
            _counter++;
        for (double i = _quietDoubleNaN; i < double.NaN; i++)
            _counter++;
        for (double i = _quietDoubleNaN; i < _quietDoubleNaN; i++)
            _counter++;
    }

    private static void TestSingleComparisonsEvaluatingToTrue()
    {
        // The following inverted conditions must be folded to "true".
        // Meaning the loop body must never execute.

        // Basic scenarios
        // VNF_LT_UN
        for (float i = 27.0f; i >= 28.0f; i++)
            _counter++;
        // VNF_LE_UN
        for (float i = -29.0f; i > 30.0f; i++)
            _counter++;
        for (float i = 31.0f; i > 31.0f; i++)
            _counter++;
        for (float i = 0.0f; i > -0.0f; i++)
            _counter++;
        // VNF_GT_UN
        for (float i = 32.0f; i <= -33.0f; i++)
            _counter++;
        // VNF_GE_UN
        for (float i = 34.0f; i < -35.0f; i++)
            _counter++;
        for (float i = 36.0f; i < 36.0f; i++)
            _counter++;
        for (float i = -0.0f; i < 0.0f; i++)
            _counter++;

        // Positive infinities on the lhs
        // VNF_GT_UN
        for (float i = float.PositiveInfinity; i <= 37.0f; i++)
            _counter++;
        // VNF_GE_UN
        for (float i = float.PositiveInfinity; i < 38.0f; i++)
            _counter++;

        // Positive infinities on the rhs
        // VNF_LT_UN
        for (float i = 39.0f; i >= float.PositiveInfinity; i++)
            _counter++;
        // VNF_LE_UN
        for (float i = -40.0f; i > float.PositiveInfinity; i++)
            _counter++;

        // Positive infinities on both sides
        // VNF_LE_UN
        for (float i = float.PositiveInfinity; i > float.PositiveInfinity; i++)
            _counter++;
        // VNF_GE_UN
        for (float i = float.PositiveInfinity; i < float.PositiveInfinity; i++)
            _counter++;

        // Negative infinities on the lhs
        // VNF_LT_UN
        for (float i = float.NegativeInfinity; i >= 41.0f; i++)
            _counter++;
        // VNF_LE_UN
        for (float i = float.NegativeInfinity; i > 42.0f; i++)
            _counter++;

        // Negative infinities on the rhs
        // VNF_GT_UN
        for (float i = 43.0f; i <= float.NegativeInfinity; i++)
            _counter++;
        // VNF_GE_UN
        for (float i = 44.0f; i < float.NegativeInfinity; i++)
            _counter++;

        // Negative infinities on both sides
        // VNF_LE_UN
        for (float i = float.NegativeInfinity; i > float.NegativeInfinity; i++)
            _counter++;
        // VNF_GE_UN
        for (float i = float.NegativeInfinity; i < float.NegativeInfinity; i++)
            _counter++;

        // NaN on the lhs
        // VNF_LT_UN
        for (float i = float.NaN; i >= 45.0f; i++)
            _counter++;
        for (float i = float.NaN; i >= float.PositiveInfinity; i++)
            _counter++;
        for (float i = float.NaN; i >= float.NegativeInfinity; i++)
            _counter++;
        for (float i = _quietFloatNaN; i >= 45.0f; i++)
            _counter++;
        for (float i = _quietFloatNaN; i >= float.PositiveInfinity; i++)
            _counter++;
        for (float i = _quietFloatNaN; i >= float.NegativeInfinity; i++)
            _counter++;
        // VNF_LE_UN
        for (float i = float.NaN; i > 46.0f; i++)
            _counter++;
        for (float i = float.NaN; i > float.PositiveInfinity; i++)
            _counter++;
        for (float i = float.NaN; i > float.NegativeInfinity; i++)
            _counter++;
        for (float i = _quietFloatNaN; i > 46.0f; i++)
            _counter++;
        for (float i = _quietFloatNaN; i > float.PositiveInfinity; i++)
            _counter++;
        for (float i = _quietFloatNaN; i > float.NegativeInfinity; i++)
            _counter++;
        // VNF_GT_UN
        for (float i = float.NaN; i <= -47.0f; i++)
            _counter++;
        for (float i = float.NaN; i <= float.PositiveInfinity; i++)
            _counter++;
        for (float i = float.NaN; i <= float.NegativeInfinity; i++)
            _counter++;
        for (float i = _quietFloatNaN; i <= -47.0f; i++)
            _counter++;
        for (float i = _quietFloatNaN; i <= float.PositiveInfinity; i++)
            _counter++;
        for (float i = _quietFloatNaN; i <= float.NegativeInfinity; i++)
            _counter++;
        // VNF_GE_UN
        for (float i = float.NaN; i < 48.0f; i++)
            _counter++;
        for (float i = float.NaN; i < float.PositiveInfinity; i++)
            _counter++;
        for (float i = float.NaN; i < float.NegativeInfinity; i++)
            _counter++;
        for (float i = _quietFloatNaN; i < 48.0f; i++)
            _counter++;
        for (float i = _quietFloatNaN; i < float.PositiveInfinity; i++)
            _counter++;
        for (float i = _quietFloatNaN; i < float.NegativeInfinity; i++)
            _counter++;

        // NaN on the rhs
        // VNF_LT_UN
        for (float i = 49.0f; i >= float.NaN; i++)
            _counter++;
        for (float i = float.NegativeInfinity; i >= float.NaN; i++)
            _counter++;
        for (float i = float.PositiveInfinity; i >= float.NaN; i++)
            _counter++;
        for (float i = 49.0f; i >= _quietFloatNaN; i++)
            _counter++;
        for (float i = float.NegativeInfinity; i >= _quietFloatNaN; i++)
            _counter++;
        for (float i = float.PositiveInfinity; i >= _quietFloatNaN; i++)
            _counter++;
        // VNF_LE_UN
        for (float i = -50.0f; i > float.NaN; i++)
            _counter++;
        for (float i = float.NegativeInfinity; i > float.NaN; i++)
            _counter++;
        for (float i = float.PositiveInfinity; i > float.NaN; i++)
            _counter++;
        for (float i = -50.0f; i > _quietFloatNaN; i++)
            _counter++;
        for (float i = float.NegativeInfinity; i > _quietFloatNaN; i++)
            _counter++;
        for (float i = float.PositiveInfinity; i > _quietFloatNaN; i++)
            _counter++;
        // VNF_GT_UN
        for (float i = 51.0f; i <= float.NaN; i++)
            _counter++;
        for (float i = float.NegativeInfinity; i <= float.NaN; i++)
            _counter++;
        for (float i = float.PositiveInfinity; i <= float.NaN; i++)
            _counter++;
        for (float i = 51.0f; i <= _quietFloatNaN; i++)
            _counter++;
        for (float i = float.NegativeInfinity; i <= _quietFloatNaN; i++)
            _counter++;
        for (float i = float.PositiveInfinity; i <= _quietFloatNaN; i++)
            _counter++;
        // VNF_GE_UN
        for (float i = 52.0f; i < float.NaN; i++)
            _counter++;
        for (float i = float.NegativeInfinity; i < float.NaN; i++)
            _counter++;
        for (float i = float.PositiveInfinity; i < float.NaN; i++)
            _counter++;
        for (float i = 52.0f; i < _quietFloatNaN; i++)
            _counter++;
        for (float i = float.NegativeInfinity; i < _quietFloatNaN; i++)
            _counter++;
        for (float i = float.PositiveInfinity; i < _quietFloatNaN; i++)
            _counter++;

        // NaN on both sides
        // VNF_LT_UN
        for (float i = float.NaN; i >= float.NaN; i++)
            _counter++;
        for (float i = float.NaN; i >= _quietFloatNaN; i++)
            _counter++;
        for (float i = _quietFloatNaN; i >= float.NaN; i++)
            _counter++;
        for (float i = _quietFloatNaN; i >= _quietFloatNaN; i++)
            _counter++;
        // VNF_LE_UN
        for (float i = float.NaN; i > float.NaN; i++)
            _counter++;
        for (float i = float.NaN; i > _quietFloatNaN; i++)
            _counter++;
        for (float i = _quietFloatNaN; i > float.NaN; i++)
            _counter++;
        for (float i = _quietFloatNaN; i > _quietFloatNaN; i++)
            _counter++;
        // VNF_GT_UN
        for (float i = float.NaN; i <= float.NaN; i++)
            _counter++;
        for (float i = float.NaN; i <= _quietFloatNaN; i++)
            _counter++;
        for (float i = _quietFloatNaN; i <= float.NaN; i++)
            _counter++;
        for (float i = _quietFloatNaN; i <= _quietFloatNaN; i++)
            _counter++;
        // VNF_GE_UN
        for (float i = float.NaN; i < float.NaN; i++)
            _counter++;
        for (float i = float.NaN; i < _quietFloatNaN; i++)
            _counter++;
        for (float i = _quietFloatNaN; i < float.NaN; i++)
            _counter++;
        for (float i = _quietFloatNaN; i < _quietFloatNaN; i++)
            _counter++;
    }

    private static void TestDoubleComparisonsEvaluatingToFalse()
    {
        // The following inverted conditions must be folded to "true".
        // Meaning the loop body must execute.
        // The "i = double.NaN" pattern is equivalent to "break".
        // We use it here as it is less likely to be optimized in the future before the loop condition is duplicated.

        // Basic scenarios
        // VNF_LT_UN
        for (double i = 54.0; i >= 53.0; i++)
        {
            _counter++;
            i = double.NaN;
        }
        _counter--;
        for (double i = 55.0; i >= 55.0; i++)
        {
            _counter++;
            i = double.NaN;
        }
        _counter--;
        // VNF_LE_UN
        for (double i = 56.0; i > -57.0; i++)
        {
            _counter++;
            i = double.NaN;
        }
        _counter--;
        // VNF_GT_UN
        for (double i = -58.0; i <= 59.0; i++)
        {
            _counter++;
            i = double.NaN;
        }
        _counter--;
        for (double i = -60.0; i <= -60.0; i++)
        {
            _counter++;
            i = double.NaN;
        }
        _counter--;
        // VNF_GE_UN
        for (double i = -62.0; i < 61.0; i++)
        {
            _counter++;
            i = double.NaN;
        }
        _counter--;

        // Positive infinities on the lhs
        // VNF_LT_UN
        for (double i = double.PositiveInfinity; i >= 63.0; i++)
        {
            _counter++;
            i = double.NaN;
        }
        _counter--;
        // VNF_LE_UN
        for (double i = double.PositiveInfinity; i > -64.0; i++)
        {
            _counter++;
            i = double.NaN;
        }
        _counter--;

        // Positive infinities on the rhs
        // VNF_GT_UN
        for (double i = -65.0; i <= double.PositiveInfinity; i++)
        {
            _counter++;
            i = double.NaN;
        }
        _counter--;
        // VNF_GE_UN
        for (double i = -66.0; i < double.PositiveInfinity; i++)
        {
            _counter++;
            i = double.NaN;
        }
        _counter--;

        // Positive infinities on both sides
        // VNF_LT_UN
        for (double i = double.PositiveInfinity; i >= double.PositiveInfinity; i++)
        {
            _counter++;
            i = double.NaN;
        }
        _counter--;
        // VNF_GT_UN
        for (double i = double.PositiveInfinity; i <= double.PositiveInfinity; i++)
        {
            _counter++;
            i = double.NaN;
        }
        _counter--;

        // Negative infinities on the lhs
        // VNF_GT_UN
        for (double i = double.NegativeInfinity; i <= 67.0; i++)
        {
            _counter++;
            i = double.NaN;
        }
        _counter--;
        // VNF_GE_UN
        for (double i = double.NegativeInfinity; i < 68.0; i++)
        {
            _counter++;
            i = double.NaN;
        }
        _counter--;

        // Negative infinities on the rhs
        // VNF_LT_UN
        for (double i = 69.0; i >= double.NegativeInfinity; i++)
        {
            _counter++;
            i = double.NaN;
        }
        _counter--;
        // VNF_LE_UN
        for (double i = 70.0; i > double.NegativeInfinity; i++)
        {
            _counter++;
            i = double.NaN;
        }
        _counter--;

        // Negative infinities on both sides
        // VNF_LT_UN
        for (double i = double.NegativeInfinity; i >= double.NegativeInfinity; i++)
        {
            _counter++;
            i = double.NaN;
        }
        _counter--;
        // VNF_GT_UN
        for (double i = double.NegativeInfinity; i <= double.NegativeInfinity; i++)
        {
            _counter++;
            i = double.NaN;
        }
        _counter--;
    }

    private static void TestSingleComparisonsEvaluatingToFalse()
    {
        // The following inverted conditions must be folded to "true".
        // Meaning the loop body must execute.
        // The "i = float.NaN" pattern is equivalent to "break".
        // We use it here as it is less likely to be optimized in the future before the loop condition is duplicated.

        // Basic scenarios
        // VNF_LT_UN
        for (float i = 71.0f; i >= 70.0f; i++)
        {
            _counter++;
            i = float.NaN;
        }
        _counter--;
        for (float i = 72.0f; i >= 72.0f; i++)
        {
            _counter++;
            i = float.NaN;
        }
        _counter--;
        // VNF_LE_UN
        for (float i = 73.0f; i > -74.0f; i++)
        {
            _counter++;
            i = float.NaN;
        }
        _counter--;
        // VNF_GT_UN
        for (float i = -75.0f; i <= 76.0f; i++)
        {
            _counter++;
            i = float.NaN;
        }
        _counter--;
        for (float i = -77.0f; i <= -77.0f; i++)
        {
            _counter++;
            i = float.NaN;
        }
        _counter--;
        // VNF_GE_UN
        for (float i = -79.0f; i < 78.0f; i++)
        {
            _counter++;
            i = float.NaN;
        }
        _counter--;

        // Positive infinities on the lhs
        // VNF_LT_UN
        for (float i = float.PositiveInfinity; i >= 80.0f; i++)
        {
            _counter++;
            i = float.NaN;
        }
        _counter--;
        // VNF_LE_UN
        for (float i = float.PositiveInfinity; i > -81.0f; i++)
        {
            _counter++;
            i = float.NaN;
        }
        _counter--;

        // Positive infinities on the rhs
        // VNF_GT_UN
        for (float i = -82.0f; i <= float.PositiveInfinity; i++)
        {
            _counter++;
            i = float.NaN;
        }
        _counter--;
        // VNF_GE_UN
        for (float i = -83.0f; i < float.PositiveInfinity; i++)
        {
            _counter++;
            i = float.NaN;
        }
        _counter--;

        // Positive infinities on both sides
        // VNF_LT_UN
        for (float i = float.PositiveInfinity; i >= float.PositiveInfinity; i++)
        {
            _counter++;
            i = float.NaN;
        }
        _counter--;
        // VNF_GT_UN
        for (float i = float.PositiveInfinity; i <= float.PositiveInfinity; i++)
        {
            _counter++;
            i = float.NaN;
        }
        _counter--;

        // Negative infinities on the lhs
        // VNF_GT_UN
        for (float i = float.NegativeInfinity; i <= 84.0f; i++)
        {
            _counter++;
            i = float.NaN;
        }
        _counter--;
        // VNF_GE_UN
        for (float i = float.NegativeInfinity; i < 85.0f; i++)
        {
            _counter++;
            i = float.NaN;
        }
        _counter--;

        // Negative infinities on the rhs
        // VNF_LT_UN
        for (float i = 86.0f; i >= float.NegativeInfinity; i++)
        {
            _counter++;
            i = float.NaN;
        }
        _counter--;
        // VNF_LE_UN
        for (float i = 87.0f; i > float.NegativeInfinity; i++)
        {
            _counter++;
            i = float.NaN;
        }
        _counter--;

        // Negative infinities on both sides
        // VNF_LT_UN
        for (float i = float.NegativeInfinity; i >= float.NegativeInfinity; i++)
        {
            _counter++;
            i = float.NaN;
        }
        _counter--;
        // VNF_GT_UN
        for (float i = float.NegativeInfinity; i <= float.NegativeInfinity; i++)
        {
            _counter++;
            i = float.NaN;
        }
        _counter--;
    }
}
