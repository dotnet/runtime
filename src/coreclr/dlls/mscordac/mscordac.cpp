// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <windows.h>

EXTERN_C BOOL WINAPI DllMain2(HANDLE instance, DWORD reason, LPVOID reserved);

// This is a workaround for missing exports on Linux. Defining DllMain forwarder here makes Linux linker export DllMain and other
// methods built under debug/daccess in the final binary.
EXTERN_C
#ifdef HOST_UNIX
DLLEXPORT // For Win32 PAL LoadLibrary emulation
#endif
BOOL WINAPI DllMain(HANDLE instance, DWORD reason, LPVOID reserved)
{
    return DllMain2(instance, reason, reserved);
}
