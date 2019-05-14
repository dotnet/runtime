// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// =============================================================================================
// Definitions for tracking method inlinings in NGen and R2R images.
// The only information stored is "who" got inlined "where", no offsets or inlining depth tracking. 
// (No good for debugger yet.)
// This information is later exposed to profilers and can be useful for ReJIT.
// Runtime inlining is not being tracked because profilers can deduce it via callbacks anyway.
//
// This file is made of two major component groups:
// a) InlineTrackingMap - This is a compilation time datastructure that holds an uncompressed
//    version of the inline tracking information. It is appended to as methods are compiled.
//    MethodInModule, InlineTrackingEntry, InlineTrackingMapTraits are all support infratsructure
//    in this group.
//
// b) PersistentInlineTrackingMap[R2R/NGen] - These are the types that understand the image persistence 
//    formats. At the end of image compilation one of them consumes all the data from an 
//    InlineTrackingMap to encode it. At runtime an instance will be constructed to read back 
//    the encoded data on demand. PersistantInlineTrackingMapR2R and PersistantInlineTrackingMapNGen
//    would nominally use a common base type or interface, but due to ngen binary serialization vtables
//    were avoided. See farther below for the different format descriptions.
// =============================================================================================

#ifndef INLINETRACKING_H_
#define INLINETRACKING_H_
#include "corhdr.h"
#include "shash.h"
#include "sarray.h"
#include "crsttypes.h"
#include "daccess.h"
#include "crossloaderallocatorhash.h"



// ---------------------------------- Compile time support ----------------------------------------------

class MethodDesc;
typedef DPTR(class MethodDesc)          PTR_MethodDesc;

class ZapHeap;

struct MethodInModule
{
    Module *m_module;
    mdMethodDef m_methodDef;

    bool operator <(const MethodInModule& other) const;

    bool operator ==(const MethodInModule& other) const;

    bool operator !=(const MethodInModule& other) const;

    MethodInModule(Module * module, mdMethodDef methodDef)
        :m_module(module), m_methodDef(methodDef)
    {
        LIMITED_METHOD_DAC_CONTRACT;
    }

    MethodInModule()
        :m_module(NULL), m_methodDef(0)
    {
        LIMITED_METHOD_DAC_CONTRACT;
    }

};

struct InlineTrackingEntry
{
    MethodInModule m_inlinee;

    //Our research shows that 70% of methods are inlined less than 4 times
    //so it's probably worth to inline enough storage for 3 inlines.
    InlineSArray<MethodInModule, 3> m_inliners;


    // SArray and SBuffer don't have sane implementations for operator=
    // but SHash uses operator= for moving values, so we have to provide 
    // implementations that don't corrupt memory.
    InlineTrackingEntry(const InlineTrackingEntry& other);
    InlineTrackingEntry &operator=(const InlineTrackingEntry &other);

    InlineTrackingEntry()
    {
        WRAPPER_NO_CONTRACT;
    }

    void Add(PTR_MethodDesc inliner);
    void SortAndDeduplicate();
};

class InlineTrackingMapTraits : public NoRemoveSHashTraits <DefaultSHashTraits<InlineTrackingEntry> >
{
public:
    typedef MethodInModule key_t;

    static key_t GetKey(const element_t &e)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return e.m_inlinee;
    }
    static BOOL Equals(key_t k1, key_t k2)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return (k1 == k2);
    }
    static count_t Hash(key_t k)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return ((count_t)k.m_methodDef ^ (count_t)(SIZE_T)k.m_module);
    }
    static const element_t Null()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        InlineTrackingEntry e;
        return e;
    }
    static bool IsNull(const element_t &e)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return !e.m_inlinee.m_module;
    }

    static const bool s_NoThrow = false;
};

