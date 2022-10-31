// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ===========================================================================
// File: ReadyToRunInfo.cpp
//

//
// Runtime support for Ready to Run
// ===========================================================================

#include "common.h"

#include "dbginterface.h"
#include "versionresilienthashcode.h"
#include "typehashingalgorithms.h"
#include "method.hpp"
#include "wellknownattributes.h"
#include "nativeimage.h"

using namespace NativeFormat;

ReadyToRunCoreInfo::ReadyToRunCoreInfo()
    : m_fForbidLoadILBodyFixups(false)
{
}

ReadyToRunCoreInfo::ReadyToRunCoreInfo(PEImageLayout* pLayout, READYTORUN_CORE_HEADER *pCoreHeader)
    : m_pLayout(pLayout), m_pCoreHeader(pCoreHeader), m_fForbidLoadILBodyFixups(false)
{
}

IMAGE_DATA_DIRECTORY * ReadyToRunCoreInfo::FindSection(ReadyToRunSectionType type) const
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    PTR_READYTORUN_SECTION pSections = dac_cast<PTR_READYTORUN_SECTION>(dac_cast<TADDR>(m_pCoreHeader) + sizeof(READYTORUN_CORE_HEADER));
    for (DWORD i = 0; i < m_pCoreHeader->NumberOfSections; i++)
    {
        // Verify that section types are sorted
        _ASSERTE(i == 0 || (pSections[i-1].Type < pSections[i].Type));

        READYTORUN_SECTION * pSection = pSections + i;
        if (pSection->Type == type)
            return &pSection->Section;
    }
    return NULL;
}

PTR_MethodDesc ReadyToRunInfo::GetMethodDescForEntryPoint(PCODE entryPoint)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    return m_pCompositeInfo->GetMethodDescForEntryPointInNativeImage(entryPoint);
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
    // Reserve stack space for parsing out the namespace in a name-based lookup
    // at this scope so the stack space is in scope for all usages in this method.
    CQuickBytes namespaceBuffer;

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

            if ((p = ns::FindSep(pszName)) != NULL)
            {
                SIZE_T d = p - pszName;

                FAULT_NOT_FATAL();
                pszNameSpace = namespaceBuffer.SetStringNoThrow(pszName, d);

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
        break;

    case mdtExportedType:
        if (SUCCEEDED(pImport->GetExportedTypeProps(mdType, NULL, NULL, pEnclosingToken, NULL, NULL)))
            return ((TypeFromToken(*pEnclosingToken) == mdtExportedType) && (*pEnclosingToken != mdExportedTypeNil));
        break;
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

    IMAGE_DATA_DIRECTORY * pDebugInfoDir = m_pComposite->FindSection(ReadyToRunSectionType::DebugInfo);
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

    return dac_cast<PTR_BYTE>(m_pComposite->GetLayout()->GetBase()) + debugInfoOffset;
}

PTR_MethodDesc ReadyToRunInfo::GetMethodDescForEntryPointInNativeImage(PCODE entryPoint)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
        SUPPORTS_DAC;
        PRECONDITION(!m_isComponentAssembly);
    }
    CONTRACTL_END;

#if defined(TARGET_AMD64) || (defined(TARGET_X86) && defined(TARGET_UNIX))
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

#ifndef DACCESS_COMPILE

void ReadyToRunInfo::SetMethodDescForEntryPointInNativeImage(PCODE entryPoint, MethodDesc *methodDesc)
{
    CONTRACTL
    {
        PRECONDITION(!m_isComponentAssembly);
    }
    CONTRACTL_END;

    CrstHolder ch(&m_Crst);

    if ((TADDR)m_entryPointToMethodDescMap.LookupValue(PCODEToPINSTR(entryPoint), (LPVOID)PCODEToPINSTR(entryPoint)) == (TADDR)INVALIDENTRY)
    {
        m_entryPointToMethodDescMap.InsertValue(PCODEToPINSTR(entryPoint), methodDesc);
    }
}

// A log file to record success/failure of R2R loads. s_r2rLogFile can have the following values:
// -1: Logging not yet initialized.
// NULL: Logging disabled.
// Any other value: Handle of the log file.
static  FILE * volatile s_r2rLogFile = (FILE *)(-1);

static void LogR2r(const char *msg, PEAssembly *pPEAssembly)
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
            fullname.Append(wszReadyToRunLogFile.GetValue());

            WCHAR pidSuffix[ARRAY_SIZE(".") + MaxUnsigned32BitDecString] = W(".");
            DWORD pid = GetCurrentProcessId();
            FormatInteger(pidSuffix + 1, ARRAY_SIZE(pidSuffix) - 1, "%u", pid);
            fullname.Append(pidSuffix);
            r2rLogFile = _wfopen(fullname.GetUnicode(), W("w"));
        }
        else
            r2rLogFile = NULL;

        if (r2rLogFile != NULL && !g_pConfig->ReadyToRun())
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

    fprintf(r2rLogFile, "%s: \"%S\".\n", msg, pPEAssembly->GetPath().GetUnicode());
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
    for (DWORD i = 0; i < pHeader->CoreHeader.NumberOfSections; i++)
    {
        if (pSections[i].Type == ReadyToRunSectionType::ImportSections)
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
        if ((pCurSection->Flags & ReadyToRunImportSectionFlags::Eager) != ReadyToRunImportSectionFlags::Eager)
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
                Module * pPrevious = InterlockedCompareExchangeT((Module **)(pFixups + i), pModule, NULL);
                return pPrevious == NULL || pPrevious == pModule;
            }
        }
    }

    return false;
}

/// <summary>
/// Try to locate composite R2R image for a given component module.
/// </summary>
static NativeImage *AcquireCompositeImage(Module * pModule, PEImageLayout * pLayout, READYTORUN_HEADER *pHeader)
{
    READYTORUN_SECTION * pSections = (READYTORUN_SECTION*)(pHeader + 1);
    LPCUTF8 ownerCompositeExecutableName = NULL;
    for (DWORD i = 0; i < pHeader->CoreHeader.NumberOfSections; i++)
    {
        if (pSections[i].Type == ReadyToRunSectionType::OwnerCompositeExecutable)
        {
            ownerCompositeExecutableName = (LPCUTF8)pLayout->GetBase() + pSections[i].Section.VirtualAddress;
            break;
        }
    }

    if (ownerCompositeExecutableName != NULL)
    {
        AssemblyBinder *binder = pModule->GetPEAssembly()->GetAssemblyBinder();
        return binder->LoadNativeImage(pModule, ownerCompositeExecutableName);
    }

    return NULL;
}

