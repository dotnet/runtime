// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

class CrashInfo;

#define STACK_OVERFLOW_EXCEPTION    0x800703e9

class ThreadInfo
{
private:
    CrashInfo& m_crashInfo;                     // crashinfo instance
    ThreadSnapshot m_snapshot;                  // thread snapshot

    bool m_managed;                             // if true, thread has managed code running
    uint64_t m_exceptionObject;                 // exception object address
    std::string m_exceptionType;                // exception type
    uint32_t m_exceptionHResult;                // exception HRESULT
    std::set<StackFrame> m_frames;              // stack frames
    int m_repeatedFrames;                       // number of repeated frames
    std::set<StackFrame>::const_iterator m_beginRepeat;   // beginning of stack overflow repeated frame sequence
    std::set<StackFrame>::const_iterator m_endRepeat;     // end of repeated frame sequence

    // no public copy constructor
    ThreadInfo(const ThreadInfo&) = delete;
    void operator=(const ThreadInfo&) = delete;

public:
    ThreadInfo(CrashInfo& crashInfo, const ThreadSnapshot& snapshot);
    ~ThreadInfo();
    bool UnwindThread(IXCLRDataProcess* pClrDataProcess, ISOSDacInterface* pSos);
    void GetThreadStack();
    void GetThreadContext(uint32_t flags, CONTEXT* context) const { m_snapshot.GetThreadContext(flags, context); }

    inline pid_t Tid() const { return m_snapshot.Tid(); }
    inline pid_t Ppid() const { return m_snapshot.Ppid(); }
    inline pid_t Tgid() const { return m_snapshot.Tgid(); }

    inline bool IsManaged() const { return m_managed; }
    inline uint64_t ManagedExceptionObject() const { return m_exceptionObject; }
    inline uint32_t ManagedExceptionHResult() const { return m_exceptionHResult; }
    inline std::string ManagedExceptionType() const { return m_exceptionType; }
    inline const std::set<StackFrame>& StackFrames() const { return m_frames; }
    inline int NumRepeatedFrames() const { return m_repeatedFrames;  }
    inline bool IsBeginRepeat(std::set<StackFrame>::const_iterator& iterator) const { return m_repeatedFrames > 0 && iterator == m_beginRepeat; }
    inline bool IsEndRepeat(std::set<StackFrame>::const_iterator& iterator) const { return  m_repeatedFrames > 0 && iterator == m_endRepeat; }

#ifdef __APPLE__
#if defined(__x86_64__)
    inline const x86_thread_state64_t* GPRegisters() const { return m_snapshot.GPRegisters(); }
    inline const x86_float_state64_t* FPRegisters() const { return m_snapshot.FPRegisters(); }
#elif defined(__aarch64__)
    inline const arm_thread_state64_t* GPRegisters() const { return m_snapshot.GPRegisters(); }
    inline const arm_neon_state64_t* FPRegisters() const { return m_snapshot.FPRegisters(); }
#endif
#else
    inline const user_regs_struct* GPRegisters() const { return m_snapshot.GPRegisters(); }
    inline const user_fpregs_struct* FPRegisters() const { return m_snapshot.FPRegisters(); }
#if defined(__i386__)
    inline const user_fpxregs_struct* FPXRegisters() const { return m_snapshot.FPXRegisters(); }
#elif defined(__arm__) && defined(__VFP_FP__) && !defined(__SOFTFP__)
    inline const user_vfpregs_struct* VFPRegisters() const { return m_snapshot.VFPRegisters(); }
#endif
#endif // __APPLE__
    inline uint64_t GetInstructionPointer() const { return m_snapshot.GetInstructionPointer(); }
    inline uint64_t GetStackPointer() const { return m_snapshot.GetStackPointer(); }
    inline uint64_t GetFramePointer() const { return m_snapshot.GetFramePointer(); }
    bool IsCrashThread() const;

private:
    void UnwindNativeFrames(CONTEXT* pContext);
    void GatherStackFrames(CONTEXT* pContext, IXCLRDataStackWalk* pStackwalk);
    void AddStackFrame(const StackFrame& frame);
};
