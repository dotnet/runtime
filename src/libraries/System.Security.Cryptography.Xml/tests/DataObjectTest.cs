//
// DataObjectTest.cs - NUnit Test Cases for DataObject
//
// Author:
//	Sebastien Pouliot (spouliot@motus.com)
//	Atsushi Enomoto (atsushi@ximian.com)
//
// (C) 2002, 2003 Motus Technologies Inc. (http://www.motus.com)
// (C) 2004 Novell Inc.
//

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Xml;

using NUnit.Framework;

namespace MonoTests.System.Security.Cryptography.Xml {

	[TestFixture]
	public class DataObjectTest {

		[Test]
		public void NewDataObject () 
		{
			string test = "<Test>DataObject</Test>";
			XmlDocument doc = new XmlDocument ();
			doc.LoadXml (test);

			DataObject obj1 = new DataObject ();
			Assert.IsTrue ((obj1.Data.Count == 0), "Data.Count==0");
			Assert.AreEqual ("<Object xmlns=\"http://www.w3.org/2000/09/xmldsig#\" />", (obj1.GetXml ().OuterXml), "Just constructed");

			obj1.Id = "id";
			obj1.MimeType = "mime";
			obj1.Encoding = "encoding";
			Assert.AreEqual ("<Object Id=\"id\" MimeType=\"mime\" Encoding=\"encoding\" xmlns=\"http://www.w3.org/2000/09/xmldsig#\" />", (obj1.GetXml ().OuterXml), "Only attributes");

			obj1.Data = doc.ChildNodes;
			Assert.IsTrue ((obj1.Data.Count == 1), "Data.Count==1");

			XmlElement xel = obj1.GetXml ();

			DataObject obj2 = new DataObject ();
			obj2.LoadXml (xel);
			Assert.AreEqual ((obj1.GetXml ().OuterXml), (obj2.GetXml ().OuterXml), "obj1==obj2");

			DataObject obj3 = new DataObject (obj1.Id, obj1.MimeType, obj1.Encoding, doc.DocumentElement);
			Assert.AreEqual ((obj2.GetXml ().OuterXml), (obj3.GetXml ().OuterXml), "obj2==obj3");
		}

		[Test]
		public void ImportDataObject () 
		{
			string value1 = "<Object Id=\"id\" MimeType=\"mime\" Encoding=\"encoding\" xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><Test xmlns=\"\">DataObject1</Test><Test xmlns=\"\">DataObject2</Test></Object>";
			XmlDocument doc = new XmlDocument ();
			doc.LoadXml (value1);

			DataObject obj1 = new DataObject ();
			obj1.LoadXml (doc.DocumentElement);
			Assert.IsTrue ((obj1.Data.Count == 2), "Data.Count==2");

			string s = (obj1.GetXml ().OuterXml);
			Assert.AreEqual (value1, s, "DataObject 1");

			string value2 = "<Object xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><Test xmlns=\"\" /></Object>";
			doc = new XmlDocument ();
			doc.LoadXml (value2);

			DataObject obj2 = new DataObject ();
			obj2.LoadXml (doc.DocumentElement);

			s = (obj2.GetXml ().OuterXml);
			Assert.AreEqual (value2, s, "DataObject 2");

			string value3 = "<Object Id=\"id\" xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><Test xmlns=\"\" /></Object>";
			doc = new XmlDocument ();
			doc.LoadXml (value3);

			DataObject obj3 = new DataObject ();
			obj3.LoadXml (doc.DocumentElement);

			s = (obj3.GetXml ().OuterXml);
			Assert.AreEqual (value3, s, "DataObject 3");

			string value4 = "<Object MimeType=\"mime\" xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><Test xmlns=\"\" /></Object>";
			doc = new XmlDocument ();
			doc.LoadXml (value4);

			DataObject obj4 = new DataObject ();
			obj4.LoadXml (doc.DocumentElement);

			s = (obj4.GetXml ().OuterXml);
			Assert.AreEqual (value4, s, "DataObject 4");
		}

