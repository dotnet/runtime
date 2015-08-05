// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace System.Net.Sockets
{
    /// <devdoc>
    ///    <para>
    ///       Provides socket exceptions to the application.
    ///    </para>
    /// </devdoc>
    public partial class SocketException : Win32Exception
    {
        private EndPoint _endPoint;

        /// <devdoc>
        ///    <para>
        ///       Creates a new instance of the <see cref='System.Net.Sockets.SocketException'/> class with the default error code.
        ///    </para>
        /// </devdoc>
        public SocketException() : base(Marshal.GetLastWin32Error())
        {
            GlobalLog.Print("SocketException::.ctor() " + NativeErrorCode.ToString() + ":" + Message);
        }

        internal SocketException(EndPoint endPoint) : base(Marshal.GetLastWin32Error())
        {
            _endPoint = endPoint;
        }

        /// <devdoc>
        ///    <para>
        ///       Creates a new instance of the <see cref='System.Net.Sockets.SocketException'/> class with the specified error code.
        ///    </para>
        /// </devdoc>
        public SocketException(int errorCode) : base(errorCode)
        {
            GlobalLog.Print("SocketException::.ctor(int) " + NativeErrorCode.ToString() + ":" + Message);
        }

        internal SocketException(int errorCode, EndPoint endPoint) : base(errorCode)
        {
            _endPoint = endPoint;
        }

        /// <devdoc>
        ///    <para>
        ///       Creates a new instance of the <see cref='System.Net.Sockets.SocketException'/> class with the specified error code as SocketError.
        ///    </para>
        /// </devdoc>
        internal SocketException(SocketError socketError) : base((int)socketError)
        {
        }

        public int ErrorCode
        {
            // The base class returns the HResult with this property.
            // We need the Win32 error code, hence the override.
            get
            {
                return NativeErrorCode;
            }
        }

        public override string Message
        {
            get
            {
                // If not null, add EndPoint.ToString() to end of base Message
                if (_endPoint == null)
                {
                    return base.Message;
                }

                return base.Message + " " + _endPoint.ToString();
            }
        }


        public SocketError SocketErrorCode
        {
            // The base class returns the HResult with this property.
            // We need the Win32 error code, hence the override.
            get
            {
                return (SocketError)NativeErrorCode;
            }
        }
    }
}
