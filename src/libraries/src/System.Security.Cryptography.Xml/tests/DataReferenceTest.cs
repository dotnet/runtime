//
// DataReferenceTest.cs
//
// Author:
//	Atsushi Enomoto  <atsushi@ximian.com>
//
// Copyright (C) 2006 Novell, Inc.
//

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
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Xml;

using NUnit.Framework;

namespace MonoTests.System.Security.Cryptography.Xml {

	[TestFixture]
	public class DataReferenceTest
	{
		[Test]
		public void LoadXml ()
		{
			string xml = "<e:EncryptedKey xmlns:e='http://www.w3.org/2001/04/xmlenc#'><e:EncryptionMethod Algorithm='http://www.w3.org/2001/04/xmlenc#rsa-oaep-mgf1p'><DigestMethod xmlns='http://www.w3.org/2000/09/xmldsig#' /></e:EncryptionMethod><KeyInfo xmlns='http://www.w3.org/2000/09/xmldsig#'><o:SecurityTokenReference xmlns:o='http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd'><o:Reference URI='#uuid-8a013fe7-86f5-4c11-bf78-61674310679f-1' /></o:SecurityTokenReference></KeyInfo><e:CipherData><e:CipherValue>LSZFpnTv+vyB5iEdIAR2WGSz6MXF9KqONvkKaNhqLuSmhQ6F7xlqLHeoQjS2XoOTXUhkFcKNF/BUzdMSg9pElJX5hlQQqx7OQS9WAH4mSYG0SAn8wt5CStXf5yjQ5quizXJ/2+zgxnuTITwYR/FRi8L+0GLw6BOu8YaLSZyjZg8=</e:CipherValue></e:CipherData><e:ReferenceList><e:DataReference URI='#_1' /><e:DataReference URI='#_6' /></e:ReferenceList></e:EncryptedKey>";
			XmlDocument doc = new XmlDocument ();
			doc.LoadXml (xml);
			EncryptedKey ek = new EncryptedKey ();
			ek.LoadXml (doc.DocumentElement);
		}
	}
}

