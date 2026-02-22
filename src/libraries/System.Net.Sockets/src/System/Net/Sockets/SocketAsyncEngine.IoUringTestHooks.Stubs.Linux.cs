// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Sockets
{
    internal sealed unsafe partial class SocketAsyncEngine
    {
        private bool TryConsumeDebugForcedSubmitError(out Interop.Error forcedError)
        {
            _ = _ioUringInitialized;
            forcedError = Interop.Error.SUCCESS;
            return false;
        }
    }
}
