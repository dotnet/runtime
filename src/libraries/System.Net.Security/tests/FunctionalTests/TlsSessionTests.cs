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

                Task serverHandshake = DriveServerHandshakeAsync(session, serverStream);

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
                Task serverHandshake = DriveServerHandshakeAsync(session, serverStream);
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
                Task serverHandshake = DriveServerHandshakeAsync(session, serverStream);
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

        // ── Helpers ────────────────────────────────────────────────────────

        private static async Task DriveServerHandshakeAsync(TlsSession session, Stream transport)
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
