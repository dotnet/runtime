// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// ZapCode.h
//

//
// ZapNodes for everything directly related to zapping of native code
//
// code:ZapMethodHeader
// code:ZapMethodEntryPoint
// code:ZapMethodEntryPointTable
// code:ZapDebugInfo
// code:ZapProfileData
// code:ZapHelperTable
// code:ZapGCInfoTable


//
// ======================================================================================

#ifndef __ZAPCODE_H__
#define __ZAPCODE_H__

// Forward declarations

class ZapBlobWithRelocs;

#ifdef REDHAWK
typedef ZapNode ZapGCInfo;
#else
#if defined(FEATURE_EH_FUNCLETS)
class ZapGCInfo;
#else
typedef ZapBlob ZapGCInfo;
#endif
#endif // REDHAWK

typedef ZapBlob ZapDebugInfo;
typedef ZapBlob ZapFixupInfo;
#ifdef REDHAWK
typedef ZapBlobWithRelocs ZapExceptionInfo;
#else
typedef ZapBlob ZapExceptionInfo;
#endif

class ZapUnwindInfo;

class ZapImport;

class ZapInfo;

class ZapGCRefMapTable;

//---------------------------------------------------------------------------------------
//
// ZapMethodHeader is the main node that all information about the compiled code is hanging from
//
class ZapMethodHeader : public ZapNode
{
    // All other kinds of method headers are tightly coupled with the main method header
    friend class ZapProfileData;
    friend class ZapCodeMethodDescs;
    friend class ZapColdCodeMap;

    friend class MethodCodeComparer;

    friend class ZapImage;
    friend class ZapInfo;

    CORINFO_METHOD_HANDLE m_handle;
    CORINFO_CLASS_HANDLE  m_classHandle;

    ZapBlobWithRelocs * m_pCode;
    ZapBlobWithRelocs * m_pColdCode;    // May be NULL

    ZapUnwindInfo * m_pUnwindInfo;
    ZapUnwindInfo * m_pColdUnwindInfo;  // May be NULL

#ifdef FEATURE_EH_FUNCLETS
    ZapUnwindInfo * m_pUnwindInfoFragments; // Linked list of all unwind info fragments
#endif

    ZapBlobWithRelocs * m_pROData;      // May be NULL

    ZapBlobWithRelocs * m_pProfileData; // May be NULL

    ZapGCInfo * m_pGCInfo;
    ZapDebugInfo * m_pDebugInfo;

    union                               // May be NULL
    {
        ZapImport ** m_pFixupList;      // Valid before place phase
        ZapFixupInfo * m_pFixupInfo;    // Valid after place phase
    };

    ZapExceptionInfo * m_pExceptionInfo; // May be NULL

    unsigned m_ProfilingDataFlags;

    unsigned m_compilationOrder;
    unsigned m_cachedLayoutOrder;

    DWORD m_methodIndex;

    ZapMethodHeader()
    {
    }

public:
    CORINFO_METHOD_HANDLE GetHandle()
    {
        return m_handle;
    }

    CORINFO_CLASS_HANDLE GetClassHandle()
    {
        return m_classHandle;
    }

    DWORD GetMethodIndex()
    {
        return m_methodIndex;
    }

    ZapBlobWithRelocs * GetCode()
    {
        return m_pCode;
    }

    ZapBlobWithRelocs * GetColdCode()
    {
        return m_pColdCode;
    }

    BOOL HasFixups()
    {
        return m_pFixupList != NULL;
    }

    ZapNode * GetFixupList()
    {
        return m_pFixupInfo;
    }

    ZapDebugInfo * GetDebugInfo()
    {
        return m_pDebugInfo;
    }

    unsigned GetCompilationOrder()
    {
        return m_compilationOrder;
    }

    unsigned GetCachedLayoutOrder()
    {
        return m_cachedLayoutOrder;
    }
    virtual ZapNodeType GetType()
    {
        return ZapNodeType_MethodHeader;
    }

