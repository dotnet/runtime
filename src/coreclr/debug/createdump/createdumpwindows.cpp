// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "createdump.h"
#include "cdaclite.h"
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

typedef NTSTATUS (NTAPI *PFN_NT_PROCESS_OPERATION)(HANDLE);

//
// cdac-lite integration: instead of letting dbghelp's auxiliary provider drive the legacy DAC
// (mscordaccore) to select managed memory for heap dumps, we ask cdac-lite -- a small native
// component that reads the runtime's contract/data descriptors -- to enumerate the managed
// regions, and feed those to MiniDumpWriteDump via a memory callback. The implementation is
// statically linked into createdump and enabled with DOTNET_DbgUseCdacLite=1.
//

class ProcessSuspension
{
    HANDLE m_process;
    PFN_NT_PROCESS_OPERATION m_resumeProcess;
    bool m_suspended;

public:
    ProcessSuspension(HANDLE process) :
        m_process(process),
        m_resumeProcess(nullptr),
        m_suspended(false)
    {
        HMODULE ntdll = GetModuleHandleW(L"ntdll.dll");
        if (ntdll == nullptr)
        {
            return;
        }

        PFN_NT_PROCESS_OPERATION suspendProcess =
            reinterpret_cast<PFN_NT_PROCESS_OPERATION>(GetProcAddress(ntdll, "NtSuspendProcess"));
        m_resumeProcess =
            reinterpret_cast<PFN_NT_PROCESS_OPERATION>(GetProcAddress(ntdll, "NtResumeProcess"));
        if (suspendProcess != nullptr && m_resumeProcess != nullptr && suspendProcess(m_process) == 0)
        {
            m_suspended = true;
        }
    }

    ~ProcessSuspension()
    {
        if (m_suspended)
        {
            m_resumeProcess(m_process);
        }
    }

    bool IsSuspended() const { return m_suspended; }
};

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
        if (machine == nullptr)
        {
            return E_POINTER;
        }
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
        if (size == nullptr)
        {
            return E_POINTER;
        }
        *size = sizeof(void*);
        return S_OK;
    }

    STDMETHOD(GetImageBase)(LPCWSTR moduleName, CLRDATA_ADDRESS* baseAddress)
    {
        if (moduleName == nullptr || baseAddress == nullptr)
        {
            return E_POINTER;
        }

        *baseAddress = 0;
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
    STDMETHOD(GetThreadContext)(ULONG32 threadId, ULONG32 contextFlags, ULONG32 contextSize, PBYTE context)
    {
        if (context == nullptr || contextSize < sizeof(CONTEXT))
        {
            return E_INVALIDARG;
        }

        HANDLE thread = OpenThread(
            THREAD_GET_CONTEXT | THREAD_QUERY_INFORMATION | THREAD_SUSPEND_RESUME,
            FALSE,
            threadId);
        if (thread == nullptr)
        {
            return HRESULT_FROM_WIN32(GetLastError());
        }

        if (SuspendThread(thread) == static_cast<DWORD>(-1))
        {
            DWORD error = GetLastError();
            CloseHandle(thread);
            return HRESULT_FROM_WIN32(error);
        }

        memset(context, 0, contextSize);
        CONTEXT* threadContext = reinterpret_cast<CONTEXT*>(context);
        threadContext->ContextFlags = contextFlags;
        BOOL result = ::GetThreadContext(thread, threadContext);
        DWORD error = result ? ERROR_SUCCESS : GetLastError();
        ResumeThread(thread);
        CloseHandle(thread);
        return result ? S_OK : HRESULT_FROM_WIN32(error);
    }
    STDMETHOD(SetThreadContext)(ULONG32, ULONG32, PBYTE) { return E_NOTIMPL; }
    STDMETHOD(Request)(ULONG32, ULONG32, BYTE*, ULONG32, BYTE*) { return E_NOTIMPL; }
};

// Collects the [address, size) regions reported by cdac-lite.
struct CdacRegionCollector
{
    std::vector<MINIDUMP_MEMORY_DESCRIPTOR64> m_regions;

    static HRESULT Collect(void* context, uint64_t address, uint32_t size)
    {
        CdacRegionCollector* collector = static_cast<CdacRegionCollector*>(context);
        MINIDUMP_MEMORY_DESCRIPTOR64 region;
        region.StartOfMemoryRange = address;
        region.DataSize = size;
        collector->m_regions.push_back(region);
        return S_OK;
    }
};

static void CdacLog(void*, const char* message)
{
    printf_status("cdaclite: %s\n", message);
}

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

