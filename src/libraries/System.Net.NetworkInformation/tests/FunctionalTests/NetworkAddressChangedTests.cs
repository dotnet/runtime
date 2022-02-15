// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Diagnostics;
using System.Threading;
using System.Linq;

namespace System.Net.NetworkInformation.Tests
{
    // Partial class used for both NetworkAddressChanged and NetworkAvailabilityChanged
    // so that the tests for each don't run concurrently
    public partial class NetworkChangedTests
    {
        private readonly NetworkAddressChangedEventHandler _addressHandler = delegate { };

        [Fact]
        public void NetworkAddressChanged_AddRemove_Success()
        {
            NetworkChange.NetworkAddressChanged += _addressHandler;
            NetworkChange.NetworkAddressChanged -= _addressHandler;
        }

        [Fact]
        public void NetworkAddressChanged_JustRemove_Success()
        {
            NetworkChange.NetworkAddressChanged -= _addressHandler;
        }

        [PlatformSpecific(TestPlatforms.Linux)]
        [OuterLoop("May take several seconds")]
        [ConditionalFact(nameof(SupportsGettingThreadsWithPsCommand))]
        public void NetworkAddressChanged_AddRemoveMultipleTimes_CheckForLeakingThreads()
        {
            for (int i = 1; i <= 10; i++)
            {
                NetworkChange.NetworkAddressChanged += _addressHandler;
                NetworkChange.NetworkAddressChanged -= _addressHandler;
            }

            Thread.Sleep(2000); //allow some time for threads to exit

            //We are searching for threads containing ".NET Network Ad"
            //because ps command trims actual thread name ".NET Network Address Change".
            //This thread is created in:
            //  src/libraries/System.Net.NetworkInformation/src/System/Net/NetworkInformation/NetworkAddressChange.Unix.cs
            int numberOfNetworkAddressChangeThreads = ProcessUtil.GetProcessThreadsWithPsCommand(Process.GetCurrentProcess().Id)
                .Where(e => e.IndexOf(".NET Network Ad") > 0).Count();

            Assert.Equal(0, numberOfNetworkAddressChangeThreads); //there should be no threads because there are no event subscribers
        }

        private static bool SupportsGettingThreadsWithPsCommand
            => TestConfiguration.SupportsGettingThreadsWithPsCommand;
    }
}
