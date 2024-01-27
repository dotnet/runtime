// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: typehandle.h
//


//

//
// ============================================================================


#ifndef TYPEHANDLE_H
#define TYPEHANDLE_H

#include "check.h"
#include "classloadlevel.h"

class TypeDesc;
class TypeHandle;
class Instantiation;
class FnPtrTypeDesc;
class ParamTypeDesc;
class TypeVarTypeDesc;
class MethodTable;
class EEClass;
class Module;
class Assembly;
class BaseDomain;
class MethodDesc;
class TypeKey;
class TypeHandleList;
class InstantiationContext;
class DataImage;
namespace Generics { class RecursionGraph; }
struct CORINFO_CLASS_STRUCT_;

typedef DPTR(class TypeVarTypeDesc) PTR_TypeVarTypeDesc;
typedef SPTR(class FnPtrTypeDesc) PTR_FnPtrTypeDesc;
typedef DPTR(class ParamTypeDesc) PTR_ParamTypeDesc;
typedef DPTR(class TypeDesc) PTR_TypeDesc;
typedef DPTR(class TypeHandle) PTR_TypeHandle;


typedef CUnorderedArray<TypeHandle, 40> DFLPendingList;

class TypeHandlePairList;

#ifdef FEATURE_COMINTEROP
class ComCallWrapperTemplate;
#endif // FEATURE_COMINTEROP

/*************************************************************************/
// A TypeHandle is the FUNDAMENTAL concept of type identity in the CLR.
// That is two types are equal if and only if their type handles
// are equal.  A TypeHandle, is a pointer sized struture that encodes
// everything you need to know to figure out what kind of type you are
// actually dealing with.

// At the present time a TypeHandle can point at two possible things
//
//      1) A MethodTable    (Arrays, Intrinsics, Classes, Value Types and their instantiations)
//      2) A TypeDesc       (all other cases: byrefs, pointer types, function pointers, generic type variables)
//
// or with IL stubs, a third thing:
//
//      3) A MethodTable for a native value type.
//
// Wherever possible, you should be using TypeHandles or MethodTables.
// Code that is known to work over Class/ValueClass types (including their
// instantaitions) is currently written to use MethodTables.
//
// TypeDescs in turn break down into several variants and are
// for special cases around the edges
//    - types for function pointers for verification and reflection
//    - types for generic parameters for verification and reflection
//
// Generic type instantiations (in C# syntax: C<ty_1,...,ty_n>) are represented by
// MethodTables, i.e. a new MethodTable gets allocated for each such instantiation.
// The entries in these tables (i.e. the code) are, however, often shared.
// Clients of TypeHandle don't need to know any of this detail; just use the
// GetInstantiation and HasInstantiation methods.

class TypeHandle
{
public:
    TypeHandle() {
        LIMITED_METHOD_DAC_CONTRACT;

        m_asTAddr = 0;
    }

    static TypeHandle FromPtr(PTR_VOID aPtr)
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return TypeHandle(dac_cast<TADDR>(aPtr));
    }
    // Create a TypeHandle from the target address of a MethodTable
    static TypeHandle FromTAddr(TADDR data)
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return TypeHandle(data);
    }

    // When you ask for a class in JitInterface when all you have
    // is a methodDesc of an array method...
    // Convert from a JitInterface handle to an internal EE TypeHandle
    explicit TypeHandle(struct CORINFO_CLASS_STRUCT_*aPtr)
    {
        LIMITED_METHOD_DAC_CONTRACT;

        m_asTAddr = dac_cast<TADDR>(aPtr);
        INDEBUGIMPL(Verify());
    }

    TypeHandle(MethodTable const * aMT) {
        LIMITED_METHOD_DAC_CONTRACT;

        m_asTAddr = dac_cast<TADDR>(aMT);
        INDEBUGIMPL(Verify());
    }

    explicit TypeHandle(TypeDesc *aType) {
        LIMITED_METHOD_DAC_CONTRACT;
        _ASSERTE(aType);

        m_asTAddr = (dac_cast<TADDR>(aType) | 2);
        INDEBUGIMPL(Verify());
    }

    inline BOOL IsNativeValueType() const;
    inline MethodTable *AsNativeValueType() const;

