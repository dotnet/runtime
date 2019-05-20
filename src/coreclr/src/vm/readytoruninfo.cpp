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
#include "versionresilienthashcode.h"
#include "typehashingalgorithms.h"
#include "method.hpp"
#include "wellknownattributes.h"

using namespace NativeFormat;

IMAGE_DATA_DIRECTORY * ReadyToRunInfo::FindSection(DWORD type)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
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
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

#if defined(_TARGET_AMD64_) || (defined(_TARGET_X86_) && defined(FEATURE_PAL))
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
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    return !m_availableTypesHashtable.IsNull();
}

BOOL ReadyToRunInfo::TryLookupTypeTokenFromName(const NameHandle *pName, mdToken * pFoundTypeToken)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
        SUPPORTS_DAC;
        PRECONDITION(!m_availableTypesHashtable.IsNull());
    }
    CONTRACTL_END;

    if (m_availableTypesHashtable.IsNull())
        return FALSE;

    LPCUTF8 pszName = NULL;
    LPCUTF8 pszNameSpace = NULL;

    //
    // Compute the hashcode of the type (hashcode based on type name and namespace name)
    //
    int dwHashCode = 0;

    if (pName->GetTypeToken() == mdtBaseType || pName->GetTypeModule() == NULL)
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
        dwHashCode ^= ComputeNameHashCode(pszNameSpace, pszName);

        // Bucket is not 'null' for a nested type, and it will have information about the nested type's encloser
        if (!pName->GetBucket().IsNull())
        {
            // Must be a token based bucket that we found earlier in the R2R types hashtable
            _ASSERT(pName->GetBucket().GetEntryType() == HashedTypeEntry::IsHashedTokenEntry);

            const HashedTypeEntry::TokenTypeEntry& tokenBasedEncloser = pName->GetBucket().GetTokenBasedEntryValue();

            // Token must be a typedef token that we previously resolved (we shouldn't get here with an exported type token)
            _ASSERT(TypeFromToken(tokenBasedEncloser.m_TypeToken) == mdtTypeDef);

            int dwCurrentHashCode;
            mdToken mdCurrentTypeToken = tokenBasedEncloser.m_TypeToken;
            if (!GetVersionResilientTypeHashCode(tokenBasedEncloser.m_pModule->GetMDImport(), mdCurrentTypeToken, &dwCurrentHashCode))
                return FALSE;
            dwHashCode ^= dwCurrentHashCode;
        }
    }
    else
    {
        // Token based lookups (ex: tokens from IL code)

        if (!GetVersionResilientTypeHashCode(pName->GetTypeModule()->GetMDImport(), pName->GetTypeToken(), &dwHashCode))
            return FALSE;
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

            if (pName->GetTypeToken() == mdtBaseType || pName->GetTypeModule() == NULL)
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
        SUPPORTS_DAC;
        PRECONDITION(TypeFromToken(mdType) == mdtTypeDef || TypeFromToken(mdType) == mdtTypeRef || TypeFromToken(mdType) == mdtExportedType);
    }
    CONTRACTL_END;

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

    NativeArray debugInfoIndex(dac_cast<PTR_NativeReader>(PTR_HOST_INT_TO_TADDR(&m_nativeReader)), pDebugInfoDir->VirtualAddress);

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
    WRAPPER_NO_CONTRACT;

    static ConfigDWORD configReadyToRun;
    return configReadyToRun.val(CLRConfig::EXTERNAL_ReadyToRun);
}

// A log file to record success/failure of R2R loads. s_r2rLogFile can have the following values:
// -1: Logging not yet initialized.
// NULL: Logging disabled.
// Any other value: Handle of the log file.
static  FILE * volatile s_r2rLogFile = (FILE *)(-1);

