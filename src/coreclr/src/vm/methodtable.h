//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//
// File: methodtable.h
//


//

//
// ============================================================================

#ifndef _METHODTABLE_H_
#define _METHODTABLE_H_

/*
 *  Include Files 
 */
#ifndef BINDER
#include "vars.hpp"
#include "cor.h"
#include "hash.h"
#include "crst.h"
#include "objecthandle.h"
#include "cgensys.h"
#include "declsec.h"
#ifdef FEATURE_COMINTEROP
#include "stdinterfaces.h"
#endif
#include "slist.h"
#include "spinlock.h"
#include "typehandle.h"
#include "eehash.h"
#include "contractimpl.h"
#include "generics.h"
#include "fixuppointer.h"
#else
#include "classloadlevel.h"
#endif // !BINDER

/*
 * Forward Declarations
 */
class    AppDomain;
class    ArrayClass;
class    ArrayMethodDesc;
struct   ClassCtorInfoEntry;
class ClassLoader;
class    DomainLocalBlock;
class FCallMethodDesc;
class    EEClass;
class    EnCFieldDesc;
class FieldDesc;
class JIT_TrialAlloc;
struct LayoutRawFieldInfo;
class MetaSig;
class    MethodDesc;
class    MethodDescChunk;
class    MethodTable;
class    Module;
class    Object;
class    Stub;
class    Substitution;
class    TypeHandle;
#ifdef FEATURE_REMOTING
class   CrossDomainOptimizationInfo;
typedef DPTR(CrossDomainOptimizationInfo) PTR_CrossDomainOptimizationInfo;
#endif
class   Dictionary;
class   AllocMemTracker;
class   SimpleRWLock;
class   MethodDataCache;
class   EEClassLayoutInfo;
#ifdef FEATURE_COMINTEROP
class   ComCallWrapperTemplate;
#endif
#ifdef FEATURE_COMINTEROP_UNMANAGED_ACTIVATION
class ClassFactoryBase;
#endif // FEATURE_COMINTEROP_UNMANAGED_ACTIVATION
class ArgDestination;

//============================================================================
// This is the in-memory structure of a class and it will evolve.
//============================================================================

// <TODO>
// Add a sync block
// Also this class currently has everything public - this may changes
// Might also need to hold onto the meta data loader fot this class</TODO>

//
// A MethodTable contains an array of these structures, which describes each interface implemented
// by this class (directly declared or indirectly declared).
//
// Generic type instantiations (in C# syntax: C<ty_1,...,ty_n>) are represented by
// MethodTables, i.e. a new MethodTable gets allocated for each such instantiation.
// The entries in these tables (i.e. the code) are, however, often shared.
//
// In particular, a MethodTable's vtable contents (and hence method descriptors) may be 
// shared between compatible instantiations (e.g. List<string> and List<object> have
// the same vtable *contents*).  Likewise the EEClass will be shared between 
// compatible instantiations whenever the vtable contents are. 
//
// !!! Thus that it is _not_ generally the case that GetClass.GetMethodTable() == t. !!!
//
// Instantiated interfaces have their own method tables unique to the instantiation e.g. I<string> is
// distinct from I<int> and I<object>
// 
// For generic types the interface map lists generic interfaces
// For instantiated types the interface map lists instantiated interfaces
//   e.g. for C<T> : I<T>, J<string>
// the interface map for C would list I and J
// the interface map for C<int> would list I<int> and J<string>
//
struct InterfaceInfo_t
{
#ifdef DACCESS_COMPILE
    friend class NativeImageDumper;
#endif

    FixupPointer<PTR_MethodTable> m_pMethodTable;        // Method table of the interface

public:
    FORCEINLINE PTR_MethodTable GetMethodTable()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pMethodTable.GetValue();
    }

#ifndef DACCESS_COMPILE
    void SetMethodTable(MethodTable * pMT)
    {
        LIMITED_METHOD_CONTRACT;
        m_pMethodTable.SetValue(pMT);
    }

    // Get approximate method table. This is used by the type loader before the type is fully loaded.
    PTR_MethodTable GetApproxMethodTable(Module * pContainingModule);
#endif
};  // struct InterfaceInfo_t

typedef DPTR(InterfaceInfo_t) PTR_InterfaceInfo;

namespace ClassCompat
{
    struct InterfaceInfo_t;
};

// Data needed when simulating old VTable layout for COM Interop
// This is necessary as the data is saved in MethodDescs and we need
// to simulate different values without copying or changing the existing
// MethodDescs
//
// This will be created in a parallel array to ppMethodDescList and
// ppUnboxMethodDescList in the bmtMethAndFieldDescs structure below
struct InteropMethodTableSlotData
{
    enum
    {
        e_DUPLICATE = 0x0001              // The entry is duplicate
    };

    MethodDesc *pMD;                // The MethodDesc for this slot
    WORD        wSlot;              // The simulated slot value for the MethodDesc
    WORD        wFlags;             // The simulated duplicate value
    MethodDesc *pDeclMD;            // To keep track of MethodImpl's

    void SetDuplicate()
    {
        wFlags |= e_DUPLICATE;
    }

    BOOL IsDuplicate() {
        return ((BOOL)(wFlags & e_DUPLICATE));
    }

    WORD GetSlot() {
        return wSlot;
    }

    void SetSlot(WORD wSlot) {
        this->wSlot = wSlot;
    }
};  // struct InteropMethodTableSlotData

#ifdef FEATURE_COMINTEROP
struct InteropMethodTableData
{
    WORD cVTable;                          // Count of vtable slots
    InteropMethodTableSlotData *pVTable;    // Data for each slot

    WORD cNonVTable;                       // Count of non-vtable slots
    InteropMethodTableSlotData *pNonVTable; // Data for each slot

    WORD            cInterfaceMap;         // Count of interfaces
    ClassCompat::InterfaceInfo_t *
                    pInterfaceMap;         // The interface map

    // Utility methods
    static WORD GetRealMethodDesc(MethodTable *pMT, MethodDesc *pMD);
    static WORD GetSlotForMethodDesc(MethodTable *pMT, MethodDesc *pMD);
    ClassCompat::InterfaceInfo_t* FindInterface(MethodTable *pInterface);
    WORD GetStartSlotForInterface(MethodTable* pInterface);
};

class InteropMethodTableSlotDataMap
{
protected:
    InteropMethodTableSlotData *m_pSlotData;
    DWORD                       m_cSlotData;
    DWORD                       m_iCurSlot;

public:
    InteropMethodTableSlotDataMap(InteropMethodTableSlotData *pSlotData, DWORD cSlotData);
    InteropMethodTableSlotData *GetData(MethodDesc *pMD);
    BOOL Exists(MethodDesc *pMD);

protected:
    InteropMethodTableSlotData *Exists_Helper(MethodDesc *pMD);
    InteropMethodTableSlotData *GetNewEntry();
};  // class InteropMethodTableSlotDataMap
#endif // FEATURE_COMINTEROP

//
// This struct contains cached information on the GUID associated with a type. 
//

struct GuidInfo
{
    GUID         m_Guid;                // The actual guid of the type.
    BOOL         m_bGeneratedFromName;  // A boolean indicating if it was generated from the 
                                        // name of the type.
};

typedef DPTR(GuidInfo) PTR_GuidInfo;


// GenericsDictInfo is stored at negative offset of the dictionary
struct GenericsDictInfo
{
#ifdef _WIN64
    DWORD m_dwPadding;               // Just to keep the size a multiple of 8
#endif

    // Total number of instantiation dictionaries including inherited ones
    //   i.e. how many instantiated classes (including this one) are there in the hierarchy?
    // See comments about PerInstInfo
    WORD   m_wNumDicts;

    // Number of type parameters (NOT including those of superclasses).
    WORD   m_wNumTyPars;
};  // struct GenericsDictInfo
typedef DPTR(GenericsDictInfo) PTR_GenericsDictInfo;

struct GenericsStaticsInfo
{
    // Pointer to field descs for statics
    PTR_FieldDesc       m_pFieldDescs;

    // Method table ID for statics
    SIZE_T              m_DynamicTypeID;

};  // struct GenericsStaticsInfo
typedef DPTR(GenericsStaticsInfo) PTR_GenericsStaticsInfo;


// CrossModuleGenericsStaticsInfo is used in NGen images for statics of cross-module
// generic instantiations. CrossModuleGenericsStaticsInfo is optional member of
// MethodTableWriteableData.
struct CrossModuleGenericsStaticsInfo
{
    // Module this method table statics are attached to. 
    //
    // The statics has to be attached to module referenced from the generic instantiation 
    // in domain-neutral code. We need to guarantee that the module for the statics 
    // has a valid local represenation in an appdomain.
    // 
    PTR_Module          m_pModuleForStatics;

    // Method table ID for statics
    SIZE_T              m_DynamicTypeID;
};  // struct CrossModuleGenericsStaticsInfo
typedef DPTR(CrossModuleGenericsStaticsInfo) PTR_CrossModuleGenericsStaticsInfo;

// This structure records methods and fields which are interesting for VTS
// (Version Tolerant Serialization). A pointer to it is optionally appended to
// MethodTables with VTS event methods or NotSerialized or OptionallySerialized
// fields. The structure is variable length to incorporate a packed array of
// data describing the disposition of fields in the type.
struct RemotingVtsInfo
{
    enum VtsCallbackType
    {
        VTS_CALLBACK_ON_SERIALIZING = 0,
        VTS_CALLBACK_ON_SERIALIZED,
        VTS_CALLBACK_ON_DESERIALIZING,
        VTS_CALLBACK_ON_DESERIALIZED,
        VTS_NUM_CALLBACK_TYPES
    };

    FixupPointer<PTR_MethodDesc> m_pCallbacks[VTS_NUM_CALLBACK_TYPES];
#ifdef _DEBUG
    DWORD               m_dwNumFields;
#endif
    DWORD               m_rFieldTypes[1];

    static DWORD GetSize(DWORD dwNumFields)
    {
        LIMITED_METHOD_CONTRACT;
        // Encode each field in two bits. Round up allocation to the nearest DWORD.
        DWORD dwBitsRequired = dwNumFields * 2;
        DWORD dwBytesRequired = (dwBitsRequired + 7) / 8;
        return (DWORD)(offsetof(RemotingVtsInfo, m_rFieldTypes[0]) + ALIGN_UP(dwBytesRequired, sizeof(DWORD)));
    }

    void SetIsNotSerialized(DWORD dwFieldIndex)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(dwFieldIndex < m_dwNumFields);
        DWORD dwRecordIndex = dwFieldIndex * 2;
        DWORD dwOffset = dwRecordIndex / (sizeof(DWORD) * 8);
        DWORD dwMask = 1 << (dwRecordIndex % (sizeof(DWORD) * 8));
        m_rFieldTypes[dwOffset] |= dwMask;
    }

    BOOL IsNotSerialized(DWORD dwFieldIndex)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(dwFieldIndex < m_dwNumFields);
        DWORD dwRecordIndex = dwFieldIndex * 2;
        DWORD dwOffset = dwRecordIndex / (sizeof(DWORD) * 8);
        DWORD dwMask = 1 << (dwRecordIndex % (sizeof(DWORD) * 8));
        return m_rFieldTypes[dwOffset] & dwMask;
    }

    void SetIsOptionallySerialized(DWORD dwFieldIndex)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(dwFieldIndex < m_dwNumFields);
        DWORD dwRecordIndex = dwFieldIndex * 2;
        DWORD dwOffset = dwRecordIndex / (sizeof(DWORD) * 8);
        DWORD dwMask = 2 << (dwRecordIndex % (sizeof(DWORD) * 8));
        m_rFieldTypes[dwOffset] |= dwMask;
    }

    BOOL IsOptionallySerialized(DWORD dwFieldIndex)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(dwFieldIndex < m_dwNumFields);
        DWORD dwRecordIndex = dwFieldIndex * 2;
        DWORD dwOffset = dwRecordIndex / (sizeof(DWORD) * 8);
        DWORD dwMask = 2 << (dwRecordIndex % (sizeof(DWORD) * 8));
        return m_rFieldTypes[dwOffset] & dwMask;
    }
};  // struct RemotingVtsInfo
typedef DPTR(RemotingVtsInfo) PTR_RemotingVtsInfo;


struct ContextStaticsBucket
{
    // Offset which points to the CLS storage. Allocated lazily - -1 means no offset allocated yet.
    DWORD m_dwContextStaticsOffset;
    // Size of CLS fields
    WORD m_wContextStaticsSize;
};
typedef DPTR(ContextStaticsBucket) PTR_ContextStaticsBucket;

#ifdef FEATURE_COMINTEROP
struct RCWPerTypeData;
#endif // FEATURE_COMINTEROP

//
// This struct consolidates the writeable parts of the MethodTable
// so that we can layout a read-only MethodTable with a pointer
// to the writeable parts of the MethodTable in an ngen image
//
struct MethodTableWriteableData
{
    friend class MethodTable; 
#if defined(DACCESS_COMPILE)
    friend class NativeImageDumper;
#endif

    enum
    {
        // AS YOU ADD NEW FLAGS PLEASE CONSIDER WHETHER Generics::NewInstantiation NEEDS
        // TO BE UPDATED IN ORDER TO ENSURE THAT METHODTABLES DUPLICATED FOR GENERIC INSTANTIATIONS
        // CARRY THE CORRECT INITIAL FLAGS.
    
        enum_flag_RemotingConfigChecked     = 0x00000001,
        enum_flag_RequiresManagedActivation = 0x00000002,
        enum_flag_Unrestored                = 0x00000004,
        enum_flag_CriticalTypePrepared      = 0x00000008,     // CriticalFinalizerObject derived type has had backout routines prepared
        enum_flag_HasApproxParent           = 0x00000010,
        enum_flag_UnrestoredTypeKey         = 0x00000020,     
        enum_flag_IsNotFullyLoaded          = 0x00000040,
        enum_flag_DependenciesLoaded        = 0x00000080,     // class and all depedencies loaded up to CLASS_LOADED_BUT_NOT_VERIFIED

        enum_flag_SkipWinRTOverride         = 0x00000100,     // No WinRT override is needed

#ifdef FEATURE_PREJIT
        // These flags are used only at ngen time. We store them here since
        // we are running out of available flags in MethodTable. They may eventually 
        // go into ngen speficic state.
        enum_flag_NGEN_IsFixedUp            = 0x00010000, // This MT has been fixed up during NGEN
        enum_flag_NGEN_IsNeedsRestoreCached = 0x00020000, // Set if we have cached the results of needs restore computation
        enum_flag_NGEN_CachedNeedsRestore   = 0x00040000, // The result of the needs restore computation
        enum_flag_NGEN_OverridingInterface  = 0x00080000, // Overriding interface that we should generate WinRT CCW stubs for.

#ifdef FEATURE_READYTORUN_COMPILER
        enum_flag_NGEN_IsLayoutFixedComputed = 0x0010000, // Set if we have cached the result of IsLayoutFixed computation
        enum_flag_NGEN_IsLayoutFixed        = 0x0020000, // The result of the IsLayoutFixed computation
#endif

#endif // FEATURE_PREJIT

#ifdef _DEBUG
        enum_flag_ParentMethodTablePointerValid =  0x40000000,
        enum_flag_HasInjectedInterfaceDuplicates = 0x80000000,
#endif
    };
    DWORD      m_dwFlags;                  // Lot of empty bits here.

private:
    /*
     * m_hExposedClassObject is LoaderAllocator slot index to 
     * a RuntimeType instance for this class.  But
     * do NOT use it for Arrays or remoted objects!  All arrays of objects 
     * share the same MethodTable/EEClass.
     * @GENERICS: this used to live in EEClass but now lives here because it is per-instantiation data
	 * only set in code:MethodTable.GetManagedClassObject
     */
    LOADERHANDLE m_hExposedClassObject;

#ifdef _DEBUG
public:
    // to avoid verify same method table too many times when it's not changing, we cache the GC count
    // on which the method table is verified. When fast GC STRESS is turned on, we only verify the MT if 
    // current GC count is bigger than the number. Note most thing which will invalidate a MT will require a 
    // GC (like AD unload)
    Volatile<DWORD> m_dwLastVerifedGCCnt;

#ifdef _WIN64
    DWORD m_dwPadding;               // Just to keep the size a multiple of 8
#endif

#endif

    // Optional CrossModuleGenericsStaticsInfo may be here.

public:
#ifdef _DEBUG
    inline BOOL IsParentMethodTablePointerValid() const
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return (m_dwFlags & enum_flag_ParentMethodTablePointerValid);
    }
    inline void SetParentMethodTablePointerValid()
    {
        LIMITED_METHOD_CONTRACT;

        m_dwFlags |= enum_flag_ParentMethodTablePointerValid;
    }
#endif

#ifdef FEATURE_PREJIT

    void Save(DataImage *image, MethodTable *pMT, DWORD profilingFlags) const;
    void Fixup(DataImage *image, MethodTable *pMT, BOOL needsRestore);

    inline BOOL IsFixedUp() const
    {
        LIMITED_METHOD_CONTRACT;

        return (m_dwFlags & enum_flag_NGEN_IsFixedUp);
    }
    inline void SetFixedUp()
    {
        LIMITED_METHOD_CONTRACT;

        m_dwFlags |= enum_flag_NGEN_IsFixedUp;
    }

    inline BOOL IsNeedsRestoreCached() const
    {
        LIMITED_METHOD_CONTRACT;

        return (m_dwFlags & enum_flag_NGEN_IsNeedsRestoreCached);
    }

    inline BOOL GetCachedNeedsRestore() const
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(IsNeedsRestoreCached());
        return (m_dwFlags & enum_flag_NGEN_CachedNeedsRestore);
    }

    inline void SetCachedNeedsRestore(BOOL fNeedsRestore)
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(!IsNeedsRestoreCached());
        m_dwFlags |= enum_flag_NGEN_IsNeedsRestoreCached;
        if (fNeedsRestore) m_dwFlags |= enum_flag_NGEN_CachedNeedsRestore;
    }

    inline void SetIsOverridingInterface()
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;

        if ((m_dwFlags & enum_flag_NGEN_OverridingInterface) != 0) return;
        FastInterlockOr(EnsureWritablePages((ULONG *) &m_dwFlags), enum_flag_NGEN_OverridingInterface);
    }

    inline BOOL IsOverridingInterface() const
    {
        LIMITED_METHOD_CONTRACT;
        return (m_dwFlags & enum_flag_NGEN_OverridingInterface);
    }
#endif // FEATURE_PREJIT

    inline BOOL IsRemotingConfigChecked() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_dwFlags & enum_flag_RemotingConfigChecked;
    }
    inline void SetRemotingConfigChecked()
    {
        WRAPPER_NO_CONTRACT;
        // remembers that we went through the rigorous
        // checks to decide whether this class should be
        // activated locally or remote
        FastInterlockOr(EnsureWritablePages((ULONG *)&m_dwFlags), enum_flag_RemotingConfigChecked);
    }
    inline void TrySetRemotingConfigChecked()
    {
        WRAPPER_NO_CONTRACT;
        // remembers that we went through the rigorous
        // checks to decide whether this class should be
        // activated locally or remote
        if (EnsureWritablePagesNoThrow(&m_dwFlags, sizeof(m_dwFlags)))
            FastInterlockOr((ULONG *)&m_dwFlags, enum_flag_RemotingConfigChecked);
    }
    inline BOOL RequiresManagedActivation() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_dwFlags & enum_flag_RequiresManagedActivation;
    }
    inline void SetRequiresManagedActivation()
    {
        WRAPPER_NO_CONTRACT;
        FastInterlockOr(EnsureWritablePages((ULONG *) &m_dwFlags), enum_flag_RequiresManagedActivation|enum_flag_RemotingConfigChecked);
    }

    inline LOADERHANDLE GetExposedClassObjectHandle() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_hExposedClassObject;
    }

    void SetIsNotFullyLoadedForBuildMethodTable()
    {
        LIMITED_METHOD_CONTRACT;

        // Used only during method table initialization - no need for logging or Interlocked Exchange.
        m_dwFlags |= (MethodTableWriteableData::enum_flag_UnrestoredTypeKey |
                      MethodTableWriteableData::enum_flag_Unrestored |
                      MethodTableWriteableData::enum_flag_IsNotFullyLoaded |
                      MethodTableWriteableData::enum_flag_HasApproxParent);
    }

    void SetIsRestoredForBuildMethodTable()
    {
        LIMITED_METHOD_CONTRACT;

        // Used only during method table initialization - no need for logging or Interlocked Exchange.
        m_dwFlags &= ~(MethodTableWriteableData::enum_flag_UnrestoredTypeKey |
                       MethodTableWriteableData::enum_flag_Unrestored);
    }

    void SetIsFullyLoadedForBuildMethodTable()
    {
        LIMITED_METHOD_CONTRACT;

        // Used only during method table initialization - no need for logging or Interlocked Exchange.
        m_dwFlags &= ~(MethodTableWriteableData::enum_flag_UnrestoredTypeKey |
                       MethodTableWriteableData::enum_flag_Unrestored |
                       MethodTableWriteableData::enum_flag_IsNotFullyLoaded |
                       MethodTableWriteableData::enum_flag_HasApproxParent);
    }

    // Have the backout methods (Finalizer, Dispose, ReleaseHandle etc.) been prepared for this type? This currently only happens
    // for types derived from CriticalFinalizerObject.
    inline BOOL CriticalTypeHasBeenPrepared() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_dwFlags & enum_flag_CriticalTypePrepared;
    }
    inline void SetCriticalTypeHasBeenPrepared()
    {
        WRAPPER_NO_CONTRACT;
        FastInterlockOr(EnsureWritablePages((ULONG*)&m_dwFlags), enum_flag_CriticalTypePrepared);
    }

    inline CrossModuleGenericsStaticsInfo * GetCrossModuleGenericsStaticsInfo()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        SIZE_T size = sizeof(MethodTableWriteableData);
        return PTR_CrossModuleGenericsStaticsInfo(dac_cast<TADDR>(this) + size);
    }

};  // struct MethodTableWriteableData

