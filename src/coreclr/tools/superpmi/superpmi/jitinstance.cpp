// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "standardpch.h"
#include "superpmi.h"
#include "jitinstance.h"
#include "icorjitinfo.h"
#include "jithost.h"
#include "errorhandling.h"
#include "spmiutil.h"
#include "metricssummary.h"

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
    const WCHAR* altJitFlag = jit->getForceOption(W("AltJit"));
    if (altJitFlag != nullptr)
    {
        if (wcscmp(altJitFlag, W("")) == 0)
        {
            jit->forceClearAltJitFlag = true;
        }
        else
        {
            jit->forceSetAltJitFlag = true;
        }
    }
    const WCHAR* altJitNgenFlag = jit->getForceOption(W("AltJitNgen"));
    if (altJitNgenFlag != nullptr)
    {
        if (wcscmp(altJitNgenFlag, W("")) == 0)
        {
            jit->forceClearAltJitFlag = true;
        }
        else
        {
            jit->forceSetAltJitFlag = true;
        }
    }

    jit->environment.getIntConfigValue   = nullptr;
    jit->environment.getStingConfigValue = nullptr;

    if (st1 != nullptr)
        st1->Start();
    HRESULT hr = jit->StartUp(nameOfJit, false, breakOnAssert, firstContext);
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

HRESULT JitInstance::StartUp(char* PathToJit, bool copyJit, bool breakOnDebugBreakorAV, MethodContext* firstContext)
{
    // startup jit
    DWORD dwRetVal = 0;
    UINT  uRetVal  = 0;
    BOOL  bRetVal  = FALSE;

    SetBreakOnDebugBreakOrAV(breakOnDebugBreakorAV);

    char pFullPathName[MAX_PATH];
    char lpTempPathBuffer[MAX_PATH];
    char szTempFileName[MAX_PATH];

    // find the full jit path
    dwRetVal = ::GetFullPathNameA(PathToJit, MAX_PATH, pFullPathName, nullptr);
    if (dwRetVal == 0)
    {
        LogError("GetFullPathName failed (0x%08x)", ::GetLastError());
        return E_FAIL;
    }

    // Store the full path to the jit
    PathToOriginalJit = (char*)malloc(MAX_PATH);
    if (PathToOriginalJit == nullptr)
    {
        LogError("1st HeapAlloc failed (0x%08x)", ::GetLastError());
        return E_FAIL;
    }
    ::strcpy_s(PathToOriginalJit, MAX_PATH, pFullPathName);

    if (copyJit)
    {
        // Get a temp file location
        dwRetVal = ::GetTempPathA(MAX_PATH, lpTempPathBuffer);
        if (dwRetVal == 0)
        {
            LogError("GetTempPath failed (0x%08x)", ::GetLastError());
            return E_FAIL;
        }
        if (dwRetVal > MAX_PATH)
        {
            LogError("GetTempPath returned a path that was larger than MAX_PATH");
            return E_FAIL;
        }
        // Get a temp filename
        uRetVal = ::GetTempFileNameA(lpTempPathBuffer, "Jit", 0, szTempFileName);
        if (uRetVal == 0)
        {
            LogError("GetTempFileName failed (0x%08x)", ::GetLastError());
            return E_FAIL;
        }
        dwRetVal = (DWORD)::strlen(szTempFileName);

        // Store the full path to the temp jit
        PathToTempJit = (char*)malloc(MAX_PATH);
        if (PathToTempJit == nullptr)
        {
            LogError("2nd HeapAlloc failed 0x%08x)", ::GetLastError());
            return E_FAIL;
        }
        ::strcpy_s(PathToTempJit, MAX_PATH, szTempFileName);

        // Copy Temp File
        bRetVal = ::CopyFileA(PathToOriginalJit, PathToTempJit, FALSE);
        if (bRetVal == FALSE)
        {
            LogError("CopyFile failed (0x%08x)", ::GetLastError());
            return E_FAIL;
        }
    }
    else
        PathToTempJit = PathToOriginalJit;

#ifndef TARGET_UNIX // No file version APIs in the PAL
    // Do a quick version check
    DWORD dwHandle = 0;
    DWORD fviSize  = GetFileVersionInfoSizeA(PathToTempJit, &dwHandle);

    if ((fviSize != 0) && (dwHandle == 0))
    {
        unsigned char* fviData = new unsigned char[fviSize];
        if (GetFileVersionInfoA(PathToTempJit, dwHandle, fviSize, fviData))
        {
            UINT              size    = 0;
            VS_FIXEDFILEINFO* verInfo = nullptr;
            if (VerQueryValueA(fviData, "\\", (LPVOID*)&verInfo, &size))
            {
                if (size)
                {
                    if (verInfo->dwSignature == 0xfeef04bd)
                        LogDebug("'%s' is version %u.%u.%u.%u", PathToTempJit, (verInfo->dwFileVersionMS) >> 16,
                                 (verInfo->dwFileVersionMS) & 0xFFFF, (verInfo->dwFileVersionLS) >> 16,
                                 (verInfo->dwFileVersionLS) & 0xFFFF);
                }
            }
        }
        delete[] fviData;
    }
#endif // !TARGET_UNIX

    // Load Library
    hLib = ::LoadLibraryA(PathToTempJit);
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

        LogError("Jit Compiler has wrong version identifier");
        return -1;
    }

    icji = InitICorJitInfo(this);

    return S_OK;
}

