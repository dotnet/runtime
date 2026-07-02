// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "createdump.h"
#include <psapi.h>

// The Windows SDK (winternl.h) we use doesn't have the necessary field (InheritedFromUniqueProcessId)
typedef struct _PROCESS_BASIC_INFORMATION_ {
    NTSTATUS ExitStatus;
    PPEB PebBaseAddress;
    ULONG_PTR AffinityMask;
    KPRIORITY BasePriority;
    ULONG_PTR UniqueProcessId;
    ULONG_PTR InheritedFromUniqueProcessId;
} PROCESS_BASIC_INFORMATION_;

typedef HRESULT (STDAPICALLTYPE* PFN_CLRDataCreateInstance)(REFIID iid, ICLRDataTarget* target, void** iface);

//
// cdac-lite integration: instead of letting dbghelp's auxiliary provider drive the legacy DAC
// (mscordaccore) to select managed memory for heap dumps, we ask cdac-lite -- a small native
// component that reads the runtime's contract/data descriptors -- to enumerate the managed
// regions, and feed those to MiniDumpWriteDump via a memory callback. Enabled with
// DOTNET_DbgUseCdacLite=1. The cdac-lite DLL is loaded from DOTNET_DbgCdacLitePath if set,
// otherwise from next to coreclr.dll in the target process.
//

// Minimal ICLRDataTarget over a live target process (ReadProcessMemory + module base lookup).
class ProcessDataTarget : public ICLRDataTarget
{
    LONG m_ref;
    HANDLE m_process;

public:
    ProcessDataTarget(HANDLE process) : m_ref(1), m_process(process) { }

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

