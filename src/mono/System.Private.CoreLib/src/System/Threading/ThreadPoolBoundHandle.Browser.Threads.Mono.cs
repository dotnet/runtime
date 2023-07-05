// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Threading
{
    public sealed partial class ThreadPoolBoundHandle : IDisposable
    {
#pragma warning disable IDE0060
        private static ThreadPoolBoundHandle BindHandleCore(SafeHandle handle)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_OverlappedIO);
        }
#pragma warning restore IDE0060
    }
}
