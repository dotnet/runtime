// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// stackscan.cpp
//
// Implementation of the Normal-tier conservative stack scan declared in
// stackscan.h. Ports the ExecutionManager RangeSectionMap radix lookup and the
// EEJitManager NibbleMap (IP -> method code) so that, for each code pointer found
// on a thread stack, only that method's code + header + MethodDesc is captured --
// avoiding the bulk emission of every JIT/loader heap.
//
// The traversal is organized as a `CodeEnumerator`: mirroring the DAC's per-type
// EnumMemoryRegions, each Enum*/method reports the memory for one kind of runtime
// structure (JIT code, MethodDesc, MethodTable, precode, R2R map) and recurses
// into the structures it references. The enumerator owns the shared context (the
// target, the region sink, the code range map and the stub-code sentinel) plus
// the dedup/visited state, so the individual steps need not thread it through.
//*****************************************************************************

#include "stackscan.h"
#include "runtimetypes.h"

#include <set>
#include <vector>
#include <cstring>

namespace cdac
{
namespace contracts
{
    namespace
    {
        const char* const GlobalThreadStore = "ThreadStore";
        const char* const GlobalCodeRangeMap = "ExecutionManagerCodeRangeMapAddress";
        const char* const GlobalStubCodeBlockLast = "StubCodeBlockLast";

        const int MaxThreads = 100000;
        const int MaxFragments = 100000;
        const int MaxFrames = 100000; // guard against a corrupt Frame chain

        // Stacks are scanned page-by-page: a thread's [limit, base) range spans the whole
        // RESERVED stack (which can be very large, e.g. a Thread created with a big maxStackSize),
        // but only the committed portion near the base is real. Probing one slot per page and
        // skipping unreadable pages avoids millions of failing reads across the reserved-but-
        // uncommitted range.
        const uint64_t StackScanPageSize = 4096;

        // RangeSection.Flags
        const int32_t RangeSectionFlag_CodeHeap = 0x02;

        // NibbleMap constants (see NibbleMapHelpers.cs).
        const uint64_t MapUnitSizeInBytes = 4;
        const uint64_t MapUnitSizeInNibbles = 8;
        const uint64_t BytesPerBucket = 8 * MapUnitSizeInBytes; // 32
        const uint32_t NibbleMask = 0x0Fu;

        // Bound how much code we emit around a stack IP (covers the method body + code header).
        const uint32_t MaxMethodCodeEmit = 64 * 1024;

        // Bound how much of a method's GC info blob to emit. The blob is variable-length and its
        // size is only known by decoding it; SOS GetCodeHeaderData decodes just enough to read the
        // method's code length (the header prefix), so a generous fixed span covers the typical
        // method's whole blob and always covers the header the length decode needs.
        const uint32_t MaxGcInfoEmit = 4 * 1024;

        // Sentinel returned by a PtrHashMap probe for a deleted/invalid entry.
        const uint32_t HashInvalidEntry = 0xFFFFFFFFu;

        // --- Stateless helpers ------------------------------------------------
        // Pure math + descriptor-only reads with no memory emission. They take the Target
        // directly; the emitting traversal lives in CodeEnumerator below.

        // RangeSectionMap radix lookup (ExecutionManagerHelpers.RangeSectionMap).
        int MapLevels(const Target& target) { return target.PointerSize() == 8 ? 5 : 2; }
        int MaxSetBit(const Target& target) { return target.PointerSize() == 8 ? 56 : 31; }

        int GetIndexForLevel(const Target& target, uint64_t address, int level)
        {
            const int bitsPerLevel = 8;
            uint64_t used = address >> (MaxSetBit(target) + 1 - (MapLevels(target) * bitsPerLevel));
            uint64_t shifted = used >> ((level - 1) * bitsPerLevel);
            return (int)(255u & shifted);
        }

        // NibbleMap decode helpers.
        bool IsPointerUnit(uint32_t unit) { return (unit & NibbleMask) > 8; }

        uint64_t DecodePointer(uint64_t baseAddress, uint32_t unit)
        {
            uint32_t nibble = unit & NibbleMask;
            uint32_t relativePointer = (unit & ~NibbleMask) + ((nibble - 9) << 2);
            return baseAddress + relativePointer;
        }

        uint32_t GetNibbleShift(uint64_t mapIdx)
        {
            uint32_t nibbleIndexInMapUnit = (uint32_t)(mapIdx & (MapUnitSizeInNibbles - 1));
            return 28 - (nibbleIndexInMapUnit * 4);
        }

        uint64_t GetAbsoluteAddress(uint64_t baseAddress, uint64_t mapIdx, uint32_t nibble)
        {
            uint64_t mapIdxByteOffset = mapIdx * BytesPerBucket;
            uint64_t nibbleByteOffset = (uint64_t)(nibble - 1) * MapUnitSizeInBytes;
            return baseAddress + mapIdxByteOffset + nibbleByteOffset;
        }

