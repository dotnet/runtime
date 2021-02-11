// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <pal_io_common.h>

PALEXPORT intptr_t SystemIoPortsNative_SerialPortOpen(const char * name);
PALEXPORT int SystemIoPortsNative_SerialPortClose(intptr_t fd);
PALEXPORT int32_t SystemIoPortsNative_Read(intptr_t fd, void* buffer, int32_t bufferSize);
PALEXPORT int32_t SystemIoPortsNative_Write(intptr_t fd, const void* buffer, int32_t bufferSize);
PALEXPORT int32_t SystemIoPortsNative_Poll(PollEvent* pollEvents, uint32_t eventCount, int32_t milliseconds, uint32_t* triggered);
PALEXPORT int32_t SystemIoPortsNative_Shutdown(intptr_t socket, int32_t socketShutdown);
PALEXPORT int32_t SystemIoPortsNative_ConvertErrorPlatformToPal(int32_t platformErrno);
PALEXPORT int32_t SystemIoPortsNative_ConvertErrorPalToPlatform(int32_t error);
PALEXPORT const char* SystemIoPortsNative_StrErrorR(int32_t platformErrno, char* buffer, int32_t bufferSize);
