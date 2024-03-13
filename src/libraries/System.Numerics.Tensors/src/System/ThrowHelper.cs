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

        private static ArgumentException GetArgumentException(string message)
        {
            return new ArgumentException(message);
        }
    }
}