        // Computes a MethodDesc's exact size from the data descriptor: the classification base size
        // (MethodDesc::GetBaseSize / s_ClassificationSizeTable) plus any present optional slots.
        // Mirrors MethodDescOptionalSlots; no hardcoded MethodDesc size.
        uint32_t MethodDescSize(const Target& target, uint64_t methodDesc)
        {
            uint64_t flagsAddr = 0;
            uint16_t flags = 0;
            if (!target.TryGetFieldAddress(methodDesc, "MethodDesc", "Flags", flagsAddr) ||
                !target.TryReadUInt16(flagsAddr, flags))
            {
                return 0;
            }
            // MethodClassification (Flags & 0x7) -> the concrete MethodDesc subclass type.
            static const char* const classTypes[8] = {
                "MethodDesc", "FCallMethodDesc", "PInvokeMethodDesc", "EEImplMethodDesc",
                "ArrayMethodDesc", "InstantiatedMethodDesc", "CLRToCOMCallMethodDesc", "DynamicMethodDesc"
            };
            uint32_t size = 0;
            if (!target.TryGetTypeSize(classTypes[flags & 0x7], size))
            {
                return 0;
            }
            // Optional slots that follow the base MethodDesc (flag bits from MethodDescFlags).
            uint32_t slot = 0;
            if ((flags & 0x08) && target.TryGetTypeSize("NonVtableSlot", slot)) { size += slot; }   // HasNonVtableSlot
            if ((flags & 0x10) && target.TryGetTypeSize("MethodImpl", slot)) { size += slot; }       // HasMethodImpl
            if ((flags & 0x20) && target.TryGetTypeSize("NativeCodeSlot", slot)) { size += slot; }   // HasNativeCodeSlot
            if ((flags & 0x40) && target.TryGetTypeSize("AsyncMethodData", slot)) { size += slot; }  // HasAsyncMethodData
            return size;
        }

        // Computes the address of a MethodDesc's native code slot (if present), mirroring
        // MethodDescOptionalSlots.GetAddressOfNativeCodeSlot: the classification base size plus the
        // NonVtableSlot / MethodImpl optional slots that precede it. Returns false if absent.
        bool TryGetNativeCodeSlotAddress(const Target& target, uint64_t methodDesc, uint64_t& slotAddr)
        {
            uint64_t flagsAddr = 0;
            uint16_t flags = 0;
            if (!target.TryGetFieldAddress(methodDesc, "MethodDesc", "Flags", flagsAddr) ||
                !target.TryReadUInt16(flagsAddr, flags))
            {
                return false;
            }
            if ((flags & 0x20) == 0) // HasNativeCodeSlot
            {
                return false;
            }
            static const char* const classTypes[8] = {
                "MethodDesc", "FCallMethodDesc", "PInvokeMethodDesc", "EEImplMethodDesc",
                "ArrayMethodDesc", "InstantiatedMethodDesc", "CLRToCOMCallMethodDesc", "DynamicMethodDesc"
            };
            uint32_t offset = 0;
            if (!target.TryGetTypeSize(classTypes[flags & 0x7], offset))
            {
                return false;
            }
            uint32_t slot = 0;
            if ((flags & 0x08) && target.TryGetTypeSize("NonVtableSlot", slot)) { offset += slot; } // HasNonVtableSlot
            if ((flags & 0x10) && target.TryGetTypeSize("MethodImpl", slot)) { offset += slot; }     // HasMethodImpl
            slotAddr = methodDesc + offset;
            return true;
        }

        // Finds the largest RUNTIME_FUNCTION index whose BeginAddress <= rva (sorted table).
        bool FindRuntimeFunctionIndex(const Target& target, uint64_t rfTable, uint32_t rfStride,
                                      uint32_t count, uint32_t rva, uint32_t& index)
        {
            if (count == 0)
            {
                return false;
            }
            uint32_t left = 0;
            uint32_t right = count - 1;
            bool found = false;
            index = 0;
            while (left <= right)
            {
                uint32_t mid = left + (right - left) / 2;
                uint32_t begin = 0;
                target.TryReadUInt32(rfTable + (uint64_t)mid * rfStride, begin);
                if (begin <= rva)
                {
                    index = mid;
                    found = true;
                    if (mid == 0xFFFFFFFFu) break;
                    left = mid + 1;
                }
                else
                {
                    if (mid == 0) break;
                    right = mid - 1;
                }
            }
            return found;
        }

        // Enumerates the memory a stack walk reaches. Mirroring the DAC's per-type
        // EnumMemoryRegions, each Enum* method reports the memory for one kind of runtime structure
        // and recurses into the structures it references. Shared context (target, region sink, code
        // range map, stub-code sentinel) and the dedup/visited state are owned here so the steps
        // stay small and free of parameter threading.
        class CodeEnumerator
        {
        public:
            CodeEnumerator(const Target& target, RegionCallback sink, void* sinkContext,
                           uint64_t codeRangeMap, uint64_t stubCodeBlockLast)
                : m_target(target), m_sink(sink), m_sinkContext(sinkContext),
                  m_codeRangeMap(codeRangeMap), m_stubCodeBlockLast(stubCodeBlockLast)
            {
            }

            // Resolves a code pointer to its method and reports the method's code + header +
            // MethodDesc graph. Returns the MethodDesc, or 0 if not resolved. Dispatches an EEJit
            // code heap (NibbleMap) vs an R2R section (runtime-function table + map probe).
            uint64_t EnumCodeForIP(uint64_t ip)
            {
                uint64_t rangeSectionAddr = FindRangeSection(ip);
                if (rangeSectionAddr == 0)
                {
                    return 0;
                }
                data::RangeSection rangeSection;
                if (!m_target.TryRead(rangeSectionAddr, rangeSection))
                {
                    return 0;
                }
                // R2R sections: resolve via the R2R runtime-function table + EntryPointToMethodDescMap
                // probe (image code comes from disk). EEJit code heaps: resolve via NibbleMap.
                if (((int32_t)rangeSection.Flags & RangeSectionFlag_CodeHeap) == 0 || rangeSection.HeapList == 0)
                {
                    return EnumR2RMethodDesc(rangeSection, ip);
                }
                uint64_t methodDescPtr = EnumJitCodeAt(rangeSection, ip);
                if (methodDescPtr != 0)
                {
                    EnumMethodDesc(methodDescPtr, /*emitNativeCode*/ true);
                }
                return methodDescPtr;
            }