typedef DPTR(MethodTableWriteableData) PTR_MethodTableWriteableData;
typedef DPTR(MethodTableWriteableData const) PTR_Const_MethodTableWriteableData;

#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING_ITF
inline
SystemVClassificationType CorInfoType2UnixAmd64Classification(CorElementType eeType)
{
    static const SystemVClassificationType toSystemVAmd64ClassificationTypeMap[] = {
        SystemVClassificationTypeUnknown,           // ELEMENT_TYPE_END
        SystemVClassificationTypeUnknown,           // ELEMENT_TYPE_VOID
        SystemVClassificationTypeInteger,           // ELEMENT_TYPE_BOOLEAN
        SystemVClassificationTypeInteger,           // ELEMENT_TYPE_CHAR
        SystemVClassificationTypeInteger,           // ELEMENT_TYPE_I1
        SystemVClassificationTypeInteger,           // ELEMENT_TYPE_U1
        SystemVClassificationTypeInteger,           // ELEMENT_TYPE_I2
        SystemVClassificationTypeInteger,           // ELEMENT_TYPE_U2
        SystemVClassificationTypeInteger,           // ELEMENT_TYPE_I4
        SystemVClassificationTypeInteger,           // ELEMENT_TYPE_U4
        SystemVClassificationTypeInteger,           // ELEMENT_TYPE_I8
        SystemVClassificationTypeInteger,           // ELEMENT_TYPE_U8
        SystemVClassificationTypeSSE,               // ELEMENT_TYPE_R4
        SystemVClassificationTypeSSE,               // ELEMENT_TYPE_R8
        SystemVClassificationTypeIntegerReference,  // ELEMENT_TYPE_STRING
        SystemVClassificationTypeInteger,           // ELEMENT_TYPE_PTR
        SystemVClassificationTypeIntegerReference,  // ELEMENT_TYPE_BYREF
        SystemVClassificationTypeStruct,            // ELEMENT_TYPE_VALUETYPE
        SystemVClassificationTypeIntegerReference,  // ELEMENT_TYPE_CLASS
        SystemVClassificationTypeIntegerReference,  // ELEMENT_TYPE_VAR              - (type variable)
        SystemVClassificationTypeIntegerReference,  // ELEMENT_TYPE_ARRAY
        SystemVClassificationTypeIntegerReference,  // ELEMENT_TYPE_GENERICINST
        SystemVClassificationTypeStruct,            // ELEMENT_TYPE_TYPEDBYREF
        SystemVClassificationTypeUnknown,           // ELEMENT_TYPE_VALUEARRAY_UNSUPPORTED
        SystemVClassificationTypeInteger,           // ELEMENT_TYPE_I
        SystemVClassificationTypeInteger,           // ELEMENT_TYPE_U
        SystemVClassificationTypeUnknown,           // ELEMENT_TYPE_R_UNSUPPORTED

        // put the correct type when we know our implementation
        SystemVClassificationTypeInteger,           // ELEMENT_TYPE_FNPTR
        SystemVClassificationTypeIntegerReference,  // ELEMENT_TYPE_OBJECT
        SystemVClassificationTypeIntegerReference,  // ELEMENT_TYPE_SZARRAY
        SystemVClassificationTypeIntegerReference,  // ELEMENT_TYPE_MVAR

        SystemVClassificationTypeUnknown,           // ELEMENT_TYPE_CMOD_REQD
        SystemVClassificationTypeUnknown,           // ELEMENT_TYPE_CMOD_OPT
        SystemVClassificationTypeUnknown,           // ELEMENT_TYPE_INTERNAL
    };

    _ASSERTE(sizeof(toSystemVAmd64ClassificationTypeMap) == ELEMENT_TYPE_MAX);
    _ASSERTE(eeType < (CorElementType) sizeof(toSystemVAmd64ClassificationTypeMap));
    // spot check of the map
    _ASSERTE((SystemVClassificationType)toSystemVAmd64ClassificationTypeMap[ELEMENT_TYPE_I4] == SystemVClassificationTypeInteger);
    _ASSERTE((SystemVClassificationType)toSystemVAmd64ClassificationTypeMap[ELEMENT_TYPE_PTR] == SystemVClassificationTypeInteger);
    _ASSERTE((SystemVClassificationType)toSystemVAmd64ClassificationTypeMap[ELEMENT_TYPE_TYPEDBYREF] == SystemVClassificationTypeStruct);

    return (((int)eeType) < ELEMENT_TYPE_MAX) ? (toSystemVAmd64ClassificationTypeMap[eeType]) : SystemVClassificationTypeUnknown;
};

#define SYSTEMV_EIGHT_BYTE_SIZE_IN_BYTES                    8 // Size of an eightbyte in bytes.
#define SYSTEMV_MAX_NUM_FIELDS_IN_REGISTER_PASSED_STRUCT    16 // Maximum number of fields in struct passed in registers

struct SystemVStructRegisterPassingHelper
{
    SystemVStructRegisterPassingHelper(unsigned int totalStructSize) :
        structSize(totalStructSize),
        eightByteCount(0),
        inEmbeddedStruct(false),
        currentUniqueOffsetField(0),
        largestFieldOffset(-1)
    {
        for (int i = 0; i < CLR_SYSTEMV_MAX_EIGHTBYTES_COUNT_TO_PASS_IN_REGISTERS; i++)
        {
            eightByteClassifications[i] = SystemVClassificationTypeNoClass;
            eightByteSizes[i] = 0;
            eightByteOffsets[i] = 0;
        }

        // Initialize the work arrays
        for (int i = 0; i < SYSTEMV_MAX_NUM_FIELDS_IN_REGISTER_PASSED_STRUCT; i++)
        {
            fieldClassifications[i] = SystemVClassificationTypeNoClass;
            fieldSizes[i] = 0;
            fieldOffsets[i] = 0;
        }
    }

    // Input state.
    unsigned int                    structSize;

    // These fields are the output; these are what is computed by the classification algorithm.
    unsigned int                    eightByteCount;
    SystemVClassificationType       eightByteClassifications[CLR_SYSTEMV_MAX_EIGHTBYTES_COUNT_TO_PASS_IN_REGISTERS];
    unsigned int                    eightByteSizes[CLR_SYSTEMV_MAX_EIGHTBYTES_COUNT_TO_PASS_IN_REGISTERS];
    unsigned int                    eightByteOffsets[CLR_SYSTEMV_MAX_EIGHTBYTES_COUNT_TO_PASS_IN_REGISTERS];

    // Helper members to track state.
    bool                            inEmbeddedStruct;
    unsigned int                    currentUniqueOffsetField; // A virtual field that could encompass many overlapping fields.
    int                             largestFieldOffset;
    SystemVClassificationType       fieldClassifications[SYSTEMV_MAX_NUM_FIELDS_IN_REGISTER_PASSED_STRUCT];
    unsigned int                    fieldSizes[SYSTEMV_MAX_NUM_FIELDS_IN_REGISTER_PASSED_STRUCT];
    unsigned int                    fieldOffsets[SYSTEMV_MAX_NUM_FIELDS_IN_REGISTER_PASSED_STRUCT];
};

typedef DPTR(SystemVStructRegisterPassingHelper) SystemVStructRegisterPassingHelperPtr;

#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING_ITF

//===============================================================================================
//
// GC data appears before the beginning of the MethodTable
//
//@GENERICS:
// Each generic type has a corresponding "generic" method table that serves the following
// purposes:
// * The method table pointer is used as a representative for the generic type e.g. in reflection 
// * MethodDescs for methods in the vtable are used for reflection; they should never be invoked.
// Some other information (e.g. BaseSize) makes no sense "generically" but unfortunately gets put in anyway.
//
// Each distinct instantiation of a generic type has its own MethodTable structure.
// However, the EEClass structure can be shared between compatible instantiations e.g. List<string> and List<object>.
// In that case, MethodDescs are also shared between compatible instantiations (but see below about generic methods).
// Hence the vtable entries for MethodTables belonging to such an EEClass are the same.
// 
// The non-vtable section of such MethodTables are only present for one of the instantiations (the first one
// requested) as non-vtable entries are never accessed through the vtable pointer of an object so it's always possible
// to ensure that they are accessed through the representative MethodTable that contains them.

// A MethodTable is the fundamental representation of type in the runtime.  It is this structure that
// objects point at (see code:Object).  It holds the size and GC layout of the type, as well as the dispatch table
// for virtual dispach (but not interface dispatch).  There is a distinct method table for every instance of
// a generic type. From here you can get to
// 
// * code:EEClass
// 
// Important fields
//     * code:MethodTable.m_pEEClass - pointer to the cold part of the type.
//     * code:MethodTable.m_pParentMethodTable - the method table of the parent type.
//     
class MethodTableBuilder;
class MethodTable
{
    /************************************
     *  FRIEND FUNCTIONS
     ************************************/
    // DO NOT ADD FRIENDS UNLESS ABSOLUTELY NECESSARY
    // USE ACCESSORS TO READ/WRITE private field members

    // Special access for setting up String object method table correctly
    friend class ClassLoader;
    friend class JIT_TrialAlloc;
#ifndef BINDER
    friend class Module;
#else
    friend class MdilModule;
    friend class CompactTypeBuilder;
#endif
    friend class EEClass;
    friend class MethodTableBuilder;
    friend class CheckAsmOffsets;
#if defined(DACCESS_COMPILE)
    friend class NativeImageDumper;
#endif

public:
    // Do some sanity checking to make sure it's a method table 
    // and not pointing to some random memory.  In particular
    // check that (apart from the special case of instantiated generic types) we have
    // GetCanonicalMethodTable() == this;
    BOOL SanityCheck();

    static void         CallFinalizer(Object *obj);

public:
    PTR_Module GetModule();
    PTR_Module GetModule_NoLogging();
    Assembly *GetAssembly();

    PTR_Module GetModuleIfLoaded();

    // GetDomain on an instantiated type, e.g. C<ty1,ty2> returns the SharedDomain if all the
    // constituent parts of the type are SharedDomain (i.e. domain-neutral), 
    // and returns an AppDomain if any of the parts are from an AppDomain, 
    // i.e. are domain-bound.  Note that if any of the parts are domain-bound
    // then they will all belong to the same domain.
    PTR_BaseDomain GetDomain();

    // Does this immediate item live in an NGEN module?
    BOOL IsZapped();

    // For types that are part of an ngen-ed assembly this gets the
    // Module* that contains this methodtable.
    PTR_Module GetZapModule();

    // For regular, non-constructed types, GetLoaderModule() == GetModule()
    // For constructed types (e.g. int[], Dict<int[], C>) the hash table through which a type
    // is accessed lives in a "loader module". The rule for determining the loader module must ensure
    // that a type never outlives its loader module with respect to app-domain unloading
    //
    // GetModuleForStatics() is the third kind of module. GetModuleForStatics() is module that 
    // statics are attached to.
    PTR_Module GetLoaderModule();
    PTR_LoaderAllocator GetLoaderAllocator();

#ifndef BINDER
    void SetLoaderModule(Module* pModule);
#else
    void SetLoaderModule(MdilModule* pModule);
#endif
    void SetLoaderAllocator(LoaderAllocator* pAllocator);

    // Get the domain local module - useful for static init checks
    PTR_DomainLocalModule GetDomainLocalModule(AppDomain * pAppDomain);

#ifndef DACCESS_COMPILE
    // Version of GetDomainLocalModule which relies on the current AppDomain
    PTR_DomainLocalModule   GetDomainLocalModule();
#endif

    // Return whether the type lives in the shared domain.
    BOOL IsDomainNeutral();

    MethodTable *LoadEnclosingMethodTable(ClassLoadLevel targetLevel = CLASS_DEPENDENCIES_LOADED);

    LPCWSTR GetPathForErrorMessages();

    //-------------------------------------------------------------------
    // COM INTEROP
    //
    BOOL IsProjectedFromWinRT();
    BOOL IsExportedToWinRT();
    BOOL IsWinRTDelegate();
    BOOL IsWinRTRedirectedInterface(TypeHandle::InteropKind interopKind);
    BOOL IsWinRTRedirectedDelegate();

#ifdef FEATURE_COMINTEROP
    TypeHandle GetCoClassForInterface();

private:
    TypeHandle SetupCoClassForInterface();

public:
    DWORD IsComClassInterface();

    // Retrieves the COM interface type.
    CorIfaceAttr    GetComInterfaceType();
    void SetComInterfaceType(CorIfaceAttr ItfType);
    
    // Determines whether this is a WinRT-legal type
    BOOL IsLegalWinRTType(OBJECTREF *poref);

    // Determines whether this is a WinRT-legal type - don't use it with array
    BOOL IsLegalNonArrayWinRTType();
    
    MethodTable *GetDefaultWinRTInterface();

    OBJECTHANDLE GetOHDelegate();
    void SetOHDelegate (OBJECTHANDLE _ohDelegate);

    CorClassIfaceAttr GetComClassInterfaceType();
    TypeHandle GetDefItfForComClassItf();

    void GetEventInterfaceInfo(MethodTable **ppSrcItfType, MethodTable **ppEvProvType);

    BOOL            IsExtensibleRCW();

    // mark the class type as COM object class
    void SetComObjectType();

#if defined(FEATURE_TYPEEQUIVALENCE) || defined(FEATURE_REMOTING)
    // mark the type as opted into type equivalence
    void SetHasTypeEquivalence();
#endif

    // Helper to get parent class skipping over COM class in 
    // the hierarchy
    MethodTable* GetComPlusParentMethodTable();

    // class is a com object class
    BOOL IsComObjectType()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return GetFlag(enum_flag_ComObject);
    }
    // class is a WinRT object class (is itself or derives from a ProjectedFromWinRT class)
    BOOL IsWinRTObjectType();

    DWORD IsComImport();

    // class is a special COM event interface
    int IsComEventItfType();

    //-------------------------------------------------------------------
    // Sparse VTables.   These require a SparseVTableMap in the EEClass in
    // order to record how the CLR's vtable slots map across to COM 
    // Interop slots.
    //
    int IsSparseForCOMInterop();

    // COM interop helpers
    // accessors for m_pComData
    ComCallWrapperTemplate *GetComCallWrapperTemplate();
    BOOL                    SetComCallWrapperTemplate(ComCallWrapperTemplate *pTemplate);
#ifdef FEATURE_COMINTEROP_UNMANAGED_ACTIVATION
    ClassFactoryBase       *GetComClassFactory();
    BOOL                    SetComClassFactory(ClassFactoryBase *pFactory);
#endif // FEATURE_COMINTEROP_UNMANAGED_ACTIVATION
    
    OBJECTREF GetObjCreateDelegate();
    void SetObjCreateDelegate(OBJECTREF orDelegate);

private:
    // This is for COM Interop backwards compatibility
    BOOL InsertComInteropData(InteropMethodTableData *pData);
    InteropMethodTableData *CreateComInteropData(AllocMemTracker *pamTracker);

public:
    InteropMethodTableData *LookupComInteropData();
    // This is the preferable entrypoint, as it will make sure that all
    // parent MT's have their interop data created, and will create and
    // add this MT's data if not available. The caller should make sure that
    // an appropriate lock is taken to prevent duplicates.
    // NOTE: The current caller of this is ComInterop, and it makes calls
    // under its own lock to ensure not duplicates.
    InteropMethodTableData *GetComInteropData();

#else // !FEATURE_COMINTEROP
    BOOL IsComObjectType()
    {
        SUPPORTS_DAC;
        return FALSE;
    }
    BOOL IsWinRTObjectType()
    {
        LIMITED_METHOD_CONTRACT;
        return FALSE;
    }
#endif // !FEATURE_COMINTEROP

#ifdef FEATURE_ICASTABLE
    void SetICastable();
#endif  

    BOOL IsICastable(); // This type implements ICastable interface

#ifdef FEATURE_TYPEEQUIVALENCE
    // type has opted into type equivalence or is instantiated by/derived from a type that is
    BOOL HasTypeEquivalence()
    {
        LIMITED_METHOD_CONTRACT;
        return GetFlag(enum_flag_HasTypeEquivalence);
    }
#else
    BOOL HasTypeEquivalence()
    {
        LIMITED_METHOD_CONTRACT;
        return FALSE;
    }
#endif

    //-------------------------------------------------------------------
    // DYNAMIC ADDITION OF INTERFACES FOR COM INTEROP
    //
    // Support for dynamically added interfaces on extensible RCW's.

#ifdef FEATURE_COMINTEROP
    PTR_InterfaceInfo GetDynamicallyAddedInterfaceMap();
    unsigned GetNumDynamicallyAddedInterfaces();
    BOOL FindDynamicallyAddedInterface(MethodTable *pInterface);
    void AddDynamicInterface(MethodTable *pItfMT);

    BOOL HasDynamicInterfaceMap()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        // currently all ComObjects except
        // for __ComObject have dynamic Interface maps
        return GetNumInterfaces() > 0 && IsComObjectType() && !ParentEquals(g_pObjectClass);
    }
#endif // FEATURE_COMINTEROP

    BOOL IsIntrospectionOnly();

    // Checks this type and its instantiation for "IsIntrospectionOnly"
    BOOL ContainsIntrospectionOnlyTypes();

#ifndef DACCESS_COMPILE
    VOID EnsureActive();
    VOID EnsureInstanceActive();
#endif

    CHECK CheckActivated();
    CHECK CheckInstanceActivated();
    
    //-------------------------------------------------------------------
    // THE DEFAULT CONSTRUCTOR
    //

public:
    BOOL HasDefaultConstructor();
    void SetHasDefaultConstructor();
    WORD GetDefaultConstructorSlot();
    MethodDesc *GetDefaultConstructor();

    BOOL HasExplicitOrImplicitPublicDefaultConstructor();

    //-------------------------------------------------------------------
    // THE CLASS INITIALIZATION CONDITION 
    //  (and related DomainLocalBlock/DomainLocalModule storage)
    //
    // - populate the DomainLocalModule if needed
    // - run the cctor 
    //

public:

    // checks whether the class initialiser should be run on this class, and runs it if necessary
    void CheckRunClassInitThrowing();

    // checks whether or not the non-beforefieldinit class initializers have been run for all types in this type's
    // inheritance hierarchy, and runs them if necessary. This simulates the behavior of running class constructors
    // during object construction.
    void CheckRunClassInitAsIfConstructingThrowing();

#if defined(FEATURE_UNIX_AMD64_STRUCT_PASSING_ITF)
    // Helper function for ClassifyEightBytesWithManagedLayout and ClassifyEightBytesWithNativeLayout
    static SystemVClassificationType ReClassifyField(SystemVClassificationType originalClassification, SystemVClassificationType newFieldClassification);

    // Builds the internal data structures and classifies struct eightbytes for Amd System V calling convention.
    bool ClassifyEightBytes(SystemVStructRegisterPassingHelperPtr helperPtr, unsigned int nestingLevel, unsigned int startOffsetOfStruct, bool isNativeStruct);
#endif // defined(FEATURE_UNIX_AMD64_STRUCT_PASSING_ITF)

    // Copy m_dwFlags from another method table
    void CopyFlags(MethodTable * pOldMT)
    {
        LIMITED_METHOD_CONTRACT;
        m_dwFlags = pOldMT->m_dwFlags;
        m_wFlags2 = pOldMT->m_wFlags2;
    }

    // Init the m_dwFlags field for an array
    void SetIsArray(CorElementType arrayType, CorElementType elementType);
        
    BOOL IsClassPreInited();
    
    // mark the class as having its cctor run.  
#ifndef DACCESS_COMPILE
    void SetClassInited();
    BOOL  IsClassInited(AppDomain* pAppDomain = NULL);   

    BOOL IsInitError();
    void SetClassInitError();
#endif

    inline BOOL IsGlobalClass()
    {
        WRAPPER_NO_CONTRACT;
        return (GetTypeDefRid() == RidFromToken(COR_GLOBAL_PARENT_TOKEN));
    }

    // uniquely identifes this type in the Domain table
    DWORD GetClassIndex();

    bool ClassRequiresUnmanagedCodeCheck();

