// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// loader.cpp
//
// Implementation of the Loader module walk declared in loader.h.
//*****************************************************************************

#include "loader.h"
#include "runtimetypes.h"

#include <cstring>
#include <set>

namespace cdac
{
namespace contracts
{
    namespace
    {
        // Global pointing at the AppDomain*: &AppDomain::m_pTheAppDomain.
        const char* const GlobalAppDomain = "AppDomain";

        const int MaxBlocks = 1024;         // guard against corrupt block lists
        const uint32_t MaxAssemblies = 1u << 20;
    }

    int ForEachModule(const Target& target, ModuleCallback callback, void* context)
    {
        // AppDomain global is a pointer-to-pointer: deref once to get the AppDomain.
        uint64_t appDomainAddr = 0;
        if (!target.TryReadGlobalPointer(GlobalAppDomain, appDomainAddr) || appDomainAddr == 0)
        {
            return -1;
        }

        data::AppDomain appDomain;
        if (!target.TryRead(appDomainAddr, appDomain))
        {
            return -1;
        }

        // AssemblyList is an embedded ArrayListBase (a block list of Assembly*).
        data::ArrayListBase list;
        if (!target.TryRead(appDomain.AssemblyList, list))
        {
            return -1;
        }

        uint32_t total = list.Count;
        if (total > MaxAssemblies)
        {
            total = MaxAssemblies;
        }

        int visitedModules = 0;
        uint32_t seen = 0;
        uint64_t blockAddr = list.FirstBlock;

        for (int b = 0; blockAddr != 0 && b < MaxBlocks && seen < total; b++)
        {
            data::ArrayListBlock block;
            if (!target.TryRead(blockAddr, block))
            {
                break;
            }

            // The block's inline Assembly* array (ArrayStart, Size pointers) extends past the fixed
            // ArrayListBlock struct, so emit it explicitly -- the reader re-reads it to enumerate the
            // domain's assemblies (ISOSDacInterface.GetAppDomainData / SOS dumpdomain).
            target.EmitMemory(block.ArrayStart, (uint32_t)(block.Size * target.PointerSize()));

            for (uint32_t i = 0; i < block.Size && seen < total; i++)
            {
                seen++;
                uint64_t assemblyAddr = 0;
                if (!target.TryReadPointer(block.ArrayStart + (uint64_t)i * target.PointerSize(), assemblyAddr) ||
                    assemblyAddr == 0)
                {
                    continue;
                }

                data::Assembly assembly;
                if (!target.TryRead(assemblyAddr, assembly) || assembly.Module == 0)
                {
                    continue;
                }

                callback(context, assembly.Module);
                visitedModules++;
            }

            blockAddr = block.Next;
        }

        return visitedModules;
    }

    namespace
    {
        struct ModuleImageState
        {
            const Target* target;
            std::set<uint64_t> visited;
            RegionCallback sink;
            void* sinkContext;
            int emitted;
        };

        // PE structure sizes on the target. The DAC emits each of these as a fixed struct via
        // DPTR::EnumMem(), so we mirror the exact sizes rather than a coarse [Base, SizeOfHeaders].
        constexpr uint32_t kDosHeaderSize = 0x40;            // sizeof(IMAGE_DOS_HEADER)
        constexpr uint32_t kSectionHeaderSize = 40;          // sizeof(IMAGE_SECTION_HEADER)
        constexpr uint32_t kCor20HeaderSize = 72;            // sizeof(IMAGE_COR20_HEADER)
        constexpr uint32_t kReadyToRunHeaderSize = 16;       // sizeof(READYTORUN_HEADER)
        constexpr uint32_t kDebugDirEntrySize = 28;          // sizeof(IMAGE_DEBUG_DIRECTORY)
        constexpr uint32_t kReadyToRunSignature = 0x00525452; // 'RTR\0'
        constexpr uint32_t kMaxSections = 96;