    // Iterate over as many of the methods called by this method
    // as are easy to determine.  Currently this is implemented
    // by walking the Reloc list and so is only as complete as
    // the current state of the Relocs.  Note that the implementation
    // ignores virtual calls and calls in the cold code section.
    class PartialTargetMethodIterator
    {
    public:
        PartialTargetMethodIterator(ZapMethodHeader* pMethod)
            : m_pMethod(pMethod)
        {
            ZapBlobWithRelocs * pCode = pMethod->GetCode();
            m_pCurReloc = pCode ? pCode->GetRelocs() : NULL;
        }

        BOOL GetNext(CORINFO_METHOD_HANDLE *pHnd);

    private:
        ZapMethodHeader* m_pMethod;
        ZapReloc* m_pCurReloc;
    };

};

#if defined(_TARGET_X86_)
class ZapCodeBlob : public ZapBlobWithRelocs
{
protected:
    ZapCodeBlob(SIZE_T cbSize)
        : ZapBlobWithRelocs(cbSize)
    {
    }

public:
    static ZapCodeBlob * NewAlignedBlob(ZapWriter * pWriter, PVOID pData, SIZE_T cbSize, SIZE_T cbAlignment);

    virtual DWORD ComputeRVA(ZapWriter * pZapWriter, DWORD dwPos);
};
#else
typedef ZapBlobWithRelocs ZapCodeBlob;
#endif

class ZapCodeMethodDescs : public ZapNode
{
    COUNT_T m_iStartMethod;
    COUNT_T m_iEndMethod;
    COUNT_T m_nUnwindInfos;

public:
    ZapCodeMethodDescs(COUNT_T startMethod, COUNT_T endMethod, COUNT_T nUnwindInfos)
        : m_iStartMethod(startMethod), m_iEndMethod(endMethod), m_nUnwindInfos(nUnwindInfos)
    {
    }

    virtual UINT GetAligment()
    {
        return sizeof(DWORD);
    }

    virtual DWORD GetSize()
    {
        return m_nUnwindInfos * sizeof(DWORD);
    }

    virtual ZapNodeType GetType()
    {
        return ZapNodeType_CodeManagerMap;
    }

    virtual void Save(ZapWriter * pZapWriter);
};

//---------------------------------------------------------------------------------------
//
// ZapMethodEntryPoint is special type of placeholder. Unlike normal placeholder, it
// carries extra CORINFO_ACCESS_FLAGS that is used to opt into the direct call even
// when it would not be otherwise possible.
//
class ZapMethodEntryPoint : public ZapNode
{
    CORINFO_METHOD_HANDLE   m_handle;       // Target method being called
    BYTE                    m_accessFlags;  // CORINFO_ACCESS_FLAGS
    BYTE                    m_fUsed;        // Entrypoint is used - needs to be resolved

    ZapNode                *m_pEntryPoint;  // only used for abstract methods to remember the precode

public:
    ZapMethodEntryPoint(CORINFO_METHOD_HANDLE handle, CORINFO_ACCESS_FLAGS accessFlags)
        : m_handle(handle), m_accessFlags(static_cast<BYTE>(accessFlags))
    {
    }

    virtual ZapNodeType GetType()
    {
        return ZapNodeType_MethodEntryPoint;
    }

    CORINFO_METHOD_HANDLE GetHandle()
    {
        return m_handle;
    }

    CORINFO_ACCESS_FLAGS GetAccessFlags()
    {
        return (CORINFO_ACCESS_FLAGS)m_accessFlags;
    }

    void SetIsUsed()
    {
        m_fUsed = true;
    }

    BOOL IsUsed()
    {
        return m_fUsed;
    }

    void Resolve(ZapImage * pImage);
};

class ZapMethodEntryPointTable
{
    struct MethodEntryPointKey
    {
        MethodEntryPointKey(CORINFO_METHOD_HANDLE handle, CORINFO_ACCESS_FLAGS accessFlags)
            : m_handle(handle), m_accessFlags(accessFlags)
        {
        }

        CORINFO_METHOD_HANDLE   m_handle;       // Target method being called
        CORINFO_ACCESS_FLAGS    m_accessFlags;
    };

