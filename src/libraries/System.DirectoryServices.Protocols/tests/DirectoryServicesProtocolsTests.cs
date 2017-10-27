// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Collections;
using System.Globalization;
using System.Net;
using Xunit;
using System.Threading;
using System.DirectoryServices.Tests;
using System.DirectoryServices.Protocols;

namespace System.DirectoryServicesProtocols.Tests
{
    public partial class DirectoryServicesProtocolsTests
    {
        internal static bool IsLdapConfigurationExist => LdapConfiguration.Configuration != null;
        internal static bool IsActiveDirectoryServer => IsLdapConfigurationExist && LdapConfiguration.Configuration.IsActiveDirectoryServer;

        [ConditionalFact(nameof(IsLdapConfigurationExist))]
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
                    SearchResultEntry sre = SearchOrganizationalUnit(connection, LdapConfiguration.Configuration.Domain, ouName);
                    Assert.NotNull(sre);
                }
                finally
                {
                    DeleteEntry(connection, dn);
                }
            }
        }

        [ConditionalFact(nameof(IsLdapConfigurationExist))]
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
                    SearchResultEntry sre = SearchOrganizationalUnit(connection, LdapConfiguration.Configuration.Domain, ouName);
                    Assert.NotNull(sre);

                    DeleteEntry(connection, dn);
                    sre = SearchOrganizationalUnit(connection, LdapConfiguration.Configuration.Domain, ouName);
                    Assert.Null(sre);
                }
                finally
                {
                    DeleteEntry(connection, dn);
                }
            }
        }

        [ConditionalFact(nameof(IsLdapConfigurationExist))]
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
                    SearchResultEntry sre = SearchOrganizationalUnit(connection, LdapConfiguration.Configuration.Domain, ouName);
                    Assert.NotNull(sre);
                    Assert.Equal("Protocols Group 3", (string) sre.Attributes["description"][0]);
                    Assert.Throws<DirectoryOperationException>(() => AddAttribute(connection, dn, "description", "Protocols Group 3"));

                    ModifyAttribute(connection, dn, "description", "Modified Protocols Group 3");
                    sre = SearchOrganizationalUnit(connection, LdapConfiguration.Configuration.Domain, ouName);
                    Assert.NotNull(sre);
                    Assert.Equal("Modified Protocols Group 3", (string) sre.Attributes["description"][0]);

                    DeleteAttribute(connection, dn, "description");
                    sre = SearchOrganizationalUnit(connection, LdapConfiguration.Configuration.Domain, ouName);
                    Assert.NotNull(sre);
                    Assert.Null(sre.Attributes["description"]);
                }
                finally
                {
                    DeleteEntry(connection, dn);
                }
            }
        }

        [ConditionalFact(nameof(IsLdapConfigurationExist))]
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
                    SearchResultEntry sre = SearchOrganizationalUnit(connection, LdapConfiguration.Configuration.Domain, ouLevel1Name);
                    Assert.NotNull(sre);

                    AddOrganizationalUnit(connection, dnLevel2);
                    sre = SearchOrganizationalUnit(connection, dnLevel1 + "," + LdapConfiguration.Configuration.Domain, ouLevel2Name);
                    Assert.NotNull(sre);
                }
                finally
                {
                    DeleteEntry(connection, dnLevel2);
                    DeleteEntry(connection, dnLevel1);
                }
            }
        }

        [ConditionalFact(nameof(IsLdapConfigurationExist))]
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
                    SearchResultEntry sre = SearchOrganizationalUnit(connection, LdapConfiguration.Configuration.Domain, ouName);
                    Assert.NotNull(sre);

                    AddOrganizationalRole(connection, user1Dn);
                    AddOrganizationalRole(connection, user2Dn);

                    string usersRoot = dn + "," + LdapConfiguration.Configuration.Domain;

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
                    sre = SearchOrganizationalUnit(connection, LdapConfiguration.Configuration.Domain, ouName);
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

        [ConditionalFact(nameof(IsLdapConfigurationExist))]
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

                    string fullDn = dn + "," + LdapConfiguration.Configuration.Domain;

                    ModifyRequest modRequest = new ModifyRequest(fullDn, mods);
                    ModifyResponse modResponse = (ModifyResponse) connection.SendRequest(modRequest);
                    Assert.Equal(ResultCode.Success, modResponse.ResultCode);
                    Assert.Throws<DirectoryOperationException>(() => (ModifyResponse) connection.SendRequest(modRequest));

                    SearchResultEntry sre = SearchOrganizationalUnit(connection, LdapConfiguration.Configuration.Domain, ouName);
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

                    sre = SearchOrganizationalUnit(connection, LdapConfiguration.Configuration.Domain, ouName);
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

                    sre = SearchOrganizationalUnit(connection, LdapConfiguration.Configuration.Domain, ouName);
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

        [ConditionalFact(nameof(IsLdapConfigurationExist))]
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
                    SearchResultEntry sre = SearchOrganizationalUnit(connection, LdapConfiguration.Configuration.Domain, ouName1);
                    Assert.NotNull(sre);

                    AddOrganizationalUnit(connection, dn2);
                    sre = SearchOrganizationalUnit(connection, LdapConfiguration.Configuration.Domain, ouName2);
                    Assert.NotNull(sre);

                    AddOrganizationalRole(connection, userDn1);

                    string user1Root = dn1 + "," + LdapConfiguration.Configuration.Domain;
                    string user2Root = dn2 + "," + LdapConfiguration.Configuration.Domain;

                    sre = SearchUser(connection, user1Root, "protocolUser7.1");
                    Assert.NotNull(sre);

                    ModifyDNRequest modDnRequest = new ModifyDNRequest( userDn1 + "," + LdapConfiguration.Configuration.Domain,
                                                                        dn2 + "," + LdapConfiguration.Configuration.Domain,
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

        [ConditionalFact(nameof(IsLdapConfigurationExist))]
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
                    SearchResultEntry sre = SearchOrganizationalUnit(connection, LdapConfiguration.Configuration.Domain, ouName);
                    Assert.NotNull(sre);

                    for (int i=0; i<20; i++)
                    {
                        AddOrganizationalUnit(connection, "ou=ProtocolsSubGroup9." + i + "," + dn);
                    }

                    string filter = "(objectClass=organizationalUnit)";
                    SearchRequest searchRequest = new SearchRequest(
                                                            dn + "," + LdapConfiguration.Configuration.Domain,
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
                                Assert.True(((SearchResultEntry)partialResult[i]).DistinguishedName.Contains("Group9"));
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
                            Assert.True(entry.DistinguishedName.Contains("Group9"));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                state.Exception = e;
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
                    SearchResultEntry sre = SearchOrganizationalUnit(connection, LdapConfiguration.Configuration.Domain, ouName);
                    Assert.NotNull(sre);

                    for (int i=0; i<20; i++)
                    {
                        AddOrganizationalUnit(connection, "ou=ProtocolsSubGroup8." + i + "," + dn);
                    }

                    string filter = "(objectClass=*)";
                    SearchRequest searchRequest = new SearchRequest(
                                                        dn + "," + LdapConfiguration.Configuration.Domain,
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

        private void DeleteAttribute(LdapConnection connection, string entryDn, string attributeName)
        {
            string dn = entryDn + "," + LdapConfiguration.Configuration.Domain;
            ModifyRequest modifyRequest = new ModifyRequest(dn, DirectoryAttributeOperation.Delete, attributeName);
            ModifyResponse modifyResponse = (ModifyResponse) connection.SendRequest(modifyRequest);
            Assert.Equal(ResultCode.Success, modifyResponse.ResultCode);
        }

        private void ModifyAttribute(LdapConnection connection, string entryDn, string attributeName, string attributeValue)
        {
            string dn = entryDn + "," + LdapConfiguration.Configuration.Domain;
            ModifyRequest modifyRequest = new ModifyRequest(dn, DirectoryAttributeOperation.Replace, attributeName, attributeValue);
            ModifyResponse modifyResponse = (ModifyResponse) connection.SendRequest(modifyRequest);
            Assert.Equal(ResultCode.Success, modifyResponse.ResultCode);
        }

        private void AddAttribute(LdapConnection connection, string entryDn, string attributeName, string attributeValue)
        {
            string dn = entryDn + "," + LdapConfiguration.Configuration.Domain;
            ModifyRequest modifyRequest = new ModifyRequest(dn, DirectoryAttributeOperation.Add, attributeName, attributeValue);
            ModifyResponse modifyResponse = (ModifyResponse) connection.SendRequest(modifyRequest);
            Assert.Equal(ResultCode.Success, modifyResponse.ResultCode);
        }

        private void AddOrganizationalUnit(LdapConnection connection, string entryDn)
        {
            string dn = entryDn + "," + LdapConfiguration.Configuration.Domain;
            AddRequest addRequest = new AddRequest(dn, "organizationalUnit");
            AddResponse addResponse = (AddResponse) connection.SendRequest(addRequest);
            Assert.Equal(ResultCode.Success, addResponse.ResultCode);
        }

        private void AddOrganizationalRole(LdapConnection connection, string entryDn)
        {
            string dn = entryDn + "," + LdapConfiguration.Configuration.Domain;
            AddRequest addRequest = new AddRequest(dn, "organizationalRole");
            AddResponse addResponse = (AddResponse) connection.SendRequest(addRequest);
            Assert.Equal(ResultCode.Success, addResponse.ResultCode);
        }

        private void DeleteEntry(LdapConnection connection, string entryDn)
        {
            try
            {
                string dn = entryDn + "," + LdapConfiguration.Configuration.Domain;
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
            SearchResponse searchResponse = (SearchResponse) connection.SendRequest(searchRequest);

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

        private LdapConnection GetConnection()
        {
            LdapDirectoryIdentifier directoryIdentifier = String.IsNullOrEmpty(LdapConfiguration.Configuration.Port) ?
                                        new LdapDirectoryIdentifier(LdapConfiguration.Configuration.ServerName, true, false) :
                                        new LdapDirectoryIdentifier(LdapConfiguration.Configuration.ServerName,
                                                                    int.Parse(LdapConfiguration.Configuration.Port, NumberStyles.None, CultureInfo.InvariantCulture),
                                                                    true, false);
            NetworkCredential credential = new NetworkCredential(LdapConfiguration.Configuration.UserName, LdapConfiguration.Configuration.Password);

            LdapConnection connection = new LdapConnection(directoryIdentifier, credential)
            {
                AuthType = AuthType.Basic
            };

            connection.Bind();
            connection.SessionOptions.ProtocolVersion = 3;
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
