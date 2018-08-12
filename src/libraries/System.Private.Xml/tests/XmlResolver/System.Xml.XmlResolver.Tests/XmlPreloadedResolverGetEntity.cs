// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml.Resolvers;
using System.IO;
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
        public void XmlResolverGetEntityWithValidData()
        {
            var uri = new Uri("-//W3C//DTD XHTML 1.0 Transitional//EN", UriKind.RelativeOrAbsolute);
            XmlPreloadedResolver xmlResolver = 
                GetResolverWithStringData(XmlKnownDtds.Xhtml10, "Sample String Data", uri);                
            Stream streamResult = xmlResolver.GetEntity(uri, null, null) as Stream;
            Assert.NotNull(streamResult);
            byte[] data = new byte[streamResult.Length];
            streamResult.Read(data, 0, Convert.ToInt32(streamResult.Length));
            Assert.Equal("Sample String Data", Encoding.ASCII.GetString(data).Replace("\0", ""));
            
            uri = new Uri("-//W3C//DTD XHTML 1.0 Transitional//EN", UriKind.RelativeOrAbsolute);
            xmlResolver = GetResolverWithStringData(XmlKnownDtds.Xhtml10, "Sample String Data", uri);
            TextReader textResult = xmlResolver.GetEntity(uri, null, typeof(TextReader)) as TextReader;
            Assert.NotNull(textResult);
            Assert.Equal("Sample String Data", textResult.ReadLine());
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
        public void XmlResolverGetEntityAsyncWithValidData()
        {
            byte[] inpData = Encoding.ASCII.GetBytes("hello world");
            var xmlResolver = new XmlPreloadedResolver(XmlKnownDtds.Xhtml10);
            xmlResolver.Add(new Uri("-//W3C//ENTITIES Latin 1 for XHTML//EN", UriKind.RelativeOrAbsolute), inpData);
            Task<object> output = xmlResolver.GetEntityAsync(new Uri("-//W3C//ENTITIES Latin 1 for XHTML//EN", 
                UriKind.RelativeOrAbsolute), null, typeof(Stream));
            var result = new byte[inpData.Length];
            (output.Result as Stream).Read(result, 0, result.Length);
            Assert.Equal(inpData, result);
        }
                
    }
}
