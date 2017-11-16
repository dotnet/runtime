// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.DirectoryServices.ActiveDirectory;
using System;
using Xunit;

namespace System.DirectoryServices.Tests
{
    public partial class DirectoryServicesTests
    {
        [ConditionalFact(nameof(IsActiveDirectoryServer))]
        public void TestSchema()
        {
            using (ActiveDirectorySchema schema = ActiveDirectorySchema.GetSchema(ActiveDirectoryContext))
            {
                Assert.True(schema.FindAllClasses().Contains(ActiveDirectorySchemaClass.FindByName(ActiveDirectoryContext, "user")));
                Assert.True(schema.FindAllClasses().Contains(ActiveDirectorySchemaClass.FindByName(ActiveDirectoryContext, "samDomainBase")));
                Assert.NotNull(schema.FindAllDefunctClasses());
                Assert.NotNull(schema.FindAllDefunctProperties());
                Assert.True(schema.FindAllProperties(PropertyTypes.Indexed).Contains(ActiveDirectorySchemaProperty.FindByName(ActiveDirectoryContext, "ou")));
                Assert.True(schema.FindAllProperties().Contains(ActiveDirectorySchemaProperty.FindByName(ActiveDirectoryContext, "cn")));
                Assert.Equal("person", schema.FindClass("person").Name);
                Assert.Equal("cn", schema.FindProperty("cn").Name);

                using (DirectoryEntry de = schema.GetDirectoryEntry())
                {
                    Assert.True("CN=Schema".Equals(de.Name, StringComparison.OrdinalIgnoreCase));
                }
            }
        }

        [ConditionalFact(nameof(IsActiveDirectoryServer))]
        public void TestSchemaClass()
        {
            using (ActiveDirectorySchemaClass orgClass = ActiveDirectorySchemaClass.FindByName(ActiveDirectoryContext, "organization"))
            {
                Assert.Equal("organization", orgClass.Name);
                Assert.Equal("Organization", orgClass.CommonName);
                Assert.Equal("2.5.6.4", orgClass.Oid);
                Assert.Equal("bf967aa3-0de6-11d0-a285-00aa003049e2", orgClass.SchemaGuid.ToString());
                Assert.Equal("top", orgClass.SubClassOf.Name);
                Assert.NotNull(orgClass.DefaultObjectSecurityDescriptor);
                string s = orgClass.Description; // it can be null
                Assert.False(orgClass.IsDefunct);

                Assert.True(orgClass.AuxiliaryClasses.Contains(ActiveDirectorySchemaClass.FindByName(ActiveDirectoryContext, "samDomainBase")));
                Assert.True(orgClass.PossibleInferiors.Contains(ActiveDirectorySchemaClass.FindByName(ActiveDirectoryContext, "user")));

                ActiveDirectorySchemaClass country = ActiveDirectorySchemaClass.FindByName(ActiveDirectoryContext, "country");
                Assert.True(orgClass.PossibleSuperiors.Contains(country));
                int index = orgClass.PossibleSuperiors.IndexOf(country);
                Assert.Equal(country.Name, orgClass.PossibleSuperiors[index].Name);

                Assert.True(orgClass.MandatoryProperties.Contains(ActiveDirectorySchemaProperty.FindByName(ActiveDirectoryContext, "ntSecurityDescriptor")));
                Assert.True(orgClass.OptionalProperties.Contains(ActiveDirectorySchemaProperty.FindByName(ActiveDirectoryContext, "description")));
                Assert.True(orgClass.MandatoryProperties.Contains(ActiveDirectorySchemaProperty.FindByName(ActiveDirectoryContext, "objectClass")));

                using (DirectoryEntry de = orgClass.GetDirectoryEntry())
                {
                    Assert.True("CN=Organization".Equals(de.Name, StringComparison.OrdinalIgnoreCase));
                }
            }
        }

        [ConditionalFact(nameof(IsActiveDirectoryServer))]
        public void TestSchemaProperty()
        {
            using (ActiveDirectorySchemaProperty adsp = ActiveDirectorySchemaProperty.FindByName(ActiveDirectoryContext, "objectClass"))
            {
                Assert.Equal("Object-Class", adsp.CommonName);
                Assert.False(adsp.IsDefunct);
                Assert.False(adsp.IsInAnr);
                Assert.True(adsp.IsIndexed);
                Assert.False(adsp.IsIndexedOverContainer);
                Assert.True(adsp.IsInGlobalCatalog);
                Assert.True(adsp.IsOnTombstonedObject);
                Assert.False(adsp.IsSingleValued);
                Assert.False(adsp.IsTupleIndexed);
                Assert.Equal("2.5.4.0", adsp.Oid);

                using (DirectoryEntry de = adsp.GetDirectoryEntry())
                {
                    Assert.True("CN=Object-Class".Equals(de.Name, StringComparison.OrdinalIgnoreCase));
                }

                Assert.Equal(ActiveDirectorySyntax.Oid, adsp.Syntax);
            }
        }

