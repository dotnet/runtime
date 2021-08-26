// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ===========================================================================
// File: CEELOAD.H
//

//
// CEELOAD.H defines the class use to represent the PE file
// ===========================================================================

#ifndef CEELOAD_H_
#define CEELOAD_H_

#include "common.h"
#include "vars.hpp" // for LPCUTF8
#include "hash.h"
#include "clsload.hpp"
#include "cgensys.h"
#include "corsym.h"
#include "typehandle.h"
#include "arraylist.h"
#include "pefile.h"
#include "typehash.h"
#include "contractimpl.h"
#include "bitmask.h"
#include "instmethhash.h"
#include "eetwain.h"    // For EnumGCRefs (we should probably move that somewhere else, but can't
                        // find anything better (modulo common or vars.hpp)
#include "classloadlevel.h"
#include "precode.h"
#include "corbbtprof.h"
#include "ilstubcache.h"
#include "classhash.h"

#include "corcompile.h"
#include <gcinfodecoder.h>

#include "wellknownattributes.h"

#ifdef FEATURE_READYTORUN
#include "readytoruninfo.h"
#endif

#include "ilinstrumentation.h"

class PELoader;
class Stub;
class MethodDesc;
class FieldDesc;
class Crst;
class ClassConverter;
class RefClassWriter;
class ReflectionModule;
class EEStringData;
class MethodDescChunk;
class SigTypeContext;
class Assembly;
class BaseDomain;
class AppDomain;
class DomainModule;
struct DomainLocalModule;
class SystemDomain;
class Module;
class SString;
class Pending;
class MethodTable;
class AppDomain;
class DynamicMethodTable;
class CodeVersionManager;
class TieredCompilationManager;
class ProfileEmitter;
class JITInlineTrackingMap;

// Hash table parameter of available classes (name -> module/class) hash
#define AVAILABLE_CLASSES_HASH_BUCKETS 1024
#define AVAILABLE_CLASSES_HASH_BUCKETS_COLLECTIBLE 128
#define PARAMTYPES_HASH_BUCKETS 23
#define PARAMMETHODS_HASH_BUCKETS 11
#define METHOD_STUBS_HASH_BUCKETS 11
#define GUID_TO_TYPE_HASH_BUCKETS 16

// The native symbol reader dll name
#if defined(HOST_AMD64)
#define NATIVE_SYMBOL_READER_DLL W("Microsoft.DiaSymReader.Native.amd64.dll")
#elif defined(HOST_X86)
#define NATIVE_SYMBOL_READER_DLL W("Microsoft.DiaSymReader.Native.x86.dll")
#elif defined(HOST_ARM)
#define NATIVE_SYMBOL_READER_DLL W("Microsoft.DiaSymReader.Native.arm.dll")
#elif defined(HOST_ARM64)
#define NATIVE_SYMBOL_READER_DLL W("Microsoft.DiaSymReader.Native.arm64.dll")
#endif

typedef DPTR(JITInlineTrackingMap) PTR_JITInlineTrackingMap;

//
// LookupMaps are used to implement RID maps
// It is a linked list of nodes, each handling a successive (and consecutive)
// range of RIDs.
//
// LookupMapBase is non-type safe implementation of the worker methods. LookupMap is type
// safe wrapper around it.
//

typedef DPTR(struct LookupMapBase) PTR_LookupMapBase;

struct LookupMapBase
{
    DPTR(LookupMapBase) pNext;

    ArrayDPTR(TADDR)    pTable;

    // Number of elements in this node (only RIDs less than this value can be present in this node)
    DWORD               dwCount;

    // Set of flags that the map supports writing on top of the data value
    TADDR               supportedFlags;

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags,
                           bool enumThis);
    void ListEnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif // DACCESS_COMPILE

    PTR_TADDR GetIndexPtr(DWORD index)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        _ASSERTE(index < dwCount);
        return dac_cast<PTR_TADDR>(pTable) + index;
    }

    PTR_TADDR GetElementPtr(DWORD rid);
    PTR_TADDR GrowMap(Module * pModule, DWORD rid);

    // Get number of RIDs that this table can store
    DWORD GetSize();

#ifdef _DEBUG
    void DebugGetRidMapOccupancy(DWORD *pdwOccupied, DWORD *pdwSize);
#endif
};

#define NO_MAP_FLAGS ((TADDR)0)

template <typename TYPE>
struct LookupMap : LookupMapBase
{
    static TYPE GetValueAt(PTR_TADDR pValue, TADDR* pFlags, TADDR supportedFlags);

#ifndef DACCESS_COMPILE
    static void SetValueAt(PTR_TADDR pValue, TYPE value, TADDR flags);
#endif // DACCESS_COMPILE

    TYPE GetElement(DWORD rid, TADDR* pFlags);
    void SetElement(DWORD rid, TYPE value, TADDR flags);
    BOOL TrySetElement(DWORD rid, TYPE value, TADDR flags);
    void AddElement(Module * pModule, DWORD rid, TYPE value, TADDR flags);
    void EnsureElementCanBeStored(Module * pModule, DWORD rid);
    DWORD Find(TYPE value, TADDR* flags);


public:

    //
    // Retrieve the value associated with a rid
    //
    TYPE GetElement(DWORD rid)
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;

        return GetElement(rid, NULL);
    }

    TYPE GetElementAndFlags(DWORD rid, TADDR* pFlags)
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;

        _ASSERTE(pFlags != NULL);

        return GetElement(rid, pFlags);
    }

    //
    // Stores an association in a map that has been previously grown to
    // the required size. Will never throw or fail.
    //
    void SetElement(DWORD rid, TYPE value)
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;

        SetElement(rid, value, 0);
    }

    void SetElementWithFlags(DWORD rid, TYPE value, TADDR flags)
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;

        // Validate flags: that they are in the predefined range and that the range does not collide with value
        _ASSERTE((flags & supportedFlags) == flags);
        _ASSERTE((dac_cast<TADDR>(value) & supportedFlags) == 0);

        SetElement(rid, value, flags);
    }

#ifndef DACCESS_COMPILE
    void AddFlag(DWORD rid, TADDR flag)
    {
        WRAPPER_NO_CONTRACT;

        _ASSERTE((flag & supportedFlags) == flag);

        PTR_TADDR pElement = GetElementPtr(rid);
        _ASSERTE(pElement);

        if (!pElement)
        {
            return;
        }

        TADDR existingFlags;
        TYPE existingValue = GetValueAt(pElement, &existingFlags, supportedFlags);
        SetValueAt(pElement, existingValue, existingFlags | flag);
    }
#endif // DACCESS_COMPILE

    //
    // Try to store an association in a map. Will never throw or fail.
    //
    BOOL TrySetElement(DWORD rid, TYPE value)
    {
        WRAPPER_NO_CONTRACT;

        return TrySetElement(rid, value, 0);
    }

    BOOL TrySetElementWithFlags(DWORD rid, TYPE value, TADDR flags)
    {
        WRAPPER_NO_CONTRACT;

        // Validate flags: that they are in the predefined range and that the range does not collide with value
        _ASSERTE((flags & supportedFlags) == flags);
        _ASSERTE((dac_cast<TADDR>(value) & supportedFlags) == 0);

        return TrySetElement(rid, value, flags);
    }

    //
    // Stores an association in a map. Grows the map as necessary.
    //
    void AddElement(Module * pModule, DWORD rid, TYPE value)
    {
        WRAPPER_NO_CONTRACT;

        AddElement(pModule, rid, value, 0);
    }

    void AddElementWithFlags(Module * pModule, DWORD rid, TYPE value, TADDR flags)
    {
        WRAPPER_NO_CONTRACT;

        // Validate flags: that they are in the predefined range and that the range does not collide with value
        _ASSERTE((flags & supportedFlags) == flags);
        _ASSERTE((dac_cast<TADDR>(value) & supportedFlags) == 0);

        AddElement(pModule, rid, value, flags);
    }

    //
    // Find the given value in the table and return its RID
    //
    DWORD Find(TYPE value)
    {
        WRAPPER_NO_CONTRACT;

        return Find(value, NULL);
    }

    DWORD FindWithFlags(TYPE value, TADDR flags)
    {
        WRAPPER_NO_CONTRACT;

        // Validate flags: that they are in the predefined range and that the range does not collide with value
        _ASSERTE((flags & supportedFlags) == flags);
        _ASSERTE((dac_cast<TADDR>(value) & supportedFlags) == 0);

        return Find(value, &flags);
    }

    class Iterator
    {
    public:
        Iterator(LookupMap* map);

        BOOL Next();

        TYPE GetElement()
        {
            WRAPPER_NO_CONTRACT;
            SUPPORTS_DAC;

            return GetElement(NULL);
        }

        TYPE GetElementAndFlags(TADDR* pFlags)
        {
            WRAPPER_NO_CONTRACT;
            SUPPORTS_DAC;

            return GetElement(pFlags);
        }

    private:
        TYPE GetElement(TADDR* pFlags);

        LookupMap* m_map;
        DWORD m_index;
    };
};

// Place holder types for RID maps that store cross-module references

class TypeRef { };
typedef DPTR(class TypeRef) PTR_TypeRef;

class MemberRef { };
typedef DPTR(class MemberRef) PTR_MemberRef;


// flag used to mark member ref pointers to field descriptors in the member ref cache
#define IS_FIELD_MEMBER_REF ((TADDR)0x00000002)


//
// VASigCookies are allocated to encapsulate a varargs call signature.
// A reference to the cookie is embedded in the code stream.  Cookies
// are shared amongst call sites with identical signatures in the same
// module
//

typedef DPTR(struct VASigCookie) PTR_VASigCookie;
typedef DPTR(PTR_VASigCookie) PTR_PTR_VASigCookie;
struct VASigCookie
{
    // The JIT wants knows that the size of the arguments comes first
    // so please keep this field first
    unsigned        sizeOfArgs;             // size of argument list
    Volatile<PCODE> pNDirectILStub;         // will be use if target is NDirect (tag == 0)
    PTR_Module      pModule;
    Signature       signature;
};

//
// VASigCookies are allocated in VASigCookieBlocks to amortize
// allocation cost and allow proper bookkeeping.
//

struct VASigCookieBlock
{
    enum {
#ifdef _DEBUG
        kVASigCookieBlockSize = 2
#else // !_DEBUG
        kVASigCookieBlockSize = 20
#endif // !_DEBUG
    };

    VASigCookieBlock    *m_Next;
    UINT                 m_numcookies;
    VASigCookie          m_cookies[kVASigCookieBlockSize];
};

// This lookup table persists the information about boxed statics into the ngen'ed image
// which allows one to the type static initialization without touching expensive EEClasses. Note
// that since the persisted info is stored at ngen time as opposed to class layout time,
// in jitted scenarios we would still touch EEClasses. This imples that the variables which store
// this info in the EEClasses are still present.

// We used this table to store more data require to run cctors in the past (it explains the name),
// but we are only using it for boxed statics now. Boxed statics are rare. The complexity may not
// be worth the gains. We should consider removing this cache and avoid the complexity.

typedef DPTR(struct ClassCtorInfoEntry) PTR_ClassCtorInfoEntry;
struct ClassCtorInfoEntry
{
    DWORD firstBoxedStaticOffset;
    DWORD firstBoxedStaticMTIndex;
    WORD numBoxedStatics;
    WORD hasFixedAddressVTStatics; // This is WORD avoid padding in the datastructure. It is really bool.
};

#define MODULE_CTOR_ELEMENTS 256
struct ModuleCtorInfo
{
    DWORD                   numElements;
    DWORD                   numLastAllocated;
    DWORD                   numElementsHot;
    DPTR(PTR_MethodTable)   ppMT; // size is numElements
    PTR_ClassCtorInfoEntry  cctorInfoHot;   // size is numElementsHot
    PTR_ClassCtorInfoEntry  cctorInfoCold;  // size is numElements-numElementsHot

