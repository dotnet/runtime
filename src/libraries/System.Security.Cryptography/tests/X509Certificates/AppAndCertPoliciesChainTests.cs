// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Browser doesn't support X.509 certificates")]
    public static class AppAndCertPoliciesChainTests
    {
        private const string ApplicationCertPoliciesOid = "1.3.6.1.4.1.311.21.10";

        [Theory]
        [MemberData(nameof(ChainPolicyMemberData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/128890", TestPlatforms.Android)]
        public static void CertificatePolicyTest(
           ChainPolicyTestCase testCase)
        {
            using (testCase)
            {
                DynamicChainTests.TestChain4(
                    testCase.Root,
                    testCase.HighIntermediate,
                    testCase.LowIntermediate,
                    testCase.EndEntity,
                    DynamicChainTests.PlatformPolicyConstraints(testCase.ExpectedFlags),
                    testCase.ConfigureCallback);
            }
        }

        public static IEnumerable<object[]> ChainPolicyMemberData()
        {
            foreach (ChainPolicyTestCase testCase in TestCases())
            {
                yield return new object[] { testCase };
            }

            static IEnumerable<ChainPolicyTestCase> TestCases()
            {
                // Use the same keys for all chains, just to keep the total time low.
                // There are enough cases here that keygen shows up in clock time.
                RSA[] keys = [RSA.Create(2048), RSA.Create(2048), RSA.Create(2048), RSA.Create(2048)];

                // No chain.Policy.CertificatePolicy checks, EE uses the mapped policy identifier,
                // intermediate requires that the EE certs have a policy extension.
                //
                // Despite the intermediate specifying a mapping when it's forbidden (inhibit<2),
                // Everything is reported valid, because the EE cert policy C doesn't require the mapping.
                for (int rootInhibitMapping = 0; rootInhibitMapping <= 2; rootInhibitMapping++)
                {
                    yield return ChainPolicyTestCase.Build(
                        $"NoPolicyCheck/EEUsesMappedPolicyAndExtra/PolicyRequired/RootInhibit={rootInhibitMapping}",
                        keys,
                        rootInhibitMapping,
                        0,
                        [ChainPolicyTestCase.PolicyB, ChainPolicyTestCase.PolicyC],
                        [],
                        X509ChainStatusFlags.NoError);
                }

                // No chain.Policy.CertificatePolicy checks, EE uses the mapped policy identifier,
                // intermediate requires that the EE certs have a policy extension.
                //
                // The EE policy is required, and the only policy in the EE cert is the mapped one,
                // but that is forbidden by the intermediate's inhibit=<2, so it's an
                // Issuance-Chain-Policy violation.
                for (int rootInhibitMapping = 0; rootInhibitMapping <= 2; rootInhibitMapping++)
                {
                    // Windows seems to be the only OS capable of reporting this error.

                    yield return ChainPolicyTestCase.Build(
                        $"NoPolicyCheck/EEUsesMappedPolicyOnly/PolicyRequired/RootInhibit={rootInhibitMapping}",
                        keys,
                        rootInhibitMapping,
                        0,
                        [ChainPolicyTestCase.PolicyB],
                        [],
                        rootInhibitMapping < 2 && OperatingSystem.IsWindows() ?
                            X509ChainStatusFlags.NoIssuanceChainPolicy :
                            X509ChainStatusFlags.NoError);
                }

                // No chain.Policy.CertificatePolicy checks, EE uses the mapped policy identifier,
                // intermediate does not require that the EE certs have a policy extension.
                //
                // Even though the intermediate has a disallowed mapping when inhibit=<2,
                // since the EE isn't _required_ to have a policy, nothing was required to
                // traverse the map.
                for (int rootInhibitMapping = 0; rootInhibitMapping <= 2; rootInhibitMapping++)
                {
                    yield return ChainPolicyTestCase.Build(
                        $"NoPolicyCheck/EEUsesMappedPolicyOnly/PolicyOptional/RootInhibit={rootInhibitMapping}",
                        keys,
                        rootInhibitMapping,
                        -1,
                        [ChainPolicyTestCase.PolicyB],
                        [],
                        X509ChainStatusFlags.NoError);
                }

                // EE uses the mapped policy identifier,
                // intermediate requires that the EE certs have a policy extension.
                // Require that the EE cert is valid for only policy C (no mapping required).
                //
                // Despite the intermediate specifying a mapping when it's forbidden (inhibit=<2),
                // Everything is reported valid, because the EE cert policy C doesn't require the mapping.
                for (int rootInhibitMapping = 0; rootInhibitMapping <= 2; rootInhibitMapping++)
                {
                    yield return ChainPolicyTestCase.Build(
                        $"CheckExtraPolicy/EEUsesMappedPolicyAndExtra/PolicyRequired/RootInhibit={rootInhibitMapping}",
                        keys,
                        rootInhibitMapping,
                        0,
                        [ChainPolicyTestCase.PolicyB, ChainPolicyTestCase.PolicyC],
                        [ChainPolicyTestCase.PolicyC],
                        X509ChainStatusFlags.NoError);
                }

                // EE uses the mapped policy identifier,
                // intermediate does not require that the EE certs have a policy extension.
                // Require that the EE cert is valid for only policy C (no mapping required).
                //
                // Despite the intermediate specifying a mapping when it's forbidden (inhibit=<2),
                // Everything is reported valid, because the EE cert policy C doesn't require the mapping.
                for (int rootInhibitMapping = 0; rootInhibitMapping <= 2; rootInhibitMapping++)
                {
                    yield return ChainPolicyTestCase.Build(
                        $"CheckExtraPolicy/EEUsesMappedPolicyAndExtra/PolicyOptional/RootInhibit={rootInhibitMapping}",
                        keys,
                        rootInhibitMapping,
                        -1,
                        [ChainPolicyTestCase.PolicyB, ChainPolicyTestCase.PolicyC],
                        [ChainPolicyTestCase.PolicyC],
                        X509ChainStatusFlags.NoError);
                }

                // EE uses the mapped policy identifier,
                // intermediate requires that the EE certs have a policy extension.
                // Require that the EE cert is valid for only policy A (which it calls B).
                //
                // Since this requires traversing the mapping, it's NotValidForUsage whenever
                // the mapping was disallowed (inhibit<2).
                for (int rootInhibitMapping = 0; rootInhibitMapping <= 2; rootInhibitMapping++)
                {
                    yield return ChainPolicyTestCase.Build(
                        $"CheckMappedPolicy/EEUsesMappedPolicyAndExtra/PolicyRequired/RootInhibit={rootInhibitMapping}",
                        keys,
                        rootInhibitMapping,
                        0,
                        [ChainPolicyTestCase.PolicyB, ChainPolicyTestCase.PolicyC],
                        [ChainPolicyTestCase.PolicyA],
                        rootInhibitMapping < 2 ?
                            X509ChainStatusFlags.NotValidForUsage :
                            X509ChainStatusFlags.NoError);
                }

                foreach (RSA key in keys)
                {
                    key.Dispose();
                }
            }
        }

        public sealed class ChainPolicyTestCase : IDisposable
        {
            internal const string PolicyA = "0.1.2.3";
            internal const string PolicyB = "1.2.3.4";
            internal const string PolicyC = "2.3.4.5";

            private string _name;
            private string[] _eePoliciesToCheck;

            internal X509ChainStatusFlags ExpectedFlags { get; private set; }
            internal X509Certificate2 Root { get; private set; }
            internal X509Certificate2 HighIntermediate { get; private set; }
            internal X509Certificate2 LowIntermediate { get; private set; }
            internal X509Certificate2 EndEntity { get; private set; }

            private ChainPolicyTestCase()
            {
            }

            public Action<X509ChainPolicy> ConfigureCallback
            {
                get
                {
                    if (_eePoliciesToCheck.Length == 0)
                    {
                        return null;
                    }

                    return policy =>
                    {
                        foreach (string policyOid in _eePoliciesToCheck)
                        {
                            policy.CertificatePolicy.Add(new Oid(policyOid, null));
                        }
                    };
                }
            }

            public void Dispose()
            {
                Root?.Dispose();
                HighIntermediate?.Dispose();
                LowIntermediate?.Dispose();
                EndEntity?.Dispose();
            }

            internal static ChainPolicyTestCase Build(
                string name,
                RSA[] keys,
                int rootInhibitMapping,
                int intermediateRequireExplicit,
                string[] eePolicies,
                string[] eePoliciesToCheck,
                X509ChainStatusFlags expectedFlags)
            {
                X509Extension[] rootExtensions = new[]
                {
                    X509BasicConstraintsExtension.CreateForCertificateAuthority(),
                    MaybePolicyConstraints(inhibitPolicyMappingSkipCerts: rootInhibitMapping),
                };

                X509Extension[] highImedExtensions = new[]
                {
                    X509BasicConstraintsExtension.CreateForCertificateAuthority(),
                    // The "any" policy.
                    DynamicChainTests.BuildPolicyByIdentifiers("2.5.29.32.0"),
                };

                X509Extension[] lowImedExtensions = new[]
                {
                    X509BasicConstraintsExtension.CreateForCertificateAuthority(), 
                    MaybePolicyConstraints(requireExplicitPolicySkipCerts: intermediateRequireExplicit),
                    DynamicChainTests.BuildPolicyByIdentifiers(PolicyA, PolicyC),
                    DynamicChainTests.BuildPolicyMappings((PolicyA, PolicyB)),
                };

                X509Extension[] endEntityExtensions = new[]
                {
                    X509BasicConstraintsExtension.CreateForEndEntity(),
                    MaybePolicies(eePolicies),
                };

                X509Certificate2[] certs = new X509Certificate2[4];

                TestDataGenerator.MakeTestChain(
                    keys,
                    certs,
                    endEntityExtensions,
                    [lowImedExtensions, highImedExtensions],
                    rootExtensions,
                    name);

                return new ChainPolicyTestCase
                {
                    _name = name,
                    EndEntity = certs[0],
                    LowIntermediate = certs[1],
                    HighIntermediate = certs[2],
                    Root = certs[3],
                    _eePoliciesToCheck = eePoliciesToCheck,
                    ExpectedFlags = expectedFlags,
                };

                static X509Extension MaybePolicies(string[] policyOids)
                {
                    if (policyOids.Length == 0)
                    {
                        return null;
                    }

                    return DynamicChainTests.BuildPolicyByIdentifiers(policyOids);
                }

                static X509Extension MaybePolicyConstraints(
                    int requireExplicitPolicySkipCerts = -1,
                    int inhibitPolicyMappingSkipCerts = -1)
                {
                    if (inhibitPolicyMappingSkipCerts >= 0 && requireExplicitPolicySkipCerts >= 0)
                    {
                        return DynamicChainTests.BuildPolicyConstraints(inhibitPolicyMappingSkipCerts, requireExplicitPolicySkipCerts);
                    }

                    if (inhibitPolicyMappingSkipCerts >= 0)
                    {
                        return DynamicChainTests.BuildPolicyConstraints(inhibitPolicyMappingSkipCerts: inhibitPolicyMappingSkipCerts);
                    }

                    if (requireExplicitPolicySkipCerts >= 0)
                    {
                        return DynamicChainTests.BuildPolicyConstraints(requireExplicitPolicySkipCerts: requireExplicitPolicySkipCerts);
                    }

                    return null;
                }
            }

            public override string ToString()
            {
                return _name;
            }
        }

        // Explores how the Microsoft Application Policies extension (szOID_APPLICATION_CERT_POLICIES,
        // 1.3.6.1.4.1.311.21.10) interacts with the standard EKU (2.5.29.37) extension when filtering
        // a chain via X509ChainPolicy.ApplicationPolicy. Summary of the intended behavior:
        //   * Application Policies absent  -> EKU governs (with anyEKU 2.5.29.37.0 as the wildcard).
        //   * Application Policies present -> it is authoritative; EKU is ignored. Its only wildcard
        //     is anyExtendedKeyUsage (2.5.29.37.0); anyPolicy (2.5.29.32.0) matches nothing.
        //   * Application Policies present but empty -> authoritative empty set (matches nothing).
        //   * Application Policies present but undecodable -> the chain is invalid outright,
        //     regardless of EKU, criticality, or whether any application policy was requested.
        [Theory]
        [MemberData(nameof(ApplicationPolicyVsEkuMemberData))]
        public static void VerifyApplicationPolicyVsEku(AppPolicyEkuCase testCase)
        {
            X509Certificate2 rootCert = testCase.Root;
            X509Certificate2 intermediateCert = testCase.Intermediate;

            CertificateRequest request = new CertificateRequest(
                "CN=App Policy vs EKU Test End-Entity",
                s_endEntityKey,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            request.CertificateExtensions.Add(X509BasicConstraintsExtension.CreateForEndEntity());

            if (testCase.EkuOids is not null)
            {
                OidCollection oids = new OidCollection();

                foreach (string oid in testCase.EkuOids)
                {
                    oids.Add(new Oid(oid, null));
                }

                request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(oids, critical: false));
            }

            if (testCase.ApplicationPolicyValue is not null)
            {
                // The managed application policy verifier doesn't take criticality into account, and
                // Android rejects the extension if it's marked critical, so only use non-critical for the test.
                request.CertificateExtensions.Add(
                    new X509Extension(
                        ApplicationCertPoliciesOid,
                        testCase.ApplicationPolicyValue,
                        critical: false));
            }

            DateTimeOffset notBefore = DateTimeOffset.UtcNow.AddDays(-1);
            DateTimeOffset notAfter = notBefore.AddDays(30);

            using (X509Certificate2 endEntityCert =
                request.Create(intermediateCert, notBefore, notAfter, CreateTestSerial()))
            {
                DynamicChainTests.TestChain3(
                    rootCert,
                    intermediateCert,
                    endEntityCert,
                    testCase.ExpectedFlags,
                    testCase.RequestedApplicationPolicyOid is null
                        ? null
                        : policy => policy.ApplicationPolicy.Add(new Oid(testCase.RequestedApplicationPolicyOid, null)));
            }
        }

        // The end-entity subject key is irrelevant to what these cases exercise (the intermediate signs
        // the end-entity cert), so a single fixed key is imported once and reused for every case.
        private static readonly RSA s_endEntityKey = CreateEndEntityKey();

        private static RSA CreateEndEntityKey()
        {
            RSA rsa = RSA.Create();
            return rsa;
        }

        // A single shared root + issuing intermediate is generated once for the whole VerifyApplicationPolicyVsEku
        // theory. Only the end-entity certificate differs between cases, so it is (re)issued in the test body.
        private static readonly Lazy<(X509Certificate2 Root, X509Certificate2 Intermediate)> s_appPolicyIssuers =
            new Lazy<(X509Certificate2, X509Certificate2)>(CreateAppPolicyIssuers);

        private static (X509Certificate2 Root, X509Certificate2 Intermediate) CreateAppPolicyIssuers()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            using (RSA rootKey = RSA.Create(2048))
            using (RSA intermediateKey = RSA.Create(2048))
            {

                CertificateRequest rootRequest = new CertificateRequest(
                    "CN=App Policy vs EKU Test Root",
                    rootKey,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                rootRequest.CertificateExtensions.Add(X509BasicConstraintsExtension.CreateForCertificateAuthority());
                rootRequest.CertificateExtensions.Add(
                    new X509KeyUsageExtension(
                        X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign,
                        critical: false));

                X509Certificate2 root = rootRequest.CreateSelfSigned(now.AddDays(-45), now.AddDays(365));

                CertificateRequest intermediateRequest = new CertificateRequest(
                    "CN=App Policy vs EKU Test Intermediate",
                    intermediateKey,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                intermediateRequest.CertificateExtensions.Add(X509BasicConstraintsExtension.CreateForCertificateAuthority());
                intermediateRequest.CertificateExtensions.Add(
                    new X509KeyUsageExtension(
                        X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign,
                        critical: false));

                X509Certificate2 intermediate;

                using (X509Certificate2 intermediatePublic =
                    intermediateRequest.Create(root, now.AddDays(-40), now.AddDays(180), CreateTestSerial()))
                {
                    intermediate = intermediatePublic.CopyWithPrivateKey(intermediateKey);
                }

                return (root, intermediate);
            }
        }

        private static byte[] CreateTestSerial()
        {
            byte[] serial = new byte[8];
            RandomNumberGenerator.Fill(serial);

            // Keep the high bit clear so the serial encodes as a positive INTEGER.
            serial[0] &= 0x7F;

            if (serial[0] == 0)
            {
                serial[0] = 1;
            }

            return serial;
        }

        public static IEnumerable<object[]> ApplicationPolicyVsEkuMemberData()
        {
            const string ServerAuth = "1.3.6.1.5.5.7.3.1";
            const string ClientAuth = "1.3.6.1.5.5.7.3.2";
            const string TimeStamp = "1.3.6.1.5.5.7.3.8"; // RFC 3161, used only as a companion value
            const string AnyEku = "2.5.29.37.0";           // anyExtendedKeyUsage
            const string AnyPolicy = "2.5.29.32.0";         // anyPolicy (certificate policies)

            const X509ChainStatusFlags Ok = X509ChainStatusFlags.NoError;
            const X509ChainStatusFlags Usage = X509ChainStatusFlags.NotValidForUsage;
            const X509ChainStatusFlags BadExt =
                X509ChainStatusFlags.InvalidExtension | X509ChainStatusFlags.InvalidPolicyConstraints;

            // Well-formed Application Policies extension value carrying the given usage OIDs.
            static byte[] EncPol(params string[] oids) => DynamicChainTests.EncodeCertificatePoliciesValue(oids);

            AppPolicyEkuCase[] cases =
            {
                // Baseline: EKU only (sanity, including TLS Server Auth).
                new AppPolicyEkuCase("no restrictions; req=Server", null, null, ServerAuth, Ok),
                new AppPolicyEkuCase("EKU=Server; req=Server", new[] { ServerAuth }, null, ServerAuth, Ok),
                new AppPolicyEkuCase("EKU=Server; req=Client", new[] { ServerAuth }, null, ClientAuth, Usage),
                new AppPolicyEkuCase("EKU=Client; req=Server", new[] { ClientAuth }, null, ServerAuth, Usage),
                new AppPolicyEkuCase("EKU=Client; req=none", new[] { ClientAuth }, null, null, Ok),
                new AppPolicyEkuCase("EKU=anyEKU; req=Server", new[] { AnyEku }, null, ServerAuth, Ok),
                new AppPolicyEkuCase("EKU=anyEKU; req=Client", new[] { AnyEku }, null, ClientAuth, Ok),
                new AppPolicyEkuCase("EKU=anyEKU,TS; req=Client", new[] { AnyEku, TimeStamp }, null, ClientAuth, Ok),
                new AppPolicyEkuCase("EKU=anyPolicy(32.0); req=Server", new[] { AnyPolicy }, null, ServerAuth, Usage),

                // Application Policies only (no EKU): behaves like the same EKU.
                new AppPolicyEkuCase("AppPol=Server; req=Server", null, EncPol(ServerAuth), ServerAuth, Ok),
                new AppPolicyEkuCase("AppPol=Server; req=Client", null, EncPol(ServerAuth), ClientAuth, Usage),
                new AppPolicyEkuCase("AppPol=anyEKU(37.0); req=Server", null, EncPol(AnyEku), ServerAuth, Ok),
                new AppPolicyEkuCase("AppPol=anyEKU(37.0); req=Client", null, EncPol(AnyEku), ClientAuth, Ok),
                new AppPolicyEkuCase("AppPol=anyEKU(37.0),TS; req=Server", null, EncPol(AnyEku, TimeStamp), ServerAuth, Ok),
                new AppPolicyEkuCase("AppPol=anyPolicy(32.0); req=Server", null, EncPol(AnyPolicy), ServerAuth, Usage),
                new AppPolicyEkuCase("AppPol=anyPolicy(32.0); req=Client", null, EncPol(AnyPolicy), ClientAuth, Usage),
                new AppPolicyEkuCase("AppPol=TS,anyPolicy(32.0); req=Server", null, EncPol(TimeStamp, AnyPolicy), ServerAuth, Usage),

                // Conflicts: Application Policies overrides EKU entirely.
                new AppPolicyEkuCase("EKU=Server AppPol=Client; req=Server", new[] { ServerAuth }, EncPol(ClientAuth), ServerAuth, Usage),
                new AppPolicyEkuCase("EKU=Server AppPol=Client; req=Client", new[] { ServerAuth }, EncPol(ClientAuth), ClientAuth, Ok),
                new AppPolicyEkuCase("EKU=Client AppPol=Server; req=Server", new[] { ClientAuth }, EncPol(ServerAuth), ServerAuth, Ok),
                new AppPolicyEkuCase("EKU=Client AppPol=Server; req=Client", new[] { ClientAuth }, EncPol(ServerAuth), ClientAuth, Usage),
                new AppPolicyEkuCase("EKU=anyEKU AppPol=Client; req=Server", new[] { AnyEku }, EncPol(ClientAuth), ServerAuth, Usage),
                new AppPolicyEkuCase("EKU=Client AppPol=anyEKU(37.0),TS; req=Server", new[] { ClientAuth }, EncPol(AnyEku, TimeStamp), ServerAuth, Ok),
                new AppPolicyEkuCase("EKU=Client AppPol=anyEKU(37.0),TS; req=Client", new[] { ClientAuth }, EncPol(AnyEku, TimeStamp), ClientAuth, Ok),

                // Well-formed but empty Application Policies: authoritative empty set (matches nothing).
                new AppPolicyEkuCase("AppPol=empty EKU=Server; req=Server", new[] { ServerAuth }, EncPol(), ServerAuth, Usage),
                new AppPolicyEkuCase("AppPol=empty; req=none", null, EncPol(), null, Ok),

                // Undecodable Application Policies: hard failure regardless of EKU / criticality / requested usage.
                new AppPolicyEkuCase("AppPol=NULL(05 00); req=none", null, new byte[] { 0x05, 0x00 }, null, BadExt),
                new AppPolicyEkuCase("AppPol=NULL(05 00) EKU=Server; req=Server", new[] { ServerAuth }, new byte[] { 0x05, 0x00 }, ServerAuth, BadExt),
                new AppPolicyEkuCase("AppPol=badInner EKU=Server; req=Server", new[] { ServerAuth }, new byte[] { 0x30, 0x03, 0x02, 0x01, 0x2A }, ServerAuth, BadExt),
                new AppPolicyEkuCase("AppPol=truncated EKU=Server; req=Server", new[] { ServerAuth }, new byte[] { 0x30, 0x82, 0x7F, 0xFF }, ServerAuth, BadExt),
            };

            foreach (AppPolicyEkuCase testCase in cases)
            {
                (testCase.Root, testCase.Intermediate) = s_appPolicyIssuers.Value;
                yield return new object[] { testCase };
            }
        }

        public sealed class AppPolicyEkuCase
        {
            public string Name { get; }
            public string[] EkuOids { get; }
            public byte[] ApplicationPolicyValue { get; }
            public string RequestedApplicationPolicyOid { get; }
            public X509ChainStatusFlags ExpectedFlags { get; }

            // Shared across all cases; assigned by the member-data generator.
            public X509Certificate2 Root { get; set; }
            public X509Certificate2 Intermediate { get; set; }

            public AppPolicyEkuCase(
                string name,
                string[] ekuOids,
                byte[] applicationPolicyValue,
                string requestedApplicationPolicyOid,
                X509ChainStatusFlags expectedFlags)
            {
                Name = name;
                EkuOids = ekuOids;
                ApplicationPolicyValue = applicationPolicyValue;
                RequestedApplicationPolicyOid = requestedApplicationPolicyOid;
                ExpectedFlags = expectedFlags;
            }

            public override string ToString() => Name;
        }
    }
}
