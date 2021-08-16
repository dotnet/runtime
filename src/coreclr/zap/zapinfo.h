// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// ZapInfo.h
//

//
// JIT-EE interface for zapping
//
// ======================================================================================

#ifndef __ZAPINFO_H__
#define __ZAPINFO_H__

#include "zapcode.h"

class ZapInfo;
struct InlineContext;

// The compiled code often implicitly needs fixups for various subtle reasons.
// We only emit explict fixups while compiling the method, while collecting
// implicit fixups in the LoadTable. At the end of compiling, we expect
// many of the LoadTable entries to be subsumed by the explicit entries
// and will not need to be emitted.
// This is also used to detect duplicate explicit fixups for the same type.

template <typename HandleType>
class LoadTable
{
private:
    ZapImage            *m_pModule;

    struct LoadEntry
    {
        HandleType              handle;
        int                     order;      // -1 = fixed
    };

    static int __cdecl LoadEntryCmp(const void* a_, const void* b_)
    {
        return ((LoadEntry*)a_)->order - ((LoadEntry*)b_)->order;
    }

    class LoadEntryTraits : public NoRemoveSHashTraits< DefaultSHashTraits<LoadEntry> >
    {
    public:
        typedef typename NoRemoveSHashTraits<DefaultSHashTraits<LoadEntry> >::count_t count_t;
        typedef typename NoRemoveSHashTraits<DefaultSHashTraits<LoadEntry> >::element_t element_t;
        typedef HandleType key_t;

        static key_t GetKey(element_t e)
        {
            LIMITED_METHOD_CONTRACT;
            return e.handle;
        }
        static BOOL Equals(key_t k1, key_t k2)
        {
            LIMITED_METHOD_CONTRACT;
            return k1 == k2;
        }
        static count_t Hash(key_t k)
        {
            LIMITED_METHOD_CONTRACT;
            return (count_t)(size_t)k;
        }

        static const element_t Null() { LIMITED_METHOD_CONTRACT; LoadEntry e; e.handle = NULL; e.order = 0; return e; }
        static bool IsNull(const element_t &e) { LIMITED_METHOD_CONTRACT; return (e.handle == NULL); }
    };

    typedef SHash<LoadEntryTraits> LoadEntryHashTable;

    LoadEntryHashTable      m_entries;

public:
    LoadTable(ZapImage *pModule)
      : m_pModule(pModule)
    {
    }

    // fixed=TRUE if the caller can guarantee that type will be fixed up because
    // of some implicit fixup. In this case, we track 'handle' only to avoid
    // duplicates and will not actually emit an explicit fixup for 'handle'
    //
    // fixed=FALSE if the caller needs an explicit fixup. We will emit an
    // explicit fixup for 'handle' if there are no other implicit fixups.

    void Load(HandleType handle, BOOL fixed)
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
        }
        CONTRACTL_END;

        const LoadEntry *result = m_entries.LookupPtr(handle);

        if (result != NULL)
        {
            if (fixed)
                ((LoadEntry*)result)->order = -1;
            return;
        }

        LoadEntry   newEntry;

        newEntry.handle = handle;
        newEntry.order = fixed ? -1 : m_entries.GetCount();

        m_entries.Add(newEntry);
    }

    void EmitLoadFixups(CORINFO_METHOD_HANDLE currentMethodHandle, ZapInfo * pZapInfo);
};

// Declare some specializations of EmitLoadFixups().
template<> void LoadTable<CORINFO_CLASS_HANDLE>::EmitLoadFixups(CORINFO_METHOD_HANDLE currentMethodHandle, ZapInfo * pZapInfo);
template<> void LoadTable<CORINFO_METHOD_HANDLE>::EmitLoadFixups(CORINFO_METHOD_HANDLE currentMethodHandle, ZapInfo * pZapInfo);


