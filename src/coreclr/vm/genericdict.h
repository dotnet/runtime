// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: genericdict.h
//

//
// Definitions for "dictionaries" used to encapsulate generic instantiations
// and instantiation-specific information for shared-code generics
//

//
// ============================================================================

#ifndef _GENERICDICT_H
#define _GENERICDICT_H

#ifdef FEATURE_PREJIT
#include "dataimage.h"
#endif

// DICTIONARIES
//
// A dictionary is a cache of handles associated with particular
// instantiations of generic classes and generic methods, containing
// - the instantiation itself (a list of TypeHandles)
// - handles created on demand at runtime when code shared between
//   multiple instantiations needs to lookup an instantiation-specific
//   handle (for example, in newobj C<!0> and castclass !!0[])
//
// DICTIONARY ENTRIES
//
// Dictionary entries (abstracted as the type DictionaryEntry) can be:
//   a TypeHandle (for type arguments and entries associated with a TypeSpec token)
//   a MethodDesc* (for entries associated with a method MemberRef or MethodSpec token)
//   a FieldDesc* (for entries associated with a field MemberRef token)
//   a code pointer (e.g. for entries associated with an EntryPointAnnotation annotated token)
//   a dispatch stub address (for entries associated with an StubAddrAnnotation annotated token)
//
// DICTIONARY LAYOUTS
//
// A dictionary layout describes the layout of all dictionaries that can be
// accessed from the same shared code. For example, Hashtable<string,int> and
// Hashtable<object,int> share one layout, and Hashtable<int,string> and Hashtable<int,object>
// share another layout. For generic types, the dictionary layout is stored in the EEClass
// that is shared across compatible instantiations. For generic methods, the layout
// is stored in the InstantiatedMethodDesc associated with the shared generic code itself.
//

class TypeHandleList;
class Module;
class BaseDomain;
class SigTypeContext;
class SigBuilder;

enum DictionaryEntryKind
{
    EmptySlot = 0,
    TypeHandleSlot = 1,
    MethodDescSlot = 2,
    MethodEntrySlot = 3,
    ConstrainedMethodEntrySlot = 4,
    DispatchStubAddrSlot = 5,
    FieldDescSlot = 6,
    DeclaringTypeHandleSlot = 7,
};

enum DictionaryEntrySignatureSource : BYTE
{
    FromZapImage = 0,
    FromReadyToRunImage = 1,
    FromJIT = 2,
};

class DictionaryEntryLayout
{
public:
    DictionaryEntryLayout(PTR_VOID signature)
    { LIMITED_METHOD_CONTRACT; m_signature = signature; }

    DictionaryEntryKind GetKind();

    PTR_VOID m_signature;

    DictionaryEntrySignatureSource m_signatureSource;
};

typedef DPTR(DictionaryEntryLayout) PTR_DictionaryEntryLayout;


class DictionaryLayout;
typedef DPTR(DictionaryLayout) PTR_DictionaryLayout;

// Number of slots to initially allocate in a generic method dictionary layout.
#if _DEBUG
#define NUM_DICTIONARY_SLOTS 1  // Smaller number to stress the dictionary expansion logic
#else
#define NUM_DICTIONARY_SLOTS 4
#endif

// The type of dictionary layouts. We don't include the number of type
// arguments as this is obtained elsewhere
class DictionaryLayout
{
    friend class Dictionary;
#ifdef DACCESS_COMPILE
    friend class NativeImageDumper;
#endif
private:
    // Current number of non-type-argument slots
    WORD m_numSlots;

    // Number of non-type-argument slots of the initial layout before any expansion
    WORD m_numInitialSlots;

    // m_numSlots of these
    DictionaryEntryLayout m_slots[1];

    static BOOL FindTokenWorker(LoaderAllocator*                    pAllocator,
                                DWORD                               numGenericArgs,
                                DictionaryLayout*                   pDictLayout,
                                SigBuilder*                         pSigBuilder,
                                BYTE*                               pSig,
                                DWORD                               cbSig,
                                int                                 nFirstOffset,
                                DictionaryEntrySignatureSource      signatureSource,
                                CORINFO_RUNTIME_LOOKUP*             pResult,
                                WORD*                               pSlotOut,
                                DWORD                               scanFromSlot,
                                BOOL                                useEmptySlotIfFound);


    static DictionaryLayout* ExpandDictionaryLayout(LoaderAllocator*                pAllocator,
                                                    DictionaryLayout*               pCurrentDictLayout,
                                                    DWORD                           numGenericArgs,
                                                    SigBuilder*                     pSigBuilder,
                                                    BYTE*                           pSig,
                                                    int                             nFirstOffset,
                                                    DictionaryEntrySignatureSource  signatureSource,
                                                    CORINFO_RUNTIME_LOOKUP*         pResult,
                                                    WORD*                           pSlotOut);
     
