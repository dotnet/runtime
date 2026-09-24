// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "createdumpcore.h"
#include "threadsnapshot.h"

#ifndef __GLIBC__
typedef int __ptrace_request;
#endif

bool ThreadSnapshot::Initialize()
{
    if (!GetStatus(m_tid, &m_ppid, &m_tgid, nullptr, 0))
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
#elif defined(__loongarch64)
    TRACE("Thread %04x PC %016llx SP %016llx\n", m_tid, (unsigned long long)m_gpRegisters.csr_era, (unsigned long long)m_gpRegisters.regs[3]);
#elif defined(__riscv)
    TRACE("Thread %04x PC %016llx SP %016llx\n", m_tid, (unsigned long long)m_gpRegisters.pc, (unsigned long long)m_gpRegisters.sp);
#else
#error "Unsupported architecture"
#endif
    return true;
}

bool ThreadSnapshot::GetRegistersWithPTrace()
{
    struct iovec gpRegsVec = { &m_gpRegisters, sizeof(m_gpRegisters) };
    if (ptrace((__ptrace_request)PTRACE_GETREGSET, m_tid, NT_PRSTATUS, &gpRegsVec) == -1)
    {
        printf_error("ptrace(PTRACE_GETREGSET, %d, NT_PRSTATUS) FAILED %s (%d)\n", m_tid, strerror(errno), errno);
        return false;
    }
    assert(sizeof(m_gpRegisters) == gpRegsVec.iov_len);

    struct iovec fpRegsVec = { &m_fpRegisters, sizeof(m_fpRegisters) };
    if (ptrace((__ptrace_request)PTRACE_GETREGSET, m_tid, NT_FPREGSET, &fpRegsVec) == -1)
    {
#if defined(__arm__)
        // Some aarch64 kernels may not support NT_FPREGSET for arm processes. We treat this failure as non-fatal.
#else
        printf_error("ptrace(PTRACE_GETREGSET, %d, NT_FPREGSET) FAILED %s (%d)\n", m_tid, strerror(errno), errno);
        return false;
#endif
    }
    assert(sizeof(m_fpRegisters) == fpRegsVec.iov_len);

#if defined(__i386__)
    if (ptrace((__ptrace_request)PTRACE_GETFPXREGS, m_tid, nullptr, &m_fpxRegisters) == -1)
    {
        printf_error("ptrace(GETFPXREGS, %d) FAILED %s (%d)\n", m_tid, strerror(errno), errno);
        return false;
    }
#elif defined(__arm__) && defined(__VFP_FP__) && !defined(__SOFTFP__)

#if defined(ARM_VFPREGS_SIZE)
    assert(sizeof(m_vfpRegisters) == ARM_VFPREGS_SIZE);
#endif

    if (ptrace((__ptrace_request)PTRACE_GETVFPREGS, m_tid, nullptr, &m_vfpRegisters) == -1)
    {
        printf_error("ptrace(PTRACE_GETVFPREGS, %d) FAILED %s (%d)\n", m_tid, strerror(errno), errno);
        return false;
    }
#endif
    return true;
}