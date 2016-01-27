// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** 
** 
**
**
** Purpose: Interface for resource grovelers
**
** 
===========================================================*/
namespace System.Resources {    
    using System;
    using System.Globalization;
    using System.Threading;
    using System.Collections.Generic;
    using System.Runtime.Versioning;

    internal interface IResourceGroveler
    {
        ResourceSet GrovelForResourceSet(CultureInfo culture, Dictionary<String, ResourceSet> localResourceSets, bool tryParents, 
            bool createIfNotExists, ref StackCrawlMark stackMark);

#if !FEATURE_CORECLR  // PAL doesn't support eventing, and we don't compile event providers for coreclr

            bool HasNeutralResources(CultureInfo culture, String defaultResName);
#endif
    }
}
