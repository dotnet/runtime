// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ===========================================================================
// File: ReadyToRunInfo.cpp
// 

//
// Runtime support for Ready to Run
// ===========================================================================

#include "common.h"

#include "dbginterface.h"
#include "compile.h"

using namespace NativeFormat;

IMAGE_DATA_DIRECTORY * ReadyToRunInfo::FindSection(DWORD type)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
        SO_TOLERANT;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    PTR_READYTORUN_SECTION pSections = dac_cast<PTR_READYTORUN_SECTION>(dac_cast<TADDR>(m_pHeader) + sizeof(READYTORUN_HEADER));
    for (DWORD i = 0; i < m_pHeader->NumberOfSections; i++)
    {
        // Verify that section types are sorted
        _ASSERTE(i == 0 || (pSections[i-1].Type < pSections[i].Type));

        READYTORUN_SECTION * pSection = pSections + i;
        if (pSection->Type == type)
            return &pSection->Section;
    }
    return NULL;
}

MethodDesc * ReadyToRunInfo::GetMethodDescForEntryPoint(PCODE entryPoint)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
        SO_TOLERANT;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

#ifdef _TARGET_AMD64_
    // A normal method entry point is always 8 byte aligned, but a funclet can start at an odd address.
    // Since PtrHashMap can't handle odd pointers, check for this case and return NULL.
    if ((entryPoint & 0x1) != 0)
        return NULL;
#endif

    TADDR val = (TADDR)m_entryPointToMethodDescMap.LookupValue(PCODEToPINSTR(entryPoint), (LPVOID)PCODEToPINSTR(entryPoint));
    if (val == (TADDR)INVALIDENTRY)
        return NULL;
    return dac_cast<PTR_MethodDesc>(val);
}

PTR_BYTE ReadyToRunInfo::GetDebugInfo(PTR_RUNTIME_FUNCTION pRuntimeFunction)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        THROWS;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    IMAGE_DATA_DIRECTORY * pDebugInfoDir = FindSection(READYTORUN_SECTION_DEBUG_INFO);
    if (pDebugInfoDir == NULL)
        return NULL;

    SIZE_T methodIndex = pRuntimeFunction - m_pRuntimeFunctions;
    _ASSERTE(methodIndex < m_nRuntimeFunctions);

    NativeArray debugInfoIndex(&m_nativeReader, pDebugInfoDir->VirtualAddress);

    uint offset;
    if (!debugInfoIndex.TryGetAt((DWORD)methodIndex, &offset))
        return NULL;

    uint lookBack;
    uint debugInfoOffset = m_nativeReader.DecodeUnsigned(offset, &lookBack);

    if (lookBack != 0)
        debugInfoOffset = offset - lookBack;

    return dac_cast<PTR_BYTE>(m_pLayout->GetBase()) + debugInfoOffset;
}

#ifndef DACCESS_COMPILE

BOOL ReadyToRunInfo::IsReadyToRunEnabled()
{
    STANDARD_VM_CONTRACT;

    static ConfigDWORD configReadyToRun;
    return configReadyToRun.val(CLRConfig::EXTERNAL_ReadyToRun);
}

PTR_ReadyToRunInfo ReadyToRunInfo::Initialize(Module * pModule, AllocMemTracker *pamTracker)
{
    STANDARD_VM_CONTRACT;

    PEFile * pFile = pModule->GetFile();

    // Ignore ReadyToRun for introspection-only loads
    if (pFile->IsIntrospectionOnly())
        return NULL;

    if (!pFile->HasLoadedIL())
        return NULL;

    PEImageLayout * pLayout = pFile->GetLoadedIL();
    if (!pLayout->HasReadyToRunHeader())
        return NULL;

    if (!IsReadyToRunEnabled())
        return NULL;

    if (!pLayout->IsNativeMachineFormat())
    {
#ifdef FEATURE_CORECLR
        // For CoreCLR, be strict about disallowing machine mismatches.
        COMPlusThrowHR(COR_E_BADIMAGEFORMAT);
#else
        return NULL;
#endif
    }

#ifdef FEATURE_NATIVE_IMAGE_GENERATION
    // Ignore ReadyToRun during NGen
    if (IsCompilationProcess() && !IsNgenPDBCompilationProcess())
        return NULL;
#endif

#ifndef CROSSGEN_COMPILE
    // The file must have been loaded using LoadLibrary
    if (!pLayout->IsRelocated())
        return NULL;
#endif

    READYTORUN_HEADER * pHeader = pLayout->GetReadyToRunHeader();

    // Ignore the content if the image major version is higher than the major version currently supported by the runtime
    if (pHeader->MajorVersion > READYTORUN_MAJOR_VERSION)
        return NULL;

    LoaderHeap *pHeap = pModule->GetLoaderAllocator()->GetHighFrequencyHeap();
    void * pMemory = pamTracker->Track(pHeap->AllocMem((S_SIZE_T)sizeof(ReadyToRunInfo)));

    return new (pMemory) ReadyToRunInfo(pModule, pLayout, pHeader);
}

