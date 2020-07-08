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

    // For each native frame
    while (true)
    {
        uint64_t ip = 0, sp = 0;
        GetFrameLocation(pContext, &ip, &sp);

        TRACE("Unwind: sp %" PRIA PRIx64 " ip %" PRIA PRIx64 "\n", sp, ip);
        if (ip == 0 || sp <= previousSp) {
            break;
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
    uint64_t startAddress;
    size_t size;

#if defined(__aarch64__)
    startAddress = MCREG_Sp(m_gpRegisters) & PAGE_MASK;
#elif defined(__arm__)
    startAddress = m_gpRegisters.ARM_sp & PAGE_MASK;
#else
    startAddress = m_gpRegisters.rsp & PAGE_MASK;
#endif
    size = 4 * PAGE_SIZE;

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

void
ThreadInfo::GetThreadContext(uint32_t flags, CONTEXT* context) const
{
    context->ContextFlags = flags;
#if defined(__x86_64__)
    if ((flags & CONTEXT_CONTROL) == CONTEXT_CONTROL)
    {
        context->Rbp = m_gpRegisters.rbp;
        context->Rip = m_gpRegisters.rip;
        context->SegCs = m_gpRegisters.cs;
        context->EFlags = m_gpRegisters.eflags;
        context->SegSs = m_gpRegisters.ss;
        context->Rsp = m_gpRegisters.rsp;
    }
    if ((flags & CONTEXT_INTEGER) == CONTEXT_INTEGER)
    {
        context->Rdi = m_gpRegisters.rdi;
        context->Rsi = m_gpRegisters.rsi;
        context->Rbx = m_gpRegisters.rbx;
        context->Rdx = m_gpRegisters.rdx;
        context->Rcx = m_gpRegisters.rcx;
        context->Rax = m_gpRegisters.rax;
        context->R8 = m_gpRegisters.r8;
        context->R9 = m_gpRegisters.r9;
        context->R10 = m_gpRegisters.r10;
        context->R11 = m_gpRegisters.r11;
        context->R12 = m_gpRegisters.r12;
        context->R13 = m_gpRegisters.r13;
        context->R14 = m_gpRegisters.r14;
        context->R15 = m_gpRegisters.r15;
    }
    if ((flags & CONTEXT_SEGMENTS) == CONTEXT_SEGMENTS)
    {
        context->SegDs = m_gpRegisters.ds;
        context->SegEs = m_gpRegisters.es;
        context->SegFs = m_gpRegisters.fs;
        context->SegGs = m_gpRegisters.gs;
    }
    if ((flags & CONTEXT_FLOATING_POINT) == CONTEXT_FLOATING_POINT)
    {
        context->FltSave.ControlWord = m_fpRegisters.cwd;
        context->FltSave.StatusWord = m_fpRegisters.swd;
        context->FltSave.TagWord = m_fpRegisters.ftw;
        context->FltSave.ErrorOpcode = m_fpRegisters.fop;

        context->FltSave.ErrorOffset = FPREG_ErrorOffset(m_fpRegisters);
        context->FltSave.ErrorSelector = FPREG_ErrorSelector(m_fpRegisters);
        context->FltSave.DataOffset = FPREG_DataOffset(m_fpRegisters);
        context->FltSave.DataSelector = FPREG_DataSelector(m_fpRegisters);

        context->FltSave.MxCsr = m_fpRegisters.mxcsr;
        context->FltSave.MxCsr_Mask = m_fpRegisters.mxcr_mask;

        assert(sizeof(context->FltSave.FloatRegisters) == sizeof(m_fpRegisters.st_space));
        memcpy(context->FltSave.FloatRegisters, m_fpRegisters.st_space, sizeof(context->FltSave.FloatRegisters));

        assert(sizeof(context->FltSave.XmmRegisters) == sizeof(m_fpRegisters.xmm_space));
        memcpy(context->FltSave.XmmRegisters, m_fpRegisters.xmm_space, sizeof(context->FltSave.XmmRegisters));
    }
    // TODO: debug registers?
#elif defined(__aarch64__)
    if ((flags & CONTEXT_CONTROL) == CONTEXT_CONTROL)
    {
        context->Fp = MCREG_Fp(m_gpRegisters);
        context->Lr = MCREG_Lr(m_gpRegisters);
        context->Sp = MCREG_Sp(m_gpRegisters);
        context->Pc = MCREG_Pc(m_gpRegisters);
        context->Cpsr = MCREG_Cpsr(m_gpRegisters);
    }
    if ((flags & CONTEXT_INTEGER) == CONTEXT_INTEGER)
    {
        assert(sizeof(m_gpRegisters.regs) == (sizeof(context->X) + sizeof(context->Fp) + sizeof(context->Lr)));
        memcpy(context->X, m_gpRegisters.regs, sizeof(context->X));
    }
    if ((flags & CONTEXT_FLOATING_POINT) == CONTEXT_FLOATING_POINT)
    {
        assert(sizeof(m_fpRegisters.vregs) == sizeof(context->V));
        memcpy(context->V, m_fpRegisters.vregs, sizeof(context->V));
        context->Fpcr = m_fpRegisters.fpcr;
        context->Fpsr = m_fpRegisters.fpsr;
    }
#elif defined(__arm__)
    if ((flags & CONTEXT_CONTROL) == CONTEXT_CONTROL)
    {
        context->Sp = m_gpRegisters.ARM_sp;
        context->Lr = m_gpRegisters.ARM_lr;
        context->Pc = m_gpRegisters.ARM_pc;
        context->Cpsr = m_gpRegisters.ARM_cpsr;
    }
    if ((flags & CONTEXT_INTEGER) == CONTEXT_INTEGER)
    {
        context->R0 = m_gpRegisters.ARM_r0;
        context->R1 = m_gpRegisters.ARM_r1;
        context->R2 = m_gpRegisters.ARM_r2;
        context->R3 = m_gpRegisters.ARM_r3;
        context->R4 = m_gpRegisters.ARM_r4;
        context->R5 = m_gpRegisters.ARM_r5;
        context->R6 = m_gpRegisters.ARM_r6;
        context->R7 = m_gpRegisters.ARM_r7;
        context->R8 = m_gpRegisters.ARM_r8;
        context->R9 = m_gpRegisters.ARM_r9;
        context->R10 = m_gpRegisters.ARM_r10;
        context->R11 = m_gpRegisters.ARM_fp;
        context->R12 = m_gpRegisters.ARM_ip;
    }
    if ((flags & CONTEXT_FLOATING_POINT) == CONTEXT_FLOATING_POINT)
    {
#if defined(__VFP_FP__) && !defined(__SOFTFP__)
        context->Fpscr = m_vfpRegisters.fpscr;

        assert(sizeof(context->D) == sizeof(m_vfpRegisters.fpregs));
        memcpy(context->D, m_vfpRegisters.fpregs, sizeof(context->D));
#endif
    }
#else
#error Platform not supported
#endif
}
