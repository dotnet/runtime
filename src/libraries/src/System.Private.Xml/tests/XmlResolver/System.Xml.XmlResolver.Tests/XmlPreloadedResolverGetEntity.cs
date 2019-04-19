// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml.Resolvers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using System.Threading.Tasks;

namespace System.Xml.XmlResolver.Tests
{
    public class XmlPreloadedResolverGetEntity
    {        
        private XmlPreloadedResolver GetResolverWithStringData(XmlKnownDtds dtd, string data, Uri uri)
        {
            var xmlResolver = new XmlPreloadedResolver(dtd);
            xmlResolver.Add(uri, data);
            return xmlResolver;
        }

        [Fact]
        public void XmlResolverGetEntityWithNullUri()
        {
            var xmlResolver = new XmlPreloadedResolver(XmlKnownDtds.None);
            Assert.Throws<ArgumentNullException>(() => xmlResolver.GetEntity(null, null, null));
        }

        [Fact]
        public void XmlResolverGetEntityWithInvalidData()
        {
            var xmlResolver = new XmlPreloadedResolver(XmlKnownDtds.Xhtml10);
            Assert.Throws<XmlException>(() => xmlResolver.GetEntity(new Uri("https://JustAUri"), null, typeof(string)));

            xmlResolver = new XmlPreloadedResolver(new XmlPreloadedResolver(), XmlKnownDtds.Xhtml10);
            Assert.Throws<XmlException>(() => xmlResolver.GetEntity(new Uri("https://JustAUri"), null, typeof(string)));

            xmlResolver = new XmlPreloadedResolver(XmlKnownDtds.Xhtml10);
            Assert.Throws<XmlException>(() => xmlResolver.GetEntity(new Uri("-//W3C//DTD XHTML 1.0 Transitional//EN", UriKind.RelativeOrAbsolute)
                , null, typeof(string)));

            xmlResolver = new XmlPreloadedResolver(new XmlPreloadedResolver(), XmlKnownDtds.Xhtml10);
            Assert.Throws<XmlException>(() => xmlResolver.GetEntity(new Uri("-//W3C//Invalid URI ", UriKind.RelativeOrAbsolute)
                , null, typeof(string)));
        }

        [Fact]
        public void XmlResolverGetEntityWithValidUserSuppliedData()
        {
            var uri = new Uri("-//W3C//DTD FAKE 1.0 Not Real//EN", UriKind.RelativeOrAbsolute);
            XmlPreloadedResolver xmlResolver = 
                GetResolverWithStringData(XmlKnownDtds.Xhtml10, "Sample String Data", uri);                
            Stream streamResult = xmlResolver.GetEntity(uri, null, null) as Stream;
            Assert.NotNull(streamResult);
            byte[] data = new byte[streamResult.Length];
            streamResult.Read(data, 0, Convert.ToInt32(streamResult.Length));
            Assert.Equal("Sample String Data", NormalizeContent(Encoding.ASCII.GetString(data)));
            
            uri = new Uri("-//W3C//DTD FAKE 1.0 Not Real//EN", UriKind.RelativeOrAbsolute);
            xmlResolver = GetResolverWithStringData(XmlKnownDtds.Xhtml10, "Sample String Data", uri);
            TextReader textResult = xmlResolver.GetEntity(uri, null, typeof(TextReader)) as TextReader;
            Assert.NotNull(textResult);
            Assert.Equal("Sample String Data", textResult.ReadLine());
        }

        [Fact]
        public void XmlResolverGetKnownEntity()
        {
            var xmlResolver = new XmlPreloadedResolver(XmlKnownDtds.All);
            foreach (var dtdInfo in GetKnownDtds())
            {
                string expectedContent = NormalizeContent(File.ReadAllText(dtdInfo.SourcePath));

                var uri = new Uri(dtdInfo.PublicId, UriKind.RelativeOrAbsolute);
                Stream streamResult = xmlResolver.GetEntity(uri, null, null) as Stream;
                Assert.NotNull(streamResult);
                byte[] data = new byte[streamResult.Length];
                streamResult.Read(data, 0, Convert.ToInt32(streamResult.Length));
                Assert.Equal(expectedContent, NormalizeContent(Encoding.ASCII.GetString(data)));

                uri = new Uri(dtdInfo.SystemId, UriKind.RelativeOrAbsolute);
                streamResult = xmlResolver.GetEntity(uri, null, null) as Stream;
                Assert.NotNull(streamResult);
                data = new byte[streamResult.Length];
                streamResult.Read(data, 0, Convert.ToInt32(streamResult.Length));
                Assert.Equal(expectedContent, NormalizeContent(Encoding.ASCII.GetString(data)));
            }
        }

