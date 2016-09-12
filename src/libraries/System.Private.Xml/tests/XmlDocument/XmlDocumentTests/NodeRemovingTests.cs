// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Xml;

namespace System.Xml.Tests
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

            XmlNodeChangedEventHandler handler = (s, e) => { throw new ShouldNotBeInvokedException(); };
            xmlDocument.NodeRemoving += handler;
            xmlDocument.NodeRemoving -= handler;

            xmlDocument.DocumentElement.RemoveChild(xmlDocument.DocumentElement.FirstChild);
        }
    }
}
