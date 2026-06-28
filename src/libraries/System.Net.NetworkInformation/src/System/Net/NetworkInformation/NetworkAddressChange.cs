// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace System.Net.NetworkInformation
{
    public partial class NetworkChange
    {
        /// <summary>Gets a value that indicates whether network change notifications are supported on the current platform.</summary>
        /// <value><see langword="true" /> if network change notifications are supported on the current platform; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the <see cref="NetworkAddressChanged" /> and <see cref="NetworkAvailabilityChanged" /> events will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static bool IsSupported => true;

        // The list of current address-changed subscribers.
        private static readonly Dictionary<NetworkAddressChangedEventHandler, ExecutionContext?> s_addressChangedSubscribers =
            new Dictionary<NetworkAddressChangedEventHandler, ExecutionContext?>();

        // The list of current availability-changed subscribers.
        private static readonly Dictionary<NetworkAvailabilityChangedEventHandler, ExecutionContext?> s_availabilityChangedSubscribers =
            new Dictionary<NetworkAvailabilityChangedEventHandler, ExecutionContext?>();

        private static readonly NetworkAvailabilityEventArgs s_availableEventArgs = new NetworkAvailabilityEventArgs(isAvailable: true);
        private static readonly NetworkAvailabilityEventArgs s_notAvailableEventArgs = new NetworkAvailabilityEventArgs(isAvailable: false);
        private static readonly ContextCallback s_runHandlerAvailable = new ContextCallback(RunAvailabilityHandlerAvailable!);
        private static readonly ContextCallback s_runHandlerNotAvailable = new ContextCallback(RunAvailabilityHandlerNotAvailable!);
        private static readonly ContextCallback s_runAddressChangedHandler = new ContextCallback(RunAddressChangedHandler!);

        [EditorBrowsableAttribute(EditorBrowsableState.Never)]
        [Obsolete("This API supports the .NET Framework infrastructure and is not intended to be used directly from your code.", true)]
        public NetworkChange()
        {
        }

        // Introduced for supporting design-time loading of System.Windows.dll
        [EditorBrowsable(EditorBrowsableState.Never)]
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