            // Resolves + reports the MethodDesc carried by a transition Frame (see
            // ResolveFrameMethodDesc). Records it as captured so the driver's count includes it.
            void EnumFrameMethodDesc(uint64_t frameAddr, uint64_t identifier)
            {
                uint64_t md = ResolveFrameMethodDesc(frameAddr, identifier);
                if (md != 0)
                {
                    EnumMethodDesc(md, /*emitNativeCode*/ true);
                    m_captured.insert(md);
                }
            }

            // Whether 'value' was already scanned; also records it. Deep/recursive stacks push the
            // same return address thousands of times, so deduping the scanned values collapses the
            // work to O(distinct code pointers).
            bool AlreadyProcessed(uint64_t value) { return !m_processed.insert(value).second; }
            void NoteCapturedMethod(uint64_t md) { m_captured.insert(md); }
            size_t CapturedMethodCount() const { return m_captured.size(); }

        private:
            // Reports one raw memory region [start, start+size), deduped by start address. The scan
            // re-reads the same radix/nibble/code/methoddesc addresses across many stack slots
            // (recursion is the extreme case); forwarding each only once keeps the region list small.
            void Emit(const char* kind, uint64_t start, uint64_t size)
            {
                if (start != 0 && size > 0 && m_emitted.insert(start).second)
                {
                    m_sink(m_sinkContext, kind, start, size);
                }
            }

            // Walks the radix map for 'addr' and returns the RangeSection covering it, or 0.
            // Reports each map-node pointer slot it reads (the reader re-walks the same slots).
            uint64_t FindRangeSection(uint64_t addr)
            {
                const uint64_t ptrSize = m_target.PointerSize();
                uint64_t levelMap = m_codeRangeMap;
                int level = MapLevels(m_target);
                uint64_t firstFragment = 0;
                for (;;)
                {
                    int index = GetIndexForLevel(m_target, addr, level);
                    uint64_t slot = levelMap + (uint64_t)index * ptrSize;
                    uint64_t value = 0;
                    if (!m_target.TryReadPointer(slot, value))
                    {
                        return 0;
                    }
                    // The reader reads this same slot during its own lookup.
                    Emit("code-rangemap", slot, ptrSize);
                    value &= ~1ull; // low bit is the collectible flag
                    if (level == 1)
                    {
                        firstFragment = value;
                        break;
                    }
                    if (value == 0)
                    {
                        return 0;
                    }
                    levelMap = value;
                    level--;
                }

                // Walk the fragment list at the leaf to find the covering RangeSection.
                uint64_t fragment = firstFragment;
                for (int i = 0; fragment != 0 && i < MaxFragments; i++)
                {
                    data::RangeSectionFragment f;
                    if (!m_target.TryRead(fragment, f)) // struct read -> captured by EnumMem
                    {
                        break;
                    }
                    if (addr >= f.RangeBegin && addr < f.RangeEndOpen)
                    {
                        return f.RangeSection;
                    }
                    fragment = f.Next & ~1ull; // low bit is the collectible flag (managed masks it too)
                }
                return 0;
            }

            // --- NibbleMap: IP -> start of the containing method's code -------

            // Reads one map unit and reports it (the reader re-reads the same units).
            uint32_t ReadMapUnit(uint64_t mapStart, uint64_t mapIdx)
            {
                uint64_t unitAddr = mapStart + (mapIdx / MapUnitSizeInNibbles) * MapUnitSizeInBytes;
                uint32_t value = 0;
                m_target.TryReadUInt32(unitAddr, value);
                Emit("code-nibblemap", unitAddr, MapUnitSizeInBytes);
                return value;
            }

            // Returns the code start address for 'currentPC', or 0. Faithful port of
            // NibbleMapConstantLookup.FindMethodCode.
            uint64_t NibbleMapFindMethodCode(uint64_t mapBase, uint64_t mapStart, uint64_t currentPC)
            {
                uint64_t relativeAddress = currentPC - mapBase;
                uint64_t mapIdx = relativeAddress / BytesPerBucket;
                uint32_t bucketByteIndex = (uint32_t)((relativeAddress & (BytesPerBucket - 1)) / MapUnitSizeInBytes) + 1;

                uint32_t t = ReadMapUnit(mapStart, mapIdx);
                if (IsPointerUnit(t))
                {
                    return DecodePointer(mapBase, t);
                }

                // Focus on the indexed nibble.
                t = t >> GetNibbleShift(mapIdx);
                uint32_t nibble = t & NibbleMask;
                if (nibble != 0 && nibble <= bucketByteIndex)
                {
                    return GetAbsoluteAddress(mapBase, mapIdx, nibble);
                }

                // Search backwards through the current map unit.
                t = t >> 4; // shift to next nibble
                if (t != 0)
                {
                    if (mapIdx == 0)
                    {
                        return 0;
                    }
                    mapIdx = mapIdx - 1;
                    while ((t & NibbleMask) == 0)
                    {
                        t = t >> 4;
                        if (mapIdx == 0)
                        {
                            break;
                        }
                        mapIdx = mapIdx - 1;
                    }
                    return GetAbsoluteAddress(mapBase, mapIdx, t & NibbleMask);
                }

                // We finished the current map unit; if we were in the first, stop.
                if (mapIdx < MapUnitSizeInNibbles)
                {
                    return 0;
                }

                // Align down to the map unit, then move back one nibble into the previous unit.
                mapIdx = mapIdx & (~(MapUnitSizeInNibbles - 1));
                mapIdx = mapIdx - 1;

                t = ReadMapUnit(mapStart, mapIdx);
                if (t == 0)
                {
                    return 0;
                }
                if (IsPointerUnit(t))
                {
                    return DecodePointer(mapBase, t);
                }

                while (mapIdx != 0 && (t & NibbleMask) == 0)
                {
                    t = t >> 4;
                    mapIdx = mapIdx - 1;
                }
                return GetAbsoluteAddress(mapBase, mapIdx, t & NibbleMask);
            }