private:

#if defined(FEATURE_UNIX_AMD64_STRUCT_PASSING_ITF)
    void AssignClassifiedEightByteTypes(SystemVStructRegisterPassingHelperPtr helperPtr, unsigned int nestingLevel);
    // Builds the internal data structures and classifies struct eightbytes for Amd System V calling convention.
    bool ClassifyEightBytesWithManagedLayout(SystemVStructRegisterPassingHelperPtr helperPtr, unsigned int nestingLevel, unsigned int startOffsetOfStruct, bool isNativeStruct);
    bool ClassifyEightBytesWithNativeLayout(SystemVStructRegisterPassingHelperPtr helperPtr, unsigned int nestingLevel, unsigned int startOffsetOfStruct, bool isNativeStruct);
#endif // defined(FEATURE_UNIX_AMD64_STRUCT_PASSING_ITF)

    DWORD   GetClassIndexFromToken(mdTypeDef typeToken)
    {
        LIMITED_METHOD_CONTRACT;
        return RidFromToken(typeToken) - 1;
    }
    
    // called from CheckRunClassInitThrowing().  The type wasn't marked as
    // inited while we were there, so let's attempt to do the work.
    void  DoRunClassInitThrowing();

    BOOL RunClassInitEx(OBJECTREF *pThrowable); 

public:
    //-------------------------------------------------------------------
    // THE CLASS CONSTRUCTOR
    //

    MethodDesc * GetClassConstructor();

    BOOL HasClassConstructor();
    void SetHasClassConstructor();
    WORD GetClassConstructorSlot();
    void SetClassConstructorSlot (WORD wCCtorSlot);

    ClassCtorInfoEntry* GetClassCtorInfoIfExists();


    void GetSavedExtent(TADDR *ppStart, TADDR *ppEnd);

    //-------------------------------------------------------------------
    // Save/Fixup/Restore/NeedsRestore
    //
    // Restore this method table if it's not already restored
    // This is done by forcing a class load which in turn calls the Restore method
    // The pending list is required for restoring types that reference themselves through
    // instantiations of the superclass or interfaces e.g. System.Int32 : IComparable<System.Int32>


#ifdef FEATURE_PREJIT

    void Save(DataImage *image, DWORD profilingFlags);
    void Fixup(DataImage *image);

    // This is different from !IsRestored() in that it checks if restoring
    // will ever be needed for this ngened data-structure.
    // This is to be used at ngen time of a dependent module to determine 
    // if it can be accessed directly, or if the restoring mechanism needs
    // to be hooked in.
    BOOL ComputeNeedsRestore(DataImage *image, TypeHandleList *pVisited);

    BOOL NeedsRestore(DataImage *image)
    {
        WRAPPER_NO_CONTRACT;
        return ComputeNeedsRestore(image, NULL);
    }

private:
    BOOL ComputeNeedsRestoreWorker(DataImage *image, TypeHandleList *pVisited);

public:
    // This returns true at NGen time if we can eager bind to all dictionaries along the inheritance chain
    BOOL CanEagerBindToParentDictionaries(DataImage *image, TypeHandleList *pVisited);

    // This returns true at NGen time if we may need to attach statics to
    // other module than current loader module at runtime
    BOOL NeedsCrossModuleGenericsStaticsInfo();
    
    // Returns true at NGen time if we may need to write into the MethodTable at runtime
    BOOL IsWriteable();

#endif // FEATURE_PREJIT

    void AllocateRegularStaticBoxes();
    static OBJECTREF AllocateStaticBox(MethodTable* pFieldMT, BOOL fPinned, OBJECTHANDLE* pHandle = 0);

    void CheckRestore();

    // Perform restore actions on type key components of method table (EEClass pointer + Module, generic args)
    void DoRestoreTypeKey();

    inline BOOL HasUnrestoredTypeKey() const
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return !IsPreRestored() && 
            (GetWriteableData()->m_dwFlags & MethodTableWriteableData::enum_flag_UnrestoredTypeKey) != 0;
    }

    // Actually do the restore actions on the method table
    void Restore();
    
    void SetIsRestored();
 
    inline BOOL IsRestored_NoLogging()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        // If we are prerestored then we are considered a restored methodtable.
        // Note that IsPreRestored is always false for jitted code.
        if (IsPreRestored())
            return TRUE;

        return !(GetWriteableData_NoLogging()->m_dwFlags & MethodTableWriteableData::enum_flag_Unrestored);
    }
    inline BOOL IsRestored()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        g_IBCLogger.LogMethodTableAccess(this);

        // If we are prerestored then we are considered a restored methodtable.
        // Note that IsPreRestored is always false for jitted code.
        if (IsPreRestored())
            return TRUE;

        return !(GetWriteableData()->m_dwFlags & MethodTableWriteableData::enum_flag_Unrestored);
    }

    //-------------------------------------------------------------------
    // LOAD LEVEL 
    //
    // The load level of a method table is derived from various flag bits
    // See classloadlevel.h for details of each level
    //
    // Level CLASS_LOADED (fully loaded) is special: a type only
    // reaches this level once all of its dependent types are also at
    // this level (generic arguments, parent, interfaces, etc).
    // Fully loading a type to this level is done outside locks, hence the need for
    // a single atomic action that sets the level.
    // 
    inline void SetIsFullyLoaded()
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;

        PRECONDITION(!HasApproxParent());
        PRECONDITION(IsRestored_NoLogging());

        FastInterlockAnd(EnsureWritablePages(&GetWriteableDataForWrite()->m_dwFlags), ~MethodTableWriteableData::enum_flag_IsNotFullyLoaded);
    }

    // Equivalent to GetLoadLevel() == CLASS_LOADED
    inline BOOL IsFullyLoaded() 
    {
        WRAPPER_NO_CONTRACT;

        return (IsPreRestored())
            || (GetWriteableData()->m_dwFlags & MethodTableWriteableData::enum_flag_IsNotFullyLoaded) == 0;
    }

    inline BOOL IsSkipWinRTOverride()
    {
        LIMITED_METHOD_CONTRACT;
        return (GetWriteableData_NoLogging()->m_dwFlags & MethodTableWriteableData::enum_flag_SkipWinRTOverride);
    }
    
    inline void SetSkipWinRTOverride()
    {
        WRAPPER_NO_CONTRACT;
        FastInterlockOr(EnsureWritablePages(&GetWriteableDataForWrite_NoLogging()->m_dwFlags), MethodTableWriteableData::enum_flag_SkipWinRTOverride);
    }
    
    inline void SetIsDependenciesLoaded()
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;

        PRECONDITION(!HasApproxParent());
        PRECONDITION(IsRestored_NoLogging());

        FastInterlockOr(EnsureWritablePages(&GetWriteableDataForWrite()->m_dwFlags), MethodTableWriteableData::enum_flag_DependenciesLoaded);
    }

    inline ClassLoadLevel GetLoadLevel()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        g_IBCLogger.LogMethodTableAccess(this);

        // Fast path for zapped images
        if (IsPreRestored())
            return CLASS_LOADED;

        DWORD dwFlags = GetWriteableData()->m_dwFlags;

        if (dwFlags & MethodTableWriteableData::enum_flag_IsNotFullyLoaded)
        {
            if (dwFlags & MethodTableWriteableData::enum_flag_UnrestoredTypeKey)
                return CLASS_LOAD_UNRESTOREDTYPEKEY;

            if (dwFlags & MethodTableWriteableData::enum_flag_Unrestored)
                return CLASS_LOAD_UNRESTORED;

            if (dwFlags & MethodTableWriteableData::enum_flag_HasApproxParent)
                return CLASS_LOAD_APPROXPARENTS;

            if (!(dwFlags & MethodTableWriteableData::enum_flag_DependenciesLoaded))
                return CLASS_LOAD_EXACTPARENTS;

            return CLASS_DEPENDENCIES_LOADED;
        }

        return CLASS_LOADED;
    }

#ifdef _DEBUG
    CHECK CheckLoadLevel(ClassLoadLevel level)
    {
        LIMITED_METHOD_CONTRACT;
        return TypeHandle(this).CheckLoadLevel(level);
    }
#endif
 

    void DoFullyLoad(Generics::RecursionGraph * const pVisited, const ClassLoadLevel level, DFLPendingList * const pPending, BOOL * const pfBailed,
                     const InstantiationContext * const pInstContext);

    //-------------------------------------------------------------------
    // METHOD TABLES AS TYPE DESCRIPTORS
    //
    // A MethodTable can represeent a type such as "String" or an 
    // instantiated type such as "List<String>".
    //
    
    inline BOOL IsInterface()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return GetFlag(enum_flag_Category_Mask) == enum_flag_Category_Interface;
    }

    void SetIsInterface()
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(GetFlag(enum_flag_Category_Mask) == 0);
        SetFlag(enum_flag_Category_Interface);
    }

    inline BOOL IsSealed();

    inline BOOL IsAbstract();

    BOOL IsExternallyVisible();

    // Get the instantiation for this instantiated type e.g. for Dict<string,int> 
    // this would be an array {string,int}
    // If not instantiated, return NULL
    Instantiation GetInstantiation();

    // Get the instantiation for an instantiated type or a pointer to the
    // element type for an array
    Instantiation GetClassOrArrayInstantiation();
    Instantiation GetArrayInstantiation();

    // Does this method table require that additional modules be loaded?
    inline BOOL HasModuleDependencies()
    {
        LIMITED_METHOD_CONTRACT;
        return GetFlag(enum_flag_HasModuleDependencies);
    }

    inline void SetHasModuleDependencies()
    {
        SetFlag(enum_flag_HasModuleDependencies);
    }

    // See the comment in code:MethodTable.DoFullyLoad for detailed description.
    inline BOOL DependsOnEquivalentOrForwardedStructs()
    {
        LIMITED_METHOD_CONTRACT;
        return GetFlag(enum_flag_DependsOnEquivalentOrForwardedStructs);
    }

    inline void SetDependsOnEquivalentOrForwardedStructs()
    {
        SetFlag(enum_flag_DependsOnEquivalentOrForwardedStructs);
    }

    // Is this a method table for a generic type instantiation, e.g. List<string>?
    inline BOOL HasInstantiation();

    // Returns true for any class which is either itself a generic
    // instantiation or is derived from a generic
    // instantiation anywhere in it's class hierarchy,
    //
    // e.g. class D : C<int>
    // or class E : D, class D : C<int>
    //
    // Does not return true just because the class supports
    // an instantiated interface type.
    BOOL HasGenericClassInstantiationInHierarchy()
    {
        WRAPPER_NO_CONTRACT;
        return GetNumDicts() != 0;
    }

    // Is this an instantiation of a generic class at its formal
    // type parameters ie. List<T> ?
    inline BOOL IsGenericTypeDefinition();

    BOOL ContainsGenericMethodVariables();

    static BOOL ComputeContainsGenericVariables(Instantiation inst);

    inline void SetContainsGenericVariables()
    {
        LIMITED_METHOD_CONTRACT;
        SetFlag(enum_flag_ContainsGenericVariables);
    }

    inline void SetHasVariance()
    {
        LIMITED_METHOD_CONTRACT;
        SetFlag(enum_flag_HasVariance);
    }

    inline BOOL HasVariance()
    {
        LIMITED_METHOD_CONTRACT;
        return GetFlag(enum_flag_HasVariance);
    }

    // Is this something like List<T> or List<Stack<T>>?
    // List<Blah<T>> only exists for reflection and verification.
    inline DWORD ContainsGenericVariables(BOOL methodVarsOnly = FALSE)
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;
        if (methodVarsOnly)
            return ContainsGenericMethodVariables();
        else
            return GetFlag(enum_flag_ContainsGenericVariables);
    }


    inline BOOL ContainsStackPtr();

    // class is a com object class
    Module* GetDefiningModuleForOpenType();
    
    inline BOOL IsTypicalTypeDefinition()       
    {
        LIMITED_METHOD_CONTRACT;
        return !HasInstantiation() || IsGenericTypeDefinition();
    }

    typedef enum 
    { 
        modeProjected = 0x1, 
        modeRedirected = 0x2, 
        modeAll = modeProjected|modeRedirected 
    } Mode;

    // Is this a generic interface/delegate that can be used for COM interop?
    inline BOOL SupportsGenericInterop(TypeHandle::InteropKind interopKind, Mode = modeAll);

    BOOL HasSameTypeDefAs(MethodTable *pMT);
    BOOL HasSameTypeDefAs_NoLogging(MethodTable *pMT);

    //-------------------------------------------------------------------
    // GENERICS & CODE SHARING 
    //

    BOOL IsSharedByGenericInstantiations();

    // If this is a "representative" generic MT or a non-generic (regular) MT return true
    inline BOOL IsCanonicalMethodTable();

    // Return the canonical representative MT amongst the set of MT's that share
    // code with the given MT because of generics.
    PTR_MethodTable GetCanonicalMethodTable();

    // Returns fixup if canonical method table needs fixing up, NULL otherwise
    TADDR GetCanonicalMethodTableFixup();

    //-------------------------------------------------------------------
    // Accessing methods by slot number
    //
    // Some of these functions are also currently used to get non-virtual
    // methods, relying on the assumption that they are contiguous.  This
    // is not true for non-virtual methods in generic instantiations, which
    // only live on the canonical method table.

    enum
    {
        NO_SLOT = 0xffff // a unique slot number used to indicate "empty" for fields that record slot numbers
    };

#ifndef BINDER // the binder works with a slightly different representation, so remove these
    PCODE GetSlot(UINT32 slotNumber)
    {
        WRAPPER_NO_CONTRACT;
        STATIC_CONTRACT_SO_TOLERANT;
        CONSISTENCY_CHECK(slotNumber < GetNumVtableSlots());
        PTR_PCODE pSlot = GetSlotPtrRaw(slotNumber);
        if (IsZapped() && slotNumber >= GetNumVirtuals())
        {
            // Non-virtual slots in NGened images are relative pointers
            return RelativePointer<PCODE>::GetValueAtPtr(dac_cast<TADDR>(pSlot));
        }
        return *pSlot;
    }

    // Special-case for when we know that the slot number corresponds
    // to a virtual method.
    inline PCODE GetSlotForVirtual(UINT32 slotNum)
    {
        LIMITED_METHOD_CONTRACT;

        CONSISTENCY_CHECK(slotNum < GetNumVirtuals());
        // Virtual slots live in chunks pointed to by vtable indirections
        return *(GetVtableIndirections()[GetIndexOfVtableIndirection(slotNum)] + GetIndexAfterVtableIndirection(slotNum));
    }

    PTR_PCODE GetSlotPtrRaw(UINT32 slotNum)
    {
        WRAPPER_NO_CONTRACT;
        STATIC_CONTRACT_SO_TOLERANT;
        CONSISTENCY_CHECK(slotNum < GetNumVtableSlots());

        if (slotNum < GetNumVirtuals())
        {
            // Virtual slots live in chunks pointed to by vtable indirections
            return GetVtableIndirections()[GetIndexOfVtableIndirection(slotNum)] + GetIndexAfterVtableIndirection(slotNum);
        }
        else if (HasSingleNonVirtualSlot())
        {
            // Non-virtual slots < GetNumVtableSlots live in a single chunk pointed to by an optional member,
            // except when there is only one in which case it lives in the optional member itself
            _ASSERTE(slotNum == GetNumVirtuals());
            return dac_cast<PTR_PCODE>(GetNonVirtualSlotsPtr());
        }
        else
        {
            // Non-virtual slots < GetNumVtableSlots live in a single chunk pointed to by an optional member
            _ASSERTE(HasNonVirtualSlotsArray());
            g_IBCLogger.LogMethodTableNonVirtualSlotsAccess(this);
            return GetNonVirtualSlotsArray() + (slotNum - GetNumVirtuals());
        }
    }

    PTR_PCODE GetSlotPtr(UINT32 slotNum)
    {
        WRAPPER_NO_CONTRACT;
        STATIC_CONTRACT_SO_TOLERANT;

        // Slots in NGened images are relative pointers
        CONSISTENCY_CHECK(!IsZapped());

        return GetSlotPtrRaw(slotNum);
    }

    void SetSlot(UINT32 slotNum, PCODE slotVal);
#endif

    //-------------------------------------------------------------------
    // The VTABLE
    //
    // Rather than the traditional array of code pointers (or "slots") we use a two-level vtable in
    // which slots for virtual methods live in chunks.  Doing so allows the chunks to be shared among
    // method tables (the most common example being between parent and child classes where the child
    // does not override any method in the chunk).  This yields substantial space savings at the fixed 
    // cost of one additional indirection for a virtual call.
    //
    // Note that none of this should be visible outside the implementation of MethodTable; all other
    // code continues to refer to a virtual method via the traditional slot number.  This is similar to
    // how we refer to non-virtual methods as having a slot number despite having long ago moved their
    // code pointers out of the vtable.
    // 
    // Consider a class where GetNumVirtuals is 5 and (for the sake of the example) assume we break 
    // the vtable into chunks of size 3.  The layout would be as follows:
    //
    //   pMT                       chunk 1                   chunk 2
    //   ------------------        ------------------        ------------------
    //   |                |        |      M1()      |        |      M4()      |
    //   |   fixed-size   |        ------------------        ------------------
    //   |   portion of   |        |      M2()      |        |      M5()      |
    //   |   MethodTable  |        ------------------        ------------------
    //   |                |        |      M3()      |
    //   ------------------        ------------------
    //   | ptr to chunk 1 |
    //   ------------------
    //   | ptr to chunk 2 |
    //   ------------------
    //
    // We refer to "ptr to chunk 1" and "ptr to chunk 2" as "indirection slots."
    // 
    // The current chunking strategy is independent of class properties; all are of size 8.  Several 
    // other strategies were tried, and the only one that has performed better empirically is to begin 
    // with a single chunk of size 4 (matching the number of virtuals in System.Object) and then
    // continue with chunks of size 8.  However it was a small improvement and required the run-time 
    // helpers listed below to be measurably slower.
    //
    // If you want to change this, you should only need to modify the first four functions below
    // along with any assembly helper that has taken a dependency on the layout.  Currently,
    // those consist of:
    //     JIT_IsInstanceOfInterface
    //     JIT_ChkCastInterface
    //     Transparent proxy stub
    //
    // This layout only applies to the virtual methods in a class (those with slot number below GetNumVirtuals).
    // Non-virtual methods that are in the vtable (those with slot numbers between GetNumVirtuals and
    // GetNumVtableSlots) are laid out in a single chunk pointed to by an optional member.
    // See GetSlotPtrRaw for more details.

    #define VTABLE_SLOTS_PER_CHUNK 8
    #define VTABLE_SLOTS_PER_CHUNK_LOG2 3

    static DWORD GetIndexOfVtableIndirection(DWORD slotNum);
    static DWORD GetStartSlotForVtableIndirection(UINT32 indirectionIndex, DWORD wNumVirtuals);
    static DWORD GetEndSlotForVtableIndirection(UINT32 indirectionIndex, DWORD wNumVirtuals);
    static UINT32 GetIndexAfterVtableIndirection(UINT32 slotNum);
    static DWORD GetNumVtableIndirections(DWORD wNumVirtuals);
    PTR_PTR_PCODE GetVtableIndirections();
    DWORD GetNumVtableIndirections();

    class VtableIndirectionSlotIterator
    {
        friend class MethodTable;

    private:
        PTR_PTR_PCODE m_pSlot;
        DWORD m_i;
        DWORD m_count;
        PTR_MethodTable m_pMT;

        VtableIndirectionSlotIterator(MethodTable *pMT);
        VtableIndirectionSlotIterator(MethodTable *pMT, DWORD index);

    public:
        BOOL Next();
        BOOL Finished();
        DWORD GetIndex();
        DWORD GetOffsetFromMethodTable();
        PTR_PCODE GetIndirectionSlot();

#ifndef DACCESS_COMPILE
        void SetIndirectionSlot(PTR_PCODE pChunk);
#endif

        DWORD GetStartSlot();
        DWORD GetEndSlot();
        DWORD GetNumSlots();
        DWORD GetSize();
    };  // class VtableIndirectionSlotIterator

    VtableIndirectionSlotIterator IterateVtableIndirectionSlots();
    VtableIndirectionSlotIterator IterateVtableIndirectionSlotsFrom(DWORD index);

#ifdef FEATURE_PREJIT
    static BOOL CanShareVtableChunksFrom(MethodTable *pTargetMT, Module *pCurrentLoaderModule, Module *pCurrentPreferredZapModule);
    BOOL CanInternVtableChunk(DataImage *image, VtableIndirectionSlotIterator it);
#else
    static BOOL CanShareVtableChunksFrom(MethodTable *pTargetMT, Module *pCurrentLoaderModule);
