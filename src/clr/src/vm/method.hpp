//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//
// method.hpp
//

//
// See the book of the runtime entry for overall design:
// file:../../doc/BookOfTheRuntime/ClassLoader/MethodDescDesign.doc
//

#ifndef _METHOD_H 
#define _METHOD_H

#ifndef BINDER

#include "cor.h"
#include "util.hpp"
#include "clsload.hpp"
#include "codeman.h"
#include "class.h"
#include "siginfo.hpp"
#include "declsec.h"
#include "methodimpl.h"
#include "typedesc.h"
#include <stddef.h>
#include "eeconfig.h"
#include "precode.h"

#ifndef FEATURE_PREJIT
#include "fixuppointer.h"
#endif
#else // BINDER

#include "fixuppointer.h"

#define COMPLUSCALL_METHOD_DESC_ALIGNPAD_BYTES  3   // # bytes required to pad ComPlusCallMethodDesc to correct size

#endif // BINDER

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

#define METHOD_TOKEN_REMAINDER_BIT_COUNT 14
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

    // Method is static
    mdcStatic                           = 0x0020,

    // Temporary Security Interception.
    // Methods can now be intercepted by security. An intercepted method behaves
    // like it was an interpreted method. The Prestub at the top of the method desc
    // is replaced by an interception stub. Therefore, no back patching will occur.
    // We picked this approach to minimize the number variations given IL and native
    // code with edit and continue. E&C will need to find the real intercepted method
    // and if it is intercepted change the real stub. If E&C is enabled then there
    // is no back patching and needs to fix the pre-stub.
    mdcIntercepted                      = 0x0040,

    // Method requires linktime security checks.
    mdcRequiresLinktimeCheck            = 0x0080,

#if defined(CLR_STANDALONE_BINDER)
    // Binder optimization - we have already parsed the signature 
    // of this method desc and it contains no user-defined value types (including enums)
    mdcSignatureHasNoValueTypes         = 0x0100,

    // This should contain bits used for binder-internal purposes - reset these
    // before persisting the method descs
    mdcBinderBits                       = mdcSignatureHasNoValueTypes,
#else
    // Method requires inheritance security checks.
    // If this bit is set, then this method demands inheritance permissions
    // or a method that this method overrides demands inheritance permissions
    // or both.
    mdcRequiresInheritanceCheck         = 0x0100,

    // The method that this method overrides requires an inheritance security check.
    // This bit is used as an optimization to avoid looking up overridden methods
    // during the inheritance check.
    mdcParentRequiresInheritanceCheck   = 0x0200,
#endif

    // Duplicate method. When a method needs to be placed in multiple slots in the
    // method table, because it could not be packed into one slot. For eg, a method
    // providing implementation for two interfaces, MethodImpl, etc
    mdcDuplicate                        = 0x0400,

    // Has this method been verified?
    mdcVerifiedState                    = 0x0800,

    // Is the method verifiable? It needs to be verified first to determine this
    mdcVerifiable                       = 0x1000,

    // Is this method ineligible for inlining?
    mdcNotInline                        = 0x2000,

    // Is the method synchronized
    mdcSynchronized                     = 0x4000,

    // Does the method's slot number require all 16 bits
    mdcRequiresFullSlotNumber           = 0x8000
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
// program exectution, but we often fall short of that goal.  
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
#ifdef DACCESS_COMPILE
    friend class NativeImageDumper;
#endif

public:

    enum
    {
#ifdef _WIN64
        ALIGNMENT_SHIFT = 3,
#else
        ALIGNMENT_SHIFT = 2,
#endif
        ALIGNMENT       = (1<<ALIGNMENT_SHIFT),
        ALIGNMENT_MASK  = (ALIGNMENT-1)
    };

#ifdef _DEBUG 

    // These are set only for MethodDescs but every time we want to use the debugger
    // to examine these fields, the code has the thing stored in a MethodDesc*.
    // So...
    LPCUTF8         m_pszDebugMethodName;
    LPCUTF8         m_pszDebugClassName;
    LPCUTF8         m_pszDebugMethodSignature;
    FixupPointer<PTR_MethodTable>   m_pDebugMethodTable;

    PTR_GCCoverageInfo m_GcCover;

#endif // _DEBUG

    inline BOOL HasStableEntryPoint()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return (m_bFlags2 & enum_flag2_HasStableEntryPoint) != 0;
    }

    inline PCODE GetStableEntryPoint()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        _ASSERTE(HasStableEntryPoint());
        return GetMethodEntryPoint();
    }

    BOOL SetStableEntryPointInterlocked(PCODE addr);

#ifdef FEATURE_INTERPRETER
    BOOL SetEntryPointInterlocked(PCODE addr);
#endif // FEATURE_INTERPRETER

    BOOL HasTemporaryEntryPoint();
    PCODE GetTemporaryEntryPoint();

    void SetTemporaryEntryPoint(LoaderAllocator *pLoaderAllocator, AllocMemTracker *pamTracker);

    inline BOOL HasPrecode()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return (m_bFlags2 & enum_flag2_HasPrecode) != 0;
    }

#ifndef BINDER
    inline Precode* GetPrecode()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        PRECONDITION(HasPrecode());
        Precode* pPrecode = Precode::GetPrecodeFromEntryPoint(GetStableEntryPoint());
        PREFIX_ASSUME(pPrecode != NULL);
        return pPrecode;
    }
#endif // !BINDER

    inline BOOL MayHavePrecode()
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END

        return !MayHaveNativeCode() || IsRemotingInterceptedViaPrestub();
    }

#ifdef BINDER
    inline void SetHasPrecode()
    {
        LIMITED_METHOD_CONTRACT;
        m_bFlags2 |= (enum_flag2_HasPrecode | enum_flag2_HasStableEntryPoint);
    }

    inline void ResetHasPrecode()
    {

        LIMITED_METHOD_CONTRACT;
        m_bFlags2 &= ~enum_flag2_HasPrecode;
        m_bFlags2 |= enum_flag2_HasStableEntryPoint;
    }
#endif // BINDER

    void InterlockedUpdateFlags2(BYTE bMask, BOOL fSet);

    Precode* GetOrCreatePrecode();

#ifdef FEATURE_PREJIT
    Precode *     GetSavedPrecode(DataImage *image);
    Precode *     GetSavedPrecodeOrNull(DataImage *image);
#endif // FEATURE_PREJIT

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

    void PrecomputeNameHash();
    BOOL MightHaveName(ULONG nameHashValue);

#ifndef BINDER
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
#endif // !BINDER

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


    // True if and only if this is a method desriptor for :
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
#ifndef BINDER
        // Once we stop allocating dummy MethodImplSlot in MethodTableBuilder::WriteMethodImplData,
        // the check for NULL will become unnecessary.
        return HasMethodImplSlot() && (GetMethodImpl()->GetSlots() != NULL);
#else // BINDER
        return FALSE;
#endif // BINDER
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

    BOOL HasClassOrMethodInstantiation_NoLogging() const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return (HasClassInstantiation_NoLogging() || HasMethodInstantiation());
    }

    inline BOOL HasClassInstantiation() const 
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return GetMethodTable()->HasInstantiation();
    }

    inline BOOL HasClassInstantiation_NoLogging() const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return GetMethodTable_NoLogging()->HasInstantiation();
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

    BaseDomain *GetDomain();

    ReJitManager * GetReJitManager();

    PTR_LoaderAllocator GetLoaderAllocator();

    // GetLoaderAllocatorForCode returns the allocator with the responsibility for allocation.
    // This is called from GetMulticallableAddrOfCode when allocating a small trampoline stub for the method.
    // Normally a method in a shared domain will allocate memory for stubs in the shared domain.
    // That has to be different for DynamicMethod as they need to be allocated always in the AppDomain
    // that created the method.
    LoaderAllocator * GetLoaderAllocatorForCode();

    // GetDomainSpecificLoaderAllocator returns the collectable loader allocator for collectable types
    // and the loader allocator in the current domain for non-collectable types
    LoaderAllocator * GetDomainSpecificLoaderAllocator();

    inline BOOL IsDomainNeutral();

#ifdef BINDER
    MdilModule* GetLoaderModule();

    MdilModule* GetZapModule();
#else // !BINDER
    Module* GetLoaderModule();

    Module* GetZapModule();
#endif

    // Does this immediate item live in an NGEN module?
    BOOL IsZapped();

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

    void ComputeSuppressUnmanagedCodeAccessAttr(IMDInternalImport *pImport);
    BOOL HasSuppressUnmanagedCodeAccessAttr();

#ifdef FEATURE_COMINTEROP 
    inline DWORD IsComPlusCall()
    {
        WRAPPER_NO_CONTRACT;
        return mcComInterop == GetClassification();
    }
    inline DWORD IsGenericComPlusCall();
    inline void SetupGenericComPlusCall();
#else // !FEATURE_COMINTEROP
     // hardcoded to return FALSE to improve code readibility
    inline DWORD IsComPlusCall()
    {
        LIMITED_METHOD_CONTRACT;
        return FALSE;
    }
    inline DWORD IsGenericComPlusCall()
    {
        LIMITED_METHOD_CONTRACT;
        return FALSE;
    }
#endif // !FEATURE_COMINTEROP

    // Update flags in a thread safe manner.
    WORD InterlockedUpdateFlags(WORD wMask, BOOL fSet);

    inline DWORD IsInterceptedForDeclSecurity()
    {
        LIMITED_METHOD_CONTRACT;
        STATIC_CONTRACT_SO_TOLERANT;        
        return m_wFlags & mdcIntercepted;
    }

    inline void SetInterceptedForDeclSecurity()
    {
        LIMITED_METHOD_CONTRACT;
        m_wFlags |= mdcIntercepted;
    }

    inline DWORD IsInterceptedForDeclSecurityCASDemandsOnly()
    {
        LIMITED_METHOD_CONTRACT;
        STATIC_CONTRACT_SO_TOLERANT;        
        return m_bFlags2 & enum_flag2_CASDemandsOnly;
    }

    inline void SetInterceptedForDeclSecurityCASDemandsOnly()
    {
        LIMITED_METHOD_CONTRACT;
        m_bFlags2 |= enum_flag2_CASDemandsOnly;
    }

