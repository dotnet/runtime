// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Net.Http;
using System.Net;
using System.Collections.Generic;
using Microsoft.Win32.SafeHandles;
using static Interop.CoreFoundation;
using static Interop.RunLoop;

using CFRunLoopRef = System.IntPtr;
using CFRunLoopSourceRef = System.IntPtr;

namespace System.Net.Http
{
    internal sealed class MacProxy : IWebProxy
    {
        public ICredentials Credentials
        {
            get => null;
            set => throw new NotSupportedException();
        }

        private static Uri GetProxyUri(string scheme, CFProxy proxy)
        {
            var uriBuilder = new UriBuilder(
                scheme,
                proxy.HostName,
                proxy.PortNumber);

            // TODO: Issue #26593 - Credentials are not propagated

            return uriBuilder.Uri;
        }

        public Uri ExecuteProxyAutoConfiguration(SafeCreateHandle cfurl, CFProxy proxy)
        {
            Uri result = null;
            CFRunLoopRef runLoop = CFRunLoopGetCurrent();

            // Callback that will be called after executing the configuration script
            CFProxyAutoConfigurationResultCallback cb = (IntPtr client, IntPtr proxyListHandle, IntPtr error) =>
            {
                if (proxyListHandle != IntPtr.Zero)
                {
                    using (var proxyList = new SafeCFArrayHandle(proxyListHandle, false))
                    {
                        long proxyCount = CFArrayGetCount(proxyList);
                        for (int i = 0; i < proxyCount; i++)
                        {
                            IntPtr proxyValue = CFArrayGetValueAtIndex(proxyList, i);
                            using (SafeCFDictionaryHandle proxyDict = new SafeCFDictionaryHandle(proxyValue, false))
                            {
                                CFProxy proxy = new CFProxy(proxyDict);
                                if (proxy.ProxyType == CFProxy.kCFProxyTypeHTTP || proxy.ProxyType == CFProxy.kCFProxyTypeHTTPS)
                                {
                                    result = GetProxyUri("http", proxy);
                                    break;
                                }
                            }
                        }
                    }
                }
                CFRunLoopStop(runLoop);
            };

            var clientContext = new CFStreamClientContext();
            CFRunLoopSourceRef loopSource =
                proxy.ProxyType == CFProxy.kCFProxyTypeAutoConfigurationURL ?
                CFNetworkExecuteProxyAutoConfigurationURL(proxy.AutoConfigurationURL, cfurl, cb, ref clientContext) :
                CFNetworkExecuteProxyAutoConfigurationScript(proxy.AutoConfigurationJavaScript, cfurl, cb, ref clientContext);

            using (var mode = CFStringCreateWithCString(typeof(MacProxy).FullName))
            {
                IntPtr modeHandle = mode.DangerousGetHandle();
                CFRunLoopAddSource(runLoop, loopSource, modeHandle);
                CFRunLoopRunInMode(modeHandle, double.MaxValue, 0);
                CFRunLoopSourceInvalidate(loopSource);
            }

            GC.KeepAlive(cb);

            return result;
        }

        public Uri GetProxy(Uri targetUri)
        {
            using (SafeCFDictionaryHandle systemProxySettings = CFNetworkCopySystemProxySettings())
            using (SafeCreateHandle cfurl = CFURLCreateWithString(targetUri.AbsoluteUri))
            using (SafeCFArrayHandle proxies = CFNetworkCopyProxiesForURL(cfurl, systemProxySettings))
            {
                long proxyCount = CFArrayGetCount(proxies);
                for (int i = 0; i < proxyCount; i++)
                {
                    IntPtr proxyValue = CFArrayGetValueAtIndex(proxies, i);
                    using (SafeCFDictionaryHandle proxyDict = new SafeCFDictionaryHandle(proxyValue, false))
                    {
                        CFProxy proxy = new CFProxy(proxyDict);

                        if (proxy.ProxyType == CFProxy.kCFProxyTypeAutoConfigurationURL || proxy.ProxyType == CFProxy.kCFProxyTypeAutoConfigurationJavaScript)
                        {
                            Uri result = ExecuteProxyAutoConfiguration(cfurl, proxy);
                            if (result != null)
                                return result;
                        }
                        else if (proxy.ProxyType == CFProxy.kCFProxyTypeHTTP || proxy.ProxyType == CFProxy.kCFProxyTypeHTTPS)
                        {
                            return GetProxyUri("http", proxy);
                        }
                    }
                }
            }

            return null;
        }

        public bool IsBypassed(Uri targetUri)
        {
            if (targetUri == null)
                throw new ArgumentNullException(nameof(targetUri));

            Uri proxyUri = GetProxy(targetUri);
            return Equals(proxyUri, targetUri) || proxyUri == null;
        }
    }
}
