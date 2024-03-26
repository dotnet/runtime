// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// method.hpp
//

//
// See the book of the runtime entry for overall design:
// file:../../doc/BookOfTheRuntime/ClassLoader/MethodDescDesign.doc
//

#ifndef _METHOD_H
#define _METHOD_H

#include "cor.h"
#include "util.hpp"
#include "clsload.hpp"
#include "class.h"
#include "siginfo.hpp"
#include "methodimpl.h"
#include "typedesc.h"
#include <stddef.h>
#include "eeconfig.h"
#include "precode.h"

class Stub;
class FCallMethodDesc;
class FieldDesc;
class NDirect;
class MethodDescChunk;
struct LayoutRawFieldInfo;
class InstantiatedMethodDesc;
class DictionaryLayout;
class Dictionary;
class GCCoverageInfo;
class DynamicMethodDesc;
class ReJitManager;
class CodeVersionManager;
class PrepareCodeConfig;

typedef DPTR(FCallMethodDesc)        PTR_FCallMethodDesc;
typedef DPTR(ArrayMethodDesc)        PTR_ArrayMethodDesc;
typedef DPTR(DynamicMethodDesc)      PTR_DynamicMethodDesc;
typedef DPTR(InstantiatedMethodDesc) PTR_InstantiatedMethodDesc;
typedef DPTR(GCCoverageInfo)         PTR_GCCoverageInfo;        // see code:GCCoverageInfo::savedCode

#ifdef FEATURE_MINIMETADATA_IN_TRIAGEDUMPS
GVAL_DECL(DWORD, g_MiniMetaDataBuffMaxSize);
GVAL_DECL(TADDR, g_MiniMetaDataBuffAddress);
#endif // FEATURE_MINIMETADATA_IN_TRIAGEDUMPS

EXTERN_C VOID STDCALL NDirectImportThunk();

#define METHOD_TOKEN_REMAINDER_BIT_COUNT 12
#define METHOD_TOKEN_REMAINDER_MASK ((1 << METHOD_TOKEN_REMAINDER_BIT_COUNT) - 1)
#define METHOD_TOKEN_RANGE_BIT_COUNT (24 - METHOD_TOKEN_REMAINDER_BIT_COUNT)
#define METHOD_TOKEN_RANGE_MASK ((1 << METHOD_TOKEN_RANGE_BIT_COUNT) - 1)

//=============================================================
// Splits methoddef token into two pieces for
// storage inside a methoddesc.
//=============================================================
FORCEINLINE UINT16 GetTokenRange(mdToken tok)
{
    LIMITED_METHOD_CONTRACT;
    return (UINT16)((tok>>METHOD_TOKEN_REMAINDER_BIT_COUNT) & METHOD_TOKEN_RANGE_MASK);
}

FORCEINLINE VOID SplitToken(mdToken tok, UINT16 *ptokrange, UINT16 *ptokremainder)
{
    LIMITED_METHOD_CONTRACT;
    *ptokrange = (UINT16)((tok>>METHOD_TOKEN_REMAINDER_BIT_COUNT) & METHOD_TOKEN_RANGE_MASK);
    *ptokremainder = (UINT16)(tok & METHOD_TOKEN_REMAINDER_MASK);
}

FORCEINLINE mdToken MergeToken(UINT16 tokrange, UINT16 tokremainder)
{
    LIMITED_METHOD_DAC_CONTRACT;
    return (tokrange << METHOD_TOKEN_REMAINDER_BIT_COUNT) | tokremainder | mdtMethodDef;
}

// The MethodDesc is a union of several types. The following
// 3-bit field determines which type it is. Note that JIT'ed/non-JIT'ed
// is not represented here because this isn't known until the
// method is executed for the first time. Because any thread could
// change this bit, it has to be done in a place where access is
// synchronized.

// **** NOTE: if you add any new flags, make sure you add them to ClearFlagsOnUpdate
// so that when a method is replaced its relevant flags are updated

// Used in MethodDesc
enum MethodClassification
{
    mcIL        = 0, // IL
    mcFCall     = 1, // FCall (also includes tlbimped ctor, Delegate ctor)
    mcNDirect   = 2, // N/Direct
    mcEEImpl    = 3, // special method; implementation provided by EE (like Delegate Invoke)
    mcArray     = 4, // Array ECall
    mcInstantiated = 5, // Instantiated generic methods, including descriptors
                        // for both shared and unshared code (see InstantiatedMethodDesc)

#ifdef FEATURE_COMINTEROP
    // This needs a little explanation.  There are MethodDescs on MethodTables
    // which are Interfaces.  These have the mdcInterface bit set.  Then there
    // are MethodDescs on MethodTables that are Classes, where the method is
    // exposed through an interface.  These do not have the mdcInterface bit set.
    //
    // So, today, a dispatch through an 'mdcInterface' MethodDesc is either an
    // error (someone forgot to look up the method in a class' VTable) or it is
    // a case of COM Interop.

    mcComInterop    = 6,
#endif // FEATURE_COMINTEROP
    mcDynamic       = 7, // for method desc with no metadata behind
    mcCount,
};


// All flags in the MethodDesc now reside in a single 16-bit field.

enum MethodDescClassification
{
    // Method is IL, FCall etc., see MethodClassification above.
    mdcClassification                   = 0x0007,
    mdcClassificationCount              = mdcClassification+1,

    // Note that layout of code:MethodDesc::s_ClassificationSizeTable depends on the exact values
    // of mdcHasNonVtableSlot and mdcMethodImpl

    // Has local slot (vs. has real slot in MethodTable)
    mdcHasNonVtableSlot                 = 0x0008,

    // Method is a body for a method impl (MI_MethodDesc, MI_NDirectMethodDesc, etc)
    // where the function explicitly implements IInterface.foo() instead of foo().
    mdcMethodImpl                       = 0x0010,

    // Has slot for native code
    mdcHasNativeCodeSlot                = 0x0020,

    // Method was added via Edit And Continue
    mdcEnCAddedMethod                   = 0x0040,

    // Method is static
    mdcStatic                           = 0x0080,

    mdcValueTypeParametersWalked        = 0x0100, // Indicates that all typeref's in the signature of the method have been resolved
                                                  // to typedefs (or that process failed).

    mdcValueTypeParametersLoaded        = 0x0200, // Indicates if the valuetype parameter types have been loaded.

    // Duplicate method. When a method needs to be placed in multiple slots in the
    // method table, because it could not be packed into one slot. For eg, a method
    // providing implementation for two interfaces, MethodImpl, etc
    mdcDuplicate                        = 0x0400,

    mdcDoesNotHaveEquivalentValuetypeParameters = 0x0800, // Indicates that we have verified that there are no equivalent valuetype parameters
                                                          // for this method.

    mdcRequiresCovariantReturnTypeChecking = 0x1000,

    // Is this method ineligible for inlining?
    mdcNotInline                        = 0x2000,

    // Is the method synchronized
    mdcSynchronized                     = 0x4000,

    mdcIsIntrinsic                      = 0x8000  // Jit may expand method as an intrinsic
};

#define METHOD_MAX_RVA                          0x7FFFFFFF


// The size of this structure needs to be a multiple of MethodDesc::ALIGNMENT
//
// @GENERICS:
// Method descriptors for methods belonging to instantiated types may be shared between compatible instantiations
// Hence for reflection and elsewhere where exact types are important it's necessary to pair a method desc
// with the exact owning type handle.
//
// See genmeth.cpp for details of instantiated generic method descriptors.
//
// A MethodDesc is the representation of a method of a type.  These live in code:MethodDescChunk which in
// turn lives in code:EEClass.   They are conceptually cold (we do not expect to access them in normal
// program execution, but we often fall short of that goal.
//
// A Method desc knows how to get at its metadata token code:GetMemberDef, its chunk
// code:MethodDescChunk, which in turns knows how to get at its type code:MethodTable.
// It also knows how to get at its IL code (code:IMAGE_COR_ILMETHOD)
class MethodDesc
{
    friend class EEClass;
    friend class MethodTableBuilder;
    friend class ArrayClass;
    friend class NDirect;
    friend class MethodDescChunk;
    friend class InstantiatedMethodDesc;
    friend class MethodImpl;
    friend class CheckAsmOffsets;
    friend class ClrDataAccess;

    friend class MethodDescCallSite;

public:

#ifdef TARGET_64BIT
    static const int ALIGNMENT_SHIFT = 3;
#else
    static const int ALIGNMENT_SHIFT = 2;
#endif
    static const size_t ALIGNMENT = (1 << ALIGNMENT_SHIFT);
    static const size_t ALIGNMENT_MASK = (ALIGNMENT - 1);

#ifdef _DEBUG

    // These are set only for MethodDescs but every time we want to use the debugger
    // to examine these fields, the code has the thing stored in a MethodDesc*.
    // So...
    LPCUTF8         m_pszDebugMethodName;
    LPCUTF8         m_pszDebugClassName;
    LPCUTF8         m_pszDebugMethodSignature;
    PTR_MethodTable m_pDebugMethodTable;

    PTR_GCCoverageInfo m_GcCover;

#endif // _DEBUG

    inline BOOL HasStableEntryPoint()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return (m_wFlags3AndTokenRemainder & enum_flag3_HasStableEntryPoint) != 0;
    }

    inline PCODE GetStableEntryPoint()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        _ASSERTE(HasStableEntryPoint());
        _ASSERTE(!IsVersionableWithVtableSlotBackpatch());

        return GetMethodEntryPoint();
    }

    void SetMethodEntryPoint(PCODE addr);
    BOOL SetStableEntryPointInterlocked(PCODE addr);

    PCODE GetTemporaryEntryPoint();

    void SetTemporaryEntryPoint(LoaderAllocator *pLoaderAllocator, AllocMemTracker *pamTracker);

    PCODE GetInitialEntryPointForCopiedSlot()
    {
        WRAPPER_NO_CONTRACT;

        if (IsVersionableWithVtableSlotBackpatch())
        {
            return GetTemporaryEntryPoint();
        }
        return GetMethodEntryPoint();
    }

    inline BOOL HasPrecode()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return (m_wFlags3AndTokenRemainder & enum_flag3_HasPrecode) != 0;
    }

    inline Precode* GetPrecode()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        PRECONDITION(HasPrecode());
        Precode* pPrecode = Precode::GetPrecodeFromEntryPoint(GetStableEntryPoint());
        PREFIX_ASSUME(pPrecode != NULL);
        return pPrecode;
    }

    inline bool MayHavePrecode()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END

        // Ideally, methods that will not have native code (!MayHaveNativeCode() == true) should not be versionable. Currently,
        // that is not the case, in some situations it was seen that 1/4 to 1/3 of versionable methods do not have native
        // code, though there is no significant overhead from this. MayHaveNativeCode() appears to be an expensive check to do
        // for each MethodDesc, even if it's done only once, and when it was attempted, at the time it was showing up noticeably
        // in startup performance profiles.
        //
        // In particular, methods versionable with vtable slot backpatch should not have a precode (in the sense HasPrecode()
        // must return false) even if they will not have native code.
        bool result = IsVersionable() ? IsVersionableWithPrecode() : !MayHaveNativeCode();
        _ASSERTE(!result || !IsVersionableWithVtableSlotBackpatch());
        return result;
    }

    Precode* GetOrCreatePrecode();

    // Given a code address return back the MethodDesc whenever possible
    //
    static MethodDesc *  GetMethodDescFromStubAddr(PCODE addr, BOOL fSpeculative = FALSE);


    DWORD GetAttrs() const;

    DWORD GetImplAttrs();

    // This function can lie if a method impl was used to implement
    // more than one method on this class. Use GetName(int) to indicate
    // which slot you are interested in.
    // See the TypeString class for better control over name formatting.
    LPCUTF8 GetName();

    LPCUTF8 GetName(USHORT slot);

    LPCUTF8 GetNameThrowing();

    FORCEINLINE LPCUTF8 GetNameOnNonArrayClass()
    {
        WRAPPER_NO_CONTRACT;
        LPCSTR  szName;
        if (FAILED(GetMDImport()->GetNameOfMethodDef(GetMemberDef(), &szName)))
        {
            szName = NULL;
        }
        return szName;
    }

    COUNT_T GetStableHash();

    // Non-zero for InstantiatedMethodDescs
    DWORD GetNumGenericMethodArgs();

    // Return the number of class type parameters that are in scope for this method
    DWORD GetNumGenericClassArgs()
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;
        return GetMethodTable()->GetNumGenericArgs();
    }

    // True if this is a method descriptor for an instantiated generic method
    // whose method type arguments are the formal type parameters of the generic method
    // NOTE: the declaring class may not be the generic type definition e.g. consider C<int>.m<T>
    BOOL IsGenericMethodDefinition() const;

    // True if the declaring type or instantiation of method (if any) contains formal generic type parameters
    BOOL ContainsGenericVariables();

    Module* GetDefiningModuleForOpenMethod();

    // True if this is a class and method instantiation that on <__Canon,...,__Canon>
    BOOL IsTypicalSharedInstantiation();


    // True if and only if this is a method descriptor for:
    // 1. a non-generic method or a generic method at its typical method instantiation
    // 2. in a non-generic class or a typical instantiation of a generic class
    // This method can be called on a non-restored method desc
    BOOL IsTypicalMethodDefinition() const;

    // Force a load of the (typical) constraints on the type parameters of a typical method definition,
    // detecting cyclic bounds on class and method type parameters.
    void LoadConstraintsForTypicalMethodDefinition(BOOL *pfHasCircularClassConstraints,
                                                   BOOL *pfHasCircularMethodConstraints,
                                                   ClassLoadLevel level = CLASS_LOADED);

    DWORD IsClassConstructor()
    {
        WRAPPER_NO_CONTRACT;
        return IsMdClassConstructor(GetAttrs(), GetName());
    }

    DWORD IsClassConstructorOrCtor()
    {
        WRAPPER_NO_CONTRACT;
        DWORD dwAttrs = GetAttrs();
        if (IsMdRTSpecialName(dwAttrs))
        {
            LPCUTF8 name = GetName();
            return IsMdInstanceInitializer(dwAttrs, name) || IsMdClassConstructor(dwAttrs, name);
        }
        return FALSE;
    }

    inline void SetHasMethodImplSlot()
    {
        m_wFlags |= mdcMethodImpl;
    }

    inline BOOL HasMethodImplSlot()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return (mdcMethodImpl & m_wFlags);
    }

    FORCEINLINE BOOL IsMethodImpl()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        // Once we stop allocating dummy MethodImplSlot in MethodTableBuilder::WriteMethodImplData,
        // the check for NULL will become unnecessary.
        return HasMethodImplSlot() && (GetMethodImpl()->GetSlots() != NULL);
    }

    inline DWORD IsStatic()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        // This bit caches the IsMdStatic(GetAttrs()) check. We used to assert it here, but not doing it anymore. GetAttrs()
        // accesses metadata that is not compatible with contracts of this method. The metadata access can fail, the metadata
        // are not available during shutdown, the metadata access can take locks. It is not worth it to code around all these
        // just for the assert.
        // _ASSERTE((((m_wFlags & mdcStatic) != 0) == (IsMdStatic(flags) != 0)));

        return (m_wFlags & mdcStatic) != 0;
    }

    inline void SetStatic()
    {
        LIMITED_METHOD_CONTRACT;
        m_wFlags |= mdcStatic;
    }

    inline void ClearStatic()
    {
        LIMITED_METHOD_CONTRACT;
        m_wFlags &= ~mdcStatic;
    }

    inline BOOL IsIL()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return mcIL == GetClassification()  || mcInstantiated == GetClassification();
    }

    //================================================================
    // Generics-related predicates etc.

    // True if the method descriptor is an instantiation of a generic method.
    inline BOOL HasMethodInstantiation() const;

    // True if the method descriptor is either an instantiation of
    // a generic method or is an instance method in an instantiated class (or both).
    BOOL HasClassOrMethodInstantiation()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return (HasClassInstantiation() || HasMethodInstantiation());
    }

    inline BOOL HasClassInstantiation() const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return GetMethodTable()->HasInstantiation();
    }

    // Return the instantiation for an instantiated generic method
    // Return NULL if not an instantiated method
    // To get the (representative) instantiation of the declaring class use GetMethodTable()->GetInstantiation()
    // NOTE: This will assert if you try to get the instantiation of a generic method def in a non-typical class
    // e.g. C<int>.m<U> will fail but C<T>.m<U> will succeed
    Instantiation GetMethodInstantiation() const;

    // As above, but will succeed on C<int>.m<U>
    // To do this it might force a load of the typical parent
    Instantiation LoadMethodInstantiation();

    // Return a pointer to the method dictionary for an instantiated generic method
    // The initial slots in a method dictionary are the type arguments themselves
    // Return NULL if not an instantiated method
    Dictionary* GetMethodDictionary();
    DictionaryLayout* GetDictionaryLayout();

    InstantiatedMethodDesc* AsInstantiatedMethodDesc() const;

