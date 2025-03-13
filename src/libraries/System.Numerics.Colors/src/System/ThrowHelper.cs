// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System
{
    internal static class ThrowHelper
    {
        [StackTraceHidden]
        internal static void ThrowIfSpanDoesntHaveFourElementsForColor<T>(ReadOnlySpan<T> span,
            [CallerArgumentExpression(nameof(span))] string? paramName = null)
        {
            if (span.Length != 4)
                ThrowSpanDoesntHaveFourElementsForColor<T>(paramName);
        }

        [StackTraceHidden]
        internal static void ThrowSpanDoesntHaveFourElementsForColor<T>(string? paramName = null)
        {
            throw new ArgumentException(SR.Format(SR.Arg_SpanMustHaveElementsForColor, "4"), paramName);
        }
    }
}