static void LogR2r(const char *msg, PEFile *pFile)
{
    STANDARD_VM_CONTRACT;

    // Make a local copy of s_r2rLogFile, so we're not affected by other threads.
    FILE *r2rLogFile = s_r2rLogFile;
    if (r2rLogFile == (FILE *)(-1))
    {
        // Initialize Ready to Run logging. Any errors cause logging to be disabled.
        NewArrayHolder<WCHAR> wszReadyToRunLogFile;
        if (SUCCEEDED(CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_ReadyToRunLogFile, &wszReadyToRunLogFile)) && wszReadyToRunLogFile)
        {
            // Append process ID to the log file name, so multiple processes can log at the same time.
            StackSString fullname;
            fullname.Printf(W("%s.%u"), wszReadyToRunLogFile.GetValue(), GetCurrentProcessId());
            r2rLogFile = _wfopen(fullname.GetUnicode(), W("w"));
        }
        else
            r2rLogFile = NULL;

        if (r2rLogFile != NULL && !ReadyToRunInfo::IsReadyToRunEnabled())
        {
            fputs("Ready to Run not enabled.\n", r2rLogFile);
            fclose(r2rLogFile);
            r2rLogFile = NULL;
        }

        if (InterlockedCompareExchangeT(&s_r2rLogFile, r2rLogFile, (FILE *)(-1)) != (FILE *)(-1))
        {
            if (r2rLogFile != NULL)
                fclose(r2rLogFile);
            r2rLogFile = s_r2rLogFile;
        }
    }

    if (r2rLogFile == NULL)
        return;

    fprintf(r2rLogFile, "%s: \"%S\".\n", msg, pFile->GetPath().GetUnicode());
    fflush(r2rLogFile);
}

#define DoLog(msg) if (s_r2rLogFile != NULL) LogR2r(msg, pFile)

// Try to acquire an R2R image for exclusive use by a particular module.
// Returns true if successful. Returns false if the image is already been used
// by another module. Each R2R image has a space to store a pointer to the
// module that owns it. We set this pointer unless it has already be
// initialized to point to another Module.
static bool AcquireImage(Module * pModule, PEImageLayout * pLayout, READYTORUN_HEADER * pHeader)
{
    STANDARD_VM_CONTRACT;

    // First find the import sections of the image.
    READYTORUN_IMPORT_SECTION * pImportSections = NULL;
    READYTORUN_IMPORT_SECTION * pImportSectionsEnd = NULL;
    READYTORUN_SECTION * pSections = (READYTORUN_SECTION*)(pHeader + 1);
    for (DWORD i = 0; i < pHeader->NumberOfSections; i++)
    {
        if (pSections[i].Type == READYTORUN_SECTION_IMPORT_SECTIONS)
        {
            pImportSections = (READYTORUN_IMPORT_SECTION*)((PBYTE)pLayout->GetBase() + pSections[i].Section.VirtualAddress);
            pImportSectionsEnd = (READYTORUN_IMPORT_SECTION*)((PBYTE)pImportSections + pSections[i].Section.Size);
            break;
        }
    }

    // Go through the import sections to find the import for the module pointer.
    for (READYTORUN_IMPORT_SECTION * pCurSection = pImportSections; pCurSection < pImportSectionsEnd; pCurSection++)
    {
        // The import for the module pointer is always in an eager fixup section, so skip delayed fixup sections.
        if ((pCurSection->Flags & READYTORUN_IMPORT_SECTION_FLAGS_EAGER) == 0)
            continue;

        // Found an eager fixup section. Check the signature of each fixup in this section.
        PVOID *pFixups = (PVOID *)((PBYTE)pLayout->GetBase() + pCurSection->Section.VirtualAddress);
        DWORD nFixups = pCurSection->Section.Size / TARGET_POINTER_SIZE;
        DWORD *pSignatures = (DWORD *)((PBYTE)pLayout->GetBase() + pCurSection->Signatures);
        for (DWORD i = 0; i < nFixups; i++)
        {
            // See if we found the fixup for the Module pointer.
            PBYTE pSig = (PBYTE)pLayout->GetBase() + pSignatures[i];
            if (pSig[0] == READYTORUN_FIXUP_Helper && pSig[1] == READYTORUN_HELPER_Module)
            {
                Module * pPrevious = InterlockedCompareExchangeT(EnsureWritablePages((Module **)(pFixups + i)), pModule, NULL);
                return pPrevious == NULL || pPrevious == pModule;
            }
        }
    }

    return false;
}

