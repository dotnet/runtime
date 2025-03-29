// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "standardpch.h"
#include "superpmi.h"
#include "jitinstance.h"
#include "icorjitinfo.h"
#include "jithost.h"
#include "errorhandling.h"
#include "spmiutil.h"

JitInstance* JitInstance::InitJit(char*                         nameOfJit,
                                  bool                          breakOnAssert,
                                  SimpleTimer*                  st1,
                                  MethodContext*                firstContext,
                                  LightWeightMap<DWORD, DWORD>* forceOptions,
                                  LightWeightMap<DWORD, DWORD>* options)
{
    JitInstance* jit = new JitInstance();
    if (jit == nullptr)
    {
        LogError("Failed to allocate a JitInstance");
        return nullptr;
    }

    jit->forceOptions = forceOptions;
    jit->options = options;

    // The flag to cause the JIT to be invoked as an altjit is stored in the jit flags, not in
    // the environment. If the user uses the "-jitoption force" flag to force AltJit off
    // or to force it on, then propagate that to the jit flags.
    jit->forceClearAltJitFlag = false;
    jit->forceSetAltJitFlag = false;
    const char* altJitFlag = jit->getForceOption("AltJit");
    if (altJitFlag != nullptr)
    {
        if (strcmp(altJitFlag, "") == 0)
        {
            jit->forceClearAltJitFlag = true;
        }
        else
        {
            jit->forceSetAltJitFlag = true;
        }
    }
    const char* altJitNgenFlag = jit->getForceOption("AltJitNgen");
    if (altJitNgenFlag != nullptr)
    {
        if (strcmp(altJitNgenFlag, "") == 0)
        {
            jit->forceClearAltJitFlag = true;
        }
        else
        {
            jit->forceSetAltJitFlag = true;
        }
    }

    jit->environment.getIntConfigValue    = nullptr;
    jit->environment.getStringConfigValue = nullptr;

    if (st1 != nullptr)
        st1->Start();
    HRESULT hr = jit->StartUp(nameOfJit, breakOnAssert, firstContext);
    if (st1 != nullptr)
        st1->Stop();
    if (hr != S_OK)
    {
        LogError("Startup of JIT(%s) failed %d", nameOfJit, hr);
        return nullptr;
    }
    if (st1 != nullptr)
        LogVerbose("Jit startup took %fms", st1->GetMilliseconds());

    return jit;
}

