// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        internal delegate bool CtrlCallback(CtrlCode ctrlCode);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_RegisterForCtrl")]
        internal static extern void RegisterForCtrl(CtrlCallback handler);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_UnregisterForCtrl")]
        internal static extern void UnregisterForCtrl();
    }
}
