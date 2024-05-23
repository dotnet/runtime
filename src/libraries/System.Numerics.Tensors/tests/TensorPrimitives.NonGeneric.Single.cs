// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit.Sdk;

// Tests specific to .NET Standard non-generic APIs

namespace System.Numerics.Tensors.Tests
{
    public unsafe class NonGenericSingleTensorPrimitivesTests : TensorPrimitivesTests<float>
    {
        protected override void Abs(ReadOnlySpan<float> x, Span<float> destination) => TensorPrimitives.Abs(x, destination);
        protected override float Abs(float x) => MathF.Abs(x);
        protected override void Add(ReadOnlySpan<float> x, ReadOnlySpan<float> y, Span<float> destination) => TensorPrimitives.Add(x, y, destination);
        protected override void Add(ReadOnlySpan<float> x, float y, Span<float> destination) => TensorPrimitives.Add(x, y, destination);
        protected override float Add(float x, float y) => x + y;
        protected override void AddMultiply(ReadOnlySpan<float> x, ReadOnlySpan<float> y, ReadOnlySpan<float> z, Span<float> destination) => TensorPrimitives.AddMultiply(x, y, z, destination);
        protected override void AddMultiply(ReadOnlySpan<float> x, ReadOnlySpan<float> y, float z, Span<float> destination) => TensorPrimitives.AddMultiply(x, y, z, destination);
        protected override void AddMultiply(ReadOnlySpan<float> x, float y, ReadOnlySpan<float> z, Span<float> destination) => TensorPrimitives.AddMultiply(x, y, z, destination);
        protected override float AddMultiply(float x, float y, float z) => (x + y) * z;
        protected override void Cosh(ReadOnlySpan<float> x, Span<float> destination) => TensorPrimitives.Cosh(x, destination);
        protected override float Cosh(float x) => MathF.Cosh(x);
        protected override float CosineSimilarity(ReadOnlySpan<float> x, ReadOnlySpan<float> y) => TensorPrimitives.CosineSimilarity(x, y);
        protected override float Distance(ReadOnlySpan<float> x, ReadOnlySpan<float> y) => TensorPrimitives.Distance(x, y);
        protected override void Divide(ReadOnlySpan<float> x, ReadOnlySpan<float> y, Span<float> destination) => TensorPrimitives.Divide(x, y, destination);
        protected override void Divide(ReadOnlySpan<float> x, float y, Span<float> destination) => TensorPrimitives.Divide(x, y, destination);
        protected override float Divide(float x, float y) => x / y;
        protected override float Dot(ReadOnlySpan<float> x, ReadOnlySpan<float> y) => TensorPrimitives.Dot(x, y);
        protected override void Exp(ReadOnlySpan<float> x, Span<float> destination) => TensorPrimitives.Exp(x, destination);
        protected override float Exp(float x) => MathF.Exp(x);
        protected override float Log(float x) => MathF.Log(x);
        protected override void Log(ReadOnlySpan<float> x, Span<float> destination) => TensorPrimitives.Log(x, destination);
        protected override float Log2(float x) => MathF.Log(x, 2);
        protected override void Log2(ReadOnlySpan<float> x, Span<float> destination) => TensorPrimitives.Log2(x, destination);
        protected override int IndexOfMax(ReadOnlySpan<float> x) => TensorPrimitives.IndexOfMax(x);
        protected override int IndexOfMaxMagnitude(ReadOnlySpan<float> x) => TensorPrimitives.IndexOfMaxMagnitude(x);
        protected override int IndexOfMin(ReadOnlySpan<float> x) => TensorPrimitives.IndexOfMin(x);
        protected override int IndexOfMinMagnitude(ReadOnlySpan<float> x) => TensorPrimitives.IndexOfMinMagnitude(x);
        protected override float Max(ReadOnlySpan<float> x) => TensorPrimitives.Max(x);
        protected override void Max(ReadOnlySpan<float> x, ReadOnlySpan<float> y, Span<float> destination) => TensorPrimitives.Max(x, y, destination);
        protected override float Max(float x, float y) => MathF.Max(x, y);
        protected override float MaxMagnitude(ReadOnlySpan<float> x) => TensorPrimitives.MaxMagnitude(x);
        protected override void MaxMagnitude(ReadOnlySpan<float> x, ReadOnlySpan<float> y, Span<float> destination) => TensorPrimitives.MaxMagnitude(x, y, destination);
        protected override float MaxMagnitude(float x, float y)
        {
            float ax = MathF.Abs(x), ay = MathF.Abs(y);
            return (ax > ay) || float.IsNaN(ax) || (ax == ay && *(int*)&x >= 0) ? x : y;
        }
        protected override float Min(ReadOnlySpan<float> x) => TensorPrimitives.Min(x);
        protected override void Min(ReadOnlySpan<float> x, ReadOnlySpan<float> y, Span<float> destination) => TensorPrimitives.Min(x, y, destination);
        protected override float Min(float x, float y) => MathF.Min(x, y);
        protected override float MinMagnitude(ReadOnlySpan<float> x) => TensorPrimitives.MinMagnitude(x);
        protected override void MinMagnitude(ReadOnlySpan<float> x, ReadOnlySpan<float> y, Span<float> destination) => TensorPrimitives.MinMagnitude(x, y, destination);
        protected override float MinMagnitude(float x, float y)
        {
            float ax = MathF.Abs(x), ay = MathF.Abs(y);
            return (ax < ay) || float.IsNaN(ax) || (ax == ay && *(int*)&x < 0) ? x : y;
        }
        protected override void Multiply(ReadOnlySpan<float> x, ReadOnlySpan<float> y, Span<float> destination) => TensorPrimitives.Multiply(x, y, destination);
        protected override void Multiply(ReadOnlySpan<float> x, float y, Span<float> destination) => TensorPrimitives.Multiply(x, y, destination);
        protected override float Multiply(float x, float y) => x * y;
        protected override void MultiplyAdd(ReadOnlySpan<float> x, ReadOnlySpan<float> y, ReadOnlySpan<float> z, Span<float> destination) => TensorPrimitives.MultiplyAdd(x, y, z, destination);
        protected override void MultiplyAdd(ReadOnlySpan<float> x, ReadOnlySpan<float> y, float z, Span<float> destination) => TensorPrimitives.MultiplyAdd(x, y, z, destination);
        protected override void MultiplyAdd(ReadOnlySpan<float> x, float y, ReadOnlySpan<float> z, Span<float> destination) => TensorPrimitives.MultiplyAdd(x, y, z, destination);
        protected override void Negate(ReadOnlySpan<float> x, Span<float> destination) => TensorPrimitives.Negate(x, destination);
        protected override float Norm(ReadOnlySpan<float> x) => TensorPrimitives.Norm(x);
        protected override float Product(ReadOnlySpan<float> x) => TensorPrimitives.Product(x);
        protected override float ProductOfSums(ReadOnlySpan<float> x, ReadOnlySpan<float> y) => TensorPrimitives.ProductOfSums(x, y);
        protected override float ProductOfDifferences(ReadOnlySpan<float> x, ReadOnlySpan<float> y) => TensorPrimitives.ProductOfDifferences(x, y);
        protected override void Sigmoid(ReadOnlySpan<float> x, Span<float> destination) => TensorPrimitives.Sigmoid(x, destination);
        protected override void Sinh(ReadOnlySpan<float> x, Span<float> destination) => TensorPrimitives.Sinh(x, destination);
        protected override float Sinh(float x) => MathF.Sinh(x);
        protected override void SoftMax(ReadOnlySpan<float> x, Span<float> destination) => TensorPrimitives.SoftMax(x, destination);
        protected override float Sqrt(float x) => MathF.Sqrt(x);
        protected override void Subtract(ReadOnlySpan<float> x, ReadOnlySpan<float> y, Span<float> destination) => TensorPrimitives.Subtract(x, y, destination);
        protected override void Subtract(ReadOnlySpan<float> x, float y, Span<float> destination) => TensorPrimitives.Subtract(x, y, destination);
        protected override float Subtract(float x, float y) => x - y;
        protected override float Sum(ReadOnlySpan<float> x) => TensorPrimitives.Sum(x);
        protected override float SumOfMagnitudes(ReadOnlySpan<float> x) => TensorPrimitives.SumOfMagnitudes(x);
        protected override float SumOfSquares(ReadOnlySpan<float> x) => TensorPrimitives.SumOfSquares(x);
        protected override void Tanh(ReadOnlySpan<float> x, Span<float> destination) => TensorPrimitives.Tanh(x, destination);
        protected override float Tanh(float x) => MathF.Tanh(x);

