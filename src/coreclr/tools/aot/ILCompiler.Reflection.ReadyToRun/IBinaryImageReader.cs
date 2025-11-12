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
        /// Get the entire image content
        /// </summary>
        ImmutableArray<byte> GetEntireImage();

        /// <summary>
        /// Get the index in the image byte array corresponding to the RVA
        /// </summary>
        /// <param name="rva">The relative virtual address</param>
        int GetOffset(int rva);

        /// <summary>
        /// Check whether the file is a composite ReadyToRun image and returns the RVA of its ReadyToRun header if so.
        /// </summary>
        /// <param name="rva">RVA of the ReadyToRun header if available, 0 when not</param>
        /// <returns>true when the reader represents a composite ReadyToRun image, false otherwise</returns>
        bool TryGetCompositeReadyToRunHeader(out int rva);

        /// <summary>
        /// Write out image information using the specified writer
        /// </summary>
        /// <param name="writer">The writer to use</param>
        void DumpImageInformation(TextWriter writer);

        Dictionary<string, int> GetSections();

        /// <summary>
        /// Gets the machine type of the binary image
        /// </summary>
        Machine Machine { get; }

        /// <summary>
        /// Gets the operating system of the binary image
        /// </summary>
        OperatingSystem OperatingSystem { get; }

        /// <summary>
        /// Gets whether the image has metadata
        /// </summary>
        bool HasMetadata { get; }

        /// <summary>
        /// Gets the image base address
        /// </summary>
        ulong ImageBase { get; }

        /// <summary>
        /// Get whether the image is marked as an IL-only library
        /// </summary>
        bool IsILLibrary { get; }
    }
}
