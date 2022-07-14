// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: dacimpl.h
//

//
// Central header file for external data access implementation.
//
//*****************************************************************************


#ifndef __DACIMPL_H__
#define __DACIMPL_H__

#include "gcinterface.dac.h"
//---------------------------------------------------------------------------------------
// Setting DAC_HASHTABLE tells the DAC to use the hand rolled hashtable for
// storing code:DAC_INSTANCE .  Otherwise, the DAC uses the STL unordered_map to.

#define DAC_HASHTABLE

#ifndef DAC_HASHTABLE
#pragma push_macro("return")
#undef return
#include <unordered_map>
#pragma pop_macro("return")
#endif //DAC_HASHTABLE
extern CRITICAL_SECTION g_dacCritSec;

// Convert between CLRDATA_ADDRESS and TADDR.
// Note that CLRDATA_ADDRESS is sign-extended (for compat with Windbg and OS conventions).  Converting
// from pointer-size values to CLRDATA_ADDRESS should ALWAYS use this TO_CDADDR macro to avoid bugs when
// dealing with 3/4GB 32-bit address spaces.  You must not rely on the compiler's implicit conversion
// from ULONG32 to ULONG64 - it is incorrect.  Ideally we'd use some compiler tricks or static analysis
// to help detect such errors (they are nefarious since 3/4GB addresses aren't well tested) .
//
// Note: We're in the process of switching the implementation over to CORDB_ADDRESS instead, which is also
// 64 bits, but 0-extended.  This means that conversions between TADDR and CORDB_ADDRESS are simple and natural,
// but as long as we have some legacy code, conversions involving CLRDATA_ADDRESS are a pain.  Eventually we
// should eliminate CLRDATA_ADDRESS entirely from the implementation, but that will require moving SOS off of
// the old DAC stuff etc.
//
// Here are the possible conversions:
// TADDR -> CLRDATA_ADDRESS:         TO_CDADDR
// CORDB_ADDRESS -> CLRDATA_ADDRESS: TO_CDADDR
// CLRDATA_ADDRESS -> TADDR:         CLRDATA_ADDRESS_TO_TADDR
// CORDB_ADDRESS -> TADDR:           CORDB_ADDRESS_TO_TADDR
// TADDR -> CORDB_ADDRESS:           implicit
// CLRDATA_ADDRESS -> CORDB_ADDRESS: CLRDATA_ADDRESS_TO_TADDR
//
#define TO_CDADDR(taddr) ((CLRDATA_ADDRESS)(LONG_PTR)(taddr))

// Convert a CLRDATA_ADDRESS (64-bit unsigned sign-extended target address) to a TADDR
inline TADDR CLRDATA_ADDRESS_TO_TADDR(CLRDATA_ADDRESS cdAddr)
{
    SUPPORTS_DAC;
#ifndef HOST_64BIT
    static_assert_no_msg(sizeof(TADDR)==sizeof(UINT));
    INT64 iSignedAddr = (INT64)cdAddr;
    if (iSignedAddr > INT_MAX || iSignedAddr < INT_MIN)
    {
        _ASSERTE_MSG(false, "CLRDATA_ADDRESS out of range for this platform");
        DacError(E_INVALIDARG);
    }
#endif
    return (TADDR)cdAddr;
}

// No throw, Convert a CLRDATA_ADDRESS (64-bit unsigned sign-extended target address) to a TADDR
// Use this in places where we know windbg may pass in bad addresses
inline HRESULT TRY_CLRDATA_ADDRESS_TO_TADDR(CLRDATA_ADDRESS cdAddr, TADDR* pOutTaddr)
{
    SUPPORTS_DAC;
#ifndef HOST_64BIT
    static_assert_no_msg(sizeof(TADDR)==sizeof(UINT));
    INT64 iSignedAddr = (INT64)cdAddr;
    if (iSignedAddr > INT_MAX || iSignedAddr < INT_MIN)
    {
        *pOutTaddr = 0;
        return E_INVALIDARG;
    }
#endif
    *pOutTaddr = (TADDR)cdAddr;
    return S_OK;
}

// Convert a CORDB_ADDRESS (64-bit unsigned 0-extended target address) to a TADDR
inline TADDR CORDB_ADDRESS_TO_TADDR(CORDB_ADDRESS cdbAddr)
{
    SUPPORTS_DAC;
#ifndef HOST_64BIT
    static_assert_no_msg(sizeof(TADDR)==sizeof(UINT));
    if (cdbAddr > UINT_MAX)
    {
        _ASSERTE_MSG(false, "CORDB_ADDRESS out of range for this platform");
        DacError(E_INVALIDARG);
    }
#endif
    return (TADDR)cdbAddr;
}

// TO_TADDR is the old way of converting CLRDATA_ADDRESSes to TADDRs.  Unfortunately,
// this has been used in many places to also cast pointers (void* etc.) to TADDR, and
// so we can't actually require the argument to be a valid CLRDATA_ADDRESS.  New code
// should use CLRDATA_ADDRESS_TO_TADDR instead.
#define TO_TADDR(cdaddr) ((TADDR)(cdaddr))

#define TO_CDENUM(ptr) ((CLRDATA_ENUM)(ULONG_PTR)(ptr))
#define FROM_CDENUM(type, cdenum) ((type*)(ULONG_PTR)(cdenum))

#define SIMPFRAME_ALL \
    (CLRDATA_SIMPFRAME_UNRECOGNIZED | \
     CLRDATA_SIMPFRAME_MANAGED_METHOD | \
     CLRDATA_SIMPFRAME_RUNTIME_MANAGED_CODE | \
     CLRDATA_SIMPFRAME_RUNTIME_UNMANAGED_CODE)

enum DAC_USAGE_TYPE
{
    DAC_DPTR,
    DAC_VPTR,
    DAC_STRA,
    DAC_STRW,
    DAC_PAL,
};

class ReflectionModule;

struct DAC_MD_IMPORT
{
    DAC_MD_IMPORT* next;       // list link field
    TADDR peFile;              // a TADDR for a PEAssembly* or a ReflectionModule*
    IMDInternalImport* impl;   // Associated metadata interface
    bool isAlternate;          // for NGEN images set to true if the metadata corresponds to the IL image

    DAC_MD_IMPORT(TADDR peFile_,
        IMDInternalImport* impl_,
        bool isAlt_ = false,
        DAC_MD_IMPORT* next_ = NULL)
        : next(next_)
        , peFile(peFile_)
        , impl(impl_)
        , isAlternate(isAlt_)
    {
        SUPPORTS_DAC_HOST_ONLY;
    }
};


// This class maintains a cache of IMDInternalImport* and their corresponding
// source (a PEAssembly* or a ReflectionModule*), as a singly-linked list of
// DAC_MD_IMPORT nodes.  The cache is flushed whenever the process state changes
// by calling its Flush() member function.
class MDImportsCache
{
public:

    MDImportsCache()
        : m_head(NULL)
    {}

    ~MDImportsCache()
    {
        Flush();
    }

    FORCEINLINE
    IMDInternalImport* Get(TADDR key) const
    {
        SUPPORTS_DAC;
        for (DAC_MD_IMPORT* importList = m_head; importList; importList = importList->next)
        {
            if (importList->peFile == key)
            {
                return importList->impl;
            }
        }
        return NULL;
    }

    FORCEINLINE
    DAC_MD_IMPORT* Add(TADDR peFile, IMDInternalImport* impl, bool isAlt)
    {
        SUPPORTS_DAC;
        DAC_MD_IMPORT* importList = new (nothrow) DAC_MD_IMPORT(peFile, impl, isAlt, m_head);
        if (!importList)
        {
            return NULL;
        }

        m_head = importList;
        return importList;
    }

    void Flush()
    {
        DAC_MD_IMPORT* importList;

        while (m_head)
        {
            importList = m_head;
            m_head = importList->next;
            importList->impl->Release();
            delete importList;
        }
    }

private:

    DAC_MD_IMPORT* m_head;  // the beginning of the list of cached MD imports

};

struct METH_EXTENTS
{
    ULONG32 numExtents;
    ULONG32 curExtent;
    // Currently only one is needed.
    CLRDATA_ADDRESS_RANGE extents[1];
};

HRESULT ConvertUtf8(_In_ LPCUTF8 utf8,
                    ULONG32 bufLen,
                    ULONG32* nameLen,
                    _Out_writes_to_opt_(bufLen, *nameLen) PWSTR buffer);
HRESULT AllocUtf8(_In_opt_ LPCWSTR wstr,
                  ULONG32 srcChars,
                  _Outptr_ LPUTF8* utf8);

HRESULT GetFullClassNameFromMetadata(IMDInternalImport* mdImport,
                                     mdTypeDef classToken,
                                     ULONG32 bufferChars,
                                     _Inout_updates_(bufferChars) LPUTF8 buffer);
HRESULT GetFullMethodNameFromMetadata(IMDInternalImport* mdImport,
                                      mdMethodDef methodToken,
                                      ULONG32 bufferChars,
                                      _Inout_updates_(bufferChars) LPUTF8 buffer);

enum SplitSyntax
{
    SPLIT_METHOD,
    SPLIT_TYPE,
    SPLIT_FIELD,
    SPLIT_NO_NAME,
};

HRESULT SplitFullName(_In_z_ PCWSTR fullName,
                      SplitSyntax syntax,
                      ULONG32 memberDots,
                      _Outptr_opt_ LPUTF8* namespaceName,
                      _Outptr_opt_ LPUTF8* typeName,
                      _Outptr_opt_ LPUTF8* memberName,
                      _Outptr_opt_ LPUTF8* params);

int CompareUtf8(_In_ LPCUTF8 str1, _In_ LPCUTF8 str2, _In_ ULONG32 nameFlags);

#define INH_STATIC \
    (CLRDATA_VALUE_ALL_KINDS | \
     CLRDATA_VALUE_IS_INHERITED | CLRDATA_VALUE_FROM_STATIC)

HRESULT InitFieldIter(DeepFieldDescIterator* fieldIter,
                      TypeHandle typeHandle,
                      bool canHaveFields,
                      ULONG32 flags,
                      IXCLRDataTypeInstance* fromType);

ULONG32 GetTypeFieldValueFlags(TypeHandle typeHandle,
                               FieldDesc* fieldDesc,
                               ULONG32 otherFlags,
                               bool isDeref);

//----------------------------------------------------------------------------
//
// MetaEnum.
//
//----------------------------------------------------------------------------

class MetaEnum
{
public:
    MetaEnum(void)
        : m_domainIter(FALSE)
    {
        Clear();
        m_appDomain = NULL;
    }
    ~MetaEnum(void)
    {
        End();
    }

    void Clear(void)
    {
        m_mdImport = NULL;
        m_kind = 0;
        m_lastToken = mdTokenNil;
    }

    HRESULT Start(IMDInternalImport* mdImport, ULONG32 kind,
                  mdToken container);
    void End(void);

    HRESULT NextToken(mdToken* token,
                      _Outptr_opt_result_maybenull_ LPCUTF8* namespaceName,
                      _Outptr_opt_result_maybenull_ LPCUTF8* name);
    HRESULT NextDomainToken(AppDomain** appDomain,
                            mdToken* token);
    HRESULT NextTokenByName(_In_opt_ LPCUTF8 namespaceName,
                            _In_opt_ LPCUTF8 name,
                            ULONG32 nameFlags,
                            mdToken* token);
    HRESULT NextDomainTokenByName(_In_opt_ LPCUTF8 namespaceName,
                                  _In_opt_ LPCUTF8 name,
                                  ULONG32 nameFlags,
                                  AppDomain** appDomain, mdToken* token);

    static HRESULT CdNextToken(CLRDATA_ENUM* handle,
                               mdToken* token)
    {
        MetaEnum* iter = FROM_CDENUM(MetaEnum, *handle);
        if (!iter)
        {
            return S_FALSE;
        }

        return iter->NextToken(token, NULL, NULL);
    }
    static HRESULT CdNextDomainToken(CLRDATA_ENUM* handle,
                                     AppDomain** appDomain,
                                     mdToken* token)
    {
        MetaEnum* iter = FROM_CDENUM(MetaEnum, *handle);
        if (!iter)
        {
            return S_FALSE;
        }

        return iter->NextDomainToken(appDomain, token);
    }
    static HRESULT CdEnd(CLRDATA_ENUM handle)
    {
        MetaEnum* iter = FROM_CDENUM(MetaEnum, handle);
        if (iter)
        {
            delete iter;
            return S_OK;
        }
        else
        {
            return E_INVALIDARG;
        }
    }

    IMDInternalImport* m_mdImport;
    ULONG32 m_kind;
    HENUMInternal m_enum;
    AppDomain* m_appDomain;
    AppDomainIterator m_domainIter;
    mdToken m_lastToken;

    static HRESULT New(Module* mod,
                       ULONG32 kind,
                       mdToken container,
                       IXCLRDataAppDomain* pubAppDomain,
                       MetaEnum** metaEnum,
                       CLRDATA_ENUM* handle);
};

//----------------------------------------------------------------------------
//
// SplitName.
//
//----------------------------------------------------------------------------

class SplitName
{
public:
    // Type of name and splitting being done in this instance.
    SplitSyntax m_syntax;
    ULONG32 m_nameFlags;
    ULONG32 m_memberDots;

    // Split fields.
    LPUTF8 m_namespaceName;
    LPUTF8 m_typeName;
    mdTypeDef m_typeToken;
    LPUTF8 m_memberName;
    mdMethodDef m_memberToken;
    LPUTF8 m_params;
    // XXX Microsoft - Translated signature.

    // Arbitrary extra data.
    Thread* m_tlsThread;
    Module* m_module;
    MetaEnum m_metaEnum;
    DeepFieldDescIterator m_fieldEnum;
    ULONG64 m_objBase;
    FieldDesc* m_lastField;

    SplitName(SplitSyntax syntax, ULONG32 nameFlags,
              ULONG32 memberDots);
    ~SplitName(void)
    {
        Delete();
    }

    void Delete(void);
    void Clear(void);

    HRESULT SplitString(_In_opt_ PCWSTR fullName);

    bool FindType(IMDInternalImport* mdInternal);
    bool FindMethod(IMDInternalImport* mdInternal);
    bool FindField(IMDInternalImport* mdInternal);

    int Compare(LPCUTF8 str1, LPCUTF8 str2)
    {
        return CompareUtf8(str1, str2, m_nameFlags);
    }

    static HRESULT AllocAndSplitString(_In_opt_ PCWSTR fullName,
                                       SplitSyntax syntax,
                                       ULONG32 nameFlags,
                                       ULONG32 memberDots,
                                       SplitName** split);

    static HRESULT CdStartMethod(_In_opt_ PCWSTR fullName,
                                 ULONG32 nameFlags,
                                 Module* mod,
                                 mdTypeDef typeToken,
                                 AppDomain* appDomain,
                                 IXCLRDataAppDomain* pubAppDomain,
                                 SplitName** split,
                                 CLRDATA_ENUM* handle);
    static HRESULT CdNextMethod(CLRDATA_ENUM* handle,
                                mdMethodDef* token);
    static HRESULT CdNextDomainMethod(CLRDATA_ENUM* handle,
                                      AppDomain** appDomain,
                                      mdMethodDef* token);

    static HRESULT CdStartField(_In_opt_ PCWSTR fullName,
                                ULONG32 nameFlags,
                                ULONG32 fieldFlags,
                                IXCLRDataTypeInstance* fromTypeInst,
                                TypeHandle typeHandle,
                                Module* mod,
                                mdTypeDef typeToken,
                                ULONG64 objBase,
                                Thread* tlsThread,
                                IXCLRDataTask* pubTlsThread,
                                AppDomain* appDomain,
                                IXCLRDataAppDomain* pubAppDomain,
                                SplitName** split,
                                CLRDATA_ENUM* handle);
    static HRESULT CdNextField(ClrDataAccess* dac,
                               CLRDATA_ENUM* handle,
                               IXCLRDataTypeDefinition** fieldType,
                               ULONG32* fieldFlags,
                               IXCLRDataValue** value,
                               ULONG32 nameBufRetLen,
                               ULONG32* nameLenRet,
                               _Out_writes_to_opt_(nameBufRetLen, *nameLenRet) WCHAR nameBufRet[  ],
                               IXCLRDataModule** tokenScopeRet,
                               mdFieldDef* tokenRet);
    static HRESULT CdNextDomainField(ClrDataAccess* dac,
                                     CLRDATA_ENUM* handle,
                                     IXCLRDataValue** value);

    static HRESULT CdStartType(_In_opt_ PCWSTR fullName,
                               ULONG32 nameFlags,
                               Module* mod,
                               AppDomain* appDomain,
                               IXCLRDataAppDomain* pubAppDomain,
                               SplitName** split,
                               CLRDATA_ENUM* handle);
    static HRESULT CdNextType(CLRDATA_ENUM* handle,
                              mdTypeDef* token);
    static HRESULT CdNextDomainType(CLRDATA_ENUM* handle,
                                    AppDomain** appDomain,
                                    mdTypeDef* token);

    static HRESULT CdEnd(CLRDATA_ENUM handle)
    {
        SplitName* split = FROM_CDENUM(SplitName, handle);
        if (split)
        {
            delete split;
            return S_OK;
        }
        else
        {
            return E_INVALIDARG;
        }
    }
};

//----------------------------------------------------------------------------
//
// ProcessModIter.
//
//----------------------------------------------------------------------------

struct ProcessModIter
{
    AppDomainIterator m_domainIter;
    bool m_nextDomain;
    AppDomain::AssemblyIterator m_assemIter;
    Assembly* m_curAssem;

    ProcessModIter(void)
        : m_domainIter(FALSE)
    {
        SUPPORTS_DAC;
        m_nextDomain = true;
        m_curAssem = NULL;
    }

    Assembly * NextAssem()
    {
        SUPPORTS_DAC;
        for (;;)
        {
            if (m_nextDomain)
            {
                if (!m_domainIter.Next())
                {
                    break;
                }

                m_nextDomain = false;

                m_assemIter = m_domainIter.GetDomain()->IterateAssembliesEx((AssemblyIterationFlags)(
                    kIncludeLoaded | kIncludeExecution));
            }

            CollectibleAssemblyHolder<DomainAssembly *> pDomainAssembly;
            if (!m_assemIter.Next(pDomainAssembly.This()))
            {
                m_nextDomain = true;
                continue;
            }

            // Note: DAC doesn't need to keep the assembly alive - see code:CollectibleAssemblyHolder#CAH_DAC
            CollectibleAssemblyHolder<Assembly *> pAssembly = pDomainAssembly->GetAssembly();
            return pAssembly;
        }
        return NULL;
    }

    Module* NextModule(void)
    {
        SUPPORTS_DAC;
        m_curAssem = NextAssem();
        if (!m_curAssem)
        {
            return NULL;
        }

        return m_curAssem->GetModule();
    }
};

//----------------------------------------------------------------------------
//
// DacInstanceManager.
//
//----------------------------------------------------------------------------

// The data for an access may have special alignment needs and
// the cache must provide similar semantics.
#define DAC_INSTANCE_ALIGN 16

#define DAC_INSTANCE_SIG 0xdac1

// The instance manager allocates large blocks and then
// suballocates those for particular instances.
struct DAC_INSTANCE_BLOCK
{
    DAC_INSTANCE_BLOCK* next;
    ULONG32 bytesUsed;
    ULONG32 bytesFree;
};

#define DAC_INSTANCE_BLOCK_ALLOCATION 0x40000

// Sufficient memory is allocated to guarantee storage of the
// instance header plus room for alignment padding.
// Once the aligned pointer is found, this structure is prepended to
// the aligned pointer and therefore doesn't affect the alignment
// of the actual instance data.
struct DAC_INSTANCE
{
    DAC_INSTANCE* next;
    TADDR addr;
    ULONG32 size;
    // Identifying marker to give a simple
    // check for host->taddr validity.
    ULONG32 sig:16;
    // DPTR or VPTR.  See code:DAC_USAGE_TYPE
    ULONG32 usage:2;

    // Marker that can be used to prevent reporting this memory to the callback
    // object (via ICLRDataEnumMemoryRegionsCallback:EnumMemoryRegion)
    // more than once. This bit is checked only by the DacEnumHost?PtrMem
    // macros, so consistent use of those macros ensures that the memory is
    // reported at most once
    ULONG32 enumMem:1;

    // Marker to prevent metadata gets reported to mini-dump
    ULONG32 noReport:1;

    // Marker to determine if EnumMemoryRegions has been called on
    // a method descriptor
    ULONG32 MDEnumed:1;

#ifdef HOST_64BIT
    // Keep DAC_INSTANCE a multiple of DAC_INSTANCE_ALIGN
    // bytes in size.
    ULONG32 pad[2];
#endif
};

struct DAC_INSTANCE_PUSH
{
    DAC_INSTANCE_PUSH* next;
    DAC_INSTANCE_BLOCK* blocks;
    ULONG64 blockMemUsage;
    ULONG32 numInst;
    ULONG64 instMemUsage;
};

// The runtime will want the best access locality possible,
// so it's likely that many instances will be clustered.
// The hash function needs to spread near addresses across
// hash entries, so hash on the low bits of the target address.
// Not all the way down to the LSB, though, as there generally
// won't be individual accesses at the byte level.  Assume that
// most accesses will be natural-word aligned.
#define DAC_INSTANCE_HASH_BITS 10
#define DAC_INSTANCE_HASH_SHIFT 2

#define DAC_INSTANCE_HASH(addr) \
    (((ULONG32)(ULONG_PTR)(addr) >> DAC_INSTANCE_HASH_SHIFT) & \
     ((1 << DAC_INSTANCE_HASH_BITS) - 1))
