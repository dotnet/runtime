//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#include "standardpch.h"

#ifdef USE_COREDISTOOLS
#include "coredistools.h"
#endif // USE_COREDISTOOLS

#include "commandline.h"
#include "superpmi.h"
#include "jitinstance.h"
#include "neardiffer.h"
#include "simpletimer.h"
#include "methodcontext.h"
#include "methodcontextreader.h"
#include "mclist.h"
#include "methodstatsemitter.h"

extern int doParallelSuperPMI(CommandLine::Options& o);

// NOTE: these output status strings are parsed by parallelsuperpmi.cpp::ProcessChildStdOut().
// There must be a single, fixed prefix common to all strings, to ease the determination of when
// to parse the string fully.
const char* const g_AllFormatStringFixedPrefix  = "Loaded ";
const char* const g_SummaryFormatString         = "Loaded %d  Jitted %d  FailedCompile %d";
const char* const g_AsmDiffsSummaryFormatString = "Loaded %d  Jitted %d  FailedCompile %d  Diffs %d";

//#define SuperPMI_ChewMemory 0x7FFFFFFF //Amount of address space to consume on startup

SPMI_TARGET_ARCHITECTURE SpmiTargetArchitecture;

void SetSuperPmiTargetArchitecture(const char* targetArchitecture)
{
#ifdef _TARGET_AMD64_
    if ((targetArchitecture != nullptr) && (0 == _stricmp(targetArchitecture, "arm64")))
    {
        SpmiTargetArchitecture = SPMI_TARGET_ARCHITECTURE_ARM64;
    }
    else
    {
        SpmiTargetArchitecture = SPMI_TARGET_ARCHITECTURE_AMD64;
    }
#elif defined(_TARGET_X86_)
    SpmiTargetArchitecture = SPMI_TARGET_ARCHITECTURE_X86;
#endif
}

// This function uses PAL_TRY, so it can't be in the a function that requires object unwinding. Extracting it out here
// avoids compiler error.
//      
void InvokeNearDiffer(
    NearDiffer*             nearDiffer,
    CommandLine::Options*   o,
    MethodContext**         mc,
    CompileResult**         crl,
    int*                    matchCount,
    MethodContextReader**   reader,
    MCList*                 failingMCL,
    MCList*                 diffMCL
    )
{
    struct Param : FilterSuperPMIExceptionsParam_CaptureException
    {
        NearDiffer*             nearDiffer;
        CommandLine::Options*   o;
        MethodContext**         mc;
        CompileResult**         crl;
        int*                    matchCount;
        MethodContextReader**   reader;
        MCList*                 failingMCL;
        MCList*                 diffMCL;
    } param;
    param.nearDiffer = nearDiffer;
    param.o = o;
    param.mc = mc;
    param.crl = crl;
    param.matchCount = matchCount;
    param.reader = reader;
    param.failingMCL = failingMCL;
    param.diffMCL = diffMCL;

    PAL_TRY(Param*, pParam, &param)
    {
        if (pParam->nearDiffer->compare(*pParam->mc, *pParam->crl, (*pParam->mc)->cr))
        {
            (*pParam->matchCount)++;
        }
        else
        {
            LogIssue(ISSUE_ASM_DIFF,
                "main method %d of size %d differs", (*pParam->reader)->GetMethodContextIndex(), (*pParam->mc)->methodSize);

            //This is a difference in ASM outputs from Jit1 & Jit2 and not a playback failure
            //We will add this MC to the diffMCList if one is requested
            //Otherwise this will end up in failingMCList
            if ((*pParam->o).diffMCLFilename != nullptr)
                (*pParam->diffMCL).AddMethodToMCL((*pParam->reader)->GetMethodContextIndex());
            else if ((*pParam->o).mclFilename != nullptr)
                (*pParam->failingMCL).AddMethodToMCL((*pParam->reader)->GetMethodContextIndex());
        }
    }
    PAL_EXCEPT_FILTER(FilterSuperPMIExceptions_CaptureExceptionAndStop)
    {
        SpmiException e(&param.exceptionPointers);

        LogError("main method %d of size %d failed to load and compile correctly. EnvCnt=%d",
            (*reader)->GetMethodContextIndex(), (*mc)->methodSize, (*mc)->repEnvironmentGetCount());
        e.ShowAndDeleteMessage();
        if ((*o).mclFilename != nullptr)
            (*failingMCL).AddMethodToMCL((*reader)->GetMethodContextIndex());
    }
    PAL_ENDTRY
}

