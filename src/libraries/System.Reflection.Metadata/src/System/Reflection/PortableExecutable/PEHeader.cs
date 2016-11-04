// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection.Metadata;

namespace System.Reflection.PortableExecutable
{
    public sealed class PEHeader
    {
        #region Standard fields

        /// <summary>
        /// Identifies the format of the image file.
        /// </summary>
        public PEMagic Magic { get; }

        /// <summary>
        /// The linker major version number.
        /// </summary>
        public byte MajorLinkerVersion { get; }

        /// <summary>
        /// The linker minor version number.
        /// </summary>
        public byte MinorLinkerVersion { get; }

        /// <summary>
        /// The size of the code (text) section, or the sum of all code sections if there are multiple sections.
        /// </summary>
        public int SizeOfCode { get; }

        /// <summary>
        /// The size of the initialized data section, or the sum of all such sections if there are multiple data sections.
        /// </summary>
        public int SizeOfInitializedData { get; }

        /// <summary>
        /// The size of the uninitialized data section (BSS), or the sum of all such sections if there are multiple BSS sections.
        /// </summary>
        public int SizeOfUninitializedData { get; }

        /// <summary>
        /// The address of the entry point relative to the image base when the PE file is loaded into memory. 
        /// For program images, this is the starting address. For device drivers, this is the address of the initialization function.
        /// An entry point is optional for DLLs. When no entry point is present, this field must be zero.
        /// </summary>
        public int AddressOfEntryPoint { get; }

        /// <summary>
        /// The address that is relative to the image base of the beginning-of-code section when it is loaded into memory.
        /// </summary>
        public int BaseOfCode { get; }

        /// <summary>
        /// The address that is relative to the image base of the beginning-of-data section when it is loaded into memory.
        /// </summary>
        public int BaseOfData { get; }

        #endregion

        #region Windows Specific Fields

        /// <summary>
        /// The preferred address of the first byte of image when loaded into memory; 
        /// must be a multiple of 64K.
        /// </summary>
        public ulong ImageBase { get; }

        /// <summary>
        /// The alignment (in bytes) of sections when they are loaded into memory. It must be greater than or equal to <see cref="FileAlignment"/>. 
        /// The default is the page size for the architecture.
        /// </summary>
        public int SectionAlignment { get; }

        /// <summary>
        /// The alignment factor (in bytes) that is used to align the raw data of sections in the image file. 
        /// The value should be a power of 2 between 512 and 64K, inclusive. The default is 512. 
        /// If the <see cref="SectionAlignment"/> is less than the architecture's page size, 
        /// then <see cref="FileAlignment"/> must match <see cref="SectionAlignment"/>.
        /// </summary>
        public int FileAlignment { get; }

        /// <summary>
        /// The major version number of the required operating system.
        /// </summary>
        public ushort MajorOperatingSystemVersion { get; }

        /// <summary>
        /// The minor version number of the required operating system.
        /// </summary>
        public ushort MinorOperatingSystemVersion { get; }

        /// <summary>
        /// The major version number of the image.
        /// </summary>
        public ushort MajorImageVersion { get; }

        /// <summary>
        /// The minor version number of the image.
        /// </summary>
        public ushort MinorImageVersion { get; }

        /// <summary>
        /// The major version number of the subsystem.
        /// </summary>
        public ushort MajorSubsystemVersion { get; }

        /// <summary>
        /// The minor version number of the subsystem.
        /// </summary>
        public ushort MinorSubsystemVersion { get; }

        /// <summary>
        /// The size (in bytes) of the image, including all headers, as the image is loaded in memory. 
        /// It must be a multiple of <see cref="SectionAlignment"/>.
        /// </summary>
        public int SizeOfImage { get; }

        /// <summary>
        /// The combined size of an MS DOS stub, PE header, and section headers rounded up to a multiple of FileAlignment.
        /// </summary>
        public int SizeOfHeaders { get; }

        /// <summary>
        /// The image file checksum. 
        /// </summary>
        public uint CheckSum { get; }

        /// <summary>
        /// The subsystem that is required to run this image. 
        /// </summary>
        public Subsystem Subsystem { get; }

        public DllCharacteristics DllCharacteristics { get; }

        /// <summary>
        /// The size of the stack to reserve. Only <see cref="SizeOfStackCommit"/> is committed; 
        /// the rest is made available one page at a time until the reserve size is reached.
        /// </summary>
        public ulong SizeOfStackReserve { get; }

        /// <summary>
        /// The size of the stack to commit.
        /// </summary>
        public ulong SizeOfStackCommit { get; }

        /// <summary>
        /// The size of the local heap space to reserve. Only <see cref="SizeOfHeapCommit"/> is committed; 
        /// the rest is made available one page at a time until the reserve size is reached.
        /// </summary>
        public ulong SizeOfHeapReserve { get; }

