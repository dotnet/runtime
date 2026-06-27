// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Versioning;
using System.Threading;

namespace System.Net.NetworkInformation
{
    public partial class NetworkChange
    {
        /// <summary>Gets a value that indicates whether network change notifications are supported on the current platform.</summary>
        public static bool IsSupported => false;

        [EditorBrowsableAttribute(EditorBrowsableState.Never)]
        [Obsolete("This API supports the .NET Framework infrastructure and is not intended to be used directly from your code.", true)]
        public NetworkChange()
        {
        }

        [UnsupportedOSPlatform("illumos")]
        [UnsupportedOSPlatform("solaris")]
        [UnsupportedOSPlatform("haiku")]
        public static event NetworkAddressChangedEventHandler? NetworkAddressChanged
        {
            add { throw new PlatformNotSupportedException(); }
            remove { throw new PlatformNotSupportedException(); }
        }

        [UnsupportedOSPlatform("illumos")]
        [UnsupportedOSPlatform("solaris")]
        [UnsupportedOSPlatform("haiku")]
        public static event NetworkAvailabilityChangedEventHandler? NetworkAvailabilityChanged
        {
            add { throw new PlatformNotSupportedException(); }
            remove { throw new PlatformNotSupportedException(); }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This API supports the .NET Framework infrastructure and is not intended to be used directly from your code.", true)]
        public static void RegisterNetworkChange(NetworkChange nc) { }
    }
}
