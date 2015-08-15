// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Net.NetworkInformation
{
    public delegate void NetworkAddressChangedEventHandler(object sender, System.EventArgs e);
    public static partial class NetworkChange
    {
        public static event System.Net.NetworkInformation.NetworkAddressChangedEventHandler NetworkAddressChanged { add { } remove { } }
    }
    public static partial class NetworkInterface
    {
        public static bool GetIsNetworkAvailable() { return default(bool); }
    }
}