    PTR_DWORD               hotHashOffsets;  // Indices to the start of each "hash region" in the hot part of the ppMT array.
    PTR_DWORD               coldHashOffsets; // Indices to the start of each "hash region" in the cold part of the ppMT array.
    DWORD                   numHotHashes;
    DWORD                   numColdHashes;

    ArrayDPTR(PTR_MethodTable) ppHotGCStaticsMTs;            // hot table
    ArrayDPTR(PTR_MethodTable) ppColdGCStaticsMTs;           // cold table

    DWORD                   numHotGCStaticsMTs;
    DWORD                   numColdGCStaticsMTs;

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif

    typedef enum {HOT, COLD} REGION;
    FORCEINLINE DWORD GenerateHash(PTR_MethodTable pMT, REGION region)
    {
        SUPPORTS_DAC;

        DWORD tmp1  = pMT->GetTypeDefRid();
        DWORD tmp2  = pMT->GetNumVirtuals();
        DWORD tmp3  = pMT->GetNumInterfaces();

        tmp1        = (tmp1 << 7) + (tmp1 << 0); // 10000001
        tmp2        = (tmp2 << 6) + (tmp2 << 1); // 01000010
        tmp3        = (tmp3 << 4) + (tmp3 << 3); // 00011000

        tmp1       ^= (tmp1 >> 4);               // 10001001 0001
        tmp2       ^= (tmp2 >> 4);               // 01000110 0010
        tmp3       ^= (tmp3 >> 4);               // 00011001 1000

        DWORD hashVal = tmp1 + tmp2 + tmp3;

        if (region == HOT)
            hashVal     &= (numHotHashes - 1);   // numHotHashes is required to be a power of two
        else
            hashVal     &= (numColdHashes - 1);  // numColdHashes is required to be a power of two

        return hashVal;
    };

    ArrayDPTR(PTR_MethodTable) GetGCStaticMTs(DWORD index);

    PTR_MethodTable GetMT(DWORD i)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return ppMT[i];
    }

};


// For IBC Profiling we collect signature blobs for instantiated types.
// For such instantiated types and methods we create our own ibc token
//
// For instantiated types, there also may be no corresponding type token
// or method token for the instantiated types or method in our module.
// For these cases we create our own ibc token definition that is used
// to refer to these external types and methods.  We have to handle
// external nested types and namespaces and method signatures.
//
//    ParamTypeSpec               = 4,    // Instantiated Type Signature
//    ParamMethodSpec             = 5,    // Instantiated Method Signature
//    ExternalNamespaceDef        = 6,    // External Namespace Token Definition
//    ExternalTypeDef             = 7,    // External Type Token Definition
//    ExternalSignatureDef        = 8,    // External Signature Definition
//    ExternalMethodDef           = 9,    // External Method Token Definition
//
// typedef DPTR(class ProfilingBlobEntry) PTR_ProfilingBlobEntry;
class ProfilingBlobEntry
{
public:
    virtual ~ProfilingBlobEntry() { LIMITED_METHOD_CONTRACT; };
    virtual bool              IsEqual(const ProfilingBlobEntry *  other) const = 0;  // Pure Virtual
    virtual size_t            Hash()        const                              = 0;
    virtual BlobType          kind()        const                              = 0;
    virtual size_t            varSize()     const                              = 0;
    virtual void              newToken()                                       = 0;
    mdToken                   token()       const { LIMITED_METHOD_CONTRACT; return m_token; }

protected:
    mdToken                   m_token;
};

class TypeSpecBlobEntry : public ProfilingBlobEntry
{
public:
    TypeSpecBlobEntry(DWORD _cbSig, PCCOR_SIGNATURE _pSig);

    virtual ~TypeSpecBlobEntry()                  { LIMITED_METHOD_CONTRACT;  delete [] m_pSig; }
    virtual BlobType          kind()        const { LIMITED_METHOD_CONTRACT;  return ParamTypeSpec; }
    virtual size_t            varSize()     const { LIMITED_METHOD_CONTRACT;  return sizeof(COR_SIGNATURE) * m_cbSig; }
    virtual void              newToken()          { LIMITED_METHOD_CONTRACT;  m_token = ++s_lastTypeSpecToken; }
    DWORD                     flags()       const { LIMITED_METHOD_CONTRACT;  return m_flags; }
    DWORD                     cbSig()       const { LIMITED_METHOD_CONTRACT;  return m_cbSig; }
    PCCOR_SIGNATURE           pSig()        const { LIMITED_METHOD_CONTRACT;  return m_pSig;  }
    void                      orFlag(DWORD flag)  { LIMITED_METHOD_CONTRACT;  m_flags |= flag; }
    static size_t             HashInit()          { LIMITED_METHOD_CONTRACT;  return 156437; }

    virtual bool              IsEqual(const ProfilingBlobEntry *  other) const;
    virtual size_t            Hash()        const;

    static const TypeSpecBlobEntry *  FindOrAdd(PTR_Module      pModule,
                                                DWORD           _cbSig,
                                                PCCOR_SIGNATURE _pSig);

private:
    DWORD                     m_flags;
    DWORD                     m_cbSig;
    PCCOR_SIGNATURE           m_pSig;

    static idTypeSpec         s_lastTypeSpecToken;
};

class MethodSpecBlobEntry : public ProfilingBlobEntry
{
public:
    MethodSpecBlobEntry(DWORD _cbSig, PCCOR_SIGNATURE _pSig);

    virtual ~MethodSpecBlobEntry()                { LIMITED_METHOD_CONTRACT;  delete [] m_pSig; }
    virtual BlobType          kind()        const { LIMITED_METHOD_CONTRACT;  return ParamMethodSpec; }
    virtual size_t            varSize()     const { LIMITED_METHOD_CONTRACT;  return sizeof(COR_SIGNATURE) * m_cbSig; }
    virtual void              newToken()          { LIMITED_METHOD_CONTRACT;  m_token = ++s_lastMethodSpecToken; }
    DWORD                     flags()       const { LIMITED_METHOD_CONTRACT;  return m_flags; }
    DWORD                     cbSig()       const { LIMITED_METHOD_CONTRACT;  return m_cbSig; }
    PCCOR_SIGNATURE           pSig()        const { LIMITED_METHOD_CONTRACT;  return m_pSig;  }
    void                      orFlag(DWORD flag)  { LIMITED_METHOD_CONTRACT;  m_flags |= flag; }
    static size_t             HashInit()          { LIMITED_METHOD_CONTRACT;  return 187751; }

    virtual bool              IsEqual(const ProfilingBlobEntry *  other) const;
    virtual size_t            Hash()        const;

    static const MethodSpecBlobEntry *  FindOrAdd(PTR_Module      pModule,
                                                  DWORD           _cbSig,
                                                  PCCOR_SIGNATURE _pSig);

private:
    DWORD                     m_flags;
    DWORD                     m_cbSig;
    PCCOR_SIGNATURE           m_pSig;

    static idTypeSpec  s_lastMethodSpecToken;
};

class ExternalNamespaceBlobEntry : public ProfilingBlobEntry
{
public:
    ExternalNamespaceBlobEntry(LPCSTR _pName);

    virtual ~ExternalNamespaceBlobEntry()         { LIMITED_METHOD_CONTRACT;  delete [] m_pName; }
    virtual BlobType          kind()        const { LIMITED_METHOD_CONTRACT;  return ExternalNamespaceDef; }
    virtual size_t            varSize()     const { LIMITED_METHOD_CONTRACT;  return sizeof(CHAR) * m_cbName; }
    virtual void              newToken()          { LIMITED_METHOD_CONTRACT;  m_token = ++s_lastExternalNamespaceToken; }
    DWORD                     cbName()      const { LIMITED_METHOD_CONTRACT;  return m_cbName; }
    LPCSTR                    pName()       const { LIMITED_METHOD_CONTRACT;  return m_pName;  }
    static size_t             HashInit()          { LIMITED_METHOD_CONTRACT;  return 225307; }

    virtual bool              IsEqual(const ProfilingBlobEntry *  other) const;
    virtual size_t            Hash()        const;

    static const ExternalNamespaceBlobEntry *  FindOrAdd(PTR_Module pModule, LPCSTR _pName);

private:
    DWORD                     m_cbName;
    LPCSTR                    m_pName;

    static idExternalNamespace s_lastExternalNamespaceToken;
};

class ExternalTypeBlobEntry : public ProfilingBlobEntry
{
public:
    ExternalTypeBlobEntry(mdToken _assemblyRef,  mdToken _nestedClass,
                          mdToken _nameSpace,    LPCSTR  _pName);

    virtual ~ExternalTypeBlobEntry()              { LIMITED_METHOD_CONTRACT;  delete [] m_pName; }
    virtual BlobType          kind()        const { LIMITED_METHOD_CONTRACT;  return ExternalTypeDef; }
    virtual size_t            varSize()     const { LIMITED_METHOD_CONTRACT;  return sizeof(CHAR) * m_cbName; }
    virtual void              newToken()          { LIMITED_METHOD_CONTRACT;  m_token = ++s_lastExternalTypeToken; }
    mdToken                   assemblyRef() const { LIMITED_METHOD_CONTRACT;  return m_assemblyRef; }
    mdToken                   nestedClass() const { LIMITED_METHOD_CONTRACT;  return m_nestedClass; }
    mdToken                   nameSpace()   const { LIMITED_METHOD_CONTRACT;  return m_nameSpace; }
    DWORD                     cbName()      const { LIMITED_METHOD_CONTRACT;  return m_cbName; }
    LPCSTR                    pName()       const { LIMITED_METHOD_CONTRACT;  return m_pName;  }
    static size_t             HashInit()          { LIMITED_METHOD_CONTRACT;  return 270371; }

    virtual bool              IsEqual(const ProfilingBlobEntry *  other) const;
    virtual size_t            Hash()        const;

    static const ExternalTypeBlobEntry *  FindOrAdd(PTR_Module pModule,
                                                    mdToken    _assemblyRef,
                                                    mdToken    _nestedClass,
                                                    mdToken    _nameSpace,
                                                    LPCSTR     _pName);

private:
    mdToken                   m_assemblyRef;
    mdToken                   m_nestedClass;
    mdToken                   m_nameSpace;
    DWORD                     m_cbName;
    LPCSTR                    m_pName;

    static idExternalType     s_lastExternalTypeToken;
};

class ExternalSignatureBlobEntry : public ProfilingBlobEntry
{
public:
    ExternalSignatureBlobEntry(DWORD _cbSig, PCCOR_SIGNATURE _pSig);

    virtual ~ExternalSignatureBlobEntry()         { LIMITED_METHOD_CONTRACT;  delete [] m_pSig; }
    virtual BlobType          kind()        const { LIMITED_METHOD_CONTRACT;  return ExternalSignatureDef; }
    virtual size_t            varSize()     const { LIMITED_METHOD_CONTRACT;  return sizeof(COR_SIGNATURE) * m_cbSig; }
    virtual void              newToken()          { LIMITED_METHOD_CONTRACT;  m_token = ++s_lastExternalSignatureToken; }
    DWORD                     cbSig()       const { LIMITED_METHOD_CONTRACT;  return m_cbSig; }
    PCCOR_SIGNATURE           pSig()        const { LIMITED_METHOD_CONTRACT;  return m_pSig;  }
    static size_t             HashInit()          { LIMITED_METHOD_CONTRACT;  return 324449; }

    virtual bool              IsEqual(const ProfilingBlobEntry *  other) const;
    virtual size_t            Hash()        const;

    static const ExternalSignatureBlobEntry *  FindOrAdd(PTR_Module      pModule,
                                                         DWORD           _cbSig,
                                                         PCCOR_SIGNATURE _pSig);

private:
    DWORD                     m_cbSig;
    PCCOR_SIGNATURE           m_pSig;

    static idExternalSignature s_lastExternalSignatureToken;
};

class ExternalMethodBlobEntry : public ProfilingBlobEntry
{
public:
    ExternalMethodBlobEntry(mdToken _nestedClass, mdToken _signature, LPCSTR _pName);

