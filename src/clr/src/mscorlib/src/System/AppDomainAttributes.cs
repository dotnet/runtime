// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
** 
**
** Purpose: For AppDomain-related custom attributes.
**
**
=============================================================================*/

namespace System
{
    internal enum LoaderOptimization
    {
        NotSpecified = 0,
        SingleDomain = 1,
        MultiDomain = 2,
        MultiDomainHost = 3,
        [Obsolete("This method has been deprecated. Please use Assembly.Load() instead. http://go.microsoft.com/fwlink/?linkid=14202")]
        DomainMask = 3,
        [Obsolete("This method has been deprecated. Please use Assembly.Load() instead. http://go.microsoft.com/fwlink/?linkid=14202")]
        DisallowBindings = 4
    }
}

