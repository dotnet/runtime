// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ===========================================================================
// File: ReadyToRunInfo.h
//

//
// Runtime support for Ready to Run
// ===========================================================================

#ifndef _READYTORUNINFO_H_
#define _READYTORUNINFO_H_

#include "nativeformatreader.h"
#include "inlinetracking.h"
#include "wellknownattributes.h"
#include "nativeimage.h"

typedef DPTR(struct READYTORUN_SECTION) PTR_READYTORUN_SECTION;

class NativeImage;
class PrepareCodeConfig;

typedef DPTR(class ReadyToRunCoreInfo) PTR_ReadyToRunCoreInfo;
class ReadyToRunCoreInfo
{
private:
    PTR_PEImageLayout               m_pLayout;
    PTR_READYTORUN_CORE_HEADER      m_pCoreHeader;
    Volatile<bool>                  m_fForbidLoadILBodyFixups;
    
public:
    ReadyToRunCoreInfo();
    ReadyToRunCoreInfo(PEImageLayout * pLayout, READYTORUN_CORE_HEADER * pCoreHeader);

    PTR_PEImageLayout GetLayout() const { return m_pLayout; }
    IMAGE_DATA_DIRECTORY * FindSection(ReadyToRunSectionType type) const;
    void ForbidProcessMoreILBodyFixups() { m_fForbidLoadILBodyFixups = true; }
    bool IsForbidProcessMoreILBodyFixups() { return m_fForbidLoadILBodyFixups; }

    PTR_PEImageLayout GetImage() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_pLayout;
    }
};

typedef DPTR(class ReadyToRunInfo) PTR_ReadyToRunInfo;
typedef DPTR(class ReadyToRunCoreInfo) PTR_ReadyToRunCoreInfo;

class ReadyToRunInfo
{
    friend class ReadyToRunJitManager;

    PTR_Module                      m_pModule;
    PTR_ModuleBase                  m_pNativeManifestModule;
    PTR_READYTORUN_HEADER           m_pHeader;
    bool                            m_isComponentAssembly;
    PTR_NativeImage                 m_pNativeImage;
    PTR_ReadyToRunInfo              m_pCompositeInfo;

    ReadyToRunCoreInfo              m_component;
    PTR_ReadyToRunCoreInfo          m_pComposite;

    PTR_RUNTIME_FUNCTION            m_pRuntimeFunctions;
    DWORD                           m_nRuntimeFunctions;

    PTR_IMAGE_DATA_DIRECTORY        m_pSectionDelayLoadMethodCallThunks;

    PTR_READYTORUN_IMPORT_SECTION   m_pImportSections;
    DWORD                           m_nImportSections;

    bool                            m_readyToRunCodeDisabled; // Is 

    NativeFormat::NativeReader      m_nativeReader;
    NativeFormat::NativeArray       m_methodDefEntryPoints;
    NativeFormat::NativeHashtable   m_instMethodEntryPoints;
    NativeFormat::NativeHashtable   m_availableTypesHashtable;
    NativeFormat::NativeHashtable   m_pgoInstrumentationDataHashtable;

    NativeFormat::NativeHashtable   m_pMetaDataHashtable;
    NativeFormat::NativeCuckooFilter m_attributesPresence;

    Crst                            m_Crst;
    PtrHashMap                      m_entryPointToMethodDescMap;

    PTR_PersistentInlineTrackingMapR2R m_pPersistentInlineTrackingMap;
    PTR_PersistentInlineTrackingMapR2R m_pCrossModulePersistentInlineTrackingMap;

    PTR_ReadyToRunInfo              m_pNextR2RForUnrelatedCode;

public:
    ReadyToRunInfo(Module * pModule, LoaderAllocator* pLoaderAllocator, PEImageLayout * pLayout, READYTORUN_HEADER * pHeader, NativeImage * pNativeImage, AllocMemTracker *pamTracker);

    static PTR_ReadyToRunInfo ComputeAlternateGenericLocationForR2RCode(MethodDesc *pMethod);
    static PTR_ReadyToRunInfo GetUnrelatedR2RModules();
    PTR_ReadyToRunInfo GetNextUnrelatedR2RModule() { LIMITED_METHOD_CONTRACT; return m_pNextR2RForUnrelatedCode; }
    void RegisterUnrelatedR2RModule();

