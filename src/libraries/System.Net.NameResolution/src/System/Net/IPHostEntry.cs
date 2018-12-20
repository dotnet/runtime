// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net
{
    // Host information
    /// <devdoc>
    ///    <para>Provides a container class for Internet host address information.</para>
    /// </devdoc>
    public class IPHostEntry
    {
        private string _hostName;
        private string[] _aliases;
        private IPAddress[] _addressList;
        // CBT: When doing a DNS resolve, can the resulting host name trusted as an SPN?
        // Only used on Win7Sp1+.  Assume trusted by default.
        internal bool isTrustedHost = true;

        /// <devdoc>
        ///    <para>
        ///       Contains the DNS
        ///       name of the host.
        ///    </para>
        /// </devdoc>
        /// <devdoc>
        /// </devdoc>
        public string HostName
        {
            get
            {
                return _hostName;
            }
            set
            {
                _hostName = value;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Provides an
        ///       array of strings containing other DNS names that resolve to the IP addresses
        ///       in <see cref="AddressList"/>.
        ///    </para>
        /// </devdoc>
        /// <devdoc>
        /// </devdoc>
        public string[] Aliases
        {
            get
            {
                return _aliases;
            }
            set
            {
                _aliases = value;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Provides an
        ///       array of <see cref="IPAddress"/> objects.
        ///    </para>
        /// </devdoc>
        /// <devdoc>
        /// </devdoc>
        public IPAddress[] AddressList
        {
            get
            {
                return _addressList;
            }
            set
            {
                _addressList = value;
            }
        }
    } // class IPHostEntry
} // namespace System.Net
