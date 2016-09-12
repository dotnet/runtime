// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security
{
    [System.ObsoleteAttribute("SecurityCriticalScope is only used for .NET 2.0 transparency compatibility.")]
    public enum SecurityCriticalScope
    {
        Everything = 1,
        Explicit = 0,
    }
}
