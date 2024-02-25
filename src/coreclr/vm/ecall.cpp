// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ECALL.CPP -
//
// Handles our private native calling interface.
//



#include "common.h"

#include "ecall.h"

#include "comdelegate.h"

#ifndef DACCESS_COMPILE

extern const ECClass c_rgECClasses[];
extern const int c_nECClasses;


/**********

The constructors of string-like types (String, Utf8String) are special since the JIT will
replace newobj instructions with calls to the corresponding 'Ctor' method. Depending on the
CLR in use, the ctor methods may be instance methods (with a null 'this' parameter) or
static methods. See the managed definitions of String.Ctor and Utf8String.Ctor for more
information.

To add a new ctor overload, in addition to defining the constructor and Ctor methods on
the managed side, make changes to the following files. (These instructions are for
Utf8String, but String is similar.)

- src/vm/ecall.cpp (this file), update the definition of "NumberOfUtf8StringConstructors"
  and add the appropriate static asserts immediately above the definition.

- src/vm/ecall.h, search for "Utf8StringCtor" and add the DYNAMICALLY_ASSIGNED_FCALL_IMPL
  definitions corresponding to the new overloads.

- src/vm/ecalllist.h, search for "FCFuncStart(gUtf8StringFuncs)" and add the overloads
  within that block.

- src/vm/metasig.h, add the new Utf8String-returning metasig declarations; and, if necessary,
  add any void-returning metasig declarations if they haven't already been defined elsewhere.
  search "String_RetUtf8Str" for an example of how to do this.

- src/vm/corelib.h, search "DEFINE_CLASS(UTF8_STRING" and add the new DEFINE_METHOD
  declarations for the Utf8String-returning Ctor methods, referencing the new metasig declarations.

**********/

// METHOD__STRING__CTORF_XXX has to be in same order as ECall::CtorCharXxx
#define METHOD__STRING__CTORF_FIRST METHOD__STRING__CTORF_CHARARRAY
static_assert_no_msg(METHOD__STRING__CTORF_FIRST + 0 == METHOD__STRING__CTORF_CHARARRAY);
static_assert_no_msg(METHOD__STRING__CTORF_FIRST + 1 == METHOD__STRING__CTORF_CHARARRAY_START_LEN);
static_assert_no_msg(METHOD__STRING__CTORF_FIRST + 2 == METHOD__STRING__CTORF_CHAR_COUNT);
static_assert_no_msg(METHOD__STRING__CTORF_FIRST + 3 == METHOD__STRING__CTORF_CHARPTR);
static_assert_no_msg(METHOD__STRING__CTORF_FIRST + 4 == METHOD__STRING__CTORF_CHARPTR_START_LEN);
static_assert_no_msg(METHOD__STRING__CTORF_FIRST + 5 == METHOD__STRING__CTORF_READONLYSPANOFCHAR);
static_assert_no_msg(METHOD__STRING__CTORF_FIRST + 6 == METHOD__STRING__CTORF_SBYTEPTR);
static_assert_no_msg(METHOD__STRING__CTORF_FIRST + 7 == METHOD__STRING__CTORF_SBYTEPTR_START_LEN);
static_assert_no_msg(METHOD__STRING__CTORF_FIRST + 8 == METHOD__STRING__CTORF_SBYTEPTR_START_LEN_ENCODING);

// ECall::CtorCharXxx has to be in same order as METHOD__STRING__CTORF_XXX
#define ECallCtor_First ECall::CtorCharArrayManaged
static_assert_no_msg(ECallCtor_First + 0 == ECall::CtorCharArrayManaged);
static_assert_no_msg(ECallCtor_First + 1 == ECall::CtorCharArrayStartLengthManaged);
static_assert_no_msg(ECallCtor_First + 2 == ECall::CtorCharCountManaged);
static_assert_no_msg(ECallCtor_First + 3 == ECall::CtorCharPtrManaged);
static_assert_no_msg(ECallCtor_First + 4 == ECall::CtorCharPtrStartLengthManaged);
static_assert_no_msg(ECallCtor_First + 5 == ECall::CtorReadOnlySpanOfCharManaged);
static_assert_no_msg(ECallCtor_First + 6 == ECall::CtorSBytePtrManaged);
static_assert_no_msg(ECallCtor_First + 7 == ECall::CtorSBytePtrStartLengthManaged);
static_assert_no_msg(ECallCtor_First + 8 == ECall::CtorSBytePtrStartLengthEncodingManaged);

