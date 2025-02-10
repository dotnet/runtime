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

#include <vector>

// FIXME We will probably end up not needing this table.
// If deemed useful, use some hashtable implementation instead.
std::vector<std::pair<CORINFO_METHOD_HANDLE,InterpMethod*>> g_interpCodeHash;

static InterpMethod* InterpGetInterpMethod(CORINFO_METHOD_HANDLE methodHnd)
{
    // FIXME lock for multiple thread access
    for (size_t i = 0; i < g_interpCodeHash.size(); i++)
    {
        if (g_interpCodeHash[i].first == methodHnd)
        {
            return g_interpCodeHash[i].second;
        }
    }

    InterpMethod* pMethod = new InterpMethod();
    pMethod->methodHnd = methodHnd;

    g_interpCodeHash.push_back({methodHnd, pMethod});
    return pMethod;
}

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

static InterpManager g_Manager;
extern "C" INTERP_API ICorInterpreter* getInterpreter()
{
    if (!g_interpInitialized)
    {
        return nullptr;
    }
    return &g_Manager;
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

    InterpMethod *pMethod = InterpGetInterpMethod(methodInfo->ftn);
    pMethod->compiled = true;

    // FIXME this shouldn't be here
    compHnd->setMethodAttribs(methodInfo->ftn, CORINFO_FLG_INTERPRETER);

    // TODO: get rid of the need to allocate fake unwind info.
    compHnd->reserveUnwindInfo(false /* isFunclet */, false /* isColdCode */ , 8 /* unwindSize */);
    AllocMemArgs args;
    args.hotCodeSize = 16;
    args.coldCodeSize = 0;
    args.roDataSize = 0;
    args.xcptnsCount = 0;
    args.flag = CORJIT_ALLOCMEM_DEFAULT_CODE_ALIGN;
    compHnd->allocMem(&args);
    uint8_t *code = (uint8_t*)args.hotCodeBlockRW;
    *code++ = 1; // fake byte code

    // TODO: get rid of the need to allocate fake unwind info
    compHnd->allocUnwindInfo((uint8_t*)args.hotCodeBlock, (uint8_t*)args.coldCodeBlock, 0, 1, 0, nullptr, CORJIT_FUNC_ROOT);

    *entryAddress = (uint8_t*)args.hotCodeBlock;
    *nativeSizeOfCode = 1;

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

void* InterpManager::GetInterpMethod(CORINFO_METHOD_HANDLE methodHnd)
{
    return InterpGetInterpMethod(methodHnd);
}