private:
    // This constructor has been made private.  You must use the explicit static functions
    // TypeHandle::FromPtr and TypeHandle::TAddr instead of these constructors.
    // Allowing a public constructor that takes a "void *" or a "TADDR" is error-prone.
    explicit TypeHandle(TADDR aTAddr)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        m_asTAddr = aTAddr;
        INDEBUGIMPL(Verify());
    }


public:
    FORCEINLINE int operator==(const TypeHandle& typeHnd) const {
        LIMITED_METHOD_DAC_CONTRACT;

        return(m_asTAddr == typeHnd.m_asTAddr);
    }

    FORCEINLINE int operator!=(const TypeHandle& typeHnd) const {
        LIMITED_METHOD_DAC_CONTRACT;

        return(m_asTAddr != typeHnd.m_asTAddr);
    }

        // Methods for probing exactly what kind of a type handle we have
    FORCEINLINE BOOL IsNull() const {
        LIMITED_METHOD_DAC_CONTRACT;
#ifdef _PREFIX_
        if (m_asTAddr == 0) {
#ifndef DACCESS_COMPILE
            PREFIX_ASSUME(m_asPtr == NULL);
#endif
            return true;
        }
        else {
#ifndef DACCESS_COMPILE
            PREFIX_ASSUME(m_asPtr != NULL);
#endif
            return false;
        }
#else
        return(m_asTAddr == 0);
#endif
    }

    // Note that this returns denormalized BOOL to help the compiler with optimizations
    FORCEINLINE BOOL IsTypeDesc() const  {
        LIMITED_METHOD_DAC_CONTRACT;
#ifdef _PREFIX_
        if (m_asTAddr & 2) {
            PREFIX_ASSUME(m_asTAddr != NULL);
#ifndef DACCESS_COMPILE
            PREFIX_ASSUME(m_asPtr   != NULL);
#endif
            return true;
        }
        else {
            return false;
        }
#else
        return(m_asTAddr & 2);
#endif
    }

    BOOL IsEnum() const;

    BOOL IsFnPtrType() const;

    inline PTR_MethodTable AsMethodTable() const;

    inline PTR_TypeDesc AsTypeDesc() const;

    // To the extent possible, you should try to use methods like the ones
    // below that treat all types uniformly.

    // Gets the size that this type would take up embedded in another object
    // thus objects all return sizeof(void*).
    unsigned GetSize() const;

    // Returns the type name, including the generic instantiation if possible.
    // See the TypeString class for better control over name formatting.
    void GetName(SString &result) const;

    // Returns the ELEMENT_TYPE_* that you would use in a signature
    // The only normalization that happens is that for type handles
    // for instantiated types (e.g. class List<String> or
    // value type Pair<int,int>)) this returns either ELEMENT_TYPE_CLASS
    // or ELEMENT_TYPE_VALUE, _not_ ELEMENT_TYPE_WITH.
    CorElementType GetSignatureCorElementType() const;

    // This helper:
    // - Will return enums underlying type
    // - Will return underlying primitive for System.Int32 etc...
    // - Will return underlying primitive as will be used in the calling convention
    //      For example
    //              struct t
    //              {
    //                  public int i;
    //              }
    //      will return ELEMENT_TYPE_I4 in x86 instead of ELEMENT_TYPE_VALUETYPE. We
    //      call this type of value type a primitive value type
    //
    // Internal representation is used among another things for the calling convention
    // (jit benefits of primitive value types) or optimizing marshalling.
    //
    // This will NOT convert E_T_ARRAY, E_T_SZARRAY etc. to E_T_CLASS (though it probably
    // should).  Use CorTypeInfo::IsObjRef for that.
    CorElementType GetInternalCorElementType() const;

    // This helper will return the same as GetSignatureCorElementType except:
    // - Will return enums underlying type
    CorElementType GetVerifierCorElementType() const;

    //-------------------------------------------------------------------
    // CASTING
    //
    // There are two variants of the "CanCastTo" method:
    //
    // CanCastTo
    // - might throw, might trigger GC
    // - return type is boolean (FALSE = cannot cast, TRUE = can cast)
    //
    // CanCastToCached
    // - does not throw, does not trigger GC
    // - return type is three-valued (CanCast, CannotCast, MaybeCast)
    //
    // MaybeCast indicates an inconclusive result
    // - the test result could not be obtained from a cache
    //   so the caller should now call CanCastTo if it cares
    //
    // Note that if the TypeHandle is a valuetype, the caller is responsible
    // for checking that the valuetype is in its boxed form before calling
    // CanCastTo. Otherwise, the caller should be using IsBoxedAndCanCastTo()
    typedef enum { CannotCast, CanCast, MaybeCast } CastResult;

    BOOL CanCastTo(TypeHandle type, TypeHandlePairList *pVisited = NULL) const;
    BOOL IsBoxedAndCanCastTo(TypeHandle type, TypeHandlePairList *pVisited) const;
    CastResult CanCastToCached(TypeHandle type) const;

