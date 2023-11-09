// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file exists to enable TensorPrimitives.float.cs to be compiled for both
// netstandard2.0 and net8.0+ targets. It uses the XX_Single names and the operation
// methods tied to float, whereas the net8.0+ worker implementations use generic math.
// This file provides float-bound types and type defs that route one to the other.

global using AbsoluteOperator_Single = System.Numerics.Tensors.TensorPrimitives.AbsoluteOperator<float>;
global using AddOperator_Single = System.Numerics.Tensors.TensorPrimitives.AddOperator<float>;
global using AddMultiplyOperator_Single = System.Numerics.Tensors.TensorPrimitives.AddMultiplyOperator<float>;
global using CoshOperator_Single = System.Numerics.Tensors.TensorPrimitives.CoshOperator<float>;
global using SubtractSquaredOperator_Single = System.Numerics.Tensors.TensorPrimitives.SubtractSquaredOperator<float>;
global using DivideOperator_Single = System.Numerics.Tensors.TensorPrimitives.DivideOperator<float>;
global using MultiplyOperator_Single = System.Numerics.Tensors.TensorPrimitives.MultiplyOperator<float>;
global using ExpOperator_Single = System.Numerics.Tensors.TensorPrimitives.ExpOperator<float>;
global using LogOperator_Single = System.Numerics.Tensors.TensorPrimitives.LogOperator<float>;
global using Log2Operator_Single = System.Numerics.Tensors.TensorPrimitives.Log2Operator<float>;
global using MaxOperator_Single = System.Numerics.Tensors.TensorPrimitives.MaxOperator<float>;
global using MaxPropagateNaNOperator_Single = System.Numerics.Tensors.TensorPrimitives.MaxPropagateNaNOperator<float>;
global using MaxMagnitudeOperator_Single = System.Numerics.Tensors.TensorPrimitives.MaxMagnitudeOperator<float>;
global using MaxMagnitudePropagateNaNOperator_Single = System.Numerics.Tensors.TensorPrimitives.MaxMagnitudePropagateNaNOperator<float>;
global using MinOperator_Single = System.Numerics.Tensors.TensorPrimitives.MinOperator<float>;
global using MinPropagateNaNOperator_Single = System.Numerics.Tensors.TensorPrimitives.MinPropagateNaNOperator<float>;
global using MinMagnitudeOperator_Single = System.Numerics.Tensors.TensorPrimitives.MinMagnitudeOperator<float>;
global using MinMagnitudePropagateNaNOperator_Single = System.Numerics.Tensors.TensorPrimitives.MinMagnitudePropagateNaNOperator<float>;
global using MultiplyAddOperator_Single = System.Numerics.Tensors.TensorPrimitives.MultiplyAddOperator<float>;
global using NegateOperator_Single = System.Numerics.Tensors.TensorPrimitives.NegateOperator<float>;
global using IdentityOperator_Single = System.Numerics.Tensors.TensorPrimitives.IdentityOperator<float>;
global using SubtractOperator_Single = System.Numerics.Tensors.TensorPrimitives.SubtractOperator<float>;
global using SigmoidOperator_Single = System.Numerics.Tensors.TensorPrimitives.SigmoidOperator<float>;
global using SinhOperator_Single = System.Numerics.Tensors.TensorPrimitives.SinhOperator<float>;
global using SquaredOperator_Single = System.Numerics.Tensors.TensorPrimitives.SquaredOperator<float>;
global using TanhOperator_Single = System.Numerics.Tensors.TensorPrimitives.TanhOperator<float>;

// TODO: These should be made generic. Their implementations are still currently bound to float.
global using IndexOfMaxOperator_Single = System.Numerics.Tensors.TensorPrimitives.IndexOfMaxOperator;
global using IndexOfMaxMagnitudeOperator_Single = System.Numerics.Tensors.TensorPrimitives.IndexOfMaxMagnitudeOperator;
global using IndexOfMinOperator_Single = System.Numerics.Tensors.TensorPrimitives.IndexOfMinOperator;
global using IndexOfMinMagnitudeOperator_Single = System.Numerics.Tensors.TensorPrimitives.IndexOfMinMagnitudeOperator;

