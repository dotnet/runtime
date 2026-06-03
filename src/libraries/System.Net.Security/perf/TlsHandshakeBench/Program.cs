// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

using Microsoft.Win32.SafeHandles;

public static class Program
{
    public static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--trace")
        {
            int n = args.Length > 1 ? int.Parse(args[1]) : 3;
            SslProtocols proto = args.Length > 2 && args[2] == "Tls12" ? SslProtocols.Tls12 : SslProtocols.Tls13;
            bool allowResume = args.Length > 3 && bool.Parse(args[3]);
            string mode = args.Length > 4 ? args[4] : "buffered"; // buffered | fd
            TraceHarness.Run(n, proto, allowResume, mode).GetAwaiter().GetResult();
            return;
        }
        IConfig config = DefaultConfig.Instance
            .AddJob(Job.Default
                .WithToolchain(InProcessEmitToolchain.Instance)
                // Cap iteration count so resumed handshakes (~10 us) don't blow
                // past BDN's InProcessEmit 10 s budget while auto-scaling.
                // InvocationCount kept low so the per-op estimate from
                // WorkloadJitting (TLS 1.3 cold init can be ~100 ms) doesn't
                // make BDN reject the iteration as "takes too long".
                .WithIterationCount(15)
                .WithWarmupCount(5)
                .WithInvocationCount(64)
                .WithUnrollFactor(1))
            .WithOptions(ConfigOptions.DisableOptimizationsValidator);
        BenchmarkSwitcher.FromAssembly(typeof(TlsHandshakeBench).Assembly).Run(args, config);
    }
}

internal static class Probe
{
    public static int Receives;
    public static int Sends;
    public static int PollRead;
    public static int PollWrite;
    public static int ProcessHandshakeCalls;
    public static int DrainCalls;
    public static long BytesReceived;
    public static long BytesSent;

    public static void Reset()
    {
        Receives = Sends = PollRead = PollWrite = ProcessHandshakeCalls = DrainCalls = 0;
        BytesReceived = BytesSent = 0;
    }

    public static string Dump(string label)
        => $"{label,-32} recv={Receives,3} bytes={BytesReceived,5}  send={Sends,3} bytes={BytesSent,5}  pollR={PollRead,3} pollW={PollWrite,3}  proc={ProcessHandshakeCalls,3} drain={DrainCalls,3}";
}

[MemoryDiagnoser]
public class TlsHandshakeBench
{
    private const int ScratchSize = 32 * 1024;
    private const string ServerName = "tlsbench.local";

    private static TlsHandshakeBench? s_current;

    private X509Certificate2 _cert = null!;
    private SslServerAuthenticationOptions _serverOptions = null!;
    private SslClientAuthenticationOptions _clientOptions = null!;
    private TlsContext _ctxBuffered = null!;
    private TlsContext _ctxFd = null!;
    private IPEndPoint _listenerEp = null!;
    private Socket _listener = null!;

    [Params(SslProtocols.Tls12, SslProtocols.Tls13)]
    public SslProtocols Protocol { get; set; }

    [Params(true, false)]
    public bool AllowResume { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        s_current = this;
        _cert = CreateSelfSignedCert();

        _serverOptions = new SslServerAuthenticationOptions
        {
            ServerCertificate = _cert,
            ClientCertificateRequired = false,
            EnabledSslProtocols = Protocol,
            AllowTlsResume = AllowResume,
        };
        _clientOptions = new SslClientAuthenticationOptions
        {
            TargetHost = ServerName,
            EnabledSslProtocols = Protocol,
            RemoteCertificateValidationCallback = static (_, _, _, _) => true,
            AllowTlsResume = AllowResume,
        };

        // TlsContext is allocated once and reused; SSL_CTX caching is the design point.
        _ctxBuffered = TlsContext.Create(_serverOptions);
        _ctxFd = TlsContext.Create(_serverOptions);

        _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        _listener.Listen(128);
        _listenerEp = (IPEndPoint)_listener.LocalEndPoint!;
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _listener?.Dispose();
        _ctxBuffered?.Dispose();
        _ctxFd?.Dispose();
        _cert?.Dispose();
    }