// Run superpmi. The return value is as follows:
// 0    : success
// -1   : general fatal error (e.g., failed to initialize, failed to read files)
// -2   : JIT failed to initialize
// 1    : there were compilation failures
// 2    : there were asm diffs
int __cdecl main(int argc, char* argv[])
{
#ifdef FEATURE_PAL
    if (0 != PAL_Initialize(argc, argv))
    {
        fprintf(stderr, "Error: Fail to PAL_Initialize\n");
        return -1;
    }
#endif // FEATURE_PAL

    Logger::Initialize();

    SimpleTimer st1;
    SimpleTimer st2;
    SimpleTimer st3;
    SimpleTimer st4;
    st2.Start();
    JitInstance::Result res, res2;
    HRESULT hr = E_FAIL;
    MethodContext *mc = nullptr;
    JitInstance *jit = nullptr, *jit2 = nullptr;
    MethodStatsEmitter *methodStatsEmitter = nullptr;

#ifdef SuperPMI_ChewMemory
    //Chew up the base 2gb of memory on x86... helpful in finding any places where classhandles etc are de-ref'd
    SYSTEM_INFO sSysInfo;
    GetSystemInfo(&sSysInfo);

    LPVOID lpvAddr;
#undef VirtualAlloc
    do
    {
        lpvAddr = VirtualAlloc(NULL, sSysInfo.dwPageSize, MEM_RESERVE | MEM_COMMIT, PAGE_NOACCESS);
    } while ((size_t)lpvAddr < SuperPMI_ChewMemory);
#endif

    bool collectThroughput = false;
    MCList failingMCL, diffMCL;

    CommandLine::Options o;
    if (!CommandLine::Parse(argc, argv, &o))
    {
        return -1;
    }

    if (o.parallel)
    {
        return doParallelSuperPMI(o);
    }

    SetSuperPmiTargetArchitecture(o.targetArchitecture);

    if (o.methodStatsTypes != NULL && (strchr(o.methodStatsTypes, '*') != NULL || strchr(o.methodStatsTypes, 't') != NULL || strchr(o.methodStatsTypes, 'T') != NULL))
    {
        collectThroughput = true;
    }

    LogVerbose("Using jit(%s) with input (%s)", o.nameOfJit, o.nameOfInputMethodContextFile);
    std::string indexesStr = " indexCount=";
    indexesStr += std::to_string(o.indexCount);
    indexesStr += " (";
    for (int i = 0; i < o.indexCount; i++)
    {
        indexesStr += std::to_string(o.indexes[i]);
        if (i < (o.indexCount - 1))
            indexesStr += ",";
    }
    indexesStr += ")";
    LogVerbose(indexesStr.c_str());

    if (o.methodStatsTypes != nullptr)
        LogVerbose(" EmitMethodStats-Types=%s", o.methodStatsTypes);

    if (o.hash != nullptr)
        LogVerbose(" MD5Hash=%s", o.hash);

    if (o.mclFilename != nullptr)
        LogVerbose(" failingMCList=%s", o.mclFilename);

    if (o.offset > 0 && o.increment > 0)
        LogVerbose(" offset=%d increment=%d", o.offset, o.increment);

    if (o.methodStatsTypes != nullptr)
    {
        methodStatsEmitter = new MethodStatsEmitter(o.nameOfInputMethodContextFile);
        methodStatsEmitter->SetStatsTypes(o.methodStatsTypes);
    }

    if (o.mclFilename != nullptr)
    {
        failingMCL.InitializeMCL(o.mclFilename);
    }
    if (o.diffMCLFilename != nullptr)
    {
        diffMCL.InitializeMCL(o.diffMCLFilename);
    }

    // The method context reader handles skipping any unrequested method contexts
    // Used in conjunction with an MCI file, it does a lot less work...
    MethodContextReader *reader = new MethodContextReader(o.nameOfInputMethodContextFile, o.indexes, o.indexCount, o.hash, o.offset, o.increment);
    if (!reader->isValid())
    {
        return -1;
    }

    int loadedCount = 0;
    int jittedCount = 0;
    int matchCount = 0;
    int failCount = 0;
    int index = 0;

    st1.Start();
    NearDiffer nearDiffer(o.targetArchitecture, o.useCoreDisTools);
    
    if (o.applyDiff)
    {
        nearDiffer.InitAsmDiff();
    }

    while (true)
    {
        MethodContextBuffer mcb = reader->GetNextMethodContext();
        if (mcb.Error())
        {
            return -1;
        }
        else if (mcb.allDone())
        {
            LogDebug("Done processing method contexts");
            break;
        }
        if ((loadedCount % 500 == 0) && (loadedCount > 0))
        {
            st1.Stop();
            if (o.applyDiff)
            {
                LogVerbose(" %2.1f%% - Loaded %d  Jitted %d  Matching %d  FailedCompile %d at %d per second",
                    reader->PercentComplete(),
                    loadedCount,
                    jittedCount,
                    matchCount,
                    failCount,
                    (int)((double)500 / st1.GetSeconds()));
            }
            else
            {
                LogVerbose(" %2.1f%% - Loaded %d  Jitted %d  FailedCompile %d at %d per second",
                    reader->PercentComplete(),
                    loadedCount,
                    jittedCount,
                    failCount,
                    (int)((double)500 / st1.GetSeconds()));
            }
            st1.Start();
        }

        // Now read the data into a MethodContext. This could throw if the method context data is corrupt.

        loadedCount++;
        if (!MethodContext::Initialize(loadedCount, mcb.buff, mcb.size, &mc))
            return -1;

        if (jit == nullptr)
        {
            SimpleTimer st4;

            jit = JitInstance::InitJit(o.nameOfJit, o.breakOnAssert, &st4, mc);
            if (jit == nullptr)
            {
                // InitJit already printed a failure message
                return -2;
            }

            if (o.nameOfJit2 != nullptr)
            {
                jit2 = JitInstance::InitJit(o.nameOfJit2, o.breakOnAssert, &st4, mc);
                if (jit2 == nullptr)
                {
                    // InitJit already printed a failure message
                    return -2;
                }
            }
        }

        // I needed to reason about what crl contains at any point in time
        // Here is my guess based on reading the code so far
        // crl initially contains the CompileResult from the MCH file
        // However if we have a second jit it has the CompileResult from Jit1
        CompileResult *crl = mc->cr;

        mc->cr = new CompileResult();
        mc->originalCR = crl;

        jittedCount++;
        st3.Start();
        res = jit->CompileMethod(mc, reader->GetMethodContextIndex(), collectThroughput);
        st3.Stop();
        LogDebug("Method %d compiled in %fms, result %d", reader->GetMethodContextIndex(), st3.GetMilliseconds(), res);

        if ((res == JitInstance::RESULT_SUCCESS) && Logger::IsLogLevelEnabled(LOGLEVEL_DEBUG))
        {
            mc->cr->dumpToConsole(); // Dump the compile results if doing debug logging
        }

        if (o.nameOfJit2 != nullptr)
        {
            // Lets get the results for the 2nd JIT
            // We will save the first JIT's CR to save space for the 2nd JIT CR
            // Note that the recorded CR is still stored in MC->originalCR
            crl = mc->cr;
            mc->cr = new CompileResult();

            st4.Start();
            res2 = jit2->CompileMethod(mc, reader->GetMethodContextIndex(), collectThroughput);
            st4.Stop();
            LogDebug("Method %d compiled by JIT2 in %fms, result %d", reader->GetMethodContextIndex(), st4.GetMilliseconds(), res2);

            if ((res2 == JitInstance::RESULT_SUCCESS) && Logger::IsLogLevelEnabled(LOGLEVEL_DEBUG))
            {
                mc->cr->dumpToConsole(); // Dump the compile results if doing debug logging
            }

            if (res2 == JitInstance::RESULT_ERROR)
            {
                LogError("JIT2 main method %d of size %d failed to load and compile correctly. EnvCnt=%d",
                    reader->GetMethodContextIndex(), mc->methodSize, mc->repEnvironmentGetCount());
            }

            // Methods that don't compile due to missing JIT-EE information
            // should still be added to the failing MC list.
            // However, we will not add this MC# if JIT1 also failed, Else there will be duplicate logging
            if ((res == JitInstance::RESULT_SUCCESS) &&
                (res2 != JitInstance::RESULT_SUCCESS) &&
                (o.mclFilename != nullptr))
            {
                failingMCL.AddMethodToMCL(reader->GetMethodContextIndex());
            }
        }

        if (res == JitInstance::RESULT_SUCCESS)
        {
            if (collectThroughput)
            {
                if (o.nameOfJit2 != nullptr && res2 == JitInstance::RESULT_SUCCESS)
                {
                    //TODO-Bug?: bug in getting the lowest cycle time??
                    ULONGLONG dif1, dif2, dif3, dif4;
                    dif1 = (jit->times[0] - jit2->times[0]) * (jit->times[0] - jit2->times[0]);
                    dif2 = (jit->times[0] - jit2->times[1]) * (jit->times[0] - jit2->times[1]);
                    dif3 = (jit->times[1] - jit2->times[0]) * (jit->times[1] - jit2->times[0]);
                    dif4 = (jit->times[1] - jit2->times[1]) * (jit->times[1] - jit2->times[1]);

                    if (dif1 < dif2)
                    {
                        if (dif3 < dif4)
                        {
                            if (dif1 < dif3)
                            {
                                crl->clockCyclesToCompile = jit->times[0];
                                mc->cr->clockCyclesToCompile = jit2->times[0];
                            }
                            else
                            {
                                crl->clockCyclesToCompile = jit->times[1];
                                mc->cr->clockCyclesToCompile = jit2->times[0];
                            }
                        }
                        else
                        {
                            if (dif1 < dif4)
                            {
                                crl->clockCyclesToCompile = jit->times[0];
                                mc->cr->clockCyclesToCompile = jit2->times[0];
                            }
                            else
                            {
                                crl->clockCyclesToCompile = jit->times[1];
                                mc->cr->clockCyclesToCompile = jit2->times[1];
                            }
                        }
                    }
                    else
                    {
                        if (dif3 < dif4)
                        {
                            if (dif2 < dif3)
                            {
                                crl->clockCyclesToCompile = jit->times[0];
                                mc->cr->clockCyclesToCompile = jit2->times[1];
                            }
                            else
                            {
                                crl->clockCyclesToCompile = jit->times[1];
                                mc->cr->clockCyclesToCompile = jit2->times[0];
                            }
                        }
                        else
                        {
                            if (dif2 < dif4)
                            {
                                crl->clockCyclesToCompile = jit->times[0];
                                mc->cr->clockCyclesToCompile = jit2->times[1];
                            }
                            else
                            {
                                crl->clockCyclesToCompile = jit->times[1];
                                mc->cr->clockCyclesToCompile = jit2->times[1];
                            }
                        }
                    }

                    if (methodStatsEmitter != nullptr)
                    {
                        methodStatsEmitter->Emit(reader->GetMethodContextIndex(), mc, crl->clockCyclesToCompile, mc->cr->clockCyclesToCompile);
                    }
                }
                else
                {
                    if (jit->times[0] > jit->times[1])
                        mc->cr->clockCyclesToCompile = jit->times[1];
                    else
                        mc->cr->clockCyclesToCompile = jit->times[0];
                    if (methodStatsEmitter != nullptr)
                    {
                        methodStatsEmitter->Emit(reader->GetMethodContextIndex(), mc, mc->cr->clockCyclesToCompile, 0);
                    }
                }
            }

            if (!collectThroughput && methodStatsEmitter != nullptr)
            {
                //We have a separate call to Emit for collectThroughput
                methodStatsEmitter->Emit(reader->GetMethodContextIndex(), mc, -1, -1);
            }

            if (o.applyDiff)
            {
                // We need at least two compile results to diff: they can either both come from JIT
                // invocations, or one can be loaded from the method context file.

                // We need to check both CompileResults to ensure we have a valid CR
                if (crl->AllocMem == nullptr || mc->cr->AllocMem == nullptr)
                {
                    LogError("method %d is missing a compileResult, cannot do diffing", reader->GetMethodContextIndex());

                    // If we are here this means that either we have 2 Jits and the second Jit failed to compile
                    // Or we have single Jit and the MethodContext doesn't have an originalCR
                    // In both cases we don't need to add this to the MCList again
                }
                else
                {
                    InvokeNearDiffer(&nearDiffer, &o, &mc, &crl, &matchCount, &reader, &failingMCL, &diffMCL);
                }
            }
        }
        else
        {
            failCount++;

            if (o.mclFilename != nullptr)
                failingMCL.AddMethodToMCL(reader->GetMethodContextIndex());

            // The following only apply specifically to failures caused by errors (as opposed
            // to, for instance, failures caused by missing JIT-EE details).
            if (res == JitInstance::RESULT_ERROR)
            {
                LogError("main method %d of size %d failed to load and compile correctly. EnvCnt=%d", reader->GetMethodContextIndex(), mc->methodSize, mc->repEnvironmentGetCount());
                if ((o.reproName != nullptr) && (o.indexCount == -1))
                {
                    char buff[500];
                    sprintf_s(buff, 500, "%s-%d.mc", o.reproName, reader->GetMethodContextIndex());
                    HANDLE hFileOut = CreateFileA(buff, GENERIC_WRITE, 0, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN, NULL);
                    if (hFileOut == INVALID_HANDLE_VALUE)
                    {
                        LogError("Failed to open output '%s'. GetLastError()=%u", buff, GetLastError());
                        return -1;
                    }
                    mc->saveToFile(hFileOut);
                    if (CloseHandle(hFileOut) == 0)
                    {
                        LogError("CloseHandle for output file failed. GetLastError()=%u", GetLastError());
                        return -1;
                    }
                    LogInfo("Wrote out repro to '%s'", buff);
                }
                if (o.breakOnError)
                {
                    if (o.indexCount == -1)
                        LogInfo("HINT: to repro add '/c %d' to cmdline", reader->GetMethodContextIndex());
                    __debugbreak();
                }
            }
        }

        delete crl;
        delete mc;
    }
    delete reader;

    int result = 0;

    // NOTE: these output status strings are parsed by parallelsuperpmi.cpp::ProcessChildStdOut().
    if (o.applyDiff)
    {
        LogInfo(g_AsmDiffsSummaryFormatString, loadedCount, jittedCount, failCount, jittedCount - failCount - matchCount);

        if (matchCount != jittedCount)
        {
            result = 2;
        }
    }
    else
    {
        LogInfo(g_SummaryFormatString, loadedCount, jittedCount, failCount);
    }

    // Failure to JIT overrides diffs for the error code.
    if (failCount > 0)
    {
        result = 1;
    }

    st2.Stop();
    LogVerbose("Total time: %fms", st2.GetMilliseconds());

    if (methodStatsEmitter != nullptr)
    {
        delete methodStatsEmitter;
    }

    if (o.mclFilename != nullptr)
    {
        failingMCL.CloseMCL();
    }
    if (o.diffMCLFilename != nullptr)
    {
        diffMCL.CloseMCL();
    }
    Logger::Shutdown();
    return result;
}
