// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Security.Cryptography
{
    public sealed partial class SP800108HmacCounterKdf : System.IDisposable
    {
        public SP800108HmacCounterKdf(byte[] key, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { }
        public SP800108HmacCounterKdf(System.ReadOnlySpan<byte> key, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { }
        public static byte[] DeriveBytes(byte[] key, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, byte[] label, byte[] context, int derivedKeyLengthInBytes) { throw null; }
        public static byte[] DeriveBytes(byte[] key, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, string label, string context, int derivedKeyLengthInBytes) { throw null; }
        public static byte[] DeriveBytes(System.ReadOnlySpan<byte> key, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.ReadOnlySpan<byte> label, System.ReadOnlySpan<byte> context, int derivedKeyLengthInBytes) { throw null; }
        public static void DeriveBytes(System.ReadOnlySpan<byte> key, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.ReadOnlySpan<byte> label, System.ReadOnlySpan<byte> context, System.Span<byte> destination) { }
        public static byte[] DeriveBytes(System.ReadOnlySpan<byte> key, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.ReadOnlySpan<char> label, System.ReadOnlySpan<char> context, int derivedKeyLengthInBytes) { throw null; }
        public static void DeriveBytes(System.ReadOnlySpan<byte> key, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.ReadOnlySpan<char> label, System.ReadOnlySpan<char> context, System.Span<byte> destination) { }
        public byte[] DeriveKey(byte[] label, byte[] context, int derivedKeyLengthInBytes) { throw null; }
        public byte[] DeriveKey(System.ReadOnlySpan<byte> label, System.ReadOnlySpan<byte> context, int derivedKeyLengthInBytes) { throw null; }
        public void DeriveKey(System.ReadOnlySpan<byte> label, System.ReadOnlySpan<byte> context, System.Span<byte> destination) { }
        public byte[] DeriveKey(System.ReadOnlySpan<char> label, System.ReadOnlySpan<char> context, int derivedKeyLengthInBytes) { throw null; }
        public void DeriveKey(System.ReadOnlySpan<char> label, System.ReadOnlySpan<char> context, System.Span<byte> destination) { }
        public byte[] DeriveKey(string label, string context, int derivedKeyLengthInBytes) { throw null; }
        public void Dispose() { }
    }
}
