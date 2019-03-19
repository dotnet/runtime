// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection.Internal;

namespace System.Reflection.PortableExecutable
{
    public sealed class PEHeaderBuilder
    {
        // COFF:
        public Machine Machine { get; }
        public Characteristics ImageCharacteristics { get; }

        // PE:
        public byte MajorLinkerVersion { get; }
        public byte MinorLinkerVersion { get; }

        public ulong ImageBase { get; }
        public int SectionAlignment { get; }
        public int FileAlignment { get; }

        public ushort MajorOperatingSystemVersion { get; }
        public ushort MinorOperatingSystemVersion { get; }

        public ushort MajorImageVersion { get; }
        public ushort MinorImageVersion { get; }

        public ushort MajorSubsystemVersion { get; }
        public ushort MinorSubsystemVersion { get; }

        public Subsystem Subsystem { get; }
        public DllCharacteristics DllCharacteristics { get; }

        public ulong SizeOfStackReserve { get; }
        public ulong SizeOfStackCommit { get; }
        public ulong SizeOfHeapReserve { get; }
        public ulong SizeOfHeapCommit { get; }

        /// <summary>
        /// Creates PE header builder.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="fileAlignment"/> is not power of 2 between 512 and 64K, or
        /// <paramref name="sectionAlignment"/> not power of 2 or it's less than <paramref name="fileAlignment"/>.
        /// </exception>
        public PEHeaderBuilder(
            Machine machine = 0,
            int sectionAlignment = 0x2000,
            int fileAlignment = 0x200,
            ulong imageBase = 0x00400000,
            byte majorLinkerVersion = 0x30, // (what is ref.emit using?)
            byte minorLinkerVersion = 0,
            ushort majorOperatingSystemVersion = 4,
            ushort minorOperatingSystemVersion = 0,
            ushort majorImageVersion = 0,
            ushort minorImageVersion = 0,
            ushort majorSubsystemVersion = 4,
            ushort minorSubsystemVersion = 0,
            Subsystem subsystem = Subsystem.WindowsCui,
            DllCharacteristics dllCharacteristics = DllCharacteristics.DynamicBase | DllCharacteristics.NxCompatible | DllCharacteristics.NoSeh | DllCharacteristics.TerminalServerAware,
            Characteristics imageCharacteristics = Characteristics.Dll,
            ulong sizeOfStackReserve = 0x00100000,
            ulong sizeOfStackCommit = 0x1000,
            ulong sizeOfHeapReserve = 0x00100000,
            ulong sizeOfHeapCommit = 0x1000)
        {
            if (fileAlignment < 512 || fileAlignment > 64 * 1024 || BitArithmetic.CountBits(fileAlignment) != 1)
            {
                Throw.ArgumentOutOfRange(nameof(fileAlignment));
            }

            if (sectionAlignment < fileAlignment || BitArithmetic.CountBits(sectionAlignment) != 1)
            {
                Throw.ArgumentOutOfRange(nameof(sectionAlignment));
            }

            Machine = machine;
            SectionAlignment = sectionAlignment;
            FileAlignment = fileAlignment;
            ImageBase = imageBase;
            MajorLinkerVersion = majorLinkerVersion;
            MinorLinkerVersion = minorLinkerVersion;
            MajorOperatingSystemVersion = majorOperatingSystemVersion;
            MinorOperatingSystemVersion = minorOperatingSystemVersion;
            MajorImageVersion = majorImageVersion;
            MinorImageVersion = minorImageVersion;
            MajorSubsystemVersion = majorSubsystemVersion;
            MinorSubsystemVersion = minorSubsystemVersion;
            Subsystem = subsystem;
            DllCharacteristics = dllCharacteristics;
            ImageCharacteristics = imageCharacteristics;
            SizeOfStackReserve = sizeOfStackReserve;
            SizeOfStackCommit = sizeOfStackCommit;
            SizeOfHeapReserve = sizeOfHeapReserve;
            SizeOfHeapCommit = sizeOfHeapCommit;
        }

        public static PEHeaderBuilder CreateExecutableHeader()
        {
            return new PEHeaderBuilder(imageCharacteristics : Characteristics.ExecutableImage);
        }

        public static PEHeaderBuilder CreateLibraryHeader()
        {
            return new PEHeaderBuilder(imageCharacteristics: Characteristics.ExecutableImage | Characteristics.Dll);
        }

        internal bool Is32Bit => Machine != Machine.Amd64 && Machine != Machine.IA64 && Machine != Machine.Arm64;

        internal int ComputeSizeOfPEHeaders(int sectionCount) =>
            PEBuilder.DosHeaderSize +
            PEHeaders.PESignatureSize +
            CoffHeader.Size + 
            PEHeader.Size(Is32Bit) + 
            SectionHeader.Size * sectionCount;
    }
}