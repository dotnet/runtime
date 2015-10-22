// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace System.Net.NetworkInformation
{
    /// <devdoc>
    ///    <para>
    ///       Provides NetworkInformation exceptions to the application.
    ///    </para>
    /// </devdoc>
    public class NetworkInformationException : Win32Exception
    {
        /// <devdoc>
        ///    <para>
        ///       Creates a new instance of the <see cref='System.Net.NetworkInformation.NetworkInformationException'/> class with the default error code.
        ///    </para>
        /// </devdoc>
        public NetworkInformationException() : base(Marshal.GetLastWin32Error())
        {
        }

        /// <devdoc>
        ///    <para>
        ///       Creates a new instance of the <see cref='System.Net.NetworkInformation.NetworkInformationException'/> class with the specified error code.
        ///    </para>
        /// </devdoc>
        public NetworkInformationException(int errorCode) : base(errorCode)
        {
        }

        internal NetworkInformationException(SocketError socketError) : base((int)socketError)
        {
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int ErrorCode
        {
            // The base class returns the HResult with this property.
            // We need the Win32 error code, hence the override.
            get
            {
                return NativeErrorCode;
            }
        }
    }
}
