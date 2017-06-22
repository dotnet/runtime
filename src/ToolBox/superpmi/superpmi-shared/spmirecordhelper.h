//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

//----------------------------------------------------------
// SpmiRecordHelper.h - a helper to copy data between agnostic/non-agnostic types.
//----------------------------------------------------------
#ifndef _SpmiRecordsHelper
#define _SpmiRecordsHelper

#include "methodcontext.h"

class SpmiRecordsHelper
{
public:
    static MethodContext::Agnostic_CORINFO_RESOLVED_TOKENin CreateAgnostic_CORINFO_RESOLVED_TOKENin(
        CORINFO_RESOLVED_TOKEN* pResolvedToken);

    static MethodContext::Agnostic_CORINFO_RESOLVED_TOKENout CreateAgnostic_CORINFO_RESOLVED_TOKENout_without_buffers(
        CORINFO_RESOLVED_TOKEN* pResolvedToken);

    template <typename key, typename value>
    static MethodContext::Agnostic_CORINFO_RESOLVED_TOKENout StoreAgnostic_CORINFO_RESOLVED_TOKENout(
        CORINFO_RESOLVED_TOKEN* pResolvedToken, LightWeightMap<key, value>* buffers);

    template <typename key, typename value>
    static MethodContext::Agnostic_CORINFO_RESOLVED_TOKENout RestoreAgnostic_CORINFO_RESOLVED_TOKENout(
        CORINFO_RESOLVED_TOKEN* pResolvedToken, LightWeightMap<key, value>* buffers);

    template <typename key, typename value>
    static MethodContext::Agnostic_CORINFO_RESOLVED_TOKEN StoreAgnostic_CORINFO_RESOLVED_TOKEN(
        CORINFO_RESOLVED_TOKEN* pResolvedToken, LightWeightMap<key, value>* buffers);

    template <typename key, typename value>
    static MethodContext::Agnostic_CORINFO_RESOLVED_TOKEN RestoreAgnostic_CORINFO_RESOLVED_TOKEN(
        CORINFO_RESOLVED_TOKEN* pResolvedToken, LightWeightMap<key, value>* buffers);


    // Restore the out values in the first argument from the second.
    // Can't just return whole CORINFO_RESOLVED_TOKEN because [in] values in it are important too.
    template <typename key, typename value>
    static void Restore_CORINFO_RESOLVED_TOKENout(CORINFO_RESOLVED_TOKEN* pResolvedToken,
        MethodContext::Agnostic_CORINFO_RESOLVED_TOKENout& token, LightWeightMap<key, value>* buffers);

    static MethodContext::Agnostic_CORINFO_SIG_INFO CreateAgnostic_CORINFO_SIG_INFO_without_buffers(
        CORINFO_SIG_INFO& sigInfo);

    template <typename key, typename value>
    static MethodContext::Agnostic_CORINFO_SIG_INFO StoreAgnostic_CORINFO_SIG_INFO(CORINFO_SIG_INFO& sigInfo,
                                                                                   LightWeightMap<key, value>* buffers);

    template <typename key, typename value>
    static MethodContext::Agnostic_CORINFO_SIG_INFO RestoreAgnostic_CORINFO_SIG_INFO(
        CORINFO_SIG_INFO& sigInfo, LightWeightMap<key, value>* buffers);

    template <typename key, typename value>
    static CORINFO_SIG_INFO Restore_CORINFO_SIG_INFO(MethodContext::Agnostic_CORINFO_SIG_INFO& sigInfo,
                                                     LightWeightMap<key, value>* buffers);
};

inline MethodContext::Agnostic_CORINFO_RESOLVED_TOKENin SpmiRecordsHelper::CreateAgnostic_CORINFO_RESOLVED_TOKENin(
    CORINFO_RESOLVED_TOKEN* pResolvedToken)
{
    MethodContext::Agnostic_CORINFO_RESOLVED_TOKENin tokenIn;
    ZeroMemory(&tokenIn, sizeof(tokenIn));
    tokenIn.tokenContext = (DWORDLONG)pResolvedToken->tokenContext;
    tokenIn.tokenScope   = (DWORDLONG)pResolvedToken->tokenScope;
    tokenIn.token        = (DWORD)pResolvedToken->token;
    tokenIn.tokenType    = (DWORD)pResolvedToken->tokenType;
    return tokenIn;
}

inline MethodContext::Agnostic_CORINFO_RESOLVED_TOKENout SpmiRecordsHelper::
    CreateAgnostic_CORINFO_RESOLVED_TOKENout_without_buffers(CORINFO_RESOLVED_TOKEN* pResolvedToken)
{
    MethodContext::Agnostic_CORINFO_RESOLVED_TOKENout tokenOut;
    ZeroMemory(&tokenOut, sizeof(tokenOut));
    tokenOut.hClass  = (DWORDLONG)pResolvedToken->hClass;
    tokenOut.hMethod = (DWORDLONG)pResolvedToken->hMethod;
    tokenOut.hField  = (DWORDLONG)pResolvedToken->hField;

    tokenOut.cbTypeSpec   = (DWORD)pResolvedToken->cbTypeSpec;
    tokenOut.cbMethodSpec = (DWORD)pResolvedToken->cbMethodSpec;

    tokenOut.pTypeSpec_Index   = -1;
    tokenOut.pMethodSpec_Index = -1;

    return tokenOut;
}

