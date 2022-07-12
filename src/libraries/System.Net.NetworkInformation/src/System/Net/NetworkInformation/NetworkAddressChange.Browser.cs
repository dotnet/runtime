// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;

using System.ComponentModel;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.Versioning;
using System.Threading;

namespace System.Net.NetworkInformation
{
    public partial class NetworkChange
    {
        private static bool s_isBrowserNetworkChangeListenerAttached;
        private static event NetworkAvailabilityChangedEventHandler? s_networkAvailabilityChanged;

        [UnsupportedOSPlatform("illumos")]
        [UnsupportedOSPlatform("solaris")]
        public static event NetworkAvailabilityChangedEventHandler? NetworkAvailabilityChanged
        {
            add
            {
                if (!s_isBrowserNetworkChangeListenerAttached)
                {
                    BrowserNetworkInterfaceInterop.AddChangeListener(OnNetworkChanged);
                    s_isBrowserNetworkChangeListenerAttached = true;
                }

                s_networkAvailabilityChanged += value;
            }
            remove
            {
                s_networkAvailabilityChanged -= value;
            }
        }

        private static void OnNetworkChanged(bool isOnline) => s_networkAvailabilityChanged?.Invoke(null, new NetworkAvailabilityEventArgs(isOnline));

        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("illumos")]
        [UnsupportedOSPlatform("solaris")]
        public static event NetworkAddressChangedEventHandler? NetworkAddressChanged
        {
            add => throw new PlatformNotSupportedException();
            remove => throw new PlatformNotSupportedException();
        }

        [EditorBrowsableAttribute(EditorBrowsableState.Never)]
        [Obsolete("This API supports the .NET Framework infrastructure and is not intended to be used directly from your code.", true)]
        public NetworkChange()
        {
        }

        // Introduced for supporting design-time loading of System.Windows.dll
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This API supports the .NET Framework infrastructure and is not intended to be used directly from your code.", true)]
        public static void RegisterNetworkChange(NetworkChange nc) { }
    }
}
