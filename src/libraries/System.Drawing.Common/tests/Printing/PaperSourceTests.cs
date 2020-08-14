// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
//  Andy Hume <andyhume32@yahoo.co.uk>
//

using Xunit;

namespace System.Drawing.Printing.Tests
{
    public class PaperSourceTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var source = new PaperSource();
            Assert.Equal(PaperSourceKind.Custom, source.Kind);
            Assert.Equal((int)PaperSourceKind.Custom, source.RawKind);
            Assert.Empty(source.SourceName);
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
        public void RawKind_Set_GetReturnsExpected(int value, PaperSourceKind expectedKind)
        {
            var source = new PaperSource
            {
                RawKind = value
            };
            Assert.Equal(value, source.RawKind);
            Assert.Equal(expectedKind, source.Kind);

            // Set same.
            source.RawKind = value;
            Assert.Equal(value, source.RawKind);
            Assert.Equal(expectedKind, source.Kind);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("sourceName")]
        public void SourceName_Set_GetReturnsExpected(string value)
        {
            var source = new PaperSource
            {
                SourceName = value
            };
            Assert.Equal(value, source.SourceName);

            // Set same.
            source.SourceName = value;
            Assert.Equal(value, source.SourceName);
        }
    }
}