template <typename key, typename value>
inline MethodContext::Agnostic_CORINFO_RESOLVED_TOKENout SpmiRecordsHelper::StoreAgnostic_CORINFO_RESOLVED_TOKENout(
    CORINFO_RESOLVED_TOKEN* pResolvedToken, LightWeightMap<key, value>* buffers)
{
    MethodContext::Agnostic_CORINFO_RESOLVED_TOKENout tokenOut(
        CreateAgnostic_CORINFO_RESOLVED_TOKENout_without_buffers(pResolvedToken));

    tokenOut.pTypeSpec_Index =
        (DWORD)buffers->AddBuffer((unsigned char*)pResolvedToken->pTypeSpec, pResolvedToken->cbTypeSpec);
    tokenOut.pMethodSpec_Index =
        (DWORD)buffers->AddBuffer((unsigned char*)pResolvedToken->pMethodSpec, pResolvedToken->cbMethodSpec);

    return tokenOut;
}

template <typename key, typename value>
inline MethodContext::Agnostic_CORINFO_RESOLVED_TOKENout SpmiRecordsHelper::RestoreAgnostic_CORINFO_RESOLVED_TOKENout(
    CORINFO_RESOLVED_TOKEN* pResolvedToken, LightWeightMap<key, value>* buffers)
{
    MethodContext::Agnostic_CORINFO_RESOLVED_TOKENout tokenOut(
        CreateAgnostic_CORINFO_RESOLVED_TOKENout_without_buffers(pResolvedToken));
    tokenOut.pTypeSpec_Index =
        (DWORD)buffers->Contains((unsigned char*)pResolvedToken->pTypeSpec, pResolvedToken->cbTypeSpec);
    tokenOut.pMethodSpec_Index =
        (DWORD)buffers->Contains((unsigned char*)pResolvedToken->pMethodSpec, pResolvedToken->cbMethodSpec);
    return tokenOut;
}

template <typename key, typename value>
inline MethodContext::Agnostic_CORINFO_RESOLVED_TOKEN SpmiRecordsHelper::StoreAgnostic_CORINFO_RESOLVED_TOKEN(
    CORINFO_RESOLVED_TOKEN* pResolvedToken, LightWeightMap<key, value>* buffers)
{
    MethodContext::Agnostic_CORINFO_RESOLVED_TOKEN token;
    token.inValue  = CreateAgnostic_CORINFO_RESOLVED_TOKENin(pResolvedToken);
    token.outValue = StoreAgnostic_CORINFO_RESOLVED_TOKENout(pResolvedToken, buffers);
    return token;
}

template <typename key, typename value>
inline MethodContext::Agnostic_CORINFO_RESOLVED_TOKEN SpmiRecordsHelper::RestoreAgnostic_CORINFO_RESOLVED_TOKEN(
    CORINFO_RESOLVED_TOKEN* pResolvedToken, LightWeightMap<key, value>* buffers)
{
    MethodContext::Agnostic_CORINFO_RESOLVED_TOKEN token;
    ZeroMemory(&token, sizeof(token));
    token.inValue  = CreateAgnostic_CORINFO_RESOLVED_TOKENin(pResolvedToken);
    token.outValue = RestoreAgnostic_CORINFO_RESOLVED_TOKENout(pResolvedToken, buffers);
    return token;
}

template <typename key, typename value>
inline void SpmiRecordsHelper::Restore_CORINFO_RESOLVED_TOKENout(CORINFO_RESOLVED_TOKEN* pResolvedToken,
    MethodContext::Agnostic_CORINFO_RESOLVED_TOKENout& tokenOut, LightWeightMap<key, value>* buffers)
{
    pResolvedToken->hClass       = (CORINFO_CLASS_HANDLE)tokenOut.hClass;
    pResolvedToken->hMethod      = (CORINFO_METHOD_HANDLE)tokenOut.hMethod;
    pResolvedToken->hField       = (CORINFO_FIELD_HANDLE)tokenOut.hField;
    pResolvedToken->pTypeSpec    = (PCCOR_SIGNATURE)buffers->GetBuffer(tokenOut.pTypeSpec_Index);
    pResolvedToken->cbTypeSpec   = (ULONG)tokenOut.cbTypeSpec;
    pResolvedToken->pMethodSpec  = (PCCOR_SIGNATURE)buffers->GetBuffer(tokenOut.pMethodSpec_Index);
    pResolvedToken->cbMethodSpec = (ULONG)tokenOut.cbMethodSpec;
}

