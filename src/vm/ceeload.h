// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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

#ifdef FEATURE_PREJIT
#include "dataimage.h"
#endif // FEATURE_PREJIT

#ifdef FEATURE_COMINTEROP
#include "winrttypenameconverter.h"
#endif // FEATURE_COMINTEROP

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
class CompilationDomain;
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
#ifdef FEATURE_PREJIT
class TypeHandleList;
class TrackingMap;
struct MethodInModule;
class PersistentInlineTrackingMapNGen;
class JITInlineTrackingMap;

extern VerboseLevel g_CorCompileVerboseLevel;
#endif

// Hash table parameter of available classes (name -> module/class) hash
#define AVAILABLE_CLASSES_HASH_BUCKETS 1024
#define AVAILABLE_CLASSES_HASH_BUCKETS_COLLECTIBLE 128
#define PARAMTYPES_HASH_BUCKETS 23
#define PARAMMETHODS_HASH_BUCKETS 11
#define METHOD_STUBS_HASH_BUCKETS 11
#define GUID_TO_TYPE_HASH_BUCKETS 16
            
// The native symbol reader dll name
#if defined(_AMD64_)
#define NATIVE_SYMBOL_READER_DLL W("Microsoft.DiaSymReader.Native.amd64.dll")
#elif defined(_X86_)
#define NATIVE_SYMBOL_READER_DLL W("Microsoft.DiaSymReader.Native.x86.dll")
#elif defined(_ARM_)
#define NATIVE_SYMBOL_READER_DLL W("Microsoft.DiaSymReader.Native.arm.dll")
#elif defined(_ARM64_)
// Use diasymreader until the package has an arm64 version - issue #7360
//#define NATIVE_SYMBOL_READER_DLL W("Microsoft.DiaSymReader.Native.arm64.dll")
#define NATIVE_SYMBOL_READER_DLL W("diasymreader.dll")
#endif

typedef DPTR(PersistentInlineTrackingMapNGen) PTR_PersistentInlineTrackingMapNGen;
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

#ifdef FEATURE_PREJIT

//
// LookupMap cold entry compression support
//
// A lookup map (the cold section) is notionally an array of pointer values indexed by rid. The pointers are
// generally to data structures such as MethodTables or MethodDescs. When we compress such a table (at ngen
// time) we wish to avoid direct pointers, since these would need to be fixed up due to image base
// relocations. Instead we store RVAs (Relative Virtual Addresses). Unlike regular RVAs our base address is
// the map address itself (as opposed to the module base). We do this purely out of convenience since
// LookupMaps don't store the module base address.
//
// It turns out that very often the value pointers (and hence the value RVAs) are related to each other:
// adjacent map entries often point to data structures that were allocated next to or close to each other. The
// compression algorithm takes advantage of this fact: instead of storing value RVAs we store the deltas
// between RVAs. So the nth value in the table is composed of the addition of the deltas from the preceding (n
// - 1) entries. Since the deltas are often small (especially when we take structure alignment into account
// and realize that we can discard the lower 2 or 3 bits of the delta) we can store them in a compressed
// manner by discarding the insignificant leading zero bits in each value.
//
// So now we imagine our compressed table to be a sequence of entries, each entry being a variably sized delta
// from the previous entry. As a result we need some means to encode how large each delta in the table is. We
// could use a fixed size field (a 5-bit length field would be able to encode any length between 1 and 32
// bits, say). This is troublesome since although most entry values are close in value there are a few
// (usually a minority) that require much larger deltas (hot/cold data splitting based on profiling can cause
// this for instance). For most tables this would force us to use a large fixed-size length field for every
// entry, just to deal with the relatively uncommon worst case (5 bits would be enough, but many entry deltas
// can be encoded in 2 or 3 bits).
//
// Instead we utilize a compromise: we store all delta lengths with a small number of bits
// (kLookupMapLengthBits below). Instead of encoding the length directly this value indexes a per-map table of
// possible delta encoding lengths. During ngen we calculate the optimal value for each entry in this encoding
// length table. The advantage here is that it lets us encode both best case and worst case delta lengths with
// a fixed size but small field. The disadvantage is that some deltas will be encoded with more bits than they
// strictly need.
//
// This still leaves the problem of runtime lookup performance. Touches to the cold section of a LookupMap
// aren't all that critical (after all the data is meant to be cold), but looking up the last entry of a map
// with 22 thousand entries (roughly what the MethodDefToDesc map in mscorlib is sized at at the time of
// writing) is still likely to so inefficient as to be noticeable. Remember that the issue is that we have to
// decode all predecessor entries in order to compute the value of a given entry in the table.
//
// To address this we introduce an index to each compressed map. The index contains an entry for each
// kLookupMapIndexStride'th entry in the compressed map. The index entry consists of the RVA of the
// corresponding table value and the bit offset into the compressed map at which the data for the next entry
// commences. Thus we can use the index to find a value within kLookupMapIndexStride entries of our target and
// then proceed to decode only the last few compressed entries to finish the job. This reduces the lookup to a
// constant time operation once more (given a reasonable value for kLookupMapIndexStride).
//
// The main areas in which this algorithm can be tuned are the number of bits used as an index into the
// encoding lengths table (kLookupMapLengthBits) and the frequency with which entries are bookmarked in the
// index (kLookupMapIndexStride). The current values have been set based on looking at models of mscorlib,
// PresentationCore and PresentationFramework built from the actual ridmap data in their ngen images and
// methodically trying different values in order to maximize compression or balance size versus likely runtime
// performance. An alternative strategy was considered using direct (non-length prefix) encoding of the
// deltas with a couple of variantions on probability-based variable length encoding (completely unbalanced
// tree and completely balanced tree with pessimally encoded worst case escapes). But these were found to
// yield best case results similar to the above but with more complex processing required at ngen (optimal
// results for these algorithms are achieved when you have enough resources to build a probability map of your
// entire data).
//
// Note that not all lookup tables are suitable for compression. In fact we compress only TypeDefToMethodTable
// and MethodDefToDesc tables. For one thing this optimization only brings benefits to larger tables. But more
// importantly we cannot mutate compressed entries (for obvious reasons). Many of the lookup maps are only
// partially populated at ngen time or otherwise might be updated at runtime and thus are not candidates.
//
// In the threshhold timeframe (predicted to be .NET Framework 4.5.3 at the time of writing), we added profiler support
// for adding new types to NGEN images. Historically we could always do this for jitted images, but one of the
// blockers for NGEN were the compressed RID maps. We worked around that by supporting multi-node maps in which
// the first node is compressed, but all future nodes are uncompressed. The NGENed portion will all land in the
// compressed node, while the new profiler added data will land in the uncompressed portion. Note this could 
// probably be leveraged for other dynamic scenarios such as a limited form of EnC, but nothing further has
// been implemented at this time.
//

