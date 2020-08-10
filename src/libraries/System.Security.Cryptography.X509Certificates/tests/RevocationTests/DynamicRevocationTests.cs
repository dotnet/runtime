// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates.Tests.Common;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests.RevocationTests
{
    [OuterLoop("These tests run serially at about 1 second each, and the code shouldn't change that often.")]
    public static class DynamicRevocationTests
    {
        // The CI machines are doing an awful lot of things at once, be generous with the timeout;
        internal static readonly TimeSpan s_urlRetrievalLimit = TimeSpan.FromSeconds(15);

        private static readonly Oid s_tlsServerOid = new Oid("1.3.6.1.5.5.7.3.1", null);

        private static readonly X509ChainStatusFlags ThisOsRevocationStatusUnknown =
                PlatformDetection.IsOSX ?
                X509ChainStatusFlags.RevocationStatusUnknown :
                X509ChainStatusFlags.RevocationStatusUnknown | X509ChainStatusFlags.OfflineRevocation;


        private delegate void RunSimpleTest(
            CertificateAuthority root,
            CertificateAuthority intermediate,
            X509Certificate2 endEntity,
            ChainHolder chainHolder);

        public static IEnumerable<object[]> AllViableRevocation
        {
            get
            {
                for (int designation = 0; designation < 4; designation++)
                {
                    PkiOptions designationOptions = (PkiOptions)(designation << 16);

                    for (int iss = 1; iss < 4; iss++)
                    {
                        PkiOptions issuerRevocation = (PkiOptions)iss;

                        if (designationOptions.HasFlag(PkiOptions.RootAuthorityHasDesignatedOcspResponder) &&
                            !issuerRevocation.HasFlag(PkiOptions.IssuerRevocationViaOcsp))
                        {
                            continue;
                        }

                        for (int ee = 1; ee < 4; ee++)
                        {
                            PkiOptions endEntityRevocation = (PkiOptions)(ee << 2);

                            if (designationOptions.HasFlag(PkiOptions.IssuerAuthorityHasDesignatedOcspResponder) &&
                                !endEntityRevocation.HasFlag(PkiOptions.EndEntityRevocationViaOcsp))
                            {
                                continue;
                            }

                            // https://github.com/dotnet/runtime/issues/31249
                            // not all scenarios are working on macOS.
                            if (PlatformDetection.IsOSX)
                            {
                                if (!endEntityRevocation.HasFlag(PkiOptions.EndEntityRevocationViaOcsp))
                                {
                                    continue;
                                }
                            }

                            yield return new object[] { designationOptions | issuerRevocation | endEntityRevocation };
                        }
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(AllViableRevocation))]
        public static void NothingRevoked(PkiOptions pkiOptions)
        {
            SimpleTest(
                pkiOptions,
                (root, intermediate, endEntity, holder) =>
                {
                    SimpleRevocationBody(
                        holder,
                        endEntity,
                        rootRevoked: false,
                        issrRevoked: false,
                        leafRevoked: false);
                });
        }

        [Theory]
        [MemberData(nameof(AllViableRevocation))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/31249", TestPlatforms.OSX)]
        public static void RevokeIntermediate(PkiOptions pkiOptions)
        {
            SimpleTest(
                pkiOptions,
                (root, intermediate, endEntity, holder) =>
                {
                    using (X509Certificate2 intermediateCert = intermediate.CloneIssuerCert())
                    {
                        X509Chain chain = holder.Chain;
                        DateTimeOffset now = DateTimeOffset.UtcNow;
                        root.Revoke(intermediateCert, now);
                        chain.ChainPolicy.VerificationTime = now.AddSeconds(1).UtcDateTime;
                    }

                    SimpleRevocationBody(
                        holder,
                        endEntity,
                        rootRevoked: false,
                        issrRevoked: true,
                        leafRevoked: false);
                });
        }

        [Theory]
        [MemberData(nameof(AllViableRevocation))]
        public static void RevokeEndEntity(PkiOptions pkiOptions)
        {
            SimpleTest(
                pkiOptions,
                (root, intermediate, endEntity, holder) =>
                {
                    DateTimeOffset now = DateTimeOffset.UtcNow;
                    intermediate.Revoke(endEntity, now);
                    holder.Chain.ChainPolicy.VerificationTime = now.AddSeconds(1).UtcDateTime;

                    SimpleRevocationBody(
                        holder,
                        endEntity,
                        rootRevoked: false,
                        issrRevoked: false,
                        leafRevoked: true);
                });
        }

        [Theory]
        [MemberData(nameof(AllViableRevocation))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/31249", TestPlatforms.OSX)]
        public static void RevokeIntermediateAndEndEntity(PkiOptions pkiOptions)
        {
            SimpleTest(
                pkiOptions,
                (root, intermediate, endEntity, holder) =>
                {
                    using (X509Certificate2 intermediateCert = intermediate.CloneIssuerCert())
                    {
                        X509Chain chain = holder.Chain;
                        DateTimeOffset now = DateTimeOffset.UtcNow;

                        root.Revoke(intermediateCert, now);
                        intermediate.Revoke(endEntity, now);

                        chain.ChainPolicy.VerificationTime = now.AddSeconds(1).UtcDateTime;

                        SimpleRevocationBody(
                            holder,
                            endEntity,
                            rootRevoked: false,
                            issrRevoked: true,
                            leafRevoked: true);
                    }
                });
        }

        [Theory]
        [MemberData(nameof(AllViableRevocation))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/31249", TestPlatforms.OSX)]
        public static void RevokeRoot(PkiOptions pkiOptions)
        {
            SimpleTest(
                pkiOptions,
                (root, intermediate, endEntity, holder) =>
                {
                    DateTimeOffset now = DateTimeOffset.UtcNow;
                    X509Chain chain = holder.Chain;

                    root.RebuildRootWithRevocation();

                    using (X509Certificate2 revocableRoot = root.CloneIssuerCert())
                    {
                        chain.ChainPolicy.CustomTrustStore.Clear();
                        chain.ChainPolicy.CustomTrustStore.Add(revocableRoot);

                        root.Revoke(revocableRoot, now);

                        chain.ChainPolicy.VerificationTime = now.AddSeconds(1).UtcDateTime;

                        SimpleRevocationBody(
                            holder,
                            endEntity,
                            rootRevoked: true,
                            issrRevoked: false,
                            leafRevoked: false,
                            testWithRootRevocation: true);

                        // Make sure nothing weird happens during the root-only test.
                        CheckRevokedRootDirectly(holder, revocableRoot);
                    }
                });
        }

        [Theory]
        [MemberData(nameof(AllViableRevocation))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/31249", TestPlatforms.OSX)]
        public static void RevokeRootAndEndEntity(PkiOptions pkiOptions)
        {
            SimpleTest(
                pkiOptions,
                (root, intermediate, endEntity, holder) =>
                {
                    DateTimeOffset now = DateTimeOffset.UtcNow;
                    X509Chain chain = holder.Chain;

                    root.RebuildRootWithRevocation();

                    using (X509Certificate2 revocableRoot = root.CloneIssuerCert())
                    {
                        chain.ChainPolicy.CustomTrustStore.Clear();
                        chain.ChainPolicy.CustomTrustStore.Add(revocableRoot);

                        root.Revoke(revocableRoot, now);
                        intermediate.Revoke(endEntity, now);

                        chain.ChainPolicy.VerificationTime = now.AddSeconds(1).UtcDateTime;

                        SimpleRevocationBody(
                            holder,
                            endEntity,
                            rootRevoked: true,
                            issrRevoked: false,
                            leafRevoked: true,
                            testWithRootRevocation: true);
                    }
                });
        }

        [Theory]
        [MemberData(nameof(AllViableRevocation))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/31249", TestPlatforms.OSX)]
        public static void RevokeRootAndIntermediate(PkiOptions pkiOptions)
        {
            SimpleTest(
                pkiOptions,
                (root, intermediate, endEntity, holder) =>
                {
                    DateTimeOffset now = DateTimeOffset.UtcNow;
                    X509Chain chain = holder.Chain;

                    root.RebuildRootWithRevocation();

                    using (X509Certificate2 revocableRoot = root.CloneIssuerCert())
                    using (X509Certificate2 intermediatePub = intermediate.CloneIssuerCert())
                    {
                        chain.ChainPolicy.CustomTrustStore.Clear();
                        chain.ChainPolicy.CustomTrustStore.Add(revocableRoot);

                        root.Revoke(revocableRoot, now);
                        root.Revoke(intermediatePub, now);

                        chain.ChainPolicy.VerificationTime = now.AddSeconds(1).UtcDateTime;

                        SimpleRevocationBody(
                            holder,
                            endEntity,
                            rootRevoked: true,
                            issrRevoked: true,
                            leafRevoked: false,
                            testWithRootRevocation: true);
                    }
                });
        }

        [Theory]
        [MemberData(nameof(AllViableRevocation))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/31249", TestPlatforms.OSX)]
        public static void RevokeEverything(PkiOptions pkiOptions)
        {
            SimpleTest(
                pkiOptions,
                (root, intermediate, endEntity, holder) =>
                {
                    DateTimeOffset now = DateTimeOffset.UtcNow;
                    X509Chain chain = holder.Chain;

                    root.RebuildRootWithRevocation();

                    using (X509Certificate2 revocableRoot = root.CloneIssuerCert())
                    using (X509Certificate2 intermediatePub = intermediate.CloneIssuerCert())
                    {
                        chain.ChainPolicy.CustomTrustStore.Clear();
                        chain.ChainPolicy.CustomTrustStore.Add(revocableRoot);

                        root.Revoke(revocableRoot, now);
                        root.Revoke(intermediatePub, now);
                        intermediate.Revoke(endEntity, now);

                        chain.ChainPolicy.VerificationTime = now.AddSeconds(1).UtcDateTime;

                        SimpleRevocationBody(
                            holder,
                            endEntity,
                            rootRevoked: true,
                            issrRevoked: true,
                            leafRevoked: true,
                            testWithRootRevocation: true);
                    }
                });
        }

        [Theory]
        [InlineData(PkiOptions.OcspEverywhere)]
        [InlineData(PkiOptions.AllIssuerRevocation | PkiOptions.EndEntityRevocationViaOcsp)]
        [InlineData(PkiOptions.IssuerRevocationViaCrl | PkiOptions.EndEntityRevocationViaOcsp)]
        public static void RevokeEndEntity_IssuerUnrelatedOcsp(PkiOptions pkiOptions)
        {
            SimpleTest(
                pkiOptions,
                (root, intermediate, endEntity, holder) =>
                {
                    DateTimeOffset now = DateTimeOffset.UtcNow;

                    using (RSA tmpRoot = RSA.Create())
                    using (RSA rsa = RSA.Create())
                    {
                        CertificateRequest rootReq = new CertificateRequest(
                            BuildSubject(
                                "Unauthorized Root",
                                nameof(RevokeEndEntity_IssuerUnrelatedOcsp),
                                pkiOptions,
                                true),
                            tmpRoot,
                            HashAlgorithmName.SHA256,
                            RSASignaturePadding.Pkcs1);

                        rootReq.CertificateExtensions.Add(
                            new X509BasicConstraintsExtension(true, false, 0, true));
                        rootReq.CertificateExtensions.Add(
                            new X509SubjectKeyIdentifierExtension(rootReq.PublicKey, false));
                        rootReq.CertificateExtensions.Add(
                            new X509KeyUsageExtension(
                                X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign,
                                false));

                        using (CertificateAuthority unrelated = new CertificateAuthority(
                            rootReq.CreateSelfSigned(now.AddMinutes(-5), now.AddMonths(1)),
                            aiaHttpUrl: null,
                            cdpUrl: null,
                            ocspUrl: null))
                        {
                            X509Certificate2 designatedSigner = unrelated.CreateOcspSigner(
                                BuildSubject(
                                    "Unrelated Designated OCSP Responder",
                                    nameof(RevokeEndEntity_IssuerUnrelatedOcsp),
                                    pkiOptions,
                                    true),
                                rsa);

                            using (designatedSigner)
                            {
                                intermediate.DesignateOcspResponder(designatedSigner.CopyWithPrivateKey(rsa));
                            }
                        }
                    }

                    intermediate.Revoke(endEntity, now);

                    X509Chain chain = holder.Chain;
                    chain.ChainPolicy.VerificationTime = now.AddSeconds(1).UtcDateTime;

                    bool chainBuilt = chain.Build(endEntity);

                    AssertChainStatus(
                        chain,
                        rootStatus: X509ChainStatusFlags.NoError,
                        issrStatus: X509ChainStatusFlags.NoError,
                        leafStatus: ThisOsRevocationStatusUnknown);

                    Assert.False(chainBuilt, "Chain built with ExcludeRoot.");
                    holder.DisposeChainElements();

                    chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EndCertificateOnly;

                    chainBuilt = chain.Build(endEntity);

                    AssertChainStatus(
                        chain,
                        rootStatus: X509ChainStatusFlags.NoError,
                        issrStatus: X509ChainStatusFlags.NoError,
                        leafStatus: ThisOsRevocationStatusUnknown);

                    Assert.False(chainBuilt, "Chain built with EndCertificateOnly");
                });
        }

        [Theory]
        [InlineData(PkiOptions.OcspEverywhere)]
        [InlineData(PkiOptions.IssuerRevocationViaOcsp | PkiOptions.AllEndEntityRevocation)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/31249", TestPlatforms.OSX)]
        public static void RevokeEndEntity_RootUnrelatedOcsp(PkiOptions pkiOptions)
        {
            SimpleTest(
                pkiOptions,
                (root, intermediate, endEntity, holder) =>
                {
                    DateTimeOffset now = DateTimeOffset.UtcNow;

                    using (RSA tmpRoot = RSA.Create())
                    using (RSA rsa = RSA.Create())
                    {
                        CertificateRequest rootReq = new CertificateRequest(
                            BuildSubject(
                                "Unauthorized Root",
                                nameof(RevokeEndEntity_IssuerUnrelatedOcsp),
                                pkiOptions,
                                true),
                            tmpRoot,
                            HashAlgorithmName.SHA256,
                            RSASignaturePadding.Pkcs1);

                        rootReq.CertificateExtensions.Add(
                            new X509BasicConstraintsExtension(true, false, 0, true));
                        rootReq.CertificateExtensions.Add(
                            new X509SubjectKeyIdentifierExtension(rootReq.PublicKey, false));
                        rootReq.CertificateExtensions.Add(
                            new X509KeyUsageExtension(
                                X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign,
                                false));

                        using (CertificateAuthority unrelated = new CertificateAuthority(
                            rootReq.CreateSelfSigned(now.AddMinutes(-5), now.AddMonths(1)),
                            aiaHttpUrl: null,
                            cdpUrl: null,
                            ocspUrl: null))
                        {
                            X509Certificate2 designatedSigner = unrelated.CreateOcspSigner(
                                BuildSubject(
                                    "Unrelated Designated OCSP Responder",
                                    nameof(RevokeEndEntity_IssuerUnrelatedOcsp),
                                    pkiOptions,
                                    true),
                                rsa);

                            using (designatedSigner)
                            {
                                root.DesignateOcspResponder(designatedSigner.CopyWithPrivateKey(rsa));
                            }
                        }
                    }

                    using (X509Certificate2 issuerPub = intermediate.CloneIssuerCert())
                    {
                        root.Revoke(issuerPub, now);
                    }

                    X509Chain chain = holder.Chain;
                    chain.ChainPolicy.VerificationTime = now.AddSeconds(1).UtcDateTime;

                    bool chainBuilt = chain.Build(endEntity);

                    AssertChainStatus(
                        chain,
                        rootStatus: X509ChainStatusFlags.NoError,
                        issrStatus: ThisOsRevocationStatusUnknown,
                        leafStatus: X509ChainStatusFlags.NoError);

                    Assert.False(chainBuilt, "Chain built with ExcludeRoot.");
                    holder.DisposeChainElements();

                    chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EndCertificateOnly;

                    chainBuilt = chain.Build(endEntity);

                    AssertChainStatus(
                        chain,
                        rootStatus: X509ChainStatusFlags.NoError,
                        issrStatus: X509ChainStatusFlags.NoError,
                        leafStatus: X509ChainStatusFlags.NoError);

                    Assert.True(chainBuilt, "Chain built with EndCertificateOnly");
                });
        }

        [Theory]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/31249", TestPlatforms.OSX)]
        public static void RevokeIntermediate_PolicyErrors_NotTimeValid(bool policyErrors, bool notTimeValid)
        {
            SimpleTest(
                PkiOptions.OcspEverywhere,
                (root, intermediate, endEntity, holder) =>
                {
                    DateTimeOffset now = DateTimeOffset.UtcNow;
                    X509Chain chain = holder.Chain;
                    chain.ChainPolicy.UrlRetrievalTimeout = s_urlRetrievalLimit;

                    using (X509Certificate2 intermediateCert = intermediate.CloneIssuerCert())
                    {
                        root.Revoke(intermediateCert, now);
                    }

                    X509ChainStatusFlags leafProblems = X509ChainStatusFlags.NoError;
                    X509ChainStatusFlags issuerExtraProblems = X509ChainStatusFlags.NoError;

                    if (notTimeValid)
                    {
                        chain.ChainPolicy.VerificationTime = endEntity.NotAfter.AddSeconds(1);
                        leafProblems |= X509ChainStatusFlags.NotTimeValid;
                    }
                    else
                    {
                        chain.ChainPolicy.VerificationTime = now.AddSeconds(1).UtcDateTime;
                    }

                    if (policyErrors)
                    {
                        chain.ChainPolicy.ApplicationPolicy.Add(s_tlsServerOid);
                        leafProblems |= X509ChainStatusFlags.NotValidForUsage;

                        // [ActiveIssue("https://github.com/dotnet/runtime/issues/31246")]
                        // Linux reports this code at more levels than Windows does.
                        if (OperatingSystem.IsLinux())
                        {
                            issuerExtraProblems |= X509ChainStatusFlags.NotValidForUsage;
                        }
                    }

                    bool chainBuilt = chain.Build(endEntity);

                    AssertChainStatus(
                        chain,
                        rootStatus: issuerExtraProblems,
                        issrStatus: issuerExtraProblems | X509ChainStatusFlags.Revoked,
                        leafStatus: leafProblems | ThisOsRevocationStatusUnknown);

                    Assert.False(chainBuilt, "Chain built with ExcludeRoot.");
                    holder.DisposeChainElements();

                    chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EndCertificateOnly;

                    chainBuilt = chain.Build(endEntity);

                    AssertChainStatus(
                        chain,
                        rootStatus: issuerExtraProblems,
                        issrStatus: issuerExtraProblems,
                        leafStatus: leafProblems);

                    Assert.False(chainBuilt, "Chain built with EndCertificateOnly (no ignore flags)");
                    holder.DisposeChainElements();

                    chain.ChainPolicy.VerificationFlags |=
                        X509VerificationFlags.IgnoreNotTimeValid |
                        X509VerificationFlags.IgnoreWrongUsage;

                    chainBuilt = chain.Build(endEntity);

                    AssertChainStatus(
                        chain,
                        rootStatus: issuerExtraProblems,
                        issrStatus: issuerExtraProblems,
                        leafStatus: leafProblems);

                    Assert.True(chainBuilt, "Chain built with EndCertificateOnly (with ignore flags)");
                },
                pkiOptionsInTestName: false);
        }

        [Theory]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/31249", TestPlatforms.OSX)]
        public static void RevokeEndEntity_PolicyErrors_NotTimeValid(bool policyErrors, bool notTimeValid)
        {
            SimpleTest(
                PkiOptions.OcspEverywhere,
                (root, intermediate, endEntity, holder) =>
                {
                    DateTimeOffset now = DateTimeOffset.UtcNow;
                    X509Chain chain = holder.Chain;
                    chain.ChainPolicy.UrlRetrievalTimeout = s_urlRetrievalLimit;

                    intermediate.Revoke(endEntity, now);

                    X509ChainStatusFlags leafProblems = X509ChainStatusFlags.NoError;
                    X509ChainStatusFlags issuerExtraProblems = X509ChainStatusFlags.NoError;

                    if (notTimeValid)
                    {
                        chain.ChainPolicy.VerificationTime = endEntity.NotAfter.AddSeconds(1);
                        leafProblems |= X509ChainStatusFlags.NotTimeValid;
                    }
                    else
                    {
                        chain.ChainPolicy.VerificationTime = now.AddSeconds(1).UtcDateTime;
                    }

                    if (policyErrors)
                    {
                        chain.ChainPolicy.ApplicationPolicy.Add(s_tlsServerOid);
                        leafProblems |= X509ChainStatusFlags.NotValidForUsage;

                        // [ActiveIssue("https://github.com/dotnet/runtime/issues/31246")]
                        // Linux reports this code at more levels than Windows does.
                        if (OperatingSystem.IsLinux())
                        {
                            issuerExtraProblems |= X509ChainStatusFlags.NotValidForUsage;
                        }
                    }

                    bool chainBuilt = chain.Build(endEntity);

                    AssertChainStatus(
                        chain,
                        rootStatus: issuerExtraProblems,
                        issrStatus: issuerExtraProblems,
                        leafStatus: leafProblems | X509ChainStatusFlags.Revoked);

                    Assert.False(chainBuilt, "Chain built with ExcludeRoot.");
                    holder.DisposeChainElements();

                    chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EndCertificateOnly;

                    chainBuilt = chain.Build(endEntity);

                    AssertChainStatus(
                        chain,
                        rootStatus: issuerExtraProblems,
                        issrStatus: issuerExtraProblems,
                        leafStatus: leafProblems | X509ChainStatusFlags.Revoked);

                    Assert.False(chainBuilt, "Chain built with EndCertificateOnly (no ignore flags)");
                    holder.DisposeChainElements();

                    chain.ChainPolicy.VerificationFlags |=
                        X509VerificationFlags.IgnoreNotTimeValid |
                        X509VerificationFlags.IgnoreWrongUsage;

                    chainBuilt = chain.Build(endEntity);

                    AssertChainStatus(
                        chain,
                        rootStatus: issuerExtraProblems,
                        issrStatus: issuerExtraProblems,
                        leafStatus: leafProblems | X509ChainStatusFlags.Revoked);

                    Assert.False(chainBuilt, "Chain built with EndCertificateOnly (with ignore flags)");
                },
                pkiOptionsInTestName: false);
        }

        [Theory]
        [MemberData(nameof(AllViableRevocation))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/31249", TestPlatforms.OSX)]
        public static void RevokeEndEntity_RootRevocationOffline(PkiOptions pkiOptions)
        {
            BuildPrivatePki(
                pkiOptions,
                out RevocationResponder responder,
                out CertificateAuthority root,
                out CertificateAuthority intermediate,
                out X509Certificate2 endEntity,
                registerAuthorities: false,
                pkiOptionsInSubject: true);

            using (responder)
            using (root)
            using (intermediate)
            using (endEntity)
            using (ChainHolder holder = new ChainHolder())
            using (X509Certificate2 rootCert = root.CloneIssuerCert())
            using (X509Certificate2 intermediateCert = intermediate.CloneIssuerCert())
            {
                DateTimeOffset now = DateTimeOffset.UtcNow;
                X509Chain chain = holder.Chain;

                responder.AddCertificateAuthority(intermediate);
                intermediate.Revoke(endEntity, now);

                chain.ChainPolicy.ExtraStore.Add(intermediateCert);
                chain.ChainPolicy.CustomTrustStore.Add(rootCert);
                chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                chain.ChainPolicy.VerificationTime = now.AddSeconds(1).UtcDateTime;
                chain.ChainPolicy.UrlRetrievalTimeout = s_urlRetrievalLimit;

                bool chainBuilt = chain.Build(endEntity);

                AssertChainStatus(
                    chain,
                    rootStatus: X509ChainStatusFlags.NoError,
                    issrStatus: ThisOsRevocationStatusUnknown,
                    leafStatus: X509ChainStatusFlags.Revoked);

                Assert.False(chainBuilt, "Chain built with ExcludeRoot.");
                holder.DisposeChainElements();

                chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EndCertificateOnly;

                chainBuilt = chain.Build(endEntity);

                AssertChainStatus(
                    chain,
                    rootStatus: X509ChainStatusFlags.NoError,
                    issrStatus: X509ChainStatusFlags.NoError,
                    leafStatus: X509ChainStatusFlags.Revoked);

                Assert.False(chainBuilt, "Chain built with EndCertificateOnly");
                holder.DisposeChainElements();

                chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;

                chainBuilt = chain.Build(endEntity);

                // Potentially surprising result: Even in EntireChain mode,
                // root revocation is NoError, not RevocationStatusUnknown.
                AssertChainStatus(
                    chain,
                    rootStatus: X509ChainStatusFlags.NoError,
                    issrStatus: ThisOsRevocationStatusUnknown,
                    leafStatus: X509ChainStatusFlags.Revoked);

                Assert.False(chainBuilt, "Chain built with EntireChain");
            }
        }

        [Theory]
        [MemberData(nameof(AllViableRevocation))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/31249", TestPlatforms.OSX)]
        public static void NothingRevoked_RootRevocationOffline(PkiOptions pkiOptions)
        {
            BuildPrivatePki(
                pkiOptions,
                out RevocationResponder responder,
                out CertificateAuthority root,
                out CertificateAuthority intermediate,
                out X509Certificate2 endEntity,
                registerAuthorities: false,
                pkiOptionsInSubject: true);

            using (responder)
            using (root)
            using (intermediate)
            using (endEntity)
            using (ChainHolder holder = new ChainHolder())
            using (X509Certificate2 rootCert = root.CloneIssuerCert())
            using (X509Certificate2 intermediateCert = intermediate.CloneIssuerCert())
            {
                DateTimeOffset now = DateTimeOffset.UtcNow;
                X509Chain chain = holder.Chain;

                responder.AddCertificateAuthority(intermediate);

                chain.ChainPolicy.ExtraStore.Add(intermediateCert);
                chain.ChainPolicy.CustomTrustStore.Add(rootCert);
                chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                chain.ChainPolicy.VerificationTime = now.AddSeconds(1).UtcDateTime;
                chain.ChainPolicy.UrlRetrievalTimeout = s_urlRetrievalLimit;

                bool chainBuilt = chain.Build(endEntity);

                AssertChainStatus(
                    chain,
                    rootStatus: X509ChainStatusFlags.NoError,
                    issrStatus: ThisOsRevocationStatusUnknown,
                    leafStatus: X509ChainStatusFlags.NoError);

                Assert.False(chainBuilt, "Chain built with ExcludeRoot.");
                holder.DisposeChainElements();

                chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EndCertificateOnly;

                chainBuilt = chain.Build(endEntity);

                AssertChainStatus(
                    chain,
                    rootStatus: X509ChainStatusFlags.NoError,
                    issrStatus: X509ChainStatusFlags.NoError,
                    leafStatus: X509ChainStatusFlags.NoError);

                Assert.True(chainBuilt, "Chain built with EndCertificateOnly");
                holder.DisposeChainElements();

                chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;

                chainBuilt = chain.Build(endEntity);

                // Potentially surprising result: Even in EntireChain mode,
                // root revocation is NoError, not RevocationStatusUnknown.
                AssertChainStatus(
                    chain,
                    rootStatus: X509ChainStatusFlags.NoError,
                    issrStatus: ThisOsRevocationStatusUnknown,
                    leafStatus: X509ChainStatusFlags.NoError);

                Assert.False(chainBuilt, "Chain built with EntireChain");
            }
        }

        [Theory]
        [MemberData(nameof(AllViableRevocation))]
        public static void RevokeEndEntityWithInvalidRevocationSignature(PkiOptions pkiOptions)
        {
            SimpleTest(
                pkiOptions,
                (root, intermediate, endEntity, holder) =>
                {
                    intermediate.CorruptRevocationSignature = true;

                    RevokeEndEntityWithInvalidRevocation(holder, intermediate, endEntity);
                });
        }

        [Theory]
        [MemberData(nameof(AllViableRevocation))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/31249", TestPlatforms.OSX)]
        public static void RevokeIntermediateWithInvalidRevocationSignature(PkiOptions pkiOptions)
        {
            SimpleTest(
                pkiOptions,
                (root, intermediate, endEntity, holder) =>
                {
                    root.CorruptRevocationSignature = true;

                    RevokeIntermediateWithInvalidRevocation(holder, root, intermediate, endEntity);
                });
        }

        [Theory]
        [MemberData(nameof(AllViableRevocation))]
        public static void RevokeEndEntityWithInvalidRevocationName(PkiOptions pkiOptions)
        {
            SimpleTest(
                pkiOptions,
                (root, intermediate, endEntity, holder) =>
                {
                    intermediate.CorruptRevocationIssuerName = true;

                    RevokeEndEntityWithInvalidRevocation(holder, intermediate, endEntity);
                });
        }

        [Theory]
        [MemberData(nameof(AllViableRevocation))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/31249", TestPlatforms.OSX)]
        public static void RevokeIntermediateWithInvalidRevocationName(PkiOptions pkiOptions)
        {
            SimpleTest(
                pkiOptions,
                (root, intermediate, endEntity, holder) =>
                {
                    root.CorruptRevocationIssuerName = true;

                    RevokeIntermediateWithInvalidRevocation(holder, root, intermediate, endEntity);
                });
        }

        [Theory]
        [MemberData(nameof(AllViableRevocation))]
        public static void RevokeEndEntityWithExpiredRevocation(PkiOptions pkiOptions)
        {
            SimpleTest(
                pkiOptions,
                (root, intermediate, endEntity, holder) =>
                {
                    DateTime revocationTime = endEntity.NotBefore;
                    holder.Chain.ChainPolicy.VerificationTime = revocationTime.AddSeconds(1);

                    intermediate.RevocationExpiration = revocationTime;
                    intermediate.Revoke(endEntity, revocationTime);

                    SimpleRevocationBody(
                        holder,
                        endEntity,
                        rootRevoked: false,
                        issrRevoked: false,
                        leafRevoked: true);
                });
        }

        [Theory]
        [MemberData(nameof(AllViableRevocation))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/31249", TestPlatforms.OSX)]
        public static void RevokeIntermediateWithExpiredRevocation(PkiOptions pkiOptions)
        {
            SimpleTest(
                pkiOptions,
                (root, intermediate, endEntity, holder) =>
                {
                    DateTime revocationTime = endEntity.NotBefore;
                    holder.Chain.ChainPolicy.VerificationTime = revocationTime.AddSeconds(1);

                    using (X509Certificate2 intermediatePub = intermediate.CloneIssuerCert())
                    {
                        root.RevocationExpiration = revocationTime;
                        root.Revoke(intermediatePub, revocationTime);
                    }

                    SimpleRevocationBody(
                        holder,
                        endEntity,
                        rootRevoked: false,
                        issrRevoked: true,
                        leafRevoked: false);
                });
        }

        [Theory]
        [MemberData(nameof(AllViableRevocation))]
        public static void CheckEndEntityWithExpiredRevocation(PkiOptions pkiOptions)
        {
            SimpleTest(
                pkiOptions,
                (root, intermediate, endEntity, holder) =>
                {
                    intermediate.RevocationExpiration = endEntity.NotBefore;

                    RunWithInconclusiveEndEntityRevocation(holder, endEntity);
                });
        }

        [Theory]
        [MemberData(nameof(AllViableRevocation))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/31249", TestPlatforms.OSX)]
        public static void CheckIntermediateWithExpiredRevocation(PkiOptions pkiOptions)
        {
            SimpleTest(
                pkiOptions,
                (root, intermediate, endEntity, holder) =>
                {
                    root.RevocationExpiration = endEntity.NotBefore;

                    RunWithInconclusiveIntermediateRevocation(holder, endEntity);
                });
        }

        private static void RevokeEndEntityWithInvalidRevocation(
            ChainHolder holder,
            CertificateAuthority intermediate,
            X509Certificate2 endEntity)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            X509Chain chain = holder.Chain;

            intermediate.Revoke(endEntity, now);

            chain.ChainPolicy.VerificationTime = now.AddSeconds(1).UtcDateTime;

            RunWithInconclusiveEndEntityRevocation(holder, endEntity);
        }

        private static void RevokeIntermediateWithInvalidRevocation(
            ChainHolder holder,
            CertificateAuthority root,
            CertificateAuthority intermediate,
            X509Certificate2 endEntity)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            X509Chain chain = holder.Chain;

            using (X509Certificate2 intermediatePub = intermediate.CloneIssuerCert())
            {
                root.Revoke(intermediatePub, now);
            }

            chain.ChainPolicy.VerificationTime = now.AddSeconds(1).UtcDateTime;

            RunWithInconclusiveIntermediateRevocation(holder, endEntity);
        }

        private static void CheckRevokedRootDirectly(
            ChainHolder holder,
            X509Certificate2 rootCert)
        {
            holder.DisposeChainElements();
            X509Chain chain = holder.Chain;

            chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
            bool chainBuilt = chain.Build(rootCert);

            Assert.Equal(1, chain.ChainElements.Count);
            Assert.Equal(X509ChainStatusFlags.Revoked, chain.ChainElements[0].AllStatusFlags());
            Assert.Equal(X509ChainStatusFlags.Revoked, chain.AllStatusFlags());
            Assert.False(chainBuilt, "Chain validated with revoked root self-test, EntireChain");

            holder.DisposeChainElements();
            chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
            chainBuilt = chain.Build(rootCert);

            Assert.Equal(1, chain.ChainElements.Count);
            Assert.Equal(X509ChainStatusFlags.NoError, chain.ChainElements[0].AllStatusFlags());
            Assert.Equal(X509ChainStatusFlags.NoError, chain.AllStatusFlags());
            Assert.True(chainBuilt, "Chain validated with revoked root self-test, ExcludeRoot");

            holder.DisposeChainElements();
            chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EndCertificateOnly;
            chainBuilt = chain.Build(rootCert);

            Assert.Equal(1, chain.ChainElements.Count);
            Assert.Equal(X509ChainStatusFlags.Revoked, chain.ChainElements[0].AllStatusFlags());
            Assert.Equal(X509ChainStatusFlags.Revoked, chain.AllStatusFlags());
            Assert.False(chainBuilt, "Chain validated with revoked root self-test, EndCertificateOnly");
        }

        private static void RunWithInconclusiveEndEntityRevocation(
            ChainHolder holder,
            X509Certificate2 endEntity)
        {
            X509Chain chain = holder.Chain;
            bool chainBuilt = chain.Build(endEntity);

            AssertChainStatus(
                chain,
                rootStatus: X509ChainStatusFlags.NoError,
                issrStatus: X509ChainStatusFlags.NoError,
                leafStatus: ThisOsRevocationStatusUnknown);

            Assert.False(chainBuilt, "Chain built with ExcludeRoot");
            holder.DisposeChainElements();

            chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EndCertificateOnly;

            chainBuilt = chain.Build(endEntity);

            AssertChainStatus(
                chain,
                rootStatus: X509ChainStatusFlags.NoError,
                issrStatus: X509ChainStatusFlags.NoError,
                leafStatus: ThisOsRevocationStatusUnknown);

            Assert.False(chainBuilt, "Chain built with EndCertificateOnly (without ignore flags)");
            holder.DisposeChainElements();

            chain.ChainPolicy.VerificationFlags |= X509VerificationFlags.IgnoreEndRevocationUnknown;

            chainBuilt = chain.Build(endEntity);

            AssertChainStatus(
                chain,
                rootStatus: X509ChainStatusFlags.NoError,
                issrStatus: X509ChainStatusFlags.NoError,
                leafStatus: ThisOsRevocationStatusUnknown);

            Assert.True(chainBuilt, "Chain built with EndCertificateOnly (with ignore flags)");
        }

        private static void RunWithInconclusiveIntermediateRevocation(
            ChainHolder holder,
            X509Certificate2 endEntity)
        {
            X509Chain chain = holder.Chain;
            bool chainBuilt = chain.Build(endEntity);

            AssertChainStatus(
                chain,
                rootStatus: X509ChainStatusFlags.NoError,
                issrStatus: ThisOsRevocationStatusUnknown,
                leafStatus: X509ChainStatusFlags.NoError);

            Assert.False(chainBuilt, "Chain built with ExcludeRoot (without flags)");
            holder.DisposeChainElements();

            chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EndCertificateOnly;

            chainBuilt = chain.Build(endEntity);

            AssertChainStatus(
                chain,
                rootStatus: X509ChainStatusFlags.NoError,
                issrStatus: X509ChainStatusFlags.NoError,
                leafStatus: X509ChainStatusFlags.NoError);

            Assert.True(chainBuilt, "Chain built with EndCertificateOnly");
            holder.DisposeChainElements();

            chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
            chain.ChainPolicy.VerificationFlags |=
                X509VerificationFlags.IgnoreCertificateAuthorityRevocationUnknown;

            chainBuilt = chain.Build(endEntity);

            AssertChainStatus(
                chain,
                rootStatus: X509ChainStatusFlags.NoError,
                issrStatus: ThisOsRevocationStatusUnknown,
                leafStatus: X509ChainStatusFlags.NoError);

            Assert.True(chainBuilt, "Chain built with ExcludeRoot (with ignore flags)");
        }

        private static void SimpleRevocationBody(
            ChainHolder holder,
            X509Certificate2 endEntityCert,
            bool rootRevoked,
            bool issrRevoked,
            bool leafRevoked,
            bool testWithRootRevocation = false)
        {
            X509Chain chain = holder.Chain;

            // This is the default mode, and probably already set right.
            chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;

            AssertRevocationLevel(chain, endEntityCert, false, issrRevoked, leafRevoked);
            holder.DisposeChainElements();

            // The next most common is to just check on the EE certificate.
            chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EndCertificateOnly;

            AssertRevocationLevel(chain, endEntityCert, false, false, leafRevoked);

            if (testWithRootRevocation)
            {
                holder.DisposeChainElements();

                // EntireChain is unusual to request, because Root revocation has little meaning.
                chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;

                AssertRevocationLevel(chain, endEntityCert, rootRevoked, issrRevoked, leafRevoked);
            }
        }

        private static void AssertRevocationLevel(
            X509Chain chain,
            X509Certificate2 endEntityCert,
            bool rootRevoked,
            bool issrRevoked,
            bool leafRevoked)
        {
            bool chainBuilt;

            if (rootRevoked)
            {
                chainBuilt = chain.Build(endEntityCert);

                AssertChainStatus(
                    chain,
                    rootStatus: X509ChainStatusFlags.Revoked,
                    issrStatus: ThisOsRevocationStatusUnknown,
                    leafStatus: ThisOsRevocationStatusUnknown);

                Assert.False(chainBuilt, $"Chain built under {chain.ChainPolicy.RevocationFlag}");
            }
            else if (issrRevoked)
            {
                chainBuilt = chain.Build(endEntityCert);

                AssertChainStatus(
                    chain,
                    rootStatus: X509ChainStatusFlags.NoError,
                    issrStatus: X509ChainStatusFlags.Revoked,
                    leafStatus: ThisOsRevocationStatusUnknown);

                Assert.False(chainBuilt, $"Chain built under {chain.ChainPolicy.RevocationFlag}");
            }
            else if (leafRevoked)
            {
                chainBuilt = chain.Build(endEntityCert);

                AssertChainStatus(
                    chain,
                    rootStatus: X509ChainStatusFlags.NoError,
                    issrStatus: X509ChainStatusFlags.NoError,
                    leafStatus: X509ChainStatusFlags.Revoked);

                Assert.False(chainBuilt, $"Chain built under {chain.ChainPolicy.RevocationFlag}");
            }
            else
            {
                chainBuilt = chain.Build(endEntityCert);

                AssertChainStatus(
                    chain,
                    rootStatus: X509ChainStatusFlags.NoError,
                    issrStatus: X509ChainStatusFlags.NoError,
                    leafStatus: X509ChainStatusFlags.NoError);

                Assert.True(chainBuilt, $"Chain built under {chain.ChainPolicy.RevocationFlag}");
            }
        }

        private static void SimpleTest(
            PkiOptions pkiOptions,
            RunSimpleTest callback,
            [CallerMemberName] string callerName = null,
            bool pkiOptionsInTestName = true)
        {
            BuildPrivatePki(
                pkiOptions,
                out RevocationResponder responder,
                out CertificateAuthority root,
                out CertificateAuthority intermediate,
                out X509Certificate2 endEntity,
                callerName,
                pkiOptionsInSubject: pkiOptionsInTestName);

            using (responder)
            using (root)
            using (intermediate)
            using (endEntity)
            using (ChainHolder holder = new ChainHolder())
            using (X509Certificate2 rootCert = root.CloneIssuerCert())
            using (X509Certificate2 intermediateCert = intermediate.CloneIssuerCert())
            {
                if (pkiOptions.HasFlag(PkiOptions.RootAuthorityHasDesignatedOcspResponder))
                {
                    using (RSA tmpKey = RSA.Create())
                    using (X509Certificate2 tmp = root.CreateOcspSigner(
                        BuildSubject("A Root Designated OCSP Responder", callerName, pkiOptions, true),
                        tmpKey))
                    {
                        root.DesignateOcspResponder(tmp.CopyWithPrivateKey(tmpKey));
                    }
                }

                if (pkiOptions.HasFlag(PkiOptions.IssuerAuthorityHasDesignatedOcspResponder))
                {
                    using (RSA tmpKey = RSA.Create())
                    using (X509Certificate2 tmp = intermediate.CreateOcspSigner(
                        BuildSubject("An Intermediate Designated OCSP Responder", callerName, pkiOptions, true),
                        tmpKey))
                    {
                        intermediate.DesignateOcspResponder(tmp.CopyWithPrivateKey(tmpKey));
                    }
                }

                X509Chain chain = holder.Chain;
                chain.ChainPolicy.CustomTrustStore.Add(rootCert);
                chain.ChainPolicy.ExtraStore.Add(intermediateCert);
                chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                chain.ChainPolicy.VerificationTime = endEntity.NotBefore.AddMinutes(1);
                chain.ChainPolicy.UrlRetrievalTimeout = s_urlRetrievalLimit;

                callback(root, intermediate, endEntity, holder);
            }
        }

        private static void AssertChainStatus(
            X509Chain chain,
            X509ChainStatusFlags rootStatus,
            X509ChainStatusFlags issrStatus,
            X509ChainStatusFlags leafStatus)
        {
            Assert.Equal(3, chain.ChainElements.Count);

            X509ChainStatusFlags allFlags = rootStatus | issrStatus | leafStatus;
            X509ChainStatusFlags chainActual = chain.AllStatusFlags();

            X509ChainStatusFlags rootActual = chain.ChainElements[2].AllStatusFlags();
            X509ChainStatusFlags issrActual = chain.ChainElements[1].AllStatusFlags();
            X509ChainStatusFlags leafActual = chain.ChainElements[0].AllStatusFlags();

            // If things don't match, build arrays so the errors pretty print the full chain.
            if (rootActual != rootStatus ||
                issrActual != issrStatus ||
                leafActual != leafStatus ||
                chainActual != allFlags)
            {
                X509ChainStatusFlags[] expected = { rootStatus, issrStatus, leafStatus };
                X509ChainStatusFlags[] actual = { rootActual, issrActual, leafActual };

                Assert.Equal(expected, actual);
                Assert.Equal(allFlags, chainActual);
            }
        }

        internal static void BuildPrivatePki(
            PkiOptions pkiOptions,
            out RevocationResponder responder,
            out CertificateAuthority rootAuthority,
            out CertificateAuthority intermediateAuthority,
            out X509Certificate2 endEntityCert,
            [CallerMemberName] string testName = null,
            bool registerAuthorities = true,
            bool pkiOptionsInSubject = false)
        {
            bool issuerRevocationViaCrl = pkiOptions.HasFlag(PkiOptions.IssuerRevocationViaCrl);
            bool issuerRevocationViaOcsp = pkiOptions.HasFlag(PkiOptions.IssuerRevocationViaOcsp);
            bool endEntityRevocationViaCrl = pkiOptions.HasFlag(PkiOptions.EndEntityRevocationViaCrl);
            bool endEntityRevocationViaOcsp = pkiOptions.HasFlag(PkiOptions.EndEntityRevocationViaOcsp);

            Assert.True(
                issuerRevocationViaCrl || issuerRevocationViaOcsp ||
                    endEntityRevocationViaCrl || endEntityRevocationViaOcsp,
                "At least one revocation mode is enabled");

            CertificateAuthority.BuildPrivatePki(pkiOptions, out responder, out rootAuthority, out intermediateAuthority, out endEntityCert, testName, registerAuthorities, pkiOptionsInSubject);
        }

        private static string BuildSubject(
            string cn,
            string testName,
            PkiOptions pkiOptions,
            bool includePkiOptions)
        {
            if (includePkiOptions)
            {
                return $"CN=\"{cn}\", O=\"{testName}\", OU=\"{pkiOptions}\"";
            }

            return $"CN=\"{cn}\", O=\"{testName}\"";
        }
    }
}
