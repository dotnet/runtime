// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{
    public class DataObjectTests
    {
        private const string IdAttributeName = "Id";
        private const string MimeTypeAttributeName = "MimeType";
        private const string EncodingAttributeName = "Encoding";

        [Fact]
        public void Constructor_Empty()
        {
            var dataObject = new DataObject();

            Assert.NotNull(dataObject.Data);
            Assert.Empty(dataObject.Data);
            Assert.Null(dataObject.Id);
            Assert.Null(dataObject.MimeType);
            Assert.Null(dataObject.Encoding);
        }

        [Fact]
        public void Constructor_Data_Null()
        {
            Assert.Throws<ArgumentNullException>(() => new DataObject(string.Empty, string.Empty, string.Empty, null));
        }

        [Fact]
        public void Constructor_SetsValues()
        {
            const string idValue = "testId";
            const string mimeTypeValue = "testMimeType";
            const string encodingValue = "testEncoding";

            var doc = new XmlDocument();

            var dataObject = new DataObject(idValue, mimeTypeValue, encodingValue, doc.CreateElement("Object"));

            Assert.Equal(idValue, dataObject.Id);
            Assert.Equal(mimeTypeValue, dataObject.MimeType);
            Assert.Equal(encodingValue, dataObject.Encoding);
        }

        [Fact]
        public void Id_GetSet()
        {
            const string idValue = "testId";

            var dataObject = new DataObject
            {
                Id = idValue
            };

            Assert.Equal(idValue, dataObject.Id);
        }

        [Fact]
        public void MimeType_GetSet()
        {
            const string mimeTypeValue = "testMimeType";

            var dataObject = new DataObject
            {
                MimeType = mimeTypeValue
            };

            Assert.Equal(mimeTypeValue, dataObject.MimeType);
        }

        [Fact]
        public void Encoding_GetSet()
        {
            const string encodingValue = "testEncoding";

            var dataObject = new DataObject
            {
                Encoding = encodingValue
            };

            Assert.Equal(encodingValue, dataObject.Encoding);
        }

        [Fact]
        public void Data_Set_Null()
        {
            var dataObject = new DataObject();
            Assert.Throws<ArgumentNullException>(() => dataObject.Data = null);
        }

        [Fact]
        public void GetXml_CorrectXml()
        {
            const string idValue = "testId";
            const string mimeTypeValue = "testMimeType";
            const string encodingValue = "testEncoding";

            var dataObject = new DataObject
            {
                Id = idValue,
                MimeType = mimeTypeValue,
                Encoding = encodingValue
            };

            XmlElement testElement = CreateTestElement("Object", idValue, mimeTypeValue, encodingValue, 0);
            XmlElement dataObjectXml = dataObject.GetXml();

            Assert.Equal(testElement.GetAttribute(IdAttributeName), dataObjectXml.GetAttribute(IdAttributeName));
            Assert.Equal(testElement.GetAttribute(MimeTypeAttributeName), dataObjectXml.GetAttribute(MimeTypeAttributeName));
            Assert.Equal(testElement.GetAttribute(EncodingAttributeName), dataObjectXml.GetAttribute(EncodingAttributeName));
            Assert.Equal(testElement.ChildNodes.Count, dataObjectXml.ChildNodes.Count);
        }

        [Fact]
        public void LoadXml_Null()
        {
            Assert.Throws<ArgumentNullException>(() => new DataObject().LoadXml(null));
        }

        [Fact]
        public void LoadXml_SetsInstanceValues()
        {
            const string idValue = "testId";
            const string mimeTypeValue = "testMimeType";
            const string encodingValue = "testEncoding";
            const int childCount = 0;

            XmlElement element = CreateTestElement("Object", idValue, mimeTypeValue, encodingValue, childCount);

            var dataObject = new DataObject();
            dataObject.LoadXml(element);

            Assert.Equal(idValue, dataObject.Id);
            Assert.Equal(mimeTypeValue, dataObject.MimeType);
            Assert.Equal(encodingValue, dataObject.Encoding);
            Assert.Equal(childCount, dataObject.Data.Count);
        }

        private static XmlElement CreateTestElement(string name, string idValue, string mimeTypeValue, string encodingValue, int childs)
        {
            var doc = new XmlDocument();
            XmlElement element = doc.CreateElement(name, SignedXml.XmlDsigNamespaceUrl);
            XmlAttribute idAttribute = doc.CreateAttribute(IdAttributeName, SignedXml.XmlDsigNamespaceUrl);
            XmlAttribute mimeTypeAttribute = doc.CreateAttribute(MimeTypeAttributeName, SignedXml.XmlDsigNamespaceUrl);
            XmlAttribute encodingAttribute = doc.CreateAttribute(EncodingAttributeName, SignedXml.XmlDsigNamespaceUrl);
            idAttribute.Value = idValue;
            mimeTypeAttribute.Value = mimeTypeValue;
            encodingAttribute.Value = encodingValue;
            element.Attributes.Append(idAttribute);
            element.Attributes.Append(mimeTypeAttribute);
            element.Attributes.Append(encodingAttribute);

            for (var i = 0; i < childs; i++)
            {
                element.AppendChild(doc.CreateElement("childElement"));
            }

            return element;
        }

        [Fact]
        public void LoadXml_InvalidElement()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<WrongElement />");

            DataObject dataObject = new DataObject();
            dataObject.Id = "test-id";
            dataObject.Encoding = "UTF-8";
            // LoadXml replaces the properties, so pre-set values are overwritten
            dataObject.LoadXml(doc.DocumentElement);
            Assert.NotNull(dataObject.Data);
            // LoadXml reads from the XML, not the pre-set values
            Assert.Null(dataObject.Id);
            Assert.Null(dataObject.Encoding);
        }

        [Fact]
        public void GetXml_WithData()
        {
            DataObject dataObject = new DataObject();
            XmlDocument doc = new XmlDocument();
            XmlElement elem = doc.CreateElement("TestData");
            elem.InnerText = "test content";
            dataObject.Data = new XmlNodeList[] { elem.ChildNodes }[0];

            XmlElement xml = dataObject.GetXml();
            Assert.NotNull(xml);
            Assert.Equal("Object", xml.LocalName);
            // The InnerText "test content" is a text node, verify it appears in the output
            Assert.Contains("test content", xml.InnerXml);
        }

        [Fact]
        public void Properties_SetAndGet()
        {
            DataObject dataObject = new DataObject();
            
            dataObject.Id = "obj-1";
            Assert.Equal("obj-1", dataObject.Id);

            dataObject.MimeType = "text/xml";
            Assert.Equal("text/xml", dataObject.MimeType);

            dataObject.Encoding = "UTF-8";
            Assert.Equal("UTF-8", dataObject.Encoding);

            Assert.NotNull(dataObject.Data);
        }

        [Fact]
        public void LoadXml_WithData()
        {
            string xml = @"<Object Id=""obj1"" xmlns=""http://www.w3.org/2000/09/xmldsig#"">
                <test>data</test>
            </Object>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            DataObject dataObject = new DataObject();
            dataObject.LoadXml(doc.DocumentElement);
            Assert.Equal("obj1", dataObject.Id);
            Assert.Equal(1, dataObject.Data.Count);
        }

        [Fact]
        public void LoadXml_NoId()
        {
            string xml = @"<Object xmlns=""http://www.w3.org/2000/09/xmldsig#"">
                <test>data</test>
            </Object>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            DataObject dataObject = new DataObject();
            dataObject.LoadXml(doc.DocumentElement);
            Assert.Null(dataObject.Id);
        }

        [Fact]
        public void LoadXml_NoMimeType()
        {
            string xml = @"<Object Id=""obj1"" xmlns=""http://www.w3.org/2000/09/xmldsig#"">
                <test>data</test>
            </Object>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            DataObject dataObject = new DataObject();
            dataObject.LoadXml(doc.DocumentElement);
            Assert.Null(dataObject.MimeType);
        }

        [Fact]
        public void LoadXml_NoEncoding()
        {
            string xml = @"<Object Id=""obj1"" xmlns=""http://www.w3.org/2000/09/xmldsig#"">
                <test>data</test>
            </Object>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            DataObject dataObject = new DataObject();
            dataObject.LoadXml(doc.DocumentElement);
            Assert.Null(dataObject.Encoding);
        }

        [Fact]
        public void GetXml_NoData()
        {
            DataObject dataObject = new DataObject();
            dataObject.Id = "obj1";
            
            XmlElement xml = dataObject.GetXml();
            Assert.NotNull(xml);
            Assert.Equal(@"<Object Id=""obj1"" xmlns=""http://www.w3.org/2000/09/xmldsig#"" />", xml.OuterXml);
        }

        [Fact]
        public void GetXml_EmptyStrings()
        {
            DataObject dataObject = new DataObject();
            dataObject.Id = "";
            dataObject.MimeType = "";
            dataObject.Encoding = "";
            
            XmlElement xml = dataObject.GetXml();
            // Verify the full output XML - empty strings for MimeType and Encoding mean no attributes are added
            Assert.Equal(@"<Object xmlns=""http://www.w3.org/2000/09/xmldsig#"" />", xml.OuterXml);
        }

        [Fact]
        public void Constructor_NullArguments()
        {
            XmlDocument doc = new XmlDocument();
            XmlElement elem = doc.CreateElement("test");
            
            DataObject dataObject = new DataObject(null, null, null, elem);
            Assert.Null(dataObject.Id);
            Assert.Null(dataObject.MimeType);
            Assert.Null(dataObject.Encoding);
            Assert.NotNull(dataObject.Data);
            
            // Verify GetXml output - should include the test element content
            XmlElement xml = dataObject.GetXml();
            Assert.NotNull(xml);
            Assert.Equal("Object", xml.LocalName);
            Assert.Equal(SignedXml.XmlDsigNamespaceUrl, xml.NamespaceURI);
            // The element should contain the child element
            Assert.Contains("<test", xml.InnerXml);
        }
    }
}
