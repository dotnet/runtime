// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef PROCESS_READER_H
#define PROCESS_READER_H

#include <unistd.h>
#include <sys/types.h>

#ifndef __APPLE__
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
#endif // __APPLE__

typedef __typeof__(((elf_aux_entry*) 0)->a_un.a_val) elf_aux_val_t;

// All interesting auvx entry types are AT_SYSINFO_EHDR and below
#define AT_MAX (AT_SYSINFO_EHDR + 1)

#endif

#ifndef MAX_LONGPATH
#define MAX_LONGPATH   1024
#endif

class DumpRegionStore
{
public:
    DumpRegionStore(
        void* container,
        bool (*findOverlap)(void*, uint64_t, uint64_t, MemoryRegion*),
        bool (*insert)(void*, const MemoryRegion*)) noexcept :
        m_container(container),
        m_findOverlap(findOverlap),
        m_insert(insert)
    {
    }

    bool FindOverlap(uint64_t startAddress, uint64_t endAddress, MemoryRegion* result) const
    {
        return m_findOverlap(m_container, startAddress, endAddress, result);
    }

    bool Insert(const MemoryRegion* region) const
    {
        return m_insert(m_container, region);
    }

    void* Container() const { return m_container; }

private:
    void* m_container;

    bool (*m_findOverlap)(
        void* container,
        uint64_t startAddress,
        uint64_t endAddress,
        MemoryRegion* result);

    bool (*m_insert)(
        void* container,
        const MemoryRegion* region);
};

// Process snapshot containing all data needed for dump generation
class ProcessInfo
{
    pid_t m_pid;                                    // pid
    pid_t m_ppid;                                   // parent pid
    pid_t m_tgid;                                   // process group
    uint64_t m_pageSize;
    uint64_t m_runtimeBaseAddress;                  // base address of the runtime module

    char m_exeName[MAX_LONGPATH]; // prefer a constant here
    int m_crashSignal;                              // crash signal code or 0 if none
    pid_t m_crashThread;                            // crashing thread id or 0 if none
    siginfo_t m_siginfo;                            // signal info (if any)
    uint64_t m_exceptionRecord;                     // exception record address or 0 if none

#ifdef __APPLE__
    vm_map_t m_task = 0;                            // the mach task for the process
#else
    int m_fdMemory = -1;                            // /proc/<pid>/mem handle
    int m_fdPagemap = -1;                           // /proc/<pid>/pagemap handle
    bool m_canUseProcVmReadSyscall = true;
    DynamicArray<elf_aux_entry> m_auxvEntries;
    elf_aux_val_t m_auxvValues[AT_MAX]{};
#endif
    DynamicArray<ThreadSnapshot> m_threads;
    DynamicArray<ModuleRegion> m_moduleMappings;
    DynamicArray<MemoryRegion> m_otherMappings;

public:
    explicit ProcessInfo(const CreateDumpOptions& options) noexcept :
        m_pid(options.Pid),
        m_ppid(0),
        m_tgid(0),
        m_pageSize(0),
        m_runtimeBaseAddress(0),
        m_exeName{},
        m_crashSignal(options.Signal),
        m_crashThread(options.CrashThread),
        m_exceptionRecord(options.ExceptionRecord)
    {
        memset(&m_siginfo, 0, sizeof(m_siginfo));
        m_siginfo.si_signo = options.Signal;
        m_siginfo.si_code = options.SignalCode;
        m_siginfo.si_errno = options.SignalErrno;
        m_siginfo.si_addr = (void*)options.SignalAddress;
    }

    bool Initialize();
    void CleanupAndResumeProcess();
    bool EnumerateAndSuspendThreads();
    bool GatherCrashInfo(DumpRegionStore& regionStore);
    bool SelectDumpRegions(DumpRegionStore& regionStore, DumpType dumpType);
    bool ReadProcessMemory(uint64_t address, void* buffer, size_t size, size_t* read);
    bool AddMapping(const MemoryRegion& region);
    bool AddMapping(const ModuleRegion& region);
    int InsertMemoryRegion(DumpRegionStore& regionStore, const MemoryRegion& memoryRegion);
#ifndef __APPLE__
    void CalculateRuntimeBaseAddress();
#endif

    pid_t Pid() const { return m_pid; }
    pid_t Ppid() const { return m_ppid; }
    pid_t Tgid() const { return m_tgid; }
    pid_t CrashThread() const { return m_crashThread; }
    int Signal() const { return m_crashSignal; }
    const siginfo_t* SigInfo() const { return &m_siginfo; }
    uint64_t ExceptionRecord() const { return m_exceptionRecord; }
    uint64_t PageSize() const { return m_pageSize; }
    uint64_t RuntimeBaseAddress() const { return m_runtimeBaseAddress; }
    const char* Name() const { return m_exeName; }
    void SetRuntimeBaseAddress(uint64_t address) { m_runtimeBaseAddress = address; }
#ifdef __APPLE__
    vm_map_t Task() const { return m_task; }
#endif
    void SetName(const char* name)
    {
        strncpy(m_exeName, name != nullptr ? name : "", sizeof(m_exeName));
        m_exeName[sizeof(m_exeName) - 1] = '\0';
    }

    DynamicArray<ThreadSnapshot>& Threads() noexcept { return m_threads; }
    const DynamicArray<ThreadSnapshot>& Threads() const noexcept { return m_threads; }
    const DynamicArray<ModuleRegion>& ModuleMappings() const noexcept { return m_moduleMappings; }
    const DynamicArray<MemoryRegion>& OtherMappings() const noexcept { return m_otherMappings; }
#ifndef __APPLE__
    const DynamicArray<elf_aux_entry>& AuxvEntries() const noexcept { return m_auxvEntries; }
    size_t GetAuxvSize() const { return m_auxvEntries.Count() * sizeof(elf_aux_entry); }
    elf_aux_val_t AuxvValue(size_t index) const { return m_auxvValues[index]; }
#endif

private:
    bool EnumerateMemoryRegions(DumpRegionStore& regionStore);
    bool GetAuxvEntries();
    bool PageCanBeRead(uint64_t start);
    bool PageMappedToPhysicalMemory(uint64_t start);
};

#endif // PROCESS_READER_H