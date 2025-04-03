// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include <stddef.h>
#include "corjit.h"

#include "interpreter.h"
#include "eeinterp.h"

#include <string.h>
#include <stdio.h>

#ifdef _MSC_VER
#define INTERP_API
#else
#define INTERP_API __attribute__ ((visibility ("default")))
#endif // _MSC_VER

/*****************************************************************************/
ICorJitHost* g_interpHost        = nullptr;
bool         g_interpInitialized = false;
/*****************************************************************************/
extern "C" INTERP_API void jitStartup(ICorJitHost* jitHost)
{
    if (g_interpInitialized)
    {
        return;
    }
    g_interpHost = jitHost;
    // TODO Interp intialization
    g_interpInitialized = true;
}
/*****************************************************************************/
static CILInterp g_CILInterp;
extern "C" INTERP_API ICorJitCompiler* getJit()
{
    if (!g_interpInitialized)
    {
        return nullptr;
    }
    return &g_CILInterp;
}


static CORINFO_MODULE_HANDLE g_interpModule = NULL;

//****************************************************************************
CorJitResult CILInterp::compileMethod(ICorJitInfo*         compHnd,
                                   CORINFO_METHOD_INFO* methodInfo,
                                   unsigned             flags,
                                   uint8_t**            entryAddress,
                                   uint32_t*            nativeSizeOfCode)
{

    bool doInterpret;

    if (g_interpModule != NULL)
    {
        if (methodInfo->scope == g_interpModule)
            doInterpret = true;
        else
            doInterpret = false;
    }
    else
    {
        const char *methodName = compHnd->getMethodNameFromMetadata(methodInfo->ftn, nullptr, nullptr, nullptr, 0);

        // TODO: replace this by something like the JIT does to support multiple methods being specified and we don't
        // keep fetching it on each call to compileMethod
        const char *methodToInterpret = g_interpHost->getStringConfigValue("Interpreter");
        doInterpret = (methodName != NULL && strcmp(methodName, methodToInterpret) == 0);
        g_interpHost->freeStringConfigValue(methodToInterpret);
        if (doInterpret)
            g_interpModule = methodInfo->scope;
    }

    if (!doInterpret)
    {
        return CORJIT_SKIPPED;
    }

    InterpCompiler compiler(compHnd, methodInfo, false /* verbose */);
    InterpMethod *pMethod = compiler.CompileMethod();

    int32_t IRCodeSize;
    int32_t *pIRCode = compiler.GetCode(&IRCodeSize);
 
    uint32_t sizeOfCode = IRCodeSize * sizeof(int32_t);

    AllocMemArgs args {};
    args.hotCodeSize = sizeOfCode;
    args.coldCodeSize = 0;
    args.roDataSize = 0;
    args.xcptnsCount = 0;
    args.flag = CORJIT_ALLOCMEM_DEFAULT_CODE_ALIGN;
    compHnd->allocMem(&args);

    // How to set the pMethod pointer
    // * Let this code understand the code header and write it directly there - nope
    // * Cast compHnd to the interpreter specific one and call a method to set it
    // * Add a new method to the ICorJitInfo interface to set the pMethod pointer, but what to do with the JITted info? Maybe name the method as SetAuxiliaryInfo
    //   This would actually not be that bad as we already have the allocUnwindInfo that is JIT specific
    // * Add an extra slot to the AllocMemArgs that would the CInterpreterJitInfo::allocMem set on the header
    // * Add new interface IInterpreterJitInfo that would have an extra method to set the pMethod pointer.
    //   Then it would make sense to have a new interface for the JIT too that would have the reserveUnwindInfo and allocUnwindInfo methods.

    // We store the InterpMethod pointer into the code header
    compHnd->setInterpMethod(pMethod);
    memcpy ((uint8_t*)args.hotCodeBlockRW, pIRCode, IRCodeSize * sizeof(int32_t));

    *entryAddress = (uint8_t*)args.hotCodeBlock;
    *nativeSizeOfCode = sizeOfCode;

    return CORJIT_OK;
}

void CILInterp::ProcessShutdownWork(ICorStaticInfo* statInfo)
{
    g_interpInitialized = false;
}

void CILInterp::getVersionIdentifier(GUID* versionIdentifier)
{
    assert(versionIdentifier != nullptr);
    memcpy(versionIdentifier, &JITEEVersionIdentifier, sizeof(GUID));
}

void CILInterp::setTargetOS(CORINFO_OS os)
{
}
