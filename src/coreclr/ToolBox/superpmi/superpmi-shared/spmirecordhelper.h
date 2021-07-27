// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//----------------------------------------------------------
// SpmiRecordHelper.h - a helper to copy data between agnostic/non-agnostic types.
//----------------------------------------------------------
#ifndef _SpmiRecordsHelper
#define _SpmiRecordsHelper

#include "methodcontext.h"
#include "spmiutil.h"

class SpmiRecordsHelper
{
public:
    static Agnostic_CORINFO_RESOLVED_TOKENin CreateAgnostic_CORINFO_RESOLVED_TOKENin(
        CORINFO_RESOLVED_TOKEN* pResolvedToken);

    static Agnostic_CORINFO_RESOLVED_TOKENout CreateAgnostic_CORINFO_RESOLVED_TOKENout_without_buffers(
        CORINFO_RESOLVED_TOKEN* pResolvedToken);

    template <typename key, typename value>
    static Agnostic_CORINFO_RESOLVED_TOKENout StoreAgnostic_CORINFO_RESOLVED_TOKENout(
        CORINFO_RESOLVED_TOKEN* pResolvedToken, LightWeightMap<key, value>* buffers);

    template <typename key, typename value>
    static Agnostic_CORINFO_RESOLVED_TOKENout RestoreAgnostic_CORINFO_RESOLVED_TOKENout(
        CORINFO_RESOLVED_TOKEN* pResolvedToken, LightWeightMap<key, value>* buffers);

    template <typename key, typename value>
    static Agnostic_CORINFO_RESOLVED_TOKEN StoreAgnostic_CORINFO_RESOLVED_TOKEN(
        CORINFO_RESOLVED_TOKEN* pResolvedToken, LightWeightMap<key, value>* buffers);

    template <typename key, typename value>
    static Agnostic_CORINFO_RESOLVED_TOKEN RestoreAgnostic_CORINFO_RESOLVED_TOKEN(
        CORINFO_RESOLVED_TOKEN* pResolvedToken, LightWeightMap<key, value>* buffers);

    template <typename key, typename value>
    static CORINFO_RESOLVED_TOKEN Restore_CORINFO_RESOLVED_TOKEN(
        Agnostic_CORINFO_RESOLVED_TOKEN* pResolvedTokenAgnostic, LightWeightMap<key, value>* buffers);

    // Restore the out values in the first argument from the second.
    // Can't just return whole CORINFO_RESOLVED_TOKEN because [in] values in it are important too.
    template <typename key, typename value>
    static void Restore_CORINFO_RESOLVED_TOKENout(CORINFO_RESOLVED_TOKEN*                            pResolvedToken,
                                                  Agnostic_CORINFO_RESOLVED_TOKENout& token,
                                                  LightWeightMap<key, value>* buffers);

    static Agnostic_CORINFO_SIG_INFO CreateAgnostic_CORINFO_SIG_INFO_without_buffers(
        const CORINFO_SIG_INFO& sigInfo);

    static void StoreAgnostic_CORINFO_SIG_INST_HandleArray(
        unsigned handleInstCount,
        CORINFO_CLASS_HANDLE* handleInstArray,
        DenseLightWeightMap<DWORDLONG>*& handleMap, // If we initialize it, the pointer gets updated.
        /* OUT */ DWORD* handleInstCountOut,
        /* OUT */ DWORD* handleInstIndexOut);

    template <typename key, typename value>
    static Agnostic_CORINFO_SIG_INFO StoreAgnostic_CORINFO_SIG_INFO(
        const CORINFO_SIG_INFO& sigInfo,
        LightWeightMap<key, value>* buffers,
        DenseLightWeightMap<DWORDLONG>*& handleMap);

    static DWORD ContainsHandleMap(
        unsigned handleCount,
        const CORINFO_CLASS_HANDLE* handleArray,
        const DenseLightWeightMap<DWORDLONG>* handleMap);