    // Baseline: SslStream on both sides over loopback TCP.
    [Benchmark(Baseline = true)]
    public async Task SslStream_Server()
    {
        (Socket cs, Socket ss) = await ConnectPairAsync();
        using var clientStream = new NetworkStream(cs, ownsSocket: true);
        using var serverStream = new NetworkStream(ss, ownsSocket: true);
        using var client = new SslStream(clientStream, leaveInnerStreamOpen: false);
        using var server = new SslStream(serverStream, leaveInnerStreamOpen: false);

        Task c = client.AuthenticateAsClientAsync(_clientOptions);
        Task s = server.AuthenticateAsServerAsync(_serverOptions);
        await Task.WhenAll(c, s);
        if (!client.IsAuthenticated || !server.IsAuthenticated) throw new IOException("sslstream handshake not complete");

        await SslStreamPingPongAsync(client, server);
    }
    // TlsSession on the server, driven through the managed buffered path
    // (ProcessHandshake + DrainPendingOutput) over a non-blocking socket.
    // Client is SslStream.
    [Benchmark]
    public async Task TlsSession_Buffered_Server()
    {
        (Socket cs, Socket ss) = await ConnectPairAsync();
        ss.Blocking = false;

        using var clientStream = new NetworkStream(cs, ownsSocket: true);
        using var client = new SslStream(clientStream, leaveInnerStreamOpen: false);
        using TlsSession session = TlsSession.Create(_ctxBuffered);

        Task c = ClientHandshakeThenPingPongAsync(client);
        Task s = RunOnDedicatedThreadAsync(() => DriveBufferedHandshakeAndPingPong(session, ss));
        await Task.WhenAll(c, s);
        if (!session.IsHandshakeComplete) throw new IOException("server buffered handshake not complete");
        if (!client.IsAuthenticated) throw new IOException("client buffered handshake not complete");
        ss.Dispose();
    }

    // TlsSession on the server, bound directly to the socket fd via SSL_set_fd.
    // Linux/FreeBSD only — on Windows this throws PlatformNotSupportedException.
    [Benchmark]
    public async Task TlsSession_Fd_Server()
    {
        (Socket cs, Socket ss) = await ConnectPairAsync();
        ss.Blocking = false;

        using var clientStream = new NetworkStream(cs, ownsSocket: true);
        using var client = new SslStream(clientStream, leaveInnerStreamOpen: false);
        using TlsSession session = TlsSession.Create(_ctxFd, ss.SafeHandle);

        Task c = ClientHandshakeThenPingPongAsync(client);
        Task s = RunOnDedicatedThreadAsync(() => DriveFdHandshakeAndPingPong(session, ss));
        await Task.WhenAll(c, s);
        if (!session.IsHandshakeComplete) throw new IOException("server fd handshake not complete");
        if (!client.IsAuthenticated) throw new IOException("client fd handshake not complete");

        // session owns ss.SafeHandle; ss itself becomes unusable, so no explicit close needed.
        GC.KeepAlive(ss);
    }