HRESULT JitInstance::StartUp(char* PathToJit, bool breakOnDebugBreakorAV, MethodContext* firstContext)
{
    // startup jit
    DWORD dwRetVal = 0;
    UINT  uRetVal  = 0;
    BOOL  bRetVal  = FALSE;

    SetBreakOnDebugBreakOrAV(breakOnDebugBreakorAV);

    char pFullPathName[MAX_PATH];

    // find the full jit path
    dwRetVal = ::GetFullPathNameA(PathToJit, MAX_PATH, pFullPathName, nullptr);
    if (dwRetVal == 0)
    {
        LogError("GetFullPathName failed (0x%08x)", ::GetLastError());
        return E_FAIL;
    }

#ifndef TARGET_UNIX // No file version APIs in the PAL
    // Do a quick version check
    DWORD dwHandle = 0;
    DWORD fviSize  = GetFileVersionInfoSizeA(pFullPathName, &dwHandle);

    if ((fviSize != 0) && (dwHandle == 0))
    {
        unsigned char* fviData = new unsigned char[fviSize];
        if (GetFileVersionInfoA(pFullPathName, dwHandle, fviSize, fviData))
        {
            UINT              size    = 0;
            VS_FIXEDFILEINFO* verInfo = nullptr;
            if (VerQueryValueA(fviData, "\\", (LPVOID*)&verInfo, &size))
            {
                if (size)
                {
                    if (verInfo->dwSignature == 0xfeef04bd)
                        LogDebug("'%s' is version %u.%u.%u.%u", pFullPathName, (verInfo->dwFileVersionMS) >> 16,
                                 (verInfo->dwFileVersionMS) & 0xFFFF, (verInfo->dwFileVersionLS) >> 16,
                                 (verInfo->dwFileVersionLS) & 0xFFFF);
                }
            }
        }
        delete[] fviData;
    }
#endif // !TARGET_UNIX

    // Load Library
    hLib = ::LoadLibraryExA(pFullPathName, NULL, 0);
    if (hLib == 0)
    {
        LogError("LoadLibrary failed (0x%08x)", ::GetLastError());
        return E_FAIL;
    }

    // get entry points
    pngetJit = (PgetJit)::GetProcAddress(hLib, "getJit");
    if (pngetJit == 0)
    {
        LogError("GetProcAddress 'getJit' failed (0x%08x)", ::GetLastError());
        return -1;
    }
    pnjitStartup    = (PjitStartup)::GetProcAddress(hLib, "jitStartup");

    // Setup ICorJitHost and call jitStartup if necessary
    if (pnjitStartup != nullptr)
    {
        mc      = firstContext;
        jitHost = new JitHost(*this);
        if (!callJitStartup(jitHost))
        {
            LogError("jitStartup failed");
            return -1;
        }
    }

    pJitInstance = pngetJit();
    if (pJitInstance == nullptr)
    {
        LogError("pngetJit gave us null");
        return -1;
    }

    // Check the JIT version identifier.

    GUID versionId;
    memset(&versionId, 0, sizeof(GUID));
    pJitInstance->getVersionIdentifier(&versionId);

    if (memcmp(&versionId, &JITEEVersionIdentifier, sizeof(GUID)) != 0)
    {
        // Mismatched version ID. Fail the load.
        pJitInstance = NULL;

        GUID expected = JITEEVersionIdentifier;
        GUID actual = versionId;
        LogError("Jit Compiler has wrong version identifier. Expected: %08x-%04x-%04x-%02x%02x-%02x%02x%02x%02x%02x%02x. Actual: %08x-%04x-%04x-%02x%02x-%02x%02x%02x%02x%02x%02x.",
                 expected.Data1, expected.Data2, expected.Data3,
                 expected.Data4[0], expected.Data4[1], expected.Data4[2], expected.Data4[3],
                 expected.Data4[4], expected.Data4[5], expected.Data4[6], expected.Data4[7],
                 actual.Data1, actual.Data2, actual.Data3,
                 actual.Data4[0], actual.Data4[1], actual.Data4[2], actual.Data4[3],
                 actual.Data4[4], actual.Data4[5], actual.Data4[6], actual.Data4[7]);

        return -1;
    }

    icji = InitICorJitInfo(this);

    return S_OK;
}

#undef DLLEXPORT
#ifdef _MSC_VER
#define DLLEXPORT __declspec(dllexport)
#else
#define DLLEXPORT __attribute__((visibility("default")))
#endif

DLLEXPORT volatile UINT64 s_globalZero;
// A dynamic instrumentor can rewrite this function to provide precise
// instruction counts that SPMI will report in metrics. We need the volatile
// read here to model to the compiler that something might actually happen in
// this function.
extern "C" DLLEXPORT NOINLINE void Instrumentor_GetInsCount(UINT64* result)
{
    // Instrumentor may have written the value already.
    if (*result == 0)
    {
        *result = s_globalZero;
    }
}

