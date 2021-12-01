// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ==--==
//
// File: CLASS.H
//

//
// NOTE: Even though EEClass is considered to contain cold data (relative to MethodTable), these data
// structures *are* touched (especially during startup as part of soft-binding). As a result, and given the
// number of EEClasses allocated for large assemblies, the size of this structure can have a direct impact on
// performance, especially startup performance.
//
// Given that the data itself is touched infrequently, we can trade off space reduction against cpu-usage to
// good effect here. A fair amount of work has gone into reducing the size of each EEClass instance (see
// EEClassOptionalFields and EEClassPackedFields) at the expense of somewhat more convoluted runtime access.
//
// Please consider this (and measure the impact of your changes against startup scenarios) before adding
// fields to EEClass or otherwise increasing its size.
//
// ============================================================================

#ifndef CLASS_H
#define CLASS_H

/*
 *  Include Files
 */
#include "eecontract.h"
#include "argslot.h"
#include "vars.hpp"
#include "cor.h"
#include "clrex.h"
#include "hash.h"
#include "crst.h"
#include "cgensys.h"
#ifdef FEATURE_COMINTEROP
#include "stdinterfaces.h"
#endif
#include "slist.h"
#include "spinlock.h"
#include "typehandle.h"
#include "methodtable.h"
#include "eeconfig.h"
#include "typectxt.h"
#include "iterator_util.h"

#include "packedfields.inl"
#include "array.h"
#define IBCLOG(x) g_IBCLogger.##x

VOID DECLSPEC_NORETURN RealCOMPlusThrowHR(HRESULT hr);

/*
 *  Macro definitions
 */
#define MAX_LOG2_PRIMITIVE_FIELD_SIZE   3

#define MAX_PRIMITIVE_FIELD_SIZE        (1 << MAX_LOG2_PRIMITIVE_FIELD_SIZE)

/*
 *  Forward declarations
 */
class   AppDomain;
class   ArrayClass;
class   ArrayMethodDesc;
class   Assembly;
class   ClassLoader;
class   DictionaryLayout;
class   FCallMethodDesc;
class   EEClass;
class   EnCFieldDesc;
class   FieldDesc;
class   NativeFieldDescriptor;
class   EEClassNativeLayoutInfo;
struct  LayoutRawFieldInfo;
class   MetaSig;
class   MethodDesc;
class   MethodDescChunk;
class   MethodTable;
class   Module;
class   Object;
class   Stub;
class   Substitution;
class   SystemDomain;
class   TypeHandle;
class   StackingAllocator;
class   AllocMemTracker;
class   InteropMethodTableSlotDataMap;
class LoadingEntry_LockHolder;
class   DispatchMapBuilder;
class LoaderAllocator;
class ComCallWrapperTemplate;
enum class ParseNativeTypeFlags : int;

typedef DPTR(DictionaryLayout) PTR_DictionaryLayout;
typedef DPTR(NativeFieldDescriptor) PTR_NativeFieldDescriptor;
typedef DPTR(NativeFieldDescriptor const) PTR_ConstNativeFieldDescriptor;
typedef DPTR(EEClassNativeLayoutInfo) PTR_EEClassNativeLayoutInfo;


//---------------------------------------------------------------------------------
// Fields in an explicit-layout class present varying degrees of risk depending
// on how they overlap.
//
// Each level is a superset of the lower (in numerical value) level - i.e.
// all kVerifiable fields are also kLegal, but not vice-versa.
//---------------------------------------------------------------------------------
class ExplicitFieldTrust
{
    public:
        enum TrustLevel
        {
            // Note: order is important here - each guarantee also implicitly guarantees all promises
            // made by values lower in number.

            //                       What's guaranteed.                                                  What the loader does.
            //-----                  -----------------------                                             -------------------------------
            kNone         = 0,    // no guarantees at all                                              - Type refuses to load at all.
            kLegal        = 1,    // guarantees no objref <-> scalar overlap and no unaligned objref   - Type loads but field access won't verify
            kVerifiable   = 2,    // guarantees no objref <-> objref overlap and all guarantees above  - Type loads and field access will verify
            kNonOverLayed = 3,    // guarantees no overlap at all and all guarantees above             - Type loads, field access verifies and Equals() may be optimized if structure is tightly packed

            kMaxTrust     = kNonOverLayed,
        };

};

//----------------------------------------------------------------------------------------------
// This class is a helper for HandleExplicitLayout. To make it harder to introduce security holes
// into this function, we will manage all updates to the class's trust level through the ExplicitClassTrust
// class. This abstraction enforces the rule that the overall class is only as trustworthy as
// the least trustworthy field.
//----------------------------------------------------------------------------------------------
class ExplicitClassTrust : private ExplicitFieldTrust
{
    public:
        ExplicitClassTrust()
        {
            LIMITED_METHOD_CONTRACT;
            m_trust = kMaxTrust;   // Yes, we start out with maximal trust. This reflects that explicit layout structures with no fields do represent no risk.
        }

        VOID AddField(TrustLevel fieldTrust)
        {
            LIMITED_METHOD_CONTRACT;
            m_trust = min(m_trust, fieldTrust);
        }

        BOOL IsLegal()
        {
            LIMITED_METHOD_CONTRACT;
            return m_trust >= kLegal;
        }

        BOOL IsVerifiable()
        {
            LIMITED_METHOD_CONTRACT;
            return m_trust >= kVerifiable;
        }

        BOOL IsNonOverLayed()
        {
            LIMITED_METHOD_CONTRACT;
            return m_trust >= kNonOverLayed;
        }

        TrustLevel GetTrustLevel()
        {
            LIMITED_METHOD_CONTRACT;
            return m_trust;
        }

    private:
        TrustLevel      m_trust;
};

//----------------------------------------------------------------------------------------------
// This class is a helper for HandleExplicitLayout. To make it harder to introduce security holes
// into this function, this class will collect trust information about individual fields to be later
// aggregated into the overall class level.
//
// This abstraction enforces the rule that all fields are presumed guilty until explicitly declared
// safe by calling SetTrust(). If you fail to call SetTrust before leaving the block, the destructor
// will automatically cause the entire class to be declared illegal (and you will get an assert
// telling you to fix this bug.)
//----------------------------------------------------------------------------------------------
class ExplicitFieldTrustHolder : private ExplicitFieldTrust
{
    public:
        ExplicitFieldTrustHolder(ExplicitClassTrust *pExplicitClassTrust)
        {
            LIMITED_METHOD_CONTRACT;
            m_pExplicitClassTrust = pExplicitClassTrust;
#ifdef _DEBUG
            m_trustDeclared       = FALSE;
#endif
            m_fieldTrust          = kNone;
        }

        VOID SetTrust(TrustLevel fieldTrust)
        {
            LIMITED_METHOD_CONTRACT;

            _ASSERTE(fieldTrust >= kNone && fieldTrust <= kMaxTrust);
            _ASSERTE(!m_trustDeclared && "You should not set the trust value more than once.");

#ifdef _DEBUG
            m_trustDeclared = TRUE;
#endif
            m_fieldTrust = fieldTrust;
        }

        ~ExplicitFieldTrustHolder()
        {
            LIMITED_METHOD_CONTRACT;
            // If no SetTrust() was ever called, we will default to kNone (i.e. declare the entire type
            // illegal.) It'd be nice to assert here but since this case can be legitimately reached
            // on exception unwind, we cannot.
            m_pExplicitClassTrust->AddField(m_fieldTrust);
        }


    private:
        ExplicitClassTrust* m_pExplicitClassTrust;
        TrustLevel          m_fieldTrust;
#ifdef _DEBUG
        BOOL                m_trustDeclared;                // Debug flag to detect multiple Sets. (Which we treat as a bug as this shouldn't be necessary.)
#endif
};

//*******************************************************************************
// Enumerator to traverse the interface declarations of a type, automatically building
// a substitution chain on the stack.
class InterfaceImplEnum
{
    Module* m_pModule;
    HENUMInternalHolder   hEnumInterfaceImpl;
    const Substitution *m_pSubstChain;
    Substitution m_CurrSubst;
    mdTypeDef m_CurrTok;
public:
    InterfaceImplEnum(Module *pModule, mdTypeDef cl, const Substitution *pSubstChain)
        : hEnumInterfaceImpl(pModule->GetMDImport())
    {
        WRAPPER_NO_CONTRACT;
        m_pModule = pModule;
        hEnumInterfaceImpl.EnumInit(mdtInterfaceImpl, cl);
        m_pSubstChain = pSubstChain;
    }

    // Returns:
    // S_OK ... if has next (TRUE)
    // S_FALSE ... if does not have next (FALSE)
    // error code.
    HRESULT Next()
    {
        WRAPPER_NO_CONTRACT;
        HRESULT hr;
        mdInterfaceImpl ii;
        if (!m_pModule->GetMDImport()->EnumNext(&hEnumInterfaceImpl, &ii))
        {
            return S_FALSE;
        }

        IfFailRet(m_pModule->GetMDImport()->GetTypeOfInterfaceImpl(ii, &m_CurrTok));
        m_CurrSubst = Substitution(m_CurrTok, m_pModule, m_pSubstChain);
        return S_OK;
    }
    const Substitution *CurrentSubst() const { LIMITED_METHOD_CONTRACT; return &m_CurrSubst; }
    mdTypeDef CurrentToken() const { LIMITED_METHOD_CONTRACT; return m_CurrTok; }
};

#ifdef FEATURE_COMINTEROP
//
// Class used to map MethodTable slot numbers to COM vtable slots numbers
// (either for calling a classic COM component or for constructing a classic COM
// vtable via which COM components can call managed classes). This structure is
// embedded in the EEClass but the mapping list itself is only allocated if the
// COM vtable is sparse.
//

