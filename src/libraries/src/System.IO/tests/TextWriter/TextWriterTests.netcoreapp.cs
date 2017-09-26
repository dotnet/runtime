// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public partial class TextWriterTests
    {
        [Fact]
        public void WriteCharSpanTest()
        {
            using (CharArrayTextWriter tw = NewTextWriter)
            {
                var rs = new ReadOnlySpan<char>(TestDataProvider.CharData, 4, 6);
                tw.Write(rs);
                Assert.Equal(new string(rs), tw.Text);
            }
        }

        [Fact]
        public void WriteLineCharSpanTest()
        {
            using (CharArrayTextWriter tw = NewTextWriter)
            {
                var rs = new ReadOnlySpan<char>(TestDataProvider.CharData, 4, 6);
                tw.WriteLine(rs);
                Assert.Equal(new string(rs) + tw.NewLine, tw.Text);
            }
        }
    }
}
