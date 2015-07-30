// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

