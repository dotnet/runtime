// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions
{
    public enum SecurityAction
    {
        Assert = 3,
        Demand = 2,
        [System.ObsoleteAttribute("This requests should not be used")]
        Deny = 4,
        InheritanceDemand = 7,
        LinkDemand = 6,
        PermitOnly = 5,
        [System.ObsoleteAttribute("This requests should not be used")]
        RequestMinimum = 8,
        [System.ObsoleteAttribute("This requests should not be used")]
        RequestOptional = 9,
        [System.ObsoleteAttribute("This requests should not be used")]
        RequestRefuse = 10,
    }
}
