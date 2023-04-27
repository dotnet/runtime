// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef HOST_64BIT
#define ELF_CLASS ELFCLASS64
#else
#define ELF_CLASS ELFCLASS32
#endif

#define Ehdr   ElfW(Ehdr)
#define Phdr   ElfW(Phdr)
#define Shdr   ElfW(Shdr)
#define Nhdr   ElfW(Nhdr)
#define auxv_t ElfW(auxv_t)

#if defined(__x86_64__)
#define ELF_ARCH  EM_X86_64
#elif defined(__i386__)
#define ELF_ARCH  EM_386
#elif defined(__aarch64__)
#define ELF_ARCH  EM_AARCH64
#elif defined(__arm__)
#define ELF_ARCH  EM_ARM
#elif defined(__loongarch64)
#define ELF_ARCH  EM_LOONGARCH
#elif defined(__riscv)
#define ELF_ARCH  EM_RISCV
#endif

#define PH_HDR_CANARY 0xFFFF

#ifndef NT_FILE
#define NT_FILE		0x46494c45
#endif

#ifndef NT_SIGINFO	
#define NT_SIGINFO	0x53494749
#endif

class DumpWriter
{
private:
    int m_fd;
    CrashInfo& m_crashInfo;
    BYTE m_tempBuffer[0x4000];

    // no public copy constructor
    DumpWriter(const DumpWriter&) = delete;
    void operator=(const DumpWriter&) = delete;

public:
    DumpWriter(CrashInfo& crashInfo);
    virtual ~DumpWriter();
    bool OpenDump(const char* dumpFileName);
    bool WriteDump();
    static bool WriteData(int fd, const void* buffer, size_t length);

private:
    bool WriteProcessInfo();
    bool WriteAuxv();
    size_t GetNTFileInfoSize(size_t* alignmentBytes = nullptr);
    bool WriteNTFileInfo();
    bool WriteThread(const ThreadInfo& thread);
    bool WriteData(const void* buffer, size_t length) { return WriteData(m_fd, buffer, length); }

    size_t GetProcessInfoSize() const { return sizeof(Nhdr) + 8 + sizeof(prpsinfo_t); }
    size_t GetAuxvInfoSize() const { return sizeof(Nhdr) + 8 + m_crashInfo.GetAuxvSize(); }
    size_t GetThreadInfoSize() const
    {
        return (m_crashInfo.Signal() != 0 ? (sizeof(Nhdr) + 8 + sizeof(siginfo_t)) : 0)
              + (m_crashInfo.Threads().size() * ((sizeof(Nhdr) + 8 + sizeof(prstatus_t))
              + (sizeof(Nhdr) + 8 + sizeof(user_fpregs_struct))
#if defined(__i386__)
              + (sizeof(Nhdr) + 8 + sizeof(user_fpxregs_struct))
#endif
#if defined(__arm__) && defined(__VFP_FP__) && !defined(__SOFTFP__)
              + (sizeof(Nhdr) + 8 + sizeof(user_vfpregs_struct))
#endif
        ));
    }
};
