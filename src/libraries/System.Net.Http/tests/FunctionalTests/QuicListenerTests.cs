// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Quic.Tests
{
    [ConditionalClass(typeof(QuicConnection), nameof(QuicConnection.IsQuicSupported))]
    public class QuicListenerTests : MsQuicTestBase
    {
        [ActiveIssue("https://github.com/dotnet/runtime/issues/32048")]
        [Fact]
        public async Task Listener_Backlog_Success()
        {
            await Task.Run(async () =>
            {
                using QuicListener listener = CreateQuicListener();

                using QuicConnection clientConnection = CreateQuicConnection(listener.ListenEndPoint);
                await clientConnection.ConnectAsync();

                using QuicConnection serverConnection = await listener.AcceptConnectionAsync();
            }).TimeoutAfter(millisecondsTimeout: 5_000);
        }
    }
}
