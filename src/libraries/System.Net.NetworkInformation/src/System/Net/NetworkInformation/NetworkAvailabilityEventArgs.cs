// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.NetworkInformation
{
    public class NetworkAvailabilityEventArgs : EventArgs
    {
        private readonly bool _isAvailable;

        internal NetworkAvailabilityEventArgs(bool isAvailable)
        {
            _isAvailable = isAvailable;
        }

        public bool IsAvailable
        {
            get
            {
                return _isAvailable;
            }
        }
    }
}
