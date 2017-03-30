//
// DataReferenceTest.cs
//
// Author:
//	Atsushi Enomoto  <atsushi@ximian.com>
//
// Copyright (C) 2006 Novell, Inc.
//
// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{

    public class DataReferenceTest
    {
        [Fact]
        public void LoadXml()
        {
            string xml = "<e:EncryptedKey xmlns:e='http://www.w3.org/2001/04/xmlenc#'><e:EncryptionMethod Algorithm='http://www.w3.org/2001/04/xmlenc#rsa-oaep-mgf1p'><DigestMethod xmlns='http://www.w3.org/2000/09/xmldsig#' /></e:EncryptionMethod><KeyInfo xmlns='http://www.w3.org/2000/09/xmldsig#'><o:SecurityTokenReference xmlns:o='http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd'><o:Reference URI='#uuid-8a013fe7-86f5-4c11-bf78-61674310679f-1' /></o:SecurityTokenReference></KeyInfo><e:CipherData><e:CipherValue>LSZFpnTv+vyB5iEdIAR2WGSz6MXF9KqONvkKaNhqLuSmhQ6F7xlqLHeoQjS2XoOTXUhkFcKNF/BUzdMSg9pElJX5hlQQqx7OQS9WAH4mSYG0SAn8wt5CStXf5yjQ5quizXJ/2+zgxnuTITwYR/FRi8L+0GLw6BOu8YaLSZyjZg8=</e:CipherValue></e:CipherData><e:ReferenceList><e:DataReference URI='#_1' /><e:DataReference URI='#_6' /></e:ReferenceList></e:EncryptedKey>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            EncryptedKey ek = new EncryptedKey();
            ek.LoadXml(doc.DocumentElement);
        }
    }
}

