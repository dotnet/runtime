// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Internal.JitInterface;
using Internal.TypeSystem;

using static SuperPmi.SpmiRecordsHelper;
using static SuperPmi.SpmiUtil;

namespace SuperPmi.Replay;

// Compares two compilations of a method. After simple fixups, the main artifacts of the compile results (the
// code, GC info, EH info, debug info, and so on) are compared for equality; to be fast, the fixups and the
// definitions of "equality" are minimal (for example, the GC info is compared byte by byte).
//
// The asm comparison is done by the CoreDisTools library, which compares the code instruction by instruction.
// Literal operands that differ are run through heuristics that try to normalize them, by reinterpreting them as
// deltas from known base addresses; for example, a common case is a pointer into the read-only data section.
//
// Most of the logic is architecture independent, with these exceptions: the disassembler is created for the
// target architecture; the heuristics have some architecture-specific assumptions; and the code is fixed up with
// the relocations recorded during the compilation, which in theory is architecture independent, but the runtime
// implementations may have subtle differences.
internal sealed unsafe class NearDiffer : IDisposable
{
    // coredistools.h
    private enum TargetArch
    {
        Target_Host,
        Target_X86,
        Target_X64,
        Target_Thumb,
        Target_Arm64,
        Target_LoongArch64,
        Target_RiscV64,
        Target_Wasm32,
    }

    // The offset comparator's view of the two code blocks.
    private struct DiffData
    {
        public byte* block1;
        public nuint blocksize1;
        public byte* datablock1;
        public nuint datablockSize1;
        public nuint originalBlock1;
        public nuint originalDataBlock1;
        public nuint otherCodeBlock1;
        public nuint otherCodeBlockSize1;

        public byte* block2;
        public nuint blocksize2;
        public byte* datablock2;
        public nuint datablockSize2;
        public nuint originalBlock2;
        public nuint originalDataBlock2;
        public nuint otherCodeBlock2;
        public nuint otherCodeBlockSize2;
    }

    // The compile results being compared, for the offset comparator.
    private static CompileResult s_cr1;
    private static CompileResult s_cr2;

    private static delegate* unmanaged[Cdecl]<TargetArch, IntPtr, IntPtr, IntPtr> g_PtrNewDiffer; // For temporary backwards-compatibility with older coredistools
    private static delegate* unmanaged[Cdecl]<TargetArch, IntPtr, IntPtr, IntPtr, IntPtr> g_PtrNewDiffer2;
    private static delegate* unmanaged[Cdecl]<IntPtr, void> g_PtrFinishDiff;
    private static delegate* unmanaged[Cdecl]<IntPtr, void*, byte*, byte*, nuint, byte*, byte*, nuint, byte> g_PtrNearDiffCodeBlocks;
    private static delegate* unmanaged[Cdecl]<IntPtr, byte*, byte*, nuint, byte*, byte*, nuint, void> g_PtrDumpDiffBlocks;

    private readonly TargetArchitecture? targetArchitecture;
    private IntPtr corAsmDiff;

    // 'targetArchitecture' is the one given with -target, if any.
    public NearDiffer(TargetArchitecture? targetArchitecture) => this.targetArchitecture = targetArchitecture;

    public void Dispose()
    {
        if (corAsmDiff != IntPtr.Zero)
            g_PtrFinishDiff(corAsmDiff);
    }

