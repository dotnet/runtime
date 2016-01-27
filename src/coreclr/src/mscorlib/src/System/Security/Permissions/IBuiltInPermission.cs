// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions
{
    internal interface IBuiltInPermission
    {
        int GetTokenIndex();
    }

    internal static class BuiltInPermissionIndex
    {
        internal const int NUM_BUILTIN_UNRESTRICTED = 10;
        internal const int NUM_BUILTIN_NORMAL = 7;

        // Unrestricted permissions

        internal const int EnvironmentPermissionIndex = 0;
        internal const int FileDialogPermissionIndex = 1;
        internal const int FileIOPermissionIndex = 2;
        internal const int IsolatedStorageFilePermissionIndex = 3;
        internal const int ReflectionPermissionIndex = 4;
        internal const int RegistryPermissionIndex = 5;
        internal const int SecurityPermissionIndex = 6;
        internal const int UIPermissionIndex = 7;
        internal const int PrincipalPermissionIndex = 8;
        internal const int HostProtectionPermissionIndex = 9;

        // Normal permissions
        internal const int PublisherIdentityPermissionIndex = 0 + NUM_BUILTIN_UNRESTRICTED;
        internal const int SiteIdentityPermissionIndex = 1 + NUM_BUILTIN_UNRESTRICTED;
        internal const int StrongNameIdentityPermissionIndex = 2 + NUM_BUILTIN_UNRESTRICTED;
        internal const int UrlIdentityPermissionIndex = 3 + NUM_BUILTIN_UNRESTRICTED;
        internal const int ZoneIdentityPermissionIndex = 4 + NUM_BUILTIN_UNRESTRICTED;
        internal const int GacIdentityPermissionIndex = 5 + NUM_BUILTIN_UNRESTRICTED;
        internal const int KeyContainerPermissionIndex = 6 + NUM_BUILTIN_UNRESTRICTED;
    }

    [Serializable]
    internal enum BuiltInPermissionFlag
    {
        // Unrestricted permissions

        EnvironmentPermission = 0x1,
        FileDialogPermission = 0x2,
        FileIOPermission = 0x4,
        IsolatedStorageFilePermission = 0x8,
        ReflectionPermission = 0x10,
        RegistryPermission = 0x20,
        SecurityPermission = 0x40,
        UIPermission = 0x80,
        PrincipalPermission = 0x100,

        // Normal permissions
        PublisherIdentityPermission = 0x200,
        SiteIdentityPermission = 0x400,
        StrongNameIdentityPermission = 0x800,
        UrlIdentityPermission = 0x1000,
        ZoneIdentityPermission = 0x2000,
        KeyContainerPermission = 0x4000,
    }
}
