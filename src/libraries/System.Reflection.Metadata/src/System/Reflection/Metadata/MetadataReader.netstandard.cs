// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.Metadata
{
    public sealed partial class MetadataReader
    {
        internal AssemblyName GetAssemblyName(StringHandle nameHandle, Version version, StringHandle cultureHandle, BlobHandle publicKeyOrTokenHandle, AssemblyHashAlgorithm assemblyHashAlgorithm, AssemblyFlags flags)
        {
            string name = GetString(nameHandle);
            string cultureName = (!cultureHandle.IsNil) ? GetString(cultureHandle) : null;
            var hashAlgorithm = (Configuration.Assemblies.AssemblyHashAlgorithm)assemblyHashAlgorithm;
            byte[] publicKeyOrToken = !publicKeyOrTokenHandle.IsNil ? GetBlobBytes(publicKeyOrTokenHandle) : null;

            var assemblyName = new AssemblyName(name)
            {
                Version = version,
                CultureName = cultureName,
                HashAlgorithm = hashAlgorithm,
                Flags = GetAssemblyNameFlags(flags),
                ContentType = GetContentTypeFromAssemblyFlags(flags)
            };

            bool hasPublicKey = (flags & AssemblyFlags.PublicKey) != 0;
            if (hasPublicKey)
            {
                assemblyName.SetPublicKey(publicKeyOrToken);
            }
            else
            {
                assemblyName.SetPublicKeyToken(publicKeyOrToken);
            }
            
            return assemblyName;
        }

        private AssemblyNameFlags GetAssemblyNameFlags(AssemblyFlags flags)
        {
            AssemblyNameFlags assemblyNameFlags = AssemblyNameFlags.None;

            if ((flags & AssemblyFlags.PublicKey) != 0)
                assemblyNameFlags |= AssemblyNameFlags.PublicKey;

            if ((flags & AssemblyFlags.Retargetable) != 0)
                assemblyNameFlags |= AssemblyNameFlags.Retargetable;

            if ((flags & AssemblyFlags.EnableJitCompileTracking) != 0)
                assemblyNameFlags |= AssemblyNameFlags.EnableJITcompileTracking;

            if ((flags & AssemblyFlags.DisableJitCompileOptimizer) != 0)
                assemblyNameFlags |= AssemblyNameFlags.EnableJITcompileOptimizer;

            return assemblyNameFlags;
        }

        private AssemblyContentType GetContentTypeFromAssemblyFlags(AssemblyFlags flags)
        {
            return (AssemblyContentType)(((int)flags & (int)AssemblyFlags.ContentTypeMask) >> 9);
        }
    }
}
