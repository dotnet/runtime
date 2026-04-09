// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Text.Tests
{
    public class UnicodeEncodingGetDecoder
    {
        [Fact]
        public void GetDecoder()
        {
            char[] sourceChars = "abc\u1234\uD800\uDC00defg".ToCharArray();
            char[] destinationChars = new char[10];
            byte[] bytes = new UnicodeEncoding().GetBytes(sourceChars);
            int bytesUsed;
            int charsUsed;
            bool completed;
            Decoder decoder = new UnicodeEncoding().GetDecoder();
            decoder.Convert(bytes, 0, 20, destinationChars, 0, 10, true, out bytesUsed, out charsUsed, out completed);
            if (completed)
            {
                Assert.Equal(sourceChars, destinationChars);
            }
        }
    }
}
