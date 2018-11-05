// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_types.h"
#include "pal_compiler.h"

DLLEXPORT intptr_t SystemIoPortsNative_SerialPortOpen(const char * name);
DLLEXPORT int SystemIoPortsNative_SerialPortClose(intptr_t fd);
