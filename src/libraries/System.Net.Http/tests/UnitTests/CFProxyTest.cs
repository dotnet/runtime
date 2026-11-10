// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Xunit;

internal static partial class Interop
{
    internal static partial class Libraries
    {
        // The unit-test assembly also includes Windows interop for the proxy fakes.
        internal const string CoreFoundationLibrary = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
        internal const string CFNetworkLibrary = "/System/Library/Frameworks/CFNetwork.framework/CFNetwork";
    }
}

namespace System.Net.Http.Tests
{
    public partial class CFProxyTest
    {
        [LibraryImport(Interop.Libraries.CFNetworkLibrary)]
        private static partial SafeCFArrayHandle CFNetworkCopyProxiesForAutoConfigurationScript(
            SafeCreateHandle script, SafeCreateHandle targetUrl, out SafeCFErrorHandle error);

        [Theory]
        [InlineData("PROXY 127.0.0.1:1", 1)]
        [InlineData("PROXY 127.0.0.1:80", 80)]
        [InlineData("PROXY 127.0.0.1:443", 443)]
        [InlineData("PROXY 127.0.0.1:8080", 8080)]
        [InlineData("PROXY 127.0.0.1:65535", 65535)]
        [InlineData("DIRECT", -1)]
        public void ProxyAutoConfiguration_ReadsPortNumber(string scriptResult, int expectedPort)
        {
            using SafeCreateHandle script = Interop.CoreFoundation.CFStringCreateWithCString(
                $"function FindProxyForURL(url, host) {{ return '{scriptResult}'; }}");
            using SafeCreateHandle url = Interop.CoreFoundation.CFURLCreateWithString("https://example.invalid/");
            using SafeCFArrayHandle proxies = CFNetworkCopyProxiesForAutoConfigurationScript(script, url, out SafeCFErrorHandle error);
            using (error)
            {
                Assert.True(error.IsInvalid, Interop.CoreFoundation.GetErrorDescription(error));
                Assert.False(proxies.IsInvalid);
                Assert.Equal(1, Interop.CoreFoundation.CFArrayGetCount(proxies));

                using SafeCFDictionaryHandle dictionary = new SafeCFDictionaryHandle(
                    Interop.CoreFoundation.CFArrayGetValueAtIndex(proxies, 0), ownsHandle: false);
                Interop.CoreFoundation.CFProxy proxy = new Interop.CoreFoundation.CFProxy(dictionary);
                Assert.Equal(expectedPort, proxy.PortNumber);
            }
        }
    }
}
