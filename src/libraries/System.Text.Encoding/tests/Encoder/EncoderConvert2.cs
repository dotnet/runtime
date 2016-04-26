// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        #region Private Fields
        private const int c_SIZE_OF_ARRAY = 256;
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();
        #endregion

        #region Positive Test Cases
        // PosTest1: Call Convert to convert a arbitrary character array with ASCII encoder
        [Fact]
        public void PosTest1()
        {
            char[] chars = new char[c_SIZE_OF_ARRAY];
            byte[] bytes = new byte[c_SIZE_OF_ARRAY];
            Encoder encoder = Encoding.UTF8.GetEncoder();

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

        // PosTest2: Call Convert to convert a arbitrary character array with Unicode encoder
        [Fact]
        public void PosTest2()
        {
            char[] chars = new char[c_SIZE_OF_ARRAY];
            byte[] bytes = new byte[c_SIZE_OF_ARRAY];
            Encoder encoder = Encoding.Unicode.GetEncoder();

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

        // PosTest3: Call Convert to convert a ASCII character array with ASCII encoder
        [Fact]
        public void PosTest3()
        {
            char[] chars = "TestLibrary.TestFramework.BeginScenario".ToCharArray();
            byte[] bytes = new byte[chars.Length];
            Encoder encoder = Encoding.UTF8.GetEncoder();

            VerificationHelper(encoder, chars, 0, chars.Length, bytes, 0, bytes.Length, false, chars.Length, chars.Length, true, "003.1");
            VerificationHelper(encoder, chars, 0, chars.Length, bytes, 0, bytes.Length, true, chars.Length, chars.Length, true, "003.2");
            VerificationHelper(encoder, chars, 0, 0, bytes, 0, 0, true, 0, 0, true, "003.3");
        }

        // PosTest4: Call Convert to convert a ASCII character array with user implemented encoder
        [Fact]
        public void PosTest4()
        {
            char[] chars = "TestLibrary.TestFramework.BeginScenario".ToCharArray();
            byte[] bytes = new byte[chars.Length];
            Encoder encoder = new EncoderConvert2Encoder();

            VerificationHelper(encoder, chars, 0, chars.Length, bytes, 0, bytes.Length, false, chars.Length, chars.Length, true, "004.1");
            VerificationHelper(encoder, chars, 0, chars.Length, bytes, 0, bytes.Length, true, chars.Length, chars.Length, true, "004.2");
        }

        // PosTest5: Call Convert to convert partial of a ASCII character array with ASCII encoder
        [Fact]
        public void PosTest5()
        {
            char[] chars = "TestLibrary.TestFramework.BeginScenario".ToCharArray();
            byte[] bytes = new byte[chars.Length];
            Encoder encoder = Encoding.UTF8.GetEncoder();

            VerificationHelper(encoder, chars, 0, 1, bytes, 0, 1, false, 1, 1, true, "005.1");
            VerificationHelper(encoder, chars, 0, 1, bytes, 0, 1, true, 1, 1, true, "005.2");
            VerificationHelper(encoder, chars, 1, 1, bytes, 0, 1, false, 1, 1, true, "005.3");
            VerificationHelper(encoder, chars, 1, 1, bytes, 0, 1, true, 1, 1, true, "005.4");
            VerificationHelper(encoder, chars, 1, 1, bytes, 1, 1, false, 1, 1, true, "005.5");
            VerificationHelper(encoder, chars, 1, 1, bytes, 1, 1, true, 1, 1, true, "005.6");

            // Verify maxBytes is large than character count
            VerificationHelper(encoder, chars, 0, chars.Length - 1, bytes, 0, bytes.Length, false, chars.Length - 1, chars.Length - 1, true, "005.7");
            VerificationHelper(encoder, chars, 1, chars.Length - 1, bytes, 0, bytes.Length, true, chars.Length - 1, chars.Length - 1, true, "005.8");
        }

        // PosTest6: Call Convert to convert a ASCII character array with Unicode encoder
        [Fact]
        public void PosTest6()
        {
            char[] chars = "TestLibrary.TestFramework.BeginScenario".ToCharArray();
            byte[] bytes = new byte[chars.Length * 2];
            Encoder encoder = Encoding.Unicode.GetEncoder();

            VerificationHelper(encoder, chars, 0, chars.Length, bytes, 0, bytes.Length, false, chars.Length, bytes.Length, true, "006.1");
            VerificationHelper(encoder, chars, 0, chars.Length, bytes, 0, bytes.Length, true, chars.Length, bytes.Length, true, "006.2");
        }

        // PosTest7: Call Convert to convert partial of a ASCII character array with Unicode encoder
        [Fact]
        public void PosTest7()
        {
            char[] chars = "TestLibrary.TestFramework.BeginScenario".ToCharArray();
            byte[] bytes = new byte[chars.Length * 2];
            Encoder encoder = Encoding.Unicode.GetEncoder();

            VerificationHelper(encoder, chars, 0, 1, bytes, 0, 2, false, 1, 2, true, "007.1");
            VerificationHelper(encoder, chars, 0, 1, bytes, 0, 2, true, 1, 2, true, "007.2");
            VerificationHelper(encoder, chars, 1, 1, bytes, 0, 2, false, 1, 2, true, "007.3");
            VerificationHelper(encoder, chars, 1, 1, bytes, 0, 2, true, 1, 2, true, "007.4");
            VerificationHelper(encoder, chars, 1, 1, bytes, 1, 2, false, 1, 2, true, "007.5");
            VerificationHelper(encoder, chars, 1, 1, bytes, 1, 2, true, 1, 2, true, "007.6");

            VerificationHelper(encoder, chars, 0, 1, bytes, 0, bytes.Length, false, 1, 2, true, "007.3");
            VerificationHelper(encoder, chars, 0, 1, bytes, 0, bytes.Length, true, 1, 2, true, "007.4");
        }

        // PosTest8: Call Convert to convert a Unicode character array with Unicode encoder
        [Fact]
        public void PosTest8()
        {
            char[] chars = "\u8FD9\u4E2A\u4E00\u4E2A\u6D4B\u8BD5".ToCharArray();
            byte[] bytes = new byte[chars.Length * 2];
            Encoder encoder = Encoding.Unicode.GetEncoder();

            VerificationHelper(encoder, chars, 0, chars.Length, bytes, 0, bytes.Length, false, chars.Length, bytes.Length, true, "008.1");
            VerificationHelper(encoder, chars, 0, chars.Length, bytes, 0, bytes.Length, true, chars.Length, bytes.Length, true, "008.2");
        }

        // PosTest9: Call Convert to convert partial of a Unicode character array with Unicode encoder
        [Fact]
        public void PosTest9()
        {
            char[] chars = "\u8FD9\u4E2A\u4E00\u4E2A\u6D4B\u8BD5".ToCharArray();
            byte[] bytes = new byte[chars.Length * 2];
            Encoder encoder = Encoding.Unicode.GetEncoder();

            VerificationHelper(encoder, chars, 0, 1, bytes, 0, 2, false, 1, 2, true, "009.1");
            VerificationHelper(encoder, chars, 0, 1, bytes, 0, 2, true, 1, 2, true, "009.2");
            VerificationHelper(encoder, chars, 1, 1, bytes, 0, 2, false, 1, 2, true, "009.3");
            VerificationHelper(encoder, chars, 1, 1, bytes, 0, 2, true, 1, 2, true, "009.4");
            VerificationHelper(encoder, chars, 1, 1, bytes, 1, 2, false, 1, 2, true, "009.5");
            VerificationHelper(encoder, chars, 1, 1, bytes, 1, 2, true, 1, 2, true, "009.6");

            VerificationHelper(encoder, chars, 0, 1, bytes, 0, bytes.Length, false, 1, 2, true, "009.3");
            VerificationHelper(encoder, chars, 0, 1, bytes, 0, bytes.Length, true, 1, 2, true, "009.4");
        }
        #endregion

        private void VerificationHelper(Encoder encoder, char[] chars, int charIndex, int charCount,
            byte[] bytes, int byteIndex, int byteCount, bool flush, int expectedCharsUsed, int expectedBytesUsed,
            bool expectedCompleted, string errorno)
        {
            int charsUsed;
            int bytesUsed;
            bool completed;

            encoder.Convert(chars, charIndex, charCount, bytes, byteIndex, byteCount, false, out charsUsed, out bytesUsed, out completed);
            Assert.Equal(expectedCharsUsed, charsUsed);
            Assert.Equal(expectedBytesUsed, bytesUsed);
            Assert.Equal(expectedCompleted, completed);
        }
    }
}