bool JitInstance::reLoad(MethodContext* firstContext)
{
    FreeLibrary(hLib);

    // Load Library
    hLib = ::LoadLibraryA(PathToTempJit);
    if (hLib == 0)
    {
        LogError("LoadLibrary failed (0x%08x)", ::GetLastError());
        return false;
    }

    // get entry points
    pngetJit = (PgetJit)::GetProcAddress(hLib, "getJit");
    if (pngetJit == 0)
    {
        LogError("GetProcAddress 'getJit' failed (0x%08x)", ::GetLastError());
        return false;
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
            return false;
        }
    }

    pJitInstance = pngetJit();
    if (pJitInstance == nullptr)
    {
        LogError("pngetJit gave us null");
        return false;
    }

    icji = InitICorJitInfo(this);

    return true;
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

JitInstance::Result JitInstance::CompileMethod(MethodContext* MethodToCompile, int mcIndex, bool collectThroughput, MetricsSummary* metrics)
{
    struct Param : FilterSuperPMIExceptionsParam_CaptureException
    {
        JitInstance*        pThis;
        JitInstance::Result result;
        CORINFO_METHOD_INFO info;
        unsigned            flags;
        int                 mcIndex;
        bool                collectThroughput;
        MetricsSummary*     metrics;
    } param;
    param.pThis             = this;
    param.result            = RESULT_SUCCESS; // assume success
    param.flags             = 0;
    param.mcIndex           = mcIndex;
    param.collectThroughput = collectThroughput;
    param.metrics           = metrics;

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

        }
        if (jitResult == CORJIT_OK)
        {
            // capture the results of compilation
            pParam->pThis->mc->cr->recCompileMethod(&NEntryBlock, &NCodeSizeBlock, jitResult);
            pParam->pThis->mc->cr->recAllocMemCapture();
            pParam->pThis->mc->cr->recAllocGCInfoCapture();

            pParam->pThis->mc->cr->recMessageLog("Successful Compile");

            pParam->metrics->NumCodeBytes += NCodeSizeBlock;
        }
        else
        {
            LogDebug("compileMethod failed with result %d", jitResult);
            pParam->result = RESULT_ERROR;
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
            param.result = RESULT_MISSING;
        }
        else
        {
            e.ShowAndDeleteMessage();
            param.result = RESULT_ERROR;
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

    UINT64 insCountAfter = 0;
    Instrumentor_GetInsCount(&insCountAfter);

    if (param.result == RESULT_SUCCESS)
    {
        metrics->SuccessfulCompiles++;
        metrics->NumExecutedInstructions += static_cast<long long>(insCountAfter - insCountBefore);

    }
    else
    {
        metrics->FailingCompiles++;
    }

    if (param.result == RESULT_MISSING)
    {
        metrics->MissingCompiles++;
    }

    return param.result;
}

void JitInstance::timeResult(CORINFO_METHOD_INFO info, unsigned flags)
{
    uint8_t* NEntryBlock    = nullptr;
    uint32_t NCodeSizeBlock = 0;

    int sampleSize = 10;
    // Save 2 smallest times. To help reduce noise, we will look at the closest pair of these.
    unsigned __int64 time;

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

const WCHAR* JitInstance::getForceOption(const WCHAR* key)
{
    return getOption(key, forceOptions);
}

const WCHAR* JitInstance::getOption(const WCHAR* key)
{
    return getOption(key, options);
}

const WCHAR* JitInstance::getOption(const WCHAR* key, LightWeightMap<DWORD, DWORD>* options)
{
    if (options == nullptr)
    {
        return nullptr;
    }

    size_t keyLenInBytes = sizeof(WCHAR) * (wcslen(key) + 1);
    int    keyIndex      = options->Contains((unsigned char*)key, (unsigned int)keyLenInBytes);
    if (keyIndex == -1)
    {
        return nullptr;
    }

    return (const WCHAR*)options->GetBuffer(options->Get(keyIndex));
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

    Assert(environment.getIntConfigValue == nullptr && environment.getStingConfigValue == nullptr);
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

    if (environment.getStingConfigValue != nullptr)
    {
        delete environment.getStingConfigValue;
        environment.getStingConfigValue = nullptr;
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
