// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "createdump.h"

#ifndef THUMB_CODE
#define THUMB_CODE 1
#endif

#ifndef __GLIBC__
typedef int __ptrace_request;
#endif

extern CrashInfo* g_crashInfo;

// Helper for UnwindNativeFrames
static void
GetFrameLocation(CONTEXT* pContext, uint64_t* ip, uint64_t* sp)
{
#if defined(__x86_64__)
    *ip = pContext->Rip;
    *sp = pContext->Rsp;
#elif defined(__i386__)
    *ip = pContext->Eip;
    *sp = pContext->Esp;
#elif defined(__aarch64__)
    *ip = pContext->Pc;
    *sp = pContext->Sp;
#elif defined(__arm__)
    *ip = pContext->Pc & ~THUMB_CODE;
    *sp = pContext->Sp;
#endif
}

// Helper for UnwindNativeFrames
static BOOL
ReadMemoryAdapter(PVOID address, PVOID buffer, SIZE_T size)
{
    return g_crashInfo->ReadMemory(address, buffer, size);
}

void
ThreadInfo::UnwindNativeFrames(CONTEXT* pContext)
{
    uint64_t previousSp = 0;
    uint64_t previousIp = 0;
    int ipMatchCount = 0;

    // For each native frame, add a page around the IP and any unwind info not already
    // added in VisitProgramHeader (Linux) and VisitSection (MacOS) to the dump.
    while (true)
    {
        uint64_t ip = 0, sp = 0;
        GetFrameLocation(pContext, &ip, &sp);

        TRACE("Unwind: sp %" PRIA PRIx64 " ip %" PRIA PRIx64 "\n", sp, ip);
        if (ip == 0 || sp <= previousSp) {
            break;
        }
        // Break out of the endless loop if the IP matches over a 1000 times. This is a fallback
        // behavior of libunwind when the module the IP is in doesn't have unwind info and for
        // simple stack overflows. The stack memory is added to the dump in GetThreadStack and
        // it isn't necessary to add the same IP page over and over again. The only place this
        // check won't catch is the stack overflow case that repeats a sequence of IPs over and
        // over.
        if (ip == previousIp)
        {
            if (ipMatchCount++ > 1000)
            {
                TRACE("Unwind: same ip %" PRIA PRIx64 " over 1000 times\n", ip);
                break;
            }
        }
        else
        {
            ipMatchCount = 0;
        }

        // Add two pages around the instruction pointer to the core dump
        m_crashInfo.InsertMemoryRegion(ip - PAGE_SIZE, PAGE_SIZE * 2);

        // Look up the ip address to get the module base address
        uint64_t baseAddress = m_crashInfo.GetBaseAddress(ip);
        if (baseAddress == 0) {
            TRACE("Unwind: module base not found ip %" PRIA PRIx64 "\n", ip);
            break;
        }

        // Unwind the native frame adding all the memory accessed to the
        // core dump via the read memory adapter.
        if (!PAL_VirtualUnwindOutOfProc(pContext, nullptr, baseAddress, ReadMemoryAdapter)) {
            TRACE("Unwind: PAL_VirtualUnwindOutOfProc returned false\n");
            break;
        }
        previousSp = sp;
        previousIp = ip;
    }
}

bool
ThreadInfo::UnwindThread(IXCLRDataProcess* pClrDataProcess)
{
    TRACE("Unwind: thread %04x\n", Tid());

    // Get starting native context for the thread
    CONTEXT context;
    GetThreadContext(CONTEXT_ALL, &context);

    // Unwind the native frames at the top of the stack
    UnwindNativeFrames(&context);

    if (pClrDataProcess != nullptr)
    {
        ReleaseHolder<IXCLRDataTask> pTask;
        ReleaseHolder<IXCLRDataStackWalk> pStackwalk;

        // Get the managed stack walker for this thread
        if (SUCCEEDED(pClrDataProcess->GetTaskByOSThreadID(Tid(), &pTask)))
        {
            pTask->CreateStackWalk(
                CLRDATA_SIMPFRAME_UNRECOGNIZED |
                CLRDATA_SIMPFRAME_MANAGED_METHOD |
                CLRDATA_SIMPFRAME_RUNTIME_MANAGED_CODE |
                CLRDATA_SIMPFRAME_RUNTIME_UNMANAGED_CODE,
                &pStackwalk);
        }

        // For each managed frame (if any)
        if (pStackwalk != nullptr)
        {
            TRACE("Unwind: managed frames\n");
            do
            {
                // Get the managed stack frame context
                if (pStackwalk->GetContext(CONTEXT_ALL, sizeof(context), nullptr, (BYTE *)&context) != S_OK) {
                    TRACE("Unwind: stack walker GetContext FAILED\n");
                    break;
                }

                // Unwind all the native frames after the managed frame
                UnwindNativeFrames(&context);

            } while (pStackwalk->Next() == S_OK);
        }
    }

    return true;
}

void
ThreadInfo::GetThreadStack()
{
    uint64_t startAddress = GetStackPointer() & PAGE_MASK;
    size_t size = 4 * PAGE_SIZE;

    if (startAddress != 0)
    {
        MemoryRegion search(0, startAddress, startAddress + PAGE_SIZE);
        const MemoryRegion* region = CrashInfo::SearchMemoryRegions(m_crashInfo.OtherMappings(), search);
        if (region != nullptr) {

            // Use the mapping found for the size of the thread's stack
            size = region->EndAddress() - startAddress;

            if (g_diagnostics)
            {
                TRACE("Thread %04x stack found in other mapping (size %08zx): ", m_tid, size);
                region->Trace();
            }
        }
        m_crashInfo.InsertMemoryRegion(startAddress, size);
    }
    else
    {
        TRACE("Thread %04x null stack pointer\n", m_tid);
    }
}
