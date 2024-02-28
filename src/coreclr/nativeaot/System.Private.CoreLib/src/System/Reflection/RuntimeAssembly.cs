// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

using Internal.Reflection.Augments;

namespace System.Reflection
{
    // Base class for runtime implemented Assembly
    public abstract class RuntimeAssembly : Assembly
    {
        internal static Assembly? InternalGetSatelliteAssembly(Assembly mainAssembly, CultureInfo culture, Version? version, bool throwOnFileNotFound)
        {
            AssemblyName mainAssemblyAn = mainAssembly.GetName();
            AssemblyName an = new AssemblyName();

            an.CultureInfo = culture;
            an.Name = mainAssemblyAn.Name + ".resources";
            an.SetPublicKeyToken(mainAssemblyAn.GetPublicKeyToken());
            an.Flags = mainAssemblyAn.Flags;
            an.Version = version ?? mainAssemblyAn.Version;

            Assembly? retAssembly = ReflectionAugments.ReflectionCoreCallbacks.Load(an, throwOnFileNotFound);

            if (retAssembly == mainAssembly)
            {
                retAssembly = null;
            }

            return retAssembly;
        }
    }
}
