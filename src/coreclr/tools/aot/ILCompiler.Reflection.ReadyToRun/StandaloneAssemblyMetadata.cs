// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace ILCompiler.Reflection.ReadyToRun
{
    /// <summary>
    /// Metadata access interface for standalone assemblies represented by MSIL PE files.
    /// </summary>
    public class StandaloneAssemblyMetadata : IAssemblyMetadata
    {
        /// <summary>
        /// Reader representing the MSIL assembly file.
        /// </summary>
        private readonly PEReader _peReader;

        /// <summary>
        /// Metadata reader for the MSIL assembly. We create one upfront to avoid going
        /// through the GetMetadataReader() helper and constructing a new instance every time.
        /// </summary>
        private readonly MetadataReader _metadataReader;

        public StandaloneAssemblyMetadata(PEReader peReader)
        {
            _peReader = peReader;
            _metadataReader = _peReader.GetMetadataReader();
        }

        public PEReader ImageReader => _peReader;

        public MetadataReader MetadataReader => _metadataReader;
    }
}