#endif

    inline BOOL HasNonVirtualSlots()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return GetFlag(enum_flag_HasNonVirtualSlots);
    }

    inline BOOL HasSingleNonVirtualSlot()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return GetFlag(enum_flag_HasSingleNonVirtualSlot);
    }

    inline BOOL HasNonVirtualSlotsArray()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return HasNonVirtualSlots() && !HasSingleNonVirtualSlot();
    }

    TADDR GetNonVirtualSlotsPtr();

    inline PTR_PCODE GetNonVirtualSlotsArray()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        _ASSERTE(HasNonVirtualSlotsArray());        
        return RelativePointer<PTR_PCODE>::GetValueAtPtr(GetNonVirtualSlotsPtr());
    }

#ifndef DACCESS_COMPILE
    inline void SetNonVirtualSlotsArray(PTR_PCODE slots)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(HasNonVirtualSlotsArray());
        
        RelativePointer<PTR_PCODE>::SetValueAtPtr(GetNonVirtualSlotsPtr(), slots);
    }

    inline void SetHasSingleNonVirtualSlot()
    {
        LIMITED_METHOD_CONTRACT;
        SetFlag(enum_flag_HasSingleNonVirtualSlot);
    }
#endif
    
    inline unsigned GetNonVirtualSlotsArraySize()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return GetNumNonVirtualSlots() * sizeof(PCODE);
    }

    inline WORD GetNumNonVirtualSlots();

    inline WORD GetNumVirtuals()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        g_IBCLogger.LogMethodTableAccess(this);
        return GetNumVirtuals_NoLogging();
    }

    inline WORD GetNumVirtuals_NoLogging()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return m_wNumVirtuals;
    }

    inline void SetNumVirtuals (WORD wNumVtableSlots)
    {
        LIMITED_METHOD_CONTRACT;
        m_wNumVirtuals = wNumVtableSlots;
    }

    unsigned GetNumParentVirtuals()
    {
        LIMITED_METHOD_CONTRACT;
        if (IsInterface() || IsTransparentProxy()) {
            return 0;
        }
        MethodTable *pMTParent = GetParentMethodTable();
        g_IBCLogger.LogMethodTableAccess(this);
        return pMTParent == NULL ? 0 : pMTParent->GetNumVirtuals();
    }

    static inline DWORD GetVtableOffset()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return (sizeof(MethodTable));
    }

    // Return total methods: virtual, static, and instance method slots.
    WORD GetNumMethods();

    // Return number of slots in this methodtable. This is just an information about the layout of the methodtable, it should not be used
    // for functionality checks. Do not confuse with GetNumVirtuals()!
    WORD GetNumVtableSlots()
    {
        LIMITED_METHOD_DAC_CONTRACT; 
        return GetNumVirtuals() + GetNumNonVirtualSlots();
    }

    //-------------------------------------------------------------------
    // Slots <-> the MethodDesc associated with the slot.
    //

    MethodDesc* GetMethodDescForSlot(DWORD slot);

    static MethodDesc*  GetMethodDescForSlotAddress(PCODE addr, BOOL fSpeculative = FALSE);

    PCODE GetRestoredSlot(DWORD slot);

    // Returns MethodTable that GetRestoredSlot get its values from
    MethodTable * GetRestoredSlotMT(DWORD slot);

    // Used to map methods on the same slot between instantiations.
    MethodDesc * GetParallelMethodDesc(MethodDesc * pDefMD);

    //-------------------------------------------------------------------
    // BoxedEntryPoint MethodDescs.  
    //
    // Virtual methods on structs have BoxedEntryPoint method descs in their vtable.
    // See also notes for MethodDesc::FindOrCreateAssociatedMethodDesc.  You should
    // probably be using that function if you need to map between unboxing
    // stubs and non-unboxing stubs.

    MethodDesc* GetBoxedEntryPointMD(MethodDesc *pMD);

    MethodDesc* GetUnboxedEntryPointMD(MethodDesc *pMD);
    MethodDesc* GetExistingUnboxedEntryPointMD(MethodDesc *pMD);

    //-------------------------------------------------------------------
    // FIELD LAYOUT, OBJECT SIZE ETC. 
    //

    inline BOOL HasLayout();

    inline EEClassLayoutInfo *GetLayoutInfo();

    inline BOOL IsBlittable();

    inline BOOL IsManagedSequential();

    inline BOOL HasExplicitSize();

    UINT32 GetNativeSize();

    DWORD           GetBaseSize()
    { 
        LIMITED_METHOD_DAC_CONTRACT;
        return(m_BaseSize); 
    }

    void            SetBaseSize(DWORD baseSize)       
    { 
        LIMITED_METHOD_CONTRACT;
        m_BaseSize = baseSize; 
    }

    BOOL            IsStringOrArray() const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return HasComponentSize();
    }

    BOOL IsString()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return HasComponentSize() && !IsArray();
    }

    BOOL            HasComponentSize() const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return GetFlag(enum_flag_HasComponentSize);
    }

    // returns random combination of flags if this doesn't have a component size
    WORD            RawGetComponentSize()
    {
        LIMITED_METHOD_DAC_CONTRACT;
#if BIGENDIAN
        return *((WORD*)&m_dwFlags + 1);
#else // !BIGENDIAN
        return *(WORD*)&m_dwFlags;
#endif // !BIGENDIAN
    }

    // returns 0 if this doesn't have a component size

    // The component size is actually 16-bit WORD, but this method is returning SIZE_T to ensure
    // that SIZE_T is used everywhere for object size computation. It is necessary to support
    // objects bigger than 2GB.
    SIZE_T          GetComponentSize()  
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return HasComponentSize() ? RawGetComponentSize() : 0;
    }

    void SetComponentSize(WORD wComponentSize)
    {
        LIMITED_METHOD_CONTRACT;
        // it would be nice to assert here that this is either a string
        // or an array, but how do we know.
        //
        // it would also be nice to assert that the component size is > 0,
        // but it turns out that for array's of System.Void we cannot do
        // that b/c the component size is 0 (?)
        SetFlag(enum_flag_HasComponentSize);
        m_dwFlags = (m_dwFlags & ~0xFFFF) | wComponentSize;
    }

    inline WORD GetNumInstanceFields();

    inline WORD GetNumStaticFields();

    inline WORD GetNumThreadStaticFields();

    // Note that for value types GetBaseSize returns the size of instance fields for
    // a boxed value, and GetNumInstanceFieldsBytes for an unboxed value.
    // We place methods like these on MethodTable primarily so we can choose to cache
    // the information within MethodTable, and so less code manipulates EEClass
    // objects directly, because doing so can lead to bugs related to generics.
    //
    // <TODO> Use m_wBaseSize whenever this is identical to GetNumInstanceFieldBytes.
    // We would need to reserve a flag for this. </TODO>
    //
    inline DWORD GetNumInstanceFieldBytes();

    inline WORD GetNumIntroducedInstanceFields();

    // <TODO> Does this always return the same (or related) size as GetBaseSize()? </TODO>
    inline DWORD GetAlignedNumInstanceFieldBytes();


    // Note: This flag MUST be available even from an unrestored MethodTable - see GcScanRoots in siginfo.cpp.
    DWORD           ContainsPointers()  
    {
        LIMITED_METHOD_CONTRACT;
        return GetFlag(enum_flag_ContainsPointers);
    }
    BOOL            Collectible()
    {
        LIMITED_METHOD_CONTRACT;
#ifdef FEATURE_COLLECTIBLE_TYPES
        return GetFlag(enum_flag_Collectible);
#else
        return FALSE;
#endif
    }
    BOOL            ContainsPointersOrCollectible()
    {
        LIMITED_METHOD_CONTRACT;
        return GetFlag(enum_flag_ContainsPointers) || GetFlag(enum_flag_Collectible);
    }

    OBJECTHANDLE    GetLoaderAllocatorObjectHandle();
    NOINLINE BYTE *GetLoaderAllocatorObjectForGC();

    BOOL            IsNotTightlyPacked();
    
    void SetContainsPointers()
    {
        LIMITED_METHOD_CONTRACT;
        SetFlag(enum_flag_ContainsPointers);
    }

#ifdef FEATURE_64BIT_ALIGNMENT
    inline bool RequiresAlign8()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return !!GetFlag(enum_flag_RequiresAlign8);
    }

    inline void SetRequiresAlign8()
    {
        LIMITED_METHOD_CONTRACT;
        SetFlag(enum_flag_RequiresAlign8);
    }
#endif // FEATURE_64BIT_ALIGNMENT

    //-------------------------------------------------------------------
    // FIELD DESCRIPTORS
    //
    // Most of this API still lives on EEClass.  
    //
    // ************************************ WARNING *************
    // **   !!!!INSTANCE FIELDDESCS ARE REPRESENTATIVES!!!!!   **
    // ** THEY ARE SHARED BY COMPATIBLE GENERIC INSTANTIATIONS **
    // ************************************ WARNING *************

    // This goes straight to the EEClass 
    // Careful about using this method. If it's possible that fields may have been added via EnC, then
    // must use the FieldDescIterator as any fields added via EnC won't be in the raw list
    PTR_FieldDesc GetApproxFieldDescListRaw();

    // This returns a type-exact FieldDesc for a static field, but may still return a representative
    // for a non-static field.
    PTR_FieldDesc GetFieldDescByIndex(DWORD fieldIndex);

    DWORD GetIndexForFieldDesc(FieldDesc *pField);

    //-------------------------------------------------------------------
    // REMOTING and THUNKING.  
    //
    // We find a lot of information from the VTable.  But sometimes the VTable is a
    // thunking layer rather than the true type's VTable.  For instance, context
    // proxies use a single VTable for proxies to all the types we've loaded.
    // The following service adjusts a MethodTable based on the supplied instance.  As
    // we add new thunking layers, we just need to teach this service how to navigate
    // through them.
#ifdef FEATURE_REMOTING
    inline BOOL IsTransparentProxy()
    { 
        LIMITED_METHOD_DAC_CONTRACT;
        return GetFlag(enum_flag_Category_Mask) == enum_flag_Category_TransparentProxy;
    }
    inline void SetTransparentProxy()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(GetFlag(enum_flag_Category_Mask) == 0);
        SetFlag(enum_flag_Category_TransparentProxy);
    }

    inline BOOL IsMarshaledByRef()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return GetFlag(enum_flag_Category_MarshalByRef_Mask) == enum_flag_Category_MarshalByRef;
    }
    inline void SetMarshaledByRef()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(GetFlag(enum_flag_Category_Mask) == 0);
        SetFlag(enum_flag_Category_MarshalByRef);
    }

    inline BOOL IsContextful()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return GetFlag(enum_flag_Category_Mask) == enum_flag_Category_Contextful;
    }
    inline void SetIsContextful()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(GetFlag(enum_flag_Category_Mask) == 0);
        SetFlag(enum_flag_Category_Contextful);
    }
#else // FEATURE_REMOTING
    inline BOOL IsTransparentProxy()
    {
        return FALSE;
    }

    BOOL IsMarshaledByRef()
    {
        return FALSE;
    }

    BOOL IsContextful()
    {
        return FALSE;
    }
#endif // FEATURE_REMOTING
    
    inline bool RequiresFatDispatchTokens()
    {
        LIMITED_METHOD_CONTRACT;
        return !!GetFlag(enum_flag_RequiresDispatchTokenFat);
    }

    inline void SetRequiresFatDispatchTokens()
    {
        LIMITED_METHOD_CONTRACT;
        SetFlag(enum_flag_RequiresDispatchTokenFat);
    }

    inline bool HasPreciseInitCctors()
    {
        LIMITED_METHOD_CONTRACT;
        return !!GetFlag(enum_flag_HasPreciseInitCctors);
    }

    inline void SetHasPreciseInitCctors()
    {
        LIMITED_METHOD_CONTRACT;
        SetFlag(enum_flag_HasPreciseInitCctors);
    }

#if defined(FEATURE_HFA)
    inline bool IsHFA()
    {
        LIMITED_METHOD_CONTRACT;
        return !!GetFlag(enum_flag_IsHFA);
    }

    inline void SetIsHFA()
    {
        LIMITED_METHOD_CONTRACT;
        SetFlag(enum_flag_IsHFA);
    }
#endif // FEATURE_HFA

#if defined(FEATURE_UNIX_AMD64_STRUCT_PASSING_ITF)
    inline bool IsRegPassedStruct()
    {
        LIMITED_METHOD_CONTRACT;
        return !!GetFlag(enum_flag_IsRegStructPassed);
    }

    inline void SetRegPassedStruct()
    {
        LIMITED_METHOD_CONTRACT;
        SetFlag(enum_flag_IsRegStructPassed);
    }
#endif // defined(FEATURE_UNIX_AMD64_STRUCT_PASSING_ITF)

#ifdef FEATURE_HFA

    CorElementType GetHFAType();

    // The managed and unmanaged HFA type can differ for types with layout. The following two methods return the unmanaged HFA type.
    bool IsNativeHFA();
    CorElementType GetNativeHFAType();
#endif // FEATURE_HFA

#ifdef FEATURE_64BIT_ALIGNMENT
    // Returns true iff the native view of this type requires 64-bit aligment.
    bool NativeRequiresAlign8();
#endif // FEATURE_64BIT_ALIGNMENT

    // True if interface casts for an object having this type require more
    // than a simple scan of the interface map
    // See JIT_IsInstanceOfInterface
    inline BOOL InstanceRequiresNonTrivialInterfaceCast()
    {
        STATIC_CONTRACT_SO_TOLERANT;
        LIMITED_METHOD_CONTRACT;

        return GetFlag(enum_flag_NonTrivialInterfaceCast);
    }


    //-------------------------------------------------------------------
    // PARENT INTERFACES
    //
    unsigned GetNumInterfaces()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_wNumInterfaces;
    }

    //-------------------------------------------------------------------
    // CASTING
    // 
    // There are two variants of each of these methods:
    //
    // CanCastToX
    // - restore encoded pointers on demand
    // - might throw, might trigger GC
    // - return type is boolean (FALSE = cannot cast, TRUE = can cast)
    //
    // CanCastToXNoGC
    // - do not restore encoded pointers on demand
    // - does not throw, does not trigger GC
    // - return type is three-valued (CanCast, CannotCast, MaybeCast)
    // - MaybeCast indicates that the test tripped on an encoded pointer
    //   so the caller should now call CanCastToXRestoring if it cares
    // 
    BOOL CanCastToInterface(MethodTable *pTargetMT, TypeHandlePairList *pVisited = NULL);
    BOOL CanCastToClass(MethodTable *pTargetMT, TypeHandlePairList *pVisited = NULL);
    BOOL CanCastToClassOrInterface(MethodTable *pTargetMT, TypeHandlePairList *pVisited);
    BOOL CanCastByVarianceToInterfaceOrDelegate(MethodTable *pTargetMT, TypeHandlePairList *pVisited);

    BOOL CanCastToNonVariantInterface(MethodTable *pTargetMT);

    TypeHandle::CastResult CanCastToInterfaceNoGC(MethodTable *pTargetMT);
    TypeHandle::CastResult CanCastToClassNoGC(MethodTable *pTargetMT);
    TypeHandle::CastResult CanCastToClassOrInterfaceNoGC(MethodTable *pTargetMT);

    // The inline part of equivalence check.
#ifndef DACCESS_COMPILE
    FORCEINLINE BOOL IsEquivalentTo(MethodTable *pOtherMT COMMA_INDEBUG(TypeHandlePairList *pVisited = NULL));

#ifdef FEATURE_COMINTEROP
    // This method is public so that TypeHandle has direct access to it
    BOOL IsEquivalentTo_Worker(MethodTable *pOtherMT COMMA_INDEBUG(TypeHandlePairList *pVisited));      // out-of-line part, SO tolerant
private:
    BOOL IsEquivalentTo_WorkerInner(MethodTable *pOtherMT COMMA_INDEBUG(TypeHandlePairList *pVisited)); // out-of-line part, SO intolerant
#endif // FEATURE_COMINTEROP
#endif

public:
    //-------------------------------------------------------------------
    // THE METHOD TABLE PARENT (SUPERCLASS/BASE CLASS)
    //

    BOOL HasApproxParent()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return (GetWriteableData()->m_dwFlags & MethodTableWriteableData::enum_flag_HasApproxParent) != 0;
    }
    inline void SetHasExactParent()
    {
        WRAPPER_NO_CONTRACT;
        FastInterlockAnd(&(GetWriteableDataForWrite()->m_dwFlags), ~MethodTableWriteableData::enum_flag_HasApproxParent);
    }


    // Caller must know that the parent method table is not an encoded fixup
    inline PTR_MethodTable GetParentMethodTable()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        PRECONDITION(IsParentMethodTablePointerValid());

        TADDR pMT = m_pParentMethodTable;
#ifdef FEATURE_PREJIT
        if (GetFlag(enum_flag_HasIndirectParent))
            pMT = *PTR_TADDR(m_pParentMethodTable + offsetof(MethodTable, m_pParentMethodTable));
#endif
        return PTR_MethodTable(pMT);
    }

    inline static PTR_VOID GetParentMethodTableOrIndirection(PTR_VOID pMT)
    {
        WRAPPER_NO_CONTRACT;
        return PTR_VOID(*PTR_TADDR(dac_cast<TADDR>(pMT) + offsetof(MethodTable, m_pParentMethodTable)));
    }

    inline MethodTable ** GetParentMethodTablePtr()
    {
        WRAPPER_NO_CONTRACT;

#ifdef FEATURE_PREJIT
        return GetFlag(enum_flag_HasIndirectParent) ? 
            (MethodTable **)(m_pParentMethodTable + offsetof(MethodTable, m_pParentMethodTable)) :(MethodTable **)&m_pParentMethodTable;
#else
        return (MethodTable **)&m_pParentMethodTable;
#endif
    }

    // Is the parent method table pointer equal to the given argument?
    BOOL ParentEquals(PTR_MethodTable pMT)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        PRECONDITION(IsParentMethodTablePointerValid());
        g_IBCLogger.LogMethodTableAccess(this);
        return GetParentMethodTable() == pMT;
    }

#ifdef _DEBUG
    BOOL IsParentMethodTablePointerValid();
#endif

#ifndef DACCESS_COMPILE
    void SetParentMethodTable (MethodTable *pParentMethodTable)
    {
        LIMITED_METHOD_CONTRACT;
        PRECONDITION(!GetFlag(enum_flag_HasIndirectParent));
        m_pParentMethodTable = (TADDR)pParentMethodTable;
#ifdef _DEBUG
        GetWriteableDataForWrite_NoLogging()->SetParentMethodTablePointerValid();
#endif
    }
#endif // !DACCESS_COMPILE
    MethodTable * GetMethodTableMatchingParentClass(MethodTable * pWhichParent);
    Instantiation GetInstantiationOfParentClass(MethodTable *pWhichParent);

    //-------------------------------------------------------------------
    // THE  EEClass (Possibly shared between instantiations!).
    //
    // Note that it is not generally the case that GetClass.GetMethodTable() == t.

    PTR_EEClass GetClass();

    inline PTR_EEClass GetClass_NoLogging();

    PTR_EEClass GetClassWithPossibleAV();

    BOOL ValidateWithPossibleAV();

    BOOL IsClassPointerValid();

    static UINT32 GetOffsetOfFlags()
    {
        LIMITED_METHOD_CONTRACT;
        return offsetof(MethodTable, m_dwFlags);
    } 

    static UINT32 GetIfArrayThenSzArrayFlag()
    {
        LIMITED_METHOD_CONTRACT;
        return enum_flag_Category_IfArrayThenSzArray;
    } 

    //-------------------------------------------------------------------
    // CONSTRUCTION
    //
    // Do not call the following at any time except when creating a method table.
    // One day we will have proper constructors for method tables and all these
    // will disappear.
#ifndef DACCESS_COMPILE    
    inline void SetClass(EEClass *pClass)
    {
        LIMITED_METHOD_CONTRACT;
        m_pEEClass = pClass;
    }

    inline void SetCanonicalMethodTable(MethodTable * pMT)
    {
        m_pCanonMT = (TADDR)pMT | MethodTable::UNION_METHODTABLE;
    }
#endif

    inline void SetHasInstantiation(BOOL fTypicalInstantiation, BOOL fSharedByGenericInstantiations);

    //-------------------------------------------------------------------
    // INTERFACE IMPLEMENTATION
    //
 public:
    // Faster force-inlined version of ImplementsInterface
    BOOL ImplementsInterfaceInline(MethodTable *pInterface);

    BOOL ImplementsInterface(MethodTable *pInterface);
    BOOL ImplementsEquivalentInterface(MethodTable *pInterface);

    MethodDesc *GetMethodDescForInterfaceMethod(TypeHandle ownerType, MethodDesc *pInterfaceMD);
    MethodDesc *GetMethodDescForInterfaceMethod(MethodDesc *pInterfaceMD); // You can only use this one for non-generic interfaces
    
    //-------------------------------------------------------------------
    // INTERFACE MAP.  
    //

    inline PTR_InterfaceInfo GetInterfaceMap();

