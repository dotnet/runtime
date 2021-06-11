// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.IO.Tests;
using System.Net.Quic.Implementations;
using System.Net.Security;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Quic.Tests
{
    public sealed class MockQuicStreamConformanceTests : QuicStreamConformanceTests
    {
        protected override QuicImplementationProvider Provider => QuicImplementationProviders.Mock;
    }

    [ConditionalClass(typeof(QuicTestBase<MsQuicProviderFactory>), nameof(QuicTestBase<MsQuicProviderFactory>.IsSupported))]
    public sealed class MsQuicQuicStreamConformanceTests : QuicStreamConformanceTests
    {
        protected override QuicImplementationProvider Provider => QuicImplementationProviders.MsQuic;

        // TODO: These are all hanging, likely due to Stream close behavior.
        [ActiveIssue("https://github.com/dotnet/runtime/issues/756")]
        public override Task Read_Eof_Returns0(ReadWriteMode mode, bool dataAvailableFirst) => base.Read_Eof_Returns0(mode, dataAvailableFirst);
        [ActiveIssue("https://github.com/dotnet/runtime/issues/756")]
        public override Task CopyToAsync_AllDataCopied(int byteCount, bool useAsync) => base.CopyToAsync_AllDataCopied(byteCount, useAsync);
        [ActiveIssue("https://github.com/dotnet/runtime/issues/756")]
        public override Task CopyToAsync_AllDataCopied_Large(bool useAsync) => base.CopyToAsync_AllDataCopied_Large(useAsync);
        [ActiveIssue("https://github.com/dotnet/runtime/issues/756")]
        public override Task Dispose_ClosesStream(int disposeMode) => base.Dispose_ClosesStream(disposeMode);
        [ActiveIssue("https://github.com/dotnet/runtime/issues/756")]
        public override Task Write_DataReadFromDesiredOffset(ReadWriteMode mode) => base.Write_DataReadFromDesiredOffset(mode);
        [ActiveIssue("https://github.com/dotnet/runtime/issues/756")]
        public override Task Parallel_ReadWriteMultipleStreamsConcurrently() => base.Parallel_ReadWriteMultipleStreamsConcurrently();

        // TODO: new additions, find out the actual reason for hanging
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49157")]
        public override Task ReadTimeout_Expires_Throws() => base.ReadTimeout_Expires_Throws();
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49157")]
        public override Task ConcurrentBidirectionalReadsWrites_Success() => base.ConcurrentBidirectionalReadsWrites_Success();
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49157")]
        public override Task ArgumentValidation_ThrowsExpectedException() => base.ArgumentValidation_ThrowsExpectedException();
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49157")]
        public override Task ReadWriteAsync_PrecanceledOperations_ThrowsCancellationException() => base.ReadWriteAsync_PrecanceledOperations_ThrowsCancellationException();
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49157")]
        public override Task Read_DataStoredAtDesiredOffset(ReadWriteMode mode) => base.Read_DataStoredAtDesiredOffset(mode);
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49157")]
        public override Task ReadAsync_CancelPendingRead_DoesntImpactSubsequentReads() => base.ReadAsync_CancelPendingRead_DoesntImpactSubsequentReads();
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49157")]
        public override Task Disposed_ThrowsObjectDisposedException() => base.Disposed_ThrowsObjectDisposedException();
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49157")]
        public override Task Timeout_Roundtrips() => base.Timeout_Roundtrips();
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49157")]
        public override Task ZeroByteWrite_OtherDataReceivedSuccessfully(ReadWriteMode mode) => base.ZeroByteWrite_OtherDataReceivedSuccessfully(mode);
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49157")]
        public override Task ReadAsync_ContinuesOnCurrentTaskSchedulerIfDesired(bool flowExecutionContext, bool? continueOnCapturedContext) => base.ReadAsync_ContinuesOnCurrentTaskSchedulerIfDesired(flowExecutionContext, continueOnCapturedContext);
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49157")]
        public override Task ZeroByteRead_BlocksUntilDataAvailableOrNops(ReadWriteMode mode) => base.ZeroByteRead_BlocksUntilDataAvailableOrNops(mode);
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49157")]
        public override Task ReadAsync_CancelPendingTask_ThrowsCancellationException() => base.ReadAsync_CancelPendingTask_ThrowsCancellationException();
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49157")]
        public override Task ReadAsync_ContinuesOnCurrentSynchronizationContextIfDesired(bool flowExecutionContext, bool? continueOnCapturedContext) => base.ReadAsync_ContinuesOnCurrentSynchronizationContextIfDesired(flowExecutionContext, continueOnCapturedContext);
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49157")]
        public override Task ReadWriteByte_Success() => base.ReadWriteByte_Success();
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49157")]
        public override Task ReadWrite_Success(ReadWriteMode mode, int writeSize, bool startWithFlush) => base.ReadWrite_Success(mode, writeSize, startWithFlush);
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49157")]
        public override Task ReadWrite_Success_Large(ReadWriteMode mode, int writeSize, bool startWithFlush) => base.ReadWrite_Success_Large(mode, writeSize, startWithFlush);
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49157")]
        public override Task Flush_ValidOnWriteableStreamWithNoData_Success() => base.Flush_ValidOnWriteableStreamWithNoData_Success();
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49157")]
        public override Task ReadAsync_CancelPendingValueTask_ThrowsCancellationException() => base.ReadAsync_CancelPendingValueTask_ThrowsCancellationException();
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49157")]
        public override Task ReadAsync_DuringReadAsync_ThrowsIfUnsupported() => base.ReadAsync_DuringReadAsync_ThrowsIfUnsupported();
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49157")]
        public override Task ReadWrite_CustomMemoryManager_Success(bool useAsync) => base.ReadWrite_CustomMemoryManager_Success(useAsync);
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49157")]
        public override Task Flush_ValidOnReadableStream_Success() => base.Flush_ValidOnReadableStream_Success();

    }

    public abstract class QuicStreamConformanceTests : ConnectedStreamConformanceTests
    {
        public SslServerAuthenticationOptions GetSslServerAuthenticationOptions()
        {
            return new SslServerAuthenticationOptions()
            {
                ApplicationProtocols = new List<SslApplicationProtocol>() { new SslApplicationProtocol("quictest") },
                // TODO: use a cert. MsQuic currently only allows certs that are trusted.
                ServerCertificate = System.Net.Test.Common.Configuration.Certificates.GetServerCertificate()
            };
        }

        protected abstract QuicImplementationProvider Provider { get; }

        protected override async Task<StreamPair> CreateConnectedStreamsAsync()
        {
            QuicImplementationProvider provider = Provider;
            var protocol = new SslApplicationProtocol("quictest");

            var listener = new QuicListener(
                provider,
                new IPEndPoint(IPAddress.Loopback, 0),
                GetSslServerAuthenticationOptions());

            QuicConnection connection1 = null, connection2 = null;
            QuicStream stream1 = null, stream2 = null;

            await WhenAllOrAnyFailed(
                Task.Run(async () =>
                {
                    connection1 = await listener.AcceptConnectionAsync();
                    stream1 = await connection1.AcceptStreamAsync();
                }),
                Task.Run(async () =>
                {
                    connection2 = new QuicConnection(
                        provider,
                        listener.ListenEndPoint,
                        new SslClientAuthenticationOptions() { ApplicationProtocols = new List<SslApplicationProtocol>() { protocol } });
                    await connection2.ConnectAsync();
                    stream2 = connection2.OpenBidirectionalStream();
                }));

            var result = new StreamPairWithOtherDisposables(stream1, stream2);
            result.Disposables.Add(connection1);
            result.Disposables.Add(connection2);
            result.Disposables.Add(listener);

            return result;
        }

        private sealed class StreamPairWithOtherDisposables : StreamPair
        {
            public readonly List<IDisposable> Disposables = new List<IDisposable>();

            public StreamPairWithOtherDisposables(Stream stream1, Stream stream2) : base(stream1, stream2) { }

            public override void Dispose()
            {
                base.Dispose();
                Disposables.ForEach(d => d.Dispose());
            }
        }
    }
}