ReplayResults JitInstance::CompileMethod(MethodContext* MethodToCompile, int mcIndex, bool collectThroughput)
{
    struct Param : FilterSuperPMIExceptionsParam_CaptureException
    {
        JitInstance*        pThis;
        CORINFO_METHOD_INFO info;
        unsigned            flags;
        int                 mcIndex;
        bool                collectThroughput;
        bool*               isMinOpts;
        ReplayResults       results;
    } param;
    param.pThis             = this;
    param.flags             = 0;
    param.mcIndex           = mcIndex;
    param.collectThroughput = collectThroughput;
    param.results.Result    = ReplayResult::Success;

    // store to instance field our raw values, so we can figure things out a bit later...
    mc = MethodToCompile;

    times[0] = 0;
    times[1] = 0;

    stj.Start();

    UINT64 insCountBefore = 0;
    Instrumentor_GetInsCount(&insCountBefore);

    PAL_TRY(Param*, pParam, &param)
    {
        uint8_t*   NEntryBlock    = nullptr;
        uint32_t   NCodeSizeBlock = 0;
        CORINFO_OS os             = CORINFO_WINNT;

        pParam->pThis->mc->repCompileMethod(&pParam->info, &pParam->flags, &os);
        CORJIT_FLAGS jitFlags;
        pParam->pThis->getJitFlags(&jitFlags, sizeof(jitFlags));

        pParam->results.IsMinOpts =
            jitFlags.IsSet(CORJIT_FLAGS::CORJIT_FLAG_DEBUG_CODE) ||
            jitFlags.IsSet(CORJIT_FLAGS::CORJIT_FLAG_MIN_OPT) ||
            jitFlags.IsSet(CORJIT_FLAGS::CORJIT_FLAG_TIER0);

        if (pParam->collectThroughput)
        {
            pParam->pThis->lt.Start();
        }
        pParam->pThis->pJitInstance->setTargetOS(os);
        CorJitResult jitResult = pParam->pThis->pJitInstance->compileMethod(pParam->pThis->icji, &pParam->info,
                                                                       pParam->flags, &NEntryBlock, &NCodeSizeBlock);
        if (pParam->collectThroughput)
        {
            pParam->pThis->lt.Stop();
            pParam->pThis->times[0] = pParam->pThis->lt.GetCycles();
        }

        CorInfoMethodRuntimeFlags flags = pParam->pThis->mc->cr->repSetMethodAttribs(pParam->info.ftn);
        if ((flags & CORINFO_FLG_SWITCHED_TO_MIN_OPT) != 0)
        {
            pParam->results.IsMinOpts = true;
        }
        else if ((flags & CORINFO_FLG_SWITCHED_TO_OPTIMIZED) != 0)
        {
            pParam->results.IsMinOpts = false;
        }

        if (jitResult == CORJIT_SKIPPED)
        {
            SPMI_TARGET_ARCHITECTURE targetArch = GetSpmiTargetArchitecture();
            bool matchesTargetArch              = false;

            switch (pParam->pThis->mc->repGetExpectedTargetArchitecture())
            {
                case IMAGE_FILE_MACHINE_AMD64:
                    matchesTargetArch = (targetArch == SPMI_TARGET_ARCHITECTURE_AMD64);
                    break;

                case IMAGE_FILE_MACHINE_I386:
                    matchesTargetArch = (targetArch == SPMI_TARGET_ARCHITECTURE_X86);
                    break;

                case IMAGE_FILE_MACHINE_ARMNT:
                    matchesTargetArch = (targetArch == SPMI_TARGET_ARCHITECTURE_ARM);
                    break;

                case IMAGE_FILE_MACHINE_ARM64:
                    matchesTargetArch = (targetArch == SPMI_TARGET_ARCHITECTURE_ARM64);
                    break;

                default:
                    LogError("Unknown target architecture");
                    break;
            }

            // If the target architecture doesn't match the expected target architecture
            // then we have an altjit, so treat SKIPPED as OK to avoid counting the compilation as failed.

            if (!matchesTargetArch)
            {
                jitResult = CORJIT_OK;
            }
            else
            {
                // If the target matches, but the JIT is an altjit and the user specified RunAltJitCode=0,
                // then the JIT will also return CORJIT_SKIPPED, to prevent the generated code from being used.
                // However, we don't want to treat that as a replay failure.
                if (jitFlags.IsSet(CORJIT_FLAGS::CORJIT_FLAG_ALT_JIT) &&
                    (pParam->pThis->jitHost->getIntConfigValue("RunAltJitCode", 1) == 0))
                {
                    jitResult = CORJIT_OK;
                }
            }
        }

        if ((jitResult == CORJIT_OK) || (jitResult == CORJIT_BADCODE))
        {
            // capture the results of compilation
            pParam->pThis->mc->cr->recCompileMethod(&NEntryBlock, &NCodeSizeBlock, jitResult);
            pParam->pThis->mc->cr->recAllocMemCapture();
            pParam->pThis->mc->cr->recAllocGCInfoCapture();

            pParam->pThis->mc->cr->recMessageLog(jitResult == CORJIT_OK ? "Successful Compile" : "Successful Compile (BADCODE)");
        }
        else
        {
            LogDebug("compileMethod failed with result %d", jitResult);
            pParam->results.Result = ReplayResult::Error;
        }
    }
    PAL_EXCEPT_FILTER(FilterSuperPMIExceptions_CaptureExceptionAndStop)
    {
        SpmiException e(&param);

        if (e.GetCode() == EXCEPTIONCODE_MC)
        {
            char* message = e.GetExceptionMessage();
            LogMissing("Method context %d failed to replay: %s", mcIndex, message);
            e.DeleteMessage();
            param.results.Result = ReplayResult::Miss;
        }
        else if (e.GetCode() == EXCEPTIONCODE_RECORDED_EXCEPTION)
        {
            // Exception thrown by EE during recording, for example a managed
            // MissingFieldException thrown by resolveToken. Several JIT-EE
            // APIs can throw exceptions and the recorder expects and rethrows
            // their exceptions under this exception code. We do not consider
            // these a replay failure.

            // Call these methods to capture that no code/GC info was generated.
            mc->cr->recAllocMemCapture();
            mc->cr->recAllocGCInfoCapture();

            mc->cr->recMessageLog("Successful Compile (EE API exception)");
        }
        else
        {
            e.ShowAndDeleteMessage();
            param.results.Result = ReplayResult::Error;
        }
    }
    PAL_ENDTRY

    stj.Stop();
    if (collectThroughput)
    {
        // If we get here, we know it compiles
        timeResult(param.info, param.flags);
    }

    mc->cr->secondsToCompile = stj.GetSeconds();
    param.results.CompileResults = mc->cr;

    UINT64 insCountAfter = 0;
    Instrumentor_GetInsCount(&insCountAfter);

    param.results.NumExecutedInstructions = static_cast<long long>(insCountAfter - insCountBefore);
    return param.results;
}

