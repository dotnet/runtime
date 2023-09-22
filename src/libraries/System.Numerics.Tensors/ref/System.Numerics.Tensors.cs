// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        public static void Abs(System.ReadOnlySpan<float> x, System.Span<float> destination) { }
        public static void Add(System.ReadOnlySpan<float> x, System.ReadOnlySpan<float> y, System.Span<float> destination) { }
        public static void Add(System.ReadOnlySpan<float> x, float y, System.Span<float> destination) { }
        public static void AddMultiply(System.ReadOnlySpan<float> x, System.ReadOnlySpan<float> y, System.ReadOnlySpan<float> multiplier, System.Span<float> destination) { }
        public static void AddMultiply(System.ReadOnlySpan<float> x, System.ReadOnlySpan<float> y, float multiplier, System.Span<float> destination) { }
        public static void AddMultiply(System.ReadOnlySpan<float> x, float y, System.ReadOnlySpan<float> multiplier, System.Span<float> destination) { }
        public static void Cosh(System.ReadOnlySpan<float> x, System.Span<float> destination) { }
        public static float CosineSimilarity(System.ReadOnlySpan<float> x, System.ReadOnlySpan<float> y) { throw null; }
        public static float Distance(System.ReadOnlySpan<float> x, System.ReadOnlySpan<float> y) { throw null; }
        public static void Divide(System.ReadOnlySpan<float> x, System.ReadOnlySpan<float> y, System.Span<float> destination) { }
        public static void Divide(System.ReadOnlySpan<float> x, float y, System.Span<float> destination) { }
        public static float Dot(System.ReadOnlySpan<float> x, System.ReadOnlySpan<float> y) { throw null; }
        public static void Exp(System.ReadOnlySpan<float> x, System.Span<float> destination) { }
        public static int IndexOfMax(System.ReadOnlySpan<float> x) { throw null; }
        public static int IndexOfMaxMagnitude(System.ReadOnlySpan<float> x) { throw null; }
        public static int IndexOfMin(System.ReadOnlySpan<float> x) { throw null; }
        public static int IndexOfMinMagnitude(System.ReadOnlySpan<float> x) { throw null; }
        public static float Norm(System.ReadOnlySpan<float> x) { throw null; }
        public static void Log(System.ReadOnlySpan<float> x, System.Span<float> destination) { }
        public static void Log2(System.ReadOnlySpan<float> x, System.Span<float> destination) { }
        public static float Max(System.ReadOnlySpan<float> x) { throw null; }
        public static void Max(System.ReadOnlySpan<float> x, System.ReadOnlySpan<float> y, System.Span<float> destination) { throw null; }
        public static float MaxMagnitude(System.ReadOnlySpan<float> x) { throw null; }
        public static void MaxMagnitude(System.ReadOnlySpan<float> x, System.ReadOnlySpan<float> y, System.Span<float> destination) { throw null; }
        public static float Min(System.ReadOnlySpan<float> x) { throw null; }
        public static void Min(System.ReadOnlySpan<float> x, System.ReadOnlySpan<float> y, System.Span<float> destination) { throw null; }
        public static float MinMagnitude(System.ReadOnlySpan<float> x) { throw null; }
        public static void MinMagnitude(System.ReadOnlySpan<float> x, System.ReadOnlySpan<float> y, System.Span<float> destination) { throw null; }
        public static void Multiply(System.ReadOnlySpan<float> x, System.ReadOnlySpan<float> y, System.Span<float> destination) { }
        public static void Multiply(System.ReadOnlySpan<float> x, float y, System.Span<float> destination) { }
        public static void MultiplyAdd(System.ReadOnlySpan<float> x, System.ReadOnlySpan<float> y, System.ReadOnlySpan<float> addend, System.Span<float> destination) { }
        public static void MultiplyAdd(System.ReadOnlySpan<float> x, System.ReadOnlySpan<float> y, float addend, System.Span<float> destination) { }
        public static void MultiplyAdd(System.ReadOnlySpan<float> x, float y, System.ReadOnlySpan<float> addend, System.Span<float> destination) { }
        public static void Negate(System.ReadOnlySpan<float> x, System.Span<float> destination) { }
        public static float Product(System.ReadOnlySpan<float> x) { throw null; }
        public static float ProductOfDifferences(System.ReadOnlySpan<float> x, System.ReadOnlySpan<float> y) { throw null; }
        public static float ProductOfSums(System.ReadOnlySpan<float> x, System.ReadOnlySpan<float> y) { throw null; }
        public static void Sigmoid(System.ReadOnlySpan<float> x, System.Span<float> destination) { }
        public static void Sinh(System.ReadOnlySpan<float> x, System.Span<float> destination) { }
        public static void SoftMax(System.ReadOnlySpan<float> x, System.Span<float> destination) { }
        public static void Subtract(System.ReadOnlySpan<float> x, System.ReadOnlySpan<float> y, System.Span<float> destination) { }
        public static void Subtract(System.ReadOnlySpan<float> x, float y, System.Span<float> destination) { }
        public static float Sum(System.ReadOnlySpan<float> x) { throw null; }
        public static float SumOfMagnitudes(System.ReadOnlySpan<float> x) { throw null; }
        public static float SumOfSquares(System.ReadOnlySpan<float> x) { throw null; }
        public static void Tanh(System.ReadOnlySpan<float> x, System.Span<float> destination) { }
    }
}
