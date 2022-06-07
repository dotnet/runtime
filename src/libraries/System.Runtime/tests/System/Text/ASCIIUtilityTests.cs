// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Xunit;

namespace System.Text.Tests
{
    // Since many of the methods we'll be testing are internal, we'll need to invoke
    // them via reflection.
    public static unsafe class AsciiUtilityTests
    {
        private const int SizeOfVector128 = 128 / 8;

        // The delegate definitions and members below provide us access to CoreLib's internals.
        // We use UIntPtr instead of nuint everywhere here since we don't know what our target arch is.

        private delegate UIntPtr FnGetIndexOfFirstNonAsciiByte(byte* pBuffer, UIntPtr bufferLength);
        private static readonly UnsafeLazyDelegate<FnGetIndexOfFirstNonAsciiByte> _fnGetIndexOfFirstNonAsciiByte = new UnsafeLazyDelegate<FnGetIndexOfFirstNonAsciiByte>("GetIndexOfFirstNonAsciiByte");

        private delegate UIntPtr FnGetIndexOfFirstNonAsciiChar(char* pBuffer, UIntPtr bufferLength);
        private static readonly UnsafeLazyDelegate<FnGetIndexOfFirstNonAsciiChar> _fnGetIndexOfFirstNonAsciiChar = new UnsafeLazyDelegate<FnGetIndexOfFirstNonAsciiChar>("GetIndexOfFirstNonAsciiChar");

        private delegate UIntPtr FnNarrowUtf16ToAscii(char* pUtf16Buffer, byte* pAsciiBuffer, UIntPtr elementCount);
        private static readonly UnsafeLazyDelegate<FnNarrowUtf16ToAscii> _fnNarrowUtf16ToAscii = new UnsafeLazyDelegate<FnNarrowUtf16ToAscii>("NarrowUtf16ToAscii");

        private delegate UIntPtr FnWidenAsciiToUtf16(byte* pAsciiBuffer, char* pUtf16Buffer, UIntPtr elementCount);
        private static readonly UnsafeLazyDelegate<FnWidenAsciiToUtf16> _fnWidenAsciiToUtf16 = new UnsafeLazyDelegate<FnWidenAsciiToUtf16>("WidenAsciiToUtf16");

        [Fact]
        public static void GetIndexOfFirstNonAsciiByte_EmptyInput_NullReference()
        {
            Assert.Equal(UIntPtr.Zero, _fnGetIndexOfFirstNonAsciiByte.Delegate(null, UIntPtr.Zero));
        }

        [Fact]
        public static void GetIndexOfFirstNonAsciiByte_EmptyInput_NonNullReference()
        {
            byte b = default;
            Assert.Equal(UIntPtr.Zero, _fnGetIndexOfFirstNonAsciiByte.Delegate(&b, UIntPtr.Zero));
        }

        [Fact]
        public static void GetIndexOfFirstNonAsciiByte_Vector128InnerLoop()
        {
            // The purpose of this test is to make sure we're identifying the correct
            // vector (of the two that we're reading simultaneously) when performing
            // the final ASCII drain at the end of the method once we've broken out
            // of the inner loop.

            using (BoundedMemory<byte> mem = BoundedMemory.Allocate<byte>(1024))
            {
                Span<byte> bytes = mem.Span;

                for (int i = 0; i < bytes.Length; i++)
                {
                    bytes[i] &= 0x7F; // make sure each byte (of the pre-populated random data) is ASCII
                }

                // Two vectors have offsets 0 .. 31. We'll go backward to avoid having to
                // re-clear the vector every time.

                for (int i = 2 * SizeOfVector128 - 1; i >= 0; i--)
                {
                    bytes[100 + i * 13] = 0x80; // 13 is relatively prime to 32, so it ensures all possible positions are hit
                    Assert.Equal(100 + i * 13, CallGetIndexOfFirstNonAsciiByte(bytes));
                }
            }
        }

        [Fact]
        public static void GetIndexOfFirstNonAsciiByte_Boundaries()
        {
            // The purpose of this test is to make sure we're hitting all of the vectorized
            // and draining logic correctly both in the SSE2 and in the non-SSE2 enlightened
            // code paths. We shouldn't be reading beyond the boundaries we were given.

            // The 5 * Vector test should make sure that we're exercising all possible
            // code paths across both implementations.
            using (BoundedMemory<byte> mem = BoundedMemory.Allocate<byte>(5 * Vector<byte>.Count))
            {
                Span<byte> bytes = mem.Span;

                // First, try it with all-ASCII buffers.

                for (int i = 0; i < bytes.Length; i++)
                {
                    bytes[i] &= 0x7F; // make sure each byte (of the pre-populated random data) is ASCII
                }

                for (int i = bytes.Length; i >= 0; i--)
                {
                    Assert.Equal(i, CallGetIndexOfFirstNonAsciiByte(bytes.Slice(0, i)));
                }

                // Then, try it with non-ASCII bytes.

                for (int i = bytes.Length; i >= 1; i--)
                {
                    bytes[i - 1] = 0x80; // set non-ASCII
                    Assert.Equal(i - 1, CallGetIndexOfFirstNonAsciiByte(bytes.Slice(0, i)));
                }
            }
        }

