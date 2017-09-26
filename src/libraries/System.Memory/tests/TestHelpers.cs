// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Runtime.InteropServices;

namespace System
{
    public static class TestHelpers
    {
        public static void Validate<T>(this Span<T> span, params T[] expected) where T : struct, IEquatable<T>
        {
            Assert.True(span.SequenceEqual(expected));
        }

        public static void ValidateReferenceType<T>(this Span<T> span, params T[] expected)
        {
            Assert.Equal(span.Length, expected.Length);
            for (int i = 0; i < expected.Length; i++)
            {
                T actual = span[i];
                Assert.Same(expected[i], actual);
            }

            T ignore;
            AssertThrows<IndexOutOfRangeException, T>(span, (_span) => ignore = _span[expected.Length]);
        }

        public delegate void AssertThrowsAction<T>(Span<T> span);

        // Cannot use standard Assert.Throws() when testing Span - Span and closures don't get along.
        public static void AssertThrows<E, T>(Span<T> span, AssertThrowsAction<T> action) where E:Exception
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
        public static void DontBox<T>(this Span<T> span)
        {
            // This space intentionally left blank.
        }

        public static void Validate<T>(this ReadOnlySpan<T> span, params T[] expected) where T : struct, IEquatable<T>
        {
            Assert.True(span.SequenceEqual(expected));
        }

        public static void ValidateReferenceType<T>(this ReadOnlySpan<T> span, params T[] expected)
        {
            Assert.Equal(span.Length, expected.Length);
            for (int i = 0; i < expected.Length; i++)
            {
                T actual = span[i];
                Assert.Same(expected[i], actual);
            }

            T ignore;
            AssertThrows<IndexOutOfRangeException, T>(span, (_span) => ignore = _span[expected.Length]);
        }

        public delegate void AssertThrowsActionReadOnly<T>(ReadOnlySpan<T> span);

        // Cannot use standard Assert.Throws() when testing Span - Span and closures don't get along.
        public static void AssertThrows<E, T>(ReadOnlySpan<T> span, AssertThrowsActionReadOnly<T> action) where E:Exception
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
        public static void DontBox<T>(this ReadOnlySpan<T> span)
        {
            // This space intentionally left blank.
        }

        public static void Validate<T>(this Memory<T> memory, params T[] expected) where T : struct, IEquatable<T>
        {
            Assert.True(memory.Span.SequenceEqual(expected));
        }

        public static void ValidateReferenceType<T>(this Memory<T> memory, params T[] expected)
        {
            T[] bufferArray = memory.ToArray();
            Assert.Equal(memory.Length, expected.Length);
            for (int i = 0; i < expected.Length; i++)
            {
                T actual = bufferArray[i];
                Assert.Same(expected[i], actual);
            }
        }

        public static void Validate<T>(this ReadOnlyMemory<T> memory, params T[] expected) where T : struct, IEquatable<T>
        {
            Assert.True(memory.Span.SequenceEqual(expected));
        }

        public static void ValidateReferenceType<T>(this ReadOnlyMemory<T> memory, params T[] expected)
        {
            T[] bufferArray = memory.ToArray();
            Assert.Equal(memory.Length, expected.Length);
            for (int i = 0; i < expected.Length; i++)
            {
                T actual = bufferArray[i];
                Assert.Same(expected[i], actual);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public sealed class TestClass
        {
            private double _d;
            public char C0;
            public char C1;
            public char C2;
            public char C3;
            public char C4;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TestValueTypeWithReference
        {
            public int I;
            public string S;
        }

        public enum TestEnum
        {
            e0,
            e1,
            e2,
            e3,
            e4,
        }
    }
}

