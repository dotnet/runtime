// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        internal enum CertStoreAddDisposition : int
        {
            CERT_STORE_ADD_NEW                                  = 1,
            CERT_STORE_ADD_USE_EXISTING                         = 2,
            CERT_STORE_ADD_REPLACE_EXISTING                     = 3,
            CERT_STORE_ADD_ALWAYS                               = 4,
            CERT_STORE_ADD_REPLACE_EXISTING_INHERIT_PROPERTIES  = 5,
            CERT_STORE_ADD_NEWER                                = 6,
            CERT_STORE_ADD_NEWER_INHERIT_PROPERTIES             = 7,
        }
    }
}
