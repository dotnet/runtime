// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Net.Http.Tests
{
    public partial class SystemProxyInfoTest
    {
        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false, typeof(HttpNoProxy))]
        [InlineData(true, typeof(AndroidPlatformProxy))]
        public async Task Ctor_AndroidSystemProxySwitch_SelectsExpectedProxy(bool useAndroidSystemProxy, Type expectedType)
        {
            await RemoteExecutor.Invoke((switchValue, expectedTypeName) =>
            {
                // The child process may inherit HTTP(S)_PROXY / NO_PROXY (etc.) from the CI
                // environment. ConstructSystemProxy checks those first, so clear them to make
                // sure we actually exercise the AndroidPlatformProxy / HttpNoProxy selection.
                foreach (string envVar in new[] { "http_proxy", "HTTP_PROXY", "https_proxy", "HTTPS_PROXY",
                                                  "all_proxy", "ALL_PROXY", "no_proxy", "NO_PROXY", "GATEWAY_INTERFACE" })
                {
                    Environment.SetEnvironmentVariable(envVar, null);
                }

                AppContext.SetSwitch("System.Net.Http.UseAndroidSystemProxy", bool.Parse(switchValue));

                IWebProxy proxy = SystemProxyInfo.ConstructSystemProxy();

                Assert.Equal(expectedTypeName, proxy.GetType().Name);
            }, useAndroidSystemProxy.ToString(), expectedType.Name).DisposeAsync();
        }

        [Theory]
        [InlineData(Interop.AndroidCrypto.AndroidProxyType.Direct, null, 0, null)]
        [InlineData(Interop.AndroidCrypto.AndroidProxyType.Http, "proxy.example", 8080, "http://proxy.example:8080/")]
        [InlineData(Interop.AndroidCrypto.AndroidProxyType.Socks, "proxy.example", 1080, "socks5://proxy.example:1080/")]
        [InlineData((Interop.AndroidCrypto.AndroidProxyType)42, "proxy.example", 8080, null)]
        [InlineData(Interop.AndroidCrypto.AndroidProxyType.Http, "", 8080, null)]
        [InlineData(Interop.AndroidCrypto.AndroidProxyType.Http, "proxy.example", 0, null)]
        [InlineData(Interop.AndroidCrypto.AndroidProxyType.Http, "proxy.example", -1, null)]
        [InlineData(Interop.AndroidCrypto.AndroidProxyType.Socks, "proxy.example", 70000, null)]
        public void AndroidPlatformProxy_SelectProxyUri_MapsSingleEntry(
            Interop.AndroidCrypto.AndroidProxyType proxyType,
            string? host,
            int port,
            string? expectedUri)
        {
            IntPtr hostPtr = host is null ? IntPtr.Zero : Marshal.StringToCoTaskMemUni(host);
            try
            {
                Interop.AndroidCrypto.AndroidProxyInfo[] entries = [CreateProxyEntry(proxyType, hostPtr, port)];

                Uri? proxyUri = AndroidPlatformProxy.SelectProxyUri(entries);

                Assert.Equal(expectedUri, proxyUri?.AbsoluteUri);
            }
            finally
            {
                Marshal.FreeCoTaskMem(hostPtr);
            }
        }

        [Fact]
        public void AndroidPlatformProxy_SelectProxyUri_DirectEntryStopsFallback()
        {
            IntPtr hostPtr = Marshal.StringToCoTaskMemUni("proxy.example");
            try
            {
                // DIRECT precedes a usable HTTP entry: ProxySelector ordering means the
                // selected result is "no proxy", so the later entry must not be used.
                Interop.AndroidCrypto.AndroidProxyInfo[] entries =
                [
                    CreateProxyEntry(Interop.AndroidCrypto.AndroidProxyType.Direct, IntPtr.Zero, 0),
                    CreateProxyEntry(Interop.AndroidCrypto.AndroidProxyType.Http, hostPtr, 8080),
                ];

                Assert.Null(AndroidPlatformProxy.SelectProxyUri(entries));
            }
            finally
            {
                Marshal.FreeCoTaskMem(hostPtr);
            }
        }

        [Fact]
        public void AndroidPlatformProxy_SelectProxyUri_SkipsUnusableEntries()
        {
            IntPtr hostPtr = Marshal.StringToCoTaskMemUni("proxy.example");
            try
            {
                // An unknown proxy type is skipped in favor of the next usable entry.
                Interop.AndroidCrypto.AndroidProxyInfo[] entries =
                [
                    CreateProxyEntry((Interop.AndroidCrypto.AndroidProxyType)42, IntPtr.Zero, 0),
                    CreateProxyEntry(Interop.AndroidCrypto.AndroidProxyType.Http, hostPtr, 8080),
                ];

                Assert.Equal("http://proxy.example:8080/", AndroidPlatformProxy.SelectProxyUri(entries)?.AbsoluteUri);
            }
            finally
            {
                Marshal.FreeCoTaskMem(hostPtr);
            }
        }

        [Fact]
        public void AndroidPlatformProxy_SelectProxyUri_NoEntriesReturnsNull()
        {
            Assert.Null(AndroidPlatformProxy.SelectProxyUri(ReadOnlySpan<Interop.AndroidCrypto.AndroidProxyInfo>.Empty));
        }

        private static Interop.AndroidCrypto.AndroidProxyInfo CreateProxyEntry(
            Interop.AndroidCrypto.AndroidProxyType proxyType,
            IntPtr host,
            int port)
        {
            return new Interop.AndroidCrypto.AndroidProxyInfo
            {
                Type = (int)proxyType,
                Host = host,
                Port = port
            };
        }
    }
}
