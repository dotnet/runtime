// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Net.NetworkInformation.Tests
{
    public class NetworkChangeTest
    {
        [Fact]
        public void NetworkAddressChanged_AddRemove_Success()
        {
            NetworkAddressChangedEventHandler handler = NetworkChange_NetworkAddressChanged;
            NetworkChange.NetworkAddressChanged += handler;
            NetworkChange.NetworkAddressChanged -= handler;
        }

        [Fact]
        public void NetworkAddressChanged_JustRemove_Success()
        {
            NetworkAddressChangedEventHandler handler = NetworkChange_NetworkAddressChanged;
            NetworkChange.NetworkAddressChanged -= handler;
        }

        private void NetworkChange_NetworkAddressChanged(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
