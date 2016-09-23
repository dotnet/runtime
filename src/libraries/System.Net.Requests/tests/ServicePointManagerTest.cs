// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Security;
using Xunit;

#pragma warning disable CS0618 // obsolete warnings

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
        public static void CertificatePolicy_Roundtrips()
        {
            ICertificatePolicy cp = ServicePointManager.CertificatePolicy;
            Assert.NotNull(cp);

            Assert.True(cp.CheckValidationResult(null, null, null, 0));
            Assert.False(cp.CheckValidationResult(null, null, null, 1));

            ServicePointManager.CertificatePolicy = null;
            Assert.Null(ServicePointManager.CertificatePolicy);

            ServicePointManager.CertificatePolicy = cp;
            Assert.Same(cp, ServicePointManager.CertificatePolicy);
        }

        [Fact]
        public static void CheckCertificateRevocationList_Roundtrips()
        {
            Assert.False(ServicePointManager.CheckCertificateRevocationList);

            ServicePointManager.CheckCertificateRevocationList = true;
            Assert.True(ServicePointManager.CheckCertificateRevocationList);

            ServicePointManager.CheckCertificateRevocationList = false;
            Assert.False(ServicePointManager.CheckCertificateRevocationList);
        }

        [Fact]
        public static void DefaultConnectionLimit_Roundtrips()
        {
            Assert.Equal(10, ServicePointManager.DefaultConnectionLimit);

            ServicePointManager.DefaultConnectionLimit = 20;
            Assert.Equal(20, ServicePointManager.DefaultConnectionLimit);

            ServicePointManager.DefaultConnectionLimit = 10;
            Assert.Equal(10, ServicePointManager.DefaultConnectionLimit);
        }

        [Fact]
        public static void DnsRefreshTimeout_Roundtrips()
        {
            Assert.Equal(120000, ServicePointManager.DnsRefreshTimeout);

            ServicePointManager.DnsRefreshTimeout = 42;
            Assert.Equal(42, ServicePointManager.DnsRefreshTimeout);

            ServicePointManager.DnsRefreshTimeout = 120000;
            Assert.Equal(120000, ServicePointManager.DnsRefreshTimeout);
        }

        [Fact]
        public static void EnableDnsRoundRobin_Roundtrips()
        {
            Assert.False(ServicePointManager.EnableDnsRoundRobin);

            ServicePointManager.EnableDnsRoundRobin = true;
            Assert.True(ServicePointManager.EnableDnsRoundRobin);

            ServicePointManager.EnableDnsRoundRobin = false;
            Assert.False(ServicePointManager.EnableDnsRoundRobin);
        }

        [Fact]
        public static void Expect100Continue_Roundtrips()
        {
            Assert.True(ServicePointManager.Expect100Continue);

            ServicePointManager.Expect100Continue = false;
            Assert.False(ServicePointManager.Expect100Continue);

            ServicePointManager.Expect100Continue = true;
            Assert.True(ServicePointManager.Expect100Continue);
        }

        [Fact]
        public static void MaxServicePointIdleTime_Roundtrips()
        {
            Assert.Equal(100000, ServicePointManager.MaxServicePointIdleTime);

            ServicePointManager.MaxServicePointIdleTime = 42;
            Assert.Equal(42, ServicePointManager.MaxServicePointIdleTime);

            ServicePointManager.MaxServicePointIdleTime = 100000;
            Assert.Equal(100000, ServicePointManager.MaxServicePointIdleTime);
        }

        [Fact]
        public static void MaxServicePoints_Roundtrips()
        {
            Assert.Equal(0, ServicePointManager.MaxServicePoints);

            ServicePointManager.MaxServicePoints = 42;
            Assert.Equal(42, ServicePointManager.MaxServicePoints);

            ServicePointManager.MaxServicePoints = 0;
            Assert.Equal(0, ServicePointManager.MaxServicePoints);
        }

        [Fact]
        public static void ReusePort_Roundtrips()
        {
            Assert.False(ServicePointManager.ReusePort);

            ServicePointManager.ReusePort = true;
            Assert.True(ServicePointManager.ReusePort);

            ServicePointManager.ReusePort = false;
            Assert.False(ServicePointManager.ReusePort);
        }

        [Fact]
        public static void SecurityProtocol_Roundtrips()
        {
            var orig = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            Assert.Equal(orig, ServicePointManager.SecurityProtocol);

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11;
            Assert.Equal(SecurityProtocolType.Tls11, ServicePointManager.SecurityProtocol);

            ServicePointManager.SecurityProtocol = orig;
            Assert.Equal(orig, ServicePointManager.SecurityProtocol);
        }

        [Fact]
        public static void ServerCertificateValidationCallback_Roundtrips()
        {
            Assert.Null(ServicePointManager.ServerCertificateValidationCallback);

            RemoteCertificateValidationCallback callback = delegate { return true; };
            ServicePointManager.ServerCertificateValidationCallback = callback;
            Assert.Same(callback, ServicePointManager.ServerCertificateValidationCallback);

            ServicePointManager.ServerCertificateValidationCallback = null;
            Assert.Null(ServicePointManager.ServerCertificateValidationCallback);
        }

        [Fact]
        public static void UseNagleAlgorithm_Roundtrips()
        {
            Assert.True(ServicePointManager.UseNagleAlgorithm);

            ServicePointManager.UseNagleAlgorithm = false;
            Assert.False(ServicePointManager.UseNagleAlgorithm);

            ServicePointManager.UseNagleAlgorithm = true;
            Assert.True(ServicePointManager.UseNagleAlgorithm);
        }

        [Fact]
        public static void UnsupportedOperations_Throw()
        {
            Assert.Throws<PlatformNotSupportedException>(() => ServicePointManager.FindServicePoint(null));
            Assert.Throws<PlatformNotSupportedException>(() => ServicePointManager.FindServicePoint("http://localhost", null));
            Assert.Throws<PlatformNotSupportedException>(() => ServicePointManager.FindServicePoint(new Uri("http://localhost"), null));
        }

        [Fact]
        public static void InvalidArguments_Throw()
        {
            Assert.Throws<NotSupportedException>(() => ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3);
            Assert.Throws<ArgumentNullException>("uriString", () => ServicePointManager.FindServicePoint((string)null, null));
            Assert.Throws<ArgumentOutOfRangeException>("value", () => ServicePointManager.MaxServicePoints = -1);
            Assert.Throws<ArgumentOutOfRangeException>("value", () => ServicePointManager.DefaultConnectionLimit = 0);
            Assert.Throws<ArgumentOutOfRangeException>("value", () => ServicePointManager.MaxServicePointIdleTime = -2);
            Assert.Throws<ArgumentOutOfRangeException>("keepAliveTime", () => ServicePointManager.SetTcpKeepAlive(true, -1, 1));
            Assert.Throws<ArgumentOutOfRangeException>("keepAliveInterval", () => ServicePointManager.SetTcpKeepAlive(true, 1, -1));
        }
    }
}
