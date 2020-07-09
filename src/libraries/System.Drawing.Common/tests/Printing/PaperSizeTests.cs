// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

using System.Collections.Generic;
using Xunit;

namespace System.Drawing.Printing.Tests
{
    public class PaperSizeTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var size = new PaperSize();
            Assert.Equal(PaperKind.Custom, size.Kind);
            Assert.Equal(0, size.Height);
            Assert.Empty(size.PaperName);
            Assert.Equal(0, size.RawKind);
            Assert.Equal(0, size.Width);
        }

        [Theory]
        [InlineData(null, -1, -2)]
        [InlineData("", 0, 0)]
        [InlineData("name", 100, 200)]
        public void Ctor_String_Int_Int(string name, int width, int height)
        {
            var size = new PaperSize(name, width, height);
            Assert.Equal(PaperKind.Custom, size.Kind);
            Assert.Equal(height, size.Height);
            Assert.Equal(name, size.PaperName);
            Assert.Equal(0, size.RawKind);
            Assert.Equal(width, size.Width);
        }

        public static IEnumerable<object[]> RawKind_TestData()
        {
            yield return new object[] { (int)PaperKind.A4 };
            yield return new object[] { (int)PaperKind.JapaneseEnvelopeKakuNumber3 };
            yield return new object[] { (int)PaperKind.Custom };
            yield return new object[] { 999999 };
            yield return new object[] { int.MaxValue };
            yield return new object[] { -1 };
            yield return new object[] { int.MinValue };
            yield return new object[] { 2 };
            yield return new object[] { 1 + (int)PaperKind.PrcEnvelopeNumber10Rotated };
        }

        public static IEnumerable<object[]> Height_Set_TestData()
        {
            foreach (object[] testData in RawKind_TestData())
            {
                yield return new object[] { testData[0], -1 };
                yield return new object[] { testData[0], 0 };
                yield return new object[] { testData[0], 100 };
            }
        }

        [Theory]
        [MemberData(nameof(Height_Set_TestData))]
        public void Height_Set_GetReturnsExpected(int rawKind, int value)
        {
            var size = new PaperSize
            {
                RawKind = rawKind,
                Height = value
            };
            Assert.Equal(value, size.Height);

            // Set same.
            size.Height = value;
            Assert.Equal(value, size.Height);
        }

        public static IEnumerable<object[]> NonCustomRawKind_TestData()
        {
            yield return new object[] { (int)PaperKind.A4 };
            yield return new object[] { (int)PaperKind.JapaneseEnvelopeKakuNumber3 };
            yield return new object[] { 999999 };
            yield return new object[] { int.MaxValue };
            yield return new object[] { -1 };
            yield return new object[] { int.MinValue };
            yield return new object[] { 1 + (int)PaperKind.PrcEnvelopeNumber10Rotated };
        }

        [Theory]
        [MemberData(nameof(NonCustomRawKind_TestData))]
        public void Height_SetNonCustomKindConstructor_ThrowsArgumentException(int rawKind)
        {
            var size = new PaperSize("name", 100, 200)
            {
                RawKind = rawKind
            };
            AssertExtensions.Throws<ArgumentException>("value", null, () => size.Height = 1);
        }

        public static IEnumerable<object[]> PaperName_Set_TestData()
        {
            foreach (object[] testData in RawKind_TestData())
            {
                yield return new object[] { testData[0], null };
                yield return new object[] { testData[0], string.Empty };
                yield return new object[] { testData[0], "name" };
            }
        }

        [Theory]
        [MemberData(nameof(PaperName_Set_TestData))]
        public void PaperName_Set_GetReturnsExpected(int rawKind, string value)
        {
            var size = new PaperSize
            {
                RawKind = rawKind,
                PaperName = value
            };
            Assert.Equal(value, size.PaperName);

            // Set same.
            size.PaperName = value;
            Assert.Equal(value, size.PaperName);
        }

        [Theory]
        [MemberData(nameof(NonCustomRawKind_TestData))]
        public void PaperName_SetNonCustomKindConstructor_ThrowsArgumentException(int rawKind)
        {
            var size = new PaperSize("name", 100, 200)
            {
                RawKind = rawKind
            };
            AssertExtensions.Throws<ArgumentException>("value", null, () => size.PaperName = "name");
        }

        [Theory]
        [InlineData((int)PaperKind.Custom, PaperKind.Custom)]
        [InlineData((int)PaperKind.A4, PaperKind.A4)]
        [InlineData((int)PaperKind.JapaneseEnvelopeKakuNumber3, PaperKind.JapaneseEnvelopeKakuNumber3)]
        [InlineData(999999, PaperKind.Custom)]
        [InlineData(int.MaxValue, PaperKind.Custom)]
        [InlineData(1 + (int)PaperKind.PrcEnvelopeNumber10Rotated, PaperKind.Custom)]
        [InlineData(-1, (PaperKind)(-1))]
        [InlineData(int.MinValue, (PaperKind)int.MinValue)]
        public void RawKind_Set_GetReturnsExpected(int value, PaperKind expectedKind)
        {
            var size = new PaperSize
            {
                RawKind = value
            };
            Assert.Equal(value, size.RawKind);
            Assert.Equal(expectedKind, size.Kind);

            // Set same.
            size.RawKind = value;
            Assert.Equal(value, size.RawKind);
            Assert.Equal(expectedKind, size.Kind);
        }

        public static IEnumerable<object[]> Width_Set_TestData()
        {
            foreach (object[] testData in RawKind_TestData())
            {
                yield return new object[] { testData[0], -1 };
                yield return new object[] { testData[0], 0 };
                yield return new object[] { testData[0], 100 };
            }
        }

        [Theory]
        [MemberData(nameof(Width_Set_TestData))]
        public void Width_Set_GetReturnsExpected(int rawKind, int value)
        {
            var size = new PaperSize
            {
                RawKind = rawKind,
                Width = value
            };
            Assert.Equal(value, size.Width);

            // Set same.
            size.Width = value;
            Assert.Equal(value, size.Width);
        }

        [Theory]
        [MemberData(nameof(NonCustomRawKind_TestData))]
        public void Width_SetNonCustomKindConstructor_ThrowsArgumentException(int rawKind)
        {
            var size = new PaperSize("name", 100, 200)
            {
                RawKind = rawKind
            };
            AssertExtensions.Throws<ArgumentException>("value", null, () => size.Width = 1);
        }

        public static IEnumerable<object[]> ToString_TestData()
        {
            yield return new object[] { new PaperSize(), "[PaperSize  Kind=Custom Height=0 Width=0]" };
            yield return new object[] { new PaperSize("name", 1, 2), "[PaperSize name Kind=Custom Height=2 Width=1]" };
            yield return new object[] { new PaperSize("name", -1, -2), "[PaperSize name Kind=Custom Height=-2 Width=-1]" };
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public void ToString_Invoke_ReturnsExpected(PaperSize size, string expected)
        {
            Assert.Equal(expected, size.ToString());
        }
    }
}