        [Fact]
        public static void GetIndexOfFirstNonAsciiChar_EmptyInput_NullReference()
        {
            Assert.Equal(UIntPtr.Zero, _fnGetIndexOfFirstNonAsciiChar.Delegate(null, UIntPtr.Zero));
        }

        [Fact]
        public static void GetIndexOfFirstNonAsciiChar_EmptyInput_NonNullReference()
        {
            char c = default;
            Assert.Equal(UIntPtr.Zero, _fnGetIndexOfFirstNonAsciiChar.Delegate(&c, UIntPtr.Zero));
        }

        [Fact]
        public static void GetIndexOfFirstNonAsciiChar_Vector128InnerLoop()
        {
            // The purpose of this test is to make sure we're identifying the correct
            // vector (of the two that we're reading simultaneously) when performing
            // the final ASCII drain at the end of the method once we've broken out
            // of the inner loop.
            //
            // Use U+0123 instead of U+0080 for this test because if our implementation
            // uses pminuw / pmovmskb incorrectly, U+0123 will incorrectly show up as ASCII,
            // causing our test to produce a false negative.

            using (BoundedMemory<char> mem = BoundedMemory.Allocate<char>(1024))
            {
                Span<char> chars = mem.Span;

                for (int i = 0; i < chars.Length; i++)
                {
                    chars[i] &= '\u007F'; // make sure each char (of the pre-populated random data) is ASCII
                }

                // Two vectors have offsets 0 .. 31. We'll go backward to avoid having to
                // re-clear the vector every time.

                for (int i = 2 * SizeOfVector128 - 1; i >= 0; i--)
                {
                    chars[100 + i * 13] = '\u0123'; // 13 is relatively prime to 32, so it ensures all possible positions are hit
                    Assert.Equal(100 + i * 13, CallGetIndexOfFirstNonAsciiChar(chars));
                }
            }
        }

        [Fact]
        public static void GetIndexOfFirstNonAsciiChar_Boundaries()
        {
            // The purpose of this test is to make sure we're hitting all of the vectorized
            // and draining logic correctly both in the SSE2 and in the non-SSE2 enlightened
            // code paths. We shouldn't be reading beyond the boundaries we were given.
            //
            // The 5 * Vector test should make sure that we're exercising all possible
            // code paths across both implementations. The sizeof(char) is because we're
            // specifying element count, but underlying implementation reintepret casts to bytes.
            //
            // Use U+0123 instead of U+0080 for this test because if our implementation
            // uses pminuw / pmovmskb incorrectly, U+0123 will incorrectly show up as ASCII,
            // causing our test to produce a false negative.

            using (BoundedMemory<char> mem = BoundedMemory.Allocate<char>(5 * Vector<byte>.Count / sizeof(char)))
            {
                Span<char> chars = mem.Span;

                for (int i = 0; i < chars.Length; i++)
                {
                    chars[i] &= '\u007F'; // make sure each char (of the pre-populated random data) is ASCII
                }

                for (int i = chars.Length; i >= 0; i--)
                {
                    Assert.Equal(i, CallGetIndexOfFirstNonAsciiChar(chars.Slice(0, i)));
                }

                // Then, try it with non-ASCII bytes.

                for (int i = chars.Length; i >= 1; i--)
                {
                    chars[i - 1] = '\u0123'; // set non-ASCII
                    Assert.Equal(i - 1, CallGetIndexOfFirstNonAsciiChar(chars.Slice(0, i)));
                }
            }
        }

        [Fact]
        public static void WidenAsciiToUtf16_EmptyInput_NullReferences()
        {
            Assert.Equal(UIntPtr.Zero, _fnWidenAsciiToUtf16.Delegate(null, null, UIntPtr.Zero));
        }

        [Fact]
        public static void WidenAsciiToUtf16_EmptyInput_NonNullReference()
        {
            byte b = default;
            char c = default;
            Assert.Equal(UIntPtr.Zero, _fnWidenAsciiToUtf16.Delegate(&b, &c, UIntPtr.Zero));
        }

