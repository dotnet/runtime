// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace System.DirectoryServices.Protocols.Tests
{
    public class LdapSessionOptionsTests
    {
        [Theory]
        [InlineData(ReferralChasingOptions.None)]
        [InlineData(ReferralChasingOptions.External)]
        public void ReferralChasing_Set_GetReturnsExpected(ReferralChasingOptions value)
        {
            using (var connection = new LdapConnection("server"))
            {
                LdapSessionOptions options = connection.SessionOptions;
                Assert.Equal(ReferralChasingOptions.All, options.ReferralChasing);

                options.ReferralChasing = value;
                Assert.Equal(value, options.ReferralChasing);
            }
        }

        [Theory]
        [InlineData((ReferralChasingOptions)(-1))]
        [InlineData((ReferralChasingOptions)3)]
        public void ReferralChasing_SetInvalid_ThrowsInvalidEnumArgumentException(ReferralChasingOptions referralChasing)
        {
            using (var connection = new LdapConnection("server"))
            {
                AssertExtensions.Throws<InvalidEnumArgumentException>("value", () => connection.SessionOptions.ReferralChasing = referralChasing);
            }
        }

        [Fact]
        public void ReferralChasing_GetSetWhenDisposed_ThrowsObjectDisposedException()
        {
            var connection = new LdapConnection("server");
            connection.Dispose();

            Assert.Throws<ObjectDisposedException>(() => connection.SessionOptions.ReferralChasing);
            Assert.Throws<ObjectDisposedException>(() => connection.SessionOptions.ReferralChasing = ReferralChasingOptions.All);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void SecureSocketLayer_Set_GetReturnsExpected(bool value)
        {
            using (var connection = new LdapConnection("server"))
            {
                LdapSessionOptions options = connection.SessionOptions;
                Assert.False(options.SecureSocketLayer);

                options.SecureSocketLayer = value;
                Assert.False(options.SecureSocketLayer);
            }
        }

        [Fact]
        public void SecureSocketLayer_GetSetWhenDisposed_ThrowsObjectDisposedException()
        {
            var connection = new LdapConnection("server");
            connection.Dispose();

            Assert.Throws<ObjectDisposedException>(() => connection.SessionOptions.SecureSocketLayer);
            Assert.Throws<ObjectDisposedException>(() => connection.SessionOptions.SecureSocketLayer = true);
        }

        [Fact]
        public void ReferralHopLimit_Set_GetReturnsExpected()
        {
            using (var connection = new LdapConnection("server"))
            {
                LdapSessionOptions options = connection.SessionOptions;
                Assert.Equal(32, options.ReferralHopLimit);

                options.ReferralHopLimit = 10;
                Assert.Equal(10, options.ReferralHopLimit);
            }
        }

        [Fact]
        public void ReferralHopLimit_SetNegative_ThrowsArgumentException()
        {
            using (var connection = new LdapConnection("server"))
            {
                AssertExtensions.Throws<ArgumentException>("value", () => connection.SessionOptions.ReferralHopLimit = -1);
            }
        }

        [Fact]
        public void ReferralHopLimit_GetSetWhenDisposed_ThrowsObjectDisposedException()
        {
            var connection = new LdapConnection("server");
            connection.Dispose();

            Assert.Throws<ObjectDisposedException>(() => connection.SessionOptions.ReferralHopLimit);
            Assert.Throws<ObjectDisposedException>(() => connection.SessionOptions.ReferralHopLimit = 10);
        }

        [Fact]
        public void ProtocolVersion_Set_GetReturnsExpected()
        {
            using (var connection = new LdapConnection("server"))
            {
                LdapSessionOptions options = connection.SessionOptions;
                Assert.Equal(2, options.ProtocolVersion);

                options.ProtocolVersion = 3;
                Assert.Equal(3, options.ProtocolVersion);

                options.ProtocolVersion = 2;
                Assert.Equal(2, options.ProtocolVersion);
            }
        }
        
        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        public void ProtocolVersion_SetInvalid_ThrowsLdapException(int value)
        {
            using (var connection = new LdapConnection("server"))
            {
                LdapSessionOptions options = connection.SessionOptions;
                Assert.Throws<LdapException>(() => options.ProtocolVersion = value);
            }
        }

        [Fact]
        public void ProtocolVersion_GetSetWhenDisposed_ThrowsObjectDisposedException()
        {
            var connection = new LdapConnection("server");
            connection.Dispose();

            Assert.Throws<ObjectDisposedException>(() => connection.SessionOptions.ProtocolVersion);
            Assert.Throws<ObjectDisposedException>(() => connection.SessionOptions.ProtocolVersion = 10);
        }

        [Fact]
        public void HostName_Set_GetReturnsExpected()
        {
            using (var connection = new LdapConnection("server"))
            {
                LdapSessionOptions options = connection.SessionOptions;
                Assert.Null(options.HostName);

                options.HostName = "HostName";
                Assert.Equal("HostName", options.HostName);

                options.HostName = null;
                Assert.Equal("HostName", options.HostName);
            }
        }

        [Fact]
        public void HostName_GetSetWhenDisposed_ThrowsObjectDisposedException()
        {
            var connection = new LdapConnection("server");
            connection.Dispose();

            Assert.Throws<ObjectDisposedException>(() => connection.SessionOptions.HostName);
            Assert.Throws<ObjectDisposedException>(() => connection.SessionOptions.HostName = null);
        }

        [Fact]
        public void DomainName_Set_GetReturnsExpected()
        {
            using (var connection = new LdapConnection("server"))
            {
                LdapSessionOptions options = connection.SessionOptions;
                Assert.Null(options.DomainName);

                options.DomainName = "DomainName";
                Assert.Equal("DomainName", options.DomainName);

                options.DomainName = null;
                Assert.Null(options.DomainName);
            }
        }

        [Fact]
        public void DomainName_GetSetWhenDisposed_ThrowsObjectDisposedException()
        {
            var connection = new LdapConnection("server");
            connection.Dispose();

            Assert.Throws<ObjectDisposedException>(() => connection.SessionOptions.DomainName);
            Assert.Throws<ObjectDisposedException>(() => connection.SessionOptions.DomainName = null);
        }

        [Theory]
        [InlineData(LocatorFlags.AvoidSelf)]
        [InlineData(LocatorFlags.None - 1)]
        public void LocatorFlag_Set_GetReturnsExpected(LocatorFlags value)
        {
            using (var connection = new LdapConnection("server"))
            {
                LdapSessionOptions options = connection.SessionOptions;
                Assert.Equal(LocatorFlags.None, options.LocatorFlag);

                options.LocatorFlag = value;
                Assert.Equal(value, options.LocatorFlag);
            }
        }

        [Fact]
        public void LocatorFlag_GetSetWhenDisposed_ThrowsObjectDisposedException()
        {
            var connection = new LdapConnection("server");
            connection.Dispose();

            Assert.Throws<ObjectDisposedException>(() => connection.SessionOptions.LocatorFlag);
            Assert.Throws<ObjectDisposedException>(() => connection.SessionOptions.LocatorFlag = LocatorFlags.AvoidSelf);
        }

        [Fact]
        public void HostReachable_Get_ReturnsTrue()
        {
            using (var connection = new LdapConnection("server"))
            {
                LdapSessionOptions options = connection.SessionOptions;
                Assert.True(options.HostReachable);
            }
        }

        [Fact]
        public void HostReachable_GetWhenDisposed_ThrowsObjectDisposedException()
        {
            var connection = new LdapConnection("server");
            connection.Dispose();

            Assert.Throws<ObjectDisposedException>(() => connection.SessionOptions.HostReachable);
        }

        [Fact]
        public void PingKeepAliveTimeout_Set_GetReturnsExpected()
        {
            using (var connection = new LdapConnection("server"))
            {
                LdapSessionOptions options = connection.SessionOptions;
                Assert.Equal(TimeSpan.FromMinutes(2), options.PingKeepAliveTimeout);

                options.PingKeepAliveTimeout = TimeSpan.FromSeconds(10);
                Assert.Equal(TimeSpan.FromSeconds(10), options.PingKeepAliveTimeout);
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData((long)int.MaxValue + 1)]
        public void PingKeepAliveTimeout_InvalidTotalSeconds_ThrowsArgumentException(long seconds)
        {
            using (var connection = new LdapConnection("server"))
            {
                AssertExtensions.Throws<ArgumentException>("value", () => connection.SessionOptions.PingKeepAliveTimeout = TimeSpan.FromSeconds(seconds));
            }
        }

        [Fact]
        public void PingKeepAliveTimeout_GetSetWhenDisposed_ThrowsObjectDisposedException()
        {
            var connection = new LdapConnection("server");
            connection.Dispose();

            Assert.Throws<ObjectDisposedException>(() => connection.SessionOptions.PingKeepAliveTimeout);
            Assert.Throws<ObjectDisposedException>(() => connection.SessionOptions.PingKeepAliveTimeout = TimeSpan.Zero);
        }

        [Fact]
        public void PingLimit_Set_GetReturnsExpected()
        {
            using (var connection = new LdapConnection("server"))
            {
                LdapSessionOptions options = connection.SessionOptions;
                Assert.Equal(4, options.PingLimit);

                options.PingLimit = 10;
                Assert.Equal(10, options.PingLimit);
            }
        }

        [Fact]
        public void PingLimit_SetNegative_ThrowsArgumentException()
        {
            using (var connection = new LdapConnection("server"))
            {
                AssertExtensions.Throws<ArgumentException>("value", () => connection.SessionOptions.PingLimit = -1);
            }
        }

        [Fact]
        public void PingLimit_GetSetWhenDisposed_ThrowsObjectDisposedException()
        {
            var connection = new LdapConnection("server");
            connection.Dispose();

            Assert.Throws<ObjectDisposedException>(() => connection.SessionOptions.PingLimit);
            Assert.Throws<ObjectDisposedException>(() => connection.SessionOptions.PingLimit = 10);
        }

        [Fact]
        public void PingWaitTimeout_Set_GetReturnsExpected()
        {
            using (var connection = new LdapConnection("server"))
            {
                LdapSessionOptions options = connection.SessionOptions;
                Assert.Equal(TimeSpan.FromSeconds(2), options.PingWaitTimeout);

                options.PingWaitTimeout = TimeSpan.FromSeconds(10);
                Assert.Equal(TimeSpan.FromSeconds(10), options.PingWaitTimeout);
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData((long)int.MaxValue + 1)]
        public void PingWaitTimeout_InvalidTotalSeconds_ThrowsArgumentException(long seconds)
        {
            using (var connection = new LdapConnection("server"))
            {
                AssertExtensions.Throws<ArgumentException>("value", () => connection.SessionOptions.PingWaitTimeout = TimeSpan.FromSeconds(seconds));
            }
        }

        [Fact]
        public void PingWaitTimeout_GetSetWhenDisposed_ThrowsObjectDisposedException()
        {
            var connection = new LdapConnection("server");
            connection.Dispose();

            Assert.Throws<ObjectDisposedException>(() => connection.SessionOptions.PingWaitTimeout);
            Assert.Throws<ObjectDisposedException>(() => connection.SessionOptions.PingWaitTimeout = TimeSpan.Zero);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AutoReconnect_Set_GetReturnsExpected(bool value)
        {
            using (var connection = new LdapConnection("server"))
            {
                LdapSessionOptions options = connection.SessionOptions;
                Assert.True(options.AutoReconnect);

                options.AutoReconnect = value;
                Assert.Equal(value, options.AutoReconnect);
            }
        }

        [Fact]
        public void AutoReconnect_GetSetWhenDisposed_ThrowsObjectDisposedException()
        {
            var connection = new LdapConnection("server");
            connection.Dispose();

            Assert.Throws<ObjectDisposedException>(() => connection.SessionOptions.AutoReconnect);
            Assert.Throws<ObjectDisposedException>(() => connection.SessionOptions.AutoReconnect = false);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(10)]
        public void SspiFlag_Set_GetReturnsExpected(int value)
        {
            using (var connection = new LdapConnection("server"))
            {
                LdapSessionOptions options = connection.SessionOptions;
                Assert.Equal(16386, options.SspiFlag);

                options.SspiFlag = value;
                Assert.Equal(value, options.SspiFlag);
            }
        }

        [Fact]
        public void SspiFlag_GetSetWhenDisposed_ThrowsObjectDisposedException()
        {
            var connection = new LdapConnection("server");
            connection.Dispose();

            Assert.Throws<ObjectDisposedException>(() => connection.SessionOptions.SspiFlag);
            Assert.Throws<ObjectDisposedException>(() => connection.SessionOptions.SspiFlag = 10);
        }

        [Fact]
        public void SslInformation_GetNotStarted_ThrowsDirectoryOperationException()
        {
            using (var connection = new LdapConnection("server"))
            {
                LdapSessionOptions options = connection.SessionOptions;
                Assert.Throws<DirectoryOperationException>(() => options.SslInformation);
            }
        }

        [Fact]
        public void SslInformation_GetSetWhenDisposed_ThrowsObjectDisposedException()
        {
            var connection = new LdapConnection("server");
            connection.Dispose();

            Assert.Throws<ObjectDisposedException>(() => connection.SessionOptions.SslInformation);
        }

        [Fact]
        public void SecurityContext_GetNotStarted_ThrowsDirectoryOperationException()
        {
            using (var connection = new LdapConnection("server"))
            {
                LdapSessionOptions options = connection.SessionOptions;
                Assert.Throws<DirectoryOperationException>(() => options.SecurityContext);
            }
        }

        [Fact]
        public void SecurityContext_GetSetWhenDisposed_ThrowsObjectDisposedException()
        {
            var connection = new LdapConnection("server");
            connection.Dispose();

            Assert.Throws<ObjectDisposedException>(() => connection.SessionOptions.SecurityContext);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Signing_Set_GetReturnsExpected(bool value)
        {
            using (var connection = new LdapConnection("server"))
            {
                LdapSessionOptions options = connection.SessionOptions;
                Assert.False(options.Signing);

                options.Signing = value;
                Assert.Equal(value, options.Signing);
            }
        }

        [Fact]
        public void Signing_GetSetWhenDisposed_ThrowsObjectDisposedException()
        {
            var connection = new LdapConnection("server");
            connection.Dispose();

            Assert.Throws<ObjectDisposedException>(() => connection.SessionOptions.Signing);
            Assert.Throws<ObjectDisposedException>(() => connection.SessionOptions.Signing = false);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Sealing_Set_GetReturnsExpected(bool value)
        {
            using (var connection = new LdapConnection("server"))
            {
                LdapSessionOptions options = connection.SessionOptions;
                Assert.False(options.Sealing);

                options.Sealing = value;
                Assert.Equal(value, options.Sealing);
            }
        }

        [Fact]
        public void Sealing_GetSetWhenDisposed_ThrowsObjectDisposedException()
        {
            var connection = new LdapConnection("server");
            connection.Dispose();

            Assert.Throws<ObjectDisposedException>(() => connection.SessionOptions.Sealing);
            Assert.Throws<ObjectDisposedException>(() => connection.SessionOptions.Sealing = false);
        }

        [Fact]
        public void SaslMethod_Set_ThrowsLdapException()
        {
            using (var connection = new LdapConnection("server"))
            {
                LdapSessionOptions options = connection.SessionOptions;
                Assert.Null(options.SaslMethod);

                Assert.Throws<LdapException>(() => options.SaslMethod = "SaslMethod");
            }
        }

        [Fact]
        public void SaslMethod_GetSetWhenDisposed_ThrowsObjectDisposedException()
        {
            var connection = new LdapConnection("server");
            connection.Dispose();

            Assert.Throws<ObjectDisposedException>(() => connection.SessionOptions.SaslMethod);
            Assert.Throws<ObjectDisposedException>(() => connection.SessionOptions.SaslMethod = null);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void RootDseCache_Set_GetReturnsExpected(bool value)
        {
            using (var connection = new LdapConnection("server"))
            {
                LdapSessionOptions options = connection.SessionOptions;
                Assert.True(options.RootDseCache);

                options.RootDseCache = value;
                Assert.Equal(value, options.RootDseCache);
            }
        }

        [Fact]
        public void RootDseCache_GetSetWhenDisposed_ThrowsObjectDisposedException()
        {
            var connection = new LdapConnection("server");
            connection.Dispose();

            Assert.Throws<ObjectDisposedException>(() => connection.SessionOptions.RootDseCache);
            Assert.Throws<ObjectDisposedException>(() => connection.SessionOptions.RootDseCache = false);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TcpKeepAlive_Set_GetReturnsExpected(bool value)
        {
            using (var connection = new LdapConnection("server"))
            {
                LdapSessionOptions options = connection.SessionOptions;
                Assert.False(options.TcpKeepAlive);

                options.TcpKeepAlive = value;
                Assert.Equal(value, options.TcpKeepAlive);
            }
        }

        [Fact]
        public void TcpKeepAlive_GetSetWhenDisposed_ThrowsObjectDisposedException()
        {
            var connection = new LdapConnection("server");
            connection.Dispose();

            Assert.Throws<ObjectDisposedException>(() => connection.SessionOptions.TcpKeepAlive);
            Assert.Throws<ObjectDisposedException>(() => connection.SessionOptions.TcpKeepAlive = false);
        }

        [Fact]
        public void SendTimeout_Set_GetReturnsExpected()
        {
            using (var connection = new LdapConnection("server"))
            {
                LdapSessionOptions options = connection.SessionOptions;
                Assert.Equal(TimeSpan.FromSeconds(-1), options.SendTimeout);

                options.SendTimeout = TimeSpan.FromSeconds(10);
                Assert.Equal(TimeSpan.FromSeconds(10), options.SendTimeout);
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData((long)int.MaxValue + 1)]
        public void SendTimeout_InvalidTotalSeconds_ThrowsArgumentException(long seconds)
        {
            using (var connection = new LdapConnection("server"))
            {
                AssertExtensions.Throws<ArgumentException>("value", () => connection.SessionOptions.SendTimeout = TimeSpan.FromSeconds(seconds));
            }
        }

        [Fact]
        public void SendTimeout_GetSetWhenDisposed_ThrowsObjectDisposedException()
        {
            var connection = new LdapConnection("server");
            connection.Dispose();

            Assert.Throws<ObjectDisposedException>(() => connection.SessionOptions.SendTimeout);
            Assert.Throws<ObjectDisposedException>(() => connection.SessionOptions.SendTimeout = TimeSpan.Zero);
        }

        [Fact]
        public void ReferralCallback_Get_ReturnsException()
        {
            using (var connection = new LdapConnection("server"))
            {
                LdapSessionOptions options = connection.SessionOptions;
                Assert.Null(options.ReferralCallback.DereferenceConnection);
                Assert.Null(options.ReferralCallback.NotifyNewConnection);
                Assert.Null(options.ReferralCallback.QueryForConnection);
                Assert.Same(options.ReferralCallback, options.ReferralCallback);
            }
        }

        [Fact]
        public void ReferralCallback_Set_GetReturnsExpected()
        {
            using (var connection = new LdapConnection("server"))
            {
                LdapSessionOptions options = connection.SessionOptions;
                var value = new ReferralCallback
                {
                    DereferenceConnection = ReferralCallbackTests.DereferenceConnection,
                    NotifyNewConnection = ReferralCallbackTests.NotifyNewConnection,
                    QueryForConnection = ReferralCallbackTests.QueryForConnection
                };
                options.ReferralCallback = value;
                Assert.Same(value, options.ReferralCallback);

                options.ReferralCallback = null;
                Assert.Null(options.ReferralCallback);
            }
        }

        [Fact]
        public void ReferralCallback_GetGetSetWhenDisposed_ThrowsObjectDisposedException()
        {
            var connection = new LdapConnection("server");
            connection.Dispose();

            Assert.Throws<ObjectDisposedException>(() => connection.SessionOptions.ReferralCallback);
            Assert.Throws<ObjectDisposedException>(() => connection.SessionOptions.ReferralCallback = null);
        }

        [Fact]
        public void QueryClientCertificate_Set_GetReturnsExpected()
        {
            using (var connection = new LdapConnection("server"))
            {
                LdapSessionOptions options = connection.SessionOptions;
                Assert.Null(options.QueryClientCertificate);

                options.QueryClientCertificate = QueryClientCertificate;
                Assert.Equal(QueryClientCertificate, options.QueryClientCertificate);

                options.QueryClientCertificate = null;
                Assert.Null(options.QueryClientCertificate);
            }
        }

        public X509Certificate QueryClientCertificate(LdapConnection connection, byte[][] trustedCAs) => null;

        [Fact]
        public void QueryClientCertificate_GetGetSetWhenDisposed_ThrowsObjectDisposedException()
        {
            var connection = new LdapConnection("server");
            connection.Dispose();

            Assert.Throws<ObjectDisposedException>(() => connection.SessionOptions.QueryClientCertificate);
            Assert.Throws<ObjectDisposedException>(() => connection.SessionOptions.QueryClientCertificate = null);
        }

        [Fact]
        public void VerifyServerCertificate_Set_GetReturnsExpected()
        {
            using (var connection = new LdapConnection("server"))
            {
                LdapSessionOptions options = connection.SessionOptions;
                Assert.Null(options.VerifyServerCertificate);

                options.VerifyServerCertificate = VerifyServerCertificate;
                Assert.Equal(VerifyServerCertificate, options.VerifyServerCertificate);

                options.VerifyServerCertificate = null;
                Assert.Null(options.VerifyServerCertificate);
            }
        }

        public bool VerifyServerCertificate(LdapConnection connection, X509Certificate certificate) => false;

        [Fact]
        public void VerifyServerCertificate_GetGetSetWhenDisposed_ThrowsObjectDisposedException()
        {
            var connection = new LdapConnection("server");
            connection.Dispose();

            Assert.Throws<ObjectDisposedException>(() => connection.SessionOptions.VerifyServerCertificate);
            Assert.Throws<ObjectDisposedException>(() => connection.SessionOptions.VerifyServerCertificate = null);
        }

        [Fact]
        public void FastConcurrentBind_Disposed_ThrowsObjectDisposedException()
        {
            var connection = new LdapConnection("server");
            connection.Dispose();

            Assert.Throws<ObjectDisposedException>(() => connection.SessionOptions.FastConcurrentBind());
        }

        [Fact]
        public void StartTransportLayerSecurity_Disposed_ThrowsObjectDisposedException()
        {
            var connection = new LdapConnection("server");
            connection.Dispose();

            Assert.Throws<ObjectDisposedException>(() => connection.SessionOptions.StartTransportLayerSecurity(null));
        }

        [Fact]
        public void StopTransportLayerSecurity_NotStarted_ThrowsTlsOperationException()
        {
            using (var connection = new LdapConnection("server"))
            {
                LdapSessionOptions options = connection.SessionOptions;
                Assert.Throws<TlsOperationException>(() => options.StopTransportLayerSecurity());
            }
        }

        [Fact]
        public void StopTransportLayerSecurity_Disposed_ThrowsObjectDisposedException()
        {
            var connection = new LdapConnection("server");
            connection.Dispose();

            Assert.Throws<ObjectDisposedException>(() => connection.SessionOptions.StopTransportLayerSecurity());
        }
    }
}