class SparseVTableMap
{
public:

    SparseVTableMap();
    ~SparseVTableMap();

    // First run through MT slots calling RecordGap wherever a gap in VT slots
    // occurs.
    void RecordGap(WORD StartMTSlot, WORD NumSkipSlots);

    // Then call FinalizeMapping to create the actual mapping list.
    void FinalizeMapping(WORD TotalMTSlots);

    // Map MT to VT slot.
    WORD LookupVTSlot(WORD MTSlot);

    // Retrieve the number of slots in the vtable (both empty and full).
    WORD GetNumVTableSlots();

    const void* GetMapList()
    {
        LIMITED_METHOD_CONTRACT;
        return (void*)m_MapList;
    }

private:

    enum { MapGrow = 4 };

    struct Entry
    {
        WORD    m_Start;        // Starting MT slot number
        WORD    m_Span;         // # of consecutive slots that map linearly
        WORD    m_MapTo;        // Starting VT slot number
    };

    Entry      *m_MapList;      // Pointer to array of Entry structures
    WORD        m_MapEntries;   // Number of entries in above
    WORD        m_Allocated;    // Number of entries allocated

    WORD        m_LastUsed;     // Index of last entry used in successful lookup

    WORD        m_VTSlot;       // Current VT slot number, used during list build
    WORD        m_MTSlot;       // Current MT slot number, used during list build

    void AllocOrExpand();       // Allocate or expand the mapping list for a new entry
};
#endif // FEATURE_COMINTEROP

//=======================================================================
// Adjunct to the EEClass structure for classes w/ layout
//=======================================================================
class EEClassLayoutInfo
{
    static VOID CollectLayoutFieldMetadataThrowing(
       mdTypeDef cl,                // cl of the NStruct being loaded
       BYTE packingSize,            // packing size (from @dll.struct)
       BYTE nlType,                 // nltype (from @dll.struct)
       BOOL fExplicitOffsets,       // explicit offsets?
       MethodTable *pParentMT,       // the loaded superclass
       ULONG cTotalFields,              // total number of fields (instance and static)
       HENUMInternal *phEnumField,  // enumerator for fields
       Module* pModule,             // Module that defines the scope, loader and heap (for allocate FieldMarshalers)
       const SigTypeContext *pTypeContext,          // Type parameters for NStruct being loaded
       EEClassLayoutInfo *pEEClassLayoutInfoOut,  // caller-allocated structure to fill in.
       LayoutRawFieldInfo *pInfoArrayOut, // caller-allocated array to fill in.  Needs room for cTotalFields+1 elements
       LoaderAllocator * pAllocator,
       AllocMemTracker    *pamTracker
    );

    friend class ClassLoader;
    friend class EEClass;
    friend class MethodTableBuilder;
        UINT32      m_cbManagedSize;

    public:
        BYTE        m_ManagedLargestAlignmentRequirementOfAllMembers;

    private:
        enum {
            // TRUE if the GC layout of the class is bit-for-bit identical
            // to its unmanaged counterpart (i.e. no internal reference fields,
            // no ansi-unicode char conversions required, etc.) Used to
            // optimize marshaling.
            e_BLITTABLE                 = 0x01,
            // Post V1.0 addition: Is this type also sequential in managed memory?
            e_MANAGED_SEQUENTIAL        = 0x02,
            // When a sequential/explicit type has no fields, it is conceptually
            // zero-sized, but actually is 1 byte in length. This holds onto this
            // fact and allows us to revert the 1 byte of padding when another
            // explicit type inherits from this type.
            e_ZERO_SIZED                =   0x04,
            // The size of the struct is explicitly specified in the meta-data.
            e_HAS_EXPLICIT_SIZE         = 0x08
        };

        BYTE        m_bFlags;

        // Packing size in bytes (1, 2, 4, 8 etc.)
        BYTE        m_cbPackingSize;

    public:
        UINT32 GetManagedSize() const
        {
            LIMITED_METHOD_CONTRACT;
            return m_cbManagedSize;
        }

        BOOL IsBlittable() const
        {
            LIMITED_METHOD_CONTRACT;
            return (m_bFlags & e_BLITTABLE) == e_BLITTABLE;
        }

        BOOL IsManagedSequential() const
        {
            LIMITED_METHOD_CONTRACT;
            return (m_bFlags & e_MANAGED_SEQUENTIAL) == e_MANAGED_SEQUENTIAL;
        }

        // If true, this says that the type was originally zero-sized
        // and the native size was bumped up to one for similar behaviour
        // to C++ structs. However, it is necessary to keep track of this
        // so that we can ignore the one byte padding if other types derive
        // from this type, that we can
        BOOL IsZeroSized() const
        {
            LIMITED_METHOD_CONTRACT;
            return (m_bFlags & e_ZERO_SIZED) == e_ZERO_SIZED;
        }

        BOOL HasExplicitSize() const
        {
            LIMITED_METHOD_CONTRACT;
            return (m_bFlags & e_HAS_EXPLICIT_SIZE) == e_HAS_EXPLICIT_SIZE;
        }

        BYTE GetPackingSize() const
        {
            LIMITED_METHOD_CONTRACT;
            return m_cbPackingSize;
        }

    private:
        void SetIsBlittable(BOOL isBlittable)
        {
            LIMITED_METHOD_CONTRACT;
            m_bFlags = isBlittable ? (m_bFlags | e_BLITTABLE)
                                   : (m_bFlags & ~e_BLITTABLE);
        }

        void SetIsManagedSequential(BOOL isManagedSequential)
        {
            LIMITED_METHOD_CONTRACT;
            m_bFlags = isManagedSequential ? (m_bFlags | e_MANAGED_SEQUENTIAL)
                                           : (m_bFlags & ~e_MANAGED_SEQUENTIAL);
        }

        void SetIsZeroSized(BOOL isZeroSized)
        {
            LIMITED_METHOD_CONTRACT;
            m_bFlags = isZeroSized ? (m_bFlags | e_ZERO_SIZED)
                                   : (m_bFlags & ~e_ZERO_SIZED);
        }

        void SetHasExplicitSize(BOOL hasExplicitSize)
        {
            LIMITED_METHOD_CONTRACT;
            m_bFlags = hasExplicitSize ? (m_bFlags | e_HAS_EXPLICIT_SIZE)
                                       : (m_bFlags & ~e_HAS_EXPLICIT_SIZE);
        }
};

//
// This structure is used only when the classloader is building the interface map.  Before the class
// is resolved, the EEClass contains an array of these, which are all interfaces *directly* declared
// for this class/interface by the metadata - inherited interfaces will not be present if they are
// not specifically declared.
//
// This structure is destroyed after resolving has completed.
//
typedef struct
{
    // The interface method table; for instantiated interfaces, this is the generic interface
    MethodTable     *m_pMethodTable;
} BuildingInterfaceInfo_t;


//
// We should not need to touch anything in here once the classes are all loaded, unless we
// are doing reflection.  Try to avoid paging this data structure in.
//

// Size of hash bitmap for method names
#define METHOD_HASH_BYTES  8

// Hash table size - prime number
#define METHOD_HASH_BITS    61


// These are some macros for forming fully qualified class names for a class.
// These are abstracted so that we can decide later if a max length for a
// class name is acceptable.

// It doesn't make any sense not to have a small but usually quite capable
// stack buffer to build class names into. Most class names that I can think
// of would fit in 128 characters, and that's a pretty small amount of stack
// to use in exchange for not having to new and delete the memory.
#define DEFAULT_NONSTACK_CLASSNAME_SIZE (MAX_CLASSNAME_LENGTH/4)

#define DefineFullyQualifiedNameForClass() \
    ScratchBuffer<DEFAULT_NONSTACK_CLASSNAME_SIZE> _scratchbuffer_; \
    InlineSString<DEFAULT_NONSTACK_CLASSNAME_SIZE> _ssclsname_;

#define DefineFullyQualifiedNameForClassOnStack() \
    ScratchBuffer<MAX_CLASSNAME_LENGTH> _scratchbuffer_; \
    InlineSString<MAX_CLASSNAME_LENGTH> _ssclsname_;

#define DefineFullyQualifiedNameForClassW() \
    InlineSString<DEFAULT_NONSTACK_CLASSNAME_SIZE> _ssclsname_w_;

#define DefineFullyQualifiedNameForClassWOnStack() \
    InlineSString<MAX_CLASSNAME_LENGTH> _ssclsname_w_;

#define GetFullyQualifiedNameForClassNestedAware(pClass) \
    pClass->_GetFullyQualifiedNameForClassNestedAware(_ssclsname_).GetUTF8(_scratchbuffer_)

#define GetFullyQualifiedNameForClassNestedAwareW(pClass) \
    pClass->_GetFullyQualifiedNameForClassNestedAware(_ssclsname_w_).GetUnicode()

#define GetFullyQualifiedNameForClass(pClass) \
    pClass->_GetFullyQualifiedNameForClass(_ssclsname_).GetUTF8(_scratchbuffer_)

#define GetFullyQualifiedNameForClassW(pClass) \
    pClass->_GetFullyQualifiedNameForClass(_ssclsname_w_).GetUnicode()

// Structure containing EEClass fields used by a minority of EEClass instances. This separation allows us to
// save memory and improve the density of accessed fields in the EEClasses themselves. This class is reached
// via the m_rpOptionalFields field EEClass (use the GetOptionalFields() accessor rather than the field
// itself).
class EEClassOptionalFields
{
    // All fields here are intentionally private. Use the corresponding accessor on EEClass instead (this
    // makes it easier to add and remove fields from the optional section in the future). We make exceptions
    // for MethodTableBuilder and NativeImageDumper, which need raw field-level access.
    friend class EEClass;
    friend class MethodTableBuilder;

