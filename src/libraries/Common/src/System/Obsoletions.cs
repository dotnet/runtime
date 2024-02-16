// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    internal static class Obsoletions
    {
        internal const string SharedUrlFormat = "https://aka.ms/dotnet-warnings/{0}";

        // Please see docs\project\list-of-diagnostics.md for instructions on the steps required
        // to introduce a new obsoletion, apply it to downlevel builds, claim a diagnostic id,
        // and ensure the "aka.ms/dotnet-warnings/{0}" URL points to documentation for the obsoletion
        // The diagnostic ids reserved for obsoletions are SYSLIB0### (SYSLIB0001 - SYSLIB0999).

        internal const string SystemTextEncodingUTF7Message = "The UTF-7 encoding is insecure and should not be used. Consider using UTF-8 instead.";
        internal const string SystemTextEncodingUTF7DiagId = "SYSLIB0001";

        internal const string PrincipalPermissionAttributeMessage = "PrincipalPermissionAttribute is not honored by the runtime and must not be used.";
        internal const string PrincipalPermissionAttributeDiagId = "SYSLIB0002";

        internal const string CodeAccessSecurityMessage = "Code Access Security is not supported or honored by the runtime.";
        internal const string CodeAccessSecurityDiagId = "SYSLIB0003";

        internal const string ConstrainedExecutionRegionMessage = "The Constrained Execution Region (CER) feature is not supported.";
        internal const string ConstrainedExecutionRegionDiagId = "SYSLIB0004";

        internal const string GlobalAssemblyCacheMessage = "The Global Assembly Cache is not supported.";
        internal const string GlobalAssemblyCacheDiagId = "SYSLIB0005";

        internal const string ThreadAbortMessage = "Thread.Abort is not supported and throws PlatformNotSupportedException.";
        internal const string ThreadResetAbortMessage = "Thread.ResetAbort is not supported and throws PlatformNotSupportedException.";
        internal const string ThreadAbortDiagId = "SYSLIB0006";

        internal const string DefaultCryptoAlgorithmsMessage = "The default implementation of this cryptography algorithm is not supported.";
        internal const string DefaultCryptoAlgorithmsDiagId = "SYSLIB0007";

        internal const string CreatePdbGeneratorMessage = "The CreatePdbGenerator API is not supported and throws PlatformNotSupportedException.";
        internal const string CreatePdbGeneratorDiagId = "SYSLIB0008";

        internal const string AuthenticationManagerMessage = "AuthenticationManager is not supported. Methods will no-op or throw PlatformNotSupportedException.";
        internal const string AuthenticationManagerDiagId = "SYSLIB0009";

        internal const string RemotingApisMessage = "This Remoting API is not supported and throws PlatformNotSupportedException.";
        internal const string RemotingApisDiagId = "SYSLIB0010";

        internal const string BinaryFormatterMessage = "BinaryFormatter serialization is obsolete and should not be used. See https://aka.ms/binaryformatter for more information.";
        internal const string BinaryFormatterDiagId = "SYSLIB0011";

        internal const string CodeBaseMessage = "Assembly.CodeBase and Assembly.EscapedCodeBase are only included for .NET Framework compatibility. Use Assembly.Location instead.";
        internal const string CodeBaseDiagId = "SYSLIB0012";

        internal const string EscapeUriStringMessage = "Uri.EscapeUriString can corrupt the Uri string in some cases. Consider using Uri.EscapeDataString for query string components instead.";
        internal const string EscapeUriStringDiagId = "SYSLIB0013";

        internal const string WebRequestMessage = "WebRequest, HttpWebRequest, ServicePoint, and WebClient are obsolete. Use HttpClient instead.";
        internal const string WebRequestDiagId = "SYSLIB0014";

        internal const string DisablePrivateReflectionAttributeMessage = "DisablePrivateReflectionAttribute has no effect in .NET 6.0+.";
        internal const string DisablePrivateReflectionAttributeDiagId = "SYSLIB0015";

        internal const string GetContextInfoMessage = "Use the Graphics.GetContextInfo overloads that accept arguments for better performance and fewer allocations.";
        internal const string GetContextInfoDiagId = "SYSLIB0016";

        internal const string StrongNameKeyPairMessage = "Strong name signing is not supported and throws PlatformNotSupportedException.";
        internal const string StrongNameKeyPairDiagId = "SYSLIB0017";

        internal const string ReflectionOnlyLoadingMessage = "ReflectionOnly loading is not supported and throws PlatformNotSupportedException.";
        internal const string ReflectionOnlyLoadingDiagId = "SYSLIB0018";

        internal const string RuntimeEnvironmentMessage = "RuntimeEnvironment members SystemConfigurationFile, GetRuntimeInterfaceAsIntPtr, and GetRuntimeInterfaceAsObject are not supported and throw PlatformNotSupportedException.";
        internal const string RuntimeEnvironmentDiagId = "SYSLIB0019";

        internal const string JsonSerializerOptionsIgnoreNullValuesMessage = "JsonSerializerOptions.IgnoreNullValues is obsolete. To ignore null values when serializing, set DefaultIgnoreCondition to JsonIgnoreCondition.WhenWritingNull.";
        internal const string JsonSerializerOptionsIgnoreNullValuesDiagId = "SYSLIB0020";

        internal const string DerivedCryptographicTypesMessage = "Derived cryptographic types are obsolete. Use the Create method on the base type instead.";
        internal const string DerivedCryptographicTypesDiagId = "SYSLIB0021";

        internal const string RijndaelMessage = "The Rijndael and RijndaelManaged types are obsolete. Use Aes instead.";
        internal const string RijndaelDiagId = "SYSLIB0022";

        internal const string RNGCryptoServiceProviderMessage = "RNGCryptoServiceProvider is obsolete. To generate a random number, use one of the RandomNumberGenerator static methods instead.";
        internal const string RNGCryptoServiceProviderDiagId = "SYSLIB0023";

        internal const string AppDomainCreateUnloadMessage = "Creating and unloading AppDomains is not supported and throws an exception.";
        internal const string AppDomainCreateUnloadDiagId = "SYSLIB0024";

        internal const string SuppressIldasmAttributeMessage = "SuppressIldasmAttribute has no effect in .NET 6.0+.";
        internal const string SuppressIldasmAttributeDiagId = "SYSLIB0025";

        internal const string X509CertificateImmutableMessage = "X509Certificate and X509Certificate2 are immutable. Use the appropriate constructor to create a new certificate.";
        internal const string X509CertificateImmutableDiagId = "SYSLIB0026";

        internal const string PublicKeyPropertyMessage = "PublicKey.Key is obsolete. Use the appropriate method to get the public key, such as GetRSAPublicKey.";
        internal const string PublicKeyPropertyDiagId = "SYSLIB0027";

        internal const string X509CertificatePrivateKeyMessage = "X509Certificate2.PrivateKey is obsolete. Use the appropriate method to get the private key, such as GetRSAPrivateKey, or use the CopyWithPrivateKey method to create a new instance with a private key.";
        internal const string X509CertificatePrivateKeyDiagId = "SYSLIB0028";

        internal const string ProduceLegacyHmacValuesMessage = "ProduceLegacyHmacValues is obsolete. Producing legacy HMAC values is not supported.";
        internal const string ProduceLegacyHmacValuesDiagId = "SYSLIB0029";

        internal const string UseManagedSha1Message = "HMACSHA1 always uses the algorithm implementation provided by the platform. Use a constructor without the useManagedSha1 parameter.";
        internal const string UseManagedSha1DiagId = "SYSLIB0030";

        internal const string CryptoConfigEncodeOIDMessage = "EncodeOID is obsolete. Use the ASN.1 functionality provided in System.Formats.Asn1.";
        internal const string CryptoConfigEncodeOIDDiagId = "SYSLIB0031";

        internal const string CorruptedStateRecoveryMessage = "Recovery from corrupted process state exceptions is not supported; HandleProcessCorruptedStateExceptionsAttribute is ignored.";
        internal const string CorruptedStateRecoveryDiagId = "SYSLIB0032";

        internal const string Rfc2898CryptDeriveKeyMessage = "Rfc2898DeriveBytes.CryptDeriveKey is obsolete and is not supported. Use PasswordDeriveBytes.CryptDeriveKey instead.";
        internal const string Rfc2898CryptDeriveKeyDiagId = "SYSLIB0033";

        internal const string CmsSignerCspParamsCtorMessage = "CmsSigner(CspParameters) is obsolete and is not supported. Use an alternative constructor instead.";
        internal const string CmsSignerCspParamsCtorDiagId = "SYSLIB0034";

        internal const string SignerInfoCounterSigMessage = "ComputeCounterSignature without specifying a CmsSigner is obsolete and is not supported. Use the overload that accepts a CmsSigner.";
        internal const string SignerInfoCounterSigDiagId = "SYSLIB0035";

        internal const string RegexCompileToAssemblyMessage = "Regex.CompileToAssembly is obsolete and not supported. Use the GeneratedRegexAttribute with the regular expression source generator instead.";
        internal const string RegexCompileToAssemblyDiagId = "SYSLIB0036";

        internal const string AssemblyNameMembersMessage = "AssemblyName members HashAlgorithm, ProcessorArchitecture, and VersionCompatibility are obsolete and not supported.";
        internal const string AssemblyNameMembersDiagId = "SYSLIB0037";

        internal const string SystemDataSerializationFormatBinaryMessage = "SerializationFormat.Binary is obsolete and should not be used. See https://aka.ms/serializationformat-binary-obsolete for more information.";
        internal const string SystemDataSerializationFormatBinaryDiagId = "SYSLIB0038";

        internal const string TlsVersion10and11Message = "TLS versions 1.0 and 1.1 have known vulnerabilities and are not recommended. Use a newer TLS version instead, or use SslProtocols.None to defer to OS defaults.";
        internal const string TlsVersion10and11DiagId = "SYSLIB0039";

        internal const string EncryptionPolicyMessage = "EncryptionPolicy.NoEncryption and AllowEncryption significantly reduce security and should not be used in production code.";
        internal const string EncryptionPolicyDiagId = "SYSLIB0040";

        internal const string Rfc2898OutdatedCtorMessage = "The default hash algorithm and iteration counts in Rfc2898DeriveBytes constructors are outdated and insecure. Use a constructor that accepts the hash algorithm and the number of iterations.";
        internal const string Rfc2898OutdatedCtorDiagId = "SYSLIB0041";

        internal const string EccXmlExportImportMessage = "ToXmlString and FromXmlString have no implementation for ECC types, and are obsolete. Use a standard import and export format such as ExportSubjectPublicKeyInfo or ImportSubjectPublicKeyInfo for public keys and ExportPkcs8PrivateKey or ImportPkcs8PrivateKey for private keys.";
        internal const string EccXmlExportImportDiagId = "SYSLIB0042";

        internal const string EcDhPublicKeyBlobMessage = "ECDiffieHellmanPublicKey.ToByteArray() and the associated constructor do not have a consistent and interoperable implementation on all platforms. Use ECDiffieHellmanPublicKey.ExportSubjectPublicKeyInfo() instead.";
        internal const string EcDhPublicKeyBlobDiagId = "SYSLIB0043";

        internal const string AssemblyNameCodeBaseMessage = "AssemblyName.CodeBase and AssemblyName.EscapedCodeBase are obsolete. Using them for loading an assembly is not supported.";
        internal const string AssemblyNameCodeBaseDiagId = "SYSLIB0044";

        internal const string CryptoStringFactoryMessage = "Cryptographic factory methods accepting an algorithm name are obsolete. Use the parameterless Create factory method on the algorithm type instead.";
        internal const string CryptoStringFactoryDiagId = "SYSLIB0045";

        internal const string ControlledExecutionRunMessage = "ControlledExecution.Run method may corrupt the process and should not be used in production code.";
        internal const string ControlledExecutionRunDiagId = "SYSLIB0046";

        internal const string XmlSecureResolverMessage = "XmlSecureResolver is obsolete. Use XmlResolver.ThrowingResolver instead when attempting to forbid XML external entity resolution.";
        internal const string XmlSecureResolverDiagId = "SYSLIB0047";

        internal const string RsaEncryptDecryptValueMessage = "RSA.EncryptValue and DecryptValue are not supported and throw NotSupportedException. Use RSA.Encrypt and RSA.Decrypt instead.";
        internal const string RsaEncryptDecryptDiagId = "SYSLIB0048";

        internal const string JsonSerializerOptionsAddContextMessage = "JsonSerializerOptions.AddContext is obsolete. To register a JsonSerializerContext, use either the TypeInfoResolver or TypeInfoResolverChain properties.";
        internal const string JsonSerializerOptionsAddContextDiagId = "SYSLIB0049";

        internal const string LegacyFormatterMessage = "Formatter-based serialization is obsolete and should not be used.";
        internal const string LegacyFormatterDiagId = "SYSLIB0050";

        internal const string LegacyFormatterImplMessage = "This API supports obsolete formatter-based serialization. It should not be called or extended by application code.";
        internal const string LegacyFormatterImplDiagId = "SYSLIB0051";

        internal const string RegexExtensibilityImplMessage = "This API supports obsolete mechanisms for Regex extensibility. It is not supported.";
        internal const string RegexExtensibilityDiagId = "SYSLIB0052";

        internal const string AesGcmTagConstructorMessage = "AesGcm should indicate the required tag size for encryption and decryption. Use a constructor that accepts the tag size.";
        internal const string AesGcmTagConstructorDiagId = "SYSLIB0053";
    }
}
