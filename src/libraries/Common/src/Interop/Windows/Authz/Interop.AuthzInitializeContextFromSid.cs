// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Authz
    {
        internal const int AUTHZ_RM_FLAG_NO_AUDIT = 0x1;
        internal const int AUTHZ_RM_FLAG_INITIALIZE_UNDER_IMPERSONATION = 0x2;
        internal const int AUTHZ_VALID_RM_INIT_FLAGS = (AUTHZ_RM_FLAG_NO_AUDIT | AUTHZ_RM_FLAG_INITIALIZE_UNDER_IMPERSONATION);

        [LibraryImport(Interop.Libraries.Authz, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool AuthzInitializeContextFromSid(
            int Flags,
            IntPtr UserSid,
            IntPtr AuthzResourceManager,
            IntPtr pExpirationTime,
            Interop.LUID Identitifier,
            IntPtr DynamicGroupArgs,
            out IntPtr pAuthzClientContext);

        [LibraryImport(Interop.Libraries.Authz)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool AuthzFreeContext(IntPtr AuthzClientContext);
    }
}
