// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.Versioning;

namespace System.Security.Cryptography
{
    public partial class CryptoConfig
    {
        [UnsupportedOSPlatform("browser")]
        public static void AddAlgorithm(Type algorithm, params string[] names) => throw new PlatformNotSupportedException(SR.SystemSecurityCryptographyAlgorithms_PlatformNotSupported);

        [UnsupportedOSPlatform("browser")]
        public static void AddOID(string oid, params string[] names) => throw new PlatformNotSupportedException(SR.SystemSecurityCryptographyAlgorithms_PlatformNotSupported);

        [UnsupportedOSPlatform("browser")]
        public static string? MapNameToOID(string name) => throw new PlatformNotSupportedException(SR.SystemSecurityCryptographyAlgorithms_PlatformNotSupported);

        [UnsupportedOSPlatform("browser")]
        [Obsolete(Obsoletions.CryptoConfigEncodeOIDMessage, DiagnosticId = Obsoletions.CryptoConfigEncodeOIDDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public static byte[] EncodeOID(string str) => throw new PlatformNotSupportedException(SR.SystemSecurityCryptographyAlgorithms_PlatformNotSupported);

        [RequiresUnreferencedCode("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
        public static object? CreateFromName(string name, params object?[]? args)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            switch (name)
            {
#pragma warning disable SYSLIB0021 // Obsolete: derived cryptographic types
                // hardcode mapping for SHA* algorithm names from https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.cryptoconfig?view=net-5.0#remarks
                case "SHA":
                case "SHA1":
                case "System.Security.Cryptography.SHA1":
                    return new SHA1Managed();
                case "SHA256":
                case "SHA-256":
                case "System.Security.Cryptography.SHA256":
                    return new SHA256Managed();
                case "SHA384":
                case "SHA-384":
                case "System.Security.Cryptography.SHA384":
                    return new SHA384Managed();
                case "SHA512":
                case "SHA-512":
                case "System.Security.Cryptography.SHA512":
                    return new SHA512Managed();
#pragma warning restore SYSLIB0021
            }

            return null;
        }

        [RequiresUnreferencedCode(CreateFromNameUnreferencedCodeMessage)]
        public static object? CreateFromName(string name)
        {
            return CreateFromName(name, null);
        }
    }
}
