// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        internal enum CtrlCode
        {
            Interrupt = 0,
            Break = 1
        }

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_RegisterForCtrl")]
        [SuppressGCTransition]
        internal static extern unsafe void RegisterForCtrl(delegate* unmanaged<CtrlCode, void> handler);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_UnregisterForCtrl")]
        [SuppressGCTransition]
        internal static extern void UnregisterForCtrl();
    }
}