    class MethodEntryPointTraits : public NoRemoveSHashTraits< DefaultSHashTraits<ZapMethodEntryPoint *> >
    {
    public:
        typedef MethodEntryPointKey key_t;

        static key_t GetKey(element_t e)
        {
            LIMITED_METHOD_CONTRACT;
            return MethodEntryPointKey(e->GetHandle(), e->GetAccessFlags());
        }
        static BOOL Equals(key_t k1, key_t k2)
        {
            LIMITED_METHOD_CONTRACT;
            return (k1.m_handle == k2.m_handle) && (k1.m_accessFlags == k2.m_accessFlags);
        }
        static count_t Hash(key_t k)
        {
            LIMITED_METHOD_CONTRACT;
            return (count_t)(size_t)k.m_handle ^ (count_t)k.m_accessFlags;
        }

        static element_t Null() { LIMITED_METHOD_CONTRACT; return NULL; }
        static bool IsNull(const element_t &e) { LIMITED_METHOD_CONTRACT; return e == NULL; }
    };

    typedef SHash< MethodEntryPointTraits > MethodEntryPointTable;

    MethodEntryPointTable m_entries;
    ZapImage * m_pImage;

public:
    ZapMethodEntryPointTable(ZapImage * pImage)
        : m_pImage(pImage)
    {
    }

    void Preallocate(COUNT_T cbILImage)
    {
        PREALLOCATE_HASHTABLE(ZapMethodEntryPointTable::m_entries, 0.0018, cbILImage);
    }

    ZapMethodEntryPoint * GetMethodEntryPoint(CORINFO_METHOD_HANDLE handle, CORINFO_ACCESS_FLAGS accessFlags);

    ZapNode * CanDirectCall(ZapMethodEntryPoint * pMethodEntryPoint, ZapMethodHeader * pCaller);

    void Resolve();
};

//---------------------------------------------------------------------------------------
//
// Zapping of unwind info
//
class ZapUnwindInfo : public ZapNode
{
    ZapNode * m_pCode;

    DWORD m_dwStartOffset;
    DWORD m_dwEndOffset;

    ZapNode * m_pUnwindData;

    ZapUnwindInfo * m_pNextFragment;

public:
    ZapUnwindInfo(ZapNode * pCode, DWORD dwStartOffset, DWORD dwEndOffset, ZapNode * pUnwindData = NULL)
        : m_pCode(pCode),
        m_dwStartOffset(dwStartOffset),
        m_dwEndOffset(dwEndOffset),
        m_pUnwindData(pUnwindData)
    {
    }

    ZapNode * GetCode()
    {
        return m_pCode;
    }

    DWORD GetStartOffset()
    {
        return m_dwStartOffset;
    }

    DWORD GetEndOffset()
    {
        return m_dwEndOffset;
    }

    DWORD GetStartAddress()
    {
        return m_pCode->GetRVA() + GetStartOffset();
    }

    DWORD GetEndAddress()
    {
        return m_pCode->GetRVA() + GetEndOffset();
    }
    // Used to set unwind data lazily
    void SetUnwindData(ZapNode * pUnwindData)
    {
        _ASSERTE(m_pUnwindData == NULL);
        m_pUnwindData = pUnwindData;
    }

    ZapNode * GetUnwindData()
    {
        return m_pUnwindData;
    }

    void SetNextFragment(ZapUnwindInfo * pFragment)
    {
        _ASSERTE(m_pNextFragment == NULL);
        m_pNextFragment = pFragment;
    }

    ZapUnwindInfo * GetNextFragment()
    {
        return m_pNextFragment;
    }

    virtual UINT GetAlignment()
    {
        return sizeof(ULONG);
    }

    virtual DWORD GetSize()
    {
        return sizeof(T_RUNTIME_FUNCTION);
    }

    virtual ZapNodeType GetType()
    {
        return ZapNodeType_UnwindInfo;
    }

    virtual void Save(ZapWriter * pZapWriter);

