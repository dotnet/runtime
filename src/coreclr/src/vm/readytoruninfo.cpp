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

BOOL ReadyToRunInfo::HasHashtableOfTypes()
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
        SO_TOLERANT;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    return !m_availableTypesHashtable.IsNull();
}

BOOL ReadyToRunInfo::TryLookupTypeTokenFromName(NameHandle *pName, mdToken * pFoundTypeToken)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
        SO_TOLERANT;
        SUPPORTS_DAC;
        PRECONDITION(!m_availableTypesHashtable.IsNull());
    }
    CONTRACTL_END;

    if (m_availableTypesHashtable.IsNull())
        return FALSE;

    LPCUTF8 pszName;
    LPCUTF8 pszNameSpace;

    //
    // Compute the hashcode of the type (hashcode based on type name and namespace name)
    //
    DWORD dwHashCode = 0;
    {
        if (pName->GetTypeToken() == mdtBaseType)
        {
            // Name-based lookups (ex: Type.GetType()). 

            pszName = pName->GetName();
            pszNameSpace = "";

            if (pName->GetNameSpace() != NULL)
            {
                pszNameSpace = pName->GetNameSpace();
            }
            else
            {
                LPCUTF8 p;
                CQuickBytes szNamespace;

                if ((p = ns::FindSep(pszName)) != NULL)
                {
                    SIZE_T d = p - pszName;

                    FAULT_NOT_FATAL();
                    pszNameSpace = szNamespace.SetStringNoThrow(pszName, d);

                    if (pszNameSpace == NULL)
                        return FALSE;

                    pszName = (p + 1);
                }
            }

            _ASSERT(pszNameSpace != NULL);
            dwHashCode = ((dwHashCode << 5) + dwHashCode) ^ HashStringA(pszName);
            dwHashCode = ((dwHashCode << 5) + dwHashCode) ^ HashStringA(pszNameSpace);

            // Bucket is not 'null' for a nested type, and it will have information about the nested type's encloser
            if (!pName->GetBucket().IsNull())
            {
                // Must be a token based bucket that we found earlier in the R2R types hashtable
                _ASSERT(pName->GetBucket().GetEntryType() == HashedTypeEntry::IsHashedTokenEntry);

                const HashedTypeEntry::TokenTypeEntry& tokenBasedEncloser = pName->GetBucket().GetTokenBasedEntryValue();

                // Token must be a typedef token that we previously resolved (we shouldn't get here with an exported type token)
                _ASSERT(TypeFromToken(tokenBasedEncloser.m_TypeToken) == mdtTypeDef);

                mdToken mdCurrentTypeToken = tokenBasedEncloser.m_TypeToken;
                do
                {
                    LPCUTF8 pszNameTemp;
                    LPCUTF8 pszNameSpaceTemp;
                    if (!GetTypeNameFromToken(tokenBasedEncloser.m_pModule->GetMDImport(), mdCurrentTypeToken, &pszNameTemp, &pszNameSpaceTemp))
                        return FALSE;

                    dwHashCode = ((dwHashCode << 5) + dwHashCode) ^ HashStringA(pszNameTemp);
                    dwHashCode = ((dwHashCode << 5) + dwHashCode) ^ HashStringA(pszNameSpaceTemp == NULL ? "" : pszNameSpaceTemp);

                } while (GetEnclosingToken(tokenBasedEncloser.m_pModule->GetMDImport(), mdCurrentTypeToken, &mdCurrentTypeToken));

            }
        }
        else
        {
            // Token based lookups (ex: tokens from IL code)

            mdToken mdCurrentTypeToken = pName->GetTypeToken();
            do
            {
                if (!GetTypeNameFromToken(pName->GetTypeModule()->GetMDImport(), mdCurrentTypeToken, &pszName, &pszNameSpace))
                    return FALSE;

                dwHashCode = ((dwHashCode << 5) + dwHashCode) ^ HashStringA(pszName);
                dwHashCode = ((dwHashCode << 5) + dwHashCode) ^ HashStringA(pszNameSpace == NULL ? "" : pszNameSpace);

            } while (GetEnclosingToken(pName->GetTypeModule()->GetMDImport(), mdCurrentTypeToken, &mdCurrentTypeToken));
        }
    }


    //
    // Lookup the type in the native hashtable using the computed token
    //
    {
        NativeHashtable::Enumerator lookup = m_availableTypesHashtable.Lookup((int)dwHashCode);
        NativeParser entryParser;
        while (lookup.GetNext(entryParser))
        {
            DWORD ridAndFlag = entryParser.GetUnsigned();
            mdToken cl = ((ridAndFlag & 1) ? ((ridAndFlag >> 1) | mdtExportedType) : ((ridAndFlag >> 1) | mdtTypeDef));
            _ASSERT(RidFromToken(cl) != 0);

            if (pName->GetTypeToken() == mdtBaseType)
            {
                // Compare type name and namespace name
                LPCUTF8 pszFoundName;
                LPCUTF8 pszFoundNameSpace;
                if (!GetTypeNameFromToken(m_pModule->GetMDImport(), cl, &pszFoundName, &pszFoundNameSpace))
                    continue;
                if (strcmp(pszName, pszFoundName) != 0 || strcmp(pszNameSpace, pszFoundNameSpace) != 0)
                    continue;

                mdToken mdFoundTypeEncloser;
                BOOL inputTypeHasEncloser = !pName->GetBucket().IsNull();
                BOOL foundTypeHasEncloser = GetEnclosingToken(m_pModule->GetMDImport(), cl, &mdFoundTypeEncloser);
                if (inputTypeHasEncloser != foundTypeHasEncloser)
                    continue;

                // Compare the enclosing types chain for a match
                if (inputTypeHasEncloser)
                {
                    const HashedTypeEntry::TokenTypeEntry& tokenBasedEncloser = pName->GetBucket().GetTokenBasedEntryValue();

                    if (!CompareTypeNameOfTokens(tokenBasedEncloser.m_TypeToken, tokenBasedEncloser.m_pModule->GetMDImport(), mdFoundTypeEncloser, m_pModule->GetMDImport()))
                        continue;
                }
            }
            else
            {
                // Compare type name, namespace name, and enclosing types chain for a match
                if (!CompareTypeNameOfTokens(pName->GetTypeToken(), pName->GetTypeModule()->GetMDImport(), cl, m_pModule->GetMDImport()))
                    continue;
            }

            // Found a match!
            *pFoundTypeToken = cl;
            return TRUE;
        }
    }

    return FALSE;   // No matching type found
}

