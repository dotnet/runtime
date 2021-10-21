// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    public enum LoaderOptimization
    {
        [System.ObsoleteAttribute("LoaderOptimization.DisallowBindings has been deprecated and is not supported.")]
        DisallowBindings = 4,
        [System.ObsoleteAttribute("LoaderOptimization.DomainMask has been deprecated and is not supported.")]
        DomainMask = 3,
        MultiDomain = 2,
        MultiDomainHost = 3,
        NotSpecified = 0,
        SingleDomain = 1,
    }
}
