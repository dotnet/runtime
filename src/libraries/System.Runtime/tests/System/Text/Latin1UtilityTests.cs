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
    public static unsafe class Latin1UtilityTests
    {
        private const int SizeOfVector128 = 128 / 8;

        // The delegate definitions and members below provide us access to CoreLib's internals.
        // We use UIntPtr instead of nuint everywhere here since we don't know what our target arch is.

        private delegate UIntPtr FnGetIndexOfFirstNonLatin1Char(char* pBuffer, UIntPtr bufferLength);
        private static readonly UnsafeLazyDelegate<FnGetIndexOfFirstNonLatin1Char> _fnGetIndexOfFirstNonLatin1Char = new UnsafeLazyDelegate<FnGetIndexOfFirstNonLatin1Char>("GetIndexOfFirstNonLatin1Char");

        private delegate UIntPtr FnNarrowUtf16ToLatin1(char* pUtf16Buffer, byte* pLatin1Buffer, UIntPtr elementCount);
        private static readonly UnsafeLazyDelegate<FnNarrowUtf16ToLatin1> _fnNarrowUtf16ToLatin1 = new UnsafeLazyDelegate<FnNarrowUtf16ToLatin1>("NarrowUtf16ToLatin1");

        private delegate void FnWidenLatin1ToUtf16(byte* pLatin1Buffer, char* pUtf16Buffer, UIntPtr elementCount);
        private static readonly UnsafeLazyDelegate<FnWidenLatin1ToUtf16> _fnWidenLatin1ToUtf16 = new UnsafeLazyDelegate<FnWidenLatin1ToUtf16>("WidenLatin1ToUtf16");

        [Fact]
        public static void GetIndexOfFirstNonLatin1Char_EmptyInput_NullReference()
        {
            Assert.Equal(UIntPtr.Zero, _fnGetIndexOfFirstNonLatin1Char.Delegate(null, UIntPtr.Zero));
        }

        [Fact]
        public static void GetIndexOfFirstNonLatin1Char_EmptyInput_NonNullReference()
        {
            char c = default;
            Assert.Equal(UIntPtr.Zero, _fnGetIndexOfFirstNonLatin1Char.Delegate(&c, UIntPtr.Zero));
        }

        [Fact]
        public static void GetIndexOfFirstNonLatin1Char_Vector128InnerLoop()
        {
            // The purpose of this test is to make sure we're identifying the correct
            // vector (of the two that we're reading simultaneously) when performing
            // the final Latin-1 drain at the end of the method once we've broken out
            // of the inner loop.
            //
            // Use U+FF80 for this test because if our implementation incorrectly uses paddw or
            // paddsw instead of paddusw, U+FF80 will incorrectly show up as Latin-1,
            // causing our test to produce a false negative.

            using (BoundedMemory<char> mem = BoundedMemory.Allocate<char>(1024))
            {
                Span<char> chars = mem.Span;

                for (int i = 0; i < chars.Length; i++)
                {
                    chars[i] &= '\u00FF'; // make sure each char (of the pre-populated random data) is Latin-1
                }

                // Two vectors have offsets 0 .. 31. We'll go backward to avoid having to
                // re-clear the vector every time.

                for (int i = 2 * SizeOfVector128 - 1; i >= 0; i--)
                {
                    chars[100 + i * 13] = '\uFF80'; // 13 is relatively prime to 32, so it ensures all possible positions are hit
                    Assert.Equal(100 + i * 13, CallGetIndexOfFirstNonLatin1Char(chars));
                }
            }
        }

        [Fact]
        public static void GetIndexOfFirstNonLatin1Char_Boundaries()
        {
            // The purpose of this test is to make sure we're hitting all of the vectorized
            // and draining logic correctly both in the SSE2 and in the non-SSE2 enlightened
            // code paths. We shouldn't be reading beyond the boundaries we were given.
            //
            // The 5 * Vector test should make sure that we're exercising all possible
            // code paths across both implementations. The sizeof(char) is because we're
            // specifying element count, but underlying implementation reintepret casts to bytes.
            //
            // Use U+FF80 for this test because if our implementation incorrectly uses paddw or
            // paddsw instead of paddusw, U+FF80 will incorrectly show up as Latin-1,
            // causing our test to produce a false negative.

            using (BoundedMemory<char> mem = BoundedMemory.Allocate<char>(5 * Vector<byte>.Count / sizeof(char)))
            {
                Span<char> chars = mem.Span;

                for (int i = 0; i < chars.Length; i++)
                {
                    chars[i] &= '\u00FF'; // make sure each char (of the pre-populated random data) is Latin1
                }

                for (int i = chars.Length; i >= 0; i--)
                {
                    Assert.Equal(i, CallGetIndexOfFirstNonLatin1Char(chars.Slice(0, i)));
                }

                // Then, try it with non-Latin-1 bytes.

                for (int i = chars.Length; i >= 1; i--)
                {
                    chars[i - 1] = '\uFF80'; // set non-Latin-1
                    Assert.Equal(i - 1, CallGetIndexOfFirstNonLatin1Char(chars.Slice(0, i)));
                }
            }
        }

        [Fact]
        public static void WidenLatin1ToUtf16_EmptyInput_NullReferences()
        {
            _fnWidenLatin1ToUtf16.Delegate(null, null, UIntPtr.Zero); // just want to make sure it doesn't AV
        }

        [Fact]
        public static void WidenLatin1ToUtf16_EmptyInput_NonNullReference()
        {
            using BoundedMemory<byte> latin1Mem = BoundedMemory.Allocate<byte>(0);
            using BoundedMemory<char> utf16Mem = BoundedMemory.Allocate<char>(0);

            fixed (byte* pLatin1 = &MemoryMarshal.GetReference(latin1Mem.Span))
            fixed (char* pUtf16 = &MemoryMarshal.GetReference(utf16Mem.Span))
            {
                _fnWidenLatin1ToUtf16.Delegate(pLatin1, pUtf16, UIntPtr.Zero); // just want to make sure it doesn't AV
            }
        }

        [Fact]
        public static void WidenLatin1ToUtf16()
        {
            using BoundedMemory<byte> latin1Mem = BoundedMemory.Allocate<byte>(128);
            using BoundedMemory<char> utf16Mem = BoundedMemory.Allocate<char>(128);

            // Fill the source with [deterministic] pseudo-random bytes, then make readonly.

            new Random(0x12345).NextBytes(latin1Mem.Span);
            latin1Mem.MakeReadonly();

            // We'll write to the UTF-16 span.
            // We test with a variety of span lengths to test alignment and fallthrough code paths.

            ReadOnlySpan<byte> latin1Span = latin1Mem.Span;
            Span<char> utf16Span = utf16Mem.Span;

            for (int i = 0; i < latin1Span.Length; i++)
            {
                utf16Span.Clear(); // remove any data from previous iteration

                // First, transcode the data from Latin-1 to UTF-16.

                CallWidenLatin1ToUtf16(latin1Span.Slice(i), utf16Span.Slice(i));

                // Then, validate that the data was transcoded properly.

                for (int j = i; j < 128; j++)
                {
                    Assert.Equal((ushort)latin1Span[i], (ushort)utf16Span[i]);
                }
            }

            // Now run the test with a bunch of sliding 48-byte windows.
            // This tests that we handle correctly the scenario where neither the beginning nor the
            // end of the buffer is properly vector-aligned.

            const int WindowSize = 48;

            for (int i = 0; i < latin1Span.Length - WindowSize; i++)
            {
                utf16Span.Clear(); // remove any data from previous iteration

                // First, transcode the data from Latin-1 to UTF-16.

                CallWidenLatin1ToUtf16(latin1Span.Slice(i, WindowSize), utf16Span.Slice(i, WindowSize));

                // Then, validate that the data was transcoded properly.

                for (int j = 0; j < WindowSize; j++)
                {
                    Assert.Equal((ushort)latin1Span[i + j], (ushort)utf16Span[i + j]);
                }
            }
        }

        [Fact]
        public static unsafe void NarrowUtf16ToLAtin1_EmptyInput_NullReferences()
        {
            Assert.Equal(UIntPtr.Zero, _fnNarrowUtf16ToLatin1.Delegate(null, null, UIntPtr.Zero));
        }

        [Fact]
        public static void NarrowUtf16ToLatin1_EmptyInput_NonNullReference()
        {
            using BoundedMemory<char> utf16Mem = BoundedMemory.Allocate<char>(0);
            using BoundedMemory<byte> latin1Mem = BoundedMemory.Allocate<byte>(0);

            fixed (char* pUtf16 = &MemoryMarshal.GetReference(utf16Mem.Span))
            fixed (byte* pLatin1 = &MemoryMarshal.GetReference(latin1Mem.Span))
            {
                Assert.Equal(UIntPtr.Zero, _fnNarrowUtf16ToLatin1.Delegate(pUtf16, pLatin1, UIntPtr.Zero));
            }
        }

        [Fact]
        public static void NarrowUtf16ToLatin1_AllLatin1Input()
        {
            using BoundedMemory<char> utf16Mem = BoundedMemory.Allocate<char>(128);
            using BoundedMemory<byte> latin1Mem = BoundedMemory.Allocate<byte>(128);

            // Fill the source with [deterministic] pseudo-random chars U+0000..U+00FF, then make readonly.

            Random rnd = new Random(0x54321);
            Span<char> utf16Span = utf16Mem.Span;
            for (int i = 0; i < utf16Span.Length; i++)
            {
                utf16Span[i] = (char)(byte)rnd.Next();
            }
            utf16Mem.MakeReadonly();

            // We'll write to the Latin-1 span.
            // We test with a variety of span lengths to test alignment and fallthrough code paths.

            Span<byte> latin1Span = latin1Mem.Span;

            for (int i = 0; i < utf16Span.Length; i++)
            {
                latin1Span.Clear(); // remove any data from previous iteration

                // First, validate that the workhorse saw the incoming data as all-Latin-1.

                Assert.Equal(128 - i, CallNarrowUtf16ToLatin1(utf16Span.Slice(i), latin1Span.Slice(i)));

                // Then, validate that the data was transcoded properly.

                for (int j = i; j < 128; j++)
                {
                    Assert.Equal((ushort)utf16Span[i], (ushort)latin1Span[i]);
                }
            }
        }

        [Fact]
        public static void NarrowUtf16ToLatin1_SomeNonLatin1Input()
        {
            using BoundedMemory<char> utf16Mem = BoundedMemory.Allocate<char>(128);
            using BoundedMemory<byte> latin1Mem = BoundedMemory.Allocate<byte>(128);

            // Fill the source with [deterministic] pseudo-random chars U+0000..U+00FF.

            Random rnd = new Random(0x54321);
            Span<char> utf16Span = utf16Mem.Span;
            for (int i = 0; i < utf16Span.Length; i++)
            {
                utf16Span[i] = (char)(byte)rnd.Next();
            }

            // We'll write to the Latin-1 span.

            Span<byte> latin1Span = latin1Mem.Span;

            for (int i = utf16Span.Length - 1; i >= 0; i--)
            {
                RandomNumberGenerator.Fill(latin1Span); // fill with garbage

                // First, keep track of the garbage we wrote to the destination.
                // We want to ensure it wasn't overwritten.

                byte[] expectedTrailingData = latin1Span.Slice(i).ToArray();

                // Then, set the desired byte as non-Latin-1, then check that the workhorse
                // correctly saw the data as non-Latin-1.

                utf16Span[i] = '\u0123';
                Assert.Equal(i, CallNarrowUtf16ToLatin1(utf16Span, latin1Span));

                // Next, validate that the Latin-1 data was transcoded properly.

                for (int j = 0; j < i; j++)
                {
                    Assert.Equal((ushort)utf16Span[j], (ushort)latin1Span[j]);
                }

                // Finally, validate that the trailing data wasn't overwritten with non-Latin-1 data.

                Assert.Equal(expectedTrailingData, latin1Span.Slice(i).ToArray());
            }
        }

        private static int CallGetIndexOfFirstNonLatin1Char(ReadOnlySpan<char> buffer)
        {
            fixed (char* pBuffer = &MemoryMarshal.GetReference(buffer))
            {
                // Conversions between UIntPtr <-> int are not checked by default.
                return checked((int)_fnGetIndexOfFirstNonLatin1Char.Delegate(pBuffer, (UIntPtr)buffer.Length));
            }
        }

        private static int CallNarrowUtf16ToLatin1(ReadOnlySpan<char> utf16, Span<byte> latin1)
        {
            Assert.Equal(utf16.Length, latin1.Length);

            fixed (char* pUtf16 = &MemoryMarshal.GetReference(utf16))
            fixed (byte* pLatin1 = &MemoryMarshal.GetReference(latin1))
            {
                // Conversions between UIntPtr <-> int are not checked by default.
                return checked((int)_fnNarrowUtf16ToLatin1.Delegate(pUtf16, pLatin1, (UIntPtr)utf16.Length));
            }
        }

        private static void CallWidenLatin1ToUtf16(ReadOnlySpan<byte> latin1, Span<char> utf16)
        {
            Assert.Equal(latin1.Length, utf16.Length);

            fixed (byte* pLatin1 = &MemoryMarshal.GetReference(latin1))
            fixed (char* pUtf16 = &MemoryMarshal.GetReference(utf16))
            {
                // Conversions between UIntPtr <-> int are not checked by default.
                // Unlike other APIs on Latin1Utility, the "widen to UTF-16" API returns void.
                _fnWidenLatin1ToUtf16.Delegate(pLatin1, pUtf16, checked((UIntPtr)latin1.Length));
            }
        }

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        private static Type GetLatin1UtilityType()
        {
            return Type.GetType("System.Text.Latin1Utility, System.Private.CoreLib");
        }

        private sealed class UnsafeLazyDelegate<TDelegate> where TDelegate : class
        {
            private readonly Lazy<TDelegate> _lazyDelegate;

            public UnsafeLazyDelegate(string methodName)
            {
                _lazyDelegate = new Lazy<TDelegate>(() =>
                {
                    Assert.True(typeof(TDelegate).IsSubclassOf(typeof(MulticastDelegate)));

                    // Get the MethodInfo for the target method

                    MethodInfo methodInfo = GetLatin1UtilityType().GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    Assert.NotNull(methodInfo);

                    // Construct the TDelegate pointing to this method

                    return (TDelegate)Activator.CreateInstance(typeof(TDelegate), new object[] { null, methodInfo.MethodHandle.GetFunctionPointer() });
                });
            }

            public TDelegate Delegate => _lazyDelegate.Value;
        }
    }
}