    //
    // GENERICS RELATED FIELDS.
    //

    // If IsSharedByGenericInstantiations(), layout of handle dictionary for generic type
    // (the last dictionary pointed to from PerInstInfo). Otherwise NULL.
    PTR_DictionaryLayout m_pDictLayout;

    // Variance info for each type parameter (gpNonVariant, gpCovariant, or gpContravariant)
    // If NULL, this type has no type parameters that are co/contravariant
    PTR_BYTE m_pVarianceInfo;

    //
    // COM RELATED FIELDS.
    //

#ifdef FEATURE_COMINTEROP
    SparseVTableMap *m_pSparseVTableMap;

    TypeHandle m_pCoClassForIntf;  // @TODO: Coclass for an interface

#ifdef FEATURE_COMINTEROP_UNMANAGED_ACTIVATION
    // Points to activation information if the type is an activatable COM class.
    ClassFactoryBase *m_pClassFactory;
#endif // FEATURE_COMINTEROP_UNMANAGED_ACTIVATION

#endif // FEATURE_COMINTEROP

    //
    // MISC FIELDS
    //

    #define    MODULE_NON_DYNAMIC_STATICS      ((DWORD)-1)
    DWORD m_cbModuleDynamicID;

#if defined(UNIX_AMD64_ABI)
    // Number of eightBytes in the following arrays
    int m_numberEightBytes;
    // Classification of the eightBytes
    SystemVClassificationType m_eightByteClassifications[CLR_SYSTEMV_MAX_EIGHTBYTES_COUNT_TO_PASS_IN_REGISTERS];
    // Size of data the eightBytes
    unsigned int m_eightByteSizes[CLR_SYSTEMV_MAX_EIGHTBYTES_COUNT_TO_PASS_IN_REGISTERS];
#endif // UNIX_AMD64_ABI

    // Set default values for optional fields.
    inline void Init();

    PTR_BYTE GetVarianceInfo()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return m_pVarianceInfo;
    }
};
typedef DPTR(EEClassOptionalFields) PTR_EEClassOptionalFields;

//
// Another mechanism used to reduce the size of the average EEClass instance is the notion of packed fields.
// This is based on the observation that EEClass has a large number of integer fields that typically contain
// small values and that are fixed once class layout has completed. We can compact these fields by discarding
// the leading zero bits (and for small values there'll be a lot of these) and packing the significant data
// into compact bitfields. This is a dynamic operation (the exact packing used depends on the exact data
// stored in the fields).
//
// The PackedDWORDFields<> class (defined in PackedFields.inl) encapsulates this. It takes one template
// parameter, the number of fields to pack, and provides operations to get and set those fields until we're
// happy with the values, at which point it will compact them for us.
//
// The packed fields themselves are stored at the end of the EEClass instance (or the LayoutEEClass or the
// DelegateEEClass etc.) so we can take advantage of the variable sized nature of the fields. We gain nothing for
// runtime allocated EEClasses (we have to allocate a maximally sized structure for the packed fields because
// we can't tell at the beginning of EEClass layout what the field values will be). But in the ngen scenario
// we can compact the fields just prior to saving and only store the portion of the EEClass that is relvant,
// helping us with our goal of packing all the EEClass instances together as tightly as possible.
//
// Since each packed field is now accessed via an array-like index, we give each of those indices a name with
// the enum below to make the code more readable.
//

enum EEClassFieldId
{
    EEClass_Field_NumInstanceFields = 0,
    EEClass_Field_NumMethods,
    EEClass_Field_NumStaticFields,
    EEClass_Field_NumHandleStatics,
    EEClass_Field_NumBoxedStatics,
    EEClass_Field_NonGCStaticFieldBytes,
    EEClass_Field_NumThreadStaticFields,
    EEClass_Field_NumHandleThreadStatics,
    EEClass_Field_NumBoxedThreadStatics,
    EEClass_Field_NonGCThreadStaticFieldBytes,
    EEClass_Field_NumNonVirtualSlots,
    EEClass_Field_COUNT
};

typedef PackedDWORDFields<EEClass_Field_COUNT> EEClassPackedFields;
typedef DPTR(EEClassPackedFields) PTR_EEClassPackedFields;

//@GENERICS:
// For most types there is a one-to-one mapping between MethodTable* and EEClass*
// However this is not the case for instantiated types where code and representation
// are shared between compatible instantiations (e.g. List<string> and List<object>)
// Then a single EEClass structure is shared between multiple MethodTable structures
// Uninstantiated generic types (e.g. List) have their own EEClass and MethodTable,
// used (a) as a representative for the generic type itself, (b) for static fields and
// methods, which aren't present in the instantiations, and (c) to hold some information
// (e.g. formal instantiations of superclass and implemented interfaces) that is common
// to all instantiations and isn't stored in the EEClass structures for instantiated types
//
//
// **  NOTE  **  NOTE  **  NOTE  **  NOTE  **  NOTE  **  NOTE  **  NOTE  **  NOTE
//
// A word about EEClass vs. MethodTable
// ------------------------------------
//
// At compile-time, we are happy to touch both MethodTable and EEClass.  However,
// at runtime we want to restrict ourselves to the MethodTable.  This is critical
// for common code paths, where we want to keep the EEClass out of our working
// set.  For uncommon code paths, like throwing exceptions or strange Contexts
// issues, it's okay to access the EEClass.
//
// To this end, the TypeHandle (CLASS_HANDLE) abstraction is now based on the
// MethodTable pointer instead of the EEClass pointer.  If you are writing a
// runtime helper that calls GetClass() to access the associated EEClass, please
// stop to wonder if you are making a mistake.
//
// **  NOTE  **  NOTE  **  NOTE  **  NOTE  **  NOTE  **  NOTE  **  NOTE  **  NOTE


// An code:EEClass is a representation of the part of a managed type that is not used very frequently (it is
// cold), and thus is segregated from the hot portion (which lives in code:MethodTable).  As noted above an
// it is also the case that EEClass is SHARED among all instantiations of a generic type, so anything that
// is specific to a paritcular type can not live off the EEClass.
//
// From here you can get to
//     code:MethodTable - The representation of the hot portion of a type.
//     code:MethodDesc - The representation of a method
//     code:FieldDesc - The representation of a field.
//
// EEClasses hold the following important fields
//     * code:EEClass.m_pMethodTable - Points a MethodTable associated with
//     * code:EEClass.m_pChunks - a list of code:MethodDescChunk which is simply a list of code:MethodDesc
//         which represent the methods.
//     * code:EEClass.m_pFieldDescList - a list of fields in the type.
//
class EEClass // DO NOT CREATE A NEW EEClass USING NEW!
{
    /************************************
     *  FRIEND FUNCTIONS
     ************************************/
    // DO NOT ADD FRIENDS UNLESS ABSOLUTELY NECESSARY
    // USE ACCESSORS TO READ/WRITE private field members

    // To access bmt stuff
    friend class MethodTable;
    friend class MethodTableBuilder;
    friend class FieldDesc;
    friend class CheckAsmOffsets;
    friend class ClrDataAccess;

    /************************************
     *  PUBLIC INSTANCE METHODS
     ************************************/
public:

    DWORD  IsSealed()
    {
        LIMITED_METHOD_CONTRACT;
        return IsTdSealed(m_dwAttrClass);
    }

    inline DWORD IsInterface()
    {
        WRAPPER_NO_CONTRACT;
        return IsTdInterface(m_dwAttrClass);
    }

    inline DWORD IsAbstract()
    {
        WRAPPER_NO_CONTRACT;
        return IsTdAbstract(m_dwAttrClass);
    }

    BOOL HasExplicitFieldOffsetLayout()
    {
        WRAPPER_NO_CONTRACT;
        return IsTdExplicitLayout(GetAttrClass()) && HasLayout();
    }

    BOOL HasSequentialLayout()
    {
        WRAPPER_NO_CONTRACT;
        return IsTdSequentialLayout(GetAttrClass());
    }
    BOOL IsBeforeFieldInit()
    {
        WRAPPER_NO_CONTRACT;
        return IsTdBeforeFieldInit(GetAttrClass());
    }

    DWORD GetProtection()
    {
        WRAPPER_NO_CONTRACT;
        return (m_dwAttrClass & tdVisibilityMask);
    }

    // class is blittable
    BOOL IsBlittable();

#ifndef DACCESS_COMPILE
    void *operator new(size_t size, LoaderHeap* pHeap, AllocMemTracker *pamTracker);
    void Destruct(MethodTable * pMT);

    static EEClass * CreateMinimalClass(LoaderHeap *pHeap, AllocMemTracker *pamTracker);
#endif // !DACCESS_COMPILE

#ifdef EnC_SUPPORTED
    // Add a new method to an already loaded type for EnC
    static HRESULT AddMethod(MethodTable * pMT, mdMethodDef methodDef, RVA newRVA, MethodDesc **ppMethod);

    // Add a new field to an already loaded type for EnC
    static HRESULT AddField(MethodTable * pMT, mdFieldDef fieldDesc, EnCFieldDesc **pAddedField);
    static VOID    FixupFieldDescForEnC(MethodTable * pMT, EnCFieldDesc *pFD, mdFieldDef fieldDef);
#endif // EnC_SUPPORTED

    inline DWORD IsComImport()
    {
        WRAPPER_NO_CONTRACT;
        return IsTdImport(m_dwAttrClass);
    }