PTR_ReadyToRunInfo ReadyToRunInfo::Initialize(Module * pModule, AllocMemTracker *pamTracker)
{
    STANDARD_VM_CONTRACT;

    PEFile * pFile = pModule->GetFile();

    if (!IsReadyToRunEnabled())
    {
        // Log message is ignored in this case.
        DoLog(NULL);
        return NULL;
    }

    if (pModule->IsCollectible())
    {
        DoLog("Ready to Run disabled - collectible module");
        return NULL;
    }

    if (!pFile->HasLoadedIL())
    {
        DoLog("Ready to Run disabled - no loaded IL image");
        return NULL;
    }

    PEImageLayout * pLayout = pFile->GetLoadedIL();
    if (!pLayout->HasReadyToRunHeader())
    {
        DoLog("Ready to Run header not found");
        return NULL;
    }

    if (CORProfilerDisableAllNGenImages() || CORProfilerUseProfileImages())
    {
        DoLog("Ready to Run disabled - profiler disabled native images");
        return NULL;
    }

    if (g_pConfig->ExcludeReadyToRun(pModule->GetSimpleName()))
    {
        DoLog("Ready to Run disabled - module on exclusion list");
        return NULL;
    }

#ifdef FEATURE_NATIVE_IMAGE_GENERATION
    // Ignore ReadyToRun during NGen
    if (IsCompilationProcess() && !IsNgenPDBCompilationProcess())
    {
        DoLog("Ready to Run disabled - compilation process");
        return NULL;
    }
#endif

    if (!pLayout->IsNativeMachineFormat())
    {
        // For CoreCLR, be strict about disallowing machine mismatches.
        COMPlusThrowHR(COR_E_BADIMAGEFORMAT);
    }

#ifndef CROSSGEN_COMPILE
    // The file must have been loaded using LoadLibrary
    if (!pLayout->IsRelocated())
    {
        DoLog("Ready to Run disabled - module not loaded for execution");
        return NULL;
    }
#endif

    READYTORUN_HEADER * pHeader = pLayout->GetReadyToRunHeader();

    // Ignore the content if the image major version is higher or lower than the major version currently supported by the runtime
    if (pHeader->MajorVersion < MINIMUM_READYTORUN_MAJOR_VERSION || pHeader->MajorVersion > READYTORUN_MAJOR_VERSION)
    {
        DoLog("Ready to Run disabled - unsupported header version");
        return NULL;
    }

    if (!AcquireImage(pModule, pLayout, pHeader))
    {
        DoLog("Ready to Run disabled - module already loaded in another AppDomain");
        return NULL;
    }

    LoaderHeap *pHeap = pModule->GetLoaderAllocator()->GetHighFrequencyHeap();
    void * pMemory = pamTracker->Track(pHeap->AllocMem((S_SIZE_T)sizeof(ReadyToRunInfo)));

    DoLog("Ready to Run initialized successfully");

    return new (pMemory) ReadyToRunInfo(pModule, pLayout, pHeader, pamTracker);
}

