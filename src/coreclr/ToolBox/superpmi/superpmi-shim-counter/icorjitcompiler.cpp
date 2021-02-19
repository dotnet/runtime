// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "standardpch.h"
#include "icorjitcompiler.h"
#include "icorjitinfo.h"

CorJitResult interceptor_ICJC::compileMethod(ICorJitInfo*                comp,            /* IN */
                                             struct CORINFO_METHOD_INFO* info,            /* IN */
                                             unsigned /* code:CorJitFlag */ flags,        /* IN */
                                             uint8_t**                   nativeEntry,     /* OUT */
                                             uint32_t*                   nativeSizeOfCode /* OUT */
                                             )
{
    interceptor_ICJI our_ICorJitInfo;
    our_ICorJitInfo.original_ICorJitInfo = comp;

    our_ICorJitInfo.mcs = mcs;

    mcs->AddCall("compileMethod");
    CorJitResult temp =
        original_ICorJitCompiler->compileMethod(&our_ICorJitInfo, info, flags, nativeEntry, nativeSizeOfCode);

    return temp;
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
