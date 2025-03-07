// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Numerics.Tensors
{
    internal static class TensorOperation
    {
        public static void Invoke<TOperation, T>(in TensorSpan<T> x)
            where TOperation : TensorOperation.IOperation<T>
        {
            scoped Span<nint> indexes = RentedBuffer.Create(x.Rank, out nint linearOffset, out RentedBuffer rentedBuffer);

            for (nint i = 0; i < x.FlattenedLength; i++)
            {
                linearOffset = x._shape.AdjustToNextIndex(linearOffset, indexes);
                TOperation.Invoke(
                    ref Unsafe.Add(ref x._reference, linearOffset)
                );
            }

            rentedBuffer.Dispose();
        }

        public static void Invoke<TOperation, TArg, TResult>(in TensorSpan<TResult> destination, TArg scalar)
            where TOperation : TensorOperation.IUnaryOperation_Scalar<TArg, TResult>
        {
            scoped Span<nint> indexes = RentedBuffer.Create(destination.Rank, out nint linearOffset, out RentedBuffer rentedBuffer);

            for (nint i = 0; i < destination.FlattenedLength; i++)
            {
                linearOffset = destination._shape.AdjustToNextIndex(linearOffset, indexes);
                TOperation.Invoke(
                    ref Unsafe.Add(ref destination._reference, linearOffset),
                    scalar
                );
            }

            rentedBuffer.Dispose();
        }

        public static void Invoke<TOperation, TArg, TResult>(in ReadOnlyTensorSpan<TArg> x, Span<TResult> destination)
            where TOperation : TensorOperation.IUnaryOperation_Span<TArg, TResult>
        {
            scoped Span<nint> indexes = RentedBuffer.Create(x.Rank, out nint linearOffset, out RentedBuffer rentedBuffer);

            for (int i = 0; i < destination.Length; i++)
            {
                linearOffset = x._shape.AdjustToNextIndex(linearOffset, indexes);
                TOperation.Invoke(
                    in Unsafe.Add(ref x._reference, linearOffset),
                    ref destination[i]
                );
            }

            rentedBuffer.Dispose();
        }

        public static void Invoke<TOperation, TArg, TResult>(in ReadOnlyTensorSpan<TArg> x, in TensorSpan<TResult> destination)
            where TOperation : TensorOperation.IUnaryOperation_Span<TArg, TResult>
        {
            scoped Span<nint> xIndexes = RentedBuffer.Create(x.Rank, out nint xLinearOffset, out RentedBuffer xRentedBuffer);
            scoped Span<nint> destinationIndexes = RentedBuffer.Create(destination.Rank, out nint destinationLinearOffset, out RentedBuffer destinationRentedBuffer);

            for (nint i = 0; i < destination.FlattenedLength; i++)
            {
                xLinearOffset = x._shape.AdjustToNextIndex(xLinearOffset, xIndexes);
                destinationLinearOffset = destination._shape.AdjustToNextIndex(destinationLinearOffset, destinationIndexes);

                TOperation.Invoke(
                    in Unsafe.Add(ref x._reference, xLinearOffset),
                    ref Unsafe.Add(ref destination._reference, destinationLinearOffset)
                );
            }

            xRentedBuffer.Dispose();
            destinationRentedBuffer.Dispose();
        }

        public static void Invoke<TOperation, TArg, TResult>(in ReadOnlyTensorSpan<TArg> x, in ReadOnlyTensorSpan<TArg> y, in TensorSpan<TResult> destination)
            where TOperation : TensorOperation.IBinaryOperation_Tensor_Tensor<TArg, TResult>
        {
            scoped Span<nint> xIndexes = RentedBuffer.Create(x.Rank, out nint xLinearOffset, out RentedBuffer xRentedBuffer);
            scoped Span<nint> yIndexes = RentedBuffer.Create(y.Rank, out nint yLinearOffset, out RentedBuffer yRentedBuffer);
            scoped Span<nint> destinationIndexes = RentedBuffer.Create(destination.Rank, out nint destinationLinearOffset, out RentedBuffer destinationRentedBuffer);

            for (nint i = 0; i < destination.FlattenedLength; i++)
            {
                xLinearOffset = x._shape.AdjustToNextIndex(xLinearOffset, xIndexes);
                yLinearOffset = y._shape.AdjustToNextIndex(yLinearOffset, yIndexes);
                destinationLinearOffset = destination._shape.AdjustToNextIndex(destinationLinearOffset, destinationIndexes);

                TOperation.Invoke(
                    in Unsafe.Add(ref x._reference, xLinearOffset),
                    in Unsafe.Add(ref y._reference, yLinearOffset),
                    ref Unsafe.Add(ref destination._reference, destinationLinearOffset)
                );
            }

            xRentedBuffer.Dispose();
            yRentedBuffer.Dispose();
            destinationRentedBuffer.Dispose();
        }

        public static void Invoke<TOperation, TArg, TResult>(in ReadOnlyTensorSpan<TArg> x, TArg y, in TensorSpan<TResult> destination)
            where TOperation : TensorOperation.IBinaryOperation_Tensor_Tensor<TArg, TResult>
        {
            scoped Span<nint> xIndexes = RentedBuffer.Create(x.Rank, out nint xLinearOffset, out RentedBuffer xRentedBuffer);
            scoped Span<nint> destinationIndexes = RentedBuffer.Create(destination.Rank, out nint destinationLinearOffset, out RentedBuffer destinationRentedBuffer);

            for (nint i = 0; i < destination.FlattenedLength; i++)
            {
                xLinearOffset = x._shape.AdjustToNextIndex(xLinearOffset, xIndexes);
                destinationLinearOffset = destination._shape.AdjustToNextIndex(destinationLinearOffset, destinationIndexes);

                TOperation.Invoke(
                    in Unsafe.Add(ref x._reference, xLinearOffset),
                    y,
                    ref Unsafe.Add(ref destination._reference, destinationLinearOffset)
                );
            }

            xRentedBuffer.Dispose();
            destinationRentedBuffer.Dispose();
        }

        public static void ValidateCompatibility<TArg, TResult>(in ReadOnlyTensorSpan<TArg> x, in TensorSpan<TResult> destination)
        {
        }

        public static void ValidateCompatibility<TArg, TResult>(in ReadOnlyTensorSpan<TArg> x, in ReadOnlyTensorSpan<TArg> y, in TensorSpan<TResult> destination)
        {

        }

        public static void ValidateCompatibility<TArg, TResult>(in ReadOnlyTensorSpan<TArg> x, in ReadOnlyTensorSpan<TArg> y, out Tensor<TResult> destination)
        {

        }

        public readonly struct Clear<T>
            : IOperation<T>
        {
            public static void Invoke(ref T destination)
            {
                destination = default!;
            }

            public static void Invoke(Span<T> destination)
            {
                destination.Clear();
            }
        }

        public readonly struct CopyTo<T>
            : IUnaryOperation_Span<T, T>
        {
            public static void Invoke(ref readonly T source, ref T destination)
            {
                destination = source;
            }

            public static void Invoke(ReadOnlySpan<T> source, Span<T> destination)
            {
                source.CopyTo(destination);
            }
        }

        public readonly struct Equals<T>
            : IBinaryOperation_Tensor_Scalar<T, bool>,
              IBinaryOperation_Tensor_Tensor<T, bool>
            where T : IEqualityOperators<T, T, bool>
        {
            public static void Invoke(ref readonly T left, T right, ref bool destination)
            {
                destination = (left == right);
            }

            public static void Invoke(ref readonly T left, ref readonly T right, ref bool destination)
            {
                destination = (left == right);
            }

            public static void Invoke(ReadOnlySpan<T> left, T right, Span<bool> destination)
            {
                for (int i = 0; i < destination.Length; i++)
                {
                    destination[i] = (left[i] == right);
                }
            }

            public static void Invoke(ReadOnlySpan<T> left, ReadOnlySpan<T> right, Span<bool> destination)
            {
                for (int i = 0; i < destination.Length; i++)
                {
                    destination[i] = (left[i] == right[i]);
                }
            }
        }

        public readonly struct Fill<T>
            : IUnaryOperation_Scalar<T, T>
        {
            public static void Invoke(ref T destination, T value)
            {
                destination = value;
            }

            public static void Invoke(Span<T> destination, T value)
            {
                destination.Fill(value);
            }
        }

        public interface IBinaryOperation_Tensor_Scalar<T, TResult>
        {
            static abstract void Invoke(ref readonly T x, T y, ref TResult destination);
            static abstract void Invoke(ReadOnlySpan<T> x, T y, Span<TResult> destination);
        }

        public interface IBinaryOperation_Tensor_Tensor<T, TResult>
        {
            static abstract void Invoke(ref readonly T x, ref readonly T y, ref TResult destination);
            static abstract void Invoke(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<TResult> destination);
        }

        public interface IOperation<T>
        {
            static abstract void Invoke(ref T destination);
            static abstract void Invoke(Span<T> destination);
        }

        public interface IUnaryOperation_Scalar<T, TResult>
        {
            static abstract void Invoke(ref TResult destination, T x);
            static abstract void Invoke(Span<TResult> destination, T x);
        }

        public interface IUnaryOperation_Span<T, TResult>
        {
            static abstract void Invoke(ref readonly T x, ref TResult destination);
            static abstract void Invoke(ReadOnlySpan<T> x, Span<TResult> destination);
        }

        private ref struct RentedBuffer : IDisposable
        {
            private nint[]? _array;
            private TensorShape.InlineBuffer<nint> _inline;

            public static Span<nint> Create(int rank, out nint linearOffset, [UnscopedRef] out RentedBuffer rentedBuffer)
            {
                linearOffset = 0;

                if (rank > TensorShape.MaxInlineRank)
                {
                    rentedBuffer._array = ArrayPool<nint>.Shared.Rent(rank);
                    Unsafe.SkipInit(out rentedBuffer._inline);

                    rentedBuffer._array[rank - 1] = -1;
                    return rentedBuffer._array.AsSpan(0, rank);
                }
                else
                {
                    rentedBuffer._array = null;
                    rentedBuffer._inline = default;

                    rentedBuffer._inline[rank - 1] = -1;
                    return ((Span<nint>)rentedBuffer._inline)[..rank];
                }
            }

            public void Dispose()
            {
                if (_array is not null)
                {
                    ArrayPool<nint>.Shared.Return(_array);
                    _array = null;
                }
            }
        }
    }
}