    STDMETHOD(GetMachineType)(ULONG32* machine)
    {
#if defined(_M_ARM64)
        *machine = IMAGE_FILE_MACHINE_ARM64;
#elif defined(_M_ARM)
        *machine = IMAGE_FILE_MACHINE_ARMNT;
#elif defined(_M_IX86)
        *machine = IMAGE_FILE_MACHINE_I386;
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
        HMODULE modules[1024];
        DWORD needed = 0;
        if (!EnumProcessModulesEx(m_process, modules, sizeof(modules), &needed, LIST_MODULES_ALL))
        {
            return E_FAIL;
        }
        DWORD count = needed / sizeof(HMODULE);
        if (count > ARRAY_SIZE(modules))
        {
            count = ARRAY_SIZE(modules);
        }
        WCHAR name[MAX_PATH];
        for (DWORD i = 0; i < count; i++)
        {
            if (GetModuleBaseNameW(m_process, modules[i], name, ARRAY_SIZE(name)) > 0 &&
                _wcsicmp(name, moduleName) == 0)
            {
                *baseAddress = (CLRDATA_ADDRESS)(ULONG_PTR)modules[i];
                return S_OK;
            }
        }
        return E_FAIL;
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

// Collects the [address, size) regions reported by cdac-lite's EnumMemoryRegions.
class CdacRegionCollector : public ICLRDataEnumMemoryRegionsCallback
{
    LONG m_ref;

public:
    std::vector<MINIDUMP_MEMORY_DESCRIPTOR64> m_regions;

    CdacRegionCollector() : m_ref(1) { }

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
        *ppvObject = nullptr;
        return E_NOINTERFACE;
    }

    STDMETHOD_(ULONG, AddRef)() { return InterlockedIncrement(&m_ref); }
    STDMETHOD_(ULONG, Release)() { return InterlockedDecrement(&m_ref); }

    STDMETHOD(EnumMemoryRegion)(CLRDATA_ADDRESS address, ULONG32 size)
    {
        MINIDUMP_MEMORY_DESCRIPTOR64 region;
        region.StartOfMemoryRange = address;
        region.DataSize = size;
        m_regions.push_back(region);
        return S_OK;
    }
};

struct CdacMemoryCallbackState
{
    const std::vector<MINIDUMP_MEMORY_DESCRIPTOR64>* regions;
    size_t index;
};

// MiniDumpWriteDump memory callback: supplies one cdac-lite region per MemoryCallback invocation.
static BOOL CALLBACK
CdacMemoryCallback(PVOID param, const PMINIDUMP_CALLBACK_INPUT input, PMINIDUMP_CALLBACK_OUTPUT output)
{
    CdacMemoryCallbackState* state = (CdacMemoryCallbackState*)param;
    if (input->CallbackType == MemoryCallback)
    {
        if (state->index < state->regions->size())
        {
            const MINIDUMP_MEMORY_DESCRIPTOR64& region = (*state->regions)[state->index++];
            output->MemoryBase = region.StartOfMemoryRange;
            output->MemorySize = (ULONG)region.DataSize;
        }
        else
        {
            output->MemoryBase = 0;
            output->MemorySize = 0;
        }
    }
    return TRUE;
}

// Determines the cdac-lite DLL path: DOTNET_DbgCdacLitePath env var, else next to the target's coreclr.dll.
static bool
GetCdacLitePath(HANDLE hProcess, std::string& path)
{
    char envPath[MAX_LONGPATH];
    DWORD envLen = GetEnvironmentVariableA("DOTNET_DbgCdacLitePath", envPath, ARRAY_SIZE(envPath));
    if (envLen > 0 && envLen < ARRAY_SIZE(envPath))
    {
        path.assign(envPath, envLen);
        return true;
    }

    HMODULE modules[1024];
    DWORD needed = 0;
    if (!EnumProcessModulesEx(hProcess, modules, sizeof(modules), &needed, LIST_MODULES_ALL))
    {
        return false;
    }
    DWORD count = needed / sizeof(HMODULE);
    if (count > ARRAY_SIZE(modules))
    {
        count = ARRAY_SIZE(modules);
    }
    char name[MAX_PATH];
    for (DWORD i = 0; i < count; i++)
    {
        if (GetModuleBaseNameA(hProcess, modules[i], name, ARRAY_SIZE(name)) > 0 &&
            _stricmp(name, MAKEDLLNAME_A("coreclr")) == 0)
        {
            char fullPath[MAX_LONGPATH];
            if (GetModuleFileNameExA(hProcess, modules[i], fullPath, ARRAY_SIZE(fullPath)) > 0)
            {
                std::string coreclrPath(fullPath);
                size_t sep = coreclrPath.find_last_of("\\/");
                if (sep != std::string::npos)
                {
                    path.assign(coreclrPath, 0, sep + 1);
                    path.append(MAKEDLLNAME_A("cdaclite"));
                    return true;
                }
            }
        }
    }
    return false;
}

// Writes a dump for the target process using cdac-lite for managed-memory selection instead of
// the legacy DAC. 'heapTier' selects the region set: true = heap dump (GC heaps + private R/W
// sweep); false = Normal dump (stack-walk-reachable state only, no GC heap sweep). Returns false
// if cdac-lite could not be used (the caller falls back to the normal MiniDumpWriteDump path).
static bool
TryCreateDumpWithCdacLite(HANDLE hProcess, DWORD pid, HANDLE hFile, bool heapTier)
{
    std::string cdacLitePath;
    if (!GetCdacLitePath(hProcess, cdacLitePath))
    {
        printf_error("cdac-lite: could not locate cdaclite.dll (set DOTNET_DbgCdacLitePath)\n");
        return false;
    }

    HMODULE cdacLite = LoadLibraryA(cdacLitePath.c_str());
    if (cdacLite == nullptr)
    {
        printf_error("cdac-lite: LoadLibrary(%s) FAILED - %s\n", cdacLitePath.c_str(), GetLastErrorString().c_str());
        return false;
    }

    bool result = false;
    PFN_CLRDataCreateInstance pfnCreate = (PFN_CLRDataCreateInstance)GetProcAddress(cdacLite, "CLRDataCreateInstance");
    if (pfnCreate == nullptr)
    {
        printf_error("cdac-lite: GetProcAddress(CLRDataCreateInstance) FAILED\n");
        return false;
    }

    ReleaseHolder<ProcessDataTarget> dataTarget = new ProcessDataTarget(hProcess);
    ReleaseHolder<ICLRDataEnumMemoryRegions> enumRegions;
    HRESULT hr = pfnCreate(__uuidof(ICLRDataEnumMemoryRegions), dataTarget, (void**)&enumRegions);
    if (FAILED(hr) || enumRegions == nullptr)
    {
        printf_error("cdac-lite: CLRDataCreateInstance(ICLRDataEnumMemoryRegions) FAILED (%08x)\n", hr);
        return false;
    }

    CdacRegionCollector collector;
    // miniDumpFlags: MiniDumpWithPrivateReadWriteMemory (0x200) => heap tier (full GC heap +
    // R/W sweep); MiniDumpNormal (0) => Normal tier (stack-walk-reachable state only).
    ULONG32 enumFlags = heapTier ? MiniDumpWithPrivateReadWriteMemory : MiniDumpNormal;
    hr = enumRegions->EnumMemoryRegions(&collector, enumFlags, CLRDATA_ENUM_MEM_DEFAULT);
    if (FAILED(hr))
    {
        printf_error("cdac-lite: EnumMemoryRegions FAILED (%08x)\n", hr);
        return false;
    }
    printf_status("cdac-lite: selected %zu managed region(s) [%s tier]\n",
        collector.m_regions.size(), heapTier ? "heap" : "normal");

    // Heap tier (DAC heap-dump model): let dbghelp sweep all private read/write pages
    // (MiniDumpWithPrivateReadWriteMemory) to capture the GC/loader/handle heaps -- object bytes
    // included -- the same way the DAC path relies on that sweep. cdac-lite's memory callback adds
    // the memory the sweep misses (executable JIT/stub RX pages, image-backed contract descriptor).
    // Normal tier: MiniDumpNormal (stacks + module headers); cdac-lite supplies the stack-walk
    // code + method metadata via the callback, no R/W heap sweep.
    // (MiniDumpWithoutAuxiliaryState is intentionally NOT set: it breaks ClrMD's module export
    // directory lookup for the contract descriptor. dbghelp does not load the legacy DAC anyway.)
    MINIDUMP_TYPE dumpType = heapTier
        ? (MINIDUMP_TYPE)(MiniDumpNormal | MiniDumpWithPrivateReadWriteMemory)
        : MiniDumpNormal;

    CdacMemoryCallbackState state = { &collector.m_regions, 0 };
    MINIDUMP_CALLBACK_INFORMATION callbackInfo = {};
    callbackInfo.CallbackRoutine = &CdacMemoryCallback;
    callbackInfo.CallbackParam = &state;

    if (MiniDumpWriteDump(hProcess, pid, hFile, dumpType, NULL, NULL, &callbackInfo))
    {
        result = true;
    }
    else
    {
        printf_error("cdac-lite: MiniDumpWriteDump - %s\n", GetLastErrorString().c_str());
    }

    return result;
}

//
// The Windows create dump code
//
bool
CreateDump(const CreateDumpOptions& options)
{
    HANDLE hFile = INVALID_HANDLE_VALUE;
    HANDLE hProcess = NULL;
    bool result = false;

    _ASSERTE(options.CreateDump);
    _ASSERTE(!options.CrashReport);

    AStringHolder pszName = new char[MAX_LONGPATH + 1];
    std::string dumpPath;

    // On Windows, createdump is restricted for security reasons to only the .NET process (parent process) that launched createdump
    PROCESS_BASIC_INFORMATION_ processInformation;
    NTSTATUS status = NtQueryInformationProcess(GetCurrentProcess(), PROCESSINFOCLASS::ProcessBasicInformation, &processInformation, sizeof(processInformation), NULL);
    if (status != 0)
    {
        printf_error("Failed to get parent process id status %d\n", status);
        goto exit;
    }
    int pid = (int)processInformation.InheritedFromUniqueProcessId;

    hProcess = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, FALSE, pid);
    if (hProcess == NULL)
    {
        printf_error("Invalid process id '%d' - %s\n", pid, GetLastErrorString().c_str());
        goto exit;
    }
    if (GetModuleBaseNameA(hProcess, NULL, pszName, MAX_LONGPATH) <= 0)
    {
        printf_error("Get process name FAILED - %s\n", GetLastErrorString().c_str());
        goto exit;
    }
    if (!FormatDumpName(dumpPath, options.DumpPathTemplate, pszName, pid))
    {
        goto exit;
    }
    printf_status("Writing %s for process %d to file %s\n", GetDumpTypeString(options.DumpType), pid, dumpPath.c_str());

