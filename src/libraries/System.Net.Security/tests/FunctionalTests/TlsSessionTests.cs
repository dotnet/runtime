// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

using TestCertificates = System.Net.Test.Common.Configuration.Certificates;

namespace System.Net.Security.Tests
{
    [PlatformSpecific(TestPlatforms.Linux | TestPlatforms.FreeBSD | TestPlatforms.Windows | TestPlatforms.OSX)]
    public class TlsSessionTests
    {
        private const int CipherBufSize = 32 * 1024;

        [Fact]
        public async Task ServerSession_AgainstSslStreamClient_HandshakeAndPingPong_Succeeds()
        {
            using X509Certificate2 serverCert = TestCertificates.GetServerCertificate();
            string serverName = serverCert.GetNameInfo(X509NameType.SimpleName, forIssuer: false);

            (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();
            using (clientStream)
            using (serverStream)
            using (SslStream clientSsl = new SslStream(clientStream, leaveInnerStreamOpen: false, TestHelper.AllowAnyServerCertificate))
            {
                using TlsContext ctx = TlsContext.Create(new SslServerAuthenticationOptions
                {
                    ServerCertificate = serverCert,
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                    ClientCertificateRequired = false,
                });
                using TlsSession session = TlsSession.Create(ctx);

                Task clientHandshake = clientSsl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                {
                    TargetHost = serverName,
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                    RemoteCertificateValidationCallback = TestHelper.AllowAnyServerCertificate,
                });

                Task serverHandshake = DriveHandshakeAsync(session, serverStream);

                await Task.WhenAll(clientHandshake, serverHandshake).WaitAsync(TimeSpan.FromSeconds(30));

                Assert.True(session.IsHandshakeComplete);
                Assert.True(clientSsl.IsAuthenticated);
                Assert.True(session.NegotiatedProtocol is SslProtocols.Tls12 or SslProtocols.Tls13);

                // Steady-state ping-pong.
                byte[] ping = "PING"u8.ToArray();
                byte[] pong = "PONG"u8.ToArray();

                // Client → server
                Task clientWrite = clientSsl.WriteAsync(ping).AsTask();
                byte[] received = await ReadOnePlaintextRecordAsync(session, serverStream, expectedLength: ping.Length);
                await clientWrite;
                Assert.Equal(ping, received);

                // Server → client
                await WritePlaintextAsync(session, serverStream, pong);
                byte[] back = new byte[pong.Length];
                int n = 0;
                while (n < back.Length)
                {
                    int r = await clientSsl.ReadAsync(back.AsMemory(n));
                    Assert.True(r > 0, "Client read returned 0 unexpectedly.");
                    n += r;
                }
                Assert.Equal(pong, back);
            }
        }

        // Server starts with TlsContext.Create((SslServerAuthenticationOptions?)null) - no options
        // baked in. ProcessHandshake parses the ClientHello, surfaces NeedsServerOptions with
        // ClientHelloInfo populated, and the caller picks options based on SNI before resuming.
        [Fact]
        public async Task ServerSession_DeferredOptions_SelectedFromSni_Succeeds()
        {
            using X509Certificate2 serverCert = TestCertificates.GetServerCertificate();
            string serverName = serverCert.GetNameInfo(X509NameType.SimpleName, forIssuer: false);

            int factoryCalls = 0;
            string? observedSni = null;

            (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();
            using (clientStream)
            using (serverStream)
            using (SslStream clientSsl = new SslStream(clientStream, leaveInnerStreamOpen: false, TestHelper.AllowAnyServerCertificate))
            {
                using TlsContext ctx = TlsContext.Create((SslServerAuthenticationOptions?)null);
                using TlsSession session = TlsSession.Create(ctx);

                Task clientHandshake = clientSsl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                {
                    TargetHost = serverName,
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                    RemoteCertificateValidationCallback = TestHelper.AllowAnyServerCertificate,
                });

                using TlsContext hostCtx = TlsContext.Create(new SslServerAuthenticationOptions
                {
                    ServerCertificate = serverCert,
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                });
                Task serverHandshake = DriveHandshakeAsync(session, serverStream, hello =>
                {
                    factoryCalls++;
                    observedSni = hello.ServerName;
                    return hostCtx;
                });

                await Task.WhenAll(clientHandshake, serverHandshake).WaitAsync(TimeSpan.FromSeconds(30));

                Assert.True(session.IsHandshakeComplete);
                Assert.True(clientSsl.IsAuthenticated);
                Assert.Equal(1, factoryCalls);
                Assert.Equal(serverName, observedSni);
            }
        }