// This is a hashtable that is used by each module to track inlines in the code inside this module.
// For each key (MethodInModule) it stores an array of methods (MethodInModule), each of those methods
// directly or indirectly inlined code from MethodInModule specified by the key.
// 
// It is important to understand that even though each module has an its own instance of the map,
// map can had methods from other modules both as keys and values.
// - If module has code inlined from other modules we naturally get methods from other modules as keys in the map.
// - During NGgen process, modules can generate code for generic classes and methods from other modules and 
//   embed them into the image (like List<MyStruct>.FindAll() might get embeded into module of MyStruct).
//   In such cases values of the map can belong to other modules.
//
// Currently this map is created and updated by modules only during native image generation
// and later saved as PersistentInlineTrackingMap.
class InlineTrackingMap : public SHash < InlineTrackingMapTraits >
{
private:
    Crst m_mapCrst;

public:
    InlineTrackingMap();
    void AddInlining(MethodDesc *inliner, MethodDesc *inlinee);
};

typedef DPTR(InlineTrackingMap) PTR_InlineTrackingMap;




// ------------------------------------ Persistance support ----------------------------------------------------------





// NGEN format
//
// This is a persistent map that is stored inside each NGen-ed module image and is used to track 
// inlines in the NGEN-ed code inside this module. 
// At runtime this map is used by profiler to track methods that inline a given method, 
// thus answering a question "give me all methods from this native image that has code from this method?"
// It doesn't require any load time unpacking and serves requests directly from NGEN image.
//
// It is composed of two arrays:
// m_inlineeIndex - sorted (by ZapInlineeRecord.key i.e. by module then token) array of ZapInlineeRecords, given an inlinee module name hash (8 bits) 
//                  and a method token (24 bits) we use binary search to find if this method has ever been inlined in NGen-ed code of this image.
//                  Each record has m_offset, which is an offset inside m_inlinersBuffer, it has more data on where the method got inlined.
//
//                  It is totally possible to have more than one ZapInlineeRecords with the same key, not only due hash collision, but also due to 
//                  the fact that we create one record for each (inlinee module / inliner module) pair. 
//                  For example: we have MyModule!MyType that uses mscorlib!List<T>. Let's say List<T>.ctor got inlined into
//                  MyType.GetAllThinds() and into List<MyType>.FindAll. In this case we'll have two InlineeRecords for mscorlib!List<T>.ctor
//                  one for MyModule and another one for mscorlib.
//                  PersistentInlineTrackingMap.GetInliners() always reads all ZapInlineeRecords as long as they have the same key, few of them filtered out 
//                  as hash collisions others provide legitimate inlining information for methods from different modules.
//
// m_inlinersBuffer - byte array compressed by NibbleWriter. At any valid offset taken from ZapInlineeRecord from m_inlineeIndex, there is a compressed chunk 
//                    of this format: 
//                    [InlineeModuleZapIndex][InlinerModuleZapIndex] [N - # of following inliners] [#1 inliner method RID] ... [#N inliner method RID]
//                    [InlineeModuleZapIndex] is used to verify that we actually found a desired inlinee module (not just a name hash collision).
//                    [InlinerModuleZapIndex] is an index of a module that owns following method tokens (inliners)
//                    [1..N inliner RID] are the sorted diff compressed method RIDs from the module specified by InlinerModuleZapIndex, 
//                    those methods directly or indirectly inlined code from inlinee method specified by ZapInlineeRecord.
//                    Since all the RIDs are sorted we'are actually able to save some space by using diffs instead of values, because NibbleWriter 
//                    is good at saving small numbers.
//                    For example for RIDs: 5, 6, 19, 25, 30, we'll write: 5, 1 (=6-5), 13 (=19-6), 6 (=25-19), 5 (=30-25)
//
// m_inlineeIndex
// +-----+-----+--------------------------------------------------+-----+-----+
// |  -  |  -  | m_key {module name hash, method token); m_offset |  -  |  -  |  
// +-----+-----+--------------------------------------------|-----+-----+-----+
//                                                          |
//                      +-----------------------------------+
//                      |
// m_inlinersBuffer    \-/
// +-----------------+-----------------------+------------------------+------------------------+------+------+--------+------+-------------+
// |  -     -     -  | InlineeModuleZapIndex | InlinerModuleZapIndex  | SavedInlinersCount (N) | rid1 | rid2 | ...... | ridN |  -   -   -  |
// +-----------------+-----------------------+------------------------+------------------------+------+------+--------+------+-------------+
//









