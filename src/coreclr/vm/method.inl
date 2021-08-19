// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



#ifndef _METHOD_INL_
#define _METHOD_INL_

inline BOOL MethodDesc::HasTemporaryEntryPoint()
{
    WRAPPER_NO_CONTRACT;
    return GetMethodDescChunk()->HasTemporaryEntryPoints();
}

inline InstantiatedMethodDesc* MethodDesc::AsInstantiatedMethodDesc() const
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    _ASSERTE(GetClassification() == mcInstantiated);
    return dac_cast<PTR_InstantiatedMethodDesc>(this);
}

inline BOOL MethodDesc::IsZapped()
{
    WRAPPER_NO_CONTRACT;
    return FALSE;
}

inline PTR_DynamicResolver DynamicMethodDesc::GetResolver()
{
    LIMITED_METHOD_CONTRACT;

    return m_pResolver;
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

inline bool MethodDesc::IsILStub()
{
    WRAPPER_NO_CONTRACT;

    g_IBCLogger.LogMethodDescAccess(this);
    return ((mcDynamic == GetClassification()) && dac_cast<PTR_DynamicMethodDesc>(this)->IsILStub());
}

inline BOOL MethodDesc::IsQCall()
{
    WRAPPER_NO_CONTRACT;
    return (IsNDirect() && dac_cast<PTR_NDirectMethodDesc>(this)->IsQCall());
}

#ifdef FEATURE_COMINTEROP
FORCEINLINE DWORD MethodDesc::IsGenericComPlusCall()
{
    LIMITED_METHOD_CONTRACT;
    return m_wFlags & mdcHasComPlusCallInfo;
}

inline void MethodDesc::SetupGenericComPlusCall()
{
    LIMITED_METHOD_CONTRACT;
    m_wFlags |= mdcHasComPlusCallInfo;

    AsInstantiatedMethodDesc()->IMD_SetupGenericComPlusCall();
}
#endif // FEATURE_COMINTEROP


#ifdef FEATURE_COMINTEROP

// static
inline ComPlusCallInfo *ComPlusCallInfo::FromMethodDesc(MethodDesc *pMD)
{
    LIMITED_METHOD_CONTRACT;
    if (pMD->IsComPlusCall())
    {
        return ((ComPlusCallMethodDesc *)pMD)->m_pComPlusCallInfo;
    }
    else if (pMD->IsEEImpl())
    {
        return ((DelegateEEClass *)pMD->GetClass())->m_pComPlusCallInfo;
    }
    else
    {
        _ASSERTE(pMD->IsGenericComPlusCall());
        return pMD->AsInstantiatedMethodDesc()->IMD_GetComPlusCallInfo();
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

