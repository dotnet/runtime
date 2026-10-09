// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;
using Xunit.Sdk;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Browser doesn't support X.509 certificates")]
    public static class CorruptPoliciesChainTests
    {
        private const string ApplicationCertPoliciesOid = "1.3.6.1.4.1.311.21.10";
        private const string CabfDvOid = "2.23.140.1.2.1";
        private const string TlsClientAuthOid = "1.3.6.1.5.5.7.3.2";
        private const string TlsServerAuthOid = "1.3.6.1.5.5.7.3.1";
        private const string UnofficialMappedPolicyOid = "1.0.0.127";

        private static readonly X509Extension s_unmappedPolicyExtension =
            DynamicChainTests.BuildPolicyByIdentifiers(CabfDvOid);

        private static readonly X509Extension s_policyMapping =
            DynamicChainTests.BuildPolicyMappings((CabfDvOid, UnofficialMappedPolicyOid));

        private static readonly X509Extension s_mappedPolicyExtension =
            DynamicChainTests.BuildPolicyByIdentifiers(UnofficialMappedPolicyOid);

        private static readonly X509Extension s_applicationPolicyExtension =
            new X509Extension(
                ApplicationCertPoliciesOid,
                DynamicChainTests.EncodeCertificatePoliciesValue(TlsClientAuthOid),
                critical: false);

        private static readonly X509Extension s_ekuExtension =
            new X509EnhancedKeyUsageExtension(
                new OidCollection { new Oid(TlsClientAuthOid, null) },
                critical: false);

        private static readonly X509Extension s_caTrue =
            X509BasicConstraintsExtension.CreateForCertificateAuthority();

        private static readonly X509Extension s_caFalse =
            X509BasicConstraintsExtension.CreateForEndEntity();

        private static readonly X509Extension s_corruptPolicies =
            new X509Extension(s_unmappedPolicyExtension.Oid, [0x05], critical: false);

        private static readonly X509Extension s_corruptMapping =
            new X509Extension(s_policyMapping.Oid, [0x04, 0x13], critical: false);

        private static readonly X509Extension s_corruptApplicationPolicy =
            new X509Extension(s_applicationPolicyExtension.Oid, [0x01, 0x00], critical: false);

        private static readonly X509Extension s_corruptEku =
            new X509Extension(s_ekuExtension.Oid, [0x30, 0x11], critical: false);

        private static readonly RSA[] s_keys =
        {
            RSA.Create(),
            RSA.Create(),
            RSA.Create(),
            RSA.Create(),
            RSA.Create(),
        };

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public static void CorruptCertificatePolicy(int level)
        {
            // Corruption at the root is fine for usage, but it will still
            // generate an InvalidExtension error.
            bool notValidForUsage = level != 4;

            RunCase(
                corruptCertificatePolicy: level,
                corruptApplicationPolicy: -1,
                checkCertificatePolicy: true,
                checkApplicationPolicy: false,
                leafNotValidForUsage: notValidForUsage);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public static void CorruptApplicationPolicy_WithEku(int level)
        {
            // When the ApplicationPolicy is corrupt, it seems to treat
            // the element as valid for all usages (but is still
            // scoped by the issuers)
            const bool notValidForUsage = false;

            RunCase(
                corruptApplicationPolicy: level,
                checkApplicationPolicy: true,
                leafNotValidForUsage: notValidForUsage,
                omitEku: false);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public static void CorruptApplicationPolicy_NoEku(int level)
        {
            // When the ApplicationPolicy is corrupt, it seems to treat
            // the element as valid for all usages (but is still
            // scoped by the issuers)
            const bool notValidForUsage = false;

            RunCase(
                corruptApplicationPolicy: level,
                checkApplicationPolicy: true,
                leafNotValidForUsage: notValidForUsage,
                omitEku: true);
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(0, true)]
        [InlineData(1, false)]
        [InlineData(1, true)]
        [InlineData(2, false)]
        [InlineData(2, true)]
        [InlineData(3, false)]
        [InlineData(3, true)]
        [InlineData(4, false)]
        [InlineData(4, true)]
        public static void CorruptApplicationPolicy_CheckExtraUsage(int level, bool withEku)
        {
            // When the ApplicationPolicy is corrupt, it seems to treat
            // the element as valid for all usages (but is still
            // scoped by the issuers)

            RunCase(
                corruptApplicationPolicy: level,
                checkApplicationPolicy: true,
                omitEku: !withEku,
                checkUnrelatedAppPolicy: true);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public static void CorruptEku_WithAppPol(int level)
        {
            // AppPol always wins over EKU, so usage is valid.
            const bool notValidForUsage = false;

            RunCase(
                corruptEku: level,
                checkApplicationPolicy: true,
                leafNotValidForUsage: notValidForUsage,
                omitApplicationPolicy: false);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public static void CorruptEku_NoAppPol(int level)
        {
            // When the ApplicationPolicy is corrupt, it seems to treat
            // the element as valid for all usages (but is still
            // scoped by the issuers)
            const bool notValidForUsage = false;

            RunCase(
                corruptEku: level,
                checkApplicationPolicy: true,
                leafNotValidForUsage: notValidForUsage,
                omitApplicationPolicy: true);
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(0, true)]
        [InlineData(1, false)]
        [InlineData(1, true)]
        [InlineData(2, false)]
        [InlineData(2, true)]
        [InlineData(3, false)]
        [InlineData(3, true)]
        [InlineData(4, false)]
        [InlineData(4, true)]
        public static void CorruptEku_CheckExtraUsage(int level, bool withAppPol)
        {
            // When the ApplicationPolicy is corrupt, it seems to treat
            // the element as valid for all usages (but is still
            // scoped by the issuers)

            RunCase(
                corruptEku: level,
                checkApplicationPolicy: true,
                omitApplicationPolicy: !withAppPol,
                checkUnrelatedAppPolicy: true);
        }

        [Theory]
        [InlineData(4, 3)]
        [InlineData(3, 3)]
        [InlineData(2, 3)]
        [InlineData(1, 3)]
        [InlineData(0, 3)]
        [InlineData(2, 1)]
        [InlineData(1, 1)]
        [InlineData(0, 1)]
        public static void CorruptPolicyWithMapping(int policyLevel, int mappingLevel)
        {
            bool notValidForUsage = policyLevel != 4;

            RunCase(
                corruptCertificatePolicy: policyLevel,
                mappingLevel: mappingLevel,
                checkCertificatePolicy: true,
                leafNotValidForUsage: notValidForUsage);
        }

        [Theory]
        [InlineData(3)]
        [InlineData(2)]
        [InlineData(1)]
        public static void PolicyWithCorruptMapping(int mappingLevel)
        {
            // The driver for the test issues certs below the mapping level
            // as unmapped, so this checks that the corrupt mapping counts as
            // an empty mapping for usage purposes.

            RunCase(
                mappingLevel: mappingLevel,
                checkCertificatePolicy: true,
                corruptMapping: true);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public static void CorruptCertificatePolicyNoChecks(int level)
        {
            RunCase(corruptCertificatePolicy: level);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public static void CorruptApplicationPolicyNoChecks(int level)
        {
            RunCase(corruptApplicationPolicy: level);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public static void CorruptEkuNoChecks(int level)
        {
            RunCase(corruptEku: level);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public static void CorruptMappingNoChecks(int level)
        {
            RunCase(corruptMapping: true, mappingLevel: level);
        }

        private static void RunCase(
            int corruptCertificatePolicy = -1,
            int corruptApplicationPolicy = -1,
            int corruptEku = -1,
            int mappingLevel = -1,
            bool corruptMapping = false,
            bool checkCertificatePolicy = false,
            bool checkApplicationPolicy = false,
            bool checkUnrelatedAppPolicy = false,
            bool leafNotValidForUsage = false,
            bool omitEku = false,
            bool omitApplicationPolicy = false,
            [CallerMemberName] string testName = null)
        {
            X509Certificate2[] certs = new X509Certificate2[5];

            X509Extension appPolicy = omitApplicationPolicy ? null : s_applicationPolicyExtension;
            X509Extension eku = omitEku ? null : s_ekuExtension;

            try
            {
                lock (s_keys)
                {
                    TestDataGenerator.MakeTestChain(
                        s_keys,
                        certs,
                        endEntityExtensions: Extensions(0),
                        intermediateExtensions: [
                            Extensions(1),
                            Extensions(2),
                            Extensions(3),
                        ],
                        rootExtensions: Extensions(4),
                        $"{testName}/{corruptCertificatePolicy}/{corruptApplicationPolicy}/{corruptEku}");
                }

                X509Extension[] Extensions(int level)
                {
                    X509Extension policyExt =
                        level == corruptCertificatePolicy ? s_corruptPolicies :
                        corruptMapping ? s_unmappedPolicyExtension :
                        level < mappingLevel ? s_mappedPolicyExtension : s_unmappedPolicyExtension;

                    X509Extension mapping = level == mappingLevel ?
                        corruptMapping ? s_corruptMapping : s_policyMapping :
                        null;

                    X509Extension actualAppPolicy =
                        appPolicy is null ? null :
                        level == corruptApplicationPolicy ? s_corruptApplicationPolicy : appPolicy;

                    X509Extension actualEku =
                        eku is null ? null :
                        level == corruptEku ? s_corruptEku : eku;

                    return new X509Extension[]
                    {
                        level == 0 ? s_caFalse : s_caTrue,
                        actualAppPolicy,
                        actualEku,
                        policyExt,
                        mapping,
                    };
                }

                string errors = "";

                using (ChainHolder holder = new ChainHolder())
                {
                    X509Chain chain = holder.Chain;
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                    chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                    chain.ChainPolicy.CustomTrustStore.Add(certs[4]);
                    chain.ChainPolicy.ExtraStore.Add(certs[3]);
                    chain.ChainPolicy.ExtraStore.Add(certs[2]);
                    chain.ChainPolicy.ExtraStore.Add(certs[1]);

                    if (checkCertificatePolicy)
                    {
                        chain.ChainPolicy.CertificatePolicy.Add(new Oid(CabfDvOid, null));
                    }

                    if (checkApplicationPolicy)
                    {
                        chain.ChainPolicy.ApplicationPolicy.Add(new Oid(TlsClientAuthOid, null));
                    }

                    if (checkUnrelatedAppPolicy)
                    {
                        chain.ChainPolicy.ApplicationPolicy.Add(new Oid(TlsServerAuthOid, null));
                    }

                    bool isValid = chain.Build(certs[0]);
                    X509ChainStatusFlags aggregateFlags = X509ChainStatusFlags.NoError;
                    int expectedLength = certs.Length;
                    bool detectCorruptEku = corruptEku >= 0 && omitApplicationPolicy;

                    if (PlatformDetection.IsOpenSslSupported && corruptEku >= 0)
                    {
                        // The OpenSSL chain engine will stop processing the chain when it hits a corrupt EKU,
                        // so the chain length is shorter than expected.
                        expectedLength = int.Max(1, corruptEku);
                    }

                    Assert.Equal(expectedLength, chain.ChainElements.Count);

                    for (int i = expectedLength - 1; i >= 0; i--)
                    {
                        X509ChainStatusFlags expectedStatus = X509ChainStatusFlags.NoError;

                        if (i == expectedLength - 1 && expectedLength < certs.Length)
                        {
                            expectedStatus |= X509ChainStatusFlags.PartialChain;
                        }

                        if (corruptCertificatePolicy == i ||
                            corruptApplicationPolicy == i ||
                            (detectCorruptEku && corruptEku == i) ||
                            (corruptMapping && mappingLevel == i))
                        {
                            expectedStatus |=
                                X509ChainStatusFlags.InvalidExtension |
                                X509ChainStatusFlags.InvalidPolicyConstraints;
                        }

                        if (i == 0 && leafNotValidForUsage)
                        {
                            expectedStatus |= X509ChainStatusFlags.NotValidForUsage;
                        }

                        if (checkUnrelatedAppPolicy)
                        {
                            if (i < expectedLength - 1)
                            {
                                expectedStatus |= X509ChainStatusFlags.NotValidForUsage;
                            }
                            else if (corruptApplicationPolicy == i)
                            {
                                // If only the root has a corrupt application policy,
                                // then it is valid for all usages, so no error.
                                // But all lower CAs have scoped usages, so they still
                                // trigger NotValidForUsage along with the end-entity.
                            }
                            else if (omitApplicationPolicy && corruptEku == i)
                            {
                                // If only the root has a corrupt EKU,
                                // then it is valid for all usages, so no error.
                                // But all lower CAs have scoped usages, so they still
                                // trigger NotValidForUsage along with the end-entity.
                            }
                            else
                            {
                                // The root will still be scoped, so expect an error.
                                expectedStatus |= X509ChainStatusFlags.NotValidForUsage;
                            }
                        }

                        aggregateFlags |= expectedStatus;
                        X509ChainStatusFlags actual = chain.ChainElements[i].AllStatusFlags();

                        if (expectedStatus != actual)
                        {
                            errors += $"Element {i}: Expected [{expectedStatus}], Actual [{actual}]{Environment.NewLine}";
                        }
                    }

                    if (aggregateFlags != chain.AllStatusFlags())
                    {
                        errors += $"Aggregate: Expected [{aggregateFlags}], Actual [{chain.AllStatusFlags()}]{Environment.NewLine}";
                    }

                    if (errors.Length > 0)
                    {
                        throw new XunitException(errors);
                    }

                    if (aggregateFlags == 0)
                    {
                        AssertExtensions.TrueExpression(isValid, "chain.Build(certs[0])");
                    }
                    else
                    {
                        AssertExtensions.FalseExpression(isValid, "chain.Build(certs[0])");
                    }
                }
            }
            finally
            {
                foreach (X509Certificate2 cert in certs)
                {
                    cert?.Dispose();
                }
            }
        }
    }
}