#define NumberOfStringConstructors 9

void ECall::PopulateManagedStringConstructors()
{
    STANDARD_VM_CONTRACT;

    INDEBUG(static bool fInitialized = false);
    _ASSERTE(!fInitialized);    // assume this method is only called once

    _ASSERTE(g_pStringClass != NULL);
    for (int i = 0; i < NumberOfStringConstructors; i++)
    {
        MethodDesc* pMD = CoreLibBinder::GetMethod((BinderMethodID)(METHOD__STRING__CTORF_FIRST + i));
        _ASSERTE(pMD != NULL);

        PCODE pDest = pMD->GetMultiCallableAddrOfCode();

        ECall::DynamicallyAssignFCallImpl(pDest, ECallCtor_First + i);
    }

    INDEBUG(fInitialized = true);
}

void ECall::PopulateManagedHelpers()
{

    STANDARD_VM_CONTRACT;

    MethodDesc* pMD = CoreLibBinder::GetMethod((BinderMethodID)(METHOD__CASTHELPERS__ISINSTANCEOFANY));
    PCODE pDest = pMD->GetMultiCallableAddrOfCode();
    SetJitHelperFunction(CORINFO_HELP_ISINSTANCEOFANY, pDest);
    // array cast uses the "ANY" helper
    SetJitHelperFunction(CORINFO_HELP_ISINSTANCEOFARRAY, pDest);

    pMD = CoreLibBinder::GetMethod((BinderMethodID)(METHOD__CASTHELPERS__ISINSTANCEOFINTERFACE));
    pDest = pMD->GetMultiCallableAddrOfCode();
    SetJitHelperFunction(CORINFO_HELP_ISINSTANCEOFINTERFACE, pDest);

    pMD = CoreLibBinder::GetMethod((BinderMethodID)(METHOD__CASTHELPERS__ISINSTANCEOFCLASS));
    pDest = pMD->GetMultiCallableAddrOfCode();
    SetJitHelperFunction(CORINFO_HELP_ISINSTANCEOFCLASS, pDest);

    pMD = CoreLibBinder::GetMethod((BinderMethodID)(METHOD__CASTHELPERS__CHKCASTANY));
    pDest = pMD->GetMultiCallableAddrOfCode();
    SetJitHelperFunction(CORINFO_HELP_CHKCASTANY, pDest);
    // array cast uses the "ANY" helper
    SetJitHelperFunction(CORINFO_HELP_CHKCASTARRAY, pDest);

    pMD = CoreLibBinder::GetMethod((BinderMethodID)(METHOD__CASTHELPERS__CHKCASTINTERFACE));
    pDest = pMD->GetMultiCallableAddrOfCode();
    SetJitHelperFunction(CORINFO_HELP_CHKCASTINTERFACE, pDest);

    pMD = CoreLibBinder::GetMethod((BinderMethodID)(METHOD__CASTHELPERS__CHKCASTCLASS));
    pDest = pMD->GetMultiCallableAddrOfCode();
    SetJitHelperFunction(CORINFO_HELP_CHKCASTCLASS, pDest);

    pMD = CoreLibBinder::GetMethod((BinderMethodID)(METHOD__CASTHELPERS__CHKCASTCLASSSPECIAL));
    pDest = pMD->GetMultiCallableAddrOfCode();
    SetJitHelperFunction(CORINFO_HELP_CHKCASTCLASS_SPECIAL, pDest);

    pMD = CoreLibBinder::GetMethod((BinderMethodID)(METHOD__CASTHELPERS__UNBOX));
    pDest = pMD->GetMultiCallableAddrOfCode();
    SetJitHelperFunction(CORINFO_HELP_UNBOX, pDest);

    pMD = CoreLibBinder::GetMethod((BinderMethodID)(METHOD__CASTHELPERS__STELEMREF));
    pDest = pMD->GetMultiCallableAddrOfCode();
    SetJitHelperFunction(CORINFO_HELP_ARRADDR_ST, pDest);

    pMD = CoreLibBinder::GetMethod((BinderMethodID)(METHOD__CASTHELPERS__LDELEMAREF));
    pDest = pMD->GetMultiCallableAddrOfCode();
    SetJitHelperFunction(CORINFO_HELP_LDELEMA_REF, pDest);

    pMD = CoreLibBinder::GetMethod((BinderMethodID)(METHOD__SPAN_HELPERS__MEMSET));
    pDest = pMD->GetMultiCallableAddrOfCode();
    SetJitHelperFunction(CORINFO_HELP_MEMSET, pDest);

    pMD = CoreLibBinder::GetMethod((BinderMethodID)(METHOD__SPAN_HELPERS__MEMZERO));
    pDest = pMD->GetMultiCallableAddrOfCode();
    SetJitHelperFunction(CORINFO_HELP_MEMZERO, pDest);

    pMD = CoreLibBinder::GetMethod((BinderMethodID)(METHOD__SPAN_HELPERS__MEMCOPY));
    pDest = pMD->GetMultiCallableAddrOfCode();
    SetJitHelperFunction(CORINFO_HELP_MEMCPY, pDest);

    pMD = CoreLibBinder::GetMethod((BinderMethodID)(METHOD__MATH__LONG_MULTIPLY_OVERFLOW));
    pDest = pMD->GetMultiCallableAddrOfCode();
    SetJitHelperFunction(CORINFO_HELP_LMUL_OVF, pDest);

    pMD = CoreLibBinder::GetMethod((BinderMethodID)(METHOD__MATH__ULONG_MULTIPLY_OVERFLOW));
    pDest = pMD->GetMultiCallableAddrOfCode();
    SetJitHelperFunction(CORINFO_HELP_ULMUL_OVF, pDest);

    pMD = CoreLibBinder::GetMethod((BinderMethodID)(METHOD__MATH__ULONG_TO_DOUBLE));
    pDest = pMD->GetMultiCallableAddrOfCode();
    SetJitHelperFunction(CORINFO_HELP_ULNG2DBL, pDest);

    pMD = CoreLibBinder::GetMethod((BinderMethodID)(METHOD__MATH__DOUBLE_TO_ULONG));
    pDest = pMD->GetMultiCallableAddrOfCode();
    SetJitHelperFunction(CORINFO_HELP_DBL2ULNG, pDest);

    pMD = CoreLibBinder::GetMethod((BinderMethodID)(METHOD__MATH__DOUBLE_TO_INT_OVERFLOW));
    pDest = pMD->GetMultiCallableAddrOfCode();
    SetJitHelperFunction(CORINFO_HELP_DBL2INT_OVF, pDest);

    pMD = CoreLibBinder::GetMethod((BinderMethodID)(METHOD__MATH__DOUBLE_TO_UINT_OVERFLOW));
    pDest = pMD->GetMultiCallableAddrOfCode();
    SetJitHelperFunction(CORINFO_HELP_DBL2UINT_OVF, pDest);

    pMD = CoreLibBinder::GetMethod((BinderMethodID)(METHOD__MATH__DOUBLE_TO_LONG_OVERFLOW));
    pDest = pMD->GetMultiCallableAddrOfCode();
    SetJitHelperFunction(CORINFO_HELP_DBL2LNG_OVF, pDest);

    pMD = CoreLibBinder::GetMethod((BinderMethodID)(METHOD__MATH__DOUBLE_TO_ULONG_OVERFLOW));
    pDest = pMD->GetMultiCallableAddrOfCode();
    SetJitHelperFunction(CORINFO_HELP_DBL2ULNG_OVF, pDest);

    pMD = CoreLibBinder::GetMethod((BinderMethodID)(METHOD__MATH__DOUBLE_REMINDER));
    pDest = pMD->GetMultiCallableAddrOfCode();
    SetJitHelperFunction(CORINFO_HELP_DBLREM, pDest);

    pMD = CoreLibBinder::GetMethod((BinderMethodID)(METHOD__MATHF__FLOAT_REMINDER));
    pDest = pMD->GetMultiCallableAddrOfCode();
    SetJitHelperFunction(CORINFO_HELP_FLTREM, pDest);

    pMD = CoreLibBinder::GetMethod((BinderMethodID)(METHOD__MATH__DOUBLE_TO_INT));
    pDest = pMD->GetMultiCallableAddrOfCode();
    SetJitHelperFunction(CORINFO_HELP_DBL2INT, pDest);

    pMD = CoreLibBinder::GetMethod((BinderMethodID)(METHOD__MATH__DOUBLE_TO_UINT));
    pDest = pMD->GetMultiCallableAddrOfCode();
    SetJitHelperFunction(CORINFO_HELP_DBL2UINT, pDest);

    pMD = CoreLibBinder::GetMethod((BinderMethodID)(METHOD__MATH__ROUND));
    pDest = pMD->GetMultiCallableAddrOfCode();
    SetJitHelperFunction(CORINFO_HELP_DBLROUND, pDest);

    pMD = CoreLibBinder::GetMethod((BinderMethodID)(METHOD__MATHF__ROUND));
    pDest = pMD->GetMultiCallableAddrOfCode();
    SetJitHelperFunction(CORINFO_HELP_FLTROUND, pDest);
}

