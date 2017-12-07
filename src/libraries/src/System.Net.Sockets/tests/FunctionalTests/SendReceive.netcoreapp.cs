// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Sockets.Tests
{
    public sealed class SendReceiveSpanSync : SendReceive<SocketHelperSpanSync> { }
    public sealed class SendReceiveSpanSyncForceNonBlocking : SendReceive<SocketHelperSpanSyncForceNonBlocking> { }
    public sealed class SendReceiveMemoryArrayTask : SendReceive<SocketHelperMemoryArrayTask> { }
    public sealed class SendReceiveMemoryNativeTask : SendReceive<SocketHelperMemoryNativeTask> { }
}
