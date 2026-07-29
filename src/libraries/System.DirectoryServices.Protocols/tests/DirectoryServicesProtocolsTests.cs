// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.DirectoryServices.Tests;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Xunit;

using LdapTestServer = System.DirectoryServices.Protocols.Tests.TestServer.LdapTestServer;

namespace System.DirectoryServices.Protocols.Tests
{
    [ConditionalClass(typeof(DirectoryServicesProtocolsTests), nameof(LdapConfigurationExists))]
    public sealed class DirectoryServicesProtocolsTests : DirectoryServicesProtocolsTests<DirectoryServicesProtocolsTests.ExternalCapabilities>
    {
        private static readonly int s_port =
            LdapConfiguration.Configuration?.Port is null ?
                389 :
                int.Parse(LdapConfiguration.Configuration.Port, NumberStyles.None, CultureInfo.InvariantCulture);

        internal static bool UseTls => LdapConfigurationExists && LdapConfiguration.Configuration.UseTls;

        private sealed class ExternalConnectionState : ConnectionState
        {
            internal ExternalConnectionState(LdapConnection connection)
                : base(connection)
            {
            }

            internal override string SearchDn => LdapConfiguration.Configuration.SearchDn;
        }

        public sealed class ExternalCapabilities : Capabilities
        {
            internal override bool SupportsPagination => LdapConfiguration.Configuration?.IsActiveDirectoryServer ?? false;
            internal override bool SupportsServerSideSort => LdapConfiguration.Configuration?.SupportsServerSideSort ?? false;
        }

        protected override ConnectionState Connect()
        {
            return new ExternalConnectionState(GetConnection());
        }

        protected override ConnectionState ConnectWithPortInHostname()
        {
            LdapDirectoryIdentifier identifier = new LdapDirectoryIdentifier($"{LdapConfiguration.Configuration.ServerName}:{s_port}", fullyQualifiedDnsHostName: true, connectionless: false);
            return new ExternalConnectionState(GetConnection(identifier));
        }

        protected override ConnectionState ConnectUnboundWithServerSpecifiedTwice()
        {
            LdapDirectoryIdentifier directoryIdentifier = string.IsNullOrEmpty(LdapConfiguration.Configuration.Port) ?
                new LdapDirectoryIdentifier(new string[] { LdapConfiguration.Configuration.ServerName, LdapConfiguration.Configuration.ServerName }, true, false) :
                new LdapDirectoryIdentifier(new string[] { LdapConfiguration.Configuration.ServerName, LdapConfiguration.Configuration.ServerName },
                    s_port,
                    true, false);

            return new ExternalConnectionState(GetConnection(directoryIdentifier, bind: false));
        }

#if NET
        [ConditionalFact(typeof(DirectoryServicesProtocolsTests), nameof(UseTls))]
        [PlatformSpecific(TestPlatforms.Linux)]
        public void StartNewTlsSessionContext()
        {
            using (var connection = GetConnection(bind: false))
            {
                // We use "." as the directory since it must be a valid directory for StartNewTlsSessionContext() + Bind() to be successful even
                // though there are no client certificates in ".".
                connection.SessionOptions.TrustedCertificatesDirectory = ".";

                // For a real-world scenario, we would call 'StartTransportLayerSecurity(null)' here which would do the TLS handshake including
                // providing the client certificate to the server and validating the server certificate. However, this requires additional
                // setup that we don't have including trusting the server certificate and by specifying "demand" in the setup of the server
                // via 'LDAP_TLS_VERIFY_CLIENT=demand' to force the TLS handshake to occur.

                connection.SessionOptions.StartNewTlsSessionContext();
                connection.Bind();

                SearchRequest searchRequest = new(LdapConfiguration.Configuration.SearchDn, "(objectClass=*)", SearchScope.Subtree);
                _ = (SearchResponse)connection.SendRequest(searchRequest);
            }
        }

        [ConditionalFact(typeof(DirectoryServicesProtocolsTests), nameof(UseTls))]
        [PlatformSpecific(TestPlatforms.Linux)]
        public void StartNewTlsSessionContext_ThrowsLdapException()
        {
            using (var connection = GetConnection(bind: false))
            {
                // Create a new session context without setting TrustedCertificatesDirectory.
                connection.SessionOptions.StartNewTlsSessionContext();
                Assert.Throws<PlatformNotSupportedException>(() => connection.Bind());
            }
        }

        [ConditionalFact(typeof(DirectoryServicesProtocolsTests), nameof(LdapConfigurationExists))]
        [PlatformSpecific(TestPlatforms.Linux)]
        public void TrustedCertificatesDirectory_ThrowsDirectoryNotFoundException()
        {
            using (var connection = GetConnection(bind: false))
            {
                Assert.Throws<DirectoryNotFoundException>(() => connection.SessionOptions.TrustedCertificatesDirectory = "nonexistent");
            }
        }

        [ConditionalFact(typeof(DirectoryServicesProtocolsTests), nameof(LdapConfigurationExists))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void StartNewTlsSessionContext_ThrowsPlatformNotSupportedException()
        {
            using (var connection = new LdapConnection("server"))
            {
                LdapSessionOptions options = connection.SessionOptions;
                Assert.Throws<PlatformNotSupportedException>(() => options.StartNewTlsSessionContext());
            }
        }
#endif
    }

    [ActiveIssue("https://github.com/dotnet/runtime/issues/127070", TestRuntimes.Mono)]
    [ConditionalClass(typeof(DirectoryServicesTestHelpers), nameof(DirectoryServicesTestHelpers.IsWindowsOrLibLdapIsInstalled))]
    public sealed partial class DirectoryServicesProtocolsTests_Local : DirectoryServicesProtocolsTests<DirectoryServicesProtocolsTests_Local.LocalCapabilities>
    {
        private class LocalConnectionState : ConnectionState
        {
            private LdapTestServer TestServer { get; }
            
            internal LocalConnectionState(LdapConnection connection, LdapTestServer testServer)
                : base(connection)
            {
                TestServer = testServer;
            }

