// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Security.Cryptography.Csp.Tests
{
    public static partial class ShimHelpers
    {
        // Shims should override all virtual members and forward to their _impl.
        public static void VerifyAllBaseMembersOverridden(Type shimType)
        {
            string[] namesToNotVerify =
            {
                "Dispose",
                // DecryptValue and EncryptValue throw PNSE in base class, so they don't need to be checked.
                "DecryptValue",
                "EncryptValue",
                // PEM Import/Export defers to Import methods.
                "ImportFromPem",
                "ImportFromEncryptedPem",
                // Key Import/Export defers to ImportParameters/ExportParameters (covered by *KeyFileTests)
                "ImportRSAPrivateKey",
                "ImportRSAPublicKey",
                "ImportSubjectPublicKeyInfo",
                "ImportPkcs8PrivateKey",
                "ImportEncryptedPkcs8PrivateKey",
                "ExportRSAPrivateKey",
                "ExportRSAPublicKey",
                "ExportSubjectPublicKeyInfo",
                "ExportPkcs8PrivateKey",
                "ExportEncryptedPkcs8PrivateKey",
                "ExportRSAPrivateKey",
                "HashData",
                "TryHashData",
                "TryExportRSAPrivateKey",
                "TryExportRSAPublicKey",
                "TryExportSubjectPublicKeyInfo",
                "TryExportPkcs8PrivateKey",
                "TryExportEncryptedPkcs8PrivateKey",
                // DSASignatureFormat methods defer to older methods.
                "CreateSignatureCore",
                "SignDataCore",
                "TryCreateSignatureCore",
                "TrySignDataCore",
                "VerifyDataCore",
                "VerifySignatureCore",
                // CryptoServiceProviders will not get one-shot APIs as they are being deprecated
                "TryEncryptEcbCore",
                "TryDecryptEcbCore",
                "TryEncryptCbcCore",
                "TryDecryptCbcCore",
                "TryEncryptCfbCore",
                "TryDecryptCfbCore",
                "EncryptKeyWrapPaddedCore",
                "DecryptKeyWrapPaddedCore",
            };

            IEnumerable<MethodInfo> baseMethods = shimType.
                GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).
                Where(m => m.IsVirtual && !namesToNotVerify.Any(n => n.Equals(m.Name)));

            foreach (MethodInfo info in baseMethods)
            {
                // Ensure the override is on the shim; ignore virtual methods on System.Object
                bool methodOverridden = info.DeclaringType == shimType || info.DeclaringType == typeof(object);

                Assert.True(methodOverridden, $"Member overridden: {info.Name}");
            }
        }
    }
}
