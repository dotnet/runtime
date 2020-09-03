// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_compiler.h"
#include "pal_types.h"

typedef struct LowLevelMonitor LowLevelMonitor;

PALEXPORT LowLevelMonitor *SystemNative_LowLevelMonitor_Create(void);

PALEXPORT void SystemNative_LowLevelMonitor_Destroy(LowLevelMonitor* monitor);

PALEXPORT void SystemNative_LowLevelMonitor_Acquire(LowLevelMonitor* monitor);

PALEXPORT void SystemNative_LowLevelMonitor_Release(LowLevelMonitor* monitor);

PALEXPORT void SystemNative_LowLevelMonitor_Wait(LowLevelMonitor* monitor);

PALEXPORT void SystemNative_LowLevelMonitor_Signal_Release(LowLevelMonitor* monitor);
