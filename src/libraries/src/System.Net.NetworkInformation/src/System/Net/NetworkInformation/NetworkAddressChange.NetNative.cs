// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using RTConnectivity = Windows.Networking.Connectivity;

namespace System.Net.NetworkInformation
{
    public static class NetworkChange
    {
        public static event NetworkAddressChangedEventHandler NetworkAddressChanged;

        static NetworkChange()
        {
            RTConnectivity.NetworkInformation.NetworkStatusChanged += NetworkInformation_NetworkStatusChanged;
        }

        private static void NetworkInformation_NetworkStatusChanged(Object sender)
        {
            NetworkAddressChanged.Invoke(null, EventArgs.Empty);
            return;
        }
    }
}
