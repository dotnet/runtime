// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file exists to enable TensorPrimitives.Single.cs to be compiled for both
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
global using IndexOfMaxOperator_Single = System.Numerics.Tensors.TensorPrimitives.IndexOfMaxOperator<float>;
global using IndexOfMaxMagnitudeOperator_Single = System.Numerics.Tensors.TensorPrimitives.IndexOfMaxMagnitudeOperator<float>;
global using IndexOfMinOperator_Single = System.Numerics.Tensors.TensorPrimitives.IndexOfMinOperator<float>;
global using IndexOfMinMagnitudeOperator_Single = System.Numerics.Tensors.TensorPrimitives.IndexOfMinMagnitudeOperator<float>;
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

namespace System.Numerics.Tensors
{
    public static unsafe partial class TensorPrimitives
    {
        private static void InvokeSpanIntoSpan<TSingleUnaryOperator>(
            ReadOnlySpan<float> x, Span<float> destination)
            where TSingleUnaryOperator : struct, IUnaryOperator<float, float> =>
            InvokeSpanIntoSpan<float, TSingleUnaryOperator>(x, destination);

        private static void InvokeSpanSpanIntoSpan<TSingleBinaryOperator>(
            ReadOnlySpan<float> x, ReadOnlySpan<float> y, Span<float> destination)
            where TSingleBinaryOperator : struct, IBinaryOperator<float> =>
            InvokeSpanSpanIntoSpan<float, TSingleBinaryOperator>(x, y, destination);

        private static void InvokeSpanScalarIntoSpan<TSingleBinaryOperator>(
            ReadOnlySpan<float> x, float y, Span<float> destination)
            where TSingleBinaryOperator : struct, IBinaryOperator<float> =>
            InvokeSpanScalarIntoSpan<float, IdentityOperator<float>, TSingleBinaryOperator>(x, y, destination);

        private static unsafe void InvokeSpanScalarIntoSpan<TSingleTransformOperator, TSingleBinaryOperator>(
            ReadOnlySpan<float> x, float y, Span<float> destination)
            where TSingleTransformOperator : struct, IUnaryOperator<float, float>
            where TSingleBinaryOperator : struct, IBinaryOperator<float> =>
            InvokeSpanScalarIntoSpan<float, TSingleTransformOperator, TSingleBinaryOperator>(x, y, destination);

        private static unsafe void InvokeSpanSpanSpanIntoSpan<TSingleTernaryOperator>(
            ReadOnlySpan<float> x, ReadOnlySpan<float> y, ReadOnlySpan<float> z, Span<float> destination)
            where TSingleTernaryOperator : struct, ITernaryOperator<float> =>
            InvokeSpanSpanSpanIntoSpan<float, TSingleTernaryOperator>(x, y, z, destination);

        private static void InvokeSpanSpanScalarIntoSpan<TSingleTernaryOperator>(
            ReadOnlySpan<float> x, ReadOnlySpan<float> y, float z, Span<float> destination)
            where TSingleTernaryOperator : struct, ITernaryOperator<float> =>
            InvokeSpanSpanScalarIntoSpan<float, TSingleTernaryOperator>(x, y, z, destination);

        private static void InvokeSpanScalarSpanIntoSpan<TSingleTernaryOperator>(
            ReadOnlySpan<float> x, float y, ReadOnlySpan<float> z, Span<float> destination)
            where TSingleTernaryOperator : struct, ITernaryOperator<float> =>
            InvokeSpanScalarSpanIntoSpan<float, TSingleTernaryOperator>(x, y, z, destination);

        private static unsafe float Aggregate<TSingleTransformOperator, TSingleAggregationOperator>(
            ReadOnlySpan<float> x)
            where TSingleTransformOperator : struct, IUnaryOperator<float, float>
            where TSingleAggregationOperator : struct, IAggregationOperator<float> =>
            Aggregate<float, TSingleTransformOperator, TSingleAggregationOperator>(x);

        private static float Aggregate<TSingleBinaryOperator, TSingleAggregationOperator>(
            ReadOnlySpan<float> x, ReadOnlySpan<float> y)
            where TSingleBinaryOperator : struct, IBinaryOperator<float>
            where TSingleAggregationOperator : struct, IAggregationOperator<float> =>
            Aggregate<float, TSingleBinaryOperator, TSingleAggregationOperator>(x, y);

        private static float MinMaxCore<TSingleMinMaxOperator>(ReadOnlySpan<float> x)
            where TSingleMinMaxOperator : struct, IAggregationOperator<float> =>
            MinMaxCore<float, TSingleMinMaxOperator>(x);
    }
}