#ifndef BINDER
    // If the method is in an Edit and Contine (EnC) module, then
    // we DON'T want to backpatch this, ever.  We MUST always call
    // through the precode so that we can update the method.
    inline DWORD IsEnCMethod()
    {
        WRAPPER_NO_CONTRACT;
        Module *pModule = GetModule();
        PREFIX_ASSUME(pModule != NULL);
        return pModule->IsEditAndContinueEnabled();
    }
#endif // !BINDER

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


    BOOL IsIntrospectionOnly();
#ifndef DACCESS_COMPILE
    VOID EnsureActive();
#endif
    CHECK CheckActivated();


    //================================================================
    // REMOTING
    //
    // IsRemoting...: These predicates indicate how are remoting
    // intercepts are implemented.
    //
    // Remoting intercepts are required for all invocations of  methods on
    // MarshalByRef classes (including virtual calls on methods
    // which end up invoking a method on the MarshalByRef class).
    //
    // Remoting intercepts are implemented by one of the following techniques:
    //  (1) Non-virtual methods: inserting a stub in DoPrestub (for non-virtual calls)
    //   See: IsRemotingInterceptedViaPrestub
    //
    //  (2) Virtual methods: by transparent proxy vtables, where all the entries in the vtable
    //      are a special hook which traps into the remoting logic
    //   See: IsRemotingInterceptedViaVirtualDispatch (context indicates
    //        if it is a virtual call)
    //
    //  (3) Non-virtual-calls on virtual methods:
    //      by forcing calls to be indirect and wrapping the
    //      call with a stub returned by GetNonVirtualEntryPointForVirtualMethod.
    //      (this is used when invoking virtual methods non-virtually using 'call')
    //   See: IsRemotingInterceptedViaVirtualDispatch (context indicates
    //        if it is a virtual call)
    //
    // Ultimately essentially all calls go through CTPMethodTable::OnCall in
    // remoting.cpp.
    //
    // Check if this methoddesc needs to be intercepted
    // by the context code, using a stub.
    // Also see IsRemotingInterceptedViaVirtualDispatch()
    BOOL IsRemotingInterceptedViaPrestub();

    // Check if is intercepted by the context code, using the virtual table
    // of TransparentProxy.
    // If such a function is called non-virtually, it needs to be handled specially
    BOOL IsRemotingInterceptedViaVirtualDispatch();

    BOOL MayBeRemotingIntercepted();

#ifndef BINDER
    //================================================================
    // Does it represent a one way method call with no out/return parameters?
#ifdef FEATURE_REMOTING
    inline BOOL IsOneWay()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END

        return (S_OK == GetMDImport()->GetCustomAttributeByName(GetMemberDef(),
                                                                "System.Runtime.Remoting.Messaging.OneWayAttribute",
                                                                NULL,
                                                                NULL));

    }
#endif // FEATURE_REMOTING
#endif

    //================================================================
    // FCalls.
    BOOL IsFCall()
    {
        WRAPPER_NO_CONTRACT;
        return mcFCall == GetClassification();
    }

    BOOL IsFCallOrIntrinsic();

    BOOL IsQCall();

    //================================================================
    // Has the method been verified?
    // This does not mean that the IL is verifiable, just that we have
    // determined if the IL is verfiable or unverifiable.
    // (Is this is dead code since the JIT now does verification?)

    inline BOOL IsVerified()
    {
        LIMITED_METHOD_CONTRACT;
        return m_wFlags & mdcVerifiedState;
    }

    inline void SetIsVerified(BOOL isVerifiable)
    {
        WRAPPER_NO_CONTRACT;

        WORD flags = isVerifiable ? (WORD(mdcVerifiedState) | WORD(mdcVerifiable))
                                  : (WORD(mdcVerifiedState));
        InterlockedUpdateFlags(flags, TRUE);
    }

    inline void ResetIsVerified()
    {
        WRAPPER_NO_CONTRACT;
        InterlockedUpdateFlags(mdcVerifiedState | mdcVerifiable, FALSE);
    }

    BOOL IsVerifiable();

    // fThrowException is used to prevent Verifier from
    // throwin an exception on error
    // fForceVerify is to be used by tools that need to
    // force verifier to verify code even if the code is fully trusted.
    HRESULT Verify(COR_ILMETHOD_DECODER* ILHeader,
                   BOOL fThrowException,
                   BOOL fForceVerify);


    //================================================================
    //

    inline void ClearFlagsOnUpdate()
    {
        WRAPPER_NO_CONTRACT;
        ResetIsVerified();
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

        return (m_bFlags2 & enum_flag2_IsUnboxingStub) != 0;
    }

    void SetIsUnboxingStub()
    {    
        LIMITED_METHOD_CONTRACT;
        m_bFlags2 |= enum_flag2_IsUnboxingStub;
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

#ifndef BINDER

    //==================================================================
    // Access the underlying metadata

    BOOL HasILHeader()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            SO_TOLERANT;
            MODE_ANY;
        }
        CONTRACTL_END;
        return IsIL() && !IsUnboxingStub() && GetRVA();
    }

    COR_ILMETHOD* GetILHeader(BOOL fAllowOverrides = FALSE);
#endif // !BINDER

    BOOL HasStoredSig()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return IsEEImpl() || IsArray() || IsNoMetadata();
    }

    PCCOR_SIGNATURE GetSig();

    void GetSig(PCCOR_SIGNATURE *ppSig, DWORD *pcSig);
    SigParser GetSigParser();
#ifndef BINDER


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
#endif // !BINDER

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

    // Be careful about races with profiler when using this method. The profiler can 
    // replace preimplemented code of the method with jitted code.
    // Avoid code patterns like if(IsPreImplemented()) { PCODE pCode = GetPreImplementedCode(); ... }.
    // Use PCODE pCode = GetPreImplementedCode(); if (pCode != NULL) { ... } instead.
    BOOL IsPreImplemented()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return GetPreImplementedCode() != NULL;
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
        g_IBCLogger.LogMethodTableAccess(pMT);
        return
            !IsEnCAddedMethod()
            // The slot numbers are currently meaningless for
            // some unboxed-this-generic-method-instantiations
            && !(pMT->IsValueType() && !IsStatic() && !IsUnboxingStub())
            && GetSlot() < pMT->GetNumVirtuals();
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
    // EnC

    inline BOOL IsEnCAddedMethod();

    //==================================================================
    //

    inline EEClass* GetClass()
    {
        WRAPPER_NO_CONTRACT;
        MethodTable *pMT = GetMethodTable_NoLogging();
        g_IBCLogger.LogEEClassAndMethodTableAccess(pMT);
        EEClass *pClass = pMT->GetClass_NoLogging();
        PREFIX_ASSUME(pClass != NULL);
        return pClass;
    }

    inline PTR_MethodTable GetMethodTable() const;
    inline PTR_MethodTable GetMethodTable_NoLogging() const;

    inline DPTR(RelativeFixupPointer<PTR_MethodTable>) GetMethodTablePtr() const;

  public:
    inline MethodDescChunk* GetMethodDescChunk() const;
    inline int GetMethodDescIndex() const;
    // If this is an method desc. (whether non-generic shared-instantiated or exact-instantiated)
    // inside a shared class then get the method table for the representative
    // class.
    inline MethodTable* GetCanonicalMethodTable();

#ifdef BINDER
    MdilModule *GetModule();
#else
    Module *GetModule() const;
    Module *GetModule_NoLogging() const;

    Assembly *GetAssembly() const
    {
        WRAPPER_NO_CONTRACT;
        Module *pModule = GetModule();
        PREFIX_ASSUME(pModule != NULL);
        return pModule->GetAssembly();
    }
#endif // !BINDER

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
#ifndef DACCESS_COMPILE
        // The DAC build uses this method to test for "sanity" of a MethodDesc, and
        // doesn't need the assert.
        _ASSERTE(! IsEnCAddedMethod() || !"Cannot get slot for method added via EnC");
#endif // !DACCESS_COMPILE

        // Check if this MD is using the packed slot layout
        if (!RequiresFullSlotNumber())
        {
            return (m_wSlotNumber & enum_packedSlotLayout_SlotMask);
        }

        return m_wSlotNumber;
    }

    inline VOID SetSlot(WORD wSlotNum)
    {
        LIMITED_METHOD_CONTRACT;

        // Check if we have to avoid using the packed slot layout
        if (wSlotNum > enum_packedSlotLayout_SlotMask)
        {
            SetRequiresFullSlotNumber();
        }
#ifdef  CLR_STANDALONE_BINDER
        else if (RequiresFullSlotNumber())
        {
            ClearRequiresFullSlotNumber();
            m_wSlotNumber = 0;
        }
#endif

        // Set only the portion of m_wSlotNumber we are using
        if (!RequiresFullSlotNumber())
        {
            m_wSlotNumber &= ~enum_packedSlotLayout_SlotMask;
            m_wSlotNumber |= wSlotNum;
        }
        else
        {
            m_wSlotNumber = wSlotNum;
        }
    }

    PTR_PCODE GetAddrOfSlot();

    PTR_MethodDesc GetDeclMethodDesc(UINT32 slotNumber);