#ifdef BINDER
    void SetNumInterfaces(DWORD dwNumInterfaces)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        m_wNumInterfaces = (WORD)dwNumInterfaces;
        _ASSERTE(m_wNumInterfaces == dwNumInterfaces);
    }
#endif

#ifndef DACCESS_COMPILE
    void SetInterfaceMap(WORD wNumInterfaces, InterfaceInfo_t* iMap);
#endif

    inline int HasInterfaceMap()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return (m_wNumInterfaces != 0);
    }

    // Where possible, use this iterator over the interface map instead of accessing the map directly
    // That way we can easily change the implementation of the map
    class InterfaceMapIterator
    {
        friend class MethodTable;

    private:
        PTR_InterfaceInfo m_pMap;
        DWORD m_i;
        DWORD m_count;

        InterfaceMapIterator(MethodTable *pMT)
          : m_pMap(pMT->GetInterfaceMap()),
            m_i((DWORD) -1),
            m_count(pMT->GetNumInterfaces())
        {
            WRAPPER_NO_CONTRACT;
        }

        InterfaceMapIterator(MethodTable *pMT, DWORD index)
          : m_pMap(pMT->GetInterfaceMap() + index),
            m_i(index),
            m_count(pMT->GetNumInterfaces())
        {
            WRAPPER_NO_CONTRACT;
            CONSISTENCY_CHECK(index >= 0 && index < m_count);
        }

    public:
        InterfaceInfo_t* GetInterfaceInfo()
        {
            LIMITED_METHOD_CONTRACT;
            return m_pMap;
        }

        // Move to the next item in the map, returning TRUE if an item
        // exists or FALSE if we've run off the end
        inline BOOL Next()
        {
            LIMITED_METHOD_CONTRACT;
            PRECONDITION(!Finished());
            if (m_i != (DWORD) -1)
                m_pMap++;
            return (++m_i < m_count);
        }

        // Have we iterated over all of the items?
        BOOL Finished()
        {
            return (m_i == m_count);
        }

        // Get the interface at the current position
        inline PTR_MethodTable GetInterface()
        {
            CONTRACT(PTR_MethodTable)
            {
                GC_NOTRIGGER;
                NOTHROW;
                SUPPORTS_DAC;
                PRECONDITION(m_i != (DWORD) -1 && m_i < m_count);
                POSTCONDITION(CheckPointer(RETVAL));
            }
            CONTRACT_END;

            RETURN (m_pMap->GetMethodTable());
        }

#ifndef DACCESS_COMPILE
        void SetInterface(MethodTable *pMT)
        {
            WRAPPER_NO_CONTRACT;
            m_pMap->SetMethodTable(pMT);
        }
#endif

        DWORD GetIndex()
        {
            LIMITED_METHOD_CONTRACT;
            return m_i;            
        }
    };  // class InterfaceMapIterator

    // Create a new iterator over the interface map
    // The iterator starts just before the first item in the map
    InterfaceMapIterator IterateInterfaceMap()
    {
        WRAPPER_NO_CONTRACT;
        return InterfaceMapIterator(this);
    }

    // Create a new iterator over the interface map, starting at the index specified
    InterfaceMapIterator IterateInterfaceMapFrom(DWORD index)
    {
        WRAPPER_NO_CONTRACT;
        return InterfaceMapIterator(this, index);
    }

    //-------------------------------------------------------------------
    // ADDITIONAL INTERFACE MAP DATA
    //

    // We store extra info (flag bits) for interfaces implemented on this MethodTable in a separate optional
    // location for better data density (if we put them in the interface map directly data alignment could
    // have us using 32 or even 64 bits to represent a single boolean value). Currently the only flag we
    // persist is IsDeclaredOnClass (was the interface explicitly declared by this class).

    // Currently we always store extra info whenever we have an interface map (in the future you could imagine
    // this being limited to those scenarios in which at least one of the interfaces has a non-default value
    // for a flag).
    inline BOOL HasExtraInterfaceInfo()
    {
        SUPPORTS_DAC;
        return HasInterfaceMap();
    }

    // Count of interfaces that can have their extra info stored inline in the optional data structure itself
    // (once the interface count exceeds this limit the optional data slot will instead point to a buffer with
    // the information).
    enum { kInlinedInterfaceInfoThreshhold = sizeof(TADDR) * 8 };

    // Calculate how many bytes of storage will be required to track additional information for interfaces.
    // This will be zero if there are no interfaces, but can also be zero for small numbers of interfaces as
    // well, and callers should be ready to handle this.
    static SIZE_T GetExtraInterfaceInfoSize(DWORD cInterfaces);

    // Called after GetExtraInterfaceInfoSize above to setup a new MethodTable with the additional memory to
    // track extra interface info. If there are a non-zero number of interfaces implemented on this class but
    // GetExtraInterfaceInfoSize() returned zero, this call must still be made (with a NULL argument).
    void InitializeExtraInterfaceInfo(PVOID pInfo);

#ifdef FEATURE_PREJIT
    // Ngen support.
    void SaveExtraInterfaceInfo(DataImage *pImage);
    void FixupExtraInterfaceInfo(DataImage *pImage);
#endif // FEATURE_PREJIT

#ifdef DACCESS_COMPILE
    void EnumMemoryRegionsForExtraInterfaceInfo();
#endif // DACCESS_COMPILE

    // For the given interface in the map (specified via map index) mark the interface as declared explicitly
    // on this class. This is not legal for dynamically added interfaces (as used by RCWs).
    void SetInterfaceDeclaredOnClass(DWORD index);

    // For the given interface in the map (specified via map index) return true if the interface was declared
    // explicitly on this class.
    bool IsInterfaceDeclaredOnClass(DWORD index);

    //-------------------------------------------------------------------
    // VIRTUAL/INTERFACE CALL RESOLUTION
    //
    // These should probably go in method.hpp since they don't have 
    // much to do with method tables per se.
    //

    // get the method desc given the interface method desc
    static MethodDesc *GetMethodDescForInterfaceMethodAndServer(TypeHandle ownerType, MethodDesc *pItfMD, OBJECTREF *pServer);

#ifdef FEATURE_COMINTEROP
    // get the method desc given the interface method desc on a COM implemented server (if fNullOk is set then NULL is an allowable return value)
    MethodDesc *GetMethodDescForComInterfaceMethod(MethodDesc *pItfMD, bool fNullOk);
#endif // FEATURE_COMINTEROP


    // Try a partial resolve of the constraint call, up to generic code sharing.  
    //
    // Note that this will not necessarily resolve the call exactly, since we might be compiling
    // shared generic code - it may just resolve it to a candidate suitable for
    // JIT compilation, and require a runtime lookup for the actual code pointer 
    // to call.
    //
    // Return NULL if the call could not be resolved, e.g. because it is invoked
    // on a type that inherits the implementation of the method from System.Object
    // or System.ValueType.
    //
    // Always returns an unboxed entry point with a uniform calling convention.
    MethodDesc * TryResolveConstraintMethodApprox(
        TypeHandle   ownerType, 
        MethodDesc * pMD, 
        BOOL *       pfForceUseRuntimeLookup = NULL);

    //-------------------------------------------------------------------
    // CONTRACT IMPLEMENTATIONS
    //

    inline BOOL HasDispatchMap()
    {
        WRAPPER_NO_CONTRACT;
        return GetDispatchMap() != NULL;
    }

    PTR_DispatchMap GetDispatchMap();

    inline BOOL HasDispatchMapSlot()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return GetFlag(enum_flag_HasDispatchMapSlot);
    }

#ifndef DACCESS_COMPILE
    void SetDispatchMap(DispatchMap *pDispatchMap)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(HasDispatchMapSlot());

        TADDR pSlot = GetMultipurposeSlotPtr(enum_flag_HasDispatchMapSlot, c_DispatchMapSlotOffsets);
        RelativePointer<PTR_DispatchMap>::SetValueAtPtr(pSlot, pDispatchMap);
    }
#endif // !DACCESS_COMPILE

protected:
    BOOL FindEncodedMapDispatchEntry(UINT32 typeID,
                                     UINT32 slotNumber,
                                     DispatchMapEntry *pEntry);

    BOOL FindIntroducedImplementationTableDispatchEntry(UINT32 slotNumber,
                                                        DispatchMapEntry *pEntry,
                                                        BOOL fVirtualMethodsOnly);

    BOOL FindDispatchEntryForCurrentType(UINT32 typeID,
                                         UINT32 slotNumber,
                                         DispatchMapEntry *pEntry);

    BOOL FindDispatchEntry(UINT32 typeID,
                           UINT32 slotNumber,
                           DispatchMapEntry *pEntry);

public:
    BOOL FindDispatchImpl(
        UINT32         typeID, 
        UINT32         slotNumber, 
        DispatchSlot * pImplSlot);

    DispatchSlot FindDispatchSlot(UINT32 typeID, UINT32 slotNumber);

    DispatchSlot FindDispatchSlot(DispatchToken tok);

    // You must use the second of these two if there is any chance the pMD is a method
    // on a generic interface such as IComparable<T> (which it normally can be).  The 
    // ownerType is used to provide an exact qualification in the case the pMD is
    // a shared method descriptor.
    DispatchSlot FindDispatchSlotForInterfaceMD(MethodDesc *pMD);
    DispatchSlot FindDispatchSlotForInterfaceMD(TypeHandle ownerType, MethodDesc *pMD);

    MethodDesc *ReverseInterfaceMDLookup(UINT32 slotNumber);

    // Lookup, does not assign if not already done.
    UINT32 LookupTypeID();
    // Lookup, will assign ID if not already done.
    UINT32 GetTypeID();


    MethodTable *LookupDispatchMapType(DispatchMapTypeID typeID);

    MethodDesc *GetIntroducingMethodDesc(DWORD slotNumber);

    // Determines whether all methods in the given interface have their final implementing
    // slot in a parent class. I.e. if this returns TRUE, it is trivial (no VSD lookup) to
    // dispatch pItfMT methods on this class if one knows how to dispatch them on pParentMT.
    BOOL ImplementsInterfaceWithSameSlotsAsParent(MethodTable *pItfMT, MethodTable *pParentMT);

    // Determines whether all methods in the given interface have their final implementation
    // in a parent class. I.e. if this returns TRUE, this class behaves the same as pParentMT
    // when it comes to dispatching pItfMT methods.
    BOOL HasSameInterfaceImplementationAsParent(MethodTable *pItfMT, MethodTable *pParentMT);

public:
    static MethodDesc *MapMethodDeclToMethodImpl(MethodDesc *pMDDecl);

    //-------------------------------------------------------------------
    // FINALIZATION SEMANTICS
    //

    DWORD  CannotUseSuperFastHelper()
    {
        WRAPPER_NO_CONTRACT;
        return HasFinalizer();
    }

    void SetHasFinalizer()
    {
        LIMITED_METHOD_CONTRACT;
        SetFlag(enum_flag_HasFinalizer);
    }

    void SetHasCriticalFinalizer()
    {
        LIMITED_METHOD_CONTRACT;
        SetFlag(enum_flag_HasCriticalFinalizer);
    }
    // Does this class have non-trivial finalization requirements?
    DWORD HasFinalizer()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return GetFlag(enum_flag_HasFinalizer);
    }
    // Must this class be finalized during a rude appdomain unload, and
    // must it's finalizer run in a different order from normal finalizers?
    DWORD HasCriticalFinalizer() const
    {
        LIMITED_METHOD_CONTRACT;
        return GetFlag(enum_flag_HasCriticalFinalizer);
    }

    // Have the backout methods (Finalizer, Dispose, ReleaseHandle etc.) been prepared for this type? This currently only happens
    // for types derived from CriticalFinalizerObject.
    BOOL CriticalTypeHasBeenPrepared()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(HasCriticalFinalizer());
        return GetWriteableData()->CriticalTypeHasBeenPrepared();
    }

    void SetCriticalTypeHasBeenPrepared()
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;

        _ASSERTE(HasCriticalFinalizer());
        GetWriteableDataForWrite()->SetCriticalTypeHasBeenPrepared();
    }

    //-------------------------------------------------------------------
    // STATIC FIELDS
    //

    DWORD  GetOffsetOfFirstStaticHandle();
    DWORD  GetOffsetOfFirstStaticMT();

#ifndef DACCESS_COMPILE
    inline PTR_BYTE GetNonGCStaticsBasePointer();
    inline PTR_BYTE GetGCStaticsBasePointer();
    inline PTR_BYTE GetNonGCThreadStaticsBasePointer();
    inline PTR_BYTE GetGCThreadStaticsBasePointer();
#endif //!DACCESS_COMPILE

    inline PTR_BYTE GetNonGCThreadStaticsBasePointer(PTR_Thread pThread, PTR_AppDomain pDomain);
    inline PTR_BYTE GetGCThreadStaticsBasePointer(PTR_Thread pThread, PTR_AppDomain pDomain);

    inline DWORD IsDynamicStatics()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return !TestFlagWithMask(enum_flag_StaticsMask, enum_flag_StaticsMask_NonDynamic);
    }

    inline void SetDynamicStatics(BOOL fGeneric)
    {
        LIMITED_METHOD_CONTRACT;
        SetFlag(fGeneric ? enum_flag_StaticsMask_Generics : enum_flag_StaticsMask_Dynamic);
    }

    inline void SetHasBoxedRegularStatics()
    {
        LIMITED_METHOD_CONTRACT;
        SetFlag(enum_flag_HasBoxedRegularStatics);
    }

    inline DWORD HasBoxedRegularStatics()
    {
        LIMITED_METHOD_CONTRACT;
        return GetFlag(enum_flag_HasBoxedRegularStatics);
    }

    DWORD HasFixedAddressVTStatics();

    //-------------------------------------------------------------------
    // PER-INSTANTIATION STATICS INFO
    //


    void SetupGenericsStaticsInfo(FieldDesc* pStaticFieldDescs);

    BOOL HasGenericsStaticsInfo()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return GetFlag(enum_flag_StaticsMask_Generics);
    }

    PTR_FieldDesc GetGenericsStaticFieldDescs()
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(HasGenericsStaticsInfo());
        return GetGenericsStaticsInfo()->m_pFieldDescs;
    }

    BOOL HasCrossModuleGenericStaticsInfo()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return TestFlagWithMask(enum_flag_StaticsMask, enum_flag_StaticsMask_CrossModuleGenerics);
    }

    PTR_Module GetGenericsStaticsModuleAndID(DWORD * pID);

    WORD GetNumHandleRegularStatics();

    WORD GetNumBoxedRegularStatics ();
    WORD GetNumBoxedThreadStatics ();

    //-------------------------------------------------------------------
    // DYNAMIC ID
    //

    // Used for generics and reflection emit in memory
    DWORD GetModuleDynamicEntryID();
#ifndef BINDER
    Module* GetModuleForStatics();
#else // BINDER
    MdilModule* GetModuleForStatics();
#endif
    //-------------------------------------------------------------------
    // GENERICS DICT INFO
    //

    // Number of generic arguments, whether this is a method table for 
    // a generic type instantiation, e.g. List<string> or the "generic" MethodTable
    // e.g. for List.
#ifdef BINDER
    DWORD GetNumGenericArgs();
#else
    inline DWORD GetNumGenericArgs()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        if (HasInstantiation())
            return (DWORD) (GetGenericsDictInfo()->m_wNumTyPars);
        else
            return 0;
    }
#endif

    inline DWORD GetNumDicts()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        if (HasPerInstInfo())
        {
            PTR_GenericsDictInfo  pDictInfo = GetGenericsDictInfo();
            return (DWORD) (pDictInfo->m_wNumDicts);
        }
        else
            return 0;
    }

    //-------------------------------------------------------------------
    // OBJECTS 
    //

    OBJECTREF Allocate();
    
    // This flavor of Allocate is more efficient, but can only be used
    // if IsRestored(), CheckInstanceActivated(), IsClassInited() are known to be true.
    // A sufficient condition is that another instance of the exact same type already
    // exists in the same appdomain. It's currently called only from Delegate.Combine
    // via COMDelegate::InternalAllocLike.
    OBJECTREF AllocateNoChecks();

    OBJECTREF Box(void* data);
    OBJECTREF FastBox(void** data);
#ifndef DACCESS_COMPILE
    BOOL UnBoxInto(void *dest, OBJECTREF src);
    BOOL UnBoxIntoArg(ArgDestination *argDest, OBJECTREF src);
    void UnBoxIntoUnchecked(void *dest, OBJECTREF src);
#endif

#ifdef _DEBUG
    // Used for debugging class layout. Dumps to the debug console
    // when debug is true.
    void DebugDumpVtable(LPCUTF8 szClassName, BOOL fDebug);
    void Debug_DumpInterfaceMap(LPCSTR szInterfaceMapPrefix);
    void Debug_DumpDispatchMap();
    void DebugDumpFieldLayout(LPCUTF8 pszClassName, BOOL debug);
    void DebugRecursivelyDumpInstanceFields(LPCUTF8 pszClassName, BOOL debug);
    void DebugDumpGCDesc(LPCUTF8 pszClassName, BOOL debug);
#endif //_DEBUG
    
    inline BOOL IsAgileAndFinalizable()
    {
        LIMITED_METHOD_CONTRACT;
        // Right now, System.Thread is the only cases of this. 
        // Things should stay this way - please don't change without talking to EE team.
        return this == g_pThreadClass;
    }


    //-------------------------------------------------------------------
    // ENUMS, DELEGATES, VALUE TYPES, ARRAYS
    //
    // #KindsOfElementTypes
    // GetInternalCorElementType() retrieves the internal representation of the type. It's not always
    // appropiate to use this. For example, we treat enums as their underlying type or some structs are
    // optimized to be ints. To get the signature type or the verifier type (same as signature except for
    // enums are normalized to the primtive type that underlies them), use the APIs in Typehandle.h
    //      
    //   * code:TypeHandle.GetSignatureCorElementType()
    //   * code:TypeHandle.GetVerifierCorElementType()
    //   * code:TypeHandle.GetInternalCorElementType()
    CorElementType GetInternalCorElementType();
    void SetInternalCorElementType(CorElementType _NormType);

    // See code:TypeHandle::GetVerifierCorElementType for description
    CorElementType GetVerifierCorElementType();

    // See code:TypeHandle::GetSignatureCorElementType for description
    CorElementType GetSignatureCorElementType();

    // A true primitive is one who's GetVerifierCorElementType() == 
    //      ELEMENT_TYPE_I, 
    //      ELEMENT_TYPE_I4, 
    //      ELEMENT_TYPE_TYPEDREF etc.
    // Note that GetIntenalCorElementType might return these same values for some additional
    // types such as Enums and some structs.
    BOOL IsTruePrimitive();
    void SetIsTruePrimitive();

    // Is this delegate? Returns false for System.Delegate and System.MulticastDelegate.
    inline BOOL IsDelegate()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        // We do not allow single cast delegates anymore, just check for multicast delegate
        _ASSERTE(g_pMulticastDelegateClass);
        return ParentEquals(g_pMulticastDelegateClass);
    }

    // Is this System.Object?
    inline BOOL IsObjectClass()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(g_pObjectClass);
        return (this == g_pObjectClass);
    }
#ifndef BINDER
    // Is this System.ValueType?
    inline DWORD IsValueTypeClass()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(g_pValueTypeClass);
        return (this == g_pValueTypeClass);
    }
#else // BINDER
    // Is this System.ValueType?
    bool IsValueTypeClass();

    // Is this System.Enum?
    bool IsEnumClass();
