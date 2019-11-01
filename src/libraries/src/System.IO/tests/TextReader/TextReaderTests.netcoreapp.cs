// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public partial class TextReaderTests
    {
        [Fact]
        public void ReadSpan()
        {
            (char[] chArr, CharArrayTextReader textReader) baseInfo = GetCharArray();
            using (CharArrayTextReader tr = baseInfo.textReader)
            {
                char[] chArr = new char[baseInfo.chArr.Length];
                var chSpan = new Span<char>(chArr, 0, baseInfo.chArr.Length);

                var read = tr.Read(chSpan);
                Assert.Equal(chArr.Length, read);

                for (int i = 0; i < baseInfo.chArr.Length; i++)
                {
                    Assert.Equal(baseInfo.chArr[i], chArr[i]);
                }
            }
        }

        [Fact]
        public void ReadBlockSpan()
        {
            (char[] chArr, CharArrayTextReader textReader) baseInfo = GetCharArray();
            using (CharArrayTextReader tr = baseInfo.textReader)
            {
                char[] chArr = new char[baseInfo.chArr.Length];
                var chSpan = new Span<char>(chArr, 0, baseInfo.chArr.Length);

                var read = tr.ReadBlock(chSpan);
                Assert.Equal(chArr.Length, read);

                for (int i = 0; i < baseInfo.chArr.Length; i++)
                {
                    Assert.Equal(baseInfo.chArr[i], chArr[i]);
                }
            }
        }
    }
}
