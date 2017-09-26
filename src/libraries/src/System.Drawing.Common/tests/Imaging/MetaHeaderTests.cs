// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Drawing.Imaging.Tests
{
    public class MetaHeaderTests
    {
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Ctor_Default()
        {
            MetaHeader mh = new MetaHeader();
            Assert.Equal(0, mh.HeaderSize);
            Assert.Equal(0, mh.MaxRecord);
            Assert.Equal(0, mh.NoObjects);
            Assert.Equal(0, mh.NoParameters);
            Assert.Equal(0, mh.Size);
            Assert.Equal(0, mh.Type);
            Assert.Equal(0, mh.Version);
        }

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(short.MaxValue)]
        [InlineData(0)]
        [InlineData(short.MinValue)]
        public void ShortProperties_SetValues_ReturnsExpected(short value)
        {
            MetaHeader mh = new MetaHeader();
            mh.HeaderSize = value;
            mh.NoObjects = value;
            mh.NoParameters = value;
            mh.Type = value;
            mh.Version = value;
            Assert.Equal(value, mh.HeaderSize);
            Assert.Equal(value, mh.NoObjects);
            Assert.Equal(value, mh.NoParameters);
            Assert.Equal(value, mh.Type);
            Assert.Equal(value, mh.Version);
        }

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(int.MaxValue)]
        [InlineData(0)]
        [InlineData(int.MinValue)]
        public void IntProperties_SetValues_ReturnsExpected(int value)
        {
            MetaHeader mh = new MetaHeader();
            mh.Size = value;
            mh.MaxRecord = value;
            Assert.Equal(value, mh.Size);
            Assert.Equal(value, mh.MaxRecord);
        }
    }
}
