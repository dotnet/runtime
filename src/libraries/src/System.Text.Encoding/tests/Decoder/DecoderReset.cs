// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Tests
{
    public class DecoderReset
    {
        #region Positive Test Cases
        // PosTest1: Call Reset to reset the decoder instance without perform any conversions
        [Fact]
        public void PosTest1()
        {
            Decoder decoder = Encoding.UTF8.GetDecoder();
            decoder.Reset();
        }

        // PosTest2: Call Reset to reset the decoder instance after a valid conversions
        [Fact]
        public void PosTest2()
        {
            Decoder decoder = Encoding.UTF8.GetDecoder();
            byte[] bytes = new byte[127];
            char[] chars = new char[bytes.Length];
            for (int i = 0; i < bytes.Length; ++i)
            {
                bytes[i] = (byte)i;
            }

            decoder.GetChars(bytes, 0, bytes.Length, chars, 0, false);
            decoder.Reset();

            decoder.GetChars(bytes, 0, bytes.Length, chars, 0, true);
            decoder.Reset();
        }

        // PosTest3: Call Reset to reset the decoder instance after a invalid conversions
        [Fact]
        public void PosTest3()
        {
            Decoder decoder = Encoding.Unicode.GetDecoder();
            byte[] bytes = new byte[127];
            char[] chars = new char[bytes.Length];
            for (int i = 0; i < bytes.Length; ++i)
            {
                bytes[i] = (byte)i;
            }

            AssertExtensions.Throws<ArgumentException>("chars", () =>
            {
                decoder.GetChars(bytes, 0, bytes.Length, chars, chars.Length - 1, false);
            });
            decoder.Reset();

            decoder.GetChars(bytes, 0, bytes.Length, chars, 0, false);

            AssertExtensions.Throws<ArgumentException>("chars", () =>
            {
                decoder.GetChars(bytes, 0, bytes.Length, chars, chars.Length - 1, true);
            });
            decoder.Reset();
            decoder.GetChars(bytes, 0, bytes.Length, chars, 0, true);
        }
        #endregion
    }
}