static CrstStatic gFCallLock;

// This variable is used to force the compiler not to tailcall a function.
RAW_KEYWORD(volatile) int FC_NO_TAILCALL;

#endif // !DACCESS_COMPILE

// To provide a quick check, this is the lowest and highest
// addresses of any FCALL starting address
GVAL_IMPL_INIT(TADDR, gLowestFCall, (TADDR)-1);
GVAL_IMPL(TADDR, gHighestFCall);

GARY_IMPL(PTR_ECHash, gFCallMethods, FCALL_HASH_SIZE);

inline unsigned FCallHash(PCODE pTarg) {
    LIMITED_METHOD_DAC_CONTRACT;
    return pTarg % FCALL_HASH_SIZE;
}

#ifdef DACCESS_COMPILE

GARY_IMPL(PCODE, g_FCDynamicallyAssignedImplementations,
          ECall::NUM_DYNAMICALLY_ASSIGNED_FCALL_IMPLEMENTATIONS);

#else // !DACCESS_COMPILE

PCODE g_FCDynamicallyAssignedImplementations[ECall::NUM_DYNAMICALLY_ASSIGNED_FCALL_IMPLEMENTATIONS] = {
    #undef DYNAMICALLY_ASSIGNED_FCALL_IMPL
    #define DYNAMICALLY_ASSIGNED_FCALL_IMPL(id,defaultimpl) GetEEFuncEntryPoint(defaultimpl),
    DYNAMICALLY_ASSIGNED_FCALLS()
};

