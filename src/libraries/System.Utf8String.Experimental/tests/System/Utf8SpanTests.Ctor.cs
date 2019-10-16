// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

using static System.Tests.Utf8TestUtilities;

using ustring = System.Utf8String;

namespace System.Text.Tests
{
    public unsafe partial class Utf8SpanTests
    {
        [Fact]
        public static void Ctor_EmptyUtf8String()
        {
            // Arrange

            ustring str = ustring.Empty;

            // Act

            Utf8Span span = new Utf8Span(str);

            // Assert
            // GetPinnableReference should be 'null' to match behavior of empty ROS<byte>.GetPinnableReference();

            Assert.True(span.IsEmpty);
            Assert.Equal(IntPtr.Zero, (IntPtr)(void*)Unsafe.AsPointer(ref Unsafe.AsRef(in span.GetPinnableReference())));
            Assert.True(Unsafe.AreSame(ref Unsafe.AsRef(in str.GetPinnableReference()), ref MemoryMarshal.GetReference(span.Bytes)));
            Assert.Equal(0, span.Length);
        }

        [Fact]
        public static void Ctor_NonEmptyUtf8String()
        {
            // Arrange

            ustring str = u8("Hello!");

            // Act

            Utf8Span span = new Utf8Span(str);

            // Assert

            Assert.False(span.IsEmpty);
            Assert.True(Unsafe.AreSame(ref Unsafe.AsRef(in str.GetPinnableReference()), ref Unsafe.AsRef(in span.GetPinnableReference())));
            Assert.True(Unsafe.AreSame(ref Unsafe.AsRef(in str.GetPinnableReference()), ref MemoryMarshal.GetReference(span.Bytes)));
            Assert.Equal(6, span.Length);
        }

        [Fact]
        public static void Ctor_NullUtf8String()
        {
            // Arrange

            ustring str = null;

            // Act

            Utf8Span span = new Utf8Span(str);

            // Assert
            // GetPinnableReference should be 'null' to match behavior of empty ROS<byte>.GetPinnableReference();

            Assert.True(span.IsEmpty);
            Assert.Equal(IntPtr.Zero, (IntPtr)(void*)Unsafe.AsPointer(ref Unsafe.AsRef(in span.GetPinnableReference())));
            Assert.Equal(IntPtr.Zero, (IntPtr)(void*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(span.Bytes)));
            Assert.Equal(0, span.Length);
        }

        [Fact]
        public static void Ctor_UnsafeFromByteSpan_NonEmpty()
        {
            // Arrange

            ReadOnlySpan<byte> original = new byte[] { 1, 2, 3, 4, 5 };

            // Act

            Utf8Span span = Utf8Span.UnsafeCreateWithoutValidation(original);

            // Assert

            Assert.False(span.IsEmpty);
            Assert.True(Unsafe.AreSame(ref Unsafe.AsRef(in original.GetPinnableReference()), ref Unsafe.AsRef(in span.GetPinnableReference())));
            Assert.True(Unsafe.AreSame(ref Unsafe.AsRef(in original.GetPinnableReference()), ref MemoryMarshal.GetReference(span.Bytes)));
            Assert.Equal(5, span.Length);
        }

        [Fact]
        public static void Ctor_UnsafeFromByteSpan_NonNullEmptyArray()
        {
            // Arrange

            ReadOnlySpan<byte> original = new byte[0];

            // Act

            Utf8Span span = Utf8Span.UnsafeCreateWithoutValidation(original);

            // Assert
            // GetPinnableReference should be 'null' to match behavior of empty ROS<byte>.GetPinnableReference();

            Assert.True(span.IsEmpty);
            Assert.Equal(IntPtr.Zero, (IntPtr)(void*)Unsafe.AsPointer(ref Unsafe.AsRef(in span.GetPinnableReference())));
            Assert.True(Unsafe.AreSame(ref MemoryMarshal.GetReference(original), ref MemoryMarshal.GetReference(span.Bytes)));
            Assert.Equal(0, span.Length);
        }

        [Fact]
        public static void Ctor_UnsafeFromByteSpan_Default()
        {
            // Arrange

            ReadOnlySpan<byte> original = default;

            // Act

            Utf8Span span = Utf8Span.UnsafeCreateWithoutValidation(original);

            // Assert
            // GetPinnableReference should be 'null' to match behavior of empty ROS<byte>.GetPinnableReference();

            Assert.True(span.IsEmpty);
            Assert.Equal(IntPtr.Zero, (IntPtr)(void*)Unsafe.AsPointer(ref Unsafe.AsRef(in span.GetPinnableReference())));
            Assert.Equal(IntPtr.Zero, (IntPtr)(void*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(span.Bytes)));
            Assert.Equal(0, span.Length);
        }
    }
}
