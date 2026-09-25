// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef PROCESS_READER_H
#define PROCESS_READER_H

#include <unistd.h>
#include <sys/types.h>

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

    char m_exeName[MAX_LONGPATH]; // prefer a constant here
    int m_crashSignal;                              // crash signal code or 0 if none
    pid_t m_crashThread;                            // crashing thread id or 0 if none
    int m_signalCode;                               // crash signal code or 0 if none
    int m_signalErrno;
    uint64_t m_signalAddress;
    uint64_t m_exceptionRecord;                     // exception record address or 0 if none

#ifdef __APPLE__
    vm_map_t m_task = 0;                            // the mach task for the process
#else
    int m_fdMemory = -1;
    int m_fdPagemap = -1;
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
        m_exeName{},
        m_crashSignal(options.Signal),
        m_crashThread(options.CrashThread),
        m_signalCode(options.SignalCode),
        m_signalErrno(options.SignalErrno),
        m_signalAddress(options.SignalAddress),
        m_exceptionRecord(options.ExceptionRecord)
    {
    }

    bool Initialize();
    void CleanupAndResumeProcess();
    bool EnumerateAndSuspendThreads();
    bool GatherCrashInfo(DumpRegionStore& regionStore);
    bool SelectDumpRegions(DumpRegionStore& regionStore, DumpType dumpType);
    bool ReadProcessMemory(uint64_t address, void* buffer, size_t size, size_t* read);
    bool AddMapping(const MemoryRegion& region);
    bool AddMapping(const ModuleRegion& region);

    pid_t Pid() const { return m_pid; }
    pid_t Ppid() const { return m_ppid; }
    pid_t Tgid() const { return m_tgid; }
    pid_t CrashThread() const { return m_crashThread; }
    int Signal() const { return m_crashSignal; }
    uint64_t ExceptionRecord() const { return m_exceptionRecord; }
    uint64_t PageSize() const { return m_pageSize; }
    const char* Name() const { return m_exeName; }
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
    elf_aux_val_t AuxvValue(size_t index) const { return m_auxvValues[index]; }
#endif

private:
    bool EnumerateMemoryRegions(DumpRegionStore& regionStore);
    bool GetAuxvEntries();
    int InsertMemoryRegion(DumpRegionStore& regionStore, const MemoryRegion& memoryRegion);
    bool PageCanBeRead(uint64_t start);
    bool PageMappedToPhysicalMemory(uint64_t start);
};

#endif // PROCESS_READER_H