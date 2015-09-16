using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Xunit;

public static class XmlDictionaryWriterTest
{
    [Fact]
    public static void XmlBaseWriter_WriteBase64Async()
    {
        string actual;
        int byteSize = 1024;
        byte[] bytes = GetByteArray(byteSize);
        string expect = GetExpectString(bytes, byteSize);
        using (var ms = new AsyncMemoryStream())
        {
            var writer = XmlDictionaryWriter.CreateTextWriter(ms);
            writer.WriteStartDocument();
            writer.WriteStartElement("data");
            var task = writer.WriteBase64Async(bytes, 0, byteSize);
            task.Wait();
            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Flush();
            ms.Position = 0;
            var sr = new StreamReader(ms);
            actual = sr.ReadToEnd();
        }

        Assert.StrictEqual(expect, actual);
    }

    [Fact]
    public static void XmlBaseWriter_FlushAsync()
    {
        string actual;
        int byteSize = 1024;
        byte[] bytes = GetByteArray(byteSize);
        string expect = GetExpectString(bytes, byteSize);
        using (var ms = new AsyncMemoryStream())
        {
            var writer = XmlDictionaryWriter.CreateTextWriter(ms);
            writer.WriteStartDocument();
            writer.WriteStartElement("data");
            writer.WriteBase64(bytes, 0, byteSize);
            writer.WriteEndElement();
            writer.WriteEndDocument();
            var task = writer.FlushAsync();
            task.Wait();
            ms.Position = 0;
            var sr = new StreamReader(ms);
            actual = sr.ReadToEnd();
        }

        Assert.StrictEqual(expect, actual);
    }

    [Fact]
    public static void XmlBaseWriter_WriteStartEndElementAsync()
    {
        string actual;
        int byteSize = 1024;
        byte[] bytes = GetByteArray(byteSize);
        string expect = GetExpectString(bytes, byteSize);
        using (var ms = new AsyncMemoryStream())
        {
            var writer = XmlDictionaryWriter.CreateTextWriter(ms);
            writer.WriteStartDocument();
            // NOTE: the async method has only one overload that takes 3 params
            var t1 = writer.WriteStartElementAsync(null, "data", null);
            t1.Wait();
            writer.WriteBase64(bytes, 0, byteSize);
            var t2 = writer.WriteEndElementAsync();
            t2.Wait();
            writer.WriteEndDocument();
            writer.Flush();
            ms.Position = 0;
            var sr = new StreamReader(ms);
            actual = sr.ReadToEnd();
        }

        Assert.StrictEqual(expect, actual);
    }

    [Fact]
    public static void XmlBaseWriter_CheckAsync_ThrowInvalidOperationException()
    {
        int byteSize = 1024;
        byte[] bytes = GetByteArray(byteSize);
        using (var ms = new MemoryStreamWithBlockAsync())
        {
            var writer = XmlDictionaryWriter.CreateTextWriter(ms);
            writer.WriteStartDocument();
            writer.WriteStartElement("data");

            ms.blockAsync(true);
            var t1 = writer.WriteBase64Async(bytes, 0, byteSize);
            var t2 = Assert.ThrowsAsync<InvalidOperationException>(() => writer.WriteBase64Async(bytes, 0, byteSize));

            InvalidOperationException e = t2.Result;
            Assert.StrictEqual(e.Message, "An asynchronous operation is already in progress.");

            // let the first task complete
            ms.blockAsync(false);
            t1.Wait();
        }
    }

    private static byte[] GetByteArray(int byteSize)
    {
        var bytes = new byte[byteSize];
        for (int i = 0; i < byteSize; i++)
        {
            bytes[i] = 8;
        }

        return bytes;
    }

    private static string GetExpectString(byte[] bytes, int byteSize)
    {
        using (var ms = new MemoryStream())
        {
            var writer = XmlDictionaryWriter.CreateTextWriter(ms);
            writer.WriteStartDocument();
            writer.WriteStartElement("data");
            writer.WriteBase64(bytes, 0, byteSize);
            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Flush();
            ms.Position = 0;
            var sr = new StreamReader(ms);
            return sr.ReadToEnd();
        }

    }

    public class AsyncMemoryStream : MemoryStream
    {
        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await Task.Delay(1).ConfigureAwait(false);
            await base.WriteAsync(buffer, offset, count, cancellationToken);
        }
    }

    public class MemoryStreamWithBlockAsync : MemoryStream
    {
        private bool _blockAsync;
        public void blockAsync(bool blockAsync)
        {
            _blockAsync = blockAsync;
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            while (_blockAsync)
            {
                await Task.Delay(10).ConfigureAwait(false);
            }

            await base.WriteAsync(buffer, offset, count, cancellationToken);
        }
    }
}
