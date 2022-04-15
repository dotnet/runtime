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

internal class TcpClientConnection : AbstractConnection
{
    public TcpClient TcpClient { get; init; }
    private readonly ILogger _logger;

    public TcpClientConnection(TcpClient tcpClient!!, ILogger logger!!)
    {
        TcpClient = tcpClient;
        _logger = logger;
    }

    // FIXME: client_initiated_close not really being used in case of dtclient
    public override async Task<string?> ReadOne(TaskCompletionSource client_initiated_close, CancellationToken token)
    {
#pragma warning disable CA1835 // Prefer the 'Memory'-based overloads for 'ReadAsync' and 'WriteAsync'
        try
        {
            byte[] buffer = new byte[1000000];
            NetworkStream? stream = TcpClient.GetStream();
            int bytesRead = 0;
            while (bytesRead == 0 || Convert.ToChar(buffer[bytesRead - 1]) != ':')
            {
                int readLen = await stream.ReadAsync(buffer, bytesRead, 1, token);
                bytesRead += readLen;
            }

            string str = Encoding.UTF8.GetString(buffer, 0, bytesRead - 1);
            int offset = bytesRead;
            int len = int.Parse(str);

            bytesRead = await stream.ReadAsync(buffer, 0, len, token);
            while (bytesRead != len)
                bytesRead += await stream.ReadAsync(buffer, bytesRead, len - bytesRead, token);

            return Encoding.UTF8.GetString(buffer, 0, len);
        }
        catch (Exception ex)
        {
            _logger.LogError($"TcpClientConnection.ReadOne: {ex}");
            // umm.. should set this only when it was a clean connection closed?
            // client_initiated_close.TrySetResult();
            // return null;
            if (!token.IsCancellationRequested)
                throw;
            return null;
        }
    }

    public override Task SendAsync(byte[] bytes, CancellationToken token)
    {
        byte[]? bytesWithHeader = Encoding.UTF8.GetBytes($"{bytes.Length}:").Concat(bytes).ToArray();
        NetworkStream toStream = TcpClient.GetStream();
        return toStream.WriteAsync(bytesWithHeader, token).AsTask();
    }

    public override Task Shutdown(CancellationToken cancellationToken)
    {
        TcpClient.Close();
        TcpClient.Dispose();
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        TcpClient.Dispose();
        base.Dispose();
    }
}
