// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NET.HostModel;

/// <summary>
/// Offsets and constants of PE file. see https://learn.microsoft.com/windows/win32/debug/pe-format
/// </summary>
internal static class PEOffsets
{
    private const int PESignatureSize = sizeof(int);
    private const int CoffHeaderSize = 20;
    public const ushort DosImageSignature = 0x5A4D;
    public const int PEHeaderSize = PESignatureSize + CoffHeaderSize;
    public const int OneSectionHeaderSize = 40;
    public const int DataDirectoryEntrySize = 8;

    public const int ResourceTableDataDirectoryIndex = 2;

    public static class DosStub
    {
        public const int PESignatureOffset = 0x3c;
    }

    /// offsets relative to Lfanew, which is pointer to first byte in header
    public static class PEHeader
    {
        public const int NumberOfSections = PESignatureSize + 2;

        private const int OptionalHeaderBase = PESignatureSize + CoffHeaderSize;
        public const int InitializedDataSize = OptionalHeaderBase + 8;
        public const int SizeOfImage = OptionalHeaderBase + 56;
        public const int Subsystem = OptionalHeaderBase + 68;
        public const int PE64DataDirectories = OptionalHeaderBase + 112;
        public const int PE32DataDirectories = OptionalHeaderBase + 96;
    }

    /// offsets relative to each section header
    public static class SectionHeader
    {
        public const int VirtualSize = 8;
        public const int VirtualAddress = 12;
        public const int RawSize = 16;
        public const int RawPointer = 20;
        public const int RelocationsPointer = 24;
        public const int LineNumbersPointer = 28;
        public const int NumberOfRelocations = 32;
        public const int NumberOfLineNumbers = 34;
        public const int SectionCharacteristics = 36;
    }

    public static class DataDirectoryEntry
    {
        public const int VirtualAddressOffset = 0;
        public const int SizeOffset = 4;
    }

    public enum Subsystem : ushort
    {
        WindowsGui = 2,
        WindowsCui = 3,
    }
}
