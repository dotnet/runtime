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

        bool IsValidImageRange(uint32_t imageSize, uint32_t rva, uint32_t size)
        {
            return rva <= imageSize && size <= imageSize - rva;
        }

        // Emit the loaded PE headers, CLI header, and ECMA metadata. Keeping these bytes in the dump
        // avoids depending on a dump reader to distinguish the runtime's flat and loaded mappings
        // when paging managed images from disk on Unix.
        void EmitLoadedImageMetadata(const Target& target, const data::PEImageLayout& loaded)
        {
            uint64_t base = loaded.Base;
            uint32_t imageSize = loaded.Size;
            if (base == 0)
            {
                return;
            }

            uint8_t dos[0x40]; // sizeof(IMAGE_DOS_HEADER)
            if (!target.ReadBuffer(base, dos, sizeof(dos)) || dos[0] != 'M' || dos[1] != 'Z')
            {
                return;
            }
            uint32_t e_lfanew = 0;
            memcpy(&e_lfanew, dos + 0x3C, sizeof(e_lfanew));

            // PE signature(4) + COFF header(20) + optional header including all data directories.
            uint8_t peHdr[4 + 20 + 240];
            if (!target.ReadBuffer(base + e_lfanew, peHdr, sizeof(peHdr)) || peHdr[0] != 'P' || peHdr[1] != 'E')
            {
                return;
            }
            const uint32_t optionalHeaderOffset = 4 + 20;
            uint16_t optionalHeaderMagic = 0;
            memcpy(&optionalHeaderMagic, peHdr + optionalHeaderOffset, sizeof(optionalHeaderMagic));

            uint32_t sizeOfHeaders = 0;
            memcpy(&sizeOfHeaders, peHdr + optionalHeaderOffset + 60, sizeof(sizeOfHeaders));

            if (sizeOfHeaders != 0 && sizeOfHeaders <= imageSize)
            {
                target.EmitMemory(base, sizeOfHeaders);
            }

            uint32_t dataDirectoryOffset = optionalHeaderOffset + ((optionalHeaderMagic == 0x20B) ? 112 : 96);
            uint32_t corHeaderRva = 0;
            uint32_t corHeaderSize = 0;
            memcpy(&corHeaderRva, peHdr + dataDirectoryOffset + 14 * 8, sizeof(corHeaderRva));
            memcpy(&corHeaderSize, peHdr + dataDirectoryOffset + 14 * 8 + 4, sizeof(corHeaderSize));
            if (corHeaderRva == 0 || corHeaderSize < 16 ||
                !IsValidImageRange(imageSize, corHeaderRva, corHeaderSize))
            {
                return;
            }

            target.EmitMemory(base + corHeaderRva, corHeaderSize);

            uint8_t corHeader[16];
            if (!target.ReadBuffer(base + corHeaderRva, corHeader, sizeof(corHeader)))
            {
                return;
            }

            uint32_t metadataRva = 0;
            uint32_t metadataSize = 0;
            memcpy(&metadataRva, corHeader + 8, sizeof(metadataRva));
            memcpy(&metadataSize, corHeader + 12, sizeof(metadataSize));
            if (metadataRva != 0 && metadataSize != 0 &&
                IsValidImageRange(imageSize, metadataRva, metadataSize))
            {
                target.EmitMemory(base + metadataRva, metadataSize);
            }
        }

        // Emit the module's locator chain: Module -> PEAssembly -> PEImage -> PEImageLayout, plus
        // the loaded image's PE headers and metadata. The struct reads auto-emit each structure into the
        // dump (via the Target's EnumMem sink), giving a reader the path from a Module to the
        // loaded image base.
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
                EmitLoadedImageMetadata(target, loaded);
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
                    if (r2r.DelayLoadMethodCallThunks != 0)
                    {
                        target.EmitStruct("ImageDataDirectory", r2r.DelayLoadMethodCallThunks);
                    }
                    if (r2r.HotColdMap != 0 && r2r.NumHotColdMap != 0 &&
                        r2r.NumHotColdMap <= UINT32_MAX / sizeof(uint32_t))
                    {
                        target.EmitMemory(r2r.HotColdMap, r2r.NumHotColdMap * sizeof(uint32_t));
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
