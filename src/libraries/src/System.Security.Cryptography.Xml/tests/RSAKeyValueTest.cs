//
// RSAKeyValueTest.cs - NUnit Test Cases for RSAKeyValue
//
// Author:
//	Sebastien Pouliot (spouliot@motus.com)
//
// (C) 2002, 2003 Motus Technologies Inc. (http://www.motus.com)
//

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Xml;

using NUnit.Framework;

namespace MonoTests.System.Security.Cryptography.Xml {

	[TestFixture]
	public class RSAKeyValueTest {

		[Test]
		public void GeneratedKey () 
		{
			RSAKeyValue rsa1 = new RSAKeyValue ();
			Assert.IsNotNull (rsa1.Key, "Key");
			XmlElement xmlkey = rsa1.GetXml ();

			RSAKeyValue rsa2 = new RSAKeyValue ();
			rsa2.LoadXml (xmlkey);

			Assert.IsTrue ((rsa1.GetXml ().OuterXml) == (rsa2.GetXml ().OuterXml), "rsa1==rsa2");

			RSA key = rsa1.Key;
			RSAKeyValue rsa3 = new RSAKeyValue (key);
			Assert.IsTrue ((rsa3.GetXml ().OuterXml) == (rsa1.GetXml ().OuterXml), "rsa3==rsa1");
			Assert.IsTrue ((rsa3.GetXml ().OuterXml) == (rsa2.GetXml ().OuterXml), "rsa3==rsa2");
		}

		[Test]
		public void ImportKey () 
		{
			string rsaKey = "<KeyValue xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><RSAKeyValue><Modulus>ogZ1/O7iks9ncETqNxLDKoPvgrT4nFx1a3lOmpywEmgbc5+8vI5dSzReH4v0YrflY75rIJx13CYWMsaHfQ78GtXvaeshHlQ3lLTuSdYEJceKll/URlBoKQtOj5qYIVSFOIVGHv4Y/0lnLftOzIydem29KKH6lJQlJawBBssR12s=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue>";
			XmlDocument doc = new XmlDocument ();
			doc.LoadXml (rsaKey);

			RSAKeyValue rsa1 = new RSAKeyValue ();
			rsa1.LoadXml (doc.DocumentElement);

			string s = (rsa1.GetXml ().OuterXml);
			Assert.AreEqual (rsaKey, s, "RSA Key");
		}

		[Test]
		[ExpectedException (typeof (ArgumentNullException))]
		public void InvalidValue1 () 
		{
			RSAKeyValue rsa = new RSAKeyValue ();
			rsa.LoadXml (null);
		}

		[Test]
		[ExpectedException (typeof (CryptographicException))]
		public void InvalidValue2 () 
		{
			string badKey = "<Test></Test>";
			XmlDocument doc = new XmlDocument ();
			doc.LoadXml (badKey);

			RSAKeyValue rsa = new RSAKeyValue ();
			rsa.LoadXml (doc.DocumentElement);
		}
	}
}
