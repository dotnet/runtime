// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// DebugInfoStore




#ifndef __DebugInfoStore_H_
#define __DebugInfoStore_H_

// Debugging information is described in CorInfo.h
#include "corinfo.h"

#include "nibblestream.h"

//-----------------------------------------------------------------------------
// Information to request Debug info.
//-----------------------------------------------------------------------------
class DebugInfoRequest
{
public:
#ifdef _DEBUG
    // Must initialize via an Init*() function, not just a ctor.
    // In debug, ctor sets fields to values that will cause asserts if not initialized.
    DebugInfoRequest()
    {
        SUPPORTS_DAC;
        m_pMD = NULL;
        m_addrStart = NULL;
    }
#endif
    // Eventually we may have many ways to initialize a request.

    // Init given a method desc and starting address for a native code blob.
    void InitFromStartingAddr(MethodDesc * pDesc, PCODE addrCode);


    MethodDesc * GetMD() const { LIMITED_METHOD_DAC_CONTRACT; return m_pMD; }
    PCODE GetStartAddress() const { LIMITED_METHOD_DAC_CONTRACT; return m_addrStart; }

protected:
    MethodDesc * m_pMD;
    PCODE        m_addrStart;

};

//-----------------------------------------------------------------------------
// A Debug-Info Store abstracts the storage of debugging information
//-----------------------------------------------------------------------------


// We pass the IDS an allocator which it uses to hand the data back.
// pData is data the allocator may use for 'new'.
// Eg, perhaps we have multiple heaps (eg, loader-heaps per appdomain).
typedef BYTE* (*FP_IDS_NEW)(void * pData, size_t cBytes);


//-----------------------------------------------------------------------------
// Utility routines used for compression
// Note that the compression is just an implementation detail of the stores,
// and so these are just utility routines exposed to the stores.
//-----------------------------------------------------------------------------
class CompressDebugInfo
{
    // Compress incoming data and write it to the provided NibbleWriter.
    static void CompressBoundaries(
        IN ULONG32                       cMap,
        IN ICorDebugInfo::OffsetMapping *pMap,
        IN OUT NibbleWriter * pWriter
    );

    static void CompressVars(
        IN ULONG32                         cVars,
        IN ICorDebugInfo::NativeVarInfo    *vars,
        IN OUT NibbleWriter * pBuffer
    );

    static void CompressRichDebugInfo(
        IN ULONG32                           cInlineTree,
        IN ICorDebugInfo::InlineTreeNode*    pInlineTree,
        IN ULONG32                           cRichOffsetMappings,
        IN ICorDebugInfo::RichOffsetMapping* pRichOffsetMappings,
        IN OUT NibbleWriter*                 pWriter);

public:
    // Stores the result in LoaderHeap
    static PTR_BYTE CompressBoundariesAndVars(
        IN ICorDebugInfo::OffsetMapping * pOffsetMapping,
        IN ULONG            iOffsetMapping,
        IN ICorDebugInfo::NativeVarInfo * pNativeVarInfo,
        IN ULONG            iNativeVarInfo,
        IN PatchpointInfo * patchpointInfo,
        IN ICorDebugInfo::InlineTreeNode * pInlineTree,
        IN ULONG            iInlineTree,
        IN ICorDebugInfo::RichOffsetMapping * pRichOffsetMappings,
        IN ULONG            iRichOffsetMappings,
        IN BOOL             writeFlagByte,
        IN LoaderHeap     * pLoaderHeap
    );

    // Uncompress data supplied by Compress functions.
    static void RestoreBoundariesAndVars(
        IN FP_IDS_NEW fpNew, IN void * pNewData,
        IN PTR_BYTE                         pDebugInfo,
        OUT ULONG32                       * pcMap, // number of entries in ppMap
        OUT ICorDebugInfo::OffsetMapping **ppMap, // pointer to newly allocated array
        OUT ULONG32                         *pcVars,
        OUT ICorDebugInfo::NativeVarInfo    **ppVars,
        BOOL hasFlagByte
    );

#ifdef FEATURE_ON_STACK_REPLACEMENT
    static PatchpointInfo * RestorePatchpointInfo(
        IN PTR_BYTE pDebugInfo
    );
#endif

    static void RestoreRichDebugInfo(
        IN FP_IDS_NEW                          fpNew,
        IN void*                               pNewData,
        IN PTR_BYTE                            pDebugInfo,
        OUT ICorDebugInfo::InlineTreeNode**    ppInlineTree,
        OUT ULONG32*                           pNumInlineTree,
        OUT ICorDebugInfo::RichOffsetMapping** ppRichMappings,
        OUT ULONG32*                           pNumRichMappings);

#ifdef DACCESS_COMPILE
    static void EnumMemoryRegions(CLRDataEnumMemoryFlags flags, PTR_BYTE pDebugInfo, BOOL hasFlagByte);
#endif
};

//-----------------------------------------------------------------------------
// Debug-Info-manager. This is like a process-wide store.
// There should be only 1 instance of this and it's process-wide.
// It will delegate to sub-stores as needed
//-----------------------------------------------------------------------------
class DebugInfoManager
{
public:
    static BOOL GetBoundariesAndVars(
        const DebugInfoRequest & request,
        IN FP_IDS_NEW fpNew, IN void * pNewData,
        OUT ULONG32 * pcMap,
        OUT ICorDebugInfo::OffsetMapping ** ppMap,
        OUT ULONG32 * pcVars,
        OUT ICorDebugInfo::NativeVarInfo ** ppVars);

    static BOOL GetRichDebugInfo(
        const DebugInfoRequest & request,
        IN FP_IDS_NEW fpNew, IN void * pNewData,
        OUT ICorDebugInfo::InlineTreeNode**    ppInlineTree,
        OUT ULONG32*                           pNumInlineTree,
        OUT ICorDebugInfo::RichOffsetMapping** ppRichMappings,
        OUT ULONG32*                           pNumRichMappings);

#ifdef DACCESS_COMPILE
    static void EnumMemoryRegionsForMethodDebugInfo(CLRDataEnumMemoryFlags flags, MethodDesc * pMD);
#endif
};



#endif // __DebugInfoStore_H_
