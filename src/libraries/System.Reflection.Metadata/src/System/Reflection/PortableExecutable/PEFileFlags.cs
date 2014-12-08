// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Reflection.PortableExecutable
{
    public enum Characteristics : ushort
    {
        RelocsStripped = 0x0001,         // Relocation info stripped from file.
        ExecutableImage = 0x0002,        // File is executable  (i.e. no unresolved external references).
        LineNumsStripped = 0x0004,       // Line numbers stripped from file.
        LocalSymsStripped = 0x0008,      // Local symbols stripped from file.
        AggressiveWSTrim = 0x0010,       // Aggressively trim working set
        LargeAddressAware = 0x0020,      // App can handle >2gb addresses
        BytesReversedLo = 0x0080,        // Bytes of machine word are reversed.
        Bit32Machine = 0x0100,           // 32 bit word machine.
        DebugStripped = 0x0200,          // Debugging info stripped from file in .DBG file
        RemovableRunFromSwap = 0x0400,   // If Image is on removable media, copy and run from the swap file.
        NetRunFromSwap = 0x0800,         // If Image is on Net, copy and run from the swap file.
        System = 0x1000,                 // System File.
        Dll = 0x2000,                    // File is a DLL.
        UpSystemOnly = 0x4000,           // File should only be run on a UP machine
        BytesReversedHi = 0x8000,        // Bytes of machine word are reversed.
    }

    public enum PEMagic : ushort
    {
        PE32 = 0x010B,
        PE32Plus = 0x020B,
    }

    public enum Subsystem : ushort
    {
        Unknown = 0,                // Unknown subsystem.
        Native = 1,                 // Image doesn't require a subsystem.
        WindowsGui = 2,             // Image runs in the Windows GUI subsystem.
        WindowsCui = 3,             // Image runs in the Windows character subsystem.
        OS2Cui = 5,                 // image runs in the OS/2 character subsystem.
        PosixCui = 7,               // image runs in the Posix character subsystem.
        NativeWindows = 8,          // image is a native Win9x driver.
        WindowsCEGui = 9,           // Image runs in the Windows CE subsystem.
        EfiApplication = 10,        // Extensible Firmware Interface (EFI) application.
        EfiBootServiceDriver = 11,  // EFI driver with boot services.
        EfiRuntimeDriver = 12,      // EFI driver with run-time services.
        EfiRom = 13,                // EFI ROM image.
        Xbox = 14,
    }

    [Flags]
    public enum DllCharacteristics : ushort
    {
        ProcessInit = 0x0001,   // Reserved.
        ProcessTerm = 0x0002,   // Reserved.
        ThreadInit = 0x0004,    // Reserved.
        ThreadTerm = 0x0008,    // Reserved.
        DynamicBase = 0x0040,   //
        NxCompatible = 0x0100,  //
        NoIsolation = 0x0200,   // Image understands isolation and doesn't want it
        NoSeh = 0x0400,         // Image does not use SEH.  No SE handler may reside in this image
        NoBind = 0x0800,        // Do not bind this image.
        AppContainer = 0x1000,  // The image must run inside an AppContainer
        WdmDriver = 0x2000,    // Driver uses WDM model
        //                     0x4000     // Reserved.
        TerminalServerAware = 0x8000,
    }

    [Flags]
    public enum SectionCharacteristics : uint
    {
        TypeReg = 0x00000000,               // Reserved.
        TypeDSect = 0x00000001,             // Reserved.
        TypeNoLoad = 0x00000002,            // Reserved.
        TypeGroup = 0x00000004,             // Reserved.
        TypeNoPad = 0x00000008,             // Reserved.
        TypeCopy = 0x00000010,              // Reserved.

        ContainsCode = 0x00000020,               // Section contains code.
        ContainsInitializedData = 0x00000040,    // Section contains initialized data.
        ContainsUninitializedData = 0x00000080,  // Section contains uninitialized data.

        LinkerOther = 0x00000100,            // Reserved.
        LinkerInfo = 0x00000200,             // Section contains comments or some other type of information.
        TypeOver = 0x00000400,            // Reserved.
        LinkerRemove = 0x00000800,           // Section contents will not become part of image.
        LinkerComdat = 0x00001000,           // Section contents comdat.
        //                               0x00002000  // Reserved.
        MemProtected = 0x00004000,
        NoDeferSpecExc = 0x00004000,   // Reset speculative exceptions handling bits in the TLB entries for this section.
        GPRel = 0x00008000,               // Section content can be accessed relative to GP
        MemFardata = 0x00008000,
        MemSysheap = 0x00010000,
        MemPurgeable = 0x00020000,
        Mem16Bit = 0x00020000,
        MemLocked = 0x00040000,
        MemPreload = 0x00080000,

        Align1Bytes = 0x00100000,     //
        Align2Bytes = 0x00200000,     //
        Align4Bytes = 0x00300000,     //
        Align8Bytes = 0x00400000,     //
        Align16Bytes = 0x00500000,    // Default alignment if no others are specified.
        Align32Bytes = 0x00600000,    //
        Align64Bytes = 0x00700000,    //
        Align128Bytes = 0x00800000,   //
        Align256Bytes = 0x00900000,   //
        Align512Bytes = 0x00A00000,   //
        Align1024Bytes = 0x00B00000,  //
        Align2048Bytes = 0x00C00000,  //
        Align4096Bytes = 0x00D00000,  //
        Align8192Bytes = 0x00E00000,  //
        // Unused                     0x00F00000
        AlignMask = 0x00F00000,

        LinkerNRelocOvfl = 0x01000000,   // Section contains extended relocations.
        MemDiscardable = 0x02000000,     // Section can be discarded.
        MemNotCached = 0x04000000,       // Section is not cachable.
        MemNotPaged = 0x08000000,        // Section is not pageable.
        MemShared = 0x10000000,          // Section is shareable.
        MemExecute = 0x20000000,         // Section is executable.
        MemRead = 0x40000000,            // Section is readable.
        MemWrite = 0x80000000,           // Section is writeable.
    }
}