PTR_ReadyToRunInfo ReadyToRunInfo::Initialize(Module * pModule, AllocMemTracker *pamTracker)
{
    STANDARD_VM_CONTRACT;

    PEAssembly * pFile = pModule->GetPEAssembly();

    if (!g_pConfig->ReadyToRun())
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

    if (!pFile->HasLoadedPEImage())
    {
        DoLog("Ready to Run disabled - no loaded PE image");
        return NULL;
    }

    PEImageLayout * pLayout = pFile->GetLoadedLayout();
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

    if (!pLayout->IsNativeMachineFormat())
    {
        // For CoreCLR, be strict about disallowing machine mismatches.
        COMPlusThrowHR(COR_E_BADIMAGEFORMAT);
    }

    // The file must have been loaded using LoadLibrary
    if (!pLayout->IsRelocated())
    {
        DoLog("Ready to Run disabled - module not loaded for execution");
        return NULL;
    }

    READYTORUN_HEADER * pHeader = pLayout->GetReadyToRunHeader();

    // Ignore the content if the image major version is higher or lower than the major version currently supported by the runtime
    if (pHeader->MajorVersion < MINIMUM_READYTORUN_MAJOR_VERSION || pHeader->MajorVersion > READYTORUN_MAJOR_VERSION)
    {
        DoLog("Ready to Run disabled - unsupported header version");
        return NULL;
    }

    NativeImage *nativeImage = NULL;
    if (pHeader->CoreHeader.Flags & READYTORUN_FLAG_COMPONENT)
    {
        nativeImage = AcquireCompositeImage(pModule, pLayout, pHeader);
        if (nativeImage == NULL)
        {
            DoLog("Ready to Run disabled - composite image not found");
            return NULL;
        }
    }
    else
    {
        if (!AcquireImage(pModule, pLayout, pHeader))
        {
            DoLog("Ready to Run disabled - module already loaded in another assembly load context");
            return NULL;
        }
    }

    LoaderHeap *pHeap = pModule->GetLoaderAllocator()->GetHighFrequencyHeap();
    void * pMemory = pamTracker->Track(pHeap->AllocMem((S_SIZE_T)sizeof(ReadyToRunInfo)));

    DoLog("Ready to Run initialized successfully");

    return new (pMemory) ReadyToRunInfo(pModule, pModule->GetLoaderAllocator(), pLayout, pHeader, nativeImage, pamTracker);
}

bool ReadyToRunInfo::IsNativeImageSharedBy(PTR_Module pModule1, PTR_Module pModule2)
{
    return pModule1->GetReadyToRunInfo()->m_pComposite == pModule2->GetReadyToRunInfo()->m_pComposite;
}

// -------------------------------
// Infrastructure for handling R2R modules which have code in them which is not
// tightly associated with the module itself.
ReadyToRunInfo* s_pGlobalR2RModules = NULL;
PTR_ReadyToRunInfo ReadyToRunInfo::GetUnrelatedR2RModules() { return s_pGlobalR2RModules; }
void ReadyToRunInfo::RegisterUnrelatedR2RModule()
{
    LIMITED_METHOD_CONTRACT; // This function takes no locks, and can never fail

    if (m_pNativeImage == NULL)
    {
        // Produce a singly linked list of R2R modules with code in unrelated modules.
        // This code shouldn't be run until the module is given process lifetime tenure
        if (m_pHeader->CoreHeader.Flags & READYTORUN_FLAG_UNRELATED_R2R_CODE)
        {
            ReadyToRunInfo* oldGlobalValue;
            oldGlobalValue = s_pGlobalR2RModules;
            if (InterlockedCompareExchangeT(&m_pNextR2RForUnrelatedCode, dac_cast<PTR_ReadyToRunInfo>(dac_cast<TADDR>(oldGlobalValue) | 0x1), NULL) != NULL)
            {
                // Some other thread is registering or has registered this R2R image for unrelated generics
                // ReadyToRun code loading. we can simply return, as this process cannot fail.
                return;
            }

            while (InterlockedCompareExchangeT(&s_pGlobalR2RModules, this, oldGlobalValue) != oldGlobalValue)
            {
                oldGlobalValue = s_pGlobalR2RModules;
                m_pNextR2RForUnrelatedCode = dac_cast<PTR_ReadyToRunInfo>(dac_cast<TADDR>(oldGlobalValue) | 0x1);
            }
        }
    }
    else
    {
        m_pCompositeInfo->RegisterUnrelatedR2RModule();
    }
}

// Helper function for ComputeAlternateGenericLocationForR2RCode
static Module* ComputeAlternateGenericLocationForR2RCodeFromInstantiation(Module* pDefinitionModule, Instantiation inst)
{
    STANDARD_VM_CONTRACT;
    for (uint32_t i = 0; i < inst.GetNumArgs(); i++)
    {
        TypeHandle instArg = inst[i];

        // System.__Canon does not contribute to logical loader module
        if (instArg == TypeHandle(g_pCanonMethodTableClass))
            continue;

        CorElementType ety = instArg.GetSignatureCorElementType();
        if (CorTypeInfo::IsPrimitiveType_NoThrow(ety))
            continue;

        // Any type that is in the same module as the definition module is also ignored
        Module* instArgModule = instArg.GetLoaderModule();
        if (instArgModule == pDefinitionModule)
            continue;

        // Return the R2R module of thist instantiating argument. This may be NULL if that assembly isn't R2R
        return instArgModule;
    }

    return NULL;
}

PTR_ReadyToRunInfo ReadyToRunInfo::ComputeAlternateGenericLocationForR2RCode(MethodDesc *pMethod)
{
    STANDARD_VM_CONTRACT;
    // Alternate algorithm for generic method placement
    // This is designed to provide a second location to look for a generic instantiation that isn't the
    // defining module of the method. This algorithm is designed to be:
    // 1. Cheap to compute
    // 2. Able to find many generic instantiations assuming a fairly low level of generic complexity
    // 3. Particularly useful for cases where a second module instantiates a generic from a defining module
    //    over types entirely defined in a second module.
    // 4. Simple to implement in a compatible fashion in crossgen2, so that the runtime and compile can agree
    //    on code that should be useable. See ReadyToRunCompilationGroupBase.CrossModuleCompileableUncached
    //    for the managed implementation.

    // Collectible assemblies are complex to handle, and currently do not participate in R2R.
    if (pMethod->GetLoaderAllocator()->IsCollectible())
        return NULL;

    Module* pDefinitionModule = pMethod->GetModule();
    Module* resultModule = NULL;
    if (pMethod->HasMethodInstantiation())
    {
        resultModule = ComputeAlternateGenericLocationForR2RCodeFromInstantiation(pDefinitionModule, pMethod->GetMethodInstantiation());
    }
    if (resultModule == NULL && pMethod->HasClassInstantiation())
    {
        resultModule = ComputeAlternateGenericLocationForR2RCodeFromInstantiation(pDefinitionModule, pMethod->GetClassInstantiation());
    }

    if (resultModule != NULL)
    {
        // This may return NULL, if resultModule is not an R2R module. That is OK and intended.
        return resultModule->GetReadyToRunInfo();
    }
    else
    {
        return NULL;
    }
}

