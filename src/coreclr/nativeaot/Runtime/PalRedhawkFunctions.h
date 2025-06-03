// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

uint16_t PalCaptureStackBackTrace(uint32_t arg1, uint32_t arg2, void* arg3, uint32_t* arg4);
UInt32_BOOL PalCloseHandle(HANDLE arg1);
void PalFlushProcessWriteBuffers();
uint32_t PalGetCurrentProcessId();

#ifdef UNICODE
uint32_t PalGetEnvironmentVariable(_In_opt_ LPCWSTR lpName, _Out_writes_to_opt_(nSize, return + 1) LPWSTR lpBuffer, _In_ uint32_t nSize);
#else
uint32_t PalGetEnvironmentVariable(_In_opt_ LPCSTR lpName, _Out_writes_to_opt_(nSize, return + 1) LPSTR lpBuffer, _In_ uint32_t nSize);
#endif

UInt32_BOOL PalResetEvent(HANDLE arg1);
UInt32_BOOL PalSetEvent(HANDLE arg1);
uint32_t PalWaitForSingleObjectEx(HANDLE arg1, uint32_t arg2, UInt32_BOOL arg3);

#ifdef PAL_REDHAWK_INCLUDED
void PalGetSystemTimeAsFileTime(FILETIME * arg1);
#endif
