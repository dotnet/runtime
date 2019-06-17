// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "createdump.h"
#include <asm/ptrace.h>

#if defined(__aarch64__)
// See src/pal/src/include/pal/context.h
#define MCREG_Fp(mc)      ((mc).regs[29])
#define MCREG_Lr(mc)      ((mc).regs[30])
#define MCREG_Sp(mc)      ((mc).sp)
#define MCREG_Pc(mc)      ((mc).pc)
#define MCREG_Cpsr(mc)    ((mc).pstate)
#endif

#ifndef THUMB_CODE
#define THUMB_CODE 1
#endif

#ifndef __GLIBC__
typedef int __ptrace_request;
#endif

#define FPREG_ErrorOffset(fpregs) *(DWORD*)&((fpregs).rip)
#define FPREG_ErrorSelector(fpregs) *(((WORD*)&((fpregs).rip)) + 2)
#define FPREG_DataOffset(fpregs) *(DWORD*)&((fpregs).rdp)
#define FPREG_DataSelector(fpregs) *(((WORD*)&((fpregs).rdp)) + 2)

extern CrashInfo* g_crashInfo;

ThreadInfo::ThreadInfo(pid_t tid) :
    m_tid(tid)
{
}

ThreadInfo::~ThreadInfo()
{
}

bool
ThreadInfo::Initialize(ICLRDataTarget* pDataTarget)
{
    if (!CrashInfo::GetStatus(m_tid, &m_ppid, &m_tgid, nullptr)) 
    {
        return false;
    }
    if (pDataTarget != nullptr)
    {
        if (!GetRegistersWithDataTarget(pDataTarget))
        {
            return false;
        }
    }
    else {
        if (!GetRegistersWithPTrace())
        {
            return false;
        }
    }

#if defined(__aarch64__)
    TRACE("Thread %04x PC %016llx SP %016llx\n", m_tid, (unsigned long long)MCREG_Pc(m_gpRegisters), (unsigned long long)MCREG_Sp(m_gpRegisters));
#elif defined(__arm__)
    TRACE("Thread %04x PC %08lx SP %08lx\n", m_tid, (unsigned long)m_gpRegisters.ARM_pc, (unsigned long)m_gpRegisters.ARM_sp);
#elif defined(__x86_64__)
    TRACE("Thread %04x RIP %016llx RSP %016llx\n", m_tid, (unsigned long long)m_gpRegisters.rip, (unsigned long long)m_gpRegisters.rsp);
#else
#error "Unsupported architecture"
#endif
    return true;
}

void
ThreadInfo::ResumeThread()
{
    if (ptrace(PTRACE_DETACH, m_tid, nullptr, nullptr) != -1)
    {
        int waitStatus;
        waitpid(m_tid, &waitStatus, __WALL);
    }
}

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
ThreadInfo::UnwindNativeFrames(CrashInfo& crashInfo, CONTEXT* pContext)
{
    // For each native frame
    while (true)
    {
        uint64_t ip = 0, sp = 0;
        GetFrameLocation(pContext, &ip, &sp);

        TRACE("Unwind: sp %" PRIA PRIx64 " ip %" PRIA PRIx64 "\n", sp, ip);
        if (ip == 0) {
            break;
        }
        // Add two pages around the instruction pointer to the core dump
        crashInfo.InsertMemoryRegion(ip - PAGE_SIZE, PAGE_SIZE * 2);

        // Look up the ip address to get the module base address
        uint64_t baseAddress = crashInfo.GetBaseAddress(ip);
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
    }
}

bool
ThreadInfo::UnwindThread(CrashInfo& crashInfo, IXCLRDataProcess* pClrDataProcess)
{
    TRACE("Unwind: thread %04x\n", Tid());

    // Get starting native context for the thread
    CONTEXT context;
    GetThreadContext(CONTEXT_ALL, &context);

    // Unwind the native frames at the top of the stack
    UnwindNativeFrames(crashInfo, &context);

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
                UnwindNativeFrames(crashInfo, &context);

            } while (pStackwalk->Next() == S_OK);
        }
    }

    return true;
}

