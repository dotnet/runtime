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

- src/vm/ecall.cpp, add the overloads to the dynamic String table.

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

static bool IsManagedFCall(DWORD index)
{
    LIMITED_METHOD_CONTRACT;

    if (ECallCtor_First <= index && index < ECallCtor_First + NumberOfStringConstructors)
        return true;
    return false;
}

void ECall::PopulateManagedStringConstructors()
{
    STANDARD_VM_CONTRACT;

    INDEBUG(static bool fInitialized = false);
    _ASSERTE(!fInitialized);    // assume this method is only called once

    _ASSERTE(g_pStringClass != NULL);
#ifdef _DEBUG
    // Validate the metadata-free classification used by MethodDesc::IsStringCtor.
    for (MethodTable::IntroducedMethodIterator it(g_pStringClass); it.IsValid(); it.Next())
    {
        MethodDesc* pMD = it.GetMethodDesc();
        _ASSERTE(pMD->IsStringCtor() == (pMD->IsCtor() != 0));
    }
#endif // _DEBUG

    for (int i = 0; i < NumberOfStringConstructors; i++)
    {
        MethodDesc* pMD = CoreLibBinder::GetMethod((BinderMethodID)(METHOD__STRING__CTORF_FIRST + i));
        _ASSERTE(pMD != NULL);

        PCODE pDest = pMD->GetMultiCallableAddrOfCode();
        ECall::DynamicallyAssignFCallImpl(pDest, (DWORD)(ECallCtor_First + i));
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
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(index < NUM_DYNAMICALLY_ASSIGNED_FCALL_IMPLEMENTATIONS);
    g_FCDynamicallyAssignedImplementations[index] = impl;
}

/*******************************************************************************/
struct DynamicFCall
{
    LPCUTF8 m_name;
    LPHARDCODEDMETASIG m_signature;
    DWORD m_id;
};

static const DynamicFCall s_StringFCalls[] =
{
    { "FastAllocateString", nullptr, ECall::FastAllocateString },
    { COR_CTOR_METHOD_NAME, &gsig_IM_ArrChar_RetVoid, ECall::CtorCharArrayManaged },
    { COR_CTOR_METHOD_NAME, &gsig_IM_ArrChar_Int_Int_RetVoid, ECall::CtorCharArrayStartLengthManaged },
    { COR_CTOR_METHOD_NAME, &gsig_IM_PtrChar_RetVoid, ECall::CtorCharPtrManaged },
    { COR_CTOR_METHOD_NAME, &gsig_IM_PtrChar_Int_Int_RetVoid, ECall::CtorCharPtrStartLengthManaged },
    { COR_CTOR_METHOD_NAME, &gsig_IM_Char_Int_RetVoid, ECall::CtorCharCountManaged },
    { COR_CTOR_METHOD_NAME, &gsig_IM_ReadOnlySpanOfChar_RetVoid, ECall::CtorReadOnlySpanOfCharManaged },
    { COR_CTOR_METHOD_NAME, &gsig_IM_PtrSByt_RetVoid, ECall::CtorSBytePtrManaged },
    { COR_CTOR_METHOD_NAME, &gsig_IM_PtrSByt_Int_Int_RetVoid, ECall::CtorSBytePtrStartLengthManaged },
    { COR_CTOR_METHOD_NAME, &gsig_IM_PtrSByt_Int_Int_Encoding_RetVoid, ECall::CtorSBytePtrStartLengthEncodingManaged },
};

static_assert(ARRAY_SIZE(s_StringFCalls) == ECall::NUM_DYNAMICALLY_ASSIGNED_FCALL_IMPLEMENTATIONS);

static int FindDynamicFCall(MethodDesc* pMD)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (pMD->GetMethodTable() != g_pStringClass)
        return ECall::InvalidDynamicFCallId;

    DWORD cachedID = ((FCallMethodDesc*)pMD)->GetECallID();
    if (cachedID != 0)
        return cachedID - 1;

    LPCUTF8 szMethodName = pMD->GetName();
    PCCOR_SIGNATURE pMethodSig;
    ULONG       cbMethodSigLen;

    pMD->GetSig(&pMethodSig, &cbMethodSigLen);
    Module* pModule = pMD->GetModule();

    for (const DynamicFCall& entry : s_StringFCalls)
    {
        if (strcmp(entry.m_name, szMethodName) != 0)
            continue;

        if (entry.m_signature != nullptr)
        {
            Signature sig = CoreLibBinder::GetTargetSignature(entry.m_signature);

            //@GENERICS: none of these methods belong to generic classes so there is no instantiation info to pass in
            if (!MetaSig::CompareMethodSigs(pMethodSig, cbMethodSigLen, pModule, NULL,
                                            sig.GetRawSig(), sig.GetRawSigLen(), CoreLibBinder::GetModule(), NULL, FALSE))
            {
                continue;
            }
        }

        ((FCallMethodDesc*)pMD)->SetECallID(entry.m_id + 1);
        return entry.m_id;
    }

    return ECall::InvalidDynamicFCallId;
}

/*******************************************************************************
* Returns 0 if it is an ECALL,
* Otherwise returns the native entry point (FCALL)
*/
PCODE ECall::GetFCallImpl(MethodDesc * pMD, bool throwForInvalidFCall, bool* pHasManagedImpl)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(pMD->IsFCall());
    }
    CONTRACTL_END;

    // Avoid null checks in the common case
    bool hasManagedImplDummy = false;
    if (pHasManagedImpl == nullptr) pHasManagedImpl = &hasManagedImplDummy;
    *pHasManagedImpl = false;

    MethodTable * pMT = pMD->GetMethodTable();

    //
    // Delegate constructors are QCalls for which the entrypoint points to the target of the delegate
    // We have to intercept these and set the call target to the managed helper Delegate.DelegateConstruct
    //
    if (pMT->IsDelegate() && pMD->IsCtor())
    {
        _ASSERTE(pMD->IsCtor());

        *pHasManagedImpl = true;

        // We don't want to use the pMD passed in,
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

    PCODE pImplementation;
    int iDynamicID = FindDynamicFCall(pMD);
    if (iDynamicID != InvalidDynamicFCallId)
    {
        *pHasManagedImpl = IsManagedFCall(iDynamicID);
        pImplementation = g_FCDynamicallyAssignedImplementations[iDynamicID];
    }
    else
    {
        LPCUTF8 nameSpace;
        LPCUTF8 typeName = pMT->GetFullyQualifiedNameInfo(&nameSpace);
        StackSString entryPoint(SString::Utf8, typeName);
        entryPoint.AppendUTF8("_");
        entryPoint.AppendUTF8(pMD->GetName());
        pImplementation = (PCODE)QCallResolveDllImport(entryPoint.GetUTF8());
    }

    if (pImplementation == (PCODE)NULL && throwForInvalidFCall)
    {
        CONSISTENCY_CHECK_MSGF(false, ("Could not resolve internal call %s::%s.\n",
            pMD->m_pszDebugClassName, pMD->m_pszDebugMethodName));
        COMPlusThrow(kMissingMethodException);
    }

    return pImplementation;
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