    static int __cdecl CompareUnwindInfo(const void * a, const void * b);
};

#ifdef FEATURE_EH_FUNCLETS
//---------------------------------------------------------------------------------------
//
// Zapping of unwind data
//
class ZapUnwindData : public ZapBlob
{
public:
    ZapUnwindData(SIZE_T cbSize)
        : ZapBlob(cbSize)
    {
    }

    virtual UINT GetAlignment();

    virtual DWORD GetSize();

    virtual ZapNodeType GetType()
    {
        return ZapNodeType_UnwindData;
    }

    BOOL IsFilterFunclet()
    {
        return GetType() == ZapNodeType_FilterFuncletUnwindData;
    }

    ZapNode * GetPersonalityRoutine(ZapImage * pImage);
    virtual void Save(ZapWriter * pZapWriter);

    static ZapUnwindData * NewUnwindData(ZapWriter * pWriter, PVOID pData, SIZE_T cbSize, BOOL fIsFilterFunclet);
};

class ZapFilterFuncletUnwindData : public ZapUnwindData
{
public:
    ZapFilterFuncletUnwindData(SIZE_T cbSize)
        : ZapUnwindData(cbSize)
    {
    }

    virtual ZapNodeType GetType()
    {
        return ZapNodeType_FilterFuncletUnwindData;
    }
};
class ZapUnwindDataTable
{
    ZapImage * m_pImage;

    struct ZapUnwindDataKey
    {
        ZapUnwindDataKey(PVOID pUnwindData, SIZE_T cbUnwindData, BOOL fIsFilterFunclet)
            : m_unwindData(pUnwindData, cbUnwindData), m_fIsFilterFunclet(fIsFilterFunclet)
        {
        }

        ZapBlob::SHashKey m_unwindData;
        BOOL m_fIsFilterFunclet;
    };

    class ZapUnwindDataTraits : public NoRemoveSHashTraits< DefaultSHashTraits<ZapUnwindData *> >
    {
    public:
        typedef ZapUnwindDataKey key_t;

        static key_t GetKey(element_t e)
        {
            LIMITED_METHOD_CONTRACT;
            return ZapUnwindDataKey(e->GetData(), e->GetBlobSize(), e->IsFilterFunclet());
        }
        static BOOL Equals(key_t k1, key_t k2)
        {
            LIMITED_METHOD_CONTRACT;
            return ZapBlob::SHashTraits::Equals(k1.m_unwindData, k2.m_unwindData) && (k1.m_fIsFilterFunclet == k2.m_fIsFilterFunclet);
        }
        static count_t Hash(key_t k)
        {
            LIMITED_METHOD_CONTRACT;
            return ZapBlob::SHashTraits::Hash(k.m_unwindData) ^ k.m_fIsFilterFunclet;
        }

        static element_t Null() { LIMITED_METHOD_CONTRACT; return NULL; }
        static bool IsNull(const element_t &e) { LIMITED_METHOD_CONTRACT; return e == NULL; }
    };
    // Hashtable with all unwind data blobs. If two methods have unwind data
    // we store it just once.
    SHash< ZapUnwindDataTraits >  m_blobs;

public:
    ZapUnwindDataTable(ZapImage * pImage)
        : m_pImage(pImage)
    {
    }

    void Preallocate(COUNT_T cbILImage)
    {
        PREALLOCATE_HASHTABLE(ZapUnwindDataTable::m_blobs, 0.0003, cbILImage);
    }

    ZapUnwindData * GetUnwindData(PVOID pBlob, SIZE_T cbBlob, BOOL fIsFilterFunclet);
};
#endif // FEATURE_EH_FUNCLETS


//---------------------------------------------------------------------------------------
//
// Zapping of GC info
//
#ifdef FEATURE_EH_FUNCLETS
class ZapGCInfo : public ZapUnwindData
{
    DWORD m_cbGCInfo;

public:
    ZapGCInfo(SIZE_T cbGCInfo, SIZE_T cbUnwindInfo)
        : ZapUnwindData(cbUnwindInfo), m_cbGCInfo((DWORD)cbGCInfo)
    {
        if (m_cbGCInfo > ZAPWRITER_MAX_SIZE)
            ThrowHR(COR_E_OVERFLOW);
    }

