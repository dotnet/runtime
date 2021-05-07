// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    /// <summary>Base class providing tests for any Stream-derived type.</summary>
    [SkipOnPlatform(TestPlatforms.Browser, "lots of operations aren't supported on browser")]
    public abstract class StreamConformanceTests : FileCleanupTestBase
    {
        /// <summary>Gets the name of the byte[] argument to Read/Write methods.</summary>
        protected virtual string ReadWriteBufferName => "buffer";
        /// <summary>Gets the name of the int offset argument to Read/Write methods.</summary>
        protected virtual string ReadWriteOffsetName => "offset";
        /// <summary>Gets the name of the int count argument to Read/Write methods.</summary>
        protected virtual string ReadWriteCountName => "count";
        /// <summary>Gets the name of the IAsyncResult argument to EndRead/Write methods.</summary>
        protected virtual string ReadWriteAsyncResultName => "asyncResult";
        /// <summary>Gets the name of the Stream destination argument to CopyTo{Async}.</summary>
        protected virtual string CopyToStreamName => "destination";
        /// <summary>Gets the name of the int bufferSize argument to CopyTo{Async}.</summary>
        protected virtual string CopyToBufferSizeName => "bufferSize";

        /// <summary>Gets the type of exception thrown when an invalid IAsyncResult is passed to an EndRead/Write method.</summary>
        protected virtual Type InvalidIAsyncResultExceptionType => typeof(ArgumentException);
        /// <summary>Gets the type of exception thrown when a read or write operation is unsupported.</summary>
        protected virtual Type UnsupportedReadWriteExceptionType => typeof(NotSupportedException);
        /// <summary>Gets the type of exception thrown when a CopyTo{Async} operation is unsupported.</summary>
        protected virtual Type UnsupportedCopyExceptionType => typeof(NotSupportedException);
        /// <summary>Gets the type of exception thrown when setting a Read/WriteTimeout is unsupported.</summary>
        protected virtual Type UnsupportedTimeoutExceptionType => typeof(InvalidOperationException);
        /// <summary>
        /// Gets the type of exception thrown when an operation is invoked concurrently erroneously, or null if no exception
        /// is thrown (either because it's fully supported or not supported and non-deterministic).
        /// </summary>
        protected virtual Type UnsupportedConcurrentExceptionType => typeof(InvalidOperationException);

        /// <summary>Gets whether the stream is expected to be seekable.</summary>
        protected virtual bool CanSeek => false;
        /// <summary>Gets whether the stream is expected to support timeouts.</summary>
        protected virtual bool CanTimeout => false;
        /// <summary>Gets whether it's expected for the Position property to be usable even if CanSeek is false.</summary>
        protected virtual bool CanGetPositionWhenCanSeekIsFalse => false;
        /// <summary>Gets whether read/write operations fully support cancellation.</summary>
        protected virtual bool FullyCancelableOperations => true;
        /// <summary>Gets whether a read operation will always try to fill the full buffer provided.</summary>
        protected virtual bool ReadsReadUntilSizeOrEof => true;

        /// <summary>Gets whether the stream's CanRead/Write/etc properties are expected to return false once the stream is disposed.</summary>
        protected virtual bool CansReturnFalseAfterDispose => true;
        /// <summary>Gets whether the Stream may be used for additional operations after a read is canceled.</summary>
        protected virtual bool UsableAfterCanceledReads => true;
        protected virtual bool CanSetLength => CanSeek;
        protected virtual bool CanSetLengthGreaterThanCapacity => CanSetLength;

        /// <summary>Specifies the form of the read/write operation to use.</summary>
        public enum ReadWriteMode
        {
            /// <summary>ReadByte / WriteByte</summary>
            SyncByte,
            /// <summary>Read(Span{byte}) / Write(ReadOnlySpan{byte})</summary>
            SyncSpan,
            /// <summary>Read(byte[], int, int) / Write(byte[], int, int)</summary>
            SyncArray,
            /// <summary>ReadAsync(byte[], int, int) / WriteAsync(byte[], int, int)</summary>
            AsyncArray,
            /// <summary>ReadAsync(Memory{byte}) / WriteAsync(ReadOnlyMemory{byte})</summary>
            AsyncMemory,
            /// <summary>EndRead(BeginRead(..., null, null)) / EndWrite(BeginWrite(..., null, null))</summary>
            SyncAPM,
            /// <summary>Task.Factory.FromAsync(s.BeginRead, s.EndRead, ...) / Task.Factory.FromAsync(s.BeginWrite, s.EndWrite, ...)</summary>
            AsyncAPM
        }

        public static IEnumerable<object[]> AllReadWriteModes() =>
            from mode in Enum.GetValues<ReadWriteMode>()
            select new object[] { mode };

        public static IEnumerable<object[]> AllReadWriteModesAndValue(object value) =>
            from mode in Enum.GetValues<ReadWriteMode>()
            select new object[] { mode, value };

        /// <summary>Specifies the form of the seek operation to use.</summary>
        public enum SeekMode
        {
            /// <summary>Stream.Position = pos;</summary>
            Position,
            /// <summary>Stream.Seek(pos, SeekOrigin.Begin)</summary>
            SeekFromBeginning,
            /// <summary>Stream.Seek(pos - stream.Position, SeekOrigin.Current)</summary>
            SeekFromCurrent,
            /// <summary>Stream.Seek(pos - stream.Length, SeekOrigin.End)</summary>
            SeekFromEnd,
        }

        public static IEnumerable<object[]> AllSeekModes() =>
            from mode in Enum.GetValues<SeekMode>()
            select new object[] { mode };

        public static IEnumerable<object[]> AllSeekModesAndValue(object value) =>
            from mode in Enum.GetValues<SeekMode>()
            select new object[] { mode, value };

        protected async Task<int> ReadAsync(ReadWriteMode mode, Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
        {
            if (mode == ReadWriteMode.SyncByte)
            {
                if (count > 0)
                {
                    int b = stream.ReadByte();
                    if (b != -1)
                    {
                        buffer[offset] = (byte)b;
                        return 1;
                    }
                }

                return 0;
            }

            return mode switch
            {
                ReadWriteMode.SyncArray => stream.Read(buffer, offset, count),
                ReadWriteMode.SyncSpan => stream.Read(buffer.AsSpan(offset, count)),
                ReadWriteMode.AsyncArray => await stream.ReadAsync(buffer, offset, count, cancellationToken),
                ReadWriteMode.AsyncMemory => await stream.ReadAsync(buffer.AsMemory(offset, count), cancellationToken),
                ReadWriteMode.SyncAPM => stream.EndRead(stream.BeginRead(buffer, offset, count, null, null)),
                ReadWriteMode.AsyncAPM => await Task.Factory.FromAsync(stream.BeginRead, stream.EndRead, buffer, offset, count, null),
                _ => throw new Exception($"Unknown mode: {mode}"),
            };
        }

        protected async Task<int> ReadAllAsync(ReadWriteMode mode, Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
        {
            int bytesRead = 0;
            if (ReadsReadUntilSizeOrEof && mode != ReadWriteMode.SyncByte)
            {
                bytesRead = await ReadAsync(mode, stream, buffer, offset, count);
            }
            else
            {
                while (bytesRead < buffer.Length)
                {
                    int n = await ReadAsync(mode, stream, buffer, offset + bytesRead, count - bytesRead);
                    if (n == 0)
                    {
                        break;
                    }
                    Assert.InRange(n, 1, count - bytesRead);
                    bytesRead += n;
                }
            }

            return bytesRead;
        }

        protected async Task WriteAsync(ReadWriteMode mode, Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
        {
            switch (mode)
            {
                case ReadWriteMode.SyncByte:
                    for (int i = offset; i < offset + count; i++)
                    {
                        stream.WriteByte(buffer[i]);
                    }
                    break;

                case ReadWriteMode.SyncArray:
                    stream.Write(buffer, offset, count);
                    break;

                case ReadWriteMode.SyncSpan:
                    stream.Write(buffer.AsSpan(offset, count));
                    break;

                case ReadWriteMode.AsyncArray:
                    await stream.WriteAsync(buffer, offset, count, cancellationToken);
                    break;

                case ReadWriteMode.AsyncMemory:
                    await stream.WriteAsync(buffer.AsMemory(offset, count), cancellationToken);
                    break;

                case ReadWriteMode.SyncAPM:
                    stream.EndWrite(stream.BeginWrite(buffer, offset, count, null, null));
                    break;

                case ReadWriteMode.AsyncAPM:
                    await Task.Factory.FromAsync(stream.BeginWrite, stream.EndWrite, buffer, offset, count, null);
                    break;

                default:
                    throw new Exception($"Unknown mode: {mode}");
            }
        }

        protected async Task FlushAsync(ReadWriteMode mode, Stream stream, CancellationToken cancellationToken = default)
        {
            switch (mode)
            {
                case ReadWriteMode.SyncByte:
                case ReadWriteMode.SyncArray:
                case ReadWriteMode.SyncSpan:
                case ReadWriteMode.SyncAPM:
                    stream.Flush();
                    break;

                case ReadWriteMode.AsyncArray:
                case ReadWriteMode.AsyncMemory:
                case ReadWriteMode.AsyncAPM:
                    await stream.FlushAsync(cancellationToken);
                    break;

                default:
                    throw new Exception($"Unknown mode: {mode}");
            }
        }

        protected long Seek(SeekMode mode, Stream stream, long position)
        {
            long p;
            switch (mode)
            {
                case SeekMode.Position:
                    stream.Position = position;
                    p = stream.Position;
                    break;

                case SeekMode.SeekFromBeginning:
                    p = stream.Seek(position, SeekOrigin.Begin);
                    break;

                case SeekMode.SeekFromCurrent:
                    p = stream.Seek(position - stream.Position, SeekOrigin.Current);
                    break;

                case SeekMode.SeekFromEnd:
                    p = stream.Seek(position - stream.Length, SeekOrigin.End);
                    break;

                default:
                    throw new Exception($"Unknown mode: {mode}");
            }

            Assert.Equal(stream.Position, p);
            return p;
        }

        protected async Task ValidateMisuseExceptionsAsync(Stream stream)
        {
            byte[] oneByteBuffer = new byte[1];

            if (stream.CanRead)
            {
                // Null arguments
                foreach ((int offset, int count) in new[] { (0, 0), (1, 2) }) // validate 0, 0 isn't special-cased to be allowed with a null buffer
                {
                    AssertExtensions.Throws<ArgumentNullException>(ReadWriteBufferName, () => { stream.Read(null!, offset, count); });
                    AssertExtensions.Throws<ArgumentNullException>(ReadWriteBufferName, () => { stream.ReadAsync(null!, offset, count); });
                    AssertExtensions.Throws<ArgumentNullException>(ReadWriteBufferName, () => { stream.ReadAsync(null!, offset, count, default); });
                    AssertExtensions.Throws<ArgumentNullException>(ReadWriteBufferName, () => { stream.EndRead(stream.BeginRead(null!, offset, count, iar => { }, new object())); });
                    AssertExtensions.Throws<ArgumentNullException>(ReadWriteAsyncResultName, () => { stream.EndRead(null!); });
                }

                // Invalid offset
                AssertExtensions.Throws<ArgumentOutOfRangeException>(ReadWriteOffsetName, () => { stream.Read(oneByteBuffer, -1, 0); });
                AssertExtensions.Throws<ArgumentOutOfRangeException>(ReadWriteOffsetName, () => { stream.ReadAsync(oneByteBuffer, -1, 0); });
                AssertExtensions.Throws<ArgumentOutOfRangeException>(ReadWriteOffsetName, () => { stream.ReadAsync(oneByteBuffer, -1, 0, default); });
                AssertExtensions.Throws<ArgumentOutOfRangeException>(ReadWriteOffsetName, () => { stream.EndRead(stream.BeginRead(oneByteBuffer, -1, 0, iar => { }, new object())); });

                // Invalid count
                foreach (int count in new[] { -1, 2 })
                {
                    AssertExtensions.Throws<ArgumentOutOfRangeException>(ReadWriteCountName, () => { stream.Read(oneByteBuffer, 0, count); });
                    AssertExtensions.Throws<ArgumentOutOfRangeException>(ReadWriteCountName, () => { stream.ReadAsync(oneByteBuffer, 0, count); });
                    AssertExtensions.Throws<ArgumentOutOfRangeException>(ReadWriteCountName, () => { stream.ReadAsync(oneByteBuffer, 0, count, default); });
                    AssertExtensions.Throws<ArgumentOutOfRangeException>(ReadWriteCountName, () => { stream.EndRead(stream.BeginRead(oneByteBuffer, 0, count, iar => { }, new object())); });
                }

                // Invalid offset + count
                foreach ((int invalidOffset, int invalidCount) in new[] { (1, 1), (2, 0), (int.MaxValue, int.MaxValue) })
                {
                    AssertExtensions.Throws<ArgumentOutOfRangeException>(ReadWriteCountName, () => { stream.Read(oneByteBuffer, invalidOffset, invalidCount); });
                    AssertExtensions.Throws<ArgumentOutOfRangeException>(ReadWriteCountName, () => { stream.ReadAsync(oneByteBuffer, invalidOffset, invalidCount); });
                    AssertExtensions.Throws<ArgumentOutOfRangeException>(ReadWriteCountName, () => { stream.ReadAsync(oneByteBuffer, invalidOffset, invalidCount, default); });
                    AssertExtensions.Throws<ArgumentOutOfRangeException>(ReadWriteCountName, () => { stream.EndRead(stream.BeginRead(oneByteBuffer, invalidOffset, invalidCount, iar => { }, new object())); });
                }

                // Unknown arguments
                Assert.Throws(InvalidIAsyncResultExceptionType, () => stream.EndRead(new NotImplementedIAsyncResult()));

                // Invalid destination stream
                AssertExtensions.Throws<ArgumentNullException>(CopyToStreamName, () => { stream.CopyTo(null!); });
                AssertExtensions.Throws<ArgumentNullException>(CopyToStreamName, () => { stream.CopyTo(null!, 1); });
                AssertExtensions.Throws<ArgumentNullException>(CopyToStreamName, () => { stream.CopyToAsync(null!, default(CancellationToken)); });
                AssertExtensions.Throws<ArgumentNullException>(CopyToStreamName, () => { stream.CopyToAsync(null!, 1); });
                AssertExtensions.Throws<ArgumentNullException>(CopyToStreamName, () => { stream.CopyToAsync(null!, 1, default(CancellationToken)); });

                // Invalid buffer size
                var validDestinationStream = new MemoryStream();
                foreach (int invalidBufferSize in new[] { 0, -1 })
                {
                    AssertExtensions.Throws<ArgumentOutOfRangeException>(CopyToBufferSizeName, () => { stream.CopyTo(validDestinationStream, invalidBufferSize); });
                    AssertExtensions.Throws<ArgumentOutOfRangeException>(CopyToBufferSizeName, () => { stream.CopyToAsync(validDestinationStream, invalidBufferSize); });
                    AssertExtensions.Throws<ArgumentOutOfRangeException>(CopyToBufferSizeName, () => { stream.CopyToAsync(validDestinationStream, invalidBufferSize, default(CancellationToken)); });
                }

                // Unwriteable destination stream
                var unwriteableDestination = new MemoryStream(new byte[1], writable: false);
                Assert.Throws(UnsupportedCopyExceptionType, () => { stream.CopyTo(unwriteableDestination); });
                Assert.Throws(UnsupportedCopyExceptionType, () => { stream.CopyToAsync(unwriteableDestination); });

                // Disposed destination stream
                var disposedDestination = new MemoryStream(new byte[1]);
                disposedDestination.Dispose();
                Assert.Throws<ObjectDisposedException>(() => { stream.CopyTo(disposedDestination); });
                Assert.Throws<ObjectDisposedException>(() => { stream.CopyToAsync(disposedDestination); });
            }
            else
            {
                Assert.Throws(UnsupportedReadWriteExceptionType, () => { stream.ReadByte(); });
                Assert.Throws(UnsupportedReadWriteExceptionType, () => { stream.Read(new Span<byte>(new byte[1])); });
                Assert.Throws(UnsupportedReadWriteExceptionType, () => { stream.Read(new byte[1], 0, 1); });
                await Assert.ThrowsAsync(UnsupportedReadWriteExceptionType, () => stream.ReadAsync(new byte[1], 0, 1));
                await Assert.ThrowsAsync(UnsupportedReadWriteExceptionType, async () => await stream.ReadAsync(new Memory<byte>(new byte[1])));
                await Assert.ThrowsAsync(UnsupportedReadWriteExceptionType, () => Task.Factory.FromAsync(stream.BeginRead, stream.EndRead, new byte[1], 0, 1, null));
                Assert.True(Record.Exception(() => stream.EndRead(new NotImplementedIAsyncResult())) is Exception e && (e.GetType() == UnsupportedReadWriteExceptionType || e.GetType() == InvalidIAsyncResultExceptionType));
                Assert.Throws(UnsupportedCopyExceptionType, () => { stream.CopyTo(new MemoryStream()); });
                Assert.Throws(UnsupportedCopyExceptionType, () => { stream.CopyToAsync(new MemoryStream()); });
            }

            if (stream.CanWrite)
            {
                // Null arguments
                foreach ((int offset, int count) in new[] { (0, 0), (1, 2) }) // validate 0, 0 isn't special-cased to be allowed with a null buffer
                {
                    AssertExtensions.Throws<ArgumentNullException>(ReadWriteBufferName, () => { stream.Write(null!, offset, count); });
                    AssertExtensions.Throws<ArgumentNullException>(ReadWriteBufferName, () => { stream.WriteAsync(null!, offset, count); });
                    AssertExtensions.Throws<ArgumentNullException>(ReadWriteBufferName, () => { stream.WriteAsync(null!, offset, count, default); });
                    AssertExtensions.Throws<ArgumentNullException>(ReadWriteBufferName, () => { stream.EndWrite(stream.BeginWrite(null!, offset, count, iar => { }, new object())); });
                    AssertExtensions.Throws<ArgumentNullException>(ReadWriteAsyncResultName, () => { stream.EndWrite(null!); });
                }

                // Invalid offset
                AssertExtensions.Throws<ArgumentOutOfRangeException>(ReadWriteOffsetName, () => { stream.Write(oneByteBuffer, -1, 0); });
                AssertExtensions.Throws<ArgumentOutOfRangeException>(ReadWriteOffsetName, () => { stream.WriteAsync(oneByteBuffer, -1, 0); });
                AssertExtensions.Throws<ArgumentOutOfRangeException>(ReadWriteOffsetName, () => { stream.WriteAsync(oneByteBuffer, -1, 0, default); });
                AssertExtensions.Throws<ArgumentOutOfRangeException>(ReadWriteOffsetName, () => { stream.EndWrite(stream.BeginWrite(oneByteBuffer, -1, 0, iar => { }, new object())); });

                // Invalid count
                foreach (int count in new[] { -1, 2 })
                {
                    AssertExtensions.Throws<ArgumentOutOfRangeException>(ReadWriteCountName, () => { stream.Write(oneByteBuffer, 0, count); });
                    AssertExtensions.Throws<ArgumentOutOfRangeException>(ReadWriteCountName, () => { stream.WriteAsync(oneByteBuffer, 0, count); });
                    AssertExtensions.Throws<ArgumentOutOfRangeException>(ReadWriteCountName, () => { stream.WriteAsync(oneByteBuffer, 0, count, default); });
                    AssertExtensions.Throws<ArgumentOutOfRangeException>(ReadWriteCountName, () => { stream.EndWrite(stream.BeginWrite(oneByteBuffer, 0, count, iar => { }, new object())); });
                }

                // Invalid offset + count
                foreach ((int invalidOffset, int invalidCount) in new[] { (1, 1), (2, 0), (int.MaxValue, int.MaxValue) })
                {
                    Assert.ThrowsAny<ArgumentException>(() => { stream.Write(oneByteBuffer, invalidOffset, invalidCount); });
                    Assert.ThrowsAny<ArgumentException>(() => { stream.WriteAsync(oneByteBuffer, invalidOffset, invalidCount); });
                    Assert.ThrowsAny<ArgumentException>(() => { stream.WriteAsync(oneByteBuffer, invalidOffset, invalidCount, default); });
                    Assert.ThrowsAny<ArgumentException>(() => { stream.EndWrite(stream.BeginWrite(oneByteBuffer, invalidOffset, invalidCount, iar => { }, new object())); });
                }

                // Unknown arguments
                Assert.Throws(InvalidIAsyncResultExceptionType, () => stream.EndWrite(new NotImplementedIAsyncResult()));
            }
            else
            {
                Assert.Throws(UnsupportedReadWriteExceptionType, () => { stream.WriteByte(1); });
                Assert.Throws(UnsupportedReadWriteExceptionType, () => { stream.Write(new Span<byte>(new byte[1])); });
                Assert.Throws(UnsupportedReadWriteExceptionType, () => { stream.Write(new byte[1], 0, 1); });
                await Assert.ThrowsAsync(UnsupportedReadWriteExceptionType, () => stream.WriteAsync(new byte[1], 0, 1));
                await Assert.ThrowsAsync(UnsupportedReadWriteExceptionType, async () => await stream.WriteAsync(new Memory<byte>(new byte[1])));
                await Assert.ThrowsAsync(UnsupportedReadWriteExceptionType, () => Task.Factory.FromAsync(stream.BeginWrite, stream.EndWrite, new byte[1], 0, 1, null));
                Assert.True(Record.Exception(() => stream.EndWrite(new NotImplementedIAsyncResult())) is Exception e && (e.GetType() == UnsupportedReadWriteExceptionType || e.GetType() == InvalidIAsyncResultExceptionType));
            }

            Assert.Equal(CanSeek, stream.CanSeek);
            if (stream.CanSeek)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => { stream.Position = -1; });
                Assert.Throws<IOException>(() => { stream.Seek(-1, SeekOrigin.Begin); });
                Assert.Throws<IOException>(() => { stream.Seek(-stream.Position - 1, SeekOrigin.Current); });
                Assert.Throws<IOException>(() => { stream.Seek(-stream.Length - 1, SeekOrigin.End); });
                Assert.Throws<ArgumentException>(() => { stream.Seek(0, (SeekOrigin)(-1)); });
                Assert.Throws<ArgumentException>(() => { stream.Seek(0, (SeekOrigin)3); });
                Assert.Throws<ArgumentException>(() => { stream.Seek(0, ~SeekOrigin.Begin); });
                Assert.Throws<ArgumentOutOfRangeException>(() => { stream.SetLength(-1); });
            }
            else
            {
                Assert.Throws<NotSupportedException>(() => stream.Length);
                if (!CanGetPositionWhenCanSeekIsFalse)
                {
                    Assert.Throws<NotSupportedException>(() => stream.Position);
                }
                Assert.Throws<NotSupportedException>(() => { stream.Position = 0; });
                Assert.Throws<NotSupportedException>(() => { stream.SetLength(1); });
                Assert.Throws<NotSupportedException>(() => { stream.Seek(0, SeekOrigin.Begin); });
            }

            Assert.Equal(CanTimeout, stream.CanTimeout);
            if (stream.CanTimeout)
            {
                foreach (int timeout in new[] { 0, -2 })
                {
                    Assert.Throws<ArgumentOutOfRangeException>(() => stream.ReadTimeout = timeout);
                    Assert.Throws<ArgumentOutOfRangeException>(() => stream.WriteTimeout = timeout);
                }
            }
            else
            {
                Assert.Throws(UnsupportedTimeoutExceptionType, () => stream.ReadTimeout);
                Assert.Throws(UnsupportedTimeoutExceptionType, () => stream.ReadTimeout = 1);
                Assert.Throws(UnsupportedTimeoutExceptionType, () => stream.WriteTimeout);
                Assert.Throws(UnsupportedTimeoutExceptionType, () => stream.WriteTimeout = 1);
            }
        }

        protected async Task ValidateDisposedExceptionsAsync(Stream stream)
        {
            // Disposal should be idempotent and not throw
            stream.Dispose();
            stream.DisposeAsync().AsTask().GetAwaiter().GetResult();
            stream.Close();

            AssertDisposed(() => { stream.ReadByte(); });
            AssertDisposed(() => { stream.Read(new Span<byte>(new byte[1])); });
            AssertDisposed(() => { stream.Read(new byte[1], 0, 1); });
            await AssertDisposedAsync(async () => await stream.ReadAsync(new byte[1], 0, 1));
            await AssertDisposedAsync(async () => await stream.ReadAsync(new Memory<byte>(new byte[1])));
            AssertDisposed(() => { stream.EndRead(stream.BeginRead(new byte[1], 0, 1, null, null)); });

            AssertDisposed(() => { stream.WriteByte(1); });
            AssertDisposed(() => { stream.Write(new Span<byte>(new byte[1])); });
            AssertDisposed(() => { stream.Write(new byte[1], 0, 1); });
            await AssertDisposedAsync(async () => await stream.WriteAsync(new byte[1], 0, 1));
            await AssertDisposedAsync(async () => await stream.WriteAsync(new Memory<byte>(new byte[1])));
            AssertDisposed(() => { stream.EndWrite(stream.BeginWrite(new byte[1], 0, 1, null, null)); });

            AssertDisposed(() => stream.Flush(), successAllowed: true);
            await AssertDisposedAsync(() => stream.FlushAsync(), successAllowed: true);

            AssertDisposed(() => { stream.CopyTo(new MemoryStream()); });
            await AssertDisposedAsync(async () => await stream.CopyToAsync(new MemoryStream()));

            AssertDisposed(() => _ = stream.Length);
            AssertDisposed(() => _ = stream.Position);
            AssertDisposed(() => stream.Position = 0);
            AssertDisposed(() => stream.Seek(0, SeekOrigin.Begin));
            AssertDisposed(() => stream.SetLength(1));

            AssertDisposed(() => _ = stream.ReadTimeout);
            AssertDisposed(() => stream.ReadTimeout = 1);
            AssertDisposed(() => _ = stream.WriteTimeout);
            AssertDisposed(() => stream.WriteTimeout = 1);

            void AssertDisposed(Action action, bool successAllowed = false) => ValidateDisposedException(Record.Exception(action), successAllowed);

            async Task AssertDisposedAsync(Func<Task> func, bool successAllowed = false) => ValidateDisposedException(await Record.ExceptionAsync(func).ConfigureAwait(false), successAllowed);

            void ValidateDisposedException(Exception e, bool successAllowed = false)
            {
                // Behavior when disposed is inconsistent, and isn't specified by the Stream contract: types aren't supposed to be used
                // after they're disposed.  So, at least until we decide to be more strict, these tests are very liberal in what they except.
                Assert.True(
                    (e is null && successAllowed) ||
                    e is ObjectDisposedException ||
                    e is NotSupportedException ||
                    e is InvalidOperationException,
                    $"Unexpected: {e?.GetType().ToString() ?? "(null)"}");
            }
        }

        protected async Task AssertCanceledAsync(CancellationToken cancellationToken, Func<Task> testCode)
        {
            OperationCanceledException oce = await Assert.ThrowsAnyAsync<OperationCanceledException>(testCode);
            if (cancellationToken.CanBeCanceled)
            {
                Assert.Equal(cancellationToken, oce.CancellationToken);
            }
        }

        protected async Task ValidatePrecanceledOperations_ThrowsCancellationException(Stream stream)
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();

            if (stream.CanRead)
            {
                await AssertCanceledAsync(cts.Token, () => stream.ReadAsync(new byte[1], 0, 1, cts.Token));
                await AssertCanceledAsync(cts.Token, async () => { await stream.ReadAsync(new Memory<byte>(new byte[1]), cts.Token); });
            }

            if (stream.CanWrite)
            {
                await AssertCanceledAsync(cts.Token, () => stream.WriteAsync(new byte[1], 0, 1, cts.Token));
                await AssertCanceledAsync(cts.Token, async () => { await stream.WriteAsync(new ReadOnlyMemory<byte>(new byte[1]), cts.Token); });
            }

            Exception e = await Record.ExceptionAsync(() => stream.FlushAsync(cts.Token));
            if (e != null)
            {
                Assert.Equal(cts.Token, Assert.IsAssignableFrom<OperationCanceledException>(e).CancellationToken);
            }
        }

        protected async Task ValidateCancelableReadAsyncTask_AfterInvocation_ThrowsCancellationException(Stream stream)
        {
            if (!stream.CanRead || !FullyCancelableOperations)
            {
                return;
            }

            var cts = new CancellationTokenSource();
            Task<int> t = stream.ReadAsync(new byte[1], 0, 1, cts.Token);
            cts.Cancel();
            await AssertCanceledAsync(cts.Token, () => t);
        }

        protected async Task ValidateCancelableReadAsyncValueTask_AfterInvocation_ThrowsCancellationException(Stream stream)
        {
            if (!stream.CanRead || !FullyCancelableOperations)
            {
                return;
            }

            var cts = new CancellationTokenSource();
            Task<int> t = stream.ReadAsync(new byte[1], cts.Token).AsTask();
            cts.Cancel();
            await AssertCanceledAsync(cts.Token, () => t);
        }

        protected async Task WhenAllOrAnyFailed(Task task1, Task task2)
        {
            Task completed = await Task.WhenAny(task1, task2);
            Task incomplete = task1 == completed ? task2 : task1;
            if (completed.IsCompletedSuccessfully)
            {
                await incomplete;
            }
            else
            {
                try
                {
                    await incomplete.WaitAsync(TimeSpan.FromMilliseconds(500)); // give second task a chance to complete
                }
                catch (TimeoutException) { }

                await (incomplete.IsCompleted ? Task.WhenAll(completed, incomplete) : completed);
            }
        }

        protected sealed class NotImplementedIAsyncResult : IAsyncResult
        {
            public object AsyncState => throw new NotImplementedException();
            public WaitHandle AsyncWaitHandle => throw new NotImplementedException();
            public bool CompletedSynchronously => throw new NotImplementedException();
            public bool IsCompleted => throw new NotImplementedException();
        }

        protected sealed class CustomSynchronizationContext : SynchronizationContext
        {
            public override void Post(SendOrPostCallback d, object? state)
            {
                ThreadPool.QueueUserWorkItem(delegate
                {
                    SetSynchronizationContext(this);
                    try
                    {
                        d(state);
                    }
                    finally
                    {
                        SetSynchronizationContext(null);
                    }
                }, null);
            }
        }

        protected sealed class CustomTaskScheduler : TaskScheduler
        {
            protected override void QueueTask(Task task) => ThreadPool.QueueUserWorkItem(_ => TryExecuteTask(task));
            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) => false;
            protected override IEnumerable<Task> GetScheduledTasks() => new Task[0];
        }

        protected readonly struct JumpToThreadPoolAwaiter : ICriticalNotifyCompletion
        {
            public JumpToThreadPoolAwaiter GetAwaiter() => this;
            public bool IsCompleted => false;
            public void OnCompleted(Action continuation) => ThreadPool.QueueUserWorkItem(_ => continuation());
            public void UnsafeOnCompleted(Action continuation) => ThreadPool.UnsafeQueueUserWorkItem(_ => continuation(), null);
            public void GetResult() { }
        }

        protected sealed unsafe class NativeMemoryManager : MemoryManager<byte>
        {
            private readonly int _length;
            private IntPtr _ptr;
            public int PinRefCount;

            public NativeMemoryManager(int length) => _ptr = Marshal.AllocHGlobal(_length = length);

            ~NativeMemoryManager() => Assert.False(true, $"{nameof(NativeMemoryManager)} being finalized");

            public override Memory<byte> Memory => CreateMemory(_length);

            public override Span<byte> GetSpan() => new Span<byte>((void*)_ptr, _length);

            public override MemoryHandle Pin(int elementIndex = 0)
            {
                Interlocked.Increment(ref PinRefCount);
                Assert.InRange(elementIndex, 0, _length); // allows elementIndex == _length for zero-length instances
                return new MemoryHandle((byte*)_ptr + elementIndex, default, this);
            }

            public override void Unpin() => Interlocked.Decrement(ref PinRefCount);

            protected override void Dispose(bool disposing)
            {
                Marshal.FreeHGlobal(_ptr);
                _ptr = IntPtr.Zero;
            }
        }
    }

    public abstract class StandaloneStreamConformanceTests : StreamConformanceTests
    {
        protected override bool CanSeek => true;
        protected virtual bool NopFlushCompletesSynchronously => true;

        protected abstract Task<Stream?> CreateReadOnlyStreamCore(byte[]? initialData);
        protected async Task<Stream?> CreateReadOnlyStream(byte[]? initialData = null)
        {
            Stream? stream = await CreateReadOnlyStreamCore(initialData);
            if (stream is not null)
            {
                Assert.True(stream.CanRead);
                Assert.False(stream.CanWrite);
                if (CanSeek)
                {
                    Assert.Equal(0, stream.Position);
                    Assert.Equal(initialData?.Length ?? 0, stream.Length);
                }
            }
            return stream;
        }

        protected abstract Task<Stream?> CreateWriteOnlyStreamCore(byte[]? initialData);
        protected async Task<Stream?> CreateWriteOnlyStream(byte[]? initialData = null)
        {
            Stream? stream = await CreateWriteOnlyStreamCore(initialData);
            if (stream is not null)
            {
                Assert.False(stream.CanRead);
                Assert.True(stream.CanWrite);
                if (CanSeek)
                {
                    Assert.Equal(0, stream.Position);
                    Assert.Equal(initialData?.Length ?? 0, stream.Length);
                }
            }
            return stream;
        }

        protected abstract Task<Stream?> CreateReadWriteStreamCore(byte[]? initialData);
        protected async Task<Stream?> CreateReadWriteStream(byte[]? initialData = null)
        {
            Stream? stream = await CreateReadWriteStreamCore(initialData);
            if (stream is not null)
            {
                Assert.True(stream.CanRead);
                Assert.True(stream.CanWrite);
                if (CanSeek)
                {
                    Assert.Equal(0, stream.Position);
                    Assert.Equal(initialData?.Length ?? 0, stream.Length);
                }
            }
            return stream;
        }

        protected async IAsyncEnumerable<Stream?> GetStreamsForValidation()
        {
            yield return await CreateReadOnlyStream();
            yield return await CreateReadOnlyStream(new byte[4]);

            yield return await CreateWriteOnlyStream();
            yield return await CreateWriteOnlyStream(new byte[4]);

            yield return await CreateReadWriteStream();
            yield return await CreateReadWriteStream(new byte[4]);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51371", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public virtual async Task ArgumentValidation_ThrowsExpectedException()
        {
            await foreach (Stream? stream in GetStreamsForValidation())
            {
                if (stream != null)
                {
                    using var _ = stream;
                    await ValidateMisuseExceptionsAsync(stream);
                }
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51371", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public virtual async Task Disposed_ThrowsObjectDisposedException()
        {
            await foreach (Stream? stream in GetStreamsForValidation())
            {
                if (stream != null)
                {
                    using var _ = stream;
                    await ValidateDisposedExceptionsAsync(stream);
                }
            }
        }

        [Fact]
        public virtual async Task ReadWriteAsync_Precanceled_ThrowsOperationCanceledException()
        {
            await foreach (Stream? stream in GetStreamsForValidation())
            {
                if (stream != null)
                {
                    using var _ = stream;
                    await ValidatePrecanceledOperations_ThrowsCancellationException(stream);
                }
            }
        }

        [Theory]
        [MemberData(nameof(AllReadWriteModes))]
        public virtual async Task Read_NonEmptyStream_Nop_Success(ReadWriteMode mode)
        {
            using Stream? stream = await CreateReadOnlyStream(new byte[10]);
            if (stream is null)
            {
                return;
            }

            Assert.Equal(0, await ReadAsync(mode, stream, Array.Empty<byte>(), 0, 0));
            Assert.Equal(0, await ReadAsync(mode, stream, new byte[0], 0, 0));
            Assert.Equal(0, await ReadAsync(mode, stream, new byte[1], 0, 0));
            Assert.Equal(0, await ReadAsync(mode, stream, new byte[1], 1, 0));
        }

        [Theory]
        [MemberData(nameof(AllReadWriteModes))]
        public virtual async Task Write_Nop_Success(ReadWriteMode mode)
        {
            using Stream? stream = await CreateReadWriteStream();
            if (stream is null)
            {
                return;
            }

            await WriteAsync(mode, stream, Array.Empty<byte>(), 0, 0);
            await WriteAsync(mode, stream, new byte[0], 0, 0);
            await WriteAsync(mode, stream, new byte[1], 0, 0);
            await WriteAsync(mode, stream, new byte[1], 1, 0);

            Assert.Equal(0, stream.Position);
            Assert.Equal(0, stream.Length);
        }

        [Theory]
        [InlineData(ReadWriteMode.SyncArray)]
        [InlineData(ReadWriteMode.AsyncArray)]
        [InlineData(ReadWriteMode.AsyncAPM)]
        public virtual async Task Read_DataStoredAtDesiredOffset(ReadWriteMode mode)
        {
            const byte Expected = 42;

            using Stream? stream = await CreateReadWriteStream(new byte[] { Expected });
            if (stream is null)
            {
                return;
            }

            byte[] buffer = new byte[10];
            int offset = 2;

            Assert.Equal(1, await ReadAsync(mode, stream, buffer, offset, buffer.Length - offset));

            for (int i = 0; i < buffer.Length; i++)
            {
                Assert.Equal(i == offset ? Expected : 0, buffer[i]);
            }
        }

        [Theory]
        [InlineData(ReadWriteMode.SyncArray)]
        [InlineData(ReadWriteMode.AsyncArray)]
        [InlineData(ReadWriteMode.AsyncAPM)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51371", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public virtual async Task Write_DataReadFromDesiredOffset(ReadWriteMode mode)
        {
            using Stream? stream = await CreateReadWriteStream();
            if (stream is null)
            {
                return;
            }

            await WriteAsync(mode, stream, new[] { (byte)'a', (byte)'b', (byte)'h', (byte)'e', (byte)'l', (byte)'l', (byte)'o', (byte)'c', (byte)'d' }, 2, 5);
            stream.Position = 0;

            using StreamReader reader = new StreamReader(stream);
            Assert.Equal("hello", reader.ReadToEnd());
        }

        [Theory]
        [MemberData(nameof(AllReadWriteModes))]
        public virtual async Task Read_EmptyStream_Nop_Success(ReadWriteMode mode)
        {
            using Stream? stream = await CreateReadOnlyStream();
            if (stream is null)
            {
                return;
            }

            Assert.Equal(0, await ReadAsync(mode, stream, Array.Empty<byte>(), 0, 0));
            Assert.Equal(0, await ReadAsync(mode, stream, new byte[0], 0, 0));
            Assert.Equal(0, await ReadAsync(mode, stream, new byte[1], 0, 0));
            Assert.Equal(0, await ReadAsync(mode, stream, new byte[1], 1, 0));

            Assert.Equal(0, await ReadAsync(mode, stream, new byte[4], 0, 4));
            Assert.Equal(0, await ReadAsync(mode, stream, new byte[4], 1, 3));
        }

        [Theory]
        [MemberData(nameof(AllReadWriteModesAndValue), 1)]
        [MemberData(nameof(AllReadWriteModesAndValue), 256)]
        public virtual async Task Read_PopulatedWithInitialData_KnownSize_Success(ReadWriteMode mode, int size)
        {
            byte[] expected = RandomNumberGenerator.GetBytes(size);

            using Stream? stream = await CreateReadOnlyStream(expected);
            if (stream is null)
            {
                return;
            }

            byte[] actual = new byte[expected.Length];
            int bytesRead = 0;
            while (bytesRead < actual.Length)
            {
                int n = await ReadAsync(mode, stream, actual, bytesRead, actual.Length - bytesRead);
                Assert.InRange(n, 1, actual.Length - bytesRead);
                bytesRead += n;
            }

            if (CanSeek)
            {
                Assert.Equal(size, stream.Position);
                Assert.Equal(size, stream.Seek(0, SeekOrigin.Current));
            }

            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(AllReadWriteModesAndValue), 1)]
        [MemberData(nameof(AllReadWriteModesAndValue), 4097)]
        public virtual async Task Read_PopulatedWithInitialData_ToEof_Success(ReadWriteMode mode, int size)
        {
            byte[] expected = RandomNumberGenerator.GetBytes(size);

            using Stream? stream = await CreateReadOnlyStream(expected);
            if (stream is null)
            {
                return;
            }

            var actual = new MemoryStream();

            byte[] buffer = new byte[8];
            int bytesRead = 0;
            while ((bytesRead = await ReadAsync(mode, stream, buffer, 0, buffer.Length)) != 0)
            {
                Assert.InRange(bytesRead, 1, buffer.Length);
                actual.Write(buffer, 0, bytesRead);
            }
            Assert.Equal(0, bytesRead);

            if (CanSeek)
            {
                Assert.Equal(size, stream.Position);
                Assert.Equal(size, stream.Seek(0, SeekOrigin.Current));
            }

            AssertExtensions.SequenceEqual(expected, actual.ToArray());
        }

        [Theory]
        [MemberData(nameof(AllReadWriteModes))]
        public virtual async Task Read_PartiallySatisfied_RemainderOfBufferUntouched(ReadWriteMode mode)
        {
            byte[] expected = RandomNumberGenerator.GetBytes(20);

            using Stream? stream = await CreateReadOnlyStream(expected);
            if (stream is null)
            {
                return;
            }

            byte[] buffer = new byte[expected.Length + 10];
            const int Offset = 3;
            int bytesRead = await ReadAsync(mode, stream, buffer, Offset, buffer.Length - Offset);

            for (int i = 0; i < Offset; i++)
            {
                Assert.Equal(0, buffer[i]);
            }
            for (int i = Offset; i < Offset + bytesRead; i++)
            {
                Assert.Equal(expected[i - Offset], buffer[i]);
            }
            for (int i = Offset + bytesRead; i < buffer.Length; i++)
            {
                Assert.Equal(0, buffer[i]);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public virtual async Task Read_CustomMemoryManager_Success(bool useAsync)
        {
            byte[] expected = RandomNumberGenerator.GetBytes(20);

            using Stream? stream = await CreateReadOnlyStream(expected);
            if (stream is null)
            {
                return;
            }

            using MemoryManager<byte> memoryManager = new NativeMemoryManager(1024);
            Assert.Equal(1024, memoryManager.Memory.Length);

            int bytesRead = useAsync ?
                await stream.ReadAsync(memoryManager.Memory) :
                stream.Read(memoryManager.Memory.Span);
            Assert.InRange(bytesRead, 1, memoryManager.Memory.Length);

            Assert.True(expected.AsSpan(0, bytesRead).SequenceEqual(memoryManager.Memory.Span.Slice(0, bytesRead)));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public virtual async Task Write_CustomMemoryManager_Success(bool useAsync)
        {
            using Stream? stream = await CreateReadWriteStream();
            if (stream is null)
            {
                return;
            }

            using MemoryManager<byte> memoryManager = new NativeMemoryManager(256);
            Assert.Equal(256, memoryManager.Memory.Length);
            byte[] expected = RandomNumberGenerator.GetBytes(memoryManager.Memory.Length);
            expected.AsSpan().CopyTo(memoryManager.Memory.Span);

            if (useAsync)
            {
                await stream.WriteAsync(memoryManager.Memory);
            }
            else
            {
                stream.Write(memoryManager.Memory.Span);
            }

            stream.Position = 0;
            byte[] actual = (byte[])expected.Clone();
            Assert.Equal(actual.Length, await ReadAllAsync(ReadWriteMode.AsyncMemory, stream, actual, 0, actual.Length));
            AssertExtensions.SequenceEqual(expected, actual);
        }

        [Theory]
        [MemberData(nameof(CopyTo_CopiesAllDataFromRightPosition_Success_MemberData))]
        public virtual async Task CopyTo_CopiesAllDataFromRightPosition_Success(
            bool useAsync, byte[] expected, int position)
        {
            using Stream? stream = await CreateReadOnlyStream(expected);
            if (stream is null)
            {
                return;
            }

            if (stream.CanSeek)
            {
                stream.Position = position;
            }
            else
            {
                for (int i = 0; i < position; i++)
                {
                    Assert.NotEqual(-1, stream.ReadByte());
                }
            }

            var destination = new MemoryStream();
            if (useAsync)
            {
                await stream.CopyToAsync(destination);
            }
            else
            {
                stream.CopyTo(destination);
            }

            if (stream.CanSeek)
            {
                Assert.Equal(expected.Length, stream.Length);
                Assert.Equal(expected.Length, stream.Position);
            }

            AssertExtensions.SequenceEqual(expected.AsSpan(position).ToArray(), destination.ToArray());
        }

        public static IEnumerable<object[]> CopyTo_CopiesAllDataFromRightPosition_Success_MemberData()
        {
            byte[] expected = RandomNumberGenerator.GetBytes(16 * 1024);
            foreach (bool useAsync in new[] { false, true })
            {
                yield return new object[] { useAsync, expected, 0 }; // beginning
                yield return new object[] { useAsync, expected, 1 }; // just past beginning
                yield return new object[] { useAsync, expected, expected.Length / 2 }; // middle
                yield return new object[] { useAsync, expected, expected.Length }; // end
            }
        }

        [Theory]
        [MemberData(nameof(AllReadWriteModes))]
        public virtual async Task Write_Read_Success(ReadWriteMode mode)
        {
            const int Length = 1024;

            using Stream? stream = await CreateReadWriteStream();
            if (stream is null)
            {
                return;
            }

            byte[] expected = RandomNumberGenerator.GetBytes(Length);

            const int Copies = 3;
            for (int i = 0; i < Copies; i++)
            {
                await WriteAsync(mode, stream, expected, 0, expected.Length);
            }

            stream.Position = 0;

            byte[] actual = new byte[expected.Length];
            for (int i = 0; i < Copies; i++)
            {
                int bytesRead = await ReadAllAsync(mode, stream, actual, 0, actual.Length);
                AssertExtensions.SequenceEqual(expected, actual);
                Array.Clear(actual, 0, actual.Length);
            }
        }

        [Theory]
        [MemberData(nameof(AllReadWriteModes))]
        public virtual async Task Flush_ReadOnly_DoesntImpactReading(ReadWriteMode mode)
        {
            using Stream? stream = await CreateReadOnlyStream(new byte[] { 0, 1, 2, 3, 4, 5 });
            if (stream is null)
            {
                return;
            }

            var buffer = new byte[1];
            for (int i = 0; i <= 5; i++)
            {
                await FlushAsync(mode, stream);
                Assert.Equal(1, await ReadAsync(mode, stream, buffer, 0, buffer.Length));
                Assert.Equal(i, buffer[0]);
            }
        }

        [Theory]
        [InlineData(ReadWriteMode.SyncArray)]
        [InlineData(ReadWriteMode.AsyncArray)]
        public virtual async Task Flush_MultipleTimes_Idempotent(ReadWriteMode mode)
        {
            using Stream? stream = await CreateReadWriteStream();
            if (stream is null)
            {
                return;
            }

            await FlushAsync(mode, stream);
            await FlushAsync(mode, stream);

            stream.WriteByte(42);

            await FlushAsync(mode, stream);
            await FlushAsync(mode, stream);

            stream.Position = 0;

            await FlushAsync(mode, stream);
            await FlushAsync(mode, stream);

            Assert.Equal(42, stream.ReadByte());

            await FlushAsync(mode, stream);
            await FlushAsync(mode, stream);
        }

        [Fact]
        public virtual async Task Flush_SetLengthAtEndOfBuffer_OperatesOnValidData()
        {
            if (!CanSeek || !CanSetLengthGreaterThanCapacity)
            {
                return;
            }

            using Stream? stream = await CreateReadWriteStream();
            if (stream is null)
            {
                return;
            }

            stream.SetLength(200);
            stream.Flush();

            // write 119 bytes starting from Pos = 28
            stream.Seek(28, SeekOrigin.Begin);
            byte[] buffer = new byte[119];
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = byte.MaxValue;
            }
            stream.Write(buffer, 0, buffer.Length);
            stream.Flush();

            // read 317 bytes starting from Pos = 84;
            stream.Seek(84, SeekOrigin.Begin);
            stream.Read(new byte[1024], 0, 317);

            stream.SetLength(135);
            stream.Flush();

            // read one byte at Pos = 97
            stream.Seek(97, SeekOrigin.Begin);
            Assert.Equal(stream.ReadByte(), (int)byte.MaxValue);
        }

        [Fact]
        public virtual async Task Flush_NothingToFlush_CompletesSynchronously()
        {
            if (!NopFlushCompletesSynchronously)
            {
                return;
            }

            using Stream? stream = await CreateReadWriteStream();
            if (stream is null)
            {
                return;
            }

            Assert.True(stream.FlushAsync().IsCompletedSuccessfully);
            Assert.True(stream.FlushAsync(CancellationToken.None).IsCompletedSuccessfully);
            Assert.True(stream.FlushAsync(new CancellationTokenSource().Token).IsCompletedSuccessfully);
        }

        [Fact]
        public virtual async Task DisposeAsync_NothingToFlush_CompletesSynchronously()
        {
            using Stream? stream = await CreateReadWriteStream();
            if (stream is null)
            {
                return;
            }

            Assert.True(stream.DisposeAsync().IsCompletedSuccessfully);
        }

        [Fact]
        public virtual async Task Seek_Offset0FromMark_Success()
        {
            if (!CanSeek)
            {
                return;
            }

            using Stream? stream = await CreateReadOnlyStream(new byte[10]);
            if (stream is null)
            {
                return;
            }

            Assert.Equal(0, stream.Seek(0, SeekOrigin.Begin));
            Assert.Equal(stream.Length, stream.Seek(0, SeekOrigin.End));
            Assert.Equal(4, stream.Seek(4, SeekOrigin.Begin));
            Assert.Equal(1, stream.Seek(-3, SeekOrigin.Current));
            Assert.Equal(0, stream.Seek(0, SeekOrigin.Begin));
        }

        [Fact]
        public virtual async Task Seek_RandomWalk_ReadConsistency()
        {
            using Stream? stream = await CreateReadWriteStream();
            if (stream is null)
            {
                return;
            }

            var rand = new Random(42); // fixed seed to enable repeatable runs
            const int Trials = 1000;
            const int FileLength = 0x4000;
            const int MaxBytesToRead = 21;

            // Write data to the file
            byte[] buffer = RandomNumberGenerator.GetBytes(FileLength);
            stream.Write(buffer, 0, buffer.Length);
            Assert.Equal(buffer.Length, stream.Position);
            Assert.Equal(buffer.Length, stream.Length);
            stream.Position = 0;

            // Repeatedly jump around, reading, and making sure we get the right data back
            for (int trial = 0; trial < Trials; trial++)
            {
                // Pick some number of bytes to read
                int bytesToRead = rand.Next(1, MaxBytesToRead);

                // Jump to a random position, seeking either from one of the possible origins
                SeekOrigin origin = (SeekOrigin)rand.Next(3);
                long pos = stream.Seek(origin switch
                {
                    SeekOrigin.Begin => rand.Next(0, (int)stream.Length - bytesToRead),
                    SeekOrigin.Current => rand.Next(-(int)stream.Position + bytesToRead, (int)stream.Length - (int)stream.Position - bytesToRead),
                    _ => -rand.Next(bytesToRead, (int)stream.Length),
                }, origin);
                Assert.InRange(pos, 0, stream.Length - bytesToRead);

                // Read the requested number of bytes, and verify each is correct
                for (int i = 0; i < bytesToRead; i++)
                {
                    int byteRead = stream.ReadByte();
                    Assert.Equal(buffer[pos + i], byteRead);
                }
            }
        }

        [Theory]
        [MemberData(nameof(AllSeekModes))]
        public virtual async Task Seek_Read_RoundtripsExpectedData(SeekMode mode)
        {
            if (!CanSeek)
            {
                return;
            }

            const int Length = 512;

            byte[] expected = RandomNumberGenerator.GetBytes(Length);
            using Stream? stream = await CreateReadOnlyStream(expected);
            if (stream is null)
            {
                return;
            }

            Assert.Equal(Length, stream.Length);
            foreach (long pos in new[] { Length - 1, 0, Length / 2, 1 })
            {
                Assert.Equal(pos, Seek(mode, stream, pos));
                Assert.Equal(expected[pos], (byte)stream.ReadByte());
            }
        }

        [Theory]
        [MemberData(nameof(AllSeekModes))]
        public virtual async Task Seek_ReadWrite_RoundtripsExpectedData(SeekMode mode)
        {
            if (!CanSeek)
            {
                return;
            }

            const int Length = 512;

            byte[] expected = RandomNumberGenerator.GetBytes(Length);
            using Stream? stream = await CreateReadWriteStream(expected);
            if (stream is null)
            {
                return;
            }

            Assert.Equal(Length, stream.Length);

            var rand = new Random(42);
            foreach (long pos in new[] { Length - 1, 0, Length / 2, 1 })
            {
                Assert.Equal(pos, Seek(mode, stream, pos));
                Assert.Equal(expected[pos], (byte)stream.ReadByte());

                Seek(mode, stream, pos);
                byte b = (byte)rand.Next(0, 256);
                stream.WriteByte(b);
                Assert.Equal(pos + 1, stream.Position);

                Seek(mode, stream, pos);
                Assert.Equal(b, stream.ReadByte());
                Assert.Equal(pos + 1, stream.Position);
            }
        }

        [Fact]
        public virtual async Task SetLength_FailsForReadOnly_Throws()
        {
            using Stream? stream = await CreateReadOnlyStream(new byte[4]);
            if (stream is null)
            {
                return;
            }

            if (stream.CanSeek)
            {
                Assert.Equal(0, stream.Position);
            }

            Assert.Throws<NotSupportedException>(() => stream.SetLength(3));
            Assert.Throws<NotSupportedException>(() => stream.SetLength(4));
            if (CanSetLengthGreaterThanCapacity)
            {
                Assert.Throws<NotSupportedException>(() => stream.SetLength(5));
            }

            if (stream.CanSeek)
            {
                Assert.Equal(0, stream.Position);
            }
        }

        [Fact]
        public virtual async Task SetLength_FailsForWritableIfApplicable_Throws()
        {
            if (CanSetLength)
            {
                return;
            }

            using Stream? stream = await CreateReadWriteStream(new byte[4]);
            if (stream is null)
            {
                return;
            }

            if (stream.CanSeek)
            {
                Assert.Equal(0, stream.Position);
            }

            Assert.Throws<NotSupportedException>(() => stream.SetLength(3));
            Assert.Throws<NotSupportedException>(() => stream.SetLength(4));
            Assert.Throws<NotSupportedException>(() => stream.SetLength(5));

            if (stream.CanSeek)
            {
                Assert.Equal(0, stream.Position);
            }
        }

        [Fact]
        public virtual async Task SetLength_ChangesLengthAccordingly_Success()
        {
            if (!CanSetLength)
            {
                return;
            }

            using Stream? stream = await CreateReadWriteStream();
            if (stream is null)
            {
                return;
            }

            Assert.Equal(0, stream.Length);
            Assert.Equal(0, stream.Position);

            stream.SetLength(4);

            Assert.Equal(4, stream.Length);
            Assert.Equal(0, stream.Position);

            stream.Write(new byte[] { 1, 2, 3, 4, 5, 6 }, 0, 6);
            Assert.Equal(6, stream.Length);
            Assert.Equal(6, stream.Position);

            stream.SetLength(3);
            Assert.Equal(3, stream.Length);
            Assert.Equal(3, stream.Position);

            Assert.Equal(-1, stream.ReadByte());
            Assert.Equal(3, stream.Position);

            stream.WriteByte(42);
            Assert.Equal(4, stream.Length);
            Assert.Equal(4, stream.Position);
        }

        [Theory]
        [MemberData(nameof(AllReadWriteModes))]
        public virtual async Task SetLength_DataFromShrinkNotPreserved_Success(ReadWriteMode mode)
        {
            if (!CanSeek || !CanSetLength)
            {
                return;
            }

            byte[] expected = new byte[] { 0, 1, 2, 3 };
            byte[] actual = new byte[expected.Length];

            using Stream? stream = await CreateReadWriteStream(expected);
            if (stream is null)
            {
                return;
            }

            Assert.Equal(4, stream.Length);
            stream.SetLength(4);
            Assert.Equal(4, stream.Length);
            Assert.Equal(0, stream.Position);
            Assert.Equal(4, await ReadAllAsync(mode, stream, actual, 0, actual.Length));
            Assert.Equal(expected, actual);

            Array.Clear(actual, 0, actual.Length);

            stream.SetLength(3);
            Assert.Equal(3, stream.Position);
            stream.Position = 0;
            Assert.Equal(3, await ReadAllAsync(mode, stream, actual, 0, actual.Length));
            Assert.Equal(new byte[] { 0, 1, 2 }, actual.AsSpan(0, 3).ToArray());

            Array.Clear(actual, 0, actual.Length);

            stream.SetLength(4);
            stream.Position = 0;
            Assert.Equal(4, await ReadAllAsync(mode, stream, actual, 0, actual.Length));
            Assert.Equal(new byte[] { 0, 1, 2, 0 }, actual);
        }

        [Fact]
        public async Task SetLength_MaxValue_ThrowsExpectedException()
        {
            if (!CanSetLengthGreaterThanCapacity)
            {
                return;
            }

            using Stream? stream = await CreateReadWriteStream();
            if (stream is null)
            {
                return;
            }

            // Depending on the stream (or file system involved), this can:
            // - Throw IOException : No space left on device
            // - Throw ArgumentOutOfRangeException : Specified file length was too large for the file system.
            // - Succeed.
            try
            {
                stream.SetLength(long.MaxValue);
            }
            catch (Exception e)
            {
                Assert.True(e is IOException || e is ArgumentOutOfRangeException, $"Unexpected exception {e}");
                return;
            }

            Assert.Equal(long.MaxValue, stream.Length);
        }

        [Theory]
        [MemberData(nameof(AllReadWriteModes))]
        public virtual async Task SeekPastEnd_Write_BeyondCapacity(ReadWriteMode mode)
        {
            if (!CanSeek)
            {
                return;
            }

            using Stream? stream = await CreateReadWriteStream();
            if (stream is null)
            {
                return;
            }

            long origLength = stream.Length;

            byte[] expected = RandomNumberGenerator.GetBytes(10);

            // Move past end; doesn't change stream length.
            int pastEnd = 5;
            stream.Seek(pastEnd, SeekOrigin.End);
            Assert.Equal(stream.Length + pastEnd, stream.Position);
            stream.Position = stream.Position; // nop change
            Assert.Equal(stream.Length + pastEnd, stream.Position);

            // Write bytes
            await WriteAsync(mode, stream, expected, 0, expected.Length);
            Assert.Equal(origLength + pastEnd + expected.Length, stream.Position);
            Assert.Equal(origLength + pastEnd + expected.Length, stream.Length);

            // Read them back and validate
            stream.Position = origLength;
            byte[] actual = new byte[expected.Length + pastEnd];
            Assert.Equal(actual.Length, await ReadAllAsync(mode, stream, actual, 0, actual.Length));
            for (int i = 0; i < pastEnd; i++)
            {
                Assert.Equal(0, actual[i]);
            }
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], actual[i + pastEnd]);
            }
        }
    }

    /// <summary>Base class providing tests for two streams connected to each other such that writing to one is readable from the other, and vice versa.</summary>
    public abstract class ConnectedStreamConformanceTests : StreamConformanceTests
    {
        /// <summary>Gets whether ValueTasks returned from Read/WriteAsync methods are expected to be consumable only once.</summary>
        protected virtual bool ReadWriteValueTasksProtectSingleConsumption => false;
        /// <summary>Gets whether writes on a connected stream are expected to fail immediately after a reader is disposed.</summary>
        protected virtual bool BrokenPipePropagatedImmediately => false;
        /// <summary>Gets the amount of data a writer is able to buffer before blocking subsequent writes, or -1 if there's no such limit known.</summary>
        protected virtual int BufferedSize => -1;
        /// <summary>
        /// Gets whether the stream requires Flush{Async} to be called in order to send written data to the underlying destination.
        /// </summary>
        protected virtual bool FlushRequiredToWriteData => true;
        /// <summary>
        /// Gets whether the stream guarantees that all data written to it will be flushed as part of Flush{Async}.
        /// </summary>
        protected virtual bool FlushGuaranteesAllDataWritten => true;
        /// <summary>
        /// Gets whether a stream implements an aggressive read that tries to fill the supplied buffer and only
        /// stops when it does so or hits EOF.
        /// </summary>
        protected virtual bool ReadsMayBlockUntilBufferFullOrEOF => false;
        /// <summary>Gets whether reads for a count of 0 bytes block if no bytes are available to read.</summary>
        protected virtual bool BlocksOnZeroByteReads => false;
        /// <summary>
        /// Gets whether an otherwise bidirectional stream does not support reading/writing concurrently, e.g. due to a semaphore in the base implementation.
        /// </summary>
        protected virtual bool SupportsConcurrentBidirectionalUse => true;

        protected abstract Task<StreamPair> CreateConnectedStreamsAsync();

        protected (Stream writeable, Stream readable) GetReadWritePair(StreamPair streams) =>
            GetReadWritePairs(streams).First();

        protected IEnumerable<(Stream writeable, Stream readable)> GetReadWritePairs(StreamPair streams)
        {
            var pairs = new List<(Stream, Stream)>(2);

            if (streams.Stream1.CanWrite)
            {
                Assert.True(streams.Stream2.CanRead);
                pairs.Add((streams.Stream1, streams.Stream2));
            }

            if (streams.Stream2.CanWrite)
            {
                Assert.True(streams.Stream1.CanRead);
                pairs.Add((streams.Stream2, streams.Stream1));
            }

            Assert.InRange(pairs.Count, 1, 2);
            return pairs;
        }

        protected static bool Bidirectional(StreamPair streams) =>
            streams.Stream1.CanRead && streams.Stream1.CanWrite &&
            streams.Stream2.CanRead && streams.Stream2.CanWrite;

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51371", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public virtual async Task ArgumentValidation_ThrowsExpectedException()
        {
            using StreamPair streams = await CreateConnectedStreamsAsync();

            foreach (Stream stream in streams)
            {
                await ValidateMisuseExceptionsAsync(stream);
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51371", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public virtual async Task Disposed_ThrowsObjectDisposedException()
        {
            StreamPair streams = await CreateConnectedStreamsAsync();
            streams.Dispose();

            foreach (Stream stream in streams)
            {
                await ValidateDisposedExceptionsAsync(stream);
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51371", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public virtual async Task ReadWriteAsync_PrecanceledOperations_ThrowsCancellationException()
        {
            using StreamPair streams = await CreateConnectedStreamsAsync();

            foreach (Stream stream in streams)
            {
                await ValidatePrecanceledOperations_ThrowsCancellationException(stream);
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51371", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public virtual async Task ReadAsync_CancelPendingTask_ThrowsCancellationException()
        {
            using StreamPair streams = await CreateConnectedStreamsAsync();
            (Stream writeable, Stream readable) = GetReadWritePair(streams);

            await ValidateCancelableReadAsyncTask_AfterInvocation_ThrowsCancellationException(readable);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51371", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public virtual async Task ReadAsync_CancelPendingValueTask_ThrowsCancellationException()
        {
            using StreamPair streams = await CreateConnectedStreamsAsync();
            (Stream writeable, Stream readable) = GetReadWritePair(streams);

            await ValidateCancelableReadAsyncValueTask_AfterInvocation_ThrowsCancellationException(readable);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51371", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public virtual async Task ReadWriteByte_Success()
        {
            using StreamPair streams = await CreateConnectedStreamsAsync();

            foreach ((Stream writeable, Stream readable) in GetReadWritePairs(streams))
            {
                byte[] writerBytes = RandomNumberGenerator.GetBytes(42);
                var readerBytes = new byte[writerBytes.Length];

                Task writes = Task.Run(() =>
                {
                    foreach (byte b in writerBytes)
                    {
                        writeable.WriteByte(b);
                    }

                    if (FlushRequiredToWriteData)
                    {
                        if (FlushGuaranteesAllDataWritten)
                        {
                            writeable.Flush();
                        }
                        else
                        {
                            writeable.Dispose();
                        }
                    }
                });

                for (int i = 0; i < readerBytes.Length; i++)
                {
                    int r = readable.ReadByte();
                    Assert.InRange(r, 0, 255);
                    readerBytes[i] = (byte)r;
                }

                AssertExtensions.SequenceEqual(writerBytes, readerBytes);

                await writes;

                if (!FlushGuaranteesAllDataWritten)
                {
                    break;
                }
            }
        }

        public static IEnumerable<object[]> ReadWrite_Success_MemberData() =>
            from mode in Enum.GetValues<ReadWriteMode>()
            from writeSize in new[] { 1, 42, 10 * 1024 }
            from startWithFlush in new[] { false, true }
            select new object[] { mode, writeSize, startWithFlush };

        public static IEnumerable<object[]> ReadWrite_Success_Large_MemberData() =>
            from mode in Enum.GetValues<ReadWriteMode>()
            where mode != ReadWriteMode.SyncByte // too slow, even for outer loop, and fully covered by inner loop ReadWrite_Success test
            from writeSize in new[] { 10 * 1024 * 1024 }
            from startWithFlush in new[] { false, true }
            select new object[] { mode, writeSize, startWithFlush };

        [OuterLoop]
        [Theory]
        [MemberData(nameof(ReadWrite_Success_Large_MemberData))]
        public virtual async Task ReadWrite_Success_Large(ReadWriteMode mode, int writeSize, bool startWithFlush) =>
            await ReadWrite_Success(mode, writeSize, startWithFlush);

        [Theory]
        [MemberData(nameof(ReadWrite_Success_MemberData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51371", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public virtual async Task ReadWrite_Success(ReadWriteMode mode, int writeSize, bool startWithFlush)
        {
            foreach (CancellationToken nonCanceledToken in new[] { CancellationToken.None, new CancellationTokenSource().Token })
            {
                using StreamPair streams = await CreateConnectedStreamsAsync();

                foreach ((Stream writeable, Stream readable) in GetReadWritePairs(streams))
                {
                    if (startWithFlush)
                    {
                        await FlushAsync(mode, writeable, nonCanceledToken);
                    }

                    byte[] writerBytes = RandomNumberGenerator.GetBytes(writeSize);
                    var readerBytes = new byte[writerBytes.Length];

                    Task writes = Task.Run(async () =>
                    {
                        await WriteAsync(mode, writeable, writerBytes, 0, writerBytes.Length, nonCanceledToken);

                        if (FlushRequiredToWriteData)
                        {
                            if (FlushGuaranteesAllDataWritten)
                            {
                                await writeable.FlushAsync();
                            }
                            else
                            {
                                await writeable.DisposeAsync();
                            }
                        }
                    });

                    int n = 0;
                    while (n < readerBytes.Length)
                    {
                        int r = await ReadAsync(mode, readable, readerBytes, n, readerBytes.Length - n);
                        Assert.InRange(r, 1, readerBytes.Length - n);
                        n += r;
                    }

                    Assert.Equal(readerBytes.Length, n);
                    AssertExtensions.SequenceEqual(writerBytes, readerBytes);

                    await writes;

                    if (!FlushGuaranteesAllDataWritten)
                    {
                        break;
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(AllReadWriteModesAndValue), false)]
        [MemberData(nameof(AllReadWriteModesAndValue), true)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51371", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public virtual async Task Read_Eof_Returns0(ReadWriteMode mode, bool dataAvailableFirst)
        {
            using StreamPair streams = await CreateConnectedStreamsAsync();
            (Stream writeable, Stream readable) = GetReadWritePair(streams);

            Task write;
            if (dataAvailableFirst)
            {
                write = Task.Run(async () =>
                {
                    await writeable.WriteAsync(Encoding.UTF8.GetBytes("hello"));
                    await writeable.DisposeAsync();
                });
            }
            else
            {
                writeable.Dispose();
                write = Task.CompletedTask;
            }

            if (dataAvailableFirst)
            {
                Assert.Equal('h', readable.ReadByte());
                Assert.Equal('e', readable.ReadByte());
                Assert.Equal('l', readable.ReadByte());
                Assert.Equal('l', readable.ReadByte());
                Assert.Equal('o', readable.ReadByte());
            }

            await write;

            Assert.Equal(0, await ReadAsync(mode, readable, new byte[1], 0, 1));
        }

        [Theory]
        [InlineData(ReadWriteMode.SyncArray)]
        [InlineData(ReadWriteMode.AsyncArray)]
        [InlineData(ReadWriteMode.AsyncAPM)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51371", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public virtual async Task Read_DataStoredAtDesiredOffset(ReadWriteMode mode)
        {
            using StreamPair streams = await CreateConnectedStreamsAsync();
            (Stream writeable, Stream readable) = GetReadWritePair(streams);

            byte[] buffer = new byte[10];
            int offset = 2;
            byte value = 42;

            Task write = Task.Run(() =>
            {
                writeable.WriteByte(value);
                writeable.Dispose();
            });

            Assert.Equal(1, await ReadAsync(mode, readable, buffer, offset, buffer.Length - offset));

            await write;

            for (int i = 0; i < buffer.Length; i++)
            {
                Assert.Equal(i == offset ? value : 0, buffer[i]);
            }
        }

        [Theory]
        [InlineData(ReadWriteMode.SyncArray)]
        [InlineData(ReadWriteMode.AsyncArray)]
        [InlineData(ReadWriteMode.AsyncAPM)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51371", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public virtual async Task Write_DataReadFromDesiredOffset(ReadWriteMode mode)
        {
            using StreamPair streams = await CreateConnectedStreamsAsync();
            (Stream writeable, Stream readable) = GetReadWritePair(streams);

            byte[] buffer = new[] { (byte)'\0', (byte)'\0', (byte)'h', (byte)'e', (byte)'l', (byte)'l', (byte)'o', (byte)'\0', (byte)'\0' };
            const int Offset = 2, Count = 5;

            Task write = Task.Run(async () =>
            {
                await WriteAsync(mode, writeable, buffer, Offset, Count);
                writeable.Dispose();
            });

            using StreamReader reader = new StreamReader(readable);
            Assert.Equal("hello", reader.ReadToEnd());

            await write;
        }

        [Fact]
        public virtual async Task WriteWithBrokenPipe_Throws()
        {
            if (!BrokenPipePropagatedImmediately)
            {
                return;
            }

            using StreamPair streams = await CreateConnectedStreamsAsync();
            (Stream writeable, Stream readable) = GetReadWritePair(streams);

            readable.Dispose();
            byte[] buffer = new byte[4];

            Assert.Throws<IOException>(() => writeable.WriteByte(123));
            Assert.Throws<IOException>(() => writeable.Write(buffer));
            Assert.Throws<IOException>(() => writeable.Write(buffer, 0, buffer.Length));
            await Assert.ThrowsAsync<IOException>(() => writeable.WriteAsync(buffer).AsTask());
            await Assert.ThrowsAsync<IOException>(() => writeable.WriteAsync(buffer, 0, buffer.Length));
            Assert.Throws<IOException>(() => writeable.EndWrite(writeable.BeginWrite(buffer, 0, buffer.Length, null, null)));
            await Assert.ThrowsAsync<IOException>(() => Task.Factory.FromAsync(writeable.BeginWrite, writeable.EndWrite, buffer, 0, buffer.Length, null));
            Assert.Throws<IOException>(() => writeable.Flush());
        }

        [Fact]
        public virtual async Task ReadAsync_NonReusableValueTask_AwaitMultipleTimes_Throws()
        {
            if (!ReadWriteValueTasksProtectSingleConsumption)
            {
                return;
            }

            using StreamPair streams = await CreateConnectedStreamsAsync();
            foreach ((Stream writeable, Stream readable) in GetReadWritePairs(streams))
            {
                var bytes = new byte[1];

                ValueTask<int> r = readable.ReadAsync(bytes);
                await writeable.WriteAsync(new byte[] { 42 });
                if (FlushRequiredToWriteData)
                {
                    await writeable.FlushAsync();
                }
                Assert.Equal(1, await r);
                Assert.Equal(42, bytes[0]);

                await Assert.ThrowsAsync<InvalidOperationException>(async () => await r);
                Assert.Throws<InvalidOperationException>(() => r.GetAwaiter().IsCompleted);
                Assert.Throws<InvalidOperationException>(() => r.GetAwaiter().OnCompleted(() => { }));
                Assert.Throws<InvalidOperationException>(() => r.GetAwaiter().GetResult());
            }
        }

        [Fact]
        public virtual async Task ReadAsync_NonReusableValueTask_MultipleContinuations_Throws()
        {
            if (!ReadWriteValueTasksProtectSingleConsumption)
            {
                return;
            }

            using StreamPair streams = await CreateConnectedStreamsAsync();
            foreach ((Stream writeable, Stream readable) in GetReadWritePairs(streams))
            {
                var b = new byte[1];
                ValueTask<int> r = readable.ReadAsync(b);
                r.GetAwaiter().OnCompleted(() => { });
                Assert.Throws<InvalidOperationException>(() => r.GetAwaiter().OnCompleted(() => { }));
            }
        }

        public static IEnumerable<object[]> ReadAsync_ContinuesOnCurrentContextIfDesired_MemberData() =>
            from flowExecutionContext in new[] { true, false }
            from continueOnCapturedContext in new bool?[] { null, false, true }
            select new object[] { flowExecutionContext, continueOnCapturedContext };

        [Theory]
        [MemberData(nameof(ReadAsync_ContinuesOnCurrentContextIfDesired_MemberData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51371", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public virtual async Task ReadAsync_ContinuesOnCurrentSynchronizationContextIfDesired(bool flowExecutionContext, bool? continueOnCapturedContext)
        {
            await default(JumpToThreadPoolAwaiter); // escape xunit sync ctx

            using StreamPair streams = await CreateConnectedStreamsAsync();
            foreach ((Stream writeable, Stream readable) in GetReadWritePairs(streams))
            {
                Assert.Null(SynchronizationContext.Current);

                var continuationRan = new TaskCompletionSource<bool>();
                var asyncLocal = new AsyncLocal<int>();
                bool schedulerWasFlowed = false;
                bool executionContextWasFlowed = false;
                Action continuation = () =>
                {
                    schedulerWasFlowed = SynchronizationContext.Current is CustomSynchronizationContext;
                    executionContextWasFlowed = 42 == asyncLocal.Value;
                    continuationRan.SetResult(true);
                };

                var readBuffer = new byte[1];
                ValueTask<int> readValueTask = readable.ReadAsync(new byte[1]);

                SynchronizationContext.SetSynchronizationContext(new CustomSynchronizationContext());
                asyncLocal.Value = 42;
                switch (continueOnCapturedContext)
                {
                    case null:
                        if (flowExecutionContext)
                        {
                            readValueTask.GetAwaiter().OnCompleted(continuation);
                        }
                        else
                        {
                            readValueTask.GetAwaiter().UnsafeOnCompleted(continuation);
                        }
                        break;
                    default:
                        if (flowExecutionContext)
                        {
                            readValueTask.ConfigureAwait(continueOnCapturedContext.Value).GetAwaiter().OnCompleted(continuation);
                        }
                        else
                        {
                            readValueTask.ConfigureAwait(continueOnCapturedContext.Value).GetAwaiter().UnsafeOnCompleted(continuation);
                        }
                        break;
                }
                asyncLocal.Value = 0;
                SynchronizationContext.SetSynchronizationContext(null);

                Assert.False(readValueTask.IsCompleted);
                Assert.False(readValueTask.IsCompletedSuccessfully);
                await writeable.WriteAsync(new byte[] { 42 });
                if (FlushRequiredToWriteData)
                {
                    if (FlushGuaranteesAllDataWritten)
                    {
                        await writeable.FlushAsync();
                    }
                    else
                    {
                        await writeable.DisposeAsync();
                    }
                }

                await continuationRan.Task;
                Assert.True(readValueTask.IsCompleted);
                Assert.True(readValueTask.IsCompletedSuccessfully);

                Assert.Equal(continueOnCapturedContext != false, schedulerWasFlowed);
                Assert.Equal(flowExecutionContext, executionContextWasFlowed);

                if (!FlushGuaranteesAllDataWritten)
                {
                    break;
                }
            }
        }

        [Theory]
        [MemberData(nameof(ReadAsync_ContinuesOnCurrentContextIfDesired_MemberData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51371", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public virtual async Task ReadAsync_ContinuesOnCurrentTaskSchedulerIfDesired(bool flowExecutionContext, bool? continueOnCapturedContext)
        {
            await default(JumpToThreadPoolAwaiter); // escape xunit sync ctx

            using StreamPair streams = await CreateConnectedStreamsAsync();
            foreach ((Stream writeable, Stream readable) in GetReadWritePairs(streams))
            {
                Assert.Null(SynchronizationContext.Current);

                var continuationRan = new TaskCompletionSource();
                var asyncLocal = new AsyncLocal<int>();
                bool schedulerWasFlowed = false;
                bool executionContextWasFlowed = false;
                Action continuation = () =>
                {
                    schedulerWasFlowed = TaskScheduler.Current is CustomTaskScheduler;
                    executionContextWasFlowed = 42 == asyncLocal.Value;
                    continuationRan.SetResult();
                };

                var readBuffer = new byte[1];
                ValueTask<int> readValueTask = readable.ReadAsync(new byte[1]);

                await Task.Factory.StartNew(() =>
                {
                    Assert.IsType<CustomTaskScheduler>(TaskScheduler.Current);
                    asyncLocal.Value = 42;
                    switch (continueOnCapturedContext)
                    {
                        case null:
                            if (flowExecutionContext)
                            {
                                readValueTask.GetAwaiter().OnCompleted(continuation);
                            }
                            else
                            {
                                readValueTask.GetAwaiter().UnsafeOnCompleted(continuation);
                            }
                            break;
                        default:
                            if (flowExecutionContext)
                            {
                                readValueTask.ConfigureAwait(continueOnCapturedContext.Value).GetAwaiter().OnCompleted(continuation);
                            }
                            else
                            {
                                readValueTask.ConfigureAwait(continueOnCapturedContext.Value).GetAwaiter().UnsafeOnCompleted(continuation);
                            }
                            break;
                    }
                    asyncLocal.Value = 0;
                }, CancellationToken.None, TaskCreationOptions.None, new CustomTaskScheduler());

                Assert.False(readValueTask.IsCompleted);
                Assert.False(readValueTask.IsCompletedSuccessfully);
                await writeable.WriteAsync(new byte[] { 42 });
                if (FlushRequiredToWriteData)
                {
                    if (FlushGuaranteesAllDataWritten)
                    {
                        await writeable.FlushAsync();
                    }
                    else
                    {
                        await writeable.DisposeAsync();
                    }
                }

                await continuationRan.Task;
                Assert.True(readValueTask.IsCompleted);
                Assert.True(readValueTask.IsCompletedSuccessfully);

                Assert.Equal(continueOnCapturedContext != false, schedulerWasFlowed);
                Assert.Equal(flowExecutionContext, executionContextWasFlowed);

                if (!FlushGuaranteesAllDataWritten)
                {
                    break;
                }
            }
        }

        [Theory]
        [InlineData(ReadWriteMode.SyncArray)]
        [InlineData(ReadWriteMode.SyncSpan)]
        [InlineData(ReadWriteMode.AsyncArray)]
        [InlineData(ReadWriteMode.AsyncMemory)]
        [InlineData(ReadWriteMode.SyncAPM)]
        [InlineData(ReadWriteMode.AsyncAPM)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51371", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public virtual async Task ZeroByteRead_BlocksUntilDataAvailableOrNops(ReadWriteMode mode)
        {
            using StreamPair streams = await CreateConnectedStreamsAsync();
            foreach ((Stream writeable, Stream readable) in GetReadWritePairs(streams))
            {
                for (int iter = 0; iter < 2; iter++)
                {
                    Task<int> zeroByteRead = Task.Run(() => ReadAsync(mode, readable, Array.Empty<byte>(), 0, 0));

                    if (BlocksOnZeroByteReads)
                    {
                        Assert.False(zeroByteRead.IsCompleted);

                        Task write = Task.Run(async () =>
                        {
                            await writeable.WriteAsync(Encoding.UTF8.GetBytes("hello"));
                            if (FlushRequiredToWriteData)
                            {
                                if (FlushGuaranteesAllDataWritten)
                                {
                                    await writeable.FlushAsync();
                                }
                                else
                                {
                                    await writeable.DisposeAsync();
                                }
                            }
                        });
                        Assert.Equal(0, await zeroByteRead);

                        var readBytes = new byte[5];
                        int count = 0;
                        while (count < readBytes.Length)
                        {
                            int n = await readable.ReadAsync(readBytes.AsMemory(count));
                            Assert.InRange(n, 1, readBytes.Length - count);
                            count += n;
                        }

                        Assert.Equal("hello", Encoding.UTF8.GetString(readBytes));
                        await write;
                    }
                    else
                    {
                        Assert.Equal(0, await zeroByteRead);
                    }

                    if (!FlushGuaranteesAllDataWritten)
                    {
                        return;
                    }
                }
            }
        }

        [Theory]
        [InlineData(ReadWriteMode.SyncArray)]
        [InlineData(ReadWriteMode.SyncSpan)]
        [InlineData(ReadWriteMode.AsyncArray)]
        [InlineData(ReadWriteMode.AsyncMemory)]
        [InlineData(ReadWriteMode.SyncAPM)]
        [InlineData(ReadWriteMode.AsyncAPM)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51371", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public virtual async Task ZeroByteWrite_OtherDataReceivedSuccessfully(ReadWriteMode mode)
        {
            byte[][] buffers = new[] { Array.Empty<byte>(), Encoding.UTF8.GetBytes("hello"), Array.Empty<byte>(), Encoding.UTF8.GetBytes("world") };

            using StreamPair streams = await CreateConnectedStreamsAsync();
            foreach ((Stream writeable, Stream readable) in GetReadWritePairs(streams))
            {
                Task writes = Task.Run(async () =>
                {
                    foreach (byte[] buffer in buffers)
                    {
                        await WriteAsync(mode, writeable, buffer, 0, buffer.Length);
                    }
                });

                if (FlushRequiredToWriteData)
                {
                    writes = writes.ContinueWith(t =>
                    {
                        t.GetAwaiter().GetResult();
                        if (FlushGuaranteesAllDataWritten)
                        {
                            writeable.Flush();
                        }
                        else
                        {
                            writeable.Dispose();
                        }
                    }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
                }

                var readBytes = new byte[buffers.Sum(b => b.Length)];
                int count = 0;
                while (count < readBytes.Length)
                {
                    int n = await readable.ReadAsync(readBytes.AsMemory(count));
                    Assert.InRange(n, 1, readBytes.Length - count);
                    count += n;
                }

                Assert.Equal("helloworld", Encoding.UTF8.GetString(readBytes));
                await writes;

                if (!FlushGuaranteesAllDataWritten)
                {
                    break;
                }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51371", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public virtual async Task ReadWrite_CustomMemoryManager_Success(bool useAsync)
        {
            using StreamPair streams = await CreateConnectedStreamsAsync();
            foreach ((Stream writeable, Stream readable) in GetReadWritePairs(streams))
            {
                using MemoryManager<byte> writeBuffer = new NativeMemoryManager(1024);
                using MemoryManager<byte> readBuffer = new NativeMemoryManager(writeBuffer.Memory.Length);

                Assert.Equal(1024, writeBuffer.Memory.Length);
                Assert.Equal(writeBuffer.Memory.Length, readBuffer.Memory.Length);

                Random.Shared.NextBytes(writeBuffer.Memory.Span);
                readBuffer.Memory.Span.Clear();

                Task write = useAsync ?
                    writeable.WriteAsync(writeBuffer.Memory).AsTask() :
                    Task.Run(() => writeable.Write(writeBuffer.Memory.Span));
                if (FlushRequiredToWriteData)
                {
                    write = write.ContinueWith(t =>
                    {
                        t.GetAwaiter().GetResult();
                        if (FlushGuaranteesAllDataWritten)
                        {
                            writeable.Flush();
                        }
                        else
                        {
                            writeable.Dispose();
                        }
                    }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
                }

                try
                {
                    int bytesRead = 0;
                    while (bytesRead < readBuffer.Memory.Length)
                    {
                        int n = useAsync ?
                            await readable.ReadAsync(readBuffer.Memory.Slice(bytesRead)) :
                            readable.Read(readBuffer.Memory.Span.Slice(bytesRead));
                        if (n == 0)
                        {
                            break;
                        }
                        Assert.InRange(n, 1, readBuffer.Memory.Length - bytesRead);
                        bytesRead += n;
                    }

                    Assert.True(writeBuffer.Memory.Span.SequenceEqual(readBuffer.Memory.Span));
                }
                finally
                {
                    await write;
                }

                if (!FlushGuaranteesAllDataWritten)
                {
                    break;
                }
            }
        }

        [Fact]
        public virtual async Task ConcurrentBidirectionalReadsWrites_Success()
        {
            if (!SupportsConcurrentBidirectionalUse)
            {
                return;
            }

            using StreamPair streams = await CreateConnectedStreamsAsync();
            Stream client = streams.Stream1, server = streams.Stream2;
            if (!(client.CanRead && client.CanWrite && server.CanRead && server.CanWrite))
            {
                return;
            }

            const string Text = "This is a test.  This is only a test.";
            byte[] sendBuffer = Encoding.UTF8.GetBytes(Text);
            DateTime endTime = DateTime.UtcNow + TimeSpan.FromSeconds(2);
            Func<Stream, Stream, Task> work = async (client, server) =>
            {
                var readBuffer = new byte[sendBuffer.Length];
                while (DateTime.UtcNow < endTime)
                {
                    await WhenAllOrAnyFailed(
                        client.WriteAsync(sendBuffer, 0, sendBuffer.Length),
                        Task.Run(async () =>
                        {
                            int received = 0, bytesRead = 0;
                            while (received < readBuffer.Length && (bytesRead = await server.ReadAsync(readBuffer.AsMemory(received))) > 0)
                            {
                                received += bytesRead;
                            }
                            Assert.InRange(bytesRead, 1, int.MaxValue);
                            Assert.Equal(Text, Encoding.UTF8.GetString(readBuffer));
                        }));
                }
            };

            await WhenAllOrAnyFailed(
                Task.Run(() => work(client, server)),
                Task.Run(() => work(server, client)));
        }

        public static IEnumerable<object[]> CopyToAsync_AllDataCopied_MemberData() =>
            from byteCount in new int[] { 0, 1, 1024, 4095, 4096 }
            from useAsync in new bool[] { true, false }
            select new object[] { byteCount, useAsync };

        [OuterLoop]
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public virtual async Task CopyToAsync_AllDataCopied_Large(bool useAsync) =>
            await CopyToAsync_AllDataCopied(1024 * 1024, useAsync);

        [Theory]
        [MemberData(nameof(CopyToAsync_AllDataCopied_MemberData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51371", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public virtual async Task CopyToAsync_AllDataCopied(int byteCount, bool useAsync)
        {
            using StreamPair streams = await CreateConnectedStreamsAsync();
            (Stream writeable, Stream readable) = GetReadWritePair(streams);

            var results = new MemoryStream();
            byte[] dataToCopy = RandomNumberGenerator.GetBytes(byteCount);

            Task copyTask;
            if (useAsync)
            {
                copyTask = readable.CopyToAsync(results);
                await writeable.WriteAsync(dataToCopy);
            }
            else
            {
                copyTask = Task.Run(() => readable.CopyTo(results));
                writeable.Write(new ReadOnlySpan<byte>(dataToCopy));
            }

            writeable.Dispose();
            await copyTask;

            AssertExtensions.SequenceEqual(dataToCopy, results.ToArray());
        }

        [OuterLoop("May take several seconds")]
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public virtual async Task Parallel_ReadWriteMultipleStreamsConcurrently()
        {
            await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(async () =>
            {
                await CopyToAsync_AllDataCopied(byteCount: 10 * 1024, useAsync: true);
            })));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51371", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public virtual async Task Timeout_Roundtrips()
        {
            using StreamPair streams = await CreateConnectedStreamsAsync();
            foreach ((Stream writeable, Stream readable) in GetReadWritePairs(streams))
            {
                if (writeable.CanTimeout)
                {
                    Assert.Equal(-1, writeable.WriteTimeout);

                    writeable.WriteTimeout = 100;
                    Assert.InRange(writeable.WriteTimeout, 100, int.MaxValue);
                    writeable.WriteTimeout = 100; // same value again
                    Assert.InRange(writeable.WriteTimeout, 100, int.MaxValue);

                    writeable.WriteTimeout = -1;
                    Assert.Equal(-1, writeable.WriteTimeout);
                }

                if (readable.CanTimeout)
                {
                    Assert.Equal(-1, readable.ReadTimeout);

                    readable.ReadTimeout = 100;
                    Assert.InRange(readable.ReadTimeout, 100, int.MaxValue);
                    readable.ReadTimeout = 100; // same value again
                    Assert.InRange(readable.ReadTimeout, 100, int.MaxValue);

                    readable.ReadTimeout = -1;
                    Assert.Equal(-1, readable.ReadTimeout);
                }
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51371", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public virtual async Task ReadTimeout_Expires_Throws()
        {
            using StreamPair streams = await CreateConnectedStreamsAsync();
            foreach ((Stream writeable, Stream readable) in GetReadWritePairs(streams))
            {
                if (!readable.CanTimeout)
                {
                    continue;
                }

                Assert.Equal(-1, readable.ReadTimeout);

                readable.ReadTimeout = 1;
                Assert.ThrowsAny<IOException>(() => readable.Read(new byte[1], 0, 1));
            }
        }

        [Fact]
        public virtual async Task ReadAsync_CancelPendingRead_DoesntImpactSubsequentReads()
        {
            if (!UsableAfterCanceledReads)
            {
                return;
            }

            using StreamPair streams = await CreateConnectedStreamsAsync();
            foreach ((Stream writeable, Stream readable) in GetReadWritePairs(streams))
            {
                CancellationTokenSource cts;

                cts = new CancellationTokenSource();
                cts.Cancel();
                await AssertCanceledAsync(cts.Token, () => readable.ReadAsync(new byte[1], 0, 1, cts.Token));
                await AssertCanceledAsync(cts.Token, async () => { await readable.ReadAsync(new Memory<byte>(new byte[1]), cts.Token); });

                cts = new CancellationTokenSource();
                Task<int> t = readable.ReadAsync(new byte[1], 0, 1, cts.Token);
                cts.Cancel();
                await AssertCanceledAsync(cts.Token, () => t);

                cts = new CancellationTokenSource();
                ValueTask<int> vt = readable.ReadAsync(new Memory<byte>(new byte[1]), cts.Token);
                cts.Cancel();
                await AssertCanceledAsync(cts.Token, async () => await vt);

                byte[] buffer = new byte[1];
                vt = readable.ReadAsync(new Memory<byte>(buffer));
                Assert.False(vt.IsCompleted);
                await writeable.WriteAsync(new ReadOnlyMemory<byte>(new byte[1] { 42 }));
                if (FlushRequiredToWriteData)
                {
                    await writeable.FlushAsync();
                }
                Assert.Equal(1, await vt);
                Assert.Equal(42, buffer[0]);
            }
        }

        [Fact]
        public virtual async Task WriteAsync_CancelPendingWrite_SucceedsOrThrowsOperationCanceled()
        {
            if (BufferedSize == -1)
            {
                return;
            }

            using StreamPair streams = await CreateConnectedStreamsAsync();
            foreach ((Stream writeable, Stream readable) in GetReadWritePairs(streams))
            {
                var buffer = new byte[BufferedSize + 1];
                Exception e;

                var cts = new CancellationTokenSource();
                Task t = writeable.WriteAsync(buffer, 0, buffer.Length, cts.Token);
                cts.Cancel();
                e = await Record.ExceptionAsync(async () => await t);
                if (e != null)
                {
                    Assert.IsAssignableFrom<OperationCanceledException>(e);
                }

                cts = new CancellationTokenSource();
                ValueTask vt = writeable.WriteAsync(new Memory<byte>(buffer), cts.Token);
                cts.Cancel();
                e = await Record.ExceptionAsync(async () => await vt);
                if (e != null)
                {
                    Assert.IsAssignableFrom<OperationCanceledException>(e);
                }
            }
        }

        [Fact]
        public virtual async Task ClosedConnection_WritesFailImmediately_ThrowException()
        {
            if (!BrokenPipePropagatedImmediately)
            {
                return;
            }

            using StreamPair streams = await CreateConnectedStreamsAsync();
            (Stream writeable, Stream readable) = GetReadWritePair(streams);

            readable.Dispose();
            Assert.Throws<IOException>(() => writeable.WriteByte(1));
            Assert.Throws<IOException>(() => writeable.Write(new byte[1], 0, 1));
            Assert.Throws<IOException>(() => writeable.Write(new byte[1]));
            Assert.Throws<IOException>(() => writeable.EndWrite(writeable.BeginWrite(new byte[1], 0, 1, null, null)));
            await Assert.ThrowsAsync<IOException>(async () => { await writeable.WriteAsync(new byte[1], 0, 1); });
            await Assert.ThrowsAsync<IOException>(async () => { await writeable.WriteAsync(new byte[1]); });
            await Assert.ThrowsAsync<IOException>(async () => { await Task.Factory.FromAsync(writeable.BeginWrite, writeable.EndWrite, new byte[1], 0, 1, null); });
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public virtual async Task ReadAsync_DuringReadAsync_ThrowsIfUnsupported()
        {
            if (UnsupportedConcurrentExceptionType is null)
            {
                return;
            }

            using StreamPair streams = await CreateConnectedStreamsAsync();
            (Stream writeable, Stream readable) = GetReadWritePair(streams);

            ValueTask<int> read = readable.ReadAsync(new byte[1]);
            await Assert.ThrowsAsync(UnsupportedConcurrentExceptionType, async () => await readable.ReadAsync(new byte[1]));

            writeable.WriteByte(1);
            writeable.Dispose();

            Assert.Equal(1, await read);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51371", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public virtual async Task Flush_ValidOnWriteableStreamWithNoData_Success()
        {
            using StreamPair streams = await CreateConnectedStreamsAsync();
            foreach (Stream stream in streams)
            {
                if (stream.CanWrite)
                {
                    stream.Flush();
                    await stream.FlushAsync();
                }
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51371", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public virtual async Task Flush_ValidOnReadableStream_Success()
        {
            using StreamPair streams = await CreateConnectedStreamsAsync();
            foreach (Stream stream in streams)
            {
                if (stream.CanRead)
                {
                    stream.Flush();
                    await stream.FlushAsync();
                }
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51371", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public virtual async Task Dispose_ClosesStream(int disposeMode)
        {
            if (!CansReturnFalseAfterDispose)
            {
                return;
            }

            StreamPair streams = await CreateConnectedStreamsAsync();

            foreach (Stream stream in streams)
            {
                switch (disposeMode)
                {
                    case 0: stream.Close(); break;
                    case 1: stream.Dispose(); break;
                    case 2: await stream.DisposeAsync(); break;
                }

                Assert.False(stream.CanRead);
                Assert.False(stream.CanWrite);
            }
        }
    }

    /// <summary>Base class for a connected stream that wraps another.</summary>
    public abstract class WrappingConnectedStreamConformanceTests : ConnectedStreamConformanceTests
    {
        protected abstract Task<StreamPair> CreateWrappedConnectedStreamsAsync(StreamPair wrapped, bool leaveOpen = false);
        protected virtual bool WrappedUsableAfterClose => true;
        protected virtual bool SupportsLeaveOpen => true;
        /// <summary>
        /// Indicates whether the stream will issue a zero byte read on the underlying stream when a user performs
        /// a zero byte read and no data is currently available to return to the user.
        /// </summary>
        protected virtual bool ZeroByteReadPerformsZeroByteReadOnUnderlyingStream => false;

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public virtual async Task Flush_FlushesUnderlyingStream(bool flushAsync)
        {
            if (!FlushGuaranteesAllDataWritten)
            {
                return;
            }

            using StreamPair streams = ConnectedStreams.CreateBidirectional();
            (Stream writeable, Stream readable) = GetReadWritePair(streams);

            var tracker = new CallTrackingStream(writeable);
            using StreamPair wrapper = await CreateWrappedConnectedStreamsAsync((tracker, readable));

            int orig = tracker.TimesCalled(nameof(tracker.Flush)) + tracker.TimesCalled(nameof(tracker.FlushAsync));

            tracker.WriteByte(1);

            if (flushAsync)
            {
                await wrapper.Stream1.FlushAsync();
            }
            else
            {
                wrapper.Stream1.Flush();
            }

            Assert.InRange(tracker.TimesCalled(nameof(tracker.Flush)) + tracker.TimesCalled(nameof(tracker.FlushAsync)), orig + 1, int.MaxValue);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        public virtual async Task Dispose_Flushes(bool useAsync, bool leaveOpen)
        {
            if (leaveOpen && (!SupportsLeaveOpen || ReadsMayBlockUntilBufferFullOrEOF))
            {
                return;
            }

            using StreamPair streams = ConnectedStreams.CreateBidirectional();
            using StreamPair wrapper = await CreateWrappedConnectedStreamsAsync(streams, leaveOpen);
            (Stream writeable, Stream readable) = GetReadWritePair(wrapper);

            writeable.WriteByte(1);

            if (useAsync)
            {
                await writeable.DisposeAsync();
            }
            else
            {
                writeable.Dispose();
            }

            Assert.Equal(1, readable.ReadByte());
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        public virtual async Task Dispose_ClosesInnerStreamIfDesired(bool useAsync, bool leaveOpen)
        {
            if (!SupportsLeaveOpen && leaveOpen)
            {
                return;
            }

            using StreamPair streams = ConnectedStreams.CreateBidirectional();
            (Stream writeable, Stream readable) = GetReadWritePair(streams);
            using StreamPair wrapper = await CreateWrappedConnectedStreamsAsync((writeable, readable), leaveOpen);
            (Stream writeableWrapper, Stream readableWrapper) = GetReadWritePair(wrapper);

            if (useAsync)
            {
                await writeableWrapper.DisposeAsync();
            }
            else
            {
                writeableWrapper.Dispose();
            }

            if (leaveOpen)
            {
                await WhenAllOrAnyFailed(
                    writeable.WriteAsync(new byte[] { 42 }, 0, 1),
                    Task.Run(() => readable.ReadByte()));
            }
            else
            {
                Assert.Throws<ObjectDisposedException>(() => writeable.WriteByte(42));
            }
        }

        [Fact]
        public virtual async Task UseWrappedAfterClose_Success()
        {
            if (!WrappedUsableAfterClose || !SupportsLeaveOpen)
            {
                return;
            }

            using StreamPair streams = ConnectedStreams.CreateBidirectional();

            using (StreamPair wrapper = await CreateWrappedConnectedStreamsAsync(streams, leaveOpen: true))
            {
                foreach ((Stream writeable, Stream readable) in GetReadWritePairs(wrapper))
                {
                    writeable.WriteByte(42);
                    readable.ReadByte();
                }
            }

            foreach ((Stream writeable, Stream readable) in GetReadWritePairs(streams))
            {
                writeable.WriteByte(42);
                readable.ReadByte();
            }
        }

        [Fact]
        public virtual async Task NestedWithinSelf_ReadWrite_Success()
        {
            using StreamPair streams = ConnectedStreams.CreateBidirectional();
            using StreamPair wrapper1 = await CreateWrappedConnectedStreamsAsync(streams);
            using StreamPair wrapper2 = await CreateWrappedConnectedStreamsAsync(wrapper1);
            using StreamPair wrapper3 = await CreateWrappedConnectedStreamsAsync(wrapper2);

            if (Bidirectional(wrapper3) && FlushGuaranteesAllDataWritten)
            {
                foreach ((Stream writeable, Stream readable) in GetReadWritePairs(wrapper3))
                {
                    for (int i = 0; i < 10; i++)
                    {
                        await WhenAllOrAnyFailed(
                            Task.Run(() =>
                            {
                                writeable.WriteByte((byte)i);
                                if (FlushRequiredToWriteData)
                                {
                                    writeable.Flush();
                                }
                            }),
                            Task.Run(() => Assert.Equal(i, readable.ReadByte())));
                    }
                }
            }
            else
            {
                (Stream writeable, Stream readable) = GetReadWritePair(wrapper3);
                await WhenAllOrAnyFailed(
                    Task.Run(() =>
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            writeable.WriteByte((byte)i);
                        }
                        writeable.Dispose();
                    }),
                    Task.Run(() =>
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            Assert.Equal(i, readable.ReadByte());
                        }
                        Assert.Equal(-1, readable.ReadByte());
                    }));
            }
        }

        [Theory]
        [InlineData(ReadWriteMode.SyncArray)]
        [InlineData(ReadWriteMode.SyncSpan)]
        [InlineData(ReadWriteMode.AsyncArray)]
        [InlineData(ReadWriteMode.AsyncMemory)]
        [InlineData(ReadWriteMode.SyncAPM)]
        [InlineData(ReadWriteMode.AsyncAPM)]
        public virtual async Task ZeroByteRead_PerformsZeroByteReadOnUnderlyingStreamWhenDataNeeded(ReadWriteMode mode)
        {
            if (!ZeroByteReadPerformsZeroByteReadOnUnderlyingStream)
            {
                return;
            }

            // This is the data we will send across the connected streams. We assume this data will both
            // (a) produce at least two readable bytes, so we can unblock the reader and read a single byte without clearing its buffer; and
            // (b) produce no more than 1K of readable bytes, so we can clear the reader buffer below.
            // If this isn't the case for some Stream(s), we can modify the data or parameterize it per Stream.
            byte[] data = Encoding.UTF8.GetBytes("hello world");

            using StreamPair innerStreams = ConnectedStreams.CreateBidirectional();
            (Stream innerWriteable, Stream innerReadable) = GetReadWritePair(innerStreams);

            var tracker = new ZeroByteReadTrackingStream(innerReadable);
            using StreamPair streams = await CreateWrappedConnectedStreamsAsync((innerWriteable, tracker));

            (Stream writeable, Stream readable) = GetReadWritePair(streams);

            for (int iter = 0; iter < 2; iter++)
            {
                // Register to be signalled for the zero byte read.
                var signalTask = tracker.WaitForZeroByteReadAsync();

                // Issue zero byte read against wrapper stream.
                Task<int> zeroByteRead = Task.Run(() => ReadAsync(mode, readable, Array.Empty<byte>(), 0, 0));

                // The tracker stream will signal us when the zero byte read actually happens.
                await signalTask;

                // Write some data (see notes above re 'data')
                await writeable.WriteAsync(data);
                if (FlushRequiredToWriteData)
                {
                    await writeable.FlushAsync();
                }

                // Reader should be unblocked, and we should have issued a zero byte read against the underlying stream as part of unblocking.
                int bytesRead = await zeroByteRead;
                Assert.Equal(0, bytesRead);

                byte[] buffer = new byte[1024];

                // Should be able to read one byte without blocking
                var readTask = ReadAsync(mode, readable, buffer, 0, 1);
                Assert.True(readTask.IsCompleted);
                bytesRead = await readTask;
                Assert.Equal(1, bytesRead);

                // Issue zero byte read against wrapper stream. Since there is still data available, this should complete immediately and not do another zero-byte read.
                readTask = ReadAsync(mode, readable, Array.Empty<byte>(), 0, 0);
                Assert.True(readTask.IsCompleted);
                Assert.Equal(0, await readTask);

                // Clear the reader stream of any buffered data by doing a large read, which again should not block.
                readTask = ReadAsync(mode, readable, buffer, 1, buffer.Length - 1);
                Assert.True(readTask.IsCompleted);
                bytesRead += await readTask;

                if (FlushGuaranteesAllDataWritten)
                {
                    AssertExtensions.SequenceEqual(data, buffer.AsSpan().Slice(0, bytesRead));
                }
            }
        }

        private sealed class ZeroByteReadTrackingStream : DelegatingStream
        {
            private TaskCompletionSource? _signal;

            public ZeroByteReadTrackingStream(Stream innerStream) : base(innerStream)
            {
            }

            public Task WaitForZeroByteReadAsync()
            {
                if (_signal is not null)
                {
                    throw new Exception("Already registered to wait");
                }

                _signal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                return _signal.Task;
            }

            private void CheckForZeroByteRead(int bufferLength)
            {
                if (bufferLength == 0)
                {
                    var signal = _signal;
                    if (signal is null)
                    {
                        throw new Exception("Unexpected zero byte read");
                    }

                    _signal = null;
                    signal.SetResult();
                }
            }

            public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
            {
                CheckForZeroByteRead(count);
                return base.BeginRead(buffer, offset, count, callback, state);
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                CheckForZeroByteRead(count);
                return base.Read(buffer, offset, count);
            }

            public override int Read(Span<byte> buffer)
            {
                CheckForZeroByteRead(buffer.Length);
                return base.Read(buffer);
            }

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                CheckForZeroByteRead(count);
                return base.ReadAsync(buffer, offset, count, cancellationToken);
            }

            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                CheckForZeroByteRead(buffer.Length);
                return base.ReadAsync(buffer, cancellationToken);
            }
        }
    }

    /// <summary>Provides a disposable, enumerable tuple of two streams.</summary>
    public class StreamPair : IDisposable, IEnumerable<Stream>
    {
        public readonly Stream Stream1, Stream2;

        public StreamPair(Stream stream1, Stream stream2)
        {
            Stream1 = stream1;
            Stream2 = stream2;
        }

        public StreamPair((Stream, Stream) streams)
        {
            Stream1 = streams.Item1;
            Stream2 = streams.Item2;
        }

        public static implicit operator StreamPair((Stream, Stream) streams) => new StreamPair(streams);
        public static implicit operator (Stream, Stream)(StreamPair streams) => (streams.Stream1, streams.Stream2);

        public virtual void Dispose()
        {
            Stream1?.Dispose();
            Stream2?.Dispose();
        }

        public IEnumerator<Stream> GetEnumerator()
        {
            yield return Stream1;
            yield return Stream2;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
