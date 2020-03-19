// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_types.h"
#include "pal_compiler.h"

PALEXPORT intptr_t SystemIoPortsNative_SerialPortOpen(const char * name);
PALEXPORT int SystemIoPortsNative_SerialPortClose(intptr_t fd);
