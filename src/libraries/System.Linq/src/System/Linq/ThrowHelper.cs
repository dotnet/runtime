// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Linq
{
    internal static class ThrowHelper
    {
        [DoesNotReturn]
        internal static void ThrowArgumentNullException(string argument) => throw new ArgumentNullException(argument);

        [DoesNotReturn]
        internal static void ThrowArgumentOutOfRangeException(string argument) => throw new ArgumentOutOfRangeException(argument);

        [DoesNotReturn]
        internal static void ThrowMoreThanOneElementException() => throw new InvalidOperationException(SR.MoreThanOneElement);

        [DoesNotReturn]
        internal static void ThrowMoreThanOneMatchException() => throw new InvalidOperationException(SR.MoreThanOneMatch);

        [DoesNotReturn]
        internal static void ThrowNoElementsException() => throw new InvalidOperationException(SR.NoElements);

        [DoesNotReturn]
        internal static void ThrowNoMatchException() => throw new InvalidOperationException(SR.NoMatch);

        [DoesNotReturn]
        internal static void ThrowNotSupportedException() => throw new NotSupportedException();
    }
}