    EEClassLayoutInfo *GetLayoutInfo();
    PTR_EEClassNativeLayoutInfo GetNativeLayoutInfo();

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags, MethodTable *pMT);
#endif

    /************************************
     *  INSTANCE MEMBER VARIABLES
     ************************************/
#ifdef _DEBUG
public:
    inline LPCUTF8 GetDebugClassName ()
    {
        LIMITED_METHOD_CONTRACT;
        return m_szDebugClassName;
    }
    inline void SetDebugClassName (LPCUTF8 szDebugClassName)
    {
        LIMITED_METHOD_CONTRACT;
        m_szDebugClassName = szDebugClassName;
    }

    /*
     * Controls debugging breaks and output if a method class
     * is mentioned in the registry ("BreakOnClassBuild")
     * Method layout within this class can cause a debug
     * break by setting "BreakOnMethodName". Not accessible
     * outside the class.
     */

#endif // _DEBUG

#ifdef FEATURE_COMINTEROP
    /*
     * Used to map MethodTable slots to VTable slots
     */
    inline SparseVTableMap* GetSparseCOMInteropVTableMap ()
    {
        LIMITED_METHOD_CONTRACT;
        return HasOptionalFields() ? GetOptionalFields()->m_pSparseVTableMap : NULL;
    }
    inline void SetSparseCOMInteropVTableMap (SparseVTableMap *map)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(HasOptionalFields());
        GetOptionalFields()->m_pSparseVTableMap = map;
    }
#endif // FEATURE_COMINTEROP

public:
    /*
     * Maintain back pointer to statcally hot portion of EEClass.
     * For an EEClass representing multiple instantiations of a generic type, this is the method table
     * for the first instantiation requested and is the only one containing entries for non-virtual instance methods
     * (i.e. non-vtable entries).
     */

    // Note that EEClass structures may be shared between generic instantiations
    // (see IsSharedByGenericInstantiations).  In these cases  EEClass::GetMethodTable
    // will return the method table pointer corresponding to the "canonical"
    // instantiation, as defined in typehandle.h.
    //
    inline PTR_MethodTable GetMethodTable()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;

        return m_pMethodTable;
    }

    // DO NOT ADD ANY ASSERTS TO THIS METHOD.
    // DO NOT USE THIS METHOD.
    // Yes folks, for better or worse the debugger pokes supposed object addresses
    // to try to see if objects are valid, possibly firing an AccessViolation or worse,
    // and then catches the AV and reports a failure to the debug client.  This makes
    // the debugger slightly more robust should any corrupted object references appear
    // in a session. Thus it is "correct" behaviour for this to AV when used with
    // an invalid object pointer, and incorrect behaviour for it to
    // assert.
    inline PTR_MethodTable GetMethodTableWithPossibleAV()
    {
        CANNOT_HAVE_CONTRACT;
        SUPPORTS_DAC;

        return m_pMethodTable;
    }

#ifndef DACCESS_COMPILE
    inline void SetMethodTable(MethodTable*  pMT)
    {
        LIMITED_METHOD_CONTRACT;
        m_pMethodTable = pMT;
    }
#endif // !DACCESS_COMPILE

    /*
     * Number of fields in the class, including inherited fields.
     * Does not include fields added from EnC.
     */
    inline WORD GetNumInstanceFields()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        return (WORD)GetPackableField(EEClass_Field_NumInstanceFields);
    }

    inline void SetNumInstanceFields (WORD wNumInstanceFields)
    {
        LIMITED_METHOD_CONTRACT;
        SetPackableField(EEClass_Field_NumInstanceFields, wNumInstanceFields);
    }

    /*
     * Number of static fields declared in this class.
     * Implementation Note: Static values are laid out at the end of the MethodTable vtable.
     */
    inline WORD GetNumStaticFields()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        return (WORD)GetPackableField(EEClass_Field_NumStaticFields);
    }
    inline void SetNumStaticFields (WORD wNumStaticFields)
    {
        LIMITED_METHOD_CONTRACT;
        SetPackableField(EEClass_Field_NumStaticFields, wNumStaticFields);
    }

    inline WORD GetNumThreadStaticFields()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        return (WORD)GetPackableField(EEClass_Field_NumThreadStaticFields);
    }

    inline void SetNumThreadStaticFields (WORD wNumThreadStaticFields)
    {
        LIMITED_METHOD_CONTRACT;
        SetPackableField(EEClass_Field_NumThreadStaticFields, wNumThreadStaticFields);
    }

    // Statics are stored in a big chunk inside the module

    inline  DWORD GetModuleDynamicID()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        return HasOptionalFields() ? GetOptionalFields()->m_cbModuleDynamicID : MODULE_NON_DYNAMIC_STATICS;
    }

    inline void SetModuleDynamicID(DWORD cbModuleDynamicID)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(HasOptionalFields());
        GetOptionalFields()->m_cbModuleDynamicID = cbModuleDynamicID;
    }

    /*
     * Difference between the InterfaceMap ptr and Vtable in the
     * MethodTable used to indicate the number of static bytes
     * Now interfaceMap ptr can be optional hence we store it here
     */
    inline DWORD GetNonGCRegularStaticFieldBytes()
    {
        LIMITED_METHOD_CONTRACT;
        return GetPackableField(EEClass_Field_NonGCStaticFieldBytes);
    }
    inline void SetNonGCRegularStaticFieldBytes (DWORD cbNonGCStaticFieldBytes)
    {
        LIMITED_METHOD_CONTRACT;
        SetPackableField(EEClass_Field_NonGCStaticFieldBytes, cbNonGCStaticFieldBytes);
    }

    inline DWORD GetNonGCThreadStaticFieldBytes()
    {
        LIMITED_METHOD_CONTRACT;
        return GetPackableField(EEClass_Field_NonGCThreadStaticFieldBytes);
    }
    inline void SetNonGCThreadStaticFieldBytes (DWORD cbNonGCStaticFieldBytes)
    {
        LIMITED_METHOD_CONTRACT;
        SetPackableField(EEClass_Field_NonGCThreadStaticFieldBytes, cbNonGCStaticFieldBytes);
    }

    inline WORD GetNumNonVirtualSlots()
    {
        LIMITED_METHOD_CONTRACT;
        return (WORD)GetPackableField(EEClass_Field_NumNonVirtualSlots);
    }
    inline void SetNumNonVirtualSlots(WORD wNumNonVirtualSlots)
    {
        LIMITED_METHOD_CONTRACT;
        SetPackableField(EEClass_Field_NumNonVirtualSlots, wNumNonVirtualSlots);
    }

    inline BOOL IsEquivalentType()
    {
        LIMITED_METHOD_CONTRACT;
        return m_VMFlags & VMFLAG_IS_EQUIVALENT_TYPE;
    }

#ifdef FEATURE_TYPEEQUIVALENCE
    inline void SetIsEquivalentType()
    {
        LIMITED_METHOD_CONTRACT;
        m_VMFlags |= VMFLAG_IS_EQUIVALENT_TYPE;
    }
#endif // FEATURE_TYPEEQUIVALENCE

    /*
     * Number of static handles allocated
     */
    inline WORD GetNumHandleRegularStatics ()
    {
        LIMITED_METHOD_CONTRACT;
        return (WORD)GetPackableField(EEClass_Field_NumHandleStatics);
    }
    inline void SetNumHandleRegularStatics (WORD wNumHandleRegularStatics)
    {
        LIMITED_METHOD_CONTRACT;
        SetPackableField(EEClass_Field_NumHandleStatics, wNumHandleRegularStatics);
    }

    /*
     * Number of static handles allocated for ThreadStatics
     */
    inline WORD GetNumHandleThreadStatics ()
    {
        LIMITED_METHOD_CONTRACT;
        return (WORD)GetPackableField(EEClass_Field_NumHandleThreadStatics);
    }
    inline void SetNumHandleThreadStatics (WORD wNumHandleThreadStatics)
    {
        LIMITED_METHOD_CONTRACT;
        SetPackableField(EEClass_Field_NumHandleThreadStatics, wNumHandleThreadStatics);
    }

    /*
     * Number of boxed statics allocated
     */
    inline WORD GetNumBoxedRegularStatics ()
    {
        LIMITED_METHOD_CONTRACT;
        return (WORD)GetPackableField(EEClass_Field_NumBoxedStatics);
    }
    inline void SetNumBoxedRegularStatics (WORD wNumBoxedRegularStatics)
    {
        LIMITED_METHOD_CONTRACT;
        SetPackableField(EEClass_Field_NumBoxedStatics, wNumBoxedRegularStatics);
    }

    /*
     * Number of boxed statics allocated for ThreadStatics
     */
    inline WORD GetNumBoxedThreadStatics ()
    {
        LIMITED_METHOD_CONTRACT;
        return (WORD)GetPackableField(EEClass_Field_NumBoxedThreadStatics);
    }
    inline void SetNumBoxedThreadStatics (WORD wNumBoxedThreadStatics)
    {
        LIMITED_METHOD_CONTRACT;
        SetPackableField(EEClass_Field_NumBoxedThreadStatics, wNumBoxedThreadStatics);
    }

    /*
     * Number of bytes to subract from code:MethodTable::GetBaseSize() to get the actual number of bytes
     * of instance fields stored in the object on the GC heap.
     */
    inline DWORD GetBaseSizePadding()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_cbBaseSizePadding;
    }
    inline void SetBaseSizePadding(DWORD dwPadding)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(FitsIn<BYTE>(dwPadding));
        m_cbBaseSizePadding = static_cast<BYTE>(dwPadding);
    }

    inline DWORD GetUnboxedNumInstanceFieldBytes()
    {
        DWORD cbBoxedSize = GetMethodTable()->GetNumInstanceFieldBytes();

        _ASSERTE(GetMethodTable()->IsValueType() || GetMethodTable()->IsEnum());
        return cbBoxedSize;
    }


    /*
     * Pointer to a list of FieldDescs declared in this class
     * There are (m_wNumInstanceFields - GetParentClass()->m_wNumInstanceFields + m_wNumStaticFields) entries
     * in this array
     */
    inline PTR_FieldDesc GetFieldDescList()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        // Careful about using this method. If it's possible that fields may have been added via EnC, then
        // must use the FieldDescIterator as any fields added via EnC won't be in the raw list
        return m_pFieldDescList;
    }

    PTR_FieldDesc GetFieldDescByIndex(DWORD fieldIndex);