    // BDN's InProcessEmit drives the workload from a single thread via blocking
    // wait; using Task.Run for the server side competes for thread-pool threads
    // with SslStream's continuations and can starve under tight measurement loops.
    // A dedicated thread sidesteps the issue.
    private static Task RunOnDedicatedThreadAsync(Action action)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var t = new Thread(() =>
        {
            try { action(); tcs.SetResult(); }
            catch (Exception ex) { tcs.SetException(ex); }
        }) { IsBackground = true };
        t.Start();
        return tcs.Task;
    }

    private async ValueTask<(Socket Client, Socket Server)> ConnectPairAsync()
    {
        var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        client.NoDelay = true;
        Task<Socket> acceptTask = _listener.AcceptAsync();
        await client.ConnectAsync(_listenerEp);
        Socket server = await acceptTask;
        server.NoDelay = true;
        return (client, server);
    }

    private static async Task ClientHandshakeThenPingPongAsync(SslStream client)
    {
        TlsHandshakeBench bench = s_current!;
        await client.AuthenticateAsClientAsync(bench._clientOptions);
        if (!client.IsAuthenticated) throw new IOException("client handshake not complete");
        // Send 1 byte, read 1 byte. This drains any post-handshake server messages
        // (TLS 1.3 NewSessionTicket) and validates end-to-end data flow.
        byte[] one = new byte[1] { 0xAB };
        await client.WriteAsync(one);
        byte[] rx = new byte[1];
        int n = await client.ReadAsync(rx);
        if (n != 1 || rx[0] != 0xCD) throw new IOException($"client ping/pong failed n={n}");
    }

    private static async Task SslStreamPingPongAsync(SslStream client, SslStream server)
    {
        byte[] one = new byte[1] { 0xAB };
        byte[] rx = new byte[1];
        Task c1 = client.WriteAsync(one).AsTask();
        Task<int> s1 = server.ReadAsync(rx).AsTask();
        await Task.WhenAll(c1, s1);
        if (s1.Result != 1 || rx[0] != 0xAB) throw new IOException("sslstream ping failed");
        rx[0] = 0;
        byte[] back = new byte[1] { 0xCD };
        Task s2 = server.WriteAsync(back).AsTask();
        Task<int> c2 = client.ReadAsync(rx).AsTask();
        await Task.WhenAll(s2, c2);
        if (c2.Result != 1 || rx[0] != 0xCD) throw new IOException("sslstream pong failed");
    }

    private static void DriveBufferedHandshakeAndPingPong(TlsSession session, Socket socket)
    {
        DriveBufferedHandshake(session, socket);
        if (!session.IsHandshakeComplete) throw new IOException("buffered handshake not complete before ping/pong");
        byte[] netIn = ArrayPool<byte>.Shared.Rent(ScratchSize);
        byte[] netOut = ArrayPool<byte>.Shared.Rent(ScratchSize);
        byte[] plain = ArrayPool<byte>.Shared.Rent(ScratchSize);
        try
        {
            // TLS 1.3: server emits NewSessionTicket records after Finished. They sit in
            // OpenSSL's output BIO until we drain them; the client may also block waiting
            // on them depending on the implementation.
            DrainPending(session, socket, netOut);
            // Read 1 plaintext byte from peer.
            // Important: if the peer coalesced its client-Finished with the first app-data record
            // into one TCP segment, the ping ciphertext was already absorbed by OpenSSL during
            // the handshake. The first Decrypt call below is intentionally made with empty input
            // so TlsSession can drain that buffered plaintext before we wait on the socket.
            int inUsed = 0;
            while (true)
            {
                TlsOperationStatus s = session.Decrypt(netIn.AsSpan(0, inUsed), plain, out int consumed, out int produced);
                if (consumed > 0)
                {
                    if (consumed < inUsed) Buffer.BlockCopy(netIn, consumed, netIn, 0, inUsed - consumed);
                    inUsed -= consumed;
                }
                if (produced > 0)
                {
                    if (plain[0] != 0xAB) throw new IOException("buffered ping mismatch");
                    break;
                }
                switch (s)
                {
                    case TlsOperationStatus.WantRead:
                        inUsed += NonBlockingReceiveSome(socket, netIn, inUsed);
                        continue;
                    case TlsOperationStatus.WantWrite:
                        DrainPending(session, socket, netOut);
                        continue;
                    case TlsOperationStatus.Closed:
                        throw new IOException("closed during ping read");
                }
            }
            // Write 1 plaintext byte back
            byte[] pong = new byte[1] { 0xCD };
            int sent = 0;
            while (sent < 1)
            {
                TlsOperationStatus s = session.Encrypt(pong.AsSpan(sent), netOut, out int consumed, out int produced);
                sent += consumed;
                if (produced > 0) NonBlockingSendAll(socket, netOut, 0, produced);
                if (s == TlsOperationStatus.WantWrite) DrainPending(session, socket, netOut);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(plain);
            ArrayPool<byte>.Shared.Return(netOut);
            ArrayPool<byte>.Shared.Return(netIn);
        }
    }

    private static void DriveFdHandshakeAndPingPong(TlsSession session, Socket socket)
    {
        DriveFdHandshake(session, socket);
        if (!session.IsHandshakeComplete) throw new IOException("fd handshake not complete before ping/pong");
        // 1-byte read
        byte[] rx = new byte[1];
        while (true)
        {
            TlsOperationStatus s = session.Read(rx, out int produced);
            if (produced == 1) { if (rx[0] != 0xAB) throw new IOException("fd ping mismatch"); break; }
            switch (s)
            {
                case TlsOperationStatus.WantRead: socket.Poll(-1, SelectMode.SelectRead); continue;
                case TlsOperationStatus.WantWrite: socket.Poll(-1, SelectMode.SelectWrite); continue;
                default: throw new IOException($"fd read status {s}");
            }
        }
        // 1-byte write
        byte[] tx = new byte[1] { 0xCD };
        int sent = 0;
        while (sent < 1)
        {
            TlsOperationStatus s = session.Write(tx.AsSpan(sent), out int consumed);
            sent += consumed;
            if (sent == 1) break;
            switch (s)
            {
                case TlsOperationStatus.WantRead: socket.Poll(-1, SelectMode.SelectRead); continue;
                case TlsOperationStatus.WantWrite: socket.Poll(-1, SelectMode.SelectWrite); continue;
                default: throw new IOException($"fd write status {s}");
            }
        }
    }

    private static void DriveBufferedHandshake(TlsSession session, Socket socket)
    {
        byte[] netIn = ArrayPool<byte>.Shared.Rent(ScratchSize);
        byte[] netOut = ArrayPool<byte>.Shared.Rent(ScratchSize);
        int inUsed = 0;
        try
        {
            while (!session.IsHandshakeComplete)
            {
                Probe.ProcessHandshakeCalls++;
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
                        throw new IOException("Closed in handshake.");
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(netOut);
            ArrayPool<byte>.Shared.Return(netIn);
        }
    }

    private static void DriveFdHandshake(TlsSession session, Socket socket)
    {
        // fd-mode: OpenSSL drives the socket directly. Wait on socket readiness
        // (not SpinWait) when WantRead/WantWrite surfaces.
        while (true)
        {
            TlsOperationStatus s = session.Handshake();
            switch (s)
            {
                case TlsOperationStatus.Complete:
                    return;
                case TlsOperationStatus.NeedsCertificateValidation:
                    session.AcceptWithDefaultValidation();
                    continue;
                case TlsOperationStatus.WantRead:
                    Probe.PollRead++;
                    socket.Poll(-1, SelectMode.SelectRead);
                    continue;
                case TlsOperationStatus.WantWrite:
                    Probe.PollWrite++;
                    socket.Poll(-1, SelectMode.SelectWrite);
                    continue;
                default:
                    throw new IOException($"Unexpected handshake status: {s}");
            }
        }
    }

    private static void DrainPending(TlsSession session, Socket socket, byte[] scratch)
    {
        while (session.HasPendingOutput)
        {
            Probe.DrainCalls++;
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
                Probe.Sends++; Probe.BytesSent += n;
                offset += n;
                count -= n;
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.WouldBlock)
            {
                Probe.PollWrite++;
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
                Probe.Receives++; Probe.BytesReceived += n;
                if (n == 0)
                {
                    throw new IOException("Unexpected EOF.");
                }
                return n;
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.WouldBlock)
            {
                Probe.PollRead++;
                socket.Poll(-1, SelectMode.SelectRead);
            }
        }
    }

    private static X509Certificate2 CreateSelfSignedCert()
    {
        using RSA rsa = RSA.Create(2048);
        var req = new CertificateRequest(
            $"CN={ServerName}",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        var san = new SubjectAlternativeNameBuilder();
        san.AddDnsName(ServerName);
        req.CertificateExtensions.Add(san.Build());
        req.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        req.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, false));
        req.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
            new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false));

        DateTimeOffset now = DateTimeOffset.UtcNow;
        using X509Certificate2 ephemeral = req.CreateSelfSigned(now.AddMinutes(-5), now.AddDays(1));
        // Round-trip to a PFX-backed cert so the private key is reliably available
        // to both managed (Windows) and OpenSSL paths.
        return X509CertificateLoader.LoadPkcs12(ephemeral.Export(X509ContentType.Pkcs12), null);
    }
}

