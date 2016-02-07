// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection.PortableExecutable;

#if SRM
namespace System.Reflection.PortableExecutable
#else
namespace Roslyn.Reflection.PortableExecutable
#endif
{
#if SRM && FUTURE
    public
#endif
    sealed class PEDirectoriesBuilder
    {
        public int AddressOfEntryPoint { get; set; }

        public DirectoryEntry ExportTable { get; set; }
        public DirectoryEntry ImportTable { get; set; }
        public DirectoryEntry ResourceTable { get; set; }
        public DirectoryEntry ExceptionTable { get; set; }
        public DirectoryEntry CertificateTable { get; set; }
        public DirectoryEntry BaseRelocationTable { get; set; }
        public DirectoryEntry DebugTable { get; set; }
        public DirectoryEntry CopyrightTable { get; set; }
        public DirectoryEntry GlobalPointerTable { get; set; }
        public DirectoryEntry ThreadLocalStorageTable { get; set; }
        public DirectoryEntry LoadConfigTable { get; set; }
        public DirectoryEntry BoundImportTable { get; set; }
        public DirectoryEntry ImportAddressTable { get; set; }
        public DirectoryEntry DelayImportTable { get; set; }
        public DirectoryEntry CorHeaderTable { get; set; }
    }
}
