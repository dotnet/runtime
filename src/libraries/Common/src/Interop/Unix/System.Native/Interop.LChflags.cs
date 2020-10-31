// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [Flags]
        internal enum UserFlags : uint
        {
            UF_HIDDEN = 0x8000
        }

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_LChflags", SetLastError = true)]
        internal static extern int LChflags(string path, uint flags);

        internal static readonly bool CanSetHiddenFlag = (LChflagsCanSetHiddenFlag() != 0);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_LChflagsCanSetHiddenFlag")]
        [SuppressGCTransition]
        private static extern int LChflagsCanSetHiddenFlag();
    }
}