    template <typename key, typename value>
    static Agnostic_CORINFO_SIG_INFO RestoreAgnostic_CORINFO_SIG_INFO(
        const CORINFO_SIG_INFO& sigInfo,
        LightWeightMap<key, value>* buffers,
        const DenseLightWeightMap<DWORDLONG>* handleMap);

    static void DeserializeCORINFO_SIG_INST_HandleArray(
        DWORD handleInstCount,
        DWORD handleInstIndex,
        const DenseLightWeightMap<DWORDLONG>* handleMap,
        /* OUT */ unsigned* handleInstCountOut,
        /* OUT */ CORINFO_CLASS_HANDLE** handleInstArrayOut);

    static void DeserializeCORINFO_SIG_INST(
        CORINFO_SIG_INFO& sigInfoOut,
        const Agnostic_CORINFO_SIG_INFO& sigInfo,
        const DenseLightWeightMap<DWORDLONG>* handleMap);

    template <typename key, typename value>
    static CORINFO_SIG_INFO Restore_CORINFO_SIG_INFO(
        const Agnostic_CORINFO_SIG_INFO& sigInfo,
        LightWeightMap<key, value>* buffers,
        const DenseLightWeightMap<DWORDLONG>* handleMap);

    static Agnostic_CORINFO_LOOKUP_KIND CreateAgnostic_CORINFO_LOOKUP_KIND(
        const CORINFO_LOOKUP_KIND* pGenericLookupKind);

    static CORINFO_LOOKUP_KIND RestoreCORINFO_LOOKUP_KIND(Agnostic_CORINFO_LOOKUP_KIND& lookupKind);

    static Agnostic_CORINFO_CONST_LOOKUP StoreAgnostic_CORINFO_CONST_LOOKUP(
        CORINFO_CONST_LOOKUP* pLookup);

    static CORINFO_CONST_LOOKUP RestoreCORINFO_CONST_LOOKUP(Agnostic_CORINFO_CONST_LOOKUP& lookup);

    static Agnostic_CORINFO_RUNTIME_LOOKUP StoreAgnostic_CORINFO_RUNTIME_LOOKUP(
        CORINFO_RUNTIME_LOOKUP* pLookup);

    static CORINFO_RUNTIME_LOOKUP RestoreCORINFO_RUNTIME_LOOKUP(Agnostic_CORINFO_RUNTIME_LOOKUP& Lookup);

    static Agnostic_CORINFO_LOOKUP StoreAgnostic_CORINFO_LOOKUP(CORINFO_LOOKUP* pLookup);

    static CORINFO_LOOKUP RestoreCORINFO_LOOKUP(Agnostic_CORINFO_LOOKUP& agnosticLookup);
};

inline Agnostic_CORINFO_RESOLVED_TOKENin SpmiRecordsHelper::CreateAgnostic_CORINFO_RESOLVED_TOKENin(
    CORINFO_RESOLVED_TOKEN* pResolvedToken)
{
    Agnostic_CORINFO_RESOLVED_TOKENin tokenIn;
    ZeroMemory(&tokenIn, sizeof(tokenIn));

    if (pResolvedToken != nullptr)
    {
        tokenIn.tokenContext = CastHandle(pResolvedToken->tokenContext);
        tokenIn.tokenScope   = CastHandle(pResolvedToken->tokenScope);
        tokenIn.token        = (DWORD)pResolvedToken->token;
        tokenIn.tokenType    = (DWORD)pResolvedToken->tokenType;
    }
    return tokenIn;
}

