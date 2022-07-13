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
        private static event NetworkAvailabilityChangedEventHandler? s_networkAvailabilityChanged;

        [UnsupportedOSPlatform("illumos")]
        [UnsupportedOSPlatform("solaris")]
        public static event NetworkAvailabilityChangedEventHandler? NetworkAvailabilityChanged
        {
            add
            {
                if (s_networkAvailabilityChanged == null)
                    BrowserNetworkInterfaceInterop.SetChangeListener(OnNetworkAvailabilityChanged);

                s_networkAvailabilityChanged += value;
            }
            remove
            {
                s_networkAvailabilityChanged -= value;

                if (s_networkAvailabilityChanged == null)
                    BrowserNetworkInterfaceInterop.RemoveChangeListener();
            }
        }

        private static void OnNetworkAvailabilityChanged(bool isOnline) => s_networkAvailabilityChanged?.Invoke(null, new NetworkAvailabilityEventArgs(isOnline));

        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("illumos")]
        [UnsupportedOSPlatform("solaris")]
        public static event NetworkAddressChangedEventHandler? NetworkAddressChanged
        {
            add => throw new System.PlatformNotSupportedException(System.SR.SystemNetNetworkInformation_PlatformNotSupported);
            remove => throw new System.PlatformNotSupportedException(System.SR.SystemNetNetworkInformation_PlatformNotSupported);
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
