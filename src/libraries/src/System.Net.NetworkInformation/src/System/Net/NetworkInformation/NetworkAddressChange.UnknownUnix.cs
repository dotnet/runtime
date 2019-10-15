// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.NetworkInformation
{
    public partial class NetworkChange
    {
        public static event NetworkAddressChangedEventHandler NetworkAddressChanged
        {
            add { throw new PlatformNotSupportedException(); }
            remove { throw new PlatformNotSupportedException(); }
        }

        public static event NetworkAvailabilityChangedEventHandler NetworkAvailabilityChanged
        {
            add { throw new PlatformNotSupportedException(); }
            remove { throw new PlatformNotSupportedException(); }
        }
    }
}
