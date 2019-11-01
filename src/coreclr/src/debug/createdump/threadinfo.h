// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

class CrashInfo;

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
    pid_t m_tid;                                // thread id
    pid_t m_ppid;                               // parent process
    pid_t m_tgid;                               // thread group
    struct user_regs_struct m_gpRegisters;      // general purpose registers
    struct user_fpregs_struct m_fpRegisters;    // floating point registers
#if defined(__i386__)
    struct user_fpxregs_struct m_fpxRegisters;  // x86 floating point registers
#elif defined(__arm__) && defined(__VFP_FP__) && !defined(__SOFTFP__)
    struct user_vfpregs_struct m_vfpRegisters;  // ARM VFP/NEON registers
#endif

public:
    ThreadInfo(pid_t tid);
    ~ThreadInfo();
    bool Initialize(ICLRDataTarget* pDataTarget);
    void ResumeThread();
    bool UnwindThread(CrashInfo& crashInfo, IXCLRDataProcess* pClrDataProcess);
    void GetThreadStack(CrashInfo& crashInfo);
    void GetThreadContext(uint32_t flags, CONTEXT* context) const;

    inline pid_t Tid() const { return m_tid; }
    inline pid_t Ppid() const { return m_ppid; }
    inline pid_t Tgid() const { return m_tgid; }

    inline const user_regs_struct* GPRegisters() const { return &m_gpRegisters; }
    inline const user_fpregs_struct* FPRegisters() const { return &m_fpRegisters; }
#if defined(__i386__)
    inline const user_fpxregs_struct* FPXRegisters() const { return &m_fpxRegisters; }
#elif defined(__arm__) && defined(__VFP_FP__) && !defined(__SOFTFP__)
    inline const user_vfpregs_struct* VFPRegisters() const { return &m_vfpRegisters; }
#endif

private:
    void UnwindNativeFrames(CrashInfo& crashInfo, CONTEXT* pContext);
    bool GetRegistersWithPTrace();
    bool GetRegistersWithDataTarget(ICLRDataTarget* dataTarget);
};