inline Agnostic_CORINFO_RESOLVED_TOKENout SpmiRecordsHelper::
    CreateAgnostic_CORINFO_RESOLVED_TOKENout_without_buffers(CORINFO_RESOLVED_TOKEN* pResolvedToken)
{
    Agnostic_CORINFO_RESOLVED_TOKENout tokenOut;
    ZeroMemory(&tokenOut, sizeof(tokenOut));

    if (pResolvedToken != nullptr)
    {
        tokenOut.hClass  = CastHandle(pResolvedToken->hClass);
        tokenOut.hMethod = CastHandle(pResolvedToken->hMethod);
        tokenOut.hField  = CastHandle(pResolvedToken->hField);

        tokenOut.cbTypeSpec   = (DWORD)pResolvedToken->cbTypeSpec;
        tokenOut.cbMethodSpec = (DWORD)pResolvedToken->cbMethodSpec;
    }

    tokenOut.pTypeSpec_Index   = -1;
    tokenOut.pMethodSpec_Index = -1;

    return tokenOut;
}

template <typename key, typename value>
inline Agnostic_CORINFO_RESOLVED_TOKENout SpmiRecordsHelper::StoreAgnostic_CORINFO_RESOLVED_TOKENout(
    CORINFO_RESOLVED_TOKEN* pResolvedToken, LightWeightMap<key, value>* buffers)
{
    Agnostic_CORINFO_RESOLVED_TOKENout tokenOut(
        CreateAgnostic_CORINFO_RESOLVED_TOKENout_without_buffers(pResolvedToken));

    if (pResolvedToken != nullptr)
    {
        tokenOut.pTypeSpec_Index =
            (DWORD)buffers->AddBuffer((unsigned char*)pResolvedToken->pTypeSpec, pResolvedToken->cbTypeSpec);
        tokenOut.pMethodSpec_Index =
            (DWORD)buffers->AddBuffer((unsigned char*)pResolvedToken->pMethodSpec, pResolvedToken->cbMethodSpec);
    }

    return tokenOut;
}

template <typename key, typename value>
inline Agnostic_CORINFO_RESOLVED_TOKENout SpmiRecordsHelper::RestoreAgnostic_CORINFO_RESOLVED_TOKENout(
    CORINFO_RESOLVED_TOKEN* pResolvedToken, LightWeightMap<key, value>* buffers)
{
    Agnostic_CORINFO_RESOLVED_TOKENout tokenOut(
        CreateAgnostic_CORINFO_RESOLVED_TOKENout_without_buffers(pResolvedToken));
    if (pResolvedToken != nullptr)
    {
        tokenOut.pTypeSpec_Index =
            (DWORD)buffers->Contains((unsigned char*)pResolvedToken->pTypeSpec, pResolvedToken->cbTypeSpec);
        tokenOut.pMethodSpec_Index =
            (DWORD)buffers->Contains((unsigned char*)pResolvedToken->pMethodSpec, pResolvedToken->cbMethodSpec);
    }
    return tokenOut;
}

template <typename key, typename value>
inline Agnostic_CORINFO_RESOLVED_TOKEN SpmiRecordsHelper::StoreAgnostic_CORINFO_RESOLVED_TOKEN(
    CORINFO_RESOLVED_TOKEN* pResolvedToken, LightWeightMap<key, value>* buffers)
{
    Agnostic_CORINFO_RESOLVED_TOKEN token;
    token.inValue  = CreateAgnostic_CORINFO_RESOLVED_TOKENin(pResolvedToken);
    token.outValue = StoreAgnostic_CORINFO_RESOLVED_TOKENout(pResolvedToken, buffers);
    return token;
}

template <typename key, typename value>
inline Agnostic_CORINFO_RESOLVED_TOKEN SpmiRecordsHelper::RestoreAgnostic_CORINFO_RESOLVED_TOKEN(
    CORINFO_RESOLVED_TOKEN* pResolvedToken, LightWeightMap<key, value>* buffers)
{
    Agnostic_CORINFO_RESOLVED_TOKEN token;
    ZeroMemory(&token, sizeof(token));
    token.inValue  = CreateAgnostic_CORINFO_RESOLVED_TOKENin(pResolvedToken);
    token.outValue = RestoreAgnostic_CORINFO_RESOLVED_TOKENout(pResolvedToken, buffers);
    return token;
}

