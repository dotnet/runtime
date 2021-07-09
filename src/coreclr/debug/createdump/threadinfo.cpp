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

        if (ip == 0 || sp <= previousSp) {
            TRACE_VERBOSE("Unwind: sp not increasing or ip == 0 sp %p ip %p\n", (void*)sp, (void*)ip);
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
                TRACE("Unwind: same ip %p over 1000 times\n", (void*)ip);
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
        uint64_t baseAddress = m_crashInfo.GetBaseAddressFromAddress(ip);
        if (baseAddress == 0) {
            TRACE_VERBOSE("Unwind: module base not found ip %p\n", (void*)ip);
            break;
        }

        // Unwind the native frame adding all the memory accessed to the core dump via the read memory adapter.
        ULONG64 functionStart;
        if (!PAL_VirtualUnwindOutOfProc(pContext, nullptr, &functionStart, baseAddress, ReadMemoryAdapter)) {
            TRACE("Unwind: PAL_VirtualUnwindOutOfProc returned false\n");
            break;
        }

        if (m_crashInfo.GatherFrames())
        {
            // Add stack frame for the crash report. The function start returned by the unwinder is for
            // "ip" and not for the new context returned in pContext.
            StackFrame frame(baseAddress, ip, sp, ip - functionStart);
            AddStackFrame(frame);
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
            m_managed = true;

            ReleaseHolder<IXCLRDataExceptionState> pException;
            if (SUCCEEDED(pTask->GetCurrentExceptionState(&pException)))
            {
                TRACE("Unwind: found managed exception\n");

                ReleaseHolder<IXCLRDataValue> pExceptionValue;
                if (SUCCEEDED(pException->GetManagedObject(&pExceptionValue)))
                {
                    CLRDATA_ADDRESS exceptionObject;
                    if (SUCCEEDED(pExceptionValue->GetAddress(&exceptionObject)))
                    {
                        m_exceptionObject = exceptionObject;
                        TRACE("Unwind: exception object %p\n", (void*)exceptionObject);
                    }
                    ReleaseHolder<IXCLRDataTypeInstance> pExceptionType;
                    if (SUCCEEDED(pExceptionValue->GetType(&pExceptionType)))
                    {
                        ArrayHolder<WCHAR> typeName = new WCHAR[MAX_LONGPATH + 1];
                        if (SUCCEEDED(pExceptionType->GetName(0, MAX_LONGPATH, nullptr, typeName.GetPtr())))
                        {
                            m_exceptionType = FormatString("%S", typeName.GetPtr());
                            TRACE("Unwind: exception type %s\n", m_exceptionType.c_str());
                        }
                    }
                }
            }

            // For each managed stack frame
            do
            {
                // Get the managed stack frame context
                if (pStackwalk->GetContext(CONTEXT_ALL, sizeof(context), nullptr, (BYTE *)&context) != S_OK) {
                    TRACE("Unwind: stack walker GetContext FAILED\n");
                    break;
                }

                // Get and save more detail information for the crash report if enabled
                if (m_crashInfo.GatherFrames())
                {
                    GatherStackFrames(&context, pStackwalk);
                }

                // Unwind all the native frames after the managed frame
                UnwindNativeFrames(&context);

            } while (pStackwalk->Next() == S_OK);
        }
    }

    return true;
}

void
ThreadInfo::GatherStackFrames(CONTEXT* pContext, IXCLRDataStackWalk* pStackwalk)
{
    uint64_t ip = 0, sp = 0;
    GetFrameLocation(pContext, &ip, &sp);

    uint64_t moduleAddress = 0;
    mdMethodDef token = 0;
    uint32_t nativeOffset = 0;
    uint32_t ilOffset = 0;

    ReleaseHolder<IXCLRDataFrame> pFrame;
    if (SUCCEEDED(pStackwalk->GetFrame(&pFrame)))
    {
        CLRDataSimpleFrameType simpleType;
        CLRDataDetailedFrameType detailedType;
        pFrame->GetFrameType(&simpleType, &detailedType);

        if ((simpleType & (CLRDATA_SIMPFRAME_MANAGED_METHOD | CLRDATA_SIMPFRAME_RUNTIME_MANAGED_CODE)) != 0)
        {
            ReleaseHolder<IXCLRDataMethodInstance> pMethod;
            if (SUCCEEDED(pFrame->GetMethodInstance(&pMethod)))
            {
                ReleaseHolder<IXCLRDataModule> pModule;
                if (SUCCEEDED(pMethod->GetTokenAndScope(&token, &pModule)))
                {
                    DacpGetModuleData moduleData;
                    if (SUCCEEDED(moduleData.Request(pModule)))
                    {
                        moduleAddress = moduleData.LoadedPEAddress;
                    }
                    else
                    {
                        TRACE("Unwind: DacpGetModuleData.Request sp %p ip %p FAILED\n", (void*)sp, (void*)ip);
                    }
                }
                else
                {
                    TRACE("Unwind: GetTokenAndScope sp %p ip %p FAILED\n", (void*)sp, (void*)ip);
                }
                if (FAILED(pMethod->GetILOffsetsByAddress(ip, 1, NULL, &ilOffset)))
                {
                    TRACE("Unwind: GetILOffsetsByAddress sp %p ip %p FAILED\n", (void*)sp, (void*)ip);
                }
                CLRDATA_ADDRESS startAddress;
                if (SUCCEEDED(pMethod->GetRepresentativeEntryAddress(&startAddress)))
                {
                    nativeOffset = ip - startAddress;
                }
                else
                {
                    TRACE("Unwind: GetRepresentativeEntryAddress sp %p ip %p FAILED\n", (void*)sp, (void*)ip);
                }
            }
            else
            {
                TRACE("Unwind: GetMethodInstance sp %p ip %p FAILED\n", (void*)sp, (void*)ip);
            }
        }
        else
        {
            TRACE("Unwind: simpleType %08x detailedType %08x\n", simpleType, detailedType);
        }
    }

    // Add managed stack frame for the crash info notes
    StackFrame frame(moduleAddress, ip, sp, nativeOffset, token, ilOffset);
    AddStackFrame(frame);
}

void
ThreadInfo::AddStackFrame(const StackFrame& frame)
{
    std::pair<std::set<StackFrame>::iterator,bool> result = m_frames.insert(frame);
    if (result.second)
    {
        TRACE("Unwind: sp %p ip %p off %08x mod %p%c\n",
            (void*)frame.StackPointer(),
            (void*)frame.ReturnAddress(),
            frame.NativeOffset(),
            (void*)frame.ModuleAddress(),
            frame.IsManaged() ? '*' : ' ');
    }
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
