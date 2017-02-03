//
// XmlLicenseTransformTest.cs - NUnit Test Cases for XmlLicenseTransform
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
	public class UnprotectedXmlLicenseTransform : XmlLicenseTransform {
		public XmlNodeList UnprotectedGetInnerXml ()
		{
			return base.GetInnerXml ();
		}
	}

	[TestFixture]
	public class XmlLicenseTransformTest {
		private UnprotectedXmlLicenseTransform transform;

		[SetUp]
		public void SetUp ()
		{
			transform = new UnprotectedXmlLicenseTransform ();
		}

		[Test] // ctor ()
		public void Constructor1 ()
		{
			Assert.AreEqual ("urn:mpeg:mpeg21:2003:01-REL-R-NS:licenseTransform",
				transform.Algorithm, "Algorithm");
			Assert.IsNull (transform.Decryptor, "Decryptor");

			Type[] input = transform.InputTypes;
			Assert.AreEqual (1, input.Length, "Input #");
			Assert.AreEqual (typeof (XmlDocument), input [0], "Input Type");

			Type[] output = transform.OutputTypes;
			Assert.AreEqual (1, output.Length, "Output #");
			Assert.AreEqual (typeof (XmlDocument), output [0], "Output Type");
		}

		[Test]
		public void InputTypes ()
		{
			// property does not return a clone
			transform.InputTypes [0] = null;
			Assert.IsNull (transform.InputTypes [0]);

			// it's not a static array
			transform = new UnprotectedXmlLicenseTransform ();
			Assert.IsNotNull (transform.InputTypes [0]);
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
			Assert.IsNull (transform.OutputTypes [0], "#1");

			// it's not a static array
			transform = new UnprotectedXmlLicenseTransform ();
			Assert.IsNotNull (transform.OutputTypes [0], "#2");
		}
	}
}

