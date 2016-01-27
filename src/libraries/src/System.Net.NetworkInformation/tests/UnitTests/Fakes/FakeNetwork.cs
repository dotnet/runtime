// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

using Windows.Networking.Connectivity;

namespace System.Net.NetworkInformation.Unit.Tests
{
    public static class FakeNetwork
    {
        public static NetworkConnectivityLevel NetworkConnectivityLevel { get; set; }
        public static bool IsConnectionProfilePresent { get; set; }
    }
}