ReadyToRunInfo::ReadyToRunInfo(Module * pModule, LoaderAllocator* pLoaderAllocator, PEImageLayout * pLayout, READYTORUN_HEADER * pHeader, NativeImage *pNativeImage, AllocMemTracker *pamTracker)
    : m_pModule(pModule),
    m_pHeader(pHeader),
    m_pNativeImage(pModule != NULL ? pNativeImage: NULL), // m_pNativeImage is only set for composite image components, not the composite R2R info itself
    m_readyToRunCodeDisabled(FALSE),
    m_Crst(CrstReadyToRunEntryPointToMethodDescMap),
    m_pPersistentInlineTrackingMap(NULL),
    m_pNextR2RForUnrelatedCode(NULL)
{
    STANDARD_VM_CONTRACT;

    if ((pNativeImage != NULL) && (pModule != NULL))
    {
        // In multi-assembly composite images, per assembly sections are stored next to their core headers.
        m_pCompositeInfo = pNativeImage->GetReadyToRunInfo();
        m_pComposite = m_pCompositeInfo->GetComponentInfo();
        m_component = ReadyToRunCoreInfo(m_pComposite->GetLayout(), pNativeImage->GetComponentAssemblyHeader(pModule->GetSimpleName()));
        m_isComponentAssembly = true;
        m_pNativeManifestModule = pNativeImage->GetReadyToRunInfo()->GetNativeManifestModule();
    }
    else
    {
        m_pCompositeInfo = this;
        m_component = ReadyToRunCoreInfo(pLayout, &pHeader->CoreHeader);
        m_pComposite = &m_component;
        m_isComponentAssembly = false;
        IMDInternalImport *pNativeMDImport;
        IMAGE_DATA_DIRECTORY * pNativeMetadataSection = m_pComposite->FindSection(ReadyToRunSectionType::ManifestMetadata);
        if (pNativeMetadataSection != NULL)
        {
            pNativeMDImport = NULL;
            IfFailThrow(GetMetaDataInternalInterface((void *) m_pComposite->GetLayout()->GetDirectoryData(pNativeMetadataSection),
                                                        pNativeMetadataSection->Size,
                                                        ofRead,
                                                        IID_IMDInternalImport,
                                                        (void **) &pNativeMDImport));

            HENUMInternal assemblyEnum;
            HRESULT hr = pNativeMDImport->EnumAllInit(mdtAssemblyRef, &assemblyEnum);
            mdAssemblyRef assemblyRef;
            int32_t manifestAssemblyCount = 0;
            GUID emptyGuid  = {0};

            AssemblyBinder* binder = pModule != NULL ? pModule->GetPEAssembly()->GetAssemblyBinder() : pNativeImage->GetAssemblyBinder();
            auto pComponentAssemblyMvids = FindSection(ReadyToRunSectionType::ManifestAssemblyMvids);
            if (pComponentAssemblyMvids != NULL)
            {
                const GUID *componentMvids = (const GUID *)m_pComposite->GetLayout()->GetDirectoryData(pComponentAssemblyMvids);
                // Take load lock so that DeclareDependencyOnMvid can be called

                BaseDomain::LoadLockHolder lock(AppDomain::GetCurrentDomain(), pNativeImage == NULL); // LoadLock is already held for composite images
                AppDomain::GetCurrentDomain()->AssertLoadLockHeld();

                while (pNativeMDImport->EnumNext(&assemblyEnum, &assemblyRef))
                {
                    const GUID *componentMvid = &componentMvids[manifestAssemblyCount];

                    if (IsEqualGUID(*componentMvid, emptyGuid))
                    {
                        // Empty guid does not need further handling.
                        continue;
                    }

                    LPCSTR assemblyName;
                    IfFailThrow(pNativeMDImport->GetAssemblyRefProps(assemblyRef, NULL, NULL, &assemblyName, NULL, NULL, NULL, NULL));

                    binder->DeclareDependencyOnMvid(assemblyName, *componentMvid, pNativeImage != NULL, pModule != NULL ? pModule->GetSimpleName() : pNativeImage->GetFileName());
                    manifestAssemblyCount++;
                }
            }
        }
        else
        {
            pNativeMDImport = NULL;
        }

        m_pNativeManifestModule = CreateNativeManifestModule(pLoaderAllocator, pNativeMDImport, pModule, pamTracker);
    }

    IMAGE_DATA_DIRECTORY * pRuntimeFunctionsDir = m_pComposite->FindSection(ReadyToRunSectionType::RuntimeFunctions);
    if (pRuntimeFunctionsDir != NULL)
    {
        m_pRuntimeFunctions = (T_RUNTIME_FUNCTION *)m_pComposite->GetLayout()->GetDirectoryData(pRuntimeFunctionsDir);
        m_nRuntimeFunctions = pRuntimeFunctionsDir->Size / sizeof(T_RUNTIME_FUNCTION);
    }
    else
    {
        m_nRuntimeFunctions = 0;
    }

    IMAGE_DATA_DIRECTORY * pHotColdMapDir = m_pComposite->FindSection(ReadyToRunSectionType::HotColdMap);
    if (pHotColdMapDir != NULL)
    {
        m_pHotColdMap = (PTR_ULONG)m_pComposite->GetLayout()->GetDirectoryData(pHotColdMapDir);
        m_nHotColdMap = pHotColdMapDir->Size / sizeof(ULONG);
    }
    else
    {
        m_nHotColdMap = 0;
    }

    IMAGE_DATA_DIRECTORY * pImportSectionsDir = m_pComposite->FindSection(ReadyToRunSectionType::ImportSections);
    if (pImportSectionsDir != NULL)
    {
        m_pImportSections = (READYTORUN_IMPORT_SECTION*)m_pComposite->GetLayout()->GetDirectoryData(pImportSectionsDir);
        m_nImportSections = pImportSectionsDir->Size / sizeof(READYTORUN_IMPORT_SECTION);
    }
    else
    {
        m_nImportSections = 0;
    }

    m_nativeReader = NativeReader((BYTE *)m_pComposite->GetLayout()->GetBase(), m_pComposite->GetLayout()->GetVirtualSize());

    IMAGE_DATA_DIRECTORY * pEntryPointsDir = m_component.FindSection(ReadyToRunSectionType::MethodDefEntryPoints);
    if (pEntryPointsDir != NULL)
    {
        m_methodDefEntryPoints = NativeArray(&m_nativeReader, pEntryPointsDir->VirtualAddress);
    }

    m_pSectionDelayLoadMethodCallThunks = m_pComposite->FindSection(ReadyToRunSectionType::DelayLoadMethodCallThunks);

    IMAGE_DATA_DIRECTORY * pinstMethodsDir = m_pComposite->FindSection(ReadyToRunSectionType::InstanceMethodEntryPoints);
    if (pinstMethodsDir != NULL)
    {
        NativeParser parser = NativeParser(&m_nativeReader, pinstMethodsDir->VirtualAddress);
        m_instMethodEntryPoints = NativeHashtable(parser);
    }

    IMAGE_DATA_DIRECTORY * pAvailableTypesDir = m_component.FindSection(ReadyToRunSectionType::AvailableTypes);
    if (pAvailableTypesDir != NULL)
    {
        NativeParser parser = NativeParser(&m_nativeReader, pAvailableTypesDir->VirtualAddress);
        m_availableTypesHashtable = NativeHashtable(parser);
    }

    // For format version 5.2 and later, there is an optional table of instrumentation data
#ifdef FEATURE_PGO
    if (IsImageVersionAtLeast(5, 2))
    {
        IMAGE_DATA_DIRECTORY * pPgoInstrumentationDataDir = m_pComposite->FindSection(ReadyToRunSectionType::PgoInstrumentationData);
        if (pPgoInstrumentationDataDir)
        {
            NativeParser parser = NativeParser(&m_nativeReader, pPgoInstrumentationDataDir->VirtualAddress);
            m_pgoInstrumentationDataHashtable = NativeHashtable(parser);
        }

        // Force the Pgo manager infrastructure to be initialized
        pLoaderAllocator->GetOrCreatePgoManager();
    }
#endif

    if (!m_isComponentAssembly)
    {
        // For component assemblies we don't initialize the reverse lookup map mapping entry points to MethodDescs;
        // we need to use the global map in the composite image ReadyToRunInfo instance to be able to reverse translate
        // all methods within the composite image.
        LockOwner lock = {&m_Crst, IsOwnerOfCrst};
        m_entryPointToMethodDescMap.Init(TRUE, &lock);
    }

    if (IsImageVersionAtLeast(6, 3))
    {
        IMAGE_DATA_DIRECTORY* pCrossModuleInlineTrackingInfoDir = m_pComposite->FindSection(ReadyToRunSectionType::CrossModuleInlineInfo);
        if (pCrossModuleInlineTrackingInfoDir != NULL)
        {
            const BYTE* pCrossModuleInlineTrackingMapData = (const BYTE*)m_pComposite->GetImage()->GetDirectoryData(pCrossModuleInlineTrackingInfoDir);
            CrossModulePersistentInlineTrackingMapR2R::TryLoad(pModule, pLoaderAllocator, pCrossModuleInlineTrackingMapData, pCrossModuleInlineTrackingInfoDir->Size,
                pamTracker, (CrossModulePersistentInlineTrackingMapR2R**)&m_pCrossModulePersistentInlineTrackingMap);
        }
    }

    // For format version 4.1 and later, there is an optional inlining table
    if (IsImageVersionAtLeast(4, 1))
    {
        IMAGE_DATA_DIRECTORY* pInlineTrackingInfoDir = m_component.FindSection(ReadyToRunSectionType::InliningInfo2);
        if (pInlineTrackingInfoDir != NULL)
        {
            const BYTE* pInlineTrackingMapData = (const BYTE*)m_pComposite->GetImage()->GetDirectoryData(pInlineTrackingInfoDir);
            PersistentInlineTrackingMapR2R2::TryLoad(pModule, pInlineTrackingMapData, pInlineTrackingInfoDir->Size,
                pamTracker, (PersistentInlineTrackingMapR2R2**)&m_pPersistentInlineTrackingMap);
        }
    }

    // For format version 2.1 and later, there is an optional inlining table
    if (m_pPersistentInlineTrackingMap == nullptr && IsImageVersionAtLeast(2, 1))
    {
        IMAGE_DATA_DIRECTORY * pInlineTrackingInfoDir = m_component.FindSection(ReadyToRunSectionType::InliningInfo);
        if (pInlineTrackingInfoDir != NULL)
        {
            const BYTE* pInlineTrackingMapData = (const BYTE*)m_pComposite->GetImage()->GetDirectoryData(pInlineTrackingInfoDir);
            PersistentInlineTrackingMapR2R::TryLoad(pModule, pInlineTrackingMapData, pInlineTrackingInfoDir->Size,
                                                    pamTracker, &m_pPersistentInlineTrackingMap);
        }
    }

    // For format version 3.1 and later, there is an optional attributes section
    IMAGE_DATA_DIRECTORY *attributesPresenceDataInfoDir = m_component.FindSection(ReadyToRunSectionType::AttributePresence);
    if (attributesPresenceDataInfoDir != NULL)
    {
        NativeCuckooFilter newFilter(
            (BYTE *)m_pComposite->GetLayout()->GetBase(),
            m_pComposite->GetLayout()->GetVirtualSize(),
            attributesPresenceDataInfoDir->VirtualAddress,
            attributesPresenceDataInfoDir->Size);

        m_attributesPresence = newFilter;
    }
}

