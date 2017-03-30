//
// XmlDsigExcC14NWithCommentsTransformTest.cs - Test Cases for
// XmlDsigExcC14NWithCommentsTransform
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
// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{
    public class UnprotectedXmlDsigExcC14NWithCommentsTransform : XmlDsigExcC14NWithCommentsTransform
    {
        public UnprotectedXmlDsigExcC14NWithCommentsTransform()
        {
        }

        public UnprotectedXmlDsigExcC14NWithCommentsTransform(string inclusiveNamespacesPrefixList)
            : base(inclusiveNamespacesPrefixList)
        {
        }

        public XmlNodeList UnprotectedGetInnerXml()
        {
            return base.GetInnerXml();
        }
    }

    public class XmlDsigExcC14NWithCommentsTransformTest
    {
        private UnprotectedXmlDsigExcC14NWithCommentsTransform transform;

        public XmlDsigExcC14NWithCommentsTransformTest()
        {
            transform = new UnprotectedXmlDsigExcC14NWithCommentsTransform();
        }

        [Fact] // ctor ()
        public void Constructor1()
        {
            CheckProperties(transform);
            Assert.Null(transform.InclusiveNamespacesPrefixList);
        }

        [Fact] // ctor (Boolean)
        public void Constructor2()
        {
            transform = new UnprotectedXmlDsigExcC14NWithCommentsTransform(null);
            CheckProperties(transform);
            Assert.Null(transform.InclusiveNamespacesPrefixList);

            transform = new UnprotectedXmlDsigExcC14NWithCommentsTransform(string.Empty);
            CheckProperties(transform);
            Assert.Equal(string.Empty, transform.InclusiveNamespacesPrefixList);

            transform = new UnprotectedXmlDsigExcC14NWithCommentsTransform("#default xsd");
            CheckProperties(transform);
            Assert.Equal("#default xsd", transform.InclusiveNamespacesPrefixList);
        }

        void CheckProperties(XmlDsigExcC14NWithCommentsTransform transform)
        {
            Assert.Equal("http://www.w3.org/2001/10/xml-exc-c14n#WithComments", transform.Algorithm);

            Type[] input = transform.InputTypes;
            Assert.Equal(3, input.Length);
            // check presence of every supported input types
            bool istream = false;
            bool ixmldoc = false;
            bool ixmlnl = false;
            foreach (Type t in input)
            {
                if (t == typeof(Stream))
                    istream = true;
                if (t == typeof(XmlDocument))
                    ixmldoc = true;
                if (t == typeof(XmlNodeList))
                    ixmlnl = true;
            }
            Assert.True(istream, "Input Stream");
            Assert.True(ixmldoc, "Input XmlDocument");
            Assert.True(ixmlnl, "Input XmlNodeList");

            Type[] output = transform.OutputTypes;
            Assert.Equal(1, output.Length);
            Assert.Equal(typeof(Stream), output[0]);
        }

        [Fact]
        public void InputTypes()
        {
            Type[] input = transform.InputTypes;
            input[0] = null;
            input[1] = null;
            input[2] = null;
            // property does not return a clone
            Assert.All(transform.InputTypes, Assert.Null);

            // it's not a static array
            transform = new UnprotectedXmlDsigExcC14NWithCommentsTransform();
            Assert.All(transform.InputTypes, Assert.NotNull);
        }

        [Fact]
        public void GetInnerXml()
        {
            XmlNodeList xnl = transform.UnprotectedGetInnerXml();
            Assert.Null(xnl);
        }

        [Fact]
        public void OutputTypes()
        {
            // property does not return a clone
            transform.OutputTypes[0] = null;
            Assert.Null(transform.OutputTypes[0]);

            // it's not a static array
            transform = new UnprotectedXmlDsigExcC14NWithCommentsTransform();
            Assert.NotNull(transform.OutputTypes[0]);
        }
    }
}

