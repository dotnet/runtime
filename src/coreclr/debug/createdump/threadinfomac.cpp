// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "createdump.h"

ThreadInfo::ThreadInfo(CrashInfo& crashInfo, pid_t tid, mach_port_t port) :
    m_crashInfo(crashInfo),
    m_tid(tid),
    m_port(port)
{
}

ThreadInfo::~ThreadInfo()
{
    kern_return_t result = ::mach_port_deallocate(mach_task_self(), m_port);
    if (result != KERN_SUCCESS)
    {
        fprintf(stderr, "~ThreadInfo: mach_port_deallocate FAILED %x %s\n", result, mach_error_string(result));
    }
}

bool
ThreadInfo::Initialize()
{
    m_ppid = 0;
    m_tgid = 0;

#if defined(TARGET_AMD64)
    x86_thread_state64_t state;
    mach_msg_type_number_t stateCount = x86_THREAD_STATE64_COUNT;
    kern_return_t result = ::thread_get_state(Port(), x86_THREAD_STATE64, (thread_state_t)&state, &stateCount);
    if (result != KERN_SUCCESS)
    {
        fprintf(stderr, "thread_get_state(%x) FAILED %x %s\n", m_tid, result, mach_error_string(result));
        return false;
    }

    m_gpRegisters.rbp = state.__rbp;
    m_gpRegisters.rip = state.__rip;
    m_gpRegisters.cs = state.__cs;
    m_gpRegisters.eflags = state.__rflags;
    m_gpRegisters.ss = 0;
    m_gpRegisters.rsp = state.__rsp;
    m_gpRegisters.rdi = state.__rdi;

    m_gpRegisters.rsi = state.__rsi;
    m_gpRegisters.rbx = state.__rbx;
    m_gpRegisters.rdx = state.__rdx;
    m_gpRegisters.rcx = state.__rcx;
    m_gpRegisters.rax = state.__rax;
    m_gpRegisters.orig_rax = state.__rax;
    m_gpRegisters.r8 = state.__r8;
    m_gpRegisters.r9 = state.__r9;
    m_gpRegisters.r10 = state.__r10;
    m_gpRegisters.r11 = state.__r11;
    m_gpRegisters.r12 = state.__r12;
    m_gpRegisters.r13 = state.__r13;
    m_gpRegisters.r14 = state.__r14;
    m_gpRegisters.r15 = state.__r15;

    m_gpRegisters.fs = state.__fs;
    m_gpRegisters.gs = state.__gs;
    m_gpRegisters.ds = 0;
    m_gpRegisters.es = 0;
    m_gpRegisters.gs_base = 0;
    m_gpRegisters.fs_base = 0;

    x86_float_state64_t fpstate;
    stateCount = x86_FLOAT_STATE64_COUNT;
    result = ::thread_get_state(Port(), x86_FLOAT_STATE64, (thread_state_t)&fpstate, &stateCount);
    if (result != KERN_SUCCESS)
    {
        fprintf(stderr, "thread_get_state(%x) FAILED %x %s\n", m_tid, result, mach_error_string(result));
        return false;
    }

    m_fpRegisters.cwd = *((unsigned short *)&fpstate.__fpu_fcw);
    m_fpRegisters.swd = *((unsigned short *)&fpstate.__fpu_fsw);
    m_fpRegisters.ftw = fpstate.__fpu_ftw;
    m_fpRegisters.fop = fpstate.__fpu_fop;

    FPREG_ErrorOffset(m_fpRegisters) = fpstate.__fpu_ip;
    FPREG_ErrorSelector(m_fpRegisters) = fpstate.__fpu_cs;
    FPREG_DataOffset(m_fpRegisters) = fpstate.__fpu_dp;
    FPREG_DataSelector(m_fpRegisters) = fpstate.__fpu_ds;

    m_fpRegisters.mxcsr = fpstate.__fpu_mxcsr;
    m_fpRegisters.mxcr_mask = fpstate.__fpu_mxcsrmask;

    memcpy(m_fpRegisters.st_space, &fpstate.__fpu_stmm0, sizeof(m_fpRegisters.st_space));
    memcpy(m_fpRegisters.xmm_space, &fpstate.__fpu_xmm0, sizeof(m_fpRegisters.xmm_space));
#elif defined(TARGET_ARM64)
    arm_thread_state64_t state;
    mach_msg_type_number_t stateCount = ARM_THREAD_STATE64_COUNT;
    kern_return_t result = ::thread_get_state(Port(), ARM_THREAD_STATE64, (thread_state_t)&state, &stateCount);
    if (result != KERN_SUCCESS)
    {
        fprintf(stderr, "thread_get_state(%x) FAILED %x %s\n", m_tid, result, mach_error_string(result));
        return false;
    }

    memcpy(m_gpRegisters.regs, &state.__x, sizeof(state.__x));
    m_gpRegisters.regs[29] =  arm_thread_state64_get_fp(state);
    m_gpRegisters.regs[30] =  (uint64_t)arm_thread_state64_get_lr_fptr(state);

    m_gpRegisters.sp = arm_thread_state64_get_sp(state);
    m_gpRegisters.pc = (uint64_t)arm_thread_state64_get_pc_fptr(state);

    arm_neon_state64_t fpstate;
    stateCount = ARM_NEON_STATE64_COUNT;
    result = ::thread_get_state(Port(), ARM_NEON_STATE64, (thread_state_t)&fpstate, &stateCount);
    if (result != KERN_SUCCESS)
    {
        fprintf(stderr, "thread_get_state(%x) FAILED %x %s\n", m_tid, result, mach_error_string(result));
        return false;
    }

    memcpy(m_fpRegisters.vregs, &fpstate.__v, sizeof(m_fpRegisters.vregs));
    m_fpRegisters.fpsr = fpstate.__fpsr;
    m_fpRegisters.fpcr = fpstate.__fpcr;
#else
#error Unexpected architecture
#endif

    return true;
}