void ECall::DynamicallyAssignFCallImpl(PCODE impl, DWORD index)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(index < NUM_DYNAMICALLY_ASSIGNED_FCALL_IMPLEMENTATIONS);
    g_FCDynamicallyAssignedImplementations[index] = impl;
}

/*******************************************************************************/
static INT FindImplsIndexForClass(MethodTable* pMT)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    LPCUTF8 pszNamespace = 0;
    LPCUTF8 pszName = pMT->GetFullyQualifiedNameInfo(&pszNamespace);

    // Array classes get null from the above routine, but they have no ecalls.
    if (pszName == NULL)
        return (-1);

    unsigned low  = 0;
    unsigned high = c_nECClasses;

#ifdef _DEBUG
    static bool checkedSort = false;
    if (!checkedSort) {
        checkedSort = true;
        for (unsigned i = 1; i < high; i++)  {
                // Make certain list is sorted!
            int cmp = strcmp(c_rgECClasses[i].m_szClassName, c_rgECClasses[i-1].m_szClassName);
            if (cmp == 0)
                cmp = strcmp(c_rgECClasses[i].m_szNameSpace, c_rgECClasses[i-1].m_szNameSpace);
            _ASSERTE(cmp > 0 && W("You forgot to keep ECall class names sorted"));      // Hey, you forgot to sort the new class
        }
    }