ReadyToRunInfo::ReadyToRunInfo(Module * pModule, PEImageLayout * pLayout, READYTORUN_HEADER * pHeader, AllocMemTracker *pamTracker)
    : m_pModule(pModule), m_pLayout(pLayout), m_pHeader(pHeader), m_Crst(CrstReadyToRunEntryPointToMethodDescMap),
    m_pPersistentInlineTrackingMap(NULL)
{
    STANDARD_VM_CONTRACT;

    IMAGE_DATA_DIRECTORY * pRuntimeFunctionsDir = FindSection(READYTORUN_SECTION_RUNTIME_FUNCTIONS);
    if (pRuntimeFunctionsDir != NULL)
    {
        m_pRuntimeFunctions = (T_RUNTIME_FUNCTION *)pLayout->GetDirectoryData(pRuntimeFunctionsDir);
        m_nRuntimeFunctions = pRuntimeFunctionsDir->Size / sizeof(T_RUNTIME_FUNCTION);
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

    IMAGE_DATA_DIRECTORY * pinstMethodsDir = FindSection(READYTORUN_SECTION_INSTANCE_METHOD_ENTRYPOINTS);
    if (pinstMethodsDir != NULL)
    {
        NativeParser parser = NativeParser(&m_nativeReader, pinstMethodsDir->VirtualAddress);
        m_instMethodEntryPoints = NativeHashtable(parser);
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

    // For format version 2.1 and later, there is an optional inlining table 
    if (IsImageVersionAtLeast(2, 1))
    {
        IMAGE_DATA_DIRECTORY * pInlineTrackingInfoDir = FindSection(READYTORUN_SECTION_INLINING_INFO);
        if (pInlineTrackingInfoDir != NULL)
        {
            const BYTE* pInlineTrackingMapData = (const BYTE*)GetImage()->GetDirectoryData(pInlineTrackingInfoDir);
            PersistentInlineTrackingMapR2R::TryLoad(pModule, pInlineTrackingMapData, pInlineTrackingInfoDir->Size,
                                                    pamTracker, &m_pPersistentInlineTrackingMap);
        }
    }

    // For format version 2.2 and later, there is an optional profile-data section
    if (IsImageVersionAtLeast(2, 2))
    {
        IMAGE_DATA_DIRECTORY * pProfileDataInfoDir = FindSection(READYTORUN_SECTION_PROFILEDATA_INFO);
        if (pProfileDataInfoDir != NULL)
        {
            CORCOMPILE_METHOD_PROFILE_LIST * pMethodProfileList;
            pMethodProfileList = (CORCOMPILE_METHOD_PROFILE_LIST *)GetImage()->GetDirectoryData(pProfileDataInfoDir);

            pModule->SetMethodProfileList(pMethodProfileList);  
        }
    }

    // For format version 3.1 and later, there is an optional attributes section
    IMAGE_DATA_DIRECTORY *attributesPresenceDataInfoDir = FindSection(READYTORUN_SECTION_ATTRIBUTEPRESENCE);
    if (attributesPresenceDataInfoDir != NULL)
    {
        NativeCuckooFilter newFilter(
            (byte *)pLayout->GetBase(),
            pLayout->GetVirtualSize(),
            attributesPresenceDataInfoDir->VirtualAddress,
            attributesPresenceDataInfoDir->Size);

        m_attributesPresence = newFilter;
    }
}

static bool SigMatchesMethodDesc(MethodDesc* pMD, SigPointer &sig, Module * pModule)
{
    STANDARD_VM_CONTRACT;

    ZapSig::Context    zapSigContext(pModule, (void *)pModule, ZapSig::NormalTokens);
    ZapSig::Context *  pZapSigContext = &zapSigContext;

    DWORD methodFlags;
    IfFailThrow(sig.GetData(&methodFlags));

    if (methodFlags & ENCODE_METHOD_SIG_OwnerType)
    {
        PCCOR_SIGNATURE pSigType;
        DWORD cbSigType;
        sig.GetSignature(&pSigType, &cbSigType);
        if (!ZapSig::CompareSignatureToTypeHandle(pSigType, pModule, TypeHandle(pMD->GetMethodTable()), pZapSigContext))
            return false;

        IfFailThrow(sig.SkipExactlyOne());
    }

    _ASSERTE((methodFlags & ENCODE_METHOD_SIG_SlotInsteadOfToken) == 0);
    _ASSERTE((methodFlags & ENCODE_METHOD_SIG_MemberRefToken) == 0);

    RID rid;
    IfFailThrow(sig.GetData(&rid));
    if (RidFromToken(pMD->GetMemberDef()) != rid)
        return false;

    if (methodFlags & ENCODE_METHOD_SIG_MethodInstantiation)
    {
        DWORD numGenericArgs;
        IfFailThrow(sig.GetData(&numGenericArgs));
        Instantiation inst = pMD->GetMethodInstantiation();
        if (numGenericArgs != inst.GetNumArgs())
            return false;

        for (DWORD i = 0; i < numGenericArgs; i++)
        {
            PCCOR_SIGNATURE pSigArg;
            DWORD cbSigArg;
            sig.GetSignature(&pSigArg, &cbSigArg);
            if (!ZapSig::CompareSignatureToTypeHandle(pSigArg, pModule, inst[i], pZapSigContext))
                return false;

            IfFailThrow(sig.SkipExactlyOne());
        }
    }

    return true;
}

PCODE ReadyToRunInfo::GetEntryPoint(MethodDesc * pMD, PrepareCodeConfig* pConfig, BOOL fFixups)
{
    STANDARD_VM_CONTRACT;

    PCODE pEntryPoint = NULL;
#ifndef CROSSGEN_COMPILE
#ifdef PROFILING_SUPPORTED
    BOOL fShouldSearchCache = TRUE;
#endif // PROFILING_SUPPORTED
#endif // CROSSGEN_COMPILE
    mdToken token = pMD->GetMemberDef();
    int rid = RidFromToken(token);
    if (rid == 0)
        goto done;

    uint offset;
    if (pMD->HasClassOrMethodInstantiation())
    {
        if (m_instMethodEntryPoints.IsNull())
            goto done;

        NativeHashtable::Enumerator lookup = m_instMethodEntryPoints.Lookup(GetVersionResilientMethodHashCode(pMD));
        NativeParser entryParser;
        offset = (uint)-1;
        while (lookup.GetNext(entryParser))
        {
            PCCOR_SIGNATURE pBlob = (PCCOR_SIGNATURE)entryParser.GetBlob();
            SigPointer sig(pBlob);
            if (SigMatchesMethodDesc(pMD, sig, m_pModule))
            {
                // Get the updated SigPointer location, so we can calculate the size of the blob,
                // in order to skip the blob and find the entry point data.
                PCCOR_SIGNATURE pSigNew;
                DWORD cbSigNew;
                sig.GetSignature(&pSigNew, &cbSigNew);
                offset = entryParser.GetOffset() + (uint)(pSigNew - pBlob);
                break;
            }
        }

        if (offset == (uint)-1)
            goto done;
    }
    else
    {
        if (!m_methodDefEntryPoints.TryGetAt(rid - 1, &offset))
            goto done;
    }

#ifndef CROSSGEN_COMPILE
#ifdef PROFILING_SUPPORTED
        {
            BEGIN_PIN_PROFILER(CORProfilerTrackCacheSearches());
            g_profControlBlock.pProfInterface->
                JITCachedFunctionSearchStarted((FunctionID)pMD, &fShouldSearchCache);
            END_PIN_PROFILER();
        }
        if (!fShouldSearchCache)
        {
            pConfig->SetProfilerRejectedPrecompiledCode();
            goto done;
        }
#endif // PROFILING_SUPPORTED
#endif // CROSSGEN_COMPILE

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
            {
#ifndef CROSSGEN_COMPILE
                pConfig->SetReadyToRunRejectedPrecompiledCode();
#endif // CROSSGEN_COMPILE
                goto done;
            }
        }

        id >>= 2;
    }
    else
    {
        id >>= 1;
    }

    _ASSERTE(id < m_nRuntimeFunctions);
    pEntryPoint = dac_cast<TADDR>(m_pLayout->GetBase()) + m_pRuntimeFunctions[id].BeginAddress;

    {
        CrstHolder ch(&m_Crst);

        if (m_entryPointToMethodDescMap.LookupValue(PCODEToPINSTR(pEntryPoint), (LPVOID)PCODEToPINSTR(pEntryPoint)) == (LPVOID)INVALIDENTRY)
            m_entryPointToMethodDescMap.InsertValue(PCODEToPINSTR(pEntryPoint), pMD);
    }

#ifndef CROSSGEN_COMPILE
#ifdef PROFILING_SUPPORTED
        {
            BEGIN_PIN_PROFILER(CORProfilerTrackCacheSearches());
            g_profControlBlock.pProfInterface->
                JITCachedFunctionSearchFinished((FunctionID)pMD, COR_PRF_CACHED_FUNCTION_FOUND);
            END_PIN_PROFILER();
        }
#endif // PROFILING_SUPPORTED
#endif // CROSSGEN_COMPILE

    if (g_pDebugInterface != NULL)
    {
#if defined(CROSSGEN_COMPILE)
        g_pDebugInterface->JITComplete(NativeCodeVersion(pMD), pEntryPoint);
#else
        g_pDebugInterface->JITComplete(pConfig->GetCodeVersion(), pEntryPoint);
#endif
    }

done:
    if (ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context, R2RGetEntryPoint))
    {
        ETW::MethodLog::GetR2RGetEntryPoint(pMD, pEntryPoint);
    }
    return pEntryPoint;
}

