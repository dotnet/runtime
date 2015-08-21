// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.NameResolution.Tests
{
    public class DnsTests
    {
        [Fact]
        public void GetHostAddressesAsync()
        {
            var hostAddressesTask1 = Dns.GetHostAddressesAsync(TestSettings.LocalHost);
            var hostAddressesTask2 = Dns.GetHostAddressesAsync(TestSettings.LocalHost);

            Task.WaitAll(hostAddressesTask1, hostAddressesTask2);

            var list1 = hostAddressesTask1.Result;
            var list2 = hostAddressesTask1.Result;

            Assert.NotNull(list1);
            Assert.NotNull(list2);

            Assert.Equal(list1.Length, list2.Length);
            for (int i = 0; i < list1.Length; i++)
            {
                Assert.Equal(list1[i], list2[i]);
            }
        }

        [Fact]
        public async Task GetHostEntryAsyncWithAddress()
        {
            IPAddress localIPAddress = await TestSettings.GetLocalIPAddress();

            GetHostEntryAsync(() => Dns.GetHostEntryAsync(localIPAddress));
        }

        [Fact]
        public void GetHostEntryAsyncWithHost()
        {
            GetHostEntryAsync(() => Dns.GetHostEntryAsync(TestSettings.LocalHost));
        }

        private static void GetHostEntryAsync(Func<Task<IPHostEntry>> getHostEntryFunc)
        {
            var hostEntryTask1 = getHostEntryFunc();
            var hostEntryTask2 = getHostEntryFunc();

            Task.WaitAll(hostEntryTask1, hostEntryTask2);

            var list1 = hostEntryTask1.Result.AddressList;
            var list2 = hostEntryTask2.Result.AddressList;

            Assert.NotNull(list1);
            Assert.NotNull(list2);

            Assert.Equal(list1.Length, list2.Length);
            for (var i = 0; i < list1.Length; i++)
            {
                Assert.Equal(list1[i], list2[i]);
            }
        }
    }
}