#ifndef DACCESS_COMPILE
    // Type equivalence based on Guid and TypeIdentifier attributes
    inline BOOL IsEquivalentTo(TypeHandle type COMMA_INDEBUG(TypeHandlePairList *pVisited = NULL)) const;
#endif

    // Get the parent, known to be decoded
    TypeHandle GetParent() const;

    // Obtain element type for an array, byref or pointer, returning NULL otherwise
    TypeHandle GetTypeParam() const;

    // Obtain instantiation from an instantiated type
    // NULL if not instantiated
    Instantiation GetInstantiation() const;

    // Does this type satisfy its class constraints, recursively up the hierarchy
    BOOL SatisfiesClassConstraints() const;

    TypeHandle Instantiate(Instantiation inst) const;
    TypeHandle MakePointer() const;
    TypeHandle MakeByRef() const;
    TypeHandle MakeSZArray() const;
    TypeHandle MakeArray(int rank) const;
    TypeHandle MakeNativeValueType() const;

    // Obtain instantiation from an instantiated type *or* a pointer to the element type for an array
    Instantiation GetClassOrArrayInstantiation() const;

    // Is this type instantiated?
    BOOL HasInstantiation() const;

    // Is this a generic type whose type arguments are its formal type parameters?
    BOOL IsGenericTypeDefinition() const;

    // Is this either a non-generic type (e.g. a non-genric class type or an array type or a pointer type etc.)
    // or a generic type whose type arguments are its formal type parameters?
    //Equivalent to (!HasInstantiation() || IsGenericTypeDefinition());
    inline BOOL IsTypicalTypeDefinition() const;

    BOOL IsSharedByGenericInstantiations() const;

    // Recursively search the type arguments and if
    // one of the type arguments is Canon then return TRUE
    //
    // A<__Canon>    is the canonical TypeHandle (aka "representative" generic MT)
    // A<B<__Canon>> is a subtype that contains a Canonical type
    //
    BOOL IsCanonicalSubtype() const;

#ifndef DACCESS_COMPILE
    bool IsManagedClassObjectPinned() const;

    // Allocates a RuntimeType object with the given TypeHandle. If the LoaderAllocator
    // represents a not-unloadable context, it allocates the object on a frozen segment
    // so the direct reference will be stored to the pDest argument. In case of unloadable
    // context, an index to the pinned table will be saved.
    void AllocateManagedClassObject(RUNTIMETYPEHANDLE* pDest);

    FORCEINLINE static bool GetManagedClassObjectFromHandleFast(RUNTIMETYPEHANDLE handle, OBJECTREF* pRef)
    {
        LIMITED_METHOD_CONTRACT;

        // For a non-unloadable context, handle is expected to be either null (is not cached yet)
        // or be a direct pointer to a frozen RuntimeType object

        if (handle & 1)
        {
            // Clear the "is pinned object" bit from the managed reference
            *pRef = (OBJECTREF)(handle - 1);
            return true;
        }
        return false;
    }
#endif

    // Similar to IsCanonicalSubtype, but applied to a vector.
    static BOOL IsCanonicalSubtypeInstantiation(Instantiation inst);

    // For an uninstantiated generic type, return the number of type parameters required for instantiation
    // For an instantiated type, return the number of type parameters in the instantiation
    // Otherwise return 0
    DWORD GetNumGenericArgs() const;

    BOOL IsValueType() const;
    BOOL IsInterface() const;
    BOOL IsAbstract() const;

    inline DWORD IsObjectType() const
    {
        LIMITED_METHOD_CONTRACT;
        return *this == TypeHandle(g_pObjectClass);
    }

    // Retrieve the key corresponding to this handle
    TypeKey GetTypeKey() const;

    // To what level has this type been loaded?
    ClassLoadLevel GetLoadLevel() const;

    // Equivalent to GetLoadLevel() == CLASS_LOADED
    BOOL IsFullyLoaded() const;

    void DoFullyLoad(Generics::RecursionGraph *pVisited, ClassLoadLevel level, DFLPendingList *pPending, BOOL *pfBailed,
                     const InstantiationContext *pInstContext);

    inline void SetIsFullyLoaded();


