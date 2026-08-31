// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace System.Net.WebSockets.Client.Tests
{
    public partial class ClientWebSocketTestBase
    {
        protected Task RunEchoAsync(Func<Uri, Task> clientFunc, bool useSsl)
            => throw new PlatformNotSupportedException();

        protected Task RunEchoHeadersAsync(Func<Uri, Task> clientFunc, bool useSsl)
            => throw new PlatformNotSupportedException();
    }
}