template <typename key, typename value>
inline CORINFO_RESOLVED_TOKEN SpmiRecordsHelper::Restore_CORINFO_RESOLVED_TOKEN(
    Agnostic_CORINFO_RESOLVED_TOKEN* pResolvedTokenAgnostic, LightWeightMap<key, value>* buffers)
{
    CORINFO_RESOLVED_TOKEN token;
    ZeroMemory(&token, sizeof(token));

    token.tokenContext = (CORINFO_CONTEXT_HANDLE)pResolvedTokenAgnostic->inValue.tokenContext;
    token.tokenScope   = (CORINFO_MODULE_HANDLE)pResolvedTokenAgnostic->inValue.tokenScope;
    token.token        = (mdToken)pResolvedTokenAgnostic->inValue.token;
    token.tokenType    = (CorInfoTokenKind)pResolvedTokenAgnostic->inValue.tokenType;

    Restore_CORINFO_RESOLVED_TOKENout(&token, pResolvedTokenAgnostic->outValue, buffers);
    return token;
}

template <typename key, typename value>
inline void SpmiRecordsHelper::Restore_CORINFO_RESOLVED_TOKENout(
    CORINFO_RESOLVED_TOKEN*                            pResolvedToken,
    Agnostic_CORINFO_RESOLVED_TOKENout& tokenOut,
    LightWeightMap<key, value>* buffers)
{
    if (pResolvedToken != nullptr)
    {
        pResolvedToken->hClass       = (CORINFO_CLASS_HANDLE)tokenOut.hClass;
        pResolvedToken->hMethod      = (CORINFO_METHOD_HANDLE)tokenOut.hMethod;
        pResolvedToken->hField       = (CORINFO_FIELD_HANDLE)tokenOut.hField;
        pResolvedToken->pTypeSpec    = (PCCOR_SIGNATURE)buffers->GetBuffer(tokenOut.pTypeSpec_Index);
        pResolvedToken->cbTypeSpec   = (ULONG)tokenOut.cbTypeSpec;
        pResolvedToken->pMethodSpec  = (PCCOR_SIGNATURE)buffers->GetBuffer(tokenOut.pMethodSpec_Index);
        pResolvedToken->cbMethodSpec = (ULONG)tokenOut.cbMethodSpec;
    }
}

inline Agnostic_CORINFO_SIG_INFO SpmiRecordsHelper::CreateAgnostic_CORINFO_SIG_INFO_without_buffers(
    const CORINFO_SIG_INFO& sigInfo)
{
    Agnostic_CORINFO_SIG_INFO sig;
    ZeroMemory(&sig, sizeof(sig));
    sig.callConv               = (DWORD)sigInfo.callConv;
    sig.retTypeClass           = CastHandle(sigInfo.retTypeClass);
    sig.retTypeSigClass        = CastHandle(sigInfo.retTypeSigClass);
    sig.retType                = (DWORD)sigInfo.retType;
    sig.flags                  = (DWORD)sigInfo.flags;
    sig.numArgs                = (DWORD)sigInfo.numArgs;
    sig.sigInst_classInstCount = (DWORD)sigInfo.sigInst.classInstCount;
    sig.sigInst_methInstCount  = (DWORD)sigInfo.sigInst.methInstCount;
    sig.args                   = CastHandle(sigInfo.args);
    sig.cbSig                  = (DWORD)sigInfo.cbSig;
    sig.methodSignature        = CastPointer(sigInfo.methodSignature);
    sig.scope                  = CastHandle(sigInfo.scope);
    sig.token                  = (DWORD)sigInfo.token;
    return sig;
}

