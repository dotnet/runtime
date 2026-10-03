// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Kerberos.NET.Entities;
using Kerberos.NET.Server;

namespace System.Net.Security.Kerberos;

class FakeKdcServer
{
    private readonly KdcServer _kdcServer;
    private readonly TcpListener _tcpListener;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _running;
    private readonly object _runningLock;

    public FakeKdcServer(KdcServerOptions serverOptions)
    {
        _kdcServer = new KdcServer(serverOptions);
        // Active Directory includes a PAC even when the client does not ask for one with
        // PA-PAC-REQUEST, which MIT Kerberos never sends. Force the same behavior here so that
        // principals configured with a PAC actually get one; principals without one are
        // unaffected because their IKerberosPrincipal.GeneratePac returns null.
        _kdcServer.RegisterMessageHandler(MessageType.KRB_AS_REQ, (message, options) => new PacAsReqMessageHandler(message, options));
        _kdcServer.RegisterMessageHandler(MessageType.KRB_TGS_REQ, (message, options) => new PacTgsReqMessageHandler(message, options));
        _tcpListener = new TcpListener(System.Net.IPAddress.Loopback, 0);
        _runningLock = new object();
    }

    private sealed class PacAsReqMessageHandler : KdcAsReqMessageHandler
    {
        public PacAsReqMessageHandler(ReadOnlyMemory<byte> message, KdcServerOptions options)
            : base(message, options)
        {
        }

        public override void ExecutePreValidate(PreAuthenticationContext context)
        {
            context.IncludePac ??= true;
            base.ExecutePreValidate(context);
        }
    }

    private sealed class PacTgsReqMessageHandler : KdcTgsReqMessageHandler
    {
        public PacTgsReqMessageHandler(ReadOnlyMemory<byte> message, KdcServerOptions options)
            : base(message, options)
        {
        }

        public override void ExecutePreValidate(PreAuthenticationContext context)
        {
            context.IncludePac ??= true;
            base.ExecutePreValidate(context);
        }
    }

    public Task<IPEndPoint> Start()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _running = true;
        _tcpListener.Start();

        var cancellationToken = _cancellationTokenSource.Token;
        Task.Run(async () => {
            try
            {
                byte[] sizeBuffer = new byte[4];
                do
                {
                    using var socket = await _tcpListener.AcceptSocketAsync(cancellationToken);
                    using var socketStream = new NetworkStream(socket);

                    await socketStream.ReadExactlyAsync(sizeBuffer, cancellationToken);
                    var messageSize = BinaryPrimitives.ReadInt32BigEndian(sizeBuffer);
                    var requestRented = ArrayPool<byte>.Shared.Rent(messageSize);
                    var request = requestRented.AsMemory(0, messageSize);
                    await socketStream.ReadExactlyAsync(request);                    
                    var response = await _kdcServer.ProcessMessage(request);
                    ArrayPool<byte>.Shared.Return(requestRented);
                    var responseLength = response.Length + 4;
                    var responseRented = ArrayPool<byte>.Shared.Rent(responseLength);
                    BinaryPrimitives.WriteInt32BigEndian(responseRented.AsSpan(0, 4), responseLength);
                    response.CopyTo(responseRented.AsMemory(4, responseLength));
                    await socketStream.WriteAsync(responseRented.AsMemory(0, responseLength + 4), cancellationToken);
                    ArrayPool<byte>.Shared.Return(responseRented);
                }
                while (!cancellationToken.IsCancellationRequested);
            }
            finally
            {
                lock (_runningLock)
                {
                    _running = false;
                    Monitor.Pulse(_runningLock);
                }
            }
        });
        return Task.FromResult((IPEndPoint)_tcpListener.LocalEndpoint);
    }

    public void Stop()
    {
        if (_running)
        {
            _cancellationTokenSource?.Cancel();
            lock (_runningLock)
            {
                while (_running)
                {
                    Monitor.Wait(_runningLock);
                }
            }
            _tcpListener.Stop();
        }
    }
}
