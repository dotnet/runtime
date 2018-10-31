// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// typedef for our parsing of the auxv variables in /proc/pid/auxv.
#if defined(__i386) || defined(__ARM_EABI__) 
typedef Elf32_auxv_t elf_aux_entry;
#define PRIx PRIx32
#define PRIu PRIu32
#define PRId PRId32
#define PRIA "08"
#define PRIxA PRIA PRIx
#elif defined(__x86_64__) || defined(__aarch64__)
typedef Elf64_auxv_t elf_aux_entry;
#define PRIx PRIx64
#define PRIu PRIu64
#define PRId PRId64
#define PRIA "016"
#define PRIxA PRIA PRIx
#endif

typedef __typeof__(((elf_aux_entry*) 0)->a_un.a_val) elf_aux_val_t;

// All interesting auvx entry types are AT_SYSINFO_EHDR and below
#define AT_MAX (AT_SYSINFO_EHDR + 1)

class CrashInfo : public ICLRDataEnumMemoryRegionsCallback
{
private:
    LONG m_ref;                                     // reference count
    pid_t m_pid;                                    // pid
    pid_t m_ppid;                                   // parent pid
    pid_t m_tgid;                                   // process group
    char* m_name;                                   // exe name
    bool m_sos;                                     // if true, running under sos
    std::string m_coreclrPath;                      // the path of the coreclr module or empty if none
    ICLRDataTarget* m_dataTarget;                   // read process memory, etc.
    std::array<elf_aux_val_t, AT_MAX> m_auxvValues; // auxv values
    std::vector<elf_aux_entry> m_auxvEntries;       // full auxv entries
    std::vector<ThreadInfo*> m_threads;             // threads found and suspended
    std::set<MemoryRegion> m_moduleMappings;        // module memory mappings
    std::set<MemoryRegion> m_otherMappings;         // other memory mappings
    std::set<MemoryRegion> m_memoryRegions;         // memory regions from DAC, etc.
    std::set<MemoryRegion> m_moduleAddresses;       // memory region to module base address

public:
    CrashInfo(pid_t pid, ICLRDataTarget* dataTarget, bool sos);
    virtual ~CrashInfo();
    bool EnumerateAndSuspendThreads();
    bool GatherCrashInfo(MINIDUMP_TYPE minidumpType);
    void ResumeThreads();
    bool ReadMemory(void* address, void* buffer, size_t size);
    uint64_t GetBaseAddress(uint64_t ip);
    void InsertMemoryRegion(uint64_t address, size_t size);
    static bool GetStatus(pid_t pid, pid_t* ppid, pid_t* tgid, char** name);
    static const MemoryRegion* SearchMemoryRegions(const std::set<MemoryRegion>& regions, const MemoryRegion& search);

    inline pid_t Pid() const { return m_pid; }
    inline pid_t Ppid() const { return m_ppid; }
    inline pid_t Tgid() const { return m_tgid; }
    inline const char* Name() const { return m_name; }
    inline ICLRDataTarget* DataTarget() const { return m_dataTarget; }

    inline const std::vector<ThreadInfo*> Threads() const { return m_threads; }
    inline const std::set<MemoryRegion> ModuleMappings() const { return m_moduleMappings; }
    inline const std::set<MemoryRegion> OtherMappings() const { return m_otherMappings; }
    inline const std::set<MemoryRegion> MemoryRegions() const { return m_memoryRegions; }
    inline const std::vector<elf_aux_entry> AuxvEntries() const { return m_auxvEntries; }
    inline size_t GetAuxvSize() const { return m_auxvEntries.size() * sizeof(elf_aux_entry); }

    // IUnknown
    STDMETHOD(QueryInterface)(___in REFIID InterfaceId, ___out PVOID* Interface);
    STDMETHOD_(ULONG, AddRef)();
    STDMETHOD_(ULONG, Release)();

    // ICLRDataEnumMemoryRegionsCallback
    virtual HRESULT STDMETHODCALLTYPE EnumMemoryRegion(/* [in] */ CLRDATA_ADDRESS address, /* [in] */ ULONG32 size);

private:
    bool GetAuxvEntries();
    bool EnumerateModuleMappings();
    bool GetDSOInfo();
    bool GetELFInfo(uint64_t baseAddress);
    bool EnumerateProgramHeaders(ElfW(Phdr)* phdrAddr, int phnum, uint64_t baseAddress, ElfW(Dyn)** pdynamicAddr);
    bool EnumerateMemoryRegionsWithDAC(MINIDUMP_TYPE minidumpType);
    bool EnumerateManagedModules(IXCLRDataProcess* pClrDataProcess);
    bool UnwindAllThreads(IXCLRDataProcess* pClrDataProcess);
    void ReplaceModuleMapping(CLRDATA_ADDRESS baseAddress, const char* pszName);
    void InsertMemoryBackedRegion(const MemoryRegion& region);
    void InsertMemoryRegion(const MemoryRegion& region);
    uint32_t GetMemoryRegionFlags(uint64_t start);
    bool ValidRegion(const MemoryRegion& region);
    void CombineMemoryRegions();
};
