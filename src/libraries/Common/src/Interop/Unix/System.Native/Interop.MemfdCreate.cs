// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_MemfdCreate", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
        internal static partial SafeFileHandle MemfdCreate(string name, int isReadonly);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_IsMemfdSupported", SetLastError = true)]
        private static partial int MemfdSupportedImpl();

        private static NullableBool s_memfdSupported;

        internal static bool IsMemfdSupported
        {
            get
            {
                NullableBool memfdSupported = s_memfdSupported;
                if (memfdSupported == NullableBool.Undefined)
                {
                    s_memfdSupported = memfdSupported = MemfdSupportedImpl() == 1 ? NullableBool.True : NullableBool.False;
                }
                return memfdSupported == NullableBool.True;
            }
        }
    }
}
