// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

#if NETFRAMEWORK || NETSTANDARD
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
#endif
#if NETFRAMEWORK || NETSTANDARD || NET8_0
namespace System.Security.Cryptography.X509Certificates
{
    public sealed partial class Pkcs12LoaderLimits
    {
        public Pkcs12LoaderLimits() { }
        public Pkcs12LoaderLimits(System.Security.Cryptography.X509Certificates.Pkcs12LoaderLimits copyFrom) { }
        public static System.Security.Cryptography.X509Certificates.Pkcs12LoaderLimits DangerousNoLimits { get { throw null; } }
        public static System.Security.Cryptography.X509Certificates.Pkcs12LoaderLimits Defaults { get { throw null; } }
        public bool IgnoreEncryptedAuthSafes { get { throw null; } set { } }
        public bool IgnorePrivateKeys { get { throw null; } set { } }
        public int? IndividualKdfIterationLimit { get { throw null; } set { } }
        public bool IsReadOnly { get { throw null; } }
        public int? MacIterationLimit { get { throw null; } set { } }
        public int? MaxCertificates { get { throw null; } set { } }
        public int? MaxKeys { get { throw null; } set { } }
        public bool PreserveCertificateAlias { get { throw null; } set { } }
        public bool PreserveKeyName { get { throw null; } set { } }
        public bool PreserveStorageProvider { get { throw null; } set { } }
        public bool PreserveUnknownAttributes { get { throw null; } set { } }
        public int? TotalKdfIterationLimit { get { throw null; } set { } }
        public void MakeReadOnly() { }
    }
    public sealed partial class Pkcs12LoadLimitExceededException : System.Security.Cryptography.CryptographicException
    {
        public Pkcs12LoadLimitExceededException(string propertyName) { }
    }
#if NET
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
#endif
    public static partial class X509CertificateLoader
    {
        public static System.Security.Cryptography.X509Certificates.X509Certificate2 LoadCertificate(byte[] data) { throw null; }
        public static System.Security.Cryptography.X509Certificates.X509Certificate2 LoadCertificate(System.ReadOnlySpan<byte> data) { throw null; }
        public static System.Security.Cryptography.X509Certificates.X509Certificate2 LoadCertificateFromFile(string path) { throw null; }
        public static System.Security.Cryptography.X509Certificates.X509Certificate2 LoadPkcs12(byte[] data, string? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags = System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.DefaultKeySet, System.Security.Cryptography.X509Certificates.Pkcs12LoaderLimits? loaderLimits = null) { throw null; }
        public static System.Security.Cryptography.X509Certificates.X509Certificate2 LoadPkcs12(System.ReadOnlySpan<byte> data, System.ReadOnlySpan<char> password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags = System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.DefaultKeySet, System.Security.Cryptography.X509Certificates.Pkcs12LoaderLimits? loaderLimits = null) { throw null; }
        public static System.Security.Cryptography.X509Certificates.X509Certificate2Collection LoadPkcs12Collection(byte[] data, string? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags = System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.DefaultKeySet, System.Security.Cryptography.X509Certificates.Pkcs12LoaderLimits? loaderLimits = null) { throw null; }
        public static System.Security.Cryptography.X509Certificates.X509Certificate2Collection LoadPkcs12Collection(System.ReadOnlySpan<byte> data, System.ReadOnlySpan<char> password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags = System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.DefaultKeySet, System.Security.Cryptography.X509Certificates.Pkcs12LoaderLimits? loaderLimits = null) { throw null; }
        public static System.Security.Cryptography.X509Certificates.X509Certificate2Collection LoadPkcs12CollectionFromFile(string path, System.ReadOnlySpan<char> password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags = System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.DefaultKeySet, System.Security.Cryptography.X509Certificates.Pkcs12LoaderLimits? loaderLimits = null) { throw null; }
        public static System.Security.Cryptography.X509Certificates.X509Certificate2Collection LoadPkcs12CollectionFromFile(string path, string? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags = System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.DefaultKeySet, System.Security.Cryptography.X509Certificates.Pkcs12LoaderLimits? loaderLimits = null) { throw null; }
        public static System.Security.Cryptography.X509Certificates.X509Certificate2 LoadPkcs12FromFile(string path, System.ReadOnlySpan<char> password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags = System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.DefaultKeySet, System.Security.Cryptography.X509Certificates.Pkcs12LoaderLimits? loaderLimits = null) { throw null; }
        public static System.Security.Cryptography.X509Certificates.X509Certificate2 LoadPkcs12FromFile(string path, string? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags = System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.DefaultKeySet, System.Security.Cryptography.X509Certificates.Pkcs12LoaderLimits? loaderLimits = null) { throw null; }
    }
}
#endif
