//
// Unit tests for Transform
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

using NUnit.Framework;

using System;
using System.IO;
using System.Security.Cryptography.Xml;
using System.Xml;

namespace MonoTests.System.Security.Cryptography.Xml {

	public class ConcreteTransform : Transform {

		protected override XmlNodeList GetInnerXml ()
		{
			throw new NotImplementedException ();
		}

		public override object GetOutput (Type type)
		{
			return new MemoryStream ();
		}

		public override object GetOutput ()
		{
			throw new NotImplementedException ();
		}

		public override Type [] InputTypes {
			get { throw new NotImplementedException (); }
		}

		public override void LoadInnerXml (global::System.Xml.XmlNodeList nodeList)
		{
			throw new NotImplementedException ();
		}

		public override void LoadInput (object obj)
		{
			throw new NotImplementedException ();
		}

		public override Type [] OutputTypes {
			get { throw new NotImplementedException (); }
		}
	}

	[TestFixture]
	public class TransformTest {

		[Test]
		[ExpectedException (typeof (NullReferenceException))]
		public void GetDigestedOutput_Null ()
		{
			new ConcreteTransform ().GetDigestedOutput (null);
		}
	}
}
