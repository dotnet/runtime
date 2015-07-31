// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Net.Sockets;

namespace System.Net
{
    /// <devdoc>
    ///    <para>
    ///       Identifies a network address.
    ///    </para>
    /// </devdoc>
    public abstract class EndPoint
    {
        /// <devdoc>
        ///    <para>
        ///       Returns the Address Family to which the EndPoint belongs.
        ///    </para>
        /// </devdoc>
        public virtual AddressFamily AddressFamily
        {
            get
            {
                throw NotImplemented.ByDesignWithMessage(SR.net_PropertyNotImplementedException);
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Serializes EndPoint information into a SocketAddress structure.
        ///    </para>
        /// </devdoc>
        public virtual SocketAddress Serialize()
        {
            throw NotImplemented.ByDesignWithMessage(SR.net_MethodNotImplementedException);
        }

        /// <devdoc>
        ///    <para>
        ///       Creates an EndPoint instance from a SocketAddress structure.
        ///    </para>
        /// </devdoc>
        public virtual EndPoint Create(SocketAddress socketAddress)
        {
            throw NotImplemented.ByDesignWithMessage(SR.net_MethodNotImplementedException);
        }
    }
}