#ifndef DACCESS_COMPILE
    inline void SetFieldDescList (FieldDesc* pFieldDescList)
    {
        LIMITED_METHOD_CONTRACT;
        m_pFieldDescList = pFieldDescList;
    }
#endif // !DACCESS_COMPILE

    inline WORD GetNumMethods()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return (WORD)GetPackableField(EEClass_Field_NumMethods);
    }
    inline void SetNumMethods (WORD wNumMethods)
    {
        LIMITED_METHOD_CONTRACT;
        SetPackableField(EEClass_Field_NumMethods, wNumMethods);
    }

    /*
     * Cached metadata for this class (GetTypeDefProps)
     */
    inline DWORD GetAttrClass()
    {
        LIMITED_METHOD_CONTRACT;
        return m_dwAttrClass;
    }
    inline void SetAttrClass (DWORD dwAttrClass)
    {
        LIMITED_METHOD_CONTRACT;
        m_dwAttrClass = dwAttrClass;
    }


#ifdef FEATURE_COMINTEROP
    inline DWORD IsComClassInterface()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_VMFlags & VMFLAG_HASCOCLASSATTRIB);
    }
    inline VOID SetIsComClassInterface()
    {
        LIMITED_METHOD_CONTRACT;
        m_VMFlags |= VMFLAG_HASCOCLASSATTRIB;
    }
    inline void SetComEventItfType()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(IsInterface());
        m_VMFlags |= VMFLAG_COMEVENTITFMASK;
    }
    // class is a special COM event interface
    inline BOOL IsComEventItfType()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_VMFlags & VMFLAG_COMEVENTITFMASK);
    }
#endif // FEATURE_COMINTEROP

    inline void SetHasVTableMethodImpl()
    {
        LIMITED_METHOD_CONTRACT;
        m_VMFlags |= VMFLAG_VTABLEMETHODIMPL;
    }

    inline BOOL HasVTableMethodImpl()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_VMFlags & VMFLAG_VTABLEMETHODIMPL);
    }

    inline void SetHasCovariantOverride()
    {
        LIMITED_METHOD_CONTRACT;
        m_VMFlags |= VMFLAG_COVARIANTOVERRIDE;
    }

    inline BOOL HasCovariantOverride()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_VMFlags & VMFLAG_COVARIANTOVERRIDE);
    }

#ifdef _DEBUG
    inline DWORD IsDestroyed()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_wAuxFlags & AUXFLAG_DESTROYED);
    }
#endif

    inline DWORD IsUnsafeValueClass()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_VMFlags & VMFLAG_UNSAFEVALUETYPE);
    }


private:
    inline void SetUnsafeValueClass()
    {
        LIMITED_METHOD_CONTRACT;
        m_VMFlags |= VMFLAG_UNSAFEVALUETYPE;
    }

public:
    inline BOOL HasNoGuid()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_VMFlags & VMFLAG_NO_GUID);
    }
    inline void SetHasNoGuid()
    {
        WRAPPER_NO_CONTRACT;
        FastInterlockOr(&m_VMFlags, VMFLAG_NO_GUID);
    }

public:
    inline BOOL IsAlign8Candidate()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_VMFlags & VMFLAG_PREFER_ALIGN8);
    }
    inline void SetAlign8Candidate()
    {
        LIMITED_METHOD_CONTRACT;
        m_VMFlags |= VMFLAG_PREFER_ALIGN8;
    }
#ifdef _DEBUG
    inline void SetDestroyed()
    {
        LIMITED_METHOD_CONTRACT;
        m_wAuxFlags |= AUXFLAG_DESTROYED;
    }
#endif
    inline void SetHasFixedAddressVTStatics()
    {
        LIMITED_METHOD_CONTRACT;
        m_VMFlags |= (DWORD) VMFLAG_FIXED_ADDRESS_VT_STATICS;
    }
    void SetHasOnlyAbstractMethods()
    {
        LIMITED_METHOD_CONTRACT;
        m_VMFlags |= (DWORD) VMFLAG_ONLY_ABSTRACT_METHODS;
    }
#ifdef FEATURE_COMINTEROP
    void SetSparseForCOMInterop()
    {
        LIMITED_METHOD_CONTRACT;
        m_VMFlags |= (DWORD) VMFLAG_SPARSE_FOR_COMINTEROP;
    }
    inline void SetMarshalingType(UINT32 mType)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(mType !=0);
        _ASSERTE((m_VMFlags & VMFLAG_MARSHALINGTYPE_MASK) == 0);
        switch(mType)
        {
        case 1: m_VMFlags |= VMFLAG_MARSHALINGTYPE_INHIBIT;
            break;
        case 2: m_VMFlags |= VMFLAG_MARSHALINGTYPE_FREETHREADED;
            break;
        case 3: m_VMFlags |= VMFLAG_MARSHALINGTYPE_STANDARD;
            break;
        default:
            _ASSERTE(!"Invalid MarshalingBehaviorAttribute value");
        }
    }
#endif // FEATURE_COMINTEROP
    inline void SetHasLayout()
    {
        LIMITED_METHOD_CONTRACT;
        m_VMFlags |= (DWORD) VMFLAG_HASLAYOUT;  //modified before the class is published
    }
    inline void SetHasOverLayedFields()
    {
        LIMITED_METHOD_CONTRACT;
        m_VMFlags |= VMFLAG_HASOVERLAYEDFIELDS;
    }
    inline void SetIsNested()
    {
        LIMITED_METHOD_CONTRACT;
        m_VMFlags |= VMFLAG_ISNESTED;
    }

#ifdef FEATURE_READYTORUN
    inline BOOL HasLayoutDependsOnOtherModules()
    {
        LIMITED_METHOD_CONTRACT;
        return m_VMFlags & VMFLAG_LAYOUT_DEPENDS_ON_OTHER_MODULES;
    }

    inline void SetHasLayoutDependsOnOtherModules()
    {
        LIMITED_METHOD_CONTRACT;
        m_VMFlags |= VMFLAG_LAYOUT_DEPENDS_ON_OTHER_MODULES;
    }
#endif

    // Is this delegate? Returns false for System.Delegate and System.MulticastDelegate.
    inline BOOL IsDelegate()
    {
        LIMITED_METHOD_CONTRACT;
        return m_VMFlags & VMFLAG_DELEGATE;
    }
    inline void SetIsDelegate()
    {
        LIMITED_METHOD_CONTRACT;
        m_VMFlags |= VMFLAG_DELEGATE;
    }

    BOOL HasFixedAddressVTStatics()
    {
        LIMITED_METHOD_CONTRACT;
        return m_VMFlags & VMFLAG_FIXED_ADDRESS_VT_STATICS;
    }

    BOOL HasOnlyAbstractMethods()
    {
        LIMITED_METHOD_CONTRACT;
        return m_VMFlags & VMFLAG_ONLY_ABSTRACT_METHODS;
    }

#ifdef FEATURE_COMINTEROP
    BOOL IsSparseForCOMInterop()
    {
        LIMITED_METHOD_CONTRACT;
        return m_VMFlags & VMFLAG_SPARSE_FOR_COMINTEROP;
    }
    BOOL IsMarshalingTypeSet()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_VMFlags & VMFLAG_MARSHALINGTYPE_MASK);
    }
    BOOL IsMarshalingTypeFreeThreaded()
    {
        LIMITED_METHOD_CONTRACT;
        return ((m_VMFlags & VMFLAG_MARSHALINGTYPE_MASK) == VMFLAG_MARSHALINGTYPE_FREETHREADED);
    }
    BOOL IsMarshalingTypeInhibit()
    {
        LIMITED_METHOD_CONTRACT;
        return ((m_VMFlags & VMFLAG_MARSHALINGTYPE_MASK) == VMFLAG_MARSHALINGTYPE_INHIBIT);
    }
    BOOL IsMarshalingTypeStandard()
    {
        LIMITED_METHOD_CONTRACT;
        return ((m_VMFlags & VMFLAG_MARSHALINGTYPE_MASK) == VMFLAG_MARSHALINGTYPE_STANDARD);
    }