    virtual PBYTE GetData()
    {
        return (PBYTE)(this + 1);
    }

    PBYTE GetGCInfo()
    {
        return GetData() + GetUnwindInfoSize();
    }

    DWORD GetGCInfoSize()
    {
        return m_cbGCInfo;
    }

    PBYTE GetUnwindInfo()
    {
        return GetData();
    }

    DWORD GetUnwindInfoSize()
    {
        return GetBlobSize();
    }

    virtual ZapNodeType GetType()
    {
        return ZapNodeType_UnwindDataAndGCInfo;
    }

    virtual DWORD GetSize()
    {
        return ZapUnwindData::GetSize() + m_cbGCInfo;
    }

    virtual void Save(ZapWriter * pZapWriter)
    {
        ZapUnwindData::Save(pZapWriter);

        pZapWriter->Write(GetGCInfo(), GetGCInfoSize());
    }

    static ZapGCInfo * NewGCInfo(ZapWriter * pWriter, PVOID pGCInfo, SIZE_T cbGCInfo, PVOID pUnwindInfo, SIZE_T cbUnwindInfo);
};

class ZapGCInfoTable
{
    ZapImage * m_pImage;

    struct GCInfoKey
    {
        GCInfoKey(PVOID pGCInfo, SIZE_T cbGCInfo, PVOID pUnwindInfo, SIZE_T cbUnwindInfo)
            : m_gcInfo(pGCInfo, cbGCInfo), m_unwindInfo(pUnwindInfo, cbUnwindInfo)
        {
        }

        ZapBlob::SHashKey m_gcInfo;
        ZapBlob::SHashKey m_unwindInfo;
    };

    class GCInfoTraits : public NoRemoveSHashTraits< DefaultSHashTraits<ZapGCInfo *> >
    {
    public:
        typedef GCInfoKey key_t;

        static key_t GetKey(element_t e)
        {
            LIMITED_METHOD_CONTRACT;
            return GCInfoKey(e->GetGCInfo(), e->GetGCInfoSize(), e->GetUnwindInfo(), e->GetUnwindInfoSize());
        }
        static BOOL Equals(key_t k1, key_t k2)
        {
            LIMITED_METHOD_CONTRACT;
            return ZapBlob::SHashTraits::Equals(k1.m_gcInfo, k2.m_gcInfo) && ZapBlob::SHashTraits::Equals(k1.m_unwindInfo, k2.m_unwindInfo);
        }
        static count_t Hash(key_t k)
        {
            LIMITED_METHOD_CONTRACT;
            return ZapBlob::SHashTraits::Hash(k.m_gcInfo) ^ ZapBlob::SHashTraits::Hash(k.m_unwindInfo);
        }

        static element_t Null() { LIMITED_METHOD_CONTRACT; return NULL; }
        static bool IsNull(const element_t &e) { LIMITED_METHOD_CONTRACT; return e == NULL; }
    };

    // Hashtable with all GC info blobs. If two methods have same GC info
    // we store it just once.
    SHash< GCInfoTraits >  m_blobs;

public:
    ZapGCInfoTable(ZapImage * pImage)
        : m_pImage(pImage)
    {
    }

    void Preallocate(COUNT_T cbILImage)
    {
        PREALLOCATE_HASHTABLE(ZapGCInfoTable::m_blobs, 0.0021, cbILImage);
    }

    // Returns interned instance of the GC info blob
    ZapGCInfo * GetGCInfo(PVOID pGCInfo, SIZE_T cbGCInfo, PVOID pUnwindInfo, SIZE_T cbUnwindInfo);
};
#else
class ZapGCInfoTable
{
    ZapImage * m_pImage;

    // Hashtable with all GC info blobs. If two methods have same GC info
    // we store it just once.
    SHash< NoRemoveSHashTraits < ZapBlob::SHashTraits > >  m_blobs;

public:
    ZapGCInfoTable(ZapImage * pImage)
        : m_pImage(pImage)
    {
    }

