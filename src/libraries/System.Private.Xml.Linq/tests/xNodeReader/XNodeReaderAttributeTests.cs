// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using CoreXml.Test.XLinq;
using Xunit;

namespace System.Xml.Linq.xNodeReader.Tests
{
    public class XNodeReaderAttributeTests
    {
        private readonly BridgeHelpers _bridgeHelpers;

        public XNodeReaderAttributeTests()
        {
            _bridgeHelpers = new BridgeHelpers();
        }

        [Fact]
        public void GetAttributeThrowsOnIndexMinusOne()
        {
            var dataReader = _bridgeHelpers.GetReader();
            _bridgeHelpers.PositionOnElement(dataReader, "ACT1");

            Assert.Throws<ArgumentOutOfRangeException>(() => dataReader.GetAttribute(-1));
        }

        [Fact]
        public void GetAttributeThrowsOnIndexMinusTwo()
        {
            XmlReader dataReader = _bridgeHelpers.GetReader();
            _bridgeHelpers.PositionOnElement(dataReader, "ACT1");

            Assert.Throws<ArgumentOutOfRangeException>(() => dataReader.GetAttribute(-2));
        }

        [Fact]
        public void IndexerThrowsOnIndexMinusOne()
        {
            var dataReader = _bridgeHelpers.GetReader();
            _bridgeHelpers.PositionOnElement(dataReader, "ACT1");
            Assert.Throws<ArgumentOutOfRangeException>(() => dataReader[-1]);
        }

        [Fact]
        public void IndexerThrowsOnIndexMinusTwo()
        {
            var dataReader = _bridgeHelpers.GetReader();
            _bridgeHelpers.PositionOnElement(dataReader, "ACT1");

            Assert.Throws<ArgumentOutOfRangeException>(() => dataReader[-2]);
        }

    }
}
