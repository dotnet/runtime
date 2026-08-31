// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection.PortableExecutable;

namespace ILCompiler.Reflection.ReadyToRun
{
    /// <summary>
    /// Interface for abstracting binary image reading across different formats (PE, MachO)
    /// </summary>
    public interface IBinaryImageReader
    {
        /// <summary>
        /// Gets the machine type of the binary image
        /// </summary>
        Machine Machine { get; }

        /// <summary>
        /// Gets the operating system of the binary image
        /// </summary>
        OperatingSystem OperatingSystem { get; }

        /// <summary>
        /// Gets the image base address
        /// </summary>
        ulong ImageBase { get; }

        /// <summary>
        /// Get the entire image content
        /// </summary>
        ImmutableArray<byte> GetEntireImage();

        /// <summary>
        /// Get the index in the image byte array corresponding to the RVA
        /// </summary>
        /// <param name="rva">The relative virtual address</param>
        int GetOffset(int rva);

        /// <summary>
        /// Try to get the ReadyToRun header RVA for this image.
        /// </summary>
        /// <param name="rva">RVA of the ReadyToRun header if available, 0 when not</param>
        /// <param name="isComposite">true when the reader represents a composite ReadyToRun image, false for regular R2R</param>
        /// <returns>true when the reader represents a ReadyToRun image (composite or regular), false otherwise</returns>
        bool TryGetReadyToRunHeader(out int rva, out bool isComposite);

        /// <summary>
        /// Creates standalone assembly metadata from the image's embedded metadata.
        /// </summary>
        /// <returns>Assembly metadata, or null if the image has no embedded metadata</returns>
        IAssemblyMetadata GetStandaloneAssemblyMetadata();

        /// <summary>
        /// Creates manifest assembly metadata from the R2R manifest
        /// </summary>
        /// <param name="manifestReader">Manifest metadata reader</param>
        /// <returns>Manifest assembly metadata</returns>
        IAssemblyMetadata GetManifestAssemblyMetadata(System.Reflection.Metadata.MetadataReader manifestReader);

        /// <summary>
        /// Write out image information using the specified writer
        /// </summary>
        /// <param name="writer">The writer to use</param>
        void DumpImageInformation(TextWriter writer);

        /// <summary>
        /// Gets the sections (name and size) of the binary image
        /// </summary>
        Dictionary<string, int> GetSections();

    }
}
