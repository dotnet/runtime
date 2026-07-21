// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// TransformChainTest.cs - Test Cases for TransformChain
//
// Author:
//  Sebastien Pouliot (spouliot@motus.com)
//
// (C) 2002, 2003 Motus Technologies Inc. (http://www.motus.com)

using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{

    public class TransformChainTest
    {

        [Fact]
        public void EmptyChain()
        {
            TransformChain chain = new TransformChain();
            Assert.Equal(0, chain.Count);
            Assert.NotNull(chain.GetEnumerator());
            Assert.Equal("System.Security.Cryptography.Xml.TransformChain", chain.ToString());
        }

        [Fact]
        public void FullChain()
        {
            TransformChain chain = new TransformChain();

            XmlDsigBase64Transform base64 = new XmlDsigBase64Transform();
            chain.Add(base64);
            Assert.Equal(base64, chain[0]);
            Assert.Equal(1, chain.Count);

            XmlDsigC14NTransform c14n = new XmlDsigC14NTransform();
            chain.Add(c14n);
            Assert.Equal(c14n, chain[1]);
            Assert.Equal(2, chain.Count);

            XmlDsigC14NWithCommentsTransform c14nc = new XmlDsigC14NWithCommentsTransform();
            chain.Add(c14nc);
            Assert.Equal(c14nc, chain[2]);
            Assert.Equal(3, chain.Count);

            XmlDsigEnvelopedSignatureTransform esign = new XmlDsigEnvelopedSignatureTransform();
            chain.Add(esign);
            Assert.Equal(esign, chain[3]);
            Assert.Equal(4, chain.Count);

            XmlDsigXPathTransform xpath = new XmlDsigXPathTransform();
            chain.Add(xpath);
            Assert.Equal(xpath, chain[4]);
            Assert.Equal(5, chain.Count);

            XmlDsigXsltTransform xslt = new XmlDsigXsltTransform();
            chain.Add(xslt);
            Assert.Equal(xslt, chain[5]);
            Assert.Equal(6, chain.Count);
        }

        [Fact]
        public void SameTransformInstanceCanBeAppliedTwice()
        {
            const string Expected = "transform reuse works";

            TransformChain chain = new TransformChain();
            XmlDsigBase64Transform transform = new XmlDsigBase64Transform();
            chain.Add(transform);
            chain.Add(transform);

            string doublyEncoded = Convert.ToBase64String(
                Encoding.UTF8.GetBytes(
                    Convert.ToBase64String(Encoding.UTF8.GetBytes(Expected))));

            MethodInfo transformToOctetStream = typeof(TransformChain).GetMethod(
                "TransformToOctetStream",
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(Stream), typeof(XmlResolver), typeof(string) },
                modifiers: null);

            if (transformToOctetStream == null)
            {
                transformToOctetStream = typeof(TransformChain).GetMethod(
                    "TransformToOctetStream",
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    binder: null,
                    types: new[] { typeof(object), typeof(XmlResolver), typeof(string) },
                    modifiers: null);
            }

            Assert.NotNull(transformToOctetStream);

            using MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes(doublyEncoded));
            using Stream output = (Stream)transformToOctetStream.Invoke(chain, new object[] { input, null, null });
            using StreamReader reader = new StreamReader(output, Encoding.UTF8);

            Assert.Equal(Expected, reader.ReadToEnd());
        }
    }
}
