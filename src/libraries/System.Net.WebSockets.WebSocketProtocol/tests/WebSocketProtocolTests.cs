// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Xunit;

namespace System.Net.WebSockets.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/38807", TestPlatforms.Browser)]
    public sealed class WebSocketProtocolCreateTests : WebSocketCreateTest
    {
        protected override WebSocket CreateFromStream(Stream stream, bool isServer, string subProtocol, TimeSpan keepAliveInterval) =>
            WebSocketProtocol.CreateFromStream(stream, isServer, subProtocol, keepAliveInterval);
    }
}
