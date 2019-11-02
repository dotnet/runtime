// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "createdump.h"

bool g_diagnostics = false;

//
// The common create dump code
//
bool
CreateDumpCommon(const char* dumpPathTemplate, MINIDUMP_TYPE minidumpType, CrashInfo* crashInfo)
{
    ReleaseHolder<DumpWriter> dumpWriter = new DumpWriter(*crashInfo);
    bool result = false;

    ArrayHolder<char> dumpPath = new char[PATH_MAX];
    snprintf(dumpPath, PATH_MAX, dumpPathTemplate, crashInfo->Pid());

    const char* dumpType = "minidump";
    switch (minidumpType)
    {
        case MiniDumpWithPrivateReadWriteMemory:
            dumpType = "minidump with heap";
            break;

        case MiniDumpFilterTriage:
            dumpType = "triage minidump";
            break;

        case MiniDumpWithFullMemory:
            dumpType = "full dump";
            break;

        default:
            break;
    }
    printf("Writing %s to file %s\n", dumpType, (char*)dumpPath);

    // Suspend all the threads in the target process and build the list of threads
    if (!crashInfo->EnumerateAndSuspendThreads())
    {
        goto exit;
    }
    // Gather all the info about the process, threads (registers, etc.) and memory regions
    if (!crashInfo->GatherCrashInfo(minidumpType))
    {
        goto exit;
    }
    if (!dumpWriter->OpenDump(dumpPath))
    {
        goto exit;
    }
    if (!dumpWriter->WriteDump())
    {
        goto exit;
    }
    result = true;
exit:
    crashInfo->ResumeThreads();
    return result;
}

//
// Entry point for SOS createdump command
//
bool
CreateDumpForSOS(const char* programPath, const char* dumpPathTemplate, pid_t pid, MINIDUMP_TYPE minidumpType, ICLRDataTarget* dataTarget)
{
    ReleaseHolder<CrashInfo> crashInfo = new CrashInfo(pid, dataTarget, true);
    return CreateDumpCommon(dumpPathTemplate, minidumpType, crashInfo);
}
