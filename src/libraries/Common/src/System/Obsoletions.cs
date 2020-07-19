// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    internal static class Obsoletions
    {
        internal const string SharedUrlFormat = "https://aka.ms/dotnet-warnings/{0}";

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
        internal const string ThreadAbortDiagId = "SYSLIB0006";

        internal const string DefaultCryptoAlgorithmsMessage = "The default implementation of this cryptography algorithm is not supported.";
        internal const string DefaultCryptoAlgorithmsDiagId = "SYSLIB0007";

        internal const string CreatePdbGeneratorMessage = "The CreatePdbGenerator API is not supported and throws PlatformNotSupportedException.";
        internal const string CreatePdbGeneratorDiagId = "SYSLIB0008";

        internal const string AuthenticationManagerMessage = "The AuthenticationManager Authenticate and PreAuthenticate methods are not supported and throw PlatformNotSupportedException.";
        internal const string AuthenticationManagerDiagId = "SYSLIB0009";

        internal const string RemotingApisMessage = "This Remoting API is not supported and throws PlatformNotSupportedException.";
        internal const string RemotingApisDiagId = "SYSLIB0010";

        internal const string BinaryFormatterMessage = "BinaryFormatter serialization is obsolete and should not be used. See https://aka.ms/binaryformatter for more information.";
        internal const string BinaryFormatterDiagId = "SYSLIB0011";
    }
}