bool 
ThreadInfo::GetRegistersWithPTrace()
{
#if defined(__aarch64__)
    struct iovec gpRegsVec = { &m_gpRegisters, sizeof(m_gpRegisters) };
    if (ptrace((__ptrace_request)PTRACE_GETREGSET, m_tid, NT_PRSTATUS, &gpRegsVec) == -1)
    {
        fprintf(stderr, "ptrace(PTRACE_GETREGSET, %d, NT_PRSTATUS) FAILED %d (%s)\n", m_tid, errno, strerror(errno));
        return false;
    }
    assert(sizeof(m_gpRegisters) == gpRegsVec.iov_len);

    struct iovec fpRegsVec = { &m_fpRegisters, sizeof(m_fpRegisters) };
    if (ptrace((__ptrace_request)PTRACE_GETREGSET, m_tid, NT_FPREGSET, &fpRegsVec) == -1)
    {
        fprintf(stderr, "ptrace(PTRACE_GETREGSET, %d, NT_FPREGSET) FAILED %d (%s)\n", m_tid, errno, strerror(errno));
        return false;
    }
    assert(sizeof(m_fpRegisters) == fpRegsVec.iov_len);
#else
    if (ptrace((__ptrace_request)PTRACE_GETREGS, m_tid, nullptr, &m_gpRegisters) == -1)
    {
        fprintf(stderr, "ptrace(GETREGS, %d) FAILED %d (%s)\n", m_tid, errno, strerror(errno));
        return false;
    }
    if (ptrace((__ptrace_request)PTRACE_GETFPREGS, m_tid, nullptr, &m_fpRegisters) == -1)
    {
        fprintf(stderr, "ptrace(GETFPREGS, %d) FAILED %d (%s)\n", m_tid, errno, strerror(errno));
        return false;
    }
#if defined(__i386__)
    if (ptrace((__ptrace_request)PTRACE_GETFPXREGS, m_tid, nullptr, &m_fpxRegisters) == -1)
    {
        fprintf(stderr, "ptrace(GETFPXREGS, %d) FAILED %d (%s)\n", m_tid, errno, strerror(errno));
        return false;
    }
#elif defined(__arm__) && defined(__VFP_FP__) && !defined(__SOFTFP__)

#if defined(ARM_VFPREGS_SIZE)
    assert(sizeof(m_vfpRegisters) == ARM_VFPREGS_SIZE);
#endif

    if (ptrace((__ptrace_request)PTRACE_GETVFPREGS, m_tid, nullptr, &m_vfpRegisters) == -1)
    {
        fprintf(stderr, "ptrace(PTRACE_GETVFPREGS, %d) FAILED %d (%s)\n", m_tid, errno, strerror(errno));
        return false;
    }
#endif
#endif
    return true;
}