    virtual ~ExternalMethodBlobEntry()            { LIMITED_METHOD_CONTRACT;  delete [] m_pName; }
    virtual BlobType          kind()        const { LIMITED_METHOD_CONTRACT;  return ExternalMethodDef; }
    virtual size_t            varSize()     const { LIMITED_METHOD_CONTRACT;  return sizeof(CHAR) * m_cbName; }
    virtual void              newToken()          { LIMITED_METHOD_CONTRACT;  m_token = ++s_lastExternalMethodToken; }
    mdToken                   nestedClass() const { LIMITED_METHOD_CONTRACT;  return m_nestedClass; }
    mdToken                   signature()   const { LIMITED_METHOD_CONTRACT;  return m_signature; }
    DWORD                     cbName()      const { LIMITED_METHOD_CONTRACT;  return m_cbName; }
    LPCSTR                    pName()       const { LIMITED_METHOD_CONTRACT;  return m_pName;  }
    static size_t             HashInit()          { LIMITED_METHOD_CONTRACT;  return 389357; }

    virtual bool              IsEqual(const ProfilingBlobEntry *  other) const;
    virtual size_t            Hash()        const;

    static const ExternalMethodBlobEntry *  FindOrAdd(PTR_Module pModule,
                                                      mdToken    _nestedClass,
                                                      mdToken    _signature,
                                                      LPCSTR     _pName);

private:
    mdToken                   m_nestedClass;
    mdToken                   m_signature;
    DWORD                     m_cbName;
    LPCSTR                    m_pName;

    static idExternalMethod   s_lastExternalMethodToken;
};

struct IbcNameHandle
{
    mdToken  tkIbcNameSpace;
    mdToken  tkIbcNestedClass;

    LPCSTR   szName;
    LPCSTR   szNamespace;
    mdToken  tkEnclosingClass;
};

//
// Hashtable of ProfilingBlobEntry *
//
class ProfilingBlobTraits : public NoRemoveSHashTraits<DefaultSHashTraits<ProfilingBlobEntry *> >
{
public:
    typedef ProfilingBlobEntry *  key_t;

    static key_t GetKey(element_t e)
    {
        LIMITED_METHOD_CONTRACT;
        return e;
    }
    static BOOL Equals(key_t k1, key_t k2)
    {
        LIMITED_METHOD_CONTRACT;
        return k1->IsEqual(k2);
    }
    static count_t Hash(key_t k)
    {
        LIMITED_METHOD_CONTRACT;
        return (count_t) k->Hash();
    }
    static element_t Null()
    {
        LIMITED_METHOD_CONTRACT;
        return NULL;
    }

    static bool IsNull(const element_t &e)
    {
        LIMITED_METHOD_CONTRACT;
        return (e == NULL);
    }
};

typedef SHash<ProfilingBlobTraits> ProfilingBlobTable;
typedef DPTR(ProfilingBlobTable) PTR_ProfilingBlobTable;

//
// A Module is the primary unit of code packaging in the runtime.  It
// corresponds mostly to an OS executable image, although other kinds
// of modules exist.
//
class UMEntryThunk;

// Hashtable of absolute addresses of IL blobs for dynamics, keyed by token

 struct  DynamicILBlobEntry
{
    mdToken     m_methodToken;
    TADDR       m_il;
};

class DynamicILBlobTraits : public NoRemoveSHashTraits<DefaultSHashTraits<DynamicILBlobEntry> >
{
public:
    typedef mdToken key_t;

    static key_t GetKey(element_t e)
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        return e.m_methodToken;
    }
    static BOOL Equals(key_t k1, key_t k2)
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        return k1 == k2;
    }
    static count_t Hash(key_t k)
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        return (count_t)(size_t)k;
    }
    static const element_t Null()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        DynamicILBlobEntry e;
        e.m_il = TADDR(0);
        e.m_methodToken = 0;
        return e;
    }
    static bool IsNull(const element_t &e)
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        return e.m_methodToken == 0;
    }
};

typedef SHash<DynamicILBlobTraits> DynamicILBlobTable;
typedef DPTR(DynamicILBlobTable) PTR_DynamicILBlobTable;


#ifdef FEATURE_COMINTEROP

//---------------------------------------------------------------------------------------
//
// The type of each entry in the Guid to MT hash
//
typedef DPTR(GUID) PTR_GUID;
typedef DPTR(struct GuidToMethodTableEntry) PTR_GuidToMethodTableEntry;
struct GuidToMethodTableEntry
{
    PTR_GUID        m_Guid;
    PTR_MethodTable m_pMT;
};

//---------------------------------------------------------------------------------------
//
// The hash type itself
//
typedef DPTR(class GuidToMethodTableHashTable) PTR_GuidToMethodTableHashTable;
class GuidToMethodTableHashTable : public NgenHashTable<GuidToMethodTableHashTable, GuidToMethodTableEntry, 4>
{
public:
    typedef NgenHashTable<GuidToMethodTableHashTable, GuidToMethodTableEntry, 4> Base_t;
    friend class Base_t;

#ifndef DACCESS_COMPILE

private:
    GuidToMethodTableHashTable(Module *pModule, LoaderHeap *pHeap, DWORD cInitialBuckets)
        : Base_t(pModule, pHeap, cInitialBuckets)
    { LIMITED_METHOD_CONTRACT; }

public:
    static GuidToMethodTableHashTable* Create(Module* pModule, DWORD cInitialBuckets, AllocMemTracker *pamTracker);

    GuidToMethodTableEntry * InsertValue(PTR_GUID pGuid, PTR_MethodTable pMT, BOOL bReplaceIfFound, AllocMemTracker *pamTracker);

#endif // !DACCESS_COMPILE

public:
    typedef Base_t::LookupContext LookupContext;

    PTR_MethodTable GetValue(const GUID * pGuid, LookupContext *pContext);
    GuidToMethodTableEntry * FindItem(const GUID * pGuid, LookupContext *pContext);

private:
    BOOL CompareKeys(PTR_GuidToMethodTableEntry pEntry, const GUID * pGuid);
    static DWORD Hash(const GUID * pGuid);

public:
    // An iterator for the table
    struct Iterator
    {
    public:
        Iterator() : m_pTable(NULL), m_fIterating(false)
        { LIMITED_METHOD_DAC_CONTRACT; }
        Iterator(GuidToMethodTableHashTable * pTable) : m_pTable(pTable), m_fIterating(false)
        { LIMITED_METHOD_DAC_CONTRACT; }

    private:
        friend class GuidToMethodTableHashTable;

        GuidToMethodTableHashTable * m_pTable;
        BaseIterator              m_sIterator;
        bool                      m_fIterating;
    };

    BOOL FindNext(Iterator *it, GuidToMethodTableEntry **ppEntry);
    DWORD GetCount();

#ifdef DACCESS_COMPILE
    // do not save this in mini-/heap-dumps
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
    { SUPPORTS_DAC; }
    void EnumMemoryRegionsForEntry(GuidToMethodTableEntry *pEntry, CLRDataEnumMemoryFlags flags)
    { SUPPORTS_DAC; }
#endif // DACCESS_COMPILE
};

#endif // FEATURE_COMINTEROP


//Hash for MemberRef to Desc tables (fieldDesc or MethodDesc)
typedef DPTR(struct MemberRefToDescHashEntry) PTR_MemberRefToDescHashEntry;

struct MemberRefToDescHashEntry
{
    TADDR m_value;
};

typedef DPTR(class MemberRefToDescHashTable) PTR_MemberRefToDescHashTable;

#define MEMBERREF_MAP_INITIAL_SIZE 10

class MemberRefToDescHashTable: public NgenHashTable<MemberRefToDescHashTable, MemberRefToDescHashEntry, 2>
{
	friend class NgenHashTable<MemberRefToDescHashTable, MemberRefToDescHashEntry, 2>;
#ifndef DACCESS_COMPILE

private:
    MemberRefToDescHashTable(Module *pModule, LoaderHeap *pHeap, DWORD cInitialBuckets):
       NgenHashTable<MemberRefToDescHashTable, MemberRefToDescHashEntry, 2>(pModule, pHeap, cInitialBuckets)
    { LIMITED_METHOD_CONTRACT; }

public:

    static MemberRefToDescHashTable* Create(Module *pModule, DWORD cInitialBuckets, AllocMemTracker *pamTracker);

    MemberRefToDescHashEntry* Insert(mdMemberRef token, MethodDesc *value);
    MemberRefToDescHashEntry* Insert(mdMemberRef token , FieldDesc *value);
#endif //!DACCESS_COMPILE

public:
    typedef NgenHashTable<MemberRefToDescHashTable, MemberRefToDescHashEntry, 2>::LookupContext LookupContext;

    PTR_MemberRef GetValue(mdMemberRef token, BOOL *pfIsMethod);

#ifdef DACCESS_COMPILE

    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
    {
        WRAPPER_NO_CONTRACT;
        BaseEnumMemoryRegions(flags);
    }

    void EnumMemoryRegionsForEntry(MemberRefToDescHashEntry *pEntry, CLRDataEnumMemoryFlags flags)
    { SUPPORTS_DAC; }

#endif
};

#ifdef FEATURE_READYTORUN
typedef DPTR(class ReadyToRunInfo)      PTR_ReadyToRunInfo;
#endif

struct ThreadLocalModule;

// A code:Module represents a DLL or EXE file loaded from the disk. It could either be a IL module or a
// Native code (NGEN module). A module live in a code:Assembly
//
// Some important fields are
//    * code:Module.m_file - this points at a code:PEFile that understands the layout of a PE file. The most
//        important part is getting at the code:Module (see file:..\inc\corhdr.h#ManagedHeader) from there
//        you can get at the Meta-data and IL)
//    * code:Module.m_pAvailableClasses - this is a table that lets you look up the types (the code:EEClass)
//        for all the types in the module
//
// See file:..\inc\corhdr.h#ManagedHeader for more on the layout of managed exectuable files.

class Module
{
#ifdef DACCESS_COMPILE
    friend class ClrDataAccess;
    friend class NativeImageDumper;
#endif

    friend class DataImage;

    VPTR_BASE_CONCRETE_VTABLE_CLASS(Module)

private:
    PTR_CUTF8               m_pSimpleName; // Cached simple name for better performance and easier diagnostics

    PTR_PEFile              m_file;

    enum {
        // These are the values set in m_dwTransientFlags.
        // Note that none of these flags survive a prejit save/restore.

        MODULE_IS_TENURED           = 0x00000001,   // Set once we know for sure the Module will not be freed until the appdomain itself exits
        // unused                   = 0x00000002,
        CLASSES_FREED               = 0x00000004,
        IS_EDIT_AND_CONTINUE        = 0x00000008,   // is EnC Enabled for this module

        IS_PROFILER_NOTIFIED        = 0x00000010,
        IS_ETW_NOTIFIED             = 0x00000020,

        //
        // Note: the order of these must match the order defined in
        // cordbpriv.h for DebuggerAssemblyControlFlags. The three
        // values below should match the values defined in
        // DebuggerAssemblyControlFlags when shifted right
        // DEBUGGER_INFO_SHIFT bits.
        //
        DEBUGGER_USER_OVERRIDE_PRIV = 0x00000400,
        DEBUGGER_ALLOW_JIT_OPTS_PRIV= 0x00000800,
        DEBUGGER_TRACK_JIT_INFO_PRIV= 0x00001000,
        DEBUGGER_ENC_ENABLED_PRIV   = 0x00002000,   // this is what was attempted to be set.  IS_EDIT_AND_CONTINUE is actual result.
        DEBUGGER_PDBS_COPIED        = 0x00004000,
        DEBUGGER_IGNORE_PDBS        = 0x00008000,
        DEBUGGER_INFO_MASK_PRIV     = 0x0000Fc00,
        DEBUGGER_INFO_SHIFT_PRIV    = 10,

        // Used to indicate that this module has had it's IJW fixups properly installed.
        IS_IJW_FIXED_UP             = 0x00080000,
        IS_BEING_UNLOADED           = 0x00100000,

        // Used to indicate that the module is loaded sufficiently for generic candidate instantiations to work
        MODULE_READY_FOR_TYPELOAD  = 0x00200000,