#endif // BINDER

    // Is this value type? Returns false for System.ValueType and System.Enum.
    inline BOOL IsValueType();

    // Returns "TRUE" iff "this" is a struct type such that return buffers used for returning a value
    // of this type must be stack-allocated.  This will generally be true only if the struct 
    // contains GC pointers, and does not exceed some size limit.  Maintaining this as an invariant allows
    // an optimization: the JIT may assume that return buffer pointers for return types for which this predicate
    // returns TRUE are always stack allocated, and thus, that stores to the GC-pointer fields of such return
    // buffers do not require GC write barriers.
    BOOL IsStructRequiringStackAllocRetBuf();

    // Is this enum? Returns false for System.Enum.
    inline BOOL IsEnum();

    // Is this array? Returns false for System.Array.
    inline BOOL IsArray()           
    { 
        LIMITED_METHOD_DAC_CONTRACT;
        return GetFlag(enum_flag_Category_Array_Mask) == enum_flag_Category_Array; 
    }
    inline BOOL IsMultiDimArray()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        PRECONDITION(IsArray());
        return !GetFlag(enum_flag_Category_IfArrayThenSzArray);
    }

    // Returns true if this type is Nullable<T> for some T.
    inline BOOL IsNullable()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return GetFlag(enum_flag_Category_Mask) == enum_flag_Category_Nullable;
    }

    inline void SetIsNullable()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(GetFlag(enum_flag_Category_Mask) == enum_flag_Category_ValueType);
        SetFlag(enum_flag_Category_Nullable);
    }
    
    inline BOOL IsStructMarshalable() 
    {
        LIMITED_METHOD_CONTRACT;
        PRECONDITION(!IsInterface());
        return GetFlag(enum_flag_IfNotInterfaceThenMarshalable); 
    }

    inline void SetStructMarshalable()
    {
        LIMITED_METHOD_CONTRACT;
        PRECONDITION(!IsInterface());
        SetFlag(enum_flag_IfNotInterfaceThenMarshalable);
    }
    
    // The following methods are only valid for the 
    // method tables for array types.  These MTs may 
    // be shared between array types and thus GetArrayElementTypeHandle
    // may only be approximate.  If you need the exact element type handle then
    // you should probably be calling GetArrayElementTypeHandle on a TypeHandle,
    // or an ArrayTypeDesc, or on an object reference that is known to be an array,
    // e.g. a BASEARRAYREF.
    //
    // At the moment only the object[] MethodTable is shared between array types.
    // In the future the amount of sharing of method tables is likely to be increased.
    CorElementType GetArrayElementType();
    DWORD GetRank(); 

    TypeHandle GetApproxArrayElementTypeHandle()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        _ASSERTE (IsArray());
        return TypeHandle::FromTAddr(m_ElementTypeHnd);
    }

    void SetApproxArrayElementTypeHandle(TypeHandle th)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        m_ElementTypeHnd = th.AsTAddr();
    }

    TypeHandle * GetApproxArrayElementTypeHandlePtr()
    {
        LIMITED_METHOD_CONTRACT;
        return (TypeHandle *)&m_ElementTypeHnd;
    }

    static inline DWORD GetOffsetOfArrayElementTypeHandle()
    {
        LIMITED_METHOD_CONTRACT;
        return offsetof(MethodTable, m_ElementTypeHnd);
    }

    //-------------------------------------------------------------------
    // UNDERLYING METADATA
    //


    // Get the RID/token for the metadata for the corresponding type declaration
    unsigned GetTypeDefRid();
    unsigned GetTypeDefRid_NoLogging();

    inline mdTypeDef GetCl()
    {
        LIMITED_METHOD_CONTRACT;
        return TokenFromRid(GetTypeDefRid(), mdtTypeDef);
    }

    inline mdTypeDef GetCl_NoLogging()
    {
        LIMITED_METHOD_CONTRACT;
        return TokenFromRid(GetTypeDefRid_NoLogging(), mdtTypeDef);
    }

    void SetCl(mdTypeDef token);

#ifdef _DEBUG
// Make this smaller in debug builds to exercise the overflow codepath
#define METHODTABLE_TOKEN_OVERFLOW 0xFFF
#else
#define METHODTABLE_TOKEN_OVERFLOW 0xFFFF
#endif

    BOOL HasTokenOverflow()
    {
        LIMITED_METHOD_CONTRACT;
        return m_wToken == METHODTABLE_TOKEN_OVERFLOW;
    }

    // Get the MD Import for the metadata for the corresponding type declaration
    IMDInternalImport* GetMDImport();
    
    mdTypeDef GetEnclosingCl();

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif

    //-------------------------------------------------------------------
    // REMOTEABLE METHOD INFO
    //
#ifdef FEATURE_REMOTING
    BOOL    HasRemotableMethodInfo();

    PTR_CrossDomainOptimizationInfo GetRemotableMethodInfo()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            SO_TOLERANT;
            MODE_ANY;
        }
        CONTRACTL_END;
        _ASSERTE(HasRemotableMethodInfo());
        return *GetRemotableMethodInfoPtr();
    }
    void SetupRemotableMethodInfo(AllocMemTracker *pamTracker);

    //-------------------------------------------------------------------
    // REMOTING VTS INFO
    //
    // This optional addition to MethodTables allows us to locate VTS (Version
    // Tolerant Serialization) event callback methods and optionally
    // serializable fields quickly. We also store the NotSerialized field
    // information in here so remoting can avoid one more touch of the metadata
    // during cross appdomain cloning.
    //

    void SetHasRemotingVtsInfo();
    BOOL HasRemotingVtsInfo();
    PTR_RemotingVtsInfo GetRemotingVtsInfo();
    PTR_RemotingVtsInfo AllocateRemotingVtsInfo( AllocMemTracker *pamTracker, DWORD dwNumFields);
#endif // FEATURE_REMOTING

#ifdef FEATURE_COMINTEROP
    void SetHasGuidInfo();
    BOOL HasGuidInfo();
    void SetHasCCWTemplate();
    BOOL HasCCWTemplate();
    void SetHasRCWPerTypeData();
    BOOL HasRCWPerTypeData();
#endif // FEATURE_COMINTEROP

    // The following two methods produce correct results only if this type is
    // marked Serializable (verified by assert in checked builds) and the field
    // in question was introduced in this type (the index is the FieldDesc
    // index).
    BOOL IsFieldNotSerialized(DWORD dwFieldIndex);
    BOOL IsFieldOptionallySerialized(DWORD dwFieldIndex);

    //-------------------------------------------------------------------
    // DICTIONARIES FOR GENERIC INSTANTIATIONS
    //
    // The PerInstInfo pointer is a pointer to per-instantiation pointer table, 
    // each entry of which points to an instantiation "dictionary"
    // for an instantiated type; the last pointer points to a
    // dictionary which is specific to this method table, previous 
    // entries point to dictionaries in superclasses. Instantiated interfaces and structs 
    // have just single dictionary (no inheritance).
    //
    // GetNumDicts() gives the number of dictionaries.
    //
    //@nice GENERICS: instead of a separate table of pointers, put the pointers 
    // in the vtable itself. Advantages:
    // * Time: we save an indirection as we don't need to go through PerInstInfo first.
    // * Space: no need for PerInstInfo (1 word)
    // Problem is that lots of code assumes that the vtable is filled 
    // uniformly with pointers to MethodDesc stubs.
    //
    // The dictionary for the method table is just an array of handles for 
    // type parameters in the following cases:
    // * instantiated interfaces (no code)
    // * instantiated types whose code is not shared
    // Otherwise, it starts with the type parameters and then has a fixed 
    // number of slots for handles (types & methods)
    // that are filled in lazily at run-time. Finally there is a "spill-bucket" 
    // pointer used when the dictionary gets filled.
    // In summary:
    //    typar_1              type handle for first type parameter
    //    ...
    //    typar_n              type handle for last type parameter
    //    slot_1               slot for first run-time handle (initially null)
    //    ...
    //    slot_m               slot for last run-time handle (initially null)
    //    next_bucket          pointer to spill bucket (possibly null)
    // The spill bucket contains just run-time handle slots.
    //   (Alternative: continue chaining buckets. 
    //    Advantage: no need to deallocate when growing dictionaries. 
    //    Disadvantage: more indirections required at run-time.)
    //
    // The layout of dictionaries is determined by GetClass()->GetDictionaryLayout()
    // Thus the layout can vary between incompatible instantiations. This is sometimes useful because individual type
    // parameters may or may not be shared. For example, consider a two parameter class Dict<K,D>. In instantiations shared with 
    // Dict<double,string> any reference to K is known at JIT-compile-time (it's double) but any token containing D 
    // must have a dictionary entry. On the other hand, for instantiations shared with Dict<string,double> the opposite holds.
    //

    // Return a pointer to the per-instantiation information. See field itself for comments.
    DPTR(PTR_Dictionary) GetPerInstInfo()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        _ASSERTE(HasPerInstInfo());
        return dac_cast<DPTR(PTR_Dictionary)>(m_pMultipurposeSlot1);
    }
    BOOL HasPerInstInfo()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return GetFlag(enum_flag_HasPerInstInfo) && !IsArray();
    }
#ifndef DACCESS_COMPILE
    static inline DWORD GetOffsetOfPerInstInfo()
    {
        LIMITED_METHOD_CONTRACT;
        return offsetof(MethodTable, m_pPerInstInfo);
    }
    void SetPerInstInfo(Dictionary** pPerInstInfo)
    {
        LIMITED_METHOD_CONTRACT;
        m_pPerInstInfo = pPerInstInfo;
    }
    void SetDictInfo(WORD numDicts, WORD numTyPars)
    {
        WRAPPER_NO_CONTRACT;
        GenericsDictInfo* pInfo = GetGenericsDictInfo();
        pInfo->m_wNumDicts  = numDicts;
        pInfo->m_wNumTyPars = numTyPars;
    }
#endif // !DACCESS_COMPILE
    PTR_GenericsDictInfo GetGenericsDictInfo()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        // GenericsDictInfo is stored at negative offset of the dictionary
        return dac_cast<PTR_GenericsDictInfo>(GetPerInstInfo()) - 1;
    }

    // Get a pointer to the dictionary for this instantiated type
    // (The instantiation is stored in the initial slots of the dictionary)
    // If not instantiated, return NULL
    Dictionary* GetDictionary();

#ifdef FEATURE_PREJIT
    // 
    // After the zapper compiles all code in a module it may attempt 
    // to populate entries in all dictionaries
    // associated with generic types.  This is an optional step - nothing will
    // go wrong at runtime except we may get more one-off calls to JIT_GenericHandle.
    // Although these are one-off we prefer to avoid them since they touch metadata
    // pages.
    //
    // Fully populating a dictionary may in theory load more types. However 
    // for the moment only those entries that refer to types that
    // are already loaded will be filled in.
    void PrepopulateDictionary(DataImage * image, BOOL nonExpansive);
#endif // FEATURE_PREJIT

    // Return a substitution suitbale for interpreting
    // the metadata in parent class, assuming we already have a subst.
    // suitable for interpreting the current class.
    //
    // If, for example, the definition for the current class is
    //   D<T> : C<List<T>, T[] > 
    // then this (for C<!0,!1>) will be 
    //   0 --> List<T>
    //   1 --> T[]
    // added to the chain of substitutions.
    // 
    // Subsequently, if the definition for C is
    //   C<T, U> : B< Dictionary<T, U> >
    // then the next subst (for B<!0>) will be
    //   0 --> Dictionary< List<T>, T[] >

    Substitution GetSubstitutionForParent(const Substitution *pSubst); 

    inline DWORD GetAttrClass();

    inline BOOL IsSerializable();
#ifdef FEATURE_REMOTING
    inline BOOL CannotBeBlittedByObjectCloner();
#endif
    inline BOOL HasFieldsWhichMustBeInited();
    inline BOOL SupportsAutoNGen();
    inline BOOL RunCCTorAsIfNGenImageExists();

    //-------------------------------------------------------------------
    // SECURITY SEMANTICS 
    //


    BOOL IsNoSecurityProperties()
    {
        LIMITED_METHOD_CONTRACT;
        return GetFlag(enum_flag_NoSecurityProperties);
    }

    void SetNoSecurityProperties()
    {
        LIMITED_METHOD_CONTRACT;
        SetFlag(enum_flag_NoSecurityProperties);
    }

    void SetIsAsyncPinType()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(GetFlag(enum_flag_Category_Mask) == 0);
        SetFlag(enum_flag_Category_AsyncPin);
    }

    BOOL IsAsyncPinType()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return GetFlag(enum_flag_Category_Mask) == enum_flag_Category_AsyncPin;
    }

    inline BOOL IsPreRestored() const
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return GetFlag(enum_flag_IsPreRestored);
    }

    //-------------------------------------------------------------------
    // THE EXPOSED CLASS OBJECT
    //
    /*
     * m_ExposedClassObject is a RuntimeType instance for this class.  But
     * do NOT use it for Arrays or remoted objects!  All arrays of objects 
     * share the same MethodTable/EEClass.
     * @GENERICS: this is per-instantiation data
     */
    // There are two version of GetManagedClassObject.  The GetManagedClassObject()
    //  method will get the class object.  If it doesn't exist it will be created.
    //  GetManagedClassObjectIfExists() will return null if the Type object doesn't exist.
    OBJECTREF GetManagedClassObject();
    OBJECTREF GetManagedClassObjectIfExists();

   
    // ------------------------------------------------------------------
    // Private part of MethodTable
    // ------------------------------------------------------------------

    inline void SetWriteableData(PTR_MethodTableWriteableData pMTWriteableData)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(pMTWriteableData);
        m_pWriteableData = pMTWriteableData;
    }
    
    inline PTR_Const_MethodTableWriteableData GetWriteableData() const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        g_IBCLogger.LogMethodTableWriteableDataAccess(this);
        return m_pWriteableData;
    }

    inline PTR_Const_MethodTableWriteableData GetWriteableData_NoLogging() const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_pWriteableData;
    }

    inline PTR_MethodTableWriteableData GetWriteableDataForWrite()
    {
        LIMITED_METHOD_CONTRACT;
        g_IBCLogger.LogMethodTableWriteableDataWriteAccess(this);
        return m_pWriteableData;
    }

    inline PTR_MethodTableWriteableData GetWriteableDataForWrite_NoLogging()
    {
        return m_pWriteableData;
    }

    //-------------------------------------------------------------------
    // Remoting related
    // 
    inline BOOL IsRemotingConfigChecked()
    {
        WRAPPER_NO_CONTRACT;
        return GetWriteableData()->IsRemotingConfigChecked();
    }
    inline void SetRemotingConfigChecked()
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;

        GetWriteableDataForWrite()->SetRemotingConfigChecked();
    }
    inline void TrySetRemotingConfigChecked()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            SO_TOLERANT;
        }
        CONTRACTL_END;

        GetWriteableDataForWrite()->TrySetRemotingConfigChecked();
    }
    inline BOOL RequiresManagedActivation()
    {
        WRAPPER_NO_CONTRACT;
        return GetWriteableData()->RequiresManagedActivation();
    }
    inline void SetRequiresManagedActivation()
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;

        GetWriteableDataForWrite()->SetRequiresManagedActivation();
    }

    // Determines whether the type may require managed activation. The actual answer is known later
    // once the remoting config is checked.
    inline BOOL MayRequireManagedActivation()
    {
        LIMITED_METHOD_CONTRACT;
        return IsMarshaledByRef();
    }

    //-------------------------------------------------------------------
    // The GUID Info 
    // Used by COM interop to get GUIDs (IIDs and CLSIDs)

    // Get/store cached GUID information
    PTR_GuidInfo GetGuidInfo();
    void SetGuidInfo(GuidInfo* pGuidInfo);

    // Get and cache the GUID for this interface/class
    HRESULT GetGuidNoThrow(GUID *pGuid, BOOL bGenerateIfNotFound, BOOL bClassic = TRUE);

    // Get and cache the GUID for this interface/class
    void    GetGuid(GUID *pGuid, BOOL bGenerateIfNotFound, BOOL bClassic = TRUE);

#ifdef FEATURE_COMINTEROP
    // Get the GUID used for WinRT interop
    //   * for projection generic interfaces returns the equivalent WinRT type's GUID
    //   * for everything else returns the GetGuid(, TRUE)
    BOOL    GetGuidForWinRT(GUID *pGuid);

private:
    // Create RCW data associated with this type.
    RCWPerTypeData *CreateRCWPerTypeData(bool bThrowOnOOM);

public:
    // Get the RCW data associated with this type or NULL if the type does not need such data or allocation
    // failed (only if bThrowOnOOM is false).
    RCWPerTypeData *GetRCWPerTypeData(bool bThrowOnOOM = true);
#endif // FEATURE_COMINTEROP

    // Convenience method - determine if the interface/class has a guid specified (even if not yet cached)
    BOOL HasExplicitGuid();

public :
    // Helper routines for the GetFullyQualifiedNameForClass macros defined at the top of class.h.
    // You probably should not use these functions directly.
    SString &_GetFullyQualifiedNameForClassNestedAware(SString &ssBuf);
    SString &_GetFullyQualifiedNameForClass(SString &ssBuf);
    LPCUTF8 GetFullyQualifiedNameInfo(LPCUTF8 *ppszNamespace);

private:
    template<typename RedirectFunctor> SString &_GetFullyQualifiedNameForClassNestedAwareInternal(SString &ssBuf);

public :
    //-------------------------------------------------------------------
    // Debug Info 
    //


#ifdef _DEBUG
    inline LPCUTF8 GetDebugClassName()
    {
        LIMITED_METHOD_CONTRACT;
        return debug_m_szClassName;
    }
    inline void SetDebugClassName(LPCUTF8 name)
    {
        LIMITED_METHOD_CONTRACT;
        debug_m_szClassName = name;
    }

    // Was the type created with injected duplicates?
    // TRUE means that we tried to inject duplicates (not that we found one to inject).
    inline BOOL Debug_HasInjectedInterfaceDuplicates() const
    {
        LIMITED_METHOD_CONTRACT;
        return (GetWriteableData()->m_dwFlags & MethodTableWriteableData::enum_flag_HasInjectedInterfaceDuplicates) != 0;
    }
    inline void Debug_SetHasInjectedInterfaceDuplicates()
    {
        LIMITED_METHOD_CONTRACT;
        GetWriteableDataForWrite()->m_dwFlags |= MethodTableWriteableData::enum_flag_HasInjectedInterfaceDuplicates;
    }
#endif // _DEBUG


#ifndef DACCESS_COMPILE
public:
    //--------------------------------------------------------------------------------------
    class MethodData
    {
      public:
        inline ULONG AddRef()
            { LIMITED_METHOD_CONTRACT; return (ULONG) InterlockedIncrement((LONG*)&m_cRef); }

        ULONG Release();

        // Since all methods that return a MethodData already AddRef'd, we do NOT
        // want to AddRef when putting a holder around it. We only want to release it.
        static void HolderAcquire(MethodData *pEntry)
            { LIMITED_METHOD_CONTRACT; return; }
        static void HolderRelease(MethodData *pEntry)
            { WRAPPER_NO_CONTRACT; if (pEntry != NULL) pEntry->Release(); }

      protected:
        ULONG m_cRef;

      public:
        MethodData() : m_cRef(1) { LIMITED_METHOD_CONTRACT; }
        virtual ~MethodData() { LIMITED_METHOD_CONTRACT; }

        virtual MethodData  *GetDeclMethodData() = 0;
        virtual MethodTable *GetDeclMethodTable() = 0;
        virtual MethodDesc  *GetDeclMethodDesc(UINT32 slotNumber) = 0;
        
        virtual MethodData  *GetImplMethodData() = 0;
        virtual MethodTable *GetImplMethodTable() = 0;
        virtual DispatchSlot GetImplSlot(UINT32 slotNumber) = 0;
        // Returns INVALID_SLOT_NUMBER if no implementation exists.
        virtual UINT32       GetImplSlotNumber(UINT32 slotNumber) = 0;
        virtual MethodDesc  *GetImplMethodDesc(UINT32 slotNumber) = 0;
        virtual void InvalidateCachedVirtualSlot(UINT32 slotNumber) = 0;

        virtual UINT32 GetNumVirtuals() = 0;
        virtual UINT32 GetNumMethods() = 0;

      protected:
        static const UINT32 INVALID_SLOT_NUMBER = UINT32_MAX;

        // This is used when building the data
        struct MethodDataEntry
        {
          private:
            static const UINT32 INVALID_CHAIN_AND_INDEX = (UINT32)(-1);
            static const UINT16 INVALID_IMPL_SLOT_NUM = (UINT16)(-1);

            // This contains both the chain delta and the table index. The
            // reason that they are combined is that we need atomic update
            // of both, and it is convenient that both are on UINT16 in size.
            UINT32           m_chainDeltaAndTableIndex;
            UINT16           m_implSlotNum;     // For virtually remapped slots
            DispatchSlot     m_slot;            // The entry in the DispatchImplTable
            MethodDesc      *m_pMD;             // The MethodDesc for this slot

          public:
            inline MethodDataEntry() : m_slot(NULL)
                { WRAPPER_NO_CONTRACT; Init(); }

            inline void Init()
            {
                LIMITED_METHOD_CONTRACT;
                m_chainDeltaAndTableIndex = INVALID_CHAIN_AND_INDEX;
                m_implSlotNum = INVALID_IMPL_SLOT_NUM;
                m_slot = NULL;
                m_pMD = NULL;
            }

            inline BOOL IsDeclInit()
                { LIMITED_METHOD_CONTRACT; return m_chainDeltaAndTableIndex != INVALID_CHAIN_AND_INDEX; }
            inline BOOL IsImplInit()
                { LIMITED_METHOD_CONTRACT; return m_implSlotNum != INVALID_IMPL_SLOT_NUM; }

            inline void SetDeclData(UINT32 chainDelta, UINT32 tableIndex)
                { LIMITED_METHOD_CONTRACT; m_chainDeltaAndTableIndex = ((((UINT16) chainDelta) << 16) | ((UINT16) tableIndex)); }
            inline UINT32 GetChainDelta()
                { LIMITED_METHOD_CONTRACT; CONSISTENCY_CHECK(IsDeclInit()); return m_chainDeltaAndTableIndex >> 16; }
            inline UINT32 GetTableIndex()
                { LIMITED_METHOD_CONTRACT; CONSISTENCY_CHECK(IsDeclInit()); return (m_chainDeltaAndTableIndex & (UINT32)UINT16_MAX); }

            inline void SetImplData(UINT32 implSlotNum)
                { LIMITED_METHOD_CONTRACT; m_implSlotNum = (UINT16) implSlotNum; }
            inline UINT32 GetImplSlotNum()
                { LIMITED_METHOD_CONTRACT; CONSISTENCY_CHECK(IsImplInit()); return m_implSlotNum; }

            inline void SetSlot(DispatchSlot slot)
                { LIMITED_METHOD_CONTRACT; m_slot = slot; }
            inline DispatchSlot GetSlot()
                { LIMITED_METHOD_CONTRACT; return m_slot; }

            inline void SetMethodDesc(MethodDesc *pMD)
                { LIMITED_METHOD_CONTRACT; m_pMD = pMD; }
            inline MethodDesc *GetMethodDesc()
                { LIMITED_METHOD_CONTRACT; return m_pMD; }
                
        };

        static void ProcessMap(
            const DispatchMapTypeID * rgTypeIDs, 
            UINT32                    cTypeIDs, 
            MethodTable *             pMT, 
            UINT32                    cCurrentChainDepth, 
            MethodDataEntry *         rgWorkingData);
    };  // class MethodData

    typedef ::Holder < MethodData *, MethodData::HolderAcquire, MethodData::HolderRelease > MethodDataHolder;
    typedef ::Wrapper < MethodData *, MethodData::HolderAcquire, MethodData::HolderRelease > MethodDataWrapper;

