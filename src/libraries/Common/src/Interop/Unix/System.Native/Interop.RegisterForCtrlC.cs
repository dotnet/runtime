// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Sys
    {
        internal enum CtrlCode
        {
            Interrupt = 0,
            Break = 1
        }

        internal delegate void CtrlCallback(CtrlCode ctrlCode);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_RegisterForCtrl")]
        [SuppressGCTransition]
        internal static extern void RegisterForCtrl(CtrlCallback handler);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_UnregisterForCtrl")]
        [SuppressGCTransition]
        internal static extern void UnregisterForCtrl();
    }
}