static bool SigMatchesMethodDesc(MethodDesc* pMD, SigPointer &sig, ModuleBase * pModule)
{
    STANDARD_VM_CONTRACT;

    ModuleBase *pOrigModule = pModule;
    ZapSig::Context    zapSigContext(pModule, (void *)pModule, ZapSig::NormalTokens);
    ZapSig::Context *  pZapSigContext = &zapSigContext;

    uint32_t methodFlags;
    IfFailThrow(sig.GetData(&methodFlags));

    _ASSERTE((methodFlags & ENCODE_METHOD_SIG_SlotInsteadOfToken) == 0);
    _ASSERTE(((methodFlags & (ENCODE_METHOD_SIG_MemberRefToken | ENCODE_METHOD_SIG_UpdateContext)) == 0) ||
             ((methodFlags & (ENCODE_METHOD_SIG_MemberRefToken | ENCODE_METHOD_SIG_UpdateContext)) == (ENCODE_METHOD_SIG_MemberRefToken | ENCODE_METHOD_SIG_UpdateContext)));

    if ( methodFlags & ENCODE_METHOD_SIG_UpdateContext)
    {
        uint32_t updatedModuleIndex;
        IfFailThrow(sig.GetData(&updatedModuleIndex));
        pModule = pZapSigContext->GetZapSigModule()->GetModuleFromIndex(updatedModuleIndex);
    }

    if (methodFlags & ENCODE_METHOD_SIG_OwnerType)
    {
        PCCOR_SIGNATURE pSigType;
        uint32_t cbSigType;
        sig.GetSignature(&pSigType, &cbSigType);
        if (!ZapSig::CompareSignatureToTypeHandle(pSigType, pModule, TypeHandle(pMD->GetMethodTable()), pZapSigContext))
            return false;

        IfFailThrow(sig.SkipExactlyOne());
    }

    RID rid;
    IfFailThrow(sig.GetData(&rid));

    if ((methodFlags & ENCODE_METHOD_SIG_MemberRefToken) != 0)
    {
        // member referenced via Manifest data
        // But we've already verified that the owner type is defined, so we can assume the type from the passed in methoddesc
        IMDInternalImport * pInternalImport = pModule->GetMDImport();

        LPCUTF8     szMember;
        PCCOR_SIGNATURE pSig;
        DWORD       cSig;

        IfFailThrow(pInternalImport->GetNameAndSigOfMemberRef(TokenFromRid(rid, mdtMemberRef), &pSig, &cSig, &szMember));

        _ASSERTE(!isCallConv(MetaSig::GetCallingConvention(Signature(pSig, cSig)), IMAGE_CEE_CS_CALLCONV_FIELD));

        if (strcmp(szMember, pMD->GetName()) != 0)
        {
            // Name doesn't match
            return false;
        }

        PCCOR_SIGNATURE pTargetMethodSig;
        DWORD       cTargetMethodSig;

        pMD->GetSig(&pTargetMethodSig, &cTargetMethodSig);
        if (!MetaSig::CompareMethodSigs(pSig, cSig, pModule, NULL, pTargetMethodSig, cTargetMethodSig, pMD->GetModule(), NULL, FALSE))
        {
            // Sig doesn't match
            return false;
        }

        rid = RidFromToken(pMD->GetMemberDef());
    }

    if (RidFromToken(pMD->GetMemberDef()) != rid)
        return false;

    if (methodFlags & ENCODE_METHOD_SIG_MethodInstantiation)
    {
        uint32_t numGenericArgs;
        IfFailThrow(sig.GetData(&numGenericArgs));
        Instantiation inst = pMD->GetMethodInstantiation();
        if (numGenericArgs != inst.GetNumArgs())
            return false;

        for (uint32_t i = 0; i < numGenericArgs; i++)
        {
            PCCOR_SIGNATURE pSigArg;
            uint32_t cbSigArg;
            sig.GetSignature(&pSigArg, &cbSigArg);
            if (!ZapSig::CompareSignatureToTypeHandle(pSigArg, pOrigModule, inst[i], pZapSigContext))
                return false;

            IfFailThrow(sig.SkipExactlyOne());
        }
    }

    return true;
}