#define DAC_INSTANCE_HASH_SIZE (1 << DAC_INSTANCE_HASH_BITS)


struct DumpMemoryReportStatics
{
    TSIZE_T    m_cbStack;              // number of bytes that we report directly for stack walk
    TSIZE_T    m_cbNgen;               // number of bytes that we report directly for ngen images
    TSIZE_T    m_cbModuleList;         // number of bytes that we report for module list directly
    TSIZE_T    m_cbClrStatics;         // number of bytes that we report for CLR statics
    TSIZE_T    m_cbClrHeapStatics;     // number of bytes that we report for CLR heap statics
    TSIZE_T    m_cbImplicity;          // number of bytes that we report implicitly
};


class DacInstanceManager
{
public:
    DacInstanceManager(void);
    ~DacInstanceManager(void);

    DAC_INSTANCE* Add(DAC_INSTANCE* inst);

    DAC_INSTANCE* Alloc(TADDR addr, ULONG32 size, DAC_USAGE_TYPE usage);
    void ReturnAlloc(DAC_INSTANCE* inst);
    DAC_INSTANCE* Find(TADDR addr);
    HRESULT Write(DAC_INSTANCE* inst, bool throwEx);
    void Supersede(DAC_INSTANCE* inst);
    void Flush(void);
    void Flush(bool fSaveBlock);
    void ClearEnumMemMarker(void);

    void AddSuperseded(DAC_INSTANCE* inst)
    {
        SUPPORTS_DAC;
        inst->next = m_superseded;
        m_superseded = inst;
    }

    UINT DumpAllInstances(ICLRDataEnumMemoryRegionsCallback *pCallBack);

private:

    DAC_INSTANCE_BLOCK* FindInstanceBlock(DAC_INSTANCE* inst);
    void FreeAllBlocks(bool fSaveBlock);

    void InitEmpty(void)
    {
        m_blocks = NULL;
        // m_unusedBlock is not NULLed here; it can contain one block we will use after
        // a flush is complete.
        m_blockMemUsage = 0;
        m_numInst = 0;
        m_instMemUsage = 0;
#ifdef DAC_HASHTABLE
        ZeroMemory(m_hash, sizeof(m_hash));
#endif
        m_superseded = NULL;
        m_instPushed = NULL;
    }

#if defined(DAC_HASHTABLE)

    typedef struct _HashInstanceKey {
        TADDR addr;
        DAC_INSTANCE* instance;
    } HashInstanceKey;

    typedef struct _HashInstanceKeyBlock {
        // Blocks are chained in reverse order of allocation so that the most recently allocated
        // block is searched first.
        _HashInstanceKeyBlock* next;

        // Entries to a block are added from the max index on down so that recently added
        // entries are at the start of the block.
        DWORD firstElement;
        HashInstanceKey instanceKeys[] ;
    } HashInstanceKeyBlock;

// The hashing function does a good job of distributing the entries across buckets. To handle a
// SO on x86, we have under 250 entries in a bucket. A 4K block size allows 511 entries on x86 and
// about half that on x64. On x64, the number of entries added to the hash table is significantly
// smaller than on x86 (and the max recursion depth for default stack sizes is also far less), so
// 4K is generally adequate.

#define HASH_INSTANCE_BLOCK_ALLOC_SIZE (4 * 1024)
#define HASH_INSTANCE_BLOCK_NUM_ELEMENTS ((HASH_INSTANCE_BLOCK_ALLOC_SIZE - offsetof(_HashInstanceKeyBlock, instanceKeys))/sizeof(HashInstanceKey))
#endif // #if defined(DAC_HASHTABLE)

    DAC_INSTANCE_BLOCK* m_blocks;
    DAC_INSTANCE_BLOCK* m_unusedBlock;
    ULONG64 m_blockMemUsage;
    ULONG32 m_numInst;
    ULONG64 m_instMemUsage;

#if defined(DAC_HASHTABLE)
    HashInstanceKeyBlock* m_hash[DAC_INSTANCE_HASH_SIZE];
#else //DAC_HASHTABLE

    // We're going to use the STL unordered_map for our instance hash.
    // This has the benefit of scaling to different workloads appropriately (as opposed to having a
    // fixed number of buckets).

    class DacHashCompare : public std::hash_compare<TADDR>
    {
    public:
        // Custom hash function
        // The default hash function uses a pseudo-randomizing function to get a random
        // distribution.  In our case, we'd actually like a more even distribution to get
        // better access locality (see comments for DAC_INSTANCE_HASH_BITS).
        //
        // Also, when enumerating the hash during dump generation, clustering nearby addresses
        // together can have a significant positive impact on the performance of the minidump
        // library (at least the un-optimized version 6.5 linked into our dw20.exe - on Vista+
        // we use the OS's version 6.7+ with radically improved perf characteristics).  Having
        // a random distribution is actually the worst-case because it means most blocks won't
        // be merged until near the end, and a large number of intermediate blocks will have to
        // be searched with each call.
        //
        // The default pseudo-randomizing function also requires a call to ldiv which shows up as
        // a 3%-5% perf hit in most perf-sensitive scenarios, so this should also always be
        // faster.
        inline size_t operator()(const TADDR& keyval) const
        {
            return (unsigned)(keyval >>DAC_INSTANCE_HASH_SHIFT);
        }

        // Explicitly bring in the two-argument comparison function from the base class (just less-than)
        // This is necessary because once we override one form of operator() above, we don't automatically
        // get the others by C++ inheritance rules.
    using std::hash_compare<TADDR>::operator();

#ifdef NIDUMP_CUSTOMIZED_DAC_HASH   // not set
        //this particular number is supposed to be roughly the same amount of
        //memory as the old code (buckets * number of entries in the old
        //blocks.)
        //disabled for now.  May tweak implementation later.  It turns out that
        //having a large number of initial buckets is excellent for nidump, but it
        // is terrible for most other scenarios due to the cost of clearing them at
        // every Flush.  Once there is a better perf suite, we can tweak these values more.
        static const size_t min_buckets = DAC_INSTANCE_HASH_SIZE * 256;
#endif

    };
    typedef std::unordered_map<TADDR, DAC_INSTANCE*, DacHashCompare > DacInstanceHash;
    typedef DacInstanceHash::value_type DacInstanceHashValue;
    typedef DacInstanceHash::iterator DacInstanceHashIterator;
    DacInstanceHash m_hash;
#endif //DAC_HASHTABLE

    DAC_INSTANCE* m_superseded;
    DAC_INSTANCE_PUSH* m_instPushed;
};


#ifdef FEATURE_MINIMETADATA_IN_TRIAGEDUMPS

class DacStreamManager;

#endif // FEATURE_MINIMETADATA_IN_TRIAGEDUMPS


//----------------------------------------------------------------------------
//
// ClrDataAccess.
//
//----------------------------------------------------------------------------

