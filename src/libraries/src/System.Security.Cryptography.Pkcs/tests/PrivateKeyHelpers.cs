// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography.Pkcs.Tests
{
    internal static class PrivateKeyHelpers
    {
        internal static RSA MakeExportable(this RSA rsa)
        {
            if (rsa is RSACng rsaCng)
            {
                const CngExportPolicies Exportability =
                    CngExportPolicies.AllowExport |
                    CngExportPolicies.AllowPlaintextExport;

                if ((rsaCng.Key.ExportPolicy & Exportability) == CngExportPolicies.AllowExport)
                {
                    RSA copy = RSA.Create();

                    copy.ImportEncryptedPkcs8PrivateKey(
                        nameof(MakeExportable),
                        rsa.ExportEncryptedPkcs8PrivateKey(
                            nameof(MakeExportable),
                            new PbeParameters(
                                PbeEncryptionAlgorithm.TripleDes3KeyPkcs12,
                                HashAlgorithmName.SHA1,
                                2048)),
                        out _);
                    return copy;
                }
            }

            return rsa;
        }
    }
}
