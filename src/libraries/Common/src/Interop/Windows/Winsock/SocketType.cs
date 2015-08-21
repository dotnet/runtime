// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if SYSTEM_NET_SOCKETS_DLL
namespace System.Net.Sockets
#else
namespace System.Net.Internals
#endif
{
    /// <devdoc>
    ///    <para>
    ///       Specifies the type of socket an instance of the <see cref='System.Net.Sockets.Socket'/> class represents.
    ///    </para>
    /// </devdoc>
    public enum SocketType
    {
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Stream = 1,    // stream socket
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Dgram = 2,    // datagram socket
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Raw = 3,    // raw-protocolinterface
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Rdm = 4,    // reliably-delivered message
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Seqpacket = 5,    // sequenced packet stream
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Unknown = -1,   // Unknown socket type
    } // enum SocketType
} // namespace System.Net.Sockets
