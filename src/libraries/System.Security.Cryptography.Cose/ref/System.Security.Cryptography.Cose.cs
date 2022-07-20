// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Security.Cryptography.Cose
{
    public readonly partial struct CoseHeaderLabel : System.IEquatable<System.Security.Cryptography.Cose.CoseHeaderLabel>
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public CoseHeaderLabel(int label) { throw null; }
        public CoseHeaderLabel(string label) { throw null; }
        public static System.Security.Cryptography.Cose.CoseHeaderLabel Algorithm { get { throw null; } }
        public static System.Security.Cryptography.Cose.CoseHeaderLabel ContentType { get { throw null; } }
        public static System.Security.Cryptography.Cose.CoseHeaderLabel CriticalHeaders { get { throw null; } }
        public static System.Security.Cryptography.Cose.CoseHeaderLabel KeyIdentifier { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public bool Equals(System.Security.Cryptography.Cose.CoseHeaderLabel other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Security.Cryptography.Cose.CoseHeaderLabel left, System.Security.Cryptography.Cose.CoseHeaderLabel right) { throw null; }
        public static bool operator !=(System.Security.Cryptography.Cose.CoseHeaderLabel left, System.Security.Cryptography.Cose.CoseHeaderLabel right) { throw null; }
    }
    public sealed partial class CoseHeaderMap : System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.Security.Cryptography.Cose.CoseHeaderLabel, System.Security.Cryptography.Cose.CoseHeaderValue>>, System.Collections.Generic.IDictionary<System.Security.Cryptography.Cose.CoseHeaderLabel, System.Security.Cryptography.Cose.CoseHeaderValue>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.Security.Cryptography.Cose.CoseHeaderLabel, System.Security.Cryptography.Cose.CoseHeaderValue>>, System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<System.Security.Cryptography.Cose.CoseHeaderLabel, System.Security.Cryptography.Cose.CoseHeaderValue>>, System.Collections.Generic.IReadOnlyDictionary<System.Security.Cryptography.Cose.CoseHeaderLabel, System.Security.Cryptography.Cose.CoseHeaderValue>, System.Collections.IEnumerable
    {
        public CoseHeaderMap() { }
        public int Count { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public System.Security.Cryptography.Cose.CoseHeaderValue this[System.Security.Cryptography.Cose.CoseHeaderLabel key] { get { throw null; } set { } }
        public System.Collections.Generic.ICollection<System.Security.Cryptography.Cose.CoseHeaderLabel> Keys { get { throw null; } }
        System.Collections.Generic.IEnumerable<System.Security.Cryptography.Cose.CoseHeaderLabel> System.Collections.Generic.IReadOnlyDictionary<System.Security.Cryptography.Cose.CoseHeaderLabel,System.Security.Cryptography.Cose.CoseHeaderValue>.Keys { get { throw null; } }
        System.Collections.Generic.IEnumerable<System.Security.Cryptography.Cose.CoseHeaderValue> System.Collections.Generic.IReadOnlyDictionary<System.Security.Cryptography.Cose.CoseHeaderLabel,System.Security.Cryptography.Cose.CoseHeaderValue>.Values { get { throw null; } }
        public System.Collections.Generic.ICollection<System.Security.Cryptography.Cose.CoseHeaderValue> Values { get { throw null; } }
        public void Add(System.Collections.Generic.KeyValuePair<System.Security.Cryptography.Cose.CoseHeaderLabel, System.Security.Cryptography.Cose.CoseHeaderValue> item) { }
        public void Add(System.Security.Cryptography.Cose.CoseHeaderLabel label, byte[] value) { }
        public void Add(System.Security.Cryptography.Cose.CoseHeaderLabel label, int value) { }
        public void Add(System.Security.Cryptography.Cose.CoseHeaderLabel label, System.ReadOnlySpan<byte> value) { }
        public void Add(System.Security.Cryptography.Cose.CoseHeaderLabel key, System.Security.Cryptography.Cose.CoseHeaderValue value) { }
        public void Add(System.Security.Cryptography.Cose.CoseHeaderLabel label, string value) { }
        public void Clear() { }
        public bool Contains(System.Collections.Generic.KeyValuePair<System.Security.Cryptography.Cose.CoseHeaderLabel, System.Security.Cryptography.Cose.CoseHeaderValue> item) { throw null; }
        public bool ContainsKey(System.Security.Cryptography.Cose.CoseHeaderLabel key) { throw null; }
        public void CopyTo(System.Collections.Generic.KeyValuePair<System.Security.Cryptography.Cose.CoseHeaderLabel, System.Security.Cryptography.Cose.CoseHeaderValue>[] array, int arrayIndex) { }
        public System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<System.Security.Cryptography.Cose.CoseHeaderLabel, System.Security.Cryptography.Cose.CoseHeaderValue>> GetEnumerator() { throw null; }
        public byte[] GetValueAsBytes(System.Security.Cryptography.Cose.CoseHeaderLabel label) { throw null; }
        public int GetValueAsBytes(System.Security.Cryptography.Cose.CoseHeaderLabel label, System.Span<byte> destination) { throw null; }
        public int GetValueAsInt32(System.Security.Cryptography.Cose.CoseHeaderLabel label) { throw null; }
        public string GetValueAsString(System.Security.Cryptography.Cose.CoseHeaderLabel label) { throw null; }
        public bool Remove(System.Collections.Generic.KeyValuePair<System.Security.Cryptography.Cose.CoseHeaderLabel, System.Security.Cryptography.Cose.CoseHeaderValue> item) { throw null; }
        public bool Remove(System.Security.Cryptography.Cose.CoseHeaderLabel label) { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public bool TryGetValue(System.Security.Cryptography.Cose.CoseHeaderLabel key, out System.Security.Cryptography.Cose.CoseHeaderValue value) { throw null; }
    }
    public readonly partial struct CoseHeaderValue : System.IEquatable<System.Security.Cryptography.Cose.CoseHeaderValue>
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public System.ReadOnlyMemory<byte> EncodedValue { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public bool Equals(System.Security.Cryptography.Cose.CoseHeaderValue other) { throw null; }
        public static System.Security.Cryptography.Cose.CoseHeaderValue FromBytes(byte[] value) { throw null; }
        public static System.Security.Cryptography.Cose.CoseHeaderValue FromBytes(System.ReadOnlySpan<byte> value) { throw null; }
        public static System.Security.Cryptography.Cose.CoseHeaderValue FromEncodedValue(byte[] encodedValue) { throw null; }
        public static System.Security.Cryptography.Cose.CoseHeaderValue FromEncodedValue(System.ReadOnlySpan<byte> encodedValue) { throw null; }
        public static System.Security.Cryptography.Cose.CoseHeaderValue FromInt32(int value) { throw null; }
        public static System.Security.Cryptography.Cose.CoseHeaderValue FromString(string value) { throw null; }
        public override int GetHashCode() { throw null; }
        public byte[] GetValueAsBytes() { throw null; }
        public int GetValueAsBytes(System.Span<byte> destination) { throw null; }
        public int GetValueAsInt32() { throw null; }
        public string GetValueAsString() { throw null; }
        public static bool operator ==(System.Security.Cryptography.Cose.CoseHeaderValue left, System.Security.Cryptography.Cose.CoseHeaderValue right) { throw null; }
        public static bool operator !=(System.Security.Cryptography.Cose.CoseHeaderValue left, System.Security.Cryptography.Cose.CoseHeaderValue right) { throw null; }
    }
    public abstract partial class CoseMessage
    {
        internal CoseMessage() { }
        public System.ReadOnlyMemory<byte>? Content { get { throw null; } }
        public System.Security.Cryptography.Cose.CoseHeaderMap ProtectedHeaders { get { throw null; } }
        public System.Security.Cryptography.Cose.CoseHeaderMap UnprotectedHeaders { get { throw null; } }
        public static System.Security.Cryptography.Cose.CoseMultiSignMessage DecodeMultiSign(byte[] cborPayload) { throw null; }
        public static System.Security.Cryptography.Cose.CoseMultiSignMessage DecodeMultiSign(System.ReadOnlySpan<byte> cborPayload) { throw null; }
        public static System.Security.Cryptography.Cose.CoseSign1Message DecodeSign1(byte[] cborPayload) { throw null; }
        public static System.Security.Cryptography.Cose.CoseSign1Message DecodeSign1(System.ReadOnlySpan<byte> cborPayload) { throw null; }
        public byte[] Encode() { throw null; }
        public int Encode(System.Span<byte> destination) { throw null; }
        public abstract int GetEncodedLength();
        public abstract bool TryEncode(System.Span<byte> destination, out int bytesWritten);
    }
    public sealed partial class CoseMultiSignMessage : System.Security.Cryptography.Cose.CoseMessage
    {
        internal CoseMultiSignMessage() { }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Security.Cryptography.Cose.CoseSignature> Signatures { get { throw null; } }
        public void AddSignatureForDetached(byte[] detachedContent, System.Security.Cryptography.Cose.CoseSigner signer, byte[]? associatedData = null) { }
        public void AddSignatureForDetached(System.IO.Stream detachedContent, System.Security.Cryptography.Cose.CoseSigner signer, System.ReadOnlySpan<byte> associatedData = default(System.ReadOnlySpan<byte>)) { }
        public void AddSignatureForDetached(System.ReadOnlySpan<byte> detachedContent, System.Security.Cryptography.Cose.CoseSigner signer, System.ReadOnlySpan<byte> associatedData = default(System.ReadOnlySpan<byte>)) { }
        public System.Threading.Tasks.Task AddSignatureForDetachedAsync(System.IO.Stream detachedContent, System.Security.Cryptography.Cose.CoseSigner signer, System.ReadOnlyMemory<byte> associatedData = default(System.ReadOnlyMemory<byte>), System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public void AddSignatureForEmbedded(System.Security.Cryptography.Cose.CoseSigner signer, byte[]? associatedData = null) { }
        public void AddSignatureForEmbedded(System.Security.Cryptography.Cose.CoseSigner signer, System.ReadOnlySpan<byte> associatedData) { }
        public override int GetEncodedLength() { throw null; }
        public void RemoveSignature(int index) { }
        public void RemoveSignature(System.Security.Cryptography.Cose.CoseSignature signature) { }
        public static byte[] SignDetached(byte[] detachedContent, System.Security.Cryptography.Cose.CoseSigner signer, System.Security.Cryptography.Cose.CoseHeaderMap? protectedHeaders = null, System.Security.Cryptography.Cose.CoseHeaderMap? unprotectedHeaders = null, byte[]? associatedData = null) { throw null; }
        public static byte[] SignDetached(System.IO.Stream detachedContent, System.Security.Cryptography.Cose.CoseSigner signer, System.Security.Cryptography.Cose.CoseHeaderMap? protectedHeaders = null, System.Security.Cryptography.Cose.CoseHeaderMap? unprotectedHeaders = null, System.ReadOnlySpan<byte> associatedData = default(System.ReadOnlySpan<byte>)) { throw null; }
        public static byte[] SignDetached(System.ReadOnlySpan<byte> detachedContent, System.Security.Cryptography.Cose.CoseSigner signer, System.Security.Cryptography.Cose.CoseHeaderMap? protectedHeaders = null, System.Security.Cryptography.Cose.CoseHeaderMap? unprotectedHeaders = null, System.ReadOnlySpan<byte> associatedData = default(System.ReadOnlySpan<byte>)) { throw null; }
        public static System.Threading.Tasks.Task<byte[]> SignDetachedAsync(System.IO.Stream detachedContent, System.Security.Cryptography.Cose.CoseSigner signer, System.Security.Cryptography.Cose.CoseHeaderMap? protectedHeaders = null, System.Security.Cryptography.Cose.CoseHeaderMap? unprotectedHeaders = null, System.ReadOnlyMemory<byte> associatedData = default(System.ReadOnlyMemory<byte>), System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static byte[] SignEmbedded(byte[] embeddedContent, System.Security.Cryptography.Cose.CoseSigner signer, System.Security.Cryptography.Cose.CoseHeaderMap? protectedHeaders = null, System.Security.Cryptography.Cose.CoseHeaderMap? unprotectedHeaders = null, byte[]? associatedData = null) { throw null; }
        public static byte[] SignEmbedded(System.ReadOnlySpan<byte> embeddedContent, System.Security.Cryptography.Cose.CoseSigner signer, System.Security.Cryptography.Cose.CoseHeaderMap? protectedHeaders = null, System.Security.Cryptography.Cose.CoseHeaderMap? unprotectedHeaders = null, System.ReadOnlySpan<byte> associatedData = default(System.ReadOnlySpan<byte>)) { throw null; }
        public override bool TryEncode(System.Span<byte> destination, out int bytesWritten) { throw null; }
        public static bool TrySignDetached(System.ReadOnlySpan<byte> detachedContent, System.Span<byte> destination, System.Security.Cryptography.Cose.CoseSigner signer, out int bytesWritten, System.Security.Cryptography.Cose.CoseHeaderMap? protectedHeaders = null, System.Security.Cryptography.Cose.CoseHeaderMap? unprotectedHeaders = null, System.ReadOnlySpan<byte> associatedData = default(System.ReadOnlySpan<byte>)) { throw null; }
        public static bool TrySignEmbedded(System.ReadOnlySpan<byte> embeddedContent, System.Span<byte> destination, System.Security.Cryptography.Cose.CoseSigner signer, out int bytesWritten, System.Security.Cryptography.Cose.CoseHeaderMap? protectedHeaders = null, System.Security.Cryptography.Cose.CoseHeaderMap? unprotectedHeaders = null, System.ReadOnlySpan<byte> associatedData = default(System.ReadOnlySpan<byte>)) { throw null; }
    }
    public sealed partial class CoseSign1Message : System.Security.Cryptography.Cose.CoseMessage
    {
        internal CoseSign1Message() { }
        public override int GetEncodedLength() { throw null; }
        public static byte[] SignDetached(byte[] detachedContent, System.Security.Cryptography.Cose.CoseSigner signer, byte[]? associatedData = null) { throw null; }
        public static byte[] SignDetached(System.IO.Stream detachedContent, System.Security.Cryptography.Cose.CoseSigner signer, System.ReadOnlySpan<byte> associatedData = default(System.ReadOnlySpan<byte>)) { throw null; }
        public static byte[] SignDetached(System.ReadOnlySpan<byte> detachedContent, System.Security.Cryptography.Cose.CoseSigner signer, System.ReadOnlySpan<byte> associatedData = default(System.ReadOnlySpan<byte>)) { throw null; }
        public static System.Threading.Tasks.Task<byte[]> SignDetachedAsync(System.IO.Stream detachedContent, System.Security.Cryptography.Cose.CoseSigner signer, System.ReadOnlyMemory<byte> associatedData = default(System.ReadOnlyMemory<byte>), System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static byte[] SignEmbedded(byte[] embeddedContent, System.Security.Cryptography.Cose.CoseSigner signer, byte[]? associatedData = null) { throw null; }
        public static byte[] SignEmbedded(System.ReadOnlySpan<byte> embeddedContent, System.Security.Cryptography.Cose.CoseSigner signer, System.ReadOnlySpan<byte> associatedData = default(System.ReadOnlySpan<byte>)) { throw null; }
        public override bool TryEncode(System.Span<byte> destination, out int bytesWritten) { throw null; }
        public static bool TrySignDetached(System.ReadOnlySpan<byte> detachedContent, System.Span<byte> destination, System.Security.Cryptography.Cose.CoseSigner signer, out int bytesWritten, System.ReadOnlySpan<byte> associatedData = default(System.ReadOnlySpan<byte>)) { throw null; }
        public static bool TrySignEmbedded(System.ReadOnlySpan<byte> embeddedContent, System.Span<byte> destination, System.Security.Cryptography.Cose.CoseSigner signer, out int bytesWritten, System.ReadOnlySpan<byte> associatedData = default(System.ReadOnlySpan<byte>)) { throw null; }
        public bool VerifyDetached(System.Security.Cryptography.AsymmetricAlgorithm key, byte[] detachedContent, byte[]? associatedData = null) { throw null; }
        public bool VerifyDetached(System.Security.Cryptography.AsymmetricAlgorithm key, System.IO.Stream detachedContent, System.ReadOnlySpan<byte> associatedData = default(System.ReadOnlySpan<byte>)) { throw null; }
        public bool VerifyDetached(System.Security.Cryptography.AsymmetricAlgorithm key, System.ReadOnlySpan<byte> detachedContent, System.ReadOnlySpan<byte> associatedData = default(System.ReadOnlySpan<byte>)) { throw null; }
        public System.Threading.Tasks.Task<bool> VerifyDetachedAsync(System.Security.Cryptography.AsymmetricAlgorithm key, System.IO.Stream detachedContent, System.ReadOnlyMemory<byte> associatedData = default(System.ReadOnlyMemory<byte>), System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public bool VerifyEmbedded(System.Security.Cryptography.AsymmetricAlgorithm key, byte[]? associatedData = null) { throw null; }
        public bool VerifyEmbedded(System.Security.Cryptography.AsymmetricAlgorithm key, System.ReadOnlySpan<byte> associatedData) { throw null; }
    }
    public sealed partial class CoseSignature
    {
        internal CoseSignature() { }
        public System.Security.Cryptography.Cose.CoseHeaderMap ProtectedHeaders { get { throw null; } }
        public System.Security.Cryptography.Cose.CoseHeaderMap UnprotectedHeaders { get { throw null; } }
        public bool VerifyDetached(System.Security.Cryptography.AsymmetricAlgorithm key, byte[] detachedContent, byte[]? associatedData = null) { throw null; }
        public bool VerifyDetached(System.Security.Cryptography.AsymmetricAlgorithm key, System.IO.Stream detachedContent, System.ReadOnlySpan<byte> associatedData = default(System.ReadOnlySpan<byte>)) { throw null; }
        public bool VerifyDetached(System.Security.Cryptography.AsymmetricAlgorithm key, System.ReadOnlySpan<byte> detachedContent, System.ReadOnlySpan<byte> associatedData = default(System.ReadOnlySpan<byte>)) { throw null; }
        public System.Threading.Tasks.Task<bool> VerifyDetachedAsync(System.Security.Cryptography.AsymmetricAlgorithm key, System.IO.Stream detachedContent, System.ReadOnlyMemory<byte> associatedData = default(System.ReadOnlyMemory<byte>), System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public bool VerifyEmbedded(System.Security.Cryptography.AsymmetricAlgorithm key, byte[]? associatedData = null) { throw null; }
        public bool VerifyEmbedded(System.Security.Cryptography.AsymmetricAlgorithm key, System.ReadOnlySpan<byte> associatedData) { throw null; }
    }
    public sealed partial class CoseSigner
    {
        public CoseSigner(System.Security.Cryptography.AsymmetricAlgorithm key, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.Cose.CoseHeaderMap? protectedHeaders = null, System.Security.Cryptography.Cose.CoseHeaderMap? unprotectedHeaders = null) { }
        public CoseSigner(System.Security.Cryptography.RSA key, System.Security.Cryptography.RSASignaturePadding signaturePadding, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.Cose.CoseHeaderMap? protectedHeaders = null, System.Security.Cryptography.Cose.CoseHeaderMap? unprotectedHeaders = null) { }
        public System.Security.Cryptography.HashAlgorithmName HashAlgorithm { get { throw null; } }
        public System.Security.Cryptography.AsymmetricAlgorithm Key { get { throw null; } }
        public System.Security.Cryptography.Cose.CoseHeaderMap ProtectedHeaders { get { throw null; } }
        public System.Security.Cryptography.RSASignaturePadding? RSASignaturePadding { get { throw null; } }
        public System.Security.Cryptography.Cose.CoseHeaderMap UnprotectedHeaders { get { throw null; } }
    }
}
