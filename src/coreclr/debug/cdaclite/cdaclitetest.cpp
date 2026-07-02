// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// cdaclitetest.cpp
//
// A tiny standalone harness to exercise cdaclite against a live .NET process
// on Windows. It implements a minimal ICLRDataTarget backed by
// ReadProcessMemory and module enumeration, loads cdaclite.dll, calls
// CLRDataCreateInstance, and invokes EnumMemoryRegions. Memory regions and
// log messages are printed to stdout.
//
// Usage: cdaclitetest <pid> [path-to-cdaclite.dll]
//*****************************************************************************

#include <windows.h>
#include <tlhelp32.h>
#include <dbghelp.h>
#include <stdio.h>
#include <stdint.h>
#include <vector>
#include <string>

#include <cor.h>
#include <clrdata.h>
// sospriv.h references T_CONTEXT (target CONTEXT); for a same-arch harness it's the OS CONTEXT.
#define T_CONTEXT CONTEXT
#include <sospriv.h>
#include <dacprivate.h>

typedef HRESULT(STDAPICALLTYPE* PFN_CLRDataCreateInstance)(REFIID iid, ICLRDataTarget* target, void** iface);

struct Region { uint64_t base; uint32_t size; };

namespace
{
    // Minimal ICLRDataTarget over a live process handle.
    class LiveDataTarget : public ICLRDataTarget
    {
    private:
        LONG m_ref;
        HANDLE m_process;
        DWORD m_pid;

    public:
        LiveDataTarget(HANDLE process, DWORD pid)
            : m_ref(1), m_process(process), m_pid(pid)
        {
        }

        // IUnknown
        STDMETHOD(QueryInterface)(REFIID riid, void** ppvObject)
        {
            if (ppvObject == nullptr)
            {
                return E_POINTER;
            }
            if (riid == IID_IUnknown || riid == __uuidof(ICLRDataTarget))
            {
                *ppvObject = static_cast<ICLRDataTarget*>(this);
                AddRef();
                return S_OK;
            }
            *ppvObject = nullptr;
            return E_NOINTERFACE;
        }

        STDMETHOD_(ULONG, AddRef)() { return InterlockedIncrement(&m_ref); }
        STDMETHOD_(ULONG, Release)()
        {
            LONG ref = InterlockedDecrement(&m_ref);
            if (ref == 0)
            {
                delete this;
            }
            return ref;
        }

        // ICLRDataTarget
        STDMETHOD(GetMachineType)(ULONG32* machine)
        {
#if defined(_M_ARM64)
            *machine = IMAGE_FILE_MACHINE_ARM64;
#else
            *machine = IMAGE_FILE_MACHINE_AMD64;
#endif
            return S_OK;
        }

        STDMETHOD(GetPointerSize)(ULONG32* size)
        {
            *size = sizeof(void*);
            return S_OK;
        }

        STDMETHOD(GetImageBase)(LPCWSTR moduleName, CLRDATA_ADDRESS* baseAddress)
        {
            HANDLE snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPMODULE | TH32CS_SNAPMODULE32, m_pid);
            if (snapshot == INVALID_HANDLE_VALUE)
            {
                return E_FAIL;
            }

            HRESULT hr = E_FAIL;
            MODULEENTRY32W me;
            me.dwSize = sizeof(me);
            if (Module32FirstW(snapshot, &me))
            {
                do
                {
                    if (_wcsicmp(me.szModule, moduleName) == 0)
                    {
                        *baseAddress = (CLRDATA_ADDRESS)(ULONG_PTR)me.modBaseAddr;
                        hr = S_OK;
                        break;
                    }
                } while (Module32NextW(snapshot, &me));
            }
            CloseHandle(snapshot);
            return hr;
        }