void JitInstance::timeResult(CORINFO_METHOD_INFO info, unsigned flags)
{
    uint8_t* NEntryBlock    = nullptr;
    uint32_t NCodeSizeBlock = 0;

    int sampleSize = 10;
    // Save 2 smallest times. To help reduce noise, we will look at the closest pair of these.
    uint64_t time;

    for (int i = 0; i < sampleSize; i++)
    {
        delete mc->cr;
        mc->cr = new CompileResult();
        lt.Start();
        pJitInstance->compileMethod(icji, &info, flags, &NEntryBlock, &NCodeSizeBlock);
        lt.Stop();
        time = lt.GetCycles();
        if (times[1] == 0)
        {
            if (time < times[0])
            {
                times[1] = times[0];
                times[0] = time;
            }
            else
                times[1] = time;
        }
        else if (time < times[1])
        {
            if (time < times[0])
            {
                times[1] = times[0];
                times[0] = time;
            }
            else
                times[1] = time;
        }
    }
}

/*-------------------------- Misc ---------------------------------------*/

const char* JitInstance::getForceOption(const char* key)
{
    return getOption(key, forceOptions);
}

const char* JitInstance::getOption(const char* key)
{
    return getOption(key, options);
}

const char* JitInstance::getOption(const char* key, LightWeightMap<DWORD, DWORD>* options)
{
    if (options == nullptr)
    {
        return nullptr;
    }

    size_t keyLenInBytes = sizeof(char) * (strlen(key) + 1);
    int    keyIndex      = options->Contains((unsigned char*)key, (unsigned int)keyLenInBytes);
    if (keyIndex == -1)
    {
        return nullptr;
    }

    return (const char*)options->GetBuffer(options->Get(keyIndex));
}

