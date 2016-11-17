// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Runtime.InteropServices;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        private static void Validate<T>(this ReadOnlySpan<T> span, params T[] expected)
        {
            bool isValueType = default(T) != null || Nullable.GetUnderlyingType(typeof(T)) != null;
            Assert.Equal(span.Length, expected.Length);
            for (int i = 0; i < expected.Length; i++)
            {
                T actual = span[i];
                if (isValueType)
                    Assert.Equal(expected[i], actual);
                else
                    Assert.Same(expected[i], actual);
            }

            object ignore;
            AssertThrows<IndexOutOfRangeException, T>(span, (_span) => ignore = _span[expected.Length]);
        }

        private delegate void AssertThrowsAction<T>(ReadOnlySpan<T> span);

        // Cannot use standard Assert.Throws() when testing Span - Span and closures don't get along.
        private static void AssertThrows<E, T>(ReadOnlySpan<T> span, AssertThrowsAction<T> action) where E:Exception
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

        // 
        // The innocent looking construct:
        //
        //    Assert.Throws<E>( () => new Span() );
        //
        // generates a hidden box of the Span as the return value of the lambda. This makes the IL illegal and unloadable on 
        // runtimes that enforce the actual Span rules (never mind that we expect never to reach the box instruction...)
        //
        // The workaround is to code it like this:
        //
        //    Assert.Throws<E>( () => new Span().DontBox() );
        // 
        // which turns the lambda return type back to "void" and eliminates the troublesome box instruction.
        //
        private static void DontBox<T>(this ReadOnlySpan<T> span)
        {
            // This space intentionally left blank.
        }

        [StructLayout(LayoutKind.Sequential)]
        private sealed class TestClass
        {
            private double _d;
            public char C0;
            public char C1;
            public char C2;
            public char C3;
            public char C4;
        }
    }
}

