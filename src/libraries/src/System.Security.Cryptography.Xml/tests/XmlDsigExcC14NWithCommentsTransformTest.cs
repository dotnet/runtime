//
// XmlDsigExcC14NWithCommentsTransformTest.cs - NUnit Test Cases for
// XmlDsigExcC14NWithCommentsTransform
//
// Author:
//  original:
//	Sebastien Pouliot <sebastien@ximian.com>
//	Aleksey Sanin (aleksey@aleksey.com)
//  this file:
//	Gert Driesen <drieseng@users.sourceforge.net>
//
// (C) 2003 Aleksey Sanin (aleksey@aleksey.com)
// (C) 2004 Novell (http://www.novell.com)
// (C) 2008 Gert Driesen
//


using System;
using System.IO;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;

using NUnit.Framework;

namespace MonoTests.System.Security.Cryptography.Xml {
	public class UnprotectedXmlDsigExcC14NWithCommentsTransform : XmlDsigExcC14NWithCommentsTransform {
		public UnprotectedXmlDsigExcC14NWithCommentsTransform ()
		{
		}

		public UnprotectedXmlDsigExcC14NWithCommentsTransform (string inclusiveNamespacesPrefixList)
			: base (inclusiveNamespacesPrefixList)
		{
		}

		public XmlNodeList UnprotectedGetInnerXml ()
		{
			return base.GetInnerXml ();
		}
	}

	[TestFixture]
	public class XmlDsigExcC14NWithCommentsTransformTest {
		private UnprotectedXmlDsigExcC14NWithCommentsTransform transform;

		[SetUp]
		public void SetUp ()
		{
			transform = new UnprotectedXmlDsigExcC14NWithCommentsTransform ();
		}

		[Test] // ctor ()
		public void Constructor1 ()
		{
			CheckProperties (transform);
			Assert.IsNull (transform.InclusiveNamespacesPrefixList);
		}

		[Test] // ctor (Boolean)
		public void Constructor2 ()
		{
			transform = new UnprotectedXmlDsigExcC14NWithCommentsTransform (null);
			CheckProperties (transform);
			Assert.IsNull (transform.InclusiveNamespacesPrefixList);

			transform = new UnprotectedXmlDsigExcC14NWithCommentsTransform (string.Empty);
			CheckProperties (transform);
			Assert.AreEqual (string.Empty, transform.InclusiveNamespacesPrefixList);

			transform = new UnprotectedXmlDsigExcC14NWithCommentsTransform ("#default xsd");
			CheckProperties (transform);
			Assert.AreEqual ("#default xsd", transform.InclusiveNamespacesPrefixList);
		}

		void CheckProperties (XmlDsigExcC14NWithCommentsTransform transform)
		{
			Assert.AreEqual ("http://www.w3.org/2001/10/xml-exc-c14n#WithComments",
				transform.Algorithm, "Algorithm");

			Type[] input = transform.InputTypes;
			Assert.AreEqual (3, input.Length, "Input #");
			// check presence of every supported input types
			bool istream = false;
			bool ixmldoc = false;
			bool ixmlnl = false;
			foreach (Type t in input) {
				if (t == typeof (Stream))
					istream = true;
				if (t == typeof (XmlDocument))
					ixmldoc = true;
				if (t == typeof (XmlNodeList))
					ixmlnl = true;
			}
			Assert.IsTrue (istream, "Input Stream");
			Assert.IsTrue (ixmldoc, "Input XmlDocument");
			Assert.IsTrue (ixmlnl, "Input XmlNodeList");

			Type[] output = transform.OutputTypes;
			Assert.AreEqual (1, output.Length, "Output #");
			Assert.AreEqual (typeof (Stream), output [0], "Output Type");
		}

		[Test]
		public void InputTypes ()
		{
			Type [] input = transform.InputTypes;
			input [0] = null;
			input [1] = null;
			input [2] = null;
			// property does not return a clone
			foreach (Type t in transform.InputTypes)
				Assert.IsNull (t);

			// it's not a static array
			transform = new UnprotectedXmlDsigExcC14NWithCommentsTransform ();
			foreach (Type t in transform.InputTypes)
				Assert.IsNotNull (t);
		}

		[Test]
		public void GetInnerXml ()
		{
			XmlNodeList xnl = transform.UnprotectedGetInnerXml ();
			Assert.IsNull (xnl, "Default InnerXml");
		}

		[Test]
		public void OutputTypes ()
		{
			// property does not return a clone
			transform.OutputTypes [0] = null;
			Assert.IsNull (transform.OutputTypes [0]);

			// it's not a static array
			transform = new UnprotectedXmlDsigExcC14NWithCommentsTransform ();
			Assert.IsNotNull (transform.OutputTypes [0]);
		}
	}
}