BOOL ReadyToRunInfo::MethodIterator::Next()
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_ANY;
    }
    CONTRACTL_END;

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

MethodDesc * ReadyToRunInfo::MethodIterator::GetMethodDesc_NoRestore()
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_ANY;
    }
    CONTRACTL_END;

    uint offset;
    if (!m_pInfo->m_methodDefEntryPoints.TryGetAt(m_methodDefIndex, &offset))
    {
        return NULL;
    }

    uint id;
    offset = m_pInfo->m_nativeReader.DecodeUnsigned(offset, &id);

    if (id & 1)
    {
        if (id & 2)
        {
            uint val;
            m_pInfo->m_nativeReader.DecodeUnsigned(offset, &val);
            offset -= val;
        }

        id >>= 2;
    }
    else
    {
        id >>= 1;
    }

    _ASSERTE(id < m_pInfo->m_nRuntimeFunctions);
    PCODE pEntryPoint = dac_cast<TADDR>(m_pInfo->m_pLayout->GetBase()) + m_pInfo->m_pRuntimeFunctions[id].BeginAddress;

    return m_pInfo->GetMethodDescForEntryPoint(pEntryPoint);
}

PCODE ReadyToRunInfo::MethodIterator::GetMethodStartAddress()
{
    STANDARD_VM_CONTRACT;

    PCODE ret = m_pInfo->GetEntryPoint(GetMethodDesc(), NULL, FALSE);
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

    return OBJECT_SIZE + dwCumulativeInstanceFieldPos - dwOffsetBias;
}

