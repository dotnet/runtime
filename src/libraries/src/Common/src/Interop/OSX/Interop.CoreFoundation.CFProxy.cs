// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

using CFRunLoopSourceRef = System.IntPtr;

internal static partial class Interop
{
    internal static partial class CoreFoundation
    {
        [DllImport(Libraries.CFNetworkLibrary)]
        internal static extern SafeCFDictionaryHandle CFNetworkCopySystemProxySettings();

        [DllImport(Libraries.CFNetworkLibrary)]
        internal static extern SafeCFArrayHandle CFNetworkCopyProxiesForURL(SafeCreateHandle url, SafeCFDictionaryHandle proxySettings);

        internal delegate void CFProxyAutoConfigurationResultCallback(IntPtr client, IntPtr proxyList, IntPtr error);

        [DllImport(Libraries.CFNetworkLibrary)]
        internal static extern CFRunLoopSourceRef CFNetworkExecuteProxyAutoConfigurationURL(
            IntPtr proxyAutoConfigURL,
            SafeCreateHandle targetURL,
            CFProxyAutoConfigurationResultCallback cb,
            ref CFStreamClientContext clientContext);

        [DllImport(Libraries.CFNetworkLibrary)]
        internal static extern CFRunLoopSourceRef CFNetworkExecuteProxyAutoConfigurationScript(
            IntPtr proxyAutoConfigurationScript,
            SafeCreateHandle targetURL,
            CFProxyAutoConfigurationResultCallback cb,
            ref CFStreamClientContext clientContext);

        [StructLayout(LayoutKind.Sequential)]
        internal struct CFStreamClientContext
        {
            public IntPtr Version;
            public IntPtr Info;
            public IntPtr Retain;
            public IntPtr Release;
            public IntPtr CopyDescription;
        }

        internal class CFProxy
        {
            private SafeCFDictionaryHandle _dictionary;

            internal static readonly string kCFProxyTypeAutoConfigurationURL;
            internal static readonly string kCFProxyTypeAutoConfigurationJavaScript;
            internal static readonly string kCFProxyTypeFTP;
            internal static readonly string kCFProxyTypeHTTP;
            internal static readonly string kCFProxyTypeHTTPS;
            internal static readonly string kCFProxyTypeSOCKS;

            private static readonly IntPtr kCFProxyAutoConfigurationJavaScriptKey;
            private static readonly IntPtr kCFProxyAutoConfigurationURLKey;
            private static readonly IntPtr kCFProxyHostNameKey;
            private static readonly IntPtr kCFProxyPasswordKey;
            private static readonly IntPtr kCFProxyPortNumberKey;
            private static readonly IntPtr kCFProxyTypeKey;
            private static readonly IntPtr kCFProxyUsernameKey;

            static CFProxy()
            {
                IntPtr lib = NativeLibrary.Load(Interop.Libraries.CFNetworkLibrary);
                if (lib != IntPtr.Zero)
                {
                    kCFProxyTypeAutoConfigurationURL = LoadCFStringSymbol(lib, "kCFProxyTypeAutoConfigurationURL");
                    kCFProxyTypeAutoConfigurationJavaScript = LoadCFStringSymbol(lib, "kCFProxyTypeAutoConfigurationJavaScript");
                    kCFProxyTypeFTP = LoadCFStringSymbol(lib, "kCFProxyTypeFTP");
                    kCFProxyTypeHTTP = LoadCFStringSymbol(lib, "kCFProxyTypeHTTP");
                    kCFProxyTypeHTTPS = LoadCFStringSymbol(lib, "kCFProxyTypeHTTPS");
                    kCFProxyTypeSOCKS = LoadCFStringSymbol(lib, "kCFProxyTypeSOCKS");

                    kCFProxyAutoConfigurationJavaScriptKey = LoadSymbol(lib, "kCFProxyAutoConfigurationJavaScriptKey");
                    kCFProxyAutoConfigurationURLKey = LoadSymbol(lib, "kCFProxyAutoConfigurationURLKey");
                    kCFProxyHostNameKey = LoadSymbol(lib, "kCFProxyHostNameKey");
                    kCFProxyPasswordKey = LoadSymbol(lib, "kCFProxyPasswordKey");
                    kCFProxyPortNumberKey = LoadSymbol(lib, "kCFProxyPortNumberKey");
                    kCFProxyTypeKey = LoadSymbol(lib, "kCFProxyTypeKey");
                    kCFProxyUsernameKey = LoadSymbol(lib, "kCFProxyUsernameKey");
                }
            }

            public CFProxy(SafeCFDictionaryHandle dictionary)
            {
                _dictionary = dictionary;
            }

            private static IntPtr LoadSymbol(IntPtr lib, string name)
            {
                IntPtr indirect = NativeLibrary.GetExport(lib, name);
                return indirect == IntPtr.Zero ? IntPtr.Zero : Marshal.ReadIntPtr(indirect);
            }

            private static string LoadCFStringSymbol(IntPtr lib, string name)
            {
                using (SafeCFStringHandle cfString = new SafeCFStringHandle(LoadSymbol(lib, name), false))
                {
                    Debug.Assert(!cfString.IsInvalid);
                    return Interop.CoreFoundation.CFStringToString(cfString);
                }
            }

            private string GetString(IntPtr key)
            {
                IntPtr dictValue = CFDictionaryGetValue(_dictionary, key);
                if (dictValue != IntPtr.Zero)
                {
                    using (SafeCFStringHandle handle = new SafeCFStringHandle(dictValue, false))
                    {
                        return CFStringToString(handle);
                    }
                }
                return null;
            }

            public string ProxyType => GetString(kCFProxyTypeKey);
            public string HostName => GetString(kCFProxyHostNameKey);
            public string Username => GetString(kCFProxyUsernameKey);
            public string Password => GetString(kCFProxyPasswordKey);

            public int PortNumber
            {
                get
                {
                    IntPtr dictValue = CFDictionaryGetValue(_dictionary, kCFProxyPortNumberKey);
                    if (dictValue != IntPtr.Zero && CFNumberGetValue(dictValue, CFNumberType.kCFNumberIntType, out int value) > 0)
                    {
                        return value;
                    }
                    return -1;
                }
            }

            public IntPtr AutoConfigurationURL => CFDictionaryGetValue(_dictionary, kCFProxyAutoConfigurationURLKey);
            public IntPtr AutoConfigurationJavaScript => CFDictionaryGetValue(_dictionary, kCFProxyAutoConfigurationJavaScriptKey);
        }
    }
}