protected:
    inline void SetRequiresFullSlotNumber()
    {
        LIMITED_METHOD_CONTRACT;
        m_wFlags |= mdcRequiresFullSlotNumber;
    }

#ifdef CLR_STANDALONE_BINDER
    inline void ClearRequiresFullSlotNumber()
    {

        LIMITED_METHOD_CONTRACT;
        m_wFlags &= ~mdcRequiresFullSlotNumber;
    }
#endif

    inline DWORD RequiresFullSlotNumber()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return (m_wFlags & mdcRequiresFullSlotNumber) != 0;
    }

public:
    //==================================================================
    // Security...

    DWORD GetSecurityFlagsDuringPreStub();
    DWORD GetSecurityFlagsDuringClassLoad(IMDInternalImport *pInternalImport,
                           mdToken tkMethod, mdToken tkClass,
                           DWORD *dwClassDeclFlags, DWORD *dwClassNullDeclFlags,
                           DWORD *dwMethDeclFlags, DWORD *dwMethNullDeclFlags);

    inline DWORD RequiresLinktimeCheck()
    {
        LIMITED_METHOD_CONTRACT;
        return m_wFlags & mdcRequiresLinktimeCheck;
    }

#if defined(CLR_STANDALONE_BINDER)
    inline BOOL SignatureHasNoValueTypes()
    {
        LIMITED_METHOD_CONTRACT;
        return m_wFlags & mdcSignatureHasNoValueTypes;
    }

    inline void SetSignatureHasNoValueTypes()
    {
        LIMITED_METHOD_CONTRACT;
        m_wFlags |= mdcSignatureHasNoValueTypes;
    }

    // Clear bits used for binder-internal purposes
    inline void ClearBinderBits()
    {
        LIMITED_METHOD_CONTRACT;
        m_wFlags &= ~mdcBinderBits;
    }
#else
    inline DWORD RequiresInheritanceCheck()
    {
        LIMITED_METHOD_CONTRACT;
        return m_wFlags & mdcRequiresInheritanceCheck;
    }

    inline DWORD ParentRequiresInheritanceCheck()
    {
        LIMITED_METHOD_CONTRACT;
        return m_wFlags & mdcParentRequiresInheritanceCheck;
    }
#endif

    void SetRequiresLinktimeCheck()
    {
        LIMITED_METHOD_CONTRACT;
        m_wFlags |= mdcRequiresLinktimeCheck;
    }

#ifndef BINDER
    void SetRequiresInheritanceCheck()
    {
        LIMITED_METHOD_CONTRACT;
        m_wFlags |= mdcRequiresInheritanceCheck;
    }

    void SetParentRequiresInheritanceCheck()
    {
        LIMITED_METHOD_CONTRACT;
        m_wFlags |= mdcParentRequiresInheritanceCheck;
    }
#endif

    mdMethodDef GetMemberDef() const;
    mdMethodDef GetMemberDef_NoLogging() const;

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
#ifdef FEATURE_INTERPRETER
    BOOL IsReallyPointingToPrestub();
#endif // FEATURE_INTERPRETER

public:

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

#ifdef BINDER
        return TRUE;
#else // !BINDER
        return GetPrecode()->IsPointingToNativeCode(GetNativeCode());
#endif // BINDER
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

#ifdef FEATURE_INTERPRETER
    BOOL SetNativeCodeInterlocked(PCODE addr, PCODE pExpected, BOOL fStable);
#else  // FEATURE_INTERPRETER
    BOOL SetNativeCodeInterlocked(PCODE addr, PCODE pExpected = NULL);
#endif // FEATURE_INTERPRETER

    TADDR GetAddrOfNativeCodeSlot();

    BOOL MayHaveNativeCode();

    ULONG GetRVA();

    BOOL IsClassConstructorTriggeredViaPrestub();

public:

    // Returns preimplemented code of the method if method has one.
    // Returns NULL if method has no preimplemented code.
    // Be careful about races with profiler when using this method. The profiler can 
    // replace preimplemented code of the method with jitted code.
    PCODE GetPreImplementedCode();

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
    // callable entrypoint would result into unnecesary allocation of indirection stub. Caller should use
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
    // Returns the address of the native code. The native code can be one of:
    // - jitted code if !IsPreImplemented()
    // - ngened code if IsPreImplemented()
    PCODE GetNativeCode();

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
    // of correctness to pass in the the corresponding non-unboxing MD.
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
    // an NGEN image then then other will be, and if one is "used" at runtime then
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


public:

    // does this function return an object reference?
    MetaSig::RETURNTYPE ReturnsObject(
#ifdef _DEBUG 
    bool supportStringConstructors = false
#endif
        );


    void Destruct();

public:
    // In general you don't want to call GetCallTarget - you want to
    // use either "call" directly or call MethodDesc::GetSingleCallableAddrOfVirtualizedCode and
    // then "CallTarget".  Note that GetCallTarget is approximately GetSingleCallableAddrOfCode
    // but the additional wierdness that class-based-virtual calls (but not interface calls nor calls
    // on proxies) are resolved to their target.  Because of this, many clients of "Call" (see above)
    // end up doing some resolution for interface calls and/or proxies themselves.
    PCODE GetCallTarget(OBJECTREF* pThisObj, TypeHandle ownerType = TypeHandle());

    MethodImpl *GetMethodImpl();


#if defined(FEATURE_PREJIT ) && !defined(DACCESS_COMPILE)
    //================================================================
    // Precompilation (NGEN)

    void Save(DataImage *image);
    void Fixup(DataImage *image);
    void FixupSlot(DataImage *image, PVOID p, SSIZE_T offset, ZapRelocationType type = IMAGE_REL_BASED_PTR);

    //
    // Helper class used to regroup MethodDesc chunks before saving them into NGen image.
    // The regrouping takes into account IBC data and optional NGen-specific MethodDesc members.
    //
    class SaveChunk
    {
        DataImage * m_pImage;

        ZapStoredStructure * m_pFirstNode;
        MethodDescChunk * m_pLastChunk;

        typedef enum _MethodPriorityEnum
        {
            NoFlags = -1,	
            HotMethodDesc = 0x0,
            WriteableMethodDesc = 0x1,
            ColdMethodDesc = 0x2,
            ColdWriteableMethodDesc=  ColdMethodDesc | WriteableMethodDesc

        } MethodPriorityEnum;

        struct MethodInfo
        {
            MethodDesc * m_pMD;
            //MethodPriorityEnum
            BYTE m_priority;

            BOOL m_fHasPrecode:1;
            BOOL m_fHasNativeCodeSlot:1;
            BOOL m_fHasFixupList:1;
        };

        InlineSArray<MethodInfo, 20> m_methodInfos;

        static int __cdecl MethodInfoCmp(const void* a_, const void* b_);

        SIZE_T GetSavedMethodDescSize(MethodInfo * pMethodInfo);

        void SaveOneChunk(COUNT_T start, COUNT_T count, ULONG size, DWORD priority);

    public:
        SaveChunk(DataImage * image)
            : m_pImage(image), m_pFirstNode(NULL), m_pLastChunk(NULL)
        {
            LIMITED_METHOD_CONTRACT;
        }

        void Append(MethodDesc * pMD);

        ZapStoredStructure * Save();
    };

    bool CanSkipDoPrestub(MethodDesc * callerMD, 
                          CorInfoIndirectCallReason *pReason,
                          CORINFO_ACCESS_FLAGS  accessFlags = CORINFO_ACCESS_ANY);

    // This is different from !IsRestored() in that it checks if restoring
    // will ever be needed for this ngened data-structure.
    // This is to be used at ngen time of a dependent module to determine
    // if it can be accessed directly, or if the restoring mechanism needs
    // to be hooked in.
    BOOL NeedsRestore(DataImage *image, BOOL fAssumeMethodTableRestored = FALSE)
    {
        WRAPPER_NO_CONTRACT;
        return ComputeNeedsRestore(image, NULL, fAssumeMethodTableRestored);
    }

    BOOL ComputeNeedsRestore(DataImage *image, TypeHandleList *pVisited, BOOL fAssumeMethodTableRestored = FALSE);

    //
    // After the zapper compiles all code in a module it may attempt
    // to populate entries in all dictionaries
    // associated with instantiations of generic methods.  This is an optional step - nothing will
    // go wrong at runtime except we may get more one-off calls to JIT_GenericHandle.
    // Although these are one-off we prefer to avoid them since they touch metadata
    // pages.
    //
    // Fully populating a dictionary may in theory load more types, methods etc. However
    // for the moment only those entries that refer to things that
    // are already loaded will be filled in.
    void PrepopulateDictionary(DataImage * image, BOOL nonExpansive);