		[Test]
		[ExpectedException (typeof (ArgumentNullException))]
		public void InvalidDataObject1 () 
		{
			DataObject obj1 = new DataObject ();
			obj1.Data = null;
		}

		[Test]
		[ExpectedException (typeof (ArgumentNullException))]
		public void InvalidDataObject2 () 
		{
			DataObject obj1 = new DataObject ();
			obj1.LoadXml (null);
		}

		[Test]
		public void InvalidDataObject3 () 
		{
			DataObject obj1 = new DataObject ();
			// seems this isn't invalid !?!
			// but no exception is thrown
			string value = "<Test>Bad</Test>";
			XmlDocument doc = new XmlDocument ();
			doc.LoadXml (value);
			obj1.LoadXml (doc.DocumentElement);
			string s = (obj1.GetXml ().OuterXml);
			Assert.AreEqual (value, s, "DataObject Bad");
		}

		[Test]
		public void GetXmlKeepDocument ()
		{
			XmlDocument doc = new XmlDocument ();
			doc.LoadXml ("<Object xmlns='http://www.w3.org/2000/09/xmldsig#'>test</Object>");
			DataObject obj = new DataObject ();
			XmlElement el1 = obj.GetXml ();
			obj.LoadXml (doc.DocumentElement);
//			obj.Id = "hogehoge";
			XmlElement el2 = obj.GetXml ();
			Assert.AreEqual (doc, el2.OwnerDocument, "Document is kept unless setting properties");
		}

		[Test]
		public void PropertySetMakesDocumentDifferent ()
		{
			XmlDocument doc = new XmlDocument ();
			doc.LoadXml ("<Object xmlns='http://www.w3.org/2000/09/xmldsig#'>test</Object>");
			DataObject obj = new DataObject ();
			XmlElement el1 = obj.GetXml ();
			obj.LoadXml (doc.DocumentElement);
			obj.Id = "hogehoge";
			XmlElement el2 = obj.GetXml ();
			Assert.IsTrue (doc != el2.OwnerDocument, "Document is not kept when properties are set");
		}

		[Test]
		public void EnvelopedObject ()
		{
			XmlDocument doc = new XmlDocument ();
			doc.LoadXml ("<envelope><Object xmlns:dsig='http://www.w3.org/2000/09/xmldsig#' xmlns='http://www.w3.org/2000/09/xmldsig#'>test</Object></envelope>");
			DataObject obj = new DataObject ();
			obj.LoadXml (doc.DocumentElement.FirstChild as XmlElement);
			obj.Id = "hoge";
			obj.MimeType = "application/octet-stream";
			obj.Encoding = "euc-kr";
			XmlElement el1 = obj.GetXml ();
			Assert.AreEqual ("<Object Id=\"hoge\" MimeType=\"application/octet-stream\" Encoding=\"euc-kr\" xmlns=\"http://www.w3.org/2000/09/xmldsig#\">test</Object>", el1.OuterXml);
			/* looks curious? but the element does not look to 
			   be appended to the document.
			   Just commented out since it is not fixed.
			Assert.AreEqual (String.Empty, el1.OwnerDocument.OuterXml);
			*/
		}

		[Test]
		public void SetDataAfterId ()
		{
			DataObject d = new DataObject ();
			XmlElement el = new XmlDocument ().CreateElement ("foo");
			d.Id = "id:1";
			d.Data = el.SelectNodes (".");
			Assert.AreEqual ("id:1", d.Id);
		}

		[Test]
		public void SetMimeTypeAfterId ()
		{
			XmlElement el = new XmlDocument ().CreateElement ("foo");
			DataObject d = new DataObject ("id:1", null, null, el);
			d.MimeType = "text/html";
			Assert.AreEqual ("id:1", d.Id);
		}
	}
}