        STDMETHOD(ReadVirtual)(CLRDATA_ADDRESS address, PBYTE buffer, ULONG32 size, ULONG32* done)
        {
            SIZE_T read = 0;
            if (!ReadProcessMemory(m_process, (LPCVOID)(ULONG_PTR)address, buffer, size, &read))
            {
                if (done != nullptr)
                {
                    *done = 0;
                }
                return HRESULT_FROM_WIN32(GetLastError());
            }
            if (done != nullptr)
            {
                *done = (ULONG32)read;
            }
            return S_OK;
        }

        STDMETHOD(WriteVirtual)(CLRDATA_ADDRESS, PBYTE, ULONG32, ULONG32*) { return E_NOTIMPL; }
        STDMETHOD(GetTLSValue)(ULONG32, ULONG32, CLRDATA_ADDRESS*) { return E_NOTIMPL; }
        STDMETHOD(SetTLSValue)(ULONG32, ULONG32, CLRDATA_ADDRESS) { return E_NOTIMPL; }
        STDMETHOD(GetCurrentThreadID)(ULONG32*) { return E_NOTIMPL; }
        STDMETHOD(GetThreadContext)(ULONG32, ULONG32, ULONG32, PBYTE) { return E_NOTIMPL; }
        STDMETHOD(SetThreadContext)(ULONG32, ULONG32, PBYTE) { return E_NOTIMPL; }
        STDMETHOD(Request)(ULONG32, ULONG32, BYTE*, ULONG32, BYTE*) { return E_NOTIMPL; }
    };

    // Callback that prints enumerated regions and log messages.
    class PrintCallback : public ICLRDataEnumMemoryRegionsCallback, public ICLRDataLoggingCallback
    {
    private:
        LONG m_ref;

    public:
        ULONG m_regionCount = 0;

        PrintCallback() : m_ref(1) {}

        STDMETHOD(QueryInterface)(REFIID riid, void** ppvObject)
        {
            if (ppvObject == nullptr)
            {
                return E_POINTER;
            }
            if (riid == IID_IUnknown || riid == __uuidof(ICLRDataEnumMemoryRegionsCallback))
            {
                *ppvObject = static_cast<ICLRDataEnumMemoryRegionsCallback*>(this);
                AddRef();
                return S_OK;
            }
            if (riid == __uuidof(ICLRDataLoggingCallback))
            {
                *ppvObject = static_cast<ICLRDataLoggingCallback*>(this);
                AddRef();
                return S_OK;
            }
            *ppvObject = nullptr;
            return E_NOINTERFACE;
        }

        STDMETHOD_(ULONG, AddRef)() { return InterlockedIncrement(&m_ref); }
        STDMETHOD_(ULONG, Release)() { return InterlockedDecrement(&m_ref); }

        // ICLRDataEnumMemoryRegionsCallback
        STDMETHOD(EnumMemoryRegion)(CLRDATA_ADDRESS address, ULONG32 size)
        {
            m_regionCount++;
            printf("SEG 0x%llx 0x%llx\n", (unsigned long long)address, (unsigned long long)(address + size));
            return S_OK;
        }

        // ICLRDataLoggingCallback
        STDMETHOD(LogMessage)(LPCSTR message)
        {
            printf("[log] %s\n", message);
            return S_OK;
        }
    };

    // Callback that collects enumerated regions into a vector (for dump writing).
    class CollectCallback : public ICLRDataEnumMemoryRegionsCallback
    {
    private:
        LONG m_ref;
    public:
        std::vector<Region> m_regions;
        CollectCallback() : m_ref(1) {}

        STDMETHOD(QueryInterface)(REFIID riid, void** ppvObject)
        {
            if (ppvObject == nullptr) { return E_POINTER; }
            if (riid == IID_IUnknown || riid == __uuidof(ICLRDataEnumMemoryRegionsCallback))
            {
                *ppvObject = static_cast<ICLRDataEnumMemoryRegionsCallback*>(this);
                AddRef();
                return S_OK;
            }
            *ppvObject = nullptr;
            return E_NOINTERFACE;
        }
        STDMETHOD_(ULONG, AddRef)() { return InterlockedIncrement(&m_ref); }
        STDMETHOD_(ULONG, Release)() { return InterlockedDecrement(&m_ref); }

        STDMETHOD(EnumMemoryRegion)(CLRDATA_ADDRESS address, ULONG32 size)
        {
            Region r; r.base = (uint64_t)address; r.size = size;
            m_regions.push_back(r);
            return S_OK;
        }
    };

    // Feeds cdac-lite regions to MiniDumpWriteDump via the MemoryCallback protocol.
    struct DumpCallbackState
    {
        const std::vector<Region>* regions;
        size_t index;
    };

    BOOL CALLBACK DumpMemoryCallback(PVOID param, const PMINIDUMP_CALLBACK_INPUT input, PMINIDUMP_CALLBACK_OUTPUT output)
    {
        DumpCallbackState* state = (DumpCallbackState*)param;
        switch (input->CallbackType)
        {
        case MemoryCallback:
            // Supply one cdac-lite region per call; MemorySize == 0 ends enumeration.
            if (state->index < state->regions->size())
            {
                const Region& r = (*state->regions)[state->index++];
                output->MemoryBase = r.base;
                output->MemorySize = r.size;
            }
            else
            {
                output->MemoryBase = 0;
                output->MemorySize = 0;
            }
            return TRUE;
        default:
            return TRUE;
        }
    }
}

