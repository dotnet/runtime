// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Secur32
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct NegotiateCallerNameRequest
        {
            public int messageType;
            public LUID logonId;
        }

        [LibraryImport(Libraries.Secur32)]
        internal static partial uint LsaCallAuthenticationPackage(
            LsaLogonProcessSafeHandle lsaHandle,
            int authenticationPackage,
            in NegotiateCallerNameRequest protocolSubmitBuffer,
            int submitBufferLength,
            out IntPtr protocolReturnBuffer,
            out int returnBufferLength,
            out uint protocolStatus);
    }
}
