using Xunit;
using System.Xml;

namespace XmlDocumentTests.XmlElementTests
{
    public class HasAttributeTests
    {

        [Fact]
        public static void ExistingAttribute()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<elem1 attr1=\"foobar\">This is a test</elem1>");

            Assert.True(xmlDocument.DocumentElement.HasAttribute("attr1"));
        }

        [Fact]
        public static void NonExistingAttribute()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<elem1/>");

            Assert.False(xmlDocument.DocumentElement.HasAttribute("attr1"));
        }

        [Fact]
        public static void ExistingNamespace()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<elem1 xmlns=\"ns1\" xmlns:bb=\"ns2\" xmlns:cc=\"ns3\" bb:att1=\"foo\" attr=\"some\" cc:att2=\"bar\"></elem1>");

            Assert.True(xmlDocument.DocumentElement.HasAttribute("att1", "ns2"));
        }

        [Fact]
        public static void NonExistingNamespace()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<elem1 xmlns=\"ns1\" xmlns:bb=\"ns2\" xmlns:cc=\"ns3\" bb:att1=\"foo\" attr=\"some\" cc:att2=\"bar\"></elem1>");

            Assert.False(xmlDocument.DocumentElement.HasAttribute("att1", "nscc"));
        }

        [Fact]
        public static void InvalidExistingNamespace()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<elem1 xmlns=\"ns1\" xmlns:bb=\"ns2\" xmlns:cc=\"ns3\" bb:att1=\"foo\" attr=\"some\" cc:att2=\"bar\"></elem1>");

            Assert.False(xmlDocument.DocumentElement.HasAttribute("attr", "ns2"));
        }

        [Fact]
        public static void WrongNamespace()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<elem1 xmlns=\"ns1\" xmlns:bb=\"ns2\" xmlns:cc=\"ns3\" bb:att1=\"foo\" attr=\"some\" cc:att2=\"bar\"></elem1>");

            Assert.False(xmlDocument.DocumentElement.HasAttribute("att1", "nsa"));
        }
    }
}
