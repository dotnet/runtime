// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;

namespace ILCompiler.Reflection.ReadyToRun
{
    /// <summary>
    /// Reader for the metadata associated with the R2R manifest
    /// </summary>
    internal class ManifestAssemblyMetadata : IAssemblyMetadata
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

        public ManifestAssemblyMetadata(PEReader peReader, MetadataReader metadataReader)
        {
            _peReader = peReader;
            _metadataReader = metadataReader;
        }

        public PEReader ImageReader => _peReader;

        public MetadataReader MetadataReader => _metadataReader;

    }
}