class ClrDataAccess
    : public IXCLRDataProcess2,
      public ICLRDataEnumMemoryRegions,
      public ISOSDacInterface,
      public ISOSDacInterface2,
      public ISOSDacInterface3,
      public ISOSDacInterface4,
      public ISOSDacInterface5,
      public ISOSDacInterface6,
      public ISOSDacInterface7,
      public ISOSDacInterface8,
      public ISOSDacInterface9,
      public ISOSDacInterface10,
      public ISOSDacInterface11,
      public ISOSDacInterface12
{
public:
    ClrDataAccess(ICorDebugDataTarget * pTarget, ICLRDataTarget * pLegacyTarget=0);
    virtual ~ClrDataAccess(void);

    // IUnknown.
    STDMETHOD(QueryInterface)(THIS_
                              IN REFIID interfaceId,
                              OUT PVOID* iface);
    STDMETHOD_(ULONG, AddRef)(THIS);
    STDMETHOD_(ULONG, Release)(THIS);

    //
    // IXCLRDataProcess.
    //

    virtual HRESULT STDMETHODCALLTYPE Flush( void);

    virtual HRESULT STDMETHODCALLTYPE StartEnumTasks(
        /* [out] */ CLRDATA_ENUM *handle);

    virtual HRESULT STDMETHODCALLTYPE EnumTask(
        /* [in, out] */ CLRDATA_ENUM* handle,
        /* [out] */ IXCLRDataTask **task);

    virtual HRESULT STDMETHODCALLTYPE EndEnumTasks(
        /* [in] */ CLRDATA_ENUM handle);

    virtual HRESULT STDMETHODCALLTYPE GetTaskByOSThreadID(
        /* [in] */ ULONG32 OSThreadID,
        /* [out] */ IXCLRDataTask **task);

    virtual HRESULT STDMETHODCALLTYPE GetTaskByUniqueID(
        /* [in] */ ULONG64 uniqueID,
        /* [out] */ IXCLRDataTask **task);

    virtual HRESULT STDMETHODCALLTYPE GetFlags(
        /* [out] */ ULONG32 *flags);

    virtual HRESULT STDMETHODCALLTYPE IsSameObject(
        /* [in] */ IXCLRDataProcess *process);

    virtual HRESULT STDMETHODCALLTYPE GetManagedObject(
        /* [out] */ IXCLRDataValue **value);

    virtual HRESULT STDMETHODCALLTYPE GetDesiredExecutionState(
        /* [out] */ ULONG32 *state);

    virtual HRESULT STDMETHODCALLTYPE SetDesiredExecutionState(
        /* [in] */ ULONG32 state);

    virtual HRESULT STDMETHODCALLTYPE GetAddressType(
        /* [in] */ CLRDATA_ADDRESS address,
        /* [out] */ CLRDataAddressType* type);

    virtual HRESULT STDMETHODCALLTYPE GetRuntimeNameByAddress(
        /* [in] */ CLRDATA_ADDRESS address,
        /* [in] */ ULONG32 flags,
        /* [in] */ ULONG32 bufLen,
        /* [out] */ ULONG32 *nameLen,
        /* [size_is][out] */ _Out_writes_bytes_opt_(bufLen) WCHAR nameBuf[  ],
        /* [out] */ CLRDATA_ADDRESS* displacement);

    virtual HRESULT STDMETHODCALLTYPE StartEnumAppDomains(
        /* [out] */ CLRDATA_ENUM *handle);

    virtual HRESULT STDMETHODCALLTYPE EnumAppDomain(
        /* [in, out] */ CLRDATA_ENUM* handle,
        /* [out] */ IXCLRDataAppDomain **appDomain);

    virtual HRESULT STDMETHODCALLTYPE EndEnumAppDomains(
        /* [in] */ CLRDATA_ENUM handle);

    virtual HRESULT STDMETHODCALLTYPE GetAppDomainByUniqueID(
        /* [in] */ ULONG64 uniqueID,
        /* [out] */ IXCLRDataAppDomain **appDomain);

    virtual HRESULT STDMETHODCALLTYPE StartEnumAssemblies(
        /* [out] */ CLRDATA_ENUM *handle);

    virtual HRESULT STDMETHODCALLTYPE EnumAssembly(
        /* [in, out] */ CLRDATA_ENUM* handle,
        /* [out] */ IXCLRDataAssembly **assembly);

    virtual HRESULT STDMETHODCALLTYPE EndEnumAssemblies(
        /* [in] */ CLRDATA_ENUM handle);

    virtual HRESULT STDMETHODCALLTYPE StartEnumModules(
        /* [out] */ CLRDATA_ENUM *handle);

    virtual HRESULT STDMETHODCALLTYPE EnumModule(
        /* [in, out] */ CLRDATA_ENUM* handle,
        /* [out] */ IXCLRDataModule **mod);

    virtual HRESULT STDMETHODCALLTYPE EndEnumModules(
        /* [in] */ CLRDATA_ENUM handle);

    virtual HRESULT STDMETHODCALLTYPE GetModuleByAddress(
        /* [in] */ CLRDATA_ADDRESS address,
        /* [out] */ IXCLRDataModule** mod);

    virtual HRESULT STDMETHODCALLTYPE StartEnumMethodDefinitionsByAddress(
        /* [in] */ CLRDATA_ADDRESS address,
        /* [out] */ CLRDATA_ENUM *handle);

    virtual HRESULT STDMETHODCALLTYPE EnumMethodDefinitionByAddress(
        /* [in] */ CLRDATA_ENUM* handle,
        /* [out] */ IXCLRDataMethodDefinition **method);

    virtual HRESULT STDMETHODCALLTYPE EndEnumMethodDefinitionsByAddress(
        /* [in] */ CLRDATA_ENUM handle);

    virtual HRESULT STDMETHODCALLTYPE StartEnumMethodInstancesByAddress(
        /* [in] */ CLRDATA_ADDRESS address,
        /* [in] */ IXCLRDataAppDomain* appDomain,
        /* [out] */ CLRDATA_ENUM *handle);

    virtual HRESULT STDMETHODCALLTYPE EnumMethodInstanceByAddress(
        /* [in] */ CLRDATA_ENUM* handle,
        /* [out] */ IXCLRDataMethodInstance **method);

    virtual HRESULT STDMETHODCALLTYPE EndEnumMethodInstancesByAddress(
        /* [in] */ CLRDATA_ENUM handle);

    virtual HRESULT STDMETHODCALLTYPE GetDataByAddress(
        /* [in] */ CLRDATA_ADDRESS address,
        /* [in] */ ULONG32 flags,
        /* [in] */ IXCLRDataAppDomain* appDomain,
        /* [in] */ IXCLRDataTask* tlsTask,
        /* [in] */ ULONG32 bufLen,
        /* [out] */ ULONG32 *nameLen,
        /* [size_is][out] */ _Out_writes_to_opt_(bufLen, *nameLen) WCHAR nameBuf[  ],
        /* [out] */ IXCLRDataValue **value,
        /* [out] */ CLRDATA_ADDRESS *displacement);

    virtual HRESULT STDMETHODCALLTYPE GetExceptionStateByExceptionRecord(
        /* [in] */ EXCEPTION_RECORD64 *record,
        /* [out] */ IXCLRDataExceptionState **exception);

    virtual HRESULT STDMETHODCALLTYPE TranslateExceptionRecordToNotification(
        /* [in] */ EXCEPTION_RECORD64 *record,
        /* [in] */ IXCLRDataExceptionNotification *notify);

    virtual HRESULT STDMETHODCALLTYPE CreateMemoryValue(
        /* [in] */ IXCLRDataAppDomain* appDomain,
        /* [in] */ IXCLRDataTask* tlsTask,
        /* [in] */ IXCLRDataTypeInstance* type,
        /* [in] */ CLRDATA_ADDRESS addr,
        /* [out] */ IXCLRDataValue** value);

    virtual HRESULT STDMETHODCALLTYPE SetAllTypeNotifications(
        /* [in] */ IXCLRDataModule* mod,
        /* [in] */ ULONG32 flags);

    virtual HRESULT STDMETHODCALLTYPE SetAllCodeNotifications(
        /* [in] */ IXCLRDataModule* mod,
        /* [in] */ ULONG32 flags);

    virtual HRESULT STDMETHODCALLTYPE GetTypeNotifications(
        /* [in] */ ULONG32 numTokens,
        /* [in, size_is(numTokens)] */ IXCLRDataModule* mods[],
        /* [in] */ IXCLRDataModule* singleMod,
        /* [in, size_is(numTokens)] */ mdTypeDef tokens[],
        /* [out, size_is(numTokens)] */ ULONG32 flags[]);

    virtual HRESULT STDMETHODCALLTYPE SetTypeNotifications(
        /* [in] */ ULONG32 numTokens,
        /* [in, size_is(numTokens)] */ IXCLRDataModule* mods[],
        /* [in] */ IXCLRDataModule* singleMod,
        /* [in, size_is(numTokens)] */ mdTypeDef tokens[],
        /* [in, size_is(numTokens)] */ ULONG32 flags[],
        /* [in] */ ULONG32 singleFlags);

    virtual HRESULT STDMETHODCALLTYPE GetCodeNotifications(
        /* [in] */ ULONG32 numTokens,
        /* [in, size_is(numTokens)] */ IXCLRDataModule* mods[],
        /* [in] */ IXCLRDataModule* singleMod,
        /* [in, size_is(numTokens)] */ mdMethodDef tokens[],
        /* [out, size_is(numTokens)] */ ULONG32 flags[]);

    virtual HRESULT STDMETHODCALLTYPE SetCodeNotifications(
        /* [in] */ ULONG32 numTokens,
        /* [in, size_is(numTokens)] */ IXCLRDataModule* mods[],
        /* [in] */ IXCLRDataModule* singleMod,
        /* [in, size_is(numTokens)] */ mdMethodDef tokens[],
        /* [in, size_is(numTokens)] */ ULONG32 flags[],
        /* [in] */ ULONG32 singleFlags);

    virtual HRESULT STDMETHODCALLTYPE GetOtherNotificationFlags(
        /* [out] */ ULONG32* flags);

    virtual HRESULT STDMETHODCALLTYPE SetOtherNotificationFlags(
        /* [in] */ ULONG32 flags);

    virtual HRESULT STDMETHODCALLTYPE FollowStub(
        /* [in] */ ULONG32 inFlags,
        /* [in] */ CLRDATA_ADDRESS inAddr,
        /* [in] */ CLRDATA_FOLLOW_STUB_BUFFER* inBuffer,
        /* [out] */ CLRDATA_ADDRESS* outAddr,
        /* [out] */ CLRDATA_FOLLOW_STUB_BUFFER* outBuffer,
        /* [out] */ ULONG32* outFlags);

    virtual HRESULT STDMETHODCALLTYPE FollowStub2(
        /* [in] */ IXCLRDataTask* task,
        /* [in] */ ULONG32 inFlags,
        /* [in] */ CLRDATA_ADDRESS inAddr,
        /* [in] */ CLRDATA_FOLLOW_STUB_BUFFER* inBuffer,
        /* [out] */ CLRDATA_ADDRESS* outAddr,
        /* [out] */ CLRDATA_FOLLOW_STUB_BUFFER* outBuffer,
        /* [out] */ ULONG32* outFlags);

    virtual HRESULT STDMETHODCALLTYPE Request(
        /* [in] */ ULONG32 reqCode,
        /* [in] */ ULONG32 inBufferSize,
        /* [size_is][in] */ BYTE *inBuffer,
        /* [in] */ ULONG32 outBufferSize,
        /* [size_is][out] */ BYTE *outBuffer);


    //
    // IXCLRDataProcess2.
    //
    STDMETHOD(GetGcNotification)(/* [out] */ GcEvtArgs* gcEvtArgs);
    STDMETHOD(SetGcNotification)(/* [in] */ GcEvtArgs gcEvtArgs);

    //
    // ICLRDataEnumMemoryRegions.
    //
    virtual HRESULT STDMETHODCALLTYPE EnumMemoryRegions(
        /* [in] */ ICLRDataEnumMemoryRegionsCallback *callback,
        /* [in] */ ULONG32 miniDumpFlags,
        /* [in] */ CLRDataEnumMemoryFlags clrFlags);


    // ISOSDacInterface
    virtual HRESULT STDMETHODCALLTYPE GetThreadStoreData(struct DacpThreadStoreData *data);
    virtual HRESULT STDMETHODCALLTYPE GetAppDomainStoreData(struct DacpAppDomainStoreData *data);
    virtual HRESULT STDMETHODCALLTYPE GetAppDomainList(unsigned int count, CLRDATA_ADDRESS values[], unsigned int *pNeeded);
    virtual HRESULT STDMETHODCALLTYPE GetAppDomainData(CLRDATA_ADDRESS addr, struct DacpAppDomainData *data);
    virtual HRESULT STDMETHODCALLTYPE GetAppDomainName(CLRDATA_ADDRESS addr, unsigned int count, _Inout_updates_z_(count) WCHAR *name, unsigned int *pNeeded);
    virtual HRESULT STDMETHODCALLTYPE GetAssemblyList(CLRDATA_ADDRESS appDomain, int count, CLRDATA_ADDRESS values[], int *fetched);
    virtual HRESULT STDMETHODCALLTYPE GetAssemblyData(CLRDATA_ADDRESS baseDomainPtr, CLRDATA_ADDRESS assembly, struct DacpAssemblyData *data);
    virtual HRESULT STDMETHODCALLTYPE GetAssemblyName(CLRDATA_ADDRESS assembly, unsigned int count, _Inout_updates_z_(count) WCHAR *name, unsigned int *pNeeded);
    virtual HRESULT STDMETHODCALLTYPE GetThreadData(CLRDATA_ADDRESS thread, struct DacpThreadData *data);
    virtual HRESULT STDMETHODCALLTYPE GetThreadFromThinlockID(UINT thinLockId, CLRDATA_ADDRESS *pThread);
    virtual HRESULT STDMETHODCALLTYPE GetStackLimits(CLRDATA_ADDRESS threadPtr, CLRDATA_ADDRESS *lower, CLRDATA_ADDRESS *upper, CLRDATA_ADDRESS *fp);
    virtual HRESULT STDMETHODCALLTYPE GetDomainFromContext(CLRDATA_ADDRESS context, CLRDATA_ADDRESS *domain);

    virtual HRESULT STDMETHODCALLTYPE GetMethodDescData(CLRDATA_ADDRESS methodDesc, CLRDATA_ADDRESS ip, struct DacpMethodDescData *data, ULONG cRevertedRejitVersions, DacpReJitData * rgRevertedRejitData, ULONG * pcNeededRevertedRejitData);
    virtual HRESULT STDMETHODCALLTYPE GetMethodDescPtrFromIP(CLRDATA_ADDRESS ip, CLRDATA_ADDRESS * ppMD);
    virtual HRESULT STDMETHODCALLTYPE GetMethodDescName(CLRDATA_ADDRESS methodDesc, unsigned int count, _Inout_updates_z_(count) WCHAR *name, unsigned int *pNeeded);
    virtual HRESULT STDMETHODCALLTYPE GetMethodDescPtrFromFrame(CLRDATA_ADDRESS frameAddr, CLRDATA_ADDRESS * ppMD);
    virtual HRESULT STDMETHODCALLTYPE GetCodeHeaderData(CLRDATA_ADDRESS ip, struct DacpCodeHeaderData *data);
    virtual HRESULT STDMETHODCALLTYPE GetThreadpoolData(struct DacpThreadpoolData *data);
    virtual HRESULT STDMETHODCALLTYPE GetWorkRequestData(CLRDATA_ADDRESS addrWorkRequest, struct DacpWorkRequestData *data);
    virtual HRESULT STDMETHODCALLTYPE GetObjectData(CLRDATA_ADDRESS objAddr, struct DacpObjectData *data);
    virtual HRESULT STDMETHODCALLTYPE GetObjectStringData(CLRDATA_ADDRESS obj, unsigned int count, _Inout_updates_z_(count) WCHAR *stringData, unsigned int *pNeeded);
    virtual HRESULT STDMETHODCALLTYPE GetObjectClassName(CLRDATA_ADDRESS obj, unsigned int count, _Inout_updates_z_(count) WCHAR *className, unsigned int *pNeeded);
    virtual HRESULT STDMETHODCALLTYPE GetMethodTableName(CLRDATA_ADDRESS mt, unsigned int count, _Inout_updates_z_(count) WCHAR *mtName, unsigned int *pNeeded);
    virtual HRESULT STDMETHODCALLTYPE GetMethodTableData(CLRDATA_ADDRESS mt, struct DacpMethodTableData *data);
    virtual HRESULT STDMETHODCALLTYPE GetMethodTableFieldData(CLRDATA_ADDRESS mt, struct DacpMethodTableFieldData *data);
    virtual HRESULT STDMETHODCALLTYPE GetMethodTableTransparencyData(CLRDATA_ADDRESS mt, struct DacpMethodTableTransparencyData *data);
    virtual HRESULT STDMETHODCALLTYPE GetMethodTableForEEClass(CLRDATA_ADDRESS eeClass, CLRDATA_ADDRESS *value);
    virtual HRESULT STDMETHODCALLTYPE GetFieldDescData(CLRDATA_ADDRESS fieldDesc, struct DacpFieldDescData *data);
    virtual HRESULT STDMETHODCALLTYPE GetFrameName(CLRDATA_ADDRESS vtable, unsigned int count, _Inout_updates_z_(count) WCHAR *frameName, unsigned int *pNeeded);
    virtual HRESULT STDMETHODCALLTYPE GetModule(CLRDATA_ADDRESS addr, IXCLRDataModule **mod);
    virtual HRESULT STDMETHODCALLTYPE GetModuleData(CLRDATA_ADDRESS moduleAddr, struct DacpModuleData *data);
    virtual HRESULT STDMETHODCALLTYPE TraverseModuleMap(ModuleMapType mmt, CLRDATA_ADDRESS moduleAddr, MODULEMAPTRAVERSE pCallback, LPVOID token);
    virtual HRESULT STDMETHODCALLTYPE GetMethodDescFromToken(CLRDATA_ADDRESS moduleAddr, mdToken token, CLRDATA_ADDRESS *methodDesc);
    virtual HRESULT STDMETHODCALLTYPE GetPEFileBase(CLRDATA_ADDRESS addr, CLRDATA_ADDRESS *base);
    virtual HRESULT STDMETHODCALLTYPE GetPEFileName(CLRDATA_ADDRESS addr, unsigned int count, _Inout_updates_z_(count) WCHAR *fileName, unsigned int *pNeeded);
    virtual HRESULT STDMETHODCALLTYPE GetAssemblyModuleList(CLRDATA_ADDRESS assembly, unsigned int count, CLRDATA_ADDRESS modules[], unsigned int *pNeeded);
    virtual HRESULT STDMETHODCALLTYPE GetGCHeapData(struct DacpGcHeapData *data);
    virtual HRESULT STDMETHODCALLTYPE GetGCHeapList(unsigned int count, CLRDATA_ADDRESS heaps[], unsigned int *pNeeded);
    virtual HRESULT STDMETHODCALLTYPE GetGCHeapDetails(CLRDATA_ADDRESS heap, struct DacpGcHeapDetails *details);
    virtual HRESULT STDMETHODCALLTYPE GetGCHeapStaticData(struct DacpGcHeapDetails *data);
    virtual HRESULT STDMETHODCALLTYPE GetHeapSegmentData(CLRDATA_ADDRESS seg, struct DacpHeapSegmentData *data);
    virtual HRESULT STDMETHODCALLTYPE GetDomainLocalModuleData(CLRDATA_ADDRESS addr, struct DacpDomainLocalModuleData *data);
    virtual HRESULT STDMETHODCALLTYPE GetDomainLocalModuleDataFromAppDomain(CLRDATA_ADDRESS appDomainAddr, int moduleID, struct DacpDomainLocalModuleData *data);
    virtual HRESULT STDMETHODCALLTYPE GetDomainLocalModuleDataFromModule(CLRDATA_ADDRESS moduleAddr, struct DacpDomainLocalModuleData *data);
    virtual HRESULT STDMETHODCALLTYPE GetSyncBlockData(unsigned int number, struct DacpSyncBlockData *data);
    virtual HRESULT STDMETHODCALLTYPE GetSyncBlockCleanupData(CLRDATA_ADDRESS addr, struct DacpSyncBlockCleanupData *data);
    virtual HRESULT STDMETHODCALLTYPE TraverseRCWCleanupList(CLRDATA_ADDRESS cleanupListPtr, VISITRCWFORCLEANUP pCallback, LPVOID token);
    virtual HRESULT STDMETHODCALLTYPE TraverseEHInfo(CLRDATA_ADDRESS ip, DUMPEHINFO pCallback, LPVOID token);
    virtual HRESULT STDMETHODCALLTYPE GetStressLogAddress(CLRDATA_ADDRESS *stressLog);
    virtual HRESULT STDMETHODCALLTYPE GetJitManagerList(unsigned int count, struct DacpJitManagerInfo managers[], unsigned int *pNeeded);
    virtual HRESULT STDMETHODCALLTYPE GetJitHelperFunctionName(CLRDATA_ADDRESS ip, unsigned int count, _Inout_updates_z_(count) char *name, unsigned int *pNeeded);
    virtual HRESULT STDMETHODCALLTYPE GetJumpThunkTarget(T_CONTEXT *ctx, CLRDATA_ADDRESS *targetIP, CLRDATA_ADDRESS *targetMD);
    virtual HRESULT STDMETHODCALLTYPE TraverseLoaderHeap(CLRDATA_ADDRESS loaderHeapAddr, VISITHEAP pCallback);
    virtual HRESULT STDMETHODCALLTYPE GetCodeHeapList(CLRDATA_ADDRESS jitManager, unsigned int count, struct DacpJitCodeHeapInfo codeHeaps[], unsigned int *pNeeded);
    virtual HRESULT STDMETHODCALLTYPE GetMethodTableSlot(CLRDATA_ADDRESS mt, unsigned int slot, CLRDATA_ADDRESS *value);
    virtual HRESULT STDMETHODCALLTYPE TraverseVirtCallStubHeap(CLRDATA_ADDRESS pAppDomain, VCSHeapType heaptype, VISITHEAP pCallback);
    virtual HRESULT STDMETHODCALLTYPE GetNestedExceptionData(CLRDATA_ADDRESS exception, CLRDATA_ADDRESS *exceptionObject, CLRDATA_ADDRESS *nextNestedException);
    virtual HRESULT STDMETHODCALLTYPE GetUsefulGlobals(struct DacpUsefulGlobalsData *data);
    virtual HRESULT STDMETHODCALLTYPE GetILForModule(CLRDATA_ADDRESS moduleAddr, DWORD rva, CLRDATA_ADDRESS *il);
    virtual HRESULT STDMETHODCALLTYPE GetClrWatsonBuckets(CLRDATA_ADDRESS thread, void *pGenericModeBlock);
    virtual HRESULT STDMETHODCALLTYPE GetOOMData(CLRDATA_ADDRESS oomAddr, struct DacpOomData *data);
    virtual HRESULT STDMETHODCALLTYPE GetOOMStaticData(struct DacpOomData *data);
    virtual HRESULT STDMETHODCALLTYPE GetHeapAnalyzeData(CLRDATA_ADDRESS addr,struct  DacpGcHeapAnalyzeData *data);
    virtual HRESULT STDMETHODCALLTYPE GetHeapAnalyzeStaticData(struct DacpGcHeapAnalyzeData *data);
    virtual HRESULT STDMETHODCALLTYPE GetMethodDescTransparencyData(CLRDATA_ADDRESS methodDesc, struct DacpMethodDescTransparencyData *data);
    virtual HRESULT STDMETHODCALLTYPE GetHillClimbingLogEntry(CLRDATA_ADDRESS addr, struct DacpHillClimbingLogEntry *data);
    virtual HRESULT STDMETHODCALLTYPE GetThreadLocalModuleData(CLRDATA_ADDRESS thread, unsigned int index, struct DacpThreadLocalModuleData *data);
    virtual HRESULT STDMETHODCALLTYPE GetRCWData(CLRDATA_ADDRESS addr, struct DacpRCWData *data);
    virtual HRESULT STDMETHODCALLTYPE GetRCWInterfaces(CLRDATA_ADDRESS rcw, unsigned int count, struct DacpCOMInterfacePointerData interfaces[], unsigned int *pNeeded);
    virtual HRESULT STDMETHODCALLTYPE GetCCWData(CLRDATA_ADDRESS ccw, struct DacpCCWData *data);
    virtual HRESULT STDMETHODCALLTYPE GetCCWInterfaces(CLRDATA_ADDRESS ccw, unsigned int count, struct DacpCOMInterfacePointerData interfaces[], unsigned int *pNeeded);
    virtual HRESULT STDMETHODCALLTYPE GetTLSIndex(ULONG *pIndex);
    virtual HRESULT STDMETHODCALLTYPE GetDacModuleHandle(HMODULE *phModule);

    virtual HRESULT STDMETHODCALLTYPE GetFailedAssemblyList(CLRDATA_ADDRESS appDomain, int count, CLRDATA_ADDRESS values[], unsigned int *pNeeded);
    virtual HRESULT STDMETHODCALLTYPE GetPrivateBinPaths(CLRDATA_ADDRESS appDomain, int count, _Inout_updates_z_(count) WCHAR *paths, unsigned int *pNeeded);
    virtual HRESULT STDMETHODCALLTYPE GetAssemblyLocation(CLRDATA_ADDRESS assembly, int count, _Inout_updates_z_(count) WCHAR *location, unsigned int *pNeeded);
    virtual HRESULT STDMETHODCALLTYPE GetAppDomainConfigFile(CLRDATA_ADDRESS appDomain, int count, _Inout_updates_z_(count) WCHAR *configFile, unsigned int *pNeeded);
    virtual HRESULT STDMETHODCALLTYPE GetApplicationBase(CLRDATA_ADDRESS appDomain, int count, _Inout_updates_z_(count) WCHAR *base, unsigned int *pNeeded);

    virtual HRESULT STDMETHODCALLTYPE GetFailedAssemblyData(CLRDATA_ADDRESS assembly, unsigned int *pContext, HRESULT *pResult);
    virtual HRESULT STDMETHODCALLTYPE GetFailedAssemblyLocation(CLRDATA_ADDRESS assembly, unsigned int count, _Inout_updates_z_(count) WCHAR *location, unsigned int *pNeeded);
    virtual HRESULT STDMETHODCALLTYPE GetFailedAssemblyDisplayName(CLRDATA_ADDRESS assembly, unsigned int count, _Inout_updates_z_(count) WCHAR *name, unsigned int *pNeeded);

    virtual HRESULT STDMETHODCALLTYPE GetStackReferences(DWORD osThreadID, ISOSStackRefEnum **ppEnum);
    virtual HRESULT STDMETHODCALLTYPE GetRegisterName(int regNum, unsigned int count, _Inout_updates_z_(count) WCHAR *buffer, unsigned int *pNeeded);

    virtual HRESULT STDMETHODCALLTYPE GetHandleEnum(ISOSHandleEnum **ppHandleEnum);
    virtual HRESULT STDMETHODCALLTYPE GetHandleEnumForTypes(unsigned int types[], unsigned int count, ISOSHandleEnum **ppHandleEnum);
    virtual HRESULT STDMETHODCALLTYPE GetHandleEnumForGC(unsigned int gen, ISOSHandleEnum **ppHandleEnum);

    virtual HRESULT STDMETHODCALLTYPE GetThreadAllocData(CLRDATA_ADDRESS thread, struct DacpAllocData *data);
    virtual HRESULT STDMETHODCALLTYPE GetHeapAllocData(unsigned int count, struct DacpGenerationAllocData *data, unsigned int *pNeeded);

    // ISOSDacInterface2
    virtual HRESULT STDMETHODCALLTYPE GetObjectExceptionData(CLRDATA_ADDRESS objAddr, struct DacpExceptionObjectData *data);
    virtual HRESULT STDMETHODCALLTYPE IsRCWDCOMProxy(CLRDATA_ADDRESS rcwAddr, BOOL* isDCOMProxy);

    // ISOSDacInterface3
    virtual HRESULT STDMETHODCALLTYPE GetGCInterestingInfoData(CLRDATA_ADDRESS interestingInfoAddr, struct DacpGCInterestingInfoData *data);
    virtual HRESULT STDMETHODCALLTYPE GetGCInterestingInfoStaticData(struct DacpGCInterestingInfoData *data);
    virtual HRESULT STDMETHODCALLTYPE GetGCGlobalMechanisms(size_t* globalMechanisms);

    // ISOSDacInterface4
    virtual HRESULT STDMETHODCALLTYPE GetClrNotification(CLRDATA_ADDRESS arguments[], int count, int *pNeeded);

    // ISOSDacInterface5
    virtual HRESULT STDMETHODCALLTYPE GetTieredVersions(CLRDATA_ADDRESS methodDesc, int rejitId, struct DacpTieredVersionData *nativeCodeAddrs, int cNativeCodeAddrs, int *pcNativeCodeAddrs);

    // ISOSDacInterface6
    virtual HRESULT STDMETHODCALLTYPE GetMethodTableCollectibleData(CLRDATA_ADDRESS mt, struct DacpMethodTableCollectibleData *data);

    // ISOSDacInterface7
    virtual HRESULT STDMETHODCALLTYPE GetPendingReJITID(CLRDATA_ADDRESS methodDesc, int *pRejitId);
    virtual HRESULT STDMETHODCALLTYPE GetReJITInformation(CLRDATA_ADDRESS methodDesc, int rejitId, struct DacpReJitData2 *pReJitData);
    virtual HRESULT STDMETHODCALLTYPE GetProfilerModifiedILInformation(CLRDATA_ADDRESS methodDesc, struct DacpProfilerILData *pILData);
    virtual HRESULT STDMETHODCALLTYPE GetMethodsWithProfilerModifiedIL(CLRDATA_ADDRESS mod, CLRDATA_ADDRESS *methodDescs, int cMethodDescs, int *pcMethodDescs);

    // ISOSDacInterface8
    virtual HRESULT STDMETHODCALLTYPE GetNumberGenerations(unsigned int *pGenerations);
    virtual HRESULT STDMETHODCALLTYPE GetGenerationTable(unsigned int cGenerations, struct DacpGenerationData *pGenerationData, unsigned int *pNeeded);
    virtual HRESULT STDMETHODCALLTYPE GetFinalizationFillPointers(unsigned int cFillPointers, CLRDATA_ADDRESS *pFinalizationFillPointers, unsigned int *pNeeded);

    virtual HRESULT STDMETHODCALLTYPE GetGenerationTableSvr(CLRDATA_ADDRESS heapAddr, unsigned int cGenerations, struct DacpGenerationData *pGenerationData, unsigned int *pNeeded);
    virtual HRESULT STDMETHODCALLTYPE GetFinalizationFillPointersSvr(CLRDATA_ADDRESS heapAddr, unsigned int cFillPointers, CLRDATA_ADDRESS *pFinalizationFillPointers, unsigned int *pNeeded);

    virtual HRESULT STDMETHODCALLTYPE GetAssemblyLoadContext(CLRDATA_ADDRESS methodTable, CLRDATA_ADDRESS* assemblyLoadContext);

    // ISOSDacInterface9
    virtual HRESULT STDMETHODCALLTYPE GetBreakingChangeVersion(int* pVersion);

    // ISOSDacInterface10
    virtual HRESULT STDMETHODCALLTYPE GetObjectComWrappersData(CLRDATA_ADDRESS objAddr, CLRDATA_ADDRESS *rcw, unsigned int count, CLRDATA_ADDRESS *mowList, unsigned int *pNeeded);
    virtual HRESULT STDMETHODCALLTYPE IsComWrappersCCW(CLRDATA_ADDRESS ccw, BOOL *isComWrappersCCW);
    virtual HRESULT STDMETHODCALLTYPE GetComWrappersCCWData(CLRDATA_ADDRESS ccw, CLRDATA_ADDRESS *managedObject, int *refCount);
    virtual HRESULT STDMETHODCALLTYPE IsComWrappersRCW(CLRDATA_ADDRESS rcw, BOOL *isComWrappersRCW);
    virtual HRESULT STDMETHODCALLTYPE GetComWrappersRCWData(CLRDATA_ADDRESS rcw, CLRDATA_ADDRESS *identity);

    // ISOSDacInterface11
    virtual HRESULT STDMETHODCALLTYPE IsTrackedType(
        CLRDATA_ADDRESS objAddr,
        BOOL *isTrackedType,
        BOOL *hasTaggedMemory);
    virtual HRESULT STDMETHODCALLTYPE GetTaggedMemory(
        CLRDATA_ADDRESS objAddr,
        CLRDATA_ADDRESS *taggedMemory,
        size_t *taggedMemorySizeInBytes);

    // ISOSDacInterface12
    virtual HRESULT STDMETHODCALLTYPE GetGlobalAllocationContext( 
        CLRDATA_ADDRESS *allocPtr,
        CLRDATA_ADDRESS *allocLimit);

    //
    // ClrDataAccess.
    //

    HRESULT Initialize(void);

    BOOL IsExceptionFromManagedCode(EXCEPTION_RECORD * pExceptionRecord);
#ifndef TARGET_UNIX
    HRESULT GetWatsonBuckets(DWORD dwThreadId, GenericModeBlock * pGM);
#endif // TARGET_UNIX


    Thread* FindClrThreadByTaskId(ULONG64 taskId);
    HRESULT IsPossibleCodeAddress(IN TADDR address);

    PCSTR GetJitHelperName(IN TADDR address,
                           IN bool dynamicHelpersOnly = false);
    HRESULT GetFullMethodName(IN MethodDesc* methodDesc,
                              IN ULONG32 symbolChars,
                              IN ULONG32* symbolLen,
                              _Out_writes_to_opt_(symbolChars, *symbolLen) LPWSTR symbol);
    HRESULT RawGetMethodName(/* [in] */ CLRDATA_ADDRESS address,
                             /* [in] */ ULONG32 flags,
                             /* [in] */ ULONG32 bufLen,
                             /* [out] */ ULONG32 *nameLen,
                             /* [size_is][out] */ _Out_writes_bytes_opt_(bufLen) WCHAR nameBuf[  ],
                             /* [out] */ CLRDATA_ADDRESS* displacement);

    HRESULT FollowStubStep(
        /* [in] */ Thread* thread,
        /* [in] */ ULONG32 inFlags,
        /* [in] */ TADDR inAddr,
        /* [in] */ union STUB_BUF* inBuffer,
        /* [out] */ TADDR* outAddr,
        /* [out] */ union STUB_BUF* outBuffer,
        /* [out] */ ULONG32* outFlags);

    DebuggerJitInfo* GetDebuggerJitInfo(MethodDesc* methodDesc,
                                        TADDR addr)
    {
        if (g_pDebugger)
        {
            return g_pDebugger->GetJitInfo(methodDesc, (PBYTE)addr, NULL);
        }

        return NULL;
    }

    HRESULT GetMethodExtents(MethodDesc* methodDesc,
                             METH_EXTENTS** extents);
    HRESULT GetMethodVarInfo(MethodDesc* methodDesc,
                             TADDR address,
                             ULONG32* numVarInfo,
                             ICorDebugInfo::NativeVarInfo** varInfo,
                             ULONG32* codeOffset);

    // If the method has multiple copies of code (because of EnC or code-pitching),
    // this returns the info corresponding to address.
    // If 'address' and 'codeOffset' are both non-NULL, *codeOffset gets set to
    // the offset of 'address' from the start of the method.
    HRESULT GetMethodNativeMap(MethodDesc* methodDesc,
                               TADDR address,
                               ULONG32* numMap,
                               DebuggerILToNativeMap** map,
                               bool* mapAllocated,
                               CLRDATA_ADDRESS* codeStart,
                               ULONG32* codeOffset);

    // Get the MethodDesc for a function
    MethodDesc * FindLoadedMethodRefOrDef(Module* pModule, mdToken memberRef);

#ifndef TARGET_UNIX
    HRESULT GetClrWatsonBucketsWorker(Thread * pThread, GenericModeBlock * pGM);
#endif // TARGET_UNIX

    HRESULT ServerGCHeapDetails(CLRDATA_ADDRESS heapAddr,
                                DacpGcHeapDetails *detailsData);
    HRESULT GetServerAllocData(unsigned int count, struct DacpGenerationAllocData *data, unsigned int *pNeeded);
    HRESULT ServerOomData(CLRDATA_ADDRESS addr, DacpOomData *oomData);
    HRESULT ServerGCInterestingInfoData(CLRDATA_ADDRESS addr, DacpGCInterestingInfoData *interestingInfoData);
    HRESULT ServerGCHeapAnalyzeData(CLRDATA_ADDRESS heapAddr,
                                DacpGcHeapAnalyzeData *analyzeData);

    //
    // Memory enumeration.
    //

    HRESULT EnumMemoryRegionsWrapper(CLRDataEnumMemoryFlags flags);

    // skinny minidump functions
    HRESULT EnumMemoryRegionsWorkerSkinny(CLRDataEnumMemoryFlags flags);
    // triage minidump functions
    HRESULT EnumMemoryRegionsWorkerMicroTriage(CLRDataEnumMemoryFlags flags);
    HRESULT EnumMemoryRegionsWorkerHeap(CLRDataEnumMemoryFlags flags);

    HRESULT EnumMemWalkStackHelper(CLRDataEnumMemoryFlags flags, IXCLRDataStackWalk  *pStackWalk, Thread * pThread);
    HRESULT DumpManagedObject(CLRDataEnumMemoryFlags flags, OBJECTREF objRef);
    HRESULT DumpManagedExcepObject(CLRDataEnumMemoryFlags flags, OBJECTREF objRef);
    HRESULT DumpManagedStackTraceStringObject(CLRDataEnumMemoryFlags flags, STRINGREF orefStackTrace);
#if (defined(FEATURE_COMINTEROP) || defined(FEATURE_COMWRAPPERS)) && !defined(TARGET_UNIX)
    HRESULT DumpStowedExceptionObject(CLRDataEnumMemoryFlags flags, CLRDATA_ADDRESS ccwPtr);
    HRESULT EnumMemStowedException(CLRDataEnumMemoryFlags flags);
#endif

    HRESULT EnumMemWriteDataSegment();

    // Custom Dump
    HRESULT EnumMemoryRegionsWorkerCustom();

    // helper function for dump code
    void EnumWksGlobalMemoryRegions(CLRDataEnumMemoryFlags flags);
    void EnumSvrGlobalMemoryRegions(CLRDataEnumMemoryFlags flags);

    HRESULT EnumMemCollectImages();
    HRESULT EnumMemCLRStatic(CLRDataEnumMemoryFlags flags);
    HRESULT EnumMemCLRHeapCrticalStatic(CLRDataEnumMemoryFlags flags);
    HRESULT EnumMemDumpModuleList(CLRDataEnumMemoryFlags flags);
    HRESULT EnumMemDumpAppDomainInfo(CLRDataEnumMemoryFlags flags);
    HRESULT EnumMemDumpAllThreadsStack(CLRDataEnumMemoryFlags flags);
    HRESULT EnumMemCLRMainModuleInfo();

    bool ReportMem(TADDR addr, TSIZE_T size, bool fExpectSuccess = true);
    bool DacUpdateMemoryRegion(TADDR addr, TSIZE_T bufferSize, BYTE* buffer);

    void ClearDumpStats();
    JITNotification* GetHostJitNotificationTable();
    GcNotification*  GetHostGcNotificationTable();

    void* GetMetaDataFromHost(PEAssembly* pPEAssembly,
                              bool* isAlternate);

    virtual
    interface IMDInternalImport* GetMDImport(const PEAssembly* pPEAssembly,
                                             const ReflectionModule* reflectionModule,
                                             bool throwEx);

    interface IMDInternalImport* GetMDImport(const PEAssembly* pPEAssembly,
                                             bool throwEx)
    {
        return GetMDImport(pPEAssembly, NULL, throwEx);
    }

    interface IMDInternalImport* GetMDImport(const ReflectionModule* reflectionModule,
                                             bool throwEx)
    {
        return GetMDImport(NULL, reflectionModule, throwEx);
    }

    //ClrDump support
    HRESULT STDMETHODCALLTYPE DumpNativeImage(CLRDATA_ADDRESS loadedBase,
                                              LPCWSTR name,
                                              IXCLRDataDisplay *display,
                                              IXCLRLibrarySupport *support,
                                              IXCLRDisassemblySupport *dis);

    // Set whether inconsistencies in the target should raise asserts.
    void SetTargetConsistencyChecks(bool fEnableAsserts);

    // Get whether inconsistencies in the target should raise asserts.
    bool TargetConsistencyAssertsEnabled();

    // Get the ICLRDataTarget2 instance, if any
    ICLRDataTarget2 * GetLegacyTarget2()        { return m_pLegacyTarget2; }

    // Get the ICLRDataTarget3 instance, if any
    ICLRDataTarget3 * GetLegacyTarget3()        { return m_pLegacyTarget3; }

    //
    // Public Fields
    // Note that it would be nice if all of these were made private.  However, the visibility
    // model of the DAC implementation is that the public surface area is declared in daccess.h
    // (implemented in dacfn.cpp), and the private surface area (like ClrDataAccess) is declared
    // in dacimpl.h which is only included by the DAC infrastructure.  Therefore the DAC
    // infrastructure agressively uses these fields, and we don't get a huge amount of benefit from
    // reworking this model (since there is some form of encapsulation in place already).
    //

    // The underlying data target - always present (strong reference)
    ICorDebugDataTarget * m_pTarget;

    // Mutable version of the data target if any - optional (strong reference)
    ICorDebugMutableDataTarget * m_pMutableTarget;

    TADDR m_globalBase;
    DacGlobals m_dacGlobals;
    DacInstanceManager m_instances;
    ULONG32 m_instanceAge;
    bool m_debugMode;

#ifdef FEATURE_MINIMETADATA_IN_TRIAGEDUMPS

protected:
    DacStreamManager * m_streams;

public:
    // Used to mark the point after which enumerated EE structs of interest
    // will get their names cached in the triage/mini-dump
    void InitStreamsForWriting(IN CLRDataEnumMemoryFlags flags);

    // Used during triage/mini-dump collection to populate the map of
    // pointers to EE struct (MethodDesc* for now) to their corresponding
    // name.
    bool MdCacheAddEEName(TADDR taEEStruct, const SString& name);

    // Used to mark the end point for the name caching. Will update streams
    // based on built caches
    void EnumStreams(IN CLRDataEnumMemoryFlags flags);

    // Used during triage/mini-dump analysis to retrieve the name associated
    // with an EE struct pointer (MethodDesc* for now).
    bool MdCacheGetEEName(TADDR taEEStruct, SString & eeName);

#endif // FEATURE_MINIMETADATA_IN_TRIAGEDUMPS

private:
    // Read the DAC table and initialize m_dacGlobals
    HRESULT GetDacGlobalValues();

    // Verify the target mscorwks.dll matches the version expected
    HRESULT VerifyDlls();

    // Check whether a region of memory is fully readable.
    bool IsFullyReadable(TADDR addr, TSIZE_T size);

    // Legacy target interfaces - optional
    ICLRDataTarget * m_pLegacyTarget;
    ICLRDataTarget2 * m_pLegacyTarget2;
    ICLRDataTarget3 * m_pLegacyTarget3;
    IXCLRDataTarget3 * m_target3;
    ICLRMetadataLocator * m_legacyMetaDataLocator;

    LONG m_refs;
    MDImportsCache m_mdImports;
    ICLRDataEnumMemoryRegionsCallback* m_enumMemCb;
    ICLRDataEnumMemoryRegionsCallback2* m_updateMemCb;
    CLRDataEnumMemoryFlags m_enumMemFlags;
    JITNotification* m_jitNotificationTable;
    GcNotification*  m_gcNotificationTable;
    TSIZE_T m_cbMemoryReported;
    DumpMemoryReportStatics m_dumpStats;

    // If true, inconsistencies in the target will cause ASSERTs to be raised in DEBUG builds
    bool m_fEnableTargetConsistencyAsserts;

#ifdef _DEBUG
protected:
    // If true, a mscorwks/mscordacwks mismatch will trigger a nice assert dialog
    bool m_fEnableDllVerificationAsserts;
private:
#endif

protected:
    // Populates a DacpJitCodeHeapInfo with proper information about the
    // code heap type and the information needed to locate it.
    DacpJitCodeHeapInfo DACGetHeapInfoForCodeHeap(CodeHeap *heapAddr);

#ifdef FEATURE_COMINTEROP
    // Returns CCW pointer based on a target address.
    PTR_ComCallWrapper DACGetCCWFromAddress(CLRDATA_ADDRESS addr);

private:
    // Returns COM interface pointer corresponding to a given CCW and internal vtable
    // index. Returns NULL if the vtable is unused or not fully laid out.
    PTR_IUnknown DACGetCOMIPFromCCW(PTR_ComCallWrapper pCCW, int vtableIndex);
#endif

#ifdef FEATURE_COMWRAPPERS
    BOOL DACGetComWrappersCCWVTableQIAddress(CLRDATA_ADDRESS ccwPtr, TADDR *vTableAddress, TADDR *qiAddress);
    BOOL DACIsComWrappersCCW(CLRDATA_ADDRESS ccwPtr);
    TADDR DACGetManagedObjectWrapperFromCCW(CLRDATA_ADDRESS ccwPtr);
    HRESULT DACTryGetComWrappersObjectFromCCW(CLRDATA_ADDRESS ccwPtr, OBJECTREF* objRef);
#endif

protected:
#ifdef FEATURE_COMWRAPPERS
    HRESULT DACTryGetComWrappersHandleFromCCW(CLRDATA_ADDRESS ccwPtr, OBJECTHANDLE* objHandle);
#endif

public:
    // APIs for picking up the info needed for a debugger to look up an ngen image or IL image
    // from it's search path.
    static bool GetMetaDataFileInfoFromPEFile(PEAssembly *pPEAssembly,
                                              DWORD &dwImageTimestamp,
                                              DWORD &dwImageSize,
                                              DWORD &dwDataSize,
                                              DWORD &dwRvaHint,
                                              bool  &isNGEN,
                                              _Out_writes_(cchFilePath) LPWSTR wszFilePath,
                                              DWORD cchFilePath);

    static bool GetILImageInfoFromNgenPEFile(PEAssembly *pPEAssembly,
                                             DWORD &dwTimeStamp,
                                             DWORD &dwSize,
                                             _Out_writes_(cchPath) LPWSTR wszPath,
                                             const DWORD cchPath);
};