            internal override string SearchDn => TestServer.BaseDn;

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);

                if (disposing)
                {
                    TestServer.Dispose();
                }
            }
        }

        public sealed class LocalCapabilities : Capabilities
        {
            internal override bool SupportsPagination => true;
            internal override bool SupportsServerSideSort => true;
        }

        protected override ConnectionState Connect()
        {
            LdapTestServer server = StartLocalServer(out int port);
            LdapConnection connection = GetLocalConnection(port);

            return new LocalConnectionState(connection, server);
        }

        protected override ConnectionState ConnectWithPortInHostname()
        {
            LdapTestServer server = StartLocalServer(out int port);
            LdapDirectoryIdentifier identifier = new LdapDirectoryIdentifier($"localhost:{port}", fullyQualifiedDnsHostName: true, connectionless: false);
            LdapConnection connection = new LdapConnection(identifier, new NetworkCredential("cn=admin", "PLACEHOLDER"))
                {
                    AuthType = AuthType.Basic
                };

            connection.SessionOptions.ProtocolVersion = 3;
            connection.Bind();
            connection.Timeout = TimeSpan.FromSeconds(30);

            return new LocalConnectionState(connection, server);
        }

        protected override ConnectionState ConnectUnboundWithServerSpecifiedTwice()
        {
            LdapTestServer server = StartLocalServer(out int port);

            LdapDirectoryIdentifier directoryIdentifier = new LdapDirectoryIdentifier(
                new string[] { "localhost", "localhost" }, port, true, false);
            NetworkCredential credential = new NetworkCredential("cn=admin", "PLACEHOLDER");

            LdapConnection connection = new LdapConnection(directoryIdentifier, credential)
            {
                AuthType = AuthType.Basic,
            };

            return new LocalConnectionState(connection, server);
        }

        [InlineData(true)]
        [InlineData(false)]
        [Theory]
        [PlatformSpecific(TestPlatforms.Windows)] // TODO(60972): Enable on Linux/OSX when enabling the certificate acceptance callback
        public void TestVerifyServerCertificateCallback_LDAPS(bool acceptCertificate)
        {
            using (X509Certificate2 cert = CreateServerCertificate())
            using (LdapTestServer server = StartLocalServerWithLdaps(cert, out int port))
            using (LdapConnection connection = CreateLocalLdapsConnection(port, PrepareCallback(cert, acceptCertificate)))
            {
                connection.SessionOptions.ProtocolVersion = 3;

                if (acceptCertificate)
                {
                    connection.Bind();
                    connection.Timeout = TimeSpan.FromSeconds(30);

                    SearchRequest searchRequest = new SearchRequest(server.BaseDn, "(objectClass=*)", SearchScope.Base);
                    SearchResponse searchResponse = (SearchResponse)connection.SendRequest(searchRequest);
                    Assert.Equal(ResultCode.Success, searchResponse.ResultCode);
                }
                else
                {
                    Assert.Throws<LdapException>(() => connection.Bind());
                }
            }

            static VerifyServerCertificateCallback PrepareCallback(X509Certificate2 expectedCertificate, bool acceptCertificate)
            {
                byte[] expectedRawData = expectedCertificate.RawData;

                return (conn, certificate) =>
                {
                    Assert.Equal(expectedRawData, certificate.GetRawCertData());
                    return acceptCertificate;
                };
            }
        }

