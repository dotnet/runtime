//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

//----------------------------------------------------------
// SpmiRecordHelper.h - Helpers to copy data between agnostic/non-agnostic types and dump them.
//----------------------------------------------------------

#include "methodcontext.h"

class SpmiRecordsHelper
{
public:
    static MethodContext::Agnostic_CORINFO_RESOLVED_TOKENin CreateAgnostic_CORINFO_RESOLVED_TOKENin
    (CORINFO_RESOLVED_TOKEN* pResolvedToken);

    static MethodContext::Agnostic_CORINFO_RESOLVED_TOKENout CreateAgnostic_CORINFO_RESOLVED_TOKENout_without_buffers
    (CORINFO_RESOLVED_TOKEN* pResolvedToken);

    template<typename key, typename value>
    static MethodContext::Agnostic_CORINFO_RESOLVED_TOKENout StoreAgnostic_CORINFO_RESOLVED_TOKENout
    (CORINFO_RESOLVED_TOKEN* pResolvedToken, LightWeightMap<key, value>* buffers);

    template<typename key, typename value>
    static MethodContext::Agnostic_CORINFO_RESOLVED_TOKENout RestoreAgnostic_CORINFO_RESOLVED_TOKENout
    (CORINFO_RESOLVED_TOKEN* pResolvedToken, LightWeightMap<key, value>* buffers);

    template<typename key, typename value>
    static MethodContext::Agnostic_CORINFO_RESOLVED_TOKEN StoreAgnostic_CORINFO_RESOLVED_TOKEN
    (CORINFO_RESOLVED_TOKEN* pResolvedToken, LightWeightMap<key, value>* buffers);

    template<typename key, typename value>
    static MethodContext::Agnostic_CORINFO_RESOLVED_TOKEN RestoreAgnostic_CORINFO_RESOLVED_TOKEN
    (CORINFO_RESOLVED_TOKEN* pResolvedToken, LightWeightMap<key, value>* buffers);
};

inline MethodContext::Agnostic_CORINFO_RESOLVED_TOKENin SpmiRecordsHelper::CreateAgnostic_CORINFO_RESOLVED_TOKENin(CORINFO_RESOLVED_TOKEN * pResolvedToken)
{
    MethodContext::Agnostic_CORINFO_RESOLVED_TOKENin tokenIn;
    ZeroMemory(&tokenIn, sizeof(tokenIn));
    tokenIn.tokenContext = (DWORDLONG)pResolvedToken->tokenContext;
    tokenIn.tokenScope = (DWORDLONG)pResolvedToken->tokenScope;
    tokenIn.token = (DWORD)pResolvedToken->token;
    tokenIn.tokenType = (DWORD)pResolvedToken->tokenType;
    return tokenIn;
}

inline MethodContext::Agnostic_CORINFO_RESOLVED_TOKENout SpmiRecordsHelper::CreateAgnostic_CORINFO_RESOLVED_TOKENout_without_buffers(CORINFO_RESOLVED_TOKEN * pResolvedToken)
{
    MethodContext::Agnostic_CORINFO_RESOLVED_TOKENout tokenOut;
    ZeroMemory(&tokenOut, sizeof(tokenOut));
    tokenOut.hClass = (DWORDLONG)pResolvedToken->hClass;
    tokenOut.hMethod = (DWORDLONG)pResolvedToken->hMethod;
    tokenOut.hField = (DWORDLONG)pResolvedToken->hField;

    tokenOut.cbTypeSpec = (DWORD)pResolvedToken->cbTypeSpec;
    tokenOut.cbMethodSpec = (DWORD)pResolvedToken->cbMethodSpec;

    tokenOut.pTypeSpec_Index = -1;
    tokenOut.pMethodSpec_Index = -1;

    return tokenOut;
}

template<typename key, typename value>
inline MethodContext::Agnostic_CORINFO_RESOLVED_TOKENout SpmiRecordsHelper::StoreAgnostic_CORINFO_RESOLVED_TOKENout(CORINFO_RESOLVED_TOKEN * pResolvedToken, LightWeightMap<key, value>* buffers)
{
    MethodContext::Agnostic_CORINFO_RESOLVED_TOKENout tokenOut(CreateAgnostic_CORINFO_RESOLVED_TOKENout_without_buffers(pResolvedToken));

    tokenOut.pTypeSpec_Index =
        (DWORD)buffers->AddBuffer((unsigned char*)pResolvedToken->pTypeSpec, pResolvedToken->cbTypeSpec);
    tokenOut.pMethodSpec_Index =
        (DWORD)buffers->AddBuffer((unsigned char*)pResolvedToken->pMethodSpec, pResolvedToken->cbMethodSpec);

    return tokenOut;
}

template<typename key, typename value>
inline MethodContext::Agnostic_CORINFO_RESOLVED_TOKENout SpmiRecordsHelper::RestoreAgnostic_CORINFO_RESOLVED_TOKENout(CORINFO_RESOLVED_TOKEN * pResolvedToken, LightWeightMap<key, value>* buffers)
{
    MethodContext::Agnostic_CORINFO_RESOLVED_TOKENout tokenOut(CreateAgnostic_CORINFO_RESOLVED_TOKENout_without_buffers(pResolvedToken));
    tokenOut.pTypeSpec_Index =
        (DWORD)buffers->Contains((unsigned char*)pResolvedToken->pTypeSpec, pResolvedToken->cbTypeSpec);
    tokenOut.pMethodSpec_Index =
        (DWORD)buffers->Contains((unsigned char*)pResolvedToken->pMethodSpec, pResolvedToken->cbMethodSpec);
    return tokenOut;
}

template<typename key, typename value>
inline MethodContext::Agnostic_CORINFO_RESOLVED_TOKEN SpmiRecordsHelper::StoreAgnostic_CORINFO_RESOLVED_TOKEN(CORINFO_RESOLVED_TOKEN * pResolvedToken, LightWeightMap<key, value>* buffers)
{
    MethodContext::Agnostic_CORINFO_RESOLVED_TOKEN token;
    token.inValue = CreateAgnostic_CORINFO_RESOLVED_TOKENin(pResolvedToken);
    token.outValue = StoreAgnostic_CORINFO_RESOLVED_TOKENout(pResolvedToken, buffers);
    return token;
}

template<typename key, typename value>
inline MethodContext::Agnostic_CORINFO_RESOLVED_TOKEN SpmiRecordsHelper::RestoreAgnostic_CORINFO_RESOLVED_TOKEN(CORINFO_RESOLVED_TOKEN * pResolvedToken, LightWeightMap<key, value>* buffers)
{
    MethodContext::Agnostic_CORINFO_RESOLVED_TOKEN token;
    ZeroMemory(&token, sizeof(token));
    token.inValue = CreateAgnostic_CORINFO_RESOLVED_TOKENin(pResolvedToken);
    token.outValue = RestoreAgnostic_CORINFO_RESOLVED_TOKENout(pResolvedToken, buffers);
    return token;
}
