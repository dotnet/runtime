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

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_LChflags", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
        internal static partial int LChflags(string path, uint flags);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_FChflags", SetLastError = true)]
        internal static partial int FChflags(SafeHandle fd, uint flags);

        internal static readonly bool CanSetHiddenFlag = (LChflagsCanSetHiddenFlag() != 0);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_LChflagsCanSetHiddenFlag")]
        [SuppressGCTransition]
        private static partial int LChflagsCanSetHiddenFlag();

        internal static readonly bool SupportsHiddenFlag = (CanGetHiddenFlag() != 0);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_CanGetHiddenFlag")]
        [SuppressGCTransition]
        private static partial int CanGetHiddenFlag();
    }
}
