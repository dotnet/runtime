// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System.Xml;

namespace XmlDocumentTests.XmlDocumentTests
{
    public class NodeRemovingTests
    {
        [Fact]
        public static void RemoveNode()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(@"<root> <elem1>text1</elem1> <elem2>text2</elem2> </root>");

            int count = 0;
            xmlDocument.NodeRemoving += (s, e) => count++;
            xmlDocument.NodeRemoving += (s, e) => Assert.Equal(XmlNodeChangedAction.Remove, e.Action);

            Assert.Equal(0, count);
            xmlDocument.DocumentElement.RemoveChild(xmlDocument.DocumentElement.FirstChild);
            Assert.Equal(1, count);
        }

        [Fact]
        public static void RemoveEventHandler()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(@"<root> <elem1>text1</elem1> <elem2>text2</elem2> </root>");

            XmlNodeChangedEventHandler handler = (s, e) => Assert.True(false, "Handler should have been removed");
            xmlDocument.NodeRemoving += handler;
            xmlDocument.NodeRemoving -= handler;

            xmlDocument.DocumentElement.RemoveChild(xmlDocument.DocumentElement.FirstChild);
        }
    }
}