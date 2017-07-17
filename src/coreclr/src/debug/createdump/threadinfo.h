// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

class CrashInfo;

#if defined(__arm__)
#define user_regs_struct user_regs
#define user_fpregs_struct user_fpregs

#if defined(__VFP_FP__) && !defined(__SOFTFP__)
struct user_vfpregs_struct
{
  unsigned long long  fpregs[32];
  unsigned long       fpscr;
} __attribute__((__packed__));
#endif

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
    bool Initialize(ICLRDataTarget* dataTarget);
    void ResumeThread();
    void GetThreadStack(const CrashInfo& crashInfo, uint64_t* startAddress, size_t* size) const;
    void GetThreadCode(uint64_t* startAddress, size_t* size) const;
    void GetThreadContext(uint32_t flags, CONTEXT* context) const;

    const pid_t Tid() const { return m_tid; }
    const pid_t Ppid() const { return m_ppid; }
    const pid_t Tgid() const { return m_tgid; }

    const user_regs_struct* GPRegisters() const { return &m_gpRegisters; }
    const user_fpregs_struct* FPRegisters() const { return &m_fpRegisters; }
#if defined(__i386__)
    const user_fpxregs_struct* FPXRegisters() const { return &m_fpxRegisters; }
#elif defined(__arm__) && defined(__VFP_FP__) && !defined(__SOFTFP__)
    const user_vfpregs_struct* VFPRegisters() const { return &m_vfpRegisters; }
#endif

private:
    bool GetRegistersWithPTrace();
    bool GetRegistersWithDataTarget(ICLRDataTarget* dataTarget);
};