extern ClrDataAccess* g_dacImpl;

/*     DacHandleWalker.
 *
 * Iterates over the handle table, enumerating all handles of the requested type on the
 * handle table.  This also will report the handle type, whether the handle is a strong
 * reference, the AppDomain the handle comes from, as well as the reference count (in
 * the case of a RefCount handle).  Optionally this class can also be used to filter
 * based on GC generation that would be collected (that is, to emulate a GC scan of the
 * handle table).
 *
 *     General implementation details:
 * We have four sets of variables:
 * 1.  Overhead variables needed to operate in the Dac.
 * 2.  Variables needed to walk the handle table.  We walk the handle table one bucket
 *     at a time, filling the array the user gave us until we have either enumerated
 *     all handles, or filled the array.
 * 3.  Storage variables to hold the overflow.  That is, we were walking the handle
 *     table, filled the array that the user gave us, then needed to store the extra
 *     handles the handle table continued to enumerate to us.  This is implmeneted
 *     as a linked list of arrays (mHead, mHead.Next, etc).
 * 4.  Variables which store the location of where we are in the overflow data.
 *
 * Note that "mHead" is a HandleChunkHead where we stuff the user's array.  Everything
 * which follows mHead (mHead.Next, etc) is a HandleChunk containing overflow data.
 *
 * Lastly, note this does not do robust error handling.  If we fail to allocate a
 * HandleChunk while walking the handle table, we will miss handles and not report
 * this to the user.  Unfortunately this will have to be fixed in the next iteration
 * when we add more robust error handling to SOS's interface.
 */
 template <class T, REFIID IID_T>
class DefaultCOMImpl : public T
{
public:
    DefaultCOMImpl()
        : mRef(0)
    {
    }

    virtual ~DefaultCOMImpl() {}

    ULONG STDMETHODCALLTYPE AddRef()
    {
        return ++mRef;
    }

    ULONG STDMETHODCALLTYPE Release()
    {
        ULONG res = mRef--;
        if (res == 0)
            delete this;
        return res;
    }

    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void **ppObj)
    {
        if (ppObj == NULL)
            return E_INVALIDARG;

        if (IsEqualIID(riid, IID_IUnknown))
        {
            AddRef();
            *ppObj = static_cast<IUnknown*>(this);
            return S_OK;
        }
        else if (IsEqualIID(riid, IID_T))
        {
            AddRef();
            *ppObj = static_cast<T*>(this);
            return S_OK;
        }

        *ppObj = NULL;
        return E_NOINTERFACE;
    }

private:
    ULONG mRef;
};


// A stuct representing a thread's allocation context.
struct AllocInfo
{
    CORDB_ADDRESS Ptr;
    CORDB_ADDRESS Limit;

    AllocInfo()
        : Ptr(0), Limit(0)
    {
    }
};

// A struct representing a segment in the heap.
struct SegmentData
{
    CORDB_ADDRESS Start;
    CORDB_ADDRESS End;

    // Whether this segment is part of the large object heap.
    int Generation;

    SegmentData()
        : Start(0), End(0), Generation(0)
    {
    }
};

// A struct representing a gc heap in the process.
struct HeapData
{
    CORDB_ADDRESS YoungestGenPtr;
    CORDB_ADDRESS YoungestGenLimit;

    CORDB_ADDRESS Gen0Start;
    CORDB_ADDRESS Gen0End;

    CORDB_ADDRESS Gen1Start;
    size_t EphemeralSegment;

    size_t SegmentCount;
    SegmentData *Segments;

    HeapData();
    ~HeapData();
};

/* This cache is used to read data from the target process if the reads are known
 * to be sequential.  This will object will read one page of memory out of the
 * process at a time, aligned to the page boundary, to
 */
class LinearReadCache
{
public:
    LinearReadCache();
    ~LinearReadCache();

    /* Reads an address out of the target process, caching the page of memory read.
     * Params:
     *   addr - The address to read out of the target process.
     *   t - A pointer to the data to stuff it in.  We will read sizeof(T) data
     *       from the process and write it into the location t points to.  This
     *       parameter must be non-null.
     * Returns:
     *   True if the read succeeded.  False if it did not, usually as a result
     *   of the memory simply not being present in the target process.
     * Note:
     *   The state of *t is undefined if this function returns false.  We may
     *   have written partial data to it if we return false, so you must
     *   absolutely NOT use it if Read returns false.
     */
    template <class T>
    bool Read(CORDB_ADDRESS addr, T *t)
    {
        _ASSERTE(t);

        // Unfortunately the ctor can fail the alloc for the byte array.  In this case
        // we'll just fall back to non-cached reads.
        if (mPage == NULL)
            return MisalignedRead(addr, t);

        // Is addr on the current page?  If not read the page of memory addr is on.
        // If this fails, we will fall back to a raw read out of the process (which
        // is what MisalignedRead does).
        if ((addr < mCurrPageStart) || (addr - mCurrPageStart > mCurrPageSize))
            if (!MoveToPage(addr))
                return MisalignedRead(addr, t);

        // If MoveToPage succeeds, we MUST be on the right page.
        _ASSERTE(addr >= mCurrPageStart);

        // However, the amount of data requested may fall off of the page.  In that case,
        // fall back to MisalignedRead.
        CORDB_ADDRESS offset = addr - mCurrPageStart;
        if (offset + sizeof(T) > mCurrPageSize)
            return MisalignedRead(addr, t);

        // If we reach here we know we are on the right page of memory in the cache, and
        // that the read won't fall off of the end of the page.
        *t = *reinterpret_cast<T*>(mPage+offset);
        return true;
    }

    // helper used to read the MethodTable
    bool ReadMT(CORDB_ADDRESS addr, TADDR *mt)
    {
        if (!Read(addr, mt))
            return false;

        // clear the GC flag bits off the MethodTable
        // equivalent to Object::GetGCSafeMethodTable()
#if TARGET_64BIT
        *mt &= ~7;
#else
        *mt &= ~3;
#endif
        return true;
    }

private:
    /* Sets the cache to the page specified by addr, or false if we could not move to
     * that page.
     */
    bool MoveToPage(CORDB_ADDRESS addr);

    /* Attempts to read from the target process if the data is possibly hanging off
     * the end of a page.
     */
    template<class T>
    inline bool MisalignedRead(CORDB_ADDRESS addr, T *t)
    {
        return SUCCEEDED(DacReadAll(TO_TADDR(addr), t, sizeof(T), false));
    }

private:
    CORDB_ADDRESS mCurrPageStart;
    ULONG32 mPageSize, mCurrPageSize;
    BYTE *mPage;
};

DWORD DacGetNumHeaps();

/* The implementation of the dac heap walker.  This class will enumerate all objects on
 * the heap with three important caveats:
 * - This class will skip all Free objects in the heap.  Free objects are an
 *   implementation detail of the GC, and ICorDebug does not have a mechanism
 *   to expose them.
 * - This class does NOT guarantee that all objects will be enumerated.  In
 *   the event that we find heap corruption on a segment, or if the background
 *   GC is modifying a segment, the remainder of that segment will be skipped
 *   by design.
 * - The GC heap must be in a walkable state before you attempt to use this
 *   class on it.  The IDacDbiInterface::AreGCStructuresValid function will
 *   tell you whether it is safe to walk the heap or not.
 */
class DacHeapWalker
{
    static CORDB_ADDRESS HeapStart;
    static CORDB_ADDRESS HeapEnd;

public:
    DacHeapWalker();
    ~DacHeapWalker();

    /* Initializes the heap walker.  This must be called before Next or
     * HasMoreObjects.  Returns false if the initialization was not successful.
     * (In practice this should only return false if we hit an OOM trying
     * to allocate space for data structures.)  Limits the heap walk to be in the range
     * [start, end] (inclusive).  Use DacHeapWalker::HeapStart, DacHeapWalker::HeapEnd
     * as start or end to start from the beginning or end.
     */
    HRESULT Init(CORDB_ADDRESS start=HeapStart, CORDB_ADDRESS end=HeapEnd);

    /* Returns a CORDB_ADDRESS which points to the next value on the heap.
     * You must call HasMoreObjects on this class, and it must return true
     * before calling Next.
     */
    HRESULT Next(CORDB_ADDRESS *pValue, CORDB_ADDRESS *pMT, ULONG64 *size);

    /* Returns true if there are more objects on the heap, false otherwise.
     */
    inline bool HasMoreObjects() const
    {
        return mCurrHeap < mHeapCount;
    }

    HRESULT Reset(CORDB_ADDRESS start, CORDB_ADDRESS end);

    static HRESULT InitHeapDataWks(HeapData *&pHeaps, size_t &count);
    static HRESULT InitHeapDataSvr(HeapData *&pHeaps, size_t &count);

    HRESULT GetHeapData(HeapData **ppHeapData, size_t *pNumHeaps);

    SegmentData *FindSegment(CORDB_ADDRESS obj);

    HRESULT ListNearObjects(CORDB_ADDRESS obj, CORDB_ADDRESS *pPrev, CORDB_ADDRESS *pContaining, CORDB_ADDRESS *pNext);

private:
    HRESULT MoveToNextObject();

    bool GetSize(TADDR tMT, size_t &size);

    inline static size_t Align(size_t size)
    {
        if (sizeof(TADDR) == 4)
            return (size+3) & ~3;
        else
            return (size+7) & ~7;
    }

    inline static size_t AlignLarge(size_t size)
    {
        return (size + 7) & ~7;
    }

    template <class T>
    static int GetSegmentCount(T seg_start)
    {
        int count = 0;
        while (seg_start)
        {
            seg_start = seg_start->next;
            count++;
        }

        return count;
    }

    HRESULT NextSegment();
    void CheckAllocAndSegmentRange();

private:
    int mThreadCount;
    AllocInfo *mAllocInfo;

    size_t mHeapCount;
    HeapData *mHeaps;

    CORDB_ADDRESS mCurrObj;
    size_t mCurrSize;
    TADDR mCurrMT;

    size_t mCurrHeap;
    size_t mCurrSeg;

    CORDB_ADDRESS mStart;
    CORDB_ADDRESS mEnd;

    LinearReadCache mCache;
    static CORDB_ADDRESS sFreeMT;
};

struct DacGcReference;
struct SOSStackErrorList
{
    SOSStackRefError error;
    SOSStackErrorList *pNext;

    SOSStackErrorList()
        : pNext(0)
    {
    }
};

class DacStackReferenceWalker;
class DacStackReferenceErrorEnum : public DefaultCOMImpl<ISOSStackRefErrorEnum, IID_ISOSStackRefErrorEnum>
{
public:
    DacStackReferenceErrorEnum(DacStackReferenceWalker *pEnum, SOSStackErrorList *pErrors);
    ~DacStackReferenceErrorEnum();

    HRESULT STDMETHODCALLTYPE Skip(unsigned int count);
    HRESULT STDMETHODCALLTYPE Reset();
    HRESULT STDMETHODCALLTYPE GetCount(unsigned int *pCount);
    HRESULT STDMETHODCALLTYPE Next(unsigned int count, SOSStackRefError ref[], unsigned int *pFetched);

private:
    // The lifetime of the error list is tied to the enum, so we must addref/release it.
    DacStackReferenceWalker *mEnum;
    SOSStackErrorList *mHead;
    SOSStackErrorList *mCurr;
};

