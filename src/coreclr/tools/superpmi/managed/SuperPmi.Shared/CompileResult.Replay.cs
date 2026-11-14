// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Internal.JitInterface;
using Internal.TypeSystem;

using static SuperPmi.SpmiRecordsHelper;
using static SuperPmi.SpmiUtil;

namespace SuperPmi;

// The data needed to process relocations.
public struct RelocContext
{
    public MethodContext mc;
    public nuint hotCodeAddress;
    public nuint hotCodeSize;
    public nuint coldCodeAddress;
    public nuint coldCodeSize;
    public nuint roDataAddress;
    public nuint roDataSize1;
    public nuint roDataSize2;
    public nuint originalHotCodeAddress;
    public nuint originalColdCodeAddress;
    public nuint originalRoDataAddress1;
    public nuint originalRoDataAddress2;
}

public sealed unsafe partial class CompileResult
{
    // Set it to true to use CallUtils.GetRecordedCallSiteInfo.
    private static readonly bool recordCallSitesWithoutSig = false;

    public double secondsToCompile;
    public ulong clockCyclesToCompile;

    // The method full name reported by the JIT (not available with a release JIT), and the compilation tier.
    public string MethodFullName;
    public string TieringName;
    public readonly JitMetrics Metrics = new();

    // Not persisted.
    public readonly LightWeightMap<ulong, uint> CallTargetTypes = new();

    private PinnedArena memoryTracker;

    private nuint allocMemHotCodeSize, allocMemColdCodeSize, allocMemRoDataSize;
    private uint allocMemXcptnsCount;
    private void* allocMemHotCodeBlock, allocMemColdCodeBlock, allocMemRoDataBlock;

    private nuint allocGCInfoSize;
    private void* allocGCInfoRetval;

    // Memory that lives as long as the compile result.
    public PinnedArena MemoryTracker => memoryTracker ??= new PinnedArena();

    public void* allocateMemory(nuint sizeInBytes, nuint alignment = 16) => MemoryTracker.Allocate(sizeInBytes, alignment);

    public void recAssert(string assertText)
    {
        AssertLog ??= new DenseLightWeightMap<uint>();
        AssertLog.Append(AssertLog.AddBuffer(CStringBytes(assertText)));
    }

    public string repAssert() => AssertLog == null || AssertLog.GetCount() == 0 ? null : AssertLog.GetString(AssertLog.Get(0));

    // Calls aren't logged anymore, but the (empty) log still marks the compile result as non-empty.
    public void AddCall(string name) => CallLog ??= new DenseLightWeightMap<uint>();

    public void dumpToConsole()
    {
        Logger.Out.Write("***************************************** CompileResult\n");
        dumpMaps();
        Logger.Out.Write("-----------------------------------------\n");
    }

    // The EE allocates these blocks (and the exception blocks) in a single linear region, with an
    // 8-byte aligned RO block.
    public void recAllocMem(nuint hotCodeSize, nuint coldCodeSize, nuint roDataSize, uint xcptnsCount, void* hotCodeBlock, void* coldCodeBlock, void* roDataBlock)
    {
        // Grab the values, to scrape the real answers in the capture method.
        allocMemHotCodeSize = hotCodeSize;
        allocMemColdCodeSize = coldCodeSize;
        allocMemRoDataSize = roDataSize;
        allocMemXcptnsCount = xcptnsCount;
        allocMemHotCodeBlock = hotCodeBlock;
        allocMemColdCodeBlock = coldCodeBlock;
        allocMemRoDataBlock = roDataBlock;
    }

    public void recAllocMemCapture()
    {
        AllocMem ??= new LightWeightMap<uint, Agnostic_AllocMemDetails>();
        AllocMem.Add(0, new Agnostic_AllocMemDetails
        {
            hotCodeSize = (uint)allocMemHotCodeSize,
            coldCodeSize = (uint)allocMemColdCodeSize,
            roDataSize = (uint)allocMemRoDataSize,
            xcptnsCount = allocMemXcptnsCount,
            hotCodeBlock_offset = AllocMem.AddBuffer(Bytes(allocMemHotCodeBlock, allocMemHotCodeSize)),
            coldCodeBlock_offset = AllocMem.AddBuffer(Bytes(allocMemColdCodeBlock, allocMemColdCodeSize)),
            roDataBlock_offset = AllocMem.AddBuffer(Bytes(allocMemRoDataBlock, allocMemRoDataSize)),
            hotCodeBlock = CastHandle(allocMemHotCodeBlock),
            coldCodeBlock = CastHandle(allocMemColdCodeBlock),
            roDataBlock = CastHandle(allocMemRoDataBlock),
        });
    }

