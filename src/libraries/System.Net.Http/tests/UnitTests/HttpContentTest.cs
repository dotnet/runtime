// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Reflection;

using Xunit;

namespace System.Net.Http.Tests
{
    public class HttpContentTest
    {
        [Fact]
        public void Dispose_BufferContentThenDisposeContent_BufferedStreamGetsDisposed()
        {
            MockContent content = new MockContent();
            content.LoadIntoBufferAsync().Wait();

            Type type = typeof(HttpContent);
            TypeInfo typeInfo = type.GetTypeInfo();
            FieldInfo bufferedContentField = typeof(HttpContent).GetField("_bufferedContent",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(bufferedContentField);

            MemoryStream bufferedContentStream = bufferedContentField.GetValue(content) as MemoryStream;
            Assert.NotNull(bufferedContentStream);

            content.Dispose();

            // The following line will throw an ObjectDisposedException if the buffered-stream was correctly disposed.
            Assert.Throws<ObjectDisposedException>(() => { string str = bufferedContentStream.Length.ToString(); });
        }
    }
}
