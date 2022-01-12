// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

extern "C" uint16_t __stdcall CaptureStackBackTrace(uint32_t, uint32_t, void*, uint32_t*);
inline uint16_t PalCaptureStackBackTrace(uint32_t arg1, uint32_t arg2, void* arg3, uint32_t* arg4)
{
    return CaptureStackBackTrace(arg1, arg2, arg3, arg4);
}

extern "C" UInt32_BOOL __stdcall CloseHandle(HANDLE);
inline UInt32_BOOL PalCloseHandle(HANDLE arg1)
{
    return CloseHandle(arg1);
}

extern "C" UInt32_BOOL __stdcall CreateDirectoryW(LPCWSTR, LPSECURITY_ATTRIBUTES);
inline UInt32_BOOL PalCreateDirectoryW(LPCWSTR arg1, LPSECURITY_ATTRIBUTES arg2)
{
    return CreateDirectoryW(arg1, arg2);
}

extern "C" void __stdcall DeleteCriticalSection(CRITICAL_SECTION *);
inline void PalDeleteCriticalSection(CRITICAL_SECTION * arg1)
{
    DeleteCriticalSection(arg1);
}

extern "C" UInt32_BOOL __stdcall DuplicateHandle(HANDLE, HANDLE, HANDLE, HANDLE *, uint32_t, UInt32_BOOL, uint32_t);
inline UInt32_BOOL PalDuplicateHandle(HANDLE arg1, HANDLE arg2, HANDLE arg3, HANDLE * arg4, uint32_t arg5, UInt32_BOOL arg6, uint32_t arg7)
{
    return DuplicateHandle(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
}

extern "C" void __stdcall EnterCriticalSection(CRITICAL_SECTION *);
inline void PalEnterCriticalSection(CRITICAL_SECTION * arg1)
{
    EnterCriticalSection(arg1);
}

extern "C" uint32_t __stdcall EventRegister(const GUID *, void *, void *, REGHANDLE *);
inline uint32_t PalEventRegister(const GUID * arg1, void * arg2, void * arg3, REGHANDLE * arg4)
{
    return EventRegister(arg1, arg2, arg3, arg4);
}

extern "C" uint32_t __stdcall EventUnregister(REGHANDLE);
inline uint32_t PalEventUnregister(REGHANDLE arg1)
{
    return EventUnregister(arg1);
}

extern "C" uint32_t __stdcall EventWrite(REGHANDLE, const EVENT_DESCRIPTOR *, uint32_t, EVENT_DATA_DESCRIPTOR *);
inline uint32_t PalEventWrite(REGHANDLE arg1, const EVENT_DESCRIPTOR * arg2, uint32_t arg3, EVENT_DATA_DESCRIPTOR * arg4)
{
    return EventWrite(arg1, arg2, arg3, arg4);
}

extern "C" void __stdcall FlushProcessWriteBuffers();
inline void PalFlushProcessWriteBuffers()
{
    FlushProcessWriteBuffers();
}

extern "C" HANDLE __stdcall GetCurrentProcess();
inline HANDLE PalGetCurrentProcess()
{
    return GetCurrentProcess();
}

extern "C" uint32_t __stdcall GetCurrentProcessId();
inline uint32_t PalGetCurrentProcessId()
{
    return GetCurrentProcessId();
}

extern "C" HANDLE __stdcall GetCurrentThread();
inline HANDLE PalGetCurrentThread()
{
    return GetCurrentThread();
}

#ifdef UNICODE
_Success_(return != 0 && return < nSize)
extern "C" uint32_t __stdcall GetEnvironmentVariableW(_In_opt_ LPCWSTR lpName, _Out_writes_to_opt_(nSize, return + 1) LPWSTR lpBuffer, _In_ uint32_t nSize);
inline uint32_t PalGetEnvironmentVariable(_In_opt_ LPCWSTR lpName, _Out_writes_to_opt_(nSize, return + 1) LPWSTR lpBuffer, _In_ uint32_t nSize)
{
    return GetEnvironmentVariableW(lpName, lpBuffer, nSize);
}
#else
_Success_(return != 0 && return < nSize)
extern "C" uint32_t __stdcall GetEnvironmentVariableA(_In_opt_ LPCSTR lpName, _Out_writes_to_opt_(nSize, return + 1) LPSTR lpBuffer, _In_ uint32_t nSize);
inline uint32_t PalGetEnvironmentVariable(_In_opt_ LPCSTR lpName, _Out_writes_to_opt_(nSize, return + 1) LPSTR lpBuffer, _In_ uint32_t nSize)
{
    return GetEnvironmentVariableA(lpName, lpBuffer, nSize);
}
#endif

extern "C" void * __stdcall GetProcAddress(HANDLE, const char *);
inline void * PalGetProcAddress(HANDLE arg1, const char * arg2)
{
    return GetProcAddress(arg1, arg2);
}

extern "C" UInt32_BOOL __stdcall InitializeCriticalSectionEx(CRITICAL_SECTION *, uint32_t, uint32_t);
inline UInt32_BOOL PalInitializeCriticalSectionEx(CRITICAL_SECTION * arg1, uint32_t arg2, uint32_t arg3)
{
    return InitializeCriticalSectionEx(arg1, arg2, arg3);
}

extern "C" UInt32_BOOL __stdcall IsDebuggerPresent();
inline UInt32_BOOL PalIsDebuggerPresent()
{
    return IsDebuggerPresent();
}

extern "C" void __stdcall LeaveCriticalSection(CRITICAL_SECTION *);
inline void PalLeaveCriticalSection(CRITICAL_SECTION * arg1)
{
    LeaveCriticalSection(arg1);
}

extern "C" HANDLE __stdcall LoadLibraryExW(const WCHAR *, HANDLE, uint32_t);
inline HANDLE PalLoadLibraryExW(const WCHAR * arg1, HANDLE arg2, uint32_t arg3)
{
    return LoadLibraryExW(arg1, arg2, arg3);
}

extern "C" UInt32_BOOL __stdcall QueryPerformanceCounter(LARGE_INTEGER *);
inline UInt32_BOOL PalQueryPerformanceCounter(LARGE_INTEGER * arg1)
{
    return QueryPerformanceCounter(arg1);
}

extern "C" UInt32_BOOL __stdcall QueryPerformanceFrequency(LARGE_INTEGER *);
inline UInt32_BOOL PalQueryPerformanceFrequency(LARGE_INTEGER * arg1)
{
    return QueryPerformanceFrequency(arg1);
}

extern "C" void __stdcall RaiseException(uint32_t, uint32_t, uint32_t, const uint32_t *);
inline void PalRaiseException(uint32_t arg1, uint32_t arg2, uint32_t arg3, const uint32_t * arg4)
{
    RaiseException(arg1, arg2, arg3, arg4);
}

extern "C" UInt32_BOOL __stdcall ResetEvent(HANDLE);
inline UInt32_BOOL PalResetEvent(HANDLE arg1)
{
    return ResetEvent(arg1);
}

extern "C" UInt32_BOOL __stdcall SetEvent(HANDLE);
inline UInt32_BOOL PalSetEvent(HANDLE arg1)
{
    return SetEvent(arg1);
}

extern "C" uint32_t __stdcall WaitForSingleObjectEx(HANDLE, uint32_t, UInt32_BOOL);
inline uint32_t PalWaitForSingleObjectEx(HANDLE arg1, uint32_t arg2, UInt32_BOOL arg3)
{
    return WaitForSingleObjectEx(arg1, arg2, arg3);
}

#ifdef PAL_REDHAWK_INCLUDED
extern "C" void __stdcall GetSystemTimeAsFileTime(FILETIME *);
inline void PalGetSystemTimeAsFileTime(FILETIME * arg1)
{
    GetSystemTimeAsFileTime(arg1);
}

extern "C" void __stdcall RaiseFailFastException(PEXCEPTION_RECORD, PCONTEXT, uint32_t);
inline void PalRaiseFailFastException(PEXCEPTION_RECORD arg1, PCONTEXT arg2, uint32_t arg3)
{
    RaiseFailFastException(arg1, arg2, arg3);
}
#endif
