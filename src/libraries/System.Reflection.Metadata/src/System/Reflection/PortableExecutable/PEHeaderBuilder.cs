// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            return new PEHeaderBuilder(imageCharacteristics: Characteristics.Dll);
        }

        internal bool Is32Bit => Machine != Machine.Amd64 && Machine != Machine.IA64;

        internal int ComputeSizeOfPeHeaders(int sectionCount)
        {
            // TODO: constants
            int sizeOfPeHeaders = 128 + 4 + 20 + 224 + 40 * sectionCount;
            if (!Is32Bit)
            {
                sizeOfPeHeaders += 16;
            }

            return sizeOfPeHeaders;
        }
    }
}