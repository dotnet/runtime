// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Numerics.Tensors
{
    internal static class TensorOperation
    {
        public static void Invoke<TOperation, T>(in TensorSpan<T> x)
            where TOperation : IOperation<T>
        {
            scoped Span<nint> indexes = RentedBuffer.Create(x.Rank, x.Strides, out nint linearOffset, out RentedBuffer<nint> rentedBuffer);

            for (nint i = 0; i < x.FlattenedLength; i++)
            {
                linearOffset = x._shape.AdjustToNextIndex(x._shape, linearOffset, indexes);
                TOperation.Invoke(
                    ref Unsafe.Add(ref x._reference, linearOffset)
                );
            }

            rentedBuffer.Dispose();
        }

        public static bool Invoke<TOperation, TArg>(in ReadOnlyTensorSpan<TArg> x, in ReadOnlyTensorSpan<TArg> y)
            where TOperation : IBinaryOperation_Tensor_Tensor<TArg, bool>
        {
            bool result = false;

            ref readonly TensorShape destinationShape = ref ((x._shape.FlattenedLength > y._shape.FlattenedLength) ? ref x._shape : ref y._shape);
            scoped Span<nint> xIndexes = RentedBuffer.Create(destinationShape.Rank, x.Strides, out nint xLinearOffset, out RentedBuffer<nint> xRentedBuffer);
            scoped Span<nint> yIndexes = RentedBuffer.Create(destinationShape.Rank, y.Strides, out nint yLinearOffset, out RentedBuffer<nint> yRentedBuffer);

            for (nint i = 0; i < destinationShape.FlattenedLength; i++)
            {
                xLinearOffset = x._shape.AdjustToNextIndex(destinationShape, xLinearOffset, xIndexes);
                yLinearOffset = y._shape.AdjustToNextIndex(destinationShape, yLinearOffset, yIndexes);

                 TOperation.Invoke(
                    in Unsafe.Add(ref x._reference, xLinearOffset),
                    in Unsafe.Add(ref y._reference, yLinearOffset),
                    ref result
                );

                if (!result)
                {
                    break;
                }
            }

            xRentedBuffer.Dispose();
            yRentedBuffer.Dispose();

            return result;
        }

        public static bool Invoke<TOperation, TArg>(in ReadOnlyTensorSpan<TArg> x, TArg y)
            where TOperation : IBinaryOperation_Tensor_Scalar<TArg, bool>
        {
            bool result = false;

            scoped Span<nint> xIndexes = RentedBuffer.Create(x.Rank, x.Strides, out nint xLinearOffset, out RentedBuffer<nint> xRentedBuffer);

            for (nint i = 0; i < x.FlattenedLength; i++)
            {
                xLinearOffset = x._shape.AdjustToNextIndex(x._shape, xLinearOffset, xIndexes);

                TOperation.Invoke(
                    in Unsafe.Add(ref x._reference, xLinearOffset),
                    y,
                    ref result
                );

                if (!result)
                {
                    return false;
                }
            }

            xRentedBuffer.Dispose();

            return result;
        }

        public static void Invoke<TOperation, TArg, TResult>(in TensorSpan<TResult> destination, TArg scalar)
            where TOperation : IUnaryOperation_Scalar<TArg, TResult>
        {
            scoped Span<nint> indexes = RentedBuffer.Create(destination.Rank, destination.Strides, out nint linearOffset, out RentedBuffer<nint> rentedBuffer);

            for (nint i = 0; i < destination.FlattenedLength; i++)
            {
                linearOffset = destination._shape.AdjustToNextIndex(destination._shape, linearOffset, indexes);
                TOperation.Invoke(
                    ref Unsafe.Add(ref destination._reference, linearOffset),
                    scalar
                );
            }

            rentedBuffer.Dispose();
        }

        public static void Invoke<TOperation, TArg, TResult>(in ReadOnlyTensorSpan<TArg> x, in TensorSpan<TResult> destination)
            where TOperation : IUnaryOperation_Tensor<TArg, TResult>
        {
            scoped Span<nint> xIndexes = RentedBuffer.Create(destination.Rank, x.Strides, out nint xLinearOffset, out RentedBuffer<nint> xRentedBuffer);
            scoped Span<nint> destinationIndexes = RentedBuffer.Create(destination.Rank, destination.Strides, out nint destinationLinearOffset, out RentedBuffer<nint> destinationRentedBuffer);

            for (nint i = 0; i < destination.FlattenedLength; i++)
            {
                xLinearOffset = x._shape.AdjustToNextIndex(destination._shape, xLinearOffset, xIndexes);
                destinationLinearOffset = destination._shape.AdjustToNextIndex(destination._shape, destinationLinearOffset, destinationIndexes);

                TOperation.Invoke(
                    in Unsafe.Add(ref x._reference, xLinearOffset),
                    ref Unsafe.Add(ref destination._reference, destinationLinearOffset)
                );
            }

            xRentedBuffer.Dispose();
            destinationRentedBuffer.Dispose();
        }

        public static void ReverseInvoke<TOperation, TArg, TResult>(in ReadOnlyTensorSpan<TArg> x, in TensorSpan<TResult> destination)
            where TOperation : IUnaryOperation_Tensor<TArg, TResult>
        {
            scoped Span<nint> xIndexes = RentedBuffer.Create(destination.Rank, x.Strides, out nint xLinearOffset, out RentedBuffer<nint> xRentedBuffer);
            scoped Span<nint> destinationIndexes = RentedBuffer.Create(destination.Rank, destination.Strides, out nint _, out RentedBuffer<nint> destinationRentedBuffer);

            destinationIndexes[0] = destination.Lengths[0];
            for (int i = 1; i < destinationIndexes.Length; i++)
            {
                destinationIndexes[i] = destination.Lengths[i] - 1;
            }
            nint destinationLinearOffset = destination._shape.LinearLength;

            for (nint i = 0; i < destination.FlattenedLength; i++)
            {
                xLinearOffset = x._shape.AdjustToNextIndex(destination._shape, xLinearOffset, xIndexes);
                destinationLinearOffset = destination._shape.AdjustToPreviousIndex(destination._shape, destinationLinearOffset, destinationIndexes);

                TOperation.Invoke(
                    in Unsafe.Add(ref x._reference, xLinearOffset),
                    ref Unsafe.Add(ref destination._reference, destinationLinearOffset)
                );
            }

            xRentedBuffer.Dispose();
            destinationRentedBuffer.Dispose();
        }

        // For copyto/flattento
        public static void Invoke<TOperation, TArg, TResult>(in ReadOnlyTensorSpan<TArg> x, in Span<TResult> destination)
            where TOperation : IUnaryOperation_Tensor<TArg, TResult>
        {
            scoped Span<nint> xIndexes = RentedBuffer.Create(x.Rank, x.Strides, out nint xLinearOffset, out RentedBuffer<nint> xRentedBuffer);
            nint destinationIndex = -1;

            for (nint i = 0; i < destination.Length; i++)
            {
                xLinearOffset = x._shape.AdjustToNextIndex(x._shape, xLinearOffset, xIndexes);
                destinationIndex++;

                TOperation.Invoke(
                    in Unsafe.Add(ref x._reference, xLinearOffset),
                    ref Unsafe.Add(ref destination[0], destinationIndex)
                );
            }

            xRentedBuffer.Dispose();
        }

        public static void Invoke<TOperation, TArg, TResult>(in ReadOnlyTensorSpan<TArg> x, ref TResult destination)
            where TOperation : IUnaryReduction_Tensor<TArg, TResult>
        {
            scoped Span<nint> xIndexes = RentedBuffer.Create(x.Rank, x.Strides, out nint xLinearOffset, out RentedBuffer<nint> xRentedBuffer);

            for (nint i = 0; i < x.FlattenedLength; i++)
            {
                xLinearOffset = x._shape.AdjustToNextIndex(x._shape, xLinearOffset, xIndexes);

                TOperation.Invoke(
                    in Unsafe.Add(ref x._reference, xLinearOffset),
                    ref destination
                );
            }

            xRentedBuffer.Dispose();
        }

        public static void Invoke<TOperation, TArg1, TArg2, TResult>(in ReadOnlyTensorSpan<TArg1> x, in ReadOnlyTensorSpan<TArg2> y, in TensorSpan<TResult> destination)
            where TOperation : IBinaryOperation_Tensor_Tensor<TArg1, TArg2, TResult>
        {
            scoped Span<nint> xIndexes = RentedBuffer.Create(destination.Rank, x.Strides, out nint xLinearOffset, out RentedBuffer<nint> xRentedBuffer);
            scoped Span<nint> yIndexes = RentedBuffer.Create(destination.Rank, y.Strides, out nint yLinearOffset, out RentedBuffer<nint> yRentedBuffer);
            scoped Span<nint> destinationIndexes = RentedBuffer.Create(destination.Rank, destination.Strides, out nint destinationLinearOffset, out RentedBuffer<nint> destinationRentedBuffer);

            for (nint i = 0; i < destination.FlattenedLength; i++)
            {
                xLinearOffset = x._shape.AdjustToNextIndex(destination._shape, xLinearOffset, xIndexes);
                yLinearOffset = y._shape.AdjustToNextIndex(destination._shape, yLinearOffset, yIndexes);
                destinationLinearOffset = destination._shape.AdjustToNextIndex(destination._shape, destinationLinearOffset, destinationIndexes);

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

        public static void Invoke<TOperation, TArg, TResult>(in ReadOnlyTensorSpan<TArg> x, in ReadOnlyTensorSpan<TArg> y, in TensorSpan<TResult> destination)
            where TOperation : IBinaryOperation_Tensor_Tensor<TArg, TResult>
        => Invoke<TOperation, TArg, TArg, TResult>(in x, in y, in destination);

        public static void Invoke<TOperation, TArg, TResult>(in ReadOnlyTensorSpan<TArg> x, in ReadOnlyTensorSpan<TArg> y, ref TResult result)
            where TOperation : IBinaryOperation_Tensor_Tensor<TArg, TResult>
        {
            ref readonly TensorShape destinationShape = ref ((x._shape.FlattenedLength > y._shape.FlattenedLength) ? ref x._shape : ref y._shape);

            scoped Span<nint> xIndexes = RentedBuffer.Create(destinationShape.Rank, x.Strides, out nint xLinearOffset, out RentedBuffer<nint> xRentedBuffer);
            scoped Span<nint> yIndexes = RentedBuffer.Create(destinationShape.Rank, y.Strides, out nint yLinearOffset, out RentedBuffer<nint> yRentedBuffer);

            for (nint i = 0; i < destinationShape.FlattenedLength; i++)
            {
                xLinearOffset = x._shape.AdjustToNextIndex(destinationShape, xLinearOffset, xIndexes);
                yLinearOffset = y._shape.AdjustToNextIndex(destinationShape, yLinearOffset, yIndexes);

                TOperation.Invoke(
                    in Unsafe.Add(ref x._reference, xLinearOffset),
                    in Unsafe.Add(ref y._reference, yLinearOffset),
                    ref result
                );
            }

            xRentedBuffer.Dispose();
            yRentedBuffer.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Invoke<TOperation, TArg, TResult>(in ReadOnlyTensorSpan<TArg> x, TArg y, in TensorSpan<TResult> destination)
            where TOperation : IBinaryOperation_Tensor_Scalar<TArg, TResult> => Invoke<TOperation, TArg, TArg, TResult>(in x, y, in destination);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Invoke<TOperation, TArg, TResult>(in ReadOnlyTensorSpan<TArg> x, int y, in TensorSpan<TResult> destination)
            where TOperation : IBinaryOperation_Tensor_Int32<TArg, TResult> => Invoke<TOperation, TArg, int, TResult>(in x, y, in destination);

        public static void Invoke<TOperation, TArg1, TArg2, TResult>(in ReadOnlyTensorSpan<TArg1> x, TArg2 y, in TensorSpan<TResult> destination)
            where TOperation : IBinaryOperation_Tensor_Scalar<TArg1, TArg2, TResult>
        {
            scoped Span<nint> xIndexes = RentedBuffer.Create(destination.Rank, x.Strides, out nint xLinearOffset, out RentedBuffer<nint> xRentedBuffer);
            scoped Span<nint> destinationIndexes = RentedBuffer.Create(destination.Rank, destination.Strides, out nint destinationLinearOffset, out RentedBuffer<nint> destinationRentedBuffer);

            for (nint i = 0; i < destination.FlattenedLength; i++)
            {
                xLinearOffset = x._shape.AdjustToNextIndex(destination._shape, xLinearOffset, xIndexes);
                destinationLinearOffset = destination._shape.AdjustToNextIndex(destination._shape, destinationLinearOffset, destinationIndexes);

                TOperation.Invoke(
                    in Unsafe.Add(ref x._reference, xLinearOffset),
                    y,
                    ref Unsafe.Add(ref destination._reference, destinationLinearOffset)
                );
            }

            xRentedBuffer.Dispose();
            destinationRentedBuffer.Dispose();
        }

        public static void Invoke<TOperation, TArg, TResult>(TArg x, in ReadOnlyTensorSpan<TArg> y, in TensorSpan<TResult> destination)
            where TOperation : IBinaryOperation_Scalar_Tensor<TArg, TArg, TResult>
        {
            scoped Span<nint> xIndexes = RentedBuffer.Create(destination.Rank, y.Strides, out nint xLinearOffset, out RentedBuffer<nint> xRentedBuffer);
            scoped Span<nint> destinationIndexes = RentedBuffer.Create(destination.Rank, destination.Strides, out nint destinationLinearOffset, out RentedBuffer<nint> destinationRentedBuffer);

            for (nint i = 0; i < destination.FlattenedLength; i++)
            {
                xLinearOffset = y._shape.AdjustToNextIndex(destination._shape, xLinearOffset, xIndexes);
                destinationLinearOffset = destination._shape.AdjustToNextIndex(destination._shape, destinationLinearOffset, destinationIndexes);

                TOperation.Invoke(
                    x,
                    in Unsafe.Add(ref y._reference, xLinearOffset),
                    ref Unsafe.Add(ref destination._reference, destinationLinearOffset)
                );
            }

            xRentedBuffer.Dispose();
            destinationRentedBuffer.Dispose();
        }

        public static void Invoke<TOperation, TArg1, TArg2, TResult>(in ReadOnlyTensorSpan<TArg1> x, TArg2 y, ref TResult result)
            where TOperation : IBinaryOperation_Tensor_Scalar<TArg1, TArg2, TResult>
        {
            scoped Span<nint> xIndexes = RentedBuffer.Create(x.Rank, x.Strides, out nint xLinearOffset, out RentedBuffer<nint> xRentedBuffer);

            for (nint i = 0; i < x.FlattenedLength; i++)
            {
                xLinearOffset = x._shape.AdjustToNextIndex(x._shape, xLinearOffset, xIndexes);

                TOperation.Invoke(
                    in Unsafe.Add(ref x._reference, xLinearOffset),
                    y,
                    ref result
                );
            }

            xRentedBuffer.Dispose();
        }

        public static void Invoke<TOperation, TArg, TResult>(in ReadOnlyTensorSpan<TArg> x, TArg y, ref TResult result)
            where TOperation : IBinaryOperation_Tensor_Scalar<TArg, TResult> => Invoke<TOperation, TArg, TArg, TResult>(in x, y, ref result);

        public static void ValidateCompatibility<TArg>(in ReadOnlyTensorSpan<TArg> x, in ReadOnlySpan<nint> lengths)
        {
            // x can be broadcast to destination, not vice verse
            if (!TensorShape.AreCompatible(lengths, x._shape))
                ThrowHelper.ThrowArgument_LengthsNotCompatible();
        }

        public static void ValidateCompatibility<TArg, TResult>(in ReadOnlyTensorSpan<TArg> x, in ReadOnlyTensorSpan<TResult> y)
        {
            // Can be bidirectional validation
            if (!TensorShape.AreCompatible(x._shape, y._shape, true))
                ThrowHelper.ThrowArgument_LengthsNotCompatible();
        }

        public static void ValidateCompatibility<TArg, TResult>(in ReadOnlyTensorSpan<TArg> x, in TensorSpan<TResult> destination)
        {
            // x can be broadcast to destination, not vice verse
            if (!TensorShape.AreCompatible(destination._shape, x._shape, false))
                ThrowHelper.ThrowArgument_LengthsNotCompatible();
        }

        public static void ValidateCompatibility<TArg1, TArg2, TResult>(in ReadOnlyTensorSpan<TArg1> x, in ReadOnlyTensorSpan<TArg2> y, in TensorSpan<TResult> destination)
        {
            // can do bidirectional validation between x and y, that result can then be broadcast to destination
            if (TensorShape.AreCompatible(x._shape, y._shape, true))
            {
                if (TensorShape.AreCompatible(destination._shape, x._shape, false))
                {
                    if (TensorShape.AreCompatible(destination._shape, y._shape, false))
                    {
                        // all three are compatible
                        return;
                    }
                }
            }
            ThrowHelper.ThrowArgument_LengthsNotCompatible();
        }

        public static void ValidateCompatibility<TArg, TResult>(in ReadOnlyTensorSpan<TArg> x, in ReadOnlyTensorSpan<TArg> y, out Tensor<TResult> destination)
        {
            // can do bidirectional validation between x and y, that result can then be broadcast to destination
            if (TensorShape.AreCompatible(x._shape, y._shape, true))
            {
                if (x.Rank > y.Rank)
                {
                    destination = Tensor.CreateFromShapeUninitialized<TResult>(x._shape.Lengths);
                }
                else
                {
                    destination = Tensor.CreateFromShapeUninitialized<TResult>(y._shape.Lengths);
                }
                return;
            }
            destination = default!;
            ThrowHelper.ThrowArgument_LengthsNotCompatible();
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
            : IUnaryOperation_Tensor<T, T>
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

        public readonly struct EqualsAny<T>
            : IBinaryOperation_Tensor_Scalar<T, bool>,
              IBinaryOperation_Tensor_Tensor<T, bool>
            where T : IEqualityOperators<T, T, bool>
        {
            // The main loop early exits at the first false condition, so we
            // check x != y and returns false on first equal. The consumer will
            // then negate whatever the main loop returns as `true` means none
            // are equal.

            public static void Invoke(ref readonly T left, T right, ref bool destination)
            {
                destination = (left != right);
            }

            public static void Invoke(ref readonly T left, ref readonly T right, ref bool destination)
            {
                destination = (left != right);
            }

            public static void Invoke(ReadOnlySpan<T> left, T right, Span<bool> destination)
            {
                Debug.Assert(destination.Length == 1);
                bool result = false;

                for (int i = 0; i < destination.Length; i++)
                {
                    result = (left[i] != right);

                    if (!result)
                    {
                        break;
                    }
                }

                destination[0] = result;
            }

            public static void Invoke(ReadOnlySpan<T> left, ReadOnlySpan<T> right, Span<bool> destination)
            {
                Debug.Assert(destination.Length == 1);
                bool result = false;

                for (int i = 0; i < destination.Length; i++)
                {
                    result = (left[i] != right[i]);

                    if (!result)
                    {
                        break;
                    }
                }

                destination[0] = result;
            }
        }

        #region TensorOperation Primitives
        public readonly struct Abs<T>
            : IUnaryOperation_Tensor<T, T>
            where T : INumberBase<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.Abs(x);
            }

            public static void Invoke(ReadOnlySpan<T> x, Span<T> destination)
            {
                TensorPrimitives.Abs(x, destination);
            }
        }

        public readonly struct Acos<T>
            : IUnaryOperation_Tensor<T, T>
            where T : ITrigonometricFunctions<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.Acos(x);
            }

            public static void Invoke(ReadOnlySpan<T> x, Span<T> destination)
            {
                TensorPrimitives.Acos(x, destination);
            }
        }

        public readonly struct Acosh<T>
            : IUnaryOperation_Tensor<T, T>
            where T : IHyperbolicFunctions<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.Acosh(x);
            }

            public static void Invoke(ReadOnlySpan<T> x, Span<T> destination)
            {
                TensorPrimitives.Acosh(x, destination);
            }
        }

        public readonly struct AcosPi<T>
            : IUnaryOperation_Tensor<T, T>
            where T : ITrigonometricFunctions<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.AcosPi(x);
            }

            public static void Invoke(ReadOnlySpan<T> x, Span<T> destination)
            {
                TensorPrimitives.AcosPi(x, destination);
            }
        }

        public readonly struct Add<T>
            : IBinaryOperation_Tensor_Scalar<T, T>,
              IBinaryOperation_Tensor_Tensor<T, T>
            where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            public static void Invoke(ref readonly T x, T y, ref T destination)
            {
                destination = x + y;
            }

            public static void Invoke(ReadOnlySpan<T> x, T y, Span<T> destination)
            {
                TensorPrimitives.Add(x, y, destination);
            }

            public static void Invoke(ref readonly T x, ref readonly T y, ref T destination)
            {
                destination = x + y;
            }

            public static void Invoke(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            {
                TensorPrimitives.Add(x, y, destination);
            }
        }

        public readonly struct Asin<T>
        : IUnaryOperation_Tensor<T, T>
        where T : ITrigonometricFunctions<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.Asin(x);
            }

            public static void Invoke(ReadOnlySpan<T> x, Span<T> destination)
            {
                TensorPrimitives.Asin(x, destination);
            }
        }

        public readonly struct Asinh<T>
        : IUnaryOperation_Tensor<T, T>
        where T : IHyperbolicFunctions<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.Asinh(x);
            }

            public static void Invoke(ReadOnlySpan<T> x, Span<T> destination)
            {
                TensorPrimitives.Asinh(x, destination);
            }
        }

        public readonly struct AsinPi<T>
        : IUnaryOperation_Tensor<T, T>
        where T : ITrigonometricFunctions<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.AsinPi(x);
            }

            public static void Invoke(ReadOnlySpan<T> x, Span<T> destination)
            {
                TensorPrimitives.AsinPi(x, destination);
            }
        }

        public readonly struct Atan<T>
        : IUnaryOperation_Tensor<T, T>
        where T : ITrigonometricFunctions<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.Atan(x);
            }

            public static void Invoke(ReadOnlySpan<T> x, Span<T> destination)
            {
                TensorPrimitives.Atan(x, destination);
            }
        }

        public readonly struct Atan2<T>
            : IBinaryOperation_Tensor_Scalar<T, T>,
              IBinaryOperation_Tensor_Tensor<T, T>
            where T : IFloatingPointIeee754<T>
        {
            public static void Invoke(ref readonly T x, T y, ref T destination)
            {
                destination = T.Atan2(x, y);
            }

            public static void Invoke(ReadOnlySpan<T> x, T y, Span<T> destination)
            {
                for (int i = 0; i < destination.Length; i++)
                {
                    destination[i] = T.Atan2(x[i], y);
                }
            }

            public static void Invoke(ref readonly T x, ref readonly T y, ref T destination)
            {
                destination = T.Atan2(x, y);
            }

            public static void Invoke(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            {
                TensorPrimitives.Atan2(x, y, destination);
            }
        }

        public readonly struct Atan2Pi<T>
            : IBinaryOperation_Tensor_Scalar<T, T>,
              IBinaryOperation_Tensor_Tensor<T, T>
            where T : IFloatingPointIeee754<T>
        {
            public static void Invoke(ref readonly T x, T y, ref T destination)
            {
                destination = T.Atan2Pi(x, y);
            }

            public static void Invoke(ReadOnlySpan<T> x, T y, Span<T> destination)
            {
                for (int i = 0; i < destination.Length; i++)
                {
                    destination[i] = T.Atan2Pi(x[i], y);
                }
            }

            public static void Invoke(ref readonly T x, ref readonly T y, ref T destination)
            {
                destination = T.Atan2Pi(x, y);
            }

            public static void Invoke(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            {
                TensorPrimitives.Atan2Pi(x, y, destination);
            }
        }

        public readonly struct Atanh<T>
        : IUnaryOperation_Tensor<T, T>
        where T : IHyperbolicFunctions<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.Atanh(x);
            }

            public static void Invoke(ReadOnlySpan<T> x, Span<T> destination)
            {
                TensorPrimitives.Atanh(x, destination);
            }
        }

        public readonly struct AtanPi<T>
        : IUnaryOperation_Tensor<T, T>
        where T : ITrigonometricFunctions<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.AtanPi(x);
            }

            public static void Invoke(ReadOnlySpan<T> x, Span<T> destination)
            {
                TensorPrimitives.AtanPi(x, destination);
            }
        }

        public readonly struct BitwiseAnd<T>
            : IBinaryOperation_Tensor_Scalar<T, T>,
              IBinaryOperation_Tensor_Tensor<T, T>
            where T : IBitwiseOperators<T, T, T>
        {
            public static void Invoke(ref readonly T x, T y, ref T destination)
            {
                destination = x & y;
            }

            public static void Invoke(ReadOnlySpan<T> x, T y, Span<T> destination)
            {
                TensorPrimitives.BitwiseAnd(x, y, destination);
            }

            public static void Invoke(ref readonly T x, ref readonly T y, ref T destination)
            {
                destination = x & y;
            }

            public static void Invoke(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            {
                TensorPrimitives.BitwiseAnd(x, y, destination);
            }
        }

        public readonly struct BitwiseOr<T>
            : IBinaryOperation_Tensor_Scalar<T, T>,
              IBinaryOperation_Tensor_Tensor<T, T>
            where T : IBitwiseOperators<T, T, T>
        {
            public static void Invoke(ref readonly T x, T y, ref T destination)
            {
                destination = x | y;
            }

            public static void Invoke(ReadOnlySpan<T> x, T y, Span<T> destination)
            {
                TensorPrimitives.BitwiseOr(x, y, destination);
            }

            public static void Invoke(ref readonly T x, ref readonly T y, ref T destination)
            {
                destination = x | y;
            }

            public static void Invoke(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            {
                TensorPrimitives.BitwiseOr(x, y, destination);
            }
        }

        public readonly struct Cbrt<T>
        : IUnaryOperation_Tensor<T, T>
        where T : IRootFunctions<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.Cbrt(x);
            }

            public static void Invoke(ReadOnlySpan<T> x, Span<T> destination)
            {
                TensorPrimitives.Cbrt(x, destination);
            }
        }

        public readonly struct Ceiling<T>
        : IUnaryOperation_Tensor<T, T>
        where T : IFloatingPoint<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.Ceiling(x);
            }

            public static void Invoke(ReadOnlySpan<T> x, Span<T> destination)
            {
                TensorPrimitives.Ceiling(x, destination);
            }
        }

        public readonly struct ConvertChecked<TFrom, TTo>
        : IUnaryOperation_Tensor<TFrom, TTo>
        where TFrom : IEquatable<TFrom>, IEqualityOperators<TFrom, TFrom, bool>, INumberBase<TFrom>
        where TTo : INumberBase<TTo>
        {
            public static void Invoke(ref readonly TFrom x, ref TTo destination)
            {
                destination = TTo.CreateChecked(x);
            }

            public static void Invoke(ReadOnlySpan<TFrom> x, Span<TTo> destination)
            {
                TensorPrimitives.ConvertChecked(x, destination);
            }
        }

        public readonly struct ConvertSaturating<TFrom, TTo>
        : IUnaryOperation_Tensor<TFrom, TTo>
        where TFrom : IEquatable<TFrom>, IEqualityOperators<TFrom, TFrom, bool>, INumberBase<TFrom>
        where TTo : INumberBase<TTo>
        {
            public static void Invoke(ref readonly TFrom x, ref TTo destination)
            {
                destination = TTo.CreateSaturating(x);
            }

            public static void Invoke(ReadOnlySpan<TFrom> x, Span<TTo> destination)
            {
                TensorPrimitives.ConvertSaturating(x, destination);
            }
        }

        public readonly struct ConvertTruncating<TFrom, TTo>
        : IUnaryOperation_Tensor<TFrom, TTo>
        where TFrom : IEquatable<TFrom>, IEqualityOperators<TFrom, TFrom, bool>, INumberBase<TFrom>
        where TTo : INumberBase<TTo>
        {
            public static void Invoke(ref readonly TFrom x, ref TTo destination)
            {
                destination = TTo.CreateTruncating(x);
            }

            public static void Invoke(ReadOnlySpan<TFrom> x, Span<TTo> destination)
            {
                TensorPrimitives.ConvertTruncating(x, destination);
            }
        }

        public readonly struct CopySign<T>
        : IBinaryOperation_Tensor_Scalar<T, T>,
          IBinaryOperation_Tensor_Tensor<T, T>
        where T : INumber<T>
        {
            public static void Invoke(ref readonly T x, T y, ref T destination)
            {
                destination = T.CopySign(x, y);
            }

            public static void Invoke(ReadOnlySpan<T> x, T y, Span<T> destination)
            {
                TensorPrimitives.CopySign(x, y, destination);
            }

            public static void Invoke(ref readonly T x, ref readonly T y, ref T destination)
            {
                destination = T.CopySign(x, y);
            }

            public static void Invoke(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            {
                TensorPrimitives.CopySign(x, y, destination);
            }
        }

        public readonly struct Cos<T>
        : IUnaryOperation_Tensor<T, T>
        where T : ITrigonometricFunctions<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.Cos(x);
            }

            public static void Invoke(ReadOnlySpan<T> x, Span<T> destination)
            {
                TensorPrimitives.Cos(x, destination);
            }
        }

        public readonly struct Cosh<T>
        : IUnaryOperation_Tensor<T, T>
        where T : IHyperbolicFunctions<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.Cosh(x);
            }

            public static void Invoke(ReadOnlySpan<T> x, Span<T> destination)
            {
                TensorPrimitives.Cosh(x, destination);
            }
        }

        public struct CosineSimilarity<T>
            : IBinaryOperation_Tensor_Tensor<T, ValueTuple<T, T, T>>
            where T : IRootFunctions<T>
        {
            /// This method effectively computes <c>TensorPrimitives.Dot(x, y) / (<typeparamref name="T"/>.Sqrt(TensorPrimitives.SumOfSquares(x)) * <typeparamref name="T"/>.Sqrt(TensorPrimitives.SumOfSquares(y)).</c>

            public static void Invoke(ref readonly T x, ref readonly T y, ref (T, T, T) destination)
            {
                destination.Item1 += (x * y);
                destination.Item2 += (x * x);
                destination.Item3 += (y * y);
            }

            public static void Invoke(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<(T, T, T)> destination)
            {
                destination[0].Item1 += TensorPrimitives.Dot(x, y);
                destination[0].Item2 += TensorPrimitives.SumOfSquares(x);
                destination[0].Item3 += TensorPrimitives.SumOfSquares(y);
            }
        }

        public readonly struct CosPi<T>
        : IUnaryOperation_Tensor<T, T>
        where T : ITrigonometricFunctions<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.CosPi(x);
            }

            public static void Invoke(ReadOnlySpan<T> x, Span<T> destination)
            {
                TensorPrimitives.CosPi(x, destination);
            }
        }

        public readonly struct Decrement<T>
            : IUnaryOperation_Tensor<T, T>
            where T : IDecrementOperators<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                T tmp = x;
                destination = --tmp;
            }

            public static void Invoke(ReadOnlySpan<T> x, Span<T> destination)
            {
                TensorPrimitives.Decrement(x, destination);
            }
        }

        public readonly struct DegreesToRadians<T>
        : IUnaryOperation_Tensor<T, T>
        where T : ITrigonometricFunctions<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.DegreesToRadians(x);
            }

            public static void Invoke(ReadOnlySpan<T> x, Span<T> destination)
            {
                TensorPrimitives.DegreesToRadians(x, destination);
            }
        }

        public readonly struct Divide<T>
            : IBinaryOperation_Scalar_Tensor<T, T, T>,
              IBinaryOperation_Tensor_Scalar<T, T>,
              IBinaryOperation_Tensor_Tensor<T, T>
            where T : IDivisionOperators<T, T, T>
        {
            public static void Invoke(ref readonly T x, T y, ref T destination)
            {
                destination = x / y;
            }

            public static void Invoke(ReadOnlySpan<T> x, T y, Span<T> destination)
            {
                TensorPrimitives.Divide(x, y, destination);
            }

            public static void Invoke(ref readonly T x, ref readonly T y, ref T destination)
            {
                destination = x / y;
            }

            public static void Invoke(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            {
                TensorPrimitives.Divide(x, y, destination);
            }

            public static void Invoke(T x, ref readonly T y, ref T destination)
            {
                destination = x / y;
            }
            public static void Invoke(T x, ReadOnlySpan<T> y, Span<T> destination)
            {
                TensorPrimitives.Divide(x, y, destination);
            }
        }

        public struct Dot<T>
            : IBinaryOperation_Tensor_Tensor<T, T>
            where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>, IMultiplicativeIdentity<T, T>, IMultiplyOperators<T, T, T>
        {
            public static void Invoke(ref readonly T x, ref readonly T y, ref T destination)
            {
                destination += x * y;
            }

            public static void Invoke(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            {
                destination[0] += TensorPrimitives.Dot(x, y);
            }
        }

        public readonly struct Exp<T>
        : IUnaryOperation_Tensor<T, T>
        where T : IExponentialFunctions<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.Exp(x);
            }

            public static void Invoke(ReadOnlySpan<T> x, Span<T> destination)
            {
                TensorPrimitives.Exp(x, destination);
            }
        }

        public readonly struct Exp10<T>
        : IUnaryOperation_Tensor<T, T>
        where T : IExponentialFunctions<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.Exp10(x);
            }

            public static void Invoke(ReadOnlySpan<T> x, Span<T> destination)
            {
                TensorPrimitives.Exp10(x, destination);
            }
        }

        public readonly struct Exp10M1<T>
        : IUnaryOperation_Tensor<T, T>
        where T : IExponentialFunctions<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.Exp10M1(x);
            }

            public static void Invoke(ReadOnlySpan<T> x, Span<T> destination)
            {
                TensorPrimitives.Exp10M1(x, destination);
            }
        }

        public readonly struct Exp2<T>
        : IUnaryOperation_Tensor<T, T>
        where T : IExponentialFunctions<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.Exp2(x);
            }

            public static void Invoke(ReadOnlySpan<T> x, Span<T> destination)
            {
                TensorPrimitives.Exp2(x, destination);
            }
        }

        public readonly struct Exp2M1<T>
        : IUnaryOperation_Tensor<T, T>
        where T : IExponentialFunctions<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.Exp2M1(x);
            }

            public static void Invoke(ReadOnlySpan<T> x, Span<T> destination)
            {
                TensorPrimitives.Exp2M1(x, destination);
            }
        }

        public readonly struct ExpM1<T>
        : IUnaryOperation_Tensor<T, T>
        where T : IExponentialFunctions<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.ExpM1(x);
            }

            public static void Invoke(ReadOnlySpan<T> x, Span<T> destination)
            {
                TensorPrimitives.ExpM1(x, destination);
            }
        }

        public readonly struct Floor<T>
        : IUnaryOperation_Tensor<T, T>
        where T : IFloatingPoint<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.Floor(x);
            }

            public static void Invoke(ReadOnlySpan<T> x, Span<T> destination)
            {
                TensorPrimitives.Floor(x, destination);
            }
        }

        public readonly struct Hypot<T>
            : IBinaryOperation_Tensor_Tensor<T, T>
            where T : IRootFunctions<T>
        {
            public static void Invoke(ref readonly T x, ref readonly T y, ref T destination)
            {
                destination = T.Hypot(x, y);
            }

            public static void Invoke(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            {
                TensorPrimitives.Hypot(x, y, destination);
            }
        }

        public readonly struct Ieee754Remainder<T>
            : IBinaryOperation_Scalar_Tensor<T, T, T>,
              IBinaryOperation_Tensor_Scalar<T, T>,
              IBinaryOperation_Tensor_Tensor<T, T>
            where T : IFloatingPointIeee754<T>
        {
            public static void Invoke(ref readonly T x, T y, ref T destination)
            {
                destination = T.Ieee754Remainder(x, y);
            }

            public static void Invoke(ReadOnlySpan<T> x, T y, Span<T> destination)
            {
                TensorPrimitives.Ieee754Remainder(x, y, destination);
            }

            public static void Invoke(ref readonly T x, ref readonly T y, ref T destination)
            {
                destination = T.Ieee754Remainder(x, y);
            }

            public static void Invoke(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            {
                TensorPrimitives.Ieee754Remainder(x, y, destination);
            }

            public static void Invoke(T x, ref readonly T y, ref T destination)
            {
                destination = T.Ieee754Remainder(x, y);
            }
            public static void Invoke(T x, ReadOnlySpan<T> y, Span<T> destination)
            {
                TensorPrimitives.Ieee754Remainder(x, y, destination);
            }
        }

        public readonly struct ILogB<T>
        : IUnaryOperation_Tensor<T, int>
        where T : IFloatingPointIeee754<T>
        {
            public static void Invoke(ref readonly T x, ref int destination)
            {
                destination = T.ILogB(x);
            }

            public static void Invoke(ReadOnlySpan<T> x, Span<int> destination)
            {
                TensorPrimitives.ILogB(x, destination);
            }
        }

        public readonly struct Increment<T>
            : IUnaryOperation_Tensor<T, T>
            where T : IIncrementOperators<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                T tmp = x;
                destination = ++tmp;
            }

            public static void Invoke(ReadOnlySpan<T> x, Span<T> destination)
            {
                TensorPrimitives.Increment(x, destination);
            }
        }

        public readonly struct LeadingZeroCount<T>
        : IUnaryOperation_Tensor<T, T>
        where T : IBinaryInteger<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.LeadingZeroCount(x);
            }

            public static void Invoke(ReadOnlySpan<T> x, Span<T> destination)
            {
                TensorPrimitives.LeadingZeroCount(x, destination);
            }
        }

        public readonly struct Log<T>
        : IUnaryOperation_Tensor<T, T>,
          IBinaryOperation_Tensor_Tensor<T, T>,
          IBinaryOperation_Tensor_Scalar<T, T>
        where T : ILogarithmicFunctions<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.Log(x);
            }

            public static void Invoke(ReadOnlySpan<T> x, Span<T> destination)
            {
                TensorPrimitives.Log(x, destination);
            }

            public static void Invoke(ref readonly T x, ref readonly T y, ref T destination)
            {
                destination = T.Log(x, y);
            }

            public static void Invoke(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            {
                TensorPrimitives.Log(x, y, destination);
            }

            public static void Invoke(ref readonly T x, T y, ref T destination)
            {
                destination = T.Log(x, y);
            }

            public static void Invoke(ReadOnlySpan<T> x, T y, Span<T> destination)
            {
                TensorPrimitives.Log(x, y, destination);
            }
        }

        public readonly struct Log10<T>
        : IUnaryOperation_Tensor<T, T>
        where T : ILogarithmicFunctions<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.Log10(x);
            }

            public static void Invoke(ReadOnlySpan<T> x, Span<T> destination)
            {
                TensorPrimitives.Log10(x, destination);
            }
        }

        public readonly struct Log10P1<T>
        : IUnaryOperation_Tensor<T, T>
        where T : ILogarithmicFunctions<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.Log10P1(x);
            }

            public static void Invoke(ReadOnlySpan<T> x, Span<T> destination)
            {
                TensorPrimitives.Log10P1(x, destination);
            }
        }

        public readonly struct Log2<T>
        : IUnaryOperation_Tensor<T, T>
        where T : ILogarithmicFunctions<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.Log2(x);
            }

            public static void Invoke(ReadOnlySpan<T> x, Span<T> destination)
            {
                TensorPrimitives.Log2(x, destination);
            }
        }

        public readonly struct Log2P1<T>
        : IUnaryOperation_Tensor<T, T>
        where T : ILogarithmicFunctions<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.Log2P1(x);
            }

            public static void Invoke(ReadOnlySpan<T> x, Span<T> destination)
            {
                TensorPrimitives.Log2P1(x, destination);
            }
        }

        public readonly struct LogP1<T>
        : IUnaryOperation_Tensor<T, T>
        where T : ILogarithmicFunctions<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.LogP1(x);
            }

            public static void Invoke(ReadOnlySpan<T> x, Span<T> destination)
            {
                TensorPrimitives.LogP1(x, destination);
            }
        }

        public struct Max<T>
            : IUnaryReduction_Tensor<T, T>,
              IBinaryOperation_Tensor_Scalar<T, T>,
              IBinaryOperation_Tensor_Tensor<T, T>
            where T : INumber<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.Max(x, destination);
            }

            public static void Invoke(ReadOnlySpan<T> x, ref T destination)
            {
                destination = TensorPrimitives.Max(x);
            }

            public static void Invoke(ref readonly T x, ref readonly T y, ref T destination)
            {
                destination = T.Max(x, y);
            }

            public static void Invoke(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            {
                TensorPrimitives.Max(x, y, destination);
            }

            public static void Invoke(ref readonly T x, T y, ref T destination)
            {
                destination = T.Max(x, y);
            }

            public static void Invoke(ReadOnlySpan<T> x, T y, Span<T> destination)
            {
                TensorPrimitives.Max(x, y, destination);
            }
        }

        public struct MaxMagnitude<T>
            : IUnaryReduction_Tensor<T, T>,
              IBinaryOperation_Tensor_Scalar<T, T>,
              IBinaryOperation_Tensor_Tensor<T, T>
            where T : INumber<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.MaxMagnitude(x, destination);
            }

            public static void Invoke(ReadOnlySpan<T> x, ref T destination)
            {
                destination = TensorPrimitives.MaxMagnitude(x);
            }

            public static void Invoke(ref readonly T x, ref readonly T y, ref T destination)
            {
                destination = T.MaxMagnitude(x, destination);
            }

            public static void Invoke(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            {
                TensorPrimitives.MaxMagnitude(x, y, destination);
            }

            public static void Invoke(ref readonly T x, T y, ref T destination)
            {
                destination = T.MaxMagnitude(x, destination);
            }

            public static void Invoke(ReadOnlySpan<T> x, T y, Span<T> destination)
            {
                TensorPrimitives.MaxMagnitude(x, y, destination);
            }
        }

        public struct MaxMagnitudeNumber<T>
            : IUnaryReduction_Tensor<T, T>,
              IBinaryOperation_Tensor_Scalar<T, T>,
              IBinaryOperation_Tensor_Tensor<T, T>
            where T : INumberBase<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.MaxMagnitudeNumber(x, destination);
            }

            public static void Invoke(ReadOnlySpan<T> x, ref T destination)
            {
                destination = TensorPrimitives.MaxMagnitudeNumber(x);
            }

            public static void Invoke(ref readonly T x, ref readonly T y, ref T destination)
            {
                destination = T.MaxMagnitudeNumber(x, destination);
            }

            public static void Invoke(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            {
                TensorPrimitives.MaxMagnitudeNumber(x, y, destination);
            }

            public static void Invoke(ref readonly T x, T y, ref T destination)
            {
                destination = T.MaxMagnitudeNumber(x, destination);
            }

            public static void Invoke(ReadOnlySpan<T> x, T y, Span<T> destination)
            {
                TensorPrimitives.MaxMagnitudeNumber(x, y, destination);
            }
        }

        public struct MaxNumber<T>
            : IUnaryReduction_Tensor<T, T>,
              IBinaryOperation_Tensor_Scalar<T, T>,
              IBinaryOperation_Tensor_Tensor<T, T>
            where T : INumber<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.MaxNumber(x, destination);
            }

            public static void Invoke(ReadOnlySpan<T> x, ref T destination)
            {
                destination = TensorPrimitives.MaxNumber(x);
            }

            public static void Invoke(ref readonly T x, ref readonly T y, ref T destination)
            {
                destination = T.MaxNumber(x, destination);
            }

            public static void Invoke(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            {
                TensorPrimitives.MaxNumber(x, y, destination);
            }

            public static void Invoke(ref readonly T x, T y, ref T destination)
            {
                destination = T.MaxNumber(x, destination);
            }

            public static void Invoke(ReadOnlySpan<T> x, T y, Span<T> destination)
            {
                TensorPrimitives.MaxNumber(x, y, destination);
            }
        }

        public struct Min<T>
        : IUnaryReduction_Tensor<T, T>,
          IBinaryOperation_Tensor_Scalar<T, T>,
          IBinaryOperation_Tensor_Tensor<T, T>
        where T : INumber<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.Min(x, destination);
            }

            public static void Invoke(ReadOnlySpan<T> x, ref T destination)
            {
                destination = TensorPrimitives.Min(x);
            }

            public static void Invoke(ref readonly T x, ref readonly T y, ref T destination)
            {
                destination = T.Min(x, y);
            }

            public static void Invoke(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            {
                TensorPrimitives.Min(x, y, destination);
            }

            public static void Invoke(ref readonly T x, T y, ref T destination)
            {
                destination = T.Min(x, y);
            }

            public static void Invoke(ReadOnlySpan<T> x, T y, Span<T> destination)
            {
                TensorPrimitives.Min(x, y, destination);
            }
        }

        public struct MinMagnitude<T>
            : IUnaryReduction_Tensor<T, T>,
              IBinaryOperation_Tensor_Scalar<T, T>,
              IBinaryOperation_Tensor_Tensor<T, T>
            where T : INumber<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.MinMagnitude(x, destination);
            }

            public static void Invoke(ReadOnlySpan<T> x, ref T destination)
            {
                destination = TensorPrimitives.MinMagnitude(x);
            }

            public static void Invoke(ref readonly T x, ref readonly T y, ref T destination)
            {
                destination = T.MinMagnitude(x, destination);
            }

            public static void Invoke(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            {
                TensorPrimitives.MinMagnitude(x, y, destination);
            }

            public static void Invoke(ref readonly T x, T y, ref T destination)
            {
                destination = T.MinMagnitude(x, destination);
            }

            public static void Invoke(ReadOnlySpan<T> x, T y, Span<T> destination)
            {
                TensorPrimitives.MinMagnitude(x, y, destination);
            }
        }

        public struct MinMagnitudeNumber<T>
            : IUnaryReduction_Tensor<T, T>,
              IBinaryOperation_Tensor_Scalar<T, T>,
              IBinaryOperation_Tensor_Tensor<T, T>
            where T : INumberBase<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.MinMagnitudeNumber(x, destination);
            }

            public static void Invoke(ReadOnlySpan<T> x, ref T destination)
            {
                destination = TensorPrimitives.MinMagnitudeNumber(x);
            }

            public static void Invoke(ref readonly T x, ref readonly T y, ref T destination)
            {
                destination = T.MinMagnitudeNumber(x, destination);
            }

            public static void Invoke(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            {
                TensorPrimitives.MinMagnitudeNumber(x, y, destination);
            }

            public static void Invoke(ref readonly T x, T y, ref T destination)
            {
                destination = T.MinMagnitudeNumber(x, destination);
            }

            public static void Invoke(ReadOnlySpan<T> x, T y, Span<T> destination)
            {
                TensorPrimitives.MinMagnitudeNumber(x, y, destination);
            }
        }

        public struct MinNumber<T>
            : IUnaryReduction_Tensor<T, T>,
              IBinaryOperation_Tensor_Scalar<T, T>,
              IBinaryOperation_Tensor_Tensor<T, T>
            where T : INumber<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.MinNumber(x, destination);
            }

            public static void Invoke(ReadOnlySpan<T> x, ref T destination)
            {
                destination = TensorPrimitives.MinNumber(x);
            }

            public static void Invoke(ref readonly T x, ref readonly T y, ref T destination)
            {
                destination = T.MinNumber(x, destination);
            }

            public static void Invoke(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            {
                TensorPrimitives.MinNumber(x, y, destination);
            }

            public static void Invoke(ref readonly T x, T y, ref T destination)
            {
                destination = T.MinNumber(x, destination);
            }

            public static void Invoke(ReadOnlySpan<T> x, T y, Span<T> destination)
            {
                TensorPrimitives.MinNumber(x, y, destination);
            }
        }

        public readonly struct Multiply<T>
            : IBinaryOperation_Tensor_Scalar<T, T>,
              IBinaryOperation_Tensor_Tensor<T, T>
            where T : IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T>
        {
            public static void Invoke(ref readonly T x, T y, ref T destination)
            {
                destination = x * y;
            }

            public static void Invoke(ReadOnlySpan<T> x, T y, Span<T> destination)
            {
                TensorPrimitives.Multiply(x, y, destination);
            }

            public static void Invoke(ref readonly T x, ref readonly T y, ref T destination)
            {
                destination = x * y;
            }

            public static void Invoke(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            {
                TensorPrimitives.Multiply(x, y, destination);
            }
        }

        public readonly struct Negate<T>
        : IUnaryOperation_Tensor<T, T>
        where T : IUnaryNegationOperators<T, T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = -x;
            }

            public static void Invoke(ReadOnlySpan<T> x, Span<T> destination)
            {
                TensorPrimitives.Negate(x, destination);
            }
        }

        public readonly struct OnesComplement<T>
        : IUnaryOperation_Tensor<T, T>
        where T : IBitwiseOperators<T, T, T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = ~x;
            }

            public static void Invoke(ReadOnlySpan<T> x, Span<T> destination)
            {
                TensorPrimitives.OnesComplement(x, destination);
            }
        }

        public readonly struct PopCount<T>
        : IUnaryOperation_Tensor<T, T>
        where T : IBinaryInteger<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.PopCount(x);
            }

            public static void Invoke(ReadOnlySpan<T> x, Span<T> destination)
            {
                TensorPrimitives.PopCount(x, destination);
            }
        }

        public readonly struct Pow<T>
            : IBinaryOperation_Scalar_Tensor<T, T, T>,
              IBinaryOperation_Tensor_Scalar<T, T>,
              IBinaryOperation_Tensor_Tensor<T, T>
            where T : IPowerFunctions<T>
        {
            public static void Invoke(ref readonly T x, T y, ref T destination)
            {
                destination = T.Pow(x, y);
            }

            public static void Invoke(ReadOnlySpan<T> x, T y, Span<T> destination)
            {
                TensorPrimitives.Pow(x, y, destination);
            }

            public static void Invoke(ref readonly T x, ref readonly T y, ref T destination)
            {
                destination = T.Pow(x, y);
            }

            public static void Invoke(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            {
                TensorPrimitives.Pow(x, y, destination);
            }

            public static void Invoke(T x, ref readonly T y, ref T destination)
            {
                destination = T.Pow(x, y);
            }

            public static void Invoke(T x, ReadOnlySpan<T> y, Span<T> destination)
            {
                TensorPrimitives.Pow(x, y, destination);
            }
        }

        public struct Product<T>
            : IUnaryReduction_Tensor<T, T>
            where T : IMultiplicativeIdentity<T, T>, IMultiplyOperators<T, T, T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination *= x;
            }

            public static void Invoke(ReadOnlySpan<T> x, ref T destination)
            {
                destination = TensorPrimitives.Product(x);
            }
        }

        public readonly struct RadiansToDegrees<T>
        : IUnaryOperation_Tensor<T, T>
        where T : ITrigonometricFunctions<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.RadiansToDegrees(x);
            }

            public static void Invoke(ReadOnlySpan<T> x, Span<T> destination)
            {
                TensorPrimitives.RadiansToDegrees(x, destination);
            }
        }

        public readonly struct Reciprocal<T>
        : IUnaryOperation_Tensor<T, T>
        where T : IFloatingPoint<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.One / x;
            }

            public static void Invoke(ReadOnlySpan<T> x, Span<T> destination)
            {
                TensorPrimitives.Reciprocal(x, destination);
            }
        }

        public readonly struct RootN<T>
        : IBinaryOperation_Tensor_Int32<T, T>
        where T : IRootFunctions<T>
        {
            public static void Invoke(ref readonly T x, int y, ref T destination)
            {
                destination = T.RootN(x, y);
            }

            public static void Invoke(ReadOnlySpan<T> x, int y, Span<T> destination)
            {
                TensorPrimitives.RootN(x, y, destination);
            }
        }

        public readonly struct RotateLeft<T>
        : IBinaryOperation_Tensor_Int32<T, T>
        where T : IBinaryInteger<T>
        {
            public static void Invoke(ref readonly T x, int y, ref T destination)
            {
                destination = T.RotateLeft(x, y);
            }
            public static void Invoke(ReadOnlySpan<T> x, int y, Span<T> destination)
            {
                TensorPrimitives.RotateLeft(x, y, destination);
            }
        }

        public readonly struct RotateRight<T>
        : IBinaryOperation_Tensor_Int32<T, T>
        where T : IBinaryInteger<T>
        {
            public static void Invoke(ref readonly T x, int y, ref T destination)
            {
                destination = T.RotateRight(x, y);
            }
            public static void Invoke(ReadOnlySpan<T> x, int y, Span<T> destination)
            {
                TensorPrimitives.RotateRight(x, y, destination);
            }
        }

        public readonly struct Round<T>
        : IUnaryOperation_Tensor<T, T>,
          IBinaryOperation_Tensor_Scalar<T, Tuple<int, MidpointRounding>, T>
        where T : IFloatingPoint<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.Round(x);
            }

            public static void Invoke(ReadOnlySpan<T> x, Span<T> destination)
            {
                TensorPrimitives.Round(x, destination);
            }

            public static void Invoke(ref readonly T x, Tuple<int, MidpointRounding> y, ref T destination)
            {
                destination = T.Round(x, y.Item1, y.Item2);
            }

            public static void Invoke(ReadOnlySpan<T> x, Tuple<int, MidpointRounding> y, Span<T> destination)
            {
                TensorPrimitives.Round(x, y.Item1, y.Item2, destination);
            }
        }

        public readonly struct ShiftLeft<T>
            : IBinaryOperation_Tensor_Scalar<T, int, T>
            where T : IShiftOperators<T, int, T>
        {
            public static void Invoke(ref readonly T x, int shiftAmount, ref T destination)
            {
                destination = x << shiftAmount;
            }

            public static void Invoke(ReadOnlySpan<T> x, int shiftAmount, Span<T> destination)
            {
                TensorPrimitives.ShiftLeft(x, shiftAmount, destination);
            }
        }

        public readonly struct ShiftRightArithmetic<T>
            : IBinaryOperation_Tensor_Scalar<T, int, T>
            where T : IShiftOperators<T, int, T>
        {
            public static void Invoke(ref readonly T x, int shiftAmount, ref T destination)
            {
                destination = x >> shiftAmount;
            }

            public static void Invoke(ReadOnlySpan<T> x, int shiftAmount, Span<T> destination)
            {
                TensorPrimitives.ShiftRightArithmetic(x, shiftAmount, destination);
            }
        }

        public readonly struct ShiftRightLogical<T>
            : IBinaryOperation_Tensor_Scalar<T, int, T>
            where T : IShiftOperators<T, int, T>
        {
            public static void Invoke(ref readonly T x, int shiftAmount, ref T destination)
            {
                destination = x >>> shiftAmount;
            }

            public static void Invoke(ReadOnlySpan<T> x, int shiftAmount, Span<T> destination)
            {
                TensorPrimitives.ShiftRightLogical(x, shiftAmount, destination);
            }
        }

        public readonly struct Sigmoid<T>
        : IUnaryOperation_Tensor<T, T>
        where T : IExponentialFunctions<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.One / (T.One + T.Exp(-x));
            }

            public static void Invoke(ReadOnlySpan<T> x, Span<T> destination)
            {
                TensorPrimitives.Sigmoid(x, destination);
            }
        }

        public readonly struct Sin<T>
        : IUnaryOperation_Tensor<T, T>
        where T : ITrigonometricFunctions<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.Sin(x);
            }

            public static void Invoke(ReadOnlySpan<T> x, Span<T> destination)
            {
                TensorPrimitives.Sin(x, destination);
            }
        }

        public readonly struct Sinh<T>
        : IUnaryOperation_Tensor<T, T>
        where T : IHyperbolicFunctions<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.Sinh(x);
            }

            public static void Invoke(ReadOnlySpan<T> x, Span<T> destination)
            {
                TensorPrimitives.Sinh(x, destination);
            }
        }

        public readonly struct SinPi<T>
        : IUnaryOperation_Tensor<T, T>
        where T : ITrigonometricFunctions<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.SinPi(x);
            }

            public static void Invoke(ReadOnlySpan<T> x, Span<T> destination)
            {
                TensorPrimitives.SinPi(x, destination);
            }
        }

        // SoftMax Helper
        public readonly struct SumExp<T>
        : IUnaryReduction_Tensor<T, T>
        where T : IExponentialFunctions<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination += T.Exp(x);
            }

            public static void Invoke(ReadOnlySpan<T> x, ref T destination)
            {
                for (int i = 0; i < x.Length; i++)
                {
                    destination += T.Exp(x[i]);
                }
            }
        }

        public readonly struct SoftMax<T>
        : IBinaryOperation_Tensor_Scalar<T, T>
        where T : IExponentialFunctions<T>
        {
            public static void Invoke(ref readonly T x, T y, ref T destination)
            {
                destination = T.Exp(x) / y;
            }

            public static void Invoke(ReadOnlySpan<T> x, T y, Span<T> destination)
            {
                for (int i = 0; i < x.Length; i++)
                {
                    destination[i] = T.Exp(x[i]) / y;
                }
            }
        }

        public readonly struct Sqrt<T>
        : IUnaryOperation_Tensor<T, T>
        where T : IRootFunctions<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.Sqrt(x);
            }

            public static void Invoke(ReadOnlySpan<T> x, Span<T> destination)
            {
                TensorPrimitives.Sqrt(x, destination);
            }
        }

        public readonly struct Subtract<T>
            : IBinaryOperation_Scalar_Tensor<T, T, T>,
              IBinaryOperation_Tensor_Scalar<T, T>,
              IBinaryOperation_Tensor_Tensor<T, T>
            where T : ISubtractionOperators<T, T, T>
        {
            public static void Invoke(ref readonly T x, T y, ref T destination)
            {
                destination = x - y;
            }

            public static void Invoke(ReadOnlySpan<T> x, T y, Span<T> destination)
            {
                TensorPrimitives.Subtract(x, y, destination);
            }

            public static void Invoke(ref readonly T x, ref readonly T y, ref T destination)
            {
                destination = x - y;
            }

            public static void Invoke(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            {
                TensorPrimitives.Subtract(x, y, destination);
            }

            public static void Invoke(T x, ref readonly T y, ref T destination)
            {
                destination = x - y;
            }

            public static void Invoke(T x, ReadOnlySpan<T> y, Span<T> destination)
            {
                TensorPrimitives.Subtract(x, y, destination);
            }
        }

        public struct Sum<T>
            : IUnaryReduction_Tensor<T, T>
            where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination += x;
            }

            public static void Invoke(ReadOnlySpan<T> x, ref T destination)
            {
                destination += TensorPrimitives.Sum(x);
            }
        }

        public struct SumOfSquaredDifferences<T>
            : IBinaryOperation_Tensor_Scalar<T, T>,
              IBinaryOperation_Tensor_Tensor<T, T>
            where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>, IMultiplyOperators<T, T, T>, ISubtractionOperators<T, T, T>
        {
            public static void Invoke(ref readonly T x, T y, ref T destination)
            {
                destination += (x - y) * (x - y);
            }
            public static void Invoke(ReadOnlySpan<T> x, T y, Span<T> destination)
            {
                for (int i = 0; i < x.Length; i++)
                {
                    destination[i] = (x[i] - y) * (x[i] - y);
                }
            }

            public static void Invoke(ref readonly T x, ref readonly T y, ref T destination)
            {
                destination += (x - y) * (x - y);
            }

            public static void Invoke(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            {
                for (int i = 0; i < x.Length; i++)
                {
                    destination[i] = (x[i] - y[i]) * (x[i] - y[i]);
                }
            }
        }

        public readonly struct SumOfSquaredAbsoluteDifferences<T>
            : IBinaryOperation_Tensor_Scalar<T, T>
            where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>, IMultiplyOperators<T, T, T>, ISubtractionOperators<T, T, T>, INumberBase<T>
        {
            public static void Invoke(ref readonly T x, T y, ref T destination)
            {
                // Absolute value is needed before squaring to support complex numbers
                T diff = T.Abs(x - y);
                destination += diff * diff;
            }
            public static void Invoke(ReadOnlySpan<T> x, T y, Span<T> destination)
            {
                for (int i = 0; i < x.Length; i++)
                {
                    // Absolute value is needed before squaring to support complex numbers
                    T diff = T.Abs(x[i] - y);
                    destination[i] = diff * diff;
                }
            }
        }

        public readonly struct SumOfSquares<T>
        : IUnaryReduction_Tensor<T, T>
        where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>, IMultiplyOperators<T, T, T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination += x * x;
            }
            public static void Invoke(ReadOnlySpan<T> x, ref T destination)
            {
                destination += TensorPrimitives.SumOfSquares(x);
            }
        }

        public readonly struct Tan<T>
        : IUnaryOperation_Tensor<T, T>
        where T : ITrigonometricFunctions<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.Tan(x);
            }

            public static void Invoke(ReadOnlySpan<T> x, Span<T> destination)
            {
                TensorPrimitives.Tan(x, destination);
            }
        }

        public readonly struct Tanh<T>
        : IUnaryOperation_Tensor<T, T>
        where T : IHyperbolicFunctions<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.Tanh(x);
            }

            public static void Invoke(ReadOnlySpan<T> x, Span<T> destination)
            {
                TensorPrimitives.Tanh(x, destination);
            }
        }

        public readonly struct TanPi<T>
        : IUnaryOperation_Tensor<T, T>
        where T : ITrigonometricFunctions<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.TanPi(x);
            }

            public static void Invoke(ReadOnlySpan<T> x, Span<T> destination)
            {
                TensorPrimitives.TanPi(x, destination);
            }
        }

        public readonly struct TrailingZeroCount<T>
        : IUnaryOperation_Tensor<T, T>
        where T : IBinaryInteger<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.TrailingZeroCount(x);
            }

            public static void Invoke(ReadOnlySpan<T> x, Span<T> destination)
            {
                TensorPrimitives.TrailingZeroCount(x, destination);
            }
        }

        public readonly struct Truncate<T>
        : IUnaryOperation_Tensor<T, T>
        where T : IFloatingPoint<T>
        {
            public static void Invoke(ref readonly T x, ref T destination)
            {
                destination = T.Truncate(x);
            }

            public static void Invoke(ReadOnlySpan<T> x, Span<T> destination)
            {
                TensorPrimitives.Truncate(x, destination);
            }
        }

        public readonly struct Xor<T>
            : IBinaryOperation_Tensor_Scalar<T, T>,
              IBinaryOperation_Tensor_Tensor<T, T>
            where T : IBitwiseOperators<T, T, T>
        {
            public static void Invoke(ref readonly T x, T y, ref T destination)
            {
                destination = x ^ y;
            }

            public static void Invoke(ReadOnlySpan<T> x, T y, Span<T> destination)
            {
                TensorPrimitives.Xor(x, y, destination);
            }

            public static void Invoke(ref readonly T x, ref readonly T y, ref T destination)
            {
                destination = x ^ y;
            }

            public static void Invoke(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            {
                TensorPrimitives.Xor(x, y, destination);
            }
        }

        #endregion

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

        public readonly struct FilteredUpdate<T>
            : IBinaryOperation_Tensor_Scalar<bool, T, T>,
              IBinaryOperation_Tensor_Tensor<bool, T, T>
        {
            public static void Invoke(ref readonly bool x, ref readonly T y, ref T destination)
            {
                if (x)
                {
                    destination = y;
                }
            }
            public static void Invoke(ReadOnlySpan<bool> x, ReadOnlySpan<T> y, Span<T> destination)
            {
                for (int i = 0; i < x.Length; i++)
                {
                    if (x[i])
                    {
                        destination[i] = y[i];
                    }
                }
            }

            public static void Invoke(ref readonly bool x, T y, ref T destination)
            {
                if (x)
                {
                    destination = y;
                }
            }

            public static void Invoke(ReadOnlySpan<bool> x, T y, Span<T> destination)
            {
                for (int i = 0; i < x.Length; i++)
                {
                    if (x[i])
                    {
                        destination[i] = y;
                    }
                }
            }
        }

        public readonly struct GreaterThan<T>
            : IBinaryOperation_Tensor_Scalar<T, bool>,
              IBinaryOperation_Tensor_Tensor<T, bool>
            where T : IComparisonOperators<T, T, bool>
        {
            public static void Invoke(ref readonly T left, T right, ref bool destination)
            {
                destination = (left > right);
            }

            public static void Invoke(ref readonly T left, ref readonly T right, ref bool destination)
            {
                destination = (left > right);
            }

            public static void Invoke(ReadOnlySpan<T> left, T right, Span<bool> destination)
            {
                for (int i = 0; i < destination.Length; i++)
                {
                    destination[i] = (left[i] > right);
                }
            }

            public static void Invoke(ReadOnlySpan<T> left, ReadOnlySpan<T> right, Span<bool> destination)
            {
                for (int i = 0; i < destination.Length; i++)
                {
                    destination[i] = (left[i] > right[i]);
                }
            }
        }

        public readonly struct GreaterThanAny<T>
            : IBinaryOperation_Tensor_Scalar<T, bool>,
              IBinaryOperation_Tensor_Tensor<T, bool>
            where T : IComparisonOperators<T, T, bool>
        {
            // The main loop early exits at the first false condition, so we
            // check !(x > y) and returns false on first not greater. The consumer will
            // then negate whatever the main loop returns as `true` means none
            // are greater.

            public static void Invoke(ref readonly T left, T right, ref bool destination)
            {
                destination = !(left > right);
            }

            public static void Invoke(ref readonly T left, ref readonly T right, ref bool destination)
            {
                destination = !(left > right);
            }

            public static void Invoke(ReadOnlySpan<T> left, T right, Span<bool> destination)
            {
                Debug.Assert(destination.Length == 1);
                bool result = false;

                for (int i = 0; i < destination.Length; i++)
                {
                    result = !(left[i] > right);

                    if (!result)
                    {
                        break;
                    }
                }

                destination[0] = result;
            }

            public static void Invoke(ReadOnlySpan<T> left, ReadOnlySpan<T> right, Span<bool> destination)
            {
                Debug.Assert(destination.Length == 1);
                bool result = false;

                for (int i = 0; i < destination.Length; i++)
                {
                    result = !(left[i] > right[i]);

                    if (!result)
                    {
                        break;
                    }
                }

                destination[0] = result;
            }
        }

        public readonly struct GreaterThanOrEqual<T>
            : IBinaryOperation_Tensor_Scalar<T, bool>,
              IBinaryOperation_Tensor_Tensor<T, bool>
            where T : IComparisonOperators<T, T, bool>
        {
            public static void Invoke(ref readonly T left, T right, ref bool destination)
            {
                destination = (left >= right);
            }

            public static void Invoke(ref readonly T left, ref readonly T right, ref bool destination)
            {
                destination = (left >= right);
            }

            public static void Invoke(ReadOnlySpan<T> left, T right, Span<bool> destination)
            {
                for (int i = 0; i < destination.Length; i++)
                {
                    destination[i] = (left[i] >= right);
                }
            }

            public static void Invoke(ReadOnlySpan<T> left, ReadOnlySpan<T> right, Span<bool> destination)
            {
                for (int i = 0; i < destination.Length; i++)
                {
                    destination[i] = (left[i] >= right[i]);
                }
            }
        }

        public readonly struct GreaterThanOrEqualAny<T>
            : IBinaryOperation_Tensor_Scalar<T, bool>,
              IBinaryOperation_Tensor_Tensor<T, bool>
            where T : IComparisonOperators<T, T, bool>
        {
            // The main loop early exits at the first false condition, so we
            // check !(x >= y) and returns false on first not greater or equal.
            // The consumer will then negate whatever the main loop returns as
            // `true` means none are greater or equal.

            public static void Invoke(ref readonly T left, T right, ref bool destination)
            {
                destination = !(left >= right);
            }

            public static void Invoke(ref readonly T left, ref readonly T right, ref bool destination)
            {
                destination = !(left >= right);
            }

            public static void Invoke(ReadOnlySpan<T> left, T right, Span<bool> destination)
            {
                Debug.Assert(destination.Length == 1);
                bool result = false;

                for (int i = 0; i < destination.Length; i++)
                {
                    result = !(left[i] >= right);

                    if (!result)
                    {
                        break;
                    }
                }

                destination[0] = result;
            }

            public static void Invoke(ReadOnlySpan<T> left, ReadOnlySpan<T> right, Span<bool> destination)
            {
                Debug.Assert(destination.Length == 1);
                bool result = false;

                for (int i = 0; i < destination.Length; i++)
                {
                    result = !(left[i] >= right[i]);

                    if (!result)
                    {
                        break;
                    }
                }

                destination[0] = result;
            }
        }

        public readonly struct LessThan<T>
            : IBinaryOperation_Tensor_Scalar<T, bool>,
              IBinaryOperation_Tensor_Tensor<T, bool>
            where T : IComparisonOperators<T, T, bool>
        {
            public static void Invoke(ref readonly T left, T right, ref bool destination)
            {
                destination = (left < right);
            }

            public static void Invoke(ref readonly T left, ref readonly T right, ref bool destination)
            {
                destination = (left < right);
            }

            public static void Invoke(ReadOnlySpan<T> left, T right, Span<bool> destination)
            {
                for (int i = 0; i < destination.Length; i++)
                {
                    destination[i] = (left[i] < right);
                }
            }

            public static void Invoke(ReadOnlySpan<T> left, ReadOnlySpan<T> right, Span<bool> destination)
            {
                for (int i = 0; i < destination.Length; i++)
                {
                    destination[i] = (left[i] < right[i]);
                }
            }
        }

        public readonly struct LessThanAny<T>
            : IBinaryOperation_Tensor_Scalar<T, bool>,
              IBinaryOperation_Tensor_Tensor<T, bool>
            where T : IComparisonOperators<T, T, bool>
        {
            // The main loop early exits at the first false condition, so we
            // check !(x < y) and returns false on first not lesser. The consumer will
            // then negate whatever the main loop returns as `true` means none
            // are lesser.

            public static void Invoke(ref readonly T left, T right, ref bool destination)
            {
                destination = !(left < right);
            }

            public static void Invoke(ref readonly T left, ref readonly T right, ref bool destination)
            {
                destination = !(left < right);
            }

            public static void Invoke(ReadOnlySpan<T> left, T right, Span<bool> destination)
            {
                Debug.Assert(destination.Length == 1);
                bool result = false;

                for (int i = 0; i < destination.Length; i++)
                {
                    result = !(left[i] < right);

                    if (!result)
                    {
                        break;
                    }
                }

                destination[0] = result;
            }

            public static void Invoke(ReadOnlySpan<T> left, ReadOnlySpan<T> right, Span<bool> destination)
            {
                Debug.Assert(destination.Length == 1);
                bool result = false;

                for (int i = 0; i < destination.Length; i++)
                {
                    result = !(left[i] < right[i]);

                    if (!result)
                    {
                        break;
                    }
                }

                destination[0] = result;
            }
        }

        public readonly struct LessThanOrEqual<T>
            : IBinaryOperation_Tensor_Scalar<T, bool>,
              IBinaryOperation_Tensor_Tensor<T, bool>
            where T : IComparisonOperators<T, T, bool>
        {
            public static void Invoke(ref readonly T left, T right, ref bool destination)
            {
                destination = (left <= right);
            }

            public static void Invoke(ref readonly T left, ref readonly T right, ref bool destination)
            {
                destination = (left <= right);
            }

            public static void Invoke(ReadOnlySpan<T> left, T right, Span<bool> destination)
            {
                for (int i = 0; i < destination.Length; i++)
                {
                    destination[i] = (left[i] <= right);
                }
            }

            public static void Invoke(ReadOnlySpan<T> left, ReadOnlySpan<T> right, Span<bool> destination)
            {
                for (int i = 0; i < destination.Length; i++)
                {
                    destination[i] = (left[i] <= right[i]);
                }
            }
        }

        public readonly struct LessThanOrEqualAny<T>
            : IBinaryOperation_Tensor_Scalar<T, bool>,
              IBinaryOperation_Tensor_Tensor<T, bool>
            where T : IComparisonOperators<T, T, bool>
        {
            // The main loop early exits at the first false condition, so we
            // check !(x <= y) and returns false on first not lesser or equal.
            // The consumer will then negate whatever the main loop returns as
            // `true` means none are lesser or equal.

            public static void Invoke(ref readonly T left, T right, ref bool destination)
            {
                destination = !(left <= right);
            }

            public static void Invoke(ref readonly T left, ref readonly T right, ref bool destination)
            {
                destination = !(left <= right);
            }

            public static void Invoke(ReadOnlySpan<T> left, T right, Span<bool> destination)
            {
                Debug.Assert(destination.Length == 1);
                bool result = false;

                for (int i = 0; i < destination.Length; i++)
                {
                    result = !(left[i] <= right);

                    if (!result)
                    {
                        break;
                    }
                }

                destination[0] = result;
            }

            public static void Invoke(ReadOnlySpan<T> left, ReadOnlySpan<T> right, Span<bool> destination)
            {
                Debug.Assert(destination.Length == 1);
                bool result = false;

                for (int i = 0; i < destination.Length; i++)
                {
                    result = !(left[i] <= right[i]);

                    if (!result)
                    {
                        break;
                    }
                }

                destination[0] = result;
            }
        }

        public interface IBinaryOperation_Tensor_Scalar<T, TResult>
            : IBinaryOperation_Tensor_Scalar<T, T, TResult>
        {
        }

        public interface IBinaryOperation_Tensor_Int32<T, TResult>
            : IBinaryOperation_Tensor_Scalar<T, int, TResult>
        {
        }

        public interface IBinaryOperation_Tensor_Scalar<T1, T2, TResult>
        {
            static abstract void Invoke(ref readonly T1 x, T2 y, ref TResult destination);
            static abstract void Invoke(ReadOnlySpan<T1> x, T2 y, Span<TResult> destination);
        }

        public interface IBinaryOperation_Scalar_Tensor<T1, T2, TResult>
        {
            static abstract void Invoke(T1 x, ref readonly T2 y, ref TResult destination);
            static abstract void Invoke(T1 x, ReadOnlySpan<T2> y, Span<TResult> destination);
        }

        public interface IBinaryOperation_Tensor_Tensor<T1, T2, TResult>
        {
            static abstract void Invoke(ref readonly T1 x, ref readonly T2 y, ref TResult destination);
            static abstract void Invoke(ReadOnlySpan<T1> x, ReadOnlySpan<T2> y, Span<TResult> destination);
        }

        public interface IBinaryOperation_Tensor_Tensor<T, TResult>
            : IBinaryOperation_Tensor_Tensor<T, T, TResult>
        {
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

        public interface IUnaryOperation_Tensor<T, TResult>
        {
            static abstract void Invoke(ref readonly T x, ref TResult destination);
            static abstract void Invoke(ReadOnlySpan<T> x, Span<TResult> destination);
        }

        public interface IUnaryReduction_Tensor<T, TResult>
        {
            static abstract void Invoke(ref readonly T x, ref TResult destination);
            static abstract void Invoke(ReadOnlySpan<T> x, ref TResult destination);
        }

        internal readonly struct RentedBuffer
        {
            public static Span<T> Create<T>(int rank, ReadOnlySpan<nint> strides, out nint linearOffset, [UnscopedRef] out RentedBuffer<T> rentedBuffer)
                where T : INumber<T>
            {
                Span<T> output = RentedBuffer<T>.Create(rank, out rentedBuffer);
                linearOffset = 0 - (!strides.IsEmpty ? strides[^1] : 0);

                output[^1] = T.CreateChecked(-1);
                return output;
            }

            public static Span<T> CreateUninitialized<T>(int rank, [UnscopedRef] out RentedBuffer<T> rentedBuffer)
                => RentedBuffer<T>.Create(rank, out rentedBuffer);
        }

        internal ref struct RentedBuffer<T> : IDisposable
        {
            private T[]? _array;
            private TensorShape.InlineBuffer<T> _inline;

            public static Span<T> Create(int rank, [UnscopedRef] out RentedBuffer<T> rentedBuffer)
            {
                if (rank > TensorShape.MaxInlineRank)
                {
                    rentedBuffer._array = ArrayPool<T>.Shared.Rent(rank);
                    Unsafe.SkipInit(out rentedBuffer._inline);

                    Span<T> resultBuffer = rentedBuffer._array.AsSpan(0, rank);
                    resultBuffer.Clear();
                    return resultBuffer;
                }
                else
                {
                    rentedBuffer._array = null;
                    rentedBuffer._inline = default;

                    return ((Span<T>)rentedBuffer._inline)[..rank];
                }
            }

            public void Dispose()
            {
                if (_array is not null)
                {
                    ArrayPool<T>.Shared.Return(_array);
                    _array = null;
                }
            }
        }
    }
}
