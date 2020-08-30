// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

using static System.Tests.Utf8TestUtilities;

namespace System.Tests
{
    public unsafe partial class Utf8StringTests
    {
        [Fact]
        public static void Ctor_NonValidating_FromDelegate()
        {
            object expectedState = new object();
            SpanAction<byte, object> spanAction = (span, actualState) =>
            {
                Assert.Same(expectedState, actualState);
                Assert.NotEqual(0, span.Length); // shouldn't have been called for a zero-length span

                for (int i = 0; i < span.Length; i++)
                {
                    Assert.Equal(0, span[i]); // should've been zero-inited
                    span[i] = (byte)('a' + (i % 26)); // writes "abc...xyzabc...xyz..."
                }
            };

            ArgumentException exception = Assert.Throws<ArgumentOutOfRangeException>(() => Utf8String.UnsafeCreateWithoutValidation(-1, expectedState, spanAction));
            Assert.Equal("length", exception.ParamName);

            exception = Assert.Throws<ArgumentNullException>(() => Utf8String.UnsafeCreateWithoutValidation(10, expectedState, action: null));
            Assert.Equal("action", exception.ParamName);

            Assert.Same(Utf8String.Empty, Utf8String.UnsafeCreateWithoutValidation(0, expectedState, spanAction));

            Assert.Equal(u8("abcde"), Utf8String.UnsafeCreateWithoutValidation(5, expectedState, spanAction));
        }

        [Fact]
        public static void Ctor_Validating_FromDelegate()
        {
            object expectedState = new object();
            SpanAction<byte, object> spanAction = (span, actualState) =>
            {
                Assert.Same(expectedState, actualState);
                Assert.NotEqual(0, span.Length); // shouldn't have been called for a zero-length span

                for (int i = 0; i < span.Length; i++)
                {
                    Assert.Equal(0, span[i]); // should've been zero-inited
                    span[i] = (byte)('a' + (i % 26)); // writes "abc...xyzabc...xyz..."
                }
            };

            ArgumentException exception = Assert.Throws<ArgumentOutOfRangeException>(() => Utf8String.Create(-1, expectedState, spanAction));
            Assert.Equal("length", exception.ParamName);

            exception = Assert.Throws<ArgumentNullException>(() => Utf8String.Create(10, expectedState, action: null));
            Assert.Equal("action", exception.ParamName);

            Assert.Same(Utf8String.Empty, Utf8String.Create(0, expectedState, spanAction));

            Assert.Equal(u8("abcde"), Utf8String.Create(5, expectedState, spanAction));
        }

        [Fact]
        public static void Ctor_Validating_FromDelegate_ThrowsIfDelegateProvidesInvalidData()
        {
            SpanAction<byte, object> spanAction = (span, actualState) =>
            {
                span[0] = 0xFF; // never a valid UTF-8 byte
            };

            Assert.Throws<ArgumentException>(() => Utf8String.Create(10, new object(), spanAction));
        }

        [Fact]
        public static void Ctor_CreateRelaxed_FromDelegate()
        {
            object expectedState = new object();
            SpanAction<byte, object> spanAction = (span, actualState) =>
            {
                Assert.Same(expectedState, actualState);
                Assert.NotEqual(0, span.Length); // shouldn't have been called for a zero-length span

                for (int i = 0; i < span.Length; i++)
                {
                    Assert.Equal(0, span[i]); // should've been zero-inited
                    span[i] = 0xFF; // never a valid UTF-8 byte
                }
            };

            ArgumentException exception = Assert.Throws<ArgumentOutOfRangeException>(() => Utf8String.CreateRelaxed(-1, expectedState, spanAction));
            Assert.Equal("length", exception.ParamName);

            exception = Assert.Throws<ArgumentNullException>(() => Utf8String.CreateRelaxed(10, expectedState, action: null));
            Assert.Equal("action", exception.ParamName);

            Assert.Same(Utf8String.Empty, Utf8String.CreateRelaxed(0, expectedState, spanAction));

            Assert.Equal(u8("\uFFFD\uFFFD"), Utf8String.CreateRelaxed(2, expectedState, spanAction));
        }
    }
}
