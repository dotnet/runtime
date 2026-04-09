// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [LibraryImport(Interop.Libraries.Advapi32, EntryPoint = "GetSecurityInfo")]
        internal static unsafe partial uint GetSecurityInfoByHandle(
            SafeHandle handle,
            /*DWORD*/ uint objectType,
            /*DWORD*/ uint securityInformation,
            IntPtr* sidOwner,
            IntPtr* sidGroup,
            IntPtr* dacl,
            IntPtr* sacl,
            IntPtr* securityDescriptor);

        // Values for objectType
        internal enum SE_OBJECT_TYPE
        {
            SE_UNKNOWN_OBJECT_TYPE,
            SE_FILE_OBJECT,
            SE_SERVICE,
            SE_PRINTER,
            SE_REGISTRY_KEY,
            SE_LMSHARE,
            SE_KERNEL_OBJECT,
            SE_WINDOW_OBJECT,
            SE_DS_OBJECT,
            SE_DS_OBJECT_ALL,
            SE_PROVIDER_DEFINED_OBJECT,
            SE_WMIGUID_OBJECT,
            SE_REGISTRY_WOW64_32KEY,
            SE_REGISTRY_WOW64_64KEY
        }

        // Values for securityInformation
        internal const uint OWNER_SECURITY_INFORMATION = 0x1;
        internal const uint DACL_SECURITY_INFORMATION = 0x4;
    }
}