    void Preallocate(COUNT_T cbILImage)
    {
        PREALLOCATE_HASHTABLE(ZapGCInfoTable::m_blobs, 0.0021, cbILImage);
    }

    // Returns interned instance of the GC info blob
    ZapGCInfo * GetGCInfo(PVOID pBlob, SIZE_T cbBlob);
};
#endif

//---------------------------------------------------------------------------------------
//
// Zapping of debug info for native code
//
class ZapDebugInfoTable : public ZapNode
{
    COUNT_T m_nCount;
    ZapNode ** m_pTable;

    ZapImage * m_pImage;

    // Hashtable with all debug info blobs. If two methods have same debug info
    // we store it just once.
    SHash< NoRemoveSHashTraits < ZapBlob::SHashTraits > >  m_blobs;

    class LabelledEntry : public ZapNode
    {
    public:
        LabelledEntry * m_pNext;
        ZapMethodHeader * m_pMethod;

        LabelledEntry(ZapMethodHeader * pMethod)
            : m_pMethod(pMethod)
        {
        }

        virtual DWORD GetSize()
        {
            return sizeof(CORCOMPILE_DEBUG_LABELLED_ENTRY);
        }

        virtual UINT GetAlignment()
        {
            return sizeof(DWORD);
        }

        virtual ZapNodeType GetType()
        {
            return ZapNodeType_DebugInfoLabelledEntry;
        }

        virtual void Save(ZapWriter * pZapWriter);
    };

public:
    ZapDebugInfoTable(ZapImage * pImage)
        : m_pImage(pImage)
    {
    }

    void Preallocate(COUNT_T cbILImage)
    {
        PREALLOCATE_HASHTABLE(ZapDebugInfoTable::m_blobs, 0.0024, cbILImage);
    }

    // Returns interned instance of the debug info blob
    ZapDebugInfo * GetDebugInfo(PVOID pBlob, SIZE_T cbBlob);

    void PrepareLayout();
    void PlaceDebugInfo(ZapMethodHeader * pMethod);
    void FinishLayout();

    virtual DWORD GetSize()
    {
        return m_nCount * sizeof(CORCOMPILE_DEBUG_RID_ENTRY);
    }

    virtual UINT GetAlignment()
    {
        return sizeof(DWORD);
    }

    virtual ZapNodeType GetType()
    {
        return ZapNodeType_DebugInfoTable;
    }

    virtual void Save(ZapWriter * pZapWriter);
};

//---------------------------------------------------------------------------------------
//
// Zapping of IBC profile data collection area
//
class ZapProfileData : public ZapNode
{
    ZapMethodHeader *          m_pMethod;
    ZapProfileData *           m_pNext;

public:
    ZapProfileData(ZapMethodHeader * pMethod)
        : m_pMethod(pMethod)
    {
    }

    void SetNext(ZapProfileData * pNext)
    {
        m_pNext = pNext;
    }

    virtual DWORD GetSize()
    {
        return sizeof(CORCOMPILE_METHOD_PROFILE_LIST);
    }

    virtual UINT GetAlignment()
    {
        return TARGET_POINTER_SIZE;
    }

    virtual ZapNodeType GetType()
    {
        return ZapNodeType_ProfileData;
    }

    virtual void Save(ZapWriter * pZapWriter);
};

// Zapping of ExceptionInfoTable
// ExceptionInfoTable is a lookup table that has 1 entry for each method with EH information.
// The table is sorted by method start address, so binary search is used during runtime to find
// the EH info for a given method given a method start address.
class ZapExceptionInfoLookupTable : public ZapNode
{
private:
    typedef struct
    {
        ZapNode* m_pCode;
        ZapExceptionInfo* m_pExceptionInfo;
    } ExceptionInfoEntry;

    SArray<ExceptionInfoEntry> m_exceptionInfoEntries;
    ZapImage* m_pImage;
public:
    ZapExceptionInfoLookupTable(ZapImage *pImage);