        [Fact]
        public void XmlResolverGetEntityAsyncWithInvalidData()
        {
            var xmlResolver = new XmlPreloadedResolver(XmlKnownDtds.Xhtml10);
            Assert.ThrowsAsync<ArgumentNullException>(() => xmlResolver.GetEntityAsync(null, null, null));
            Assert.ThrowsAsync<XmlException>(() => xmlResolver.GetEntityAsync(new Uri("https://DummyUri"), null, null));
            Assert.ThrowsAsync<XmlException>(() => 
                xmlResolver.GetEntityAsync(new Uri("-//W3C//ENTITIES Latin 1 for XHTML//EN", UriKind.RelativeOrAbsolute), null, typeof(string)));

            xmlResolver = new XmlPreloadedResolver(new XmlPreloadedResolver(), XmlKnownDtds.Xhtml10);
            Assert.ThrowsAsync<XmlException>(() =>
                xmlResolver.GetEntityAsync(new Uri("https://DummyUri", UriKind.RelativeOrAbsolute), null, typeof(string)));

            Assert.ThrowsAsync<XmlException>(() => 
                xmlResolver.GetEntityAsync(new Uri("-//W3C//ENTITIES Latin 1 for XHTML//EN", UriKind.RelativeOrAbsolute), null, typeof(TextReader)));
        }

        [Fact]
        public void XmlResolverGetEntityAsyncWithValidUserSuppliedData()
        {
            byte[] inpData = Encoding.ASCII.GetBytes("hello world");
            var xmlResolver = new XmlPreloadedResolver(XmlKnownDtds.Xhtml10);
            xmlResolver.Add(new Uri("-//W3C//DTD FAKE 1.0 Not Real//EN", UriKind.RelativeOrAbsolute), inpData);
            Task<object> output = xmlResolver.GetEntityAsync(new Uri("-//W3C//DTD FAKE 1.0 Not Real//EN", 
                UriKind.RelativeOrAbsolute), null, typeof(Stream));
            var result = new byte[inpData.Length];
            (output.Result as Stream).Read(result, 0, result.Length);
            Assert.Equal(inpData, result);
        }

        [Fact]
        public async Task XmlResolverGetKnownEntityAsync()
        {
            var xmlResolver = new XmlPreloadedResolver(XmlKnownDtds.All);
            foreach (var dtdInfo in GetKnownDtds())
            {
                string expectedContent = NormalizeContent(File.ReadAllText(dtdInfo.SourcePath));

                var uri = new Uri(dtdInfo.PublicId, UriKind.RelativeOrAbsolute);
                Stream streamResult = await xmlResolver.GetEntityAsync(uri, null, null) as Stream;
                Assert.NotNull(streamResult);
                byte[] data = new byte[streamResult.Length];
                streamResult.Read(data, 0, Convert.ToInt32(streamResult.Length));
                Assert.Equal(expectedContent, NormalizeContent(Encoding.ASCII.GetString(data)));

                uri = new Uri(dtdInfo.SystemId, UriKind.RelativeOrAbsolute);
                streamResult = await xmlResolver.GetEntityAsync(uri, null, null) as Stream;
                Assert.NotNull(streamResult);
                data = new byte[streamResult.Length];
                streamResult.Read(data, 0, Convert.ToInt32(streamResult.Length));
                Assert.Equal(expectedContent, NormalizeContent(Encoding.ASCII.GetString(data)));
            }
        }

        private static IEnumerable<(string PublicId, string SystemId, string SourcePath)> GetKnownDtds()
        {
            var xhtmlDtds = new (string PublicId, string RelativeId)[]
            {
                ("-//W3C//DTD XHTML 1.0 Strict//EN", "xhtml1-strict.dtd"),
                ("-//W3C//DTD XHTML 1.0 Transitional//EN", "xhtml1-transitional.dtd"),
                ("-//W3C//DTD XHTML 1.0 Frameset//EN", "xhtml1-frameset.dtd"),
                ("-//W3C//ENTITIES Latin 1 for XHTML//EN", "xhtml-lat1.ent"),
                ("-//W3C//ENTITIES Symbols for XHTML//EN", "xhtml-symbol.ent"),
                ("-//W3C//ENTITIES Special for XHTML//EN", "xhtml-special.ent"),
            };
            var rssDtds = new (string PublicId, string RelativeId)[]
            {
                ("-//Netscape Communications//DTD RSS 0.91//EN", "rss-0.91.dtd"),
            };

            string dtdFolderRoot = Path.Combine("Utils", "DTDs");

            return Enumerable.Concat(
                GetKnownDtds(xhtmlDtds, "http://www.w3.org/TR/xhtml1/DTD/", Path.Combine(dtdFolderRoot, "XHTML10", "no_comments")),
                GetKnownDtds(rssDtds, "http://my.netscape.com/publish/formats/", Path.Combine(dtdFolderRoot, "RSS091", "no_comments")));

            IEnumerable<(string PublicId, string SystemId, string SourcePath)> GetKnownDtds(
                IEnumerable<(string PublicId, string RelativeId)> ids,
                string systemUrlPrefix,
                string pathPrefix) =>
                ids.Select(x => (x.PublicId, systemUrlPrefix + x.RelativeId, Path.Combine(pathPrefix, x.RelativeId)));
        }

        private static string NormalizeContent(string content) => content.Replace("\0", "").Replace("\r\n", "\n");
    }
}
