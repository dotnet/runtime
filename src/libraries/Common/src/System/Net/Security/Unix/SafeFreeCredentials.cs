// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Net.Security
{
    //
    // Implementation of handles dependable on FreeCredentialsHandle
    //
#if DEBUG
    internal abstract class SafeFreeCredentials : DebugSafeHandle
    {
#else
    internal abstract class SafeFreeCredentials : SafeHandle
    {
#endif
        internal DateTime _expiry;

        public DateTime Expiry => _expiry;

        protected SafeFreeCredentials(IntPtr handle, bool ownsHandle) : base(handle, ownsHandle)
        {
            _expiry = DateTime.MaxValue;
        }
    }

}
