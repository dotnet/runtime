// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.Net.Http.Tests
{
    // Runs MacProxy against proxy settings created in-process, in the format returned by
    // CFNetworkCopySystemProxySettings, so the tests don't depend on the machine's proxy configuration.
    // The tests that return a proxy read its port with CFNumberGetValue, whose CFNumberType parameter is
    // CFIndex-sized. Passing it as a 32-bit value crashed on the CoreCLR interpreter (iOS, tvOS, Mac Catalyst).
    public partial class MacProxyTest
    {
        private static readonly Uri s_targetUri = new Uri("http://www.example.com/");
        private static readonly Uri s_proxyUri = new Uri("http://127.0.0.1:8888/");

        [Fact]
        public void GetProxy_HttpProxy_ReturnsProxyUri()
        {
            using SafeCFDictionaryHandle settings = CreateProxySettings(
                ("kCFNetworkProxiesHTTPEnable", CreateNumber(1)),
                ("kCFNetworkProxiesHTTPProxy", Interop.CoreFoundation.CFStringCreateWithCString("127.0.0.1")),
                ("kCFNetworkProxiesHTTPPort", CreateNumber(8888)));

            Assert.Equal(s_proxyUri, MacProxy.GetProxy(s_targetUri, settings));
        }

        [Fact]
        public void GetProxy_ProxyAutoConfigurationScript_ReturnsProxyUri()
        {
            using SafeCFDictionaryHandle settings = CreateProxySettings(
                ("kCFNetworkProxiesProxyAutoConfigEnable", CreateNumber(1)),
                ("kCFNetworkProxiesProxyAutoConfigJavaScript", Interop.CoreFoundation.CFStringCreateWithCString(
                    "function FindProxyForURL(url, host) { return \"PROXY 127.0.0.1:8888\"; }")));

            Assert.Equal(s_proxyUri, MacProxy.GetProxy(s_targetUri, settings));
        }

        [Fact]
        public void GetProxy_ProxyAutoConfigurationScriptReturnsDirect_ReturnsNull()
        {
            using SafeCFDictionaryHandle settings = CreateProxySettings(
                ("kCFNetworkProxiesProxyAutoConfigEnable", CreateNumber(1)),
                ("kCFNetworkProxiesProxyAutoConfigJavaScript", Interop.CoreFoundation.CFStringCreateWithCString(
                    "function FindProxyForURL(url, host) { return \"DIRECT\"; }")));

            Assert.Null(MacProxy.GetProxy(s_targetUri, settings));
        }

        [Fact]
        public void GetProxy_NoProxy_ReturnsNull()
        {
            using SafeCFDictionaryHandle settings = CreateProxySettings();

            Assert.Null(MacProxy.GetProxy(s_targetUri, settings));
        }

        private static unsafe SafeCreateHandle CreateNumber(int value) =>
            CFNumberCreate(IntPtr.Zero, kCFNumberIntType, &value);

        // Keys are the names of CFStringRef constants exported by CFNetwork. The values are released once the dictionary has retained them.
        private static unsafe SafeCFDictionaryHandle CreateProxySettings(params (string Key, SafeCreateHandle Value)[] settings)
        {
            try
            {
                IntPtr cfNetwork = NativeLibrary.Load(Interop.Libraries.CFNetworkLibrary);
                IntPtr coreFoundation = NativeLibrary.Load(Interop.Libraries.CoreFoundationLibrary);

                IntPtr[] keys = new IntPtr[settings.Length];
                IntPtr[] values = new IntPtr[settings.Length];
                for (int i = 0; i < settings.Length; i++)
                {
                    keys[i] = Marshal.ReadIntPtr(NativeLibrary.GetExport(cfNetwork, settings[i].Key));
                    values[i] = settings[i].Value.DangerousGetHandle();
                }

                fixed (IntPtr* pKeys = keys)
                fixed (IntPtr* pValues = values)
                {
                    return CFDictionaryCreate(IntPtr.Zero, pKeys, pValues, settings.Length,
                        NativeLibrary.GetExport(coreFoundation, "kCFTypeDictionaryKeyCallBacks"),
                        NativeLibrary.GetExport(coreFoundation, "kCFTypeDictionaryValueCallBacks"));
                }
            }
            finally
            {
                foreach ((_, SafeCreateHandle value) in settings)
                {
                    value.Dispose();
                }
            }
        }

        // CFNumberType is declared as CF_ENUM(CFIndex, CFNumberType).
        private const nint kCFNumberIntType = 9;

        [LibraryImport(Interop.Libraries.CoreFoundationLibrary)]
        private static unsafe partial SafeCreateHandle CFNumberCreate(IntPtr allocator, nint theType, int* valuePtr);

        [LibraryImport(Interop.Libraries.CoreFoundationLibrary)]
        private static unsafe partial SafeCFDictionaryHandle CFDictionaryCreate(
            IntPtr allocator, IntPtr* keys, IntPtr* values, nint numValues, IntPtr keyCallBacks, IntPtr valueCallBacks);
    }
}

internal static partial class Interop
{
    // Interop\OSX\Interop.Libraries.cs can't be compiled next to Interop\Windows\Interop.Libraries.cs
    // (both define MsQuic), so define the libraries that the MacProxy interop uses.
    internal static partial class Libraries
    {
        internal const string CoreFoundationLibrary = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
        internal const string CFNetworkLibrary = "/System/Library/Frameworks/CFNetwork.framework/CFNetwork";
    }
}