        // Used during NGen only
        TYPESPECS_TRIAGED           = 0x40000000,
        MODULE_SAVED                = 0x80000000,
    };

    enum {
        // These are the values set in m_dwPersistedFlags.  These will survive
        // a prejit save/restore
        // unused                   = 0x00000001,
        COMPUTED_GLOBAL_CLASS       = 0x00000002,

        // This flag applies to assembly, but it is stored so it can be cached in ngen image
        COMPUTED_STRING_INTERNING   = 0x00000004,
        NO_STRING_INTERNING         = 0x00000008,

        // This flag applies to assembly, but it is stored so it can be cached in ngen image
        COMPUTED_WRAP_EXCEPTIONS    = 0x00000010,
        WRAP_EXCEPTIONS             = 0x00000020,

        // This flag applies to assembly, but it is stored so it can be cached in ngen image
        COMPUTED_RELIABILITY_CONTRACT=0x00000040,

        // This flag applies to assembly, but is also stored here so that it can be cached in ngen image
        COLLECTIBLE_MODULE          = 0x00000080,

        // Caches metadata version
        COMPUTED_IS_PRE_V4_ASSEMBLY = 0x00000100,
        IS_PRE_V4_ASSEMBLY          = 0x00000200,

        //If attribute value has been cached before
        DEFAULT_DLL_IMPORT_SEARCH_PATHS_IS_CACHED   = 0x00000400,

        //If module has default dll import search paths attribute
        DEFAULT_DLL_IMPORT_SEARCH_PATHS_STATUS      = 0x00000800,

        //If m_MethodDefToPropertyInfoMap has been generated
        COMPUTED_METHODDEF_TO_PROPERTYINFO_MAP = 0x00002000,

        // Low level system assembly. Used by preferred zap module computation.
        LOW_LEVEL_SYSTEM_ASSEMBLY_BY_NAME = 0x00004000,
    };

    Volatile<DWORD>          m_dwTransientFlags;
    Volatile<DWORD>          m_dwPersistedFlags;

    // Linked list of VASig cookie blocks: protected by m_pStubListCrst
    VASigCookieBlock        *m_pVASigCookieBlock;

    PTR_Assembly            m_pAssembly;
    mdFile                  m_moduleRef;

    CrstExplicitInit        m_Crst;
    CrstExplicitInit        m_FixupCrst;

    // Debugging symbols reader interface. This will only be
    // initialized if needed, either by the debugging subsystem or for
    // an exception.
    ISymUnmanagedReader *   m_pISymUnmanagedReader;

    // The reader lock is used to serialize all creation of symbol readers.
    // It does NOT seralize all access to the readers since we freely give
    // out references to the reader outside this class.  Instead, once a
    // reader object is created, it is entirely read-only and so thread-safe.
    CrstExplicitInit        m_ISymUnmanagedReaderCrst;

    // Storage for the in-memory symbol stream if any
    // Debugger may retrieve this from out-of-process.
    PTR_CGrowableStream     m_pIStreamSym;

    // For protecting additions to the heap
    CrstExplicitInit        m_LookupTableCrst;

    #define TYPE_DEF_MAP_ALL_FLAGS                    NO_MAP_FLAGS

    #define TYPE_REF_MAP_ALL_FLAGS                    NO_MAP_FLAGS
        // For type ref map, 0x1 cannot be used as a flag: reserved for FIXUP_POINTER_INDIRECTION bit
        // For type ref map, 0x2 cannot be used as a flag: reserved for TypeHandle to signify TypeDesc

    #define METHOD_DEF_MAP_ALL_FLAGS                  NO_MAP_FLAGS

    #define FIELD_DEF_MAP_ALL_FLAGS                   NO_MAP_FLAGS

    #define MEMBER_REF_MAP_ALL_FLAGS                  ((TADDR)0x00000003)
	// For member ref hash table, 0x1 is reserved for IsHot bit
        #define IS_FIELD_MEMBER_REF                   ((TADDR)0x00000002)      // denotes that target is a FieldDesc

    #define GENERIC_PARAM_MAP_ALL_FLAGS               NO_MAP_FLAGS

    #define GENERIC_TYPE_DEF_MAP_ALL_FLAGS            NO_MAP_FLAGS

    #define FILE_REF_MAP_ALL_FLAGS                    NO_MAP_FLAGS
        // For file ref map, 0x1 cannot be used as a flag: reserved for FIXUP_POINTER_INDIRECTION bit

    #define MANIFEST_MODULE_MAP_ALL_FLAGS             NO_MAP_FLAGS
        // For manifest module map, 0x1 cannot be used as a flag: reserved for FIXUP_POINTER_INDIRECTION bit

    #define PROPERTY_INFO_MAP_ALL_FLAGS               NO_MAP_FLAGS

    // Linear mapping from TypeDef token to MethodTable *
    // For generic types, IsGenericTypeDefinition() is true i.e. instantiation at formals
    LookupMap<PTR_MethodTable>      m_TypeDefToMethodTableMap;

    // Linear mapping from TypeRef token to TypeHandle *
    LookupMap<PTR_TypeRef>          m_TypeRefToMethodTableMap;

    // Linear mapping from MethodDef token to MethodDesc *
    // For generic methods, IsGenericTypeDefinition() is true i.e. instantiation at formals
    LookupMap<PTR_MethodDesc>       m_MethodDefToDescMap;

    // Linear mapping from FieldDef token to FieldDesc*
    LookupMap<PTR_FieldDesc>        m_FieldDefToDescMap;

    // mapping from MemberRef token to MethodDesc*, FieldDesc*
    PTR_MemberRefToDescHashTable        m_pMemberRefToDescHashTable;

    // Linear mapping from GenericParam token to TypeVarTypeDesc*
    LookupMap<PTR_TypeVarTypeDesc>  m_GenericParamToDescMap;

    // Linear mapping from TypeDef token to the MethodTable * for its canonical generic instantiation
    // If the type is not generic, the entry is guaranteed to be NULL.  This means we are paying extra
    // space in order to use the LookupMap infrastructure, but what it buys us is IBC support and
    // a compressed format for NGen that makes up for it.
    LookupMap<PTR_MethodTable>      m_GenericTypeDefToCanonMethodTableMap;

    // Mapping from File token to Module *
    LookupMap<PTR_Module>           m_FileReferencesMap;

    // Mapping of AssemblyRef token to Module *
    LookupMap<PTR_Module>           m_ManifestModuleReferencesMap;

    // Mapping from MethodDef token to pointer-sized value encoding property information
    LookupMap<SIZE_T>           m_MethodDefToPropertyInfoMap;

    // IL stub cache with fabricated MethodTable parented by this module.
    ILStubCache                *m_pILStubCache;

    ULONG m_DefaultDllImportSearchPathsAttributeValue;
public:
    LookupMap<PTR_MethodTable>::Iterator EnumerateTypeDefs()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;

        return LookupMap<PTR_MethodTable>::Iterator(&m_TypeDefToMethodTableMap);
    }

    // Hash of available types by name
    PTR_EEClassHashTable    m_pAvailableClasses;

    // Hashtable of generic type instances
    PTR_EETypeHashTable     m_pAvailableParamTypes;

    // For protecting additions to m_pInstMethodHashTable
    CrstExplicitInit        m_InstMethodHashTableCrst;

    // Hashtable of instantiated methods and per-instantiation static methods
    PTR_InstMethodHashTable m_pInstMethodHashTable;

    // This is used by the Debugger. We need to store a dword
    // for a count of JMC functions. This is a count, not a pointer.
    // We'll pass the address of this field
    // off to the jit, which will include it in probes injected for
    // debuggable code.
    // This means we need the dword at the time a function is jitted.
    // The Debugger has its own module structure, but those aren't created
    // if a debugger isn't attached.
    // We put it here instead of in the debugger's module because:
    // 1) we need a module structure that's around even when the debugger
    // isn't attached... so we use the EE's module.
    // 2) Needs to be here for ngen
    DWORD                   m_dwDebuggerJMCProbeCount;

    // We can skip the JMC probes if we know that a module has no JMC stuff
    // inside. So keep a strict count of all functions inside us.
    bool HasAnyJMCFunctions();
    void IncJMCFuncCount();
    void DecJMCFuncCount();

    // Get and set the default JMC status of this module.
    bool GetJMCStatus();
    void SetJMCStatus(bool fStatus);

    // If this is a dynamic module, eagerly serialize the metadata so that it is available for DAC.
    // This is a nop for non-dynamic modules.
    void UpdateDynamicMetadataIfNeeded();

#ifdef _DEBUG
    //
    // We call these methods to seal/unseal the
    // lists: m_pAvailableClasses and m_pAvailableParamTypes
    //
    // When they are sealed ClassLoader::PublishType cannot
    // add new generic types or methods
    //
    void SealGenericTypesAndMethods();
    void UnsealGenericTypesAndMethods();
#endif

private:
    // Set the given bit on m_dwTransientFlags. Return true if we won the race to set the bit.
    BOOL SetTransientFlagInterlocked(DWORD dwFlag);

    // Invoke fusion hooks into host to fetch PDBs
    void FetchPdbsFromHost();

    // Cannoically-cased hashtable of the available class names for
    // case insensitive lookup.  Contains pointers into
    // m_pAvailableClasses.
    PTR_EEClassHashTable    m_pAvailableClassesCaseIns;

    // Pointer to binder, if we have one
    friend class CoreLibBinder;
    PTR_CoreLibBinder      m_pBinder;

public:
    BOOL IsCollectible()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return (m_dwPersistedFlags & COLLECTIBLE_MODULE) != 0;
    }

#ifdef FEATURE_READYTORUN
private:
    PTR_ReadyToRunInfo      m_pReadyToRunInfo;
    PTR_NativeImage         m_pNativeImage;
#endif

private:
    PTR_ProfilingBlobTable  m_pProfilingBlobTable;   // While performing IBC instrumenting this hashtable is populated with the External defs
    CorProfileData *        m_pProfileData;          // While ngen-ing with IBC optimizations this contains a link to the IBC data for the assembly

    // Profile information
    BOOL                            m_nativeImageProfiling;
    CORCOMPILE_METHOD_PROFILE_LIST *m_methodProfileList;

#if PROFILING_SUPPORTED_DATA
    DWORD                   m_dwTypeCount;
    DWORD                   m_dwExportedTypeCount;
    DWORD                   m_dwCustomAttributeCount;
#endif // PROFILING_SUPPORTED_DATA

    struct TokenProfileData
    {
        static TokenProfileData *CreateNoThrow(void);

        TokenProfileData()
            // We need a critical section that can be entered in both preemptive and cooperative modes.
            // Hopefully this restriction can be removed in the future.
            : crst(CrstSaveModuleProfileData, CRST_UNSAFE_ANYMODE)
        {
            WRAPPER_NO_CONTRACT;
        }

        ~TokenProfileData()
        {
            WRAPPER_NO_CONTRACT;
        }

        Crst crst;

        struct Formats
        {
            CQuickArray<CORBBTPROF_TOKEN_INFO>   tokenArray;
            RidBitmap                   tokenBitmaps[CORBBTPROF_TOKEN_MAX_NUM_FLAGS];
        } m_formats[SectionFormatCount];

    } *m_tokenProfileData;


protected:

    void CreateDomainThunks();

protected:
    void DoInit(AllocMemTracker *pamTracker, LPCWSTR szName);

protected:
#ifndef DACCESS_COMPILE
    virtual void Initialize(AllocMemTracker *pamTracker, LPCWSTR szName = NULL);
    void InitializeForProfiling();
#endif

    void AllocateMaps();

#ifdef _DEBUG
    void DebugLogRidMapOccupancy();
#endif // _DEBUG

    static HRESULT VerifyFile(PEFile *file, BOOL fZap);

 public:
    static Module *Create(Assembly *pAssembly, mdFile kFile, PEFile *pFile, AllocMemTracker *pamTracker);

 protected:
    Module(Assembly *pAssembly, mdFile moduleRef, PEFile *file);


 public:
