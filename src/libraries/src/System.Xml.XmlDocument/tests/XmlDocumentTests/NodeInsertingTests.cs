// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Xml;

namespace System.Xml.Tests
{
    public class NodeInsertingTests
    {
        [Fact]
        public static void CreateElementAndModifyInnerText()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(@"<root> <elem1>text1</elem1> <elem2>text2</elem2> </root>");

            int count = 0;
            xmlDocument.NodeInserting += (s, e) => count++;
            xmlDocument.NodeInserting += (s, e) => Assert.Equal(XmlNodeChangedAction.Insert, e.Action);

            var node = xmlDocument.CreateElement("element");


            Assert.Equal(0, count);

            node.InnerText = "some text";
            Assert.Equal(1, count);
        }

        [Fact]
        public static void InsertNodeAfter()
        {
            var xmlDocument = new XmlDocument();

            xmlDocument.LoadXml(@"<root> <elem1>text1</elem1> <elem2>text2</elem2> </root>");

            int count = 0;
            xmlDocument.NodeInserting += (s, e) => count++;
            xmlDocument.NodeInserting += (s, e) => Assert.Equal(XmlNodeChangedAction.Insert, e.Action);

            var node = xmlDocument.CreateElement("element");
            Assert.Equal(0, count);

            xmlDocument.DocumentElement.InsertAfter(node, xmlDocument.DocumentElement.FirstChild);
            Assert.Equal(1, count);
        }

        [Fact]
        public static void InsertNodeBefore()
        {
            var xmlDocument = new XmlDocument();

            xmlDocument.LoadXml(@"<root> <elem1>text1</elem1> <elem2>text2</elem2> </root>");

            int count = 0;
            xmlDocument.NodeInserting += (s, e) => count++;
            xmlDocument.NodeInserting += (s, e) => Assert.Equal(XmlNodeChangedAction.Insert, e.Action);

            var node = xmlDocument.CreateElement("element");
            Assert.Equal(0, count);

            xmlDocument.DocumentElement.InsertBefore(node, xmlDocument.DocumentElement.FirstChild);
            Assert.Equal(1, count);
        }

        [Fact]
        public static void RemoveEventHandler()
        {
            var xmlDocument = new XmlDocument();
            var node = xmlDocument.CreateElement("element");

            XmlNodeChangedEventHandler handler = (s, e) => { throw new ShouldNotBeInvokedException(); };
            xmlDocument.NodeInserting += handler;
            xmlDocument.NodeInserting -= handler;

            node.InnerText = "some text";
        }
    }
}
