// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Security;
using System.Runtime.CompilerServices;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Net.Tests
{
    public class ServicePointManagerTest
    {
        [Fact]
        public static void RequireEncryption_ExpectedDefault()
        {
            Assert.Equal(EncryptionPolicy.RequireEncryption, ServicePointManager.EncryptionPolicy);
        }

        [Fact]
        public static void CheckCertificateRevocationList_Roundtrips()
        {
            Assert.False(ServicePointManager.CheckCertificateRevocationList);
            try
            {
                ServicePointManager.CheckCertificateRevocationList = true;
                Assert.True(ServicePointManager.CheckCertificateRevocationList);
            }
            finally
            {
                ServicePointManager.CheckCertificateRevocationList = false;
                Assert.False(ServicePointManager.CheckCertificateRevocationList);
            }
        }

        [Fact]
        public static void DefaultConnectionLimit_Roundtrips()
        {
            Assert.Equal(2, ServicePointManager.DefaultConnectionLimit);
            try
            {
                ServicePointManager.DefaultConnectionLimit = 20;
                Assert.Equal(20, ServicePointManager.DefaultConnectionLimit);
            }
            finally
            {
                ServicePointManager.DefaultConnectionLimit = 2;
                Assert.Equal(2, ServicePointManager.DefaultConnectionLimit);
            }
        }

        [Fact]
        public static void DnsRefreshTimeout_Roundtrips()
        {
            Assert.Equal(120_000, ServicePointManager.DnsRefreshTimeout);
            try
            {
                ServicePointManager.DnsRefreshTimeout = 42;
                Assert.Equal(42, ServicePointManager.DnsRefreshTimeout);
            }
            finally
            {
                ServicePointManager.DnsRefreshTimeout = 120_000;
                Assert.Equal(120_000, ServicePointManager.DnsRefreshTimeout);
            }
        }

        [Fact]
        public static void EnableDnsRoundRobin_Roundtrips()
        {
            Assert.False(ServicePointManager.EnableDnsRoundRobin);
            try
            {
                ServicePointManager.EnableDnsRoundRobin = true;
                Assert.True(ServicePointManager.EnableDnsRoundRobin);
            }
            finally
            {
                ServicePointManager.EnableDnsRoundRobin = false;
                Assert.False(ServicePointManager.EnableDnsRoundRobin);
            }
        }

        [Fact]
        public static void Expect100Continue_Roundtrips()
        {
            Assert.True(ServicePointManager.Expect100Continue);
            try
            {
                ServicePointManager.Expect100Continue = false;
                Assert.False(ServicePointManager.Expect100Continue);
            }
            finally
            {
                ServicePointManager.Expect100Continue = true;
                Assert.True(ServicePointManager.Expect100Continue);
            }
        }

        [Fact]
        public static void MaxServicePointIdleTime_Roundtrips()
        {
            Assert.Equal(100_000, ServicePointManager.MaxServicePointIdleTime);
            try
            {
                ServicePointManager.MaxServicePointIdleTime = 42;
                Assert.Equal(42, ServicePointManager.MaxServicePointIdleTime);
            }
            finally
            {
                ServicePointManager.MaxServicePointIdleTime = 100_000;
                Assert.Equal(100_000, ServicePointManager.MaxServicePointIdleTime);
            }
        }

        [Fact]
        public static void MaxServicePoints_Roundtrips()
        {
            Assert.Equal(0, ServicePointManager.MaxServicePoints);
            try
            {
                ServicePointManager.MaxServicePoints = 42;
                Assert.Equal(42, ServicePointManager.MaxServicePoints);
            }
            finally
            {
                ServicePointManager.MaxServicePoints = 0;
                Assert.Equal(0, ServicePointManager.MaxServicePoints);
            }
        }

        [Fact]
        public static void ReusePort_Roundtrips()
        {
            Assert.False(ServicePointManager.ReusePort);
            try
            {
                ServicePointManager.ReusePort = true;
                Assert.True(ServicePointManager.ReusePort);
            }
            finally
            {
                ServicePointManager.ReusePort = false;
                Assert.False(ServicePointManager.ReusePort);
            }
        }

        [Fact]
        public static void SecurityProtocol_Roundtrips()
        {
            var orig = (SecurityProtocolType)0; // SystemDefault.
            Assert.Equal(orig, ServicePointManager.SecurityProtocol);
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11;
                Assert.Equal(SecurityProtocolType.Tls11, ServicePointManager.SecurityProtocol);
            }
            finally
            {
                ServicePointManager.SecurityProtocol = orig;
                Assert.Equal(orig, ServicePointManager.SecurityProtocol);
            }
        }

        [Fact]
        public static void ServerCertificateValidationCallback_Roundtrips()
        {
            Assert.Null(ServicePointManager.ServerCertificateValidationCallback);
            try
            {
                RemoteCertificateValidationCallback callback = delegate { return true; };
                ServicePointManager.ServerCertificateValidationCallback = callback;
                Assert.Same(callback, ServicePointManager.ServerCertificateValidationCallback);
            }
            finally
            {
                ServicePointManager.ServerCertificateValidationCallback = null;
                Assert.Null(ServicePointManager.ServerCertificateValidationCallback);
            }
        }

        [Fact]
        public static void UseNagleAlgorithm_Roundtrips()
        {
            Assert.True(ServicePointManager.UseNagleAlgorithm);
            try
            {
                ServicePointManager.UseNagleAlgorithm = false;
                Assert.False(ServicePointManager.UseNagleAlgorithm);
            }
            finally
            {
                ServicePointManager.UseNagleAlgorithm = true;
                Assert.True(ServicePointManager.UseNagleAlgorithm);
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void InvalidArguments_Throw()
        {
            RemoteExecutor.Invoke(() =>
            {
                const int ssl2Client = 0x00000008;
                const int ssl2Server = 0x00000004;
                const SecurityProtocolType ssl2 = (SecurityProtocolType)(ssl2Client | ssl2Server);
                Assert.Throws<NotSupportedException>(() => ServicePointManager.SecurityProtocol = ssl2);

                AssertExtensions.Throws<ArgumentNullException>("uriString", () => ServicePointManager.FindServicePoint((string)null, null));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => ServicePointManager.MaxServicePoints = -1);
                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => ServicePointManager.DefaultConnectionLimit = 0);
                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => ServicePointManager.MaxServicePointIdleTime = -2);
                AssertExtensions.Throws<ArgumentOutOfRangeException>("keepAliveTime", () => ServicePointManager.SetTcpKeepAlive(true, -1, 1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("keepAliveInterval", () => ServicePointManager.SetTcpKeepAlive(true, 1, -1));
                AssertExtensions.Throws<ArgumentNullException>("address", () => ServicePointManager.FindServicePoint(null));
                AssertExtensions.Throws<ArgumentNullException>("uriString", () => ServicePointManager.FindServicePoint((string)null, null));
                AssertExtensions.Throws<ArgumentNullException>("address", () => ServicePointManager.FindServicePoint((Uri)null, null));
                Assert.Throws<NotSupportedException>(() => ServicePointManager.FindServicePoint("http://anything", new FixedWebProxy("https://anything")));

                ServicePoint sp = ServicePointManager.FindServicePoint($"http://{Guid.NewGuid():N}", null);
                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => sp.ConnectionLeaseTimeout = -2);
                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => sp.ConnectionLimit = 0);
                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => sp.MaxIdleTime = -2);
                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => sp.ReceiveBufferSize = -2);
                AssertExtensions.Throws<ArgumentOutOfRangeException>("keepAliveTime", () => sp.SetTcpKeepAlive(true, -1, 1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("keepAliveInterval", () => sp.SetTcpKeepAlive(true, 1, -1));
            }).Dispose();
        }

        [Fact]
        public static void SecurityProtocol_Ssl3_NotSupported()
        {
            const int ssl2Client = 0x00000008;
            const int ssl2Server = 0x00000004;
            const SecurityProtocolType ssl2 = (SecurityProtocolType)(ssl2Client | ssl2Server);

            SecurityProtocolType orig = ServicePointManager.SecurityProtocol;
            try
            {
#pragma warning disable 0618 // Ssl2, Ssl3 are deprecated.
                Assert.Throws<NotSupportedException>(() => ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3);
                Assert.Throws<NotSupportedException>(() => ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | ssl2);
#pragma warning restore
            }
            finally
            {
                ServicePointManager.SecurityProtocol = orig;
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void FindServicePoint_ReturnsCachedServicePoint()
        {
            RemoteExecutor.Invoke(() =>
            {
                const string Localhost = "http://localhost";
                string address1 = "http://" + Guid.NewGuid().ToString("N");
                string address2 = "http://" + Guid.NewGuid().ToString("N");

                Assert.NotNull(ServicePointManager.FindServicePoint(new Uri(address1)));

                Assert.Same(
                    ServicePointManager.FindServicePoint(address1, null),
                    ServicePointManager.FindServicePoint(address1, null));
                Assert.Same(
                    ServicePointManager.FindServicePoint(address1, new FixedWebProxy(address1)),
                    ServicePointManager.FindServicePoint(address1, new FixedWebProxy(address1)));
                Assert.Same(
                    ServicePointManager.FindServicePoint(address1, new FixedWebProxy(address1)),
                    ServicePointManager.FindServicePoint(address2, new FixedWebProxy(address1)));
                Assert.Same(
                    ServicePointManager.FindServicePoint(Localhost, new FixedWebProxy(address1)),
                    ServicePointManager.FindServicePoint(Localhost, new FixedWebProxy(address2)));

                Assert.NotSame(
                    ServicePointManager.FindServicePoint(address1, null),
                    ServicePointManager.FindServicePoint(address2, null));
                Assert.NotSame(
                    ServicePointManager.FindServicePoint(address1, null),
                    ServicePointManager.FindServicePoint(address1, new FixedWebProxy(address1)));
                Assert.NotSame(
                    ServicePointManager.FindServicePoint(address1, new FixedWebProxy(address1)),
                    ServicePointManager.FindServicePoint(address1, new FixedWebProxy(address2)));
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/36217", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoInterpreter))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/64674", typeof(PlatformDetection), nameof(PlatformDetection.IsArmv6Process))]
        public static void FindServicePoint_Collectible()
        {
            RemoteExecutor.Invoke(() =>
            {
                string address = "http://" + Guid.NewGuid().ToString("N");

                bool initial = GetExpect100Continue(address);
                SetExpect100Continue(address, !initial);

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                Assert.Equal(initial, GetExpect100Continue(address));
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void FindServicePoint_ReturnedServicePointMatchesExpectedValues()
        {
            RemoteExecutor.Invoke(() =>
            {
                string address = "http://" + Guid.NewGuid().ToString("N");

                DateTime start = DateTime.Now;
                ServicePoint sp = ServicePointManager.FindServicePoint(address, null);

                Assert.InRange(sp.IdleSince, start, DateTime.MaxValue);
                Assert.Equal(new Uri(address), sp.Address);
                Assert.Null(sp.BindIPEndPointDelegate);
                Assert.Null(sp.Certificate);
                Assert.Null(sp.ClientCertificate);
                Assert.Equal(-1, sp.ConnectionLeaseTimeout);
                Assert.Equal("http", sp.ConnectionName);
                Assert.Equal(0, sp.CurrentConnections);
                Assert.True(sp.Expect100Continue);
                Assert.Equal(100000, sp.MaxIdleTime);
                Assert.Equal(new Version(1, 1), sp.ProtocolVersion);
                Assert.Equal(-1, sp.ReceiveBufferSize);
                Assert.True(sp.SupportsPipelining, "SupportsPipelining");
                Assert.True(sp.UseNagleAlgorithm, "UseNagleAlgorithm");
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void FindServicePoint_PropertiesRoundtrip()
        {
            RemoteExecutor.Invoke(() =>
            {
                string address = "http://" + Guid.NewGuid().ToString("N");

                BindIPEndPoint expectedBindIPEndPointDelegate = delegate { return null; };
                int expectedConnectionLeaseTimeout = 42;
                int expectedConnectionLimit = 84;
                bool expected100Continue = false;
                int expectedMaxIdleTime = 200000;
                int expectedReceiveBufferSize = 123;
                bool expectedUseNagleAlgorithm = false;

                ServicePoint sp1 = ServicePointManager.FindServicePoint(address, null);
                sp1.BindIPEndPointDelegate = expectedBindIPEndPointDelegate;
                sp1.ConnectionLeaseTimeout = expectedConnectionLeaseTimeout;
                sp1.ConnectionLimit = expectedConnectionLimit;
                sp1.Expect100Continue = expected100Continue;
                sp1.MaxIdleTime = expectedMaxIdleTime;
                sp1.ReceiveBufferSize = expectedReceiveBufferSize;
                sp1.UseNagleAlgorithm = expectedUseNagleAlgorithm;

                ServicePoint sp2 = ServicePointManager.FindServicePoint(address, null);
                Assert.Same(expectedBindIPEndPointDelegate, sp2.BindIPEndPointDelegate);
                Assert.Equal(expectedConnectionLeaseTimeout, sp2.ConnectionLeaseTimeout);
                Assert.Equal(expectedConnectionLimit, sp2.ConnectionLimit);
                Assert.Equal(expected100Continue, sp2.Expect100Continue);
                Assert.Equal(expectedMaxIdleTime, sp2.MaxIdleTime);
                Assert.Equal(expectedReceiveBufferSize, sp2.ReceiveBufferSize);
                Assert.Equal(expectedUseNagleAlgorithm, sp2.UseNagleAlgorithm);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void FindServicePoint_NewServicePointsInheritCurrentValues()
        {
            RemoteExecutor.Invoke(() =>
            {
                string address1 = "http://" + Guid.NewGuid().ToString("N");
                string address2 = "http://" + Guid.NewGuid().ToString("N");

                bool orig100Continue = ServicePointManager.Expect100Continue;
                bool origNagle = ServicePointManager.UseNagleAlgorithm;

                ServicePointManager.Expect100Continue = false;
                ServicePointManager.UseNagleAlgorithm = false;
                ServicePoint sp1 = ServicePointManager.FindServicePoint(address1, null);
                Assert.False(sp1.Expect100Continue);
                Assert.False(sp1.UseNagleAlgorithm);

                ServicePointManager.Expect100Continue = true;
                ServicePointManager.UseNagleAlgorithm = true;
                ServicePoint sp2 = ServicePointManager.FindServicePoint(address2, null);
                Assert.True(sp2.Expect100Continue);
                Assert.True(sp2.UseNagleAlgorithm);
                Assert.False(sp1.Expect100Continue);
                Assert.False(sp1.UseNagleAlgorithm);

                ServicePointManager.Expect100Continue = orig100Continue;
                ServicePointManager.UseNagleAlgorithm = origNagle;
            }).Dispose();
        }

        // Separated out to avoid the JIT in debug builds interfering with object lifetimes
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool GetExpect100Continue(string address) =>
            ServicePointManager.FindServicePoint(address, null).Expect100Continue;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void SetExpect100Continue(string address, bool value) =>
            ServicePointManager.FindServicePoint(address, null).Expect100Continue = value;

        private sealed class FixedWebProxy : IWebProxy
        {
            private readonly Uri _proxyAddress;
            public FixedWebProxy(string proxyAddress) { _proxyAddress = new Uri(proxyAddress); }
            public Uri GetProxy(Uri destination) => _proxyAddress;
            public bool IsBypassed(Uri host) => false;
            public ICredentials Credentials { get; set; }
        }
    }
}