    static PTR_ReadyToRunInfo Initialize(Module * pModule, AllocMemTracker *pamTracker);

    bool IsComponentAssembly() const { return m_isComponentAssembly; }

    static bool IsNativeImageSharedBy(PTR_Module pModule1, PTR_Module pModule2);

    PTR_ModuleBase GetNativeManifestModule() const { return m_pNativeManifestModule; }

    PTR_READYTORUN_HEADER GetReadyToRunHeader() const { return m_pHeader; }

    PTR_IMAGE_DATA_DIRECTORY GetDelayMethodCallThunksSection() const { return m_pSectionDelayLoadMethodCallThunks; }

    PTR_NativeImage GetNativeImage() const { return m_pNativeImage; }

    PTR_PEImageLayout GetImage() const { return m_pComposite->GetImage(); }
    IMAGE_DATA_DIRECTORY * FindSection(ReadyToRunSectionType type) const { return m_pComposite->FindSection(type); }

    PCODE GetEntryPoint(MethodDesc * pMD, PrepareCodeConfig* pConfig, BOOL fFixups);

    PTR_MethodDesc GetMethodDescForEntryPoint(PCODE entryPoint);
    bool GetPgoInstrumentationData(MethodDesc * pMD, BYTE** pAllocatedMemory, ICorJitInfo::PgoInstrumentationSchema**ppSchema, UINT *pcSchema, BYTE** pInstrumentationData);

    BOOL HasHashtableOfTypes();
    BOOL TryLookupTypeTokenFromName(const NameHandle *pName, mdToken * pFoundTypeToken);

