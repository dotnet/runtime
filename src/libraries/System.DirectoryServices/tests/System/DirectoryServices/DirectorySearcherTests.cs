// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Collections;
using Xunit;
using Xunit.Sdk;
using System.Reflection;

namespace System.DirectoryServices.Tests
{
    public partial class DirectorySearcherTests
    {
        internal static bool IsLdapConfigurationExist => LdapConfiguration.Configuration != null;
        internal static bool IsActiveDirectoryServer => IsLdapConfigurationExist && LdapConfiguration.Configuration.IsActiveDirectoryServer;

        private const int ADS_SYSTEMFLAG_CR_NTDS_NC = 0x1;
        private const int ADS_SYSTEMFLAG_CR_NTDS_DOMAIN = 0x2;

        [ConditionalFact(nameof(IsLdapConfigurationExist))]
        public void DirectorySearch_IteratesCorrectly_SimpleEnumeration()
        {
            var e = GetDomains();
            Assert.NotNull(e);

            foreach (var result in e)
            {
                Assert.NotNull(result);
            }
        }

        [ConditionalFact(nameof(IsLdapConfigurationExist))]
        public void DirectorySearch_IteratesCorrectly_AfterCount()
        {
            var e = GetDomains();
            Assert.NotNull(e);
            Assert.NotEqual(0, e.Count);

            foreach (var result in e)
            {
                Assert.NotNull(result);
            }
        }


        [ConditionalFact(nameof(IsLdapConfigurationExist))]
        public void DirectorySearch_IteratesCorrectly_MixedCount()
        {
            var e = GetDomains();
            Assert.NotNull(e);
            Assert.NotEqual(0, e.Count);

            foreach (var result in e)
            {
                Assert.NotEqual(0, e.Count);
                Assert.NotNull(result);
            }
        }

        private static SearchResultCollection GetDomains()
        {
            using var entry = new DirectoryEntry("LDAP://rootDSE");
            var namingContext = entry.Properties["configurationNamingContext"][0]!.ToString();
            using var searchRoot = new DirectoryEntry($"LDAP://CN=Partitions,{namingContext}");
            using var ds = new DirectorySearcher(searchRoot)
            {
                PageSize = 1000,
                CacheResults = false
            };
            ds.SearchScope = SearchScope.OneLevel;
            ds.PropertiesToLoad.Add("distinguishedName");
            ds.PropertiesToLoad.Add("nETBIOSName");
            ds.PropertiesToLoad.Add("nCName");
            ds.PropertiesToLoad.Add("dnsRoot");
            ds.PropertiesToLoad.Add("trustParent");
            ds.PropertiesToLoad.Add("objectSid");
            ds.Filter = string.Format("(&(objectCategory=crossRef)(systemFlags={0}))", ADS_SYSTEMFLAG_CR_NTDS_DOMAIN | ADS_SYSTEMFLAG_CR_NTDS_NC);

            return ds.FindAll();
        }
    }
}
