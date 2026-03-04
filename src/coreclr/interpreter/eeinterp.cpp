// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include <stddef.h>
#include "corjit.h"

#include "interpreter.h"
#include "compiler.h"
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

#if MEASURE_MEM_ALLOC
    InterpCompiler::initMemStats();
#endif

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

    bool doInterpret = false;
    ArenaAllocatorWithDestructorT<InterpMemKindTraits> arenaAllocator;

    if ((g_interpModule != NULL) && (methodInfo->scope == g_interpModule))
        doInterpret = true;

    {
        switch (InterpConfig.InterpMode())
        {
            // 0: default, do not use interpreter except explicit opt-in via DOTNET_Interpreter
            case 0:
                break;

            // 1: use interpreter for everything except (1) methods that have R2R compiled code and (2) all code in System.Private.CoreLib. All code in System.Private.CoreLib falls back to JIT if there is no R2R available for it.
            case 1:
            {
                doInterpret = true;
                const char *assemblyName = compHnd->getClassAssemblyName(compHnd->getMethodClass(methodInfo->ftn));
                if (assemblyName && !strcmp(assemblyName, "System.Private.CoreLib"))
                    doInterpret = false;
                break;
            }

            // 2: use interpreter for everything except intrinsics. All intrinsics fallback to JIT. Implies DOTNET_ReadyToRun=0
            case 2:
                doInterpret = !(compHnd->getMethodAttribs(methodInfo->ftn) & CORINFO_FLG_INTRINSIC);
                break;

            // 3: use interpreter for everything, the full interpreter-only mode, no fallbacks to R2R or JIT whatsoever. Implies DOTNET_ReadyToRun=0, DOTNET_EnableHWIntrinsic=0, DOTNET_MaxVectorTBitWidth=128, DOTNET_PreferredVectorBitWidth=128
            case 3:
                doInterpret = true;
                break;

            default:
                NO_WAY("Unsupported value for DOTNET_InterpMode");
                break;
        }

#if !defined(FEATURE_DYNAMIC_CODE_COMPILED)
        // interpret everything when we do not have a JIT
        doInterpret = true;
#else
        // NOTE: We do this check even if doInterpret==true in order to populate g_interpModule
        const char *methodName = compHnd->getMethodNameFromMetadata(methodInfo->ftn, nullptr, nullptr, nullptr, 0);
        if (InterpConfig.Interpreter().contains(compHnd, methodInfo->ftn, compHnd->getMethodClass(methodInfo->ftn), &methodInfo->args))
        {
            doInterpret = true;
            g_interpModule = methodInfo->scope;
        }
#endif
    }

    if (!doInterpret)
    {
        return CORJIT_SKIPPED;
    }

    try
    {
        InterpreterRetryData retryData(&arenaAllocator);

        while (true)
        {
            retryData.StartCompilationAttempt();
            InterpCompiler compiler(compHnd, methodInfo, &retryData, &arenaAllocator);
            InterpMethod *pMethod = compiler.CompileMethod();
            if (pMethod == NULL)
            {
                assert(retryData.NeedsRetry());
                continue;
            }

            // Once we reach here we will not attempt to retry again.
            assert(!retryData.NeedsRetry());

            int32_t IRCodeSize = 0;
            int32_t *pIRCode = compiler.GetCode(&IRCodeSize);

            uint32_t sizeOfCode = sizeof(InterpMethod*) + IRCodeSize * sizeof(int32_t);
            uint8_t unwindInfo[8] = {0, 0, 0, 0, 0, 0, 0, 0};

            AllocMemChunk codeChunk {};
            codeChunk.alignment = 1;
            codeChunk.size = sizeOfCode;
            codeChunk.flags = CORJIT_ALLOCMEM_HOT_CODE;

            AllocMemArgs args {};
            args.chunks = &codeChunk;
            args.chunksCount = 1;
            args.xcptnsCount = 0;
            compHnd->allocMem(&args);

            // We store first the InterpMethod pointer as the code header, followed by the actual code
            *(InterpMethod**)codeChunk.blockRW = pMethod;
            memcpy ((uint8_t*)codeChunk.blockRW + sizeof(InterpMethod*), pIRCode, IRCodeSize * sizeof(int32_t));

            compiler.UpdateWithFinalMethodByteCodeAddress((InterpByteCodeStart*)codeChunk.block);
            *entryAddress = (uint8_t*)codeChunk.block;
            *nativeSizeOfCode = sizeOfCode;

            // We can't do this until we've called allocMem
            compiler.BuildGCInfo(pMethod);
            compiler.BuildEHInfo();
            compiler.dumpMethodMemStats();
            break;
        }
    }
    catch(const InterpException& e)
    {
        return e.m_result;
    }

    return CORJIT_OK;
}

