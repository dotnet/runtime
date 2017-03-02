//
// XmlLicenseTransformTest.cs - Test Cases for XmlLicenseTransform
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

using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{
    public class UnprotectedXmlLicenseTransform : XmlLicenseTransform
    {
        public XmlNodeList UnprotectedGetInnerXml()
        {
            return base.GetInnerXml();
        }
    }

    public class XmlLicenseTransformTest
    {
        private UnprotectedXmlLicenseTransform transform;

        public XmlLicenseTransformTest()
        {
            transform = new UnprotectedXmlLicenseTransform();
        }

        [Fact] // ctor ()
        public void Constructor1()
        {
            Assert.Equal("urn:mpeg:mpeg21:2003:01-REL-R-NS:licenseTransform",
                transform.Algorithm);
            Assert.Null(transform.Decryptor);

            Type[] input = transform.InputTypes;
            Assert.Equal(1, input.Length);
            Assert.Equal(typeof(XmlDocument), input[0]);

            Type[] output = transform.OutputTypes;
            Assert.Equal(1, output.Length);
            Assert.Equal(typeof(XmlDocument), output[0]);
        }

        [Fact]
        public void InputTypes()
        {
            // property does not return a clone
            transform.InputTypes[0] = null;
            Assert.Null(transform.InputTypes[0]);

            // it's not a static array
            transform = new UnprotectedXmlLicenseTransform();
            Assert.NotNull(transform.InputTypes[0]);
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
            transform = new UnprotectedXmlLicenseTransform();
            Assert.NotNull(transform.OutputTypes[0]);
        }
    }
}

