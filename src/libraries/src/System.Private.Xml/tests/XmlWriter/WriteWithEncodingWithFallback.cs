// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;
using Xunit;

namespace System.Xml.Tests
{
    public class XmlWriterTests_EncodingFallback
    {
        private const char SurHighStart = '\ud800';
        //const char SurHighEnd = '\udbff';
        private const char SurLowStart = '\udc00';
        //const char SurLowEnd = '\udfff';

        // This character is allowed in xml tag name but not in the us-ascii encoding.
        private const char ProblematicChar = '\u0300';
        private const string ProblematicCharEntity = "&#x300;";

        private static readonly string s_ExampleSurrogate = new string(new char[2] { SurHighStart, SurLowStart });
        // To convert from surrogate pair to entity hex use following formula:
        // (highChar - 0xD800) * 0x400 + (lowChar - 0xDC00) + 0x10000
        private const string ExampleSurrogateEntity = "&#x10000;";

        [Fact]
        public static void XmlWriterConvertsInvalidCharacterToEntity()
        {
            MemoryStream ms = new MemoryStream();
            XmlWriterSettings settings = new XmlWriterSettings();

            // Code page: 367
            settings.Encoding = Encoding.GetEncoding("us-ascii");

            string problematicString = "ABCDEF" + ProblematicChar + "GHI";
            string problematicStringAfterFallback = "ABCDEF" + ProblematicCharEntity + "GHI";

            using (XmlWriter writer = XmlWriter.Create(ms, settings))
            {
                writer.WriteStartDocument();
                writer.WriteElementString("test", problematicString);
                writer.Flush();
            }

            ms.Position = 0;
            StreamReader sr = new StreamReader(ms);
            string output = sr.ReadToEnd();

            string expectedOutput = "<?xml version=\"1.0\" encoding=\"us-ascii\"?><test>" + problematicStringAfterFallback + "</test>";
            Assert.Equal(expectedOutput, output);
        }

        [Fact]
        public static void EncodingFallbackFailsWhenInvalidCharacterInTagName()
        {
            MemoryStream ms = new MemoryStream();
            XmlWriterSettings settings = new XmlWriterSettings();

            // Code page: 367
            settings.Encoding = Encoding.GetEncoding("us-ascii");

            string problematicString = "ABCDEF" + ProblematicChar + "GHI";

            using (XmlWriter writer = XmlWriter.Create(ms, settings))
            {
                writer.WriteStartDocument();
                Assert.Throws<System.Text.EncoderFallbackException>(() =>
                    {
                        writer.WriteElementString(problematicString, "test");
                        writer.Flush();
                    });
            }
        }

        [Fact]
        public static void XmlWriterConvertsSurrogatePairToEntity()
        {
            MemoryStream ms = new MemoryStream();
            XmlWriterSettings settings = new XmlWriterSettings();

            // Code page: 367
            settings.Encoding = Encoding.GetEncoding("us-ascii");

            string problematicString = "ABCDEF" + s_ExampleSurrogate + "GHI";
            string problematicStringAfterFallback = "ABCDEF" + ExampleSurrogateEntity + "GHI";

            using (XmlWriter writer = XmlWriter.Create(ms, settings))
            {
                writer.WriteStartDocument();
                writer.WriteElementString("test", problematicString);
                writer.Flush();
            }

            ms.Position = 0;
            StreamReader sr = new StreamReader(ms);
            string output = sr.ReadToEnd();

            string expectedOutput = "<?xml version=\"1.0\" encoding=\"us-ascii\"?><test>" + problematicStringAfterFallback + "</test>";
            Assert.Equal(expectedOutput, output);
        }

        [Fact]
        public static void AsyncXmlWriterConvertsInvalidCharacterToEntity()
        {
            MemoryStream ms = new MemoryStream();
            XmlWriterSettings settings = new XmlWriterSettings();

            // Code page: 367
            settings.Encoding = Encoding.GetEncoding("us-ascii");
            settings.Async = true;

            string problematicString = "ABCDEF" + ProblematicChar + "GHI";
            string problematicStringAfterFallback = "ABCDEF" + ProblematicCharEntity + "GHI";

            using (XmlWriter writer = XmlWriter.Create(ms, settings))
            {
                writer.WriteStartDocumentAsync().Wait();
                writer.WriteElementStringAsync(null, "test", null, problematicString).Wait();
                writer.FlushAsync().Wait();
            }

            ms.Position = 0;
            StreamReader sr = new StreamReader(ms);
            string output = sr.ReadToEnd();

            string expectedOutput = "<?xml version=\"1.0\" encoding=\"us-ascii\"?><test>" + problematicStringAfterFallback + "</test>";
            Assert.Equal(expectedOutput, output);
        }

        [Fact]
        public static void AsyncEncodingFallbackFailsWhenInvalidCharacterInTagName()
        {
            MemoryStream ms = new MemoryStream();
            XmlWriterSettings settings = new XmlWriterSettings();

            // Code page: 367
            settings.Encoding = Encoding.GetEncoding("us-ascii");
            settings.Async = true;

            string problematicString = "ABCDEF" + ProblematicChar + "GHI";

            using (XmlWriter writer = XmlWriter.Create(ms, settings))
            {
                writer.WriteStartDocumentAsync().Wait();
                Exception exception = Assert.Throws<System.AggregateException>(() =>
                    {
                        writer.WriteElementStringAsync(null, problematicString, null, "test").Wait();
                        writer.FlushAsync().Wait();
                    });

                Assert.Equal(typeof(System.Text.EncoderFallbackException), exception.InnerException.GetType());
            }
        }

        [Fact]
        public static void AsyncXmlWriterConvertsSurrogatePairToEntity()
        {
            MemoryStream ms = new MemoryStream();
            XmlWriterSettings settings = new XmlWriterSettings();

            // Code page: 367
            settings.Encoding = Encoding.GetEncoding("us-ascii");
            settings.Async = true;

            string problematicString = "ABCDEF" + s_ExampleSurrogate + "GHI";
            string problematicStringAfterFallback = "ABCDEF" + ExampleSurrogateEntity + "GHI";

            using (XmlWriter writer = XmlWriter.Create(ms, settings))
            {
                writer.WriteStartDocumentAsync().Wait();
                writer.WriteElementStringAsync(null, "test", null, problematicString).Wait();
                writer.FlushAsync().Wait();
            }

            ms.Position = 0;
            StreamReader sr = new StreamReader(ms);
            string output = sr.ReadToEnd();

            string expectedOutput = "<?xml version=\"1.0\" encoding=\"us-ascii\"?><test>" + problematicStringAfterFallback + "</test>";
            Assert.Equal(expectedOutput, output);
        }
    }
}