#ifndef DACCESS_COMPILE
    virtual void Destruct();
#endif

    PTR_LoaderAllocator GetLoaderAllocator();

    PTR_PEFile GetFile() const { LIMITED_METHOD_DAC_CONTRACT; return m_file; }

    static size_t GetFileOffset() { LIMITED_METHOD_CONTRACT; return offsetof(Module, m_file); }

    BOOL IsManifest();

    void ApplyMetaData();

    void FixupVTables();

    void FreeClassTables();

#ifdef DACCESS_COMPILE
    virtual void EnumMemoryRegions(CLRDataEnumMemoryFlags flags,
                                   bool enumThis);
#endif // DACCESS_COMPILE

    ReflectionModule *GetReflectionModule() const
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;

        _ASSERTE(IsReflection());
        return dac_cast<PTR_ReflectionModule>(this);
    }

    PTR_Assembly GetAssembly() const;

    int GetClassLoaderIndex()
    {
        LIMITED_METHOD_CONTRACT;

        return RidFromToken(m_moduleRef);
    }

    MethodTable *GetGlobalMethodTable();
    bool         NeedsGlobalMethodTable();

    // This works for manifest modules too
    DomainFile *GetDomainFile();

    // Operates on assembly of module
    DomainAssembly *GetDomainAssembly();

    void SetDomainFile(DomainFile *pDomainFile);

    OBJECTREF GetExposedObject();

    ClassLoader *GetClassLoader();
    PTR_BaseDomain GetDomain();
#ifdef FEATURE_CODE_VERSIONING
    CodeVersionManager * GetCodeVersionManager();
#endif

    mdFile GetModuleRef()
    {
        LIMITED_METHOD_CONTRACT;

        return m_moduleRef;
    }

    BOOL IsResource() const { WRAPPER_NO_CONTRACT; SUPPORTS_DAC; return GetFile()->IsResource(); }
    BOOL IsPEFile() const { WRAPPER_NO_CONTRACT; return !GetFile()->IsDynamic(); }
    BOOL IsReflection() const { WRAPPER_NO_CONTRACT; SUPPORTS_DAC; return GetFile()->IsDynamic(); }
    BOOL IsIbcOptimized() const { WRAPPER_NO_CONTRACT; return GetFile()->IsIbcOptimized(); }
    // Returns true iff the debugger can see this module.
    BOOL IsVisibleToDebugger();

    BOOL IsEditAndContinueEnabled()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        _ASSERTE((m_dwTransientFlags & IS_EDIT_AND_CONTINUE) == 0 || IsEditAndContinueCapable());
        return (m_dwTransientFlags & IS_EDIT_AND_CONTINUE) != 0;
    }

    virtual BOOL IsEditAndContinueCapable() const { return FALSE; }

    BOOL IsIStream() { LIMITED_METHOD_CONTRACT; return GetFile()->IsIStream(); }

    BOOL IsSystem() { WRAPPER_NO_CONTRACT; SUPPORTS_DAC; return m_file->IsSystem(); }

    static BOOL IsEditAndContinueCapable(Assembly *pAssembly, PEFile *file);

    void EnableEditAndContinue()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        _ASSERTE(IsEditAndContinueCapable());
        LOG((LF_ENC, LL_INFO100, "EnableEditAndContinue: this:0x%x, %s\n", this, GetDebugName()));
        m_dwTransientFlags |= IS_EDIT_AND_CONTINUE;
    }

    BOOL IsTenured()
    {
        LIMITED_METHOD_CONTRACT;
        return m_dwTransientFlags & MODULE_IS_TENURED;
    }

#ifndef DACCESS_COMPILE
    VOID SetIsTenured()
    {
        LIMITED_METHOD_CONTRACT;
        FastInterlockOr(&m_dwTransientFlags, MODULE_IS_TENURED);
    }

    // CAUTION: This should only be used as backout code if an assembly is unsuccessfully
    //          added to the shared domain assembly map.
    VOID UnsetIsTenured()
    {
        LIMITED_METHOD_CONTRACT;
        FastInterlockAnd(&m_dwTransientFlags, ~MODULE_IS_TENURED);
    }
#endif // !DACCESS_COMPILE


    // This means the module has been sufficiently fixed up/security checked
    // that type loads can occur in domains. This is not sufficient to indicate
    // that domain-specific types can be loaded when applied to domain-neutral modules
    BOOL IsReadyForTypeLoad()
    {
        LIMITED_METHOD_CONTRACT;
        return m_dwTransientFlags & MODULE_READY_FOR_TYPELOAD;
    }

#ifndef DACCESS_COMPILE
    VOID SetIsReadyForTypeLoad()
    {
        LIMITED_METHOD_CONTRACT;
        FastInterlockOr(&m_dwTransientFlags, MODULE_READY_FOR_TYPELOAD);
    }
#endif

    BOOL IsLowLevelSystemAssemblyByName()
    {
        LIMITED_METHOD_CONTRACT;
        // The flag is set during initialization, so we can skip the memory barrier
        return m_dwPersistedFlags.LoadWithoutBarrier() & LOW_LEVEL_SYSTEM_ASSEMBLY_BY_NAME;
    }

#ifndef DACCESS_COMPILE
    VOID EnsureActive();
    VOID EnsureAllocated();
    VOID EnsureLibraryLoaded();
#endif

    CHECK CheckActivated();

    HRESULT GetCustomAttribute(mdToken parentToken,
                               WellKnownAttribute attribute,
                               const void  **ppData,
                               ULONG *pcbData)
    {
        if (IsReadyToRun())
        {
            if (!GetReadyToRunInfo()->MayHaveCustomAttribute(attribute, parentToken))
                return S_FALSE;
        }

        return GetMDImport()->GetCustomAttributeByName(parentToken, GetWellKnownAttributeName(attribute), ppData, pcbData);
    }

    IMDInternalImport *GetMDImport() const
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;

#ifdef DACCESS_COMPILE
        if (IsReflection())
        {
            return DacGetMDImport(GetReflectionModule(), true);
        }
#endif // DACCESS_COMPILE
        return m_file->GetPersistentMDImport();
    }

#ifndef DACCESS_COMPILE
    IMetaDataEmit *GetEmitter()
    {
        WRAPPER_NO_CONTRACT;

        return m_file->GetEmitter();
    }

    IMetaDataImport2 *GetRWImporter()
    {
        WRAPPER_NO_CONTRACT;

        return m_file->GetRWImporter();
    }

    IMetaDataAssemblyImport *GetAssemblyImporter()
    {
        WRAPPER_NO_CONTRACT;

        return m_file->GetAssemblyImporter();
    }

    HRESULT GetReadablePublicMetaDataInterface(DWORD dwOpenFlags, REFIID riid, LPVOID * ppvInterface);
#endif // !DACCESS_COMPILE

    BOOL IsInCurrentVersionBubble();

#if defined(FEATURE_READYTORUN)
    BOOL IsInSameVersionBubble(Module *target);
#endif // FEATURE_READYTORUN


    LPCWSTR GetPathForErrorMessages();


#ifdef FEATURE_ISYM_READER
    // Gets an up-to-date symbol reader for this module, lazily creating it if necessary
    // The caller must call Release
    ISymUnmanagedReader *GetISymUnmanagedReader(void);
    ISymUnmanagedReader *GetISymUnmanagedReaderNoThrow(void);
#endif // FEATURE_ISYM_READER

    // Save a copy of the provided debugging symbols in the InMemorySymbolStream.
    // These are used by code:Module::GetInMemorySymbolStream and code:Module.GetISymUnmanagedReader
    // This can only be called during module creation, before anyone may have tried to create a reader.
    void SetSymbolBytes(LPCBYTE pSyms, DWORD cbSyms);

    // Does the current configuration permit reading of symbols for this module?
    // Note that this may require calling into managed code (to resolve security policy).
    BOOL IsSymbolReadingEnabled(void);

    BOOL IsPersistedObject(void *address);


    // Get the in-memory symbol stream for this module, if any.
    // If none, this will return null.  This is used by modules loaded in-memory (eg. from a byte-array)
    // and by dynamic modules.
    PTR_CGrowableStream GetInMemorySymbolStream()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;

        return m_pIStreamSym;
    }

#ifndef DACCESS_COMPILE
    // Set the in-memory stream for debug symbols
    // This must only be called when there is no existing stream.
    // This takes an AddRef on the supplied stream.
    void SetInMemorySymbolStream(CGrowableStream *pStream)
    {
        LIMITED_METHOD_CONTRACT;

        // Must have provided valid stream data
        CONSISTENCY_CHECK(pStream != NULL);

        // we expect set to only be called once
        CONSISTENCY_CHECK(m_pIStreamSym == NULL);

        m_pIStreamSym = pStream;
        m_pIStreamSym->AddRef();
    }

    // Release and clear the in-memory symbol stream if any
    void ClearInMemorySymbolStream()
    {
        LIMITED_METHOD_CONTRACT;
        if( m_pIStreamSym != NULL )
        {
            m_pIStreamSym->Release();
            m_pIStreamSym = NULL;
        }
    }

    // Release the symbol reader if any
    // Caller is responsible for aquiring the reader lock if this could occur
    // concurrently with other uses of the reader (i.e. not shutdown/unload time)
    void ReleaseISymUnmanagedReader(void);

#endif // DACCESS_COMPILE

    // IL stub cache
    ILStubCache* GetILStubCache();

    // Classes
    void AddClass(mdTypeDef classdef);
    void BuildClassForModule();
    PTR_EEClassHashTable GetAvailableClassHash()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        {
            // IsResource() may lock when accessing metadata, but this is only in debug,
            // for the assert below
            CONTRACT_VIOLATION(TakesLockViolation);

            _ASSERTE(!IsResource());
        }

        return m_pAvailableClasses;
    }
#ifndef DACCESS_COMPILE
    void SetAvailableClassHash(EEClassHashTable *pAvailableClasses)
    {
        LIMITED_METHOD_CONTRACT;
        {
            // IsResource() may lock when accessing metadata, but this is only in debug,
            // for the assert below
            CONTRACT_VIOLATION(TakesLockViolation);

            _ASSERTE(!IsResource());
        }
        m_pAvailableClasses = pAvailableClasses;
    }
#endif // !DACCESS_COMPILE
    PTR_EEClassHashTable GetAvailableClassCaseInsHash()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        {
            // IsResource() may lock when accessing metadata, but this is only in debug,
            // for the assert below
            CONTRACT_VIOLATION(TakesLockViolation);

            _ASSERTE(!IsResource());
        }
        return m_pAvailableClassesCaseIns;
    }
#ifndef DACCESS_COMPILE
    void SetAvailableClassCaseInsHash(EEClassHashTable *pAvailableClassesCaseIns)
    {
        LIMITED_METHOD_CONTRACT;
        {
            // IsResource() may lock when accessing metadata, but this is only in debug,
            // for the assert below
            CONTRACT_VIOLATION(TakesLockViolation);

            _ASSERTE(!IsResource());
        }
        m_pAvailableClassesCaseIns = pAvailableClassesCaseIns;
    }
#endif // !DACCESS_COMPILE

    // Constructed types tables
    EETypeHashTable *GetAvailableParamTypes()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        {
            // IsResource() may lock when accessing metadata, but this is only in debug,
            // for the assert below
            CONTRACT_VIOLATION(TakesLockViolation);

            _ASSERTE(!IsResource());
        }
        return m_pAvailableParamTypes;
    }

    InstMethodHashTable *GetInstMethodHashTable()
    {
        LIMITED_METHOD_CONTRACT;
        {
            // IsResource() may lock when accessing metadata, but this is only in debug,
            // for the assert below
            CONTRACT_VIOLATION(TakesLockViolation);

            _ASSERTE(!IsResource());
        }
        return m_pInstMethodHashTable;
    }

    // Creates a new Method table for an array.  Used to make type handles
    // Note that if kind == SZARRAY or ARRAY, we get passed the GENERIC_ARRAY
    // needed to create the array.  That way we dont need to load classes during
    // the class load, which avoids the need for a 'being loaded' list
    MethodTable* CreateArrayMethodTable(TypeHandle elemType, CorElementType kind, unsigned rank, class AllocMemTracker *pamTracker);

    // string helper
    void InitializeStringData(DWORD token, EEStringData *pstrData, CQuickBytes *pqb);

    // Resolving
    OBJECTHANDLE ResolveStringRef(DWORD Token, BaseDomain *pDomain, bool bNeedToSyncWithFixups);

    CHECK CheckStringRef(RVA rva);

    // Module/Assembly traversal
    Assembly * GetAssemblyIfLoaded(
            mdAssemblyRef       kAssemblyRef,
            IMDInternalImport * pMDImportOverride = NULL,
            BOOL                fDoNotUtilizeExtraChecks = FALSE,
            AssemblyBinder      *pBindingContextForLoadedAssembly = NULL
            );