inline void SpmiRecordsHelper::StoreAgnostic_CORINFO_SIG_INST_HandleArray(
    unsigned handleInstCount,
    CORINFO_CLASS_HANDLE* handleInstArray,
    DenseLightWeightMap<DWORDLONG>*& handleMap, // If we initialize it, the pointer gets updated.
    /* OUT */ DWORD* handleInstCountOut,
    /* OUT */ DWORD* handleInstIndexOut)
{
    unsigned handleInstIndex;

    // We shouldn't need to check (handleInstArray != nullptr), but often, crossgen2 sets (leaves?)
    // handleInstCount > 0 and handleInstArray == nullptr.
    if ((handleInstCount > 0) && (handleInstArray != nullptr))
    {
        if (handleMap == nullptr)
            handleMap = new DenseLightWeightMap<DWORDLONG>(); // this updates the caller

        // If the we already have this array, we need to re-use it, so when we look it up on
        // replay we get the same index. It also reduces the amount of data in the handle map,
        // since there are typically few handle unique arrays.
        handleInstIndex = ContainsHandleMap(handleInstCount, handleInstArray, handleMap);
        if (handleInstIndex == (DWORD)-1)
        {
            handleInstIndex = handleMap->GetCount();
            for (unsigned int i = 0; i < handleInstCount; i++)
            {
                handleMap->Append(CastHandle(handleInstArray[i]));
            }
            AssertCodeMsg(handleInstIndex + handleInstCount == handleMap->GetCount(), EXCEPTIONCODE_MC,
                "Unexpected size of handleMap");
        }
    }
    else
    {
        if (handleInstCount > 0)
        {
            // This is really a VM/crossgen2 bug (or, a by-design crossgen2 feature to avoid creating
            // an array the JIT doesn't look at).
            handleInstCount = 0;
        }

        handleInstIndex = (DWORD)-1;
    }

    *handleInstCountOut = handleInstCount;
    *handleInstIndexOut = handleInstIndex;
}

template <typename key, typename value>
inline Agnostic_CORINFO_SIG_INFO SpmiRecordsHelper::StoreAgnostic_CORINFO_SIG_INFO(
    const CORINFO_SIG_INFO& sigInfo,
    LightWeightMap<key, value>* buffers,
    DenseLightWeightMap<DWORDLONG>*& handleMap)
{
    Agnostic_CORINFO_SIG_INFO sig(CreateAgnostic_CORINFO_SIG_INFO_without_buffers(sigInfo));

    StoreAgnostic_CORINFO_SIG_INST_HandleArray(sigInfo.sigInst.classInstCount, sigInfo.sigInst.classInst, handleMap, &sig.sigInst_classInstCount, &sig.sigInst_classInst_Index);
    StoreAgnostic_CORINFO_SIG_INST_HandleArray(sigInfo.sigInst.methInstCount, sigInfo.sigInst.methInst, handleMap, &sig.sigInst_methInstCount, &sig.sigInst_methInst_Index);
    sig.pSig_Index = (DWORD)buffers->AddBuffer((unsigned char*)sigInfo.pSig, sigInfo.cbSig);

    return sig;
}

inline DWORD SpmiRecordsHelper::ContainsHandleMap(
    unsigned handleCount,
    const CORINFO_CLASS_HANDLE* handleArray,
    const DenseLightWeightMap<DWORDLONG>* handleMap)
{
    // Special case: no handles isn't found.
    if (handleCount == 0)
    {
        return (DWORD)-1;
    }

    for (unsigned mapIndex = 0; mapIndex < handleMap->GetCount(); mapIndex++)
    {
        if (mapIndex + handleCount > handleMap->GetCount())
        {
            // The handles can't be found; it would overflow the handleMap.
            return (DWORD)-1;
        }

        // See if the handle map starting at `mapIndex` contains all the handles we're looking for, in order.
        bool found = true; // Assume we'll find it
        for (unsigned handleIndex = 0; handleIndex < handleCount; handleIndex++)
        {
            if (handleMap->Get(mapIndex + handleIndex) != CastHandle(handleArray[handleIndex]))
            {
                found = false;
                break;
            }
        }
        if (found)
        {
            return (DWORD)mapIndex;
        }
    }
    return (DWORD)-1; // not found
}

