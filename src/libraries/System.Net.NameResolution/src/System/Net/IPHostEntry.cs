// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net
{
    /// <summary>Provides a container class for Internet host address information.</summary>
    public class IPHostEntry
    {
        // Technically there's nothing to prevent someone from doing `new IPHostEntry()`, at which point
        // all of these fields will be null.  However, that it not the intended usage of this type, which
        // is intended only to be returned from the various methods on the Dns type (and things that wrap
        // it), in which case the implementation will set all of the members to be non-null.  Thus, the
        // intent is that these be non-nullable.  Ideally the type would have been designed originally with
        // its ctor being internal-only.
        #pragma warning disable CS8618

        /// <summary>Gets or sets the DNS name of the host.</summary>
        public string HostName { get; set; }

        /// <summary>Gets or sets a list of aliases that are associated with a host.</summary>
        public string[] Aliases { get; set; }

        /// <summary>Gets or sets a list of IP addresses that are associated with a host.</summary>
        public IPAddress[] AddressList { get; set; }
    }
}
