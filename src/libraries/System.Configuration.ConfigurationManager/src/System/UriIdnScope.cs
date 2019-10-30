// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System
{
    // This is used to control when host names are converted to idn names and
    // vice versa
    public enum UriIdnScope
    {
        None,                   // Never use Idn
        AllExceptIntranet,      // Use Idn in Internet and not intranet
        All                     // Internet and intranet
    }
}
