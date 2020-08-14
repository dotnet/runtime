// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Security.Cryptography.Pkcs;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public abstract partial class PfxFormatTests
    {
        [Flags]
        public enum SingleCertOptions
        {
            Default = 0,

            SkipMac = 1 << 0,

            UnshroudedKey = 1 << 1,

            KeyAndCertInSameContents = 1 << 2,
            KeyContentsLast = 1 << 3,

            PlaintextCertContents = 1 << 4,

            EncryptKeyContents = 1 << 5,
        }

        public static IEnumerable<object[]> AllSingleCertVariations
        {
            get
            {
                for (int skipMac = 0; skipMac < 2; skipMac++)
                {
                    for (int unshroudedKey = 0; unshroudedKey < 2; unshroudedKey++)
                    {
                        // 3, not 4.  Don't do SameContents | KeyLast, it's the same as SameContents.
                        for (int keySplit = 0; keySplit < 3; keySplit++)
                        {
                            for (int plaintextCert = 0; plaintextCert < 2; plaintextCert++)
                            {
                                // Only toggle EncryptKey if SameContents isn't set.
                                int encryptKeyLimit = keySplit == 1 ? 1: 2;

                                for (int encryptKey = 0; encryptKey < encryptKeyLimit; encryptKey++)
                                {
                                    yield return new object[] {
                                        (SingleCertOptions)(
                                            skipMac * (int)SingleCertOptions.SkipMac |
                                            unshroudedKey * (int)SingleCertOptions.UnshroudedKey |
                                            keySplit * (int)SingleCertOptions.KeyAndCertInSameContents |
                                            plaintextCert * (int)SingleCertOptions.PlaintextCertContents |
                                            encryptKey * (int)SingleCertOptions.EncryptKeyContents),
                                    };
                                }
                            }
                        }
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(AllSingleCertVariations))]
        public void OneCertWithOneKey(SingleCertOptions options)
        {
            bool sameContainer = (options & SingleCertOptions.KeyAndCertInSameContents) != 0;
            bool dontShroudKey = (options & SingleCertOptions.UnshroudedKey) != 0;
            bool keyContainerLast = (options & SingleCertOptions.KeyContentsLast) != 0;
            bool encryptCertSafeContents = (options & SingleCertOptions.PlaintextCertContents) == 0;
            bool encryptKeySafeContents = (options & SingleCertOptions.EncryptKeyContents) != 0;
            bool skipMac = (options & SingleCertOptions.SkipMac) != 0;
            string password = options.ToString();

            using (var cert = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword, s_exportableImportFlags))
            using (RSA key = cert.GetRSAPrivateKey())
            {
                if (dontShroudKey && OperatingSystem.IsWindows())
                {
                    // CNG keys are only encrypted-exportable, so we need to export them encrypted.
                    // Then we can import it into a new, fully-exportable key. (Sigh.)
                    byte[] tmpPkcs8 = key.ExportEncryptedPkcs8PrivateKey(password, s_windowsPbe);
                    key.ImportEncryptedPkcs8PrivateKey(password, tmpPkcs8, out _);
                }

                Pkcs12Builder builder = new Pkcs12Builder();

                Pkcs12SafeContents certContents = new Pkcs12SafeContents();
                Pkcs12SafeContents keyContents = sameContainer ? null : new Pkcs12SafeContents();
                Pkcs12SafeContents keyEffectiveContents = keyContents ?? certContents;

                Pkcs12SafeBag certBag = certContents.AddCertificate(cert);
                Pkcs12SafeBag keyBag;

                if (dontShroudKey)
                {
                    keyBag = keyEffectiveContents.AddKeyUnencrypted(key);
                }
                else
                {
                    keyBag = keyEffectiveContents.AddShroudedKey(key, password, s_windowsPbe);
                }

                certBag.Attributes.Add(s_keyIdOne);
                keyBag.Attributes.Add(s_keyIdOne);

                if (sameContainer)
                {
                    AddContents(certContents, builder, password, encryptCertSafeContents);
                }
                else if (keyContainerLast)
                {
                    AddContents(certContents, builder, password, encryptCertSafeContents);
                    AddContents(keyContents, builder, password, encryptKeySafeContents);
                }
                else
                {
                    AddContents(keyContents, builder, password, encryptKeySafeContents);
                    AddContents(certContents, builder, password, encryptCertSafeContents);
                }

                if (skipMac)
                {
                    builder.SealWithoutIntegrity();
                }
                else
                {
                    builder.SealWithMac(password, s_digestAlgorithm, MacCount);
                }

                ReadPfx(builder.Encode(), password, cert);
            }
        }
    }
}