internal static class TraceHarness
{
    public static async Task Run(int iterations, SslProtocols protocol, bool allowResume, string mode)
    {
        Console.WriteLine($"=== TRACE iterations={iterations} protocol={protocol} allowResume={allowResume} mode={mode} ===");

        using X509Certificate2 cert = TraceCert();
        string serverName = "tlsbench.local";
        var serverOpts = new SslServerAuthenticationOptions
        {
            ServerCertificate = cert,
            EnabledSslProtocols = protocol,
            ClientCertificateRequired = false,
            AllowTlsResume = allowResume,
        };
        var clientOpts = new SslClientAuthenticationOptions
        {
            TargetHost = serverName,
            EnabledSslProtocols = protocol,
            RemoteCertificateValidationCallback = static (_, _, _, _) => true,
            AllowTlsResume = allowResume,
        };
        using TlsContext ctx = TlsContext.Create(serverOpts);

        var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        listener.Listen(8);
        var ep = (IPEndPoint)listener.LocalEndPoint!;

        for (int i = 0; i < iterations; i++)
        {
            Console.WriteLine($"\n--- iteration {i} ---");
            var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            Task<Socket> accept = listener.AcceptAsync();
            await client.ConnectAsync(ep);
            Socket server = await accept;
            server.NoDelay = true;
            server.Blocking = false;

            using TlsSession session = mode == "fd"
                ? TlsSession.Create(ctx, server.SafeHandle)
                : TlsSession.Create(ctx);

            using var clientStream = new NetworkStream(client, ownsSocket: true);
            using var sslClient = new SslStream(clientStream, leaveInnerStreamOpen: false);

            Task c = Task.Run(async () =>
            {
                Console.WriteLine($"[i{i}][C] AuthenticateAsClient start");
                await sslClient.AuthenticateAsClientAsync(clientOpts);
                Console.WriteLine($"[i{i}][C] AuthenticateAsClient done; protocol={sslClient.SslProtocol} cipher={sslClient.NegotiatedCipherSuite}");
                byte[] tx = new byte[] { 0xAB };
                Console.WriteLine($"[i{i}][C] WriteAsync 1 byte (ping)");
                await sslClient.WriteAsync(tx);
                byte[] rx = new byte[1];
                Console.WriteLine($"[i{i}][C] ReadAsync 1 byte (pong)");
                int n = await sslClient.ReadAsync(rx);
                Console.WriteLine($"[i{i}][C] ReadAsync returned n={n} val=0x{rx[0]:X}");
            });

            Task s = Task.Run(() =>
            {
                try
                {
                    DriveTraced(session, server, i, mode);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[i{i}][S] EXCEPTION: {ex}");
                    throw;
                }
            });

            await Task.WhenAll(c, s);
            try { server.Shutdown(SocketShutdown.Both); } catch { }
            server.Dispose();
            Console.WriteLine($"[i{i}] iteration complete");
        }
        listener.Dispose();
    }

