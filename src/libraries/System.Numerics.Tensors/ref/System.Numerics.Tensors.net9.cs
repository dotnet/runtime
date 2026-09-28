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
    public readonly ref partial struct ReadOnlyTensorSpan<T> : System.Numerics.Tensors.IReadOnlyTensor, System.Numerics.Tensors.IReadOnlyTensor<System.Numerics.Tensors.ReadOnlyTensorSpan<T>, T>
    {
        object? System.Numerics.Tensors.IReadOnlyTensor.this[params scoped System.ReadOnlySpan<System.Buffers.NIndex> indexes] { get { throw null; } }
        object? System.Numerics.Tensors.IReadOnlyTensor.this[params scoped System.ReadOnlySpan<nint> indexes] { get { throw null; } }
        System.Numerics.Tensors.ReadOnlyTensorSpan<T> System.Numerics.Tensors.IReadOnlyTensor<System.Numerics.Tensors.ReadOnlyTensorSpan<T>, T>.AsReadOnlyTensorSpan() { throw null; }
        System.Numerics.Tensors.ReadOnlyTensorSpan<T> System.Numerics.Tensors.IReadOnlyTensor<System.Numerics.Tensors.ReadOnlyTensorSpan<T>, T>.AsReadOnlyTensorSpan(params scoped System.ReadOnlySpan<nint> startIndexes) { throw null; }
        System.Numerics.Tensors.ReadOnlyTensorSpan<T> System.Numerics.Tensors.IReadOnlyTensor<System.Numerics.Tensors.ReadOnlyTensorSpan<T>, T>.AsReadOnlyTensorSpan(params scoped System.ReadOnlySpan<System.Buffers.NIndex> startIndexes) { throw null; }
        System.Numerics.Tensors.ReadOnlyTensorSpan<T> System.Numerics.Tensors.IReadOnlyTensor<System.Numerics.Tensors.ReadOnlyTensorSpan<T>, T>.AsReadOnlyTensorSpan(params scoped System.ReadOnlySpan<System.Buffers.NRange> ranges) { throw null; }
        System.Numerics.Tensors.ReadOnlyTensorSpan<T> System.Numerics.Tensors.IReadOnlyTensor<System.Numerics.Tensors.ReadOnlyTensorSpan<T>, T>.ToDenseTensor() { throw null; }
    }
    public readonly ref partial struct TensorDimensionSpan<T>
    {
        public ref partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Numerics.Tensors.TensorSpan<T>>, System.Collections.IEnumerator, System.IDisposable
        {
            readonly object? System.Collections.IEnumerator.Current { get { throw null; } }
            void System.IDisposable.Dispose() { }
        }
    }
    public readonly ref partial struct TensorSpan<T> : System.Numerics.Tensors.IReadOnlyTensor, System.Numerics.Tensors.IReadOnlyTensor<System.Numerics.Tensors.TensorSpan<T>, T>, System.Numerics.Tensors.ITensor, System.Numerics.Tensors.ITensor<System.Numerics.Tensors.TensorSpan<T>, T>
    {
        object? System.Numerics.Tensors.IReadOnlyTensor.this[params scoped System.ReadOnlySpan<System.Buffers.NIndex> indexes] { get { throw null; } }
        object? System.Numerics.Tensors.IReadOnlyTensor.this[params scoped System.ReadOnlySpan<nint> indexes] { get { throw null; } }
        ref readonly T System.Numerics.Tensors.IReadOnlyTensor<System.Numerics.Tensors.TensorSpan<T>, T>.this[params scoped System.ReadOnlySpan<System.Buffers.NIndex> indexes] { get { throw null; } }
        ref readonly T System.Numerics.Tensors.IReadOnlyTensor<System.Numerics.Tensors.TensorSpan<T>, T>.this[params scoped System.ReadOnlySpan<nint> indexes] { get { throw null; } }
        System.Numerics.Tensors.ReadOnlyTensorSpan<T> System.Numerics.Tensors.IReadOnlyTensor<System.Numerics.Tensors.TensorSpan<T>, T>.AsReadOnlyTensorSpan() { throw null; }
        System.Numerics.Tensors.ReadOnlyTensorSpan<T> System.Numerics.Tensors.IReadOnlyTensor<System.Numerics.Tensors.TensorSpan<T>, T>.AsReadOnlyTensorSpan(params scoped System.ReadOnlySpan<nint> startIndexes) { throw null; }
        System.Numerics.Tensors.ReadOnlyTensorSpan<T> System.Numerics.Tensors.IReadOnlyTensor<System.Numerics.Tensors.TensorSpan<T>, T>.AsReadOnlyTensorSpan(params scoped System.ReadOnlySpan<System.Buffers.NIndex> startIndexes) { throw null; }
        System.Numerics.Tensors.ReadOnlyTensorSpan<T> System.Numerics.Tensors.IReadOnlyTensor<System.Numerics.Tensors.TensorSpan<T>, T>.AsReadOnlyTensorSpan(params scoped System.ReadOnlySpan<System.Buffers.NRange> ranges) { throw null; }
        System.Numerics.Tensors.ReadOnlyTensorDimensionSpan<T> System.Numerics.Tensors.IReadOnlyTensor<System.Numerics.Tensors.TensorSpan<T>, T>.GetDimensionSpan(int dimension) { throw null; }
        ref readonly T System.Numerics.Tensors.IReadOnlyTensor<System.Numerics.Tensors.TensorSpan<T>, T>.GetPinnableReference() { throw null; }
        System.ReadOnlySpan<T> System.Numerics.Tensors.IReadOnlyTensor<System.Numerics.Tensors.TensorSpan<T>, T>.GetSpan(scoped System.ReadOnlySpan<nint> startIndexes, int length) { throw null; }
        System.ReadOnlySpan<T> System.Numerics.Tensors.IReadOnlyTensor<System.Numerics.Tensors.TensorSpan<T>, T>.GetSpan(scoped System.ReadOnlySpan<System.Buffers.NIndex> startIndexes, int length) { throw null; }
        System.Numerics.Tensors.TensorSpan<T> System.Numerics.Tensors.IReadOnlyTensor<System.Numerics.Tensors.TensorSpan<T>, T>.ToDenseTensor() { throw null; }

        bool System.Numerics.Tensors.ITensor.IsReadOnly { get { throw null; } }
        object? System.Numerics.Tensors.ITensor.this[params scoped System.ReadOnlySpan<System.Buffers.NIndex> indexes] { get { throw null; } set { } }
        object? System.Numerics.Tensors.ITensor.this[params scoped System.ReadOnlySpan<nint> indexes] { get { throw null; } set { } }
        void System.Numerics.Tensors.ITensor.Fill(object value) { }
        static System.Numerics.Tensors.TensorSpan<T> ITensor<TensorSpan<T>, T>.CreateFromShape(scoped System.ReadOnlySpan<nint> lengths, bool pinned) { throw null; }
        static System.Numerics.Tensors.TensorSpan<T> ITensor<TensorSpan<T>, T>.CreateFromShape(scoped System.ReadOnlySpan<nint> lengths, scoped System.ReadOnlySpan<nint> strides, bool pinned) { throw null; }
        static System.Numerics.Tensors.TensorSpan<T> ITensor<TensorSpan<T>, T>.CreateFromShapeUninitialized(scoped System.ReadOnlySpan<nint> lengths, bool pinned) { throw null; }
        static System.Numerics.Tensors.TensorSpan<T> ITensor<TensorSpan<T>, T>.CreateFromShapeUninitialized(scoped System.ReadOnlySpan<nint> lengths, scoped System.ReadOnlySpan<nint> strides, bool pinned) { throw null; }
        System.Numerics.Tensors.TensorSpan<T> System.Numerics.Tensors.ITensor<System.Numerics.Tensors.TensorSpan<T>, T>.AsTensorSpan() { throw null; }
        System.Numerics.Tensors.TensorSpan<T> System.Numerics.Tensors.ITensor<System.Numerics.Tensors.TensorSpan<T>, T>.AsTensorSpan(params scoped System.ReadOnlySpan<nint> startIndexes) { throw null; }
        System.Numerics.Tensors.TensorSpan<T> System.Numerics.Tensors.ITensor<System.Numerics.Tensors.TensorSpan<T>, T>.AsTensorSpan(params scoped System.ReadOnlySpan<System.Buffers.NIndex> startIndexes) { throw null; }
        System.Numerics.Tensors.TensorSpan<T> System.Numerics.Tensors.ITensor<System.Numerics.Tensors.TensorSpan<T>, T>.AsTensorSpan(params scoped System.ReadOnlySpan<System.Buffers.NRange> ranges) { throw null; }
    }
}
