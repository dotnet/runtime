// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Collections;
using Xunit;

namespace System.DirectoryServices.Tests
{
    public class DirectoryServicesTests
    {
        internal static bool IsLdapConfigurationExist => LdapConfiguration.Configuration != null;
        
        [ConditionalFact(nameof(IsLdapConfigurationExist))]
        public void TestOU() // adding and removing organization unit
        {
            using (DirectoryEntry de = new DirectoryEntry(LdapConfiguration.Configuration.LdapPath,
                                                            LdapConfiguration.Configuration.UserName,
                                                            LdapConfiguration.Configuration.Password,
                                                            LdapConfiguration.Configuration.AuthenticationTypes))
            {
                string ouName = "NetCoreDevs";
                
                // ensure cleanup before doing the creation.
                DeleteOU(de, ouName);

                CreateOU(de, ouName, ".Net Core Developers Unit");
                try
                {
                    SearchOUByName(de, ouName);
                }
                finally 
                {
                    // Clean up the added ou
                    DeleteOU(de, ouName);
                }
            }
        }

        [ConditionalFact(nameof(IsLdapConfigurationExist))]
        public void TestOrganizationalRole() // adding and removing users to/from the ou
        {
            using (DirectoryEntry de = new DirectoryEntry(LdapConfiguration.Configuration.LdapPath,
                                                            LdapConfiguration.Configuration.UserName,
                                                            LdapConfiguration.Configuration.Password,
                                                            LdapConfiguration.Configuration.AuthenticationTypes))
            {
                DeleteOU(de, "CoreFxRootOU");

                using (DirectoryEntry rootOU = CreateOU(de, "CoreFxRootOU", "CoreFx Root OU"))
                {
                    try
                    {
                        DirectoryEntry child1OU = CreateOU(rootOU, "CoreFxChild1OU", "CoreFx Child OU 1");
                        DirectoryEntry child2OU = CreateOU(rootOU, "CoreFxChild2OU", "CoreFx Child OU 2");

                        CreateOrganizationalRole(child1OU, "user.ou1.1", "User 1 is in CoreFx ou 1", "1 111 111 11111");
                        CreateOrganizationalRole(child1OU, "user.ou1.2", "User 2 is in CoreFx ou 1", "1 222 222 22222");

                        CreateOrganizationalRole(child2OU, "user.ou2.1", "User 1 is in CoreFx ou 2", "1 333 333 3333");
                        CreateOrganizationalRole(child2OU, "user.ou2.2", "User 2 is in CoreFx ou 2", "1 333 333 3333");

                        // now let's search for the added data:
                        SearchOUByName(rootOU, "CoreFxChild1OU");
                        SearchOUByName(rootOU, "CoreFxChild2OU");

                        SearchOrganizationalRole(child1OU, "user.ou1.1");
                        SearchOrganizationalRole(child1OU, "user.ou1.2");

                        SearchOrganizationalRole(child2OU, "user.ou2.1");
                        SearchOrganizationalRole(child2OU, "user.ou2.2");
                    }
                    finally
                    {
                        // rootOU.DeleteTree(); doesn't work as getting "A protocol error occurred. (Exception from HRESULT: 0x80072021)"
                        DeleteOU(de, "CoreFxRootOU");
                    }
                }
            }
        }

        private DirectoryEntry CreateOU(DirectoryEntry de, string ou, string description)
        {
            DirectoryEntry ouCoreDevs = de.Children.Add($"ou={ou}","Class");
            ouCoreDevs.Properties["objectClass"].Value = "organizationalUnit";    // has to be organizationalUnit
            ouCoreDevs.Properties["description"].Value = description;
            ouCoreDevs.Properties["ou"].Value = ou;
            ouCoreDevs.CommitChanges();
            return ouCoreDevs;
        }

        private DirectoryEntry CreateOrganizationalRole(DirectoryEntry ouEntry, string cn, string description, string phone)
        {
            DirectoryEntry cnEntry = ouEntry.Children.Add($"cn={cn}","Class");
            cnEntry.Properties["objectClass"].Value = "organizationalRole";
            cnEntry.Properties["cn"].Value = cn;
            cnEntry.Properties["description"].Value = description;
            cnEntry.Properties["ou"].Value = ouEntry.Name;
            cnEntry.Properties["telephoneNumber"].Value = phone;
            cnEntry.CommitChanges();
            return cnEntry;
        }

        private void DeleteOU(DirectoryEntry parentDe, string ou)
        {
            try
            {
                // We didn't use DirectoryEntry.DeleteTree as it fails on OpenDJ with "A protocol error occurred. (Exception from HRESULT: 0x80072021)"
                // Also on AD servers DirectoryEntry.Children.Remove(de) will fail if the de is not a leaf entry. so we had to do it recursively.
                DirectoryEntry de = parentDe.Children.Find($"ou={ou}");
                DeleteDirectoryEntry(parentDe, de);
            }
            catch
            {
                // ignore the error if the test failed early and couldn't create the OU we are trying to delete
            }
        }

        private void DeleteDirectoryEntry(DirectoryEntry parent, DirectoryEntry de)
        {
            foreach (DirectoryEntry child in de.Children)
            {
                DeleteDirectoryEntry(de, child);
            }

            parent.Children.Remove(de);
            parent.CommitChanges();
        }

        private void SearchOUByName(DirectoryEntry de, string ouName)
        {
            using (DirectorySearcher ds = new DirectorySearcher(de))
            {
                ds.ClientTimeout = new TimeSpan(0, 2, 0);
                ds.Filter = $"(&(objectClass=organizationalUnit)(ou={ouName}))";
                ds.PropertiesToLoad.Add("ou");
                SearchResult sr = ds.FindOne();
                Assert.Equal(ouName, sr.Properties["ou"][0]);
            }
        }

        private void SearchOrganizationalRole(DirectoryEntry de, string cnName)
        {
            using (DirectorySearcher ds = new DirectorySearcher(de))
            {
                ds.Filter = $"(&(objectClass=organizationalRole)(cn={cnName}))";
                ds.PropertiesToLoad.Add("cn");
                SearchResult sr = ds.FindOne();
                Assert.Equal(cnName, sr.Properties["cn"][0]);
            }
        }
    }
}