#ifdef _DEBUG
    // Check that this type matches the key given
    // i.e. that all aspects (element type, module/token, rank for arrays, instantiation for generic types) match up
    CHECK CheckMatchesKey(const TypeKey *pKey) const;

    // Check that this type is loaded up to the level indicated
    // Also check that it is non-null
    CHECK CheckLoadLevel(ClassLoadLevel level);

    // Equivalent to CheckLoadLevel(CLASS_LOADED)
    CHECK CheckFullyLoaded();
#endif

    bool IsHFA() const;
    CorInfoHFAElemType GetHFAType() const;

    bool IsFloatHfa() const;

#ifdef FEATURE_64BIT_ALIGNMENT
    bool RequiresAlign8() const;
#endif // FEATURE_64BIT_ALIGNMENT

#ifndef DACCESS_COMPILE

    BOOL IsBlittable() const;
    BOOL HasLayout() const;

#ifdef FEATURE_COMINTEROP
    TypeHandle GetCoClassForInterface() const;
    DWORD IsComClassInterface() const;
    BOOL IsComObjectType() const;
    BOOL IsComEventItfType() const;
    CorIfaceAttr GetComInterfaceType() const;
    TypeHandle GetDefItfForComClassItf() const;

    ComCallWrapperTemplate *GetComCallWrapperTemplate() const;
    BOOL SetComCallWrapperTemplate(ComCallWrapperTemplate *pTemplate);
#endif // FEATURE_COMINTEROP

#endif

    // Unlike AsMethodTable, GetMethodTable will get the method table
    // of the type, regardless of whether it is a TypeDesc.
    // Note, however this method table may be non-exact/shared for TypeDescs.
    // for example all pointers and function pointers use ELEMENT_TYPE_U.
    // And some types (like ByRef or generic type parameters) have no
    // method table and this function returns NULL for them.
    inline PTR_MethodTable GetMethodTable() const;

    // Returns the type which should be used for visibility checking.
    inline MethodTable* GetMethodTableOfRootTypeParam() const;

    // Returns the type of the array element
    inline TypeHandle GetArrayElementTypeHandle() const;

    // Returns the rank for the SZARRAY or ARRAY type
    inline unsigned int GetRank() const;

    // Return the canonical representative MT amongst the set of MT's that share
    // code with the MT for the given TypeHandle because of generics.
    PTR_MethodTable GetCanonicalMethodTable() const;

    // The module that defined the underlying type
    // (First strip off array/ptr qualifiers and generic type arguments)
    PTR_Module GetModule() const;

    // The module where this type lives for the purposes of loading and prejitting
    // Note: NGen time result might differ from runtime result for parameterized types (generics, arrays, etc.)
    // See code:ClassLoader::ComputeLoaderModule or file:clsload.hpp#LoaderModule for more information
    PTR_Module GetLoaderModule() const;

    // The assembly that defined this type (== GetModule()->GetAssembly())
    Assembly * GetAssembly() const;

    // GetDomain on an instantiated type, e.g. C<ty1,ty2> returns the SharedDomain if all the
    // constituent parts of the type are SharedDomain (i.e. domain-neutral),
    // and returns an AppDomain if any of the parts are from an AppDomain,
    // i.e. are domain-bound.  If any of the parts are domain-bound
    // then they will all belong to the same domain.
    PTR_BaseDomain GetDomain() const;

    PTR_LoaderAllocator GetLoaderAllocator() const;

    // Get the class token, assuming the type handle represents a named type,
    // i.e. a class, a value type, a generic instantiation etc.
    inline mdTypeDef GetCl() const;

    // Shortcuts

    // ARRAY or SZARRAY
    BOOL IsArray() const;

    // VAR or MVAR
    BOOL IsGenericVariable() const;

    // BYREF
    BOOL IsByRef() const;

    // BYREFLIKE (does not return TRUE for IsByRef types)
    BOOL IsByRefLike() const;

    // PTR
    BOOL IsPointer() const;

    // True if this type *is* a formal generic type parameter or any component of it is a formal generic type parameter
    BOOL ContainsGenericVariables(BOOL methodOnly=FALSE) const;

    Module* GetDefiningModuleForOpenType() const;

    // Is type that has a type parameter (ARRAY, SZARRAY, BYREF, PTR)
    BOOL HasTypeParam() const;

    BOOL IsRestored() const;

    // Does this type have zap-encoded components (generic arguments, etc)?
    BOOL HasUnrestoredTypeKey() const;

    void DoRestoreTypeKey();

    void CheckRestore() const;
    BOOL IsExternallyVisible() const;

    // Does this type participate in type equivalence?
    inline BOOL HasTypeEquivalence() const;

    FnPtrTypeDesc* AsFnPtrType() const;

    TypeVarTypeDesc* AsGenericVariable() const;

    Instantiation GetInstantiationOfParentClass(MethodTable *pWhichParent) const;

    PTR_VOID AsPtr() const {                     // Please don't use this if you can avoid it
        LIMITED_METHOD_DAC_CONTRACT;

        return(PTR_VOID(m_asTAddr));
    }

    TADDR AsTAddr() const {
        LIMITED_METHOD_DAC_CONTRACT;

        return m_asTAddr;
    }

    INDEBUGIMPL(BOOL Verify();)             // DEBUGGING Make certain this is a valid type handle

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif

    OBJECTREF GetManagedClassObject() const;
    OBJECTREF GetManagedClassObjectFast() const;

    static TypeHandle MergeArrayTypeHandlesToCommonParent(
        TypeHandle ta, TypeHandle tb);

    static TypeHandle MergeTypeHandlesToCommonParent(
        TypeHandle ta, TypeHandle tb);


    BOOL NotifyDebuggerLoad(AppDomain *domain, BOOL attaching) const;
    void NotifyDebuggerUnload(AppDomain *domain) const;

    // Execute the callback functor for each MethodTable that makes up the given type handle.  This method
    // does not invoke the functor for generic variables
    template<class T>
    inline void ForEachComponentMethodTable(T &callback) const;

