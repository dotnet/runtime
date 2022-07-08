// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Kerberos.NET.Server;

namespace System.Net.Security.Kerberos;

class FakeKdcServer
{
    private readonly KdcServer kdcServer;
    private readonly TcpListener tcpListener;
    private CancellationTokenSource? cancellationTokenSource;
    private bool running;
    private readonly object runningLock;

    public FakeKdcServer(KdcServerOptions serverOptions)
    {
        kdcServer = new KdcServer(serverOptions);
        tcpListener = new TcpListener(System.Net.IPAddress.Loopback, 0);
        runningLock = new object();
    }

    public Task<IPEndPoint> Start()
    {
        cancellationTokenSource = new CancellationTokenSource();
        running = true;
        tcpListener.Start();

        var cancellationToken = cancellationTokenSource.Token;
        Task.Run(async () => {
            try
            {
                byte[] sizeBuffer = new byte[4];
                do
                {
                    using var socket = await tcpListener.AcceptSocketAsync(cancellationToken);
                    using var socketStream = new NetworkStream(socket);

                    await socketStream.ReadExactlyAsync(sizeBuffer, cancellationToken);
                    var messageSize = BinaryPrimitives.ReadInt32BigEndian(sizeBuffer);
                    var requestRented = ArrayPool<byte>.Shared.Rent(messageSize);
                    var request = requestRented.AsMemory(0, messageSize);
                    await socketStream.ReadExactlyAsync(request);                    
                    var response = await kdcServer.ProcessMessage(request);
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
                lock (runningLock)
                {
                    running = false;
                    Monitor.Pulse(runningLock);
                }
            }
        });
        return Task.FromResult((IPEndPoint)tcpListener.LocalEndpoint);
    }

    public void Stop()
    {
        if (running)
        {
            cancellationTokenSource?.Cancel();
            lock (runningLock)
            {
                while (running)
                {
                    Monitor.Wait(runningLock);
                }
            }
            tcpListener.Stop();
        }
    }
}