#endif // FEATURE_PREJIT && !DACCESS_COMPILE

    TADDR GetFixupList();

    BOOL IsRestored_NoLogging();
    BOOL IsRestored();
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

    PCODE DoPrestub(MethodTable *pDispatchingMT);

    PCODE MakeJitWorker(COR_ILMETHOD_DECODER* ILHeader, DWORD  flags, DWORD flags2);

    VOID GetMethodInfo(SString &namespaceOrClassName, SString &methodName, SString &methodSignature);
    VOID GetMethodInfoWithNewSig(SString &namespaceOrClassName, SString &methodName, SString &methodSignature);
    VOID GetMethodInfoNoSig(SString &namespaceOrClassName, SString &methodName);
    VOID GetFullMethodInfo(SString& fullMethodSigName);

    BOOL IsCritical()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(HasCriticalTransparentInfo());
        return (m_bFlags2 & enum_flag2_Transparency_Mask) != enum_flag2_Transparency_Transparent;
    }

    BOOL IsTreatAsSafe()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(HasCriticalTransparentInfo());
        return (m_bFlags2 & enum_flag2_Transparency_Mask) == enum_flag2_Transparency_TreatAsSafe;
    }

    BOOL IsTransparent()
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(HasCriticalTransparentInfo());
        return !IsCritical();
    }

    BOOL HasCriticalTransparentInfo()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_bFlags2 & enum_flag2_Transparency_Mask) != enum_flag2_Transparency_Unknown;
    }

    void SetCriticalTransparentInfo(BOOL fIsCritical, BOOL fIsTreatAsSafe)
    {
        WRAPPER_NO_CONTRACT;

        // TreatAsSafe has to imply critical
        _ASSERTE(fIsCritical || !fIsTreatAsSafe);

        EnsureWritablePages(this);
        InterlockedUpdateFlags2(
            static_cast<BYTE>(fIsTreatAsSafe ? enum_flag2_Transparency_TreatAsSafe :
                fIsCritical ? enum_flag2_Transparency_Critical :
                    enum_flag2_Transparency_Transparent),
            TRUE);

        _ASSERTE(HasCriticalTransparentInfo());
    }

    BOOL RequiresLinkTimeCheckHostProtectionOnly()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_bFlags2 & enum_flag2_HostProtectionLinkCheckOnly) != 0;
    }

    void SetRequiresLinkTimeCheckHostProtectionOnly()
    {
        LIMITED_METHOD_CONTRACT;
        m_bFlags2 |= enum_flag2_HostProtectionLinkCheckOnly;
    }

    BOOL HasTypeEquivalentStructParameters()
#ifndef FEATURE_TYPEEQUIVALENCE
    {
        LIMITED_METHOD_CONTRACT;
        return FALSE;
    }
#else
        ;
#endif
#ifdef BINDER
    typedef void (*WalkValueTypeParameterFnPtr)(MdilModule *pModule, mdToken token, const SigParser *ptr, SigTypeContext *pTypeContext, void *pData);
#else
    typedef void (*WalkValueTypeParameterFnPtr)(Module *pModule, mdToken token, Module *pDefModule, mdToken tkDefToken, const SigParser *ptr, SigTypeContext *pTypeContext, void *pData);
#endif

    void WalkValueTypeParameters(MethodTable *pMT, WalkValueTypeParameterFnPtr function, void *pData);

    void PrepareForUseAsADependencyOfANativeImage()
    {
        WRAPPER_NO_CONTRACT;
        if (!IsZapped() && !HaveValueTypeParametersBeenWalked())
            PrepareForUseAsADependencyOfANativeImageWorker();
    }

    void PrepareForUseAsADependencyOfANativeImageWorker();

    //================================================================
    // The actual data stored in a MethodDesc follows.

protected:
    enum {
        // There are flags available for use here (currently 5 flags bits are available); however, new bits are hard to come by, so any new flags bits should
        // have a fairly strong justification for existence.
        enum_flag3_TokenRemainderMask                       = 0x3FFF, // This must equal METHOD_TOKEN_REMAINDER_MASK calculated higher in this file
                                                                      // These are seperate to allow the flags space available and used to be obvious here
                                                                      // and for the logic that splits the token to be algorithmically generated based on the 
                                                                      // #define
        enum_flag3_HasForwardedValuetypeParameter           = 0x4000, // Indicates that a type-forwarded type is used as a valuetype parameter (this flag is only valid for ngenned items)
        enum_flag3_ValueTypeParametersWalked                = 0x4000, // Indicates that all typeref's in the signature of the method have been resolved to typedefs (or that process failed) (this flag is only valid for non-ngenned methods)
        enum_flag3_DoesNotHaveEquivalentValuetypeParameters = 0x8000, // Indicates that we have verified that there are no equivalent valuetype parameters for this method
    };
    UINT16      m_wFlags3AndTokenRemainder;
    
    BYTE        m_chunkIndex;

    enum {
        // enum_flag2_HasPrecode implies that enum_flag2_HasStableEntryPoint is set.
        enum_flag2_HasStableEntryPoint      = 0x01,   // The method entrypoint is stable (either precode or actual code)
        enum_flag2_HasPrecode               = 0x02,   // Precode has been allocated for this method

        enum_flag2_IsUnboxingStub           = 0x04,
        enum_flag2_HasNativeCodeSlot        = 0x08,   // Has slot for native code

        enum_flag2_Transparency_Mask        = 0x30,
        enum_flag2_Transparency_Unknown     = 0x00,   // The transparency has not been computed yet
        enum_flag2_Transparency_Transparent = 0x10,   // Method is transparent
        enum_flag2_Transparency_Critical    = 0x20,   // Method is critical
        enum_flag2_Transparency_TreatAsSafe = 0x30,   // Method is treat as safe. Also implied critical.

        // CAS Demands: Demands for Permissions that are CAS Permissions. CAS Perms are those 
        // that derive from CodeAccessPermission and need a stackwalk to evaluate demands
        // Non-CAS perms are those that don't need a stackwalk and don't derive from CodeAccessPermission. The implementor 
        // specifies the behavior on a demand. Examples: CAS: FileIOPermission. Non-CAS: PrincipalPermission.
        // This bit gets set if the demands are BCL CAS demands only. Even if there are non-BCL CAS demands, we don't set this
        // bit.
        enum_flag2_CASDemandsOnly           = 0x40,

        enum_flag2_HostProtectionLinkCheckOnly = 0x80, // Method has LinkTime check due to HP only.
    };
    BYTE        m_bFlags2;

    // The slot number of this MethodDesc in the vtable array.
    // Note that we may store other information in the high bits if available -- 
    // see enum_packedSlotLayout and mdcRequiresFullSlotNumber for details.
    WORD m_wSlotNumber;

    enum {
        enum_packedSlotLayout_SlotMask      = 0x03FF,
        enum_packedSlotLayout_NameHashMask  = 0xFC00
    };

    WORD m_wFlags;



public:
#ifdef DACCESS_COMPILE 
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif

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
        return (m_bFlags2 & enum_flag2_HasNativeCodeSlot) != 0;
    }

    inline void SetHasNativeCodeSlot()
    {
        LIMITED_METHOD_CONTRACT;
        m_bFlags2 |= enum_flag2_HasNativeCodeSlot;
    }

    static const SIZE_T s_ClassificationSizeTable[];

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

#ifdef FEATURE_COMINTEROP
    inline BOOL DoesNotHaveEquivalentValuetypeParameters()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return (m_wFlags3AndTokenRemainder & enum_flag3_DoesNotHaveEquivalentValuetypeParameters) != 0;
    }

    inline void SetDoesNotHaveEquivalentValuetypeParameters()
    {
        LIMITED_METHOD_CONTRACT;
        InterlockedUpdateFlags3(enum_flag3_DoesNotHaveEquivalentValuetypeParameters, TRUE);
    }
#endif //FEATURE_COMINTEROP

    inline BOOL HasForwardedValuetypeParameter()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        // This should only be asked of Zapped MethodDescs
        _ASSERTE(IsZapped());
        return (m_wFlags3AndTokenRemainder & enum_flag3_HasForwardedValuetypeParameter) != 0;
    }

    inline void SetHasForwardedValuetypeParameter()
    {
        LIMITED_METHOD_CONTRACT;
        InterlockedUpdateFlags3(enum_flag3_HasForwardedValuetypeParameter, TRUE);
    }

    inline BOOL HaveValueTypeParametersBeenWalked()
    {
        LIMITED_METHOD_DAC_CONTRACT;
#ifndef CLR_STANDALONE_BINDER
        // This should only be asked of non-Zapped MethodDescs, and only during execution (not compilation)
        _ASSERTE(!IsZapped() && !IsCompilationProcess());
#endif
        return (m_wFlags3AndTokenRemainder & enum_flag3_ValueTypeParametersWalked) != 0;
    }

    inline void SetValueTypeParametersWalked()
    {
        LIMITED_METHOD_CONTRACT;
#ifndef CLR_STANDALONE_BINDER
        _ASSERTE(!IsZapped() && !IsCompilationProcess());
#endif
        InterlockedUpdateFlags3(enum_flag3_ValueTypeParametersWalked, TRUE);
    }

    //
    // Optional MethodDesc slots appear after the end of base MethodDesc in this order:
    //

    // class MethodImpl;                            // Present if HasMethodImplSlot() is true

    typedef RelativePointer<PCODE> NonVtableSlot;   // Present if HasNonVtableSlot() is true 
                                                    // RelativePointer for NGen, PCODE for JIT

#define FIXUP_LIST_MASK 1
    typedef RelativePointer<TADDR> NativeCodeSlot;  // Present if HasNativeCodeSlot() is true
                                                    // lower order bit (FIXUP_LIST_MASK) used to determine if FixupListSlot is present
    typedef RelativePointer<TADDR> FixupListSlot;

// Stub Dispatch code
public:
    MethodDesc *GetInterfaceMD();

// StubMethodInfo for use in creating RuntimeMethodHandles
    REFLECTMETHODREF GetStubMethodInfo();
    
    PrecodeType GetPrecodeType();
};

/******************************************************************/

// A code:MethodDescChunk is a container that holds one or more code:MethodDesc.  Logically it is just
// compression.  Basically fields that are common among methods descs in the chunk are stored in the chunk
// and the MethodDescs themselves just store and index that allows them to find their Chunk.  Semantically
// a code:MethodDescChunk is just a set of code:MethodDesc.  
class MethodDescChunk
{
    friend class MethodDesc;
    friend class CheckAsmOffsets;
#ifdef BINDER
    friend class MdilModule;
#endif
#if defined(FEATURE_PREJIT) && !defined(DACCESS_COMPILE)
    friend class MethodDesc::SaveChunk;
#endif
#ifdef DACCESS_COMPILE 
    friend class NativeImageDumper;
#endif // DACCESS_COMPILE