        [Fact]
        public static void WidenAsciiToUtf16_AllAsciiInput()
        {
            using BoundedMemory<byte> asciiMem = BoundedMemory.Allocate<byte>(128);
            using BoundedMemory<char> utf16Mem = BoundedMemory.Allocate<char>(128);

            // Fill source with 00 .. 7F, then trap future writes.

            Span<byte> asciiSpan = asciiMem.Span;
            for (int i = 0; i < asciiSpan.Length; i++)
            {
                asciiSpan[i] = (byte)i;
            }
            asciiMem.MakeReadonly();

            // We'll write to the UTF-16 span.
            // We test with a variety of span lengths to test alignment and fallthrough code paths.

            Span<char> utf16Span = utf16Mem.Span;

            for (int i = 0; i < asciiSpan.Length; i++)
            {
                utf16Span.Clear(); // remove any data from previous iteration

                // First, validate that the workhorse saw the incoming data as all-ASCII.

                Assert.Equal(128 - i, CallWidenAsciiToUtf16(asciiSpan.Slice(i), utf16Span.Slice(i)));

                // Then, validate that the data was transcoded properly.

                for (int j = i; j < 128; j++)
                {
                    Assert.Equal((ushort)asciiSpan[i], (ushort)utf16Span[i]);
                }
            }
        }

        [Fact]
        public static void WidenAsciiToUtf16_SomeNonAsciiInput()
        {
            using BoundedMemory<byte> asciiMem = BoundedMemory.Allocate<byte>(128);
            using BoundedMemory<char> utf16Mem = BoundedMemory.Allocate<char>(128);

            // Fill source with 00 .. 7F, then trap future writes.

            Span<byte> asciiSpan = asciiMem.Span;
            for (int i = 0; i < asciiSpan.Length; i++)
            {
                asciiSpan[i] = (byte)i;
            }

            // We'll write to the UTF-16 span.

            Span<char> utf16Span = utf16Mem.Span;

            for (int i = asciiSpan.Length - 1; i >= 0; i--)
            {
                RandomNumberGenerator.Fill(MemoryMarshal.Cast<char, byte>(utf16Span)); // fill with garbage

                // First, keep track of the garbage we wrote to the destination.
                // We want to ensure it wasn't overwritten.

                char[] expectedTrailingData = utf16Span.Slice(i).ToArray();

                // Then, set the desired byte as non-ASCII, then check that the workhorse
                // correctly saw the data as non-ASCII.

                asciiSpan[i] |= (byte)0x80;
                Assert.Equal(i, CallWidenAsciiToUtf16(asciiSpan, utf16Span));

                // Next, validate that the ASCII data was transcoded properly.

                for (int j = 0; j < i; j++)
                {
                    Assert.Equal((ushort)asciiSpan[j], (ushort)utf16Span[j]);
                }

                // Finally, validate that the trailing data wasn't overwritten with non-ASCII data.

                Assert.Equal(expectedTrailingData, utf16Span.Slice(i).ToArray());
            }
        }

        [Fact]
        public static unsafe void NarrowUtf16ToAscii_EmptyInput_NullReferences()
        {
            Assert.Equal(UIntPtr.Zero, _fnNarrowUtf16ToAscii.Delegate(null, null, UIntPtr.Zero));
        }

        [Fact]
        public static void NarrowUtf16ToAscii_EmptyInput_NonNullReference()
        {
            char c = default;
            byte b = default;
            Assert.Equal(UIntPtr.Zero, _fnNarrowUtf16ToAscii.Delegate(&c, &b, UIntPtr.Zero));
        }

        [Fact]
        public static void NarrowUtf16ToAscii_AllAsciiInput()
        {
            using BoundedMemory<char> utf16Mem = BoundedMemory.Allocate<char>(128);
            using BoundedMemory<byte> asciiMem = BoundedMemory.Allocate<byte>(128);

            // Fill source with 00 .. 7F.

            Span<char> utf16Span = utf16Mem.Span;
            for (int i = 0; i < utf16Span.Length; i++)
            {
                utf16Span[i] = (char)i;
            }
            utf16Mem.MakeReadonly();

            // We'll write to the ASCII span.
            // We test with a variety of span lengths to test alignment and fallthrough code paths.

            Span<byte> asciiSpan = asciiMem.Span;

            for (int i = 0; i < utf16Span.Length; i++)
            {
                asciiSpan.Clear(); // remove any data from previous iteration

                // First, validate that the workhorse saw the incoming data as all-ASCII.

                Assert.Equal(128 - i, CallNarrowUtf16ToAscii(utf16Span.Slice(i), asciiSpan.Slice(i)));

                // Then, validate that the data was transcoded properly.

                for (int j = i; j < 128; j++)
                {
                    Assert.Equal((ushort)utf16Span[i], (ushort)asciiSpan[i]);
                }
            }
        }

