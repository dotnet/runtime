// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

using Internal.Reflection.Augments;

namespace System.Resources
{
    internal partial class ManifestBasedResourceGroveler
    {
        // Internal version of GetSatelliteAssembly that avoids throwing FileNotFoundException
        private static Assembly? InternalGetSatelliteAssembly(Assembly mainAssembly,
                                                             CultureInfo culture,
                                                             Version? version)
        {
            AssemblyName mainAssemblyAn = mainAssembly.GetName();
            AssemblyName an = new AssemblyName();

            an.CultureInfo = culture;
            an.Name = mainAssemblyAn.Name + ".resources";
            an.SetPublicKeyToken(mainAssemblyAn.GetPublicKeyToken());
            an.Flags = mainAssemblyAn.Flags;
            an.Version = version ?? mainAssemblyAn.Version;

            Assembly? retAssembly = ReflectionAugments.ReflectionCoreCallbacks.Load(an, false);

            if (retAssembly == mainAssembly)
            {
                retAssembly = null;
            }

            return retAssembly;
        }
    }
}