bool 
ThreadInfo::GetRegistersWithDataTarget(ICLRDataTarget* pDataTarget)
{
    CONTEXT context;
    context.ContextFlags = CONTEXT_ALL;
    if (pDataTarget->GetThreadContext(m_tid, context.ContextFlags, sizeof(context), reinterpret_cast<PBYTE>(&context)) != S_OK)
    {
        return false;
    }
#if defined(__x86_64__)
    m_gpRegisters.rbp = context.Rbp;
    m_gpRegisters.rip = context.Rip;
    m_gpRegisters.cs = context.SegCs;
    m_gpRegisters.eflags = context.EFlags;
    m_gpRegisters.ss = context.SegSs;
    m_gpRegisters.rsp = context.Rsp;
    m_gpRegisters.rdi = context.Rdi;

    m_gpRegisters.rsi = context.Rsi;
    m_gpRegisters.rbx = context.Rbx;
    m_gpRegisters.rdx = context.Rdx;
    m_gpRegisters.rcx = context.Rcx;
    m_gpRegisters.rax = context.Rax;
    m_gpRegisters.orig_rax = context.Rax;
    m_gpRegisters.r8 = context.R8;
    m_gpRegisters.r9 = context.R9;
    m_gpRegisters.r10 = context.R10;
    m_gpRegisters.r11 = context.R11;
    m_gpRegisters.r12 = context.R12;
    m_gpRegisters.r13 = context.R13;
    m_gpRegisters.r14 = context.R14;
    m_gpRegisters.r15 = context.R15;

    m_gpRegisters.ds = context.SegDs;
    m_gpRegisters.es = context.SegEs;
    m_gpRegisters.fs = context.SegFs;
    m_gpRegisters.gs = context.SegGs;
    m_gpRegisters.fs_base = 0;
    m_gpRegisters.gs_base = 0;

    m_fpRegisters.cwd = context.FltSave.ControlWord;
    m_fpRegisters.swd = context.FltSave.StatusWord;
    m_fpRegisters.ftw = context.FltSave.TagWord;
    m_fpRegisters.fop = context.FltSave.ErrorOpcode;

    FPREG_ErrorOffset(m_fpRegisters) = context.FltSave.ErrorOffset;
    FPREG_ErrorSelector(m_fpRegisters) = context.FltSave.ErrorSelector;
    FPREG_DataOffset(m_fpRegisters) = context.FltSave.DataOffset;
    FPREG_DataSelector(m_fpRegisters) = context.FltSave.DataSelector;

    m_fpRegisters.mxcsr = context.FltSave.MxCsr;
    m_fpRegisters.mxcr_mask = context.FltSave.MxCsr_Mask;

    assert(sizeof(context.FltSave.FloatRegisters) == sizeof(m_fpRegisters.st_space));
    memcpy(m_fpRegisters.st_space, context.FltSave.FloatRegisters, sizeof(m_fpRegisters.st_space));

    assert(sizeof(context.FltSave.XmmRegisters) == sizeof(m_fpRegisters.xmm_space));
    memcpy(m_fpRegisters.xmm_space, context.FltSave.XmmRegisters, sizeof(m_fpRegisters.xmm_space));
#elif defined(__aarch64__)
    // See MCREG maps in PAL's context.h
    assert(sizeof(m_gpRegisters.regs) == (sizeof(context.X) + sizeof(context.Fp) + sizeof(context.Lr)));
    memcpy(m_gpRegisters.regs, context.X, sizeof(context.X));
    MCREG_Fp(m_gpRegisters) = context.Fp;
    MCREG_Lr(m_gpRegisters) = context.Lr;
    MCREG_Sp(m_gpRegisters) = context.Sp;
    MCREG_Pc(m_gpRegisters) = context.Pc;
    MCREG_Cpsr(m_gpRegisters) = context.Cpsr;

    assert(sizeof(m_fpRegisters.vregs) == sizeof(context.V));
    memcpy(m_fpRegisters.vregs, context.V, sizeof(context.V));
    m_fpRegisters.fpcr = context.Fpcr;
    m_fpRegisters.fpsr = context.Fpsr;
#elif defined(__arm__)
    m_gpRegisters.ARM_sp = context.Sp;
    m_gpRegisters.ARM_lr = context.Lr;
    m_gpRegisters.ARM_pc = context.Pc;
    m_gpRegisters.ARM_cpsr = context.Cpsr;

    m_gpRegisters.ARM_r0 = context.R0;
    m_gpRegisters.ARM_ORIG_r0 = context.R0;
    m_gpRegisters.ARM_r1 = context.R1;
    m_gpRegisters.ARM_r2 = context.R2;
    m_gpRegisters.ARM_r3 = context.R3;
    m_gpRegisters.ARM_r4 = context.R4;
    m_gpRegisters.ARM_r5 = context.R5;
    m_gpRegisters.ARM_r6 = context.R6;
    m_gpRegisters.ARM_r7 = context.R7;
    m_gpRegisters.ARM_r8 = context.R8;
    m_gpRegisters.ARM_r9 = context.R9;
    m_gpRegisters.ARM_r10 = context.R10;
    m_gpRegisters.ARM_fp = context.R11;
    m_gpRegisters.ARM_ip = context.R12;

#if defined(__VFP_FP__) && !defined(__SOFTFP__)
    m_vfpRegisters.fpscr = context.Fpscr;

    assert(sizeof(context.D) == sizeof(m_vfpRegisters.fpregs));
    memcpy(m_vfpRegisters.fpregs, context.D, sizeof(context.D));
#endif
#else 
#error Platform not supported
#endif
    return true;
}

void
ThreadInfo::GetThreadStack(CrashInfo& crashInfo)
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

    MemoryRegion search(0, startAddress, startAddress + PAGE_SIZE);
    const MemoryRegion* region = CrashInfo::SearchMemoryRegions(crashInfo.OtherMappings(), search);
    if (region != nullptr) {

        // Use the mapping found for the size of the thread's stack
        size = region->EndAddress() - startAddress;

        if (g_diagnostics)
        {
            TRACE("Thread %04x stack found in other mapping (size %08zx): ", m_tid, size);
            region->Trace();
        }
    }
    crashInfo.InsertMemoryRegion(startAddress, size);
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
