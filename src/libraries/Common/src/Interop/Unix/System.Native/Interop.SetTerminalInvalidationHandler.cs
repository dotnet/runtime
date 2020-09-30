// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Sys
    {
        internal delegate void TerminalInvalidationCallback();

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_SetTerminalInvalidationHandler")]
        [SuppressGCTransition]
        internal static extern void SetTerminalInvalidationHandler(TerminalInvalidationCallback handler);
    }
}