private:
    static TypeHandle MergeClassWithInterface(
        TypeHandle tClass, TypeHandle tInterface);

    union
    {
        TADDR               m_asTAddr;      // we look at the low order bits
#ifndef DACCESS_COMPILE
        void *              m_asPtr;
        PTR_MethodTable     m_asMT;
        PTR_TypeDesc        m_asTypeDesc;
        PTR_ParamTypeDesc   m_asParamTypeDesc;
        PTR_TypeVarTypeDesc m_asTypeVarTypeDesc;
        PTR_FnPtrTypeDesc   m_asFnPtrTypeDesc;
#endif
    };
};

class TypeHandleList
{
    TypeHandle m_typeHandle;
    TypeHandleList* m_pNext;
    bool m_fBrokenCycle;
 public:
    TypeHandleList(TypeHandle t, TypeHandleList* pNext) : m_typeHandle(t),m_pNext(pNext),m_fBrokenCycle(false) { };
    static BOOL Exists(TypeHandleList* pList, TypeHandle t)
    {
        LIMITED_METHOD_CONTRACT;
        while (pList != NULL) { if (pList->m_typeHandle == t) return TRUE; pList = pList->m_pNext; }
        return FALSE;
    }

    // Supports enumeration of the list.
    static BOOL GetNext(TypeHandleList** ppList, TypeHandle* pHandle)
    {
        LIMITED_METHOD_CONTRACT;
        if (*ppList != NULL)
        {
            *pHandle = (*ppList)->m_typeHandle;
            (*ppList) = (*ppList)->m_pNext;
            return TRUE;
        }
        return FALSE;
    }

    void MarkBrokenCycle(TypeHandle th)
    {
        LIMITED_METHOD_CONTRACT;
        TypeHandleList* pList = this;
        while (pList->m_typeHandle != th) { pList->m_fBrokenCycle = true; pList = pList->m_pNext; }
    }
    bool HasBrokenCycleMark()
    {
        LIMITED_METHOD_CONTRACT;
        return m_fBrokenCycle;
    }
};

