// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.NetworkInformation
{
    public delegate void NetworkAddressChangedEventHandler(object sender, EventArgs e);

    public delegate void NetworkAvailabilityChangedEventHandler(object sender, NetworkAvailabilityEventArgs e);
}