    enum {
        enum_flag_TokenRangeMask                           = 0x03FF, // This must equal METHOD_TOKEN_RANGE_MASK calculated higher in this file
                                                                     // These are seperate to allow the flags space available and used to be obvious here
                                                                     // and for the logic that splits the token to be algorithmically generated based on the 
                                                                     // #define
        enum_flag_HasCompactEntrypoints                    = 0x4000, // Compact temporary entry points
        enum_flag_IsZapped                                 = 0x8000, // This chunk lives in NGen module
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
                                        BOOL fComPlusCallInfo,
                                        MethodTable *initialMT,
                                        class AllocMemTracker *pamTracker);

    BOOL HasTemporaryEntryPoints()
    {
        LIMITED_METHOD_CONTRACT;
        return !IsZapped();
    }

    TADDR GetTemporaryEntryPoints()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(HasTemporaryEntryPoints());
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
    //

    TADDR AllocateCompactEntryPoints(LoaderAllocator *pLoaderAllocator, AllocMemTracker *pamTracker);

    static MethodDesc* GetMethodDescFromCompactEntryPoint(PCODE addr, BOOL fSpeculative = FALSE);
    static SIZE_T SizeOfCompactEntryPoints(int count);

    static BOOL IsCompactEntryPointAtAddress(PCODE addr)
    {
#if defined(_TARGET_X86_) || defined(_TARGET_AMD64_)
        // Compact entrypoints start at odd addresses
        LIMITED_METHOD_DAC_CONTRACT;
        return (addr & 1) != 0;
#else
        #error Unsupported platform
#endif
    }
#endif // HAS_COMPACT_ENTRYPOINTS

    FORCEINLINE PTR_MethodTable GetMethodTable()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_methodTable.GetValue(PTR_HOST_MEMBER_TADDR(MethodDescChunk, this, m_methodTable));
    }

    inline DPTR(RelativeFixupPointer<PTR_MethodTable>) GetMethodTablePtr() const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return dac_cast<DPTR(RelativeFixupPointer<PTR_MethodTable>)>(PTR_HOST_MEMBER_TADDR(MethodDescChunk, this, m_methodTable));
    }

#ifndef DACCESS_COMPILE 
    inline void SetMethodTable(MethodTable * pMT)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(m_methodTable.IsNull());
        _ASSERTE(pMT != NULL);
        m_methodTable.SetValue(PTR_HOST_MEMBER_TADDR(MethodDescChunk, this, m_methodTable), pMT);
    }

    inline void SetSizeAndCount(ULONG sizeOfMethodDescs, COUNT_T methodDescCount)
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(FitsIn<BYTE>((sizeOfMethodDescs / MethodDesc::ALIGNMENT) - 1));
        m_size = static_cast<BYTE>((sizeOfMethodDescs / MethodDesc::ALIGNMENT) - 1);
        _ASSERTE(SizeOf() == sizeof(MethodDescChunk) + sizeOfMethodDescs);
    
        _ASSERTE(FitsIn<BYTE>(methodDescCount - 1));
        m_count = static_cast<BYTE>(methodDescCount - 1);
        _ASSERTE(GetCount() == methodDescCount);
    }
#endif // !DACCESS_COMPILE

#ifndef BINDER
#ifdef FEATURE_PREJIT 
#ifndef DACCESS_COMPILE 
    inline void RestoreMTPointer(ClassLoadLevel level = CLASS_LOADED)
    {
        LIMITED_METHOD_CONTRACT;
        Module::RestoreMethodTablePointer(&m_methodTable, NULL, level);
    }
#endif // !DACCESS_COMPILE
#endif // FEATURE_PREJIT
#endif // !BINDER

#ifndef DACCESS_COMPILE 
    void SetNextChunk(MethodDescChunk *chunk)
    {
        LIMITED_METHOD_CONTRACT;
        m_next.SetValueMaybeNull(chunk);
    }
#endif // !DACCESS_COMPILE

    PTR_MethodDescChunk GetNextChunk()
    {
        LIMITED_METHOD_CONTRACT;
        return m_next.GetValueMaybeNull(PTR_HOST_MEMBER_TADDR(MethodDescChunk, this, m_next));
    }

    UINT32 GetCount()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_count + 1;
    }

    BOOL IsZapped()
    {
        LIMITED_METHOD_DAC_CONTRACT;
#ifdef FEATURE_PREJIT
        return (m_flagsAndTokenRange & enum_flag_IsZapped) != 0;
#else
        return FALSE;
#endif
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
    void SetIsZapped()
    {
        LIMITED_METHOD_CONTRACT;
        m_flagsAndTokenRange |= enum_flag_IsZapped;
    }

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

    RelativeFixupPointer<PTR_MethodTable> m_methodTable;

    RelativePointer<PTR_MethodDescChunk> m_next;

    BYTE                 m_size;        // The size of this chunk minus 1 (in multiples of MethodDesc::ALIGNMENT)
    BYTE                 m_count;       // The number of MethodDescs in this chunk minus 1
    UINT16               m_flagsAndTokenRange;

    // Followed by array of method descs...
};

inline int MethodDesc::GetMethodDescIndex() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    return m_chunkIndex;
}

inline MethodDescChunk *MethodDesc::GetMethodDescChunk() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    return
        PTR_MethodDescChunk(dac_cast<TADDR>(this) -
                            (sizeof(MethodDescChunk) + (GetMethodDescIndex() * MethodDesc::ALIGNMENT)));
}

// convert an entry point into a MethodDesc
MethodDesc* Entry2MethodDesc(PCODE entryPoint, MethodTable *pMT);


typedef DPTR(class StoredSigMethodDesc) PTR_StoredSigMethodDesc;
class StoredSigMethodDesc : public MethodDesc
{
  public:
    // Put the sig RVA in here - this allows us to avoid
    // touching the method desc table when mscorlib is prejitted.

    TADDR           m_pSig;
    DWORD           m_cSig;
#ifdef _WIN64 
    // m_dwExtendedFlags is not used by StoredSigMethodDesc itself.
    // It is used by child classes. We allocate the space here to get
    // optimal layout.
    DWORD           m_dwExtendedFlags;
#endif

    bool HasStoredMethodSig(void)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_pSig != 0;
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
            DacInstantiateTypeByAddress(m_pSig, m_cSig, true);
#else // !DACCESS_COMPILE
#ifndef BINDER
        g_IBCLogger.LogNDirectCodeAccess(this);
#endif
        return (PCCOR_SIGNATURE)m_pSig;
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
#ifdef DACCESS_COMPILE
    friend class NativeImageDumper;
#endif

    DWORD   m_dwECallID;
#ifdef _WIN64 
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
#ifdef DACCESS_COMPILE
    friend class NativeImageDumper;
#endif
#ifdef MDIL
    friend class CompactTypeBuilder;
    friend class MdilModule;
#endif

protected:
    PTR_CUTF8           m_pszMethodName;
    PTR_DynamicResolver m_pResolver;

#ifndef _WIN64
    // We use m_dwExtendedFlags from StoredSigMethodDesc on WIN64
    DWORD               m_dwExtendedFlags;   // see DynamicMethodDesc::ExtendedFlags enum
#endif

    typedef enum ExtendedFlags
    {
        nomdAttrs           = 0x0000FFFF, // method attributes (LCG)
        nomdILStubAttrs     = mdMemberAccessMask | mdStatic, //  method attributes (IL stubs)

        // attributes (except mdStatic and mdMemberAccessMask) have different meaning for IL stubs
        // mdMemberAccessMask     = 0x0007,
        nomdReverseStub           = 0x0008,
        // mdStatic               = 0x0010,
        nomdCALLIStub             = 0x0020,
        nomdDelegateStub          = 0x0040,
        nomdCopyCtorArgs          = 0x0080,
        nomdUnbreakable           = 0x0100,
        nomdDelegateCOMStub       = 0x0200,  // CLR->COM or COM->CLR call via a delegate (WinRT specific)
        nomdSignatureNeedsRestore = 0x0400,
        nomdStubNeedsCOMStarted   = 0x0800,  // EnsureComStarted must be called before executing the method
        nomdMulticastStub         = 0x1000,
        nomdUnboxingILStub        = 0x2000,

        nomdILStub          = 0x00010000,
        nomdLCGMethod       = 0x00020000,
        nomdStackArgSize    = 0xFFFC0000, // native stack arg size for IL stubs
    } ExtendedFlags;

public:
    bool IsILStub() { LIMITED_METHOD_DAC_CONTRACT; return !!(m_dwExtendedFlags & nomdILStub); }
    bool IsLCGMethod() { LIMITED_METHOD_DAC_CONTRACT; return !!(m_dwExtendedFlags & nomdLCGMethod); }

	inline PTR_DynamicResolver    GetResolver();
    inline PTR_LCGMethodResolver  GetLCGMethodResolver();
    inline PTR_ILStubResolver     GetILStubResolver();

    PTR_CUTF8 GetMethodName() { LIMITED_METHOD_DAC_CONTRACT; return m_pszMethodName; }

    WORD GetAttrs()
    {
        LIMITED_METHOD_CONTRACT;
        return (IsILStub() ? (m_dwExtendedFlags & nomdILStubAttrs) : (m_dwExtendedFlags & nomdAttrs));
    }

    DWORD GetExtendedFlags()
    {
        LIMITED_METHOD_CONTRACT;
        return m_dwExtendedFlags;
    }