void CILInterp::ProcessShutdownWork(ICorStaticInfo* statInfo)
{
#if MEASURE_MEM_ALLOC
    if (InterpCompiler::s_dspMemStats)
    {
        InterpCompiler::dumpAggregateMemStats(stdout);
        InterpCompiler::dumpMaxMemStats(stdout);
        InterpCompiler::dumpMemStatsHistograms(stdout);
    }
#endif
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
    if (IsInterpDumpActive())
        printf("Error during interpreter method compilation: %s\n", message ? message : "unknown error");
    throw InterpException(message, CORJIT_INTERNALERROR);
}

INTERPRETER_NORETURN void BADCODE(const char* message)
{
    if (IsInterpDumpActive())
        printf("Error during interpreter method compilation: %s\n", message ? message : "unknown error");
    throw InterpException(message, CORJIT_BADCODE);
}

INTERPRETER_NORETURN void SKIPCODE(const char* message)
{
    if (IsInterpDumpActive())
        printf("Skip during interpreter method compilation: %s\n", message ? message : "unknown error");
    throw InterpException(message, CORJIT_SKIPPED);
}

INTERPRETER_NORETURN void NOMEM()
{
    throw InterpException(NULL, CORJIT_OUTOFMEM);
}

/*****************************************************************************/
// Define the static Names array for InterpMemKindTraits
const char* const InterpMemKindTraits::Names[] = {
#define InterpMemKindMacro(kind) #kind,
#include "interpmemkind.h"
};

// The interpreter normally uses the host allocator (allocateSlab/freeSlab). In DEBUG
// builds, when InterpDirectAlloc is enabled, allocations bypass the host allocator
// and go directly to the OS, so this may return true.
bool InterpMemKindTraits::bypassHostAllocator()
{
#if defined(DEBUG)
    // When InterpDirectAlloc is set, interpreter allocation requests are forwarded
    // directly to the OS. This allows taking advantage of pageheap and other gflag
    // knobs for ensuring that we do not have buffer overruns in the interpreter.

    return InterpConfig.InterpDirectAlloc() != 0;
#else  // defined(DEBUG)
    return false;
#endif // !defined(DEBUG)
}

// The interpreter doesn't currently support fault injection.
bool InterpMemKindTraits::shouldInjectFault()
{
#if defined(DEBUG)
    return InterpConfig.ShouldInjectFault() != 0;
#else
    return false;
#endif
}

// Allocates a block of memory using malloc.
void* InterpMemKindTraits::allocateHostMemory(size_t size, size_t* pActualSize)
{
#if defined(DEBUG)
    if (bypassHostAllocator())
    {
        *pActualSize = size;
        if (size == 0)
        {
            size = 1;
        }
        void* p = malloc(size);
        if (p == nullptr)
        {
            NOMEM();
        }
        return p;
    }
#endif // !defined(DEBUG)

    return g_interpHost->allocateSlab(size, pActualSize);
}

// Frees a block of memory previously allocated by allocateHostMemory.
void InterpMemKindTraits::freeHostMemory(void* block, size_t size)
{
#if defined(DEBUG)
    if (bypassHostAllocator())
    {
        free(block);
        return;
    }
#endif // !defined(DEBUG)

    g_interpHost->freeSlab(block, size);
}

// Fills a memory block with an uninitialized pattern for DEBUG builds.
void InterpMemKindTraits::fillWithUninitializedPattern(void* block, size_t size)
{
#if defined(DEBUG)
    // Use 0xCD pattern (same as MSVC debug heap) to help catch use-before-init bugs
    memset(block, 0xCD, size);
#else
    (void)block;
    (void)size;
#endif
}

// Called when the allocator runs out of memory.
void InterpMemKindTraits::outOfMemory()
{
    NOMEM();
}
