// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.NetworkInformation;

using Xunit;

namespace System.Net.Primitives.PalTests
{
    public class HostInformationPalTests
    {
        [Fact]
        public void HostName_NotNull()
        {
            Assert.NotNull(HostInformationPal.GetHostName());
        }

        [Fact]
        public void DomainName_NotNull()
        {
            Assert.NotNull(HostInformationPal.GetDomainName());
        }
    }
}