        // Translates an RVA to a target address within an image layout. For a mapped image
        // (FLAG_MAPPED) RVA == offset-from-base. For a flat (raw file) image the RVA must be
        // converted to a file offset by walking the section table, matching PEDecoder's
        // RvaToAddr for a flat layout (and Loader_1.RvaToOffset in the managed cDAC).
        uint64_t RvaToAddr(uint64_t base, uint32_t rva, bool isMapped, const uint8_t* sections, uint32_t numSections)
        {
            if (isMapped)
            {
                return base + rva;
            }
            for (uint32_t i = 0; i < numSections; i++)
            {
                const uint8_t* s = sections + (size_t)i * kSectionHeaderSize;
                uint32_t va = 0, rawSize = 0, rawPtr = 0;
                memcpy(&va, s + 12, sizeof(va));       // VirtualAddress
                memcpy(&rawSize, s + 16, sizeof(rawSize)); // SizeOfRawData
                memcpy(&rawPtr, s + 20, sizeof(rawPtr));   // PointerToRawData
                if (rva >= va && rva < va + rawSize)
                {
                    return base + (rva - va) + rawPtr;
                }
            }
            return base + rva;
        }

        // Mirrors PEDecoder::EnumMemoryRegions for a single image layout (pedecoder.cpp): emits the
        // DOS header, NT headers, section table, COR (CLI) header, and R2R header -- the exact regions
        // the legacy DAC records. When emitDebugDir is set (the loaded layout, per
        // PEImage::EnumMemoryRegions) it also emits the debug directory and the blobs its entries
        // point to (used to locate managed PDBs). The ECMA metadata and code bytes are deliberately
        // NOT emitted; a reader reads those from the on-disk image, exactly like the DAC. Managed
        // assemblies are PE on every platform, so this PE parse is valid on Linux/macOS.
        void EmitPEDecoderRegions(const Target& target, uint64_t base, uint32_t flags, bool emitDebugDir)
        {
            if (base == 0)
            {
                return;
            }
            const bool isMapped = (flags & 0x1) != 0; // FLAG_MAPPED

            uint8_t dos[kDosHeaderSize];
            if (!target.ReadBuffer(base, dos, sizeof(dos)) || dos[0] != 'M' || dos[1] != 'Z')
            {
                return;
            }
            uint32_t e_lfanew = 0;
            memcpy(&e_lfanew, dos + 0x3C, sizeof(e_lfanew));

            // PE signature(4) + COFF header(20) + optional header (PE32+: 240). Covers all data dirs.
            uint8_t peHdr[4 + 20 + 240];
            if (!target.ReadBuffer(base + e_lfanew, peHdr, sizeof(peHdr)) || peHdr[0] != 'P' || peHdr[1] != 'E')
            {
                return;
            }

            uint16_t numberOfSections = 0;
            memcpy(&numberOfSections, peHdr + 4 + 2, sizeof(numberOfSections));     // IMAGE_FILE_HEADER.NumberOfSections
            uint16_t sizeOfOptionalHeader = 0;
            memcpy(&sizeOfOptionalHeader, peHdr + 4 + 16, sizeof(sizeOfOptionalHeader)); // IMAGE_FILE_HEADER.SizeOfOptionalHeader
            const uint32_t optOffset = 4 + 20; // within peHdr
            uint16_t optMagic = 0;
            memcpy(&optMagic, peHdr + optOffset, sizeof(optMagic));
            const bool isPE32Plus = (optMagic == 0x20B);

            // DOS header + NT headers, exactly as PEDecoder::EnumMemoryRegions records them
            // (DacEnumMemoryRegion(m_base, sizeof(IMAGE_DOS_HEADER)) then m_pNTHeaders.EnumMem()).
            // createdump excludes module image content from heap dumps by design -- only Full dumps
            // write m_moduleMappings (crashinfo.cpp GatherCrashInfo) -- so, just like the DAC, we
            // re-emit these header structures into the dump; the ECMA metadata and code are read from
            // the on-disk image. createdump page-rounds each emitted region, so these discrete regions
            // land as the module's first header page.
            target.EmitMemory(base, kDosHeaderSize);
            target.EmitMemory(base + e_lfanew, 4 + 20 + (isPE32Plus ? 240u : 224u)); // sizeof(IMAGE_NT_HEADERS)

            // Section table (immediately after the optional header) -- also read for RVA->offset mapping.
            const uint64_t firstSection = base + e_lfanew + optOffset + sizeOfOptionalHeader;
            uint32_t numSec = numberOfSections;
            if (numSec > kMaxSections)
            {
                numSec = kMaxSections;
            }
            uint8_t sections[kMaxSections * kSectionHeaderSize];
            if (numSec != 0 && !target.ReadBuffer(firstSection, sections, kSectionHeaderSize * numSec))
            {
                numSec = 0;
            }
            if (numSec != 0)
            {
                target.EmitMemory(firstSection, kSectionHeaderSize * numSec);
            }

            const uint32_t dataDirOffset = optOffset + (isPE32Plus ? 112 : 96);

            // COR (CLI) header (data directory 14 = IMAGE_DIRECTORY_ENTRY_COMHEADER).
            uint32_t corRVA = 0;
            memcpy(&corRVA, peHdr + dataDirOffset + 14 * 8, sizeof(corRVA));
            if (corRVA != 0)
            {
                uint64_t corAddr = RvaToAddr(base, corRVA, isMapped, sections, numSec);
                target.EmitMemory(corAddr, kCor20HeaderSize);

                // R2R header via the COR header's ManagedNativeHeader directory (RVA @+64, Size @+68).
                uint8_t cor[kCor20HeaderSize];
                if (target.ReadBuffer(corAddr, cor, sizeof(cor)))
                {
                    uint32_t r2rRVA = 0, r2rSize = 0;
                    memcpy(&r2rRVA, cor + 64, sizeof(r2rRVA));
                    memcpy(&r2rSize, cor + 68, sizeof(r2rSize));
                    if (r2rRVA != 0 && r2rSize >= kReadyToRunHeaderSize)
                    {
                        uint64_t r2rAddr = RvaToAddr(base, r2rRVA, isMapped, sections, numSec);
                        uint32_t sig = 0;
                        if (target.ReadBuffer(r2rAddr, &sig, sizeof(sig)) && sig == kReadyToRunSignature)
                        {
                            target.EmitMemory(r2rAddr, kReadyToRunHeaderSize);
                        }
                    }
                }
            }

            // Debug directory (data directory 6): loaded layout only, matching PEImage::EnumMemoryRegions.
            // Emits the directory plus each entry's raw data (e.g. CodeView records for managed PDBs).
            if (emitDebugDir)
            {
                uint32_t dbgRVA = 0, dbgSize = 0;
                memcpy(&dbgRVA, peHdr + dataDirOffset + 6 * 8, sizeof(dbgRVA));
                memcpy(&dbgSize, peHdr + dataDirOffset + 6 * 8 + 4, sizeof(dbgSize));
                if (dbgRVA != 0 && dbgSize != 0)
                {
                    uint64_t dbgAddr = RvaToAddr(base, dbgRVA, isMapped, sections, numSec);
                    target.EmitMemory(dbgAddr, dbgSize);
                    for (uint32_t i = 0; i < dbgSize / kDebugDirEntrySize; i++)
                    {
                        uint8_t entry[kDebugDirEntrySize];
                        if (!target.ReadBuffer(dbgAddr + (uint64_t)i * kDebugDirEntrySize, entry, sizeof(entry)))
                        {
                            break;
                        }
                        uint32_t sizeOfData = 0, addrOfRawData = 0;
                        memcpy(&sizeOfData, entry + 16, sizeof(sizeOfData));    // IMAGE_DEBUG_DIRECTORY.SizeOfData
                        memcpy(&addrOfRawData, entry + 20, sizeof(addrOfRawData)); // .AddressOfRawData (RVA)
                        if (addrOfRawData != 0 && sizeOfData != 0)
                        {
                            target.EmitMemory(RvaToAddr(base, addrOfRawData, isMapped, sections, numSec), sizeOfData);
                        }
                    }
                }
            }
        }

