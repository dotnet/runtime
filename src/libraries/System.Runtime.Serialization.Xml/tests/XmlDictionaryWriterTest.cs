// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Xunit;

public static class XmlDictionaryWriterTest
{
    [Theory]
    [InlineData("01020304-0506-0708-090a-0b0c0d0e0f10")]
    [InlineData("ffffffff-ffff-ffff-ffff-ffffffffffff")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public static void UniqueId_GuidConversions_PreserveByteOrder(string text)
    {
        Guid guid = new Guid(text);
        UniqueId fromGuid = new UniqueId(guid);
        UniqueId fromBytes = new UniqueId(guid.ToByteArray());
        Assert.Equal(fromBytes.ToString(), fromGuid.ToString());
        Assert.Equal(guid != Guid.Empty, fromGuid.TryGetGuid(out Guid actual));
        Assert.Equal(guid, actual);
        Assert.Equal(fromBytes.TryGetGuid(out Guid expected), fromGuid.TryGetGuid(out actual));
        Assert.Equal(expected, actual);

        UniqueId nonGuid = new UniqueId("urn:not-a-guid");
        Assert.False(nonGuid.TryGetGuid(out actual));
        Assert.Equal(Guid.Empty, actual);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(497)]
    [InlineData(510)]
    [InlineData(1024)]
    public static async Task BinaryWriter_CharEntitiesAsync_RoundTripWithAsyncOnlyStream(int length)
    {
        using AsyncOnlyWriteStream output = new AsyncOnlyWriteStream();
        XmlDictionaryWriter writer = await CreateAsyncWriter("binary", output, Encoding.UTF8);
        await writer.WriteStartElementAsync(null, "root", null);
        await InvokeWriterAsync(writer, "WriteStartAttributeAsync",
            new[] { typeof(string), typeof(string), typeof(string), typeof(CancellationToken) },
            null, "attribute", null, CancellationToken.None);
        await writer.WriteCharEntityAsync('<');
        await writer.WriteCharEntityAsync('0');
        await writer.WriteSurrogateCharEntityAsync('\ude42', '\ud83d');
        await InvokeWriterAsync(writer, "WriteEndAttributeAsync", new[] { typeof(CancellationToken) }, CancellationToken.None);
        string prefix = new string('x', length);
        await writer.WriteStringAsync(prefix);
        await writer.WriteCharEntityAsync('<');
        await writer.WriteCharEntityAsync('0');
        await writer.WriteCharEntityAsync('1');
        await writer.WriteCharEntityAsync('\u00e9');
        await writer.WriteSurrogateCharEntityAsync('\ude42', '\ud83d');
        await writer.WriteEndElementAsync();
        await writer.FlushAsync();
        Assert.True(output.AsyncWrites > 0);

        using XmlDictionaryReader reader = XmlDictionaryReader.CreateBinaryReader(output.Bytes, XmlDictionaryReaderQuotas.Max);
        reader.MoveToContent();
        Assert.Equal("<0\U0001f642", reader.GetAttribute("attribute"));
        Assert.Equal(prefix + "<01\u00e9\U0001f642", reader.ReadElementContentAsString());
        await writer.DisposeAsync();
    }

    [Fact]
    public static async Task BinaryWriter_CharEntityAsync_CancelsSuspendedFlush()
    {
        using AsyncOnlyWriteStream output = new AsyncOnlyWriteStream
        {
            WriteGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously)
        };
        XmlDictionaryWriter writer = await CreateAsyncWriter("binary", output, Encoding.UTF8);
        await writer.WriteStartElementAsync(null, "root", null);
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        Task pending = null;
        for (int i = 0; i < 1024; i++)
        {
            Task operation = InvokeWriterAsync(writer, "WriteCharEntityAsync",
                new[] { typeof(char), typeof(CancellationToken) }, '\u00e9', cancellation.Token);
            if (!operation.IsCompleted)
            {
                pending = operation;
                break;
            }
            await operation;
        }

        Assert.NotNull(pending);
        await output.WriteStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(cancellation.Token, output.LastWriteToken);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
    }

    [Theory]
    [InlineData("urn:uuid:01020304-0506-0708-090a-0b0c0d0e0f10")]
    [InlineData("urn:uuid:00000000-0000-0000-0000-000000000000")]
    [InlineData("urn:custom<&\u00e9")]
    public static void TextWriter_UniqueId_PreservesRepresentation(string value)
    {
        UniqueId identifier = new UniqueId(value);
        foreach (UniqueId candidate in identifier.IsGuid
            ? new[] { identifier, new UniqueId(new Guid(value.Substring(9))) }
            : new[] { identifier })
        {
            using MemoryStream output = new MemoryStream();
            using (XmlDictionaryWriter writer = XmlDictionaryWriter.CreateTextWriter(output, Encoding.UTF8, ownsStream: false))
            {
                writer.WriteStartElement("r");
                writer.WriteValue(candidate);
                writer.WriteEndElement();
            }
            using XmlReader reader = XmlReader.Create(new MemoryStream(output.ToArray()));
            reader.MoveToContent();
            Assert.Equal(value, reader.ReadElementContentAsString());
        }
    }