// For GCCONTEXT
#include "gcenv.h"

 /* DacStackReferenceWalker.
 */
class DacStackReferenceWalker : public DefaultCOMImpl<ISOSStackRefEnum, IID_ISOSStackRefEnum>
{
    struct DacScanContext : public ScanContext
    {
        DacStackReferenceWalker *pWalker;
        Frame *pFrame;
        TADDR sp, pc;
        bool stop;
        GCEnumCallback pEnumFunc;

        DacScanContext()
            : pWalker(NULL), pFrame(0), sp(0), pc(0), stop(false), pEnumFunc(0)
        {
        }
    };

    typedef struct _StackRefChunkHead
    {
        struct _StackRefChunkHead *next;  // Next chunk
        unsigned int count;               // The count of how many StackRefs were written to pData
        unsigned int size;                // The capacity of pData (in bytes)
        void *pData;                      // The overflow data

        _StackRefChunkHead()
            : next(0), count(0), size(0), pData(0)
        {
        }
    } StackRefChunkHead;

    // The actual struct used for storing overflow StackRefs
    typedef struct _StackRefChunk : public StackRefChunkHead
    {
        SOSStackRefData data[64];

        _StackRefChunk()
        {
            pData = data;
            size = sizeof(data);
        }
    } StackRefChunk;
public:
    DacStackReferenceWalker(ClrDataAccess *dac, DWORD osThreadID);
    virtual ~DacStackReferenceWalker();

    HRESULT Init();

    HRESULT STDMETHODCALLTYPE Skip(unsigned int count);
    HRESULT STDMETHODCALLTYPE Reset();
    HRESULT STDMETHODCALLTYPE GetCount(unsigned int *pCount);
    HRESULT STDMETHODCALLTYPE Next(unsigned int count,
                                   SOSStackRefData refs[],
                                   unsigned int *pFetched);

   // Dac-Dbi Functions
   HRESULT Next(ULONG celt, DacGcReference roots[], ULONG *pceltFetched);
   Thread *GetThread() const
   {
        return mThread;
   }

    HRESULT STDMETHODCALLTYPE EnumerateErrors(ISOSStackRefErrorEnum **ppEnum);

private:
    static StackWalkAction Callback(CrawlFrame *pCF, VOID *pData);
    static void GCEnumCallbackSOS(LPVOID hCallback, OBJECTREF *pObject, uint32_t flags, DacSlotLocation loc);
    static void GCReportCallbackSOS(PTR_PTR_Object ppObj, ScanContext *sc, uint32_t flags);
    static void GCEnumCallbackDac(LPVOID hCallback, OBJECTREF *pObject, uint32_t flags, DacSlotLocation loc);
    static void GCReportCallbackDac(PTR_PTR_Object ppObj, ScanContext *sc, uint32_t flags);

    CLRDATA_ADDRESS ReadPointer(TADDR addr);

    template <class StructType>
    StructType *GetNextObject(DacScanContext *ctx)
    {
        SUPPORTS_DAC;

        // If we failed on a previous call (OOM) don't keep trying to allocate, it's not going to work.
        if (ctx->stop || !mCurr)
            return NULL;

        // We've moved past the size of the current chunk.  We'll allocate a new chunk
        // and stuff the references there.  These are cleaned up by the destructor.
        if (mCurr->count >= mCurr->size/sizeof(StructType))
        {
            if (mCurr->next == NULL)
            {
                StackRefChunk *next = new (nothrow) StackRefChunk;
                if (next != NULL)
                {
                    mCurr->next = next;
                }
                else
                {
                    ctx->stop = true;
                    return NULL;
                }
            }

            mCurr = mCurr->next;
        }

        // Fill the current ref.
        StructType *pData = (StructType*)mCurr->pData;
        return &pData[mCurr->count++];
    }

    template <class IntType, class StructType>
    IntType WalkStack(IntType count, StructType refs[], promote_func promote, GCEnumCallback enumFunc)
    {
        _ASSERTE(mThread);
        _ASSERTE(!mEnumerated);

        // If this is the first time we were called, fill local data structures.
        // This will fill out the user's handles as well.
        _ASSERTE(mCurr == NULL);
        _ASSERTE(mHead.next == NULL);

        class ProfilerFilterContextHolder
        {
            Thread* m_pThread;

        public:
            ProfilerFilterContextHolder() : m_pThread(NULL)
            {
            }

            void Activate(Thread* pThread)
            {
                m_pThread = pThread;
            }

            ~ProfilerFilterContextHolder()
            {
                if (m_pThread != NULL)
                    m_pThread->SetProfilerFilterContext(NULL);
            }
        };

        ProfilerFilterContextHolder contextHolder;
        T_CONTEXT ctx;

        // Get the current thread's context and set that as the filter context
        if (mThread->GetFilterContext() == NULL && mThread->GetProfilerFilterContext() == NULL)
        {
            mDac->m_pTarget->GetThreadContext(mThread->GetOSThreadId(), CONTEXT_FULL, sizeof(ctx), (BYTE*)&ctx);
            mThread->SetProfilerFilterContext(&ctx);
            contextHolder.Activate(mThread);
        }

        // Setup GCCONTEXT structs for the stackwalk.
        GCCONTEXT gcctx;
        DacScanContext dsc;
        dsc.pWalker = this;
        dsc.pEnumFunc = enumFunc;
        gcctx.f = promote;
        gcctx.sc = &dsc;

        // Put the user's array/count in the
        mHead.size = count*sizeof(StructType);
        mHead.pData = refs;
        mHead.count = 0;

        mCurr = &mHead;

        // Walk the stack, set mEnumerated to true to ensure we don't do it again.
        unsigned int flagsStackWalk = ALLOW_INVALID_OBJECTS|ALLOW_ASYNC_STACK_WALK|SKIP_GSCOOKIE_CHECK;
#if defined(FEATURE_EH_FUNCLETS)
        flagsStackWalk |= GC_FUNCLET_REFERENCE_REPORTING;
#endif // defined(FEATURE_EH_FUNCLETS)

        mEnumerated = true;
        mThread->StackWalkFrames(DacStackReferenceWalker::Callback, &gcctx, flagsStackWalk);

        // We have filled the user's array as much as we could.  If there's more data than
        // could fit, mHead.Next will contain a linked list of refs to enumerate.
        mCurr = mHead.next;

        // Return how many we put in the user's array.
        return mHead.count;
    }

    template <class IntType, class StructType, promote_func PromoteFunc, GCEnumCallback EnumFunc>
    HRESULT DoStackWalk(IntType count, StructType stackRefs[], IntType *pFetched)
    {
        HRESULT hr = S_OK;
        IntType fetched = 0;
        if (!mEnumerated)
        {
            // If this is the first time we were called, fill local data structures.
            // This will fill out the user's handles as well.
            fetched = (IntType)WalkStack((unsigned int)count, stackRefs, PromoteFunc, EnumFunc);
        }

        while (fetched < count)
        {
            if (mCurr == NULL)
            {
                // Case 1: We have no more refs to walk.
                hr = S_FALSE;
                break;
            }
            else if (mChunkIndex >= mCurr->count)
            {
                // Case 2: We have exhausted the current chunk.
                mCurr = mCurr->next;
                mChunkIndex = 0;
            }
            else
            {
                // Case 3:  The last call to "Next" filled the user's array and had some ref
                // data leftover.  Walk the linked-list of arrays copying them into the user's
                // buffer until we have either exhausted the user's array or the leftover data.
                IntType toCopy = count - fetched;  // Fill the user's buffer...

                // ...unless that would go past the bounds of the current chunk.
                if (toCopy + mChunkIndex > mCurr->count)
                    toCopy = mCurr->count - mChunkIndex;

                memcpy(stackRefs+fetched, (StructType*)mCurr->pData+mChunkIndex, toCopy*sizeof(StructType));
                mChunkIndex += toCopy;
                fetched += toCopy;
            }
        }

        *pFetched = fetched;

        return hr;
    }

private:
    // Dac variables required for entering/leaving the dac.
    ClrDataAccess *mDac;
    ULONG32 m_instanceAge;

    // Operational variables
    Thread *mThread;
    SOSStackErrorList *mErrors;
    bool mEnumerated;

    // Storage variables
    StackRefChunkHead mHead;
    unsigned int mChunkIndex;

    // Iterator variables
    StackRefChunkHead *mCurr;
    int mIteratorIndex;

    // Heap.  Used to resolve interior pointers.
    DacHeapWalker mHeap;
};



struct DacGcReference;
class DacHandleWalker : public DefaultCOMImpl<ISOSHandleEnum, IID_ISOSHandleEnum>
{
    typedef struct _HandleChunkHead
    {
        struct _HandleChunkHead *Next;  // Next chunk
        unsigned int Count;             // The count of how many handles were written to pData
        unsigned int Size;              // The capacity of pData
        void *pData;           // The overflow data

        _HandleChunkHead()
            : Next(0), Count(0), Size(0), pData(0)
        {
        }
    } HandleChunkHead;

    // The actual struct used for storing overflow handles
    typedef struct _HandleChunk : public HandleChunkHead
    {
        SOSHandleData Data[128];

        _HandleChunk()
        {
            pData = Data;
            Size = sizeof(Data);
        }
    } HandleChunk;

    // Parameter used in HndEnumHandles callback.
    struct DacHandleWalkerParam
    {
        HandleChunkHead *Curr;      // The current chunk to write to
        HRESULT Result;             // HRESULT of the current enumeration
        CLRDATA_ADDRESS AppDomain;  // The AppDomain for the current bucket we are walking
        unsigned int Type;          // The type of handle we are currently walking

        DacHandleWalkerParam(HandleChunk *curr)
            : Curr(curr), Result(S_OK), AppDomain(0), Type(0)
        {
        }
    };

public:
    DacHandleWalker();
    virtual ~DacHandleWalker();

    HRESULT Init(ClrDataAccess *dac, UINT types[], UINT typeCount);
    HRESULT Init(ClrDataAccess *dac, UINT types[], UINT typeCount, int gen);
    HRESULT Init(UINT32 typemask);

    // SOS functions
    HRESULT STDMETHODCALLTYPE Skip(unsigned int count);
    HRESULT STDMETHODCALLTYPE Reset();
    HRESULT STDMETHODCALLTYPE GetCount(unsigned int *pCount);
    HRESULT STDMETHODCALLTYPE Next(unsigned int count,
                                   SOSHandleData handles[],
                                   unsigned int *pNeeded);

   // Dac-Dbi Functions
   HRESULT Next(ULONG celt, DacGcReference roots[], ULONG *pceltFetched);
private:
    static void CALLBACK EnumCallback(PTR_UNCHECKED_OBJECTREF pref, LPARAM *pExtraInfo, LPARAM userParam, LPARAM type);
    static void GetRefCountedHandleInfo(
        OBJECTREF oref, unsigned int uType,
        unsigned int *pRefCount, unsigned int *pJupiterRefCount, BOOL *pIsPegged, BOOL *pIsStrong);
    static UINT32 BuildTypemask(UINT types[], UINT typeCount);

private:
    static void CALLBACK EnumCallbackSOS(PTR_UNCHECKED_OBJECTREF pref, uintptr_t *pExtraInfo, uintptr_t userParam, uintptr_t type);
    static void CALLBACK EnumCallbackDac(PTR_UNCHECKED_OBJECTREF pref, uintptr_t *pExtraInfo, uintptr_t userParam, uintptr_t type);

    bool FetchMoreHandles(HANDLESCANPROC proc);
    static inline bool IsAlwaysStrongReference(unsigned int type)
    {
        return type == HNDTYPE_STRONG || type == HNDTYPE_PINNED || type == HNDTYPE_ASYNCPINNED || type == HNDTYPE_SIZEDREF;
    }

    template <class StructType, class IntType, HANDLESCANPROC EnumFunc>
    HRESULT DoHandleWalk(IntType celt, StructType handles[], IntType *pceltFetched)
    {
        SUPPORTS_DAC;

        if (handles == NULL || pceltFetched == NULL)
            return E_POINTER;

        HRESULT hr = S_OK;
        IntType fetched = 0;
        bool done = false;

        // On each iteration of the loop, either fetch more handles (filling in
        // the user's data structure), or copy handles from previous calls to
        // FetchMoreHandles which we could not store in the user's data (or simply
        // advance the current chunk to the next chunk).
        while (fetched < celt)
        {
            if (mCurr == NULL)
            {
                // Case 1:  We have no overflow data.  Stuff the user's array/size into
                // mHead, fetch more handles.  Additionally, if the previous call to
                // FetchMoreHandles returned false (mMap == NULL), break.
                if (mMap == NULL)
                    break;

                mHead.pData = handles+fetched;
                mHead.Size = (celt - fetched)*sizeof(StructType);

                done = !FetchMoreHandles(EnumFunc);
                fetched += mHead.Count;

                // Sanity check to make sure we haven't overflowed.  This should not happen.
                _ASSERTE(fetched <= celt);
            }
            else if (mChunkIndex >= mCurr->Count)
            {
                // Case 2:  We have overflow data, but the current index into the current
                // chunk is past the bounds.  Move to the next.  This could set mCurr to
                // null, which we'll catch on the next iteration.
                mCurr = mCurr->Next;
                mChunkIndex = 0;
            }
            else
            {
                // Case 3:  The last call to "Next" filled the user's array and had some handle
                // data leftover.  Walk the linked-list of arrays copying them into the user's
                // buffer until we have either exhausted the user's array or the leftover data.
                unsigned int toCopy = celt - fetched;  // Fill the user's buffer...

                // ...unless that would go past the bounds of the current chunk.
                if (toCopy + mChunkIndex > mCurr->Count)
                    toCopy = mCurr->Count - mChunkIndex;

                memcpy(handles+fetched, ((StructType*)(mCurr->pData))+mChunkIndex, toCopy*sizeof(StructType));
                mChunkIndex += toCopy;
                fetched += toCopy;
            }
        }

        if (fetched < celt)
            hr = S_FALSE;

        *pceltFetched = fetched;

        return hr;
    }

private:
    // Dac variables required for entering/leaving the dac.
    ClrDataAccess *mDac;
    ULONG32 m_instanceAge;

    // Handle table walking variables.
    dac_handle_table_map *mMap;
    int mIndex;
    UINT32 mTypeMask;
    int mGenerationFilter;

    // Storage variables
    HandleChunk mHead;
    unsigned int mChunkIndex;

    // Iterator variables
    HandleChunkHead *mCurr;
    int mIteratorIndex;
};


//----------------------------------------------------------------------------
//
// ClrDataAppDomain.
//
//----------------------------------------------------------------------------

class ClrDataAppDomain : public IXCLRDataAppDomain
{
public:
    ClrDataAppDomain(ClrDataAccess* dac,
                     AppDomain* appDomain);
    virtual ~ClrDataAppDomain(void);

    // IUnknown.
    STDMETHOD(QueryInterface)(THIS_
                              IN REFIID interfaceId,
                              OUT PVOID* iface);
    STDMETHOD_(ULONG, AddRef)(THIS);
    STDMETHOD_(ULONG, Release)(THIS);

    //
    // IXCLRDataAppDomain.
    //

    virtual HRESULT STDMETHODCALLTYPE GetProcess(
        /* [out] */ IXCLRDataProcess **process);

    virtual HRESULT STDMETHODCALLTYPE GetName(
        /* [in] */ ULONG32 bufLen,
        /* [out] */ ULONG32 *nameLen,
        /* [size_is][out] */ _Out_writes_to_opt_(bufLen, *nameLen) WCHAR name[  ]);

    virtual HRESULT STDMETHODCALLTYPE GetUniqueID(
        /* [out] */ ULONG64 *id);

    virtual HRESULT STDMETHODCALLTYPE GetFlags(
        /* [out] */ ULONG32 *flags);

    virtual HRESULT STDMETHODCALLTYPE IsSameObject(
        /* [in] */ IXCLRDataAppDomain *appDomain);

    virtual HRESULT STDMETHODCALLTYPE GetManagedObject(
        /* [out] */ IXCLRDataValue **value);

    virtual HRESULT STDMETHODCALLTYPE Request(
        /* [in] */ ULONG32 reqCode,
        /* [in] */ ULONG32 inBufferSize,
        /* [size_is][in] */ BYTE *inBuffer,
        /* [in] */ ULONG32 outBufferSize,
        /* [size_is][out] */ BYTE *outBuffer);

    AppDomain* GetAppDomain(void)
    {
        SUPPORTS_DAC;
        return m_appDomain;
    }

private:
    LONG m_refs;
    ClrDataAccess* m_dac;
    ULONG32 m_instanceAge;
    AppDomain* m_appDomain;
};

//----------------------------------------------------------------------------
//
// ClrDataAssembly.
//
//----------------------------------------------------------------------------

class ClrDataAssembly : public IXCLRDataAssembly
{
public:
    ClrDataAssembly(ClrDataAccess* dac,
                    Assembly* assembly);
    virtual ~ClrDataAssembly(void);

    // IUnknown.
    STDMETHOD(QueryInterface)(THIS_
                              IN REFIID interfaceId,
                              OUT PVOID* iface);
    STDMETHOD_(ULONG, AddRef)(THIS);
    STDMETHOD_(ULONG, Release)(THIS);

    //
    // IXCLRDataAssembly.
    //

    virtual HRESULT STDMETHODCALLTYPE StartEnumModules(
        /* [out] */ CLRDATA_ENUM *handle);

    virtual HRESULT STDMETHODCALLTYPE EnumModule(
        /* [in, out] */ CLRDATA_ENUM* handle,
        /* [out] */ IXCLRDataModule **mod);

    virtual HRESULT STDMETHODCALLTYPE EndEnumModules(
        /* [in] */ CLRDATA_ENUM handle);

    virtual HRESULT STDMETHODCALLTYPE StartEnumAppDomains(
        /* [out] */ CLRDATA_ENUM *handle);

    virtual HRESULT STDMETHODCALLTYPE EnumAppDomain(
        /* [in, out] */ CLRDATA_ENUM* handle,
        /* [out] */ IXCLRDataAppDomain **appDomain);

    virtual HRESULT STDMETHODCALLTYPE EndEnumAppDomains(
        /* [in] */ CLRDATA_ENUM handle);

    virtual HRESULT STDMETHODCALLTYPE GetName(
        /* [in] */ ULONG32 bufLen,
        /* [out] */ ULONG32 *nameLen,
        /* [size_is][out] */ _Out_writes_to_opt_(bufLen, *nameLen) WCHAR name[  ]);

    virtual HRESULT STDMETHODCALLTYPE GetFileName(
        /* [in] */ ULONG32 bufLen,
        /* [out] */ ULONG32 *nameLen,
        /* [size_is][out] */ _Out_writes_to_opt_(bufLen, *nameLen) WCHAR name[  ]);

    virtual HRESULT STDMETHODCALLTYPE GetDisplayName(
        /* [in] */ ULONG32 bufLen,
        /* [out] */ ULONG32 *nameLen,
        /* [size_is][out] */ _Out_writes_to_opt_(bufLen, *nameLen) WCHAR name[  ]);

    virtual HRESULT STDMETHODCALLTYPE GetFlags(
        /* [out] */ ULONG32 *flags);

    virtual HRESULT STDMETHODCALLTYPE IsSameObject(
        /* [in] */ IXCLRDataAssembly *assembly);

    virtual HRESULT STDMETHODCALLTYPE Request(
        /* [in] */ ULONG32 reqCode,
        /* [in] */ ULONG32 inBufferSize,
        /* [size_is][in] */ BYTE *inBuffer,
        /* [in] */ ULONG32 outBufferSize,
        /* [size_is][out] */ BYTE *outBuffer);

private:
    LONG m_refs;
    ClrDataAccess* m_dac;
    ULONG32 m_instanceAge;
    Assembly* m_assembly;
};

//----------------------------------------------------------------------------
//
// ClrDataModule.
//
//----------------------------------------------------------------------------

class ClrDataModule : public IXCLRDataModule, IXCLRDataModule2
{
public:
    ClrDataModule(ClrDataAccess* dac,
                  Module* module);
    virtual ~ClrDataModule(void);

    // IUnknown.
    STDMETHOD(QueryInterface)(THIS_
                              IN REFIID interfaceId,
                              OUT PVOID* iface);
    STDMETHOD_(ULONG, AddRef)(THIS);
    STDMETHOD_(ULONG, Release)(THIS);

    //
    // IXCLRDataModule.
    //

    virtual HRESULT STDMETHODCALLTYPE StartEnumAssemblies(
        /* [out] */ CLRDATA_ENUM *handle);

    virtual HRESULT STDMETHODCALLTYPE EnumAssembly(
        /* [in, out] */ CLRDATA_ENUM* handle,
        /* [out] */ IXCLRDataAssembly **assembly);

    virtual HRESULT STDMETHODCALLTYPE EndEnumAssemblies(
        /* [in] */ CLRDATA_ENUM handle);

    virtual HRESULT STDMETHODCALLTYPE StartEnumAppDomains(
        /* [out] */ CLRDATA_ENUM *handle);

    virtual HRESULT STDMETHODCALLTYPE EnumAppDomain(
        /* [in, out] */ CLRDATA_ENUM* handle,
        /* [out] */ IXCLRDataAppDomain **appDomain);

    virtual HRESULT STDMETHODCALLTYPE EndEnumAppDomains(
        /* [in] */ CLRDATA_ENUM handle);

    virtual HRESULT STDMETHODCALLTYPE StartEnumTypeDefinitions(
        /* [out] */ CLRDATA_ENUM *handle);

