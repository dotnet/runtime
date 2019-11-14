// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml.Resolvers;
using Xunit;

namespace System.Xml.XmlResolver.Tests
{
    public class XmlPreloadedResolverResolveUriTests
    {
        [Fact]
        public void XmlResolverResolveUriWithEmptyUri()
        {
            var resolver = new XmlPreloadedResolver(XmlKnownDtds.Xhtml10);
            Assert.Throws<ArgumentNullException>(() => resolver.ResolveUri(null, null));
        }

        [Fact]
        public void XmlResolverResolveUriWithNoneDTD()
        {
            var resolver = new XmlPreloadedResolver(XmlKnownDtds.None);
            Uri result = resolver.ResolveUri(new Uri("https://Uri"), "-//W3C//");
            Assert.NotNull(result);
        }

        [Fact]
        public void XmlResolverResolveUriWithRss091DTD()
        {
            var resolver = new XmlPreloadedResolver(XmlKnownDtds.Rss091);
            Uri result = resolver.ResolveUri(new Uri("https://JustAUri"), "-//Netscape Communications//DTD RSS 0.91//EN");
            Assert.Equal("-//Netscape Communications//DTD RSS 0.91//EN", result.OriginalString);

            result = resolver.ResolveUri(new Uri("https://JustAUri"), "-//Invalid//Uri");
            Assert.Equal("https://justauri/-//Invalid//Uri", result.OriginalString);
        }

        [Fact]
        public void XmlResolverResolveUriWithXhtml10DTD()
        {
            var resolver = new XmlPreloadedResolver(XmlKnownDtds.Xhtml10);
            Uri result = resolver.ResolveUri(new Uri("https://JustAUri"), "-//W3C//DTD XHTML 1.0 Frameset//EN");
            Assert.Equal("-//W3C//DTD XHTML 1.0 Frameset//EN", result.OriginalString);

            result = resolver.ResolveUri(new Uri("https://JustAUri"), "-//Invalid//Uri");
            Assert.Equal("https://justauri/-//Invalid//Uri", result.OriginalString);
        }
    }
}
