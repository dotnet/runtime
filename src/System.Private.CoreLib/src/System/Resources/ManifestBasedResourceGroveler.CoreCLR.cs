// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Reflection;

namespace System.Resources
{
    internal partial class ManifestBasedResourceGroveler
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