    virtual HRESULT STDMETHODCALLTYPE EnumTypeDefinition(
        /* [in, out] */ CLRDATA_ENUM* handle,
        /* [out] */ IXCLRDataTypeDefinition **typeDefinition);

    virtual HRESULT STDMETHODCALLTYPE EndEnumTypeDefinitions(
        /* [in] */ CLRDATA_ENUM handle);

    virtual HRESULT STDMETHODCALLTYPE StartEnumTypeInstances(
        /* [in] */ IXCLRDataAppDomain* appDomain,
        /* [out] */ CLRDATA_ENUM *handle);

    virtual HRESULT STDMETHODCALLTYPE EnumTypeInstance(
        /* [in, out] */ CLRDATA_ENUM* handle,
        /* [out] */ IXCLRDataTypeInstance **typeInstance);

    virtual HRESULT STDMETHODCALLTYPE EndEnumTypeInstances(
        /* [in] */ CLRDATA_ENUM handle);

    virtual HRESULT STDMETHODCALLTYPE StartEnumTypeDefinitionsByName(
        /* [in] */ LPCWSTR name,
        /* [in] */ ULONG32 flags,
        /* [out] */ CLRDATA_ENUM *handle);

    virtual HRESULT STDMETHODCALLTYPE EnumTypeDefinitionByName(
        /* [out][in] */ CLRDATA_ENUM *handle,
        /* [out] */ IXCLRDataTypeDefinition **type);

    virtual HRESULT STDMETHODCALLTYPE EndEnumTypeDefinitionsByName(
        /* [in] */ CLRDATA_ENUM handle);

    virtual HRESULT STDMETHODCALLTYPE StartEnumTypeInstancesByName(
        /* [in] */ LPCWSTR name,
        /* [in] */ ULONG32 flags,
        /* [in] */ IXCLRDataAppDomain *appDomain,
        /* [out] */ CLRDATA_ENUM *handle);

    virtual HRESULT STDMETHODCALLTYPE EnumTypeInstanceByName(
        /* [out][in] */ CLRDATA_ENUM *handle,
        /* [out] */ IXCLRDataTypeInstance **type);

    virtual HRESULT STDMETHODCALLTYPE EndEnumTypeInstancesByName(
        /* [in] */ CLRDATA_ENUM handle);

    virtual HRESULT STDMETHODCALLTYPE GetTypeDefinitionByToken(
        /* [in] */ mdTypeDef token,
        /* [out] */ IXCLRDataTypeDefinition **typeDefinition);

    virtual HRESULT STDMETHODCALLTYPE StartEnumMethodDefinitionsByName(
        /* [in] */ LPCWSTR name,
        /* [in] */ ULONG32 flags,
        /* [out] */ CLRDATA_ENUM *handle);

    virtual HRESULT STDMETHODCALLTYPE EnumMethodDefinitionByName(
        /* [in] */ CLRDATA_ENUM* handle,
        /* [out] */ IXCLRDataMethodDefinition **method);

    virtual HRESULT STDMETHODCALLTYPE EndEnumMethodDefinitionsByName(
        /* [in] */ CLRDATA_ENUM handle);

    virtual HRESULT STDMETHODCALLTYPE StartEnumMethodInstancesByName(
        /* [in] */ LPCWSTR name,
        /* [in] */ ULONG32 flags,
        /* [in] */ IXCLRDataAppDomain* appDomain,
        /* [out] */ CLRDATA_ENUM *handle);

    virtual HRESULT STDMETHODCALLTYPE EnumMethodInstanceByName(
        /* [in] */ CLRDATA_ENUM* handle,
        /* [out] */ IXCLRDataMethodInstance **method);

    virtual HRESULT STDMETHODCALLTYPE EndEnumMethodInstancesByName(
        /* [in] */ CLRDATA_ENUM handle);

    virtual HRESULT STDMETHODCALLTYPE GetMethodDefinitionByToken(
        /* [in] */ mdMethodDef token,
        /* [out] */ IXCLRDataMethodDefinition **methodDefinition);

    virtual HRESULT STDMETHODCALLTYPE StartEnumDataByName(
        /* [in] */ LPCWSTR name,
        /* [in] */ ULONG32 flags,
        /* [in] */ IXCLRDataAppDomain* appDomain,
        /* [in] */ IXCLRDataTask* tlsTask,
        /* [out] */ CLRDATA_ENUM *handle);

    virtual HRESULT STDMETHODCALLTYPE EnumDataByName(
        /* [in] */ CLRDATA_ENUM* handle,
        /* [out] */ IXCLRDataValue **value);

    virtual HRESULT STDMETHODCALLTYPE EndEnumDataByName(
        /* [in] */ CLRDATA_ENUM handle);

    virtual HRESULT STDMETHODCALLTYPE GetName(
        /* [in] */ ULONG32 bufLen,
        /* [out] */ ULONG32 *nameLen,
        /* [size_is][out] */ _Out_writes_to_opt_(bufLen, *nameLen) WCHAR name[  ]);

    virtual HRESULT STDMETHODCALLTYPE GetFileName(
        /* [in] */ ULONG32 bufLen,
        /* [out] */ ULONG32 *nameLen,
        /* [size_is][out] */ _Out_writes_to_opt_(bufLen, *nameLen) WCHAR name[  ]);

    virtual HRESULT STDMETHODCALLTYPE GetVersionId(
        /* [out] */ GUID* vid);

    virtual HRESULT STDMETHODCALLTYPE GetFlags(
        /* [out] */ ULONG32 *flags);

    virtual HRESULT STDMETHODCALLTYPE IsSameObject(
        /* [in] */ IXCLRDataModule *mod);

    virtual HRESULT STDMETHODCALLTYPE StartEnumExtents(
        /* [out] */ CLRDATA_ENUM *handle);

    virtual HRESULT STDMETHODCALLTYPE EnumExtent(
        /* [in, out] */ CLRDATA_ENUM* handle,
        /* [out] */ CLRDATA_MODULE_EXTENT *extent);

    virtual HRESULT STDMETHODCALLTYPE EndEnumExtents(
        /* [in] */ CLRDATA_ENUM handle);

    virtual HRESULT STDMETHODCALLTYPE Request(
        /* [in] */ ULONG32 reqCode,
        /* [in] */ ULONG32 inBufferSize,
        /* [size_is][in] */ BYTE *inBuffer,
        /* [in] */ ULONG32 outBufferSize,
        /* [size_is][out] */ BYTE *outBuffer);

    HRESULT RequestGetModulePtr(IN ULONG32 inBufferSize,
                                IN BYTE* inBuffer,
                                IN ULONG32 outBufferSize,
                                OUT BYTE* outBuffer);

    HRESULT RequestGetModuleData(IN ULONG32 inBufferSize,
                                 IN BYTE* inBuffer,
                                 IN ULONG32 outBufferSize,
                                 OUT BYTE* outBuffer);

    Module* GetModule(void)
    {
        return m_module;
    }

    //
    // IXCLRDataModule2
    //
    virtual HRESULT STDMETHODCALLTYPE SetJITCompilerFlags(
        /* [in] */ DWORD dwFlags );

private:
    // Returns an instance of IID_IMetaDataImport.
    HRESULT GetMdInterface(PVOID* retIface);

    LONG m_refs;
    ClrDataAccess* m_dac;
    ULONG32 m_instanceAge;
    Module* m_module;
    IMetaDataImport* m_mdImport;
    bool m_setExtents;
    CLRDATA_MODULE_EXTENT m_extents[2];
    CLRDATA_MODULE_EXTENT* m_extentsEnd;
};

//----------------------------------------------------------------------------
//
// ClrDataTypeDefinition.
//
//----------------------------------------------------------------------------

class ClrDataTypeDefinition : public IXCLRDataTypeDefinition
{
public:
    ClrDataTypeDefinition(ClrDataAccess* dac,
                          Module* module,
                          mdTypeDef token,
                          TypeHandle typeHandle);
    virtual ~ClrDataTypeDefinition(void);

    // IUnknown.
    STDMETHOD(QueryInterface)(THIS_
                              IN REFIID interfaceId,
                              OUT PVOID* iface);
    STDMETHOD_(ULONG, AddRef)(THIS);
    STDMETHOD_(ULONG, Release)(THIS);

    //
    // IXCLRDataTypeDefinition.
    //

    virtual HRESULT STDMETHODCALLTYPE GetModule(
        /* [out] */ IXCLRDataModule **mod);

    virtual HRESULT STDMETHODCALLTYPE StartEnumMethodDefinitions(
        /* [out] */ CLRDATA_ENUM *handle);

    virtual HRESULT STDMETHODCALLTYPE EnumMethodDefinition(
        /* [in, out] */ CLRDATA_ENUM* handle,
        /* [out] */ IXCLRDataMethodDefinition **methodDefinition);

    virtual HRESULT STDMETHODCALLTYPE EndEnumMethodDefinitions(
        /* [in] */ CLRDATA_ENUM handle);

    virtual HRESULT STDMETHODCALLTYPE StartEnumMethodDefinitionsByName(
        /* [in] */ LPCWSTR name,
        /* [in] */ ULONG32 flags,
        /* [out] */ CLRDATA_ENUM *handle);

    virtual HRESULT STDMETHODCALLTYPE EnumMethodDefinitionByName(
        /* [in] */ CLRDATA_ENUM* handle,
        /* [out] */ IXCLRDataMethodDefinition **method);

    virtual HRESULT STDMETHODCALLTYPE EndEnumMethodDefinitionsByName(
        /* [in] */ CLRDATA_ENUM handle);

    virtual HRESULT STDMETHODCALLTYPE GetMethodDefinitionByToken(
        /* [in] */ mdMethodDef token,
        /* [out] */ IXCLRDataMethodDefinition **methodDefinition);

    virtual HRESULT STDMETHODCALLTYPE StartEnumInstances(
        /* [in] */ IXCLRDataAppDomain* appDomain,
        /* [out] */ CLRDATA_ENUM *handle);

    virtual HRESULT STDMETHODCALLTYPE EnumInstance(
        /* [out][in] */ CLRDATA_ENUM *handle,
        /* [out] */ IXCLRDataTypeInstance **instance);

    virtual HRESULT STDMETHODCALLTYPE EndEnumInstances(
        /* [in] */ CLRDATA_ENUM handle);

    virtual HRESULT STDMETHODCALLTYPE GetNumFields(
        /* [in] */ ULONG32 flags,
        /* [out] */ ULONG32 *numFields);

    virtual HRESULT STDMETHODCALLTYPE StartEnumFields(
        /* [in] */ ULONG32 flags,
        /* [out] */ CLRDATA_ENUM *handle);

    virtual HRESULT STDMETHODCALLTYPE EnumField(
        /* [out][in] */ CLRDATA_ENUM *handle,
        /* [in] */ ULONG32 nameBufLen,
        /* [out] */ ULONG32 *nameLen,
        /* [size_is][out] */ _Out_writes_to_opt_(nameBufLen, *nameLen) WCHAR nameBuf[  ],
        /* [out] */ IXCLRDataTypeDefinition **type,
        /* [out] */ ULONG32 *flags,
        /* [out] */ mdFieldDef *token);

    virtual HRESULT STDMETHODCALLTYPE EnumField2(
        /* [out][in] */ CLRDATA_ENUM *handle,
        /* [in] */ ULONG32 nameBufLen,
        /* [out] */ ULONG32 *nameLen,
        /* [size_is][out] */ _Out_writes_to_opt_(nameBufLen, *nameLen) WCHAR nameBuf[  ],
        /* [out] */ IXCLRDataTypeDefinition **type,
        /* [out] */ ULONG32 *flags,
        /* [out] */ IXCLRDataModule** tokenScope,
        /* [out] */ mdFieldDef *token);

    virtual HRESULT STDMETHODCALLTYPE EndEnumFields(
        /* [in] */ CLRDATA_ENUM handle);

    virtual HRESULT STDMETHODCALLTYPE StartEnumFieldsByName(
        /* [in] */ LPCWSTR name,
        /* [in] */ ULONG32 nameFlags,
        /* [in] */ ULONG32 fieldFlags,
        /* [out] */ CLRDATA_ENUM *handle);

    virtual HRESULT STDMETHODCALLTYPE EnumFieldByName(
        /* [out][in] */ CLRDATA_ENUM *handle,
        /* [out] */ IXCLRDataTypeDefinition **type,
        /* [out] */ ULONG32 *flags,
        /* [out] */ mdFieldDef *token);

    virtual HRESULT STDMETHODCALLTYPE EnumFieldByName2(
        /* [out][in] */ CLRDATA_ENUM *handle,
        /* [out] */ IXCLRDataTypeDefinition **type,
        /* [out] */ ULONG32 *flags,
        /* [out] */ IXCLRDataModule** tokenScope,
        /* [out] */ mdFieldDef *token);

    virtual HRESULT STDMETHODCALLTYPE EndEnumFieldsByName(
        /* [in] */ CLRDATA_ENUM handle);

    virtual HRESULT STDMETHODCALLTYPE GetFieldByToken(
        /* [in] */ mdFieldDef token,
        /* [in] */ ULONG32 nameBufLen,
        /* [out] */ ULONG32 *nameLen,
        /* [size_is][out] */ _Out_writes_to_opt_(nameBufLen, *nameLen) WCHAR nameBuf[  ],
        /* [out] */ IXCLRDataTypeDefinition **type,
        /* [out] */ ULONG32* flags);

    virtual HRESULT STDMETHODCALLTYPE GetFieldByToken2(
        /* [in] */ IXCLRDataModule* tokenScope,
        /* [in] */ mdFieldDef token,
        /* [in] */ ULONG32 nameBufLen,
        /* [out] */ ULONG32 *nameLen,
        /* [size_is][out] */ _Out_writes_to_opt_(nameBufLen, *nameLen) WCHAR nameBuf[  ],
        /* [out] */ IXCLRDataTypeDefinition **type,
        /* [out] */ ULONG32* flags);

    virtual HRESULT STDMETHODCALLTYPE GetName(
        /* [in] */ ULONG32 flags,
        /* [in] */ ULONG32 bufLen,
        /* [out] */ ULONG32 *nameLen,
        /* [size_is][out] */ _Out_writes_to_opt_(bufLen, *nameLen) WCHAR nameBuf[  ]);

    virtual HRESULT STDMETHODCALLTYPE GetTokenAndScope(
        /* [out] */ mdTypeDef *token,
        /* [out] */ IXCLRDataModule **mod);

    virtual HRESULT STDMETHODCALLTYPE GetCorElementType(
        /* [out] */ CorElementType *type);

    virtual HRESULT STDMETHODCALLTYPE GetFlags(
        /* [out] */ ULONG32 *flags);

    virtual HRESULT STDMETHODCALLTYPE GetBase(
        /* [out] */ IXCLRDataTypeDefinition **base);

    virtual HRESULT STDMETHODCALLTYPE IsSameObject(
        /* [in] */ IXCLRDataTypeDefinition *type);

    virtual HRESULT STDMETHODCALLTYPE Request(
        /* [in] */ ULONG32 reqCode,
        /* [in] */ ULONG32 inBufferSize,
        /* [size_is][in] */ BYTE *inBuffer,
        /* [in] */ ULONG32 outBufferSize,
        /* [size_is][out] */ BYTE *outBuffer);

    virtual HRESULT STDMETHODCALLTYPE GetArrayRank(
        /* [out] */ ULONG32* rank);

    virtual HRESULT STDMETHODCALLTYPE GetTypeNotification(
        /* [out] */ ULONG32* flags);

    virtual HRESULT STDMETHODCALLTYPE SetTypeNotification(
        /* [in] */ ULONG32 flags);

    static HRESULT NewFromModule(ClrDataAccess* dac,
                                 Module* module,
                                 mdTypeDef token,
                                 ClrDataTypeDefinition** typeDef,
                                 IXCLRDataTypeDefinition** pubTypeDef);

    TypeHandle GetTypeHandle(void)
    {
        return m_typeHandle;
    }

private:
    LONG m_refs;
    ClrDataAccess* m_dac;
    ULONG32 m_instanceAge;
    Module* m_module;
    mdTypeDef m_token;
    TypeHandle m_typeHandle;
};

//----------------------------------------------------------------------------
//
// ClrDataTypeInstance.
//
//----------------------------------------------------------------------------

class ClrDataTypeInstance : public IXCLRDataTypeInstance
{
public:
    ClrDataTypeInstance(ClrDataAccess* dac,
                        AppDomain* appDomain,
                        TypeHandle typeHandle);
    virtual ~ClrDataTypeInstance(void);

    // IUnknown.
    STDMETHOD(QueryInterface)(THIS_
                              IN REFIID interfaceId,
                              OUT PVOID* iface);
    STDMETHOD_(ULONG, AddRef)(THIS);
    STDMETHOD_(ULONG, Release)(THIS);

    //
    // IXCLRDataTypeInstance.
    //

    virtual HRESULT STDMETHODCALLTYPE StartEnumMethodInstances(
        /* [out] */ CLRDATA_ENUM *handle);

    virtual HRESULT STDMETHODCALLTYPE EnumMethodInstance(
        /* [in, out] */ CLRDATA_ENUM* handle,
        /* [out] */ IXCLRDataMethodInstance **methodInstance);

    virtual HRESULT STDMETHODCALLTYPE EndEnumMethodInstances(
        /* [in] */ CLRDATA_ENUM handle);

    virtual HRESULT STDMETHODCALLTYPE StartEnumMethodInstancesByName(
        /* [in] */ LPCWSTR name,
        /* [in] */ ULONG32 flags,
        /* [out] */ CLRDATA_ENUM *handle);

    virtual HRESULT STDMETHODCALLTYPE EnumMethodInstanceByName(
        /* [in] */ CLRDATA_ENUM* handle,
        /* [out] */ IXCLRDataMethodInstance **method);

    virtual HRESULT STDMETHODCALLTYPE EndEnumMethodInstancesByName(
        /* [in] */ CLRDATA_ENUM handle);

    virtual HRESULT STDMETHODCALLTYPE GetNumStaticFields(
        /* [out] */ ULONG32 *numFields);

    virtual HRESULT STDMETHODCALLTYPE GetStaticFieldByIndex(
        /* [in] */ ULONG32 index,
        /* [in] */ IXCLRDataTask *tlsTask,
        /* [out] */ IXCLRDataValue **field,
        /* [in] */ ULONG32 bufLen,
        /* [out] */ ULONG32 *nameLen,
        /* [size_is][out] */ _Out_writes_to_opt_(bufLen, *nameLen) WCHAR nameBuf[  ],
        /* [out] */ mdFieldDef *token);

    virtual HRESULT STDMETHODCALLTYPE StartEnumStaticFieldsByName(
        /* [in] */ LPCWSTR name,
        /* [in] */ ULONG32 flags,
        /* [in] */ IXCLRDataTask *tlsTask,
        /* [out] */ CLRDATA_ENUM *handle);

    virtual HRESULT STDMETHODCALLTYPE EnumStaticFieldByName(
        /* [out][in] */ CLRDATA_ENUM *handle,
        /* [out] */ IXCLRDataValue **value);

    virtual HRESULT STDMETHODCALLTYPE EndEnumStaticFieldsByName(
        /* [in] */ CLRDATA_ENUM handle);

    virtual HRESULT STDMETHODCALLTYPE GetNumStaticFields2(
        /* [in] */ ULONG32 flags,
        /* [out] */ ULONG32 *numFields);

    virtual HRESULT STDMETHODCALLTYPE StartEnumStaticFields(
        /* [in] */ ULONG32 flags,
        /* [in] */ IXCLRDataTask *tlsTask,
        /* [out] */ CLRDATA_ENUM *handle);

    virtual HRESULT STDMETHODCALLTYPE EnumStaticField(
        /* [out][in] */ CLRDATA_ENUM *handle,
        /* [out] */ IXCLRDataValue **value);

    virtual HRESULT STDMETHODCALLTYPE EnumStaticField2(
        /* [out][in] */ CLRDATA_ENUM *handle,
        /* [out] */ IXCLRDataValue **value,
        /* [in] */ ULONG32 bufLen,
        /* [out] */ ULONG32 *nameLen,
        /* [size_is][out] */ _Out_writes_to_opt_(bufLen, *nameLen) WCHAR nameBuf[  ],
        /* [out] */ IXCLRDataModule** tokenScope,
        /* [out] */ mdFieldDef *token);

    virtual HRESULT STDMETHODCALLTYPE EndEnumStaticFields(
        /* [in] */ CLRDATA_ENUM handle);

    virtual HRESULT STDMETHODCALLTYPE StartEnumStaticFieldsByName2(
        /* [in] */ LPCWSTR name,
        /* [in] */ ULONG32 nameFlags,
        /* [in] */ ULONG32 fieldFlags,
        /* [in] */ IXCLRDataTask *tlsTask,
        /* [out] */ CLRDATA_ENUM *handle);

    virtual HRESULT STDMETHODCALLTYPE EnumStaticFieldByName3(
        /* [out][in] */ CLRDATA_ENUM *handle,
        /* [out] */ IXCLRDataValue **value,
        /* [out] */ IXCLRDataModule** tokenScope,
        /* [out] */ mdFieldDef *token);

    virtual HRESULT STDMETHODCALLTYPE EnumStaticFieldByName2(
        /* [out][in] */ CLRDATA_ENUM *handle,
        /* [out] */ IXCLRDataValue **value);

    virtual HRESULT STDMETHODCALLTYPE EndEnumStaticFieldsByName2(
        /* [in] */ CLRDATA_ENUM handle);

