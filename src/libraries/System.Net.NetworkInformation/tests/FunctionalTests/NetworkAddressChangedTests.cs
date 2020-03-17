// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Net.NetworkInformation.Tests
{
    // Partial class used for both NetworkAddressChanged and NetworkAvailabilityChanged
    // so that the tests for each don't run concurrently
    public partial class NetworkChangedTests
    {
        private readonly NetworkAddressChangedEventHandler _addressHandler = delegate { };

        [Fact]
        public void NetworkAddressChanged_AddRemove_Success()
        {
            NetworkChange.NetworkAddressChanged += _addressHandler;
            NetworkChange.NetworkAddressChanged -= _addressHandler;
        }

        [Fact]
        public void NetworkAddressChanged_JustRemove_Success()
        {
            NetworkChange.NetworkAddressChanged -= _addressHandler;
        }
    }
}