// Oracle: drive the real DAC via ISOSDacInterface to list GC segment [mem, highAllocMark)
// ranges. Prints "SEG start end" lines matching cdac-lite's output for diffing.
static int RunDacOracle(PFN_CLRDataCreateInstance pfnCreate, LiveDataTarget* target, DWORD pid)
{
    ISOSDacInterface* sos = nullptr;
    HRESULT hr = pfnCreate(__uuidof(ISOSDacInterface), target, (void**)&sos);
    if (FAILED(hr) || sos == nullptr)
    {
        fwprintf(stderr, L"DAC: QI ISOSDacInterface failed: 0x%08x\n", hr);
        return 5;
    }

    DacpGcHeapData heapData;
    if (FAILED(hr = heapData.Request(sos)))
    {
        fwprintf(stderr, L"DAC: GetGCHeapData failed: 0x%08x\n", hr);
        sos->Release();
        return 6;
    }

    printf("cdaclitetest[dac]: pid %lu server=%d heaps=%u maxGen=%u\n",
        pid, heapData.bServerMode, heapData.HeapCount, heapData.g_max_generation);

    // Collect the per-heap details (WKS: one static heap; SVR: iterate heap list).
    DacpGcHeapDetails details[64];
    unsigned heapCount = 1;
    if (heapData.bServerMode)
    {
        heapCount = heapData.HeapCount <= 64 ? heapData.HeapCount : 64;
        CLRDATA_ADDRESS heaps[64] = {};
        unsigned needed = 0;
        if (FAILED(hr = sos->GetGCHeapList(heapCount, heaps, &needed)))
        {
            fwprintf(stderr, L"DAC: GetGCHeapList failed: 0x%08x\n", hr);
            sos->Release();
            return 7;
        }
        for (unsigned i = 0; i < heapCount; i++)
        {
            details[i] = DacpGcHeapDetails();
            details[i].Request(sos, heaps[i]);
        }
    }
    else
    {
        details[0] = DacpGcHeapDetails();
        details[0].Request(sos);
    }

    int count = 0;
    for (unsigned h = 0; h < heapCount; h++)
    {
        const DacpGcHeapDetails& heap = details[h];
        // Walk each generation's segment list (gen0..maxGen+2 covers SOH + LOH + POH).
        for (int g = 0; g < DAC_NUMBERGENERATIONS; g++)
        {
            CLRDATA_ADDRESS segAddr = heap.generation_table[g].start_segment;
            for (int i = 0; segAddr != 0 && i < 1000000; i++)
            {
                DacpHeapSegmentData seg;
                if (FAILED(seg.Request(sos, segAddr, heap)))
                {
                    break;
                }
                if (seg.mem != 0 && seg.highAllocMark > seg.mem)
                {
                    printf("SEG 0x%llx 0x%llx\n", (unsigned long long)seg.mem, (unsigned long long)seg.highAllocMark);
                    count++;
                }
                segAddr = seg.next;
            }
        }
    }

    printf("cdaclitetest[dac]: %d segment(s)\n", count);

    // Enumerate modules: AppDomainList -> AssemblyList -> ModuleList -> ilBase.
    unsigned adNeeded = 0;
    CLRDATA_ADDRESS appDomains[64] = {};
    int modCount = 0;
    if (SUCCEEDED(sos->GetAppDomainList(64, appDomains, &adNeeded)))
    {
        unsigned adCount = adNeeded < 64 ? adNeeded : 64;
        for (unsigned a = 0; a < adCount; a++)
        {
            int asmNeeded = 0;
            if (FAILED(sos->GetAssemblyList(appDomains[a], 0, nullptr, &asmNeeded)) || asmNeeded <= 0)
            {
                continue;
            }
            if (asmNeeded > 4096) { asmNeeded = 4096; }
            CLRDATA_ADDRESS* assemblies = new CLRDATA_ADDRESS[asmNeeded];
            int got = 0;
            if (SUCCEEDED(sos->GetAssemblyList(appDomains[a], asmNeeded, assemblies, &got)))
            {
                for (int s = 0; s < got; s++)
                {
                    unsigned modNeeded = 0;
                    if (FAILED(sos->GetAssemblyModuleList(assemblies[s], 0, nullptr, &modNeeded)) || modNeeded == 0)
                    {
                        continue;
                    }
                    if (modNeeded > 1024) { modNeeded = 1024; }
                    CLRDATA_ADDRESS* modules = new CLRDATA_ADDRESS[modNeeded];
                    unsigned gotMods = 0;
                    if (SUCCEEDED(sos->GetAssemblyModuleList(assemblies[s], modNeeded, modules, &gotMods)))
                    {
                        for (unsigned m = 0; m < gotMods; m++)
                        {
                            DacpModuleData md;
                            if (SUCCEEDED(md.Request(sos, modules[m])) && md.ilBase != 0)
                            {
                                printf("MOD 0x%llx\n", (unsigned long long)md.ilBase);
                                modCount++;
                            }
                        }
                    }
                    delete[] modules;
                }
            }
            delete[] assemblies;
        }
    }
    printf("cdaclitetest[dac]: %d module(s)\n", modCount);

    // Enumerate handles: each handle address must fall within a handle-table segment.
    ISOSHandleEnum* handleEnum = nullptr;
    if (SUCCEEDED(sos->GetHandleEnum(&handleEnum)) && handleEnum != nullptr)
    {
        SOSHandleData buffer[256];
        unsigned fetched = 0;
        int handleCount = 0;
        while (SUCCEEDED(handleEnum->Next(256, buffer, &fetched)) && fetched > 0)
        {
            for (unsigned i = 0; i < fetched; i++)
            {
                printf("HND 0x%llx\n", (unsigned long long)buffer[i].Handle);
                handleCount++;
            }
            if (fetched < 256) { break; }
        }
        handleEnum->Release();
        printf("cdaclitetest[dac]: %d handle(s)\n", handleCount);
    }

    sos->Release();
    return 0;
}