#endif // _DEBUG
    while (high > low) {
        unsigned mid  = (high + low) / 2;
        int cmp = strcmp(pszName, c_rgECClasses[mid].m_szClassName);
        if (cmp == 0)
            cmp = strcmp(pszNamespace, c_rgECClasses[mid].m_szNameSpace);

        if (cmp == 0) {
            return(mid);
        }
        if (cmp > 0)
            low = mid+1;
        else
            high = mid;
    }

    return (-1);
}

/*******************************************************************************/
/*  Finds the implementation for the given method desc.  */

static INT FindECIndexForMethod(MethodDesc *pMD, const LPVOID* impls)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    LPCUTF8 szMethodName = pMD->GetName();
    PCCOR_SIGNATURE pMethodSig;
    ULONG       cbMethodSigLen;

    pMD->GetSig(&pMethodSig, &cbMethodSigLen);
    Module* pModule = pMD->GetModule();

    for (ECFunc* cur = (ECFunc*)impls; !cur->IsEndOfArray(); cur = cur->NextInArray())
    {
        if (strcmp(cur->m_szMethodName, szMethodName) != 0)
            continue;

        if (cur->HasSignature())
        {
            Signature sig = CoreLibBinder::GetTargetSignature(cur->m_pMethodSig);

            //@GENERICS: none of these methods belong to generic classes so there is no instantiation info to pass in
            if (!MetaSig::CompareMethodSigs(pMethodSig, cbMethodSigLen, pModule, NULL,
                                            sig.GetRawSig(), sig.GetRawSigLen(), CoreLibBinder::GetModule(), NULL, FALSE))
            {
                continue;
            }
        }

        // We have found a match!
        return static_cast<INT>((LPVOID*)cur - impls);
    }

    return -1;
}

/*******************************************************************************/
/* ID is formed of 2 USHORTs - class index  in high word, method index in low word.  */
/* class index starts at 1. id == 0 means no implementation.                    */

DWORD ECall::GetIDForMethod(MethodDesc *pMD)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    INT ImplsIndex = FindImplsIndexForClass(pMD->GetMethodTable());
    if (ImplsIndex < 0)
        return 0;
    INT ECIndex = FindECIndexForMethod(pMD, c_rgECClasses[ImplsIndex].m_pECFunc);
    if (ECIndex < 0)
        return 0;

    return (ImplsIndex<<16) | (ECIndex + 1);
}

static ECFunc *FindECFuncForID(DWORD id)
{
    LIMITED_METHOD_CONTRACT;

    if (id == 0)
        return NULL;

    INT ImplsIndex  = (id >> 16);
    INT ECIndex     = (id & 0xffff) - 1;

    return (ECFunc*)(c_rgECClasses[ImplsIndex].m_pECFunc + ECIndex);
}

static ECFunc* FindECFuncForMethod(MethodDesc* pMD)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(pMD->IsFCall());
    }
    CONTRACTL_END;

    DWORD id = ((FCallMethodDesc *)pMD)->GetECallID();
    if (id == 0)
    {
        id = ECall::GetIDForMethod(pMD);

        CONSISTENCY_CHECK_MSGF(0 != id,
                    ("No method entry found for %s::%s.\n",
                    pMD->m_pszDebugClassName, pMD->m_pszDebugMethodName));

        // Cache the id
        ((FCallMethodDesc *)pMD)->SetECallID(id);
    }

    return FindECFuncForID(id);
}