#endif // FEATURE_COMINTEROP
    BOOL HasLayout()
    {
        LIMITED_METHOD_CONTRACT;
        return m_VMFlags & VMFLAG_HASLAYOUT;
    }
    BOOL HasOverLayedField()
    {
        LIMITED_METHOD_CONTRACT;
        return m_VMFlags & VMFLAG_HASOVERLAYEDFIELDS;
    }
    BOOL IsNested()
    {
        LIMITED_METHOD_CONTRACT;
        return m_VMFlags & VMFLAG_ISNESTED;
    }
    BOOL HasFieldsWhichMustBeInited()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_VMFlags & VMFLAG_HAS_FIELDS_WHICH_MUST_BE_INITED);
    }
    void SetHasFieldsWhichMustBeInited()
    {
        LIMITED_METHOD_CONTRACT;
        m_VMFlags |= (DWORD)VMFLAG_HAS_FIELDS_WHICH_MUST_BE_INITED;
    }
    void SetCannotBeBlittedByObjectCloner()
    {
        /* no op */
    }
    DWORD HasNonPublicFields()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_VMFlags & VMFLAG_HASNONPUBLICFIELDS);
    }
    void SetHasNonPublicFields()
    {
        LIMITED_METHOD_CONTRACT;
        m_VMFlags |= (DWORD)VMFLAG_HASNONPUBLICFIELDS;
    }
    DWORD IsNotTightlyPacked()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_VMFlags & VMFLAG_NOT_TIGHTLY_PACKED);
    }
    void SetIsNotTightlyPacked()
    {
        LIMITED_METHOD_CONTRACT;
        m_VMFlags |= (DWORD)VMFLAG_NOT_TIGHTLY_PACKED;
    }
    DWORD ContainsMethodImpls()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_VMFlags & VMFLAG_CONTAINS_METHODIMPLS);
    }
    void SetContainsMethodImpls()
    {
        LIMITED_METHOD_CONTRACT;
        m_VMFlags |= (DWORD)VMFLAG_CONTAINS_METHODIMPLS;
    }


    BOOL IsManagedSequential();

    BOOL HasExplicitSize();

    static void GetBestFitMapping(MethodTable * pMT, BOOL *pfBestFitMapping, BOOL *pfThrowOnUnmappableChar);

    /*
     * The CorElementType for this class (most classes = ELEMENT_TYPE_CLASS)
     */
public:
    // This is what would be used in the calling convention for this type.
    CorElementType  GetInternalCorElementType()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return CorElementType(m_NormType);
    }
    void SetInternalCorElementType (CorElementType _NormType)
    {
        LIMITED_METHOD_CONTRACT;
        m_NormType = static_cast<BYTE>(_NormType);
    }

    /*
     * Chain of MethodDesc chunks for the MethodTable
     */
public:
    inline PTR_MethodDescChunk GetChunks();

#ifndef DACCESS_COMPILE
    inline void SetChunks (MethodDescChunk* pChunks)
    {
        LIMITED_METHOD_CONTRACT;
        m_pChunks = pChunks;
    }
#endif // !DACCESS_COMPILE
    void AddChunk (MethodDescChunk* pNewChunk);

    void AddChunkIfItHasNotBeenAdded (MethodDescChunk* pNewChunk);

    inline PTR_GuidInfo GetGuidInfo()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return m_pGuidInfo;
    }

    inline void SetGuidInfo(GuidInfo* pGuidInfo)
    {
        WRAPPER_NO_CONTRACT;
        #ifndef DACCESS_COMPILE
        m_pGuidInfo = pGuidInfo;
        #endif // DACCESS_COMPILE
    }


#if defined(UNIX_AMD64_ABI)
    // Get number of eightbytes used by a struct passed in registers.
    inline int GetNumberEightBytes()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(HasOptionalFields());
        return GetOptionalFields()->m_numberEightBytes;
    }

    // Get eightbyte classification for the eightbyte with the specified index.
    inline SystemVClassificationType GetEightByteClassification(int index)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(HasOptionalFields());
        return GetOptionalFields()->m_eightByteClassifications[index];
    }

    // Get size of the data in the eightbyte with the specified index.
    inline unsigned int GetEightByteSize(int index)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(HasOptionalFields());
        return GetOptionalFields()->m_eightByteSizes[index];
    }

    // Set the eightByte classification
    inline void SetEightByteClassification(int eightByteCount, SystemVClassificationType *eightByteClassifications, unsigned int *eightByteSizes)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(HasOptionalFields());
        GetOptionalFields()->m_numberEightBytes = eightByteCount;
        for (int i = 0; i < eightByteCount; i++)
        {
            GetOptionalFields()->m_eightByteClassifications[i] = eightByteClassifications[i];
            GetOptionalFields()->m_eightByteSizes[i] = eightByteSizes[i];
        }
    }
#endif // UNIX_AMD64_ABI

#if defined(FEATURE_HFA)
    bool CheckForHFA(MethodTable ** pByValueClassCache);
#else // !FEATURE_HFA
    bool CheckForHFA();
#endif // FEATURE_HFA

#ifdef FEATURE_COMINTEROP
    inline TypeHandle GetCoClassForInterface()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(HasOptionalFields());
        return GetOptionalFields()->m_pCoClassForIntf;
    }

    inline void SetCoClassForInterface(TypeHandle th)
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(HasOptionalFields());
        GetOptionalFields()->m_pCoClassForIntf = th;
    }

    OBJECTHANDLE GetOHDelegate()
    {
        LIMITED_METHOD_CONTRACT;
        return m_ohDelegate;
    }
    void SetOHDelegate (OBJECTHANDLE _ohDelegate)
    {
        LIMITED_METHOD_CONTRACT;
        m_ohDelegate = _ohDelegate;
    }
    // Set the COM interface type.
    CorIfaceAttr GetComInterfaceType()
    {
        LIMITED_METHOD_CONTRACT;
        return m_ComInterfaceType;
    }

    void SetComInterfaceType(CorIfaceAttr ItfType)
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(IsInterface());
        m_ComInterfaceType = ItfType;
    }

    inline ComCallWrapperTemplate *GetComCallWrapperTemplate()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pccwTemplate;
    }
    inline BOOL SetComCallWrapperTemplate(ComCallWrapperTemplate *pTemplate)
    {
        WRAPPER_NO_CONTRACT;
        return (InterlockedCompareExchangeT(&m_pccwTemplate, pTemplate, NULL) == NULL);
    }

#ifdef FEATURE_COMINTEROP_UNMANAGED_ACTIVATION
    inline ClassFactoryBase *GetComClassFactory()
    {
        LIMITED_METHOD_CONTRACT;
        return HasOptionalFields() ? GetOptionalFields()->m_pClassFactory : NULL;
    }
    inline BOOL SetComClassFactory(ClassFactoryBase *pFactory)
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(HasOptionalFields());
        return (InterlockedCompareExchangeT(&GetOptionalFields()->m_pClassFactory, pFactory, NULL) == NULL);
    }
#endif // FEATURE_COMINTEROP_UNMANAGED_ACTIVATION
#endif // FEATURE_COMINTEROP


public:
    PTR_DictionaryLayout GetDictionaryLayout()
    {
        SUPPORTS_DAC;
        WRAPPER_NO_CONTRACT;
        return HasOptionalFields() ? GetOptionalFields()->m_pDictLayout : NULL;
    }

    void SetDictionaryLayout(PTR_DictionaryLayout pLayout)
    {
        SUPPORTS_DAC;
        WRAPPER_NO_CONTRACT;
        _ASSERTE(HasOptionalFields());
        GetOptionalFields()->m_pDictLayout = pLayout;
    }

#ifndef DACCESS_COMPILE
    static CorGenericParamAttr GetVarianceOfTypeParameter(BYTE * pbVarianceInfo, DWORD i)
    {
        LIMITED_METHOD_CONTRACT;
        if (pbVarianceInfo == NULL)
            return gpNonVariant;
        else
            return (CorGenericParamAttr) (pbVarianceInfo[i]);
    }

    CorGenericParamAttr GetVarianceOfTypeParameter(DWORD i)
    {
        WRAPPER_NO_CONTRACT;
        return GetVarianceOfTypeParameter(GetVarianceInfo(), i);
    }

    BYTE* GetVarianceInfo()
    {
        LIMITED_METHOD_CONTRACT;
        return HasOptionalFields() ? GetOptionalFields()->GetVarianceInfo() : NULL;
    }

    void SetVarianceInfo(BYTE *pVarianceInfo)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(HasOptionalFields());
        GetOptionalFields()->m_pVarianceInfo = pVarianceInfo;
    }
#endif // !DACCESS_COMPILE

    // Check that a signature blob uses type parameters correctly
    // in accordance with the variance annotations specified by this class
    // The position parameter indicates the variance of the context we're in
    // (result type is gpCovariant, argument types are gpContravariant, deeper in a signature
    // we might be gpNonvariant e.g. in a pointer type or non-variant generic type)
    static BOOL
    CheckVarianceInSig(
        DWORD numGenericArgs,
        BYTE *pVarianceInfo,
        Module * pModule,
        SigPointer sp,
        CorGenericParamAttr position);

#if defined(_DEBUG)
public:
    enum{
        AUXFLAG_DESTROYED = 0x00000008, // The Destruct() method has already been called on this class
    };
#endif // defined(_DEBUG)

    //-------------------------------------------------------------
    // CONCRETE DATA LAYOUT
    //
    // Although accessed far less frequently than MethodTables, EEClasses are still
    // pulled into working set, especially at startup.  This has motivated several space
    // optimizations in field layout where each is balanced against the need to access
    // a particular field efficiently.
    //
    // Currently, the following strategy is used:
    //
    //     - Any field that has a default value for the vast majority of EEClass instances
    //       should be stored in the EEClassOptionalFields (see header comment)
    //
    //     - Any field that is nearly always a small positive integer and is infrequently
    //       accessed should be in the EEClassPackedFields (see header comment)
    //
    // If none of these categories apply - such as for always-meaningful pointer members or
    // sets of flags - a full field is used.  Please avoid adding such members if possible.
    //-------------------------------------------------------------

    // @TODO: needed for asm code in cgenx86.cpp. Can this enum be private?
    //
    // Flags for m_VMFlags
    //
