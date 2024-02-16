// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices.Tests;
using System.Globalization;
using System.Net;
using System.Text;
using System.Threading;
using Xunit;

namespace System.DirectoryServices.Protocols.Tests
{
    public partial class DirectoryServicesProtocolsTests
    {
        internal static bool LdapConfigurationExists => LdapConfiguration.Configuration != null;
        internal static bool IsActiveDirectoryServer => LdapConfigurationExists && LdapConfiguration.Configuration.IsActiveDirectoryServer;

        internal static bool IsServerSideSortSupported => LdapConfigurationExists && LdapConfiguration.Configuration.SupportsServerSideSort;

        [ConditionalFact(nameof(LdapConfigurationExists))]
        public void TestInvalidFilter()
        {
            using LdapConnection connection = GetConnection();

            LdapException ex = Assert.Throws<LdapException>(() =>
            {
                var searchRequest = new SearchRequest(LdapConfiguration.Configuration.SearchDn, "==invalid==", SearchScope.OneLevel);
                _ = (SearchResponse) connection.SendRequest(searchRequest);
            });

            Assert.Equal(/* LdapError.FilterError */ 0x57, ex.ErrorCode);
        }

        [ConditionalFact(nameof(LdapConfigurationExists))]
        public void TestInvalidSearchDn()
        {
            using LdapConnection connection = GetConnection();

            DirectoryOperationException ex = Assert.Throws<DirectoryOperationException>(() =>
            {
                var searchRequest = new SearchRequest("==invaliddn==", "(objectClass=*)", SearchScope.OneLevel);
                var searchResponse = (SearchResponse) connection.SendRequest(searchRequest);
            });

            Assert.Equal(ResultCode.InvalidDNSyntax, ex.Response.ResultCode);
        }

        [ConditionalFact(nameof(LdapConfigurationExists))]
        public void TestUnavailableCriticalExtension()
        {
            using LdapConnection connection = GetConnection();

            DirectoryOperationException ex = Assert.Throws<DirectoryOperationException>(() =>
            {
                var searchRequest = new SearchRequest(LdapConfiguration.Configuration.SearchDn, "(objectClass=*)", SearchScope.OneLevel);
                var control = new DirectoryControl("==invalid-control==", value: null, isCritical: true, serverSide: true);
                searchRequest.Controls.Add(control);
                _ = (SearchResponse) connection.SendRequest(searchRequest);
            });

            Assert.Equal(ResultCode.UnavailableCriticalExtension, ex.Response.ResultCode);
        }

        [ConditionalFact(nameof(LdapConfigurationExists))]
        public void TestUnavailableNonCriticalExtension()
        {
            using LdapConnection connection = GetConnection();

            var searchRequest = new SearchRequest(LdapConfiguration.Configuration.SearchDn, "(objectClass=*)", SearchScope.OneLevel);
            var control = new DirectoryControl("==invalid-control==", value: null, isCritical: false, serverSide: true);
            searchRequest.Controls.Add(control);
            _ = (SearchResponse) connection.SendRequest(searchRequest);
            // Does not throw
        }

        [ConditionalFact(nameof(LdapConfigurationExists))]
        public void TestServerWithPortNumber()
        {
            using LdapConnection connection = GetConnection($"{LdapConfiguration.Configuration.ServerName}:{LdapConfiguration.Configuration.Port}");

            var searchRequest = new SearchRequest(LdapConfiguration.Configuration.SearchDn, "(objectClass=*)", SearchScope.Subtree);

            _ = (SearchResponse)connection.SendRequest(searchRequest);
            // Shall succeed
        }