#ifdef FEATURE_CODE_VERSIONING
    CodeVersionManager* GetCodeVersionManager();
#endif

    MethodDescBackpatchInfoTracker* GetBackpatchInfoTracker();

    PTR_LoaderAllocator GetLoaderAllocator();

    // GetDomainSpecificLoaderAllocator returns the collectable loader allocator for collectable types
    // and the loader allocator in the current domain for non-collectable types
    LoaderAllocator * GetDomainSpecificLoaderAllocator();

    Module* GetLoaderModule();

    // Strip off method and class instantiation if present and replace by the typical instantiation
    // e.g. C<int>.m<string> -> C<T>.m<U>.  Does not modify the MethodDesc, but returns
    // the appropriate stripped MethodDesc.
    // This is the identity function on non-instantiated method descs in non-instantiated classes
    MethodDesc* LoadTypicalMethodDefinition();

    // Strip off the method instantiation (if present) and replace by the typical instantiation
    // e.g. // C<int>.m<string> -> C<int>.m<U>.   Does not modify the MethodDesc, but returns
    // the appropriate stripped MethodDesc.
    // This is the identity function on non-instantiated method descs
    MethodDesc* StripMethodInstantiation();

    // Return the instantiation of a method's enclosing class
    // Return NULL if the enclosing class is not instantiated
    // If the method code is shared then this might be a *representative* instantiation
    //
    // See GetExactClassInstantiation if you need to get the exact
    // instantiation of a shared method desc.
    Instantiation GetClassInstantiation() const;

    // Is the code shared between multiple instantiations of class or method?
    // If so, then when compiling the code we might need to look up tokens
    // in the class or method dictionary.  Also, when debugging the exact generic arguments
    // need to be ripped off the stack, either from the this pointer or from one of the
    // extra args below.
    BOOL IsSharedByGenericInstantiations(); // shared code of any kind

    BOOL IsSharedByGenericMethodInstantiations(); // shared due to method instantiation

    // How does a method shared between generic instantiations get at
    // the extra instantiation information at runtime?  Only one of the following three
    // will ever hold:
    //
    // AcquiresInstMethodTableFromThis()
    //    The method is in a generic class but is not itself a
    // generic method (the normal case). Furthermore a "this" pointer
    // is available and we can get the exact instantiation from it.
    //
    // RequiresInstMethodTableArg()
    //    The method is shared between generic classes but is not
    // itself generic.  Furthermore no "this" pointer is given
    // (e.g. a value type method), so we pass in the exact-instantiation
    // method table as an extra argument.
    //   i.e. per-inst static methods in shared-code instantiated generic
    //        classes (e.g. static void MyClass<string>::m())
    //   i.e. shared-code instance methods in instantiated generic
    //        structs (e.g. void MyValueType<string>::m())
    //
    // RequiresInstMethodDescArg()
    //    The method is itself generic and is shared between generic
    // instantiations but is not itself generic.  Furthermore
    // no "this" pointer is given (e.g. a value type method), so we pass in the
    // exact-instantiation method table as an extra argument.
    //   i.e. shared-code instantiated generic methods
    //
    // These are used for direct calls to instantiated generic methods
    //     e.g. call void C::m<string>()  implemented by calculating dict(m<string>) at compile-time and passing it as an extra parameter
    //          call void C::m<!0>()      implemented by calculating dict(m<!0>) at run-time (if the caller lives in shared-class code)

    BOOL AcquiresInstMethodTableFromThis();
    BOOL RequiresInstMethodTableArg();
    BOOL RequiresInstMethodDescArg();
    BOOL RequiresInstArg();

    // Can this method handle be given out to reflection for use in a MethodInfo
    // object?
    BOOL IsRuntimeMethodHandle();

    // Given a method table of an object and a method that comes from some
    // superclass of the class of that object, find that superclass.
    MethodTable * GetExactDeclaringType(MethodTable * ownerOrSubType);

    // Given a type handle of an object and a method that comes from some
    // superclass of the class of that object, find the instantiation of
    // that superclass, i.e. the class instantiation which will be relevant
    // to interpreting the signature of the method.  The type handle of
    // the object does not need to be given in all circumstances, in
    // particular it is only needed for MethodDescs pMD that
    // return true for pMD->RequiresInstMethodTableArg() or
    // pMD->RequiresInstMethodDescArg(). In other cases it is
    // allowed to be null.
    //
    // Will return NULL if the method is not in a generic class.
    Instantiation GetExactClassInstantiation(TypeHandle possibleObjType);


    BOOL SatisfiesMethodConstraints(TypeHandle thParent, BOOL fThrowIfNotSatisfied = FALSE);


    BOOL HasSameMethodDefAs(MethodDesc * pMD);

    //================================================================
    // Classifications of kinds of MethodDescs.

    inline BOOL IsRuntimeSupplied()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return mcFCall == GetClassification()
            || mcArray == GetClassification();
    }

    inline DWORD IsArray() const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return mcArray == GetClassification();
    }

    inline DWORD IsEEImpl() const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return mcEEImpl == GetClassification();
    }

    inline DWORD IsNoMetadata() const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return (mcDynamic == GetClassification());
    }

    inline PTR_DynamicMethodDesc AsDynamicMethodDesc();
    inline bool IsDynamicMethod();
    inline bool IsILStub();
    inline bool IsLCGMethod();

    inline DWORD IsNDirect()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return mcNDirect == GetClassification();
    }

    inline DWORD IsInterface()
    {
        WRAPPER_NO_CONTRACT;
        return GetMethodTable()->IsInterface();
    }

    BOOL HasUnmanagedCallersOnlyAttribute();
    BOOL ShouldSuppressGCTransition();

#ifdef FEATURE_COMINTEROP
    inline DWORD IsComPlusCall()
    {
        WRAPPER_NO_CONTRACT;
        return mcComInterop == GetClassification();
    }
#else // !FEATURE_COMINTEROP
     // hardcoded to return FALSE to improve code readability
    inline DWORD IsComPlusCall()
    {
        LIMITED_METHOD_CONTRACT;
        return FALSE;
    }
#endif // !FEATURE_COMINTEROP

    // Update flags in a thread safe manner.
    WORD InterlockedUpdateFlags(WORD wMask, BOOL fSet);

    // If the method is in an Edit and Continue (EnC) module, then
    // we DON'T want to backpatch this, ever.  We MUST always call
    // through the precode so that we can update the method.
    inline DWORD InEnCEnabledModule()
    {
        WRAPPER_NO_CONTRACT;
        Module *pModule = GetModule();
        PREFIX_ASSUME(pModule != NULL);
        return pModule->IsEditAndContinueEnabled();
    }

    inline BOOL IsNotInline()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_wFlags & mdcNotInline);
    }

    inline void SetNotInline(BOOL set)
    {
        WRAPPER_NO_CONTRACT;
        InterlockedUpdateFlags(mdcNotInline, set);
    }

#ifndef DACCESS_COMPILE
    VOID EnsureActive();
#endif
    CHECK CheckActivated();

    //================================================================
    // FCalls.
    BOOL IsFCall()
    {
        WRAPPER_NO_CONTRACT;
        return mcFCall == GetClassification();
    }

    BOOL IsQCall();

    //================================================================
    //

    inline void ClearFlagsOnUpdate()
    {
        WRAPPER_NO_CONTRACT;
        SetNotInline(FALSE);
    }

    // Restore the MethodDesc to it's initial, pristine state, so that
    // it can be reused for new code (eg. for EnC, method rental, etc.)
    //
    // Things to think about before calling this:
    //
    // Does the caller need to free up the jitted code for the old IL
    // (including any other IJitManager datastructures) ?
    // Does the caller guarantee thread-safety ?
    //
    void Reset();

    //================================================================
    // About the signature.

    BOOL IsVarArg();
    BOOL IsVoid();
    BOOL HasRetBuffArg();

    // Returns the # of bytes of stack used by arguments. Does not include
    // arguments passed in registers.
    UINT SizeOfArgStack();

    // Returns the # of bytes of stack used by arguments in a call from native to this function.
    // Does not include arguments passed in registers.
    UINT SizeOfNativeArgStack();

    // Returns the # of bytes to pop after a call. Not necessary the
    // same as SizeOfArgStack()!
    UINT CbStackPop();

    //================================================================
    // Unboxing stubs.
    //
    // Return TRUE if this is this a special stub used to implement delegates to an
    // instance method in a value class and/or virtual methods on a value class.
    //
    // For every BoxedEntryPointStub there is associated unboxed-this-MethodDesc
    // which accepts an unboxed "this" pointer.
    //
    // The action of a typical BoxedEntryPointStub is to
    // bump up the this pointer by one word so that it points to the interior of the object
    // and then call the underlying unboxed-this-MethodDesc.
    //
    // Additionally, if the non-BoxedEntryPointStub is RequiresInstMethodTableArg()
    // then pass on the MethodTable as an extra argument to the
    // underlying unboxed-this-MethodDesc.
    BOOL IsUnboxingStub()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return (m_wFlags3AndTokenRemainder & enum_flag3_IsUnboxingStub) != 0;
    }

    void SetIsUnboxingStub()
    {
        LIMITED_METHOD_CONTRACT;
        m_wFlags3AndTokenRemainder |= enum_flag3_IsUnboxingStub;
    }


    //================================================================
    // Instantiating Stubs
    //
    // Return TRUE if this is this a special stub used to implement an
    // instantiated generic method or per-instantiation static method.
    // The action of an instantiating stub is
    // * pass on a MethodTable or InstantiatedMethodDesc extra argument to shared code
    BOOL IsInstantiatingStub();


    // A wrapper stub is either an unboxing stub or an instantiating stub
    BOOL IsWrapperStub();
    MethodDesc *GetWrappedMethodDesc();
    MethodDesc *GetExistingWrappedMethodDesc();

    //==================================================================
    // Access the underlying metadata

    BOOL HasILHeader()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;
        return IsIL() && !IsUnboxingStub() && GetRVA();
    }

    COR_ILMETHOD* GetILHeader(BOOL fAllowOverrides = FALSE);

    BOOL HasStoredSig()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return IsEEImpl() || IsArray() || IsNoMetadata();
    }

    PCCOR_SIGNATURE GetSig();

    void GetSig(PCCOR_SIGNATURE *ppSig, DWORD *pcSig);
    SigParser GetSigParser();

    // Convenience methods for common signature wrapper types.
    SigPointer GetSigPointer();
    Signature GetSignature();


    void GetSigFromMetadata(IMDInternalImport * importer,
                            PCCOR_SIGNATURE   * ppSig,
                            DWORD             * pcSig);


    IMDInternalImport* GetMDImport() const
    {
        WRAPPER_NO_CONTRACT;
        Module *pModule = GetModule();
        PREFIX_ASSUME(pModule != NULL);
        return pModule->GetMDImport();
    }

    HRESULT GetCustomAttribute(WellKnownAttribute attribute,
                               const void  **ppData,
                               ULONG *pcbData) const
    {
        WRAPPER_NO_CONTRACT;
        Module *pModule = GetModule();
        PREFIX_ASSUME(pModule != NULL);
        return pModule->GetCustomAttribute(GetMemberDef(), attribute, ppData, pcbData);
    }

#ifndef DACCESS_COMPILE
    IMetaDataEmit* GetEmitter()
    {
        WRAPPER_NO_CONTRACT;
        Module *pModule = GetModule();
        PREFIX_ASSUME(pModule != NULL);
        return pModule->GetEmitter();
    }

    IMetaDataImport* GetRWImporter()
    {
        WRAPPER_NO_CONTRACT;
        Module *pModule = GetModule();
        PREFIX_ASSUME(pModule != NULL);
        return pModule->GetRWImporter();
    }
#endif // !DACCESS_COMPILE

#ifdef FEATURE_COMINTEROP
    WORD GetComSlot();
    LONG GetComDispid();
