// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET8_0_OR_GREATER
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Security.Cryptography.AesGcm))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Security.Cryptography.AuthenticationTagMismatchException))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Security.Cryptography.SP800108HmacCounterKdf))]
#endif
#if NET9_0_OR_GREATER
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Security.Cryptography.X509Certificates.Pkcs12LoaderLimits))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Security.Cryptography.X509Certificates.Pkcs12LoadLimitExceededException))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Security.Cryptography.X509Certificates.X509CertificateLoader))]
#endif
#if NET10_0_OR_GREATER
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Security.Cryptography.MLDsa))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Security.Cryptography.MLDsaAlgorithm))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Security.Cryptography.MLDsaCng))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Security.Cryptography.MLKem))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Security.Cryptography.MLKemAlgorithm))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Security.Cryptography.SlhDsa))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Security.Cryptography.SlhDsaAlgorithm))]
#endif
#if NET || NETSTANDARD2_1_OR_GREATER
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Security.Cryptography.PbeEncryptionAlgorithm))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Security.Cryptography.PbeParameters))]
#endif