BOOL ReadyToRunInfo::IsImageVersionAtLeast(int majorVersion, int minorVersion)
{
	LIMITED_METHOD_CONTRACT;
	return (m_pHeader->MajorVersion == majorVersion && m_pHeader->MinorVersion >= minorVersion) ||
		   (m_pHeader->MajorVersion > majorVersion);

}

static DWORD s_wellKnownAttributeHashes[(DWORD)WellKnownAttribute::CountOfWellKnownAttributes];

bool ReadyToRunInfo::MayHaveCustomAttribute(WellKnownAttribute attribute, mdToken token)
{
    UINT32 hash = 0;
    UINT16 fingerprint = 0;
    if (!m_attributesPresence.HashComputationImmaterial())
    {
        DWORD wellKnownHash = s_wellKnownAttributeHashes[(DWORD)attribute];
        if (wellKnownHash == 0)
        {
            // TODO, investigate using constexpr to compute string hashes at compile time initially
            s_wellKnownAttributeHashes[(DWORD)attribute] = wellKnownHash = ComputeNameHashCode(GetWellKnownAttributeName(attribute));
        }

        hash = CombineTwoValuesIntoHash(wellKnownHash, token);
        fingerprint = hash >> 16;
    }
    
    return m_attributesPresence.MayExist(hash, fingerprint);
}

void ReadyToRunInfo::DisableCustomAttributeFilter()
{
    m_attributesPresence.DisableFilter();
}

#endif // DACCESS_COMPILE
