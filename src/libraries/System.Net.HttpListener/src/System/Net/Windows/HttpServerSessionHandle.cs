// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Threading;

namespace System.Net
{
    //
    // This class is a wrapper for Http.sys V2 server session. CreateServerSession returns an ID and not a real handle
    // but we use CriticalHandle because it provides us the guarantee that CloseServerSession will always get called.
    //
    internal sealed class HttpServerSessionHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private readonly ulong _serverSessionId;

        internal HttpServerSessionHandle(ulong id) : base(true)
        {
            _serverSessionId = id;

            // This class uses no real handle so we need to set a dummy handle. Otherwise, IsInvalid always remains
            // true.
            SetHandle(new IntPtr(1));
        }

        internal ulong DangerousGetServerSessionId()
        {
            return _serverSessionId;
        }

        protected override bool ReleaseHandle()
        {
            // Closing server session also closes all open url groups under that server session.
            return (Interop.HttpApi.HttpCloseServerSession(_serverSessionId) ==
                Interop.HttpApi.ERROR_SUCCESS);
        }
    }
}
