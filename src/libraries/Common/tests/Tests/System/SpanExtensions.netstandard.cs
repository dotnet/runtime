// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Tests
{
    /// <summary>
    /// Helper methods that exist in the platform but not in the portable Span.
    /// </summary>
    public static class SpanExtensions
    {
        public static int LastIndexOf(this ReadOnlySpan<char> span, ReadOnlySpan<char> value, StringComparison comparisonType)
        {
            return span.ToString().LastIndexOf(value.ToString(), comparisonType);
        }
    }
}