    // Loads CoreDisTools, which is next to superpmi.
    public bool InitAsmDiff()
    {
        string libraryName = OperatingSystem.IsWindows() ? "coredistools.dll" : OperatingSystem.IsMacOS() ? "libcoredistools.dylib" : "libcoredistools.so";
        if (!NativeLibrary.TryLoad(Path.Combine(AppContext.BaseDirectory, libraryName), out IntPtr hCoreDisToolsLib))
        {
            Logger.LogError($"LoadLibrary({libraryName}) failed (0x0000007e)");
            return false;
        }

        IntPtr GetExport(string name, bool required = true)
        {
            if (!NativeLibrary.TryGetExport(hCoreDisToolsLib, name, out IntPtr address))
            {
                if (required)
                    Logger.LogError($"GetProcAddress '{name}' failed (0x0000007f)");
                else
                    Logger.LogWarning($"GetProcAddress '{name}' failed (0x0000007f)"); // Just a warning; fall back to NewDiffer
            }
            return address;
        }

        if ((g_PtrNewDiffer = (delegate* unmanaged[Cdecl]<TargetArch, IntPtr, IntPtr, IntPtr>)GetExport("NewDiffer")) == null)
            return false;
        g_PtrNewDiffer2 = (delegate* unmanaged[Cdecl]<TargetArch, IntPtr, IntPtr, IntPtr, IntPtr>)GetExport("NewDiffer2", required: false);
        if ((g_PtrFinishDiff = (delegate* unmanaged[Cdecl]<IntPtr, void>)GetExport("FinishDiff")) == null ||
            (g_PtrNearDiffCodeBlocks = (delegate* unmanaged[Cdecl]<IntPtr, void*, byte*, byte*, nuint, byte*, byte*, nuint, byte>)GetExport("NearDiffCodeBlocks")) == null ||
            (g_PtrDumpDiffBlocks = (delegate* unmanaged[Cdecl]<IntPtr, byte*, byte*, nuint, byte*, byte*, nuint, void>)GetExport("DumpDiffBlocks")) == null)
        {
            return false;
        }

        TargetArch coreDisTargetArchitecture = targetArchitecture switch
        {
            TargetArchitecture.X64 => TargetArch.Target_X64,
            TargetArchitecture.X86 => TargetArch.Target_X86,
            TargetArchitecture.ARM => TargetArch.Target_Thumb,
            TargetArchitecture.ARM64 => TargetArch.Target_Arm64,
            TargetArchitecture.LoongArch64 => TargetArch.Target_LoongArch64,
            TargetArchitecture.RiscV64 => TargetArch.Target_RiscV64,
            // Requires coredistools >= 1.7.0 (Target_Wasm32 and framed entry points). NearDiffCodeBlocks and
            // DumpDiffBlocks route to the Wasm framing-aware implementations inside coredistools.
            TargetArchitecture.Wasm32 => TargetArch.Target_Wasm32,
            _ => TargetArch.Target_Host,
        };

        // Only arm32 uses this feature currently.
        IntPtr munger = targetArchitecture == TargetArchitecture.ARM
            ? (IntPtr)(delegate* unmanaged[Cdecl]<void*, nuint, nuint, ulong*, ulong*, uint*, uint*, byte>)&CoreDisMungeOffsetsCallback
            : IntPtr.Zero;

        IntPtr printers = SpmiJitInterface.GetCoreDisToolsPrinters(&CoreDisToolsLog);
        IntPtr comparator = (IntPtr)(delegate* unmanaged[Cdecl]<void*, nuint, nuint, ulong, ulong, byte>)&CoreDisCompareOffsetsCallback;
        corAsmDiff = g_PtrNewDiffer2 == null
            ? g_PtrNewDiffer(coreDisTargetArchitecture, printers, comparator)
            : g_PtrNewDiffer2(coreDisTargetArchitecture, printers, comparator, munger);
        return true;
    }

