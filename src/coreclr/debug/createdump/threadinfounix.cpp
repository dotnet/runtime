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

ThreadInfo::ThreadInfo(CrashInfo& crashInfo, const ThreadSnapshot& snapshot) :
    m_crashInfo(crashInfo),
    m_snapshot(snapshot),
    m_managed(false),
    m_exceptionObject(0),
    m_exceptionHResult(0),
    m_repeatedFrames(0)
{
    m_beginRepeat = m_frames.end();
    m_endRepeat = m_frames.end();
}

ThreadInfo::~ThreadInfo()
{
}

void
ThreadInfo::GetThreadContext(uint32_t flags, CONTEXT* context) const
{
    context->ContextFlags = flags;
#if defined(__x86_64__)
    if ((flags & CONTEXT_CONTROL) == CONTEXT_CONTROL)
    {
        context->Rbp = m_snapshot.m_gpRegisters.rbp;
        context->Rip = m_snapshot.m_gpRegisters.rip;
        context->SegCs = m_snapshot.m_gpRegisters.cs;
        context->EFlags = m_snapshot.m_gpRegisters.eflags;
        context->SegSs = m_snapshot.m_gpRegisters.ss;
        context->Rsp = m_snapshot.m_gpRegisters.rsp;
    }
    if ((flags & CONTEXT_INTEGER) == CONTEXT_INTEGER)
    {
        context->Rdi = m_snapshot.m_gpRegisters.rdi;
        context->Rsi = m_snapshot.m_gpRegisters.rsi;
        context->Rbx = m_snapshot.m_gpRegisters.rbx;
        context->Rdx = m_snapshot.m_gpRegisters.rdx;
        context->Rcx = m_snapshot.m_gpRegisters.rcx;
        context->Rax = m_snapshot.m_gpRegisters.rax;
        context->R8 = m_snapshot.m_gpRegisters.r8;
        context->R9 = m_snapshot.m_gpRegisters.r9;
        context->R10 = m_snapshot.m_gpRegisters.r10;
        context->R11 = m_snapshot.m_gpRegisters.r11;
        context->R12 = m_snapshot.m_gpRegisters.r12;
        context->R13 = m_snapshot.m_gpRegisters.r13;
        context->R14 = m_snapshot.m_gpRegisters.r14;
        context->R15 = m_snapshot.m_gpRegisters.r15;
    }
    if ((flags & CONTEXT_SEGMENTS) == CONTEXT_SEGMENTS)
    {
        context->SegDs = m_snapshot.m_gpRegisters.ds;
        context->SegEs = m_snapshot.m_gpRegisters.es;
        context->SegFs = m_snapshot.m_gpRegisters.fs;
        context->SegGs = m_snapshot.m_gpRegisters.gs;
    }
    if ((flags & CONTEXT_FLOATING_POINT) == CONTEXT_FLOATING_POINT)
    {
        context->FltSave.ControlWord = m_snapshot.m_fpRegisters.cwd;
        context->FltSave.StatusWord = m_snapshot.m_fpRegisters.swd;
        context->FltSave.TagWord = m_snapshot.m_fpRegisters.ftw;
        context->FltSave.ErrorOpcode = m_snapshot.m_fpRegisters.fop;

        context->FltSave.ErrorOffset = FPREG_ErrorOffset(m_snapshot.m_fpRegisters);
        context->FltSave.ErrorSelector = FPREG_ErrorSelector(m_snapshot.m_fpRegisters);
        context->FltSave.DataOffset = FPREG_DataOffset(m_snapshot.m_fpRegisters);
        context->FltSave.DataSelector = FPREG_DataSelector(m_snapshot.m_fpRegisters);

        context->FltSave.MxCsr = m_snapshot.m_fpRegisters.mxcsr;
        context->FltSave.MxCsr_Mask = m_snapshot.m_fpRegisters.mxcr_mask;

        assert(sizeof(context->FltSave.FloatRegisters) == sizeof(m_snapshot.m_fpRegisters.st_space));
        memcpy(context->FltSave.FloatRegisters, m_snapshot.m_fpRegisters.st_space, sizeof(context->FltSave.FloatRegisters));

        assert(sizeof(context->FltSave.XmmRegisters) == sizeof(m_snapshot.m_fpRegisters.xmm_space));
        memcpy(context->FltSave.XmmRegisters, m_snapshot.m_fpRegisters.xmm_space, sizeof(context->FltSave.XmmRegisters));
    }
    // TODO: debug registers?
#elif defined(__aarch64__)
    if ((flags & CONTEXT_CONTROL) == CONTEXT_CONTROL)
    {
        context->Fp = MCREG_Fp(m_snapshot.m_gpRegisters);
        context->Lr = MCREG_Lr(m_snapshot.m_gpRegisters);
        context->Sp = MCREG_Sp(m_snapshot.m_gpRegisters);
        context->Pc = MCREG_Pc(m_snapshot.m_gpRegisters);
        context->Cpsr = MCREG_Cpsr(m_snapshot.m_gpRegisters);
    }
    if ((flags & CONTEXT_INTEGER) == CONTEXT_INTEGER)
    {
        assert(sizeof(m_snapshot.m_gpRegisters.regs) == (sizeof(context->X) + sizeof(context->Fp) + sizeof(context->Lr)));
        memcpy(context->X, m_snapshot.m_gpRegisters.regs, sizeof(context->X));
    }
    if ((flags & CONTEXT_FLOATING_POINT) == CONTEXT_FLOATING_POINT)
    {
        assert(sizeof(m_snapshot.m_fpRegisters.vregs) == sizeof(context->V));
        memcpy(context->V, m_snapshot.m_fpRegisters.vregs, sizeof(context->V));
        context->Fpcr = m_snapshot.m_fpRegisters.fpcr;
        context->Fpsr = m_snapshot.m_fpRegisters.fpsr;
    }
#elif defined(__arm__)
    if ((flags & CONTEXT_CONTROL) == CONTEXT_CONTROL)
    {
        context->Sp = m_snapshot.m_gpRegisters.ARM_sp;
        context->Lr = m_snapshot.m_gpRegisters.ARM_lr;
        context->Pc = m_snapshot.m_gpRegisters.ARM_pc;
        context->Cpsr = m_snapshot.m_gpRegisters.ARM_cpsr;
    }
    if ((flags & CONTEXT_INTEGER) == CONTEXT_INTEGER)
    {
        context->R0 = m_snapshot.m_gpRegisters.ARM_r0;
        context->R1 = m_snapshot.m_gpRegisters.ARM_r1;
        context->R2 = m_snapshot.m_gpRegisters.ARM_r2;
        context->R3 = m_snapshot.m_gpRegisters.ARM_r3;
        context->R4 = m_snapshot.m_gpRegisters.ARM_r4;
        context->R5 = m_snapshot.m_gpRegisters.ARM_r5;
        context->R6 = m_snapshot.m_gpRegisters.ARM_r6;
        context->R7 = m_snapshot.m_gpRegisters.ARM_r7;
        context->R8 = m_snapshot.m_gpRegisters.ARM_r8;
        context->R9 = m_snapshot.m_gpRegisters.ARM_r9;
        context->R10 = m_snapshot.m_gpRegisters.ARM_r10;
        context->R11 = m_snapshot.m_gpRegisters.ARM_fp;
        context->R12 = m_snapshot.m_gpRegisters.ARM_ip;
    }
    if ((flags & CONTEXT_FLOATING_POINT) == CONTEXT_FLOATING_POINT)
    {
#if defined(__VFP_FP__) && !defined(__SOFTFP__)
        context->Fpscr = m_vfpRegisters.fpscr;

        assert(sizeof(context->D) == sizeof(m_vfpRegisters.fpregs));
        memcpy(context->D, m_vfpRegisters.fpregs, sizeof(context->D));
#endif
    }
#elif defined(__loongarch64)
    if (flags & CONTEXT_CONTROL)
    {
        context->Ra = MCREG_Ra(m_snapshot.m_gpRegisters);
        context->Sp = MCREG_Sp(m_snapshot.m_gpRegisters);
        context->Fp = MCREG_Fp(m_snapshot.m_gpRegisters);
        context->Pc = MCREG_Pc(m_snapshot.m_gpRegisters);
    }
    if (flags & CONTEXT_INTEGER)
    {
        memcpy(&context->A0, &m_snapshot.m_gpRegisters.regs[4], sizeof(context->A0)*(21 - 4 + 1));
        memcpy(&context->S0, &m_snapshot.m_gpRegisters.regs[23], sizeof(context->S0)*9);
    }
    if (flags & CONTEXT_FLOATING_POINT)
    {
        assert(sizeof(context->F) == sizeof(m_snapshot.m_fpRegisters.regs));
        memcpy(context->F, m_snapshot.m_fpRegisters.regs, sizeof(context->F));
        context->Fcsr = m_snapshot.m_fpRegisters.fcsr;
        context->Fcc  = m_snapshot.m_fpRegisters.fcc;
    }
#elif defined(__riscv)
    if ((flags & CONTEXT_CONTROL) == CONTEXT_CONTROL)
    {
        context->Ra = MCREG_Ra(m_snapshot.m_gpRegisters);
        context->Sp = MCREG_Sp(m_snapshot.m_gpRegisters);
        context->Fp = MCREG_Fp(m_snapshot.m_gpRegisters);
        context->Pc = MCREG_Pc(m_snapshot.m_gpRegisters);
    }

    if (flags & CONTEXT_INTEGER)
    {
        context->Gp = m_snapshot.m_gpRegisters.gp;
        context->Tp = m_snapshot.m_gpRegisters.tp;
        context->T0 = m_snapshot.m_gpRegisters.t0;
        context->T1 = m_snapshot.m_gpRegisters.t1;
        context->T2 = m_snapshot.m_gpRegisters.t2;
        context->S1 = m_snapshot.m_gpRegisters.s1;
        context->A0 = m_snapshot.m_gpRegisters.a0;
        context->A1 = m_snapshot.m_gpRegisters.a1;
        context->A2 = m_snapshot.m_gpRegisters.a2;
        context->A3 = m_snapshot.m_gpRegisters.a3;
        context->A4 = m_snapshot.m_gpRegisters.a4;
        context->A5 = m_snapshot.m_gpRegisters.a5;
        context->A6 = m_snapshot.m_gpRegisters.a6;
        context->A7 = m_snapshot.m_gpRegisters.a7;
        context->S2 = m_snapshot.m_gpRegisters.s2;
        context->S3 = m_snapshot.m_gpRegisters.s3;
        context->S4 = m_snapshot.m_gpRegisters.s4;
        context->S5 = m_snapshot.m_gpRegisters.s5;
        context->S6 = m_snapshot.m_gpRegisters.s6;
        context->S7 = m_snapshot.m_gpRegisters.s7;
        context->S8 = m_snapshot.m_gpRegisters.s8;
        context->S9 = m_snapshot.m_gpRegisters.s9;
        context->S10 = m_snapshot.m_gpRegisters.s10;
        context->S11 = m_snapshot.m_gpRegisters.s11;
        context->T3 = m_snapshot.m_gpRegisters.t3;
        context->T4 = m_snapshot.m_gpRegisters.t4;
        context->T5 = m_snapshot.m_gpRegisters.t5;
        context->T6 = m_snapshot.m_gpRegisters.t6;
    }
    if (flags & CONTEXT_FLOATING_POINT)
    {
        assert(sizeof(context->F) == sizeof(m_snapshot.m_fpRegisters.fpregs));
        memcpy(context->F, m_snapshot.m_fpRegisters.fpregs, sizeof(context->F));
        context->Fcsr = m_snapshot.m_fpRegisters.fcsr;
    }
#else
#error Platform not supported
#endif
}