        /// <summary>
        /// The size of the local heap space to commit.
        /// </summary>
        public ulong SizeOfHeapCommit { get; }

        /// <summary>
        /// The number of data-directory entries in the remainder of the <see cref="PEHeader"/>. Each describes a location and size.
        /// </summary>
        public int NumberOfRvaAndSizes { get; }

        #endregion

        #region Directory Entries 

        /// <remarks>
        /// Aka IMAGE_DIRECTORY_ENTRY_EXPORT.
        /// </remarks>
        public DirectoryEntry ExportTableDirectory { get; }

        /// <remarks>
        /// Aka IMAGE_DIRECTORY_ENTRY_IMPORT.
        /// </remarks>
        public DirectoryEntry ImportTableDirectory { get; }

        /// <remarks>
        /// Aka IMAGE_DIRECTORY_ENTRY_RESOURCE.
        /// </remarks>
        public DirectoryEntry ResourceTableDirectory { get; }

        /// <remarks>
        /// Aka IMAGE_DIRECTORY_ENTRY_EXCEPTION.
        /// </remarks>
        public DirectoryEntry ExceptionTableDirectory { get; }

        /// <summary>
        /// The Certificate Table entry points to a table of attribute certificates. 
        /// </summary>
        /// <remarks>
        /// These certificates are not loaded into memory as part of the image.
        /// As such, the first field of this entry, which is normally an RVA, is a file pointer instead.
        /// 
        /// Aka IMAGE_DIRECTORY_ENTRY_SECURITY.
        /// </remarks>
        public DirectoryEntry CertificateTableDirectory { get; }

        /// <remarks>
        /// Aka IMAGE_DIRECTORY_ENTRY_BASERELOC.
        /// </remarks>
        public DirectoryEntry BaseRelocationTableDirectory { get; }

        /// <remarks>
        /// Aka IMAGE_DIRECTORY_ENTRY_DEBUG.
        /// </remarks>
        public DirectoryEntry DebugTableDirectory { get; }

        /// <remarks>
        /// Aka IMAGE_DIRECTORY_ENTRY_COPYRIGHT or IMAGE_DIRECTORY_ENTRY_ARCHITECTURE.
        /// </remarks>
        public DirectoryEntry CopyrightTableDirectory { get; }

        /// <remarks>
        /// Aka IMAGE_DIRECTORY_ENTRY_GLOBALPTR.
        /// </remarks>
        public DirectoryEntry GlobalPointerTableDirectory { get; }

        /// <remarks>
        /// Aka IMAGE_DIRECTORY_ENTRY_TLS.
        /// </remarks>
        public DirectoryEntry ThreadLocalStorageTableDirectory { get; }

        /// <remarks>
        /// Aka IMAGE_DIRECTORY_ENTRY_LOAD_CONFIG.
        /// </remarks>
        public DirectoryEntry LoadConfigTableDirectory { get; }

        /// <remarks>
        /// Aka IMAGE_DIRECTORY_ENTRY_BOUND_IMPORT.
        /// </remarks>
        public DirectoryEntry BoundImportTableDirectory { get; }

        /// <remarks>
        /// Aka IMAGE_DIRECTORY_ENTRY_IAT.
        /// </remarks>
        public DirectoryEntry ImportAddressTableDirectory { get; }

        /// <remarks>
        /// Aka IMAGE_DIRECTORY_ENTRY_DELAY_IMPORT.
        /// </remarks>
        public DirectoryEntry DelayImportTableDirectory { get; }

        /// <remarks>
        /// Aka IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR.
        /// </remarks>
        public DirectoryEntry CorHeaderTableDirectory { get; }

        #endregion

        internal const int OffsetOfChecksum =
            sizeof(short) +                              // Magic
            sizeof(byte) +                               // MajorLinkerVersion
            sizeof(byte) +                               // MinorLinkerVersion
            sizeof(int) +                                // SizeOfCode
            sizeof(int) +                                // SizeOfInitializedData
            sizeof(int) +                                // SizeOfUninitializedData
            sizeof(int) +                                // AddressOfEntryPoint
            sizeof(int) +                                // BaseOfCode
            sizeof(long) +                               // PE32:  BaseOfData (int), ImageBase (int) 
                                                         // PE32+: ImageBase (long)
            sizeof(int) +                                // SectionAlignment
            sizeof(int) +                                // FileAlignment
            sizeof(short) +                              // MajorOperatingSystemVersion
            sizeof(short) +                              // MinorOperatingSystemVersion
            sizeof(short) +                              // MajorImageVersion
            sizeof(short) +                              // MinorImageVersion
            sizeof(short) +                              // MajorSubsystemVersion
            sizeof(short) +                              // MinorSubsystemVersion
            sizeof(int) +                                // Win32VersionValue
            sizeof(int) +                                // SizeOfImage
            sizeof(int);                                 // SizeOfHeaders

