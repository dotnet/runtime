// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.PortableExecutable
{
    public sealed class PEDirectoriesBuilder
    {
        public int AddressOfEntryPoint { get; set; }

        /// <remarks>
        /// Aka IMAGE_DIRECTORY_ENTRY_EXPORT.
        /// </remarks>
        public DirectoryEntry ExportTable { get; set; }

        /// <remarks>
        /// Aka IMAGE_DIRECTORY_ENTRY_IMPORT.
        /// </remarks>
        public DirectoryEntry ImportTable { get; set; }

        /// <remarks>
        /// Aka IMAGE_DIRECTORY_ENTRY_RESOURCE.
        /// </remarks>
        public DirectoryEntry ResourceTable { get; set; }

        /// <remarks>
        /// Aka IMAGE_DIRECTORY_ENTRY_EXCEPTION.
        /// </remarks>
        public DirectoryEntry ExceptionTable { get; set; }

        /// <remarks>
        /// Aka IMAGE_DIRECTORY_ENTRY_BASERELOC.
        /// </remarks>
        public DirectoryEntry BaseRelocationTable { get; set; }

        /// <remarks>
        /// Aka IMAGE_DIRECTORY_ENTRY_DEBUG.
        /// </remarks>
        public DirectoryEntry DebugTable { get; set; }

        /// <remarks>
        /// Aka IMAGE_DIRECTORY_ENTRY_COPYRIGHT or IMAGE_DIRECTORY_ENTRY_ARCHITECTURE.
        /// </remarks>
        public DirectoryEntry CopyrightTable { get; set; }

        /// <remarks>
        /// Aka IMAGE_DIRECTORY_ENTRY_GLOBALPTR.
        /// </remarks>
        public DirectoryEntry GlobalPointerTable { get; set; }

        /// <remarks>
        /// Aka IMAGE_DIRECTORY_ENTRY_TLS.
        /// </remarks>
        public DirectoryEntry ThreadLocalStorageTable { get; set; }

        /// <remarks>
        /// Aka IMAGE_DIRECTORY_ENTRY_LOAD_CONFIG.
        /// </remarks>
        public DirectoryEntry LoadConfigTable { get; set; }

        /// <remarks>
        /// Aka IMAGE_DIRECTORY_ENTRY_BOUND_IMPORT.
        /// </remarks>
        public DirectoryEntry BoundImportTable { get; set; }

        /// <remarks>
        /// Aka IMAGE_DIRECTORY_ENTRY_IAT.
        /// </remarks>
        public DirectoryEntry ImportAddressTable { get; set; }

        /// <remarks>
        /// Aka IMAGE_DIRECTORY_ENTRY_DELAY_IMPORT.
        /// </remarks>
        public DirectoryEntry DelayImportTable { get; set; }

        /// <remarks>
        /// Aka IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR.
        /// </remarks>
        public DirectoryEntry CorHeaderTable { get; set; }
    }
}
