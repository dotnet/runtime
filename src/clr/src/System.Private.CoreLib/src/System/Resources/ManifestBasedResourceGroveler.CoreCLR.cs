// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Resources
{
    internal partial class ManifestBasedResourceGroveler
    {
        // Internal version of GetSatelliteAssembly that avoids throwing FileNotFoundException
        private static Assembly InternalGetSatelliteAssembly(Assembly mainAssembly,
                                                             CultureInfo culture,
                                                             Version version)
        {
            return ((RuntimeAssembly)mainAssembly).InternalGetSatelliteAssembly(culture, version, throwOnFileNotFound: false);
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetNeutralResourcesLanguageAttribute(RuntimeAssembly assemblyHandle, StringHandleOnStack cultureName, out short fallbackLocation);

        private static bool GetNeutralResourcesLanguageAttribute(Assembly assemblyHandle, ref string cultureName, out short fallbackLocation)
        {
            return GetNeutralResourcesLanguageAttribute(((RuntimeAssembly)assemblyHandle).GetNativeHandle(),
                                                        JitHelpers.GetStringHandleOnStack(ref cultureName),
                                                        out fallbackLocation);
        }
    }
}
