// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifdef BIT64
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
#endif

#define PH_HDR_CANARY 0xFFFF

#ifndef NT_FILE
#define NT_FILE		0x46494c45
#endif

class DumpWriter : IUnknown
{
private:
    LONG m_ref;                         // reference count
    int m_fd;
    CrashInfo& m_crashInfo;
    BYTE m_tempBuffer[0x4000];

public:
    DumpWriter(CrashInfo& crashInfo);
    virtual ~DumpWriter();
    bool OpenDump(const char* dumpFileName);
    bool WriteDump();

    // IUnknown
    STDMETHOD(QueryInterface)(___in REFIID InterfaceId, ___out PVOID* Interface);
    STDMETHOD_(ULONG, AddRef)();
    STDMETHOD_(ULONG, Release)();

private:
    bool WriteProcessInfo();
    bool WriteAuxv();
    size_t GetNTFileInfoSize(size_t* alignmentBytes = nullptr);
    bool WriteNTFileInfo();
    bool WriteThread(const ThreadInfo& thread, int fatal_signal);
    bool WriteData(const void* buffer, size_t length);

    size_t GetProcessInfoSize() const { return sizeof(Nhdr) + 8 + sizeof(prpsinfo_t); }
    size_t GetAuxvInfoSize() const { return sizeof(Nhdr) + 8 + m_crashInfo.GetAuxvSize(); }
    size_t GetThreadInfoSize() const
    {
        return m_crashInfo.Threads().size() * ((sizeof(Nhdr) + 8 + sizeof(prstatus_t))
            + sizeof(Nhdr) + 8 + sizeof(user_fpregs_struct)
#if defined(__i386__)
            + sizeof(Nhdr) + 8 + sizeof(user_fpxregs_struct)
#endif
#if defined(__arm__) && defined(__VFP_FP__) && !defined(__SOFTFP__)
            + sizeof(Nhdr) + 8 + sizeof(user_vfpregs_struct)
#endif
        );
    }
};