// Writes a dump for the target process using cdac-lite for managed-memory selection instead of
// the legacy DAC.
static bool
TryCreateDumpWithCdacLite(HANDLE hProcess, DWORD pid, HANDLE hFile, DumpType requestedDumpType)
{
    bool result = false;
    bool heapTier = requestedDumpType == DumpType::Heap;

    ProcessSuspension suspension(hProcess);
    if (!suspension.IsSuspended())
    {
        printf_error("cdac-lite: failed to suspend target process\n");
        return false;
    }

    ReleaseHolder<ProcessDataTarget> dataTarget{ new ProcessDataTarget(hProcess) };
    CLRDATA_ADDRESS clrBase = 0;
    HRESULT hr = dataTarget->GetImageBase(TARGET_MAIN_CLR_DLL_NAME_W, &clrBase);
    if (FAILED(hr))
    {
        printf_error("cdac-lite: runtime module not found (%08x)\n", hr);
        return false;
    }

    CdacRegionCollector collector;
    // miniDumpFlags: MiniDumpWithPrivateReadWriteMemory (0x200) => heap tier (full GC heap +
    // R/W sweep); MiniDumpNormal (0) => Normal tier (stack-walk-reachable state only).
    ULONG32 enumFlags = heapTier ? MiniDumpWithPrivateReadWriteMemory : MiniDumpNormal;
    hr = cdac::EnumerateMemoryRegions(
        dataTarget,
        clrBase,
        enumFlags,
        &CdacRegionCollector::Collect,
        &collector,
        &CdacLog);
    if (FAILED(hr))
    {
        printf_error("cdac-lite: EnumMemoryRegions FAILED (%08x)\n", hr);
        return false;
    }
    printf_status("cdac-lite: selected %zu managed region(s) [%s tier]\n",
        collector.m_regions.size(), heapTier ? "heap" : "normal");

    // Preserve the requested dump tier's flags. cdac-lite's memory callback adds the managed memory
    // that the selected tier misses (executable JIT/stub RX pages and image-backed descriptor data).
    // MiniDumpWithoutAuxiliaryState stops dbghelp from loading the legacy DAC (mscordaccore) as an
    // auxiliary provider. Otherwise the DAC's own enumeration would mask whether cdac-lite is
    // self-sufficient and add its startup cost.
    MINIDUMP_TYPE dumpType = static_cast<MINIDUMP_TYPE>(
        GetMiniDumpType(requestedDumpType) |
        MiniDumpWithoutAuxiliaryState);

    CdacMemoryCallbackState state = { &collector.m_regions, 0 };
    MINIDUMP_CALLBACK_INFORMATION callbackInfo = {};
    callbackInfo.CallbackRoutine = &CdacMemoryCallback;
    callbackInfo.CallbackParam = &state;

    int retryCount = 10;
    for (int i = 0; i <= retryCount; i++)
    {
        state.index = 0;
        if (MiniDumpWriteDump(hProcess, pid, hFile, dumpType, NULL, NULL, &callbackInfo))
        {
            result = true;
            break;
        }

        int error = GetLastError();
        if (error != ERROR_PARTIAL_COPY || i == retryCount)
        {
            printf_error("cdac-lite: MiniDumpWriteDump - %s\n", GetLastErrorString().c_str());
            break;
        }

        printf_error("cdac-lite: retry %d of MiniDumpWriteDump due to - %s\n", i, GetLastErrorString().c_str());
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

    hProcess = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ | PROCESS_SUSPEND_RESUME, FALSE, pid);
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

    char envVal[8];
    DWORD envLen = GetEnvironmentVariableA("DOTNET_DbgUseCdacLite", envVal, ARRAY_SIZE(envVal));
    bool useCdacLite = (envLen == 1 && envVal[0] == '1');

    bool cdacHandled = false;
    // cdac-lite selects managed memory for non-full dumps; full dumps already capture everything.
    if (useCdacLite && options.DumpType != DumpType::Full)
    {
        // Heap tier for withheap dumps; Normal tier for normal/triage (stack-walk-reachable only).
        bool heapTier = (options.DumpType == DumpType::Heap);
        printf_status("cdac-lite: collecting managed memory (DOTNET_DbgUseCdacLite=1, %s tier)\n",
            heapTier ? "heap" : "normal");
        cdacHandled = true;
        result = TryCreateDumpWithCdacLite(hProcess, pid, hFile, options.DumpType);
        if (!result)
        {
            printf_error("cdac-lite: dump generation failed\n");
        }
    }

    if (!cdacHandled)
    {
        MINIDUMP_TYPE dumpType = GetMiniDumpType(options.DumpType);
        if (useCdacLite)
        {
            // Full dumps do not need managed-memory enumeration, but strict cDAC-lite testing still
            // excludes auxiliary providers so the legacy DAC cannot participate.
            dumpType = static_cast<MINIDUMP_TYPE>(dumpType | MiniDumpWithoutAuxiliaryState);
            printf_status("cdac-lite: auxiliary providers disabled for full dump\n");
        }

        int retryCount = 10;
        // Retry the write dump on ERROR_PARTIAL_COPY
        for (int i = 0; i <= retryCount; i++)
        {
            if (MiniDumpWriteDump(hProcess, pid, hFile, dumpType, NULL, NULL, NULL))
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