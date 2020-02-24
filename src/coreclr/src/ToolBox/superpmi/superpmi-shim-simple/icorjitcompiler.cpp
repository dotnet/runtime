//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#include "standardpch.h"
#include "icorjitcompiler.h"
#include "icorjitinfo.h"

CorJitResult __stdcall interceptor_ICJC::compileMethod(ICorJitInfo*                comp,     /* IN */
                                                       struct CORINFO_METHOD_INFO* info,     /* IN */
                                                       unsigned /* code:CorJitFlag */ flags, /* IN */
                                                       BYTE** nativeEntry,                   /* OUT */
                                                       ULONG* nativeSizeOfCode               /* OUT */
                                                       )
{
    interceptor_ICJI our_ICorJitInfo;
    our_ICorJitInfo.original_ICorJitInfo = comp;

    CorJitResult temp =
        original_ICorJitCompiler->compileMethod(&our_ICorJitInfo, info, flags, nativeEntry, nativeSizeOfCode);

    return temp;
}

void interceptor_ICJC::ProcessShutdownWork(ICorStaticInfo* info)
{
    original_ICorJitCompiler->ProcessShutdownWork(info);
}

void interceptor_ICJC::getVersionIdentifier(GUID* versionIdentifier /* OUT */)
{
    original_ICorJitCompiler->getVersionIdentifier(versionIdentifier);
}

unsigned interceptor_ICJC::getMaxIntrinsicSIMDVectorLength(CORJIT_FLAGS cpuCompileFlags)
{
    return original_ICorJitCompiler->getMaxIntrinsicSIMDVectorLength(cpuCompileFlags);
}
