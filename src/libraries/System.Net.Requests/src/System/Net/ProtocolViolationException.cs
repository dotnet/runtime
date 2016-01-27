// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net
{
    /// <devdoc>
    ///    <para>
    ///       An exception class used when an attempt is made to use an invalid
    ///       protocol.
    ///    </para>
    /// </devdoc>
    public class ProtocolViolationException : InvalidOperationException
    {
        /// <devdoc>
        ///    <para>
        ///       Creates a new instance of the <see cref='System.Net.ProtocolViolationException'/>class.
        ///    </para>
        /// </devdoc>
        public ProtocolViolationException() : base()
        {
        }

        /// <devdoc>
        ///    <para>
        ///       Creates a new instance of the <see cref='System.Net.ProtocolViolationException'/>
        ///       class with the specified message.
        ///    </para>
        /// </devdoc>
        public ProtocolViolationException(string message) : base(message)
        {
        }
    }
}