bool ReadyToRunInfo::GetPgoInstrumentationData(MethodDesc * pMD, BYTE** pAllocatedMemory, ICorJitInfo::PgoInstrumentationSchema**ppSchema, UINT *pcSchema, BYTE** pInstrumentationData)
{
    STANDARD_VM_CONTRACT;

    PCODE pEntryPoint = NULL;
#ifdef PROFILING_SUPPORTED
    BOOL fShouldSearchCache = TRUE;
#endif // PROFILING_SUPPORTED
    mdToken token = pMD->GetMemberDef();
    int rid = RidFromToken(token);
    if (rid == 0)
        return false;

    // If R2R code is disabled for this module, simply behave as if it is never found
    if (ReadyToRunCodeDisabled())
        return false;

    if (m_pgoInstrumentationDataHashtable.IsNull())
        return false;

    NativeHashtable::Enumerator lookup = m_pgoInstrumentationDataHashtable.Lookup(GetVersionResilientMethodHashCode(pMD));
    NativeParser entryParser;
    while (lookup.GetNext(entryParser))
    {
        PCCOR_SIGNATURE pBlob = (PCCOR_SIGNATURE)entryParser.GetBlob();
        SigPointer sig(pBlob);
        if (SigMatchesMethodDesc(pMD, sig, m_pModule))
        {
            // Get the updated SigPointer location, so we can calculate the size of the blob,
            // in order to skip the blob and find the entry point data.
            entryParser = NativeParser(entryParser.GetNativeReader(), entryParser.GetOffset() + (uint)(sig.GetPtr() - pBlob));
            uint32_t versionAndFlags = entryParser.GetUnsigned();
            const uint32_t flagsMask = 0x3;
            const uint32_t versionShift = 2;
            uint32_t flags = versionAndFlags & flagsMask;
            uint32_t version = versionAndFlags >> versionShift;

            // Only version 0 is supported
            if (version != 0)
                return false;

            uint offset = entryParser.GetOffset();

            if (flags == 1)
            {
                // Offset is correct already
            }
            else if (flags == 3)
            {
                // Adjust offset as relative pointer
                uint32_t val;
                m_nativeReader.DecodeUnsigned(offset, &val);
                offset -= val;
            }

            BYTE* instrumentationDataPtr = ((BYTE*)GetImage()->GetBase()) + offset;
            IMAGE_DATA_DIRECTORY * pPgoInstrumentationDataDir = m_pComposite->FindSection(ReadyToRunSectionType::PgoInstrumentationData);
            size_t maxSize = offset - pPgoInstrumentationDataDir->VirtualAddress + pPgoInstrumentationDataDir->Size;

            return SUCCEEDED(PgoManager::getPgoInstrumentationResultsFromR2RFormat(this, m_pModule, m_pModule->GetReadyToRunImage(), instrumentationDataPtr, maxSize, pAllocatedMemory, ppSchema, pcSchema, pInstrumentationData));
        }
    }

    return false;
}