    hFile = CreateFileA(dumpPath.c_str(), GENERIC_READ | GENERIC_WRITE, 0, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
    if (hFile == INVALID_HANDLE_VALUE)
    {
        printf_error("Invalid dump path '%s' - %s\n", dumpPath.c_str(), GetLastErrorString().c_str());
        goto exit;
    }

    bool cdacHandled = false;
    {
        char envVal[8];
        DWORD envLen = GetEnvironmentVariableA("DOTNET_DbgUseCdacLite", envVal, ARRAY_SIZE(envVal));
        bool useCdacLite = (envLen == 1 && envVal[0] == '1');
        // cdac-lite selects managed memory for non-full dumps; full dumps already capture everything.
        if (useCdacLite && options.DumpType != DumpType::Full)
        {
            // Heap tier for withheap dumps; Normal tier for normal/triage (stack-walk-reachable only).
            bool heapTier = (options.DumpType == DumpType::Heap);
            printf_status("cdac-lite: collecting managed memory (DOTNET_DbgUseCdacLite=1, %s tier)\n",
                heapTier ? "heap" : "normal");
            result = TryCreateDumpWithCdacLite(hProcess, pid, hFile, heapTier);
            cdacHandled = result;
            if (!result)
            {
                printf_error("cdac-lite: collection failed; falling back to default dump path\n");
            }
        }
    }

    if (!cdacHandled)
    {
        int retryCount = 10;
        // Retry the write dump on ERROR_PARTIAL_COPY
        for (int i = 0; i <= retryCount; i++)
        {
            if (MiniDumpWriteDump(hProcess, pid, hFile, GetMiniDumpType(options.DumpType), NULL, NULL, NULL))
            {
                result = true;
                break;
            }
            else
            {
                int err = GetLastError();
                if (err != ERROR_PARTIAL_COPY || i == retryCount)
                {
                    printf_error("MiniDumpWriteDump - %s\n", GetLastErrorString().c_str());
                    break;
                }
                else
                {
                     printf_error("Retry %d of MiniDumpWriteDump due to - %s\n", i, GetLastErrorString().c_str());
                }
            }
        }
    }

exit:
    if (hProcess != NULL)
    {
        CloseHandle(hProcess);
    }

    if (hFile != INVALID_HANDLE_VALUE)
    {
        CloseHandle(hFile);
    }

    return result;
}

std::string
GetLastErrorString()
{
    DWORD error = GetLastError();
    std::string result;
    LPSTR messageBuffer;
    DWORD length = FormatMessage(
        FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
        NULL,
        error,
        MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
        (LPTSTR)&messageBuffer,
        0,
        NULL);
    if (length > 0)
    {
        result.append(messageBuffer, length);
        LocalFree(messageBuffer);

        // Remove the \r\n at the end of the system message. Assumes that the \r is first.
        size_t found = result.find_last_of('\r');
        if (found != std::string::npos)
        {
            result.erase(found);
        }
        result.append(" ");
    }
    char buffer[64];
    _snprintf_s(buffer, sizeof(buffer), sizeof(buffer), "(%d)", error);
    result.append(buffer);
    return result;
}


typedef DWORD(WINAPI *pfnGetTempPathA)(DWORD nBufferLength, LPSTR  lpBuffer);

static volatile pfnGetTempPathA
g_pfnGetTempPathA = nullptr;


DWORD
GetTempPathWrapper(
    IN DWORD nBufferLength,
    OUT LPSTR lpBuffer)
{
    if (g_pfnGetTempPathA == nullptr)
    {
        HMODULE hKernel32 = LoadLibraryExW(L"kernel32.dll", NULL, LOAD_LIBRARY_SEARCH_SYSTEM32);

        pfnGetTempPathA pLocalGetTempPathA = NULL;
        if (hKernel32 != NULL)
        {
            // store to thread local variable to prevent data race
            pLocalGetTempPathA = (pfnGetTempPathA)::GetProcAddress(hKernel32, "GetTempPath2A");
        }

        if (pLocalGetTempPathA == NULL) // method is only available with Windows 10 Creators Update or later
        {
            g_pfnGetTempPathA = &GetTempPathA;
        }
        else
        {
            g_pfnGetTempPathA = pLocalGetTempPathA;
        }
    }

    return g_pfnGetTempPathA(nBufferLength, lpBuffer);
}