            // Reports the NibbleMap units, code-header pointer, RealCodeHeader (+ its unwind-info
            // array), and method code bytes for a JIT'd code address in a known CodeHeap
            // RangeSection. Returns the code header's MethodDesc (or 0). Does NOT report the
            // MethodDesc graph -- callers do that -- so it can be reused both for a stack return
            // address AND for a MethodDesc's own native-code entry point (the reader re-resolves the
            // latter during validation) without recursing.
            uint64_t EnumJitCodeAt(const data::RangeSection& rangeSection, uint64_t ip)
            {
                data::CodeHeapListNode heapNode;
                if (!m_target.TryRead(rangeSection.HeapList, heapNode))
                {
                    return 0;
                }
                if (ip < heapNode.StartAddress || ip > heapNode.EndAddress)
                {
                    return 0;
                }

                uint64_t codeStart = NibbleMapFindMethodCode(heapNode.MapBase, heapNode.HeaderMap, ip);
                if (codeStart == 0 || codeStart > ip)
                {
                    return 0;
                }

                // The code header pointer is stored one pointer before the code start.
                uint64_t codeHeaderIndirect = codeStart - m_target.PointerSize();
                uint64_t codeHeaderAddress = 0;
                if (!m_target.TryReadPointer(codeHeaderIndirect, codeHeaderAddress) || codeHeaderAddress == 0)
                {
                    return 0;
                }
                Emit("code-header-ptr", codeHeaderIndirect, m_target.PointerSize());

                // Stub code blocks have a small sentinel value instead of a real header.
                if (codeHeaderAddress <= m_stubCodeBlockLast)
                {
                    return 0;
                }

                data::RealCodeHeader codeHeader;
                if (!m_target.TryRead(codeHeaderAddress, codeHeader)) // captured by EnumMem
                {
                    return 0;
                }

                // The AMD64 unwinder reads the method's unwind info (RealCodeHeader.UnwindInfos, an
                // inline array of NumUnwindInfos RuntimeFunction entries) to unwind the frame. That
                // trailing array extends past the fixed RealCodeHeader, so emit it explicitly.
                uint64_t unwindInfosAddr = 0;
                uint32_t rfStride = 0;
                if (codeHeader.NumUnwindInfos != 0 &&
                    m_target.TryGetFieldAddress(codeHeaderAddress, "RealCodeHeader", "UnwindInfos", unwindInfosAddr) &&
                    m_target.TryGetTypeSize("RuntimeFunction", rfStride) && rfStride != 0)
                {
                    Emit("unwind-infos", unwindInfosAddr, (uint64_t)codeHeader.NumUnwindInfos * rfStride);
                }

                // SOS GetCodeHeaderData decodes the method's GC info blob (RealCodeHeader.GCInfo) to
                // report MethodSize (GcInfoDecoder reads the method's code length from it). The blob
                // is a separate, variable-length allocation the code header only points to, so emit a
                // bounded span for it here.
                if (codeHeader.GCInfo != 0)
                {
                    Emit("gc-info", codeHeader.GCInfo, MaxGcInfoEmit);
                }

                // Emit the method's code (code start through the IP, bounded).
                uint64_t emitEnd = ip + m_target.PointerSize();
                uint64_t codeSize = emitEnd - codeStart;
                if (codeSize > MaxMethodCodeEmit)
                {
                    codeSize = MaxMethodCodeEmit;
                }
                Emit("jit-code", codeStart, codeSize);

                return codeHeader.MethodDesc;
            }

            // Reports a MethodTable and the structures the reader touches while validating a
            // MethodDesc (RuntimeTypeSystem.SlotIsVtableSlot / GetTypeHandle): the MethodTable
            // header + a generous span for inline vtable slots, plus its Module and EEClass/
            // canonical MethodTable. These are Data-type reads captured via the Target's EnumMem.
            void EnumMethodTable(uint64_t mtPtr)
            {
                if (mtPtr == 0)
                {
                    return;
                }
                m_target.EmitStruct("MethodTable", mtPtr);
                // Inline vtable slots follow the fixed MethodTable fields; emit a generous span.
                m_target.EmitMemory(mtPtr, 0x400);

                uint64_t module = 0;
                if (m_target.TryReadFieldPointer(mtPtr, "MethodTable", "Module", module) && module != 0)
                {
                    m_target.EmitStruct("Module", module);
                }
                // EEClassOrCanonMT: low bits are a discriminator (0 = EEClass, 1 = canonical MethodTable).
                uint64_t eeClassOrCanon = 0;
                if (m_target.TryReadFieldPointer(mtPtr, "MethodTable", "EEClassOrCanonMT", eeClassOrCanon) && eeClassOrCanon != 0)
                {
                    uint64_t masked = eeClassOrCanon & ~(uint64_t)0x3;
                    if ((eeClassOrCanon & 0x3) == 0)
                    {
                        m_target.EmitStruct("EEClass", masked);
                    }
                    else
                    {
                        // Canonical MethodTable: emit its header too (bounded; canon usually == self).
                        if (masked != mtPtr)
                        {
                            m_target.EmitStruct("MethodTable", masked);
                            m_target.EmitMemory(masked, 0x400);
                        }
                    }
                }
            }