#endif // FEATURE_COMINTEROP

    inline DWORD IsCtor()
    {
        WRAPPER_NO_CONTRACT;
        return IsMdInstanceInitializer(GetAttrs(), GetName());
    }

    inline DWORD IsFinal()
    {
        WRAPPER_NO_CONTRACT;
        return IsMdFinal(GetAttrs());
    }

    inline DWORD IsPrivate()
    {
        WRAPPER_NO_CONTRACT;
        return IsMdPrivate(GetAttrs());
    }

    inline DWORD IsPublic() const
    {
        WRAPPER_NO_CONTRACT;
        return IsMdPublic(GetAttrs());
    }

    inline DWORD IsProtected() const
    {
        WRAPPER_NO_CONTRACT;
        return IsMdFamily(GetAttrs());
    }

    inline DWORD IsVirtual()
    {
        WRAPPER_NO_CONTRACT;
        return IsMdVirtual(GetAttrs());
    }

    inline DWORD IsAbstract()
    {
        WRAPPER_NO_CONTRACT;
        return IsMdAbstract(GetAttrs());
    }

    //==================================================================
    // Flags..

    inline void SetSynchronized()
    {
        LIMITED_METHOD_CONTRACT;
        m_wFlags |= mdcSynchronized;
    }

    inline DWORD IsSynchronized()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return (m_wFlags & mdcSynchronized) != 0;
    }

    //==================================================================
    // The MethodDesc in relation to the VTable it is associated with.
    // WARNING: Not all MethodDescs have slots, nor do they all have
    // entries in MethodTables.  Beware.

    // Does the method has virtual slot? Note that methods implementing interfaces
    // on value types do not have virtual slots, but they are marked as virtual in metadata.
    inline BOOL IsVtableMethod()
    {
        LIMITED_METHOD_CONTRACT;
        MethodTable *pMT = GetMethodTable();
        return
            !IsEnCAddedMethod()
            // The slot numbers are currently meaningless for
            // some unboxed-this-generic-method-instantiations
            && !(pMT->IsValueType() && !IsStatic() && !IsUnboxingStub())
            && GetSlot() < pMT->GetNumVirtuals();
    }

    // Is this a default interface method (virtual non-abstract instance method)
    inline BOOL IsDefaultInterfaceMethod()
    {
        LIMITED_METHOD_CONTRACT;

#ifdef FEATURE_DEFAULT_INTERFACES
        return (GetMethodTable()->IsInterface() && !IsStatic() && IsVirtual() && !IsAbstract());
#else
        return false;
#endif // FEATURE_DEFAULT_INTERFACES
    }

    inline BOOL HasNonVtableSlot();

    void SetHasNonVtableSlot()
    {
        LIMITED_METHOD_CONTRACT;
        m_wFlags |= mdcHasNonVtableSlot;
    }

    // duplicate methods
    inline BOOL  IsDuplicate()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_wFlags & mdcDuplicate) == mdcDuplicate;
    }

    void SetDuplicate()
    {
        LIMITED_METHOD_CONTRACT;
        // method table is not setup yet
        //_ASSERTE(!GetClass()->IsInterface());
        m_wFlags |= mdcDuplicate;
    }

    //==================================================================
    //

    inline EEClass* GetClass()
    {
        WRAPPER_NO_CONTRACT;
        MethodTable *pMT = GetMethodTable();
        EEClass *pClass = pMT->GetClass();
        PREFIX_ASSUME(pClass != NULL);
        return pClass;
    }

    inline PTR_MethodTable GetMethodTable() const;

    inline DPTR(PTR_MethodTable) GetMethodTablePtr() const;

  public:
    inline MethodDescChunk* GetMethodDescChunk() const;
    inline int GetMethodDescChunkIndex() const;
    // If this is an method desc. (whether non-generic shared-instantiated or exact-instantiated)
    // inside a shared class then get the method table for the representative
    // class.
    inline MethodTable* GetCanonicalMethodTable();

    Module *GetModule() const;

    Assembly *GetAssembly() const
    {
        WRAPPER_NO_CONTRACT;
        Module *pModule = GetModule();
        PREFIX_ASSUME(pModule != NULL);
        return pModule->GetAssembly();
    }

    //==================================================================
    // The slot number of this method in the corresponding method table.
    //
    // Use with extreme caution.  The slot number will not be
    // valid for EnC code or for MethodDescs representing instantiation
    // of generic methods.  It may also not mean what you think it will mean
    // for strange method descs such as BoxedEntryPointStubs.
    //
    // In any case we should be moving to use slot numbers a lot less
    // since they make the EE code inflexible.

    inline WORD GetSlot()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_wSlotNumber;
    }

    inline VOID SetSlot(WORD wSlotNum)
    {
        LIMITED_METHOD_CONTRACT;
        m_wSlotNumber = wSlotNum;
    }

    inline BOOL IsVirtualSlot()
    {
        return GetSlot() < GetMethodTable()->GetNumVirtuals();
    }
    inline BOOL IsVtableSlot()
    {
        return IsVirtualSlot() && !HasNonVtableSlot();
    }

    PTR_PCODE GetAddrOfSlot();

    PTR_MethodDesc GetDeclMethodDesc(UINT32 slotNumber);

public:
    mdMethodDef GetMemberDef() const;

#ifdef _DEBUG
    BOOL SanityCheck();
#endif // _DEBUG

public:

    void SetMemberDef(mdMethodDef mb);

    //================================================================
    // Set the offset of this method desc in a chunk table (which allows us
    // to work back to the method table/module pointer stored at the head of
    // the table.
    void SetChunkIndex(MethodDescChunk *pChunk);

    BOOL IsPointingToPrestub();

public:

    // True iff it is possible to change the code this method will run using the CodeVersionManager. Note: EnC currently returns
    // false here because it uses its own separate scheme to manage versionability. We will likely want to converge them at some
    // point.
    bool IsVersionable()
    {
        WRAPPER_NO_CONTRACT;
        return IsEligibleForTieredCompilation() || IsEligibleForReJIT();
    }

    // True iff all calls to the method should funnel through a Precode which can be updated to point to the current method
    // body. This versioning technique can introduce more indirections than optimal but it has low memory overhead when a
    // FixupPrecode may be shared with the temporary entry point that is created anyway.
    bool IsVersionableWithPrecode()
    {
        WRAPPER_NO_CONTRACT;
        return IsVersionable() && !Helper_IsEligibleForVersioningWithVtableSlotBackpatch();
    }

    // True iff all calls to the method should go through a backpatchable vtable slot or through a FuncPtrStub. This versioning
    // technique eliminates extra indirections from precodes but is more memory intensive to track all the appropriate slots.
    // See Helper_IsEligibleForVersioningWithEntryPointSlotBackpatch() for more details.
    bool IsVersionableWithVtableSlotBackpatch()
    {
        WRAPPER_NO_CONTRACT;
        return IsVersionable() && Helper_IsEligibleForVersioningWithVtableSlotBackpatch();
    }

    bool IsEligibleForReJIT()
    {
        WRAPPER_NO_CONTRACT;

#ifdef FEATURE_REJIT
        return
            ReJitManager::IsReJITEnabled() &&

            // Previously we didn't support these methods because of functional requirements for
            // jumpstamps, keeping this in for back compat.
            IsIL() &&
            !IsWrapperStub() &&

            // Functional requirement
            CodeVersionManager::IsMethodSupported(PTR_MethodDesc(this));
#else // FEATURE_REJIT
        return false;
#endif
    }

public:

    bool IsEligibleForTieredCompilation()
    {
        LIMITED_METHOD_DAC_CONTRACT;

#ifdef FEATURE_TIERED_COMPILATION
        return (m_wFlags3AndTokenRemainder & enum_flag3_IsEligibleForTieredCompilation) != 0;
#else
        return false;
#endif
    }

    // This method must return the same value for all methods in one MethodDescChunk
    bool DetermineIsEligibleForTieredCompilationInvariantForAllMethodsInChunk();

    // Is this method allowed to be recompiled and the entrypoint redirected so that we
    // can optimize its performance? Eligibility is invariant for the lifetime of a method.

    bool DetermineAndSetIsEligibleForTieredCompilation();

    bool IsJitOptimizationDisabled();
    bool IsJitOptimizationDisabledForAllMethodsInChunk();
    bool IsJitOptimizationDisabledForSpecificMethod();
    bool IsJitOptimizationLevelRequested();

private:
    // This function is not intended to be called in most places, and is named as such to discourage calling it accidentally
    bool Helper_IsEligibleForVersioningWithVtableSlotBackpatch()
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(IsVersionable());
        _ASSERTE(IsIL() || IsDynamicMethod());

#if defined(FEATURE_CODE_VERSIONING)
        _ASSERTE(CodeVersionManager::IsMethodSupported(PTR_MethodDesc(this)));

        // For a method eligible for code versioning and vtable slot backpatch:
        //   - It does not have a precode (HasPrecode() returns false)
        //   - It does not have a stable entry point (HasStableEntryPoint() returns false)
        //   - A call to the method may be:
        //     - An indirect call through the MethodTable's backpatchable vtable slot
        //     - A direct call to a backpatchable FuncPtrStub, perhaps through a JumpStub
        //     - For interface methods, an indirect call through the virtual stub dispatch (VSD) indirection cell to a
        //       backpatchable DispatchStub or a ResolveStub that refers to a backpatchable ResolveCacheEntry
        //   - The purpose is that typical calls to the method have no additional overhead when code versioning is enabled
        //
        // Recording and backpatching slots:
        //   - In order for all vtable slots for the method to be backpatchable:
        //     - A vtable slot initially points to the MethodDesc's temporary entry point, even when the method is inherited by
        //       a derived type (the slot's value is not copied from the parent)
        //     - The temporary entry point always points to the prestub and is never backpatched, in order to be able to
        //       discover new vtable slots through which the method may be called
        //     - The prestub, as part of DoBackpatch(), records any slots that are transitioned from the temporary entry point
        //       to the method's at-the-time current, non-prestub entry point
        //     - Any further changes to the method's entry point cause recorded slots to be backpatched in
        //       BackpatchEntryPointSlots()
        //   - In order for the FuncPtrStub to be backpatchable:
        //     - After the FuncPtrStub is created and exposed, it is patched to point to the method's at-the-time current entry
        //       point if necessary
        //     - Any further changes to the method's entry point cause the FuncPtrStub to be backpatched in
        //       BackpatchEntryPointSlots()
        //   - In order for VSD entities to be backpatchable:
        //     - A DispatchStub's entry point target is aligned and recorded for backpatching in BackpatchEntryPointSlots()
        //     - A ResolveCacheEntry's entry point target is recorded for backpatching in BackpatchEntryPointSlots()
        //
        // Slot lifetime and management of recorded slots:
        //   - A slot is recorded in the LoaderAllocator in which the slot is allocated, see
        //     RecordAndBackpatchEntryPointSlot()
        //   - An inherited slot that has a shorter lifetime than the MethodDesc, when recorded, needs to be accessible by the
        //     MethodDesc for backpatching, so the dependent LoaderAllocator with the slot to backpatch is also recorded in the
        //     MethodDesc's LoaderAllocator, see
        //     MethodDescBackpatchInfo::AddDependentLoaderAllocator_Locked()
        //   - At the end of a LoaderAllocator's lifetime, the LoaderAllocator is unregistered from dependency LoaderAllocators,
        //     see MethodDescBackpatchInfoTracker::ClearDependencyMethodDescEntryPointSlots()
        //   - When a MethodDesc's entry point changes, backpatching also includes iterating over recorded dependent
        //     LoaderAllocators to backpatch the relevant slots recorded there, see BackpatchEntryPointSlots()
        //
        // Synchronization between entry point changes and backpatching slots
        //   - A global lock is used to ensure that all recorded backpatchable slots corresponding to a MethodDesc point to the
        //     same entry point, see DoBackpatch() and BackpatchEntryPointSlots() for examples
        //
        // Typical slot value transitions when tiered compilation is enabled:
        //   - Initially, the slot contains the method's temporary entry point, which always points to the prestub (see above)
        //   - After the tier 0 JIT completes, the slot is transitioned to the tier 0 entry point, and the slot is recorded for
        //     backpatching
        //   - When tiered compilation decides to begin counting calls for the method, the slot is transitioned to the temporary
        //     entry point (call counting currently happens in the prestub)
        //   - When the call count reaches the tier 1 threshold, the slot is transitioned to the tier 0 entry point and a tier 1
        //     JIT is scheduled
        //   - After the tier 1 JIT completes, the slot is transitioned to the tier 1 entry point

        return
            // Policy
            g_pConfig->BackpatchEntryPointSlots() &&

            // Functional requirement - The entry point must be through a vtable slot in the MethodTable that may be recorded
            // and backpatched
            IsVtableSlot() &&

            // Functional requirement - True interface methods are not backpatched, see DoBackpatch()
            !(IsInterface() && !IsStatic());
#else
        // Entry point slot backpatch is disabled for CrossGen
        return false;
#endif
    }

public:
    bool MayHaveEntryPointSlotsToBackpatch()
    {
        WRAPPER_NO_CONTRACT;

        // This is the only case currently. In the future, a method that does not have a vtable slot may still record entry
        // point slots that need to be backpatched on entry point change, and in such cases the conditions here may be changed.
        return IsVersionableWithVtableSlotBackpatch();
    }


private:
    // Gets the prestub entry point to use for backpatching. Entry point slot backpatch uses this entry point as an oracle to
    // determine if the entry point actually changed and warrants backpatching.
    PCODE GetPrestubEntryPointToBackpatch()
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(MayHaveEntryPointSlotsToBackpatch());

        // At the moment this is the only case, see MayHaveEntryPointSlotsToBackpatch()
        _ASSERTE(IsVersionableWithVtableSlotBackpatch());
        return GetTemporaryEntryPoint();
    }

    // Gets the entry point stored in the primary storage location for backpatching. Entry point slot backpatch uses this entry
    // point as an oracle to determine if the entry point actually changed and warrants backpatching.
    PCODE GetEntryPointToBackpatch_Locked()
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(MethodDescBackpatchInfoTracker::IsLockOwnedByCurrentThread());
        _ASSERTE(MayHaveEntryPointSlotsToBackpatch());

        // At the moment this is the only case, see MayHaveEntryPointSlotsToBackpatch()
        _ASSERTE(IsVersionableWithVtableSlotBackpatch());
        return GetMethodEntryPoint();
    }

    // Sets the entry point stored in the primary storage location for backpatching. Entry point slot backpatch uses this entry
    // point as an oracle to determine if the entry point actually changed and warrants backpatching.
    void SetEntryPointToBackpatch_Locked(PCODE entryPoint)
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(MethodDescBackpatchInfoTracker::IsLockOwnedByCurrentThread());
        _ASSERTE(entryPoint != NULL);
        _ASSERTE(MayHaveEntryPointSlotsToBackpatch());

        // At the moment this is the only case, see MayHaveEntryPointSlotsToBackpatch(). If that changes in the future, this
        // function may have to handle other cases in SetCodeEntryPoint().
        _ASSERTE(IsVersionableWithVtableSlotBackpatch());
        SetMethodEntryPoint(entryPoint);
    }

public:
    void RecordAndBackpatchEntryPointSlot(LoaderAllocator *slotLoaderAllocator, TADDR slot, EntryPointSlots::SlotType slotType);
private:
    void RecordAndBackpatchEntryPointSlot_Locked(LoaderAllocator *mdLoaderAllocator, LoaderAllocator *slotLoaderAllocator, TADDR slot, EntryPointSlots::SlotType slotType, PCODE currentEntryPoint);

public:
    bool TryBackpatchEntryPointSlotsFromPrestub(PCODE entryPoint)
    {
        WRAPPER_NO_CONTRACT;
        return TryBackpatchEntryPointSlots(entryPoint, false /* isPrestubEntryPoint */, true /* onlyFromPrestubEntryPoint */);
    }

    void BackpatchEntryPointSlots(PCODE entryPoint)
    {
        WRAPPER_NO_CONTRACT;
        BackpatchEntryPointSlots(entryPoint, false /* isPrestubEntryPoint */);
    }

    void BackpatchToResetEntryPointSlots()
    {
        WRAPPER_NO_CONTRACT;
        BackpatchEntryPointSlots(GetPrestubEntryPointToBackpatch(), true /* isPrestubEntryPoint */);
    }

