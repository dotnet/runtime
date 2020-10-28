// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    /// <summary>Base class providing tests for any Stream-derived type.</summary>
    [PlatformSpecific(~TestPlatforms.Browser)] // lots of operations aren't supported on browser
    public abstract class StreamConformanceTests
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

        /// <summary>Gets whether the stream's CanRead/Write/etc properties are expected to return false once the stream is disposed.</summary>
        protected virtual bool CansReturnFalseAfterDispose => true;
        /// <summary>Gets whether the Stream may be used for additional operations after a read is canceled.</summary>
        protected virtual bool UsableAfterCanceledReads => true;

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

        protected async Task ValidateMisuseExceptionsAsync(Stream stream)
        {
            byte[] oneByteBuffer = new byte[1];

            if (stream.CanRead)
            {
                // Null arguments
                foreach ((int offset, int count) in new[] { (0, 0), (1, 2) }) // validate 0, 0 isn't special-cased to be allowed with a null buffer
                {
                    AssertExtensions.Throws<ArgumentNullException>(ReadWriteBufferName, () => { stream.Read(null, offset, count); });
                    AssertExtensions.Throws<ArgumentNullException>(ReadWriteBufferName, () => { stream.Read(null, offset, count); });
                    AssertExtensions.Throws<ArgumentNullException>(ReadWriteBufferName, () => { stream.ReadAsync(null, offset, count); });
                    AssertExtensions.Throws<ArgumentNullException>(ReadWriteBufferName, () => { stream.ReadAsync(null, offset, count, default); });
                    AssertExtensions.Throws<ArgumentNullException>(ReadWriteBufferName, () => { stream.EndRead(stream.BeginRead(null, offset, count, iar => { }, new object())); });
                    AssertExtensions.Throws<ArgumentNullException>(ReadWriteAsyncResultName, () => { stream.EndRead(null); });
                }

                // Invalid offset
                AssertExtensions.Throws<ArgumentOutOfRangeException>(ReadWriteOffsetName, () => { stream.Read(oneByteBuffer, -1, 0); });
                AssertExtensions.Throws<ArgumentOutOfRangeException>(ReadWriteOffsetName, () => { stream.ReadAsync(oneByteBuffer, -1, 0); });
                AssertExtensions.Throws<ArgumentOutOfRangeException>(ReadWriteOffsetName, () => { stream.ReadAsync(oneByteBuffer, -1, 0, default); });
                AssertExtensions.Throws<ArgumentOutOfRangeException>(ReadWriteOffsetName, () => { stream.EndRead(stream.BeginRead(oneByteBuffer, -1, 0, iar => { }, new object())); });
                Assert.ThrowsAny<ArgumentException>(() => { stream.Read(oneByteBuffer, 2, 0); });
                Assert.ThrowsAny<ArgumentException>(() => { stream.ReadAsync(oneByteBuffer, 2, 0); });
                Assert.ThrowsAny<ArgumentException>(() => { stream.ReadAsync(oneByteBuffer, 2, 0, default); });
                Assert.ThrowsAny<ArgumentException>(() => { stream.EndRead(stream.BeginRead(oneByteBuffer, 2, 0, iar => { }, new object())); });

                // Invalid count
                AssertExtensions.Throws<ArgumentOutOfRangeException>(ReadWriteCountName, () => { stream.Read(oneByteBuffer, 0, -1); });
                AssertExtensions.Throws<ArgumentOutOfRangeException>(ReadWriteCountName, () => { stream.ReadAsync(oneByteBuffer, 0, -1); });
                AssertExtensions.Throws<ArgumentOutOfRangeException>(ReadWriteCountName, () => { stream.ReadAsync(oneByteBuffer, 0, -1, default); });
                AssertExtensions.Throws<ArgumentOutOfRangeException>(ReadWriteCountName, () => { stream.EndRead(stream.BeginRead(oneByteBuffer, 0, -1, iar => { }, new object())); });
                Assert.ThrowsAny<ArgumentException>(() => { stream.Read(oneByteBuffer, 0, 2); });
                Assert.ThrowsAny<ArgumentException>(() => { stream.ReadAsync(oneByteBuffer, 0, 2); });
                Assert.ThrowsAny<ArgumentException>(() => { stream.ReadAsync(oneByteBuffer, 0, 2, default); });
                Assert.ThrowsAny<ArgumentException>(() => { stream.EndRead(stream.BeginRead(oneByteBuffer, 0, 2, iar => { }, new object())); });

                // Invalid offset + count
                foreach ((int invalidOffset, int invalidCount) in new[] { (1, 1) })
                {
                    Assert.ThrowsAny<ArgumentException>(() => { stream.Read(oneByteBuffer, invalidOffset, invalidCount); });
                    Assert.ThrowsAny<ArgumentException>(() => { stream.ReadAsync(oneByteBuffer, invalidOffset, invalidCount); });
                    Assert.ThrowsAny<ArgumentException>(() => { stream.ReadAsync(oneByteBuffer, invalidOffset, invalidCount, default); });
                    Assert.ThrowsAny<ArgumentException>(() => { stream.EndRead(stream.BeginRead(oneByteBuffer, invalidOffset, invalidCount, iar => { }, new object())); });
                }

                // Unknown arguments
                Assert.Throws(InvalidIAsyncResultExceptionType, () => stream.EndRead(new NotImplementedIAsyncResult()));

                // Invalid destination stream
                AssertExtensions.Throws<ArgumentNullException>(CopyToStreamName, () => { stream.CopyTo(null); });
                AssertExtensions.Throws<ArgumentNullException>(CopyToStreamName, () => { stream.CopyTo(null, 1); });
                AssertExtensions.Throws<ArgumentNullException>(CopyToStreamName, () => { stream.CopyToAsync(null, default(CancellationToken)); });
                AssertExtensions.Throws<ArgumentNullException>(CopyToStreamName, () => { stream.CopyToAsync(null, 1); });
                AssertExtensions.Throws<ArgumentNullException>(CopyToStreamName, () => { stream.CopyToAsync(null, 1, default(CancellationToken)); });

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
                Assert.Throws(UnsupportedReadWriteExceptionType, () => { stream.ReadAsync(new byte[1], 0, 1); });
                Assert.Throws(UnsupportedReadWriteExceptionType, () => { stream.ReadAsync(new Memory<byte>(new byte[1])); });
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
                    AssertExtensions.Throws<ArgumentNullException>(ReadWriteBufferName, () => { stream.Write(null, offset, count); });
                    AssertExtensions.Throws<ArgumentNullException>(ReadWriteBufferName, () => { stream.WriteAsync(null, offset, count); });
                    AssertExtensions.Throws<ArgumentNullException>(ReadWriteBufferName, () => { stream.WriteAsync(null, offset, count, default); });
                    AssertExtensions.Throws<ArgumentNullException>(ReadWriteBufferName, () => { stream.EndWrite(stream.BeginWrite(null, offset, count, iar => { }, new object())); });
                    AssertExtensions.Throws<ArgumentNullException>(ReadWriteAsyncResultName, () => { stream.EndWrite(null); });
                }

                // Invalid offset
                AssertExtensions.Throws<ArgumentOutOfRangeException>(ReadWriteOffsetName, () => { stream.Write(oneByteBuffer, -1, 0); });
                AssertExtensions.Throws<ArgumentOutOfRangeException>(ReadWriteOffsetName, () => { stream.WriteAsync(oneByteBuffer, -1, 0); });
                AssertExtensions.Throws<ArgumentOutOfRangeException>(ReadWriteOffsetName, () => { stream.WriteAsync(oneByteBuffer, -1, 0, default); });
                AssertExtensions.Throws<ArgumentOutOfRangeException>(ReadWriteOffsetName, () => { stream.EndWrite(stream.BeginWrite(oneByteBuffer, -1, 0, iar => { }, new object())); });
                Assert.ThrowsAny<ArgumentException>(() => { stream.Write(oneByteBuffer, 2, 0); });
                Assert.ThrowsAny<ArgumentException>(() => { stream.WriteAsync(oneByteBuffer, 2, 0); });
                Assert.ThrowsAny<ArgumentException>(() => { stream.WriteAsync(oneByteBuffer, 2, 0, default); });
                Assert.ThrowsAny<ArgumentException>(() => { stream.EndWrite(stream.BeginWrite(oneByteBuffer, 2, 0, iar => { }, new object())); });

                // Invalid count
                AssertExtensions.Throws<ArgumentOutOfRangeException>(ReadWriteCountName, () => { stream.Write(oneByteBuffer, 0, -1); });
                AssertExtensions.Throws<ArgumentOutOfRangeException>(ReadWriteCountName, () => { stream.WriteAsync(oneByteBuffer, 0, -1); });
                AssertExtensions.Throws<ArgumentOutOfRangeException>(ReadWriteCountName, () => { stream.WriteAsync(oneByteBuffer, 0, -1, default); });
                AssertExtensions.Throws<ArgumentOutOfRangeException>(ReadWriteCountName, () => { stream.EndWrite(stream.BeginWrite(oneByteBuffer, 0, -1, iar => { }, new object())); });
                Assert.ThrowsAny<ArgumentException>(() => { stream.Write(oneByteBuffer, 0, 2); });
                Assert.ThrowsAny<ArgumentException>(() => { stream.WriteAsync(oneByteBuffer, 0, 2); });
                Assert.ThrowsAny<ArgumentException>(() => { stream.WriteAsync(oneByteBuffer, 0, 2, default); });
                Assert.ThrowsAny<ArgumentException>(() => { stream.EndWrite(stream.BeginWrite(oneByteBuffer, 0, 2, iar => { }, new object())); });

                // Invalid offset + count
                foreach ((int invalidOffset, int invalidCount) in new[] { (1, 1) })
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
                Assert.Throws(UnsupportedReadWriteExceptionType, () => { stream.WriteAsync(new byte[1], 0, 1); });
                Assert.Throws(UnsupportedReadWriteExceptionType, () => { stream.WriteAsync(new Memory<byte>(new byte[1])); });
                await Assert.ThrowsAsync(UnsupportedReadWriteExceptionType, () => Task.Factory.FromAsync(stream.BeginWrite, stream.EndWrite, new byte[1], 0, 1, null));
                Assert.True(Record.Exception(() => stream.EndWrite(new NotImplementedIAsyncResult())) is Exception e && (e.GetType() == UnsupportedReadWriteExceptionType || e.GetType() == InvalidIAsyncResultExceptionType));
            }

            Assert.Equal(CanSeek, stream.CanSeek);
            if (stream.CanSeek)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => { stream.Position = -1; });
                Assert.Throws<ArgumentOutOfRangeException>(() => { stream.Seek(-1, SeekOrigin.Begin); });
                Assert.Throws<ArgumentOutOfRangeException>(() => { stream.Seek(0, (SeekOrigin)(-1)); });
                Assert.Throws<ArgumentOutOfRangeException>(() => { stream.Seek(0, (SeekOrigin)3); });
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
                Assert.Throws<ArgumentOutOfRangeException>(() => stream.ReadTimeout = 0);
                Assert.Throws<ArgumentOutOfRangeException>(() => stream.ReadTimeout = -2);
                Assert.Throws<ArgumentOutOfRangeException>(() => stream.WriteTimeout = 0);
                Assert.Throws<ArgumentOutOfRangeException>(() => stream.WriteTimeout = -2);
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
            await AssertDisposedAsync(async() => await stream.ReadAsync(new Memory<byte>(new byte[1])));
            AssertDisposed(() => { stream.EndRead(stream.BeginRead(new byte[1], 0, 1, null, null)); });

            AssertDisposed(() => { stream.WriteByte(1); });
            AssertDisposed(() => { stream.Write(new Span<byte>(new byte[1])); });
            AssertDisposed(() => { stream.Write(new byte[1], 0, 1); });
            await AssertDisposedAsync(async () => await stream.WriteAsync(new byte[1], 0, 1));
            await AssertDisposedAsync(async() => await stream.WriteAsync(new Memory<byte>(new byte[1])));
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

        protected async Task ValidatePrecanceledOperations_ThrowsCancellationException(Stream stream)
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();

            OperationCanceledException oce;

            if (stream.CanRead)
            {
                oce = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => stream.ReadAsync(new byte[1], 0, 1, cts.Token));
                Assert.Equal(cts.Token, oce.CancellationToken);

                oce = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => { await stream.ReadAsync(new Memory<byte>(new byte[1]), cts.Token); });
                Assert.Equal(cts.Token, oce.CancellationToken);
            }

            if (stream.CanWrite)
            {
                oce = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => stream.WriteAsync(new byte[1], 0, 1, cts.Token));
                Assert.Equal(cts.Token, oce.CancellationToken);

                oce = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => { await stream.WriteAsync(new ReadOnlyMemory<byte>(new byte[1]), cts.Token); });
                Assert.Equal(cts.Token, oce.CancellationToken);
            }

            Exception e = await Record.ExceptionAsync(() => stream.FlushAsync(cts.Token));
            if (e != null)
            {
                Assert.IsAssignableFrom<OperationCanceledException>(e);
            }
        }

        protected async Task ValidateCancelableReads_AfterInvocation_ThrowsCancellationException(Stream stream)
        {
            if (!stream.CanRead || !FullyCancelableOperations)
            {
                return;
            }

            CancellationTokenSource cts;
            OperationCanceledException oce;

            cts = new CancellationTokenSource(1);
            oce = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => stream.ReadAsync(new byte[1], 0, 1, cts.Token));
            Assert.Equal(cts.Token, oce.CancellationToken);

            cts = new CancellationTokenSource(1);
            oce = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => { await stream.ReadAsync(new Memory<byte>(new byte[1]), cts.Token); });
            Assert.Equal(cts.Token, oce.CancellationToken);
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
                var cts = new CancellationTokenSource();
                await Task.WhenAny(incomplete, Task.Delay(500, cts.Token)); // give second task a chance to complete
                cts.Cancel();
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
            public override void Post(SendOrPostCallback d, object state)
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
            protected override IEnumerable<Task> GetScheduledTasks() => null;
        }

        protected readonly struct JumpToThreadPoolAwaiter : ICriticalNotifyCompletion
        {
            public JumpToThreadPoolAwaiter GetAwaiter() => this;
            public bool IsCompleted => false;
            public void OnCompleted(Action continuation) => ThreadPool.QueueUserWorkItem(_ => continuation());
            public void UnsafeOnCompleted(Action continuation) => ThreadPool.UnsafeQueueUserWorkItem(_ => continuation(), null);
            public void GetResult() { }
        }

        protected sealed class MisbehavingDelegatingStream : Stream
        {
            public enum Mode
            {
                Default,
                ReturnNullTasks,
                ReturnTooSmallCounts,
                ReturnTooLargeCounts,
                ReadSlowly
            }

            private readonly Stream _stream;
            private readonly Mode _mode;

            public MisbehavingDelegatingStream(Stream innerStream, Mode mode)
            {
                _stream = innerStream;
                _mode = mode;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                switch (_mode)
                {
                    case Mode.ReturnTooSmallCounts:
                        return -1;
                    case Mode.ReturnTooLargeCounts:
                        return buffer.Length + 1;
                    case Mode.ReadSlowly:
                        return _stream.Read(buffer, offset, 1);
                    default:
                        return 0;
                }
            }

            public override void Write(byte[] buffer, int offset, int count) =>
                _stream.Write(buffer, offset, count);

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
                _mode == Mode.ReturnNullTasks ?
                   null :
                   base.ReadAsync(buffer, offset, count, cancellationToken);

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
                _mode == Mode.ReturnNullTasks ?
                   null :
                   base.WriteAsync(buffer, offset, count, cancellationToken);

            public override void Flush() => _stream.Flush();
            public override bool CanRead => _stream.CanRead;
            public override bool CanSeek => _stream.CanSeek;
            public override bool CanWrite => _stream.CanWrite;
            public override long Length => _stream.Length;
            public override long Position { get => _stream.Position; set => _stream.Position = value; }
            public override long Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);
            public override void SetLength(long value) => _stream.SetLength(value);
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
        public virtual async Task ArgumentValidation_ThrowsExpectedException()
        {
            using StreamPair streams = await CreateConnectedStreamsAsync();

            foreach (Stream stream in streams)
            {
                await ValidateMisuseExceptionsAsync(stream);
            }
        }

        [Fact]
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
        public virtual async Task ReadWriteAsync_Canceled_ThrowsOperationCanceledException()
        {
            using StreamPair streams = await CreateConnectedStreamsAsync();

            foreach (Stream stream in streams)
            {
                await ValidatePrecanceledOperations_ThrowsCancellationException(stream);
                await ValidateCancelableReads_AfterInvocation_ThrowsCancellationException(stream);
            }
        }

        [Fact]
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

                Assert.Equal(writerBytes, readerBytes);

                await writes;

                if (!FlushGuaranteesAllDataWritten)
                {
                    break;
                }
            }
        }

        public static IEnumerable<object[]> ReadWrite_Success_MemberData() =>
            from mode in new[] { ReadWriteMode.SyncSpan, ReadWriteMode.SyncArray, ReadWriteMode.SyncAPM, ReadWriteMode.AsyncArray, ReadWriteMode.AsyncMemory, ReadWriteMode.AsyncAPM }
            from writeSize in new[] { 1, 42, 10 * 1024 }
            from startWithFlush in new[] { false, true }
            select new object[] { mode, writeSize, startWithFlush };

        public static IEnumerable<object[]> ReadWrite_Success_Large_MemberData() =>
            from mode in new[] { ReadWriteMode.SyncSpan, ReadWriteMode.SyncArray, ReadWriteMode.SyncAPM, ReadWriteMode.AsyncArray, ReadWriteMode.AsyncMemory, ReadWriteMode.AsyncAPM }
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
        public virtual async Task ReadWrite_Success(ReadWriteMode mode, int writeSize, bool startWithFlush)
        {
            foreach (CancellationToken nonCanceledToken in new[] { CancellationToken.None, new CancellationTokenSource().Token })
            {
                using StreamPair streams = await CreateConnectedStreamsAsync();

                foreach ((Stream writeable, Stream readable) in GetReadWritePairs(streams))
                {
                    if (startWithFlush)
                    {
                        switch (mode)
                        {
                            case ReadWriteMode.SyncArray:
                            case ReadWriteMode.SyncSpan:
                            case ReadWriteMode.SyncAPM:
                                writeable.Flush();
                                break;

                            case ReadWriteMode.AsyncArray:
                            case ReadWriteMode.AsyncMemory:
                            case ReadWriteMode.AsyncAPM:
                                await writeable.FlushAsync(nonCanceledToken);
                                break;

                            default:
                                throw new Exception($"Unknown mode: {mode}");
                        }
                    }

                    byte[] writerBytes = RandomNumberGenerator.GetBytes(writeSize);
                    var readerBytes = new byte[writerBytes.Length];

                    Task writes = Task.Run(async () =>
                    {
                        switch (mode)
                        {
                            case ReadWriteMode.SyncArray:
                                writeable.Write(writerBytes, 0, writerBytes.Length);
                                break;

                            case ReadWriteMode.SyncSpan:
                                writeable.Write(writerBytes);
                                break;

                            case ReadWriteMode.AsyncArray:
                                await writeable.WriteAsync(writerBytes, 0, writerBytes.Length, nonCanceledToken);
                                break;

                            case ReadWriteMode.AsyncMemory:
                                await writeable.WriteAsync(writerBytes, nonCanceledToken);
                                break;

                            case ReadWriteMode.SyncAPM:
                                writeable.EndWrite(writeable.BeginWrite(writerBytes, 0, writerBytes.Length, null, null));
                                break;

                            case ReadWriteMode.AsyncAPM:
                                await Task.Factory.FromAsync(writeable.BeginWrite, writeable.EndWrite, writerBytes, 0, writerBytes.Length, null);
                                break;

                            default:
                                throw new Exception($"Unknown mode: {mode}");
                        }

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
                        int r = mode switch
                        {
                            ReadWriteMode.SyncArray => readable.Read(readerBytes, n, readerBytes.Length - n),
                            ReadWriteMode.SyncSpan => readable.Read(readerBytes.AsSpan(n)),
                            ReadWriteMode.AsyncArray => await readable.ReadAsync(readerBytes, n, readerBytes.Length - n, nonCanceledToken),
                            ReadWriteMode.AsyncMemory => await readable.ReadAsync(readerBytes.AsMemory(n), nonCanceledToken),
                            ReadWriteMode.SyncAPM => readable.EndRead(readable.BeginRead(readerBytes, n, readerBytes.Length - n, null, null)),
                            ReadWriteMode.AsyncAPM => await Task.Factory.FromAsync(readable.BeginRead, readable.EndRead, readerBytes, n, readerBytes.Length - n, null),
                            _ => throw new Exception($"Unknown mode: {mode}"),
                        };
                        Assert.InRange(r, 1, readerBytes.Length - n);
                        n += r;
                    }

                    Assert.Equal(readerBytes.Length, n);
                    Assert.Equal(writerBytes, readerBytes);

                    await writes;

                    if (!FlushGuaranteesAllDataWritten)
                    {
                        break;
                    }
                }
            }
        }

        [Theory]
        [InlineData(ReadWriteMode.SyncByte, false)]
        [InlineData(ReadWriteMode.SyncArray, false)]
        [InlineData(ReadWriteMode.SyncSpan, false)]
        [InlineData(ReadWriteMode.AsyncArray, false)]
        [InlineData(ReadWriteMode.AsyncMemory, false)]
        [InlineData(ReadWriteMode.SyncAPM, false)]
        [InlineData(ReadWriteMode.AsyncAPM, false)]
        [InlineData(ReadWriteMode.SyncByte, true)]
        [InlineData(ReadWriteMode.SyncArray, true)]
        [InlineData(ReadWriteMode.SyncSpan, true)]
        [InlineData(ReadWriteMode.AsyncArray, true)]
        [InlineData(ReadWriteMode.AsyncMemory, true)]
        [InlineData(ReadWriteMode.SyncAPM, true)]
        [InlineData(ReadWriteMode.AsyncAPM, true)]
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

            if (mode == ReadWriteMode.SyncByte)
            {
                Assert.Equal(-1, readable.ReadByte());
            }
            else
            {
                Assert.Equal(0, mode switch
                {
                    ReadWriteMode.SyncArray => readable.Read(new byte[1], 0, 1),
                    ReadWriteMode.SyncSpan => readable.Read(new byte[1]),
                    ReadWriteMode.AsyncArray => await readable.ReadAsync(new byte[1], 0, 1),
                    ReadWriteMode.AsyncMemory => await readable.ReadAsync(new byte[1]),
                    ReadWriteMode.SyncAPM => readable.EndRead(readable.BeginRead(new byte[1], 0, 1, null, null)),
                    ReadWriteMode.AsyncAPM => await Task.Factory.FromAsync(readable.BeginRead, readable.EndRead, new byte[1], 0, 1, null),
                    _ => throw new Exception($"Unknown mode: {mode}"),
                });
            }
        }

        [Theory]
        [InlineData(ReadWriteMode.SyncArray)]
        [InlineData(ReadWriteMode.AsyncArray)]
        [InlineData(ReadWriteMode.AsyncAPM)]
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

            Assert.Equal(1, mode switch
            {
                ReadWriteMode.SyncArray => readable.Read(buffer, offset, buffer.Length - offset),
                ReadWriteMode.AsyncArray => await readable.ReadAsync(buffer, offset, buffer.Length - offset),
                ReadWriteMode.AsyncAPM => await Task.Factory.FromAsync(readable.BeginRead, readable.EndRead, buffer, offset, buffer.Length - offset, null),
                _ => throw new Exception($"Unknown mode: {mode}"),
            });

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
        public virtual async Task Write_DataReadFromDesiredOffset(ReadWriteMode mode)
        {
            using StreamPair streams = await CreateConnectedStreamsAsync();
            (Stream writeable, Stream readable) = GetReadWritePair(streams);

            byte[] buffer = new[] { (byte)'\0', (byte)'\0', (byte)'h', (byte)'e', (byte)'l', (byte)'l', (byte)'o', (byte)'\0', (byte)'\0' };
            const int Offset = 2, Count = 5;

            Task write = Task.Run(async () =>
            {
                switch (mode)
                {
                    case ReadWriteMode.SyncArray:
                        writeable.Write(buffer, Offset, Count);
                        break;

                    case ReadWriteMode.AsyncArray:
                        await writeable.WriteAsync(buffer, Offset, Count);
                        break;

                    case ReadWriteMode.AsyncAPM:
                        await Task.Factory.FromAsync(writeable.BeginWrite, writeable.EndWrite, buffer, Offset, Count, null);
                        break;

                    default:
                        throw new Exception($"Unknown mode: {mode}");
                }

                writeable.Dispose();
            });

            Assert.Equal("hello", new StreamReader(readable).ReadToEnd());
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
        public virtual async Task ZeroByteRead_BlocksUntilDataAvailableOrNops(ReadWriteMode mode)
        {
            using StreamPair streams = await CreateConnectedStreamsAsync();
            foreach ((Stream writeable, Stream readable) in GetReadWritePairs(streams))
            {
                for (int iter = 0; iter < 2; iter++)
                {
                    Task<int> zeroByteRead = mode switch
                    {
                        ReadWriteMode.SyncSpan => Task.Run(() => readable.Read(Span<byte>.Empty)),
                        ReadWriteMode.SyncArray => Task.Run(() => readable.Read(new byte[0], 0, 0)),
                        ReadWriteMode.AsyncArray => readable.ReadAsync(new byte[0], 0, 0),
                        ReadWriteMode.AsyncMemory => readable.ReadAsync(Memory<byte>.Empty).AsTask(),
                        ReadWriteMode.SyncAPM => Task.Run(() => readable.EndRead(readable.BeginRead(Array.Empty<byte>(), 0, 0, null, null))),
                        ReadWriteMode.AsyncAPM => Task.Factory.FromAsync(readable.BeginRead, readable.EndRead, Array.Empty<byte>(), 0, 0, null),
                        _ => throw new Exception($"Unknown mode: {mode}"),
                    };

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
        public virtual async Task ZeroByteWrite_OtherDataReceivedSuccessfully(ReadWriteMode mode)
        {
            byte[][] buffers = new[] { Array.Empty<byte>(), Encoding.UTF8.GetBytes("hello"), Array.Empty<byte>(), Encoding.UTF8.GetBytes("world") };

            using StreamPair streams = await CreateConnectedStreamsAsync();
            foreach ((Stream writeable, Stream readable) in GetReadWritePairs(streams))
            {
                Task writes;
                switch (mode)
                {
                    case ReadWriteMode.SyncSpan:
                        writes = Task.Run(() =>
                        {
                            foreach (byte[] buffer in buffers)
                            {
                                writeable.Write(buffer.AsSpan());
                            }
                        });
                        break;

                    case ReadWriteMode.SyncArray:
                        writes = Task.Run(() =>
                        {
                            foreach (byte[] buffer in buffers)
                            {
                                writeable.Write(buffer, 0, buffer.Length);
                            }
                        });
                        break;

                    case ReadWriteMode.AsyncArray:
                        writes = Task.Run(async () =>
                        {
                            foreach (byte[] buffer in buffers)
                            {
                                await writeable.WriteAsync(buffer, 0, buffer.Length);
                            }
                        });
                        break;

                    case ReadWriteMode.AsyncMemory:
                        writes = Task.Run(async () =>
                        {
                            foreach (byte[] buffer in buffers)
                            {
                                await writeable.WriteAsync(buffer);
                            }
                        });
                        break;

                    case ReadWriteMode.SyncAPM:
                        writes = Task.Run(() =>
                        {
                            foreach (byte[] buffer in buffers)
                            {
                                writeable.EndWrite(writeable.BeginWrite(buffer, 0, buffer.Length, null, null));
                            }
                        });
                        break;

                    case ReadWriteMode.AsyncAPM:
                        writes = Task.Run(async () =>
                        {
                            foreach (byte[] buffer in buffers)
                            {
                                await Task.Factory.FromAsync(writeable.BeginWrite, writeable.EndWrite, buffer, 0, buffer.Length, null);
                            }
                        });
                        break;

                    default:
                        throw new Exception($"Unknown mode: {mode}");
                }

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

            Assert.Equal(dataToCopy, results.ToArray());
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
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => readable.ReadAsync(new byte[1], 0, 1, new CancellationToken(true)));
                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => { await readable.ReadAsync(new Memory<byte>(new byte[1]), new CancellationToken(true)); });

                var cts = new CancellationTokenSource();
                Task<int> t = readable.ReadAsync(new byte[1], 0, 1, cts.Token);
                cts.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t);

                cts = new CancellationTokenSource();
                ValueTask<int> vt = readable.ReadAsync(new Memory<byte>(new byte[1]), cts.Token);
                cts.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await vt);

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
