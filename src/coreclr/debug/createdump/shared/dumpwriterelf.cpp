// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "createdumpcore.h"
#include "dumpwriter.h"

DumpWriter::DumpWriter(ProcessInfo& processInfo) :
    m_fd(-1),
    m_processInfo(processInfo)
{
}

bool
DumpWriter::WriteProcessInfo()
{
    prpsinfo_t processInfo;
    memset(&processInfo, 0, sizeof(processInfo));
    processInfo.pr_sname = 'R';
    processInfo.pr_pid = m_processInfo.Pid();
    processInfo.pr_ppid = m_processInfo.Ppid();
    processInfo.pr_pgrp = m_processInfo.Tgid();
    strncpy(processInfo.pr_fname, m_processInfo.Name(), sizeof(processInfo.pr_fname));

    Nhdr nhdr;
    memset(&nhdr, 0, sizeof(nhdr));
    nhdr.n_namesz = 5;
    nhdr.n_descsz = sizeof(prpsinfo_t);
    nhdr.n_type = NT_PRPSINFO;

    TRACE("Writing process information to core file\n");

    // Write process info data to core file
    if (!WriteData(&nhdr, sizeof(nhdr)) ||
        !WriteData("CORE\0PRP", 8) ||
        !WriteData(&processInfo, sizeof(prpsinfo_t))) {
        return false;
    }
    return true;
}

bool
DumpWriter::WriteAuxv()
{
    Nhdr nhdr;
    memset(&nhdr, 0, sizeof(nhdr));
    nhdr.n_namesz = 5;
    nhdr.n_descsz = m_processInfo.GetAuxvSize();
    nhdr.n_type = NT_AUXV;

    TRACE("Writing %zd auxv entries to core file\n", m_processInfo.AuxvEntries().size());

    if (!WriteData(&nhdr, sizeof(nhdr)) ||
        !WriteData("CORE\0AUX", 8)) {
        return false;
    }
    for (const elf_aux_entry& auxvEntry : m_processInfo.AuxvEntries())
    {
        if (!WriteData(&auxvEntry, sizeof(auxvEntry))) {
            return false;
        }
    }
    return true;
}

bool
DumpWriter::WriteThread(const ThreadSnapshot& thread)
{
    prstatus_t pr;
    memset(&pr, 0, sizeof(pr));
    const siginfo_t* siginfo = nullptr;

    if (m_processInfo.Signal() != 0 && thread.Tid() == m_processInfo.CrashThread())
    {
        siginfo = m_processInfo.SigInfo();
        pr.pr_info.si_signo = siginfo->si_signo;
        pr.pr_info.si_code = siginfo->si_code;
        pr.pr_info.si_errno = siginfo->si_errno;
        pr.pr_cursig = siginfo->si_signo;
    }
    pr.pr_pid = thread.Tid();
    pr.pr_ppid = thread.Ppid();
    pr.pr_pgrp = thread.Tgid();
    memcpy(&pr.pr_reg, thread.GPRegisters(), sizeof(user_regs_struct));

    Nhdr nhdr;
    memset(&nhdr, 0, sizeof(nhdr));

    // Name size is CORE plus the NULL terminator
    // The format requires 4 byte alignment so the
    // value written in 8 bytes.  Stuff the last 3
    // bytes with the type of NT_PRSTATUS so it is
    // easier to debug in a hex editor.
    nhdr.n_namesz = 5;
    nhdr.n_descsz = sizeof(prstatus_t);
    nhdr.n_type = NT_PRSTATUS;
    if (!WriteData(&nhdr, sizeof(nhdr)) ||
        !WriteData("CORE\0THR", 8) ||
        !WriteData(&pr, sizeof(prstatus_t))) {
        return false;
    }

    nhdr.n_descsz = sizeof(user_fpregs_struct);
    nhdr.n_type = NT_FPREGSET;
    if (!WriteData(&nhdr, sizeof(nhdr)) ||
        !WriteData("CORE\0FLT", 8) ||
        !WriteData(thread.FPRegisters(), sizeof(user_fpregs_struct))) {
        return false;
    }

#if defined(__i386__)
    nhdr.n_namesz = 6;
    nhdr.n_descsz = sizeof(user_fpxregs_struct);
    nhdr.n_type = NT_PRXFPREG;
    if (!WriteData(&nhdr, sizeof(nhdr)) ||
        !WriteData("LINUX\0\0\0", 8) ||
        !WriteData(thread.FPXRegisters(), sizeof(user_fpxregs_struct))) {
        return false;
    }
#endif

#if defined(__arm__) && defined(__VFP_FP__) && !defined(__SOFTFP__)
    nhdr.n_namesz = 6;
    nhdr.n_descsz = sizeof(user_vfpregs_struct);
    nhdr.n_type = NT_ARM_VFP;
    if (!WriteData(&nhdr, sizeof(nhdr)) ||
        !WriteData("LINUX\0\0\0", 8) ||
        !WriteData(thread.VFPRegisters(), sizeof(user_vfpregs_struct))) {
        return false;
    }
#endif

    if (siginfo != nullptr)
    {
        TRACE("Writing NT_SIGINFO tid %04x signo %d (%04x) code %04x errno %04x addr %p\n",
            thread.Tid(), siginfo->si_signo, siginfo->si_signo, siginfo->si_code, siginfo->si_errno, siginfo->si_addr);

        nhdr.n_namesz = 5;
        nhdr.n_descsz = sizeof(siginfo_t);
        nhdr.n_type = NT_SIGINFO;
        if (!WriteData(&nhdr, sizeof(nhdr)) ||
            !WriteData("CORE\0SIG", 8) ||
            !WriteData(siginfo, sizeof(siginfo_t))) {
            return false;
        }
    }
    return true;
}
