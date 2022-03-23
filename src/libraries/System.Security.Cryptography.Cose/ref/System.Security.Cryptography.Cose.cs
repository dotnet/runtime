// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Security.Cryptography.Cose
{
    public readonly partial struct CoseHeaderLabel : System.IEquatable<System.Security.Cryptography.Cose.CoseHeaderLabel>
    {
        public CoseHeaderLabel(int label) { throw null; }
        public CoseHeaderLabel(string label) { throw null; }
        public static System.Security.Cryptography.Cose.CoseHeaderLabel Algorithm { get { throw null; } }
        public static System.Security.Cryptography.Cose.CoseHeaderLabel ContentType { get { throw null; } }
        public static System.Security.Cryptography.Cose.CoseHeaderLabel CounterSignature { get { throw null; } }
        public static System.Security.Cryptography.Cose.CoseHeaderLabel Critical { get { throw null; } }
        public static System.Security.Cryptography.Cose.CoseHeaderLabel IV { get { throw null; } }
        public static System.Security.Cryptography.Cose.CoseHeaderLabel KeyIdentifier { get { throw null; } }
        public static System.Security.Cryptography.Cose.CoseHeaderLabel PartialIV { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public bool Equals(System.Security.Cryptography.Cose.CoseHeaderLabel other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Security.Cryptography.Cose.CoseHeaderLabel left, System.Security.Cryptography.Cose.CoseHeaderLabel right) { throw null; }
        public static bool operator !=(System.Security.Cryptography.Cose.CoseHeaderLabel left, System.Security.Cryptography.Cose.CoseHeaderLabel right) { throw null; }
    }
    public sealed partial class CoseHeaderMap : System.Collections.Generic.IEnumerable<(System.Security.Cryptography.Cose.CoseHeaderLabel Label, System.ReadOnlyMemory<byte> EncodedValue)>, System.Collections.IEnumerable
    {
        public CoseHeaderMap() { }
        public bool IsReadOnly { get { throw null; } }
        public System.ReadOnlyMemory<byte> GetEncodedValue(System.Security.Cryptography.Cose.CoseHeaderLabel label) { throw null; }
        public System.Security.Cryptography.Cose.CoseHeaderMap.Enumerator GetEnumerator() { throw null; }
        public System.ReadOnlySpan<byte> GetValueAsBytes(System.Security.Cryptography.Cose.CoseHeaderLabel label) { throw null; }
        public int GetValueAsInt32(System.Security.Cryptography.Cose.CoseHeaderLabel label) { throw null; }
        public string GetValueAsString(System.Security.Cryptography.Cose.CoseHeaderLabel label) { throw null; }
        public void Remove(System.Security.Cryptography.Cose.CoseHeaderLabel label) { }
        public void SetEncodedValue(System.Security.Cryptography.Cose.CoseHeaderLabel label, System.ReadOnlySpan<byte> encodedValue) { }
        public void SetValue(System.Security.Cryptography.Cose.CoseHeaderLabel label, int value) { }
        public void SetValue(System.Security.Cryptography.Cose.CoseHeaderLabel label, System.ReadOnlySpan<byte> value) { }
        public void SetValue(System.Security.Cryptography.Cose.CoseHeaderLabel label, string value) { }
        System.Collections.Generic.IEnumerator<(System.Security.Cryptography.Cose.CoseHeaderLabel Label, System.ReadOnlyMemory<byte> EncodedValue)> System.Collections.Generic.IEnumerable<(System.Security.Cryptography.Cose.CoseHeaderLabel Label, System.ReadOnlyMemory<System.Byte> EncodedValue)>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public bool TryGetEncodedValue(System.Security.Cryptography.Cose.CoseHeaderLabel label, out System.ReadOnlyMemory<byte> encodedValue) { throw null; }
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<(System.Security.Cryptography.Cose.CoseHeaderLabel Label, System.ReadOnlyMemory<byte> EncodedValue)>, System.Collections.IEnumerator, System.IDisposable
        {
            public readonly (System.Security.Cryptography.Cose.CoseHeaderLabel Label, System.ReadOnlyMemory<byte> EncodedValue) Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public void Dispose() { }
            public bool MoveNext() { throw null; }
            public void Reset() { }
        }
    }
    public abstract partial class CoseMessage
    {
        internal CoseMessage() { }
        public System.ReadOnlyMemory<byte>? Content { get { throw null; } }
        public System.Security.Cryptography.Cose.CoseHeaderMap ProtectedHeaders { get { throw null; } }
        public System.Security.Cryptography.Cose.CoseHeaderMap UnprotectedHeaders { get { throw null; } }
        public static System.Security.Cryptography.Cose.CoseSign1Message DecodeSign1(byte[] cborPayload) { throw null; }
    }
    public sealed partial class CoseSign1Message : System.Security.Cryptography.Cose.CoseMessage
    {
        internal CoseSign1Message() { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static byte[] Sign(byte[] content, System.Security.Cryptography.AsymmetricAlgorithm key, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.Cose.CoseHeaderMap? protectedHeaders = null, System.Security.Cryptography.Cose.CoseHeaderMap? unprotectedHeaders = null, bool isDetached = false) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static byte[] Sign(byte[] content, System.Security.Cryptography.ECDsa key, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, bool isDetached = false) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static byte[] Sign(byte[] content, System.Security.Cryptography.RSA key, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, bool isDetached = false) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public bool Verify(System.Security.Cryptography.ECDsa key) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public bool Verify(System.Security.Cryptography.ECDsa key, System.ReadOnlySpan<byte> content) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public bool Verify(System.Security.Cryptography.RSA key) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public bool Verify(System.Security.Cryptography.RSA key, System.ReadOnlySpan<byte> content) { throw null; }
    }
}