        [InlineData(true)]
        [InlineData(false)]
        [Theory]
        [PlatformSpecific(TestPlatforms.Windows)] // TODO(60972): Enable on Linux/OSX when enabling the certificate acceptance callback
        public void TestVerifyServerCertificateCallback_StartTLS(bool acceptCertificate)
        {
            using (X509Certificate2 cert = CreateServerCertificate())
            using (LdapTestServer server = StartLocalServerWithStartTLS(cert, out int port))
            using (LdapConnection connection = GetLocalConnection(port))
            {
                byte[] expectedRawData = cert.RawData;
                connection.SessionOptions.VerifyServerCertificate = (conn, certificate) =>
                {
                    Assert.Equal(expectedRawData, certificate.GetRawCertData());
                    return acceptCertificate;
                };

                if (!acceptCertificate)
                {
                    Assert.Throws<LdapException>(() =>
                        connection.SessionOptions.StartTransportLayerSecurity(null));
                }
                else
                {
                    connection.SessionOptions.StartTransportLayerSecurity(null);
                }

                SearchRequest searchRequest = new SearchRequest(server.BaseDn, "(objectClass=*)", SearchScope.Base);

                if (acceptCertificate)
                {
                    SearchResponse searchResponse = (SearchResponse)connection.SendRequest(searchRequest);
                    Assert.Equal(ResultCode.Success, searchResponse.ResultCode);
                }
                else
                {
                    Assert.Throws<LdapException>(() => connection.SendRequest(searchRequest));
                }
            }
        }
    }

    public abstract partial class DirectoryServicesProtocolsTests<TCapabilities>
        where TCapabilities : DirectoryServicesProtocolsTests<TCapabilities>.Capabilities, new()
    {
        private static readonly Capabilities s_capabilities = new TCapabilities();

        internal static bool ServerSupportsPagination => s_capabilities.SupportsPagination;
        internal static bool LdapConfigurationExists => LdapConfiguration.Configuration != null;

        internal static bool IsServerSideSortSupported => s_capabilities.SupportsServerSideSort;

        protected abstract class ConnectionState : IDisposable
        {
            internal LdapConnection Connection { get; }

            internal ConnectionState(LdapConnection connection)
            {
                Connection = connection;
            }

            internal abstract string SearchDn { get; }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    Connection.Dispose();
                }
            }
        }

        public abstract class Capabilities
        {
            internal abstract bool SupportsPagination { get; }
            internal abstract bool SupportsServerSideSort { get; }
        }

        protected abstract ConnectionState Connect();
        protected abstract ConnectionState ConnectWithPortInHostname();
        protected abstract ConnectionState ConnectUnboundWithServerSpecifiedTwice();

        [Fact]
        public void TestInvalidFilter()
        {
            using (ConnectionState state = Connect())
            {
                LdapConnection connection = state.Connection;

                LdapException ex = Assert.Throws<LdapException>(() =>
                {
                    var searchRequest = new SearchRequest(state.SearchDn, "==invalid==", SearchScope.OneLevel);
                    _ = (SearchResponse)connection.SendRequest(searchRequest);
                });

                Assert.Equal( /* LdapError.FilterError */ 0x57, ex.ErrorCode);
            }
        }

        [Fact]
        public void TestInvalidSearchDn()
        {
            using (ConnectionState state = Connect())
            {
                DirectoryOperationException ex = Assert.Throws<DirectoryOperationException>(() =>
                {
                    var searchRequest = new SearchRequest("==invaliddn==", "(objectClass=*)", SearchScope.OneLevel);
                    var searchResponse = (SearchResponse)state.Connection.SendRequest(searchRequest);
                });

                Assert.Equal(ResultCode.InvalidDNSyntax, ex.Response.ResultCode);
            }
        }

        [Fact]
        public void TestUnavailableCriticalExtension()
        {
            using (ConnectionState state = Connect())
            {
                DirectoryOperationException ex = Assert.Throws<DirectoryOperationException>(() =>
                {
                    var searchRequest = new SearchRequest(state.SearchDn, "(objectClass=*)", SearchScope.OneLevel);
                    var control = new DirectoryControl("==invalid-control==", value: null, isCritical: true, serverSide: true);
                    searchRequest.Controls.Add(control);
                    _ = (SearchResponse)state.Connection.SendRequest(searchRequest);
                });

                Assert.Equal(ResultCode.UnavailableCriticalExtension, ex.Response.ResultCode);
            }
        }

        [Fact]
        public void TestUnavailableNonCriticalExtension()
        {
            using (ConnectionState state = Connect())
            {
                var searchRequest = new SearchRequest(state.SearchDn, "(objectClass=*)", SearchScope.OneLevel);
                var control = new DirectoryControl("==invalid-control==", value: null, isCritical: false, serverSide: true);
                searchRequest.Controls.Add(control);
                _ = (SearchResponse)state.Connection.SendRequest(searchRequest);
                // Does not throw
            }
        }

        [Fact]
        public void TestServerWithPortNumber()
        {
            using (ConnectionState state = ConnectWithPortInHostname())
            {
                var searchRequest = new SearchRequest(state.SearchDn, "(objectClass=*)", SearchScope.Subtree);
                _ = (SearchResponse)state.Connection.SendRequest(searchRequest);
                // Shall succeed
            }
        }

        [InlineData(60)]
        [InlineData(0)]
        [InlineData(-60)]
        [Theory]
        public void TestSearchWithTimeLimit(int timeLimit)
        {
            using (ConnectionState state = Connect())
            {
                var searchRequest = new SearchRequest(state.SearchDn, "(objectClass=*)", SearchScope.Subtree);
                if (timeLimit < 0)
                {
                    Assert.Throws<ArgumentException>(() => searchRequest.TimeLimit = TimeSpan.FromSeconds(timeLimit));
                }
                else
                {
                    searchRequest.TimeLimit = TimeSpan.FromSeconds(timeLimit);
                    _ = (SearchResponse)state.Connection.SendRequest(searchRequest);
                    // Shall succeed
                }
            }
        }

        [Fact]
        public void TestAddingOU()
        {
            using (ConnectionState state = Connect())
            {
                string ouName = "ProtocolsGroup1";
                string dn = "ou=" + ouName;

                try
                {
                    DeleteEntry(state.Connection, dn, state.SearchDn);
                    AddOrganizationalUnit(state.Connection, dn, state.SearchDn);
                    SearchResultEntry sre = SearchOrganizationalUnit(state.Connection, state.SearchDn, ouName);
                    Assert.NotNull(sre);
                }
                finally
                {
                    DeleteEntry(state.Connection, dn, state.SearchDn);
                }
            }
        }

        [Fact]
        public void TestDeleteOU()
        {
            using (ConnectionState state = Connect())
            {
                string ouName = "ProtocolsGroup2";
                string dn = "ou=" + ouName;
                try
                {
                    DeleteEntry(state.Connection, dn, state.SearchDn);
                    AddOrganizationalUnit(state.Connection, dn, state.SearchDn);
                    SearchResultEntry sre = SearchOrganizationalUnit(state.Connection, state.SearchDn, ouName);
                    Assert.NotNull(sre);

                    DeleteEntry(state.Connection, dn, state.SearchDn);
                    sre = SearchOrganizationalUnit(state.Connection, state.SearchDn, ouName);
                    Assert.Null(sre);
                }
                finally
                {
                    DeleteEntry(state.Connection, dn, state.SearchDn);
                }
            }
        }

        [Fact]
        public void TestAddAndModifyAttribute()
        {
            using (ConnectionState state = Connect())
            {
                string ouName = "ProtocolsGroup3";
                string dn = "ou=" + ouName;
                try
                {
                    DeleteEntry(state.Connection, dn, state.SearchDn);
                    AddOrganizationalUnit(state.Connection, dn, state.SearchDn);

                    AddAttribute(state.Connection, dn, "description", "Protocols Group 3", state.SearchDn);
                    SearchResultEntry sre = SearchOrganizationalUnit(state.Connection, state.SearchDn, ouName);
                    Assert.NotNull(sre);
                    Assert.Equal("Protocols Group 3", (string)sre.Attributes["description"][0]);
                    Assert.Throws<DirectoryOperationException>(() => AddAttribute(state.Connection, dn, "description", "Protocols Group 3", state.SearchDn));

                    ModifyAttribute(state.Connection, dn, "description", "Modified Protocols Group 3", state.SearchDn);
                    sre = SearchOrganizationalUnit(state.Connection, state.SearchDn, ouName);
                    Assert.NotNull(sre);
                    Assert.Equal("Modified Protocols Group 3", (string)sre.Attributes["description"][0]);

                    DeleteAttribute(state.Connection, dn, "description", state.SearchDn);
                    sre = SearchOrganizationalUnit(state.Connection, state.SearchDn, ouName);
                    Assert.NotNull(sre);
                    Assert.Null(sre.Attributes["description"]);
                }
                finally
                {
                    DeleteEntry(state.Connection, dn, state.SearchDn);
                }
            }
        }

        [Fact]
        public void TestNestedOUs()
        {
            using (ConnectionState state = Connect())
            {
                string ouLevel1Name = "ProtocolsGroup4-1";
                string dnLevel1 = "ou=" + ouLevel1Name;
                string ouLevel2Name = "ProtocolsGroup4-2";
                string dnLevel2 = "ou=" + ouLevel2Name+ "," + dnLevel1;

                DeleteEntry(state.Connection, dnLevel2, state.SearchDn);
                DeleteEntry(state.Connection, dnLevel1, state.SearchDn);

                try
                {
                    AddOrganizationalUnit(state.Connection, dnLevel1, state.SearchDn);
                    SearchResultEntry sre = SearchOrganizationalUnit(state.Connection, state.SearchDn, ouLevel1Name);
                    Assert.NotNull(sre);

                    AddOrganizationalUnit(state.Connection, dnLevel2, state.SearchDn);
                    sre = SearchOrganizationalUnit(state.Connection, dnLevel1 + "," + state.SearchDn, ouLevel2Name);
                    Assert.NotNull(sre);
                }
                finally
                {
                    DeleteEntry(state.Connection, dnLevel2, state.SearchDn);
                    DeleteEntry(state.Connection, dnLevel1, state.SearchDn);
                }
            }
        }

        [Fact]
        public void TestAddUser()
        {
            using (ConnectionState state = Connect())
            {
                string ouName = "ProtocolsGroup5";
                string dn = "ou=" + ouName;
                string user1Dn = "cn=protocolUser1" + "," + dn;
                string user2Dn = "cn=protocolUser2" + "," + dn;

                DeleteEntry(state.Connection, user1Dn, state.SearchDn);
                DeleteEntry(state.Connection, user2Dn, state.SearchDn);
                DeleteEntry(state.Connection, dn, state.SearchDn);

                try
                {
                    AddOrganizationalUnit(state.Connection, dn, state.SearchDn);
                    SearchResultEntry sre = SearchOrganizationalUnit(state.Connection, state.SearchDn, ouName);
                    Assert.NotNull(sre);

                    AddOrganizationalRole(state.Connection, user1Dn, state.SearchDn);
                    AddOrganizationalRole(state.Connection, user2Dn, state.SearchDn);

                    string usersRoot = dn + "," + state.SearchDn;

                    sre = SearchUser(state.Connection, usersRoot, "protocolUser1");
                    Assert.NotNull(sre);

                    sre = SearchUser(state.Connection, usersRoot, "protocolUser2");
                    Assert.NotNull(sre);

                    DeleteEntry(state.Connection, user1Dn, state.SearchDn);
                    sre = SearchUser(state.Connection, usersRoot, "protocolUser1");
                    Assert.Null(sre);

                    DeleteEntry(state.Connection, user2Dn, state.SearchDn);
                    sre = SearchUser(state.Connection, usersRoot, "protocolUser2");
                    Assert.Null(sre);

                    DeleteEntry(state.Connection, dn, state.SearchDn);
                    sre = SearchOrganizationalUnit(state.Connection, state.SearchDn, ouName);
                    Assert.Null(sre);
                }
                finally
                {
                    DeleteEntry(state.Connection, user1Dn, state.SearchDn);
                    DeleteEntry(state.Connection, user2Dn, state.SearchDn);
                    DeleteEntry(state.Connection, dn, state.SearchDn);
                }
            }
        }

        [Fact]
        public void TestAddingMultipleAttributes()
        {
            using (ConnectionState state = Connect())
            {
                string ouName = "ProtocolsGroup6";
                string dn = "ou=" + ouName;
                try
                {
                    DeleteEntry(state.Connection, dn, state.SearchDn);
                    AddOrganizationalUnit(state.Connection, dn, state.SearchDn);

                    DirectoryAttributeModification mod1 = new DirectoryAttributeModification();
                    mod1.Operation = DirectoryAttributeOperation.Add;
                    mod1.Name = "description";
                    mod1.Add("Description 5");

                    DirectoryAttributeModification mod2 = new DirectoryAttributeModification();
                    mod2.Operation = DirectoryAttributeOperation.Add;
                    mod2.Name = "postalAddress";
                    mod2.Add("123 4th Ave NE, State, Country");

                    DirectoryAttributeModification[] mods = new DirectoryAttributeModification[2] { mod1, mod2 };

                    string fullDn = dn + "," + state.SearchDn;

                    ModifyRequest modRequest = new ModifyRequest(fullDn, mods);
                    ModifyResponse modResponse = (ModifyResponse)state.Connection.SendRequest(modRequest);
                    Assert.Equal(ResultCode.Success, modResponse.ResultCode);
                    Assert.Throws<DirectoryOperationException>(() => (ModifyResponse)state.Connection.SendRequest(modRequest));

                    SearchResultEntry sre = SearchOrganizationalUnit(state.Connection, state.SearchDn, ouName);
                    Assert.NotNull(sre);
                    Assert.Equal("Description 5", (string)sre.Attributes["description"][0]);
                    Assert.Throws<DirectoryOperationException>(() => AddAttribute(state.Connection, dn, "description", "Description 5", state.SearchDn));
                    Assert.Equal("123 4th Ave NE, State, Country", (string)sre.Attributes["postalAddress"][0]);
                    Assert.Throws<DirectoryOperationException>(() => AddAttribute(state.Connection, dn, "postalAddress", "123 4th Ave NE, State, Country", state.SearchDn));

                    mod1 = new DirectoryAttributeModification();
                    mod1.Operation = DirectoryAttributeOperation.Replace;
                    mod1.Name = "description";
                    mod1.Add("Modified Description 5");

                    mod2 = new DirectoryAttributeModification();
                    mod2.Operation = DirectoryAttributeOperation.Replace;
                    mod2.Name = "postalAddress";
                    mod2.Add("689 5th Ave NE, State, Country");
                    mods = new DirectoryAttributeModification[2] { mod1, mod2 };
                    modRequest = new ModifyRequest(fullDn, mods);
                    modResponse = (ModifyResponse)state.Connection.SendRequest(modRequest);
                    Assert.Equal(ResultCode.Success, modResponse.ResultCode);

                    sre = SearchOrganizationalUnit(state.Connection, state.SearchDn, ouName);
                    Assert.NotNull(sre);
                    Assert.Equal("Modified Description 5", (string)sre.Attributes["description"][0]);
                    Assert.Throws<DirectoryOperationException>(() => AddAttribute(state.Connection, dn, "description", "Modified Description 5", state.SearchDn));
                    Assert.Equal("689 5th Ave NE, State, Country", (string)sre.Attributes["postalAddress"][0]);
                    Assert.Throws<DirectoryOperationException>(() => AddAttribute(state.Connection, dn, "postalAddress", "689 5th Ave NE, State, Country", state.SearchDn));

                    mod1 = new DirectoryAttributeModification();
                    mod1.Operation = DirectoryAttributeOperation.Delete;
                    mod1.Name = "description";

                    mod2 = new DirectoryAttributeModification();
                    mod2.Operation = DirectoryAttributeOperation.Delete;
                    mod2.Name = "postalAddress";
                    mods = new DirectoryAttributeModification[2] { mod1, mod2 };
                    modRequest = new ModifyRequest(fullDn, mods);
                    modResponse = (ModifyResponse)state.Connection.SendRequest(modRequest);
                    Assert.Equal(ResultCode.Success, modResponse.ResultCode);

                    sre = SearchOrganizationalUnit(state.Connection, state.SearchDn, ouName);
                    Assert.NotNull(sre);
                    Assert.Null(sre.Attributes["description"]);
                    Assert.Null(sre.Attributes["postalAddress"]);
                }
                finally
                {
                    DeleteEntry(state.Connection, dn, state.SearchDn);
                }
            }
        }

        [Fact]
        public void TestMoveAndRenameUser()
        {
            using (ConnectionState state = Connect())
            {
                string ouName1 = "ProtocolsGroup7.1";
                string dn1 = "ou=" + ouName1;

                string ouName2 = "ProtocolsGroup7.2";
                string dn2 = "ou=" + ouName2;

                string userDn1 = "cn=protocolUser7.1" + "," + dn1;
                string userDn2 = "cn=protocolUser7.2" + "," + dn2;

                DeleteEntry(state.Connection, userDn1, state.SearchDn);
                DeleteEntry(state.Connection, userDn2, state.SearchDn);
                DeleteEntry(state.Connection, dn1, state.SearchDn);
                DeleteEntry(state.Connection, dn2, state.SearchDn);

                try
                {
                    AddOrganizationalUnit(state.Connection, dn1, state.SearchDn);
                    SearchResultEntry sre = SearchOrganizationalUnit(state.Connection, state.SearchDn, ouName1);
                    Assert.NotNull(sre);

                    AddOrganizationalUnit(state.Connection, dn2, state.SearchDn);
                    sre = SearchOrganizationalUnit(state.Connection, state.SearchDn, ouName2);
                    Assert.NotNull(sre);

                    AddOrganizationalRole(state.Connection, userDn1, state.SearchDn);

                    string user1Root = dn1 + "," + state.SearchDn;
                    string user2Root = dn2 + "," + state.SearchDn;

                    sre = SearchUser(state.Connection, user1Root, "protocolUser7.1");
                    Assert.NotNull(sre);

                    ModifyDNRequest modDnRequest = new ModifyDNRequest( userDn1 + "," + state.SearchDn,
                                                                        dn2 + "," + state.SearchDn,
                                                                        "cn=protocolUser7.2");
                    ModifyDNResponse modDnResponse = (ModifyDNResponse)state.Connection.SendRequest(modDnRequest);
                    Assert.Equal(ResultCode.Success, modDnResponse.ResultCode);

                    sre = SearchUser(state.Connection, user1Root, "protocolUser7.1");
                    Assert.Null(sre);

                    sre = SearchUser(state.Connection, user2Root, "protocolUser7.2");
                    Assert.NotNull(sre);
                }
                finally
                {
                    DeleteEntry(state.Connection, userDn1, state.SearchDn);
                    DeleteEntry(state.Connection, userDn2, state.SearchDn);
                    DeleteEntry(state.Connection, dn1, state.SearchDn);
                    DeleteEntry(state.Connection, dn2, state.SearchDn);
                }
            }
        }

        [Fact]
        public void TestAsyncSearch()
        {
            using (ConnectionState state = Connect())
            {
                string ouName = "ProtocolsGroup9";
                string dn = "ou=" + ouName;

                try
                {
                    for (int i=0; i<20; i++)
                    {
                        DeleteEntry(state.Connection, "ou=ProtocolsSubGroup9." + i + "," + dn, state.SearchDn);
                    }
                    DeleteEntry(state.Connection, dn, state.SearchDn);

                    AddOrganizationalUnit(state.Connection, dn, state.SearchDn);
                    SearchResultEntry sre = SearchOrganizationalUnit(state.Connection, state.SearchDn, ouName);
                    Assert.NotNull(sre);

                    for (int i=0; i<20; i++)
                    {
                        AddOrganizationalUnit(state.Connection, "ou=ProtocolsSubGroup9." + i + "," + dn, state.SearchDn);
                    }

                    string filter = "(objectClass=organizationalUnit)";
                    SearchRequest searchRequest = new SearchRequest(
                                                            dn + "," + state.SearchDn,
                                                            filter,
                                                            SearchScope.OneLevel,
                                                            null);

                    ASyncOperationState opState = new ASyncOperationState(state.Connection);
                    IAsyncResult asyncResult = state.Connection.BeginSendRequest(
                                                    searchRequest,
                                                    PartialResultProcessing.ReturnPartialResultsAndNotifyCallback,
                                                    RunAsyncSearch,
                                                    opState);

                    asyncResult.AsyncWaitHandle.WaitOne();
                    Assert.True(opState.Exception == null, opState.Exception == null ? "" : opState.Exception.ToString());
                }
                finally
                {
                    for (int i=0; i<20; i++)
                    {
                        DeleteEntry(state.Connection, "ou=ProtocolsSubGroup9." + i + "," + dn, state.SearchDn);
                    }
                    DeleteEntry(state.Connection, dn, state.SearchDn);
                }
            }
        }

        private static void RunAsyncSearch(IAsyncResult asyncResult)
        {
            ASyncOperationState state = (ASyncOperationState) asyncResult.AsyncState;

            try
            {
                if (!asyncResult.IsCompleted)
                {
                    PartialResultsCollection partialResult = null;
                    partialResult = state.Connection.GetPartialResults(asyncResult);

                    if (partialResult != null)
                    {
                        for (int i = 0; i < partialResult.Count; i++)
                        {
                            if (partialResult[i] is SearchResultEntry)
                            {
                                Assert.Contains("Group9", ((SearchResultEntry)partialResult[i]).DistinguishedName);
                            }
                        }
                    }
                }
                else
                {
                    SearchResponse response = (SearchResponse) state.Connection.EndSendRequest(asyncResult);

                    if (response != null)
                    {
                        foreach (SearchResultEntry entry in response.Entries)
                        {
                            Assert.Contains("Group9", entry.DistinguishedName);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                state.Exception = e;
            }
        }

        public static IEnumerable<object[]> TestCompareRequestTheory_TestData()
        {
            yield return new object[] { "input", "input", ResultCode.CompareTrue };
            yield return new object[] { "input", "input"u8.ToArray(), ResultCode.CompareTrue };

            yield return new object[] { "input", "false", ResultCode.CompareFalse };
            yield return new object[] { "input", new byte[] { 1, 2, 3, 4, 5 }, ResultCode.CompareFalse };

            yield return new object[] { "http://example.com/", "http://example.com/", ResultCode.CompareTrue };
            yield return new object[] { "http://example.com/", new Uri("http://example.com/"), ResultCode.CompareTrue };
            yield return new object[] { "http://example.com/", "http://example.com/"u8.ToArray(), ResultCode.CompareTrue };

            yield return new object[] { "http://example.com/", "http://false/", ResultCode.CompareFalse };
            yield return new object[] { "http://example.com/", new Uri("http://false/"), ResultCode.CompareFalse };
            yield return new object[] { "http://example.com/", "http://false/"u8.ToArray(), ResultCode.CompareFalse };
        }

        [Theory]
        [MemberData(nameof(TestCompareRequestTheory_TestData))]
        public void TestCompareRequestTheory(object value, object assertion, ResultCode compareResult)
        {
            using (ConnectionState state = Connect())
            {
                string ouName = "ProtocolsGroup10";
                string rdn = "ou=" + ouName;

                DeleteEntry(state.Connection, rdn, state.SearchDn);
                AddOrganizationalUnit(state.Connection, rdn, state.SearchDn);

                string dn = rdn + "," + state.SearchDn;

                // set description to value
                ModifyRequest mod = new ModifyRequest(dn, DirectoryAttributeOperation.Replace, "description", value);
                DirectoryResponse response = state.Connection.SendRequest(mod);
                Assert.Equal(ResultCode.Success, response.ResultCode);

                // compare description to assertion
                CompareRequest cmp = new CompareRequest(dn, new DirectoryAttribute("description", assertion));
                response = state.Connection.SendRequest(cmp);
                // assert compare result
                Assert.Equal(compareResult, response.ResultCode);

                // compare description to value
                cmp = new CompareRequest(dn, new DirectoryAttribute("description", value));
                response = state.Connection.SendRequest(cmp);
                // compare result always true
                Assert.Equal(ResultCode.CompareTrue, response.ResultCode);
            }
        }

        [Fact]
        public void TestCompareRequest()
        {
            using (ConnectionState state = Connect())
            {
                // negative case: ou=NotFound does not exist
                CompareRequest cmp = new CompareRequest("ou=NotFound," + state.SearchDn, "ou", "NotFound");
                Assert.Throws<DirectoryOperationException>(() => state.Connection.SendRequest(cmp));
            }
        }

        [ConditionalFact(nameof(ServerSupportsPagination))]
        public void TestPageRequests()
        {
            using (ConnectionState state = Connect())
            {
                string ouName = "ProtocolsGroup8";
                string dn = "ou=" + ouName;

                try
                {
                    for (int i=0; i<20; i++)
                    {
                        DeleteEntry(state.Connection, "ou=ProtocolsSubGroup8." + i + "," + dn, state.SearchDn);
                    }
                    DeleteEntry(state.Connection, dn, state.SearchDn);

                    AddOrganizationalUnit(state.Connection, dn, state.SearchDn);
                    SearchResultEntry sre = SearchOrganizationalUnit(state.Connection, state.SearchDn, ouName);
                    Assert.NotNull(sre);

                    for (int i=0; i<20; i++)
                    {
                        AddOrganizationalUnit(state.Connection, "ou=ProtocolsSubGroup8." + i + "," + dn, state.SearchDn);
                    }

                    string filter = "(objectClass=*)";
                    SearchRequest searchRequest = new SearchRequest(
                                                        dn + "," + state.SearchDn,
                                                        filter,
                                                        SearchScope.Subtree,
                                                        null);

                    PageResultRequestControl pageRequest = new PageResultRequestControl(5);
                    searchRequest.Controls.Add(pageRequest);
                    SearchOptionsControl searchOptions = new SearchOptionsControl(SearchOption.DomainScope);
                    searchRequest.Controls.Add(searchOptions);
                    while (true)
                    {
                        SearchResponse searchResponse = (SearchResponse)state.Connection.SendRequest(searchRequest);
                        Assert.Equal(1, searchResponse.Controls.Length);
                        Assert.True(searchResponse.Controls[0] is PageResultResponseControl);

                        PageResultResponseControl pageResponse = (PageResultResponseControl)searchResponse.Controls[0];

                        if (pageResponse.Cookie.Length == 0)
                            break;

                        pageRequest.Cookie = pageResponse.Cookie;
                    }
                }
                finally
                {
                    for (int i=0; i<20; i++)
                    {
                        DeleteEntry(state.Connection, "ou=ProtocolsSubGroup8." + i + "," + dn, state.SearchDn);
                    }
                    DeleteEntry(state.Connection, dn, state.SearchDn);
                }
            }
        }

        [ConditionalFact(nameof(IsServerSideSortSupported))]
        public void TestSortedSearch()
        {
            using (ConnectionState state = Connect())
            {
                string ouName = "ProtocolsGroup10";
                string dn = "ou=" + ouName;

                try
                {
                    for (int i=0; i<10; i++)
                    {
                        DeleteEntry(state.Connection, "ou=ProtocolsSubGroup10." + i + "," + dn, state.SearchDn);
                    }
                    DeleteEntry(state.Connection, dn, state.SearchDn);

                    AddOrganizationalUnit(state.Connection, dn, state.SearchDn);
                    SearchResultEntry sre = SearchOrganizationalUnit(state.Connection, state.SearchDn, ouName);
                    Assert.NotNull(sre);

                    for (int i=0; i<10; i++)
                    {
                        AddOrganizationalUnit(state.Connection, "ou=ProtocolsSubGroup10." + i + "," + dn, state.SearchDn);
                    }

                    string filter = "(objectClass=*)";
                    SearchRequest searchRequest = new SearchRequest(
                                                        dn + "," + state.SearchDn,
                                                        filter,
                                                        SearchScope.Subtree,
                                                        null);

                    SortRequestControl sortRequestControl = new SortRequestControl("ou", true);
                    searchRequest.Controls.Add(sortRequestControl);

                    SearchResponse searchResponse = (SearchResponse)state.Connection.SendRequest(searchRequest);
                    Assert.Equal(1, searchResponse.Controls.Length);
                    Assert.True(searchResponse.Controls[0] is SortResponseControl);
                    Assert.True(searchResponse.Entries.Count > 0);
                    Assert.Equal("ou=ProtocolsSubGroup10.9," + dn + "," + state.SearchDn, searchResponse.Entries[0].DistinguishedName, true);
                }
                finally
                {
                    for (int i=0; i<20; i++)
                    {
                        DeleteEntry(state.Connection, "ou=ProtocolsSubGroup10." + i + "," + dn, state.SearchDn);
                    }
                    DeleteEntry(state.Connection, dn, state.SearchDn);
                }
            }
        }

        [Fact]
        public void TestMultipleServerBind()
        {
            using (ConnectionState state = ConnectUnboundWithServerSpecifiedTwice())
            {
                LdapConnection connection = state.Connection;

                // Set server protocol before bind; OpenLDAP servers default
                // to LDAP v2, which we do not support, and will return LDAP_PROTOCOL_ERROR
                connection.SessionOptions.ProtocolVersion = 3;
                connection.Bind();
                connection.Timeout = new TimeSpan(0, 3, 0);
            }
        }

        private static void DeleteAttribute(LdapConnection connection, string entryDn, string attributeName, string searchDn)
        {
            string dn = entryDn + "," + searchDn;
            ModifyRequest modifyRequest = new ModifyRequest(dn, DirectoryAttributeOperation.Delete, attributeName);
            ModifyResponse modifyResponse = (ModifyResponse) connection.SendRequest(modifyRequest);
            Assert.Equal(ResultCode.Success, modifyResponse.ResultCode);
        }

        private static void ModifyAttribute(LdapConnection connection, string entryDn, string attributeName, string attributeValue, string searchDn)
        {
            string dn = entryDn + "," + searchDn;
            ModifyRequest modifyRequest = new ModifyRequest(dn, DirectoryAttributeOperation.Replace, attributeName, attributeValue);
            ModifyResponse modifyResponse = (ModifyResponse) connection.SendRequest(modifyRequest);
            Assert.Equal(ResultCode.Success, modifyResponse.ResultCode);
        }

        private static void AddAttribute(LdapConnection connection, string entryDn, string attributeName, string attributeValue, string searchDn)
        {
            string dn = entryDn + "," + searchDn;
            ModifyRequest modifyRequest = new ModifyRequest(dn, DirectoryAttributeOperation.Add, attributeName, attributeValue);
            ModifyResponse modifyResponse = (ModifyResponse) connection.SendRequest(modifyRequest);
            Assert.Equal(ResultCode.Success, modifyResponse.ResultCode);
        }

        private static void AddOrganizationalUnit(LdapConnection connection, string entryDn, string searchDn)
        {
            string dn = entryDn + "," + searchDn;
            AddRequest addRequest = new AddRequest(dn, "organizationalUnit");
            AddResponse addResponse = (AddResponse) connection.SendRequest(addRequest);
            Assert.Equal(ResultCode.Success, addResponse.ResultCode);
        }

        private static void AddOrganizationalRole(LdapConnection connection, string entryDn, string searchDn)
        {
            string dn = entryDn + "," + searchDn;
            AddRequest addRequest = new AddRequest(dn, "organizationalRole");
            AddResponse addResponse = (AddResponse) connection.SendRequest(addRequest);
            Assert.Equal(ResultCode.Success, addResponse.ResultCode);
        }

        private static void DeleteEntry(LdapConnection connection, string entryDn, string searchDn)
        {
            try
            {
                string dn = entryDn + "," + searchDn;
                DeleteRequest delRequest = new DeleteRequest(dn);
                DeleteResponse delResponse = (DeleteResponse) connection.SendRequest(delRequest);
                Assert.Equal(ResultCode.Success, delResponse.ResultCode);
            }
            catch
            {
                // ignore the exception as we use this for clean up
            }
        }

        private static SearchResultEntry SearchOrganizationalUnit(LdapConnection connection, string rootDn, string ouName)
        {
            string filter = $"(&(objectClass=organizationalUnit)(ou={ouName}))";
            SearchRequest searchRequest = new SearchRequest(rootDn, filter, SearchScope.OneLevel, null);
            IAsyncResult asyncResult = connection.BeginSendRequest(searchRequest, PartialResultProcessing.NoPartialResultSupport, null, null);
            SearchResponse searchResponse = (SearchResponse)connection.EndSendRequest(asyncResult);

            if (searchResponse.Entries.Count > 0)
                return searchResponse.Entries[0];

            return null;
        }

        private static SearchResultEntry SearchUser(LdapConnection connection, string rootDn, string userName)
        {
            string filter = $"(&(objectClass=organizationalRole)(cn={userName}))";
            SearchRequest searchRequest = new SearchRequest(rootDn, filter, SearchScope.OneLevel, null);
            SearchResponse searchResponse = (SearchResponse) connection.SendRequest(searchRequest);

            if (searchResponse.Entries.Count > 0)
                return searchResponse.Entries[0];

            return null;
        }

        protected static LdapConnection GetConnection(bool bind = true)
        {
            LdapDirectoryIdentifier directoryIdentifier = string.IsNullOrEmpty(LdapConfiguration.Configuration.Port) ?
                                        new LdapDirectoryIdentifier(LdapConfiguration.Configuration.ServerName, fullyQualifiedDnsHostName: true, connectionless: false) :
                                        new LdapDirectoryIdentifier(LdapConfiguration.Configuration.ServerName,
                                                                    int.Parse(LdapConfiguration.Configuration.Port, NumberStyles.None, CultureInfo.InvariantCulture),
                                                                    fullyQualifiedDnsHostName: true, connectionless: false);
            return GetConnection(directoryIdentifier, bind);
        }

        protected static LdapConnection GetConnection(LdapDirectoryIdentifier directoryIdentifier, bool bind = true)
        {
            NetworkCredential credential = new NetworkCredential(LdapConfiguration.Configuration.UserName, LdapConfiguration.Configuration.Password);

            LdapConnection connection = new LdapConnection(directoryIdentifier, credential)
            {
                AuthType = AuthType.Basic
            };

            // Set server protocol before bind; OpenLDAP servers default
            // to LDAP v2, which we do not support, and will return LDAP_PROTOCOL_ERROR
            connection.SessionOptions.ProtocolVersion = 3;
            connection.SessionOptions.SecureSocketLayer = LdapConfiguration.Configuration.UseTls;

            if (bind)
            {
                connection.Bind();
            }

            connection.Timeout = new TimeSpan(0, 3, 0);
            return connection;
        }
    }

    internal class ASyncOperationState
    {
        internal ASyncOperationState(LdapConnection connection)
        {
            Connection = connection;
        }

        internal LdapConnection Connection { get; set; }
        internal Exception Exception { get; set; }
    }

    public partial class DirectoryServicesProtocolsTests_Local
    {
        private static LdapTestServer StartLocalServer(out int port)
        {
            var server = new LdapTestServer();
            port = server.Start();

            return server;
        }

        private static LdapTestServer StartLocalServerWithStartTLS(X509Certificate2 certificate, out int port)
        {
            var server = new LdapTestServer();
            port = server.Start(certificate);

            return server;
        }

        private static LdapTestServer StartLocalServerWithLdaps(X509Certificate2 certificate, out int port)
        {
            var server = new LdapTestServer();
            port = server.StartLdaps(certificate);

            return server;
        }

        private static LdapConnection GetLocalConnection(int port)
        {
            LdapConnection connection = new LdapConnection(
                new LdapDirectoryIdentifier("localhost", port, fullyQualifiedDnsHostName: false, connectionless: false),
                new NetworkCredential("cn=admin", "PLACEHOLDER"))
            {
                AuthType = AuthType.Basic
            };

            connection.SessionOptions.ProtocolVersion = 3;
            connection.Bind();
            connection.Timeout = TimeSpan.FromSeconds(30);

            return connection;
        }

        private static LdapConnection CreateLocalLdapsConnection(int port, VerifyServerCertificateCallback verifyServerCertificate)
        {
            LdapConnection connection = new LdapConnection(
                new LdapDirectoryIdentifier("localhost", port, fullyQualifiedDnsHostName: false, connectionless: false),
                new NetworkCredential("cn=admin", "PLACEHOLDER"))
            {
                AuthType = AuthType.Basic
            };

            connection.SessionOptions.ProtocolVersion = 3;
            connection.SessionOptions.SecureSocketLayer = true;
            connection.SessionOptions.VerifyServerCertificate = verifyServerCertificate;

            return connection;
        }

        private static X509Certificate2 CreateServerCertificate()
        {
            using (RSA key = RSA.Create(2048))
            {
                CertificateRequest request = new CertificateRequest(
                    "CN=localhost",
                    key,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                request.CertificateExtensions.Add(
                    new X509KeyUsageExtension(
                        X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                        critical: true));

                request.CertificateExtensions.Add(
                    new X509EnhancedKeyUsageExtension(
                        new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") },
                        critical: false));

                SubjectAlternativeNameBuilder sanBuilder = new SubjectAlternativeNameBuilder();
                sanBuilder.AddDnsName("localhost");
                sanBuilder.AddIpAddress(System.Net.IPAddress.Loopback);
                request.CertificateExtensions.Add(sanBuilder.Build());

                DateTimeOffset now = DateTimeOffset.UtcNow;
                X509Certificate2 cert = request.CreateSelfSigned(now.AddMinutes(-5), now.AddHours(1));

                // On Windows, SslStream requires the cert to come from a PFX
                // to have an accessible private key.
                if (PlatformDetection.IsWindows)
                {
                    using (cert)
                    {
                        byte[] pfxBytes = cert.Export(X509ContentType.Pfx);
                        return X509CertificateLoader.LoadPkcs12(pfxBytes, null);
                    }
                }

                return cert;
            }
        }

    }
}
