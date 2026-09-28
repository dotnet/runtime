// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct SECURITY_ATTRIBUTES
        {
            internal uint nLength;
            internal unsafe void* lpSecurityDescriptor;
            internal BOOL bInheritHandle;

            internal static unsafe SECURITY_ATTRIBUTES Create() =>
                new SECURITY_ATTRIBUTES
                {
                    nLength = (uint)sizeof(SECURITY_ATTRIBUTES)
                };

            internal static unsafe SECURITY_ATTRIBUTES Create(void* securityDescriptor) =>
                new SECURITY_ATTRIBUTES
                {
                    nLength = (uint)sizeof(SECURITY_ATTRIBUTES),
                    lpSecurityDescriptor = securityDescriptor
                };

            internal static unsafe SECURITY_ATTRIBUTES Create(bool inheritable) =>
                new SECURITY_ATTRIBUTES
                {
                    nLength = (uint)sizeof(SECURITY_ATTRIBUTES),
                    bInheritHandle = inheritable ? BOOL.TRUE : BOOL.FALSE
                };
        }
    }
}