// RestoreAgnostic_CORINFO_SIG_INFO: from a CORINFO_SIG_INFO, fill out an Agnostic_CORINFO_SIG_INFO from the data already stored.
// Don't create and store a new Agnostic_CORINFO_SIG_INFO. The buffers and indices need to be found in the existing stored data.
// This is used when we need to create an Agnostic_CORINFO_SIG_INFO to be used as a key to look up in an existing map.
template <typename key, typename value>
inline Agnostic_CORINFO_SIG_INFO SpmiRecordsHelper::RestoreAgnostic_CORINFO_SIG_INFO(
    const CORINFO_SIG_INFO& sigInfo,
    LightWeightMap<key, value>* buffers,
    const DenseLightWeightMap<DWORDLONG>* handleMap)
{
    Agnostic_CORINFO_SIG_INFO sig(CreateAgnostic_CORINFO_SIG_INFO_without_buffers(sigInfo));
    sig.sigInst_classInst_Index = ContainsHandleMap(sigInfo.sigInst.classInstCount, sigInfo.sigInst.classInst, handleMap);
    sig.sigInst_methInst_Index  = ContainsHandleMap(sigInfo.sigInst.methInstCount, sigInfo.sigInst.methInst, handleMap);
    sig.pSig_Index = (DWORD)buffers->Contains((unsigned char*)sigInfo.pSig, sigInfo.cbSig);
    return sig;
}

inline void SpmiRecordsHelper::DeserializeCORINFO_SIG_INST_HandleArray(
    DWORD handleInstCount,
    DWORD handleInstIndex,
    const DenseLightWeightMap<DWORDLONG>* handleMap,
    /* OUT */ unsigned* handleInstCountOut,
    /* OUT */ CORINFO_CLASS_HANDLE** handleInstArrayOut)
{
    CORINFO_CLASS_HANDLE* handleInstArray;

    if (handleInstCount > 0)
    {
        handleInstArray = new CORINFO_CLASS_HANDLE[handleInstCount]; // memory leak?
        for (unsigned int i = 0; i < handleInstCount; i++)
        {
            DWORD key = handleInstIndex + i;
            handleInstArray[i] = (CORINFO_CLASS_HANDLE)handleMap->Get(key);
        }
    }
    else
    {
        handleInstArray = nullptr;
    }

    *handleInstCountOut = handleInstCount;
    *handleInstArrayOut = handleInstArray;
}

inline void SpmiRecordsHelper::DeserializeCORINFO_SIG_INST(
    CORINFO_SIG_INFO& sigInfoOut, const Agnostic_CORINFO_SIG_INFO& sigInfo, const DenseLightWeightMap<DWORDLONG>* handleMap)
{
    DeserializeCORINFO_SIG_INST_HandleArray(sigInfo.sigInst_classInstCount, sigInfo.sigInst_classInst_Index, handleMap, &sigInfoOut.sigInst.classInstCount, &sigInfoOut.sigInst.classInst);
    DeserializeCORINFO_SIG_INST_HandleArray(sigInfo.sigInst_methInstCount, sigInfo.sigInst_methInst_Index, handleMap, &sigInfoOut.sigInst.methInstCount, &sigInfoOut.sigInst.methInst);
}