            // Reports the precode stub the reader reads to round-trip a MethodDesc's temporary entry
            // point (PrecodeStubs.GetMethodDescFromStubAddress): the stub instruction bytes + the
            // StubPrecodeData stored one code page later. The PrecodeMachineDescriptor is emitted
            // once in enumerate.cpp.
            void EnumPrecodeStub(uint64_t entryPoint)
            {
                if (entryPoint == 0)
                {
                    return;
                }
                uint64_t platformMetadata = 0;
                if (!m_target.TryGetGlobalValue("PlatformMetadata", platformMetadata) || platformMetadata == 0)
                {
                    return;
                }
                uint64_t descAddr = 0;
                if (!m_target.TryGetFieldAddress(platformMetadata, "PlatformMetadata", "PrecodeMachineDescriptor", descAddr) || descAddr == 0)
                {
                    descAddr = platformMetadata; // embedded at offset 0
                }
                uint32_t stubCodePageSize = 0;
                m_target.TryReadFieldUInt32(descAddr, "PrecodeMachineDescriptor", "StubCodePageSize", stubCodePageSize);

                // Stub instruction bytes (type identification reads a few bytes at the entry point).
                m_target.EmitMemory(entryPoint, 64);
                // The precode data (StubPrecodeData/FixupPrecodeData) is stored one code page later.
                if (stubCodePageSize != 0)
                {
                    m_target.EmitMemory(entryPoint + stubCodePageSize, 128);
                }
            }

            // Reports a MethodDesc and everything the reader dereferences while validating it
            // (RuntimeTypeSystem.ValidateMethodDescPointer): the MethodDesc, its MethodDescChunk
            // (which sits BEFORE the MethodDesc at md - sizeof(chunk) - ChunkIndex*alignment),
            // MethodDescCodeData, the MethodTable graph, and the temporary-entry-point precode stub.
            // For JIT'd methods (emitNativeCode) it also covers the MethodDesc's own native-code
            // entry point: validation's HasNativeCode -> GetCodePointer -> GetCodeBlockHandle reads
            // the native code slot (pCode) then re-resolves that entry via RangeSection+NibbleMap+
            // code-header, and pCode can differ from any stack return address (e.g. a tiered/OSR
            // version not live on a stack), so its NibbleMap/header were never emitted by the scan.
            // (R2R native code is served from the on-disk image, so callers pass emitNativeCode=false.)
            void EnumMethodDesc(uint64_t methodDesc, bool emitNativeCode)
            {
                if (methodDesc == 0)
                {
                    return;
                }
                // Emit the MethodDesc using its exact descriptor-computed size (classification + slots).
                uint32_t mdSize = MethodDescSize(m_target, methodDesc);
                if (mdSize > 0)
                {
                    Emit("methoddesc", methodDesc, mdSize);
                }

                // The MethodDescChunk sits BEFORE the MethodDesc:
                //   chunk = methodDesc - sizeof(MethodDescChunk) - ChunkIndex * MethodDescAlignment.
                // All three come from the data descriptor / globals.
                uint64_t chunkIndexAddr = 0;
                uint8_t chunkIndex = 0;
                if (m_target.TryGetFieldAddress(methodDesc, "MethodDesc", "ChunkIndex", chunkIndexAddr))
                {
                    m_target.TryReadUInt8(chunkIndexAddr, chunkIndex);
                }
                uint64_t alignment = 0;
                uint32_t chunkSize = 0;
                if (!m_target.TryGetGlobalValue("MethodDescAlignment", alignment) || alignment == 0 ||
                    !m_target.TryGetStructSpan("MethodDescChunk", chunkSize))
                {
                    return;
                }
                uint64_t chunkAddr = methodDesc - chunkSize - (uint64_t)chunkIndex * alignment;
                Emit("methoddesc-chunk", chunkAddr, chunkSize);

                // The MethodDescChunk holds the MethodTable; validation reads it (and its graph).
                uint64_t mtPtr = 0;
                if (m_target.TryReadFieldPointer(chunkAddr, "MethodDescChunk", "MethodTable", mtPtr) && mtPtr != 0)
                {
                    EnumMethodTable(mtPtr);
                }

                // MethodDesc validation reads MethodDescCodeData (temporary entry point / tier) via
                // the MethodDesc's CodeData pointer, then round-trips the temporary entry point
                // through the precode stub.
                uint64_t codeData = 0;
                if (m_target.TryReadFieldPointer(methodDesc, "MethodDesc", "CodeData", codeData) && codeData != 0)
                {
                    m_target.EmitStruct("MethodDescCodeData", codeData);
                    uint64_t tempEntryPoint = 0;
                    if (m_target.TryReadFieldPointer(codeData, "MethodDescCodeData", "TemporaryEntryPoint", tempEntryPoint))
                    {
                        EnumPrecodeStub(tempEntryPoint);
                    }
                }

                // JIT'd code: also cover the MethodDesc's own native-code entry point (see comment).
                if (emitNativeCode)
                {
                    uint64_t nativeCodeSlotAddr = 0;
                    uint64_t pCode = 0;
                    if (TryGetNativeCodeSlotAddress(m_target, methodDesc, nativeCodeSlotAddr) &&
                        m_target.TryReadPointer(nativeCodeSlotAddr, pCode) && pCode != 0)
                    {
                        uint64_t rsAddr = FindRangeSection(pCode);
                        if (rsAddr != 0)
                        {
                            data::RangeSection rs;
                            if (m_target.TryRead(rsAddr, rs) &&
                                ((int32_t)rs.Flags & RangeSectionFlag_CodeHeap) != 0 && rs.HeapList != 0)
                            {
                                EnumJitCodeAt(rs, pCode);
                            }
                        }
                    }
                }
            }

