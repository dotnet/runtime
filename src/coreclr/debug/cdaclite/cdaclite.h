// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// cdaclite.h
//
// cdac-lite: native crash-dump memory enumeration backed by the runtime's
// contract descriptor, without a version-matched mscordaccore.
//
// The implementation is split by concern:
//   * datatarget.{h,cpp} -- the data-target source (memory reads / symbol lookup)
//   * enumerate.cpp       -- descriptor discovery and memory enumeration
//*****************************************************************************

#ifndef CDACLITE_CDACLITE_H
#define CDACLITE_CDACLITE_H

#include <windows.h>
#include <clrdata.h>
#include <stdint.h>

namespace cdac
{
    typedef HRESULT (*MemoryRegionCallback)(void* context, uint64_t address, uint32_t size);
    typedef void (*LoggingCallback)(void* context, const char* message);

    HRESULT EnumerateMemoryRegions(
        ICLRDataTarget* target,
        uint64_t clrBase,
        ULONG32 miniDumpFlags,
        MemoryRegionCallback regionCallback,
        void* regionContext,
        LoggingCallback loggingCallback = nullptr,
        void* loggingContext = nullptr);
}

#endif // CDACLITE_CDACLITE_H