    static PVOID CreateSignatureWithSlotData(SigBuilder* pSigBuilder, LoaderAllocator* pAllocator, WORD slot);

public:
    // Create an initial dictionary layout containing numSlots slots
    static DictionaryLayout* Allocate(WORD numSlots, LoaderAllocator *pAllocator, AllocMemTracker *pamTracker);

    // Total number of bytes used for this dictionary, which might be stored inline in
    // another structure (e.g. MethodTable). This may include the final back-pointer
    // to previous dictionaries after dictionary expansion; pSlotSize is used to return
    // the size to be stored in the "size slot" of the dictionary.
    static DWORD GetDictionarySizeFromLayout(DWORD numGenericArgs, PTR_DictionaryLayout pDictLayout, DWORD *pSlotSize);

    static BOOL FindToken(MethodTable*                      pMT,
                          LoaderAllocator*                  pAllocator,
                          int                               nFirstOffset,
                          SigBuilder*                       pSigBuilder,
                          BYTE*                             pSig,
                          DictionaryEntrySignatureSource    signatureSource,
                          CORINFO_RUNTIME_LOOKUP*           pResult,
                          WORD*                             pSlotOut);

    static BOOL FindToken(MethodDesc*                       pMD,
                          LoaderAllocator*                  pAllocator,
                          int                               nFirstOffset,
                          SigBuilder*                       pSigBuilder,
                          BYTE*                             pSig,
                          DictionaryEntrySignatureSource    signatureSource,
                          CORINFO_RUNTIME_LOOKUP*           pResult,
                          WORD*                             pSlotOut);

    DWORD GetMaxSlots();
    DWORD GetNumInitialSlots();
    DWORD GetNumUsedSlots();
    
    PTR_DictionaryEntryLayout GetEntryLayout(DWORD i)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(i >= 0 && i < GetMaxSlots());
        return dac_cast<PTR_DictionaryEntryLayout>(
            dac_cast<TADDR>(this) + offsetof(DictionaryLayout, m_slots) + sizeof(DictionaryEntryLayout) * i);
    }


#ifdef FEATURE_PREJIT
    DWORD GetObjectSize();

    // Trim the canonical dictionary layout to include only those used slots actually used.
    // WARNING!!!
    // You should only call this if
    //    (a) you're actually saving this shared instantiation into it's PreferredZapModule,
    //        i.e. you must be both saving the shared instantiation and the module
    //        you're ngen'ing MUST be that the PreferredZapModule.
    //    (b) you're sure you've compiled all the shared code for this type
    //        within the context of this NGEN session.
    // This is currently the same as saying we can hardbind to the EEClass - if it's in another
    // module then we will have already trimmed the layout, and if it's being saved into the
    // current module then we can only hardbind to it if the current module is the PreferredZapModule.
    //
    // Note after calling this both GetObjectSize for this layout and the
    // computed dictionary size for all dictionaries based on this layout may
    // be reduced.  This may in turn affect the size of all non-canonical
    // method tables, potentially trimming some dictionary words off the end
    // of the method table.
    void Trim();
    void Save(DataImage *image);
    void Fixup(DataImage *image, BOOL fMethod);
#endif // FEATURE_PREJIT

};


// The type of dictionaries. This is just an abstraction around an open-ended array
class Dictionary
{
#ifdef DACCESS_COMPILE
    friend class NativeImageDumper;
#endif
private:
    // First N entries are generic instantiations arguments. They are stored as FixupPointers
    // in NGen images. It means that the lowest bit is used to mark optional indirection (see code:FixupPointer).
    // The rest of the open array are normal pointers (no optional indirection).
    DictionaryEntry m_pEntries[1];

    TADDR EntryAddr(ULONG32 idx)
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        return PTR_HOST_MEMBER_TADDR(Dictionary, this, m_pEntries) +
            idx * sizeof(m_pEntries[0]);
    }

public:
    inline DPTR(FixupPointer<TypeHandle>) GetInstantiation()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        return dac_cast<DPTR(FixupPointer<TypeHandle>)>(EntryAddr(0));
    }

#ifndef DACCESS_COMPILE
    inline void* AsPtr()
    {
        LIMITED_METHOD_CONTRACT;
        return (void*)m_pEntries;
    }
#endif // #ifndef DACCESS_COMPILE

private:

