// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// Copyright (C) 2009 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
// Author:
// 	Andy Hume <andyhume32@yahoo.co.uk>
//

using Xunit;

namespace System.Drawing.Printing.Tests
{
    public class PaperSourceTests
    {
        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [Fact]
        public void Ctor_Default()
        {
            PaperSource ps = new PaperSource();
            Assert.Equal(PaperSourceKind.Custom, ps.Kind);
            Assert.Equal((int)PaperSourceKind.Custom, ps.RawKind);
            Assert.Equal(string.Empty, ps.SourceName);
        }

        [Theory]
        [InlineData((int)PaperSourceKind.Custom, PaperSourceKind.Custom)]
        [InlineData((int)PaperSourceKind.Upper, PaperSourceKind.Upper)]
        [InlineData((int)PaperSourceKind.TractorFeed, PaperSourceKind.TractorFeed)]
        [InlineData((int)PaperSourceKind.SmallFormat, PaperSourceKind.SmallFormat)]
        [InlineData((int)PaperSourceKind.Middle, PaperSourceKind.Middle)]
        [InlineData((int)PaperSourceKind.ManualFeed, PaperSourceKind.ManualFeed)]
        [InlineData((int)PaperSourceKind.Manual, PaperSourceKind.Manual)]
        [InlineData((int)PaperSourceKind.Lower, PaperSourceKind.Lower)]
        [InlineData((int)PaperSourceKind.LargeFormat, PaperSourceKind.LargeFormat)]
        [InlineData((int)PaperSourceKind.LargeCapacity, PaperSourceKind.LargeCapacity)]
        [InlineData((int)PaperSourceKind.FormSource, PaperSourceKind.FormSource)]
        [InlineData((int)PaperSourceKind.Envelope, PaperSourceKind.Envelope)]
        [InlineData((int)PaperSourceKind.Cassette, PaperSourceKind.Cassette)]
        [InlineData((int)PaperSourceKind.AutomaticFeed, PaperSourceKind.AutomaticFeed)]
        [InlineData(int.MaxValue, PaperSourceKind.Custom)]
        [InlineData(int.MinValue, (PaperSourceKind)int.MinValue)]
        [InlineData(0, (PaperSourceKind)0)]
        [InlineData(256, PaperSourceKind.Custom)]
        public void RawKind_ReturnsExpected(int rawKind, PaperSourceKind expectedKind)
        {
            PaperSource ps = new PaperSource();
            ps.RawKind = rawKind;
            Assert.Equal(expectedKind, ps.Kind);
            Assert.Equal(rawKind, ps.RawKind);
        }

        [Fact]
        public void SourceName_Success()
        {
            PaperSource ps = new PaperSource();
            ps.SourceName = "NewName";
            Assert.Equal("NewName", ps.SourceName);
        }
    }
}
