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
        // BenchmarkDotNet doesn't know about net11.0 monikers, so we can't spawn
        // child processes the normal way. Run in-process — for benchmarks dominated
        // by socket I/O and crypto, the overhead difference is negligible.
        IConfig config = DefaultConfig.Instance
            .AddJob(Job.Default.WithToolchain(InProcessEmitToolchain.Instance))
            // We may be running against a Debug-config local runtime layout when a
            // full Release testhost hasn't been built. Don't fail validation on that.
            .WithOptions(ConfigOptions.DisableOptimizationsValidator);
        BenchmarkSwitcher.FromAssembly(typeof(TlsHandshakeBench).Assembly).Run(args, config);
    }
}

[MemoryDiagnoser]
public class TlsHandshakeBench
{
    private const int ScratchSize = 32 * 1024;
    private const string ServerName = "tlsbench.local";

    private X509Certificate2 _cert = null!;
    private SslServerAuthenticationOptions _serverOptions = null!;
    private SslClientAuthenticationOptions _clientOptions = null!;
    private TlsContext _ctxBuffered = null!;
    private TlsContext _ctxFd = null!;
    private IPEndPoint _listenerEp = null!;
    private Socket _listener = null!;

    [Params(SslProtocols.Tls12, SslProtocols.Tls13)]
    public SslProtocols Protocol { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _cert = CreateSelfSignedCert();

        _serverOptions = new SslServerAuthenticationOptions
        {
            ServerCertificate = _cert,
            ClientCertificateRequired = false,
            EnabledSslProtocols = Protocol,
        };
        _clientOptions = new SslClientAuthenticationOptions
        {
            TargetHost = ServerName,
            EnabledSslProtocols = Protocol,
            RemoteCertificateValidationCallback = static (_, _, _, _) => true,
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
    }

    // TlsSession on the server, driven through the managed buffered path
    // (ProcessHandshake + DrainPendingOutput). Client is SslStream.
    [Benchmark]
    public async Task TlsSession_Buffered_Server()
    {
        (Socket cs, Socket ss) = await ConnectPairAsync();
        using var clientStream = new NetworkStream(cs, ownsSocket: true);
        using var serverStream = new NetworkStream(ss, ownsSocket: true);
        using var client = new SslStream(clientStream, leaveInnerStreamOpen: false);
        using TlsSession session = TlsSession.Create(_ctxBuffered);

        Task c = client.AuthenticateAsClientAsync(_clientOptions);
        Task s = DriveBufferedHandshakeAsync(session, serverStream);
        await Task.WhenAll(c, s);
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

        Task c = client.AuthenticateAsClientAsync(_clientOptions);
        Task s = Task.Run(() => DriveFdHandshake(session));
        await Task.WhenAll(c, s);

        // session owns ss.SafeHandle; ss itself becomes unusable, so no explicit close needed.
        GC.KeepAlive(ss);
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

    private static async Task DriveBufferedHandshakeAsync(TlsSession session, Stream transport)
    {
        byte[] netIn = ArrayPool<byte>.Shared.Rent(ScratchSize);
        byte[] netOut = ArrayPool<byte>.Shared.Rent(ScratchSize);
        int inUsed = 0;
        try
        {
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
                    await transport.WriteAsync(netOut.AsMemory(0, produced));
                }

                switch (status)
                {
                    case TlsOperationStatus.Complete:
                        continue;
                    case TlsOperationStatus.NeedsCertificateValidation:
                        session.AcceptWithDefaultValidation();
                        continue;
                    case TlsOperationStatus.WantWrite:
                        while (session.HasPendingOutput)
                        {
                            session.DrainPendingOutput(netOut, out int n);
                            if (n > 0)
                            {
                                await transport.WriteAsync(netOut.AsMemory(0, n));
                            }
                        }
                        continue;
                    case TlsOperationStatus.WantRead:
                        int r = await transport.ReadAsync(netIn.AsMemory(inUsed));
                        if (r == 0) throw new IOException("EOF in handshake.");
                        inUsed += r;
                        continue;
                    case TlsOperationStatus.Closed:
                        throw new IOException("Closed in handshake.");
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(netIn);
            ArrayPool<byte>.Shared.Return(netOut);
        }
    }

    private static void DriveFdHandshake(TlsSession session)
    {
        // fd-mode: OpenSSL drives the socket directly. We just spin on WantRead/WantWrite.
        // Non-blocking socket + level-triggered poll keeps the loop tight on loopback.
        SpinWait spin = default;
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
                case TlsOperationStatus.WantWrite:
                    spin.SpinOnce();
                    continue;
                default:
                    throw new IOException($"Unexpected handshake status: {s}");
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