        // Two consecutive handshakes against the same TlsContext / SslStream client
        // pair. With AllowTlsResume=true (default), the second handshake should resume
        // and transfer significantly fewer bytes than the first (no Certificate
        // message, abbreviated key exchange). With AllowTlsResume=false the byte
        // counts must be similar.
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows))]
        [InlineData(SslProtocols.Tls12, true)]
        [InlineData(SslProtocols.Tls12, false)]
        [InlineData(SslProtocols.Tls13, true)]
        [InlineData(SslProtocols.Tls13, false)]
        public async Task ServerSession_TlsResume_HonorsAllowTlsResumeOption(SslProtocols protocol, bool allowResume)
        {
            if (OperatingSystem.IsMacOS())
            {
                // Legacy SecureTransport server-side session cache / ticket issuance is not wired up,
                // so resumption never measurably shrinks the second handshake.
                return;
            }

            using X509Certificate2 serverCert = TestCertificates.GetServerCertificate();
            string serverName = serverCert.GetNameInfo(X509NameType.SimpleName, forIssuer: false);

            using TlsContext serverCtx = TlsContext.Create(new SslServerAuthenticationOptions
            {
                ServerCertificate = serverCert,
                EnabledSslProtocols = protocol,
                ClientCertificateRequired = false,
                AllowTlsResume = allowResume,
            });

            long bytes1 = await MeasureHandshakeBytesAsync(serverCtx, serverName, protocol);
            long bytes2 = await MeasureHandshakeBytesAsync(serverCtx, serverName, protocol);

            if (allowResume)
            {
                // Resumption omits the server Certificate (~1KB+ for the test cert) plus
                // the full key-exchange / cert-verify sequence on TLS 1.2. 60% headroom.
                Assert.True(bytes2 < bytes1 * 0.6,
                    $"Expected resumed handshake to be much smaller. first={bytes1} second={bytes2}");
            }
            else
            {
                // No resume: byte counts must be within ~25% of each other.
                long diff = Math.Abs(bytes2 - bytes1);
                Assert.True(diff < bytes1 / 4,
                    $"Expected similar handshake sizes when resume disabled. first={bytes1} second={bytes2}");
            }
        }

        private static async Task<long> MeasureHandshakeBytesAsync(TlsContext serverCtx, string serverName, SslProtocols protocol)
        {
            (Socket cs, Socket ss) = await CreateLoopbackSocketPairAsync();
            using (cs)
            using (ss)
            {
                var counter = new ByteCountingStream(new NetworkStream(ss, ownsSocket: false));
                using var clientStream = new NetworkStream(cs, ownsSocket: false);
                using var clientSsl = new SslStream(clientStream, leaveInnerStreamOpen: false, TestHelper.AllowAnyServerCertificate);
                using TlsSession session = TlsSession.Create(serverCtx);

                Task clientHandshake = clientSsl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                {
                    TargetHost = serverName,
                    EnabledSslProtocols = protocol,
                    RemoteCertificateValidationCallback = TestHelper.AllowAnyServerCertificate,
                });
                Task serverHandshake = DriveHandshakeAsync(session, counter);
                await Task.WhenAll(clientHandshake, serverHandshake).WaitAsync(TimeSpan.FromSeconds(30));

                // Round-trip a byte so any TLS 1.3 NewSessionTicket records are flushed
                // and counted before the connection tears down.
                await clientSsl.WriteAsync(new byte[] { 0xAB });
                byte[] scratch = ArrayPool<byte>.Shared.Rent(CipherBufSize);
                try
                {
                    byte[] received = await ReadOnePlaintextRecordAsync(session, counter, expectedLength: 1);
                    Assert.Equal(0xAB, received[0]);
                    await WritePlaintextAsync(session, counter, new byte[] { 0xCD });
                    byte[] rx = new byte[1];
                    int n = await clientSsl.ReadAsync(rx);
                    Assert.Equal(1, n);
                    Assert.Equal(0xCD, rx[0]);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(scratch);
                }

                return counter.BytesRead + counter.BytesWritten;
            }
        }

        private sealed class ByteCountingStream : Stream
        {
            private readonly Stream _inner;
            public long BytesRead;
            public long BytesWritten;
            public ByteCountingStream(Stream inner) { _inner = inner; }
            public override bool CanRead => _inner.CanRead;
            public override bool CanWrite => _inner.CanWrite;
            public override bool CanSeek => false;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            public override void Flush() => _inner.Flush();
            public override Task FlushAsync(CancellationToken ct) => _inner.FlushAsync(ct);
            public override long Seek(long o, SeekOrigin r) => throw new NotSupportedException();
            public override void SetLength(long v) => throw new NotSupportedException();
            public override int Read(byte[] b, int o, int c) { int n = _inner.Read(b, o, c); BytesRead += n; return n; }
            public override async ValueTask<int> ReadAsync(Memory<byte> m, CancellationToken ct = default) { int n = await _inner.ReadAsync(m, ct); BytesRead += n; return n; }
            public override void Write(byte[] b, int o, int c) { _inner.Write(b, o, c); BytesWritten += c; }
            public override async ValueTask WriteAsync(ReadOnlyMemory<byte> m, CancellationToken ct = default) { await _inner.WriteAsync(m, ct); BytesWritten += m.Length; }
        }

        [Fact]
        public void TlsContext_NullServerOptions_DefersResolution()
        {
            // Null server options are allowed: the server-side session parses the ClientHello
            // and suspends on NeedsServerOptions so the caller can resolve options via
            // SetServerContext (e.g. SNI-driven). Only the client overload rejects null.
            using TlsContext ctx = TlsContext.Create((SslServerAuthenticationOptions)null!);
            Assert.True(ctx.IsServer);

            Assert.Throws<ArgumentNullException>(() => TlsContext.Create((SslClientAuthenticationOptions)null!));
        }

        [Fact]
        public void TlsSession_OperationsBeforeHandshake_Throw()
        {
            using X509Certificate2 serverCert = TestCertificates.GetServerCertificate();
            using TlsContext ctx = TlsContext.Create(new SslServerAuthenticationOptions { ServerCertificate = serverCert });
            using TlsSession session = TlsSession.Create(ctx);

            byte[] buf = new byte[16];
            Assert.Throws<InvalidOperationException>(() => session.Encrypt(buf, buf, out _, out _));
            Assert.Throws<InvalidOperationException>(() => session.Decrypt(buf, buf, out _, out _));
        }

        [Fact]
        public async Task ServerSession_Shutdown_DeliversCloseNotifyToSslStreamClient()
        {
            using X509Certificate2 serverCert = TestCertificates.GetServerCertificate();
            string serverName = serverCert.GetNameInfo(X509NameType.SimpleName, forIssuer: false);

            (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();
            using (clientStream)
            using (serverStream)
            using (SslStream clientSsl = new SslStream(clientStream, leaveInnerStreamOpen: false, TestHelper.AllowAnyServerCertificate))
            {
                using TlsContext ctx = TlsContext.Create(new SslServerAuthenticationOptions
                {
                    ServerCertificate = serverCert,
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                    ClientCertificateRequired = false,
                });
                using TlsSession session = TlsSession.Create(ctx);

                Task clientHandshake = clientSsl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                {
                    TargetHost = serverName,
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                    RemoteCertificateValidationCallback = TestHelper.AllowAnyServerCertificate,
                });
                Task serverHandshake = DriveHandshakeAsync(session, serverStream);
                await Task.WhenAll(clientHandshake, serverHandshake).WaitAsync(TimeSpan.FromSeconds(30));

                byte[] scratch = ArrayPool<byte>.Shared.Rent(CipherBufSize);
                try
                {
                    TlsOperationStatus status;
                    do
                    {
                        status = session.Shutdown(scratch, out int produced);
                        if (produced > 0)
                        {
                            await serverStream.WriteAsync(scratch.AsMemory(0, produced));
                            await serverStream.FlushAsync();
                        }
                    }
                    while (status == TlsOperationStatus.WantWrite);

                    Assert.Equal(TlsOperationStatus.Closed, status);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(scratch);
                }

                // Client should observe EOF (close_notify) on the next read.
                byte[] buf = new byte[16];
                int n = await clientSsl.ReadAsync(buf).AsTask().WaitAsync(TimeSpan.FromSeconds(30));
                Assert.Equal(0, n);
            }
        }

        [Fact]
        public async Task ServerSession_MutualAuth_InitialHandshake_InvokesValidator()
        {
            using X509Certificate2 serverCert = TestCertificates.GetServerCertificate();
            using X509Certificate2 clientCert = TestCertificates.GetClientCertificate();
            string serverName = serverCert.GetNameInfo(X509NameType.SimpleName, forIssuer: false);

            int validatorCalls = 0;
            X509Certificate2? observedClientCert = null;

            (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();
            using (clientStream)
            using (serverStream)
            using (SslStream clientSsl = new SslStream(clientStream, leaveInnerStreamOpen: false, TestHelper.AllowAnyServerCertificate))
            {
                using TlsContext ctx = TlsContext.Create(new SslServerAuthenticationOptions
                {
                    ServerCertificate = serverCert,
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                    ClientCertificateRequired = true,
                    RemoteCertificateValidationCallback = (s, c, ch, e) =>
                    {
                        validatorCalls++;
                        observedClientCert = c as X509Certificate2;
                        return true;
                    },
                });
                using TlsSession session = TlsSession.Create(ctx);

                Task clientHandshake = clientSsl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                {
                    TargetHost = serverName,
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                    ClientCertificates = new X509CertificateCollection { clientCert },
                    RemoteCertificateValidationCallback = TestHelper.AllowAnyServerCertificate,
                });
                Task serverHandshake = DriveHandshakeAsync(session, serverStream);
                await Task.WhenAll(clientHandshake, serverHandshake).WaitAsync(TimeSpan.FromSeconds(30));

                Assert.True(session.IsHandshakeComplete);
                Assert.Equal(1, validatorCalls);
                Assert.NotNull(observedClientCert);
                Assert.Equal(clientCert.Thumbprint, observedClientCert!.Thumbprint);

                using X509Certificate2? remote = session.GetRemoteCertificate();
                Assert.NotNull(remote);
                Assert.Equal(clientCert.Thumbprint, remote!.Thumbprint);
            }
        }

        // Cross-platform baseline: SslStream on BOTH sides, server rejects client cert.
        // - TLS 1.2: server validates client cert before sending ServerFinished, so the client's
        //   AuthenticateAsClientAsync must throw AuthenticationException.
        // - TLS 1.3: server sends Finished before processing the client's Certificate, so the
        //   client's AuthenticateAsClientAsync completes; the rejection surfaces only on the
        //   first encrypted I/O after handshake (per TLS 1.3 spec, RFC 8446 §4.4.2.4).
        // This pins the protocol-level expectation against which TlsSession behavior is compared.
        [Theory]
        [InlineData(SslProtocols.Tls12)]
        [InlineData(SslProtocols.Tls13)]
        public async Task SslStreamServer_RejectsClientCert_ClientObservesAlert(SslProtocols protocol)
        {
            using X509Certificate2 serverCert = TestCertificates.GetServerCertificate();
            using X509Certificate2 clientCert = TestCertificates.GetClientCertificate();
            string serverName = serverCert.GetNameInfo(X509NameType.SimpleName, forIssuer: false);

            (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();
            using (clientStream)
            using (serverStream)
            using (SslStream clientSsl = new SslStream(clientStream, leaveInnerStreamOpen: false, TestHelper.AllowAnyServerCertificate))
            using (SslStream serverSsl = new SslStream(serverStream, leaveInnerStreamOpen: false, (_, _, _, _) => false))
            {
                Task serverAuth = serverSsl.AuthenticateAsServerAsync(new SslServerAuthenticationOptions
                {
                    ServerCertificate = serverCert,
                    EnabledSslProtocols = protocol,
                    ClientCertificateRequired = true,
                });
                Task clientAuth = clientSsl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                {
                    TargetHost = serverName,
                    EnabledSslProtocols = protocol,
                    ClientCertificates = new X509CertificateCollection { clientCert },
                    RemoteCertificateValidationCallback = TestHelper.AllowAnyServerCertificate,
                });

                await Assert.ThrowsAsync<AuthenticationException>(() => serverAuth.WaitAsync(TimeSpan.FromSeconds(30)));

                if (protocol == SslProtocols.Tls12)
                {
                    await Assert.ThrowsAsync<AuthenticationException>(() => clientAuth.WaitAsync(TimeSpan.FromSeconds(30)));
                    Assert.False(clientSsl.IsAuthenticated);
                }
                else
                {
                    await clientAuth.WaitAsync(TimeSpan.FromSeconds(30));
                    Assert.True(clientSsl.IsAuthenticated);
                    byte[] buf = new byte[1];
                    await Assert.ThrowsAnyAsync<IOException>(async () =>
                    {
                        await clientSsl.WriteAsync(buf).AsTask().WaitAsync(TimeSpan.FromSeconds(30));
                        await clientSsl.ReadAsync(buf).AsTask().WaitAsync(TimeSpan.FromSeconds(30));
                    });
                }
            }
        }

        // TlsSession does NOT wire SslAuthenticationOptions.RemoteCertificateValidator (unlike
        // SslStream, which sets it to SslStream.VerifyRemoteCertificate). The OpenSSL
        // CertVerifyCallback therefore takes the wedge branch (accept-and-defer) even when the
        // caller passes RemoteCertificateValidationCallback on the underlying server options:
        // the callback is only invoked later, by AcceptWithDefaultValidation, after the caller
        // resolves the post-hoc NeedsCertificateValidation suspension. Document that with a
        // test so the API contract is explicit — there is exactly one validation timing on
        // TlsSession (post-hoc), and the SslStream-style in-callback timing is unavailable.
        [Theory]
        [InlineData(SslProtocols.Tls12)]
        [InlineData(SslProtocols.Tls13)]
        public async Task ServerSession_RemoteCertificateValidationCallback_IsInvokedPostHoc(SslProtocols protocol)
        {
            using X509Certificate2 serverCert = TestCertificates.GetServerCertificate();
            using X509Certificate2 clientCert = TestCertificates.GetClientCertificate();
            string serverName = serverCert.GetNameInfo(X509NameType.SimpleName, forIssuer: false);

            int validatorCalls = 0;
            (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();
            using (clientStream)
            using (serverStream)
            using (SslStream clientSsl = new SslStream(clientStream, leaveInnerStreamOpen: false, TestHelper.AllowAnyServerCertificate))
            {
                using TlsContext ctx = TlsContext.Create(new SslServerAuthenticationOptions
                {
                    ServerCertificate = serverCert,
                    EnabledSslProtocols = protocol,
                    ClientCertificateRequired = true,
                    RemoteCertificateValidationCallback = (_, _, _, _) =>
                    {
                        Interlocked.Increment(ref validatorCalls);
                        return true;
                    },
                });
                using TlsSession session = TlsSession.Create(ctx);

                Task clientAuth = clientSsl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                {
                    TargetHost = serverName,
                    EnabledSslProtocols = protocol,
                    ClientCertificates = new X509CertificateCollection { clientCert },
                    RemoteCertificateValidationCallback = TestHelper.AllowAnyServerCertificate,
                });
                Task serverHandshake = DriveHandshakeAsync(session, serverStream);
                await Task.WhenAll(clientAuth, serverHandshake).WaitAsync(TimeSpan.FromSeconds(30));

                // The callback is invoked exactly once, via AcceptWithDefaultValidation inside
                // DriveHandshakeAsync's NeedsCertificateValidation branch — not from within the
                // OpenSSL CertVerifyCallback during the wire handshake.
                Assert.Equal(1, validatorCalls);
                Assert.True(session.IsHandshakeComplete);
                Assert.True(clientSsl.IsAuthenticated);
            }
        }

        // TlsSession server rejects the presented client cert post-hoc via
        // SetRemoteCertificateValidationResult on NeedsCertificateValidation. On OpenSSL 3.x
        // SSL_set_retry_verify is not honored for the peer-cert verification callback on a
        // server SSL (the callback is not re-entered), so CertVerifyCallback takes the
        // accept-and-defer branch on the server path. The wire handshake therefore completes
        // before the caller's verdict is known.
        //
        // Documented current behavior (mirrors SslStreamServer_RejectsClientCert_... only for
        // the server-side fault surfacing; the client will NOT see a fatal alert until upstream
        // OpenSSL gains server-side retry-verify support):
        // - Both TLS 1.2 and 1.3: client's AuthenticateAsClientAsync completes; the reject
        //   surfaces on the server as AuthenticationException when the caller invokes
        //   Encrypt/Decrypt after SetRemoteCertificateValidationResult(errors).
        // - The client observes an EndOfStream/IOException only when it attempts I/O after
        //   the server closes the transport (post-hoc, not mid-handshake).
        //
        // Once the OpenSSL fix lands, this test should be tightened to assert an
        // AuthenticationException on the client side (TLS 1.2) or on first I/O (TLS 1.3),
        // matching SslStreamServer_RejectsClientCert_ClientObservesAlert.
        [Theory]
        [InlineData(SslProtocols.Tls12)]
        [InlineData(SslProtocols.Tls13)]
        public async Task ServerSession_ExternalValidation_RejectsClientCert_ServerFaultsPostHoc(SslProtocols protocol)
        {
            using X509Certificate2 serverCert = TestCertificates.GetServerCertificate();
            using X509Certificate2 clientCert = TestCertificates.GetClientCertificate();
            string serverName = serverCert.GetNameInfo(X509NameType.SimpleName, forIssuer: false);

            (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();
            using (clientStream)
            using (serverStream)
            using (SslStream clientSsl = new SslStream(clientStream, leaveInnerStreamOpen: false, TestHelper.AllowAnyServerCertificate))
            {
                using TlsContext ctx = TlsContext.Create(new SslServerAuthenticationOptions
                {
                    ServerCertificate = serverCert,
                    EnabledSslProtocols = protocol,
                    ClientCertificateRequired = true,
                    // No RemoteCertificateValidationCallback — caller drives validation externally.
                });
                using TlsSession session = TlsSession.Create(ctx);

                Task clientAuth = clientSsl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                {
                    TargetHost = serverName,
                    EnabledSslProtocols = protocol,
                    ClientCertificates = new X509CertificateCollection { clientCert },
                    RemoteCertificateValidationCallback = TestHelper.AllowAnyServerCertificate,
                });

                bool suspensionObserved = false;
                string? observedClientCertThumbprint = null;
                Exception? serverFault = null;
                Task serverHandshake = Task.Run(async () =>
                {
                    try
                    {
                        await DriveHandshakeWithExternalValidationAsync(
                            session, serverStream,
                            onSuspend: () =>
                            {
                                suspensionObserved = true;
                                // Capture the thumbprint before SetRemoteCertificateValidationResult
                                // disposes the pending cert on the reject path.
                                using (X509Certificate2? observed = session.GetRemoteCertificate())
                                {
                                    observedClientCertThumbprint = observed?.Thumbprint;
                                }
                                session.SetRemoteCertificateValidationResult(SslPolicyErrors.RemoteCertificateChainErrors);
                            });
                    }
                    catch (AuthenticationException ex) { serverFault = ex; }
                });

                // Client's handshake completes on both TLS versions today (upstream OpenSSL
                // limitation; the caller's server-side rejection cannot inject a mid-handshake
                // alert). Give the client a moment to finish and don't assert on it.
                await clientAuth.WaitAsync(TimeSpan.FromSeconds(30));

                await serverHandshake.WaitAsync(TimeSpan.FromSeconds(30));
                Assert.True(suspensionObserved, "Server never observed NeedsCertificateValidation.");
                Assert.NotNull(observedClientCertThumbprint);
                Assert.Equal(clientCert.Thumbprint, observedClientCertThumbprint);

                // Server-side, the rejection MUST surface as AuthenticationException on the
                // next session operation. If the DriveHandshakeWithExternalValidationAsync
                // helper already threw, we captured it; otherwise assert on an Encrypt call.
                if (serverFault is null)
                {
                    byte[] pt = "blocked"u8.ToArray();
                    byte[] ct = new byte[CipherBufSize];
                    Assert.Throws<AuthenticationException>(() => session.Encrypt(pt, ct, out _, out _));
                }

            }
        }
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows))]
        [InlineData(SslProtocols.Tls12)]
        [InlineData(SslProtocols.Tls13)]
        [SkipOnPlatform(TestPlatforms.OSX, "SecureTransport does not surface a deferred client-credential prompt; SslStream supplies the certificate up-front.")]
        public async Task ClientSession_WantCredentials_SetClientCertificateContext_ResumesHandshake(SslProtocols protocol)
        {
            // Server (SslStream) demands a client certificate. The client TlsContext is
            // created without one, so ProcessHandshake must surface WantCredentials when
            // the CertificateRequest arrives. Supplying an SslStreamCertificateContext via
            // SetClientCertificateContext and re-entering ProcessHandshake with empty input
            // must resume the handshake to completion and deliver the cert to the server.
            // SChannel resolves client credentials up-front via AcquireCredentialsHandle
            // and never surfaces CredentialsNeeded; this flow is OpenSSL-only.
            //
            // AllowTlsResume is disabled on both peers so a session cached by a sibling
            // parallel test cannot let this client resume and skip the CertificateRequest.
            using X509Certificate2 serverCert = TestCertificates.GetServerCertificate();
            using X509Certificate2 clientCert = TestCertificates.GetClientCertificate();
            string serverName = serverCert.GetNameInfo(X509NameType.SimpleName, forIssuer: false);

            X509Certificate2? observedClientCert = null;

            (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();
            using (clientStream)
            using (serverStream)
            using (SslStream serverSsl = new SslStream(serverStream, leaveInnerStreamOpen: false))
            {
                Task serverHandshake = serverSsl.AuthenticateAsServerAsync(new SslServerAuthenticationOptions
                {
                    ServerCertificate = serverCert,
                    EnabledSslProtocols = protocol,
                    AllowTlsResume = false,
                    ClientCertificateRequired = true,
                    RemoteCertificateValidationCallback = (s, c, ch, e) =>
                    {
                        observedClientCert = c as X509Certificate2;
                        return true;
                    },
                });

                using TlsContext ctx = TlsContext.Create(new SslClientAuthenticationOptions
                {
                    TargetHost = serverName,
                    EnabledSslProtocols = protocol,
                    AllowTlsResume = false,
                    RemoteCertificateValidationCallback = TestHelper.AllowAnyServerCertificate,
                });
                using TlsSession session = TlsSession.Create(ctx);

                int wantCredentialsCount = 0;
                Task clientHandshake = Task.Run(async () =>
                {
                    byte[] netIn = ArrayPool<byte>.Shared.Rent(CipherBufSize);
                    byte[] netOut = ArrayPool<byte>.Shared.Rent(CipherBufSize);
                    int inUsed = 0;
                    try
                    {
                        while (!session.IsHandshakeComplete)
                        {
                            TlsOperationStatus status = session.ProcessHandshake(
                                netIn.AsSpan(0, inUsed),
                                netOut,
                                out int consumed,
                                out int produced);

                            if (consumed > 0)
                            {
                                if (consumed < inUsed)
                                {
                                    Buffer.BlockCopy(netIn, consumed, netIn, 0, inUsed - consumed);
                                }
                                inUsed -= consumed;
                            }

                            if (produced > 0)
                            {
                                await clientStream.WriteAsync(netOut.AsMemory(0, produced));
                                await clientStream.FlushAsync();
                            }

                            switch (status)
                            {
                                case TlsOperationStatus.Complete:
                                    continue;

                                case TlsOperationStatus.NeedsCertificateValidation:
                                    session.AcceptWithDefaultValidation();
                                    continue;

                                case TlsOperationStatus.WantCredentials:
                                    wantCredentialsCount++;
                                    // GetAcceptableIssuers should not throw while suspended on WantCredentials.
                                    // The server in this test does not configure SslCertificateTrust, so the
                                    // returned list is platform-dependent: OpenSSL omits the CA hints entirely
                                    // (null), SChannel may surface an empty hint set (null per our contract).
                                    IReadOnlyList<string>? issuers = session.GetAcceptableIssuers();
                                    Assert.True(issuers is null || issuers.Count > 0);
                                    session.SetClientCertificateContext(
                                        SslStreamCertificateContext.Create(clientCert, additionalCertificates: null));
                                    continue;

                                case TlsOperationStatus.WantWrite:
                                    await DrainAsync(session, clientStream, netOut);
                                    continue;

                                case TlsOperationStatus.WantRead:
                                    int r = await clientStream.ReadAsync(netIn.AsMemory(inUsed));
                                    if (r == 0)
                                    {
                                        throw new IOException("Unexpected EOF during handshake.");
                                    }
                                    inUsed += r;
                                    continue;

                                case TlsOperationStatus.Closed:
                                    throw new IOException("Peer closed connection during handshake.");
                            }
                        }
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(netIn);
                        ArrayPool<byte>.Shared.Return(netOut);
                    }
                });

                await Task.WhenAll(clientHandshake, serverHandshake).WaitAsync(TimeSpan.FromSeconds(30));

                Assert.True(session.IsHandshakeComplete);
                Assert.Equal(1, wantCredentialsCount);
                Assert.NotNull(observedClientCert);
                Assert.Equal(clientCert.Thumbprint, observedClientCert!.Thumbprint);
            }
        }

        [Fact]
        public async Task ServerSession_OptionalClientCert_NoCertSent_HandshakeCompletesWithoutValidatorCall()
        {
            // Server: ClientCertificateRequired = false, client sends no certificate.
            // Matches SslStream semantics: when a user RemoteCertificateValidationCallback is
            // supplied, it is invoked once with a null certificate and RemoteCertificateNotAvailable
            // so the caller can decide whether to accept the anonymous client. GetRemoteCertificate
            // returns null because no peer certificate was exchanged.
            using X509Certificate2 serverCert = TestCertificates.GetServerCertificate();
            string serverName = serverCert.GetNameInfo(X509NameType.SimpleName, forIssuer: false);

            int validatorCalls = 0;
            X509Certificate? observedCert = null;
            SslPolicyErrors observedErrors = SslPolicyErrors.None;

            (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();
            using (clientStream)
            using (serverStream)
            using (SslStream clientSsl = new SslStream(clientStream, leaveInnerStreamOpen: false, TestHelper.AllowAnyServerCertificate))
            {
                using TlsContext ctx = TlsContext.Create(new SslServerAuthenticationOptions
                {
                    ServerCertificate = serverCert,
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                    ClientCertificateRequired = false,
                    RemoteCertificateValidationCallback = (s, c, ch, e) =>
                    {
                        Interlocked.Increment(ref validatorCalls);
                        observedCert = c;
                        observedErrors = e;
                        return true;
                    },
                });
                using TlsSession session = TlsSession.Create(ctx);

                Task clientHandshake = clientSsl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                {
                    TargetHost = serverName,
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                });
                Task serverHandshake = DriveHandshakeAsync(session, serverStream);
                await Task.WhenAll(clientHandshake, serverHandshake).WaitAsync(TimeSpan.FromSeconds(30));

                Assert.True(session.IsHandshakeComplete);
                Assert.Equal(1, validatorCalls);
                Assert.Null(observedCert);
                Assert.Equal(SslPolicyErrors.RemoteCertificateNotAvailable, observedErrors);
                Assert.Null(session.GetRemoteCertificate());
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.OSX, "SecureTransport does not expose the TLS exporter required to compute tls-server-end-point channel binding here.")]
        public async Task ServerSession_ChannelBinding_MatchesSslStreamClient()
        {
            using X509Certificate2 serverCert = TestCertificates.GetServerCertificate();
            string serverName = serverCert.GetNameInfo(X509NameType.SimpleName, forIssuer: false);

            (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();
            using (clientStream)
            using (serverStream)
            using (SslStream clientSsl = new SslStream(clientStream, leaveInnerStreamOpen: false, TestHelper.AllowAnyServerCertificate))
            {
                using TlsContext ctx = TlsContext.Create(new SslServerAuthenticationOptions
                {
                    ServerCertificate = serverCert,
                    EnabledSslProtocols = SslProtocols.Tls12,
                    ClientCertificateRequired = false,
                });
                using TlsSession session = TlsSession.Create(ctx);

                Task clientHandshake = clientSsl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                {
                    TargetHost = serverName,
                    EnabledSslProtocols = SslProtocols.Tls12,
                    RemoteCertificateValidationCallback = TestHelper.AllowAnyServerCertificate,
                });
                Task serverHandshake = DriveHandshakeAsync(session, serverStream);
                await Task.WhenAll(clientHandshake, serverHandshake).WaitAsync(TimeSpan.FromSeconds(30));

                using ChannelBinding? serverBinding = session.GetChannelBinding(ChannelBindingKind.Unique);
                using ChannelBinding? clientBinding = clientSsl.TransportContext?.GetChannelBinding(ChannelBindingKind.Unique);

                Assert.NotNull(serverBinding);
                Assert.NotNull(clientBinding);
                Assert.False(serverBinding!.IsInvalid);
                Assert.Equal(clientBinding!.Size, serverBinding.Size);

                byte[] s = new byte[serverBinding.Size];
                byte[] c = new byte[clientBinding.Size];
                System.Runtime.InteropServices.Marshal.Copy(serverBinding.DangerousGetHandle(), s, 0, s.Length);
                System.Runtime.InteropServices.Marshal.Copy(clientBinding.DangerousGetHandle(), c, 0, c.Length);
                Assert.Equal(c, s);
            }
        }

        // Pure in-memory TlsSession <-> TlsSession exchange. The test runs both
        // TLS 1.2 and TLS 1.3 to exercise the post-handshake record path.
        //
        // TLS 1.3 note: after the server consumes the client Finished, OpenSSL
        // emits one or more NewSessionTicket records on the server->client side.
        // The client MUST process those (via Decrypt) before its first Encrypt
        // call -- otherwise OpenSSL on the client side has not yet finalized
        // its write-key transition from client_handshake_traffic_secret to
        // client_application_traffic_secret, and the server (which has already
        // transitioned its read key) rejects the resulting ciphertext as
        // "decryption failed or bad record mac". In real network deployments
        // the client's receive pump consumes these bytes naturally; in this
        // synchronous in-memory loop we drain them explicitly below.
        [Theory]
        [InlineData(SslProtocols.Tls12)]
        [InlineData(SslProtocols.Tls13)]
        public void TwoSessions_HandshakeAndPingPong_InMemory_Succeeds(SslProtocols protocols)
        {
            if (protocols == SslProtocols.Tls13 && OperatingSystem.IsMacOS())
            {
                // SecureTransport (the legacy macOS TLS backend used here) does not implement TLS 1.3.
                return;
            }

            using X509Certificate2 serverCert = TestCertificates.GetServerCertificate();
            string serverName = serverCert.GetNameInfo(X509NameType.SimpleName, forIssuer: false);

            using TlsContext serverCtx = TlsContext.Create(new SslServerAuthenticationOptions
            {
                ServerCertificate = serverCert,
                EnabledSslProtocols = protocols,
                ClientCertificateRequired = false,
            });
            using TlsContext clientCtx = TlsContext.Create(new SslClientAuthenticationOptions
            {
                TargetHost = serverName,
                EnabledSslProtocols = protocols,
                RemoteCertificateValidationCallback = TestHelper.AllowAnyServerCertificate,
            });

            using TlsSession server = TlsSession.Create(serverCtx);
            using TlsSession client = TlsSession.Create(clientCtx);

            byte[] cToS = new byte[CipherBufSize]; int cToSLen = 0;
            byte[] sToC = new byte[CipherBufSize]; int sToCLen = 0;

            for (int round = 0; round < 32 && (!client.IsHandshakeComplete || !server.IsHandshakeComplete); round++)
            {
                StepHandshakeInMemory(client, sToC, ref sToCLen, cToS, ref cToSLen);
                StepHandshakeInMemory(server, cToS, ref cToSLen, sToC, ref sToCLen);
            }

            Assert.True(client.IsHandshakeComplete);
            Assert.True(server.IsHandshakeComplete);
            Assert.Equal(client.NegotiatedProtocol, server.NegotiatedProtocol);
            Assert.Equal(protocols, client.NegotiatedProtocol);

            // Drain any leftover server->client post-handshake bytes (TLS 1.3 NST)
            // through the client before exchanging app data. See comment above.
            DrainAppDataInto(client, sToC, ref sToCLen);

            byte[] ping = "PING from client"u8.ToArray();
            byte[] pong = "PONG from server"u8.ToArray();

            Assert.Equal(ping, RoundtripRecord(client, server, ping));
            Assert.Equal(pong, RoundtripRecord(server, client, pong));
        }

        private static void DrainAppDataInto(TlsSession session, byte[] cipher, ref int cipherLen)
        {
            byte[] scratch = new byte[CipherBufSize];
            while (cipherLen > 0)
            {
                session.Decrypt(
                    cipher.AsSpan(0, cipherLen), scratch, out int consumed, out _);
                if (consumed == 0)
                {
                    break;
                }
                if (consumed < cipherLen)
                {
                    Buffer.BlockCopy(cipher, consumed, cipher, 0, cipherLen - consumed);
                }
                cipherLen -= consumed;
            }
        }

        // TlsSession driven against a real non-blocking Socket (Socket.Blocking=false).
        // The peer is a plain SslStream over a NetworkStream. This exercises the
        // "give me raw socket bytes, I don't care about your I/O model" contract:
        // TlsSession sees only Send/Receive returning WouldBlock and never blocks
        // on I/O itself.
        [Fact]
        public async Task ServerSession_OnNonBlockingSocket_AgainstSslStreamClient_Succeeds()
        {
            using X509Certificate2 serverCert = TestCertificates.GetServerCertificate();
            string serverName = serverCert.GetNameInfo(X509NameType.SimpleName, forIssuer: false);

            (Socket clientSocket, Socket serverSocket) = await CreateLoopbackSocketPairAsync();
            serverSocket.Blocking = false;

            using (clientSocket)
            using (serverSocket)
            using (NetworkStream clientNetStream = new NetworkStream(clientSocket, ownsSocket: false))
            using (SslStream clientSsl = new SslStream(clientNetStream, leaveInnerStreamOpen: true, TestHelper.AllowAnyServerCertificate))
            {
                using TlsContext ctx = TlsContext.Create(new SslServerAuthenticationOptions
                {
                    ServerCertificate = serverCert,
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                    ClientCertificateRequired = false,
                });
                using TlsSession session = TlsSession.Create(ctx);

                Task clientHandshake = clientSsl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                {
                    TargetHost = serverName,
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                    RemoteCertificateValidationCallback = TestHelper.AllowAnyServerCertificate,
                });

                Task serverHandshake = Task.Run(() => DriveHandshakeNonBlocking(session, serverSocket));

                await Task.WhenAll(clientHandshake, serverHandshake).WaitAsync(TimeSpan.FromSeconds(30));

                Assert.True(session.IsHandshakeComplete);
                Assert.True(clientSsl.IsAuthenticated);

                byte[] ping = "PING over non-blocking socket"u8.ToArray();
                byte[] pong = "PONG over non-blocking socket"u8.ToArray();

                Task clientWrite = clientSsl.WriteAsync(ping).AsTask();
                byte[] got = await Task.Run(() => ReadOnePlaintextNonBlocking(session, serverSocket, ping.Length))
                                       .WaitAsync(TimeSpan.FromSeconds(30));
                await clientWrite;
                Assert.Equal(ping, got);

                await Task.Run(() => WritePlaintextNonBlocking(session, serverSocket, pong))
                          .WaitAsync(TimeSpan.FromSeconds(30));
                byte[] back = new byte[pong.Length];
                int n = 0;
                while (n < back.Length)
                {
                    int r = await clientSsl.ReadAsync(back.AsMemory(n));
                    Assert.True(r > 0);
                    n += r;
                }
                Assert.Equal(pong, back);
            }
        }

        private static async Task<(Socket Client, Socket Server)> CreateLoopbackSocketPairAsync()
        {
            using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 0));
            listener.Listen(1);

            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Task<Socket> acceptTask = listener.AcceptAsync();
            await client.ConnectAsync(listener.LocalEndPoint!);
            Socket server = await acceptTask;
            client.NoDelay = true;
            server.NoDelay = true;
            return (client, server);
        }

        private static void DriveHandshakeNonBlocking(TlsSession session, Socket socket)
        {
            byte[] netIn = new byte[CipherBufSize];
            byte[] netOut = new byte[CipherBufSize];
            int inUsed = 0;

            while (!session.IsHandshakeComplete)
            {
                TlsOperationStatus status = session.ProcessHandshake(
                    netIn.AsSpan(0, inUsed), netOut, out int consumed, out int produced);

                if (consumed > 0)
                {
                    if (consumed < inUsed)
                    {
                        Buffer.BlockCopy(netIn, consumed, netIn, 0, inUsed - consumed);
                    }
                    inUsed -= consumed;
                }

                if (produced > 0)
                {
                    NonBlockingSendAll(socket, netOut, 0, produced);
                }

                switch (status)
                {
                    case TlsOperationStatus.Complete:
                        continue;
                    case TlsOperationStatus.NeedsCertificateValidation:
                        session.AcceptWithDefaultValidation();
                        continue;
                    case TlsOperationStatus.WantWrite:
                        DrainPending(session, socket, netOut);
                        continue;
                    case TlsOperationStatus.WantRead:
                        inUsed += NonBlockingReceiveSome(socket, netIn, inUsed);
                        continue;
                    case TlsOperationStatus.Closed:
                        throw new IOException("Peer closed connection during handshake.");
                }
            }
        }

        private static byte[] ReadOnePlaintextNonBlocking(TlsSession session, Socket socket, int expectedLength)
        {
            byte[] netIn = new byte[CipherBufSize];
            byte[] plain = new byte[CipherBufSize];
            int inUsed = 0;

            while (true)
            {
                TlsOperationStatus status = session.Decrypt(
                    netIn.AsSpan(0, inUsed), plain, out int consumed, out int produced);

                if (consumed > 0)
                {
                    if (consumed < inUsed)
                    {
                        Buffer.BlockCopy(netIn, consumed, netIn, 0, inUsed - consumed);
                    }
                    inUsed -= consumed;
                }

                if (produced > 0)
                {
                    Assert.Equal(expectedLength, produced);
                    return plain.AsSpan(0, produced).ToArray();
                }

                switch (status)
                {
                    case TlsOperationStatus.Complete:
                        continue;
                    case TlsOperationStatus.WantRead:
                        inUsed += NonBlockingReceiveSome(socket, netIn, inUsed);
                        continue;
                    case TlsOperationStatus.WantWrite:
                        DrainPending(session, socket, new byte[CipherBufSize]);
                        continue;
                    case TlsOperationStatus.Closed:
                        throw new IOException("Connection closed while reading plaintext.");
                }
            }
        }

        private static void WritePlaintextNonBlocking(TlsSession session, Socket socket, ReadOnlySpan<byte> data)
        {
            byte[] outBuf = new byte[CipherBufSize];
            int sent = 0;
            while (sent < data.Length)
            {
                TlsOperationStatus status = session.Encrypt(
                    data.Slice(sent), outBuf, out int consumed, out int produced);
                sent += consumed;
                if (produced > 0)
                {
                    NonBlockingSendAll(socket, outBuf, 0, produced);
                }
                if (status == TlsOperationStatus.WantWrite)
                {
                    DrainPending(session, socket, outBuf);
                }
            }
        }

        private static void DrainPending(TlsSession session, Socket socket, byte[] scratch)
        {
            while (session.HasPendingOutput)
            {
                session.DrainPendingOutput(scratch, out int n);
                if (n > 0)
                {
                    NonBlockingSendAll(socket, scratch, 0, n);
                }
            }
        }

        private static void NonBlockingSendAll(Socket socket, byte[] buffer, int offset, int count)
        {
            while (count > 0)
            {
                try
                {
                    int n = socket.Send(buffer, offset, count, SocketFlags.None);
                    offset += n;
                    count -= n;
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.WouldBlock)
                {
                    socket.Poll(-1, SelectMode.SelectWrite);
                }
            }
        }

        private static int NonBlockingReceiveSome(Socket socket, byte[] buffer, int offset)
        {
            while (true)
            {
                try
                {
                    int n = socket.Receive(buffer, offset, buffer.Length - offset, SocketFlags.None);
                    if (n == 0)
                    {
                        throw new IOException("Unexpected EOF.");
                    }
                    return n;
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.WouldBlock)
                {
                    socket.Poll(-1, SelectMode.SelectRead);
                }
            }
        }

        [Fact]
        public async Task ClientSession_AgainstSslStreamServer_HandshakeAndPingPong_Succeeds()
        {
            using X509Certificate2 serverCert = TestCertificates.GetServerCertificate();
            string serverName = serverCert.GetNameInfo(X509NameType.SimpleName, forIssuer: false);

            (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();
            using (clientStream)
            using (serverStream)
            using (SslStream serverSsl = new SslStream(serverStream, leaveInnerStreamOpen: false))
            {
                using TlsContext ctx = TlsContext.Create(new SslClientAuthenticationOptions
                {
                    TargetHost = serverName,
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                    RemoteCertificateValidationCallback = TestHelper.AllowAnyServerCertificate,
                });
                using TlsSession session = TlsSession.Create(ctx);

                Task serverHandshake = serverSsl.AuthenticateAsServerAsync(new SslServerAuthenticationOptions
                {
                    ServerCertificate = serverCert,
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                    ClientCertificateRequired = false,
                });
                Task clientHandshake = DriveHandshakeAsync(session, clientStream);

                await Task.WhenAll(clientHandshake, serverHandshake).WaitAsync(TimeSpan.FromSeconds(30));

                Assert.True(session.IsHandshakeComplete);
                Assert.True(serverSsl.IsAuthenticated);
                Assert.True(session.NegotiatedProtocol is SslProtocols.Tls12 or SslProtocols.Tls13);

                byte[] ping = "PING"u8.ToArray();
                byte[] pong = "PONG"u8.ToArray();

                await WritePlaintextAsync(session, clientStream, ping);
                byte[] gotByServer = new byte[ping.Length];
                int n = 0;
                while (n < gotByServer.Length)
                {
                    int r = await serverSsl.ReadAsync(gotByServer.AsMemory(n));
                    Assert.True(r > 0);
                    n += r;
                }
                Assert.Equal(ping, gotByServer);

                Task serverWrite = serverSsl.WriteAsync(pong).AsTask();
                byte[] gotByClient = await ReadOnePlaintextRecordAsync(session, clientStream, expectedLength: pong.Length);
                await serverWrite;
                Assert.Equal(pong, gotByClient);
            }
        }

        // ── External certificate validation ───────────────────────────────

        [Fact]
        public async Task ClientSession_ExternalCertificateValidation_SuspendsAndAccepts()
        {
            using X509Certificate2 serverCert = TestCertificates.GetServerCertificate();
            string serverName = serverCert.GetNameInfo(X509NameType.SimpleName, forIssuer: false);

            (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();
            using (clientStream)
            using (serverStream)
            using (SslStream serverSsl = new SslStream(serverStream, leaveInnerStreamOpen: false))
            {
                using TlsContext ctx = TlsContext.Create(new SslClientAuthenticationOptions
                {
                    TargetHost = serverName,
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                    // Intentionally no RemoteCertificateValidationCallback — caller drives validation externally.
                });
                using TlsSession session = TlsSession.Create(ctx);

                Task serverHandshake = serverSsl.AuthenticateAsServerAsync(new SslServerAuthenticationOptions
                {
                    ServerCertificate = serverCert,
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                    ClientCertificateRequired = false,
                });

                bool suspensionObserved = false;
                X509Certificate2? observedRemoteCert = null;
                Task clientHandshake = Task.Run(async () =>
                {
                    await DriveHandshakeWithExternalValidationAsync(
                        session, clientStream,
                        onSuspend: () =>
                        {
                            suspensionObserved = true;
                            observedRemoteCert = session.GetRemoteCertificate();
                            session.SetRemoteCertificateValidationResult(SslPolicyErrors.None);
                        });
                });

                await Task.WhenAll(clientHandshake, serverHandshake).WaitAsync(TimeSpan.FromSeconds(30));

                Assert.True(suspensionObserved, "Caller never observed NeedsCertificateValidation.");
                Assert.NotNull(observedRemoteCert);
                Assert.Equal(serverCert.Thumbprint, observedRemoteCert!.Thumbprint);
                Assert.True(session.IsHandshakeComplete);
                Assert.True(serverSsl.IsAuthenticated);

                // After accepting, Encrypt/Decrypt must work normally.
                byte[] ping = "PING external"u8.ToArray();
                await WritePlaintextAsync(session, clientStream, ping);
                byte[] got = new byte[ping.Length];
                int n = 0;
                while (n < got.Length)
                {
                    int r = await serverSsl.ReadAsync(got.AsMemory(n));
                    Assert.True(r > 0);
                    n += r;
                }
                Assert.Equal(ping, got);

                observedRemoteCert?.Dispose();
            }
        }

        [Fact]
        public async Task ClientSession_ExternalCertificateValidation_AcceptWithDefaultValidation_FailsOnUntrustedCert()
        {
            // The test cert chain isn't installed in the system trust store, so the default
            // validation policy must report at least RemoteCertificateChainErrors.
            using X509Certificate2 serverCert = TestCertificates.GetServerCertificate();
            string serverName = serverCert.GetNameInfo(X509NameType.SimpleName, forIssuer: false);

            (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();
            using (clientStream)
            using (serverStream)
            using (SslStream serverSsl = new SslStream(serverStream, leaveInnerStreamOpen: false))
            {
                using TlsContext ctx = TlsContext.Create(new SslClientAuthenticationOptions
                {
                    TargetHost = serverName,
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                });
                using TlsSession session = TlsSession.Create(ctx);

                Task serverHandshake = serverSsl.AuthenticateAsServerAsync(new SslServerAuthenticationOptions
                {
                    ServerCertificate = serverCert,
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                    ClientCertificateRequired = false,
                });

                SslPolicyErrors observedErrors = SslPolicyErrors.None;
                Task clientHandshake = Task.Run(async () =>
                {
                    await DriveHandshakeWithExternalValidationAsync(
                        session, clientStream,
                        onSuspend: () =>
                        {
                            observedErrors = session.AcceptWithDefaultValidation();
                        });
                });

                // The server-side handshake will complete (OpenSSL accepted the cert in the
                // CertVerifyCallback), but the client side rejects post-hoc, so any subsequent
                // app-data exchange throws on the client.
                await serverHandshake.WaitAsync(TimeSpan.FromSeconds(30));
                await clientHandshake.WaitAsync(TimeSpan.FromSeconds(30));

                Assert.NotEqual(SslPolicyErrors.None, observedErrors);

                // Encrypt must now throw because validation reported errors.
                byte[] plain = "should-fail"u8.ToArray();
                byte[] ct = new byte[CipherBufSize];
                Assert.Throws<AuthenticationException>(() =>
                    session.Encrypt(plain, ct, out _, out _));
            }
        }

        [Fact]
        public async Task ClientSession_ExternalValidation_SetResultWithErrors_FaultsSession()
        {
            // Standalone-mode regression: when the caller explicitly rejects via
            // SetRemoteCertificateValidationResult with non-None errors, subsequent
            // session operations (Encrypt and Decrypt) must throw AuthenticationException,
            // regardless of whether the underlying chain itself would have validated.
            using X509Certificate2 serverCert = TestCertificates.GetServerCertificate();
            string serverName = serverCert.GetNameInfo(X509NameType.SimpleName, forIssuer: false);

            (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();
            using (clientStream)
            using (serverStream)
            using (SslStream serverSsl = new SslStream(serverStream, leaveInnerStreamOpen: false))
            {
                using TlsContext ctx = TlsContext.Create(new SslClientAuthenticationOptions
                {
                    TargetHost = serverName,
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                });
                using TlsSession session = TlsSession.Create(ctx);

                Task serverHandshake = serverSsl.AuthenticateAsServerAsync(new SslServerAuthenticationOptions
                {
                    ServerCertificate = serverCert,
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                    ClientCertificateRequired = false,
                });

                Task clientHandshake = Task.Run(async () =>
                {
                    await DriveHandshakeWithExternalValidationAsync(
                        session, clientStream,
                        onSuspend: () =>
                        {
                            session.SetRemoteCertificateValidationResult(SslPolicyErrors.RemoteCertificateNameMismatch);
                        });
                });

                await serverHandshake.WaitAsync(TimeSpan.FromSeconds(30));
                await clientHandshake.WaitAsync(TimeSpan.FromSeconds(30));

                byte[] plain = "rejected"u8.ToArray();
                byte[] ct = new byte[CipherBufSize];
                Assert.Throws<AuthenticationException>(() =>
                    session.Encrypt(plain, ct, out _, out _));

                byte[] pt = new byte[CipherBufSize];
                Assert.Throws<AuthenticationException>(() =>
                    session.Decrypt(ct, pt, out _, out _));
            }
        }

        [Fact]
        public async Task ClientSession_ExternalValidation_CallbackRejectsCleanChain_FaultsSession()
        {
            // Regression: a user RemoteCertificateValidationCallback can reject by returning false
            // even when sslPolicyErrors == None. AcceptWithDefaultValidation must treat that as a
            // rejection, not accept the connection.
            using X509Certificate2 serverCert = TestCertificates.GetServerCertificate();
            string serverName = serverCert.GetNameInfo(X509NameType.SimpleName, forIssuer: false);

            (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();
            using (clientStream)
            using (serverStream)
            using (SslStream serverSsl = new SslStream(serverStream, leaveInnerStreamOpen: false))
            {
                using TlsContext ctx = TlsContext.Create(new SslClientAuthenticationOptions
                {
                    TargetHost = serverName,
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                    // Callback overrides default chain validation, returns false unconditionally,
                    // and reports no policy errors. The session must still reject.
                    RemoteCertificateValidationCallback = (_, _, _, _) => false,
                });
                using TlsSession session = TlsSession.Create(ctx);

                Task serverHandshake = serverSsl.AuthenticateAsServerAsync(new SslServerAuthenticationOptions
                {
                    ServerCertificate = serverCert,
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                    ClientCertificateRequired = false,
                });

                SslPolicyErrors observedErrors = SslPolicyErrors.RemoteCertificateNotAvailable;
                Task clientHandshake = Task.Run(async () =>
                {
                    await DriveHandshakeWithExternalValidationAsync(
                        session, clientStream,
                        onSuspend: () =>
                        {
                            observedErrors = session.AcceptWithDefaultValidation();
                        });
                });

                await serverHandshake.WaitAsync(TimeSpan.FromSeconds(30));
                await clientHandshake.WaitAsync(TimeSpan.FromSeconds(30));

                // AcceptWithDefaultValidation returns the original sslPolicyErrors (None here, since
                // the test cert happens to validate against the trust store on some hosts), but the
                // false return from the user callback must still fault the session.
                Assert.Throws<AuthenticationException>(() =>
                    session.Encrypt("x"u8.ToArray(), new byte[CipherBufSize], out _, out _));

                _ = observedErrors;
            }
        }

        [Fact]
        public void TlsSession_ExternalValidation_SetResultBeforeSuspended_Throws()
        {
            using X509Certificate2 serverCert = TestCertificates.GetServerCertificate();
            using TlsContext ctx = TlsContext.Create(new SslServerAuthenticationOptions { ServerCertificate = serverCert });
            using TlsSession session = TlsSession.Create(ctx);

            Assert.Throws<InvalidOperationException>(() =>
                session.SetRemoteCertificateValidationResult(SslPolicyErrors.None));
            Assert.Throws<InvalidOperationException>(() =>
                session.AcceptWithDefaultValidation());
        }

        // After a handshake-time AuthenticationException, flush any TLS alert bytes the PAL
        // queued in the session's pending buffer to the wire so the peer observes a fatal
        // alert instead of timing out / seeing handshake-completed.
        private static async Task DrainAfterAuthFaultAsync(TlsSession session, Stream transport)
        {
            byte[] scratch = ArrayPool<byte>.Shared.Rent(CipherBufSize);
            try
            {
                while (session.HasPendingOutput)
                {
                    session.DrainPendingOutput(scratch, out int n);
                    if (n > 0)
                    {
                        await transport.WriteAsync(scratch.AsMemory(0, n));
                        await transport.FlushAsync();
                    }
                }
            }
            catch
            {
                // Best-effort: the peer may have already torn down the connection.
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(scratch);
            }
        }

        // Like DriveHandshakeAsync but pauses on NeedsCertificateValidation to invoke the supplied
        // callback. The suspension is reported post-hoc: IsHandshakeComplete is already true on the
        // TLS state machine (TLS records have been exchanged), but Encrypt/Decrypt block until the
        // caller posts a verdict via SetRemoteCertificateValidationResult / AcceptWithDefaultValidation.
        private static async Task DriveHandshakeWithExternalValidationAsync(
            TlsSession session, Stream transport, Action onSuspend)
        {
            byte[] netIn = ArrayPool<byte>.Shared.Rent(CipherBufSize);
            byte[] netOut = ArrayPool<byte>.Shared.Rent(CipherBufSize);
            int inUsed = 0;

            try
            {
                while (!session.IsHandshakeComplete)
                {
                    TlsOperationStatus status;
                    int consumed;
                    int produced;
                    try
                    {
                        status = session.ProcessHandshake(
                            netIn.AsSpan(0, inUsed),
                            netOut,
                            out consumed,
                            out produced);
                    }
                    catch (AuthenticationException)
                    {
                        // External validator rejected the peer certificate; the session is
                        // permanently faulted. Treat this as a terminal handshake state so the
                        // caller can assert on the resulting Encrypt/Decrypt behavior.
                        return;
                    }

                    if (consumed > 0)
                    {
                        if (consumed < inUsed)
                        {
                            Buffer.BlockCopy(netIn, consumed, netIn, 0, inUsed - consumed);
                        }
                        inUsed -= consumed;
                    }

                    if (produced > 0)
                    {
                        await transport.WriteAsync(netOut.AsMemory(0, produced));
                        await transport.FlushAsync();
                    }

                    switch (status)
                    {
                        case TlsOperationStatus.NeedsCertificateValidation:
                            onSuspend();
                            continue;

                        case TlsOperationStatus.Complete:
                            continue;

                        case TlsOperationStatus.WantWrite:
                            await DrainAsync(session, transport, netOut);
                            continue;

                        case TlsOperationStatus.WantRead:
                            int r = await transport.ReadAsync(netIn.AsMemory(inUsed));
                            if (r == 0)
                            {
                                throw new IOException("Unexpected EOF during handshake.");
                            }
                            inUsed += r;
                            continue;

                        case TlsOperationStatus.Closed:
                            throw new IOException("Peer closed connection during handshake.");
                    }
                }

                // Flush anything still pending (e.g. server-emitted NewSessionTickets in TLS 1.3
                // that arrived after the local handshake reached completion).
                while (session.HasPendingOutput)
                {
                    TlsOperationStatus drain = session.DrainPendingOutput(netOut, out int n);
                    if (n > 0)
                    {
                        await transport.WriteAsync(netOut.AsMemory(0, n));
                        await transport.FlushAsync();
                    }
                    if (drain != TlsOperationStatus.WantWrite)
                    {
                        break;
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(netIn);
                ArrayPool<byte>.Shared.Return(netOut);
            }
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private static void StepHandshakeInMemory(
            TlsSession session, byte[] input, ref int inputLen, byte[] output, ref int outputLen)
        {
            if (session.IsHandshakeComplete)
            {
                return;
            }

            TlsOperationStatus status = session.ProcessHandshake(
                input.AsSpan(0, inputLen),
                output.AsSpan(outputLen),
                out int consumed,
                out int produced);

            if (consumed > 0)
            {
                if (consumed < inputLen)
                {
                    Buffer.BlockCopy(input, consumed, input, 0, inputLen - consumed);
                }
                inputLen -= consumed;
            }
            outputLen += produced;

            while (session.HasPendingOutput)
            {
                session.DrainPendingOutput(output.AsSpan(outputLen), out int n);
                outputLen += n;
            }

            if (status == TlsOperationStatus.NeedsCertificateValidation)
            {
                // In-memory helper: defer to the default validation path (which honors
                // any RemoteCertificateValidationCallback on the underlying options).
                session.AcceptWithDefaultValidation();
            }

            Assert.NotEqual(TlsOperationStatus.Closed, status);
        }

        private static byte[] RoundtripRecord(TlsSession sender, TlsSession receiver, byte[] plaintext)
        {
            byte[] ct = new byte[CipherBufSize];
            int ctLen = 0;
            int sent = 0;
            while (sent < plaintext.Length)
            {
                sender.Encrypt(
                    plaintext.AsSpan(sent),
                    ct.AsSpan(ctLen),
                    out int consumed,
                    out int produced);
                sent += consumed;
                ctLen += produced;
                while (sender.HasPendingOutput)
                {
                    sender.DrainPendingOutput(ct.AsSpan(ctLen), out int n);
                    ctLen += n;
                }
            }

            byte[] pt = new byte[CipherBufSize];
            int ptLen = 0;
            int ctOff = 0;
            while (ctOff < ctLen)
            {
                receiver.Decrypt(
                    ct.AsSpan(ctOff, ctLen - ctOff),
                    pt.AsSpan(ptLen),
                    out int consumed,
                    out int produced);
                if (consumed == 0 && produced == 0)
                {
                    break;
                }
                ctOff += consumed;
                ptLen += produced;
            }
            return pt.AsSpan(0, ptLen).ToArray();
        }

        [Fact]
        public async Task ServerSession_ApplicationProtocols_NegotiatesAlpn()
        {
            using X509Certificate2 serverCert = TestCertificates.GetServerCertificate();
            string serverName = serverCert.GetNameInfo(X509NameType.SimpleName, forIssuer: false);

            (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();
            using (clientStream)
            using (serverStream)
            using (SslStream clientSsl = new SslStream(clientStream, leaveInnerStreamOpen: false, TestHelper.AllowAnyServerCertificate))
            {
                using TlsContext ctx = TlsContext.Create(new SslServerAuthenticationOptions
                {
                    ServerCertificate = serverCert,
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                    ApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http2, SslApplicationProtocol.Http11 },
                });
                using TlsSession session = TlsSession.Create(ctx);

                Task clientHandshake = clientSsl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                {
                    TargetHost = serverName,
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                    RemoteCertificateValidationCallback = TestHelper.AllowAnyServerCertificate,
                    ApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http11, SslApplicationProtocol.Http2 },
                });
                Task serverHandshake = DriveHandshakeAsync(session, serverStream);
                await Task.WhenAll(clientHandshake, serverHandshake).WaitAsync(TimeSpan.FromSeconds(30));

                Assert.True(session.IsHandshakeComplete);
                Assert.Equal(SslApplicationProtocol.Http2, session.NegotiatedApplicationProtocol);
                Assert.Equal(SslApplicationProtocol.Http2, clientSsl.NegotiatedApplicationProtocol);
            }
        }

        [Fact]
        public async Task ServerSession_ServerCertificateSelectionCallback_InvokedWithSni()
        {
            using X509Certificate2 serverCert = TestCertificates.GetServerCertificate();
            string serverName = serverCert.GetNameInfo(X509NameType.SimpleName, forIssuer: false);

            string? observedSni = null;
            int callbackCount = 0;

            (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();
            using (clientStream)
            using (serverStream)
            using (SslStream clientSsl = new SslStream(clientStream, leaveInnerStreamOpen: false, TestHelper.AllowAnyServerCertificate))
            {
                using TlsContext ctx = TlsContext.Create(new SslServerAuthenticationOptions
                {
                    ServerCertificateSelectionCallback = (sender, hostName) =>
                    {
                        callbackCount++;
                        observedSni = hostName;
                        Assert.IsType<TlsSession>(sender);
                        return serverCert;
                    },
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                });
                using TlsSession session = TlsSession.Create(ctx);

                Task clientHandshake = clientSsl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                {
                    TargetHost = serverName,
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                    RemoteCertificateValidationCallback = TestHelper.AllowAnyServerCertificate,
                });
                Task serverHandshake = DriveHandshakeAsync(session, serverStream);
                await Task.WhenAll(clientHandshake, serverHandshake).WaitAsync(TimeSpan.FromSeconds(30));

                Assert.True(session.IsHandshakeComplete);
                Assert.Equal(1, callbackCount);
                Assert.Equal(serverName, observedSni);
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.OSX, "SecureTransport does not support post-handshake renegotiation.")]
        public async Task ServerSession_RequestClientCertificate_Tls12_ProducesHandshakeBytes()
        {
            using X509Certificate2 serverCert = TestCertificates.GetServerCertificate();
            string serverName = serverCert.GetNameInfo(X509NameType.SimpleName, forIssuer: false);

            (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();
            using (clientStream)
            using (serverStream)
            using (SslStream clientSsl = new SslStream(clientStream, leaveInnerStreamOpen: false, TestHelper.AllowAnyServerCertificate))
            {
                using TlsContext ctx = TlsContext.Create(new SslServerAuthenticationOptions
                {
                    ServerCertificate = serverCert,
                    EnabledSslProtocols = SslProtocols.Tls12,
                    AllowRenegotiation = true,
                });
                using TlsSession session = TlsSession.Create(ctx);

                Task clientHandshake = clientSsl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                {
                    TargetHost = serverName,
                    EnabledSslProtocols = SslProtocols.Tls12,
                    RemoteCertificateValidationCallback = TestHelper.AllowAnyServerCertificate,
                    AllowRenegotiation = true,
                });
                Task serverHandshake = DriveHandshakeAsync(session, serverStream);
                await Task.WhenAll(clientHandshake, serverHandshake).WaitAsync(TimeSpan.FromSeconds(30));

                Assert.True(session.IsHandshakeComplete);
                Assert.Equal(SslProtocols.Tls12, session.NegotiatedProtocol);

                // On TLS 1.2, post-handshake client-cert request is implemented
                // as a renegotiation initiated by a HelloRequest. We only verify
                // that the API runs and emits handshake bytes; driving the
                // exchange back through SslStream is out of scope because the
                // standalone TlsSession leaves the post-handshake read loop to
                // the caller.
                byte[] reneg = ArrayPool<byte>.Shared.Rent(CipherBufSize);
                try
                {
                    TlsOperationStatus status = session.RequestClientCertificate(reneg, out int produced);
                    Assert.NotEqual(TlsOperationStatus.Closed, status);
                    Assert.True(produced > 0, "RequestClientCertificate should emit a HelloRequest.");
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(reneg);
                }
            }
        }

        private static Task DriveHandshakeAsync(TlsSession session, Stream transport)
            => DriveHandshakeAsync(session, transport, serverContextFactory: null);

        private static async Task DriveHandshakeAsync(
            TlsSession session,
            Stream transport,
            Func<SslClientHelloInfo, TlsContext>? serverContextFactory)
        {
            byte[] netIn = ArrayPool<byte>.Shared.Rent(CipherBufSize);
            byte[] netOut = ArrayPool<byte>.Shared.Rent(CipherBufSize);
            int inUsed = 0;

            try
            {
                while (!session.IsHandshakeComplete)
                {
                    TlsOperationStatus status = session.ProcessHandshake(
                        netIn.AsSpan(0, inUsed),
                        netOut,
                        out int consumed,
                        out int produced);

                    if (consumed > 0)
                    {
                        if (consumed < inUsed)
                        {
                            Buffer.BlockCopy(netIn, consumed, netIn, 0, inUsed - consumed);
                        }
                        inUsed -= consumed;
                    }

                    if (produced > 0)
                    {
                        await transport.WriteAsync(netOut.AsMemory(0, produced));
                        await transport.FlushAsync();
                    }

                    switch (status)
                    {
                        case TlsOperationStatus.Complete:
                            continue;

                        case TlsOperationStatus.NeedsServerOptions:
                            if (serverContextFactory is null)
                            {
                                throw new InvalidOperationException(
                                    "Handshake suspended on NeedsServerOptions but no factory was supplied.");
                            }
                            session.SetServerContext(serverContextFactory(session.ClientHelloInfo!.Value));
                            continue;

                        case TlsOperationStatus.NeedsCertificateValidation:
                            session.AcceptWithDefaultValidation();
                            continue;

                        case TlsOperationStatus.WantWrite:
                            await DrainAsync(session, transport, netOut);
                            continue;

                        case TlsOperationStatus.WantRead:
                            int r = await transport.ReadAsync(netIn.AsMemory(inUsed));
                            if (r == 0)
                            {
                                throw new IOException("Unexpected EOF during handshake.");
                            }
                            inUsed += r;
                            continue;

                        case TlsOperationStatus.Closed:
                            throw new IOException("Peer closed connection during handshake.");
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(netIn);
                ArrayPool<byte>.Shared.Return(netOut);
            }
        }

        private static async Task<byte[]> ReadOnePlaintextRecordAsync(
            TlsSession session, Stream transport, int expectedLength)
        {
            byte[] netIn = ArrayPool<byte>.Shared.Rent(CipherBufSize);
            byte[] plain = ArrayPool<byte>.Shared.Rent(CipherBufSize);
            int inUsed = 0;

            try
            {
                while (true)
                {
                    TlsOperationStatus status = session.Decrypt(
                        netIn.AsSpan(0, inUsed),
                        plain,
                        out int consumed,
                        out int produced);

                    if (consumed > 0)
                    {
                        if (consumed < inUsed)
                        {
                            Buffer.BlockCopy(netIn, consumed, netIn, 0, inUsed - consumed);
                        }
                        inUsed -= consumed;
                    }

                    if (produced > 0)
                    {
                        Assert.Equal(expectedLength, produced);
                        byte[] result = plain.AsSpan(0, produced).ToArray();
                        return result;
                    }

                    switch (status)
                    {
                        case TlsOperationStatus.Complete:
                            continue;

                        case TlsOperationStatus.WantRead:
                            int r = await transport.ReadAsync(netIn.AsMemory(inUsed));
                            if (r == 0)
                            {
                                throw new IOException("Unexpected EOF while reading plaintext.");
                            }
                            inUsed += r;
                            continue;

                        case TlsOperationStatus.WantWrite:
                            byte[] outBuf = ArrayPool<byte>.Shared.Rent(CipherBufSize);
                            try { await DrainAsync(session, transport, outBuf); }
                            finally { ArrayPool<byte>.Shared.Return(outBuf); }
                            continue;

                        case TlsOperationStatus.Closed:
                            throw new IOException("Connection closed while reading plaintext.");
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(netIn);
                ArrayPool<byte>.Shared.Return(plain);
            }
        }

        private static async Task WritePlaintextAsync(TlsSession session, Stream transport, ReadOnlyMemory<byte> data)
        {
            byte[] outBuf = ArrayPool<byte>.Shared.Rent(CipherBufSize);
            try
            {
                int sent = 0;
                while (sent < data.Length)
                {
                    TlsOperationStatus status = session.Encrypt(
                        data.Span[sent..],
                        outBuf,
                        out int consumed,
                        out int produced);

                    sent += consumed;

                    if (produced > 0)
                    {
                        await transport.WriteAsync(outBuf.AsMemory(0, produced));
                        await transport.FlushAsync();
                    }

                    if (status == TlsOperationStatus.WantWrite)
                    {
                        await DrainAsync(session, transport, outBuf);
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(outBuf);
            }
        }

        private static async Task DrainAsync(TlsSession session, Stream transport, byte[] scratch)
        {
            while (session.HasPendingOutput)
            {
                TlsOperationStatus s = session.DrainPendingOutput(scratch, out int n);
                if (n > 0)
                {
                    await transport.WriteAsync(scratch.AsMemory(0, n));
                    await transport.FlushAsync();
                }
                if (s != TlsOperationStatus.WantWrite)
                {
                    break;
                }
            }
        }

        [Fact]
        public async Task SocketBoundSession_HandshakeAndPingPong_Succeeds()
        {
            using X509Certificate2 serverCert = TestCertificates.GetServerCertificate();
            string serverName = serverCert.GetNameInfo(X509NameType.SimpleName, forIssuer: false);

            using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(1);
            int port = ((IPEndPoint)listener.LocalEndPoint!).Port;

            using Socket clientUnderlying = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Task connect = clientUnderlying.ConnectAsync(IPAddress.Loopback, port);
            Socket serverSocket = await listener.AcceptAsync();
            await connect;

            // Configure as non-blocking; TlsSession contract requires it.
            serverSocket.Blocking = false;

            // Hand the raw handle to TlsSession; it takes ownership.
            SafeSocketHandle serverHandle = serverSocket.SafeHandle;

            using TlsContext ctx = TlsContext.Create(new SslServerAuthenticationOptions
            {
                ServerCertificate = serverCert,
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                ClientCertificateRequired = false,
            });
            using TlsSession session = TlsSession.Create(ctx, serverHandle);
            Assert.Same(serverHandle, session.Socket);

            using SslStream clientSsl = new SslStream(new NetworkStream(clientUnderlying, ownsSocket: false), leaveInnerStreamOpen: false, TestHelper.AllowAnyServerCertificate);
            Task clientHandshake = clientSsl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
            {
                TargetHost = serverName,
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                RemoteCertificateValidationCallback = TestHelper.AllowAnyServerCertificate,
            });

            Task serverHandshake = Task.Run(async () =>
            {
                while (true)
                {
                    TlsOperationStatus s = session.Handshake();
                    if (s == TlsOperationStatus.Complete)
                    {
                        return;
                    }
                    if (s == TlsOperationStatus.NeedsCertificateValidation)
                    {
                        session.AcceptWithDefaultValidation();
                        continue;
                    }
                    if (s == TlsOperationStatus.WantRead || s == TlsOperationStatus.WantWrite)
                    {
                        // Simple poll-based scheduler; tests run on loopback so this is cheap.
                        await Task.Delay(5);
                        continue;
                    }
                    throw new InvalidOperationException($"Unexpected handshake status: {s}");
                }
            });

            await Task.WhenAll(clientHandshake, serverHandshake).WaitAsync(TimeSpan.FromSeconds(30));
            Assert.True(session.IsHandshakeComplete);

            // Client → server ping
            byte[] ping = "PING"u8.ToArray();
            Task clientWrite = clientSsl.WriteAsync(ping).AsTask();
            byte[] received = new byte[ping.Length];
            int got = 0;
            while (got < received.Length)
            {
                TlsOperationStatus rs = session.Read(received.AsSpan(got), out int n);
                if (n > 0)
                {
                    got += n;
                    continue;
                }
                if (rs == TlsOperationStatus.WantRead)
                {
                    await Task.Delay(5);
                    continue;
                }
                Assert.Fail($"Unexpected read status: {rs}");
            }
            await clientWrite;
            Assert.Equal(ping, received);

            // Server → client pong
            byte[] pong = "PONG"u8.ToArray();
            int sent = 0;
            while (sent < pong.Length)
            {
                TlsOperationStatus ws = session.Write(pong.AsSpan(sent), out int n);
                sent += n;
                if (ws == TlsOperationStatus.Complete)
                {
                    continue;
                }
                if (ws == TlsOperationStatus.WantWrite)
                {
                    await Task.Delay(5);
                    continue;
                }
                Assert.Fail($"Unexpected write status: {ws}");
            }

            byte[] back = new byte[pong.Length];
            int r = 0;
            while (r < back.Length)
            {
                int n = await clientSsl.ReadAsync(back.AsMemory(r));
                Assert.True(n > 0);
                r += n;
            }
            Assert.Equal(pong, back);

            // Cleanup: session owns serverHandle and will close it on dispose.
        }

        // Socket-bound server session with deferred options. The handshake loop must
        // suspend on NeedsServerOptions, expose ClientHelloInfo (SNI), and continue
        // after SetServerContext. On the OpenSSL socket-bound fast path, the managed
        // pre-fetch loop peels the ClientHello off the socket to surface SNI, then
        // SetServerContext activates fd-mode with a socket-replay BIO that hands
        // those bytes to OpenSSL before the fd is consulted again.
        [Fact]
        public async Task SocketBoundSession_DeferredOptions_SelectedFromSni_Succeeds()
        {
            using X509Certificate2 serverCert = TestCertificates.GetServerCertificate();
            string serverName = serverCert.GetNameInfo(X509NameType.SimpleName, forIssuer: false);

            using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(1);
            int port = ((IPEndPoint)listener.LocalEndPoint!).Port;

            using Socket clientUnderlying = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Task connect = clientUnderlying.ConnectAsync(IPAddress.Loopback, port);
            Socket serverSocket = await listener.AcceptAsync();
            await connect;

            serverSocket.Blocking = false;
            SafeSocketHandle serverHandle = serverSocket.SafeHandle;

            int factoryCalls = 0;
            string? observedSni = null;

            using TlsContext ctx = TlsContext.Create((SslServerAuthenticationOptions?)null);
            using TlsContext hostCtx = TlsContext.Create(new SslServerAuthenticationOptions
            {
                ServerCertificate = serverCert,
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
            });
            using TlsSession session = TlsSession.Create(ctx, serverHandle);

            using SslStream clientSsl = new SslStream(new NetworkStream(clientUnderlying, ownsSocket: false), leaveInnerStreamOpen: false, TestHelper.AllowAnyServerCertificate);
            Task clientHandshake = clientSsl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
            {
                TargetHost = serverName,
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                RemoteCertificateValidationCallback = TestHelper.AllowAnyServerCertificate,
            });

            Task serverHandshake = Task.Run(async () =>
            {
                while (true)
                {
                    TlsOperationStatus s = session.Handshake();
                    if (s == TlsOperationStatus.Complete)
                    {
                        return;
                    }
                    if (s == TlsOperationStatus.NeedsServerOptions)
                    {
                        factoryCalls++;
                        observedSni = session.ClientHelloInfo!.Value.ServerName;
                        session.SetServerContext(hostCtx);
                        continue;
                    }
                    if (s == TlsOperationStatus.NeedsCertificateValidation)
                    {
                        session.AcceptWithDefaultValidation();
                        continue;
                    }
                    if (s == TlsOperationStatus.WantRead || s == TlsOperationStatus.WantWrite)
                    {
                        await Task.Delay(5);
                        continue;
                    }
                    throw new InvalidOperationException($"Unexpected handshake status: {s}");
                }
            });

            await Task.WhenAll(clientHandshake, serverHandshake).WaitAsync(TimeSpan.FromSeconds(30));
            Assert.True(session.IsHandshakeComplete);
            Assert.Equal(1, factoryCalls);
            Assert.Equal(serverName, observedSni);
        }

        // Two consecutive sessions on the same TlsContext, each carrying a different SNI.
        // Verifies the deferred-options factory returns the right cert per connection and
        // that the OpenSSL socket-bound fast path picks up the freshly-set cert per session
        // (i.e. no cert leaks from a stale/preallocated shared SSL_CTX).
        [Fact]
        public async Task SocketBoundSession_DeferredOptions_MultipleSni_SelectsMatchingCert()
        {
            using X509Certificate2 certA = TestCertificates.GetServerCertificate();
            using X509Certificate2 certB = CreateSelfSignedServerCert("tls-session-vhost-b.example");
            string nameA = certA.GetNameInfo(X509NameType.SimpleName, forIssuer: false);
            const string nameB = "tls-session-vhost-b.example";

            using TlsContext ctx = TlsContext.Create((SslServerAuthenticationOptions?)null);
            using TlsContext hostCtxA = TlsContext.Create(new SslServerAuthenticationOptions
            {
                ServerCertificate = certA,
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
            });
            using TlsContext hostCtxB = TlsContext.Create(new SslServerAuthenticationOptions
            {
                ServerCertificate = certB,
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
            });

            for (int iter = 0; iter < 2; iter++)
            {
                string requestedSni = iter == 0 ? nameA : nameB;
                X509Certificate2 expectedCert = iter == 0 ? certA : certB;
                string? sniSeenByServer = null;
                X509Certificate2? certSeenByClient = null;

                using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(1);
                int port = ((IPEndPoint)listener.LocalEndPoint!).Port;

                using Socket clientUnderlying = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                Task connect = clientUnderlying.ConnectAsync(IPAddress.Loopback, port);
                Socket serverSocket = await listener.AcceptAsync();
                await connect;

                serverSocket.Blocking = false;
                SafeSocketHandle serverHandle = serverSocket.SafeHandle;

                using TlsSession session = TlsSession.Create(ctx, serverHandle);

                using SslStream clientSsl = new SslStream(new NetworkStream(clientUnderlying, ownsSocket: false), leaveInnerStreamOpen: false);

                Task clientHandshake = clientSsl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                {
                    TargetHost = requestedSni,
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                    RemoteCertificateValidationCallback = (_, cert, _, _) =>
                    {
                        // Capture the raw cert bytes: the X509Certificate handed to the callback
                        // is short-lived and gets disposed with SslStream.
                        if (cert is not null)
                        {
                            certSeenByClient = X509CertificateLoader.LoadCertificate(cert.Export(X509ContentType.Cert));
                        }
                        return true;
                    },
                });

                Task serverHandshake = Task.Run(async () =>
                {
                    while (true)
                    {
                        TlsOperationStatus s = session.Handshake();
                        if (s == TlsOperationStatus.Complete)
                        {
                            return;
                        }
                        if (s == TlsOperationStatus.NeedsServerOptions)
                        {
                            sniSeenByServer = session.ClientHelloInfo!.Value.ServerName;
                            TlsContext pickCtx = string.Equals(sniSeenByServer, nameB, StringComparison.OrdinalIgnoreCase) ? hostCtxB : hostCtxA;
                            session.SetServerContext(pickCtx);
                            continue;
                        }
                        if (s == TlsOperationStatus.NeedsCertificateValidation)
                        {
                            session.AcceptWithDefaultValidation();
                            continue;
                        }
                        if (s == TlsOperationStatus.WantRead || s == TlsOperationStatus.WantWrite)
                        {
                            await Task.Delay(5);
                            continue;
                        }
                        throw new InvalidOperationException($"Unexpected handshake status: {s}");
                    }
                });

                await Task.WhenAll(clientHandshake, serverHandshake).WaitAsync(TimeSpan.FromSeconds(30));

                Assert.True(session.IsHandshakeComplete);
                Assert.Equal(requestedSni, sniSeenByServer);
                Assert.NotNull(certSeenByClient);
                Assert.Equal(expectedCert.Thumbprint, certSeenByClient!.Thumbprint);
                certSeenByClient.Dispose();
            }
        }

        // ApplicationProtocols supplied in the deferred SetServerContext call must survive
        // the handoff onto the fd-mode replay-BIO path and be honored by OpenSSL's ALPN
        // selection callback.
        [Fact]
        public async Task SocketBoundSession_DeferredOptions_WithAlpn_NegotiatesProtocol()
        {
            using X509Certificate2 serverCert = TestCertificates.GetServerCertificate();
            string serverName = serverCert.GetNameInfo(X509NameType.SimpleName, forIssuer: false);

            using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(1);
            int port = ((IPEndPoint)listener.LocalEndPoint!).Port;

            using Socket clientUnderlying = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Task connect = clientUnderlying.ConnectAsync(IPAddress.Loopback, port);
            Socket serverSocket = await listener.AcceptAsync();
            await connect;

            serverSocket.Blocking = false;
            SafeSocketHandle serverHandle = serverSocket.SafeHandle;

            using TlsContext ctx = TlsContext.Create((SslServerAuthenticationOptions?)null);
            using TlsContext hostCtx = TlsContext.Create(new SslServerAuthenticationOptions
            {
                ServerCertificate = serverCert,
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                ApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http2 },
            });
            using TlsSession session = TlsSession.Create(ctx, serverHandle);

            using SslStream clientSsl = new SslStream(new NetworkStream(clientUnderlying, ownsSocket: false), leaveInnerStreamOpen: false, TestHelper.AllowAnyServerCertificate);
            Task clientHandshake = clientSsl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
            {
                TargetHost = serverName,
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                ApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http2, SslApplicationProtocol.Http11 },
                RemoteCertificateValidationCallback = TestHelper.AllowAnyServerCertificate,
            });

            Task serverHandshake = Task.Run(async () =>
            {
                while (true)
                {
                    TlsOperationStatus s = session.Handshake();
                    if (s == TlsOperationStatus.Complete)
                    {
                        return;
                    }
                    if (s == TlsOperationStatus.NeedsServerOptions)
                    {
                        session.SetServerContext(hostCtx);
                        continue;
                    }
                    if (s == TlsOperationStatus.NeedsCertificateValidation)
                    {
                        session.AcceptWithDefaultValidation();
                        continue;
                    }
                    if (s == TlsOperationStatus.WantRead || s == TlsOperationStatus.WantWrite)
                    {
                        await Task.Delay(5);
                        continue;
                    }
                    throw new InvalidOperationException($"Unexpected handshake status: {s}");
                }
            });

            await Task.WhenAll(clientHandshake, serverHandshake).WaitAsync(TimeSpan.FromSeconds(30));
            Assert.True(session.IsHandshakeComplete);
            Assert.Equal(SslApplicationProtocol.Http2, clientSsl.NegotiatedApplicationProtocol);
        }

        // Deferred SetServerContext with an EnabledSslProtocols set that has no overlap
        // with the client's ClientHello must fail the handshake cleanly (no crash, no hang)
        // via the socket-replay BIO path.
        [Fact]
        public async Task SocketBoundSession_DeferredOptions_ProtocolMismatch_Fails()
        {
            using X509Certificate2 serverCert = TestCertificates.GetServerCertificate();
            string serverName = serverCert.GetNameInfo(X509NameType.SimpleName, forIssuer: false);

            using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(1);
            int port = ((IPEndPoint)listener.LocalEndPoint!).Port;

            using Socket clientUnderlying = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Task connect = clientUnderlying.ConnectAsync(IPAddress.Loopback, port);
            Socket serverSocket = await listener.AcceptAsync();
            await connect;

            serverSocket.Blocking = false;
            SafeSocketHandle serverHandle = serverSocket.SafeHandle;

            using TlsContext ctx = TlsContext.Create((SslServerAuthenticationOptions?)null);
            using TlsContext hostCtx = TlsContext.Create(new SslServerAuthenticationOptions
            {
                ServerCertificate = serverCert,
                EnabledSslProtocols = SslProtocols.Tls13,
            });
            using TlsSession session = TlsSession.Create(ctx, serverHandle);

            using SslStream clientSsl = new SslStream(new NetworkStream(clientUnderlying, ownsSocket: false), leaveInnerStreamOpen: false, TestHelper.AllowAnyServerCertificate);
            Task clientHandshake = clientSsl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
            {
                TargetHost = serverName,
                EnabledSslProtocols = SslProtocols.Tls12,
                RemoteCertificateValidationCallback = TestHelper.AllowAnyServerCertificate,
            });

            Task serverHandshake = Task.Run(async () =>
            {
                while (true)
                {
                    TlsOperationStatus s = session.Handshake();
                    if (s == TlsOperationStatus.Complete)
                    {
                        return;
                    }
                    if (s == TlsOperationStatus.NeedsServerOptions)
                    {
                        session.SetServerContext(hostCtx);
                        continue;
                    }
                    if (s == TlsOperationStatus.NeedsCertificateValidation)
                    {
                        session.AcceptWithDefaultValidation();
                        continue;
                    }
                    if (s == TlsOperationStatus.WantRead || s == TlsOperationStatus.WantWrite)
                    {
                        await Task.Delay(5);
                        continue;
                    }
                    throw new InvalidOperationException($"Unexpected handshake status: {s}");
                }
            });

            await Assert.ThrowsAnyAsync<AuthenticationException>(() => serverHandshake).WaitAsync(TimeSpan.FromSeconds(30));

            // Client side may fail with either AuthenticationException or IOException depending
            // on how quickly the server-side alert lands; either is acceptable.
            await Assert.ThrowsAnyAsync<Exception>(() => clientHandshake).WaitAsync(TimeSpan.FromSeconds(30));
            Assert.False(session.IsHandshakeComplete);
        }

        private static X509Certificate2 CreateSelfSignedServerCert(string commonName)
        {
            using System.Security.Cryptography.RSA rsa = System.Security.Cryptography.RSA.Create(2048);
            var req = new CertificateRequest($"CN={commonName}", rsa, System.Security.Cryptography.HashAlgorithmName.SHA256, System.Security.Cryptography.RSASignaturePadding.Pkcs1);
            req.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
            req.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new System.Security.Cryptography.OidCollection { new System.Security.Cryptography.Oid("1.3.6.1.5.5.7.3.1") }, false));
            req.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, false));
            var san = new SubjectAlternativeNameBuilder();
            san.AddDnsName(commonName);
            req.CertificateExtensions.Add(san.Build());
            X509Certificate2 cert = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(1));
            if (OperatingSystem.IsWindows())
            {
                X509Certificate2 fromPfx = X509CertificateLoader.LoadPkcs12(cert.Export(X509ContentType.Pfx), (string?)null);
                cert.Dispose();
                return fromPfx;
            }
            return cert;
        }

        [Fact]
        public async Task ServerSession_GetClientHelloBytes_BufferedPath_ReturnsWireBytes()
        {
            using X509Certificate2 serverCert = TestCertificates.GetServerCertificate();
            string serverName = serverCert.GetNameInfo(X509NameType.SimpleName, forIssuer: false);

            (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();
            using (clientStream)
            using (serverStream)
            using (SslStream clientSsl = new SslStream(clientStream, leaveInnerStreamOpen: false, TestHelper.AllowAnyServerCertificate))
            {
                using TlsContext ctx = TlsContext.Create(new SslServerAuthenticationOptions
                {
                    ServerCertificate = serverCert,
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                });
                using TlsSession session = TlsSession.Create(ctx);

                Task clientHandshake = clientSsl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                {
                    TargetHost = serverName,
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                    RemoteCertificateValidationCallback = TestHelper.AllowAnyServerCertificate,
                });

                Task serverHandshake = DriveHandshakeAsync(session, serverStream);

                await Task.WhenAll(clientHandshake, serverHandshake).WaitAsync(TimeSpan.FromSeconds(30));
                Assert.True(session.IsHandshakeComplete);

                // Capture bytes via ToArray so we can assert on the shape without re-entering GetClientHelloBytes.
                byte[] bytes = session.GetClientHelloBytes().ToArray();
                Assert.True(bytes.Length >= 5, $"ClientHello smaller than TLS record header: {bytes.Length}");
                // TLS handshake record: content-type 0x16, TLS 1.0/1.2 legacy version 0x0301/0x0303 in header,
                // then 2-byte length, then message body starting with handshake type 0x01 (client_hello).
                Assert.Equal(0x16, bytes[0]);
                int payloadLen = (bytes[3] << 8) | bytes[4];
                Assert.Equal(5 + payloadLen, bytes.Length);
                Assert.Equal(0x01, bytes[5]);

                // ClientHelloInfo and TargetHostName are also populated on the options-up-front path.
                Assert.NotNull(session.ClientHelloInfo);
                Assert.Equal(serverName, session.ClientHelloInfo!.Value.ServerName);
                Assert.Equal(serverName, session.TargetHostName);
            }
        }

        [Fact]
        public async Task ServerSession_GetClientHelloBytes_DeferredFlow_AvailableDuringAndAfterCallback()
        {
            using X509Certificate2 serverCert = TestCertificates.GetServerCertificate();
            string serverName = serverCert.GetNameInfo(X509NameType.SimpleName, forIssuer: false);

            byte[]? bytesDuringCallback = null;

            (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();
            using (clientStream)
            using (serverStream)
            using (SslStream clientSsl = new SslStream(clientStream, leaveInnerStreamOpen: false, TestHelper.AllowAnyServerCertificate))
            {
                using TlsContext ctx = TlsContext.Create((SslServerAuthenticationOptions?)null);
                using TlsSession session = TlsSession.Create(ctx);

                Task clientHandshake = clientSsl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                {
                    TargetHost = serverName,
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                    RemoteCertificateValidationCallback = TestHelper.AllowAnyServerCertificate,
                });

                using TlsContext hostCtx = TlsContext.Create(new SslServerAuthenticationOptions
                {
                    ServerCertificate = serverCert,
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                });

                Task serverHandshake = DriveHandshakeAsync(session, serverStream, hello =>
                {
                    bytesDuringCallback = session.GetClientHelloBytes().ToArray();
                    return hostCtx;
                });

                await Task.WhenAll(clientHandshake, serverHandshake).WaitAsync(TimeSpan.FromSeconds(30));
                Assert.True(session.IsHandshakeComplete);

                Assert.NotNull(bytesDuringCallback);
                Assert.Equal(0x16, bytesDuringCallback![0]);
                Assert.Equal(0x01, bytesDuringCallback[5]);

                // Bytes stay available post-handshake and match what we captured during the callback.
                byte[] bytesAfter = session.GetClientHelloBytes().ToArray();
                Assert.Equal(bytesDuringCallback, bytesAfter);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows))]
        public async Task SocketBoundSession_GetClientHelloBytes_ReturnsWireBytes()
        {
            using X509Certificate2 serverCert = TestCertificates.GetServerCertificate();
            string serverName = serverCert.GetNameInfo(X509NameType.SimpleName, forIssuer: false);

            using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(1);
            int port = ((IPEndPoint)listener.LocalEndPoint!).Port;

            using Socket clientUnderlying = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Task connect = clientUnderlying.ConnectAsync(IPAddress.Loopback, port);
            Socket serverSocket = await listener.AcceptAsync();
            await connect;
            serverSocket.Blocking = false;

            using TlsContext ctx = TlsContext.Create(new SslServerAuthenticationOptions
            {
                ServerCertificate = serverCert,
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
            });
            using TlsSession session = TlsSession.Create(ctx, serverSocket.SafeHandle);

            using SslStream clientSsl = new SslStream(new NetworkStream(clientUnderlying, ownsSocket: false), leaveInnerStreamOpen: false, TestHelper.AllowAnyServerCertificate);
            Task clientHandshake = clientSsl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
            {
                TargetHost = serverName,
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                RemoteCertificateValidationCallback = TestHelper.AllowAnyServerCertificate,
            });

            Task serverHandshake = Task.Run(() =>
            {
                while (true)
                {
                    TlsOperationStatus s = session.Handshake();
                    if (s == TlsOperationStatus.Complete) return;
                    if (s == TlsOperationStatus.NeedsCertificateValidation) { session.AcceptWithDefaultValidation(); continue; }
                    if (s == TlsOperationStatus.WantRead) { serverSocket.Poll(-1, SelectMode.SelectRead); continue; }
                    if (s == TlsOperationStatus.WantWrite) { serverSocket.Poll(-1, SelectMode.SelectWrite); continue; }
                    throw new IOException($"Unexpected handshake status {s}");
                }
            });

            await Task.WhenAll(clientHandshake, serverHandshake).WaitAsync(TimeSpan.FromSeconds(30));
            Assert.True(session.IsHandshakeComplete);

            // Native path: span backed by socket-replay BIO's retained peek buffer.
            byte[] bytes = session.GetClientHelloBytes().ToArray();
            Assert.Equal(0x16, bytes[0]);
            int payloadLen = (bytes[3] << 8) | bytes[4];
            Assert.Equal(5 + payloadLen, bytes.Length);
            Assert.Equal(0x01, bytes[5]);

            Assert.NotNull(session.ClientHelloInfo);
            Assert.Equal(serverName, session.ClientHelloInfo!.Value.ServerName);
            Assert.Equal(serverName, session.TargetHostName);
        }

        [Fact]
        public void ClientSession_GetClientHelloBytes_Throws()
        {
            using TlsContext ctx = TlsContext.Create(new SslClientAuthenticationOptions
            {
                TargetHost = "example.com",
            });
            using TlsSession session = TlsSession.Create(ctx);

            Assert.Throws<InvalidOperationException>(() =>
            {
                ReadOnlySpan<byte> _ = session.GetClientHelloBytes();
            });
        }

        // Regression: SetClientCertificateContext used to dispose+null the shared
        // TlsContext.CredentialsHandle, racing with any concurrent session on the same
        // context. It must instead acquire session-local credentials without touching
        // the shared field. Run several client sessions in parallel against a shared
        // TlsContext, each supplying a distinct cert via SetClientCertificateContext,
        // and verify every server sees the correct client cert.
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows))]
        [SkipOnPlatform(TestPlatforms.OSX, "SecureTransport does not surface deferred client-credential prompts.")]
        public async Task SetClientCertificateContext_ConcurrentSessionsOnSharedContext_DoNotRace()
        {
            using X509Certificate2 serverCert = TestCertificates.GetServerCertificate();
            string serverName = serverCert.GetNameInfo(X509NameType.SimpleName, forIssuer: false);

            const int SessionCount = 4;
            X509Certificate2[] clientCerts = new X509Certificate2[SessionCount];
            for (int i = 0; i < SessionCount; i++)
            {
                clientCerts[i] = CreateSelfSignedServerCert($"tls-session-concurrent-client-{i}.example");
            }
            string?[] observedThumbprints = new string?[SessionCount];

            try
            {
                // Shared TlsContext across all sessions - no client cert baked in.
                using TlsContext sharedCtx = TlsContext.Create(new SslClientAuthenticationOptions
                {
                    TargetHost = serverName,
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                    AllowTlsResume = false,
                    RemoteCertificateValidationCallback = TestHelper.AllowAnyServerCertificate,
                });

                Task[] tasks = new Task[SessionCount];
                for (int i = 0; i < SessionCount; i++)
                {
                    int idx = i;
                    tasks[i] = Task.Run(() => RunConcurrentSessionAsync(sharedCtx, serverCert, clientCerts[idx], observedThumbprints, idx));
                }

                await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(60));

                for (int i = 0; i < SessionCount; i++)
                {
                    Assert.Equal(clientCerts[i].Thumbprint, observedThumbprints[i]);
                }
            }
            finally
            {
                for (int i = 0; i < SessionCount; i++)
                {
                    clientCerts[i]?.Dispose();
                }
            }
        }

        private static async Task RunConcurrentSessionAsync(
            TlsContext sharedCtx, X509Certificate2 serverCert, X509Certificate2 clientCert,
            string?[] observedThumbprints, int slot)
        {
            (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();
            using (clientStream)
            using (serverStream)
            using (SslStream serverSsl = new SslStream(serverStream, leaveInnerStreamOpen: false))
            {
                Task serverHandshake = serverSsl.AuthenticateAsServerAsync(new SslServerAuthenticationOptions
                {
                    ServerCertificate = serverCert,
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                    AllowTlsResume = false,
                    ClientCertificateRequired = true,
                    RemoteCertificateValidationCallback = (_, c, _, _) =>
                    {
                        observedThumbprints[slot] = (c as X509Certificate2)?.Thumbprint;
                        return true;
                    },
                });

                using TlsSession session = TlsSession.Create(sharedCtx);
                Task clientHandshake = Task.Run(async () =>
                {
                    byte[] netIn = ArrayPool<byte>.Shared.Rent(CipherBufSize);
                    byte[] netOut = ArrayPool<byte>.Shared.Rent(CipherBufSize);
                    int inUsed = 0;
                    try
                    {
                        while (!session.IsHandshakeComplete)
                        {
                            TlsOperationStatus status = session.ProcessHandshake(
                                netIn.AsSpan(0, inUsed), netOut, out int consumed, out int produced);
                            if (consumed > 0)
                            {
                                if (consumed < inUsed) Buffer.BlockCopy(netIn, consumed, netIn, 0, inUsed - consumed);
                                inUsed -= consumed;
                            }
                            if (produced > 0)
                            {
                                await clientStream.WriteAsync(netOut.AsMemory(0, produced));
                                await clientStream.FlushAsync();
                            }
                            switch (status)
                            {
                                case TlsOperationStatus.Complete: continue;
                                case TlsOperationStatus.NeedsCertificateValidation:
                                    session.AcceptWithDefaultValidation();
                                    continue;
                                case TlsOperationStatus.WantCredentials:
                                    session.SetClientCertificateContext(
                                        SslStreamCertificateContext.Create(clientCert, additionalCertificates: null));
                                    continue;
                                case TlsOperationStatus.WantWrite:
                                    await DrainAsync(session, clientStream, netOut);
                                    continue;
                                case TlsOperationStatus.WantRead:
                                    int r = await clientStream.ReadAsync(netIn.AsMemory(inUsed));
                                    if (r == 0) throw new IOException("EOF during handshake");
                                    inUsed += r;
                                    continue;
                                case TlsOperationStatus.Closed:
                                    throw new IOException("Peer closed during handshake");
                            }
                        }
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(netIn);
                        ArrayPool<byte>.Shared.Return(netOut);
                    }
                });

                await Task.WhenAll(serverHandshake, clientHandshake).WaitAsync(TimeSpan.FromSeconds(30));
                Assert.True(session.IsHandshakeComplete);
                Assert.True(serverSsl.IsAuthenticated);
            }
        }

        [Fact]
        public void ServerSession_GetClientHelloBytes_BeforeClientHello_Throws()
        {
            using X509Certificate2 serverCert = TestCertificates.GetServerCertificate();
            using TlsContext ctx = TlsContext.Create(new SslServerAuthenticationOptions
            {
                ServerCertificate = serverCert,
            });
            using TlsSession session = TlsSession.Create(ctx);

            Assert.Throws<InvalidOperationException>(() =>
            {
                ReadOnlySpan<byte> _ = session.GetClientHelloBytes();
            });
        }
    }
}