private:
    void BackpatchEntryPointSlots(PCODE entryPoint, bool isPrestubEntryPoint)
    {
        WRAPPER_NO_CONTRACT;

#ifdef _DEBUG // workaround for release build unused variable error
        bool success =
#endif
            TryBackpatchEntryPointSlots(entryPoint, isPrestubEntryPoint, false /* onlyFromPrestubEntryPoint */);
        _ASSERTE(success);
    }

    bool TryBackpatchEntryPointSlots(PCODE entryPoint, bool isPrestubEntryPoint, bool onlyFromPrestubEntryPoint);

public:
    void TrySetInitialCodeEntryPointForVersionableMethod(PCODE entryPoint, bool mayHaveEntryPointSlotsToBackpatch);
    void SetCodeEntryPoint(PCODE entryPoint);
    void ResetCodeEntryPoint();
    void ResetCodeEntryPointForEnC();


public:
    bool RequestedAggressiveOptimization()
    {
        WRAPPER_NO_CONTRACT;

        return
            IsIL() && // only makes sense for IL methods, and this implies !IsNoMetadata()
            IsMiAggressiveOptimization(GetImplAttrs());
    }

    // Does this method force the NativeCodeSlot to stay fixed after it
    // is first initialized to native code? Consumers of the native code
    // pointer need to be very careful about if and when they cache it
    // if it is not stable.
    //
    // The stability of the native code pointer is separate from the
    // stability of the entrypoint. A stable entrypoint can be a precode
    // which dispatches to an unstable native code pointer.
    BOOL IsNativeCodeStableAfterInit()
    {
        LIMITED_METHOD_DAC_CONTRACT;

#if defined(FEATURE_JIT_PITCHING)
        if (IsPitchable())
            return false;
#endif

        return !IsVersionable() && !InEnCEnabledModule();
    }

    //Is this method currently pointing to native code that will never change?
    BOOL IsPointingToStableNativeCode()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        if (!IsNativeCodeStableAfterInit())
            return FALSE;

        return IsPointingToNativeCode();
    }

    // Note: We are skipping the prestub based on addition information from the JIT.
    // (e.g. that the call is on same this ptr or that the this ptr is not null).
    // Thus we can end up with a running NGENed method for which IsPointingToNativeCode is false!
    BOOL IsPointingToNativeCode()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        if (!HasStableEntryPoint())
            return FALSE;

        if (!HasPrecode())
            return TRUE;

        return GetPrecode()->IsPointingToNativeCode(GetNativeCode());
    }

    // Be careful about races with profiler when using this method. The profiler can
    // replace preimplemented code of the method with jitted code.
    // Avoid code patterns like if(HasNativeCode()) { PCODE pCode = GetNativeCode(); ... }.
    // Use PCODE pCode = GetNativeCode(); if (pCode != NULL) { ... } instead.
    BOOL HasNativeCode()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return GetNativeCode() != NULL;
    }

    // Perf warning: takes the CodeVersionManagerLock on every call
    BOOL HasNativeCodeAnyVersion()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return GetNativeCodeAnyVersion() != NULL;
    }

    BOOL SetNativeCodeInterlocked(PCODE addr, PCODE pExpected = NULL);

    PTR_PCODE GetAddrOfNativeCodeSlot();

    BOOL MayHaveNativeCode();

    ULONG GetRVA();

public:
    // Returns address of code to call. The address is good for one immediate invocation only.
    // Use GetMultiCallableAddrOfCode() to get address that can be invoked multiple times.
    //
    // Only call GetSingleCallableAddrOfCode() if you can guarantee that no virtualization is
    // necessary, or if you can guarantee that it has already happened. For instance, the frame of a
    // stackwalk has obviously been virtualized as much as it will be.
    //
    PCODE GetSingleCallableAddrOfCode()
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(!IsGenericMethodDefinition());
        return GetMethodEntryPoint();
    }

    // This one is used to implement "ldftn".
    PCODE GetMultiCallableAddrOfCode(CORINFO_ACCESS_FLAGS accessFlags = CORINFO_ACCESS_LDFTN);

    // Internal version of GetMultiCallableAddrOfCode. Returns NULL if attempt to acquire directly
    // callable entrypoint would result into unnecessary allocation of indirection stub. Caller should use
    // indirect call via slot in this case.
    PCODE TryGetMultiCallableAddrOfCode(CORINFO_ACCESS_FLAGS accessFlags);

    // These return an address after resolving "virtual methods" correctly, including any
    // handling of context proxies, other thunking layers and also including
    // instantiation of generic virtual methods if required.
    // The first one returns an address which cannot be invoked
    // multiple times. Use GetMultiCallableAddrOfVirtualizedCode() for that.
    //
    // The code that implements these was taken verbatim from elsewhere in the
    // codebase, and there may be subtle differences between the two, e.g. with
    // regard to thunking.
    PCODE GetSingleCallableAddrOfVirtualizedCode(OBJECTREF *orThis, TypeHandle staticTH);
    PCODE GetMultiCallableAddrOfVirtualizedCode(OBJECTREF *orThis, TypeHandle staticTH);

    // The current method entrypoint. It is simply the value of the current method slot.
    // GetMethodEntryPoint() should be used to get an opaque method entrypoint, for instance
    // when copying or searching vtables. It should not be used to get address to call.
    //
    // GetSingleCallableAddrOfCode() and GetStableEntryPoint() are aliases with stricter preconditions.
    // Use of these aliases is as appropriate.
    //
    PCODE GetMethodEntryPoint();

    //*******************************************************************************
    // Returns the address of the native code.
    PCODE GetNativeCode();

    // Returns GetNativeCode() if it exists, but also checks to see if there
    // is a non-default code version that is populated with a code body and returns that.
    // Perf warning: takes the CodeVersionManagerLock on every call
    PCODE GetNativeCodeAnyVersion();

#if defined(FEATURE_JIT_PITCHING)
    bool IsPitchable();
    void PitchNativeCode();
#endif

    //================================================================
    // FindOrCreateAssociatedMethodDesc
    //
    // You might think that every MethodDef in the metadata had
    // one and only one MethodDesc in the source...  Well, how wrong
    // you are :-)
    //
    // Some MethodDefs can be associated with more than one MethodDesc.
    // This can happen because:
    //      (1) The method is an instance method in a struct, which
    //          can be called with either an unboxed "this" pointer or
    //          a "boxed" this pointer..  There is a different MethodDesc for
    //          these two cases.
    //      (2) The method is a generic method.  There is one primary
    //          MethodDesc for each generic method, called the GenericMethodDefinition.
    //          This is the one stored in the vtable.  New MethodDescs will
    //          be created for instantiations according to the scheme described
    //          elsewhere in this file.
    // There are also various other stubs associated with MethodDesc, but these stubs
    // do not result in new MethodDescs.
    //
    // All of the above MethodDescs are called "associates" of the primary MethodDesc.
    // Note that the primary MethodDesc for an instance method on a struct is
    // the one that accepts an unboxed "this" pointer.
    //
    // FindOrCreateAssociatedMethodDesc is the _primary_ routine
    // in the codebase for getting an associated MethodDesc from a primary MethodDesc.
    // You should treat this routine as a black box, i.e. just specify the right
    // parameters and it will do all the hard work of finding the right
    // MethodDesc for you.
    //
    // This routine can be used for "normal" MethodDescs that have nothing
    // to do with generics.  For example, if you need an BoxedEntryPointStub then
    // you may call this routine to get it.  It may also return
    // the Primary MethodDesc itself if that MethodDesc is suitable given the
    // parameters.
    //
    // NOTE: The behaviour of this method is not thoroughly defined
    // if pPrimaryMD is not really a "primary" MD.  Primary MDs are:
    //     1. Primary MDs are:never a generic method instantiation,
    //        but are instead the "uninstantiated" generic MD.
    //     2. Primary MDs are never instantiating stubs.
    //     3. Primary MDs are never BoxedEntryPointStubs.
    //
    // We assert if cases (1) or (2) occur.  However, some places in the
    // code pass in an BoxedEntryPointStub when pPrimaryMD is a virtual/interface method on
    // a struct.  These cases are confusing and should be rooted
    // out: it is probably preferable in terms
    // of correctness to pass in the corresponding non-unboxing MD.
    //
    // allowCreate may be set to FALSE to enforce that the method searched
    // should already be in existence - thus preventing creation and GCs during
    // inappropriate times.
    //
    static MethodDesc* FindOrCreateAssociatedMethodDesc(MethodDesc* pPrimaryMD,
                                                        MethodTable *pExactMT,
                                                        BOOL forceBoxedEntryPoint,
                                                        Instantiation methodInst,
                                                        BOOL allowInstParam,
                                                        BOOL forceRemotableMethod = FALSE,
                                                        BOOL allowCreate = TRUE,
                                                        ClassLoadLevel level = CLASS_LOADED);

    // Normalize methoddesc for reflection
    static MethodDesc* FindOrCreateAssociatedMethodDescForReflection(MethodDesc *pMethod,
                                                                     TypeHandle instType,
                                                                     Instantiation methodInst);

    // True if a MD is an funny BoxedEntryPointStub (not from the method table) or
    // an MD for a generic instantiation...In other words the MethodDescs and the
    // MethodTable are guaranteed to be "tightly-knit", i.e. if one is present in
    // an NGEN image then the other will be, and if one is "used" at runtime then
    // the other will be too.
    BOOL IsTightlyBoundToMethodTable();

    // For method descriptors which are non-generic this is the identity function
    // (except it returns the primary descriptor, not an BoxedEntryPointStub).
    //
    // For a generic method definition C<T>.m<U> this will return
    // C<__Canon>.m<__Canon>
    //
    // allowCreate may be set to FALSE to enforce that the method searched
    // should already be in existence - thus preventing creation and GCs during
    // inappropriate times.
    //
    MethodDesc * FindOrCreateTypicalSharedInstantiation(BOOL allowCreate = TRUE);

    // Given an object and an method descriptor for an instantiation of
    // a virtualized generic method, get the
    // corresponding instantiation of the target of a call.
    MethodDesc *ResolveGenericVirtualMethod(OBJECTREF *orThis);


private:
    ReturnKind ParseReturnKindFromSig(INDEBUG(bool supportStringConstructors = false));

public:
    // This method is used to restore ReturnKind using the class handle, it is fully supported only on x64 Ubuntu,
    // other platforms do not support multi-reg return case with pointers.
    // Use this method only when you can't hit this case
    // (like ComPlusMethodFrame::GcScanRoots) or when you can tolerate RT_Illegal return.
    // Also, on the other platforms for a single field struct return case
    // the function can't distinguish RT_Object and RT_ByRef.
    ReturnKind GetReturnKind(INDEBUG(bool supportStringConstructors = false));

public:
    // In general you don't want to call GetCallTarget - you want to
    // use either "call" directly or call MethodDesc::GetSingleCallableAddrOfVirtualizedCode and
    // then "CallTarget".  Note that GetCallTarget is approximately GetSingleCallableAddrOfCode
    // but the additional weirdness that class-based-virtual calls (but not interface calls nor calls
    // on proxies) are resolved to their target.  Because of this, many clients of "Call" (see above)
    // end up doing some resolution for interface calls and/or proxies themselves.
    PCODE GetCallTarget(OBJECTREF* pThisObj, TypeHandle ownerType = TypeHandle());

    MethodImpl *GetMethodImpl();

    TADDR GetFixupList();

    void CheckRestore(ClassLoadLevel level = CLASS_LOADED);

    //================================================================
    // Running the Prestub preparation step.

    // The stub produced by prestub requires method desc to be passed
    // in dedicated register. Used to implement stubs shared between
    // MethodDescs (e.g. PInvoke stubs)
    BOOL RequiresMethodDescCallingConvention(BOOL fEstimateForChunk = FALSE);

    // Returns true if the method has to have stable entrypoint always.
    BOOL RequiresStableEntryPoint(BOOL fEstimateForChunk = FALSE);

    //
    // Backpatch method slots
    //
    // Arguments:
    //     pMT - cached value of code:MethodDesc::GetMethodTable()
    //     pDispatchingMT - method table of the object that the method is being dispatched on, can be NULL.
    //     fFullBackPatch - indicates whether to patch all possible slots, including the ones
    //                      expensive to patch
    //
    // Return value:
    //     stable entry point (code:MethodDesc::GetStableEntryPoint())
    //
    PCODE DoBackpatch(MethodTable * pMT, MethodTable * pDispatchingMT, BOOL fFullBackPatch);

    PCODE DoPrestub(MethodTable *pDispatchingMT, CallerGCMode callerGCMode = CallerGCMode::Unknown);

    VOID GetMethodInfo(SString &namespaceOrClassName, SString &methodName, SString &methodSignature);
    VOID GetMethodInfoWithNewSig(SString &namespaceOrClassName, SString &methodName, SString &methodSignature);
    VOID GetMethodInfoNoSig(SString &namespaceOrClassName, SString &methodName);
    VOID GetFullMethodInfo(SString& fullMethodSigName);

    typedef void (*WalkValueTypeParameterFnPtr)(Module *pModule, mdToken token, Module *pDefModule, mdToken tkDefToken, const SigParser *ptr, SigTypeContext *pTypeContext, void *pData);

    void WalkValueTypeParameters(MethodTable *pMT, WalkValueTypeParameterFnPtr function, void *pData);

    void PrepareForUseAsADependencyOfANativeImage()
    {
        WRAPPER_NO_CONTRACT;
        if (!HaveValueTypeParametersBeenWalked())
            PrepareForUseAsADependencyOfANativeImageWorker();
    }

    void PrepareForUseAsAFunctionPointer();

private:
    void PrepareForUseAsADependencyOfANativeImageWorker();

    //================================================================
    // The actual data stored in a MethodDesc follows.

protected:
    enum {
        // There are flags available for use here (currently 4 flags bits are available); however, new bits are hard to come by, so any new flags bits should
        // have a fairly strong justification for existence.
        enum_flag3_TokenRemainderMask                       = 0x0FFF, // This must equal METHOD_TOKEN_REMAINDER_MASK calculated higher in this file.
                                                                      // for this method.
        // enum_flag3_HasPrecode implies that enum_flag3_HasStableEntryPoint is set.
        enum_flag3_HasStableEntryPoint                      = 0x1000,   // The method entrypoint is stable (either precode or actual code)
        enum_flag3_HasPrecode                               = 0x2000,   // Precode has been allocated for this method

        enum_flag3_IsUnboxingStub                           = 0x4000,
        enum_flag3_IsEligibleForTieredCompilation           = 0x8000,
    };
    UINT16      m_wFlags3AndTokenRemainder;

    BYTE        m_chunkIndex;
    BYTE        m_methodIndex; // Used to hold the index into the chunk of this MethodDesc. Currently all 8 bits are used, but we could likely work with only 7 bits

    // The slot number of this MethodDesc in the vtable array.
    WORD m_wSlotNumber;
    WORD m_wFlags;