    WORD GetNativeStackArgSize()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        _ASSERTE(IsILStub());
        return (WORD)((m_dwExtendedFlags & nomdStackArgSize) >> 16);
    }

    void SetNativeStackArgSize(WORD cbArgSize)
    {
        LIMITED_METHOD_CONTRACT; 
        _ASSERTE(IsILStub() && (cbArgSize % sizeof(SLOT)) == 0);
        m_dwExtendedFlags = (m_dwExtendedFlags & ~nomdStackArgSize) | ((DWORD)cbArgSize << 16);
    }

    void SetHasCopyCtorArgs(bool value)
    {
        LIMITED_METHOD_CONTRACT;
        if (value)
        {
            m_dwExtendedFlags |= nomdCopyCtorArgs;
        }
    }

    void SetUnbreakable(bool value)
    {
        LIMITED_METHOD_CONTRACT;
        if (value)
        {
            m_dwExtendedFlags |= nomdUnbreakable;
        }
    }

    void SetSignatureNeedsRestore(bool value)
    {
        LIMITED_METHOD_CONTRACT;
        if (value)
        {
            m_dwExtendedFlags |= nomdSignatureNeedsRestore;
        }
    }

    void SetStubNeedsCOMStarted(bool value)
    {
        LIMITED_METHOD_CONTRACT;
        if (value)
        {
            m_dwExtendedFlags |= nomdStubNeedsCOMStarted;
        }
    }

    bool IsRestored()
    {
        LIMITED_METHOD_CONTRACT;

        if (IsSignatureNeedsRestore())
        {
            // Since we don't update the signatreNeedsRestore bit when we actually
            // restore the signature, the bit will have a stall value.  The signature
            // bit in the metadata will always contain the correct, up-to-date
            // information. 
            Volatile<BYTE> *pVolatileSig = (Volatile<BYTE> *)GetStoredMethodSig();
            if ((*pVolatileSig & IMAGE_CEE_CS_CALLCONV_NEEDSRESTORE) != 0)
                return false;
        }            

        return true;
    }

    bool IsReverseStub()     { LIMITED_METHOD_DAC_CONTRACT; _ASSERTE(IsILStub()); return (0 != (m_dwExtendedFlags & nomdReverseStub));  }
    bool IsCALLIStub()       { LIMITED_METHOD_DAC_CONTRACT; _ASSERTE(IsILStub()); return (0 != (m_dwExtendedFlags & nomdCALLIStub));    }
    bool IsDelegateStub()    { LIMITED_METHOD_DAC_CONTRACT; _ASSERTE(IsILStub()); return (0 != (m_dwExtendedFlags & nomdDelegateStub)); }
    bool IsCLRToCOMStub()    { LIMITED_METHOD_CONTRACT; _ASSERTE(IsILStub()); return ((0 == (m_dwExtendedFlags & mdStatic)) && !IsReverseStub() && !IsDelegateStub()); }
    bool IsCOMToCLRStub()    { LIMITED_METHOD_CONTRACT; _ASSERTE(IsILStub()); return ((0 == (m_dwExtendedFlags & mdStatic)) &&  IsReverseStub()); }
    bool IsPInvokeStub()     { LIMITED_METHOD_CONTRACT; _ASSERTE(IsILStub()); return ((0 != (m_dwExtendedFlags & mdStatic)) && !IsReverseStub() && !IsCALLIStub()); }
    bool HasCopyCtorArgs()   { LIMITED_METHOD_CONTRACT; _ASSERTE(IsILStub()); return (0 != (m_dwExtendedFlags & nomdCopyCtorArgs));  }
    bool IsUnbreakable()     { LIMITED_METHOD_CONTRACT; _ASSERTE(IsILStub()); return (0 != (m_dwExtendedFlags & nomdUnbreakable));  }
    bool IsDelegateCOMStub() { LIMITED_METHOD_CONTRACT; _ASSERTE(IsILStub()); return (0 != (m_dwExtendedFlags & nomdDelegateCOMStub));  }
    bool IsSignatureNeedsRestore() { LIMITED_METHOD_CONTRACT; _ASSERTE(IsILStub()); return (0 != (m_dwExtendedFlags & nomdSignatureNeedsRestore)); }
    bool IsStubNeedsCOMStarted()   { LIMITED_METHOD_CONTRACT; _ASSERTE(IsILStub()); return (0 != (m_dwExtendedFlags & nomdStubNeedsCOMStarted)); }
#ifdef FEATURE_STUBS_AS_IL
    bool IsMulticastStub() { 
        LIMITED_METHOD_DAC_CONTRACT; 
        _ASSERTE(IsILStub());
        return !!(m_dwExtendedFlags & nomdMulticastStub);
    }
    bool IsUnboxingILStub() { 
        LIMITED_METHOD_DAC_CONTRACT; 
        _ASSERTE(IsILStub());
        return !!(m_dwExtendedFlags & nomdUnboxingILStub);
    }
#endif

    // Whether the stub takes a context argument that is an interop MethodDesc.
    bool HasMDContextArg()
    {
        LIMITED_METHOD_CONTRACT;
        return ((IsCLRToCOMStub() && !IsDelegateCOMStub()) || IsPInvokeStub());
    }

    void Restore();
    void Fixup(DataImage* image);
    //
    // following implementations defined in DynamicMethod.cpp
    //
    void Destroy(BOOL fDomainUnload = FALSE);
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

        // The ru
        DWORD dwSlot = GetSlot();
        DWORD dwVirtuals = GetMethodTable()->GetNumVirtuals();
        _ASSERTE(dwSlot >= dwVirtuals);
        return dwSlot - dwVirtuals;
    }

    LPCUTF8 GetMethodName();
    DWORD GetAttrs();
    CorInfoIntrinsics GetIntrinsicID();
};

#ifdef HAS_NDIRECT_IMPORT_PRECODE
typedef NDirectImportPrecode NDirectImportThunkGlue;
#else // HAS_NDIRECT_IMPORT_PRECODE

class NDirectImportThunkGlue
{
    PVOID m_dummy; // Dummy field to make the alignment right

public:
    LPVOID GetEntrypoint()
    {
        LIMITED_METHOD_CONTRACT;
        return NULL;
    }
    void Init(MethodDesc *pMethod)
    {        
        LIMITED_METHOD_CONTRACT;
    }
};
#ifdef FEATURE_PREJIT
PORTABILITY_WARNING("NDirectImportThunkGlue");
#endif // FEATURE_PREJIT

#endif // HAS_NDIRECT_IMPORT_PRECODE

typedef DPTR(NDirectImportThunkGlue)      PTR_NDirectImportThunkGlue;


//
// This struct consolidates the writeable parts of the NDirectMethodDesc
// so that we can eventually layout a read-only NDirectMethodDesc with a pointer
// to the writeable parts in an ngen image
//
class NDirectWriteableData
{
public:
    // The JIT generates an indirect call through this location in some cases.
    // Initialized to NDirectImportThunkGlue. Patched to the true target or 
    // host interceptor stub or alignment thunk after linking.
    LPVOID      m_pNDirectTarget;
};

typedef DPTR(NDirectWriteableData)      PTR_NDirectWriteableData;

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
        // If we are hosted, stack imbalance MDA is active, or alignment thunks are needed,
        // we will intercept m_pNDirectTarget. The true target is saved here.
        LPVOID      m_pNativeNDirectTarget;
            
        // Information about the entrypoint
        LPCUTF8     m_pszEntrypointName;

        union
        {
            LPCUTF8     m_pszLibName;
            DWORD       m_dwECallID;    // ECallID for QCalls
        };

        // The writeable part of the methoddesc.
        PTR_NDirectWriteableData    m_pWriteableData;

#ifdef HAS_NDIRECT_IMPORT_PRECODE
        PTR_NDirectImportThunkGlue  m_pImportThunkGlue;        
#else // HAS_NDIRECT_IMPORT_PRECODE
        NDirectImportThunkGlue      m_ImportThunkGlue;
#endif // HAS_NDIRECT_IMPORT_PRECODE

#ifndef FEATURE_CORECLR
        ULONG       m_DefaultDllImportSearchPathsAttributeValue; // DefaultDllImportSearchPathsAttribute is saved.
#endif

        // Various attributes needed at runtime.
        WORD        m_wFlags;

#if defined(_TARGET_X86_)
        // Size of outgoing arguments (on stack). Note that in order to get the @n stdcall name decoration,
        // it may be necessary to subtract 4 as the hidden large structure pointer parameter does not count.
        // See code:kStdCallWithRetBuf
        WORD        m_cbStackArgumentSize;
#endif // defined(_TARGET_X86_)

        // This field gets set only when this MethodDesc is marked as PreImplemented
        RelativePointer<PTR_MethodDesc> m_pStubMD;

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

        kHasSuppressUnmanagedCodeAccess = 0x0002,

#ifndef FEATURE_CORECLR
        kDefaultDllImportSearchPathsIsCached = 0x0004, // set if we cache attribute value.
#endif

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

#if !defined(FEATURE_CORECLR)
        kDefaultDllImportSearchPathsStatus = 0x2000, // either method has custom attribute or not.
#endif

        kHasCopyCtorArgs                = 0x4000,

        kStdCallWithRetBuf              = 0x8000,   // Call returns large structure, only valid if kStdCall is also set

    };

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

#ifdef FEATURE_MIXEDMODE // IJW
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
#endif // FEATURE_MIXEDMODE

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

#ifndef FEATURE_CORECLR
    BOOL HasSuppressUnmanagedCodeAccessAttr() const
    {
        LIMITED_METHOD_CONTRACT;

        return (ndirect.m_wFlags & kHasSuppressUnmanagedCodeAccess) != 0;
    }

    void SetSuppressUnmanagedCodeAccessAttr(BOOL value)
    {
        LIMITED_METHOD_CONTRACT;

        if (value)
            ndirect.m_wFlags |= kHasSuppressUnmanagedCodeAccess;
    }
