//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#include "standardpch.h"
#include "icorjitcompiler.h"
#include "icorjitinfo.h"

interceptor_IEEMM* current_IEEMM = nullptr; // we want this to live beyond the scope of a single compileMethodCall

CorJitResult __stdcall interceptor_ICJC::compileMethod(ICorJitInfo*                comp,     /* IN */
                                                       struct CORINFO_METHOD_INFO* info,     /* IN */
                                                       unsigned /* code:CorJitFlag */ flags, /* IN */
                                                       BYTE** nativeEntry,                   /* OUT */
                                                       ULONG* nativeSizeOfCode               /* OUT */
                                                       )
{
    interceptor_ICJI our_ICorJitInfo;
    our_ICorJitInfo.original_ICorJitInfo = comp;

    if (current_IEEMM == nullptr)
        current_IEEMM   = new interceptor_IEEMM();
    our_ICorJitInfo.mcs = mcs;

    mcs->AddCall("compileMethod");
    CorJitResult temp =
        original_ICorJitCompiler->compileMethod(&our_ICorJitInfo, info, flags, nativeEntry, nativeSizeOfCode);

    return temp;
}

void interceptor_ICJC::clearCache()
{
    mcs->AddCall("clearCache");
    original_ICorJitCompiler->clearCache();
}

BOOL interceptor_ICJC::isCacheCleanupRequired()
{
    mcs->AddCall("isCacheCleanupRequired");
    return original_ICorJitCompiler->isCacheCleanupRequired();
}

void interceptor_ICJC::ProcessShutdownWork(ICorStaticInfo* info)
{
    mcs->AddCall("ProcessShutdownWork");
    original_ICorJitCompiler->ProcessShutdownWork(info);
}

void interceptor_ICJC::getVersionIdentifier(GUID* versionIdentifier /* OUT */)
{
    mcs->AddCall("getVersionIdentifier");
    original_ICorJitCompiler->getVersionIdentifier(versionIdentifier);
}

unsigned interceptor_ICJC::getMaxIntrinsicSIMDVectorLength(CORJIT_FLAGS cpuCompileFlags)
{
    mcs->AddCall("getMaxIntrinsicSIMDVectorLength");
    return original_ICorJitCompiler->getMaxIntrinsicSIMDVectorLength(cpuCompileFlags);
}

void interceptor_ICJC::setRealJit(ICorJitCompiler* realJitCompiler)
{
    mcs->AddCall("setRealJit");
    original_ICorJitCompiler->setRealJit(realJitCompiler);
}