template <typename key, typename value>
inline CORINFO_SIG_INFO SpmiRecordsHelper::Restore_CORINFO_SIG_INFO(const Agnostic_CORINFO_SIG_INFO& sigInfo,
                                                                    LightWeightMap<key, value>* buffers,
                                                                    const DenseLightWeightMap<DWORDLONG>* handleMap)
{
    CORINFO_SIG_INFO sig;
    sig.callConv        = (CorInfoCallConv)sigInfo.callConv;
    sig.retTypeClass    = (CORINFO_CLASS_HANDLE)sigInfo.retTypeClass;
    sig.retTypeSigClass = (CORINFO_CLASS_HANDLE)sigInfo.retTypeSigClass;
    sig.retType         = (CorInfoType)sigInfo.retType;
    sig.flags           = (unsigned)sigInfo.flags;
    sig.numArgs         = (unsigned)sigInfo.numArgs;
    sig.args            = (CORINFO_ARG_LIST_HANDLE)sigInfo.args;
    sig.cbSig           = (unsigned int)sigInfo.cbSig;
    sig.pSig            = (PCCOR_SIGNATURE)buffers->GetBuffer(sigInfo.pSig_Index);
    sig.methodSignature = (MethodSignatureInfo*)sigInfo.methodSignature;
    sig.scope           = (CORINFO_MODULE_HANDLE)sigInfo.scope;
    sig.token           = (mdToken)sigInfo.token;

    DeserializeCORINFO_SIG_INST(sig, sigInfo, handleMap);

    return sig;
}

inline Agnostic_CORINFO_LOOKUP_KIND SpmiRecordsHelper::CreateAgnostic_CORINFO_LOOKUP_KIND(
    const CORINFO_LOOKUP_KIND* pGenericLookupKind)
{
    Agnostic_CORINFO_LOOKUP_KIND genericLookupKind;
    ZeroMemory(&genericLookupKind, sizeof(genericLookupKind));
    if (pGenericLookupKind != nullptr)
    {
        genericLookupKind.needsRuntimeLookup = (DWORD)pGenericLookupKind->needsRuntimeLookup;
        genericLookupKind.runtimeLookupKind  = (DWORD)pGenericLookupKind->runtimeLookupKind;
        genericLookupKind.runtimeLookupFlags = pGenericLookupKind->runtimeLookupFlags;
    }
    // We don't store result->runtimeLookupArgs, which is opaque data. Ok?
    return genericLookupKind;
}

inline CORINFO_LOOKUP_KIND SpmiRecordsHelper::RestoreCORINFO_LOOKUP_KIND(
    Agnostic_CORINFO_LOOKUP_KIND& lookupKind)
{
    CORINFO_LOOKUP_KIND genericLookupKind;
    genericLookupKind.needsRuntimeLookup = lookupKind.needsRuntimeLookup != 0;
    genericLookupKind.runtimeLookupKind  = (CORINFO_RUNTIME_LOOKUP_KIND)lookupKind.runtimeLookupKind;
    genericLookupKind.runtimeLookupFlags = lookupKind.runtimeLookupFlags;
    genericLookupKind.runtimeLookupArgs  = nullptr; // We don't store this opaque data. Ok?
    return genericLookupKind;
}

inline Agnostic_CORINFO_CONST_LOOKUP SpmiRecordsHelper::StoreAgnostic_CORINFO_CONST_LOOKUP(
    CORINFO_CONST_LOOKUP* pLookup)
{
    Agnostic_CORINFO_CONST_LOOKUP constLookup;
    ZeroMemory(&constLookup, sizeof(constLookup));
    constLookup.accessType = (DWORD)pLookup->accessType;
    constLookup.handle     = CastHandle(pLookup->handle);
    return constLookup;
}

inline CORINFO_CONST_LOOKUP SpmiRecordsHelper::RestoreCORINFO_CONST_LOOKUP(
    Agnostic_CORINFO_CONST_LOOKUP& lookup)
{
    CORINFO_CONST_LOOKUP constLookup;
    constLookup.accessType = (InfoAccessType)lookup.accessType;
    constLookup.handle     = (CORINFO_GENERIC_HANDLE)lookup.handle;
    return constLookup;
}

