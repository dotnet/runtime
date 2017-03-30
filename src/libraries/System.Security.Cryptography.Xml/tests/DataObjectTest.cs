//
// DataObjectTest.cs - Test Cases for DataObject
//
// Author:
//	Sebastien Pouliot (spouliot@motus.com)
//	Atsushi Enomoto (atsushi@ximian.com)
//
// (C) 2002, 2003 Motus Technologies Inc. (http://www.motus.com)
// (C) 2004 Novell Inc.
//
// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{

    public class DataObjectTest
    {

        [Fact]
        public void NewDataObject()
        {
            string test = "<Test>DataObject</Test>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(test);

            DataObject obj1 = new DataObject();
            Assert.True((obj1.Data.Count == 0), "Data.Count==0");
            Assert.Equal("<Object xmlns=\"http://www.w3.org/2000/09/xmldsig#\" />", (obj1.GetXml().OuterXml));

            obj1.Id = "id";
            obj1.MimeType = "mime";
            obj1.Encoding = "encoding";
            Assert.Equal("<Object Id=\"id\" MimeType=\"mime\" Encoding=\"encoding\" xmlns=\"http://www.w3.org/2000/09/xmldsig#\" />", (obj1.GetXml().OuterXml));

            obj1.Data = doc.ChildNodes;
            Assert.True((obj1.Data.Count == 1), "Data.Count==1");

            XmlElement xel = obj1.GetXml();

            DataObject obj2 = new DataObject();
            obj2.LoadXml(xel);
            Assert.Equal((obj1.GetXml().OuterXml), (obj2.GetXml().OuterXml));

            DataObject obj3 = new DataObject(obj1.Id, obj1.MimeType, obj1.Encoding, doc.DocumentElement);
            Assert.Equal((obj2.GetXml().OuterXml), (obj3.GetXml().OuterXml));
        }

        [Fact]
        public void ImportDataObject()
        {
            string value1 = "<Object Id=\"id\" MimeType=\"mime\" Encoding=\"encoding\" xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><Test xmlns=\"\">DataObject1</Test><Test xmlns=\"\">DataObject2</Test></Object>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(value1);

            DataObject obj1 = new DataObject();
            obj1.LoadXml(doc.DocumentElement);
            Assert.True((obj1.Data.Count == 2), "Data.Count==2");

            string s = (obj1.GetXml().OuterXml);
            Assert.Equal(value1, s);

            string value2 = "<Object xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><Test xmlns=\"\" /></Object>";
            doc = new XmlDocument();
            doc.LoadXml(value2);

            DataObject obj2 = new DataObject();
            obj2.LoadXml(doc.DocumentElement);

            s = (obj2.GetXml().OuterXml);
            Assert.Equal(value2, s);

            string value3 = "<Object Id=\"id\" xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><Test xmlns=\"\" /></Object>";
            doc = new XmlDocument();
            doc.LoadXml(value3);

            DataObject obj3 = new DataObject();
            obj3.LoadXml(doc.DocumentElement);

            s = (obj3.GetXml().OuterXml);
            Assert.Equal(value3, s);

            string value4 = "<Object MimeType=\"mime\" xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><Test xmlns=\"\" /></Object>";
            doc = new XmlDocument();
            doc.LoadXml(value4);

            DataObject obj4 = new DataObject();
            obj4.LoadXml(doc.DocumentElement);

            s = (obj4.GetXml().OuterXml);
            Assert.Equal(value4, s);
        }

        [Fact]
        public void InvalidDataObject1()
        {
            DataObject obj1 = new DataObject();
            Assert.Throws<ArgumentNullException>(() => obj1.Data = null);
        }

        [Fact]
        public void InvalidDataObject2()
        {
            DataObject obj1 = new DataObject();
            Assert.Throws<ArgumentNullException>(() => obj1.LoadXml(null));
        }

        [Fact]
        public void InvalidDataObject3()
        {
            DataObject obj1 = new DataObject();
            // seems this isn't invalid !?!
            // but no exception is thrown
            string value = "<Test>Bad</Test>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(value);
            obj1.LoadXml(doc.DocumentElement);
            string s = (obj1.GetXml().OuterXml);
            Assert.Equal(value, s);
        }

        [Fact]
        public void GetXmlKeepDocument()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<Object xmlns='http://www.w3.org/2000/09/xmldsig#'>test</Object>");
            DataObject obj = new DataObject();
            XmlElement el1 = obj.GetXml();
            obj.LoadXml(doc.DocumentElement);
            //			obj.Id = "hogehoge";
            XmlElement el2 = obj.GetXml();
            Assert.Equal(doc, el2.OwnerDocument);
        }

        [Fact]
        public void PropertySetMakesDocumentDifferent()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<Object xmlns='http://www.w3.org/2000/09/xmldsig#'>test</Object>");
            DataObject obj = new DataObject();
            XmlElement el1 = obj.GetXml();
            obj.LoadXml(doc.DocumentElement);
            obj.Id = "hogehoge";
            XmlElement el2 = obj.GetXml();
            Assert.True(doc != el2.OwnerDocument, "Document is not kept when properties are set");
        }

        [Fact]
        public void EnvelopedObject()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<envelope><Object xmlns:dsig='http://www.w3.org/2000/09/xmldsig#' xmlns='http://www.w3.org/2000/09/xmldsig#'>test</Object></envelope>");
            DataObject obj = new DataObject();
            obj.LoadXml(doc.DocumentElement.FirstChild as XmlElement);
            obj.Id = "hoge";
            obj.MimeType = "application/octet-stream";
            obj.Encoding = "euc-kr";
            XmlElement el1 = obj.GetXml();
            Assert.Equal("<Object Id=\"hoge\" MimeType=\"application/octet-stream\" Encoding=\"euc-kr\" xmlns=\"http://www.w3.org/2000/09/xmldsig#\">test</Object>", el1.OuterXml);
            /* looks curious? but the element does not look to 
			   be appended to the document.
			   Just commented out since it is not fixed.
			Assert.AreEqual (String.Empty, el1.OwnerDocument.OuterXml);
			*/
        }

        [Fact]
        public void SetDataAfterId()
        {
            DataObject d = new DataObject();
            XmlElement el = new XmlDocument().CreateElement("foo");
            d.Id = "id:1";
            d.Data = el.SelectNodes(".");
            Assert.Equal("id:1", d.Id);
        }

        [Fact]
        public void SetMimeTypeAfterId()
        {
            XmlElement el = new XmlDocument().CreateElement("foo");
            DataObject d = new DataObject("id:1", null, null, el);
            d.MimeType = "text/html";
            Assert.Equal("id:1", d.Id);
        }
    }
}
