// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Net.NetworkInformation.Unit.Tests;

namespace Windows.Networking.Connectivity
{
    public enum NetworkConnectivityLevel
    {
        None = 0,
        LocalAccess = 1,
        ConstrainedInternetAccess = 2,
        InternetAccess = 3
    }

    public class ConnectionProfile
    {
        public ConnectionProfile()
        {
        }

        public NetworkConnectivityLevel GetNetworkConnectivityLevel()
        {
            return FakeNetwork.NetworkConnectivityLevel;
        }
    }

    public class NetworkInformation
    {
        public static ConnectionProfile GetInternetConnectionProfile()
        {
            if (FakeNetwork.IsConnectionProfilePresent)
            {
                return new ConnectionProfile();
            }
            else
            {
                return null;
            }
        }
    }
}
