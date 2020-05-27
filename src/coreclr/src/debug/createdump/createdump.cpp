// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "createdump.h"

bool g_diagnostics = false;

//
// The Linux create dump code
//
bool
CreateDump(const char* dumpPath, int pid, MINIDUMP_TYPE minidumpType)
{
    ReleaseHolder<DumpDataTarget> dataTarget = new DumpDataTarget(pid);
    ReleaseHolder<CrashInfo> crashInfo = new CrashInfo(pid, dataTarget, false);
    ReleaseHolder<DumpWriter> dumpWriter = new DumpWriter(*crashInfo);
    bool result = false;

    // The initialize the data target's ReadVirtual support (opens /proc/$pid/mem)
    if (!dataTarget->Initialize(crashInfo))
    {
        goto exit;
    }
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
