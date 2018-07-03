// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information
//
// MonoTests.System.Security.Cryptography.Xml.AssertCrypto.cs
//
// Author:
//	Sebastien Pouliot  <sebastien@ximian.com>
//
// (C) 2002, 2003 Motus Technologies Inc. (http://www.motus.com)
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
// 
// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{

    public class AssertCrypto
    {

        // because most crypto stuff works with byte[] buffers
        public static void AssertEquals(string msg, byte[] array1, byte[] array2)
        {
            if ((array1 == null) && (array2 == null))
                return;
            Assert.NotNull(array1);
            Assert.NotNull(array2);

            bool a = (array1.Length == array2.Length);
            if (a)
            {
                for (int i = 0; i < array1.Length; i++)
                {
                    if (array1[i] != array2[i])
                    {
                        a = false;
                        break;
                    }
                }
            }
            msg += " -> Expected " + BitConverter.ToString(array1, 0);
            msg += " is different than " + BitConverter.ToString(array2, 0);
            Assert.True(a, msg);
        }

        private const string xmldsig = " xmlns=\"http://www.w3.org/2000/09/xmldsig#\"";

        // not to be used to test C14N output
        public static void AssertXmlEquals(string msg, string expected, string actual)
        {
            expected = expected.Replace(xmldsig, string.Empty);
            actual = actual.Replace(xmldsig, string.Empty);
            Assert.Equal(expected, actual);
        }
    }
}
