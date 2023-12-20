// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        internal enum CertStoreSaveAs : int
        {
            CERT_STORE_SAVE_AS_STORE = 1,
            CERT_STORE_SAVE_AS_PKCS7 = 2,
        }
    }
}
