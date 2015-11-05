// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        [DllImport(Libraries.SystemNative, SetLastError = true)]
        internal static extern bool RegisterForCtrl(CtrlCallback handler);

        [DllImport(Libraries.SystemNative)]
        internal static extern void UnregisterForCtrl();
    }
}
