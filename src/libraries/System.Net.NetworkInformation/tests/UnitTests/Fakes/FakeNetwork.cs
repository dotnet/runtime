// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
