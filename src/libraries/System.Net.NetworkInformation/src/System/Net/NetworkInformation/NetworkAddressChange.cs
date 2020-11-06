// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;

namespace System.Net.NetworkInformation
{
    public partial class NetworkChange
    {
        private static readonly NetworkAvailabilityEventArgs s_availableEventArgs = new NetworkAvailabilityEventArgs(isAvailable: true);
        private static readonly NetworkAvailabilityEventArgs s_notAvailableEventArgs = new NetworkAvailabilityEventArgs(isAvailable: false);

        // Introduced for supporting design-time loading of System.Windows.dll
        [Obsolete("This API supports the .NET Framework infrastructure and is not intended to be used directly from your code.", true)]
        public static void RegisterNetworkChange(NetworkChange nc) { }

        private static void RunAddressChangedHandler(object state)
        {
            ((NetworkAddressChangedEventHandler)state)(null, EventArgs.Empty);
        }

        private static void RunAvailabilityHandlerAvailable(object state)
        {
            ((NetworkAvailabilityChangedEventHandler)state)(null, s_availableEventArgs);
        }

        private static void RunAvailabilityHandlerNotAvailable(object state)
        {
            ((NetworkAvailabilityChangedEventHandler)state)(null, s_notAvailableEventArgs);
        }
    }
}