    private static void DriveTraced(TlsSession session, Socket socket, int iter, string mode)
    {
        const int ScratchSize = 32 * 1024;
        byte[] netIn = new byte[ScratchSize];
        byte[] netOut = new byte[ScratchSize];
        int inUsed = 0;

        if (mode == "fd")
        {
            Console.WriteLine($"[i{iter}][S] fd handshake start");
            while (true)
            {
                TlsOperationStatus s = session.Handshake();
                Console.WriteLine($"[i{iter}][S] Handshake -> {s} complete={session.IsHandshakeComplete}");
                if (s == TlsOperationStatus.Complete) break;
                if (s == TlsOperationStatus.NeedsCertificateValidation) { session.AcceptWithDefaultValidation(); continue; }
                if (s == TlsOperationStatus.WantRead) { socket.Poll(-1, SelectMode.SelectRead); continue; }
                if (s == TlsOperationStatus.WantWrite) { socket.Poll(-1, SelectMode.SelectWrite); continue; }
                throw new IOException($"unexpected {s}");
            }
        }
        else
        {
            Console.WriteLine($"[i{iter}][S] buffered handshake start");
            while (!session.IsHandshakeComplete)
            {
                TlsOperationStatus status = session.ProcessHandshake(netIn.AsSpan(0, inUsed), netOut, out int consumed, out int produced);
                Console.WriteLine($"[i{iter}][S] ProcessHandshake in={inUsed} consumed={consumed} produced={produced} -> {status} complete={session.IsHandshakeComplete}");
                if (consumed > 0)
                {
                    if (consumed < inUsed) Buffer.BlockCopy(netIn, consumed, netIn, 0, inUsed - consumed);
                    inUsed -= consumed;
                }
                if (produced > 0)
                {
                    int sent = TraceSend(socket, netOut, 0, produced, iter, "hs");
                    Console.WriteLine($"[i{iter}][S]   wrote {sent} bytes to socket");
                }
                switch (status)
                {
                    case TlsOperationStatus.Complete: continue;
                    case TlsOperationStatus.NeedsCertificateValidation: session.AcceptWithDefaultValidation(); continue;
                    case TlsOperationStatus.WantWrite:
                        Console.WriteLine($"[i{iter}][S]   draining pending");
                        DrainTraced(session, socket, netOut, iter);
                        continue;
                    case TlsOperationStatus.WantRead:
                        inUsed += TraceRecv(socket, netIn, inUsed, iter, "hs");
                        continue;
                    case TlsOperationStatus.Closed:
                        throw new IOException("closed in handshake");
                }
            }
            Console.WriteLine($"[i{iter}][S] handshake complete; post-handshake drain");
            DrainTraced(session, socket, netOut, iter);
        }

        // ping read
        Console.WriteLine($"[i{iter}][S] reading 1-byte ping");
        if (mode == "fd")
        {
            byte[] rx = new byte[1];
            while (true)
            {
                TlsOperationStatus s = session.Read(rx, out int produced);
                Console.WriteLine($"[i{iter}][S] Read -> {s} produced={produced}");
                if (produced == 1) break;
                if (s == TlsOperationStatus.WantRead) { socket.Poll(-1, SelectMode.SelectRead); continue; }
                if (s == TlsOperationStatus.WantWrite) { socket.Poll(-1, SelectMode.SelectWrite); continue; }
                throw new IOException($"read {s}");
            }
        }
        else
        {
            byte[] plain = new byte[1024];
            while (true)
            {
                TlsOperationStatus s = session.Decrypt(netIn.AsSpan(0, inUsed), plain, out int consumed, out int produced);
                Console.WriteLine($"[i{iter}][S] Decrypt in={inUsed} consumed={consumed} produced={produced} -> {s}");
                if (consumed > 0)
                {
                    if (consumed < inUsed) Buffer.BlockCopy(netIn, consumed, netIn, 0, inUsed - consumed);
                    inUsed -= consumed;
                }
                if (produced > 0) break;
                switch (s)
                {
                    case TlsOperationStatus.WantRead:
                        inUsed += TraceRecv(socket, netIn, inUsed, iter, "ping");
                        continue;
                    case TlsOperationStatus.WantWrite:
                        DrainTraced(session, socket, netOut, iter);
                        continue;
                    case TlsOperationStatus.Closed:
                        throw new IOException("closed");
                }
            }
        }

        // pong write
        Console.WriteLine($"[i{iter}][S] writing 1-byte pong");
        if (mode == "fd")
        {
            byte[] tx = new byte[] { 0xCD };
            int written = 0;
            while (written < 1)
            {
                TlsOperationStatus s = session.Write(tx.AsSpan(written), out int consumed);
                written += consumed;
                Console.WriteLine($"[i{iter}][S] Write -> {s} consumed={consumed}");
                if (written == 1) break;
                if (s == TlsOperationStatus.WantWrite) { socket.Poll(-1, SelectMode.SelectWrite); continue; }
                if (s == TlsOperationStatus.WantRead) { socket.Poll(-1, SelectMode.SelectRead); continue; }
                throw new IOException($"write {s}");
            }
        }
        else
        {
            byte[] tx = new byte[] { 0xCD };
            int written = 0;
            while (written < 1)
            {
                TlsOperationStatus s = session.Encrypt(tx.AsSpan(written), netOut, out int consumed, out int produced);
                written += consumed;
                Console.WriteLine($"[i{iter}][S] Encrypt -> {s} consumed={consumed} produced={produced}");
                if (produced > 0) TraceSend(socket, netOut, 0, produced, iter, "pong");
                if (s == TlsOperationStatus.WantWrite) DrainTraced(session, socket, netOut, iter);
            }
        }
        Console.WriteLine($"[i{iter}][S] done");
    }

