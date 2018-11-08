// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace System.Reflection.TypeLoading.Ecma
{
    /// <summary>
    /// Base class for all Module objects created by a MetadataLoadContext and get its metadata from a PEReader.
    /// </summary>
    internal sealed partial class EcmaModule
    {
        internal unsafe InternalManifestResourceInfo GetInternalManifestResourceInfo(string resourceName)
        {
            MetadataReader reader = Reader;

            InternalManifestResourceInfo result = new InternalManifestResourceInfo();
            ManifestResourceHandleCollection manifestResources = reader.ManifestResources;
            foreach (ManifestResourceHandle resourceHandle in manifestResources)
            {
                ManifestResource resource = resourceHandle.GetManifestResource(reader);
                if (resource.Name.Equals(resourceName, reader))
                {
                    result.Found = true;
                    if (resource.Implementation.IsNil)
                    {
                        checked
                        {
                            // Embedded data resource
                            result.ResourceLocation = ResourceLocation.Embedded | ResourceLocation.ContainedInManifestFile;
                            PEReader pe = _guardedPEReader.PEReader;

                            PEMemoryBlock resourceDirectory = pe.GetSectionData(pe.PEHeaders.CorHeader.ResourcesDirectory.RelativeVirtualAddress);
                            BlobReader blobReader = resourceDirectory.GetReader((int)resource.Offset, resourceDirectory.Length - (int)resource.Offset);
                            uint length = blobReader.ReadUInt32();
                            result.PointerToResource = blobReader.CurrentPointer;

                            // Length check the size of the resource to ensure it fits in the PE file section, note, this is only safe as its in a checked region
                            if (length + sizeof(Int32) > blobReader.Length)
                                throw new BadImageFormatException();
                            result.SizeOfResource = length;
                        }
                    }
                    else
                    {
                        if (resource.Implementation.Kind == HandleKind.AssemblyFile)
                        {
                            // Get file name
                            result.ResourceLocation = default;
                            AssemblyFile file = ((AssemblyFileHandle)resource.Implementation).GetAssemblyFile(reader);
                            result.FileName = file.Name.GetString(reader);
                            if (file.ContainsMetadata)
                            {
                                EcmaModule module = (EcmaModule)Assembly.GetModule(result.FileName);
                                if (module == null)
                                    throw new BadImageFormatException(SR.Format(SR.ManifestResourceInfoReferencedBadModule, result.FileName));
                                result = module.GetInternalManifestResourceInfo(resourceName);
                            }
                        }
                        else if (resource.Implementation.Kind == HandleKind.AssemblyReference)
                        {
                            // Resolve assembly reference
                            result.ResourceLocation = ResourceLocation.ContainedInAnotherAssembly;
                            RoAssemblyName destinationAssemblyName = ((AssemblyReferenceHandle)resource.Implementation).ToRoAssemblyName(reader);
                            result.ReferencedAssembly = Loader.ResolveAssembly(destinationAssemblyName);
                        }
                    }
                }
            }

            return result;
        }
    }
}
