// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Reflection.PortableExecutable;

namespace System.Reflection.Metadata
{
    public sealed partial class MetadataReader
    {
        internal AssemblyName GetAssemblyName(StringHandle nameHandle, Version version, StringHandle cultureHandle, BlobHandle publicKeyOrTokenHandle, AssemblyHashAlgorithm assemblyHashAlgorithm, AssemblyFlags flags)
        {
            string name = GetString(nameHandle);
            // compat: normalize 'null' culture name to "" to match AssemblyName.GetAssemblyName()
            string cultureName = (!cultureHandle.IsNil) ? GetString(cultureHandle) : "";
            var hashAlgorithm = (Configuration.Assemblies.AssemblyHashAlgorithm)assemblyHashAlgorithm;
            byte[]? publicKeyOrToken = !publicKeyOrTokenHandle.IsNil ? GetBlobBytes(publicKeyOrTokenHandle) : null;

            var assemblyName = new AssemblyName(name)
            {
                Version = version,
                CultureName = cultureName,
#pragma warning disable SYSLIB0037 // AssemblyName.HashAlgorithm is obsolete
                HashAlgorithm = hashAlgorithm,
#pragma warning restore
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

        internal static AssemblyName GetAssemblyName(string assemblyFile)
        {
            AssemblyName assemblyName;
            try
            {
                using FileStream fs = new FileStream(assemblyFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                using PEReader peReader = new PEReader(fs, PEStreamOptions.PrefetchMetadata);
                MetadataReader mdReader = peReader.GetMetadataReader(MetadataReaderOptions.None);
                assemblyName = mdReader.GetAssemblyDefinition().GetAssemblyName();
            }
            catch (InvalidOperationException ex)
            {
                throw new BadImageFormatException(ex.Message);
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
