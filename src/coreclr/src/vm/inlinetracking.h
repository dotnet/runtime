// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// =============================================================================================
// Definitions for tracking method inlinings in NGen images.
// The only information stored is "who" got inlined "where", no offsets or inlining depth tracking. 
// (No good for debugger yet.)
// This information is later exposed to profilers and can be useful for ReJIT.
// Runtime inlining is not being tracked because profilers can deduce it via callbacks anyway.
// =============================================================================================
#ifndef INLINETRACKING_H_
#define INLINETRACKING_H_
#include "corhdr.h"
#include "shash.h"
#include "sarray.h"
#include "crsttypes.h"
#include "daccess.h"

class MethodDesc;
typedef DPTR(class MethodDesc)          PTR_MethodDesc;

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
        return ((count_t)k.m_methodDef ^ (count_t)k.m_module);
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

// This is a persistent map that is stored inside each NGen-ed module image and is used to track 
// inlines in the NGEN-ed code inside this module. 
// At runtime this map is used by profiler to track methods that inline a given method, 
// thus answering a question "give me all methods from this native image that has code from this method?"
// It doesn't require any load time unpacking and serves requests directly from NGEN image.
//
// It is composed of two arrays:
// m_inlineeIndex - sorted (by InlineeRecord.key i.e. by module then token) array of InlineeRecords, given an inlinee module name hash (8 bits) 
//                  and a method token (24 bits) we use binary search to find if this method has ever been inlined in NGen-ed code of this image.
//                  Each record has m_offset, which is an offset inside m_inlinersBuffer, it has more data on where the method got inlined.
//
//                  It is totally possible to have more than one InlineeRecords with the same key, not only due hash collision, but also due to 
//                  the fact that we create one record for each (inlinee module / inliner module) pair. 
//                  For example: we have MyModule!MyType that uses mscorlib!List<T>. Let's say List<T>.ctor got inlined into
//                  MyType.GetAllThinds() and into List<MyType>.FindAll. In this case we'll have two InlineeRecords for mscorlib!List<T>.ctor
//                  one for MyModule and another one for mscorlib.
//                  PersistentInlineTrackingMap.GetInliners() always reads all InlineeRecords as long as they have the same key, few of them filtered out as hash collisions
//                  others provide legitimate inlining information for methods from different modules.
//
// m_inlinersBuffer - byte array compressed by NibbleWriter. At any valid offset taken from InlineeRecord from m_inlineeIndex, there is a compressed chunk 
//                    of this format: 
//                    [InlineeModuleZapIndex][InlinerModuleZapIndex] [N - # of following inliners] [#1 inliner method RID] ... [#N inliner method RID]
//                    [InlineeModuleZapIndex] is used to verify that we actually found a desired inlinee module (not just a name hash collision).
//                    [InlinerModuleZapIndex] is an index of a module that owns following method tokens (inliners)
//                    [1..N inliner RID] are the sorted diff compressed method RIDs from the module specified by InlinerModuleZapIndex, 
//                    those methods directly or indirectly inlined code from inlinee method specified by InlineeRecord.
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
class PersistentInlineTrackingMap
{
private:
    struct InlineeRecord
    {
        DWORD m_key;
        DWORD m_offset;

        InlineeRecord()
            : m_key(0)
        {
            LIMITED_METHOD_CONTRACT;
        }

        InlineeRecord(RID rid, LPCUTF8 simpleName);

        bool operator <(const InlineeRecord& other) const
        {
            LIMITED_METHOD_DAC_CONTRACT;
            return m_key < other.m_key;
        }

        bool operator ==(const InlineeRecord& other) const
        {
            LIMITED_METHOD_DAC_CONTRACT;
            return m_key == other.m_key;
        }
    };
    typedef DPTR(InlineeRecord) PTR_InlineeRecord;

    PTR_Module m_module;

    PTR_InlineeRecord m_inlineeIndex;
    DWORD m_inlineeIndexSize;

    PTR_BYTE m_inlinersBuffer;
    DWORD m_inlinersBufferSize;

public:

    PersistentInlineTrackingMap(Module *module)
        : m_module(dac_cast<PTR_Module>(module))
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(module != NULL);
    }

    void Save(DataImage *image, InlineTrackingMap* runtimeMap);
    void Fixup(DataImage *image);

    COUNT_T GetInliners(PTR_Module inlineeOwnerMod, mdMethodDef inlineeTkn, COUNT_T inlinersSize, MethodInModule inliners[], BOOL *incompleteData);

private:
    void ProcessInlineTrackingEntry(DataImage *image, SBuffer *inlinersBuffer, SArray<InlineeRecord> *inlineeIndex, InlineTrackingEntry *entry);
    Module *GetModuleByIndex(DWORD index);
};

typedef DPTR(PersistentInlineTrackingMap) PTR_PersistentInlineTrackingMap;

#endif //INLINETRACKING_H_