    [Theory]
    [InlineData(0, "\ufffe", "&#xFFFE;")]
    [InlineData(7, "\ufffe", "&#xFFFE;")]
    [InlineData(7, "\uffff", "&#xFFFF;")]
    [InlineData(7, "\ufffe\uffff&", "&#xFFFE;&#xFFFF;&amp;")]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods,
        "System.Xml.XmlUTF8NodeWriter", "System.Private.DataContractSerialization")]
    public static void Utf8NodeWriter_EscapedByteSlice_UsesSliceBounds(int offset, string value, string expected)
    {
        Type type = typeof(XmlDictionaryWriter).Assembly.GetType("System.Xml.XmlUTF8NodeWriter", throwOnError: true);
        object writer = Activator.CreateInstance(type);
        using MemoryStream output = new MemoryStream();
        type.GetMethod("SetOutput", new[] { typeof(Stream), typeof(bool), typeof(Encoding) })
            .Invoke(writer, new object[] { output, false, null });
        byte[] text = Encoding.UTF8.GetBytes(value);
        byte[] buffer = Enumerable.Repeat((byte)'!', offset + text.Length + 7).ToArray();
        text.CopyTo(buffer, offset);
        type.GetMethod("WriteEscapedText", new[] { typeof(byte[]), typeof(int), typeof(int) })
            .Invoke(writer, new object[] { buffer, offset, text.Length });
        type.GetMethod("Flush").Invoke(writer, null);
        Assert.Equal(expected, Encoding.UTF8.GetString(output.ToArray()));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods,
        "System.Xml.XmlCanonicalWriter", "System.Private.DataContractSerialization")]
    public static async Task CanonicalWriter_MemoryAttributeSlices_PreserveEscaping(bool bytes, bool escaped)
    {
        Type type = typeof(XmlDictionaryWriter).Assembly.GetType("System.Xml.XmlCanonicalWriter", throwOnError: true);
        object writer = Activator.CreateInstance(type);
        using MemoryStream output = new MemoryStream();
        type.GetMethod("SetOutput").Invoke(writer, new object[] { output, false, null });
        type.GetMethod("WriteStartElement", new[] { typeof(string), typeof(string) })
            .Invoke(writer, new object[] { "", "r" });
        type.GetMethod("WriteStartAttribute", new[] { typeof(string), typeof(string) })
            .Invoke(writer, new object[] { "", "a" });
        string value = escaped ? "h\u00e9\ud83d\ude42<&\"\r\t\n" : "h\u00e9\ud83d\ude42";
        Type memoryType = bytes ? typeof(ReadOnlyMemory<byte>) : typeof(ReadOnlyMemory<char>);
        object memory = bytes
            ? (object)new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes("prefix" + value + "suffix"), 6, Encoding.UTF8.GetByteCount(value))
            : ("prefix" + value + "suffix").AsMemory(6, value.Length);
        await (Task)type.GetMethod(escaped ? "WriteEscapedTextAsync" : "WriteTextAsync", new[] { memoryType })
            .Invoke(writer, new[] { memory });
        type.GetMethod("WriteEndAttribute").Invoke(writer, null);
        type.GetMethod("WriteEndStartElement").Invoke(writer, new object[] { true });
        type.GetMethod("Flush").Invoke(writer, null);
        string expected = escaped ? "h\u00e9\ud83d\ude42&lt;&amp;&quot;&#xD;&#x9;&#xA;" : value;
        Assert.Equal($"<r a=\"{expected}\"></r>", Encoding.UTF8.GetString(output.ToArray()));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(170)]
    [InlineData(513)]
    [InlineData(2049)]
    public static async Task XmlBaseWriter_LongNames_UseOnlyAsyncWrites(int length)
    {
        string name = "n" + new string('\u00e9', length);
        using AsyncOnlyWriteStream stream = new AsyncOnlyWriteStream();
        XmlDictionaryWriter writer = XmlDictionaryWriter.CreateTextWriter(stream, Encoding.UTF8, ownsStream: false);
        await writer.WriteStartElementAsync(null, name, null);
        await writer.WriteStartElementAsync(null, "child", null);
        await writer.WriteEndElementAsync();
        await writer.WriteEndElementAsync();
        await writer.FlushAsync();
        Assert.Equal($"<{name}><child/></{name}>", Encoding.UTF8.GetString(stream.Bytes));
        Assert.True(stream.AsyncWrites > 0);
    }

    [Theory]
    [InlineData("\ud800")]
    [InlineData("\udfff")]
    [InlineData("\ud801\udc37")]
    public static async Task AsyncWriter_PrimitiveDictionaryNames_PreserveUtf16Validation(string name)
    {
        XmlDictionary dictionary = new XmlDictionary();
        XmlDictionaryString root = dictionary.Add("root");
        XmlDictionaryString item = dictionary.Add(name);
        XmlDictionaryString ns = dictionary.Add("urn:primitive");
        Encoding encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
        using MemoryStream expected = new MemoryStream();
        XmlDictionaryWriter baseline = XmlDictionaryWriter.CreateTextWriter(expected, encoding, ownsStream: false);
        baseline.WriteStartElement(null, root, ns);
        Exception expectedError = Record.Exception(() =>
        {
            baseline.WriteStartElement(null, item, ns);
            baseline.WriteValue(42);
            baseline.WriteEndElement();
            baseline.WriteEndElement();
            baseline.Flush();
        });

        using AsyncOnlyWriteStream output = new AsyncOnlyWriteStream();
        XmlDictionaryWriter writer = XmlDictionaryWriter.CreateTextWriter(output, encoding, ownsStream: false);
        await InvokeWriterAsync(writer, "WriteStartElementAsync",
            new[] { typeof(string), typeof(XmlDictionaryString), typeof(XmlDictionaryString), typeof(CancellationToken) },
            null, root, ns, CancellationToken.None);
        MethodInfo method = typeof(XmlDictionaryWriter).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(candidate => candidate.Name == "WritePrimitiveElementAsync" &&
                candidate.IsGenericMethodDefinition && candidate.GetParameters().Length == 4);
        Func<string, XmlDictionaryString, XmlDictionaryString, object, Task> write =
            method.MakeGenericMethod(typeof(int)).CreateDelegate<Func<string, XmlDictionaryString, XmlDictionaryString, object, Task>>(writer);
        Exception actualError = await Record.ExceptionAsync(() => write(null, item, ns, 42));
        if (expectedError is not null)
        {
            Assert.IsType(expectedError.GetType(), actualError);
        }
        else
        {
            Assert.Null(actualError);
            await writer.WriteEndElementAsync();
            await writer.FlushAsync();
            Assert.Equal(expected.ToArray(), output.Bytes);
        }
    }

    private sealed class AsyncOnlyWriteStream : Stream
    {
        private readonly MemoryStream _output = new MemoryStream();

        public byte[] Bytes => _output.ToArray();
        public int AsyncWrites { get; private set; }
        public int AsyncFlushes { get; private set; }
        public int AsyncDisposals { get; private set; }
        public int SyncDisposals { get; private set; }
        public CancellationToken LastWriteToken { get; private set; }
        public CancellationToken LastFlushToken { get; private set; }
        public TaskCompletionSource<bool> WriteStarted { get; } = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> FlushStarted { get; } = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> WriteGate { get; set; }
        public TaskCompletionSource<bool> FlushGate { get; set; }
        public IOException WriteFailure { get; set; }
        public IOException FlushFailure { get; set; }
        public IOException DisposeFailure { get; set; }
        public override bool CanRead => false;
        public override bool CanWrite => true;
        public override bool CanSeek => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override int Read(byte[] buffer, int offset, int count) => throw new InvalidOperationException("Synchronous read.");
        public override void Write(byte[] buffer, int offset, int count) => throw new InvalidOperationException("Synchronous write.");
        public override void Write(ReadOnlySpan<byte> buffer) => throw new InvalidOperationException("Synchronous write.");
        public override void WriteByte(byte value) => throw new InvalidOperationException("Synchronous write.");
        public override void Flush() => throw new InvalidOperationException("Synchronous flush.");
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            LastWriteToken = cancellationToken;
            WriteStarted.TrySetResult(true);
            await Task.Yield();
            if (WriteGate is not null)
                await WriteGate.Task.WaitAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (WriteFailure is not null)
                throw WriteFailure;
            AsyncWrites++;
            _output.Write(buffer.Span);
        }

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            LastFlushToken = cancellationToken;
            FlushStarted.TrySetResult(true);
            await Task.Yield();
            if (FlushGate is not null)
                await FlushGate.Task.WaitAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (FlushFailure is not null)
                throw FlushFailure;
            AsyncFlushes++;
        }

        public override async ValueTask DisposeAsync()
        {
            await Task.Yield();
            AsyncDisposals++;
            if (DisposeFailure is not null)
                throw DisposeFailure;
            GC.SuppressFinalize(this);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                SyncDisposals++;
                _output.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    private static Encoding GetWriterEncoding(string name) => name switch
    {
        "utf16" => Encoding.Unicode,
        "utf16be" => Encoding.BigEndianUnicode,
        _ => new UTF8Encoding(false)
    };

    private static async Task<XmlDictionaryWriter> CreateAsyncWriter(string format, Stream stream, Encoding encoding,
        bool ownsStream = false, IXmlDictionary dictionary = null, bool indent = false, CancellationToken cancellationToken = default)
    {
        Type declaringType = format == "json" ? typeof(JsonReaderWriterFactory) : typeof(XmlDictionaryWriter);
        string methodName = format switch
        {
            "binary" => "CreateBinaryWriterAsync",
            "json" => "CreateJsonWriterAsync",
            _ => "CreateTextWriterAsync"
        };
        object[] arguments = format switch
        {
            "binary" => new object[] { stream, dictionary, null, ownsStream, cancellationToken },
            "json" => new object[] { stream, encoding, ownsStream, indent, "  ", cancellationToken },
            _ => new object[] { stream, encoding, ownsStream, cancellationToken }
        };
        MethodInfo method = declaringType.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return await (ValueTask<XmlDictionaryWriter>)method.Invoke(null, arguments);
    }

    private static Task InvokeWriterAsync(XmlDictionaryWriter writer, string methodName, Type[] parameterTypes, params object[] arguments)
    {
        MethodInfo method = typeof(XmlDictionaryWriter).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null, types: parameterTypes, modifiers: null);
        Assert.NotNull(method);
        object result;
        try
        {
            result = method.Invoke(writer, arguments);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
        return result switch
        {
            Task task => task,
            ValueTask task => task.AsTask(),
            _ => throw new InvalidOperationException("Unexpected async writer return type.")
        };
    }

    private static Task WriteValueAsync(XmlDictionaryWriter writer, object value, CancellationToken cancellationToken = default) =>
        InvokeWriterAsync(writer, "WriteValueAsync", new[] { typeof(object), typeof(CancellationToken) }, value, cancellationToken);

    private static Task WriteArrayAsync(XmlDictionaryWriter writer, Array array, int offset, int count, CancellationToken cancellationToken = default) =>
        InvokeWriterAsync(writer, "WriteArrayAsync", new[] { typeof(string), typeof(string), typeof(string), typeof(Array), typeof(int), typeof(int), typeof(CancellationToken) },
            null, "a", null, array, offset, count, cancellationToken);

    private static Task FlushWriterAsync(XmlDictionaryWriter writer, CancellationToken cancellationToken) =>
        InvokeWriterAsync(writer, "FlushAsync", new[] { typeof(CancellationToken) }, cancellationToken);

    private static Task CloseWriterAsync(XmlDictionaryWriter writer, CancellationToken cancellationToken) =>
        InvokeWriterAsync(writer, "CloseAsync", new[] { typeof(CancellationToken) }, cancellationToken);

    [Theory]
    [InlineData("utf8")]
    [InlineData("utf16")]
    [InlineData("utf16be")]
    public static async Task AsyncWriter_TextOperations_MatchSynchronousOutput(string encodingName)
    {
        Encoding encoding = GetWriterEncoding(encodingName);
        string prefix = "p" + new string('\u00e9', 170);
        string localName = "n" + new string('\u00e9', 513);
        string ns = "urn:" + new string('\u00e9', 600);
        string text = string.Concat(Enumerable.Repeat("a<&\"\t\r\n\u00e9\U0001f642", 160));
        byte[] bytes = GetByteArray(2051);
        XmlDictionary dictionary = new XmlDictionary();
        XmlDictionaryString dictionaryName = dictionary.Add(localName);
        XmlDictionaryString dictionaryNamespace = dictionary.Add(ns);
        using MemoryStream expected = new MemoryStream();
        using (XmlDictionaryWriter writer = XmlDictionaryWriter.CreateTextWriter(expected, encoding, ownsStream: false))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement(prefix, dictionaryName, dictionaryNamespace);
            writer.WriteAttributeString("xml", "lang", null, "en");
            writer.WriteAttributeString(null, "attribute", null, text);
            writer.WriteStartElement("child");
            writer.WriteString(text);
            writer.WriteChars(text.ToCharArray(), 1, text.Length - 1);
            writer.WriteBase64(bytes, 0, 1000);
            writer.WriteBase64(bytes, 1000, bytes.Length - 1000);
            writer.WriteString("tail");
            writer.WriteCharEntity('<');
            writer.WriteSurrogateCharEntity('\ude42', '\ud83d');
            writer.WriteCData("CDATA \u00e9\U0001f642");
            writer.WriteComment("comment");
            writer.WriteRaw("&gt;");
            writer.WriteBinHex(bytes, 0, bytes.Length);
            writer.WriteEndElement();
            writer.WriteFullEndElement();
            writer.WriteEndDocument();
        }
        using AsyncOnlyWriteStream output = new AsyncOnlyWriteStream();
        XmlDictionaryWriter asyncWriter = await CreateAsyncWriter("text", output, encoding);
        await asyncWriter.WriteStartDocumentAsync();
        await InvokeWriterAsync(asyncWriter, "WriteStartElementAsync",
            new[] { typeof(string), typeof(XmlDictionaryString), typeof(XmlDictionaryString), typeof(CancellationToken) },
            prefix, dictionaryName, dictionaryNamespace, CancellationToken.None);
        await asyncWriter.WriteAttributeStringAsync("xml", "lang", null, "en");
        await asyncWriter.WriteAttributeStringAsync(null, "attribute", null, text);
        await asyncWriter.WriteStartElementAsync(null, "child", null);
        await asyncWriter.WriteStringAsync(text);
        await asyncWriter.WriteCharsAsync(text.ToCharArray(), 1, text.Length - 1);
        await asyncWriter.WriteBase64Async(bytes, 0, 1000);
        await asyncWriter.WriteBase64Async(bytes, 1000, bytes.Length - 1000);
        await asyncWriter.WriteStringAsync("tail");
        await asyncWriter.WriteCharEntityAsync('<');
        await asyncWriter.WriteSurrogateCharEntityAsync('\ude42', '\ud83d');
        await asyncWriter.WriteCDataAsync("CDATA \u00e9\U0001f642");
        await asyncWriter.WriteCommentAsync("comment");
        await asyncWriter.WriteRawAsync("&gt;");
        await asyncWriter.WriteBinHexAsync(bytes, 0, bytes.Length);
        await asyncWriter.WriteEndElementAsync();
        await asyncWriter.WriteFullEndElementAsync();
        await asyncWriter.WriteEndDocumentAsync();
        await asyncWriter.DisposeAsync();
        Assert.Equal(expected.ToArray(), output.Bytes);
        Assert.True(output.AsyncWrites > 1);
        Assert.True(output.AsyncFlushes > 0);
        Assert.Equal(0, output.AsyncDisposals);
        using XmlDictionaryReader reader = XmlDictionaryReader.CreateTextReader(output.Bytes, 0, output.Bytes.Length, encoding, XmlDictionaryReaderQuotas.Max, null);
        Assert.Equal(localName, reader.MoveToContent() == XmlNodeType.Element ? reader.LocalName : null);
        Assert.Equal(ns, reader.NamespaceURI);
        Assert.Equal(text, reader.GetAttribute("attribute"));
    }

    [Fact]
    public static async Task AsyncWriter_NamespaceAndBase64Tail_DoNotUseSynchronousIO()
    {
        using AsyncOnlyWriteStream output = new AsyncOnlyWriteStream();
        XmlDictionaryWriter writer = XmlDictionaryWriter.CreateTextWriter(output, new UTF8Encoding(false), ownsStream: false);
        string ns = "urn:" + new string('x', 2049);
        await writer.WriteStartElementAsync("p", "root", ns);
        await writer.WriteStartElementAsync(null, "child", string.Empty);
        await writer.WriteBase64Async(new byte[] { 1 }, 0, 1);
        await writer.WriteEndElementAsync();
        await writer.WriteEndElementAsync();
        await writer.FlushAsync();
        Assert.Equal($"<p:root xmlns:p=\"{ns}\"><child>AQ==</child></p:root>", Encoding.UTF8.GetString(output.Bytes));
        await writer.DisposeAsync();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(400)]
    public static async Task AsyncWriter_BinaryBase64Segments_PreserveBufferBoundaries(int namespaceLength)
    {
        int[] lengths = { 0, 1, 2, 3, 4, 5, 8, 127, 128, 255, 256, 501, 502, 503, 504, 505, 506, 507, 508, 509, 510, 511, 512, 513, 1025 };
        using AsyncOnlyWriteStream output = new AsyncOnlyWriteStream();
        XmlDictionaryWriter writer = await CreateAsyncWriter("binary", output, Encoding.UTF8);
        await writer.WriteStartElementAsync(null, "root", "urn:" + new string('n', namespaceLength));
        foreach (int length in lengths)
        {
            byte[] bytes = GetByteArray(length);
            await writer.WriteStartElementAsync(null, "item", string.Empty);
            int first = Math.Min(1, length);
            await writer.WriteBase64Async(bytes, 0, first);
            await writer.WriteBase64Async(bytes, first, length - first);
            await writer.WriteEndElementAsync();
        }
        await writer.WriteEndElementAsync();
        await writer.FlushAsync();
        Assert.True(output.AsyncWrites > 1);
        using XmlDictionaryReader reader = XmlDictionaryReader.CreateBinaryReader(output.Bytes, XmlDictionaryReaderQuotas.Max);
        reader.ReadStartElement();
        foreach (int length in lengths)
            Assert.Equal(GetByteArray(length), reader.ReadElementContentAsBase64());
        reader.ReadEndElement();
        Assert.True(reader.EOF);
    }

    public static IEnumerable<object[]> AsyncBinaryValues()
    {
        object[] values =
        {
            false, true, 0, 1, -128, 127, -129, 32767, 32768, int.MaxValue, long.MinValue, ulong.MaxValue,
            1.23456788f, float.NaN, float.NegativeInfinity, 1.0 / 3.0, double.PositiveInfinity,
            decimal.MinValue, new decimal(0x20212223, 0x10111213, 0x01020304, true, 0x1b),
            new DateTime(2022, 8, 26, 12, 34, 56, DateTimeKind.Utc),
            TimeSpan.MaxValue, new Guid("01020304-0506-0708-090a-0b0c0d0e0f10"),
            new UniqueId(new Guid("01020304-0506-0708-090a-0b0c0d0e0f10")),
            new UniqueId("urn:" + new string('\u00e9', 513)),
            new string('\u00e9', 85), new string('\u4e00', 86), new string('\u4e00', 2049)
        };
        foreach (object value in values)
            yield return new[] { value };
    }

    [Theory]
    [MemberData(nameof(AsyncBinaryValues))]
    public static async Task AsyncWriter_BinaryValues_PreserveTypedBytes(object value)
    {
        using MemoryStream expected = new MemoryStream();
        using (XmlDictionaryWriter writer = XmlDictionaryWriter.CreateBinaryWriter(expected, null, null, ownsStream: false))
        {
            writer.WriteStartElement("value");
            writer.WriteValue(value);
            writer.WriteEndElement();
        }
        using AsyncOnlyWriteStream output = new AsyncOnlyWriteStream();
        XmlDictionaryWriter asyncWriter = await CreateAsyncWriter("binary", output, Encoding.UTF8);
        await asyncWriter.WriteStartElementAsync(null, "value", null);
        await WriteValueAsync(asyncWriter, value);
        await asyncWriter.WriteEndElementAsync();
        await asyncWriter.DisposeAsync();
        Assert.Equal(expected.ToArray(), output.Bytes);
        using XmlDictionaryReader expectedReader = XmlDictionaryReader.CreateBinaryReader(expected.ToArray(), XmlDictionaryReaderQuotas.Max);
        using XmlDictionaryReader actualReader = XmlDictionaryReader.CreateBinaryReader(output.Bytes, XmlDictionaryReaderQuotas.Max);
        expectedReader.ReadStartElement("value");
        actualReader.ReadStartElement("value");
        Assert.Equal(expectedReader.ValueType, actualReader.ValueType);
        Assert.Equal(expectedReader.ReadContentAsString(), actualReader.ReadContentAsString());
    }

    public static IEnumerable<object[]> AsyncBinaryArrays()
    {
        const int length = 205;
        Array[] arrays =
        {
            Enumerable.Range(0, length).Select(i => i % 2 == 0).ToArray(),
            Enumerable.Range(0, length).Select(i => (short)(i * 127 - 16384)).ToArray(),
            Enumerable.Range(0, length).Select(i => i * 65537 - 65536).ToArray(),
            Enumerable.Range(0, length).Select(i => long.MinValue + i * 65537L).ToArray(),
            Enumerable.Range(0, length).Select(i => i / 3.0f).ToArray(),
            Enumerable.Range(0, length).Select(i => i / 3.0).ToArray(),
            Enumerable.Range(0, length).Select(i => i / 3.0m).ToArray(),
            Enumerable.Range(0, length).Select(i => new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(i)).ToArray(),
            Enumerable.Range(0, length).Select(i => new Guid(i, 2, 3, new byte[] { 4, 5, 6, 7, 8, 9, 10, 11 })).ToArray(),
            Enumerable.Range(0, length).Select(i => TimeSpan.FromTicks(long.MinValue + i)).ToArray()
        };
        foreach (Array array in arrays)
            yield return new object[] { array };
    }

    private static void WriteArray(XmlDictionaryWriter writer, Array array, int offset, int count)
    {
        switch (array)
        {
            case bool[] values: writer.WriteArray(null, "a", null, values, offset, count); break;
            case short[] values: writer.WriteArray(null, "a", null, values, offset, count); break;
            case int[] values: writer.WriteArray(null, "a", null, values, offset, count); break;
            case long[] values: writer.WriteArray(null, "a", null, values, offset, count); break;
            case float[] values: writer.WriteArray(null, "a", null, values, offset, count); break;
            case double[] values: writer.WriteArray(null, "a", null, values, offset, count); break;
            case decimal[] values: writer.WriteArray(null, "a", null, values, offset, count); break;
            case DateTime[] values: writer.WriteArray(null, "a", null, values, offset, count); break;
            case Guid[] values: writer.WriteArray(null, "a", null, values, offset, count); break;
            case TimeSpan[] values: writer.WriteArray(null, "a", null, values, offset, count); break;
            default: throw new InvalidOperationException();
        }
    }

    private static int ReadArray(XmlDictionaryReader reader, Array array)
    {
        return array switch
        {
            bool[] values => reader.ReadArray("a", string.Empty, values, 0, values.Length),
            short[] values => reader.ReadArray("a", string.Empty, values, 0, values.Length),
            int[] values => reader.ReadArray("a", string.Empty, values, 0, values.Length),
            long[] values => reader.ReadArray("a", string.Empty, values, 0, values.Length),
            float[] values => reader.ReadArray("a", string.Empty, values, 0, values.Length),
            double[] values => reader.ReadArray("a", string.Empty, values, 0, values.Length),
            decimal[] values => reader.ReadArray("a", string.Empty, values, 0, values.Length),
            DateTime[] values => reader.ReadArray("a", string.Empty, values, 0, values.Length),
            Guid[] values => reader.ReadArray("a", string.Empty, values, 0, values.Length),
            TimeSpan[] values => reader.ReadArray("a", string.Empty, values, 0, values.Length),
            _ => throw new InvalidOperationException()
        };
    }

    [Theory]
    [MemberData(nameof(AsyncBinaryArrays))]
    public static async Task AsyncWriter_BinaryArrays_PreservePackedRepresentation(Array array)
    {
        const int offset = 1;
        int count = array.Length - 2;
        using MemoryStream expected = new MemoryStream();
        using (XmlDictionaryWriter writer = XmlDictionaryWriter.CreateBinaryWriter(expected, null, null, ownsStream: false))
        {
            writer.WriteStartElement("root");
            WriteArray(writer, array, offset, count);
            writer.WriteEndElement();
        }
        using AsyncOnlyWriteStream output = new AsyncOnlyWriteStream();
        XmlDictionaryWriter asyncWriter = await CreateAsyncWriter("binary", output, Encoding.UTF8);
        await asyncWriter.WriteStartElementAsync(null, "root", null);
        await WriteArrayAsync(asyncWriter, array, offset, count);
        await asyncWriter.WriteEndElementAsync();
        await asyncWriter.DisposeAsync();
        Assert.Equal(expected.ToArray(), output.Bytes);
        using XmlDictionaryReader reader = XmlDictionaryReader.CreateBinaryReader(output.Bytes, XmlDictionaryReaderQuotas.Max);
        reader.ReadStartElement("root");
        Assert.True(reader.IsStartArray(out Type elementType));
        Assert.Equal(array.GetType().GetElementType(), elementType);
        Array actual = Array.CreateInstance(elementType, count);
        Assert.Equal(count, ReadArray(reader, actual));
        Assert.Equal(array.Cast<object>().Skip(offset).Take(count), actual.Cast<object>());
    }

    [Theory]
    [InlineData("utf8", false)]
    [InlineData("utf16", false)]
    [InlineData("utf16be", false)]
    [InlineData("utf8", true)]
    [InlineData("utf16", true)]
    [InlineData("utf16be", true)]
    public static async Task AsyncWriter_JsonMappedNamesAndChunks_MatchSynchronousOutput(string encodingName, bool indent)
    {
        Encoding encoding = GetWriterEncoding(encodingName);
        string mappedName = "mapped /\"\n" + new string('\u00e9', 513);
        string text = string.Concat(Enumerable.Repeat("a\"/\\\t\u0085\u2028\u2029\U0001f642", 170));
        string serverType = "type:" + new string('\u00e9', 600);
        byte[] bytes = GetByteArray(1025);
        using MemoryStream expected = new MemoryStream();
        using (XmlDictionaryWriter writer = JsonReaderWriterFactory.CreateJsonWriter(expected, encoding, ownsStream: false, indent, "  "))
        {
            writer.WriteStartElement("root");
            writer.WriteAttributeString("type", "object");
            writer.WriteAttributeString("__type", serverType);
            writer.WriteStartElement("p", "item", "item");
            writer.WriteAttributeString("item", mappedName);
            writer.WriteString(text);
            writer.WriteEndElement();
            writer.WriteStartElement("array");
            writer.WriteAttributeString("type", "array");
            writer.WriteStartElement("item");
            writer.WriteAttributeString("type", "number");
            writer.WriteValue(123456789L);
            writer.WriteEndElement();
            writer.WriteStartElement("item");
            writer.WriteAttributeString("type", "boolean");
            writer.WriteValue(true);
            writer.WriteEndElement();
            writer.WriteStartElement("item");
            writer.WriteBase64(bytes, 0, bytes.Length);
            writer.WriteEndElement();
            writer.WriteStartElement("item");
            writer.WriteAttributeString("type", "null");
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndElement();
        }
        using AsyncOnlyWriteStream output = new AsyncOnlyWriteStream();
        XmlDictionaryWriter asyncWriter = await CreateAsyncWriter("json", output, encoding, indent: indent);
        await asyncWriter.WriteStartElementAsync(null, "root", null);
        await asyncWriter.WriteAttributeStringAsync(null, "type", null, "object");
        await asyncWriter.WriteAttributeStringAsync(null, "__type", null, serverType);
        await asyncWriter.WriteStartElementAsync("p", "item", "item");
        await asyncWriter.WriteAttributeStringAsync(null, "item", null, mappedName);
        await asyncWriter.WriteStringAsync(text.Substring(0, 300));
        await asyncWriter.WriteCharsAsync(text.ToCharArray(), 300, text.Length - 300);
        await asyncWriter.WriteEndElementAsync();
        await asyncWriter.WriteStartElementAsync(null, "array", null);
        await asyncWriter.WriteAttributeStringAsync(null, "type", null, "array");
        await asyncWriter.WriteStartElementAsync(null, "item", null);
        await asyncWriter.WriteAttributeStringAsync(null, "type", null, "number");
        await WriteValueAsync(asyncWriter, 123456789L);
        await asyncWriter.WriteEndElementAsync();
        await asyncWriter.WriteStartElementAsync(null, "item", null);
        await asyncWriter.WriteAttributeStringAsync(null, "type", null, "boolean");
        await WriteValueAsync(asyncWriter, true);
        await asyncWriter.WriteEndElementAsync();
        await asyncWriter.WriteStartElementAsync(null, "item", null);
        await asyncWriter.WriteBase64Async(bytes, 0, bytes.Length);
        await asyncWriter.WriteEndElementAsync();
        await asyncWriter.WriteStartElementAsync(null, "item", null);
        await asyncWriter.WriteAttributeStringAsync(null, "type", null, "null");
        await asyncWriter.WriteEndElementAsync();
        await asyncWriter.WriteEndElementAsync();
        await asyncWriter.WriteEndElementAsync();
        await asyncWriter.DisposeAsync();
        Assert.Equal(expected.ToArray(), output.Bytes);
        Assert.True(output.AsyncWrites > 1);
        Assert.True(output.AsyncFlushes > 0);
        using XmlDictionaryReader expectedReader = JsonReaderWriterFactory.CreateJsonReader(expected.ToArray(), 0, (int)expected.Length,
            encoding, XmlDictionaryReaderQuotas.Max, null);
        using XmlDictionaryReader actualReader = JsonReaderWriterFactory.CreateJsonReader(output.Bytes, 0, output.Bytes.Length,
            encoding, XmlDictionaryReaderQuotas.Max, null);
        expectedReader.MoveToContent();
        actualReader.MoveToContent();
        Assert.Equal(expectedReader.ReadOuterXml(), actualReader.ReadOuterXml());
    }

    [Fact]
    public static async Task AsyncWriter_BinaryDictionaryNamesAndAttributeChunks_PreserveKeys()
    {
        XmlDictionary dictionary = new XmlDictionary();
        XmlDictionaryString root = dictionary.Add("root");
        XmlDictionaryString ns = dictionary.Add("urn:" + new string('\u00e9', 513));
        XmlDictionaryString name = dictionary.Add("n" + new string('\u00e9', 513));
        XmlDictionaryString value = dictionary.Add(new string('\u4e00', 513));
        XmlDictionaryString qName = dictionary.Add("qualified");
        XmlDictionaryString qNs = dictionary.Add("urn:qualified");
        byte[] bytes = GetByteArray(2051);
        using MemoryStream expected = new MemoryStream();
        using (XmlDictionaryWriter writer = XmlDictionaryWriter.CreateBinaryWriter(expected, dictionary, null, ownsStream: false))
        {
            writer.WriteStartElement("p", root, ns);
            writer.WriteXmlnsAttribute("q", qNs);
            writer.WriteStartAttribute(null, name, XmlDictionaryString.Empty);
            writer.WriteString(value);
            writer.WriteString("suffix");
            writer.WriteEndAttribute();
            writer.WriteStartElement(null, name, XmlDictionaryString.Empty);
            writer.WriteQualifiedName(qName, qNs);
            writer.WriteValue(new object[] { 42, true, string.Empty, "tail" });
            writer.WriteBase64(bytes, 0, 1000);
            writer.WriteBase64(bytes, 1000, bytes.Length - 1000);
            writer.WriteEndElement();
            writer.WriteEndElement();
        }
        using AsyncOnlyWriteStream output = new AsyncOnlyWriteStream();
        XmlDictionaryWriter asyncWriter = await CreateAsyncWriter("binary", output, Encoding.UTF8, dictionary: dictionary);
        await InvokeWriterAsync(asyncWriter, "WriteStartElementAsync",
            new[] { typeof(string), typeof(XmlDictionaryString), typeof(XmlDictionaryString), typeof(CancellationToken) },
            "p", root, ns, CancellationToken.None);
        await InvokeWriterAsync(asyncWriter, "WriteXmlnsAttributeAsync", new[] { typeof(string), typeof(XmlDictionaryString), typeof(CancellationToken) },
            "q", qNs, CancellationToken.None);
        await InvokeWriterAsync(asyncWriter, "WriteStartAttributeAsync",
            new[] { typeof(string), typeof(XmlDictionaryString), typeof(XmlDictionaryString), typeof(CancellationToken) },
            null, name, XmlDictionaryString.Empty, CancellationToken.None);
        await InvokeWriterAsync(asyncWriter, "WriteStringAsync", new[] { typeof(XmlDictionaryString), typeof(CancellationToken) }, value, CancellationToken.None);
        await asyncWriter.WriteStringAsync("suffix");
        await InvokeWriterAsync(asyncWriter, "WriteEndAttributeAsync", new[] { typeof(CancellationToken) }, CancellationToken.None);
        await InvokeWriterAsync(asyncWriter, "WriteStartElementAsync",
            new[] { typeof(string), typeof(XmlDictionaryString), typeof(XmlDictionaryString), typeof(CancellationToken) },
            null, name, XmlDictionaryString.Empty, CancellationToken.None);
        await InvokeWriterAsync(asyncWriter, "WriteQualifiedNameAsync", new[] { typeof(XmlDictionaryString), typeof(XmlDictionaryString), typeof(CancellationToken) },
            qName, qNs, CancellationToken.None);
        await WriteValueAsync(asyncWriter, new object[] { 42, true, string.Empty, "tail" });
        await asyncWriter.WriteBase64Async(bytes, 0, 1000);
        await asyncWriter.WriteBase64Async(bytes, 1000, bytes.Length - 1000);
        await asyncWriter.WriteEndElementAsync();
        await asyncWriter.WriteEndElementAsync();
        await asyncWriter.DisposeAsync();
        Assert.Equal(expected.ToArray(), output.Bytes);
        using XmlDictionaryReader expectedReader = XmlDictionaryReader.CreateBinaryReader(expected.ToArray(), 0, (int)expected.Length,
            dictionary, XmlDictionaryReaderQuotas.Max);
        using XmlDictionaryReader actualReader = XmlDictionaryReader.CreateBinaryReader(output.Bytes, 0, output.Bytes.Length,
            dictionary, XmlDictionaryReaderQuotas.Max);
        expectedReader.MoveToContent();
        actualReader.MoveToContent();
        Assert.Equal(expectedReader.ReadOuterXml(), actualReader.ReadOuterXml());
    }

    [Theory]
    [InlineData("text")]
    [InlineData("binary")]
    [InlineData("json")]
    public static async Task AsyncWriter_TypedOverloadsAndCompoundAttributes_AreTokenAware(string format)
    {
        XmlDictionary dictionary = new XmlDictionary();
        XmlDictionaryString attributeName = dictionary.Add(format == "json" ? "type" : "attribute");
        string attributeValue = format == "json" ? "object" : new string('\u00e9', 513);
        object[] values =
        {
            true, int.MaxValue, long.MinValue, ulong.MaxValue, 1.25f, 1.0 / 3.0, 1.25m,
            new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTimeOffset(2022, 1, 1, 0, 0, 0, TimeSpan.FromHours(2)),
            new Guid("01020304-0506-0708-090a-0b0c0d0e0f10"),
            TimeSpan.FromDays(1), new UniqueId("urn:identifier"), "text", dictionary.Add("dictionary text")
        };
        using MemoryStream expected = new MemoryStream();
        using (XmlDictionaryWriter syncWriter = CreateSyncWriter(format, expected))
        {
            syncWriter.WriteStartElement("root");
            syncWriter.WriteAttributeString(null, attributeName, null, attributeValue);
            foreach (object value in values)
            {
                syncWriter.WriteStartElement("value");
                if (format == "json" && value is bool or int or long or ulong or float or double or decimal)
                    syncWriter.WriteAttributeString("type", value is bool ? "boolean" : "number");
                syncWriter.WriteValue(value);
                syncWriter.WriteEndElement();
            }
            syncWriter.WriteEndElement();
        }
        using AsyncOnlyWriteStream output = new AsyncOnlyWriteStream();
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        XmlDictionaryWriter writer = await CreateAsyncWriter(format, output, new UTF8Encoding(false));
        await writer.WriteStartElementAsync(null, "root", null);
        await InvokeWriterAsync(writer, "WriteAttributeStringAsync",
            new[] { typeof(string), typeof(XmlDictionaryString), typeof(XmlDictionaryString), typeof(string), typeof(CancellationToken) },
            null, attributeName, null, attributeValue, cancellation.Token);
        foreach (object value in values)
        {
            await writer.WriteStartElementAsync(null, "value", null);
            if (format == "json" && value is bool or int or long or ulong or float or double or decimal)
                await InvokeWriterAsync(writer, "WriteAttributeStringAsync",
                    new[] { typeof(string), typeof(string), typeof(string), typeof(string), typeof(CancellationToken) },
                    null, "type", null, value is bool ? "boolean" : "number", cancellation.Token);
            await InvokeWriterAsync(writer, "WriteValueAsync", new[] { value.GetType(), typeof(CancellationToken) }, value, cancellation.Token);
            await writer.WriteEndElementAsync();
        }
        await writer.WriteEndElementAsync();
        await FlushWriterAsync(writer, cancellation.Token);
        Assert.Equal(cancellation.Token, output.LastWriteToken);
        Assert.Equal(cancellation.Token, output.LastFlushToken);
        Assert.Equal(expected.ToArray(), output.Bytes);
        await writer.DisposeAsync();
    }

    [Theory]
    [InlineData("text", false)]
    [InlineData("binary", false)]
    [InlineData("text", true)]
    [InlineData("binary", true)]
    public static async Task AsyncWriter_QualifiedNameAttribute_RegistersNamespaceAsynchronously(string format, bool dictionaryNames)
    {
        XmlDictionary dictionary = new XmlDictionary();
        XmlDictionaryString attributeName = dictionary.Add("type");
        XmlDictionaryString attributeNamespace = dictionary.Add("urn:attribute");
        XmlDictionaryString valueName = dictionary.Add("Value");
        XmlDictionaryString valueNamespace = dictionary.Add("urn:value");
        string text = new string('\u00e9', 2049);
        using MemoryStream expected = new MemoryStream();
        using (XmlDictionaryWriter syncWriter = format == "binary"
            ? XmlDictionaryWriter.CreateBinaryWriter(expected, dictionary, null, ownsStream: false)
            : CreateSyncWriter(format, expected))
        {
            syncWriter.WriteStartElement("root");
            if (dictionaryNames)
            {
                syncWriter.WriteStartAttribute("i", attributeName, attributeNamespace);
                syncWriter.WriteQualifiedName(valueName, valueNamespace);
            }
            else
            {
                syncWriter.WriteStartAttribute("i", attributeName.Value, attributeNamespace.Value);
                syncWriter.WriteQualifiedName(valueName.Value, valueNamespace.Value);
            }
            syncWriter.WriteEndAttribute();
            syncWriter.WriteString(text);
            syncWriter.WriteEndElement();
        }
        using AsyncOnlyWriteStream output = new AsyncOnlyWriteStream();
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        XmlDictionaryWriter writer = await CreateAsyncWriter(format, output, new UTF8Encoding(false), dictionary: dictionary);
        await writer.WriteStartElementAsync(null, "root", null);
        if (dictionaryNames)
            await InvokeWriterAsync(writer, "WriteAttributeQualifiedNameAsync",
                new[] { typeof(string), typeof(XmlDictionaryString), typeof(XmlDictionaryString), typeof(XmlDictionaryString), typeof(XmlDictionaryString), typeof(CancellationToken) },
                "i", attributeName, attributeNamespace, valueName, valueNamespace, cancellation.Token);
        else
            await InvokeWriterAsync(writer, "WriteAttributeQualifiedNameAsync",
                new[] { typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(CancellationToken) },
                "i", attributeName.Value, attributeNamespace.Value, valueName.Value, valueNamespace.Value, cancellation.Token);
        await InvokeWriterAsync(writer, "WriteStringAsync", new[] { typeof(string), typeof(CancellationToken) }, text, cancellation.Token);
        await writer.WriteEndElementAsync();
        await FlushWriterAsync(writer, cancellation.Token);
        Assert.Equal(expected.ToArray(), output.Bytes);
        Assert.True(output.AsyncWrites > 1);
        await writer.DisposeAsync();
        using XmlDictionaryReader reader = format == "binary"
            ? XmlDictionaryReader.CreateBinaryReader(output.Bytes, 0, output.Bytes.Length, dictionary, XmlDictionaryReaderQuotas.Max)
            : XmlDictionaryReader.CreateTextReader(output.Bytes, XmlDictionaryReaderQuotas.Max);
        reader.MoveToContent();
        string qualifiedName = reader.GetAttribute("type", "urn:attribute");
        Assert.NotNull(qualifiedName);
        string[] parts = qualifiedName.Split(':');
        Assert.Equal(2, parts.Length);
        Assert.Equal("Value", parts[1]);
        Assert.Equal("urn:value", reader.LookupNamespace(parts[0]));
    }

    private static XmlDictionaryWriter CreateSyncWriter(string format, Stream stream, bool ownsStream = false) => format switch
    {
        "binary" => XmlDictionaryWriter.CreateBinaryWriter(stream, null, null, ownsStream),
        "json" => JsonReaderWriterFactory.CreateJsonWriter(stream, new UTF8Encoding(false), ownsStream),
        _ => XmlDictionaryWriter.CreateTextWriter(stream, new UTF8Encoding(false), ownsStream)
    };

    [Theory]
    [InlineData("text")]
    [InlineData("binary")]
    [DynamicDependency("WritePrimitiveValueAsync``1(System.Object)", "System.Xml.XmlDictionaryWriter", "System.Private.DataContractSerialization")]
    public static async Task AsyncWriter_TaggedSmallScalars_PreserveCanonicalRepresentation(string format)
    {
        object[] values = { 'Z', byte.MaxValue, sbyte.MinValue, short.MinValue, ushort.MaxValue, uint.MaxValue };
        using MemoryStream expected = new MemoryStream();
        using MemoryStream expectedCanonical = new MemoryStream();
        using (XmlDictionaryWriter writer = CreateSyncWriter(format, expected))
        {
            writer.WriteStartElement("root");
            writer.StartCanonicalization(expectedCanonical, includeComments: false, inclusivePrefixes: null);
            foreach (object value in values)
            {
                writer.WriteStartElement("item");
                writer.WriteStartAttribute("value");
                WriteValue(writer, value);
                writer.WriteEndAttribute();
                WriteValue(writer, value);
                writer.WriteEndElement();
            }
            writer.EndCanonicalization();
            writer.WriteEndElement();
        }

        using AsyncOnlyWriteStream output = new AsyncOnlyWriteStream();
        using AsyncOnlyWriteStream canonical = new AsyncOnlyWriteStream();
        XmlDictionaryWriter asynchronous = await CreateAsyncWriter(format, output, new UTF8Encoding(false));
        await asynchronous.WriteStartElementAsync(null, "root", null);
        await InvokeWriterAsync(asynchronous, "StartCanonicalizationAsync",
            new[] { typeof(Stream), typeof(bool), typeof(string[]), typeof(CancellationToken) },
            canonical, false, null, CancellationToken.None);
        MethodInfo writePrimitive = typeof(XmlDictionaryWriter).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(method => method.Name == "WritePrimitiveValueAsync" && method.IsGenericMethodDefinition && method.GetParameters().Length == 1);
        foreach (object value in values)
        {
            MethodInfo write = writePrimitive.MakeGenericMethod(value.GetType());
            await asynchronous.WriteStartElementAsync(null, "item", null);
            await InvokeWriterAsync(asynchronous, "WriteStartAttributeAsync",
                new[] { typeof(string), typeof(string), typeof(string), typeof(CancellationToken) },
                null, "value", null, CancellationToken.None);
            await ((Task)write.Invoke(asynchronous, new[] { value })).ConfigureAwait(false);
            await InvokeWriterAsync(asynchronous, "WriteEndAttributeAsync", new[] { typeof(CancellationToken) }, CancellationToken.None);
            await ((Task)write.Invoke(asynchronous, new[] { value })).ConfigureAwait(false);
            await asynchronous.WriteEndElementAsync();
        }
        await InvokeWriterAsync(asynchronous, "EndCanonicalizationAsync", new[] { typeof(CancellationToken) }, CancellationToken.None);
        await asynchronous.WriteEndElementAsync();
        await asynchronous.DisposeAsync();
        Assert.Equal(expected.ToArray(), output.Bytes);
        Assert.Equal(expectedCanonical.ToArray(), canonical.Bytes);
        Assert.True(output.AsyncWrites > 0);
        Assert.True(canonical.AsyncWrites > 0);

        static void WriteValue(XmlDictionaryWriter writer, object value)
        {
            switch (value)
            {
                case char item: writer.WriteValue((int)item); break;
                case byte item: writer.WriteValue(item); break;
                case sbyte item: writer.WriteValue(item); break;
                case short item: writer.WriteValue(item); break;
                case ushort item: writer.WriteValue(item); break;
                case uint item: writer.WriteValue((long)item); break;
                default: throw new InvalidOperationException();
            }
        }
    }

    [Theory]
    [InlineData("text", false)]
    [InlineData("binary", false)]
    [InlineData("text", true)]
    [InlineData("binary", true)]
    public static async Task AsyncWriter_Canonicalization_MatchesSynchronousOutput(string format, bool includeComments)
    {
        string attribute = string.Concat(Enumerable.Repeat("\"<&\t\r\n\u4e00\U0001f642", 100));
        string text = new string('\u4e00', 2049) + "\r\n\U0001f642";
        byte[] bytes = GetByteArray(2051);
        int[] array = { -1, 0, 1, int.MaxValue };
        using MemoryStream expected = new MemoryStream();
        using MemoryStream expectedCanonical = new MemoryStream();
        using (XmlDictionaryWriter writer = CreateSyncWriter(format, expected))
        {
            writer.WriteStartElement("p", "root", "urn:root");
            writer.WriteXmlnsAttribute("z", "urn:z");
            writer.StartCanonicalization(expectedCanonical, includeComments, new[] { "z" });
            writer.WriteStartElement("p", "child", "urn:root");
            writer.WriteAttributeString("z", "last", "urn:z", attribute);
            writer.WriteAttributeString(null, "first", null, "value");
            writer.WriteString(text);
            writer.WriteCData("CDATA <&>\r\n");
            writer.WriteComment("comment");
            writer.WriteBase64(bytes, 0, 1000);
            writer.WriteBase64(bytes, 1000, bytes.Length - 1000);
            writer.WriteValue(1.0 / 3.0);
            writer.WriteValue(42);
            writer.WriteArray(null, "a", null, array, 0, array.Length);
            writer.WriteEndElement();
            writer.EndCanonicalization();
            writer.WriteEndElement();
        }
        using AsyncOnlyWriteStream output = new AsyncOnlyWriteStream();
        using AsyncOnlyWriteStream canonical = new AsyncOnlyWriteStream();
        XmlDictionaryWriter asyncWriter = await CreateAsyncWriter(format, output, new UTF8Encoding(false));
        await asyncWriter.WriteStartElementAsync("p", "root", "urn:root");
        await InvokeWriterAsync(asyncWriter, "WriteXmlnsAttributeAsync", new[] { typeof(string), typeof(string), typeof(CancellationToken) },
            "z", "urn:z", CancellationToken.None);
        await InvokeWriterAsync(asyncWriter, "StartCanonicalizationAsync", new[] { typeof(Stream), typeof(bool), typeof(string[]), typeof(CancellationToken) },
            canonical, includeComments, new[] { "z" }, CancellationToken.None);
        await asyncWriter.WriteStartElementAsync("p", "child", "urn:root");
        await asyncWriter.WriteAttributeStringAsync("z", "last", "urn:z", attribute);
        await asyncWriter.WriteAttributeStringAsync(null, "first", null, "value");
        await asyncWriter.WriteStringAsync(text);
        await asyncWriter.WriteCDataAsync("CDATA <&>\r\n");
        await asyncWriter.WriteCommentAsync("comment");
        await asyncWriter.WriteBase64Async(bytes, 0, 1000);
        await asyncWriter.WriteBase64Async(bytes, 1000, bytes.Length - 1000);
        await WriteValueAsync(asyncWriter, 1.0 / 3.0);
        await InvokeWriterAsync(asyncWriter, "WriteValueAsync", new[] { typeof(int), typeof(CancellationToken) }, 42, CancellationToken.None);
        await WriteArrayAsync(asyncWriter, array, 0, array.Length);
        await asyncWriter.WriteEndElementAsync();
        await InvokeWriterAsync(asyncWriter, "EndCanonicalizationAsync", new[] { typeof(CancellationToken) }, CancellationToken.None);
        await asyncWriter.WriteEndElementAsync();
        await asyncWriter.DisposeAsync();
        Assert.Equal(expected.ToArray(), output.Bytes);
        Assert.Equal(expectedCanonical.ToArray(), canonical.Bytes);
        Assert.True(output.AsyncWrites > 1);
        Assert.True(canonical.AsyncWrites > 1);
        Assert.True(canonical.AsyncFlushes > 0);
        Assert.Equal(0, canonical.AsyncDisposals);
    }

    [Theory]
    [InlineData("text", false)]
    [InlineData("binary", false)]
    [InlineData("json", false)]
    [InlineData("text", true)]
    [InlineData("binary", true)]
    [InlineData("json", true)]
    public static async Task AsyncWriter_CancellationAndGuards_PreserveBufferedOutput(string format, bool cancelFlush)
    {
        using AsyncOnlyWriteStream output = new AsyncOnlyWriteStream();
        XmlDictionaryWriter writer = await CreateAsyncWriter(format, output, new UTF8Encoding(false));
        await writer.WriteStartElementAsync(null, "root", null);
        await writer.WriteStringAsync("before");
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        if (cancelFlush)
            output.FlushGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        else
            output.WriteGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task pending = FlushWriterAsync(writer, cancellation.Token);
        await (cancelFlush ? output.FlushStarted.Task : output.WriteStarted.Task);
        Assert.False(pending.IsCompleted);
        Assert.Throws<InvalidOperationException>(() => writer.WriteString("overlap"));
        Assert.Throws<InvalidOperationException>(() => writer.Close());
        await Assert.ThrowsAsync<InvalidOperationException>(() => writer.WriteStringAsync("overlap"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => WriteValueAsync(writer, 42));
        cancellation.Cancel();
        OperationCanceledException error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(cancellation.Token, cancelFlush ? output.LastFlushToken : output.LastWriteToken);
        output.WriteGate = null;
        output.FlushGate = null;
        await writer.WriteStringAsync("after");
        await writer.WriteEndElementAsync();
        await writer.DisposeAsync();
        using MemoryStream expected = new MemoryStream();
        using (XmlDictionaryWriter syncWriter = CreateSyncWriter(format, expected))
        {
            syncWriter.WriteStartElement("root");
            syncWriter.WriteString("before");
            if (cancelFlush)
                syncWriter.Flush();
            syncWriter.WriteString("after");
            syncWriter.WriteEndElement();
        }
        Assert.Equal(expected.ToArray(), output.Bytes);
        Assert.Equal(CancellationToken.None, output.LastFlushToken);
    }

    [Theory]
    [InlineData("text")]
    [InlineData("binary")]
    [InlineData("json")]
    public static async Task AsyncWriter_PreCanceledOperation_DoesNotMutateState(string format)
    {
        using AsyncOnlyWriteStream output = new AsyncOnlyWriteStream();
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        XmlDictionaryWriter writer = await CreateAsyncWriter(format, output, new UTF8Encoding(false));
        Task operation = WriteValueAsync(writer, 42, cancellation.Token);
        Assert.True(operation.IsCanceled);
        Assert.False(operation.IsFaulted);
        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.Equal(WriteState.Start, writer.WriteState);
        Assert.Empty(output.Bytes);
        await writer.WriteStartElementAsync(null, "root", null);
        await writer.WriteStringAsync("value");
        await writer.DisposeAsync();
        Assert.NotEmpty(output.Bytes);
        Assert.Equal(CancellationToken.None, output.LastWriteToken);
    }

    [Theory]
    [InlineData("text", false)]
    [InlineData("binary", false)]
    [InlineData("json", false)]
    [InlineData("text", true)]
    [InlineData("binary", true)]
    [InlineData("json", true)]
    public static async Task AsyncWriter_IOFaults_PropagateAndDoNotLoseBuffer(string format, bool failFlush)
    {
        using AsyncOnlyWriteStream output = new AsyncOnlyWriteStream();
        XmlDictionaryWriter writer = await CreateAsyncWriter(format, output, new UTF8Encoding(false));
        await writer.WriteStartElementAsync(null, "root", null);
        await writer.WriteStringAsync("value");
        IOException expectedError = new IOException("Injected async I/O failure.");
        if (failFlush)
            output.FlushFailure = expectedError;
        else
            output.WriteFailure = expectedError;
        Assert.Same(expectedError, await Assert.ThrowsAsync<IOException>(() => writer.FlushAsync()));
        output.WriteFailure = null;
        output.FlushFailure = null;
        await writer.FlushAsync();
        await writer.WriteEndElementAsync();
        await writer.DisposeAsync();
        using MemoryStream expected = new MemoryStream();
        using (XmlDictionaryWriter syncWriter = CreateSyncWriter(format, expected))
        {
            syncWriter.WriteStartElement("root");
            syncWriter.WriteString("value");
            syncWriter.Flush();
            syncWriter.WriteEndElement();
        }
        Assert.Equal(expected.ToArray(), output.Bytes);
    }

    [Theory]
    [InlineData("text", false)]
    [InlineData("binary", false)]
    [InlineData("json", false)]
    [InlineData("text", true)]
    [InlineData("binary", true)]
    [InlineData("json", true)]
    public static async Task AsyncWriter_Disposal_FinalizesAndPreservesOwnership(string format, bool ownsStream)
    {
        using AsyncOnlyWriteStream output = new AsyncOnlyWriteStream();
        XmlDictionaryWriter writer = await CreateAsyncWriter(format, output, new UTF8Encoding(false), ownsStream);
        await writer.WriteStartElementAsync(null, "root", null);
        await writer.WriteStringAsync("value");
        await writer.DisposeAsync();
        await writer.DisposeAsync();
        Assert.Equal(WriteState.Closed, writer.WriteState);
        Assert.Equal(ownsStream ? 1 : 0, output.AsyncDisposals);
        Assert.Equal(0, output.SyncDisposals);
        Assert.True(output.AsyncFlushes > 0);
        using MemoryStream expected = new MemoryStream();
        using (XmlDictionaryWriter syncWriter = CreateSyncWriter(format, expected))
        {
            syncWriter.WriteStartElement("root");
            syncWriter.WriteString("value");
        }
        Assert.Equal(expected.ToArray(), output.Bytes);
    }

    [Fact]
    public static async Task AsyncWriter_BinaryArrayAfterDisposal_IsRejected()
    {
        using AsyncOnlyWriteStream output = new AsyncOnlyWriteStream();
        XmlDictionaryWriter writer = await CreateAsyncWriter("binary", output, new UTF8Encoding(false));
        await writer.WriteStartElementAsync(null, "root", null);
        await writer.DisposeAsync();
        byte[] expected = output.Bytes;
        await Assert.ThrowsAsync<InvalidOperationException>(() => WriteArrayAsync(writer, new int[] { 1 }, 0, 1));
        Assert.Equal(expected, output.Bytes);
        Assert.Equal(WriteState.Closed, writer.WriteState);
    }

    [Theory]
    [InlineData("text")]
    [InlineData("binary")]
    [InlineData("json")]
    public static async Task AsyncWriter_CanceledDisposal_StillClosesOwnedStream(string format)
    {
        using AsyncOnlyWriteStream output = new AsyncOnlyWriteStream();
        XmlDictionaryWriter writer = await CreateAsyncWriter(format, output, new UTF8Encoding(false), ownsStream: true);
        await writer.WriteStartElementAsync(null, "root", null);
        await writer.WriteStringAsync("value");
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CloseWriterAsync(writer, cancellation.Token));
        Assert.Equal(WriteState.Closed, writer.WriteState);
        Assert.Equal(1, output.AsyncDisposals);
        Assert.Equal(0, output.SyncDisposals);
    }

    [Theory]
    [InlineData("text")]
    [InlineData("binary")]
    [InlineData("json")]
    public static async Task AsyncWriter_DisposalFault_LeavesWriterClosed(string format)
    {
        using AsyncOnlyWriteStream output = new AsyncOnlyWriteStream();
        IOException expected = new IOException("Injected async disposal failure.");
        output.DisposeFailure = expected;
        XmlDictionaryWriter writer = await CreateAsyncWriter(format, output, new UTF8Encoding(false), ownsStream: true);
        await writer.WriteStartElementAsync(null, "root", null);
        await writer.WriteStringAsync("value");
        Assert.Same(expected, await Assert.ThrowsAsync<IOException>(() => writer.DisposeAsync().AsTask()));
        Assert.Equal(WriteState.Closed, writer.WriteState);
        Assert.Equal(1, output.AsyncDisposals);
        await writer.DisposeAsync();
        Assert.Equal(1, output.AsyncDisposals);
    }

    [Theory]
    [InlineData("utf16")]
    [InlineData("utf16be")]
    public static async Task AsyncWriter_Initialization_IsCancelableAndWritesBOMAsynchronously(string encodingName)
    {
        Encoding encoding = GetWriterEncoding(encodingName);
        using AsyncOnlyWriteStream canceledOutput = new AsyncOnlyWriteStream();
        using CancellationTokenSource canceled = new CancellationTokenSource();
        canceled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CreateAsyncWriter("text", canceledOutput, encoding, cancellationToken: canceled.Token));
        Assert.Empty(canceledOutput.Bytes);
        Assert.Equal(0, canceledOutput.AsyncDisposals);
        using AsyncOnlyWriteStream output = new AsyncOnlyWriteStream();
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        XmlDictionaryWriter writer = await CreateAsyncWriter("text", output, encoding, cancellationToken: cancellation.Token);
        Assert.Equal(encoding.GetPreamble(), output.Bytes);
        Assert.Equal(cancellation.Token, output.LastWriteToken);
        await writer.WriteStartElementAsync(null, "root", null);
        await writer.DisposeAsync();
    }

    [Fact]
    public static async Task AsyncWriter_ExternalXmlWriter_ForwardsProtectedAsyncAttributes()
    {
        string text = new string('\u00e9', 2049) + "<&\U0001f642";
        XmlWriterSettings settings = new XmlWriterSettings { Async = true, Encoding = new UTF8Encoding(false), CloseOutput = false };
        using AsyncOnlyWriteStream output = new AsyncOnlyWriteStream();
        XmlWriter core = XmlWriter.Create(output, settings);
        XmlDictionaryWriter writer = XmlDictionaryWriter.CreateDictionaryWriter(core);
        await writer.WriteStartElementAsync(null, "root", null);
        await writer.WriteAttributeStringAsync(null, "attribute", null, text);
        await writer.WriteStringAsync(text);
        await writer.WriteEndElementAsync();
        await writer.DisposeAsync();
        using MemoryStream expected = new MemoryStream();
        settings.Async = false;
        using (XmlWriter syncWriter = XmlWriter.Create(expected, settings))
        {
            syncWriter.WriteStartElement("root");
            syncWriter.WriteAttributeString("attribute", text);
            syncWriter.WriteString(text);
            syncWriter.WriteEndElement();
        }
        Assert.Equal(expected.ToArray(), output.Bytes);
        Assert.True(output.AsyncWrites > 0);
        Assert.Equal(0, output.AsyncDisposals);
    }

    [Fact]
    public static async Task AsyncWriter_ExternalSynchronousXmlWriter_DoesNotFallback()
    {
        using AsyncOnlyWriteStream output = new AsyncOnlyWriteStream();
        XmlWriter core = XmlWriter.Create(output, new XmlWriterSettings { Async = false, Encoding = new UTF8Encoding(false), CloseOutput = false });
        XmlDictionaryWriter writer = XmlDictionaryWriter.CreateDictionaryWriter(core);
        await Assert.ThrowsAsync<InvalidOperationException>(() => writer.WriteStartElementAsync(null, "root", null));
        Assert.Empty(output.Bytes);
        Assert.Equal(0, output.AsyncWrites);
    }

    private sealed class AsyncOnlyReadStream : Stream
    {
        private readonly MemoryStream _input;

        public AsyncOnlyReadStream(byte[] bytes) => _input = new MemoryStream(bytes, writable: false);
        public int AsyncReads { get; private set; }
        public CancellationToken LastToken { get; private set; }
        public override bool CanRead => true;
        public override bool CanWrite => false;
        public override bool CanSeek => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Flush() => throw new InvalidOperationException("Synchronous flush.");
        public override void Write(byte[] buffer, int offset, int count) => throw new InvalidOperationException("Synchronous write.");
        public override int Read(byte[] buffer, int offset, int count) => throw new InvalidOperationException("Synchronous read.");
        public override int Read(Span<byte> buffer) => throw new InvalidOperationException("Synchronous read.");
        public override int ReadByte() => throw new InvalidOperationException("Synchronous read.");
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            AsyncReads++;
            LastToken = cancellationToken;
            return _input.Read(buffer.Span);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _input.Dispose();
            base.Dispose(disposing);
        }
    }

    private sealed class AsyncStreamProvider : IStreamProvider
    {
        public AsyncStreamProvider(byte[] bytes) => Stream = new AsyncOnlyReadStream(bytes);
        public AsyncOnlyReadStream Stream { get; }
        public int Releases { get; private set; }
        public Stream GetStream() => Stream;
        public void ReleaseStream(Stream stream)
        {
            Assert.Same(Stream, stream);
            Releases++;
            stream.Dispose();
        }
    }

    [Theory]
    [InlineData("text", 0)]
    [InlineData("binary", 0)]
    [InlineData("json", 0)]
    [InlineData("text", 1)]
    [InlineData("binary", 1)]
    [InlineData("json", 1)]
    [InlineData("text", 2)]
    [InlineData("binary", 2)]
    [InlineData("json", 2)]
    public static async Task AsyncWriter_StreamProvider_UsesAsyncReadsAndAlwaysReleases(string format, int failure)
    {
        byte[] bytes = GetByteArray(2051);
        AsyncStreamProvider provider = new AsyncStreamProvider(bytes);
        using AsyncOnlyWriteStream output = new AsyncOnlyWriteStream();
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        XmlDictionaryWriter writer = await CreateAsyncWriter(format, output, new UTF8Encoding(false));
        await writer.WriteStartElementAsync(null, "root", null);
        IOException expectedError = new IOException("Injected provider write failure.");
        if (failure == 1)
            output.WriteFailure = expectedError;
        else if (failure == 2)
            output.WriteGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task operation = WriteValueAsync(writer, provider, cancellation.Token);
        if (failure == 1)
            Assert.Same(expectedError, await Assert.ThrowsAsync<IOException>(() => operation));
        else if (failure == 2)
        {
            await output.WriteStarted.Task;
            cancellation.Cancel();
            OperationCanceledException error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
            Assert.Equal(cancellation.Token, error.CancellationToken);
        }
        else
        {
            await operation;
        }
        Assert.Equal(1, provider.Releases);
        Assert.True(provider.Stream.AsyncReads > 0);
        Assert.Equal(cancellation.Token, provider.Stream.LastToken);
        output.WriteFailure = null;
        output.WriteGate = null;
        await writer.DisposeAsync();
        if (failure == 0)
        {
            using MemoryStream expected = new MemoryStream();
            using (XmlDictionaryWriter syncWriter = CreateSyncWriter(format, expected))
            {
                syncWriter.WriteStartElement("root");
                syncWriter.WriteBase64(bytes, 0, 256);
                syncWriter.WriteBase64(bytes, 256, bytes.Length - 256);
            }
            Assert.Equal(expected.ToArray(), output.Bytes);
        }
    }

    [Theory]
    [InlineData("text", "utf16")]
    [InlineData("text", "utf16be")]
    [InlineData("json", "utf16")]
    [InlineData("json", "utf16be")]
    public static async Task AsyncWriter_Transcoding_DisposesOwnedStreamAfterFlushFailure(string format, string encodingName)
    {
        using AsyncOnlyWriteStream output = new AsyncOnlyWriteStream();
        XmlDictionaryWriter writer = await CreateAsyncWriter(format, output, GetWriterEncoding(encodingName), ownsStream: true);
        await writer.WriteStartElementAsync(null, "root", null);
        await writer.WriteStringAsync("\u4e00\U0001f642");
        IOException expected = new IOException("Injected transcoding flush failure.");
        output.FlushFailure = expected;
        Assert.Same(expected, await Assert.ThrowsAsync<IOException>(() => writer.DisposeAsync().AsTask()));
        Assert.Equal(WriteState.Closed, writer.WriteState);
        Assert.Equal(1, output.AsyncDisposals);
        Assert.Equal(0, output.SyncDisposals);
    }

    [Theory]
    [InlineData("text", "utf16")]
    [InlineData("text", "utf16be")]
    [InlineData("json", "utf16")]
    [InlineData("json", "utf16be")]
    public static async Task AsyncWriter_Transcoding_CanceledDisposal_DoesNotFlushWithoutToken(string format, string encodingName)
    {
        using AsyncOnlyWriteStream output = new AsyncOnlyWriteStream();
        XmlDictionaryWriter writer = await CreateAsyncWriter(format, output, GetWriterEncoding(encodingName), ownsStream: true);
        await writer.WriteStartElementAsync(null, "root", null);
        await writer.WriteStringAsync(new string('\u4e00', 2049));
        byte[] before = output.Bytes;
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        OperationCanceledException error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CloseWriterAsync(writer, cancellation.Token));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(cancellation.Token, output.LastFlushToken);
        Assert.Equal(before, output.Bytes);
        Assert.Equal(1, output.AsyncDisposals);
        Assert.Equal(0, output.SyncDisposals);
    }

    [Fact]
    public static async Task AsyncWriter_InvalidBinaryUnicode_MatchesSynchronousFailure()
    {
        string value = new string('a', 85) + "\ud800";
        using MemoryStream expected = new MemoryStream();
        using (XmlDictionaryWriter syncWriter = XmlDictionaryWriter.CreateBinaryWriter(expected, null, null, ownsStream: false))
        {
            syncWriter.WriteStartElement("root");
            Assert.Throws<EncoderFallbackException>(() => syncWriter.WriteString(value));
        }
        using AsyncOnlyWriteStream output = new AsyncOnlyWriteStream();
        XmlDictionaryWriter writer = await CreateAsyncWriter("binary", output, Encoding.UTF8);
        await writer.WriteStartElementAsync(null, "root", null);
        await Assert.ThrowsAsync<EncoderFallbackException>(() => writer.WriteStringAsync(value));
        await writer.DisposeAsync();
        Assert.Equal(expected.ToArray(), output.Bytes);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
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

        Assert.Equal(expect, actual);
    }

    [Fact]
    public static void XmlBaseWriter_WriteBinHex()
    {
        var str = "The quick brown fox jumps over the lazy dog.";
        var bytes = Encoding.Unicode.GetBytes(str);
        string expect = @"<data>540068006500200071007500690063006B002000620072006F0077006E00200066006F00780020006A0075006D007000730020006F00760065007200200074006800650020006C0061007A007900200064006F0067002E00</data>";
        string actual;
        using (var ms = new MemoryStream())
        {
            var writer = XmlDictionaryWriter.CreateTextWriter(ms);
            writer.WriteStartElement("data");
            writer.WriteBinHex(bytes, 0, bytes.Length);
            writer.WriteEndElement();
            writer.Flush();
            ms.Position = 0;
            var sr = new StreamReader(ms);
            actual = sr.ReadToEnd();
        }

        Assert.Equal(expect, actual);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
    public static void XmlBaseWriter_FlushAsync()
    {
        string actual = null;
        int byteSize = 1024;
        byte[] bytes = GetByteArray(byteSize);
        string expect = GetExpectString(bytes, byteSize);
        string lastCompletedOperation = null;
        try
        {
            using (var ms = new AsyncMemoryStream())
            {
                var writer = XmlDictionaryWriter.CreateTextWriter(ms);
                lastCompletedOperation = "XmlDictionaryWriter.CreateTextWriter()";

                writer.WriteStartDocument();
                lastCompletedOperation = "writer.WriteStartDocument()";

                writer.WriteStartElement("data");
                lastCompletedOperation = "writer.WriteStartElement()";

                writer.WriteBase64(bytes, 0, byteSize);
                lastCompletedOperation = "writer.WriteBase64()";

                writer.WriteEndElement();
                lastCompletedOperation = "writer.WriteEndElement()";

                writer.WriteEndDocument();
                lastCompletedOperation = "writer.WriteEndDocument()";

                var task = writer.FlushAsync();
                lastCompletedOperation = "writer.FlushAsync()";

                task.Wait();
                ms.Position = 0;
                var sr = new StreamReader(ms);
                actual = sr.ReadToEnd();
            }
        }
        catch (Exception e)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"An error occurred: {e.Message}");
            sb.AppendLine(e.StackTrace);
            sb.AppendLine();
            sb.AppendLine($"The last completed operation before the exception was: {lastCompletedOperation}");
            Assert.Fail(sb.ToString());
        }

        Assert.Equal(expect, actual);

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

        Assert.Equal(expect, actual);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
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
            bool isAsyncIsRunningException = e.Message.Contains("XmlAsyncIsRunningException") || e.Message.Contains("in progress");
            Assert.True(isAsyncIsRunningException, "The exception is not XmlAsyncIsRunningException.");

            // let the first task complete
            ms.blockAsync(false);
            t1.Wait();
        }
    }

    [Fact]
    public static void XmlDictionaryWriter_InvalidUnicodeChar()
    {
        using (var ms = new MemoryStream())
        {
            var writer = XmlDictionaryWriter.CreateTextWriter(ms);
            writer.WriteStartDocument();
            writer.WriteStartElement("data");

            // This is an invalid char. Writing this char shouldn't
            // throw exception.
            writer.WriteString("\uDB1B");

            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Flush();
            ms.Position = 0;
        }
    }

    [Fact]
    public static void CreateMtomReaderWriter_Throw_PNSE()
    {
        using (var stream = new MemoryStream())
        {
            string startInfo = "application/soap+xml";
            Assert.Throws<PlatformNotSupportedException>(() => XmlDictionaryWriter.CreateMtomWriter(stream, Encoding.UTF8, int.MaxValue, startInfo));
        }
    }

    [Fact]
    public static void CreateTextReaderWriterTest()
    {
        string expected = "<localName>the value</localName>";
        using (MemoryStream stream = new MemoryStream())
        {
            using (XmlDictionaryWriter writer = XmlDictionaryWriter.CreateTextWriter(stream, Encoding.UTF8, false))
            {
                writer.WriteElementString("localName", "the value");
                writer.Flush();
                byte[] bytes = stream.ToArray();
                StreamReader reader = new StreamReader(stream);
                stream.Position = 0;
                string content = reader.ReadToEnd();
                Assert.Equal(expected, content);
                reader.Close();

                using (XmlDictionaryReader xreader = XmlDictionaryReader.CreateTextReader(bytes, new XmlDictionaryReaderQuotas()))
                {
                    xreader.Read();
                    string xml = xreader.ReadOuterXml();
                    Assert.Equal(expected, xml);
                }
            }
        }
    }

    [Fact]
    public static void StreamProvoiderTest()
    {
        List<string> ReaderWriterType = new List<string>
            {
                "Binary",
                //"MTOM", //MTOM methods not supported now.
                //"MTOM",
                //"MTOM",
                "Text",
                "Text",
                "Text"
            };

        List<string> Encodings = new List<string>
            {
                "utf-8",
                "utf-8",
                "utf-16",
                "unicodeFFFE",
                "utf-8",
                "utf-16",
                "unicodeFFFE"
            };

        for (int i = 0; i < ReaderWriterType.Count; i++)
        {
            string rwTypeStr = ReaderWriterType[i];
            ReaderWriterFactory.ReaderWriterType rwType = (ReaderWriterFactory.ReaderWriterType)
            Enum.Parse(typeof(ReaderWriterFactory.ReaderWriterType), rwTypeStr, true);
            Encoding encoding = Encoding.GetEncoding(Encodings[i]);

            Random rndGen = new Random();
            int byteArrayLength = rndGen.Next(100, 2000);
            byte[] byteArray = new byte[byteArrayLength];
            rndGen.NextBytes(byteArray);
            MyStreamProvider myStreamProvider = new MyStreamProvider(new MemoryStream(byteArray));
            bool success = false;
            bool successBase64 = false;
            MemoryStream ms = new MemoryStream();
            success = WriteTest(ms, rwType, encoding, myStreamProvider);
            Assert.True(success);
            success = ReadTest(ms, encoding, rwType, byteArray);
            Assert.True(success);
            if (rwType == ReaderWriterFactory.ReaderWriterType.Text)
            {
                ms = new MemoryStream();
                myStreamProvider = new MyStreamProvider(new MemoryStream(byteArray));
                success = AsyncWriteTest(ms, encoding, myStreamProvider);
                Assert.True(success);
                successBase64 = AsyncWriteBase64Test(ms, byteArray, encoding, myStreamProvider);
                Assert.True(successBase64);
            }
        }
    }

    [Fact]
    public static void IXmlBinaryReaderWriterInitializerTest()
    {
        DataContractSerializer serializer = new DataContractSerializer(typeof(TestData));
        MemoryStream ms = new MemoryStream();
        TestData td = new TestData();
        XmlDictionaryWriter binaryWriter = XmlDictionaryWriter.CreateBinaryWriter(ms, null, null, false);
        IXmlBinaryWriterInitializer writerInitializer = (IXmlBinaryWriterInitializer)binaryWriter;
        writerInitializer.SetOutput(ms, null, null, false);
        serializer.WriteObject(ms, td);
        binaryWriter.Flush();
        byte[] xmlDoc = ms.ToArray();
        binaryWriter.Close();
        XmlDictionaryReader binaryReader = XmlDictionaryReader.CreateBinaryReader(xmlDoc, 0, xmlDoc.Length, null, XmlDictionaryReaderQuotas.Max, null, new OnXmlDictionaryReaderClose((XmlDictionaryReader reader) => { }));
        IXmlBinaryReaderInitializer readerInitializer = (IXmlBinaryReaderInitializer)binaryReader;
        readerInitializer.SetInput(xmlDoc, 0, xmlDoc.Length, null, XmlDictionaryReaderQuotas.Max, null, new OnXmlDictionaryReaderClose((XmlDictionaryReader reader) => { }));
        binaryReader.ReadContentAsObject();
        binaryReader.Close();
    }

    [Fact]
    public static void IXmlTextReaderInitializerTest()
    {
        var writer = new SampleTextWriter();
        var ms = new MemoryStream();
        var encoding = Encoding.UTF8;
        writer.SetOutput(ms, encoding, true);
    }

    [Fact]
    public static void FragmentTest()
    {
        string rwTypeStr = "Text";
        ReaderWriterFactory.ReaderWriterType rwType = (ReaderWriterFactory.ReaderWriterType)
            Enum.Parse(typeof(ReaderWriterFactory.ReaderWriterType), rwTypeStr, true);
        Encoding encoding = Encoding.GetEncoding("utf-8");
        MemoryStream ms = new MemoryStream();
        XmlDictionaryWriter writer = (XmlDictionaryWriter)ReaderWriterFactory.CreateXmlWriter(rwType, ms, encoding);
        Assert.False(FragmentHelper.CanFragment(writer));
    }

    [Fact]
    public static void BinaryWriter_PrimitiveTypes()
    {
        using MemoryStream ms = new();
        using XmlDictionaryWriter writer = XmlDictionaryWriter.CreateBinaryWriter(ms);
        writer.WriteStartElement("root");

        AssertBytesWritten(x => x.WriteValue((byte)0x78), XmlBinaryNodeType.Int8Text, new byte[] { 0x78 });
        AssertBytesWritten(x => x.WriteValue((short)0x1234), XmlBinaryNodeType.Int16Text, new byte[] { 0x34, 0x12 });
        AssertBytesWritten(x => x.WriteValue(unchecked((short)0xf234)), XmlBinaryNodeType.Int16Text, new byte[] { 0x34, 0xf2 });
        AssertBytesWritten(x => x.WriteValue((int)0x12345678), XmlBinaryNodeType.Int32Text, new byte[] { 0x78, 0x56, 0x34, 0x12 });
        AssertBytesWritten(x => x.WriteValue((long)0x0102030412345678), XmlBinaryNodeType.Int64Text, new byte[] { 0x78, 0x56, 0x34, 0x12, 04, 03, 02, 01 });

        // Integer values should be represented using smalles possible type
        AssertBytesWritten(x => x.WriteValue((long)0), XmlBinaryNodeType.ZeroText, Span<byte>.Empty);
        AssertBytesWritten(x => x.WriteValue((long)1), XmlBinaryNodeType.OneText, Span<byte>.Empty);
        AssertBytesWritten(x => x.WriteValue((int)0x00000078), XmlBinaryNodeType.Int8Text, new byte[] { 0x78 });
        AssertBytesWritten(x => x.WriteValue(unchecked((int)0xfffffff0)), XmlBinaryNodeType.Int8Text, new byte[] { 0xf0 });
        AssertBytesWritten(x => x.WriteValue((int)0x00001234), XmlBinaryNodeType.Int16Text, new byte[] { 0x34, 0x12 });
        AssertBytesWritten(x => x.WriteValue(unchecked((int)0xfffff234)), XmlBinaryNodeType.Int16Text, new byte[] { 0x34, 0xf2 });
        AssertBytesWritten(x => x.WriteValue((long)0x12345678), XmlBinaryNodeType.Int32Text, new byte[] { 0x78, 0x56, 0x34, 0x12 });
        AssertBytesWritten(x => x.WriteValue(unchecked((long)0xfffffffff2345678)), XmlBinaryNodeType.Int32Text, new byte[] { 0x78, 0x56, 0x34, 0xf2 });

        float f = 1.23456788f;
        ReadOnlySpan<byte> floatBytes = new byte[] { 0x52, 0x06, 0x9e, 0x3f };
        double d = 1.0 / 3.0;
        ReadOnlySpan<byte> doubleBytes = new byte[] { 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0xd5, 0x3f };
        Guid guid = new Guid(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 });
        DateTime datetime = new DateTime(2022, 8, 26, 12, 34, 56, DateTimeKind.Utc);
        Span<byte> datetimeBytes = stackalloc byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(datetimeBytes, datetime.ToBinary());

        AssertBytesWritten(x => x.WriteValue(f), XmlBinaryNodeType.FloatText, floatBytes);
        AssertBytesWritten(x => x.WriteValue(new decimal(0x20212223, 0x10111213, 0x01020304, true, scale: 0x1b)), XmlBinaryNodeType.DecimalText,
                           new byte[] { 0x0, 0x0, 0x1b, 0x80, 0x4, 0x3, 0x2, 0x1, 0x23, 0x22, 0x21, 0x20, 0x13, 0x12, 0x11, 0x10 });
        AssertBytesWritten(x => x.WriteValue(guid), XmlBinaryNodeType.GuidText, guid.ToByteArray());
        AssertBytesWritten(x => x.WriteValue(new TimeSpan(0x0807060504030201)), XmlBinaryNodeType.TimeSpanText, new byte[] { 01, 02, 03, 04, 05, 06, 07, 08 });
        AssertBytesWritten(x => x.WriteValue(datetime), XmlBinaryNodeType.DateTimeText, datetimeBytes);

        // Double can be represented as float or int as long as no detail is lost
        AssertBytesWritten(x => x.WriteValue((double)f), XmlBinaryNodeType.FloatText, floatBytes);
        AssertBytesWritten(x => x.WriteValue((double)0x0100), XmlBinaryNodeType.Int16Text, new byte[] { 0x00, 0x01 });
        AssertBytesWritten(x => x.WriteValue(d), XmlBinaryNodeType.DoubleText, doubleBytes);


        void AssertBytesWritten(Action<XmlDictionaryWriter> action, XmlBinaryNodeType nodeType, ReadOnlySpan<byte> expected)
        {
            writer.WriteStartElement("a");

            // Reset stream so we only compare the actual value written (including end element)
            writer.Flush();
            ms.Position = 0;
            ms.SetLength(0);

            action(writer);

            writer.Flush();
            ms.TryGetBuffer(out ArraySegment<byte> segement);
            Assert.Equal(nodeType, (XmlBinaryNodeType)segement[0]);
            AssertExtensions.SequenceEqual(expected, segement.AsSpan(1));
            writer.WriteEndElement();
        }
    }


    [Fact]
    public static void BinaryWriter_Arrays()
    {
        using var ms = new MemoryStream();
        using var writer = XmlDictionaryWriter.CreateBinaryWriter(ms);
        writer.WriteStartElement("root");
        int offset = 1;
        int count = 2;

        bool[] bools = new bool[] { false, true, false, true };
        AssertBytesWritten(x => x.WriteArray(null, "a", null, bools, offset, count), XmlBinaryNodeType.BoolTextWithEndElement,
                           count, new byte[] { 1, 0 });

        short[] shorts = new short[] { -1, 0x0102, 0x1122, -1 };
        AssertBytesWritten(x => x.WriteArray(null, "a", null, shorts, offset, count), XmlBinaryNodeType.Int16TextWithEndElement,
                           count, new byte[] { 2, 1, 0x22, 0x11 });

        int[] ints = new int[] { -1, 0x01020304, 0x11223344, -1 };
        AssertBytesWritten(x => x.WriteArray(null, "a", null, ints, offset, count), XmlBinaryNodeType.Int32TextWithEndElement,
                           count, new byte[] { 4, 3, 2, 1, 0x44, 0x33, 0x22, 0x11 });

        long[] longs = new long[] { -1, 0x0102030405060708, 0x1122334455667788, -1 };
        AssertBytesWritten(x => x.WriteArray(null, "a", null, longs, offset, count), XmlBinaryNodeType.Int64TextWithEndElement,
                           count, new byte[] { 8, 7, 6, 5, 4, 3, 2, 1, 0x88, 0x77, 0x66, 0x55, 0x44, 0x33, 0x22, 0x11 });

        float[] floats = new float[] { -1.0f, 1.23456788f, 1.23456788f, -1.0f };
        AssertBytesWritten(x => x.WriteArray(null, "a", null, floats, offset, count), XmlBinaryNodeType.FloatTextWithEndElement,
                           count, new byte[] { 0x52, 0x06, 0x9e, 0x3f, 0x52, 0x06, 0x9e, 0x3f });

        double[] doubles = new double[] { -1.0, 1.0 / 3.0, 1.0 / 3.0, -1.0 };
        AssertBytesWritten(x => x.WriteArray(null, "a", null, doubles, offset, count), XmlBinaryNodeType.DoubleTextWithEndElement,
                           count, new byte[] { 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0xd5, 0x3f,
                                               0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0xd5, 0x3f });

        decimal[] decimals = new[] {
            new decimal(0x20212223, 0x10111213, 0x01020304, true, scale: 0x1b),
            new decimal(0x50515253, 0x40414243, 0x31323334, false, scale: 0x1c)
        };
        AssertBytesWritten(x => x.WriteArray(null, "a", null, decimals, 0, decimals.Length), XmlBinaryNodeType.DecimalTextWithEndElement,
                           decimals.Length, new byte[] { 0x0, 0x0, 0x1b, 0x80, 0x4, 0x3, 0x2, 0x1,
                                                         0x23, 0x22, 0x21, 0x20, 0x13, 0x12, 0x11, 0x10,
                                                         0x0, 0x0, 0x1c, 0x00, 0x34, 0x33, 0x32, 0x31,
                                                         0x53, 0x52, 0x51, 0x50, 0x43, 0x42, 0x41, 0x40 });

        DateTime[] datetimes = new[] {
            new DateTime(2022, 8, 26, 12, 34, 56, DateTimeKind.Utc),
            new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Local)
        };
        Span<byte> datetimeBytes = stackalloc byte[8 * datetimes.Length];
        for (int i = 0; i < datetimes.Length; i++)
        {
            BinaryPrimitives.WriteInt64LittleEndian(datetimeBytes.Slice(8 * i), datetimes[i].ToBinary());
        }
        AssertBytesWritten(x => x.WriteArray(null, "a", null, datetimes, 0, datetimes.Length), XmlBinaryNodeType.DateTimeTextWithEndElement,
                           datetimes.Length, datetimeBytes);

        TimeSpan[] timespans = new[] { new TimeSpan(0x0807060504030201), new TimeSpan(0x1817161514131211) };
        AssertBytesWritten(x => x.WriteArray(null, "a", null, timespans, 0, timespans.Length), XmlBinaryNodeType.TimeSpanTextWithEndElement,
                           timespans.Length, new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                                                          0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18 });

        Guid[] guids = new Guid[]
        {
            new Guid(new ReadOnlySpan<byte>(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 })),
            new Guid(new ReadOnlySpan<byte>(new byte[] { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 130, 140, 150, 160 }))
        };
        AssertBytesWritten(x => x.WriteArray(null, "a", null, guids, 0, guids.Length), XmlBinaryNodeType.GuidTextWithEndElement,
                           guids.Length, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16,
                                                      10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 130, 140, 150, 160 });

        // Write more than 512 bytes in a single call to trigger different writing logic in XmlStreamNodeWriter.WriteBytes
        long[] many_longs = Enumerable.Range(0x01020304, 127).Select(i => (long)i | (long)(~i << 32)).ToArray();
        Span<byte> many_longBytes = stackalloc byte[8 * many_longs.Length];
        for (int i = 0; i < many_longs.Length; i++)
        {
            BinaryPrimitives.WriteInt64LittleEndian(many_longBytes.Slice(8 * i), many_longs[i]);
        }
        AssertBytesWritten(x => x.WriteArray(null, "a", null, many_longs, 0, many_longs.Length), XmlBinaryNodeType.Int64TextWithEndElement,
                           many_longs.Length, many_longBytes);

        void AssertBytesWritten(Action<XmlDictionaryWriter> action, XmlBinaryNodeType nodeType, int count, ReadOnlySpan<byte> expected)
        {
            // Reset stream so we only compare the actual value written (including end element)
            writer.Flush();
            ms.Position = 0;
            ms.SetLength(0);

            action(writer);

            writer.Flush();
            ms.TryGetBuffer(out ArraySegment<byte> segement);

            var actual = segement.AsSpan();
            Assert.Equal(XmlBinaryNodeType.Array, (XmlBinaryNodeType)actual[0]);
            Assert.Equal(XmlBinaryNodeType.ShortElement, (XmlBinaryNodeType)actual[1]);
            int elementLength = actual[2];
            Assert.InRange(elementLength, 0, 0x8f); // verify count is single byte
            Assert.Equal(XmlBinaryNodeType.EndElement, (XmlBinaryNodeType)actual[3 + elementLength]);

            actual = actual.Slice(4 + elementLength);
            // nodetype and count
            Assert.Equal(nodeType, (XmlBinaryNodeType)actual[0]);
            Assert.Equal(checked((sbyte)count), (sbyte)actual[1]);

            AssertExtensions.SequenceEqual(expected, actual.Slice(2));
        }
    }

    [Fact]
    public static void XmlBaseWriter_WriteString()
    {
        const byte Chars8Text = 152;
        const byte Chars16Text = 154;
        MemoryStream ms = new MemoryStream();
        XmlDictionaryWriter writer = (XmlDictionaryWriter)XmlDictionaryWriter.CreateBinaryWriter(ms);
        writer.WriteStartElement("root");

        int[] lengths = new[] { 7, 8, 9, 15, 16, 17, 31, 32, 36, 258 };
        byte[] buffer = new byte[lengths.Max() + 1];

        foreach (var length in lengths)
        {
            string allAscii = string.Create(length, null, (Span<char> chars, object _) =>
            {
                for (int i = 0; i < chars.Length; ++i)
                    chars[i] = (char)(i % 128);
            });
            string multiByteLast = string.Create(length, null, (Span<char> chars, object _) =>
            {
                for (int i = 0; i < chars.Length; ++i)
                    chars[i] = (char)(i % 128);
                chars[^1] = '\u00E4'; // '�' - Latin Small Letter a with Diaeresis. Latin-1 Supplement.
            });

            int numBytes = Encoding.UTF8.GetBytes(allAscii, buffer);
            Assert.True(numBytes == length, "Test setup wrong - allAscii");
            ValidateWriteText(ms, writer, allAscii, expected: buffer.AsSpan(0, numBytes));

            numBytes = Encoding.UTF8.GetBytes(multiByteLast, buffer);
            Assert.True(numBytes == length + 1, "Test setup wrong - multiByte");
            ValidateWriteText(ms, writer, multiByteLast, expected: buffer.AsSpan(0, numBytes));
        }

        static void ValidateWriteText(MemoryStream ms, XmlDictionaryWriter writer, string text, ReadOnlySpan<byte> expected)
        {
            writer.Flush();
            ms.Seek(0, SeekOrigin.Begin);
            ms.SetLength(0);
            writer.WriteString(text);
            writer.Flush();

            ms.TryGetBuffer(out ArraySegment<byte> arraySegment);
            ReadOnlySpan<byte> buffer = arraySegment;

            if (expected.Length <= byte.MaxValue)
            {
                Assert.Equal(Chars8Text, buffer[0]);
                Assert.Equal(expected.Length, buffer[1]);
                buffer = buffer.Slice(2);
            }
            else if (expected.Length <= ushort.MaxValue)
            {
                Assert.Equal(Chars16Text, buffer[0]);
                Assert.Equal(expected.Length, (int)(buffer[1]) | ((int)buffer[2] << 8));
                buffer = buffer.Slice(3);
            }
            else
                Assert.Fail("test use to long length");

            AssertExtensions.SequenceEqual(expected, buffer);
        }
    }

    private static bool ReadTest(MemoryStream ms, Encoding encoding, ReaderWriterFactory.ReaderWriterType rwType, byte[] byteArray)
    {
        ms.Position = 0;
        XmlDictionaryReader reader = (XmlDictionaryReader)ReaderWriterFactory.CreateXmlReader(rwType, ms, encoding);
        reader.ReadToDescendant("Root");
        byte[] bytesFromReader = reader.ReadElementContentAsBase64();
        if (bytesFromReader.Length != byteArray.Length)
        {
            return false;
        }
        else
        {
            for (int i = 0; i < byteArray.Length; i++)
            {
                if (byteArray[i] != bytesFromReader[i])
                {
                    return false;
                }
            }
        }
        return true;
    }

    static bool WriteTest(MemoryStream ms, ReaderWriterFactory.ReaderWriterType rwType, Encoding encoding, MyStreamProvider myStreamProvider)
    {
        XmlWriter writer = ReaderWriterFactory.CreateXmlWriter(rwType, ms, encoding);
        XmlDictionaryWriter writeD = writer as XmlDictionaryWriter;
        writeD.WriteStartElement("Root");
        writeD.WriteValue(myStreamProvider);

        if (rwType != ReaderWriterFactory.ReaderWriterType.MTOM)
        {
            // stream should be released right after WriteValue
            Assert.True(myStreamProvider.StreamReleased, "Error, stream not released after WriteValue");
        }
        writer.WriteEndElement();

        // stream should be released now for MTOM
        if (rwType == ReaderWriterFactory.ReaderWriterType.MTOM)
        {
            Assert.True(myStreamProvider.StreamReleased, "Error, stream not released after WriteEndElement");
        }
        writer.Flush();
        return true;
    }

    static bool AsyncWriteTest(MemoryStream ms, Encoding encoding, MyStreamProvider myStreamProvider)
    {
        XmlDictionaryWriter writer = XmlDictionaryWriter.CreateTextWriter(ms);
        writer.WriteStartElement("Root");
        Task writeValueAsynctask = writer.WriteValueAsync(myStreamProvider);
        writeValueAsynctask.Wait();
        Assert.True(myStreamProvider.StreamReleased, "Error, stream not released.");
        writer.WriteEndElement();
        writer.Flush();
        return true;
    }

    static bool AsyncWriteBase64Test(MemoryStream ms, byte[] byteArray, Encoding encoding, MyStreamProvider myStreamProvider)
    {
        XmlDictionaryWriter writer = XmlDictionaryWriter.CreateTextWriter(ms);
        writer.WriteStartElement("Root");
        Task writeValueBase64Asynctask = writer.WriteBase64Async(byteArray, 0, byteArray.Length);
        writeValueBase64Asynctask.Wait();
        Assert.True(myStreamProvider.StreamReleased, "Error, stream not released.");
        writer.WriteEndElement();
        writer.Flush();
        return true;
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
    private static void SimulateWriteFragment(XmlDictionaryWriter writer, bool useFragmentAPI, int nestedLevelsLeft)
    {
        if (nestedLevelsLeft <= 0)
        {
            return;
        }

        Random rndGen = new Random(nestedLevelsLeft);
        int signatureLen = rndGen.Next(100, 200);
        byte[] signature = new byte[signatureLen];
        rndGen.NextBytes(signature);

        MemoryStream fragmentStream = new MemoryStream();

        if (!useFragmentAPI) // simulating in the writer itself
        {
            writer.WriteStartElement("SignatureValue_" + nestedLevelsLeft);
            writer.WriteBase64(signature, 0, signatureLen);
            writer.WriteEndElement();
        }

        if (useFragmentAPI)
        {
            FragmentHelper.Start(writer, fragmentStream);
        }

        writer.WriteStartElement("Fragment" + nestedLevelsLeft);
        for (int i = 0; i < 5; i++)
        {
            writer.WriteStartElement(string.Format("Element{0}_{1}", nestedLevelsLeft, i));
            writer.WriteAttributeString("attr1", "value1");
            writer.WriteAttributeString("attr2", "value2");
        }
        writer.WriteString("This is a text with unicode characters: <>&;\u0301\u2234");
        for (int i = 0; i < 5; i++)
        {
            writer.WriteEndElement();
        }

        // write other nested fragments...
        SimulateWriteFragment(writer, useFragmentAPI, nestedLevelsLeft - 1);

        writer.WriteEndElement(); // Fragment{nestedLevelsLeft}
        writer.Flush();

        if (useFragmentAPI)
        {
            FragmentHelper.End(writer);
            writer.WriteStartElement("SignatureValue_" + nestedLevelsLeft);
            writer.WriteBase64(signature, 0, signatureLen);
            writer.WriteEndElement();

            FragmentHelper.Write(writer, fragmentStream.GetBuffer(), 0, (int)fragmentStream.Length);

            writer.Flush();
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
