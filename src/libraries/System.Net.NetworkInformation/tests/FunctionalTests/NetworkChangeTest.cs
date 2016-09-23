// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Net.NetworkInformation.Tests
{
    public class NetworkChangeTest
    {
        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // https://github.com/Microsoft/BashOnWindows/issues/308
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
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // https://github.com/Microsoft/BashOnWindows/issues/308
        public void NetworkAvailabilityChanged_AddRemove_Success()
        {
            NetworkAvailabilityChangedEventHandler handler = NetworkChange_NetworkAvailabilityChanged;
            NetworkChange.NetworkAvailabilityChanged += handler;
            NetworkChange.NetworkAvailabilityChanged -= handler;
        }

        [Fact]
        public void NetworkAvailabilityChanged_JustRemove_Success()
        {
            NetworkAvailabilityChangedEventHandler handler = NetworkChange_NetworkAvailabilityChanged;
            NetworkChange.NetworkAvailabilityChanged -= handler;
        }

        private void NetworkChange_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
        }
    }
}
