// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class EncoderConvert2Encoder : Encoder
    {
        private Encoder _encoder = null;

        public EncoderConvert2Encoder()
        {
            _encoder = Encoding.UTF8.GetEncoder();
        }

        public override int GetByteCount(char[] chars, int index, int count, bool flush)
        {
            if (index >= count)
                throw new ArgumentException();

            return _encoder.GetByteCount(chars, index, count, flush);
        }

        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex, bool flush)
        {
            return _encoder.GetBytes(chars, charIndex, charCount, bytes, byteIndex, flush);
        }
    }

    // Convert(System.Char[],System.Int32,System.Int32,System.Byte[],System.Int32,System.Int32,System.Boolean,System.Int32@,System.Int32@,System.Boolean@)
    public class EncoderConvert2
    {
        private const int c_SIZE_OF_ARRAY = 256;
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        public static IEnumerable<object[]> Encoders_RandomInput()
        {
            yield return new object[] { Encoding.ASCII.GetEncoder() };
            yield return new object[] { Encoding.UTF8.GetEncoder() };
            yield return new object[] { Encoding.Unicode.GetEncoder() };
        }
              
        public static IEnumerable<object[]> Encoders_Convert()
        {
            yield return new object[] { Encoding.ASCII.GetEncoder(), 1 };
            yield return new object[] { Encoding.UTF8.GetEncoder(), 1 };
            yield return new object[] { Encoding.Unicode.GetEncoder(), 2 };
        }
        
        // Call Convert to convert an arbitrary character array encoders
        [Theory]
        [MemberData(nameof(Encoders_RandomInput))]
        public void EncoderConvertRandomCharArray(Encoder encoder)
        {
            char[] chars = new char[c_SIZE_OF_ARRAY];
            byte[] bytes = new byte[c_SIZE_OF_ARRAY];

            for (int i = 0; i < chars.Length; ++i)
            {
                chars[i] = _generator.GetChar(-55);
            }

            int charsUsed;
            int bytesUsed;
            bool completed;
            encoder.Convert(chars, 0, chars.Length, bytes, 0, bytes.Length, false, out charsUsed, out bytesUsed, out completed);

            // set flush to true and try again
            encoder.Convert(chars, 0, chars.Length, bytes, 0, bytes.Length, true, out charsUsed, out bytesUsed, out completed);
        }

        // Call Convert to convert a ASCII character array encoders
        [Theory]
        [MemberData(nameof(Encoders_Convert))]
        public void EncoderConvertASCIICharArray(Encoder encoder, int multiplier)
        {
            char[] chars = "TestLibrary.TestFramework.BeginScenario".ToCharArray();
            byte[] bytes = new byte[chars.Length * multiplier];

            VerificationHelper(encoder, chars, 0, chars.Length, bytes, 0, bytes.Length, false, chars.Length, chars.Length * multiplier, expectedCompleted: true);
            VerificationHelper(encoder, chars, 0, chars.Length, bytes, 0, bytes.Length, true, chars.Length, chars.Length * multiplier, expectedCompleted: true);
            VerificationHelper(encoder, chars, 0, 0, bytes, 0, 0, true, 0, 0, expectedCompleted: true);
        }

        // Call Convert to convert a ASCII character array with user implemented encoder
        [Fact]
        public void EncoderCustomConvertASCIICharArray()
        {
            char[] chars = "TestLibrary.TestFramework.BeginScenario".ToCharArray();
            byte[] bytes = new byte[chars.Length];
            Encoder encoder = new EncoderConvert2Encoder();

            VerificationHelper(encoder, chars, 0, chars.Length, bytes, 0, bytes.Length, false, chars.Length, chars.Length, expectedCompleted: true);
            VerificationHelper(encoder, chars, 0, chars.Length, bytes, 0, bytes.Length, true, chars.Length, chars.Length, expectedCompleted: true);
        }

        // Call Convert to convert partial of a ASCII character array with UTF8 encoder
        [Fact]
        public void EncoderUTF8ConvertASCIICharArrayPartial()
        {
            char[] chars = "TestLibrary.TestFramework.BeginScenario".ToCharArray();
            byte[] bytes = new byte[chars.Length];
            Encoder encoder = Encoding.UTF8.GetEncoder();

            VerificationHelper(encoder, chars, 0, 1, bytes, 0, 1, false, 1, 1, expectedCompleted: true);
            VerificationHelper(encoder, chars, 0, 1, bytes, 0, 1, true, 1, 1, expectedCompleted: true);
            VerificationHelper(encoder, chars, 1, 1, bytes, 0, 1, false, 1, 1, expectedCompleted: true);
            VerificationHelper(encoder, chars, 1, 1, bytes, 0, 1, true, 1, 1, expectedCompleted: true);
            VerificationHelper(encoder, chars, 1, 1, bytes, 1, 1, false, 1, 1, expectedCompleted: true);
            VerificationHelper(encoder, chars, 1, 1, bytes, 1, 1, true, 1, 1, expectedCompleted: true);

            // Verify maxBytes is large than character count
            VerificationHelper(encoder, chars, 0, chars.Length - 1, bytes, 0, bytes.Length, false, chars.Length - 1, chars.Length - 1, expectedCompleted: true);
            VerificationHelper(encoder, chars, 1, chars.Length - 1, bytes, 0, bytes.Length, true, chars.Length - 1, chars.Length - 1, expectedCompleted: true);
        }

        // Call Convert to convert partial of a ASCII character array with Unicode encoder
        [Fact]
        public void EncoderUnicodeConvertASCIICharArrayPartial()
        {
            char[] chars = "TestLibrary.TestFramework.BeginScenario".ToCharArray();
            byte[] bytes = new byte[chars.Length * 2];
            Encoder encoder = Encoding.Unicode.GetEncoder();

            VerificationHelper(encoder, chars, 0, 1, bytes, 0, 2, false, 1, 2, expectedCompleted: true);
            VerificationHelper(encoder, chars, 0, 1, bytes, 0, 2, true, 1, 2, expectedCompleted: true);
            VerificationHelper(encoder, chars, 1, 1, bytes, 0, 2, false, 1, 2, expectedCompleted: true);
            VerificationHelper(encoder, chars, 1, 1, bytes, 0, 2, true, 1, 2, expectedCompleted: true);
            VerificationHelper(encoder, chars, 1, 1, bytes, 1, 2, false, 1, 2, expectedCompleted: true);
            VerificationHelper(encoder, chars, 1, 1, bytes, 1, 2, true, 1, 2, expectedCompleted: true);

            VerificationHelper(encoder, chars, 0, 1, bytes, 0, bytes.Length, false, 1, 2, expectedCompleted: true);
            VerificationHelper(encoder, chars, 0, 1, bytes, 0, bytes.Length, true, 1, 2, expectedCompleted: true);
        }

        // Call Convert to convert a Unicode character array with Unicode encoder
        [Fact]
        public void EncoderUnicodeConvertUnicodeCharArray()
        {
            char[] chars = "\u8FD9\u4E2A\u4E00\u4E2A\u6D4B\u8BD5".ToCharArray();
            byte[] bytes = new byte[chars.Length * 2];
            Encoder encoder = Encoding.Unicode.GetEncoder();

            VerificationHelper(encoder, chars, 0, chars.Length, bytes, 0, bytes.Length, false, chars.Length, bytes.Length, expectedCompleted: true);
            VerificationHelper(encoder, chars, 0, chars.Length, bytes, 0, bytes.Length, true, chars.Length, bytes.Length, expectedCompleted: true);
        }

        // Call Convert to convert partial of a Unicode character array with Unicode encoder
        [Fact]
        public void EncoderUnicodeConvertUnicodeCharArrayPartial()
        {
            char[] chars = "\u8FD9\u4E2A\u4E00\u4E2A\u6D4B\u8BD5".ToCharArray();
            byte[] bytes = new byte[chars.Length * 2];
            Encoder encoder = Encoding.Unicode.GetEncoder();

            VerificationHelper(encoder, chars, 0, 1, bytes, 0, 2, false, 1, 2, expectedCompleted: true);
            VerificationHelper(encoder, chars, 0, 1, bytes, 0, 2, true, 1, 2, expectedCompleted: true);
            VerificationHelper(encoder, chars, 1, 1, bytes, 0, 2, false, 1, 2, expectedCompleted: true);
            VerificationHelper(encoder, chars, 1, 1, bytes, 0, 2, true, 1, 2, expectedCompleted: true);
            VerificationHelper(encoder, chars, 1, 1, bytes, 1, 2, false, 1, 2, expectedCompleted: true);
            VerificationHelper(encoder, chars, 1, 1, bytes, 1, 2, true, 1, 2, expectedCompleted: true);

            VerificationHelper(encoder, chars, 0, 1, bytes, 0, bytes.Length, false, 1, 2, expectedCompleted: true);
            VerificationHelper(encoder, chars, 0, 1, bytes, 0, bytes.Length, true, 1, 2, expectedCompleted: true);
        }
        
        // Call Convert to convert partial of a ASCII character array with ASCII encoder
        [Fact]
        public void EncoderASCIIConvertASCIICharArrayPartial()
        {
            char[] chars = "TestLibrary.TestFramework.BeginScenario".ToCharArray();
            byte[] bytes = new byte[chars.Length];
            Encoder encoder = Encoding.ASCII.GetEncoder();

            VerificationHelper(encoder, chars, 0, 1, bytes, 0, 1, false, 1, 1, expectedCompleted: true);
            VerificationHelper(encoder, chars, 0, 1, bytes, 0, 1, true, 1, 1, expectedCompleted: true);
            VerificationHelper(encoder, chars, 1, 1, bytes, 0, 1, false, 1, 1, expectedCompleted: true);
            VerificationHelper(encoder, chars, 1, 1, bytes, 0, 1, true, 1, 1, expectedCompleted: true);
            VerificationHelper(encoder, chars, 1, 1, bytes, 1, 1, false, 1, 1, expectedCompleted: true);
            VerificationHelper(encoder, chars, 1, 1, bytes, 1, 1, true, 1, 1, expectedCompleted: true);

            // Verify maxBytes is large than character count
            VerificationHelper(encoder, chars, 0, chars.Length - 1, bytes, 0, bytes.Length, false, chars.Length - 1, chars.Length - 1, expectedCompleted: true);
            VerificationHelper(encoder, chars, 1, chars.Length - 1, bytes, 0, bytes.Length, true, chars.Length - 1, chars.Length - 1, expectedCompleted: true);
        }
        
        // Call Convert to convert partial of a Unicode character array with ASCII encoder
        [Fact]
        public void EncoderASCIIConvertUnicodeCharArrayPartial()
        {
            char[] chars = "\uD83D\uDE01Test".ToCharArray();
            byte[] bytes = new byte[chars.Length * 2];
            Encoder encoder = Encoding.ASCII.GetEncoder();

            VerificationHelper(encoder, chars, 0, 1, bytes, 0, 1, true, 1, 1, expectedCompleted: true);
            VerificationHelper(encoder, chars, 0, 1, bytes, 0, 1, false, 1, 1, expectedCompleted: true);
            VerificationHelper(encoder, chars, 0, 2, bytes, 0, 2, false, 2, 2, expectedCompleted: true);
            VerificationHelper(encoder, chars, 0, 4, bytes, 0, 4, false, 4, 4, expectedCompleted: true);
            VerificationHelper(encoder, chars, 0, 4, bytes, 0, 4, true, 4, 4, expectedCompleted: true);
            VerificationHelper(encoder, chars, 2, 2, bytes, 0, 2, true, 2, 2, expectedCompleted: true);
            VerificationHelper(encoder, chars, 1, 3, bytes, 1, 3, false, 3, 3, expectedCompleted: true);
            VerificationHelper(encoder, chars, 1, 3, bytes, 1, 5, true, 3, 3, expectedCompleted: true);

            VerificationHelper(encoder, chars, 0, 1, bytes, 0, bytes.Length, false, 1, 1, expectedCompleted: true);
            VerificationHelper(encoder, chars, 1, 1, bytes, 0, bytes.Length, true, 1, 1, expectedCompleted: true);
        }
        
        // Call Convert to convert partial of a Unicode character array with UTF8 encoder
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // [ActiveIssue(11057)]
        public void EncoderUTF8ConvertUnicodeCharArrayPartial()
        {
            char[] chars = "\uD83D\uDE01Test".ToCharArray();
            byte[] bytes = new byte[chars.Length * 2];
            Encoder encoder = Encoding.UTF8.GetEncoder();

            VerificationHelper(encoder, chars, 0, 1, bytes, 0, 3, true, 1, 3, expectedCompleted: true);
            VerificationHelper(encoder, chars, 0, 2, bytes, 0, 7, false, 2, 4, expectedCompleted: true);
            VerificationHelper(encoder, chars, 0, 4, bytes, 0, 6, false, 4, 6, expectedCompleted: true);
            VerificationHelper(encoder, chars, 0, 4, bytes, 0, 6, true, 4, 6, expectedCompleted: true);
            VerificationHelper(encoder, chars, 2, 2, bytes, 0, 2, true, 2, 2, expectedCompleted: true);
            VerificationHelper(encoder, chars, 1, 3, bytes, 1, 3, false, 1, 3, expectedCompleted: false);
            VerificationHelper(encoder, chars, 1, 3, bytes, 1, 5, true, 3, 5, expectedCompleted: true);

            VerificationHelper(encoder, chars, 0, 1, bytes, 0, bytes.Length, false, 1, 0, expectedCompleted: false);
            VerificationHelper(encoder, chars, 1, 1, bytes, 0, bytes.Length, true, 1, 4, expectedCompleted: true);
        }
 
        // Call Convert to convert partial of a ASCII+Unicode character array with ASCII encoder
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // [ActiveIssue(11057)]
        public void EncoderASCIIConvertMixedASCIIUnicodeCharArrayPartial()
        {
            char[] chars = "T\uD83D\uDE01est".ToCharArray();
            byte[] bytes = new byte[chars.Length * 2];
            Encoder encoder = Encoding.ASCII.GetEncoder();

            VerificationHelper(encoder, chars, 0, 1, bytes, 0, 1, true, 1, 1, expectedCompleted: true);
            VerificationHelper(encoder, chars, 0, 4, bytes, 0, 1, false, 3, 1, expectedCompleted: false);
            VerificationHelper(encoder, chars, 3, 1, bytes, 0, 2, false, 0, 2, expectedCompleted: false);
            VerificationHelper(encoder, chars, 3, 1, bytes, 0, 2, false, 1, 1, expectedCompleted: true);
            VerificationHelper(encoder, chars, 0, 5, bytes, 0, 5, false, 5, 5, expectedCompleted: true);
            VerificationHelper(encoder, chars, 0, 4, bytes, 0, 4, true, 4, 4, expectedCompleted: true);
            VerificationHelper(encoder, chars, 2, 2, bytes, 0, 2, true, 2, 2, expectedCompleted: true);
            VerificationHelper(encoder, chars, 1, 3, bytes, 1, 5, false, 3, 3, expectedCompleted: true);
            VerificationHelper(encoder, chars, 1, 3, bytes, 1, 3, true, 3, 3, expectedCompleted: true);

            VerificationHelper(encoder, chars, 0, 2, bytes, 0, bytes.Length, false, 2, 2, expectedCompleted: true);
            VerificationHelper(encoder, chars, 1, 1, bytes, 0, bytes.Length, true, 1, 1, expectedCompleted: true);
        }
        
        // Call Convert to convert partial of a ASCII+Unicode character array with UTF8 encoder
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // [ActiveIssue(11057)]
        public void EncoderUTF8ConvertMixedASCIIUnicodeCharArrayPartial()
        {
            char[] chars = "T\uD83D\uDE01est".ToCharArray();
            byte[] bytes = new byte[chars.Length * 2];
            Encoder encoder = Encoding.UTF8.GetEncoder();

            VerificationHelper(encoder, chars, 0, 1, bytes, 0, 1, true, 1, 1, expectedCompleted: true);
            VerificationHelper(encoder, chars, 0, 2, bytes, 0, 1, false, 2, 1, expectedCompleted: false);
            VerificationHelper(encoder, chars, 2, 1, bytes, 0, 5, false, 1, 4, expectedCompleted: true);
            VerificationHelper(encoder, chars, 1, 2, bytes, 0, 7, false, 2, 4, expectedCompleted: true);
            VerificationHelper(encoder, chars, 0, 5, bytes, 0, 7, false, 5, 7, expectedCompleted: true);
            VerificationHelper(encoder, chars, 0, 4, bytes, 0, 6, true, 4, 6, expectedCompleted: true);
            VerificationHelper(encoder, chars, 2, 2, bytes, 0, 3, true, 1, 3, expectedCompleted: false);
            VerificationHelper(encoder, chars, 1, 3, bytes, 1, 5, false, 3, 5, expectedCompleted: true);
            VerificationHelper(encoder, chars, 1, 3, bytes, 1, 5, true, 3, 5, expectedCompleted: true);

            VerificationHelper(encoder, chars, 0, 2, bytes, 0, bytes.Length, false, 2, 1, expectedCompleted: false);
            VerificationHelper(encoder, chars, 2, 2, bytes, 0, bytes.Length, false, 2, 5, expectedCompleted: true);
            VerificationHelper(encoder, chars, 1, 1, bytes, 0, bytes.Length, true, 1, 3, expectedCompleted: true);
        }

        private void VerificationHelper(Encoder encoder, char[] chars, int charIndex, int charCount,
            byte[] bytes, int byteIndex, int byteCount, bool flush, int expectedCharsUsed, int expectedBytesUsed,
            bool expectedCompleted)
        {
            int charsUsed;
            int bytesUsed;
            bool completed;

            encoder.Convert(chars, charIndex, charCount, bytes, byteIndex, byteCount, flush, out charsUsed, out bytesUsed, out completed);
            Assert.Equal(expectedCharsUsed, charsUsed);
            Assert.Equal(expectedBytesUsed, bytesUsed);
            Assert.Equal(expectedCompleted, completed);
        }
    }
}
