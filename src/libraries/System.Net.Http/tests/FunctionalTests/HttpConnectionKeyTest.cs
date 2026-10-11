// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Net.Http.Functional.Tests
{
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public class HttpConnectionKeyTest
    {
        public static IEnumerable<object[]> KeyComponents()
        {
            yield return new object[] { "Https", "localhost", 80, "localhost-ssl", new Uri("http://localhost"), "domain1/userA", false};
            yield return new object[] { "Http", "localhost1", 80, "localhost-ssl", new Uri("http://localhost"), "domain1/userA", false };
            yield return new object[] { "Http", "localhost", 81, "localhost-ssl", new Uri("http://localhost"), "domain1/userA", false };
            yield return new object[] { "Http", "localhost", 80, "localhost-ssl1", new Uri("http://localhost"), "domain1/userA", false };
            yield return new object[] { "Http", "localhost", 80, "localhost-ssl", new Uri("http://localhost1"), "domain1/userA", false };
            yield return new object[] { "Http", "localhost", 80, "localhost-ssl", new Uri("http://localhost"), "domain1/userB", false };
            yield return new object[] { "Http", "localhost", 80, "localhost-ssl", new Uri("http://localhost"), "domain1/userA", true };
        }

        [Theory, MemberData(nameof(KeyComponents))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/129223", typeof(PlatformDetection), nameof(PlatformDetection.IsWasi))]
        public void Equals_DifferentParameters_ReturnsTrueIfAllEqual(string kindString, string host, int port, string sslHostName, Uri proxyUri, string identity, bool expected)
        {
            Type connectionKindType = Type.GetType("System.Net.Http.HttpConnectionKind, System.Net.Http");
            Type keyType = Type.GetType("System.Net.Http.HttpConnectionPoolManager+HttpConnectionKey, System.Net.Http");
            object referenceKey = Activator.CreateInstance(keyType, Enum.Parse(connectionKindType, "Http"), "localhost", 80, "localhost-ssl", new Uri("http://localhost"), "domain1/userA");
            object actualKey = Activator.CreateInstance(keyType, Enum.Parse(connectionKindType, kindString), host, port, sslHostName, proxyUri, identity);
            Assert.Equal(expected, referenceKey.Equals(actualKey));
        }

        [ConditionalTheory(typeof(SocketsHttpHandler), nameof(SocketsHttpHandler.IsSupported))]
        [InlineData("http://[fe80::1%1]/", "http://[fe80::1%2]/", false)]
        [InlineData("http://[fe80::1]/", "http://[fe80::1%1]/", false)]
        [InlineData("http://[fe80::1%eth0]/", "http://[fe80::1%eth1]/", false)]
        [InlineData("http://[fe80::1%eth0]/", "http://[fe80::1%ETH0]/", false)]
        [InlineData("http://[fe80::1%1]/", "http://[fe80::1%1]/", true)]
        [InlineData("http://[fe80::1%1]/", "HTTP://[fe80:0:0:0:0:0:0:1%1]:80/", true)]
        [InlineData("http://[fe80::1]/", "http://[fe80:0:0:0:0:0:0:1]:80/", true)]
        [InlineData("http://user@[fe80::1%1]/#first", "http://other@[fe80::1%1]/#second", true)]
        [InlineData("http://[fe80::1%1]:80/", "http://[fe80::1%1]:81/", false)]
        [InlineData("http://[fe80::1%1]/", "https://[fe80::1%1]/", false)]
        [InlineData("http://[fe80::1%1]/first", "http://[fe80::1%1]/second", false)]
        [InlineData("http://PROXY.TEST/", "http://proxy.test:80/", true)]
        [InlineData("http://127.0.0.1/", "http://127.1:80/", true)]
        public void Equals_ProxyUris_DistinguishesIPv6Scope(string firstProxyUri, string secondProxyUri, bool expected)
        {
            Type connectionKindType = Type.GetType("System.Net.Http.HttpConnectionKind, System.Net.Http");
            Type keyType = Type.GetType("System.Net.Http.HttpConnectionPoolManager+HttpConnectionKey, System.Net.Http");
            object kind = Enum.Parse(connectionKindType, "Http");

            foreach (string sslHostName in new[] { "localhost", "localhost-ssl" })
            {
                object firstKey = Activator.CreateInstance(keyType, kind, "localhost", 80, sslHostName, new Uri(firstProxyUri), string.Empty);
                object secondKey = Activator.CreateInstance(keyType, kind, "localhost", 80, sslHostName, new Uri(secondProxyUri), string.Empty);

                Assert.Equal(expected, firstKey.Equals(secondKey));
                Assert.Equal(expected, secondKey.Equals(firstKey));
                Assert.Equal(expected ? 1 : 2, new HashSet<object> { firstKey, secondKey }.Count);
                if (expected)
                {
                    Assert.Equal(firstKey.GetHashCode(), secondKey.GetHashCode());
                }
            }
        }
    }
}