    BOOL SkipTypeValidation()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pHeader->CoreHeader.Flags & READYTORUN_FLAG_SKIP_TYPE_VALIDATION;
    }

    BOOL IsPartial()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pHeader->CoreHeader.Flags & READYTORUN_FLAG_PARTIAL;
    }

    void DisableAllR2RCode()
    {
        LIMITED_METHOD_CONTRACT;
        m_readyToRunCodeDisabled = true;
    }

    bool ReadyToRunCodeDisabled()
    {
        LIMITED_METHOD_CONTRACT;
        return m_readyToRunCodeDisabled;
    }

    void ForbidProcessMoreILBodyFixups() { m_pComposite->ForbidProcessMoreILBodyFixups(); }
    bool IsForbidProcessMoreILBodyFixups() { return m_pComposite->IsForbidProcessMoreILBodyFixups(); }

    BOOL HasNonShareablePInvokeStubs()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pHeader->CoreHeader.Flags & READYTORUN_FLAG_NONSHARED_PINVOKE_STUBS;
    }

    bool MultiModuleVersionBubble()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pHeader->CoreHeader.Flags & READYTORUN_FLAG_MULTIMODULE_VERSION_BUBBLE;
    }

    PTR_READYTORUN_IMPORT_SECTION GetImportSections(COUNT_T * pCount)
    {
        LIMITED_METHOD_CONTRACT;
        *pCount = m_nImportSections;
        return m_pImportSections;
    }

    PTR_READYTORUN_IMPORT_SECTION GetImportSectionFromIndex(COUNT_T index)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(index < m_nImportSections);
        return m_pImportSections + index;
    }

    PTR_READYTORUN_IMPORT_SECTION GetImportSectionForRVA(RVA rva)
    {
        LIMITED_METHOD_CONTRACT;

        PTR_READYTORUN_IMPORT_SECTION pEnd = m_pImportSections + m_nImportSections;
        for (PTR_READYTORUN_IMPORT_SECTION pSection = m_pImportSections; pSection < pEnd; pSection++)
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

        NativeFormat::NativeHashtable::AllEntriesEnumerator m_genericEnum;
        NativeFormat::NativeParser m_genericParser;
        uint m_genericCurrentOffset;
        RID m_genericCurrentRid;
        PCCOR_SIGNATURE m_genericCurrentSig;

        void ParseGenericMethodSignatureAndRid(uint *offset, RID *rid);

    public:
        MethodIterator(ReadyToRunInfo * pInfo) :
            m_pInfo(pInfo),
            m_methodDefIndex(-1),
            m_genericEnum(),
            m_genericParser(),
            m_genericCurrentOffset(-1),
            m_genericCurrentRid(-1),
            m_genericCurrentSig(NULL)
        {
            NativeFormat::PTR_NativeHashtable pHash = NULL;
            if (!pInfo->m_instMethodEntryPoints.IsNull())
            {
                pHash = NativeFormat::PTR_NativeHashtable(&pInfo->m_instMethodEntryPoints);
            }

            m_genericEnum = NativeFormat::NativeHashtable::AllEntriesEnumerator(pHash);
        }

        BOOL Next();

        MethodDesc * GetMethodDesc();
        MethodDesc * GetMethodDesc_NoRestore();
        PCODE GetMethodStartAddress();
    };

    static DWORD GetFieldBaseOffset(MethodTable * pMT);

    PTR_PersistentInlineTrackingMapR2R GetInlineTrackingMap()
    {
        return m_pPersistentInlineTrackingMap;
    }

    bool HasReadyToRunInlineTrackingMap()
    {
        return (m_pPersistentInlineTrackingMap != NULL || m_pCrossModulePersistentInlineTrackingMap != NULL);
    }

    COUNT_T GetInliners(PTR_Module inlineeOwnerMod, mdMethodDef inlineeTkn, COUNT_T inlinersSize, MethodInModule *inliners, BOOL *incompleteData)
    {
        COUNT_T inlinersCount = 0;
        if (m_pPersistentInlineTrackingMap != NULL)
        {
            COUNT_T newInliners = m_pPersistentInlineTrackingMap->GetInliners(inlineeOwnerMod, inlineeTkn, inlinersSize, inliners, incompleteData);
            if (newInliners < inlinersSize)
            {
                inlinersSize -= newInliners;
                inliners += newInliners;
            }
            inlinersCount += newInliners;
        }

        if (m_pCrossModulePersistentInlineTrackingMap != NULL)
        {
            COUNT_T newInliners = m_pCrossModulePersistentInlineTrackingMap->GetInliners(inlineeOwnerMod, inlineeTkn, inlinersSize, inliners, incompleteData);
            if (newInliners < inlinersSize)
            {
                inlinersSize -= newInliners;
                inliners += newInliners;
            }
            inlinersCount += newInliners;
        }

        return inlinersCount;
    }


    bool MayHaveCustomAttribute(WellKnownAttribute attribute, mdToken token);
    void DisableCustomAttributeFilter();

    BOOL IsImageVersionAtLeast(int majorVersion, int minorVersion);
private:
    BOOL GetTypeNameFromToken(IMDInternalImport * pImport, mdToken mdType, LPCUTF8 * ppszName, LPCUTF8 * ppszNameSpace);
    BOOL GetEnclosingToken(IMDInternalImport * pImport, mdToken mdType, mdToken * pEnclosingToken);
    BOOL CompareTypeNameOfTokens(mdToken mdToken1, IMDInternalImport * pImport1, mdToken mdToken2, IMDInternalImport * pImport2);

    PTR_MethodDesc GetMethodDescForEntryPointInNativeImage(PCODE entryPoint);
    void SetMethodDescForEntryPointInNativeImage(PCODE entryPoint, PTR_MethodDesc methodDesc);
    
    PTR_ReadyToRunCoreInfo GetComponentInfo() { return dac_cast<PTR_ReadyToRunCoreInfo>(&m_component); }
};

class DynamicHelpers
{
private:
    static void EmitHelperWithArg(BYTE*& pCode, size_t rxOffset, LoaderAllocator * pAllocator, TADDR arg, PCODE target);
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
    static PCODE CreateDictionaryLookupHelper(LoaderAllocator * pAllocator, CORINFO_RUNTIME_LOOKUP * pLookup, DWORD dictionaryIndexAndSlot, Module * pModule);
};

#endif // _READYTORUNINFO_H_
