// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.Net.Security
{
#if DEBUG
    internal sealed class SecurityContextTokenHandle : DebugSafeHandleZeroOrMinusOneIsInvalid
    {
#else
    internal sealed class SecurityContextTokenHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
#endif
        private int _disposed;

        public SecurityContextTokenHandle() : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            if (!IsInvalid)
            {
                if (Interlocked.Increment(ref _disposed) == 1)
                {
                    return Interop.Kernel32.CloseHandle(handle);
                }
            }
            return true;
        }
    }
}