/*******************************************************************************
* Returns 0 if it is an ECALL,
* Otherwise returns the native entry point (FCALL)
*/
PCODE ECall::GetFCallImpl(MethodDesc * pMD, BOOL * pfSharedOrDynamicFCallImpl /*=NULL*/)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(pMD->IsFCall());
    }
    CONTRACTL_END;

    MethodTable * pMT = pMD->GetMethodTable();

    //
    // Delegate constructors are FCalls for which the entrypoint points to the target of the delegate
    // We have to intercept these and set the call target to the helper COMDelegate::DelegateConstruct
    //
    if (pMT->IsDelegate())
    {
        if (pfSharedOrDynamicFCallImpl)
            *pfSharedOrDynamicFCallImpl = TRUE;

        // COMDelegate::DelegateConstruct is the only fcall used by user delegates.
        // All the other gDelegateFuncs are only used by System.Delegate
        _ASSERTE(pMD->IsCtor());

        // We need to set up the ECFunc properly.  We don't want to use the pMD passed in,
        // since it may disappear.  Instead, use the stable one on Delegate.  Remember
        // that this is 1:M between the FCall and the pMDs.
        return GetFCallImpl(CoreLibBinder::GetMethod(METHOD__DELEGATE__CONSTRUCT_DELEGATE));
    }

    // COM imported classes have special constructors
    if (pMT->IsComObjectType()
#ifdef FEATURE_COMINTEROP
        && (g_pBaseCOMObject == NULL || pMT != g_pBaseCOMObject)
#endif // FEATURE_COMINTEROP
    )
    {
        if (pfSharedOrDynamicFCallImpl)
            *pfSharedOrDynamicFCallImpl = TRUE;

        // This has to be tlbimp constructor
        _ASSERTE(pMD->IsCtor());

        // FCComCtor does not need to be in the fcall hashtable since it does not erect frame.
        return GetEEFuncEntryPoint(FCComCtor);
    }

    if (!pMD->GetModule()->IsSystem())
        COMPlusThrow(kSecurityException, BFA_ECALLS_MUST_BE_IN_SYS_MOD);

    ECFunc* ret = FindECFuncForMethod(pMD);

    // ECall is a set of tables to call functions within the EE from the classlibs.
    // First we use the class name & namespace to find an array of function pointers for
    // a class, then use the function name (& sometimes signature) to find the correct
    // function pointer for your method.  Methods in the BCL will be marked as
    // [MethodImplAttribute(MethodImplOptions.InternalCall)] and extern.
    //
    // You'll see this assert in several situations, almost all being the fault of whomever
    // last touched a particular ecall or fcall method, either here or in the classlibs.
    // However, you must also ensure you don't have stray copies of System.Private.CoreLib.dll on your machine.
    // 1) You forgot to add your class to c_rgECClasses, the list of classes w/ ecall & fcall methods.
    // 2) You forgot to add your particular method to the ECFunc array for your class.
    // 3) You misspelled the name of your function and/or classname.
    // 4) The signature of the managed function doesn't match the hardcoded metadata signature
    //    listed in your ECFunc array.  The hardcoded metadata sig is only necessary to disambiguate
    //    overloaded ecall functions - usually you can leave it set to NULL.
    // 5) Your copy of System.Private.CoreLib.dll & coreclr.dll are out of sync - rebuild both.
    // 6) You've loaded the wrong copy of System.Private.CoreLib.dll.  In Visual Studio's debug menu,
    //    select the "Modules..." dialog.  Verify the path for System.Private.CoreLib is right.
    // 7) Someone mucked around with how the signatures in metasig.h are parsed, changing the
    //    interpretation of a part of the signature (this is very rare & extremely unlikely,
    //    but has happened at least once).

    CONSISTENCY_CHECK_MSGF(ret != NULL,
        ("Could not find an ECALL entry for %s::%s.\n"
        "Read comment above this assert in vm/ecall.cpp\n",
        pMD->m_pszDebugClassName, pMD->m_pszDebugMethodName));

    PCODE pImplementation = (PCODE)ret->m_pImplementation;

    int iDynamicID = ret->DynamicID();
    if (iDynamicID != InvalidDynamicFCallId)
    {
        if (pfSharedOrDynamicFCallImpl)
            *pfSharedOrDynamicFCallImpl = TRUE;

        pImplementation = g_FCDynamicallyAssignedImplementations[iDynamicID];
        _ASSERTE(pImplementation != NULL);
        return pImplementation;
    }


    // Insert the implementation into hash table if it is not there already.

    CrstHolder holder(&gFCallLock);

    MethodDesc * pMDinTable = ECall::MapTargetBackToMethod(pImplementation, &pImplementation);

    if (pMDinTable != NULL)
    {
        if (pMDinTable != pMD)
        {
            // The fcall entrypoints has to be at unique addresses. If you get failure here, use the following steps
            // to fix it:
            // 1. Consider merging the offending fcalls into one fcall. Do they really do different things?
            // 2. If it does not make sense to merge the offending fcalls into one,
            // add FCUnique(<a random unique number here>); to one of the offending fcalls.

            _ASSERTE(!"Duplicate pImplementation entries found in reverse fcall table");
            ThrowHR(E_FAIL);
        }
    }
    else
    {
        ECHash * pEntry = (ECHash *)(PVOID)SystemDomain::GetGlobalLoaderAllocator()->GetHighFrequencyHeap()->AllocMem(S_SIZE_T(sizeof(ECHash)));

        pEntry->m_pImplementation = pImplementation;
        pEntry->m_pMD = pMD;

        if(gLowestFCall > pImplementation)
            gLowestFCall = pImplementation;
        if(gHighestFCall < pImplementation)
            gHighestFCall = pImplementation;

        // add to hash table
        ECHash** spot = &gFCallMethods[FCallHash(pImplementation)];
        for(;;) {
            if (*spot == 0) {                   // found end of list
                *spot = pEntry;
                break;
            }
            spot = &(*spot)->m_pNext;
        }
    }

    if (pfSharedOrDynamicFCallImpl)
        *pfSharedOrDynamicFCallImpl = FALSE;

    _ASSERTE(pImplementation != NULL);
    return pImplementation;
}

