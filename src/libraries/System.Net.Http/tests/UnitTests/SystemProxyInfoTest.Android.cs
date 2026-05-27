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
                AppContext.SetSwitch("System.Net.Http.UseAndroidSystemProxy", bool.Parse(switchValue));

                IWebProxy proxy = SystemProxyInfo.ConstructSystemProxy();

                Assert.Equal(expectedTypeName, proxy.GetType().Name);
            }, useAndroidSystemProxy.ToString(), expectedType.Name).DisposeAsync();
        }

        [Theory]
        [InlineData(Interop.AndroidCrypto.AndroidProxyType.Direct, null, 0, true, null)]
        [InlineData(Interop.AndroidCrypto.AndroidProxyType.Http, "proxy.example", 8080, true, "http://proxy.example:8080/")]
        [InlineData(Interop.AndroidCrypto.AndroidProxyType.Socks, "proxy.example", 1080, true, "socks5://proxy.example:1080/")]
        [InlineData((Interop.AndroidCrypto.AndroidProxyType)42, "proxy.example", 8080, false, null)]
        [InlineData(Interop.AndroidCrypto.AndroidProxyType.Http, "", 8080, false, null)]
        public void AndroidPlatformProxy_TryCreateProxyUri_PreservesProxySelectorEntrySemantics(
            Interop.AndroidCrypto.AndroidProxyType proxyType,
            string? host,
            int port,
            bool expectedResult,
            string? expectedUri)
        {
            IntPtr hostPtr = host is null ? IntPtr.Zero : Marshal.StringToCoTaskMemUni(host);
            try
            {
                bool result = AndroidPlatformProxy.TryCreateProxyUri(CreateProxyEntry(proxyType, hostPtr, port), out Uri? proxyUri);

                Assert.Equal(expectedResult, result);
                Assert.Equal(expectedUri, proxyUri?.AbsoluteUri);
            }
            finally
            {
                Marshal.FreeCoTaskMem(hostPtr);
            }
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
