// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

internal static partial class Interop
{
    internal class LuidOptions
    {
        // Access Control library.
        internal const uint ANONYMOUS_LOGON_LUID = 0x3e6;
    }

    internal class SecurityIdentifier
    {
        internal const int SECURITY_ANONYMOUS_LOGON_RID = 0x00000007;
        internal const int SECURITY_AUTHENTICATED_USER_RID = 0x0000000B;
        internal const int SECURITY_LOCAL_SYSTEM_RID = 0x00000012;
        internal const int SECURITY_BUILTIN_DOMAIN_RID = 0x00000020;
    }

    internal class SecurityGroups
    {
        internal const uint SE_GROUP_MANDATORY = 0x00000001;
        internal const uint SE_GROUP_ENABLED_BY_DEFAULT = 0x00000002;
        internal const uint SE_GROUP_ENABLED = 0x00000004;
        internal const uint SE_GROUP_OWNER = 0x00000008;
        internal const uint SE_GROUP_USE_FOR_DENY_ONLY = 0x00000010;
        internal const uint SE_GROUP_LOGON_ID = 0xC0000000;
        internal const uint SE_GROUP_RESOURCE = 0x20000000;
    }

    internal class DuplicateHandleOptions
    {
        internal const uint DUPLICATE_CLOSE_SOURCE = 0x00000001;
        internal const uint DUPLICATE_SAME_ACCESS = 0x00000002;
        internal const uint DUPLICATE_SAME_ATTRIBUTES = 0x00000004;
    }
}