#ifndef DACCESS_COMPILE

    inline TypeHandle GetTypeHandleSlot(DWORD numGenericArgs, DWORD i)
    {
        LIMITED_METHOD_CONTRACT;
        return *GetTypeHandleSlotAddr(numGenericArgs, i);
    }
    inline MethodDesc* GetMethodDescSlot(DWORD numGenericArgs, DWORD i)
    {
        LIMITED_METHOD_CONTRACT;
        return *GetMethodDescSlotAddr(numGenericArgs, i);
    }
    inline FieldDesc* GetFieldDescSlot(DWORD numGenericArgs, DWORD i)
    {
        LIMITED_METHOD_CONTRACT;
        return *GetFieldDescSlotAddr(numGenericArgs, i);
    }
    inline TypeHandle* GetTypeHandleSlotAddr(DWORD numGenericArgs, DWORD i)
    {
        LIMITED_METHOD_CONTRACT;
        return ((TypeHandle*)&m_pEntries[numGenericArgs + i]);
    }
    inline MethodDesc** GetMethodDescSlotAddr(DWORD numGenericArgs, DWORD i)
    {
        LIMITED_METHOD_CONTRACT;
        return ((MethodDesc**)&m_pEntries[numGenericArgs + i]);
    }
    inline FieldDesc** GetFieldDescSlotAddr(DWORD numGenericArgs, DWORD i)
    {
        LIMITED_METHOD_CONTRACT;
        return ((FieldDesc**)&m_pEntries[numGenericArgs + i]);
    }
    inline DictionaryEntry* GetSlotAddr(DWORD numGenericArgs, DWORD i)
    {
        LIMITED_METHOD_CONTRACT;
        return ((void**)&m_pEntries[numGenericArgs + i]);
    }
    inline DictionaryEntry GetSlot(DWORD numGenericArgs, DWORD i)
    {
        LIMITED_METHOD_CONTRACT;
        return *GetSlotAddr(numGenericArgs, i);
    }
    inline BOOL IsSlotEmpty(DWORD numGenericArgs, DWORD i)
    {
        LIMITED_METHOD_CONTRACT;
        return GetSlot(numGenericArgs, i) == NULL;
    }

    inline DWORD GetDictionarySlotsSize(DWORD numGenericArgs)
    {
        LIMITED_METHOD_CONTRACT;
        return VolatileLoadWithoutBarrier((DWORD*)GetSlotAddr(numGenericArgs, 0));
    }
    
    inline Dictionary **GetBackPointerSlot(DWORD numGenericArgs)
    {
        LIMITED_METHOD_CONTRACT;
        return (Dictionary **)((uint8_t *)m_pEntries + GetDictionarySlotsSize(numGenericArgs));
    }
#endif // #ifndef DACCESS_COMPILE

public:

#ifndef DACCESS_COMPILE

    static DictionaryEntry PopulateEntry(MethodDesc * pMD,
                                         MethodTable * pMT,
                                         LPVOID signature,
                                         BOOL nonExpansive,
                                         DictionaryEntry ** ppSlot,
                                         DWORD dictionaryIndexAndSlot = -1,
                                         Module * pModule = NULL);

    void PrepopulateDictionary(MethodDesc * pMD,
                               MethodTable * pMT,
                               BOOL nonExpansive);

private:
    static Dictionary* GetTypeDictionaryWithSizeCheck(MethodTable* pMT, ULONG slotIndex);
    static Dictionary* GetMethodDictionaryWithSizeCheck(MethodDesc* pMD, ULONG slotIndex);

#endif // #ifndef DACCESS_COMPILE

public:

#ifdef FEATURE_PREJIT

    // Fixup the dictionary entries, including the type arguments
    //
    // WARNING!!!
    // You should only pass "canSaveSlots=TRUE" if you are certain the dictionary layout
    // matches that which will be used at runtime.  This means you must either
    // be able to hard-bind to the EEClass of the canonical instantiation, or else
    // you are saving a copy of the canonical instantiation itself.
    //
    // If we can't save slots, then we will zero all entries in the dictionary (apart from the
    // instantiation itself) and load at runtime.
    void Fixup(DataImage *image,
               BOOL canSaveInstantiation,
               BOOL canSaveSlots,
               DWORD numGenericArgs,            // Must be non-zero
               Module *pModule,                 // module of the generic code
               DictionaryLayout *pDictLayout);  // If NULL, then only type arguments are present

    BOOL IsWriteable(DataImage *image,
               BOOL canSaveSlots,
               DWORD numGenericArgs,            // Must be non-zero
               Module *pModule,                 // module of the generic code
               DictionaryLayout *pDictLayout);  // If NULL, then only type arguments are present

    BOOL ComputeNeedsRestore(DataImage *image,
                             TypeHandleList *pVisited,
                             DWORD numGenericArgs);
    void Restore(DWORD numGenericArgs, ClassLoadLevel level);
#endif // FEATURE_PREJIT
};

#endif
