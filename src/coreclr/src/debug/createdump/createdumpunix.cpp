// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "createdump.h"

//
// The Linux/MacOS create dump code
//
bool
CreateDump(const char* dumpPath, int pid, MINIDUMP_TYPE minidumpType)
{
    ReleaseHolder<CrashInfo> crashInfo = new CrashInfo(pid);
    DumpWriter dumpWriter(*crashInfo);
    bool result = false;

    // Initialize the crash info 
    if (!crashInfo->Initialize())
    {
        goto exit;
    }
    printf("Process %d %s\n", crashInfo->Pid(), crashInfo->Name().c_str());

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
    if (!dumpWriter.OpenDump(dumpPath))
    {
        goto exit;
    }
    if (!dumpWriter.WriteDump())
    {
        goto exit;
    }
    result = true;
exit:
    crashInfo->CleanupAndResumeProcess();
    return result;
}