    // Code can't be placed at the addresses used during recording, so the blocks point into the AllocMem
    // map, and the original addresses are returned separately.
    public Agnostic_AllocMemDetails repAllocMem(out byte* hotCodeBlock, out byte* coldCodeBlock, out byte* roDataBlock)
    {
        Agnostic_AllocMemDetails value = AllocMem.Get(0);
        hotCodeBlock = value.hotCodeSize > 0 ? AllocMem.GetBufferPointer(value.hotCodeBlock_offset) : null;
        coldCodeBlock = value.coldCodeSize > 0 ? AllocMem.GetBufferPointer(value.coldCodeBlock_offset) : null;
        roDataBlock = value.roDataSize > 0 ? AllocMem.GetBufferPointer(value.roDataBlock_offset) : null;
        return value;
    }

    public void recSetBoundaries(CORINFO_METHOD_STRUCT_* ftn, uint cMap, OffsetMapping* pMap)
    {
        SetBoundaries ??= new LightWeightMap<uint, Agnostic_SetBoundaries>();
        SetBoundaries.Add(0, new Agnostic_SetBoundaries
        {
            ftn = CastHandle(ftn),
            cMap = cMap,
            pMap_offset = SetBoundaries.AddBuffer(Bytes(pMap, (nuint)sizeof(OffsetMapping) * cMap)),
        });
    }

    public bool repSetBoundaries(out CORINFO_METHOD_STRUCT_* ftn, out uint cMap, out OffsetMapping* pMap)
    {
        if (SetBoundaries == null || SetBoundaries.GetCount() == 0)
        {
            ftn = (CORINFO_METHOD_STRUCT_*)-1;
            cMap = uint.MaxValue;
            pMap = null;
            return false;
        }

        Agnostic_SetBoundaries value = SetBoundaries.Get(0);
        ftn = (CORINFO_METHOD_STRUCT_*)value.ftn;
        cMap = value.cMap;
        pMap = (OffsetMapping*)SetBoundaries.GetBufferPointer(value.pMap_offset);
        return true;
    }

    public void recSetVars(CORINFO_METHOD_STRUCT_* ftn, uint cVars, NativeVarInfo* vars)
    {
        SetVars ??= new LightWeightMap<uint, Agnostic_SetVars>();
        SetVars.Add(0, new Agnostic_SetVars
        {
            ftn = CastHandle(ftn),
            cVars = cVars,
            vars_offset = SetVars.AddBuffer(Bytes(vars, (nuint)sizeof(NativeVarInfo) * cVars)), // not deep enough: vlt memory is pointer sized
        });
    }

    public bool repSetVars(out CORINFO_METHOD_STRUCT_* ftn, out uint cVars, out NativeVarInfo* vars)
    {
        if (SetVars == null || SetVars.GetCount() == 0)
        {
            ftn = (CORINFO_METHOD_STRUCT_*)-1;
            cVars = uint.MaxValue;
            vars = null;
            return false;
        }

        Agnostic_SetVars value = SetVars.Get(0);
        ftn = (CORINFO_METHOD_STRUCT_*)value.ftn;
        cVars = value.cVars;
        vars = (NativeVarInfo*)SetVars.GetBufferPointer(value.vars_offset);
        return true;
    }

    public void recSetPatchpointInfo(PatchpointInfo* patchpointInfo)
    {
        SetPatchpointInfo ??= new LightWeightMap<uint, Agnostic_SetPatchpointInfo>();
        SetPatchpointInfo.Add(0, new Agnostic_SetPatchpointInfo
        {
            index = SetPatchpointInfo.AddBuffer(Bytes(patchpointInfo, PatchpointInfoSize(patchpointInfo))),
        });
    }

    public bool repSetPatchpointInfo(out PatchpointInfo* patchpointInfo)
    {
        if (SetPatchpointInfo == null || SetPatchpointInfo.GetCount() == 0)
        {
            patchpointInfo = null;
            return false;
        }

        patchpointInfo = (PatchpointInfo*)SetPatchpointInfo.GetBufferPointer(SetPatchpointInfo.Get(0).index);
        return true;
    }

    public void recAllocGCInfo(nuint size, void* retval)
    {
        allocGCInfoSize = size;
        allocGCInfoRetval = retval;
    }

    public void recAllocGCInfoCapture()
    {
        AllocGCInfo ??= new LightWeightMap<uint, Agnostic_AllocGCInfo>();
        AllocGCInfo.Add(0, new Agnostic_AllocGCInfo
        {
            size = allocGCInfoSize,
            retval_offset = AllocGCInfo.AddBuffer(Bytes(allocGCInfoRetval, (uint)allocGCInfoSize)),
        });
    }

    public void repAllocGCInfo(out nuint size, out void* retval)
    {
        Agnostic_AllocGCInfo value = AllocGCInfo.Get(0);
        size = (nuint)value.size;
        retval = size > 0 ? AllocGCInfo.GetBufferPointer(value.retval_offset) : null;
    }