BOOL ECall::IsSharedFCallImpl(PCODE pImpl)
{
    LIMITED_METHOD_CONTRACT;

    PCODE pNativeCode = pImpl;

    return
        (pNativeCode == GetEEFuncEntryPoint(FCComCtor)) ||
        (pNativeCode == GetEEFuncEntryPoint(COMDelegate::DelegateConstruct));
}

BOOL ECall::CheckUnusedECalls(SetSHash<DWORD>& usedIDs)
{
    STANDARD_VM_CONTRACT;

    BOOL fUnusedFCallsFound = FALSE;

    INT num = c_nECClasses;
    for (INT ImplsIndex=0; ImplsIndex < num; ImplsIndex++)
    {
        const ECClass * pECClass = c_rgECClasses + ImplsIndex;

        BOOL fUnreferencedType = TRUE;
        for (ECFunc* ptr = (ECFunc*)pECClass->m_pECFunc; !ptr->IsEndOfArray(); ptr = ptr->NextInArray())
        {
            if (ptr->DynamicID() == InvalidDynamicFCallId && !ptr->IsUnreferenced())
            {
                INT ECIndex = static_cast<INT>((LPVOID*)ptr - pECClass->m_pECFunc);

                DWORD id = (ImplsIndex<<16) | (ECIndex + 1);

                if (!usedIDs.Contains(id))
                {
                    printf("CheckCoreLibExtended: Unused ecall found: %s.%s::%s\n", pECClass->m_szNameSpace, c_rgECClasses[ImplsIndex].m_szClassName, ptr->m_szMethodName);
                    fUnusedFCallsFound = TRUE;
                    continue;
                }
            }
            fUnreferencedType = FALSE;
        }

        if (fUnreferencedType)
        {
            printf("CheckCoreLibExtended: Unused type found: %s.%s\n", c_rgECClasses[ImplsIndex].m_szNameSpace, c_rgECClasses[ImplsIndex].m_szClassName);
            fUnusedFCallsFound = TRUE;
            continue;
        }
    }

    return !fUnusedFCallsFound;
}


// This function is a stub implementation for the constructor of a ComImport class.
// The actual work to implement COM Activation (and built-in COM support checks) is done as part
// of the implementation of object allocation. As a result, the constructor itself has no extra
// work to do once the object has been allocated. As a result, we just provide a dummy implementation
// here since a constructor has to have an implementation.
FCIMPL1(VOID, FCComCtor, LPVOID pV)
{
    FCALL_CONTRACT;

    FCUnique(0x34);
}
FCIMPLEND



/* static */
void ECall::Init()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    gFCallLock.Init(CrstFCall);

    // It is important to do an explicit increment here instead of just in-place initialization
    // so that the global optimizer cannot figure out the value and remove the side-effect that
    // we depend on in FC_INNER_RETURN macros and other places
    FC_NO_TAILCALL++;
}
#endif // !DACCESS_COMPILE

