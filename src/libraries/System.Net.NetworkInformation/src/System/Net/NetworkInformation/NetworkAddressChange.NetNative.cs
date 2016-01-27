// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