class ZapInfo
    : public ICorJitInfo
{
    friend class ZapImage;

    // Owning ZapImage
    ZapImage * m_pImage;

    Zapper * m_zapper;
    ICorDynamicInfo * m_pEEJitInfo;
    ICorCompileInfo * m_pEECompileInfo;

    // Current method being compiled; it is non-nil only for
    // method defs whose IL is in this module and (for generic code)
    // have <object> instantiation. It is also nil for IL_STUBs.
    mdMethodDef                 m_currentMethodToken;
    CORINFO_METHOD_HANDLE       m_currentMethodHandle;
    CORINFO_METHOD_INFO         m_currentMethodInfo;

    // m_currentMethodModule==m_hModule except for generic types/methods
    // defined in another assembly but instantiated in the current assembly.
    CORINFO_MODULE_HANDLE       m_currentMethodModule;

    unsigned                    m_currentMethodProfilingDataFlags;

    // Debug information reported by the JIT compiler for the current method
    ICorDebugInfo::NativeVarInfo *m_pNativeVarInfo;
    ULONG32                     m_iNativeVarInfo;
    ICorDebugInfo::OffsetMapping *m_pOffsetMapping;
    ULONG32                     m_iOffsetMapping;

    BYTE *                      m_pGCInfo;
    SIZE_T                      m_cbGCInfo;


    ZapBlobWithRelocs *         m_pCode;
    ZapBlobWithRelocs *         m_pColdCode;
    ZapBlobWithRelocs *         m_pROData;

#ifdef FEATURE_EH_FUNCLETS
    // Unwind info of the main method body. It will get merged with GC info.
    BYTE *                      m_pMainUnwindInfo;
    ULONG                       m_cbMainUnwindInfo;

    ZapUnwindInfo *             m_pUnwindInfo;
    ZapUnwindInfo *             m_pUnwindInfoFragments;
#if defined(TARGET_AMD64)
    ZapUnwindInfo *             m_pChainedColdUnwindInfo;
#endif
#endif // FEATURE_EH_FUNCLETS

    ZapExceptionInfo *          m_pExceptionInfo;

    ZapBlobWithRelocs *         m_pProfileData;

    ZapImport *                 m_pProfilingHandle;

    struct CodeRelocation : ZapReloc
    {
        ZapBlobWithRelocs * m_pNode;
    };

    SArray<CodeRelocation>   m_CodeRelocations;

    static int __cdecl CompareCodeRelocation(const void * a, const void * b);

    struct ImportEntry
    {
        ZapImport * pImport;
        bool fConditional; // Conditional imports are emitted only if they are actually referenced by the code.
    };

    class ImportTraits : public NoRemoveSHashTraits< DefaultSHashTraits<ImportEntry> >
    {
    public:
        typedef ZapImport * key_t;

        static key_t GetKey(element_t e)
        {
            LIMITED_METHOD_CONTRACT;
            return e.pImport;
        }
        static BOOL Equals(key_t k1, key_t k2)
        {
            LIMITED_METHOD_CONTRACT;
            return k1 == k2;
        }
        static count_t Hash(key_t k)
        {
            LIMITED_METHOD_CONTRACT;
            return (count_t)(size_t)k;
        }

        static const element_t Null() { LIMITED_METHOD_CONTRACT; ImportEntry e; e.pImport = NULL; return e; }
        static bool IsNull(const element_t &e) { LIMITED_METHOD_CONTRACT; return e.pImport == NULL; }
    };

    SHash<ImportTraits>         m_ImportSet;
    SArray<ZapImport *>         m_Imports;

    InlineSString<128>          m_currentMethodName;

    // Cache to reduce the number of entries in CORCOMPILE_LOAD_TABLE if it
    // is implied by some other fixup type
    LoadTable<CORINFO_CLASS_HANDLE>  m_ClassLoadTable;
    LoadTable<CORINFO_METHOD_HANDLE> m_MethodLoadTable;

    CORJIT_FLAGS m_jitFlags;

    void InitMethodName();

    CORJIT_FLAGS ComputeJitFlags(CORINFO_METHOD_HANDLE handle);

    ZapDebugInfo * EmitDebugInfo();
    ZapGCInfo * EmitGCInfo();
    ZapImport ** EmitFixupList();

    void PublishCompiledMethod();

    void EmitCodeRelocations();

    void ProcessReferences();

    BOOL CurrentMethodHasProfileData();

    void embedGenericSignature(CORINFO_LOOKUP * pLookup);

    PVOID embedDirectCall(CORINFO_METHOD_HANDLE ftn,
                          CORINFO_ACCESS_FLAGS accessFlags,
                          BOOL fAllowThunk);

    struct ProfileDataResults
    {
        ProfileDataResults(CORINFO_METHOD_HANDLE ftn) : m_ftn(ftn) {}
        ProfileDataResults* m_next = nullptr;
        CORINFO_METHOD_HANDLE m_ftn;
        SArray<PgoInstrumentationSchema> m_schema;
        BYTE *pInstrumentationData = nullptr;
        HRESULT m_hr = E_FAIL;
    };
    ProfileDataResults *m_pgoResults = nullptr;

public:
    ZapInfo(ZapImage * pImage, mdMethodDef md, CORINFO_METHOD_HANDLE handle, CORINFO_MODULE_HANDLE module, unsigned methodProfilingDataFlags);
    ~ZapInfo();

#ifdef ALLOW_SXS_JIT_NGEN
    void ResetForJitRetry();
#endif // ALLOW_SXS_JIT_NGEN

    void CompileMethod();

    void AppendImport(ZapImport * pImport);
    void AppendConditionalImport(ZapImport * pImport);

    ULONG GetNumFixups();

    // ICorJitInfo
#include "icorjitinfoimpl_generated.h"

    int  canHandleException(struct _EXCEPTION_POINTERS *pExceptionPointers);
    void * getAddressOfPInvokeFixup(CORINFO_METHOD_HANDLE method,
                                    void **ppIndirection);
    ZapImport * GetProfilingHandleImport();
};

#endif // __ZAPINFO_H__
