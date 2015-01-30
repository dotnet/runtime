// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System.Xml;

namespace XmlDocumentTests.XmlDocumentTests
{
    public class NodeChangingTests
    {
        [Fact]
        public static void ChangingNodeFiresChangingEvent()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(@"<root> <elem1>text1</elem1> <elem2>text2</elem2> </root>");

            int count = 0;
            xmlDocument.NodeChanging += (s, e) => count++;
            xmlDocument.NodeChanging += (s, e) => Assert.Equal(XmlNodeChangedAction.Change, e.Action);

            Assert.Equal(0, count);
            xmlDocument.DocumentElement.FirstChild.InnerText = "newvalue";
            Assert.Equal(1, count);
        }

        [Fact]
        public static void RemoveEventHandler()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(@"<root> <elem1>text1</elem1> <elem2>text2</elem2> </root>");

            XmlNodeChangedEventHandler handler = (s, e) => Assert.True(false, "Handler should have been removed");
            xmlDocument.NodeChanging += handler;
            xmlDocument.NodeChanging -= handler;

            xmlDocument.DocumentElement.FirstChild.InnerText = "newvalue";
        }
    }
}