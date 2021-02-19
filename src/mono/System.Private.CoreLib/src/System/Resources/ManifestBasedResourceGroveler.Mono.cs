// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Reflection;

namespace System.Resources
{
    internal partial class ManifestBasedResourceGroveler
    {
        private static Assembly? InternalGetSatelliteAssembly(Assembly mainAssembly, CultureInfo culture, Version? version)
        {
            return (RuntimeAssembly.InternalGetSatelliteAssembly(mainAssembly, culture, version, throwOnFileNotFound: false));
        }
    }
}
