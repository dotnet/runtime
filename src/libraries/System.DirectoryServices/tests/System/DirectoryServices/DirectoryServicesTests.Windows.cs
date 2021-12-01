// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Collections;
using Xunit;
using ActiveDirectoryComInterop;

namespace System.DirectoryServices.Tests
{
    public partial class DirectoryServicesTests
    {
        [ConditionalFact(nameof(IsActiveDirectoryServer))]
        public void TestComInterfaces()
        {
            using (DirectoryEntry de = CreateRootEntry())
            {
                DeleteOU(de, "dateRoot");

                try
                {
                    using (DirectoryEntry rootOU = CreateOU(de, "dateRoot", "Date OU"))
                    {
                        long deTime = GetTimeValue((IADsLargeInteger) de.Properties["uSNCreated"].Value);
                        long rootOUTime = GetTimeValue((IADsLargeInteger) rootOU.Properties["uSNCreated"].Value);

                        // we are sure rootOU is created after de
                        Assert.True(rootOUTime > deTime);

                        IADs iads = (IADs) rootOU.NativeObject;
                        Assert.Equal("ou=dateRoot", iads.Name);
                        Assert.Equal("Class", iads.Class);
                        Assert.Contains(LdapConfiguration.Configuration.ServerName, iads.ADsPath, StringComparison.OrdinalIgnoreCase);

                        IADsSecurityDescriptor iadsSD = (IADsSecurityDescriptor) de.Properties["ntSecurityDescriptor"].Value;
                        Assert.Contains(iadsSD.Owner.Split('\\')[0], LdapConfiguration.Configuration.SearchDn, StringComparison.OrdinalIgnoreCase);
                        Assert.Contains(iadsSD.Group.Split('\\')[0], LdapConfiguration.Configuration.SearchDn, StringComparison.OrdinalIgnoreCase);
                    }
                }
                finally
                {
                    DeleteOU(de, "dateRoot");
                }
            }
        }

        private long GetTimeValue(IADsLargeInteger largeInteger)
        {
            return (long) largeInteger.LowPart | (long) (largeInteger.HighPart << 32);
        }
    }
}