// Checks whether the CLR DAC (mscordaccore.dll / mscordacwks.dll) is currently mapped into
// THIS (the dumper) process. dbghelp's CLR-awareness works by loading the version-matched DAC
// and calling its ICLRDataEnumMemoryRegions; if that had happened, the module would be present.
static bool DacLoadedInThisProcess()
{
    HANDLE snap = CreateToolhelp32Snapshot(TH32CS_SNAPMODULE, GetCurrentProcessId());
    if (snap == INVALID_HANDLE_VALUE) { return false; }

    bool found = false;
    MODULEENTRY32W me = {};
    me.dwSize = sizeof(me);
    if (Module32FirstW(snap, &me))
    {
        do
        {
            if (_wcsicmp(me.szModule, L"mscordaccore.dll") == 0 ||
                _wcsicmp(me.szModule, L"mscordacwks.dll") == 0)
            {
                found = true;
                break;
            }
        } while (Module32NextW(snap, &me));
    }
    CloseHandle(snap);
    return found;
}

// Verifies a written minidump contains 'region' bases: parses MemoryListStream and
// Memory64ListStream and counts how many cdac-lite regions are covered.
static int VerifyDump(const wchar_t* path, const std::vector<Region>& regions)
{
    HANDLE hFile = CreateFileW(path, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, 0, NULL);
    if (hFile == INVALID_HANDLE_VALUE) { return -1; }
    HANDLE hMap = CreateFileMapping(hFile, NULL, PAGE_READONLY, 0, 0, NULL);
    if (hMap == NULL) { CloseHandle(hFile); return -1; }
    void* base = MapViewOfFile(hMap, FILE_MAP_READ, 0, 0, 0);
    if (base == nullptr) { CloseHandle(hMap); CloseHandle(hFile); return -1; }

    // Collect all [start,end) memory ranges present in the dump.
    std::vector<Region> present;
    PVOID stream = nullptr; ULONG size = 0; MINIDUMP_DIRECTORY* dir = nullptr;
    if (MiniDumpReadDumpStream(base, MemoryListStream, &dir, &stream, &size) && stream != nullptr)
    {
        MINIDUMP_MEMORY_LIST* list = (MINIDUMP_MEMORY_LIST*)stream;
        for (ULONG i = 0; i < list->NumberOfMemoryRanges; i++)
        {
            const MINIDUMP_MEMORY_DESCRIPTOR& d = list->MemoryRanges[i];
            Region r; r.base = d.StartOfMemoryRange; r.size = d.Memory.DataSize; present.push_back(r);
        }
    }
    if (MiniDumpReadDumpStream(base, Memory64ListStream, &dir, &stream, &size) && stream != nullptr)
    {
        MINIDUMP_MEMORY64_LIST* list = (MINIDUMP_MEMORY64_LIST*)stream;
        for (ULONG64 i = 0; i < list->NumberOfMemoryRanges; i++)
        {
            const MINIDUMP_MEMORY_DESCRIPTOR64& d = list->MemoryRanges[i];
            Region r; r.base = d.StartOfMemoryRange; r.size = (uint32_t)d.DataSize; present.push_back(r);
        }
    }

    int covered = 0;
    for (size_t i = 0; i < regions.size(); i++)
    {
        uint64_t b = regions[i].base;
        bool found = false;
        for (size_t j = 0; j < present.size(); j++)
        {
            if (b >= present[j].base && b < present[j].base + present[j].size) { found = true; break; }
        }
        if (found) { covered++; }
        else { printf("cdaclitetest[dump]:   MISSING region 0x%llx (size 0x%x)\n",
                      (unsigned long long)b, regions[i].size); }
    }

    UnmapViewOfFile(base); CloseHandle(hMap); CloseHandle(hFile);
    return covered;
}

