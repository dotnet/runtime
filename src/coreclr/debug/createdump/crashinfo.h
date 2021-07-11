// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef __APPLE__
#include "../dbgutil/machoreader.h"
#else
#include "../dbgutil/elfreader.h"

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

#endif

extern const std::string GetFileName(const std::string& fileName);
extern std::string FormatString(const char* format, ...);
extern std::string FormatGuid(const GUID* guid);

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
    HMODULE m_hdac;                                 // dac module handle when loaded
    bool m_gatherFrames;                            // if true, add the native and managed stack frames to the thread info
    pid_t m_crashThread;                            // crashing thread id or 0 if none
    uint32_t m_signal;                              // crash signal code or 0 if none
    std::string m_name;                             // exe name
#ifdef __APPLE__
    vm_map_t m_task;                                // the mach task for the process
#else
    bool m_canUseProcVmReadSyscall;
    int m_fd;                                       // /proc/<pid>/mem handle
#endif
    std::string m_coreclrPath;                      // the path of the coreclr module or empty if none
#ifdef __APPLE__
    std::set<MemoryRegion> m_allMemoryRegions;      // all memory regions on MacOS
#else
    std::array<elf_aux_val_t, AT_MAX> m_auxvValues; // auxv values
    std::vector<elf_aux_entry> m_auxvEntries;       // full auxv entries
#endif
    std::vector<ThreadInfo*> m_threads;             // threads found and suspended
    std::set<MemoryRegion> m_moduleMappings;        // module memory mappings
    std::set<MemoryRegion> m_otherMappings;         // other memory mappings
    std::set<MemoryRegion> m_memoryRegions;         // memory regions from DAC, etc.
    std::set<MemoryRegion> m_moduleAddresses;       // memory region to module base address
    std::set<ModuleInfo*, bool (*)(const ModuleInfo* lhs, const ModuleInfo* rhs)> m_moduleInfos; // module infos (base address and module name)
    ModuleInfo* m_mainModule;                       // the module containing "Main"

    // no public copy constructor
    CrashInfo(const CrashInfo&) = delete;
    void operator=(const CrashInfo&) = delete;

public:
    CrashInfo(pid_t pid, bool gatherFrames, pid_t crashThread, uint32_t signal);
    virtual ~CrashInfo();

    bool Initialize();
    void CleanupAndResumeProcess();
    bool EnumerateAndSuspendThreads();
    bool GatherCrashInfo(MINIDUMP_TYPE minidumpType);
    bool ReadMemory(void* address, void* buffer, size_t size);                          // read memory and add to dump
    bool ReadProcessMemory(void* address, void* buffer, size_t size, size_t* read);     // read raw memory
    uint64_t GetBaseAddressFromAddress(uint64_t address);
    uint64_t GetBaseAddressFromName(const char* moduleName);
    ModuleInfo* GetModuleInfoFromBaseAddress(uint64_t baseAddress);
    void AddModuleAddressRange(uint64_t startAddress, uint64_t endAddress, uint64_t baseAddress);
    void AddModuleInfo(bool isManaged, uint64_t baseAddress, IXCLRDataModule* pClrDataModule, const std::string& moduleName);
    void InsertMemoryRegion(uint64_t address, size_t size);
    static const MemoryRegion* SearchMemoryRegions(const std::set<MemoryRegion>& regions, const MemoryRegion& search);

    inline pid_t Pid() const { return m_pid; }
    inline pid_t Ppid() const { return m_ppid; }
    inline pid_t Tgid() const { return m_tgid; }
#ifdef __APPLE__
    inline vm_map_t Task() const { return m_task; }
#endif
    inline const bool GatherFrames() const { return m_gatherFrames; }
    inline const pid_t CrashThread() const { return m_crashThread; }
    inline const uint32_t Signal() const { return m_signal; }
    inline const std::string& Name() const { return m_name; }
    inline const ModuleInfo* MainModule() const { return m_mainModule; }

    inline const std::vector<ThreadInfo*> Threads() const { return m_threads; }
    inline const std::set<MemoryRegion> ModuleMappings() const { return m_moduleMappings; }
    inline const std::set<MemoryRegion> OtherMappings() const { return m_otherMappings; }
    inline const std::set<MemoryRegion> MemoryRegions() const { return m_memoryRegions; }
#ifndef __APPLE__
    inline const std::vector<elf_aux_entry> AuxvEntries() const { return m_auxvEntries; }
    inline size_t GetAuxvSize() const { return m_auxvEntries.size() * sizeof(elf_aux_entry); }
#endif

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
    void TraceVerbose(const char* format, ...);
};
