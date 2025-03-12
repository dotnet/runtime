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

//****************************************************************************
CorJitResult CILInterp::compileMethod(ICorJitInfo*         compHnd,
                                   CORINFO_METHOD_INFO* methodInfo,
                                   unsigned             flags,
                                   uint8_t**            entryAddress,
                                   uint32_t*            nativeSizeOfCode)
{

    const char *methodName = compHnd->getMethodNameFromMetadata(methodInfo->ftn, nullptr, nullptr, nullptr, 0);

    // TODO: replace this by something like the JIT does to support multiple methods being specified and we don't
    // keep fetching it on each call to compileMethod
    const char *methodToInterpret = g_interpHost->getStringConfigValue("AltJit");
    bool doInterpret = (methodName != NULL && strcmp(methodName, methodToInterpret) == 0);
    g_interpHost->freeStringConfigValue(methodToInterpret);

    if (!doInterpret)
    {
        return CORJIT_SKIPPED;
    }

    InterpCompiler compiler(compHnd, methodInfo);
    InterpMethod *pMethod = compiler.CompileMethod();

    int32_t IRCodeSize;
    int32_t *pIRCode = compiler.GetCode(&IRCodeSize);
 
    // FIXME this shouldn't be here
    compHnd->setMethodAttribs(methodInfo->ftn, CORINFO_FLG_INTERPRETER);

    uint32_t sizeOfCode = sizeof(InterpMethod*) + IRCodeSize * sizeof(int32_t);
    uint8_t unwindInfo[8] = {0, 0, 0, 0, 0, 0, 0, 0};

    // TODO: get rid of the need to allocate fake unwind info.
    compHnd->reserveUnwindInfo(false /* isFunclet */, false /* isColdCode */ , sizeof(unwindInfo) /* unwindSize */);
    AllocMemArgs args;
    args.hotCodeSize = sizeOfCode;
    args.coldCodeSize = 0;
    args.roDataSize = 0;
    args.xcptnsCount = 0;
    args.flag = CORJIT_ALLOCMEM_DEFAULT_CODE_ALIGN;
    compHnd->allocMem(&args);

    // We store first the InterpMethod pointer as the code header, followed by the actual code
    *(InterpMethod**)args.hotCodeBlockRW = pMethod;
    memcpy ((uint8_t*)args.hotCodeBlockRW + sizeof(InterpMethod*), pIRCode, IRCodeSize * sizeof(int32_t));

    // TODO: get rid of the need to allocate fake unwind info
    compHnd->allocUnwindInfo((uint8_t*)args.hotCodeBlock, (uint8_t*)args.coldCodeBlock, 0, 1, sizeof(unwindInfo), unwindInfo, CORJIT_FUNC_ROOT);

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
