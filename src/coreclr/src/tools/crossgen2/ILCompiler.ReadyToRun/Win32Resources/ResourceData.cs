// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.TypeSystem.Ecma;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace ILCompiler.Win32Resources
{
    /// <summary>
    /// Resource abstraction to allow examination
    /// of a PE file that contains resources.
    /// </summary>
    public unsafe partial class ResourceData
    {
        BlobReader _resourceDataBlob;
        PEReader _peFile;

        /// <summary>
        /// Initialize a ResourceData instance from a PE file
        /// </summary>
        /// <param name="ecmaModule"></param>
        public ResourceData(EcmaModule ecmaModule)
        {
            var ecmaData = ecmaModule.PEReader.GetEntireImage().GetContent();
            _peFile = ecmaModule.PEReader;

            DirectoryEntry resourceDirectory = _peFile.PEHeaders.PEHeader.ResourceTableDirectory;
            if (resourceDirectory.Size != 0)
            {
                _resourceDataBlob = ecmaModule.PEReader.GetSectionData(resourceDirectory.RelativeVirtualAddress).GetReader(0, resourceDirectory.Size);
                ReadResourceData();
            }
        }

        /// <summary>
        /// Find a resource in the resource data
        /// </summary>
        public byte[] FindResource(string name, string type, ushort language)
        {
            return FindResourceInternal(name, type, language);
        }

        /// <summary>
        /// Find a resource in the resource data
        /// </summary>
        public byte[] FindResource(ushort name, string type, ushort language)
        {
            return FindResourceInternal(name, type, language);
        }

        /// <summary>
        /// Find a resource in the resource data
        /// </summary>
        public byte[] FindResource(string name, ushort type, ushort language)
        {
            return FindResourceInternal(name, type, language);
        }

        /// <summary>
        /// Find a resource in the resource data
        /// </summary>
        public byte[] FindResource(ushort name, ushort type, ushort language)
        {
            return FindResourceInternal(name, type, language);
        }
    }
}