ReadyToRunInfo::ReadyToRunInfo(Module * pModule, PEImageLayout * pLayout, READYTORUN_HEADER * pHeader)
    : m_pModule(pModule), m_pLayout(pLayout), m_pHeader(pHeader), m_Crst(CrstLeafLock)
{
    STANDARD_VM_CONTRACT;

    IMAGE_DATA_DIRECTORY * pRuntimeFunctionsDir = FindSection(READYTORUN_SECTION_RUNTIME_FUNCTIONS);
    if (pRuntimeFunctionsDir != NULL)
    {
        m_pRuntimeFunctions = (RUNTIME_FUNCTION *)pLayout->GetDirectoryData(pRuntimeFunctionsDir);
        m_nRuntimeFunctions = pRuntimeFunctionsDir->Size / sizeof(RUNTIME_FUNCTION);
    }
    else
    {
        m_nRuntimeFunctions = 0;
    }

    IMAGE_DATA_DIRECTORY * pImportSectionsDir = FindSection(READYTORUN_SECTION_IMPORT_SECTIONS);
    if (pImportSectionsDir != NULL)
    {
        m_pImportSections = (CORCOMPILE_IMPORT_SECTION*)pLayout->GetDirectoryData(pImportSectionsDir);
        m_nImportSections = pImportSectionsDir->Size / sizeof(CORCOMPILE_IMPORT_SECTION);
    }
    else
    {
        m_nImportSections = 0;
    }

    m_nativeReader = NativeReader((byte *)pLayout->GetBase(), pLayout->GetVirtualSize());

    IMAGE_DATA_DIRECTORY * pEntryPointsDir = FindSection(READYTORUN_SECTION_METHODDEF_ENTRYPOINTS);
    if (pEntryPointsDir != NULL)
    {
        m_methodDefEntryPoints = NativeArray(&m_nativeReader, pEntryPointsDir->VirtualAddress);
    }

    {
        LockOwner lock = {&m_Crst, IsOwnerOfCrst};
        m_entryPointToMethodDescMap.Init(TRUE, &lock);
    }
}

PCODE ReadyToRunInfo::GetEntryPoint(MethodDesc * pMD, BOOL fFixups /*=TRUE*/)
{
    STANDARD_VM_CONTRACT;

    // READYTORUN: FUTURE: Support for generics
    if (pMD->HasClassOrMethodInstantiation())
        return NULL;

    mdToken token = pMD->GetMemberDef();
    int rid = RidFromToken(token);
    if (rid == 0)
        return NULL;

    uint offset;
    if (!m_methodDefEntryPoints.TryGetAt(rid - 1, &offset))
        return NULL;

    uint id;
    offset = m_nativeReader.DecodeUnsigned(offset, &id);

    if (id & 1)
    {
        if (id & 2)
        {
            uint val;
            m_nativeReader.DecodeUnsigned(offset, &val);
            offset -= val;
        }

        if (fFixups)
        {
            if (!m_pModule->FixupDelayList(dac_cast<TADDR>(m_pLayout->GetBase()) + offset))
                return NULL;
        }

        id >>= 2;
    }
    else
    {
        id >>= 1;
    }

    _ASSERTE(id < m_nRuntimeFunctions);
    PCODE pEntryPoint = dac_cast<TADDR>(m_pLayout->GetBase()) + m_pRuntimeFunctions[id].BeginAddress;

    {
        CrstHolder ch(&m_Crst);

        if (m_entryPointToMethodDescMap.LookupValue(PCODEToPINSTR(pEntryPoint), (LPVOID)PCODEToPINSTR(pEntryPoint)) == (LPVOID)INVALIDENTRY)
            m_entryPointToMethodDescMap.InsertValue(PCODEToPINSTR(pEntryPoint), pMD);
    }

    if (g_pDebugInterface != NULL)
    {
        g_pDebugInterface->JITComplete(pMD, pEntryPoint);
    }

    return pEntryPoint;
}

BOOL ReadyToRunInfo::MethodIterator::Next()
{
    STANDARD_VM_CONTRACT;

    while (++m_methodDefIndex < (int)m_pInfo->m_methodDefEntryPoints.GetCount())
    {
        uint offset;
        if (m_pInfo->m_methodDefEntryPoints.TryGetAt(m_methodDefIndex, &offset))
            return TRUE;
    }
    return FALSE;
}

MethodDesc * ReadyToRunInfo::MethodIterator::GetMethodDesc()
{
    STANDARD_VM_CONTRACT;

    return MemberLoader::GetMethodDescFromMethodDef(m_pInfo->m_pModule, mdtMethodDef | (m_methodDefIndex + 1), FALSE);
}

PCODE ReadyToRunInfo::MethodIterator::GetMethodStartAddress()
{
    STANDARD_VM_CONTRACT;

    PCODE ret = m_pInfo->GetEntryPoint(GetMethodDesc(), FALSE);
    _ASSERTE(ret != NULL);
    return ret;
}

DWORD ReadyToRunInfo::GetFieldBaseOffset(MethodTable * pMT)
{
    STANDARD_VM_CONTRACT;

    DWORD dwAlignment = DATA_ALIGNMENT;
    DWORD dwOffsetBias = 0;
#ifdef FEATURE_64BIT_ALIGNMENT
    dwOffsetBias = 4;
    if (pMT->RequiresAlign8())
        dwAlignment = 8;
#endif

    MethodTable * pParentMT = pMT->GetParentMethodTable();
    DWORD dwCumulativeInstanceFieldPos = (pParentMT != NULL) ? pParentMT->GetNumInstanceFieldBytes() : 0;

    dwCumulativeInstanceFieldPos += dwOffsetBias;

    dwCumulativeInstanceFieldPos = (DWORD)ALIGN_UP(dwCumulativeInstanceFieldPos, dwAlignment);

    return (DWORD)sizeof(Object) + dwCumulativeInstanceFieldPos - dwOffsetBias;
}

#endif // DACCESS_COMPILE
