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
        public async Task ServerSession_RequestRenegotiation_Tls12_ProducesHelloRequest()
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

                // Server requests renegotiation. We only verify that the API runs
                // and produces a HelloRequest byte stream; driving the full
                // renegotiation back through SslStream is intentionally out of
                // scope here since SslStream's client-side reneg path needs the
                // server to also pump the post-handshake read loop, which the
                // standalone TlsSession leaves to the caller.
                byte[] reneg = ArrayPool<byte>.Shared.Rent(CipherBufSize);
                try
                {
                    TlsOperationStatus status = session.RequestRenegotiation(reneg, out int produced);
                    Assert.NotEqual(TlsOperationStatus.Closed, status);
                    Assert.True(produced > 0, "RequestRenegotiation should emit a HelloRequest.");
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(reneg);
                }
            }
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