class TypeHandlePairList // TODO: Template for TypeHandleList, TypeHandlePairList, TokenPairList?
{
    TypeHandle m_typeHandle1;
    TypeHandle m_typeHandle2;
    TypeHandlePairList *m_pNext;
public:
    TypeHandlePairList(TypeHandle t1, TypeHandle t2, TypeHandlePairList *pNext) : m_typeHandle1(t1), m_typeHandle2(t2), m_pNext(pNext) { };
    static BOOL Exists(TypeHandlePairList *pList, TypeHandle t1, TypeHandle t2)
    {
        LIMITED_METHOD_CONTRACT;
        while (pList != NULL)
        {
            if (pList->m_typeHandle1 == t1 && pList->m_typeHandle2 == t2)
                return TRUE;
            if (pList->m_typeHandle1 == t2 && pList->m_typeHandle2 == t1)
                return TRUE;

            pList = pList->m_pNext;
        }
        return FALSE;
    }
};

#if CHECK_INVARIANTS
inline CHECK CheckPointer(TypeHandle th, IsNullOK ok = NULL_NOT_OK)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;
    SUPPORTS_DAC;
    STATIC_CONTRACT_CANNOT_TAKE_LOCK;

    if (th.IsNull())
    {
        CHECK_MSG(ok, "Illegal null TypeHandle");
    }
    else
    {
        __if_exists(TypeHandle::Check)
        {
            CHECK(th.Check());
        }
#if 0
        CHECK(CheckInvariant(o));
#endif
    }

    CHECK_OK;
}

#endif  // CHECK_INVARIANTS

/*************************************************************************/
// Instantiation is representation of generic instantiation.
// It is simple read-only array of TypeHandles. In NGen, the type handles
// may be encoded using indirections. That's one reason why it is convenient
// to have wrapper class that performs the decoding.
class Instantiation
{
public:
    // Construct empty instantiation
    Instantiation()
        : m_pArgs((TypeHandle*)NULL), m_nArgs(0)
    {
        LIMITED_METHOD_DAC_CONTRACT;
    }

    // Copy construct
    Instantiation(const Instantiation & inst)
        : m_pArgs(inst.m_pArgs), m_nArgs(inst.m_nArgs)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        _ASSERTE(m_nArgs == 0 || m_pArgs != NULL);
    }

    // Construct instantiation from array of TypeHandles
    Instantiation(TypeHandle *pArgs, DWORD nArgs)
        : m_nArgs(nArgs)
    {
        LIMITED_METHOD_DAC_CONTRACT;

        DACCOP_IGNORE(CastOfMarshalledType, "Dual mode DAC problem, but since the size is the same, the cast is safe");
        m_pArgs = pArgs;
        _ASSERTE(m_nArgs == 0 || m_pArgs != NULL);
    }

#ifdef DACCESS_COMPILE
    // This method will create local copy of the instantiation arguments.
    Instantiation(PTR_TypeHandle pArgs, DWORD nArgs)
    {
        LIMITED_METHOD_DAC_CONTRACT;

        // Create a local copy of the instanitation under DAC
        PVOID pLocalArgs = PTR_READ(dac_cast<TADDR>(pArgs), nArgs * sizeof(TypeHandle));
        m_pArgs = (TypeHandle*)pLocalArgs;

        m_nArgs = nArgs;

        _ASSERTE(m_nArgs == 0 || m_pArgs != NULL);
    }
#endif

    // Return i-th instantiation argument
    TypeHandle operator[](DWORD iArg) const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        _ASSERTE(iArg < m_nArgs);
        return m_pArgs[iArg];
    }

    DWORD GetNumArgs() const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_nArgs;
    }

    BOOL IsEmpty() const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_nArgs == 0;
    }

    // Unsafe access to the instantiation. Do not use unless absolutely necessary!!!
    TypeHandle * GetRawArgs() const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_pArgs;
    }

    bool ContainsAllOneType(TypeHandle th)
    {
        for (auto i = GetNumArgs(); i > 0;)
        {
            if ((*this)[--i] != th)
                return false;
        }
        return true;
    }

private:
    // Note that for DAC builds, m_pArgs may be host allocated buffer, not a copy of an object marshalled by DAC.
    TypeHandle* m_pArgs;
    DWORD m_nArgs;
};

#endif // TYPEHANDLE_H
