// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace System.Runtime.Serialization.Tests
{
    // Serializer entry points used by the shared serializer tests. Projects that define AsyncApi run
    // every call through the asynchronous API so the same tests validate both API shapes.
    internal static class SerializerTestApi
    {
#if AsyncApi
        private static bool UseAsync => true;
#else
        private static bool UseAsync => false;
#endif

        public static void TestWriteObject(this XmlObjectSerializer serializer, Stream stream, object graph)
        {
            if (UseAsync)
            {
                RunAsync(() => serializer.WriteObjectAsync(new YieldingAsyncStream(stream), graph));
            }
            else
            {
                serializer.WriteObject(stream, graph);
            }
        }

        public static void TestWriteObject(this XmlObjectSerializer serializer, XmlWriter writer, object graph)
        {
            if (UseAsync)
            {
                RunAsync(() => serializer.WriteObjectAsync(writer, graph));
            }
            else
            {
                serializer.WriteObject(writer, graph);
            }
        }

        public static void TestWriteObject(this XmlObjectSerializer serializer, XmlDictionaryWriter writer, object graph)
        {
            if (UseAsync)
            {
                RunAsync(() => serializer.WriteObjectAsync(writer, graph));
            }
            else
            {
                serializer.WriteObject(writer, graph);
            }
        }

        public static void TestWriteObject(this DataContractSerializer serializer, XmlDictionaryWriter writer, object graph, DataContractResolver dataContractResolver)
        {
            if (UseAsync)
            {
                RunAsync(() => serializer.WriteObjectAsync(writer, graph, dataContractResolver));
            }
            else
            {
                serializer.WriteObject(writer, graph, dataContractResolver);
            }
        }

        public static void TestWriteStartObject(this XmlObjectSerializer serializer, XmlWriter writer, object graph)
        {
            if (UseAsync)
            {
                RunAsync(() => serializer.WriteStartObjectAsync(writer, graph));
            }
            else
            {
                serializer.WriteStartObject(writer, graph);
            }
        }

        public static void TestWriteStartObject(this XmlObjectSerializer serializer, XmlDictionaryWriter writer, object graph)
        {
            if (UseAsync)
            {
                RunAsync(() => serializer.WriteStartObjectAsync(writer, graph));
            }
            else
            {
                serializer.WriteStartObject(writer, graph);
            }
        }

        public static void TestWriteObjectContent(this XmlObjectSerializer serializer, XmlWriter writer, object graph)
        {
            if (UseAsync)
            {
                RunAsync(() => serializer.WriteObjectContentAsync(writer, graph));
            }
            else
            {
                serializer.WriteObjectContent(writer, graph);
            }
        }

        public static void TestWriteObjectContent(this XmlObjectSerializer serializer, XmlDictionaryWriter writer, object graph)
        {
            if (UseAsync)
            {
                RunAsync(() => serializer.WriteObjectContentAsync(writer, graph));
            }
            else
            {
                serializer.WriteObjectContent(writer, graph);
            }
        }

        public static void TestWriteEndObject(this XmlObjectSerializer serializer, XmlWriter writer)
        {
            if (UseAsync)
            {
                RunAsync(() => serializer.WriteEndObjectAsync(writer));
            }
            else
            {
                serializer.WriteEndObject(writer);
            }
        }

        public static void TestWriteEndObject(this XmlObjectSerializer serializer, XmlDictionaryWriter writer)
        {
            if (UseAsync)
            {
                RunAsync(() => serializer.WriteEndObjectAsync(writer));
            }
            else
            {
                serializer.WriteEndObject(writer);
            }
        }

        public static object TestReadObject(this XmlObjectSerializer serializer, Stream stream) =>
            UseAsync ? RunAsync(() => serializer.ReadObjectAsync(new YieldingAsyncStream(stream))) : serializer.ReadObject(stream);

        public static object TestReadObject(this XmlObjectSerializer serializer, XmlReader reader) =>
            UseAsync ? RunAsync(() => serializer.ReadObjectAsync(reader)) : serializer.ReadObject(reader);

        public static object TestReadObject(this XmlObjectSerializer serializer, XmlReader reader, bool verifyObjectName) =>
            UseAsync ? RunAsync(() => serializer.ReadObjectAsync(reader, verifyObjectName)) : serializer.ReadObject(reader, verifyObjectName);

        public static object TestReadObject(this XmlObjectSerializer serializer, XmlDictionaryReader reader) =>
            UseAsync ? RunAsync(() => serializer.ReadObjectAsync(reader)) : serializer.ReadObject(reader);

        public static object TestReadObject(this XmlObjectSerializer serializer, XmlDictionaryReader reader, bool verifyObjectName) =>
            UseAsync ? RunAsync(() => serializer.ReadObjectAsync(reader, verifyObjectName)) : serializer.ReadObject(reader, verifyObjectName);

        public static object TestReadObject(this DataContractSerializer serializer, XmlDictionaryReader reader, bool verifyObjectName, DataContractResolver dataContractResolver) =>
            UseAsync
                ? RunAsync(() => serializer.ReadObjectAsync(reader, verifyObjectName, dataContractResolver))
                : serializer.ReadObject(reader, verifyObjectName, dataContractResolver);

        public static bool TestIsStartObject(this XmlObjectSerializer serializer, XmlReader reader) =>
            UseAsync ? RunAsync(() => serializer.IsStartObjectAsync(reader)) : serializer.IsStartObject(reader);

        public static bool TestIsStartObject(this XmlObjectSerializer serializer, XmlDictionaryReader reader) =>
            UseAsync ? RunAsync(() => serializer.IsStartObjectAsync(reader)) : serializer.IsStartObject(reader);

        // The shared tests are synchronous, so the async variants require a thread pool.
        private static void RunAsync(Func<Task> operation) => Task.Run(operation).GetAwaiter().GetResult();

        private static T RunAsync<T>(Func<Task<T>> operation) => Task.Run(operation).GetAwaiter().GetResult();

        // Forwards to the caller's stream, yields before every operation so asynchronous continuations
        // are exercised, limits reads to small chunks to cross buffer boundaries, and rejects synchronous I/O.
        private sealed class YieldingAsyncStream : Stream
        {
            private const int MaxReadSize = 61;
            private readonly Stream _inner;

            public YieldingAsyncStream(Stream inner) => _inner = inner;

            public override bool CanRead => _inner.CanRead;
            public override bool CanWrite => _inner.CanWrite;
            public override bool CanSeek => false;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

            public override int Read(byte[] buffer, int offset, int count) => throw new InvalidOperationException("Synchronous read on an asynchronous serializer operation.");
            public override void Write(byte[] buffer, int offset, int count) => throw new InvalidOperationException("Synchronous write on an asynchronous serializer operation.");
            public override void Flush() => throw new InvalidOperationException("Synchronous flush on an asynchronous serializer operation.");
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
                ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

            public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                return await _inner.ReadAsync(buffer.Slice(0, Math.Min(buffer.Length, MaxReadSize)), cancellationToken).ConfigureAwait(false);
            }

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
                WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

            public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                await _inner.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
            }

            public override async Task FlushAsync(CancellationToken cancellationToken)
            {
                await Task.Yield();
                await _inner.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            // The caller owns the wrapped stream.
            protected override void Dispose(bool disposing)
            {
            }

            public override ValueTask DisposeAsync() => default;
        }
    }
}
