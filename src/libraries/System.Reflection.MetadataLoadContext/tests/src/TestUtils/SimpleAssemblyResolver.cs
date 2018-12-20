// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.Tests
{
    /// <summary>
    /// Resolves the core assembly by creating a simulated core assembly and other assemblies by match
    /// on Name, Version, PublicKeyToken and CultureName.
    /// </summary>
    public class SimpleAssemblyResolver : CoreMetadataAssemblyResolver
    {
        private static readonly Version s_Version0000 = new Version(0, 0, 0, 0);

        public SimpleAssemblyResolver() { }

        public override Assembly Resolve(MetadataLoadContext context, AssemblyName assemblyName)
        {
            Assembly core = base.Resolve(context, assemblyName);
            if (core != null)
                return core;

            ReadOnlySpan<byte> pktFromAssemblyName = assemblyName.GetPublicKeyToken();
            foreach (Assembly assembly in context.GetAssemblies())
            {
                AssemblyName assemblyNameFromContext = assembly.GetName();
                if (assemblyName.Name.Equals(assemblyNameFromContext.Name, StringComparison.OrdinalIgnoreCase) &&
                    NormalizeVersion(assemblyName.Version).Equals(assemblyNameFromContext.Version) &&
                    pktFromAssemblyName.SequenceEqual(assemblyNameFromContext.GetPublicKeyToken()) &&
                    NormalizeCultureName(assemblyName.CultureName).Equals(NormalizeCultureName(assemblyNameFromContext.CultureName)))
                    return assembly;
            }

            return null;
        }

        private Version NormalizeVersion(Version version)
        {
            if (version == null)
                return s_Version0000;

            return version;
        }

        private string NormalizeCultureName(string cultureName)
        {
            if (cultureName == null)
                return string.Empty;

            return cultureName;
        }
    }
}
