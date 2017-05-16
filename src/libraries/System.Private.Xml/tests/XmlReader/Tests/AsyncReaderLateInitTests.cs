// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;
using Xunit;

namespace System.Xml.Tests
{
    public static class AsyncReaderLateInitTests
    {
        private const string _dummyXml = @"<?xml version=""1.0""?>
                <root>
                    <a/><!-- comment -->
                    <b>bbb</b>
                    <c>
                        <d>ddd</d>
                    </c>
                </root>";

        private static Stream GetDummyXmlStream()
        {
            byte[] buffer = Encoding.UTF8.GetBytes(_dummyXml);
            return new MemoryStream(buffer);
        }

        private static TextReader GetDummyXmlTextReader()
        {
            return new StringReader(_dummyXml);
        }

        [Fact]
        public static void ReadAsyncAfterInitializationWithStreamDoesNotThrow()
        {
            using (XmlReader reader = XmlReader.Create(GetDummyXmlStream(), new XmlReaderSettings() { Async = true }))
            {
                reader.ReadAsync().Wait();
            }
        }

        [Fact]
        public static void ReadAfterInitializationWithStreamOnAsyncReaderDoesNotThrow()
        {
            using (XmlReader reader = XmlReader.Create(GetDummyXmlStream(), new XmlReaderSettings() { Async = true }))
            {
                reader.Read();
            }
        }

        [Fact]
        public static void ReadAsyncAfterInitializationWithTextReaderDoesNotThrow()
        {
            using (XmlReader reader = XmlReader.Create(GetDummyXmlTextReader(), new XmlReaderSettings() { Async = true }))
            {
                reader.ReadAsync().Wait();
            }
        }

        [Fact]
        public static void ReadAfterInitializationWithTextReaderOnAsyncReaderDoesNotThrow()
        {
            using (XmlReader reader = XmlReader.Create(GetDummyXmlTextReader(), new XmlReaderSettings() { Async = true }))
            {
                reader.Read();
            }
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap |  //[ActiveIssue(13121)]  // Path cannot be resolved in UWP
        TargetFrameworkMonikers.NetFramework)]  //[ActiveIssue(19806)] // Full framework uses WebRequest instead of HttpRequest
        public static void ReadAsyncAfterInitializationWithUriThrows()
        {
            using (XmlReader reader = XmlReader.Create("http://test.test/test.html", new XmlReaderSettings() { Async = true }))
            {
                Assert.Throws<System.Net.Http.HttpRequestException>(() => reader.ReadAsync().GetAwaiter().GetResult());
            }
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework)] // Full framework uses WebRequest instead of HttpRequest
        public static void ReadAsyncAfterInitializationWithUriThrows_FullFramework()
        {
            using (XmlReader reader = XmlReader.Create("http://test.test/test.html", new XmlReaderSettings() { Async = true }))
            {
                Assert.Throws<System.Net.WebException>(() => reader.ReadAsync().GetAwaiter().GetResult());
            }
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap |  //[ActiveIssue(13121)]  // Path cannot be resolved in UWP
        TargetFrameworkMonikers.NetFramework)]  //[ActiveIssue(19806)] // Full framework uses WebRequest instead of HttpRequest
        public static void ReadAfterInitializationWithUriOnAsyncReaderTrows()
        {
            using (XmlReader reader = XmlReader.Create("http://test.test/test.html", new XmlReaderSettings() { Async = true }))
            {
                AggregateException ae = Assert.Throws<System.AggregateException>(() => reader.Read());
                Assert.Equal(typeof(System.Net.Http.HttpRequestException), ae.InnerException.GetType());
            }
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework)] // Full framework uses WebRequest instead of HttpRequest
        public static void ReadAfterInitializationWithUriOnAsyncReaderTrows_FullFramework()
        {
            using (XmlReader reader = XmlReader.Create("http://test.test/test.html", new XmlReaderSettings() { Async = true }))
            {
                AggregateException ae = Assert.Throws<System.AggregateException>(() => reader.Read());
                Assert.Equal(typeof(System.Net.WebException), ae.InnerException.GetType());
            }
        }
    }
}
