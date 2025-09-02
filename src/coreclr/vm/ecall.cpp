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
static_assert(METHOD__STRING__CTORF_FIRST + 0 == METHOD__STRING__CTORF_CHARARRAY);
static_assert(METHOD__STRING__CTORF_FIRST + 1 == METHOD__STRING__CTORF_CHARARRAY_START_LEN);
static_assert(METHOD__STRING__CTORF_FIRST + 2 == METHOD__STRING__CTORF_CHAR_COUNT);
static_assert(METHOD__STRING__CTORF_FIRST + 3 == METHOD__STRING__CTORF_CHARPTR);
static_assert(METHOD__STRING__CTORF_FIRST + 4 == METHOD__STRING__CTORF_CHARPTR_START_LEN);
static_assert(METHOD__STRING__CTORF_FIRST + 5 == METHOD__STRING__CTORF_READONLYSPANOFCHAR);
static_assert(METHOD__STRING__CTORF_FIRST + 6 == METHOD__STRING__CTORF_SBYTEPTR);
static_assert(METHOD__STRING__CTORF_FIRST + 7 == METHOD__STRING__CTORF_SBYTEPTR_START_LEN);
static_assert(METHOD__STRING__CTORF_FIRST + 8 == METHOD__STRING__CTORF_SBYTEPTR_START_LEN_ENCODING);

// ECall::CtorCharXxx has to be in same order as METHOD__STRING__CTORF_XXX
#define ECallCtor_First ECall::CtorCharArrayManaged
static_assert(ECallCtor_First + 0 == ECall::CtorCharArrayManaged);
static_assert(ECallCtor_First + 1 == ECall::CtorCharArrayStartLengthManaged);
static_assert(ECallCtor_First + 2 == ECall::CtorCharCountManaged);
static_assert(ECallCtor_First + 3 == ECall::CtorCharPtrManaged);
static_assert(ECallCtor_First + 4 == ECall::CtorCharPtrStartLengthManaged);
static_assert(ECallCtor_First + 5 == ECall::CtorReadOnlySpanOfCharManaged);
static_assert(ECallCtor_First + 6 == ECall::CtorSBytePtrManaged);
static_assert(ECallCtor_First + 7 == ECall::CtorSBytePtrStartLengthManaged);
static_assert(ECallCtor_First + 8 == ECall::CtorSBytePtrStartLengthEncodingManaged);

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

#endif // !DACCESS_COMPILE

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
PCODE ECall::GetFCallImpl(MethodDesc * pMD, bool throwForInvalidFCall)
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
    // Delegate constructors are QCalls for which the entrypoint points to the target of the delegate
    // We have to intercept these and set the call target to the managed helper Delegate.DelegateConstruct
    //
    if (pMT->IsDelegate())
    {
        _ASSERTE(pMD->IsCtor());

        // We need to set up the ECFunc properly.  We don't want to use the pMD passed in,
        // since it may disappear.  Instead, use the stable one on Delegate.  Remember
        // that this is 1:M between the method and the constructors.
        return CoreLibBinder::GetMethod(METHOD__DELEGATE__CONSTRUCT_DELEGATE)->GetMultiCallableAddrOfCode();
    }

#ifdef FEATURE_COMINTEROP
    // COM imported classes have special constructors
    if (pMT->IsComObjectType() && pMT != g_pBaseCOMObject)
    {
        // This has to be tlbimp constructor
        _ASSERTE(pMD->IsCtor());

        // FCComCtor does not need to be in the fcall hashtable since it does not erect frame.
        return GetEEFuncEntryPoint(FCComCtor);
    }
#else // !FEATURE_COMINTEROP
    // This code path is taken when a class marked with ComImport is being created.
    // If we get here and COM interop isn't suppported, throw.
    if (pMT->IsComObjectType())
    {
        if (throwForInvalidFCall)
            COMPlusThrow(kPlatformNotSupportedException, IDS_EE_ERROR_COM);
        return (PCODE)NULL;
    }
#endif // FEATURE_COMINTEROP

    if (!pMD->GetModule()->IsSystem())
    {
        if (throwForInvalidFCall)
            COMPlusThrow(kSecurityException, BFA_ECALLS_MUST_BE_IN_SYS_MOD);
        return (PCODE)NULL;
    }

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
        pImplementation = g_FCDynamicallyAssignedImplementations[iDynamicID];
        _ASSERTE(pImplementation != (PCODE)NULL);
        return pImplementation;
    }

    _ASSERTE(pImplementation != (PCODE)NULL);
    return pImplementation;
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
                    minipal_log_print_error("CheckCoreLibExtended: Unused ecall found: %s.%s::%s\n", pECClass->m_szNameSpace, c_rgECClasses[ImplsIndex].m_szClassName, ptr->m_szMethodName);
                    fUnusedFCallsFound = TRUE;
                    continue;
                }
            }
            fUnreferencedType = FALSE;
        }

        if (fUnreferencedType)
        {
            minipal_log_print_error("CheckCoreLibExtended: Unused type found: %s.%s\n", c_rgECClasses[ImplsIndex].m_szNameSpace, c_rgECClasses[ImplsIndex].m_szClassName);
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
}
FCIMPLEND

#endif // !DACCESS_COMPILE
