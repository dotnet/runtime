// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "createdump.h"

ThreadInfo::ThreadInfo(CrashInfo& crashInfo, pid_t tid, mach_port_t port) :
    m_crashInfo(crashInfo),
    m_tid(tid),
    m_ppid(0),
    m_tgid(0),
    m_managed(false),
    m_exceptionObject(0),
    m_exceptionHResult(0),
    m_repeatedFrames(0),
    m_port(port)
{
    m_beginRepeat = m_frames.end();
    m_endRepeat = m_frames.end();
}

ThreadInfo::~ThreadInfo()
{
    kern_return_t result = ::mach_port_deallocate(mach_task_self(), m_port);
    if (result != KERN_SUCCESS)
    {
        printf_error("Internal error: ~ThreadInfo: mach_port_deallocate FAILED %s (%x)\n", mach_error_string(result), result);
    }
}

bool
ThreadInfo::Initialize()
{
    m_ppid = 0;
    m_tgid = 0;

#if defined(__x86_64__)
    mach_msg_type_number_t stateCount = x86_THREAD_STATE64_COUNT;
    kern_return_t result = ::thread_get_state(Port(), x86_THREAD_STATE64, (thread_state_t)&m_gpRegisters, &stateCount);
    if (result != KERN_SUCCESS)
    {
        printf_error("thread_get_state(%x) FAILED %s (%x)\n", m_tid, mach_error_string(result), result);
        return false;
    }

    stateCount = x86_FLOAT_STATE64_COUNT;
    result = ::thread_get_state(Port(), x86_FLOAT_STATE64, (thread_state_t)&m_fpRegisters, &stateCount);
    if (result != KERN_SUCCESS)
    {
        printf_error("thread_get_state(%x) FAILED %s (%x)\n", m_tid, mach_error_string(result), result);
        return false;
    }
#elif defined(__aarch64__)
    mach_msg_type_number_t stateCount = ARM_THREAD_STATE64_COUNT;
    kern_return_t result = ::thread_get_state(Port(), ARM_THREAD_STATE64, (thread_state_t)&m_gpRegisters, &stateCount);
    if (result != KERN_SUCCESS)
    {
        printf_error("thread_get_state(%x) FAILED %s (%x)\n", m_tid,  mach_error_string(result), result);
        return false;
    }

    stateCount = ARM_NEON_STATE64_COUNT;
    result = ::thread_get_state(Port(), ARM_NEON_STATE64, (thread_state_t)&m_fpRegisters, &stateCount);
    if (result != KERN_SUCCESS)
    {
        printf_error("thread_get_state(%x) FAILED %s (%x)\n", m_tid, mach_error_string(result), result);
        return false;
    }
#else
#error Unexpected architecture
#endif
    return true;
}

void
ThreadInfo::GetThreadContext(uint32_t flags, CONTEXT* context) const
{
    context->ContextFlags = flags;
#if defined(__x86_64__)
    if ((flags & CONTEXT_CONTROL) == CONTEXT_CONTROL)
    {
        context->Rbp = m_gpRegisters.__rbp;
        context->Rip = m_gpRegisters.__rip;
        context->SegCs = m_gpRegisters.__cs;
        context->EFlags = m_gpRegisters.__rflags;
        // TODO: get "full" register state for the segment regs
        context->SegSs = 0; // m_gpRegisters.__ss;
        context->Rsp = m_gpRegisters.__rsp;
    }
    if ((flags & CONTEXT_INTEGER) == CONTEXT_INTEGER)
    {
        context->Rdi = m_gpRegisters.__rdi;
        context->Rsi = m_gpRegisters.__rsi;
        context->Rbx = m_gpRegisters.__rbx;
        context->Rdx = m_gpRegisters.__rdx;
        context->Rcx = m_gpRegisters.__rcx;
        context->Rax = m_gpRegisters.__rax;
        context->R8 = m_gpRegisters.__r8;
        context->R9 = m_gpRegisters.__r9;
        context->R10 = m_gpRegisters.__r10;
        context->R11 = m_gpRegisters.__r11;
        context->R12 = m_gpRegisters.__r12;
        context->R13 = m_gpRegisters.__r13;
        context->R14 = m_gpRegisters.__r14;
        context->R15 = m_gpRegisters.__r15;
    }
    if ((flags & CONTEXT_SEGMENTS) == CONTEXT_SEGMENTS)
    {
        // TODO: get "full" register state for the segment regs
        context->SegDs = 0; // m_gpRegisters.__ds;
        context->SegEs = 0; // m_gpRegisters.__es;
        context->SegFs = m_gpRegisters.__fs;
        context->SegGs = m_gpRegisters.__gs;
    }
    if ((flags & CONTEXT_FLOATING_POINT) == CONTEXT_FLOATING_POINT)
    {
        context->FltSave.ControlWord = *((unsigned short *)&m_fpRegisters.__fpu_fcw);
        context->FltSave.StatusWord = *((unsigned short *)&m_fpRegisters.__fpu_fsw);
        context->FltSave.TagWord = m_fpRegisters.__fpu_ftw;
        context->FltSave.ErrorOpcode = m_fpRegisters.__fpu_fop;

        context->FltSave.ErrorOffset = m_fpRegisters.__fpu_ip;
        context->FltSave.ErrorSelector = m_fpRegisters.__fpu_cs;
        context->FltSave.DataOffset = m_fpRegisters.__fpu_dp;
        context->FltSave.DataSelector = m_fpRegisters.__fpu_ds;

        context->FltSave.MxCsr = m_fpRegisters.__fpu_mxcsr;
        context->FltSave.MxCsr_Mask = m_fpRegisters.__fpu_mxcsrmask;

        assert(sizeof(context->FltSave.FloatRegisters) == sizeof(m_fpRegisters.__fpu_stmm0) * 8);
        memcpy(context->FltSave.FloatRegisters, &m_fpRegisters.__fpu_stmm0, sizeof(context->FltSave.FloatRegisters));

        assert(sizeof(context->FltSave.XmmRegisters) == sizeof(m_fpRegisters.__fpu_xmm0) * 16);
        memcpy(context->FltSave.XmmRegisters, &m_fpRegisters.__fpu_xmm0, sizeof(context->FltSave.XmmRegisters));
    }
    // TODO: debug registers?
#elif defined(__aarch64__)
    if ((flags & CONTEXT_CONTROL) == CONTEXT_CONTROL)
    {
        context->Fp = arm_thread_state64_get_fp(m_gpRegisters);
        context->Lr = (uint64_t)arm_thread_state64_get_lr_fptr(m_gpRegisters);
        context->Sp = arm_thread_state64_get_sp(m_gpRegisters);
        context->Pc = (uint64_t)arm_thread_state64_get_pc_fptr(m_gpRegisters);
        context->Cpsr = m_gpRegisters.__cpsr;
    }
    if ((flags & CONTEXT_INTEGER) == CONTEXT_INTEGER)
    {
        assert(sizeof(m_gpRegisters.__x) == (sizeof(context->X)));
        memcpy(context->X, m_gpRegisters.__x, sizeof(context->X));
    }
    if ((flags & CONTEXT_FLOATING_POINT) == CONTEXT_FLOATING_POINT)
    {
        assert(sizeof(m_fpRegisters.__v) == sizeof(context->V));
        memcpy(context->V, m_fpRegisters.__v, sizeof(context->V));
        context->Fpcr = m_fpRegisters.__fpcr;
        context->Fpsr = m_fpRegisters.__fpsr;
    }
#else
#error Platform not supported
#endif
}