PCODE ReadyToRunInfo::GetEntryPoint(MethodDesc * pMD, PrepareCodeConfig* pConfig, BOOL fFixups)
{
    STANDARD_VM_CONTRACT;
#ifdef LOG_R2R_ENTRYPOINT
    SString tNamespace, tMethodName, tMethodSignature;
    SString tFullname;
    StackScratchBuffer scratch;
    const char* szFullName = "";
    bool printedStart = false;
#endif

    PCODE pEntryPoint = NULL;
#ifdef PROFILING_SUPPORTED
    BOOL fShouldSearchCache = TRUE;
#endif // PROFILING_SUPPORTED
    mdToken token = pMD->GetMemberDef();
    int rid = RidFromToken(token);
    if (rid == 0)
        goto done;
    // If R2R code is disabled for this module, simply behave as if it is never found
    if (ReadyToRunCodeDisabled())
        goto done;

    ETW::MethodLog::GetR2RGetEntryPointStart(pMD);

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
                offset = entryParser.GetOffset() + (uint)(sig.GetPtr() - pBlob);
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

#ifdef PROFILING_SUPPORTED
        {
            BEGIN_PROFILER_CALLBACK(CORProfilerTrackCacheSearches());
            (&g_profControlBlock)->
                JITCachedFunctionSearchStarted((FunctionID)pMD, &fShouldSearchCache);
            END_PROFILER_CALLBACK();
        }
        if (!fShouldSearchCache)
        {
            pConfig->SetProfilerRejectedPrecompiledCode();
            goto done;
        }
        if (CORProfilerTrackTransitions() && pMD->HasUnmanagedCallersOnlyAttribute())
        {
            pConfig->SetProfilerRejectedPrecompiledCode();
            goto done;
        }
#endif // PROFILING_SUPPORTED

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
            BOOL mayUsePrecompiledNDirectMethods = TRUE;
            mayUsePrecompiledNDirectMethods = !pConfig->IsForMulticoreJit();

            if (!m_pModule->FixupDelayList(dac_cast<TADDR>(GetImage()->GetBase()) + offset, mayUsePrecompiledNDirectMethods))
            {
                pConfig->SetReadyToRunRejectedPrecompiledCode();
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
    pEntryPoint = dac_cast<TADDR>(GetImage()->GetBase()) + m_pRuntimeFunctions[id].BeginAddress;
    m_pCompositeInfo->SetMethodDescForEntryPointInNativeImage(pEntryPoint, pMD);

#ifdef PROFILING_SUPPORTED
        {
            BEGIN_PROFILER_CALLBACK(CORProfilerTrackCacheSearches());
            (&g_profControlBlock)->
                JITCachedFunctionSearchFinished((FunctionID)pMD, COR_PRF_CACHED_FUNCTION_FOUND);
            END_PROFILER_CALLBACK();
        }
#endif // PROFILING_SUPPORTED

    if (g_pDebugInterface != NULL)
    {
        g_pDebugInterface->JITComplete(pConfig->GetCodeVersion(), pEntryPoint);
    }

done:
    if (ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context, R2RGetEntryPoint))
    {
        ETW::MethodLog::GetR2RGetEntryPoint(pMD, pEntryPoint);
    }
    return pEntryPoint;
}

void ReadyToRunInfo::MethodIterator::ParseGenericMethodSignatureAndRid(uint *pOffset, RID *pRid)
{
    _ASSERTE(!m_genericParser.IsNull());

    HRESULT hr = S_OK;
    *pOffset = -1;
    *pRid = -1;

    PCCOR_SIGNATURE pBlob = (PCCOR_SIGNATURE)m_genericParser.GetBlob();
    SigPointer sig(pBlob);

    uint32_t methodFlags = 0;
    // Skip the signature so we can get to the offset
    hr = sig.GetData(&methodFlags);
    if (FAILED(hr))
    {
        return;
    }

    _ASSERTE((methodFlags & ENCODE_METHOD_SIG_SlotInsteadOfToken) == 0);
    _ASSERTE(((methodFlags & (ENCODE_METHOD_SIG_MemberRefToken | ENCODE_METHOD_SIG_UpdateContext)) == 0) ||
             ((methodFlags & (ENCODE_METHOD_SIG_MemberRefToken | ENCODE_METHOD_SIG_UpdateContext)) == (ENCODE_METHOD_SIG_MemberRefToken | ENCODE_METHOD_SIG_UpdateContext)));

    if ( methodFlags & ENCODE_METHOD_SIG_UpdateContext)
    {
        uint32_t updatedModuleIndex;
        IfFailThrow(sig.GetData(&updatedModuleIndex));
    }

    if (methodFlags & ENCODE_METHOD_SIG_OwnerType)
    {
        hr = sig.SkipExactlyOne();
        if (FAILED(hr))
        {
            return;
        }
    }

    hr = sig.GetData(pRid);
    if (FAILED(hr))
    {
        return;
    }

    if (methodFlags & ENCODE_METHOD_SIG_MethodInstantiation)
    {
        uint32_t numGenericArgs;
        hr = sig.GetData(&numGenericArgs);
        if (FAILED(hr))
        {
            return;
        }

        for (DWORD i = 0; i < numGenericArgs; i++)
        {
            hr = sig.SkipExactlyOne();
            if (FAILED(hr))
            {
                return;
            }
        }
    }

    // Now that we have the size of the signature we can grab the offset and decode it
    PCCOR_SIGNATURE pSigNew;
    uint32_t cbSigNew;
    sig.GetSignature(&pSigNew, &cbSigNew);

    m_genericCurrentSig = pBlob;
    *pOffset = m_genericParser.GetOffset() + (uint)(pSigNew - pBlob);
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

    if (m_pInfo->ReadyToRunCodeDisabled())
        return FALSE;

    // Enumerate non-generic methods
    while (++m_methodDefIndex < (int)m_pInfo->m_methodDefEntryPoints.GetCount())
    {
        uint offset;
        if (m_pInfo->m_methodDefEntryPoints.TryGetAt(m_methodDefIndex, &offset))
        {
            return TRUE;
        }
    }

    // Enumerate generic instantiations
    m_genericParser = m_genericEnum.GetNext();
    if (!m_genericParser.IsNull())
    {
        ParseGenericMethodSignatureAndRid(&m_genericCurrentOffset, &m_genericCurrentRid);
        return TRUE;
    }

    return FALSE;
}

