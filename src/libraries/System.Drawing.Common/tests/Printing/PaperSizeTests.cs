// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.using System;
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
    public class PaperSizeTests
    {
        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [Fact]
        public void Ctor_Default()
        {
            PaperSize ps = new PaperSize();
            Assert.Equal(PaperKind.Custom, ps.Kind);
            Assert.Equal(string.Empty, ps.PaperName);
        }

        [Fact]
        public void Ctor_Name_Width_Height()
        {
            PaperSize ps = new PaperSize("foo", 100, 200);
            Assert.Equal(PaperKind.Custom, ps.Kind);
            Assert.Equal(100, ps.Width);
            Assert.Equal(200, ps.Height);
            Assert.Equal("foo", ps.PaperName);
        }

        [Theory]
        [InlineData((int)PaperKind.A4)]
        [InlineData((int)PaperKind.JapaneseEnvelopeKakuNumber3)]
        [InlineData(999999)]
        [InlineData(int.MaxValue)]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        [InlineData(1 + (int)PaperKind.PrcEnvelopeNumber10Rotated)]
        public void PaperProperties_OnNoRealCustomKind_ThrowAnArgumentException(int rawKind)
        {
            PaperSize ps = new PaperSize("foo", 100, 100);
            ps.RawKind = rawKind;
            AssertExtensions.Throws<ArgumentException>(null, () => ps.Width = 1);
            AssertExtensions.Throws<ArgumentException>(null, () => ps.Height = 1);
            AssertExtensions.Throws<ArgumentException>(null, () => ps.PaperName = "NewName");
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [Theory]
        [InlineData((int)PaperKind.A4)]
        [InlineData((int)PaperKind.JapaneseEnvelopeKakuNumber3)]
        [InlineData((int)PaperKind.Custom)]
        [InlineData(999999)]
        [InlineData(int.MaxValue)]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        [InlineData(2)]
        [InlineData(1 + (int)PaperKind.PrcEnvelopeNumber10Rotated)]
        public void PaperProperties_DefaultCtor_Success(int rawKind)
        {
            PaperSize ps = new PaperSize();
            ps.RawKind = rawKind;
            ps.Height = 1;
            ps.Width = 1;
            ps.PaperName = "NewName";
            Assert.Equal(1, ps.Height);
            Assert.Equal(1, ps.Width);
            Assert.Equal("NewName", ps.PaperName);
        }

        [Theory]
        [InlineData((int)PaperKind.Custom, PaperKind.Custom)]
        [InlineData((int)PaperKind.A4, PaperKind.A4)]
        [InlineData((int)PaperKind.JapaneseEnvelopeKakuNumber3, PaperKind.JapaneseEnvelopeKakuNumber3)]
        [InlineData(999999, PaperKind.Custom)]
        [InlineData(int.MaxValue, PaperKind.Custom)]
        [InlineData(1 + (int)PaperKind.PrcEnvelopeNumber10Rotated, PaperKind.Custom)]
        public void RawKind_ReturnsExpected(int rawKind, int expectedKind)
        {
            PaperSize ps = new PaperSize();
            ps.RawKind = rawKind;
            Assert.Equal((PaperKind)expectedKind, ps.Kind);
            Assert.Equal(rawKind, ps.RawKind);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        public void RawKind_NegativeValues_ReturnsExpected(int rawKind)
        {
            PaperSize ps = new PaperSize();
            ps.RawKind = rawKind;
            Assert.Equal((PaperKind)rawKind, ps.Kind);
            Assert.Equal(rawKind, ps.RawKind);
        }
    }
}
