// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "createdump.h"

#define FPREG_ErrorOffset(fpregs) *(DWORD*)&((fpregs).rip)
#define FPREG_ErrorSelector(fpregs) *(((WORD*)&((fpregs).rip)) + 2)
#define FPREG_DataOffset(fpregs) *(DWORD*)&((fpregs).rdp)
#define FPREG_DataSelector(fpregs) *(((WORD*)&((fpregs).rdp)) + 2)

ThreadInfo::ThreadInfo(pid_t tid) :
    m_tid(tid)
{
}

ThreadInfo::~ThreadInfo()
{
}

bool
ThreadInfo::Initialize(ICLRDataTarget* dataTarget)
{
    if (!CrashInfo::GetStatus(m_tid, &m_ppid, &m_tgid, nullptr)) 
    {
        return false;
    }
    if (dataTarget != nullptr)
    {
        if (!GetRegistersWithDataTarget(dataTarget))
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
    TRACE("Thread %04x RIP %016llx RSP %016llx\n", m_tid, m_gpRegisters.rip, m_gpRegisters.rsp);
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

bool 
ThreadInfo::GetRegistersWithPTrace()
{
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
#endif
    return true;
}

bool 
ThreadInfo::GetRegistersWithDataTarget(ICLRDataTarget* dataTarget)
{
    CONTEXT context;
    context.ContextFlags = CONTEXT_ALL;
    if (dataTarget->GetThreadContext(m_tid, context.ContextFlags, sizeof(context), reinterpret_cast<PBYTE>(&context)) != S_OK)
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
#else 
#error Platform not supported
#endif
    return true;
}

void
ThreadInfo::GetThreadStack(const CrashInfo& crashInfo, uint64_t* startAddress, size_t* size) const
{
    *startAddress = m_gpRegisters.rsp & PAGE_MASK;
    *size = 4 * PAGE_SIZE;

    for (const MemoryRegion& mapping : crashInfo.OtherMappings())
    {
        if (*startAddress >= mapping.StartAddress() && *startAddress < mapping.EndAddress())
        {
            // Use the mapping found for the size of the thread's stack
            *size = mapping.EndAddress() - *startAddress;

            if (g_diagnostics)
            {
                TRACE("Thread %04x stack found in other mapping (size %08lx): ", m_tid, *size);
                mapping.Print();
            }
            break;
        }
    }
}

void
ThreadInfo::GetThreadCode(uint64_t* startAddress, size_t* size) const
{
    *startAddress = m_gpRegisters.rip & PAGE_MASK;
    *size = PAGE_SIZE;
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
#else 
#error Platform not supported
#endif
}