BOOL ReadyToRunInfo::GetTypeNameFromToken(IMDInternalImport * pImport, mdToken mdType, LPCUTF8 * ppszName, LPCUTF8 * ppszNameSpace)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
        SO_TOLERANT;
        SUPPORTS_DAC;
        PRECONDITION(TypeFromToken(mdType) == mdtTypeDef || TypeFromToken(mdType) == mdtTypeRef || TypeFromToken(mdType) == mdtExportedType);
    }
    CONTRACTL_END;

    switch (TypeFromToken(mdType))
    {
    case mdtTypeDef: 
        return SUCCEEDED(pImport->GetNameOfTypeDef(mdType, ppszName, ppszNameSpace));
    case mdtTypeRef: 
        return SUCCEEDED(pImport->GetNameOfTypeRef(mdType, ppszNameSpace, ppszName));
    case mdtExportedType:
        return SUCCEEDED(pImport->GetExportedTypeProps(mdType, ppszNameSpace, ppszName, NULL, NULL, NULL));
    }

    return FALSE;
}

BOOL ReadyToRunInfo::GetEnclosingToken(IMDInternalImport * pImport, mdToken mdType, mdToken * pEnclosingToken)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
        SO_TOLERANT;
        SUPPORTS_DAC;
        PRECONDITION(TypeFromToken(mdType) == mdtTypeDef || TypeFromToken(mdType) == mdtTypeRef || TypeFromToken(mdType) == mdtExportedType);
    }
    CONTRACTL_END;

    mdToken mdEncloser;
    switch (TypeFromToken(mdType))
    {
    case mdtTypeDef:
        return SUCCEEDED(pImport->GetNestedClassProps(mdType, pEnclosingToken));

    case mdtTypeRef:
        if (SUCCEEDED(pImport->GetResolutionScopeOfTypeRef(mdType, pEnclosingToken)))
            return ((TypeFromToken(*pEnclosingToken) == mdtTypeRef) && (*pEnclosingToken != mdTypeRefNil));

    case mdtExportedType:
        if (SUCCEEDED(pImport->GetExportedTypeProps(mdType, NULL, NULL, pEnclosingToken, NULL, NULL)))
            return ((TypeFromToken(*pEnclosingToken) == mdtExportedType) && (*pEnclosingToken != mdExportedTypeNil));
    }

    return FALSE;
}

BOOL ReadyToRunInfo::CompareTypeNameOfTokens(mdToken mdToken1, IMDInternalImport * pImport1, mdToken mdToken2, IMDInternalImport * pImport2)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
        SO_TOLERANT;
        SUPPORTS_DAC;
        PRECONDITION(TypeFromToken(mdToken1) == mdtTypeDef || TypeFromToken(mdToken1) == mdtTypeRef || TypeFromToken(mdToken1) == mdtExportedType);
        PRECONDITION(TypeFromToken(mdToken2) == mdtTypeDef || TypeFromToken(mdToken2) == mdtExportedType);
    }
    CONTRACTL_END;

    BOOL hasEncloser;
    do
    {
        LPCUTF8 pszName1;
        LPCUTF8 pszNameSpace1;
        if (!GetTypeNameFromToken(pImport1, mdToken1, &pszName1, &pszNameSpace1))
            return FALSE;

        LPCUTF8 pszName2;
        LPCUTF8 pszNameSpace2;
        if (!GetTypeNameFromToken(pImport2, mdToken2, &pszName2, &pszNameSpace2))
            return FALSE;

        if (strcmp(pszName1, pszName2) != 0 || strcmp(pszNameSpace1, pszNameSpace2) != 0)
            return FALSE;

        if ((hasEncloser = GetEnclosingToken(pImport1, mdToken1, &mdToken1)) != GetEnclosingToken(pImport2, mdToken2, &mdToken2))
            return FALSE;

    } while (hasEncloser);

    return TRUE;
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

    IMAGE_DATA_DIRECTORY * pAvailableTypesDir = FindSection(READYTORUN_SECTION_AVAILABLE_TYPES);
    if (pAvailableTypesDir != NULL)
    {
        NativeParser parser = NativeParser(&m_nativeReader, pAvailableTypesDir->VirtualAddress);
        m_availableTypesHashtable = NativeHashtable(parser);
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