protected:
    //--------------------------------------------------------------------------------------
    class MethodDataObject : public MethodData
    {
      public:
        // Static method that returns the amount of memory to allocate for a particular type.
        static UINT32 GetObjectSize(MethodTable *pMT);

        // Constructor. Make sure you have allocated enough memory using GetObjectSize.
        inline MethodDataObject(MethodTable *pMT)
            { WRAPPER_NO_CONTRACT; Init(pMT, NULL); }

        inline MethodDataObject(MethodTable *pMT, MethodData *pParentData)
            { WRAPPER_NO_CONTRACT; Init(pMT, pParentData); }

        virtual ~MethodDataObject() { LIMITED_METHOD_CONTRACT; }

        virtual MethodData  *GetDeclMethodData()
            { LIMITED_METHOD_CONTRACT; return this; }
        virtual MethodTable *GetDeclMethodTable()
            { LIMITED_METHOD_CONTRACT; return m_pMT; }
        virtual MethodDesc *GetDeclMethodDesc(UINT32 slotNumber);

        virtual MethodData  *GetImplMethodData()
            { LIMITED_METHOD_CONTRACT; return this; }
        virtual MethodTable *GetImplMethodTable()
            { LIMITED_METHOD_CONTRACT; return m_pMT; }
        virtual DispatchSlot GetImplSlot(UINT32 slotNumber);
        virtual UINT32       GetImplSlotNumber(UINT32 slotNumber);
        virtual MethodDesc  *GetImplMethodDesc(UINT32 slotNumber);
        virtual void InvalidateCachedVirtualSlot(UINT32 slotNumber);

        virtual UINT32 GetNumVirtuals()
            { LIMITED_METHOD_CONTRACT; return m_pMT->GetNumVirtuals(); }
        virtual UINT32 GetNumMethods()
            { LIMITED_METHOD_CONTRACT; return m_pMT->GetCanonicalMethodTable()->GetNumMethods(); }

      protected:
        void Init(MethodTable *pMT, MethodData *pParentData);

        BOOL PopulateNextLevel();

        // This is the method table for the actual type we're gathering the data for
        MethodTable *m_pMT;

        // This is used in staged map decoding - it indicates which type we will next decode.
        UINT32       m_iNextChainDepth;
        static const UINT32 MAX_CHAIN_DEPTH = UINT32_MAX;
        
        BOOL m_containsMethodImpl;

        // NOTE: Use of these APIs are unlocked and may appear to be erroneous. However, since calls
        //       to ProcessMap will result in identical values being placed in the MethodDataObjectEntry
        //       array, it it is not a problem if there is a race, since one thread may just end up
        //       doing some duplicate work.

        inline UINT32 GetNextChainDepth()
        { LIMITED_METHOD_CONTRACT; return VolatileLoad(&m_iNextChainDepth); }

        inline void SetNextChainDepth(UINT32 iDepth)
        {
            LIMITED_METHOD_CONTRACT;
            if (GetNextChainDepth() < iDepth) {
                VolatileStore(&m_iNextChainDepth, iDepth);
            }
        }

        // This is used when building the data
        struct MethodDataObjectEntry
        {
          private:
            MethodDesc *m_pMDDecl;
            MethodDesc *m_pMDImpl;

          public:
            inline MethodDataObjectEntry() : m_pMDDecl(NULL), m_pMDImpl(NULL) {}

            inline void SetDeclMethodDesc(MethodDesc *pMD)
                { LIMITED_METHOD_CONTRACT; m_pMDDecl = pMD; }
            inline MethodDesc *GetDeclMethodDesc()
                { LIMITED_METHOD_CONTRACT; return m_pMDDecl; }
            inline void SetImplMethodDesc(MethodDesc *pMD)
                { LIMITED_METHOD_CONTRACT; m_pMDImpl = pMD; }
            inline MethodDesc *GetImplMethodDesc()
                { LIMITED_METHOD_CONTRACT; return m_pMDImpl; }
        };

        //
        // At the end of this object is an array, so you cannot derive from this class.
        //

        inline MethodDataObjectEntry *GetEntryData()
            { LIMITED_METHOD_CONTRACT; return (MethodDataObjectEntry *)(this + 1); }

        inline MethodDataObjectEntry *GetEntry(UINT32 i)
            { LIMITED_METHOD_CONTRACT; CONSISTENCY_CHECK(i < GetNumMethods()); return GetEntryData() + i; }

        void FillEntryDataForAncestor(MethodTable *pMT);

        // MethodDataObjectEntry m_rgEntries[...];
    };  // class MethodDataObject

    //--------------------------------------------------------------------------------------
    class MethodDataInterface : public MethodData
    {
      public:
        // Static method that returns the amount of memory to allocate for a particular type.
        static UINT32 GetObjectSize(MethodTable *pMT)
            { LIMITED_METHOD_CONTRACT; return sizeof(MethodDataInterface); }

        // Constructor. Make sure you have allocated enough memory using GetObjectSize.
        MethodDataInterface(MethodTable *pMT)
        {
            LIMITED_METHOD_CONTRACT;
            CONSISTENCY_CHECK(CheckPointer(pMT));
            CONSISTENCY_CHECK(pMT->IsInterface());
            m_pMT = pMT;
        }
        virtual ~MethodDataInterface()
            { LIMITED_METHOD_CONTRACT; }

        //
        // Decl data
        //
        virtual MethodData  *GetDeclMethodData()
            { LIMITED_METHOD_CONTRACT; return this; }
        virtual MethodTable *GetDeclMethodTable()
            { LIMITED_METHOD_CONTRACT; return m_pMT; }
        virtual MethodDesc *GetDeclMethodDesc(UINT32 slotNumber);

        //
        // Impl data
        //
        virtual MethodData  *GetImplMethodData()
            { LIMITED_METHOD_CONTRACT; return this; }
        virtual MethodTable *GetImplMethodTable()
            { LIMITED_METHOD_CONTRACT; return m_pMT; }
        virtual DispatchSlot GetImplSlot(UINT32 slotNumber)
            { WRAPPER_NO_CONTRACT; return DispatchSlot(m_pMT->GetRestoredSlot(slotNumber)); }
        virtual UINT32       GetImplSlotNumber(UINT32 slotNumber)
            { LIMITED_METHOD_CONTRACT; return slotNumber; }
        virtual MethodDesc  *GetImplMethodDesc(UINT32 slotNumber);
        virtual void InvalidateCachedVirtualSlot(UINT32 slotNumber);

        //
        // Slot count data
        //
        virtual UINT32 GetNumVirtuals()
            { LIMITED_METHOD_CONTRACT; return m_pMT->GetNumVirtuals(); }
        virtual UINT32 GetNumMethods()
            { LIMITED_METHOD_CONTRACT; return m_pMT->GetNumMethods(); }

      protected:
        // This is the method table for the actual type we're gathering the data for
        MethodTable *m_pMT;
    };  // class MethodDataInterface

    //--------------------------------------------------------------------------------------
    class MethodDataInterfaceImpl : public MethodData
    {
      public:
        // Object construction-related methods
        static UINT32 GetObjectSize(MethodTable *pMTDecl);

        MethodDataInterfaceImpl(
            const DispatchMapTypeID * rgDeclTypeIDs, 
            UINT32                    cDeclTypeIDs, 
            MethodData *              pDecl, 
            MethodData *              pImpl);
        virtual ~MethodDataInterfaceImpl();

        // Decl-related methods
        virtual MethodData  *GetDeclMethodData()
            { LIMITED_METHOD_CONTRACT; return m_pDecl; }
        virtual MethodTable *GetDeclMethodTable()
            { WRAPPER_NO_CONTRACT; return m_pDecl->GetDeclMethodTable(); }
        virtual MethodDesc  *GetDeclMethodDesc(UINT32 slotNumber)
            { WRAPPER_NO_CONTRACT; return m_pDecl->GetDeclMethodDesc(slotNumber); }

        // Impl-related methods
        virtual MethodData  *GetImplMethodData()
            { LIMITED_METHOD_CONTRACT; return m_pImpl; }
        virtual MethodTable *GetImplMethodTable()
            { WRAPPER_NO_CONTRACT; return m_pImpl->GetImplMethodTable(); }
        virtual DispatchSlot GetImplSlot(UINT32 slotNumber);
        virtual UINT32       GetImplSlotNumber(UINT32 slotNumber);
        virtual MethodDesc  *GetImplMethodDesc(UINT32 slotNumber);
        virtual void InvalidateCachedVirtualSlot(UINT32 slotNumber);

        virtual UINT32 GetNumVirtuals()
            { WRAPPER_NO_CONTRACT; return m_pDecl->GetNumVirtuals(); }
        virtual UINT32 GetNumMethods()
            { WRAPPER_NO_CONTRACT; return m_pDecl->GetNumVirtuals(); }

      protected:
        UINT32 MapToImplSlotNumber(UINT32 slotNumber);

        BOOL PopulateNextLevel();
        void Init(
            const DispatchMapTypeID * rgDeclTypeIDs, 
            UINT32                    cDeclTypeIDs, 
            MethodData *              pDecl, 
            MethodData *              pImpl);
        
        MethodData *m_pDecl;
        MethodData *m_pImpl;

        // This is used in staged map decoding - it indicates which type(s) we will find.
        const DispatchMapTypeID * m_rgDeclTypeIDs;
        UINT32                    m_cDeclTypeIDs;
        UINT32                    m_iNextChainDepth;
        static const UINT32       MAX_CHAIN_DEPTH = UINT32_MAX;

        inline UINT32 GetNextChainDepth()
        { LIMITED_METHOD_CONTRACT; return VolatileLoad(&m_iNextChainDepth); }

        inline void SetNextChainDepth(UINT32 iDepth)
        {
            LIMITED_METHOD_CONTRACT;
            if (GetNextChainDepth() < iDepth) {
                VolatileStore(&m_iNextChainDepth, iDepth);
            }
        }

        //
        // At the end of this object is an array, so you cannot derive from this class.
        //

        inline MethodDataEntry *GetEntryData()
            { LIMITED_METHOD_CONTRACT; return (MethodDataEntry *)(this + 1); }

        inline MethodDataEntry *GetEntry(UINT32 i)
            { LIMITED_METHOD_CONTRACT; CONSISTENCY_CHECK(i < GetNumMethods()); return GetEntryData() + i; }

        // MethodDataEntry m_rgEntries[...];
    };  // class MethodDataInterfaceImpl

    //--------------------------------------------------------------------------------------
    static MethodDataCache *s_pMethodDataCache;
    static BOOL             s_fUseParentMethodData;
    static BOOL             s_fUseMethodDataCache;

public:
    static void AllowMethodDataCaching()
        { WRAPPER_NO_CONTRACT; CheckInitMethodDataCache(); s_fUseMethodDataCache = TRUE; }
    static void ClearMethodDataCache();
    static void AllowParentMethodDataCopy()
        { LIMITED_METHOD_CONTRACT; s_fUseParentMethodData = TRUE; }
    // NOTE: The fCanCache argument determines if the resulting MethodData object can
    //       be added to the global MethodDataCache. This is used when requesting a
    //       MethodData object for a type currently being built.
    static MethodData *GetMethodData(MethodTable *pMT, BOOL fCanCache = TRUE);
    static MethodData *GetMethodData(MethodTable *pMTDecl, MethodTable *pMTImpl, BOOL fCanCache = TRUE);
    // This method is used by BuildMethodTable because the exact interface has not yet been loaded.
    // NOTE: This method does not cache the resulting MethodData object in the global MethodDataCache.
    static MethodData * GetMethodData(
        const DispatchMapTypeID * rgDeclTypeIDs, 
        UINT32                    cDeclTypeIDs, 
        MethodTable *             pMTDecl, 
        MethodTable *             pMTImpl);

protected:
    static void CheckInitMethodDataCache();
    static MethodData *FindParentMethodDataHelper(MethodTable *pMT);
    static MethodData *FindMethodDataHelper(MethodTable *pMTDecl, MethodTable *pMTImpl);
    static MethodData *GetMethodDataHelper(MethodTable *pMTDecl, MethodTable *pMTImpl, BOOL fCanCache);
    // NOTE: This method does not cache the resulting MethodData object in the global MethodDataCache.
    static MethodData * GetMethodDataHelper(
        const DispatchMapTypeID * rgDeclTypeIDs, 
        UINT32                    cDeclTypeIDs, 
        MethodTable *             pMTDecl, 
        MethodTable *             pMTImpl);

public:
    //--------------------------------------------------------------------------------------
    class MethodIterator
    {
    public:
        MethodIterator(MethodTable *pMT);
        MethodIterator(MethodTable *pMTDecl, MethodTable *pMTImpl);
        MethodIterator(MethodData *pMethodData);
        MethodIterator(const MethodIterator &it);
        inline ~MethodIterator() { WRAPPER_NO_CONTRACT; m_pMethodData->Release(); }
        INT32 GetNumMethods() const;
        inline BOOL IsValid() const;
        inline BOOL MoveTo(UINT32 idx);
        inline BOOL Prev();
        inline BOOL Next();
        inline void MoveToBegin();
        inline void MoveToEnd();
        inline UINT32 GetSlotNumber() const;
        inline UINT32 GetImplSlotNumber() const;
        inline BOOL IsVirtual() const;
        inline UINT32 GetNumVirtuals() const;
        inline DispatchSlot GetTarget() const;

        // Can be called only if IsValid()=TRUE
        inline MethodDesc *GetMethodDesc() const;
        inline MethodDesc *GetDeclMethodDesc() const;

    protected:
        void Init(MethodTable *pMTDecl, MethodTable *pMTImpl);

        MethodData         *m_pMethodData;
        INT32               m_iCur;           // Current logical slot index
        INT32               m_iMethods;
    };  // class MethodIterator
#endif // !DACCESS_COMPILE

    //--------------------------------------------------------------------------------------
    // This iterator lets you walk over all the method bodies introduced by this type.
    // This includes new static methods, new non-virtual methods, and any overrides
    // of the parent's virtual methods. It does not include virtual method implementations
    // provided by the parent
    
    class IntroducedMethodIterator
    {
    public:
        IntroducedMethodIterator(MethodTable *pMT, BOOL restrictToCanonicalTypes = TRUE);
        inline BOOL IsValid() const;
        BOOL Next();

        // Can be called only if IsValid()=TRUE
        inline MethodDesc *GetMethodDesc() const;

        // Static worker methods of the iterator. These are meant to be used
        // by RuntimeTypeHandle::GetFirstIntroducedMethod and RuntimeTypeHandle::GetNextIntroducedMethod 
        // only to expose this iterator to managed code.
        static MethodDesc * GetFirst(MethodTable * pMT);
        static MethodDesc * GetNext(MethodDesc * pMD);

    protected:
        MethodDesc      *m_pMethodDesc;     // Current method desc

        // Cached info about current method desc
        MethodDescChunk *m_pChunk;
        TADDR            m_pChunkEnd;

        void SetChunk(MethodDescChunk * pChunk);
    };  // class IntroducedMethodIterator

    //-------------------------------------------------------------------
    // INSTANCE MEMBER VARIABLES 
    //

#ifdef DACCESS_COMPILE
public:
#else
private:
#endif
    enum WFLAGS_LOW_ENUM
    {
        // AS YOU ADD NEW FLAGS PLEASE CONSIDER WHETHER Generics::NewInstantiation NEEDS
        // TO BE UPDATED IN ORDER TO ENSURE THAT METHODTABLES DUPLICATED FOR GENERIC INSTANTIATIONS
        // CARRY THE CORECT FLAGS.
        //

        // We are overloading the low 2 bytes of m_dwFlags to be a component size for Strings
        // and Arrays and some set of flags which we can be assured are of a specified state
        // for Strings / Arrays, currently these will be a bunch of generics flags which don't
        // apply to Strings / Arrays.

        enum_flag_UNUSED_ComponentSize_1    = 0x00000001,

        enum_flag_StaticsMask               = 0x00000006,
        enum_flag_StaticsMask_NonDynamic    = 0x00000000,
        enum_flag_StaticsMask_Dynamic       = 0x00000002,   // dynamic statics (EnC, reflection.emit)
        enum_flag_StaticsMask_Generics      = 0x00000004,   // generics statics
        enum_flag_StaticsMask_CrossModuleGenerics       = 0x00000006, // cross module generics statics (NGen)
        enum_flag_StaticsMask_IfGenericsThenCrossModule = 0x00000002, // helper constant to get rid of unnecessary check

        enum_flag_NotInPZM                  = 0x00000008,   // True if this type is not in its PreferredZapModule

        enum_flag_GenericsMask              = 0x00000030,
        enum_flag_GenericsMask_NonGeneric   = 0x00000000,   // no instantiation
        enum_flag_GenericsMask_GenericInst  = 0x00000010,   // regular instantiation, e.g. List<String>
        enum_flag_GenericsMask_SharedInst   = 0x00000020,   // shared instantiation, e.g. List<__Canon> or List<MyValueType<__Canon>>
        enum_flag_GenericsMask_TypicalInst  = 0x00000030,   // the type instantiated at its formal parameters, e.g. List<T>

#ifdef FEATURE_REMOTING
        enum_flag_ContextStatic             = 0x00000040,
#endif
        enum_flag_HasRemotingVtsInfo        = 0x00000080,   // Optional data present indicating VTS methods and optional fields

        enum_flag_HasVariance               = 0x00000100,   // This is an instantiated type some of whose type parameters are co or contra-variant

        enum_flag_HasDefaultCtor            = 0x00000200,
        enum_flag_HasPreciseInitCctors      = 0x00000400,   // Do we need to run class constructors at allocation time? (Not perf important, could be moved to EEClass

#if defined(FEATURE_HFA)
#if defined(FEATURE_UNIX_AMD64_STRUCT_PASSING_ITF)
#error Can't define both FEATURE_HFA and FEATURE_UNIX_AMD64_STRUCT_PASSING_ITF
#endif
        enum_flag_IsHFA                     = 0x00000800,   // This type is an HFA (Homogenous Floating-point Aggregate)
#endif // FEATURE_HFA

#if defined(FEATURE_UNIX_AMD64_STRUCT_PASSING_ITF)
#if defined(FEATURE_HFA)
#error Can't define both FEATURE_HFA and FEATURE_UNIX_AMD64_STRUCT_PASSING_ITF
#endif
        enum_flag_IsRegStructPassed         = 0x00000800,   // This type is a System V register passed struct.
#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING_ITF

        // In a perfect world we would fill these flags using other flags that we already have
        // which have a constant value for something which has a component size.
        enum_flag_UNUSED_ComponentSize_4    = 0x00001000,
        enum_flag_UNUSED_ComponentSize_5    = 0x00002000,
        enum_flag_UNUSED_ComponentSize_6    = 0x00004000,
        enum_flag_UNUSED_ComponentSize_7    = 0x00008000,

#define SET_FALSE(flag)     (flag & 0)
#define SET_TRUE(flag)      (flag & 0xffff)

        // IMPORTANT! IMPORTANT! IMPORTANT!
        //
        // As you change the flags in WFLAGS_LOW_ENUM you also need to change this
        // to be up to date to reflect the default values of those flags for the
        // case where this MethodTable is for a String or Array
        enum_flag_StringArrayValues = SET_TRUE(enum_flag_StaticsMask_NonDynamic) |
                                      SET_FALSE(enum_flag_NotInPZM) |
                                      SET_TRUE(enum_flag_GenericsMask_NonGeneric) |
#ifdef FEATURE_REMOTING
                                      SET_FALSE(enum_flag_ContextStatic) |
#endif
                                      SET_FALSE(enum_flag_HasVariance) |
                                      SET_FALSE(enum_flag_HasDefaultCtor) |
                                      SET_FALSE(enum_flag_HasPreciseInitCctors),

    };  // enum WFLAGS_LOW_ENUM

    enum WFLAGS_HIGH_ENUM
    {
        // DO NOT use flags that have bits set in the low 2 bytes.
        // These flags are DWORD sized so that our atomic masking
        // operations can operate on the entire 4-byte aligned DWORD
        // instead of the logical non-aligned WORD of flags.  The
        // low WORD of flags is reserved for the component size.

        // The following bits describe mutually exclusive locations of the type
        // in the type hiearchy.
        enum_flag_Category_Mask             = 0x000F0000,

        enum_flag_Category_Class            = 0x00000000,
        enum_flag_Category_Unused_1         = 0x00010000,

        enum_flag_Category_MarshalByRef_Mask= 0x000E0000,
        enum_flag_Category_MarshalByRef     = 0x00020000,
        enum_flag_Category_Contextful       = 0x00030000, // sub-category of MarshalByRef

        enum_flag_Category_ValueType        = 0x00040000,
        enum_flag_Category_ValueType_Mask   = 0x000C0000,
        enum_flag_Category_Nullable         = 0x00050000, // sub-category of ValueType
        enum_flag_Category_PrimitiveValueType=0x00060000, // sub-category of ValueType, Enum or primitive value type
        enum_flag_Category_TruePrimitive    = 0x00070000, // sub-category of ValueType, Primitive (ELEMENT_TYPE_I, etc.)

        enum_flag_Category_Array            = 0x00080000,
        enum_flag_Category_Array_Mask       = 0x000C0000,
        // enum_flag_Category_IfArrayThenUnused                 = 0x00010000, // sub-category of Array
        enum_flag_Category_IfArrayThenSzArray                   = 0x00020000, // sub-category of Array

        enum_flag_Category_Interface        = 0x000C0000,
        enum_flag_Category_Unused_2         = 0x000D0000,
        enum_flag_Category_TransparentProxy = 0x000E0000,
        enum_flag_Category_AsyncPin         = 0x000F0000,

        enum_flag_Category_ElementTypeMask  = 0x000E0000, // bits that matter for element type mask


        enum_flag_HasFinalizer                = 0x00100000, // instances require finalization

        enum_flag_IfNotInterfaceThenMarshalable = 0x00200000, // Is this type marshalable by the pinvoke marshalling layer
#ifdef FEATURE_COMINTEROP
        enum_flag_IfInterfaceThenHasGuidInfo    = 0x00200000, // Does the type has optional GuidInfo
#endif // FEATURE_COMINTEROP

        enum_flag_ICastable                   = 0x00400000, // class implements ICastable interface

        enum_flag_HasIndirectParent           = 0x00800000, // m_pParentMethodTable has double indirection

        enum_flag_ContainsPointers            = 0x01000000,

        enum_flag_HasTypeEquivalence          = 0x02000000, // can be equivalent to another type

#ifdef FEATURE_COMINTEROP
        enum_flag_HasRCWPerTypeData           = 0x04000000, // has optional pointer to RCWPerTypeData
#endif // FEATURE_COMINTEROP

        enum_flag_HasCriticalFinalizer        = 0x08000000, // finalizer must be run on Appdomain Unload
        enum_flag_Collectible                 = 0x10000000,
        enum_flag_ContainsGenericVariables    = 0x20000000,   // we cache this flag to help detect these efficiently and
                                                              // to detect this condition when restoring

        enum_flag_ComObject                   = 0x40000000, // class is a com object
        
        enum_flag_HasComponentSize            = 0x80000000,   // This is set if component size is used for flags.

        // Types that require non-trivial interface cast have this bit set in the category
        enum_flag_NonTrivialInterfaceCast   =  enum_flag_Category_Array
                                             | enum_flag_ComObject
                                             | enum_flag_ICastable

    };  // enum WFLAGS_HIGH_ENUM

