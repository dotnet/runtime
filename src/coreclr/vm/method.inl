// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



#ifndef _METHOD_INL_
#define _METHOD_INL_

inline bool MethodDesc::IsEligibleForTieredCompilation()
{
    LIMITED_METHOD_DAC_CONTRACT;

#ifdef FEATURE_TIERED_COMPILATION
    _ASSERTE(GetMethodDescChunk()->DeterminedIfMethodsAreEligibleForTieredCompilation());
#endif
    return IsEligibleForTieredCompilation_NoCheckMethodDescChunk();
}

inline bool MethodDesc::IsEligibleForTieredCompilation_NoCheckMethodDescChunk()
{
    LIMITED_METHOD_DAC_CONTRACT;

    // Just like above, but without the assert. This is used in the path which initializes the flag.
#ifdef FEATURE_TIERED_COMPILATION
    return (VolatileLoadWithoutBarrier(&m_wFlags3AndTokenRemainder) & enum_flag3_IsEligibleForTieredCompilation) != 0;
#else
    return false;
#endif
}

inline InstantiatedMethodDesc* MethodDesc::AsInstantiatedMethodDesc() const
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    _ASSERTE(GetClassification() == mcInstantiated);
    return dac_cast<PTR_InstantiatedMethodDesc>(this);
}

inline SigParser MethodDesc::GetSigParser()
{
    WRAPPER_NO_CONTRACT;

    PCCOR_SIGNATURE pSig;
    ULONG cSig;
    GetSig(&pSig, &cSig);

    return SigParser(pSig, cSig);
}

inline SigPointer MethodDesc::GetSigPointer()
{
    WRAPPER_NO_CONTRACT;

    PCCOR_SIGNATURE pSig;
    ULONG cSig;
    GetSig(&pSig, &cSig);

    return SigPointer(pSig, cSig);
}

inline PTR_DynamicResolver DynamicMethodDesc::GetResolver()
{
    LIMITED_METHOD_CONTRACT;

    return m_pResolver;
}

inline PTR_LCGMethodResolver DynamicMethodDesc::GetLCGMethodResolver()
{
    CONTRACTL
    {
        MODE_ANY;
        GC_NOTRIGGER;
        NOTHROW;
        PRECONDITION(IsLCGMethod());
    }
    CONTRACTL_END;

    return PTR_LCGMethodResolver(m_pResolver);
}

inline PTR_ILStubResolver DynamicMethodDesc::GetILStubResolver()
{
    CONTRACTL
    {
        MODE_ANY;
        GC_NOTRIGGER;
        NOTHROW;
        PRECONDITION(IsILStub());
    }
    CONTRACTL_END;

    return PTR_ILStubResolver(m_pResolver);
}

inline PTR_DynamicMethodDesc MethodDesc::AsDynamicMethodDesc()
{
    CONTRACTL
    {
        MODE_ANY;
        GC_NOTRIGGER;
        NOTHROW;
        PRECONDITION(IsDynamicMethod());
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    return dac_cast<PTR_DynamicMethodDesc>(this);
}

// NOTE: Not all methods that create IL at runtime are considered dynamic methods. This only returns TRUE
// for methods represented by DynamicMethodDesc. Transient IL (see TryGenerateTransientILImplementation) also
// generates IL at runtime but returns FALSE here.
inline bool MethodDesc::IsDynamicMethod()
{
    LIMITED_METHOD_CONTRACT;
    return (mcDynamic == GetClassification());
}

inline bool MethodDesc::IsLCGMethod()
{
    WRAPPER_NO_CONTRACT;
    return ((mcDynamic == GetClassification()) && dac_cast<PTR_DynamicMethodDesc>(this)->IsLCGMethod());
}

// NOTE: This method only detects the subset of ILStubs that are internally represented using DynamicMethodDesc.
// There are other methods compiled from IL generated at runtime via ILStubLinker that still return FALSE here.
// See TryGenerateTransientILImplementation.
inline bool MethodDesc::IsILStub()
{
    WRAPPER_NO_CONTRACT;
    return ((mcDynamic == GetClassification()) && dac_cast<PTR_DynamicMethodDesc>(this)->IsILStub());
}

// This method is intended to identify runtime defined methods that don't have a corresponding Metadata or
// LCG definition so they can be filtered from diagnostic introspection (stacktraces, code viewing, stepping, etc). 
// Partly this is a user experience consideration to preserve the abstraction users would expect based on
// source code and assembly contents. Partly it is also a technical limitation that many parts of
// diagnostics don't know how to work with methods that aren't backed by metadata and IL in a module.
// Currently this method only triages methods whose code was generated from IL. We rely on filtering that occurs implicitly
// elsewhere to avoid including other kinds of stubs like prestubs or unboxing stubs.
inline bool MethodDesc::IsDiagnosticsHidden()
{
    WRAPPER_NO_CONTRACT;
    return IsILStub() || IsAsyncThunkMethod();
}

inline BOOL MethodDesc::IsQCall()
{
    WRAPPER_NO_CONTRACT;
    return (IsPInvoke() && dac_cast<PTR_PInvokeMethodDesc>(this)->IsQCall());
}

#ifdef FEATURE_COMINTEROP

// static
inline CLRToCOMCallInfo *CLRToCOMCallInfo::FromMethodDesc(MethodDesc *pMD)
{
    LIMITED_METHOD_CONTRACT;
    if (pMD->IsCLRToCOMCall())
    {
        return ((CLRToCOMCallMethodDesc *)pMD)->m_pCLRToCOMCallInfo;
    }
    else
    {
        _ASSERTE(pMD->IsEEImpl());
        return ((DelegateEEClass *)pMD->GetClass())->m_pCLRToCOMCallInfo;
    }
}

#endif //FEATURE_COMINTEROP

#ifdef FEATURE_CODE_VERSIONING
inline CodeVersionManager * MethodDesc::GetCodeVersionManager()
{
    LIMITED_METHOD_CONTRACT;
    return GetModule()->GetCodeVersionManager();
}
#endif

inline MethodDescBackpatchInfoTracker * MethodDesc::GetBackpatchInfoTracker()
{
    LIMITED_METHOD_CONTRACT;
    return GetLoaderAllocator()->GetMethodDescBackpatchInfoTracker();
}

#endif  // _METHOD_INL_

