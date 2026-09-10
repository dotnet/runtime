// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef WRITEBARRIERSERVICES_H
#define WRITEBARRIERSERVICES_H

#include "../gc/writebarrier.h"

enum WriteBarrierPatch : uint8_t
{
    WRITE_BARRIER_PATCH_WRITE_WATCH_TABLE,
    WRITE_BARRIER_PATCH_REGION_TO_GENERATION,
    WRITE_BARRIER_PATCH_REGION_SHR_DEST,
    WRITE_BARRIER_PATCH_REGION_SHR_SRC,
    WRITE_BARRIER_PATCH_LOWER,
    WRITE_BARRIER_PATCH_UPPER,
    WRITE_BARRIER_PATCH_CARD_TABLE,
    WRITE_BARRIER_PATCH_CARD_BUNDLE_TABLE,
    WRITE_BARRIER_PATCH_LOWEST_ADDRESS,
    WRITE_BARRIER_PATCH_HIGHEST_ADDRESS,
    WRITE_BARRIER_PATCH_GC_SHADOW,
    WRITE_BARRIER_PATCH_GC_SHADOW_END
};

class WriteBarrierServices final
{
public:
    WriteBarrierServices();
    void PublishWriteBarrierHelpers();

    WriteBarrierCodeDescriptor GetWriteBarrierCode(WriteBarrierType writeBarrier);
    WriteBarrierPatchLocations GetWriteBarrierPatchLocations(WriteBarrierType writeBarrier);
    void ValidateWriteBarrierLayout();
    bool UpdateWriteBarrierImplementationState(const WriteBarrierParameters& args);

private:
    uint8_t* GetExecutableAddress(uint8_t* source);
    uint8_t* GetWriteBarrierPatchLocation(WriteBarrierType writeBarrier, WriteBarrierPatch patch);
    uint8_t* GetValidatedWriteBarrierPatchLocation(WriteBarrierType writeBarrier, WriteBarrierPatch patch);
    bool UpdateWriteBarrierPointer(WriteBarrierPatch patch, uint8_t* value);

    uint8_t* m_codeCopy;
    uint8_t* m_codeStart;
};

class WriteBarrierRuntimeModeHolder
{
public:
    explicit WriteBarrierRuntimeModeHolder(bool enterMode);
    ~WriteBarrierRuntimeModeHolder();

private:
    bool m_modeChanged;
};

extern WriteBarrierServices g_writeBarrierServices;

#endif // WRITEBARRIERSERVICES_H
