// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// Copyright (C) 2005-2006 Novell, Inc (http://www.novell.com)
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

using System.Collections.Generic;
using Xunit;

namespace System.Drawing.Imaging.Tests
{
    public class PropertyItemTests
    {
        private const string PngFile = "16x16_nonindexed_24bit.png";

        public static IEnumerable<object[]> Properties_TestData()
        {
            yield return new object[] { int.MaxValue, int.MaxValue, short.MaxValue, new byte[1] { 0 } };
            yield return new object[] { int.MinValue, int.MinValue, short.MinValue, new byte[2] { 1, 1} };
            yield return new object[] { 0, 0, 0, new byte[0] };
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Properties_TestData))]
        public void Properties_SetValues_ReturnsExpected(int id, int len, short type, byte[] value)
        {
            using (Image image = new Bitmap(Helpers.GetTestBitmapPath(PngFile)))
            using (Image clone = (Image)image.Clone())
            {
                PropertyItem[] propItems = clone.PropertyItems;
                PropertyItem propItem = propItems[0];
                Assert.Equal(771, propItem.Id);
                Assert.Equal(1, propItem.Len);
                Assert.Equal(1, propItem.Type);
                Assert.Equal(new byte[1] { 0 }, propItem.Value);

                propItem.Id = id;
                propItem.Len = len;
                propItem.Type = type;
                propItem.Value = value;

                Assert.Equal(id, propItem.Id);
                Assert.Equal(len, propItem.Len);
                Assert.Equal(type, propItem.Type);
                Assert.Equal(value, propItem.Value);
            }
        }
    }
}