namespace System.Numerics.Tensors
{
    public static unsafe partial class TensorPrimitives
    {
        private static void InvokeSpanIntoSpan<TFloatUnaryOperator>(
            ReadOnlySpan<float> x, Span<float> destination)
            where TFloatUnaryOperator : struct, IUnaryOperator<float> =>
            InvokeSpanIntoSpan<float, TFloatUnaryOperator>(x, destination);

        private static void InvokeSpanSpanIntoSpan<TFloatBinaryOperator>(
            ReadOnlySpan<float> x, ReadOnlySpan<float> y, Span<float> destination)
            where TFloatBinaryOperator : struct, IBinaryOperator<float> =>
            InvokeSpanSpanIntoSpan<float, TFloatBinaryOperator>(x, y, destination);

        private static void InvokeSpanScalarIntoSpan<TFloatBinaryOperator>(
            ReadOnlySpan<float> x, float y, Span<float> destination)
            where TFloatBinaryOperator : struct, IBinaryOperator<float> =>
            InvokeSpanScalarIntoSpan<float, IdentityOperator<float>, TFloatBinaryOperator>(x, y, destination);

        private static unsafe void InvokeSpanScalarIntoSpan<TFloatTransformOperator, TFloatBinaryOperator>(
            ReadOnlySpan<float> x, float y, Span<float> destination)
            where TFloatTransformOperator : struct, IUnaryOperator<float>
            where TFloatBinaryOperator : struct, IBinaryOperator<float> =>
            InvokeSpanScalarIntoSpan<float, TFloatTransformOperator, TFloatBinaryOperator>(x, y, destination);

        private static unsafe void InvokeSpanSpanSpanIntoSpan<TFloatTernaryOperator>(
            ReadOnlySpan<float> x, ReadOnlySpan<float> y, ReadOnlySpan<float> z, Span<float> destination)
            where TFloatTernaryOperator : struct, ITernaryOperator<float> =>
            InvokeSpanSpanSpanIntoSpan<float, TFloatTernaryOperator>(x, y, z, destination);

        private static void InvokeSpanSpanScalarIntoSpan<TFloatTernaryOperator>(
            ReadOnlySpan<float> x, ReadOnlySpan<float> y, float z, Span<float> destination)
            where TFloatTernaryOperator : struct, ITernaryOperator<float> =>
            InvokeSpanSpanScalarIntoSpan<float, TFloatTernaryOperator>(x, y, z, destination);

        private static void InvokeSpanScalarSpanIntoSpan<TFloatTernaryOperator>(
            ReadOnlySpan<float> x, float y, ReadOnlySpan<float> z, Span<float> destination)
            where TFloatTernaryOperator : struct, ITernaryOperator<float> =>
            InvokeSpanScalarSpanIntoSpan<float, TFloatTernaryOperator>(x, y, z, destination);

        private static unsafe float Aggregate<TFloatTransformOperator, TFloatAggregationOperator>(
            ReadOnlySpan<float> x)
            where TFloatTransformOperator : struct, IUnaryOperator<float>
            where TFloatAggregationOperator : struct, IAggregationOperator<float> =>
            Aggregate<float, TFloatTransformOperator, TFloatAggregationOperator>(x);

        private static float Aggregate<TFloatBinaryOperator, TFloatAggregationOperator>(
            ReadOnlySpan<float> x, ReadOnlySpan<float> y)
            where TFloatBinaryOperator : struct, IBinaryOperator<float>
            where TFloatAggregationOperator : struct, IAggregationOperator<float> =>
            Aggregate<float, TFloatBinaryOperator, TFloatAggregationOperator>(x, y);

        private static float MinMaxCore<TFloatMinMaxOperator>(ReadOnlySpan<float> x)
            where TFloatMinMaxOperator : struct, IAggregationOperator<float> =>
            MinMaxCore<float, TFloatMinMaxOperator>(x);
    }
}