// NIDump needs to be able to see these flags
// TODO: figure out how to make these private
#if defined(DACCESS_COMPILE)
public:
#else
private:
#endif
    enum WFLAGS2_ENUM
    {
        // AS YOU ADD NEW FLAGS PLEASE CONSIDER WHETHER Generics::NewInstantiation NEEDS
        // TO BE UPDATED IN ORDER TO ENSURE THAT METHODTABLES DUPLICATED FOR GENERIC INSTANTIATIONS
        // CARRY THE CORECT FLAGS.

        // The following bits describe usage of optional slots. They have to stay
        // together because of we index using them into offset arrays.
        enum_flag_MultipurposeSlotsMask     = 0x001F,
        enum_flag_HasPerInstInfo            = 0x0001,
        enum_flag_HasInterfaceMap           = 0x0002,
        enum_flag_HasDispatchMapSlot        = 0x0004,
        enum_flag_HasNonVirtualSlots        = 0x0008,
        enum_flag_HasModuleOverride         = 0x0010,

        enum_flag_IsZapped                  = 0x0020, // This could be fetched from m_pLoaderModule if we run out of flags

        enum_flag_IsPreRestored             = 0x0040, // Class does not need restore
                                                      // This flag is set only for NGENed classes (IsZapped is true)

        enum_flag_HasModuleDependencies     = 0x0080,

        enum_flag_NoSecurityProperties      = 0x0100, // Class does not have security properties (that is,
                                                      // GetClass()->GetSecurityProperties will return 0).

        enum_flag_RequiresDispatchTokenFat  = 0x0200,

        enum_flag_HasCctor                  = 0x0400,
        enum_flag_HasCCWTemplate            = 0x0800, // Has an extra field pointing to a CCW template

#ifdef FEATURE_64BIT_ALIGNMENT
        enum_flag_RequiresAlign8            = 0x1000, // Type requires 8-byte alignment (only set on platforms that require this and don't get it implicitly)
#endif

        enum_flag_HasBoxedRegularStatics    = 0x2000, // GetNumBoxedRegularStatics() != 0

        enum_flag_HasSingleNonVirtualSlot   = 0x4000,

        enum_flag_DependsOnEquivalentOrForwardedStructs= 0x8000, // Declares methods that have type equivalent or type forwarded structures in their signature

    };  // enum WFLAGS2_ENUM

    __forceinline void ClearFlag(WFLAGS_LOW_ENUM flag)
    {
        _ASSERTE(!IsStringOrArray());
        m_dwFlags &= ~flag;
    }
    __forceinline void SetFlag(WFLAGS_LOW_ENUM flag)
    {
        _ASSERTE(!IsStringOrArray());
        m_dwFlags |= flag;
    }
    __forceinline DWORD GetFlag(WFLAGS_LOW_ENUM flag) const
    {
        SUPPORTS_DAC;
        return (IsStringOrArray() ? (enum_flag_StringArrayValues & flag) : (m_dwFlags & flag));
    }
    __forceinline BOOL TestFlagWithMask(WFLAGS_LOW_ENUM mask, WFLAGS_LOW_ENUM flag) const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return (IsStringOrArray() ? (((DWORD)enum_flag_StringArrayValues & (DWORD)mask) == (DWORD)flag) :
            ((m_dwFlags & (DWORD)mask) == (DWORD)flag));
    }
                                                               
    __forceinline void ClearFlag(WFLAGS_HIGH_ENUM flag)
    {
        m_dwFlags &= ~flag;
    }
    __forceinline void SetFlag(WFLAGS_HIGH_ENUM flag)
    {
        m_dwFlags |= flag;
    }
    __forceinline DWORD GetFlag(WFLAGS_HIGH_ENUM flag) const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_dwFlags & flag;
    }
    __forceinline BOOL TestFlagWithMask(WFLAGS_HIGH_ENUM mask, WFLAGS_HIGH_ENUM flag) const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return ((m_dwFlags & (DWORD)mask) == (DWORD)flag);
    }

    __forceinline void ClearFlag(WFLAGS2_ENUM flag)
    {
        m_wFlags2 &= ~flag;
    }
    __forceinline void SetFlag(WFLAGS2_ENUM flag)
    {
        m_wFlags2 |= flag;
    }
    __forceinline DWORD GetFlag(WFLAGS2_ENUM flag) const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_wFlags2 & flag;
    }
    __forceinline BOOL TestFlagWithMask(WFLAGS2_ENUM mask, WFLAGS2_ENUM flag) const
    {
        return (m_wFlags2 & (DWORD)mask) == (DWORD)flag;
    }

    // Just exposing a couple of these for x86 asm versions of JIT_IsInstanceOfClass and JIT_IsInstanceOfInterface
public:
    enum
    {
        public_enum_flag_HasTypeEquivalence = enum_flag_HasTypeEquivalence,
        public_enum_flag_NonTrivialInterfaceCast = enum_flag_NonTrivialInterfaceCast,
    };

private: 
    /*
     * This stuff must be first in the struct and should fit on a cache line - don't move it. Used by the GC.
     */   
    // struct
    // {

    // Low WORD is component size for array and string types (HasComponentSize() returns true).
    // Used for flags otherwise.
    DWORD           m_dwFlags;

    // Base size of instance of this class when allocated on the heap
    DWORD           m_BaseSize;
    // }

    WORD            m_wFlags2;

    // Class token if it fits into 16-bits. If this is (WORD)-1, the class token is stored in the TokenOverflow optional member.
    WORD            m_wToken;
        
    // <NICE> In the normal cases we shouldn't need a full word for each of these </NICE>
    WORD            m_wNumVirtuals;
    WORD            m_wNumInterfaces;
    
#ifdef _DEBUG
    LPCUTF8         debug_m_szClassName;
#endif //_DEBUG
    
    // Parent PTR_MethodTable if enum_flag_HasIndirectParent is not set. Pointer to indirection cell
    // if enum_flag_enum_flag_HasIndirectParent is set. The indirection is offset by offsetof(MethodTable, m_pParentMethodTable).
    // It allows casting helpers to go through parent chain natually. Casting helper do not need need the explicit check
    // for enum_flag_HasIndirectParentMethodTable.
    TADDR           m_pParentMethodTable;

    PTR_Module      m_pLoaderModule;    // LoaderModule. It is equal to the ZapModule in ngened images
    
    PTR_MethodTableWriteableData m_pWriteableData;
    
    // The value of lowest two bits describe what the union contains
    enum LowBits {
        UNION_EECLASS      = 0,    //  0 - pointer to EEClass. This MethodTable is the canonical method table.
        UNION_INVALID      = 1,    //  1 - not used
        UNION_METHODTABLE  = 2,    //  2 - pointer to canonical MethodTable.
        UNION_INDIRECTION  = 3     //  3 - pointer to indirection cell that points to canonical MethodTable.
    };                             //      (used only if FEATURE_PREJIT is defined)
    static const TADDR UNION_MASK = 3; 

    union {
        EEClass *   m_pEEClass;
        TADDR       m_pCanonMT;
    };

    __forceinline static LowBits union_getLowBits(TADDR pCanonMT)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return LowBits(pCanonMT & UNION_MASK);
    }
    __forceinline static TADDR   union_getPointer(TADDR pCanonMT)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return (pCanonMT & ~UNION_MASK);
    }

    // m_pPerInstInfo and m_pInterfaceMap have to be at fixed offsets because of performance sensitive 
    // JITed code and JIT helpers. However, they are frequently not present. The space is used by other
    // multipurpose slots on first come first served basis if the fixed ones are not present. The other 
    // multipurpose are DispatchMapSlot, NonVirtualSlots, ModuleOverride (see enum_flag_MultipurposeSlotsMask).
    // The multipurpose slots that do not fit are stored after vtable slots.

    union
    {
        PTR_Dictionary *    m_pPerInstInfo;
        TADDR               m_ElementTypeHnd;
        TADDR               m_pMultipurposeSlot1;
    };
    public:
    union
    {
        InterfaceInfo_t *   m_pInterfaceMap;
        TADDR               m_pMultipurposeSlot2;
    };

    // VTable and Non-Virtual slots go here

    // Overflow multipurpose slots go here

    // Optional Members go here
    //    See above for the list of optional members

    // Generic dictionary pointers go here

    // Interface map goes here

    // Generic instantiation+dictionary goes here

private:

    // disallow direct creation
    void *operator new(size_t dummy);
    void operator delete(void *pData);
    MethodTable();
    
    // Optional members.  These are used for fields in the data structure where
    // the fields are (a) known when MT is created and (b) there is a default
    // value for the field in the common case.  That is, they are normally used
    // for data that is only relevant to a small number of method tables.

    // Optional members and multipurpose slots have similar purpose, but they differ in details:
    // - Multipurpose slots can only accomodate pointer sized structures right now. It is non-trivial 
    //   to add new ones, the access is faster.
    // - Optional members can accomodate structures of any size. It is trivial to add new ones,
    //   the access is slower.

    // The following macro will automatically create GetXXX accessors for the optional members.
#define METHODTABLE_OPTIONAL_MEMBERS() \
    /*                          NAME                    TYPE                            GETTER                     */ \
    /* Accessing this member efficiently is currently performance critical for static field accesses               */ \
    /* in generic classes, so place it early in the list. */                                                          \
    METHODTABLE_OPTIONAL_MEMBER(GenericsStaticsInfo,    GenericsStaticsInfo,            GetGenericsStaticsInfo      ) \
    /* Accessed by interop, fairly frequently. */                                                                     \
    METHODTABLE_COMINTEROP_OPTIONAL_MEMBERS()                                                                         \
    /* Accessed during x-domain transition only, so place it late in the list. */                                     \
    METHODTABLE_REMOTING_OPTIONAL_MEMBERS()                                                                           \
    /* Accessed during certain generic type load operations only, so low priority */                                  \
    METHODTABLE_OPTIONAL_MEMBER(ExtraInterfaceInfo,     TADDR,                          GetExtraInterfaceInfoPtr    ) \
    /* TypeDef token for assemblies with more than 64k types. Never happens in real world. */                         \
    METHODTABLE_OPTIONAL_MEMBER(TokenOverflow,          TADDR,                          GetTokenOverflowPtr         ) \

#ifdef FEATURE_COMINTEROP
#define METHODTABLE_COMINTEROP_OPTIONAL_MEMBERS() \
    METHODTABLE_OPTIONAL_MEMBER(GuidInfo,               PTR_GuidInfo,                   GetGuidInfoPtr              ) \
    METHODTABLE_OPTIONAL_MEMBER(RCWPerTypeData,         RCWPerTypeData *,               GetRCWPerTypeDataPtr        ) \
    METHODTABLE_OPTIONAL_MEMBER(CCWTemplate,            ComCallWrapperTemplate *,       GetCCWTemplatePtr           )
#else
#define METHODTABLE_COMINTEROP_OPTIONAL_MEMBERS()
#endif

#ifdef FEATURE_REMOTING
#define METHODTABLE_REMOTING_OPTIONAL_MEMBERS() \
    METHODTABLE_OPTIONAL_MEMBER(RemotingVtsInfo,        PTR_RemotingVtsInfo,            GetRemotingVtsInfoPtr       ) \
    METHODTABLE_OPTIONAL_MEMBER(RemotableMethodInfo,    PTR_CrossDomainOptimizationInfo,GetRemotableMethodInfoPtr   ) \
    METHODTABLE_OPTIONAL_MEMBER(ContextStatics,         PTR_ContextStaticsBucket,       GetContextStaticsBucketPtr  )
#else
#define METHODTABLE_REMOTING_OPTIONAL_MEMBERS()
#endif

    enum OptionalMemberId
    {
#undef METHODTABLE_OPTIONAL_MEMBER
#define METHODTABLE_OPTIONAL_MEMBER(NAME, TYPE, GETTER) OptionalMember_##NAME,
        METHODTABLE_OPTIONAL_MEMBERS()
        OptionalMember_Count,

        OptionalMember_First = OptionalMember_GenericsStaticsInfo,
    };

    FORCEINLINE DWORD GetOffsetOfOptionalMember(OptionalMemberId id);

public:

    //
    // Public accessor helpers for the optional members of MethodTable
    //

#undef METHODTABLE_OPTIONAL_MEMBER
#define METHODTABLE_OPTIONAL_MEMBER(NAME, TYPE, GETTER) \
    inline DPTR(TYPE) GETTER() \
    { \
        LIMITED_METHOD_CONTRACT; \
        STATIC_CONTRACT_SO_TOLERANT; \
        _ASSERTE(Has##NAME()); \
        return dac_cast<DPTR(TYPE)>(dac_cast<TADDR>(this) + GetOffsetOfOptionalMember(OptionalMember_##NAME)); \
    }

    METHODTABLE_OPTIONAL_MEMBERS()
 
private:
    inline DWORD GetStartOffsetOfOptionalMembers()
    {
        WRAPPER_NO_CONTRACT;
        return GetOffsetOfOptionalMember(OptionalMember_First);
    }

    inline DWORD GetEndOffsetOfOptionalMembers()
    {
        WRAPPER_NO_CONTRACT;
        return GetOffsetOfOptionalMember(OptionalMember_Count);
    }

    inline static DWORD GetOptionalMembersAllocationSize(
                                                  DWORD dwMultipurposeSlotsMask,
                                                  BOOL needsRemotableMethodInfo,
                                                  BOOL needsGenericsStaticsInfo,
                                                  BOOL needsGuidInfo,
                                                  BOOL needsCCWTemplate,
                                                  BOOL needsRCWPerTypeData,
                                                  BOOL needsRemotingVtsInfo,
                                                  BOOL needsContextStatic,
                                                  BOOL needsTokenOverflow);
    inline DWORD GetOptionalMembersSize();

    // The PerInstInfo is a (possibly empty) array of pointers to 
    // Instantiations/Dictionaries. This array comes after the optional members.
    inline DWORD GetPerInstInfoSize();

    // This is the size of the interface map chunk in the method table.
    // If the MethodTable has a dynamic interface map then the size includes the pointer
    // that stores the extra info for that map.
    // The interface map itself comes after the PerInstInfo (if any)
    inline DWORD GetInterfaceMapSize();

    // The instantiation/dictionary comes at the end of the MethodTable after
    //  the interface map.  
    inline DWORD GetInstAndDictSize();

private:
    // Helper template to compute the offsets at compile time
    template<int mask>
    struct MultipurposeSlotOffset;

    static const BYTE c_DispatchMapSlotOffsets[];
    static const BYTE c_NonVirtualSlotsOffsets[];
    static const BYTE c_ModuleOverrideOffsets[];

    static const BYTE c_OptionalMembersStartOffsets[]; // total sizes of optional slots

    TADDR GetMultipurposeSlotPtr(WFLAGS2_ENUM flag, const BYTE * offsets);

    void SetMultipurposeSlotsMask(DWORD dwMask)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE((m_wFlags2 & enum_flag_MultipurposeSlotsMask) == 0);
        m_wFlags2 |= (WORD)dwMask;
    }

    BOOL HasModuleOverride()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return GetFlag(enum_flag_HasModuleOverride);
    }

    DPTR(RelativeFixupPointer<PTR_Module>) GetModuleOverridePtr()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return dac_cast<DPTR(RelativeFixupPointer<PTR_Module>)>(GetMultipurposeSlotPtr(enum_flag_HasModuleOverride, c_ModuleOverrideOffsets));
    }

#ifdef BINDER
    void SetModule(PTR_Module pModule);
#else
    void SetModule(Module * pModule);
#endif

    /************************************
    //
    // CONTEXT STATIC
    //
    ************************************/

public:
#ifdef FEATURE_REMOTING
    inline BOOL HasContextStatics();
    inline void SetHasContextStatics();

    inline PTR_ContextStaticsBucket GetContextStaticsBucket()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        _ASSERTE(HasContextStatics());
        PTR_ContextStaticsBucket pBucket = *GetContextStaticsBucketPtr();
        _ASSERTE(pBucket != NULL);
        return pBucket;
    }

    inline DWORD GetContextStaticsOffset();
    inline WORD GetContextStaticsSize();

    void SetupContextStatics(AllocMemTracker *pamTracker, WORD dwContextStaticsSize);
    DWORD AllocateContextStaticsOffset();
#endif

    BOOL Validate ();

#ifdef FEATURE_READYTORUN_COMPILER
    //
    // Is field layout in this type fixed within the current version bubble?
    // This check does not take the inheritance chain into account.
    //
    BOOL IsLayoutFixedInCurrentVersionBubble();

    //
    // Is field layout of the inheritance chain fixed within the current version bubble?
    //
    BOOL IsInheritanceChainLayoutFixedInCurrentVersionBubble();
#endif

};  // class MethodTable

#if defined(FEATURE_COMINTEROP) && !defined(DACCESS_COMPILE)
WORD GetEquivalentMethodSlot(MethodTable * pOldMT, MethodTable * pNewMT, WORD wMTslot, BOOL *pfFound);
#endif // defined(FEATURE_COMINTEROP) && !defined(DACCESS_COMPILE)

MethodTable* CreateMinimalMethodTable(Module* pContainingModule, 
                                      LoaderHeap* pCreationHeap,
                                      AllocMemTracker* pamTracker);

#endif // !_METHODTABLE_H_
