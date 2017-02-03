//
// MonoTests.System.Security.Cryptography.Xml.AssertCrypto.cs
//
// Author:
//	Sebastien Pouliot  <sebastien@ximian.com>
//
// (C) 2002, 2003 Motus Technologies Inc. (http://www.motus.com)
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//

using System;
using System.Security.Cryptography;

using NUnit.Framework;

namespace MonoTests.System.Security.Cryptography.Xml {

	public class AssertCrypto {

		// because most crypto stuff works with byte[] buffers
		static public void AssertEquals (string msg, byte[] array1, byte[] array2) 
		{
			if ((array1 == null) && (array2 == null))
				return;
			if (array1 == null)
				Assert.Fail (msg + " -> First array is NULL");
			if (array2 == null)
				Assert.Fail (msg + " -> Second array is NULL");

			bool a = (array1.Length == array2.Length);
			if (a) {
				for (int i = 0; i < array1.Length; i++) {
					if (array1 [i] != array2 [i]) {
						a = false;
						break;
					}
				}
			}
			msg += " -> Expected " + BitConverter.ToString (array1, 0);
			msg += " is different than " + BitConverter.ToString (array2, 0);
			Assert.IsTrue (a, msg);
		}

		private const string xmldsig = " xmlns=\"http://www.w3.org/2000/09/xmldsig#\"";

		// not to be used to test C14N output
		static public void AssertXmlEquals (string msg, string expected, string actual)
		{
			expected = expected.Replace (xmldsig, String.Empty);
			actual = actual.Replace (xmldsig, String.Empty);
			Assert.AreEqual (expected, actual, msg);
		}
	}
}
