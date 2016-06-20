// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;
using Xunit;

namespace System.Net.Primitives.Tests
{
    public static class CredentialCacheTests
    {
        private const string UriPrefix = "http://name";
        private const string HostPrefix = "name";
        private const int Port = 80;
        private const string AuthenticationType = "authType";

        private static readonly NetworkCredential s_credential = new NetworkCredential();

        [Benchmark]
        [MeasureGCCounts]
        [InlineData("http://notfound", 0)]
        [InlineData("http://notfound", 10)]
        [InlineData("http://name5", 10)]
        public static void GetCredential_Uri(string uriString, int uriCount)
        {
            var uri = new Uri(uriString);
            CredentialCache cc = CreateCredentialCache(uriCount, hostPortCount: 0);

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    cc.GetCredential(uri, AuthenticationType);
                }
            }
        }

        [Benchmark]
        [MeasureGCCounts]
        [InlineData("notfound", 0)]
        [InlineData("notfound", 10)]
        [InlineData("name5", 10)]
        public static void GetCredential_HostPort(string host, int hostPortCount)
        {
            CredentialCache cc = CreateCredentialCache(uriCount: 0, hostPortCount: 0);

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    cc.GetCredential(host, Port, AuthenticationType);
                }
            }
        }

        [Benchmark]
        [MeasureGCCounts]
        [InlineData(0, 0)]
        [InlineData(10, 0)]
        [InlineData(0, 10)]
        [InlineData(10, 10)]
        public static void ForEach(int uriCount, int hostPortCount)
        {
            CredentialCache cc = CreateCredentialCache(uriCount, hostPortCount);

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    foreach (var c in cc)
                    {
                        // just iterate
                    }
                }
            }
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