        internal static int Size(bool is32Bit) =>
            OffsetOfChecksum +
            sizeof(int) +                                // Checksum
            sizeof(short) +                              // Subsystem
            sizeof(short) +                              // DllCharacteristics
            4 * (is32Bit ? sizeof(int) : sizeof(long)) + // SizeOfStackReserve, SizeOfStackCommit, SizeOfHeapReserve, SizeOfHeapCommit
            sizeof(int) +                                // LoaderFlags
            sizeof(int) +                                // NumberOfRvaAndSizes
            16 * sizeof(long);                           // directory entries

        internal PEHeader(ref PEBinaryReader reader)
        {
            PEMagic magic = (PEMagic)reader.ReadUInt16();
            if (magic != PEMagic.PE32 && magic != PEMagic.PE32Plus)
            {
                throw new BadImageFormatException(SR.UnknownPEMagicValue);
            }

            Magic = magic;
            MajorLinkerVersion = reader.ReadByte();
            MinorLinkerVersion = reader.ReadByte();
            SizeOfCode = reader.ReadInt32();
            SizeOfInitializedData = reader.ReadInt32();
            SizeOfUninitializedData = reader.ReadInt32();
            AddressOfEntryPoint = reader.ReadInt32();
            BaseOfCode = reader.ReadInt32();

            if (magic == PEMagic.PE32Plus)
            {
                BaseOfData = 0; // not present
            }
            else
            {
                Debug.Assert(magic == PEMagic.PE32);
                BaseOfData = reader.ReadInt32();
            }

            if (magic == PEMagic.PE32Plus)
            {
                ImageBase = reader.ReadUInt64();
            }
            else
            {
                ImageBase = reader.ReadUInt32();
            }

            // NT additional fields:
            SectionAlignment = reader.ReadInt32();
            FileAlignment = reader.ReadInt32();
            MajorOperatingSystemVersion = reader.ReadUInt16();
            MinorOperatingSystemVersion = reader.ReadUInt16();
            MajorImageVersion = reader.ReadUInt16();
            MinorImageVersion = reader.ReadUInt16();
            MajorSubsystemVersion = reader.ReadUInt16();
            MinorSubsystemVersion = reader.ReadUInt16();

            // Win32VersionValue (reserved, should be 0)
            reader.ReadUInt32();

            SizeOfImage = reader.ReadInt32();
            SizeOfHeaders = reader.ReadInt32();
            CheckSum = reader.ReadUInt32();
            Subsystem = (Subsystem)reader.ReadUInt16();
            DllCharacteristics = (DllCharacteristics)reader.ReadUInt16();

            if (magic == PEMagic.PE32Plus)
            {
                SizeOfStackReserve = reader.ReadUInt64();
                SizeOfStackCommit = reader.ReadUInt64();
                SizeOfHeapReserve = reader.ReadUInt64();
                SizeOfHeapCommit = reader.ReadUInt64();
            }
            else
            {
                SizeOfStackReserve = reader.ReadUInt32();
                SizeOfStackCommit = reader.ReadUInt32();
                SizeOfHeapReserve = reader.ReadUInt32();
                SizeOfHeapCommit = reader.ReadUInt32();
            }

            // loader flags
            reader.ReadUInt32();

            NumberOfRvaAndSizes = reader.ReadInt32();

            // directory entries:
            ExportTableDirectory = new DirectoryEntry(ref reader);
            ImportTableDirectory = new DirectoryEntry(ref reader);
            ResourceTableDirectory = new DirectoryEntry(ref reader);
            ExceptionTableDirectory = new DirectoryEntry(ref reader);
            CertificateTableDirectory = new DirectoryEntry(ref reader);
            BaseRelocationTableDirectory = new DirectoryEntry(ref reader);
            DebugTableDirectory = new DirectoryEntry(ref reader);
            CopyrightTableDirectory = new DirectoryEntry(ref reader);
            GlobalPointerTableDirectory = new DirectoryEntry(ref reader);
            ThreadLocalStorageTableDirectory = new DirectoryEntry(ref reader);
            LoadConfigTableDirectory = new DirectoryEntry(ref reader);
            BoundImportTableDirectory = new DirectoryEntry(ref reader);
            ImportAddressTableDirectory = new DirectoryEntry(ref reader);
            DelayImportTableDirectory = new DirectoryEntry(ref reader);
            CorHeaderTableDirectory = new DirectoryEntry(ref reader);

            // ReservedDirectory (should be 0, 0)
            new DirectoryEntry(ref reader);
        }
    }
}