// Some useful constants used when compressing tables.
enum {
    kLookupMapLengthBits    = 2,                            // Bits used to encode an index into a table of possible value lengths
    kLookupMapLengthEntries = 1 << kLookupMapLengthBits,    // Number of entries in the encoding table above
    kLookupMapIndexStride   = 0x10,                         // The range of table entries covered by one index entry (power of two for faster hash lookup)
    kBitsPerRVA             = sizeof(DWORD) * 8,            // Bits in an (uncompressed) table value RVA (RVAs
                                                            // currently still 32-bit even on 64-bit platforms)
#ifdef _WIN64
    kFlagBits               = 3,                            // Number of bits at the bottom of a value
                                                            // pointer that may be used for flags
#else // _WIN64
    kFlagBits               = 2,
#endif // _WIN64

};

#endif // FEATURE_PREJIT

struct LookupMapBase
{
    DPTR(LookupMapBase) pNext;

    ArrayDPTR(TADDR)    pTable;

    // Number of elements in this node (only RIDs less than this value can be present in this node)
    DWORD               dwCount;

    // Set of flags that the map supports writing on top of the data value
    TADDR               supportedFlags;

#ifdef FEATURE_PREJIT
    struct  HotItem
    {
        DWORD   rid;
        TADDR   value;
        static int __cdecl Cmp(const void* a_, const void* b_);
    };
    DWORD               dwNumHotItems;
    ArrayDPTR(HotItem)  hotItemList;
    PTR_TADDR FindHotItemValuePtr(DWORD rid);

    //
    // Compressed map support
    //
    PTR_CBYTE           pIndex;             // Bookmark for every kLookupMapIndexStride'th entry in the table
    DWORD               cIndexEntryBits;    // Number of bits in every index entry
    DWORD               cbTable;            // Number of bytes of compressed table data at pTable
    DWORD               cbIndex;            // Number of bytes of index data at pIndex
    BYTE                rgEncodingLengths[kLookupMapLengthEntries]; // Table of delta encoding lengths for
                                                                    // compressed values

    // Returns true if this map instance is compressed (this can only happen at runtime when running against
    // an ngen image). Currently and for the forseeable future only TypeDefToMethodTable and MethodDefToDesc
    // tables can be compressed.
    bool MapIsCompressed()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return pIndex != NULL;
    }

protected:
    // Internal routine used to iterate though one entry in the compressed table.
    INT32 GetNextCompressedEntry(BitStreamReader *pTableStream, INT32 iLastValue);

public:
    // Public method used to retrieve the full value (non-RVA) of a compressed table entry.
    TADDR GetValueFromCompressedMap(DWORD rid);

#ifndef DACCESS_COMPILE
    void CreateHotItemList(DataImage *image, CorProfileData *profileData, int table, BOOL fSkipNullEntries = FALSE);
    void Save(DataImage *image, DataImage::ItemKind kind, CorProfileData *profileData, int table, BOOL fCopyValues = FALSE);
    void SaveUncompressedMap(DataImage *image, DataImage::ItemKind kind, BOOL fCopyValues = FALSE);
    void ConvertSavedMapToUncompressed(DataImage *image, DataImage::ItemKind kind);
    void Fixup(DataImage *image, BOOL fFixupEntries = TRUE);
#endif // !DACCESS_COMPILE

#ifdef _DEBUG
    void    CheckConsistentHotItemList();
#endif

#endif // FEATURE_PREJIT

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags,
                           bool enumThis);
    void ListEnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif // DACCESS_COMPILE

    PTR_TADDR GetIndexPtr(DWORD index)
    {
        LIMITED_METHOD_DAC_CONTRACT;
#ifdef FEATURE_PREJIT
        _ASSERTE(!MapIsCompressed());
#endif // FEATURE_PREJIT
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
        _ASSERTE(!MapIsCompressed());
        _ASSERTE(dwNumHotItems == 0);

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
#ifdef FEATURE_PREJIT
        // Support for iterating compressed maps.
        INT32 m_currentEntry;           // RVA of current entry value
        BitStreamReader m_tableStream;  // Our current context in the compressed bit stream
#endif // FEATURE_PREJIT
    };
};

// Place holder types for RID maps that store cross-module references

class TypeRef { };
typedef DPTR(class TypeRef) PTR_TypeRef;

class MemberRef { };
typedef DPTR(class MemberRef) PTR_MemberRef;


// flag used to mark member ref pointers to field descriptors in the member ref cache
#define IS_FIELD_MEMBER_REF ((TADDR)0x00000002)


#ifdef FEATURE_PREJIT
//
// NGen image layout information that we need to quickly access at runtime
//
typedef DPTR(struct NGenLayoutInfo) PTR_NGenLayoutInfo;
struct NGenLayoutInfo
{
    // One range for each hot, unprofiled, cold code sections
    MemoryRange             m_CodeSections[3];

    // Pointer to the RUNTIME_FUNCTION table for hot, unprofiled, and cold code sections.
    PTR_RUNTIME_FUNCTION    m_pRuntimeFunctions[3];

    // Number of RUNTIME_FUNCTIONs for hot, unprofiled, and cold code sections.
    DWORD                   m_nRuntimeFunctions[3];

    // A parallel arrays of MethodDesc RVAs for hot and unprofiled methods. Both of the array are parallel for m_pRuntimeFunctions
    // The first array is for hot methods. The second array is for unprofiled methods.
    PTR_DWORD               m_MethodDescs[2];

    // Lookup table to speed up RUNTIME_FUNCTION lookup.
    // The first array is for hot methods. The second array is for unprofiled methods.
    // Number of elements is m_UnwindInfoLookupTableEntryCount + 1.
    // Last element of the lookup table is a sentinal entry that's good to cover the rest of the code section.
    // Values are indices into m_pRuntimeFunctions array.
    PTR_DWORD               m_UnwindInfoLookupTable[2];

    // Count of lookup entries in m_UnwindInfoLookupTable
    DWORD                   m_UnwindInfoLookupTableEntryCount[2];

    // Map for matching the cold code with hot code. Index is relative position of RUNTIME_FUNCTION within the section.
    PTR_CORCOMPILE_COLD_METHOD_ENTRY m_ColdCodeMap;

    // One range for each hot, cold, write, hot writeable, and cold writeable precode sections
    MemoryRange             m_Precodes[4];

    MemoryRange             m_JumpStubs;
    MemoryRange             m_StubLinkStubs;
    MemoryRange             m_VirtualMethodThunks;
    MemoryRange             m_ExternalMethodThunks;
    MemoryRange             m_ExceptionInfoLookupTable;

