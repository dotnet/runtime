// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class XmlNodeReaderConstructionTests
    {
        [Fact]
        public void NodeReaderConstructionWithEmptyDocument()
        {
            var nodeReader = new XmlNodeReader(new XmlDocument());
            Assert.Equal(0, nodeReader.Depth);
            Assert.Equal(ReadState.Initial, nodeReader.ReadState);
            Assert.False(nodeReader.EOF);            
            Assert.Equal(XmlNodeType.None, nodeReader.NodeType);
        }

        [Fact]
        public void NodeReaderConstructionWithNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var nodeReader = new XmlNodeReader(null);
            });
        }
    }
}
