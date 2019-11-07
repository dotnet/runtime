// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        public static void Ctor_ByteArrayOffset_Empty_ReturnsEmpty()
        {
            byte[] inputData = new byte[] { (byte)'H', (byte)'e', (byte)'l', (byte)'l', (byte)'o' };
            Assert.Same(Utf8String.Empty, new Utf8String(inputData, 3, 0));
        }

        [Fact]
        public static void Ctor_ByteArrayOffset_ValidData_ReturnsOriginalContents()
        {
            byte[] inputData = new byte[] { (byte)'x', (byte)'H', (byte)'e', (byte)'l', (byte)'l', (byte)'o', (byte)'x' };
            Utf8String expected = u8("Hello");

            var actual = new Utf8String(inputData, 1, 5);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void Ctor_ByteArrayOffset_InvalidData_Throws()
        {
            byte[] inputData = new byte[] { (byte)'x', (byte)'H', (byte)'e', (byte)0xFF, (byte)'l', (byte)'o', (byte)'x' };

            Assert.Throws<ArgumentException>(() => new Utf8String(inputData, 0, inputData.Length));
        }

        [Fact]
        public static void Ctor_ByteArrayOffset_NullValue_Throws()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => new Utf8String((byte[])null, 0, 0));
            Assert.Equal("value", exception.ParamName);
        }

        [Fact]
        public static void Ctor_ByteArrayOffset_InvalidStartIndexOrLength_Throws()
        {
            byte[] inputData = new byte[] { (byte)'H', (byte)'e', (byte)'l', (byte)'l', (byte)'o' };

            Assert.Throws<ArgumentOutOfRangeException>(() => new Utf8String(inputData, 1, 5));
        }

        [Fact]
        public static void Ctor_BytePointer_Null_Throws()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => new Utf8String((byte*)null));
            Assert.Equal("value", exception.ParamName);
        }

        [Fact]
        public static void Ctor_BytePointer_Empty_ReturnsEmpty()
        {
            byte[] inputData = new byte[] { 0 }; // standalone null byte

            using (BoundedMemory<byte> boundedMemory = BoundedMemory.AllocateFromExistingData(inputData))
            {
                Assert.Same(Utf8String.Empty, new Utf8String((byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(boundedMemory.Span))));
            }
        }

        [Fact]
        public static void Ctor_BytePointer_ValidData_ReturnsOriginalContents()
        {
            byte[] inputData = new byte[] { (byte)'H', (byte)'e', (byte)'l', (byte)'l', (byte)'o', (byte)'\0' };

            using (BoundedMemory<byte> boundedMemory = BoundedMemory.AllocateFromExistingData(inputData))
            {
                Assert.Equal(u8("Hello"), new Utf8String((byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(boundedMemory.Span))));
            }
        }

        [Fact]
        public static void Ctor_BytePointer_InvalidData_Throws()
        {
            byte[] inputData = new byte[] { (byte)'H', (byte)'e', (byte)0xFF, (byte)'l', (byte)'o', (byte)'\0' };

            using (BoundedMemory<byte> boundedMemory = BoundedMemory.AllocateFromExistingData(inputData))
            {
                Assert.Throws<ArgumentException>(() => new Utf8String((byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(boundedMemory.Span))));
            }
        }

        [Fact]
        public static void Ctor_ByteSpan_Empty_ReturnsEmpty()
        {
            Assert.Same(Utf8String.Empty, new Utf8String(ReadOnlySpan<byte>.Empty));
        }

        [Fact]
        public static void Ctor_ByteSpan_ValidData_ReturnsOriginalContents()
        {
            byte[] inputData = new byte[] { (byte)'H', (byte)'e', (byte)'l', (byte)'l', (byte)'o' };
            Utf8String expected = u8("Hello");

            var actual = new Utf8String(inputData.AsSpan());
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void Ctor_ByteSpan_InvalidData_Throws()
        {
            byte[] inputData = new byte[] { (byte)'H', (byte)'e', (byte)0xFF, (byte)'l', (byte)'o' };

            Assert.Throws<ArgumentException>(() => new Utf8String(inputData.AsSpan()));
        }

        [Fact]
        public static void Ctor_CharArrayOffset_Empty_ReturnsEmpty()
        {
            char[] inputData = "H\U00012345ello".ToCharArray(); // ok to have an empty slice in the middle of a multi-byte subsequence
            Assert.Same(Utf8String.Empty, new Utf8String(inputData, 3, 0));
        }

        [Fact]
        public static void Ctor_CharArrayOffset_ValidData_ReturnsAsUtf8()
        {
            char[] inputData = "H\U00012345\u07ffello".ToCharArray();
            Utf8String expected = u8("\u07ffello");

            var actual = new Utf8String(inputData, 3, 5);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void Ctor_CharArrayOffset_InvalidData_Throws()
        {
            char[] inputData = "H\ud800ello".ToCharArray();

            Assert.Throws<ArgumentException>(() => new Utf8String(inputData, 0, inputData.Length));
        }

        [Fact]
        public static void Ctor_CharArrayOffset_NullValue_Throws()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => new Utf8String((char[])null, 0, 0));
            Assert.Equal("value", exception.ParamName);
        }

        [Fact]
        public static void Ctor_CharArrayOffset_InvalidStartIndexOrLength_Throws()
        {
            char[] inputData = "Hello".ToCharArray();

            Assert.Throws<ArgumentOutOfRangeException>(() => new Utf8String(inputData, 1, 5));
        }

        [Fact]
        public static void Ctor_CharPointer_Null_Throws()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => new Utf8String((char*)null));
            Assert.Equal("value", exception.ParamName);
        }

        [Fact]
        public static void Ctor_CharPointer_Empty_ReturnsEmpty()
        {
            char[] inputData = new char[] { '\0' }; // standalone null char

            using (BoundedMemory<char> boundedMemory = BoundedMemory.AllocateFromExistingData(inputData))
            {
                Assert.Same(Utf8String.Empty, new Utf8String((char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(boundedMemory.Span))));
            }
        }

        [Fact]
        public static void Ctor_CharPointer_ValidData_ReturnsOriginalContents()
        {
            char[] inputData = "Hello\0".ToCharArray(); // need to manually null-terminate

            using (BoundedMemory<char> boundedMemory = BoundedMemory.AllocateFromExistingData(inputData))
            {
                Assert.Equal(u8("Hello"), new Utf8String((char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(boundedMemory.Span))));
            }
        }

        [Fact]
        public static void Ctor_CharPointer_InvalidData_Throws()
        {
            char[] inputData = "He\ud800llo\0".ToCharArray(); // need to manually null-terminate

            using (BoundedMemory<char> boundedMemory = BoundedMemory.AllocateFromExistingData(inputData))
            {
                Assert.Throws<ArgumentException>(() => new Utf8String((char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(boundedMemory.Span))));
            }
        }

        [Fact]
        public static void Ctor_CharSpan_Empty_ReturnsEmpty()
        {
            Assert.Same(Utf8String.Empty, new Utf8String(ReadOnlySpan<char>.Empty));
        }

        [Fact]
        public static void Ctor_CharSpan_ValidData_ReturnsOriginalContents()
        {
            char[] inputData = "Hello".ToCharArray();
            Utf8String expected = u8("Hello");

            var actual = new Utf8String(inputData.AsSpan());
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void Ctor_CharSpan_InvalidData_Throws()
        {
            char[] inputData = "He\ud800llo".ToCharArray();

            Assert.Throws<ArgumentException>(() => new Utf8String(inputData.AsSpan()));
        }

        [Fact]
        public static void Ctor_String_Null_Throws()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => new Utf8String((string)null));
            Assert.Equal("value", exception.ParamName);
        }

        [Fact]
        public static void Ctor_String_Empty_ReturnsEmpty()
        {
            Assert.Same(Utf8String.Empty, new Utf8String(string.Empty));
        }

        [Fact]
        public static void Ctor_String_ValidData_ReturnsOriginalContents()
        {
            Assert.Equal(u8("Hello"), new Utf8String("Hello"));
        }

        [Fact]
        public static void Ctor_String_InvalidData_Throws()
        {
            Assert.Throws<ArgumentException>(() => new Utf8String("He\uD800lo"));
        }

        [Fact]
        public static void Ctor_NonValidating_FromByteSpan()
        {
            byte[] inputData = new byte[] { (byte)'x', (byte)'y', (byte)'z' };
            Utf8String actual = Utf8String.UnsafeCreateWithoutValidation(inputData);

            Assert.Equal(u8("xyz"), actual);
        }

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
        public static void Ctor_CreateFromRelaxed_Utf16()
        {
            Assert.Same(Utf8String.Empty, Utf8String.CreateFromRelaxed(ReadOnlySpan<char>.Empty));
            Assert.Equal(u8("xy\uFFFDz"), Utf8String.CreateFromRelaxed("xy\ud800z"));
        }

        [Fact]
        public static void Ctor_CreateFromRelaxed_Utf8()
        {
            Assert.Same(Utf8String.Empty, Utf8String.CreateFromRelaxed(ReadOnlySpan<byte>.Empty));
            Assert.Equal(u8("xy\uFFFDz"), Utf8String.CreateFromRelaxed(new byte[] { (byte)'x', (byte)'y', 0xF4, 0x80, 0x80, (byte)'z' }));
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

        [Fact]
        public static void Ctor_TryCreateFrom_Utf8()
        {
            Utf8String value;

            // Empty string

            Assert.True(Utf8String.TryCreateFrom(ReadOnlySpan<byte>.Empty, out value));
            Assert.Same(Utf8String.Empty, value);

            // Well-formed ASCII contents

            Assert.True(Utf8String.TryCreateFrom(new byte[] { (byte)'H', (byte)'e', (byte)'l', (byte)'l', (byte)'o' }, out value));
            Assert.Equal(u8("Hello"), value);

            // Well-formed non-ASCII contents

            Assert.True(Utf8String.TryCreateFrom(new byte[] { 0xF0, 0x9F, 0x91, 0xBD }, out value)); // U+1F47D EXTRATERRESTRIAL ALIEN
            Assert.Equal(u8("\U0001F47D"), value);

            // Ill-formed contents

            Assert.False(Utf8String.TryCreateFrom(new byte[] { 0xF0, 0x9F, 0x91, (byte)'x' }, out value));
            Assert.Null(value);
        }

        [Fact]
        public static void Ctor_TryCreateFrom_Utf16()
        {
            Utf8String value;

            // Empty string

            Assert.True(Utf8String.TryCreateFrom(ReadOnlySpan<char>.Empty, out value));
            Assert.Same(Utf8String.Empty, value);

            // Well-formed ASCII contents

            Assert.True(Utf8String.TryCreateFrom("Hello", out value));
            Assert.Equal(u8("Hello"), value);

            // Well-formed non-ASCII contents

            Assert.True(Utf8String.TryCreateFrom("\U0001F47D", out value)); // U+1F47D EXTRATERRESTRIAL ALIEN
            Assert.Equal(u8("\U0001F47D"), value);

            // Ill-formed contents

            Assert.False(Utf8String.TryCreateFrom("\uD800x", out value));
            Assert.Null(value);
        }
    }
}