    PCODE                   m_pPrestubJumpStub;
#ifdef HAS_FIXUP_PRECODE
    PCODE                   m_pPrecodeFixupJumpStub;
#endif
    PCODE                   m_pVirtualImportFixupJumpStub;
    PCODE                   m_pExternalMethodFixupJumpStub;
    DWORD                   m_rvaFilterPersonalityRoutine;
};
#endif // FEATURE_PREJIT


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
    DPTR(RelativePointer<PTR_MethodTable>) ppMT; // size is numElements
    PTR_ClassCtorInfoEntry  cctorInfoHot;   // size is numElementsHot
    PTR_ClassCtorInfoEntry  cctorInfoCold;  // size is numElements-numElementsHot

    PTR_DWORD               hotHashOffsets;  // Indices to the start of each "hash region" in the hot part of the ppMT array. 
    PTR_DWORD               coldHashOffsets; // Indices to the start of each "hash region" in the cold part of the ppMT array. 
    DWORD                   numHotHashes;
    DWORD                   numColdHashes;

    ArrayDPTR(RelativeFixupPointer<PTR_MethodTable>) ppHotGCStaticsMTs;            // hot table
    ArrayDPTR(RelativeFixupPointer<PTR_MethodTable>) ppColdGCStaticsMTs;           // cold table

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

    ArrayDPTR(RelativeFixupPointer<PTR_MethodTable>) GetGCStaticMTs(DWORD index);

    PTR_MethodTable GetMT(DWORD i)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return ppMT[i].GetValue(dac_cast<TADDR>(ppMT) + i * sizeof(RelativePointer<PTR_MethodTable>));
    }

#ifdef FEATURE_PREJIT

    void AddElement(MethodTable *pMethodTable);
    void Save(DataImage *image, CorProfileData *profileData);
    void Fixup(DataImage *image);

    class ClassCtorInfoEntryArraySort : public CQuickSort<DWORD>
    {
    private:
        DPTR(RelativePointer<PTR_MethodTable>) m_pBase1;

    public:
        //Constructor
        ClassCtorInfoEntryArraySort(DWORD *base, DPTR(RelativePointer<PTR_MethodTable>) base1, int count)
          : CQuickSort<DWORD>(base, count)
        {
            WRAPPER_NO_CONTRACT;

            m_pBase1 = base1;
        }
        
        //Returns -1,0,or 1 if first's nativeStartOffset is less than, equal to, or greater than second's
        FORCEINLINE int Compare(DWORD *first, DWORD *second)
        {
            LIMITED_METHOD_CONTRACT;
        
            if (*first < *second)
                return -1;
            else if (*first == *second)
                return 0;
            else
                return 1;
        }
        
#ifndef DACCESS_COMPILE
        // Swap is overwriten so that we can sort both the MethodTable pointer
        // array and the ClassCtorInfoEntry array in parrallel.
        FORCEINLINE void Swap(SSIZE_T iFirst, SSIZE_T iSecond)
        {
            LIMITED_METHOD_CONTRACT;

            DWORD sTemp;
            PTR_MethodTable sTemp1;

            if (iFirst == iSecond) return;

            sTemp = m_pBase[iFirst];
            m_pBase[iFirst] = m_pBase[iSecond];
            m_pBase[iSecond] = sTemp;

            sTemp1 = m_pBase1[iFirst].GetValueMaybeNull();
            m_pBase1[iFirst].SetValueMaybeNull(m_pBase1[iSecond].GetValueMaybeNull());
            m_pBase1[iSecond].SetValueMaybeNull(sTemp1);
        }
#endif // !DACCESS_COMPILE
    };
#endif // FEATURE_PREJIT
};



#ifdef FEATURE_PREJIT

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


#define METHODTABLE_RESTORE_REASON() \
    RESTORE_REASON_FUNC(CanNotPreRestoreHardBindToParentMethodTable) \
    RESTORE_REASON_FUNC(CanNotPreRestoreHardBindToCanonicalMethodTable) \
    RESTORE_REASON_FUNC(CrossModuleNonCanonicalMethodTable) \
    RESTORE_REASON_FUNC(CanNotHardBindToInstanceMethodTableChain) \
    RESTORE_REASON_FUNC(GenericsDictionaryNeedsRestore) \
    RESTORE_REASON_FUNC(InterfaceIsGeneric) \
    RESTORE_REASON_FUNC(CrossModuleGenericsStatics) \
    RESTORE_REASON_FUNC(ComImportStructDependenciesNeedRestore) \
    RESTORE_REASON_FUNC(CrossAssembly) \
    RESTORE_REASON_FUNC(ArrayElement) \
    RESTORE_REASON_FUNC(ProfilingEnabled)

#undef RESTORE_REASON_FUNC
#define RESTORE_REASON_FUNC(s) s ,
typedef enum
{

    METHODTABLE_RESTORE_REASON()

    TotalMethodTables
} MethodTableRestoreReason;
#undef RESTORE_REASON_FUNC

class NgenStats
{
public:
    NgenStats()
    {
        LIMITED_METHOD_CONTRACT;
        memset (MethodTableRestoreNumReasons, 0, sizeof(DWORD)*(TotalMethodTables+1));
    }

    DWORD MethodTableRestoreNumReasons[TotalMethodTables + 1];
};
#endif // FEATURE_PREJIT

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


// ESymbolFormat specified the format used by a symbol stream
typedef enum 
{
    eSymbolFormatNone,      /* symbol format to use not yet determined */
    eSymbolFormatPDB,       /* PDB format from diasymreader.dll - only safe for trusted scenarios */
    eSymbolFormatILDB       /* ILDB format from ildbsymbols.dll */
}ESymbolFormat;


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

#if defined(FEATURE_PREJIT) && !defined(DACCESS_COMPILE)

public:
    void Save(DataImage *pImage, CorProfileData *pProfileData);
    void Fixup(DataImage *pImage);

private:
    // We save all entries
    bool ShouldSave(DataImage *pImage, GuidToMethodTableEntry *pEntry)
    { LIMITED_METHOD_CONTRACT; return true; }

    bool IsHotEntry(GuidToMethodTableEntry *pEntry, CorProfileData *pProfileData)
    { LIMITED_METHOD_CONTRACT; return false; }

    bool SaveEntry(DataImage *pImage, CorProfileData *pProfileData, 
                        GuidToMethodTableEntry *pOldEntry, GuidToMethodTableEntry *pNewEntry, 
                        EntryMappingTable *pMap);

    void FixupEntry(DataImage *pImage, GuidToMethodTableEntry *pEntry, void *pFixupBase, DWORD cbFixupOffset);
    
#endif // FEATURE_PREJIT && !DACCESS_COMPILE

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

