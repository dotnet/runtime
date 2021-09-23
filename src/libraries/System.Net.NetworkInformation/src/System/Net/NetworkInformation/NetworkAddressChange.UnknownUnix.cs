// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Runtime.Versioning;

namespace System.Net.NetworkInformation
{
    public partial class NetworkChange
    {
        static NetworkChange()
        {
            // fake usage of static readonly fields to avoid getting CA1823 warning when we are compiling this partial.
            var addressChangedSubscribers = new Dictionary<NetworkAddressChangedEventHandler, ExecutionContext?>(s_addressChangedSubscribers);
            var availabilityChangedSubscribers = new Dictionary<NetworkAvailabilityChangedEventHandler, ExecutionContext?>(s_availabilityChangedSubscribers);
            NetworkAvailabilityEventArgs args = addressChangedSubscribers.Count > 0 ? s_availableEventArgs : s_notAvailableEventArgs;
            ContextCallback callbackContext = s_runAddressChangedHandler != null ? s_runHandlerAvailable : s_runHandlerNotAvailable;
        }

        [UnsupportedOSPlatform("illumos")]
        [UnsupportedOSPlatform("solaris")]
        public static event NetworkAddressChangedEventHandler? NetworkAddressChanged
        {
            add { throw new PlatformNotSupportedException(); }
            remove { throw new PlatformNotSupportedException(); }
        }

        [UnsupportedOSPlatform("illumos")]
        [UnsupportedOSPlatform("solaris")]
        public static event NetworkAvailabilityChangedEventHandler? NetworkAvailabilityChanged
        {
            add { throw new PlatformNotSupportedException(); }
            remove { throw new PlatformNotSupportedException(); }
        }
    }
}