// Returns extended flags for a particular compilation instance, adjusted for altjit.
// This is a helper call; it does not record the call in the CompileResult.
uint32_t JitInstance::getJitFlags(CORJIT_FLAGS* jitFlags, uint32_t sizeInBytes)
{
    uint32_t ret = mc->repGetJitFlags(jitFlags, sizeInBytes);
    if (forceClearAltJitFlag)
    {
        jitFlags->Clear(CORJIT_FLAGS::CORJIT_FLAG_ALT_JIT);
    }
    else if (forceSetAltJitFlag)
    {
        jitFlags->Set(CORJIT_FLAGS::CORJIT_FLAG_ALT_JIT);
    }
    return ret;
}

// Used to allocate memory that needs to handed to the EE.
// For eg, use this to allocated memory for reporting debug info,
// which will be handed to the EE by setVars() and setBoundaries()
void* JitInstance::allocateArray(size_t cBytes)
{
    mc->cr->AddCall("allocateArray");
    return mc->cr->allocateMemory(cBytes);
}

// Used to allocate memory that needs to live as long as the jit
// instance does.
void* JitInstance::allocateLongLivedArray(size_t cBytes)
{
    return new BYTE[cBytes];
}

// JitCompiler will free arrays passed by the EE using this
// For eg, The EE returns memory in getVars() and getBoundaries()
// to the JitCompiler, which the JitCompiler should release using
// freeArray()
void JitInstance::freeArray(void* array)
{
    mc->cr->AddCall("freeArray");
    // We don't bother freeing this until the mc->cr itself gets freed.
}

// Used to free memory allocated by JitInstance::allocateLongLivedArray.
void JitInstance::freeLongLivedArray(void* array)
{
    delete [] (BYTE*)array;
}

// Helper for calling pnjitStartup. Needed to allow SEH here.
bool JitInstance::callJitStartup(ICorJitHost* jithost)
{
    // Calling into the collection, which could fail, especially
    // for altjits. So protect the call.

    struct Param : FilterSuperPMIExceptionsParam_CaptureException
    {
        JitInstance* pThis;
        ICorJitHost* jithost;
        bool         result;
    } param;
    param.pThis   = this;
    param.jithost = jithost;
    param.result  = false;

    PAL_TRY(Param*, pParam, &param)
    {
        pParam->pThis->pnjitStartup(pParam->jithost);
        pParam->result = true;
    }
    PAL_EXCEPT_FILTER(FilterSuperPMIExceptions_CaptureExceptionAndStop)
    {
        SpmiException e(&param);

        LogError("failed to call jitStartup.");
        e.ShowAndDeleteMessage();
    }
    PAL_ENDTRY

    Assert(environment.getIntConfigValue == nullptr && environment.getStringConfigValue == nullptr);
    environment = mc->cloneEnvironment();

    return param.result;
}

// Reset JitConfig, that stores Environment variables.
bool JitInstance::resetConfig(MethodContext* firstContext)
{
    if (pnjitStartup == nullptr)
    {
        return false;
    }

    if (environment.getIntConfigValue != nullptr)
    {
        delete environment.getIntConfigValue;
        environment.getIntConfigValue = nullptr;
    }

    if (environment.getStringConfigValue != nullptr)
    {
        delete environment.getStringConfigValue;
        environment.getStringConfigValue = nullptr;
    }

    mc                   = firstContext;
    ICorJitHost* newHost = new JitHost(*this);

    if (!callJitStartup(newHost))
    {
        return false;
    }

    delete static_cast<JitHost*>(jitHost);
    jitHost = newHost;
    return true;
}

const MethodContext::Environment& JitInstance::getEnvironment()
{
    return environment;
}

void JitInstance::updateForceOptions(LightWeightMap<DWORD, DWORD>* newForceOptions)
{
    forceOptions = newForceOptions;
}