#if defined(FEATURE_PREJIT) && !defined(DACCESS_COMPILE)

    void Fixup(DataImage *pImage)
    {
        WRAPPER_NO_CONTRACT;
        BaseFixup(pImage);
    }

    void Save(DataImage *pImage, CorProfileData *pProfileData);


private:
    bool ShouldSave(DataImage *pImage, MemberRefToDescHashEntry *pEntry)
    {
        return IsHotEntry(pEntry, NULL);
    }

    bool IsHotEntry(MemberRefToDescHashEntry *pEntry, CorProfileData *pProfileData) // yes according to IBC data
    {
		LIMITED_METHOD_CONTRACT;

        _ASSERTE(pEntry != NULL);
		// Low order bit of data field indicates a hot entry.
		return (pEntry->m_value & 0x1) != 0;

    }


    bool SaveEntry(DataImage *pImage, CorProfileData *pProfileData, 
                        MemberRefToDescHashEntry *pOldEntry, MemberRefToDescHashEntry *pNewEntry, 
                        EntryMappingTable *pMap)
    {
        //The entries are mutable
        return FALSE;
    }

    void FixupEntry(DataImage *pImage, MemberRefToDescHashEntry *pEntry, void *pFixupBase, DWORD cbFixupOffset);

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

    MethodDesc              *m_pDllMain;

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

    // Format the above stream is in (if any)
    ESymbolFormat           m_symbolFormat;

    // For protecting additions to the heap
    CrstExplicitInit        m_LookupTableCrst;

    #define TYPE_DEF_MAP_ALL_FLAGS                    ((TADDR)0x00000001)
        #define ZAPPED_TYPE_NEEDS_NO_RESTORE          ((TADDR)0x00000001)

    #define TYPE_REF_MAP_ALL_FLAGS                    NO_MAP_FLAGS
        // For type ref map, 0x1 cannot be used as a flag: reserved for FIXUP_POINTER_INDIRECTION bit
        // For type ref map, 0x2 cannot be used as a flag: reserved for TypeHandle to signify TypeDesc

    #define METHOD_DEF_MAP_ALL_FLAGS                  NO_MAP_FLAGS

    #define FIELD_DEF_MAP_ALL_FLAGS                   NO_MAP_FLAGS

    #define MEMBER_REF_MAP_ALL_FLAGS                  ((TADDR)0x00000003)
	// For member ref hash table, 0x1 is reserved for IsHot bit
        #define IS_FIELD_MEMBER_REF                   ((TADDR)0x00000002)      // denotes that target is a FieldDesc

    #define GENERIC_PARAM_MAP_ALL_FLAGS               NO_MAP_FLAGS

    #define GENERIC_TYPE_DEF_MAP_ALL_FLAGS            ((TADDR)0x00000001)
        #define ZAPPED_GENERIC_TYPE_NEEDS_NO_RESTORE  ((TADDR)0x00000001)

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

#ifdef PROFILING_SUPPORTED_DATA 
     // a wrapper for the underlying PEFile metadata emitter which validates that the metadata edits being
     // made are supported modifications to the type system
     VolatilePtr<IMetaDataEmit> m_pValidatedEmitter;
#endif

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

#ifdef FEATURE_PREJIT
    // Mapping from tokens to IL marshaling stubs (NGEN only).
    PTR_StubMethodHashTable m_pStubMethodHashTable;
#endif // FEATURE_PREJIT

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
    friend class MscorlibBinder;
    PTR_MscorlibBinder      m_pBinder;

public:
    BOOL IsCollectible()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return (m_dwPersistedFlags & COLLECTIBLE_MODULE) != 0;
    }

#ifdef FEATURE_READYTORUN
private:
    PTR_ReadyToRunInfo      m_pReadyToRunInfo;
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
#endif // PROFILING_SUPPORTED_DATA

#ifdef FEATURE_PREJIT
    PTR_NGenLayoutInfo      m_pNGenLayoutInfo;

#if defined(FEATURE_COMINTEROP)
        public:

        #ifndef DACCESS_COMPILE
            BOOL CanCacheWinRTTypeByGuid(MethodTable *pMT);
            void CacheWinRTTypeByGuid(PTR_MethodTable pMT, PTR_GuidInfo pgi = NULL);
        #endif // !DACCESS_COMPILE

            PTR_MethodTable LookupTypeByGuid(const GUID & guid);
            void GetCachedWinRTTypes(SArray<PTR_MethodTable> * pTypes, SArray<GUID> * pGuids);

        private:
            PTR_GuidToMethodTableHashTable m_pGuidToTypeHash;   // A map from GUID to Type, for the "WinRT-interesting" types

#endif // defined(FEATURE_COMINTEROP)

    // Module wide static fields information
    ModuleCtorInfo          m_ModuleCtorInfo;

#endif // FEATURE_PREJIT

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

#ifdef FEATURE_PREJIT
    // Stats for prejit log
    NgenStats                *m_pNgenStats;
#endif // FEATURE_PREJIT


protected:

    void CreateDomainThunks();

protected:
    void DoInit(AllocMemTracker *pamTracker, LPCWSTR szName);

protected:
#ifndef DACCESS_COMPILE
    virtual void Initialize(AllocMemTracker *pamTracker, LPCWSTR szName = NULL);
    void InitializeForProfiling();
#ifdef FEATURE_PREJIT 
    void InitializeNativeImage(AllocMemTracker* pamTracker);
#endif
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
#ifdef  FEATURE_PREJIT
    void DeleteNativeCodeRanges();
#endif
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
        // We are seeing cases where this flag is set for a module that is not an EditAndContinueModule.  This should
        // never happen unless the module is EditAndContinueCapable, in which case we would have created an EditAndContinueModule
        // not a Module.  
        //_ASSERTE((m_dwTransientFlags & IS_EDIT_AND_CONTINUE) == 0 || IsEditAndContinueCapable());
        return (IsEditAndContinueCapable()) && ((m_dwTransientFlags & IS_EDIT_AND_CONTINUE) != 0); 
    }

    BOOL IsEditAndContinueCapable();
    
    BOOL IsIStream() { LIMITED_METHOD_CONTRACT; return GetFile()->IsIStream(); }

    BOOL IsSystem() { WRAPPER_NO_CONTRACT; SUPPORTS_DAC; return m_file->IsSystem(); }

    static BOOL IsEditAndContinueCapable(Assembly *pAssembly, PEFile *file);

    void EnableEditAndContinue()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        // _ASSERTE(IsEditAndContinueCapable());
        LOG((LF_ENC, LL_INFO100, "EnableEditAndContinue: this:0x%x, %s\n", this, GetDebugName()));
        m_dwTransientFlags |= IS_EDIT_AND_CONTINUE;
    }

    void DisableEditAndContinue()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        // don't _ASSERTE(IsEditAndContinueCapable());
        LOG((LF_ENC, LL_INFO100, "DisableEditAndContinue: this:0x%x, %s\n", this, GetDebugName()));
        m_dwTransientFlags = m_dwTransientFlags.Load() & (~IS_EDIT_AND_CONTINUE);
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