public:
#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif

    BYTE GetMethodDescIndex()
    {
        return m_methodIndex;
    }

    void SetMethodDescIndex(COUNT_T index)
    {
        _ASSERTE(index <= 255);
        m_methodIndex = (BYTE)index;
    }

public:
    inline DWORD GetClassification() const
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return (m_wFlags & mdcClassification);
    }

    inline void SetClassification(DWORD classification)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE((m_wFlags & mdcClassification) == 0);
        m_wFlags |= classification;
    }

    inline BOOL HasNativeCodeSlot()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return (m_wFlags & mdcHasNativeCodeSlot) != 0;
    }

    inline void SetHasNativeCodeSlot()
    {
        LIMITED_METHOD_CONTRACT;
        m_wFlags |= mdcHasNativeCodeSlot;
    }

#ifdef FEATURE_METADATA_UPDATER
    inline BOOL IsEnCAddedMethod()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return (m_wFlags & mdcEnCAddedMethod) != 0;
    }

    inline void SetIsEnCAddedMethod()
    {
        LIMITED_METHOD_CONTRACT;
        m_wFlags |= mdcEnCAddedMethod;
    }
#else
    inline BOOL IsEnCAddedMethod()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return FALSE;
    }
#endif // !FEATURE_METADATA_UPDATER

    inline BOOL IsIntrinsic()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return (m_wFlags & mdcIsIntrinsic) != 0;
    }

    inline void SetIsIntrinsic()
    {
        LIMITED_METHOD_CONTRACT;
        m_wFlags |= mdcIsIntrinsic;
    }

    BOOL RequiresCovariantReturnTypeChecking()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return (m_wFlags & mdcRequiresCovariantReturnTypeChecking) != 0;
    }

    void SetRequiresCovariantReturnTypeChecking()
    {
        LIMITED_METHOD_CONTRACT;
        m_wFlags |= mdcRequiresCovariantReturnTypeChecking;
    }

    static const BYTE s_ClassificationSizeTable[];

    static SIZE_T GetBaseSize(DWORD classification)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        _ASSERTE(classification < mdcClassificationCount);
        return s_ClassificationSizeTable[classification];
    }

    SIZE_T GetBaseSize()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return GetBaseSize(GetClassification());
    }

    SIZE_T SizeOf();

    WORD InterlockedUpdateFlags3(WORD wMask, BOOL fSet);

    inline BOOL HaveValueTypeParametersBeenWalked()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return (m_wFlags & mdcValueTypeParametersWalked) != 0;
    }

    inline void SetValueTypeParametersWalked()
    {
        LIMITED_METHOD_CONTRACT;
        InterlockedUpdateFlags(mdcValueTypeParametersWalked, TRUE);
    }

    inline BOOL HaveValueTypeParametersBeenLoaded()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return (m_wFlags & mdcValueTypeParametersLoaded) != 0;
    }

    inline void SetValueTypeParametersLoaded()
    {
        LIMITED_METHOD_CONTRACT;
        InterlockedUpdateFlags(mdcValueTypeParametersLoaded, TRUE);
    }

#ifdef FEATURE_TYPEEQUIVALENCE
    inline BOOL DoesNotHaveEquivalentValuetypeParameters()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return (m_wFlags & mdcDoesNotHaveEquivalentValuetypeParameters) != 0;
    }

    inline void SetDoesNotHaveEquivalentValuetypeParameters()
    {
        LIMITED_METHOD_CONTRACT;
        InterlockedUpdateFlags(mdcDoesNotHaveEquivalentValuetypeParameters, TRUE);
    }
#endif // FEATURE_TYPEEQUIVALENCE

    //
    // Optional MethodDesc slots appear after the end of base MethodDesc in this order:
    //

    // class MethodImpl;                            // Present if HasMethodImplSlot() is true

    typedef PCODE NonVtableSlot;   // Present if HasNonVtableSlot() is true
    typedef PCODE NativeCodeSlot;  // Present if HasNativeCodeSlot() is true

// Stub Dispatch code
public:
    MethodDesc *GetInterfaceMD();

// StubMethodInfo for use in creating RuntimeMethodHandles
    REFLECTMETHODREF GetStubMethodInfo();

    PrecodeType GetPrecodeType();


    // ---------------------------------------------------------------------------------
    // IL based Code generation pipeline
    // ---------------------------------------------------------------------------------

#ifndef DACCESS_COMPILE
public:
    PCODE PrepareInitialCode(CallerGCMode callerGCMode = CallerGCMode::Unknown);
    PCODE PrepareCode(PrepareCodeConfig* pConfig);

private:
    PCODE PrepareILBasedCode(PrepareCodeConfig* pConfig);
    PCODE GetPrecompiledCode(PrepareCodeConfig* pConfig, bool shouldTier);
    PCODE GetPrecompiledR2RCode(PrepareCodeConfig* pConfig);
    PCODE GetMulticoreJitCode(PrepareCodeConfig* pConfig, bool* pWasTier0);
    PCODE JitCompileCode(PrepareCodeConfig* pConfig);
    PCODE JitCompileCodeLockedEventWrapper(PrepareCodeConfig* pConfig, JitListLockEntry* pEntry);
    PCODE JitCompileCodeLocked(PrepareCodeConfig* pConfig, COR_ILMETHOD_DECODER* pilHeader, JitListLockEntry* pLockEntry, ULONG* pSizeOfCode);

public:
    bool TryGenerateUnsafeAccessor(DynamicResolver** resolver, COR_ILMETHOD_DECODER** methodILDecoder);
#endif // DACCESS_COMPILE

#ifdef HAVE_GCCOVER
private:
    static CrstStatic m_GCCoverCrst;

public:
    static void Init();
#endif
};

#ifndef DACCESS_COMPILE
class PrepareCodeConfig
{
public:
    PrepareCodeConfig();
    PrepareCodeConfig(NativeCodeVersion nativeCodeVersion, BOOL needsMulticoreJitNotification, BOOL mayUsePrecompiledCode);

    MethodDesc* GetMethodDesc() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_pMethodDesc;
    }

    NativeCodeVersion GetCodeVersion() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_nativeCodeVersion;
    }

    BOOL NeedsMulticoreJitNotification();
    BOOL MayUsePrecompiledCode();
    virtual PCODE IsJitCancellationRequested();
    virtual BOOL SetNativeCode(PCODE pCode, PCODE * ppAlternateCodeToUse);
    virtual COR_ILMETHOD* GetILHeader();
    virtual CORJIT_FLAGS GetJitCompilationFlags();
#ifdef FEATURE_ON_STACK_REPLACEMENT
    virtual unsigned GetILOffset() const { return 0; }
#endif
    BOOL ProfilerRejectedPrecompiledCode();
    BOOL ReadyToRunRejectedPrecompiledCode();
    void SetProfilerRejectedPrecompiledCode();
    void SetReadyToRunRejectedPrecompiledCode();
    CallerGCMode GetCallerGCMode();
    void SetCallerGCMode(CallerGCMode mode);

public:
    bool IsForMulticoreJit() const
    {
        WRAPPER_NO_CONTRACT;

    #ifdef FEATURE_MULTICOREJIT
        return m_isForMulticoreJit;
    #else
        return false;
    #endif
    }

#ifdef FEATURE_MULTICOREJIT
protected:
    void SetIsForMulticoreJit()
    {
        WRAPPER_NO_CONTRACT;
        m_isForMulticoreJit = true;
    }
#endif

#ifdef FEATURE_CODE_VERSIONING
public:
    bool ProfilerMayHaveActivatedNonDefaultCodeVersion() const
    {
        WRAPPER_NO_CONTRACT;
        return m_profilerMayHaveActivatedNonDefaultCodeVersion;
    }

    void SetProfilerMayHaveActivatedNonDefaultCodeVersion()
    {
        WRAPPER_NO_CONTRACT;
        m_profilerMayHaveActivatedNonDefaultCodeVersion = true;
    }

    bool GeneratedOrLoadedNewCode() const
    {
        WRAPPER_NO_CONTRACT;
        return m_generatedOrLoadedNewCode;
    }

    void SetGeneratedOrLoadedNewCode()
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(!m_generatedOrLoadedNewCode);

        m_generatedOrLoadedNewCode = true;
    }
#endif

#ifdef FEATURE_TIERED_COMPILATION
public:
    bool WasTieringDisabledBeforeJitting() const
    {
        WRAPPER_NO_CONTRACT;
        return m_wasTieringDisabledBeforeJitting;
    }

    void SetWasTieringDisabledBeforeJitting()
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(GetMethodDesc()->IsEligibleForTieredCompilation());

        m_wasTieringDisabledBeforeJitting = true;
    }

    bool ShouldCountCalls() const
    {
        WRAPPER_NO_CONTRACT;
        return m_shouldCountCalls;
    }

    void SetShouldCountCalls()
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(!m_shouldCountCalls);

        m_shouldCountCalls = true;
    }
#endif

public:
    enum class JitOptimizationTier : UINT8
    {
        Unknown, // to identify older runtimes that would send this value
        MinOptJitted,
        Optimized,
        QuickJitted,
        OptimizedTier1,
        OptimizedTier1OSR,
        InstrumentedTier,
        InstrumentedTierOptimized,

        Count
    };

    static JitOptimizationTier GetJitOptimizationTier(PrepareCodeConfig *config, MethodDesc *methodDesc);
    static const char *GetJitOptimizationTierStr(PrepareCodeConfig *config, MethodDesc *methodDesc);

    bool JitSwitchedToMinOpt() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_jitSwitchedToMinOpt;
    }

    void SetJitSwitchedToMinOpt()
    {
        LIMITED_METHOD_CONTRACT;

#ifdef FEATURE_TIERED_COMPILATION
        m_jitSwitchedToOptimized = false;
#endif
        m_jitSwitchedToMinOpt = true;
    }

#ifdef FEATURE_TIERED_COMPILATION
public:
    bool JitSwitchedToOptimized() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_jitSwitchedToOptimized;
    }

    void SetJitSwitchedToOptimized()
    {
        LIMITED_METHOD_CONTRACT;

        if (!m_jitSwitchedToMinOpt)
        {
            m_jitSwitchedToOptimized = true;
        }
    }

    bool FinalizeOptimizationTierForTier0Load();
    bool FinalizeOptimizationTierForTier0LoadOrJit();
#endif

public:
    PrepareCodeConfig *GetNextInSameThread() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_nextInSameThread;
    }

    void SetNextInSameThread(PrepareCodeConfig *config)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(config == nullptr || m_nextInSameThread == nullptr);

        m_nextInSameThread = config;
    }

protected:
    MethodDesc* m_pMethodDesc;
    NativeCodeVersion m_nativeCodeVersion;
    BOOL m_needsMulticoreJitNotification;
    BOOL m_mayUsePrecompiledCode;
    BOOL m_ProfilerRejectedPrecompiledCode;
    BOOL m_ReadyToRunRejectedPrecompiledCode;
    CallerGCMode m_callerGCMode;

#ifdef FEATURE_MULTICOREJIT
private:
    bool m_isForMulticoreJit;
#endif

#ifdef FEATURE_CODE_VERSIONING
private:
    bool m_profilerMayHaveActivatedNonDefaultCodeVersion;
    bool m_generatedOrLoadedNewCode;
#endif

#ifdef FEATURE_TIERED_COMPILATION
private:
    bool m_wasTieringDisabledBeforeJitting;
    bool m_shouldCountCalls;
#endif

private:
    bool m_jitSwitchedToMinOpt; // when it wasn't requested
#ifdef FEATURE_TIERED_COMPILATION
    bool m_jitSwitchedToOptimized; // when a different tier was requested
#endif
    PrepareCodeConfig *m_nextInSameThread;
};

#ifdef FEATURE_CODE_VERSIONING
class VersionedPrepareCodeConfig : public PrepareCodeConfig
{
public:
    VersionedPrepareCodeConfig();
    VersionedPrepareCodeConfig(NativeCodeVersion codeVersion);
    HRESULT FinishConfiguration();
    virtual PCODE IsJitCancellationRequested();
    virtual COR_ILMETHOD* GetILHeader();
    virtual CORJIT_FLAGS GetJitCompilationFlags();
private:
    ILCodeVersion m_ilCodeVersion;
};

class PrepareCodeConfigBuffer
{
private:
    UINT8 m_buffer[sizeof(VersionedPrepareCodeConfig)];

public:
    PrepareCodeConfigBuffer(NativeCodeVersion codeVersion);

public:
    PrepareCodeConfig *GetConfig() const
    {
        WRAPPER_NO_CONTRACT;
        return (PrepareCodeConfig *)m_buffer;
    }

    PrepareCodeConfigBuffer(const PrepareCodeConfigBuffer &) = delete;
    PrepareCodeConfigBuffer &operator =(const PrepareCodeConfigBuffer &) = delete;
};
#endif // FEATURE_CODE_VERSIONING

class MulticoreJitPrepareCodeConfig : public PrepareCodeConfig
{
private:
    bool m_wasTier0;

public:
    MulticoreJitPrepareCodeConfig(MethodDesc* pMethod);

    bool WasTier0() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_wasTier0;
    }

    void SetWasTier0()
    {
        LIMITED_METHOD_CONTRACT;
        m_wasTier0 = true;
    }

    virtual BOOL SetNativeCode(PCODE pCode, PCODE * ppAlternateCodeToUse) override;
};
#endif // DACCESS_COMPILE

/******************************************************************/

// A code:MethodDescChunk is a container that holds one or more code:MethodDesc.  Logically it is just
// compression.  Basically fields that are common among methods descs in the chunk are stored in the chunk
// and the MethodDescs themselves just store and index that allows them to find their Chunk.  Semantically
// a code:MethodDescChunk is just a set of code:MethodDesc.
class MethodDescChunk
{
    friend class MethodDesc;
    friend class CheckAsmOffsets;

    enum {
        enum_flag_TokenRangeMask                           = 0x0FFF, // This must equal METHOD_TOKEN_RANGE_MASK calculated higher in this file
                                                                     // These are separate to allow the flags space available and used to be obvious here
                                                                     // and for the logic that splits the token to be algorithmically generated based on the
                                                                     // #define
        enum_flag_HasCompactEntrypoints                    = 0x4000, // Compact temporary entry points
        // unused                                          = 0x8000,
    };

public:
    //
    // Allocates methodDescCount identical MethodDescs in smallest possible number of chunks.
    // If methodDescCount is zero, one chunk with maximum number of MethodDescs is allocated.
    //
    static MethodDescChunk *CreateChunk(LoaderHeap *pHeap, DWORD methodDescCount,
                                        DWORD classification,
                                        BOOL fNonVtableSlot,
                                        BOOL fNativeCodeSlot,
                                        MethodTable *initialMT,
                                        class AllocMemTracker *pamTracker);