public:
    enum
    {
#ifdef FEATURE_READYTORUN
        VMFLAG_LAYOUT_DEPENDS_ON_OTHER_MODULES = 0x00000001,
#endif
        VMFLAG_DELEGATE                        = 0x00000002,

        // VMFLAG_UNUSED                       = 0x0000001c,

        VMFLAG_FIXED_ADDRESS_VT_STATICS        = 0x00000020, // Value type Statics in this class will be pinned
        VMFLAG_HASLAYOUT                       = 0x00000040,
        VMFLAG_ISNESTED                        = 0x00000080,

        VMFLAG_IS_EQUIVALENT_TYPE              = 0x00000200,

        //   OVERLAYED is used to detect whether Equals can safely optimize to a bit-compare across the structure.
        VMFLAG_HASOVERLAYEDFIELDS              = 0x00000400,

        // Set this if this class or its parent have instance fields which
        // must be explicitly inited in a constructor (e.g. pointers of any
        // kind, gc or native).
        //
        // Currently this is used by the verifier when verifying value classes
        // - it's ok to use uninitialised value classes if there are no
        // pointer fields in them.
        VMFLAG_HAS_FIELDS_WHICH_MUST_BE_INITED = 0x00000800,

        VMFLAG_UNSAFEVALUETYPE                 = 0x00001000,

        VMFLAG_BESTFITMAPPING_INITED           = 0x00002000, // VMFLAG_BESTFITMAPPING and VMFLAG_THROWONUNMAPPABLECHAR are valid only if this is set
        VMFLAG_BESTFITMAPPING                  = 0x00004000, // BestFitMappingAttribute.Value
        VMFLAG_THROWONUNMAPPABLECHAR           = 0x00008000, // BestFitMappingAttribute.ThrowOnUnmappableChar

        // unused                              = 0x00010000,
        VMFLAG_NO_GUID                         = 0x00020000,
        VMFLAG_HASNONPUBLICFIELDS              = 0x00040000,
        // unused                              = 0x00080000,
        VMFLAG_CONTAINS_STACK_PTR              = 0x00100000,
        VMFLAG_PREFER_ALIGN8                   = 0x00200000, // Would like to have 8-byte alignment
        VMFLAG_ONLY_ABSTRACT_METHODS           = 0x00400000, // Type only contains abstract methods

#ifdef FEATURE_COMINTEROP
        VMFLAG_SPARSE_FOR_COMINTEROP           = 0x00800000,
        // interfaces may have a coclass attribute
        VMFLAG_HASCOCLASSATTRIB                = 0x01000000,
        VMFLAG_COMEVENTITFMASK                 = 0x02000000, // class is a special COM event interface
#endif // FEATURE_COMINTEROP
        VMFLAG_VTABLEMETHODIMPL                = 0x04000000, // class uses MethodImpl to override virtual function defined on class
        VMFLAG_COVARIANTOVERRIDE               = 0x08000000, // class has a covariant override

        // This one indicates that the fields of the valuetype are
        // not tightly packed and is used to check whether we can
        // do bit-equality on value types to implement ValueType::Equals.
        // It is not valid for classes, and only matters if ContainsPointer
        // is false.
        VMFLAG_NOT_TIGHTLY_PACKED              = 0x10000000,

        // True if methoddesc on this class have any real (non-interface) methodimpls
        VMFLAG_CONTAINS_METHODIMPLS            = 0x20000000,

#ifdef FEATURE_COMINTEROP
        VMFLAG_MARSHALINGTYPE_MASK             = 0xc0000000,

        VMFLAG_MARSHALINGTYPE_INHIBIT          = 0x40000000,
        VMFLAG_MARSHALINGTYPE_FREETHREADED     = 0x80000000,
        VMFLAG_MARSHALINGTYPE_STANDARD         = 0xc0000000,
#endif
    };

public:
    // C_ASSERTs in Jitinterface.cpp need this to be public to check the offset.
    // Put it first so the offset rarely changes, which just reduces the number of times we have to fiddle
    // with the offset.
    PTR_GuidInfo m_pGuidInfo;  // The cached guid information for interfaces.

#ifdef _DEBUG
public:
    LPCUTF8 m_szDebugClassName;
    BOOL m_fDebuggingClass;
#endif

private:
    // Layout rest of fields below from largest to smallest to lessen the chance of wasting bytes with
    // compiler injected padding (especially with the difference between pointers and DWORDs on 64-bit).
    PTR_EEClassOptionalFields m_rpOptionalFields;

    // TODO: Remove this field. It is only used by SOS and object validation for stress.
    PTR_MethodTable m_pMethodTable;

    PTR_FieldDesc m_pFieldDescList;
    PTR_MethodDescChunk m_pChunks;

#ifdef FEATURE_COMINTEROP
    union
    {
        // For COM+ wrapper objects that extend an unmanaged class, this field
        // may contain a delegate to be called to allocate the aggregated
        // unmanaged class (instead of using CoCreateInstance).
        OBJECTHANDLE    m_ohDelegate;

        // For interfaces this contains the COM interface type.
        CorIfaceAttr    m_ComInterfaceType;
    };

    ComCallWrapperTemplate *m_pccwTemplate;   // points to interop data structures used when this type is exposed to COM
#endif // FEATURE_COMINTEROP

    DWORD m_dwAttrClass;
    DWORD m_VMFlags;

    /*
     * We maintain some auxillary flags in DEBUG builds,
     * this frees up some bits in m_wVMFlags
     */
#if defined(_DEBUG)
    WORD m_wAuxFlags;
#endif

    // NOTE: Following BYTE fields are layed out together so they'll fit within the same DWORD for efficient
    // structure packing.
    BYTE m_NormType;
    BYTE m_fFieldsArePacked;        // TRUE iff fields pointed to by GetPackedFields() are in packed state
    BYTE m_cbFixedEEClassFields;    // Count of bytes of normal fields of this instance (EEClass,
                                    // LayoutEEClass etc.). Doesn't count bytes of "packed" fields
    BYTE m_cbBaseSizePadding;       // How many bytes of padding are included in BaseSize

public:
    // EEClass optional field support. Whether a particular EEClass instance has optional fields is determined
    // at class load time. The entire EEClassOptionalFields structure is allocated if the EEClass has need of
    // one or more optional fields.

#ifndef DACCESS_COMPILE
    void AttachOptionalFields(EEClassOptionalFields *pFields)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(m_rpOptionalFields == NULL);

        m_rpOptionalFields = pFields;
    }
#endif // !DACCESS_COMPILE

    bool HasOptionalFields()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_rpOptionalFields != NULL;
    }

    PTR_EEClassOptionalFields GetOptionalFields()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_rpOptionalFields;
    }

private:
    //
    // Support for packed fields.
    //

    // Get pointer to the packed fields structure attached to this instance.
    PTR_EEClassPackedFields GetPackedFields();

    // Get the value of the given field. Works regardless of whether the field is currently in its packed or
    // unpacked state.
    DWORD GetPackableField(EEClassFieldId eField);

    // Set the value of the given field. The field *must* be in the unpacked state for this to be legal (in
    // practice all packable fields must be initialized during class construction and from then on remain
    // immutable).
    void SetPackableField(EEClassFieldId eField, DWORD dwValue);

    //-------------------------------------------------------------
    // END CONCRETE DATA LAYOUT
    //-------------------------------------------------------------



    /************************************
     *  PROTECTED METHODS
     ************************************/
protected:
#ifndef DACCESS_COMPILE
    /*
     * Constructor: prevent any other class from doing a new()
     */
    EEClass(DWORD cbFixedEEClassFields);

    /*
     * Destructor: prevent any other class from deleting
     */
    ~EEClass()
    {
        LIMITED_METHOD_CONTRACT;
    }
#endif // !DACCESS_COMPILE

};

// --------------------------------------------------------------------------------------------
template <typename Data>
class FixedCapacityStackingAllocatedUTF8StringHash
{
public:
    // Entry
    struct HashEntry
    {
        HashEntry *   m_pNext;        // Next item with same bucketed hash value
        DWORD         m_dwHashValue;  // Hash value
        LPCUTF8       m_pKey;         // String key
        Data          m_data;         // Data
    };

    HashEntry **      m_pBuckets;       // Pointer to first entry for each bucket
    DWORD             m_dwNumBuckets;
    BYTE *            m_pMemory;        // Current pointer into preallocated memory for entries
    BYTE *            m_pMemoryStart;   // Start pointer of pre-allocated memory fo entries

    INDEBUG(BYTE *    m_pDebugEndMemory;)

    FixedCapacityStackingAllocatedUTF8StringHash()
        : m_pMemoryStart(NULL)
        { LIMITED_METHOD_CONTRACT; }

    static DWORD
    GetHashCode(
        LPCUTF8 szString)
        { WRAPPER_NO_CONTRACT; return HashStringA(szString); }

    // Throws on error
    void
    Init(
        DWORD               dwMaxEntries,
        StackingAllocator * pAllocator);

    // Insert new entry at head of list
    void
    Insert(
        LPCUTF8         pszName,
        const Data &    data);

    // Return the first matching entry in the list, or NULL if there is no such entry
    HashEntry *
    Lookup(
        LPCUTF8 pszName);

    // Return the next matching entry in the list, or NULL if there is no such entry.
    HashEntry *
    FindNext(
        HashEntry * pEntry);
};


//---------------------------------------------------------------------------------------
//
class LayoutEEClass : public EEClass
{
public:
    DAC_ALIGNAS(EEClass) // Align the first member to the alignment of the base class
    EEClassLayoutInfo m_LayoutInfo;
    Volatile<PTR_EEClassNativeLayoutInfo> m_nativeLayoutInfo;

#ifndef DACCESS_COMPILE
    LayoutEEClass() : EEClass(sizeof(LayoutEEClass))
    {
        LIMITED_METHOD_CONTRACT;
        m_nativeLayoutInfo = NULL;
    }
#endif // !DACCESS_COMPILE
};

