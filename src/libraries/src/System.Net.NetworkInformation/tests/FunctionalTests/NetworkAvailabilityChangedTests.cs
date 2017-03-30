// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Net.NetworkInformation.Tests
{
    // Partial class used for both NetworkAddressChanged and NetworkAvailabilityChanged
    // so that the tests for each don't run concurrently
    public partial class NetworkChangedTests
    {
        private readonly NetworkAvailabilityChangedEventHandler _availabilityHandler = delegate { };

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // https://github.com/Microsoft/BashOnWindows/issues/308
        public void NetworkAvailabilityChanged_AddRemove_Success()
        {
            NetworkChange.NetworkAvailabilityChanged += _availabilityHandler;
            NetworkChange.NetworkAvailabilityChanged -= _availabilityHandler;
        }

        [Fact]
        public void NetworkAvailabilityChanged_JustRemove_Success()
        {
            NetworkChange.NetworkAvailabilityChanged -= _availabilityHandler;
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // https://github.com/Microsoft/BashOnWindows/issues/308
        public void NetworkAddressChanged_AddAndRemove_NetworkAvailabilityChanged_JustRemove_Success()
        {
            NetworkChange.NetworkAddressChanged += _addressHandler;
            NetworkChange.NetworkAvailabilityChanged -= _availabilityHandler;
            NetworkChange.NetworkAddressChanged -= _addressHandler;
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // https://github.com/Microsoft/BashOnWindows/issues/308
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void NetworkAvailabilityChanged_NetworkAddressChanged_AddAndRemove_Success(bool addAddressFirst, bool removeAddressFirst)
        {
            if (addAddressFirst)
            {
                NetworkChange.NetworkAddressChanged += _addressHandler;
                NetworkChange.NetworkAvailabilityChanged += _availabilityHandler;
            }
            else
            {
                NetworkChange.NetworkAvailabilityChanged += _availabilityHandler;
                NetworkChange.NetworkAddressChanged += _addressHandler;
            }

            if (removeAddressFirst)
            {
                NetworkChange.NetworkAddressChanged -= _addressHandler;
                NetworkChange.NetworkAvailabilityChanged -= _availabilityHandler;
            }
            else
            {
                NetworkChange.NetworkAvailabilityChanged -= _availabilityHandler;
                NetworkChange.NetworkAddressChanged -= _addressHandler;
            }
        }
    }
}