        [ConditionalFact(nameof(IsActiveDirectoryServer))]
        public void TestSchemaFilter()
        {
            // using (ActiveDirectorySchemaClass schema = ActiveDirectorySchemaClass.FindByName(ActiveDirectoryContext, "user"))
            using (ActiveDirectorySchema schema = ActiveDirectorySchema.GetSchema(ActiveDirectoryContext))
            using (DirectoryEntry de = schema.GetDirectoryEntry())
            {
                // by default there is no filters
                Assert.Equal(0, de.Children.SchemaFilter.Count);

                int topClassCount = 0;

                foreach (DirectoryEntry child in de.Children)
                {
                    string s  = (string) child.Properties["objectClass"][0];
                    topClassCount += s.Equals("top", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
                }

                de.Children.SchemaFilter.Add("top");
                Assert.Equal(1, de.Children.SchemaFilter.Count);
                Assert.True(de.Children.SchemaFilter.Contains("top"));
                Assert.Equal(0, de.Children.SchemaFilter.IndexOf("top"));
                Assert.Equal("top", de.Children.SchemaFilter[0]);

                int newTopClassCount = 0;

                foreach (DirectoryEntry child in de.Children)
                {
                    // we expect to get top only entries
                    string s  = (string) child.Properties["objectClass"][0];
                    Assert.True(s.Equals("top", StringComparison.OrdinalIgnoreCase));
                    newTopClassCount += 1;
                }

                Assert.Equal(topClassCount, newTopClassCount);

                de.Children.SchemaFilter.Remove("top");
                Assert.Equal(0, de.Children.SchemaFilter.Count);

                de.Children.SchemaFilter.Add("top");
                Assert.Equal(1, de.Children.SchemaFilter.Count);
                de.Children.SchemaFilter.RemoveAt(0);
                Assert.Equal(0, de.Children.SchemaFilter.Count);

                de.Children.SchemaFilter.AddRange(new string [] {"top", "user"});
                Assert.Equal(2, de.Children.SchemaFilter.Count);
                de.Children.SchemaFilter.Insert(0, "person");
                Assert.Equal(3, de.Children.SchemaFilter.Count);
                Assert.Equal("person", de.Children.SchemaFilter[0]);
                Assert.Equal("user", de.Children.SchemaFilter[2]);

                de.Children.SchemaFilter.Clear();
                Assert.Equal(0, de.Children.SchemaFilter.Count);
            }
        }

        [ConditionalFact(nameof(IsActiveDirectoryServer))]
        public void TestForestRootDomain()
        {
            using (Forest forest = Forest.GetForest(ActiveDirectoryContext))
            {
                using (Domain rootDomain = forest.RootDomain)
                {
                    Assert.Equal(forest.Name, rootDomain.Forest.Name);
                    Assert.Null(rootDomain.Parent);

                    forest.Domains.Contains(rootDomain);
                    int index = forest.Domains.IndexOf(rootDomain);
                    Assert.Equal(rootDomain.Name, forest.Domains[index].Name);

                    Domain [] domains = new Domain[0];

                    Assert.Throws<ArgumentException>(() => forest.Domains.CopyTo(domains, 0));
                    Assert.Throws<ArgumentNullException>(() => forest.Domains.CopyTo(null, 0));

                    domains = new Domain[forest.Domains.Count];
                    Assert.Throws<ArgumentOutOfRangeException>(() => forest.Domains.CopyTo(domains, -1));
                    forest.Domains.CopyTo(domains, 0);

                    Assert.NotNull(domains.FirstOrDefault(d => d.Name.Equals(rootDomain.Name)));
                }
            }
        }

        [ConditionalFact(nameof(IsActiveDirectoryServer))]
        public void TestForestSites()
        {
            using (Forest forest = Forest.GetForest(ActiveDirectoryContext))
            {
                Assert.True(forest.Sites.Count > 0);
                using (ActiveDirectorySite site = forest.Sites[0])
                {
                    Assert.True(forest.Sites.Contains(site));
                    Assert.Equal(0, forest.Sites.IndexOf(site));
                }
            }
        }

        [ConditionalFact(nameof(IsActiveDirectoryServer))]
        public void TestForestRoleOwnersAndModes()
        {
            using (Forest forest = Forest.GetForest(ActiveDirectoryContext))
            {
                Assert.Equal(forest.Name, forest.NamingRoleOwner.Forest.Name);
                Assert.Equal(forest.Name, forest.SchemaRoleOwner.Forest.Name);

                Assert.True(
                    forest.ForestMode == ForestMode.Unknown ||
                    forest.ForestMode == ForestMode.Windows2000Forest ||
                    forest.ForestMode == ForestMode.Windows2003Forest ||
                    forest.ForestMode == ForestMode.Windows2003InterimForest ||
                    forest.ForestMode == ForestMode.Windows2008Forest ||
                    forest.ForestMode == ForestMode.Windows2008R2Forest ||
                    forest.ForestMode == ForestMode.Windows2012R2Forest ||
                    forest.ForestMode == ForestMode.Windows8Forest);

                Assert.True(forest.ForestModeLevel >= 0);
                Assert.Equal(forest.Name, forest.NamingRoleOwner.Forest.Name);
            }
        }

        [ConditionalFact(nameof(IsActiveDirectoryServer))]
        public void TestForestSchema()
        {
            using (Forest forest = Forest.GetForest(ActiveDirectoryContext))
            {
                using (ActiveDirectorySchema schema = forest.Schema)
                using (ActiveDirectorySchemaClass  adsc = schema.FindClass("top"))
                {
                    Assert.True("top".Equals(adsc.CommonName, StringComparison.OrdinalIgnoreCase));
                }
            }
        }

        [ConditionalFact(nameof(IsActiveDirectoryServer))]
        public void TestForestGlobalCatalog()
        {
            using (Forest forest = Forest.GetForest(ActiveDirectoryContext))
            {
                int count = 0;
                GlobalCatalogCollection gcCollection = forest.FindAllGlobalCatalogs();
                foreach (GlobalCatalog gc in gcCollection)
                {
                    count++;
                }

                Assert.True(count > 0);
                Assert.True(gcCollection.Contains(gcCollection[0]));
                Assert.Equal(0, gcCollection.IndexOf(gcCollection[0]));

                gcCollection = forest.FindAllGlobalCatalogs(forest.Sites[0].Name);
                count = 0;
                foreach (GlobalCatalog gc in gcCollection)
                {
                    count++;
                }

                Assert.True(count > 0);
                Assert.True(gcCollection.Contains(gcCollection[0]));
                Assert.Equal(0, gcCollection.IndexOf(gcCollection[0]));

                GlobalCatalog globalCatalog = forest.FindGlobalCatalog(forest.Sites[0].Name);

                DirectoryContext forestContext = new DirectoryContext(
                                                        DirectoryContextType.Forest,
                                                        forest.Name,
                                                        LdapConfiguration.Configuration.UserName,
                                                        LdapConfiguration.Configuration.Password);

                Assert.Equal(globalCatalog.Name, GlobalCatalog.FindOne(forestContext, forest.Sites[0].Name).Name);
            }
        }

        [ConditionalFact(nameof(IsActiveDirectoryServer))]
        public void TestDomain()
        {
            using (Domain domain = Domain.GetDomain(ActiveDirectoryContext))
            {
                Assert.Equal(domain.Forest.Name, Forest.GetForest(ActiveDirectoryContext).Name);
                Assert.NotNull(domain.Children);

                DomainControllerCollection domainControllers = domain.DomainControllers;
                Assert.True(domainControllers.Contains(domain.PdcRoleOwner));
                Assert.True(domainControllers.IndexOf(domain.RidRoleOwner) >= 0);
                Assert.True(domainControllers.Contains(domain.InfrastructureRoleOwner));

                Assert.True(domain.DomainModeLevel >= 0);

                Assert.True(
                    domain.DomainMode == DomainMode.Unknown ||
                    domain.DomainMode == DomainMode.Windows2000MixedDomain ||
                    domain.DomainMode == DomainMode.Windows2000NativeDomain ||
                    domain.DomainMode == DomainMode.Windows2003Domain ||
                    domain.DomainMode == DomainMode.Windows2003InterimDomain ||
                    domain.DomainMode == DomainMode.Windows2008Domain ||
                    domain.DomainMode == DomainMode.Windows2008R2Domain ||
                    domain.DomainMode == DomainMode.Windows2012R2Domain ||
                    domain.DomainMode == DomainMode.Windows8Domain);

                if (domain.Forest.RootDomain.Name.Equals(domain.Name))
                {
                    Assert.Null(domain.Parent);
                }

                Assert.Throws<ArgumentNullException>(() => domain.GetSidFilteringStatus(null));
                Assert.Throws<ArgumentException>(() => domain.GetSidFilteringStatus(""));
            }
        }

        [ConditionalFact(nameof(IsActiveDirectoryServer))]
        public void TestDomainController()
        {
            using (Domain domain = Domain.GetDomain(ActiveDirectoryContext))
            {
                DirectoryContext dc = new DirectoryContext(
                                                    DirectoryContextType.Domain,
                                                    domain.Name,
                                                    LdapConfiguration.Configuration.UserName,
                                                    LdapConfiguration.Configuration.Password);

                using (DomainController controller = DomainController.FindOne(dc))
                {
                    using (Forest forest = Forest.GetForest(ActiveDirectoryContext))
                    {
                        Assert.Equal(forest.Name, controller.Forest.Name);
                    }

                    DomainControllerCollection dcc = DomainController.FindAll(dc);
                    Assert.True(dcc.Contains(controller));
                    Assert.True(dcc.IndexOf(controller) >= 0);

                    Assert.Equal(domain.Name, controller.Domain.Name);

                    Assert.True(controller.CurrentTime > DateTime.Today.AddDays(-2));

                    Assert.True(controller.HighestCommittedUsn > 0);

                    Assert.NotNull(controller.InboundConnections);
                    Assert.NotNull(controller.OutboundConnections);

                    foreach (ActiveDirectoryRole adr in controller.Roles)
                    {
                        Assert.True(
                            adr == ActiveDirectoryRole.InfrastructureRole ||
                            adr == ActiveDirectoryRole.NamingRole ||
                            adr == ActiveDirectoryRole.PdcRole ||
                            adr == ActiveDirectoryRole.RidRole ||
                            adr == ActiveDirectoryRole.SchemaRole);

                        Assert.True(controller.Roles.Contains(adr));
                        Assert.True(controller.Roles.IndexOf(adr) >= 0);
                    }

                    Assert.NotNull(controller.SiteName);

                    Assert.True(controller.OSVersion.IndexOf("Windows", StringComparison.OrdinalIgnoreCase) >= 0);
                    Assert.True(controller.IPAddress.IndexOf('.') >= 0);
                }
            }
        }

        [ConditionalFact(nameof(IsActiveDirectoryServer))]
        public void TestSites()
        {
            using (Forest forest = Forest.GetForest(ActiveDirectoryContext))
            {
                using (ActiveDirectorySite site = forest.Sites[0])
                using (ActiveDirectorySite s = ActiveDirectorySite.FindByName(ActiveDirectoryContext, site.Name))
                {
                    Assert.Equal(site.Name, s.Name);
                    Assert.True(s.Domains.Contains(forest.RootDomain));
                    Assert.NotNull(s.AdjacentSites);
                    Assert.NotNull(s.BridgeheadServers);
                    Assert.NotNull(s.PreferredRpcBridgeheadServers);
                    Assert.NotNull(s.PreferredSmtpBridgeheadServers);
                    Assert.NotNull(s.Subnets);

                    Assert.True(s.SiteLinks.Count > 0);
                    using (ActiveDirectorySiteLink adsl = s.SiteLinks[0])
                    {
                        Assert.True(s.SiteLinks.Contains(adsl));
                        Assert.Equal(0, s.SiteLinks.IndexOf(adsl));
                        Assert.True(adsl.Sites.Contains(s));
                        Assert.True(adsl.Cost >= 0);
                        Assert.True(adsl.TransportType == ActiveDirectoryTransportType.Rpc || adsl.TransportType == ActiveDirectoryTransportType.Smtp);
                    }

                    Assert.True(s.Servers.Contains(s.InterSiteTopologyGenerator));

                    using (DirectoryServer ds = s.Servers[0])
                    {
                        Assert.NotNull(ds.InboundConnections);
                        Assert.NotNull(ds.OutboundConnections);
                        Assert.True(ds.IPAddress.IndexOf('.') >= 0);
                        Assert.Equal(s.Name, ds.SiteName);

                        Assert.True(ds.Partitions.Count > 0);
                        string firstPartition = ds.Partitions[0];
                        Assert.True(ds.Partitions.Contains(firstPartition));
                        Assert.Equal(0, ds.Partitions.IndexOf(firstPartition));

                        string [] partitions = new string[0];
                        Assert.Throws<ArgumentException>(() => ds.Partitions.CopyTo(partitions, 0));
                        Assert.Throws<ArgumentNullException>(() => ds.Partitions.CopyTo(null, 0));
                        Assert.Throws<ArgumentOutOfRangeException>(() => ds.Partitions.CopyTo(partitions, -1));

                        partitions = new string[ds.Partitions.Count];
                        ds.Partitions.CopyTo(partitions, 0);
                        Assert.True(partitions.Contains(firstPartition));
                    }
                }
            }
        }

        private static DirectoryContext ActiveDirectoryContext => new DirectoryContext(
                                            DirectoryContextType.DirectoryServer,
                                            LdapConfiguration.Configuration.ServerName +
                                            (String.IsNullOrEmpty(LdapConfiguration.Configuration.Port) ? "" : ":" + LdapConfiguration.Configuration.Port),
                                            LdapConfiguration.Configuration.UserName,
                                            LdapConfiguration.Configuration.Password);

    }
}
