// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System
{
    internal static class ThrowHelper
    {
        [DoesNotReturn]
        public static void ThrowArgument_DestinationTooShort() =>
            ThrowArgument_DestinationTooShort("destination");

        [DoesNotReturn]
        public static void ThrowArgument_DestinationTooShort(string destinationName) =>
            throw new ArgumentException(SR.Argument_DestinationTooShort, destinationName);

        [DoesNotReturn]
        public static void ThrowArgument_SpansMustHaveSameLength() =>
            throw new ArgumentException(SR.Argument_SpansMustHaveSameLength);

        [DoesNotReturn]
        public static void ThrowArgument_SpansMustBeNonEmpty() =>
            throw new ArgumentException(SR.Argument_SpansMustBeNonEmpty);

        [DoesNotReturn]
        public static void ThrowArgument_InputAndDestinationSpanMustNotOverlap() =>
            throw new ArgumentException(SR.Argument_InputAndDestinationSpanMustNotOverlap, "destination");

        [DoesNotReturn]
        internal static void ThrowArrayTypeMismatchException()
        {
            throw new ArrayTypeMismatchException();
        }

        [DoesNotReturn]
        internal static void ThrowArgumentOutOfRangeException()
        {
            throw new ArgumentOutOfRangeException();
        }

        [DoesNotReturn]
        internal static void ThrowInvalidTypeWithPointersNotSupported(Type targetType)
        {
            throw new ArgumentException(SR.Format(SR.Argument_InvalidTypeWithPointersNotSupported, targetType));
        }

        [DoesNotReturn]
        internal static void ThrowIndexOutOfRangeException()
        {
            throw new IndexOutOfRangeException();
        }

        [DoesNotReturn]
        public static void ThrowArgumentException_DestinationTooShort()
        {
            throw GetArgumentException(SR.DestinationTooShort);
        }

        [DoesNotReturn]
        public static void ThrowArgument_LengthsMustEqualArrayLength()
        {
            throw new ArgumentOutOfRangeException();
        }

        [DoesNotReturn]
        public static void ThrowArgument_IndicesLengthMustEqualRank() =>
            throw new ArgumentException(SR.ThrowArgument_IndicesLengthMustEqualRank);

        private static ArgumentException GetArgumentException(string message)
        {
            return new ArgumentException(message);
        }

        [DoesNotReturn]
        public static void ThrowValueArgumentOutOfRange_NeedNonNegNumException()
        {
            throw new ArgumentOutOfRangeException("value", SR.ThrowArgument_ValueNonNegative);
        }

        [DoesNotReturn]
        public static void ThrowArgument_FilterTensorMustEqualTensorLength()
        {
            throw new ArgumentException(SR.ThrowArgument_FilterTensorMustEqualTensorLength);
        }

        [DoesNotReturn]
        public static void ThrowArgument_SetSliceNoRange(string? paramNames)
        {
            throw new ArgumentException(SR.ThrowArgument_SetSliceNoRange, paramNames);
        }

        [DoesNotReturn]
        public static void ThrowArgument_SetSliceInvalidShapes(string? paramNames)
        {
            throw new ArgumentException(SR.ThrowArgument_SetSliceInvalidShapes, paramNames);
        }

        [DoesNotReturn]
        public static void ThrowArgument_ShapesNotBroadcastCompatible()
        {
            throw new ArgumentException(SR.ThrowArgument_ShapesNotBroadcastCompatible);
        }

        [DoesNotReturn]
        public static void ThrowArgument_SplitNotSplitEvenly()
        {
            throw new ArgumentException(SR.ThrowArgument_SplitNotSplitEvenly);
        }

        [DoesNotReturn]
        public static void ThrowArgument_DimensionsNotSame(string? paramNames)
        {
            throw new ArgumentException(SR.ThrowArgument_DimensionsNotSame, paramNames);
        }

        [DoesNotReturn]
        public static void ThrowArgument_1DTensorRequired(string? paramNames)
        {
            throw new ArgumentException(SR.ThrowArgument_1DTensorRequired, paramNames);
        }

        [DoesNotReturn]
        public static void ThrowArgument_2DTensorRequired(string? paramNames)
        {
            throw new ArgumentException(SR.Argument_2DTensorRequired, paramNames);
        }

        [DoesNotReturn]
        public static void ThrowArgument_IncorrectNumberOfFilterItems(string? paramNames)
        {
            throw new ArgumentException(SR.ThrowArgument_IncorrectNumberOfFilterItems, paramNames);
        }

        [DoesNotReturn]
        public static void ThrowArgument_StackTooFewTensors()
        {
            throw new ArgumentException(SR.ThrowArgument_StackTooFewTensors);
        }

        [DoesNotReturn]
        public static void ThrowArgument_OnlyOneWildcard()
        {
            throw new ArgumentException(SR.ThrowArgument_OnlyOneWildcard);
        }

        [DoesNotReturn]
        public static void ThrowArgument_InvalidReshapeDimensions()
        {
            throw new ArgumentException(SR.ThrowArgument_InvalidReshapeDimensions);
        }

        [DoesNotReturn]
        public static void ThrowArgument_AxisLargerThanRank()
        {
            throw new ArgumentException(SR.ThrowArgument_AxisLargerThanRank);
        }

        [DoesNotReturn]
        public static void ThrowArgument_InvalidSqueezeAxis()
        {
            throw new ArgumentException(SR.ThrowArgument_InvalidSqueezeAxis);
        }

        [DoesNotReturn]
        public static void ThrowArgument_ConcatenateTooFewTensors()
        {
            throw new ArgumentException(SR.ThrowArgument_ConcatenateTooFewTensors);
        }

        [DoesNotReturn]
        public static void ThrowArgument_InvalidAxis()
        {
            throw new ArgumentException(SR.ThrowArgument_InvalidAxis);
        }

        [DoesNotReturn]
        public static void ThrowArgument_InvalidConcatenateShape()
        {
            throw new ArgumentException(SR.ThrowArgument_InvalidConcatenateShape);
        }

        [DoesNotReturn]
        public static void ThrowArgument_TransposeTooFewDimensions()
        {
            throw new ArgumentException(SR.ThrowArgument_TransposeTooFewDimensions);
        }

        [DoesNotReturn]
        public static void ThrowArgument_PermuteAxisOrder()
        {
            throw new ArgumentException(SR.ThrowArgument_PermuteAxisOrder);
        }

        [DoesNotReturn]
        public static void ThrowArgument_InPlaceInvalidShape()
        {
            throw new ArgumentException(SR.ThrowArgument_InPlaceInvalidShape);
        }

        [DoesNotReturn]
        public static void ThrowArgument_InvalidStridesAndLengths()
        {
            throw new ArgumentException(SR.ThrowArgument_InvalidStridesAndLengths);
        }

        [DoesNotReturn]
        public static void ThrowArgument_StrideLessThan0()
        {
            throw new ArgumentOutOfRangeException(SR.ThrowArgument_StrideLessThan0);
        }

        [DoesNotReturn]
        internal static void ThrowArgument_IncompatibleDimensions(nint leftDim, nint rightDim)
        {
            throw new ArgumentException(SR.Format(SR.Argument_IncompatibleDimensions, leftDim, rightDim));
        }

        [DoesNotReturn]
        internal static void ThrowArgument_StackShapesNotSame()
        {
            throw new ArgumentException(SR.ThrowArgument_StackShapesNotSame);
        }
    }
}