#if defined(PROFILING_SUPPORTED) && !defined(CROSSGEN_COMPILE) 
    IMetaDataEmit *GetValidatedEmitter();
#endif

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

    BOOL IsWindowsRuntimeModule();

    BOOL IsInCurrentVersionBubble();

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
    // and by dynamic modules.  Callers that actually do anything with the return value will almost
    // certainly want to check GetInMemorySymbolStreamFormat to know how to interpret the bytes
    // in the stream.
    PTR_CGrowableStream GetInMemorySymbolStream()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;

        // Symbol format should be "none" if-and-only-if our stream is null
        // If this fails, it may mean somebody is trying to examine this module after 
        // code:Module::Destruct has been called.
        _ASSERTE( (m_symbolFormat == eSymbolFormatNone) == (m_pIStreamSym == NULL) );

        return m_pIStreamSym;
    }

    // Get the format of the in-memory symbol stream for this module, or 
    // eSymbolFormatNone if no in-memory symbols.
    ESymbolFormat GetInMemorySymbolStreamFormat()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;

        // Symbol format should be "none" if-and-only-if our stream is null
        // If this fails, it may mean somebody is trying to examine this module after 
        // code:Module::Destruct has been called.
        _ASSERTE( (m_symbolFormat == eSymbolFormatNone) == (m_pIStreamSym == NULL) );

        return m_symbolFormat;
    }

#ifndef DACCESS_COMPILE
    // Set the in-memory stream for debug symbols
    // This must only be called when there is no existing stream.
    // This takes an AddRef on the supplied stream.
    void SetInMemorySymbolStream(CGrowableStream *pStream, ESymbolFormat symbolFormat)
    {
        LIMITED_METHOD_CONTRACT;

        // Must have provided valid stream data
        CONSISTENCY_CHECK(pStream != NULL);
        CONSISTENCY_CHECK(symbolFormat != eSymbolFormatNone);

        // we expect set to only be called once
        CONSISTENCY_CHECK(m_pIStreamSym == NULL);
        CONSISTENCY_CHECK(m_symbolFormat == eSymbolFormatNone);    

        m_symbolFormat = symbolFormat;
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
            // We could set m_symbolFormat to eSymbolFormatNone to be consistent with not having
            // a stream, but no-one should be trying to look at it after destruct time, so it's
            // better to leave it inconsistent and get an ASSERT if someone tries to examine the
            // module's sybmol stream after the module was destructed.
        }
    }

    // Release the symbol reader if any
    // Caller is responsible for aquiring the reader lock if this could occur
    // concurrently with other uses of the reader (i.e. not shutdown/unload time)
    void ReleaseISymUnmanagedReader(void);

    virtual void ReleaseILData();


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

#ifdef FEATURE_PREJIT    
    // Gets or creates the token -> IL stub MethodDesc hash.
    StubMethodHashTable *GetStubMethodHashTable();
#endif // FEATURE_PREJIT    

    // Creates a new Method table for an array.  Used to make type handles
    // Note that if kind == SZARRAY or ARRAY, we get passed the GENERIC_ARRAY
    // needed to create the array.  That way we dont need to load classes during
    // the class load, which avoids the need for a 'being loaded' list
    MethodTable* CreateArrayMethodTable(TypeHandle elemType, CorElementType kind, unsigned rank, class AllocMemTracker *pamTracker);

    // This is called from CreateArrayMethodTable
    MethodTable* CreateGenericArrayMethodTable(TypeHandle elemType);

    // string helper
    void InitializeStringData(DWORD token, EEStringData *pstrData, CQuickBytes *pqb);

    // Resolving
    OBJECTHANDLE ResolveStringRef(DWORD Token, BaseDomain *pDomain, bool bNeedToSyncWithFixups);
#ifdef FEATURE_PREJIT
    OBJECTHANDLE ResolveStringRefHelper(DWORD token, BaseDomain *pDomain, PTR_CORCOMPILE_IMPORT_SECTION pSection, EEStringData *strData);
#endif
    
    CHECK CheckStringRef(RVA rva);

    // Module/Assembly traversal
    Assembly * GetAssemblyIfLoaded(
            mdAssemblyRef       kAssemblyRef, 
            LPCSTR              szWinRtNamespace = NULL, 
            LPCSTR              szWinRtClassName = NULL, 
            IMDInternalImport * pMDImportOverride = NULL,
            BOOL                fDoNotUtilizeExtraChecks = FALSE,
            ICLRPrivBinder      *pBindingContextForLoadedAssembly = NULL
            );

private:
    // Helper function used by GetAssemblyIfLoaded. Do not call directly.
    Assembly *GetAssemblyIfLoadedFromNativeAssemblyRefWithRefDefMismatch(mdAssemblyRef kAssemblyRef, BOOL *pfDiscoveredAssemblyRefMatchesTargetDefExactly);
public:

    DomainAssembly * LoadAssembly(
            mdAssemblyRef kAssemblyRef, 
            LPCUTF8       szWinRtTypeNamespace = NULL,
            LPCUTF8       szWinRtTypeClassName = NULL);
    Module *GetModuleIfLoaded(mdFile kFile, BOOL onlyLoadedInAppDomain, BOOL loadAllowed);
    DomainFile *LoadModule(AppDomain *pDomain, mdFile kFile, BOOL loadResources = TRUE, BOOL bindOnly = FALSE);
    PTR_Module LookupModule(mdToken kFile, BOOL loadResources = TRUE); //wrapper over GetModuleIfLoaded, takes modulerefs as well
    DWORD GetAssemblyRefFlags(mdAssemblyRef tkAssemblyRef);

    bool HasBindableIdentity(mdAssemblyRef tkAssemblyRef)
    { 
        WRAPPER_NO_CONTRACT; 
        return !IsAfContentType_WindowsRuntime(GetAssemblyRefFlags(tkAssemblyRef)); 
    }

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
            if (!IsCompilationProcess() && (flags & ZAPPED_TYPE_NEEDS_NO_RESTORE))
            {
                // Make sure the flag is consistent with the target data and implies the load level we think it does
                _ASSERTE(th.AsMethodTable()->IsPreRestored());
                _ASSERTE(th.GetLoadLevel() == CLASS_LOADED);

                *pLoadLevel = CLASS_LOADED;
            }
            else
            {
                *pLoadLevel = th.GetLoadLevel();
            }
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
            if (!IsCompilationProcess() && (flags & ZAPPED_GENERIC_TYPE_NEEDS_NO_RESTORE))
            {
                // Make sure the flag is consistent with the target data and implies the load level we think it does
                _ASSERTE(th.AsMethodTable()->IsPreRestored());
                _ASSERTE(th.GetLoadLevel() == CLASS_LOADED);

                *pLoadLevel = CLASS_LOADED;
            }
            else
            {
                *pLoadLevel = th.GetLoadLevel();
            }
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

    mdTypeRef LookupTypeRefByMethodTable(MethodTable *pMT);

    mdMemberRef LookupMemberRefByMethodDesc(MethodDesc *pMD);

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

