// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace System.IO.Tests
{
    public class BinaryWriter_EncodingTests
    {
        [Fact]
        public void Ctor_Default_UsesFastUtf8()
        {
            BinaryWriter writer = new BinaryWriter(new MemoryStream());
            Assert.True(IsUsingFastUtf8(writer));
        }

        [Fact]
        public void Ctor_EncodingUtf8Singleton_UsesFastUtf8()
        {
            BinaryWriter writer = new BinaryWriter(new MemoryStream(), Encoding.UTF8);
            Assert.True(IsUsingFastUtf8(writer));
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void Ctor_NewUtf8Encoding_UsesFastUtf8(bool emitIdentifier, bool throwOnInvalidBytes)
        {
            BinaryWriter writer = new BinaryWriter(new MemoryStream(), new UTF8Encoding(emitIdentifier, throwOnInvalidBytes));
            Assert.True(IsUsingFastUtf8(writer));
        }

        [Fact]
        public void Ctor_Utf8EncodingWithSingleCharReplacementChar_UsesFastUtf8()
        {
            Encoding encoding = Encoding.GetEncoding("utf-8", new EncoderReplacementFallback("x"), DecoderFallback.ExceptionFallback);
            BinaryWriter writer = new BinaryWriter(new MemoryStream(), encoding);
            Assert.True(IsUsingFastUtf8(writer));
        }

        [Fact]
        public void Ctor_Utf8EncodingWithMultiCharReplacementChar_DoesNotUseFastUtf8()
        {
            Encoding encoding = Encoding.GetEncoding("utf-8", new EncoderReplacementFallback("xx"), DecoderFallback.ExceptionFallback);
            BinaryWriter writer = new BinaryWriter(new MemoryStream(), encoding);
            Assert.False(IsUsingFastUtf8(writer));
        }

        [Fact]
        public void Ctor_NotUtf8EncodingType_DoesNotUseFastUtf8()
        {
            BinaryWriter writer = new BinaryWriter(new MemoryStream(), new UnicodeEncoding());
            Assert.False(IsUsingFastUtf8(writer));
        }

        [Fact]
        public void Ctor_Utf8EncodingDerivedTypeWithWrongCodePage_DoesNotUseFastUtf8()
        {
            BinaryWriter writer = new BinaryWriter(new MemoryStream(), new NotActuallyUTF8Encoding());
            Assert.False(IsUsingFastUtf8(writer));
        }

        [Fact]
        public void Ctor_Utf8EncodingDerivedTypeWithCorrectCodePage_DoesNotUseFastUtf8()
        {
            BinaryWriter writer = new BinaryWriter(new MemoryStream(), new MyCustomUTF8Encoding());
            Assert.True(IsUsingFastUtf8(writer));
        }

        [Theory]
        [InlineData('x')] // 1 UTF-8 byte
        [InlineData('\u00e9')] // LATIN SMALL LETTER E WITH ACUTE (2 UTF-8 bytes)
        [InlineData('\u2130')] // SCRIPT CAPITAL E (3 UTF-8 bytes)
        public void WriteSingleChar_FastUtf8(char ch)
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            writer.Write(ch);

            Assert.Equal(Encoding.UTF8.GetBytes(new char[] { ch }), stream.ToArray());
        }

        [Theory]
        [InlineData('x')] // 1 UTF-8 byte
        [InlineData('\u00e9')] // LATIN SMALL LETTER E WITH ACUTE (2 UTF-8 bytes)
        [InlineData('\u2130')] // SCRIPT CAPITAL E (3 UTF-8 bytes)
        public void WriteSingleChar_NotUtf8NoArrayPoolRentalNeeded(char ch)
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream, Encoding.Unicode /* little endian */);

            writer.Write(ch);

            Assert.Equal(Encoding.Unicode.GetBytes(new char[] { ch }), stream.ToArray());
        }

        [Fact]
        public void WriteSingleChar_ArrayPoolRentalNeeded()
        {
            string replacementString = new string('v', 10_000);
            Encoding encoding = Encoding.GetEncoding("ascii", new EncoderReplacementFallback(replacementString), DecoderFallback.ExceptionFallback);
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream, encoding);

            writer.Write('\uFFFD'); // not ASCII

            Assert.Equal(Encoding.ASCII.GetBytes(replacementString), stream.ToArray());
        }

        [Theory]
        [InlineData(8 * 1024)] // both char count & byte count within 64k rental boundary
        [InlineData(32 * 1024)] // char count within 64k rental boundary, byte count not
        [InlineData(256 * 1024)] // neither char count nor byte count within 64k rental boundary
        public void WriteChars_FastUtf8(int stringLengthInChars)
        {
            string stringToWrite = GenerateLargeUnicodeString(stringLengthInChars);
            byte[] expectedBytes = Encoding.UTF8.GetBytes(stringToWrite);

            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            writer.Write(stringToWrite.ToCharArray()); // writing a char buffer doesn't emit the length upfront 
            Assert.Equal(expectedBytes, stream.GetBuffer()[..expectedBytes.Length]);
        }

        [Theory]
        [InlineData(24)] // within stackalloc path
        [InlineData(8 * 1024)] // both char count & byte count within 64k rental boundary
        [InlineData(32 * 1024)] // char count within 64k rental boundary, byte count not
        [InlineData(256 * 1024)] // neither char count nor byte count within 64k rental boundary
        public void WriteString_FastUtf8(int stringLengthInChars)
        {
            string stringToWrite = GenerateLargeUnicodeString(stringLengthInChars);
            byte[] expectedBytes = Encoding.UTF8.GetBytes(stringToWrite);

            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            writer.Write(stringToWrite);
            stream.Position = 0;

            Assert.Equal(expectedBytes.Length /* byte count */, new BinaryReader(stream).Read7BitEncodedInt());
            Assert.Equal(expectedBytes, stream.GetBuffer()[Get7BitEncodedIntByteLength((uint)expectedBytes.Length)..(int)stream.Length]);
        }

        [Theory]
        [InlineData(127 / 3)] // within stackalloc fast path
        [InlineData(127 / 3 + 1)] // not within stackalloc fast path
        public void WriteString_FastUtf8_UsingThreeByteChars(int stringLengthInChars)
        {
            string stringToWrite = new string('\u2023', stringLengthInChars); // TRIANGULAR BULLET
            byte[] expectedBytes = Encoding.UTF8.GetBytes(stringToWrite);

            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            writer.Write(stringToWrite);
            stream.Position = 0;

            Assert.Equal(expectedBytes.Length /* byte count */, new BinaryReader(stream).Read7BitEncodedInt());
            Assert.Equal(expectedBytes, stream.GetBuffer()[Get7BitEncodedIntByteLength((uint)expectedBytes.Length)..(int)stream.Length]);
        }

        [Theory]
        [InlineData(8 * 1024)] // both char count & byte count within 64k rental boundary
        [InlineData(48 * 1024)] // char count within 64k rental boundary, byte count not
        [InlineData(256 * 1024)] // neither char count nor byte count within 64k rental boundary
        public void WriteString_NotUtf8(int stringLengthInChars)
        {
            string stringToWrite = GenerateLargeUnicodeString(stringLengthInChars);
            byte[] expectedBytes = Encoding.Unicode.GetBytes(stringToWrite);

            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream, Encoding.Unicode /* little endian */);

            writer.Write(stringToWrite);
            stream.Position = 0;

            Assert.Equal(expectedBytes.Length /* byte count */, new BinaryReader(stream).Read7BitEncodedInt());
            Assert.Equal(expectedBytes, stream.GetBuffer()[Get7BitEncodedIntByteLength((uint)expectedBytes.Length)..(int)stream.Length]);
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Android, "OOM on Android could be uncatchable & kill the test runner")]
        public unsafe void WriteChars_VeryLargeArray_DoesNotOverflow()
        {
            const nuint INPUT_LEN_IN_CHARS = 1_500_000_000;
            const nuint OUTPUT_LEN_IN_BYTES = 3_500_000_000; // overallocate

            SafeBuffer unmanagedInputBuffer = null;
            SafeBuffer unmanagedOutputBufer = null;
            try
            {
                try
                {
                    unmanagedInputBuffer = SafeBufferUtil.CreateSafeBuffer(INPUT_LEN_IN_CHARS * sizeof(char));
                    unmanagedOutputBufer = SafeBufferUtil.CreateSafeBuffer(OUTPUT_LEN_IN_BYTES * sizeof(byte));
                }
                catch (OutOfMemoryException)
                {
                    return; // skip test in low-mem conditions
                }

                Span<char> inputSpan = new Span<char>((char*)unmanagedInputBuffer.DangerousGetHandle(), (int)INPUT_LEN_IN_CHARS);
                inputSpan.Fill('\u0224'); // LATIN CAPITAL LETTER Z WITH HOOK
                Stream outStream = new UnmanagedMemoryStream(unmanagedOutputBufer, 0, (long)unmanagedOutputBufer.ByteLength, FileAccess.ReadWrite);
                BinaryWriter writer = new BinaryWriter(outStream);

                writer.Write(inputSpan); // will write 3 billion bytes to the output

                Assert.Equal(3_000_000_000, outStream.Position);
            }
            finally
            {
                unmanagedInputBuffer?.Dispose();
                unmanagedOutputBufer?.Dispose();
            }
        }

        private static bool IsUsingFastUtf8(BinaryWriter writer)
        {
            return (bool)writer.GetType().GetField("_useFastUtf8", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(writer);
        }

        private static string GenerateLargeUnicodeString(int charCount)
        {
            return string.Create(charCount, (object)null, static (buffer, _) =>
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    buffer[i] = (char)((i % 0xF00) + 0x100); // U+0100..U+0FFF (mix of 2-byte and 3-byte chars)
                }
            });
        }

        private static int Get7BitEncodedIntByteLength(uint value) => (BitOperations.Log2(value) / 7) + 1;

        // subclasses UTF8Encoding, but returns a non-UTF8 code page
        private class NotActuallyUTF8Encoding : UTF8Encoding
        {
            public override int CodePage => 65000; // UTF-7 code page
        }

        // subclasses UTF8Encoding, returns UTF-8 code page
        private class MyCustomUTF8Encoding : UTF8Encoding
        {
        }
    }
}
