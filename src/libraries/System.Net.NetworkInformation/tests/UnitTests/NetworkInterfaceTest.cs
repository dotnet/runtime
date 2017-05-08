// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Windows.Networking.Connectivity;

using Xunit;

namespace System.Net.NetworkInformation.Unit.Tests
{
    public class NetworkInterfaceTest
    {
        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "dotnet/corefx #17993")]
        public void GetIsNetworkAvailable_ConnectionProfileNotPresent_ReturnsFalse()
        {
            FakeNetwork.IsConnectionProfilePresent = false;
            Assert.False(NetworkInterface.GetIsNetworkAvailable());
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "dotnet/corefx #17993")]
        public void GetIsNetworkAvailable_NetworkConnectivityLevelIsNone_ReturnsFalse()
        {
            FakeNetwork.IsConnectionProfilePresent = true;
            FakeNetwork.NetworkConnectivityLevel = NetworkConnectivityLevel.None;
            Assert.False(NetworkInterface.GetIsNetworkAvailable());
        }

        [Fact]
        public void GetIsNetworkAvailable_NetworkConnectivityLevelIsLocalAccess_ReturnsTrue()
        {
            FakeNetwork.IsConnectionProfilePresent = true;
            FakeNetwork.NetworkConnectivityLevel = NetworkConnectivityLevel.LocalAccess;
            Assert.True(NetworkInterface.GetIsNetworkAvailable());
        }

        [Fact]
        public void GetIsNetworkAvailable_NetworkConnectivityLevelIsConstrainedInternetAccess_ReturnsTrue()
        {
            FakeNetwork.IsConnectionProfilePresent = true;
            FakeNetwork.NetworkConnectivityLevel = NetworkConnectivityLevel.ConstrainedInternetAccess;
            Assert.True(NetworkInterface.GetIsNetworkAvailable());
        }

        [Fact]
        public void GetIsNetworkAvailable_NetworkConnectivityLevelIsInternetAccess_ReturnsTrue()
        {
            FakeNetwork.IsConnectionProfilePresent = true;
            FakeNetwork.NetworkConnectivityLevel = NetworkConnectivityLevel.InternetAccess;
            Assert.True(NetworkInterface.GetIsNetworkAvailable());
        }
    }
}
