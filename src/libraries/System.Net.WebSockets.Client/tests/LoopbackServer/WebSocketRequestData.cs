// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Net.Test.Common;

namespace System.Net.WebSockets.Client.Tests
{
    public class WebSocketRequestData
    {
        public Dictionary<string, string?> Headers { get; set; } = new Dictionary<string, string?>();
        public Stream? WebSocketStream { get; set; }

        public Version HttpVersion { get; set; }
        public LoopbackServer.Connection? Http11Connection { get; set; }
        public Http2LoopbackConnection? Http2Connection { get; set; }
        public int? Http2StreamId { get; set; }
    }
}