#ifdef FEATURE_PREJIT
    void FinalizeLookupMapsPreSave(DataImage *image);
#endif

    DWORD GetAssemblyRefMax() {LIMITED_METHOD_CONTRACT;  return m_ManifestModuleReferencesMap.GetSize(); }

    MethodDesc *FindMethodThrowing(mdToken pMethod);
    MethodDesc *FindMethod(mdToken pMethod);

    void PopulatePropertyInfoMap();
    HRESULT GetPropertyInfoForMethodDef(mdMethodDef md, mdProperty *ppd, LPCSTR *pName, ULONG *pSemantic);

    #define NUM_PROPERTY_SET_HASHES 4
#ifdef FEATURE_PREJIT
    void PrecomputeMatchingProperties(DataImage *image);
#endif
    BOOL MightContainMatchingProperty(mdProperty tkProperty, ULONG nameHash);

private:
    ArrayDPTR(BYTE)    m_propertyNameSet;
    DWORD              m_nPropertyNameSet;

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

    BOOL HasNativeOrReadyToRunInlineTrackingMap();
    COUNT_T GetNativeOrReadyToRunInliners(PTR_Module inlineeOwnerMod, mdMethodDef inlineeTkn, COUNT_T inlinersSize, MethodInModule inliners[], BOOL *incompleteData);
#if defined(PROFILING_SUPPORTED) && !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)
    BOOL HasJitInlineTrackingMap();
    PTR_JITInlineTrackingMap GetJitInlineTrackingMap() { LIMITED_METHOD_CONTRACT; return m_pJitInlinerTrackingMap; }
    void AddInlining(MethodDesc *inliner, MethodDesc *inlinee);
#endif // defined(PROFILING_SUPPORTED) && !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)

public:
    void NotifyEtwLoadFinished(HRESULT hr);

    // Enregisters a VASig.
    VASigCookie *GetVASigCookie(Signature vaSignature);

    // DLL entry point
    MethodDesc *GetDllEntryPoint()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pDllMain;
    }
    void SetDllEntryPoint(MethodDesc *pMD)
    {
        LIMITED_METHOD_CONTRACT;
        m_pDllMain = pMD;
    }

#ifdef FEATURE_PREJIT
    // This data is only valid for NGEN'd modules, and for modules we're creating at NGEN time.
    ModuleCtorInfo* GetZapModuleCtorInfo()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return &m_ModuleCtorInfo;
    }
#endif

 private:


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

#ifdef FEATURE_PREJIT
    BOOL HasNativeImage() 
    { 
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;
        return m_file->HasNativeImage();
    }
    
    PEImageLayout *GetNativeImage()
    {
        CONTRACT(PEImageLayout *)
        {
            PRECONDITION(m_file->HasNativeImage());
            POSTCONDITION(CheckPointer(RETVAL));
            NOTHROW;
            GC_NOTRIGGER;
            SUPPORTS_DAC;
            CANNOT_TAKE_LOCK;
        }
        CONTRACT_END;

        _ASSERTE(!IsCollectible());
        RETURN m_file->GetLoadedNative();
    }
#else
    BOOL HasNativeImage()
    {
        LIMITED_METHOD_CONTRACT;
        return FALSE;
    }

    PEImageLayout * GetNativeImage()
    {
        // Should never get here
        PRECONDITION(HasNativeImage());
        return NULL;
    }
#endif // FEATURE_PREJIT


    BOOL            HasNativeOrReadyToRunImage();
    PEImageLayout * GetNativeOrReadyToRunImage();
    PTR_CORCOMPILE_IMPORT_SECTION GetImportSections(COUNT_T *pCount);
    PTR_CORCOMPILE_IMPORT_SECTION GetImportSectionFromIndex(COUNT_T index);
    PTR_CORCOMPILE_IMPORT_SECTION GetImportSectionForRVA(RVA rva);

    // These are overridden by reflection modules
    virtual TADDR GetIL(RVA il);

    virtual PTR_VOID GetRvaField(RVA field, BOOL fZapped);
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

#ifdef FEATURE_PREJIT
    BOOL IsZappedCode(PCODE code);
    BOOL IsZappedPrecode(PCODE code);

    CORCOMPILE_DEBUG_ENTRY GetMethodDebugInfoOffset(MethodDesc *pMD);
    PTR_BYTE GetNativeDebugInfo(MethodDesc * pMD);

    // The methods below must be called when loading back an ngen'ed image for any fields that
    // might be an encoded token (rather than a hard pointer) and/or need a restore operation
    //
    static void RestoreMethodTablePointerRaw(PTR_MethodTable * ppMT,
                                             Module *pContainingModule = NULL,
                                             ClassLoadLevel level = CLASS_LOADED);
    static void RestoreTypeHandlePointerRaw(TypeHandle *pHandle,
                                            Module *pContainingModule = NULL,
                                            ClassLoadLevel level = CLASS_LOADED);
    static void RestoreMethodDescPointerRaw(PTR_MethodDesc * ppMD,
                                            Module *pContainingModule = NULL,
                                            ClassLoadLevel level = CLASS_LOADED);

    static void RestoreMethodTablePointer(FixupPointer<PTR_MethodTable> * ppMT,
                                          Module *pContainingModule = NULL,
                                          ClassLoadLevel level = CLASS_LOADED);
    static void RestoreTypeHandlePointer(FixupPointer<TypeHandle> *pHandle,
                                         Module *pContainingModule = NULL,
                                         ClassLoadLevel level = CLASS_LOADED);
    static void RestoreMethodDescPointer(FixupPointer<PTR_MethodDesc> * ppMD,
                                          Module *pContainingModule = NULL,
                                          ClassLoadLevel level = CLASS_LOADED);

    static void RestoreMethodTablePointer(RelativeFixupPointer<PTR_MethodTable> * ppMT,
                                          Module *pContainingModule = NULL,
                                         ClassLoadLevel level = CLASS_LOADED);
    static void RestoreTypeHandlePointer(RelativeFixupPointer<TypeHandle> *pHandle,
                                         Module *pContainingModule = NULL,
                                      ClassLoadLevel level = CLASS_LOADED);
    static void RestoreMethodDescPointer(RelativeFixupPointer<PTR_MethodDesc> * ppMD,
                                         Module *pContainingModule = NULL,
                                         ClassLoadLevel level = CLASS_LOADED);
    static void RestoreFieldDescPointer(RelativeFixupPointer<PTR_FieldDesc> * ppFD);

    static void RestoreModulePointer(RelativeFixupPointer<PTR_Module> * ppModule, Module *pContainingModule);

    static PTR_Module RestoreModulePointerIfLoaded(DPTR(RelativeFixupPointer<PTR_Module>) ppModule, Module *pContainingModule);

    PCCOR_SIGNATURE GetEncodedSig(RVA fixupRva, Module **ppDefiningModule);
    PCCOR_SIGNATURE GetEncodedSigIfLoaded(RVA fixupRva, Module **ppDefiningModule);