inline MethodContext::Agnostic_CORINFO_SIG_INFO SpmiRecordsHelper::CreateAgnostic_CORINFO_SIG_INFO_without_buffers(
    CORINFO_SIG_INFO& sigInfo)
{
    MethodContext::Agnostic_CORINFO_SIG_INFO sig;
    ZeroMemory(&sig, sizeof(sig));
    sig.callConv               = (DWORD)sigInfo.callConv;
    sig.retTypeClass           = (DWORDLONG)sigInfo.retTypeClass;
    sig.retTypeSigClass        = (DWORDLONG)sigInfo.retTypeSigClass;
    sig.retType                = (DWORD)sigInfo.retType;
    sig.flags                  = (DWORD)sigInfo.flags;
    sig.numArgs                = (DWORD)sigInfo.numArgs;
    sig.sigInst_classInstCount = (DWORD)sigInfo.sigInst.classInstCount;
    sig.sigInst_methInstCount  = (DWORD)sigInfo.sigInst.methInstCount;
    sig.args                   = (DWORDLONG)sigInfo.args;
    sig.cbSig                  = (DWORD)sigInfo.cbSig;
    sig.scope                  = (DWORDLONG)sigInfo.scope;
    sig.token                  = (DWORD)sigInfo.token;
    return sig;
}

template <typename key, typename value>
inline MethodContext::Agnostic_CORINFO_SIG_INFO SpmiRecordsHelper::StoreAgnostic_CORINFO_SIG_INFO(
    CORINFO_SIG_INFO& sigInfo, LightWeightMap<key, value>* buffers)
{
    MethodContext::Agnostic_CORINFO_SIG_INFO sig(CreateAgnostic_CORINFO_SIG_INFO_without_buffers(sigInfo));
    sig.sigInst_classInst_Index =
        buffers->AddBuffer((unsigned char*)sigInfo.sigInst.classInst, sigInfo.sigInst.classInstCount * 8);
    sig.sigInst_methInst_Index =
        buffers->AddBuffer((unsigned char*)sigInfo.sigInst.methInst, sigInfo.sigInst.methInstCount * 8);
    sig.pSig_Index = (DWORD)buffers->AddBuffer((unsigned char*)sigInfo.pSig, sigInfo.cbSig);
    return sig;
}

template <typename key, typename value>
inline MethodContext::Agnostic_CORINFO_SIG_INFO SpmiRecordsHelper::RestoreAgnostic_CORINFO_SIG_INFO(
    CORINFO_SIG_INFO& sigInfo, LightWeightMap<key, value>* buffers)
{
    MethodContext::Agnostic_CORINFO_SIG_INFO sig(CreateAgnostic_CORINFO_SIG_INFO_without_buffers(sigInfo));
    sig.sigInst_classInst_Index =
        buffers->Contains((unsigned char*)sigInfo.sigInst.classInst, sigInfo.sigInst.classInstCount * 8);
    sig.sigInst_methInst_Index =
        buffers->Contains((unsigned char*)sigInfo.sigInst.methInst, sigInfo.sigInst.methInstCount * 8);
    sig.pSig_Index = (DWORD)buffers->Contains((unsigned char*)sigInfo.pSig, sigInfo.cbSig);
    return sig;
}

template <typename key, typename value>
inline CORINFO_SIG_INFO SpmiRecordsHelper::Restore_CORINFO_SIG_INFO(MethodContext::Agnostic_CORINFO_SIG_INFO& sigInfo,
                                                                    LightWeightMap<key, value>* buffers)
{
    CORINFO_SIG_INFO sig;
    sig.callConv               = (CorInfoCallConv)sigInfo.callConv;
    sig.retTypeClass           = (CORINFO_CLASS_HANDLE)sigInfo.retTypeClass;
    sig.retTypeSigClass        = (CORINFO_CLASS_HANDLE)sigInfo.retTypeSigClass;
    sig.retType                = (CorInfoType)sigInfo.retType;
    sig.flags                  = (unsigned)sigInfo.flags;
    sig.numArgs                = (unsigned)sigInfo.numArgs;
    sig.sigInst.classInstCount = (unsigned)sigInfo.sigInst_classInstCount;
    sig.sigInst.classInst      = (CORINFO_CLASS_HANDLE*)buffers->GetBuffer(sigInfo.sigInst_classInst_Index);
    sig.sigInst.methInstCount  = (unsigned)sigInfo.sigInst_methInstCount;
    sig.sigInst.methInst       = (CORINFO_CLASS_HANDLE*)buffers->GetBuffer(sigInfo.sigInst_methInst_Index);
    sig.args                   = (CORINFO_ARG_LIST_HANDLE)sigInfo.args;
    sig.cbSig                  = (unsigned int)sigInfo.cbSig;
    sig.pSig                   = (PCCOR_SIGNATURE)buffers->GetBuffer(sigInfo.pSig_Index);
    sig.scope                  = (CORINFO_MODULE_HANDLE)sigInfo.scope;
    sig.token                  = (mdToken)sigInfo.token;
    return sig;
}

#endif
