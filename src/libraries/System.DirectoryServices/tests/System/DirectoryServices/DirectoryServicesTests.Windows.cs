// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                        Assert.True(iads.ADsPath.IndexOf(LdapConfiguration.Configuration.ServerName, StringComparison.OrdinalIgnoreCase) >= 0);

                        IADsSecurityDescriptor iadsSD = (IADsSecurityDescriptor) de.Properties["ntSecurityDescriptor"].Value;
                        Assert.True(LdapConfiguration.Configuration.Domain.IndexOf(iadsSD.Owner.Split('\\')[0], StringComparison.OrdinalIgnoreCase) >= 0);
                        Assert.True(LdapConfiguration.Configuration.Domain.IndexOf(iadsSD.Group.Split('\\')[0], StringComparison.OrdinalIgnoreCase) >= 0);
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
