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
        public static void Ctor_ByteArrayOffset_InvalidData_FixesUpData()
        {
            byte[] inputData = new byte[] { (byte)'x', (byte)'H', (byte)'e', (byte)0xFF, (byte)'l', (byte)'o', (byte)'x' };
            Utf8String expected = u8("He\uFFFDlo");

            var actual = new Utf8String(inputData, 1, 5);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void Ctor_BytePointer_NullOrEmpty_ReturnsEmpty()
        {
            byte[] inputData = new byte[] { 0 }; // standalone null byte

            using (BoundedMemory<byte> boundedMemory = BoundedMemory.AllocateFromExistingData(inputData))
            {
                Assert.Same(Utf8String.Empty, new Utf8String((byte*)null));
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
        public static void Ctor_BytePointer_InvalidData_FixesUpData()
        {
            byte[] inputData = new byte[] { (byte)'H', (byte)'e', (byte)0xFF, (byte)'l', (byte)'o', (byte)'\0' };

            using (BoundedMemory<byte> boundedMemory = BoundedMemory.AllocateFromExistingData(inputData))
            {
                Assert.Equal(u8("He\uFFFDlo"), new Utf8String((byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(boundedMemory.Span))));
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
        public static void Ctor_ByteSpan_InvalidData_FixesUpData()
        {
            byte[] inputData = new byte[] { (byte)'H', (byte)'e', (byte)0xFF, (byte)'l', (byte)'o' };
            Utf8String expected = u8("He\uFFFDlo");

            var actual = new Utf8String(inputData.AsSpan());
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void Ctor_CharArrayOffset_Empty_ReturnsEmpty()
        {
            char[] inputData = "Hello".ToCharArray();
            Assert.Same(Utf8String.Empty, new Utf8String(inputData, 3, 0));
        }

        [Fact]
        public static void Ctor_CharArrayOffset_ValidData_ReturnsOriginalContents()
        {
            char[] inputData = "xHellox".ToCharArray();
            Utf8String expected = u8("Hello");

            var actual = new Utf8String(inputData, 1, 5);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void Ctor_CharArrayOffset_InvalidData_FixesUpData()
        {
            char[] inputData = new char[] { 'x', 'H', 'e', '\uD800', 'l', 'o', 'x' };
            Utf8String expected = u8("He\uFFFDlo");

            var actual = new Utf8String(inputData, 1, 5);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void Ctor_CharPointer_NullOrEmpty_ReturnsEmpty()
        {
            char[] inputData = new char[] { '\0' }; // standalone null char

            using (BoundedMemory<char> boundedMemory = BoundedMemory.AllocateFromExistingData(inputData))
            {
                Assert.Same(Utf8String.Empty, new Utf8String((char*)null));
                Assert.Same(Utf8String.Empty, new Utf8String((char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(boundedMemory.Span))));
            }
        }

        [Fact]
        public static void Ctor_CharPointer_ValidData_ReturnsOriginalContents()
        {
            char[] inputData = new char[] { 'H', 'e', 'l', 'l', 'o', '\0' };

            using (BoundedMemory<char> boundedMemory = BoundedMemory.AllocateFromExistingData(inputData))
            {
                Assert.Equal(u8("Hello"), new Utf8String((char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(boundedMemory.Span))));
            }
        }

        [Fact]
        public static void Ctor_CharPointer_InvalidData_FixesUpData()
        {
            char[] inputData = new char[] { 'H', 'e', '\uD800', 'l', 'o', '\0' }; // standalone surrogate

            using (BoundedMemory<char> boundedMemory = BoundedMemory.AllocateFromExistingData(inputData))
            {
                Assert.Equal(u8("He\uFFFDlo"), new Utf8String((char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(boundedMemory.Span))));
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
        public static void Ctor_CharSpan_InvalidData_FixesUpData()
        {
            char[] inputData = new char[] { 'H', 'e', '\uD800', 'l', 'o' };
            Utf8String expected = u8("He\uFFFDlo");

            var actual = new Utf8String(inputData.AsSpan());
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void Ctor_String_NullOrEmpty_ReturnsEmpty()
        {
            Assert.Same(Utf8String.Empty, new Utf8String((string)null));
            Assert.Same(Utf8String.Empty, new Utf8String(string.Empty));
        }

        [Fact]
        public static void Ctor_String_ValidData_ReturnsOriginalContents()
        {
            Assert.Equal(u8("Hello"), new Utf8String("Hello"));
        }

        [Fact]
        public static void Ctor_String_InvalidData_FixesUpData()
        {
            Assert.Equal(u8("He\uFFFDlo"), new Utf8String("He\uD800lo"));
        }
    }
}