inline Agnostic_CORINFO_RUNTIME_LOOKUP SpmiRecordsHelper::StoreAgnostic_CORINFO_RUNTIME_LOOKUP(
    CORINFO_RUNTIME_LOOKUP* pLookup)
{
    Agnostic_CORINFO_RUNTIME_LOOKUP runtimeLookup;
    ZeroMemory(&runtimeLookup, sizeof(runtimeLookup));
    runtimeLookup.signature            = CastPointer(pLookup->signature);
    runtimeLookup.helper               = (DWORD)pLookup->helper;
    runtimeLookup.indirections         = (DWORD)pLookup->indirections;
    runtimeLookup.testForNull          = (DWORD)pLookup->testForNull;
    runtimeLookup.testForFixup         = (DWORD)pLookup->testForFixup;
    runtimeLookup.sizeOffset           = pLookup->sizeOffset;
    runtimeLookup.indirectFirstOffset  = (DWORD)pLookup->indirectFirstOffset;
    runtimeLookup.indirectSecondOffset = (DWORD)pLookup->indirectSecondOffset;
    for (int i = 0; i < CORINFO_MAXINDIRECTIONS; i++)
        runtimeLookup.offsets[i] = (DWORDLONG)pLookup->offsets[i];
    return runtimeLookup;
}

inline CORINFO_RUNTIME_LOOKUP SpmiRecordsHelper::RestoreCORINFO_RUNTIME_LOOKUP(
    Agnostic_CORINFO_RUNTIME_LOOKUP& lookup)
{
    CORINFO_RUNTIME_LOOKUP runtimeLookup;
    runtimeLookup.signature            = (LPVOID)lookup.signature;
    runtimeLookup.helper               = (CorInfoHelpFunc)lookup.helper;
    runtimeLookup.indirections         = (WORD)lookup.indirections;
    runtimeLookup.testForNull          = lookup.testForNull != 0;
    runtimeLookup.testForFixup         = lookup.testForFixup != 0;
    runtimeLookup.sizeOffset           = lookup.sizeOffset;
    runtimeLookup.indirectFirstOffset  = lookup.indirectFirstOffset != 0;
    runtimeLookup.indirectSecondOffset = lookup.indirectSecondOffset != 0;
    for (int i                   = 0; i < CORINFO_MAXINDIRECTIONS; i++)
        runtimeLookup.offsets[i] = (size_t)lookup.offsets[i];
    return CORINFO_RUNTIME_LOOKUP();
}

inline Agnostic_CORINFO_LOOKUP SpmiRecordsHelper::StoreAgnostic_CORINFO_LOOKUP(CORINFO_LOOKUP* pLookup)
{
    Agnostic_CORINFO_LOOKUP lookup;
    ZeroMemory(&lookup, sizeof(lookup));
    lookup.lookupKind = CreateAgnostic_CORINFO_LOOKUP_KIND(&pLookup->lookupKind);
    if (pLookup->lookupKind.needsRuntimeLookup)
    {
        lookup.runtimeLookup = StoreAgnostic_CORINFO_RUNTIME_LOOKUP(&pLookup->runtimeLookup);
    }
    else
    {
        lookup.constLookup = StoreAgnostic_CORINFO_CONST_LOOKUP(&pLookup->constLookup);
    }
    return lookup;
}

inline CORINFO_LOOKUP SpmiRecordsHelper::RestoreCORINFO_LOOKUP(Agnostic_CORINFO_LOOKUP& agnosticLookup)
{
    CORINFO_LOOKUP lookup;
    ZeroMemory(&lookup, sizeof(lookup));
    lookup.lookupKind = RestoreCORINFO_LOOKUP_KIND(agnosticLookup.lookupKind);
    if (lookup.lookupKind.needsRuntimeLookup)
    {
        lookup.runtimeLookup = RestoreCORINFO_RUNTIME_LOOKUP(agnosticLookup.runtimeLookup);
    }
    else
    {
        lookup.constLookup = RestoreCORINFO_CONST_LOOKUP(agnosticLookup.constLookup);
    }
    return lookup;
}

#endif
