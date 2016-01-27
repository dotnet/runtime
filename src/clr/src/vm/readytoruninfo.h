// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ===========================================================================
// File: ReadyToRunInfo.h
//

//
// Runtime support for Ready to Run
// ===========================================================================

#ifndef _READYTORUNINFO_H_
#define _READYTORUNINFO_H_

#include "nativeformatreader.h"

typedef DPTR(struct READYTORUN_SECTION) PTR_READYTORUN_SECTION;

typedef DPTR(class ReadyToRunInfo) PTR_ReadyToRunInfo;
class ReadyToRunInfo
{
    friend class ReadyToRunJitManager;

    PTR_Module                  m_pModule;

    PTR_PEImageLayout           m_pLayout;
    PTR_READYTORUN_HEADER       m_pHeader;

    PTR_RUNTIME_FUNCTION        m_pRuntimeFunctions;
    DWORD                       m_nRuntimeFunctions;

    PTR_CORCOMPILE_IMPORT_SECTION m_pImportSections;
    DWORD                       m_nImportSections;

    NativeFormat::NativeReader  m_nativeReader;
    NativeFormat::NativeArray   m_methodDefEntryPoints;

    Crst                        m_Crst;
    PtrHashMap                  m_entryPointToMethodDescMap;

    ReadyToRunInfo(Module * pModule, PEImageLayout * pLayout, READYTORUN_HEADER * pHeader);

public:
    static BOOL IsReadyToRunEnabled();

    static PTR_ReadyToRunInfo Initialize(Module * pModule, AllocMemTracker *pamTracker);

    PCODE GetEntryPoint(MethodDesc * pMD, BOOL fFixups = TRUE);

    MethodDesc * GetMethodDescForEntryPoint(PCODE entryPoint);

    BOOL SkipTypeValidation()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pHeader->Flags & READYTORUN_FLAG_SKIP_TYPE_VALIDATION;
    }

    PTR_PEImageLayout GetImage()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pLayout;
    }

    IMAGE_DATA_DIRECTORY * FindSection(DWORD type);

    PTR_CORCOMPILE_IMPORT_SECTION GetImportSections(COUNT_T * pCount)
    {
        LIMITED_METHOD_CONTRACT;
        *pCount = m_nImportSections;
        return m_pImportSections;
    }

    PTR_CORCOMPILE_IMPORT_SECTION GetImportSectionFromIndex(COUNT_T index)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(index < m_nImportSections);
        return m_pImportSections + index;
    }

    PTR_CORCOMPILE_IMPORT_SECTION GetImportSectionForRVA(RVA rva)
    {
        LIMITED_METHOD_CONTRACT;

        PTR_CORCOMPILE_IMPORT_SECTION pEnd = m_pImportSections + m_nImportSections;
        for (PTR_CORCOMPILE_IMPORT_SECTION pSection = m_pImportSections; pSection < pEnd; pSection++)
        {
            if (rva >= VAL32(pSection->Section.VirtualAddress) && rva < VAL32(pSection->Section.VirtualAddress) + VAL32(pSection->Section.Size))
                return pSection;
        }

        return NULL;
    }

    PTR_BYTE GetDebugInfo(PTR_RUNTIME_FUNCTION pRuntimeFunction);

    class MethodIterator
    {
        ReadyToRunInfo * m_pInfo;
        int m_methodDefIndex;

    public:
        MethodIterator(ReadyToRunInfo * pInfo)
            : m_pInfo(pInfo), m_methodDefIndex(-1)
        {
        }

        BOOL Next();

        MethodDesc * GetMethodDesc();
        PCODE GetMethodStartAddress();
    };

    static DWORD GetFieldBaseOffset(MethodTable * pMT);
};

class DynamicHelpers
{
public:
    static PCODE CreateHelper(LoaderAllocator * pAllocator, TADDR arg, PCODE target);
    static PCODE CreateHelperWithArg(LoaderAllocator * pAllocator, TADDR arg, PCODE target);
    static PCODE CreateHelper(LoaderAllocator * pAllocator, TADDR arg, TADDR arg2, PCODE target);
    static PCODE CreateHelperArgMove(LoaderAllocator * pAllocator, TADDR arg, PCODE target);
    static PCODE CreateReturn(LoaderAllocator * pAllocator);
    static PCODE CreateReturnConst(LoaderAllocator * pAllocator, TADDR arg);
    static PCODE CreateReturnIndirConst(LoaderAllocator * pAllocator, TADDR arg, INT8 offset);
    static PCODE CreateHelperWithTwoArgs(LoaderAllocator * pAllocator, TADDR arg, PCODE target);
    static PCODE CreateHelperWithTwoArgs(LoaderAllocator * pAllocator, TADDR arg, TADDR arg2, PCODE target);
};

#endif // _READYTORUNINFO_H_
