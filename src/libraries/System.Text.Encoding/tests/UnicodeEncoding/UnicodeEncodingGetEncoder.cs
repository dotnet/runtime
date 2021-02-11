// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Text.Tests
{
    public class UnicodeEncodingGetEncoder
    {
        [Fact]
        public void GetEncoder()
        {
            char[] chars = "abc\u1234\uD800\uDC00defg".ToCharArray();
            byte[] bytes = new UnicodeEncoding().GetBytes(chars);
            byte[] desBytes = new byte[20];
            int buffer;
            int outChars;
            bool completed;
            Encoder encoder = new UnicodeEncoding().GetEncoder();
            encoder.Convert(chars, 0, 10, desBytes, 0, 20, true, out buffer, out outChars, out completed);
            if (completed)
            {
                Assert.Equal(bytes, desBytes);
            }
        }
    }
}