// R2R encoding variation for the map
//
// It has several differences from the NGEN encoding. NGEN refers to methods outside the current assembly via module index + foreign module's token
// but R2R can't take those fragile dependencies. Instead we refer to all methods via MethodDef tokens in the current assembly's metadata. This
// is sufficient for everything we need to track now but in the future we may need to upgrade to a more expressive encoding. Currently NonVersionable
// attributed methods may be inlined but will not be tracked. This shows up as a known limitation in the profiler APIs that expose this data.
//
// The format changes from NGEN:
//  a) The InlineIndex uses a MethodDef RID token as the key.
//  b) InlineeModuleZapIndex is omitted because the module is always the current one being compiled.
//  c) InlinerModuleZapIndex is similarly omitted.
//  d) (a), (b) and (c) together imply there is at most one entry in the inlineeIndex for any given key
//  e) A trivial header is now explicitly described
//  
//
// The resulting serialized format is a sequence of blobs:
// 1) Header (4 byte aligned)
//       short   MajorVersion - currently set to 1, increment on breaking change
//       short   MinorVersion - currently set to 0, increment on non-breaking format addition
//       int     SizeOfInlineIndex - size in bytes of the inline index
// 
// 2) InlineIndex - Immediately following header. This is a sorted (by ZapInlineeRecord.key) array of ZapInlineeRecords, given a method token (32 bits)
//                  we use binary search to find if this method has ever been inlined in R2R code of this image. Each record has m_offset, which is
//                  an offset inside InlinersBuffer, it has more data on where the method got inlined. There is at most one ZapInlineeRecord with the 
//                  same key.
//
// 3) InlinersBuffer - Located immediately following the InlineIndex (Header RVA + sizeof(Header) + header.SizeOfInlineIndex)
//                  This is a byte array compressed by NibbleWriter. At any valid offset taken from ZapInlineeRecord from InlineeIndex, there is a 
//                  compressed chunk  of this format: 
//                  [N - # of following inliners] [#1 inliner method RID] ... [#N inliner method RID]
//                  [1..N inliner RID] are the sorted diff compressed method RIDs interpreted as MethodDefs in this assembly's metadata, 
//                  Those methods directly or indirectly inlined code from inlinee method specified by ZapInlineeRecord.
//                  Since all the RIDs are sorted we'are actually able to save some space by using diffs instead of values, because NibbleWriter 
//                  is good at saving small numbers.
//                  For example for RIDs: 5, 6, 19, 25, 30, we'll write: 5, 1 (=6-5), 13 (=19-6), 6 (=25-19), 5 (=30-25)
//
// InlineeIndex
// +-----+-----+---------------------------------------+-----+-----+
// |  -  |  -  | m_key {MethodDefToken); m_offset      |  -  |  -  |  
// +-----+-----+---------------------------------|-----+-----+-----+
//                                               |
//                    +--------------------------+
//                    |
// InlinersBuffer    \-/
// +-----------------+------------------------+------+------+--------+------+-------------+
// |  -     -     -  | SavedInlinersCount (N) | rid1 | rid2 | ...... | ridN |  -   -   -  |
// +-----------------+------------------------+------+------+--------+------+-------------+
//



//A common key format for R2R and NGEN. If the formats
//diverge further this might become irrelevant
struct ZapInlineeRecord
{
    DWORD m_key;
    DWORD m_offset;

    ZapInlineeRecord()
        : m_key(0)
    {
        LIMITED_METHOD_CONTRACT;
    }

    void InitForR2R(RID rid)
    {
        LIMITED_METHOD_CONTRACT;
        m_key = rid;
    }

    void InitForNGen(RID rid, LPCUTF8 simpleName);

    bool operator <(const ZapInlineeRecord& other) const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_key < other.m_key;
    }

    bool operator ==(const ZapInlineeRecord& other) const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_key == other.m_key;
    }
};

typedef DPTR(ZapInlineeRecord) PTR_ZapInlineeRecord;


// This type knows how to serialize and deserialize the inline tracking map format within an NGEN image. See
// above for a description of the format.
class PersistentInlineTrackingMapNGen
{
private:
    PTR_Module m_module;

