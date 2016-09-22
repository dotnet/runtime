// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using RTConnectivity = Windows.Networking.Connectivity;

namespace System.Net.NetworkInformation
{
    public class NetworkChange
    {
        //introduced for supporting design-time loading of System.Windows.dll
        [Obsolete("This API supports the .NET Framework infrastructure and is not intended to be used directly from your code.", true)]
        public static void RegisterNetworkChange(NetworkChange nc) { }

        public static event NetworkAddressChangedEventHandler NetworkAddressChanged;

        public static event NetworkAvailabilityChangedEventHandler NetworkAvailabilityChanged;

        static NetworkChange()
        {
            RTConnectivity.NetworkInformation.NetworkStatusChanged += NetworkInformation_NetworkStatusChanged;
        }

        private static void NetworkInformation_NetworkStatusChanged(object sender)
        {
            NetworkAddressChanged?.Invoke(null, EventArgs.Empty);
            NetworkAvailabilityChanged?.Invoke(null, new NetworkAvailabilityEventArgs(NetworkInterface.GetIsNetworkAvailable()));
        }
    }
}