#endif

    BYTE* GetNativeFixupBlobData(RVA fixup);

    IMDInternalImport *GetNativeAssemblyImport(BOOL loadAllowed = TRUE);
    IMDInternalImport *GetNativeAssemblyImportIfLoaded();

    BOOL FixupNativeEntry(CORCOMPILE_IMPORT_SECTION * pSection, SIZE_T fixupIndex, SIZE_T *fixup);

    //this split exists to support new CLR Dump functionality in DAC.  The
    //template removes any indirections.
    BOOL FixupDelayList(TADDR pFixupList);

    template<typename Ptr, typename FixupNativeEntryCallback>
    BOOL FixupDelayListAux(TADDR pFixupList,
                           Ptr pThis, FixupNativeEntryCallback pfnCB,
                           PTR_CORCOMPILE_IMPORT_SECTION pImportSections, COUNT_T nImportSections,
                           PEDecoder * pNativeImage);
    void RunEagerFixups();

    Module *GetModuleFromIndex(DWORD ix);
    Module *GetModuleFromIndexIfLoaded(DWORD ix);

#ifdef FEATURE_PREJIT
    // This is to rebuild stub dispatch maps to module-local values.
    void UpdateStubDispatchTypeTable(DataImage *image);

    void SetProfileData(CorProfileData * profileData);
    CorProfileData *GetProfileData();

    mdTypeDef     LookupIbcTypeToken(  Module *   pExternalModule, mdToken ibcToken, SString* optionalFullNameOut = NULL);
    mdMethodDef   LookupIbcMethodToken(TypeHandle enclosingType,   mdToken ibcToken, SString* optionalFullNameOut = NULL);

    TypeHandle    LoadIBCTypeHelper(DataImage *image, CORBBTPROF_BLOB_PARAM_SIG_ENTRY *pBlobSigEntry);
    MethodDesc *  LoadIBCMethodHelper(DataImage *image, CORBBTPROF_BLOB_PARAM_SIG_ENTRY *pBlobSigEntry);
 

    void ExpandAll(DataImage *image);
    // profileData may be different than the profileData passed in to
    // ExpandAll() depending on more information that may now be available
    // (after all the methods have been compiled)

    void Save(DataImage *image);
    void Arrange(DataImage *image);
    void PlaceType(DataImage *image, TypeHandle th, DWORD profilingFlags);
    void PlaceMethod(DataImage *image, MethodDesc *pMD, DWORD profilingFlags);
    void Fixup(DataImage *image);

    bool AreAllClassesFullyLoaded();

    // Precompute type-specific auxiliary information saved into NGen image
    void PrepareTypesForSave(DataImage *image);

    static void SaveMethodTable(DataImage *image,
                                MethodTable *pMT,
                                DWORD profilingFlags);

    static void SaveTypeHandle(DataImage *image,
                               TypeHandle t,
                               DWORD profilingFlags);

private:
    static BOOL CanEagerBindTo(Module *targetModule, Module *pPreferredZapModule, void *address);
public:

    static PTR_Module ComputePreferredZapModule(Module * pDefinitionModule,        // the module that declares the generic type or method
                                                Instantiation classInst,           // the type arguments to the type (if any)
                                                Instantiation methodInst = Instantiation()); // the type arguments to the method (if any)

    static PTR_Module ComputePreferredZapModuleHelper(Module * pDefinitionModule,
                                                      Instantiation classInst,
                                                      Instantiation methodInst);

    static PTR_Module ComputePreferredZapModule(TypeKey * pKey);

    // Return true if types or methods of this instantiation are *always* precompiled and saved
    // in the preferred zap module
    // At present, only true for <__Canon,...,__Canon> instantiation
    static BOOL IsAlwaysSavedInPreferredZapModule(Instantiation classInst,
                                                  Instantiation methodInst = Instantiation());

    static PTR_Module GetPreferredZapModuleForTypeHandle(TypeHandle t);
    static PTR_Module GetPreferredZapModuleForMethodTable(MethodTable * pMT);
    static PTR_Module GetPreferredZapModuleForMethodDesc(const MethodDesc * pMD);
    static PTR_Module GetPreferredZapModuleForFieldDesc(FieldDesc * pFD);
    static PTR_Module GetPreferredZapModuleForTypeDesc(PTR_TypeDesc pTD);

    void PrepopulateDictionaries(DataImage *image, BOOL nonExpansive);


    void LoadTokenTables();
    void LoadHelperTable();

    PTR_NGenLayoutInfo GetNGenLayoutInfo()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_pNGenLayoutInfo;
    }

    PCODE GetPrestubJumpStub()
    { 
        LIMITED_METHOD_DAC_CONTRACT; 

        if (!m_pNGenLayoutInfo)
            return NULL;

        return m_pNGenLayoutInfo->m_pPrestubJumpStub;
    }

#ifdef HAS_FIXUP_PRECODE
    PCODE GetPrecodeFixupJumpStub()
    { 
        LIMITED_METHOD_DAC_CONTRACT; 

        if (!m_pNGenLayoutInfo)
            return NULL;

        return m_pNGenLayoutInfo->m_pPrecodeFixupJumpStub;
    }
#endif

    BOOL IsVirtualImportThunk(PCODE code)
    {
        LIMITED_METHOD_DAC_CONTRACT;

        if (!m_pNGenLayoutInfo)
            return FALSE;

        return m_pNGenLayoutInfo->m_VirtualMethodThunks.IsInRange(code);
    }
#endif // FEATURE_PREJIT

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

#ifdef FEATURE_PREJIT
    BOOL AreTypeSpecsTriaged()
    {
        return m_dwTransientFlags & TYPESPECS_TRIAGED;
    }

    void SetTypeSpecsTriaged()
    {
        FastInterlockOr(&m_dwTransientFlags, TYPESPECS_TRIAGED);
    }

    BOOL IsModuleSaved()
    {
        return m_dwTransientFlags & MODULE_SAVED;
    }

    void SetIsModuleSaved()
    {
        FastInterlockOr(&m_dwTransientFlags, MODULE_SAVED);
    }

