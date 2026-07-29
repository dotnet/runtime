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

        // Emits ONLY the PE header page of a managed module's loaded image layout: the DOS header,
        // NT headers, and section table (i.e. [Base, SizeOfHeaders]). This is the minimal
        // information a reader needs to identify the module (COFF TimeDateStamp + OptionalHeader
        // SizeOfImage form the PE identity key) and to parse its section table. Everything else --
        // the COR/R2R headers, ECMA metadata, debug directory, and code -- is deliberately NOT
        // emitted; a reader pages those in from the on-disk assembly, exactly like the DAC does.
        // createdump excludes module image content from heap/mini dumps by design (only Full dumps
        // write m_moduleMappings), so cdac-lite re-emits just this header page. Managed assemblies
        // are PE on every platform, so this parse is valid on Linux/macOS.
        void EmitLoadedImageHeaders(const Target& target, uint64_t base)
        {
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

            // PE signature(4) + COFF header(20) + optional header. SizeOfHeaders lives at optional
            // header offset 60 for both PE32 and PE32+.
            uint8_t peHdr[4 + 20 + 240];
            if (!target.ReadBuffer(base + e_lfanew, peHdr, sizeof(peHdr)) || peHdr[0] != 'P' || peHdr[1] != 'E')
            {
                return;
            }
            uint32_t sizeOfHeaders = 0;
            memcpy(&sizeOfHeaders, peHdr + 4 + 20 + 60, sizeof(sizeOfHeaders));

            // The header page (DOS + NT headers + section table). createdump page-rounds the region,
            // so [Base, SizeOfHeaders] lands as the module's first header page in the dump.
            if (sizeOfHeaders != 0)
            {
                target.EmitMemory(base, sizeOfHeaders);
            }
        }

        // Emit the module's locator chain: Module -> PEAssembly -> PEImage -> PEImageLayout, plus
        // the loaded image's PE header page. The struct reads auto-emit each structure into the
        // dump (via the Target's EnumMem sink), giving a reader the path from a Module to the
        // loaded image base. The reader then pages the ECMA metadata and code in from the on-disk
        // assembly, so only the header page (needed to identify the module) is emitted here.
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
                EmitLoadedImageHeaders(target, loaded.Base);
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