    TADDR GetTemporaryEntryPoints()
    {
        LIMITED_METHOD_CONTRACT;
        return *(dac_cast<DPTR(TADDR)>(this) - 1);
    }

    PCODE GetTemporaryEntryPoint(int index);

    void EnsureTemporaryEntryPointsCreated(LoaderAllocator *pLoaderAllocator, AllocMemTracker *pamTracker)
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;

        if (GetTemporaryEntryPoints() == NULL)
            CreateTemporaryEntryPoints(pLoaderAllocator, pamTracker);
    }

    void CreateTemporaryEntryPoints(LoaderAllocator *pLoaderAllocator, AllocMemTracker *pamTracker);

#ifdef HAS_COMPACT_ENTRYPOINTS
    //
    // There two implementation options for temporary entrypoints:
    //
    // (1) Compact entrypoints. They provide as dense entrypoints as possible, but can't be patched
    // to point to the final code. The call to unjitted method is indirect call via slot.
    //
    // (2) Precodes. The precode will be patched to point to the final code eventually, thus
    // the temporary entrypoint can be embedded in the code. The call to unjitted method is
    // direct call to direct jump.
    //
    // We use (1) for x86 and (2) for 64-bit to get the best performance on each platform.
    // For ARM (1) is used.

    TADDR AllocateCompactEntryPoints(LoaderAllocator *pLoaderAllocator, AllocMemTracker *pamTracker);

    static MethodDesc* GetMethodDescFromCompactEntryPoint(PCODE addr, BOOL fSpeculative = FALSE);
    static SIZE_T SizeOfCompactEntryPoints(int count);

    static BOOL IsCompactEntryPointAtAddress(PCODE addr);

#ifdef TARGET_ARM
    static int GetCompactEntryPointMaxCount ();
#endif // TARGET_ARM
#endif // HAS_COMPACT_ENTRYPOINTS

    FORCEINLINE PTR_MethodTable GetMethodTable()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_methodTable;
    }

    inline DPTR(PTR_MethodTable) GetMethodTablePtr() const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return dac_cast<DPTR(PTR_MethodTable)>(PTR_HOST_MEMBER_TADDR(MethodDescChunk, this, m_methodTable));
    }

#ifndef DACCESS_COMPILE
    inline void SetMethodTable(MethodTable * pMT)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(m_methodTable == NULL);
        _ASSERTE(pMT != NULL);
        m_methodTable = pMT;
    }

    inline void SetSizeAndCount(SIZE_T sizeOfMethodDescs, COUNT_T methodDescCount)
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(FitsIn<BYTE>((sizeOfMethodDescs / MethodDesc::ALIGNMENT) - 1));
        m_size = static_cast<BYTE>((sizeOfMethodDescs / MethodDesc::ALIGNMENT) - 1);
        _ASSERTE(SizeOf() == sizeof(MethodDescChunk) + sizeOfMethodDescs);

        _ASSERTE(FitsIn<BYTE>(methodDescCount - 1));
        m_count = static_cast<BYTE>(methodDescCount - 1);
        _ASSERTE(GetCount() == methodDescCount);
    }

    void SetNextChunk(MethodDescChunk *chunk)
    {
        LIMITED_METHOD_CONTRACT;
        m_next = chunk;
    }
#endif // !DACCESS_COMPILE

    PTR_MethodDescChunk GetNextChunk()
    {
        LIMITED_METHOD_CONTRACT;
        return m_next;
    }

    UINT32 GetCount()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_count + 1;
    }

    inline BOOL HasCompactEntryPoints()
    {
        LIMITED_METHOD_DAC_CONTRACT;

#ifdef HAS_COMPACT_ENTRYPOINTS
        return (m_flagsAndTokenRange & enum_flag_HasCompactEntrypoints) != 0;
#else
        return FALSE;
#endif
    }

    inline UINT16 GetTokRange()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_flagsAndTokenRange & enum_flag_TokenRangeMask;
    }

    inline SIZE_T SizeOf()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return sizeof(MethodDescChunk) + (m_size + 1) * MethodDesc::ALIGNMENT;
    }

    inline MethodDesc *GetFirstMethodDesc()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return PTR_MethodDesc(dac_cast<TADDR>(this) + sizeof(MethodDescChunk));
    }

    // Maximum size of one chunk (corresponts to the maximum of m_size = 0xFF)
    static const SIZE_T MaxSizeOfMethodDescs = 0x100 * MethodDesc::ALIGNMENT;

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif

private:
    void SetHasCompactEntryPoints()
    {
        LIMITED_METHOD_CONTRACT;
        m_flagsAndTokenRange |= enum_flag_HasCompactEntrypoints;
    }

    void SetTokenRange(UINT16 tokenRange)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE((tokenRange & ~enum_flag_TokenRangeMask) == 0);
        static_assert_no_msg(enum_flag_TokenRangeMask == METHOD_TOKEN_RANGE_MASK);
        m_flagsAndTokenRange = (m_flagsAndTokenRange & ~enum_flag_TokenRangeMask) | tokenRange;
    }

    PTR_MethodTable m_methodTable;

    PTR_MethodDescChunk  m_next;

    BYTE                 m_size;        // The size of this chunk minus 1 (in multiples of MethodDesc::ALIGNMENT)
    BYTE                 m_count;       // The number of MethodDescs in this chunk minus 1
    UINT16               m_flagsAndTokenRange;

    // Followed by array of method descs...
};

inline int MethodDesc::GetMethodDescChunkIndex() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    return m_chunkIndex;
}

inline MethodDescChunk *MethodDesc::GetMethodDescChunk() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    return
        PTR_MethodDescChunk(dac_cast<TADDR>(this) -
                            (sizeof(MethodDescChunk) + (GetMethodDescChunkIndex() * MethodDesc::ALIGNMENT)));
}

MethodDesc* NonVirtualEntry2MethodDesc(PCODE entryPoint);
// convert an entry point into a MethodDesc
MethodDesc* Entry2MethodDesc(PCODE entryPoint, MethodTable *pMT);


typedef DPTR(class StoredSigMethodDesc) PTR_StoredSigMethodDesc;
class StoredSigMethodDesc : public MethodDesc
{
public:
    // Put the sig RVA in here - this allows us to avoid
    // touching the method desc table when CoreLib is prejitted.

    TADDR           m_pSig;
    DWORD           m_cSig;

protected:
    // m_dwExtendedFlags is not used by StoredSigMethodDesc itself.
    // It is used by child classes. We allocate the space here to get
    // optimal layout.
    DWORD           m_dwExtendedFlags;

public:
    TADDR GetSigRVA()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_pSig;
    }

    bool HasStoredMethodSig(void)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_pSig != NULL;
    }
    PCCOR_SIGNATURE GetStoredMethodSig(DWORD* sigLen = NULL)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        if (sigLen)
        {
            *sigLen = m_cSig;
        }
#ifdef DACCESS_COMPILE
        return (PCCOR_SIGNATURE)
            DacInstantiateTypeByAddress(GetSigRVA(), m_cSig, true);
#else // !DACCESS_COMPILE
        return (PCCOR_SIGNATURE) m_pSig;
#endif // !DACCESS_COMPILE
    }
    void SetStoredMethodSig(PCCOR_SIGNATURE sig, DWORD sigBytes)
    {
#ifndef DACCESS_COMPILE
        m_pSig = (TADDR)sig;
        m_cSig = sigBytes;
#endif // !DACCESS_COMPILE
    }

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif
};

//-----------------------------------------------------------------------
// Operations specific to FCall methods. We use a derived class to get
// the compiler involved in enforcing proper method type usage.
// DO NOT ADD FIELDS TO THIS CLASS.
//-----------------------------------------------------------------------

class FCallMethodDesc : public MethodDesc
{

    DWORD   m_dwECallID;
#ifdef TARGET_64BIT
    DWORD   m_padding;
#endif

public:
    void SetECallID(DWORD dwID)
    {
        LIMITED_METHOD_CONTRACT;
        m_dwECallID = dwID;
    }

    DWORD GetECallID()
    {
        LIMITED_METHOD_CONTRACT;
        return m_dwECallID;
    }
};

class HostCodeHeap;
class LCGMethodResolver;
typedef DPTR(LCGMethodResolver)       PTR_LCGMethodResolver;
class ILStubResolver;
typedef DPTR(ILStubResolver)          PTR_ILStubResolver;
class DynamicResolver;
typedef DPTR(DynamicResolver)         PTR_DynamicResolver;

class DynamicMethodDesc : public StoredSigMethodDesc
{
    friend class ILStubCache;
    friend class ILStubState;
    friend class DynamicMethodTable;
    friend class MethodDesc;

protected:
    PTR_CUTF8           m_pszMethodName;
    PTR_DynamicResolver m_pResolver;

public:
    enum ILStubType : DWORD
    {
        StubNotSet = 0,
        StubCLRToNativeInterop,
        StubCLRToCOMInterop,
        StubNativeToCLRInterop,
        StubCOMToCLRInterop,
        StubStructMarshalInterop,
#ifdef FEATURE_ARRAYSTUB_AS_IL
        StubArrayOp,
#endif
#ifdef FEATURE_MULTICASTSTUB_AS_IL
        StubMulticastDelegate,
#endif
        StubWrapperDelegate,
#ifdef FEATURE_INSTANTIATINGSTUB_AS_IL
        StubUnboxingIL,
        StubInstantiating,
#endif
        StubTailCallStoreArgs,
        StubTailCallCallTarget,

        StubVirtualStaticMethodDispatch,

        StubLast
    };

    enum Flag : DWORD
    {
        // Flags for DynamicMethodDesc
        // Define new flags in descending order. This allows the IL type enumeration to increase naturally.
        FlagNone                = 0x00000000,
        FlagPublic              = 0x00000800,
        FlagStatic              = 0x00001000,
        FlagRequiresCOM         = 0x00002000,
        FlagIsLCGMethod         = 0x00004000,
        FlagIsILStub            = 0x00008000,
        FlagIsDelegate          = 0x00010000,
        FlagIsCALLI             = 0x00020000,
        FlagMask                = 0x0003f800,
        StackArgSizeMask        = 0xfffc0000, // native stack arg size for IL stubs
        ILStubTypeMask          = ~(FlagMask | StackArgSizeMask)
    };
    static_assert_no_msg((FlagMask & StubLast) == 0);
    static_assert_no_msg((StackArgSizeMask & FlagMask) == 0);

    // MethodDesc memory is acquired in an uninitialized state.
    // The first step should be to explicitly set the entire
    // flag state and then modify it.
    void InitializeFlags(DWORD flags)
    {
        m_dwExtendedFlags = flags;
    }
    bool HasFlags(DWORD flags) const
    {
        return !!(m_dwExtendedFlags & flags);
    }
    void SetFlags(DWORD flags)
    {
        m_dwExtendedFlags |= flags;
    }
    void ClearFlags(DWORD flags)
    {
        m_dwExtendedFlags = (m_dwExtendedFlags & ~flags);
    }

    ILStubType GetILStubType() const
    {
        ILStubType type = (ILStubType)(m_dwExtendedFlags & ILStubTypeMask);
        _ASSERTE(type == StubNotSet || HasFlags(FlagIsILStub));
        return type;
    }

    void SetILStubType(ILStubType type)
    {
        _ASSERTE(HasFlags(FlagIsILStub));
        m_dwExtendedFlags |= type;
    }

public:
    bool IsILStub() const { LIMITED_METHOD_DAC_CONTRACT; return HasFlags(FlagIsILStub); }
    bool IsLCGMethod() const { LIMITED_METHOD_DAC_CONTRACT; return HasFlags(FlagIsLCGMethod); }

	inline PTR_DynamicResolver    GetResolver();
    inline PTR_LCGMethodResolver  GetLCGMethodResolver();
    inline PTR_ILStubResolver     GetILStubResolver();

    PTR_CUTF8 GetMethodName()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_pszMethodName;
    }

    // Based on the current flags, compute the equivalent as COR metadata.
    WORD GetAttrs() const
    {
        LIMITED_METHOD_CONTRACT;
        WORD asMetadata = 0;
        asMetadata |= HasFlags(FlagPublic) ? mdPublic : 0;
        asMetadata |= HasFlags(FlagStatic) ? mdStatic : 0;
        return asMetadata;
    }

    WORD GetNativeStackArgSize()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        _ASSERTE(IsILStub());
        return (WORD)((m_dwExtendedFlags & StackArgSizeMask) >> 16);
    }

    void SetNativeStackArgSize(WORD cbArgSize)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(IsILStub());
#if !defined(OSX_ARM64_ABI)
        _ASSERTE((cbArgSize % TARGET_POINTER_SIZE) == 0);
#endif
        m_dwExtendedFlags = (m_dwExtendedFlags & ~StackArgSizeMask) | ((DWORD)cbArgSize << 16);
    }

    bool IsReverseStub() const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        _ASSERTE(IsILStub());
        ILStubType type = GetILStubType();
        return type == StubCOMToCLRInterop || type == StubNativeToCLRInterop;
    }

    bool IsStepThroughStub() const
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(IsILStub());

        bool isStepThrough = false;

#ifdef FEATURE_INSTANTIATINGSTUB_AS_IL
        ILStubType type = GetILStubType();
        isStepThrough = type == StubUnboxingIL || type == StubInstantiating;
#endif // FEATURE_INSTANTIATINGSTUB_AS_IL

        return isStepThrough;
    }

    bool IsCLRToCOMStub() const
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(IsILStub());
        return !HasFlags(FlagStatic) && GetILStubType() == StubCLRToCOMInterop;
    }
    bool IsCOMToCLRStub() const
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(IsILStub());
        return !HasFlags(FlagStatic) && GetILStubType() == StubCOMToCLRInterop;
    }
    bool IsPInvokeStub() const
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(IsILStub());
        return HasFlags(FlagStatic)
            && !HasFlags(FlagIsCALLI)
            && GetILStubType() == StubCLRToNativeInterop;
    }

#ifdef FEATURE_MULTICASTSTUB_AS_IL
    bool IsMulticastStub() const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        _ASSERTE(IsILStub());
        return GetILStubType() == DynamicMethodDesc::StubMulticastDelegate;
    }
#endif
    bool IsWrapperDelegateStub() const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        _ASSERTE(IsILStub());
        return GetILStubType() == DynamicMethodDesc::StubWrapperDelegate;
    }
#ifdef FEATURE_INSTANTIATINGSTUB_AS_IL
    bool IsUnboxingILStub() const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        _ASSERTE(IsILStub());
        return GetILStubType() == DynamicMethodDesc::StubUnboxingIL;
    }
#endif

    // Whether the stub takes a context argument that is an interop MethodDesc.
    bool HasMDContextArg() const
    {
        LIMITED_METHOD_CONTRACT;
        return IsCLRToCOMStub() || (IsPInvokeStub() && !HasFlags(FlagIsDelegate));
    }

    //
    // following implementations defined in DynamicMethod.cpp
    //
    void Destroy();
};


class ArrayMethodDesc : public StoredSigMethodDesc
{
public:
    // The VTABLE for an array look like