#endif

    DWORD GetECallID() const
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(IsQCall());
        return ndirect.m_dwECallID;
    }

    void SetECallID(DWORD dwID)
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(IsQCall());
        ndirect.m_dwECallID = dwID;
    }

    LPCUTF8 GetLibName() const
    {
        LIMITED_METHOD_CONTRACT;

        return IsQCall() ? "QCall" : ndirect.m_pszLibName;
    }

    LPCUTF8 GetEntrypointName() const
    {
        LIMITED_METHOD_CONTRACT;

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

    // Returns TRUE if this MethodDesc is internal call from mscorlib to mscorwks
    BOOL IsQCall() const
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return (ndirect.m_wFlags & kIsQCall) != 0;
    }

#ifndef FEATURE_CORECLR
    BOOL HasDefaultDllImportSearchPathsAttribute();

    BOOL IsDefaultDllImportSearchPathsAttributeCached()
    {
        LIMITED_METHOD_CONTRACT;
        return (ndirect.m_wFlags  & kDefaultDllImportSearchPathsIsCached) != 0;
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
#endif // !FEATURE_CORECLR

    BOOL HasCopyCtorArgs() const
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return (ndirect.m_wFlags & kHasCopyCtorArgs) != 0;
    }

    void SetHasCopyCtorArgs(BOOL value)
    {
        WRAPPER_NO_CONTRACT;

        if (value)
        {
            InterlockedSetNDirectFlags(kHasCopyCtorArgs);
        }
    }

    BOOL IsStdCallWithRetBuf() const
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return (ndirect.m_wFlags & kStdCallWithRetBuf) != 0;
    }

    NDirectWriteableData* GetWriteableData() const
    {
        LIMITED_METHOD_CONTRACT;

        return ndirect.m_pWriteableData;
    }

    NDirectImportThunkGlue* GetNDirectImportThunkGlue()
    {
        LIMITED_METHOD_CONTRACT;

#ifdef HAS_NDIRECT_IMPORT_PRECODE
        return ndirect.m_pImportThunkGlue;
#else
        return &ndirect.m_ImportThunkGlue;
#endif
    }

    LPVOID GetNDirectTarget()
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(IsNDirect());
        return GetWriteableData()->m_pNDirectTarget;
    }

    LPVOID GetNativeNDirectTarget()
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(IsNDirect());
        _ASSERTE_IMPL(!NDirectTargetIsImportThunk());

        LPVOID pNativeNDirectTarget = ndirect.m_pNativeNDirectTarget;
        if (pNativeNDirectTarget != NULL)
            return pNativeNDirectTarget;

        return GetNDirectTarget();
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
    LPVOID FindEntryPoint(HINSTANCE hMod) const;

private:
    Stub* GenerateStubForHost(LPVOID pNativeTarget, Stub *pInnerStub);
#ifdef MDA_SUPPORTED    
    Stub* GenerateStubForMDA(LPVOID pNativeTarget, Stub *pInnerStub, BOOL fCalledByStub);
#endif // MDA_SUPPORTED

public:

    void SetStackArgumentSize(WORD cbDstBuffer, CorPinvokeMap unmgdCallConv)
    {
        LIMITED_METHOD_CONTRACT;

#if defined(_TARGET_X86_)
        // thiscall passes the this pointer in ECX
        if (unmgdCallConv == pmCallConvThiscall)
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
#endif // defined(_TARGET_X86_)
    }

#if defined(_TARGET_X86_)
    WORD GetStackArgumentSize() const
    {
        LIMITED_METHOD_DAC_CONTRACT;

        _ASSERTE(ndirect.m_cbStackArgumentSize != 0xFFFF);

        // If we have a methoddesc, stackArgSize is the number of bytes of
        // the outgoing marshalling buffer.
        return ndirect.m_cbStackArgumentSize;
    }
#endif // defined(_TARGET_X86_)

#ifdef FEATURE_MIXEDMODE // IJW
    VOID InitEarlyBoundNDirectTarget();
#endif

    // In AppDomains, we can trigger declarer's cctor when we link the P/Invoke,
    // which takes care of inlined calls as well. See code:NDirect.NDirectLink.
    // Although the cctor is guaranteed to run in the shared domain before the
    // target is invoked (code:IsClassConstructorTriggeredByILStub), we will
    // trigger at it link time as well because linking may depend on it - the
    // cctor may change the target DLL, change DLL search path etc.
    BOOL IsClassConstructorTriggeredAtLinkTime()
#ifndef CLR_STANDALONE_BINDER
    {
        LIMITED_METHOD_CONTRACT;       
        MethodTable * pMT = GetMethodTable();
        // Try to avoid touching the EEClass if possible
        if (pMT->IsClassPreInited())
            return FALSE;      
        return !pMT->GetClass()->IsBeforeFieldInit();
    }
#else
    ;
#endif

#ifndef DACCESS_COMPILE
    // In the shared domain and in NGENed code, we will trigger declarer's cctor
    // in the marshaling stub by calling code:StubHelpers.InitDeclaringType. If
    // this returns TRUE, the call must not be inlined.
    BOOL IsClassConstructorTriggeredByILStub()
#ifndef CLR_STANDALONE_BINDER
    {
        WRAPPER_NO_CONTRACT;
        
        return (IsClassConstructorTriggeredAtLinkTime() &&
                (IsZapped() || GetDomain()->IsSharedDomain() || SystemDomain::GetCurrentDomain()->IsCompilationDomain()));
    }
#else
    ;
#endif
#endif //!DACCESS_COMPILE
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
// CLR->COM calls. It is currently used by code:ComPlusCallMethodDesc (ordinary CLR->COM calls),
// code:InstantiatedMethodDesc (optional field, CLR->COM calls on shared generic interfaces),
// and code:DelegateEEClass (delegate->COM calls for WinRT).
typedef DPTR(struct ComPlusCallInfo) PTR_ComPlusCallInfo;
struct ComPlusCallInfo
{
    // Returns ComPlusCallInfo associated with a method. pMD must be a ComPlusCallMethodDesc or
    // EEImplMethodDesc that has already been initialized for COM interop.
    inline static ComPlusCallInfo *FromMethodDesc(MethodDesc *pMD);

    enum Flags
    {
        kHasSuppressUnmanagedCodeAccess = 0x1,
        kRequiresArgumentWrapping       = 0x2,
        kHasCopyCtorArgs                = 0x4,
    };

#if defined(FEATURE_REMOTING) && !defined(HAS_REMOTING_PRECODE)
    // These two fields cannot overlap in this case because of AMD64 GenericComPlusCallStub uses m_pILStub on the COM event provider path
    struct
#else
    union
#endif
    {
        // IL stub for CLR to COM call
        PCODE m_pILStub; 

        // MethodDesc of the COM event provider to forward the call to (COM event interfaces)
        MethodDesc *m_pEventProviderMD;
    };

    // method table of the interface which this represents
    PTR_MethodTable m_pInterfaceMT;

    // We need only 3 bits here, see enum Flags below.
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

#ifdef _TARGET_X86_
    // Size of outgoing arguments (on stack). This is currently used only
    // on x86 when we have an InlinedCallFrame representing a CLR->COM call.
    WORD        m_cbStackArgumentSize;

    void SetHasCopyCtorArgs(BOOL value)
    {
        LIMITED_METHOD_CONTRACT;
        if (value)
            FastInterlockOr(reinterpret_cast<DWORD *>(&m_flags), kHasCopyCtorArgs);
    }

    BOOL HasCopyCtorArgs()
    {
        LIMITED_METHOD_CONTRACT;
        return ((m_flags & kHasCopyCtorArgs) != 0);
    }

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

    union
    {
        LPVOID      m_pRetThunk;         // used for late-bound calls
        LPVOID      m_pInterceptStub;    // used for early-bound IL stub calls
    };

    Stub *GenerateStubForHost(LoaderHeap *pHeap, Stub *pInnerStub);
#else // _TARGET_X86_
    void InitStackArgumentSize()
    {
        LIMITED_METHOD_CONTRACT;
    }

    void SetStackArgumentSize(WORD cbDstBuffer)
    {
        LIMITED_METHOD_CONTRACT;
    }
#endif // _TARGET_X86_

    // This field gets set only when this MethodDesc is marked as PreImplemented
    RelativePointer<PTR_MethodDesc> m_pStubMD;

#ifdef FEATURE_PREJIT
    BOOL ShouldSave(DataImage *image);
    void Fixup(DataImage *image);
#endif
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

#ifndef FEATURE_CORECLR

#ifndef BINDER
    BOOL HasSuppressUnmanagedCodeAccessAttr()
    {
        LIMITED_METHOD_CONTRACT;

        if (m_pComPlusCallInfo != NULL)
        {
            return (m_pComPlusCallInfo->m_flags & ComPlusCallInfo::kHasSuppressUnmanagedCodeAccess) != 0;
        }
        
        // it is possible that somebody will call this before we initialized m_pComPlusCallInfo
        return (GetMDImport()->GetCustomAttributeByName(GetMemberDef(),
                                                        COR_SUPPRESS_UNMANAGED_CODE_CHECK_ATTRIBUTE_ANSI,
                                                        NULL,
                                                        NULL) == S_OK);
    }
#endif // !BINDER

    void SetSuppressUnmanagedCodeAccessAttr(BOOL value)
    {
        LIMITED_METHOD_CONTRACT;

        if (value)
            FastInterlockOr(reinterpret_cast<DWORD *>(&m_pComPlusCallInfo->m_flags), ComPlusCallInfo::kHasSuppressUnmanagedCodeAccess);
    }
#endif // FEATURE_CORECLR

