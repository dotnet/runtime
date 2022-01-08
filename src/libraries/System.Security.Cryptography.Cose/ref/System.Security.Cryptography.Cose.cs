// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Security.Cryptography.Cose
{
    public readonly partial struct CoseHeaderLabel : System.IEquatable<System.Security.Cryptography.Cose.CoseHeaderLabel>
    {
        public static readonly System.Security.Cryptography.Cose.CoseHeaderLabel Algorithm;
        public static readonly System.Security.Cryptography.Cose.CoseHeaderLabel ContentType;
        public static readonly System.Security.Cryptography.Cose.CoseHeaderLabel CounterSignature;
        public static readonly System.Security.Cryptography.Cose.CoseHeaderLabel Critical;
        public static readonly System.Security.Cryptography.Cose.CoseHeaderLabel IV;
        public static readonly System.Security.Cryptography.Cose.CoseHeaderLabel KeyIdentifier;
        public static readonly System.Security.Cryptography.Cose.CoseHeaderLabel PartialIV;
        public CoseHeaderLabel(int label) { throw null; }
        public CoseHeaderLabel(string label) { throw null; }
        public bool Equals(System.Security.Cryptography.Cose.CoseHeaderLabel other) { throw null; }
    }
    public partial class CoseHeaderMap : System.Collections.Generic.IEnumerable<(System.Security.Cryptography.Cose.CoseHeaderLabel, System.ReadOnlyMemory<byte>)>, System.Collections.IEnumerable
    {
        public CoseHeaderMap() { }
        public bool IsReadOnly { get { throw null; } }
        public System.ReadOnlyMemory<byte> GetEncodedValue(System.Security.Cryptography.Cose.CoseHeaderLabel label) { throw null; }
        public System.Collections.Generic.IEnumerator<(System.Security.Cryptography.Cose.CoseHeaderLabel, System.ReadOnlyMemory<byte>)> GetEnumerator() { throw null; }
        public System.ReadOnlySpan<byte> GetValueAsBytes(System.Security.Cryptography.Cose.CoseHeaderLabel label) { throw null; }
        public int GetValueAsInt32(System.Security.Cryptography.Cose.CoseHeaderLabel label) { throw null; }
        public string GetValueAsString(System.Security.Cryptography.Cose.CoseHeaderLabel label) { throw null; }
        public void Remove(System.Security.Cryptography.Cose.CoseHeaderLabel label) { }
        public void SetEncodedValue(System.Security.Cryptography.Cose.CoseHeaderLabel label, System.ReadOnlyMemory<byte> encodedValue) { }
        public void SetValue(System.Security.Cryptography.Cose.CoseHeaderLabel label, int value) { }
        public void SetValue(System.Security.Cryptography.Cose.CoseHeaderLabel label, System.ReadOnlySpan<byte> value) { }
        public void SetValue(System.Security.Cryptography.Cose.CoseHeaderLabel label, string value) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public bool TryGetEncodedValue(System.Security.Cryptography.Cose.CoseHeaderLabel label, out System.ReadOnlyMemory<byte> encodedValue) { throw null; }
    }
    public abstract partial class CoseMessage
    {
        internal CoseMessage() { }
        public System.ReadOnlyMemory<byte>? Content { get { throw null; } }
        public System.Security.Cryptography.Cose.CoseHeaderMap ProtectedHeader { get { throw null; } }
        public System.Security.Cryptography.Cose.CoseHeaderMap UnprotectedHeader { get { throw null; } }
        public static System.Security.Cryptography.Cose.CoseSign1Message DecodeSign1(byte[] cborPayload) { throw null; }
    }
    public sealed partial class CoseSign1Message : System.Security.Cryptography.Cose.CoseMessage
    {
        internal CoseSign1Message() { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static byte[] Sign(byte[] content, System.Security.Cryptography.Cose.CoseHeaderMap protectedHeaders, System.Security.Cryptography.Cose.CoseHeaderMap unprotectedHeaders, System.Security.Cryptography.AsymmetricAlgorithm key, bool isDetached = false) { throw null; }
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
