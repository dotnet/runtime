// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// cdaclite.h
//
// cdac-lite: a minimal data access component implementing just enough of the
// classic DAC entry point (CLRDataCreateInstance + ICLRDataEnumMemoryRegions)
// to drive crash-dump memory enumeration without a version-matched mscordaccore.
//
// The implementation is split by concern:
//   * datatarget.{h,cpp} -- the data-target source (memory reads / symbol lookup)
//   * cdaclite.cpp        -- creation (CLRDataCreateInstance) + COM object lifetime
//   * enumerate.cpp       -- the memory enumeration (EnumMemoryRegions + contracts)
//*****************************************************************************

#ifndef CDACLITE_CDACLITE_H
#define CDACLITE_CDACLITE_H

#include <windows.h>
#include <clrdata.h>
#include <stdint.h>
#include <stdarg.h>

namespace cdac
{
    // Minimal ICLRDataEnumMemoryRegions implementation backed by the runtime's
    // contract descriptor. Created by CLRDataCreateInstance (cdaclite.cpp); the
    // enumeration itself lives in enumerate.cpp.
    class CDacLite : public ICLRDataEnumMemoryRegions
    {
    public:
        CDacLite(ICLRDataTarget* target, uint64_t contractDescriptorAddr);

        // IUnknown
        STDMETHOD(QueryInterface)(REFIID riid, void** ppvObject) override;
        STDMETHOD_(ULONG, AddRef)() override;
        STDMETHOD_(ULONG, Release)() override;

        // ICLRDataEnumMemoryRegions (implemented in enumerate.cpp)
        STDMETHOD(EnumMemoryRegions)(ICLRDataEnumMemoryRegionsCallback* callback, ULONG32 miniDumpFlags, CLRDataEnumMemoryFlags clrFlags) override;

    private:
        virtual ~CDacLite();

        // Logs to the callback's ICLRDataLoggingCallback if present, else to stderr.
        void Log(ICLRDataEnumMemoryRegionsCallback* callback, const char* format, ...);

        // State shared with the region sinks while an enumeration is in progress.
        struct RegionSinkState
        {
            CDacLite* owner;
            ICLRDataEnumMemoryRegionsCallback* callback;
            uint32_t count;
        };

        // Region sink (contract-reported regions) + EnumMem sink (implicitly-read structs).
        static void RegionSinkThunk(void* context, const char* kind, uint64_t start, uint64_t size);
        static void EnumMemThunk(void* context, uint64_t address, uint32_t size);

        LONG m_ref;
        ICLRDataTarget* m_target;
        uint64_t m_contractDescriptorAddr;
    };
}

#endif // CDACLITE_CDACLITE_H
