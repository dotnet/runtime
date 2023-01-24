// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Runtime.InteropServices;
using System.Buffers;
using Microsoft.DotNet.XUnitExtensions;

namespace System.SpanTests
{
    public static partial class MemoryMarshalTests
    {
        [Fact]
        public static unsafe void CreateReadOnlySpanFromNullTerminated_Char_Null()
        {
            Assert.True(MemoryMarshal.CreateReadOnlySpanFromNullTerminated((char*)null).IsEmpty);
            Assert.True(MemoryMarshal.CreateReadOnlySpanFromNullTerminated((byte*)null).IsEmpty);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(256)]
        public static unsafe void CreateReadOnlySpanFromNullTerminated_Char(int expectedLength)
        {
            using BoundedMemory<char> data = BoundedMemory.Allocate<char>(expectedLength + 1);
            data.Span.Fill('s');
            data.Span[^1] = '\0';
            data.MakeReadonly();

            fixed (char* expectedPtr = data.Span)
            {
                ReadOnlySpan<char> actual = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(expectedPtr);
                Assert.Equal(expectedLength, actual.Length);
                fixed (char* actualPtr = &MemoryMarshal.GetReference(actual))
                {
                    Assert.Equal((IntPtr)expectedPtr, (IntPtr)actualPtr);
                }
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(256)]
        public static unsafe void CreateReadOnlySpanFromNullTerminated_Byte(int expectedLength)
        {
            using BoundedMemory<byte> data = BoundedMemory.Allocate<byte>(expectedLength + 1);
            data.Span.Fill(0xFF);
            data.Span[^1] = 0;
            data.MakeReadonly();

            fixed (byte* expectedPtr = data.Span)
            {
                ReadOnlySpan<byte> actual = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(expectedPtr);
                Assert.Equal(expectedLength, actual.Length);
                fixed (byte* actualPtr = &MemoryMarshal.GetReference(actual))
                {
                    Assert.Equal((IntPtr)expectedPtr, (IntPtr)actualPtr);
                }
            }
        }

        [OuterLoop]
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))]
        public static unsafe void CreateReadOnlySpanFromNullTerminated_Char_ExceedsMaximum()
        {
            char* mem;
            try
            {
                mem = (char*)Marshal.AllocHGlobal(unchecked((nint)(sizeof(char) * (2L + int.MaxValue))));
            }
            catch (OutOfMemoryException)
            {
                throw new SkipTestException("Unable to allocate 4GB of memory");
            }

            try
            {
                new Span<char>(mem, int.MaxValue).Fill('s');
                *(mem + int.MaxValue) = 's';
                *(mem + int.MaxValue + 1) = '\0';

                Assert.Throws<ArgumentException>(() => MemoryMarshal.CreateReadOnlySpanFromNullTerminated(mem));
            }
            finally
            {
                Marshal.FreeHGlobal((IntPtr)mem);
            }
        }

        [OuterLoop]
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))]
        public static unsafe void CreateReadOnlySpanFromNullTerminated_Byte_ExceedsMaximum()
        {
            byte* mem;
            try
            {
                mem = (byte*)Marshal.AllocHGlobal(unchecked((nint)(2L + int.MaxValue)));
            }
            catch (OutOfMemoryException)
            {
                throw new SkipTestException("Unable to allocate 2GB of memory");
            }

            try
            {
                new Span<byte>(mem, int.MaxValue).Fill(0xFF);
                *(mem + int.MaxValue) = 0xFF;
                *(mem + int.MaxValue + 1) = 0;

                Assert.Throws<ArgumentException>(() => MemoryMarshal.CreateReadOnlySpanFromNullTerminated(mem));
            }
            finally
            {
                Marshal.FreeHGlobal((IntPtr)mem);
            }
        }
    }
}