        [Fact]
        public static void NarrowUtf16ToAscii_SomeNonAsciiInput()
        {
            using BoundedMemory<char> utf16Mem = BoundedMemory.Allocate<char>(128);
            using BoundedMemory<byte> asciiMem = BoundedMemory.Allocate<byte>(128);

            // Fill source with 00 .. 7F.

            Span<char> utf16Span = utf16Mem.Span;
            for (int i = 0; i < utf16Span.Length; i++)
            {
                utf16Span[i] = (char)i;
            }

            // We'll write to the ASCII span.

            Span<byte> asciiSpan = asciiMem.Span;

            for (int i = utf16Span.Length - 1; i >= 0; i--)
            {
                RandomNumberGenerator.Fill(asciiSpan); // fill with garbage

                // First, keep track of the garbage we wrote to the destination.
                // We want to ensure it wasn't overwritten.

                byte[] expectedTrailingData = asciiSpan.Slice(i).ToArray();

                // Then, set the desired byte as non-ASCII, then check that the workhorse
                // correctly saw the data as non-ASCII.

                utf16Span[i] = '\u0123'; // use U+0123 instead of U+0080 since it catches inappropriate pmovmskb usage
                Assert.Equal(i, CallNarrowUtf16ToAscii(utf16Span, asciiSpan));

                // Next, validate that the ASCII data was transcoded properly.

                for (int j = 0; j < i; j++)
                {
                    Assert.Equal((ushort)utf16Span[j], (ushort)asciiSpan[j]);
                }

                // Finally, validate that the trailing data wasn't overwritten with non-ASCII data.

                Assert.Equal(expectedTrailingData, asciiSpan.Slice(i).ToArray());
            }
        }

        private static int CallGetIndexOfFirstNonAsciiByte(ReadOnlySpan<byte> buffer)
        {
            fixed (byte* pBuffer = &MemoryMarshal.GetReference(buffer))
            {
                // Conversions between UIntPtr <-> int are not checked by default.
                return checked((int)_fnGetIndexOfFirstNonAsciiByte.Delegate(pBuffer, (UIntPtr)buffer.Length));
            }
        }

        private static int CallGetIndexOfFirstNonAsciiChar(ReadOnlySpan<char> buffer)
        {
            fixed (char* pBuffer = &MemoryMarshal.GetReference(buffer))
            {
                // Conversions between UIntPtr <-> int are not checked by default.
                return checked((int)_fnGetIndexOfFirstNonAsciiChar.Delegate(pBuffer, (UIntPtr)buffer.Length));
            }
        }

        private static int CallNarrowUtf16ToAscii(ReadOnlySpan<char> utf16, Span<byte> ascii)
        {
            Assert.Equal(utf16.Length, ascii.Length);

            fixed (char* pUtf16 = &MemoryMarshal.GetReference(utf16))
            fixed (byte* pAscii = &MemoryMarshal.GetReference(ascii))
            {
                // Conversions between UIntPtr <-> int are not checked by default.
                return checked((int)_fnNarrowUtf16ToAscii.Delegate(pUtf16, pAscii, (UIntPtr)utf16.Length));
            }
        }

        private static int CallWidenAsciiToUtf16(ReadOnlySpan<byte> ascii, Span<char> utf16)
        {
            Assert.Equal(ascii.Length, utf16.Length);

            fixed (byte* pAscii = &MemoryMarshal.GetReference(ascii))
            fixed (char* pUtf16 = &MemoryMarshal.GetReference(utf16))
            {
                // Conversions between UIntPtr <-> int are not checked by default.
                return checked((int)_fnWidenAsciiToUtf16.Delegate(pAscii, pUtf16, (UIntPtr)ascii.Length));
            }
        }

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        private static Type GetAsciiUtilityType()
        {
            return Type.GetType("System.Text.ASCIIUtility, System.Private.CoreLib");
        }

        private sealed class UnsafeLazyDelegate<TDelegate> where TDelegate : Delegate
        {
            private readonly Lazy<TDelegate> _lazyDelegate;

            public UnsafeLazyDelegate(string methodName)
            {
                _lazyDelegate = new Lazy<TDelegate>(() =>
                {
                    Assert.True(typeof(TDelegate).IsSubclassOf(typeof(MulticastDelegate)));

                    // Get the MethodInfo for the target method

                    MethodInfo methodInfo = GetAsciiUtilityType().GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    Assert.NotNull(methodInfo);

                    // Construct the TDelegate pointing to this method

                    return methodInfo.CreateDelegate<TDelegate>();
                });
            }

            public TDelegate Delegate => _lazyDelegate.Value;
        }
    }
}
