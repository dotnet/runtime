// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Reflection;

namespace System.Resources
{
    internal sealed partial class ManifestBasedResourceGroveler
    {
        // Internal version of GetSatelliteAssembly that avoids throwing FileNotFoundException
        private static Assembly? InternalGetSatelliteAssembly(Assembly mainAssembly,
                                                             CultureInfo culture,
                                                             Version? version)
        {
            return ((RuntimeAssembly)mainAssembly).InternalGetSatelliteAssembly(culture, version, throwOnFileNotFound: false);
        }
    }
}
