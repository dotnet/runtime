// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tests;
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

        [Theory, InlineData(true), InlineData(false)]
        public static void ReadAfterInitializationWithStreamOnAsyncReaderDoesNotThrow(bool async)
        {
            using (XmlReader reader = XmlReader.Create(GetDummyXmlStream(), new XmlReaderSettings() { Async = async }))
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

        [Theory, InlineData(true), InlineData(false)]
        public static void ReadAfterInitializationWithTextReaderOnAsyncReaderDoesNotThrow(bool async)
        {
            using (XmlReader reader = XmlReader.Create(GetDummyXmlTextReader(), new XmlReaderSettings() { Async = async }))
            {
                reader.Read();
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49187", TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS)]
        public static void ReadAsyncAfterInitializationWithUriThrows()
        {
            using (XmlReader reader = XmlReader.Create("http://test.test/test.html", new XmlReaderSettings() { Async = true }))
            {
                Assert.Throws<System.Net.Http.HttpRequestException>(() => reader.ReadAsync().GetAwaiter().GetResult());
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49187", TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS)]
        public static void ReadAfterInitializationWithUriOnAsyncReaderTrows()
        {
            using (XmlReader reader = XmlReader.Create("http://test.test/test.html", new XmlReaderSettings() { Async = true }))
            {
                Assert.Throws<System.Net.Http.HttpRequestException>(() => reader.Read());
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsSubsystemForLinux), nameof(PlatformDetection.IsThreadingSupported))] // [ActiveIssue("https://github.com/dotnet/runtime/issues/18258")]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49187", TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS)]
        public static void InitializationWithUriOnNonAsyncReaderThrows()
        {
            Assert.Throws<System.Net.Http.HttpRequestException>(() => XmlReader.Create("http://test.test/test.html", new XmlReaderSettings() { Async = false }));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void SynchronizationContextCurrent_NotUsedForAsyncOperations()
        {
            Task.Run(() =>
            {
                var sc = new TrackingSynchronizationContext();
                SynchronizationContext.SetSynchronizationContext(sc);

                using (XmlReader reader = XmlReader.Create(new DribbleReadXmlAsyncStream(_dummyXml), new XmlReaderSettings { Async = true,  }))
                {
                    while (reader.ReadAsync().GetAwaiter().GetResult());
                }

                Assert.True(sc.CallStacks.Count == 0, "Sync Ctx used: " + string.Join(Environment.NewLine + Environment.NewLine, sc.CallStacks));
            }).GetAwaiter().GetResult();
        }

        private sealed class DribbleReadXmlAsyncStream : Stream
        {
            private readonly byte[] _bytes;
            private int _pos;

            public DribbleReadXmlAsyncStream(string xml) => _bytes = Encoding.UTF8.GetBytes(xml);

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
                Task.Run(() => // to dribble out a byte at a time
                {
                    if (count <= 0 || _pos >= _bytes.Length)
                    {
                        return 0;
                    }

                    buffer[offset] = _bytes[_pos++];
                    return 1;
                });

            public override int Read(byte[] buffer, int offset, int count) => ReadAsync(buffer, offset, count).GetAwaiter().GetResult();
            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            public override void Flush() { }
            public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }
    }
}
