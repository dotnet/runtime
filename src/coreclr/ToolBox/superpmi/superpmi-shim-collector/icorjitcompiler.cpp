// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "standardpch.h"
#include "spmiutil.h"
#include "icorjitcompiler.h"
#include "icorjitinfo.h"
#include "jithost.h"
#include "superpmi-shim-collector.h"

#define fatMC                               // this is nice to have on so ildump works...

CorJitResult interceptor_ICJC::compileMethod(ICorJitInfo*                comp,     /* IN */
                                             struct CORINFO_METHOD_INFO* info,     /* IN */
                                             unsigned /* code:CorJitFlag */ flags, /* IN */
                                             uint8_t** nativeEntry,                /* OUT */
                                             uint32_t* nativeSizeOfCode            /* OUT */
                                             )
{
    interceptor_ICJI our_ICorJitInfo;
    our_ICorJitInfo.original_ICorJitInfo = comp;

    auto* mc = new MethodContext();
    if (g_ourJitHost != nullptr)
    {
        g_ourJitHost->setMethodContext(mc);
    }

    our_ICorJitInfo.mc = mc;
    our_ICorJitInfo.mc->cr->recProcessName(GetCommandLineA());

    our_ICorJitInfo.mc->recCompileMethod(info, flags);

    // force some extra data into our tables..
    // data probably not needed with RyuJIT, but needed in 4.5 and 4.5.1 to help with catching cached values
    our_ICorJitInfo.getBuiltinClass(CLASSID_SYSTEM_OBJECT);
    our_ICorJitInfo.getBuiltinClass(CLASSID_TYPED_BYREF);
    our_ICorJitInfo.getBuiltinClass(CLASSID_TYPE_HANDLE);
    our_ICorJitInfo.getBuiltinClass(CLASSID_FIELD_HANDLE);
    our_ICorJitInfo.getBuiltinClass(CLASSID_METHOD_HANDLE);
    our_ICorJitInfo.getBuiltinClass(CLASSID_STRING);
    our_ICorJitInfo.getBuiltinClass(CLASSID_RUNTIME_TYPE);

#ifdef fatMC
    // to build up a fat mc
    CORINFO_CLASS_HANDLE ourClass = our_ICorJitInfo.getMethodClass(info->ftn);
    our_ICorJitInfo.getClassAttribs(ourClass);
    our_ICorJitInfo.getClassName(ourClass);
    our_ICorJitInfo.isValueClass(ourClass);
    our_ICorJitInfo.asCorInfoType(ourClass);

    const char* className = nullptr;
    our_ICorJitInfo.getMethodName(info->ftn, &className);
#endif

    // Record data from the global context, if any
    if (g_globalContext != nullptr)
    {
        our_ICorJitInfo.mc->recGlobalContext(*g_globalContext);
    }

    CorJitResult temp =
        original_ICorJitCompiler->compileMethod(&our_ICorJitInfo, info, flags, nativeEntry, nativeSizeOfCode);

    if (temp == CORJIT_OK)
    {
        // capture the results of compilation
        our_ICorJitInfo.mc->cr->recCompileMethod(nativeEntry, nativeSizeOfCode, temp);

        our_ICorJitInfo.mc->cr->recAllocMemCapture();
        our_ICorJitInfo.mc->cr->recAllocGCInfoCapture();
        our_ICorJitInfo.mc->saveToFile(hFile);
    }

    delete mc;

    if (g_ourJitHost != nullptr)
    {
        g_ourJitHost->setMethodContext(g_globalContext);
    }

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