MethodDesc * ReadyToRunInfo::MethodIterator::GetMethodDesc()
{
    STANDARD_VM_CONTRACT;

    mdMethodDef methodToken = mdTokenNil;
    if (m_methodDefIndex < (int)m_pInfo->m_methodDefEntryPoints.GetCount())
    {
        methodToken = mdtMethodDef | (m_methodDefIndex + 1);
        return MemberLoader::GetMethodDescFromMethodDef(m_pInfo->m_pModule, methodToken, FALSE);
    }
    else
    {
        _ASSERTE(m_genericCurrentOffset > 0 && m_genericCurrentSig != NULL);
        return ZapSig::DecodeMethod(m_pInfo->m_pModule, m_pInfo->m_pModule, m_genericCurrentSig);
    }

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
    if (m_methodDefIndex < (int)m_pInfo->m_methodDefEntryPoints.GetCount())
    {
        if (!m_pInfo->m_methodDefEntryPoints.TryGetAt(m_methodDefIndex, &offset))
        {
            return NULL;
        }
    }
    else
    {
        if (m_genericCurrentOffset <= 0)
        {
            // Failed to parse generic info.
            return NULL;
        }

        offset = m_genericCurrentOffset;
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
    PCODE pEntryPoint = dac_cast<TADDR>(m_pInfo->GetImage()->GetBase()) + m_pInfo->m_pRuntimeFunctions[id].BeginAddress;

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

class NativeManifestModule : public ModuleBase
{
    IMDInternalImport* m_pMDImport;
    ReadyToRunInfo *m_pReadyToRunInfo;
    Module* m_pILModule;

    // Mapping of ModuleRef token to Module *
    LookupMap<PTR_Module>           m_ModuleReferencesMap;
public:

    NativeManifestModule(LoaderAllocator* pLoaderAllocator, IMDInternalImport *pManifestMetadata, Module* pModule, AllocMemTracker *pamTracker)
    {
        // NOTE: Composite images will not set m_pILModule to anything other than NULL. This implies that cross module
        // type loading outside of System.Private.CoreLib is not supported
        m_pILModule = pModule;
        m_loaderAllocator = pLoaderAllocator;
        m_pMDImport = pManifestMetadata;
        m_LookupTableCrst.Init(CrstModuleLookupTable, CrstFlags(CRST_UNSAFE_ANYMODE | CRST_DEBUGGER_THREAD));
        {
            // Get the number of AssemblyReferences in the map
            m_ManifestModuleReferencesMap.dwCount = pManifestMetadata->GetCountWithTokenKind(mdtAssemblyRef)+1;

            // Get # ModuleRefs
            m_ModuleReferencesMap.dwCount = pManifestMetadata->GetCountWithTokenKind(mdtModuleRef)+1;

            // Get # TypeRefs
            m_TypeRefToMethodTableMap.dwCount = pManifestMetadata->GetCountWithTokenKind(mdtTypeRef)+1;

            // Get # of MemberRefs
            m_MemberRefMap.dwCount = pManifestMetadata->GetCountWithTokenKind(mdtMemberRef)+1;

            S_SIZE_T nTotal;
            nTotal += m_ManifestModuleReferencesMap.dwCount;
            nTotal += m_ModuleReferencesMap.dwCount;
            nTotal += m_TypeRefToMethodTableMap.dwCount;
            nTotal += m_MemberRefMap.dwCount;
            PTR_TADDR pTable = (PTR_TADDR)pamTracker->Track(pLoaderAllocator->GetLowFrequencyHeap()->AllocMem(nTotal * S_SIZE_T(sizeof(TADDR))));

            // Note: Memory allocated on loader heap is zero filled
            // memset(pTable, 0, nTotal * sizeof(void*));

            m_ManifestModuleReferencesMap.pNext  = NULL;
            m_ManifestModuleReferencesMap.supportedFlags = MANIFEST_MODULE_MAP_ALL_FLAGS;
            m_ManifestModuleReferencesMap.pTable = pTable;

            m_ModuleReferencesMap.pNext  = NULL;
            m_ModuleReferencesMap.supportedFlags = NO_MAP_FLAGS;
            m_ModuleReferencesMap.pTable = &m_ManifestModuleReferencesMap.pTable[m_ManifestModuleReferencesMap.dwCount];

            m_TypeRefToMethodTableMap.pNext  = NULL;
            m_TypeRefToMethodTableMap.supportedFlags = TYPE_REF_MAP_ALL_FLAGS;
            m_TypeRefToMethodTableMap.pTable = &m_ModuleReferencesMap.pTable[m_ModuleReferencesMap.dwCount];

            m_MemberRefMap.pNext = NULL;
            m_MemberRefMap.supportedFlags = MEMBER_REF_MAP_ALL_FLAGS;
            m_MemberRefMap.pTable = &m_TypeRefToMethodTableMap.pTable[m_TypeRefToMethodTableMap.dwCount];
        }
    }

    IMDInternalImport *GetMDImport() const final
    {
        return m_pMDImport;
    }

    PTR_Module LookupModule(mdToken kFile) final
    {
        return GetModuleIfLoaded(kFile);
    }

    DomainAssembly * LoadAssemblyImpl(mdAssemblyRef kAssemblyRef) final
    {
        STANDARD_VM_CONTRACT;
        // Since we can only load via ModuleRef, this should never fail unless the module is improperly formatted
        COMPlusThrowHR(COR_E_BADIMAGEFORMAT);
    }

    // Decompose a null terminated moduleName into a pointer to an internal assemblyName, the length of that, and the associate module index
    // and return true if it can be decomposed.
    bool DecomposeModuleRef(LPCSTR moduleName, LPCSTR* pAssemblyName, size_t *pAssemblyNameLen, uint32_t *pIndex)
    {
        if (moduleName[0] != '#')
            return false;

        LPCSTR colonAddress = strchr(moduleName, ':');
        if (colonAddress == NULL)
            return false;

        uint32_t index = 0;
        LPCSTR numberCur = colonAddress + 1;

        // Some number must be specified
        if (*numberCur == '\0')
            return false;

        while (*numberCur != '\0')
        {
            if (index > 100000)
                return false; // Check to make sure we stay in a reasonable range for a module index.

            index = index * 10;
            char numberChar = *numberCur;
            if (numberChar < '0' || numberChar > '9')
                return false;

            index += numberChar - '0';
            numberCur++;
        }

        *pIndex = index;
        *pAssemblyNameLen = colonAddress - moduleName - 1;
        *pAssemblyName = moduleName + 1;
        return true;
    }

    // Find the assemblyRef with a given simple name in a module, or return mdTokenNil
    HRESULT GetAssemblyRefTokenOfIndirectDependency(Module* module, LPCSTR assemblyName, size_t assemblyNameLen, mdToken *pAssemblyRef)
    {
        auto pMDImport = module->GetMDImport();
        //Get the assembly refs.
        HENUMInternalHolder hEnumTypeRefs(pMDImport);
        mdToken assemblyRef = mdTokenNil;
        HRESULT hr;

        hEnumTypeRefs.EnumAllInit(mdtAssemblyRef);
        while (hEnumTypeRefs.EnumNext(&assemblyRef))
        {
            LPCSTR name;
            IfFailRet(pMDImport->GetAssemblyRefProps(assemblyRef, NULL, NULL, &name, NULL, NULL, NULL, NULL));
            size_t strIndex = 0;
            bool nameCompareFailed = false;
            for (; strIndex < assemblyNameLen; strIndex++)
            {
                if (name[strIndex] != assemblyName[strIndex])
                {
                    nameCompareFailed = true;
                    break;
                }
            }

            // The loop above checks everything up to the null terminator
            if (name[strIndex] != '\0')
                nameCompareFailed = true;

            if (nameCompareFailed)
            {
                continue;
            }

            *pAssemblyRef = assemblyRef;
            return S_OK;
        }

        *pAssemblyRef = mdTokenNil;
        return S_FALSE;
    }
    Module *GetModuleIfLoaded(mdFile kFile) final
    {
        CONTRACT(Module *)
        {
            INSTANCE_CHECK;
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(TypeFromToken(kFile) == mdtFile
                        || TypeFromToken(kFile) == mdtModuleRef);
            POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
            FORBID_FAULT;
            SUPPORTS_DAC;
        }
        CONTRACT_END;

        // Native manifest module functionality isn't actually multi-module assemblies, and File tokens are not useable
        if (TypeFromToken(kFile) == mdtFile)
            return NULL;

        _ASSERTE(TypeFromToken(kFile) == mdtModuleRef);
        Module* module = m_ModuleReferencesMap.GetElement(RidFromToken(kFile));
        if (module != NULL)
            RETURN module;

        LPCSTR moduleName;
        if (FAILED(GetMDImport()->GetModuleRefProps(kFile, &moduleName)))
        {
            RETURN NULL;
        }

        LPCSTR assemblyNameInModuleRef;
        size_t assemblyNameLen;
        uint32_t index;

        if (strcmp(moduleName, "System.Private.CoreLib") == 0)
        {
            // Special handling for CoreLib
            module = SystemDomain::SystemModule();
        }
        else if (DecomposeModuleRef(moduleName, &assemblyNameInModuleRef, &assemblyNameLen, &index))
        {
            // This disable cross module inlining beyond System.Private.CoreLib for composite images
            if (m_pILModule == NULL)
                return NULL;

            auto moduleBase = m_pILModule->GetModuleFromIndexIfLoaded(index);
            _ASSERTE(moduleBase == NULL || moduleBase->IsFullModule());
            module = static_cast<Module*>(moduleBase);

            if (module != NULL)
            {
                if (assemblyNameLen != 0) // #:<num> is a direct reference to a module index, #<assemblyName>:<num> is indirect
                {
                    mdToken assemblyRef;
                    if (FAILED(GetAssemblyRefTokenOfIndirectDependency(module, assemblyNameInModuleRef, assemblyNameLen, &assemblyRef)))
                    {
                        RETURN NULL;
                    }

                    if (assemblyRef == mdTokenNil)
                    {
                        module = NULL;
                    }
                    else
                    {
                        auto assemblyOfFinalModule = module->GetAssemblyIfLoaded(assemblyRef);
                        if (assemblyOfFinalModule != NULL)
                            module = assemblyOfFinalModule->GetModule();
                    }
                }
            }
        }

#ifndef DACCESS_COMPILE
        if (module != NULL)
            m_ModuleReferencesMap.TrySetElement(RidFromToken(kFile), module);
#endif
        RETURN module;
    }

    DomainAssembly *LoadModule(mdFile kFile) final
    {
        // Native manifest module functionality isn't actually multi-module assemblies, and File tokens are not useable
        if (TypeFromToken(kFile) == mdtFile)
            COMPlusThrowHR(COR_E_BADIMAGEFORMAT);

        _ASSERTE(TypeFromToken(kFile) == mdtModuleRef);
        Module* module = m_ModuleReferencesMap.GetElement(RidFromToken(kFile));
        if (module != NULL)
            return module->GetDomainAssembly();

        LPCSTR moduleName;
        if (FAILED(GetMDImport()->GetModuleRefProps(kFile, &moduleName)))
        {
            COMPlusThrowHR(COR_E_BADIMAGEFORMAT);
        }

        LPCSTR assemblyNameInModuleRef;
        size_t assemblyNameLen;
        uint32_t index;
        if (strcmp(moduleName, "System.Private.CoreLib") == 0)
        {
            // Special handling for CoreLib
            module = SystemDomain::SystemModule();
        }
        else if (DecomposeModuleRef(moduleName, &assemblyNameInModuleRef, &assemblyNameLen, &index))
        {
            // This disable cross module inlining beyond System.Private.CoreLib for composite images
            if (m_pILModule == NULL)
                COMPlusThrowHR(COR_E_FILENOTFOUND);

            auto moduleBase = m_pILModule->GetModuleFromIndex(index);
            _ASSERTE(moduleBase == NULL || moduleBase->IsFullModule());
            module = static_cast<Module*>(moduleBase);

            if (assemblyNameLen != 0) // #:<num> is a direct reference to a module index, #<assemblyName>:<num> is indirect
            {
                mdToken assemblyRef;

                IfFailThrow(GetAssemblyRefTokenOfIndirectDependency(module, assemblyNameInModuleRef, assemblyNameLen, &assemblyRef));
                if (assemblyRef == mdTokenNil)
                {
                    COMPlusThrowHR(COR_E_FILENOTFOUND);
                }
                auto domainAssemblyOfFinalModule = module->LoadAssembly(assemblyRef);
                module = domainAssemblyOfFinalModule->GetModule();
            }
        }
        else
        {
            // Unexpected ModuleRef string format
            COMPlusThrowHR(COR_E_BADIMAGEFORMAT);
        }

#ifndef DACCESS_COMPILE
        m_ModuleReferencesMap.TrySetElement(RidFromToken(kFile), module);
#endif

        return module->GetDomainAssembly();
    }

    virtual void DECLSPEC_NORETURN ThrowTypeLoadExceptionImpl(IMDInternalImport *pInternalImport,
                                                  mdToken token,
                                                  UINT resIDWhy)
    {
        STANDARD_VM_CONTRACT;
        // This should never fail
        COMPlusThrowHR(COR_E_BADIMAGEFORMAT);
    }
};

ModuleBase* CreateNativeManifestModule(LoaderAllocator* pLoaderAllocator, IMDInternalImport *pManifestMetadata, Module* pModule, AllocMemTracker *pamTracker)
{
    void *mem = pamTracker->Track(pLoaderAllocator->GetLowFrequencyHeap()->AllocMem(S_SIZE_T(sizeof(NativeManifestModule))));
    return new (mem) NativeManifestModule(pLoaderAllocator, pManifestMetadata, pModule, pamTracker);
}

#endif // DACCESS_COMPILE
