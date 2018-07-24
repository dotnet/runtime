// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml.Resolvers;
using Xunit;
using System.Net;
using System.IO;

namespace System.Xml.XmlResolver.Tests
{
    public class XmlPreloadedResolverMiscTests
    {
        [Fact]
        public void XmlResolverCredentialsCheck()
        {
            var xmlResolver = new XmlPreloadedResolver(new XmlPreloadedResolver());
            NetworkCredential credential = new NetworkCredential();
            xmlResolver.Credentials = credential;                        
        }

        [Fact]
        public void XmlResolverSupportsTypeWithInvalidData()
        {
            var xmlResolver = new XmlPreloadedResolver();
            Assert.Throws<ArgumentNullException>(() => xmlResolver.SupportsType(null, null));

            xmlResolver = new XmlPreloadedResolver();
            var uri = new Uri("-//W3C//Dummy URI", UriKind.RelativeOrAbsolute);
            bool result = xmlResolver.SupportsType(uri, null);
            Assert.True(result);

            result = xmlResolver.SupportsType(uri, typeof(string));
            Assert.False(result);

            xmlResolver = new XmlPreloadedResolver(new XmlPreloadedResolver());
            result = xmlResolver.SupportsType(uri, typeof(string));
            Assert.False(result);
        }

        [Fact]
        public void XmlResolverSupportsTypeWithValidData()
        {
            var xmlResolver = new XmlPreloadedResolver();
            var uri = new Uri("-//W3C//DTD XHTML 1.0 Strict//EN", UriKind.RelativeOrAbsolute);
            bool result = xmlResolver.SupportsType(uri, typeof(string));
            Assert.False(result);

            xmlResolver = new XmlPreloadedResolver(new XmlPreloadedResolver(), XmlKnownDtds.Xhtml10);
            Assert.True(xmlResolver.SupportsType(uri, typeof(Stream)));

            xmlResolver.Add(uri, "String Value");
            result = xmlResolver.SupportsType(uri, typeof(TextReader));
            Assert.True(result);

            result = xmlResolver.SupportsType(uri, null);
            Assert.True(result);
        }
    }
}