        protected override float ConvertFromSingle(float f) => f;

        protected override float NaN => float.NaN;
        protected override float NegativeZero => -0f;
        protected override float Zero => 0f;
        protected override float One => 1f;
        protected override float NegativeOne => -1f;
        protected override float MinValue => float.MinValue;

        protected override IEnumerable<(int Length, float Element)> VectorLengthAndIteratedRange(float min, float max, float increment)
        {
            foreach (int length in new[] { 4, 8, 16 })
            {
                for (float f = min; f <= max; f += increment)
                {
                    yield return (length, f);
                }
            }
        }

        protected override float NextRandom() => (float)((Random.NextDouble() * 2) - 1); // For testing purposes, get a mix of negative and positive values.

        protected override void AssertEqualTolerance(float expected, float actual, float? tolerance = null)
        {
            if (!Helpers.IsEqualWithTolerance(expected, actual, tolerance))
            {
                throw EqualException.ForMismatchedValues(expected, actual);
            }
        }

        protected override IEnumerable<float> GetSpecialValues()
        {
            // NaN
            yield return UInt32ToSingle(0xFFC0_0000); // -qNaN / float.NaN
            yield return UInt32ToSingle(0xFFFF_FFFF); // -qNaN / all-bits-set
            yield return UInt32ToSingle(0x7FC0_0000); // +qNaN
            yield return UInt32ToSingle(0xFFA0_0000); // -sNaN
            yield return UInt32ToSingle(0x7FA0_0000); // +sNaN

            // +Infinity, -Infinity
            yield return float.PositiveInfinity;
            yield return float.NegativeInfinity;

            // +Zero, -Zero
            yield return +0.0f;
            yield return -0.0f;

            // Subnormals
            yield return +float.Epsilon;
            yield return -float.Epsilon;
            yield return UInt32ToSingle(0x007F_FFFF);
            yield return UInt32ToSingle(0x807F_FFFF);

            // Normals
            yield return UInt32ToSingle(0x0080_0000);
            yield return UInt32ToSingle(0x8080_0000);
            yield return UInt32ToSingle(0x7F7F_FFFF); // MaxValue
            yield return UInt32ToSingle(0xFF7F_FFFF); // MinValue
        }

        protected override void SetSpecialValues(Span<float> x, Span<float> y)
        {
            int pos;

            // NaNs
            pos = Random.Next(x.Length);
            x[pos] = float.NaN;
            y[pos] = UInt32ToSingle(0x7FC0_0000);

            // +Infinity, -Infinity
            pos = Random.Next(x.Length);
            x[pos] = float.PositiveInfinity;
            y[pos] = float.NegativeInfinity;

            // +Zero, -Zero
            pos = Random.Next(x.Length);
            x[pos] = +0.0f;
            y[pos] = -0.0f;

            // +Epsilon, -Epsilon
            pos = Random.Next(x.Length);
            x[pos] = +float.Epsilon;
            y[pos] = -float.Epsilon;

            // Same magnitude, opposite sign
            pos = Random.Next(x.Length);
            x[pos] = +5.0f;
            y[pos] = -5.0f;
        }

        private static unsafe float UInt32ToSingle(uint i) => *(float*)&i;
    }
}
