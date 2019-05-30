// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Net.Tests
{
    public class ServicePointManagerTest
    {
        [Fact]
        public static void RequireEncryption_ExpectedDefault()
        {
            RemoteExecutor.Invoke(() => Assert.Equal(EncryptionPolicy.RequireEncryption, ServicePointManager.EncryptionPolicy)).Dispose();
        }

        [Fact]
        public static void CheckCertificateRevocationList_Roundtrips()
        {
            RemoteExecutor.Invoke(() =>
            {
                Assert.False(ServicePointManager.CheckCertificateRevocationList);

                ServicePointManager.CheckCertificateRevocationList = true;
                Assert.True(ServicePointManager.CheckCertificateRevocationList);

                ServicePointManager.CheckCertificateRevocationList = false;
                Assert.False(ServicePointManager.CheckCertificateRevocationList);
            }).Dispose();
        }

        [Fact]
        public static void DefaultConnectionLimit_Roundtrips()
        {
            RemoteExecutor.Invoke(() =>
            {
                Assert.Equal(2, ServicePointManager.DefaultConnectionLimit);

                ServicePointManager.DefaultConnectionLimit = 20;
                Assert.Equal(20, ServicePointManager.DefaultConnectionLimit);

                ServicePointManager.DefaultConnectionLimit = 2;
                Assert.Equal(2, ServicePointManager.DefaultConnectionLimit);
            }).Dispose();
        }

        [Fact]
        public static void DnsRefreshTimeout_Roundtrips()
        {
            RemoteExecutor.Invoke(() =>
            {
                Assert.Equal(120000, ServicePointManager.DnsRefreshTimeout);

                ServicePointManager.DnsRefreshTimeout = 42;
                Assert.Equal(42, ServicePointManager.DnsRefreshTimeout);

                ServicePointManager.DnsRefreshTimeout = 120000;
                Assert.Equal(120000, ServicePointManager.DnsRefreshTimeout);
            }).Dispose();
        }

        [Fact]
        public static void EnableDnsRoundRobin_Roundtrips()
        {
            RemoteExecutor.Invoke(() =>
            {
                Assert.False(ServicePointManager.EnableDnsRoundRobin);

                ServicePointManager.EnableDnsRoundRobin = true;
                Assert.True(ServicePointManager.EnableDnsRoundRobin);

                ServicePointManager.EnableDnsRoundRobin = false;
                Assert.False(ServicePointManager.EnableDnsRoundRobin);
            }).Dispose();
        }

        [Fact]
        public static void Expect100Continue_Roundtrips()
        {
            RemoteExecutor.Invoke(() =>
            {
                Assert.True(ServicePointManager.Expect100Continue);

                ServicePointManager.Expect100Continue = false;
                Assert.False(ServicePointManager.Expect100Continue);

                ServicePointManager.Expect100Continue = true;
                Assert.True(ServicePointManager.Expect100Continue);
            }).Dispose();
        }

        [Fact]
        public static void MaxServicePointIdleTime_Roundtrips()
        {
            RemoteExecutor.Invoke(() =>
            {
                Assert.Equal(100000, ServicePointManager.MaxServicePointIdleTime);

                ServicePointManager.MaxServicePointIdleTime = 42;
                Assert.Equal(42, ServicePointManager.MaxServicePointIdleTime);

                ServicePointManager.MaxServicePointIdleTime = 100000;
                Assert.Equal(100000, ServicePointManager.MaxServicePointIdleTime);
            }).Dispose();
        }

        [Fact]
        public static void MaxServicePoints_Roundtrips()
        {
            RemoteExecutor.Invoke(() =>
            {
                Assert.Equal(0, ServicePointManager.MaxServicePoints);

                ServicePointManager.MaxServicePoints = 42;
                Assert.Equal(42, ServicePointManager.MaxServicePoints);

                ServicePointManager.MaxServicePoints = 0;
                Assert.Equal(0, ServicePointManager.MaxServicePoints);
            }).Dispose();
        }

        [Fact]
        public static void ReusePort_Roundtrips()
        {
            RemoteExecutor.Invoke(() =>
            {
                Assert.False(ServicePointManager.ReusePort);

                ServicePointManager.ReusePort = true;
                Assert.True(ServicePointManager.ReusePort);

                ServicePointManager.ReusePort = false;
                Assert.False(ServicePointManager.ReusePort);
            }).Dispose();
        }

        [Fact]
        public static void SecurityProtocol_Roundtrips()
        {
            RemoteExecutor.Invoke(() =>
            {
                var orig = (SecurityProtocolType)0; // SystemDefault.
                Assert.Equal(orig, ServicePointManager.SecurityProtocol);

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11;
                Assert.Equal(SecurityProtocolType.Tls11, ServicePointManager.SecurityProtocol);

                ServicePointManager.SecurityProtocol = orig;
                Assert.Equal(orig, ServicePointManager.SecurityProtocol);
            }).Dispose();
        }

        [Fact]
        public static void ServerCertificateValidationCallback_Roundtrips()
        {
            RemoteExecutor.Invoke(() =>
            {
                Assert.Null(ServicePointManager.ServerCertificateValidationCallback);

                RemoteCertificateValidationCallback callback = delegate { return true; };
                ServicePointManager.ServerCertificateValidationCallback = callback;
                Assert.Same(callback, ServicePointManager.ServerCertificateValidationCallback);

                ServicePointManager.ServerCertificateValidationCallback = null;
                Assert.Null(ServicePointManager.ServerCertificateValidationCallback);
            }).Dispose();
        }

        [Fact]
        public static void UseNagleAlgorithm_Roundtrips()
        {
            RemoteExecutor.Invoke(() =>
            {
                Assert.True(ServicePointManager.UseNagleAlgorithm);

                ServicePointManager.UseNagleAlgorithm = false;
                Assert.False(ServicePointManager.UseNagleAlgorithm);

                ServicePointManager.UseNagleAlgorithm = true;
                Assert.True(ServicePointManager.UseNagleAlgorithm);
            }).Dispose();
        }

        [Fact]
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

                ServicePoint sp = ServicePointManager.FindServicePoint("http://" + Guid.NewGuid().ToString("N"), null);
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
            RemoteExecutor.Invoke(() =>
            {
                const int ssl2Client = 0x00000008;
                const int ssl2Server = 0x00000004;
                const SecurityProtocolType ssl2 = (SecurityProtocolType)(ssl2Client | ssl2Server);

#pragma warning disable 0618 // Ssl2, Ssl3 are deprecated.
                Assert.Throws<NotSupportedException>(() => ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3);
                Assert.Throws<NotSupportedException>(() => ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | ssl2);
#pragma warning restore
            }).Dispose();
        }

        [Fact]
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

        [Fact]
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

        [Fact]
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
                Assert.Equal(true, sp.Expect100Continue);
                Assert.Equal(100000, sp.MaxIdleTime);
                Assert.Equal(new Version(1, 1), sp.ProtocolVersion);
                Assert.Equal(-1, sp.ReceiveBufferSize);
                Assert.True(sp.SupportsPipelining, "SupportsPipelining");
                Assert.True(sp.UseNagleAlgorithm, "UseNagleAlgorithm");
            }).Dispose();
        }

        [Fact]
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

        [Fact]
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
        private static bool GetExpect100Continue(string address) =>
            ServicePointManager.FindServicePoint(address, null).Expect100Continue;
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
