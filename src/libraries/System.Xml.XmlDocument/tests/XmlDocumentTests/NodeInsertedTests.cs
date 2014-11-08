using Xunit;
using System;
using System.Xml;

namespace XmlDocumentTests.XmlDocumentTests
{
    public class NodeInsertedTests
    {
        [Fact]
        public static void CreateElementAndModifyInnerText()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(@"<root> <elem1>text1</elem1> <elem2>text2</elem2> </root>");

            int count = 0;
            xmlDocument.NodeInserted += (s, e) => count++;
            xmlDocument.NodeInserted += (s, e) => Assert.Equal(XmlNodeChangedAction.Insert, e.Action);

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
            xmlDocument.NodeInserted += (s, e) => count++;
            xmlDocument.NodeInserted += (s, e) => Assert.Equal(XmlNodeChangedAction.Insert, e.Action);

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
            xmlDocument.NodeInserted += (s, e) => count++;
            xmlDocument.NodeInserted += (s, e) => Assert.Equal(XmlNodeChangedAction.Insert, e.Action);

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

            XmlNodeChangedEventHandler handler = (s, e) => Assert.True(false, "Handler should have been removed");
            xmlDocument.NodeInserted += handler;
            xmlDocument.NodeInserted -= handler;

            node.InnerText = "some text";
        }
    }
}