    private static void DrainTraced(TlsSession session, Socket socket, byte[] scratch, int iter)
    {
        while (session.HasPendingOutput)
        {
            session.DrainPendingOutput(scratch, out int n);
            Console.WriteLine($"[i{iter}][S]   DrainPendingOutput n={n}");
            if (n > 0) TraceSend(socket, scratch, 0, n, iter, "drain");
        }
    }

    private static int TraceSend(Socket s, byte[] buf, int off, int count, int iter, string tag)
    {
        int total = 0;
        while (count > 0)
        {
            try
            {
                int n = s.Send(buf, off, count, SocketFlags.None);
                Console.WriteLine($"[i{iter}][S]     send/{tag} n={n}");
                off += n; count -= n; total += n;
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.WouldBlock)
            {
                s.Poll(-1, SelectMode.SelectWrite);
            }
        }
        return total;
    }

    private static int TraceRecv(Socket s, byte[] buf, int off, int iter, string tag)
    {
        while (true)
        {
            try
            {
                int n = s.Receive(buf, off, buf.Length - off, SocketFlags.None);
                Console.WriteLine($"[i{iter}][S]     recv/{tag} n={n}");
                if (n == 0) throw new IOException("EOF");
                return n;
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.WouldBlock)
            {
                s.Poll(-1, SelectMode.SelectRead);
            }
        }
    }

    private static X509Certificate2 TraceCert()
    {
        using RSA rsa = RSA.Create(2048);
        var req = new CertificateRequest("CN=tlsbench.local", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var san = new SubjectAlternativeNameBuilder();
        san.AddDnsName("tlsbench.local");
        req.CertificateExtensions.Add(san.Build());
        req.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        req.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, false));
        req.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false));
        DateTimeOffset now = DateTimeOffset.UtcNow;
        using X509Certificate2 ephem = req.CreateSelfSigned(now.AddMinutes(-5), now.AddDays(1));
        return X509CertificateLoader.LoadPkcs12(ephem.Export(X509ContentType.Pkcs12), null);
    }
}
