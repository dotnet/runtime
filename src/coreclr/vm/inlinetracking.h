// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
#include "crst.h"
#include "daccess.h"
#include "crossloaderallocatorhash.h"

#ifdef FEATURE_INLINE_TRACKING_ENABLED


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
//   embed them into the image (like List<MyStruct>.FindAll() might get embedded into module of MyStruct).
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


#ifndef DACCESS_COMPILE
// Used to walk the NGEN/R2R inlining data
class NativeImageInliningIterator
{
public:
    NativeImageInliningIterator();

    HRESULT Reset(Module* pInlinerModule, MethodInModule inlinee);
    BOOL Next();
    MethodInModule GetMethod();

private:
    Module *m_pModule;
    MethodInModule m_inlinee;
    NewArrayHolder<MethodInModule> m_dynamicBuffer;
    COUNT_T m_dynamicBufferSize;
    COUNT_T m_dynamicAvailable;
    COUNT_T m_currentPos;

    const COUNT_T s_bufferSize = 10;
    const COUNT_T s_failurePos = -2;
};
#endif // DACCESS_COMPILE

// ------------------------------------ Persistence support ---------------------------------------------------------

#ifdef FEATURE_READYTORUN
class PersistentInlineTrackingMapR2R
{
public:
    virtual COUNT_T GetInliners(PTR_Module inlineeOwnerMod, mdMethodDef inlineeTkn, COUNT_T inlinersSize, MethodInModule inliners[], BOOL *incompleteData) = 0;
};

typedef DPTR(PersistentInlineTrackingMapR2R) PTR_PersistentInlineTrackingMapR2R;

#ifndef DACCESS_COMPILE
class PersistentInlineTrackingMapR2R2 : private PersistentInlineTrackingMapR2R
{
private:
    PTR_Module m_module;

    NativeFormat::NativeReader m_reader;
    NativeFormat::NativeHashtable m_hashtable;

public:

    // runtime deserialization
    static BOOL TryLoad(Module* pModule, const BYTE* pBuffer, DWORD cbBuffer, AllocMemTracker* pamTracker, PersistentInlineTrackingMapR2R2** ppLoadedMap);
    virtual COUNT_T GetInliners(PTR_Module inlineeOwnerMod, mdMethodDef inlineeTkn, COUNT_T inlinersSize, MethodInModule inliners[], BOOL* incompleteData) override;

private:
    Module* GetModuleByIndex(DWORD index);
};

typedef DPTR(PersistentInlineTrackingMapR2R2) PTR_PersistentInlineTrackingMapR2R2;
#endif

#ifndef DACCESS_COMPILE
namespace NativeFormat
{
    class NativeParser;
}

class CrossModulePersistentInlineTrackingMapR2R : private PersistentInlineTrackingMapR2R
{
private:
    PTR_Module m_module;

    NativeFormat::NativeReader m_reader;
    NativeFormat::NativeHashtable m_hashtable;

public:

    // runtime deserialization
    static BOOL TryLoad(Module* pModule, LoaderAllocator* pLoaderAllocator, const BYTE* pBuffer, DWORD cbBuffer, AllocMemTracker* pamTracker, CrossModulePersistentInlineTrackingMapR2R** ppLoadedMap);
    virtual COUNT_T GetInliners(PTR_Module inlineeOwnerMod, mdMethodDef inlineeTkn, COUNT_T inlinersSize, MethodInModule inliners[], BOOL* incompleteData) override;

private:
    Module* GetModuleByIndex(DWORD index);
    void GetILBodySection(MethodDesc*** pppMethods, COUNT_T* pcMethods);
};

typedef DPTR(CrossModulePersistentInlineTrackingMapR2R) PTR_CrossModulePersistentInlineTrackingMapR2R;
#endif

#endif // FEATURE_READYTORUN

#if !defined(DACCESS_COMPILE)
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
            GC_NOTRIGGER;
            CAN_TAKE_LOCK;
            MODE_ANY;
        }
        CONTRACTL_END;

        CrstHolder holder(&s_mapCrst);

        auto lambda = [&](LoaderAllocator *loaderAllocatorOfInliner, MethodDesc *lambdaInlinee, MethodDesc *lambdaInliner)
        {
            _ASSERTE(lambdaInlinee == inlinee);

            return func(lambdaInliner, lambdaInlinee);
        };

        m_map.VisitValuesOfKey(inlinee, lambda);
    }

    static void StaticInitialize()
    {
        WRAPPER_NO_CONTRACT;
        s_mapCrst.Init(CrstJitInlineTrackingMap, CrstFlags(CRST_DEBUGGER_THREAD));
    }

    static CrstBase *GetMapCrst() { return &s_mapCrst; }

private:
    BOOL InliningExistsDontTakeLock(MethodDesc *inliner, MethodDesc *inlinee);

    static CrstStatic s_mapCrst;
    InliningInfoTrackerHash m_map;
};

typedef DPTR(JITInlineTrackingMap) PTR_JITInlineTrackingMap;

#endif // !defined(DACCESS_COMPILE)

#endif // FEATURE_INLINE_TRACKING_ENABLED

#endif // INLINETRACKING_H_
