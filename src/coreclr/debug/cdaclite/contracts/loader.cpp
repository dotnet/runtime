// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// loader.cpp
//
// Implementation of the Loader module walk declared in loader.h.
//*****************************************************************************

#include "loader.h"
#include "runtimetypes.h"

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