        // Emit the module's metadata-locator chain: Module -> PEAssembly -> PEImage ->
        // PEImageLayout, plus the PE header regions of the image, matching the legacy DAC.
        // PEImage::EnumMemoryRegions (peimage.cpp) enumerates BOTH m_pLayouts[IMAGE_FLAT] and
        // [IMAGE_LOADED] and additionally emits the debug directory for the loaded layout, so we
        // do the same. The reader then reads ECMA metadata from the on-disk image via these headers.
        void EmitModuleMetadataChain(const Target& target, const data::Module& module)
        {
            if (module.PEAssembly == 0)
            {
                return;
            }
            data::PEAssembly peAssembly;
            if (!target.TryRead(module.PEAssembly, peAssembly) || peAssembly.PEImage == 0)
            {
                return;
            }
            data::PEImage peImage;
            if (!target.TryRead(peAssembly.PEImage, peImage) || peImage.LoadedImageLayout == 0)
            {
                return;
            }
            data::PEImageLayout loaded;
            if (target.TryRead(peImage.LoadedImageLayout, loaded)) // struct read -> auto-emitted
            {
                EmitPEDecoderRegions(target, loaded.Base, (uint32_t)loaded.Flags, /*emitDebugDir*/ true);
            }
            if (peImage.FlatImageLayout != 0 && peImage.FlatImageLayout != peImage.LoadedImageLayout)
            {
                data::PEImageLayout flat;
                if (target.TryRead(peImage.FlatImageLayout, flat)) // struct read -> auto-emitted
                {
                    EmitPEDecoderRegions(target, flat.Base, (uint32_t)flat.Flags, /*emitDebugDir*/ false);
                }
            }
        }

