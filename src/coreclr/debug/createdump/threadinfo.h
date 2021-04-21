// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

class CrashInfo;

#if defined(__aarch64__)
// See src/pal/src/include/pal/context.h
#define MCREG_Fp(mc)      ((mc).regs[29])
#define MCREG_Lr(mc)      ((mc).regs[30])
#define MCREG_Sp(mc)      ((mc).sp)
#define MCREG_Pc(mc)      ((mc).pc)
#define MCREG_Cpsr(mc)    ((mc).pstate)
#endif

#define FPREG_ErrorOffset(fpregs) *(DWORD*)&((fpregs).rip)
#define FPREG_ErrorSelector(fpregs) *(((WORD*)&((fpregs).rip)) + 2)
#define FPREG_DataOffset(fpregs) *(DWORD*)&((fpregs).rdp)
#define FPREG_DataSelector(fpregs) *(((WORD*)&((fpregs).rdp)) + 2)
#if defined(__arm__)
#define user_regs_struct user_regs
#define user_fpregs_struct user_fpregs
#endif

#if defined(__aarch64__)
#define user_fpregs_struct user_fpsimd_struct
#endif

#if defined(__arm__) && defined(__VFP_FP__) && !defined(__SOFTFP__)
struct user_vfpregs_struct
{
  unsigned long long  fpregs[32];
  unsigned long       fpscr;
} __attribute__((__packed__));
#endif

class ThreadInfo
{
private:
    CrashInfo& m_crashInfo;                     // crashinfo instance
    pid_t m_tid;                                // thread id
    pid_t m_ppid;                               // parent process
    pid_t m_tgid;                               // thread group

#ifdef __APPLE__
    mach_port_t m_port;                         // MacOS thread port
#if defined(__x86_64__)
    x86_thread_state64_t m_gpRegisters;         // MacOS general purpose registers
    x86_float_state64_t m_fpRegisters;          // MacOS floating point registers
#elif defined(__aarch64__)
    arm_thread_state64_t m_gpRegisters;         // MacOS general purpose arm64 registers
    arm_neon_state64_t m_fpRegisters;           // MacOS floating point arm64 registers
#endif
#else // __APPLE__
    struct user_regs_struct m_gpRegisters;      // general purpose registers
    struct user_fpregs_struct m_fpRegisters;    // floating point registers
#if defined(__i386__)
    struct user_fpxregs_struct m_fpxRegisters;  // x86 floating point registers
#elif defined(__arm__) && defined(__VFP_FP__) && !defined(__SOFTFP__)
    struct user_vfpregs_struct m_vfpRegisters;  // ARM VFP/NEON registers
#endif
#endif // __APPLE__

public:
#ifdef __APPLE__
    ThreadInfo(CrashInfo& crashInfo, pid_t tid, mach_port_t port);
    inline mach_port_t Port() const { return m_port; }
#else
    ThreadInfo(CrashInfo& crashInfo, pid_t tid);
#endif
    ~ThreadInfo();
    bool Initialize();
    bool UnwindThread(IXCLRDataProcess* pClrDataProcess);
    void GetThreadStack();
    void GetThreadContext(uint32_t flags, CONTEXT* context) const;

    inline pid_t Tid() const { return m_tid; }
    inline pid_t Ppid() const { return m_ppid; }
    inline pid_t Tgid() const { return m_tgid; }

#ifdef __APPLE__
#if defined(__x86_64__)
    inline const x86_thread_state64_t* GPRegisters() const { return &m_gpRegisters; }
    inline const x86_float_state64_t* FPRegisters() const { return &m_fpRegisters; }
    inline const uint64_t GetStackPointer() const { return m_gpRegisters.__rsp; }
#elif defined(__aarch64__)
    inline const arm_thread_state64_t* GPRegisters() const { return &m_gpRegisters; }
    inline const arm_neon_state64_t* FPRegisters() const { return &m_fpRegisters; }
    inline const uint64_t GetStackPointer() const { return arm_thread_state64_get_sp(m_gpRegisters); }
#endif
#else // __APPLE__
    inline const user_regs_struct* GPRegisters() const { return &m_gpRegisters; }
    inline const user_fpregs_struct* FPRegisters() const { return &m_fpRegisters; }
#if defined(__i386__)
    inline const user_fpxregs_struct* FPXRegisters() const { return &m_fpxRegisters; }
#elif defined(__arm__) && defined(__VFP_FP__) && !defined(__SOFTFP__)
    inline const user_vfpregs_struct* VFPRegisters() const { return &m_vfpRegisters; }
#endif
    inline const uint64_t GetStackPointer() const
    {
#if defined(__x86_64__)
        return m_gpRegisters.rsp;
#elif defined(__aarch64__)
        return MCREG_Sp(m_gpRegisters);
#elif defined(__arm__)
        return m_gpRegisters.ARM_sp;
#endif
    }
#endif // __APPLE__

private:
    void UnwindNativeFrames(CONTEXT* pContext);
#ifndef __APPLE__
    bool GetRegistersWithPTrace();
#endif
};
