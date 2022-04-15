// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.WebAssembly.Diagnostics
{
    internal class TcpClientConnection : AbstractConnection
    {
        public TcpClient TcpClient { get; init; }
        private readonly ILogger _logger;

        public TcpClientConnection(TcpClient tcpClient!!, ILogger logger!!)
        {
            TcpClient = tcpClient;
            _logger = logger;
        }

        public override DevToolsQueueBase NewQueue() => new DevToolsQueueFirefox(TcpClient);

        public override async Task<string> ReadOne(TaskCompletionSource client_initiated_close, CancellationToken token)
        {
#pragma warning disable CA1835 // Prefer the 'Memory'-based overloads for 'ReadAsync' and 'WriteAsync'
            try
            {
                while (true)
                {
                    byte[] buffer = new byte[1000000];
                    var stream = TcpClient.GetStream();
                    int bytesRead = 0;
                    while (bytesRead == 0 || Convert.ToChar(buffer[bytesRead - 1]) != ':')
                    {
                        var readLen = await stream.ReadAsync(buffer, bytesRead, 1, token);
                        bytesRead+=readLen;
                    }
                    var str = Encoding.UTF8.GetString(buffer, 0, bytesRead - 1);
                    int len = int.Parse(str);
                    bytesRead = await stream.ReadAsync(buffer, 0, len, token);
                    while (bytesRead != len)
                        bytesRead += await stream.ReadAsync(buffer, bytesRead, len - bytesRead, token);
                    str = Encoding.UTF8.GetString(buffer, 0, len);
                    return str;
                }
            }
            catch (Exception)
            {
                client_initiated_close.TrySetResult();
                return null;
            }
        }

        public override void Dispose()
        {
            TcpClient.Dispose();
            base.Dispose();
        }
    }
}
