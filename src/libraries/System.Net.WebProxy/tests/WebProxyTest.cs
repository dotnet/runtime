// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Net.Tests
{
    public class WebProxyTest
    {
        public static IEnumerable<object[]> Ctor_ExpectedPropertyValues_MemberData()
        {
            yield return new object[] { new WebProxy(), null, false, false, Array.Empty<string>(), null };

            yield return new object[] { new WebProxy("http://anything"), new Uri("http://anything"), false, false, Array.Empty<string>(), null };
            yield return new object[] { new WebProxy("anything", 42), new Uri("http://anything:42"), false, false, Array.Empty<string>(), null };
            yield return new object[] { new WebProxy(new Uri("http://anything")), new Uri("http://anything"), false, false, Array.Empty<string>(), null };

            yield return new object[] { new WebProxy("http://anything", true), new Uri("http://anything"), false, true, Array.Empty<string>(), null };
            yield return new object[] { new WebProxy(new Uri("http://anything"), true), new Uri("http://anything"), false, true, Array.Empty<string>(), null };

            yield return new object[] { new WebProxy("http://anything.com", true, new[] { ".*.com" }), new Uri("http://anything.com"), false, true, new[] { ".*.com" }, null };
            yield return new object[] { new WebProxy(new Uri("http://anything.com"), true, new[] { ".*.com" }), new Uri("http://anything.com"), false, true, new[] { ".*.com" }, null };

            var c = new DummyCredentials();
            yield return new object[] { new WebProxy("http://anything.com", true, new[] { ".*.com" }, c), new Uri("http://anything.com"), false, true, new[] { ".*.com" }, c };
            yield return new object[] { new WebProxy(new Uri("http://anything.com"), true, new[] { ".*.com" }, c), new Uri("http://anything.com"), false, true, new[] { ".*.com" }, c };
        }

        [Theory]
        [MemberData(nameof(Ctor_ExpectedPropertyValues_MemberData))]
        public static void WebProxy_Ctor_ExpectedPropertyValues(
            WebProxy p, Uri address, bool useDefaultCredentials, bool bypassLocal, string[] bypassedAddresses, ICredentials creds)
        {
            Assert.Equal(address, p.Address);
            Assert.Equal(useDefaultCredentials, p.UseDefaultCredentials);
            Assert.Equal(bypassLocal, p.BypassProxyOnLocal);
            Assert.Equal(bypassedAddresses, p.BypassList);
            Assert.Equal(bypassedAddresses, (string[])p.BypassArrayList.ToArray(typeof(string)));
            Assert.Equal(creds, p.Credentials);
        }

        [Fact]
        public static void WebProxy_BypassList_Roundtrip()
        {
            var p = new WebProxy();
            Assert.Empty(p.BypassList);
            Assert.Empty(p.BypassArrayList);

            string[] strings;

            strings = new string[] { "hello", "world" };
            p.BypassList = strings;
            Assert.Equal(strings, p.BypassList);
            Assert.Equal(strings, (string[])p.BypassArrayList.ToArray(typeof(string)));

            strings = new string[] { "hello" };
            p.BypassList = strings;
            Assert.Equal(strings, p.BypassList);
            Assert.Equal(strings, (string[])p.BypassArrayList.ToArray(typeof(string)));
        }

        [Fact]
        public static void WebProxy_UseDefaultCredentials_Roundtrip()
        {
            var p = new WebProxy();
            Assert.False(p.UseDefaultCredentials);
            Assert.Null(p.Credentials);

            p.UseDefaultCredentials = true;
            Assert.True(p.UseDefaultCredentials);
            Assert.NotNull(p.Credentials);

            p.UseDefaultCredentials = false;
            Assert.False(p.UseDefaultCredentials);
            Assert.Null(p.Credentials);
        }

        [Fact]
        public static void WebProxy_BypassProxyOnLocal_Roundtrip()
        {
            var p = new WebProxy();
            Assert.False(p.BypassProxyOnLocal);

            p.BypassProxyOnLocal = true;
            Assert.True(p.BypassProxyOnLocal);

            p.BypassProxyOnLocal = false;
            Assert.False(p.BypassProxyOnLocal);
        }

        [Fact]
        public static void WebProxy_Address_Roundtrip()
        {
            var p = new WebProxy();
            Assert.Null(p.Address);

            p.Address = new Uri("http://hello");
            Assert.Equal(new Uri("http://hello"), p.Address);

            p.Address = null;
            Assert.Null(p.Address);
        }

        [Fact]
        public static void WebProxy_InvalidArgs_Throws()
        {
            var p = new WebProxy();
            AssertExtensions.Throws<ArgumentNullException>("destination", () => p.GetProxy(null));
            AssertExtensions.Throws<ArgumentNullException>("host", () => p.IsBypassed(null));
            AssertExtensions.Throws<ArgumentNullException>("c", () => p.BypassList = null);
            Assert.ThrowsAny<ArgumentException>(() => p.BypassList = new string[] { "*.com" });
        }

        [Fact]
        public static void WebProxy_InvalidBypassUrl_AddedDirectlyToList_SilentlyEaten()
        {
            var p = new WebProxy("http://bing.com");
            p.BypassArrayList.Add("*.com");
            p.IsBypassed(new Uri("http://microsoft.com")); // exception should be silently eaten
        }

        [Fact]
        public static void WebProxy_BypassList_DoesntContainUrl_NotBypassed()
        {
            var p = new WebProxy("http://microsoft.com");
            Assert.Equal(new Uri("http://microsoft.com"), p.GetProxy(new Uri("http://bing.com")));
        }

        [Fact]
        public static void WebProxy_BypassList_ContainsUrl_IsBypassed()
        {
            var p = new WebProxy("http://microsoft.com", false, new[] { "hello", "bing.*", "world" });
            Assert.Equal(new Uri("http://bing.com"), p.GetProxy(new Uri("http://bing.com")));
        }

        public static IEnumerable<object[]> BypassOnLocal_MemberData()
        {
            // Local

            yield return new object[] { new Uri($"http://nodotinhostname"), true };
            yield return new object[] { new Uri($"http://{Guid.NewGuid().ToString("N")}"), true };
            foreach (IPAddress address in Dns.GetHostEntryAsync(Dns.GetHostName()).GetAwaiter().GetResult().AddressList)
            {
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    Uri uri;
                    try { uri = new Uri($"http://{address}"); }
                    catch (UriFormatException) { continue; }

                    yield return new object[] { uri, true };
                }
            }

            string domain = IPGlobalProperties.GetIPGlobalProperties().DomainName;
            if (!string.IsNullOrWhiteSpace(domain))
            {
                Uri uri = null;
                try { uri = new Uri($"http://{Guid.NewGuid().ToString("N")}.{domain}"); }
                catch (UriFormatException) { }

                if (uri != null)
                {
                    yield return new object[] { uri, true };
                }
            }

            // Non-local

            yield return new object[] { new Uri($"http://{Guid.NewGuid().ToString("N")}.com"), false };
            yield return new object[] { new Uri($"http://{IPAddress.None}"), false };
        }

        [ActiveIssue(23766, TestPlatforms.AnyUnix)]
        [Theory]
        [MemberData(nameof(BypassOnLocal_MemberData))]
        public static void WebProxy_BypassOnLocal_MatchesExpected(Uri destination, bool isLocal)
        {
            Uri proxyUri = new Uri("http://microsoft.com");

            try
            {
                Assert.Equal(isLocal, new WebProxy(proxyUri, true).IsBypassed(destination));
                Assert.False(new WebProxy(proxyUri, false).IsBypassed(destination));

                Assert.Equal(isLocal ? destination : proxyUri, new WebProxy(proxyUri, true).GetProxy(destination));
                Assert.Equal(proxyUri, new WebProxy(proxyUri, false).GetProxy(destination));
            }
            catch (SocketException exception)
            {
                // On Unix, getaddrinfo returns host not found, if all the machine discovery settings on the local network
                // is turned off. Hence dns lookup for it's own hostname fails.
                Assert.Equal(SocketError.HostNotFound, exception.SocketErrorCode);
                Assert.Throws<SocketException>(() => Dns.GetHostEntryAsync(Dns.GetHostName()).GetAwaiter().GetResult());
                Assert.True(RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX));
            }
        }

        [Fact]
        public static void WebProxy_BypassOnLocal_SpecialCases()
        {
            Assert.True(new WebProxy().IsBypassed(new Uri("http://anything.com")));
            Assert.True(new WebProxy((string)null).IsBypassed(new Uri("http://anything.com")));
            Assert.True(new WebProxy((Uri)null).IsBypassed(new Uri("http://anything.com")));
            Assert.True(new WebProxy("microsoft", BypassOnLocal: true).IsBypassed(new Uri($"http://{IPAddress.Loopback}")));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Not yet fixed in the .NET framework")]
        public static void WebProxy_BypassOnLocal_ConfiguredToNotBypassLocal()
        {
            Assert.False(new WebProxy("microsoft", BypassOnLocal: false).IsBypassed(new Uri($"http://{IPAddress.Loopback}")));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public static void WebProxy_GetDefaultProxy_NotSupported()
        {
#pragma warning disable 0618 // obsolete method
            Assert.Throws<PlatformNotSupportedException>(() => WebProxy.GetDefaultProxy());
#pragma warning restore 0618
        }

        private class DummyCredentials : ICredentials
        {
            public NetworkCredential GetCredential(Uri uri, string authType) => null;
        }
    }
}
