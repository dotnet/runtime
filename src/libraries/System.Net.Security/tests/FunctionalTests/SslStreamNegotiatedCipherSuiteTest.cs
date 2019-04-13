// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Test.Common;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Net.Security.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public class NegotiatedCipherSuiteTest
    {
#pragma warning disable CS0618 // Ssl2 and Ssl3 are obsolete
        private const SslProtocols AllProtocols =
            SslProtocols.Ssl2 | SslProtocols.Ssl3 |
            SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12 | SslProtocols.Tls13;
#pragma warning restore CS0618

        private const SslProtocols NonTls13Protocols = AllProtocols & (~SslProtocols.Tls13);

        private static bool IsKnownPlatformSupportingTls13 => PlatformDetection.IsUbuntu1810OrHigher;
        private static bool CipherSuitesPolicySupported => s_cipherSuitePolicySupported.Value;
        private static bool Tls13Supported { get; set; } = IsKnownPlatformSupportingTls13 || ProtocolsSupported(SslProtocols.Tls13);
        private static bool CipherSuitesPolicyAndTls13Supported => Tls13Supported && CipherSuitesPolicySupported;

        private static HashSet<TlsCipherSuite> s_tls13CipherSuiteLookup = new HashSet<TlsCipherSuite>(GetTls13CipherSuites());
        private static HashSet<TlsCipherSuite> s_tls12CipherSuiteLookup = new HashSet<TlsCipherSuite>(GetTls12CipherSuites());
        private static HashSet<TlsCipherSuite> s_tls10And11CipherSuiteLookup = new HashSet<TlsCipherSuite>(GetTls10And11CipherSuites());

        private static Dictionary<SslProtocols, HashSet<TlsCipherSuite>> s_protocolCipherSuiteLookup = new Dictionary<SslProtocols, HashSet<TlsCipherSuite>>()
        {
            { SslProtocols.Tls12, s_tls12CipherSuiteLookup },
            { SslProtocols.Tls11, s_tls10And11CipherSuiteLookup },
            { SslProtocols.Tls, s_tls10And11CipherSuiteLookup },
        };

        private static Lazy<bool> s_cipherSuitePolicySupported = new Lazy<bool>(() =>
        {
            try
            {
                new CipherSuitesPolicy(Array.Empty<TlsCipherSuite>());
                return true;
            }
            catch (PlatformNotSupportedException) { }

            return false;
        });

        private static IReadOnlyList<TlsCipherSuite> SupportedNonTls13CipherSuites = GetSupportedNonTls13CipherSuites();

        [ConditionalFact(nameof(IsKnownPlatformSupportingTls13))]
        public void Tls13IsSupported_GetValue_ReturnsTrue()
        {
            // Validate that flag used in this file works correctly
            Assert.True(Tls13Supported);
        }

        [ConditionalFact(nameof(Tls13Supported))]
        public void NegotiatedCipherSuite_SslProtocolIsTls13_ShouldBeTls13()
        {
            var p = new ConnectionParams()
            {
                SslProtocols = SslProtocols.Tls13
            };

            NegotiatedParams ret = ConnectAndGetNegotiatedParams(p, p);
            ret.Succeeded();

            Assert.True(
                s_tls13CipherSuiteLookup.Contains(ret.CipherSuite),
                $"`{ret.CipherSuite}` is not recognized as TLS 1.3 cipher suite");
        }

        [Theory]
        [InlineData(SslProtocols.Tls)]
        [InlineData(SslProtocols.Tls11)]
        [InlineData(SslProtocols.Tls12)]
        public void NegotiatedCipherSuite_SslProtocolIsLowerThanTls13_ShouldMatchTheProtocol(SslProtocols protocol)
        {
            var p = new ConnectionParams()
            {
                SslProtocols = protocol
            };

            NegotiatedParams ret = ConnectAndGetNegotiatedParams(p, p);
            ret.Succeeded();

            Assert.True(
                s_protocolCipherSuiteLookup[protocol].Contains(ret.CipherSuite),
                $"`{ret.CipherSuite}` is not recognized as {protocol} cipher suite");
        }

        [Fact]
        public void NegotiatedCipherSuite_BeforeNegotiationStarted_ShouldThrow()
        {
            using (var ms = new MemoryStream())
            using (var server = new SslStream(ms, leaveInnerStreamOpen: false))
            {
                Assert.Throws<InvalidOperationException>(() => server.NegotiatedCipherSuite);
            }
        }

        [ConditionalFact(nameof(CipherSuitesPolicySupported))]
        public void CipherSuitesPolicy_AllowSomeCipherSuitesWithNoEncryptionOption_Fails()
        {
            CheckPrereqsForNonTls13Tests(1);
            var p = new ConnectionParams()
            {
                CipherSuitesPolicy = BuildPolicy(TlsCipherSuite.TLS_AES_128_GCM_SHA256,
                                                 SupportedNonTls13CipherSuites[0]),
                EncryptionPolicy = EncryptionPolicy.NoEncryption,
            };

            NegotiatedParams ret = ConnectAndGetNegotiatedParams(p, p);
            ret.Failed();
        }

        [ConditionalFact(nameof(CipherSuitesPolicySupported))]
        public void CipherSuitesPolicy_NothingAllowed_Fails()
        {
            CipherSuitesPolicy csp = BuildPolicy();

            var sp = new ConnectionParams();
            sp.CipherSuitesPolicy = csp;

            var cp = new ConnectionParams();
            cp.CipherSuitesPolicy = csp;

            NegotiatedParams ret = ConnectAndGetNegotiatedParams(sp, cp);
            ret.Failed();
        }

        [ConditionalFact(nameof(CipherSuitesPolicyAndTls13Supported))]
        public void CipherSuitesPolicy_AllowOneOnOneSideTls13_Success()
        {
            bool hasSucceededAtLeastOnce = false;
            AllowOneOnOneSide(GetTls13CipherSuites(),
                              RequiredByTls13Spec,
                              (cs) => hasSucceededAtLeastOnce = true);
            Assert.True(hasSucceededAtLeastOnce);
        }

        [ConditionalFact(nameof(CipherSuitesPolicySupported))]
        public void CipherSuitesPolicy_AllowTwoOnBothSidesWithSingleOverlapNonTls13_Success()
        {
            CheckPrereqsForNonTls13Tests(3);
            var a = new ConnectionParams()
            {
                CipherSuitesPolicy = BuildPolicy(SupportedNonTls13CipherSuites[0],
                                                 SupportedNonTls13CipherSuites[1])
            };
            var b = new ConnectionParams()
            {
                CipherSuitesPolicy = BuildPolicy(SupportedNonTls13CipherSuites[1],
                                                 SupportedNonTls13CipherSuites[2])
            };

            for (int i = 0; i < 2; i++)
            {
                NegotiatedParams ret = i == 0 ?
                    ConnectAndGetNegotiatedParams(a, b) :
                    ConnectAndGetNegotiatedParams(b, a);

                ret.Succeeded();
                ret.CheckCipherSuite(SupportedNonTls13CipherSuites[1]);
            }
        }

        [ConditionalFact(nameof(CipherSuitesPolicySupported))]
        public void CipherSuitesPolicy_AllowTwoOnBothSidesWithNoOverlapNonTls13_Fails()
        {
            CheckPrereqsForNonTls13Tests(4);
            var a = new ConnectionParams()
            {
                CipherSuitesPolicy = BuildPolicy(SupportedNonTls13CipherSuites[0],
                                                 SupportedNonTls13CipherSuites[1])
            };
            var b = new ConnectionParams()
            {
                CipherSuitesPolicy = BuildPolicy(SupportedNonTls13CipherSuites[2],
                                                 SupportedNonTls13CipherSuites[3])
            };

            for (int i = 0; i < 2; i++)
            {
                NegotiatedParams ret = i == 0 ?
                    ConnectAndGetNegotiatedParams(a, b) :
                    ConnectAndGetNegotiatedParams(b, a);

                ret.Failed();
            }
        }

        [ConditionalFact(nameof(CipherSuitesPolicySupported))]
        public void CipherSuitesPolicy_AllowSameTwoOnBothSidesLessPreferredIsTls13_Success()
        {
            CheckPrereqsForNonTls13Tests(1);
            var p = new ConnectionParams()
            {
                CipherSuitesPolicy = BuildPolicy(SupportedNonTls13CipherSuites[0],
                                                 TlsCipherSuite.TLS_AES_128_GCM_SHA256)
            };

            NegotiatedParams ret = ConnectAndGetNegotiatedParams(p, p);
            ret.Succeeded();

            // If both sides can speak TLS 1.3 they should speak it
            if (Tls13Supported)
            {
                ret.CheckCipherSuite(TlsCipherSuite.TLS_AES_128_GCM_SHA256);
            }
            else
            {
                ret.CheckCipherSuite(SupportedNonTls13CipherSuites[0]);
            }
        }

        [ConditionalFact(nameof(CipherSuitesPolicySupported))]
        public void CipherSuitesPolicy_TwoCipherSuitesWithAllOverlapping_Success()
        {
            CheckPrereqsForNonTls13Tests(2);
            var a = new ConnectionParams()
            {
                CipherSuitesPolicy = BuildPolicy(SupportedNonTls13CipherSuites[0],
                                                 SupportedNonTls13CipherSuites[1])
            };
            var b = new ConnectionParams()
            {
                CipherSuitesPolicy = BuildPolicy(SupportedNonTls13CipherSuites[1],
                                                 SupportedNonTls13CipherSuites[0])
            };

            for (int i = 0; i < 2; i++)
            {
                bool isAClient = i == 0;
                NegotiatedParams ret = isAClient ?
                    ConnectAndGetNegotiatedParams(b, a) :
                    ConnectAndGetNegotiatedParams(a, b);

                ret.Succeeded();
                Assert.True(ret.CipherSuite == SupportedNonTls13CipherSuites[0] ||
                            ret.CipherSuite == SupportedNonTls13CipherSuites[1]);
            }
        }

        [ConditionalFact(nameof(CipherSuitesPolicySupported))]
        public void CipherSuitesPolicy_ThreeCipherSuitesWithTwoOverlapping_Success()
        {
            CheckPrereqsForNonTls13Tests(4);
            var a = new ConnectionParams()
            {
                CipherSuitesPolicy = BuildPolicy(SupportedNonTls13CipherSuites[0],
                                                 SupportedNonTls13CipherSuites[1],
                                                 SupportedNonTls13CipherSuites[2])
            };
            var b = new ConnectionParams()
            {
                CipherSuitesPolicy = BuildPolicy(SupportedNonTls13CipherSuites[3],
                                                 SupportedNonTls13CipherSuites[2],
                                                 SupportedNonTls13CipherSuites[1])
            };

            for (int i = 0; i < 2; i++)
            {
                bool isAClient = i == 0;
                NegotiatedParams ret = isAClient ?
                    ConnectAndGetNegotiatedParams(b, a) :
                    ConnectAndGetNegotiatedParams(a, b);

                ret.Succeeded();

                Assert.True(ret.CipherSuite == SupportedNonTls13CipherSuites[1] ||
                            ret.CipherSuite == SupportedNonTls13CipherSuites[2]);
            }
        }

        [ConditionalFact(nameof(CipherSuitesPolicyAndTls13Supported))]
        public void CipherSuitesPolicy_OnlyTls13CipherSuiteAllowedButChosenProtocolsDoesNotAllowIt_Fails()
        {
            var a = new ConnectionParams()
            {
                CipherSuitesPolicy = BuildPolicy(TlsCipherSuite.TLS_AES_128_GCM_SHA256),
                SslProtocols = NonTls13Protocols,
            };

            var b = new ConnectionParams();

            for (int i = 0; i < 2; i++)
            {
                NegotiatedParams ret = i == 0 ?
                    ConnectAndGetNegotiatedParams(a, b) :
                    ConnectAndGetNegotiatedParams(b, a);
                ret.Failed();
            }
        }

        [ConditionalFact(nameof(CipherSuitesPolicyAndTls13Supported))]
        public void CipherSuitesPolicy_OnlyTls13CipherSuiteAllowedOtherSideDoesNotAllowTls13_Fails()
        {
            var a = new ConnectionParams()
            {
                CipherSuitesPolicy = BuildPolicy(TlsCipherSuite.TLS_AES_128_GCM_SHA256)
            };

            var b = new ConnectionParams()
            {
                SslProtocols = NonTls13Protocols
            };

            for (int i = 0; i < 2; i++)
            {
                NegotiatedParams ret = i == 0 ?
                    ConnectAndGetNegotiatedParams(a, b) :
                    ConnectAndGetNegotiatedParams(b, a);
                ret.Failed();
            }
        }

        [ConditionalFact(nameof(CipherSuitesPolicySupported))]
        public void CipherSuitesPolicy_OnlyNonTls13CipherSuitesAllowedButChosenProtocolDoesNotAllowIt_Fails()
        {
            CheckPrereqsForNonTls13Tests(1);
            var a = new ConnectionParams()
            {
                CipherSuitesPolicy = BuildPolicy(SupportedNonTls13CipherSuites[0]),
                SslProtocols = SslProtocols.Tls13,
            };

            var b = new ConnectionParams();

            for (int i = 0; i < 2; i++)
            {
                NegotiatedParams ret = i == 0 ?
                    ConnectAndGetNegotiatedParams(a, b) :
                    ConnectAndGetNegotiatedParams(b, a);
                ret.Failed();
            }
        }

        [ConditionalFact(nameof(CipherSuitesPolicySupported))]
        public void CipherSuitesPolicy_OnlyNonTls13CipherSuiteAllowedButOtherSideDoesNotAllowIt_Fails()
        {
            CheckPrereqsForNonTls13Tests(1);
            var a = new ConnectionParams()
            {
                CipherSuitesPolicy = BuildPolicy(SupportedNonTls13CipherSuites[0])
            };

            var b = new ConnectionParams()
            {
                SslProtocols = SslProtocols.Tls13
            };

            for (int i = 0; i < 2; i++)
            {
                NegotiatedParams ret = i == 0 ?
                    ConnectAndGetNegotiatedParams(a, b) :
                    ConnectAndGetNegotiatedParams(b, a);
                ret.Failed();
            }
        }

        [Fact]
        public void CipherSuitesPolicy_CtorWithNull_Fails()
        {
            Assert.Throws<ArgumentNullException>(() => new CipherSuitesPolicy(null));
        }

        [ConditionalFact(nameof(CipherSuitesPolicySupported))]
        public void CipherSuitesPolicy_AllowedCipherSuitesIncludesSubsetOfInput_Success()
        {
            TlsCipherSuite[] allCipherSuites = (TlsCipherSuite[])Enum.GetValues(typeof(TlsCipherSuite));
            var r = new Random(123);
            int[] numOfCipherSuites = new int[] { 0, 1, 2, 5, 10, 15, 30 };

            foreach (int n in numOfCipherSuites)
            {
                HashSet<TlsCipherSuite> cipherSuites = PickRandomValues(allCipherSuites, n, r);
                var csp = new CipherSuitesPolicy(cipherSuites);
                Assert.NotNull(csp.AllowedCipherSuites);
                Assert.InRange(csp.AllowedCipherSuites.Count(), 0, n);

                foreach (var cs in csp.AllowedCipherSuites)
                {
                    Assert.True(cipherSuites.Contains(cs));
                }
            }
        }

        private HashSet<TlsCipherSuite> PickRandomValues(TlsCipherSuite[] all, int n, Random r)
        {
            var ret = new HashSet<TlsCipherSuite>();

            while (ret.Count != n)
            {
                ret.Add(all[r.Next() % n]);
            }

            return ret;
        }

        private static void AllowOneOnOneSide(IEnumerable<TlsCipherSuite> cipherSuites,
                                       Predicate<TlsCipherSuite> mustSucceed,
                                       Action<TlsCipherSuite> cipherSuitePicked = null)
        {
            foreach (TlsCipherSuite cs in cipherSuites)
            {
                CipherSuitesPolicy csp = BuildPolicy(cs);

                var paramsA = new ConnectionParams()
                {
                    CipherSuitesPolicy = csp,
                };

                var paramsB = new ConnectionParams();
                int score = 0; // 1 for success 0 for fail. Sum should be even

                for (int i = 0; i < 2; i++)
                {
                    NegotiatedParams ret = i == 0 ?
                        ConnectAndGetNegotiatedParams(paramsA, paramsB) :
                        ConnectAndGetNegotiatedParams(paramsB, paramsA);

                    score += ret.HasSucceeded ? 1 : 0;
                    if (mustSucceed(cs) || ret.HasSucceeded)
                    {
                        // we do not always guarantee success but if it succeeds it
                        // must use the picked cipher suite
                        ret.Succeeded();
                        ret.CheckCipherSuite(cs);

                        if (cipherSuitePicked != null && i == 0)
                        {
                            cipherSuitePicked(cs);
                        }
                    }
                }

                // we should either get 2 successes or 2 failures
                Assert.True(score % 2 == 0);
            }
        }

        private static void CheckPrereqsForNonTls13Tests(int minCipherSuites)
        {
            if (SupportedNonTls13CipherSuites.Count < minCipherSuites)
            {
                // We do not want to accidentally make the tests pass due to the bug in the code
                // This situation is rather unexpected but can happen on i.e. Alpine
                // Make sure at least some tests run.

                if (Tls13Supported)
                {
                    throw new SkipTestException($"Test requires that at least {minCipherSuites} non TLS 1.3 cipher suites are supported.");
                }
                else
                {
                    throw new Exception($"Less than {minCipherSuites} cipher suites are supported: {string.Join(", ", SupportedNonTls13CipherSuites)}");
                }
            }
        }

        private static bool ProtocolsSupported(SslProtocols protocols)
        {
            var defaultParams = new ConnectionParams();
            defaultParams.SslProtocols = protocols;
            NegotiatedParams ret = ConnectAndGetNegotiatedParams(defaultParams, defaultParams);
            return ret.HasSucceeded && protocols.HasFlag(ret.Protocol);
        }

        private static IEnumerable<TlsCipherSuite> GetTls13CipherSuites()
        {
            // https://tools.ietf.org/html/rfc8446#appendix-B.4
            yield return TlsCipherSuite.TLS_AES_128_GCM_SHA256;
            yield return TlsCipherSuite.TLS_AES_256_GCM_SHA384;
            yield return TlsCipherSuite.TLS_CHACHA20_POLY1305_SHA256;
            yield return TlsCipherSuite.TLS_AES_128_CCM_SHA256;
            yield return TlsCipherSuite.TLS_AES_128_CCM_8_SHA256;
        }

        private static IEnumerable<TlsCipherSuite> GetTls12CipherSuites()
        {
            // openssl ciphers -tls1_2 -s --stdname -v
            yield return TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384;
            yield return TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384;
            yield return TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_GCM_SHA384;
            yield return TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CHACHA20_POLY1305_SHA256;
            yield return TlsCipherSuite.TLS_ECDHE_RSA_WITH_CHACHA20_POLY1305_SHA256;
            yield return TlsCipherSuite.TLS_DHE_RSA_WITH_CHACHA20_POLY1305_SHA256;
            yield return TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256;
            yield return TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256;
            yield return TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_GCM_SHA256;
            yield return TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA384;
            yield return TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA384;
            yield return TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_CBC_SHA256;
            yield return TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA256;
            yield return TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA256;
            yield return TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_CBC_SHA256;
            yield return TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA;
            yield return TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA;
            yield return TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_CBC_SHA;
            yield return TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA;
            yield return TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA;
            yield return TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_CBC_SHA;
            yield return TlsCipherSuite.TLS_RSA_WITH_AES_256_GCM_SHA384;
            yield return TlsCipherSuite.TLS_RSA_WITH_AES_128_GCM_SHA256;
            yield return TlsCipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA256;
            yield return TlsCipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA256;
            yield return TlsCipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA;
            yield return TlsCipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA;
        }

        private static IEnumerable<TlsCipherSuite> GetTls10And11CipherSuites()
        {
            // openssl ciphers -tls1_1 -s --stdname -v
            yield return TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA;
            yield return TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA;
            yield return TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_CBC_SHA;
            yield return TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA;
            yield return TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA;
            yield return TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_CBC_SHA;
            yield return TlsCipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA;
            yield return TlsCipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA;

            // rfc5289 values (OSX)
            yield return TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA256;
            yield return TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA384;
            yield return TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_128_CBC_SHA256;
            yield return TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_256_CBC_SHA384;
            yield return TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA256;
            yield return TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA384;
            yield return TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_128_CBC_SHA256;
            yield return TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_256_CBC_SHA384;
            yield return TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256;
            yield return TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384;
            yield return TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_128_GCM_SHA256;
            yield return TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_256_GCM_SHA384;
            yield return TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256;
            yield return TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384;
            yield return TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_128_GCM_SHA256;
            yield return TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_256_GCM_SHA384;
        }

        private static IEnumerable<TlsCipherSuite> GetNonTls13CipherSuites()
        {
            var tls13cs = new HashSet<TlsCipherSuite>(GetTls13CipherSuites());
            foreach (TlsCipherSuite cs in typeof(TlsCipherSuite).GetEnumValues())
            {
                if (!tls13cs.Contains(cs))
                {
                    yield return cs;
                }
            }
        }

        private static IReadOnlyList<TlsCipherSuite> GetSupportedNonTls13CipherSuites()
        {
            // This function is used to initialize static property.
            // We do not want skipped tests to fail because of that.
            if (!CipherSuitesPolicySupported)
                return null;

            var ret = new List<TlsCipherSuite>();
            AllowOneOnOneSide(GetNonTls13CipherSuites(), (cs) => false, (cs) => ret.Add(cs));

            return ret;
        }

        private static bool RequiredByTls13Spec(TlsCipherSuite cs)
        {
            // per spec only one MUST be implemented
            return cs == TlsCipherSuite.TLS_AES_128_GCM_SHA256;
        }

        private static CipherSuitesPolicy BuildPolicy(params TlsCipherSuite[] cipherSuites)
        {
            return new CipherSuitesPolicy(cipherSuites);
        }

        private static async Task<Exception> WaitForSecureConnection(VirtualNetwork connection, Func<Task> server, Func<Task> client)
        {
            Task serverTask = null;
            Task clientTask = null;

            // check if failed synchronously
            try
            {
                serverTask = server();
                clientTask = client();
            }
            catch (Exception e)
            {
                connection.BreakConnection();

                if (!(e is AuthenticationException || e is Win32Exception))
                {
                    throw;
                }

                if (serverTask != null)
                {
                    // i.e. for server we used DEFAULT options but for client we chose not supported cipher suite
                    //      this will cause client to fail synchronously while server awaits connection
                    try
                    {
                        // since we broke connection the server should finish
                        await serverTask;
                    }
                    catch (AuthenticationException) { }
                    catch (Win32Exception) { }
                    catch (VirtualNetwork.VirtualNetworkConnectionBroken) { }
                }

                return e;
            }

            // Since we got here it means client and server have at least 1 choice
            // of cipher suite
            // Now we expect both sides to fail or both to succeed

            Exception failure = null;

            try
            {
                await serverTask.ConfigureAwait(false);
            }
            catch (Exception e) when (e is AuthenticationException || e is Win32Exception)
            {
                failure = e;

                // avoid client waiting for server's response
                connection.BreakConnection();
            }

            try
            {
                await clientTask.ConfigureAwait(false);

                // Fail if server has failed but client has succeeded
                Assert.Null(failure);
            }
            catch (Exception e) when (e is VirtualNetwork.VirtualNetworkConnectionBroken || e is AuthenticationException || e is Win32Exception)
            {
                // Fail if server has succeeded but client has failed
                Assert.NotNull(failure);

                if (e.GetType() != typeof(VirtualNetwork.VirtualNetworkConnectionBroken))
                {
                    failure = new AggregateException(new Exception[] { failure, e });
                }
            }

            return failure;
        }

        private static NegotiatedParams ConnectAndGetNegotiatedParams(ConnectionParams serverParams, ConnectionParams clientParams)
        {
            VirtualNetwork vn = new VirtualNetwork();
            using (VirtualNetworkStream serverStream = new VirtualNetworkStream(vn, isServer: true),
                                        clientStream = new VirtualNetworkStream(vn, isServer: false))
            using (SslStream server = new SslStream(serverStream, leaveInnerStreamOpen: false),
                             client = new SslStream(clientStream, leaveInnerStreamOpen: false))
            {
                var serverOptions = new SslServerAuthenticationOptions();
                serverOptions.ServerCertificate = Configuration.Certificates.GetSelfSignedServerCertificate();
                serverOptions.EncryptionPolicy = serverParams.EncryptionPolicy;
                serverOptions.EnabledSslProtocols = serverParams.SslProtocols;
                serverOptions.CipherSuitesPolicy = serverParams.CipherSuitesPolicy;

                var clientOptions = new SslClientAuthenticationOptions();
                clientOptions.EncryptionPolicy = clientParams.EncryptionPolicy;
                clientOptions.EnabledSslProtocols = clientParams.SslProtocols;
                clientOptions.CipherSuitesPolicy = clientParams.CipherSuitesPolicy;
                clientOptions.TargetHost = "test";
                clientOptions.RemoteCertificateValidationCallback =
                    new RemoteCertificateValidationCallback((object sender,
                                                             X509Certificate certificate,
                                                             X509Chain chain,
                                                             SslPolicyErrors sslPolicyErrors) => {
                                                                 return true;
                                                             });

                Func<Task> serverTask = () => server.AuthenticateAsServerAsync(serverOptions, CancellationToken.None);
                Func<Task> clientTask = () => client.AuthenticateAsClientAsync(clientOptions, CancellationToken.None);

                Exception failure = WaitForSecureConnection(vn, serverTask, clientTask).Result;

                if (failure == null)
                {
                    // send some bytes, make sure they can talk
                    byte[] data = new byte[] { 1, 2, 3 };
                    server.WriteAsync(data, 0, data.Length);

                    for (int i = 0; i < data.Length; i++)
                    {
                        Assert.Equal(data[i], client.ReadByte());
                    }

                    return new NegotiatedParams(server, client);
                }
                else
                {
                    return new NegotiatedParams(failure);
                }
            }
        }

        private class ConnectionParams
        {
            public CipherSuitesPolicy CipherSuitesPolicy = null;
            public EncryptionPolicy EncryptionPolicy = EncryptionPolicy.RequireEncryption;
            public SslProtocols SslProtocols = SslProtocols.None;
        }

        private class NegotiatedParams
        {
            private Exception _failure;

            public bool HasSucceeded => _failure == null;
            public SslProtocols Protocol { get; private set; }
            public TlsCipherSuite CipherSuite { get; private set; }

            public NegotiatedParams(Exception failure)
            {
                _failure = failure;
            }

            public NegotiatedParams(SslStream serverStream, SslStream clientStream)
            {
                _failure = null;
                CipherSuite = serverStream.NegotiatedCipherSuite;
                Protocol = serverStream.SslProtocol;

                Assert.Equal(CipherSuite, clientStream.NegotiatedCipherSuite);
                Assert.Equal(Protocol, clientStream.SslProtocol);
            }

            public void Failed()
            {
                Assert.NotNull(_failure);
            }

            public void Succeeded()
            {
                if (!HasSucceeded)
                {
                    // for better error message we throw
                    throw _failure;
                }
            }

            public void CheckCipherSuite(TlsCipherSuite expectedCipherSuite)
            {
                Assert.Equal(expectedCipherSuite, CipherSuite);
            }

            public override string ToString()
            {
                // Only for debugging

                if (HasSucceeded)
                {
                    return $"[{Protocol}, {CipherSuite}]";
                }
                else
                {
                    return $"[Failed: {_failure.ToString()}]";
                }
            }
        }
    }
}
