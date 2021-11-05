// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        [Flags]
        internal enum CertStoreFlags : int
        {
            CERT_STORE_NO_CRYPT_RELEASE_FLAG                = 0x00000001,
            CERT_STORE_SET_LOCALIZED_NAME_FLAG              = 0x00000002,
            CERT_STORE_DEFER_CLOSE_UNTIL_LAST_FREE_FLAG     = 0x00000004,
            CERT_STORE_DELETE_FLAG                          = 0x00000010,
            CERT_STORE_UNSAFE_PHYSICAL_FLAG                 = 0x00000020,
            CERT_STORE_SHARE_STORE_FLAG                     = 0x00000040,
            CERT_STORE_SHARE_CONTEXT_FLAG                   = 0x00000080,
            CERT_STORE_MANIFOLD_FLAG                        = 0x00000100,
            CERT_STORE_ENUM_ARCHIVED_FLAG                   = 0x00000200,
            CERT_STORE_UPDATE_KEYID_FLAG                    = 0x00000400,
            CERT_STORE_BACKUP_RESTORE_FLAG                  = 0x00000800,
            CERT_STORE_READONLY_FLAG                        = 0x00008000,
            CERT_STORE_OPEN_EXISTING_FLAG                   = 0x00004000,
            CERT_STORE_CREATE_NEW_FLAG                      = 0x00002000,
            CERT_STORE_MAXIMUM_ALLOWED_FLAG                 = 0x00001000,
            CERT_SYSTEM_STORE_CURRENT_USER                  = 0x00010000,
            CERT_SYSTEM_STORE_LOCAL_MACHINE                 = 0x00020000,
            None                                            = 0x00000000,
        }
    }
}