            // PtrHashMap probe (HashMapLookup.GetValue + PtrHashMapLookup). Buckets touched during
            // the probe are emitted ONLY if the key is found -- the conservative stack scan probes
            // with many false-positive "keys" (stack data that happens to fall in an R2R range), and
            // emitting their probe chains would touch most of a large map. Buffering + emit-on-hit
            // keeps emission proportional to real frames (what the DAC does). Returns the value, or
            // 0 if not present.
            uint64_t HashMapProbe(uint64_t mapAddr, uint64_t key, uint64_t valueMask)
            {
                data::HashMap hm;
                if (!m_target.TryRead(mapAddr, hm) || hm.Buckets == 0)
                {
                    return 0;
                }
                // PtrHashMap::SanitizeKey
                if (key <= 1)
                {
                    key += 100;
                }
                uint32_t size = 0;
                if (!m_target.TryReadUInt32(hm.Buckets, size) || size <= 1)
                {
                    return 0;
                }

                // Bucket layout comes from the descriptor: total size, slot count, and the Values
                // field offset (Keys are the first field). No hardcoded 64/4/32.
                uint32_t bucketSize = 0;
                if (!m_target.TryGetTypeSize("Bucket", bucketSize) || bucketSize == 0)
                {
                    return 0;
                }
                uint64_t slotsPerBucket = 0;
                m_target.TryGetGlobalValue("HashMapSlotsPerBucket", slotsPerBucket);
                uint64_t valuesOffset = 0;
                m_target.TryGetFieldAddress(0, "Bucket", "Values", valuesOffset);
                if (slotsPerBucket == 0 || valuesOffset == 0)
                {
                    return 0;
                }
                const uint64_t ptrSize = m_target.PointerSize();

                uint32_t seed = (uint32_t)(key >> 2);
                uint32_t increment = (uint32_t)(1 + (((uint32_t)(key >> 5) + 1) % (size - 1)));
                uint64_t buckets = hm.Buckets + bucketSize;

                std::vector<uint64_t> touched; // bucket addresses visited on the probe chain
                for (uint32_t i = 0; i < size; i++)
                {
                    uint64_t bucketAddr = buckets + (uint64_t)bucketSize * (seed % size);
                    touched.push_back(bucketAddr);

                    uint64_t values0 = 0;
                    for (uint64_t slot = 0; slot < slotsPerBucket; slot++)
                    {
                        uint64_t k = 0;
                        if (m_target.TryReadPointer(bucketAddr + slot * ptrSize, k) && k == key)
                        {
                            uint64_t v = 0;
                            m_target.TryReadPointer(bucketAddr + valuesOffset + slot * ptrSize, v);
                            // Found: emit the count slot + the probe-chain buckets so the reader can re-probe.
                            Emit("r2r-hashcount", hm.Buckets, bucketSize);
                            for (uint64_t b : touched)
                            {
                                Emit("r2r-hashbucket", b, bucketSize);
                            }
                            return (v & valueMask) << 1; // PtrHashMap stores value >> 1
                        }
                    }
                    m_target.TryReadPointer(bucketAddr + valuesOffset, values0);

                    seed += increment;
                    // IsCollision: high (mask) bit of Values[0] set => probe continues.
                    if ((values0 & ~valueMask) == 0)
                    {
                        break;
                    }
                }
                return 0; // not found: emit nothing
            }

            // Resolves an R2R IP to its MethodDesc, reporting the probe-touched buckets + the
            // MethodDesc. For an IP in an R2R RangeSection we resolve the MethodDesc the way the
            // reader does (ExecutionManagerHelpers.ReadyToRunJitManager), so the memory the reader
            // touches (probe-chain hash buckets + the MethodDesc) is captured. We do NOT emit the
            // whole EntryPointToMethodDescMap -- it can be MBs; the DAC likewise only probes it
            // (dotnet/diagnostics#5910). The RUNTIME_FUNCTION table is image-backed, so it is read
            // (from the live target) but not emitted.
            uint64_t EnumR2RMethodDesc(const data::RangeSection& rangeSection, uint64_t ip)
            {
                if (rangeSection.R2RModule == 0)
                {
                    return 0;
                }
                data::Module module;
                if (!m_target.TryRead(rangeSection.R2RModule, module) || module.ReadyToRunInfo == 0)
                {
                    return 0;
                }
                data::ReadyToRunInfo r2r;
                if (!m_target.TryRead(module.ReadyToRunInfo, r2r) || r2r.RuntimeFunctions == 0 || r2r.NumRuntimeFunctions == 0)
                {
                    return 0;
                }

                uint64_t imageBase = rangeSection.RangeBegin;
                if (ip < imageBase)
                {
                    return 0;
                }
                uint32_t rva = (uint32_t)(ip - imageBase);

                uint32_t rfStride = 0;
                if (!m_target.TryGetTypeSize("RuntimeFunction", rfStride) || rfStride == 0)
                {
                    return 0;
                }

                uint32_t index = 0;
                if (!FindRuntimeFunctionIndex(m_target, r2r.RuntimeFunctions, rfStride, r2r.NumRuntimeFunctions, rva, index))
                {
                    return 0;
                }

                uint64_t compositeInfo = r2r.CompositeInfo != 0 ? r2r.CompositeInfo : module.ReadyToRunInfo;
                uint64_t mapAddr = 0;
                if (!m_target.TryGetFieldAddress(compositeInfo, "ReadyToRunInfo", "EntryPointToMethodDescMap", mapAddr))
                {
                    return 0;
                }

                uint64_t valueMask = 0x7FFFFFFFFFFFFFFFull;
                m_target.TryGetGlobalValue("HashMapValueMask", valueMask);

                // Funclets have no map entry; walk back to the enclosing function's entry point.
                // Bound the walk: real funclets are within a few runtime functions of their parent,
                // and an unbounded walk would spin for a false-positive (non-code) key with a high index.
                const uint32_t MaxFuncletWalk = 16;
                uint32_t walkEnd = index > MaxFuncletWalk ? index - MaxFuncletWalk : 0;
                for (uint32_t i = index; ; i--)
                {
                    uint32_t beginRva = 0;
                    m_target.TryReadUInt32(r2r.RuntimeFunctions + (uint64_t)i * rfStride, beginRva);
                    uint64_t entryPoint = imageBase + beginRva;
                    uint64_t methodDesc = HashMapProbe(mapAddr, entryPoint, valueMask);
                    if (methodDesc != 0 && (uint32_t)methodDesc != HashInvalidEntry)
                    {
                        // R2R native code is image-backed (served from disk), so emitNativeCode=false
                        // (only JIT'd code needs native-code-entry re-emission).
                        EnumMethodDesc(methodDesc, /*emitNativeCode*/ false);
                        return methodDesc;
                    }
                    if (i == 0 || i == walkEnd)
                    {
                        break;
                    }
                }
                return 0;
            }