    public void recCompileMethod(byte* nativeEntry, uint nativeSizeOfCode, CorJitResult result)
    {
        CompileMethod ??= new LightWeightMap<uint, Agnostic_CompileMethodResults>();
        CompileMethod.Add(0, new Agnostic_CompileMethodResults
        {
            nativeEntry = CastHandle(nativeEntry),
            nativeSizeOfCode = nativeSizeOfCode,
            CorJitResult = (uint)result,
        });
    }

    public void repCompileMethod(out byte* nativeEntry, out uint nativeSizeOfCode, out CorJitResult result)
    {
        Agnostic_CompileMethodResults value = CompileMethod.Get(0);
        nativeEntry = (byte*)value.nativeEntry;
        nativeSizeOfCode = value.nativeSizeOfCode;
        result = (CorJitResult)value.CorJitResult;
    }

    // Message logging is disabled.
    public void recMessageLog(string message)
    {
    }

    public void recReportInliningDecision(CORINFO_METHOD_STRUCT_* inlinerHnd, CORINFO_METHOD_STRUCT_* inlineeHnd, CorInfoInline inlineResult, byte* reason)
    {
        ReportInliningDecision ??= new DenseLightWeightMap<Agnostic_ReportInliningDecision>();
        ReportInliningDecision.Append(new Agnostic_ReportInliningDecision
        {
            inlinerHnd = CastHandle(inlinerHnd),
            inlineeHnd = CastHandle(inlineeHnd),
            inlineResult = (uint)inlineResult,
            reason_offset = reason != null ? ReportInliningDecision.AddBuffer(CString(reason)) : uint.MaxValue,
        });
    }

    public CorInfoInline repReportInliningDecision(CORINFO_METHOD_STRUCT_* inlinerHnd, CORINFO_METHOD_STRUCT_* inlineeHnd)
    {
        CorInfoInline result = CorInfoInline.INLINE_FAIL;
        if (ReportInliningDecision != null)
        {
            foreach (Agnostic_ReportInliningDecision item in ReportInliningDecision.GetRawItems())
            {
                if (item.inlinerHnd == CastHandle(inlinerHnd) && item.inlineeHnd == CastHandle(inlineeHnd) && item.inlineResult == (uint)CorInfoInline.INLINE_PASS)
                    result = CorInfoInline.INLINE_PASS;
            }
        }
        return result;
    }

    public void recSetEHcount(uint cEH)
    {
        SetEHcount ??= new LightWeightMap<uint, uint>();
        SetEHcount.Add(0, cEH);
    }

    public uint repSetEHcount()
    {
        SetEHcount ??= new LightWeightMap<uint, uint>();
        int index = SetEHcount.GetIndex(0);
        return index < 0 ? 0 : SetEHcount.GetItem(index);
    }

    public void recSetEHinfo(uint EHnumber, ref CORINFO_EH_CLAUSE clause)
    {
        SetEHinfo ??= new LightWeightMap<uint, Agnostic_CORINFO_EH_CLAUSE>();
        SetEHinfo.Add(EHnumber, new Agnostic_CORINFO_EH_CLAUSE
        {
            Flags = (uint)clause.Flags,
            TryOffset = clause.TryOffset,
            TryLength = clause.TryLength,
            HandlerOffset = clause.HandlerOffset,
            HandlerLength = clause.HandlerLength,
            ClassToken = clause.ClassTokenOrOffset,
        });
    }

    public Agnostic_CORINFO_EH_CLAUSE repSetEHinfo(uint EHnumber) => SetEHinfo.Get(EHnumber);

    public void recSetMethodAttribs(CORINFO_METHOD_STRUCT_* ftn, CorInfoMethodRuntimeFlags attribs)
    {
        SetMethodAttribs ??= new LightWeightMap<ulong, uint>();
        int index = SetMethodAttribs.GetIndex(CastHandle(ftn));
        if (index == -1)
            SetMethodAttribs.Add(CastHandle(ftn), (uint)attribs);
        else
            SetMethodAttribs.Update(index, SetMethodAttribs.GetItem(index) | (uint)attribs);
    }

    public CorInfoMethodRuntimeFlags repSetMethodAttribs(CORINFO_METHOD_STRUCT_* ftn)
    {
        int index = SetMethodAttribs?.GetIndex(CastHandle(ftn)) ?? -1;
        return index == -1 ? 0 : (CorInfoMethodRuntimeFlags)SetMethodAttribs.GetItem(index);
    }

