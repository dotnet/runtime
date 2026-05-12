// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Formats.Cbor
{
    internal static class Polyfill
    {
        extension(ArgumentOutOfRangeException)
        {
            public static void ThrowIfNegative(int value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
            {
                if (value < 0)
                {
                    ThrowArgumentOutOfRangeException_Negative(paramName, value);
                }
            }
        }

        [DoesNotReturn]
        private static void ThrowArgumentOutOfRangeException_Negative(string? paramName, int value) =>
            throw new ArgumentOutOfRangeException(
                paramName,
                value,
                SR.Format(SR.ArgumentOutOfRange_Generic_MustBeNonNegative, paramName, value));
    }
}
