// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

using Xunit;

namespace System.Net.Primitives.Tests
{
    public class CredentialCacheTests
    {
        private const int Iterations = 10000000;

        private const string UriPrefix = "http://name";
        private const string HostPrefix = "name";
        private const int Port = 80;
        private const string AuthenticationType = "authType";

        private static readonly NetworkCredential s_credential = new NetworkCredential();

        [Theory]
        [OuterLoop]
        [Trait("Perf", "true")]
        [InlineData("http://notfound", 0, 14500000)]
        [InlineData("http://notfound", 10, 50200000)]
        [InlineData("http://name5", 10, 60200000)]
        public void GetCredential_Uri(string uriString, int uriCount, long expectedMaxTicks)
        {
            const int HostPortCount = 0;
            const int ExpectedMaxGen0CollectionCount = 0;

            var uri = new Uri(uriString);

            AssertGen0CollectionCountAndTime(
                uriCount,
                HostPortCount,
                ExpectedMaxGen0CollectionCount,
                expectedMaxTicks,
                cc => cc.GetCredential(uri, AuthenticationType));
        }

        [Theory]
        [OuterLoop]
        [Trait("Perf", "true")]
        [InlineData("notfound", 0, 16000000)]
        [InlineData("notfound", 10, 10000000)]
        [InlineData("name5", 10, 12000000)]
        public void GetCredential_HostPort(string host, int hostPortCount, long expectedMaxTicks)
        {
            const int UriCount = 0;
            const int ExpectedMaxGen0CollectionCount = 0;

            AssertGen0CollectionCountAndTime(
                UriCount,
                hostPortCount,
                ExpectedMaxGen0CollectionCount,
                expectedMaxTicks,
                cc => cc.GetCredential(host, Port, AuthenticationType));
        }

        [Theory]
        [OuterLoop]
        [Trait("Perf", "true")]
        [InlineData(0, 0, 96, 1650000)]
        [InlineData(10, 0, 153, 17500000)]
        [InlineData(0, 10, 153, 15500000)]
        [InlineData(10, 10, 229, 31800000)]
        public void ForEach(int uriCount, int hostPortCount, int expectedMaxGen0CollectionCount, long expectedMaxTicks)
        {
            AssertGen0CollectionCountAndTime(
                uriCount,
                hostPortCount,
                expectedMaxGen0CollectionCount,
                expectedMaxTicks,
                cc => { foreach (var c in cc) { } });
        }

        private static void AssertGen0CollectionCountAndTime(int uriCount, int hostPortCount, int expectedMaxGen0CollectionCount, long expectedMaxTicks, Action<CredentialCache> action)
        {
            CredentialCache cc = CreateCredentialCache(uriCount, hostPortCount);

            var sw = new Stopwatch();
            int gen0 = GC.CollectionCount(0);
            sw.Start();
            for (int i = 0; i < Iterations; i++)
            {
                action(cc);
            }
            sw.Stop();

            // TODO (#9048): Uncomment the asserts below when we can ensure invariant max values.
            //Assert.InRange(GC.CollectionCount(0) - gen0, 0, expectedMaxGen0CollectionCount);
            //Assert.InRange(sw.Elapsed.Ticks, 0, expectedMaxTicks);
        }

        private static CredentialCache CreateCredentialCache(int uriCount, int hostPortCount)
        {
            var cc = new CredentialCache();

            for (int i = 0; i < uriCount; i++)
            {
                Uri uri = new Uri(UriPrefix + i.ToString());
                cc.Add(uri, AuthenticationType, s_credential);
            }

            for (int i = 0; i < hostPortCount; i++)
            {
                string host = HostPrefix + i.ToString();
                cc.Add(host, Port, AuthenticationType, s_credential);
            }

            return cc;
        }
    }
}
