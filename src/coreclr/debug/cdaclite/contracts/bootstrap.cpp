// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// bootstrap.cpp
//
// Implementation of the bootstrap-singleton enumeration declared in bootstrap.h.
//*****************************************************************************

#include "bootstrap.h"
#include "runtimetypes.h"

#include <cstring>

namespace cdac
{
namespace contracts
{
    namespace
    {
        // Emits the runtime module's PE headers + export directory. A reader (ClrMD) resolves the
        // DotNetRuntimeContractDescriptor export from the module's export table to bootstrap; that
        // memory is NOT part of a MiniDumpNormal's default module capture. cdac-lite emits it
        // explicitly so it is self-sufficient (and createdump can exclude the legacy DAC via
        // MiniDumpWithoutAuxiliaryState). Returns the number of regions emitted.
        int EmitModuleExportDirectory(const Target& target, RegionCallback sink, void* sinkContext)
        {
            uint64_t clrBase = target.ClrBase();
            if (clrBase == 0)
            {
                return 0;
            }

            uint8_t dos[0x40];
            if (!target.ReadBuffer(clrBase, dos, sizeof(dos)) || dos[0] != 'M' || dos[1] != 'Z')
            {
                return 0;
            }
            uint32_t e_lfanew = 0;
            memcpy(&e_lfanew, dos + 0x3C, sizeof(e_lfanew));

            // NT headers: Signature(4) + IMAGE_FILE_HEADER(20) + IMAGE_OPTIONAL_HEADER. The data
            // directory array begins at offset 112 (PE32+) or 96 (PE32) within the optional header;
            // the export directory is entry 0 (RVA, then Size).
            uint8_t peHdr[0x18 + 0xF0];
            if (!target.ReadBuffer(clrBase + e_lfanew, peHdr, sizeof(peHdr)) ||
                peHdr[0] != 'P' || peHdr[1] != 'E')
            {
                return 0;
            }
            uint16_t optMagic = 0;
            memcpy(&optMagic, peHdr + 0x18, sizeof(optMagic));
            uint32_t dataDirOffset = 0x18 + ((optMagic == 0x20B) ? 112 : 96);
            uint32_t exportRVA = 0, exportSize = 0;
            memcpy(&exportRVA, peHdr + dataDirOffset, sizeof(exportRVA));
            memcpy(&exportSize, peHdr + dataDirOffset + 4, sizeof(exportSize));

            // PE headers (import/export directory pointers, section table) live in the first page.
            sink(sinkContext, "pe-headers", clrBase, 0x1000);
            int emitted = 1;
            // Export directory + function/name/ordinal tables + name strings (spanned by Size).
            if (exportRVA != 0 && exportSize != 0)
            {
                sink(sinkContext, "export-dir", clrBase + exportRVA, exportSize);
                emitted++;
            }
            return emitted;
        }
    }

    int EnumerateBootstrapRegions(const Target& target, RegionCallback sink, void* sinkContext)
    {
        int emitted = EmitModuleExportDirectory(target, sink, sinkContext);

        // SystemDomain: reading a transition frame (e.g. InlinedCallFrame on a P/Invoke-parked
        // thread) lazily resolves a well-known managed type once, which calls
        // Loader.GetSystemAssembly -> SystemDomain.SystemAssembly. Emit the SystemDomain object.
        uint64_t systemDomainAddr = 0;
        if (target.TryReadGlobalPointer("SystemDomain", systemDomainAddr) && systemDomainAddr != 0)
        {
            target.EmitStruct("SystemDomain", systemDomainAddr);
            emitted++;

            // The global LoaderAllocator is embedded in the SystemDomain (GlobalLoaderAllocator is a
            // field address). SOS dumpdomain (ISOSDacInterface.GetAppDomainData) reads its
            // High/Low/Stub loader-heap pointers, so emit the full LoaderAllocator struct.
            uint64_t globalLoaderAllocator = 0;
            if (target.TryGetFieldAddress(systemDomainAddr, "SystemDomain", "GlobalLoaderAllocator", globalLoaderAllocator) &&
                globalLoaderAllocator != 0)
            {
                target.EmitStruct("LoaderAllocator", globalLoaderAllocator);
                emitted++;
            }
        }

        // PlatformMetadata: resolving an R2R runtime function reads the cDAC platform metadata
        // (IPlatformMetadata.GetCodePointerFlags). This global holds the struct address directly
        // (like the code range map), so read it with TryGetGlobalValue -- no extra deref.
        uint64_t platformMetadataAddr = 0;
        if (target.TryGetGlobalValue("PlatformMetadata", platformMetadataAddr) && platformMetadataAddr != 0)
        {
            target.EmitStruct("PlatformMetadata", platformMetadataAddr);
            // The PrecodeMachineDescriptor is embedded at the start of the PlatformMetadata; the
            // MethodDesc validation reads it to identify precode stubs.
            target.EmitStruct("PrecodeMachineDescriptor", platformMetadataAddr);
            emitted++;
        }

        // Debugger data: the stack walk's hijack check (IDebugger.GetHijackKind, called per frame)
        // reads g_pDebugger. The DAC's skinny enumeration emits it via g_pDebugger->EnumMemoryRegions;
        // cdac-lite emits it here so the walk works without the DAC.
        uint64_t debuggerAddr = 0;
        if (target.TryReadGlobalPointer("Debugger", debuggerAddr) && debuggerAddr != 0)
        {
            target.EmitStruct("Debugger", debuggerAddr);
            emitted++;
        }

        return emitted;
    }
}
} // namespace contracts
