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

using System.Globalization;
using System.Collections.Generic;

namespace System.Resources
{
    internal interface IResourceGroveler
    {
        ResourceSet? GrovelForResourceSet(CultureInfo culture, Dictionary<string, ResourceSet> localResourceSets, bool tryParents,
            bool createIfNotExists);
    }
}
