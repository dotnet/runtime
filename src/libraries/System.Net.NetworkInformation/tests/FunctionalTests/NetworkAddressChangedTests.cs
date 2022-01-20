// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

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
        [Fact]
        public async void NetworkAddressChanged_AddRemoveMultipleTimes_CheckForLeakingThreads()
        {
            static async Task<int> GetNumberOfNetworkAddressChangeThreadsAsync()
            {
                int pid = Process.GetCurrentProcess().Id;
                ProcessStartInfo psi = new ProcessStartInfo("ps", $"-T -p {pid}");
                psi.RedirectStandardOutput = true;

                using Process process = Process.Start(psi);
                if (process == null)
                {
                    throw new Exception("Could not create process 'ps'");
                }

                int threadCounter = 0;
                string output = await process.StandardOutput.ReadToEndAsync();
                using StringReader sr = new StringReader(output);
                while (true)
                {
                    string? line = await sr.ReadLineAsync();
                    if (line == null)
                    {
                        break;
                    }

                    if (line.IndexOf(".NET Network Ad") > 0)
                    {
                        //We are searching for threads containing ".NET Network Ad"
                        //because ps command trims actual thread name ".NET Network Address Change".
                        //This thread is created in:
                        //  src/libraries/System.Net.NetworkInformation/src/System/Net/NetworkInformation/NetworkAddressChange.Unix.cs
                        threadCounter++;
                    }
                }

                return threadCounter;
            }

            for (int i = 1; i <= 10; i++)
            {
                NetworkChange.NetworkAddressChanged += _addressHandler;
                NetworkChange.NetworkAddressChanged -= _addressHandler;
            }

            await Task.Delay(2000); //allow some time for threads to exit
            int numberOfNetworkAddressChangeThreads = await GetNumberOfNetworkAddressChangeThreadsAsync();
            Assert.Equal(0, numberOfNetworkAddressChangeThreads); //there should be no threads because there are no event subscribers
        }
    }
}