private:
    // Helper function used by GetAssemblyIfLoaded. Do not call directly.
    Assembly *GetAssemblyIfLoadedFromNativeAssemblyRefWithRefDefMismatch(mdAssemblyRef kAssemblyRef, BOOL *pfDiscoveredAssemblyRefMatchesTargetDefExactly);
public:

    DomainAssembly * LoadAssembly(mdAssemblyRef kAssemblyRef);
    Module *GetModuleIfLoaded(mdFile kFile, BOOL onlyLoadedInAppDomain, BOOL loadAllowed);
    DomainFile *LoadModule(AppDomain *pDomain, mdFile kFile, BOOL loadResources = TRUE, BOOL bindOnly = FALSE);
    PTR_Module LookupModule(mdToken kFile, BOOL loadResources = TRUE); //wrapper over GetModuleIfLoaded, takes modulerefs as well
    DWORD GetAssemblyRefFlags(mdAssemblyRef tkAssemblyRef);

    // RID maps
    TypeHandle LookupTypeDef(mdTypeDef token, ClassLoadLevel *pLoadLevel = NULL)
    {
        LIMITED_METHOD_DAC_CONTRACT;

        BAD_FORMAT_NOTHROW_ASSERT(TypeFromToken(token) == mdtTypeDef);

        g_IBCLogger.LogRidMapAccess( MakePair( this, token ) );

        TADDR flags;
        TypeHandle th = TypeHandle(m_TypeDefToMethodTableMap.GetElementAndFlags(RidFromToken(token), &flags));

        if (pLoadLevel && !th.IsNull())
        {
            *pLoadLevel = th.GetLoadLevel();
        }

        return th;
    }

    TypeHandle LookupFullyCanonicalInstantiation(mdTypeDef token, ClassLoadLevel *pLoadLevel = NULL)
    {
        LIMITED_METHOD_DAC_CONTRACT;

        BAD_FORMAT_NOTHROW_ASSERT(TypeFromToken(token) == mdtTypeDef);

        g_IBCLogger.LogRidMapAccess( MakePair( this, token ) );

        TADDR flags;
        TypeHandle th = TypeHandle(m_GenericTypeDefToCanonMethodTableMap.GetElementAndFlags(RidFromToken(token), &flags));

        if (pLoadLevel && !th.IsNull())
        {
            *pLoadLevel = th.GetLoadLevel();
        }

        return th;
    }

#ifndef DACCESS_COMPILE
    VOID EnsureTypeDefCanBeStored(mdTypeDef token)
    {
        WRAPPER_NO_CONTRACT; // THROWS/GC_NOTRIGGER/INJECT_FAULT()/MODE_ANY
        m_TypeDefToMethodTableMap.EnsureElementCanBeStored(this, RidFromToken(token));
    }

    void EnsuredStoreTypeDef(mdTypeDef token, TypeHandle value)
    {
        WRAPPER_NO_CONTRACT; // NOTHROW/GC_NOTRIGGER/FORBID_FAULT/MODE_ANY

        _ASSERTE(TypeFromToken(token) == mdtTypeDef);
        m_TypeDefToMethodTableMap.SetElement(RidFromToken(token), value.AsMethodTable());
    }

#endif // !DACCESS_COMPILE

    TypeHandle LookupTypeRef(mdTypeRef token);

#ifndef DACCESS_COMPILE
    //
    // Increase the size of the TypeRef-to-MethodTable LookupMap to make sure the specified token
    // can be stored.  Note that nothing is actually added to the LookupMap at this point.
    //
    // Arguments:
    //    token - the TypeRef metadata token we need to accommodate
    //

    void EnsureTypeRefCanBeStored(mdTypeRef token)
    {
        WRAPPER_NO_CONTRACT; // THROWS/GC_NOTRIGGER/INJECT_FAULT()/MODE_ANY

        _ASSERTE(TypeFromToken(token) == mdtTypeRef);
        m_TypeRefToMethodTableMap.EnsureElementCanBeStored(this, RidFromToken(token));
    }

    void StoreTypeRef(mdTypeRef token, TypeHandle value)
    {
        WRAPPER_NO_CONTRACT;

        _ASSERTE(TypeFromToken(token) == mdtTypeRef);

        g_IBCLogger.LogRidMapAccess( MakePair( this, token ) );

        // The TypeRef cache is strictly a lookaside cache. If we get an OOM trying to grow the table,
        // we cannot abort the load. (This will cause fatal errors during gc promotion.)
        m_TypeRefToMethodTableMap.TrySetElement(RidFromToken(token),
            dac_cast<PTR_TypeRef>(value.AsTAddr()));
    }
#endif // !DACCESS_COMPILE

    MethodDesc *LookupMethodDef(mdMethodDef token);

#ifndef DACCESS_COMPILE
    void EnsureMethodDefCanBeStored(mdMethodDef token)
    {
        WRAPPER_NO_CONTRACT; // THROWS/GC_NOTRIGGER/INJECT_FAULT()/MODE_ANY
        m_MethodDefToDescMap.EnsureElementCanBeStored(this, RidFromToken(token));
    }

    void EnsuredStoreMethodDef(mdMethodDef token, MethodDesc *value)
    {
        WRAPPER_NO_CONTRACT; // NOTHROW/GC_NOTRIGGER/FORBID_FAULT/MODE_ANY

        _ASSERTE(TypeFromToken(token) == mdtMethodDef);
        m_MethodDefToDescMap.SetElement(RidFromToken(token), value);
    }
#endif // !DACCESS_COMPILE

#ifndef DACCESS_COMPILE
    FieldDesc *LookupFieldDef(mdFieldDef token)
    {
        WRAPPER_NO_CONTRACT;

        _ASSERTE(TypeFromToken(token) == mdtFieldDef);
        return m_FieldDefToDescMap.GetElement(RidFromToken(token));
    }
#else // DACCESS_COMPILE
    // FieldDesc isn't defined at this point so PTR_FieldDesc can't work.
    FieldDesc *LookupFieldDef(mdFieldDef token);
#endif // DACCESS_COMPILE

#ifndef DACCESS_COMPILE
    void EnsureFieldDefCanBeStored(mdFieldDef token)
    {
        WRAPPER_NO_CONTRACT; // THROWS/GC_NOTRIGGER/INJECT_FAULT()/MODE_ANY
        m_FieldDefToDescMap.EnsureElementCanBeStored(this, RidFromToken(token));
    }

    void EnsuredStoreFieldDef(mdFieldDef token, FieldDesc *value)
    {
        WRAPPER_NO_CONTRACT; // NOTHROW/GC_NOTRIGGER/FORBID_FAULT/MODE_ANY

        _ASSERTE(TypeFromToken(token) == mdtFieldDef);
        m_FieldDefToDescMap.SetElement(RidFromToken(token), value);
    }
#endif // !DACCESS_COMPILE

    FORCEINLINE TADDR LookupMemberRef(mdMemberRef token, BOOL *pfIsMethod)
    {
        WRAPPER_NO_CONTRACT;

        _ASSERTE(TypeFromToken(token) == mdtMemberRef);

        TADDR pResult = dac_cast<TADDR>(m_pMemberRefToDescHashTable->GetValue(token, pfIsMethod));
        g_IBCLogger.LogRidMapAccess( MakePair( this, token ) );
        return pResult;
    }
    MethodDesc *LookupMemberRefAsMethod(mdMemberRef token);
#ifndef DACCESS_COMPILE
    void StoreMemberRef(mdMemberRef token, FieldDesc *value)
    {
        WRAPPER_NO_CONTRACT;

        _ASSERTE(TypeFromToken(token) == mdtMemberRef);
        CrstHolder ch(this->GetLookupTableCrst());
        m_pMemberRefToDescHashTable->Insert(token, value);
    }
    void StoreMemberRef(mdMemberRef token, MethodDesc *value)
    {
        WRAPPER_NO_CONTRACT;

        _ASSERTE(TypeFromToken(token) == mdtMemberRef);
        CrstHolder ch(this->GetLookupTableCrst());
        m_pMemberRefToDescHashTable->Insert(token, value);
    }
#endif // !DACCESS_COMPILE

    PTR_TypeVarTypeDesc LookupGenericParam(mdGenericParam token)
    {
        WRAPPER_NO_CONTRACT;

        _ASSERTE(TypeFromToken(token) == mdtGenericParam);
        return m_GenericParamToDescMap.GetElement(RidFromToken(token));
    }
#ifndef DACCESS_COMPILE
    void StoreGenericParamThrowing(mdGenericParam token, TypeVarTypeDesc *value)
    {
        WRAPPER_NO_CONTRACT;

        _ASSERTE(TypeFromToken(token) == mdtGenericParam);
        m_GenericParamToDescMap.AddElement(this, RidFromToken(token), value);
    }
#endif // !DACCESS_COMPILE

    PTR_Module LookupFile(mdFile token)
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;

        _ASSERTE(TypeFromToken(token) == mdtFile);
        return m_FileReferencesMap.GetElement(RidFromToken(token));
    }


#ifndef DACCESS_COMPILE
    void EnsureFileCanBeStored(mdFile token)
    {
        WRAPPER_NO_CONTRACT; // THROWS/GC_NOTRIGGER/INJECT_FAULT()/MODE_ANY

        _ASSERTE(TypeFromToken(token) == mdtFile);
        m_FileReferencesMap.EnsureElementCanBeStored(this, RidFromToken(token));
    }

    void EnsuredStoreFile(mdFile token, Module *value)
    {
        WRAPPER_NO_CONTRACT; // NOTHROW/GC_NOTRIGGER/FORBID_FAULT


        _ASSERTE(TypeFromToken(token) == mdtFile);
        m_FileReferencesMap.SetElement(RidFromToken(token), value);
    }


    void StoreFileThrowing(mdFile token, Module *value)
    {
        WRAPPER_NO_CONTRACT;


        _ASSERTE(TypeFromToken(token) == mdtFile);
        m_FileReferencesMap.AddElement(this, RidFromToken(token), value);
    }

    BOOL StoreFileNoThrow(mdFile token, Module *value)
    {
        WRAPPER_NO_CONTRACT;

        _ASSERTE(TypeFromToken(token) == mdtFile);
        return m_FileReferencesMap.TrySetElement(RidFromToken(token), value);
    }

    mdAssemblyRef FindManifestModule(Module *value)
    {
        WRAPPER_NO_CONTRACT;

        return m_ManifestModuleReferencesMap.Find(value) | mdtAssembly;
    }
#endif // !DACCESS_COMPILE

    DWORD GetFileMax() { LIMITED_METHOD_DAC_CONTRACT;  return m_FileReferencesMap.GetSize(); }

    Assembly *LookupAssemblyRef(mdAssemblyRef token);