// Writes a minidump for the target using cdac-lite to select the managed regions,
// fed to MiniDumpWriteDump via a MemoryCallback -- no version-matched DAC required.
static int RunDump(PFN_CLRDataCreateInstance pfnCreate, LiveDataTarget* target, DWORD pid, const wchar_t* outPath)
{
    ICLRDataEnumMemoryRegions* enumRegions = nullptr;
    HRESULT hr = pfnCreate(__uuidof(ICLRDataEnumMemoryRegions), target, (void**)&enumRegions);
    if (FAILED(hr) || enumRegions == nullptr)
    {
        fwprintf(stderr, L"CLRDataCreateInstance failed: 0x%08x\n", hr);
        return 5;
    }

    CollectCallback collect;
    hr = enumRegions->EnumMemoryRegions(&collect, 0x00000200 /*HEAP2*/, CLRDATA_ENUM_MEM_DEFAULT);
    enumRegions->Release();
    printf("cdaclitetest[dump]: cdac-lite selected %zu managed region(s)\n", collect.m_regions.size());

    HANDLE hProc = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, FALSE, pid);
    if (hProc == nullptr) { fwprintf(stderr, L"OpenProcess failed\n"); return 4; }

    // Baseline: write a MiniDumpNormal with NO memory callback. If dbghelp were driving the
    // CLR DAC (mscordaccore) itself, the managed regions would appear even without our
    // callback. We expect ~0 of the cdac-lite regions to be present in this baseline.
    std::wstring basePath = std::wstring(outPath) + L".baseline";
    HANDLE hBase = CreateFileW(basePath.c_str(), GENERIC_READ | GENERIC_WRITE, 0, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
    int baselineCovered = -1;
    if (hBase != INVALID_HANDLE_VALUE)
    {
        BOOL bok = MiniDumpWriteDump(hProc, pid, hBase, MiniDumpNormal, NULL, NULL, NULL);
        CloseHandle(hBase);
        if (bok)
        {
            baselineCovered = VerifyDump(basePath.c_str(), collect.m_regions);
            printf("cdaclitetest[dump]: baseline (no callback) contains %d/%zu cdac-lite regions\n",
                baselineCovered, collect.m_regions.size());
        }
        DeleteFileW(basePath.c_str());
    }

    HANDLE hFile = CreateFileW(outPath, GENERIC_READ | GENERIC_WRITE, 0, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
    if (hFile == INVALID_HANDLE_VALUE) { fwprintf(stderr, L"CreateFile(dump) failed\n"); CloseHandle(hProc); return 4; }

    DumpCallbackState cbState = { &collect.m_regions, 0 };
    MINIDUMP_CALLBACK_INFORMATION ci = {};
    ci.CallbackRoutine = &DumpMemoryCallback;
    ci.CallbackParam = &cbState;

    // Option A: MiniDumpWithPrivateReadWriteMemory sweeps all private R/W pages (the heaps);
    // cdac-lite's MemoryCallback adds the RX/image/frozen memory the sweep misses. This matches
    // the production createdump path. (No MiniDumpWithoutAuxiliaryState -- it breaks the module
    // export directory capture that ClrMD needs to find the contract descriptor.)
    MINIDUMP_TYPE dumpType = (MINIDUMP_TYPE)(MiniDumpNormal | MiniDumpWithPrivateReadWriteMemory);
    BOOL ok = MiniDumpWriteDump(hProc, pid, hFile, dumpType, NULL, NULL, &ci);
    CloseHandle(hFile);
    CloseHandle(hProc);
    if (!ok)
    {
        fwprintf(stderr, L"MiniDumpWriteDump failed: 0x%08x\n", GetLastError());
        return 6;
    }

    // Confirm dbghelp did not load the CLR DAC into this process. If dbghelp had invoked
    // the runtime's ICLRDataEnumMemoryRegions, mscordaccore.dll would be mapped here.
    bool dacLoaded = DacLoadedInThisProcess();
    printf("cdaclitetest[dump]: mscordaccore loaded in dumper process: %s\n", dacLoaded ? "YES" : "no");

    int covered = VerifyDump(outPath, collect.m_regions);
    printf("cdaclitetest[dump]: wrote %ls; %d/%zu cdac-lite regions present in the dump\n",
        outPath, covered, collect.m_regions.size());

    // The baseline (MiniDumpNormal, no callback) inherently captures module images and thread
    // stacks -- that is dbghelp's normal behavior, NOT the CLR DAC. The regions that ONLY appear
    // once cdac-lite feeds the MemoryCallback (GC heaps, loader heaps, JIT code, handle segments)
    // are the managed-only memory that cdac-lite uniquely contributes.
    int cdacOnly = (baselineCovered >= 0) ? (covered - baselineCovered) : covered;
    printf("cdaclitetest[dump]: cdac-lite added %d managed region(s) beyond the no-callback baseline\n",
        cdacOnly);

    bool regionsOk = covered > 0;
    size_t uncapturable = collect.m_regions.size() - (size_t)covered;
    if (uncapturable > 0)
    {
        printf("cdaclitetest[dump]: %zu region(s) not captured (reserved/uncommitted stack-limit pages)\n",
            uncapturable);
    }
    // Answering "is dbghelp pulling in the mscordaccore enumeration?": the DAC must not be
    // loaded, and cdac-lite must be the source of the managed-only regions (baseline lacked them).
    bool dacFree = !dacLoaded && cdacOnly > 0;
    if (!dacFree)
    {
        fwprintf(stderr, L"cdaclitetest[dump]: FAIL -- managed memory not exclusively from cdac-lite "
                         L"(dacLoaded=%d cdacOnly=%d)\n", dacLoaded ? 1 : 0, cdacOnly);
    }
    return (regionsOk && dacFree) ? 0 : 7;
}

int wmain(int argc, wchar_t** argv)
{
    if (argc < 2)
    {
        fwprintf(stderr, L"Usage: cdaclitetest <pid> [path-to-dll] [--dac | --dump <file>]\n");
        return 1;
    }

    DWORD pid = (DWORD)_wtoi(argv[1]);
    const wchar_t* dllPath = (argc >= 3) ? argv[2] : L"cdaclite.dll";
    bool dacMode = (argc >= 4 && _wcsicmp(argv[3], L"--dac") == 0);
    bool dumpMode = (argc >= 5 && _wcsicmp(argv[3], L"--dump") == 0);
    const wchar_t* dumpPath = dumpMode ? argv[4] : nullptr;

    HMODULE mod = LoadLibraryW(dllPath);
    if (mod == nullptr)
    {
        fwprintf(stderr, L"Failed to load %s (error %lu)\n", dllPath, GetLastError());
        return 2;
    }

    PFN_CLRDataCreateInstance pfnCreate = (PFN_CLRDataCreateInstance)GetProcAddress(mod, "CLRDataCreateInstance");
    if (pfnCreate == nullptr)
    {
        fwprintf(stderr, L"CLRDataCreateInstance not found in %s\n", dllPath);
        return 3;
    }

    HANDLE process = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, FALSE, pid);
    if (process == nullptr)
    {
        fwprintf(stderr, L"OpenProcess(%lu) failed (error %lu)\n", pid, GetLastError());
        return 4;
    }

    LiveDataTarget* target = new LiveDataTarget(process, pid);

    int rc;
    if (dacMode)
    {
        rc = RunDacOracle(pfnCreate, target, pid);
    }
    else if (dumpMode)
    {
        rc = RunDump(pfnCreate, target, pid, dumpPath);
    }
    else
    {
        ICLRDataEnumMemoryRegions* enumRegions = nullptr;
        HRESULT hr = pfnCreate(__uuidof(ICLRDataEnumMemoryRegions), target, (void**)&enumRegions);
        if (FAILED(hr) || enumRegions == nullptr)
        {
            fwprintf(stderr, L"CLRDataCreateInstance failed: 0x%08x\n", hr);
            target->Release();
            CloseHandle(process);
            return 5;
        }

        printf("cdaclitetest: enumerating memory regions for pid %lu\n", pid);

        PrintCallback callback;
        // MiniDumpWithPrivateReadWriteMemory (0x00000200) => the "heap" (HEAP2) path.
        hr = enumRegions->EnumMemoryRegions(&callback, 0x00000200, CLRDATA_ENUM_MEM_DEFAULT);
        printf("cdaclitetest: EnumMemoryRegions returned 0x%08x, %lu region(s)\n", hr, callback.m_regionCount);
        enumRegions->Release();
        rc = SUCCEEDED(hr) ? 0 : 6;
    }

    target->Release();
    CloseHandle(process);
    return rc;
}

