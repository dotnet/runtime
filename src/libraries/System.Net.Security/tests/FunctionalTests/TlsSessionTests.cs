// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO;
using System.Net.Test.Common;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Xunit;

using TestCertificates = System.Net.Test.Common.Configuration.Certificates;

namespace System.Net.Security.Tests
{
    [PlatformSpecific(TestPlatforms.Linux | TestPlatforms.FreeBSD)]
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

        [Fact]
        public void TlsContext_RejectsNullOptions()
        {
            Assert.Throws<ArgumentNullException>(() => TlsContext.Create((SslServerAuthenticationOptions)null!));
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

        [Fact]
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

        // Pinned to TLS 1.2: a pure in-memory two-session loop currently can't
        // handle the TLS 1.3 post-handshake NewSessionTicket records that OpenSSL
        // emits after the server consumes the client Finished. With SslStream on
        // one side the data-path handles those records transparently; the
        // standalone TlsSession surface does not yet (same scope as PHA).
        [Fact]
        public void TwoSessions_HandshakeAndPingPong_InMemory_Succeeds()
        {
            using X509Certificate2 serverCert = TestCertificates.GetServerCertificate();
            string serverName = serverCert.GetNameInfo(X509NameType.SimpleName, forIssuer: false);

            using TlsContext serverCtx = TlsContext.Create(new SslServerAuthenticationOptions
            {
                ServerCertificate = serverCert,
                EnabledSslProtocols = SslProtocols.Tls12,
                ClientCertificateRequired = false,
            });
            using TlsContext clientCtx = TlsContext.Create(new SslClientAuthenticationOptions
            {
                TargetHost = serverName,
                EnabledSslProtocols = SslProtocols.Tls12,
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
            Assert.Equal(SslProtocols.Tls12, client.NegotiatedProtocol);

            byte[] ping = "PING from client"u8.ToArray();
            byte[] pong = "PONG from server"u8.ToArray();

            Assert.Equal(ping, RoundtripRecord(client, server, ping));
            Assert.Equal(pong, RoundtripRecord(server, client, pong));
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

        private static async Task DriveHandshakeAsync(TlsSession session, Stream transport)
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
    }
}