    public void recReportTailCallDecision(CORINFO_METHOD_STRUCT_* callerHnd, CORINFO_METHOD_STRUCT_* calleeHnd, bool fIsTailPrefix,
        CorInfoTailCall tailCallResult, byte* reason)
    {
        ReportTailCallDecision ??= new DenseLightWeightMap<Agnostic_ReportTailCallDecision>();
        ReportTailCallDecision.Append(new Agnostic_ReportTailCallDecision
        {
            callerHnd = CastHandle(callerHnd),
            calleeHnd = CastHandle(calleeHnd),
            fIsTailPrefix = fIsTailPrefix ? 1u : 0u,
            tailCallResult = (uint)tailCallResult,
            reason_index = reason != null ? ReportTailCallDecision.AddBuffer(CString(reason)) : uint.MaxValue,
        });
    }

    public void recReportFatalError(CorJitResult result)
    {
        ReportFatalError ??= new DenseLightWeightMap<uint>();
        ReportFatalError.Append((uint)result);
    }

    public void recRecordRelocation(void* location, void* target, CorInfoReloc fRelocType, int addlDelta)
        => repRecordRelocation(location, target, fRelocType, addlDelta);

    public void repRecordRelocation(void* location, void* target, CorInfoReloc fRelocType, int addlDelta)
    {
        RecordRelocation ??= new DenseLightWeightMap<Agnostic_RecordRelocation>();
        RecordRelocation.Append(new Agnostic_RecordRelocation
        {
            location = CastHandle(location),
            target = CastHandle(target),
            fRelocType = (uint)fRelocType,
            addlDelta = (uint)addlDelta,
        });
    }

