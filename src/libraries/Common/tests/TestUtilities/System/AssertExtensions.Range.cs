// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System
{
    public static partial class AssertExtensions
    {
        /// <summary>
        /// Given a span <paramref name="span"/>, throws if the ranges <paramref name="expectedRange"/>
        /// and <paramref name="actualRange"/> point to different slices within the span.
        /// </summary>
        public static void RangesEqual<T>(Span<T> span, Range expectedRange, Range actualRange)
            => RangesEqual((ReadOnlySpan<T>)span, expectedRange, actualRange);

        /// <summary>
        /// Given a span <paramref name="span"/>, throws if the ranges <paramref name="expectedRange"/>
        /// and <paramref name="actualRange"/> point to different slices within the span.
        /// </summary>
        public static void RangesEqual<T>(ReadOnlySpan<T> span, Range expectedRange, Range actualRange)
        {
            // Normalize (make absolute) the expected and actual ranges
            // Normalization call below will throw if Ranges are out-of-bounds w.r.t. the input span

            (int expectedOffset, int expectedLength) = expectedRange.GetOffsetAndLength(span.Length);
            (int actualOffset, int actualLength) = actualRange.GetOffsetAndLength(span.Length);

            Assert.Equal(expectedOffset..(expectedOffset + expectedLength), actualOffset..(actualOffset + actualLength));
        }
    }
}
