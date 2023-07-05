// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



#ifndef _METHOD_INL_
#define _METHOD_INL_

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
    return ((mcDynamic == GetClassification()) && dac_cast<PTR_DynamicMethodDesc>(this)->IsILStub());
}

inline BOOL MethodDesc::IsQCall()
{
    WRAPPER_NO_CONTRACT;
    return (IsNDirect() && dac_cast<PTR_NDirectMethodDesc>(this)->IsQCall());
}

#ifdef FEATURE_COMINTEROP

// static
inline ComPlusCallInfo *ComPlusCallInfo::FromMethodDesc(MethodDesc *pMD)
{
    LIMITED_METHOD_CONTRACT;
    if (pMD->IsComPlusCall())
    {
        return ((ComPlusCallMethodDesc *)pMD)->m_pComPlusCallInfo;
    }
    else
    {
        _ASSERTE(pMD->IsEEImpl());
        return ((DelegateEEClass *)pMD->GetClass())->m_pComPlusCallInfo;
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