MethodDesc* ECall::MapTargetBackToMethod(PCODE pTarg, PCODE * ppAdjustedEntryPoint /*=NULL*/)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        HOST_NOCALLS;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    // Searching all of the entries is expensive
    // and we are often called with pTarg == NULL so
    // check for this value and early exit.

    if (!pTarg)
        return NULL;

    // Could this possibily be an FCall?
    if ((pTarg < gLowestFCall) || (pTarg > gHighestFCall))
        return NULL;

    ECHash * pECHash = gFCallMethods[FCallHash(pTarg)];
    while (pECHash != NULL)
    {
        if (pECHash->m_pImplementation == pTarg)
        {
            return pECHash->m_pMD;
        }
        pECHash = pECHash->m_pNext;
    }
    return NULL;
}

#ifndef DACCESS_COMPILE

#ifdef _DEBUG

void FCallAssert(void*& cache, void* target)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_DEBUG_ONLY;

    if (cache != 0)
    {
        return;
    }

    //
    // Special case fcalls with 1:N mapping between implementation and methoddesc
    //
    if (ECall::IsSharedFCallImpl((PCODE)target))
    {
        cache = (void*)1;
        return;
    }

    MethodDesc* pMD = ECall::MapTargetBackToMethod((PCODE)target);
    if (pMD != 0)
    {
        return;
    }

    // Slow but only for debugging.  This is needed because in some places
    // we call FCALLs directly from EE code.

    unsigned num = c_nECClasses;
    for (unsigned i=0; i < num; i++)
    {
        for (ECFunc* ptr = (ECFunc*)c_rgECClasses[i].m_pECFunc; !ptr->IsEndOfArray(); ptr = ptr->NextInArray())
        {
            if (ptr->m_pImplementation  == target)
            {
                cache = target;
                return;
            }
        }
    }

    // Now check the dynamically assigned table too.
    for (unsigned i=0; i<ECall::NUM_DYNAMICALLY_ASSIGNED_FCALL_IMPLEMENTATIONS; i++)
    {
        if (g_FCDynamicallyAssignedImplementations[i] == (PCODE)target)
        {
            cache = target;
            return;
        }
    }

    _ASSERTE(!"Could not find FCall implementation in ECall.cpp");
}

void HCallAssert(void*& cache, void* target)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        DEBUG_ONLY;
    }
    CONTRACTL_END;

    if (cache != 0)
        cache = ECall::MapTargetBackToMethod((PCODE)target);
    _ASSERTE(cache == 0 || "Use FCIMPL for fcalls");
}

#endif // _DEBUG

#endif // !DACCESS_COMPILE

#ifdef DACCESS_COMPILE

void ECall::EnumFCallMethods()
{
    SUPPORTS_DAC;
    gLowestFCall.EnumMem();
    gHighestFCall.EnumMem();
    gFCallMethods.EnumMem();

    // save all ECFunc for stackwalks.
    // TODO: we could be smarter and only save buckets referenced during stackwalks. But we
    // need that entire bucket so that traversals such as MethodDesc* ECall::MapTargetBackToMethod will work.
    for (UINT i=0;i<FCALL_HASH_SIZE;i++)
    {
        ECHash *ecHash = gFCallMethods[i];
        while (ecHash)
        {
            // If we can't read the target memory, stop immediately so we don't work
            // with broken data.
            if (!DacEnumMemoryRegion(dac_cast<TADDR>(ecHash), sizeof(ECHash)))
                break;
            ecHash = ecHash->m_pNext;

#if defined (_DEBUG)
            // Test hook: when testing on debug builds, we want an easy way to test that the while
            // correctly terminates in the face of ridiculous stuff from the target.
            if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_DumpGeneration_IntentionallyCorruptDataFromTarget) == 1)
            {
                // Force us to struggle on with something bad.
                if (!ecHash)
                {
                    ecHash = (ECHash *)(((unsigned char *)&gFCallMethods[i])+1);
                }
            }
#endif // defined (_DEBUG)

        }
    }
}

#endif // DACCESS_COMPILE
