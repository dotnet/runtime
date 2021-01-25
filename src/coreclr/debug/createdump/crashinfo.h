// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef __APPLE__
#include "../dbgutil/machoreader.h"
#else
#include "../dbgutil/elfreader.h"
#endif

// typedef for our parsing of the auxv variables in /proc/pid/auxv.
#if TARGET_64BIT
typedef Elf64_auxv_t elf_aux_entry;
#define PRIx PRIx64
#define PRIu PRIu64
#define PRId PRId64
#define PRIA "016"
#define PRIxA PRIA PRIx
#else
typedef Elf32_auxv_t elf_aux_entry;
#define PRIx PRIx32
#define PRIu PRIu32
#define PRId PRId32
#define PRIA "08"
#define PRIxA PRIA PRIx
#endif

typedef __typeof__(((elf_aux_entry*) 0)->a_un.a_val) elf_aux_val_t;

// All interesting auvx entry types are AT_SYSINFO_EHDR and below
#define AT_MAX (AT_SYSINFO_EHDR + 1)

class CrashInfo : public ICLRDataEnumMemoryRegionsCallback,
#ifdef __APPLE__
    public MachOReader
#else
    public ElfReader
#endif
{
private:
    LONG m_ref;                                     // reference count
    pid_t m_pid;                                    // pid
    pid_t m_ppid;                                   // parent pid
    pid_t m_tgid;                                   // process group
    std::string m_name;                             // exe name
#ifdef __APPLE__
    vm_map_t m_task;                                // the mach task for the process
#else
#ifndef HAVE_PROCESS_VM_READV
    int m_fd;                                       // /proc/<pid>/mem handle
#endif
#endif
    std::string m_coreclrPath;                      // the path of the coreclr module or empty if none
#ifdef __APPLE__
    std::set<MemoryRegion> m_allMemoryRegions;      // all memory regions on MacOS
#else
    std::array<elf_aux_val_t, AT_MAX> m_auxvValues; // auxv values
#endif
    std::vector<elf_aux_entry> m_auxvEntries;       // full auxv entries
    std::vector<ThreadInfo*> m_threads;             // threads found and suspended
    std::set<MemoryRegion> m_moduleMappings;        // module memory mappings
    std::set<MemoryRegion> m_otherMappings;         // other memory mappings
    std::set<MemoryRegion> m_memoryRegions;         // memory regions from DAC, etc.
    std::set<MemoryRegion> m_moduleAddresses;       // memory region to module base address

public:
    CrashInfo(pid_t pid);
    virtual ~CrashInfo();

    bool Initialize();
    void CleanupAndResumeProcess();
    bool EnumerateAndSuspendThreads();
    bool GatherCrashInfo(MINIDUMP_TYPE minidumpType);
    bool ReadMemory(void* address, void* buffer, size_t size);                          // read memory and add to dump
    bool ReadProcessMemory(void* address, void* buffer, size_t size, size_t* read);     // read raw memory
    uint64_t GetBaseAddress(uint64_t ip);
    void InsertMemoryRegion(uint64_t address, size_t size);
    static const MemoryRegion* SearchMemoryRegions(const std::set<MemoryRegion>& regions, const MemoryRegion& search);

    inline pid_t Pid() const { return m_pid; }
    inline pid_t Ppid() const { return m_ppid; }
    inline pid_t Tgid() const { return m_tgid; }
#ifdef __APPLE__
    inline vm_map_t Task() const { return m_task; }
#endif
    inline const std::string& Name() const { return m_name; }

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
#ifdef __APPLE__
    bool EnumerateMemoryRegions();
    bool TryFindDyLinker(mach_vm_address_t address, mach_vm_size_t size, bool* found);
    void VisitModule(MachOModule& module);
    void VisitSegment(MachOModule& module, const segment_command_64& segment);
    void VisitSection(MachOModule& module, const section_64& section);
#else
    bool GetAuxvEntries();
    bool GetDSOInfo();
    void VisitModule(uint64_t baseAddress, std::string& moduleName);
    void VisitProgramHeader(uint64_t loadbias, uint64_t baseAddress, ElfW(Phdr)* phdr);
    bool EnumerateModuleMappings();
#endif 
    bool EnumerateMemoryRegionsWithDAC(MINIDUMP_TYPE minidumpType);
    bool EnumerateManagedModules(IXCLRDataProcess* pClrDataProcess);
    bool UnwindAllThreads(IXCLRDataProcess* pClrDataProcess);
    void ReplaceModuleMapping(CLRDATA_ADDRESS baseAddress, ULONG64 size, const std::string& pszName);
    void InsertMemoryBackedRegion(const MemoryRegion& region);
    void InsertMemoryRegion(const MemoryRegion& region);
    uint32_t GetMemoryRegionFlags(uint64_t start);
    bool ValidRegion(const MemoryRegion& region);
    void CombineMemoryRegions();
    void Trace(const char* format, ...);
};