        [InlineData(60)]
        [InlineData(0)]
        [InlineData(-60)]
        [ConditionalTheory(nameof(LdapConfigurationExists))]
        public void TestSearchWithTimeLimit(int timeLimit)
        {
            using LdapConnection connection = GetConnection();

            var searchRequest = new SearchRequest(LdapConfiguration.Configuration.SearchDn, "(objectClass=*)", SearchScope.Subtree);
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

        [ConditionalFact(nameof(LdapConfigurationExists))]
        public void TestAddingOU()
        {
            using (LdapConnection connection = GetConnection())
            {
                string ouName = "ProtocolsGroup1";
                string dn = "ou=" + ouName;

                try
                {
                    DeleteEntry(connection, dn);
                    AddOrganizationalUnit(connection, dn);
                    SearchResultEntry sre = SearchOrganizationalUnit(connection, LdapConfiguration.Configuration.SearchDn, ouName);
                    Assert.NotNull(sre);
                }
                finally
                {
                    DeleteEntry(connection, dn);
                }
            }
        }

        [ConditionalFact(nameof(LdapConfigurationExists))]
        public void TestDeleteOU()
        {
            using (LdapConnection connection = GetConnection())
            {
                string ouName = "ProtocolsGroup2";
                string dn = "ou=" + ouName;
                try
                {
                    DeleteEntry(connection, dn);
                    AddOrganizationalUnit(connection, dn);
                    SearchResultEntry sre = SearchOrganizationalUnit(connection, LdapConfiguration.Configuration.SearchDn, ouName);
                    Assert.NotNull(sre);

                    DeleteEntry(connection, dn);
                    sre = SearchOrganizationalUnit(connection, LdapConfiguration.Configuration.SearchDn, ouName);
                    Assert.Null(sre);
                }
                finally
                {
                    DeleteEntry(connection, dn);
                }
            }
        }

        [ConditionalFact(nameof(LdapConfigurationExists))]
        public void TestAddAndModifyAttribute()
        {
            using (LdapConnection connection = GetConnection())
            {
                string ouName = "ProtocolsGroup3";
                string dn = "ou=" + ouName;
                try
                {
                    DeleteEntry(connection, dn);
                    AddOrganizationalUnit(connection, dn);

                    AddAttribute(connection, dn, "description", "Protocols Group 3");
                    SearchResultEntry sre = SearchOrganizationalUnit(connection, LdapConfiguration.Configuration.SearchDn, ouName);
                    Assert.NotNull(sre);
                    Assert.Equal("Protocols Group 3", (string) sre.Attributes["description"][0]);
                    Assert.Throws<DirectoryOperationException>(() => AddAttribute(connection, dn, "description", "Protocols Group 3"));

                    ModifyAttribute(connection, dn, "description", "Modified Protocols Group 3");
                    sre = SearchOrganizationalUnit(connection, LdapConfiguration.Configuration.SearchDn, ouName);
                    Assert.NotNull(sre);
                    Assert.Equal("Modified Protocols Group 3", (string) sre.Attributes["description"][0]);

                    DeleteAttribute(connection, dn, "description");
                    sre = SearchOrganizationalUnit(connection, LdapConfiguration.Configuration.SearchDn, ouName);
                    Assert.NotNull(sre);
                    Assert.Null(sre.Attributes["description"]);
                }
                finally
                {
                    DeleteEntry(connection, dn);
                }
            }
        }

        [ConditionalFact(nameof(LdapConfigurationExists))]
        public void TestNestedOUs()
        {
            using (LdapConnection connection = GetConnection())
            {
                string ouLevel1Name = "ProtocolsGroup4-1";
                string dnLevel1 = "ou=" + ouLevel1Name;
                string ouLevel2Name = "ProtocolsGroup4-2";
                string dnLevel2 = "ou=" + ouLevel2Name+ "," + dnLevel1;

                DeleteEntry(connection, dnLevel2);
                DeleteEntry(connection, dnLevel1);

                try
                {
                    AddOrganizationalUnit(connection, dnLevel1);
                    SearchResultEntry sre = SearchOrganizationalUnit(connection, LdapConfiguration.Configuration.SearchDn, ouLevel1Name);
                    Assert.NotNull(sre);

                    AddOrganizationalUnit(connection, dnLevel2);
                    sre = SearchOrganizationalUnit(connection, dnLevel1 + "," + LdapConfiguration.Configuration.SearchDn, ouLevel2Name);
                    Assert.NotNull(sre);
                }
                finally
                {
                    DeleteEntry(connection, dnLevel2);
                    DeleteEntry(connection, dnLevel1);
                }
            }
        }

        [ConditionalFact(nameof(LdapConfigurationExists))]
        public void TestAddUser()
        {
            using (LdapConnection connection = GetConnection())
            {
                string ouName = "ProtocolsGroup5";
                string dn = "ou=" + ouName;
                string user1Dn = "cn=protocolUser1" + "," + dn;
                string user2Dn = "cn=protocolUser2" + "," + dn;

                DeleteEntry(connection, user1Dn);
                DeleteEntry(connection, user2Dn);
                DeleteEntry(connection, dn);

                try
                {
                    AddOrganizationalUnit(connection, dn);
                    SearchResultEntry sre = SearchOrganizationalUnit(connection, LdapConfiguration.Configuration.SearchDn, ouName);
                    Assert.NotNull(sre);

                    AddOrganizationalRole(connection, user1Dn);
                    AddOrganizationalRole(connection, user2Dn);

                    string usersRoot = dn + "," + LdapConfiguration.Configuration.SearchDn;

                    sre = SearchUser(connection, usersRoot, "protocolUser1");
                    Assert.NotNull(sre);

                    sre = SearchUser(connection, usersRoot, "protocolUser2");
                    Assert.NotNull(sre);

                    DeleteEntry(connection, user1Dn);
                    sre = SearchUser(connection, usersRoot, "protocolUser1");
                    Assert.Null(sre);

                    DeleteEntry(connection, user2Dn);
                    sre = SearchUser(connection, usersRoot, "protocolUser2");
                    Assert.Null(sre);

                    DeleteEntry(connection, dn);
                    sre = SearchOrganizationalUnit(connection, LdapConfiguration.Configuration.SearchDn, ouName);
                    Assert.Null(sre);
                }
                finally
                {
                    DeleteEntry(connection, user1Dn);
                    DeleteEntry(connection, user2Dn);
                    DeleteEntry(connection, dn);
                }
            }
        }

        [ConditionalFact(nameof(LdapConfigurationExists))]
        public void TestAddingMultipleAttributes()
        {
            using (LdapConnection connection = GetConnection())
            {
                string ouName = "ProtocolsGroup6";
                string dn = "ou=" + ouName;
                try
                {
                    DeleteEntry(connection, dn);
                    AddOrganizationalUnit(connection, dn);

                    DirectoryAttributeModification mod1 = new DirectoryAttributeModification();
                    mod1.Operation = DirectoryAttributeOperation.Add;
                    mod1.Name = "description";
                    mod1.Add("Description 5");

                    DirectoryAttributeModification mod2 = new DirectoryAttributeModification();
                    mod2.Operation = DirectoryAttributeOperation.Add;
                    mod2.Name = "postalAddress";
                    mod2.Add("123 4th Ave NE, State, Country");

                    DirectoryAttributeModification[] mods = new DirectoryAttributeModification[2] { mod1, mod2 };

                    string fullDn = dn + "," + LdapConfiguration.Configuration.SearchDn;

                    ModifyRequest modRequest = new ModifyRequest(fullDn, mods);
                    ModifyResponse modResponse = (ModifyResponse) connection.SendRequest(modRequest);
                    Assert.Equal(ResultCode.Success, modResponse.ResultCode);
                    Assert.Throws<DirectoryOperationException>(() => (ModifyResponse) connection.SendRequest(modRequest));

                    SearchResultEntry sre = SearchOrganizationalUnit(connection, LdapConfiguration.Configuration.SearchDn, ouName);
                    Assert.NotNull(sre);
                    Assert.Equal("Description 5", (string) sre.Attributes["description"][0]);
                    Assert.Throws<DirectoryOperationException>(() => AddAttribute(connection, dn, "description", "Description 5"));
                    Assert.Equal("123 4th Ave NE, State, Country", (string) sre.Attributes["postalAddress"][0]);
                    Assert.Throws<DirectoryOperationException>(() => AddAttribute(connection, dn, "postalAddress", "123 4th Ave NE, State, Country"));

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

                    sre = SearchOrganizationalUnit(connection, LdapConfiguration.Configuration.SearchDn, ouName);
                    Assert.NotNull(sre);
                    Assert.Equal("Modified Description 5", (string) sre.Attributes["description"][0]);
                    Assert.Throws<DirectoryOperationException>(() => AddAttribute(connection, dn, "description", "Modified Description 5"));
                    Assert.Equal("689 5th Ave NE, State, Country", (string) sre.Attributes["postalAddress"][0]);
                    Assert.Throws<DirectoryOperationException>(() => AddAttribute(connection, dn, "postalAddress", "689 5th Ave NE, State, Country"));

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

                    sre = SearchOrganizationalUnit(connection, LdapConfiguration.Configuration.SearchDn, ouName);
                    Assert.NotNull(sre);
                    Assert.Null(sre.Attributes["description"]);
                    Assert.Null(sre.Attributes["postalAddress"]);
                }
                finally
                {
                    DeleteEntry(connection, dn);
                }
            }
        }

        [ConditionalFact(nameof(LdapConfigurationExists))]
        public void TestMoveAndRenameUser()
        {
            using (LdapConnection connection = GetConnection())
            {
                string ouName1 = "ProtocolsGroup7.1";
                string dn1 = "ou=" + ouName1;

                string ouName2 = "ProtocolsGroup7.2";
                string dn2 = "ou=" + ouName2;

                string userDn1 = "cn=protocolUser7.1" + "," + dn1;
                string userDn2 = "cn=protocolUser7.2" + "," + dn2;

                DeleteEntry(connection, userDn1);
                DeleteEntry(connection, userDn2);
                DeleteEntry(connection, dn1);
                DeleteEntry(connection, dn2);

                try
                {
                    AddOrganizationalUnit(connection, dn1);
                    SearchResultEntry sre = SearchOrganizationalUnit(connection, LdapConfiguration.Configuration.SearchDn, ouName1);
                    Assert.NotNull(sre);

                    AddOrganizationalUnit(connection, dn2);
                    sre = SearchOrganizationalUnit(connection, LdapConfiguration.Configuration.SearchDn, ouName2);
                    Assert.NotNull(sre);

                    AddOrganizationalRole(connection, userDn1);

                    string user1Root = dn1 + "," + LdapConfiguration.Configuration.SearchDn;
                    string user2Root = dn2 + "," + LdapConfiguration.Configuration.SearchDn;

                    sre = SearchUser(connection, user1Root, "protocolUser7.1");
                    Assert.NotNull(sre);

                    ModifyDNRequest modDnRequest = new ModifyDNRequest( userDn1 + "," + LdapConfiguration.Configuration.SearchDn,
                                                                        dn2 + "," + LdapConfiguration.Configuration.SearchDn,
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
                    DeleteEntry(connection, userDn1);
                    DeleteEntry(connection, userDn2);
                    DeleteEntry(connection, dn1);
                    DeleteEntry(connection, dn2);
                }
            }
        }

        [ConditionalFact(nameof(LdapConfigurationExists))]
        public void TestAsyncSearch()
        {
            using (LdapConnection connection = GetConnection())
            {
                string ouName = "ProtocolsGroup9";
                string dn = "ou=" + ouName;

                try
                {
                    for (int i=0; i<20; i++)
                    {
                        DeleteEntry(connection, "ou=ProtocolsSubGroup9." + i + "," + dn);
                    }
                    DeleteEntry(connection, dn);

                    AddOrganizationalUnit(connection, dn);
                    SearchResultEntry sre = SearchOrganizationalUnit(connection, LdapConfiguration.Configuration.SearchDn, ouName);
                    Assert.NotNull(sre);

                    for (int i=0; i<20; i++)
                    {
                        AddOrganizationalUnit(connection, "ou=ProtocolsSubGroup9." + i + "," + dn);
                    }

                    string filter = "(objectClass=organizationalUnit)";
                    SearchRequest searchRequest = new SearchRequest(
                                                            dn + "," + LdapConfiguration.Configuration.SearchDn,
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
                        DeleteEntry(connection, "ou=ProtocolsSubGroup9." + i + "," + dn);
                    }
                    DeleteEntry(connection, dn);
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

        [ConditionalTheory(nameof(LdapConfigurationExists))]
        [MemberData(nameof(TestCompareRequestTheory_TestData))]
        public void TestCompareRequestTheory(object value, object assertion, ResultCode compareResult)
        {
            using (LdapConnection connection = GetConnection())
            {
                string ouName = "ProtocolsGroup10";
                string rdn = "ou=" + ouName;

                DeleteEntry(connection, rdn);
                AddOrganizationalUnit(connection, rdn);

                string dn = rdn + "," + LdapConfiguration.Configuration.SearchDn;

                // set description to value
                var mod = new ModifyRequest(dn, DirectoryAttributeOperation.Replace, "description", value);
                var response = connection.SendRequest(mod);
                Assert.Equal(ResultCode.Success, response.ResultCode);

                // compare description to assertion
                var cmp = new CompareRequest(dn, new DirectoryAttribute("description", assertion));
                response = connection.SendRequest(cmp);
                // assert compare result
                Assert.Equal(compareResult, response.ResultCode);

                // compare description to value
                cmp = new CompareRequest(dn, new DirectoryAttribute("description", value));
                response = connection.SendRequest(cmp);
                // compare result always true
                Assert.Equal(ResultCode.CompareTrue, response.ResultCode);
            }
        }

        [ConditionalFact(nameof(LdapConfigurationExists))]
        public void TestCompareRequest()
        {
            using (LdapConnection connection = GetConnection())
            {
                // negative case: ou=NotFound does not exist
                var cmp = new CompareRequest("ou=NotFound," + LdapConfiguration.Configuration.SearchDn, "ou", "NotFound");
                Assert.Throws<DirectoryOperationException>(() => connection.SendRequest(cmp));
            }
        }

        [ConditionalFact(nameof(IsActiveDirectoryServer))]
        public void TestPageRequests()
        {
            using (LdapConnection connection = GetConnection())
            {
                string ouName = "ProtocolsGroup8";
                string dn = "ou=" + ouName;

                try
                {
                    for (int i=0; i<20; i++)
                    {
                        DeleteEntry(connection, "ou=ProtocolsSubGroup8." + i + "," + dn);
                    }
                    DeleteEntry(connection, dn);

                    AddOrganizationalUnit(connection, dn);
                    SearchResultEntry sre = SearchOrganizationalUnit(connection, LdapConfiguration.Configuration.SearchDn, ouName);
                    Assert.NotNull(sre);

                    for (int i=0; i<20; i++)
                    {
                        AddOrganizationalUnit(connection, "ou=ProtocolsSubGroup8." + i + "," + dn);
                    }

                    string filter = "(objectClass=*)";
                    SearchRequest searchRequest = new SearchRequest(
                                                        dn + "," + LdapConfiguration.Configuration.SearchDn,
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
                        DeleteEntry(connection, "ou=ProtocolsSubGroup8." + i + "," + dn);
                    }
                    DeleteEntry(connection, dn);
                }
            }
        }

        [ConditionalFact(nameof(IsServerSideSortSupported))]
        public void TestSortedSearch()
        {
            using (LdapConnection connection = GetConnection())
            {
                string ouName = "ProtocolsGroup10";
                string dn = "ou=" + ouName;

                try
                {
                    for (int i=0; i<10; i++)
                    {
                        DeleteEntry(connection, "ou=ProtocolsSubGroup10." + i + "," + dn);
                    }
                    DeleteEntry(connection, dn);

                    AddOrganizationalUnit(connection, dn);
                    SearchResultEntry sre = SearchOrganizationalUnit(connection, LdapConfiguration.Configuration.SearchDn, ouName);
                    Assert.NotNull(sre);

                    for (int i=0; i<10; i++)
                    {
                        AddOrganizationalUnit(connection, "ou=ProtocolsSubGroup10." + i + "," + dn);
                    }

                    string filter = "(objectClass=*)";
                    SearchRequest searchRequest = new SearchRequest(
                                                        dn + "," + LdapConfiguration.Configuration.SearchDn,
                                                        filter,
                                                        SearchScope.Subtree,
                                                        null);

                    var sortRequestControl = new SortRequestControl("ou", true);
                    searchRequest.Controls.Add(sortRequestControl);

                    SearchResponse searchResponse = (SearchResponse) connection.SendRequest(searchRequest);
                    Assert.Equal(1, searchResponse.Controls.Length);
                    Assert.True(searchResponse.Controls[0] is SortResponseControl);
                    Assert.True(searchResponse.Entries.Count > 0);
                    Assert.Equal("ou=ProtocolsSubGroup10.9," + dn + "," + LdapConfiguration.Configuration.SearchDn, searchResponse.Entries[0].DistinguishedName);
                }
                finally
                {
                    for (int i=0; i<20; i++)
                    {
                        DeleteEntry(connection, "ou=ProtocolsSubGroup10." + i + "," + dn);
                    }
                    DeleteEntry(connection, dn);
                }
            }
        }

        [ConditionalFact(nameof(LdapConfigurationExists))]
        public void TestMultipleServerBind()
        {
            LdapDirectoryIdentifier directoryIdentifier = string.IsNullOrEmpty(LdapConfiguration.Configuration.Port) ?
                                        new LdapDirectoryIdentifier(new string[] { LdapConfiguration.Configuration.ServerName, LdapConfiguration.Configuration.ServerName }, true, false) :
                                        new LdapDirectoryIdentifier(new string[] { LdapConfiguration.Configuration.ServerName, LdapConfiguration.Configuration.ServerName },
                                                                    int.Parse(LdapConfiguration.Configuration.Port, NumberStyles.None, CultureInfo.InvariantCulture),
                                                                    true, false);
            NetworkCredential credential = new NetworkCredential(LdapConfiguration.Configuration.UserName, LdapConfiguration.Configuration.Password);

            using LdapConnection connection = new LdapConnection(directoryIdentifier, credential)
            {
                AuthType = AuthType.Basic
            };

            // Set server protocol before bind; OpenLDAP servers default
            // to LDAP v2, which we do not support, and will return LDAP_PROTOCOL_ERROR
            connection.SessionOptions.ProtocolVersion = 3;
            connection.SessionOptions.SecureSocketLayer = LdapConfiguration.Configuration.UseTls;
            connection.Bind();

            connection.Timeout = new TimeSpan(0, 3, 0);
        }

        private void DeleteAttribute(LdapConnection connection, string entryDn, string attributeName)
        {
            string dn = entryDn + "," + LdapConfiguration.Configuration.SearchDn;
            ModifyRequest modifyRequest = new ModifyRequest(dn, DirectoryAttributeOperation.Delete, attributeName);
            ModifyResponse modifyResponse = (ModifyResponse) connection.SendRequest(modifyRequest);
            Assert.Equal(ResultCode.Success, modifyResponse.ResultCode);
        }

        private void ModifyAttribute(LdapConnection connection, string entryDn, string attributeName, string attributeValue)
        {
            string dn = entryDn + "," + LdapConfiguration.Configuration.SearchDn;
            ModifyRequest modifyRequest = new ModifyRequest(dn, DirectoryAttributeOperation.Replace, attributeName, attributeValue);
            ModifyResponse modifyResponse = (ModifyResponse) connection.SendRequest(modifyRequest);
            Assert.Equal(ResultCode.Success, modifyResponse.ResultCode);
        }

        private void AddAttribute(LdapConnection connection, string entryDn, string attributeName, string attributeValue)
        {
            string dn = entryDn + "," + LdapConfiguration.Configuration.SearchDn;
            ModifyRequest modifyRequest = new ModifyRequest(dn, DirectoryAttributeOperation.Add, attributeName, attributeValue);
            ModifyResponse modifyResponse = (ModifyResponse) connection.SendRequest(modifyRequest);
            Assert.Equal(ResultCode.Success, modifyResponse.ResultCode);
        }

        private void AddOrganizationalUnit(LdapConnection connection, string entryDn)
        {
            string dn = entryDn + "," + LdapConfiguration.Configuration.SearchDn;
            AddRequest addRequest = new AddRequest(dn, "organizationalUnit");
            AddResponse addResponse = (AddResponse) connection.SendRequest(addRequest);
            Assert.Equal(ResultCode.Success, addResponse.ResultCode);
        }

        private void AddOrganizationalRole(LdapConnection connection, string entryDn)
        {
            string dn = entryDn + "," + LdapConfiguration.Configuration.SearchDn;
            AddRequest addRequest = new AddRequest(dn, "organizationalRole");
            AddResponse addResponse = (AddResponse) connection.SendRequest(addRequest);
            Assert.Equal(ResultCode.Success, addResponse.ResultCode);
        }

        private void DeleteEntry(LdapConnection connection, string entryDn)
        {
            try
            {
                string dn = entryDn + "," + LdapConfiguration.Configuration.SearchDn;
                DeleteRequest delRequest = new DeleteRequest(dn);
                DeleteResponse delResponse = (DeleteResponse) connection.SendRequest(delRequest);
                Assert.Equal(ResultCode.Success, delResponse.ResultCode);
            }
            catch
            {
                // ignore the exception as we use this for clean up
            }
        }

        private SearchResultEntry SearchOrganizationalUnit(LdapConnection connection, string rootDn, string ouName)
        {
            string filter = $"(&(objectClass=organizationalUnit)(ou={ouName}))";
            SearchRequest searchRequest = new SearchRequest(rootDn, filter, SearchScope.OneLevel, null);
            IAsyncResult asyncResult = connection.BeginSendRequest(searchRequest, PartialResultProcessing.NoPartialResultSupport, null, null);
            SearchResponse searchResponse = (SearchResponse)connection.EndSendRequest(asyncResult);

            if (searchResponse.Entries.Count > 0)
                return searchResponse.Entries[0];

            return null;
        }

        private SearchResultEntry SearchUser(LdapConnection connection, string rootDn, string userName)
        {
            string filter = $"(&(objectClass=organizationalRole)(cn={userName}))";
            SearchRequest searchRequest = new SearchRequest(rootDn, filter, SearchScope.OneLevel, null);
            SearchResponse searchResponse = (SearchResponse) connection.SendRequest(searchRequest);

            if (searchResponse.Entries.Count > 0)
                return searchResponse.Entries[0];

            return null;
        }

        private LdapConnection GetConnection(string server)
        {
            LdapDirectoryIdentifier directoryIdentifier = new LdapDirectoryIdentifier(server, fullyQualifiedDnsHostName: true, connectionless: false);

            return GetConnection(directoryIdentifier);
        }

        private LdapConnection GetConnection()
        {
            LdapDirectoryIdentifier directoryIdentifier = string.IsNullOrEmpty(LdapConfiguration.Configuration.Port) ?
                                        new LdapDirectoryIdentifier(LdapConfiguration.Configuration.ServerName, fullyQualifiedDnsHostName: true, connectionless: false) :
                                        new LdapDirectoryIdentifier(LdapConfiguration.Configuration.ServerName,
                                                                    int.Parse(LdapConfiguration.Configuration.Port, NumberStyles.None, CultureInfo.InvariantCulture),
                                                                    fullyQualifiedDnsHostName: true, connectionless: false);
            return GetConnection(directoryIdentifier);
        }

        private static LdapConnection GetConnection(LdapDirectoryIdentifier directoryIdentifier)
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
            connection.Bind();

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