class UMThunkMarshInfo;

#ifdef FEATURE_COMINTEROP
struct ComPlusCallInfo;
#endif // FEATURE_COMINTEROP

class DelegateEEClass : public EEClass
{
public:
    DAC_ALIGNAS(EEClass) // Align the first member to the alignment of the base class
    PTR_Stub                         m_pStaticCallStub;
    PTR_Stub                         m_pInstRetBuffCallStub;
    PTR_MethodDesc                   m_pInvokeMethod;
    PTR_Stub                         m_pMultiCastInvokeStub;
    PTR_Stub                         m_pWrapperDelegateInvokeStub;
    UMThunkMarshInfo*                m_pUMThunkMarshInfo;
    PTR_MethodDesc                   m_pBeginInvokeMethod;
    PTR_MethodDesc                   m_pEndInvokeMethod;
    Volatile<PCODE>                  m_pMarshalStub;

#ifdef FEATURE_COMINTEROP
    ComPlusCallInfo *m_pComPlusCallInfo;
#endif // FEATURE_COMINTEROP

    PTR_MethodDesc GetInvokeMethod()
    {
        return m_pInvokeMethod;
    }

    PTR_MethodDesc GetBeginInvokeMethod()
    {
        return m_pBeginInvokeMethod;
    }

    PTR_MethodDesc GetEndInvokeMethod()
    {
        return m_pEndInvokeMethod;
    }

#ifndef DACCESS_COMPILE
    DelegateEEClass() : EEClass(sizeof(DelegateEEClass))
    {
        LIMITED_METHOD_CONTRACT;
        // Note: Memory allocated on loader heap is zero filled
    }

    // We need a LoaderHeap that lives at least as long as the DelegateEEClass, but ideally no longer
    LoaderHeap *GetStubHeap();
#endif // !DACCESS_COMPILE

};


typedef DPTR(ArrayClass) PTR_ArrayClass;


// Dynamically generated array class structure
class ArrayClass : public EEClass
{
    friend MethodTable* Module::CreateArrayMethodTable(TypeHandle elemTypeHnd, CorElementType arrayKind, unsigned Rank, AllocMemTracker *pamTracker);

#ifndef DACCESS_COMPILE
    ArrayClass() : EEClass(sizeof(ArrayClass)) { LIMITED_METHOD_CONTRACT; }
#else
    friend class NativeImageDumper;
#endif

private:

    DAC_ALIGNAS(EEClass) // Align the first member to the alignment of the base class
    unsigned char   m_rank;
    CorElementType  m_ElementType;// Cache of element type in m_ElementTypeHnd

public:
    DWORD GetRank() {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        return m_rank;
    }
    void SetRank (unsigned Rank) {
        LIMITED_METHOD_CONTRACT;
        // The only code path calling this function is code:ClassLoader::CreateTypeHandleForTypeKey, which has
        // checked the rank already.  Assert that the rank is less than MAX_RANK and that it fits in one byte.
        _ASSERTE((Rank <= MAX_RANK) && (Rank <= (unsigned char)(-1)));
        m_rank = (unsigned char)Rank;
    }

    CorElementType GetArrayElementType() {
        LIMITED_METHOD_CONTRACT;
        return m_ElementType;
    }
    void SetArrayElementType(CorElementType ElementType) {
        LIMITED_METHOD_CONTRACT;
        m_ElementType = ElementType;
    }


    // Allocate a new MethodDesc for the methods we add to this class
    void InitArrayMethodDesc(
        ArrayMethodDesc* pNewMD,
        PCCOR_SIGNATURE pShortSig,
        DWORD   cShortSig,
        DWORD   dwVtableSlot,
        LoaderAllocator *pLoaderAllocator,
        AllocMemTracker *pamTracker);

    // Generate a short sig for an array accessor
    VOID GenerateArrayAccessorCallSig(DWORD   dwRank,
                                      DWORD   dwFuncType, // Load, store, or <init>
                                      PCCOR_SIGNATURE *ppSig, // Generated signature
                                      DWORD * pcSig,      // Generated signature size
                                      LoaderAllocator *pLoaderAllocator,
                                      AllocMemTracker *pamTracker
#ifdef FEATURE_ARRAYSTUB_AS_IL
                                      ,BOOL fForStubAsIL
#endif
    );


};

inline EEClassLayoutInfo *EEClass::GetLayoutInfo()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(HasLayout());
    return &((LayoutEEClass *) this)->m_LayoutInfo;
}

inline PTR_EEClassNativeLayoutInfo EEClass::GetNativeLayoutInfo()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(HasLayout());

    return ((LayoutEEClass*)this)->m_nativeLayoutInfo;
}

inline BOOL EEClass::IsBlittable()
{
    LIMITED_METHOD_CONTRACT;

    // Either we have an opaque bunch of bytes, or we have some fields that are
    // all isomorphic and explicitly layed out.
    return (HasLayout() && GetLayoutInfo()->IsBlittable());
}

inline BOOL EEClass::IsManagedSequential()
{
    LIMITED_METHOD_CONTRACT;
    return HasLayout() && GetLayoutInfo()->IsManagedSequential();
}

inline BOOL EEClass::HasExplicitSize()
{
    LIMITED_METHOD_CONTRACT;
    return HasLayout() && GetLayoutInfo()->HasExplicitSize();
}

//==========================================================================
// These routines manage the prestub (a bootstrapping stub that all
// FunctionDesc's are initialized with.)
//==========================================================================
VOID InitPreStubManager();

EXTERN_C void STDCALL ThePreStub();

inline PCODE GetPreStubEntryPoint()
{
    return GetEEFuncEntryPoint(ThePreStub);
}

#if defined(HAS_COMPACT_ENTRYPOINTS) && defined(TARGET_ARM)

EXTERN_C void STDCALL ThePreStubCompactARM();

inline PCODE GetPreStubCompactARMEntryPoint()
{
    return GetEEFuncEntryPoint(ThePreStubCompactARM);
}

#endif // defined(HAS_COMPACT_ENTRYPOINTS) && defined(TARGET_ARM)

PCODE TheUMThunkPreStub();

PCODE TheVarargNDirectStub(BOOL hasRetBuffArg);



// workaround: These classification bits need cleanup bad: for now, this gets around
// IJW setting both mdUnmanagedExport & mdPinvokeImpl on expored methods.
#define IsReallyMdPinvokeImpl(x) ( ((x) & mdPinvokeImpl) && !((x) & mdUnmanagedExport) )

//
// The MethodNameHash is a temporary loader structure which may be allocated if there are a large number of
// methods in a class, to quickly get from a method name to a MethodDesc (potentially a chain of MethodDescs).
//

#define METH_NAME_CACHE_SIZE        5
#define MAX_MISSES                  3

#ifdef EnC_SUPPORTED

struct EnCAddedFieldElement;

#endif // EnC_SUPPORTED


// --------------------------------------------------------------------------------------------
// For generic instantiations the FieldDescs stored for instance
// fields are approximate, not exact, i.e. they are representatives owned by
// canonical instantiation and they do not carry exact type information.
// This will not include EnC related fields. (See EncApproxFieldDescIterator for that)
class ApproxFieldDescIterator
{
private:
    int m_iteratorType;
    PTR_FieldDesc m_pFieldDescList;
    int m_currField;
    int m_totalFields;

  public:
    enum IteratorType {
       INSTANCE_FIELDS = 0x1,
       STATIC_FIELDS   = 0x2,
       ALL_FIELDS      = (INSTANCE_FIELDS | STATIC_FIELDS)
    };
    ApproxFieldDescIterator();
    ApproxFieldDescIterator(MethodTable *pMT, int iteratorType)
    {
        SUPPORTS_DAC;
        Init(pMT, iteratorType);
    }
    void Init(MethodTable *pMT, int iteratorType);
    PTR_FieldDesc Next();

    int GetIteratorType() {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        return m_iteratorType;
    }

    int Count() {
        LIMITED_METHOD_CONTRACT;
        return m_totalFields;
    }
    int CountRemaining() {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        return m_totalFields - m_currField - 1;
    }
};

//
// DeepFieldDescIterator iterates over the entire
// set of fields available to a class, inherited or
// introduced.
//

class DeepFieldDescIterator
{
private:
    ApproxFieldDescIterator m_fieldIter;
    int m_numClasses;
    int m_curClass;
    MethodTable* m_classes[16];
    int m_deepTotalFields;
    bool m_lastNextFromParentClass;

    bool NextClass();

public:
    DeepFieldDescIterator()
    {
        LIMITED_METHOD_CONTRACT;

        m_numClasses = 0;
        m_curClass = 0;
        m_deepTotalFields = 0;
        m_lastNextFromParentClass = false;
    }
    DeepFieldDescIterator(MethodTable* pMT, int iteratorType,
                          bool includeParents = true)
    {
        WRAPPER_NO_CONTRACT;

        Init(pMT, iteratorType, includeParents);
    }
    void Init(MethodTable* pMT, int iteratorType,
              bool includeParents = true);

    FieldDesc* Next();

    bool Skip(int numSkip);

    int Count()
    {
        LIMITED_METHOD_CONTRACT;
        return m_deepTotalFields;
    }
    bool IsFieldFromParentClass()
    {
        LIMITED_METHOD_CONTRACT;
        return m_lastNextFromParentClass;
    }
};

#endif // !CLASS_H
