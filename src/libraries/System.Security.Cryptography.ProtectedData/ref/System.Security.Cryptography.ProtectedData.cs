// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Security.Cryptography
{
    public enum DataProtectionScope
    {
        CurrentUser = 0,
        LocalMachine = 1,
    }
    public static partial class ProtectedData
    {
        public static byte[] Protect(byte[] userData, byte[] optionalEntropy, System.Security.Cryptography.DataProtectionScope scope) { throw null; }
        public static byte[] Unprotect(byte[] encryptedData, byte[] optionalEntropy, System.Security.Cryptography.DataProtectionScope scope) { throw null; }
    }
}