            // Resolves the MethodDesc carried by a transition Frame, mirroring
            // FrameHelpers.GetMethodDescPtr. Transition frames store their MethodDesc in the
            // on-stack Frame object (not as a code return address), so the conservative stack scan
            // never resolves them. 'identifier' is the vtable pointer at the frame address
            // (Data.Frame.Identifier).
            uint64_t ResolveFrameMethodDesc(uint64_t frameAddr, uint64_t identifier)
            {
                if (identifier == 0)
                {
                    return 0;
                }
                const uint64_t ptrSize = m_target.PointerSize();
                uint64_t id = 0;

                // InlinedCallFrame (P/Invoke): MethodDesc is Datum, when the frame has an active call
                // (CallerReturnAddress != 0) to a real function (Datum != 0 and low bit clear).
                if (m_target.TryGetGlobalValue("InlinedCallFrameIdentifier", id) && id == identifier)
                {
                    uint64_t callerRet = 0, datum = 0;
                    m_target.TryReadFieldPointer(frameAddr, "InlinedCallFrame", "CallerReturnAddress", callerRet);
                    m_target.TryReadFieldPointer(frameAddr, "InlinedCallFrame", "Datum", datum);
                    if (callerRet != 0 && datum != 0 && (datum & 0x1) == 0)
                    {
                        return datum & ~(ptrSize - 1);
                    }
                    return 0;
                }

                // FramedMethodFrame and its subclasses share the layout; read MethodDescPtr.
                static const char* const framedIds[] = {
                    "FramedMethodFrameIdentifier", "DynamicHelperFrameIdentifier",
                    "ExternalMethodFrameIdentifier", "PrestubMethodFrameIdentifier",
                    "CallCountingHelperFrameIdentifier"
                };
                for (const char* gid : framedIds)
                {
                    if (m_target.TryGetGlobalValue(gid, id) && id == identifier)
                    {
                        uint64_t md = 0;
                        m_target.TryReadFieldPointer(frameAddr, "FramedMethodFrame", "MethodDescPtr", md);
                        return md;
                    }
                }

                // StubDispatchFrame: MethodDescPtr (the MT+slot fallback is omitted -- uncommon on
                // crash stacks and would pull in the type-system slot machinery).
                if (m_target.TryGetGlobalValue("StubDispatchFrameIdentifier", id) && id == identifier)
                {
                    uint64_t md = 0;
                    m_target.TryReadFieldPointer(frameAddr, "StubDispatchFrame", "MethodDescPtr", md);
                    return md;
                }
                return 0;
            }

            const Target& m_target;
            RegionCallback m_sink;
            void* m_sinkContext;
            uint64_t m_codeRangeMap;    // RangeSectionMap.TopLevelData (the radix map root)
            uint64_t m_stubCodeBlockLast;
            std::set<uint64_t> m_emitted;   // deduped explicit region starts
            std::set<uint64_t> m_processed; // distinct scanned stack values
            std::set<uint64_t> m_captured;  // distinct MethodDescs resolved
        };
    }