    virtual HRESULT STDMETHODCALLTYPE GetStaticFieldByToken(
        /* [in] */ mdFieldDef token,
        /* [in] */ IXCLRDataTask *tlsTask,
        /* [out] */ IXCLRDataValue **field,
        /* [in] */ ULONG32 bufLen,
        /* [out] */ ULONG32 *nameLen,
        /* [size_is][out] */ _Out_writes_to_opt_(bufLen, *nameLen) WCHAR nameBuf[  ]);

    virtual HRESULT STDMETHODCALLTYPE GetStaticFieldByToken2(
        /* [in] */ IXCLRDataModule* tokenScope,
        /* [in] */ mdFieldDef token,
        /* [in] */ IXCLRDataTask *tlsTask,
        /* [out] */ IXCLRDataValue **field,
        /* [in] */ ULONG32 bufLen,
        /* [out] */ ULONG32 *nameLen,
        /* [size_is][out] */ _Out_writes_to_opt_(bufLen, *nameLen) WCHAR nameBuf[  ]);

    virtual HRESULT STDMETHODCALLTYPE GetName(
        /* [in] */ ULONG32 flags,
        /* [in] */ ULONG32 bufLen,
        /* [out] */ ULONG32 *nameLen,
        /* [size_is][out] */ _Out_writes_to_opt_(bufLen, *nameLen) WCHAR nameBuf[  ]);

    virtual HRESULT STDMETHODCALLTYPE GetModule(
        /* [out] */ IXCLRDataModule **mod);

    virtual HRESULT STDMETHODCALLTYPE GetDefinition(
        /* [out] */ IXCLRDataTypeDefinition **typeDefinition);

    virtual HRESULT STDMETHODCALLTYPE GetFlags(
        /* [out] */ ULONG32 *flags);

    virtual HRESULT STDMETHODCALLTYPE GetBase(
        /* [out] */ IXCLRDataTypeInstance **base);

    virtual HRESULT STDMETHODCALLTYPE IsSameObject(
        /* [in] */ IXCLRDataTypeInstance *type);

    virtual HRESULT STDMETHODCALLTYPE GetNumTypeArguments(
        /* [out] */ ULONG32 *numTypeArgs);

    virtual HRESULT STDMETHODCALLTYPE GetTypeArgumentByIndex(
        /* [in] */ ULONG32 index,
        /* [out] */ IXCLRDataTypeInstance **typeArg);

    virtual HRESULT STDMETHODCALLTYPE Request(
        /* [in] */ ULONG32 reqCode,
        /* [in] */ ULONG32 inBufferSize,
        /* [size_is][in] */ BYTE *inBuffer,
        /* [in] */ ULONG32 outBufferSize,
        /* [size_is][out] */ BYTE *outBuffer);

    static HRESULT NewFromModule(ClrDataAccess* dac,
                                 AppDomain* appDomain,
                                 Module* module,
                                 mdTypeDef token,
                                 ClrDataTypeInstance** typeInst,
                                 IXCLRDataTypeInstance** pubTypeInst);

    TypeHandle GetTypeHandle(void)
    {
        return m_typeHandle;
    }

private:
    LONG m_refs;
    ClrDataAccess* m_dac;
    ULONG32 m_instanceAge;
    AppDomain* m_appDomain;
    TypeHandle m_typeHandle;
};

//----------------------------------------------------------------------------
//
// ClrDataMethodDefinition.
//
//----------------------------------------------------------------------------

class ClrDataMethodDefinition : public IXCLRDataMethodDefinition
{
public:
    ClrDataMethodDefinition(ClrDataAccess* dac,
                            Module* module,
                            mdMethodDef token,
                            MethodDesc* methodDesc);
    virtual ~ClrDataMethodDefinition(void);

    // IUnknown.
    STDMETHOD(QueryInterface)(THIS_
                              IN REFIID interfaceId,
                              OUT PVOID* iface);
    STDMETHOD_(ULONG, AddRef)(THIS);
    STDMETHOD_(ULONG, Release)(THIS);

    //
    // IXCLRDataMethodDefinition.
    //

    virtual HRESULT STDMETHODCALLTYPE GetTypeDefinition(
        /* [out] */ IXCLRDataTypeDefinition **typeDefinition);

    virtual HRESULT STDMETHODCALLTYPE StartEnumInstances(
        /* [in] */ IXCLRDataAppDomain* appDomain,
        /* [out] */ CLRDATA_ENUM *handle);

    virtual HRESULT STDMETHODCALLTYPE EnumInstance(
        /* [out][in] */ CLRDATA_ENUM *handle,
        /* [out] */ IXCLRDataMethodInstance **instance);

    virtual HRESULT STDMETHODCALLTYPE EndEnumInstances(
        /* [in] */ CLRDATA_ENUM handle);

    virtual HRESULT STDMETHODCALLTYPE GetName(
        /* [in] */ ULONG32 flags,
        /* [in] */ ULONG32 bufLen,
        /* [out] */ ULONG32 *nameLen,
        /* [size_is][out] */ _Out_writes_to_opt_(bufLen, *nameLen) WCHAR name[  ]);

    virtual HRESULT STDMETHODCALLTYPE GetTokenAndScope(
        /* [out] */ mdMethodDef *token,
        /* [out] */ IXCLRDataModule **mod);

    virtual HRESULT STDMETHODCALLTYPE GetFlags(
        /* [out] */ ULONG32 *flags);

    virtual HRESULT STDMETHODCALLTYPE IsSameObject(
        /* [in] */ IXCLRDataMethodDefinition *method);

    virtual HRESULT STDMETHODCALLTYPE GetLatestEnCVersion(
        /* [out] */ ULONG32* version);

    virtual HRESULT STDMETHODCALLTYPE StartEnumExtents(
        /* [out] */ CLRDATA_ENUM *handle);

    virtual HRESULT STDMETHODCALLTYPE EnumExtent(
        /* [out][in] */ CLRDATA_ENUM *handle,
        /* [out] */ CLRDATA_METHDEF_EXTENT *extent);

    virtual HRESULT STDMETHODCALLTYPE EndEnumExtents(
        /* [in] */ CLRDATA_ENUM handle);

    virtual HRESULT STDMETHODCALLTYPE GetCodeNotification(
        /* [out] */ ULONG32 *flags);

    virtual HRESULT STDMETHODCALLTYPE SetCodeNotification(
        /* [in] */ ULONG32 flags);

    virtual HRESULT STDMETHODCALLTYPE GetRepresentativeEntryAddress(
        /* [out] */ CLRDATA_ADDRESS* addr);

    virtual HRESULT STDMETHODCALLTYPE HasClassOrMethodInstantiation(
        /*[out]*/ BOOL* bGeneric);

    virtual HRESULT STDMETHODCALLTYPE Request(
        /* [in] */ ULONG32 reqCode,
        /* [in] */ ULONG32 inBufferSize,
        /* [size_is][in] */ BYTE *inBuffer,
        /* [in] */ ULONG32 outBufferSize,
        /* [size_is][out] */ BYTE *outBuffer);

    COR_ILMETHOD* GetIlMethod(void);

    static HRESULT NewFromModule(ClrDataAccess* dac,
                                 Module* module,
                                 mdMethodDef token,
                                 ClrDataMethodDefinition** methDef,
                                 IXCLRDataMethodDefinition** pubMethDef);

    static HRESULT GetSharedMethodFlags(MethodDesc* methodDesc,
                                        ULONG32* flags);

    // We don't need this if we are able to form name using token
    MethodDesc *GetMethodDesc() { return m_methodDesc;}
private:
    LONG m_refs;
    ClrDataAccess* m_dac;
    ULONG32 m_instanceAge;
    Module* m_module;
    mdMethodDef m_token;
    MethodDesc* m_methodDesc;
};

//----------------------------------------------------------------------------
//
// ClrDataMethodInstance.
//
//----------------------------------------------------------------------------

class ClrDataMethodInstance : public IXCLRDataMethodInstance
{
public:
    ClrDataMethodInstance(ClrDataAccess* dac,
                          AppDomain* appDomain,
                          MethodDesc* methodDesc);
    virtual ~ClrDataMethodInstance(void);

    // IUnknown.
    STDMETHOD(QueryInterface)(THIS_
                              IN REFIID interfaceId,
                              OUT PVOID* iface);
    STDMETHOD_(ULONG, AddRef)(THIS);
    STDMETHOD_(ULONG, Release)(THIS);

    //
    // IXCLRDataMethodInstance.
    //

    virtual HRESULT STDMETHODCALLTYPE GetTypeInstance(
        /* [out] */ IXCLRDataTypeInstance **typeInstance);

    virtual HRESULT STDMETHODCALLTYPE GetDefinition(
        /* [out] */ IXCLRDataMethodDefinition **methodDefinition);

    virtual HRESULT STDMETHODCALLTYPE GetTokenAndScope(
        /* [out] */ mdMethodDef *token,
        /* [out] */ IXCLRDataModule **mod);

    virtual HRESULT STDMETHODCALLTYPE GetName(
        /* [in] */ ULONG32 flags,
        /* [in] */ ULONG32 bufLen,
        /* [out] */ ULONG32 *nameLen,
        /* [size_is][out] */ _Out_writes_to_opt_(bufLen, *nameLen) WCHAR name[  ]);

    virtual HRESULT STDMETHODCALLTYPE GetFlags(
        /* [out] */ ULONG32 *flags);

    virtual HRESULT STDMETHODCALLTYPE IsSameObject(
        /* [in] */ IXCLRDataMethodInstance *method);

    virtual HRESULT STDMETHODCALLTYPE GetEnCVersion(
        /* [out] */ ULONG32* version);

    virtual HRESULT STDMETHODCALLTYPE GetNumTypeArguments(
        /* [out] */ ULONG32 *numTypeArgs);

    virtual HRESULT STDMETHODCALLTYPE GetTypeArgumentByIndex(
        /* [in] */ ULONG32 index,
        /* [out] */ IXCLRDataTypeInstance **typeArg);

    virtual HRESULT STDMETHODCALLTYPE GetILOffsetsByAddress(
        /* [in] */ CLRDATA_ADDRESS address,
        /* [in] */ ULONG32 offsetsLen,
        /* [out] */ ULONG32 *offsetsNeeded,
        /* [size_is][out] */ ULONG32 ilOffsets[  ]);

    virtual HRESULT STDMETHODCALLTYPE GetAddressRangesByILOffset(
        /* [in] */ ULONG32 ilOffset,
        /* [in] */ ULONG32 rangesLen,
        /* [out] */ ULONG32 *rangesNeeded,
        /* [size_is][out] */ CLRDATA_ADDRESS_RANGE addressRanges[  ]);

    virtual HRESULT STDMETHODCALLTYPE GetILAddressMap(
        /* [in] */ ULONG32 mapLen,
        /* [out] */ ULONG32 *mapNeeded,
        /* [size_is][out] */ CLRDATA_IL_ADDRESS_MAP maps[  ]);

    virtual HRESULT STDMETHODCALLTYPE StartEnumExtents(
        /* [out] */ CLRDATA_ENUM *handle);

    virtual HRESULT STDMETHODCALLTYPE EnumExtent(
        /* [out][in] */ CLRDATA_ENUM *handle,
        /* [out] */ CLRDATA_ADDRESS_RANGE *extent);

    virtual HRESULT STDMETHODCALLTYPE EndEnumExtents(
        /* [in] */ CLRDATA_ENUM handle);

    virtual HRESULT STDMETHODCALLTYPE GetRepresentativeEntryAddress(
        /* [out] */ CLRDATA_ADDRESS* addr);

    virtual HRESULT STDMETHODCALLTYPE Request(
        /* [in] */ ULONG32 reqCode,
        /* [in] */ ULONG32 inBufferSize,
        /* [size_is][in] */ BYTE *inBuffer,
        /* [in] */ ULONG32 outBufferSize,
        /* [size_is][out] */ BYTE *outBuffer);

    static HRESULT NewFromModule(ClrDataAccess* dac,
                                 AppDomain* appDomain,
                                 Module* module,
                                 mdMethodDef token,
                                 ClrDataMethodInstance** methInst,
                                 IXCLRDataMethodInstance** pubMethInst);

private:
    friend class ClrDataAccess;
    LONG m_refs;
    ClrDataAccess* m_dac;
    ULONG32 m_instanceAge;
    AppDomain* m_appDomain;
    MethodDesc* m_methodDesc;
};

//----------------------------------------------------------------------------
//
// ClrDataTask.
//
//----------------------------------------------------------------------------

class ClrDataTask : public IXCLRDataTask
{
public:
    ClrDataTask(ClrDataAccess* dac,
                Thread* Thread);
    virtual ~ClrDataTask(void);

    // IUnknown.
    STDMETHOD(QueryInterface)(THIS_
                              IN REFIID interfaceId,
                              OUT PVOID* iface);
    STDMETHOD_(ULONG, AddRef)(THIS);
    STDMETHOD_(ULONG, Release)(THIS);

    //
    // IXCLRDataTask.
    //

    virtual HRESULT STDMETHODCALLTYPE GetProcess(
        /* [out] */ IXCLRDataProcess **process);

    virtual HRESULT STDMETHODCALLTYPE GetCurrentAppDomain(
        /* [out] */ IXCLRDataAppDomain **appDomain);

    virtual HRESULT STDMETHODCALLTYPE GetName(
        /* [in] */ ULONG32 bufLen,
        /* [out] */ ULONG32 *nameLen,
        /* [size_is][out] */ _Out_writes_to_opt_(bufLen, *nameLen) WCHAR name[  ]);

    virtual HRESULT STDMETHODCALLTYPE GetUniqueID(
        /* [out] */ ULONG64 *id);

    virtual HRESULT STDMETHODCALLTYPE GetFlags(
        /* [out] */ ULONG32 *flags);

    virtual HRESULT STDMETHODCALLTYPE IsSameObject(
        /* [in] */ IXCLRDataTask *task);

    virtual HRESULT STDMETHODCALLTYPE GetManagedObject(
        /* [out] */ IXCLRDataValue **value);

    virtual HRESULT STDMETHODCALLTYPE GetDesiredExecutionState(
        /* [out] */ ULONG32 *state);

    virtual HRESULT STDMETHODCALLTYPE SetDesiredExecutionState(
        /* [in] */ ULONG32 state);

    virtual HRESULT STDMETHODCALLTYPE CreateStackWalk(
        /* [in] */ ULONG32 flags,
        /* [out] */ IXCLRDataStackWalk **stackWalk);

    virtual HRESULT STDMETHODCALLTYPE GetOSThreadID(
        /* [out] */ ULONG32 *id);

    virtual HRESULT STDMETHODCALLTYPE GetContext(
        /* [in] */ ULONG32 contextFlags,
        /* [in] */ ULONG32 contextBufSize,
        /* [out] */ ULONG32 *contextSize,
        /* [size_is][out] */ BYTE contextBuf[  ]);

    virtual HRESULT STDMETHODCALLTYPE SetContext(
        /* [in] */ ULONG32 contextSize,
        /* [size_is][in] */ BYTE context[  ]);

    virtual HRESULT STDMETHODCALLTYPE GetCurrentExceptionState(
        /* [out] */ IXCLRDataExceptionState **exception);

    virtual HRESULT STDMETHODCALLTYPE GetLastExceptionState(
        /* [out] */ IXCLRDataExceptionState **exception);

    virtual HRESULT STDMETHODCALLTYPE Request(
        /* [in] */ ULONG32 reqCode,
        /* [in] */ ULONG32 inBufferSize,
        /* [size_is][in] */ BYTE *inBuffer,
        /* [in] */ ULONG32 outBufferSize,
        /* [size_is][out] */ BYTE *outBuffer);

    Thread* GetThread(void)
    {
        return m_thread;
    }

private:
    LONG m_refs;
    ClrDataAccess* m_dac;
    ULONG32 m_instanceAge;
    Thread* m_thread;
};

//----------------------------------------------------------------------------
//
// ClrDataStackWalk.
//
//----------------------------------------------------------------------------

class ClrDataStackWalk : public IXCLRDataStackWalk
{
public:
    ClrDataStackWalk(ClrDataAccess* dac,
                     Thread* Thread,
                     ULONG32 flags);
    virtual ~ClrDataStackWalk(void);

    // IUnknown.
    STDMETHOD(QueryInterface)(THIS_
                              IN REFIID interfaceId,
                              OUT PVOID* iface);
    STDMETHOD_(ULONG, AddRef)(THIS);
    STDMETHOD_(ULONG, Release)(THIS);

    //
    // IXCLRDataStackWalk.
    //

    virtual HRESULT STDMETHODCALLTYPE GetContext(
        /* [in] */ ULONG32 contextFlags,
        /* [in] */ ULONG32 contextBufSize,
        /* [out] */ ULONG32 *contextSize,
        /* [size_is][out] */ BYTE contextBuf[  ]);

    virtual HRESULT STDMETHODCALLTYPE SetContext(
        /* [in] */ ULONG32 contextSize,
        /* [size_is][in] */ BYTE context[  ]);

    virtual HRESULT STDMETHODCALLTYPE Next( void);

    virtual HRESULT STDMETHODCALLTYPE GetStackSizeSkipped(
        /* [out] */ ULONG64 *stackSizeSkipped);

    virtual HRESULT STDMETHODCALLTYPE GetFrameType(
        /* [out] */ CLRDataSimpleFrameType *simpleType,
        /* [out] */ CLRDataDetailedFrameType *detailedType);

    virtual HRESULT STDMETHODCALLTYPE GetFrame(
        /* [out] */ IXCLRDataFrame **frame);

    virtual HRESULT STDMETHODCALLTYPE Request(
        /* [in] */ ULONG32 reqCode,
        /* [in] */ ULONG32 inBufferSize,
        /* [size_is][in] */ BYTE *inBuffer,
        /* [in] */ ULONG32 outBufferSize,
        /* [size_is][out] */ BYTE *outBuffer);

    virtual HRESULT STDMETHODCALLTYPE SetContext2(
        /* [in] */ ULONG32 flags,
        /* [in] */ ULONG32 contextSize,
        /* [size_is][in] */ BYTE context[  ]);

    HRESULT Init(void);

private:
    void FilterFrames(void);
    void RawGetFrameType(
        /* [out] */ CLRDataSimpleFrameType* simpleType,
        /* [out] */ CLRDataDetailedFrameType* detailedType);

    LONG m_refs;
    ClrDataAccess* m_dac;
    ULONG32 m_instanceAge;
    Thread* m_thread;
    ULONG32 m_walkFlags;
    StackFrameIterator m_frameIter;
    REGDISPLAY m_regDisp;
    T_CONTEXT m_context;
    TADDR m_stackPrev;

    // This is part of a test hook for debugging.  Unless you're code:ClrDataStackWalk::Next
    //  you should never do anything with this member.
    INDEBUG( int m_framesUnwound; )

};

//----------------------------------------------------------------------------
//
// ClrDataFrame.
//
//----------------------------------------------------------------------------

class ClrDataFrame : public IXCLRDataFrame,
                            IXCLRDataFrame2
{
    friend class ClrDataStackWalk;

public:
    ClrDataFrame(ClrDataAccess* dac,
                 CLRDataSimpleFrameType simpleType,
                 CLRDataDetailedFrameType detailedType,
                 AppDomain* appDomain,
                 MethodDesc* methodDesc);
    virtual ~ClrDataFrame(void);

    // IUnknown.
    STDMETHOD(QueryInterface)(THIS_
                              IN REFIID interfaceId,
                              OUT PVOID* iface);
    STDMETHOD_(ULONG, AddRef)(THIS);
    STDMETHOD_(ULONG, Release)(THIS);

    //
    // IXCLRDataFrame.
    //

    virtual HRESULT STDMETHODCALLTYPE GetContext(
        /* [in] */ ULONG32 contextFlags,
        /* [in] */ ULONG32 contextBufSize,
        /* [out] */ ULONG32 *contextSize,
        /* [size_is][out] */ BYTE contextBuf[  ]);

    virtual HRESULT STDMETHODCALLTYPE GetFrameType(
        /* [out] */ CLRDataSimpleFrameType *simpleType,
        /* [out] */ CLRDataDetailedFrameType *detailedType);

    virtual HRESULT STDMETHODCALLTYPE GetAppDomain(
        /* [out] */ IXCLRDataAppDomain **appDomain);

    virtual HRESULT STDMETHODCALLTYPE GetNumArguments(
        /* [out] */ ULONG32 *numParams);

    virtual HRESULT STDMETHODCALLTYPE GetArgumentByIndex(
        /* [in] */ ULONG32 index,
        /* [out] */ IXCLRDataValue **arg,
        /* [in] */ ULONG32 bufLen,
        /* [out] */ ULONG32 *nameLen,
        /* [size_is][out] */ _Out_writes_to_opt_(bufLen, *nameLen) WCHAR name[  ]);

    virtual HRESULT STDMETHODCALLTYPE GetNumLocalVariables(
        /* [out] */ ULONG32 *numLocals);

    virtual HRESULT STDMETHODCALLTYPE GetLocalVariableByIndex(
        /* [in] */ ULONG32 index,
        /* [out] */ IXCLRDataValue **localVariable,
        /* [in] */ ULONG32 bufLen,
        /* [out] */ ULONG32 *nameLen,
        /* [size_is][out] */ _Out_writes_to_opt_(bufLen, *nameLen) WCHAR name[  ]);

    virtual HRESULT STDMETHODCALLTYPE GetNumTypeArguments(
        /* [out] */ ULONG32 *numTypeArgs);

    virtual HRESULT STDMETHODCALLTYPE GetTypeArgumentByIndex(
        /* [in] */ ULONG32 index,
        /* [out] */ IXCLRDataTypeInstance **typeArg);

    virtual HRESULT STDMETHODCALLTYPE GetCodeName(
        /* [in] */ ULONG32 flags,
        /* [in] */ ULONG32 bufLen,
        /* [out] */ ULONG32 *nameLen,
        /* [size_is][out] */ _Out_writes_bytes_opt_(bufLen) WCHAR nameBuf[  ]);

    virtual HRESULT STDMETHODCALLTYPE GetMethodInstance(
        /* [out] */ IXCLRDataMethodInstance **method);

    virtual HRESULT STDMETHODCALLTYPE Request(
        /* [in] */ ULONG32 reqCode,
        /* [in] */ ULONG32 inBufferSize,
        /* [size_is][in] */ BYTE *inBuffer,
        /* [in] */ ULONG32 outBufferSize,
        /* [size_is][out] */ BYTE *outBuffer);

    //
    // IXCLRDataFrame2.
    //

    virtual HRESULT STDMETHODCALLTYPE GetExactGenericArgsToken(
        /* [out] */ IXCLRDataValue ** genericToken);

private:
    HRESULT GetMethodSig(MetaSig** sig,
                         ULONG32* count);
    HRESULT GetLocalSig(MetaSig** sig,
                        ULONG32* count);
    HRESULT ValueFromDebugInfo(MetaSig* sig,
                               bool isArg,
                               DWORD sigIndex,
                               DWORD varInfoSlot,
                               IXCLRDataValue** value);

    LONG m_refs;
    ClrDataAccess* m_dac;
    ULONG32 m_instanceAge;
    CLRDataSimpleFrameType m_simpleType;
    CLRDataDetailedFrameType m_detailedType;
    AppDomain* m_appDomain;
    MethodDesc* m_methodDesc;
    REGDISPLAY m_regDisp;
    T_CONTEXT m_context;
    MetaSig* m_methodSig;
    MetaSig* m_localSig;
};