    //  System.Object Vtable
    //  System.Array Vtable
    //  type[] Vtable
    //      Get(<rank specific)
    //      Set(<rank specific)
    //      Address(<rank specific)
    //      .ctor(int)      // Possibly more

    enum {
        ARRAY_FUNC_GET      = 0,
        ARRAY_FUNC_SET      = 1,
        ARRAY_FUNC_ADDRESS  = 2,
        ARRAY_FUNC_CTOR     = 3, // Anything >= ARRAY_FUNC_CTOR is .ctor
    };

    // Get the index of runtime provided array method
    DWORD GetArrayFuncIndex()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        DWORD dwSlot = GetSlot();
        DWORD dwVirtuals = GetMethodTable()->GetNumVirtuals();
        _ASSERTE(dwSlot >= dwVirtuals);
        return dwSlot - dwVirtuals;
    }

    LPCUTF8 GetMethodName();
    DWORD GetAttrs();
};

#ifdef HAS_NDIRECT_IMPORT_PRECODE
typedef NDirectImportPrecode NDirectImportThunkGlue;
#else // HAS_NDIRECT_IMPORT_PRECODE

class NDirectImportThunkGlue
{
    PVOID m_dummy; // Dummy field to make the alignment right

public:
    LPVOID GetEntryPoint()
    {
        LIMITED_METHOD_CONTRACT;
        return NULL;
    }
    void Init(MethodDesc *pMethod)
    {
        LIMITED_METHOD_CONTRACT;
    }
};

#endif // HAS_NDIRECT_IMPORT_PRECODE

typedef DPTR(NDirectImportThunkGlue)      PTR_NDirectImportThunkGlue;


//-----------------------------------------------------------------------
// Operations specific to NDirect methods. We use a derived class to get
// the compiler involved in enforcing proper method type usage.
// DO NOT ADD FIELDS TO THIS CLASS.
//-----------------------------------------------------------------------
class NDirectMethodDesc : public MethodDesc
{
public:
    struct temp1
    {
        // Information about the entrypoint
        PTR_CUTF8   m_pszEntrypointName;

        union
        {
            PTR_CUTF8   m_pszLibName;
            DWORD       m_dwECallID;    // ECallID for QCalls
        };

        // The JIT generates an indirect call through this location in some cases.
        // Initialized to NDirectImportThunkGlue. Patched to the true target or
        // host interceptor stub or alignment thunk after linking.
        LPVOID      m_pNDirectTarget;

#ifdef HAS_NDIRECT_IMPORT_PRECODE
        PTR_NDirectImportThunkGlue  m_pImportThunkGlue;
#else // HAS_NDIRECT_IMPORT_PRECODE
        NDirectImportThunkGlue      m_ImportThunkGlue;
#endif // HAS_NDIRECT_IMPORT_PRECODE

        ULONG       m_DefaultDllImportSearchPathsAttributeValue; // DefaultDllImportSearchPathsAttribute is saved.

        // Various attributes needed at runtime.
        WORD        m_wFlags;

#if defined(TARGET_X86)
        // Size of outgoing arguments (on stack). Note that in order to get the @n stdcall name decoration,
        WORD        m_cbStackArgumentSize;
#endif // defined(TARGET_X86)

    } ndirect;

    enum Flags
    {
        // There are two groups of flag bits here each which gets initialized
        // at different times.

        //
        // Group 1: The init group.
        //
        //   This group is set during MethodDesc construction. No race issues
        //   here since they are initialized before the MD is ever published
        //   and never change after that.

        kEarlyBound                     = 0x0001,   // IJW managed->unmanaged thunk. Standard [sysimport] stuff otherwise.

        // unused                       = 0x0002,

        kDefaultDllImportSearchPathsIsCached = 0x0004, // set if we cache attribute value.

        // kUnusedMask                  = 0x0008

        //
        // Group 2: The runtime group.
        //
        //   This group is set during runtime potentially by multiple threads
        //   at the same time. All flags in this category has to be set via interlocked operation.
        //
        kIsMarshalingRequiredCached     = 0x0010,   // Set if we have cached the results of marshaling required computation
        kCachedMarshalingRequired       = 0x0020,   // The result of the marshaling required computation

        kNativeAnsi                     = 0x0040,

        kLastError                      = 0x0080,   // setLastError keyword specified
        kNativeNoMangle                 = 0x0100,   // nomangle keyword specified

        kVarArgs                        = 0x0200,
        kStdCall                        = 0x0400,
        kThisCall                       = 0x0800,

        kIsQCall                        = 0x1000,

        kDefaultDllImportSearchPathsStatus = 0x2000, // either method has custom attribute or not.

        kNDirectPopulated               = 0x8000, // Indicate if the NDirect has been fully populated.
    };

    // Resolve the import to the NDirect target and set it on the NDirectMethodDesc.
    static void* ResolveAndSetNDirectTarget(_In_ NDirectMethodDesc* pMD);

    // Attempt to get a resolved NDirect target. This will return true for already resolved
    // targets and methods that are resolved at JIT time, such as those marked SuppressGCTransition
    static BOOL TryGetResolvedNDirectTarget(_In_ NDirectMethodDesc* pMD, _Out_ void** ndirectTarget);

    // Retrieves the cached result of marshaling required computation, or performs the computation
    // if the result is not cached yet.
    BOOL MarshalingRequired()
    {
        STANDARD_VM_CONTRACT;

        if ((ndirect.m_wFlags & kIsMarshalingRequiredCached) == 0)
        {
            // Compute the flag and cache the result
            InterlockedSetNDirectFlags(kIsMarshalingRequiredCached |
                (ComputeMarshalingRequired() ? kCachedMarshalingRequired : 0));
        }
        _ASSERTE((ndirect.m_wFlags & kIsMarshalingRequiredCached) != 0);
        return (ndirect.m_wFlags & kCachedMarshalingRequired) != 0;
    }

    BOOL ComputeMarshalingRequired();

    // Atomically set specified flags. Only setting of the bits is supported.
    void InterlockedSetNDirectFlags(WORD wFlags);

    void SetIsEarlyBound()
    {
        LIMITED_METHOD_CONTRACT;
        ndirect.m_wFlags |= kEarlyBound;
    }

    BOOL IsEarlyBound()
    {
        LIMITED_METHOD_CONTRACT;
        return (ndirect.m_wFlags & kEarlyBound) != 0;
    }

    BOOL IsNativeAnsi() const
    {
        LIMITED_METHOD_CONTRACT;

        return (ndirect.m_wFlags & kNativeAnsi) != 0;
    }

    BOOL IsNativeNoMangled() const
    {
        LIMITED_METHOD_CONTRACT;

        return (ndirect.m_wFlags & kNativeNoMangle) != 0;
    }

    PTR_CUTF8 GetLibNameRaw()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return ndirect.m_pszLibName;
    }

#ifndef DACCESS_COMPILE
    LPCUTF8 GetLibName() const
    {
        LIMITED_METHOD_CONTRACT;

        return IsQCall() ? "QCall" : ndirect.m_pszLibName;
    }
#endif // !DACCESS_COMPILE

    PTR_CUTF8 GetEntrypointName() const
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return ndirect.m_pszEntrypointName;
    }

    BOOL IsVarArgs() const
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return (ndirect.m_wFlags & kVarArgs) != 0;
    }

    BOOL IsStdCall() const
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return (ndirect.m_wFlags & kStdCall) != 0;
    }

    BOOL IsThisCall() const
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return (ndirect.m_wFlags & kThisCall) != 0;
    }

    // Returns TRUE if this MethodDesc is internal call from CoreLib to VM
    BOOL IsQCall() const
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return (ndirect.m_wFlags & kIsQCall) != 0;
    }

    BOOL HasDefaultDllImportSearchPathsAttribute();

    BOOL IsDefaultDllImportSearchPathsAttributeCached()
    {
        LIMITED_METHOD_CONTRACT;
        return (ndirect.m_wFlags & kDefaultDllImportSearchPathsIsCached) != 0;
    }

    BOOL IsPopulated()
    {
        LIMITED_METHOD_CONTRACT;
        return (ndirect.m_wFlags & kNDirectPopulated) != 0;
    }

    ULONG DefaultDllImportSearchPathsAttributeCachedValue()
    {
        LIMITED_METHOD_CONTRACT;
        return ndirect.m_DefaultDllImportSearchPathsAttributeValue & 0xFFFFFFFD;
    }

    BOOL DllImportSearchAssemblyDirectory()
    {
        LIMITED_METHOD_CONTRACT;
        return (ndirect.m_DefaultDllImportSearchPathsAttributeValue & 0x2) != 0;
    }

    PTR_NDirectImportThunkGlue GetNDirectImportThunkGlue()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return ndirect.m_pImportThunkGlue;
    }

    LPVOID GetNDirectTarget()
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(IsNDirect());
        return ndirect.m_pNDirectTarget;
    }

    VOID SetNDirectTarget(LPVOID pTarget);

#ifndef DACCESS_COMPILE
    BOOL NDirectTargetIsImportThunk()
    {
        WRAPPER_NO_CONTRACT;

        _ASSERTE(IsNDirect());

        return (GetNDirectTarget() == GetNDirectImportThunkGlue()->GetEntrypoint());
    }
#endif // !DACCESS_COMPILE

    //  Find the entry point name and function address
    //  based on the module and data from NDirectMethodDesc
    //
    LPVOID FindEntryPoint(NATIVE_LIBRARY_HANDLE hMod);

#ifdef TARGET_WINDOWS
private:
    FARPROC FindEntryPointWithMangling(NATIVE_LIBRARY_HANDLE mod, PTR_CUTF8 entryPointName);
    FARPROC FindEntryPointWithSuffix(NATIVE_LIBRARY_HANDLE mod, PTR_CUTF8 entryPointName, char suffix);
#endif
public:

    void SetStackArgumentSize(WORD cbDstBuffer, CorInfoCallConvExtension unmgdCallConv)
    {
        LIMITED_METHOD_CONTRACT;

#if defined(TARGET_X86)
        // thiscall passes the this pointer in ECX
        if (unmgdCallConv == CorInfoCallConvExtension::Thiscall)
        {
            _ASSERTE(cbDstBuffer >= sizeof(SLOT));
            cbDstBuffer -= sizeof(SLOT);
        }

        // Don't write to the field if it's already initialized to avoid creating private pages (NGEN)
        if (ndirect.m_cbStackArgumentSize == 0xFFFF)
        {
            ndirect.m_cbStackArgumentSize = cbDstBuffer;
        }
        else
        {
            _ASSERTE(ndirect.m_cbStackArgumentSize == cbDstBuffer);
        }
#endif // defined(TARGET_X86)
    }

#if defined(TARGET_X86)
    void EnsureStackArgumentSize();

    WORD GetStackArgumentSize() const
    {
        LIMITED_METHOD_DAC_CONTRACT;

        _ASSERTE(ndirect.m_cbStackArgumentSize != 0xFFFF);

        // If we have a methoddesc, stackArgSize is the number of bytes of
        // the outgoing marshalling buffer.
        return ndirect.m_cbStackArgumentSize;
    }
#endif // defined(TARGET_X86)

    VOID InitEarlyBoundNDirectTarget();

    // In AppDomains, we can trigger declarer's cctor when we link the P/Invoke,
    // which takes care of inlined calls as well. See code:NDirect.NDirectLink.
    // Although the cctor is guaranteed to run in the shared domain before the
    // target is invoked, we will trigger it at link time as well because linking
    // may depend on it - cctor may change the target DLL, DLL search path etc.
    BOOL IsClassConstructorTriggeredAtLinkTime()
    {
        LIMITED_METHOD_CONTRACT;
        MethodTable * pMT = GetMethodTable();
        // Try to avoid touching the EEClass if possible
        if (!pMT->HasClassConstructor())
            return FALSE;
        return !pMT->GetClass()->IsBeforeFieldInit();
    }
};  //class NDirectMethodDesc

//-----------------------------------------------------------------------
// Operations specific to EEImplCall methods. We use a derived class to get
// the compiler involved in enforcing proper method type usage.
//
// For now, the only EE impl is the delegate Invoke method. If we
// add other EE impl types in the future, may need a discriminator
// field here.
//-----------------------------------------------------------------------
class EEImplMethodDesc : public StoredSigMethodDesc
{ };

#ifdef FEATURE_COMINTEROP

// This is the extra information needed to be associated with a method in order to use it for
// CLR->COM calls. It is currently used by code:ComPlusCallMethodDesc (ordinary CLR->COM calls).
typedef DPTR(struct ComPlusCallInfo) PTR_ComPlusCallInfo;
struct ComPlusCallInfo
{
    // Returns ComPlusCallInfo associated with a method. pMD must be a ComPlusCallMethodDesc or
    // EEImplMethodDesc that has already been initialized for COM interop.
    inline static ComPlusCallInfo *FromMethodDesc(MethodDesc *pMD);

    union
    {
        // IL stub for CLR to COM call
        PCODE m_pILStub;

        // MethodDesc of the COM event provider to forward the call to (COM event interfaces)
        MethodDesc *m_pEventProviderMD;
    };

    // method table of the interface which this represents
    PTR_MethodTable m_pInterfaceMT;

    enum Flags
    {
        kRequiresArgumentWrapping       = 0x1,
    };
    BYTE        m_flags;

    // ComSlot() (is cached when we first invoke the method and generate
    // the stubs for it. There's probably a better place to do this
    // caching but I'm not sure I know all the places these things are
    // created.)
    WORD        m_cachedComSlot;

    PCODE * GetAddrOfILStubField()
    {
        LIMITED_METHOD_CONTRACT;
        return &m_pILStub;
    }

#ifdef TARGET_X86
    // Size of outgoing arguments (on stack). This is currently used only
    // on x86 when we have an InlinedCallFrame representing a CLR->COM call.
    WORD        m_cbStackArgumentSize;

    void InitStackArgumentSize()
    {
        LIMITED_METHOD_CONTRACT;

        m_cbStackArgumentSize = 0xFFFF;
    }

    void SetStackArgumentSize(WORD cbDstBuffer)
    {
        LIMITED_METHOD_CONTRACT;

        // Don't write to the field if it's already initialized to avoid creating private pages (NGEN)
        if (m_cbStackArgumentSize == 0xFFFF)
        {
            m_cbStackArgumentSize = cbDstBuffer;
        }
        _ASSERTE(m_cbStackArgumentSize == cbDstBuffer);
    }

    WORD GetStackArgumentSize()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        _ASSERTE(m_cbStackArgumentSize != 0xFFFF);
        return m_cbStackArgumentSize;
    }

    LPVOID      m_pRetThunk;

#else // TARGET_X86
    void InitStackArgumentSize()
    {
        LIMITED_METHOD_CONTRACT;
    }

    void SetStackArgumentSize(WORD cbDstBuffer)
    {
        LIMITED_METHOD_CONTRACT;
    }
#endif // TARGET_X86
};


