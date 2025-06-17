// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        public static void ConvertToIntegerNative<TFrom, TTo>(System.ReadOnlySpan<TFrom> source, System.Span<TTo> destination) where TFrom : System.Numerics.IFloatingPoint<TFrom> where TTo : System.Numerics.IBinaryInteger<TTo> { }
        public static void ConvertToInteger<TFrom, TTo>(System.ReadOnlySpan<TFrom> source, System.Span<TTo> destination) where TFrom : System.Numerics.IFloatingPoint<TFrom> where TTo : System.Numerics.IBinaryInteger<TTo> { }
    }
    public readonly ref partial struct ReadOnlyTensorDimensionSpan<T>
    {
        public ref partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Numerics.Tensors.ReadOnlyTensorSpan<T>>, System.Collections.IEnumerator, System.IDisposable
        {
            readonly object? System.Collections.IEnumerator.Current { get { throw null; } }
            void System.IDisposable.Dispose() { }
        }
    }
    public readonly ref partial struct TensorDimensionSpan<T>
    {
        public ref partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Numerics.Tensors.TensorSpan<T>>, System.Collections.IEnumerator, System.IDisposable
        {
            readonly object? System.Collections.IEnumerator.Current { get { throw null; } }
            void System.IDisposable.Dispose() { }
        }
    }
}
