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
    public partial class DirectoryServicesProtocolsTests
    {
        internal static bool LdapConfigurationExists => LdapConfiguration.Configuration != null;
        internal static bool IsActiveDirectoryServer => LdapConfigurationExists && LdapConfiguration.Configuration.IsActiveDirectoryServer;
        internal static bool UseTls => LdapConfigurationExists && LdapConfiguration.Configuration.UseTls;

        internal static bool IsServerSideSortSupported => LdapConfigurationExists && LdapConfiguration.Configuration.SupportsServerSideSort;

        [ConditionalFact(typeof(DirectoryServicesProtocolsTests), nameof(LdapConfigurationExists))]
        public void TestInvalidFilter()
        {
            using (LdapConnection connection = GetConnection())
            {
                TestInvalidFilterCore(connection, LdapConfiguration.Configuration.SearchDn);
            }
        }

        [ConditionalFact(typeof(DirectoryServicesTestHelpers), nameof(DirectoryServicesTestHelpers.IsWindowsOrLibLdapIsInstalled))]
        public void TestInvalidFilter_Local()
        {
            using (LdapTestServer server = StartLocalServer(out int port))
            using (LdapConnection connection = GetLocalConnection(port))
            {
                TestInvalidFilterCore(connection, server.BaseDn);
            }
        }

        private static void TestInvalidFilterCore(LdapConnection connection, string searchDn)
        {
            LdapException ex = Assert.Throws<LdapException>(() =>
            {
                var searchRequest = new SearchRequest(searchDn, "==invalid==", SearchScope.OneLevel);
                _ = (SearchResponse) connection.SendRequest(searchRequest);
            });

            Assert.Equal(/* LdapError.FilterError */ 0x57, ex.ErrorCode);
        }

        [ConditionalFact(typeof(DirectoryServicesProtocolsTests), nameof(LdapConfigurationExists))]
        public void TestInvalidSearchDn()
        {
            using (LdapConnection connection = GetConnection())
            {
                TestInvalidSearchDnCore(connection);
            }
        }

        [ConditionalFact(typeof(DirectoryServicesTestHelpers), nameof(DirectoryServicesTestHelpers.IsWindowsOrLibLdapIsInstalled))]
        public void TestInvalidSearchDn_Local()
        {
            using (LdapTestServer server = StartLocalServer(out int port))
            using (LdapConnection connection = GetLocalConnection(port))
            {
                TestInvalidSearchDnCore(connection);
            }
        }

        private static void TestInvalidSearchDnCore(LdapConnection connection)
        {
            DirectoryOperationException ex = Assert.Throws<DirectoryOperationException>(() =>
            {
                var searchRequest = new SearchRequest("==invaliddn==", "(objectClass=*)", SearchScope.OneLevel);
                var searchResponse = (SearchResponse) connection.SendRequest(searchRequest);
            });

            Assert.Equal(ResultCode.InvalidDNSyntax, ex.Response.ResultCode);
        }

        [ConditionalFact(typeof(DirectoryServicesProtocolsTests), nameof(LdapConfigurationExists))]
        public void TestUnavailableCriticalExtension()
        {
            using (LdapConnection connection = GetConnection())
            {
                TestUnavailableCriticalExtensionCore(connection, LdapConfiguration.Configuration.SearchDn);
            }
        }

        [ConditionalFact(typeof(DirectoryServicesTestHelpers), nameof(DirectoryServicesTestHelpers.IsWindowsOrLibLdapIsInstalled))]
        public void TestUnavailableCriticalExtension_Local()
        {
            using (LdapTestServer server = StartLocalServer(out int port))
            using (LdapConnection connection = GetLocalConnection(port))
            {
                TestUnavailableCriticalExtensionCore(connection, server.BaseDn);
            }
        }

        private static void TestUnavailableCriticalExtensionCore(LdapConnection connection, string searchDn)
        {
            DirectoryOperationException ex = Assert.Throws<DirectoryOperationException>(() =>
            {
                var searchRequest = new SearchRequest(searchDn, "(objectClass=*)", SearchScope.OneLevel);
                var control = new DirectoryControl("==invalid-control==", value: null, isCritical: true, serverSide: true);
                searchRequest.Controls.Add(control);
                _ = (SearchResponse) connection.SendRequest(searchRequest);
            });

            Assert.Equal(ResultCode.UnavailableCriticalExtension, ex.Response.ResultCode);
        }

        [ConditionalFact(typeof(DirectoryServicesProtocolsTests), nameof(LdapConfigurationExists))]
        public void TestUnavailableNonCriticalExtension()
        {
            using (LdapConnection connection = GetConnection())
            {
                TestUnavailableNonCriticalExtensionCore(connection, LdapConfiguration.Configuration.SearchDn);
            }
        }

        [ConditionalFact(typeof(DirectoryServicesTestHelpers), nameof(DirectoryServicesTestHelpers.IsWindowsOrLibLdapIsInstalled))]
        public void TestUnavailableNonCriticalExtension_Local()
        {
            using (LdapTestServer server = StartLocalServer(out int port))
            using (LdapConnection connection = GetLocalConnection(port))
            {
                TestUnavailableNonCriticalExtensionCore(connection, server.BaseDn);
            }
        }

        private static void TestUnavailableNonCriticalExtensionCore(LdapConnection connection, string searchDn)
        {
            var searchRequest = new SearchRequest(searchDn, "(objectClass=*)", SearchScope.OneLevel);
            var control = new DirectoryControl("==invalid-control==", value: null, isCritical: false, serverSide: true);
            searchRequest.Controls.Add(control);
            _ = (SearchResponse) connection.SendRequest(searchRequest);
            // Does not throw
        }

        [ConditionalFact(typeof(DirectoryServicesProtocolsTests), nameof(LdapConfigurationExists))]
        public void TestServerWithPortNumber()
        {
            using (LdapConnection connection = GetConnection($"{LdapConfiguration.Configuration.ServerName}:{LdapConfiguration.Configuration.Port}"))
            {
                TestServerWithPortNumberCore(connection, LdapConfiguration.Configuration.SearchDn);
            }
        }

        [ConditionalFact(typeof(DirectoryServicesTestHelpers), nameof(DirectoryServicesTestHelpers.IsWindowsOrLibLdapIsInstalled))]
        public void TestServerWithPortNumber_Local()
        {
            using (LdapTestServer server = StartLocalServer(out int port))
            {
                LdapDirectoryIdentifier identifier = new LdapDirectoryIdentifier($"localhost:{port}", fullyQualifiedDnsHostName: false, connectionless: false);
                using (LdapConnection connection = new LdapConnection(identifier, new NetworkCredential("cn=admin", "PLACEHOLDER")) { AuthType = AuthType.Basic })
                {
                    connection.SessionOptions.ProtocolVersion = 3;
                    connection.Bind();

                    TestServerWithPortNumberCore(connection, server.BaseDn);
                }
            }
        }

        private static void TestServerWithPortNumberCore(LdapConnection connection, string searchDn)
        {
            var searchRequest = new SearchRequest(searchDn, "(objectClass=*)", SearchScope.Subtree);
            _ = (SearchResponse)connection.SendRequest(searchRequest);
            // Shall succeed
        }

        [InlineData(60)]
        [InlineData(0)]
        [InlineData(-60)]
        [ConditionalTheory(typeof(DirectoryServicesProtocolsTests), nameof(LdapConfigurationExists))]
        public void TestSearchWithTimeLimit(int timeLimit)
        {
            using (LdapConnection connection = GetConnection())
            {
                TestSearchWithTimeLimitCore(connection, LdapConfiguration.Configuration.SearchDn, timeLimit);
            }
        }

        [InlineData(60)]
        [InlineData(0)]
        [InlineData(-60)]
        [ConditionalTheory(typeof(DirectoryServicesTestHelpers), nameof(DirectoryServicesTestHelpers.IsWindowsOrLibLdapIsInstalled))]
        public void TestSearchWithTimeLimit_Local(int timeLimit)
        {
            using (LdapTestServer server = StartLocalServer(out int port))
            using (LdapConnection connection = GetLocalConnection(port))
            {
                TestSearchWithTimeLimitCore(connection, server.BaseDn, timeLimit);
            }
        }

        private static void TestSearchWithTimeLimitCore(LdapConnection connection, string searchDn, int timeLimit)
        {
            var searchRequest = new SearchRequest(searchDn, "(objectClass=*)", SearchScope.Subtree);
            if (timeLimit < 0)
            {
                Assert.Throws<ArgumentException>(() => searchRequest.TimeLimit = TimeSpan.FromSeconds(timeLimit));
            }
            else
            {
                searchRequest.TimeLimit = TimeSpan.FromSeconds(timeLimit);
                _ = (SearchResponse)connection.SendRequest(searchRequest);
                // Shall succeed
            }
        }

        [ConditionalFact(typeof(DirectoryServicesProtocolsTests), nameof(LdapConfigurationExists))]
        public void TestAddingOU()
        {
            using (LdapConnection connection = GetConnection())
            {
                TestAddingOUCore(connection, LdapConfiguration.Configuration.SearchDn);
            }
        }

        [ConditionalFact(typeof(DirectoryServicesTestHelpers), nameof(DirectoryServicesTestHelpers.IsWindowsOrLibLdapIsInstalled))]
        public void TestAddingOU_Local()
        {
            using (LdapTestServer server = StartLocalServer(out int port))
            using (LdapConnection connection = GetLocalConnection(port))
            {
                TestAddingOUCore(connection, server.BaseDn);
            }
        }

        private static void TestAddingOUCore(LdapConnection connection, string searchDn)
        {
            string ouName = "ProtocolsGroup1";
            string dn = "ou=" + ouName;

            try
            {
                DeleteEntry(connection, dn, searchDn);
                AddOrganizationalUnit(connection, dn, searchDn);
                SearchResultEntry sre = SearchOrganizationalUnit(connection, searchDn, ouName);
                Assert.NotNull(sre);
            }
            finally
            {
                DeleteEntry(connection, dn, searchDn);
            }
        }

        [ConditionalFact(typeof(DirectoryServicesProtocolsTests), nameof(LdapConfigurationExists))]
        public void TestDeleteOU()
        {
            using (LdapConnection connection = GetConnection())
            {
                TestDeleteOUCore(connection, LdapConfiguration.Configuration.SearchDn);
            }
        }

        [ConditionalFact(typeof(DirectoryServicesTestHelpers), nameof(DirectoryServicesTestHelpers.IsWindowsOrLibLdapIsInstalled))]
        public void TestDeleteOU_Local()
        {
            using (LdapTestServer server = StartLocalServer(out int port))
            using (LdapConnection connection = GetLocalConnection(port))
            {
                TestDeleteOUCore(connection, server.BaseDn);
            }
        }

        private static void TestDeleteOUCore(LdapConnection connection, string searchDn)
        {
            string ouName = "ProtocolsGroup2";
            string dn = "ou=" + ouName;
            try
            {
                DeleteEntry(connection, dn, searchDn);
                AddOrganizationalUnit(connection, dn, searchDn);
                SearchResultEntry sre = SearchOrganizationalUnit(connection, searchDn, ouName);
                Assert.NotNull(sre);

                DeleteEntry(connection, dn, searchDn);
                sre = SearchOrganizationalUnit(connection, searchDn, ouName);
                Assert.Null(sre);
            }
            finally
            {
                DeleteEntry(connection, dn, searchDn);
            }
        }

        [ConditionalFact(typeof(DirectoryServicesProtocolsTests), nameof(LdapConfigurationExists))]
        public void TestAddAndModifyAttribute()
        {
            using (LdapConnection connection = GetConnection())
            {
                TestAddAndModifyAttributeCore(connection, LdapConfiguration.Configuration.SearchDn);
            }
        }

        [ConditionalFact(typeof(DirectoryServicesTestHelpers), nameof(DirectoryServicesTestHelpers.IsWindowsOrLibLdapIsInstalled))]
        public void TestAddAndModifyAttribute_Local()
        {
            using (LdapTestServer server = StartLocalServer(out int port))
            using (LdapConnection connection = GetLocalConnection(port))
            {
                TestAddAndModifyAttributeCore(connection, server.BaseDn);
            }
        }

        private static void TestAddAndModifyAttributeCore(LdapConnection connection, string searchDn)
        {
            string ouName = "ProtocolsGroup3";
            string dn = "ou=" + ouName;
            try
            {
                DeleteEntry(connection, dn, searchDn);
                AddOrganizationalUnit(connection, dn, searchDn);

                AddAttribute(connection, dn, "description", "Protocols Group 3", searchDn);
                SearchResultEntry sre = SearchOrganizationalUnit(connection, searchDn, ouName);
                Assert.NotNull(sre);
                Assert.Equal("Protocols Group 3", (string) sre.Attributes["description"][0]);
                Assert.Throws<DirectoryOperationException>(() => AddAttribute(connection, dn, "description", "Protocols Group 3", searchDn));

                ModifyAttribute(connection, dn, "description", "Modified Protocols Group 3", searchDn);
                sre = SearchOrganizationalUnit(connection, searchDn, ouName);
                Assert.NotNull(sre);
                Assert.Equal("Modified Protocols Group 3", (string) sre.Attributes["description"][0]);

                DeleteAttribute(connection, dn, "description", searchDn);
                sre = SearchOrganizationalUnit(connection, searchDn, ouName);
                Assert.NotNull(sre);
                Assert.Null(sre.Attributes["description"]);
            }
            finally
            {
                DeleteEntry(connection, dn, searchDn);
            }
        }

        [ConditionalFact(typeof(DirectoryServicesProtocolsTests), nameof(LdapConfigurationExists))]
        public void TestNestedOUs()
        {
            using (LdapConnection connection = GetConnection())
            {
                TestNestedOUsCore(connection, LdapConfiguration.Configuration.SearchDn);
            }
        }

        [ConditionalFact(typeof(DirectoryServicesTestHelpers), nameof(DirectoryServicesTestHelpers.IsWindowsOrLibLdapIsInstalled))]
        public void TestNestedOUs_Local()
        {
            using (LdapTestServer server = StartLocalServer(out int port))
            using (LdapConnection connection = GetLocalConnection(port))
            {
                TestNestedOUsCore(connection, server.BaseDn);
            }
        }

        private static void TestNestedOUsCore(LdapConnection connection, string searchDn)
        {
            string ouLevel1Name = "ProtocolsGroup4-1";
            string dnLevel1 = "ou=" + ouLevel1Name;
            string ouLevel2Name = "ProtocolsGroup4-2";
            string dnLevel2 = "ou=" + ouLevel2Name+ "," + dnLevel1;

            DeleteEntry(connection, dnLevel2, searchDn);
            DeleteEntry(connection, dnLevel1, searchDn);

            try
            {
                AddOrganizationalUnit(connection, dnLevel1, searchDn);
                SearchResultEntry sre = SearchOrganizationalUnit(connection, searchDn, ouLevel1Name);
                Assert.NotNull(sre);

                AddOrganizationalUnit(connection, dnLevel2, searchDn);
                sre = SearchOrganizationalUnit(connection, dnLevel1 + "," + searchDn, ouLevel2Name);
                Assert.NotNull(sre);
            }
            finally
            {
                DeleteEntry(connection, dnLevel2, searchDn);
                DeleteEntry(connection, dnLevel1, searchDn);
            }
        }

        [ConditionalFact(typeof(DirectoryServicesProtocolsTests), nameof(LdapConfigurationExists))]
        public void TestAddUser()
        {
            using (LdapConnection connection = GetConnection())
            {
                TestAddUserCore(connection, LdapConfiguration.Configuration.SearchDn);
            }
        }

        [ConditionalFact(typeof(DirectoryServicesTestHelpers), nameof(DirectoryServicesTestHelpers.IsWindowsOrLibLdapIsInstalled))]
        public void TestAddUser_Local()
        {
            using (LdapTestServer server = StartLocalServer(out int port))
            using (LdapConnection connection = GetLocalConnection(port))
            {
                TestAddUserCore(connection, server.BaseDn);
            }
        }

        private static void TestAddUserCore(LdapConnection connection, string searchDn)
        {
            string ouName = "ProtocolsGroup5";
            string dn = "ou=" + ouName;
            string user1Dn = "cn=protocolUser1" + "," + dn;
            string user2Dn = "cn=protocolUser2" + "," + dn;

            DeleteEntry(connection, user1Dn, searchDn);
            DeleteEntry(connection, user2Dn, searchDn);
            DeleteEntry(connection, dn, searchDn);

            try
            {
                AddOrganizationalUnit(connection, dn, searchDn);
                SearchResultEntry sre = SearchOrganizationalUnit(connection, searchDn, ouName);
                Assert.NotNull(sre);

                AddOrganizationalRole(connection, user1Dn, searchDn);
                AddOrganizationalRole(connection, user2Dn, searchDn);

                string usersRoot = dn + "," + searchDn;

                sre = SearchUser(connection, usersRoot, "protocolUser1");
                Assert.NotNull(sre);

                sre = SearchUser(connection, usersRoot, "protocolUser2");
                Assert.NotNull(sre);

                DeleteEntry(connection, user1Dn, searchDn);
                sre = SearchUser(connection, usersRoot, "protocolUser1");
                Assert.Null(sre);

                DeleteEntry(connection, user2Dn, searchDn);
                sre = SearchUser(connection, usersRoot, "protocolUser2");
                Assert.Null(sre);

                DeleteEntry(connection, dn, searchDn);
                sre = SearchOrganizationalUnit(connection, searchDn, ouName);
                Assert.Null(sre);
            }
            finally
            {
                DeleteEntry(connection, user1Dn, searchDn);
                DeleteEntry(connection, user2Dn, searchDn);
                DeleteEntry(connection, dn, searchDn);
            }
        }

        [ConditionalFact(typeof(DirectoryServicesProtocolsTests), nameof(LdapConfigurationExists))]
        public void TestAddingMultipleAttributes()
        {
            using (LdapConnection connection = GetConnection())
            {
                TestAddingMultipleAttributesCore(connection, LdapConfiguration.Configuration.SearchDn);
            }
        }

        [ConditionalFact(typeof(DirectoryServicesTestHelpers), nameof(DirectoryServicesTestHelpers.IsWindowsOrLibLdapIsInstalled))]
        public void TestAddingMultipleAttributes_Local()
        {
            using (LdapTestServer server = StartLocalServer(out int port))
            using (LdapConnection connection = GetLocalConnection(port))
            {
                TestAddingMultipleAttributesCore(connection, server.BaseDn);
            }
        }

        private static void TestAddingMultipleAttributesCore(LdapConnection connection, string searchDn)
        {
            string ouName = "ProtocolsGroup6";
            string dn = "ou=" + ouName;
            try
            {
                DeleteEntry(connection, dn, searchDn);
                AddOrganizationalUnit(connection, dn, searchDn);

                DirectoryAttributeModification mod1 = new DirectoryAttributeModification();
                mod1.Operation = DirectoryAttributeOperation.Add;
                mod1.Name = "description";
                mod1.Add("Description 5");

                DirectoryAttributeModification mod2 = new DirectoryAttributeModification();
                mod2.Operation = DirectoryAttributeOperation.Add;
                mod2.Name = "postalAddress";
                mod2.Add("123 4th Ave NE, State, Country");

                DirectoryAttributeModification[] mods = new DirectoryAttributeModification[2] { mod1, mod2 };

                string fullDn = dn + "," + searchDn;

                ModifyRequest modRequest = new ModifyRequest(fullDn, mods);
                ModifyResponse modResponse = (ModifyResponse) connection.SendRequest(modRequest);
                Assert.Equal(ResultCode.Success, modResponse.ResultCode);
                Assert.Throws<DirectoryOperationException>(() => (ModifyResponse) connection.SendRequest(modRequest));

                SearchResultEntry sre = SearchOrganizationalUnit(connection, searchDn, ouName);
                Assert.NotNull(sre);
                Assert.Equal("Description 5", (string) sre.Attributes["description"][0]);
                Assert.Throws<DirectoryOperationException>(() => AddAttribute(connection, dn, "description", "Description 5", searchDn));
                Assert.Equal("123 4th Ave NE, State, Country", (string) sre.Attributes["postalAddress"][0]);
                Assert.Throws<DirectoryOperationException>(() => AddAttribute(connection, dn, "postalAddress", "123 4th Ave NE, State, Country", searchDn));

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
                modResponse = (ModifyResponse) connection.SendRequest(modRequest);
                Assert.Equal(ResultCode.Success, modResponse.ResultCode);

                sre = SearchOrganizationalUnit(connection, searchDn, ouName);
                Assert.NotNull(sre);
                Assert.Equal("Modified Description 5", (string) sre.Attributes["description"][0]);
                Assert.Throws<DirectoryOperationException>(() => AddAttribute(connection, dn, "description", "Modified Description 5", searchDn));
                Assert.Equal("689 5th Ave NE, State, Country", (string) sre.Attributes["postalAddress"][0]);
                Assert.Throws<DirectoryOperationException>(() => AddAttribute(connection, dn, "postalAddress", "689 5th Ave NE, State, Country", searchDn));

                mod1 = new DirectoryAttributeModification();
                mod1.Operation = DirectoryAttributeOperation.Delete;
                mod1.Name = "description";

                mod2 = new DirectoryAttributeModification();
                mod2.Operation = DirectoryAttributeOperation.Delete;
                mod2.Name = "postalAddress";
                mods = new DirectoryAttributeModification[2] { mod1, mod2 };
                modRequest = new ModifyRequest(fullDn, mods);
                modResponse = (ModifyResponse) connection.SendRequest(modRequest);
                Assert.Equal(ResultCode.Success, modResponse.ResultCode);

                sre = SearchOrganizationalUnit(connection, searchDn, ouName);
                Assert.NotNull(sre);
                Assert.Null(sre.Attributes["description"]);
                Assert.Null(sre.Attributes["postalAddress"]);
            }
            finally
            {
                DeleteEntry(connection, dn, searchDn);
            }
        }

        [ConditionalFact(typeof(DirectoryServicesProtocolsTests), nameof(LdapConfigurationExists))]
        public void TestMoveAndRenameUser()
        {
            using (LdapConnection connection = GetConnection())
            {
                TestMoveAndRenameUserCore(connection, LdapConfiguration.Configuration.SearchDn);
            }
        }

        [ConditionalFact(typeof(DirectoryServicesTestHelpers), nameof(DirectoryServicesTestHelpers.IsWindowsOrLibLdapIsInstalled))]
        public void TestMoveAndRenameUser_Local()
        {
            using (LdapTestServer server = StartLocalServer(out int port))
            using (LdapConnection connection = GetLocalConnection(port))
            {
                TestMoveAndRenameUserCore(connection, server.BaseDn);
            }
        }

        private static void TestMoveAndRenameUserCore(LdapConnection connection, string searchDn)
        {
            string ouName1 = "ProtocolsGroup7.1";
            string dn1 = "ou=" + ouName1;

            string ouName2 = "ProtocolsGroup7.2";
            string dn2 = "ou=" + ouName2;

            string userDn1 = "cn=protocolUser7.1" + "," + dn1;
            string userDn2 = "cn=protocolUser7.2" + "," + dn2;

            DeleteEntry(connection, userDn1, searchDn);
            DeleteEntry(connection, userDn2, searchDn);
            DeleteEntry(connection, dn1, searchDn);
            DeleteEntry(connection, dn2, searchDn);

            try
            {
                AddOrganizationalUnit(connection, dn1, searchDn);
                SearchResultEntry sre = SearchOrganizationalUnit(connection, searchDn, ouName1);
                Assert.NotNull(sre);

                AddOrganizationalUnit(connection, dn2, searchDn);
                sre = SearchOrganizationalUnit(connection, searchDn, ouName2);
                Assert.NotNull(sre);

                AddOrganizationalRole(connection, userDn1, searchDn);

                string user1Root = dn1 + "," + searchDn;
                string user2Root = dn2 + "," + searchDn;

                sre = SearchUser(connection, user1Root, "protocolUser7.1");
                Assert.NotNull(sre);

                ModifyDNRequest modDnRequest = new ModifyDNRequest( userDn1 + "," + searchDn,
                                                                    dn2 + "," + searchDn,
                                                                    "cn=protocolUser7.2");
                ModifyDNResponse modDnResponse = (ModifyDNResponse) connection.SendRequest(modDnRequest);
                Assert.Equal(ResultCode.Success, modDnResponse.ResultCode);

                sre = SearchUser(connection, user1Root, "protocolUser7.1");
                Assert.Null(sre);

                sre = SearchUser(connection, user2Root, "protocolUser7.2");
                Assert.NotNull(sre);
            }
            finally
            {
                DeleteEntry(connection, userDn1, searchDn);
                DeleteEntry(connection, userDn2, searchDn);
                DeleteEntry(connection, dn1, searchDn);
                DeleteEntry(connection, dn2, searchDn);
            }
        }

        [ConditionalFact(typeof(DirectoryServicesProtocolsTests), nameof(LdapConfigurationExists))]
        public void TestAsyncSearch()
        {
            using (LdapConnection connection = GetConnection())
            {
                TestAsyncSearchCore(connection, LdapConfiguration.Configuration.SearchDn);
            }
        }

        [ConditionalFact(typeof(DirectoryServicesTestHelpers), nameof(DirectoryServicesTestHelpers.IsWindowsOrLibLdapIsInstalled))]
        public void TestAsyncSearch_Local()
        {
            using (LdapTestServer server = StartLocalServer(out int port))
            using (LdapConnection connection = GetLocalConnection(port))
            {
                TestAsyncSearchCore(connection, server.BaseDn);
            }
        }

        private static void TestAsyncSearchCore(LdapConnection connection, string searchDn)
        {
            string ouName = "ProtocolsGroup9";
            string dn = "ou=" + ouName;

            try
            {
                for (int i=0; i<20; i++)
                {
                    DeleteEntry(connection, "ou=ProtocolsSubGroup9." + i + "," + dn, searchDn);
                }
                DeleteEntry(connection, dn, searchDn);

                AddOrganizationalUnit(connection, dn, searchDn);
                SearchResultEntry sre = SearchOrganizationalUnit(connection, searchDn, ouName);
                Assert.NotNull(sre);

                for (int i=0; i<20; i++)
                {
                    AddOrganizationalUnit(connection, "ou=ProtocolsSubGroup9." + i + "," + dn, searchDn);
                }

                string filter = "(objectClass=organizationalUnit)";
                SearchRequest searchRequest = new SearchRequest(
                                                        dn + "," + searchDn,
                                                        filter,
                                                        SearchScope.OneLevel,
                                                        null);

                ASyncOperationState state = new ASyncOperationState(connection);
                IAsyncResult asyncResult = connection.BeginSendRequest(
                                                searchRequest,
                                                PartialResultProcessing.ReturnPartialResultsAndNotifyCallback,
                                                RunAsyncSearch,
                                                state);

                asyncResult.AsyncWaitHandle.WaitOne();
                Assert.True(state.Exception == null, state.Exception == null ? "" : state.Exception.ToString());
            }
            finally
            {
                for (int i=0; i<20; i++)
                {
                    DeleteEntry(connection, "ou=ProtocolsSubGroup9." + i + "," + dn, searchDn);
                }
                DeleteEntry(connection, dn, searchDn);
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

        [ConditionalTheory(typeof(DirectoryServicesProtocolsTests), nameof(LdapConfigurationExists))]
        [MemberData(nameof(TestCompareRequestTheory_TestData))]
        public void TestCompareRequestTheory(object value, object assertion, ResultCode compareResult)
        {
            using (LdapConnection connection = GetConnection())
            {
                TestCompareRequestTheoryCore(connection, LdapConfiguration.Configuration.SearchDn, value, assertion, compareResult);
            }
        }

        [ConditionalTheory(typeof(DirectoryServicesTestHelpers), nameof(DirectoryServicesTestHelpers.IsWindowsOrLibLdapIsInstalled))]
        [MemberData(nameof(TestCompareRequestTheory_TestData))]
        public void TestCompareRequestTheory_Local(object value, object assertion, ResultCode compareResult)
        {
            using (LdapTestServer server = StartLocalServer(out int port))
            using (LdapConnection connection = GetLocalConnection(port))
            {
                TestCompareRequestTheoryCore(connection, server.BaseDn, value, assertion, compareResult);
            }
        }

        private static void TestCompareRequestTheoryCore(LdapConnection connection, string searchDn, object value, object assertion, ResultCode compareResult)
        {
            string ouName = "ProtocolsGroup10";
            string rdn = "ou=" + ouName;

            DeleteEntry(connection, rdn, searchDn);
            AddOrganizationalUnit(connection, rdn, searchDn);

            string dn = rdn + "," + searchDn;

            // set description to value
            ModifyRequest mod = new ModifyRequest(dn, DirectoryAttributeOperation.Replace, "description", value);
            DirectoryResponse response = connection.SendRequest(mod);
            Assert.Equal(ResultCode.Success, response.ResultCode);

            // compare description to assertion
            CompareRequest cmp = new CompareRequest(dn, new DirectoryAttribute("description", assertion));
            response = connection.SendRequest(cmp);
            // assert compare result
            Assert.Equal(compareResult, response.ResultCode);

            // compare description to value
            cmp = new CompareRequest(dn, new DirectoryAttribute("description", value));
            response = connection.SendRequest(cmp);
            // compare result always true
            Assert.Equal(ResultCode.CompareTrue, response.ResultCode);
        }

        [ConditionalFact(typeof(DirectoryServicesProtocolsTests), nameof(LdapConfigurationExists))]
        public void TestCompareRequest()
        {
            using (LdapConnection connection = GetConnection())
            {
                TestCompareRequestCore(connection, LdapConfiguration.Configuration.SearchDn);
            }
        }

        [ConditionalFact(typeof(DirectoryServicesTestHelpers), nameof(DirectoryServicesTestHelpers.IsWindowsOrLibLdapIsInstalled))]
        public void TestCompareRequest_Local()
        {
            using (LdapTestServer server = StartLocalServer(out int port))
            using (LdapConnection connection = GetLocalConnection(port))
            {
                TestCompareRequestCore(connection, server.BaseDn);
            }
        }

        private static void TestCompareRequestCore(LdapConnection connection, string searchDn)
        {
            // negative case: ou=NotFound does not exist
            CompareRequest cmp = new CompareRequest("ou=NotFound," + searchDn, "ou", "NotFound");
            Assert.Throws<DirectoryOperationException>(() => connection.SendRequest(cmp));
        }

        [ConditionalFact(typeof(DirectoryServicesProtocolsTests), nameof(IsActiveDirectoryServer))]
        public void TestPageRequests()
        {
            using (LdapConnection connection = GetConnection())
            {
                TestPageRequestsCore(connection, LdapConfiguration.Configuration.SearchDn);
            }
        }

        [ConditionalFact(typeof(DirectoryServicesTestHelpers), nameof(DirectoryServicesTestHelpers.IsWindowsOrLibLdapIsInstalled))]
        public void TestPageRequests_Local()
        {
            using (LdapTestServer server = StartLocalServer(out int port))
            using (LdapConnection connection = GetLocalConnection(port))
            {
                TestPageRequestsCore(connection, server.BaseDn);
            }
        }

        private static void TestPageRequestsCore(LdapConnection connection, string searchDn)
        {
            string ouName = "ProtocolsGroup8";
            string dn = "ou=" + ouName;

            try
            {
                for (int i=0; i<20; i++)
                {
                    DeleteEntry(connection, "ou=ProtocolsSubGroup8." + i + "," + dn, searchDn);
                }
                DeleteEntry(connection, dn, searchDn);

                AddOrganizationalUnit(connection, dn, searchDn);
                SearchResultEntry sre = SearchOrganizationalUnit(connection, searchDn, ouName);
                Assert.NotNull(sre);

                for (int i=0; i<20; i++)
                {
                    AddOrganizationalUnit(connection, "ou=ProtocolsSubGroup8." + i + "," + dn, searchDn);
                }

                string filter = "(objectClass=*)";
                SearchRequest searchRequest = new SearchRequest(
                                                    dn + "," + searchDn,
                                                    filter,
                                                    SearchScope.Subtree,
                                                    null);

                PageResultRequestControl pageRequest = new PageResultRequestControl(5);
                searchRequest.Controls.Add(pageRequest);
                SearchOptionsControl searchOptions = new SearchOptionsControl(SearchOption.DomainScope);
                searchRequest.Controls.Add(searchOptions);
                while (true)
                {
                    SearchResponse searchResponse = (SearchResponse) connection.SendRequest(searchRequest);
                    Assert.Equal(1, searchResponse.Controls.Length);
                    Assert.True(searchResponse.Controls[0] is PageResultResponseControl);

                    PageResultResponseControl pageResponse = (PageResultResponseControl) searchResponse.Controls[0];

                    if (pageResponse.Cookie.Length == 0)
                        break;

                    pageRequest.Cookie = pageResponse.Cookie;
                }
            }
            finally
            {
                for (int i=0; i<20; i++)
                {
                    DeleteEntry(connection, "ou=ProtocolsSubGroup8." + i + "," + dn, searchDn);
                }
                DeleteEntry(connection, dn, searchDn);
            }
        }

        [ConditionalFact(typeof(DirectoryServicesProtocolsTests), nameof(IsServerSideSortSupported))]
        public void TestSortedSearch()
        {
            using (LdapConnection connection = GetConnection())
            {
                TestSortedSearchCore(connection, LdapConfiguration.Configuration.SearchDn);
            }
        }

        [ConditionalFact(typeof(DirectoryServicesTestHelpers), nameof(DirectoryServicesTestHelpers.IsWindowsOrLibLdapIsInstalled))]
        public void TestSortedSearch_Local()
        {
            using (LdapTestServer server = StartLocalServer(out int port))
            using (LdapConnection connection = GetLocalConnection(port))
            {
                TestSortedSearchCore(connection, server.BaseDn);
            }
        }

        private static void TestSortedSearchCore(LdapConnection connection, string searchDn)
        {
            string ouName = "ProtocolsGroup10";
            string dn = "ou=" + ouName;

            try
            {
                for (int i=0; i<10; i++)
                {
                    DeleteEntry(connection, "ou=ProtocolsSubGroup10." + i + "," + dn, searchDn);
                }
                DeleteEntry(connection, dn, searchDn);

                AddOrganizationalUnit(connection, dn, searchDn);
                SearchResultEntry sre = SearchOrganizationalUnit(connection, searchDn, ouName);
                Assert.NotNull(sre);

                for (int i=0; i<10; i++)
                {
                    AddOrganizationalUnit(connection, "ou=ProtocolsSubGroup10." + i + "," + dn, searchDn);
                }

                string filter = "(objectClass=*)";
                SearchRequest searchRequest = new SearchRequest(
                                                    dn + "," + searchDn,
                                                    filter,
                                                    SearchScope.Subtree,
                                                    null);

                SortRequestControl sortRequestControl = new SortRequestControl("ou", true);
                searchRequest.Controls.Add(sortRequestControl);

                SearchResponse searchResponse = (SearchResponse) connection.SendRequest(searchRequest);
                Assert.Equal(1, searchResponse.Controls.Length);
                Assert.True(searchResponse.Controls[0] is SortResponseControl);
                Assert.True(searchResponse.Entries.Count > 0);
                Assert.Equal("ou=ProtocolsSubGroup10.9," + dn + "," + searchDn, searchResponse.Entries[0].DistinguishedName, true);
            }
            finally
            {
                for (int i=0; i<20; i++)
                {
                    DeleteEntry(connection, "ou=ProtocolsSubGroup10." + i + "," + dn, searchDn);
                }
                DeleteEntry(connection, dn, searchDn);
            }
        }

        [ConditionalFact(typeof(DirectoryServicesProtocolsTests), nameof(LdapConfigurationExists))]
        public void TestMultipleServerBind()
        {
            LdapDirectoryIdentifier directoryIdentifier = string.IsNullOrEmpty(LdapConfiguration.Configuration.Port) ?
                                        new LdapDirectoryIdentifier(new string[] { LdapConfiguration.Configuration.ServerName, LdapConfiguration.Configuration.ServerName }, true, false) :
                                        new LdapDirectoryIdentifier(new string[] { LdapConfiguration.Configuration.ServerName, LdapConfiguration.Configuration.ServerName },
                                                                    int.Parse(LdapConfiguration.Configuration.Port, NumberStyles.None, CultureInfo.InvariantCulture),
                                                                    true, false);
            NetworkCredential credential = new NetworkCredential(LdapConfiguration.Configuration.UserName, LdapConfiguration.Configuration.Password);

            using (LdapConnection connection = new LdapConnection(directoryIdentifier, credential) { AuthType = AuthType.Basic })
            {
                connection.SessionOptions.SecureSocketLayer = LdapConfiguration.Configuration.UseTls;
                TestMultipleServerBindCore(connection);
            }
        }

        [ConditionalFact(typeof(DirectoryServicesTestHelpers), nameof(DirectoryServicesTestHelpers.IsWindowsOrLibLdapIsInstalled))]
        public void TestMultipleServerBind_Local()
        {
            using (LdapTestServer server = StartLocalServer(out int port))
            {
                LdapDirectoryIdentifier directoryIdentifier = new LdapDirectoryIdentifier(
                    new string[] { "localhost", "localhost" }, port, false, false);
                NetworkCredential credential = new NetworkCredential("cn=admin", "PLACEHOLDER");

                using (LdapConnection connection = new LdapConnection(directoryIdentifier, credential) { AuthType = AuthType.Basic })
                {
                    TestMultipleServerBindCore(connection);
                }
            }
        }

        private static void TestMultipleServerBindCore(LdapConnection connection)
        {
            // Set server protocol before bind; OpenLDAP servers default
            // to LDAP v2, which we do not support, and will return LDAP_PROTOCOL_ERROR
            connection.SessionOptions.ProtocolVersion = 3;
            connection.Bind();
            connection.Timeout = new TimeSpan(0, 3, 0);
        }

        [InlineData(true)]
        [InlineData(false)]
        [ConditionalTheory(typeof(DirectoryServicesTestHelpers), nameof(DirectoryServicesTestHelpers.IsWindowsOrLibLdapIsInstalled))]
        public void TestVerifyServerCertificateCallback_LDAPS_Local(bool acceptCertificate)
        {
            using (X509Certificate2 cert = CreateServerCertificate())
            using (LdapTestServer server = StartLocalServerWithLdaps(cert, out int port))
            using (LdapConnection connection = CreateLocalLdapsConnection(port, PrepareCallback(cert, acceptCertificate)))
            {
                if (acceptCertificate)
                {
                    connection.Bind();

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
        [ConditionalTheory(typeof(DirectoryServicesTestHelpers), nameof(DirectoryServicesTestHelpers.IsWindowsOrLibLdapIsInstalled))]
        public void TestVerifyServerCertificateCallback_StartTLS_Local(bool acceptCertificate)
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

                SearchRequest searchRequest = new (LdapConfiguration.Configuration.SearchDn, "(objectClass=*)", SearchScope.Subtree);
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

        private static LdapConnection GetConnection(string server)
        {
            LdapDirectoryIdentifier directoryIdentifier = new LdapDirectoryIdentifier(server, fullyQualifiedDnsHostName: true, connectionless: false);

            return GetConnection(directoryIdentifier);
        }

        private static LdapConnection GetConnection(bool bind = true)
        {
            LdapDirectoryIdentifier directoryIdentifier = string.IsNullOrEmpty(LdapConfiguration.Configuration.Port) ?
                                        new LdapDirectoryIdentifier(LdapConfiguration.Configuration.ServerName, fullyQualifiedDnsHostName: true, connectionless: false) :
                                        new LdapDirectoryIdentifier(LdapConfiguration.Configuration.ServerName,
                                                                    int.Parse(LdapConfiguration.Configuration.Port, NumberStyles.None, CultureInfo.InvariantCulture),
                                                                    fullyQualifiedDnsHostName: true, connectionless: false);
            return GetConnection(directoryIdentifier, bind);
        }

        private static LdapConnection GetConnection(LdapDirectoryIdentifier directoryIdentifier, bool bind = true)
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
}