    // A replay replaces the recorded CompileResult. Usually the collection has also been "thinned" (by
    // "mcs -removeDup -thin"), which removes the CompileResult, so the original addresses are lost.
    //
    // The generated code may still depend on the original relative addresses. For example, a relocation
    // based on the result of GetFunctionEntryPoint may have fit in a rel32 delta, or, if it didn't fit,
    // the VM may have returned a jump stub in the JIT code section that does fit.
    //
    // Ideally, the original addresses and relocations would be kept even if the CompileResult is deleted;
    // then a new relocation with the same target as an original one could reuse the original delta.
    //
    // For now, a rel32 relocation that doesn't fit gets an address immediately after the current section
    // (using originalAddr), assuming a jump stub was needed. Multiple calls to potentially different
    // functions can share the address, since even if they had used different ones, no textual diffs would
    // appear: most of the textual call names are "hackishMethodName".
    public void applyRelocs(ref RelocContext rc, byte* block1, uint blocksize1, void* originalAddr)
    {
        if (RecordRelocation == null || blocksize1 == 0)
            return;

        // For wasm32, the JIT writes padded ULEB128/SLEB128 placeholders (PADDED_RELOC_SIZE bytes of
        // `80 80 ... 80 00` that decode to zero) in place of the real handle at every reloc site. Both
        // baseline and diff JITs write the same placeholder bytes, so the byte-by-byte comparison succeeds
        // at reloc sites without any patching here. The near differ validates reloc-target equivalence via
        // compareOffsetsWasm when the surrounding immediate bytes happen to differ.
        if (GetSpmiTargetArchitecture() == TargetArchitecture.Wasm32)
            return;

        nuint section_begin = (nuint)block1;
        nuint section_end = (nuint)block1 + blocksize1; // address is exclusive

        Logger.LogDebug($"applyRelocs block [0x{(ulong)block1:x},0x{(ulong)(block1 + blocksize1):x}) block size {blocksize1}, orig addr " +
            $"0x{CastHandle(originalAddr):x}");

        ReadOnlySpan<Agnostic_RecordRelocation> relocations = RecordRelocation.GetRawItems();
        for (int i = 0; i < relocations.Length; i++)
        {
            Agnostic_RecordRelocation tmp = relocations[i];

            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.Out.Write("  ");
                dmpRecordRelocation((uint)i, tmp);
                Logger.Out.Write("\n");
            }

            TargetArchitecture targetArch = GetSpmiTargetArchitecture();
            var relocType = (CorInfoReloc)tmp.fRelocType;
            bool wasRelocHandled = false;
            ulong fixupLocation = tmp.location;
            nuint address = section_begin + (nuint)fixupLocation - (nuint)originalAddr;
            bool inSection = section_begin <= address && address < section_end; // A reloc for our section?

            // Do platform specific relocations first.

            if (targetArch is TargetArchitecture.X86 or TargetArchitecture.ARM && relocType == CorInfoReloc.DIRECT)
            {
                if (inSection)
                {
                    Logger.LogDebug($"    fixupLoc-{fixupLocation:X16} (@0x{(ulong)address:x}) : {*(uint*)address:X8} => {(uint)tmp.target:X8}");
                    *(uint*)address = (uint)tmp.target;
                }
                wasRelocHandled = true;
            }

            if (targetArch == TargetArchitecture.ARM)
            {
                switch (relocType)
                {
                    case CorInfoReloc.ARM32_THUMB_MOV32:
                    case CorInfoReloc.ARM32_THUMB_MOV32_PCREL:
                        if (inSection)
                            PutThumb2Mov32((ushort*)address, (uint)(int)(tmp.target - fixupLocation));
                        wasRelocHandled = true;
                        break;

                    case CorInfoReloc.ARM32_THUMB_BRANCH24:
                        if (inSection)
                        {
                            // Like the arm64 ARM64_BRANCH26 and x64 RELATIVE32 handling, hardcode the bottom bits
                            // of the target into the instruction, so the encoding doesn't depend on where SuperPMI
                            // allocated the code buffer. Otherwise a BL whose target is out of the +-16MB range for
                            // one of the two compared blocks would get a buffer-dependent placeholder, producing
                            // spurious asm diffs.
                            ulong target = tmp.target + (ulong)(long)(int)tmp.addlDelta;
                            PutThumb2BlRel24((ushort*)address, (int)(target & 0x00FFFFFE));
                        }
                        wasRelocHandled = true;
                        break;
                }
            }

            if (targetArch == TargetArchitecture.ARM64)
            {
                ulong target = tmp.target + (ulong)(long)(int)tmp.addlDelta;
                if (relocType is CorInfoReloc.ARM64_PAGEBASE_REL21 or CorInfoReloc.ARM64_LIN_TLSDESC_ADR_PAGE21 or
                    CorInfoReloc.ARM64_PAGEOFFSET_12A or CorInfoReloc.ARM64_PAGEOFFSET_12L)
                {
                    // Is the target in the RO data section?
                    if (rc.originalRoDataAddress2 <= (nuint)target && (nuint)target < rc.originalRoDataAddress2 + rc.roDataSize2)
                    {
                        nuint ro_section_offset = (nuint)target - rc.originalRoDataAddress2;
                        if (rc.originalHotCodeAddress <= (nuint)fixupLocation && (nuint)fixupLocation < rc.originalHotCodeAddress + rc.hotCodeSize)
                        {
                            // The fixup location is in the hot section.
                            target = rc.originalHotCodeAddress + rc.hotCodeSize + ro_section_offset - fixupLocation;
                        }
                        else if (rc.originalColdCodeAddress <= (nuint)fixupLocation && (nuint)fixupLocation < rc.originalColdCodeAddress + rc.coldCodeSize)
                        {
                            // The fixup location is in the cold section.
                            target = rc.originalColdCodeAddress + rc.coldCodeSize + ro_section_offset - fixupLocation;
                        }
                    }
                }

                switch (relocType)
                {
                    case CorInfoReloc.ARM64_BRANCH26: // 26 bit offset << 2 & sign ext, for B and BL
                        // Like x64's IMAGE_REL_BASED_REL32 handling, hardcode the bottom bits of the target
                        // into the instruction.
                        if (inSection)
                            PutArm64Rel28((uint*)address, (int)target);
                        wasRelocHandled = true;
                        break;

                    case CorInfoReloc.ARM64_PAGEBASE_REL21: // ADRP 21 bit PC-relative page address
                    case CorInfoReloc.ARM64_LIN_TLSDESC_ADR_PAGE21: // ADRP 21 bit for TLSDesc
                        if (inSection)
                        {
                            // The page delta is deliberately (target page - target page): the ADRP immediates of the two
                            // compilations being diffed then don't differ.
                            long targetPage = (long)target & unchecked((long)0xFFFFFFFFFFFFF000);
                            long pageDelta = targetPage - targetPage;
                            PutArm64Rel21((uint*)address, (int)(pageDelta >> 12) & 0x1FFFFF);
                        }
                        wasRelocHandled = true;
                        break;

                    case CorInfoReloc.ARM64_PAGEOFFSET_12A: // ADD 12 bit page offset
                        if (inSection)
                            PutArm64Rel12((uint*)address, (int)(nuint)target & 0xFFF);
                        wasRelocHandled = true;
                        break;

                    case CorInfoReloc.ARM64_PAGEOFFSET_12L: // LDR 12 bit page offset
                        if (inSection)
                            PutArm64Rel12Ldr((uint*)address, (int)(nuint)target & 0xFFF);
                        wasRelocHandled = true;
                        break;

                    case CorInfoReloc.ARM64_WIN_TLS_SECREL_HIGH12A: // TLSDESC ADD for High-12 Add
                    case CorInfoReloc.ARM64_WIN_TLS_SECREL_LOW12A: // TLSDESC ADD for Low-12 Add
                    case CorInfoReloc.ARM64_LIN_TLSDESC_LD64_LO12:
                    case CorInfoReloc.ARM64_LIN_TLSDESC_ADD_LO12: // TLSDESC ADD for corresponding ADRP
                    case CorInfoReloc.ARM64_LIN_TLSDESC_CALL:
                        // These are patched later by the linker during actual execution, and don't need relocation.
                        wasRelocHandled = true;
                        break;
                }
            }

            if (targetArch == TargetArchitecture.RiscV64 &&
                relocType is CorInfoReloc.RISCV64_CALL_PLT or CorInfoReloc.RISCV64_PCREL_I or CorInfoReloc.RISCV64_PCREL_S)
            {
                if (inSection)
                {
                    // Like x64's IMAGE_REL_BASED_REL32 handling, hardcode the bottom bits of the target into the
                    // instruction.
                    long offset = (long)tmp.target;
                    int lo12 = (int)((offset << (64 - 12)) >> (64 - 12));
                    int hi20 = (int)(offset - lo12);
                    PutRiscV64AuipcCombo((uint*)address, (long)lo12 + hi20, relocType == CorInfoReloc.RISCV64_PCREL_S);
                }
                wasRelocHandled = true;
            }

            if (IsSpmiTarget64Bit())
            {
                if (!wasRelocHandled && relocType == CorInfoReloc.DIRECT)
                {
                    // Write 64 bits into the location.
                    if (inSection)
                    {
                        Logger.LogDebug($"    fixupLoc-{fixupLocation:X16} (@0x{(ulong)address:x}) {*(ulong*)address:X16} => {tmp.target:X16}");
                        *(ulong*)address = tmp.target;
                    }
                    wasRelocHandled = true;
                }
                else if (relocType == CorInfoReloc.AMD64_LIN_TLSGD)
                {
                    // These are patched later by the linker during actual execution, and don't need relocation.
                    wasRelocHandled = true;
                }
            }

            if (wasRelocHandled)
                continue;

            // Now do all-platform relocations.
            if (relocType is CorInfoReloc.RELATIVE32 or CorInfoReloc.AMD64_WIN_SECREL)
            {
                if (inSection)
                    ApplyRelative32(ref rc, tmp, address, fixupLocation, blocksize1, originalAddr);
                wasRelocHandled = true;
            }

            if (!wasRelocHandled)
                Logger.LogError($"Unknown reloc type {tmp.fRelocType}");
        }
    }

    private static void ApplyRelative32(ref RelocContext rc, in Agnostic_RecordRelocation tmp, nuint address, ulong fixupLocation, uint blocksize1, void* originalAddr)
    {
        ulong target = tmp.target + (ulong)(long)(int)tmp.addlDelta;
        ulong baseAddr = fixupLocation + sizeof(int);
        long delta = (long)(target - baseAddr);
        bool deltaIsFinal = false;
        MethodContext mc = rc.mc;

        if (IsSpmiTarget64Bit())
        {
            if (GetSpmiTargetArchitecture() == TargetArchitecture.X64 && mc.GetRelocTypeHint != null)
            {
                // For just AMD64: the VM tries to allocate the JIT code buffer near the CLR assemblies, so that
                // 32-bit offsets (and RELATIVE32 relocations) can be used in the code. If a RELATIVE32
                // relocation doesn't fit, the VM throws away the JIT result, disables RELATIVE32 relocations,
                // and restarts the compilation. SuperPMI doesn't know where the original compilation was
                // allocated (though maybe that should be added to the MC, not just the CompileResult), and
                // doesn't control where the JIT buffer is allocated. So if getRelocTypeHint() was called on
                // the target address and returned IMAGE_REL_BASED_REL32, use the low-order 32 bits of the
                // target address. That's unique enough for assembly diffs: the delta compares identically and
                // doesn't depend on where SuperPMI allocated the JIT memory.
                int index = mc.GetRelocTypeHint.GetIndex(tmp.target);
                if (index != -1 && (CorInfoReloc)mc.GetRelocTypeHint.GetItem(index) == CorInfoReloc.RELATIVE32)
                {
                    Logger.LogDebug($"    RELATIVE32 target used as argument to getRelocTypeHint: setting delta={(int)tmp.target} " +
                        $"(0x{(uint)tmp.target:X})");
                    delta = (int)tmp.target;
                    deltaIsFinal = true;
                }
            }

            // If the target is the result of getHelperFtn(), the VM would create a jump stub if the RELATIVE32
            // address doesn't fit. Don't fail with a RELATIVE32 overflow if the actual target address doesn't
            // fit: use its low-order 32 bits. The whole table is searched, since the helper id isn't known.
            if (!deltaIsFinal && mc.GetHelperFtn != null)
            {
                foreach (Agnostic_GetHelperFtn value in mc.GetHelperFtn.GetRawItems())
                {
                    if (value.helperLookup.handle == tmp.target)
                    {
                        Logger.LogDebug($"    RELATIVE32 target is result of getHelperFtn(): setting delta={(int)tmp.target} " +
                            $"(0x{(uint)tmp.target:X})");
                        delta = (int)tmp.target;
                        deltaIsFinal = true;
                        break;
                    }
                }
            }

            // Likewise for results of GetFunctionEntryPoint().
            if (!deltaIsFinal && mc.GetFunctionEntryPoint != null)
            {
                foreach (DLD value in mc.GetFunctionEntryPoint.GetRawItems())
                {
                    if (value.A == tmp.target)
                    {
                        Logger.LogDebug($"    RELATIVE32 target is result of getFunctionEntryPoint(): setting delta={(int)tmp.target} " +
                            $"(0x{(uint)tmp.target:X})");
                        delta = (int)tmp.target;
                        deltaIsFinal = true;
                        break;
                    }
                }
            }

            // A relocation to the RO data section must fit in 32 bits for both the baseline and diff
            // compilations, so pretend the RO data section immediately follows the current code section.
            if (!deltaIsFinal && rc.originalRoDataAddress1 <= (nuint)target && (nuint)target < rc.originalRoDataAddress1 + rc.roDataSize1)
            {
                nuint ro_section_offset = (nuint)target - rc.originalRoDataAddress1;
                if (rc.originalHotCodeAddress <= (nuint)fixupLocation && (nuint)fixupLocation < rc.originalHotCodeAddress + rc.hotCodeSize)
                {
                    delta = (long)(rc.originalHotCodeAddress + rc.hotCodeSize + ro_section_offset - baseAddr);
                    deltaIsFinal = true;
                    Logger.LogDebug($"    RELATIVE32 hot code target is in RO data section: setting delta={(int)delta} (0x{(uint)delta:X})");
                }
                else if (rc.originalColdCodeAddress <= (nuint)fixupLocation && (nuint)fixupLocation < rc.originalColdCodeAddress + rc.coldCodeSize)
                {
                    delta = (long)(rc.originalColdCodeAddress + rc.coldCodeSize + ro_section_offset - baseAddr);
                    deltaIsFinal = true;
                    Logger.LogDebug($"    RELATIVE32 cold code target is in RO data section: setting delta={(int)delta} (0x{(uint)delta:X})");
                }
            }

            if (delta != (int)delta)
            {
                // This won't fit in a signed 32-bit offset. Use something that fits, assuming the original
                // compilation fit fine. This is only an issue for 32-bit offsets on 64-bit targets.
                target = CastHandle(originalAddr) + blocksize1;
                long newdelta = (long)(target - baseAddr);
                Logger.LogDebug($"    RELATIVE32 overflow. Mapping target to {target:X16}. Mapping delta: {delta:X16} => {newdelta:X16}");
                delta = newdelta;
            }
        }

        if (IsSpmiTarget32Bit())
        {
            // Only 32 bits matter, regardless of sign. Note that SuperPMI sometimes returns arbitrary values,
            // such as 0xCAFE0003 from MethodContext::repGetHelperFtn().
            delta &= 0xFFFFFFFF;
        }
        else if (delta != (int)delta)
        {
            Logger.LogError($"RELATIVE32 relocation overflows field! delta=0x{delta:X16}");
        }

        if (GetSpmiTargetArchitecture() == TargetArchitecture.X64 && !deltaIsFinal)
        {
            // During an actual compile, recordRelocation() is called before the compile is finished, and it
            // writes the relative offset into the fixup location. Then emitEndCodeGen() patches forward jumps
            // by subtracting any adjustment due to overestimated instruction sizes. Since the relocs are
            // applied after the compile has finished, reverse that: add in the (negative) adjustment that's
            // now in the fixup location.
            delta += *(int*)address;
        }

        // Write 32 bits into the location.
        Logger.LogDebug($"    fixupLoc-{fixupLocation:X16} (@0x{(ulong)address:x}) : {*(uint*)address:X8} => {(uint)delta:X8}");
        *(uint*)address = (uint)delta;
    }

    // Finds the recorded relocation (if any) whose location is in [originalBufferStart + originalBufferOffset,
    // + windowSize). The wasm32 near differ uses this to map a coredistools opcode-byte block offset to a
    // JIT-recorded reloc on the immediate-payload byte.
    public bool findRelocationInRange(nuint originalBufferStart, nuint originalBufferOffset, nuint windowSize, out Agnostic_RecordRelocation relocation)
    {
        relocation = default;
        if (RecordRelocation == null || windowSize == 0)
            return false;

        nuint rangeLo = originalBufferStart + originalBufferOffset;
        nuint rangeHi = rangeLo + windowSize;

        // Guard against wraparound (rangeLo near SIZE_MAX), rather than scanning an inverted range that could
        // match unrelated relocs.
        if (rangeHi < rangeLo)
            return false;

        foreach (Agnostic_RecordRelocation item in RecordRelocation.GetRawItems())
        {
            if (rangeLo <= (nuint)item.location && (nuint)item.location < rangeHi)
            {
                relocation = item;
                return true;
            }
        }
        return false;
    }

    public void recProcessName(string name)
    {
        ProcessName ??= new DenseLightWeightMap<uint>();
        ProcessName.Append(name != null ? ProcessName.AddBuffer(CStringBytes(name)) : uint.MaxValue);
    }

    public void recReserveUnwindInfo(bool isFunclet, bool isColdCode, uint unwindSize)
    {
        ReserveUnwindInfo ??= new DenseLightWeightMap<Agnostic_ReserveUnwindInfo>();
        ReserveUnwindInfo.Append(new Agnostic_ReserveUnwindInfo { isFunclet = isFunclet ? 1u : 0u, isColdCode = isColdCode ? 1u : 0u, unwindSize = unwindSize });
    }

    public void recAllocUnwindInfo(byte* pHotCode, byte* pColdCode, uint startOffset, uint endOffset, uint unwindSize, byte* pUnwindBlock, CorJitFuncKind funcKind)
    {
        AllocUnwindInfo ??= new DenseLightWeightMap<Agnostic_AllocUnwindInfo>();
        AllocUnwindInfo.Append(new Agnostic_AllocUnwindInfo
        {
            pHotCode = CastHandle(pHotCode),
            pColdCode = CastHandle(pColdCode),
            startOffset = startOffset,
            endOffset = endOffset,
            unwindSize = unwindSize,
            pUnwindBlock_index = AllocUnwindInfo.AddBuffer(Bytes(pUnwindBlock, unwindSize)),
            funcKind = (uint)funcKind,
        });
    }

    public void recRecordCallSite(uint instrOffset, CORINFO_SIG_INFO* callSig, CORINFO_METHOD_STRUCT_* methodHandle)
        => repRecordCallSite(instrOffset, callSig, methodHandle);

    public void repRecordCallSite(uint instrOffset, CORINFO_SIG_INFO* callSig, CORINFO_METHOD_STRUCT_* methodHandle)
    {
        if (RecordCallSiteWithSignature == null)
        {
            // Most call site records only have methodHandle, so two separate maps give better performance and
            // smaller memory use. These maps aren't read during a normal replay.
            RecordCallSiteWithSignature = new LightWeightMap<uint, Agnostic_RecordCallSite>();
            if (recordCallSitesWithoutSig)
                RecordCallSiteWithoutSignature = new LightWeightMap<uint, ulong>();
        }

        if (callSig != null)
        {
            DenseLightWeightMap<ulong> handleMap = CrSigInstHandleMap;
            Agnostic_CORINFO_SIG_INFO sig = StoreAgnostic_CORINFO_SIG_INFO(*callSig, RecordCallSiteWithSignature, ref handleMap);
            CrSigInstHandleMap = handleMap;
            RecordCallSiteWithSignature.Add(instrOffset, new Agnostic_RecordCallSite { callSig = sig, methodHandle = CastHandle(methodHandle) });
        }
        else if (recordCallSitesWithoutSig)
        {
            RecordCallSiteWithoutSignature.Add(instrOffset, CastHandle(methodHandle));
        }
    }

    public bool fndRecordCallSiteSigInfo(uint instrOffset, CORINFO_SIG_INFO* pCallSig)
    {
        if (RecordCallSiteWithSignature == null || RecordCallSiteWithSignature.GetIndex(instrOffset) == -1)
            return false;

        Agnostic_RecordCallSite value = RecordCallSiteWithSignature.Get(instrOffset);
        if (value.callSig.callConv == uint.MaxValue)
            return false;

        *pCallSig = Restore_CORINFO_SIG_INFO(value.callSig, RecordCallSiteWithSignature, CrSigInstHandleMap, MemoryTracker);
        return true;
    }

    public bool fndRecordCallSiteMethodHandle(uint instrOffset, CORINFO_METHOD_STRUCT_** pMethodHandle)
    {
        if (RecordCallSiteWithSignature != null && RecordCallSiteWithSignature.GetIndex(instrOffset) != -1)
            *pMethodHandle = (CORINFO_METHOD_STRUCT_*)RecordCallSiteWithSignature.Get(instrOffset).methodHandle;
        else if (RecordCallSiteWithoutSignature != null && RecordCallSiteWithoutSignature.GetIndex(instrOffset) != -1)
            *pMethodHandle = (CORINFO_METHOD_STRUCT_*)RecordCallSiteWithoutSignature.Get(instrOffset);
        return false;
    }

    private static ReadOnlySpan<byte> Bytes(void* data, nuint length) => data == null ? default : new ReadOnlySpan<byte>(data, (int)length);

    private static ReadOnlySpan<byte> CString(byte* s) => new(s, System.Runtime.InteropServices.MemoryMarshal.CreateReadOnlySpanFromNullTerminated(s).Length + 1);

    private static byte[] CStringBytes(string s)
    {
        byte[] bytes = new byte[System.Text.Encoding.UTF8.GetByteCount(s) + 1];
        System.Text.Encoding.UTF8.GetBytes(s, bytes);
        return bytes;
    }
}
