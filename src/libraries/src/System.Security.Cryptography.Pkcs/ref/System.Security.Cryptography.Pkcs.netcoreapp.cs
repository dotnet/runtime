// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

using System.Security.Cryptography.X509Certificates;

namespace System.Security.Cryptography.Pkcs
{
    public sealed partial class Rfc3161TimestampRequest
    {
        private Rfc3161TimestampRequest() { }
        public int Version => throw null;
        public ReadOnlyMemory<byte> GetMessageHash() => throw null;
        public Oid HashAlgorithmId => throw null;
        public Oid RequestedPolicyId => throw null;
        public bool RequestSignerCertificate => throw null;
        public ReadOnlyMemory<byte>? GetNonce() => throw null;
        public bool HasExtensions => throw null;
        public X509ExtensionCollection GetExtensions() => throw null;
        public byte[] Encode() => throw null;
        public bool TryEncode(Span<byte> destination, out int bytesWritten) => throw null;
        public Rfc3161TimestampToken ProcessResponse(ReadOnlyMemory<byte> responseBytes, out int bytesConsumed) => throw null;
        public static Rfc3161TimestampRequest CreateFromData(ReadOnlySpan<byte> data, HashAlgorithmName hashAlgorithm, Oid requestedPolicyId = null, ReadOnlyMemory<byte>? nonce = null, bool requestSignerCertificates = false, X509ExtensionCollection extensions = null) => throw null;
        public static Rfc3161TimestampRequest CreateFromHash(ReadOnlyMemory<byte> hash, HashAlgorithmName hashAlgorithm, Oid requestedPolicyId = null, ReadOnlyMemory<byte>? nonce = null, bool requestSignerCertificates = false, X509ExtensionCollection extensions = null) => throw null;
        public static Rfc3161TimestampRequest CreateFromHash(ReadOnlyMemory<byte> hash, Oid hashAlgorithmId, Oid requestedPolicyId = null, ReadOnlyMemory<byte>? nonce = null, bool requestSignerCertificates = false, X509ExtensionCollection extensions = null) => throw null;
        public static Rfc3161TimestampRequest CreateFromSignerInfo(SignerInfo signerInfo, HashAlgorithmName hashAlgorithm, Oid requestedPolicyId = null, ReadOnlyMemory<byte>? nonce = null, bool requestSignerCertificates = false, X509ExtensionCollection extensions = null) => throw null;
        public static bool TryDecode(ReadOnlyMemory<byte> encodedBytes, out Rfc3161TimestampRequest request, out int bytesConsumed) => throw null;
    }
    public sealed partial class Rfc3161TimestampToken
    {
        private Rfc3161TimestampToken() { }
        public Rfc3161TimestampTokenInfo TokenInfo => throw null;
        public SignedCms AsSignedCms() => throw null;
        public bool VerifySignatureForHash(ReadOnlySpan<byte> hash, HashAlgorithmName hashAlgorithm, out X509Certificate2 signerCertificate, X509Certificate2Collection extraCandidates = null) => throw null;
        public bool VerifySignatureForHash(ReadOnlySpan<byte> hash, Oid hashAlgorithmId, out X509Certificate2 signerCertificate, X509Certificate2Collection extraCandidates = null) => throw null;
        public bool VerifySignatureForData(ReadOnlySpan<byte> data, out X509Certificate2 signerCertificate, X509Certificate2Collection extraCandidates = null) => throw null;
        public bool VerifySignatureForSignerInfo(SignerInfo signerInfo, out X509Certificate2 signerCertificate, X509Certificate2Collection extraCandidates = null) => throw null;
        public static bool TryDecode(ReadOnlyMemory<byte> encodedBytes, out Rfc3161TimestampToken token, out int bytesConsumed) => throw null;
    }
    public sealed partial class Rfc3161TimestampTokenInfo
    {
        public Rfc3161TimestampTokenInfo(Oid policyId, Oid hashAlgorithmId, ReadOnlyMemory<byte> messageHash, ReadOnlyMemory<byte> serialNumber, DateTimeOffset timestamp, long? accuracyInMicroseconds=null, bool isOrdering=false, ReadOnlyMemory<byte>? nonce=null, ReadOnlyMemory<byte>? timestampAuthorityName=null, X509ExtensionCollection extensions =null) { throw null; }
        public int Version => throw null;
        public Oid PolicyId=> throw null;
        public Oid HashAlgorithmId => throw null;
        public ReadOnlyMemory<byte> GetMessageHash() { throw null; }
        public ReadOnlyMemory<byte> GetSerialNumber() { throw null; }
        public DateTimeOffset Timestamp => throw null;
        public long? AccuracyInMicroseconds => throw null;
        public bool IsOrdering => throw null;
        public ReadOnlyMemory<byte>? GetNonce() { throw null; }
        public ReadOnlyMemory<byte>? GetTimestampAuthorityName() { throw null; }
        public bool HasExtensions => throw null;
        public X509ExtensionCollection GetExtensions() { throw null; }
        public byte[] Encode() => throw null;
        public bool TryEncode(Span<byte> destination, out int bytesWritten) => throw null;
        public static bool TryDecode(ReadOnlyMemory<byte> encodedBytes, out Rfc3161TimestampTokenInfo timestampTokenInfo, out int bytesConsumed) { throw null; }
    }
    public sealed partial class SignerInfo
    {
        public Oid SignatureAlgorithm => throw null;
        public byte[] GetSignature() => throw null;
    }
}
