// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Sockets
{
    partial class TcpClient
    {
        private void ConnectCore(string host, int port)
        {
            StartConnectCore(host, port);

            try
            {
                // Since Socket.Connect(host, port) won't work, get the addresses manually,
                // and then delegate to Connect(IPAddress[], int).
                IPAddress[] addresses = Dns.GetHostAddresses(host);

                Task t = ConnectCorePrivate(addresses, port, (s, a, p) => { s.Connect(a, p); return Task.CompletedTask; });

                Debug.Assert(t.IsCompleted);
                t.GetAwaiter().GetResult();
            }
            finally
            {
                ExitClientLock();
            }
        }

        private void ConnectCore(IPAddress[] addresses, int port)
        {
            StartConnectCore(addresses, port);
            Task t = ConnectCorePrivate(addresses, port, (s, a, p) => { s.Connect(a, p); return Task.CompletedTask; });

            Debug.Assert(t.IsCompleted);
            t.GetAwaiter().GetResult();
        }
    }
}
