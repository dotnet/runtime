// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Microsoft.DotNet.RemoteExecutor;

namespace System.Net.Sockets.Tests
{
    public class InlineContinuations
    {
        [OuterLoop]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.AnyUnix)] // Inline Socket mode is specific to Unix Socket implementation.
        public void InlineSocketContinuations()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.StartInfo.EnvironmentVariables.Add("DOTNET_SYSTEM_NET_SOCKETS_INLINE_COMPLETIONS", "1");
            options.TimeOut = (int)TimeSpan.FromMinutes(20).TotalMilliseconds;

            RemoteExecutor.Invoke(async () =>
            {
                // Connect/Accept tests
                await new AcceptEap(null).Accept_ConcurrentAcceptsBeforeConnects_Success(5);
                await new AcceptEap(null).Accept_ConcurrentAcceptsAfterConnects_Success(5);

                // Send/Receive tests
                await new SendReceive_Eap(null).SendRecv_Stream_TCP(IPAddress.Loopback, useMultipleBuffers: false);
                await new SendReceive_Eap(null).SendRecv_Stream_TCP_MultipleConcurrentReceives(IPAddress.Loopback, useMultipleBuffers: false);
                await new SendReceive_Eap(null).SendRecv_Stream_TCP_MultipleConcurrentSends(IPAddress.Loopback, useMultipleBuffers: false);
                await new SendReceive_Eap(null).TcpReceiveSendGetsCanceledByDispose(receiveOrSend: true, ipv6Server: false, dualModeClient: false, owning: true);
                await new SendReceive_Eap(null).TcpReceiveSendGetsCanceledByDispose(receiveOrSend: false, ipv6Server: false, dualModeClient: false, owning: true);
            }, options).Dispose();
        }
    }
}
