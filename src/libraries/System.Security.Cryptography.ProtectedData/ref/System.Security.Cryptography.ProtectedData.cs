// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
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
        public static byte[] Protect(byte[] userData, byte[]? optionalEntropy, System.Security.Cryptography.DataProtectionScope scope) { throw null; }
#if NET
        public static byte[] Protect(System.ReadOnlySpan<byte> userData, System.Security.Cryptography.DataProtectionScope scope, System.ReadOnlySpan<byte> optionalEntropy = default(System.ReadOnlySpan<byte>)) { throw null; }
        public static int Protect(System.ReadOnlySpan<byte> userData, System.Security.Cryptography.DataProtectionScope scope, System.Span<byte> destination, System.ReadOnlySpan<byte> optionalEntropy = default(System.ReadOnlySpan<byte>)) { throw null; }
        public static bool TryProtect(System.ReadOnlySpan<byte> userData, System.Security.Cryptography.DataProtectionScope scope, System.Span<byte> destination, out int bytesWritten, System.ReadOnlySpan<byte> optionalEntropy = default(System.ReadOnlySpan<byte>)) { throw null; }
        public static bool TryUnprotect(System.ReadOnlySpan<byte> encryptedData, System.Security.Cryptography.DataProtectionScope scope, System.Span<byte> destination, out int bytesWritten, System.ReadOnlySpan<byte> optionalEntropy = default(System.ReadOnlySpan<byte>)) { throw null; }
#endif
        public static byte[] Unprotect(byte[] encryptedData, byte[]? optionalEntropy, System.Security.Cryptography.DataProtectionScope scope) { throw null; }
#if NET
        public static byte[] Unprotect(System.ReadOnlySpan<byte> encryptedData, System.Security.Cryptography.DataProtectionScope scope, System.ReadOnlySpan<byte> optionalEntropy = default(System.ReadOnlySpan<byte>)) { throw null; }
        public static int Unprotect(System.ReadOnlySpan<byte> encryptedData, System.Security.Cryptography.DataProtectionScope scope, System.Span<byte> destination, System.ReadOnlySpan<byte> optionalEntropy = default(System.ReadOnlySpan<byte>)) { throw null; }
#endif
    }
}
