// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;

namespace System.Resources
{
    /// <summary>
    /// Interface for resource grovelers.
    /// </summary>
    internal interface IResourceGroveler
    {
        ResourceSet? GrovelForResourceSet(CultureInfo culture, Dictionary<string, ResourceSet> localResourceSets, bool tryParents,
            bool createIfNotExists);
    }
}
