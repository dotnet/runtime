// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "UnwindHelpers.h"

// A managed frame and its unwind information
struct CompactUnwindEntry
{
    TADDR Start;
    uint32_t Length;
    PTR_uint8_t pLsda;
    const CompactUnwindDescriptor* pDescriptor;
};

// Lookup table of the managed frames of a module, see CompactUnwindTableNode in ILCompiler
class CompactUnwindTable
{
    // A block header, decoded when the table is registered so that lookups don't decode it
    struct Block
    {
        // Offset of the code of the first entry from m_codeBase
        uint32_t Start;
        uint32_t LsdaBase;
        uint32_t BitOffset;
        uint16_t Count;
        // The field widths of the entries minus one, 5 bits each, as in the table
        uint16_t Widths;
    };

    PTR_uint8_t m_pTable = NULL;
    PTR_uint8_t m_pLsdaBase = NULL;
    const uint32_t* m_pDescriptorOffsets = NULL;
    uint32_t m_nDescriptors = 0;

    // Blocks of entries and their bit stream
    TADDR m_codeBase = 0;
    Block* m_pBlocks = NULL;
    uint32_t m_nBlocks = 0;
    PTR_uint8_t m_pStream = NULL;

    // For each page of 1 << m_pageShift bytes of code from m_codeBase, the block that contains its start,
    // followed by the last block
    uint32_t* m_pPageBlocks = NULL;
    uint32_t m_nPages = 0;
    uint32_t m_pageShift = 0;

public:
    CompactUnwindTable* m_pNext = NULL;

    ~CompactUnwindTable();

    bool Initialize(void* pTable, int length);
    bool FindEntry(TADDR controlPC, CompactUnwindEntry* pEntry) const;
};

class UnixNativeCodeManager : public ICodeManager
{
    TADDR m_moduleBase;

    PTR_VOID m_pvManagedCodeStartRange;
    uint32_t m_cbManagedCodeRange;

    PTR_PTR_VOID m_pClasslibFunctions;
    uint32_t m_nClasslibFunctions;

    // Tables of single-file compilations and of the modules of Mach-O multi-file compilations
    CompactUnwindTable* m_pCompactUnwindTables = NULL;

    // Records of ELF multi-file compilations, see CompactUnwindRecordsNode in ILCompiler, and their index
    // sorted by the start of the code when the linker didn't keep them in that order
    PTR_uint8_t m_pCompactUnwindRecordsStart = NULL;
    PTR_uint8_t m_pCompactUnwindRecordsEnd = NULL;
    PTR_uint8_t* m_pCompactUnwindRecordIndex = NULL;
    uint32_t m_nCompactUnwindRecords = 0;

    PTR_uint8_t GetCompactUnwindRecord(uint32_t index);

    bool FindCompactUnwindRecord(TADDR controlPC, CompactUnwindEntry* pEntry);
    bool FindCompactUnwindEntry(TADDR controlPC, CompactUnwindEntry* pEntry);
    bool IsInCompactUnwindProlog(PTR_VOID pvAddress);

    bool GetFunctionBounds(TADDR controlPC, TADDR* pStart, TADDR* pEnd);

    bool VirtualUnwind(MethodInfo* pMethodInfo, REGDISPLAY* pRegisterSet);

public:
    bool AddCompactUnwindTable(void* pTable, int length);
    bool SetCompactUnwindRecords(void* pHeader, int length);

    UnixNativeCodeManager(TADDR moduleBase,
                          PTR_VOID pvManagedCodeStartRange, uint32_t cbManagedCodeRange,
                          PTR_PTR_VOID pClasslibFunctions, uint32_t nClasslibFunctions);

    virtual ~UnixNativeCodeManager();

    //
    // Code manager methods
    //

    bool FindMethodInfo(PTR_VOID        ControlPC,
                        MethodInfo *    pMethodInfoOut);

    bool IsFunclet(MethodInfo * pMethodInfo);

    bool IsFilter(MethodInfo * pMethodInfo);

    PTR_VOID GetFramePointer(MethodInfo *   pMethodInfo,
                             REGDISPLAY *   pRegisterSet);

    uint32_t GetCodeOffset(MethodInfo* pMethodInfo, PTR_VOID address, PTR_uint8_t* gcInfo);

    bool IsSafePoint(MethodInfo * pMethodInfo, PTR_VOID pvAddress);

    void EnumGcRefs(MethodInfo *    pMethodInfo,
                    PTR_VOID        safePointAddress,
                    REGDISPLAY *    pRegisterSet,
                    GCEnumContext * hCallback,
                    bool            isActiveStackFrame);

    bool UnwindStackFrame(MethodInfo *    pMethodInfo,
                          uint32_t        flags,
                          REGDISPLAY *    pRegisterSet,                 // in/out
                          PInvokeTransitionFrame**      ppPreviousTransitionFrame);   // out

    uintptr_t GetConservativeUpperBoundForOutgoingArgs(MethodInfo *   pMethodInfo,
                                                       REGDISPLAY *   pRegisterSet);

    bool IsUnwindable(PTR_VOID pvAddress);

    int TrailingEpilogueInstructionsCount(MethodInfo * pMethodInfo, PTR_VOID pvAddress);

    bool GetReturnAddressHijackInfo(MethodInfo *    pMethodInfo,
                                    REGDISPLAY *    pRegisterSet,       // in
                                    PTR_PTR_VOID *  ppvRetAddrLocation, // out
                                    uintptr_t *     pSpForArm64PacSign);// out

    PTR_VOID RemapHardwareFaultToGCSafePoint(MethodInfo * pMethodInfo, PTR_VOID controlPC);

    bool EHEnumInit(MethodInfo * pMethodInfo, PTR_VOID * pMethodStartAddress, EHEnumState * pEHEnumState);

    bool EHEnumNext(EHEnumState * pEHEnumState, EHClause * pEHClause);

    PTR_VOID GetMethodStartAddress(MethodInfo * pMethodInfo);

    void * GetClasslibFunction(ClasslibFunctionId functionId);

    PTR_VOID GetAssociatedData(PTR_VOID ControlPC);

    PTR_VOID GetOsModuleHandle();
};
