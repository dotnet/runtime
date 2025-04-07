// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        internal const int SECURITY_DESCRIPTOR_MIN_LENGTH = // AlignUp(4, sizeof(nint)) + 4 * sizeof(nint)
#if TARGET_64BIT
            40;
#else
            20;
#endif

        internal const int SECURITY_DESCRIPTOR_REVISION = 1;

        [LibraryImport(Libraries.Advapi32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool InitializeSecurityDescriptor(nint pSecurityDescriptor, int dwRevision);

        [LibraryImport(Libraries.Advapi32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool SetSecurityDescriptorOwner(
            nint pSecurityDescriptor,
            nint pOwner,
            [MarshalAs(UnmanagedType.Bool)] bool bOwnerDefaulted);

        [LibraryImport(Libraries.Advapi32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool SetSecurityDescriptorGroup(
            nint pSecurityDescriptor,
            nint pGroup,
            [MarshalAs(UnmanagedType.Bool)] bool bGroupDefaulted);

        [LibraryImport(Libraries.Advapi32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool SetSecurityDescriptorDacl(
            nint pSecurityDescriptor,
            [MarshalAs(UnmanagedType.Bool)] bool bDaclPresent,
            nint pDacl,
            [MarshalAs(UnmanagedType.Bool)] bool bDaclDefaulted);

        [LibraryImport(Libraries.Advapi32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool SetSecurityDescriptorSacl(
            nint pSecurityDescriptor,
            [MarshalAs(UnmanagedType.Bool)] bool bSaclPresent,
            nint pSacl,
            [MarshalAs(UnmanagedType.Bool)] bool bSaclDefaulted);
    }
}
