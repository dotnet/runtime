// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Kernel32
    {
        [DllImport(Libraries.Kernel32, EntryPoint = "CreateFile2", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern SafeFileHandle CreateFile2Private(
            string lpFileName,
            int dwDesiredAccess,
            FileShare dwShareMode,
            FileMode dwCreationDisposition,
            ref Kernel32.CREATEFILE2_EXTENDED_PARAMETERS pCreateExParams);

        internal static SafeFileHandle CreateFile2(
            string lpFileName,
            int dwDesiredAccess,
            FileShare dwShareMode,
            FileMode dwCreationDisposition,
            ref Kernel32.CREATEFILE2_EXTENDED_PARAMETERS pCreateExParams)
        {
            string? lpFileNameWithPrefix = PathInternal.EnsureExtendedPrefixIfNeeded(lpFileName);
            Debug.Assert(lpFileNameWithPrefix != null, "null not expected when non-null passed"); // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/26761
            return CreateFile2Private(lpFileNameWithPrefix, dwDesiredAccess, dwShareMode, dwCreationDisposition, ref pCreateExParams);
        }
    }
}