#ifndef DACCESS_COMPILE
    //
    // Increase the size of the AssemblyRef-to-Module LookupMap to make sure the specified token
    // can be stored.  Note that nothing is actually added to the LookupMap at this point.
    //
    // Arguments:
    //    token - the AssemblyRef metadata token we need to accommodate
    //

    void EnsureAssemblyRefCanBeStored(mdAssemblyRef token)
    {
        WRAPPER_NO_CONTRACT; // THROWS/GC_NOTRIGGER/INJECT_FAULT()/MODE_ANY

        _ASSERTE(TypeFromToken(token) == mdtAssemblyRef);
        m_ManifestModuleReferencesMap.EnsureElementCanBeStored(this, RidFromToken(token));
    }

    void ForceStoreAssemblyRef(mdAssemblyRef token, Assembly *value);
    void StoreAssemblyRef(mdAssemblyRef token, Assembly *value);

    mdAssemblyRef FindAssemblyRef(Assembly *targetAssembly);

    void          CreateAssemblyRefByNameTable(AllocMemTracker *pamTracker);
    bool          HasReferenceByName(LPCUTF8 pModuleName);

#endif // !DACCESS_COMPILE

    DWORD GetAssemblyRefMax() {LIMITED_METHOD_CONTRACT;  return m_ManifestModuleReferencesMap.GetSize(); }

    MethodDesc *FindMethodThrowing(mdToken pMethod);
    MethodDesc *FindMethod(mdToken pMethod);

    HRESULT GetPropertyInfoForMethodDef(mdMethodDef md, mdProperty *ppd, LPCSTR *pName, ULONG *pSemantic);

public:

    // Debugger stuff
    BOOL NotifyDebuggerLoad(AppDomain *pDomain, DomainFile * pDomainFile, int level, BOOL attaching);
    void NotifyDebuggerUnload(AppDomain *pDomain);

    void SetDebuggerInfoBits(DebuggerAssemblyControlFlags newBits);

    DebuggerAssemblyControlFlags GetDebuggerInfoBits(void)
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;

        return (DebuggerAssemblyControlFlags)((m_dwTransientFlags &
                                               DEBUGGER_INFO_MASK_PRIV) >>
                                              DEBUGGER_INFO_SHIFT_PRIV);
    }

    void UpdateNewlyAddedTypes();

#ifdef PROFILING_SUPPORTED
    BOOL IsProfilerNotified() {LIMITED_METHOD_CONTRACT;  return (m_dwTransientFlags & IS_PROFILER_NOTIFIED) != 0; }
    void NotifyProfilerLoadFinished(HRESULT hr);
#endif // PROFILING_SUPPORTED

    BOOL HasReadyToRunInlineTrackingMap();
    COUNT_T GetReadyToRunInliners(PTR_Module inlineeOwnerMod, mdMethodDef inlineeTkn, COUNT_T inlinersSize, MethodInModule inliners[], BOOL *incompleteData);
#if defined(PROFILING_SUPPORTED) && !defined(DACCESS_COMPILE)
    BOOL HasJitInlineTrackingMap();
    PTR_JITInlineTrackingMap GetJitInlineTrackingMap() { LIMITED_METHOD_CONTRACT; return m_pJitInlinerTrackingMap; }
    void AddInlining(MethodDesc *inliner, MethodDesc *inlinee);
#endif // defined(PROFILING_SUPPORTED) && !defined(DACCESS_COMPILE)

public:
    void NotifyEtwLoadFinished(HRESULT hr);

    // Enregisters a VASig.
    VASigCookie *GetVASigCookie(Signature vaSignature);

public:
#ifndef DACCESS_COMPILE
    BOOL Equals(Module *pModule) { WRAPPER_NO_CONTRACT; return m_file->Equals(pModule->m_file); }
    BOOL Equals(PEFile *pFile) { WRAPPER_NO_CONTRACT; return m_file->Equals(pFile); }
#endif // !DACCESS_COMPILE

    LPCUTF8 GetSimpleName()
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(m_pSimpleName != NULL);
        return m_pSimpleName;
    }

    HRESULT GetScopeName(LPCUTF8 * pszName) { WRAPPER_NO_CONTRACT; return m_file->GetScopeName(pszName); }
    const SString &GetPath() { WRAPPER_NO_CONTRACT; return m_file->GetPath(); }

#ifdef LOGGING
    LPCWSTR GetDebugName() { WRAPPER_NO_CONTRACT; return m_file->GetDebugName(); }
#endif

    PEImageLayout * GetReadyToRunImage();
    PTR_CORCOMPILE_IMPORT_SECTION GetImportSections(COUNT_T *pCount);
    PTR_CORCOMPILE_IMPORT_SECTION GetImportSectionFromIndex(COUNT_T index);
    PTR_CORCOMPILE_IMPORT_SECTION GetImportSectionForRVA(RVA rva);

    // These are overridden by reflection modules
    virtual TADDR GetIL(RVA il);

    virtual PTR_VOID GetRvaField(RVA field);
    CHECK CheckRvaField(RVA field);
    CHECK CheckRvaField(RVA field, COUNT_T size);

    const void *GetInternalPInvokeTarget(RVA target)
    { WRAPPER_NO_CONTRACT; return m_file->GetInternalPInvokeTarget(target); }

    BOOL HasTls();
    BOOL IsRvaFieldTls(DWORD field);
    UINT32 GetFieldTlsOffset(DWORD field);
    UINT32 GetTlsIndex();

    BOOL IsSigInIL(PCCOR_SIGNATURE signature);

    mdToken GetEntryPointToken();

    BYTE *GetProfilerBase();


    // Active transition path management
    //
    // This list keeps track of module which we have active transition
    // paths to.  An active transition path is where we move from
    // active execution in one module to another module without
    // involving triggering the file loader to ensure that the
    // destination module is active.  We must explicitly list these
    // relationships so the the loader can ensure that the activation
    // constraints are a priori satisfied.
    //
    // Conditional vs. Unconditional describes how we deal with
    // activation failure of a dependency.  In the unconditional case,
    // we propagate the activation failure to the depending module.
    // In the conditional case, we activate a "trigger" in the active
    // transition path which will cause the path to fail in particular
    // app domains where the destination module failed to activate.
    // (This trigger in the path typically has a perf cost even in the
    // nonfailing case.)
    //
    // In either case we must try to perform the activation eagerly -
    // even in the conditional case we have to know whether to turn on
    // the trigger or not before we let the active transition path
    // execute.

    void AddActiveDependency(Module *pModule, BOOL unconditional);

    BYTE* GetNativeFixupBlobData(RVA fixup);

    IMDInternalImport *GetNativeAssemblyImport(BOOL loadAllowed = TRUE);
    IMDInternalImport *GetNativeAssemblyImportIfLoaded();

    BOOL FixupNativeEntry(CORCOMPILE_IMPORT_SECTION * pSection, SIZE_T fixupIndex, SIZE_T *fixup, BOOL mayUsePrecompiledNDirectMethods = TRUE);

    //this split exists to support new CLR Dump functionality in DAC.  The
    //template removes any indirections.
    BOOL FixupDelayList(TADDR pFixupList, BOOL mayUsePrecompiledNDirectMethods = TRUE);

    template<typename Ptr, typename FixupNativeEntryCallback>
    BOOL FixupDelayListAux(TADDR pFixupList,
                           Ptr pThis, FixupNativeEntryCallback pfnCB,
                           PTR_CORCOMPILE_IMPORT_SECTION pImportSections, COUNT_T nImportSections,
                           PEDecoder * pNativeImage, BOOL mayUsePrecompiledNDirectMethods = TRUE);
    void RunEagerFixups();
    void RunEagerFixupsUnlocked();

    Module *GetModuleFromIndex(DWORD ix);
    Module *GetModuleFromIndexIfLoaded(DWORD ix);

    ICorJitInfo::BlockCounts * AllocateMethodBlockCounts(mdToken _token, DWORD _size, DWORD _ILSize);
    HANDLE OpenMethodProfileDataLogFile(GUID mvid);
    static void ProfileDataAllocateTokenLists(ProfileEmitter * pEmitter, TokenProfileData* pTokenProfileData);
    HRESULT WriteMethodProfileDataLogFile(bool cleanup);
    static void WriteAllModuleProfileData(bool cleanup);
    void SetMethodProfileList(CORCOMPILE_METHOD_PROFILE_LIST * value)
    {
        m_methodProfileList = value;
    }

    void CreateProfilingData();
    void DeleteProfilingData();

    PTR_ProfilingBlobTable GetProfilingBlobTable();

    void LogTokenAccess(mdToken token, SectionFormat format, ULONG flagNum);
    void LogTokenAccess(mdToken token, ULONG flagNum);

    BOOL IsReadyToRun() const
    {
        LIMITED_METHOD_DAC_CONTRACT;

#ifdef FEATURE_READYTORUN
        return m_pReadyToRunInfo != NULL;
#else
        return FALSE;
#endif
    }

#ifdef FEATURE_READYTORUN
    PTR_ReadyToRunInfo GetReadyToRunInfo() const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_pReadyToRunInfo;
    }

    PTR_NativeImage GetCompositeNativeImage() const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_pNativeImage;
    }
#endif

#ifdef _DEBUG
    //Similar to the ExpandAll we use for NGen, this forces jitting of all methods in a module.  This is
    //used for debug purposes though.
    void ExpandAll();
#endif

    BOOL IsIJWFixedUp() { return m_dwTransientFlags & IS_IJW_FIXED_UP; }
    void SetIsIJWFixedUp();

    BOOL IsBeingUnloaded() { return m_dwTransientFlags & IS_BEING_UNLOADED; }
    void   SetBeingUnloaded();
    void   StartUnload();


public:
    idTypeSpec   LogInstantiatedType(TypeHandle typeHnd, ULONG flagNum);
    idMethodSpec LogInstantiatedMethod(const MethodDesc * md, ULONG flagNum);

    static DWORD EncodeModuleHelper(void* pModuleContext, Module *pReferencedModule);
    static void  TokenDefinitionHelper(void* pModuleContext, Module *pReferencedModule, DWORD index, mdToken* token);

public:
    MethodTable* MapZapType(UINT32 typeID);

    void SetDynamicIL(mdToken token, TADDR blobAddress, BOOL fTemporaryOverride);
    TADDR GetDynamicIL(mdToken token, BOOL fAllowTemporary);

    // store and retrieve the instrumented IL offset mapping for a particular method
#if !defined(DACCESS_COMPILE)
    void SetInstrumentedILOffsetMapping(mdMethodDef token, InstrumentedILOffsetMapping mapping);
#endif // !DACCESS_COMPILE
    InstrumentedILOffsetMapping GetInstrumentedILOffsetMapping(mdMethodDef token);

public:
    // This helper returns to offsets for the slots/bytes/handles. They return the offset in bytes from the beggining
    // of the 1st GC pointer in the statics block for the module.
    void        GetOffsetsForRegularStaticData(
                    mdTypeDef cl,
                    BOOL bDynamic,
                    DWORD dwGCStaticHandles,
                    DWORD dwNonGCStaticBytes,
                    DWORD * pOutStaticHandleOffset,
                    DWORD * pOutNonGCStaticOffset);

    void        GetOffsetsForThreadStaticData(
                    mdTypeDef cl,
                    BOOL bDynamic,
                    DWORD dwGCStaticHandles,
                    DWORD dwNonGCStaticBytes,
                    DWORD * pOutStaticHandleOffset,
                    DWORD * pOutNonGCStaticOffset);


    BOOL        IsStaticStoragePrepared(mdTypeDef tkType);

    DWORD       GetNumGCThreadStaticHandles()
    {
        return m_dwMaxGCThreadStaticHandles;;
    }

    CrstBase*           GetFixupCrst()
    {
        return &m_FixupCrst;
    }

    void                AllocateRegularStaticHandles(AppDomain* pDomainMT);

    void                FreeModuleIndex();

    DWORD               GetDomainLocalModuleSize()
    {
        return m_dwRegularStaticsBlockSize;
    }

    DWORD               GetThreadLocalModuleSize()
    {
        return m_dwThreadStaticsBlockSize;
    }

    DWORD               AllocateDynamicEntry(MethodTable *pMT);

    // We need this for the jitted shared case,
    inline MethodTable* GetDynamicClassMT(DWORD dynamicClassID);

    static ModuleIndex AllocateModuleIndex();
    static void FreeModuleIndex(ModuleIndex index);

    ModuleIndex          GetModuleIndex()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_ModuleIndex;
    }

    SIZE_T               GetModuleID()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return dac_cast<TADDR>(m_ModuleID);
    }

    SIZE_T *             GetAddrModuleID()
    {
        LIMITED_METHOD_CONTRACT;
        return (SIZE_T*) &m_ModuleID;
    }

    static SIZE_T       GetOffsetOfModuleID()
    {
        LIMITED_METHOD_CONTRACT;
        return offsetof(Module, m_ModuleID);
    }

    PTR_DomainLocalModule   GetDomainLocalModule();

    // LoaderHeap for storing IJW thunks
    PTR_LoaderHeap           m_pThunkHeap;

    // Self-initializing accessor for IJW thunk heap
    LoaderHeap              *GetThunkHeap();
    // Self-initializing accessor for domain-independent IJW thunk heap
    LoaderHeap              *GetDllThunkHeap();

