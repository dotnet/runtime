// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.DotNet.XUnitExtensions;

namespace System.Security.Cryptography.Tests
{
    internal static class ExceptionHelpers
    {
        extension (SkipTestException)
        {
            internal static void ThrowWhen([DoesNotReturnIf(true)] bool condition, [CallerArgumentExpression(nameof(condition))] string? conditionText = null)
            {
                if (condition)
                {
                    throw new SkipTestException($"Skipping because condition is true: {conditionText ?? "<unknown_condition>"}");
                }
            }

            internal static void ThrowUnless([DoesNotReturnIf(false)] bool condition, [CallerArgumentExpression(nameof(condition))] string? conditionText = null)
            {
                if (!condition)
                {
                    throw new SkipTestException($"Skipping because condition is false: {conditionText ?? "<unknown_condition>"}");
                }
            }
        }
    }
}
