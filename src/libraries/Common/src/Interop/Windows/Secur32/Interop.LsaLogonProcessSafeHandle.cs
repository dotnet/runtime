// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
#if NET
using System.Runtime.Versioning;
#endif
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
#if NET
    [SupportedOSPlatform("windows")]
#endif
    internal static partial class Secur32
    {
        internal sealed class LsaLogonProcessSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public LsaLogonProcessSafeHandle() : base(true) { }

            internal LsaLogonProcessSafeHandle(IntPtr value) : base(true)
            {
                SetHandle(value);
            }

            protected override bool ReleaseHandle() => LsaDeregisterLogonProcess(handle) == 0;
        }
    }
}
