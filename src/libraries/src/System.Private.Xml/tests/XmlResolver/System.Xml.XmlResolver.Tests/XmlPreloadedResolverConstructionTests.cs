// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Xml.Resolvers;
using Xunit;

namespace System.Xml.XmlResolver.Tests
{
    public class XmlPreloadedResolverConstructionTests
    {
        [Fact]
        public void XmlResolverWithDefaultConstructor()
        {
            var resolver = new XmlPreloadedResolver();
            Assert.Equal(14, resolver.PreloadedUris.Count());
        }

        [Fact]
        public void XmlResolverWithKnownDtdsXhtml10Constructor()
        {
            var resolver = new XmlPreloadedResolver(XmlKnownDtds.Xhtml10);
            string[] expectedUris = { "-//W3C//DTD XHTML 1.0 Strict//EN" , "-//W3C//DTD XHTML 1.0 Transitional//EN" ,
            "-//W3C//ENTITIES Symbols for XHTML//EN", "-//W3C//ENTITIES Latin 1 for XHTML//EN", "-//W3C//ENTITIES Symbols for XHTML//EN",
            "-//W3C//ENTITIES Special for XHTML//EN", "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd",
            "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd", "http://www.w3.org/TR/xhtml1/DTD/xhtml1-frameset.dtd",
            "http://www.w3.org/TR/xhtml1/DTD/xhtml-lat1.ent", "http://www.w3.org/TR/xhtml1/DTD/xhtml-symbol.ent",
            "http://www.w3.org/TR/xhtml1/DTD/xhtml-special.ent"};

            Assert.Equal(expectedUris.Length, resolver.PreloadedUris.Count());
            foreach (string uriString in expectedUris)
            {
                Assert.True(resolver.PreloadedUris.Any(u => u.OriginalString.Equals(uriString)));
            }
        }

        [Fact]
        public void XmlResolverWithKnownDtdsRss091Constructor()
        {
            var resolver = new XmlPreloadedResolver(XmlKnownDtds.Rss091);
            string[] expectedUris = { "-//Netscape Communications//DTD RSS 0.91//EN", "http://my.netscape.com/publish/formats/rss-0.91.dtd" };
            Assert.Equal(expectedUris.Length, resolver.PreloadedUris.Count());
            foreach (string uriString in expectedUris)
            {
                Assert.True(resolver.PreloadedUris.Any(u => u.OriginalString.Equals(uriString)));
            }
        }

        [Fact]
        public void XmlResolverWithFallbackResolverConstructor()
        {
            var fallbackResolver = new XmlPreloadedResolver(XmlKnownDtds.None);
            var resolver = new XmlPreloadedResolver(fallbackResolver);            
            string[] expectedUris = { "-//W3C//DTD XHTML 1.0 Strict//EN" , "-//W3C//DTD XHTML 1.0 Transitional//EN" ,
            "-//W3C//ENTITIES Symbols for XHTML//EN", "-//W3C//ENTITIES Latin 1 for XHTML//EN", "-//W3C//ENTITIES Symbols for XHTML//EN",
            "-//W3C//ENTITIES Special for XHTML//EN", "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd",
            "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd", "http://www.w3.org/TR/xhtml1/DTD/xhtml1-frameset.dtd",
            "http://www.w3.org/TR/xhtml1/DTD/xhtml-lat1.ent", "http://www.w3.org/TR/xhtml1/DTD/xhtml-symbol.ent",
            "http://www.w3.org/TR/xhtml1/DTD/xhtml-special.ent", "-//Netscape Communications//DTD RSS 0.91//EN",
                "http://my.netscape.com/publish/formats/rss-0.91.dtd" };
            Assert.Equal(expectedUris.Length, resolver.PreloadedUris.Count());            
            foreach (string uriString in expectedUris)
            {
                Assert.True(resolver.PreloadedUris.Any(u => u.OriginalString.Equals(uriString)));
            }
        }

        [Fact]
        public void XmlResolverWithFallbackAndKnownDtdsConstructor()
        {
            var fallbackResolver = new XmlPreloadedResolver();
            var resolver = new XmlPreloadedResolver(fallbackResolver, XmlKnownDtds.Rss091);
            string[] expectedUris = { "-//Netscape Communications//DTD RSS 0.91//EN", "http://my.netscape.com/publish/formats/rss-0.91.dtd" };
            Assert.Equal(expectedUris.Length, resolver.PreloadedUris.Count());
            foreach (string uriString in expectedUris)
            {
                Assert.True(resolver.PreloadedUris.Any(u => u.OriginalString.Equals(uriString)));
            }
        }

        [Fact]
        public void XmlResolverWithParameterizedConstructor()
        {
            var fallbackResolver = new XmlPreloadedResolver();
            var resolver = new XmlPreloadedResolver(fallbackResolver, XmlKnownDtds.Xhtml10, null);
            string[] expectedUris = { "-//W3C//DTD XHTML 1.0 Strict//EN" , "-//W3C//DTD XHTML 1.0 Transitional//EN" ,
            "-//W3C//ENTITIES Symbols for XHTML//EN", "-//W3C//ENTITIES Latin 1 for XHTML//EN", "-//W3C//ENTITIES Symbols for XHTML//EN",
            "-//W3C//ENTITIES Special for XHTML//EN", "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd",
            "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd", "http://www.w3.org/TR/xhtml1/DTD/xhtml1-frameset.dtd",
            "http://www.w3.org/TR/xhtml1/DTD/xhtml-lat1.ent", "http://www.w3.org/TR/xhtml1/DTD/xhtml-symbol.ent",
            "http://www.w3.org/TR/xhtml1/DTD/xhtml-special.ent"};

            Assert.Equal(expectedUris.Length, resolver.PreloadedUris.Count());
            foreach (string uriString in expectedUris)
            {
                Assert.True(resolver.PreloadedUris.Any(u => u.OriginalString.Equals(uriString)));
            }
        }
    }
}