    [UnmanagedCallersOnly]
    private static void CoreDisToolsLog(int level, byte* message)
    {
        string text = Marshal.PtrToStringUTF8((IntPtr)message);
        if (level == 0)
            Logger.LogError(text);
        else if (level == 1)
            Logger.LogWarning(text);
        else
            Logger.LogVerbose(text);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte CoreDisCompareOffsetsCallback(void* payload, nuint blockOffset, nuint instrLen, ulong offset1, ulong offset2)
        => compareOffsets((DiffData*)payload, blockOffset, instrLen, offset1, offset2) ? (byte)1 : (byte)0;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte CoreDisMungeOffsetsCallback(void* payload, nuint blockOffset, nuint instrLen, ulong* offset1, ulong* offset2, uint* skip1, uint* skip2)
        => mungeOffsets((DiffData*)payload, blockOffset, offset1, offset2, skip1, skip2) ? (byte)1 : (byte)0;

    // On Thumb-2, movw/movt pairs that generate a 32-bit address are treated as a single instruction.
    //
    // Something similar might be needed for mov/movk/movk/movk on Arm64, but the number of movk can differ
    // with the data, so zeroing out the constants doesn't work well; the disassembler would need a callback to
    // skip the instructions.
    private static bool mungeOffsets(DiffData* data, nuint blockOffset, ulong* offset1, ulong* offset2, uint* skip1, uint* skip2)
    {
        if (GetSpmiTargetArchitecture() != TargetArchitecture.ARM)
            return false;

        static bool FindMovwMovtAddress(byte* block, nuint blocksize, nuint blockOffset, out uint reg, out ulong addr)
        {
            var iaddr = (uint*)(block + blockOffset);
            var iaddrEnd = (uint*)(block + blocksize);
            addr = 0;
            reg = 0;
            if (iaddr < iaddrEnd && GetArm32MovwConstant(iaddr, out reg, out uint conW) &&
                iaddr + 1 < iaddrEnd && GetArm32MovtConstant(iaddr + 1, out uint regT, out uint conT) && reg == regT)
            {
                addr = conW + ((ulong)conT << 16);
                return true;
            }
            return false;
        }

        if (FindMovwMovtAddress(data->block1, data->blocksize1, blockOffset, out uint reg1, out ulong addr1) &&
            FindMovwMovtAddress(data->block2, data->blocksize2, blockOffset, out uint reg2, out ulong addr2) && reg1 == reg2)
        {
            // Use the constructed constant instead, and skip the movt instruction.
            *offset1 = addr1;
            *offset2 = addr2;
            *skip1 = *skip2 = 1;
            return true;
        }
        return false;
    }

    // Determines whether two syntactically different constants are semantically equivalent, with heuristics.
    private static bool compareOffsets(DiffData* data, nuint blockOffset, nuint instrLen, ulong offset1, ulong offset2)
    {
        // The trivial case.
        if (offset1 == offset2)
            return true;

        // For wasm32, the recorded relocations are the source of truth: handles materialize as i32.const
        // immediates with padded ULEB128/SLEB128 placeholders, which the JIT records separately in a reloc
        // table.
        if (GetSpmiTargetArchitecture() == TargetArchitecture.Wasm32)
            return compareOffsetsWasm(data, blockOffset, instrLen, offset1, offset2);

        nuint ip1 = data->originalBlock1 + blockOffset;
        nuint ip2 = data->originalBlock2 + blockOffset;
        nuint ipRelOffset1 = ip1 + instrLen + (nuint)offset1;
        nuint ipRelOffset2 = ip2 + instrLen + (nuint)offset2;

        // A call to a flat address, the most common case. Only the lower 32 bits are compared, the best that
        // can be done with the current replay form.
        if ((uint)ipRelOffset1 == (uint)ipRelOffset2)
            return true;

        // An offset into the read-only section (for example, loading a float value), that fits in the section.
        nuint roOffset1a = (nuint)offset1 - data->originalDataBlock1;
        nuint roOffset2a = (nuint)offset2 - data->originalDataBlock2;
        if (roOffset1a == roOffset2a && roOffset1a < data->datablockSize1)
            return true;

        // An IP-relative offset into the read-only data section.
        nuint roOffset1b = ipRelOffset1 - data->originalDataBlock1;
        nuint roOffset2b = ipRelOffset2 - data->originalDataBlock2;
        if (roOffset1b == roOffset2b && roOffset1b < data->datablockSize1)
            return true;

        // An address in our own code section.
        nuint gOffset1a = (nuint)offset1 - data->originalBlock1;
        nuint gOffset2a = (nuint)offset2 - data->originalBlock2;
        if (gOffset1a == gOffset2a && gOffset1a < data->blocksize1)
            return true;

        // An address in the other code block.
        nuint gOffset1b = (nuint)offset1 - data->otherCodeBlock1;
        nuint gOffset2b = (nuint)offset2 - data->otherCodeBlock2;
        if (gOffset1b == gOffset2b && gOffset1b < data->otherCodeBlockSize1)
            return true;

        // An offset into the hot code block from the cold code block (why?). It would be nice to check that it
        // fits in the other code block.
        nuint ocOffset1 = ipRelOffset1 - data->otherCodeBlock1;
        nuint ocOffset2 = ipRelOffset2 - data->otherCodeBlock2;
        if (ocOffset1 == ocOffset2)
            return true;

        // It's rather odd to look up artifacts of cr1 in cr2; one would expect cr1.map(artifact1) to be compared
        // with cr2.map(artifact2). This is left as it is for now, since it seems to work.

        // The VSD calling case.
        if (s_cr2.CallTargetTypes.GetIndex(ipRelOffset1 - 8) != -1)
            return true;

        // The x86 VSD calling cases.
        if (s_cr2.CallTargetTypes.GetIndex((nuint)offset1 - 4) != -1 || s_cr2.CallTargetTypes.GetIndex((nuint)offset2) != -1)
            return true;

        return false;
    }

    // For wasm32, the integer immediates that materialize JIT handles (function indices, type indices, memory
    // addresses, globals) are 5-byte ULEB128/SLEB128 placeholders (80 80 80 80 00), which the JIT records
    // separately in a reloc table. For un-prefixed forms like i32.const and call, the placeholder is at the
    // opcode byte + 1; for un-prefixed memory ops (i32.load, i32.store, ...), the layout is
    // <opcode> <align>:u32 <reloc-offset>:u32, so it's at the opcode byte + 2. Either way, both JITs write the
    // same placeholder bytes, so the byte-by-byte comparison succeeds at reloc sites without any patching. So
    // an immediate mismatch here is one of:
    //
    //   1. A hard-coded non-reloc literal that differs: a real codegen change.
    //   2. A reloc site with relocs on both sides but different immediates (for example, if a future change
    //      patches the placeholder into real bytes): equivalent when the reloc kinds match, and the targets are
    //      equal after the addlDelta correction.
    //   3. A reloc on only one side: genuinely asymmetric codegen.
    //
    // 'blockOffset' is the opcode byte's offset in the framed buffer, per the coredistools Wasm32 contract. The
    // recorded reloc location is the payload byte, which can be at the opcode byte + 1, + 2, or further for
    // prefixed opcodes; so the entire immediate window [blockOffset + 1, blockOffset + instrLen) is searched.
    private static bool compareOffsetsWasm(DiffData* data, nuint blockOffset, nuint instrLen, ulong offset1, ulong offset2)
    {
        if (offset1 == offset2)
            return true;

        // A single-byte opcode (without an immediate) shouldn't get here at all; treat it as a real mismatch.
        nuint windowSize = instrLen > 1 ? instrLen - 1 : 0;
        if (windowSize == 0)
            return false;

        // Multiple relocs in a single instruction aren't emitted today, so the first one is used.
        if (!s_cr1.findRelocationInRange(data->originalBlock1, blockOffset + 1, windowSize, out Agnostic_RecordRelocation reloc1) ||
            !s_cr2.findRelocationInRange(data->originalBlock2, blockOffset + 1, windowSize, out Agnostic_RecordRelocation reloc2))
        {
            // A non-reloc immediate mismatch (case 1), or an asymmetric reloc (case 3).
            return false;
        }

        // Different reloc kinds at the same site are a real codegen change.
        if (reloc1.fRelocType != reloc2.fRelocType)
            return false;

        // Different targets are a different handle picked for the same source: a real mismatch for now. A future
        // revision could plug in handle-equivalence logic, like the remapping in compareOffsets.
        return reloc1.target + (ulong)(long)(int)reloc1.addlDelta == reloc2.target + (ulong)(long)(int)reloc2.addlDelta;
    }

    // Compares two code sections for syntactic equality: the core of the asm diffing.
    private bool compareCodeSection(byte* block1, uint blocksize1, byte* datablock1, uint datablockSize1, void* originalBlock1,
        void* originalDataBlock1, void* otherCodeBlock1, uint otherCodeBlockSize1, byte* block2, uint blocksize2, byte* datablock2,
        uint datablockSize2, void* originalBlock2, void* originalDataBlock2, void* otherCodeBlock2, uint otherCodeBlockSize2)
    {
        var data = new DiffData
        {
            block1 = block1,
            blocksize1 = blocksize1,
            datablock1 = datablock1,
            datablockSize1 = datablockSize1,
            originalBlock1 = (nuint)originalBlock1,
            originalDataBlock1 = (nuint)originalDataBlock1,
            otherCodeBlock1 = (nuint)otherCodeBlock1,
            otherCodeBlockSize1 = otherCodeBlockSize1,
            block2 = block2,
            blocksize2 = blocksize2,
            datablock2 = datablock2,
            datablockSize2 = datablockSize2,
            originalBlock2 = (nuint)originalBlock2,
            originalDataBlock2 = (nuint)originalDataBlock2,
            otherCodeBlock2 = (nuint)otherCodeBlock2,
            otherCodeBlockSize2 = otherCodeBlockSize2,
        };

        bool areSame = g_PtrNearDiffCodeBlocks(corAsmDiff, &data, (byte*)originalBlock1, block1, blocksize1, (byte*)originalBlock2, block2, blocksize2) != 0;
        if (!areSame)
            g_PtrDumpDiffBlocks(corAsmDiff, (byte*)originalBlock1, block1, blocksize1, (byte*)originalBlock2, block2, blocksize2);
        return areSame;
    }

    private static bool compareReadOnlyDataBlock(uint blocksize1, uint blocksize2)
    {
        // No RO data.
        if (blocksize1 == 0 && blocksize2 == 0)
            return true;

        if (blocksize1 != blocksize2)
        {
            Logger.LogVerbose($"compareReadOnlyDataBlock found non-matching sizes {blocksize1} {blocksize2}");
            return false;
        }

        // TODO-Cleanup: the values in the data block seem to wobble. A good near comparison for them needs
        // further investigation.
        return true;
    }

    private static bool compareEHInfo(CompileResult cr1, CompileResult cr2)
    {
        uint cEHSize_1 = cr1.repSetEHcount();
        uint cEHSize_2 = cr2.repSetEHcount();

        // No exception handling.
        if (cEHSize_1 == 0 && cEHSize_2 == 0)
            return true;

        if (cEHSize_1 != cEHSize_2)
        {
            Logger.LogVerbose($"compareEHInfo found non-matching sizes {cEHSize_1} {cEHSize_2}");
            return false;
        }

        for (uint i = 0; i < cEHSize_1; i++)
        {
            Agnostic_CORINFO_EH_CLAUSE eh1 = cr1.repSetEHinfo(i);
            Agnostic_CORINFO_EH_CLAUSE eh2 = cr2.repSetEHinfo(i);
            if (eh1.Flags != eh2.Flags)
            {
                Logger.LogVerbose($"EH flags don't match {eh1.Flags} != {eh2.Flags}");
                return false;
            }
            if (eh1.TryOffset != eh2.TryOffset || eh1.TryLength != eh2.TryLength)
            {
                Logger.LogVerbose($"EH try information don't match, offset: {eh1.TryOffset} {eh2.TryOffset}, length: {eh1.TryLength} {eh2.TryLength}");
                return false;
            }
            if (eh1.HandlerOffset != eh2.HandlerOffset || eh1.HandlerLength != eh2.HandlerLength)
            {
                Logger.LogVerbose($"EH handler information don't match, offset: {eh1.HandlerOffset} {eh2.HandlerOffset}, length: {eh1.HandlerLength} {eh2.HandlerLength}");
                return false;
            }
            if (eh1.ClassToken != eh2.ClassToken)
            {
                Logger.LogVerbose($"EH class tokens don't match {eh1.ClassToken}!={eh2.ClassToken}");
                return false;
            }
        }

        return true;
    }

    private static bool compareGCInfo(CompileResult cr1, CompileResult cr2)
    {
        cr1.repAllocGCInfo(out nuint gcInfo1Size, out void* gcInfo1);
        cr2.repAllocGCInfo(out nuint gcInfo2Size, out void* gcInfo2);

        if (gcInfo1Size != gcInfo2Size)
        {
            Logger.LogVerbose($"Reported GCInfo sizes don't match: {(uint)gcInfo1Size} != {(uint)gcInfo2Size}");
            return false;
        }

        if (!new ReadOnlySpan<byte>(gcInfo1, (int)gcInfo1Size).SequenceEqual(new ReadOnlySpan<byte>(gcInfo2, (int)gcInfo2Size)))
        {
            Logger.LogVerbose("GCInfo doesn't match.");
            return false;
        }

        return true;
    }

    private static bool compareVars(MethodContext mc, CompileResult cr1, CompileResult cr2)
    {
        CORINFO_METHOD_INFO info = mc.repCompileMethod(out _, out _);

        bool set1 = cr1.repSetVars(out CORINFO_METHOD_STRUCT_* ftn_1, out uint cVars_1, out _);
        bool set2 = cr2.repSetVars(out CORINFO_METHOD_STRUCT_* ftn_2, out uint cVars_2, out _);
        if (!set1 && !set2)
            return true; // There are no vars for either.
        if (set1 != set2)
        {
            Logger.LogVerbose("missing matching vars sets");
            return false;
        }

        // No vars.
        if (cVars_1 == 0 && cVars_2 == 0)
            return true;

        if (ftn_1 != ftn_2)
        {
            // This situation is interesting to find out about.
            if (Debugger.IsAttached)
                Debugger.Break();
            Logger.LogVerbose($"compareVars found non-matching CORINFO_METHOD_HANDLE 0x{CastHandle(ftn_1):x} 0x{CastHandle(ftn_2):x}");
            return false;
        }
        if (ftn_1 != info.ftn)
        {
            Logger.LogVerbose($"compareVars found issues with the CORINFO_METHOD_HANDLE 0x{CastHandle(ftn_1):x} 0x{CastHandle(info.ftn):x}");
            return false;
        }

        if (cVars_1 != cVars_2)
        {
            Logger.LogVerbose($"compareVars found non-matching var count {cVars_1} {cVars_2}");
            return false;
        }

        // TODO-Cleanup: the values in the NativeVarInfo array seem to wobble. A good near comparison for them
        // needs further investigation.
        return true;
    }

    private static bool compareBoundaries(MethodContext mc, CompileResult cr1, CompileResult cr2)
    {
        bool set1 = cr1.repSetBoundaries(out CORINFO_METHOD_STRUCT_* ftn_1, out uint cMap_1, out OffsetMapping* map_1);
        bool set2 = cr2.repSetBoundaries(out CORINFO_METHOD_STRUCT_* ftn_2, out uint cMap_2, out OffsetMapping* map_2);
        if (!set1 && !set2)
            return true; // There are no boundaries for either.
        if (set1 != set2)
        {
            Logger.LogVerbose("missing matching boundary sets");
            return false;
        }

        if (ftn_1 != ftn_2)
        {
            Logger.LogVerbose($"compareBoundaries found non-matching CORINFO_METHOD_HANDLE 0x{CastHandle(ftn_1):x} 0x{CastHandle(ftn_2):x}");
            return false;
        }

        // No maps.
        if (cMap_1 == 0 && cMap_2 == 0)
            return true;

        if (cMap_1 != cMap_2)
        {
            Logger.LogVerbose($"compareBoundaries found non-matching var count {cMap_1} {cMap_2}");
            return false;
        }

        for (uint i = 0; i < cMap_1; i++)
        {
            if (map_1[i].ilOffset != map_2[i].ilOffset)
            {
                Logger.LogVerbose($"compareBoundaries found non-matching ilOffset {map_1[i].ilOffset} {map_2[i].ilOffset} for map: {i}");
                return false;
            }
            if (map_1[i].nativeOffset != map_2[i].nativeOffset)
            {
                Logger.LogVerbose($"compareBoundaries found non-matching nativeOffset {map_1[i].nativeOffset} {map_2[i].nativeOffset} for map: {i}");
                return false;
            }
            if (map_1[i].source != map_2[i].source)
            {
                Logger.LogVerbose($"compareBoundaries found non-matching source {(uint)map_1[i].source} {(uint)map_2[i].source} for map: {i}");
                return false;
            }
        }

        return true;
    }

    // A compiled method's code and data blocks, and the addresses they had when compiled.
    private struct Blocks
    {
        public uint hotCodeSize, coldCodeSize, roDataSize1, roDataSize2, xcptnsCount;
        public byte* hotCodeBlock, coldCodeBlock, roDataBlock;
        public void* origHotCodeBlock, origColdCodeBlock, origRoDataBlock1, origRoDataBlock2;

        public Blocks(CompileResult cr)
        {
            Agnostic_AllocMemDetails value = cr.repAllocMem(out hotCodeBlock, out coldCodeBlock, out roDataBlock);
            hotCodeSize = value.hotCodeSize;
            coldCodeSize = value.coldCodeSize;
            roDataSize1 = value.roDataSize;
            xcptnsCount = value.xcptnsCount;
            origHotCodeBlock = (void*)value.hotCodeBlock;
            origColdCodeBlock = (void*)value.coldCodeBlock;
            origRoDataBlock1 = (void*)value.roDataBlock;

            // On Arm64 the constant pool is appended to the method's code, so the hot code size is the sum of
            // their sizes. Adjust the sizes and the RO data block pointers.
            if (GetSpmiTargetArchitecture() == TargetArchitecture.ARM64 && hotCodeSize > 0)
            {
                cr.repCompileMethod(out _, out uint nativeSizeOfCode, out _);
                roDataSize2 = roDataSize1;
                roDataSize1 = hotCodeSize - nativeSizeOfCode;
                roDataBlock = hotCodeBlock + nativeSizeOfCode;
                origRoDataBlock2 = origRoDataBlock1;
                origRoDataBlock1 = (byte*)origHotCodeBlock + nativeSizeOfCode;
                hotCodeSize = nativeSizeOfCode;
            }
        }

        public readonly string Describe(int n) => $"HCS{n} {hotCodeSize} CCS{n} {coldCodeSize} RDS{n} {roDataSize1} xcpnt{n} {xcptnsCount} HCB " +
            $"0x{(ulong)hotCodeBlock:x} CCB 0x{(ulong)coldCodeBlock:x} RDB 0x{(ulong)roDataBlock:x} ohcb 0x{CastHandle(origHotCodeBlock):x} occb " +
            $"0x{CastHandle(origColdCodeBlock):x} odb 0x{CastHandle(origRoDataBlock1):x}";

        // Fixes up the code with the recorded relocations, for the original base addresses.
        public readonly void ApplyRelocs(MethodContext mc, CompileResult cr)
        {
            var rc = new RelocContext
            {
                mc = mc,
                hotCodeAddress = (nuint)hotCodeBlock,
                hotCodeSize = hotCodeSize,
                coldCodeAddress = (nuint)coldCodeBlock,
                coldCodeSize = coldCodeSize,
                roDataAddress = (nuint)roDataBlock,
                roDataSize1 = roDataSize1,
                roDataSize2 = roDataSize2,
                originalHotCodeAddress = (nuint)origHotCodeBlock,
                originalColdCodeAddress = (nuint)origColdCodeBlock,
                originalRoDataAddress1 = (nuint)origRoDataBlock1,
                originalRoDataAddress2 = (nuint)origRoDataBlock2,
            };
            cr.applyRelocs(ref rc, hotCodeBlock, hotCodeSize, origHotCodeBlock);
            cr.applyRelocs(ref rc, coldCodeBlock, coldCodeSize, origColdCodeBlock);
            cr.applyRelocs(ref rc, roDataBlock, roDataSize1, origRoDataBlock1);
        }
    }

    // Compares two compilations of a method, after fixing up their code with the relocations recorded during
    // the compilations, for the original base addresses. Returns whether they're the same.
    public bool compare(MethodContext mc, CompileResult cr1, CompileResult cr2)
    {
        var b1 = new Blocks(cr1);
        var b2 = new Blocks(cr2);

        Logger.LogDebug(b1.Describe(1));
        Logger.LogDebug(b2.Describe(2));

        b1.ApplyRelocs(mc, cr1);
        b2.ApplyRelocs(mc, cr2);

        s_cr1 = cr1;
        s_cr2 = cr2;

        return compareCodeSection(b1.hotCodeBlock, b1.hotCodeSize, b1.roDataBlock, b1.roDataSize1, b1.origHotCodeBlock, b1.origRoDataBlock1,
                   b1.origColdCodeBlock, b1.coldCodeSize, b2.hotCodeBlock, b2.hotCodeSize, b2.roDataBlock, b2.roDataSize1, b2.origHotCodeBlock,
                   b2.origRoDataBlock1, b2.origColdCodeBlock, b2.coldCodeSize) &&
               compareCodeSection(b1.coldCodeBlock, b1.coldCodeSize, b1.roDataBlock, b1.roDataSize1, b1.origColdCodeBlock, b1.origRoDataBlock1,
                   b1.origHotCodeBlock, b1.hotCodeSize, b2.coldCodeBlock, b2.coldCodeSize, b2.roDataBlock, b2.roDataSize1, b2.origColdCodeBlock,
                   b2.origRoDataBlock1, b2.origHotCodeBlock, b2.hotCodeSize) &&
               compareReadOnlyDataBlock(b1.roDataSize1, b2.roDataSize1) &&
               compareEHInfo(cr1, cr2) &&
               compareGCInfo(cr1, cr2) &&
               compareVars(mc, cr1, cr2) &&
               compareBoundaries(mc, cr1, cr2);
    }
}
