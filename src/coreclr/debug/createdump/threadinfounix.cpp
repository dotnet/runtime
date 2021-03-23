// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "createdump.h"

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

bool GetStatus(pid_t pid, pid_t* ppid, pid_t* tgid, std::string* name);

ThreadInfo::ThreadInfo(CrashInfo& crashInfo, pid_t tid) :
    m_crashInfo(crashInfo),
    m_tid(tid)
{
}

ThreadInfo::~ThreadInfo()
{
}

bool
ThreadInfo::Initialize()
{
    if (!GetStatus(m_tid, &m_ppid, &m_tgid, nullptr))
    {
        return false;
    }
    if (!GetRegistersWithPTrace())
    {
        return false;
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

bool
ThreadInfo::GetRegistersWithPTrace()
{
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
#if defined(__arm__)
        // Some aarch64 kernels may not support NT_FPREGSET for arm processes. We treat this failure as non-fatal.
#else
        fprintf(stderr, "ptrace(PTRACE_GETREGSET, %d, NT_FPREGSET) FAILED %d (%s)\n", m_tid, errno, strerror(errno));
        return false;
#endif
    }
    assert(sizeof(m_fpRegisters) == fpRegsVec.iov_len);

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
    return true;
}
