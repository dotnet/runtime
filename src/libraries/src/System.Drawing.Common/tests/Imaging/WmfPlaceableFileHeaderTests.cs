// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Drawing.Imaging.Tests
{
    public class WmfPlaceableFileHeaderTests
    {
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_Default()
        {
            WmfPlaceableFileHeader fileHeader = new WmfPlaceableFileHeader();
            Assert.Equal(0, fileHeader.BboxBottom);
            Assert.Equal(0, fileHeader.BboxLeft);
            Assert.Equal(0, fileHeader.BboxRight);
            Assert.Equal(0, fileHeader.BboxTop);
            Assert.Equal(0, fileHeader.Checksum);
            Assert.Equal(0, fileHeader.Hmf);
            Assert.Equal(0, fileHeader.Inch);
            Assert.Equal(unchecked((int)0x9aC6CDD7), fileHeader.Key);
            Assert.Equal(0, fileHeader.Reserved);
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(short.MaxValue)]
        [InlineData(0)]
        [InlineData(short.MinValue)]
        public void ShortProperties_SetValues_ReturnsExpected(short value)
        {
            WmfPlaceableFileHeader fileHeader = new WmfPlaceableFileHeader();
            fileHeader.BboxBottom = value;
            fileHeader.BboxLeft = value;
            fileHeader.BboxRight = value;
            fileHeader.BboxTop = value;
            fileHeader.Checksum = value;
            fileHeader.Hmf = value;
            fileHeader.Inch = value;
            fileHeader.Key = value;
            fileHeader.Reserved = value;
            Assert.Equal(value, fileHeader.BboxBottom);
            Assert.Equal(value, fileHeader.BboxLeft);
            Assert.Equal(value, fileHeader.BboxRight);
            Assert.Equal(value, fileHeader.BboxTop);
            Assert.Equal(value, fileHeader.Checksum);
            Assert.Equal(value, fileHeader.Hmf);
            Assert.Equal(value, fileHeader.Inch);
            Assert.Equal(value, fileHeader.Key);
            Assert.Equal(value, fileHeader.Reserved);
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(int.MaxValue)]
        [InlineData(0)]
        [InlineData(int.MinValue)]
        public void IntProperties_SetValues_ReturnsExpected(int value)
        {
            WmfPlaceableFileHeader fileHeader = new WmfPlaceableFileHeader();
            fileHeader.Key = value;
            fileHeader.Reserved = value;
            Assert.Equal(value, fileHeader.Key);
            Assert.Equal(value, fileHeader.Reserved);
        }
    }
}
