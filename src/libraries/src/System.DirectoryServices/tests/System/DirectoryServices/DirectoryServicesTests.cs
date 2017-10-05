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
            using (DirectoryEntry de = CreateRootEntry())
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
            using (DirectoryEntry de = CreateRootEntry())
            {
                DeleteOU(de, "CoreFxRootOU");

                using (DirectoryEntry rootOU = CreateOU(de, "CoreFxRootOU", "CoreFx Root OU"))
                {
                    try
                    {
                        DirectoryEntry child1OU = CreateOU(rootOU, "CoreFxChild1OU", "CoreFx Child OU 1");
                        DirectoryEntry child2OU = CreateOU(rootOU, "CoreFxChild2OU", "CoreFx Child OU 2");

                        CreateOrganizationalRole(child1OU, "user.ou1.1", "User 1 is in CoreFx ou 1", "1 111 111 1111");
                        CreateOrganizationalRole(child1OU, "user.ou1.2", "User 2 is in CoreFx ou 1", "1 222 222 2222");

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

        [ConditionalFact(nameof(IsLdapConfigurationExist))]
        public void TestPropertyCaching() 
        {
            using (DirectoryEntry de = CreateRootEntry())
            {
                DeleteOU(de, "CachingOU");

                using (DirectoryEntry rootOU = CreateOU(de, "CachingOU", "Caching OU"))
                {
                    try
                    {
                        using (DirectoryEntry userEntry = CreateOrganizationalRole(rootOU, "caching.user.1", "User 1 in caching OU", "1 111 111 1111"))
                        {
                            Assert.True(userEntry.UsePropertyCache);
                            
                            SearchOrganizationalRole(rootOU, "caching.user.1");
                            string originalPhone = (string) userEntry.Properties["telephoneNumber"].Value;
                            string newPhone = "1 222 222 2222";
                            userEntry.Properties["telephoneNumber"].Value = newPhone;

                            using (DirectoryEntry sameUserEntry = GetOrganizationalRole(rootOU, "caching.user.1"))
                            {
                                Assert.Equal(originalPhone, (string) sameUserEntry.Properties["telephoneNumber"].Value);
                            }

			                userEntry.CommitChanges();

                            using (DirectoryEntry sameUserEntry = GetOrganizationalRole(rootOU, "caching.user.1"))
                            {
                                Assert.Equal(newPhone, (string) sameUserEntry.Properties["telephoneNumber"].Value);
                            }

			                userEntry.UsePropertyCache = false;
                            Assert.False(userEntry.UsePropertyCache);

                            userEntry.Properties["telephoneNumber"].Value = originalPhone;
                            using (DirectoryEntry sameUserEntry = GetOrganizationalRole(rootOU, "caching.user.1"))
                            {
                                Assert.Equal(originalPhone, (string) sameUserEntry.Properties["telephoneNumber"].Value);
                            }
                        }
                    }
                    finally
                    {
                        DeleteOU(de, "CachingOU");
                    }
                }
            }
        }

        [ConditionalFact(nameof(IsLdapConfigurationExist))]
        public void TestMoveTo() 
        {
            using (DirectoryEntry de = CreateRootEntry())
            {
                DeleteOU(de, "MoveingOU");
                
                try
                {
                    using (DirectoryEntry rootOU = CreateOU(de, "MoveingOU", "Moving Root OU"))
                    {
                        using (DirectoryEntry fromOU = CreateOU(rootOU, "MoveFromOU", "Moving From OU"))
                        using (DirectoryEntry toOU   = CreateOU(rootOU, "MoveToOU", "Moving To OU"))
                        {
                            DirectoryEntry user = CreateOrganizationalRole(fromOU, "user.move.1", "User to move across OU's", "1 111 111 1111");
                            SearchOrganizationalRole(fromOU, "user.move.1");

                            user.MoveTo(toOU);
                            user.CommitChanges();

                            SearchOrganizationalRole(toOU, "user.move.1");
                            Assert.Throws<DirectoryServicesCOMException>(() => SearchOrganizationalRole(fromOU, "user.move.1"));

                            user.MoveTo(fromOU, "cn=user.moved.1");
                            user.CommitChanges();

                            SearchOrganizationalRole(fromOU, "user.moved.1");

                            Assert.Throws<DirectoryServicesCOMException>(() => SearchOrganizationalRole(fromOU, "user.move.1"));
                            Assert.Throws<DirectoryServicesCOMException>(() => SearchOrganizationalRole(toOU, "user.move.1"));
                            Assert.Throws<DirectoryServicesCOMException>(() => user.MoveTo(toOU, "user.move.2"));
                        }
                    }
                }
                finally
                {
                    DeleteOU(de, "MoveingOU");
                }
            }
        }

        [ConditionalFact(nameof(IsLdapConfigurationExist))]
        public void TestCopyTo()
        {
            using (DirectoryEntry de = CreateRootEntry())
            {
                DeleteOU(de, "CopyingOU");
                
                try
                {
                    using (DirectoryEntry rootOU = CreateOU(de, "CopyingOU", "Copying Root OU"))
                    {
                        using (DirectoryEntry fromOU = CreateOU(rootOU, "CopyFromOU", "Copying From OU"))
                        using (DirectoryEntry toOU   = CreateOU(rootOU, "CopyToOU", "Copying To OU"))
                        {
                            DirectoryEntry user = CreateOrganizationalRole(fromOU, "user.move.1", "User to copy across OU's", "1 111 111 1111");
                            SearchOrganizationalRole(fromOU, "user.move.1");

                            // The Lightweight Directory Access Protocol (LDAP) provider does not currently support the CopyTo(DirectoryEntry) method.
                            Assert.Throws<NotImplementedException>(() => user.CopyTo(toOU));
                            Assert.Throws<NotImplementedException>(() => user.CopyTo(toOU, "user.move.2"));
                        }
                    }
                }
                finally
                {
                    DeleteOU(de, "CopyingOU");
                }
            }
        }

        [ConditionalFact(nameof(IsLdapConfigurationExist))]
        public void TestRename() 
        {
            using (DirectoryEntry de = CreateRootEntry())
            {
                DeleteOU(de, "RenamingOU");
                
                try
                {
                    using (DirectoryEntry rootOU = CreateOU(de, "RenamingOU", "Renaming Root OU"))
                    {
                        DirectoryEntry user = CreateOrganizationalRole(rootOU, "user.before.rename", "User to rename", "1 111 111 1111");
                        SearchOrganizationalRole(rootOU, "user.before.rename");


                        user.Rename("cn=user.after.rename");
                        user.CommitChanges();

                        Assert.Throws<DirectoryServicesCOMException>(() => SearchOrganizationalRole(rootOU, "user.before.rename"));

                        SearchOrganizationalRole(rootOU, "user.after.rename");

                        Assert.Throws<DirectoryServicesCOMException>(() => user.Rename("user.after.after.rename"));
                    }
                }
                finally
                {
                    DeleteOU(de, "RenamingOU");
                }
            }
        }

        [ConditionalFact(nameof(IsLdapConfigurationExist))]
        public void TestParent()
        {
            using (DirectoryEntry de = CreateRootEntry())
            {
                DeleteOU(de, "GrandParent");
                
                try
                {
                    using (DirectoryEntry grandOU = CreateOU(de, "GrandParent", "Grand Parent OU"))
                    using (DirectoryEntry parentOU = CreateOU(grandOU, "Parent", "Parent OU"))
                    using (DirectoryEntry childOU = CreateOU(parentOU, "Child", "Child OU"))
                    {
                        SearchOUByName(de, "GrandParent");
                        SearchOUByName(grandOU, "Parent");
                        SearchOUByName(parentOU, "Child");

                        Assert.Equal(de.Name, grandOU.Parent.Name);
                        Assert.Equal(grandOU.Name, parentOU.Parent.Name);
                        Assert.Equal(parentOU.Name, childOU.Parent.Name);
                    }
                }
                finally
                {
                    DeleteOU(de, "GrandParent");
                }
            }
        }
        
        [ConditionalFact(nameof(IsLdapConfigurationExist))]
        public void TestDeleteTree()
        {
            using (DirectoryEntry de = CreateRootEntry())
            {
                DeleteOU(de, "RootToDelete");
                
                try
                {
                    using (DirectoryEntry rootOU = CreateOU(de, "RootToDelete", "Root OU"))
                    using (DirectoryEntry childOU = CreateOU(rootOU, "Child1", "Root Child 1 OU"))
                    using (DirectoryEntry anotherChildOU = CreateOU(rootOU, "Child2", "Root Child 2 OU"))
                    using (DirectoryEntry grandChildOU = CreateOU(childOU, "GrandChild", "Grand Child OU"))
                    using (DirectoryEntry user1 = CreateOrganizationalRole(grandChildOU, "user.grandChild.1", "Grand Child User", "1 111 111 1111"))
                    using (DirectoryEntry user2 = CreateOrganizationalRole(grandChildOU, "user.grandChild.2", "Grand Child User", "1 222 222 2222"))
                    {
                        SearchOUByName(de, "RootToDelete");
                        SearchOUByName(rootOU, "Child1");
                        SearchOUByName(rootOU, "Child2");
                        SearchOUByName(childOU, "GrandChild");
                        SearchOrganizationalRole(grandChildOU, "user.grandChild.1");

                        if (LdapConfiguration.Configuration.IsActiveDirectoryServer)
                        {
                            rootOU.DeleteTree();

                            Assert.Throws<DirectoryServicesCOMException>(() => SearchOUByName(de, "RootToDelete"));
                            Assert.Throws<DirectoryServicesCOMException>(() => SearchOUByName(rootOU, "Child1"));
                            Assert.Throws<DirectoryServicesCOMException>(() => SearchOUByName(rootOU, "Child2"));
                            Assert.Throws<DirectoryServicesCOMException>(() => SearchOUByName(childOU, "GrandChild"));
                            Assert.Throws<DirectoryServicesCOMException>(() => SearchOrganizationalRole(grandChildOU, "user.grandChild.1"));
                        }
                        else
                        {
                            // on non Active Directory Servers, DeleteTree() throws DirectoryServicesCOMException : A protocol error occurred. (Exception from HRESULT: 0x80072021)
                            Assert.Throws<DirectoryServicesCOMException>(() => rootOU.DeleteTree());
                        }
                    }
                }
                finally
                {
                    DeleteOU(de, "RootToDelete");
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
                if (sr == null)
                {
                    throw new DirectoryServicesCOMException($"Couldn't find {ouName} in the entry {de.Name}");
                }
                Assert.Equal(ouName, sr.Properties["ou"][0]);
            }
        }

        private void SearchOrganizationalRole(DirectoryEntry de, string cnName)
        {
            using (DirectorySearcher ds = new DirectorySearcher(de))
            {
                ds.ClientTimeout = new TimeSpan(0, 2, 0);
                ds.Filter = $"(&(objectClass=organizationalRole)(cn={cnName}))";
                ds.PropertiesToLoad.Add("cn");
                SearchResult sr = ds.FindOne();

                if (sr == null)
                {
                    throw new DirectoryServicesCOMException($"Couldn't find {cnName} in the entry {de.Name}");
                }

                Assert.Equal(cnName, sr.Properties["cn"][0]);
            }
        }

        private DirectoryEntry GetOrganizationalRole(DirectoryEntry de, string cnName)
        {
            using (DirectorySearcher ds = new DirectorySearcher(de))
            {
                ds.ClientTimeout = new TimeSpan(0, 2, 0);
                ds.Filter = $"(&(objectClass=organizationalRole)(cn={cnName}))";
                ds.PropertiesToLoad.Add("cn");
                SearchResult sr = ds.FindOne();
                return sr.GetDirectoryEntry();
            }
        }

        private DirectoryEntry CreateRootEntry()
        {
            return new DirectoryEntry(LdapConfiguration.Configuration.LdapPath,
                                      LdapConfiguration.Configuration.UserName,
                                      LdapConfiguration.Configuration.Password,
                                      LdapConfiguration.Configuration.AuthenticationTypes);
        }
    }
}