#endif  // FEATURE_PREJIT

    BOOL IsReadyToRun()
    {
        LIMITED_METHOD_DAC_CONTRACT;

#ifdef FEATURE_READYTORUN
        return m_pReadyToRunInfo != NULL;
#else
        return FALSE;
#endif
    }

#ifdef FEATURE_READYTORUN
    PTR_ReadyToRunInfo GetReadyToRunInfo()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_pReadyToRunInfo;
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

#ifdef FEATURE_PREJIT
    NgenStats *GetNgenStats()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pNgenStats;
    }
#endif // FEATURE_PREJIT

    // LoaderHeap for storing IJW thunks
    PTR_LoaderHeap           m_pThunkHeap;

    // Self-initializing accessor for IJW thunk heap
    LoaderHeap              *GetThunkHeap();
    // Self-initializing accessor for domain-independent IJW thunk heap
    LoaderHeap              *GetDllThunkHeap();

    void            EnumRegularStaticGCRefs        (promote_func* fn, ScanContext* sc);

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

    // This is a compressed read only copy of m_inlineTrackingMap, which is being saved to NGEN image.
    PTR_PersistentInlineTrackingMapNGen m_pPersistentInlineTrackingMapNGen;

#if defined(PROFILING_SUPPORTED) && !defined(DACCESS_COMPILE)
    PTR_JITInlineTrackingMap m_pJitInlinerTrackingMap;
#endif // defined(PROFILING_SUPPORTED) && !defined(DACCESS_COMPILE)


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

        _ASSERTE(rid <= GetNativeAssemblyImport()->GetCountWithTokenKind(mdtAssemblyRef));
        return NativeMetadataAssemblyRefMap[rid - 1];
    }

    void SetNativeMetadataAssemblyRefInCache(DWORD rid, PTR_Assembly pAssembly);
#endif // !defined(DACCESS_COMPILE)
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
    ICeeGen * m_pCeeFileGen;
private:
    Assembly             *m_pCreatingAssembly;
    ISymUnmanagedWriter  *m_pISymUnmanagedWriter;
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

    // If true, then only other transient modules can depend on this module.
    bool m_fIsTransient;

#if !defined DACCESS_COMPILE && !defined CROSSGEN_COMPILE
    // Returns true iff metadata capturing is suppressed
    bool IsMetadataCaptureSuppressed();

    // Toggle whether CaptureModuleMetaDataToMemory should do antyhing. This can be an important perf win to
    // allow batching up metadata capture. Use SuppressMetadataCaptureHolder to ensure they're balanced.
    // These are not nestable.
    void SuppressMetadataCapture();
    void ResumeMetadataCapture();

    // Glue functions for holders.
    static void SuppressCaptureWrapper(ReflectionModule * pModule)
    {
        pModule->SuppressMetadataCapture();
    }
    static void ResumeCaptureWrapper(ReflectionModule * pModule)
    {
        pModule->ResumeMetadataCapture();
    }

    ReflectionModule(Assembly *pAssembly, mdFile token, PEFile *pFile);
#endif // !DACCESS_COMPILE && !CROSSGEN_COMPILE

public:

#ifdef DACCESS_COMPILE
    // Accessor to expose m_pDynamicMetadata to debugger.
    PTR_SBuffer GetDynamicMetadataBuffer() const;
#endif

#if !defined DACCESS_COMPILE && !defined CROSSGEN_COMPILE
    static ReflectionModule *Create(Assembly *pAssembly, PEFile *pFile, AllocMemTracker *pamTracker, LPCWSTR szName, BOOL fIsTransient);
    void Initialize(AllocMemTracker *pamTracker, LPCWSTR szName);
    void Destruct();

    void ReleaseILData();
#endif // !DACCESS_COMPILE && !CROSSGEN_COMPILE

    // Overides functions to access sections
    virtual TADDR GetIL(RVA target);
    virtual PTR_VOID GetRvaField(RVA rva, BOOL fZapped);

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

    ICeeGen *GetCeeGen() {LIMITED_METHOD_CONTRACT;  return m_pCeeFileGen; }

    RefClassWriter *GetClassWriter()
    {
        LIMITED_METHOD_CONTRACT;

        return m_pInMemoryWriter;
    }

    ISymUnmanagedWriter *GetISymUnmanagedWriter()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pISymUnmanagedWriter;
    }

    // Note: we now use the same writer instance for the life of a module,
    // so there should no longer be any need for the extra indirection.
    ISymUnmanagedWriter **GetISymUnmanagedWriterAddr()
    {
        LIMITED_METHOD_CONTRACT;

        // We must have setup the writer before trying to get
        // the address for it. Any calls to this before a
        // SetISymUnmanagedWriter are very incorrect.
        _ASSERTE(m_pISymUnmanagedWriter != NULL);

        return &m_pISymUnmanagedWriter;
    }

    bool IsTransient()
    {
        LIMITED_METHOD_CONTRACT;

        return m_fIsTransient;
    }

    void SetIsTransient(bool fIsTransient)
    {
        LIMITED_METHOD_CONTRACT;

        m_fIsTransient = fIsTransient;
    }

#ifndef DACCESS_COMPILE
#ifndef CROSSGEN_COMPILE

    typedef Wrapper<
        ReflectionModule*, 
        ReflectionModule::SuppressCaptureWrapper, 
        ReflectionModule::ResumeCaptureWrapper> SuppressMetadataCaptureHolder;
#endif // !CROSSGEN_COMPILE

    HRESULT SetISymUnmanagedWriter(ISymUnmanagedWriter *pWriter)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            INJECT_FAULT(return E_OUTOFMEMORY;);
        }
        CONTRACTL_END


        // Setting to NULL when we've never set a writer before should
        // do nothing.
        if ((pWriter == NULL) && (m_pISymUnmanagedWriter == NULL))
            return S_OK;

        if (m_pISymUnmanagedWriter != NULL)
        {
            // We shouldn't be trying to replace an existing writer anymore
            _ASSERTE( pWriter == NULL );

            m_pISymUnmanagedWriter->Release();
        }
        
        m_pISymUnmanagedWriter = pWriter;
        return S_OK;
    }
#endif // !DACCESS_COMPILE

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

// Rerieve the full command line for the current process.
LPCWSTR GetManagedCommandLine();
// Save the command line for the current process.
void SaveManagedCommandLine(LPCWSTR pwzAssemblyPath, int argc, LPCWSTR *argv);

#endif // !CEELOAD_H_
