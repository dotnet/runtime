// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include <stddef.h>
#include "corjit.h"

#include "interpreter.h"
#include "eeinterp.h"

#include <string.h>
#include <stdio.h>

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

    assert(!InterpConfig.IsInitialized());
    InterpConfig.Initialize(jitHost);

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
#ifdef TARGET_WASM
        // interpret everything on wasm
        doInterpret = true;
#else
        doInterpret = (InterpConfig.Interpreter().contains(compHnd, methodInfo->ftn, compHnd->getMethodClass(methodInfo->ftn), &methodInfo->args));
#endif

        if (doInterpret)
            g_interpModule = methodInfo->scope;
    }

    if (!doInterpret)
    {
        return CORJIT_SKIPPED;
    }

    try
    {
        InterpCompiler compiler(compHnd, methodInfo);
        InterpMethod *pMethod = compiler.CompileMethod();
        int32_t IRCodeSize = 0;
        int32_t *pIRCode = compiler.GetCode(&IRCodeSize);

        uint32_t sizeOfCode = sizeof(InterpMethod*) + IRCodeSize * sizeof(int32_t);
        uint8_t unwindInfo[8] = {0, 0, 0, 0, 0, 0, 0, 0};

        AllocMemArgs args {};
        args.hotCodeSize = sizeOfCode;
        args.coldCodeSize = 0;
        args.roDataSize = 0;
        args.xcptnsCount = 0;
        args.flag = CORJIT_ALLOCMEM_DEFAULT_CODE_ALIGN;
        compHnd->allocMem(&args);

        // We store first the InterpMethod pointer as the code header, followed by the actual code
        *(InterpMethod**)args.hotCodeBlockRW = pMethod;
        memcpy ((uint8_t*)args.hotCodeBlockRW + sizeof(InterpMethod*), pIRCode, IRCodeSize * sizeof(int32_t));

        *entryAddress = (uint8_t*)args.hotCodeBlock;
        *nativeSizeOfCode = sizeOfCode;

        // We can't do this until we've called allocMem
        compiler.BuildGCInfo(pMethod);
        compiler.BuildEHInfo();
    }
    catch(const InterpException& e)
    {
        return e.m_result;
    }

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

INTERPRETER_NORETURN void NO_WAY(const char* message)
{
    throw InterpException(message, CORJIT_INTERNALERROR);
}

INTERPRETER_NORETURN void BADCODE(const char* message)
{
    throw InterpException(message, CORJIT_BADCODE);
}

INTERPRETER_NORETURN void NOMEM()
{
    throw InterpException(NULL, CORJIT_OUTOFMEM);
}
