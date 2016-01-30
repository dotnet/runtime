// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Net.Sockets
{
    [Flags]
    internal enum AddressInfoHints
    {
        AI_PASSIVE = 0x01, /* Socket address will be used in bind() call */
        AI_CANONNAME = 0x02, /* Return canonical name in first ai_canonname */
        AI_NUMERICHOST = 0x04, /* Nodename must be a numeric address string */
        AI_FQDN = 0x20000, /* Return the FQDN in ai_canonname. This is different than AI_CANONNAME bit flag that
                                   * returns the canonical name registered in DNS which may be different than the fully
                                   * qualified domain name that the flat name resolved to. Only one of the AI_FQDN and 
                                   * AI_CANONNAME bits can be set.  Win7+ */
    }
}
