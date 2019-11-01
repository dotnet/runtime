// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    }
}
