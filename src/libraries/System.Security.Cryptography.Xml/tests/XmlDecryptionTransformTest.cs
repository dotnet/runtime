//
// Unit tests for XmlDecryptionTransform
//
// Author:
//	Sebastien Pouliot  <sebastien@ximian.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System;
using System.Security.Cryptography.Xml;
using System.Xml;
using Xunit;

namespace MonoTests.System.Security.Cryptography.Xml {

	public class UnprotectedXmlDecryptionTransform : XmlDecryptionTransform {

		public bool UnprotectedIsTargetElement (XmlElement inputElement, string idValue)
		{
			return base.IsTargetElement (inputElement, idValue);
		}
	}

	[TestFixture]
	public class XmlDecryptionTransformTest {

		private UnprotectedXmlDecryptionTransform transform;

		[TestFixtureSetUp]
		public void FixtureSetUp ()
		{
			transform = new UnprotectedXmlDecryptionTransform ();
		}

		[Test]
		public void IsTargetElement_XmlElementNull ()
		{
			Assert.IsFalse (transform.UnprotectedIsTargetElement (null, "value"));
		}

		[Test]
		public void IsTargetElement_StringNull ()
		{
			XmlDocument doc = new XmlDocument ();
			Assert.IsFalse (transform.UnprotectedIsTargetElement (doc.DocumentElement, null));
		}
	}
}