//-----------------------------------------------------------------------
// Operations specific to ComPlusCall methods. We use a derived class to get
// the compiler involved in enforcing proper method type usage.
// DO NOT ADD FIELDS TO THIS CLASS.
//-----------------------------------------------------------------------
class ComPlusCallMethodDesc : public MethodDesc
{
public:
    ComPlusCallInfo *m_pComPlusCallInfo; // initialized in code:ComPlusCall.PopulateComPlusCallMethodDesc

    void InitRetThunk();
    void InitComEventCallInfo();

    PCODE * GetAddrOfILStubField()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pComPlusCallInfo->GetAddrOfILStubField();
    }

    MethodTable* GetInterfaceMethodTable()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(m_pComPlusCallInfo->m_pInterfaceMT != NULL);
        return m_pComPlusCallInfo->m_pInterfaceMT;
    }

    MethodDesc* GetEventProviderMD()
    {
        LIMITED_METHOD_CONTRACT;

        return m_pComPlusCallInfo->m_pEventProviderMD;
    }


    BOOL RequiresArgumentWrapping()
    {
        LIMITED_METHOD_CONTRACT;

        return (m_pComPlusCallInfo->m_flags & ComPlusCallInfo::kRequiresArgumentWrapping) != 0;
    }

    void SetLateBoundFlags(BYTE newFlags)
    {
        LIMITED_METHOD_CONTRACT;

        InterlockedOr((LONG*)&m_pComPlusCallInfo->m_flags, newFlags);
    }

#ifdef TARGET_X86
    WORD GetStackArgumentSize()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_pComPlusCallInfo->GetStackArgumentSize();
    }

    void SetStackArgumentSize(WORD cbDstBuffer)
    {
        LIMITED_METHOD_CONTRACT;
        m_pComPlusCallInfo->SetStackArgumentSize(cbDstBuffer);
    }
#else // TARGET_X86
    void SetStackArgumentSize(WORD cbDstBuffer)
    {
        LIMITED_METHOD_CONTRACT;
    }
#endif // TARGET_X86
};
#endif // FEATURE_COMINTEROP

//-----------------------------------------------------------------------
// InstantiatedMethodDesc's are used for generics and
// come in four flavours, discriminated by the
// low order bits of the first field:
//
//  001 --> GenericMethodDefinition
//  010 --> UnsharedMethodInstantiation
//  011 --> SharedMethodInstantiation
//  100 --> WrapperStubWithInstantiations - and unboxing or instantiating stub
//
// A SharedMethodInstantiation descriptor extends MethodDesc
// with a pointer to dictionary layout and a representative instantiation.
//
// A GenericMethodDefinition is the instantiation of a
// generic method at its formals, used for verifying the method and
// also for reflection.
//
// A WrapperStubWithInstantiations extends MethodDesc with:
//    (1) a method instantiation
//    (2) an "underlying" method descriptor.
// A WrapperStubWithInstantiations may be placed in a MethodChunk for
// a method table which specifies an exact instantiation for the class/struct.
// A WrapperStubWithInstantiations may be either
// an BoxedEntryPointStub or an exact-instantiation stub.
//
// Exact-instantiation stubs are used as extra type-context parameters. When
// used as an entry, instantiating stubs pass an instantiation
// dictionary on to the underlying method.  These entries are required to
// implement ldftn instructions on instantiations of shared generic
// methods, as the InstantiatingStub's pointer does not expect a
// dictionary argument; instead, it passes itself on to the shared
// code as the dictionary.
//
// An UnsharedMethodInstantiation contains just an instantiation.
// These are fully-specialized wrt method and class type parameters.
// These satisfy (!IMD_IsGenericMethodDefinition() &&
//                !IMD_IsSharedByGenericMethodInstantiations() &&
//                !IMD_IsWrapperStubWithInstantiations())
//
// Note that plain MethodDescs may represent shared code w.r.t. class type
// parameters (see MethodDesc::IsSharedByGenericInstantiations()).
//-----------------------------------------------------------------------

class InstantiatedMethodDesc final : public MethodDesc
{

public:

    // All varities of InstantiatedMethodDesc's support this method.
    BOOL IMD_HasMethodInstantiation()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        if (IMD_IsGenericMethodDefinition())
            return TRUE;
        else
            return m_pPerInstInfo != NULL;
    }

    // All varieties of InstantiatedMethodDesc's support this method.
    Instantiation IMD_GetMethodInstantiation()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        // No lock needed here. In the case of a generic dictionary expansion, the values of the old dictionary
        // slots are copied to the newly allocated dictionary, and the old dictionary is kept around. Whether we
        // return the old or new dictionary here, the values of the instantiation arguments will always be the same.
        return (m_pPerInstInfo != NULL)
                ? Instantiation(m_pPerInstInfo->GetInstantiation(), m_wNumGenericArgs)
                : Instantiation();
    }

    PTR_Dictionary IMD_GetMethodDictionary()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return m_pPerInstInfo;
    }

    BOOL IMD_IsGenericMethodDefinition()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return((m_wFlags2 & KindMask) == GenericMethodDefinition);
    }

    BOOL IMD_IsSharedByGenericMethodInstantiations()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return((m_wFlags2 & KindMask) == SharedMethodInstantiation);
    }
    BOOL IMD_IsWrapperStubWithInstantiations()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return((m_wFlags2 & KindMask) == WrapperStubWithInstantiations);
    }

    PTR_DictionaryLayout GetDictLayoutRaw()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_pDictLayout;
    }

    PTR_MethodDesc IMD_GetWrappedMethodDesc()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        _ASSERTE(IMD_IsWrapperStubWithInstantiations());
        return m_pWrappedMethodDesc;
    }

#ifndef DACCESS_COMPILE
    // Get the dictionary layout, if there is one
    DictionaryLayout* IMD_GetDictionaryLayout()
    {
        WRAPPER_NO_CONTRACT;
        if (IMD_IsWrapperStubWithInstantiations() && IMD_HasMethodInstantiation())
        {
            InstantiatedMethodDesc* pIMD = IMD_GetWrappedMethodDesc()->AsInstantiatedMethodDesc();
            return pIMD->m_pDictLayout;
        }
        else if (IMD_IsSharedByGenericMethodInstantiations())
            return m_pDictLayout;
        else
            return NULL;
    }

    void IMD_SetDictionaryLayout(DictionaryLayout* pNewLayout)
    {
        WRAPPER_NO_CONTRACT;
        if (IMD_IsWrapperStubWithInstantiations() && IMD_HasMethodInstantiation())
        {
            InstantiatedMethodDesc* pIMD = IMD_GetWrappedMethodDesc()->AsInstantiatedMethodDesc();
            pIMD->m_pDictLayout = pNewLayout;
        }
        else if (IMD_IsSharedByGenericMethodInstantiations())
        {
            m_pDictLayout = pNewLayout;
        }
    }
#endif // !DACCESS_COMPILE

    // Setup the IMD as shared code
    void SetupSharedMethodInstantiation(DWORD numGenericArgs, TypeHandle *pPerInstInfo, DictionaryLayout *pDL);

    // Setup the IMD as unshared code
    void SetupUnsharedMethodInstantiation(DWORD numGenericArgs, TypeHandle *pInst);

    // Setup the IMD as the special MethodDesc for a "generic" method
    void SetupGenericMethodDefinition(IMDInternalImport *pIMDII, LoaderAllocator* pAllocator, AllocMemTracker *pamTracker,
        Module *pModule, mdMethodDef tok);

    // Setup the IMD as a wrapper around another method desc
    void SetupWrapperStubWithInstantiations(MethodDesc* wrappedMD,DWORD numGenericArgs, TypeHandle *pGenericMethodInst);

private:
    friend class MethodDesc; // this fields are currently accessed by MethodDesc::Save/Restore etc.
    union {
        PTR_DictionaryLayout m_pDictLayout; //SharedMethodInstantiation

        PTR_MethodDesc m_pWrappedMethodDesc; // For WrapperStubWithInstantiations
    };

public: // <TODO>make private: JITinterface.cpp accesses through this </TODO>
    // Note we can't steal bits off m_pPerInstInfo as the JIT generates code to access through it!!

    // Type parameters to method (exact)
    // For non-unboxing instantiating stubs this is actually
    // a dictionary and further slots may hang off the end of the
    // instantiation.
    //
    // For generic method definitions that are not the typical method definition (e.g. C<int>.m<U>)
    // this field is null; to obtain the instantiation use LoadMethodInstantiation
    PTR_Dictionary m_pPerInstInfo;  //SHARED

private:
    enum
    {
        KindMask                        = 0x07,
        GenericMethodDefinition         = 0x01,
        UnsharedMethodInstantiation     = 0x02,
        SharedMethodInstantiation       = 0x03,
        WrapperStubWithInstantiations   = 0x04,
    };
    WORD          m_wFlags2;
    WORD          m_wNumGenericArgs;

public:
    static InstantiatedMethodDesc *FindOrCreateExactClassMethod(MethodTable *pExactMT,
                                                                MethodDesc *pCanonicalMD);

    static InstantiatedMethodDesc* FindLoadedInstantiatedMethodDesc(MethodTable *pMT,
                                                                    mdMethodDef methodDef,
                                                                    Instantiation methodInst,
                                                                    BOOL getSharedNotStub);

private:

    static InstantiatedMethodDesc *NewInstantiatedMethodDesc(MethodTable *pMT,
                                                             MethodDesc* pGenericMDescInRepMT,
                                                             MethodDesc* pSharedMDescForStub,
                                                             Instantiation methodInst,
                                                             BOOL getSharedNotStub);

};

inline PTR_MethodTable MethodDesc::GetMethodTable() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    MethodDescChunk *pChunk = GetMethodDescChunk();
    PREFIX_ASSUME(pChunk != NULL);
    return pChunk->GetMethodTable();
}

inline DPTR(PTR_MethodTable) MethodDesc::GetMethodTablePtr() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    MethodDescChunk *pChunk = GetMethodDescChunk();
    PREFIX_ASSUME(pChunk != NULL);
    return pChunk->GetMethodTablePtr();
}

inline MethodTable* MethodDesc::GetCanonicalMethodTable()
{
    LIMITED_METHOD_DAC_CONTRACT;

    return GetMethodTable()->GetCanonicalMethodTable();
}

inline mdMethodDef MethodDesc::GetMemberDef() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    MethodDescChunk *pChunk = GetMethodDescChunk();
    PREFIX_ASSUME(pChunk != NULL);
    UINT16   tokrange = pChunk->GetTokRange();

    UINT16 tokremainder = m_wFlags3AndTokenRemainder & enum_flag3_TokenRemainderMask;
    static_assert_no_msg(enum_flag3_TokenRemainderMask == METHOD_TOKEN_REMAINDER_MASK);

    return MergeToken(tokrange, tokremainder);
}

// Set the offset of this method desc in a chunk table (which allows us
// to work back to the method table/module pointer stored at the head of
// the table.
inline void MethodDesc::SetChunkIndex(MethodDescChunk * pChunk)
{
    WRAPPER_NO_CONTRACT;

    // Calculate the offset (mod 8) from the chunk table header.
    SIZE_T offset = (BYTE*)this - (BYTE*)pChunk->GetFirstMethodDesc();
    _ASSERTE((offset & ALIGNMENT_MASK) == 0);
    offset >>= ALIGNMENT_SHIFT;

    // Make sure that we did not overflow the BYTE
    _ASSERTE(offset == (BYTE)offset);
    m_chunkIndex = (BYTE)offset;

    // Make sure that the MethodDescChunk is setup correctly
    _ASSERTE(GetMethodDescChunk() == pChunk);
}

inline void MethodDesc::SetMemberDef(mdMethodDef mb)
{
    WRAPPER_NO_CONTRACT;

    UINT16 tokrange;
    UINT16 tokremainder;
    SplitToken(mb, &tokrange, &tokremainder);

    _ASSERTE((tokremainder & ~enum_flag3_TokenRemainderMask) == 0);
    m_wFlags3AndTokenRemainder = (m_wFlags3AndTokenRemainder & ~enum_flag3_TokenRemainderMask) | tokremainder;

    if (GetMethodDescChunkIndex() == 0)
    {
        GetMethodDescChunk()->SetTokenRange(tokrange);
    }

#ifdef _DEBUG
    if (mb != 0)
    {
        _ASSERTE(GetMemberDef() == mb);
    }
#endif
}

#ifdef _DEBUG

inline BOOL MethodDesc::SanityCheck()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    // Sanity test - we don't care about the result we just want it to not AV.
    return GetMethodTable() == m_pDebugMethodTable && this->GetModule() != NULL;
}

#endif // _DEBUG

inline BOOL MethodDesc::HasNonVtableSlot()
{
    LIMITED_METHOD_DAC_CONTRACT;

    return (m_wFlags & mdcHasNonVtableSlot) != 0;
}

inline Instantiation MethodDesc::GetMethodInstantiation() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    return
        (GetClassification() == mcInstantiated)
        ? AsInstantiatedMethodDesc()->IMD_GetMethodInstantiation()
        : Instantiation();
}

inline Instantiation MethodDesc::GetClassInstantiation() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    return GetMethodTable()->GetInstantiation();
}

inline BOOL MethodDesc::IsGenericMethodDefinition() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    return GetClassification() == mcInstantiated && AsInstantiatedMethodDesc()->IMD_IsGenericMethodDefinition();
}

// True if the method descriptor is an instantiation of a generic method.
inline BOOL MethodDesc::HasMethodInstantiation() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    return mcInstantiated == GetClassification() && AsInstantiatedMethodDesc()->IMD_HasMethodInstantiation();
}

#if defined(FEATURE_GDBJIT)
class CalledMethod
{
private:
    MethodDesc * m_pMD;
    void * m_CallAddr;
    CalledMethod * m_pNext;
public:
    CalledMethod(MethodDesc *pMD, void * addr, CalledMethod * next) : m_pMD(pMD), m_CallAddr(addr), m_pNext(next)  {}
    ~CalledMethod() {}
    MethodDesc * GetMethodDesc() { return m_pMD; }
    void * GetCallAddr() { return m_CallAddr; }
    CalledMethod * GetNext() { return m_pNext; }
};
#endif

#ifdef FEATURE_READYTORUN
struct ReadyToRunStandaloneMethodMetadata
{
    ReadyToRunStandaloneMethodMetadata() :
        pByteData(nullptr),
        cByteData(0),
        pTypes(nullptr),
        cTypes(0)
    {}

    ~ReadyToRunStandaloneMethodMetadata()
    {
        if (pByteData != nullptr)
            delete[] pByteData;
        if (pTypes != nullptr)
            delete[] pTypes;
    }

    const uint8_t * pByteData;
    size_t cByteData;
    const TypeHandle * pTypes;
    size_t cTypes;
};

ReadyToRunStandaloneMethodMetadata* GetReadyToRunStandaloneMethodMetadata(MethodDesc *pMD);
void InitReadyToRunStandaloneMethodMetadata();
#endif // FEATURE_READYTORUN

#include "method.inl"

#endif // !_METHOD_H
