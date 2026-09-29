// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// DataReferenceTest.cs
//
// Author:
//  Atsushi Enomoto  <atsushi@ximian.com>
//
// Copyright (C) 2006 Novell, Inc.

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

        [Fact]
        public void Constructor_Empty()
        {
            DataReference dataRef = new DataReference();
            Assert.Equal(string.Empty, dataRef.Uri);
        }

        [Fact]
        public void Constructor_WithUri()
        {
            string uri = "#data1";
            DataReference dataRef = new DataReference(uri);
            Assert.Equal(uri, dataRef.Uri);
        }

        [Fact]
        public void Constructor_WithUriAndTransformChain()
        {
            string uri = "#data1";
            TransformChain tc = new TransformChain();
            tc.Add(new XmlDsigBase64Transform());

            DataReference dataRef = new DataReference(uri, tc);
            Assert.Equal(uri, dataRef.Uri);
            Assert.NotNull(dataRef.TransformChain);
            Assert.Equal(1, dataRef.TransformChain.Count);
        }

        [Fact]
        public void GetXml_SimpleDataReference()
        {
            DataReference dataRef = new DataReference("#encrypted-data-1");
            XmlElement xml = dataRef.GetXml();
            Assert.Equal(@"<DataReference URI=""#encrypted-data-1"" xmlns=""http://www.w3.org/2001/04/xmlenc#"" />", xml.OuterXml);
        }

        [Fact]
        public void GetXml_WithTransforms()
        {
            DataReference dataRef = new DataReference("#data1");
            dataRef.TransformChain.Add(new XmlDsigC14NTransform());
            
            XmlElement xml = dataRef.GetXml();
            Assert.Equal("DataReference", xml.LocalName);
            Assert.NotNull(xml.SelectSingleNode("//*[local-name()='Transforms']"));
        }

        [Fact]
        public void LoadXml_SimpleDataReference()
        {
            string xml = @"<DataReference URI=""#encrypted-element"" xmlns=""http://www.w3.org/2001/04/xmlenc#"" />";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            DataReference dataRef = new DataReference();
            dataRef.LoadXml(doc.DocumentElement);
            Assert.Equal("#encrypted-element", dataRef.Uri);
        }

        [Fact]
        public void ReferenceType_IsDataReference()
        {
            DataReference dataRef = new DataReference();
            XmlElement xml = dataRef.GetXml();
            Assert.Equal("DataReference", xml.LocalName);
        }
    }
}