        // Iterates modules and captures only the memory that is NOT available from the on-disk
        // binaries: in-memory symbol (PDB) streams. The DAC does not dump file-backed module
        // images -- the analyzer re-reads image bytes (code, R2R, ECMA metadata) from the binary
        // on disk -- so cdac-lite doesn't either, keeping dumps DAC-sized.
        void EmitModuleExtras(void* context, uint64_t moduleAddr)
        {
            ModuleImageState* state = (ModuleImageState*)context;
            const Target& target = *state->target;

            data::Module module;
            if (!target.TryRead(moduleAddr, module))
            {
                return;
            }

            // Emit the module's metadata-locator chain and the ECMA metadata bytes.
            EmitModuleMetadataChain(target, module);

            // If the module has an in-memory symbol stream, capture its buffer (ILoader.TryGetSymbolStream).
            // In-memory PDBs have no on-disk backing, so they must be in the dump.
            if (module.GrowableSymbolStream != 0)
            {
                data::CGrowableSymbolStream symStream;
                if (target.TryRead(module.GrowableSymbolStream, symStream) &&
                    symStream.Buffer != 0 && (uint32_t)symStream.Size != 0)
                {
                    target.EmitMemory(symStream.Buffer, (uint32_t)symStream.Size);
                    state->emitted++;
                }
            }

            // ReadyToRun modules: resolving an R2R frame (ExecutionManager.GetCodeBlockHandle ->
            // ReadyToRunJitManager) reads ReadyToRunInfo -> ReadyToRunHeader / DebugInfoSection.
            // The RuntimeFunctions table it indexes lives in the on-disk image, but these runtime
            // locator structs must be in the dump.
            if (module.ReadyToRunInfo != 0)
            {
                data::ReadyToRunInfo r2r;
                if (target.TryRead(module.ReadyToRunInfo, r2r))
                {
                    if (r2r.ReadyToRunHeader != 0)
                    {
                        target.EmitStruct("ReadyToRunHeader", r2r.ReadyToRunHeader);
                    }
                    if (r2r.DebugInfoSection != 0)
                    {
                        target.EmitStruct("ImageDataDirectory", r2r.DebugInfoSection);
                    }
                    if (r2r.CompositeInfo != 0 && r2r.CompositeInfo != module.ReadyToRunInfo)
                    {
                        target.EmitStruct("ReadyToRunInfo", r2r.CompositeInfo);
                    }
                    state->emitted++;
                }
            }
        }
    }

    int EnumerateModuleRegions(const Target& target, RegionCallback sink, void* sinkContext)
    {
        ModuleImageState state;
        state.target = &target;
        state.sink = sink;
        state.sinkContext = sinkContext;
        state.emitted = 0;

        if (ForEachModule(target, &EmitModuleExtras, &state) < 0)
        {
            return -1;
        }
        return state.emitted;
    }
}
} // namespace contracts