    PTR_ZapInlineeRecord m_inlineeIndex;
    DWORD m_inlineeIndexSize;

    PTR_BYTE m_inlinersBuffer;
    DWORD m_inlinersBufferSize;

public:

    PersistentInlineTrackingMapNGen(Module *module)
        : m_module(dac_cast<PTR_Module>(module))
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(module != NULL);
    }

    // runtime deserialization
    COUNT_T GetInliners(PTR_Module inlineeOwnerMod, mdMethodDef inlineeTkn, COUNT_T inlinersSize, MethodInModule inliners[], BOOL *incompleteData);

    // compile-time serialization
#ifndef DACCESS_COMPILE
    void Save(DataImage *image, InlineTrackingMap* runtimeMap);
    void Fixup(DataImage *image);

private:
#endif

    Module *GetModuleByIndex(DWORD index);

};

typedef DPTR(PersistentInlineTrackingMapNGen) PTR_PersistentInlineTrackingMapNGen;


// This type knows how to serialize and deserialize the inline tracking map format within an R2R image. See
// above for a description of the format.
#ifdef FEATURE_READYTORUN
class PersistentInlineTrackingMapR2R
{
private:
    PTR_Module m_module;

    PTR_ZapInlineeRecord m_inlineeIndex;
    DWORD m_inlineeIndexSize;

    PTR_BYTE m_inlinersBuffer;
    DWORD m_inlinersBufferSize;

public:

    // runtime deserialization
#ifndef DACCESS_COMPILE
    static BOOL TryLoad(Module* pModule, const BYTE* pBuffer, DWORD cbBuffer, AllocMemTracker *pamTracker, PersistentInlineTrackingMapR2R** ppLoadedMap);
#endif
    COUNT_T GetInliners(PTR_Module inlineeOwnerMod, mdMethodDef inlineeTkn, COUNT_T inlinersSize, MethodInModule inliners[], BOOL *incompleteData);


    // compile time serialization
#ifndef DACCESS_COMPILE
    static void Save(ZapHeap* pHeap, SBuffer *saveTarget, InlineTrackingMap* runtimeMap);
#endif

};

typedef DPTR(PersistentInlineTrackingMapR2R) PTR_PersistentInlineTrackingMapR2R;
#endif //FEATURE_READYTORUN

#if !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)
// For inline tracking of JIT methods at runtime we use the CrossLoaderAllocatorHash
class InliningInfoTrackerHashTraits : public NoRemoveDefaultCrossLoaderAllocatorHashTraits<MethodDesc *, MethodDesc *>
{
};

typedef CrossLoaderAllocatorHash<InliningInfoTrackerHashTraits> InliningInfoTrackerHash;

class JITInlineTrackingMap
{
public:
    JITInlineTrackingMap(LoaderAllocator *pAssociatedLoaderAllocator);

    void AddInlining(MethodDesc *inliner, MethodDesc *inlinee);
    void AddInliningDontTakeLock(MethodDesc *inliner, MethodDesc *inlinee);
    
    template <class VisitFunc>
    void VisitInliners(MethodDesc *inlinee, VisitFunc &func)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_TRIGGERS;
            CAN_TAKE_LOCK;
            MODE_ANY;
        }
        CONTRACTL_END;

        GCX_COOP();
        CrstHolder holder(&m_mapCrst);

        auto lambda = [&](OBJECTREF obj, MethodDesc *lambdaInlinee, MethodDesc *lambdaInliner)
        {
            _ASSERTE(lambdaInlinee == inlinee);

            return func(lambdaInliner, lambdaInlinee);
        };

        m_map.VisitValuesOfKey(inlinee, lambda);
    }

private:
    BOOL InliningExistsDontTakeLock(MethodDesc *inliner, MethodDesc *inlinee);

    Crst m_mapCrst;
    InliningInfoTrackerHash m_map;
};

typedef DPTR(JITInlineTrackingMap) PTR_JITInlineTrackingMap;

#endif // !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)

#endif //INLINETRACKING_H_