    int EnumerateStackScanRegions(const Target& target, RegionCallback sink, void* sinkContext)
    {
        // The code range map global holds the RangeSectionMap address directly (the managed
        // ExecutionManager contract uses ReadGlobalPointer(ExecutionManagerCodeRangeMapAddress)
        // without an extra deref). Use TryGetGlobalValue -- NOT TryReadGlobalPointer, which would
        // add a spurious dereference (correct for globals like ThreadStore whose value is the
        // address of a pointer variable, but wrong here).
        uint64_t codeRangeMapPtr = 0;
        if (!target.TryGetGlobalValue(GlobalCodeRangeMap, codeRangeMapPtr) || codeRangeMapPtr == 0)
        {
            return -1;
        }
        // The global points at the RangeSectionMap; its TopLevelData is the radix map root.
        data::RangeSectionMap rangeSectionMap;
        if (!target.TryRead(codeRangeMapPtr, rangeSectionMap) || rangeSectionMap.TopLevelData == 0)
        {
            return -1;
        }
        uint64_t topMap = rangeSectionMap.TopLevelData;

        uint64_t stubCodeBlockLast = 0;
        target.TryGetGlobalValue(GlobalStubCodeBlockLast, stubCodeBlockLast);

        uint64_t threadStoreAddr = 0;
        if (!target.TryReadGlobalPointer(GlobalThreadStore, threadStoreAddr) || threadStoreAddr == 0)
        {
            return -1;
        }
        data::ThreadStore threadStore;
        if (!target.TryRead(threadStoreAddr, threadStore))
        {
            return -1;
        }

        const uint64_t ptrSize = target.PointerSize();
        CodeEnumerator enumerator(target, sink, sinkContext, topMap, stubCodeBlockLast);
        std::set<uint64_t> visitedThreads;

        uint64_t threadAddr = threadStore.FirstThreadLink;
        for (int i = 0; threadAddr != 0 && i < MaxThreads; i++)
        {
            if (!visitedThreads.insert(threadAddr).second)
            {
                break;
            }
            data::Thread thread;
            if (!target.TryRead(threadAddr, thread))
            {
                break;
            }

            uint64_t stackLow = thread.CachedStackLimit;
            uint64_t stackHigh = thread.CachedStackBase;
            if (stackLow != 0 && stackHigh > stackLow)
            {
                // The stack grows DOWN: committed pages are contiguous from the current stack
                // pointer up to the base, with reserved-but-uncommitted pages below. Scan pages
                // top-down starting at the base and stop at the first uncommitted (unreadable)
                // page. This is O(committed stack) rather than O(reserved stack), which matters
                // when a thread was created with a very large maxStackSize (e.g. hundreds of MB).
                // Each page is read in ONE bulk read and scanned in-memory, rather than issuing a
                // separate target read per 8-byte slot (which dominated collection time on deep
                // stacks -- hundreds of thousands of individual reads).
                uint8_t pageBuf[StackScanPageSize];
                uint64_t page = (stackHigh - 1) & ~(StackScanPageSize - 1);
                for (;;)
                {
                    uint64_t pageStart = page < stackLow ? stackLow : page;
                    uint64_t pageEnd = page + StackScanPageSize;
                    if (pageEnd > stackHigh)
                    {
                        pageEnd = stackHigh;
                    }
                    uint32_t pageLen = (uint32_t)(pageEnd - pageStart);
                    if (!target.ReadBuffer(pageStart, pageBuf, pageLen))
                    {
                        break; // reached uncommitted stack; nothing deeper is live
                    }

                    // Conservatively scan every pointer-aligned slot in this committed page.
                    for (uint32_t off = 0; off + ptrSize <= pageLen; off += (uint32_t)ptrSize)
                    {
                        uint64_t value = 0;
                        memcpy(&value, pageBuf + off, sizeof(uint64_t));
                        if (value == 0)
                        {
                            continue;
                        }
                        // Skip values that point into this thread's own stack: saved frame pointers
                        // (RBP chain), spilled locals holding stack addresses, etc. These are never
                        // code, and on a deep stack they are the dominant source of distinct values;
                        // filtering them here avoids a radix-map probe per frame.
                        if (value >= stackLow && value < stackHigh)
                        {
                            continue;
                        }
                        // Skip values already resolved on this or a prior thread (recursion pushes
                        // the same return address many times; other slots repeat too).
                        if (enumerator.AlreadyProcessed(value))
                        {
                            continue;
                        }
                        uint64_t methodDesc = enumerator.EnumCodeForIP(value);
                        if (methodDesc != 0)
                        {
                            enumerator.NoteCapturedMethod(methodDesc);
                        }
                    }

                    if (page <= stackLow || page < StackScanPageSize)
                    {
                        break;
                    }
                    page -= StackScanPageSize;
                }
            }

            // Walk the explicit Frame chain (Thread.m_pFrame -> Frame.Next). Transition frames
            // (P/Invoke InlinedCallFrame, FramedMethodFrame, StubDispatchFrame, ...) carry a
            // MethodDesc in the on-stack Frame object rather than as a code return address, so the
            // conservative stack scan above -- which only resolves code IPs -- never captures them.
            // The reader resolves these frames' MethodDescs during the walk, so emit each frame's
            // MethodDesc graph. (The frame objects themselves live on the stack and are already
            // captured by the committed-page scan above.)
            const uint64_t frameTerminator = (ptrSize == 8) ? 0xFFFFFFFFFFFFFFFFull : 0xFFFFFFFFull;
            uint64_t frameAddr = thread.Frame;
            std::set<uint64_t> visitedFrames;
            for (int f = 0; frameAddr != 0 && frameAddr != frameTerminator && f < MaxFrames; f++)
            {
                if (!visitedFrames.insert(frameAddr).second)
                {
                    break; // cycle guard
                }
                uint64_t identifier = 0;
                target.TryReadPointer(frameAddr, identifier); // vtable ptr at offset 0 == Frame.Identifier
                enumerator.EnumFrameMethodDesc(frameAddr, identifier);

                uint64_t next = 0;
                if (!target.TryReadFieldPointer(frameAddr, "Frame", "Next", next))
                {
                    break;
                }
                frameAddr = next;
            }

            threadAddr = thread.LinkNext;
        }

        return (int)enumerator.CapturedMethodCount();
    }
}
} // namespace contracts
