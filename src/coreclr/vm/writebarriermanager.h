// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ===========================================================================
// File: writebarriermanager.h
// ===========================================================================


#ifndef WRITEBARRIERMANAGER_H
#define WRITEBARRIERMANAGER_H

#if defined(TARGET_AMD64) || defined(TARGET_ARM64)


#if defined(TARGET_AMD64)
// Write barrier variables are inlined into the assembly code
#define WRITE_BARRIER_VARS_INLINE
// Else: Write barrier variables are in a table separate to the asm code
#endif

class WriteBarrierManager
{
public:
    enum WriteBarrierType
    {
        WRITE_BARRIER_UNINITIALIZED,
        WRITE_BARRIER_PREGROW64,
        WRITE_BARRIER_POSTGROW64,
#ifdef FEATURE_SVR_GC
        WRITE_BARRIER_SVR64,
#endif // FEATURE_SVR_GC
        WRITE_BARRIER_BYTE_REGIONS64,
        WRITE_BARRIER_BIT_REGIONS64,
#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        WRITE_BARRIER_WRITE_WATCH_PREGROW64,
        WRITE_BARRIER_WRITE_WATCH_POSTGROW64,
#ifdef FEATURE_SVR_GC
        WRITE_BARRIER_WRITE_WATCH_SVR64,
#endif // FEATURE_SVR_GC
        WRITE_BARRIER_WRITE_WATCH_BYTE_REGIONS64,
        WRITE_BARRIER_WRITE_WATCH_BIT_REGIONS64,
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        WRITE_BARRIER_BUFFER
    };

    WriteBarrierManager();
    void Initialize();

    int UpdateEphemeralBounds(bool isRuntimeSuspended);
    int UpdateWriteWatchAndCardTableLocations(bool isRuntimeSuspended, bool bReqUpperBoundsCheck);

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    int SwitchToWriteWatchBarrier(bool isRuntimeSuspended);
    int SwitchToNonWriteWatchBarrier(bool isRuntimeSuspended);
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    size_t GetCurrentWriteBarrierSize();

private:
    size_t GetSpecificWriteBarrierSize(WriteBarrierType writeBarrier);
    PCODE  GetCurrentWriteBarrierCode();
    int    ChangeWriteBarrierTo(WriteBarrierType newWriteBarrier, bool isRuntimeSuspended);
    bool   NeedDifferentWriteBarrier(bool bReqUpperBoundsCheck, bool bUseBitwiseWriteBarrier, WriteBarrierType* pNewWriteBarrierType);


#if defined(WRITE_BARRIER_VARS_INLINE)
    PBYTE  CalculatePatchLocation(LPVOID base, LPVOID label, int offset);
    void Validate();
    void UpdatePatchLocations(WriteBarrierType newWriteBarrier);
#endif // WRITE_BARRIER_VARS_INLINE


    WriteBarrierType    m_currentWriteBarrier;

    PBYTE   m_pWriteWatchTableImmediate;    // PREGROW | POSTGROW | SVR | WRITE_WATCH | REGION
    PBYTE   m_pLowerBoundImmediate;         // PREGROW | POSTGROW |     | WRITE_WATCH | REGION
    PBYTE   m_pCardTableImmediate;          // PREGROW | POSTGROW | SVR | WRITE_WATCH | REGION
    PBYTE   m_pCardBundleTableImmediate;    // PREGROW | POSTGROW | SVR | WRITE_WATCH | REGION
    PBYTE   m_pUpperBoundImmediate;         //         | POSTGROW |     | WRITE_WATCH | REGION
    PBYTE   m_pRegionToGenTableImmediate;   //         |          |     | WRITE_WATCH | REGION
    PBYTE   m_pRegionShrDest;               //         |          |     | WRITE_WATCH | REGION
    PBYTE   m_pRegionShrSrc;                //         |          |     | WRITE_WATCH | RETION

#if defined(TARGET_ARM64)
    PBYTE   m_lowestAddress;
    PBYTE   m_highestAddress;
#if defined(WRITE_BARRIER_CHECK)
    PBYTE   m_pGCShadow;
    PBYTE   m_pGCShadowEnd;
#endif // WRITE_BARRIER_CHECK
#endif // TARGET_AMD64

};

extern WriteBarrierManager g_WriteBarrierManager;

#endif // TARGET_AMD64 || TARGET_ARM64

#endif // WRITEBARRIERMANAGER_H
