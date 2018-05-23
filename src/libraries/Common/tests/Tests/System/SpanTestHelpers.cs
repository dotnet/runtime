// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public static class SpanTestHelpers
    {
        public delegate void AssertThrowsActionReadOnly<T>(ReadOnlySpan<T> span);

        // Cannot use standard Assert.Throws() when testing Span - Span and closures don't get along.
        public static void AssertThrows<E, T>(ReadOnlySpan<T> span, AssertThrowsActionReadOnly<T> action) where E : Exception
        {
            try
            {
                action(span);
                Assert.False(true, "Expected exception: " + typeof(E).GetType());
            }
            catch (E)
            {
            }
            catch (Exception wrongException)
            {
                Assert.False(true, "Wrong exception thrown: Expected " + typeof(E).GetType() + ": Actual: " + wrongException.GetType());
            }
        }
    }
}
