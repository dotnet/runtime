// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

#nullable enable
namespace Microsoft.WebAssembly.Diagnostics;

internal class FirefoxDebuggerConnection : WasmDebuggerConnection
{
    public TcpClient TcpClient { get; init; }
    private readonly ILogger _logger;
    private bool _isDisposed;
    private readonly byte[] _lengthBuffer;

    public FirefoxDebuggerConnection(TcpClient tcpClient!!, string id, ILogger logger!!)
            : base(id)
    {
        TcpClient = tcpClient;
        _logger = logger;
        _lengthBuffer = new byte[10];
    }

    public override async Task<string?> ReadOne(TaskCompletionSource client_initiated_close,
                                                TaskCompletionSource<Exception> side_exception,
                                                CancellationToken token)
    {
#pragma warning disable CA1835 // Prefer the 'Memory'-based overloads for 'ReadAsync' and 'WriteAsync'
        try
        {
            NetworkStream? stream = TcpClient.GetStream();
            int bytesRead = 0;
            while (bytesRead == 0 || Convert.ToChar(_lengthBuffer[bytesRead - 1]) != ':')
            {
                if (ShouldFail())
                    return null;

                int readLen = await stream.ReadAsync(_lengthBuffer, bytesRead, 1, token);
                bytesRead += readLen;
            }

            string str = Encoding.UTF8.GetString(_lengthBuffer, 0, bytesRead - 1);
            int offset = bytesRead;
            if (!int.TryParse(str, out int messageLen))
                throw new Exception($"Protocol error: Could not parse length prefix: '{str}'");

            if (ShouldFail())
                return null;

            byte[] buffer = new byte[messageLen];
            bytesRead = await stream.ReadAsync(buffer, 0, messageLen, token);
            while (bytesRead != messageLen)
            {
                if (ShouldFail())
                    return null;
                bytesRead += await stream.ReadAsync(buffer, bytesRead, messageLen - bytesRead, token);
            }

            return Encoding.UTF8.GetString(buffer, 0, messageLen);

            bool ShouldFail()
            {
                if (token.IsCancellationRequested)
                {
                    return true;
                }

                if (!TcpClient.Connected)
                {
                    client_initiated_close.TrySetResult();
                    return true;
                }

                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"FirefoxConnection.ReadOne: ({this}) {ex}, token: {token.IsCancellationRequested}");
            if (!token.IsCancellationRequested)
            {
                side_exception.TrySetResult(ex);
                throw;
            }
            return null;
        }
    }

    public override Task SendAsync(byte[] bytes, CancellationToken token)
    {
        byte[]? bytesWithHeader = Encoding.UTF8.GetBytes($"{bytes.Length}:").Concat(bytes).ToArray();
        NetworkStream toStream = TcpClient.GetStream();
        return toStream.WriteAsync(bytesWithHeader, token).AsTask();
    }

    public override Task ShutdownAsync(CancellationToken cancellationToken)
    {
        TcpClient.Close();
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        if (_isDisposed)
            return;

        try
        {
            TcpClient.Close();
            base.Dispose();

            _isDisposed = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to dispose {this}: {ex}");
            throw;
        }
    }

    public override string ToString() => $"[ {Id} connection ]";
}
