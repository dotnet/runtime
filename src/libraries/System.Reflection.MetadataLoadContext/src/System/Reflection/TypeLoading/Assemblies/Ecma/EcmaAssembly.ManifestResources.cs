// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection.Metadata;

namespace System.Reflection.TypeLoading.Ecma
{
    /// <summary>
    /// Base class for all Assembly objects created by a MetadataLoadContext and get its metadata from a PEReader.
    /// </summary>
    internal sealed partial class EcmaAssembly
    {
        public sealed override ManifestResourceInfo? GetManifestResourceInfo(string resourceName)
        {
            if (resourceName == null)
                throw new ArgumentNullException(nameof(resourceName));
            if (resourceName.Length == 0)
                throw new ArgumentException(nameof(resourceName));

            InternalManifestResourceInfo internalManifestResourceInfo = GetEcmaManifestModule().GetInternalManifestResourceInfo(resourceName);

            if (!internalManifestResourceInfo.Found)
            {
                return null;
            }

            if (internalManifestResourceInfo.ResourceLocation == ResourceLocation.ContainedInAnotherAssembly)
            {
                // Must get resource info from other assembly, and OR in the contained in another assembly information
                ManifestResourceInfo underlyingManifestResourceInfo = internalManifestResourceInfo.ReferencedAssembly.GetManifestResourceInfo(resourceName)!;
                internalManifestResourceInfo.FileName = underlyingManifestResourceInfo.FileName ?? string.Empty;
                internalManifestResourceInfo.ResourceLocation = underlyingManifestResourceInfo.ResourceLocation | ResourceLocation.ContainedInAnotherAssembly;
                if (underlyingManifestResourceInfo.ReferencedAssembly != null)
                    internalManifestResourceInfo.ReferencedAssembly = underlyingManifestResourceInfo.ReferencedAssembly;
            }

            return new ManifestResourceInfo(internalManifestResourceInfo.ReferencedAssembly, internalManifestResourceInfo.FileName, internalManifestResourceInfo.ResourceLocation);
        }

        public sealed override string[] GetManifestResourceNames()
        {
            MetadataReader reader = Reader;

            ManifestResourceHandleCollection manifestResources = reader.ManifestResources;
            string[] resourceNames = new string[manifestResources.Count];

            int iResource = 0;
            foreach (ManifestResourceHandle resourceHandle in manifestResources)
            {
                ManifestResource resource = resourceHandle.GetManifestResource(reader);
                resourceNames[iResource] = resource.Name.GetString(reader);
                iResource++;
            }

            return resourceNames;
        }

        [UnconditionalSuppressMessage("SingleFile", "IL3002:RequiresAssemblyFiles on Module.GetFile",
            Justification = "ResourceLocation should never be ContainedInAnotherAssembly if embedded in a single-file")]
        public sealed override Stream? GetManifestResourceStream(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (name.Length == 0)
                throw new ArgumentException(nameof(name));

            InternalManifestResourceInfo internalManifestResourceInfo = GetEcmaManifestModule().GetInternalManifestResourceInfo(name);

            if (!internalManifestResourceInfo.Found)
            {
                return null;
            }

            if ((internalManifestResourceInfo.ResourceLocation & ResourceLocation.Embedded) != 0)
            {
                unsafe
                {
                    return new UnmanagedMemoryStream(internalManifestResourceInfo.PointerToResource, internalManifestResourceInfo.SizeOfResource);
                }
            }
            else
            {
                if (internalManifestResourceInfo.ResourceLocation == ResourceLocation.ContainedInAnotherAssembly)
                {
                    return internalManifestResourceInfo.ReferencedAssembly.GetManifestResourceStream(name);
                }
                else
                {
                    return GetFile(internalManifestResourceInfo.FileName);
                }
            }
        }
    }
}