    BOOL RequiresArgumentWrapping()
    {
        LIMITED_METHOD_CONTRACT;

        return (m_pComPlusCallInfo->m_flags & ComPlusCallInfo::kRequiresArgumentWrapping) != 0;
    }

    void SetLateBoundFlags(BYTE newFlags)
    {
        LIMITED_METHOD_CONTRACT;

        FastInterlockOr(reinterpret_cast<DWORD *>(&m_pComPlusCallInfo->m_flags), newFlags);
    }

#ifdef _TARGET_X86_
    BOOL HasCopyCtorArgs()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pComPlusCallInfo->HasCopyCtorArgs();
    }

    void SetHasCopyCtorArgs(BOOL value)
    {
        LIMITED_METHOD_CONTRACT;
        m_pComPlusCallInfo->SetHasCopyCtorArgs(value);
    }

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
#else // _TARGET_X86_
    void SetStackArgumentSize(WORD cbDstBuffer)
    {
        LIMITED_METHOD_CONTRACT;
    }
#endif // _TARGET_X86_
};
#endif // FEATURE_COMINTEROP

//-----------------------------------------------------------------------
// InstantiatedMethodDesc's are used for generics and
// come in four flavours, discriminated by the
// low order bits of the first field:
//
//  00 --> GenericMethodDefinition
//  01 --> UnsharedMethodInstantiation
//  10 --> SharedMethodInstantiation
//  11 --> WrapperStubWithInstantiations - and unboxing or instantiating stub
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

class InstantiatedMethodDesc : public MethodDesc
{
#ifdef DACCESS_COMPILE
    friend class NativeImageDumper;
#endif

#ifdef BINDER
    friend class CompactTypeBuilder;
    friend class MdilModule;
#endif
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
#ifndef BINDER
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return Instantiation(m_pPerInstInfo->GetInstantiation(), m_wNumGenericArgs);
    }
#else
    ; // The binder requires a special implementation of this method as its methoddesc data structure holds the instantiation in a different way.
#endif

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

    BOOL IMD_IsEnCAddedMethod()
    {
        LIMITED_METHOD_CONTRACT;

#ifdef EnC_SUPPORTED
        return((m_wFlags2 & KindMask) == EnCAddedMethod);
#else
        return FALSE;
#endif
    }

#ifdef FEATURE_COMINTEROP
    BOOL IMD_HasComPlusCallInfo()
    {
        LIMITED_METHOD_CONTRACT;
        return ((m_wFlags2 & HasComPlusCallInfo) != 0);
    }

    void IMD_SetupGenericComPlusCall()
    {
        LIMITED_METHOD_CONTRACT;

        m_wFlags2 |= InstantiatedMethodDesc::HasComPlusCallInfo;

        IMD_GetComPlusCallInfo()->InitStackArgumentSize();
    }

    PTR_ComPlusCallInfo IMD_GetComPlusCallInfo()
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(IMD_HasComPlusCallInfo());
        SIZE_T size = s_ClassificationSizeTable[m_wFlags & (mdcClassification | mdcHasNonVtableSlot | mdcMethodImpl)];

        if (HasNativeCodeSlot())
        {
            size += (*dac_cast<PTR_TADDR>(dac_cast<TADDR>(this) + size) & FIXUP_LIST_MASK) ?
                (sizeof(NativeCodeSlot) + sizeof(FixupListSlot)) : sizeof(NativeCodeSlot);
        }

        return dac_cast<PTR_ComPlusCallInfo>(dac_cast<TADDR>(this) + size);
    }
#endif // FEATURE_COMINTEROP

    // Get the dictionary layout, if there is one
    DictionaryLayout* IMD_GetDictionaryLayout()
    {
        WRAPPER_NO_CONTRACT;
        if (IMD_IsWrapperStubWithInstantiations() && IMD_HasMethodInstantiation())
            return IMD_GetWrappedMethodDesc()->AsInstantiatedMethodDesc()->m_pDictLayout;
        else
        if (IMD_IsSharedByGenericMethodInstantiations())
            return m_pDictLayout;
        else
            return NULL;
    }

#ifdef BINDER
    void IMD_SetDictionaryLayout(DictionaryLayout *dictionaryLayout)
    {

        LIMITED_METHOD_CONTRACT;

        m_pDictLayout = dictionaryLayout;
    }
#endif

    MethodDesc* IMD_GetWrappedMethodDesc()
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(IMD_IsWrapperStubWithInstantiations());
        return m_pWrappedMethodDesc.GetValue();
    }



    // Setup the IMD as shared code
    void SetupSharedMethodInstantiation(DWORD numGenericArgs, TypeHandle *pPerInstInfo, DictionaryLayout *pDL);

    // Setup the IMD as unshared code
    void SetupUnsharedMethodInstantiation(DWORD numGenericArgs, TypeHandle *pInst);

    // Setup the IMD as the special MethodDesc for a "generic" method
    void SetupGenericMethodDefinition(IMDInternalImport *pIMDII, LoaderAllocator* pAllocator, AllocMemTracker *pamTracker,
        Module *pModule, mdMethodDef tok);

    // Setup the IMD as a wrapper around another method desc
    void SetupWrapperStubWithInstantiations(MethodDesc* wrappedMD,DWORD numGenericArgs, TypeHandle *pGenericMethodInst);
    

#ifdef EnC_SUPPORTED
    void SetupEnCAddedMethod()
    {
        LIMITED_METHOD_CONTRACT;
        m_wFlags2 = EnCAddedMethod;
    }
#endif

private:
    enum
    {
        KindMask                            = 0x07,
        GenericMethodDefinition             = 0x00,
        UnsharedMethodInstantiation         = 0x01,
        SharedMethodInstantiation           = 0x02,
        WrapperStubWithInstantiations       = 0x03,

#ifdef EnC_SUPPORTED
        // Non-virtual method added through EditAndContinue.
        EnCAddedMethod                      = 0x07,
#endif // EnC_SUPPORTED

        Unrestored                          = 0x08,

#ifdef FEATURE_COMINTEROP
        HasComPlusCallInfo                  = 0x10, // this IMD contains an optional ComPlusCallInfo
#endif // FEATURE_COMINTEROP
    };

    friend class MethodDesc; // this fields are currently accessed by MethodDesc::Save/Restore etc.
    union {
        DictionaryLayout * m_pDictLayout; //SharedMethodInstantiation

        FixupPointer<PTR_MethodDesc> m_pWrappedMethodDesc; // For WrapperStubWithInstantiations
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

inline PTR_MethodTable MethodDesc::GetMethodTable_NoLogging() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    MethodDescChunk *pChunk = GetMethodDescChunk();
    PREFIX_ASSUME(pChunk != NULL);
    return pChunk->GetMethodTable();
}

inline PTR_MethodTable MethodDesc::GetMethodTable() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    g_IBCLogger.LogMethodDescAccess(this);
    return GetMethodTable_NoLogging();
}

#ifndef BINDER
inline DPTR(RelativeFixupPointer<PTR_MethodTable>) MethodDesc::GetMethodTablePtr() const
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
#endif // !BINDER

inline mdMethodDef MethodDesc::GetMemberDef_NoLogging() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    MethodDescChunk *pChunk = GetMethodDescChunk();
    PREFIX_ASSUME(pChunk != NULL);
    UINT16   tokrange = pChunk->GetTokRange();

    UINT16 tokremainder = m_wFlags3AndTokenRemainder & enum_flag3_TokenRemainderMask;
    static_assert_no_msg(enum_flag3_TokenRemainderMask == METHOD_TOKEN_REMAINDER_MASK);

    return MergeToken(tokrange, tokremainder);
}

inline mdMethodDef MethodDesc::GetMemberDef() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    g_IBCLogger.LogMethodDescAccess(this);
    return GetMemberDef_NoLogging();
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

    if (GetMethodDescIndex() == 0)
    {
        GetMethodDescChunk()->SetTokenRange(tokrange);
    }

#ifdef _DEBUG
    if (mb != 0)
    {
        _ASSERTE(GetMemberDef_NoLogging() == mb);
    }
#endif
}

#ifdef _DEBUG 

#ifndef BINDER
inline BOOL MethodDesc::SanityCheck()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;


    // Do a simple sanity test
    if (IsRestored())
    {
        // If it looks good, do a more intensive sanity test. We don't care about the result,
        // we just want it to not AV.
        return GetMethodTable() == m_pDebugMethodTable.GetValue() && this->GetModule() != NULL;
    }
    
    return TRUE;
}

#endif // !BINDER
#endif // _DEBUG

inline BOOL MethodDesc::IsEnCAddedMethod()
{
    LIMITED_METHOD_DAC_CONTRACT;

#ifdef BINDER
    return FALSE;
#else // !BINDER
    return (GetClassification() == mcInstantiated) && AsInstantiatedMethodDesc()->IMD_IsEnCAddedMethod();
#endif // !BINDER
}

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

    g_IBCLogger.LogMethodDescAccess(this);
    return GetClassification() == mcInstantiated && AsInstantiatedMethodDesc()->IMD_IsGenericMethodDefinition();
}

// True if the method descriptor is an instantiation of a generic method.
inline BOOL MethodDesc::HasMethodInstantiation() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    return mcInstantiated == GetClassification() && AsInstantiatedMethodDesc()->IMD_HasMethodInstantiation();
}

#ifdef BINDER
inline BOOL MethodDesc::IsTypicalMethodDefinition() const
{
    WRAPPER_NO_CONTRACT;

    if (HasMethodInstantiation() && !IsGenericMethodDefinition())
        return FALSE;

    if (HasClassInstantiation() && !GetMethodTable()->IsGenericTypeDefinition())
        return FALSE;

    return TRUE;
}
#endif // !BINDER

#include "method.inl"


#endif // !_METHOD_H