//----------------------------------------------------------------------------
//
// ClrDataExceptionState.
//
//----------------------------------------------------------------------------

#ifdef FEATURE_EH_FUNCLETS
typedef ExceptionTracker ClrDataExStateType;
#else // FEATURE_EH_FUNCLETS
typedef ExInfo ClrDataExStateType;
#endif // FEATURE_EH_FUNCLETS


class ClrDataExceptionState : public IXCLRDataExceptionState
{
public:
    ClrDataExceptionState(ClrDataAccess* dac,
                          AppDomain* appDomain,
                          Thread* thread,
                          ULONG32 flags,
                          ClrDataExStateType* exInfo,
                          OBJECTHANDLE throwable,
                          ClrDataExStateType* prevExInfo);
    virtual ~ClrDataExceptionState(void);

    // IUnknown.
    STDMETHOD(QueryInterface)(THIS_
                              IN REFIID interfaceId,
                              OUT PVOID* iface);
    STDMETHOD_(ULONG, AddRef)(THIS);
    STDMETHOD_(ULONG, Release)(THIS);

    //
    // IXCLRDataExceptionState.
    //

    virtual HRESULT STDMETHODCALLTYPE GetFlags(
        /* [out] */ ULONG32 *flags);

    virtual HRESULT STDMETHODCALLTYPE GetPrevious(
        /* [out] */ IXCLRDataExceptionState **exState);

    virtual HRESULT STDMETHODCALLTYPE GetManagedObject(
        /* [out] */ IXCLRDataValue **value);

    virtual HRESULT STDMETHODCALLTYPE GetBaseType(
        /* [out] */ CLRDataBaseExceptionType *type);

    virtual HRESULT STDMETHODCALLTYPE GetCode(
        /* [out] */ ULONG32 *code);

    virtual HRESULT STDMETHODCALLTYPE GetString(
        /* [in] */ ULONG32 bufLen,
        /* [out] */ ULONG32 *strLen,
        /* [size_is][out] */ _Out_writes_to_opt_(bufLen, *strLen) WCHAR str[  ]);

    virtual HRESULT STDMETHODCALLTYPE IsSameState(
        /* [in] */ EXCEPTION_RECORD64 *exRecord,
        /* [in] */ ULONG32 contextSize,
        /* [size_is][in] */ BYTE cxRecord[  ]);

    virtual HRESULT STDMETHODCALLTYPE IsSameState2(
        /* [in] */ ULONG32 flags,
        /* [in] */ EXCEPTION_RECORD64 *exRecord,
        /* [in] */ ULONG32 contextSize,
        /* [size_is][in] */ BYTE cxRecord[  ]);

    virtual HRESULT STDMETHODCALLTYPE GetTask(
        /* [out] */ IXCLRDataTask** task);

    virtual HRESULT STDMETHODCALLTYPE Request(
        /* [in] */ ULONG32 reqCode,
        /* [in] */ ULONG32 inBufferSize,
        /* [size_is][in] */ BYTE *inBuffer,
        /* [in] */ ULONG32 outBufferSize,
        /* [size_is][out] */ BYTE *outBuffer);

    static HRESULT NewFromThread(ClrDataAccess* dac,
                                 Thread* thread,
                                 ClrDataExceptionState** exception,
                                 IXCLRDataExceptionState** pubException);

    PTR_CONTEXT          GetCurrentContextRecord();
    PTR_EXCEPTION_RECORD GetCurrentExceptionRecord();

friend class ClrDataAccess;
private:
    LONG m_refs;
    ClrDataAccess* m_dac;
    ULONG32 m_instanceAge;
    AppDomain* m_appDomain;
    Thread* m_thread;
    ULONG32 m_flags;
    ClrDataExStateType* m_exInfo;
    OBJECTHANDLE m_throwable;
    ClrDataExStateType* m_prevExInfo;
};

//----------------------------------------------------------------------------
//
// ClrDataValue.
//
//----------------------------------------------------------------------------

class ClrDataValue : public IXCLRDataValue
{
public:
    ClrDataValue(ClrDataAccess* dac,
                 AppDomain* appDomain,
                 Thread* thread,
                 ULONG32 flags,
                 TypeHandle typeHandle,
                 ULONG64 baseAddr,
                 ULONG32 numLocs,
                 NativeVarLocation* locs);
    virtual ~ClrDataValue(void);

    // IUnknown.
    STDMETHOD(QueryInterface)(THIS_
                              IN REFIID interfaceId,
                              OUT PVOID* iface);
    STDMETHOD_(ULONG, AddRef)(THIS);
    STDMETHOD_(ULONG, Release)(THIS);

    //
    // IXCLRDataValue.
    //

    virtual HRESULT STDMETHODCALLTYPE GetFlags(
        /* [out] */ ULONG32 *flags);

    virtual HRESULT STDMETHODCALLTYPE GetAddress(
        /* [out] */ CLRDATA_ADDRESS *address);

    virtual HRESULT STDMETHODCALLTYPE GetSize(
        /* [out] */ ULONG64 *size);

    virtual HRESULT STDMETHODCALLTYPE GetBytes(
        /* [in] */ ULONG32 bufLen,
        /* [out] */ ULONG32 *dataSize,
        /* [size_is][out] */ BYTE buffer[  ]);

    virtual HRESULT STDMETHODCALLTYPE SetBytes(
        /* [in] */ ULONG32 bufLen,
        /* [out] */ ULONG32 *dataSize,
        /* [size_is][in] */ BYTE buffer[  ]);

    virtual HRESULT STDMETHODCALLTYPE GetType(
        /* [out] */ IXCLRDataTypeInstance **typeInstance);

    virtual HRESULT STDMETHODCALLTYPE GetNumFields(
        /* [out] */ ULONG32 *numFields);

    virtual HRESULT STDMETHODCALLTYPE GetFieldByIndex(
        /* [in] */ ULONG32 index,
        /* [out] */ IXCLRDataValue **field,
        /* [in] */ ULONG32 bufLen,
        /* [out] */ ULONG32 *nameLen,
        /* [size_is][out] */ _Out_writes_to_opt_(bufLen, *nameLen) WCHAR nameBuf[  ],
        /* [out] */ mdFieldDef *token);

    virtual HRESULT STDMETHODCALLTYPE GetNumFields2(
        /* [in] */ ULONG32 flags,
        /* [in] */ IXCLRDataTypeInstance *fromType,
        /* [out] */ ULONG32 *numFields);

    virtual HRESULT STDMETHODCALLTYPE StartEnumFields(
        /* [in] */ ULONG32 flags,
        /* [in] */ IXCLRDataTypeInstance *fromType,
        /* [out] */ CLRDATA_ENUM *handle);

    virtual HRESULT STDMETHODCALLTYPE EnumField(
        /* [out][in] */ CLRDATA_ENUM *handle,
        /* [out] */ IXCLRDataValue **field,
        /* [in] */ ULONG32 nameBufLen,
        /* [out] */ ULONG32 *nameLen,
        /* [size_is][out] */ _Out_writes_to_opt_(nameBufLen, *nameLen) WCHAR nameBuf[  ],
        /* [out] */ mdFieldDef *token);

    virtual HRESULT STDMETHODCALLTYPE EnumField2(
        /* [out][in] */ CLRDATA_ENUM *handle,
        /* [out] */ IXCLRDataValue **field,
        /* [in] */ ULONG32 nameBufLen,
        /* [out] */ ULONG32 *nameLen,
        /* [size_is][out] */ _Out_writes_to_opt_(nameBufLen, *nameLen) WCHAR nameBuf[  ],
        /* [out] */ IXCLRDataModule** tokenScope,
        /* [out] */ mdFieldDef *token);

    virtual HRESULT STDMETHODCALLTYPE EndEnumFields(
        /* [in] */ CLRDATA_ENUM handle);

    virtual HRESULT STDMETHODCALLTYPE StartEnumFieldsByName(
        /* [in] */ LPCWSTR name,
        /* [in] */ ULONG32 nameFlags,
        /* [in] */ ULONG32 fieldFlags,
        /* [in] */ IXCLRDataTypeInstance *fromType,
        /* [out] */ CLRDATA_ENUM *handle);

    virtual HRESULT STDMETHODCALLTYPE EnumFieldByName(
        /* [out][in] */ CLRDATA_ENUM *handle,
        /* [out] */ IXCLRDataValue **field,
        /* [out] */ mdFieldDef *token);

    virtual HRESULT STDMETHODCALLTYPE EnumFieldByName2(
        /* [out][in] */ CLRDATA_ENUM *handle,
        /* [out] */ IXCLRDataValue **field,
        /* [out] */ IXCLRDataModule** tokenScope,
        /* [out] */ mdFieldDef *token);

    virtual HRESULT STDMETHODCALLTYPE EndEnumFieldsByName(
        /* [in] */ CLRDATA_ENUM handle);

    virtual HRESULT STDMETHODCALLTYPE GetFieldByToken(
        /* [in] */ mdFieldDef token,
        /* [out] */ IXCLRDataValue **field,
        /* [in] */ ULONG32 bufLen,
        /* [out] */ ULONG32 *nameLen,
        /* [size_is][out] */ _Out_writes_to_opt_(bufLen, *nameLen) WCHAR nameBuf[  ]);

    virtual HRESULT STDMETHODCALLTYPE GetFieldByToken2(
        /* [in] */ IXCLRDataModule* tokenScope,
        /* [in] */ mdFieldDef token,
        /* [out] */ IXCLRDataValue **field,
        /* [in] */ ULONG32 bufLen,
        /* [out] */ ULONG32 *nameLen,
        /* [size_is][out] */ _Out_writes_to_opt_(bufLen, *nameLen) WCHAR nameBuf[  ]);

    virtual HRESULT STDMETHODCALLTYPE GetAssociatedValue(
        /* [out] */ IXCLRDataValue **assocValue);

    virtual HRESULT STDMETHODCALLTYPE GetAssociatedType(
        /* [out] */ IXCLRDataTypeInstance **assocType);

    virtual HRESULT STDMETHODCALLTYPE GetString(
        /* [in] */ ULONG32 bufLen,
        /* [out] */ ULONG32 *strLen,
        /* [size_is][out] */ _Out_writes_to_opt_(bufLen, *strLen) WCHAR str[  ]);

    virtual HRESULT STDMETHODCALLTYPE GetArrayProperties(
        /* [out] */ ULONG32 *rank,
        /* [out] */ ULONG32 *totalElements,
        /* [in] */ ULONG32 numDim,
        /* [size_is][out] */ ULONG32 dims[  ],
        /* [in] */ ULONG32 numBases,
        /* [size_is][out] */ LONG32 bases[  ]);

    virtual HRESULT STDMETHODCALLTYPE GetArrayElement(
        /* [in] */ ULONG32 numInd,
        /* [size_is][in] */ LONG32 indices[  ],
        /* [out] */ IXCLRDataValue **value);

    virtual HRESULT STDMETHODCALLTYPE GetNumLocations(
        /* [out] */ ULONG32* numLocs);

    virtual HRESULT STDMETHODCALLTYPE GetLocationByIndex(
        /* [in] */ ULONG32 loc,
        /* [out] */ ULONG32* flags,
        /* [out] */ CLRDATA_ADDRESS* arg);

    virtual HRESULT STDMETHODCALLTYPE Request(
        /* [in] */ ULONG32 reqCode,
        /* [in] */ ULONG32 inBufferSize,
        /* [size_is][in] */ BYTE *inBuffer,
        /* [in] */ ULONG32 outBufferSize,
        /* [size_is][out] */ BYTE *outBuffer);

    HRESULT GetRefAssociatedValue(IXCLRDataValue** assocValue);

    static HRESULT NewFromFieldDesc(ClrDataAccess* dac,
                                    AppDomain* appDomain,
                                    ULONG32 flags,
                                    FieldDesc* fieldDesc,
                                    ULONG64 objBase,
                                    Thread* tlsThread,
                                    ClrDataValue** value,
                                    IXCLRDataValue** pubValue,
                                    ULONG32 nameBufRetLen,
                                    ULONG32* nameLenRet,
                                    _Out_writes_to_opt_(nameBufRetLen, *nameLenRet) WCHAR nameBufRet[  ],
                                    IXCLRDataModule** tokenScopeRet,
                                    mdFieldDef* tokenRet);

    HRESULT NewFromSubField(FieldDesc* fieldDesc,
                            ULONG32 flags,
                            ClrDataValue** value,
                            IXCLRDataValue** pubValue,
                            ULONG32 nameBufRetLen,
                            ULONG32* nameLenRet,
                            _Out_writes_to_opt_(nameBufRetLen, *nameLenRet) WCHAR nameBufRet[  ],
                            IXCLRDataModule** tokenScopeRet,
                            mdFieldDef* tokenRet)
    {
        return ClrDataValue::NewFromFieldDesc(m_dac,
                                              m_appDomain,
                                              flags,
                                              fieldDesc,
                                              m_baseAddr,
                                              m_thread,
                                              value,
                                              pubValue,
                                              nameBufRetLen,
                                              nameLenRet,
                                              nameBufRet,
                                              tokenScopeRet,
                                              tokenRet);
    }

    bool CanHaveFields(void)
    {
        return (m_flags & CLRDATA_VALUE_IS_REFERENCE) == 0;
    }

    HRESULT IntGetBytes(
        /* [in] */ ULONG32 bufLen,
        /* [size_is][out] */ BYTE buffer[  ]);

private:
    LONG m_refs;
    ClrDataAccess* m_dac;
    ULONG32 m_instanceAge;
    AppDomain* m_appDomain;
    Thread* m_thread;
    ULONG32 m_flags;
    TypeHandle m_typeHandle;
    ULONG64 m_totalSize;
    ULONG64 m_baseAddr;
    ULONG32 m_numLocs;
    NativeVarLocation m_locs[MAX_NATIVE_VAR_LOCS];
};

//----------------------------------------------------------------------------
//
// EnumMethodDefinitions.
//
//----------------------------------------------------------------------------

class EnumMethodDefinitions
{
public:
    HRESULT Start(Module* mod,
                  bool useAddrFilter,
                  CLRDATA_ADDRESS addrFilter);
    HRESULT Next(ClrDataAccess* dac,
                 IXCLRDataMethodDefinition **method);

    static HRESULT CdStart(Module* mod,
                           bool useAddrFilter,
                           CLRDATA_ADDRESS addrFilter,
                           CLRDATA_ENUM* handle);
    static HRESULT CdNext(ClrDataAccess* dac,
                          CLRDATA_ENUM* handle,
                          IXCLRDataMethodDefinition** method);
    static HRESULT CdEnd(CLRDATA_ENUM handle);

    Module* m_module;
    bool m_useAddrFilter;
    CLRDATA_ADDRESS m_addrFilter;
    MetaEnum m_typeEnum;
    mdToken m_typeToken;
    bool m_needMethodStart;
    MetaEnum m_methodEnum;
};

//----------------------------------------------------------------------------
//
// EnumMethodInstances.
//
//----------------------------------------------------------------------------

class EnumMethodInstances
{
public:
    EnumMethodInstances(MethodDesc* methodDesc,
                        IXCLRDataAppDomain* givenAppDomain);

    HRESULT Next(ClrDataAccess* dac,
                 IXCLRDataMethodInstance **instance);

    static HRESULT CdStart(MethodDesc* methodDesc,
                           IXCLRDataAppDomain* appDomain,
                           CLRDATA_ENUM* handle);
    static HRESULT CdNext(ClrDataAccess* dac,
                          CLRDATA_ENUM* handle,
                          IXCLRDataMethodInstance** method);
    static HRESULT CdEnd(CLRDATA_ENUM handle);

    MethodDesc* m_methodDesc;
    AppDomain* m_givenAppDomain;
    bool m_givenAppDomainUsed;
    AppDomainIterator m_domainIter;
    AppDomain* m_appDomain;
    LoadedMethodDescIterator m_methodIter;
};

//----------------------------------------------------------------------------
//
// Internal functions.
//
//----------------------------------------------------------------------------

#define DAC_ENTER() \
    EnterCriticalSection(&g_dacCritSec); \
    ClrDataAccess* __prevDacImpl = g_dacImpl; \
    g_dacImpl = this;

// When entering a child object we validate that
// the process's host instance cache hasn't been flushed
// since the child was created.
#define DAC_ENTER_SUB(dac) \
    EnterCriticalSection(&g_dacCritSec); \
    if (dac->m_instanceAge != m_instanceAge) \
    { \
        LeaveCriticalSection(&g_dacCritSec); \
        return E_INVALIDARG; \
    } \
    ClrDataAccess* __prevDacImpl = g_dacImpl; \
    g_dacImpl = (dac)

#define DAC_LEAVE() \
    g_dacImpl = __prevDacImpl; \
    LeaveCriticalSection(&g_dacCritSec)


#define SOSHelperEnter() \
    DAC_ENTER_SUB(mDac); \
    HRESULT hr = S_OK;   \
    EX_TRY               \
    {

#define SOSHelperLeave() \
    }                   \
    EX_CATCH            \
    {                   \
        if (!DacExceptionFilter(GET_EXCEPTION(), mDac, &hr)) \
        {               \
            EX_RETHROW; \
        }               \
    }                   \
    EX_END_CATCH(SwallowAllExceptions) \
    DAC_LEAVE();

HRESULT DacGetHostVtPtrs(void);
bool DacExceptionFilter(Exception* ex, ClrDataAccess* process,
                        HRESULT* status);
Thread* __stdcall DacGetThread(ULONG32 osThread);
BOOL DacGetThreadContext(Thread* thread, T_CONTEXT* context);

// Imports from request_svr.cpp, to provide data we need from the SVR namespace
int GCHeapCount();
HRESULT GetServerHeapData(CLRDATA_ADDRESS addr, DacpHeapSegmentData *pSegment);
HRESULT GetServerHeaps(CLRDATA_ADDRESS pGCHeaps[], ICorDebugDataTarget* pTarget);

#if defined(DAC_MEASURE_PERF)

#if defined(TARGET_X86)

// Assume Pentium CPU

#define CCNT_OVERHEAD_32  8
#define CCNT_OVERHEAD    13

#pragma warning( disable: 4035 )        /* Don't complain about lack of return value */

__inline unsigned __int64 GetCycleCount ()
{
__asm   _emit   0x0F
__asm   _emit   0x31    /* rdtsc */
    // return EDX:EAX       causes annoying warning
};

__inline unsigned GetCycleCount32 ()  // enough for about 40 seconds
{
    LIMITED_METHOD_CONTRACT;

__asm   push    EDX
__asm   _emit   0x0F
__asm   _emit   0x31    /* rdtsc */
__asm   pop     EDX
    // return EAX       causes annoying warning
};

#pragma warning( default: 4035 )

#else // #if defined(TARGET_X86)

#define CCNT_OVERHEAD    0 // Don't know

__inline unsigned __int64 GetCycleCount()
{
    LIMITED_METHOD_CONTRACT;

    LARGE_INTEGER qwTmp;
    QueryPerformanceCounter(&qwTmp);
    return qwTmp.QuadPart;
}

#endif  // #if defined(TARGET_X86)

extern unsigned __int64 g_nTotalTime;
extern unsigned __int64 g_nStackTotalTime;
extern unsigned __int64 g_nReadVirtualTotalTime;
extern unsigned __int64 g_nFindTotalTime;
extern unsigned __int64 g_nFindHashTotalTime;
extern unsigned __int64 g_nFindHits;
extern unsigned __int64 g_nFindCalls;
extern unsigned __int64 g_nFindFails;
extern unsigned __int64 g_nStackWalk;
extern unsigned __int64 g_nFindStackTotalTime;

#endif // #if defined(DAC_MEASURE_PERF)

#endif // #ifndef __DACIMPL_H__