    void PlaceExceptionInfoEntry(ZapNode* pCode, ZapExceptionInfo* pExceptionInfo);

    virtual ZapNodeType GetType()
    {
        return ZapNodeType_ExceptionInfoTable;
    }
    virtual UINT GetAlignment()
    {
        return TARGET_POINTER_SIZE;
    }
    virtual DWORD GetSize();
    virtual void Save(ZapWriter* pZapWriter);
};


class ZapUnwindInfoLookupTable : public ZapNode
{

public:
    ZapUnwindInfoLookupTable(ZapVirtualSection * pRuntimeFunctionSection, ZapNode * pCodeSection, DWORD totalCodeSize):
        m_pRuntimeFunctionSection(pRuntimeFunctionSection), m_pCodeSection(pCodeSection), m_TotalCodeSize(totalCodeSize)
    {
    }

    COUNT_T GetNumEntries()
    {
        return m_TotalCodeSize/RUNTIME_FUNCTION_LOOKUP_STRIDE + 1;
    }
    virtual ZapNodeType GetType()
    {
        return ZapNodeType_UnwindInfoLookupTable;
    }
    virtual UINT GetAlignment()
    {
        return sizeof(DWORD);
    }
    virtual DWORD GetSize();
    virtual void Save(ZapWriter* pZapWriter);

private:
    ZapVirtualSection * m_pRuntimeFunctionSection;
    ZapNode * m_pCodeSection;
    DWORD m_TotalCodeSize;
};

class ZapColdCodeMap : public ZapNode
{
public:
    ZapColdCodeMap(ZapVirtualSection * pRuntimeFunctionSection):
        m_pRuntimeFunctionSection (pRuntimeFunctionSection)
    {
    }

    virtual ZapNodeType GetType()
    {
        return ZapNodeType_ColdCodeMap;
    }
    virtual UINT GetAlignment()
    {
        return sizeof(DWORD);
    }
    virtual DWORD GetSize();
    virtual void Save(ZapWriter* pZapWriter);

private:
    ZapVirtualSection * m_pRuntimeFunctionSection;
};

//
//---------------------------------------------------------------------------------------
//
// Jump thunk for JIT helper
//
#ifdef _DEBUG
const static PCSTR s_rgHelperNames[] = {
#define JITHELPER(code,pfnHelper,sig) #code ,
#define DYNAMICJITHELPER(code,pfnHelper,sig) "<dynamic> " #code ,
#include <jithelpers.h>
};
#endif // _DEBUG

class ZapHelperThunk : public ZapNode
{
    DWORD m_dwHelper;

public:
    ZapHelperThunk(DWORD dwHelper)
        : m_dwHelper(dwHelper)
    {
#ifdef _DEBUG
        static_assert_no_msg(COUNTOF(s_rgHelperNames) == CORINFO_HELP_COUNT);
        LOG((LF_ZAP, LL_INFO1000000, "Created ZapHelperThunk for helper %3d (%s)\n",
            (USHORT)m_dwHelper, s_rgHelperNames[(USHORT)m_dwHelper]));
#endif // _DEBUG
    }

    virtual DWORD GetSize();

    virtual ZapNodeType GetType()
    {
        return ZapNodeType_HelperThunk;
    }

    virtual void Save(ZapWriter * pZapWriter);
};

class ZapLazyHelperThunk : public ZapNode
{
    CorInfoHelpFunc m_dwHelper;

    ZapNode * m_pArg;
    ZapNode * m_pTarget;

    DWORD SaveWorker(ZapWriter * pZapWriter);

public:
    ZapLazyHelperThunk(CorInfoHelpFunc dwHelper)
        : m_dwHelper(dwHelper)
    {
    }

    void Place(ZapImage * pImage);

    virtual DWORD GetSize();

    virtual ZapNodeType GetType()
    {
        return ZapNodeType_LazyHelperThunk;
    }

    virtual void Save(ZapWriter * pZapWriter);
};

#endif // __ZAPCODE_H__
