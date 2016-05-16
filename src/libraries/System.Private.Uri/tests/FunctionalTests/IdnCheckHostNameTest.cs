// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Common.Tests;

using Xunit;

namespace System.PrivateUri.Tests
{
    public class IdnCheckHostNameTest
    {
        [Fact]
        public void IdnCheckHostName_Empty_Unknown()
        {
            Assert.Equal(UriHostNameType.Unknown, Uri.CheckHostName(String.Empty));
        }

        [Fact]
        public void IdnCheckHostName_FlatDns_Dns()
        {
            Assert.Equal(UriHostNameType.Dns, Uri.CheckHostName("Host"));
        }

        [Fact]
        public void IdnCheckHostName_FqdnDns_Dns()
        {
            Assert.Equal(UriHostNameType.Dns, Uri.CheckHostName("Host.corp.micorosoft.com"));
        }

        [Fact]
        public void IdnCheckHostName_IPv4_IPv4()
        {
            Assert.Equal(UriHostNameType.IPv4, Uri.CheckHostName(IPAddress.Loopback.ToString()));
        }

        [Fact]
        public void IdnCheckHostName_IPv6WithoutBrackets_IPv6()
        {
            Assert.Equal(UriHostNameType.IPv6, Uri.CheckHostName(IPAddress.IPv6Loopback.ToString()));
        }

        [Fact]
        public void IdnCheckHostName_IPv6WithBrackets_IPv6()
        {
            Assert.Equal(UriHostNameType.IPv6, Uri.CheckHostName("[" + IPAddress.IPv6Loopback.ToString() + "]"));
        }

        [Fact]
        public void IdnCheckHostName_UnicodeIdnOffIriOn_Dns()
        {
            using (var helper = new ThreadCultureChange())
            {
                Assert.Equal(UriHostNameType.Dns, Uri.CheckHostName("nZMot\u00E1\u00D3\u0063vKi\u00CD.contoso.com"));
                helper.ChangeCultureInfo("zh-cn");
                Assert.Equal(UriHostNameType.Dns, Uri.CheckHostName("nZMot\u00E1\u00D3\u0063vKi\u00CD.contoso.com"));
            }
        }
    }
}
