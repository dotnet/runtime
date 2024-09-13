// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_MemfdCreate", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
        internal static partial SafeFileHandle MemfdCreate(string name, int isReadonly);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_IsMemfdSupported", SetLastError = true)]
        private static partial int MemfdSupportedImpl();

        private static volatile sbyte s_memfdSupported;

        internal static bool IsMemfdSupported
        {
            get
            {
                sbyte memfdSupported = s_memfdSupported;
                if (memfdSupported == 0)
                {
                    Interlocked.CompareExchange(ref s_memfdSupported, (sbyte)(MemfdSupportedImpl() == 1 ? 1 : -1), 0);
                    memfdSupported = s_memfdSupported;
                }
                return memfdSupported > 0;
            }
        }
    }
}