protected:

    void            BuildStaticsOffsets     (AllocMemTracker *pamTracker);
    void            AllocateStatics         (AllocMemTracker *pamTracker);

    // ModuleID is quite ugly. We should try to switch to using ModuleIndex instead
    // where appropriate, and we should clean up code that uses ModuleID
    PTR_DomainLocalModule   m_ModuleID;       // MultiDomain case: tagged (low bit 1) ModuleIndex
                                              // SingleDomain case: pointer to domain local module

    ModuleIndex             m_ModuleIndex;

    // reusing the statics area of a method table to store
    // these for the non domain neutral case, but they're now unified
    // it so that we don't have different code paths for this.
    PTR_DWORD               m_pRegularStaticOffsets;        // Offset of statics in each class
    PTR_DWORD               m_pThreadStaticOffsets;         // Offset of ThreadStatics in each class

    // All types with RID <= this value have static storage allocated within the module by AllocateStatics
    // If AllocateStatics hasn't run yet, the value is 0
    RID                     m_maxTypeRidStaticsAllocated;

    // @NICE: see if we can remove these fields
    DWORD                   m_dwMaxGCRegularStaticHandles;  // Max number of handles we can have.
    DWORD                   m_dwMaxGCThreadStaticHandles;

    // Size of the precomputed statics block. This includes class init bytes, gc handles and non gc statics
    DWORD                   m_dwRegularStaticsBlockSize;
    DWORD                   m_dwThreadStaticsBlockSize;

    // For 'dynamic' statics (Reflection and generics)
    SIZE_T                  m_cDynamicEntries;              // Number of used entries in DynamicStaticsInfo table
    SIZE_T                  m_maxDynamicEntries;            // Size of table itself, including unused entries

    // Info we need for dynamic statics that we can store per-module (ie, no need for it to be duplicated
    // per appdomain)
    struct DynamicStaticsInfo
    {
        MethodTable*        pEnclosingMT;                   // Enclosing type; necessarily in this loader module
    };
    DynamicStaticsInfo*     m_pDynamicStaticsInfo;          // Table with entry for each dynamic ID


public:
    //-----------------------------------------------------------------------------------------
    // If true,  strings only need to be interned at a per module basis, instead of at a
    // per appdomain basis, which is the default. Use the module accessor so you don't need
    // to touch the metadata in the ngen case
    //-----------------------------------------------------------------------------------------
    BOOL                    IsNoStringInterning();

    //-----------------------------------------------------------------------------------------
    // Returns a BOOL to indicate if we have computed whether compiler has instructed us to
    // wrap the non-CLS compliant exceptions or not.
    //-----------------------------------------------------------------------------------------
    BOOL                    IsRuntimeWrapExceptionsStatusComputed();

    //-----------------------------------------------------------------------------------------
    // If true,  any non-CLSCompliant exceptions (i.e. ones which derive from something other
    // than System.Exception) are wrapped in a RuntimeWrappedException instance.  In other
    // words, they become compliant
    //-----------------------------------------------------------------------------------------
    BOOL                    IsRuntimeWrapExceptions();

    BOOL                    HasDefaultDllImportSearchPathsAttribute();

    BOOL IsDefaultDllImportSearchPathsAttributeCached()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_dwPersistedFlags & DEFAULT_DLL_IMPORT_SEARCH_PATHS_IS_CACHED) != 0;
    }

    ULONG DefaultDllImportSearchPathsAttributeCachedValue()
    {
        LIMITED_METHOD_CONTRACT;
        return m_DefaultDllImportSearchPathsAttributeValue & 0xFFFFFFFD;
    }

    BOOL DllImportSearchAssemblyDirectory()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_DefaultDllImportSearchPathsAttributeValue & 0x2) != 0;
    }

    //-----------------------------------------------------------------------------------------
    // True iff metadata version string is 1.* or 2.*.
    // @TODO (post-Dev10): All places that need this information should call this function
    // instead of parsing the version themselves.
    //-----------------------------------------------------------------------------------------
    BOOL                    IsPreV4Assembly();

protected:


    // initialize Crst controlling the Dynamic IL hashtables
    void                    InitializeDynamicILCrst();

public:

    CrstBase *GetLookupTableCrst()
    {
        LIMITED_METHOD_CONTRACT;
        return &m_LookupTableCrst;
    }

private:

    // This struct stores the data used by the managed debugging infrastructure.  If it turns out that
    // the debugger is increasing the size of the Module class by too much, we can consider allocating
    // this struct lazily on demand.
    struct DebuggerSpecificData
    {
        // Mutex protecting update access to the DynamicILBlobTable and TemporaryILBlobTable
        PTR_Crst                 m_pDynamicILCrst;

                                                // maps tokens for EnC/dynamics/reflection emit to their corresponding IL blobs
                                                // this map *always* overrides the Metadata RVA
        PTR_DynamicILBlobTable   m_pDynamicILBlobTable;

                                                // maps tokens for to their corresponding overriden IL blobs
                                                // this map conditionally overrides the Metadata RVA and the DynamicILBlobTable
        PTR_DynamicILBlobTable   m_pTemporaryILBlobTable;

        // hash table storing any profiler-provided instrumented IL offset mapping
        PTR_ILOffsetMappingTable m_pILOffsetMappingTable;

        // Strict count of # of methods in this module that are JMC-enabled.
        LONG    m_cTotalJMCFuncs;

        // The default JMC status for methods in this module.
        // Individual methods can be overridden.
        bool    m_fDefaultJMCStatus;
    };

    DebuggerSpecificData  m_debuggerSpecificData;

#if defined(PROFILING_SUPPORTED) || defined(PROFILING_SUPPORTED_DATA)
    PTR_JITInlineTrackingMap m_pJitInlinerTrackingMap;
#endif // defined(PROFILING_SUPPORTED) || defined(PROFILING_SUPPORTED_DATA)


    LPCSTR               *m_AssemblyRefByNameTable;  // array that maps mdAssemblyRef tokens into their simple name
    DWORD                 m_AssemblyRefByNameCount;  // array size

    // a.dll calls a method in b.dll and that method call a method in c.dll. When ngening
    // a.dll it is possible then method in b.dll can be inlined. When that happens a.ni.dll stores
    // an added native metadata which has information about assemblyRef to c.dll
    // Now due to facades, this scenario is very common. This led to lots of calls to
    // binder to get the module corresponding to assemblyRef in native metadata.
    // Adding a lookup map to cache assembly ptr so that AssemblySpec::LoadAssembly()
    // is not called for each fixup

    PTR_Assembly           *m_NativeMetadataAssemblyRefMap;

public:
#if !defined(DACCESS_COMPILE)
    PTR_Assembly GetNativeMetadataAssemblyRefFromCache(DWORD rid)
    {
        PTR_Assembly * NativeMetadataAssemblyRefMap = VolatileLoadWithoutBarrier(&m_NativeMetadataAssemblyRefMap);

        if (NativeMetadataAssemblyRefMap == NULL)
            return NULL;

        _ASSERTE(rid <= GetNativeMetadataAssemblyCount());
        return NativeMetadataAssemblyRefMap[rid - 1];
    }

    void SetNativeMetadataAssemblyRefInCache(DWORD rid, PTR_Assembly pAssembly);

    uint32_t GetNativeMetadataAssemblyCount();
#endif // !defined(DACCESS_COMPILE)

    // For protecting dictionary layout slot expansions
    CrstExplicitInit        m_DictionaryCrst;
};

//
// A ReflectionModule is a module created by reflection
//

class ReflectionModule : public Module
{
    VPTR_VTABLE_CLASS(ReflectionModule, Module)

 public:
    HCEESECTION m_sdataSection;

 protected:
    ICeeGenInternal * m_pCeeFileGen;
private:
    Assembly             *m_pCreatingAssembly;
    RefClassWriter       *m_pInMemoryWriter;


    // Simple Critical Section used for basic leaf-lock operatons.
    CrstExplicitInit        m_CrstLeafLock;

    // Buffer of Metadata storage for dynamic modules. May be NULL. This provides a reasonable way for
    // the debugger to get metadata of dynamic modules from out of process.
    // A dynamic module will eagerly serialize its metadata to this buffer.
    PTR_SBuffer m_pDynamicMetadata;

    // If true, does not eagerly serialize metadata in code:ReflectionModule.CaptureModuleMetaDataToMemory.
    // This is used to allow bulk emitting types without re-emitting the metadata between each type.
    bool m_fSuppressMetadataCapture;

#if !defined DACCESS_COMPILE
    ReflectionModule(Assembly *pAssembly, mdFile token, PEFile *pFile);
#endif // !DACCESS_COMPILE

public:

#ifdef DACCESS_COMPILE
    // Accessor to expose m_pDynamicMetadata to debugger.
    PTR_SBuffer GetDynamicMetadataBuffer() const;
#endif

#if !defined DACCESS_COMPILE
    static ReflectionModule *Create(Assembly *pAssembly, PEFile *pFile, AllocMemTracker *pamTracker, LPCWSTR szName);
    void Initialize(AllocMemTracker *pamTracker, LPCWSTR szName);
    void Destruct();
#endif // !DACCESS_COMPILE

    // Overrides functions to access sections
    virtual TADDR GetIL(RVA target);
    virtual PTR_VOID GetRvaField(RVA rva);

    Assembly* GetCreatingAssembly( void )
    {
        LIMITED_METHOD_CONTRACT;

        return m_pCreatingAssembly;
    }

    void SetCreatingAssembly( Assembly* assembly )
    {
        LIMITED_METHOD_CONTRACT;

        m_pCreatingAssembly = assembly;
    }

    ICeeGenInternal *GetCeeGen() {LIMITED_METHOD_CONTRACT;  return m_pCeeFileGen; }

    RefClassWriter *GetClassWriter()
    {
        LIMITED_METHOD_CONTRACT;

        return m_pInMemoryWriter;
    }

    // Eagerly serialize the metadata to a buffer that the debugger can retrieve.
    void CaptureModuleMetaDataToMemory();
};

// Module holders
FORCEINLINE void VoidModuleDestruct(Module *pModule)
{
#ifndef DACCESS_COMPILE
    if (g_fEEStarted)
        pModule->Destruct();
#endif
}

typedef Wrapper<Module*, DoNothing, VoidModuleDestruct, 0> ModuleHolder;



FORCEINLINE void VoidReflectionModuleDestruct(ReflectionModule *pModule)
{
#ifndef DACCESS_COMPILE
    pModule->Destruct();
#endif
}

typedef Wrapper<ReflectionModule*, DoNothing, VoidReflectionModuleDestruct, 0> ReflectionModuleHolder;



//----------------------------------------------------------------------
// VASigCookieEx (used to create a fake VASigCookie for unmanaged->managed
// calls to vararg functions. These fakes are distinguished from the
// real thing by having a null mdVASig.
//----------------------------------------------------------------------
struct VASigCookieEx : public VASigCookie
{
    const BYTE *m_pArgs;        // pointer to first unfixed unmanaged arg
};

// Save the command line for the current process.
void SaveManagedCommandLine(LPCWSTR pwzAssemblyPath, int argc, LPCWSTR *argv);

LPCWSTR GetCommandLineForDiagnostics();

#endif // !CEELOAD_H_
